using CodemotionRome19.Core.Azure;

namespace CodemotionRome19.Functions.Models
{
    public class AzureResourceToDeploy
    {
        public AzureResource AzureResource { get; set; }
        public string RequestedByUser { get; set; }
    }
}