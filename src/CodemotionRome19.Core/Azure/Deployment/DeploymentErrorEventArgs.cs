using System;

namespace CodemotionRome19.Core.Azure.Deployment
{
    public class DeploymentErrorEventArgs : DeploymentEventArgs
    {
        public DeploymentErrorEventArgs(AzureResource resource, Exception exception)
            : base(resource)
        {
            Exception = exception;
        }

        public Exception Exception { get; }
    }
}
