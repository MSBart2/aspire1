// Alert rules for Application Insights monitoring
targetScope = 'resourceGroup'

@description('The base name for the application')
param appName string

@description('The environment name (e.g., dev, staging, prod)')
param environment string

@description('The Azure region for all resources')
param location string

@description('The Application Insights resource ID')
param appInsightsId string

@description('Email address for alert notifications')
param alertEmail string

@description('Tags to apply to all resources')
param tags object

// Action Group for email notifications
resource actionGroup 'Microsoft.Insights/actionGroups@2023-01-01' = {
  name: '${appName}-alerts-${environment}'
  location: 'global'
  tags: tags
  properties: {
    groupShortName: 'AspireAlert'
    enabled: true
    emailReceivers: [
      {
        name: 'AlertEmail'
        emailAddress: alertEmail
        useCommonAlertSchema: true
      }
    ]
  }
}

// Alert: Cache Miss Rate > 50%
resource cacheMissAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: '${appName}-cache-miss-alert-${environment}'
  location: 'global'
  tags: tags
  properties: {
    description: 'Alert when cache miss rate exceeds 50% over 5 minutes'
    severity: 2 // Warning
    enabled: true
    scopes: [
      appInsightsId
    ]
    evaluationFrequency: 'PT5M' // Every 5 minutes
    windowSize: 'PT5M' // 5-minute window
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'CacheMissRate'
          metricName: 'cache.misses'
          metricNamespace: 'microsoft.insights/components/kusto'
          operator: 'GreaterThan'
          threshold: 50
          timeAggregation: 'Total'
          criterionType: 'StaticThresholdCriterion'
        }
      ]
    }
    autoMitigate: true
    actions: [
      {
        actionGroupId: actionGroup.id
      }
    ]
  }
}

// Scheduled Query Alert: API Error Rate > 5 per minute
resource apiErrorAlert 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = {
  name: '${appName}-api-error-alert-${environment}'
  location: location
  tags: tags
  properties: {
    description: 'Alert when API error rate exceeds 5 errors per minute'
    severity: 1 // Error
    enabled: true
    scopes: [
      appInsightsId
    ]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT5M'
    criteria: {
      allOf: [
        {
          query: '''
            requests
            | where success == false
            | where name contains "weatherforecast"
            | summarize ErrorCount = count() by bin(timestamp, 1m)
            | where ErrorCount > 5
          '''
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: 0
          failingPeriods: {
            numberOfEvaluationPeriods: 1
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    autoMitigate: true
    actions: {
      actionGroups: [
        actionGroup.id
      ]
    }
  }
}

// Additional Alert: High API Call Duration (P95 > 1000ms)
resource apiLatencyAlert 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = {
  name: '${appName}-api-latency-alert-${environment}'
  location: location
  tags: tags
  properties: {
    description: 'Alert when P95 API call duration exceeds 1000ms'
    severity: 2 // Warning
    enabled: true
    scopes: [
      appInsightsId
    ]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT10M'
    criteria: {
      allOf: [
        {
          query: '''
            customMetrics
            | where name == "api.call.duration"
            | summarize P95 = percentile(value, 95) by bin(timestamp, 5m)
            | where P95 > 1000
          '''
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: 0
          failingPeriods: {
            numberOfEvaluationPeriods: 2
            minFailingPeriodsToAlert: 2
          }
        }
      ]
    }
    autoMitigate: true
    actions: {
      actionGroups: [
        actionGroup.id
      ]
    }
  }
}

// Outputs
output actionGroupId string = actionGroup.id
output cacheMissAlertId string = cacheMissAlert.id
output apiErrorAlertId string = apiErrorAlert.id
output apiLatencyAlertId string = apiLatencyAlert.id
