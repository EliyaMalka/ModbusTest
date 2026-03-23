namespace ModbusTestClient.Core.Models
{
    public class RegisterResult
    {
        public ushort Address { get; set; }
        public ushort Value { get; set; }
        public string HexValue => $"0x{Value:X4}";
        public string BinaryValue => Convert.ToString(Value, 2).PadLeft(16, '0');
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string TimestampStr => Timestamp.ToString("HH:mm:ss.fff");
    }
}
