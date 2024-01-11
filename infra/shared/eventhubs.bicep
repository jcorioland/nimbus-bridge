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

resource commandsEventHub 'Microsoft.EventHub/namespaces/eventhubs@2021-11-01' = {
  name: 'commands'
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

resource commandsEventHubListenConnectionString 'Microsoft.EventHub/namespaces/eventhubs/authorizationRules@2021-11-01' = {
  parent: commandsEventHub
  name: 'ListenCommandsHub'
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
    name: 'Premium_LRS'
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

var commandsEventHubListenConnectionStringValue = listKeys(commandsEventHubListenConnectionString.id, commandsEventHubListenConnectionString.apiVersion).primaryConnectionString
var responsesEventHubSendConnectionStringValue = listKeys(responsesEventHubSendConnectionString.id, responsesEventHubSendConnectionString.apiVersion).primaryConnectionString

resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' existing = {
  name: keyVaultName
}

resource commandsListenConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: keyVault
  name: 'CommandsEventHubListenConnectionString'
  properties: {
    value: commandsEventHubListenConnectionStringValue
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
