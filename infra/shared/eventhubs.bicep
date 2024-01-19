param namespaceName string
param checkpointBlobStoreName string
param identityPrincipalId string
param location string = resourceGroup().location
param tags object = {}
param keyVaultName string

resource eventHubsNamespace 'Microsoft.EventHub/namespaces@2021-11-01' = {
  name: namespaceName
  location: location
  tags: tags
  sku: {
    name: 'Standard'
    tier: 'Standard'
    capacity: 1
  }
}

resource northwindCommandsEventHub 'Microsoft.EventHub/namespaces/eventhubs@2021-11-01' = {
  name: 'northwind-commands'
  parent: eventHubsNamespace
  properties: {
    messageRetentionInDays: 1
    partitionCount: 4
  }
}

resource contosoCommandsEventHub 'Microsoft.EventHub/namespaces/eventhubs@2021-11-01' = {
  name: 'contoso-commands'
  parent: eventHubsNamespace
  properties: {
    messageRetentionInDays: 1
    partitionCount: 4
  }
}

resource responsesEventHub 'Microsoft.EventHub/namespaces/eventhubs@2021-11-01' = {
  name: 'responses'
  parent: eventHubsNamespace
  properties: {
    messageRetentionInDays: 1
    partitionCount: 4
  }
}

resource eventHubsDataOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: eventHubsNamespace
  name: guid(subscription().id, resourceGroup().id, identityPrincipalId, 'eventHubsDataOwner')
  properties: {
    roleDefinitionId:  subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions', 'f526a384-b230-433a-b45c-95f59c4a2dec')
    principalType: 'ServicePrincipal'
    principalId: identityPrincipalId
  }
}

resource contosoCommandsEventHubListenConnectionString 'Microsoft.EventHub/namespaces/eventhubs/authorizationRules@2021-11-01' = {
  parent: contosoCommandsEventHub
  name: 'ContosoListenCommandsHub'
  properties: {
    rights: [
      'Listen'
    ]
  }
}

resource northwindCommandsEventHubListenConnectionString 'Microsoft.EventHub/namespaces/eventhubs/authorizationRules@2021-11-01' = {
  parent: northwindCommandsEventHub
  name: 'NorthwindListenCommandsHub'
  properties: {
    rights: [
      'Listen'
    ]
  }
}

resource responsesEventHubSendConnectionString 'Microsoft.EventHub/namespaces/eventhubs/authorizationRules@2021-11-01' = {
  parent: responsesEventHub
  name: 'SendResponsesHub'
  properties: {
    rights: [
      'Send'
    ]
  }
}

resource checkpointBlobStore 'Microsoft.Storage/storageAccounts@2021-02-01' = {
  name: checkpointBlobStoreName
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
}

resource storageBlobDataContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: checkpointBlobStore
  name: guid(subscription().id, resourceGroup().id, identityPrincipalId, 'storageBlobDataContributor')
  properties: {
    roleDefinitionId:  subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
    principalType: 'ServicePrincipal'
    principalId: identityPrincipalId
  }
}

resource checkpointBlobService 'Microsoft.Storage/storageAccounts/blobServices@2021-02-01' = {
  parent: checkpointBlobStore
  name: 'default'
}

resource checkpointBlobContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2021-02-01' = {
  parent: checkpointBlobService
  name: 'checkpoint'
}

var contosoCommandsEventHubListenConnectionStringValue = listKeys(contosoCommandsEventHubListenConnectionString.id, contosoCommandsEventHubListenConnectionString.apiVersion).primaryConnectionString
var northwindCommandsEventHubListenConnectionStringValue = listKeys(northwindCommandsEventHubListenConnectionString.id, northwindCommandsEventHubListenConnectionString.apiVersion).primaryConnectionString
var responsesEventHubSendConnectionStringValue = listKeys(responsesEventHubSendConnectionString.id, responsesEventHubSendConnectionString.apiVersion).primaryConnectionString

resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' existing = {
  name: keyVaultName
}

resource contosoCommandsListenConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: keyVault
  name: 'ContosoCommandsEventHubListenConnectionString'
  properties: {
    value: contosoCommandsEventHubListenConnectionStringValue
  }
}

resource northwindCommandsListenConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: keyVault
  name: 'NorthwindCommandsEventHubListenConnectionString'
  properties: {
    value: northwindCommandsEventHubListenConnectionStringValue
  }
}

resource responsesSendConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: keyVault
  name: 'ResponsesEventHubSendConnectionString'
  properties: {
    value: responsesEventHubSendConnectionStringValue
  }
}

resource eventHubsNamespaceFqdnSecret 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: keyVault
  name: 'EventHubsNamespaceFqdn'
  properties: {
    value: '${namespaceName}.servicebus.windows.net'
  }
}

resource checkpointBlobContainerUrl 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: keyVault
  name: 'CheckpointBlobContainerUrl'
  properties: {
    value: 'https://${checkpointBlobStoreName}.blob.core.windows.net/checkpoint'
  }
}

output namespaceName string = eventHubsNamespace.name
output checkpointStorageAccountName string = checkpointBlobStore.name
