using System.Threading.Tasks;
using IAuthenticated = Microsoft.Azure.Management.Fluent.Azure.IAuthenticated;

namespace CodemotionRome19.Core.Azure.Deployment
{
    public class AzureFunctionDeployment : BaseDeployment
    {
        public AzureFunctionDeployment(
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
                .AppServices.FunctionApps
                .Define(AppName)
                .WithRegion(Options.Region);

            var create = Options.UseExistingResourceGroup
                ? definition.WithExistingResourceGroup(Options.ResourceGroupName)
                : definition.WithNewResourceGroup(Options.ResourceGroupName);
            
            return create.CreateAsync();
        }

        protected override string GetDeploymentName() => $"'{AppName}' Azure Function";
    }
}
