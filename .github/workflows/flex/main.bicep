param resourceGroupName string = resourceGroup().name
param resourceGroupLocation string = resourceGroup().location

module storageModule 'storage.bicep' = {
  name: 'orleansStorageModule'
  params: {
    name: replace(resourceGroupName, '-resourcegroup', 'storage')
    resourceGroupLocation: resourceGroupLocation
  }
}

module logsModule 'logs-and-insights.bicep' = {
  name: 'orleansLogModule'
  params: {
    operationalInsightsName: replace(resourceGroupName, 'resourcegroup', 'logs')
    appInsightsName: replace(resourceGroupName, 'resourcegroup', 'insights')
    resourceGroupLocation: resourceGroupLocation
  }
}

resource vnet 'Microsoft.Network/virtualNetworks@2021-05-01' = {
  name: 'orleans-vnet'
  location: resourceGroupLocation
  properties: {
    addressSpace: {
      addressPrefixes: [
        '172.17.0.0/16'
      ]
    }
    subnets: [
      {
        name: 'default'
        properties: {
          addressPrefix: '172.17.0.0/24'
          delegations: [
            {
              name: 'delegation'
              properties: {
                serviceName: 'Microsoft.Web/serverFarms'
              }
            }
          ]
        }
      }
    ]
  }
}

module siloModule 'app-service.bicep' = {
  name: 'orleansSiloModule'
  params: {
    appName: replace(resourceGroupName, '-resourcegroup', '-app-silo')
    resourceGroupName: resourceGroupName
    resourceGroupLocation: resourceGroupLocation
    vnetSubnetId: vnet.properties.subnets[0].id
    appInsightsConnectionString: logsModule.outputs.appInsightsConnectionString
    appInsightsInstrumentationKey: logsModule.outputs.appInsightsInstrumentationKey
    storageConnectionString: storageModule.outputs.connectionString
  }
}
