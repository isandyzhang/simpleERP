targetScope = 'resourceGroup'

@description('App Service 網站名稱，會變成 <名稱>.azurewebsites.net')
param appName string

var location = resourceGroup().location

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: 'asp-simpleerp'
  location: location
  sku: {
    name: 'F1'
    capacity: 1
  }
  properties: {
    reserved: true
  }
}

resource app 'Microsoft.Web/sites@2023-12-01' = {
  name: appName
  location: location
  kind: 'app,linux'
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    clientAffinityEnabled: false
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: false
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      http20Enabled: true
    }
  }
}

output hostName string = app.properties.defaultHostName
output planSku string = plan.sku.name
output planTier string = plan.sku.tier
