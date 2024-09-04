@description('The existing VMSS resource you want to target in this experiment')
param vmssName string

@description('VMSS resource group name')
param vmssResourceGroup string

@description('Desired name for your Chaos Experiment')
param experimentName string = 'VMSS-ZoneDown-Experiment-${vmssName}'


// Define Chaos Studio experiment steps for a VMSS Zone Down Experiment
var experimentSteps = [
  {
    name: 'Step1'
    branches: [
      {
        name: 'Branch1'
        actions: [
          {
            name: 'urn:csci:microsoft:virtualMachineScaleSet:shutdown/2.0'
            type: 'continuous'
            duration: 'PT5M'
            parameters: [
              {
                key: 'abruptShutdown'
                value: 'true'
              }
            ]
            selectorId: 'Selector1'
          }
        ]
      }
    ]
  }
]

// Create a Chaos Target. This is a child of the VMSS resource and needs to be deployed to the same resource group
module chaosTarget './chaosTarget.bicep' = {
  name: 'chaosTarget'
  scope: resourceGroup(vmssResourceGroup)
  params: {
    vmssName: vmssName
  }
}


// Deploy the Chaos Studio experiment resource
resource chaosExperiment 'Microsoft.Chaos/experiments@2024-01-01' = {
  name: experimentName
  location: 'westus'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    selectors: [
      {
        id: 'Selector1'
        type: 'List'
        targets: [
          {
            id: chaosTarget.outputs.targetResourceId
            type: 'ChaosTarget'
          }
        ]
        filter: {
          type:'Simple'
          parameters: {
            zones:['1']
          }
        }
      }
    ]
    steps: experimentSteps
  }
}

// Assign RBAC roles to the Chaos Experiment to allow it to target the VMSS
module vmssRoleAssignment './vmssRoleAssignment.bicep' = {
  name: 'vmssRoleAssignment'
  scope: resourceGroup(vmssResourceGroup)
  params: {
    vmssName: vmssName
    chaosExperimentPrincipalId: chaosExperiment.identity.principalId
  }
}
