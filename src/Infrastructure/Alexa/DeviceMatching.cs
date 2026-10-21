using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace RoomPilot
{
    public static class DeviceMatching
    {
        static string Normalize(string value)
        {
            string text = (value ?? "").Normalize(NormalizationForm.FormD);
            text = new string (text.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
            return String.Join(" ", text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
        }

        // Suggestions are reviewed in the mapping screen and never send a device command.
        public static AlexaEndpoint Suggest(DeviceConfig slot, IEnumerable<AlexaEndpoint> devices)
        {
            var aliases = new List<string>
            {
                Normalize(slot.Name)
            };
            if (slot.Key == "lamparina")
                aliases.Add("lampada");
            var candidates = devices.Where(d => aliases.Contains(Normalize(d.Name))).GroupBy(d => d.Id).Select(g => g.First()).ToList();
            return candidates.Count == 1 ? candidates[0] : null;
        }
    }
}
