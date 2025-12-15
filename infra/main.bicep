// Main infrastructure file for aspire1 Azure deployment
targetScope = 'resourceGroup'

@description('The environment name (e.g., dev, staging, prod)')
param environment string = 'dev'

@description('The Azure region for all resources')
param location string = resourceGroup().location

@description('The base name for the application')
param appName string = 'aspire1'

@description('Email address for alert notifications')
param alertEmail string

@description('Tags to apply to all resources')
param tags object = {
  Environment: environment
  Application: appName
  ManagedBy: 'Aspire'
}

// Application Insights (includes Log Analytics Workspace)
module appInsights 'app-insights.bicep' = {
  name: '${appName}-appinsights-deployment'
  params: {
    appName: appName
    environment: environment
    location: location
    tags: tags
  }
}

// Dashboard for custom metrics
module dashboard 'dashboard.bicep' = {
  name: '${appName}-dashboard-deployment'
  params: {
    appName: appName
    environment: environment
    location: location
    appInsightsId: appInsights.outputs.appInsightsId
    appInsightsName: appInsights.outputs.appInsightsName
    tags: tags
  }
}

// Alert rules for monitoring
module alerts 'alerts.bicep' = {
  name: '${appName}-alerts-deployment'
  params: {
    appName: appName
    environment: environment
    location: location
    appInsightsId: appInsights.outputs.appInsightsId
    alertEmail: alertEmail
    tags: tags
  }
}

// Azure App Configuration for feature flags
module appConfig 'app-config.bicep' = {
  name: '${appName}-appconfig-deployment'
  params: {
    appName: appName
    environment: environment
    location: location
    tags: tags
  }
}

// Azure Cache for Redis for distributed caching
module redis 'redis.bicep' = {
  name: '${appName}-redis-deployment'
  params: {
    appName: appName
    environment: environment
    location: location
    redisSku: 'Standard'
    redisCapacity: 1
    tags: tags
  }
}

// Outputs for use by azd and other modules
output appInsightsConnectionString string = appInsights.outputs.connectionString
output appInsightsInstrumentationKey string = appInsights.outputs.instrumentationKey
output appInsightsName string = appInsights.outputs.appInsightsName
output logAnalyticsWorkspaceId string = appInsights.outputs.workspaceId
output dashboardId string = dashboard.outputs.dashboardId
output appConfigEndpoint string = appConfig.outputs.appConfigEndpoint
output appConfigName string = appConfig.outputs.appConfigName
output redisConnectionString string = redis.outputs.redisConnectionString
output redisHostName string = redis.outputs.redisHostName
output redisName string = redis.outputs.redisName
