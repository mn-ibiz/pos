using HospitalityPOS.Core.DTOs;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for SignalR-based real-time synchronization.
/// </summary>
public interface ISyncHubService : IDisposable
{
    #region Connection Management

    /// <summary>
    /// Gets the current connection status.
    /// </summary>
    SignalRConnectionStatusDto ConnectionStatus { get; }

    /// <summary>
    /// Gets whether the hub is currently connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Connects to the SignalR hub.
    /// </summary>
    /// <returns>True if connection succeeded.</returns>
    Task<bool> ConnectAsync();

    /// <summary>
    /// Disconnects from the SignalR hub.
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// Starts auto-reconnection if enabled.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the service and disconnects.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Events

    /// <summary>
    /// Event raised when connection status changes.
    /// </summary>
    event EventHandler<SignalRConnectionStatusDto>? ConnectionStatusChanged;

    /// <summary>
    /// Event raised when a sync item is received.
    /// </summary>
    event EventHandler<SignalRSyncItemDto>? SyncItemReceived;

    /// <summary>
    /// Event raised when sync progress updates.
    /// </summary>
    event EventHandler<SignalRSyncProgressDto>? SyncProgressChanged;

    /// <summary>
    /// Event raised when an error occurs.
    /// </summary>
    event EventHandler<string>? ErrorOccurred;

    #endregion

    #region Sync Operations

    /// <summary>
    /// Sends a sync item to the hub.
    /// </summary>
    /// <param name="item">The sync item to send.</param>
    /// <returns>True if sent successfully.</returns>
    Task<bool> SendSyncItemAsync(SignalRSyncItemDto item);

    /// <summary>
    /// Sends multiple sync items as a batch.
    /// </summary>
    /// <param name="items">The items to send.</param>
    /// <returns>Number of items sent successfully.</returns>
    Task<int> SendBatchAsync(IEnumerable<SignalRSyncItemDto> items);

    /// <summary>
    /// Requests batch sync from the hub.
    /// </summary>
    /// <param name="request">The batch sync request.</param>
    /// <returns>The batch sync response.</returns>
    Task<SignalRBatchSyncResponseDto?> RequestBatchSyncAsync(SignalRBatchSyncRequestDto request);

    /// <summary>
    /// Syncs all pending items from the queue.
    /// </summary>
    /// <returns>Sync progress result.</returns>
    Task<SignalRSyncProgressDto> SyncPendingItemsAsync();

    /// <summary>
    /// Sends an acknowledgment for a received sync item.
    /// </summary>
    /// <param name="ack">The acknowledgment.</param>
    Task SendAcknowledgmentAsync(SignalRSyncAckDto ack);

    #endregion

    #region Heartbeat

    /// <summary>
    /// Sends a heartbeat to the hub.
    /// </summary>
    /// <param name="heartbeat">The heartbeat data.</param>
    Task SendHeartbeatAsync(SignalRHeartbeatDto heartbeat);

    /// <summary>
    /// Gets the last heartbeat response time.
    /// </summary>
    DateTime? LastHeartbeatResponse { get; }

    #endregion

    #region Hub Method Invocation

    /// <summary>
    /// Invokes a hub method with no return value.
    /// </summary>
    /// <param name="methodName">The method name.</param>
    /// <param name="args">The arguments.</param>
    Task InvokeAsync(string methodName, params object[] args);

    /// <summary>
    /// Invokes a hub method with a return value.
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    /// <param name="methodName">The method name.</param>
    /// <param name="args">The arguments.</param>
    /// <returns>The return value.</returns>
    Task<T?> InvokeAsync<T>(string methodName, params object[] args);

    #endregion

    #region Configuration

    /// <summary>
    /// Gets the current configuration.
    /// </summary>
    SignalRConfiguration Configuration { get; }

    /// <summary>
    /// Updates the configuration.
    /// </summary>
    /// <param name="configuration">The new configuration.</param>
    void UpdateConfiguration(SignalRConfiguration configuration);

    #endregion
}

/// <summary>
/// Factory for creating sync hub service instances.
/// </summary>
public interface ISyncHubServiceFactory
{
    /// <summary>
    /// Creates a new sync hub service instance.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <returns>A new sync hub service instance.</returns>
    ISyncHubService Create(SignalRConfiguration configuration);
}
