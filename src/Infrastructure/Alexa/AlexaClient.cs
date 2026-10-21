using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace RoomPilot
{
    public sealed partial class AlexaClient : IDisposable
    {
        public const string AppName = "room-pilot";
        // Amazon's mobile registration uses this protocol version and device class.
        public const string ApiVersion = "2.2.651540.0";
        const string Agent = "AmazonWebView/Amazon Alexa/2.2.651540.0/iOS/18.3.1/iPhone";
        readonly HttpClient http;
        readonly AlexaSessionStore store;
        AlexaSession session;
        public Action<AlexaDiagnostic> Diagnostic { get; set; }
        public Action<string> Progress { get; set; }

        public AlexaClient(AlexaSession initial, AlexaSessionStore sessionStore) : this(initial, sessionStore, null)
        {
        }

        public AlexaClient(AlexaSession initial, AlexaSessionStore sessionStore, HttpMessageHandler handler)
        {
            session = initial;
            store = sessionStore;
            http = handler == null ? new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false, AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate }) : new HttpClient(handler, false);
            http.Timeout = TimeSpan.FromSeconds(30);
        }

        public bool Connected
        {
            get
            {
                return session != null && AmazonHosts.Allowed(session.Domain) && !String.IsNullOrEmpty(session.Cookie) && !String.IsNullOrEmpty(session.Csrf);
            }
        }

        static object At(object obj, params string[] path)
        {
            foreach (string key in path)
            {
                var dict = obj as IDictionary<string, object>;
                if (dict == null)
                    return null;
                obj = Json.Get(dict, key);
            }

            return obj;
        }

        async Task<IDictionary<string, object>> SendAsync(string host, string path, string method, object data, string cookie, string csrf, bool form)
        {
            // Host/path are chosen by our code, never supplied by a server response.
            if (!AmazonHosts.LoginUrl(new Uri("https://" + host)))
                throw new UserError(L10n.Text("error.amazonDestination"));
            using (var request = new HttpRequestMessage(new HttpMethod(method), "https://" + host + path))
            {
                request.Headers.TryAddWithoutValidation("User-Agent", Agent);
                request.Headers.TryAddWithoutValidation("Accept", "application/json");
                request.Headers.TryAddWithoutValidation("Accept-Language", L10n.CurrentLanguage + "," + L10n.CurrentLanguage.Substring(0, 2) + ";q=0.9");
                request.Headers.TryAddWithoutValidation("Accept-Charset", "utf-8");
                if (!String.IsNullOrEmpty(cookie))
                    request.Headers.TryAddWithoutValidation("Cookie", cookie);
                if (!String.IsNullOrEmpty(csrf))
                    request.Headers.TryAddWithoutValidation("csrf", csrf);
                if (host.StartsWith("alexa."))
                {
                    request.Headers.TryAddWithoutValidation("Origin", "https://" + host);
                    request.Headers.Referrer = new Uri("https://" + host + "/spa/index.html");
                }
                else
                    request.Headers.TryAddWithoutValidation("x-amzn-identity-auth-domain", "api." + host.Substring(host.IndexOf('.') + 1));
                if (data != null)
                    request.Content = form ? (HttpContent)new FormUrlEncodedContent((IEnumerable<KeyValuePair<string, string>>)data) : new StringContent(Json.Serializer().Serialize(data), Encoding.UTF8, "application/json");
                if (request.Content != null && !form)
                    request.Content.Headers.ContentType.CharSet = null;
                if (Progress != null)
                    Progress(AlexaDiagnostic.StepFor(path));
                HttpResponseMessage response;
                try
                {
                    response = await http.SendAsync(request).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    throw new UserError(L10n.Text("error.amazonTimeout"));
                }
                catch (HttpRequestException)
                {
                    throw new UserError(L10n.Text("error.amazonConnection"));
                }

                using (response)
                {
                    string body = response.Content == null ? "" : await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (body.Length > 2097152)
                        throw new UserError(L10n.Text("error.amazonSize"));
                    var diagnostic = AlexaDiagnostic.Create(path, (int)response.StatusCode, response.IsSuccessStatusCode ? null : body);
                    if (Diagnostic != null)
                        Diagnostic(diagnostic);
                    if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                        throw new AlexaLoginRequired();
                    if (!response.IsSuccessStatusCode)
                        throw new UserError(diagnostic.UserMessage());
                    try
                    {
                        return Json.Object(Json.Serializer().DeserializeObject(body));
                    }
                    catch
                    {
                        throw new UserError(L10n.Text("error.amazonResponse"));
                    }
                }
            }
        }

        async Task<IDictionary<string, object>> SendAuthenticatedAsync(string path, string method, object body)
        {
            if (session == null)
                throw new AlexaLoginRequired();
            bool refreshed = false;
            if (!Connected || DateTime.UtcNow - session.CookieDate > TimeSpan.FromDays(5))
            {
                await RefreshSessionAsync().ConfigureAwait(false);
                refreshed = true;
            }

            try
            {
                var result = await SendAsync("alexa." + session.Domain, path, method, body, session.Cookie, session.Csrf, false).ConfigureAwait(false);
                if (store != null && refreshed)
                    store.Save(session);
                return result;
            }
            catch (AlexaLoginRequired)
            {
                if (refreshed)
                    throw;
            }

            await RefreshSessionAsync().ConfigureAwait(false);
            var retried = await SendAsync("alexa." + session.Domain, path, method, body, session.Cookie, session.Csrf, false).ConfigureAwait(false);
            if (store != null)
                store.Save(session);
            return retried;
        }

        public void Dispose()
        {
            http.Dispose();
        }
    }
}
