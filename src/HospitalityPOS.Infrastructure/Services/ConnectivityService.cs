using System.Net.NetworkInformation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Configuration options for the connectivity service.
/// </summary>
public class ConnectivityServiceOptions
{
    /// <summary>
    /// The section name in appsettings.json.
    /// </summary>
    public const string SectionName = "Connectivity";

    /// <summary>
    /// URL to ping for eTIMS connectivity check.
    /// </summary>
    public string EtimsEndpoint { get; set; } = "https://etims.kra.go.ke";

    /// <summary>
    /// URL to ping for M-Pesa connectivity check.
    /// </summary>
    public string MpesaEndpoint { get; set; } = "https://api.safaricom.co.ke";

    /// <summary>
    /// URL to ping for cloud backup connectivity check.
    /// </summary>
    public string CloudEndpoint { get; set; } = "https://api.hospitalitypos.co.ke";

    /// <summary>
    /// Fallback URL for general internet connectivity check.
    /// </summary>
    public string FallbackEndpoint { get; set; } = "https://www.google.com";

    /// <summary>
    /// Timeout in seconds for connectivity checks.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Default monitoring interval in seconds.
    /// </summary>
    public int DefaultIntervalSeconds { get; set; } = 30;
}

/// <summary>
/// Service for monitoring network connectivity status.
/// </summary>
public class ConnectivityService : IConnectivityService, IDisposable
{
    private readonly ILogger<ConnectivityService> _logger;
    private readonly HttpClient _httpClient;
    private readonly ConnectivityServiceOptions _options;
    private readonly object _lock = new();

    private Timer? _monitoringTimer;
    private ConnectivityStatus _currentStatus = ConnectivityStatus.Offline;
    private DateTime? _lastOnlineTime;
    private DateTime? _lastCheckTime;
    private bool _isMonitoring;
    private bool _disposed;

    /// <inheritdoc />
    public ConnectivityStatus CurrentStatus
    {
        get
        {
            lock (_lock)
            {
                return _currentStatus;
            }
        }
    }

    /// <inheritdoc />
    public DateTime? LastOnlineTime
    {
        get
        {
            lock (_lock)
            {
                return _lastOnlineTime;
            }
        }
    }

    /// <inheritdoc />
    public DateTime? LastCheckTime
    {
        get
        {
            lock (_lock)
            {
                return _lastCheckTime;
            }
        }
    }

    /// <inheritdoc />
    public bool IsMonitoring
    {
        get
        {
            lock (_lock)
            {
                return _isMonitoring;
            }
        }
    }

    /// <inheritdoc />
    public event EventHandler<ConnectivityChangedEventArgs>? StatusChanged;

    public ConnectivityService(
        ILogger<ConnectivityService> logger,
        IHttpClientFactory httpClientFactory,
        IOptions<ConnectivityServiceOptions>? options = null)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("Connectivity");
        _httpClient.Timeout = TimeSpan.FromSeconds(options?.Value.TimeoutSeconds ?? 10);
        _options = options?.Value ?? new ConnectivityServiceOptions();
    }

    /// <inheritdoc />
    public async Task<ConnectivityStatus> CheckConnectivityAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var serviceStatuses = await GetServiceStatusesAsync(cancellationToken);
            var newStatus = DetermineOverallStatus(serviceStatuses);

            UpdateStatus(newStatus);

            return newStatus;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking connectivity");
            UpdateStatus(ConnectivityStatus.Offline);
            return ConnectivityStatus.Offline;
        }
    }

    /// <inheritdoc />
    public void StartMonitoring(TimeSpan? interval = null)
    {
        lock (_lock)
        {
            if (_isMonitoring)
            {
                _logger.LogWarning("Connectivity monitoring is already running");
                return;
            }

            var checkInterval = interval ?? TimeSpan.FromSeconds(_options.DefaultIntervalSeconds);
            _monitoringTimer = new Timer(
                async _ => await MonitorCallback(),
                null,
                TimeSpan.Zero,
                checkInterval);

            _isMonitoring = true;
            _logger.LogInformation("Started connectivity monitoring with interval {Interval}", checkInterval);
        }
    }

    /// <inheritdoc />
    public void StopMonitoring()
    {
        lock (_lock)
        {
            if (!_isMonitoring)
            {
                return;
            }

            _monitoringTimer?.Dispose();
            _monitoringTimer = null;
            _isMonitoring = false;

            _logger.LogInformation("Stopped connectivity monitoring");
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsServiceReachableAsync(string serviceType, CancellationToken cancellationToken = default)
    {
        var endpoint = serviceType.ToLowerInvariant() switch
        {
            "etims" => _options.EtimsEndpoint,
            "mpesa" => _options.MpesaEndpoint,
            "cloud" => _options.CloudEndpoint,
            _ => _options.FallbackEndpoint
        };

        return await PingEndpointAsync(endpoint, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, bool>> GetServiceStatusesAsync(CancellationToken cancellationToken = default)
    {
        var tasks = new Dictionary<string, Task<bool>>
        {
            ["eTIMS"] = PingEndpointAsync(_options.EtimsEndpoint, cancellationToken),
            ["M-Pesa"] = PingEndpointAsync(_options.MpesaEndpoint, cancellationToken),
            ["Cloud"] = PingEndpointAsync(_options.CloudEndpoint, cancellationToken),
            ["Internet"] = PingEndpointAsync(_options.FallbackEndpoint, cancellationToken)
        };

        await Task.WhenAll(tasks.Values);

        return tasks.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Result);
    }

    private async Task<bool> PingEndpointAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error pinging endpoint {Url}", url);
            return false;
        }
    }

    private ConnectivityStatus DetermineOverallStatus(Dictionary<string, bool> serviceStatuses)
    {
        var reachableCount = serviceStatuses.Count(kvp => kvp.Value);

        if (reachableCount == 0)
        {
            return ConnectivityStatus.Offline;
        }

        if (reachableCount < serviceStatuses.Count)
        {
            return ConnectivityStatus.Degraded;
        }

        return ConnectivityStatus.Online;
    }

    private void UpdateStatus(ConnectivityStatus newStatus)
    {
        ConnectivityStatus previousStatus;

        lock (_lock)
        {
            previousStatus = _currentStatus;
            _lastCheckTime = DateTime.UtcNow;

            if (newStatus == _currentStatus)
            {
                if (newStatus == ConnectivityStatus.Online)
                {
                    _lastOnlineTime = DateTime.UtcNow;
                }
                return;
            }

            _currentStatus = newStatus;

            if (newStatus == ConnectivityStatus.Online)
            {
                _lastOnlineTime = DateTime.UtcNow;
            }
        }

        _logger.LogInformation(
            "Connectivity status changed from {PreviousStatus} to {NewStatus}",
            previousStatus,
            newStatus);

        StatusChanged?.Invoke(this, new ConnectivityChangedEventArgs
        {
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            Timestamp = DateTime.UtcNow,
            Message = $"Connectivity changed from {previousStatus} to {newStatus}"
        });
    }

    private async Task MonitorCallback()
    {
        try
        {
            await CheckConnectivityAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in connectivity monitoring callback");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        StopMonitoring();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
