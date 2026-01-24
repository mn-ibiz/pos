using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;
using HospitalityPOS.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using HospitalityPOS.WPF.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for SMS provider configuration settings.
/// </summary>
public partial class SmsSettingsViewModel : ObservableObject, INavigationAware
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;
    private readonly ILogger _logger;

    #region Observable Properties

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _busyMessage = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _successMessage;

    // Configuration fields
    [ObservableProperty]
    private string _selectedProvider = SmsProviders.AfricasTalking;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _apiKey = string.Empty;

    [ObservableProperty]
    private string _senderId = string.Empty;

    [ObservableProperty]
    private string _businessName = "HospitalityPOS";

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private bool _useSandbox = true;

    [ObservableProperty]
    private int _dailyLimit = 1000;

    // Test fields
    [ObservableProperty]
    private string _testPhoneNumber = string.Empty;

    [ObservableProperty]
    private bool _isTesting;

    [ObservableProperty]
    private string? _testResult;

    [ObservableProperty]
    private bool _testSuccessful;

    // Status info
    [ObservableProperty]
    private int _todayCount;

    [ObservableProperty]
    private DateTime? _lastTestedAt;

    [ObservableProperty]
    private bool? _lastTestSuccessful;

    [ObservableProperty]
    private string? _lastTestError;

    [ObservableProperty]
    private bool _hasExistingConfig;

    // Provider list
    public ObservableCollection<string> Providers { get; } = new(SmsProviders.All);

    #endregion

    public SmsSettingsViewModel(
        IServiceScopeFactory scopeFactory,
        IDialogService dialogService,
        INavigationService navigationService,
        ILogger logger)
    {
        _scopeFactory = scopeFactory;
        _dialogService = dialogService;
        _navigationService = navigationService;
        _logger = logger;
    }

    #region Navigation

    public void OnNavigatedTo(object? parameter)
    {
        _ = LoadConfigurationAsync();
    }

    public void OnNavigatedFrom()
    {
        // Nothing to clean up
    }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task LoadConfigurationAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            BusyMessage = "Loading SMS configuration...";
            ErrorMessage = null;

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<POSDbContext>();

            var config = await context.Set<SmsConfiguration>().FirstOrDefaultAsync();

            if (config != null)
            {
                HasExistingConfig = true;
                SelectedProvider = config.Provider;
                Username = config.Username;
                ApiKey = config.ApiKey;
                SenderId = config.SenderId;
                BusinessName = config.BusinessName;
                IsEnabled = config.IsEnabled;
                UseSandbox = config.UseSandbox;
                DailyLimit = config.DailyLimit;
                TodayCount = config.TodayCount;
                LastTestedAt = config.LastTestedAt;
                LastTestSuccessful = config.LastTestSuccessful;
                LastTestError = config.LastTestError;
            }
            else
            {
                HasExistingConfig = false;
                // Set defaults
                SelectedProvider = SmsProviders.AfricasTalking;
                Username = string.Empty;
                ApiKey = string.Empty;
                SenderId = string.Empty;
                BusinessName = "HospitalityPOS";
                IsEnabled = false;
                UseSandbox = true;
                DailyLimit = 1000;
            }

            _logger.Information("SMS configuration loaded. Configured: {HasConfig}, Enabled: {IsEnabled}",
                HasExistingConfig, IsEnabled);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load SMS configuration");
            ErrorMessage = "Failed to load SMS settings.";
        }
        finally
        {
            IsBusy = false;
            BusyMessage = string.Empty;
        }
    }

    [RelayCommand]
    private async Task SaveConfigurationAsync()
    {
        if (IsBusy) return;

        // Validate
        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "Username is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            ErrorMessage = "API Key is required.";
            return;
        }

        try
        {
            IsBusy = true;
            BusyMessage = "Saving SMS configuration...";
            ErrorMessage = null;
            SuccessMessage = null;

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<POSDbContext>();

            var config = await context.Set<SmsConfiguration>().FirstOrDefaultAsync();
            var isNew = config == null;

            if (isNew)
            {
                config = new SmsConfiguration();
                context.Set<SmsConfiguration>().Add(config);
            }

            config.Provider = SelectedProvider;
            config.Username = Username.Trim();
            config.ApiKey = ApiKey.Trim();
            config.SenderId = SenderId?.Trim() ?? string.Empty;
            config.BusinessName = BusinessName?.Trim() ?? "HospitalityPOS";
            config.IsEnabled = IsEnabled;
            config.UseSandbox = UseSandbox;
            config.DailyLimit = DailyLimit > 0 ? DailyLimit : 1000;
            config.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            HasExistingConfig = true;
            SuccessMessage = "SMS configuration saved successfully.";
            _logger.Information("SMS configuration saved. Provider: {Provider}, Enabled: {Enabled}, Sandbox: {Sandbox}",
                SelectedProvider, IsEnabled, UseSandbox);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save SMS configuration");
            ErrorMessage = "Failed to save SMS settings.";
        }
        finally
        {
            IsBusy = false;
            BusyMessage = string.Empty;
        }
    }

    [RelayCommand]
    private async Task TestSmsAsync()
    {
        if (IsTesting) return;

        // Validate test phone number
        if (string.IsNullOrWhiteSpace(TestPhoneNumber))
        {
            ErrorMessage = "Please enter a test phone number.";
            return;
        }

        // Must save configuration first
        if (!HasExistingConfig || string.IsNullOrWhiteSpace(ApiKey))
        {
            ErrorMessage = "Please save your configuration before testing.";
            return;
        }

        try
        {
            IsTesting = true;
            TestResult = null;
            ErrorMessage = null;
            SuccessMessage = null;

            using var scope = _scopeFactory.CreateScope();
            var smsService = scope.ServiceProvider.GetRequiredService<SmsService>();

            var result = await smsService.TestConfigurationAsync(TestPhoneNumber.Trim());

            TestSuccessful = result.IsSuccess;
            TestResult = result.IsSuccess
                ? $"Test SMS sent successfully! Message ID: {result.MessageId}"
                : $"Test failed: {result.ErrorMessage}";

            // Reload to get updated test status
            await LoadConfigurationAsync();

            _logger.Information("SMS test completed. Success: {Success}, Phone: {Phone}",
                result.IsSuccess, TestPhoneNumber);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to test SMS configuration");
            TestSuccessful = false;
            TestResult = $"Test error: {ex.Message}";
        }
        finally
        {
            IsTesting = false;
        }
    }

    [RelayCommand]
    private async Task ResetDailyCounterAsync()
    {
        var confirmed = await _dialogService.ShowConfirmAsync(
            "Reset Counter",
            "This will reset the daily SMS counter to 0. Continue?");

        if (!confirmed) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<POSDbContext>();

            var config = await context.Set<SmsConfiguration>().FirstOrDefaultAsync();
            if (config != null)
            {
                config.TodayCount = 0;
                config.LastResetDate = DateTime.Today;
                config.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();

                TodayCount = 0;
                SuccessMessage = "Daily counter reset successfully.";
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to reset daily counter");
            ErrorMessage = "Failed to reset counter.";
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    #endregion
}
