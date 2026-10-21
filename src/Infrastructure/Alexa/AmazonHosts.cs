using System;
using System.Linq;

namespace RoomPilot
{
    public static class AmazonHosts
    {
        public static readonly string[] Domains =
        {
            "amazon.com.br",
            "amazon.com",
            "amazon.de",
            "amazon.co.uk",
            "amazon.ca",
            "amazon.com.au",
            "amazon.co.jp",
            "amazon.in",
            "amazon.fr",
            "amazon.it",
            "amazon.es",
            "amazon.com.mx"
        };
        public static bool Allowed(string domain)
        {
            return Domains.Contains(domain, StringComparer.OrdinalIgnoreCase);
        }

        public static bool LoginUrl(Uri uri)
        {
            return uri != null && uri.Scheme == "https" && uri.IsDefaultPort && Domains.Any(d => uri.Host == d || uri.Host.EndsWith("." + d, StringComparison.OrdinalIgnoreCase));
        }

        public static string Require(string domain)
        {
            if (!Allowed(domain))
                throw new UserError(L10n.Text("error.region"));
            return domain;
        }
    }
}
