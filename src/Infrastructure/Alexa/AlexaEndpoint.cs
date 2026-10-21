namespace RoomPilot
{
    public sealed class AlexaEndpoint
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Manufacturer { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
