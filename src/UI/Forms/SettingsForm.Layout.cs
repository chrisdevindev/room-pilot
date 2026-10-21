using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace RoomPilot
{
    public sealed partial class SettingsForm
    {
        void InitializeLayout()
        {
            Theme.Form(this, L10n.Text("window.settings"), 850, 686);
            Icon = Theme.CreateIcon();
            Controls.Add(Theme.Label(L10n.Text("settings.heading"), 23, 17, 700, 38, 21, Theme.Text));
            Controls.Add(Theme.Label(L10n.Text("settings.subtitle"), 25, 61, 790, 25, 10, Theme.Muted));
            tabs = new TabControl
            {
                Bounds = new Rectangle(20, 102, 810, 496)
            };
            Controls.Add(tabs);
            BuildAccount(Page(L10n.Text("tab.account")));
            BuildDevices(Page(L10n.Text("tab.devices")));
            BuildShortcuts(Page(L10n.Text("tab.shortcuts")));
            BuildPreferences(Page(L10n.Text("tab.preferences")));
            message = Theme.Label("", 23, 610, 550, 57, 9, Theme.Accent);
            var cancel = Theme.Button(L10n.Text("action.cancel"), 584, 624, 112, 40, false);
            cancel.Click += delegate
            {
                if (!busy)
                {
                    DialogResult = DialogResult.Cancel;
                    Close();
                }
            };
            var save = Theme.Button(L10n.Text("action.save"), 712, 624, 118, 40, true);
            save.Click += delegate
            {
                Save();
            };
            Controls.AddRange(new Control[] { message, cancel, save });
        }

        TabPage Page(string title)
        {
            var page = new TabPage(title)
            {
                BackColor = Theme.Surface,
                ForeColor = Theme.Text
            };
            tabs.TabPages.Add(page);
            return page;
        }

        static FlowLayoutPanel ScrollRows(int top, int height)
        {
            return new FlowLayoutPanel
            {
                Bounds = new Rectangle(16, top, 770, height),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };
        }

        void BuildAccount(TabPage page)
        {
            page.Controls.Add(Theme.Label(L10n.Text("account.heading"), 20, 18, 750, 33, 14, Theme.Text));
            page.Controls.Add(Theme.Label(L10n.Text("account.instructions"), 20, 63, 745, 47, 10, Theme.Muted));
            page.Controls.Add(Theme.Label(L10n.Text("account.region"), 20, 127, 330, 23, 10, Theme.Muted));
            region = new ThemedComboBox
            {
                Bounds = new Rectangle(20, 157, 350, 29)
            };
            region.Items.AddRange(AmazonHosts.Domains);
            region.SelectedItem = draft.AmazonDomain;
            var login = Theme.Button(L10n.Text("account.signIn"), 20, 212, 225, 44, true);
            login.Click += delegate
            {
                try
                {
                    SignIn();
                }
                catch (Exception error)
                {
                    Error(error);
                }
            };
            var list = Theme.Button(L10n.Text("account.query"), 261, 212, 225, 44, false);
            list.Click += async delegate
            {
                await LoadDevicesAsync();
            };
            connection = Theme.Label(sessions.Exists ? L10n.Text("account.saved") : L10n.Text("account.disconnected"), 20, 279, 745, 55, 10, Theme.Accent);
            var logout = Theme.Button(L10n.Text("account.signOut"), 20, 354, 190, 37, false);
            logout.Click += delegate
            {
                pendingSession = null;
                clearSession = true;
                foreach (var row in deviceEditors)
                    row.ClearSelection();
                connection.Text = L10n.Text("account.signOutPending");
            };
            page.Controls.AddRange(new Control[] { region, login, list, connection, logout });
            page.Controls.Add(Theme.Label(L10n.Text("account.unofficial"), 20, 407, 745, 47, 9, Theme.Muted));
        }

        void BuildDevices(TabPage page)
        {
            page.Controls.Add(Theme.Label(L10n.Text("devices.heading"), 20, 18, 750, 33, 14, Theme.Text));
            mappingHint = Theme.Label(L10n.Text("devices.instructions"), 20, 61, 745, 45, 10, Theme.Muted);
            deviceRows = ScrollRows(117, 281);
            var add = Theme.Button(L10n.Text("devices.add"), 20, 408, 220, 36, false);
            add.Click += delegate
            {
                AddDevice(L10n.Text("default.newDevice"));
            };
            page.Controls.AddRange(new Control[] { mappingHint, deviceRows, add });
            page.Controls.Add(Theme.Label(L10n.Text("devices.saveHint"), 20, 447, 745, 20, 9, Theme.Muted));
        }

        void BuildShortcuts(TabPage page)
        {
            enabled = new CheckBox
            {
                Text = L10n.Text("shortcuts.enabled"),
                Bounds = new Rectangle(20, 18, 750, 29),
                Checked = draft.HotkeysEnabled,
                ForeColor = Theme.Text
            };
            page.Controls.Add(enabled);
            page.Controls.Add(Theme.Label(L10n.Text("shortcuts.action"), 22, 61, 140, 25, 9, Theme.Muted));
            page.Controls.Add(Theme.Label(L10n.Text("shortcuts.device"), 180, 61, 169, 25, 9, Theme.Muted));
            page.Controls.Add(Theme.Label(L10n.Text("shortcuts.combination"), 358, 61, 340, 25, 9, Theme.Muted));
            shortcutRows = ScrollRows(90, 284);
            var add = Theme.Button(L10n.Text("shortcuts.add"), 20, 384, 184, 34, false);
            add.Click += delegate
            {
                AddShortcut(new KeyboardCommand { Action = "panel", DeviceKey = "" });
            };
            page.Controls.AddRange(new Control[] { shortcutRows, add });
            page.Controls.Add(Theme.Label(L10n.Text("shortcuts.instructions"), 20, 435, 745, 40, 9, Theme.Muted));
        }

        void BuildPreferences(TabPage page)
        {
            page.Controls.Add(Theme.Label(L10n.Text("preferences.language"), 20, 18, 750, 24, 10, Theme.Muted));
            language = new ThemedComboBox
            {
                Bounds = new Rectangle(20, 49, 350, 29),
                AccessibleName = L10n.Text("preferences.language")
            };
            language.Items.AddRange(languageCodes.Select(code => L10n.Catalog(code)["language.name"]).ToArray());
            language.SelectedIndex = Array.IndexOf(languageCodes, draft.Language);
            page.Controls.Add(language);
            page.Controls.Add(Theme.Label(L10n.Text("preferences.languageHint"), 20, 93, 745, 49, 10, Theme.Muted));
            startup = new CheckBox
            {
                Text = L10n.Text("preferences.startup"),
                Bounds = new Rectangle(20, 157, 750, 29),
                Checked = originalStartup,
                ForeColor = Theme.Text
            };
            page.Controls.Add(startup);
            page.Controls.Add(Theme.Label(L10n.Text("preferences.closeHint"), 20, 211, 745, 80, 10, Theme.Muted));
            page.Controls.Add(Theme.Label(L10n.Text("preferences.trayHint"), 20, 309, 745, 68, 10, Theme.Muted));
            page.Controls.Add(Theme.Label(L10n.Text("preferences.privacy"), 20, 395, 745, 60, 10, Theme.Muted));
        }
    }
}
