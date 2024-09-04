@description('The region where the AKS cluster is deployed in.')
param location string
@description('''
  The suffix of the unique identifier for the resources of the current deployment.
  Used to avoid name collisions and to link resources part of the same deployment together.
''')
param resourceSuffixUID string

@description('The name of the Virtual Network under which the subnet where the AKS cluster is deployed in.')
param vnetName string
@description('The resource ID of the subnet where the AKS cluster is deployed in.')
param aksSubnetId string
@description('The resource ID of the Log Analytics workspace to which the AKS cluster is connected to.')
param workspaceId string
@description('The resource ID of the public IP address used for load balancer of the AKS cluster.')
param outboundPublicIPId string

@description('The fixed number of system node pool nodes. This value is fixed as no auto-scaling is configured by default.')
param systemNodeCount int
@description('The minimum number of user node pool nodes.')
param minUserNodeCount int
@description('The maximum number of user node pool nodes.')
param maxUserNodeCount int
@description('The maximum number of PODs per user node')
param maxUserPodsCount int
@description('The VM size (SKU) of the nodes in the AKS cluster.')
param nodeVMSize string
@description('The OS SKU of the nodes in the AKS cluster.')
@allowed(['Ubuntu', 'AKS', 'AKS-Preview'])
param nodeOsSKU string


@description('The Kubernetes version that the AKS cluster runs. Check for compatibility with helm and nginx-controller before modifying.')
var kubeVersion = '1.29.4'

@description('''
  Allows for the management of networks, without access to them. This role does not grant permission to deploy or manage Virtual Machines.
  See: https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles/networking#network-contributor
''')
var networkContributorRoleDefinitionId = resourceId(
  'Microsoft.Authorization/roleDefinitions',
  '4d97b98b-1d4f-4787-a291-c67834d212e7'
)


// Create the Azure kubernetes service cluster
resource aks 'Microsoft.ContainerService/managedClusters@2024-02-01' = {
  name: 'aks-${resourceSuffixUID}'
  location: location
  sku: {
    name: 'Base'
    tier: 'Standard'
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    kubernetesVersion: kubeVersion
    enableRBAC: true
    dnsPrefix: 'refapp'
    disableLocalAccounts: true
    aadProfile: {
      enableAzureRBAC: true
      managed: true
    }
    agentPoolProfiles: [
      {
        name: 'system'
        count: systemNodeCount
        mode: 'System'
        vmSize: nodeVMSize
        type: 'VirtualMachineScaleSets'
        osType: 'Linux'
        osSKU: nodeOsSKU
        enableAutoScaling: false
        vnetSubnetID: aksSubnetId
        availabilityZones: ['1', '2', '3']
        tags: {
          azsecpack: 'nonprod'
          AzSecPackAutoConfigReady: 'true'
          'platformsettings.host_environment.service.platform_optedin_for_rootcerts': 'true'
        }
      }
      {
        name: 'user'
        mode: 'User'
        vmSize: nodeVMSize
        type: 'VirtualMachineScaleSets'
        osType: 'Linux'
        osSKU: nodeOsSKU
        enableAutoScaling: true
        count: minUserNodeCount
        minCount: minUserNodeCount
        maxCount: maxUserNodeCount
        maxPods: maxUserPodsCount
        vnetSubnetID: aksSubnetId
        availabilityZones: ['1', '2', '3']
        tags: {
          azsecpack: 'nonprod'
          AzSecPackAutoConfigReady: 'true'
          'platformsettings.host_environment.service.platform_optedin_for_rootcerts': 'true'
        }
      }
    ]
    oidcIssuerProfile: {
      enabled: true
    }
    // https://learn.microsoft.com/en-us/azure/aks/auto-upgrade-node-os-image
    autoUpgradeProfile: {
      nodeOSUpgradeChannel: 'NodeImage'
      upgradeChannel: 'stable'
    }
    securityProfile: {
      workloadIdentity: {
        enabled: true
      }
    }
    apiServerAccessProfile: {
      enablePrivateCluster: true
      enablePrivateClusterPublicFQDN: false
    }
    servicePrincipalProfile: {
      clientId: 'msi'
    }
    networkProfile: {
      networkPlugin: 'azure'
      loadBalancerSku: 'standard'
      serviceCidr: '10.1.0.0/16'
      dnsServiceIP: '10.1.0.10'
      loadBalancerProfile: {
        outboundIPs: {
          publicIPs: [
            {
              id: outboundPublicIPId
            }
          ]
        }
      }
    }
    addonProfiles: {
      omsagent: {
        config: {
          logAnalyticsWorkspaceResourceID: workspaceId
        }
        enabled: true
      }
      azureKeyvaultSecretsProvider: {
        enabled: true
        config: {
          enableSecretRotation: 'false'
          rotationPollInterval: '2m'
        }
      }
    }
  }
}

// See: https://learn.microsoft.com/en-us/azure/azure-monitor/reference/supported-logs/microsoft-containerservice-managedclusters-logs
resource diagnosticLogs 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: aks.name
  scope: aks
  properties: {
    workspaceId: workspaceId
    logAnalyticsDestinationType: 'Dedicated'
    logs: [
      {
        category: 'kube-apiserver'
        enabled: true
      }
      {
        category: 'kube-audit'
        enabled: true
      }
      {
        category: 'kube-audit-admin'
        enabled: true
      }
      {
        category: 'kube-controller-manager'
        enabled: true
      }
      {
        category: 'kube-scheduler'
        enabled: true
      }
      {
        category: 'cluster-autoscaler'
        enabled: true
      }
      {
        category: 'cloud-controller-manager'
        enabled: true
      }
      {
        category: 'guard'
        enabled: true
      }
      {
        category: 'csi-azuredisk-controller'
        enabled: true
      }
      {
        category: 'csi-azurefile-controller'
        enabled: true
      }
      {
        category: 'csi-snapshot-controller'
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

// Assign the 'Network Contributor' role to the AKS service principal on the VNet
// it's deployed in, so that AKS can create the internal load balancer with a
// static public IP from within the AKS subnet.
// See: https://learn.microsoft.com/en-us/azure/aks/configure-kubenet#prerequisites
resource vnet 'Microsoft.Network/virtualNetworks@2023-11-01' existing = {
  name: vnetName
}

resource networkContributorRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, vnet.id, networkContributorRoleDefinitionId)
  scope: vnet
  properties: {
    description: 'Lets AKS manage the VNet, but not access it'
    principalId: aks.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: networkContributorRoleDefinitionId
  }
}

output aksid string = aks.id
output aksnodesrg string = aks.properties.nodeResourceGroup
output aksPrivateFqdn string = aks.properties.privateFQDN
output kubeletIdentityObjectId string = aks.properties.identityProfile.kubeletidentity.objectId
