using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the printer settings management screen.
/// </summary>
public partial class PrinterSettingsViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;

    #region Observable Properties

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
    private bool _partialCut = true;

    [ObservableProperty]
    private bool _openCashDrawer = true;

    [ObservableProperty]
    private bool _printLogo = true;

    [ObservableProperty]
    private bool _beepOnPrint;

    [ObservableProperty]
    private int _cutFeedLines = 3;

    [ObservableProperty]
    private int _receiptCopies = 1;

    [ObservableProperty]
    private bool _autoPrintOnSettlement = true;

    [ObservableProperty]
    private string _footerMessage = "Thank you for your business!";

    [ObservableProperty]
    private bool _printCustomerCopy;

    [ObservableProperty]
    private bool _isWindowsDriver = true;

    [ObservableProperty]
    private bool _isNetwork;

    [ObservableProperty]
    private bool _isSerial;

    [ObservableProperty]
    private bool _isUsb;

    [ObservableProperty]
    private ObservableCollection<string> _availableWindowsPrinters = new();

    [ObservableProperty]
    private ObservableCollection<string> _availableSerialPorts = new();

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasStatusMessage;

    [ObservableProperty]
    private bool _isStatusSuccess;

    #endregion

    public PrinterSettingsViewModel(
        IServiceScopeFactory scopeFactory,
        ILogger logger) : base(logger)
    {
        _scopeFactory = scopeFactory;
        Title = "Printer Settings";
    }

    #region Initialization

    /// <summary>
    /// Initializes the ViewModel and loads data.
    /// </summary>
    public async Task InitializeAsync()
    {
        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var printerService = scope.ServiceProvider.GetRequiredService<IPrinterService>();
            var discoveryService = scope.ServiceProvider.GetRequiredService<IPrinterDiscoveryService>();

            // Load printers
            var printers = await printerService.GetPrintersAsync(PrinterType.Receipt);
            Printers = new ObservableCollection<Printer>(printers);

            if (Printers.Any())
            {
                SelectedPrinter = Printers.FirstOrDefault(p => p.IsDefault) ?? Printers.First();
            }

            // Load available printers
            var windowsPrinters = await discoveryService.GetWindowsPrintersAsync();
            AvailableWindowsPrinters = new ObservableCollection<string>(windowsPrinters);

            var serialPorts = await discoveryService.GetSerialPortsAsync();
            AvailableSerialPorts = new ObservableCollection<string>(serialPorts);
        }, "Loading printer settings...");
    }

    #endregion

    #region Property Changed Handlers

    partial void OnSelectedPrinterChanged(Printer? value)
    {
        if (value != null)
        {
            LoadPrinterDetails(value);
        }
        else
        {
            ClearPrinterDetails();
        }
    }

    partial void OnConnectionTypeChanged(PrinterConnectionType value)
    {
        IsWindowsDriver = value == PrinterConnectionType.WindowsDriver;
        IsNetwork = value == PrinterConnectionType.Network;
        IsSerial = value == PrinterConnectionType.Serial;
        IsUsb = value == PrinterConnectionType.USB;
    }

    partial void OnPaperWidthChanged(int value)
    {
        // Auto-update chars per line based on paper width
        // 80mm = 48 chars, 58mm = 32 chars
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
            PartialCut = printer.Settings.PartialCut;
            OpenCashDrawer = printer.Settings.OpenCashDrawer;
            PrintLogo = printer.Settings.PrintLogo;
            BeepOnPrint = printer.Settings.BeepOnPrint;
            CutFeedLines = printer.Settings.CutFeedLines;
            ReceiptCopies = printer.Settings.ReceiptCopies;
            AutoPrintOnSettlement = printer.Settings.AutoPrintOnSettlement;
            FooterMessage = printer.Settings.FooterMessage ?? "Thank you for your business!";
            PrintCustomerCopy = printer.Settings.PrintCustomerCopy;
        }
        else
        {
            // Default settings
            AutoCut = true;
            PartialCut = true;
            OpenCashDrawer = true;
            PrintLogo = true;
            BeepOnPrint = false;
            CutFeedLines = 3;
            ReceiptCopies = 1;
            AutoPrintOnSettlement = true;
            FooterMessage = "Thank you for your business!";
            PrintCustomerCopy = false;
        }

        ClearStatusMessage();
    }

    private void ClearPrinterDetails()
    {
        PrinterName = string.Empty;
        ConnectionType = PrinterConnectionType.WindowsDriver;
        WindowsPrinterName = string.Empty;
        IpAddress = string.Empty;
        Port = 9100;
        SerialPort = string.Empty;
        PaperWidth = 80;
        AutoCut = true;
        PartialCut = true;
        OpenCashDrawer = true;
        PrintLogo = true;
        BeepOnPrint = false;
        CutFeedLines = 3;
        ReceiptCopies = 1;
        AutoPrintOnSettlement = true;
        FooterMessage = "Thank you for your business!";
        PrintCustomerCopy = false;
    }

    #endregion

    #region Commands

    [RelayCommand]
    private void SetConnectionType(string connectionTypeStr)
    {
        if (Enum.TryParse<PrinterConnectionType>(connectionTypeStr, out var connectionType))
        {
            ConnectionType = connectionType;
        }
    }

    [RelayCommand]
    private void SetPaperWidth(string widthStr)
    {
        if (int.TryParse(widthStr, out var width))
        {
            PaperWidth = width;
        }
    }

    [RelayCommand]
    private async Task DetectPrintersAsync()
    {
        IsScanning = true;
        ClearStatusMessage();

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var discoveryService = scope.ServiceProvider.GetRequiredService<IPrinterDiscoveryService>();

            var windowsPrinters = await discoveryService.GetWindowsPrintersAsync();
            AvailableWindowsPrinters = new ObservableCollection<string>(windowsPrinters);

            var serialPorts = await discoveryService.GetSerialPortsAsync();
            AvailableSerialPorts = new ObservableCollection<string>(serialPorts);

            ShowStatusMessage($"Found {windowsPrinters.Count} printers and {serialPorts.Count} serial ports", true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error detecting printers");
            ShowStatusMessage($"Error: {ex.Message}", false);
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    private async Task TestPrintAsync()
    {
        if (SelectedPrinter == null)
        {
            ShowStatusMessage("Please select a printer first", false);
            return;
        }

        await ExecuteAsync(async () =>
        {
            // Apply current settings to the printer before testing
            ApplySettingsToPrinter(SelectedPrinter);

            using var scope = _scopeFactory.CreateScope();
            var printerService = scope.ServiceProvider.GetRequiredService<IPrinterService>();

            var result = await printerService.TestPrintAsync(SelectedPrinter);

            if (result.Success)
            {
                ShowStatusMessage("Test page printed successfully!", true);
            }
            else
            {
                ShowStatusMessage($"Print failed: {result.ErrorMessage}", false);
            }
        }, "Printing test page...");
    }

    [RelayCommand]
    private async Task CheckStatusAsync()
    {
        if (SelectedPrinter == null)
        {
            ShowStatusMessage("Please select a printer first", false);
            return;
        }

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var printerService = scope.ServiceProvider.GetRequiredService<IPrinterService>();

            var status = await printerService.CheckPrinterStatusAsync(SelectedPrinter);

            var statusMessage = status.Status switch
            {
                PrinterStatus.Online => "Printer is online and ready",
                PrinterStatus.Offline => "Printer is offline or disconnected",
                PrinterStatus.PaperOut => "Printer is out of paper",
                PrinterStatus.CoverOpen => "Printer cover is open",
                PrinterStatus.Error => $"Printer error: {status.ErrorMessage}",
                _ => "Printer status unknown"
            };

            ShowStatusMessage(statusMessage, status.Status == PrinterStatus.Online);

            // Update the printer in the list
            if (SelectedPrinter != null)
            {
                SelectedPrinter.Status = status.Status;
                SelectedPrinter.LastStatusCheck = DateTime.UtcNow;
                SelectedPrinter.LastError = status.ErrorMessage;
            }
        }, "Checking printer status...");
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedPrinter == null)
        {
            ShowStatusMessage("Please select a printer first", false);
            return;
        }

        if (string.IsNullOrWhiteSpace(PrinterName))
        {
            ShowStatusMessage("Please enter a printer name", false);
            return;
        }

        await ExecuteAsync(async () =>
        {
            ApplySettingsToPrinter(SelectedPrinter);

            using var scope = _scopeFactory.CreateScope();
            var printerService = scope.ServiceProvider.GetRequiredService<IPrinterService>();

            await printerService.SavePrinterAsync(SelectedPrinter);

            ShowStatusMessage("Printer settings saved successfully!", true);
        }, "Saving printer settings...");
    }

    [RelayCommand]
    private async Task SetAsDefaultAsync()
    {
        if (SelectedPrinter == null)
        {
            ShowStatusMessage("Please select a printer first", false);
            return;
        }

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var printerService = scope.ServiceProvider.GetRequiredService<IPrinterService>();

            await printerService.SetDefaultPrinterAsync(SelectedPrinter.Id, PrinterType.Receipt);

            // Update the list
            foreach (var printer in Printers)
            {
                printer.IsDefault = printer.Id == SelectedPrinter.Id;
            }

            ShowStatusMessage($"{SelectedPrinter.Name} is now the default receipt printer", true);
        }, "Setting default printer...");
    }

    [RelayCommand]
    private async Task AddPrinterAsync()
    {
        await ExecuteAsync(async () =>
        {
            var printer = new Printer
            {
                Name = "New Printer",
                Type = PrinterType.Receipt,
                ConnectionType = PrinterConnectionType.WindowsDriver,
                PaperWidth = 80,
                CharsPerLine = 48,
                Settings = new PrinterSettings
                {
                    UseEscPos = true,
                    AutoCut = true,
                    PartialCut = true,
                    OpenCashDrawer = true,
                    CutFeedLines = 3,
                    PrintLogo = true,
                    PrintDensity = 7
                }
            };

            using var scope = _scopeFactory.CreateScope();
            var printerService = scope.ServiceProvider.GetRequiredService<IPrinterService>();

            await printerService.SavePrinterAsync(printer);

            Printers.Add(printer);
            SelectedPrinter = printer;

            ShowStatusMessage("New printer added. Please configure the settings.", true);
        }, "Adding new printer...");
    }

    [RelayCommand]
    private async Task DeletePrinterAsync()
    {
        if (SelectedPrinter == null)
        {
            ShowStatusMessage("Please select a printer first", false);
            return;
        }

        if (SelectedPrinter.IsDefault)
        {
            ShowStatusMessage("Cannot delete the default printer. Set another printer as default first.", false);
            return;
        }

        var confirmed = await DialogService.ShowConfirmAsync(
            "Delete Printer",
            $"Are you sure you want to delete {SelectedPrinter.Name}?");

        if (!confirmed)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var printerService = scope.ServiceProvider.GetRequiredService<IPrinterService>();

            await printerService.DeletePrinterAsync(SelectedPrinter.Id);

            Printers.Remove(SelectedPrinter);
            SelectedPrinter = Printers.FirstOrDefault();

            ShowStatusMessage("Printer deleted successfully", true);
        }, "Deleting printer...");
    }

    #endregion

    #region Helper Methods

    private void ApplySettingsToPrinter(Printer printer)
    {
        printer.Name = PrinterName;
        printer.ConnectionType = ConnectionType;
        printer.PaperWidth = PaperWidth;
        printer.CharsPerLine = PaperWidth == 80 ? 48 : 32;

        switch (ConnectionType)
        {
            case PrinterConnectionType.WindowsDriver:
                printer.WindowsPrinterName = WindowsPrinterName;
                break;
            case PrinterConnectionType.Network:
                printer.IpAddress = IpAddress;
                printer.Port = Port;
                break;
            case PrinterConnectionType.Serial:
                printer.PortName = SerialPort;
                break;
        }

        if (printer.Settings == null)
        {
            printer.Settings = new PrinterSettings { PrinterId = printer.Id };
        }

        printer.Settings.AutoCut = AutoCut;
        printer.Settings.PartialCut = PartialCut;
        printer.Settings.OpenCashDrawer = OpenCashDrawer;
        printer.Settings.PrintLogo = PrintLogo;
        printer.Settings.BeepOnPrint = BeepOnPrint;
        printer.Settings.CutFeedLines = CutFeedLines;
        printer.Settings.ReceiptCopies = ReceiptCopies;
        printer.Settings.AutoPrintOnSettlement = AutoPrintOnSettlement;
        printer.Settings.FooterMessage = FooterMessage;
        printer.Settings.PrintCustomerCopy = PrintCustomerCopy;
    }

    private void ShowStatusMessage(string message, bool isSuccess)
    {
        StatusMessage = message;
        HasStatusMessage = true;
        IsStatusSuccess = isSuccess;
    }

    private void ClearStatusMessage()
    {
        StatusMessage = string.Empty;
        HasStatusMessage = false;
        IsStatusSuccess = false;
    }

    #endregion
}
