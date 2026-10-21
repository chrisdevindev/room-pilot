using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RoomPilot.Tests
{
    static partial class TestRunner
    {
        static async Task TestAlexa()
        {
            Check(!AmazonHosts.LoginUrl(new Uri("http://www.amazon.com")) && !AmazonHosts.LoginUrl(new Uri("https://www.amazon.com.evil.example")) && !AmazonHosts.LoginUrl(new Uri("https://www.amazon.com:8443")), "login bloqueia HTTP, domínio parecido e porta externa");
            Check(AmazonHosts.LoginUrl(new Uri("https://www.amazon.com.br/ap/signin")), "login aceita o domínio Amazon Brasil");
            Check(AmazonLoginRequest.AuthorizationCode(new Uri("https://www.amazon.com/ap/maplanding?openid.oa2.authorization_code=example-code")) == "example-code" && AmazonLoginRequest.AuthorizationCode(new Uri("https://www.amazon.com/other?openid.oa2.authorization_code=example-code")) == null, "código de autorização aceito apenas no retorno esperado da Amazon");
            var login = new AmazonLoginRequest();
            var second = new AmazonLoginRequest();
            Check(login.Verifier != second.Verifier && login.ClientId != second.ClientId && login.Url.Contains("code_challenge_method=S256") && !login.Url.Contains(login.Verifier), "autorização usa PKCE e identificadores aleatórios sem expor o verificador na URL");
            Check(AmazonLoginRequest.ClientIdFor("0123456789ABCDEF0123456789ABCDEF") == "303132333435363738394142434445463031323334353637383941424344454623413249564c5635564d32573831", "vetor do identificador de autorização inclui o serial e o tipo de dispositivo");
            string decoded = Encoding.ASCII.GetString(Enumerable.Range(0, login.ClientId.Length / 2).Select(n => Convert.ToByte(login.ClientId.Substring(n * 2, 2), 16)).ToArray());
            Check(decoded == login.Serial + "#" + AmazonLoginRequest.DeviceType, "login e registro usam o mesmo serial de dispositivo");
            var retryLogin = new AmazonLoginRequest(login.Serial);
            Check(retryLogin.ClientId == login.ClientId && retryLogin.Verifier != login.Verifier, "recomeçar login renova PKCE e preserva a identidade do aplicativo");
            Check(login.Url.Contains("device_auth_access%20offline_access"), "autorização solicita explicitamente a sessão renovável");
            var keyboard = new AppConfig();
            KeyboardCommand.Validate(keyboard.KeyboardCommands, keyboard);
            Check(keyboard.KeyboardCommands.Count == 4 && keyboard.KeyboardCommands[1].Display == "Ctrl + Alt + 1", "atalhos iniciais incluem painel e três aparelhos");
            bool rejected = false;
            try
            {
                KeyboardCommand.Validate(new[] { new KeyboardCommand { Action = "on", DeviceKey = "tomada", KeyCode = (int)System.Windows.Forms.Keys.A } }, keyboard);
            }
            catch (UserError)
            {
                rejected = true;
            }

            Check(rejected, "atalho sem modificador não intercepta a digitação comum");
            rejected = false;
            try
            {
                KeyboardCommand.Validate(new[] { keyboard.KeyboardCommands[0], keyboard.KeyboardCommands[0] }, keyboard);
            }
            catch (UserError)
            {
                rejected = true;
            }

            Check(rejected, "atalhos duplicados são rejeitados antes de registrar");
            var all = Decode("[{\"friendlyName\":\"Tomada\",\"features\":[],\"legacyAppliance\":{\"entityId\":\"plug-test\",\"actions\":[\"turnOn\",\"turnOff\"]}},{\"friendlyName\":\"Echo\",\"legacyAppliance\":{\"entityId\":\"echo-test\",\"actions\":[\"volume\"]}},{\"friendlyName\":\"Tomada\",\"features\":[{\"name\":\"Alexa.PowerController\"}],\"legacyAppliance\":{\"entityId\":\"other-plug\"}}]");
            foreach (var item in Json.Items(all))
            {
                var legacy = Json.Object(Json.Get(item, "legacyAppliance"));
                legacy["applianceId"] = Json.String(legacy, "entityId");
                legacy["entityId"] = "entity-" + Json.String(legacy, "entityId");
            }

            var endpoints = AlexaClient.ParseEndpoints(all);
            Check(endpoints.Count == 2 && endpoints.Select(d => d.Name).Distinct().Count() == 2, "lista filtra aparelhos de energia e distingue nomes repetidos");
            var currentSchema = Decode("[{\"friendlyName\":\"Lâmpada \",\"features\":[{\"name\":\"power\",\"operations\":[{\"name\":\"turnOn\"},{\"name\":\"turnOff\"}]}],\"legacyAppliance\":{\"entityId\":\"lamp-current\",\"actions\":[]}},{\"friendlyName\":\"Tomada\",\"features\":[{\"name\":\"power\",\"operations\":[{\"name\":\"turnOn\"},{\"name\":\"turnOff\"}]}],\"legacyAppliance\":{\"entityId\":\"plug-current\",\"actions\":[]}},{\"friendlyName\":\"Fita Led\",\"features\":[{\"name\":\"power\",\"operations\":[{\"name\":\"turnOn\"},{\"name\":\"turnOff\"}]}],\"legacyAppliance\":{\"entityId\":\"strip-current\",\"actions\":[]}},{\"friendlyName\":\"Echo\",\"features\":[{\"name\":\"speaker\",\"operations\":[]}],\"legacyAppliance\":{\"entityId\":\"echo-current\",\"actions\":[]}},{\"friendlyName\":\"Cena\",\"features\":[{\"name\":\"power\",\"operations\":[{\"name\":\"turnOn\"}]}],\"legacyAppliance\":{\"entityId\":\"scene-current\",\"actions\":[]}}]");
            foreach (var item in Json.Items(currentSchema))
            {
                var legacy = Json.Object(Json.Get(item, "legacyAppliance"));
                legacy["applianceId"] = Json.String(legacy, "entityId");
                legacy["entityId"] = "entity-" + Json.String(legacy, "entityId");
            }

            var currentDevices = AlexaClient.ParseEndpoints(currentSchema);
            Check(currentDevices.Count == 3, "formato real da Alexa com função power e operações reconhece os três aparelhos");
            Check(currentDevices.Any(d => d.Name == "Lâmpada"), "nome retornado pela Alexa tem espaços de borda removidos");
            Check(currentDevices.All(d => !d.Id.StartsWith("entity-")), "controle APPLIANCE usa applianceId mesmo quando entityId é diferente");
            Check(DeviceMatching.Suggest(new AppConfig().Devices[0], currentDevices).Id == "lamp-current", "lamparina recebe sugestão da entidade Lâmpada cadastrada na Alexa");
            Check(DeviceMatching.Suggest(new AppConfig().Devices[2], currentDevices).Id == "strip-current", "sugestão da fita ignora diferenças entre maiúsculas e minúsculas");
            Check(DeviceMatching.Suggest(new AppConfig().Devices[1], new[] { new AlexaEndpoint { Id = "a", Name = "Tomada" }, new AlexaEndpoint { Id = "b", Name = "Tomada" } }) == null, "sugestões por nome não escolhem entre aparelhos ambíguos");
            var state = Json.Object(Decode("{\"deviceStates\":[{\"entity\":{\"entityId\":\"plug-test\"},\"capabilityStates\":[\"{\\\"namespace\\\":\\\"Alexa.PowerController\\\",\\\"name\\\":\\\"powerState\\\",\\\"value\\\":\\\"OFF\\\"}\"]}],\"errors\":[]}"));
            Check(!AlexaClient.ParsePower(state, "plug-test"), "estado Alexa em JSON interno é lido como desligado");
            rejected = false;
            try
            {
                AlexaClient.ParsePower(state, "other-plug");
            }
            catch (UserError)
            {
                rejected = true;
            }

            Check(rejected, "estado de outro aparelho nunca é aplicado ao selecionado");
            var session = new AlexaSession
            {
                Domain = "amazon.com.br",
                Cookie = "at-main=example-cookie; csrf=example-csrf",
                Csrf = "example-csrf",
                RefreshToken = "example-refresh",
                CookieDate = DateTime.UtcNow
            };
            var sessions = new AlexaSessionStore(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "alexa-test-data"));
            sessions.Save(session);
            Check(sessions.Load().RefreshToken == session.RefreshToken && !File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "alexa-test-data", "amazon-session.dat")).Contains("example-refresh"), "sessão Amazon é salva com proteção DPAPI, sem token em texto aberto");
            using (var handler = new AlexaHandler())
            using (var client = new AlexaClient(session, null, handler))
            {
                var devices = await client.ListDevicesAsync();
                Check(devices.Count == 1 && devices[0].Id == "plug-test", "listagem por GraphQL retorna a identidade de controle da Alexa");
                Check(!await client.ReadPowerAsync("plug-test"), "consulta de energia usa a identidade escolhida");
                await client.SetPowerAsync("plug-test", true);
                Check(handler.Commands == 1 && handler.LastAction == "turnOn", "ligar envia apenas uma ação turnOn para o aparelho selecionado");
                handler.RejectCommand = true;
                rejected = false;
                try
                {
                    await client.SetPowerAsync("plug-test", false);
                }
                catch (UserError)
                {
                    rejected = true;
                }

                Check(rejected && handler.Commands == 2, "recusa da Alexa não aparece como comando concluído");
                handler.RejectCommand = false;
                handler.FailAuthentication = true;
                await client.ListDevicesAsync();
                Check(handler.Exchanges == 1, "sessão expirada é renovada uma vez após recusa da autenticação");
                Check(handler.AmazonOnly && handler.CredentialsCorrect, "requisições usam HTTPS apenas para Amazon e incluem a sessão esperada");
                handler.Offline = true;
                rejected = false;
                try
                {
                    await client.ReadPowerAsync("plug-test");
                }
                catch (UserError ex)
                {
                    rejected = ex.Message.Contains("offline");
                }

                Check(rejected, "aparelho inacessível na Alexa preserva estado desconhecido");
            }

            using (var handler = new AlexaHandler())
            using (var client = new AlexaClient(null, null, handler))
            {
                var registered = await client.RegisterAsync("example-code", login.Verifier, login.ClientId, login.Serial, new Dictionary<string, string> { { "frc", "example-frc" } }, "amazon.com.br");
                Check(registered.Csrf == "renewed-csrf" && handler.Registrations == 1 && handler.Exchanges == 1, "autorização registra o cliente e obtém cookies e CSRF da Amazon");
                Check(handler.RegistrationBody.Contains(login.Verifier) && !handler.RegistrationBody.Contains("password"), "registro envia código e verificador, sem campo de senha");
                var sent = Json.Object(Decode(handler.RegistrationBody));
                var identity = Json.Object(Json.Get(sent, "registration_data"));
                var auth = Json.Object(Json.Get(sent, "auth_data"));
                Check(AmazonLoginRequest.ClientIdFor(Json.String(identity, "device_serial")) == Json.String(auth, "client_id"), "requisição enviada à Amazon mantém a identidade autorizada");
                Check(handler.RegistrationBody.Contains("mac_dms"), "registro solicita a família de tokens esperada pelo protocolo móvel");
            }

            using (var handler = new AlexaHandler
            {
                NestedControl = true
            }

            )
            using (var client = new AlexaClient(session, null, handler))
            {
                await client.SetPowerAsync("plug-test", true);
                Check(handler.Commands == 1, "confirmação no formato antigo com entity aninhado continua aceita");
            }

            using (var handler = new AlexaHandler
            {
                WrongControlTarget = true
            }

            )
            using (var client = new AlexaClient(session, null, handler))
            {
                rejected = false;
                try
                {
                    await client.SetPowerAsync("plug-test", true);
                }
                catch (UserError)
                {
                    rejected = true;
                }

                Check(rejected, "confirmação de outro aparelho não é aceita como sucesso do comando");
            }

            using (var form = new SettingsForm(new ConfigStore(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "alexa-form-test")), new AppConfig()))
            {
                var tabs = form.Controls.OfType<System.Windows.Forms.TabControl>().Single();
                var combo = tabs.TabPages[0].Controls.OfType<System.Windows.Forms.ComboBox>().Single();
                Check(Convert.ToString(combo.SelectedItem) == "amazon.com.br" && combo.ForeColor != combo.BackColor, "região inicial aparece legível no formulário");
                form.DisplayDevices(currentDevices);
                Check(tabs.SelectedIndex == 1, "lista recebida abre automaticamente a aba Aparelhos");
                var selected = Descendants(tabs.TabPages[1]).OfType<System.Windows.Forms.ComboBox>().Select(c => (AlexaEndpoint)c.SelectedItem).ToList();
                Check(selected.Count == 3 && selected.All(d => !String.IsNullOrEmpty(d.Id)), "três sugestões aparecem na configuração antes de salvar");
                Check(form.SavedConfig == null, "receber sugestões não salva escolhas nem aciona aparelhos");
            }

            sessions.Delete();
            Check(!sessions.Exists, "sair deste PC remove a sessão local");
            SynchronizationContext.SetSynchronizationContext(null);
            var normalized = AmazonLoginCookies.Normalize(new Dictionary<string, string> { { "x-main", "\"abc%2Bdef%3D\"" }, { "session-token", "value+with+plus" }, { "frc", "example-frc" }, { "advertising-id", "ignored" }, { "at-main", "unsafe%0D%0Avalue" } });
            Check(normalized["x-main"] == "abc+def=" && normalized["session-token"] == "value+with+plus", "cookies para registro removem aspas e decodificam percentuais preservando o sinal de mais");
            Check(!normalized.ContainsKey("advertising-id") && !normalized.ContainsKey("at-main"), "registro filtra cookies alheios ao login e valores com quebra de linha");
            string sensitive = "{\"response\":{\"error\":{\"code\":\"InvalidToken\",\"message\":\"secret-example-value\"}},\"request_id\":\"private-example-id\"}";
            var diagnostic = AlexaDiagnostic.Create("/auth/register", 400, sensitive);
            string safe = Json.Serializer().Serialize(diagnostic);
            Check(diagnostic.Step == "register" && diagnostic.ErrorCode == "InvalidToken" && !safe.Contains("secret-example-value") && !safe.Contains("private-example-id"), "diagnóstico de HTTP 400 identifica a etapa e preserva somente o código seguro");
            Check(AlexaDiagnostic.ParseCode("{\"error\":\"unknown-private-value\"}") == "" && AlexaDiagnostic.ParseCode("<html>error</html>") == "", "diagnóstico descarta códigos desconhecidos e páginas de erro");
            foreach (string path in new[]
            {
                "/auth/register",
                "/ap/exchangetoken/cookies",
                "/nexus/v1/graphql"
            }

            )
            {
                using (var handler = new AlexaHandler
                {
                    FailurePath = path
                }

                )
                using (var client = new AlexaClient(session, null, handler))
                {
                    AlexaDiagnostic observed = null;
                    client.Diagnostic = d => observed = d;
                    rejected = false;
                    try
                    {
                        if (path == "/auth/register")
                            await client.RegisterAsync("example-code", login.Verifier, login.ClientId, login.Serial, new Dictionary<string, string> { { "x-main", "\"encoded%2Bcookie\"" } }, "amazon.com.br");
                        else if (path == "/ap/exchangetoken/cookies")
                            await client.RefreshSessionAsync();
                        else
                            await client.ListDevicesAsync();
                    }
                    catch (UserError ex)
                    {
                        rejected = ex.Message.Contains("HTTP 400") && ex.Message.Contains(AlexaDiagnostic.Label(AlexaDiagnostic.StepFor(path))) && !ex.Message.Contains("private-response-content");
                    }

                    Check(rejected && observed != null && observed.HttpStatus == 400 && observed.Step == AlexaDiagnostic.StepFor(path), "HTTP 400 tratado na etapa " + AlexaDiagnostic.StepFor(path));
                    Check(handler.FailureCalls == 1 && handler.Commands == 0, "falha em " + AlexaDiagnostic.StepFor(path) + " não repete autorização nem aciona aparelho");
                    if (path == "/auth/register")
                        Check(handler.RegistrationBody.Contains("encoded+cookie") && !handler.RegistrationBody.Contains("encoded%2Bcookie"), "requisição de registro recebe o cookie normalizado");
                }
            }

            using (var handler = new AlexaHandler())
            using (var client = new AlexaClient(null, null, handler))
            {
                rejected = false;
                try
                {
                    await client.RegisterAsync("example-code", login.Verifier, login.ClientId, second.Serial, new Dictionary<string, string>(), "amazon.com.br");
                }
                catch (UserError)
                {
                    rejected = true;
                }

                Check(rejected && handler.Registrations == 0 && handler.Commands == 0, "identidade divergente é bloqueada antes de enviar o registro");
            }

            Check(AlexaDiagnostic.ParseCode("{\"response\":{\"error\":{\"code\":\"InvalidValue\",\"index\":\"private-index\",\"message\":\"private-message\"}}}") == "InvalidValue", "diagnóstico reconhece InvalidValue sem expor os detalhes da resposta");
        }

        sealed class AlexaHandler : HttpMessageHandler
        {
            public int Commands, Exchanges, Registrations;
            public bool RejectCommand, FailAuthentication, Offline;
            public bool NestedControl, WrongControlTarget;
            public bool AmazonOnly = true, CredentialsCorrect = true;
            public string LastAction, RegistrationBody;
            public string FailurePath;
            public int FailureCalls;
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                AmazonOnly &= request.RequestUri.Scheme == "https" && AmazonHosts.LoginUrl(request.RequestUri);
                string body = request.Content == null ? "" : await request.Content.ReadAsStringAsync();
                string path = request.RequestUri.AbsolutePath;
                if (path == "/auth/register")
                    RegistrationBody = body;
                if (path == FailurePath)
                {
                    FailureCalls++;
                    return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent("{\"response\":{\"error\":{\"code\":\"InvalidParameterValue\",\"message\":\"private-response-content\"}}}")
                    };
                }

                string json = "{}";
                if (path == "/auth/register")
                {
                    Registrations++;
                    RegistrationBody = body;
                    json = "{\"response\":{\"success\":{\"tokens\":{\"bearer\":{\"refresh_token\":\"example-refresh\"}}}}}";
                }
                else if (path == "/ap/exchangetoken/cookies")
                {
                    Exchanges++;
                    json = "{\"response\":{\"tokens\":{\"cookies\":{\".amazon.com.br\":[{\"Name\":\"at-main\",\"Value\":\"renewed-cookie\"}]}}}}";
                }
                else if (path == "/api/language")
                {
                    var csrf = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{}")
                    };
                    csrf.Headers.TryAddWithoutValidation("Set-Cookie", "csrf=renewed-csrf; Path=/; Secure");
                    return csrf;
                }
                else
                {
                    if (FailAuthentication)
                    {
                        FailAuthentication = false;
                        return new HttpResponseMessage(HttpStatusCode.Unauthorized);
                    }

                    CredentialsCorrect &= request.Headers.Contains("csrf") && request.Headers.Contains("Cookie");
                    if (path == "/nexus/v1/graphql")
                        json = "{\"data\":{\"endpoints\":{\"items\":[{\"friendlyName\":\"Tomada teste\",\"features\":[],\"legacyAppliance\":{\"applianceId\":\"plug-test\",\"entityId\":\"entity-plug-test\",\"actions\":[\"turnOn\",\"turnOff\"]}}]}}}";
                    else if (path == "/api/phoenix/state" && request.Method == HttpMethod.Put)
                    {
                        Commands++;
                        LastAction = body.Contains("turnOn") ? "turnOn" : "turnOff";
                        string confirmedId = WrongControlTarget ? "other-plug" : "plug-test";
                        string identity = NestedControl ? "\"entity\":{\"entityId\":\"" + confirmedId + "\"}" : "\"entityId\":\"" + confirmedId + "\"";
                        json = RejectCommand ? "{\"errors\":[{\"code\":\"FAILED\"}],\"controlResponses\":[]}" : "{\"errors\":[],\"controlResponses\":[{\"code\":\"SUCCESS\"," + identity + "}]}";
                    }
                    else if (path == "/api/phoenix/state")
                    {
                        json = Offline ? "{\"deviceStates\":[],\"errors\":[{\"code\":\"ENDPOINT_UNREACHABLE\"}]}" : "{\"deviceStates\":[{\"entity\":{\"entityId\":\"plug-test\"},\"capabilityStates\":[\"{\\\"namespace\\\":\\\"Alexa.PowerController\\\",\\\"name\\\":\\\"powerState\\\",\\\"value\\\":\\\"OFF\\\"}\"]}],\"errors\":[]}";
                    }
                    else
                        throw new Exception("Endpoint de teste inesperado: " + path);
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            }
        }
    }
}
