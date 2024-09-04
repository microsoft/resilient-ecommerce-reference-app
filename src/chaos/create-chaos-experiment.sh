#!/usr/bin/env bash

# Exit on errors
set -o errexit -o pipefail -o noclobber

[[ -f .env ]] && source .env

# Parse command line arguments
for ARG in "$@"; do
  case $ARG in
    -rg=*|--resource-group=*)
      RESOURCE_GROUP_NAME="${ARG#*=}"
      shift
      ;;
    -*|--*)
      echo "Unknown argument '$ARG'" >&2
      exit 1
      ;;
    *)
      ;;
  esac
done

# Validate command line arguments
if [ -z $RESOURCE_GROUP_NAME ]; then
  echo "No resource group provided and .env file is empty. Please provide a resource group name as command line argument. E.g. '$0 -rg=my-rg-name'" >&2
  exit 1
fi

echo "Fetching AKS cluster name from resource group '$RESOURCE_GROUP_NAME'"
VMSS_RESOURCE_GROUP=$(az aks list --resource-group "$RESOURCE_GROUP_NAME" --query "[0].nodeResourceGroup" -o tsv)

echo "Fetching VMSS name from resource group '$VMSS_RESOURCE_GROUP'"
VMSS_NAME=$(az vmss list --resource-group "$VMSS_RESOURCE_GROUP" --query "[?contains(name, 'aks-user')] | [0].name" -o tsv)

SCRIPT_PATH=$( cd "$(dirname "${BASH_SOURCE[0]}")" ; pwd -P )


echo "Deploying Chaos Resources for VMSS '$VMSS_NAME'"
az deployment group create \
  --resource-group $RESOURCE_GROUP_NAME \
  --name "Deploy-Chaos-Resources" \
  --template-file $SCRIPT_PATH/chaosExperimentMain.bicep \
  --parameters vmssName="$VMSS_NAME" \
  --parameters vmssResourceGroup="$VMSS_RESOURCE_GROUP" \
  --verbose

DEFAULT_DOMAIN=$(az rest --method get --url https://graph.microsoft.com/v1.0/domains --query 'value[?isDefault].id' -o tsv)

SUBSCRIPTION_ID=$(az account show --query id -o tsv)

echo "You can use the following link to access the Chaos Experiment in the Azure Portal and start it:"
echo "https://portal.azure.com/#@$DEFAULT_DOMAIN/resource/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP_NAME/providers/Microsoft.Chaos/experiments/VMSS-ZoneDown-Experiment-$VMSS_NAME/experimentOverview"

echo "Or you can use the following command to start the Chaos Experiment:"
echo "./start-chaos-fault.sh"

