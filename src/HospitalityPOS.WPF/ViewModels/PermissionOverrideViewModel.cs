using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for Permission Override - handles temporary overrides, audit logs, and override management.
/// </summary>
public partial class PermissionOverrideViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IServiceScopeFactory _scopeFactory;

    #region Observable Properties

    [ObservableProperty]
    private ObservableCollection<PermissionOverride> _activeOverrides = new();

    [ObservableProperty]
    private ObservableCollection<PermissionOverride> _expiredOverrides = new();

    [ObservableProperty]
    private ObservableCollection<OverrideAuditEntry> _auditLog = new();

    [ObservableProperty]
    private PermissionOverride? _selectedOverride;

    [ObservableProperty]
    private OverrideSettings _settings = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private DateOnly _auditStartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-7));

    [ObservableProperty]
    private DateOnly _auditEndDate = DateOnly.FromDateTime(DateTime.Today);

    // Summary stats
    [ObservableProperty]
    private int _activeOverridesCount;

    [ObservableProperty]
    private int _overridesTodayCount;

    [ObservableProperty]
    private int _expiredTodayCount;

    // Override Creator
    [ObservableProperty]
    private bool _isOverrideCreatorOpen;

    [ObservableProperty]
    private CreateOverrideRequest _newOverride = new();

    #endregion

    public List<string> OverrideScopes { get; } = new()
    {
        "SingleAction", "Session", "TimeBound", "CountBound"
    };

    public PermissionOverrideViewModel(
        ILogger logger,
        INavigationService navigationService,
        IServiceScopeFactory scopeFactory)
        : base(logger)
    {
        _navigationService = navigationService;
        _scopeFactory = scopeFactory;
        Title = "Permission Override Management";
    }

    public async Task InitializeAsync()
    {
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            IsLoading = true;

            using var scope = _scopeFactory.CreateScope();
            var overrideService = scope.ServiceProvider.GetService<IPermissionOverrideService>();

            if (overrideService is null)
            {
                ErrorMessage = "Permission Override service not available";
                return;
            }

            // Load active overrides
            var active = await overrideService.GetActiveOverridesAsync();
            ActiveOverrides = new ObservableCollection<PermissionOverride>(active);
            ActiveOverridesCount = active.Count;

            // Load expired overrides (today)
            var today = DateOnly.FromDateTime(DateTime.Today);
            var expired = await overrideService.GetExpiredOverridesAsync(today, today);
            ExpiredOverrides = new ObservableCollection<PermissionOverride>(expired);
            ExpiredTodayCount = expired.Count;

            // Count overrides used today
            OverridesTodayCount = await overrideService.GetOverrideCountAsync(today);

            // Load audit log
            var audit = await overrideService.GetAuditLogAsync(AuditStartDate, AuditEndDate);
            AuditLog = new ObservableCollection<OverrideAuditEntry>(audit);

            // Load settings
            Settings = await overrideService.GetSettingsAsync();

            IsLoading = false;
        }, "Loading override data...");
    }

    [RelayCommand]
    private void CreateOverride()
    {
        NewOverride = new CreateOverrideRequest
        {
            Scope = "SingleAction",
            ExpiresAt = DateTime.Now.AddHours(1)
        };
        IsOverrideCreatorOpen = true;
    }

    [RelayCommand]
    private async Task SaveOverrideAsync()
    {
        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var overrideService = scope.ServiceProvider.GetService<IPermissionOverrideService>();

            if (overrideService is null)
            {
                ErrorMessage = "Permission Override service not available";
                return;
            }

            NewOverride.CreatedByUserId = SessionService.CurrentUserId;
            var result = await overrideService.CreateOverrideAsync(NewOverride);

            if (result.Success)
            {
                IsOverrideCreatorOpen = false;
                await DialogService.ShowMessageAsync("Success", "Permission override created.");
                await LoadDataAsync();
            }
            else
            {
                ErrorMessage = result.Message;
            }
        }, "Creating override...");
    }

    [RelayCommand]
    private void CancelCreateOverride()
    {
        IsOverrideCreatorOpen = false;
    }

    [RelayCommand]
    private async Task RevokeOverrideAsync(PermissionOverride? overrideItem)
    {
        if (overrideItem is null) return;

        var reason = await DialogService.ShowInputAsync("Revoke Override", "Reason for revocation:");

        if (string.IsNullOrWhiteSpace(reason)) return;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var overrideService = scope.ServiceProvider.GetService<IPermissionOverrideService>();

            if (overrideService is null)
            {
                ErrorMessage = "Permission Override service not available";
                return;
            }

            await overrideService.RevokeOverrideAsync(overrideItem.Id, SessionService.CurrentUserId, reason);
            await LoadDataAsync();
        }, "Revoking override...");
    }

    [RelayCommand]
    private async Task ExtendOverrideAsync(PermissionOverride? overrideItem)
    {
        if (overrideItem is null) return;

        var hoursStr = await DialogService.ShowInputAsync("Extend Override", "Extend by how many hours?");

        if (!int.TryParse(hoursStr, out var hours) || hours <= 0)
        {
            ErrorMessage = "Invalid hours entered";
            return;
        }

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var overrideService = scope.ServiceProvider.GetService<IPermissionOverrideService>();

            if (overrideService is null)
            {
                ErrorMessage = "Permission Override service not available";
                return;
            }

            await overrideService.ExtendOverrideAsync(overrideItem.Id, TimeSpan.FromHours(hours), SessionService.CurrentUserId);
            await DialogService.ShowMessageAsync("Success", $"Override extended by {hours} hours.");
            await LoadDataAsync();
        }, "Extending override...");
    }

    [RelayCommand]
    private async Task ViewOverrideDetailsAsync(PermissionOverride? overrideItem)
    {
        if (overrideItem is null) return;

        await DialogService.ShowMessageAsync(
            "Override Details",
            $"Permission: {overrideItem.PermissionName}\n" +
            $"User: {overrideItem.UserName}\n" +
            $"Scope: {overrideItem.Scope}\n" +
            $"Action: {overrideItem.ActionDescription}\n" +
            $"Created: {overrideItem.CreatedAt:g}\n" +
            $"Expires: {overrideItem.ExpiresAt:g}\n" +
            $"Status: {overrideItem.Status}\n" +
            $"Uses: {overrideItem.UsageCount}/{overrideItem.MaxUses?.ToString() ?? "Unlimited"}");
    }

    [RelayCommand]
    private async Task ViewAuditDetailsAsync(OverrideAuditEntry? entry)
    {
        if (entry is null) return;

        await DialogService.ShowMessageAsync(
            "Audit Entry",
            $"Action: {entry.Action}\n" +
            $"Permission: {entry.PermissionName}\n" +
            $"User: {entry.UserName}\n" +
            $"Authorized By: {entry.AuthorizedByName}\n" +
            $"Result: {entry.Result}\n" +
            $"Timestamp: {entry.Timestamp:g}\n" +
            $"Details: {entry.Details}");
    }

    [RelayCommand]
    private async Task ExportAuditLogAsync()
    {
        await DialogService.ShowMessageAsync("Export", "Audit log export functionality will be available soon.");
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var overrideService = scope.ServiceProvider.GetService<IPermissionOverrideService>();

            if (overrideService is null)
            {
                ErrorMessage = "Permission Override service not available";
                return;
            }

            await overrideService.UpdateSettingsAsync(Settings);
            await DialogService.ShowMessageAsync("Success", "Override settings saved.");
        }, "Saving settings...");
    }

    [RelayCommand]
    private async Task CleanupExpiredAsync()
    {
        var confirmed = await DialogService.ShowConfirmationAsync(
            "Cleanup Expired Overrides",
            "Remove all expired overrides from active list?");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var overrideService = scope.ServiceProvider.GetService<IPermissionOverrideService>();

            if (overrideService is null)
            {
                ErrorMessage = "Permission Override service not available";
                return;
            }

            var count = await overrideService.CleanupExpiredAsync();
            await DialogService.ShowMessageAsync("Success", $"Cleaned up {count} expired overrides.");
            await LoadDataAsync();
        }, "Cleaning up...");
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    partial void OnAuditStartDateChanged(DateOnly value) => _ = LoadDataAsync();
    partial void OnAuditEndDateChanged(DateOnly value) => _ = LoadDataAsync();
}

// DTOs
public class PermissionOverride
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string PermissionName { get; set; } = string.Empty;
    public string ActionDescription { get; set; } = string.Empty;
    public string Scope { get; set; } = "SingleAction";
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int? MaxUses { get; set; }
    public int UsageCount { get; set; }
    public string Status { get; set; } = "Active"; // Active, Expired, Revoked, Exhausted
    public int AuthorizedByUserId { get; set; }
    public string? AuthorizedByName { get; set; }
    public string? Notes { get; set; }

    public bool IsExpired => DateTime.Now > ExpiresAt;
    public bool IsExhausted => MaxUses.HasValue && UsageCount >= MaxUses.Value;
    public TimeSpan TimeRemaining => ExpiresAt > DateTime.Now ? ExpiresAt - DateTime.Now : TimeSpan.Zero;
}

public class CreateOverrideRequest
{
    public int UserId { get; set; }
    public string PermissionName { get; set; } = string.Empty;
    public string ActionDescription { get; set; } = string.Empty;
    public string Scope { get; set; } = "SingleAction";
    public DateTime ExpiresAt { get; set; }
    public int? MaxUses { get; set; }
    public int CreatedByUserId { get; set; }
    public string? Notes { get; set; }
}

public class OverrideResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public PermissionOverride? Override { get; set; }
}

public class OverrideAuditEntry
{
    public int Id { get; set; }
    public int? OverrideId { get; set; }
    public string Action { get; set; } = string.Empty; // Created, Used, Revoked, Expired, Extended
    public string PermissionName { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int? AuthorizedByUserId { get; set; }
    public string? AuthorizedByName { get; set; }
    public string Result { get; set; } = string.Empty; // Success, Denied, Expired
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; }
}

public class OverrideSettings
{
    public int DefaultExpirationMinutes { get; set; } = 60;
    public int MaxExpirationHours { get; set; } = 24;
    public bool RequirePIN { get; set; } = true;
    public int MaxConcurrentOverrides { get; set; } = 5;
    public bool NotifyOnOverride { get; set; } = true;
    public bool LogAllAttempts { get; set; } = true;
    public int AuditRetentionDays { get; set; } = 90;
}
