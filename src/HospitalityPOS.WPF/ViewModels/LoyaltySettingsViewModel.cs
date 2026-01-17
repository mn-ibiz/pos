using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using HospitalityPOS.Core.DTOs;
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

    public LoyaltySettingsViewModel(ILogger logger)
        : base(logger)
    {
        Title = "Loyalty Program Settings";
        _loyaltyService = App.Services.GetRequiredService<ILoyaltyService>();
        _navigationService = App.Services.GetRequiredService<INavigationService>();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            // Load current configuration
            CurrentConfiguration = await _loyaltyService.GetPointsConfigurationAsync();

            if (CurrentConfiguration != null)
            {
                // Populate form fields
                PointsPerCurrencyUnit = CurrentConfiguration.PointsPerCurrencyUnit;
                CurrencyUnitsPerPoint = CurrentConfiguration.CurrencyUnitsPerPoint;
                MinimumEarningAmount = CurrentConfiguration.MinimumEarningAmount;
                EarnOnDiscountedItems = CurrentConfiguration.EarnOnDiscountedItems;
                EarnOnTax = CurrentConfiguration.EarnOnTax;
                PointValueInKes = CurrentConfiguration.PointValueInKes;
                MinimumRedemptionPoints = CurrentConfiguration.MinimumRedemptionPoints;
                MaximumRedemptionPercent = CurrentConfiguration.MaximumRedemptionPercent;
                AllowPartialRedemption = CurrentConfiguration.AllowPartialRedemption;
                PointsExpiryMonths = CurrentConfiguration.PointsExpiryMonths;
                EnablePointsExpiry = CurrentConfiguration.EnablePointsExpiry;
                ExpiryWarningDays = CurrentConfiguration.ExpiryWarningDays;
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

            _logger.Information("Loyalty settings updated by user {UserId}", SessionService.CurrentUserId);
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
        _navigationService.NavigateBack();
    }
}

/// <summary>
/// Points configuration entity for loyalty program.
/// </summary>
public class PointsConfiguration
{
    public decimal PointsPerCurrencyUnit { get; set; } = 1;
    public decimal CurrencyUnitsPerPoint { get; set; } = 100;
    public decimal MinimumEarningAmount { get; set; } = 100;
    public bool EarnOnDiscountedItems { get; set; } = true;
    public bool EarnOnTax { get; set; }
    public decimal PointValueInKes { get; set; } = 1;
    public decimal MinimumRedemptionPoints { get; set; } = 100;
    public decimal MaximumRedemptionPercent { get; set; } = 50;
    public bool AllowPartialRedemption { get; set; } = true;
    public int PointsExpiryMonths { get; set; } = 12;
    public bool EnablePointsExpiry { get; set; } = true;
    public int ExpiryWarningDays { get; set; } = 30;
}
