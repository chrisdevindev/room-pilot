using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace RoomPilot
{
    static class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            bool command = CommandLine.IsRequested(args);
            try
            {
                string dataPath = CommandLine.Option(args, "--data-dir");
                if (dataPath == null)
                {
                    dataPath = PrivateStorage.DefaultDirectory;
                    PrivateStorage.SecureDirectory(dataPath);
                    PrivateStorage.ImportLegacy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data-alexa"), dataPath);
                }

                var store = new ConfigStore(dataPath);
                var config = store.Load();
                L10n.SetLanguage(config.Language);
                string language = CommandLine.Option(args, "--language");
                if (language != null)
                    L10n.SetLanguage(language);
                if (args.Contains("--migrate-only"))
                    return 0;
                string preview = CommandLine.Option(args, "--preview");
                if (preview != null)
                {
                    using (var form = new MainForm(store, config, true))
                        Render(form, preview);
                    return 0;
                }

                string settingsPreview = CommandLine.Option(args, "--preview-settings");
                if (settingsPreview != null)
                {
                    using (var form = new SettingsForm(store, config))
                    {
                        string tab = CommandLine.Option(args, "--preview-tab");
                        int tabIndex = tab == "devices" ? 1 : tab == "shortcuts" ? 2 : tab == "preferences" ? 3 : 0;
                        form.Controls.OfType<TabControl>().Single().SelectedIndex = tabIndex;
                        Render(form, settingsPreview);
                    }

                    return 0;
                }

                if (command)
                    return CommandLine.RunAsync(store, config, args).GetAwaiter().GetResult();
                Startup.UpgradeLegacyEntry();
                // Keep the old instance identity to avoid running both renamed versions together.
                string identity;
                using (var sha = SHA256.Create())
                    identity = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(store.FilePath.ToLowerInvariant()))).Replace("-", "").Substring(0, 20);
                bool created;
                using (var single = new Mutex(true, "Local\\CasaInteligenteAlexa-" + identity, out created))
                using (var wake = new EventWaitHandle(false, EventResetMode.AutoReset, "Local\\CasaInteligenteAlexa-Show-" + identity))
                {
                    if (!created)
                    {
                        wake.Set();
                        return 0;
                    }

                    using (var form = new MainForm(store, config, false, () => new AlexaDeviceService(store.DirectoryPath)))
                    {
                        form.StartInTray = args.Contains("--tray");
                        var handle = form.Handle;
                        if (args.Contains("--connect"))
                            form.Shown += delegate
                            {
                                form.BeginInvoke(new Action(form.OpenSettings));
                            };
                        var registration = ThreadPool.RegisterWaitForSingleObject(wake, delegate
                        {
                            form.ShowPanel();
                        }, null, -1, false);
                        try
                        {
                            Application.Run(form);
                        }
                        finally
                        {
                            registration.Unregister(null);
                        }
                    }

                    single.ReleaseMutex();
                }

                return 0;
            }
            catch (Exception ex)
            {
                string message = ex is UserError ? ex.Message : L10n.Text("error.launch");
                if (command || args.Contains("--migrate-only") || args.Contains("--preview") || args.Contains("--preview-settings"))
                {
                    Console.Error.WriteLine(message);
                    return 1;
                }

                MessageBox.Show(message, "room-pilot", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return 1;
            }
        }

        static void Render(Form form, string file)
        {
            form.Show();
            Application.DoEvents();
            using (var bitmap = new Bitmap(form.Width, form.Height))
            {
                form.DrawToBitmap(bitmap, new Rectangle(0, 0, form.Width, form.Height));
                bitmap.Save(Path.GetFullPath(file), System.Drawing.Imaging.ImageFormat.Png);
            }

            form.Hide();
        }
    }
}
