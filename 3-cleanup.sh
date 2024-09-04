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

echo "Cleaning up resources"
az group delete --name $RESOURCE_GROUP_NAME --yes

echo "Deleting .env file"
rm -rf $SCRIPT_PATH/.env