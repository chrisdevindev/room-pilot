using System;

namespace RoomPilot
{
    public sealed class DeviceState
    {
        public bool? Power { get; set; }
        public bool Online { get; set; }
        public string Message { get; set; }
        public bool CanControl { get; set; }
        public bool Busy { get; set; }
        public DateTime? CheckedAt { get; set; }

        public DeviceState()
        {
            Message = L10n.Text("state.pending");
        }
    }
}
