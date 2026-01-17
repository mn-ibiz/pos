using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Models;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for splitting receipts into multiple child receipts.
/// </summary>
public interface IReceiptSplitService
{
    /// <summary>
    /// Splits a receipt equally among a specified number of ways.
    /// </summary>
    /// <param name="receiptId">The receipt ID to split.</param>
    /// <param name="numberOfWays">Number of ways to split (e.g., 2, 3, 4).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The split result with the created child receipts.</returns>
    Task<SplitResult> SplitReceiptEquallyAsync(
        int receiptId,
        int numberOfWays,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Splits a receipt by moving specific items to new receipts.
    /// </summary>
    /// <param name="receiptId">The receipt ID to split.</param>
    /// <param name="splitRequests">List of split requests specifying which items go where.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The split result with the created child receipts.</returns>
    Task<SplitResult> SplitReceiptByItemsAsync(
        int receiptId,
        List<SplitItemRequest> splitRequests,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a receipt can be split.
    /// </summary>
    /// <param name="receiptId">The receipt ID to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the receipt can be split; otherwise false with reason.</returns>
    Task<(bool CanSplit, string? Reason)> CanSplitReceiptAsync(
        int receiptId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the child receipts for a parent receipt that was split.
    /// </summary>
    /// <param name="parentReceiptId">The parent receipt ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of child receipts.</returns>
    Task<IEnumerable<Receipt>> GetSplitReceiptsAsync(
        int parentReceiptId,
        CancellationToken cancellationToken = default);
}
