using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for managing cash payouts during work periods.
/// </summary>
public interface ICashPayoutService
{
    /// <summary>
    /// Records a cash payout for the specified work period.
    /// </summary>
    /// <param name="workPeriodId">The work period ID.</param>
    /// <param name="amount">The payout amount.</param>
    /// <param name="reason">The payout reason category.</param>
    /// <param name="userId">The user recording the payout.</param>
    /// <param name="customReason">Custom reason text if reason is Other.</param>
    /// <param name="reference">Optional reference number.</param>
    /// <param name="notes">Optional notes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created cash payout.</returns>
    Task<CashPayout> RecordPayoutAsync(
        int workPeriodId,
        decimal amount,
        PayoutReason reason,
        int userId,
        string? customReason = null,
        string? reference = null,
        string? notes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all payouts for a work period.
    /// </summary>
    /// <param name="workPeriodId">The work period ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of cash payouts.</returns>
    Task<IReadOnlyList<CashPayout>> GetPayoutsForWorkPeriodAsync(
        int workPeriodId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total approved payouts for a work period.
    /// </summary>
    /// <param name="workPeriodId">The work period ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The total payout amount.</returns>
    Task<decimal> GetTotalPayoutsAsync(
        int workPeriodId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a payout by ID.
    /// </summary>
    /// <param name="payoutId">The payout ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The payout if found.</returns>
    Task<CashPayout?> GetByIdAsync(int payoutId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves a pending payout.
    /// </summary>
    /// <param name="payoutId">The payout ID.</param>
    /// <param name="approverUserId">The approving user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if approved successfully.</returns>
    Task<bool> ApprovePayoutAsync(int payoutId, int approverUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects a pending payout.
    /// </summary>
    /// <param name="payoutId">The payout ID.</param>
    /// <param name="approverUserId">The rejecting user ID.</param>
    /// <param name="rejectionReason">The reason for rejection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if rejected successfully.</returns>
    Task<bool> RejectPayoutAsync(
        int payoutId,
        int approverUserId,
        string rejectionReason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a payout (only if pending and by the same user).
    /// </summary>
    /// <param name="payoutId">The payout ID.</param>
    /// <param name="userId">The requesting user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted successfully.</returns>
    Task<bool> DeletePayoutAsync(int payoutId, int userId, CancellationToken cancellationToken = default);
}
