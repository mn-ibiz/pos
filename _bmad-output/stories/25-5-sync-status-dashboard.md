# Story 25.5: Sync Status Dashboard

## Story
**As a** manager,
**I want to** see sync status at a glance,
**So that** I know data is being synchronized.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Epic
**Epic 25: Offline-First Architecture & Cloud Sync**

## Acceptance Criteria

### AC1: Status Overview
**Given** accessing sync status
**When** viewing dashboard
**Then** shows: Online/Offline, Pending items, Last sync time

### AC2: Error Details
**Given** sync issues exist
**When** viewing details
**Then** lists failed items with error reasons

### AC3: Manual Sync
**Given** manual sync needed
**When** pressing sync button
**Then** immediately attempts to sync all pending items

## Technical Notes
```csharp
public class SyncStatusDashboard
{
    public ConnectionState ConnectionState { get; set; }
    public DateTime LastSyncTime { get; set; }
    public TimeSpan TimeSinceLastSync => DateTime.Now - LastSyncTime;
    public SyncQueueSummary QueueSummary { get; set; }
    public List<SyncError> RecentErrors { get; set; }
    public SyncHealth OverallHealth { get; set; }
}

public enum ConnectionState
{
    Online,
    Offline,
    Connecting,
    Syncing,
    Error
}

public enum SyncHealth
{
    Healthy,      // All synced, no errors
    Warning,      // Some pending, no errors
    Degraded,     // Many pending or some errors
    Critical      // Many errors or long time since sync
}

public class SyncQueueSummary
{
    public int TotalPending { get; set; }
    public int CriticalPending { get; set; }  // eTIMS
    public int HighPending { get; set; }
    public int NormalPending { get; set; }
    public int FailedItems { get; set; }
    public int ItemsSyncedToday { get; set; }
}

public class SyncError
{
    public Guid QueueItemId { get; set; }
    public string EntityType { get; set; }
    public string ErrorMessage { get; set; }
    public DateTime OccurredAt { get; set; }
    public int AttemptCount { get; set; }
    public bool CanRetry { get; set; }
}

public class SyncStatusViewModel : ViewModelBase
{
    public SyncStatusDashboard Status { get; }
    public ICommand ManualSyncCommand { get; }
    public ICommand RetryFailedCommand { get; }
    public ICommand ViewErrorDetailsCommand { get; }
    public ICommand ClearErrorsCommand { get; }
}
```

## Definition of Done
- [x] Sync status dashboard UI
- [x] Real-time status updates
- [x] Error listing with details
- [x] Manual sync button
- [x] Retry failed items capability
- [x] Status bar indicator
- [x] Unit tests passing

## Implementation Summary

### DTOs Added (SyncDtos.cs)
- `SyncConnectionState` enum - Online, Offline, Connecting, Syncing, Error
- `SyncHealthStatus` enum - Healthy, Warning, Degraded, Critical
- `SyncStatusDashboardDto` - Complete dashboard with queue summary, errors, metrics, conflicts
- `SyncQueueSummaryDto` - Queue counts by priority, failed items, synced today/hour
- `SyncErrorDto` - Error details with retry info, attempt counts, next retry time
- `SyncMetricsDto` - Performance metrics, success rates, sync counts by entity type
- `SyncActivityDto` - Recent sync activity log entry
- `ManualSyncRequestDto` - Request to trigger manual sync with filters
- `ManualSyncResultDto` - Result with counts, errors, duration
- `SyncStatusBarDto` - Compact status for status bar with color-coded health
- `RetryFailedItemsRequestDto` - Request to retry failed items
- `RetryResultDto` - Result of retry operation

### Interface Created (ISyncStatusService.cs)
Comprehensive interface with:
- **Dashboard**: `GetDashboardAsync`, `GetStatusBarAsync`, connection/health properties
- **Queue Status**: `GetQueueSummaryAsync`, `GetPendingCountAsync`, `GetFailedCountAsync`
- **Errors**: `GetRecentErrorsAsync`, `GetErrorDetailsAsync`, `ClearErrorsAsync`
- **Sync Operations**: `TriggerManualSyncAsync`, `RetryFailedItemsAsync`, `RetryItemAsync`, `CancelItemAsync`
- **Metrics**: `GetMetricsAsync`, `GetRecentActivityAsync`
- **Events**: `ConnectionStateChanged`, `HealthStatusChanged`, `SyncProgressChanged`, `SyncErrorOccurred`, `DashboardUpdated`
- **Health Calculation**: `CalculateHealthStatus`, `GetHealthMessage`
- **Connection Management**: `CheckConnectionAsync`, `ReconnectAsync`, `SetConnectionState`
- **Background Updates**: `StartBackgroundUpdatesAsync`, `StopBackgroundUpdatesAsync`, `RefreshStatusAsync`

### Service Implementation (SyncStatusService.cs)
Full sync status service including:
- Dashboard aggregation from multiple sources (queue, conflicts, hub)
- Health status calculation based on pending counts, failures, last sync time
- Queue summary with priority breakdown
- Error listing with retry eligibility
- Manual sync trigger with progress events
- Retry failed items individually or in bulk
- Cancel pending sync items
- Metrics calculation with success/failure rates
- Background status update loop
- Event-driven state notifications
- Integration with SyncHubService for connection status
- Thread-safe state management with locks

### Unit Tests (SyncStatusServiceTests.cs)
Comprehensive test coverage (50+ tests) including:
- Constructor validation tests
- Dashboard generation tests
- Queue summary tests with various item states
- Error handling tests
- Sync operation tests (manual sync, retry, cancel)
- Health calculation tests for all status levels
- Connection management tests
- DTO tests for calculated properties
- Metrics calculation tests
- Event handling tests
