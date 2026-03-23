namespace ModbusTestClient.Core.Models
{
    public enum LogLevel
    {
        Info,
        Success,
        Warning,
        Error
    }

    public class LogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string TimestampStr => Timestamp.ToString("HH:mm:ss.fff");
        public LogLevel Level { get; set; }
        public string LevelText => Level.ToString().ToUpper();
        public string Message { get; set; } = string.Empty;
        public string Direction { get; set; } = string.Empty;
    }
}
