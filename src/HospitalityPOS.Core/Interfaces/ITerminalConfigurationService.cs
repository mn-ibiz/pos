using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Models;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for managing local terminal configuration files.
/// </summary>
public interface ITerminalConfigurationService
{
    /// <summary>
    /// Gets the current terminal configuration from the local file.
    /// Returns null if not configured.
    /// </summary>
    /// <returns>The terminal configuration, or null if not found.</returns>
    TerminalConfiguration? GetLocalConfiguration();

    /// <summary>
    /// Saves the terminal configuration to the local file.
    /// </summary>
    /// <param name="config">The configuration to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveLocalConfigurationAsync(
        TerminalConfiguration config,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that the local config matches the database record.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The validation result.</returns>
    Task<TerminalValidationResult> ValidateConfigurationAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if this machine is registered as a terminal.
    /// </summary>
    /// <returns>True if terminal is configured.</returns>
    bool IsTerminalConfigured();

    /// <summary>
    /// Gets the current terminal ID from local config.
    /// </summary>
    /// <returns>The terminal ID, or null if not configured.</returns>
    int? GetCurrentTerminalId();

    /// <summary>
    /// Gets the current terminal code from local config.
    /// </summary>
    /// <returns>The terminal code, or null if not configured.</returns>
    string? GetCurrentTerminalCode();

    /// <summary>
    /// Gets the configuration file path.
    /// </summary>
    /// <returns>The full path to the configuration file.</returns>
    string GetConfigurationFilePath();

    /// <summary>
    /// Deletes the local configuration for re-registration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteLocalConfigurationAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Syncs local config with the database record.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if sync was successful.</returns>
    Task<bool> SyncWithDatabaseAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reloads the configuration from the local file.
    /// </summary>
    void ReloadConfiguration();

    /// <summary>
    /// Updates the last sync timestamp.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateLastSyncAsync(
        CancellationToken cancellationToken = default);
}
