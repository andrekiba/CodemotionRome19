using System.Threading.Tasks;
using Microsoft.Azure.Management.CosmosDB.Fluent.Models;

namespace CodemotionRome19.Core.Azure.Deployment
{
    public class CosmosDbAccountDeployment : BaseDeployment
    {
        public CosmosDbAccountDeployment(
            string docDbName,
            Microsoft.Azure.Management.Fluent.Azure.IAuthenticated azure,
            DeploymentOptions options) 
            : base(azure, options)
        {
            DocDbName = docDbName;
        }

        public string DocDbName { get; }

        protected override Task ExecuteCreateAsync()
        {
            var definition = Azure
                .WithSubscription(Options.SubscriptionId)
                .CosmosDBAccounts.Define(DocDbName)
                .WithRegion(Options.Region);

            var kind = Options.UseExistingResourceGroup
                ? definition.WithExistingResourceGroup(Options.ResourceGroupName)
                : definition.WithNewResourceGroup(Options.ResourceGroupName);

            return kind
                .WithKind(DatabaseAccountKind.GlobalDocumentDB)
                .WithSessionConsistency()
                .WithWriteReplication(Options.Region)
                .WithReadReplication(Options.Region)
                .CreateAsync();
        }

        protected override string GetDeploymentName()
        {
            return $"'{DocDbName}' CosmosDB Account";
        }

        protected override string GetEventName()
        {
            return "Azure CosmosDB";
        }
    }
}
