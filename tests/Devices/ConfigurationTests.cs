using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace RoomPilot.Tests
{
    static partial class TestRunner
    {
        static void Reject(Action action, string description)
        {
            bool rejected = false;
            try
            {
                action();
            }
            catch (UserError)
            {
                rejected = true;
            }

            Check(rejected, description);
        }

        static void TestConfiguration()
        {
            var config = Credentials();
            var store = new ConfigStore(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config-tests"));
            for (int i = 0; i < 47; i++)
                DeviceCatalog.Add(config, "Aparelho " + i);
            store.Save(config);
            Check(store.Load().Devices.Count == 50, "configuração aceita cinquenta aparelhos sem nomes ou quantidades fixas");
            Check(config.Devices.Select(device => device.Key).Distinct().Count() == 50, "cada novo controle recebe uma identidade estável e única");
            var renamed = config.Devices[0];
            string key = renamed.Key, id = renamed.DeviceId;
            DeviceCatalog.Rename(renamed, "Luz do escritório");
            Check(renamed.Key == key && renamed.DeviceId == id && config.KeyboardCommands[1].DeviceKey == key, "renomear preserva vínculo Alexa e atalhos");
            config.KeyboardCommands.Add(new KeyboardCommand { Action = "on", DeviceKey = config.Devices.Last().Key, Modifiers = 3, KeyCode = (int)Keys.F12 });
            ConfigurationRules.Validate(config);
            DeviceCatalog.Remove(config, key);
            Check(config.Devices.Count == 49 && config.KeyboardCommands.All(command => command.DeviceKey != key), "remoção apaga somente o controle escolhido e seus atalhos");
            Check(config.KeyboardCommands.Any(command => command.DeviceKey == config.Devices.Last().Key), "remoção preserva atalhos de outros aparelhos");
            Reject(() => DeviceCatalog.Add(config, " "), "nome vazio não cria aparelho");
            config.Devices.Add(new DeviceConfig { Key = config.Devices[0].Key, Name = "Repetido" });
            Reject(() => ConfigurationRules.Validate(config), "identificadores duplicados são rejeitados");
            config.Devices.RemoveAt(config.Devices.Count - 1);
            config.Devices[1].DeviceId = config.Devices[0].DeviceId;
            Reject(() => ConfigurationRules.Validate(config), "o mesmo aparelho Alexa não recebe dois controles");
            var empty = new AppConfig();
            empty.Devices.Clear();
            empty.KeyboardCommands.Clear();
            store.Save(empty);
            Check(store.Load().Devices.Count == 0, "lista vazia pode ser salva e reaberta");
            var legacy = Credentials();
            legacy.Schema = 1;
            string oldJson = Json.Serializer().Serialize(legacy);
            string oldFile = DataProtection.Protect(oldJson);
            File.WriteAllText(store.FilePath, oldFile);
            var upgraded = store.Load();
            Check(upgraded.Schema == AppConfig.CurrentSchema && upgraded.Devices[0].DeviceId == legacy.Devices[0].DeviceId, "formato antigo é convertido preservando os aparelhos");
            Check(Json.Serializer().Serialize(upgraded.KeyboardCommands) == Json.Serializer().Serialize(legacy.KeyboardCommands), "conversão preserva atalhos personalizados");
            Check(File.ReadAllText(store.FilePath) == oldFile, "consultar configuração antiga não sobrescreve o arquivo original");
            store.Save(upgraded);
            Check(!File.ReadAllText(store.FilePath).Contains("lamparina-test"), "identidades continuam protegidas depois da atualização");
            upgraded.Schema = 99;
            Reject(() => store.Save(upgraded), "versão futura não é sobrescrita por engano");
            Check(DeviceActions.Parse("off") == PowerAction.Off, "comandos externos são convertidos para ações tipadas");
            Reject(() => DeviceActions.Parse("invalid"), "ação desconhecida não vira desligamento implícito");
        }

        static void TestDynamicForms()
        {
            var initial = Credentials();
            var store = new ConfigStore(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "form-tests-" + Guid.NewGuid().ToString("N")));
            using (var form = new SettingsForm(store, initial))
            {
                for (int i = 0; i < 17; i++)
                    form.AddDevice("Extra " + i);
                Check(initial.Devices.Count == 3 && !File.Exists(store.FilePath), "editar o cadastro não altera a configuração antes de salvar");
                form.RemoveDevice("lamparina");
                var saved = form.SaveDraft();
                Check(saved.Devices.Count == 19 && store.Load().Devices.Count == 19, "tela adiciona e remove aparelhos e persiste a lista dinâmica");
                Check(saved.KeyboardCommands.All(command => command.Action == "panel" || saved.Devices.Any(device => device.Key == command.DeviceKey)), "tela de cadastro não deixa atalhos para aparelhos removidos");
                var scrolling = Descendants(form).OfType<FlowLayoutPanel>().First(panel => panel.Controls.OfType<DeviceEditorRow>().Any());
                Check(scrolling.AutoScroll && scrolling.Controls.Count == 19, "cadastro organiza muitos aparelhos em uma lista com rolagem");
                using (var main = new MainForm(store, saved, true))
                {
                    Check(Descendants(main).OfType<DeviceCard>().Count() == 19, "painel cria um cartão para cada aparelho cadastrado");
                }
            }

            using (var form = new SettingsForm(store, initial))
            {
                form.DisplayDevices(new System.Collections.Generic.List<AlexaEndpoint>());
                Check(form.SaveDraft().Devices[0].DeviceId == initial.Devices[0].DeviceId, "consulta incompleta não apaga vínculos previamente salvos");
            }

            var empty = new AppConfig();
            empty.Devices.Clear();
            empty.KeyboardCommands.RemoveAll(command => command.Action != "panel");
            using (var form = new SettingsForm(store, empty))
            {
                Check(form.SaveDraft().Devices.Count == 0, "tela e atalhos de painel funcionam sem aparelhos");
            }

            SynchronizationContext.SetSynchronizationContext(null);
        }
    }
}
