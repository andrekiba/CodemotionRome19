using System.Collections.Generic;
using System.Threading.Tasks;
using CodemotionRome19.Core.Azure.Deployment;
using CodemotionRome19.Core.Models;

namespace CodemotionRome19.Core.Azure
{
    public interface IAzureService
    {
        Microsoft.Azure.Management.Fluent.Azure.IAuthenticated Authenticated { get; }

        DeploymentOptions DeploymentOptions { get; set; }

        Task<IEnumerable<Subscription>> GetSubscriptionsAsync();

        Task<IEnumerable<ResourceGroup>> GetResourceGroupsAsync();

        Task<IEnumerable<Ubication>> GetRegionsAsync();
    }
}
