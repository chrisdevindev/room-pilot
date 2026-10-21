using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace RoomPilot
{
    public sealed partial class AlexaClient
    {
        public async Task<AlexaSession> RegisterAsync(string code, string verifier, string clientId, string serial, IDictionary<string, string> cookies, string domain)
        {
            AmazonHosts.Require(domain);
            if (!String.Equals(clientId, AmazonLoginRequest.ClientIdFor(serial), StringComparison.Ordinal))
                throw new UserError(L10n.Text("error.authIdentity"));
            cookies = AmazonLoginCookies.Normalize(cookies);
            object frc;
            var copy = cookies.ToDictionary(k => k.Key, k => (object)k.Value);
            copy.TryGetValue("frc", out frc);
            var body = new
            {
                requested_extensions = new[]
                {
                    "device_info",
                    "customer_info"
                },
                cookies = new
                {
                    domain = ".amazon.com",
                    website_cookies = cookies.Select(c => new { Name = c.Key, Value = c.Value }).ToArray()
                },
                registration_data = new
                {
                    domain = "Device",
                    app_version = ApiVersion,
                    device_type = AmazonLoginRequest.DeviceType,
                    device_name = "%FIRST_NAME%'s%DUPE_STRATEGY_1ST%" + AppName,
                    os_version = "18.3.1",
                    device_serial = serial,
                    device_model = "iPhone",
                    app_name = AppName,
                    software_version = "1"
                },
                auth_data = new
                {
                    client_id = clientId,
                    authorization_code = code,
                    code_verifier = verifier,
                    code_algorithm = "SHA-256",
                    client_domain = "DeviceLegacy"
                },
                // The mobile registration protocol requests this token family together.
                // MAC signing keys are not persisted or used by our power-only connector.
                user_context_map = new
                {
                    frc = frc
                },
                requested_token_type = new[]
                {
                    "bearer",
                    "mac_dms",
                    "website_cookies"
                }
            };
            var result = await SendAsync("api.amazon.com", "/auth/register", "POST", body, String.Join("; ", cookies.Select(c => c.Key + "=" + c.Value)), null, false).ConfigureAwait(false);
            string refresh = Convert.ToString(At(result, "response", "success", "tokens", "bearer", "refresh_token"));
            if (String.IsNullOrEmpty(refresh))
                throw new UserError(L10n.Text("error.register"));
            session = new AlexaSession
            {
                Domain = domain,
                RefreshToken = refresh
            };
            await RefreshSessionAsync().ConfigureAwait(false);
            return session;
        }

        public async Task RefreshSessionAsync()
        {
            if (session == null || String.IsNullOrEmpty(session.RefreshToken))
                throw new AlexaLoginRequired();
            string domain = AmazonHosts.Require(session.Domain);
            var fields = new Dictionary<string, string>
            {
                {
                    "di.os.name",
                    "iOS"
                },
                {
                    "app_version",
                    ApiVersion
                },
                {
                    "domain",
                    "." + domain
                },
                {
                    "source_token",
                    session.RefreshToken
                },
                {
                    "requested_token_type",
                    "auth_cookies"
                },
                {
                    "source_token_type",
                    "refresh_token"
                },
                {
                    "di.hw.version",
                    "iPhone"
                },
                {
                    "di.sdk.version",
                    "6.12.4"
                },
                {
                    "app_name",
                    AppName
                },
                {
                    "di.os.version",
                    "16.6"
                }
            };
            var response = await SendAsync("www." + domain, "/ap/exchangetoken/cookies", "POST", fields, null, null, true).ConfigureAwait(false);
            var values = Json.Items(At(response, "response", "tokens", "cookies", "." + domain)).ToList();
            if (values.Count == 0)
                throw new AlexaLoginRequired();
            var jar = new CookieContainer();
            foreach (var cookie in values)
            {
                string name = Json.String(cookie, "Name"), value = Json.String(cookie, "Value");
                if (String.IsNullOrEmpty(name) || name.IndexOfAny(new[] { '\r', '\n', ';', '=' }) >= 0 || value.IndexOfAny(new[] { '\r', '\n', ';' }) >= 0)
                    continue;
                jar.Add(new Cookie(name, value, "/", "." + domain) { Secure = true });
            }

            string host = "alexa." + domain;
            // Obtain a CSRF cookie from Amazon itself; never invent one.
            foreach (string path in new[]
            {
                "/api/language",
                "/api/devices-v2/device?cached=false",
                "/api/strings"
            }

            )
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, "https://" + host + path))
                {
                    request.Headers.TryAddWithoutValidation("Cookie", jar.GetCookieHeader(new Uri("https://" + host)));
                    request.Headers.TryAddWithoutValidation("User-Agent", Agent);
                    HttpResponseMessage reply;
                    try
                    {
                        reply = await http.SendAsync(request).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        if (ex is HttpRequestException || ex is TaskCanceledException)
                            throw new UserError(L10n.Text("error.sessionFinish"));
                        throw;
                    }

                    using (reply)
                    {
                        IEnumerable<string> setCookies;
                        if (reply.Headers.TryGetValues("Set-Cookie", out setCookies))
                            foreach (string raw in setCookies)
                            {
                                try
                                {
                                    jar.SetCookies(new Uri("https://" + host), raw);
                                }
                                catch (CookieException)
                                {
                                }
                            }
                    }
                }

                var csrf = jar.GetCookies(new Uri("https://" + host))["csrf"];
                if (csrf != null && !String.IsNullOrEmpty(csrf.Value))
                {
                    session.Cookie = jar.GetCookieHeader(new Uri("https://" + host));
                    session.Csrf = csrf.Value;
                    session.CookieDate = DateTime.UtcNow;
                    return;
                }
            }

            throw new UserError(L10n.Text("error.sessionRegion"));
        }
    }
}
