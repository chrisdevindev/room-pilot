using System;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;

namespace RoomPilot
{
    public static class PrivateStorage
    {
        public static string DefaultDirectory
        {
            get
            {
                // Keep the existing private folder so the rename preserves accounts and browser data.
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CasaInteligente");
            }
        }

        public static void SecureDirectory(string directory)
        {
            Directory.CreateDirectory(directory);
            var security = new DirectorySecurity();
            security.SetAccessRuleProtection(true, false);
            var inheritance = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;
            security.AddAccessRule(new FileSystemAccessRule(WindowsIdentity.GetCurrent().User, FileSystemRights.FullControl, inheritance, PropagationFlags.None, AccessControlType.Allow));
            security.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), FileSystemRights.FullControl, inheritance, PropagationFlags.None, AccessControlType.Allow));
            Directory.SetAccessControl(directory, security);
        }

        public static void Write(string file, string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            string temporary = file + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                byte[] bytes = new UTF8Encoding(false).GetBytes(content);
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }

                if (File.Exists(file))
                    File.Replace(temporary, file, null);
                else
                    File.Move(temporary, file);
            }
            finally
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }
        }

        // Import only the old files next to this application's executable. Never inspect
        // another browser/app profile. Originals remain intact until migration is verified.
        public static bool ImportLegacy(string source, string destination)
        {
            var target = new ConfigStore(destination);
            var sessions = new AlexaSessionStore(destination);
            if (File.Exists(target.FilePath) || sessions.Exists)
                return false;
            string legacy = Path.Combine(source, "config.json");
            if (!File.Exists(legacy))
                return false;
            var config = ConfigurationRules.Upgrade(Json.Serializer().Deserialize<AppConfig>(File.ReadAllText(legacy, Encoding.UTF8)));
            ConfigStore.Validate(config);
            var session = new AlexaSessionStore(source).Load();
            if (session != null)
                AmazonHosts.Require(session.Domain);
            SecureDirectory(destination);
            bool wroteSession = false;
            try
            {
                if (session != null)
                {
                    sessions.Save(session);
                    wroteSession = true;
                }

                target.Save(config);
                if (!target.Load().Devices.Select(d => d.DeviceId).SequenceEqual(config.Devices.Select(d => d.DeviceId)) || (session != null && sessions.Load().RefreshToken != session.RefreshToken))
                    throw new IOException(L10n.Text("error.migration"));
                return true;
            }
            catch
            {
                if (wroteSession)
                    sessions.Delete();
                if (File.Exists(target.FilePath))
                    File.Delete(target.FilePath);
                throw;
            }
        }
    }
}
