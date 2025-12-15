# Architecture - aspire1.WeatherService

> **Component Type:** Minimal API (REST)
> **Framework:** ASP.NET Core 9.0
> **Purpose:** Backend API service providing weather data with Redis caching and feature flags

## 🎯 Overview

The **WeatherService** is a lightweight REST API built with ASP.NET Core Minimal APIs. It demonstrates:

- Minimal API routing (no controllers)
- Redis distributed caching with offline-first fallback
- Azure App Configuration with feature flags
- OpenTelemetry instrumentation (via ServiceDefaults)
- Health checks with version metadata and feature flag status
- Service discovery (consumed by aspire1.Web)
- SemVer-based deployment tracking

## 🏗️ Architecture

```mermaid
graph TB
    Client[aspire1.Web<br/>HTTP Client]

    subgraph "aspire1.WeatherService"
        Middleware[Middleware Pipeline]
        Routes[Minimal API Routes]
        CachedService[CachedWeatherService]
        FeatureFlags[Feature Manager]
        ServiceDefaults[ServiceDefaults<br/>OpenTelemetry, Health]

        subgraph "Endpoints"
            Root[GET /]
            Weather[GET /weatherforecast]
            Version[GET /version]
            HealthDetailed[GET /health/detailed]
            Health[GET /health]
        end
    end

    subgraph "Azure Services"
        AppInsights[Application Insights]
        AppConfig[Azure App Configuration]
        Redis[Azure Cache for Redis]
    end

    Client -->|Service Discovery| Middleware
    Middleware --> Routes
    Routes --> Root
    Routes --> Weather
    Routes --> Version
    Routes --> HealthDetailed
    Routes --> Health
    
    Weather --> FeatureFlags
    Weather --> CachedService
    CachedService --> Redis
    HealthDetailed --> FeatureFlags

    ServiceDefaults -.->|Traces, Metrics| AppInsights
    ServiceDefaults -.->|Feature Flags| AppConfig

    style Routes fill:#0078d4,stroke:#005a9e,color:#fff
    style CachedService fill:#50e6ff
    style FeatureFlags fill:#90EE90
```

## 📡 API Endpoints

### `GET /`

**Purpose:** Root status message

**Response:**

```json
"API service is running. Navigate to /weatherforecast to see sample data."
```

---

### `GET /weatherforecast`

**Purpose:** Weather data API with Redis caching and feature flag control

**Query Parameters:**
- `maxItems` (optional, default: 10) - Number of forecast days to return

**Response:**

```json
[
  {
    "date": "2025-12-10",
    "temperatureC": 15,
    "temperatureF": 59,
    "summary": "Mild",
    "humidity": 65
  }
  // ... more days
]
```

**Feature Flag:**
- Controlled by `WeatherForecast` feature flag in Azure App Configuration
- Returns 503 with error message if feature is disabled

**Implementation:**

- Delegates to `CachedWeatherService` for Redis-backed caching
- Cache TTL: 5 minutes (sliding)
- Generates random forecast data (no database)
- Falls back to in-memory cache if Redis unavailable
- Demonstrates JSON serialization

**Custom Telemetry:**

```csharp
// Track feature flag status
if (!await featureManager.IsEnabledAsync("WeatherForecast"))
{
    return Results.Json(new { error = "Weather forecast feature is currently disabled" }, statusCode: 503);
}

// Tracks total API calls
ApplicationMetrics.WeatherApiCalls.Add(1,
    new KeyValuePair<string, object?>("endpoint", "weatherforecast"),
    new KeyValuePair<string, object?>("feature_enabled", "true"));

// Tracks sunny forecasts by temperature range
foreach (var forecast in forecasts.Where(f => f.Summary?.Contains("Sunny") == true))
{
    ApplicationMetrics.SunnyForecasts.Add(1,
        new KeyValuePair<string, object?>("temperature_range",
            ApplicationMetrics.GetTemperatureRange(forecast.TemperatureC)));
}
```

**CachedWeatherService Metrics:**

```csharp
// Cache hit
ApplicationMetrics.CacheHits.Add(1,
    new KeyValuePair<string, object?>("entity", "weather"));

// Cache miss
ApplicationMetrics.CacheMisses.Add(1,
    new KeyValuePair<string, object?>("entity", "weather"));
```

---

### `GET /version`

**Purpose:** Deployment tracking and version verification

**Response:**

```json
{
  "version": "1.0.0+a1af010e18",
  "commitSha": "a1af010",
  "service": "weatherservice",
  "environment": "Production",
  "timestamp": "2025-12-09T18:30:00Z"
}
```

**Use Cases:**

- Verify deployed version in Azure Portal
- Compare versions across environments (dev/staging/prod)
- Correlate application behavior with git commits
- Smoke test after deployment

---

### `GET /health/detailed`

**Purpose:** Enhanced health check with version metadata and feature flag status for OpenTelemetry correlation

**Feature Flag:**
- Controlled by `DetailedHealth` feature flag in Azure App Configuration
- Returns minimal health info if feature is disabled

**Response (when DetailedHealth enabled):**

```json
{
  "status": "healthy",
  "version": "1.0.0+a1af010e18",
  "commitSha": "a1af010",
  "service": "weatherservice",
  "timestamp": "2025-12-09T18:30:00Z",
  "uptime": 3600.5,
  "features": {
    "detailedHealth": true,
    "weatherForecast": true
  }
}
```

**Response (when DetailedHealth disabled):**

```json
{
  "status": "healthy"
}
```

**Use Cases:**

- Debugging distributed traces (find which version produced a span)
- Uptime monitoring (seconds since container start)
- Service mesh health dashboards

---

### `GET /health`

**Purpose:** Standard Aspire health check (from ServiceDefaults)

**Response:** `Healthy` (200 OK) or `Unhealthy` (503 Service Unavailable)

**Checks:**

- `self`: Always returns `Healthy` (liveness probe)
- Additional checks can be added (database, cache, etc.)

**Configuration:**

- Managed by `ServiceDefaults.MapDefaultEndpoints()`
- Only exposed in Development environment by default
- ACA uses this for readiness/liveness probes

## 🔧 Startup Configuration

### Program.cs Flow

```mermaid
sequenceDiagram
    participant Main as Program.cs
    participant Builder as WebApplicationBuilder
    participant SD as ServiceDefaults
    participant App as WebApplication

    Main->>Builder: WebApplication.CreateBuilder(args)
    Main->>SD: builder.AddServiceDefaults()
    SD-->>Builder: OpenTelemetry, Health, Resilience
    Main->>Builder: AddProblemDetails(), AddOpenApi()
    Main->>Main: Extract version from assembly/config
    Main->>App: builder.Build()
    Main->>App: Configure middleware pipeline
    Main->>App: Map endpoints
    Main->>App: MapDefaultEndpoints()
    Main->>App: app.Run()
```

### Key Configuration Steps

1. **Service Defaults:** Registers OpenTelemetry, health checks, resilience handlers
2. **Azure App Configuration:** Connects to Azure App Config for feature flags (with offline fallback)
3. **Feature Management:** Registers `IFeatureManager` for runtime feature flag checks
4. **Redis Distributed Cache:** Configures Redis with offline-first fallback to in-memory cache
5. **Problem Details:** Standardized error responses (RFC 7807)
6. **OpenAPI:** Swagger/OpenAPI documentation generation
7. **CachedWeatherService:** Registers scoped service for Redis-backed weather caching
8. **Version Extraction:** Reads from `APP_VERSION` env var or assembly metadata
9. **Middleware Pipeline:** Exception handler, Azure App Config refresh middleware (if configured)
10. **Endpoint Mapping:** Minimal API routes with feature flag integration
11. **Health Endpoints:** `/health`, `/alive` (from ServiceDefaults)

## 🔐 Configuration & Secrets

### Configuration Sources (Priority Order)

1. **Environment Variables** (highest priority)

   - `APP_VERSION` - Injected by AppHost or azd
   - `COMMIT_SHA` - Injected by AppHost or GitHub Actions
   - `OTEL_EXPORTER_OTLP_ENDPOINT` - Application Insights endpoint
   - `AppConfig:Endpoint` - Azure App Configuration endpoint
   - `ConnectionStrings:cache` - Redis connection string (from AppHost)

2. **Azure App Configuration** (when configured)

   - Feature flags
   - Dynamic configuration with 30-second refresh

3. **Azure Key Vault References** (production)

   - Connection strings
   - API keys
   - External service credentials

4. **appsettings.{Environment}.json**

   - Environment-specific non-sensitive config
   - Local feature flag fallback

5. **appsettings.json**

   - Default non-sensitive config

6. **User Secrets** (local dev only)
   - `dotnet user-secrets set "MySecret" "value"`

## 🎛️ Feature Flags

### Azure App Configuration Integration

**Configuration:**

```csharp
var appConfigEndpoint = builder.Configuration["AppConfig:Endpoint"];
if (!string.IsNullOrEmpty(appConfigEndpoint))
{
    builder.Configuration.AddAzureAppConfiguration(options =>
    {
        options.Connect(new Uri(appConfigEndpoint), new DefaultAzureCredential())
               .UseFeatureFlags(featureFlagOptions =>
               {
                   featureFlagOptions.SetRefreshInterval(TimeSpan.FromSeconds(30));
                   featureFlagOptions.Select("*", builder.Environment.EnvironmentName);
               });
    });
}
```

**Middleware:**

```csharp
app.UseAzureAppConfiguration(); // Enables dynamic refresh
```

### Feature Flags Used

| Flag Name | Purpose | Default (Local) | Impact |
| --- | --- | --- | --- |
| `WeatherForecast` | Controls `/weatherforecast` endpoint availability | `true` | Returns 503 if disabled |
| `DetailedHealth` | Controls level of detail in `/health/detailed` | `true` | Returns minimal health if disabled |
| `WeatherHumidity` | Controls humidity data in API response and UI visibility | `true` | Returns humidity=0 in API when disabled; UI hides humidity display |


### Usage Patterns

**1. Endpoint Gating:**

```csharp
app.MapGet("/weatherforecast", async (CachedWeatherService service, IFeatureManager featureManager) =>
{
    if (!await featureManager.IsEnabledAsync("WeatherForecast"))
    {
        return Results.Json(new { error = "Weather forecast feature is currently disabled" }, statusCode: 503);
    }
    // ... implementation
});
```

**2. Conditional Response:**

```csharp
app.MapGet("/health/detailed", async (IFeatureManager featureManager) =>
{
    var showDetailed = await featureManager.IsEnabledAsync("DetailedHealth");
    
    if (showDetailed)
    {
        return Results.Ok(new { status = "healthy", version, commitSha, /* ... */ });
    }
    
    return Results.Ok(new { status = "healthy" });
});
```

### Offline-First Design

- App starts successfully without Azure App Configuration connection
- Falls back to local `appsettings.json` for feature flag definitions
- Logs warning but continues: `"Warning: Could not connect to Azure App Configuration"`
- Local fallback enables disconnected development

### Managing Feature Flags

**Azure Portal:**
1. Navigate to Azure App Configuration resource
2. Feature Manager → Add/Edit flags
3. Set environment-specific labels (Development, Production)
4. Changes reflect in app within 30 seconds (refresh interval)

**Azure CLI:**

```bash
# Enable feature
az appconfig feature set --name WeatherForecast --label Production --yes

# Disable feature
az appconfig feature set --name WeatherForecast --label Production --no

# List all features
az appconfig feature list
```

## 💾 Redis Caching with CachedWeatherService

### Architecture

The `CachedWeatherService` class provides Redis-backed distributed caching with graceful fallback:

```csharp
public class CachedWeatherService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachedWeatherService> _logger;
    private const string CacheKeyPrefix = "api:weather:forecast";
    
    // Cache TTL: 5 minutes
    // Key format: "api:weather:forecast:{maxItems}"
}
```

### Caching Flow

```mermaid
sequenceDiagram
    participant Client
    participant Endpoint as /weatherforecast
    participant Service as CachedWeatherService
    participant Redis as Redis Cache
    
    Client->>Endpoint: GET /weatherforecast?maxItems=10
    Endpoint->>Service: GetWeatherForecastAsync(10)
    Service->>Redis: GetAsync("api:weather:forecast:10")
    
    alt Cache HIT
        Redis-->>Service: Cached JSON data
        Service->>Service: Deserialize
        Service->>Service: Track CacheHits metric
        Service-->>Endpoint: WeatherForecast[]
    else Cache MISS
        Redis-->>Service: null
        Service->>Service: Track CacheMisses metric
        Service->>Service: GenerateForecasts(10)
        Service->>Redis: SetAsync with 5-min TTL
        Service-->>Endpoint: WeatherForecast[]
    end
    
    Endpoint-->>Client: JSON response
```

### Offline-First Redis Configuration

**AppHost Configuration:**

```csharp
// Local development: Redis container
if (isLocalDev)
{
    redis = builder.AddRedis("cache");
}
// Azure: Managed Redis
else
{
    redis = builder.AddAzureRedis("cache");
}
```

**WeatherService Configuration:**

```csharp
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
        builder.Services.AddDistributedMemoryCache(); // ← Fallback
    }
}
else
{
    Console.WriteLine("⚠️  Redis not configured (local development mode)");
    builder.Services.AddDistributedMemoryCache(); // ← Fallback
}
```

### Cache Behavior

**Cache Key Naming:**
- Pattern: `api:weather:forecast:{maxItems}`
- Example: `api:weather:forecast:10`
- Namespaced to prevent collisions

**Cache TTL:**
- **Expiration:** 5 minutes (absolute)
- **Rationale:** Balance between freshness and performance

**Error Handling:**

```csharp
try
{
    var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
    if (cachedData != null)
    {
        // Cache hit path
    }
}
catch (Exception ex)
{
    _logger.LogWarning(ex, "Cache read failed, falling back to generation");
}
```

- All cache operations are wrapped in try-catch
- Cache failures don't break API (graceful degradation)
- Generates fresh data if cache unavailable

### Cache Metrics

```csharp
// Track cache hits
ApplicationMetrics.CacheHits.Add(1,
    new KeyValuePair<string, object?>("entity", "weather"));

// Track cache misses
ApplicationMetrics.CacheMisses.Add(1,
    new KeyValuePair<string, object?>("entity", "weather"));
```

These metrics flow to Application Insights for monitoring:
- Cache hit rate: `CacheHits / (CacheHits + CacheMisses)`
- Target: >80% hit rate after warm-up

### Example: Adding a Database Connection

```csharp
// ❌ DON'T: Hard-code in appsettings.json
{
  "ConnectionStrings": {
    "MyDb": "Server=...;Password=..." // NEVER COMMIT THIS
  }
}

// ✅ DO: Use environment variable (Key Vault reference in ACA)
var connectionString = builder.Configuration.GetConnectionString("MyDb");
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseNpgsql(connectionString));
```

**In Azure:**

```yaml
# Bicep generates this (from AppHost)
env:
  - name: ConnectionStrings__MyDb
    secretRef: mydb-connection # Points to Key Vault secret
```

## 📊 OpenTelemetry Instrumentation

### What's Instrumented (via ServiceDefaults)

| Type        | Instrumented Components                            | Example                                 |
| ----------- | -------------------------------------------------- | --------------------------------------- |
| **Traces**  | ASP.NET Core requests, HttpClient calls            | Request → `/weatherforecast` span       |
| **Metrics** | Request rate, duration, CPU, memory, exceptions    | `http.server.request.duration`          |
| **Logs**    | Structured logs with scopes and formatted messages | `Processing weather request for 5 days` |

### Example Trace Flow

```mermaid
sequenceDiagram
    participant User
    participant Web as aspire1.Web
    participant API as aspire1.WeatherService
    participant OTEL as Application Insights

    User->>Web: GET /weather
    Note over Web: Trace ID: abc123
    Web->>API: GET /weatherforecast
    Note over API: Parent Span: Web<br/>Child Span: API
    API-->>Web: Weather data
    Web-->>User: Rendered page

    Web->>OTEL: Export trace (Span: Web)
    API->>OTEL: Export trace (Span: API)

    Note over OTEL: Correlate spans by Trace ID<br/>Show end-to-end latency
```

### Querying Traces in Application Insights

```kql
// Find all requests to /weatherforecast with version 1.0.0
dependencies
| where target == "weatherservice"
| where name contains "weatherforecast"
| extend version = tostring(customDimensions.version)
| where version == "1.0.0"
| project timestamp, duration, success, version
```

## 🚀 Deployment

### Local Development

```bash
# Run standalone (requires AppHost for service discovery)
dotnet run --project aspire1.WeatherService

# Access endpoints
curl http://localhost:7002/weatherforecast
curl http://localhost:7002/version
```

### Azure Container Apps

**Container Image:**

```bash
# Run standalone (requires AppHost for service discovery)
dotnet run --project aspire1.WeatherService

# Access endpoints
curl http://localhost:7002/weatherforecast
curl http://localhost:7002/version
```

**Environment Variables (injected by azd):**

- `APP_VERSION`: `1.0.0`
- `COMMIT_SHA`: `a1af010`
- `OTEL_EXPORTER_OTLP_ENDPOINT`: `https://...` (Application Insights)
- `ASPNETCORE_ENVIRONMENT`: `Production`

**Health Probes:**

```yaml
livenessProbe:
  httpGet:
    path: /alive
    port: 8080
  initialDelaySeconds: 5
  periodSeconds: 10

readinessProbe:
  httpGet:
    path: /health
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 5
```

**Scaling:**

- **Min Replicas:** 1 (always warm)
- **Max Replicas:** 5
- **Scale Rule:** HTTP - 50 concurrent requests per replica
- **Scale In Delay:** 5 minutes

## 🎯 Testing

### Unit Tests (Future)

```csharp
// Example with xUnit + FluentAssertions
public class WeatherForecastTests
{
    [Fact]
    public void GetWeatherForecast_Returns5Days()
    {
        // Arrange
        var forecast = GetWeatherForecast(); // Extracted logic

        // Act
        var result = forecast.ToArray();

        // Assert
        result.Should().HaveCount(5);
        result.All(f => f.Date > DateOnly.FromDateTime(DateTime.Now))
            .Should().BeTrue();
    }
}
```

### Integration Tests (Future)

```csharp
// Example with Aspire.Hosting.Testing
public class WeatherServiceTests : IAsyncLifetime
{
    private DistributedApplication _app = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.aspire1_AppHost>();
        _app = await appHost.BuildAsync();
        await _app.StartAsync();

        _client = _app.CreateHttpClient("weatherservice");
    }

    [Fact]
    public async Task WeatherForecast_ReturnsSuccessAndData()
    {
        // Act
        var response = await _client.GetAsync("/weatherforecast");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await response.Content.ReadFromJsonAsync<WeatherForecast[]>();
        data.Should().HaveCount(5);
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();
}
```

## 🐛 Troubleshooting

### Service Discovery Fails

**Symptom:** aspire1.Web can't reach `/weatherforecast`

**Diagnostics:**

```bash
# From aspire1.Web container
curl http://weatherservice/health
```

**Fix:**

- Ensure AppHost uses `WithReference(weatherService)` on Web
- Verify service name matches: `"weatherservice"` (not `"aspire1.WeatherService"`)

### Version Shows "unknown"

**Symptom:** `/version` returns `{ "version": "unknown" }`

**Diagnostics:**

```bash
# Check environment variables
azd env get-values | findstr VERSION

# Check assembly version
dotnet build -c Release
$dll = Get-Item "bin/Release/net9.0/aspire1.WeatherService.dll"
[System.Diagnostics.FileVersionInfo]::GetVersionInfo($dll.FullName)
```

**Fix:**

- Ensure MinVer is installed: `dotnet tool install -g minver-cli`
- Tag git repo: `git tag v1.0.0`
- Set environment variable: `azd env set VERSION $(minver)`

### Health Check Fails in ACA

**Symptom:** Container restarts repeatedly, logs show "Health check failed"

**Diagnostics:**

```bash
# View container logs
az containerapp logs show \
  --name aspire1-weatherservice \
  --resource-group rg-aspire1-prod \
  --follow
```

**Fix:**

- Ensure `/health` endpoint returns 200 OK
- Check `MapDefaultEndpoints()` is called in `Program.cs`
- Verify health check is configured in ACA (via AppHost `WithHttpHealthCheck()`)

## ✅ Best Practices vs ❌ Anti-Patterns

### 1. Version Extraction

#### ❌ BAD: Hard-coded version string

```csharp
var version = "1.0.0"; // Stale immediately, manual updates required
```

**Why it's bad:** Version drifts from actual deployment, no git correlation, manual maintenance

#### ✅ GOOD: Assembly metadata + environment fallback (Current implementation)

```csharp
var version = builder.Configuration["APP_VERSION"] ??
              Assembly.GetExecutingAssembly()
                      .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                      ?.InformationalVersion ?? "unknown";
var commitSha = builder.Configuration["COMMIT_SHA"] ??
                Environment.GetEnvironmentVariable("GITHUB_SHA")?[..7] ?? "local";
```

**Why it's good:** Multiple fallback sources, MinVer auto-updates assembly version, git SHA for traceability

---

### 2. Health Check Endpoints

#### ❌ BAD: No version metadata

```csharp
app.MapGet("/health", () => "Healthy");
```

**Why it's bad:** Can't correlate issues with deployed version, no uptime tracking, minimal diagnostics

#### ✅ GOOD: Enhanced health with version (Current implementation)

```csharp
app.MapGet("/health/detailed", () => new
{
    status = "healthy",
    version,
    commitSha,
var version = builder.Configuration["APP_VERSION"] ??
              Assembly.GetExecutingAssembly()
                      .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                      ?.InformationalVersion ?? "unknown";
var commitSha = builder.Configuration["COMMIT_SHA"] ??
                Environment.GetEnvironmentVariable("GITHUB_SHA")?[..7] ?? "local";
```
    timestamp = DateTime.UtcNow,
    uptime = Environment.TickCount64 / 1000.0
})
.WithName("GetDetailedHealth");
```

**Why it's good:** OpenTelemetry correlation, deployment tracking, uptime monitoring, troubleshooting context

---

### 3. Secrets Management

#### ❌ BAD: Connection string in appsettings.json

```json
{
  "ConnectionStrings": {
    "MyDb": "Server=prod.db.com;User=sa;Password=P@ssw0rd123;"
  }
}
```

**Why it's bad:** Secrets in source control, exposed in logs, no rotation, compliance violation

#### ✅ GOOD: Key Vault reference (aspire1 pattern)

```json
{
  "ConnectionStrings": {
    "MyDb": "" // Empty placeholder
  }
}
```

```yaml
# Environment variable in ACA (set by azd hooks)
ConnectionStrings__MyDb: "@Microsoft.KeyVault(SecretUri=https://kv-aspire1.vault.azure.net/secrets/mydb-connection)"
```

**Why it's good:** Zero secrets in code, managed identity auth, automatic rotation, audit trail

---

### 4. Service Defaults Registration

#### ❌ BAD: Manual OpenTelemetry setup in every service

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation())
    .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation());
// Repeated in every service, inconsistent config
```

**Why it's bad:** Code duplication, config drift, missing instrumentation, maintenance nightmare

#### ✅ GOOD: ServiceDefaults extension (Current implementation)

```csharp
builder.AddServiceDefaults(); // One line, consistent everywhere
```

**Why it's good:** DRY principle, consistent observability, includes resilience + service discovery, centralized updates

---

### 5. Endpoint Naming & Documentation

#### ❌ BAD: No endpoint names, no OpenAPI

```csharp
app.MapGet("/weatherforecast", () => { /* ... */ });
```

**Why it's bad:** Can't reference endpoint by name, no auto-generated docs, hard to maintain

#### ✅ GOOD: Named endpoints with OpenAPI (Current implementation)

```csharp
app.MapGet("/weatherforecast", () => { /* ... */ })
    .WithName("GetWeatherForecast");

app.MapGet("/version", () => new { version, commitSha })
    .WithName("GetVersion");
```

**Why it's good:** Endpoint referencing in tests, OpenAPI generation, link generation, route constraints

---

### 6. Configuration Fallback Strategy

#### ❌ BAD: Single source, crashes if missing

```csharp
var version = builder.Configuration["APP_VERSION"]; // NullReferenceException in local dev
```

**Why it's bad:** Breaks local development, no graceful degradation, environment-specific

#### ✅ GOOD: Multiple fallbacks with defaults (Current implementation)

```csharp
var version = builder.Configuration["APP_VERSION"] ??           // ACA/Production
              Assembly.GetExecutingAssembly()
                      .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                      ?.InformationalVersion ??                  // MinVer build
              "unknown";                                          // Fallback
```

**Why it's good:** Works in all environments, graceful degradation, clear priority order

---

### 7. Exception Handling

#### ❌ BAD: Generic try-catch everywhere

```csharp
app.MapGet("/weatherforecast", () =>
{
    try {
        return GetWeather();
    } catch (Exception ex) {
        return Results.Problem("Error");
    }
});
```

**Why it's bad:** Swallows valuable error details, breaks OpenTelemetry exception tracking, inconsistent responses

#### ✅ GOOD: Global exception handler (Current implementation)

```csharp
builder.Services.AddProblemDetails(); // RFC 7807 standard
app.UseExceptionHandler(); // Centralized exception handling
```

**Why it's good:** Consistent error format, OpenTelemetry captures exceptions, includes trace ID, production-safe

---

### 8. Minimal API Organization

#### ❌ BAD: Logic in endpoint handlers

```csharp
app.MapGet("/weatherforecast", () =>
{
    // 50 lines of business logic here
    var data = dbContext.Weather.Where(x => x.Date > DateTime.Now).ToList();
    return data.Select(x => new WeatherForecast { /* ... */ });
});
```

**Why it's bad:** Untestable, violates SRP, hard to maintain, couples HTTP to business logic

#### ✅ GOOD: Thin endpoints, extracted logic (Current implementation uses simple logic)

```csharp
app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast(
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");
```

**Why it's good:** Testable logic, clear separation, easy to extract to service layer when needed

---

### 9. OpenTelemetry Trace Filtering

#### ❌ BAD: Trace everything including health checks

```csharp
.WithTracing(tracing =>
{
    tracing.AddAspNetCoreInstrumentation(); // No filtering
})
```

**Why it's bad:** Noisy traces, high ingestion costs, health check spam obscures real issues

#### ✅ GOOD: Filter health endpoints (ServiceDefaults implementation)

```csharp
.WithTracing(tracing =>
{
    tracing.AddAspNetCoreInstrumentation(options =>
        options.Filter = context =>
            !context.Request.Path.StartsWithSegments("/health")
            && !context.Request.Path.StartsWithSegments("/alive")
    )
})
```

**Why it's good:** Clean traces, lower costs, focuses on business operations, reduces noise

---

### 10. Environment-Specific Configuration

#### ❌ BAD: Conditional logic based on environment

```csharp
if (app.Environment.IsProduction())
{
    app.MapOpenApi(); // Wrong! OpenAPI in prod = security risk
}
```

**Why it's bad:** Exposes API schema in production, security vulnerability, manual maintenance

#### ✅ GOOD: Development-only features (Current implementation)

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // Only in dev
}

app.UseExceptionHandler(); // Always on (production-safe details)
```

**Why it's good:** Secure by default, explicit opt-in for dev tools, follows least privilege

## 📚 Related Documentation

- [Root Architecture](../ARCHITECTURE.md)
- [AppHost Architecture](../aspire1.AppHost/ARCHITECTURE.md)
- [Web Service Architecture](../aspire1.Web/ARCHITECTURE.md)
- [Service Defaults](../aspire1.ServiceDefaults/ARCHITECTURE.md)

## 🔗 Useful Commands

```bash
# Run with custom version
$env:APP_VERSION="2.0.0"; dotnet run

# Test all endpoints
curl http://localhost:7002/
curl http://localhost:7002/weatherforecast
curl http://localhost:7002/version
curl http://localhost:7002/health/detailed
curl http://localhost:7002/health

# Build release container
dotnet publish -c Release
docker build -t aspire1-weatherservice:1.0.0 .

# Run in container locally
docker run -p 8080:8080 -e APP_VERSION=1.0.0 aspire1-weatherservice:1.0.0
```

---

**Next:** [Web Service Architecture](../aspire1.Web/ARCHITECTURE.md) →
