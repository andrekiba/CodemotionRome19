using System;
using System.Collections.Generic;

namespace CodemotionRome19.Core.Azure.Deployment
{
    public interface IDeploymentManager
    {
        event EventHandler<DeploymentEventArgs> Started;

        event EventHandler<DeploymentEventArgs> Finished;

        event EventHandler<DeploymentErrorEventArgs> Failed;

        void Deploy(
            Microsoft.Azure.Management.Fluent.Azure.IAuthenticated azure,
            DeploymentOptions options, 
            IEnumerable<AzureResource> resources);

        void Deploy(
            Microsoft.Azure.Management.Fluent.Azure.IAuthenticated azure,
            DeploymentOptions options,
            AzureResource resource);
    }
}
