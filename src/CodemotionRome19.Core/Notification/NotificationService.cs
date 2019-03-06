using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Alexa.NET.ProactiveEvents;
using Alexa.NET.ProactiveEvents.MessageReminders;
using CodemotionRome19.Core.Azure;
using CodemotionRome19.Core.Base;

namespace CodemotionRome19.Core.Notification
{
    public class NotificationService : INotificationService
    {
        readonly string clientId;
        readonly string clientSecret;

        public NotificationService(string clientId, string clientSecret)
        {
            this.clientId = clientId;
            this.clientSecret = clientSecret;
        }

        public async Task<HttpResponseMessage> SendNotification(AzureResource azureResource, Result deployResult)
        {
            var messaging = new AccessTokenClient(AccessTokenClient.ApiDomainBaseAddress);
            var token = (await messaging.Send(clientId, clientSecret)).Token;

            #region Reminder

            var deployReminder = new MessageReminder(
                new MessageReminderState(MessageReminderStatus.Unread, MessageReminderFreshness.New),
                new MessageReminderGroup("Aldo", 1, MessageReminderUrgency.Urgent));

            #endregion

            #region UserEvent

            var deployEvent = new NotificationEvent("DeployCompleted", new Dictionary<string, List<LocaleAttribute>>
            {
                {"test", new List<LocaleAttribute>(new[] {new LocaleAttribute("it-IT", "thing"),})}
            });

            #endregion 

            var boradcastReq = new BroadcastEventRequest(deployReminder)
            {
                ReferenceId = "unique-id-of-this-instance",
                ExpiryTime = DateTimeOffset.Now.AddMinutes(5),
                TimeStamp = DateTimeOffset.Now
            };

            var userReq = new UserEventRequest("userId", deployEvent)
            {
                ReferenceId = "unique-id-of-this-instance",
                ExpiryTime = DateTimeOffset.Now.AddMinutes(5),
                TimeStamp = DateTimeOffset.Now
            };

            var client = new ProactiveEventsClient(ProactiveEventsClient.EuropeEndpoint, token, true);

            return await client.Send(userReq);
        }
    }
}
