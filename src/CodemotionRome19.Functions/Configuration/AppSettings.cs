using Microsoft.Extensions.Configuration;
using IAzureConfiguration = CodemotionRome19.Core.Configuration.IAzureConfiguration;

namespace CodemotionRome19.Functions.Configuration
{
    public class AppSettings : IAzureConfiguration
    {
        public string ClientId { get; }
        public string ClientSecret { get; }
        public string TenantId { get; }
        public string SubscriptionId { get; }
        public string AldoClientId { get; }
        public string AldoClientSecret { get; }

        readonly IConfigurationRoot config;

        public AppSettings()
        {
            config = new ConfigurationBuilder()
                //.SetBasePath(context.FunctionAppDirectory)
                //.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            ClientId = config.GetValue<string>("ClientId");
            ClientSecret = config.GetValue<string>("ClientSecret");
            TenantId = config.GetValue<string>("TenantId");
            SubscriptionId = config.GetValue<string>("SubscriptionId");
            AldoClientId = config.GetValue<string>("AldoClientId");
            AldoClientSecret = config.GetValue<string>("AldoClientSecret");
        }

        public string GetValue(string key) => config.GetValue<string>(key);
    }
}
