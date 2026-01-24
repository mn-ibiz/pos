namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a cash payout/withdrawal from the cash drawer during a work period.
/// </summary>
public class CashPayout : BaseEntity
{
    /// <summary>
    /// Gets or sets the work period ID.
    /// </summary>
    public int WorkPeriodId { get; set; }

    /// <summary>
    /// Gets or sets the payout amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the reason category for the payout.
    /// </summary>
    public PayoutReason Reason { get; set; }

    /// <summary>
    /// Gets or sets the custom reason text if Reason is Other.
    /// </summary>
    public string? CustomReason { get; set; }

    /// <summary>
    /// Gets or sets an optional reference number (receipt, invoice, etc.).
    /// </summary>
    public string? Reference { get; set; }

    /// <summary>
    /// Gets or sets additional notes about the payout.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who recorded the payout.
    /// </summary>
    public int RecordedByUserId { get; set; }

    /// <summary>
    /// Gets or sets when the payout was recorded.
    /// </summary>
    public DateTime RecordedAt { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who approved the payout.
    /// </summary>
    public int? ApprovedByUserId { get; set; }

    /// <summary>
    /// Gets or sets when the payout was approved.
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// Gets or sets the payout status.
    /// </summary>
    public PayoutStatus Status { get; set; } = PayoutStatus.Approved; // Default to approved for simplicity

    /// <summary>
    /// Gets or sets the rejection reason if status is Rejected.
    /// </summary>
    public string? RejectionReason { get; set; }

    // Navigation properties
    public virtual WorkPeriod WorkPeriod { get; set; } = null!;
    public virtual User RecordedByUser { get; set; } = null!;
    public virtual User? ApprovedByUser { get; set; }
}

/// <summary>
/// Payout reason categories.
/// </summary>
public enum PayoutReason
{
    /// <summary>
    /// Petty cash for small expenses.
    /// </summary>
    PettyCash = 0,

    /// <summary>
    /// Safe drop - removing excess cash for secure storage.
    /// </summary>
    SafeDrop = 1,

    /// <summary>
    /// Cash refund to customer.
    /// </summary>
    CustomerRefund = 2,

    /// <summary>
    /// Cash payment to supplier.
    /// </summary>
    SupplierPayment = 3,

    /// <summary>
    /// Float adjustment.
    /// </summary>
    FloatAdjustment = 4,

    /// <summary>
    /// Bank deposit preparation.
    /// </summary>
    BankDeposit = 5,

    /// <summary>
    /// Other reason (requires CustomReason).
    /// </summary>
    Other = 99
}

/// <summary>
/// Payout approval status.
/// </summary>
public enum PayoutStatus
{
    /// <summary>
    /// Pending approval.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Approved by manager.
    /// </summary>
    Approved = 1,

    /// <summary>
    /// Rejected by manager.
    /// </summary>
    Rejected = 2
}
