using System.Collections.Generic;
using System.Threading.Tasks;
using CodemotionRome19.Core.Base;
using IAuthenticated = Microsoft.Azure.Management.Fluent.Azure.IAuthenticated;

namespace CodemotionRome19.Core.Azure.Deployment
{
    public interface IDeploymentService
    {
        Task<Result[]> Deploy(IAuthenticated azure, DeploymentOptions options, IEnumerable<AzureResource> resources);

        Task<Result> Deploy(IAuthenticated azure, DeploymentOptions options, AzureResource resource);
    }
}
