using System.Collections.Generic;
using System.Linq;

namespace CodemotionRome19.Core.Azure
{
    public static class AzureResourceTypes
    {
        #region Values

        public static AzureResourceType WebApp => new AzureResourceType
        {
            Id = 1,
            Name = "WebApp",
            Prefix = "web-"
        };
        public static AzureResourceType Storage => new AzureResourceType
        {
            Id = 2,
            Name = "Storage",
            Prefix = "storage-"
        };
        public static AzureResourceType CosmosDB => new AzureResourceType
        {
            Id = 3,
            Name = "CosmosDB",
            Prefix = "cosmos-"
        };
        public static AzureResourceType Functions => new AzureResourceType
        {
            Id = 4,
            Name = "Functions",
            Prefix = "function-"
        };
        public static AzureResourceType ActiveDirectory => new AzureResourceType
        {
            Id = 5,
            Name = "ActiveDirectory",
            Prefix = "ad-"
        };
        public static AzureResourceType NotificationHubs => new AzureResourceType
        {
            Id = 6,
            Name = "NotificationHubs",
            Prefix = "nhub-"
        };
        public static AzureResourceType MobileApps => new AzureResourceType
        {
            Id = 7,
            Name = "MobileApps",
            Prefix = "mobile-"
        };
        public static AzureResourceType AzureSearch => new AzureResourceType
        {
            Id = 8,
            Name = "AzureSearch",
            Prefix = "search-"
        };
        public static AzureResourceType Cdn => new AzureResourceType
        {
            Id = 9,
            Name = "Cdn",
            Prefix = "cdn-"
        };
        public static AzureResourceType MachineLearning => new AzureResourceType
        {
            Id = 10,
            Name = "MachineLearning",
            Prefix = "ml-"
        };
        public static AzureResourceType IotEdge => new AzureResourceType
        {
            Id = 11,
            Name = "IotEdge",
            Prefix = "iot-"
        };
        public static AzureResourceType CognitiveServices => new AzureResourceType
        {
            Id = 12,
            Name = "CognitiveServices",
            Prefix = "cs-"
        };
        public static AzureResourceType SqlDatabase => new AzureResourceType
        {
            Id = 13,
            Name = "SqlDatabase",
            Prefix = "sql-"
        };
        public static AzureResourceType MySqlClearDatabase => new AzureResourceType
        {
            Id = 14,
            Name = "MySqlClearDatabase",
            Prefix = "mysql-"
        };
        public static AzureResourceType RedisCache => new AzureResourceType
        {
            Id = 15,
            Name = "RedisCache",
            Prefix = "redis-"
        };
        public static AzureResourceType ApplicationInsights => new AzureResourceType
        {
            Id = 16,
            Name = "ApplicationInsights",
            Prefix = "ins-"
        };
        public static AzureResourceType AppService => new AzureResourceType
        {
            Id = 17,
            Name = "AppService",
            Prefix = "app-"
        };
        public static AzureResourceType KeyVault => new AzureResourceType
        {
            Id = 18,
            Name = "KeyVault",
            Prefix = "vault-"
        };
        public static AzureResourceType VirtualMachine => new AzureResourceType
        {
            Id = 19,
            Name = "VirtualMachine",
            Prefix = "vm-"
        };

        #endregion 

        public static List<AzureResourceType> All => new List<AzureResourceType>
        {
            WebApp,
            Storage,
            CosmosDB,
            Functions,
            ActiveDirectory,
            NotificationHubs,
            MobileApps,
            AzureSearch,
            Cdn,
            MachineLearning,
            IotEdge,
            CognitiveServices,
            SqlDatabase,
            MySqlClearDatabase,
            RedisCache,
            ApplicationInsights,
            AppService,
            KeyVault,
            VirtualMachine
        };

        public static AzureResourceType Find(int id) => All.SingleOrDefault(x=> x.Id == id);
        public static AzureResourceType Get(int id) => All.Single(x => x.Id == id);
    }

    public class AzureResourceType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<string> Synonyms { get; set; } = new List<string>();
        public string Prefix { get; set; } = string.Empty;
    }
}
