$currentUser=$(az ad signed-in-user show --query id --output tsv)
$subscriptionId=$(az account show --query id --output tsv)

# Assign Event Hubs Data Owner role to the current logged-in Azure identity
az role assignment create --role "Azure Event Hubs Data Owner" --assignee "$currentUser" --scope "/subscriptions/$subscriptionId/resourceGroups/rg-nimbusbridge-$env:AZURE_ENV_NAME/providers/Microsoft.EventHub/namespaces/$env:EVENT_HUBS_NAMESPACE_NAME"

# Assign Storage Blob Data Contributor role to the current logged-in Azure identity
az role assignment create --role "Storage Blob Data Contributor" --assignee "$currentUser" --scope "/subscriptions/$subscriptionId/resourceGroups/rg-nimbusbridge-$env:AZURE_ENV_NAME/providers/Microsoft.Storage/storageAccounts/$env:EVENT_HUBS_CHECKPOINT_STORAGE_ACCOUNT_NAME"
