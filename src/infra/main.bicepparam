using 'main.bicep'

param aksConfig = {
  systemNodeCount: 3
  minUserNodeCount: 3
  maxUserNodeCount: 6
  maxUserPodsCount: 10
  nodeVMSize: 'Standard_D2s_v3'
  nodeOsSKU: 'Ubuntu'
}

param networkConfig = {
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
