using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using ModbusTestClient.GUI.Helpers;
using ModbusTestClient.Core.Models;
using ModbusTestClient.Core.Services;

namespace ModbusTestClient.GUI.ViewModels
{
    public class MainViewModel : BaseViewModel, IDisposable
    {
        private readonly ModbusService _modbusService;
        private readonly DispatcherTimer _autoRefreshTimer;
        private ConnectionSettings _settings;

        public MainViewModel()
        {
            _modbusService = new ModbusService();
            _modbusService.OnLog += (msg, isError) => AddLog(msg, isError ? LogLevel.Error : LogLevel.Info);

            _settings = ConnectionSettings.Load();

            _autoRefreshTimer = new DispatcherTimer();
            _autoRefreshTimer.Tick += async (s, e) => await AutoRefreshTick();

            ConnectCommand = new AsyncRelayCommand(ConnectAsync, () => !IsConnected);
            DisconnectCommand = new RelayCommand(Disconnect, () => IsConnected);
            ReadHoldingRegistersCommand = new AsyncRelayCommand(ReadHoldingRegistersAsync, () => IsConnected);
            WriteSingleRegisterCommand = new AsyncRelayCommand(WriteSingleRegisterAsync, () => IsConnected);
            WriteMultipleRegistersCommand = new AsyncRelayCommand(WriteMultipleRegistersAsync, () => IsConnected);
            ReadCoilsCommand = new AsyncRelayCommand(ReadCoilsAsync, () => IsConnected);
            WriteSingleCoilCommand = new AsyncRelayCommand(WriteSingleCoilAsync, () => IsConnected);
            ClearLogCommand = new RelayCommand(ClearLog);
            ExportLogCommand = new RelayCommand(ExportLog);
            RunBatchTestCommand = new AsyncRelayCommand(RunBatchTestAsync, () => IsConnected);

            IpAddress = _settings.IpAddress;
            Port = _settings.Port.ToString();
            SlaveId = _settings.SlaveId.ToString();
            TimeoutMs = _settings.TimeoutMs.ToString();
            AutoRefreshInterval = _settings.AutoRefreshIntervalMs.ToString();

            AddLog("Modbus Test Client initialized. Ready to connect.", LogLevel.Info);
        }

        // ========== Connection Properties ==========
        private string _ipAddress = "127.0.0.1";
        public string IpAddress { get => _ipAddress; set => SetProperty(ref _ipAddress, value); }

        private string _port = "502";
        public string Port { get => _port; set => SetProperty(ref _port, value); }

        private string _slaveId = "1";
        public string SlaveId { get => _slaveId; set => SetProperty(ref _slaveId, value); }

        private string _timeoutMs = "3000";
        public string TimeoutMs { get => _timeoutMs; set => SetProperty(ref _timeoutMs, value); }

        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set { if (SetProperty(ref _isConnected, value)) OnPropertyChanged(nameof(IsNotConnected)); }
        }
        public bool IsNotConnected => !IsConnected;

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }

        private string _statusText = "Disconnected";
        public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }

        // ========== Read Holding Registers (FC3) ==========
        private string _readRegStartAddress = "0";
        public string ReadRegStartAddress { get => _readRegStartAddress; set => SetProperty(ref _readRegStartAddress, value); }

        private string _readRegQuantity = "10";
        public string ReadRegQuantity { get => _readRegQuantity; set => SetProperty(ref _readRegQuantity, value); }

        private ObservableCollection<RegisterResult> _registerResults = new();
        public ObservableCollection<RegisterResult> RegisterResults { get => _registerResults; set => SetProperty(ref _registerResults, value); }

        // ========== Write Single Register (FC6) ==========
        private string _writeSingleAddress = "0";
        public string WriteSingleAddress { get => _writeSingleAddress; set => SetProperty(ref _writeSingleAddress, value); }

        private string _writeSingleValue = "0";
        public string WriteSingleValue { get => _writeSingleValue; set => SetProperty(ref _writeSingleValue, value); }

        // ========== Write Multiple Registers (FC16) ==========
        private string _writeMultiStartAddress = "0";
        public string WriteMultiStartAddress { get => _writeMultiStartAddress; set => SetProperty(ref _writeMultiStartAddress, value); }

        private string _writeMultiValues = "0,0,0";
        public string WriteMultiValues { get => _writeMultiValues; set => SetProperty(ref _writeMultiValues, value); }

        // ========== Read Coils (FC1) ==========
        private string _readCoilStartAddress = "0";
        public string ReadCoilStartAddress { get => _readCoilStartAddress; set => SetProperty(ref _readCoilStartAddress, value); }

        private string _readCoilQuantity = "10";
        public string ReadCoilQuantity { get => _readCoilQuantity; set => SetProperty(ref _readCoilQuantity, value); }

        private ObservableCollection<CoilResult> _coilResults = new();
        public ObservableCollection<CoilResult> CoilResults { get => _coilResults; set => SetProperty(ref _coilResults, value); }

        // ========== Write Single Coil (FC5) ==========
        private string _writeCoilAddress = "0";
        public string WriteCoilAddress { get => _writeCoilAddress; set => SetProperty(ref _writeCoilAddress, value); }

        private bool _writeCoilValue;
        public bool WriteCoilValue { get => _writeCoilValue; set => SetProperty(ref _writeCoilValue, value); }

        // ========== Auto Refresh ==========
        private bool _autoRefreshEnabled;
        public bool AutoRefreshEnabled
        {
            get => _autoRefreshEnabled;
            set
            {
                if (SetProperty(ref _autoRefreshEnabled, value))
                {
                    if (value)
                    {
                        if (int.TryParse(AutoRefreshInterval, out int interval) && interval > 0)
                        {
                            _autoRefreshTimer.Interval = TimeSpan.FromMilliseconds(interval);
                            _autoRefreshTimer.Start();
                            AddLog($"Auto-refresh started ({interval}ms interval)", LogLevel.Info);
                        }
                    }
                    else
                    {
                        _autoRefreshTimer.Stop();
                        AddLog("Auto-refresh stopped", LogLevel.Info);
                    }
                }
            }
        }

        private string _autoRefreshInterval = "1000";
        public string AutoRefreshInterval { get => _autoRefreshInterval; set => SetProperty(ref _autoRefreshInterval, value); }

        // ========== Log ==========
        private ObservableCollection<LogEntry> _logEntries = new();
        public ObservableCollection<LogEntry> LogEntries { get => _logEntries; set => SetProperty(ref _logEntries, value); }

        // ========== Commands ==========
        public AsyncRelayCommand ConnectCommand { get; }
        public RelayCommand DisconnectCommand { get; }
        public AsyncRelayCommand ReadHoldingRegistersCommand { get; }
        public AsyncRelayCommand WriteSingleRegisterCommand { get; }
        public AsyncRelayCommand WriteMultipleRegistersCommand { get; }
        public AsyncRelayCommand ReadCoilsCommand { get; }
        public AsyncRelayCommand WriteSingleCoilCommand { get; }
        public RelayCommand ClearLogCommand { get; }
        public RelayCommand ExportLogCommand { get; }
        public AsyncRelayCommand RunBatchTestCommand { get; }

        // ========== Command Implementations ==========

        private async Task ConnectAsync()
        {
            try
            {
                IsBusy = true;
                StatusText = "Connecting...";
                if (!int.TryParse(Port, out int port)) throw new ArgumentException("Invalid port number");
                if (!int.TryParse(TimeoutMs, out int timeout)) throw new ArgumentException("Invalid timeout value");

                await _modbusService.ConnectAsync(IpAddress, port, timeout);
                IsConnected = true;
                StatusText = $"Connected to {IpAddress}:{port}";
                AddLog($"Successfully connected to {IpAddress}:{port}", LogLevel.Success);

                _settings.IpAddress = IpAddress;
                _settings.Port = port;
                _settings.SlaveId = byte.TryParse(SlaveId, out byte sid) ? sid : (byte)1;
                _settings.TimeoutMs = timeout;
                _settings.Save();
            }
            catch (Exception ex)
            {
                IsConnected = false;
                StatusText = "Connection failed";
                AddLog($"Connection failed: {ex.Message}", LogLevel.Error);
                MessageBox.Show($"Failed to connect:\n{ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsBusy = false; }
        }

        private void Disconnect()
        {
            try
            {
                AutoRefreshEnabled = false;
                _modbusService.Disconnect();
                IsConnected = false;
                StatusText = "Disconnected";
                AddLog("Disconnected from server", LogLevel.Info);
            }
            catch (Exception ex) { AddLog($"Disconnect error: {ex.Message}", LogLevel.Error); }
        }

        private byte GetSlaveId() => byte.TryParse(SlaveId, out byte sid) ? sid : (byte)1;

        private async Task ReadHoldingRegistersAsync()
        {
            try
            {
                IsBusy = true;
                if (!ushort.TryParse(ReadRegStartAddress, out ushort startAddr)) throw new ArgumentException("Invalid start address");
                if (!ushort.TryParse(ReadRegQuantity, out ushort qty) || qty == 0) throw new ArgumentException("Invalid quantity (must be 1-125)");

                var values = await _modbusService.ReadHoldingRegistersAsync(GetSlaveId(), startAddr, qty);
                RegisterResults.Clear();
                for (int i = 0; i < values.Length; i++)
                    RegisterResults.Add(new RegisterResult { Address = (ushort)(startAddr + i), Value = values[i], Timestamp = DateTime.Now });

                AddLog($"FC3: Read {values.Length} registers starting from address {startAddr}", LogLevel.Success);
            }
            catch (Exception ex)
            {
                AddLog($"FC3 Error: {ex.Message}", LogLevel.Error);
                MessageBox.Show($"Read error:\n{ex.Message}", "Read Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally { IsBusy = false; }
        }

        private async Task WriteSingleRegisterAsync()
        {
            try
            {
                IsBusy = true;
                if (!ushort.TryParse(WriteSingleAddress, out ushort addr)) throw new ArgumentException("Invalid address");
                ushort value;
                string valStr = WriteSingleValue.Trim();
                if (valStr.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    value = Convert.ToUInt16(valStr, 16);
                else if (!ushort.TryParse(valStr, out value))
                    throw new ArgumentException("Invalid value (0-65535 or 0x0000-0xFFFF)");

                await _modbusService.WriteSingleRegisterAsync(GetSlaveId(), addr, value);
                AddLog($"FC6: Written value {value} (0x{value:X4}) to address {addr}", LogLevel.Success);
            }
            catch (Exception ex)
            {
                AddLog($"FC6 Error: {ex.Message}", LogLevel.Error);
                MessageBox.Show($"Write error:\n{ex.Message}", "Write Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally { IsBusy = false; }
        }

        private async Task WriteMultipleRegistersAsync()
        {
            try
            {
                IsBusy = true;
                if (!ushort.TryParse(WriteMultiStartAddress, out ushort startAddr)) throw new ArgumentException("Invalid start address");
                var parts = WriteMultiValues.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length == 0) throw new ArgumentException("No values provided. Enter comma-separated values.");

                var values = new ushort[parts.Length];
                for (int i = 0; i < parts.Length; i++)
                {
                    string p = parts[i].Trim();
                    if (p.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                        values[i] = Convert.ToUInt16(p, 16);
                    else if (!ushort.TryParse(p, out values[i]))
                        throw new ArgumentException($"Invalid value at position {i}: '{parts[i]}'");
                }

                await _modbusService.WriteMultipleRegistersAsync(GetSlaveId(), startAddr, values);
                AddLog($"FC16: Written {values.Length} registers starting from address {startAddr}", LogLevel.Success);
            }
            catch (Exception ex)
            {
                AddLog($"FC16 Error: {ex.Message}", LogLevel.Error);
                MessageBox.Show($"Write error:\n{ex.Message}", "Write Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally { IsBusy = false; }
        }

        private async Task ReadCoilsAsync()
        {
            try
            {
                IsBusy = true;
                if (!ushort.TryParse(ReadCoilStartAddress, out ushort startAddr)) throw new ArgumentException("Invalid start address");
                if (!ushort.TryParse(ReadCoilQuantity, out ushort qty) || qty == 0) throw new ArgumentException("Invalid quantity (must be 1-2000)");

                var values = await _modbusService.ReadCoilsAsync(GetSlaveId(), startAddr, qty);
                CoilResults.Clear();
                for (int i = 0; i < values.Length; i++)
                    CoilResults.Add(new CoilResult { Address = (ushort)(startAddr + i), Value = values[i], Timestamp = DateTime.Now });

                AddLog($"FC1: Read {values.Length} coils starting from address {startAddr}", LogLevel.Success);
            }
            catch (Exception ex)
            {
                AddLog($"FC1 Error: {ex.Message}", LogLevel.Error);
                MessageBox.Show($"Read error:\n{ex.Message}", "Read Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally { IsBusy = false; }
        }

        private async Task WriteSingleCoilAsync()
        {
            try
            {
                IsBusy = true;
                if (!ushort.TryParse(WriteCoilAddress, out ushort addr)) throw new ArgumentException("Invalid address");
                await _modbusService.WriteSingleCoilAsync(GetSlaveId(), addr, WriteCoilValue);
                AddLog($"FC5: Written coil at address {addr} = {(WriteCoilValue ? "ON" : "OFF")}", LogLevel.Success);
            }
            catch (Exception ex)
            {
                AddLog($"FC5 Error: {ex.Message}", LogLevel.Error);
                MessageBox.Show($"Write error:\n{ex.Message}", "Write Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally { IsBusy = false; }
        }

        private async Task AutoRefreshTick()
        {
            if (!IsConnected || IsBusy) return;
            try
            {
                if (ushort.TryParse(ReadRegStartAddress, out ushort startAddr) &&
                    ushort.TryParse(ReadRegQuantity, out ushort qty) && qty > 0)
                {
                    var values = await _modbusService.ReadHoldingRegistersAsync(GetSlaveId(), startAddr, qty);
                    RegisterResults.Clear();
                    for (int i = 0; i < values.Length; i++)
                        RegisterResults.Add(new RegisterResult { Address = (ushort)(startAddr + i), Value = values[i], Timestamp = DateTime.Now });
                }
            }
            catch (Exception ex)
            {
                AddLog($"Auto-refresh error: {ex.Message}", LogLevel.Warning);
                AutoRefreshEnabled = false;
            }
        }

        private async Task RunBatchTestAsync()
        {
            try
            {
                IsBusy = true;
                AddLog("═══ Starting Batch Test ═══", LogLevel.Info);
                byte sid = GetSlaveId();
                int passed = 0, failed = 0;

                // Test 1: Write single register
                try
                {
                    AddLog("Test 1: FC6 - Write value 12345 to register 0", LogLevel.Info);
                    await _modbusService.WriteSingleRegisterAsync(sid, 0, 12345);
                    passed++; AddLog("Test 1: PASSED", LogLevel.Success);
                }
                catch (Exception ex) { failed++; AddLog($"Test 1: FAILED - {ex.Message}", LogLevel.Error); }

                // Test 2: Read back and verify
                try
                {
                    AddLog("Test 2: FC3 - Read register 0 and verify value", LogLevel.Info);
                    var result = await _modbusService.ReadHoldingRegistersAsync(sid, 0, 1);
                    if (result[0] == 12345) { passed++; AddLog($"Test 2: PASSED (read value = {result[0]})", LogLevel.Success); }
                    else { failed++; AddLog($"Test 2: FAILED (expected 12345, got {result[0]})", LogLevel.Error); }
                }
                catch (Exception ex) { failed++; AddLog($"Test 2: FAILED - {ex.Message}", LogLevel.Error); }

                // Test 3: Write multiple registers
                try
                {
                    AddLog("Test 3: FC16 - Write values [100, 200, 300] to registers 10-12", LogLevel.Info);
                    await _modbusService.WriteMultipleRegistersAsync(sid, 10, new ushort[] { 100, 200, 300 });
                    passed++; AddLog("Test 3: PASSED", LogLevel.Success);
                }
                catch (Exception ex) { failed++; AddLog($"Test 3: FAILED - {ex.Message}", LogLevel.Error); }

                // Test 4: Read multiple and verify
                try
                {
                    AddLog("Test 4: FC3 - Read registers 10-12 and verify", LogLevel.Info);
                    var result = await _modbusService.ReadHoldingRegistersAsync(sid, 10, 3);
                    if (result[0] == 100 && result[1] == 200 && result[2] == 300) { passed++; AddLog($"Test 4: PASSED (values = [{string.Join(", ", result)}])", LogLevel.Success); }
                    else { failed++; AddLog($"Test 4: FAILED (expected [100,200,300], got [{string.Join(", ", result)}])", LogLevel.Error); }
                }
                catch (Exception ex) { failed++; AddLog($"Test 4: FAILED - {ex.Message}", LogLevel.Error); }

                // Test 5: Write coil ON
                try
                {
                    AddLog("Test 5: FC5 - Write coil 0 = ON", LogLevel.Info);
                    await _modbusService.WriteSingleCoilAsync(sid, 0, true);
                    passed++; AddLog("Test 5: PASSED", LogLevel.Success);
                }
                catch (Exception ex) { failed++; AddLog($"Test 5: FAILED - {ex.Message}", LogLevel.Error); }

                // Test 6: Read coil and verify
                try
                {
                    AddLog("Test 6: FC1 - Read coil 0 and verify ON", LogLevel.Info);
                    var result = await _modbusService.ReadCoilsAsync(sid, 0, 1);
                    if (result[0]) { passed++; AddLog("Test 6: PASSED (coil is ON)", LogLevel.Success); }
                    else { failed++; AddLog("Test 6: FAILED (expected ON, got OFF)", LogLevel.Error); }
                }
                catch (Exception ex) { failed++; AddLog($"Test 6: FAILED - {ex.Message}", LogLevel.Error); }

                // Test 7: Write coil OFF
                try
                {
                    AddLog("Test 7: FC5 - Write coil 0 = OFF", LogLevel.Info);
                    await _modbusService.WriteSingleCoilAsync(sid, 0, false);
                    passed++; AddLog("Test 7: PASSED", LogLevel.Success);
                }
                catch (Exception ex) { failed++; AddLog($"Test 7: FAILED - {ex.Message}", LogLevel.Error); }

                // Test 8: Read coil and verify OFF
                try
                {
                    AddLog("Test 8: FC1 - Read coil 0 and verify OFF", LogLevel.Info);
                    var result = await _modbusService.ReadCoilsAsync(sid, 0, 1);
                    if (!result[0]) { passed++; AddLog("Test 8: PASSED (coil is OFF)", LogLevel.Success); }
                    else { failed++; AddLog("Test 8: FAILED (expected OFF, got ON)", LogLevel.Error); }
                }
                catch (Exception ex) { failed++; AddLog($"Test 8: FAILED - {ex.Message}", LogLevel.Error); }

                AddLog($"═══ Batch Test Complete: {passed} Passed, {failed} Failed ═══",
                    failed == 0 ? LogLevel.Success : LogLevel.Warning);
            }
            catch (Exception ex) { AddLog($"Batch test error: {ex.Message}", LogLevel.Error); }
            finally { IsBusy = false; }
        }

        private void ClearLog()
        {
            LogEntries.Clear();
            AddLog("Log cleared", LogLevel.Info);
        }

        private void ExportLog()
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"modbus_log_{DateTime.Now:yyyyMMdd_HHmmss}",
                    DefaultExt = ".txt",
                    Filter = "Text files (*.txt)|*.txt|CSV files (*.csv)|*.csv|All files (*.*)|*.*"
                };
                if (dialog.ShowDialog() == true)
                {
                    var lines = LogEntries.Select(e => $"{e.TimestampStr}\t{e.LevelText}\t{e.Direction}\t{e.Message}");
                    File.WriteAllLines(dialog.FileName, lines);
                    AddLog($"Log exported to {dialog.FileName}", LogLevel.Success);
                }
            }
            catch (Exception ex) { AddLog($"Export error: {ex.Message}", LogLevel.Error); }
        }

        private void AddLog(string message, LogLevel level)
        {
            string direction = message.Contains("TX →") ? "TX" : message.Contains("RX ←") ? "RX" : "INFO";
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                LogEntries.Insert(0, new LogEntry { Level = level, Message = message, Direction = direction });
                while (LogEntries.Count > 500) LogEntries.RemoveAt(LogEntries.Count - 1);
            });
        }

        public void Dispose()
        {
            _autoRefreshTimer.Stop();
            _modbusService.Dispose();
        }
    }
}
