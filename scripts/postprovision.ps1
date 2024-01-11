# Assign Event Hubs Data Owner RBAC role to the current logged-in Azure CLI user
$roleName="Azure Event Hubs Data Owner"
$currentUser=$(az ad signed-in-user show --query id --output tsv)
$subscriptionId=$(az account show --query id --output tsv)

az role assignment create --role "$roleName" --assignee "$currentUser" --scope "/subscriptions/$subscriptionId/resourceGroups/rg-nimbusbridge-$env:AZURE_ENV_NAME/providers/Microsoft.EventHub/namespaces/$env:EVENT_HUBS_NAMESPACE_NAME"
