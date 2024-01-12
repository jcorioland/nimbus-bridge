!#/bin/bash

subscriptionId=$(az account show --query id --output tsv)

# Assign Event Hubs Data Owner role to the current logged-in Azure identity
az role assignment create --role "Azure Event Hubs Data Owner" \
    --assignee-object-id "$(az ad signed-in-user show --query id --output tsv)" \
    --scope "/subscriptions/$subscriptionId/resourceGroups/rg-nimbusbridge-$AZURE_ENV_NAME/providers/Microsoft.EventHub/namespaces/$EVENT_HUBS_NAMESPACE_NAME"

# Assign Storage Blob Data Contributor role to the current logged-in Azure identity
az role assignment create --role "Storage Blob Data Contributor" \
    --assignee-object-id "$(az ad signed-in-user show --query id --output tsv)" \
    --scope "/subscriptions/$subscriptionId/resourceGroups/rg-nimbusbridge-$AZURE_ENV_NAME/providers/Microsoft.Storage/storageAccounts/$EVENT_HUBS_CHECKPOINT_STORAGE_ACCOUNT_NAME"
