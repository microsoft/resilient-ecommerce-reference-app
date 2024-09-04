@description('The AKS node cluster configuration for the service API')
param aksConfig object

@description('The network configuration of the service API')
param networkConfig object

@description('The suffix to be used for the name of resources')
param resourceSuffixUID string = ''


var serviceLocation = resourceGroup().location


resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2018-11-30' = {
  name: 'app-identity-${resourceSuffixUID}'
  location: serviceLocation
}

module network './network.bicep' = {
  name: 'vnet'
  params: {
    location: serviceLocation
    resourceSuffixUID: resourceSuffixUID
    networkConfig: networkConfig
    workspaceId: logAnalytics.outputs.workspaceId
  }
}

module logAnalytics 'logAnalytics.bicep' = {
  name: 'logAnalytics'
  params: {
    location: serviceLocation
    resourceSuffixUID: resourceSuffixUID
  }
}

module appInsights 'appInsights.bicep' = {
  name: 'appInsights'
  params: {
    location: serviceLocation
    resourceSuffixUID: resourceSuffixUID
    workspaceId: logAnalytics.outputs.workspaceId
    keyVaultName: keyVault.outputs.keyvaultName
  }
}

module keyVault './keyvault.bicep' = {
  name: 'keyvault'
  params: {
    location: serviceLocation
    resourceSuffixUID: resourceSuffixUID
    vnetId: network.outputs.vnetId
    infraSubnetId: network.outputs.infraSubnetId
    workspaceId: logAnalytics.outputs.workspaceId
  }
}

module appGateway './appGateway.bicep' = {
  name: 'appGateway'
  params: {
    location: serviceLocation
    resourceSuffixUID: resourceSuffixUID
    appGatewaySubnetId: network.outputs.appGatewaySubnetId
    loadBalancerPrivateIp: networkConfig.loadBalancerPrivateIP
    workspaceId: logAnalytics.outputs.workspaceId
  }
}

module redis './redisCache.bicep' =  {
  name: 'redis'
  params: {
    infraSubnetId: network.outputs.infraSubnetId
    location: serviceLocation
    resourceSuffixUID: resourceSuffixUID
    vnetId: network.outputs.vnetId
    workspaceId: logAnalytics.outputs.workspaceId
    appManagedIdentityName: managedIdentity.name
    appManagedIdentityPrincipalId: managedIdentity.properties.principalId
    keyVaultName: keyVault.outputs.keyvaultName
  }
}

module sqlDatabase './azureSqlDatabase.bicep' = {
  name: 'sqlDatabase'
  params: {
    infraSubnetId: network.outputs.infraSubnetId
    location: serviceLocation
    resourceSuffixUID: resourceSuffixUID
    vnetId: network.outputs.vnetId
    workspaceId: logAnalytics.outputs.workspaceId
    managedIdentityClientId: managedIdentity.properties.clientId
    azureSqlZoneRedundant: true
    keyVaultName: keyVault.outputs.keyvaultName
  }
}


module externalLoadBalancerIP 'externalLoadBalancerIP.bicep' = {
  name: 'externalLoadBalancerIP'
  params: {
    resourceSuffixUID: resourceSuffixUID
    location: serviceLocation
    workspaceId: logAnalytics.outputs.workspaceId
  }
}

module aks './aks.bicep' = {
  name: 'aks'
  params: {
    resourceSuffixUID: resourceSuffixUID
    location: serviceLocation
    vnetName: network.outputs.vnetName
    aksSubnetId: network.outputs.aksSubnetId
    workspaceId: logAnalytics.outputs.workspaceId
    outboundPublicIPId: externalLoadBalancerIP.outputs.externalLoadBalancerIPId
    systemNodeCount: aksConfig.systemNodeCount
    minUserNodeCount: aksConfig.minUserNodeCount
    maxUserNodeCount: aksConfig.maxUserNodeCount
    nodeVMSize: aksConfig.nodeVMSize
    nodeOsSKU: aksConfig.nodeOsSKU
    maxUserPodsCount: aksConfig.maxUserPodsCount
  }
}

module acr './acr.bicep' = {
  name: 'acr'
  params: {
    location: serviceLocation
    resourceSuffixUID: resourceSuffixUID
    vnetId: network.outputs.vnetId
    infraSubnetId: network.outputs.infraSubnetId
    workspaceId: logAnalytics.outputs.workspaceId
  }
}

module keyVaultAppRbacGrants './rbacGrantKeyVault.bicep' = {
  name: 'rbacGrantsKeyVault'
  params: {
    keyVaultName: keyVault.outputs.keyvaultName
    appManagedIdentityPrincipalId: managedIdentity.properties.principalId
  }
}

module frontDoor './frontDoor.bicep' = {
  name: 'frontDoor'
  params: {
    resourceSuffixUID: resourceSuffixUID
    appGatewayPublicIp: appGateway.outputs.publicIpAddress
    workspaceId: logAnalytics.outputs.workspaceId
  }
}

module rbacGrantToAcr './rbacGrantToAcr.bicep' = {
  name: 'rbacGrantToAcr'
  params: {
    containerRegistryName: acr.outputs.containerRegistryName
    kubeletIdentityObjectId: aks.outputs.kubeletIdentityObjectId
  }
}


output frontDoorEndpointHostName string = frontDoor.outputs.frontDoorEndpointHostName
output logAnalyticsWorkspaceId string = logAnalytics.outputs.workspaceId
