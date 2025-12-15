@description('The name of the application')
param appName string

@description('The environment name (e.g., dev, staging, prod)')
param environment string

@description('The location for all resources')
param location string = resourceGroup().location

@description('Tags to apply to all resources')
param tags object = {}

var appConfigName = '${appName}-${environment}-appconfig'

resource appConfiguration 'Microsoft.AppConfiguration/configurationStores@2023-03-01' = {
  name: appConfigName
  location: location
  tags: tags
  sku: {
    name: 'standard'
  }
  properties: {
    enablePurgeProtection: false
    publicNetworkAccess: 'Enabled'
  }
  identity: {
    type: 'SystemAssigned'
  }
}

output appConfigId string = appConfiguration.id
output appConfigName string = appConfiguration.name
output appConfigEndpoint string = appConfiguration.properties.endpoint
