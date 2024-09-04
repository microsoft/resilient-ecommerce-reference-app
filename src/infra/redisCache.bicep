@description('The region where the Redis Cache is deployed in.')
param location string

@description('''
  The suffix of the unique identifier for the resources of the current deployment.
  Used to avoid name collisions and to link resources part of the same deployment together.
''')
param resourceSuffixUID string

@description('The resource ID of the VNET to which the Redis Cache is connected to.')
param vnetId string

@description('The resource ID of the subnet to which the Redis Cache is connected to.')
param infraSubnetId string

@description('The resource ID of the Log Analytics workspace to which the Redis Cache is connected to.')
param workspaceId string

@description('The name of the App Managed Identity to which the Data Contributor role is assigned to.')
param appManagedIdentityName string

@description('The principal ID of the App Managed Identity to which the Data Contributor role is assigned to.')
param appManagedIdentityPrincipalId string

@description('The zones where the Redis Cache is deployed in. To be used for Zone Resiliency.')
param redisZones array = ['1', '2', '3']

@description('The name of the Key Vault where the Redis Cache properties will be stored.')
param keyVaultName string

var redisCacheSkuName = 'Premium'
var redisCacheFamilyName = 'P'
var redisCacheCapacity = 2

resource redisCache 'Microsoft.Cache/redis@2023-08-01' = {
  name: 'redis-${resourceSuffixUID}'
  location: location
  properties: {
    redisVersion: '6.0'
    minimumTlsVersion: '1.2'
    sku: {
      name: redisCacheSkuName
      family: redisCacheFamilyName
      capacity: redisCacheCapacity
    }
    enableNonSslPort: false

    publicNetworkAccess: 'Disabled'
    redisConfiguration: {
      'maxmemory-reserved': '30'
      'maxfragmentationmemory-reserved': '30'
      'maxmemory-delta': '30'
      'aad-enabled': 'True'
    }
  }
  zones: redisZones
}

// Assign Data Contributor to our App Managed Identity
resource rbacAssignment 'Microsoft.Cache/redis/accessPolicyAssignments@2023-08-01' = {
  parent: redisCache
  name: appManagedIdentityName
  properties: {
    accessPolicyName: 'Data Contributor'
    objectId: appManagedIdentityPrincipalId
    objectIdAlias: appManagedIdentityName
  }
}

// private DNS zone
resource privateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.redis.cache.windows.net'
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
  name: 'redis-pl-${resourceSuffixUID}'
  location: location
  properties: {
    privateLinkServiceConnections: [
      {
        name: 'redis-link'
        properties: {
          privateLinkServiceId: redisCache.id
          groupIds: [
            'redisCache'
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
    name: 'reds-dns'
    properties: {
      privateDnsZoneConfigs: [
        {
          name: 'redis-config'
          properties: {
            privateDnsZoneId: privateDnsZone.id
          }
        }
      ]
    }
  }
}

resource diagnosticLogs 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: redisCache.name
  scope: redisCache
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

// put redis properties into key vault
resource keyVault 'Microsoft.KeyVault/vaults@2021-11-01-preview' existing = {
  name: keyVaultName
}

resource redisEndpointSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'redis-endpoint'
  properties: {
    value: redisCache.properties.hostName
  }
}

output redisCacheId string = redisCache.id
output redisCacheName string = redisCache.name
