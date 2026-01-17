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
/// ViewModel for the kitchen printer settings management screen.
/// </summary>
public partial class KitchenPrinterSettingsViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;

    #region Observable Properties

    [ObservableProperty]
    private ObservableCollection<Printer> _kitchenPrinters = new();

    [ObservableProperty]
    private Printer? _selectedPrinter;

    [ObservableProperty]
    private ObservableCollection<CategorySelection> _categories = new();

    [ObservableProperty]
    private KOTSettings _kotSettings = new();

    [ObservableProperty]
    private string _printerName = string.Empty;

    [ObservableProperty]
    private PrinterConnectionType _connectionType;

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

    // KOT Settings Properties
    [ObservableProperty]
    private KOTFontSize _titleFontSize = KOTFontSize.Large;

    [ObservableProperty]
    private KOTFontSize _itemFontSize = KOTFontSize.Normal;

    [ObservableProperty]
    private KOTFontSize _modifierFontSize = KOTFontSize.Small;

    [ObservableProperty]
    private bool _showTableNumber = true;

    [ObservableProperty]
    private bool _showWaiterName = true;

    [ObservableProperty]
    private bool _showOrderTime = true;

    [ObservableProperty]
    private bool _showOrderNumber = true;

    [ObservableProperty]
    private bool _showCategoryHeader = true;

    [ObservableProperty]
    private bool _groupByCategory = true;

    [ObservableProperty]
    private bool _showQuantityLarge = true;

    [ObservableProperty]
    private bool _showModifiersIndented = true;

    [ObservableProperty]
    private bool _showNotesHighlighted = true;

    [ObservableProperty]
    private bool _highlightAllergies = true;

    [ObservableProperty]
    private bool _beepOnPrint = true;

    [ObservableProperty]
    private int _beepCount = 2;

    [ObservableProperty]
    private int _copiesPerOrder = 1;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasStatusMessage;

    [ObservableProperty]
    private bool _isStatusSuccess;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private string _kotPreview = string.Empty;

    #endregion

    public KitchenPrinterSettingsViewModel(
        IServiceScopeFactory scopeFactory,
        ILogger logger) : base(logger)
    {
        _scopeFactory = scopeFactory;
        Title = "Kitchen Printer Settings";
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
            var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryService>();
            var discoveryService = scope.ServiceProvider.GetRequiredService<IPrinterDiscoveryService>();

            // Load kitchen printers
            var printers = await printerService.GetPrintersAsync(PrinterType.Kitchen);
            KitchenPrinters = new ObservableCollection<Printer>(printers);

            // Load all categories
            var allCategories = await categoryService.GetActiveCategoriesAsync();
            Categories = new ObservableCollection<CategorySelection>(
                allCategories.Select(c => new CategorySelection { Category = c }));

            // Load available printers
            var windowsPrinters = await discoveryService.GetWindowsPrintersAsync();
            AvailableWindowsPrinters = new ObservableCollection<string>(windowsPrinters);

            var serialPorts = await discoveryService.GetSerialPortsAsync();
            AvailableSerialPorts = new ObservableCollection<string>(serialPorts);

            if (KitchenPrinters.Any())
            {
                SelectedPrinter = KitchenPrinters.FirstOrDefault(p => p.IsDefault) ?? KitchenPrinters.First();
            }

            UpdateKOTPreview();
        }, "Loading kitchen printer settings...");
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

    // Update preview when KOT settings change
    partial void OnShowTableNumberChanged(bool value) => UpdateKOTPreview();
    partial void OnShowWaiterNameChanged(bool value) => UpdateKOTPreview();
    partial void OnShowOrderTimeChanged(bool value) => UpdateKOTPreview();
    partial void OnShowOrderNumberChanged(bool value) => UpdateKOTPreview();
    partial void OnShowCategoryHeaderChanged(bool value) => UpdateKOTPreview();
    partial void OnGroupByCategoryChanged(bool value) => UpdateKOTPreview();
    partial void OnShowQuantityLargeChanged(bool value) => UpdateKOTPreview();
    partial void OnShowModifiersIndentedChanged(bool value) => UpdateKOTPreview();
    partial void OnShowNotesHighlightedChanged(bool value) => UpdateKOTPreview();

    private async void LoadPrinterDetails(Printer printer)
    {
        PrinterName = printer.Name;
        ConnectionType = printer.ConnectionType;
        WindowsPrinterName = printer.WindowsPrinterName ?? string.Empty;
        IpAddress = printer.IpAddress ?? string.Empty;
        Port = printer.Port ?? 9100;
        SerialPort = printer.PortName ?? string.Empty;
        PaperWidth = printer.PaperWidth;

        // Load category mappings
        using var scope = _scopeFactory.CreateScope();
        var printerService = scope.ServiceProvider.GetRequiredService<IPrinterService>();

        var mappings = await printerService.GetCategoryMappingsAsync(printer.Id);
        var mappedIds = mappings.Select(m => m.CategoryId).ToHashSet();

        foreach (var category in Categories)
        {
            category.IsSelected = mappedIds.Contains(category.Category.Id);
        }

        // Load KOT settings
        var kotSettings = await printerService.GetKOTSettingsAsync(printer.Id);
        if (kotSettings != null)
        {
            TitleFontSize = kotSettings.TitleFontSize;
            ItemFontSize = kotSettings.ItemFontSize;
            ModifierFontSize = kotSettings.ModifierFontSize;
            ShowTableNumber = kotSettings.ShowTableNumber;
            ShowWaiterName = kotSettings.ShowWaiterName;
            ShowOrderTime = kotSettings.ShowOrderTime;
            ShowOrderNumber = kotSettings.ShowOrderNumber;
            ShowCategoryHeader = kotSettings.ShowCategoryHeader;
            GroupByCategory = kotSettings.GroupByCategory;
            ShowQuantityLarge = kotSettings.ShowQuantityLarge;
            ShowModifiersIndented = kotSettings.ShowModifiersIndented;
            ShowNotesHighlighted = kotSettings.ShowNotesHighlighted;
            HighlightAllergies = kotSettings.HighlightAllergies;
            BeepOnPrint = kotSettings.BeepOnPrint;
            BeepCount = kotSettings.BeepCount;
            CopiesPerOrder = kotSettings.CopiesPerOrder;
        }
        else
        {
            // Default KOT settings
            ResetKOTSettings();
        }

        UpdateKOTPreview();
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

        foreach (var category in Categories)
        {
            category.IsSelected = false;
        }

        ResetKOTSettings();
    }

    private void ResetKOTSettings()
    {
        TitleFontSize = KOTFontSize.Large;
        ItemFontSize = KOTFontSize.Normal;
        ModifierFontSize = KOTFontSize.Small;
        ShowTableNumber = true;
        ShowWaiterName = true;
        ShowOrderTime = true;
        ShowOrderNumber = true;
        ShowCategoryHeader = true;
        GroupByCategory = true;
        ShowQuantityLarge = true;
        ShowModifiersIndented = true;
        ShowNotesHighlighted = true;
        HighlightAllergies = true;
        BeepOnPrint = true;
        BeepCount = 2;
        CopiesPerOrder = 1;
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
    private void SetTitleFontSize(string fontSizeStr)
    {
        if (Enum.TryParse<KOTFontSize>(fontSizeStr, out var fontSize))
        {
            TitleFontSize = fontSize;
            UpdateKOTPreview();
        }
    }

    [RelayCommand]
    private void SetItemFontSize(string fontSizeStr)
    {
        if (Enum.TryParse<KOTFontSize>(fontSizeStr, out var fontSize))
        {
            ItemFontSize = fontSize;
            UpdateKOTPreview();
        }
    }

    [RelayCommand]
    private void SetModifierFontSize(string fontSizeStr)
    {
        if (Enum.TryParse<KOTFontSize>(fontSizeStr, out var fontSize))
        {
            ModifierFontSize = fontSize;
            UpdateKOTPreview();
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
    private async Task AddKitchenPrinterAsync()
    {
        await ExecuteAsync(async () =>
        {
            var printer = new Printer
            {
                Name = "New Kitchen Printer",
                Type = PrinterType.Kitchen,
                ConnectionType = PrinterConnectionType.Network,
                PaperWidth = 80,
                CharsPerLine = 48,
                Settings = new PrinterSettings
                {
                    UseEscPos = true,
                    AutoCut = true,
                    PartialCut = true,
                    CutFeedLines = 3
                }
            };

            using var scope = _scopeFactory.CreateScope();
            var printerService = scope.ServiceProvider.GetRequiredService<IPrinterService>();

            await printerService.SavePrinterAsync(printer);

            // Create default KOT settings
            var kotSettings = new KOTSettings { PrinterId = printer.Id };
            await printerService.SaveKOTSettingsAsync(kotSettings);

            KitchenPrinters.Add(printer);
            SelectedPrinter = printer;

            ShowStatusMessage("New kitchen printer added. Please configure the settings.", true);
        }, "Adding new kitchen printer...");
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

            // Save printer
            await printerService.SavePrinterAsync(SelectedPrinter);

            // Save category mappings
            var selectedCategoryIds = Categories
                .Where(c => c.IsSelected)
                .Select(c => c.Category.Id)
                .ToList();

            await printerService.SaveCategoryMappingsAsync(SelectedPrinter.Id, selectedCategoryIds);

            // Save KOT settings
            var kotSettings = new KOTSettings
            {
                PrinterId = SelectedPrinter.Id,
                TitleFontSize = TitleFontSize,
                ItemFontSize = ItemFontSize,
                ModifierFontSize = ModifierFontSize,
                ShowTableNumber = ShowTableNumber,
                ShowWaiterName = ShowWaiterName,
                ShowOrderTime = ShowOrderTime,
                ShowOrderNumber = ShowOrderNumber,
                ShowCategoryHeader = ShowCategoryHeader,
                GroupByCategory = GroupByCategory,
                ShowQuantityLarge = ShowQuantityLarge,
                ShowModifiersIndented = ShowModifiersIndented,
                ShowNotesHighlighted = ShowNotesHighlighted,
                HighlightAllergies = HighlightAllergies,
                BeepOnPrint = BeepOnPrint,
                BeepCount = BeepCount,
                CopiesPerOrder = CopiesPerOrder
            };

            await printerService.SaveKOTSettingsAsync(kotSettings);

            ShowStatusMessage("Kitchen printer settings saved successfully!", true);
        }, "Saving kitchen printer settings...");
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
            ApplySettingsToPrinter(SelectedPrinter);

            using var scope = _scopeFactory.CreateScope();
            var printerService = scope.ServiceProvider.GetRequiredService<IPrinterService>();

            var result = await printerService.PrintTestKOTAsync(SelectedPrinter);

            if (result.Success)
            {
                ShowStatusMessage("Sample KOT printed successfully!", true);
            }
            else
            {
                ShowStatusMessage($"Print failed: {result.ErrorMessage}", false);
            }
        }, "Printing sample KOT...");
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
        }, "Checking printer status...");
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

            await printerService.SetDefaultPrinterAsync(SelectedPrinter.Id, PrinterType.Kitchen);

            // Update the list
            foreach (var printer in KitchenPrinters)
            {
                printer.IsDefault = printer.Id == SelectedPrinter.Id;
            }

            ShowStatusMessage($"{SelectedPrinter.Name} is now the default kitchen printer", true);
        }, "Setting default printer...");
    }

    [RelayCommand]
    private async Task DeletePrinterAsync()
    {
        if (SelectedPrinter == null)
        {
            ShowStatusMessage("Please select a printer first", false);
            return;
        }

        var confirmed = await DialogService.ShowConfirmAsync(
            "Delete Kitchen Printer",
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

            KitchenPrinters.Remove(SelectedPrinter);
            SelectedPrinter = KitchenPrinters.FirstOrDefault();

            ShowStatusMessage("Kitchen printer deleted successfully", true);
        }, "Deleting printer...");
    }

    [RelayCommand]
    private void SelectAllCategories()
    {
        foreach (var category in Categories)
        {
            category.IsSelected = true;
        }
    }

    [RelayCommand]
    private void DeselectAllCategories()
    {
        foreach (var category in Categories)
        {
            category.IsSelected = false;
        }
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
    }

    private void UpdateKOTPreview()
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("================================");
        sb.AppendLine("     ** KITCHEN ORDER **");
        sb.AppendLine("================================");

        if (ShowTableNumber)
            sb.AppendLine("TABLE: 07");

        if (ShowWaiterName)
            sb.AppendLine("WAITER: John Smith");

        if (ShowOrderTime)
            sb.AppendLine($"TIME: {DateTime.Now:HH:mm}");

        if (ShowOrderNumber)
            sb.AppendLine("ORDER: O-0042");

        sb.AppendLine("--------------------------------");

        if (GroupByCategory && ShowCategoryHeader)
        {
            sb.AppendLine("** FOOD **");
        }

        var qtyPrefix = ShowQuantityLarge ? "  2  " : "2x ";
        sb.AppendLine($"{qtyPrefix}Grilled Chicken");

        if (ShowModifiersIndented)
        {
            sb.AppendLine("      - Extra spicy");
            sb.AppendLine("      - No onions");
        }
        else
        {
            sb.AppendLine("- Extra spicy");
            sb.AppendLine("- No onions");
        }

        if (ShowNotesHighlighted)
            sb.AppendLine("      NOTE: Well done");

        sb.AppendLine();
        sb.AppendLine($"{(ShowQuantityLarge ? "  1  " : "1x ")}Fish & Chips");

        if (HighlightAllergies)
            sb.AppendLine("      *** GLUTEN FREE ***");

        sb.AppendLine("================================");
        sb.AppendLine("ITEMS: 3              NEW ORDER");
        sb.AppendLine("================================");

        KotPreview = sb.ToString();
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

/// <summary>
/// Represents a category selection for kitchen printer routing.
/// </summary>
public partial class CategorySelection : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// Gets or sets the category.
    /// </summary>
    public Category Category { get; set; } = null!;
}
