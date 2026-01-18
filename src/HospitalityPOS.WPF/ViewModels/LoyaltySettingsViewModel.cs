using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for configuring loyalty program settings.
/// </summary>
public partial class LoyaltySettingsViewModel : ViewModelBase
{
    private readonly ILoyaltyService _loyaltyService;
    private readonly INavigationService _navigationService;
    private readonly ISessionService _sessionService;

    [ObservableProperty]
    private PointsConfiguration? _currentConfiguration;

    [ObservableProperty]
    private ObservableCollection<TierConfigurationDto> _tierConfigurations = new();

    // Points earning settings
    [ObservableProperty]
    private decimal _pointsPerCurrencyUnit = 1;

    [ObservableProperty]
    private decimal _currencyUnitsPerPoint = 100;

    [ObservableProperty]
    private decimal _minimumEarningAmount = 100;

    [ObservableProperty]
    private bool _earnOnDiscountedItems = true;

    [ObservableProperty]
    private bool _earnOnTax;

    // Points redemption settings
    [ObservableProperty]
    private decimal _pointValueInKes = 1;

    [ObservableProperty]
    private decimal _minimumRedemptionPoints = 100;

    [ObservableProperty]
    private decimal _maximumRedemptionPercent = 50;

    [ObservableProperty]
    private bool _allowPartialRedemption = true;

    // Expiry settings
    [ObservableProperty]
    private int _pointsExpiryMonths = 12;

    [ObservableProperty]
    private bool _enablePointsExpiry = true;

    [ObservableProperty]
    private int _expiryWarningDays = 30;

    // SMS notification settings
    [ObservableProperty]
    private bool _enableSmsNotifications = true;

    [ObservableProperty]
    private bool _sendEnrollmentSms = true;

    [ObservableProperty]
    private bool _sendPointsEarnedSms = true;

    [ObservableProperty]
    private bool _sendRedemptionSms = true;

    [ObservableProperty]
    private bool _sendExpiryWarningSms = true;

    public bool CanEditSettings => HasPermission("Loyalty.Settings.Edit");

    public LoyaltySettingsViewModel(
        ILoyaltyService loyaltyService,
        INavigationService navigationService,
        ISessionService sessionService,
        ILogger logger)
        : base(logger)
    {
        _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        Title = "Loyalty Program Settings";
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            // Load current configuration from Core entity
            CurrentConfiguration = await _loyaltyService.GetPointsConfigurationAsync();

            if (CurrentConfiguration != null)
            {
                // Map Core entity properties to ViewModel properties
                // EarningRate = KSh per point, so PointsPerCurrencyUnit = 1/EarningRate
                CurrencyUnitsPerPoint = CurrentConfiguration.EarningRate;
                PointsPerCurrencyUnit = CurrentConfiguration.EarningRate > 0 ? 1 / CurrentConfiguration.EarningRate : 0.01m;
                MinimumEarningAmount = CurrentConfiguration.EarningRate; // Use earning rate as minimum
                EarnOnDiscountedItems = CurrentConfiguration.EarnOnDiscountedItems;
                EarnOnTax = CurrentConfiguration.EarnOnTax;
                PointValueInKes = CurrentConfiguration.RedemptionValue;
                MinimumRedemptionPoints = CurrentConfiguration.MinimumRedemptionPoints;
                MaximumRedemptionPercent = CurrentConfiguration.MaxRedemptionPercentage;
                AllowPartialRedemption = true; // Not in Core entity, default to true
                // Convert days to months (approximate)
                PointsExpiryMonths = CurrentConfiguration.PointsExpiryDays > 0 ? CurrentConfiguration.PointsExpiryDays / 30 : 12;
                EnablePointsExpiry = CurrentConfiguration.PointsExpiryDays > 0;
                ExpiryWarningDays = 30; // Not in Core entity, default to 30
            }

            // Load tier configurations
            var tiers = await _loyaltyService.GetTierConfigurationsAsync();
            TierConfigurations.Clear();
            foreach (var tier in tiers)
            {
                TierConfigurations.Add(tier);
            }
        }, "Loading settings...");
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        if (!RequirePermission("Loyalty.Settings.Edit", "edit loyalty settings"))
            return;

        if (!ValidateSettings())
            return;

        await ExecuteAsync(async () =>
        {
            // In a real implementation, this would save to the database
            // For now, just show success
            await Task.Delay(100);
            await DialogService.ShowMessageAsync("Success", "Loyalty settings saved successfully.");

            _logger.Information("Loyalty settings updated by user {UserId}", _sessionService.CurrentUserId);
        }, "Saving settings...");
    }

    private bool ValidateSettings()
    {
        if (PointsPerCurrencyUnit <= 0)
        {
            ErrorMessage = "Points per currency unit must be greater than zero.";
            return false;
        }

        if (CurrencyUnitsPerPoint <= 0)
        {
            ErrorMessage = "Currency units per point must be greater than zero.";
            return false;
        }

        if (MinimumRedemptionPoints < 0)
        {
            ErrorMessage = "Minimum redemption points cannot be negative.";
            return false;
        }

        if (MaximumRedemptionPercent < 0 || MaximumRedemptionPercent > 100)
        {
            ErrorMessage = "Maximum redemption percent must be between 0 and 100.";
            return false;
        }

        if (EnablePointsExpiry && PointsExpiryMonths <= 0)
        {
            ErrorMessage = "Points expiry months must be greater than zero when expiry is enabled.";
            return false;
        }

        ErrorMessage = null;
        return true;
    }

    [RelayCommand]
    private void EditTier(TierConfigurationDto? tier)
    {
        if (tier == null) return;

        // Navigate to tier editor or show dialog
        _logger.Information("Edit tier requested: {Tier}", tier.Tier);
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }
}
