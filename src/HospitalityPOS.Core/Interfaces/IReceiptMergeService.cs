using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Models;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for merging multiple receipts into one.
/// </summary>
public interface IReceiptMergeService
{
    /// <summary>
    /// Merges multiple receipts into a single receipt.
    /// </summary>
    /// <param name="receiptIds">The IDs of receipts to merge.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The merge result with the created merged receipt.</returns>
    Task<MergeResult> MergeReceiptsAsync(
        List<int> receiptIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if receipts can be merged.
    /// </summary>
    /// <param name="receiptIds">The IDs of receipts to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the receipts can be merged; otherwise false with reason.</returns>
    Task<(bool CanMerge, string? Reason)> CanMergeReceiptsAsync(
        List<int> receiptIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all pending receipts available for merging.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of pending receipts that can be merged.</returns>
    Task<IEnumerable<Receipt>> GetMergeableReceiptsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the source receipts that were merged into a specific receipt.
    /// </summary>
    /// <param name="mergedReceiptId">The merged receipt ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of source receipts.</returns>
    Task<IEnumerable<Receipt>> GetSourceReceiptsAsync(
        int mergedReceiptId,
        CancellationToken cancellationToken = default);
}
