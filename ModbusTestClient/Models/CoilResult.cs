namespace ModbusTestClient.Models
{
    public class CoilResult
    {
        public ushort Address { get; set; }
        public bool Value { get; set; }
        public string StateText => Value ? "ON" : "OFF";
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string TimestampStr => Timestamp.ToString("HH:mm:ss.fff");
    }
}
