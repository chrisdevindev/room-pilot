using System;
using System.Collections.Generic;

namespace RoomPilot
{
    public sealed class HotkeyRegistration : IDisposable
    {
        readonly IntPtr window;
        readonly Dictionary<int, KeyboardCommand> commands = new Dictionary<int, KeyboardCommand>();
        public const int MessageId = 0x0312;
        public HotkeyRegistration(IntPtr windowHandle)
        {
            window = windowHandle;
        }

        public List<string> Register(AppConfig config)
        {
            Dispose();
            var conflicts = new List<string>();
            if (!config.HotkeysEnabled)
                return conflicts;
            KeyboardCommand.Validate(config.KeyboardCommands, config);
            int id = 100;
            foreach (var command in config.KeyboardCommands)
            {
                if (Native.RegisterHotKey(window, id, command.Modifiers | 0x4000u, (uint)command.KeyCode))
                    commands.Add(id, command);
                else
                    conflicts.Add(command.Display);
                id++;
            }

            return conflicts;
        }

        public bool TryGet(int id, out KeyboardCommand command)
        {
            return commands.TryGetValue(id, out command);
        }

        public void Dispose()
        {
            foreach (int id in commands.Keys)
                Native.UnregisterHotKey(window, id);
            commands.Clear();
        }
    }
}
