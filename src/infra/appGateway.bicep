@description('The region where the AKS cluster is deployed in.')
param location string 

@description('''
  The suffix of the unique identifier for the resources of the current deployment.
  Used to avoid name collisions and to link resources part of the same deployment together.
''')
param resourceSuffixUID string

@description('The Id of the Subnet where the App Gateway is deployed in.')
param appGatewaySubnetId string

@description('The private IP address of the Load Balancer to which the App Gateway routes traffic.')
param loadBalancerPrivateIp string

@description('The resource ID of the Log Analytics workspace to which the App Gateway is connected to.')
param workspaceId string

@description('The minimum capacity of the App Gateway.')
param minAppGatewayCapacity int = 3

@description('The maximum capacity of the App Gateway.')
param maxAppGatewayCapacity int = 125

var appGatewayName = 'appgw-${resourceSuffixUID}' 


resource publicIPAddress 'Microsoft.Network/publicIPAddresses@2021-05-01' = {
  name: 'appgw-pip-${resourceSuffixUID}'
  location: location
  zones: ['1','2','3']
  sku: {
    name: 'Standard'
  }
  properties: {
    publicIPAddressVersion: 'IPv4'
    publicIPAllocationMethod: 'Static'
    idleTimeoutInMinutes: 4
  }
}

resource applicationGateWay 'Microsoft.Network/applicationGateways@2021-05-01' = {
  name: appGatewayName
  location: location
  zones: ['1','2','3']
  properties: {
    sku: {
      name: 'Standard_v2'
      tier: 'Standard_v2'
    }
    autoscaleConfiguration: {
      maxCapacity: maxAppGatewayCapacity
      minCapacity: minAppGatewayCapacity
    }
    gatewayIPConfigurations: [
      {
        name: 'appGatewayIpConfig'
        properties: {
          subnet: {
            id: appGatewaySubnetId
          }
        }
      }
    ]
    frontendIPConfigurations: [
      {
        name: 'appGwPublicFrontendIp'
        properties: {
          privateIPAllocationMethod: 'Dynamic'
          publicIPAddress: {
            id: publicIPAddress.id
          }
        }
      }
    ]
    frontendPorts: [
      {
        name: 'port_80'
        properties: {
          port: 80
        }
      }
    ]
    backendAddressPools: [
      {
        name: 'myBackendPool'
        properties: {
          backendAddresses: [
            {
              ipAddress: loadBalancerPrivateIp
            }
          ]
        }
      }
    ]
    backendHttpSettingsCollection: [
      {
        name: 'webAppSettings'
        properties: {
          port: 80
          protocol: 'Http'
          cookieBasedAffinity: 'Disabled'
          pickHostNameFromBackendAddress: false
          requestTimeout: 20
          probe: {
            id: resourceId('Microsoft.Network/applicationGateways/probes', appGatewayName, 'app-health')
          }
        }
      }
    ]
    httpListeners: [
      {
        name: 'httpListener'
        properties: {
          frontendIPConfiguration: {
            id: resourceId('Microsoft.Network/applicationGateways/frontendIPConfigurations', appGatewayName, 'appGwPublicFrontendIp')
          }
          frontendPort: {
            id: resourceId('Microsoft.Network/applicationGateways/frontendPorts', appGatewayName, 'port_80')
          }
          protocol: 'Http'
          requireServerNameIndication: false
        }
      }
    ]
    urlPathMaps:[
      {
        name: 'httpRule'
        properties: {
          defaultBackendAddressPool :{
            id: resourceId('Microsoft.Network/applicationGateways/backendAddressPools', appGatewayName, 'myBackendPool')
          }
          defaultBackendHttpSettings: {
            id: resourceId('Microsoft.Network/applicationGateways/backendHttpSettingsCollection', appGatewayName, 'webAppSettings')
          }
          pathRules: [
            {
              name: 'cart'
              properties: {
                paths: [
                  '/api/*'
                ]
                backendAddressPool: {
                  id: resourceId('Microsoft.Network/applicationGateways/backendAddressPools', appGatewayName, 'myBackendPool')
                }
                backendHttpSettings: {
                  id: resourceId('Microsoft.Network/applicationGateways/backendHttpSettingsCollection', appGatewayName, 'webAppSettings')
                }
              }
            }
          ]
        }
      }
    ]
    requestRoutingRules: [
      {
        name: 'httpRule'
        properties: {
          ruleType: 'PathBasedRouting'
          priority: 1
          httpListener: {
            id: resourceId('Microsoft.Network/applicationGateways/httpListeners', appGatewayName, 'httpListener')
          }
          urlPathMap: {
            id: resourceId('Microsoft.Network/applicationGateways/urlPathMaps', appGatewayName, 'httpRule')
          }
        }
      }
    ]
    probes: [
      {
        name: 'app-health'
        properties: {
          protocol: 'Http'
          host: '127.0.0.1'
          path: '/api/live'
          interval: 30
          timeout: 30
          unhealthyThreshold: 3
          pickHostNameFromBackendHttpSettings: false
          minServers: 0
          match: {}
        }
      }
    ]
    enableHttp2: false
  }
}

resource diagnosticLogs 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: applicationGateWay.name
  scope:applicationGateWay
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

resource diagnosticLogsPublicIP 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: publicIPAddress.name
  scope:publicIPAddress
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

output publicIpAddress string = publicIPAddress.properties.ipAddress
