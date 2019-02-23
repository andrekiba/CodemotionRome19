using Microsoft.Azure.Management.ResourceManager.Fluent.Core;

namespace CodemotionRome19.Core.Azure.Deployment
{
    public class DeploymentOptions
    {
        public string SubscriptionId { get; set; }

        public string ResourceGroupName { get; set; }

        public bool UseExistingResourceGroup { get; set; }

        public Region Region { get; set; }

        public static DeploymentOptions Default =>
            new DeploymentOptions
            {
                Region = Region.EuropeWest,
                ResourceGroupName = "CodemotionRome19",
                UseExistingResourceGroup = false,
            };
    }
}
