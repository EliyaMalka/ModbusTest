using System.IO;
using Newtonsoft.Json;

namespace ModbusTestClient.Core.Models
{
    public class ConnectionSettings
    {
        public string IpAddress { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 502;
        public byte SlaveId { get; set; } = 1;
        public int TimeoutMs { get; set; } = 3000;
        public int AutoRefreshIntervalMs { get; set; } = 1000;

        private static readonly string SettingsPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "connection_settings.json");

        public void Save()
        {
            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(SettingsPath, json);
            }
            catch { }
        }

        public static ConnectionSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    return JsonConvert.DeserializeObject<ConnectionSettings>(json) ?? new ConnectionSettings();
                }
            }
            catch { }
            return new ConnectionSettings();
        }
    }
}
