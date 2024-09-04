@description('The region where the KeyVault is deployed in.')
param location string

@description('''
  The suffix of the unique identifier for the resources of the current deployment.
  Used to avoid name collisions and to link resources part of the same deployment together.
''')
param resourceSuffixUID string

@description('The resource ID of the VNET where the KeyVault is connected to.')
param vnetId string

@description('The resource ID of the subnet where the KeyVault is connected to.')
param infraSubnetId string

@description('The resource ID of the Log Analytics workspace to which the KeyVault is connected to.')
param workspaceId string

// Note: no specific configuration for Zone Redundancy
// See: https://learn.microsoft.com/en-us/Azure/key-vault/general/disaster-recovery-guidance
resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' = {
  name: 'keyvault-${resourceSuffixUID}'
  location: location
  properties: {
    sku: {
      name: 'standard'
      family: 'A'
    }
    tenantId: tenant().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    publicNetworkAccess: 'Disabled'
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Deny'
      ipRules: []
      virtualNetworkRules: []
    }
  }
}

// private DNS zone
resource privateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.vaultcore.azure.net' // See: https://github.com/Azure/bicep/issues/9708
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
  name: 'keyvault-pl-${resourceSuffixUID}'
  location: location
  properties: {
    privateLinkServiceConnections: [
      {
        name: 'keyvault-link'
        properties: {
          privateLinkServiceId: keyVault.id
          groupIds: [
            'vault'
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
    name: 'vault-dns'
    properties: {
      privateDnsZoneConfigs: [
        {
          name: 'vault-config'
          properties: {
            privateDnsZoneId: privateDnsZone.id
          }
        }
      ]
    }
  }
}

resource diagnosticLogs 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: keyVault.name
  scope: keyVault
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

output keyvaultId string = keyVault.id
output keyvaultName string = keyVault.name
