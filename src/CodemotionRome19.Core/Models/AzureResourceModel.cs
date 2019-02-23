using System;
using CodemotionRome19.Core.Azure;

namespace CodemotionRome19.Core.Models
{
    public class AzureResourceModel
    {
        public string Name { get; set; }
        public AzureResourceType Type { get; set; }
        public string Description { get; set; }
        public double EstimatedCost { get; set; }
        public DateTime Created { get; set; }
        public bool IsCreated { get; set; }
        public bool IsDeploying { get; set; }
        public bool HasFailed { get; set; }
        public float Cost { get; set; }

        public static AzureResourceModel FromResource(AzureResource resource)
        {
            return new AzureResourceModel
            {
                Name = resource.Name,
                Description = resource.Description,
                Type = resource.Type,
            };
        }
    }
}