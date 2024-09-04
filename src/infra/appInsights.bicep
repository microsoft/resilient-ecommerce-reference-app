@description('The region where the App Insights is deployed in.')
param location string

@description('''
  The suffix of the unique identifier for the resources of the current deployment.
  Used to avoid name collisions and to link resources part of the same deployment together.
''')
param resourceSuffixUID string

@description('The resource ID of the Log Analytics workspace to which the App Insights is connected to.')
param workspaceId string

@description('The name of the Key Vault where the App Insights connection string is stored.')
param keyVaultName string

resource insights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'insights-${resourceSuffixUID}'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    DisableLocalAuth: false
    ForceCustomerStorageForProfiler: false
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
    RetentionInDays: 30
    SamplingPercentage: json('100')
    WorkspaceResourceId: workspaceId
  }
}

// populate connection string in key vault
resource keyVault 'Microsoft.KeyVault/vaults@2021-11-01-preview' existing = {
  name: keyVaultName
}

resource appInsightsConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'app-insights-connection-string'
  properties: {
    value: insights.properties.ConnectionString
  }
}

output appInsightsName string = insights.name
output appInsightsId string = insights.id
