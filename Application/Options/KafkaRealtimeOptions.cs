namespace FleetBharat.TMSService.Application.Options
{
    public class KafkaRealtimeOptions
    {
        public const string SectionName = "KafkaRealtime";

        public bool Enabled { get; set; }
        public string BootstrapServers { get; set; }
        public string GroupId { get; set; }
        public List<string> Topics { get; set; }
        public string AutoOffsetReset { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string SecurityProtocol { get; set; }
        public string SaslMechanism { get; set; }
    }
}
