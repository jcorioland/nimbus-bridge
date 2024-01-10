# NimbusBridge: Seamless integration of your legcacy software with Azure Cloud

NimbusBridge is an architectural framework that aim to simplify the integration of legacy software with the Azure Cloud.

## Getting Started

### Local environment

Install the following prerequisites:

- [Azure Developer CLI](https://aka.ms/azure-dev/install)
- [.NET 8](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Visual Studio Code](https://code.visualstudio.com/download) or [Visual Studio 2022](https://visualstudio.microsoft.com/downloads/)
- [Git](https://git-scm.com/downloads)
- [Powershell 7+ (pwsh)](https://github.com/powershell/powershell) - For Windows users only.
- [Docker](https://www.docker.com/products/docker-desktop/)

   > **Important**<br>
   > Ensure Docker is running before running any `azd` provisioning / deployment commands.

Then, run the following commands to get the project on your local environment:

   1. Clone the repository
   1. Run `azd auth login`
   1. Run `azd env new <env-name>` where `<env-name>` is the name of your environment
   1. Run `azd up` to provision the environment and deploy the `NimbusBridgeApi` application

> **Note**<br>
> If you want to deploy only the infrastructure and not deploy the application, run `azd provision` instead of `azd up`.