@description('The existing VMSS resource you want to target in this experiment')
param vmssName string

@description('The existing Chaos Experiment resource')
param chaosExperimentPrincipalId string

// Reference the existing Virtual Machine resource
resource vmss 'Microsoft.Compute/virtualMachineScaleSets@2024-03-01' existing = {
  name: vmssName
}


// Define the role definition for the Chaos experiment
resource chaosRoleDefinition 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: vmss
  // In this case, Virtual Machine Contributor role -- see https://learn.microsoft.com/azure/role-based-access-control/built-in-roles 
  name: '9980e02c-c2be-4d73-94e8-173b1dc7cf3c'
}

// Define the role assignment for the Chaos experiment
resource chaosRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(vmss.id, chaosExperimentPrincipalId, chaosRoleDefinition.id)
  scope: vmss
  properties: {
    roleDefinitionId: chaosRoleDefinition.id
    principalId: chaosExperimentPrincipalId
    principalType: 'ServicePrincipal'
  }
}
