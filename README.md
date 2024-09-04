# Introduction 
The Resilient Ecommerce Reference Application is a synthetic workload that mirrors a simple, bare-bones, e-commerce platform. The purpose of it is to demonstrate how to use Azure Resiliency best practices to achieve availability during zonal outages or components outages. 

# Getting Started
The automated deployment is designed to work with Linux/MACOS/WSL as all scripts are written in bash. Before running the deployment, the following prerequisites have to be met:

Install Az Cli https://learn.microsoft.com/en-us/cli/azure/install-azure-cli-linux 

Install docker https://docs.docker.com/engine/install/ubuntu/ or https://docs.docker.com/desktop/wsl/ 

Azure subscription where you have owner permissions

# Deploy test App

Clone the git repo

`git clone https://github.com/Azure/AzRefApp-Ecommerce`

Login using Az Cli: `az login` (or any other variation, depending how you authenticate, e.g. --use-device-code)

Select a subscription where you have owner permissions

Ensure the OperationsManagement resource provider is registered

`az provider register --namespace 'Microsoft.OperationsManagement'`

Deploy using:

` ./1-deploy-service.sh --service-location=region `

Region code mapping can be found by running: `az account list-locations -o table`

The output format looks like this: 

| DisplayName  | Name       | RegionalDisplayName         |
|--------------|------------|-----------------------------|
| East US      | eastus     | (US) East US                |
| West Europe  | westeurope | (Europe) West Europe        |

The script will take the name (e.g. westus, westeurope) as input. 


Start the Chaos Studio experiment that takes down AKS Zone 1 with:
` ./2-start-chaos-fault.sh `

The app saves the resource group where everything was deployed in a .env file which will be cleand-up at the end.

To clean up the deployed resources:

`./3-cleanup.sh`
