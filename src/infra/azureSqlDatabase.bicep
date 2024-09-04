param location string
param resourceSuffixUID string
param vnetId string
param infraSubnetId string
param workspaceId string
param azureSqlZoneRedundant bool
param keyVaultName string
param managedIdentityClientId string

resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name: 'sql-${resourceSuffixUID}'
  location: location
  properties: {
    // using a workaround to set the admin as the managed identity of the AKS clusters. Do not use this in production.
    administrators: {
      azureADOnlyAuthentication: true
      login: 'ManagedIdentityAdmin'
      administratorType: 'ActiveDirectory'
      sid: managedIdentityClientId
      tenantId: subscription().tenantId
      principalType: 'Application'
    }
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Disabled'
    version: '12.0'
  }
}

var sqlAppDatabaseName = 'az-ref-app'
var sqlCatalogName = sqlAppDatabaseName
var skuTierName = 'Premium'
var dtuCapacity = 125 
var requestedBackupStorageRedundancy = 'Local'
var readScale = 'Enabled'

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-05-01-preview' = {
  parent: sqlServer
  name: 'az-ref-app'
  location: location
  tags: {
    displayName: sqlCatalogName
  }
  sku: {
    name: skuTierName
    tier: skuTierName
    capacity: dtuCapacity
  }
  properties: {
    requestedBackupStorageRedundancy: requestedBackupStorageRedundancy
    readScale: readScale
    zoneRedundant: azureSqlZoneRedundant
  }
}

/*
// To allow applications hosted inside Azure to connect to your SQL server, Azure connections must be enabled. 
// To enable Azure connections, there must be a firewall rule with starting and ending IP addresses set to 0.0.0.0. 
// This recommended rule is only applicable to Azure SQL Database.
// Ref: https://learn.microsoft.com/azure/azure-sql/database/firewall-configure?view=azuresql#connections-from-inside-azure
resource allowAllWindowsAzureIps 'Microsoft.Sql/servers/firewallRules@2021-11-01-preview' = {
  name: 'AllowAllWindowsAzureIps'
  parent: sqlServer
  properties: {
    endIpAddress: '0.0.0.0'
    startIpAddress: '0.0.0.0'
  }
}

*/

// private DNS zone
resource privateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink${environment().suffixes.sqlServerHostname}'
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
  name: 'sql-privatelink-${resourceSuffixUID}'
  location: location
  properties: {
    privateLinkServiceConnections: [
      {
        name: 'sql-link'
        properties: {
          privateLinkServiceId: sqlServer.id
          groupIds: ['sqlServer']
        }
      }
    ]
    subnet: {
      id: infraSubnetId
    }
  }
  // register in DNS
  resource privateDnsZoneGroup 'privateDnsZoneGroups@2022-01-01' = {
    name: 'sql-dns-${resourceSuffixUID}'
    properties: {
      privateDnsZoneConfigs: [
        {
          name: 'sql-config'
          properties: {
            privateDnsZoneId: privateDnsZone.id
          }
        }
      ]
    }
  }
}

resource diagnosticLogs 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: sqlDatabase.name
  scope: sqlDatabase
  properties: {
    workspaceId: workspaceId
    logs: [
      {
        categoryGroup: 'allLogs'
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'Basic'
        enabled: true
      }
      {
        category: 'InstanceAndAppAdvanced'
        enabled: true
      }
      {
        category: 'WorkloadManagement'
        enabled: true
      }
    ]
  }
}

// put connection properties into key vault
resource keyVault 'Microsoft.KeyVault/vaults@2021-11-01-preview' existing = {
  name: keyVaultName
}
// application database name
resource sqlAppDatabaseNameSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'sql-app-database-name'
  properties: {
    value: sqlAppDatabaseName
  }
}
// Azure SQL Endpoints (auth is via AAD)
resource AzureSqlEndpointSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'azure-sql-endpoint'
  properties: {
    value: sqlServer.properties.fullyQualifiedDomainName
  }
}

output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output sqlCatalogName string = sqlCatalogName

output sqlServerName string = sqlServer.name
output sqlServerId string = sqlServer.id
output sqlDatabaseName string = sqlDatabase.name
