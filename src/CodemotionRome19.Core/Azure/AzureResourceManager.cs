using System;
using System.Collections.Generic;
using System.Linq;

namespace CodemotionRome19.Core.Azure
{
    public class AzureResourceManager
    {
        static readonly Lazy<AzureResourceManager> Lazy = new Lazy<AzureResourceManager>(() => new AzureResourceManager());

        AzureResourceManager()
        {
            InitializeResources();
        }

        public static AzureResourceManager Instance => Lazy.Value;

        public List<AzureResource> AvailableResources { get; private set; }

        public List<AzureResource> ResourcesToDeploy { get; set; } = new List<AzureResource>();

        void InitializeResources()
        {
            var allResources = new List<AzureResource>
            {
                new AzureResource
                {
                    Type = AzureResourceType.ActiveDirectory,
                    Name = AzureResourceNamingHelper.AzureActiveDirectory,
                    Description = AzureResourceNamingHelper.AzureActiveDirectory,
                    IsAvailable = false,
                },
                new AzureResource
                {
                    Type = AzureResourceType.AppService,
                    Name = AzureResourceNamingHelper.AppService,
                    Description = AzureResourceNamingHelper.AppService,
                    IsAvailable = true,
                },
                new AzureResource
                {
                    Type = AzureResourceType.NotificationHubs,
                    Name = AzureResourceNamingHelper.NotificationHubs,
                    Description = AzureResourceNamingHelper.NotificationHubs,
                    IsAvailable = false,
                },
                new AzureResource
                {
                    Type = AzureResourceType.MobileApps,
                    Name = AzureResourceNamingHelper.MobileApps,
                    Description = AzureResourceNamingHelper.MobileApps,
                    IsAvailable = false,
                },
                new AzureResource
                {
                    Type = AzureResourceType.AzureSearch,
                    Name = AzureResourceNamingHelper.AzureSearch,
                    Description = AzureResourceNamingHelper.AzureSearch,
                    IsAvailable = false,
                },
                new AzureResource
                {
                    Type = AzureResourceType.Cdn,
                    Name = AzureResourceNamingHelper.AzureCdn,
                    Description = AzureResourceNamingHelper.AzureCdn,
                    IsAvailable = false,
                },
                new AzureResource
                {
                    Type = AzureResourceType.MachineLearning,
                    Name = AzureResourceNamingHelper.AzureMachineLearning,
                    Description = AzureResourceNamingHelper.AzureMachineLearning,
                    IsAvailable = false,
                },
                new AzureResource
                {
                    Type = AzureResourceType.Storage,
                    Name = AzureResourceNamingHelper.AzureStorage,
                    Description = AzureResourceNamingHelper.AzureStorage,
                    IsAvailable = true,
                },
                new AzureResource
                {
                    Type = AzureResourceType.IotEdge,
                    Name = AzureResourceNamingHelper.IotEdge,
                    Description = AzureResourceNamingHelper.IotEdge,
                    IsAvailable = false,
                },
                new AzureResource
                {
                    Type = AzureResourceType.CosmosDB,
                    Name = AzureResourceNamingHelper.CosmosDb,
                    Description = AzureResourceNamingHelper.CosmosDb,
                    IsAvailable = true,
                },
                new AzureResource
                {
                    Type = AzureResourceType.CognitiveServices,
                    Name = AzureResourceNamingHelper.CognitiveServices,
                    Description = AzureResourceNamingHelper.CognitiveServices,
                    IsAvailable = false,
                },
                new AzureResource
                {
                    Type = AzureResourceType.SqlDatabase,
                    Name = AzureResourceNamingHelper.SqlDatabase,
                    Description = AzureResourceNamingHelper.SqlDatabase,
                    IsAvailable = true,
                },
                new AzureResource
                {
                    Type = AzureResourceType.MySqlClearDatabase,
                    Name = AzureResourceNamingHelper.AzureMysqlCleardbDatabase,
                    Description = AzureResourceNamingHelper.AzureMysqlCleardbDatabase,
                    IsAvailable = false,
                },
                new AzureResource
                {
                    Type = AzureResourceType.RedisCache,
                    Name = AzureResourceNamingHelper.RedisCache,
                    Description = AzureResourceNamingHelper.RedisCache,
                    IsAvailable = false,
                },
                new AzureResource
                {
                    Type = AzureResourceType.ApplicationInsights,
                    Name = AzureResourceNamingHelper.AppInsights,
                    Description = AzureResourceNamingHelper.AppInsights,
                    IsAvailable = false,
                },
                new AzureResource
                {
                    Type = AzureResourceType.Functions,
                    Name = AzureResourceNamingHelper.AzureFunctions,
                    Description = AzureResourceNamingHelper.AzureFunctions,
                    IsAvailable = true,
                },
                new AzureResource
                {
                    Type = AzureResourceType.WebApp,
                    Name = AzureResourceNamingHelper.WebApps,
                    Description = AzureResourceNamingHelper.WebApps,
                    IsAvailable = true,
                },
                new AzureResource
                {
                    Type = AzureResourceType.KeyVault,
                    Name = AzureResourceNamingHelper.KeyVault,
                    Description = AzureResourceNamingHelper.KeyVault,
                    IsAvailable = true,
                },
                new AzureResource
                {
                    Type = AzureResourceType.VirtualMachine,
                    Name = AzureResourceNamingHelper.VirtualMachine,
                    Description = AzureResourceNamingHelper.VirtualMachine,
                    IsAvailable = true,
                }
            };

            AvailableResources = allResources.Where(r => r.IsAvailable).ToList();
        }
    }
}
