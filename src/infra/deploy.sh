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


# Deploy Az Ref App to the specified resource group
RESOURCES_SUFFIX_UID=${RESOURCE_GROUP_NAME: -6}
DEPLOYMENT_NAME=refapp-deploy

SCRIPT_PATH=$( cd "$(dirname "${BASH_SOURCE[0]}")" ; pwd -P )

az deployment group create \
  --resource-group $RESOURCE_GROUP_NAME \
  --name $DEPLOYMENT_NAME \
  --template-file $SCRIPT_PATH/main.bicep \
  --parameters $SCRIPT_PATH/main.bicepparam \
  --parameters resourceSuffixUID=$RESOURCES_SUFFIX_UID \
  --verbose
