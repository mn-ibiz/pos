# Story 12.1: Receipt Printer Configuration

Status: done

## Story

As an administrator,
I want to configure the receipt printer,
So that customer receipts print correctly.

## Acceptance Criteria

1. **Given** a thermal printer is connected
   **When** configuring printer settings
   **Then** admin can specify: printer name/port (USB, Serial, Network)

2. **Given** receipt branding
   **When** configuring printer
   **Then** admin can configure receipt header (business name, address, phone)

3. **Given** receipt footer
   **When** configuring printer
   **Then** admin can configure receipt footer (thank you message)

4. **Given** printer setup
   **When** verifying configuration
   **Then** test print functionality should be available

5. **Given** operational monitoring
   **When** using printer
   **Then** printer status should be monitored

## Tasks / Subtasks

- [x] Task 1: Create Printer Configuration Entities
  - [x] Create Printer entity
  - [x] Create PrinterSettings entity
  - [x] Create ReceiptTemplate entity
  - [x] Configure EF Core mappings

- [x] Task 2: Create Printer Settings Screen
  - [x] Create PrinterSettingsView.xaml
  - [x] Create PrinterSettingsViewModel
  - [x] Printer connection type selection
  - [x] Port/IP configuration

- [x] Task 3: Create Receipt Template Editor
  - [x] Header configuration
  - [x] Footer configuration
  - [x] Logo upload
  - [x] Preview functionality

- [x] Task 4: Implement Printer Discovery
  - [x] Discover USB printers
  - [x] Discover network printers
  - [x] Serial port detection
  - [x] Status monitoring

- [x] Task 5: Implement Test Print
  - [x] Print test page
  - [x] Print sample receipt
  - [x] Verify paper width
  - [x] Show connection status

## Dev Notes

### Printer Entity

```csharp
public class Printer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public PrinterType Type { get; set; } = PrinterType.Receipt;
    public PrinterConnectionType ConnectionType { get; set; }

    // Connection settings
    public string? PortName { get; set; }           // COM1, LPT1
    public string? IpAddress { get; set; }          // 192.168.1.100
    public int? Port { get; set; } = 9100;          // Network port
    public string? UsbPath { get; set; }            // USB device path
    public string? WindowsPrinterName { get; set; } // Windows printer queue

    // Paper settings
    public int PaperWidth { get; set; } = 80;       // 80mm or 58mm
    public int CharsPerLine { get; set; } = 48;     // Characters per line

    // Status
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public PrinterStatus Status { get; set; } = PrinterStatus.Unknown;
    public DateTime? LastStatusCheck { get; set; }
    public string? LastError { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public PrinterSettings? Settings { get; set; }
}

public enum PrinterType
{
    Receipt,
    Kitchen,
    Report
}

public enum PrinterConnectionType
{
    USB,
    Serial,
    Network,
    WindowsDriver
}

public enum PrinterStatus
{
    Unknown,
    Online,
    Offline,
    Error,
    PaperOut,
    CoverOpen
}
```

### PrinterSettings Entity

```csharp
public class PrinterSettings
{
    public int Id { get; set; }
    public int PrinterId { get; set; }

    // ESC/POS Settings
    public bool UseEscPos { get; set; } = true;
    public bool AutoCut { get; set; } = true;
    public bool PartialCut { get; set; } = true;
    public bool OpenCashDrawer { get; set; } = true;
    public int CutFeedLines { get; set; } = 3;

    // Print settings
    public bool PrintLogo { get; set; } = true;
    public byte[]? LogoBitmap { get; set; }
    public int LogoWidth { get; set; } = 200;

    // Beeper
    public bool BeepOnPrint { get; set; } = false;
    public int BeepCount { get; set; } = 1;

    // Speed/Quality
    public int PrintDensity { get; set; } = 7;  // 0-15

    // Navigation
    public Printer Printer { get; set; } = null!;
}
```

### ReceiptTemplate Entity

```csharp
public class ReceiptTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = "Default";

    // Header
    public string BusinessName { get; set; } = string.Empty;
    public string? BusinessSubtitle { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? TaxPin { get; set; }

    // Footer
    public string? FooterLine1 { get; set; } = "Thank you for your visit!";
    public string? FooterLine2 { get; set; } = "Please come again";
    public string? FooterLine3 { get; set; }

    // Options
    public bool ShowTaxBreakdown { get; set; } = true;
    public bool ShowCashierName { get; set; } = true;
    public bool ShowTableNumber { get; set; } = true;
    public bool ShowQRCode { get; set; } = false;
    public string? QRCodeContent { get; set; }

    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

### Printer Settings Screen

```
+------------------------------------------+
|      PRINTER CONFIGURATION                |
+------------------------------------------+
| Receipt Printers                          |
| +--------------------------------------+  |
| | [+] Add Printer                      |  |
| +--------------------------------------+  |
| | POS Printer (Default)     [Online]   |  |
| | USB - 80mm                [Edit]     |  |
| +--------------------------------------+  |
| | Backup Printer            [Offline]  |  |
| | Network - 192.168.1.50    [Edit]     |  |
| +--------------------------------------+  |
|                                           |
+------------------------------------------+
|      PRINTER DETAILS                      |
+------------------------------------------+
|                                           |
|  Printer Name: [POS Printer___________]   |
|                                           |
|  Connection Type:                         |
|  ( ) USB Device                           |
|  (x) Windows Printer                      |
|  ( ) Network (IP)                         |
|  ( ) Serial Port                          |
|                                           |
|  Windows Printer:                         |
|  [EPSON TM-T88V Receipt____] [v] [Detect] |
|                                           |
|  Paper Width:                             |
|  (x) 80mm (48 chars)                      |
|  ( ) 58mm (32 chars)                      |
|                                           |
|  Options:                                 |
|  [x] Auto-cut paper                       |
|  [x] Open cash drawer on print            |
|  [x] Print logo                           |
|  [ ] Beep on print                        |
|                                           |
|  [Test Print]   [Save]   [Set as Default] |
+------------------------------------------+
```

### Receipt Template Editor

```
+------------------------------------------+
|      RECEIPT TEMPLATE                     |
+------------------------------------------+
|                                           |
|  HEADER                                   |
|  ─────────────────────────────────────    |
|  Business Name:                           |
|  [Mimi's Restaurant__________________]    |
|                                           |
|  Subtitle (optional):                     |
|  [Fine Dining & Bar__________________]    |
|                                           |
|  Address:                                 |
|  [123 Kenyatta Avenue________________]    |
|  [Nairobi, Kenya_____________________]    |
|                                           |
|  Phone: [+254 700 123 456__]              |
|  Email: [info@mimis.co.ke__]              |
|  KRA PIN: [P0123456789X____]              |
|                                           |
|  Logo:                                    |
|  +----------------+                       |
|  | [Upload Logo]  |   [Remove]            |
|  | 200x100 px max |                       |
|  +----------------+                       |
|                                           |
|  FOOTER                                   |
|  ─────────────────────────────────────    |
|  Line 1: [Thank you for dining with us!] |
|  Line 2: [Please visit again_________]    |
|  Line 3: [_____________________________]  |
|                                           |
|  OPTIONS                                  |
|  ─────────────────────────────────────    |
|  [x] Show tax breakdown                   |
|  [x] Show cashier name                    |
|  [x] Show table number                    |
|  [ ] Print QR code                        |
|                                           |
|  [Preview Receipt]          [Save]        |
+------------------------------------------+
```

### PrinterSettingsViewModel

```csharp
public partial class PrinterSettingsViewModel : BaseViewModel
{
    private readonly IPrinterService _printerService;
    private readonly IPrinterDiscoveryService _discoveryService;

    [ObservableProperty]
    private ObservableCollection<Printer> _printers = new();

    [ObservableProperty]
    private Printer? _selectedPrinter;

    [ObservableProperty]
    private PrinterConnectionType _connectionType;

    [ObservableProperty]
    private string _printerName = string.Empty;

    [ObservableProperty]
    private string _windowsPrinterName = string.Empty;

    [ObservableProperty]
    private string _ipAddress = string.Empty;

    [ObservableProperty]
    private int _port = 9100;

    [ObservableProperty]
    private string _serialPort = string.Empty;

    [ObservableProperty]
    private int _paperWidth = 80;

    [ObservableProperty]
    private bool _autoCut = true;

    [ObservableProperty]
    private bool _openCashDrawer = true;

    [ObservableProperty]
    private bool _printLogo = true;

    [ObservableProperty]
    private bool _beepOnPrint = false;

    [ObservableProperty]
    private ObservableCollection<string> _availableWindowsPrinters = new();

    [ObservableProperty]
    private ObservableCollection<string> _availableSerialPorts = new();

    public async Task InitializeAsync()
    {
        Printers = new ObservableCollection<Printer>(
            await _printerService.GetPrintersAsync(PrinterType.Receipt));

        if (Printers.Any())
        {
            SelectedPrinter = Printers.FirstOrDefault(p => p.IsDefault)
                           ?? Printers.First();
        }

        await DetectPrintersAsync();
    }

    partial void OnSelectedPrinterChanged(Printer? value)
    {
        if (value != null)
        {
            LoadPrinterDetails(value);
        }
    }

    private void LoadPrinterDetails(Printer printer)
    {
        PrinterName = printer.Name;
        ConnectionType = printer.ConnectionType;
        WindowsPrinterName = printer.WindowsPrinterName ?? string.Empty;
        IpAddress = printer.IpAddress ?? string.Empty;
        Port = printer.Port ?? 9100;
        SerialPort = printer.PortName ?? string.Empty;
        PaperWidth = printer.PaperWidth;

        if (printer.Settings != null)
        {
            AutoCut = printer.Settings.AutoCut;
            OpenCashDrawer = printer.Settings.OpenCashDrawer;
            PrintLogo = printer.Settings.PrintLogo;
            BeepOnPrint = printer.Settings.BeepOnPrint;
        }
    }

    [RelayCommand]
    private async Task DetectPrintersAsync()
    {
        // Detect Windows printers
        AvailableWindowsPrinters = new ObservableCollection<string>(
            await _discoveryService.GetWindowsPrintersAsync());

        // Detect serial ports
        AvailableSerialPorts = new ObservableCollection<string>(
            SerialPort.GetPortNames());
    }

    [RelayCommand]
    private async Task TestPrintAsync()
    {
        if (SelectedPrinter == null)
        {
            await _dialogService.ShowMessageAsync(
                "No Printer",
                "Please select or add a printer first.");
            return;
        }

        try
        {
            var result = await _printerService.TestPrintAsync(SelectedPrinter);

            if (result.Success)
            {
                await _dialogService.ShowMessageAsync(
                    "Test Print",
                    "Test page printed successfully!");
            }
            else
            {
                await _dialogService.ShowMessageAsync(
                    "Print Failed",
                    result.ErrorMessage ?? "Unknown error occurred.");
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync(
                "Print Error",
                ex.Message);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedPrinter == null) return;

        SelectedPrinter.Name = PrinterName;
        SelectedPrinter.ConnectionType = ConnectionType;
        SelectedPrinter.PaperWidth = PaperWidth;
        SelectedPrinter.CharsPerLine = PaperWidth == 80 ? 48 : 32;

        switch (ConnectionType)
        {
            case PrinterConnectionType.WindowsDriver:
                SelectedPrinter.WindowsPrinterName = WindowsPrinterName;
                break;
            case PrinterConnectionType.Network:
                SelectedPrinter.IpAddress = IpAddress;
                SelectedPrinter.Port = Port;
                break;
            case PrinterConnectionType.Serial:
                SelectedPrinter.PortName = SerialPort;
                break;
        }

        if (SelectedPrinter.Settings == null)
        {
            SelectedPrinter.Settings = new PrinterSettings();
        }

        SelectedPrinter.Settings.AutoCut = AutoCut;
        SelectedPrinter.Settings.OpenCashDrawer = OpenCashDrawer;
        SelectedPrinter.Settings.PrintLogo = PrintLogo;
        SelectedPrinter.Settings.BeepOnPrint = BeepOnPrint;

        await _printerService.SavePrinterAsync(SelectedPrinter);

        await _dialogService.ShowMessageAsync(
            "Saved",
            "Printer settings saved successfully.");
    }

    [RelayCommand]
    private async Task SetAsDefaultAsync()
    {
        if (SelectedPrinter == null) return;

        await _printerService.SetDefaultPrinterAsync(
            SelectedPrinter.Id,
            PrinterType.Receipt);

        foreach (var printer in Printers)
        {
            printer.IsDefault = printer.Id == SelectedPrinter.Id;
        }

        await _dialogService.ShowMessageAsync(
            "Default Printer",
            $"{SelectedPrinter.Name} is now the default receipt printer.");
    }

    [RelayCommand]
    private async Task AddPrinterAsync()
    {
        var printer = new Printer
        {
            Name = "New Printer",
            Type = PrinterType.Receipt,
            ConnectionType = PrinterConnectionType.WindowsDriver,
            PaperWidth = 80,
            CharsPerLine = 48,
            Settings = new PrinterSettings()
        };

        await _printerService.SavePrinterAsync(printer);
        Printers.Add(printer);
        SelectedPrinter = printer;
    }

    [RelayCommand]
    private async Task DeletePrinterAsync()
    {
        if (SelectedPrinter == null) return;

        if (SelectedPrinter.IsDefault)
        {
            await _dialogService.ShowMessageAsync(
                "Cannot Delete",
                "Cannot delete the default printer. Set another printer as default first.");
            return;
        }

        var confirm = await _dialogService.ShowConfirmAsync(
            "Delete Printer",
            $"Are you sure you want to delete {SelectedPrinter.Name}?");

        if (confirm)
        {
            await _printerService.DeletePrinterAsync(SelectedPrinter.Id);
            Printers.Remove(SelectedPrinter);
            SelectedPrinter = Printers.FirstOrDefault();
        }
    }

    [RelayCommand]
    private async Task CheckStatusAsync()
    {
        if (SelectedPrinter == null) return;

        var status = await _printerService.CheckPrinterStatusAsync(SelectedPrinter);
        SelectedPrinter.Status = status.Status;
        SelectedPrinter.LastStatusCheck = DateTime.Now;
        SelectedPrinter.LastError = status.ErrorMessage;

        var statusMessage = status.Status switch
        {
            PrinterStatus.Online => "Printer is online and ready",
            PrinterStatus.Offline => "Printer is offline or disconnected",
            PrinterStatus.PaperOut => "Printer is out of paper",
            PrinterStatus.CoverOpen => "Printer cover is open",
            PrinterStatus.Error => $"Printer error: {status.ErrorMessage}",
            _ => "Printer status unknown"
        };

        await _dialogService.ShowMessageAsync("Printer Status", statusMessage);
    }
}
```

### Printer Discovery Service

```csharp
public interface IPrinterDiscoveryService
{
    Task<List<string>> GetWindowsPrintersAsync();
    Task<List<DiscoveredPrinter>> DiscoverNetworkPrintersAsync();
    Task<List<string>> GetSerialPortsAsync();
    Task<bool> TestConnectionAsync(Printer printer);
}

public class PrinterDiscoveryService : IPrinterDiscoveryService
{
    public Task<List<string>> GetWindowsPrintersAsync()
    {
        var printers = new List<string>();

        foreach (string printer in PrinterSettings.InstalledPrinters)
        {
            printers.Add(printer);
        }

        return Task.FromResult(printers);
    }

    public async Task<List<DiscoveredPrinter>> DiscoverNetworkPrintersAsync()
    {
        var discovered = new List<DiscoveredPrinter>();

        // Scan common ports on local network
        var localIp = GetLocalIPAddress();
        var subnet = localIp.Substring(0, localIp.LastIndexOf('.') + 1);

        var tasks = new List<Task>();
        for (int i = 1; i <= 254; i++)
        {
            var ip = $"{subnet}{i}";
            tasks.Add(Task.Run(async () =>
            {
                if (await TryConnectAsync(ip, 9100))
                {
                    discovered.Add(new DiscoveredPrinter
                    {
                        IpAddress = ip,
                        Port = 9100
                    });
                }
            }));
        }

        await Task.WhenAll(tasks);
        return discovered;
    }

    public Task<List<string>> GetSerialPortsAsync()
    {
        return Task.FromResult(SerialPort.GetPortNames().ToList());
    }

    public async Task<bool> TestConnectionAsync(Printer printer)
    {
        try
        {
            switch (printer.ConnectionType)
            {
                case PrinterConnectionType.Network:
                    return await TryConnectAsync(
                        printer.IpAddress!,
                        printer.Port ?? 9100);

                case PrinterConnectionType.WindowsDriver:
                    return PrinterSettings.InstalledPrinters
                        .Cast<string>()
                        .Contains(printer.WindowsPrinterName);

                case PrinterConnectionType.Serial:
                    return SerialPort.GetPortNames()
                        .Contains(printer.PortName);

                default:
                    return false;
            }
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> TryConnectAsync(string ip, int port)
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(ip, port);
            var timeoutTask = Task.Delay(200);

            var completedTask = await Task.WhenAny(connectTask, timeoutTask);
            return completedTask == connectTask && client.Connected;
        }
        catch
        {
            return false;
        }
    }
}

public class DiscoveredPrinter
{
    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; }
    public string? Model { get; set; }
}
```

### Test Print Template (80mm)

```
================================================
     PRINTER TEST PAGE
================================================

Printer: EPSON TM-T88V
Connection: Windows Driver
Paper Width: 80mm (48 chars)
Date/Time: 2025-12-20 18:30:45

------------------------------------------------
FONT STYLES TEST
------------------------------------------------
Normal text
**Bold text**
DOUBLE WIDTH
Double Height
EMPHASIZED text

------------------------------------------------
ALIGNMENT TEST
------------------------------------------------
Left aligned
                              Right aligned
            Centered

------------------------------------------------
SPECIAL CHARACTERS
------------------------------------------------
Currency: KSh 1,234.56
Percentage: 16%
Symbols: @ # $ % & * ( ) [ ]

------------------------------------------------
LINE STYLES
------------------------------------------------
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
────────────────────────────────────────────────
================================================

[LOGO PRINT TEST HERE]

================================================
     TEST COMPLETE
     Printer is working correctly!
================================================

```

### References
- [Source: docs/PRD_Hospitality_POS_System.md#5.10.1-Receipt-Printing]
- [Source: docs/PRD_Hospitality_POS_System.md#PR-001 to PR-010]

## Dev Agent Record

### Agent Model Used
Claude Opus 4.5 (claude-opus-4-5-20251101)

### Completion Notes List
- Created PrinterEnums.cs with PrinterType, PrinterConnectionType, PrinterStatus
- Created Printer.cs entity with connection settings and status tracking
- Created PrinterSettings.cs entity for ESC/POS configuration
- Created ReceiptTemplate.cs entity for receipt branding
- Created PrinterModels.cs with DiscoveredPrinter, PrintTestResult, PrinterStatusResult DTOs
- Created PrinterConfiguration.cs with EF Core configurations for all entities
- Created IPrinterService.cs interface with printer and template management
- Created PrinterService.cs with full CRUD, test print, and status check
- Created PrinterDiscoveryService.cs for Windows, network, and serial discovery
- Created PrinterSettingsViewModel.cs with MVVM pattern
- Created PrinterSettingsView.xaml with dark theme UI
- Registered IPrinterService, IPrinterDiscoveryService, and PrinterSettingsViewModel in DI

### File List
- src/HospitalityPOS.Core/Enums/PrinterEnums.cs (new)
- src/HospitalityPOS.Core/Entities/Printer.cs (new)
- src/HospitalityPOS.Core/Entities/PrinterSettings.cs (new)
- src/HospitalityPOS.Core/Entities/ReceiptTemplate.cs (new)
- src/HospitalityPOS.Core/Models/PrinterModels.cs (new)
- src/HospitalityPOS.Core/Interfaces/IPrinterService.cs (new)
- src/HospitalityPOS.Infrastructure/Data/POSDbContext.cs (updated - added DbSets)
- src/HospitalityPOS.Infrastructure/Data/Configurations/PrinterConfiguration.cs (new)
- src/HospitalityPOS.Infrastructure/Services/PrinterService.cs (new)
- src/HospitalityPOS.Infrastructure/Services/PrinterDiscoveryService.cs (new)
- src/HospitalityPOS.WPF/ViewModels/PrinterSettingsViewModel.cs (new)
- src/HospitalityPOS.WPF/Views/PrinterSettingsView.xaml (new)
- src/HospitalityPOS.WPF/Views/PrinterSettingsView.xaml.cs (new)
- src/HospitalityPOS.WPF/App.xaml.cs (updated - DI registration)
