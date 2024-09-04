@description('The region where the ACR is deployed in.')
param location string

@description('''
  The suffix of the unique identifier for the resources of the current deployment.
  Used to avoid name collisions and to link resources part of the same deployment together.
''')
param resourceSuffixUID string

@description('The resource ID of the VNET where the ACR is connected to.')
param vnetId string

@description('The resource ID of the subnet where the ACR is connected to.')
param infraSubnetId string

@description('The resource ID of the Log Analytics workspace to which the ACR is connected to.')
param workspaceId string

var containerRegistryName = 'containerregistry${resourceSuffixUID}'

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2021-09-01' = {
  name: containerRegistryName
  location: location
  sku: {
    name: 'Premium'
  }
  properties: {
    adminUserEnabled: true
    dataEndpointEnabled: false
    policies: {
      quarantinePolicy: {
        status: 'disabled'
      }
      retentionPolicy: {
        status: 'enabled'
        days: 7
      }
      trustPolicy: {
        status: 'disabled'
        type: 'Notary'
      }
    }
    publicNetworkAccess: 'Enabled'
    zoneRedundancy: 'Enabled'
  }
}

// private DNS zone
resource privateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.azurecr.io'
  location: 'global'
}
// link to VNET
resource privateDnsZoneVnetLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: privateDnsZone
  name: 'vnetLink'
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: vnetId
    }
  }
}

resource privateEndpoint 'Microsoft.Network/privateEndpoints@2022-01-01' = {
  name: '${containerRegistryName}-acr'
  location: location
  properties: {
    privateLinkServiceConnections: [
      {
        name: 'acr-link'
        properties: {
          privateLinkServiceId: containerRegistry.id
          groupIds: [
            'registry'
          ] 
        }
      }
    ]
    subnet: {
      id: infraSubnetId
    }
  }

  // register in DNS
  resource privateDnsZoneGroup 'privateDnsZoneGroups@2022-01-01' = {
    name:  'acr-dns'
    properties:{
      privateDnsZoneConfigs: [
        {
          name: 'acr-config'
          properties:{
            privateDnsZoneId: privateDnsZone.id
          }
        }
      ]
    }
  }
}

resource diagnosticLogs 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: containerRegistry.name
  scope:containerRegistry
  properties: {
    workspaceId: workspaceId
    logs: [
      {
        categoryGroup: 'audit'
        enabled: true
      }
      {
        categoryGroup: 'allLogs'
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

output containerRegistryId string = containerRegistry.id
output containerRegistryName string = containerRegistry.name
output resourceGroupName string = resourceGroup().name
