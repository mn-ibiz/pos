using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Collections.ObjectModel;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for Label Printer Configuration.
/// Manages label printers, templates, and label sizes.
/// </summary>
public partial class LabelPrinterConfigurationViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;

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
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Label Printer Configuration - Coming Soon";

    // Tab selection
    [ObservableProperty]
    private int _selectedTabIndex = 0;

    public LabelPrinterConfigurationViewModel(
        ILogger logger,
        IServiceScopeFactory scopeFactory)
        : base(logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

        _logger.Information("LabelPrinterConfigurationViewModel initialized");
    }

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
                var printers = await printerService.GetAllPrintersAsync(1); // TODO: Get actual store ID
                Printers = new ObservableCollection<LabelPrinterDto>(printers);
            }

            // Load templates
            var templateService = scope.ServiceProvider.GetService<ILabelTemplateService>();
            if (templateService != null)
            {
                var templates = await templateService.GetAllTemplatesAsync(1); // TODO: Get actual store ID
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

            StatusMessage = result.Success
                ? $"Connection successful! Response time: {result.ResponseTimeMs}ms"
                : $"Connection failed: {result.ErrorMessage}";
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

            StatusMessage = result.Success
                ? "Test label printed successfully!"
                : $"Print failed: {result.ErrorMessage}";
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

    [RelayCommand]
    private void RefreshData()
    {
        _ = LoadDataAsync();
    }
}
