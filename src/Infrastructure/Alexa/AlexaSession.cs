using System;

namespace RoomPilot
{
    public sealed class AlexaSession
    {
        public string Domain { get; set; }
        public string RefreshToken { get; set; }
        public string Cookie { get; set; }
        public string Csrf { get; set; }
        public DateTime CookieDate { get; set; }
    }
}
