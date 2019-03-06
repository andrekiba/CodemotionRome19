using System.Net.Http;
using System.Threading.Tasks;
using CodemotionRome19.Core.Azure;
using CodemotionRome19.Core.Base;

namespace CodemotionRome19.Core.Notification
{
    public interface INotificationService
    {
        Task<HttpResponseMessage> SendNotification(AzureResource azureResource, Result deployResult);
    }
}
