using System;
using System.Drawing;
using System.Windows.Forms;

namespace RoomPilot
{
    sealed class DeviceCard : Panel
    {
        readonly Label status, shortcuts;
        readonly Button on, off;
        public event Action<PowerAction> Execute;
        public DeviceCard(DeviceConfig device)
        {
            Size = new Size(824, 97);
            Margin = new Padding(0, 0, 0, 10);
            BackColor = Theme.Surface;
            Controls.Add(new DeviceGlyph(0) { Bounds = new Rectangle(16, 23, 56, 56) });
            Controls.Add(Theme.Label(device.Name, 90, 9, 470, 26, 13, Theme.Text));
            status = Theme.Label(L10n.Text("state.pending"), 90, 39, 470, 27, 9.5f, Theme.Muted);
            shortcuts = Theme.Label("", 90, 72, 470, 22, 9, Theme.Muted);
            on = Theme.Button(L10n.Text("action.on"), 580, 29, 108, 42, true);
            off = Theme.Button(L10n.Text("action.off"), 700, 29, 108, 42, false);
            on.Click += delegate
            {
                if (Execute != null)
                    Execute(PowerAction.On);
            };
            off.Click += delegate
            {
                if (Execute != null)
                    Execute(PowerAction.Off);
            };
            Controls.AddRange(new Control[] { status, shortcuts, on, off });
        }

        public void Display(DeviceState state, string keyboardHint, bool available, ToolTip tooltip)
        {
            status.Text = state.Busy ? L10n.Text("state.querying") : state.Message;
            status.ForeColor = state.Power == true ? Theme.Accent : Theme.Muted;
            shortcuts.Text = keyboardHint;
            on.Enabled = off.Enabled = available;
            tooltip.SetToolTip(status, state.Message);
            tooltip.SetToolTip(shortcuts, keyboardHint);
        }
    }
}
