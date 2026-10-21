using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace RoomPilot
{
    sealed class ShortcutRow : Panel
    {
        static readonly string[] Actions =
        {
            "toggle",
            "on",
            "off",
            "panel"
        };
        readonly ComboBox action, device;
        readonly TextBox capture;
        readonly AppConfig config;
        uint modifiers;
        int code;
        public event Action Remove;
        public ShortcutRow(AppConfig appConfig, KeyboardCommand command)
        {
            config = appConfig;
            Size = new Size(735, 46);
            Margin = new Padding(0, 0, 0, 8);
            action = new ThemedComboBox
            {
                Bounds = new Rectangle(4, 4, 146, 29)
            };
            action.Items.AddRange(new[] { L10n.Text("action.toggle"), L10n.Text("action.on"), L10n.Text("action.off"), L10n.Text("action.panel") });
            action.SelectedIndex = Math.Max(0, Array.IndexOf(Actions, command.Action));
            device = new ThemedComboBox
            {
                Bounds = new Rectangle(162, 4, 168, 29),
                DisplayMember = "Name"
            };
            RefreshDevices(command.DeviceKey);
            capture = new TextBox
            {
                Bounds = new Rectangle(342, 4, 302, 29),
                ReadOnly = true,
                BackColor = Theme.Background,
                ForeColor = Theme.Text,
                ShortcutsEnabled = false
            };
            modifiers = command.Modifiers;
            code = command.KeyCode;
            capture.Text = KeyboardCommand.Format(modifiers, code);
            capture.KeyDown += CaptureShortcut;
            var remove = Theme.Button("×", 658, 3, 48, 31, false);
            remove.AccessibleName = L10n.Text("action.removeShortcut");
            remove.Click += delegate
            {
                if (Remove != null)
                    Remove();
            };
            action.SelectedIndexChanged += delegate
            {
                device.Enabled = action.SelectedIndex != 3;
            };
            device.Enabled = action.SelectedIndex != 3;
            Controls.AddRange(new Control[] { action, device, capture, remove });
        }

        void CaptureShortcut(object sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = e.Handled = true;
            if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.Menu || e.KeyCode == Keys.LWin || e.KeyCode == Keys.RWin)
                return;
            modifiers = (uint)((e.Alt ? 1 : 0) | (e.Control ? 2 : 0) | (e.Shift ? 4 : 0));
            code = (int)e.KeyCode;
            capture.Text = KeyboardCommand.Format(modifiers, code);
        }

        public void RefreshDevices(string key)
        {
            device.Items.Clear();
            device.Items.AddRange(config.Devices.ToArray());
            device.SelectedItem = config.Devices.FirstOrDefault(item => item.Key == key);
        }

        public KeyboardCommand Value()
        {
            var selected = device.SelectedItem as DeviceConfig;
            return new KeyboardCommand
            {
                Action = Actions[action.SelectedIndex],
                DeviceKey = action.SelectedIndex == 3 || selected == null ? "" : selected.Key,
                Modifiers = modifiers,
                KeyCode = code
            };
        }
    }
}
