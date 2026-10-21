using System;
using System.Threading.Tasks;

namespace RoomPilot
{
    public interface IDeviceService : IDisposable
    {
        bool IsConnected { get; }

        Task<bool?> ReadPowerAsync(string deviceId);
        Task SetPowerAsync(string deviceId, bool power);
    }
}
