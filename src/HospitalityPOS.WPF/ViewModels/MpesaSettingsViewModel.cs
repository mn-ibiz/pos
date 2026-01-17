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
/// ViewModel for M-Pesa configuration settings.
/// </summary>
public partial class MpesaSettingsViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _successMessage;

    // Configurations List
    [ObservableProperty]
    private ObservableCollection<MpesaConfiguration> _configurations = [];

    [ObservableProperty]
    private MpesaConfiguration? _selectedConfiguration;

    // Edit Form
    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private int _editId;

    [ObservableProperty]
    private string _editName = string.Empty;

    [ObservableProperty]
    private MpesaEnvironment _editEnvironment = MpesaEnvironment.Sandbox;

    [ObservableProperty]
    private string _editConsumerKey = string.Empty;

    [ObservableProperty]
    private string _editConsumerSecret = string.Empty;

    [ObservableProperty]
    private string _editBusinessShortCode = string.Empty;

    [ObservableProperty]
    private string _editPasskey = string.Empty;

    [ObservableProperty]
    private MpesaTransactionType _editTransactionType = MpesaTransactionType.CustomerBuyGoodsOnline;

    [ObservableProperty]
    private string _editCallbackUrl = string.Empty;

    [ObservableProperty]
    private string _editAccountReferencePrefix = "POS";

    [ObservableProperty]
    private string _editDefaultDescription = "Payment for goods";

    [ObservableProperty]
    private bool _isTestingConfiguration;

    [ObservableProperty]
    private string? _testResultMessage;

    public Array EnvironmentOptions => Enum.GetValues(typeof(MpesaEnvironment));
    public Array TransactionTypeOptions => Enum.GetValues(typeof(MpesaTransactionType));

    public MpesaSettingsViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [RelayCommand]
    private async Task LoadConfigurationsAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mpesaService = scope.ServiceProvider.GetRequiredService<IMpesaService>();

            var configs = await mpesaService.GetAllConfigurationsAsync();
            Configurations = new ObservableCollection<MpesaConfiguration>(configs);

            // Select active configuration if any
            SelectedConfiguration = Configurations.FirstOrDefault(c => c.IsActive);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load configurations: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void NewConfiguration()
    {
        EditId = 0;
        EditName = "New Configuration";
        EditEnvironment = MpesaEnvironment.Sandbox;
        EditConsumerKey = string.Empty;
        EditConsumerSecret = string.Empty;
        EditBusinessShortCode = string.Empty;
        EditPasskey = string.Empty;
        EditTransactionType = MpesaTransactionType.CustomerBuyGoodsOnline;
        EditCallbackUrl = string.Empty;
        EditAccountReferencePrefix = "POS";
        EditDefaultDescription = "Payment for goods";

        IsEditing = true;
        SuccessMessage = null;
        ErrorMessage = null;
    }

    [RelayCommand]
    private void EditConfiguration()
    {
        if (SelectedConfiguration == null) return;

        EditId = SelectedConfiguration.Id;
        EditName = SelectedConfiguration.Name;
        EditEnvironment = SelectedConfiguration.Environment;
        EditConsumerKey = SelectedConfiguration.ConsumerKey;
        EditConsumerSecret = SelectedConfiguration.ConsumerSecret;
        EditBusinessShortCode = SelectedConfiguration.BusinessShortCode;
        EditPasskey = SelectedConfiguration.Passkey;
        EditTransactionType = SelectedConfiguration.TransactionType;
        EditCallbackUrl = SelectedConfiguration.CallbackUrl;
        EditAccountReferencePrefix = SelectedConfiguration.AccountReferencePrefix;
        EditDefaultDescription = SelectedConfiguration.DefaultDescription;

        IsEditing = true;
        SuccessMessage = null;
        ErrorMessage = null;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        TestResultMessage = null;
    }

    [RelayCommand]
    private async Task SaveConfigurationAsync()
    {
        if (string.IsNullOrWhiteSpace(EditName) ||
            string.IsNullOrWhiteSpace(EditConsumerKey) ||
            string.IsNullOrWhiteSpace(EditConsumerSecret) ||
            string.IsNullOrWhiteSpace(EditBusinessShortCode) ||
            string.IsNullOrWhiteSpace(EditPasskey))
        {
            ErrorMessage = "Please fill in all required fields.";
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mpesaService = scope.ServiceProvider.GetRequiredService<IMpesaService>();

            var config = new MpesaConfiguration
            {
                Id = EditId,
                Name = EditName,
                Environment = EditEnvironment,
                ConsumerKey = EditConsumerKey,
                ConsumerSecret = EditConsumerSecret,
                BusinessShortCode = EditBusinessShortCode,
                Passkey = EditPasskey,
                TransactionType = EditTransactionType,
                CallbackUrl = EditCallbackUrl,
                ApiBaseUrl = EditEnvironment == MpesaEnvironment.Sandbox
                    ? "https://sandbox.safaricom.co.ke"
                    : "https://api.safaricom.co.ke",
                AccountReferencePrefix = EditAccountReferencePrefix,
                DefaultDescription = EditDefaultDescription
            };

            await mpesaService.SaveConfigurationAsync(config);

            IsEditing = false;
            SuccessMessage = "Configuration saved successfully!";
            await LoadConfigurationsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ActivateConfigurationAsync()
    {
        if (SelectedConfiguration == null) return;

        var result = MessageBox.Show(
            $"Activate configuration '{SelectedConfiguration.Name}'?\n\nThis will deactivate all other configurations.",
            "Confirm Activation",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mpesaService = scope.ServiceProvider.GetRequiredService<IMpesaService>();

            var success = await mpesaService.ActivateConfigurationAsync(SelectedConfiguration.Id);

            if (success)
            {
                SuccessMessage = "Configuration activated successfully!";
                await LoadConfigurationsAsync();
            }
            else
            {
                ErrorMessage = "Failed to activate configuration.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task TestConfigurationAsync()
    {
        if (EditId == 0)
        {
            TestResultMessage = "Please save the configuration first before testing.";
            return;
        }

        IsTestingConfiguration = true;
        TestResultMessage = null;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mpesaService = scope.ServiceProvider.GetRequiredService<IMpesaService>();

            var success = await mpesaService.TestConfigurationAsync(EditId);

            TestResultMessage = success
                ? "Configuration test successful! API credentials are valid."
                : "Configuration test failed. Please check your credentials.";
        }
        catch (Exception ex)
        {
            TestResultMessage = $"Test error: {ex.Message}";
        }
        finally
        {
            IsTestingConfiguration = false;
        }
    }

    [RelayCommand]
    private async Task TestSelectedConfigurationAsync()
    {
        if (SelectedConfiguration == null) return;

        IsTestingConfiguration = true;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mpesaService = scope.ServiceProvider.GetRequiredService<IMpesaService>();

            var success = await mpesaService.TestConfigurationAsync(SelectedConfiguration.Id);

            MessageBox.Show(
                success
                    ? "Configuration test successful! API credentials are valid."
                    : "Configuration test failed. Please check your credentials.",
                "Test Result",
                MessageBoxButton.OK,
                success ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Test error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsTestingConfiguration = false;
        }
    }

    partial void OnEditEnvironmentChanged(MpesaEnvironment value)
    {
        // Auto-populate sample values for sandbox
        if (value == MpesaEnvironment.Sandbox && EditId == 0)
        {
            if (string.IsNullOrWhiteSpace(EditCallbackUrl))
            {
                EditCallbackUrl = "https://your-domain.com/api/mpesa/callback";
            }
        }
    }

    partial void OnSelectedConfigurationChanged(MpesaConfiguration? value)
    {
        SuccessMessage = null;
        ErrorMessage = null;
    }
}
