using HospitalityPOS.Core.DTOs;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Configuration settings for KDS SignalR hub connection.
/// </summary>
public class KdsHubConfiguration
{
    public const string SectionName = "KdsHub";
    public string HubUrl { get; set; } = "https://kds.hospitalitypos.co.ke/hub";
    public string? AccessToken { get; set; }
    public int StationId { get; set; }
    public int StoreId { get; set; }
    public bool AutoReconnect { get; set; } = true;
    public int ReconnectDelaySeconds { get; set; } = 5;
    public int MaxReconnectAttempts { get; set; } = 10;
    public bool EnableHeartbeat { get; set; } = true;
    public int HeartbeatIntervalSeconds { get; set; } = 30;
}

/// <summary>
/// Connection state for KDS hub.
/// </summary>
public enum KdsHubConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting
}

/// <summary>
/// Status of KDS hub connection.
/// </summary>
public class KdsHubConnectionStatusDto
{
    public KdsHubConnectionState State { get; set; }
    public string? ServerUrl { get; set; }
    public DateTime? LastConnectedAt { get; set; }
    public DateTime? DisconnectedAt { get; set; }
    public int ReconnectAttempts { get; set; }
    public string? LastError { get; set; }
    public int StationId { get; set; }
}

/// <summary>
/// Service interface for KDS real-time SignalR communication.
/// </summary>
public interface IKdsHubService : IDisposable
{
    #region Properties

    /// <summary>
    /// Gets the current connection status.
    /// </summary>
    KdsHubConnectionStatusDto ConnectionStatus { get; }

    /// <summary>
    /// Gets whether the hub is currently connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Gets the current configuration.
    /// </summary>
    KdsHubConfiguration Configuration { get; }

    #endregion

    #region Events

    /// <summary>
    /// Raised when connection status changes.
    /// </summary>
    event EventHandler<KdsHubConnectionStatusDto>? ConnectionStatusChanged;

    /// <summary>
    /// Raised when a new order is received.
    /// </summary>
    event EventHandler<KdsOrderDto>? OrderReceived;

    /// <summary>
    /// Raised when an order is updated.
    /// </summary>
    event EventHandler<KdsOrderDto>? OrderUpdated;

    /// <summary>
    /// Raised when an order status changes.
    /// </summary>
    event EventHandler<KdsOrderStatusChangeEventArgs>? OrderStatusChanged;

    /// <summary>
    /// Raised when an order is bumped.
    /// </summary>
    event EventHandler<BumpOrderEventArgs>? OrderBumped;

    /// <summary>
    /// Raised when an order is recalled.
    /// </summary>
    event EventHandler<KdsOrderDto>? OrderRecalled;

    /// <summary>
    /// Raised when an order is voided.
    /// </summary>
    event EventHandler<KdsOrderDto>? OrderVoided;

    /// <summary>
    /// Raised when an all-call message is received.
    /// </summary>
    event EventHandler<AllCallMessageDto>? AllCallReceived;

    /// <summary>
    /// Raised when a station comes online/offline.
    /// </summary>
    event EventHandler<KdsStationStatusUpdateDto>? StationStatusChanged;

    /// <summary>
    /// Raised when an error occurs.
    /// </summary>
    event EventHandler<string>? ErrorOccurred;

    #endregion

    #region Connection Management

    /// <summary>
    /// Connects to the KDS hub.
    /// </summary>
    /// <returns>True if connection successful.</returns>
    Task<bool> ConnectAsync();

    /// <summary>
    /// Disconnects from the KDS hub.
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// Starts the hub service with automatic reconnection.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the hub service.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the hub configuration.
    /// </summary>
    void UpdateConfiguration(KdsHubConfiguration configuration);

    #endregion

    #region Station Registration

    /// <summary>
    /// Registers this station with the hub.
    /// </summary>
    /// <param name="stationId">The station ID.</param>
    /// <param name="storeId">The store ID.</param>
    Task<bool> RegisterStationAsync(int stationId, int storeId);

    /// <summary>
    /// Unregisters this station from the hub.
    /// </summary>
    Task UnregisterStationAsync();

    /// <summary>
    /// Updates station status (online/offline/paused).
    /// </summary>
    Task UpdateStationStatusAsync(KdsStationStatusDto status);

    #endregion

    #region Order Operations

    /// <summary>
    /// Notifies the hub that preparation has started.
    /// </summary>
    Task NotifyPreparationStartedAsync(int kdsOrderId, int stationId);

    /// <summary>
    /// Notifies the hub that an item is done.
    /// </summary>
    Task NotifyItemDoneAsync(int kdsOrderItemId);

    /// <summary>
    /// Notifies the hub that an order has been bumped.
    /// </summary>
    Task NotifyOrderBumpedAsync(int kdsOrderId, int stationId);

    /// <summary>
    /// Notifies the hub that an order has been recalled.
    /// </summary>
    Task NotifyOrderRecalledAsync(int kdsOrderId, int stationId, string? reason);

    /// <summary>
    /// Requests fresh order data for the station.
    /// </summary>
    Task<List<KdsOrderDto>> RequestStationOrdersAsync(int stationId);

    #endregion

    #region All-Call

    /// <summary>
    /// Sends an all-call message to specified stations.
    /// </summary>
    Task SendAllCallAsync(SendAllCallDto message);

    /// <summary>
    /// Dismisses an all-call message at this station.
    /// </summary>
    Task DismissAllCallAsync(int messageId, int stationId);

    #endregion

    #region Heartbeat

    /// <summary>
    /// Sends a heartbeat to maintain connection.
    /// </summary>
    Task SendHeartbeatAsync();

    #endregion
}

/// <summary>
/// DTO for station status updates.
/// </summary>
public class KdsStationStatusUpdateDto
{
    public int StationId { get; set; }
    public string StationName { get; set; } = string.Empty;
    public KdsStationStatusDto Status { get; set; }
    public DateTime Timestamp { get; set; }
    public int ActiveOrderCount { get; set; }
}

/// <summary>
/// Factory for creating KdsHubService instances.
/// </summary>
public interface IKdsHubServiceFactory
{
    /// <summary>
    /// Creates a KDS hub service with the specified configuration.
    /// </summary>
    IKdsHubService Create(KdsHubConfiguration configuration);
}
