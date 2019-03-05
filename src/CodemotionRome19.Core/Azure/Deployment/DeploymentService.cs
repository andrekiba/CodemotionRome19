using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using CodemotionRome19.Core.Base;
using Microsoft.Azure.Management.ResourceManager.Fluent;

namespace CodemotionRome19.Core.Azure.Deployment
{
    public class DeploymentService : IDeploymentService
    {
        public event EventHandler<DeploymentEventArgs> Started;

        public event EventHandler<DeploymentEventArgs> Finished;

        public event EventHandler<DeploymentErrorEventArgs> Failed;

        public void Deploy(
            Microsoft.Azure.Management.Fluent.Azure.IAuthenticated azure, 
            DeploymentOptions options, 
            IEnumerable<AzureResource> resources)
        {
            foreach (var resource in resources)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        Started?.Invoke(this, new DeploymentEventArgs(resource));

                        await CreateResourceAsync(azure, options, resource.Type);

                        Finished?.Invoke(this, new DeploymentEventArgs(resource));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error creating resource of type {resource.Type}: {ex}");
                        Failed?.Invoke(this, new DeploymentErrorEventArgs(resource, ex));
                    }
                });
            }
        }

        public async Task<Result> Deploy(
            Microsoft.Azure.Management.Fluent.Azure.IAuthenticated azure,
            DeploymentOptions options,
            AzureResource resource)
        {           
            try
            {
                await CreateResourceAsync(azure, options, resource.Type);
                return Result.Ok();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating resource of type {resource.Type}: {ex}");
                return Result.Fail(ex.Message);
            }
        }

        static Task CreateResourceAsync(
            Microsoft.Azure.Management.Fluent.Azure.IAuthenticated azure,
            DeploymentOptions options,
            AzureResourceType resourceType)
        {
            var resourceName = GetRandomResourceName(resourceType);
            BaseDeployment deployment = null;

            switch (resourceType)
            {
                case AzureResourceType.AppService:
                case AzureResourceType.WebApp:
                    deployment = new WebAppDeployment(resourceName, azure, options);
                    break;
                case AzureResourceType.Storage:
                    deployment = new StorageAccountDeployment(resourceName, azure, options);
                    break;
                case AzureResourceType.CosmosDB:
                    deployment = new CosmosDbAccountDeployment(resourceName, azure, options);
                    break;
                case AzureResourceType.Functions:
                    deployment = new AzureFunctionDeployment(resourceName, azure, options);
                    break;
                case AzureResourceType.SqlDatabase:
                    deployment = new SqlAzureDeployment(resourceName, azure, options);
                    break;
                case AzureResourceType.KeyVault:
                    deployment = new KeyVaultDeployment(resourceName, azure, options);
                    break;
                case AzureResourceType.VirtualMachine:
                    deployment = new VirtualMachineDeployment(resourceName, azure, options);
                    break;
                default:
                    Debug.WriteLine($"Service of type {resourceType} not supported!");
                    break;
            }

            return deployment?.CreateAsync() ?? Task.CompletedTask;
        }

        static string GetRandomResourceName(AzureResourceType resourceType)
        {
            const int maxNameLength = 20;

            const string cosmosDBPrefix = "cosmos-";
            const string functionsPrefix = "function-";
            const string storagePrefix = "storage";
            const string webAppPrefix = "web-";
            const string sqlPrefix = "sql-";
            const string kvPrefix = "vault-";
            const string vmPrefix = "vm-";

            var prefix = string.Empty;

            switch (resourceType)
            {
                case AzureResourceType.AppService:
                case AzureResourceType.WebApp:
                    prefix = webAppPrefix;
                    break;
                case AzureResourceType.Storage:
                    prefix = storagePrefix;
                    break;
                case AzureResourceType.CosmosDB:
                    prefix = cosmosDBPrefix;
                    break;
                case AzureResourceType.Functions:
                    prefix = functionsPrefix;
                    break;
                case AzureResourceType.SqlDatabase:
                    prefix = sqlPrefix;
                    break;
                case AzureResourceType.KeyVault:
                    prefix = kvPrefix;
                    break;
                case AzureResourceType.VirtualMachine:
                    prefix = vmPrefix;
                    break;
            }

            return SdkContext.RandomResourceName(prefix, maxNameLength);
        }
    }
}
