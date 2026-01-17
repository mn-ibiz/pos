using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Models;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for receipt void operations.
/// </summary>
public interface IReceiptVoidService
{
    /// <summary>
    /// Voids a receipt with the specified request details.
    /// </summary>
    /// <param name="request">The void request containing receipt ID, reason, and notes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The void result indicating success or failure.</returns>
    Task<VoidResult> VoidReceiptAsync(VoidRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a receipt can be voided.
    /// </summary>
    /// <param name="receiptId">The receipt ID to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple indicating if void is possible and reason if not.</returns>
    Task<(bool CanVoid, string? Reason)> CanVoidReceiptAsync(int receiptId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active void reasons.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active void reasons ordered by display order.</returns>
    Task<IEnumerable<VoidReason>> GetVoidReasonsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets void records for a specific receipt.
    /// </summary>
    /// <param name="receiptId">The receipt ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The void record if the receipt was voided.</returns>
    Task<ReceiptVoid?> GetVoidRecordAsync(int receiptId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets void report for a date range.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of void report items.</returns>
    Task<IEnumerable<VoidReportItem>> GetVoidReportAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets total voided amount for a work period.
    /// </summary>
    /// <param name="workPeriodId">The work period ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The total voided amount.</returns>
    Task<decimal> GetTotalVoidedAmountAsync(int workPeriodId, CancellationToken cancellationToken = default);
}
