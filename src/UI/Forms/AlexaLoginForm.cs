using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace RoomPilot
{
    public sealed class AlexaLoginForm : Form
    {
        readonly WebView2 browser = new WebView2();
        readonly Label location, status;
        readonly string directory, domain;
        AmazonLoginRequest login = new AmazonLoginRequest();
        readonly Button retry;
        bool completing;
        public AlexaSession Session { get; private set; }
        public List<AlexaEndpoint> Devices { get; private set; }

        public AlexaLoginForm(string dataDirectory, string amazonDomain)
        {
            directory = dataDirectory;
            domain = AmazonHosts.Require(amazonDomain);
            Theme.Form(this, L10n.Text("window.login"), 940, 800);
            Icon = Theme.CreateIcon();
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(760, 600);
            MaximizeBox = true;
            Controls.Add(Theme.Label(L10n.Text("login.heading"), 18, 12, 800, 31, 16, Theme.Text));
            Controls.Add(Theme.Label(L10n.Text("login.instructions"), 20, 48, 890, 25, 9, Theme.Muted));
            location = Theme.Label(L10n.Text("login.preparing"), 20, 80, 890, 25, 10, Theme.Accent);
            location.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(location);
            browser.Bounds = new Rectangle(12, 112, 916, 586);
            browser.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(browser);
            status = Theme.Label(L10n.Text("login.sessionInfo"), 20, 713, 890, 42, 9, Theme.Muted);
            status.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(status);
            retry = Theme.Button(L10n.Text("login.restart"), 20, 758, 210, 31, false);
            retry.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            retry.Visible = false;
            retry.Click += delegate
            {
                login = new AmazonLoginRequest(login.Serial);
                completing = false;
                retry.Visible = false;
                browser.Visible = true;
                browser.Enabled = true;
                browser.CoreWebView2.CookieManager.DeleteAllCookies();
                SeedCookies(browser.CoreWebView2);
                status.Text = L10n.Text("login.retry");
                browser.CoreWebView2.Navigate(login.Url);
            };
            Controls.Add(retry);
            Shown += async delegate
            {
                await Initialize();
            };
        }

        async Task Initialize()
        {
            try
            {
                // An isolated, private profile: no access to the user's normal Edge profile.
                var environment = await CoreWebView2Environment.CreateAsync(null, Path.Combine(directory, "login-browser"));
                var options = environment.CreateCoreWebView2ControllerOptions();
                options.IsInPrivateModeEnabled = true;
                options.ProfileName = "RoomPilotLogin";
                await browser.EnsureCoreWebView2Async(environment, options);
                if (IsDisposed)
                    return;
                var core = browser.CoreWebView2;
                core.Settings.IsPasswordAutosaveEnabled = false;
                core.Settings.IsGeneralAutofillEnabled = false;
                core.Settings.AreDevToolsEnabled = false;
                core.Settings.AreDefaultContextMenusEnabled = false;
                core.PermissionRequested += delegate (object sender, CoreWebView2PermissionRequestedEventArgs e)
                {
                    e.State = CoreWebView2PermissionState.Deny;
                };
                core.NewWindowRequested += delegate (object sender, CoreWebView2NewWindowRequestedEventArgs e)
                {
                    e.Handled = true;
                };
                core.DownloadStarting += delegate (object sender, CoreWebView2DownloadStartingEventArgs e)
                {
                    e.Cancel = true;
                };
                core.ServerCertificateErrorDetected += delegate (object sender, CoreWebView2ServerCertificateErrorDetectedEventArgs e)
                {
                    e.Action = CoreWebView2ServerCertificateErrorAction.Cancel;
                };
                core.NavigationStarting += Navigating;
                core.NavigationCompleted += delegate (object sender, CoreWebView2NavigationCompletedEventArgs e)
                {
                    if (!e.IsSuccess && !completing)
                        status.Text = L10n.Text("error.loginPage");
                };
                SeedCookies(core);
                core.Navigate(login.Url);
            }
            catch (Exception ex)
            {
                status.Text = ex is UserError ? ex.Message : L10n.Text("error.loginBrowser");
            }
        }

        void SeedCookies(CoreWebView2 core)
        {
            string md = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"device_user_dictionary\":[],\"device_registration_data\":{\"software_version\":\"1\"},\"app_identifier\":{\"app_version\":\"" + AlexaClient.ApiVersion + "\",\"bundle_id\":\"com.amazon.echo\"}}"));
            foreach (var pair in new[]
            {
                new KeyValuePair<string, string>("frc", login.Frc),
                new KeyValuePair<string, string>("map-md", md)
            }

            )
            {
                var cookie = core.CookieManager.CreateCookie(pair.Key, pair.Value, ".amazon.com", "/");
                cookie.IsSecure = true;
                cookie.IsHttpOnly = true;
                core.CookieManager.AddOrUpdateCookie(cookie);
            }
        }

        void SetStep(string step)
        {
            if (IsDisposed || Disposing)
                return;
            Action update = delegate
            {
                status.Text = L10n.Format("login.connecting", AlexaDiagnostic.Label(step));
            };
            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(update);
                }
                catch (InvalidOperationException)
                {
                }
            }
            else
                update();
        }

        async void Navigating(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            Uri uri;
            if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out uri) || !AmazonHosts.LoginUrl(uri))
            {
                e.Cancel = true;
                status.Text = L10n.Text("error.loginHost");
                return;
            }

            location.Text = L10n.Format("login.secure", uri.Host);
            string code = AmazonLoginRequest.AuthorizationCode(uri);
            if (String.IsNullOrEmpty(code))
                return;
            e.Cancel = true;
            if (completing)
                return;
            completing = true;
            browser.Enabled = false;
            status.Text = L10n.Text("login.authorized");
            try
            {
                var cookies = await browser.CoreWebView2.CookieManager.GetCookiesAsync("https://www.amazon.com");
                var values = new Dictionary<string, string>();
                foreach (var c in cookies)
                    values[c.Name] = c.Value;
                using (var client = new AlexaClient(null, null))
                {
                    client.Progress = SetStep;
                    client.Diagnostic = d => d.Save(directory);
                    var session = await client.RegisterAsync(code, login.Verifier, login.ClientId, login.Serial, values, domain);
                    var devices = await client.ListDevicesAsync();
                    if (IsDisposed)
                        return;
                    Session = session;
                    Devices = devices;
                    DialogResult = DialogResult.OK;
                    Close();
                }
            }
            catch (Exception ex)
            {
                if (IsDisposed)
                    return;
                browser.Visible = false;
                retry.Visible = true;
                status.Text = ex is UserError || ex is AlexaLoginRequired ? ex.Message : L10n.Text("error.loginFinish");
                location.Text = L10n.Text("error.loginNoAction");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                browser.Dispose();
            base.Dispose(disposing);
        }
    }
}
