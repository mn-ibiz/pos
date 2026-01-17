using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Interfaces;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// SignalR-based real-time KDS communication service.
/// </summary>
public class KdsHubService : IKdsHubService
{
    private readonly ILogger<KdsHubService> _logger;
    private HubConnection? _hubConnection;
    private KdsHubConfiguration _configuration;
    private KdsHubConnectionStatusDto _connectionStatus;
    private CancellationTokenSource? _reconnectCts;
    private CancellationTokenSource? _heartbeatCts;
    private bool _disposed;
    private int _registeredStationId;

    public KdsHubService(
        ILogger<KdsHubService> logger,
        KdsHubConfiguration? configuration = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? new KdsHubConfiguration();
        _connectionStatus = new KdsHubConnectionStatusDto
        {
            State = KdsHubConnectionState.Disconnected,
            ServerUrl = _configuration.HubUrl
        };
    }

    #region Properties

    public KdsHubConnectionStatusDto ConnectionStatus => _connectionStatus;

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public KdsHubConfiguration Configuration => _configuration;

    #endregion

    #region Events

    public event EventHandler<KdsHubConnectionStatusDto>? ConnectionStatusChanged;
    public event EventHandler<KdsOrderDto>? OrderReceived;
    public event EventHandler<KdsOrderDto>? OrderUpdated;
    public event EventHandler<KdsOrderStatusChangeEventArgs>? OrderStatusChanged;
    public event EventHandler<BumpOrderEventArgs>? OrderBumped;
    public event EventHandler<KdsOrderDto>? OrderRecalled;
    public event EventHandler<KdsOrderDto>? OrderVoided;
    public event EventHandler<AllCallMessageDto>? AllCallReceived;
    public event EventHandler<KdsStationStatusUpdateDto>? StationStatusChanged;
    public event EventHandler<string>? ErrorOccurred;

    #endregion

    #region Connection Management

    public async Task<bool> ConnectAsync()
    {
        if (string.IsNullOrEmpty(_configuration.HubUrl))
        {
            _logger.LogWarning("Cannot connect: KDS Hub URL is not configured");
            return false;
        }

        try
        {
            UpdateConnectionStatus(KdsHubConnectionState.Connecting);

            if (_hubConnection == null)
            {
                BuildHubConnection();
            }

            await _hubConnection!.StartAsync();

            UpdateConnectionStatus(KdsHubConnectionState.Connected, lastConnected: DateTime.UtcNow);
            _connectionStatus.ReconnectAttempts = 0;

            _logger.LogInformation("Connected to KDS SignalR hub at {Url}", _configuration.HubUrl);

            // Start heartbeat if enabled
            if (_configuration.EnableHeartbeat)
            {
                StartHeartbeatLoop();
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to KDS SignalR hub at {Url}", _configuration.HubUrl);
            UpdateConnectionStatus(KdsHubConnectionState.Disconnected, error: ex.Message);
            ErrorOccurred?.Invoke(this, $"Connection failed: {ex.Message}");
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        try
        {
            _reconnectCts?.Cancel();
            _heartbeatCts?.Cancel();

            if (_registeredStationId > 0)
            {
                await UnregisterStationAsync();
            }

            if (_hubConnection != null)
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
            }

            UpdateConnectionStatus(KdsHubConnectionState.Disconnected, disconnected: DateTime.UtcNow);
            _logger.LogInformation("Disconnected from KDS SignalR hub");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting from KDS SignalR hub");
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _reconnectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        await ConnectAsync();

        if (_configuration.AutoReconnect && !IsConnected)
        {
            _ = StartReconnectionLoopSafeAsync(_reconnectCts.Token);
        }
    }

    /// <summary>
    /// Wrapper for StartReconnectionLoopAsync with proper exception handling.
    /// </summary>
    private async Task StartReconnectionLoopSafeAsync(CancellationToken cancellationToken)
    {
        try
        {
            await StartReconnectionLoopAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation occurs
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in KDS reconnection loop");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await DisconnectAsync();
    }

    public void UpdateConfiguration(KdsHubConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _connectionStatus.ServerUrl = configuration.HubUrl;
    }

    private void BuildHubConnection()
    {
        var builder = new HubConnectionBuilder()
            .WithUrl(_configuration.HubUrl, options =>
            {
                if (!string.IsNullOrEmpty(_configuration.AccessToken))
                {
                    options.AccessTokenProvider = () => Task.FromResult(_configuration.AccessToken);
                }
            })
            .WithAutomaticReconnect(new KdsRetryPolicy(_configuration));

        _hubConnection = builder.Build();

        // Register connection event handlers
        _hubConnection.Closed += OnConnectionClosed;
        _hubConnection.Reconnecting += OnReconnecting;
        _hubConnection.Reconnected += OnReconnected;

        // Register hub method handlers
        RegisterHubHandlers();
    }

    private void RegisterHubHandlers()
    {
        if (_hubConnection == null) return;

        // Handle new order
        _hubConnection.On<KdsOrderDto>("NewOrder", order =>
        {
            _logger.LogDebug("Received new KDS order: {OrderNumber}", order.OrderNumber);
            OrderReceived?.Invoke(this, order);
        });

        // Handle order update
        _hubConnection.On<KdsOrderDto>("OrderUpdated", order =>
        {
            _logger.LogDebug("Received KDS order update: {OrderNumber}", order.OrderNumber);
            OrderUpdated?.Invoke(this, order);
        });

        // Handle order status change
        _hubConnection.On<KdsOrderStatusChangeEventArgs>("OrderStatusChanged", args =>
        {
            _logger.LogDebug("KDS order {Id} status changed: {Previous} -> {New}",
                args.KdsOrderId, args.PreviousStatus, args.NewStatus);
            OrderStatusChanged?.Invoke(this, args);
        });

        // Handle order bump
        _hubConnection.On<BumpOrderEventArgs>("OrderBumped", args =>
        {
            _logger.LogDebug("KDS order {Id} bumped at station {StationId}",
                args.KdsOrderId, args.StationId);
            OrderBumped?.Invoke(this, args);
        });

        // Handle order recall
        _hubConnection.On<KdsOrderDto>("OrderRecalled", order =>
        {
            _logger.LogDebug("KDS order {OrderNumber} recalled", order.OrderNumber);
            OrderRecalled?.Invoke(this, order);
        });

        // Handle order void
        _hubConnection.On<KdsOrderDto>("OrderVoided", order =>
        {
            _logger.LogDebug("KDS order {OrderNumber} voided", order.OrderNumber);
            OrderVoided?.Invoke(this, order);
        });

        // Handle all-call message
        _hubConnection.On<AllCallMessageDto>("AllCall", message =>
        {
            _logger.LogDebug("Received all-call message: {Message}", message.Message);
            AllCallReceived?.Invoke(this, message);
        });

        // Handle station status change
        _hubConnection.On<KdsStationStatusUpdateDto>("StationStatusChanged", status =>
        {
            _logger.LogDebug("Station {StationId} status changed to {Status}",
                status.StationId, status.Status);
            StationStatusChanged?.Invoke(this, status);
        });

        // Handle errors from hub
        _hubConnection.On<string>("Error", errorMessage =>
        {
            _logger.LogWarning("Received error from KDS hub: {Error}", errorMessage);
            ErrorOccurred?.Invoke(this, errorMessage);
        });

        // Handle heartbeat response
        _hubConnection.On("HeartbeatAck", () =>
        {
            _logger.LogTrace("Received heartbeat acknowledgment");
        });
    }

    private Task OnConnectionClosed(Exception? exception)
    {
        _logger.LogWarning(exception, "KDS SignalR connection closed");
        UpdateConnectionStatus(KdsHubConnectionState.Disconnected,
            disconnected: DateTime.UtcNow,
            error: exception?.Message);

        _heartbeatCts?.Cancel();

        if (_configuration.AutoReconnect && !_reconnectCts?.IsCancellationRequested == true)
        {
            _ = StartReconnectionLoopSafeAsync(_reconnectCts!.Token);
        }

        return Task.CompletedTask;
    }

    private Task OnReconnecting(Exception? exception)
    {
        _logger.LogInformation("KDS SignalR reconnecting...");
        _connectionStatus.ReconnectAttempts++;
        UpdateConnectionStatus(KdsHubConnectionState.Reconnecting, error: exception?.Message);
        return Task.CompletedTask;
    }

    private async Task OnReconnected(string? connectionId)
    {
        _logger.LogInformation("KDS SignalR reconnected with connection ID: {ConnectionId}", connectionId);
        UpdateConnectionStatus(KdsHubConnectionState.Connected, lastConnected: DateTime.UtcNow);
        _connectionStatus.ReconnectAttempts = 0;

        // Re-register station after reconnection
        if (_registeredStationId > 0)
        {
            await RegisterStationAsync(_registeredStationId, _configuration.StoreId);
        }

        // Restart heartbeat
        if (_configuration.EnableHeartbeat)
        {
            StartHeartbeatLoop();
        }
    }

    private async Task StartReconnectionLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested &&
               _connectionStatus.ReconnectAttempts < _configuration.MaxReconnectAttempts)
        {
            try
            {
                var delay = CalculateReconnectDelay(_connectionStatus.ReconnectAttempts);
                _logger.LogInformation("Attempting KDS reconnection in {Delay}s (attempt {Attempt}/{Max})",
                    delay, _connectionStatus.ReconnectAttempts + 1, _configuration.MaxReconnectAttempts);

                await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken);

                if (await ConnectAsync())
                {
                    // Re-register station
                    if (_registeredStationId > 0)
                    {
                        await RegisterStationAsync(_registeredStationId, _configuration.StoreId);
                    }
                    return;
                }

                _connectionStatus.ReconnectAttempts++;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "KDS reconnection attempt failed");
                _connectionStatus.ReconnectAttempts++;
            }
        }

        if (_connectionStatus.ReconnectAttempts >= _configuration.MaxReconnectAttempts)
        {
            _logger.LogError("Max KDS reconnection attempts reached. Giving up.");
            ErrorOccurred?.Invoke(this, "Max reconnection attempts reached");
        }
    }

    private int CalculateReconnectDelay(int attemptCount)
    {
        var baseDelay = _configuration.ReconnectDelaySeconds;
        var exponentialDelay = baseDelay * Math.Pow(2, attemptCount);
        var maxDelay = 120;
        var delay = Math.Min(exponentialDelay, maxDelay);
        var jitter = new Random().NextDouble() * 0.25 * delay;
        return (int)(delay + jitter);
    }

    private void UpdateConnectionStatus(
        KdsHubConnectionState state,
        DateTime? lastConnected = null,
        DateTime? disconnected = null,
        string? error = null)
    {
        _connectionStatus.State = state;
        if (lastConnected.HasValue) _connectionStatus.LastConnectedAt = lastConnected;
        if (disconnected.HasValue) _connectionStatus.DisconnectedAt = disconnected;
        if (error != null) _connectionStatus.LastError = error;
        _connectionStatus.ServerUrl = _configuration.HubUrl;
        _connectionStatus.StationId = _registeredStationId;

        ConnectionStatusChanged?.Invoke(this, _connectionStatus);
    }

    private void StartHeartbeatLoop()
    {
        _heartbeatCts?.Cancel();
        _heartbeatCts = new CancellationTokenSource();

        _ = RunHeartbeatLoopAsync(_heartbeatCts.Token);
    }

    /// <summary>
    /// Runs the heartbeat loop with proper exception handling.
    /// </summary>
    private async Task RunHeartbeatLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && IsConnected)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_configuration.HeartbeatIntervalSeconds), cancellationToken);
                    await SendHeartbeatAsync();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Heartbeat failed");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation occurs
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in heartbeat loop");
        }
    }

    #endregion

    #region Station Registration

    public async Task<bool> RegisterStationAsync(int stationId, int storeId)
    {
        if (!IsConnected)
        {
            _logger.LogWarning("Cannot register station: not connected");
            return false;
        }

        try
        {
            await _hubConnection!.InvokeAsync("RegisterStation", stationId, storeId);
            _registeredStationId = stationId;
            _connectionStatus.StationId = stationId;
            _logger.LogInformation("Registered KDS station {StationId} with hub", stationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register KDS station {StationId}", stationId);
            return false;
        }
    }

    public async Task UnregisterStationAsync()
    {
        if (!IsConnected || _registeredStationId == 0)
        {
            return;
        }

        try
        {
            await _hubConnection!.InvokeAsync("UnregisterStation", _registeredStationId);
            _logger.LogInformation("Unregistered KDS station {StationId}", _registeredStationId);
            _registeredStationId = 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unregister KDS station");
        }
    }

    public async Task UpdateStationStatusAsync(KdsStationStatusDto status)
    {
        if (!IsConnected)
        {
            _logger.LogWarning("Cannot update station status: not connected");
            return;
        }

        try
        {
            await _hubConnection!.InvokeAsync("UpdateStationStatus", _registeredStationId, status);
            _logger.LogDebug("Updated station {StationId} status to {Status}", _registeredStationId, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update station status");
        }
    }

    #endregion

    #region Order Operations

    public async Task NotifyPreparationStartedAsync(int kdsOrderId, int stationId)
    {
        if (!IsConnected)
        {
            _logger.LogWarning("Cannot notify preparation started: not connected");
            return;
        }

        try
        {
            await _hubConnection!.InvokeAsync("PreparationStarted", kdsOrderId, stationId);
            _logger.LogDebug("Notified preparation started for order {OrderId}", kdsOrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify preparation started for order {OrderId}", kdsOrderId);
        }
    }

    public async Task NotifyItemDoneAsync(int kdsOrderItemId)
    {
        if (!IsConnected)
        {
            _logger.LogWarning("Cannot notify item done: not connected");
            return;
        }

        try
        {
            await _hubConnection!.InvokeAsync("ItemDone", kdsOrderItemId);
            _logger.LogDebug("Notified item {ItemId} done", kdsOrderItemId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify item {ItemId} done", kdsOrderItemId);
        }
    }

    public async Task NotifyOrderBumpedAsync(int kdsOrderId, int stationId)
    {
        if (!IsConnected)
        {
            _logger.LogWarning("Cannot notify order bumped: not connected");
            return;
        }

        try
        {
            await _hubConnection!.InvokeAsync("OrderBumped", kdsOrderId, stationId);
            _logger.LogDebug("Notified order {OrderId} bumped at station {StationId}", kdsOrderId, stationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify order {OrderId} bumped", kdsOrderId);
        }
    }

    public async Task NotifyOrderRecalledAsync(int kdsOrderId, int stationId, string? reason)
    {
        if (!IsConnected)
        {
            _logger.LogWarning("Cannot notify order recalled: not connected");
            return;
        }

        try
        {
            await _hubConnection!.InvokeAsync("OrderRecalled", kdsOrderId, stationId, reason);
            _logger.LogDebug("Notified order {OrderId} recalled at station {StationId}", kdsOrderId, stationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify order {OrderId} recalled", kdsOrderId);
        }
    }

    public async Task<List<KdsOrderDto>> RequestStationOrdersAsync(int stationId)
    {
        if (!IsConnected)
        {
            _logger.LogWarning("Cannot request station orders: not connected");
            return new List<KdsOrderDto>();
        }

        try
        {
            var orders = await _hubConnection!.InvokeAsync<List<KdsOrderDto>>("GetStationOrders", stationId);
            _logger.LogDebug("Received {Count} orders for station {StationId}", orders?.Count ?? 0, stationId);
            return orders ?? new List<KdsOrderDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to request orders for station {StationId}", stationId);
            return new List<KdsOrderDto>();
        }
    }

    #endregion

    #region All-Call

    public async Task SendAllCallAsync(SendAllCallDto message)
    {
        if (!IsConnected)
        {
            _logger.LogWarning("Cannot send all-call: not connected");
            return;
        }

        try
        {
            await _hubConnection!.InvokeAsync("SendAllCall", message);
            _logger.LogInformation("Sent all-call message: {Message}", message.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send all-call message");
        }
    }

    public async Task DismissAllCallAsync(int messageId, int stationId)
    {
        if (!IsConnected)
        {
            _logger.LogWarning("Cannot dismiss all-call: not connected");
            return;
        }

        try
        {
            await _hubConnection!.InvokeAsync("DismissAllCall", messageId, stationId);
            _logger.LogDebug("Dismissed all-call message {MessageId} at station {StationId}", messageId, stationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dismiss all-call message {MessageId}", messageId);
        }
    }

    #endregion

    #region Heartbeat

    public async Task SendHeartbeatAsync()
    {
        if (!IsConnected)
        {
            return;
        }

        try
        {
            await _hubConnection!.InvokeAsync("Heartbeat", _registeredStationId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send heartbeat");
        }
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _reconnectCts?.Cancel();
            _reconnectCts?.Dispose();
            _heartbeatCts?.Cancel();
            _heartbeatCts?.Dispose();

            if (_hubConnection != null)
            {
                _hubConnection.Closed -= OnConnectionClosed;
                _hubConnection.Reconnecting -= OnReconnecting;
                _hubConnection.Reconnected -= OnReconnected;

                // Dispose async connection with synchronous wait since we're in Dispose
                // Use Task.Run to avoid deadlock in synchronization context
                try
                {
                    Task.Run(async () => await _hubConnection.DisposeAsync()).Wait(TimeSpan.FromSeconds(5));
                }
                catch (Exception)
                {
                    // Ignore errors during disposal - connection may already be closed
                }
            }
        }

        _disposed = true;
    }

    #endregion
}

/// <summary>
/// Retry policy for KDS SignalR automatic reconnection.
/// </summary>
internal class KdsRetryPolicy : IRetryPolicy
{
    private readonly KdsHubConfiguration _configuration;

    public KdsRetryPolicy(KdsHubConfiguration configuration)
    {
        _configuration = configuration;
    }

    public TimeSpan? NextRetryDelay(RetryContext retryContext)
    {
        if (retryContext.PreviousRetryCount >= _configuration.MaxReconnectAttempts)
        {
            return null;
        }

        var delay = _configuration.ReconnectDelaySeconds * Math.Pow(2, retryContext.PreviousRetryCount);
        var maxDelay = 120;
        return TimeSpan.FromSeconds(Math.Min(delay, maxDelay));
    }
}

/// <summary>
/// Factory for creating KdsHubService instances.
/// </summary>
public class KdsHubServiceFactory : IKdsHubServiceFactory
{
    private readonly ILogger<KdsHubService> _logger;

    public KdsHubServiceFactory(ILogger<KdsHubService> logger)
    {
        _logger = logger;
    }

    public IKdsHubService Create(KdsHubConfiguration configuration)
    {
        return new KdsHubService(_logger, configuration);
    }
}
