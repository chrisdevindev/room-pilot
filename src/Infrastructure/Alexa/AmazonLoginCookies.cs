using System;
using System.Collections.Generic;
using System.Linq;

namespace RoomPilot
{
    public static class AmazonLoginCookies
    {
        static bool Allowed(string name)
        {
            return new[]
            {
                "session-id",
                "session-id-time",
                "session-token",
                "frc",
                "map-md",
                "csrf",
                "sid",
                "i18n-prefs"
            }.Contains(name) || new[]
            {
                "ubid-",
                "lc-",
                "x-",
                "at-",
                "sess-at-"
            }.Any(p => name.StartsWith(p, StringComparison.Ordinal));
        }

        public static IDictionary<string, string> Normalize(IDictionary<string, string> source)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var pair in source)
            {
                if (String.IsNullOrEmpty(pair.Key) || !Allowed(pair.Key) || pair.Key.IndexOfAny(new[] { '\r', '\n', ';', '=' }) >= 0)
                    continue;
                string value = pair.Value ?? "";
                if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
                    value = value.Substring(1, value.Length - 2);
                // Cookie values use percent escapes, not form encoding: '+' must remain '+'.
                try
                {
                    value = Uri.UnescapeDataString(value);
                }
                catch (UriFormatException)
                {
                }

                if (value.IndexOfAny(new[] { '\r', '\n', ';' }) >= 0)
                    continue;
                result[pair.Key] = value;
            }

            return result;
        }
    }
}
