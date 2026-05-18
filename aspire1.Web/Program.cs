using aspire1.Web;
using aspire1.Web.Components;
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

// Add Redis distributed cache and session state with offline-first design
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

        // Configure session state with Redis backing
        builder.Services.AddSession(options =>
        {
            options.Cookie.Name = ".aspire1.Session";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.IdleTimeout = TimeSpan.FromMinutes(30); // Sliding expiration
            options.Cookie.MaxAge = TimeSpan.FromHours(8);  // Absolute maximum
        });

        Console.WriteLine("✅ Redis cache and session state configured successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️  Warning: Could not connect to Redis: {ex.Message}");
        Console.WriteLine("Falling back to in-memory cache and session state.");
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSession();
    }
}
else
{
    Console.WriteLine("⚠️  Redis not configured (local development mode)");
    Console.WriteLine("Using in-memory cache and session state as fallback.");
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSession(options =>
    {
        options.Cookie.Name = ".aspire1.Session";
        options.Cookie.HttpOnly = true;
        options.IdleTimeout = TimeSpan.FromMinutes(30);
    });
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddOutputCache();

builder.Services.AddHttpClient<WeatherApiClient>(client =>
    {
        // This URL uses "https+http://" to indicate HTTPS is preferred over HTTP.
        // When running through AppHost, this resolves to the weatherservice container.
        // When running standalone, fall back to localhost for debugging.
        var serviceUrl = builder.Configuration["services:weatherservice:https:0"]
                        ?? builder.Configuration["services:weatherservice:http:0"]
                        ?? "http://localhost:7002"; // Fallback for standalone debugging

        client.BaseAddress = new Uri(serviceUrl);
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseSession();

app.UseOutputCache();

// Enable Azure App Configuration middleware for dynamic refresh
if (!string.IsNullOrEmpty(builder.Configuration["AppConfig:Endpoint"]))
{
    app.UseAzureAppConfiguration();
}

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
