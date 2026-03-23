# Modbus TCP Test Client

A complete Modbus TCP testing tool with **both GUI and Console** interfaces, sharing the same core engine.

## Project Structure

```
ModbusTestClient/
├── ModbusTestClient.sln                    ← Open this in Visual Studio
├── ModbusTestClient.Core/                  ← Shared library (Models + ModbusService)
│   ├── Models/
│   │   ├── RegisterResult.cs
│   │   ├── CoilResult.cs
│   │   ├── ConnectionSettings.cs
│   │   └── LogEntry.cs
│   └── Services/
│       └── ModbusService.cs
├── ModbusTestClient.GUI/                   ← WPF GUI (Windows only)
│   ├── Helpers/
│   ├── Converters/
│   ├── ViewModels/
│   ├── MainWindow.xaml
│   └── App.xaml
└── ModbusTestClient.Console/               ← Console App (cross-platform)
    └── Program.cs
```

## Supported Modbus Functions

| Function Code | Description               |
|---------------|---------------------------|
| FC1            | Read Coils                |
| FC3            | Read Holding Registers    |
| FC5            | Write Single Coil         |
| FC6            | Write Single Register     |
| FC16           | Write Multiple Registers  |

## Requirements

- **.NET 8 SDK** — https://dotnet.microsoft.com/download/dotnet/8.0
- **Visual Studio 2022** (optional, for GUI development)

## Quick Start

### GUI Version (Windows only)
```bash
cd ModbusTestClient
dotnet restore
dotnet run --project ModbusTestClient.GUI
```

### Console Version (Windows / Linux / Mac / Codespaces)
```bash
cd ModbusTestClient
dotnet restore
dotnet run --project ModbusTestClient.Console
```

### Console with command-line args
```bash
dotnet run --project ModbusTestClient.Console -- --ip 192.168.1.100 --port 502 --slave 1 --timeout 5000
```

## Visual Studio Usage

1. Open `ModbusTestClient.sln`
2. In Solution Explorer, right-click the project you want to run:
   - `ModbusTestClient.GUI` for the graphical interface
   - `ModbusTestClient.Console` for the terminal interface
3. Select **Set as Startup Project**
4. Press F5

## GitHub Codespaces / Linux

Only the Console project works on non-Windows platforms:
```bash
dotnet run --project ModbusTestClient.Console
```

## Features

### Both versions
- Connect/disconnect to Modbus TCP servers
- Read Holding Registers (FC3)
- Write Single Register (FC6) — supports decimal and hex input
- Write Multiple Registers (FC16)
- Read Coils (FC1)
- Write Single Coil (FC5)
- Batch Test — 8 automated read/write/verify tests
- Persistent connection settings (saved to JSON)
- Full communication logging

### GUI only
- Auto-refresh polling with configurable interval
- Visual data grids with Dec/Hex/Binary display
- Log export to file
- Color-coded connection status

### Console only
- Cross-platform (Windows/Linux/Mac)
- Command-line arguments for automation
- Colored terminal output with formatted tables

## Troubleshooting

- **Connection refused**: Verify server is running on the correct IP:port
- **Timeout**: Increase timeout value (default: 3000ms)
- **GUI won't run on Linux**: Use the Console version instead
- **NuGet errors**: Run `dotnet restore` before building
