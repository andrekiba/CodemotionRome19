using CodemotionRome19.Core.Azure;

namespace CodemotionRome19.Core.Models
{
    public class AzureResourceToDeploy
    {
        public AzureResource AzureResource { get; set; }
        public string RequestedByUser { get; set; }
        public ProjectToDeploy Project { get; set; }
    }
}