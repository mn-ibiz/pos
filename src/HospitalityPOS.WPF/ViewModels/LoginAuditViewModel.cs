using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for viewing login audit history.
/// </summary>
public partial class LoginAuditViewModel : ViewModelBase, INavigationAware
{
    private readonly ILoginAuditService _loginAuditService;
    private readonly IUserService _userService;
    private readonly INavigationService _navigationService;

    #region Observable Properties

    /// <summary>
    /// Gets or sets the login audit records.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<LoginAuditDisplayItem> _auditRecords = [];

    /// <summary>
    /// Gets or sets the available users for filtering.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<UserFilterItem> _users = [];

    /// <summary>
    /// Gets or sets the selected user filter.
    /// </summary>
    [ObservableProperty]
    private UserFilterItem? _selectedUser;

    /// <summary>
    /// Gets or sets the from date filter.
    /// </summary>
    [ObservableProperty]
    private DateTime _fromDate = DateTime.Today.AddDays(-7);

    /// <summary>
    /// Gets or sets the to date filter.
    /// </summary>
    [ObservableProperty]
    private DateTime _toDate = DateTime.Today;

    /// <summary>
    /// Gets or sets the success filter (null for all).
    /// </summary>
    [ObservableProperty]
    private bool? _successFilter;

    /// <summary>
    /// Gets or sets the suspicious activity items.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<SuspiciousActivityItem> _suspiciousActivity = [];

    /// <summary>
    /// Gets or sets whether there is suspicious activity.
    /// </summary>
    [ObservableProperty]
    private bool _hasSuspiciousActivity;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginAuditViewModel"/> class.
    /// </summary>
    public LoginAuditViewModel(
        ILogger logger,
        ILoginAuditService loginAuditService,
        IUserService userService,
        INavigationService navigationService)
        : base(logger)
    {
        _loginAuditService = loginAuditService ?? throw new ArgumentNullException(nameof(loginAuditService));
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

        Title = "Login Audit";
    }

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
        _ = LoadDataAsync();
    }

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
        // No cleanup needed
    }

    /// <summary>
    /// Loads the audit data.
    /// </summary>
    [RelayCommand]
    private async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            // Load users for filter
            var users = await _userService.GetAllUsersAsync();
            Users = new ObservableCollection<UserFilterItem>(
                [new UserFilterItem { Id = null, DisplayName = "All Users" },
                 .. users.Select(u => new UserFilterItem { Id = u.Id, DisplayName = u.FullName ?? u.Username })]);

            if (SelectedUser == null)
            {
                SelectedUser = Users.FirstOrDefault();
            }

            // Load audit records
            await RefreshAuditRecordsAsync();

            // Load suspicious activity
            await LoadSuspiciousActivityAsync();

        }, "Loading audit data...");
    }

    /// <summary>
    /// Refreshes the audit records based on current filters.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAuditRecordsAsync()
    {
        await ExecuteAsync(async () =>
        {
            IReadOnlyList<LoginAudit> records;

            if (SelectedUser?.Id.HasValue == true)
            {
                records = await _loginAuditService.GetUserLoginHistoryAsync(
                    SelectedUser.Id.Value,
                    FromDate,
                    ToDate);
            }
            else
            {
                records = await _loginAuditService.GetLoginHistoryAsync(
                    FromDate,
                    ToDate,
                    SuccessFilter);
            }

            AuditRecords = new ObservableCollection<LoginAuditDisplayItem>(
                records.Select(r => new LoginAuditDisplayItem
                {
                    Id = r.Id,
                    Username = r.Username,
                    FullName = r.User?.FullName ?? r.Username,
                    Success = r.Success,
                    FailureReason = r.FailureReason,
                    Timestamp = r.Timestamp.ToLocalTime(),
                    IpAddress = r.IpAddress,
                    MachineName = r.MachineName,
                    DeviceInfo = r.DeviceInfo,
                    IsLogout = r.IsLogout,
                    SessionDurationMinutes = r.SessionDurationMinutes
                }));

            _logger.Information("Loaded {Count} audit records", AuditRecords.Count);

        }, "Loading records...");
    }

    private async Task LoadSuspiciousActivityAsync()
    {
        var suspicious = await _loginAuditService.GetSuspiciousActivityAsync(60, 3);

        SuspiciousActivity = new ObservableCollection<SuspiciousActivityItem>(
            suspicious.Select(kvp => new SuspiciousActivityItem
            {
                Username = kvp.Key,
                FailedAttempts = kvp.Value,
                TimeWindow = "Last 60 minutes"
            }));

        HasSuspiciousActivity = SuspiciousActivity.Count > 0;
    }

    /// <summary>
    /// Sets the date range filter to today.
    /// </summary>
    [RelayCommand]
    private void SetToday()
    {
        FromDate = DateTime.Today;
        ToDate = DateTime.Today;
    }

    /// <summary>
    /// Sets the date range filter to yesterday.
    /// </summary>
    [RelayCommand]
    private void SetYesterday()
    {
        FromDate = DateTime.Today.AddDays(-1);
        ToDate = DateTime.Today.AddDays(-1);
    }

    /// <summary>
    /// Sets the date range filter to this week.
    /// </summary>
    [RelayCommand]
    private void SetThisWeek()
    {
        var today = DateTime.Today;
        var dayOfWeek = (int)today.DayOfWeek;
        FromDate = today.AddDays(-dayOfWeek);
        ToDate = today;
    }

    /// <summary>
    /// Sets the date range filter to this month.
    /// </summary>
    [RelayCommand]
    private void SetThisMonth()
    {
        var today = DateTime.Today;
        FromDate = new DateTime(today.Year, today.Month, 1);
        ToDate = today;
    }

    /// <summary>
    /// Shows all records (no success filter).
    /// </summary>
    [RelayCommand]
    private void ShowAll()
    {
        SuccessFilter = null;
        _ = RefreshAuditRecordsAsync();
    }

    /// <summary>
    /// Shows only successful logins.
    /// </summary>
    [RelayCommand]
    private void ShowSuccessOnly()
    {
        SuccessFilter = true;
        _ = RefreshAuditRecordsAsync();
    }

    /// <summary>
    /// Shows only failed logins.
    /// </summary>
    [RelayCommand]
    private void ShowFailedOnly()
    {
        SuccessFilter = false;
        _ = RefreshAuditRecordsAsync();
    }

    /// <summary>
    /// Navigates back to the previous view.
    /// </summary>
    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    partial void OnSelectedUserChanged(UserFilterItem? value)
    {
        _ = RefreshAuditRecordsAsync();
    }

    partial void OnFromDateChanged(DateTime value)
    {
        _ = RefreshAuditRecordsAsync();
    }

    partial void OnToDateChanged(DateTime value)
    {
        _ = RefreshAuditRecordsAsync();
    }
}

/// <summary>
/// Display item for login audit records.
/// </summary>
public class LoginAuditDisplayItem
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public DateTime Timestamp { get; set; }
    public string? IpAddress { get; set; }
    public string? MachineName { get; set; }
    public string? DeviceInfo { get; set; }
    public bool IsLogout { get; set; }
    public int? SessionDurationMinutes { get; set; }

    public string Status => IsLogout ? "Logout" : (Success ? "Success" : "Failed");
    public string StatusColor => IsLogout ? "#3B82F6" : (Success ? "#22C55E" : "#EF4444");
    public string TimestampDisplay => Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
    public string SessionDurationDisplay => SessionDurationMinutes.HasValue
        ? $"{SessionDurationMinutes.Value} min"
        : string.Empty;
}

/// <summary>
/// User filter item for dropdown.
/// </summary>
public class UserFilterItem
{
    public int? Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>
/// Suspicious activity display item.
/// </summary>
public class SuspiciousActivityItem
{
    public string Username { get; set; } = string.Empty;
    public int FailedAttempts { get; set; }
    public string TimeWindow { get; set; } = string.Empty;
}
