using HospitalityPOS.Core.DTOs;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for monitoring system health.
/// </summary>
public interface ISystemHealthService
{
    /// <summary>
    /// Gets the current system health status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The system health status.</returns>
    Task<SystemHealthDto> GetHealthStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the database connection is healthy.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if connected, false otherwise.</returns>
    Task<bool> CheckDatabaseConnectionAsync(CancellationToken cancellationToken = default);
}
