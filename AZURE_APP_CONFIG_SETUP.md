# Azure App Configuration Setup Guide

This guide walks you through setting up Azure App Configuration with feature flags for the aspire1 solution.

## 🚀 Quick Setup

### 1. Create Azure App Configuration Resource

```bash
# Set variables
$RESOURCE_GROUP="rg-aspire1-dev"
$LOCATION="eastus"
$APPCONFIG_NAME="appconfig-aspire1-dev"  # Must be globally unique

# Create resource group (if not exists)
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create App Configuration
az appconfig create `
  --name $APPCONFIG_NAME `
  --resource-group $RESOURCE_GROUP `
  --location $LOCATION `
  --sku Standard
```

### 2. Enable Managed Identity

```bash
# Get your user's Object ID
$USER_OBJECT_ID = az ad signed-in-user show --query id -o tsv

# Assign "App Configuration Data Reader" role for local development
az role assignment create `
  --role "App Configuration Data Reader" `
  --assignee $USER_OBJECT_ID `
  --scope "/subscriptions/YOUR_SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.AppConfiguration/configurationStores/$APPCONFIG_NAME"
```

### 3. Create Feature Flags

```bash
# Create WeatherForecast feature flag (enabled by default)
az appconfig feature set `
  --name $APPCONFIG_NAME `
  --feature WeatherForecast `
  --label Development `
  --yes

# Create DetailedHealth feature flag (enabled by default)
az appconfig feature set `
  --name $APPCONFIG_NAME `
  --feature DetailedHealth `
  --label Development `
  --yes
```

### 4. Configure Local Development

Add the App Configuration endpoint to your user secrets:

```bash
# Navigate to Web project
cd aspire1.Web

# Set the App Configuration endpoint
$APPCONFIG_ENDPOINT = az appconfig show `
  --name $APPCONFIG_NAME `
  --resource-group $RESOURCE_GROUP `
  --query endpoint -o tsv

dotnet user-secrets set "AppConfig:Endpoint" $APPCONFIG_ENDPOINT
```

### 5. Test Locally

```bash
# Login to Azure CLI (if not already)
az login

# Run the application
cd ..
dotnet run --project aspire1.AppHost

# Navigate to:
# - http://localhost:5188/features  (View feature flags)
# - http://localhost:5188/weather   (Test feature-gated page)
```

## 🔧 Managing Feature Flags

### Azure Portal

1. Navigate to your App Configuration resource
2. Click **"Feature manager"** in the left menu
3. Toggle features on/off
4. Changes take effect within 30 seconds (cache refresh interval)

### Azure CLI

```bash
# Enable a feature
az appconfig feature set `
  --name $APPCONFIG_NAME `
  --feature WeatherForecast `
  --label Development `
  --yes

# Disable a feature
az appconfig feature disable `
  --name $APPCONFIG_NAME `
  --feature WeatherForecast `
  --label Development
```

### REST API

```bash
# Get feature flag status
curl -X GET "https://$APPCONFIG_NAME.azconfig.io/kv/.appconfig.featureflag%2FWeatherForecast" `
  -H "Authorization: Bearer $ACCESS_TOKEN"
```

## 🌍 Environment-Specific Configuration

Use labels to separate feature flags by environment:

```bash
# Development environment
az appconfig feature set --name $APPCONFIG_NAME --feature WeatherForecast --label Development --yes

# Staging environment
az appconfig feature set --name $APPCONFIG_NAME --feature WeatherForecast --label Staging --yes

# Production environment
az appconfig feature set --name $APPCONFIG_NAME --feature WeatherForecast --label Production --yes
```

Update `Program.cs` to use environment-specific labels:

```csharp
options.Connect(new Uri(appConfigEndpoint), new DefaultAzureCredential())
       .UseFeatureFlags(featureFlagOptions =>
       {
           featureFlagOptions.CacheExpirationInterval = TimeSpan.FromSeconds(30);
           featureFlagOptions.Select("*", builder.Environment.EnvironmentName);
       });
```

## 🔐 Azure Container Apps Deployment

### Option 1: Managed Identity (Recommended)

```bash
# Enable system-assigned managed identity on ACA
az containerapp identity assign `
  --name aspire1-web `
  --resource-group $RESOURCE_GROUP `
  --system-assigned

# Get the managed identity principal ID
$IDENTITY_PRINCIPAL_ID = az containerapp identity show `
  --name aspire1-web `
  --resource-group $RESOURCE_GROUP `
  --query principalId -o tsv

# Assign "App Configuration Data Reader" role
az role assignment create `
  --role "App Configuration Data Reader" `
  --assignee $IDENTITY_PRINCIPAL_ID `
  --scope "/subscriptions/YOUR_SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.AppConfiguration/configurationStores/$APPCONFIG_NAME"

# Set environment variable in ACA
az containerapp update `
  --name aspire1-web `
  --resource-group $RESOURCE_GROUP `
  --set-env-vars "AppConfig__Endpoint=$APPCONFIG_ENDPOINT"
```

### Option 2: Connection String (Development Only)

```bash
# Get connection string
$CONNECTION_STRING = az appconfig credential list `
  --name $APPCONFIG_NAME `
  --resource-group $RESOURCE_GROUP `
  --query "[0].connectionString" -o tsv

# Store in Key Vault (never in code!)
az keyvault secret set `
  --vault-name kv-aspire1-dev `
  --name "AppConfigConnectionString" `
  --value $CONNECTION_STRING
```

## 📊 Adding New Feature Flags

### 1. Create Feature Flag in Azure

```bash
az appconfig feature set `
  --name $APPCONFIG_NAME `
  --feature MyNewFeature `
  --label Development `
  --description "Description of the feature" `
  --yes
```

### 2. Use in Code

**Option A: IFeatureManager (runtime check)**

```csharp
@inject IFeatureManager FeatureManager

@code {
    private bool isEnabled = false;

    protected override async Task OnInitializedAsync()
    {
        isEnabled = await FeatureManager.IsEnabledAsync("MyNewFeature");
    }
}
```

**Option B: FeatureGate Attribute (controller/API)**

```csharp
[FeatureGate("MyNewFeature")]
public IActionResult MyNewEndpoint()
{
    return Ok("Feature is enabled!");
}
```

### 3. Add Local Fallback

Update `appsettings.Development.json`:

```json
{
  "FeatureManagement": {
    "WeatherForecast": true,
    "DetailedHealth": true,
    "MyNewFeature": true
  }
}
```

## 🎯 Best Practices

### ✅ DO

- Use managed identity (never connection strings in production)
- Configure sentinel key for cache refresh (30 seconds)
- Namespace feature flags: `FeatureName:SubFeature`
- Use labels for environments (Development, Staging, Production)
- Keep flags short-lived (<90 days)
- Remove dead flags aggressively
- Test both enabled/disabled paths
- Use local `appsettings.json` fallbacks for offline development

### ❌ DON'T

- Store connection strings in code or appsettings.json
- Use feature flags for secrets or configuration data
- Keep flags alive indefinitely (causes tech debt)
- Skip testing disabled state
- Use production flags in development
- Forget to document flag purpose and expiration date

## 🧪 Testing Feature Flags

### Unit Tests

```csharp
using Microsoft.FeatureManagement;
using Moq;
using Xunit;

public class WeatherPageTests
{
    [Fact]
    public async Task Weather_FeatureDisabled_ShowsWarning()
    {
        // Arrange
        var featureManager = new Mock<IFeatureManager>();
        featureManager.Setup(x => x.IsEnabledAsync("WeatherForecast"))
                     .ReturnsAsync(false);

        // Act & Assert
        // Test component behavior when feature is disabled
    }
}
```

### Integration Tests

```csharp
// Use InMemoryFeatureManager for predictable tests
builder.Services.AddSingleton<IFeatureManager>(new InMemoryFeatureManager(new Dictionary<string, bool>
{
    ["WeatherForecast"] = true,
    ["DetailedHealth"] = false
}));
```

## 📚 Troubleshooting

### Feature Flags Not Updating

**Problem:** Changes in Azure Portal not reflected in app

**Solution:**

1. Check cache expiration interval (default: 30 seconds)
2. Verify `app.UseAzureAppConfiguration()` middleware is called
3. Ensure managed identity has "App Configuration Data Reader" role

### Authentication Failed

**Problem:** "401 Unauthorized" when accessing App Configuration

**Solution:**

```bash
# Verify Azure CLI login
az account show

# Check role assignment
az role assignment list --assignee $USER_OBJECT_ID --all
```

### Local Development Not Using Azure

**Problem:** App uses local `appsettings.json` instead of Azure

**Solution:**

1. Ensure user secrets are set: `dotnet user-secrets list`
2. Verify endpoint format: `https://appconfig-aspire1-dev.azconfig.io`
3. Check environment: `ASPNETCORE_ENVIRONMENT=Development`

## 🔗 Resources

- [Azure App Configuration Documentation](https://learn.microsoft.com/azure/azure-app-configuration/)
- [Feature Management Documentation](https://learn.microsoft.com/azure/azure-app-configuration/feature-management-dotnet-reference)
- [Managed Identity Authentication](https://learn.microsoft.com/azure/azure-app-configuration/howto-integrate-azure-managed-service-identity)

---

**Next Steps:**

1. Set up App Configuration in Azure
2. Configure managed identity
3. Test feature flags locally
4. Deploy to ACA with managed identity
