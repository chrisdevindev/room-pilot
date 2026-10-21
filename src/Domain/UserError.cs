using System;

namespace RoomPilot
{
    public sealed class UserError : Exception
    {
        public UserError(string message) : base(message)
        {
        }
    }
}
