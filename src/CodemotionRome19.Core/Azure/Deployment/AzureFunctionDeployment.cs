using System.Threading.Tasks;

namespace CodemotionRome19.Core.Azure.Deployment
{
    public class AzureFunctionDeployment : BaseDeployment
    {
        public AzureFunctionDeployment(
            string appName, 
            Microsoft.Azure.Management.Fluent.Azure.IAuthenticated azure, 
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

        protected override string GetDeploymentName()
        {
            return $"'{AppName}' Azure Function";
        }

        protected override string GetEventName()
        {
            return "Azure Functions";
        }
    }
}
