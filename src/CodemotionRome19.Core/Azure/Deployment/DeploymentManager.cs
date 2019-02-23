﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ResourceManager.Fluent;

namespace CodemotionRome19.Core.Azure.Deployment
{
    public class DeploymentManager : IDeploymentManager
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
                Task.Factory.StartNew(async () =>
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

        Task CreateResourceAsync(
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
                default:
                    Debug.WriteLine($"Service of type {resourceType} not supported!");
                    break;
            }

            return deployment != null
                ? deployment.CreateAsync()
                : Task.CompletedTask;
        }

        static string GetRandomResourceName(AzureResourceType resourceType)
        {
            const int maxNameLength = 20;

            const string CosmosDBPrefix = "cosmos-";
            const string FunctionsPrefix = "function-";
            const string StoragePrefix = "storage";
            const string WebAppPrefix = "web-";
            const string SqlPrefix = "sql-";
            const string KvPrefix = "vault-";

            var prefix = string.Empty;

            switch (resourceType)
            {
                case AzureResourceType.AppService:
                case AzureResourceType.WebApp:
                    prefix = WebAppPrefix;
                    break;
                case AzureResourceType.Storage:
                    prefix = StoragePrefix;
                    break;
                case AzureResourceType.CosmosDB:
                    prefix = CosmosDBPrefix;
                    break;
                case AzureResourceType.Functions:
                    prefix = FunctionsPrefix;
                    break;
                case AzureResourceType.SqlDatabase:
                    prefix = SqlPrefix;
                    break;
                case AzureResourceType.KeyVault:
                    prefix = KvPrefix;
                    break;
            }

            return SdkContext.RandomResourceName(prefix, maxNameLength);
        }
    }
}
