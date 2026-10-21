using System;
using System.IO;

namespace RoomPilot.Tests
{
    static partial class TestRunner
    {
        static void TestPrivateStorage()
        {
            string root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "storage-tests-" + Guid.NewGuid().ToString("N"));
            string source = Path.Combine(root, "legacy"), target = Path.Combine(root, "private");
            Directory.CreateDirectory(source);
            var config = Credentials();
            config.Devices[0].Name = "Nome privado de teste";
            config.KeyboardCommands[1].KeyCode = (int)System.Windows.Forms.Keys.L;
            File.WriteAllText(Path.Combine(source, "config.json"), Json.Serializer().Serialize(config));
            var session = new AlexaSession
            {
                Domain = "amazon.com.br",
                RefreshToken = "synthetic-refresh",
                Cookie = "synthetic-cookie",
                Csrf = "synthetic-csrf",
                CookieDate = DateTime.UtcNow
            };
            new AlexaSessionStore(source).Save(session);
            Check(PrivateStorage.ImportLegacy(source, target), "migração importa sessão e configuração antigas");
            var store = new ConfigStore(target);
            var loaded = store.Load();
            Check(Json.Serializer().Serialize(loaded) == Json.Serializer().Serialize(config), "migração preserva nomes, identidades e atalhos");
            Check(Json.Serializer().Serialize(new AlexaSessionStore(target).Load()) == Json.Serializer().Serialize(session), "migração preserva toda a sessão Amazon");
            string protectedSettings = File.ReadAllText(store.FilePath), protectedSession = File.ReadAllText(Path.Combine(target, "amazon-session.dat"));
            Check(!protectedSettings.Contains("Nome privado") && !protectedSettings.Contains("lamparina-test") && !protectedSession.Contains("synthetic-refresh"), "arquivos privados são protegidos com DPAPI");
            Check(File.Exists(Path.Combine(source, "config.json")) && new AlexaSessionStore(source).Exists, "migração preserva originais para conferência");
            Check(!PrivateStorage.ImportLegacy(source, target) && File.ReadAllText(store.FilePath) == protectedSettings, "migração nunca sobrescreve uma conta já configurada");
            Check(Directory.GetFiles(target, "*.tmp").Length == 0, "gravação atômica não deixa arquivos temporários");
            File.WriteAllText(store.FilePath, "corrupted-test-data");
            bool rejected = false;
            try
            {
                store.Load();
            }
            catch (UserError ex)
            {
                rejected = !ex.Message.Contains("corrupted-test-data");
            }

            Check(rejected && File.ReadAllText(store.FilePath) == "corrupted-test-data", "configuração inválida é preservada sem expor conteúdo no erro");
            File.WriteAllText(Path.Combine(source, "config.json"), "{\"Schema\":99}");
            string invalidTarget = Path.Combine(root, "invalid");
            rejected = false;
            try
            {
                PrivateStorage.ImportLegacy(source, invalidTarget);
            }
            catch
            {
                rejected = true;
            }

            Check(rejected && !Directory.Exists(invalidTarget), "migração inválida não grava dados parciais");
            Check(!PrivateStorage.ImportLegacy(Path.Combine(root, "missing"), Path.Combine(root, "new")), "instalação nova inicia sem importar dados");
            Check(Path.GetFullPath(PrivateStorage.DefaultDirectory).StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), "dados padrão ficam na pasta privada do Windows");
        }
    }
}
