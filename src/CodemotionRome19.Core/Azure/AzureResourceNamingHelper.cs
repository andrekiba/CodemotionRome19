using System.Text.RegularExpressions;

namespace CodemotionRome19.Core.Azure
{
    public class AzureResourceNamingHelper
    {
        public const string AzureActiveDirectory = "Azure Active Directory";
        public const string AppService = "App Service";
        public const string NotificationHubs = "Notification Hubs";
        public const string MobileApps = "Mobile Apps";
        public const string AzureSearch = "Azure Search";
        public const string AzureCdn = "Azure CDN";
        public const string AzureMachineLearning = "Azure Machine Learning";
        public const string AzureStorage = "Azure Storage";
        public const string IotEdge = "IoT Edge";
        public const string CosmosDb = "Cosmos DB";
        public const string CognitiveServices = "Cognitive Services";
        public const string SqlDatabase = "SQL Database";
        public const string AzureMysqlCleardbDatabase = "Azure MySQL ClearDB Database";
        public const string RedisCache = "Redis Cache";
        public const string AppInsights = "Application Insights";
        public const string AzureFunctions = "Azure Functions";
        public const string WebApps = "Web Apps";
        public const string KeyVault = "Key Vault";

        static readonly Regex resourceGroupNameRegex = new Regex(@"^[-\w\._\(\)]+$", RegexOptions.Compiled);

        public static bool CheckResourceGroupName(string candidate)
        {
            return resourceGroupNameRegex.IsMatch(candidate ?? string.Empty);
        }
    }
}
