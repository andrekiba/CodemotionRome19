using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CodemotionRome19.Core.Base;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using IAuthenticated = Microsoft.Azure.Management.Fluent.Azure.IAuthenticated;

namespace CodemotionRome19.Core.Azure.Deployment
{
    public class DeploymentService : IDeploymentService
    {
        public async Task<Result[]> Deploy( IAuthenticated azure, DeploymentOptions options, IEnumerable<AzureResource> resources)
        {
            var tasks = resources.Select(resource => CreateResourceAsync(azure, options, resource.Name, resource.Type)).ToList();

            var processingTasks = tasks.Select(async t => await t).ToArray();

            var result = await Task.WhenAll(processingTasks);

            return result;
        }

        public async Task<Result> Deploy(IAuthenticated azure, DeploymentOptions options, AzureResource resource) => 
            await CreateResourceAsync(azure, options, resource.Name, resource.Type);

        static Task<Result> CreateResourceAsync(IAuthenticated azure, DeploymentOptions options, string resourceName, AzureResourceType resourceType)
        {
            var rName = string.IsNullOrWhiteSpace(resourceName) ? GetRandomResourceName(resourceType) : resourceName;
            BaseDeployment deployment = null;
            var notSupported = $"Service {resourceType} not supported!";

            if (resourceType.Id == AzureResourceTypes.AppService.Id || resourceType.Id == AzureResourceTypes.WebApp.Id)
                deployment = new WebAppDeployment(rName, azure, options);
            else if (resourceType.Id == AzureResourceTypes.Storage.Id)
                deployment = new StorageAccountDeployment(rName, azure, options);
            else if (resourceType.Id == AzureResourceTypes.CosmosDB.Id)
                deployment = new CosmosDbAccountDeployment(rName, azure, options);
            else if (resourceType.Id == AzureResourceTypes.Functions.Id)
                deployment = new AzureFunctionDeployment(rName, azure, options);
            else if (resourceType.Id == AzureResourceTypes.SqlDatabase.Id)
                deployment = new SqlAzureDeployment(rName, azure, options);
            else if (resourceType.Id == AzureResourceTypes.KeyVault.Id)
                deployment = new KeyVaultDeployment(rName, azure, options);
            else if (resourceType.Id == AzureResourceTypes.VirtualMachine.Id)
                deployment = new VirtualMachineDeployment(rName, azure, options);
            else
                Debug.WriteLine(notSupported);
            
            return deployment?.CreateAsync() ?? Task.FromResult(Result.Fail(notSupported));
        }

        static string GetRandomResourceName(AzureResourceType resourceType)
        {
            const int maxNameLength = 20;

            return SdkContext.RandomResourceName(resourceType.Prefix, maxNameLength);
        }
    }
}
