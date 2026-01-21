using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using HospitalityPOS.Core.Entities;
using WorkPeriodStatusEnum = HospitalityPOS.Core.Enums.WorkPeriodStatus;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the dedicated cashier shell - a streamlined, full-screen POS interface.
/// Automatically selects the appropriate POS view based on business mode:
/// - Supermarket mode: RMS-style layout with search focus
/// - Restaurant mode: Tile-based layout with categories
/// </summary>
public partial class CashierShellViewModel : ViewModelBase, IDisposable
{
    private bool _disposed;
    private readonly INavigationService _navigationService;
    private readonly ISessionService _sessionService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogService;
    private readonly IUiShellService _uiShellService;
    private readonly DispatcherTimer _clockTimer;
    private WorkPeriod? _currentWorkPeriod;

    /// <summary>
    /// Gets or sets whether the screen is locked.
    /// When locked, a PIN entry overlay is shown.
    /// </summary>
    [ObservableProperty]
    private bool _isLocked;

    /// <summary>
    /// Gets the current view (ViewModel) - typically POSViewModel.
    /// </summary>
    [ObservableProperty]
    private object? _currentView;

    /// <summary>
    /// Gets the current time for status bar display.
    /// </summary>
    [ObservableProperty]
    private DateTime _currentTime;

    /// <summary>
    /// Gets the current user's display name.
    /// </summary>
    [ObservableProperty]
    private string _currentUserName = "Cashier";

    /// <summary>
    /// Gets the current user's initials for avatar.
    /// </summary>
    [ObservableProperty]
    private string _userInitials = "?";

    /// <summary>
    /// Gets the current work period status.
    /// </summary>
    [ObservableProperty]
    private string _workPeriodStatus = "Not Started";

    /// <summary>
    /// Gets a value indicating whether a work period is currently open.
    /// </summary>
    [ObservableProperty]
    private bool _isWorkPeriodOpen;

    /// <summary>
    /// Gets the register name for status bar.
    /// </summary>
    [ObservableProperty]
    private string _registerName = "REG-001";

    /// <summary>
    /// Gets the business name for header display.
    /// </summary>
    [ObservableProperty]
    private string _businessName = "ProNet POS";

    /// <summary>
    /// PIN entry for unlocking.
    /// </summary>
    [ObservableProperty]
    private string _unlockPin = string.Empty;

    /// <summary>
    /// Error message for PIN entry.
    /// </summary>
    [ObservableProperty]
    private string? _unlockError;

    /// <summary>
    /// Gets whether the system is in Supermarket mode.
    /// </summary>
    public bool IsSupermarketMode => _uiShellService?.CurrentMode == BusinessMode.Supermarket;

    /// <summary>
    /// Gets whether the system is in Restaurant mode.
    /// </summary>
    public bool IsRestaurantMode => _uiShellService?.CurrentMode == BusinessMode.Restaurant;

    /// <summary>
    /// Gets the POSViewModel for the current view (used by both Restaurant and Supermarket views).
    /// </summary>
    [ObservableProperty]
    private POSViewModel? _posViewModel;

    public CashierShellViewModel(
        ILogger logger,
        INavigationService navigationService,
        ISessionService sessionService,
        IServiceScopeFactory scopeFactory,
        IDialogService dialogService,
        ISystemConfigurationService configService,
        IUiShellService uiShellService)
        : base(logger)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _uiShellService = uiShellService ?? throw new ArgumentNullException(nameof(uiShellService));

        // Subscribe to session events
        _sessionService.UserLoggedIn += OnUserLoggedIn;
        _sessionService.UserLoggedOut += OnUserLoggedOut;

        // Initialize the clock
        CurrentTime = DateTime.Now;
        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _clockTimer.Tick += OnClockTick;
        _clockTimer.Start();

        // Load business name from config
        _ = LoadBusinessNameAsync(configService);

        // Check if user is already logged in (event was fired before subscription)
        // This handles the case when navigating to CashierShell after login
        // IMPORTANT: Only auto-navigate to POS if we're in Supermarket or Restaurant mode
        var currentMode = ModeSelectionViewModel.SelectedLoginMode;
        if (_sessionService.CurrentUser != null &&
            (currentMode == LoginMode.Supermarket || currentMode == LoginMode.Restaurant))
        {
            var user = _sessionService.CurrentUser;
            CurrentUserName = user.DisplayName ?? user.FullName ?? "Cashier";
            if (user.FullName != null)
            {
                var names = user.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                UserInitials = names.Length >= 2
                    ? $"{names[0][0]}{names[^1][0]}".ToUpper()
                    : (names.Length == 1 ? names[0][..Math.Min(2, names[0].Length)].ToUpper() : "?");
            }
            // Navigate to POS on next dispatcher cycle to ensure UI is ready
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                NavigateToPOS();
                _ = RefreshWorkPeriodStatusAsync();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        _logger.Information("CashierShellViewModel initialized");
    }

    private async Task LoadBusinessNameAsync(ISystemConfigurationService configService)
    {
        try
        {
            var config = await configService.GetConfigurationAsync();
            if (config != null && !string.IsNullOrWhiteSpace(config.BusinessName))
            {
                BusinessName = config.BusinessName;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load business name");
        }
    }

    private void OnClockTick(object? sender, EventArgs e)
    {
        CurrentTime = DateTime.Now;
        UpdateWorkPeriodDuration();
    }

    private async void OnUserLoggedIn(object? sender, SessionEventArgs e)
    {
        // IMPORTANT: Only respond to login events if we're in Supermarket or Restaurant mode
        // Admin mode uses MainWindowViewModel and should NOT trigger CashierShell POS navigation
        var currentMode = ModeSelectionViewModel.SelectedLoginMode;
        if (currentMode != LoginMode.Supermarket && currentMode != LoginMode.Restaurant)
        {
            _logger.Debug("CashierShellViewModel ignoring login event for mode: {Mode}", currentMode);
            return;
        }

        var user = e.User;
        CurrentUserName = user?.DisplayName ?? user?.FullName ?? "Cashier";

        // Set user initials for avatar
        if (user?.FullName != null)
        {
            var names = user.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            UserInitials = names.Length >= 2
                ? $"{names[0][0]}{names[^1][0]}".ToUpper()
                : (names.Length == 1 ? names[0][..Math.Min(2, names[0].Length)].ToUpper() : "?");
        }

        IsLocked = false;
        UnlockPin = string.Empty;
        UnlockError = null;

        _logger.Information("Cashier logged in: {UserName}", CurrentUserName);

        // Navigate to POS view
        NavigateToPOS();

        // Refresh work period status
        await RefreshWorkPeriodStatusAsync();
    }

    private void OnUserLoggedOut(object? sender, SessionEventArgs e)
    {
        CurrentUserName = "Cashier";
        UserInitials = "?";
        IsLocked = false;
        _logger.Information("Cashier logged out. Reason: {Reason}", e.Reason);
    }

    /// <summary>
    /// Navigate to POS view (main cashier view).
    /// Automatically selects Restaurant or Supermarket view based on business mode.
    /// </summary>
    [RelayCommand]
    public void NavigateToPOS()
    {
        // Create the POSViewModel (shared between both view types)
        PosViewModel = App.Services.GetRequiredService<POSViewModel>();
        CurrentView = PosViewModel;

        // Notify the view about the business mode so it can show the correct UI
        OnPropertyChanged(nameof(IsSupermarketMode));
        OnPropertyChanged(nameof(IsRestaurantMode));

        // Call OnNavigatedTo to initialize the POS view properly
        // This loads work period, products, categories, etc.
        PosViewModel.OnNavigatedTo(null);

        _logger.Information("POS view loaded in {Mode} mode", IsSupermarketMode ? "Supermarket" : "Restaurant");
    }

    /// <summary>
    /// Lock the screen - requires PIN to unlock.
    /// </summary>
    [RelayCommand]
    private void LockScreen()
    {
        IsLocked = true;
        UnlockPin = string.Empty;
        UnlockError = null;
        _logger.Information("Cashier screen locked by {UserName}", CurrentUserName);
    }

    /// <summary>
    /// Attempt to unlock with PIN.
    /// </summary>
    [RelayCommand]
    private async Task UnlockWithPinAsync()
    {
        if (string.IsNullOrWhiteSpace(UnlockPin))
        {
            UnlockError = "Please enter your PIN";
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

            // Try to authenticate with PIN
            var user = await userService.AuthenticateByPinAsync(UnlockPin);

            if (user != null)
            {
                // Check if it's the same user or a different cashier
                if (user.Id == _sessionService.CurrentUserId)
                {
                    // Same user - just unlock
                    IsLocked = false;
                    UnlockPin = string.Empty;
                    UnlockError = null;
                    _logger.Information("Screen unlocked by same user {UserId}", user.Id);
                }
                else
                {
                    // Different user - switch user
                    _sessionService.SetCurrentUser(user);
                    IsLocked = false;
                    UnlockPin = string.Empty;
                    UnlockError = null;
                    _logger.Information("User switched from {OldUser} to {NewUser}",
                        _sessionService.CurrentUserId, user.Id);
                }
            }
            else
            {
                UnlockError = "Invalid PIN. Please try again.";
                UnlockPin = string.Empty;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error unlocking screen");
            UnlockError = "An error occurred. Please try again.";
            UnlockPin = string.Empty;
        }
    }

    /// <summary>
    /// Handle PIN digit entry - auto-submit when 4+ digits.
    /// </summary>
    partial void OnUnlockPinChanged(string value)
    {
        if (!string.IsNullOrEmpty(value) && value.Length >= 4)
        {
            _ = UnlockWithPinAsync();
        }
    }

    /// <summary>
    /// Logout and return to mode selection screen.
    /// </summary>
    [RelayCommand]
    private async Task LogoutAsync()
    {
        var confirm = await _dialogService.ShowConfirmationAsync(
            "Logout",
            "Are you sure you want to logout?");

        if (!confirm) return;

        _sessionService.ClearSession(LogoutReason.UserInitiated);
        _uiShellService.ClearModeSelection(); // Clear mode selection for fresh start
        ModeSelectionViewModel.SelectedLoginMode = LoginMode.None;
        _navigationService.NavigateTo<ModeSelectionViewModel>();
        _navigationService.ClearHistory();
    }

    /// <summary>
    /// Switch to admin mode (if user has permission).
    /// </summary>
    [RelayCommand]
    private async Task SwitchToAdminAsync()
    {
        // Request manager/admin PIN for override
        var confirm = await _dialogService.ShowConfirmationAsync(
            "Switch to Admin Mode",
            "This requires manager authorization. Continue?");

        if (!confirm) return;

        // For now, just logout and let them log back in as admin
        // In a full implementation, this would show a manager PIN dialog
        await LogoutAsync();
    }

    /// <summary>
    /// Refreshes the work period status from the database.
    /// </summary>
    public async Task RefreshWorkPeriodStatusAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var workPeriodService = scope.ServiceProvider.GetRequiredService<IWorkPeriodService>();
            _currentWorkPeriod = await workPeriodService.GetCurrentWorkPeriodAsync();

            IsWorkPeriodOpen = _currentWorkPeriod is not null;
            UpdateWorkPeriodDuration();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to refresh work period status");
            WorkPeriodStatus = "Error";
            IsWorkPeriodOpen = false;
        }
    }

    private void UpdateWorkPeriodDuration()
    {
        if (_currentWorkPeriod is null || _currentWorkPeriod.Status != WorkPeriodStatusEnum.Open)
        {
            WorkPeriodStatus = "Day Not Started";
            IsWorkPeriodOpen = false;
            return;
        }

        var duration = DateTime.UtcNow - _currentWorkPeriod.OpenedAt;
        var hours = (int)duration.TotalHours;
        var minutes = duration.Minutes;

        WorkPeriodStatus = $"Day Open ({hours}h {minutes:D2}m)";
        IsWorkPeriodOpen = true;
    }

    /// <summary>
    /// Releases resources used by the CashierShellViewModel.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and optionally managed resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _clockTimer.Stop();
                _sessionService.UserLoggedIn -= OnUserLoggedIn;
                _sessionService.UserLoggedOut -= OnUserLoggedOut;
            }

            _disposed = true;
        }
    }
}
