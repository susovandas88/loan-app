targetScope = 'resourceGroup'

@description('Azure region')
param location string = resourceGroup().location

@description('Short environment name, e.g. dev')
param environmentName string = 'dev'

@description('SQL admin login')
param sqlAdminLogin string

@secure()
param sqlAdminPassword string

@description('SPA origin for CORS')
param spaOrigin string = 'https://localhost:5173'

var name = 'loanapp${environmentName}${uniqueString(resourceGroup().id)}'
var tags = {
  application: 'loan-app'
  environment: environmentName
}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: 'log-${name}'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'appi-${name}'
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

resource vault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: take('kv${uniqueString(resourceGroup().id, environmentName)}', 24)
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enabledForDeployment: false
  }
}

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: take('st${uniqueString(resourceGroup().id, environmentName)}', 24)
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    supportsHttpsTrafficOnly: true
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storage
  name: 'default'
  properties: {
    cors: {
      corsRules: [
        {
          allowedOrigins: [
            spaOrigin
          ]
          allowedMethods: [
            'PUT'
            'OPTIONS'
          ]
          allowedHeaders: [
            '*'
          ]
          exposedHeaders: [
            '*'
          ]
          maxAgeInSeconds: 3600
        }
      ]
    }
  }
}

resource documentsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'loan-documents'
  properties: {
    publicAccess: 'None'
  }
}

resource serviceBus 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: 'sb-${name}'
  location: location
  tags: tags
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
}

resource queue 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  parent: serviceBus
  name: 'loan-applications'
  properties: {
    maxDeliveryCount: 10
    lockDuration: 'PT5M'
    deadLetteringOnMessageExpiration: true
  }
}

resource sql 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: 'sql-${name}'
  location: location
  tags: tags
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

resource sqlDb 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sql
  name: 'loanapp'
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
  }
}

resource sqlAllowAzure 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sql
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

var sqlConnectionString = 'Server=tcp:${sql.properties.fullyQualifiedDomainName},1433;Initial Catalog=loanapp;User ID=${sqlAdminLogin};Password=${sqlAdminPassword};Encrypt=True;TrustServerCertificate=False;'

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: 'plan-${name}'
  location: location
  tags: tags
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

resource apiIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-api-${name}'
  location: location
  tags: tags
}

resource funcIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-func-${name}'
  location: location
  tags: tags
}

resource apiApp 'Microsoft.Web/sites@2023-12-01' = {
  name: 'app-api-${name}'
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${apiIdentity.id}': {}
    }
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|9.0'
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      cors: {
        allowedOrigins: [
          spaOrigin
        ]
        supportCredentials: false
      }
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'ConnectionStrings__LoanDb'
          value: sqlConnectionString
        }
        {
          name: 'Database__Provider'
          value: 'SqlServer'
        }
        {
          name: 'Storage__Mode'
          value: 'Azure'
        }
        {
          name: 'Storage__BlobServiceUri'
          value: 'https://${storage.name}.blob.${environment().suffixes.storage}'
        }
        {
          name: 'Storage__Container'
          value: 'loan-documents'
        }
        {
          name: 'ServiceBus__Mode'
          value: 'Azure'
        }
        {
          name: 'ServiceBus__FullyQualifiedNamespace'
          value: '${serviceBus.name}.servicebus.windows.net'
        }
        {
          name: 'ServiceBus__Queue'
          value: 'loan-applications'
        }
        {
          name: 'Auth__UseDevelopmentAuth'
          value: 'false'
        }
        {
          name: 'FeatureFlags__AiAssistant'
          value: 'false'
        }
        {
          name: 'FeatureFlags__AiVerificationAssist'
          value: 'false'
        }
        {
          name: 'AZURE_CLIENT_ID'
          value: apiIdentity.properties.clientId
        }
      ]
    }
  }
}

resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: 'func-${name}'
  location: location
  tags: tags
  kind: 'functionapp,linux'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${funcIdentity.id}': {}
    }
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|9.0'
      ftpsState: 'Disabled'
      appSettings: [
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'AzureWebJobsStorage__accountName'
          value: storage.name
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'ConnectionStrings__LoanDb'
          value: sqlConnectionString
        }
        {
          name: 'Database__Provider'
          value: 'SqlServer'
        }
        {
          name: 'Storage__Mode'
          value: 'Azure'
        }
        {
          name: 'Storage__BlobServiceUri'
          value: 'https://${storage.name}.blob.${environment().suffixes.storage}'
        }
        {
          name: 'ServiceBus__Mode'
          value: 'Azure'
        }
        {
          name: 'ServiceBusConnection__fullyQualifiedNamespace'
          value: '${serviceBus.name}.servicebus.windows.net'
        }
        {
          name: 'ServiceBusQueue'
          value: 'loan-applications'
        }
        {
          name: 'AZURE_CLIENT_ID'
          value: funcIdentity.properties.clientId
        }
      ]
    }
  }
}

resource staticWeb 'Microsoft.Web/staticSites@2023-12-01' = {
  name: 'stapp-${name}'
  location: location
  tags: tags
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    allowConfigFileUpdates: true
  }
}

var blobDataContributor = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
var blobDelegator = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'db58b8e5-c6ad-4a2a-8343-364e95cb40c3')
var sbDataSender = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39')
var sbDataReceiver = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0')
var kvSecretsUser = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')

resource apiBlobRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storage.id, apiIdentity.id, blobDataContributor)
  scope: storage
  properties: {
    roleDefinitionId: blobDataContributor
    principalId: apiIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource apiBlobDelegator 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storage.id, apiIdentity.id, blobDelegator)
  scope: storage
  properties: {
    roleDefinitionId: blobDelegator
    principalId: apiIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource funcBlobRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storage.id, funcIdentity.id, blobDataContributor)
  scope: storage
  properties: {
    roleDefinitionId: blobDataContributor
    principalId: funcIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource apiSbRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(serviceBus.id, apiIdentity.id, sbDataSender)
  scope: serviceBus
  properties: {
    roleDefinitionId: sbDataSender
    principalId: apiIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource funcSbRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(serviceBus.id, funcIdentity.id, sbDataReceiver)
  scope: serviceBus
  properties: {
    roleDefinitionId: sbDataReceiver
    principalId: funcIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource apiKvRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(vault.id, apiIdentity.id, kvSecretsUser)
  scope: vault
  properties: {
    roleDefinitionId: kvSecretsUser
    principalId: apiIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource funcKvRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(vault.id, funcIdentity.id, kvSecretsUser)
  scope: vault
  properties: {
    roleDefinitionId: kvSecretsUser
    principalId: funcIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

output apiHostName string = apiApp.properties.defaultHostName
output functionHostName string = functionApp.properties.defaultHostName
output staticWebHostName string = staticWeb.properties.defaultHostname
output sqlServerName string = sql.name
output keyVaultName string = vault.name
output storageAccountName string = storage.name
output serviceBusNamespace string = serviceBus.name
output appInsightsConnectionString string = appInsights.properties.ConnectionString
