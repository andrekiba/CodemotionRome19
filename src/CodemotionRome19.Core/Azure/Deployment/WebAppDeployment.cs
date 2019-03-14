using System.Threading.Tasks;
using Microsoft.Azure.Management.AppService.Fluent;
using IAuthenticated = Microsoft.Azure.Management.Fluent.Azure.IAuthenticated;

namespace CodemotionRome19.Core.Azure.Deployment
{
    public class WebAppDeployment : BaseDeployment
    {
        public WebAppDeployment(
            string appName,
            IAuthenticated azure,
            DeploymentOptions options)
            : base(azure, options)
        {
            AppName = appName;
        }

        public string AppName { get; }

        protected override Task ExecuteCreateAsync()
        {
            var definition = Azure
              .WithSubscription(Options.SubscriptionId)
              .WebApps.Define(AppName)
              .WithRegion(Options.Region);

            var create = Options.UseExistingResourceGroup
                ? definition.WithExistingResourceGroup(Options.ResourceGroupName)
                : definition.WithNewResourceGroup(Options.ResourceGroupName);

            return create
                .WithNewWindowsPlan(PricingTier.FreeF1)
                .CreateAsync();
        }

        protected override string GetDeploymentName() => $"'{AppName}' Web App";
    }
}
