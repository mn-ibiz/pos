using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for eTIMS device configuration and management.
/// </summary>
public partial class EtimsDeviceSettingsViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private ObservableCollection<EtimsDevice> _devices = [];

    [ObservableProperty]
    private EtimsDevice? _selectedDevice;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isEditing;

    // Edit fields
    [ObservableProperty]
    private string _editDeviceSerialNumber = string.Empty;

    [ObservableProperty]
    private string _editControlUnitId = string.Empty;

    [ObservableProperty]
    private string _editBusinessPin = string.Empty;

    [ObservableProperty]
    private string _editBusinessName = string.Empty;

    [ObservableProperty]
    private string _editBranchCode = "001";

    [ObservableProperty]
    private string _editBranchName = "Main Branch";

    [ObservableProperty]
    private string _editApiBaseUrl = "https://etims.kra.go.ke";

    [ObservableProperty]
    private string _editApiKey = string.Empty;

    [ObservableProperty]
    private string _editApiSecret = string.Empty;

    [ObservableProperty]
    private string _editEnvironment = "Sandbox";

    [ObservableProperty]
    private bool _editIsPrimary = true;

    public ObservableCollection<string> Environments { get; } = ["Sandbox", "Production"];

    public EtimsDeviceSettingsViewModel(
        IServiceScopeFactory scopeFactory,
        IDialogService dialogService)
    {
        _scopeFactory = scopeFactory;
        _dialogService = dialogService;
    }

    public async Task InitializeAsync()
    {
        await LoadDevicesAsync();
    }

    [RelayCommand]
    private async Task LoadDevicesAsync()
    {
        IsLoading = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var etimsService = scope.ServiceProvider.GetRequiredService<IEtimsService>();

            var devices = await etimsService.GetAllDevicesAsync();
            Devices = new ObservableCollection<EtimsDevice>(devices);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void NewDevice()
    {
        SelectedDevice = null;
        EditDeviceSerialNumber = string.Empty;
        EditControlUnitId = string.Empty;
        EditBusinessPin = string.Empty;
        EditBusinessName = string.Empty;
        EditBranchCode = "001";
        EditBranchName = "Main Branch";
        EditApiBaseUrl = "https://etims.kra.go.ke";
        EditApiKey = string.Empty;
        EditApiSecret = string.Empty;
        EditEnvironment = "Sandbox";
        EditIsPrimary = true;
        IsEditing = true;
    }

    [RelayCommand]
    private void EditDevice()
    {
        if (SelectedDevice == null) return;

        EditDeviceSerialNumber = SelectedDevice.DeviceSerialNumber;
        EditControlUnitId = SelectedDevice.ControlUnitId;
        EditBusinessPin = SelectedDevice.BusinessPin;
        EditBusinessName = SelectedDevice.BusinessName;
        EditBranchCode = SelectedDevice.BranchCode;
        EditBranchName = SelectedDevice.BranchName;
        EditApiBaseUrl = SelectedDevice.ApiBaseUrl;
        EditApiKey = SelectedDevice.ApiKey;
        EditApiSecret = SelectedDevice.ApiSecret;
        EditEnvironment = SelectedDevice.Environment;
        EditIsPrimary = SelectedDevice.IsPrimary;
        IsEditing = true;
    }

    [RelayCommand]
    private async Task SaveDeviceAsync()
    {
        if (string.IsNullOrWhiteSpace(EditDeviceSerialNumber) ||
            string.IsNullOrWhiteSpace(EditControlUnitId) ||
            string.IsNullOrWhiteSpace(EditBusinessPin) ||
            string.IsNullOrWhiteSpace(EditBusinessName))
        {
            await _dialogService.ShowErrorAsync("Validation Error",
                "Device serial number, Control Unit ID, Business PIN, and Business Name are required.");
            return;
        }

        IsLoading = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var etimsService = scope.ServiceProvider.GetRequiredService<IEtimsService>();

            if (SelectedDevice == null)
            {
                // Create new device
                var newDevice = new EtimsDevice
                {
                    DeviceSerialNumber = EditDeviceSerialNumber,
                    ControlUnitId = EditControlUnitId,
                    BusinessPin = EditBusinessPin,
                    BusinessName = EditBusinessName,
                    BranchCode = EditBranchCode,
                    BranchName = EditBranchName,
                    ApiBaseUrl = EditApiBaseUrl,
                    ApiKey = EditApiKey,
                    ApiSecret = EditApiSecret,
                    Environment = EditEnvironment,
                    IsPrimary = EditIsPrimary
                };

                await etimsService.RegisterDeviceAsync(newDevice);
                await _dialogService.ShowMessageAsync("Success", "eTIMS device registered successfully.");
            }
            else
            {
                // Update existing device
                SelectedDevice.DeviceSerialNumber = EditDeviceSerialNumber;
                SelectedDevice.ControlUnitId = EditControlUnitId;
                SelectedDevice.BusinessPin = EditBusinessPin;
                SelectedDevice.BusinessName = EditBusinessName;
                SelectedDevice.BranchCode = EditBranchCode;
                SelectedDevice.BranchName = EditBranchName;
                SelectedDevice.ApiBaseUrl = EditApiBaseUrl;
                SelectedDevice.ApiKey = EditApiKey;
                SelectedDevice.ApiSecret = EditApiSecret;
                SelectedDevice.Environment = EditEnvironment;
                SelectedDevice.IsPrimary = EditIsPrimary;

                await etimsService.UpdateDeviceAsync(SelectedDevice);
                await _dialogService.ShowMessageAsync("Success", "Device updated successfully.");
            }

            IsEditing = false;
            await LoadDevicesAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Error", $"Failed to save device: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
    }

    [RelayCommand]
    private async Task ActivateDeviceAsync()
    {
        if (SelectedDevice == null) return;

        var result = await _dialogService.ShowConfirmationAsync(
            "Activate Device",
            $"Activate device '{SelectedDevice.DeviceSerialNumber}' as the primary eTIMS device?");

        if (!result) return;

        IsLoading = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var etimsService = scope.ServiceProvider.GetRequiredService<IEtimsService>();

            await etimsService.ActivateDeviceAsync(SelectedDevice.Id);
            await LoadDevicesAsync();

            await _dialogService.ShowMessageAsync("Success", "Device activated successfully.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Error", $"Failed to activate device: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeactivateDeviceAsync()
    {
        if (SelectedDevice == null) return;

        var result = await _dialogService.ShowConfirmationAsync(
            "Deactivate Device",
            $"Deactivate device '{SelectedDevice.DeviceSerialNumber}'? This will stop all eTIMS submissions from this device.");

        if (!result) return;

        IsLoading = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var etimsService = scope.ServiceProvider.GetRequiredService<IEtimsService>();

            await etimsService.DeactivateDeviceAsync(SelectedDevice.Id);
            await LoadDevicesAsync();

            await _dialogService.ShowMessageAsync("Success", "Device deactivated.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Error", $"Failed to deactivate device: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (SelectedDevice == null) return;

        IsLoading = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var etimsService = scope.ServiceProvider.GetRequiredService<IEtimsService>();

            var success = await etimsService.TestDeviceConnectionAsync(SelectedDevice.Id);

            if (success)
            {
                await _dialogService.ShowMessageAsync("Success", "Connection to eTIMS API successful!");
            }
            else
            {
                await _dialogService.ShowErrorAsync("Failed", "Connection test failed. Please check API credentials.");
            }

            await LoadDevicesAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Error", $"Connection test error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
