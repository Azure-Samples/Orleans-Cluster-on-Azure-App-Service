param appName string
param location string
param vnetSubnetId string
param stagingSubnetId string
param appInsightsInstrumentationKey string
param appInsightsConnectionString string
param storageConnectionString string
param tenantId string
param authenticationClientId string
@secure()
param authenticationClientSecret string

resource appServicePlan 'Microsoft.Web/serverfarms@2021-03-01' = {
  name: '${appName}-plan'
  location: location
  kind: 'app'
  sku: {
    name: 'S1'
    capacity: 1
  }
}

resource appService 'Microsoft.Web/sites@2021-03-01' = {
  name: appName
  location: location
  kind: 'app'
  properties: {
    serverFarmId: appServicePlan.id
    virtualNetworkSubnetId: vnetSubnetId
    httpsOnly: true
    siteConfig: {
      vnetPrivatePortsCount: 2
      webSocketsEnabled: true
      netFrameworkVersion: 'v6.0'
      appSettings: [
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsightsInstrumentationKey
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'ORLEANS_AZURE_STORAGE_CONNECTION_STRING'
          value: storageConnectionString
        }
        {
          name: 'ORLEANS_CLUSTER_ID'
          value: 'Default'
        }
        {
          name: 'MICROSOFT_PROVIDER_AUTHENTICATION_SECRET'
          value: authenticationClientSecret
        }
      ]
      alwaysOn: true
    }
  }
}

resource stagingSlot 'Microsoft.Web/sites/slots@2022-03-01' = {
  parent: appService
  name: '${appName}stg'
  location: location
  properties: {
    serverFarmId: appServicePlan.id
    virtualNetworkSubnetId: stagingSubnetId
    siteConfig: {
      http20Enabled: true
      vnetPrivatePortsCount: 2
      webSocketsEnabled: true
      netFrameworkVersion: 'v7.0'
      appSettings: [
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsightsInstrumentationKey
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'ORLEANS_AZURE_STORAGE_CONNECTION_STRING'
          value: storageConnectionString
        }
        {
          name: 'ORLEANS_CLUSTER_ID'
          value: 'Staging'
        }
        {
          name: 'MICROSOFT_PROVIDER_AUTHENTICATION_SECRET'
          value: authenticationClientSecret
        }
      ]
      alwaysOn: true
    }
  }
}

resource slotConfig 'Microsoft.Web/sites/config@2021-03-01' = {
  name: 'slotConfigNames'
  parent: appService
  properties: {
    appSettingNames: [
      'ORLEANS_CLUSTER_ID'
      'MICROSOFT_PROVIDER_AUTHENTICATION_SECRET'
    ]
  }
}

resource appServiceConfig 'Microsoft.Web/sites/config@2021-03-01' = {
  parent: appService
  name: 'metadata'
  properties: {
    CURRENT_STACK: 'dotnet'
  }
}

resource appServiceAuthentication 'Microsoft.Web/sites/config@2022-09-01' = {
  parent: appService
  name: 'authsettingsV2'
  properties: {
    globalValidation: {
      requireAuthentication: false
      unauthenticatedClientAction: 'AllowAnonymous'
    }
    httpSettings: {
      requireHttps: true
    }
    identityProviders: {
      azureActiveDirectory: {
        enabled: true
        registration: {
          clientId: authenticationClientId
          clientSecretSettingName: 'MICROSOFT_PROVIDER_AUTHENTICATION_SECRET'
          openIdIssuer: '${environment().authentication.loginEndpoint}${tenantId}/v2.0'
        }
        validation: {
          allowedAudiences: [
            authenticationClientId
          ]
        }
      }
    }
    login: {
      tokenStore: {
        enabled: true
      }
    }
    platform: {
      enabled: true
      runtimeVersion: '~1'
    }
  }
}

resource stagingSlotAuthentication 'Microsoft.Web/sites/slots/config@2022-09-01' = {
  parent: stagingSlot
  name: 'authsettingsV2'
  properties: appServiceAuthentication.properties
}
