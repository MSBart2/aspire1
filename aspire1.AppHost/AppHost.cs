var builder = DistributedApplication.CreateBuilder(args);

// Capture version for container tags and ACA revision labels
var version = builder.Configuration["VERSION"] ?? "1.0.0-local";
var commitSha = builder.Configuration["COMMIT_SHA"] ??
                Environment.GetEnvironmentVariable("GITHUB_SHA")?[..7] ?? "local";

// Detect if we're running locally without Docker (offline-first development)
var isLocalDev = builder.Environment.EnvironmentName == "Development" &&
                 string.IsNullOrEmpty(builder.Configuration["CONTAINER_REGISTRY"]);

// Add Application Insights for telemetry (only when deploying)
var appInsights = !isLocalDev ? builder.AddAzureApplicationInsights("appinsights") : null;
if (appInsights != null)
{
    Console.WriteLine("✅ Application Insights enabled");
}
else
{
    Console.WriteLine("⚠️  Application Insights disabled for local development");
}

// Add Azure App Configuration (only when deploying)
var appConfig = !isLocalDev ? builder.AddAzureAppConfiguration("appconfig") : null;
if (appConfig != null)
{
    Console.WriteLine("✅ Azure App Configuration enabled");
}
else
{
    Console.WriteLine("⚠️  Azure App Configuration disabled for local development");
}

// Add Redis for distributed caching and session state
IResourceBuilder<IResourceWithConnectionString>? redis = null;
if (!isLocalDev)
{
    // Use Azure Cache for Redis in deployed environments
    redis = builder.AddAzureRedis("cache");
    Console.WriteLine("✅ Azure Cache for Redis enabled (managed service)");
}
else
{
    // Use local Redis container for development
    redis = builder.AddRedis("cache");
    Console.WriteLine("✅ Redis container enabled for local development");
}

var weatherService = builder.AddProject<Projects.aspire1_WeatherService>("weatherservice")
    .WithHttpHealthCheck("/health")
    .WithEnvironment("APP_VERSION", version)
    .WithEnvironment("COMMIT_SHA", commitSha);

// Only reference Azure resources if they were added
if (appInsights != null)
{
    weatherService.WithReference(appInsights);
}
if (appConfig != null)
{
    weatherService.WithReference(appConfig);
}
if (redis != null)
{
    weatherService.WithReference(redis);
}

// Only add container annotations when deploying (CONTAINER_REGISTRY is set by azd)
if (!string.IsNullOrEmpty(builder.Configuration["CONTAINER_REGISTRY"]))
{
    weatherService.WithAnnotation(new ContainerImageAnnotation
    {
        Registry = builder.Configuration["CONTAINER_REGISTRY"],
        Image = "aspire1-weatherservice",
        Tag = version
    });
}

var webFrontend = builder.AddProject<Projects.aspire1_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithEnvironment("APP_VERSION", version)
    .WithEnvironment("COMMIT_SHA", commitSha)
    .WithReference(weatherService)
    .WaitFor(weatherService);

// Only reference Azure resources if they were added
if (appInsights != null)
{
    webFrontend.WithReference(appInsights);
}
if (appConfig != null)
{
    webFrontend.WithReference(appConfig);
}
if (redis != null)
{
    webFrontend.WithReference(redis);
}

// Only add container annotations when deploying
if (!string.IsNullOrEmpty(builder.Configuration["CONTAINER_REGISTRY"]))
{
    webFrontend.WithAnnotation(new ContainerImageAnnotation
    {
        Registry = builder.Configuration["CONTAINER_REGISTRY"],
        Image = "aspire1-web",
        Tag = version
    });
}

builder.Build().Run();
