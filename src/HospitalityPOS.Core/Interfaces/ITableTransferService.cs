using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Models;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for table transfer operations.
/// </summary>
public interface ITableTransferService
{
    /// <summary>
    /// Transfers a single table to a new waiter.
    /// </summary>
    /// <param name="request">The transfer request details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The transfer result.</returns>
    Task<TransferResult> TransferTableAsync(
        TransferTableRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Transfers multiple tables to a new waiter (bulk transfer).
    /// </summary>
    /// <param name="request">The bulk transfer request details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The transfer result.</returns>
    Task<TransferResult> BulkTransferAsync(
        BulkTransferRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the transfer history for a specific table.
    /// </summary>
    /// <param name="tableId">The table ID.</param>
    /// <param name="limit">Maximum number of records to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of transfer log entries.</returns>
    Task<List<TableTransferLog>> GetTransferHistoryAsync(
        int tableId,
        int limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all tables currently assigned to a specific waiter.
    /// </summary>
    /// <param name="waiterId">The waiter's user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of tables assigned to the waiter.</returns>
    Task<List<Table>> GetTablesByWaiterAsync(
        int waiterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active waiters (users who can be assigned tables).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active waiter users.</returns>
    Task<List<User>> GetActiveWaitersAsync(CancellationToken cancellationToken = default);
}
