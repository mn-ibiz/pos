using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the advanced terminal configuration editor.
/// Provides a user-friendly interface for configuring terminal hardware and settings.
/// </summary>
public partial class TerminalConfigurationViewModel : ViewModelBase, INavigationAware
{
    private readonly ITerminalService _terminalService;
    private readonly IPrinterService _printerService;
    private readonly ISessionService _sessionService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;

    private int _terminalId;
    private Terminal? _terminal;
    private TerminalPrinterConfig? _originalPrinterConfig;
    private TerminalHardwareConfig? _originalHardwareConfig;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _pageTitle = "Terminal Configuration";

    [ObservableProperty]
    private string _terminalCode = string.Empty;

    [ObservableProperty]
    private string _terminalName = string.Empty;

    // ==================== Printer Configuration ====================

    [ObservableProperty]
    private ObservableCollection<PrinterDisplayItem> _availablePrinters = [];

    [ObservableProperty]
    private PrinterDisplayItem? _selectedReceiptPrinter;

    [ObservableProperty]
    private int _receiptPaperWidth = 80;

    [ObservableProperty]
    private bool _enableReceiptPrinting = true;

    [ObservableProperty]
    private bool _autoPrintReceipt = true;

    [ObservableProperty]
    private int _receiptCopies = 1;

    [ObservableProperty]
    private PrinterDisplayItem? _selectedKitchenPrinter;

    [ObservableProperty]
    private bool _enableKitchenPrinting;

    [ObservableProperty]
    private PrinterDisplayItem? _selectedLabelPrinter;

    [ObservableProperty]
    private bool _enableLabelPrinting;

    // ==================== Cash Drawer Configuration ====================

    [ObservableProperty]
    private bool _enableCashDrawer = true;

    [ObservableProperty]
    private string _cashDrawerPort = "COM1";

    [ObservableProperty]
    private bool _autoOpenOnCash = true;

    [ObservableProperty]
    private ObservableCollection<string> _availablePorts = [];

    // ==================== Customer Display Configuration ====================

    [ObservableProperty]
    private bool _enableCustomerDisplay;

    [ObservableProperty]
    private string _customerDisplayPort = "COM2";

    [ObservableProperty]
    private int _customerDisplayLines = 2;

    [ObservableProperty]
    private int _customerDisplayCharsPerLine = 20;

    // ==================== Scale Configuration ====================

    [ObservableProperty]
    private bool _enableScale;

    [ObservableProperty]
    private string _scalePort = "COM3";

    [ObservableProperty]
    private string _scaleBaudRate = "9600";

    [ObservableProperty]
    private ObservableCollection<string> _baudRates = ["2400", "4800", "9600", "19200", "38400", "57600", "115200"];

    // ==================== Barcode Scanner Configuration ====================

    [ObservableProperty]
    private bool _enableBarcodeScanner = true;

    [ObservableProperty]
    private string _scannerMode = "Keyboard Wedge";

    [ObservableProperty]
    private ObservableCollection<string> _scannerModes = ["Keyboard Wedge", "Serial (COM)", "USB HID"];

    [ObservableProperty]
    private string _scannerPort = "COM4";

    // ==================== Feature Toggles ====================

    [ObservableProperty]
    private bool _allowRefunds = true;

    [ObservableProperty]
    private bool _allowVoids = true;

    [ObservableProperty]
    private bool _allowDiscounts = true;

    [ObservableProperty]
    private bool _allowPriceOverride;

    [ObservableProperty]
    private bool _requireManagerForRefund;

    [ObservableProperty]
    private bool _requireManagerForVoid;

    [ObservableProperty]
    private bool _requireManagerForDiscount;

    [ObservableProperty]
    private bool _enableOfflineMode;

    [ObservableProperty]
    private bool _enableTrainingMode;

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalConfigurationViewModel"/> class.
    /// </summary>
    public TerminalConfigurationViewModel(
        ITerminalService terminalService,
        IPrinterService printerService,
        ISessionService sessionService,
        INavigationService navigationService,
        IDialogService dialogService,
        ILogger logger) : base(logger)
    {
        _terminalService = terminalService ?? throw new ArgumentNullException(nameof(terminalService));
        _printerService = printerService ?? throw new ArgumentNullException(nameof(printerService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        // Initialize available COM ports
        AvailablePorts = ["COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8"];
    }

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
        if (parameter is int terminalId)
        {
            _terminalId = terminalId;
            _ = LoadConfigurationAsync();
        }
        else
        {
            _dialogService.ShowErrorAsync("Error", "No terminal specified.");
            _navigationService.GoBack();
        }
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
    }

    private async Task LoadConfigurationAsync()
    {
        try
        {
            IsLoading = true;

            // Load terminal
            _terminal = await _terminalService.GetTerminalByIdAsync(_terminalId).ConfigureAwait(true);

            if (_terminal is null)
            {
                await _dialogService.ShowErrorAsync("Error", "Terminal not found.");
                _navigationService.GoBack();
                return;
            }

            TerminalCode = _terminal.Code;
            TerminalName = _terminal.Name;
            PageTitle = $"Configure: {_terminal.Name}";

            // Load available printers
            await LoadPrintersAsync();

            // Parse printer configuration
            LoadPrinterConfiguration();

            // Parse hardware configuration
            LoadHardwareConfiguration();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error loading terminal configuration for terminal {TerminalId}", _terminalId);
            await _dialogService.ShowErrorAsync("Error", $"Failed to load configuration: {ex.Message}");
            _navigationService.GoBack();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadPrintersAsync()
    {
        try
        {
            var storeId = _sessionService.CurrentStoreId ?? 1;
            var printers = await _printerService.GetAllPrintersAsync(storeId).ConfigureAwait(true);

            AvailablePrinters.Clear();
            AvailablePrinters.Add(new PrinterDisplayItem { Id = 0, Name = "(None)" });

            foreach (var printer in printers.Where(p => p.IsActive))
            {
                AvailablePrinters.Add(new PrinterDisplayItem
                {
                    Id = printer.Id,
                    Name = printer.Name,
                    PrinterName = printer.PrinterName ?? string.Empty,
                    PrinterType = printer.PrinterType.ToString()
                });
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to load printers");
        }
    }

    private void LoadPrinterConfiguration()
    {
        if (string.IsNullOrEmpty(_terminal?.PrinterConfiguration))
        {
            _originalPrinterConfig = new TerminalPrinterConfig();
            return;
        }

        try
        {
            _originalPrinterConfig = JsonSerializer.Deserialize<TerminalPrinterConfig>(_terminal.PrinterConfiguration)
                ?? new TerminalPrinterConfig();

            // Apply to UI
            EnableReceiptPrinting = _originalPrinterConfig.EnableReceiptPrinting;
            AutoPrintReceipt = _originalPrinterConfig.AutoPrintReceipt;
            ReceiptPaperWidth = _originalPrinterConfig.ReceiptPaperWidth;
            ReceiptCopies = _originalPrinterConfig.ReceiptCopies;
            EnableKitchenPrinting = _originalPrinterConfig.EnableKitchenPrinting;
            EnableLabelPrinting = _originalPrinterConfig.EnableLabelPrinting;

            // Set selected printers
            SelectedReceiptPrinter = AvailablePrinters.FirstOrDefault(p => p.Id == _originalPrinterConfig.ReceiptPrinterId)
                ?? AvailablePrinters.FirstOrDefault();
            SelectedKitchenPrinter = AvailablePrinters.FirstOrDefault(p => p.Id == _originalPrinterConfig.KitchenPrinterId)
                ?? AvailablePrinters.FirstOrDefault();
            SelectedLabelPrinter = AvailablePrinters.FirstOrDefault(p => p.Id == _originalPrinterConfig.LabelPrinterId)
                ?? AvailablePrinters.FirstOrDefault();
        }
        catch (JsonException ex)
        {
            _logger.Warning(ex, "Failed to parse printer configuration JSON");
            _originalPrinterConfig = new TerminalPrinterConfig();
        }
    }

    private void LoadHardwareConfiguration()
    {
        if (string.IsNullOrEmpty(_terminal?.HardwareConfiguration))
        {
            _originalHardwareConfig = new TerminalHardwareConfig();
            return;
        }

        try
        {
            _originalHardwareConfig = JsonSerializer.Deserialize<TerminalHardwareConfig>(_terminal.HardwareConfiguration)
                ?? new TerminalHardwareConfig();

            // Apply to UI - Cash Drawer
            EnableCashDrawer = _originalHardwareConfig.EnableCashDrawer;
            CashDrawerPort = _originalHardwareConfig.CashDrawerPort ?? "COM1";
            AutoOpenOnCash = _originalHardwareConfig.AutoOpenOnCash;

            // Customer Display
            EnableCustomerDisplay = _originalHardwareConfig.EnableCustomerDisplay;
            CustomerDisplayPort = _originalHardwareConfig.CustomerDisplayPort ?? "COM2";
            CustomerDisplayLines = _originalHardwareConfig.CustomerDisplayLines;
            CustomerDisplayCharsPerLine = _originalHardwareConfig.CustomerDisplayCharsPerLine;

            // Scale
            EnableScale = _originalHardwareConfig.EnableScale;
            ScalePort = _originalHardwareConfig.ScalePort ?? "COM3";
            ScaleBaudRate = _originalHardwareConfig.ScaleBaudRate ?? "9600";

            // Barcode Scanner
            EnableBarcodeScanner = _originalHardwareConfig.EnableBarcodeScanner;
            ScannerMode = _originalHardwareConfig.ScannerMode ?? "Keyboard Wedge";
            ScannerPort = _originalHardwareConfig.ScannerPort ?? "COM4";

            // Feature Toggles
            AllowRefunds = _originalHardwareConfig.AllowRefunds;
            AllowVoids = _originalHardwareConfig.AllowVoids;
            AllowDiscounts = _originalHardwareConfig.AllowDiscounts;
            AllowPriceOverride = _originalHardwareConfig.AllowPriceOverride;
            RequireManagerForRefund = _originalHardwareConfig.RequireManagerForRefund;
            RequireManagerForVoid = _originalHardwareConfig.RequireManagerForVoid;
            RequireManagerForDiscount = _originalHardwareConfig.RequireManagerForDiscount;
            EnableOfflineMode = _originalHardwareConfig.EnableOfflineMode;
            EnableTrainingMode = _originalHardwareConfig.EnableTrainingMode;
        }
        catch (JsonException ex)
        {
            _logger.Warning(ex, "Failed to parse hardware configuration JSON");
            _originalHardwareConfig = new TerminalHardwareConfig();
        }
    }

    /// <summary>
    /// Saves the terminal configuration.
    /// </summary>
    [RelayCommand]
    private async Task SaveConfigurationAsync()
    {
        if (_terminal is null || _sessionService.CurrentUser is null)
        {
            return;
        }

        try
        {
            IsLoading = true;

            // Build printer configuration
            var printerConfig = new TerminalPrinterConfig
            {
                EnableReceiptPrinting = EnableReceiptPrinting,
                AutoPrintReceipt = AutoPrintReceipt,
                ReceiptPrinterId = SelectedReceiptPrinter?.Id ?? 0,
                ReceiptPaperWidth = ReceiptPaperWidth,
                ReceiptCopies = ReceiptCopies,
                EnableKitchenPrinting = EnableKitchenPrinting,
                KitchenPrinterId = SelectedKitchenPrinter?.Id ?? 0,
                EnableLabelPrinting = EnableLabelPrinting,
                LabelPrinterId = SelectedLabelPrinter?.Id ?? 0
            };

            // Build hardware configuration
            var hardwareConfig = new TerminalHardwareConfig
            {
                EnableCashDrawer = EnableCashDrawer,
                CashDrawerPort = CashDrawerPort,
                AutoOpenOnCash = AutoOpenOnCash,
                EnableCustomerDisplay = EnableCustomerDisplay,
                CustomerDisplayPort = CustomerDisplayPort,
                CustomerDisplayLines = CustomerDisplayLines,
                CustomerDisplayCharsPerLine = CustomerDisplayCharsPerLine,
                EnableScale = EnableScale,
                ScalePort = ScalePort,
                ScaleBaudRate = ScaleBaudRate,
                EnableBarcodeScanner = EnableBarcodeScanner,
                ScannerMode = ScannerMode,
                ScannerPort = ScannerPort,
                AllowRefunds = AllowRefunds,
                AllowVoids = AllowVoids,
                AllowDiscounts = AllowDiscounts,
                AllowPriceOverride = AllowPriceOverride,
                RequireManagerForRefund = RequireManagerForRefund,
                RequireManagerForVoid = RequireManagerForVoid,
                RequireManagerForDiscount = RequireManagerForDiscount,
                EnableOfflineMode = EnableOfflineMode,
                EnableTrainingMode = EnableTrainingMode
            };

            // Serialize to JSON
            var printerJson = JsonSerializer.Serialize(printerConfig, new JsonSerializerOptions { WriteIndented = false });
            var hardwareJson = JsonSerializer.Serialize(hardwareConfig, new JsonSerializerOptions { WriteIndented = false });

            // Update terminal
            await _terminalService.UpdateTerminalConfigurationAsync(
                _terminalId,
                printerJson,
                hardwareJson,
                _sessionService.CurrentUser.Id).ConfigureAwait(true);

            await _dialogService.ShowInfoAsync("Success", "Terminal configuration saved successfully.");
            _navigationService.GoBack();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error saving terminal configuration");
            await _dialogService.ShowErrorAsync("Error", $"Failed to save configuration: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Resets configuration to defaults.
    /// </summary>
    [RelayCommand]
    private async Task ResetToDefaultsAsync()
    {
        var confirm = await _dialogService.ShowConfirmAsync(
            "Reset Configuration",
            "Are you sure you want to reset all settings to their defaults?");

        if (!confirm)
        {
            return;
        }

        // Reset printer settings
        EnableReceiptPrinting = true;
        AutoPrintReceipt = true;
        ReceiptPaperWidth = 80;
        ReceiptCopies = 1;
        EnableKitchenPrinting = false;
        EnableLabelPrinting = false;
        SelectedReceiptPrinter = AvailablePrinters.FirstOrDefault();
        SelectedKitchenPrinter = AvailablePrinters.FirstOrDefault();
        SelectedLabelPrinter = AvailablePrinters.FirstOrDefault();

        // Reset hardware settings
        EnableCashDrawer = true;
        CashDrawerPort = "COM1";
        AutoOpenOnCash = true;
        EnableCustomerDisplay = false;
        CustomerDisplayPort = "COM2";
        CustomerDisplayLines = 2;
        CustomerDisplayCharsPerLine = 20;
        EnableScale = false;
        ScalePort = "COM3";
        ScaleBaudRate = "9600";
        EnableBarcodeScanner = true;
        ScannerMode = "Keyboard Wedge";
        ScannerPort = "COM4";

        // Reset feature toggles
        AllowRefunds = true;
        AllowVoids = true;
        AllowDiscounts = true;
        AllowPriceOverride = false;
        RequireManagerForRefund = false;
        RequireManagerForVoid = false;
        RequireManagerForDiscount = false;
        EnableOfflineMode = false;
        EnableTrainingMode = false;
    }

    /// <summary>
    /// Tests the receipt printer connection.
    /// </summary>
    [RelayCommand]
    private async Task TestReceiptPrinterAsync()
    {
        if (SelectedReceiptPrinter is null || SelectedReceiptPrinter.Id == 0)
        {
            await _dialogService.ShowErrorAsync("Error", "No receipt printer selected.");
            return;
        }

        try
        {
            await _dialogService.ShowInfoAsync("Test Print", $"Test page would be sent to: {SelectedReceiptPrinter.Name}");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Error", $"Printer test failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Tests the cash drawer.
    /// </summary>
    [RelayCommand]
    private async Task TestCashDrawerAsync()
    {
        if (!EnableCashDrawer)
        {
            await _dialogService.ShowErrorAsync("Error", "Cash drawer is not enabled.");
            return;
        }

        try
        {
            await _dialogService.ShowInfoAsync("Test Drawer", $"Open command would be sent to: {CashDrawerPort}");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Error", $"Cash drawer test failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Cancels and goes back.
    /// </summary>
    [RelayCommand]
    private async Task CancelAsync()
    {
        var hasChanges = HasUnsavedChanges();

        if (hasChanges)
        {
            var confirm = await _dialogService.ShowConfirmAsync(
                "Unsaved Changes",
                "You have unsaved changes. Are you sure you want to discard them?");

            if (!confirm)
            {
                return;
            }
        }

        _navigationService.GoBack();
    }

    /// <summary>
    /// Goes back to the previous view.
    /// </summary>
    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    private bool HasUnsavedChanges()
    {
        // Compare current settings with original
        if (_originalPrinterConfig is null || _originalHardwareConfig is null)
        {
            return false;
        }

        return EnableReceiptPrinting != _originalPrinterConfig.EnableReceiptPrinting ||
               AutoPrintReceipt != _originalPrinterConfig.AutoPrintReceipt ||
               ReceiptPaperWidth != _originalPrinterConfig.ReceiptPaperWidth ||
               EnableCashDrawer != _originalHardwareConfig.EnableCashDrawer ||
               AllowRefunds != _originalHardwareConfig.AllowRefunds ||
               AllowVoids != _originalHardwareConfig.AllowVoids ||
               AllowDiscounts != _originalHardwareConfig.AllowDiscounts;
    }
}

/// <summary>
/// Printer display item for selection.
/// </summary>
public class PrinterDisplayItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PrinterName { get; set; } = string.Empty;
    public string PrinterType { get; set; } = string.Empty;
}

/// <summary>
/// Terminal printer configuration DTO.
/// </summary>
public class TerminalPrinterConfig
{
    public bool EnableReceiptPrinting { get; set; } = true;
    public bool AutoPrintReceipt { get; set; } = true;
    public int ReceiptPrinterId { get; set; }
    public int ReceiptPaperWidth { get; set; } = 80;
    public int ReceiptCopies { get; set; } = 1;
    public bool EnableKitchenPrinting { get; set; }
    public int KitchenPrinterId { get; set; }
    public bool EnableLabelPrinting { get; set; }
    public int LabelPrinterId { get; set; }
}

/// <summary>
/// Terminal hardware configuration DTO.
/// </summary>
public class TerminalHardwareConfig
{
    // Cash Drawer
    public bool EnableCashDrawer { get; set; } = true;
    public string? CashDrawerPort { get; set; } = "COM1";
    public bool AutoOpenOnCash { get; set; } = true;

    // Customer Display
    public bool EnableCustomerDisplay { get; set; }
    public string? CustomerDisplayPort { get; set; } = "COM2";
    public int CustomerDisplayLines { get; set; } = 2;
    public int CustomerDisplayCharsPerLine { get; set; } = 20;

    // Scale
    public bool EnableScale { get; set; }
    public string? ScalePort { get; set; } = "COM3";
    public string? ScaleBaudRate { get; set; } = "9600";

    // Barcode Scanner
    public bool EnableBarcodeScanner { get; set; } = true;
    public string? ScannerMode { get; set; } = "Keyboard Wedge";
    public string? ScannerPort { get; set; } = "COM4";

    // Feature Toggles
    public bool AllowRefunds { get; set; } = true;
    public bool AllowVoids { get; set; } = true;
    public bool AllowDiscounts { get; set; } = true;
    public bool AllowPriceOverride { get; set; }
    public bool RequireManagerForRefund { get; set; }
    public bool RequireManagerForVoid { get; set; }
    public bool RequireManagerForDiscount { get; set; }
    public bool EnableOfflineMode { get; set; }
    public bool EnableTrainingMode { get; set; }
}
