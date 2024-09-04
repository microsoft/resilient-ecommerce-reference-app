@description('The existing VMSS resource you want to target in this experiment')
param vmssName string

// Reference the existing Virtual Machine resource
resource vmss 'Microsoft.Compute/virtualMachineScaleSets@2024-03-01' existing = {
  name: vmssName
}

// Deploy the Chaos Studio target resource to the Virtual Machine Scale Set
resource chaosTarget 'Microsoft.Chaos/targets@2024-01-01' = {
  name: 'Microsoft-VirtualMachineScaleSet'
  scope: vmss
  properties: {}

  // Define the capability -- in this case, VM Shutdown
  resource chaosCapability 'capabilities' = {
    name: 'Shutdown-2.0'
  }
}

output targetResourceId string = chaosTarget.id
