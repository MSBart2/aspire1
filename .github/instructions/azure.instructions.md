# Azure Instructions for .NET Aspire Applications

**CRITICAL**: These instructions apply ONLY when working with Azure-related tasks for this .NET Aspire 8.2+ application targeting Azure Container Apps.

## Target Azure Architecture

### Deployment Platform
- **Primary Target**: Azure Container Apps Environment with Dapr and KEDA
- **Never suggest**: Azure App Service or Azure Kubernetes Service unless explicitly requested
- **Deployment Tool**: Azure Developer CLI (azd) 1.9+ only
- **Infrastructure**: Bicep templates in `/infra/` directory

### Required Azure Services Stack

#### Core Platform Services
- **Azure Container Apps Environment** - Primary hosting platform
- **Azure Container Registry** - Container image storage  
- **Azure Log Analytics Workspace** - Centralized logging
- **Azure Application Insights** - APM and distributed tracing
- **Azure Monitor** - Metrics and alerting

#### Application Services  
- **Azure Cache for Redis (Premium)** - Distributed caching and session state
- **Azure App Configuration** - Feature flags and configuration management
- **Azure Key Vault** - Secrets management with managed identity
- **Azure Service Bus** - Messaging (if needed)
- **Azure Cosmos DB** - NoSQL database (if needed)

## Azure Developer CLI (azd) Patterns

### Project Structure Requirements
```
azure.yaml              # azd project configuration
.azure/                 # azd environment data (gitignored)
infra/
├── main.bicep          # Main infrastructure template
├── app-insights.bicep  # Application Insights setup
├── redis.bicep         # Redis configuration  
├── app-config.bicep    # App Configuration setup
└── alerts.bicep        # Monitoring and alerting
```

### Essential azd Commands
```bash
# Initial setup and deployment
azd auth login
azd init
azd up

# Deploy application only (after infrastructure exists)
azd deploy

# Environment management
azd env new <env-name>
azd env select <env-name>
azd env set <key> <value>

# Teardown
azd down --force --purge
```

### azd Configuration Patterns

#### azure.yaml Structure
```yaml
name: aspire1
metadata:
  template: aspire1@latest
services:
  webfrontend:
    project: aspire1.Web
    host: containerapp
    language: dotnet
  weatherservice:
    project: aspire1.WeatherService  
    host: containerapp
    language: dotnet
```

#### Environment Variables
- Store in `.azure/<env-name>/.env` (gitignored)
- Use `azd env set` for secrets
- Reference Azure resources: `azd env set REDIS_CONNECTION_STRING "$(azd env get-values --output json | jq -r .REDIS_CONNECTION_STRING)"`

## Azure Service Integration Patterns

### 1. Azure Cache for Redis

#### Bicep Template Pattern
```bicep
resource redis 'Microsoft.Cache/redis@2023-08-01' = {
  name: 'redis-${uniqueString(resourceGroup().id)}'
  location: location
  properties: {
    sku: {
      name: 'Premium'
      family: 'P'
      capacity: 1
    }
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
    redisConfiguration: {
      'aof-backup-enabled': 'true'
      'rdb-backup-enabled': 'true'
    }
  }
}
```

#### AppHost Integration
```csharp
var redis = builder.AddRedis("redis")
    .WithDataVolume()
    .WithRedisInsight();

var apiService = builder.AddProject<Projects.aspire1_WeatherService>("weatherservice")
    .WithReference(redis);
```

#### Service Configuration  
```csharp
// In Program.cs
builder.AddStackExchangeRedisCache("redis");
builder.AddStackExchangeRedisOutputCache("redis");

// Caching patterns
builder.Services.AddScoped<ICachedWeatherService, CachedWeatherService>();
```

### 2. Azure App Configuration

#### Bicep Template Pattern
```bicep
resource appConfig 'Microsoft.AppConfiguration/configurationStores@2023-03-01' = {
  name: 'appconfig-${uniqueString(resourceGroup().id)}'
  location: location
  sku: {
    name: 'Standard'
  }
  properties: {
    enablePurgeProtection: false
  }
}
```

#### AppHost Integration
```csharp
var appConfig = builder.AddAzureAppConfiguration("appconfig");

var apiService = builder.AddProject<Projects.aspire1_WeatherService>("weatherservice")
    .WithReference(appConfig);
```

#### Feature Flag Configuration
```csharp
// In Program.cs  
builder.Configuration.AddAzureAppConfiguration(options =>
{
    options.Connect(connectionString)
           .ConfigureRefresh(refresh => refresh
               .Register("Settings:Sentinel", refreshAll: true)
               .SetCacheExpiration(TimeSpan.FromSeconds(30)))
           .UseFeatureFlags(featureFlags => featureFlags
               .CacheExpirationInterval(TimeSpan.FromSeconds(30)));
});

builder.Services.AddFeatureManagement();
```

### 3. Azure Key Vault

#### Managed Identity Pattern (REQUIRED)
```bicep
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: 'kv-${uniqueString(resourceGroup().id)}'
  location: location
  properties: {
    tenantId: subscription().tenantId
    sku: { name: 'standard', family: 'A' }
    accessPolicies: []
    enabledForTemplateDeployment: true
    enableRbacAuthorization: true
  }
}

// Grant Container App access
resource keyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  name: guid(keyVault.id, containerApp.id, '4633458b-17de-408a-b874-0445c86b69e6')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalId: containerApp.identity.principalId
  }
}
```

#### Secret Reference Pattern
```bash
# Set secrets via azd
azd env set DATABASE_CONNECTION_STRING "@Microsoft.KeyVault(SecretUri=https://your-kv.vault.azure.net/secrets/db-connection)"
```

### 4. Container Apps Configuration

#### Bicep Pattern for Aspire Apps
```bicep
resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'ca-${serviceName}'
  location: location
  properties: {
    environmentId: containerAppsEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        transport: 'http'
        allowInsecure: false
      }
      dapr: {
        enabled: true
        appId: serviceName
        appPort: 8080
      }
    }
    template: {
      containers: [{
        name: serviceName
        image: containerImage
        env: [
          { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
          { name: 'ConnectionStrings__Redis', secretRef: 'redis-connection' }
          { name: 'ConnectionStrings__AppConfig', secretRef: 'appconfig-connection' }
        ]
        resources: {
          cpu: json('0.25')
          memory: '0.5Gi'
        }
      }]
    }
  }
}
```

## CI/CD with GitHub Actions

### Required azd Hooks

#### `.azure/deploy.hooks.yml`
```yaml
prepackage:
  shell: pwsh
  run: |
    Write-Host "Building .NET solution..."
    dotnet restore
    dotnet build --no-restore --configuration Release
    dotnet test --no-build --configuration Release --logger trx --results-directory TestResults
    
postdeploy:
  shell: pwsh  
  run: |
    Write-Host "Running smoke tests..."
    $webUrl = azd env get-values --output json | ConvertFrom-Json | Select-Object -ExpandProperty WEB_URI
    Invoke-RestMethod -Uri "$webUrl/health/detailed" -Method Get
```

### GitHub Actions Workflow Pattern
```yaml
name: Deploy to Azure

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

permissions:
  id-token: write
  contents: read

env:
  AZURE_CLIENT_ID: ${{ vars.AZURE_CLIENT_ID }}
  AZURE_TENANT_ID: ${{ vars.AZURE_TENANT_ID }}
  AZURE_SUBSCRIPTION_ID: ${{ vars.AZURE_SUBSCRIPTION_ID }}

jobs:
  deploy:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '9.0.x'
        
    - name: Install azd
      uses: Azure/setup-azd@v1.0.0
      
    - name: Azure Login
      uses: Azure/login@v2
      with:
        client-id: ${{ vars.AZURE_CLIENT_ID }}
        tenant-id: ${{ vars.AZURE_TENANT_ID }}
        subscription-id: ${{ vars.AZURE_SUBSCRIPTION_ID }}
        
    - name: Deploy to Azure
      run: azd deploy --no-prompt
      env:
        AZURE_ENV_NAME: ${{ vars.AZURE_ENV_NAME }}
```

## Monitoring and Observability

### Application Insights Integration
```csharp
// In ServiceDefaults
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRedisInstrumentation())
    .UseAzureMonitor();
```

### Custom Metrics Pattern
```csharp
public class ApplicationMetrics
{
    private readonly Counter<int> _weatherRequestCounter;
    private readonly Histogram<double> _weatherRequestDuration;
    
    public ApplicationMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("aspire1.WeatherService");
        _weatherRequestCounter = meter.CreateCounter<int>("weather_requests_total");
        _weatherRequestDuration = meter.CreateHistogram<double>("weather_request_duration_ms");
    }
}
```

## Security Best Practices

### Managed Identity Configuration
- **Always use**: System-assigned or user-assigned managed identities
- **Never use**: Connection strings with embedded credentials  
- **Key Vault**: All secrets via Key Vault references with managed identity
- **Redis**: Use managed identity authentication where possible

### Network Security
```bicep
// Container Apps Environment with private networking
resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  properties: {
    vnetConfiguration: {
      internal: true
      infrastructureSubnetId: subnet.id
    }
  }
}
```

## Local Development with Azure Services

### Offline-First Development
- **Redis**: Use local Redis container via Aspire
- **App Configuration**: Fallback to `appsettings.json` for feature flags  
- **Key Vault**: Use User Secrets (`dotnet user-secrets`) locally
- **Application Insights**: Optional in development, required in production

### Development Service Pattern
```csharp
// In AppHost Program.cs
if (builder.Environment.IsDevelopment())
{
    var redis = builder.AddRedis("redis").WithDataVolume();
    // Use local services
}
else
{
    var redis = builder.AddAzureRedis("redis");
    // Use Azure services  
}
```

## Troubleshooting Common Azure Issues

### Container Apps Deployment Issues
1. Check container logs: `az containerapp logs show`  
2. Verify managed identity permissions
3. Validate Bicep template deployment
4. Check azd environment variables: `azd env get-values`

### Service Discovery Issues
1. Verify Dapr configuration in Container Apps
2. Check service naming consistency between AppHost and Bicep
3. Validate internal ingress configuration

### Performance Monitoring
- Use Application Insights dependency tracking
- Monitor Redis cache hit rates
- Track custom metrics for business operations
- Set up Azure Monitor alerts for critical thresholds

## Anti-Patterns to Avoid

❌ **Don't**: Hard-code Azure resource URLs
❌ **Don't**: Use connection strings with embedded credentials  
❌ **Don't**: Deploy to Azure App Service (use Container Apps)
❌ **Don't**: Skip managed identity configuration
❌ **Don't**: Use Basic Redis tier in production
❌ **Don't**: Store secrets in application configuration files

✅ **Do**: Use azd for all deployment operations
✅ **Do**: Leverage Aspire service discovery with Azure resources
✅ **Do**: Use managed identities for all Azure service authentication
✅ **Do**: Implement proper health checks for all services
✅ **Do**: Use Bicep for infrastructure as code