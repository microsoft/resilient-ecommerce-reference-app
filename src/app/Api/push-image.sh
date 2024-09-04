#!/usr/bin/env bash

# Exit on errors
set -o errexit -o pipefail -o noclobber

# Parse command line arguments
for ARG in "$@"; do
  case $ARG in
    -tag=*|--image-tag=*)
      TAG="${ARG#*=}"
      shift
      ;;
    -acr=*|--acr-name=*)
      ACR="${ARG#*=}"
      shift
      ;;
    -h|--help)
      echo "
                Usage: $0 [-rg=rg-name] [-cl=client-location] [-sl=service-location] [--what-if]

                -tag, --image-tag             Image tag. e.g. staging, latest
                -acr, --acr-name              Azure Container Registry name. e.g. test.azureacr.io
                -bn, --build-number           Build number. Required when tag is latest. e.g. 1
                -h, --help                    Show this help message

                Examples:
                $0 -sl=eastus
            "
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


if [[ -z $TAG ]];then
  echo "Need valid image TAG on command line, arg1. Example -tag=staging -acr=test.azureacr.io"
  exit 1
fi

if [[ -z $ACR ]];then
  echo "Need valid ACR on command line, arg1. Example -tag=staging -acr=test.azureacr.io"
  exit 1
fi


SCRIPT_PATH=$( cd "$(dirname "${BASH_SOURCE[0]}")" ; pwd -P )
IMAGE=api-webapp

set -e
az acr login --name $ACR

docker buildx build --platform linux/amd64 -t $ACR/$IMAGE:$TAG -f $SCRIPT_PATH/Dockerfile $SCRIPT_PATH/.

docker push $ACR/$IMAGE:$TAG