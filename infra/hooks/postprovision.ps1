#!/usr/bin/env pwsh
# Postprovision hook to configure Azure App Configuration with feature flags

param(
    [string]$ResourceGroupName = $env:AZURE_RESOURCE_GROUP_NAME,
    [string]$AppConfigName = $env:AZURE_APPCONFIG_NAME,
    [string]$Environment = $env:AZURE_ENV_NAME
)

Write-Host "🔧 Configuring Azure App Configuration..." -ForegroundColor Cyan

if (-not $AppConfigName) {
    Write-Host "⚠️  AZURE_APPCONFIG_NAME not set, skipping feature flag configuration" -ForegroundColor Yellow
    exit 0
}

# Create feature flags if they don't exist
$features = @(
    @{
        name        = "WeatherForecast"
        description = "Enable weather forecast feature"
        enabled     = $true
    },
    @{
        name        = "DetailedHealth"
        description = "Enable detailed health endpoints with version metadata"
        enabled     = $true
    }
)

foreach ($feature in $features) {
    Write-Host "  Setting feature flag: $($feature.name)" -ForegroundColor Gray

    $label = if ($Environment) { $Environment } else { "Development" }

    az appconfig feature set `
        --name $AppConfigName `
        --feature $feature.name `
        --label $label `
        --description $feature.description `
        --yes `
        2>&1 | Out-Null

    if ($LASTEXITCODE -eq 0) {
        $status = if ($feature.enabled) { "✅ Enabled" } else { "⚙️  Disabled" }
        Write-Host "    $status - $($feature.name) [$label]" -ForegroundColor Green
    }
    else {
        Write-Host "    ⚠️  Failed to set $($feature.name)" -ForegroundColor Yellow
    }
}

Write-Host "✅ Azure App Configuration configured" -ForegroundColor Green
Write-Host ""
Write-Host "📊 View feature flags:" -ForegroundColor Cyan
Write-Host "   Portal: https://portal.azure.com/#resource/subscriptions/$env:AZURE_SUBSCRIPTION_ID/resourceGroups/$ResourceGroupName/providers/Microsoft.AppConfiguration/configurationStores/$AppConfigName/featureManager" -ForegroundColor Gray
Write-Host ""
