!#/bin/bash

subscriptionId=$(az account show --query id --output tsv)

# Assign Event Hubs Data Owner role to the current logged-in Azure identity
az role assignment create --role "Azure Event Hubs Data Owner" \
    --assignee-object-id "$(az ad signed-in-user show --query id --output tsv)" \
    --scope "/subscriptions/$subscriptionId/resourceGroups/rg-nimbusbridge-$AZURE_ENV_NAME/providers/Microsoft.EventHub/namespaces/$EVENT_HUBS_NAMESPACE_NAME"
