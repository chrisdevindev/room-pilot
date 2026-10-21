using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace RoomPilot
{
    public sealed class MainForm : Form
    {
        readonly ConfigStore store;
        readonly Func<IDeviceService> serviceFactory;
        readonly bool preview;
        readonly Label bannerTitle, bannerBody, feedback, shortcutHint, subtitle;
        readonly Button refresh, settings;
        readonly FlowLayoutPanel deviceList;
        readonly ToolTip tooltip = new ToolTip();
        readonly Timer timer = new Timer
        {
            Interval = 120000
        };
        readonly Dictionary<string, DeviceCard> cards = new Dictionary<string, DeviceCard>();
        AppConfig config;
        DeviceController controller;
        HotkeyRegistration hotkeys;
        TrayMenu tray;
        bool exiting, settingsOpen;
        public bool StartInTray { get; set; }

        bool HasConnection
        {
            get
            {
                return config.Devices.Any(controller.IsConfigured);
            }
        }

        public MainForm(ConfigStore configStore, AppConfig initial, bool previewMode) : this(configStore, initial, previewMode, null)
        {
        }

        public MainForm(ConfigStore configStore, AppConfig initial, bool previewMode, Func<IDeviceService> createService)
        {
            store = configStore;
            serviceFactory = createService;
            config = initial;
            preview = previewMode;
            Theme.Form(this, L10n.Text("window.main"), 900, 646);
            Icon = Theme.CreateIcon();
            Controls.Add(Theme.Label("room-pilot", 28, 21, 650, 44, 24, Theme.Text));
            subtitle = Theme.Label(L10n.Text("app.subtitle"), 30, 73, 680, 27, 11, Theme.Muted);
            Controls.Add(subtitle);
            settings = Theme.Button(L10n.Text("action.settings"), 720, 30, 152, 42, false);
            settings.Click += delegate
            {
                OpenSettings();
            };
            Controls.Add(settings);
            var banner = new Panel
            {
                Bounds = new Rectangle(28, 116, 844, 72),
                BackColor = Color.FromArgb(28, 48, 46)
            };
            bannerTitle = Theme.Label("", 17, 11, 810, 25, 11, Theme.Accent);
            bannerBody = Theme.Label("", 17, 40, 810, 23, 9.5f, Theme.Muted);
            banner.Controls.AddRange(new Control[] { bannerTitle, bannerBody });
            deviceList = new FlowLayoutPanel
            {
                Bounds = new Rectangle(28, 208, 844, 326),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };
            feedback = Theme.Label(L10n.Text("main.closeHint"), 30, 552, 654, 43, 9, Theme.Muted);
            refresh = Theme.Button(L10n.Text("action.refresh"), 696, 551, 176, 39, false);
            refresh.Click += async delegate
            {
                await controller.RefreshAsync();
            };
            shortcutHint = Theme.Label("", 30, 609, 842, 25, 9, Theme.Muted);
            Controls.AddRange(new Control[] { banner, deviceList, feedback, refresh, shortcutHint });
            RebuildDevices();
            timer.Tick += async delegate
            {
                if (HasConnection)
                    await controller.RefreshAsync();
            };
            Shown += async delegate
            {
                if (preview)
                    return;
                hotkeys = new HotkeyRegistration(Handle);
                RegisterKeys();
                if (StartInTray)
                    Hide();
                timer.Start();
                if (HasConnection)
                    await controller.RefreshAsync();
            };
            FormClosing += delegate (object sender, FormClosingEventArgs e)
            {
                if (e.CloseReason == CloseReason.UserClosing && !exiting && !preview)
                {
                    e.Cancel = true;
                    Hide();
                }
            };
        }

        void RebuildDevices()
        {
            if (controller != null)
                controller.Dispose();
            if (tray != null)
            {
                tray.Dispose();
                tray = null;
            }

            foreach (var card in cards.Values)
                card.Dispose();
            cards.Clear();
            deviceList.Controls.Clear();
            IDeviceService service = null;
            if (!preview)
            {
                try
                {
                    service = serviceFactory == null ? null : serviceFactory();
                }
                catch (UserError error)
                {
                    feedback.Text = error.Message;
                }
            }

            controller = new DeviceController(config, service);
            controller.Changed += delegate
            {
                Ui(Render);
            };
            controller.Notice += text => Ui(delegate
            {
                feedback.Text = text;
                if (!Visible && tray != null)
                    tray.Notice(text);
            });
            foreach (var device in config.Devices)
            {
                string key = device.Key;
                var card = new DeviceCard(device);
                card.Execute += async action => await controller.ExecuteAsync(key, action);
                cards.Add(key, card);
                deviceList.Controls.Add(card);
            }

            if (config.Devices.Count == 0)
                deviceList.Controls.Add(Theme.Label(L10n.Text("main.empty"), 0, 0, 780, 50, 11, Theme.Muted));
            if (!preview)
                CreateTray();
            Render();
        }

        void CreateTray()
        {
            tray = new TrayMenu(config, Icon);
            tray.OpenPanel += ShowPanel;
            tray.OpenSettings += OpenSettings;
            tray.Refresh += async delegate
            {
                await controller.RefreshAsync();
            };
            tray.Execute += async (key, action) => await controller.ExecuteAsync(key, action);
            tray.Exit += delegate
            {
                exiting = true;
                Close();
            };
        }

        void Ui(Action action)
        {
            if (IsDisposed || Disposing)
                return;
            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(action);
                }
                catch (InvalidOperationException)
                {
                }
            }
            else
                action();
        }

        string DeviceShortcuts(string key)
        {
            if (!config.HotkeysEnabled)
                return L10n.Text("main.shortcutsDisabled");
            var commands = config.KeyboardCommands.Where(command => command.DeviceKey == key && command.Action != "panel");
            return String.Join(" · ", commands.Select(command => command.Display + " (" + DeviceActions.Label(DeviceActions.Parse(command.Action)) + ")"));
        }

        void Render()
        {
            int ready = controller.States.Values.Count(state => state.CanControl);
            bannerTitle.Text = HasConnection ? L10n.Text("main.connected") : L10n.Text("main.connect");
            bannerBody.Text = HasConnection ? L10n.Format("main.confirmedCount", ready, config.Devices.Count) : L10n.Text("main.connectHint");
            foreach (var device in config.Devices)
            {
                var state = controller.States[device.Key];
                bool available = controller.IsConfigured(device) && !controller.Busy;
                cards[device.Key].Display(state, DeviceShortcuts(device.Key), available, tooltip);
                if (tray != null)
                    tray.Display(device, state, available);
            }

            refresh.Enabled = HasConnection && !controller.Busy;
            settings.Enabled = !controller.Busy;
            var panel = config.HotkeysEnabled ? config.KeyboardCommands.FirstOrDefault(command => command.Action == "panel") : null;
            shortcutHint.Text = (panel == null ? "" : L10n.Format("main.panelShortcut", panel.Display)) + L10n.Text("main.trayHint");
        }

        public void ShowPanel()
        {
            Ui(delegate
            {
                Show();
                WindowState = FormWindowState.Normal;
                Activate();
            });
        }

        public async void OpenSettings()
        {
            if (controller.Busy || settingsOpen)
                return;
            settingsOpen = true;
            timer.Stop();
            if (hotkeys != null)
                hotkeys.Dispose();
            if (tray != null)
                tray.Enabled = false;
            try
            {
                using (var dialog = new SettingsForm(store, config, true))
                {
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        config = dialog.SavedConfig;
                        ApplyLanguage();
                        feedback.Text = L10n.Text("main.saved");
                        if (HasConnection)
                            await controller.RefreshAsync();
                    }
                }
            }
            finally
            {
                settingsOpen = false;
                if (tray != null)
                    tray.Enabled = true;
                RegisterKeys();
                timer.Start();
            }
        }

        internal void ApplyLanguage()
        {
            L10n.SetLanguage(config.Language);
            Text = L10n.Text("window.main");
            subtitle.Text = L10n.Text("app.subtitle");
            settings.Text = L10n.Text("action.settings");
            refresh.Text = L10n.Text("action.refresh");
            RebuildDevices();
        }

        void RegisterKeys()
        {
            if (preview || hotkeys == null)
                return;
            try
            {
                var conflicts = hotkeys.Register(config);
                if (conflicts.Count > 0)
                    feedback.Text = L10n.Format("main.shortcutConflict", String.Join(", ", conflicts));
            }
            catch (UserError error)
            {
                feedback.Text = error.Message;
            }
        }

        protected override void WndProc(ref Message message)
        {
            KeyboardCommand command;
            if (message.Msg == HotkeyRegistration.MessageId && hotkeys != null && !settingsOpen && hotkeys.TryGet(message.WParam.ToInt32(), out command))
            {
                if (command.Action == "panel")
                    ShowPanel();
                else
                {
                    var pending = controller.ExecuteAsync(command.DeviceKey, DeviceActions.Parse(command.Action));
                }
            }

            base.WndProc(ref message);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                timer.Dispose();
                if (hotkeys != null)
                    hotkeys.Dispose();
                if (tray != null)
                    tray.Dispose();
                tooltip.Dispose();
                if (controller != null && !controller.Busy)
                    controller.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
