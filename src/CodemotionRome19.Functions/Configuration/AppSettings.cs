using Microsoft.Extensions.Configuration;

namespace CodemotionRome19.Functions.Configuration
{
    public class AppSettings
    {
        public string ClientId { get; }
        public string ClientSecret { get; }
        public string TenantId { get; }
        public string SubscriptionId { get; }

        public AppSettings()
        {
            var config = new ConfigurationBuilder()
                //.SetBasePath(context.FunctionAppDirectory)
                //.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            ClientId = config.GetValue<string>("ClientId");
            ClientSecret = config.GetValue<string>("ClientSecret");
            TenantId = config.GetValue<string>("TenantId");
            SubscriptionId = config.GetValue<string>("SubscriptionId");
        }
    }
}
