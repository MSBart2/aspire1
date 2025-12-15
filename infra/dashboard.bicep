// Application Insights Dashboard with custom metrics
targetScope = 'resourceGroup'

@description('The base name for the application')
param appName string

@description('The environment name (e.g., dev, staging, prod)')
param environment string

@description('The Azure region for all resources')
param location string

@description('The Application Insights resource ID')
param appInsightsId string

@description('The Application Insights resource name')
param appInsightsName string

@description('Tags to apply to all resources')
param tags object

// Dashboard resource
resource dashboard 'Microsoft.Portal/dashboards@2020-09-01-preview' = {
  name: '${appName}-dashboard-${environment}'
  location: location
  tags: union(tags, {
    'hidden-title': '${appName} Telemetry Dashboard (${environment})'
  })
  properties: {
    lenses: [
      {
        order: 0
        parts: [
          // Counter Clicks by Range
          {
            position: {
              x: 0
              y: 0
              colSpan: 6
              rowSpan: 4
            }
            metadata: {
              inputs: [
                {
                  name: 'resourceId'
                  value: appInsightsId
                }
                {
                  name: 'query'
                  value: 'customMetrics\n| where name == "counter.clicks"\n| extend range = tostring(customDimensions.range)\n| summarize TotalClicks = sum(value) by range\n| order by TotalClicks desc'
                }
                {
                  name: 'chartType'
                  value: 'Bar'
                }
                {
                  name: 'title'
                  value: 'Counter Clicks by Range'
                }
              ]
              type: 'Extension/Microsoft_OperationsManagementSuite_Workspace/PartType/LogsDashboardPart'
            }
          }
          // Sunny Forecasts Over Time
          {
            position: {
              x: 6
              y: 0
              colSpan: 6
              rowSpan: 4
            }
            metadata: {
              inputs: [
                {
                  name: 'resourceId'
                  value: appInsightsId
                }
                {
                  name: 'query'
                  value: 'customMetrics\n| where name == "weather.sunny.count"\n| extend temp_range = tostring(customDimensions.temperature_range)\n| summarize SunnyCount = sum(value) by bin(timestamp, 1h), temp_range\n| order by timestamp asc'
                }
                {
                  name: 'chartType'
                  value: 'Line'
                }
                {
                  name: 'title'
                  value: 'Sunny Forecasts by Temperature Range'
                }
              ]
              type: 'Extension/Microsoft_OperationsManagementSuite_Workspace/PartType/LogsDashboardPart'
            }
          }
          // Cache Hit/Miss Ratio
          {
            position: {
              x: 0
              y: 4
              colSpan: 6
              rowSpan: 4
            }
            metadata: {
              inputs: [
                {
                  name: 'resourceId'
                  value: appInsightsId
                }
                {
                  name: 'query'
                  value: 'let hits = customMetrics | where name == "cache.hits" | summarize Hits = sum(value);\nlet misses = customMetrics | where name == "cache.misses" | summarize Misses = sum(value);\nhits | extend Misses = toscalar(misses) | extend Total = Hits + Misses, HitRate = round(Hits * 100.0 / (Hits + Misses), 2) | project Type = "Hits", Count = Hits, Percentage = HitRate\n| union (misses | extend Hits = toscalar(hits) | extend Total = Hits + Misses, MissRate = round(Misses * 100.0 / (Hits + Misses), 2) | project Type = "Misses", Count = Misses, Percentage = MissRate)'
                }
                {
                  name: 'chartType'
                  value: 'Pie'
                }
                {
                  name: 'title'
                  value: 'Cache Hit vs Miss Ratio'
                }
              ]
              type: 'Extension/Microsoft_OperationsManagementSuite_Workspace/PartType/LogsDashboardPart'
            }
          }
          // API Call Duration Percentiles
          {
            position: {
              x: 6
              y: 4
              colSpan: 6
              rowSpan: 4
            }
            metadata: {
              inputs: [
                {
                  name: 'resourceId'
                  value: appInsightsId
                }
                {
                  name: 'query'
                  value: 'customMetrics\n| where name == "api.call.duration"\n| extend endpoint = tostring(customDimensions.endpoint)\n| summarize P50 = percentile(value, 50), P95 = percentile(value, 95), P99 = percentile(value, 99) by bin(timestamp, 5m), endpoint\n| order by timestamp asc'
                }
                {
                  name: 'chartType'
                  value: 'Line'
                }
                {
                  name: 'title'
                  value: 'API Call Duration (P50, P95, P99)'
                }
              ]
              type: 'Extension/Microsoft_OperationsManagementSuite_Workspace/PartType/LogsDashboardPart'
            }
          }
          // Weather API Call Volume
          {
            position: {
              x: 0
              y: 8
              colSpan: 12
              rowSpan: 4
            }
            metadata: {
              inputs: [
                {
                  name: 'resourceId'
                  value: appInsightsId
                }
                {
                  name: 'query'
                  value: 'customMetrics\n| where name == "weather.api.calls"\n| summarize CallCount = sum(value) by bin(timestamp, 5m)\n| order by timestamp asc'
                }
                {
                  name: 'chartType'
                  value: 'Area'
                }
                {
                  name: 'title'
                  value: 'Weather API Call Volume Over Time'
                }
              ]
              type: 'Extension/Microsoft_OperationsManagementSuite_Workspace/PartType/LogsDashboardPart'
            }
          }
        ]
      }
    ]
    metadata: {
      model: {
        timeRange: {
          value: {
            relative: {
              duration: 24
              timeUnit: 1 // Hours
            }
          }
          type: 'MsPortalFx.Composition.Configuration.ValueTypes.TimeRange'
        }
        filterLocale: {
          value: 'en-us'
        }
        filters: {
          value: {
            MsPortalFx_TimeRange: {
              model: {
                format: 'local'
                granularity: 'auto'
                relative: '24h'
              }
              displayCache: {
                name: 'Local Time'
                value: 'Past 24 hours'
              }
            }
          }
        }
      }
    }
  }
}

// Outputs
output dashboardId string = dashboard.id
output dashboardName string = dashboard.name
