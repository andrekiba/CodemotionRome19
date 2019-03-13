using System;
using System.Linq;

namespace CodemotionRome19.Core.Base
{
    public static class StringExtensions
    {
        public static string ToLowerCamelCase(this string text)
        {
            return string.Join("", text.Split(new[] { "-", "_", " " }, StringSplitOptions.RemoveEmptyEntries)
                .Select((w, i) => i == 0 ? w.ToLower() : w.Substring(0, 1).ToUpper() + w.Substring(1).ToLower()));
        }
    }

    public static class DateTimeExtensions
    {
        public static DateTime ItaNow()
        {
            const string zoneId = "Central European Standard Time";
            var zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
            var itaNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
            return itaNow;
        }
    }
}
