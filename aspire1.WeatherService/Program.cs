using System.Reflection;
using aspire1.WeatherService.Services;
using Azure.Identity;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.FeatureManagement;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add Azure App Configuration with feature flags
var appConfigEndpoint = builder.Configuration["AppConfig:Endpoint"];
if (!string.IsNullOrEmpty(appConfigEndpoint))
{
    try
    {
        builder.Configuration.AddAzureAppConfiguration(options =>
        {
            options.Connect(new Uri(appConfigEndpoint), new DefaultAzureCredential())
                   .UseFeatureFlags(featureFlagOptions =>
                   {
                       featureFlagOptions.SetRefreshInterval(TimeSpan.FromSeconds(30));
                       // Use sentinel key for cache refresh
                       featureFlagOptions.Select("*", builder.Environment.EnvironmentName);
                   });
        });
    }
    catch (Exception ex)
    {
        // Log warning but continue - fall back to local appsettings.json
        Console.WriteLine($"Warning: Could not connect to Azure App Configuration: {ex.Message}");
        Console.WriteLine("Falling back to local feature flag configuration.");
    }
}

// Add feature management
builder.Services.AddFeatureManagement();

// Add Redis distributed cache with offline-first design
var redisConnectionName = builder.Configuration.GetConnectionString("cache");
if (!string.IsNullOrEmpty(redisConnectionName))
{
    try
    {
        builder.AddRedisClient("cache");
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionName;
        });
        Console.WriteLine("✅ Redis distributed cache configured successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️  Warning: Could not connect to Redis: {ex.Message}");
        Console.WriteLine("Falling back to in-memory distributed cache.");
        builder.Services.AddDistributedMemoryCache();
    }
}
else
{
    Console.WriteLine("⚠️  Redis not configured (local development mode)");
    Console.WriteLine("Using in-memory distributed cache as fallback.");
    builder.Services.AddDistributedMemoryCache();
}

// Add services to the container.
builder.Services.AddProblemDetails();

// Register cached weather service
builder.Services.AddScoped<CachedWeatherService>();

// Capture version info at startup
var version = builder.Configuration["APP_VERSION"] ??
              Assembly.GetExecutingAssembly()
                      .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                      ?.InformationalVersion ?? "unknown";
var commitSha = builder.Configuration["COMMIT_SHA"] ??
                Environment.GetEnvironmentVariable("GITHUB_SHA")?[..7] ?? "local";

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

// Enable Azure App Configuration middleware for dynamic refresh
if (!string.IsNullOrEmpty(builder.Configuration["AppConfig:Endpoint"]))
{
    app.UseAzureAppConfiguration();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => "API service is running. Navigate to /weatherforecast to see sample data.");

app.MapGet("/weatherforecast", async (CachedWeatherService weatherService, IFeatureManager featureManager, CancellationToken cancellationToken) =>
{
    // Check if feature is enabled
    if (!await featureManager.IsEnabledAsync("WeatherForecast"))
    {
        return Results.Json(
            new { error = "Weather forecast feature is currently disabled" },
            statusCode: 503
        );
    }

    var forecasts = await weatherService.GetWeatherForecastAsync(10, cancellationToken);

    // Check if humidity feature is enabled
    var humidityEnabled = await featureManager.IsEnabledAsync("WeatherHumidity");
    if (!humidityEnabled)
    {
        // Strip humidity data when feature is disabled
        forecasts = forecasts.Select(f => f with { Humidity = 0 }).ToArray();
    }

    // Track weather API call
    ApplicationMetrics.WeatherApiCalls.Add(1,
        new KeyValuePair<string, object?>("endpoint", "weatherforecast"),
        new KeyValuePair<string, object?>("feature_enabled", "true"));

    // Track sunny forecasts with temperature categorization
    foreach (var forecast in forecasts.Where(f => f.Summary?.Contains("Sunny", StringComparison.OrdinalIgnoreCase) == true))
    {
        ApplicationMetrics.SunnyForecasts.Add(1,
            new KeyValuePair<string, object?>("temperature_range", ApplicationMetrics.GetTemperatureRange(forecast.TemperatureC)));
    }

    return Results.Ok(forecasts);
})
.WithName("GetWeatherForecast");

// Version endpoint for deployment tracking
app.MapGet("/version", () => new
{
    version,
    commitSha,
    service = "apiservice",
    environment = app.Environment.EnvironmentName,
    timestamp = DateTime.UtcNow
})
.WithName("GetVersion");

// Enhanced health with version for OpenTelemetry correlation
app.MapGet("/health/detailed", async (IFeatureManager featureManager) =>
{
    var showDetailed = await featureManager.IsEnabledAsync("DetailedHealth");

    if (showDetailed)
    {
        return Results.Ok(new
        {
            status = "healthy",
            version,
            commitSha,
            service = "apiservice",
            timestamp = DateTime.UtcNow,
            uptime = Environment.TickCount64 / 1000.0,
            features = new
            {
                detailedHealth = true,
                weatherForecast = await featureManager.IsEnabledAsync("WeatherForecast")
            }
        });
    }

    return Results.Ok(new { status = "healthy" });
})
.WithName("GetDetailedHealth");
app.MapDefaultEndpoints();

app.Run();
