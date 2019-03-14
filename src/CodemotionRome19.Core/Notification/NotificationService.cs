using System;
using System.Threading.Tasks;
using Alexa.NET.ProactiveEvents;
using Alexa.NET.ProactiveEvents.MessageReminders;
using CodemotionRome19.Core.Azure;
using CodemotionRome19.Core.Base;
using CodemotionRome19.Core.Configuration;

namespace CodemotionRome19.Core.Notification
{
    public class NotificationService : INotificationService
    {
        readonly IConfiguration configuration;

        public NotificationService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async Task<Result> SendUserNotification(string userId, string message)
        {
            var messaging = new AccessTokenClient(AccessTokenClient.ApiDomainBaseAddress);
            var token = (await messaging.Send(configuration.AldoClientId, configuration.AldoClientSecret)).Token;

            var deployEvent = new MessageReminder(
                new MessageReminderState(MessageReminderStatus.Unread, MessageReminderFreshness.New),
                new MessageReminderGroup(message, 1, MessageReminderUrgency.Urgent));

            var userReq = new UserEventRequest(userId, deployEvent)
            {
                ReferenceId = Guid.NewGuid().ToString(),
                ExpiryTime = DateTimeExtensions.ItaNow().AddMinutes(6),
                TimeStamp = DateTimeExtensions.ItaNow()
            };

            var client = new ProactiveEventsClient(ProactiveEventsClient.EuropeEndpoint, token, true);

            var result = await client.Send(userReq);

            if(result.IsSuccessStatusCode)
                return Result.Ok();

            var errorContent = await result.Content.ReadAsStringAsync();
            var error = $"{result.ReasonPhrase}\n\r{errorContent}";
            return Result.Fail(error);
        }
    }
}
