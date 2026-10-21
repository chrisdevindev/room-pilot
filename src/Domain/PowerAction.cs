namespace RoomPilot
{
    public enum PowerAction
    {
        Toggle,
        On,
        Off
    }

    public static class DeviceActions
    {
        public static PowerAction Parse(string value)
        {
            switch (value)
            {
                case "toggle":
                    return PowerAction.Toggle;
                case "on":
                    return PowerAction.On;
                case "off":
                    return PowerAction.Off;
                default:
                    throw new UserError(L10n.Text("error.chooseAction"));
            }
        }

        public static string Label(PowerAction action)
        {
            return action == PowerAction.On ? L10n.Text("action.on") : action == PowerAction.Off ? L10n.Text("action.off") : L10n.Text("action.toggle");
        }
    }
}
