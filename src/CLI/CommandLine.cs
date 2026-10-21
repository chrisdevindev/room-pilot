using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomPilot
{
    public static class CommandLine
    {
        public static string Option(string[] args, string name)
        {
            int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }

        public static bool IsRequested(string[] args)
        {
            return args.Contains("--command") || args.Contains("--list-devices");
        }

        public static async Task<int> RunAsync(ConfigStore store, AppConfig config, string[] args)
        {
            if (args.Contains("--list-devices"))
            {
                Write(config.Devices.Select(item => new { key = item.Key, name = item.Name }).ToArray(), args);
                return 0;
            }

            string action = Option(args, "--command");
            string selection = Option(args, "--device");
            var device = config.Devices.FirstOrDefault(item => item.Key == selection);
            if (device == null)
            {
                var matches = config.Devices.Where(item => String.Equals(item.Name, selection, StringComparison.OrdinalIgnoreCase)).ToList();
                if (matches.Count == 1)
                    device = matches[0];
            }

            if (device == null)
                throw new UserError(L10n.Text("error.cliDevice"));
            PowerAction command = action == "status" ? PowerAction.Toggle : DeviceActions.Parse(action);
            using (var controller = new DeviceController(config, new AlexaDeviceService(store.DirectoryPath)))
            {
                string message = "";
                controller.Notice += notice => message = notice;
                bool success;
                if (action == "status")
                {
                    await controller.RefreshAsync().ConfigureAwait(false);
                    success = controller.States[device.Key].CanControl;
                }
                else
                    success = await controller.ExecuteAsync(device.Key, command).ConfigureAwait(false);
                var state = controller.States[device.Key];
                Write(new { success = success, device = device.Key, power = state.Power, online = state.Online, message = String.IsNullOrEmpty(message) ? state.Message : message }, args);
                return success ? 0 : 2;
            }
        }

        static void Write(object result, string[] args)
        {
            string json = Json.Serializer().Serialize(result);
            Console.WriteLine(json);
            string file = Option(args, "--result");
            if (file != null)
                File.WriteAllText(Path.GetFullPath(file), json, new UTF8Encoding(false));
        }
    }
}
