using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Interfaces;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// SignalR-based real-time synchronization service.
/// </summary>
public class SyncHubService : ISyncHubService
{
    private readonly ILogger<SyncHubService> _logger;
    private readonly ISyncQueueService _syncQueueService;
    private HubConnection? _hubConnection;
    private SignalRConfiguration _configuration;
    private SignalRConnectionStatusDto _connectionStatus;
    private CancellationTokenSource? _reconnectCts;
    private bool _disposed;
    private DateTime? _lastHeartbeatResponse;

    public SyncHubService(
        ILogger<SyncHubService> logger,
        ISyncQueueService syncQueueService,
        SignalRConfiguration? configuration = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _syncQueueService = syncQueueService ?? throw new ArgumentNullException(nameof(syncQueueService));
        _configuration = configuration ?? new SignalRConfiguration();
        _connectionStatus = new SignalRConnectionStatusDto
        {
            State = SignalRConnectionState.Disconnected,
            ServerUrl = _configuration.HubUrl
        };
    }

    #region Properties

    /// <inheritdoc />
    public SignalRConnectionStatusDto ConnectionStatus => _connectionStatus;

    /// <inheritdoc />
    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    /// <inheritdoc />
    public DateTime? LastHeartbeatResponse => _lastHeartbeatResponse;

    /// <inheritdoc />
    public SignalRConfiguration Configuration => _configuration;

    #endregion

    #region Events

    /// <inheritdoc />
    public event EventHandler<SignalRConnectionStatusDto>? ConnectionStatusChanged;

    /// <inheritdoc />
    public event EventHandler<SignalRSyncItemDto>? SyncItemReceived;

    /// <inheritdoc />
    public event EventHandler<SignalRSyncProgressDto>? SyncProgressChanged;

    /// <inheritdoc />
    public event EventHandler<string>? ErrorOccurred;

    #endregion

    #region Connection Management

    /// <inheritdoc />
    public async Task<bool> ConnectAsync()
    {
        if (string.IsNullOrEmpty(_configuration.HubUrl))
        {
            _logger.LogWarning("Cannot connect: Hub URL is not configured");
            return false;
        }

        try
        {
            UpdateConnectionStatus(SignalRConnectionState.Connecting);

            if (_hubConnection == null)
            {
                BuildHubConnection();
            }

            await _hubConnection!.StartAsync();

            UpdateConnectionStatus(SignalRConnectionState.Connected, lastConnected: DateTime.UtcNow);
            _connectionStatus.ReconnectAttempts = 0;

            _logger.LogInformation("Connected to SignalR hub at {Url}", _configuration.HubUrl);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to SignalR hub at {Url}", _configuration.HubUrl);
            UpdateConnectionStatus(SignalRConnectionState.Disconnected, error: ex.Message);
            ErrorOccurred?.Invoke(this, $"Connection failed: {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task DisconnectAsync()
    {
        try
        {
            _reconnectCts?.Cancel();

            if (_hubConnection != null)
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
            }

            UpdateConnectionStatus(SignalRConnectionState.Disconnected, disconnected: DateTime.UtcNow);
            _logger.LogInformation("Disconnected from SignalR hub");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting from SignalR hub");
        }
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _reconnectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        await ConnectAsync();

        if (_configuration.AutoReconnect && !IsConnected)
        {
            _ = StartReconnectionLoopAsync(_reconnectCts.Token);
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await DisconnectAsync();
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
            .WithAutomaticReconnect(new SignalRRetryPolicy(_configuration));

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

        // Handle incoming sync items
        _hubConnection.On<SignalRSyncItemDto>("ReceiveSyncItem", item =>
        {
            _logger.LogDebug("Received sync item: {EntityType}:{EntityId}", item.EntityType, item.EntityId);
            SyncItemReceived?.Invoke(this, item);
        });

        // Handle sync progress updates
        _hubConnection.On<SignalRSyncProgressDto>("SyncProgress", progress =>
        {
            SyncProgressChanged?.Invoke(this, progress);
        });

        // Handle heartbeat response
        _hubConnection.On("HeartbeatResponse", () =>
        {
            _lastHeartbeatResponse = DateTime.UtcNow;
        });

        // Handle errors from hub
        _hubConnection.On<string>("Error", errorMessage =>
        {
            _logger.LogWarning("Received error from hub: {Error}", errorMessage);
            ErrorOccurred?.Invoke(this, errorMessage);
        });
    }

    private Task OnConnectionClosed(Exception? exception)
    {
        _logger.LogWarning(exception, "SignalR connection closed");
        UpdateConnectionStatus(SignalRConnectionState.Disconnected,
            disconnected: DateTime.UtcNow,
            error: exception?.Message);

        if (_configuration.AutoReconnect && !_reconnectCts?.IsCancellationRequested == true)
        {
            _ = StartReconnectionLoopAsync(_reconnectCts!.Token);
        }

        return Task.CompletedTask;
    }

    private Task OnReconnecting(Exception? exception)
    {
        _logger.LogInformation("SignalR reconnecting...");
        _connectionStatus.ReconnectAttempts++;
        UpdateConnectionStatus(SignalRConnectionState.Reconnecting, error: exception?.Message);
        return Task.CompletedTask;
    }

    private Task OnReconnected(string? connectionId)
    {
        _logger.LogInformation("SignalR reconnected with connection ID: {ConnectionId}", connectionId);
        UpdateConnectionStatus(SignalRConnectionState.Connected, lastConnected: DateTime.UtcNow);
        _connectionStatus.ReconnectAttempts = 0;

        // Sync pending items after reconnection
        _ = SyncPendingItemsAsync();

        return Task.CompletedTask;
    }

    private async Task StartReconnectionLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested &&
               _connectionStatus.ReconnectAttempts < _configuration.MaxReconnectAttempts)
        {
            try
            {
                var delay = CalculateReconnectDelay(_connectionStatus.ReconnectAttempts);
                _logger.LogInformation("Attempting reconnection in {Delay}s (attempt {Attempt}/{Max})",
                    delay, _connectionStatus.ReconnectAttempts + 1, _configuration.MaxReconnectAttempts);

                await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken);

                if (await ConnectAsync())
                {
                    return; // Successfully reconnected
                }

                _connectionStatus.ReconnectAttempts++;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reconnection attempt failed");
                _connectionStatus.ReconnectAttempts++;
            }
        }

        if (_connectionStatus.ReconnectAttempts >= _configuration.MaxReconnectAttempts)
        {
            _logger.LogError("Max reconnection attempts reached. Giving up.");
            ErrorOccurred?.Invoke(this, "Max reconnection attempts reached");
        }
    }

    private int CalculateReconnectDelay(int attemptCount)
    {
        // Exponential backoff with jitter
        var baseDelay = _configuration.ReconnectDelaySeconds;
        var exponentialDelay = baseDelay * Math.Pow(2, attemptCount);
        var maxDelay = 120; // 2 minutes max
        var delay = Math.Min(exponentialDelay, maxDelay);

        // Add jitter (0-25% of delay)
        var jitter = new Random().NextDouble() * 0.25 * delay;
        return (int)(delay + jitter);
    }

    private void UpdateConnectionStatus(
        SignalRConnectionState state,
        DateTime? lastConnected = null,
        DateTime? disconnected = null,
        string? error = null)
    {
        _connectionStatus.State = state;
        if (lastConnected.HasValue) _connectionStatus.LastConnectedAt = lastConnected;
        if (disconnected.HasValue) _connectionStatus.DisconnectedAt = disconnected;
        if (error != null) _connectionStatus.LastError = error;
        _connectionStatus.ServerUrl = _configuration.HubUrl;

        ConnectionStatusChanged?.Invoke(this, _connectionStatus);
    }

    #endregion

    #region Sync Operations

    /// <inheritdoc />
    public async Task<bool> SendSyncItemAsync(SignalRSyncItemDto item)
    {
        if (!IsConnected)
        {
            _logger.LogWarning("Cannot send sync item: not connected");
            return false;
        }

        try
        {
            await _hubConnection!.InvokeAsync("SendSyncItem", item);
            _logger.LogDebug("Sent sync item: {EntityType}:{EntityId}", item.EntityType, item.EntityId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send sync item: {EntityType}:{EntityId}", item.EntityType, item.EntityId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<int> SendBatchAsync(IEnumerable<SignalRSyncItemDto> items)
    {
        if (!IsConnected)
        {
            _logger.LogWarning("Cannot send batch: not connected");
            return 0;
        }

        var sentCount = 0;
        var itemList = items.ToList();

        foreach (var batch in itemList.Chunk(_configuration.BatchSize))
        {
            try
            {
                await _hubConnection!.InvokeAsync("SendSyncBatch", batch);
                sentCount += batch.Length;

                SyncProgressChanged?.Invoke(this, new SignalRSyncProgressDto
                {
                    TotalItems = itemList.Count,
                    CompletedItems = sentCount,
                    FailedItems = 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send batch of {Count} items", batch.Length);
            }
        }

        return sentCount;
    }

    /// <inheritdoc />
    public async Task<SignalRBatchSyncResponseDto?> RequestBatchSyncAsync(SignalRBatchSyncRequestDto request)
    {
        if (!IsConnected)
        {
            _logger.LogWarning("Cannot request batch sync: not connected");
            return null;
        }

        try
        {
            var response = await _hubConnection!.InvokeAsync<SignalRBatchSyncResponseDto>(
                "RequestBatchSync", request);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to request batch sync");
            return new SignalRBatchSyncResponseDto
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<SignalRSyncProgressDto> SyncPendingItemsAsync()
    {
        var progress = new SignalRSyncProgressDto();

        if (!IsConnected)
        {
            _logger.LogWarning("Cannot sync pending items: not connected");
            return progress;
        }

        try
        {
            // Get pending items from queue
            var pendingItems = await _syncQueueService.GetPendingItemsAsync(_configuration.BatchSize);
            progress.TotalItems = pendingItems.Count;

            if (pendingItems.Count == 0)
            {
                _logger.LogDebug("No pending items to sync");
                return progress;
            }

            _logger.LogInformation("Syncing {Count} pending items", pendingItems.Count);

            foreach (var item in pendingItems)
            {
                try
                {
                    progress.CurrentItem = $"{item.EntityType}:{item.EntityId}";

                    var syncItem = new SignalRSyncItemDto
                    {
                        EntityType = item.EntityType,
                        EntityId = item.EntityId,
                        Operation = item.OperationType.ToString(),
                        Payload = item.Payload ?? string.Empty,
                        Timestamp = item.CreatedAt,
                        CorrelationId = Guid.NewGuid().ToString()
                    };

                    if (await SendSyncItemAsync(syncItem))
                    {
                        await _syncQueueService.MarkAsCompletedAsync(item.Id);
                        progress.CompletedItems++;
                    }
                    else
                    {
                        await _syncQueueService.MarkAsFailedAsync(item.Id, "Failed to send to hub");
                        progress.FailedItems++;
                    }

                    SyncProgressChanged?.Invoke(this, progress);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to sync item {Id}", item.Id);
                    await _syncQueueService.MarkAsFailedAsync(item.Id, ex.Message);
                    progress.FailedItems++;
                }
            }

            _logger.LogInformation("Sync complete: {Completed} succeeded, {Failed} failed",
                progress.CompletedItems, progress.FailedItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing pending items");
            ErrorOccurred?.Invoke(this, $"Sync failed: {ex.Message}");
        }

        return progress;
    }

    /// <inheritdoc />
    public async Task SendAcknowledgmentAsync(SignalRSyncAckDto ack)
    {
        if (!IsConnected)
        {
            _logger.LogWarning("Cannot send acknowledgment: not connected");
            return;
        }

        try
        {
            await _hubConnection!.InvokeAsync("Acknowledge", ack);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send acknowledgment for {CorrelationId}", ack.CorrelationId);
        }
    }

    #endregion

    #region Heartbeat

    /// <inheritdoc />
    public async Task SendHeartbeatAsync(SignalRHeartbeatDto heartbeat)
    {
        if (!IsConnected)
        {
            return;
        }

        try
        {
            await _hubConnection!.InvokeAsync("Heartbeat", heartbeat);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send heartbeat");
        }
    }

    #endregion

    #region Hub Method Invocation

    /// <inheritdoc />
    public async Task InvokeAsync(string methodName, params object[] args)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Not connected to hub");
        }

        await _hubConnection!.InvokeCoreAsync(methodName, args);
    }

    /// <inheritdoc />
    public async Task<T?> InvokeAsync<T>(string methodName, params object[] args)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Not connected to hub");
        }

        return await _hubConnection!.InvokeCoreAsync<T>(methodName, args);
    }

    #endregion

    #region Configuration

    /// <inheritdoc />
    public void UpdateConfiguration(SignalRConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _connectionStatus.ServerUrl = configuration.HubUrl;
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

            if (_hubConnection != null)
            {
                _hubConnection.Closed -= OnConnectionClosed;
                _hubConnection.Reconnecting -= OnReconnecting;
                _hubConnection.Reconnected -= OnReconnected;
                _ = _hubConnection.DisposeAsync();
            }
        }

        _disposed = true;
    }

    #endregion
}

/// <summary>
/// Retry policy for SignalR automatic reconnection.
/// </summary>
internal class SignalRRetryPolicy : IRetryPolicy
{
    private readonly SignalRConfiguration _configuration;

    public SignalRRetryPolicy(SignalRConfiguration configuration)
    {
        _configuration = configuration;
    }

    public TimeSpan? NextRetryDelay(RetryContext retryContext)
    {
        if (retryContext.PreviousRetryCount >= _configuration.MaxReconnectAttempts)
        {
            return null; // Stop retrying
        }

        // Exponential backoff
        var delay = _configuration.ReconnectDelaySeconds * Math.Pow(2, retryContext.PreviousRetryCount);
        var maxDelay = 120; // 2 minutes max
        return TimeSpan.FromSeconds(Math.Min(delay, maxDelay));
    }
}

/// <summary>
/// Factory for creating SyncHubService instances.
/// </summary>
public class SyncHubServiceFactory : ISyncHubServiceFactory
{
    private readonly ILogger<SyncHubService> _logger;
    private readonly ISyncQueueService _syncQueueService;

    public SyncHubServiceFactory(
        ILogger<SyncHubService> logger,
        ISyncQueueService syncQueueService)
    {
        _logger = logger;
        _syncQueueService = syncQueueService;
    }

    /// <inheritdoc />
    public ISyncHubService Create(SignalRConfiguration configuration)
    {
        return new SyncHubService(_logger, _syncQueueService, configuration);
    }
}
