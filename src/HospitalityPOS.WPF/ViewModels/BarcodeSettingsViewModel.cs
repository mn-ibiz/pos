using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for barcode and scale settings.
/// </summary>
public partial class BarcodeSettingsViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _successMessage;

    // Weighted Barcode Configs
    [ObservableProperty]
    private ObservableCollection<WeightedBarcodeConfig> _weightedConfigs = [];

    [ObservableProperty]
    private WeightedBarcodeConfig? _selectedWeightedConfig;

    // Scale Configurations
    [ObservableProperty]
    private ObservableCollection<ScaleConfiguration> _scaleConfigs = [];

    [ObservableProperty]
    private ScaleConfiguration? _selectedScaleConfig;

    // Internal Barcode Sequence
    [ObservableProperty]
    private InternalBarcodeSequence? _barcodeSequence;

    // Weighted Config Edit
    [ObservableProperty]
    private bool _isEditingWeightedConfig;

    [ObservableProperty]
    private int _editWeightedId;

    [ObservableProperty]
    private string _editWeightedName = string.Empty;

    [ObservableProperty]
    private WeightedBarcodePrefix _editWeightedPrefix = WeightedBarcodePrefix.Prefix20;

    [ObservableProperty]
    private WeightedBarcodeFormat _editWeightedFormat = WeightedBarcodeFormat.StandardPrice;

    [ObservableProperty]
    private int _editArticleCodeStart = 2;

    [ObservableProperty]
    private int _editArticleCodeLength = 5;

    [ObservableProperty]
    private int _editValueStart = 7;

    [ObservableProperty]
    private int _editValueLength = 5;

    [ObservableProperty]
    private int _editValueDecimals = 2;

    [ObservableProperty]
    private bool _editIsPrice = true;

    [ObservableProperty]
    private bool _editWeightedIsActive = true;

    // Scale Config Edit
    [ObservableProperty]
    private bool _isEditingScaleConfig;

    [ObservableProperty]
    private int _editScaleId;

    [ObservableProperty]
    private string _editScaleName = string.Empty;

    [ObservableProperty]
    private ScaleType _editScaleType = ScaleType.POSIntegrated;

    [ObservableProperty]
    private ScaleProtocol _editScaleProtocol = ScaleProtocol.Serial;

    [ObservableProperty]
    private string _editConnectionString = string.Empty;

    [ObservableProperty]
    private int _editBaudRate = 9600;

    [ObservableProperty]
    private WeightUnit _editWeightUnit = WeightUnit.Kilograms;

    [ObservableProperty]
    private decimal _editMinWeight = 0.005m;

    [ObservableProperty]
    private decimal _editMaxWeight = 30.0m;

    [ObservableProperty]
    private bool _editScaleIsActive;

    [ObservableProperty]
    private bool _isTestingScale;

    [ObservableProperty]
    private string? _scaleTestResult;

    // Enums for ComboBoxes
    public Array PrefixOptions => Enum.GetValues(typeof(WeightedBarcodePrefix));
    public Array FormatOptions => Enum.GetValues(typeof(WeightedBarcodeFormat));
    public Array ScaleTypeOptions => Enum.GetValues(typeof(ScaleType));
    public Array ProtocolOptions => Enum.GetValues(typeof(ScaleProtocol));
    public Array WeightUnitOptions => Enum.GetValues(typeof(WeightUnit));

    public BarcodeSettingsViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var barcodeService = scope.ServiceProvider.GetRequiredService<IBarcodeService>();

            var weightedConfigs = await barcodeService.GetWeightedBarcodeConfigsAsync();
            WeightedConfigs = new ObservableCollection<WeightedBarcodeConfig>(weightedConfigs);

            var scaleConfigs = await barcodeService.GetAllScaleConfigurationsAsync();
            ScaleConfigs = new ObservableCollection<ScaleConfiguration>(scaleConfigs);

            BarcodeSequence = await barcodeService.GetBarcodeSequenceAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load settings: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    #region Weighted Config Commands

    [RelayCommand]
    private void NewWeightedConfig()
    {
        EditWeightedId = 0;
        EditWeightedName = "New Configuration";
        EditWeightedPrefix = WeightedBarcodePrefix.Prefix20;
        EditWeightedFormat = WeightedBarcodeFormat.StandardPrice;
        EditArticleCodeStart = 2;
        EditArticleCodeLength = 5;
        EditValueStart = 7;
        EditValueLength = 5;
        EditValueDecimals = 2;
        EditIsPrice = true;
        EditWeightedIsActive = true;

        IsEditingWeightedConfig = true;
    }

    [RelayCommand]
    private void EditWeightedConfig()
    {
        if (SelectedWeightedConfig == null) return;

        EditWeightedId = SelectedWeightedConfig.Id;
        EditWeightedName = SelectedWeightedConfig.Name;
        EditWeightedPrefix = SelectedWeightedConfig.Prefix;
        EditWeightedFormat = SelectedWeightedConfig.Format;
        EditArticleCodeStart = SelectedWeightedConfig.ArticleCodeStart;
        EditArticleCodeLength = SelectedWeightedConfig.ArticleCodeLength;
        EditValueStart = SelectedWeightedConfig.ValueStart;
        EditValueLength = SelectedWeightedConfig.ValueLength;
        EditValueDecimals = SelectedWeightedConfig.ValueDecimals;
        EditIsPrice = SelectedWeightedConfig.IsPrice;
        EditWeightedIsActive = SelectedWeightedConfig.IsActive;

        IsEditingWeightedConfig = true;
    }

    [RelayCommand]
    private void CancelWeightedEdit()
    {
        IsEditingWeightedConfig = false;
    }

    [RelayCommand]
    private async Task SaveWeightedConfigAsync()
    {
        if (string.IsNullOrWhiteSpace(EditWeightedName))
        {
            ErrorMessage = "Please enter a name.";
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var barcodeService = scope.ServiceProvider.GetRequiredService<IBarcodeService>();

            var config = new WeightedBarcodeConfig
            {
                Id = EditWeightedId,
                Name = EditWeightedName,
                Prefix = EditWeightedPrefix,
                Format = EditWeightedFormat,
                ArticleCodeStart = EditArticleCodeStart,
                ArticleCodeLength = EditArticleCodeLength,
                ValueStart = EditValueStart,
                ValueLength = EditValueLength,
                ValueDecimals = EditValueDecimals,
                IsPrice = EditIsPrice,
                IsActive = EditWeightedIsActive
            };

            await barcodeService.SaveWeightedBarcodeConfigAsync(config);

            IsEditingWeightedConfig = false;
            SuccessMessage = "Configuration saved successfully!";
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save: {ex.Message}";
        }
    }

    #endregion

    #region Scale Config Commands

    [RelayCommand]
    private void NewScaleConfig()
    {
        EditScaleId = 0;
        EditScaleName = "New Scale";
        EditScaleType = ScaleType.POSIntegrated;
        EditScaleProtocol = ScaleProtocol.Serial;
        EditConnectionString = "COM1";
        EditBaudRate = 9600;
        EditWeightUnit = WeightUnit.Kilograms;
        EditMinWeight = 0.005m;
        EditMaxWeight = 30.0m;
        EditScaleIsActive = false;

        IsEditingScaleConfig = true;
        ScaleTestResult = null;
    }

    [RelayCommand]
    private void EditScaleConfig()
    {
        if (SelectedScaleConfig == null) return;

        EditScaleId = SelectedScaleConfig.Id;
        EditScaleName = SelectedScaleConfig.Name;
        EditScaleType = SelectedScaleConfig.ScaleType;
        EditScaleProtocol = SelectedScaleConfig.Protocol;
        EditConnectionString = SelectedScaleConfig.ConnectionString;
        EditBaudRate = SelectedScaleConfig.BaudRate ?? 9600;
        EditWeightUnit = SelectedScaleConfig.WeightUnit;
        EditMinWeight = SelectedScaleConfig.MinWeight;
        EditMaxWeight = SelectedScaleConfig.MaxWeight;
        EditScaleIsActive = SelectedScaleConfig.IsActive;

        IsEditingScaleConfig = true;
        ScaleTestResult = null;
    }

    [RelayCommand]
    private void CancelScaleEdit()
    {
        IsEditingScaleConfig = false;
        ScaleTestResult = null;
    }

    [RelayCommand]
    private async Task SaveScaleConfigAsync()
    {
        if (string.IsNullOrWhiteSpace(EditScaleName))
        {
            ErrorMessage = "Please enter a scale name.";
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var barcodeService = scope.ServiceProvider.GetRequiredService<IBarcodeService>();

            var config = new ScaleConfiguration
            {
                Id = EditScaleId,
                Name = EditScaleName,
                ScaleType = EditScaleType,
                Protocol = EditScaleProtocol,
                ConnectionString = EditConnectionString,
                BaudRate = EditBaudRate,
                WeightUnit = EditWeightUnit,
                MinWeight = EditMinWeight,
                MaxWeight = EditMaxWeight,
                IsActive = EditScaleIsActive
            };

            await barcodeService.SaveScaleConfigurationAsync(config);

            IsEditingScaleConfig = false;
            SuccessMessage = "Scale configuration saved successfully!";
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ActivateScaleAsync()
    {
        if (SelectedScaleConfig == null) return;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var barcodeService = scope.ServiceProvider.GetRequiredService<IBarcodeService>();

            await barcodeService.ActivateScaleAsync(SelectedScaleConfig.Id);
            SuccessMessage = "Scale activated successfully!";
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to activate: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task TestScaleConnectionAsync()
    {
        if (EditScaleId == 0)
        {
            ScaleTestResult = "Please save the configuration first.";
            return;
        }

        IsTestingScale = true;
        ScaleTestResult = null;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var barcodeService = scope.ServiceProvider.GetRequiredService<IBarcodeService>();

            var success = await barcodeService.TestScaleConnectionAsync(EditScaleId);
            ScaleTestResult = success
                ? "Connection test successful!"
                : "Connection test failed.";
        }
        catch (Exception ex)
        {
            ScaleTestResult = $"Error: {ex.Message}";
        }
        finally
        {
            IsTestingScale = false;
        }
    }

    #endregion

    #region Barcode Sequence

    [RelayCommand]
    private async Task UpdateBarcodeSequenceAsync()
    {
        if (BarcodeSequence == null) return;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var barcodeService = scope.ServiceProvider.GetRequiredService<IBarcodeService>();

            await barcodeService.UpdateBarcodeSequenceAsync(BarcodeSequence);
            SuccessMessage = "Barcode sequence updated successfully!";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to update: {ex.Message}";
        }
    }

    #endregion

    #region Barcode Testing

    [ObservableProperty]
    private string _testBarcodeInput = string.Empty;

    [ObservableProperty]
    private string? _testBarcodeResult;

    [RelayCommand]
    private async Task TestBarcodeLookupAsync()
    {
        if (string.IsNullOrWhiteSpace(TestBarcodeInput))
        {
            TestBarcodeResult = "Please enter a barcode to test.";
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var barcodeService = scope.ServiceProvider.GetRequiredService<IBarcodeService>();

            var result = await barcodeService.LookupBarcodeAsync(TestBarcodeInput);

            if (result.Found)
            {
                TestBarcodeResult = $"Found: {result.Product?.Name ?? "Unknown"}\n" +
                                   $"Type: {result.DetectedType}\n" +
                                   $"Quantity: {result.Quantity}\n" +
                                   $"Weighted: {result.IsWeighted}\n" +
                                   (result.EmbeddedPrice.HasValue ? $"Price: {result.EmbeddedPrice:C}\n" : "") +
                                   (result.Weight.HasValue ? $"Weight: {result.Weight:F3}kg\n" : "");
            }
            else
            {
                TestBarcodeResult = result.ErrorMessage ?? "Product not found.";
            }
        }
        catch (Exception ex)
        {
            TestBarcodeResult = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task TestWeightedBarcodeAsync()
    {
        if (string.IsNullOrWhiteSpace(TestBarcodeInput))
        {
            TestBarcodeResult = "Please enter a weighted barcode to test.";
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var barcodeService = scope.ServiceProvider.GetRequiredService<IBarcodeService>();

            var result = await barcodeService.ParseWeightedBarcodeAsync(TestBarcodeInput);

            if (result.Success)
            {
                TestBarcodeResult = $"Prefix: {result.Prefix}\n" +
                                   $"Article Code: {result.ArticleCode}\n" +
                                   $"Value: {result.Value:N4}\n" +
                                   $"Is Price: {result.IsPrice}";
            }
            else
            {
                TestBarcodeResult = result.ErrorMessage ?? "Failed to parse barcode.";
            }
        }
        catch (Exception ex)
        {
            TestBarcodeResult = $"Error: {ex.Message}";
        }
    }

    #endregion
}
