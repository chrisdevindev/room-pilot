using System;
using System.Security.Cryptography;
using System.Text;

namespace RoomPilot
{
    public static class DataProtection
    {
        public static string Protect(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return "";
            return Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(value), null, DataProtectionScope.CurrentUser));
        }

        public static string Unprotect(string encrypted)
        {
            if (String.IsNullOrEmpty(encrypted))
                return "";
            try
            {
                return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(encrypted), null, DataProtectionScope.CurrentUser));
            }
            catch
            {
                throw new UserError(L10n.Text("error.protectedData"));
            }
        }
    }
}
