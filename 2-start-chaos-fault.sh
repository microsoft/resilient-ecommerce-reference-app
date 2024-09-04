#!/usr/bin/env bash

# Exit on errors
set -o errexit -o pipefail -o noclobber

SCRIPT_PATH=$( cd "$(dirname "${BASH_SOURCE[0]}")" ; pwd -P )

if [[ -f $SCRIPT_PATH/.env ]]; then 
    source $SCRIPT_PATH/.env
else
    echo "No .env file found. Please run 1-deploy-service.sh first"
    exit 1
fi

DEFAULT_DOMAIN=$(az rest --method get --url https://graph.microsoft.com/v1.0/domains --query 'value[?isDefault].id' -o tsv)
SUBSCRIPTION_ID=$(az account show --query id -o tsv)

echo "Fetching AKS cluster name from resource group '$RESOURCE_GROUP_NAME'"
VMSS_RESOURCE_GROUP=$(az aks list --resource-group "$RESOURCE_GROUP_NAME" --query "[0].nodeResourceGroup" -o tsv)

echo "Fetching VMSS name from resource group '$VMSS_RESOURCE_GROUP'"
VMSS_NAME=$(az vmss list --resource-group "$VMSS_RESOURCE_GROUP" --query "[?contains(name, 'aks-user')] | [0].name" -o tsv)


az rest --method post --uri https://management.azure.com/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP_NAME/providers/Microsoft.Chaos/experiments/VMSS-ZoneDown-Experiment-$VMSS_NAME/start?api-version=2023-11-01

echo "Waiting for the Chaos experiment to be active"


while true; do
    STATUS=$(az rest --method get --uri https://management.azure.com/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP_NAME/providers/Microsoft.Chaos/experiments/VMSS-ZoneDown-Experiment-$VMSS_NAME/executions?api-version=2023-11-01 --query 'value | [0].properties.status' -o tsv)
    if [[ "$STATUS" == "Running" || "$STATUS" == "Success" || "$STATUS" == "Failed" ]]; then
        echo "Chaos experiment is now $STATUS"
        break
    else
        echo "Chaos experiment status: $STATUS. Checking again in 5 seconds..."
        sleep 5
    fi
done

if [[ "$STATUS" == "Running" ]]; then

    echo "Fetching Azure Front Door endpoint"
    FRONT_DOOR_PROFILE=$(az afd profile list --resource-group $RESOURCE_GROUP_NAME --query "[0].name" -o tsv)
    FRONT_DOOR_HOSTNAME=$(az afd endpoint list --profile-name $FRONT_DOOR_PROFILE --resource-group $RESOURCE_GROUP_NAME --query "[0].hostName" -o tsv)

    echo "Health check for $FRONT_DOOR_HOSTNAME"

    bash $SCRIPT_PATH/tests/run-health-checks.sh --host=$FRONT_DOOR_HOSTNAME
else
    echo "Chaos experiment is not Running. Exiting..."
    exit 1
fi