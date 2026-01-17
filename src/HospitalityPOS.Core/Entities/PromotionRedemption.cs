namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a redemption/usage of a promotion at a store.
/// </summary>
public class PromotionRedemption : BaseEntity
{
    public int PromotionId { get; set; }
    public int StoreId { get; set; }
    public int ReceiptId { get; set; }
    public int? ReceiptItemId { get; set; }

    /// <summary>
    /// The original price before discount.
    /// </summary>
    public decimal OriginalAmount { get; set; }

    /// <summary>
    /// The discount amount given.
    /// </summary>
    public decimal DiscountGiven { get; set; }

    /// <summary>
    /// The final amount after discount.
    /// </summary>
    public decimal FinalAmount { get; set; }

    /// <summary>
    /// Quantity of items to which the promotion was applied.
    /// </summary>
    public int QuantityApplied { get; set; } = 1;

    /// <summary>
    /// When the promotion was redeemed.
    /// </summary>
    public DateTime RedeemedAt { get; set; }

    /// <summary>
    /// Coupon code used (if applicable).
    /// </summary>
    public string? CouponCodeUsed { get; set; }

    /// <summary>
    /// Loyalty member ID if linked to customer.
    /// </summary>
    public int? LoyaltyMemberId { get; set; }

    /// <summary>
    /// User ID who processed the transaction.
    /// </summary>
    public int? ProcessedByUserId { get; set; }

    /// <summary>
    /// Whether this redemption was voided.
    /// </summary>
    public bool IsVoided { get; set; }

    /// <summary>
    /// When the redemption was voided.
    /// </summary>
    public DateTime? VoidedAt { get; set; }

    /// <summary>
    /// User ID who voided the redemption.
    /// </summary>
    public int? VoidedByUserId { get; set; }

    /// <summary>
    /// Reason for voiding.
    /// </summary>
    public string? VoidReason { get; set; }

    // Navigation properties
    public virtual CentralPromotion Promotion { get; set; } = null!;
    public virtual Store Store { get; set; } = null!;
    public virtual Receipt Receipt { get; set; } = null!;
    public virtual ReceiptItem? ReceiptItem { get; set; }
    public virtual LoyaltyMember? LoyaltyMember { get; set; }
    public virtual User? ProcessedByUser { get; set; }
    public virtual User? VoidedByUser { get; set; }
}
