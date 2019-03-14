using System.Threading.Tasks;
using Microsoft.Azure.Management.Sql.Fluent.Models;
using IAuthenticated = Microsoft.Azure.Management.Fluent.Azure.IAuthenticated;

namespace CodemotionRome19.Core.Azure.Deployment
{
    public class SqlAzureDeployment : BaseDeployment
    {
        public SqlAzureDeployment(
            string serverName,
            IAuthenticated azure,
            DeploymentOptions options)
            : base(azure, options)
        {
            ServerName = serverName;
        }

        public string ServerName { get; }

        protected override async Task ExecuteCreateAsync()
        {
            var definition = Azure
                .WithSubscription(Options.SubscriptionId)
                .SqlServers.Define(ServerName)
                .WithRegion(Options.Region);

            var withLogin = Options.UseExistingResourceGroup
                ? definition.WithExistingResourceGroup(Options.ResourceGroupName)
                : definition.WithNewResourceGroup(Options.ResourceGroupName);

            var sqlServer = await withLogin
                .WithAdministratorLogin("andre")
                .WithAdministratorPassword("4ndr3P4ssw0rd")
                .CreateAsync();

            var database = await sqlServer.Databases.Define($"{ServerName}_db")
                .WithEdition(DatabaseEditions.Basic)
                .CreateAsync();
        }

        protected override string GetDeploymentName() => $"'{ServerName}' SQL Azure";
    }
}
