using System.Collections.Generic;
using System.Threading.Tasks;
using CodemotionRome19.Core.Azure.Deployment;
using CodemotionRome19.Core.Models;
using Microsoft.Azure.Management.Fluent;

namespace CodemotionRome19.Core.Azure
{
    public interface IAzureService
    {
        IAzure Azure { get; }

        DeploymentOptions DeploymentOptions { get; set; }

        Task<IEnumerable<Subscription>> GetSubscriptionsAsync();

        Task<IEnumerable<ResourceGroup>> GetResourceGroupsAsync();

        Task<IEnumerable<Ubication>> GetRegionsAsync();

        Task<IAzure> Authenticate(string clientId, string clientSecret, string tenantId, string subscriptionId);
    }
}
