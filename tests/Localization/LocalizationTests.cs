using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RoomPilot.Tests
{
    static partial class TestRunner
    {
        static void TestLocalization()
        {
            var portuguese = L10n.Catalog(L10n.Portuguese);
            var english = L10n.Catalog(L10n.English);
            Check(portuguese.Keys.OrderBy(key => key).SequenceEqual(english.Keys.OrderBy(key => key)), "os dois catálogos têm as mesmas chaves");
            Check(portuguese.Values.Concat(english.Values).All(value => !String.IsNullOrWhiteSpace(value)), "nenhuma tradução está vazia");
            bool validFormats = true;
            foreach (var key in portuguese.Keys)
            {
                var first = Regex.Matches(portuguese[key], "\\{[0-9]+\\}").Cast<Match>().Select(match => match.Value).OrderBy(value => value);
                var second = Regex.Matches(english[key], "\\{[0-9]+\\}").Cast<Match>().Select(match => match.Value).OrderBy(value => value);
                validFormats &= first.SequenceEqual(second);
                try
                {
                    String.Format(CultureInfo.InvariantCulture, portuguese[key], "a", "b", "c");
                    String.Format(CultureInfo.InvariantCulture, english[key], "a", "b", "c");
                }
                catch (FormatException)
                {
                    validFormats = false;
                }
            }

            Check(validFormats, "parâmetros das traduções são válidos e equivalentes");
            Check(L10n.DetectLanguage("pt-PT") == L10n.Portuguese && L10n.DetectLanguage("en-GB") == L10n.English && L10n.DetectLanguage("ja-JP") == L10n.English, "idioma inicial usa português ou o fallback inglês");
            try
            {
                L10n.SetLanguage(L10n.English);
                Check(DeviceActions.Label(PowerAction.On) == "Turn on" && new DeviceState().Message == "Connection pending", "ações e estados aparecem em inglês");
                Check(Task.Run(() => L10n.Text("action.save")).GetAwaiter().GetResult() == "Save", "tarefas em segundo plano usam o idioma selecionado");
                Check(L10n.Format("main.confirmedCount", 1.5, 2).Contains("1.5"), "formatação usa a cultura inglesa selecionada");
                var defaults = new AppConfig();
                Check(defaults.Devices.Select(device => device.Key).SequenceEqual(new[] { "lamparina", "tomada", "fita" }) && defaults.KeyboardCommands[2].DeviceKey == "tomada", "idioma não traduz identificadores de aparelhos ou atalhos");
                Check(defaults.Devices[0].Name == "Light" && defaults.Language == L10n.English, "instalação nova cria nomes iniciais no idioma atual");
                Check(new AmazonLoginRequest().Url.Contains("language=en_US"), "página Amazon solicita o idioma inglês sem alterar os parâmetros de autorização");
                var session = new AlexaSession
                {
                    Domain = "amazon.com.br",
                    Cookie = "synthetic-cookie",
                    Csrf = "synthetic-csrf",
                    RefreshToken = "synthetic-refresh"
                };
                string sessionJson = Json.Serializer().Serialize(session);
                var diagnostic = AlexaDiagnostic.Create("/auth/register", 400, "{\"error\":\"InvalidValue\"}");
                Check(diagnostic.Step == "register" && diagnostic.UserMessage().Contains("application registration") && diagnostic.UserMessage().Contains("HTTP 400"), "diagnóstico traduz a explicação e preserva códigos de protocolo");
                Reject(() => DeviceCatalog.ValidName(""), "erros de validação continuam bloqueando nomes inválidos em inglês");
                try
                {
                    DeviceCatalog.ValidName("");
                }
                catch (UserError error)
                {
                    Check(error.Message.StartsWith("Device names"), "mensagens de erro aparecem em inglês");
                }

                string directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "language-tests-" + Guid.NewGuid().ToString("N"));
                var store = new ConfigStore(directory);
                defaults.Devices[0].Name = "Minha luz {0}";
                using (var form = new SettingsForm(store, defaults))
                {
                    var tabs = form.Controls.OfType<TabControl>().Single();
                    Check(tabs.TabPages[1].Text == "Devices" && tabs.TabPages[3].Text == "Preferences", "abas de configuração aparecem em inglês");
                    var selector = Descendants(form).OfType<ComboBox>().Single(combo => combo.AccessibleName == "Language");
                    selector.SelectedIndex = 0;
                    var saved = form.SaveDraft();
                    Check(store.Load().Language == L10n.Portuguese && saved.Devices[0].Name == "Minha luz {0}", "idioma é salvo sem traduzir nomes personalizados");
                }

                using (var main = new MainForm(store, defaults, true))
                {
                    defaults.Language = L10n.Portuguese;
                    main.ApplyLanguage();
                    Check(Descendants(main).OfType<Button>().Any(button => button.Text == "Configurar") && Descendants(main).OfType<Button>().Count(button => button.Text == "Ligar") == 3, "troca de idioma recria botões do painel em português");
                    defaults.Language = L10n.English;
                    main.ApplyLanguage();
                    Check(Descendants(main).OfType<Button>().Any(button => button.Text == "Settings") && Descendants(main).OfType<Button>().Count(button => button.Text == "Turn on") == 3, "troca para inglês atualiza também os cartões dos aparelhos");
                }

                using (var login = new AlexaLoginForm(directory, "amazon.com.br"))
                    Check(login.Text == "Sign in to Amazon · room-pilot", "janela de login usa idioma e marca atuais sem abrir conexão");
                var old = Credentials();
                old.Schema = 2;
                old.Language = null;
                var migrated = ConfigurationRules.Upgrade(old);
                Check(migrated.Language == L10n.Portuguese && migrated.Schema == AppConfig.CurrentSchema, "configuração anterior mantém português durante a atualização");
                Check(Json.Serializer().Serialize(session) == sessionJson, "trocar idioma não altera a sessão Amazon");
                Reject(() => L10n.SetLanguage("unsupported"), "idioma sem catálogo é rejeitado");
                L10n.SetLanguage(L10n.Portuguese);
                Check(L10n.Text("action.save") == "Salvar" && L10n.Format("main.confirmedCount", 1.5, 2).Contains("1,5"), "português restaura textos e formatação numérica");
            }
            finally
            {
                L10n.SetLanguage(L10n.Portuguese);
            }
        }
    }
}
