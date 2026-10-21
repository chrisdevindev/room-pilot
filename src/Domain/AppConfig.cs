using System.Collections.Generic;

namespace RoomPilot
{
    public sealed class AppConfig
    {
        public const int CurrentSchema = 3;
        public int Schema { get; set; }
        public bool HotkeysEnabled { get; set; }
        public string Language { get; set; }
        public string AmazonDomain { get; set; }
        public List<DeviceConfig> Devices { get; set; }
        public List<KeyboardCommand> KeyboardCommands { get; set; }

        public AppConfig()
        {
            Schema = CurrentSchema;
            Language = L10n.CurrentLanguage;
            HotkeysEnabled = true;
            AmazonDomain = "amazon.com.br";
            KeyboardCommands = KeyboardCommand.Defaults();
            // Starter controls keep their stable keys so existing shortcuts still work.
            Devices = new List<DeviceConfig>
            {
                new DeviceConfig
                {
                    Key = "lamparina",
                    Name = L10n.Text("default.light")
                },
                new DeviceConfig
                {
                    Key = "tomada",
                    Name = L10n.Text("default.outlet")
                },
                new DeviceConfig
                {
                    Key = "fita",
                    Name = L10n.Text("default.strip")
                }
            };
        }
    }
}
