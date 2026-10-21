using System.IO;
using System.Text;

namespace RoomPilot
{
    public sealed class ConfigStore
    {
        public string DirectoryPath { get; private set; }

        public string FilePath
        {
            get
            {
                return Path.Combine(DirectoryPath, "settings.dat");
            }
        }

        public ConfigStore(string path)
        {
            DirectoryPath = Path.GetFullPath(path);
        }

        public AppConfig Load()
        {
            if (!File.Exists(FilePath))
                return new AppConfig();
            try
            {
                string json = DataProtection.Unprotect(File.ReadAllText(FilePath, Encoding.UTF8));
                var config = ConfigurationRules.Upgrade(Json.Serializer().Deserialize<AppConfig>(json));
                Validate(config);
                return config;
            }
            catch
            {
                throw new UserError(L10n.Format("error.configRead", FilePath));
            }
        }

        public void Save(AppConfig config)
        {
            Validate(config);
            PrivateStorage.Write(FilePath, DataProtection.Protect(Json.Serializer().Serialize(config)));
        }

        internal static void Validate(AppConfig config)
        {
            ConfigurationRules.Validate(config);
            AmazonHosts.Require(config.AmazonDomain);
        }
    }
}
