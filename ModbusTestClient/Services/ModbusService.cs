using System.Net.Sockets;
using NModbus;

namespace ModbusTestClient.Services
{
    public class ModbusService : IDisposable
    {
        private TcpClient? _tcpClient;
        private IModbusMaster? _master;
        private readonly object _lock = new();

        public bool IsConnected => _tcpClient?.Connected == true && _master != null;

        public event Action<string, bool>? OnLog; // message, isError

        public async Task ConnectAsync(string ipAddress, int port, int timeoutMs)
        {
            await Task.Run(() =>
            {
                lock (_lock)
                {
                    Disconnect();

                    _tcpClient = new TcpClient();
                    _tcpClient.ReceiveTimeout = timeoutMs;
                    _tcpClient.SendTimeout = timeoutMs;

                    var result = _tcpClient.BeginConnect(ipAddress, port, null, null);
                    bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(timeoutMs));

                    if (!success)
                    {
                        _tcpClient.Close();
                        _tcpClient = null;
                        throw new TimeoutException($"Connection to {ipAddress}:{port} timed out after {timeoutMs}ms");
                    }

                    _tcpClient.EndConnect(result);

                    var factory = new ModbusFactory();
                    _master = factory.CreateMaster(_tcpClient);
                    _master.Transport.ReadTimeout = timeoutMs;
                    _master.Transport.WriteTimeout = timeoutMs;
                    _master.Transport.Retries = 1;

                    Log($"Connected to {ipAddress}:{port}");
                }
            });
        }

        public void Disconnect()
        {
            lock (_lock)
            {
                try
                {
                    _master?.Dispose();
                    _tcpClient?.Close();
                    _tcpClient?.Dispose();
                }
                catch { }
                finally
                {
                    _master = null;
                    _tcpClient = null;
                }
            }
        }

        /// <summary>
        /// FC3 - Read Holding Registers
        /// </summary>
        public async Task<ushort[]> ReadHoldingRegistersAsync(byte slaveId, ushort startAddress, ushort quantity)
        {
            EnsureConnected();
            Log($"TX → FC3 Read Holding Registers: SlaveID={slaveId}, Start={startAddress}, Qty={quantity}");

            var result = await Task.Run(() =>
            {
                lock (_lock)
                {
                    return _master!.ReadHoldingRegisters(slaveId, startAddress, quantity);
                }
            });

            Log($"RX ← FC3 Response: {string.Join(", ", result.Select(r => $"{r} (0x{r:X4})"))}");
            return result;
        }

        /// <summary>
        /// FC6 - Write Single Register
        /// </summary>
        public async Task WriteSingleRegisterAsync(byte slaveId, ushort address, ushort value)
        {
            EnsureConnected();
            Log($"TX → FC6 Write Single Register: SlaveID={slaveId}, Addr={address}, Value={value} (0x{value:X4})");

            await Task.Run(() =>
            {
                lock (_lock)
                {
                    _master!.WriteSingleRegister(slaveId, address, value);
                }
            });

            Log($"RX ← FC6 Write OK");
        }

        /// <summary>
        /// FC16 - Write Multiple Registers
        /// </summary>
        public async Task WriteMultipleRegistersAsync(byte slaveId, ushort startAddress, ushort[] values)
        {
            EnsureConnected();
            Log($"TX → FC16 Write Multiple Registers: SlaveID={slaveId}, Start={startAddress}, Values=[{string.Join(", ", values)}]");

            await Task.Run(() =>
            {
                lock (_lock)
                {
                    _master!.WriteMultipleRegisters(slaveId, startAddress, values);
                }
            });

            Log($"RX ← FC16 Write OK ({values.Length} registers written)");
        }

        /// <summary>
        /// FC1 - Read Coils
        /// </summary>
        public async Task<bool[]> ReadCoilsAsync(byte slaveId, ushort startAddress, ushort quantity)
        {
            EnsureConnected();
            Log($"TX → FC1 Read Coils: SlaveID={slaveId}, Start={startAddress}, Qty={quantity}");

            var result = await Task.Run(() =>
            {
                lock (_lock)
                {
                    return _master!.ReadCoils(slaveId, startAddress, quantity);
                }
            });

            Log($"RX ← FC1 Response: {string.Join(", ", result.Select(r => r ? "ON" : "OFF"))}");
            return result;
        }

        /// <summary>
        /// FC5 - Write Single Coil
        /// </summary>
        public async Task WriteSingleCoilAsync(byte slaveId, ushort address, bool value)
        {
            EnsureConnected();
            Log($"TX → FC5 Write Single Coil: SlaveID={slaveId}, Addr={address}, Value={value}");

            await Task.Run(() =>
            {
                lock (_lock)
                {
                    _master!.WriteSingleCoil(slaveId, address, value);
                }
            });

            Log($"RX ← FC5 Write OK");
        }

        private void EnsureConnected()
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected to Modbus server. Please connect first.");
        }

        private void Log(string message, bool isError = false)
        {
            OnLog?.Invoke(message, isError);
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
