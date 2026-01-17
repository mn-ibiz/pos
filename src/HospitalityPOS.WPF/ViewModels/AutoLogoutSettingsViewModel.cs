using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for configuring auto-logout settings.
/// </summary>
public partial class AutoLogoutSettingsViewModel : ViewModelBase, INavigationAware
{
    private readonly IAutoLogoutService _autoLogoutService;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;

    #region Observable Properties

    /// <summary>
    /// Gets or sets whether auto-logout is enabled.
    /// </summary>
    [ObservableProperty]
    private bool _enableAutoLogout;

    /// <summary>
    /// Gets or sets whether to logout after each transaction.
    /// </summary>
    [ObservableProperty]
    private bool _logoutAfterTransaction;

    /// <summary>
    /// Gets or sets whether to logout after inactivity timeout.
    /// </summary>
    [ObservableProperty]
    private bool _logoutAfterInactivity;

    /// <summary>
    /// Gets or sets the inactivity timeout in minutes.
    /// </summary>
    [ObservableProperty]
    private int _inactivityTimeoutMinutes;

    /// <summary>
    /// Gets or sets the warning time before logout in seconds.
    /// </summary>
    [ObservableProperty]
    private int _warningBeforeLogoutSeconds;

    /// <summary>
    /// Gets or sets whether to show a timeout warning.
    /// </summary>
    [ObservableProperty]
    private bool _showTimeoutWarning;

    /// <summary>
    /// Gets or sets whether to allow staying logged in.
    /// </summary>
    [ObservableProperty]
    private bool _allowStayLoggedIn;

    /// <summary>
    /// Gets or sets whether waiters can only view their own tickets.
    /// </summary>
    [ObservableProperty]
    private bool _enforceOwnTicketsOnly;

    /// <summary>
    /// Gets or sets whether PIN is required for void/discount.
    /// </summary>
    [ObservableProperty]
    private bool _requirePinForVoidDiscount;

    /// <summary>
    /// Gets the available timeout options.
    /// </summary>
    public int[] AvailableTimeoutMinutes => AutoLogoutSettings.AvailableTimeoutMinutes;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoLogoutSettingsViewModel"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="autoLogoutService">The auto-logout service.</param>
    /// <param name="dialogService">The dialog service.</param>
    /// <param name="navigationService">The navigation service.</param>
    public AutoLogoutSettingsViewModel(
        ILogger logger,
        IAutoLogoutService autoLogoutService,
        IDialogService dialogService,
        INavigationService navigationService)
        : base(logger)
    {
        _autoLogoutService = autoLogoutService ?? throw new ArgumentNullException(nameof(autoLogoutService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

        Title = "Auto-Logout Settings";
    }

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
        LoadSettings();
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
        // No action needed
    }

    /// <summary>
    /// Loads the current settings.
    /// </summary>
    private void LoadSettings()
    {
        var settings = _autoLogoutService.Settings;

        EnableAutoLogout = settings.EnableAutoLogout;
        LogoutAfterTransaction = settings.LogoutAfterTransaction;
        LogoutAfterInactivity = settings.LogoutAfterInactivity;
        InactivityTimeoutMinutes = settings.InactivityTimeoutMinutes;
        WarningBeforeLogoutSeconds = settings.WarningBeforeLogoutSeconds;
        ShowTimeoutWarning = settings.ShowTimeoutWarning;
        AllowStayLoggedIn = settings.AllowStayLoggedIn;
        EnforceOwnTicketsOnly = settings.EnforceOwnTicketsOnly;
        RequirePinForVoidDiscount = settings.RequirePinForVoidDiscount;
    }

    /// <summary>
    /// Saves the settings.
    /// </summary>
    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        await ExecuteAsync(async () =>
        {
            // Validate settings
            if (!AutoLogoutSettings.AvailableTimeoutMinutes.Contains(InactivityTimeoutMinutes))
            {
                await _dialogService.ShowErrorAsync(
                    "Invalid Setting",
                    $"Inactivity timeout must be one of: {string.Join(", ", AutoLogoutSettings.AvailableTimeoutMinutes)} minutes.");
                return;
            }

            if (WarningBeforeLogoutSeconds < 10 || WarningBeforeLogoutSeconds > 120)
            {
                await _dialogService.ShowErrorAsync(
                    "Invalid Setting",
                    "Warning time must be between 10 and 120 seconds.");
                return;
            }

            var settings = new AutoLogoutSettings
            {
                EnableAutoLogout = EnableAutoLogout,
                LogoutAfterTransaction = LogoutAfterTransaction,
                LogoutAfterInactivity = LogoutAfterInactivity,
                InactivityTimeoutMinutes = InactivityTimeoutMinutes,
                WarningBeforeLogoutSeconds = WarningBeforeLogoutSeconds,
                ShowTimeoutWarning = ShowTimeoutWarning,
                AllowStayLoggedIn = AllowStayLoggedIn,
                EnforceOwnTicketsOnly = EnforceOwnTicketsOnly,
                RequirePinForVoidDiscount = RequirePinForVoidDiscount
            };

            await _autoLogoutService.UpdateSettingsAsync(settings);

            await _dialogService.ShowMessageAsync(
                "Settings Saved",
                "Auto-logout settings have been saved successfully.");

            _navigationService.GoBack();
        }, "Saving settings...").ConfigureAwait(true);
    }

    /// <summary>
    /// Cancels and goes back.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        _navigationService.GoBack();
    }

    /// <summary>
    /// Resets to default settings.
    /// </summary>
    [RelayCommand]
    private async Task ResetToDefaultsAsync()
    {
        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Reset Settings",
            "Are you sure you want to reset to default settings?");

        if (confirmed)
        {
            var defaults = new AutoLogoutSettings();

            EnableAutoLogout = defaults.EnableAutoLogout;
            LogoutAfterTransaction = defaults.LogoutAfterTransaction;
            LogoutAfterInactivity = defaults.LogoutAfterInactivity;
            InactivityTimeoutMinutes = defaults.InactivityTimeoutMinutes;
            WarningBeforeLogoutSeconds = defaults.WarningBeforeLogoutSeconds;
            ShowTimeoutWarning = defaults.ShowTimeoutWarning;
            AllowStayLoggedIn = defaults.AllowStayLoggedIn;
            EnforceOwnTicketsOnly = defaults.EnforceOwnTicketsOnly;
            RequirePinForVoidDiscount = defaults.RequirePinForVoidDiscount;
        }
    }
}
