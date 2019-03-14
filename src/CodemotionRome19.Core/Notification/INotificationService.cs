using System.Net.Http;
using System.Threading.Tasks;
using CodemotionRome19.Core.Azure;
using CodemotionRome19.Core.Base;
using CodemotionRome19.Core.Models;

namespace CodemotionRome19.Core.Notification
{
    public interface INotificationService
    {
        Task<Result> SendUserNotification(string userId, string message);
    }
}
