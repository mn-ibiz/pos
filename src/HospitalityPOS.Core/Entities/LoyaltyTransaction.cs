using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a loyalty points transaction (earning or redemption).
/// </summary>
public class LoyaltyTransaction : BaseEntity
{
    /// <summary>
    /// Gets or sets the loyalty member ID.
    /// </summary>
    public int LoyaltyMemberId { get; set; }

    /// <summary>
    /// Gets or sets the associated receipt ID (if applicable).
    /// </summary>
    public int? ReceiptId { get; set; }

    /// <summary>
    /// Gets or sets the transaction type.
    /// </summary>
    public LoyaltyTransactionType TransactionType { get; set; }

    /// <summary>
    /// Gets or sets the points amount (positive for earned, negative for redeemed).
    /// </summary>
    public decimal Points { get; set; }

    /// <summary>
    /// Gets or sets the monetary value associated with this transaction.
    /// For earnings: the spend amount that generated the points.
    /// For redemptions: the discount value applied.
    /// </summary>
    public decimal MonetaryValue { get; set; }

    /// <summary>
    /// Gets or sets the points balance after this transaction.
    /// </summary>
    public decimal BalanceAfter { get; set; }

    /// <summary>
    /// Gets or sets the transaction description/reason.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the reference number (e.g., receipt number).
    /// </summary>
    public string? ReferenceNumber { get; set; }

    /// <summary>
    /// Gets or sets bonus points (if any were applied).
    /// </summary>
    public decimal BonusPoints { get; set; }

    /// <summary>
    /// Gets or sets the bonus multiplier applied (1.0 = no bonus).
    /// </summary>
    public decimal BonusMultiplier { get; set; } = 1.0m;

    /// <summary>
    /// Gets or sets the transaction timestamp.
    /// </summary>
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the user who processed this transaction.
    /// </summary>
    public int ProcessedByUserId { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the loyalty member.
    /// </summary>
    public virtual LoyaltyMember LoyaltyMember { get; set; } = null!;

    /// <summary>
    /// Gets or sets the associated receipt.
    /// </summary>
    public virtual Receipt? Receipt { get; set; }

    /// <summary>
    /// Gets or sets the user who processed this transaction.
    /// </summary>
    public virtual User ProcessedByUser { get; set; } = null!;
}
