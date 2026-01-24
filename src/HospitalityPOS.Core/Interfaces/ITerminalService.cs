using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for managing POS terminals.
/// </summary>
public interface ITerminalService
{
    #region CRUD Operations

    /// <summary>
    /// Creates a new terminal.
    /// </summary>
    /// <param name="request">The terminal creation request.</param>
    /// <param name="createdByUserId">The ID of the user creating the terminal.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created terminal.</returns>
    Task<Terminal> CreateTerminalAsync(
        CreateTerminalRequest request,
        int createdByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a terminal by ID.
    /// </summary>
    /// <param name="terminalId">The terminal ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The terminal if found; otherwise, null.</returns>
    Task<Terminal?> GetTerminalByIdAsync(
        int terminalId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a terminal by code within a store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="code">The terminal code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The terminal if found; otherwise, null.</returns>
    Task<Terminal?> GetTerminalByCodeAsync(
        int storeId,
        string code,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a terminal by its machine identifier.
    /// </summary>
    /// <param name="machineIdentifier">The machine identifier (MAC address or GUID).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The terminal if found; otherwise, null.</returns>
    Task<Terminal?> GetTerminalByMachineIdAsync(
        string machineIdentifier,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all terminals for a store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of terminals for the store.</returns>
    Task<IReadOnlyList<Terminal>> GetTerminalsByStoreAsync(
        int storeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a terminal.
    /// </summary>
    /// <param name="terminalId">The terminal ID.</param>
    /// <param name="request">The update request.</param>
    /// <param name="modifiedByUserId">The ID of the user modifying the terminal.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated terminal.</returns>
    Task<Terminal> UpdateTerminalAsync(
        int terminalId,
        UpdateTerminalRequest request,
        int modifiedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a terminal (soft delete).
    /// </summary>
    /// <param name="terminalId">The terminal ID.</param>
    /// <param name="userId">The ID of the user performing the action.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deactivated successfully.</returns>
    Task<bool> DeactivateTerminalAsync(
        int terminalId,
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reactivates a previously deactivated terminal.
    /// </summary>
    /// <param name="terminalId">The terminal ID.</param>
    /// <param name="userId">The ID of the user performing the action.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if reactivated successfully.</returns>
    Task<bool> ReactivateTerminalAsync(
        int terminalId,
        int userId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Validation

    /// <summary>
    /// Checks if a terminal code is unique within a store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="code">The terminal code to check.</param>
    /// <param name="excludeId">Optional terminal ID to exclude (for updates).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the code is unique.</returns>
    Task<bool> IsTerminalCodeUniqueAsync(
        int storeId,
        string code,
        int? excludeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a machine identifier is available.
    /// </summary>
    /// <param name="machineIdentifier">The machine identifier to check.</param>
    /// <param name="excludeId">Optional terminal ID to exclude (for updates).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the machine identifier is available.</returns>
    Task<bool> IsMachineIdentifierAvailableAsync(
        string machineIdentifier,
        int? excludeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a terminal's configuration and status.
    /// </summary>
    /// <param name="terminalId">The terminal ID.</param>
    /// <param name="machineIdentifier">Optional machine identifier to verify.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The validation result.</returns>
    Task<TerminalValidationResult> ValidateTerminalAsync(
        int terminalId,
        string? machineIdentifier = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Registration

    /// <summary>
    /// Registers a new terminal with hardware binding.
    /// </summary>
    /// <param name="request">The registration request.</param>
    /// <param name="createdByUserId">The ID of the user registering the terminal.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The registered terminal.</returns>
    Task<Terminal> RegisterTerminalAsync(
        TerminalRegistrationRequest request,
        int createdByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Binds a machine identifier to a terminal.
    /// </summary>
    /// <param name="terminalId">The terminal ID.</param>
    /// <param name="machineIdentifier">The machine identifier to bind.</param>
    /// <param name="userId">The ID of the user performing the action.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if binding was successful.</returns>
    Task<bool> BindMachineAsync(
        int terminalId,
        string machineIdentifier,
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unbinds the machine identifier from a terminal.
    /// </summary>
    /// <param name="terminalId">The terminal ID.</param>
    /// <param name="userId">The ID of the user performing the action.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if unbinding was successful.</returns>
    Task<bool> UnbindMachineAsync(
        int terminalId,
        int userId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Status

    /// <summary>
    /// Updates the heartbeat for a terminal.
    /// </summary>
    /// <param name="terminalId">The terminal ID.</param>
    /// <param name="heartbeat">The heartbeat data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if update was successful.</returns>
    Task<bool> UpdateHeartbeatAsync(
        int terminalId,
        TerminalHeartbeat heartbeat,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of a terminal.
    /// </summary>
    /// <param name="terminalId">The terminal ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The terminal status.</returns>
    Task<TerminalStatusDto?> GetTerminalStatusAsync(
        int terminalId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of all terminals in a store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of terminal statuses.</returns>
    Task<IReadOnlyList<TerminalStatusDto>> GetAllTerminalStatusesAsync(
        int storeId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Queries

    /// <summary>
    /// Gets unassigned terminals (without machine identifier).
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of unassigned terminals.</returns>
    Task<IReadOnlyList<Terminal>> GetUnassignedTerminalsAsync(
        int storeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active terminals for a store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active terminals.</returns>
    Task<IReadOnlyList<Terminal>> GetActiveTerminalsAsync(
        int storeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the next terminal number for auto-generating codes.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="type">The terminal type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The next available number.</returns>
    Task<int> GetNextTerminalNumberAsync(
        int storeId,
        TerminalType type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates the next available terminal code.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="type">The terminal type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated terminal code (e.g., REG-001).</returns>
    Task<string> GenerateTerminalCodeAsync(
        int storeId,
        TerminalType type,
        CancellationToken cancellationToken = default);

    #endregion
}
