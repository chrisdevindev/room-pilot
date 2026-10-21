using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RoomPilot
{
    public sealed partial class SettingsForm : Form
    {
        readonly ConfigStore store;
        readonly AppConfig draft;
        readonly AlexaSessionStore sessions;
        readonly bool originalStartup;
        readonly string[] languageCodes = L10n.SupportedLanguages.ToArray();
        readonly List<DeviceEditorRow> deviceEditors = new List<DeviceEditorRow>();
        readonly List<ShortcutRow> shortcutEditors = new List<ShortcutRow>();
        List<AlexaEndpoint> availableDevices = new List<AlexaEndpoint>();
        AlexaSession pendingSession;
        ComboBox region, language;
        Label connection, mappingHint, message;
        FlowLayoutPanel deviceRows, shortcutRows;
        CheckBox enabled, startup;
        TabControl tabs;
        bool clearSession, busy;
        public AppConfig SavedConfig { get; private set; }

        public SettingsForm(ConfigStore configStore, AppConfig initial) : this(configStore, initial, false)
        {
        }

        public SettingsForm(ConfigStore configStore, AppConfig initial, bool loadSavedSession)
        {
            store = configStore;
            sessions = new AlexaSessionStore(store.DirectoryPath);
            draft = ConfigurationRules.Upgrade(Json.Serializer().Deserialize<AppConfig>(Json.Serializer().Serialize(initial)));
            originalStartup = Startup.IsEnabled();
            InitializeLayout();
            foreach (var device in draft.Devices)
                AddDeviceRow(device);
            foreach (var command in draft.KeyboardCommands)
                AddShortcut(command);
            tabs.SelectedIndexChanged += delegate
            {
                if (tabs.SelectedIndex == 2)
                    foreach (var row in shortcutEditors)
                        row.RefreshDevices(row.Value().DeviceKey);
            };
            FormClosing += delegate (object sender, FormClosingEventArgs e)
            {
                if (busy)
                {
                    e.Cancel = true;
                    message.Text = L10n.Text("settings.wait");
                }
            };
            Shown += async delegate
            {
                if (loadSavedSession && sessions.Exists)
                    await LoadDevicesAsync();
            };
        }

        void SetBusy(bool value)
        {
            busy = value;
            tabs.Enabled = !value;
        }

        void Error(Exception error)
        {
            message.Text = error is UserError || error is AlexaLoginRequired ? error.Message : L10n.Text("error.settings");
        }

        void SignIn()
        {
            if (busy)
                return;
            using (var dialog = new AlexaLoginForm(store.DirectoryPath, Convert.ToString(region.SelectedItem)))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;
                pendingSession = dialog.Session;
                clearSession = false;
                sessions.Save(pendingSession);
                DisplayDevices(dialog.Devices);
                connection.Text = L10n.Text("settings.connected");
            }
        }

        async Task LoadDevicesAsync()
        {
            if (busy)
                return;
            SetBusy(true);
            message.Text = L10n.Text("settings.querying");
            try
            {
                var session = clearSession ? null : pendingSession ?? sessions.Load();
                if (session == null)
                    throw new UserError(L10n.Text("error.signInFirst"));
                using (var client = new AlexaClient(session, null))
                {
                    var devices = await client.ListDevicesAsync();
                    pendingSession = session;
                    sessions.Save(session);
                    DisplayDevices(devices);
                    connection.Text = devices.Count == 1 ? L10n.Text("settings.foundOne") : L10n.Format("settings.foundCount", devices.Count);
                    message.Text = L10n.Text("settings.review");
                }
            }
            catch (Exception error)
            {
                Error(error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        public void DisplayDevices(List<AlexaEndpoint> devices)
        {
            availableDevices = devices;
            foreach (var row in deviceEditors)
                row.Populate(devices, true);
            mappingHint.Text = devices.Count == 1 ? L10n.Text("devices.foundOne") : L10n.Format("devices.foundCount", devices.Count);
            tabs.SelectedIndex = 1;
        }

        void AddDeviceRow(DeviceConfig device)
        {
            var row = new DeviceEditorRow(device);
            row.Populate(availableDevices, true);
            row.Remove += delegate
            {
                RemoveDevice(device.Key);
            };
            deviceEditors.Add(row);
            deviceRows.Controls.Add(row);
        }

        internal DeviceConfig AddDevice(string name)
        {
            var device = DeviceCatalog.Add(draft, name);
            AddDeviceRow(device);
            var row = deviceEditors.Last();
            deviceRows.ScrollControlIntoView(row);
            row.FocusName();
            return device;
        }

        internal void RemoveDevice(string key)
        {
            var commands = shortcutEditors.Select(row => row.Value()).Where(command => command.Action == "panel" || command.DeviceKey != key).ToList();
            DeviceCatalog.Remove(draft, key);
            var editor = deviceEditors.Single(row => row.Device.Key == key);
            deviceEditors.Remove(editor);
            deviceRows.Controls.Remove(editor);
            editor.Dispose();
            foreach (var row in shortcutEditors)
                row.Dispose();
            shortcutEditors.Clear();
            shortcutRows.Controls.Clear();
            foreach (var command in commands)
                AddShortcut(command);
            message.Text = L10n.Text("settings.removed");
        }

        void AddShortcut(KeyboardCommand command)
        {
            var row = new ShortcutRow(draft, command);
            row.Remove += delegate
            {
                shortcutRows.Controls.Remove(row);
                shortcutEditors.Remove(row);
                row.Dispose();
            };
            shortcutEditors.Add(row);
            shortcutRows.Controls.Add(row);
        }

        internal AppConfig SaveDraft()
        {
            foreach (var row in deviceEditors)
                row.Commit();
            draft.KeyboardCommands = shortcutEditors.Select(row => row.Value()).ToList();
            draft.AmazonDomain = Convert.ToString(region.SelectedItem);
            draft.HotkeysEnabled = enabled.Checked;
            draft.Language = languageCodes[language.SelectedIndex];
            var session = clearSession ? null : pendingSession ?? sessions.Load();
            if (session != null && session.Domain != draft.AmazonDomain)
                throw new UserError(L10n.Text("error.regionChanged"));
            store.Save(draft);
            if (clearSession)
                sessions.Delete();
            else if (pendingSession != null)
                sessions.Save(pendingSession);
            if (startup.Checked != originalStartup)
                Startup.Set(startup.Checked);
            return draft;
        }

        void Save()
        {
            if (busy)
                return;
            try
            {
                SavedConfig = SaveDraft();
                L10n.SetLanguage(SavedConfig.Language);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception error)
            {
                Error(error);
            }
        }
    }
}
