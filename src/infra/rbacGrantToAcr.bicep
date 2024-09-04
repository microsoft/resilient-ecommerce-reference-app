param containerRegistryName string
param kubeletIdentityObjectId string

var acrPullRoleId = '7f951dda-4ed3-4680-a7ca-43fe172d538d'

resource acrPullRole 'Microsoft.Authorization/roleDefinitions@2018-01-01-preview' existing = {
  scope: subscription()
  name: acrPullRoleId
}

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2021-09-01' existing = {
  name: containerRegistryName
}

resource assignAcrPullToAks 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = {
  name: guid(subscription().id, resourceGroup().id, containerRegistryName, 'AssignAcrPullToAks',kubeletIdentityObjectId)
  scope: containerRegistry
  properties: {
    description: 'Assign AcrPull role to AKS'
    principalId: kubeletIdentityObjectId
    principalType: 'ServicePrincipal'
    roleDefinitionId: acrPullRole.id 
  }
}
