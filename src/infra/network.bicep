@description('The region where the Network artefacts are deployed in.')
param location string

@description('''
  The suffix of the unique identifier for the resources of the current deployment.
  Used to avoid name collisions and to link resources part of the same deployment together.
''')
param resourceSuffixUID string

@description('''The network configuration for the deployment.
  Format of network config object:
  networkConfig = {
      vnet: {
        cidr: '10.0.0.0/22'
      }
      subnets: {
        AppGatewaySubnet: {
          cidr: '10.0.1.0/24'
        }
        InfraSubnet: {
          cidr: '10.0.2.0/24'
        }
        AKSSubnet: {
          cidr: '10.0.3.0/24'
        }
      }
      dnsResolverPrivateIP: '10.0.0.40'
      loadBalancerPrivateIP: '10.0.3.250'
    }
''')
param networkConfig object

@description('The resource ID of the Log Analytics workspace to which the Network artefacts are connected to.')
param workspaceId string



var infraSubnetName = 'InfraSubnet'
var infraSubnetCidr = networkConfig.subnets[infraSubnetName].cidr


var appGatewaySubnetName = 'AppGatewaySubnet'
var appGatewaySubnetCidr = networkConfig.subnets[appGatewaySubnetName].cidr

var aksSubnetName = 'AKSSubnet'
var aksSubnetCidr = networkConfig.subnets[aksSubnetName].cidr

resource vnet 'Microsoft.Network/virtualNetworks@2022-01-01' = {
  name: 'vnet-${resourceSuffixUID}'
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: [
        networkConfig.vnet.cidr
      ]
    }
    subnets: [
      {
        name: infraSubnetName
        properties: {
          addressPrefix: infraSubnetCidr
          networkSecurityGroup: {
            id: infraSubnetNsg.id
          }
        }
      }
      {
        name: appGatewaySubnetName
        properties: {
          addressPrefix: appGatewaySubnetCidr
          networkSecurityGroup: {
            id: appGatewaySubnetNsg.id
          }
        }
      }
      {
        name: aksSubnetName
        properties: {
          addressPrefix: aksSubnetCidr
          networkSecurityGroup: {
            id: aksSubnetNsg.id
          }
        }
      }
    ]
  }
  
  resource infraSubnet 'subnets' existing = {
    name: infraSubnetName
  }
  resource appGatewaySubnet 'subnets' existing = {
    name: appGatewaySubnetName
  }
  resource aksSubnet 'subnets' existing = {
    name: aksSubnetName
  }
}

// Define NSGs
resource infraSubnetNsg 'Microsoft.Network/networkSecurityGroups@2022-01-01' = {
  name: 'vnet-nsg-${resourceSuffixUID}'
  location: location
  properties: {}
}

resource appGatewaySubnetNsg 'Microsoft.Network/networkSecurityGroups@2022-01-01' = {
  name: 'appgateway-subnet-nsg-${resourceSuffixUID}'
  location: location
  properties: {
    securityRules: [
      {
        name: 'OFP-rule-300'
        properties: {
          description: 'OFP-rule-300 - RequiredPortsForAppGateway'
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '65200-65535'
          sourceAddressPrefix: 'GatewayManager'
          destinationAddressPrefix: '*'
          access: 'Allow'
          priority: 300
          direction: 'Inbound'
        }
      }
      {
        name: 'OFP-rule-301'
        properties: {
          description: 'OFP-rule-301 - Allow FrontDoor to talk to AppGateway'
          protocol: 'TCP'
          sourcePortRange: '*'
          sourceAddressPrefix: 'AzureFrontDoor.Backend'
          destinationAddressPrefix: 'VirtualNetwork'
          access: 'Allow'
          priority: 301
          direction: 'Inbound'
          sourcePortRanges: []
          destinationPortRanges: [
            '443'
            '80'
          ]
          sourceAddressPrefixes: []
          destinationAddressPrefixes: []
        }
      }
    ]
  }
}



resource aksSubnetNsg 'Microsoft.Network/networkSecurityGroups@2022-01-01' = {
  name: 'aks-subnet-nsg-${resourceSuffixUID}'
  location: location
  properties: {
    securityRules: [
      {
        name: 'OFP-rule-302'
        properties: {
          description: 'OFP-rule-302 - Allow port 80 to the subnet from the virtual network'
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '80'
          sourceAddressPrefix: 'VirtualNetwork'
          destinationAddressPrefix: '*'
          access: 'Allow'
          priority: 302
          direction: 'Inbound'
        }
      }
    ]
  }
}

resource diagnosticLogs 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: vnet.name
  scope: vnet
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
        category: 'AllMetrics'
        enabled: true
      }
    ]
  }
}

output vnetName string = vnet.name
output vnetId string = vnet.id
output infraSubnetId string = vnet::infraSubnet.id
output appGatewaySubnetId string = vnet::appGatewaySubnet.id
output aksSubnetId string = vnet::aksSubnet.id
