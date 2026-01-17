using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using HospitalityPOS.Core.Constants;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;
using HospitalityPOS.WPF.Views.Dialogs;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// Main ViewModel that serves as the shell for the application.
/// Manages navigation and displays the current view.
/// </summary>
public partial class MainViewModel : ViewModelBase, IDisposable
{
    private bool _disposed;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly ISessionService _sessionService;
    private readonly IAutoLogoutService _autoLogoutService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DispatcherTimer _clockTimer;

    private TimeoutWarningDialog? _warningDialog;
    private WorkPeriod? _currentWorkPeriod;

    /// <summary>
    /// Gets the current view (ViewModel) to display.
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
    private string _currentUserName = "Not Logged In";

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
    /// Gets the application title.
    /// </summary>
    [ObservableProperty]
    private string _applicationTitle = "Hospitality POS System";

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="navigationService">The navigation service.</param>
    /// <param name="dialogService">The dialog service.</param>
    /// <param name="sessionService">The session service.</param>
    /// <param name="autoLogoutService">The auto-logout service.</param>
    /// <param name="scopeFactory">The service scope factory for accessing scoped services.</param>
    public MainViewModel(
        ILogger logger,
        INavigationService navigationService,
        IDialogService dialogService,
        ISessionService sessionService,
        IAutoLogoutService autoLogoutService,
        IServiceScopeFactory scopeFactory)
        : base(logger)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _autoLogoutService = autoLogoutService ?? throw new ArgumentNullException(nameof(autoLogoutService));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

        // Subscribe to navigation events
        _navigationService.Navigated += OnNavigated;

        // Subscribe to session events
        _sessionService.UserLoggedIn += OnUserLoggedIn;
        _sessionService.UserLoggedOut += OnUserLoggedOut;

        // Subscribe to auto-logout events
        _autoLogoutService.TimeoutWarning += OnTimeoutWarning;
        _autoLogoutService.WarningCountdownTick += OnWarningCountdownTick;
        _autoLogoutService.WarningCancelled += OnWarningCancelled;

        // Initialize the clock (also updates work period duration)
        CurrentTime = DateTime.Now;
        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _clockTimer.Tick += OnClockTick;
        _clockTimer.Start();

        _logger.Information("MainViewModel initialized");
    }

    private void OnClockTick(object? sender, EventArgs e)
    {
        CurrentTime = DateTime.Now;
        UpdateWorkPeriodDuration();
    }

    private async void OnUserLoggedIn(object? sender, SessionEventArgs e)
    {
        CurrentUserName = e.User?.FullName ?? "Unknown User";
        _logger.Information("User logged in: {UserName}", CurrentUserName);

        // Refresh work period status when user logs in
        await RefreshWorkPeriodStatusAsync();
    }

    private void OnUserLoggedOut(object? sender, SessionEventArgs e)
    {
        CurrentUserName = "Not Logged In";
        CloseWarningDialog();
        _logger.Information("User logged out. Reason: {Reason}", e.Reason);
    }

    private void OnTimeoutWarning(object? sender, TimeoutWarningEventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _warningDialog = new TimeoutWarningDialog(e.SecondsRemaining)
            {
                Owner = Application.Current.MainWindow
            };

            var result = _warningDialog.ShowDialog();

            if (result == true && _warningDialog.StayLoggedIn)
            {
                _autoLogoutService.StayLoggedIn();
            }
            else
            {
                _ = _autoLogoutService.LogoutNowAsync();
            }

            _warningDialog = null;
        });
    }

    private void OnWarningCountdownTick(object? sender, int secondsRemaining)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _warningDialog?.UpdateCountdown(secondsRemaining);
        });
    }

    private void OnWarningCancelled(object? sender, EventArgs e)
    {
        CloseWarningDialog();
    }

    private void CloseWarningDialog()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (_warningDialog is not null)
            {
                _warningDialog.Close();
                _warningDialog = null;
            }
        });
    }

    private void OnNavigated(object? sender, NavigationEventArgs e)
    {
        CurrentView = e.ViewModel;
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
        if (_currentWorkPeriod is null || _currentWorkPeriod.Status != WorkPeriodStatus.Open)
        {
            WorkPeriodStatus = "Not Started";
            IsWorkPeriodOpen = false;
            return;
        }

        var duration = DateTime.UtcNow - _currentWorkPeriod.OpenedAt;
        var hours = (int)duration.TotalHours;
        var minutes = duration.Minutes;

        WorkPeriodStatus = $"OPEN - {hours}h {minutes:D2}m";
        IsWorkPeriodOpen = true;
    }

    /// <summary>
    /// Opens a new work period.
    /// </summary>
    [RelayCommand]
    private async Task OpenWorkPeriodAsync()
    {
        // Check permission
        var overrideResult = await RequirePermissionOrOverrideAsync(
            PermissionNames.WorkPeriod.Open,
            "Open Work Period");

        if (!overrideResult.IsAuthorized)
        {
            return;
        }

        try
        {
            // Get previous closing balance for carry-forward option
            decimal? previousClosingBalance = null;
            using (var scope = _scopeFactory.CreateScope())
            {
                var workPeriodService = scope.ServiceProvider.GetRequiredService<IWorkPeriodService>();
                var lastPeriod = await workPeriodService.GetLastClosedWorkPeriodAsync();
                previousClosingBalance = lastPeriod?.ClosingCash;
            }

            // Show dialog
            var openingFloat = await _dialogService.ShowOpenWorkPeriodDialogAsync(previousClosingBalance);

            if (!openingFloat.HasValue)
            {
                // User cancelled
                return;
            }

            // Open the work period
            using (var scope = _scopeFactory.CreateScope())
            {
                var workPeriodService = scope.ServiceProvider.GetRequiredService<IWorkPeriodService>();
                _currentWorkPeriod = await workPeriodService.OpenWorkPeriodAsync(
                    openingFloat.Value,
                    _sessionService.CurrentUserId);
            }

            IsWorkPeriodOpen = true;
            UpdateWorkPeriodDuration();

            await _dialogService.ShowMessageAsync(
                "Work Period Opened",
                $"Work period has been opened successfully.\n\nOpening Float: KSh {openingFloat.Value:N2}");

            _logger.Information("Work period opened with float {OpeningFloat:C}", openingFloat.Value);
        }
        catch (InvalidOperationException ex)
        {
            await _dialogService.ShowErrorAsync("Cannot Open Work Period", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to open work period");
            await _dialogService.ShowErrorAsync("Error", "An error occurred while opening the work period.");
        }
    }

    /// <summary>
    /// Generates and displays an X-Report for the current work period.
    /// </summary>
    [RelayCommand]
    private async Task GenerateXReportAsync()
    {
        if (_currentWorkPeriod is null)
        {
            await _dialogService.ShowWarningAsync(
                "No Work Period",
                "No work period is currently open. Please open a work period first.");
            return;
        }

        // Check permission
        var overrideResult = await RequirePermissionOrOverrideAsync(
            PermissionNames.Reports.XReport,
            "Generate X-Report");

        if (!overrideResult.IsAuthorized)
        {
            return;
        }

        try
        {
            SetBusy(true, "Generating X-Report...");

            using var scope = _scopeFactory.CreateScope();
            var workPeriodService = scope.ServiceProvider.GetRequiredService<IWorkPeriodService>();

            var xReport = await workPeriodService.GenerateXReportAsync(
                _currentWorkPeriod.Id,
                _sessionService.CurrentUserId,
                _sessionService.CurrentUser?.FullName ?? "Unknown");

            SetBusy(false);

            await _dialogService.ShowXReportDialogAsync(xReport);

            _logger.Information("X-Report #{ReportNumber} generated successfully", xReport.ReportNumber);
        }
        catch (Exception ex)
        {
            SetBusy(false);
            _logger.Error(ex, "Failed to generate X-Report");
            await _dialogService.ShowErrorAsync("Error", "An error occurred while generating the X-Report.");
        }
    }

    /// <summary>
    /// Closes the current work period with cash reconciliation.
    /// </summary>
    [RelayCommand]
    private async Task CloseWorkPeriodAsync()
    {
        if (_currentWorkPeriod is null)
        {
            await _dialogService.ShowWarningAsync(
                "No Work Period",
                "No work period is currently open.");
            return;
        }

        // Check permission
        var overrideResult = await RequirePermissionOrOverrideAsync(
            PermissionNames.WorkPeriod.Close,
            "Close Work Period");

        if (!overrideResult.IsAuthorized)
        {
            return;
        }

        try
        {
            SetBusy(true, "Preparing to close work period...");

            using var scope = _scopeFactory.CreateScope();
            var workPeriodService = scope.ServiceProvider.GetRequiredService<IWorkPeriodService>();

            // Get unsettled receipts
            var unsettledReceipts = await workPeriodService.GetUnsettledReceiptsAsync(_currentWorkPeriod.Id);

            // Calculate expected cash
            var expectedCash = await workPeriodService.CalculateExpectedCashAsync(_currentWorkPeriod.Id);

            // Calculate cash sales (opening float already in expected cash)
            var cashSales = expectedCash - _currentWorkPeriod.OpeningFloat;
            var cashPayouts = 0m; // Placeholder - would be calculated from CashPayout entity

            SetBusy(false);

            // Show close dialog
            var result = await _dialogService.ShowCloseWorkPeriodDialogAsync(
                expectedCash,
                _currentWorkPeriod.OpeningFloat,
                cashSales,
                cashPayouts,
                unsettledReceipts);

            if (!result.HasValue)
            {
                // User cancelled
                return;
            }

            SetBusy(true, "Closing work period...");

            // Close the work period
            var closedPeriod = await workPeriodService.CloseWorkPeriodAsync(
                result.Value.ClosingCash,
                _sessionService.CurrentUserId,
                result.Value.Notes);

            // Generate Z-Report
            SetBusy(true, "Generating Z-Report...");
            var zReport = await workPeriodService.GenerateZReportAsync(closedPeriod.Id);

            SetBusy(false);

            // Update UI state
            _currentWorkPeriod = null;
            IsWorkPeriodOpen = false;
            WorkPeriodStatus = "Not Started";

            // Show Z-Report
            await _dialogService.ShowZReportDialogAsync(zReport);

            _logger.Information(
                "Work period {WorkPeriodId} closed. Z-Report #{ZReportNumber} generated. Variance: {Variance:C}",
                closedPeriod.Id,
                closedPeriod.ZReportNumber,
                closedPeriod.Variance);
        }
        catch (InvalidOperationException ex)
        {
            SetBusy(false);
            await _dialogService.ShowErrorAsync("Cannot Close Work Period", ex.Message);
        }
        catch (Exception ex)
        {
            SetBusy(false);
            _logger.Error(ex, "Failed to close work period");
            await _dialogService.ShowErrorAsync("Error", "An error occurred while closing the work period.");
        }
    }

    /// <summary>
    /// Navigates back to the previous view.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    private bool CanGoBack() => _navigationService.CanGoBack;

    /// <summary>
    /// Shows the about dialog.
    /// </summary>
    [RelayCommand]
    private async Task ShowAboutAsync()
    {
        await _dialogService.ShowMessageAsync(
            "About",
            "Hospitality POS System\nVersion 1.0.0\n\n.NET 10 | WPF | SQL Server Express");
    }

    /// <summary>
    /// Navigates to the role management screen.
    /// </summary>
    [RelayCommand]
    private void NavigateToRoleManagement()
    {
        if (!HasPermission(PermissionNames.Users.AssignRoles))
        {
            _logger.Warning("User lacks permission to access role management");
            return;
        }

        _navigationService.NavigateTo<RoleManagementViewModel>();
    }

    /// <summary>
    /// Navigates to the user management screen.
    /// </summary>
    [RelayCommand]
    private void NavigateToUserManagement()
    {
        if (!HasPermission(PermissionNames.Users.View))
        {
            _logger.Warning("User lacks permission to access user management");
            return;
        }

        _navigationService.NavigateTo<UserManagementViewModel>();
    }

    /// <summary>
    /// Navigates to the category management screen.
    /// </summary>
    [RelayCommand]
    private void NavigateToCategoryManagement()
    {
        if (!HasPermission(PermissionNames.Products.View))
        {
            _logger.Warning("User lacks permission to access category management");
            return;
        }

        _navigationService.NavigateTo<CategoryManagementViewModel>();
    }

    /// <summary>
    /// Navigates to the product management screen.
    /// </summary>
    [RelayCommand]
    private void NavigateToProductManagement()
    {
        if (!HasPermission(PermissionNames.Products.View))
        {
            _logger.Warning("User lacks permission to access product management");
            return;
        }

        _navigationService.NavigateTo<ProductManagementViewModel>();
    }

    /// <summary>
    /// Navigates to the inventory management screen.
    /// </summary>
    [RelayCommand]
    private void NavigateToInventory()
    {
        if (!HasPermission(PermissionNames.Products.View))
        {
            _logger.Warning("User lacks permission to access inventory management");
            return;
        }

        _navigationService.NavigateTo<InventoryViewModel>();
    }

    /// <summary>
    /// Navigates to the POS screen.
    /// </summary>
    [RelayCommand]
    private async Task NavigateToPOSAsync()
    {
        // Check if work period is open
        if (!IsWorkPeriodOpen)
        {
            await _dialogService.ShowWarningAsync(
                "Work Period Required",
                "Please start a work period before accessing the POS screen.");
            return;
        }

        // Check permission for order creation
        if (!HasPermission(PermissionNames.Orders.Create))
        {
            _logger.Warning("User lacks permission to create orders");
            await _dialogService.ShowErrorAsync("Access Denied", "You do not have permission to create orders.");
            return;
        }

        _navigationService.NavigateTo<POSViewModel>();
    }

    /// <summary>
    /// Confirms and exits the application.
    /// </summary>
    [RelayCommand]
    private async Task ExitApplicationAsync()
    {
        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Exit Application",
            "Are you sure you want to exit the application?");

        if (confirmed)
        {
            _clockTimer.Stop();
            System.Windows.Application.Current.Shutdown();
        }
    }

    /// <summary>
    /// Updates the current user display.
    /// </summary>
    /// <param name="displayName">The user's display name.</param>
    public void SetCurrentUser(string? displayName)
    {
        CurrentUserName = string.IsNullOrEmpty(displayName) ? "Not Logged In" : displayName;
    }

    /// <summary>
    /// Updates the work period status display.
    /// </summary>
    /// <param name="status">The work period status text.</param>
    public void SetWorkPeriodStatus(string? status)
    {
        WorkPeriodStatus = string.IsNullOrEmpty(status) ? "Not Started" : status;
    }

    /// <summary>
    /// Sets the busy state with an optional message.
    /// </summary>
    /// <param name="isBusy">Whether the ViewModel is busy.</param>
    /// <param name="message">Optional message to display.</param>
    private void SetBusy(bool isBusy, string message = "")
    {
        IsBusy = isBusy;
        BusyMessage = message;
    }

    /// <summary>
    /// Releases resources used by the MainViewModel.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and optionally managed resources.
    /// </summary>
    /// <param name="disposing">True to release both managed and unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Stop and dispose the timer
                _clockTimer.Stop();

                // Close warning dialog if open
                CloseWarningDialog();

                // Unsubscribe from navigation events
                _navigationService.Navigated -= OnNavigated;

                // Unsubscribe from session events
                _sessionService.UserLoggedIn -= OnUserLoggedIn;
                _sessionService.UserLoggedOut -= OnUserLoggedOut;

                // Unsubscribe from auto-logout events
                _autoLogoutService.TimeoutWarning -= OnTimeoutWarning;
                _autoLogoutService.WarningCountdownTick -= OnWarningCountdownTick;
                _autoLogoutService.WarningCancelled -= OnWarningCancelled;
            }

            _disposed = true;
        }
    }
}
