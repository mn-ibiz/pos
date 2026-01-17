using HospitalityPOS.Core.DTOs;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Represents the network connectivity status.
/// </summary>
public enum ConnectivityStatus
{
    /// <summary>
    /// Full network connectivity available.
    /// </summary>
    Online,

    /// <summary>
    /// Limited connectivity - some services may be unavailable.
    /// </summary>
    Degraded,

    /// <summary>
    /// No network connectivity.
    /// </summary>
    Offline
}

/// <summary>
/// Event args for connectivity status changes.
/// </summary>
public class ConnectivityChangedEventArgs : EventArgs
{
    /// <summary>
    /// The previous connectivity status.
    /// </summary>
    public ConnectivityStatus PreviousStatus { get; init; }

    /// <summary>
    /// The new connectivity status.
    /// </summary>
    public ConnectivityStatus NewStatus { get; init; }

    /// <summary>
    /// When the status change was detected.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Optional message describing the status change.
    /// </summary>
    public string? Message { get; init; }
}

/// <summary>
/// Service interface for monitoring network connectivity status.
/// Provides real-time connectivity monitoring for eTIMS, M-Pesa, and cloud services.
/// </summary>
public interface IConnectivityService
{
    /// <summary>
    /// Gets the current connectivity status.
    /// </summary>
    ConnectivityStatus CurrentStatus { get; }

    /// <summary>
    /// Gets the last time the system was confirmed online.
    /// </summary>
    DateTime? LastOnlineTime { get; }

    /// <summary>
    /// Gets the last time connectivity was checked.
    /// </summary>
    DateTime? LastCheckTime { get; }

    /// <summary>
    /// Gets whether the service is currently monitoring connectivity.
    /// </summary>
    bool IsMonitoring { get; }

    /// <summary>
    /// Event raised when connectivity status changes.
    /// </summary>
    event EventHandler<ConnectivityChangedEventArgs>? StatusChanged;

    /// <summary>
    /// Checks the current connectivity status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current connectivity status.</returns>
    Task<ConnectivityStatus> CheckConnectivityAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts periodic connectivity monitoring.
    /// </summary>
    /// <param name="interval">The interval between checks. Default is 30 seconds.</param>
    void StartMonitoring(TimeSpan? interval = null);

    /// <summary>
    /// Stops connectivity monitoring.
    /// </summary>
    void StopMonitoring();

    /// <summary>
    /// Checks if a specific service endpoint is reachable.
    /// </summary>
    /// <param name="serviceType">The type of service to check (eTIMS, MPesa, Cloud).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the service is reachable.</returns>
    Task<bool> IsServiceReachableAsync(string serviceType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed connectivity information for all monitored services.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary of service names to their reachability status.</returns>
    Task<Dictionary<string, bool>> GetServiceStatusesAsync(CancellationToken cancellationToken = default);
}
