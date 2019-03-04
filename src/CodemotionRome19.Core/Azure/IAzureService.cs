using System.Collections.Generic;
using System.Threading.Tasks;
using CodemotionRome19.Core.Azure.Deployment;
using CodemotionRome19.Core.Models;
using Microsoft.Azure.Management.Fluent;
using IAuthenticated = Microsoft.Azure.Management.Fluent.Azure.IAuthenticated;

namespace CodemotionRome19.Core.Azure
{
    public interface IAzureService
    {
        IAzure Azure { get; }

        DeploymentOptions DeploymentOptions { get; set; }

        Task<IEnumerable<Subscription>> GetSubscriptionsAsync();

        Task<IEnumerable<ResourceGroup>> GetResourceGroupsAsync();

        Task<IEnumerable<Ubication>> GetRegionsAsync();

        Task<IAzure> AuthenticateAzure(string clientId, string clientSecret, string tenantId, string subscriptionId);

        Task<IAuthenticated> Authenticate(string clientId, string clientSecret, string tenantId);
    }
}
