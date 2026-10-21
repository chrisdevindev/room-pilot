using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace RoomPilot
{
    public static class L10n
    {
        public const string Portuguese = "pt-BR";
        public const string English = "en-US";
        static readonly IDictionary<string, IDictionary<string, string>> catalogs = LoadCatalogs();
        static volatile string currentLanguage = DetectLanguage(CultureInfo.InstalledUICulture.Name);
        public static string CurrentLanguage
        {
            get
            {
                return currentLanguage;
            }
        }

        public static IEnumerable<string> SupportedLanguages
        {
            get
            {
                return catalogs.Keys.OrderBy(code => code == Portuguese ? 0 : code == English ? 1 : 2).ThenBy(code => code);
            }
        }

        public static string DetectLanguage(string culture)
        {
            return (culture ?? "").StartsWith("pt", StringComparison.OrdinalIgnoreCase) ? Portuguese : English;
        }

        public static bool IsSupported(string language)
        {
            return language != null && catalogs.ContainsKey(language);
        }

        public static void SetLanguage(string language)
        {
            if (!IsSupported(language))
                throw new UserError(Text("error.language"));
            currentLanguage = language;
            var culture = CultureInfo.GetCultureInfo(language);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }

        public static string Text(string key)
        {
            string value;
            if (catalogs[CurrentLanguage].TryGetValue(key, out value) || catalogs[English].TryGetValue(key, out value))
                return value;
            throw new InvalidOperationException("Unknown translation key: " + key);
        }

        public static string Format(string key, params object[] arguments)
        {
            return String.Format(CultureInfo.GetCultureInfo(CurrentLanguage), Text(key), arguments);
        }

        public static IDictionary<string, string> Catalog(string language)
        {
            return new ReadOnlyDictionary<string, string>(catalogs[language]);
        }

        static IDictionary<string, IDictionary<string, string>> LoadCatalogs()
        {
            var result = new Dictionary<string, IDictionary<string, string>>(StringComparer.Ordinal);
            const string prefix = "RoomPilot.Localization.";
            var assembly = Assembly.GetExecutingAssembly();
            foreach (string resource in assembly.GetManifestResourceNames().Where(name => name.StartsWith(prefix, StringComparison.Ordinal) && name.EndsWith(".json", StringComparison.Ordinal)))
            {
                string language = resource.Substring(prefix.Length, resource.Length - prefix.Length - ".json".Length);
                CultureInfo.GetCultureInfo(language);
                using (var stream = assembly.GetManifestResourceStream(resource))
                {
                    if (stream == null)
                        throw new InvalidOperationException("Missing translation catalog: " + language);
                    using (var reader = new StreamReader(stream))
                        result.Add(language, Json.Serializer().Deserialize<Dictionary<string, string>>(reader.ReadToEnd()));
                }
            }

            if (!result.ContainsKey(English) || !result.ContainsKey(Portuguese))
                throw new InvalidOperationException("Required translation catalog missing.");
            return result;
        }
    }
}
