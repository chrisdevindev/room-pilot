using System;

namespace RoomPilot
{
    public sealed class AlexaLoginRequired : Exception
    {
        public AlexaLoginRequired() : base(L10n.Text("error.loginRequired"))
        {
        }
    }
}
