using System;

namespace CodemotionRome19.Core.Azure.Deployment
{
    public class DeploymentEventArgs : EventArgs
    {
        public DeploymentEventArgs(AzureResource resource)
        {
            Resource = resource;
        }

        public AzureResource Resource { get; }
    }
}
