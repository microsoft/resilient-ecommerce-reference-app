#!/usr/bin/env bash

# Exit on errors
set -o errexit -o pipefail -o noclobber

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
  echo "No resource group provided. Please provide a resource group name as command line argument. E.g. '$0 -rg=my-rg-name'" >&2
  exit 1
fi

SCRIPT_PATH=$( cd "$(dirname "${BASH_SOURCE[0]}")" ; pwd -P )

export AKS_CLUSTER_NAME=$(az aks list --resource-group $RESOURCE_GROUP_NAME --query "[0].name" -o tsv)
if [ -z $AKS_CLUSTER_NAME ]; then
  echo "No AKS cluster found in resource group '$RESOURCE_GROUP_NAME'" >&2
  exit 1
fi


export ILB_IP="10.0.3.250"
export APP_IDENTITY_NAME=$(az identity list --resource-group $RESOURCE_GROUP_NAME --query "[0].name" -o tsv)
export AKS_OIDC_ISSUER=$(az aks show -n ${AKS_CLUSTER_NAME} -g $RESOURCE_GROUP_NAME --query "oidcIssuerProfile.issuerUrl" -o tsv)
export CLIENT_ID=$(az identity show --resource-group $RESOURCE_GROUP_NAME --name $APP_IDENTITY_NAME --query "clientId" -o tsv)
export KEYVAULT_NAME=$(az keyvault list --resource-group $RESOURCE_GROUP_NAME --query "[0].name" -o tsv)
export USER_ASSIGNED_CLIENT_ID=$(az identity show --resource-group $RESOURCE_GROUP_NAME --name $APP_IDENTITY_NAME --query "clientId" -o tsv)
export IDENTITY_TENANT=$(az account show --query "tenantId" -o tsv)

echo "Fetching ACR URI"
export ACR_URI=$(az acr list --resource-group $RESOURCE_GROUP_NAME --query "[0].loginServer" -o tsv)
echo "ACR URI: $ACR_URI"

rm -rf $SCRIPT_PATH/.tmp
mkdir $SCRIPT_PATH/.tmp

echo "Step 1 - Creating ingress"
envsubst < $SCRIPT_PATH/controller-ingress-nginx.yaml > $SCRIPT_PATH/.tmp/controller-ingress-nginx-processed.yaml
az aks command invoke --name $AKS_CLUSTER_NAME \
    --resource-group $RESOURCE_GROUP_NAME \
    --command 'helm upgrade --install ingress-nginx ingress-nginx \
          --repo https://kubernetes.github.io/ingress-nginx \
          --namespace ingress-controller \
          --version 4.9.1 \
          --wait-for-jobs \
          --debug \
          --create-namespace \
          -f controller-ingress-nginx-processed.yaml' \
    --file $SCRIPT_PATH/.tmp/controller-ingress-nginx-processed.yaml

echo "Step 2 - Creating app namespace"
envsubst < $SCRIPT_PATH/app-namespace.yaml > $SCRIPT_PATH/.tmp/app-namespace-processed.yaml
az aks command invoke --name $AKS_CLUSTER_NAME \
    --resource-group $RESOURCE_GROUP_NAME \
    --command 'kubectl apply -f app-namespace-processed.yaml' \
    --file $SCRIPT_PATH/.tmp/app-namespace-processed.yaml

az identity federated-credential create \
  --name $APP_IDENTITY_NAME \
  --identity-name $APP_IDENTITY_NAME \
  --resource-group $RESOURCE_GROUP_NAME \
  --issuer $AKS_OIDC_ISSUER \
  --subject system:serviceaccount:app:workload

echo "Step 3 - Container insights config"
az aks command invoke --name $AKS_CLUSTER_NAME \
    --resource-group $RESOURCE_GROUP_NAME \
    --command 'kubectl apply -f container-azm-ms-agentconfig.yaml' \
    --file $SCRIPT_PATH/container-azm-ms-agentconfig.yaml

rm -rf $SCRIPT_PATH/.tmp
