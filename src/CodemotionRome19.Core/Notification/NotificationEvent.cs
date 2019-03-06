using System.Collections.Generic;
using Alexa.NET.ProactiveEvents;

namespace CodemotionRome19.Core.Notification
{
    public class NotificationEvent : ProactiveEvent
    {
        private Dictionary<string, List<LocaleAttribute>> Locales { get; }

        public NotificationEvent() : this(new Dictionary<string, List<LocaleAttribute>>()) { }

        public NotificationEvent(Dictionary<string, List<LocaleAttribute>> locales) : base("NotificationEvent")
        {
            Locales = locales;
        }

        public override IEnumerable<KeyValuePair<string, List<LocaleAttribute>>> GetLocales()
        {
            return Locales;
        }
    }
}
