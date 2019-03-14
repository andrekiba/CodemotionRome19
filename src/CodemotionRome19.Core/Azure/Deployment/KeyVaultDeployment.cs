using System.Threading.Tasks;
using IAuthenticated = Microsoft.Azure.Management.Fluent.Azure.IAuthenticated;

namespace CodemotionRome19.Core.Azure.Deployment
{
    public class KeyVaultDeployment : BaseDeployment
    {
        public KeyVaultDeployment(
            string vaultName, 
            IAuthenticated azure,
            DeploymentOptions options)
            : base(azure, options)
        {
            VaultName = vaultName;
        }

        public string VaultName { get; }

        protected override Task ExecuteCreateAsync()
        {
            var definition = Azure
                .WithSubscription(Options.SubscriptionId)
                .Vaults.Define(VaultName)
                .WithRegion(Options.Region);

            var withPolicy = Options.UseExistingResourceGroup
                ? definition.WithExistingResourceGroup(Options.ResourceGroupName)
                : definition.WithNewResourceGroup(Options.ResourceGroupName);

            return withPolicy
                .WithEmptyAccessPolicy()
                .CreateAsync();
        }

        protected override string GetDeploymentName() => $"'{VaultName}' Key Vault";
    }
}
