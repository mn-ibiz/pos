using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for the terminal status dashboard view.
/// </summary>
public partial class TerminalStatusDashboardViewModel : ViewModelBase, INavigationAware
{
    private readonly ITerminalHealthService _healthService;
    private readonly ISessionService _sessionService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly DispatcherTimer _refreshTimer;

    private int _currentStoreId;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _autoRefreshEnabled = true;

    [ObservableProperty]
    private int _refreshIntervalSeconds = 30;

    // Summary Properties
    [ObservableProperty]
    private int _totalTerminals;

    [ObservableProperty]
    private int _onlineTerminals;

    [ObservableProperty]
    private int _offlineTerminals;

    [ObservableProperty]
    private int _inactiveTerminals;

    [ObservableProperty]
    private int _terminalsWithWarnings;

    [ObservableProperty]
    private double _healthPercentage;

    [ObservableProperty]
    private string _overallStatus = "Unknown";

    [ObservableProperty]
    private string _overallStatusColor = "#8888AA";

    [ObservableProperty]
    private DateTime? _lastCheckTime;

    // Collections
    [ObservableProperty]
    private ObservableCollection<TerminalHealthDisplayItem> _terminals = [];

    [ObservableProperty]
    private ObservableCollection<TerminalHealthDisplayItem> _offlineTerminalsList = [];

    [ObservableProperty]
    private ObservableCollection<TerminalHealthDisplayItem> _warningTerminalsList = [];

    [ObservableProperty]
    private TerminalHealthDisplayItem? _selectedTerminal;

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalStatusDashboardViewModel"/> class.
    /// </summary>
    public TerminalStatusDashboardViewModel(
        ITerminalHealthService healthService,
        ISessionService sessionService,
        INavigationService navigationService,
        IDialogService dialogService,
        ILogger logger) : base(logger)
    {
        _healthService = healthService ?? throw new ArgumentNullException(nameof(healthService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        // Setup auto-refresh timer
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(RefreshIntervalSeconds)
        };
        _refreshTimer.Tick += async (s, e) => await RefreshAsync();
    }

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
        _currentStoreId = _sessionService.CurrentStoreId ?? 1;
        _ = LoadDashboardAsync();

        if (AutoRefreshEnabled)
        {
            _refreshTimer.Start();
        }
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
        _refreshTimer.Stop();
    }

    partial void OnAutoRefreshEnabledChanged(bool value)
    {
        if (value)
        {
            _refreshTimer.Start();
        }
        else
        {
            _refreshTimer.Stop();
        }
    }

    partial void OnRefreshIntervalSecondsChanged(int value)
    {
        _refreshTimer.Interval = TimeSpan.FromSeconds(value);
    }

    /// <summary>
    /// Loads the dashboard data.
    /// </summary>
    [RelayCommand]
    private async Task LoadDashboardAsync()
    {
        try
        {
            IsLoading = true;

            // Get health summary
            var summary = await _healthService.GetStoreHealthSummaryAsync(_currentStoreId).ConfigureAwait(true);

            TotalTerminals = summary.TotalTerminals;
            OnlineTerminals = summary.OnlineTerminals;
            OfflineTerminals = summary.OfflineTerminals;
            InactiveTerminals = summary.InactiveTerminals;
            TerminalsWithWarnings = summary.TerminalsWithWarnings;
            HealthPercentage = summary.HealthPercentage;
            OverallStatus = summary.OverallStatus;
            OverallStatusColor = GetStatusColor(summary.OverallStatus);

            // Get all terminal health
            var allHealth = await _healthService.GetAllTerminalHealthAsync(_currentStoreId).ConfigureAwait(true);
            Terminals = new ObservableCollection<TerminalHealthDisplayItem>(
                allHealth.Select(h => MapToDisplayItem(h)).OrderBy(t => t.Code));

            // Get offline terminals
            OfflineTerminalsList = new ObservableCollection<TerminalHealthDisplayItem>(
                Terminals.Where(t => t.IsActive && !t.IsOnline));

            // Get terminals with warnings
            WarningTerminalsList = new ObservableCollection<TerminalHealthDisplayItem>(
                Terminals.Where(t => t.HasWarnings));

            LastCheckTime = _healthService.GetLastCheckTime();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error loading terminal status dashboard");
            await _dialogService.ShowErrorAsync("Error", "Failed to load terminal status. Please try again.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Refreshes the dashboard data.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        // Don't show loading indicator for auto-refresh
        var wasLoading = IsLoading;
        try
        {
            var summary = await _healthService.GetStoreHealthSummaryAsync(_currentStoreId).ConfigureAwait(true);

            TotalTerminals = summary.TotalTerminals;
            OnlineTerminals = summary.OnlineTerminals;
            OfflineTerminals = summary.OfflineTerminals;
            InactiveTerminals = summary.InactiveTerminals;
            TerminalsWithWarnings = summary.TerminalsWithWarnings;
            HealthPercentage = summary.HealthPercentage;
            OverallStatus = summary.OverallStatus;
            OverallStatusColor = GetStatusColor(summary.OverallStatus);

            var allHealth = await _healthService.GetAllTerminalHealthAsync(_currentStoreId).ConfigureAwait(true);
            Terminals = new ObservableCollection<TerminalHealthDisplayItem>(
                allHealth.Select(h => MapToDisplayItem(h)).OrderBy(t => t.Code));

            OfflineTerminalsList = new ObservableCollection<TerminalHealthDisplayItem>(
                Terminals.Where(t => t.IsActive && !t.IsOnline));

            WarningTerminalsList = new ObservableCollection<TerminalHealthDisplayItem>(
                Terminals.Where(t => t.HasWarnings));

            LastCheckTime = _healthService.GetLastCheckTime();
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Error during dashboard refresh");
        }
    }

    /// <summary>
    /// Manually triggers a health check.
    /// </summary>
    [RelayCommand]
    private async Task RunHealthCheckAsync()
    {
        try
        {
            IsLoading = true;
            var count = await _healthService.RunHealthCheckNowAsync(_currentStoreId).ConfigureAwait(true);
            await _dialogService.ShowInfoAsync("Health Check Complete", $"Checked {count} terminals.");
            await LoadDashboardAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error running manual health check");
            await _dialogService.ShowErrorAsync("Error", $"Health check failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Views details of the selected terminal.
    /// </summary>
    [RelayCommand]
    private async Task ViewTerminalDetailsAsync()
    {
        if (SelectedTerminal is null)
        {
            return;
        }

        var health = await _healthService.GetTerminalHealthAsync(SelectedTerminal.TerminalId).ConfigureAwait(true);

        if (health is null)
        {
            await _dialogService.ShowErrorAsync("Error", "Terminal not found.");
            return;
        }

        var details = $"""
            Terminal: {health.Code} - {health.Name}
            Type: {GetTerminalTypeName(health.TerminalType)}
            Status: {health.StatusText}

            Last Heartbeat: {(health.LastHeartbeat.HasValue ? health.LastHeartbeat.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") : "Never")}
            Time Since Heartbeat: {(health.SecondsSinceLastHeartbeat.HasValue ? $"{health.SecondsSinceLastHeartbeat} seconds" : "N/A")}

            IP Address: {health.IpAddress ?? "N/A"}
            Current User: {health.CurrentUserName ?? "None"}
            Work Period Open: {(health.IsWorkPeriodOpen ? "Yes" : "No")}
            App Version: {health.AppVersion ?? "N/A"}

            Printer Available: {(health.IsPrinterAvailable ? "Yes" : "No")}
            Cash Drawer Available: {(health.IsCashDrawerAvailable ? "Yes" : "No")}

            Warnings: {(health.Warnings.Count > 0 ? string.Join(", ", health.Warnings) : "None")}
            """;

        await _dialogService.ShowInfoAsync("Terminal Details", details);
    }

    /// <summary>
    /// Navigates to terminal management.
    /// </summary>
    [RelayCommand]
    private void ManageTerminals()
    {
        _navigationService.NavigateTo<TerminalManagementViewModel>();
    }

    /// <summary>
    /// Navigates back to the previous view.
    /// </summary>
    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    private static string GetStatusColor(string status) => status switch
    {
        "Healthy" => "#4CAF50",
        "Good" => "#8BC34A",
        "Degraded" => "#FFB347",
        "Critical" => "#FF6B6B",
        _ => "#8888AA"
    };

    private static string GetTerminalTypeName(TerminalType type) => type switch
    {
        TerminalType.Register => "Register",
        TerminalType.Till => "Till",
        TerminalType.AdminWorkstation => "Admin Workstation",
        TerminalType.KitchenDisplay => "Kitchen Display",
        TerminalType.MobileTerminal => "Mobile Terminal",
        TerminalType.SelfCheckout => "Self Checkout",
        _ => type.ToString()
    };

    private static TerminalHealthDisplayItem MapToDisplayItem(TerminalHealthStatus health)
    {
        return new TerminalHealthDisplayItem
        {
            TerminalId = health.TerminalId,
            Code = health.Code,
            Name = health.Name,
            TerminalType = health.TerminalType,
            TerminalTypeName = GetTerminalTypeName(health.TerminalType),
            Status = health.Status,
            StatusText = health.StatusText,
            IsOnline = health.IsOnline,
            IsActive = health.IsActive,
            LastHeartbeat = health.LastHeartbeat,
            SecondsSinceLastHeartbeat = health.SecondsSinceLastHeartbeat,
            IpAddress = health.IpAddress,
            CurrentUserName = health.CurrentUserName,
            IsWorkPeriodOpen = health.IsWorkPeriodOpen,
            IsPrinterAvailable = health.IsPrinterAvailable,
            IsCashDrawerAvailable = health.IsCashDrawerAvailable,
            Warnings = health.Warnings,
            HasWarnings = health.HasWarnings
        };
    }
}

/// <summary>
/// Terminal health display item for the UI.
/// </summary>
public class TerminalHealthDisplayItem
{
    public int TerminalId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public TerminalType TerminalType { get; set; }
    public string TerminalTypeName { get; set; } = string.Empty;
    public TerminalStatus Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastHeartbeat { get; set; }
    public int? SecondsSinceLastHeartbeat { get; set; }
    public string? IpAddress { get; set; }
    public string? CurrentUserName { get; set; }
    public bool IsWorkPeriodOpen { get; set; }
    public bool IsPrinterAvailable { get; set; }
    public bool IsCashDrawerAvailable { get; set; }
    public List<string> Warnings { get; set; } = [];
    public bool HasWarnings { get; set; }

    public string StatusColor => Status switch
    {
        TerminalStatus.Online => "#4CAF50",
        TerminalStatus.Offline => "#FF6B6B",
        TerminalStatus.Maintenance => "#FFB347",
        TerminalStatus.Error => "#F44336",
        _ => "#8888AA"
    };

    public string LastSeenText => LastHeartbeat.HasValue
        ? LastHeartbeat.Value.ToLocalTime().ToString("HH:mm:ss")
        : "Never";

    public string TimeSinceText => SecondsSinceLastHeartbeat.HasValue
        ? SecondsSinceLastHeartbeat.Value switch
        {
            < 60 => $"{SecondsSinceLastHeartbeat}s",
            < 3600 => $"{SecondsSinceLastHeartbeat / 60}m",
            _ => $"{SecondsSinceLastHeartbeat / 3600}h"
        }
        : "-";
}
