param namespaceName string
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
