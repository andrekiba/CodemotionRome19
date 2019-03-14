using System.Threading.Tasks;
using IAuthenticated = Microsoft.Azure.Management.Fluent.Azure.IAuthenticated;

namespace CodemotionRome19.Core.Azure.Deployment
{
    public class StorageAccountDeployment : BaseDeployment
    {
        public StorageAccountDeployment(
            string accountName,
            IAuthenticated azure,
            DeploymentOptions options)
            : base(azure, options)
        {
            AccountName = accountName;
        }

        public string AccountName { get; }

        protected override Task ExecuteCreateAsync()
        {
            var definition = Azure
               .WithSubscription(Options.SubscriptionId)
               .StorageAccounts.Define(AccountName)
               .WithRegion(Options.Region);

            var create = Options.UseExistingResourceGroup
                ? definition.WithExistingResourceGroup(Options.ResourceGroupName)
                : definition.WithNewResourceGroup(Options.ResourceGroupName);

            return create.WithBlobStorageAccountKind()
                .CreateAsync();
        }

        protected override string GetDeploymentName() => $"'{AccountName}' Azure Storage";
    }
}
