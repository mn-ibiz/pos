using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for terminal health monitoring and status tracking.
/// </summary>
public interface ITerminalHealthService
{
    /// <summary>
    /// Gets the health status of all terminals in a store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of terminal health statuses.</returns>
    Task<IReadOnlyList<TerminalHealthStatus>> GetAllTerminalHealthAsync(
        int storeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the health status of a specific terminal.
    /// </summary>
    /// <param name="terminalId">The terminal ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The terminal health status.</returns>
    Task<TerminalHealthStatus?> GetTerminalHealthAsync(
        int terminalId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets terminals that are currently offline (no heartbeat in timeout period).
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of offline terminals.</returns>
    Task<IReadOnlyList<TerminalHealthStatus>> GetOfflineTerminalsAsync(
        int storeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets terminals with health warnings (e.g., printer unavailable, cash drawer issues).
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of terminals with warnings.</returns>
    Task<IReadOnlyList<TerminalHealthStatus>> GetTerminalsWithWarningsAsync(
        int storeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the overall health summary for a store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The store health summary.</returns>
    Task<StoreTerminalHealthSummary> GetStoreHealthSummaryAsync(
        int storeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Manually triggers a health check on all terminals.
    /// </summary>
    /// <param name="storeId">Optional store ID to check (null = all stores).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of terminals checked.</returns>
    Task<int> RunHealthCheckNowAsync(int? storeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the time of the last health check.
    /// </summary>
    DateTime? GetLastCheckTime();

    /// <summary>
    /// Gets whether a health check is currently running.
    /// </summary>
    bool IsRunning { get; }
}

/// <summary>
/// Terminal health status information.
/// </summary>
public class TerminalHealthStatus
{
    /// <summary>
    /// Gets or sets the terminal ID.
    /// </summary>
    public int TerminalId { get; set; }

    /// <summary>
    /// Gets or sets the terminal code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the terminal name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the terminal type.
    /// </summary>
    public TerminalType TerminalType { get; set; }

    /// <summary>
    /// Gets or sets the store ID.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the overall status.
    /// </summary>
    public TerminalStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the status text description.
    /// </summary>
    public string StatusText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the terminal is online.
    /// </summary>
    public bool IsOnline { get; set; }

    /// <summary>
    /// Gets or sets whether the terminal is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the last heartbeat timestamp.
    /// </summary>
    public DateTime? LastHeartbeat { get; set; }

    /// <summary>
    /// Gets or sets the seconds since last heartbeat.
    /// </summary>
    public int? SecondsSinceLastHeartbeat { get; set; }

    /// <summary>
    /// Gets or sets the current IP address.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the current logged-in user.
    /// </summary>
    public string? CurrentUserName { get; set; }

    /// <summary>
    /// Gets or sets whether a work period is open.
    /// </summary>
    public bool IsWorkPeriodOpen { get; set; }

    /// <summary>
    /// Gets or sets whether the printer is available.
    /// </summary>
    public bool IsPrinterAvailable { get; set; }

    /// <summary>
    /// Gets or sets whether the cash drawer is available.
    /// </summary>
    public bool IsCashDrawerAvailable { get; set; }

    /// <summary>
    /// Gets or sets the application version.
    /// </summary>
    public string? AppVersion { get; set; }

    /// <summary>
    /// Gets or sets the list of health warnings.
    /// </summary>
    public List<string> Warnings { get; set; } = [];

    /// <summary>
    /// Gets whether this terminal has any warnings.
    /// </summary>
    public bool HasWarnings => Warnings.Count > 0;
}

/// <summary>
/// Summary of terminal health for a store.
/// </summary>
public class StoreTerminalHealthSummary
{
    /// <summary>
    /// Gets or sets the store ID.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the total number of terminals.
    /// </summary>
    public int TotalTerminals { get; set; }

    /// <summary>
    /// Gets or sets the number of online terminals.
    /// </summary>
    public int OnlineTerminals { get; set; }

    /// <summary>
    /// Gets or sets the number of offline terminals.
    /// </summary>
    public int OfflineTerminals { get; set; }

    /// <summary>
    /// Gets or sets the number of inactive terminals.
    /// </summary>
    public int InactiveTerminals { get; set; }

    /// <summary>
    /// Gets or sets the number of terminals with warnings.
    /// </summary>
    public int TerminalsWithWarnings { get; set; }

    /// <summary>
    /// Gets or sets the number of terminals with open work periods.
    /// </summary>
    public int TerminalsWithOpenWorkPeriods { get; set; }

    /// <summary>
    /// Gets or sets the overall health percentage (0-100).
    /// </summary>
    public double HealthPercentage { get; set; }

    /// <summary>
    /// Gets or sets the overall health status.
    /// </summary>
    public string OverallStatus { get; set; } = "Unknown";

    /// <summary>
    /// Gets or sets the timestamp of this summary.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
