# Modbus TCP Test Client

A WPF application for testing Modbus TCP servers/simulators.  
Supports reading and writing registers and coils through a clean graphical interface.

## Supported Functions

| Function Code | Description               |
|---------------|---------------------------|
| FC1            | Read Coils                |
| FC3            | Read Holding Registers    |
| FC5            | Write Single Coil         |
| FC6            | Write Single Register     |
| FC16           | Write Multiple Registers  |

## Requirements

- **Windows 10/11**
- **.NET 8 SDK** (download from https://dotnet.microsoft.com/download/dotnet/8.0)
- **Visual Studio 2022** (optional, for IDE experience)

## Quick Start

### Option 1: Command Line
```bash
cd ModbusTestClient
dotnet restore
dotnet build
dotnet run
```

### Option 2: Visual Studio
1. Open `ModbusTestClient.sln` in Visual Studio 2022
2. NuGet packages will restore automatically
3. Press F5 to build and run

## How to Use

### 1. Connect
- Enter the IP address and port of your Modbus TCP server
- Set the Slave ID (Unit ID)  
- Click **Connect**

### 2. Read/Write Operations
- **Read Registers tab**: Enter start address and quantity, click Read
- **Write Single tab**: Enter address and value (decimal or hex like 0xFF)
- **Write Multi tab**: Enter start address and comma-separated values
- **Coils tab**: Read and write individual coils (ON/OFF)

### 3. Auto Refresh
- Enable auto-refresh in the Read Registers tab
- Set interval in milliseconds
- Registers will be polled automatically

### 4. Batch Test
- Click "Run Batch Test" to run 8 automated tests
- Writes and reads back data to verify your server
- Tests use registers 0, 10-12 and coil 0

### 5. Log
- All communication is logged at the bottom
- Export to file for analysis
- TX = sent to server, RX = received from server

## NuGet Dependencies

- **NModbus** (v3.0.81) - Modbus protocol implementation
- **Newtonsoft.Json** (v13.0.3) - Settings serialization

## Project Structure

```
ModbusTestClient/
├── App.xaml / App.xaml.cs           - Application entry point & global styles
├── MainWindow.xaml / .cs            - Main UI layout  
├── Services/
│   └── ModbusService.cs             - Core Modbus TCP communication
├── ViewModels/
│   └── MainViewModel.cs             - MVVM logic & commands
├── Models/
│   ├── RegisterResult.cs            - Register data model
│   ├── CoilResult.cs                - Coil data model  
│   ├── ConnectionSettings.cs        - Persistent settings
│   └── LogEntry.cs                  - Log entry model
├── Helpers/
│   ├── BaseViewModel.cs             - INotifyPropertyChanged base
│   └── RelayCommand.cs              - ICommand implementations
└── Converters/
    └── Converters.cs                - WPF value converters
```

## Troubleshooting

- **Connection refused**: Make sure your Modbus server is running and listening on the correct IP:port
- **Timeout**: Increase the timeout value, or check network connectivity
- **Illegal function**: The server doesn't support the requested function code
- **Illegal data address**: The register/coil address doesn't exist in the server
