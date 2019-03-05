using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CodemotionRome19.Core.Base;

namespace CodemotionRome19.Core.Azure.Deployment
{
    public interface IDeploymentService
    {
        event EventHandler<DeploymentEventArgs> Started;

        event EventHandler<DeploymentEventArgs> Finished;

        event EventHandler<DeploymentErrorEventArgs> Failed;

        void Deploy(
            Microsoft.Azure.Management.Fluent.Azure.IAuthenticated azure,
            DeploymentOptions options, 
            IEnumerable<AzureResource> resources);

        Task<Result> Deploy(
            Microsoft.Azure.Management.Fluent.Azure.IAuthenticated azure,
            DeploymentOptions options,
            AzureResource resource);
    }
}
