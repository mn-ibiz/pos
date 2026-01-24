using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a receipt for payment processing.
/// </summary>
public class Receipt : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique receipt number (format: R-yyyyMMdd-sequence).
    /// </summary>
    public string ReceiptNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the associated order ID.
    /// </summary>
    public int? OrderId { get; set; }

    /// <summary>
    /// Gets or sets the work period ID.
    /// </summary>
    public int? WorkPeriodId { get; set; }

    /// <summary>
    /// Gets or sets the work period session ID (cashier session tracking).
    /// </summary>
    public int? WorkPeriodSessionId { get; set; }

    /// <summary>
    /// Gets or sets the terminal ID where the receipt was processed.
    /// </summary>
    public int? TerminalId { get; set; }

    /// <summary>
    /// Gets or sets the terminal code (denormalized for queries).
    /// </summary>
    public string? TerminalCode { get; set; }

    /// <summary>
    /// Gets or sets the store/branch ID for multi-branch operations.
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// Gets or sets the owner (user who created the receipt) ID.
    /// </summary>
    public int OwnerId { get; set; }

    /// <summary>
    /// Gets or sets the table number (denormalized from order).
    /// </summary>
    public string? TableNumber { get; set; }

    /// <summary>
    /// Gets or sets the customer name (denormalized from order).
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// Gets or sets the date/time of the receipt.
    /// </summary>
    public DateTime ReceiptDate { get; set; }

    /// <summary>
    /// Gets or sets the receipt status.
    /// </summary>
    public ReceiptStatus Status { get; set; } = ReceiptStatus.Pending;

    /// <summary>
    /// Gets or sets whether this receipt has been voided.
    /// </summary>
    public bool IsVoided { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public bool IsVoid { get => IsVoided; set => IsVoided = value; }

    /// <summary>
    /// Gets or sets whether this receipt has been fully paid.
    /// </summary>
    public bool IsPaid { get; set; }

    /// <summary>
    /// Gets or sets the subtotal before tax and discounts.
    /// </summary>
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Gets or sets the tax amount.
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Gets or sets the discount amount.
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Gets or sets the total amount (subtotal + tax - discount).
    /// </summary>
    public decimal TotalAmount { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public decimal Total { get => TotalAmount; set => TotalAmount = value; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public decimal SubTotal { get => Subtotal; set => Subtotal = value; }
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the amount paid.
    /// </summary>
    public decimal PaidAmount { get; set; }

    /// <summary>
    /// Gets or sets the change amount.
    /// </summary>
    public decimal ChangeAmount { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the receipt was voided.
    /// </summary>
    public DateTime? VoidedAt { get; set; }

    /// <summary>
    /// Gets or sets the user ID who voided the receipt.
    /// </summary>
    public int? VoidedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the reason for voiding.
    /// </summary>
    public string? VoidReason { get; set; }

    /// <summary>
    /// Gets or sets the parent receipt ID (for split receipts).
    /// </summary>
    public int? ParentReceiptId { get; set; }

    /// <summary>
    /// Gets or sets whether this receipt was created from a split.
    /// </summary>
    public bool IsSplit { get; set; }

    /// <summary>
    /// Gets or sets the split number (1, 2, 3...) for split receipts.
    /// </summary>
    public int? SplitNumber { get; set; }

    /// <summary>
    /// Gets or sets the type of split (Equal or ByItem).
    /// </summary>
    public SplitType? SplitType { get; set; }

    /// <summary>
    /// Gets or sets the receipt ID this was merged into.
    /// </summary>
    public int? MergedIntoReceiptId { get; set; }

    /// <summary>
    /// Gets or sets whether this receipt is the result of merging other receipts.
    /// </summary>
    public bool IsMerged { get; set; }

    /// <summary>
    /// Gets or sets the customer ID.
    /// </summary>
    public int? CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the loyalty member ID (if linked to loyalty program).
    /// </summary>
    public int? LoyaltyMemberId { get; set; }

    /// <summary>
    /// Gets or sets the points earned on this receipt.
    /// </summary>
    public decimal PointsEarned { get; set; }

    /// <summary>
    /// Gets or sets the points redeemed on this receipt.
    /// </summary>
    public decimal PointsRedeemed { get; set; }

    /// <summary>
    /// Gets or sets the discount amount from points redemption.
    /// </summary>
    public decimal PointsDiscountAmount { get; set; }

    /// <summary>
    /// Gets or sets the member's points balance after this transaction.
    /// </summary>
    public decimal? PointsBalanceAfter { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the receipt was settled.
    /// </summary>
    public DateTime? SettledAt { get; set; }

    /// <summary>
    /// Gets or sets the user ID who settled the receipt.
    /// </summary>
    public int? SettledByUserId { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the associated order.
    /// </summary>
    public virtual Order? Order { get; set; }

    /// <summary>
    /// Gets or sets the work period.
    /// </summary>
    public virtual WorkPeriod? WorkPeriod { get; set; }

    /// <summary>
    /// Gets or sets the work period session (cashier session).
    /// </summary>
    public virtual WorkPeriodSession? Session { get; set; }

    /// <summary>
    /// Gets or sets the terminal where the receipt was processed.
    /// </summary>
    public virtual Terminal? Terminal { get; set; }

    /// <summary>
    /// Gets or sets the store/branch.
    /// </summary>
    public virtual Store? Store { get; set; }

    /// <summary>
    /// Gets or sets the owner (user who created the receipt).
    /// </summary>
    public virtual User Owner { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user who voided the receipt.
    /// </summary>
    public virtual User? VoidedByUser { get; set; }

    /// <summary>
    /// Gets or sets the user who settled the receipt.
    /// </summary>
    public virtual User? SettledByUser { get; set; }

    /// <summary>
    /// Gets or sets the loyalty member (if linked to loyalty program).
    /// </summary>
    public virtual LoyaltyMember? LoyaltyMember { get; set; }

    /// <summary>
    /// Gets or sets the parent receipt (for split receipts).
    /// </summary>
    public virtual Receipt? ParentReceipt { get; set; }

    /// <summary>
    /// Gets or sets the receipt this was merged into.
    /// </summary>
    public virtual Receipt? MergedIntoReceipt { get; set; }

    /// <summary>
    /// Gets or sets the child receipts from splitting.
    /// </summary>
    public virtual ICollection<Receipt> SplitReceipts { get; set; } = new List<Receipt>();

    /// <summary>
    /// Gets or sets the receipts that were merged into this one.
    /// </summary>
    public virtual ICollection<Receipt> MergedFromReceipts { get; set; } = new List<Receipt>();

    /// <summary>
    /// Gets or sets the receipt line items.
    /// </summary>
    public virtual ICollection<ReceiptItem> ReceiptItems { get; set; } = new List<ReceiptItem>();

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public virtual ICollection<ReceiptItem> Items { get => ReceiptItems; }

    /// <summary>
    /// Gets or sets the payments applied to this receipt.
    /// </summary>
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
