using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace RoomPilot
{
    sealed class TrayMenu : IDisposable
    {
        readonly NotifyIcon icon;
        readonly ContextMenuStrip menu = new ContextMenuStrip();
        readonly Dictionary<string, ToolStripMenuItem> devices = new Dictionary<string, ToolStripMenuItem>();
        public event Action OpenPanel, OpenSettings, Refresh, Exit;
        public event Action<string, PowerAction> Execute;
        public bool Enabled
        {
            set
            {
                menu.Enabled = value;
            }
        }

        public TrayMenu(AppConfig config, Icon image)
        {
            menu.Items.Add(L10n.Text("action.panel"), null, delegate
            {
                if (OpenPanel != null)
                    OpenPanel();
            });
            menu.Items.Add(new ToolStripSeparator());
            foreach (var device in config.Devices)
            {
                var item = new ToolStripMenuItem(device.Name);
                string key = device.Key;
                foreach (PowerAction action in Enum.GetValues(typeof(PowerAction)))
                {
                    PowerAction selected = action;
                    item.DropDownItems.Add(DeviceActions.Label(action), null, delegate
                    {
                        if (Execute != null)
                            Execute(key, selected);
                    });
                }

                menu.Items.Add(item);
                devices.Add(key, item);
            }

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(L10n.Text("action.refresh"), null, delegate
            {
                if (Refresh != null)
                    Refresh();
            });
            menu.Items.Add(L10n.Text("action.settings"), null, delegate
            {
                if (OpenSettings != null)
                    OpenSettings();
            });
            menu.Items.Add(L10n.Text("action.exit"), null, delegate
            {
                if (Exit != null)
                    Exit();
            });
            icon = new NotifyIcon
            {
                Text = L10n.Text("window.main"),
                Icon = image,
                ContextMenuStrip = menu,
                Visible = true
            };
            icon.MouseClick += delegate (object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left && menu.Enabled)
                    menu.Show(Cursor.Position);
            };
            icon.DoubleClick += delegate
            {
                if (OpenPanel != null)
                    OpenPanel();
            };
        }

        public void Display(DeviceConfig device, DeviceState state, bool available)
        {
            var item = devices[device.Key];
            item.Text = device.Name + " · " + (state.Power == true ? L10n.Text("state.on") : state.Power == false ? L10n.Text("state.off") : L10n.Text("state.unconfirmed"));
            item.Enabled = available;
        }

        public void Notice(string message)
        {
            icon.ShowBalloonTip(3500, "room-pilot", message, ToolTipIcon.Info);
        }

        public void Dispose()
        {
            icon.Visible = false;
            icon.Dispose();
            menu.Dispose();
        }
    }
}
