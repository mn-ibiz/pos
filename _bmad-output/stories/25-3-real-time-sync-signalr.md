# Story 25.3: Real-Time Sync (SignalR)

## Story
**As the** system,
**I want** real-time data updates when online,
**So that** HQ and stores have current data.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Epic
**Epic 25: Offline-First Architecture & Cloud Sync**

## Acceptance Criteria

### AC1: Real-Time Connection
**Given** internet is connected
**When** SignalR connection established
**Then** changes sync in real-time

### AC2: Auto-Reconnection
**Given** connection drops
**When** reconnecting
**Then** automatically re-establishes and syncs pending items

### AC3: Batch Sync on Reconnect
**Given** large backlog exists
**When** reconnecting
**Then** batch syncs in priority order without blocking UI

## Technical Notes
```csharp
public class SignalRSyncService
{
    private readonly HubConnection _hubConnection;
    private readonly ISyncQueueService _syncQueue;
    private bool _isConnected;

    public event EventHandler<ConnectionStatus> ConnectionStatusChanged;
    public event EventHandler<SyncProgress> SyncProgressChanged;

    public async Task ConnectAsync();
    public async Task DisconnectAsync();
    public async Task SendAsync<T>(string method, T data);
    public async Task SyncPendingItemsAsync();
}

public class ConnectionStatus
{
    public bool IsConnected { get; set; }
    public DateTime LastConnectedAt { get; set; }
    public DateTime? DisconnectedAt { get; set; }
    public int ReconnectAttempts { get; set; }
    public string LastError { get; set; }
}

public class SyncProgress
{
    public int TotalItems { get; set; }
    public int CompletedItems { get; set; }
    public int FailedItems { get; set; }
    public decimal ProgressPercent => TotalItems > 0
        ? (decimal)CompletedItems / TotalItems * 100 : 0;
    public TimeSpan EstimatedTimeRemaining { get; set; }
}

public class SignalRConfiguration
{
    public string HubUrl { get; set; }
    public int ReconnectDelaySeconds { get; set; } = 5;
    public int MaxReconnectAttempts { get; set; } = 10;
    public int KeepAliveIntervalSeconds { get; set; } = 15;
    public int BatchSize { get; set; } = 50;
}

// SignalR Hub Methods
public interface ISyncHub
{
    Task ReceiveTransaction(TransactionSyncDto transaction);
    Task ReceiveProductUpdate(ProductSyncDto product);
    Task ReceivePriceUpdate(PriceSyncDto price);
    Task ReceiveInventoryUpdate(InventorySyncDto inventory);
    Task SyncComplete(SyncCompletionDto completion);
}
```

## Definition of Done
- [x] SignalR hub connection established
- [x] Real-time sync for transactions
- [x] Auto-reconnection with backoff
- [x] Background batch sync
- [x] Connection status indicator
- [x] Unit tests passing

## Implementation Summary

### DTOs Added (SyncDtos.cs)
- `SignalRConfiguration` - Configuration for hub URL, reconnect settings, batch size, timeout
- `SignalRConnectionState` enum - Disconnected, Connecting, Connected, Reconnecting
- `SignalRConnectionStatusDto` - Connection state, timestamps, reconnect attempts, errors
- `SignalRSyncItemDto` - Sync item with entity type, ID, operation, payload, correlation ID
- `SignalRBatchSyncRequestDto` - Request for batch sync with store ID, timestamp, entity types
- `SignalRBatchSyncResponseDto` - Response with items, pagination support
- `SignalRSyncAckDto` - Acknowledgment with correlation ID and result
- `SignalRSyncProgressDto` - Progress with counts, percentage, ETA
- `SignalRHeartbeatDto` - Health check with store ID and pending count

### Interface Created (ISyncHubService.cs)
Comprehensive interface with:
- **Connection Management**: `ConnectAsync`, `DisconnectAsync`, `StartAsync`, `StopAsync`
- **Properties**: `ConnectionStatus`, `IsConnected`, `LastHeartbeatResponse`, `Configuration`
- **Events**: `ConnectionStatusChanged`, `SyncItemReceived`, `SyncProgressChanged`, `ErrorOccurred`
- **Sync Operations**: `SendSyncItemAsync`, `SendBatchAsync`, `RequestBatchSyncAsync`, `SyncPendingItemsAsync`, `SendAcknowledgmentAsync`
- **Heartbeat**: `SendHeartbeatAsync`
- **Hub Invocation**: `InvokeAsync`, `InvokeAsync<T>`
- **Configuration**: `UpdateConfiguration`

### Factory Created (ISyncHubServiceFactory)
Factory pattern for creating SyncHubService instances with different configurations

### Service Implementation (SyncHubService.cs)
Full SignalR client implementation including:
- HubConnection management with proper lifecycle
- Automatic reconnection with exponential backoff (5s, 10s, 20s... up to 2 min)
- Connection status tracking and event notifications
- Handler registration for incoming sync items, progress, heartbeat, errors
- Batch sync with configurable batch size
- Pending items sync on reconnection
- Heartbeat for connection health monitoring
- Proper disposal of resources

### Retry Policy (SignalRRetryPolicy)
Custom IRetryPolicy implementation for SignalR's automatic reconnection with exponential backoff

### Package Added
- `Microsoft.AspNetCore.SignalR.Client` v10.0.0 to Infrastructure project

### Unit Tests (SyncHubServiceTests.cs)
Comprehensive test coverage (30+ tests) including:
- Constructor validation tests
- Connection status tests
- Connect/Disconnect tests
- Configuration management tests
- Send sync item tests (when not connected)
- Batch sync tests
- Heartbeat tests
- Invoke tests
- Dispose tests
- Event handling tests
- Factory tests
- DTO validation tests (default values, calculations)
