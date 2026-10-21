namespace RoomPilot
{
    public sealed class DeviceConfig
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public string Brand { get; set; }
        public string DeviceId { get; set; }

        public DeviceConfig()
        {
            DeviceId = "";
            Brand = "";
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
