using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace RoomPilot
{
    public static class Startup
    {
        const string RegistryPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
        const string Name = "room-pilot";
        const string LegacyName = "CasaInteligente";
        public static void UpgradeLegacyEntry()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RegistryPath, true))
                {
                    if (key == null || key.GetValue(Name) != null)
                        return;
                    string legacy = key.GetValue(LegacyName) as string;
                    string expected = "\"" + Path.Combine(Application.StartupPath, "Casa inteligente.exe") + "\" --tray";
                    if (legacy != expected)
                        return;
                    key.SetValue(Name, Command());
                    key.DeleteValue(LegacyName, false);
                }
            }
            // Startup migration is optional; a restricted registry must not block the app.
            catch (System.Security.SecurityException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        public static bool IsEnabled()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RegistryPath))
                {
                    string command = key == null ? null : key.GetValue(Name) as string;
                    return !String.IsNullOrEmpty(command) && command == Command();
                }
            }
            catch (System.Security.SecurityException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        static string Command()
        {
            return "\"" + Application.ExecutablePath + "\" --tray";
        }

        public static void Set(bool enabled)
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(RegistryPath))
                {
                    if (enabled)
                        key.SetValue(Name, Command());
                    else
                        key.DeleteValue(Name, false);
                }
            }
            catch (System.Security.SecurityException)
            {
                throw new UserError(L10n.Text("error.startup"));
            }
            catch (UnauthorizedAccessException)
            {
                throw new UserError(L10n.Text("error.startupAccess"));
            }
        }
    }
}
