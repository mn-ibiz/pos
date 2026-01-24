using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.WPF.Services;

namespace HospitalityPOS.WPF.ViewModels;

/// <summary>
/// ViewModel for Conflict Resolution - handles data sync conflicts, resolution, and audit.
/// </summary>
public partial class ConflictResolutionViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IServiceScopeFactory _scopeFactory;

    #region Observable Properties

    [ObservableProperty]
    private ObservableCollection<SyncConflict> _conflicts = new();

    [ObservableProperty]
    private ObservableCollection<SyncConflict> _resolvedConflicts = new();

    [ObservableProperty]
    private ObservableCollection<ConflictAuditLog> _auditLogs = new();

    [ObservableProperty]
    private SyncConflict? _selectedConflict;

    [ObservableProperty]
    private ConflictResolutionSettings _settings = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _selectedTabIndex;

    // Summary stats
    [ObservableProperty]
    private int _pendingConflictsCount;

    [ObservableProperty]
    private int _resolvedTodayCount;

    [ObservableProperty]
    private int _autoResolvedCount;

    [ObservableProperty]
    private DateTime? _lastSyncTime;

    // Conflict Details
    [ObservableProperty]
    private bool _isConflictDetailsOpen;

    [ObservableProperty]
    private ConflictDetails? _currentConflictDetails;

    #endregion

    public List<string> ResolutionStrategies { get; } = new()
    {
        "LastWriteWins", "LocalPreferred", "RemotePreferred", "Manual", "Merge"
    };

    public ConflictResolutionViewModel(
        ILogger logger,
        INavigationService navigationService,
        IServiceScopeFactory scopeFactory)
        : base(logger)
    {
        _navigationService = navigationService;
        _scopeFactory = scopeFactory;
        Title = "Conflict Resolution";
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
            var conflictService = scope.ServiceProvider.GetService<IConflictResolutionService>();

            if (conflictService is null)
            {
                ErrorMessage = "Conflict Resolution service not available";
                return;
            }

            // Load pending conflicts
            var pending = await conflictService.GetPendingConflictsAsync();
            Conflicts = new ObservableCollection<SyncConflict>(pending);
            PendingConflictsCount = pending.Count;

            // Load resolved conflicts (today)
            var today = DateOnly.FromDateTime(DateTime.Today);
            var resolved = await conflictService.GetResolvedConflictsAsync(today, today);
            ResolvedConflicts = new ObservableCollection<SyncConflict>(resolved);
            ResolvedTodayCount = resolved.Count;
            AutoResolvedCount = resolved.Count(c => c.ResolutionMethod == "Auto");

            // Load audit logs
            var logs = await conflictService.GetAuditLogsAsync(50);
            AuditLogs = new ObservableCollection<ConflictAuditLog>(logs);

            // Load settings
            Settings = await conflictService.GetSettingsAsync();

            // Get last sync time
            LastSyncTime = await conflictService.GetLastSyncTimeAsync();

            IsLoading = false;
        }, "Loading conflict data...");
    }

    [RelayCommand]
    private async Task ViewConflictDetailsAsync(SyncConflict? conflict)
    {
        if (conflict is null) return;

        SelectedConflict = conflict;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var conflictService = scope.ServiceProvider.GetService<IConflictResolutionService>();

            if (conflictService is null)
            {
                ErrorMessage = "Conflict Resolution service not available";
                return;
            }

            CurrentConflictDetails = await conflictService.GetConflictDetailsAsync(conflict.Id);
            IsConflictDetailsOpen = true;
        }, "Loading conflict details...");
    }

    [RelayCommand]
    private void CloseConflictDetails()
    {
        IsConflictDetailsOpen = false;
    }

    [RelayCommand]
    private async Task UseLocalValueAsync()
    {
        if (SelectedConflict is null) return;

        await ResolveConflictAsync(SelectedConflict.Id, "Local");
    }

    [RelayCommand]
    private async Task UseRemoteValueAsync()
    {
        if (SelectedConflict is null) return;

        await ResolveConflictAsync(SelectedConflict.Id, "Remote");
    }

    [RelayCommand]
    private async Task MergeValuesAsync()
    {
        if (SelectedConflict is null || CurrentConflictDetails is null) return;

        // For now, show message - in real implementation, would show merge dialog
        await DialogService.ShowMessageAsync("Merge",
            "Merge functionality allows combining values from both sources.\nThis feature is available for compatible data types.");
    }

    [RelayCommand]
    private async Task IgnoreConflictAsync(SyncConflict? conflict)
    {
        if (conflict is null) return;

        var confirmed = await DialogService.ShowConfirmationAsync(
            "Ignore Conflict",
            "Are you sure you want to ignore this conflict?\nThe local value will be kept.");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var conflictService = scope.ServiceProvider.GetService<IConflictResolutionService>();

            if (conflictService is null)
            {
                ErrorMessage = "Conflict Resolution service not available";
                return;
            }

            await conflictService.IgnoreConflictAsync(conflict.Id, SessionService.CurrentUserId, "User chose to ignore");
            IsConflictDetailsOpen = false;
            await LoadDataAsync();
        }, "Ignoring conflict...");
    }

    private async Task ResolveConflictAsync(int conflictId, string resolution)
    {
        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var conflictService = scope.ServiceProvider.GetService<IConflictResolutionService>();

            if (conflictService is null)
            {
                ErrorMessage = "Conflict Resolution service not available";
                return;
            }

            var request = new ManualResolutionRequest
            {
                ConflictId = conflictId,
                Resolution = resolution,
                ResolvedByUserId = SessionService.CurrentUserId
            };

            var result = await conflictService.ResolveManuallyAsync(request);

            if (result.Success)
            {
                IsConflictDetailsOpen = false;
                await DialogService.ShowMessageAsync("Success", "Conflict resolved successfully.");
                await LoadDataAsync();
            }
            else
            {
                ErrorMessage = result.Message;
            }
        }, "Resolving conflict...");
    }

    [RelayCommand]
    private async Task AutoResolveAllAsync()
    {
        var confirmed = await DialogService.ShowConfirmationAsync(
            "Auto-Resolve All",
            $"Auto-resolve all {PendingConflictsCount} pending conflicts using configured rules?");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var conflictService = scope.ServiceProvider.GetService<IConflictResolutionService>();

            if (conflictService is null)
            {
                ErrorMessage = "Conflict Resolution service not available";
                return;
            }

            var result = await conflictService.AutoResolveAllAsync();
            await DialogService.ShowMessageAsync(
                "Auto-Resolve Complete",
                $"Resolved: {result.ResolvedCount}\nFailed: {result.FailedCount}\nSkipped: {result.SkippedCount}");
            await LoadDataAsync();
        }, "Auto-resolving conflicts...");
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var conflictService = scope.ServiceProvider.GetService<IConflictResolutionService>();

            if (conflictService is null)
            {
                ErrorMessage = "Conflict Resolution service not available";
                return;
            }

            await conflictService.UpdateSettingsAsync(Settings);
            await DialogService.ShowMessageAsync("Success", "Conflict resolution settings saved.");
        }, "Saving settings...");
    }

    [RelayCommand]
    private async Task TriggerSyncAsync()
    {
        await ExecuteAsync(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var conflictService = scope.ServiceProvider.GetService<IConflictResolutionService>();

            if (conflictService is null)
            {
                ErrorMessage = "Conflict Resolution service not available";
                return;
            }

            await conflictService.TriggerSyncAsync();
            await DialogService.ShowMessageAsync("Success", "Sync triggered successfully.");
            await LoadDataAsync();
        }, "Triggering sync...");
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }
}

// DTOs
public class SyncConflict
{
    public int Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public string LocalValue { get; set; } = string.Empty;
    public string RemoteValue { get; set; } = string.Empty;
    public DateTime LocalModifiedAt { get; set; }
    public DateTime RemoteModifiedAt { get; set; }
    public string? LocalModifiedBy { get; set; }
    public string? RemoteModifiedBy { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Resolved, Ignored
    public string? ResolutionMethod { get; set; } // Auto, Manual
    public DateTime DetectedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public class ConflictDetails
{
    public int ConflictId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public List<FieldConflict> ConflictingFields { get; set; } = new();
    public string LocalVersion { get; set; } = string.Empty;
    public string RemoteVersion { get; set; } = string.Empty;
    public bool CanMerge { get; set; }
    public string? SuggestedResolution { get; set; }
}

public class FieldConflict
{
    public string FieldName { get; set; } = string.Empty;
    public string LocalValue { get; set; } = string.Empty;
    public string RemoteValue { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
}

public class ManualResolutionRequest
{
    public int ConflictId { get; set; }
    public string Resolution { get; set; } = string.Empty; // Local, Remote, Merged
    public string? MergedValue { get; set; }
    public int ResolvedByUserId { get; set; }
    public string? Notes { get; set; }
}

public class ResolutionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class AutoResolveResult
{
    public int ResolvedCount { get; set; }
    public int FailedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class ConflictAuditLog
{
    public int Id { get; set; }
    public int ConflictId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Resolution { get; set; } = string.Empty;
    public int? ResolvedByUserId { get; set; }
    public string? ResolvedByName { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Notes { get; set; }
}

public class ConflictResolutionSettings
{
    public string DefaultStrategy { get; set; } = "LastWriteWins";
    public bool AutoResolveEnabled { get; set; } = true;
    public bool NotifyOnConflict { get; set; } = true;
    public int ConflictRetentionDays { get; set; } = 30;
    public Dictionary<string, string> EntityStrategies { get; set; } = new();
}
