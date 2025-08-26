using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Nop.Plugin.Misc.PushNotifications.Helpers
{
    public static class ScheduleHelper
    {
        // AllowedDays example: "Mon-Fri" or "Sat,Sun" or "Mon,Wed,Fri"
        public static bool IsAllowedNow(string allowedDays, string allowedHours, bool useUtc)
        {
            var now = useUtc ? DateTime.UtcNow : DateTime.Now;
            if (!IsAllowedDay(now, allowedDays))
                return false;

            if (!IsAllowedHour(now, allowedHours))
                return false;

            return true;
        }

        private static bool IsAllowedDay(DateTime now, string allowedDays)
        {
            if (string.IsNullOrWhiteSpace(allowedDays))
                return true; // no restriction

            var map = new Dictionary<string, DayOfWeek>(StringComparer.OrdinalIgnoreCase)
            {
                ["Mon"] = DayOfWeek.Monday,
                ["Tue"] = DayOfWeek.Tuesday,
                ["Wed"] = DayOfWeek.Wednesday,
                ["Thu"] = DayOfWeek.Thursday,
                ["Fri"] = DayOfWeek.Friday,
                ["Sat"] = DayOfWeek.Saturday,
                ["Sun"] = DayOfWeek.Sunday,
            };

            allowedDays = allowedDays.Replace(" ", string.Empty);
            var parts = allowedDays.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var allowed = new HashSet<DayOfWeek>();
            foreach (var part in parts)
            {
                if (part.Contains('-'))
                {
                    var range = part.Split('-', StringSplitOptions.RemoveEmptyEntries);
                    if (range.Length != 2 || !map.ContainsKey(range[0]) || !map.ContainsKey(range[1]))
                        continue;
                    var start = map[range[0]];
                    var end = map[range[1]];
                    var d = start;
                    for (int i = 0; i < 7; i++)
                    {
                        allowed.Add(d);
                        if (d == end) break;
                        d = (DayOfWeek)(((int)d + 1) % 7);
                    }
                }
                else
                {
                    if (map.TryGetValue(part, out var day))
                        allowed.Add(day);
                }
            }
            if (allowed.Count == 0) return true; // invalid string -> do not block
            return allowed.Contains(now.DayOfWeek);
        }

        // AllowedHours example: "09:00-12:30,18:00-21:00"
        private static bool IsAllowedHour(DateTime now, string allowedHours)
        {
            if (string.IsNullOrWhiteSpace(allowedHours))
                return true; // no restriction

            allowedHours = allowedHours.Replace(" ", string.Empty);
            var ranges = allowedHours.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var minutesNow = now.Hour * 60 + now.Minute;
            foreach (var range in ranges)
            {
                var parts = range.Split('-', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2) continue;
                if (!TimeSpanTryParse(parts[0], out var start) || !TimeSpanTryParse(parts[1], out var end))
                    continue;
                var mStart = (int)start.TotalMinutes;
                var mEnd = (int)end.TotalMinutes;
                if (mEnd < mStart)
                {
                    // crosses midnight
                    if (minutesNow >= mStart || minutesNow <= mEnd)
                        return true;
                }
                else
                {
                    if (minutesNow >= mStart && minutesNow <= mEnd)
                        return true;
                }
            }
            return false;
        }

        private static bool TimeSpanTryParse(string s, out TimeSpan ts)
        {
            return TimeSpan.TryParseExact(s, @"hh\:mm", CultureInfo.InvariantCulture, out ts) ||
                   TimeSpan.TryParseExact(s, @"h\:mm", CultureInfo.InvariantCulture, out ts);
        }
    }
}
