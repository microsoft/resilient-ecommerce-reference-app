@description('The region where the Log Analytics is deployed in.')
param location string

@description('''
  The suffix of the unique identifier for the resources of the current deployment.
  Used to avoid name collisions and to link resources part of the same deployment together.
''')
param resourceSuffixUID string

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2021-06-01' = {
  name: 'insights-ws-${resourceSuffixUID}'
  location: location
  properties: {
    retentionInDays: 90
    sku: {
      name: 'PerGB2018'
    }
  }
}

output workspaceId string = logAnalyticsWorkspace.id
output workspaceName string = logAnalyticsWorkspace.name
