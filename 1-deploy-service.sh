#!/usr/bin/env bash

# Exit on errors
set -o errexit -o pipefail -o noclobber
SCRIPT_PATH=$( cd "$(dirname "${BASH_SOURCE[0]}")" ; pwd -P )

__usage="
To deploy a new environment, run this script with the location where you want to deploy the service to. 
A new resource group will be created in this location.
Usage: $0 [-sl=service-location]
    -sl, --service-location       Location to deploy the service to. A new resource group will be created in this location
    -h, --help                    Show this help message

Examples:
    $0 -sl=eastus

To apply changes to existing environment, run the script without any argument and it will use the existing resource group from .env file.
Examples:
    $0
"

# Parse command line arguments
for ARG in "$@"; do
  case $ARG in
    -sl=*|--service-location=*)
      SERVICE_LOCATION="${ARG#*=}"
      shift
      ;;
    -h|--help)
      echo "$__usage"
      exit 0
      ;;
    -*|--*)
      echo "Unknown argument '$ARG'" >&2
      exit 1
      ;;
    *)
      ;;
  esac
done

# Validate input, set defaults, and create resource group if needed
if [ -z $SERVICE_LOCATION ]; then
  if [[ -f $SCRIPT_PATH/.env ]]; then 
    source $SCRIPT_PATH/.env
    echo "Using existing resource group: '$RESOURCE_GROUP_NAME' from .env file"
  else
    echo "No service location provided and there is not .env file. Please provide a service location. Sample usage '$0 -sl=eastus'. Run with '--help' for more information" >&2
    exit 1
  fi
else
  echo "Deploying to location: $SERVICE_LOCATION"
  GUID=$(uuidgen)
  RESOURCE_GROUP_NAME="refapp-public-${GUID:0:6}"

  echo "Deploying to new resource group: '$RESOURCE_GROUP_NAME'"
  az group create --name $RESOURCE_GROUP_NAME --location $SERVICE_LOCATION
  rm -rf $SCRIPT_PATH/.env
  echo "Saving resource group name to .env file"
  echo "RESOURCE_GROUP_NAME=$RESOURCE_GROUP_NAME" > $SCRIPT_PATH/.env 
fi


echo "Deploy Az Ref App Infrastructure to $RESOURCE_GROUP_NAME"
bash $SCRIPT_PATH/src/infra/deploy.sh -rg=$RESOURCE_GROUP_NAME


echo "Fetching ACR URI and Id"

ACR_URI=$(az acr list --resource-group $RESOURCE_GROUP_NAME --query "[0].loginServer" -o tsv)
ACR_ID=$(az acr list --resource-group $RESOURCE_GROUP_NAME --query "[0].id" -o tsv)
LOGGED_IN_USER=$(az ad signed-in-user show --query "id" -o tsv)
echo "ACR URI: $ACR_URI"


echo "Adding Container Registry Repository Contributor role to user"
az role assignment create --assignee "$LOGGED_IN_USER" \
        --role "Container Registry Repository Contributor" \
        --scope "$ACR_ID"


echo "Adding AKS Azure Kubernetes Service RBAC Cluster Admin to user"
AKS_ID=$(az aks list --resource-group $RESOURCE_GROUP_NAME --query "[0].id" -o tsv)

az role assignment create --assignee "$LOGGED_IN_USER" \
        --role "Azure Kubernetes Service RBAC Cluster Admin" \
        --scope "$AKS_ID"


echo "Push the image to ACR"

bash $SCRIPT_PATH/src/app/Api/push-image.sh -tag=latest -acr=$ACR_URI

echo "Deploy AKS services to cluster"

bash $SCRIPT_PATH/src/aksdeployment/deploy.sh -rg=$RESOURCE_GROUP_NAME

echo "Deploy Chaos Resources"

bash $SCRIPT_PATH/src/chaos/create-chaos-experiment.sh -rg=$RESOURCE_GROUP_NAME

echo "Fetching Azure Front Door endpoint"
FRONT_DOOR_PROFILE=$(az afd profile list --resource-group $RESOURCE_GROUP_NAME --query "[0].name" -o tsv)
FRONT_DOOR_HOSTNAME=$(az afd endpoint list --profile-name $FRONT_DOOR_PROFILE --resource-group $RESOURCE_GROUP_NAME --query "[0].hostName" -o tsv)

echo "Health check for $FRONT_DOOR_HOSTNAME"

bash $SCRIPT_PATH/tests/run-health-checks.sh --host=$FRONT_DOOR_HOSTNAME

echo "Deployment completed successfully"

