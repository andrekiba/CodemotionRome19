using System.Collections.Generic;
using Alexa.NET.ProactiveEvents;

namespace CodemotionRome19.Core.Notification
{
    public class NotificationEvent : ProactiveEvent
    {
        Dictionary<string, List<LocaleAttribute>> Locales { get; }

        public NotificationEvent(string name = "NotificationEvent",
            Dictionary<string, List<LocaleAttribute>> locales = null) : base(name)
        {
            Locales = locales ?? new Dictionary<string, List<LocaleAttribute>>();
        }

        public override IEnumerable<KeyValuePair<string, List<LocaleAttribute>>> GetLocales()
        {
            return Locales;
        }
    }
}
