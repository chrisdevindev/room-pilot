using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace RoomPilot
{
    public sealed class AmazonLoginRequest
    {
        public const string DeviceType = "A2IVLV5VM2W81";
        public string Verifier { get; private set; }
        public string ClientId { get; private set; }
        public string Serial { get; private set; }
        public string Frc { get; private set; }
        public string Url { get; private set; }

        static byte[] Random(int length)
        {
            var bytes = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(bytes);
            return bytes;
        }

        static string Url64(byte[] value)
        {
            return Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        public static string ClientIdFor(string serial)
        {
            if (String.IsNullOrEmpty(serial) || serial.Length != 32 || !serial.All(Uri.IsHexDigit))
                throw new UserError(L10n.Text("error.appIdentity"));
            return BitConverter.ToString(Encoding.ASCII.GetBytes(serial + "#" + DeviceType)).Replace("-", "").ToLowerInvariant();
        }

        public AmazonLoginRequest() : this(null)
        {
        }

        public AmazonLoginRequest(string existingSerial)
        {
            Verifier = Url64(Random(32));
            Frc = Convert.ToBase64String(Random(313));
            Serial = existingSerial ?? BitConverter.ToString(Random(16)).Replace("-", "");
            // The authorization and registration must identify the SAME virtual device.
            ClientId = ClientIdFor(Serial);
            string challenge;
            using (var sha = SHA256.Create())
                challenge = Url64(sha.ComputeHash(Encoding.ASCII.GetBytes(Verifier)));
            var query = new Dictionary<string, string>
            {
                {
                    "openid.return_to",
                    "https://www.amazon.com/ap/maplanding"
                },
                {
                    "openid.assoc_handle",
                    "amzn_dp_project_dee_ios"
                },
                {
                    "openid.identity",
                    "http://specs.openid.net/auth/2.0/identifier_select"
                },
                {
                    "pageId",
                    "amzn_dp_project_dee_ios"
                },
                {
                    "accountStatusPolicy",
                    "P1"
                },
                {
                    "openid.claimed_id",
                    "http://specs.openid.net/auth/2.0/identifier_select"
                },
                {
                    "openid.mode",
                    "checkid_setup"
                },
                {
                    "openid.ns.oa2",
                    "http://www.amazon.com/ap/ext/oauth/2"
                },
                {
                    "openid.oa2.client_id",
                    "device:" + ClientId
                },
                {
                    "openid.ns.pape",
                    "http://specs.openid.net/extensions/pape/1.0"
                },
                {
                    "openid.oa2.response_type",
                    "code"
                },
                {
                    "openid.ns",
                    "http://specs.openid.net/auth/2.0"
                },
                {
                    "openid.pape.max_auth_age",
                    "0"
                },
                {
                    "openid.oa2.scope",
                    "device_auth_access offline_access"
                },
                {
                    "openid.oa2.code_challenge_method",
                    "S256"
                },
                {
                    "openid.oa2.code_challenge",
                    challenge
                },
                {
                    "language",
                    L10n.CurrentLanguage.Replace('-', '_')
                }
            };
            Url = "https://www.amazon.com/ap/signin?" + String.Join("&", query.Select(k => Uri.EscapeDataString(k.Key) + "=" + Uri.EscapeDataString(k.Value)));
        }

        public static string AuthorizationCode(Uri uri)
        {
            if (uri == null || uri.Scheme != "https" || uri.Host != "www.amazon.com" || uri.AbsolutePath != "/ap/maplanding" || !uri.IsDefaultPort)
                return null;
            foreach (string part in uri.Query.TrimStart('?').Split('&'))
            {
                string[] pair = part.Split(new[] { '=' }, 2);
                if (pair.Length == 2 && Uri.UnescapeDataString(pair[0]) == "openid.oa2.authorization_code")
                    return Uri.UnescapeDataString(pair[1].Replace("+", " "));
            }

            return null;
        }
    }
}
