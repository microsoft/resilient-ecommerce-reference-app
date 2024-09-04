//Helper module to grant a managed identity access to a key vault
@description('KeyVault Name')
param keyVaultName string

@description('The principal ID of the App managed identity to grant access to the key vault')
param appManagedIdentityPrincipalId string

// Get the target key vault
resource keyVault 'Microsoft.KeyVault/vaults@2021-11-01-preview' existing = {
  name: keyVaultName
}

// See: https://learn.microsoft.com/en-us/azure/key-vault/general/rbac-guide?tabs=azure-cli#azure-built-in-roles-for-key-vault-data-plane-operations
var keyVaultSecretsUserRole  = '4633458b-17de-408a-b874-0445c86b69e6'

var secretUserRoleAssignmentName= guid(appManagedIdentityPrincipalId, keyVaultSecretsUserRole, resourceGroup().id)
resource secretReaderRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: secretUserRoleAssignmentName
  scope: keyVault
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRole)
    principalId: appManagedIdentityPrincipalId
  }
}
