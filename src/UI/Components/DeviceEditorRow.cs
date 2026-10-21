using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace RoomPilot
{
    sealed class DeviceEditorRow : Panel
    {
        readonly TextBox name;
        readonly ComboBox endpoint;
        public DeviceConfig Device { get; private set; }

        public event Action Remove;
        public DeviceEditorRow(DeviceConfig device)
        {
            Device = device;
            Size = new Size(735, 91);
            Margin = new Padding(0, 0, 0, 10);
            name = new TextBox
            {
                Text = device.Name,
                MaxLength = 80,
                Bounds = new Rectangle(4, 4, 603, 28),
                BackColor = Theme.Background,
                ForeColor = Theme.Text,
                AccessibleName = L10n.Text("device.name")
            };
            name.TextChanged += delegate
            {
                Device.Name = name.Text;
            };
            endpoint = new ThemedComboBox
            {
                Bounds = new Rectangle(4, 46, 710, 29),
                AccessibleName = L10n.Text("device.endpoint")
            };
            var remove = Theme.Button(L10n.Text("action.remove"), 615, 3, 100, 31, false);
            remove.Click += delegate
            {
                if (Remove != null)
                    Remove();
            };
            Controls.AddRange(new Control[] { name, endpoint, remove });
            Populate(new List<AlexaEndpoint>(), false);
        }

        public void Populate(List<AlexaEndpoint> available, bool suggest)
        {
            var previous = endpoint.SelectedItem as AlexaEndpoint;
            string id = previous == null ? Device.DeviceId : previous.Id;
            var selected = available.FirstOrDefault(device => device.Id == id);
            endpoint.Items.Clear();
            endpoint.Items.Add(new AlexaEndpoint { Id = "", Name = L10n.Text("device.none") });
            if (!String.IsNullOrEmpty(id) && selected == null)
            {
                selected = new AlexaEndpoint
                {
                    Id = id,
                    Name = L10n.Text("device.savedMissing")
                };
                endpoint.Items.Add(selected);
            }

            endpoint.Items.AddRange(available.ToArray());
            if (String.IsNullOrEmpty(id) && suggest)
                selected = DeviceMatching.Suggest(Device, available);
            endpoint.SelectedItem = selected ?? endpoint.Items[0];
        }

        public void ClearSelection()
        {
            endpoint.SelectedIndex = 0;
        }

        public void FocusName()
        {
            name.Focus();
            name.SelectAll();
        }

        public void Commit()
        {
            DeviceCatalog.Rename(Device, name.Text);
            var selected = endpoint.SelectedItem as AlexaEndpoint;
            Device.DeviceId = selected == null ? "" : selected.Id;
            Device.Brand = selected == null || String.IsNullOrEmpty(selected.Manufacturer) ? Device.Brand : selected.Manufacturer;
        }
    }
}
