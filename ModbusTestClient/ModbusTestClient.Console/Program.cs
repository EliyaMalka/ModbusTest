using ModbusTestClient.Core.Models;
using ModbusTestClient.Core.Services;

namespace ModbusTestClient.Console
{
    class Program
    {
        private static ModbusService _modbusService = new();
        private static ConnectionSettings _settings = ConnectionSettings.Load();
        private static byte _slaveId = 1;

        static async Task Main(string[] args)
        {
            System.Console.OutputEncoding = System.Text.Encoding.UTF8;

            _modbusService.OnLog += (msg, isError) =>
            {
                var prevColor = System.Console.ForegroundColor;
                System.Console.ForegroundColor = isError ? ConsoleColor.Red : ConsoleColor.DarkGray;
                System.Console.WriteLine($"  [{DateTime.Now:HH:mm:ss.fff}] {msg}");
                System.Console.ForegroundColor = prevColor;
            };

            PrintHeader();

            // Check for command-line args: --ip 127.0.0.1 --port 502 --slave 1
            ParseArgs(args);

            while (true)
            {
                PrintMenu();
                string? choice = System.Console.ReadLine()?.Trim();

                try
                {
                    switch (choice)
                    {
                        case "1": await ConnectAsync(); break;
                        case "2": Disconnect(); break;
                        case "3": await ReadHoldingRegistersAsync(); break;
                        case "4": await WriteSingleRegisterAsync(); break;
                        case "5": await WriteMultipleRegistersAsync(); break;
                        case "6": await ReadCoilsAsync(); break;
                        case "7": await WriteSingleCoilAsync(); break;
                        case "8": await RunBatchTestAsync(); break;
                        case "9": ChangeSettings(); break;
                        case "0":
                        case "q":
                        case "Q":
                            _modbusService.Dispose();
                            PrintColor("Goodbye!", ConsoleColor.Cyan);
                            return;
                        default:
                            PrintColor("Invalid option. Try again.", ConsoleColor.Yellow);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    PrintColor($"Error: {ex.Message}", ConsoleColor.Red);
                }

                System.Console.WriteLine();
            }
        }

        static void ParseArgs(string[] args)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                switch (args[i].ToLower())
                {
                    case "--ip": _settings.IpAddress = args[++i]; break;
                    case "--port": _settings.Port = int.Parse(args[++i]); break;
                    case "--slave": _slaveId = byte.Parse(args[++i]); break;
                    case "--timeout": _settings.TimeoutMs = int.Parse(args[++i]); break;
                }
            }
        }

        static void PrintHeader()
        {
            System.Console.Clear();
            PrintColor("╔══════════════════════════════════════════════╗", ConsoleColor.Cyan);
            PrintColor("║       Modbus TCP Test Client (Console)      ║", ConsoleColor.Cyan);
            PrintColor("║     FC1 | FC3 | FC5 | FC6 | FC16 Support   ║", ConsoleColor.Cyan);
            PrintColor("╚══════════════════════════════════════════════╝", ConsoleColor.Cyan);
            System.Console.WriteLine();
        }

        static void PrintMenu()
        {
            string status = _modbusService.IsConnected
                ? $"CONNECTED to {_settings.IpAddress}:{_settings.Port} (Slave {_slaveId})"
                : "DISCONNECTED";

            var statusColor = _modbusService.IsConnected ? ConsoleColor.Green : ConsoleColor.Red;

            System.Console.WriteLine("─────────────────────────────────────────────");
            System.Console.Write("  Status: ");
            PrintColor(status, statusColor);
            System.Console.WriteLine("─────────────────────────────────────────────");
            System.Console.WriteLine("  1) Connect");
            System.Console.WriteLine("  2) Disconnect");
            System.Console.WriteLine("  ─── Registers ───");
            System.Console.WriteLine("  3) Read Holding Registers  (FC3)");
            System.Console.WriteLine("  4) Write Single Register   (FC6)");
            System.Console.WriteLine("  5) Write Multiple Registers(FC16)");
            System.Console.WriteLine("  ─── Coils ───");
            System.Console.WriteLine("  6) Read Coils              (FC1)");
            System.Console.WriteLine("  7) Write Single Coil       (FC5)");
            System.Console.WriteLine("  ─── Tools ───");
            System.Console.WriteLine("  8) Run Batch Test");
            System.Console.WriteLine("  9) Change Connection Settings");
            System.Console.WriteLine("  0) Exit");
            System.Console.Write("\n  Choose [0-9]: ");
        }

        static async Task ConnectAsync()
        {
            if (_modbusService.IsConnected)
            {
                PrintColor("Already connected. Disconnect first.", ConsoleColor.Yellow);
                return;
            }

            System.Console.Write($"  IP Address [{_settings.IpAddress}]: ");
            string? ip = System.Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(ip)) _settings.IpAddress = ip;

            System.Console.Write($"  Port [{_settings.Port}]: ");
            string? portStr = System.Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(portStr) && int.TryParse(portStr, out int port)) _settings.Port = port;

            System.Console.Write($"  Slave ID [{_slaveId}]: ");
            string? slaveStr = System.Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(slaveStr) && byte.TryParse(slaveStr, out byte sid)) _slaveId = sid;

            PrintColor($"  Connecting to {_settings.IpAddress}:{_settings.Port}...", ConsoleColor.Yellow);

            await _modbusService.ConnectAsync(_settings.IpAddress, _settings.Port, _settings.TimeoutMs);
            _settings.SlaveId = _slaveId;
            _settings.Save();

            PrintColor($"  Connected successfully!", ConsoleColor.Green);
        }

        static void Disconnect()
        {
            if (!_modbusService.IsConnected)
            {
                PrintColor("Not connected.", ConsoleColor.Yellow);
                return;
            }

            _modbusService.Disconnect();
            PrintColor("  Disconnected.", ConsoleColor.Green);
        }

        static async Task ReadHoldingRegistersAsync()
        {
            EnsureConnected();

            ushort startAddr = ReadUShort("Start Address", 0);
            ushort quantity = ReadUShort("Quantity", 10);

            var values = await _modbusService.ReadHoldingRegistersAsync(_slaveId, startAddr, quantity);

            System.Console.WriteLine();
            PrintColor("  ┌─────────┬────────────┬────────────┬──────────────────┐", ConsoleColor.White);
            PrintColor("  │ Address │ Value(Dec) │ Value(Hex) │     Binary       │", ConsoleColor.White);
            PrintColor("  ├─────────┼────────────┼────────────┼──────────────────┤", ConsoleColor.White);

            for (int i = 0; i < values.Length; i++)
            {
                ushort addr = (ushort)(startAddr + i);
                string binary = Convert.ToString(values[i], 2).PadLeft(16, '0');
                System.Console.WriteLine($"  │ {addr,7} │ {values[i],10} │ 0x{values[i]:X4}     │ {binary} │");
            }

            PrintColor("  └─────────┴────────────┴────────────┴──────────────────┘", ConsoleColor.White);
            PrintColor($"  Read {values.Length} registers successfully.", ConsoleColor.Green);
        }

        static async Task WriteSingleRegisterAsync()
        {
            EnsureConnected();

            ushort addr = ReadUShort("Address", 0);
            ushort value = ReadUShortOrHex("Value (decimal or 0xHex)");

            await _modbusService.WriteSingleRegisterAsync(_slaveId, addr, value);
            PrintColor($"  Written value {value} (0x{value:X4}) to address {addr}.", ConsoleColor.Green);
        }

        static async Task WriteMultipleRegistersAsync()
        {
            EnsureConnected();

            ushort startAddr = ReadUShort("Start Address", 0);
            System.Console.Write("  Values (comma separated, e.g. 100,200,0xFF): ");
            string? input = System.Console.ReadLine()?.Trim() ?? "";

            var parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0) throw new ArgumentException("No values provided.");

            var values = new ushort[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                string p = parts[i].Trim();
                if (p.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    values[i] = Convert.ToUInt16(p, 16);
                else if (!ushort.TryParse(p, out values[i]))
                    throw new ArgumentException($"Invalid value: '{parts[i]}'");
            }

            await _modbusService.WriteMultipleRegistersAsync(_slaveId, startAddr, values);
            PrintColor($"  Written {values.Length} registers starting from address {startAddr}.", ConsoleColor.Green);
        }

        static async Task ReadCoilsAsync()
        {
            EnsureConnected();

            ushort startAddr = ReadUShort("Start Address", 0);
            ushort quantity = ReadUShort("Quantity", 10);

            var values = await _modbusService.ReadCoilsAsync(_slaveId, startAddr, quantity);

            System.Console.WriteLine();
            PrintColor("  ┌─────────┬───────┐", ConsoleColor.White);
            PrintColor("  │ Address │ State │", ConsoleColor.White);
            PrintColor("  ├─────────┼───────┤", ConsoleColor.White);

            for (int i = 0; i < values.Length; i++)
            {
                ushort addr = (ushort)(startAddr + i);
                string state = values[i] ? " ON " : " OFF";
                var color = values[i] ? ConsoleColor.Green : ConsoleColor.DarkGray;
                System.Console.Write($"  │ {addr,7} │ ");
                PrintColorInline(state, color);
                System.Console.WriteLine("  │");
            }

            PrintColor("  └─────────┴───────┘", ConsoleColor.White);
            PrintColor($"  Read {values.Length} coils successfully.", ConsoleColor.Green);
        }

        static async Task WriteSingleCoilAsync()
        {
            EnsureConnected();

            ushort addr = ReadUShort("Address", 0);
            System.Console.Write("  Value (ON/OFF or 1/0): ");
            string? input = System.Console.ReadLine()?.Trim().ToUpper() ?? "";

            bool value = input switch
            {
                "ON" or "1" or "TRUE" => true,
                "OFF" or "0" or "FALSE" => false,
                _ => throw new ArgumentException("Invalid value. Use ON/OFF or 1/0.")
            };

            await _modbusService.WriteSingleCoilAsync(_slaveId, addr, value);
            PrintColor($"  Written coil at address {addr} = {(value ? "ON" : "OFF")}.", ConsoleColor.Green);
        }

        static async Task RunBatchTestAsync()
        {
            EnsureConnected();

            PrintColor("\n  ═══ Starting Batch Test ═══\n", ConsoleColor.Cyan);
            int passed = 0, failed = 0;

            // Test 1
            if (await RunTest("Test 1: FC6 - Write 12345 to register 0", async () =>
            {
                await _modbusService.WriteSingleRegisterAsync(_slaveId, 0, 12345);
            })) passed++; else failed++;

            // Test 2
            if (await RunTest("Test 2: FC3 - Read register 0, verify = 12345", async () =>
            {
                var r = await _modbusService.ReadHoldingRegistersAsync(_slaveId, 0, 1);
                if (r[0] != 12345) throw new Exception($"Expected 12345, got {r[0]}");
            })) passed++; else failed++;

            // Test 3
            if (await RunTest("Test 3: FC16 - Write [100,200,300] to registers 10-12", async () =>
            {
                await _modbusService.WriteMultipleRegistersAsync(_slaveId, 10, new ushort[] { 100, 200, 300 });
            })) passed++; else failed++;

            // Test 4
            if (await RunTest("Test 4: FC3 - Read registers 10-12, verify", async () =>
            {
                var r = await _modbusService.ReadHoldingRegistersAsync(_slaveId, 10, 3);
                if (r[0] != 100 || r[1] != 200 || r[2] != 300)
                    throw new Exception($"Expected [100,200,300], got [{string.Join(",", r)}]");
            })) passed++; else failed++;

            // Test 5
            if (await RunTest("Test 5: FC5 - Write coil 0 = ON", async () =>
            {
                await _modbusService.WriteSingleCoilAsync(_slaveId, 0, true);
            })) passed++; else failed++;

            // Test 6
            if (await RunTest("Test 6: FC1 - Read coil 0, verify ON", async () =>
            {
                var r = await _modbusService.ReadCoilsAsync(_slaveId, 0, 1);
                if (!r[0]) throw new Exception("Expected ON, got OFF");
            })) passed++; else failed++;

            // Test 7
            if (await RunTest("Test 7: FC5 - Write coil 0 = OFF", async () =>
            {
                await _modbusService.WriteSingleCoilAsync(_slaveId, 0, false);
            })) passed++; else failed++;

            // Test 8
            if (await RunTest("Test 8: FC1 - Read coil 0, verify OFF", async () =>
            {
                var r = await _modbusService.ReadCoilsAsync(_slaveId, 0, 1);
                if (r[0]) throw new Exception("Expected OFF, got ON");
            })) passed++; else failed++;

            System.Console.WriteLine();
            var resultColor = failed == 0 ? ConsoleColor.Green : ConsoleColor.Yellow;
            PrintColor($"  ═══ Batch Test Complete: {passed} Passed, {failed} Failed ═══", resultColor);
        }

        static async Task<bool> RunTest(string name, Func<Task> test)
        {
            System.Console.Write($"  {name}... ");
            try
            {
                await test();
                PrintColor("PASSED", ConsoleColor.Green);
                return true;
            }
            catch (Exception ex)
            {
                PrintColor($"FAILED - {ex.Message}", ConsoleColor.Red);
                return false;
            }
        }

        static void ChangeSettings()
        {
            System.Console.Write($"  IP Address [{_settings.IpAddress}]: ");
            string? ip = System.Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(ip)) _settings.IpAddress = ip;

            System.Console.Write($"  Port [{_settings.Port}]: ");
            string? portStr = System.Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(portStr) && int.TryParse(portStr, out int port)) _settings.Port = port;

            System.Console.Write($"  Slave ID [{_slaveId}]: ");
            string? slaveStr = System.Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(slaveStr) && byte.TryParse(slaveStr, out byte sid)) _slaveId = sid;

            System.Console.Write($"  Timeout ms [{_settings.TimeoutMs}]: ");
            string? timeoutStr = System.Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(timeoutStr) && int.TryParse(timeoutStr, out int timeout)) _settings.TimeoutMs = timeout;

            _settings.SlaveId = _slaveId;
            _settings.Save();
            PrintColor("  Settings saved.", ConsoleColor.Green);
        }

        // ========== Helpers ==========

        static void EnsureConnected()
        {
            if (!_modbusService.IsConnected)
                throw new InvalidOperationException("Not connected. Use option 1 to connect first.");
        }

        static ushort ReadUShort(string prompt, ushort defaultValue)
        {
            System.Console.Write($"  {prompt} [{defaultValue}]: ");
            string? input = System.Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(input)) return defaultValue;
            if (ushort.TryParse(input, out ushort val)) return val;
            throw new ArgumentException($"Invalid value for {prompt}");
        }

        static ushort ReadUShortOrHex(string prompt)
        {
            System.Console.Write($"  {prompt}: ");
            string input = System.Console.ReadLine()?.Trim() ?? "";
            if (input.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return Convert.ToUInt16(input, 16);
            if (ushort.TryParse(input, out ushort val)) return val;
            throw new ArgumentException("Invalid value (0-65535 or 0x0000-0xFFFF)");
        }

        static void PrintColor(string text, ConsoleColor color)
        {
            var prev = System.Console.ForegroundColor;
            System.Console.ForegroundColor = color;
            System.Console.WriteLine(text);
            System.Console.ForegroundColor = prev;
        }

        static void PrintColorInline(string text, ConsoleColor color)
        {
            var prev = System.Console.ForegroundColor;
            System.Console.ForegroundColor = color;
            System.Console.Write(text);
            System.Console.ForegroundColor = prev;
        }
    }
}
