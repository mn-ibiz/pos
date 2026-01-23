# feat: Implement Label Printer Configuration UI

**Labels:** `enhancement` `frontend` `printing` `labels` `priority-critical`

## Overview

Create a WPF view (`LabelPrinterConfigurationView.xaml`) that allows administrators to manage label printers from the application UI. Currently, the backend service `LabelPrinterService.cs` (815 lines) is fully implemented but there is no UI to configure printers.

## Background

The backend supports:
- **Connection Types**: Serial (COM), Network (TCP/IP), USB, Windows Driver
- **Print Languages**: ZPL (Zebra), EPL (Eltron), TSPL (TSC), Raw/ESC-POS
- **Features**: Connection testing, test label printing, status monitoring, default printer selection

But users cannot access any of this functionality without a UI.

## Requirements

### View Components

Create `Views/Settings/LabelPrinterConfigurationView.xaml` with:

#### 1. Printer List Section
```
┌─────────────────────────────────────────────────────────────────┐
│ Label Printers                              [+ Add] [Refresh]   │
├───────────────────────┬─────────┬───────────┬──────────────────┤
│ Name                  │ Type    │ Language  │ Status           │
├───────────────────────┼─────────┼───────────┼──────────────────┤
│ ● Shelf Printer 1     │ Network │ ZPL       │ ● Online         │
│   Barcode Printer     │ USB     │ EPL       │ ● Offline        │
│   Promo Label Printer │ Serial  │ TSPL      │ ● Online         │
└───────────────────────┴─────────┴───────────┴──────────────────┘
```

- DataGrid with columns: Name, Type, Language, Status (with colored indicator)
- Row selection enables edit panel
- Default printer indicated with star/checkmark icon
- Status indicator: Green = Online, Red = Offline, Yellow = Busy, Orange = Error

#### 2. Printer Edit Panel
```
┌─ Selected Printer: Shelf Printer 1 ────────────────────────────┐
│ Name:        [Shelf Printer 1              ]                   │
│ Type:        [Network              ▼]                          │
│ Language:    [ZPL (Zebra)          ▼]                          │
│                                                                │
│ ── Connection Settings (Dynamic based on Type) ──             │
│ IP Address:  [192.168.1.100        ] Port: [9100]              │
│ Timeout:     [5000  ] ms                                       │
│                                                                │
│ Default Size: [38 x 25 mm          ▼]                          │
│ ☑ Set as Default Printer                                       │
│                                                                │
│ [Test Connection]  [Print Test Label]  [Save]  [Delete]        │
└────────────────────────────────────────────────────────────────┘
```

#### 3. Dynamic Connection Fields by Type

| Type | Fields |
|------|--------|
| **Serial** | COM Port (dropdown), Baud Rate (9600, 19200, 38400, 115200), Data Bits (7, 8) |
| **Network** | IP Address, Port (default 9100), Timeout (ms) |
| **USB** | Printer Name (dropdown from Windows printers) |
| **Windows** | Printer Name (dropdown from Windows printers) |

### ViewModel

Create `ViewModels/Settings/LabelPrinterConfigurationViewModel.cs`:

```csharp
public partial class LabelPrinterConfigurationViewModel : ObservableObject
{
    private readonly ILabelPrinterService _printerService;

    [ObservableProperty] private ObservableCollection<LabelPrinterDto> _printers;
    [ObservableProperty] private LabelPrinterDto? _selectedPrinter;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private bool _isTesting;

    // Edit form properties
    [ObservableProperty] private string _printerName;
    [ObservableProperty] private LabelPrinterTypeDto _printerType;
    [ObservableProperty] private LabelPrintLanguageDto _printLanguage;
    [ObservableProperty] private string _connectionString;
    [ObservableProperty] private int? _port;
    [ObservableProperty] private int? _baudRate;
    [ObservableProperty] private int? _timeoutMs;
    [ObservableProperty] private int? _defaultLabelSizeId;
    [ObservableProperty] private bool _isDefault;

    // Commands
    public IAsyncRelayCommand LoadPrintersCommand { get; }
    public IAsyncRelayCommand AddPrinterCommand { get; }
    public IAsyncRelayCommand SavePrinterCommand { get; }
    public IAsyncRelayCommand DeletePrinterCommand { get; }
    public IAsyncRelayCommand TestConnectionCommand { get; }
    public IAsyncRelayCommand PrintTestLabelCommand { get; }
    public IRelayCommand RefreshStatusCommand { get; }
}
```

### Service Integration

Use existing `ILabelPrinterService` methods:
- `GetAllPrintersAsync(storeId)` - Load printer list
- `CreatePrinterAsync(dto)` - Add new printer
- `UpdatePrinterAsync(id, dto)` - Update printer
- `DeletePrinterAsync(id)` - Delete printer
- `TestPrinterConnectionAsync(id)` - Test connection
- `PrintTestLabelAsync(id)` - Print test label
- `SetDefaultPrinterAsync(id)` - Set as default

### Helper Methods

```csharp
// Enumerate available COM ports
private List<string> GetAvailableComPorts()
{
    return System.IO.Ports.SerialPort.GetPortNames().ToList();
}

// Get Windows printers
private List<string> GetWindowsPrinters()
{
    return System.Drawing.Printing.PrinterSettings
        .InstalledPrinters.Cast<string>().ToList();
}
```

## Acceptance Criteria

### Functional Requirements
- [ ] View displays all configured label printers for current store
- [ ] Can add new printer with all connection types (Serial, Network, USB, Windows)
- [ ] Can edit existing printer configuration
- [ ] Can delete printer (with confirmation dialog)
- [ ] Connection test shows success/failure with message
- [ ] Test label prints to selected printer
- [ ] Can set default printer (only one default at a time)
- [ ] Status indicator reflects real printer status
- [ ] Connection fields change dynamically based on printer type
- [ ] COM port dropdown shows available system ports
- [ ] Windows printer dropdown shows installed printers

### UI/UX Requirements
- [ ] Consistent with existing admin settings views
- [ ] Loading indicators during async operations
- [ ] Error messages displayed via toast notifications
- [ ] Confirmation dialog before delete
- [ ] Disable Save button until required fields are filled
- [ ] Disable Test/Print buttons until printer is saved
- [ ] Real-time validation of IP address format

### Integration
- [ ] View accessible from Settings menu
- [ ] Registered in DI container
- [ ] Navigation works from sidebar

## Technical Notes

### Existing Backend Files
- `Core/Interfaces/ILabelPrinterService.cs` - Service interface (144 lines)
- `Infrastructure/Services/LabelPrinterService.cs` - Full implementation (815 lines)
- `Core/Entities/LabelPrintingEntities.cs` - LabelPrinter entity (lines 132-171)
- `Core/DTOs/LabelPrintingDtos.cs` - DTOs for printer operations

### Test Label Content (from LabelPrinterService.cs:703-758)
The service already generates test labels in ZPL/EPL/TSPL format with:
- Printer name
- Test date/time
- Test barcode
- "Test Print Successful" text

### Connection Testing (from LabelPrinterService.cs:482-664)
- Serial: Opens port, sends status request
- Network: TCP connection to IP:Port
- USB: Checks Windows printer registry
- Windows: Validates printer exists in system

## UI Mockup

```
┌─────────────────────────────────────────────────────────────────────┐
│ Settings > Label Printers                                      [X] │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│ ┌─ Configured Printers ─────────────────── [+ Add New] [↻ Refresh] │
│ │                                                                  │
│ │ ┌────────────────────────────────────────────────────────────┐  │
│ │ │ ★ Shelf Printer 1          Network   ZPL     ● Online     │  │
│ │ │   IP: 192.168.1.100:9100                                   │  │
│ │ ├────────────────────────────────────────────────────────────┤  │
│ │ │   Barcode Printer           USB      EPL     ● Offline    │  │
│ │ │   DYMO LabelWriter 450                                     │  │
│ │ ├────────────────────────────────────────────────────────────┤  │
│ │ │   Promo Printer             Serial   TSPL    ● Online     │  │
│ │ │   COM3 @ 9600                                              │  │
│ │ └────────────────────────────────────────────────────────────┘  │
│ └──────────────────────────────────────────────────────────────────│
│                                                                     │
│ ┌─ Edit: Shelf Printer 1 ──────────────────────────────────────────┐
│ │                                                                  │
│ │  Name:         [Shelf Printer 1                    ]             │
│ │                                                                  │
│ │  ┌─ Connection ───────────────────────────────────────────────┐ │
│ │  │ Type:       (●) Network  ( ) Serial  ( ) USB  ( ) Windows  │ │
│ │  │                                                             │ │
│ │  │ IP Address: [192.168.1.100      ]  Port: [9100    ]         │ │
│ │  │ Timeout:    [5000               ] ms                        │ │
│ │  └─────────────────────────────────────────────────────────────┘ │
│ │                                                                  │
│ │  ┌─ Print Settings ───────────────────────────────────────────┐ │
│ │  │ Language:   [ZPL (Zebra)                    ▼]              │ │
│ │  │ Label Size: [38 x 25 mm - Standard Shelf    ▼]              │ │
│ │  │ ☑ Set as default printer                                   │ │
│ │  └─────────────────────────────────────────────────────────────┘ │
│ │                                                                  │
│ │  [🔌 Test Connection]  [🏷️ Print Test Label]                    │
│ │                                                                  │
│ │              [💾 Save Changes]  [🗑️ Delete Printer]              │
│ └──────────────────────────────────────────────────────────────────┘
└─────────────────────────────────────────────────────────────────────┘
```

## Test Cases

1. **Add Network Printer** - Create with IP and port, test connection
2. **Add Serial Printer** - Create with COM port and baud rate
3. **Add USB Printer** - Select from Windows printer list
4. **Edit Printer** - Change name, connection settings
5. **Delete Printer** - Confirm and remove
6. **Test Connection Success** - Valid IP returns success
7. **Test Connection Failure** - Invalid IP returns error message
8. **Print Test Label** - Label prints correctly
9. **Set Default** - Only one default at a time
10. **Status Refresh** - Status updates on refresh

## Dependencies

- None (foundational UI component)

## Blocks

- Issue #011: Label Template Management UI
- Issue #012: Visual Template Designer

## Estimated Complexity

**Medium** - Standard CRUD UI with dynamic form fields and async operations
