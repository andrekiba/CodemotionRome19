using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Alexa.NET.ProactiveEvents;
using Alexa.NET.ProactiveEvents.MessageReminders;
using CodemotionRome19.Core.Azure;
using CodemotionRome19.Core.Base;
using CodemotionRome19.Core.Configuration;
using CodemotionRome19.Core.Models;

namespace CodemotionRome19.Core.Notification
{
    public class NotificationService : INotificationService
    {
        //readonly string clientId;
        //readonly string clientSecret;

        //public NotificationService(string clientId, string clientSecret)
        //{
        //    this.clientId = clientId;
        //    this.clientSecret = clientSecret;
        //}

        readonly IAzureConfiguration azureConfiguration;

        public NotificationService(IAzureConfiguration azureConfiguration)
        {
            this.azureConfiguration = azureConfiguration;
        }

        public async Task<Result> SendUserNotification(string userId, string message)
        {
            var messaging = new AccessTokenClient(AccessTokenClient.ApiDomainBaseAddress);
            var token = (await messaging.Send(azureConfiguration.AldoClientId, azureConfiguration.AldoClientSecret)).Token;

            #region Reminder

            var deployReminder = new MessageReminder(
                new MessageReminderState(MessageReminderStatus.Unread, MessageReminderFreshness.New),
                new MessageReminderGroup(message, 1, MessageReminderUrgency.Urgent));

            #endregion

            #region UserEvent

            var deployEvent = new NotificationEvent("DeployCompleted", new Dictionary<string, List<LocaleAttribute>>
            {
                {"test", new List<LocaleAttribute>(new[] {new LocaleAttribute("it-IT", "thing"),})}
            });

            #endregion 

            var broadcastReq = new BroadcastEventRequest(deployReminder)
            {
                ReferenceId = Guid.NewGuid().ToString(),
                ExpiryTime = DateTimeExtensions.ItaNow().AddMinutes(6),
                TimeStamp = DateTimeExtensions.ItaNow()
            };

            var userReq = new UserEventRequest(userId, deployReminder)
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
