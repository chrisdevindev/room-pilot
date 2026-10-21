using System.IO;

namespace RoomPilot
{
    public sealed class AlexaSessionStore
    {
        readonly string file;
        public AlexaSessionStore(string directory)
        {
            file = Path.Combine(directory, "amazon-session.dat");
        }

        public bool Exists
        {
            get
            {
                return File.Exists(file);
            }
        }

        public AlexaSession Load()
        {
            if (!Exists)
                return null;
            try
            {
                return Json.Serializer().Deserialize<AlexaSession>(DataProtection.Unprotect(File.ReadAllText(file)));
            }
            catch
            {
                throw new UserError(L10n.Text("error.sessionRead"));
            }
        }

        public void Save(AlexaSession session)
        {
            PrivateStorage.Write(file, DataProtection.Protect(Json.Serializer().Serialize(session)));
        }

        public void Delete()
        {
            if (Exists)
                File.Delete(file);
        }
    }
}
