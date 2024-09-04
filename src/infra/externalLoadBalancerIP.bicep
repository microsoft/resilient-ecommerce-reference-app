@description('The region where the Public IP is deployed in.')
param location string

@description('''
  The suffix of the unique identifier for the resources of the current deployment.
  Used to avoid name collisions and to link resources part of the same deployment together.
''')
param resourceSuffixUID string

@description('The resource ID of the Log Analytics workspace to which the Public IP is connected to.')
param workspaceId string

resource publicIP 'Microsoft.Network/publicIPAddresses@2022-01-01' = {
  name: 'elb-pip-${resourceSuffixUID}'
  location: location
  zones: ['1', '2', '3']
  sku: {
    name: 'Standard'
  }
  properties: {
    publicIPAllocationMethod: 'Static'
    publicIPAddressVersion: 'IPv4'
  }
}

resource diagnosticLogs 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: publicIP.name
  scope: publicIP
  properties: {
    workspaceId: workspaceId
    logs: [
      {
        categoryGroup: 'audit'
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
      }
    ]
  }
}

output externalLoadBalancerIP string = publicIP.properties.ipAddress
output externalLoadBalancerIPId string = publicIP.id
