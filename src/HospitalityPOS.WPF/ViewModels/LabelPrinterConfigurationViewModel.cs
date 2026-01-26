using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Collections.ObjectModel;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for Label Printer Configuration.
/// Manages label printers, templates, and label sizes with full CRUD operations.
/// </summary>
public partial class LabelPrinterConfigurationViewModel : ViewModelBase, INavigationAware
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogService;
    private readonly ISessionService _sessionService;

    #region Observable Properties - Collections

    [ObservableProperty]
    private ObservableCollection<LabelPrinterDto> _printers = new();

    [ObservableProperty]
    private LabelPrinterDto? _selectedPrinter;

    [ObservableProperty]
    private ObservableCollection<LabelTemplateDto> _templates = new();

    [ObservableProperty]
    private LabelTemplateDto? _selectedTemplate;

    [ObservableProperty]
    private ObservableCollection<LabelSizeDto> _labelSizes = new();

    [ObservableProperty]
    private LabelSizeDto? _selectedLabelSize;

    #endregion

    #region Observable Properties - Form Fields

    [ObservableProperty]
    private bool _isFormVisible;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private int _editingPrinterId;

    [ObservableProperty]
    private string _formName = string.Empty;

    [ObservableProperty]
    private string _formConnectionString = string.Empty;

    [ObservableProperty]
    private LabelPrinterTypeDto _formPrinterType = LabelPrinterTypeDto.Network;

    [ObservableProperty]
    private LabelPrintLanguageDto _formPrintLanguage = LabelPrintLanguageDto.ZPL;

    [ObservableProperty]
    private int? _formDefaultLabelSizeId;

    [ObservableProperty]
    private bool _formIsDefault;

    [ObservableProperty]
    private int? _formBaudRate = 9600;

    [ObservableProperty]
    private int? _formPort = 9100;

    [ObservableProperty]
    private int? _formTimeoutMs = 5000;

    // Connection type options
    public Array PrinterTypes => Enum.GetValues(typeof(LabelPrinterTypeDto));
    public Array PrintLanguages => Enum.GetValues(typeof(LabelPrintLanguageDto));

    // Computed visibility for connection-specific fields
    public bool ShowSerialFields => FormPrinterType == LabelPrinterTypeDto.Serial;
    public bool ShowNetworkFields => FormPrinterType == LabelPrinterTypeDto.Network;

    #endregion

    #region Observable Properties - Label Size Form

    [ObservableProperty]
    private bool _isLabelSizeFormVisible;

    [ObservableProperty]
    private bool _isLabelSizeEditMode;

    [ObservableProperty]
    private int _editingLabelSizeId;

    [ObservableProperty]
    private string _labelSizeFormName = string.Empty;

    [ObservableProperty]
    private decimal _labelSizeFormWidth;

    [ObservableProperty]
    private decimal _labelSizeFormHeight;

    [ObservableProperty]
    private int _labelSizeFormDotsPerMm = 8;

    [ObservableProperty]
    private string _labelSizeFormDescription = string.Empty;

    #endregion

    #region Observable Properties - State

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private int _selectedTabIndex = 0;

    #endregion

    public LabelPrinterConfigurationViewModel(
        ILogger logger,
        IServiceScopeFactory scopeFactory,
        IDialogService dialogService,
        ISessionService sessionService)
        : base(logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));

        _logger.Information("LabelPrinterConfigurationViewModel initialized");
    }

    partial void OnFormPrinterTypeChanged(LabelPrinterTypeDto value)
    {
        OnPropertyChanged(nameof(ShowSerialFields));
        OnPropertyChanged(nameof(ShowNetworkFields));

        // Set default connection string placeholder
        FormConnectionString = value switch
        {
            LabelPrinterTypeDto.Serial => "COM1",
            LabelPrinterTypeDto.Network => "192.168.1.100",
            LabelPrinterTypeDto.USB => "USB001",
            LabelPrinterTypeDto.Windows => "Zebra ZD420",
            _ => string.Empty
        };
    }

    #region INavigationAware

    public Task OnNavigatedTo(object? parameter)
    {
        return LoadDataAsync();
    }

    public Task OnNavigatedFrom()
    {
        return Task.CompletedTask;
    }

    #endregion

    #region Load Data

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading label printer configuration...";

            using var scope = _scopeFactory.CreateScope();

            // Load printers
            var printerService = scope.ServiceProvider.GetService<ILabelPrinterService>();
            if (printerService != null)
            {
                var printers = await printerService.GetAllPrintersAsync(_sessionService.CurrentStoreId ?? 1);
                Printers = new ObservableCollection<LabelPrinterDto>(printers);
            }

            // Load templates
            var templateService = scope.ServiceProvider.GetService<ILabelTemplateService>();
            if (templateService != null)
            {
                var templates = await templateService.GetAllTemplatesAsync(_sessionService.CurrentStoreId ?? 1);
                Templates = new ObservableCollection<LabelTemplateDto>(templates);
            }

            // Load label sizes
            if (printerService != null)
            {
                var sizes = await printerService.GetAllLabelSizesAsync();
                LabelSizes = new ObservableCollection<LabelSizeDto>(sizes);
            }

            StatusMessage = $"Loaded {Printers.Count} printers, {Templates.Count} templates, {LabelSizes.Count} label sizes";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load label printer configuration");
            StatusMessage = "Failed to load data. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void RefreshData()
    {
        _ = LoadDataAsync();
    }

    #endregion

    #region Printer CRUD Operations

    [RelayCommand]
    private void ShowAddPrinterForm()
    {
        IsEditMode = false;
        EditingPrinterId = 0;
        FormName = string.Empty;
        FormConnectionString = "192.168.1.100";
        FormPrinterType = LabelPrinterTypeDto.Network;
        FormPrintLanguage = LabelPrintLanguageDto.ZPL;
        FormDefaultLabelSizeId = LabelSizes.FirstOrDefault()?.Id;
        FormIsDefault = Printers.Count == 0;
        FormBaudRate = 9600;
        FormPort = 9100;
        FormTimeoutMs = 5000;
        IsFormVisible = true;
    }

    [RelayCommand]
    private void ShowEditPrinterForm(LabelPrinterDto? printer)
    {
        if (printer == null) return;

        IsEditMode = true;
        EditingPrinterId = printer.Id;
        FormName = printer.Name;
        FormConnectionString = printer.ConnectionString;
        FormPrinterType = printer.PrinterType;
        FormPrintLanguage = printer.PrintLanguage;
        FormDefaultLabelSizeId = printer.DefaultLabelSizeId;
        FormIsDefault = printer.IsDefault;
        FormBaudRate = printer.BaudRate ?? 9600;
        FormPort = printer.Port ?? 9100;
        FormTimeoutMs = printer.TimeoutMs ?? 5000;
        IsFormVisible = true;
    }

    [RelayCommand]
    private void CancelPrinterForm()
    {
        IsFormVisible = false;
    }

    [RelayCommand]
    private async Task SavePrinterAsync()
    {
        if (string.IsNullOrWhiteSpace(FormName))
        {
            await _dialogService.ShowMessageAsync("Validation Error", "Please enter a printer name.");
            return;
        }

        if (string.IsNullOrWhiteSpace(FormConnectionString))
        {
            await _dialogService.ShowMessageAsync("Validation Error", "Please enter a connection string.");
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = IsEditMode ? "Updating printer..." : "Creating printer...";

            using var scope = _scopeFactory.CreateScope();
            var printerService = scope.ServiceProvider.GetRequiredService<ILabelPrinterService>();

            if (IsEditMode)
            {
                var dto = new UpdateLabelPrinterDto
                {
                    Name = FormName,
                    ConnectionString = FormConnectionString,
                    PrinterType = FormPrinterType,
                    PrintLanguage = FormPrintLanguage,
                    DefaultLabelSizeId = FormDefaultLabelSizeId,
                    IsDefault = FormIsDefault,
                    BaudRate = ShowSerialFields ? FormBaudRate : null,
                    Port = ShowNetworkFields ? FormPort : null,
                    TimeoutMs = FormTimeoutMs
                };

                await printerService.UpdatePrinterAsync(EditingPrinterId, dto);
                StatusMessage = "Printer updated successfully";
            }
            else
            {
                var dto = new CreateLabelPrinterDto
                {
                    Name = FormName,
                    ConnectionString = FormConnectionString,
                    StoreId = _sessionService.CurrentStoreId ?? 1,
                    PrinterType = FormPrinterType,
                    PrintLanguage = FormPrintLanguage,
                    DefaultLabelSizeId = FormDefaultLabelSizeId,
                    IsDefault = FormIsDefault,
                    BaudRate = ShowSerialFields ? FormBaudRate : null,
                    Port = ShowNetworkFields ? FormPort : null,
                    TimeoutMs = FormTimeoutMs
                };

                await printerService.CreatePrinterAsync(dto);
                StatusMessage = "Printer created successfully";
            }

            IsFormVisible = false;
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save printer");
            StatusMessage = $"Error: {ex.Message}";
            await _dialogService.ShowMessageAsync("Error", $"Failed to save printer: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeletePrinterAsync(LabelPrinterDto? printer)
    {
        if (printer == null) return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Delete Printer",
            $"Are you sure you want to delete the printer '{printer.Name}'?\n\nThis action cannot be undone.");

        if (!confirmed) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Deleting printer...";

            using var scope = _scopeFactory.CreateScope();
            var printerService = scope.ServiceProvider.GetRequiredService<ILabelPrinterService>();

            await printerService.DeletePrinterAsync(printer.Id);

            Printers.Remove(printer);
            StatusMessage = "Printer deleted successfully";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to delete printer {PrinterId}", printer.Id);
            StatusMessage = $"Error: {ex.Message}";
            await _dialogService.ShowMessageAsync("Error", $"Failed to delete printer: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SetDefaultPrinterAsync(LabelPrinterDto? printer)
    {
        if (printer == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = $"Setting {printer.Name} as default...";

            using var scope = _scopeFactory.CreateScope();
            var printerService = scope.ServiceProvider.GetRequiredService<ILabelPrinterService>();

            await printerService.SetDefaultPrinterAsync(printer.Id, _sessionService.CurrentStoreId ?? 1);

            // Update local state
            foreach (var p in Printers)
            {
                p.IsDefault = p.Id == printer.Id;
            }

            // Force refresh to update UI
            await LoadDataAsync();
            StatusMessage = $"{printer.Name} is now the default printer";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to set default printer {PrinterId}", printer.Id);
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Printer Operations

    [RelayCommand]
    private async Task TestPrinterConnectionAsync()
    {
        if (SelectedPrinter == null)
        {
            StatusMessage = "Please select a printer first";
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = $"Testing connection to {SelectedPrinter.Name}...";

            using var scope = _scopeFactory.CreateScope();
            var printerService = scope.ServiceProvider.GetRequiredService<ILabelPrinterService>();
            var result = await printerService.TestPrinterConnectionAsync(SelectedPrinter.Id);

            if (result.Success)
            {
                StatusMessage = $"Connection successful! Response time: {result.ResponseTimeMs}ms";
                await _dialogService.ShowMessageAsync("Connection Test",
                    $"Connection to '{SelectedPrinter.Name}' successful!\n\nResponse time: {result.ResponseTimeMs}ms\n{result.PrinterInfo ?? ""}");
            }
            else
            {
                StatusMessage = $"Connection failed: {result.Message}";
                await _dialogService.ShowMessageAsync("Connection Test Failed",
                    $"Failed to connect to '{SelectedPrinter.Name}'.\n\n{result.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to test printer connection");
            StatusMessage = $"Test failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task PrintTestLabelAsync()
    {
        if (SelectedPrinter == null)
        {
            StatusMessage = "Please select a printer first";
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = $"Printing test label to {SelectedPrinter.Name}...";

            using var scope = _scopeFactory.CreateScope();
            var printerService = scope.ServiceProvider.GetRequiredService<ILabelPrinterService>();
            var result = await printerService.PrintTestLabelAsync(SelectedPrinter.Id);

            if (result.Success)
            {
                StatusMessage = "Test label printed successfully!";
                await _dialogService.ShowMessageAsync("Test Label",
                    $"Test label sent to '{SelectedPrinter.Name}' successfully!");
            }
            else
            {
                StatusMessage = $"Print failed: {result.Message}";
                await _dialogService.ShowMessageAsync("Print Failed",
                    $"Failed to print test label.\n\n{result.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to print test label");
            StatusMessage = $"Print failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Label Size CRUD Operations

    [RelayCommand]
    private void ShowAddLabelSizeForm()
    {
        IsLabelSizeEditMode = false;
        EditingLabelSizeId = 0;
        LabelSizeFormName = string.Empty;
        LabelSizeFormWidth = 50;
        LabelSizeFormHeight = 25;
        LabelSizeFormDotsPerMm = 8;
        LabelSizeFormDescription = string.Empty;
        IsLabelSizeFormVisible = true;
    }

    [RelayCommand]
    private void ShowEditLabelSizeForm(LabelSizeDto? size)
    {
        if (size == null) return;

        IsLabelSizeEditMode = true;
        EditingLabelSizeId = size.Id;
        LabelSizeFormName = size.Name;
        LabelSizeFormWidth = size.WidthMm;
        LabelSizeFormHeight = size.HeightMm;
        LabelSizeFormDotsPerMm = size.DotsPerMm;
        LabelSizeFormDescription = size.Description ?? string.Empty;
        IsLabelSizeFormVisible = true;
    }

    [RelayCommand]
    private void CancelLabelSizeForm()
    {
        IsLabelSizeFormVisible = false;
    }

    [RelayCommand]
    private async Task SaveLabelSizeAsync()
    {
        if (string.IsNullOrWhiteSpace(LabelSizeFormName))
        {
            await _dialogService.ShowMessageAsync("Validation Error", "Please enter a label size name.");
            return;
        }

        if (LabelSizeFormWidth <= 0 || LabelSizeFormHeight <= 0)
        {
            await _dialogService.ShowMessageAsync("Validation Error", "Width and height must be greater than zero.");
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = IsLabelSizeEditMode ? "Updating label size..." : "Creating label size...";

            using var scope = _scopeFactory.CreateScope();
            var printerService = scope.ServiceProvider.GetRequiredService<ILabelPrinterService>();

            if (IsLabelSizeEditMode)
            {
                var dto = new UpdateLabelSizeDto
                {
                    Name = LabelSizeFormName,
                    WidthMm = LabelSizeFormWidth,
                    HeightMm = LabelSizeFormHeight,
                    DotsPerMm = LabelSizeFormDotsPerMm,
                    Description = string.IsNullOrWhiteSpace(LabelSizeFormDescription) ? null : LabelSizeFormDescription
                };

                await printerService.UpdateLabelSizeAsync(EditingLabelSizeId, dto);
                StatusMessage = "Label size updated successfully";
            }
            else
            {
                var dto = new CreateLabelSizeDto
                {
                    Name = LabelSizeFormName,
                    WidthMm = LabelSizeFormWidth,
                    HeightMm = LabelSizeFormHeight,
                    DotsPerMm = LabelSizeFormDotsPerMm,
                    Description = string.IsNullOrWhiteSpace(LabelSizeFormDescription) ? null : LabelSizeFormDescription
                };

                await printerService.CreateLabelSizeAsync(dto);
                StatusMessage = "Label size created successfully";
            }

            IsLabelSizeFormVisible = false;
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save label size");
            StatusMessage = $"Error: {ex.Message}";
            await _dialogService.ShowMessageAsync("Error", $"Failed to save label size: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteLabelSizeAsync(LabelSizeDto? size)
    {
        if (size == null) return;

        if (size.TemplateCount > 0)
        {
            await _dialogService.ShowMessageAsync("Cannot Delete",
                $"This label size is used by {size.TemplateCount} template(s). Please remove or reassign those templates first.");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Delete Label Size",
            $"Are you sure you want to delete the label size '{size.Name}'?");

        if (!confirmed) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Deleting label size...";

            using var scope = _scopeFactory.CreateScope();
            var printerService = scope.ServiceProvider.GetRequiredService<ILabelPrinterService>();

            await printerService.DeleteLabelSizeAsync(size.Id);

            LabelSizes.Remove(size);
            StatusMessage = "Label size deleted successfully";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to delete label size {SizeId}", size.Id);
            StatusMessage = $"Error: {ex.Message}";
            await _dialogService.ShowMessageAsync("Error", $"Failed to delete label size: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Navigation Commands

    [RelayCommand]
    private void GoBack()
    {
        // Navigate back to previous view
        // This would typically be handled by the navigation service
    }

    #endregion
}
