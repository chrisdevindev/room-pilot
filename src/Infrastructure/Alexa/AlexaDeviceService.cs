using System.Threading.Tasks;

namespace RoomPilot
{
    public sealed class AlexaDeviceService : IDeviceService
    {
        readonly AlexaClient client;
        public bool IsConnected
        {
            get
            {
                return client.Connected;
            }
        }

        public AlexaDeviceService(string directory)
        {
            var sessions = new AlexaSessionStore(directory);
            client = new AlexaClient(sessions.Load(), sessions);
            client.Diagnostic = diagnostic => diagnostic.Save(directory);
        }

        public async Task<bool?> ReadPowerAsync(string deviceId)
        {
            try
            {
                return await client.ReadPowerAsync(deviceId).ConfigureAwait(false);
            }
            catch (AlexaLoginRequired error)
            {
                throw new UserError(error.Message);
            }
        }

        public async Task SetPowerAsync(string deviceId, bool power)
        {
            try
            {
                await client.SetPowerAsync(deviceId, power).ConfigureAwait(false);
            }
            catch (AlexaLoginRequired error)
            {
                throw new UserError(error.Message);
            }
        }

        public void Dispose()
        {
            client.Dispose();
        }
    }
}
