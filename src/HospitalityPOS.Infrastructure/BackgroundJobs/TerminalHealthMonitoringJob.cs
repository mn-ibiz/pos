using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.BackgroundJobs;

/// <summary>
/// Configuration options for terminal health monitoring.
/// </summary>
public class TerminalHealthMonitoringOptions
{
    public const string SectionName = "TerminalHealthMonitoring";

    /// <summary>
    /// Interval in seconds between health checks. Default is 30 seconds.
    /// </summary>
    public int IntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Timeout in seconds for considering a terminal offline. Default is 60 seconds.
    /// </summary>
    public int HeartbeatTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Whether to log status changes. Default is true.
    /// </summary>
    public bool LogStatusChanges { get; set; } = true;

    /// <summary>
    /// Maximum consecutive errors before applying extended backoff. Default is 5.
    /// </summary>
    public int MaxConsecutiveErrors { get; set; } = 5;
}

/// <summary>
/// Background job that monitors terminal health and detects offline terminals.
/// </summary>
public class TerminalHealthMonitoringJob : BackgroundService, ITerminalHealthService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TerminalHealthMonitoringJob> _logger;
    private readonly TerminalHealthMonitoringOptions _options;
    private readonly SemaphoreSlim _runLock = new(1, 1);

    private DateTime? _lastCheckTime;
    private int _consecutiveErrors;
    private volatile bool _isRunning;

    // Cache of last known terminal statuses for change detection
    private readonly Dictionary<int, TerminalStatus> _lastKnownStatuses = new();

    public bool IsRunning => _isRunning;

    public TerminalHealthMonitoringJob(
        IServiceProvider serviceProvider,
        ILogger<TerminalHealthMonitoringJob> logger,
        IOptions<TerminalHealthMonitoringOptions>? options = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options?.Value ?? new TerminalHealthMonitoringOptions();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "TerminalHealthMonitoringJob started with {Interval}s interval, {Timeout}s heartbeat timeout",
            _options.IntervalSeconds,
            _options.HeartbeatTimeoutSeconds);

        // Initial delay to let the application start up
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessHealthCheckAsync(stoppingToken);
                _consecutiveErrors = 0; // Reset on success
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during terminal health monitoring");
                _consecutiveErrors++;
            }

            // Calculate next delay with exponential backoff on errors
            var delay = CalculateNextDelay();
            await Task.Delay(delay, stoppingToken);
        }

        _logger.LogInformation("TerminalHealthMonitoringJob stopped");
    }

    public DateTime? GetLastCheckTime() => _lastCheckTime;

    public async Task<int> RunHealthCheckNowAsync(int? storeId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Manual terminal health check triggered for store {StoreId}", storeId ?? 0);
        return await ProcessHealthCheckAsync(cancellationToken, storeId);
    }

    public async Task<IReadOnlyList<TerminalHealthStatus>> GetAllTerminalHealthAsync(
        int storeId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<POSDbContext>();

        var terminals = await context.Terminals
            .AsNoTracking()
            .Where(t => t.StoreId == storeId)
            .ToListAsync(cancellationToken);

        return terminals.Select(t => MapToHealthStatus(t)).ToList();
    }

    public async Task<TerminalHealthStatus?> GetTerminalHealthAsync(
        int terminalId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<POSDbContext>();

        var terminal = await context.Terminals
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == terminalId, cancellationToken);

        return terminal is null ? null : MapToHealthStatus(terminal);
    }

    public async Task<IReadOnlyList<TerminalHealthStatus>> GetOfflineTerminalsAsync(
        int storeId,
        CancellationToken cancellationToken = default)
    {
        var allHealth = await GetAllTerminalHealthAsync(storeId, cancellationToken);
        return allHealth.Where(h => h.IsActive && !h.IsOnline).ToList();
    }

    public async Task<IReadOnlyList<TerminalHealthStatus>> GetTerminalsWithWarningsAsync(
        int storeId,
        CancellationToken cancellationToken = default)
    {
        var allHealth = await GetAllTerminalHealthAsync(storeId, cancellationToken);
        return allHealth.Where(h => h.HasWarnings).ToList();
    }

    public async Task<StoreTerminalHealthSummary> GetStoreHealthSummaryAsync(
        int storeId,
        CancellationToken cancellationToken = default)
    {
        var allHealth = await GetAllTerminalHealthAsync(storeId, cancellationToken);

        var totalActive = allHealth.Count(h => h.IsActive);
        var online = allHealth.Count(h => h.IsActive && h.IsOnline);
        var offline = allHealth.Count(h => h.IsActive && !h.IsOnline);
        var inactive = allHealth.Count(h => !h.IsActive);
        var withWarnings = allHealth.Count(h => h.HasWarnings);
        var withOpenWorkPeriods = allHealth.Count(h => h.IsWorkPeriodOpen);

        var healthPercentage = totalActive > 0 ? (online * 100.0 / totalActive) : 100.0;

        var overallStatus = healthPercentage switch
        {
            100 => "Healthy",
            >= 80 => "Good",
            >= 50 => "Degraded",
            > 0 => "Critical",
            _ => "No Terminals"
        };

        return new StoreTerminalHealthSummary
        {
            StoreId = storeId,
            TotalTerminals = allHealth.Count,
            OnlineTerminals = online,
            OfflineTerminals = offline,
            InactiveTerminals = inactive,
            TerminalsWithWarnings = withWarnings,
            TerminalsWithOpenWorkPeriods = withOpenWorkPeriods,
            HealthPercentage = healthPercentage,
            OverallStatus = overallStatus,
            Timestamp = DateTime.UtcNow
        };
    }

    private TimeSpan CalculateNextDelay()
    {
        var baseDelay = TimeSpan.FromSeconds(_options.IntervalSeconds);

        // Apply exponential backoff if there are consecutive errors
        if (_consecutiveErrors >= _options.MaxConsecutiveErrors)
        {
            var backoffMultiplier = Math.Min(Math.Pow(2, _consecutiveErrors - _options.MaxConsecutiveErrors + 1), 8);
            return TimeSpan.FromSeconds(baseDelay.TotalSeconds * backoffMultiplier);
        }

        return baseDelay;
    }

    private async Task<int> ProcessHealthCheckAsync(CancellationToken cancellationToken, int? filterStoreId = null)
    {
        // Prevent concurrent runs
        if (!await _runLock.WaitAsync(0, cancellationToken))
        {
            _logger.LogDebug("Health check already running, skipping this cycle");
            return 0;
        }

        try
        {
            _isRunning = true;

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<POSDbContext>();

            var query = context.Terminals.AsNoTracking().Where(t => t.IsActive);

            if (filterStoreId.HasValue)
            {
                query = query.Where(t => t.StoreId == filterStoreId.Value);
            }

            var terminals = await query.ToListAsync(cancellationToken);
            var checkedCount = 0;

            foreach (var terminal in terminals)
            {
                var currentStatus = DetermineTerminalStatus(terminal);

                // Check for status changes
                if (_options.LogStatusChanges && _lastKnownStatuses.TryGetValue(terminal.Id, out var previousStatus))
                {
                    if (currentStatus != previousStatus)
                    {
                        LogStatusChange(terminal.Code, terminal.Name, previousStatus, currentStatus);
                    }
                }

                _lastKnownStatuses[terminal.Id] = currentStatus;
                checkedCount++;
            }

            _lastCheckTime = DateTime.UtcNow;

            // Log summary
            var onlineCount = terminals.Count(t => IsTerminalOnline(t));
            var offlineCount = terminals.Count(t => !IsTerminalOnline(t));

            if (offlineCount > 0)
            {
                _logger.LogWarning(
                    "Terminal health check: {Online} online, {Offline} offline",
                    onlineCount, offlineCount);
            }
            else
            {
                _logger.LogDebug(
                    "Terminal health check completed: {Count} terminals all online",
                    checkedCount);
            }

            return checkedCount;
        }
        finally
        {
            _isRunning = false;
            _runLock.Release();
        }
    }

    private TerminalStatus DetermineTerminalStatus(Core.Entities.Terminal terminal)
    {
        if (!terminal.IsActive)
        {
            return TerminalStatus.Offline;
        }

        if (!terminal.LastHeartbeat.HasValue)
        {
            return TerminalStatus.Unknown;
        }

        var secondsSinceHeartbeat = (DateTime.UtcNow - terminal.LastHeartbeat.Value).TotalSeconds;

        if (secondsSinceHeartbeat <= _options.HeartbeatTimeoutSeconds)
        {
            return TerminalStatus.Online;
        }

        return TerminalStatus.Offline;
    }

    private bool IsTerminalOnline(Core.Entities.Terminal terminal)
    {
        return DetermineTerminalStatus(terminal) == TerminalStatus.Online;
    }

    private void LogStatusChange(string code, string name, TerminalStatus previous, TerminalStatus current)
    {
        if (current == TerminalStatus.Offline && previous == TerminalStatus.Online)
        {
            _logger.LogWarning(
                "Terminal '{Code}' ({Name}) went OFFLINE",
                code, name);
        }
        else if (current == TerminalStatus.Online && previous == TerminalStatus.Offline)
        {
            _logger.LogInformation(
                "Terminal '{Code}' ({Name}) came ONLINE",
                code, name);
        }
        else
        {
            _logger.LogInformation(
                "Terminal '{Code}' ({Name}) status changed from {Previous} to {Current}",
                code, name, previous, current);
        }
    }

    private TerminalHealthStatus MapToHealthStatus(Core.Entities.Terminal terminal)
    {
        var status = DetermineTerminalStatus(terminal);
        var isOnline = status == TerminalStatus.Online;

        var warnings = new List<string>();

        // Check for warnings (only if online)
        if (isOnline)
        {
            // Note: These would come from the last heartbeat data
            // For now, we just check basic status
        }

        // Check for configuration issues
        if (string.IsNullOrEmpty(terminal.MachineIdentifier))
        {
            warnings.Add("No machine identifier - terminal not bound to hardware");
        }

        int? secondsSinceHeartbeat = null;
        if (terminal.LastHeartbeat.HasValue)
        {
            secondsSinceHeartbeat = (int)(DateTime.UtcNow - terminal.LastHeartbeat.Value).TotalSeconds;
        }

        return new TerminalHealthStatus
        {
            TerminalId = terminal.Id,
            Code = terminal.Code,
            Name = terminal.Name,
            TerminalType = terminal.TerminalType,
            StoreId = terminal.StoreId,
            Status = status,
            StatusText = GetStatusText(status),
            IsOnline = isOnline,
            IsActive = terminal.IsActive,
            LastHeartbeat = terminal.LastHeartbeat,
            SecondsSinceLastHeartbeat = secondsSinceHeartbeat,
            IpAddress = terminal.IpAddress,
            Warnings = warnings
        };
    }

    private static string GetStatusText(TerminalStatus status) => status switch
    {
        TerminalStatus.Online => "Online",
        TerminalStatus.Offline => "Offline",
        TerminalStatus.Maintenance => "Maintenance",
        TerminalStatus.Error => "Error",
        _ => "Unknown"
    };

    public override void Dispose()
    {
        _runLock.Dispose();
        base.Dispose();
    }
}
