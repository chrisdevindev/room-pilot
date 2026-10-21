using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace RoomPilot
{
    public static class ConfigurationRules
    {
        public static AppConfig Upgrade(AppConfig config)
        {
            if (config == null)
                throw new InvalidDataException();
            if (config.Schema == 1 || config.Schema == 2)
            {
                config.Schema = AppConfig.CurrentSchema;
                config.Language = L10n.Portuguese;
            }

            if (config.KeyboardCommands == null)
                config.KeyboardCommands = KeyboardCommand.Defaults().Where(command => command.Action == "panel" || (config.Devices != null && config.Devices.Any(device => device != null && device.Key == command.DeviceKey))).ToList();
            return config;
        }

        public static void Validate(AppConfig config)
        {
            if (config == null || config.Schema != AppConfig.CurrentSchema || config.Devices == null || config.KeyboardCommands == null)
                throw new UserError(L10n.Text("error.configFormat"));
            if (!L10n.IsSupported(config.Language))
                throw new UserError(L10n.Text("error.language"));
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var selected = new HashSet<string>(StringComparer.Ordinal);
            foreach (var device in config.Devices)
            {
                if (device == null || !Regex.IsMatch(device.Key ?? "", "^[a-zA-Z0-9][a-zA-Z0-9_-]{0,63}$") || !keys.Add(device.Key))
                    throw new UserError(L10n.Text("error.deviceKey"));
                DeviceCatalog.ValidName(device.Name);
                if (!String.IsNullOrWhiteSpace(device.DeviceId) && !selected.Add(device.DeviceId))
                    throw new UserError(L10n.Text("error.duplicateDevice"));
            }

            KeyboardCommand.Validate(config.KeyboardCommands, config);
        }
    }
}
