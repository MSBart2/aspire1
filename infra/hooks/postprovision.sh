#!/bin/bash
# Postprovision hook to configure Azure App Configuration with feature flags

RESOURCE_GROUP_NAME="${AZURE_RESOURCE_GROUP_NAME}"
APPCONFIG_NAME="${AZURE_APPCONFIG_NAME}"
ENVIRONMENT="${AZURE_ENV_NAME:-Development}"

echo "🔧 Configuring Azure App Configuration..."

if [ -z "$APPCONFIG_NAME" ]; then
    echo "⚠️  AZURE_APPCONFIG_NAME not set, skipping feature flag configuration"
    exit 0
fi

# Create feature flags if they don't exist
declare -a features=(
    "WeatherForecast:Enable weather forecast feature:true"
    "DetailedHealth:Enable detailed health endpoints with version metadata:true"
)

for feature_info in "${features[@]}"; do
    IFS=':' read -r name description enabled <<< "$feature_info"
    echo "  Setting feature flag: $name"

    az appconfig feature set \
        --name "$APPCONFIG_NAME" \
        --feature "$name" \
        --label "$ENVIRONMENT" \
        --description "$description" \
        --yes \
        >/dev/null 2>&1

    if [ $? -eq 0 ]; then
        if [ "$enabled" = "true" ]; then
            echo "    ✅ Enabled - $name [$ENVIRONMENT]"
        else
            echo "    ⚙️  Disabled - $name [$ENVIRONMENT]"
        fi
    else
        echo "    ⚠️  Failed to set $name"
    fi
done

echo "✅ Azure App Configuration configured"
echo ""
echo "📊 View feature flags:"
echo "   Portal: https://portal.azure.com/#resource/subscriptions/$AZURE_SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP_NAME/providers/Microsoft.AppConfiguration/configurationStores/$APPCONFIG_NAME/featureManager"
echo ""
