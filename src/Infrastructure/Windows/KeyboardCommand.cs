using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace RoomPilot
{
    public sealed class KeyboardCommand
    {
        public string DeviceKey { get; set; }
        public string Action { get; set; }
        public uint Modifiers { get; set; }
        public int KeyCode { get; set; }

        public string Display
        {
            get
            {
                return Format(Modifiers, KeyCode);
            }
        }

        public static string Format(uint mods, int key)
        {
            if (key == 0)
                return L10n.Text("shortcut.capture");
            var parts = new List<string>();
            if ((mods & 2) != 0)
                parts.Add("Ctrl");
            if ((mods & 1) != 0)
                parts.Add("Alt");
            if ((mods & 4) != 0)
                parts.Add("Shift");
            if ((mods & 8) != 0)
                parts.Add("Win");
            string name = ((Keys)key).ToString();
            if (key >= (int)Keys.D0 && key <= (int)Keys.D9)
                name = (key - (int)Keys.D0).ToString();
            parts.Add(name);
            return String.Join(" + ", parts);
        }

        public static List<KeyboardCommand> Defaults()
        {
            return new List<KeyboardCommand>
            {
                new KeyboardCommand
                {
                    Action = "panel",
                    DeviceKey = "",
                    Modifiers = 3,
                    KeyCode = (int)Keys.D0
                },
                new KeyboardCommand
                {
                    Action = "toggle",
                    DeviceKey = "lamparina",
                    Modifiers = 3,
                    KeyCode = (int)Keys.D1
                },
                new KeyboardCommand
                {
                    Action = "toggle",
                    DeviceKey = "tomada",
                    Modifiers = 3,
                    KeyCode = (int)Keys.D2
                },
                new KeyboardCommand
                {
                    Action = "toggle",
                    DeviceKey = "fita",
                    Modifiers = 3,
                    KeyCode = (int)Keys.D3
                }
            };
        }

        public static void Validate(IEnumerable<KeyboardCommand> commands, AppConfig config)
        {
            if (commands == null)
                throw new UserError(L10n.Text("error.shortcuts"));
            var used = new HashSet<string>();
            foreach (var c in commands)
            {
                if (c == null)
                    throw new UserError(L10n.Text("error.shortcut"));
                bool special = (c.KeyCode >= (int)Keys.F1 && c.KeyCode <= (int)Keys.F24);
                if (c.KeyCode < 8 || c.KeyCode > 254 || c.KeyCode == 16 || c.KeyCode == 17 || c.KeyCode == 18 || c.KeyCode == 91 || c.KeyCode == 92 || (c.Modifiers & ~15u) != 0 || (!special && (c.Modifiers & 11) == 0))
                    throw new UserError(L10n.Text("error.shortcutKeys"));
                if (c.Action != "panel" && c.Action != "on" && c.Action != "off" && c.Action != "toggle")
                    throw new UserError(L10n.Text("error.shortcutAction"));
                if (c.Action != "panel" && !config.Devices.Any(d => d.Key == c.DeviceKey))
                    throw new UserError(L10n.Text("error.shortcutDevice"));
                if (!used.Add(c.Modifiers + ":" + c.KeyCode))
                    throw new UserError(L10n.Format("error.shortcutDuplicate", c.Display));
            }
        }
    }
}
