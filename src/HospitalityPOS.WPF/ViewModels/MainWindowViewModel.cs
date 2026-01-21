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
/// MainWindow ViewModel that serves as the admin shell for the application.
/// Manages navigation and displays the current view with full sidebar access.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase, IDisposable
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
    /// Gets the current user's role for display.
    /// </summary>
    [ObservableProperty]
    private string _currentUserRole = "";

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
    /// Gets a value indicating whether a work period is closed.
    /// </summary>
    [ObservableProperty]
    private bool _isWorkPeriodClosed = true;

    /// <summary>
    /// Gets the application title.
    /// </summary>
    [ObservableProperty]
    private string _applicationTitle = "ProNet POS";

    /// <summary>
    /// Gets the current page title for breadcrumb display.
    /// </summary>
    [ObservableProperty]
    private string _currentPageTitle = "";

    /// <summary>
    /// Gets the register name for status bar.
    /// </summary>
    [ObservableProperty]
    private string _registerName = "REG-001";

    /// <summary>
    /// Gets a value indicating whether the sidebar should be visible.
    /// Hidden by default until user logs in with admin/manager role.
    /// </summary>
    [ObservableProperty]
    private bool _showSidebar = false;

    /// <summary>
    /// Gets a value indicating whether there are low stock alerts.
    /// </summary>
    [ObservableProperty]
    private bool _hasLowStockAlert = false;

    /// <summary>
    /// Gets the count of low stock items.
    /// </summary>
    [ObservableProperty]
    private int _lowStockCount = 0;

    // ==================== System Health Properties ====================

    /// <summary>
    /// Gets whether the database is connected.
    /// </summary>
    [ObservableProperty]
    private bool _isDatabaseConnected = true;

    /// <summary>
    /// Gets the database latency in milliseconds.
    /// </summary>
    [ObservableProperty]
    private int _databaseLatencyMs;

    /// <summary>
    /// Gets whether the printer is available.
    /// </summary>
    [ObservableProperty]
    private bool _isPrinterAvailable = true;

    /// <summary>
    /// Gets the printer status message.
    /// </summary>
    [ObservableProperty]
    private string _printerStatus = "Checking...";

    /// <summary>
    /// Gets the available disk space in GB.
    /// </summary>
    [ObservableProperty]
    private double _availableDiskSpaceGb;

    /// <summary>
    /// Gets whether disk space is low.
    /// </summary>
    [ObservableProperty]
    private bool _isDiskSpaceLow;

    /// <summary>
    /// Gets the memory usage percentage.
    /// </summary>
    [ObservableProperty]
    private double _memoryUsagePercent;

    /// <summary>
    /// Gets whether memory usage is high.
    /// </summary>
    [ObservableProperty]
    private bool _isMemoryHigh;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    public MainWindowViewModel(
        ILogger logger,
        INavigationService navigationService,
        ISessionService sessionService,
        IServiceScopeFactory scopeFactory,
        IDialogService dialogService,
        IUiShellService uiShellService)
        : base(logger)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _uiShellService = uiShellService ?? throw new ArgumentNullException(nameof(uiShellService));

        // Subscribe to navigation events
        _navigationService.Navigated += OnNavigated;

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

        // Start system health monitoring (every 30 seconds)
        _healthTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30)
        };
        _healthTimer.Tick += async (s, e) => await RefreshSystemHealthAsync();
        _healthTimer.Start();

        // Initial health check
        _ = RefreshSystemHealthAsync();

        _logger.Information("MainWindowViewModel initialized");
    }

    private readonly DispatcherTimer _healthTimer;

    #region Navigation Commands

    [RelayCommand]
    private void NavigateToDashboard() => NavigateWithSidebar<DashboardViewModel>("Dashboard");

    [RelayCommand]
    private void NavigateToPOS() => NavigateWithSidebar<POSViewModel>("Point of Sale");

    [RelayCommand]
    private void NavigateToInventory() => NavigateWithSidebar<InventoryViewModel>("Stock Levels");

    [RelayCommand]
    private void NavigateToGoodsReceiving() => NavigateWithSidebar<GoodsReceivingViewModel>("Receive Stock");

    [RelayCommand]
    private void NavigateToPurchaseOrders() => NavigateWithSidebar<PurchaseOrdersViewModel>("Purchase Orders");

    [RelayCommand]
    private void NavigateToSuppliers() => NavigateWithSidebar<SuppliersViewModel>("Suppliers");

    [RelayCommand]
    private void NavigateToSalesReports() => NavigateWithSidebar<SalesReportsViewModel>("Sales Reports");

    [RelayCommand]
    private void NavigateToInventoryReports() => NavigateWithSidebar<InventoryReportsViewModel>("Inventory Reports");

    [RelayCommand]
    private void NavigateToZReportHistory() => NavigateWithSidebar<ZReportHistoryViewModel>("Z-Report History");

    [RelayCommand]
    private void NavigateToProductManagement() => NavigateWithSidebar<ProductManagementViewModel>("Products");

    [RelayCommand]
    private void NavigateToCategoryManagement() => NavigateWithSidebar<CategoryManagementViewModel>("Categories");

    [RelayCommand]
    private void NavigateToVariantOptions() => NavigateWithSidebar<VariantOptionsViewModel>("Variant Options");

    [RelayCommand]
    private void NavigateToModifierGroups() => NavigateWithSidebar<ModifierGroupsViewModel>("Modifier Groups");

    [RelayCommand]
    private void NavigateToEmployees() => NavigateWithSidebar<EmployeesViewModel>("Employees");

    [RelayCommand]
    private void NavigateToUserManagement() => NavigateWithSidebar<UserManagementViewModel>("Users");

    [RelayCommand]
    private void NavigateToRoleManagement() => NavigateWithSidebar<RoleManagementViewModel>("Roles & Permissions");

    [RelayCommand]
    private void NavigateToPaymentMethods() => NavigateWithSidebar<PaymentMethodsViewModel>("Payment Methods");

    [RelayCommand]
    private void NavigateToOrganizationSettings() => NavigateWithSidebar<OrganizationSettingsViewModel>("Organization Settings");

    [RelayCommand]
    private void NavigateToExpenses() => NavigateWithSidebar<ExpenseDashboardViewModel>("Expense Management");

    private void NavigateWithSidebar<TViewModel>(string pageTitle) where TViewModel : class
    {
        ShowSidebar = true;
        CurrentPageTitle = pageTitle;
        _navigationService.NavigateTo<TViewModel>();
    }

    #endregion

    #region Work Period Commands

    [RelayCommand]
    private async Task OpenWorkPeriodAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var workPeriodService = scope.ServiceProvider.GetRequiredService<IWorkPeriodService>();

            // Open with 0 opening float (can be enhanced with a dialog later)
            var workPeriod = await workPeriodService.OpenWorkPeriodAsync(
                openingFloat: 0m,
                userId: _sessionService.CurrentUserId,
                notes: null);

            await RefreshWorkPeriodStatusAsync();
            _logger.Information("Work period opened by user {UserId}", _sessionService.CurrentUserId);
        }
        catch (InvalidOperationException ex)
        {
            await _dialogService.ShowErrorAsync("Cannot Open Day", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to open work period");
            await _dialogService.ShowErrorAsync("Error", "Failed to open work period. Please try again.");
        }
    }

    [RelayCommand]
    private async Task CloseWorkPeriodAsync()
    {
        var confirm = await _dialogService.ShowConfirmationAsync(
            "Close Work Day",
            "Are you sure you want to close the work day? This will finalize all transactions.");

        if (!confirm) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var workPeriodService = scope.ServiceProvider.GetRequiredService<IWorkPeriodService>();

            // Close with 0 closing cash (can be enhanced with a dialog later)
            var workPeriod = await workPeriodService.CloseWorkPeriodAsync(
                closingCash: 0m,
                userId: _sessionService.CurrentUserId,
                notes: null);

            await RefreshWorkPeriodStatusAsync();
            _logger.Information("Work period closed by user {UserId}", _sessionService.CurrentUserId);
        }
        catch (InvalidOperationException ex)
        {
            await _dialogService.ShowErrorAsync("Cannot Close Day", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to close work period");
            await _dialogService.ShowErrorAsync("Error", "Failed to close work period. Please try again.");
        }
    }

    [RelayCommand]
    private async Task GenerateXReportAsync()
    {
        await _dialogService.ShowInfoAsync("X-Report", "X-Report generation coming soon.");
    }

    #endregion

    #region User Commands

    [RelayCommand]
    private void Logout()
    {
        _sessionService.ClearSession(LogoutReason.UserInitiated);
        _uiShellService.ClearModeSelection(); // Clear mode selection for fresh start
        ShowSidebar = false;
        ModeSelectionViewModel.SelectedLoginMode = LoginMode.None;
        _navigationService.NavigateTo<ModeSelectionViewModel>();
        _navigationService.ClearHistory();
    }

    #endregion

    private void OnClockTick(object? sender, EventArgs e)
    {
        CurrentTime = DateTime.Now;
        UpdateWorkPeriodDuration();
    }

    private async void OnUserLoggedIn(object? sender, SessionEventArgs e)
    {
        var user = e.User;
        CurrentUserName = user?.FullName ?? "Unknown User";

        // Set user initials for avatar
        if (user?.FullName != null)
        {
            var names = user.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            UserInitials = names.Length >= 2
                ? $"{names[0][0]}{names[^1][0]}".ToUpper()
                : (names.Length == 1 ? names[0][..Math.Min(2, names[0].Length)].ToUpper() : "?");
        }

        // Set user role for display
        if (user?.UserRoles?.Any() == true)
        {
            CurrentUserRole = user.UserRoles.First().Role?.Name ?? "User";
        }

        // Show sidebar for admin users
        ShowSidebar = IsAdminOrManagerRole(user);

        _logger.Information("User logged in: {UserName}, Role: {Role}", CurrentUserName, CurrentUserRole);

        // Refresh work period status when user logs in
        await RefreshWorkPeriodStatusAsync();
    }

    private void OnUserLoggedOut(object? sender, SessionEventArgs e)
    {
        CurrentUserName = "Not Logged In";
        CurrentUserRole = "";
        UserInitials = "?";
        ShowSidebar = false;
        _logger.Information("User logged out. Reason: {Reason}", e.Reason);
    }

    private void OnNavigated(object? sender, NavigationEventArgs e)
    {
        CurrentView = e.ViewModel;

        // Hide sidebar for login, setup, and mode selection screens
        if (e.ViewModel is LoginViewModel or SetupWizardViewModel or ModeSelectionViewModel or CashierShellViewModel)
        {
            ShowSidebar = false;
        }
    }

    /// <summary>
    /// Determines if the user has an admin or manager role (should see full UI).
    /// </summary>
    public static bool IsAdminOrManagerRole(User? user)
    {
        if (user?.UserRoles == null) return false;

        var roleNames = user.UserRoles
            .Where(ur => ur.Role != null)
            .Select(ur => ur.Role!.Name.ToLowerInvariant())
            .ToList();

        return roleNames.Contains("administrator") ||
               roleNames.Contains("manager") ||
               roleNames.Contains("supervisor");
    }

    /// <summary>
    /// Determines if the user has a cashier-only role (should see simplified UI).
    /// </summary>
    public static bool IsCashierOnlyRole(User? user)
    {
        if (user?.UserRoles == null) return true; // Default to restricted

        var roleNames = user.UserRoles
            .Where(ur => ur.Role != null)
            .Select(ur => ur.Role!.Name.ToLowerInvariant())
            .ToList();

        // Cashier-only if they only have Cashier or Waiter roles
        return roleNames.All(r => r == "cashier" || r == "waiter");
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
            WorkPeriodStatus = "Not Started";
            IsWorkPeriodOpen = false;
            IsWorkPeriodClosed = true;
            return;
        }

        var duration = DateTime.UtcNow - _currentWorkPeriod.OpenedAt;
        var hours = (int)duration.TotalHours;
        var minutes = duration.Minutes;

        WorkPeriodStatus = $"OPEN - {hours}h {minutes:D2}m";
        IsWorkPeriodOpen = true;
        IsWorkPeriodClosed = false;
    }

    /// <summary>
    /// Refreshes the system health status.
    /// </summary>
    private async Task RefreshSystemHealthAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var healthService = scope.ServiceProvider.GetService<ISystemHealthService>();
            if (healthService == null) return;

            var health = await healthService.GetHealthStatusAsync();

            IsDatabaseConnected = health.IsDatabaseConnected;
            DatabaseLatencyMs = health.DatabaseLatencyMs;
            IsPrinterAvailable = health.IsPrinterAvailable;
            PrinterStatus = health.PrinterStatus;
            AvailableDiskSpaceGb = health.AvailableDiskSpaceGb;
            IsDiskSpaceLow = health.IsDiskSpaceLow;
            MemoryUsagePercent = health.MemoryUsagePercent;
            IsMemoryHigh = health.IsMemoryHigh;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to refresh system health");
        }
    }

    /// <summary>
    /// Releases resources used by the MainWindowViewModel.
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
                _healthTimer.Stop();

                _navigationService.Navigated -= OnNavigated;
                _sessionService.UserLoggedIn -= OnUserLoggedIn;
                _sessionService.UserLoggedOut -= OnUserLoggedOut;
            }

            _disposed = true;
        }
    }
}
