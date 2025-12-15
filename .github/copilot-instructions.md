# .NET Aspire Repository Instructions

This repository contains a .NET Aspire solution targeting Azure Container Apps with comprehensive observability, secrets management, and CI/CD practices.

## Project Overview

- **Technology Stack**: .NET 9, .NET Aspire 8.2+, Azure Developer CLI (azd) 1.9+
- **Target Platform**: Azure Container Apps Environment with Dapr and KEDA
- **Architecture**: Microservices with service discovery, distributed tracing, and centralized configuration

## Architecture-First Development

Before making any code recommendations or changes, always:

1. Read the relevant `ARCHITECTURE.md` file for context on purpose, intent, and existing patterns
2. Check solution root `ARCHITECTURE.md` for high-level topology, service discovery, and deployment patterns
3. Check project-specific `ARCHITECTURE.md` when modifying code in that project
4. Review the "Good vs Bad Implementations" section in the relevant `ARCHITECTURE.md` to see real-world examples
5. Ground recommendations in documented architecture decisions (service discovery, health checks, versioning, secrets management)
6. Reference existing endpoints and patterns from architecture docs before suggesting new ones
7. Respect documented configurations (OpenTelemetry, resilience, caching strategies)
8. Match documented patterns shown in good examples, avoiding documented anti-patterns

### Architecture Documentation Map

- `/ARCHITECTURE.md` - Solution-wide: topology, deployment, CI/CD, observability, troubleshooting, dependencies & change impact analysis
- `/aspire1.AppHost/ARCHITECTURE.md` - Service orchestration, service discovery, AppHost configuration, Redis & Azure App Config integration
- `/aspire1.WeatherService/ARCHITECTURE.md` - API endpoints, OpenTelemetry, health checks, deployment, Redis caching, feature flags
- `/aspire1.Web/ARCHITECTURE.md` - Blazor Server, SignalR, HTTP clients, WeatherApiClient patterns, session state, feature flags
- `/aspire1.ServiceDefaults/ARCHITECTURE.md` - OpenTelemetry, health checks, resilience, service discovery

Each `ARCHITECTURE.md` includes a "Best Practices vs Anti-Patterns" section with real examples from this codebase.

### Loading Architecture Documentation Efficiently

**Only load the ARCHITECTURE.md files relevant to your current task scope:**

1. **Always load first**: `/ARCHITECTURE.md` for high-level context, dependencies, and change impact analysis

2. **Load based on task scope**:
   - **Modifying/reviewing AppHost**: Load `/aspire1.AppHost/ARCHITECTURE.md`
   - **Modifying/reviewing WeatherService API**: Load `/aspire1.WeatherService/ARCHITECTURE.md`
   - **Modifying/reviewing Web frontend**: Load `/aspire1.Web/ARCHITECTURE.md`
   - **Modifying/reviewing ServiceDefaults**: Load `/aspire1.ServiceDefaults/ARCHITECTURE.md`
   - **Cross-cutting concerns** (telemetry, health checks, resilience): Load `/aspire1.ServiceDefaults/ARCHITECTURE.md` + relevant project docs
   - **Service discovery issues**: Load `/aspire1.AppHost/ARCHITECTURE.md` + affected service docs
   - **Deployment/infrastructure**: Load `/ARCHITECTURE.md` + `/aspire1.AppHost/ARCHITECTURE.md`

3. **Load multiple when**:
   - Changes affect service contracts (DTO structures, endpoints): Load both WeatherService and Web docs
   - Adding new Azure resources: Load AppHost + affected service docs
   - Changing ServiceDefaults: Load ServiceDefaults + all service docs (WeatherService, Web)
   - Investigating integration issues: Load docs for all services involved in the flow

4. **Don't load**:
   - Documentation for services you're not modifying
   - All docs "just in case" - be selective based on actual task scope

**Example scoping rules**:
- Task: "Add new endpoint to WeatherService" → Load `/ARCHITECTURE.md` + `/aspire1.WeatherService/ARCHITECTURE.md`
- Task: "Fix service discovery between Web and WeatherService" → Load `/ARCHITECTURE.md` + `/aspire1.AppHost/ARCHITECTURE.md` + `/aspire1.Web/ARCHITECTURE.md` + `/aspire1.WeatherService/ARCHITECTURE.md`
- Task: "Update health check patterns" → Load `/ARCHITECTURE.md` + `/aspire1.ServiceDefaults/ARCHITECTURE.md`
- Task: "Add feature flag to UI" → Load `/aspire1.Web/ARCHITECTURE.md`
- Task: "Understand dependencies before changing DTO" → Load `/ARCHITECTURE.md` (Dependencies & Change Impact section) + `/aspire1.WeatherService/ARCHITECTURE.md` + `/aspire1.Web/ARCHITECTURE.md`

## Code Patterns

### Service Discovery

- Use `WithReference()` for service-to-service communication in AppHost
- Avoid hard-coded URLs; leverage Aspire''s service discovery mechanisms
- Example: `builder.AddProject<Projects.aspire1_Web>("webfrontend").WithReference(apiService)`

### Health Checks

- Use versioned health endpoints (e.g., `/health/detailed`)
- Match existing endpoint naming conventions (`/version`, `/health/detailed`)
- Include version metadata in health responses

### Resilience

- Follow documented resilience patterns (retry, circuit breaker) from ServiceDefaults
- Apply appropriate timeouts and fallback strategies
- ServiceDefaults automatically configures HttpClient resilience

### Observability

- Use documented OpenTelemetry patterns
- Exclude health endpoints from traces to reduce noise
- Ensure all services emit structured logs and metrics
- All telemetry flows to Application Insights in Azure

## Deployment and Infrastructure

### Target Platform

- Default deployment target: Azure Container Apps (ACA Environment with Dapr)
- Do not suggest Azure App Service or Azure Kubernetes Service unless explicitly requested

### Secrets Management

- **Local Development**: Use User Secrets (`dotnet user-secrets`) and azd environment variables
- **Dev/Production**: Use Azure Key Vault references with managed identity only
- **Never**: Store secrets in `appsettings.json`, connection strings in AppHost code, or commit secrets to source control
- Use user-assigned managed identities when cross-resource access is required
- For local development: Azure CLI authentication or `.env` files with User Secrets

### CI/CD Pipeline Requirements

- Use GitHub Actions with Azure Developer CLI (azd) only
- Cache dotnet restore, NuGet packages, and azd bicep modules
- Run restore/build/test in parallel where possible
- Target deployment time: under 120 seconds for typical Aspire apps
- Zero secrets in logs
- Use matrix strategy for multiple environments
- Provide exact workflow YAML snippets and azd hooks when suggesting improvements

## Azure Services Configuration

### Feature Flags (Azure App Configuration)

- Use `Microsoft.Extensions.Configuration.AzureAppConfiguration` + `Microsoft.FeatureManagement.AspNetCore`
- Authenticate with managed identity only (never connection strings)
- Configure sentinel key for 30-second cache refresh
- Use labels for environments (dev, staging, prod)
- Namespace flags appropriately (e.g., `FeatureName:SubFeature`)
- In AppHost: `AddAzureAppConfiguration().WithReference()`
- Local development: `UseFeatureFlags()` with `appsettings.json` fallback
- Enable/disable via Azure Portal or CLI with zero redeployment

### Feature Flag Patterns

- Use `IFeatureManager` for runtime checks
- Use `[FeatureGate]` attribute for controllers and Minimal APIs
- Use `IVariantFeatureManager` for A/B testing
- Keep flags short-lived (less than 90 days)
- Remove unused flags aggressively
- Test both enabled and disabled paths in unit tests using `InMemoryFeatureManager`

### Redis Caching and Sessions

- **Service**: Azure Cache for Redis (Premium tier for persistence and geo-replication)
- **In AppHost**: Use `AddRedis()` with `WithReference()`
- **Distributed Cache**: `Microsoft.Extensions.Caching.StackExchangeRedis` with `IDistributedCache`
- **Session State**: `AddStackExchangeRedisCache()` for distributed sessions
- **Expiration**:
  - Sessions: Sliding expiration 20-30 minutes
  - Cache: Absolute expiration 1-24 hours based on data volatility
- **Caching Patterns**:
  - Cache-aside for reads (check cache → miss → fetch from DB → store in cache)
  - Write-through for critical updates
  - Key naming: `{service}:{entity}:{id}` (e.g., `api:user:12345`)
  - Use `RedisKey` for type safety
  - Serialize with `System.Text.Json` for performance
  - Cache null results (5 minute TTL) to prevent database hammering
  - Monitor hit rates via OpenTelemetry custom metrics
- Use Redis channels for pub/sub scenarios
- Never store secrets in cache

### Session Management

- Use `AddSession()` + `AddStackExchangeRedisCache()` for distributed sessions
- Store only user context (userId, tenantId, culture) - never business data
- Cookie settings: `SameSite=Lax`, `HttpOnly=true`, `Secure=true` (production)
- Sliding expiration: 30 minutes, absolute maximum: 8 hours
- For APIs: Use Redis-backed JWT refresh tokens instead of sessions
- AppHost: `redis.WithReference()` ensures connection string injection

## Development Practices

### Offline-First Development

- All Azure resource integrations must have local fallbacks
- Application must run and debug completely disconnected from Azure
- Wrap all Azure connections in try-catch with graceful fallback to local alternatives (appsettings.json, in-memory, local emulators)
- Never block application startup on Azure service availability

### Testing Strategy

#### Unit Tests

- Framework: xUnit + FluentAssertions + NSubstitute
- Requirements: Fast (under 100ms), no I/O, no real dependencies
- Mock all external dependencies
- Naming convention: `[MethodName]_[Scenario]_[ExpectedResult]`
- Target: 80%+ coverage on business logic

#### Integration Tests

- Use `Aspire.Hosting.Testing` package
- Spin up `DistributedApplication` with real containers (Postgres, Redis, etc.)
- Use `WebApplicationFactory` for HTTP services
- Clean up resources in `Dispose()`
- Tests should hit real APIs and databases but complete in under 5 seconds each
- For AppHost: Use `DistributedApplicationTestingBuilder`, assert service discovery works, verify health endpoints, test `WithReference()` chains

#### Test Structure

- Unit tests: `[ProjectName].Tests`
- Integration tests: `[ProjectName].IntegrationTests`
- Keep integration tests separate so CI can run unit tests in under 10 seconds
- Run integration tests in parallel matrix jobs

#### Integration Test Best Practices

- Use unique database names per test class (Guid suffix)
- Seed minimal data
- Always clean up in `finally` or `Dispose()`
- Use Respawn for database resets between tests if needed
- Never test deployment infrastructure here (use azd deploy smoke tests for that)

## Build and Validation

### Initial Setup

Before starting development, install Git hooks to enforce branching strategy:

```powershell
.\scripts\Install-GitHooks.ps1
```

This installs hooks that prevent direct commits and pushes to `main`/`master` branches, enforcing a feature branch workflow.

### Build Commands

```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build aspire1.sln --no-restore

# Run tests
dotnet test aspire1.sln --no-build --verbosity normal

# Run AppHost (starts all services via Aspire Dashboard)
dotnet run --project aspire1.AppHost/aspire1.AppHost.csproj
```

### Versioning

- Uses MinVer for automatic semantic versioning based on git tags
- Version command: `minver` (from solution root)

### Deployment

```bash
# Full provision and deploy
azd up

# Deploy only (after infrastructure exists)
azd deploy

# Tear down environment
azd down --force --purge
```

### Common Issues and Workarounds

- Always run `dotnet restore` before building after changing dependencies
- If build fails, check that all ARCHITECTURE.md files are present (they''re referenced by projects)
- For local debugging, ensure Aspire Dashboard is accessible (typically https://localhost:15888)
- When adding new services, update the AppHost Program.cs and relevant ARCHITECTURE.md files

## Code Examples

### Good: Service Discovery with WithReference

```csharp
// In AppHost Program.cs
var weatherService = builder.AddProject<Projects.aspire1_WeatherService>("weatherservice");
var webApp = builder.AddProject<Projects.aspire1_Web>("webfrontend")
    .WithReference(weatherService);  // ✓ Automatic service discovery
```

### Bad: Hard-coded URLs

```csharp
// ✗ Don''t do this
var httpClient = new HttpClient { BaseAddress = new Uri("https://localhost:7123") };
```

### Good: Key Vault Reference for Secrets

```bash
# Set in Azure environment
azd env set ConnectionStrings__MyDb "@Microsoft.KeyVault(SecretUri=https://kv.vault.azure.net/secrets/mydb-connection)"
```

### Bad: Secrets in Configuration Files

```json
// ✗ Never put secrets in appsettings.json
{
  "ConnectionStrings": {
    "MyDb": "Server=prod.db.com;Password=secret123;"
  }
}
```

### Good: Versioned Health Endpoint

```csharp
app.MapGet("/health/detailed", (IConfiguration config) => new
{
    Status = "Healthy",
    Version = config["App:Version"],
    Timestamp = DateTime.UtcNow
});
```

### Good: HTTP Client with Resilience

```csharp
// WeatherApiClient already includes resilience patterns from ServiceDefaults
public class WeatherApiClient(HttpClient httpClient)
{
    public async Task<WeatherForecast[]> GetWeatherAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<WeatherForecast[]>("/weatherforecast", cancellationToken)
            ?? [];
    }
}
```

## Response Guidelines

When providing code recommendations:

- Always reference the specific ARCHITECTURE.md file that contains relevant patterns
- Show both the anti-pattern to avoid and the correct implementation
- Include complete code examples, not just snippets
- Verify suggestions against documented patterns
- When suggesting new endpoints or services, follow existing naming conventions documented in ARCHITECTURE.md files
- If a WeatherApiClient-style typed client already exists, use that pattern for new HTTP clients
- Provide clear explanations of the reasoning behind recommendations
- Highlight potential pitfalls or common mistakes to avoid
- Suggest validation steps to verify implementations
