using System;

namespace RoomPilot
{
    public static class DeviceCatalog
    {
        public static DeviceConfig Add(AppConfig config, string name)
        {
            var device = new DeviceConfig
            {
                Key = "device-" + Guid.NewGuid().ToString("N"),
                Name = ValidName(name)
            };
            config.Devices.Add(device);
            return device;
        }

        public static void Rename(DeviceConfig device, string name)
        {
            device.Name = ValidName(name);
        }

        public static void Remove(AppConfig config, string key)
        {
            config.Devices.RemoveAll(device => device.Key == key);
            config.KeyboardCommands.RemoveAll(command => command.DeviceKey == key && command.Action != "panel");
        }

        public static string ValidName(string name)
        {
            name = (name ?? "").Trim();
            if (name.Length == 0 || name.Length > 80)
                throw new UserError(L10n.Text("error.deviceName"));
            return name;
        }
    }
}
