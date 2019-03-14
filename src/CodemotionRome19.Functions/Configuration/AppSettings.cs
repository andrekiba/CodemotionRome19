using Microsoft.Extensions.Configuration;

namespace CodemotionRome19.Functions.Configuration
{
    public class AppSettings : Core.Configuration.IConfiguration
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
