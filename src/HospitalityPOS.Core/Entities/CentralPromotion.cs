namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Type of promotion.
/// </summary>
public enum PromotionType
{
    /// <summary>Percentage discount.</summary>
    PercentageDiscount = 1,
    /// <summary>Fixed amount discount.</summary>
    AmountDiscount = 2,
    /// <summary>Buy one get one free or similar.</summary>
    BOGO = 3,
    /// <summary>Bundle discount (buy together).</summary>
    Bundle = 4,
    /// <summary>Fixed price offer.</summary>
    FixedPrice = 5,
    /// <summary>Mix and match (any X items from group for fixed price).</summary>
    MixAndMatch = 6,
    /// <summary>Quantity break (tiered pricing based on quantity).</summary>
    QuantityBreak = 7,
    /// <summary>Combo deal (specific products together for special price).</summary>
    Combo = 8,
    /// <summary>Coupon-based promotion.</summary>
    Coupon = 9,
    /// <summary>Automatic markdown based on expiry date.</summary>
    AutoMarkdown = 10
}

/// <summary>
/// Status of a promotion.
/// </summary>
public enum PromotionStatus
{
    /// <summary>Promotion is in draft state.</summary>
    Draft = 0,
    /// <summary>Promotion is active.</summary>
    Active = 1,
    /// <summary>Promotion is scheduled for future.</summary>
    Scheduled = 2,
    /// <summary>Promotion has been paused.</summary>
    Paused = 3,
    /// <summary>Promotion has ended.</summary>
    Ended = 4,
    /// <summary>Promotion was cancelled.</summary>
    Cancelled = 5
}

/// <summary>
/// Represents a centrally managed promotion that can be deployed to stores.
/// </summary>
public class CentralPromotion : BaseEntity
{
    /// <summary>
    /// Unique code for the promotion.
    /// </summary>
    public string PromotionCode { get; set; } = string.Empty;

    /// <summary>
    /// Name of the promotion.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the promotion for customers.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Internal notes about the promotion.
    /// </summary>
    public string? InternalNotes { get; set; }

    /// <summary>
    /// Type of promotion.
    /// </summary>
    public PromotionType Type { get; set; } = PromotionType.PercentageDiscount;

    /// <summary>
    /// Fixed discount amount (for AmountDiscount type).
    /// </summary>
    public decimal? DiscountAmount { get; set; }

    /// <summary>
    /// Percentage discount (for PercentageDiscount type).
    /// </summary>
    public decimal? DiscountPercent { get; set; }

    /// <summary>
    /// Fixed offer price (for FixedPrice type).
    /// </summary>
    public decimal? OfferPrice { get; set; }

    /// <summary>
    /// Minimum purchase amount to qualify for promotion.
    /// </summary>
    public decimal? MinimumPurchase { get; set; }

    /// <summary>
    /// Minimum quantity required.
    /// </summary>
    public int MinQuantity { get; set; } = 1;

    /// <summary>
    /// Maximum quantity allowed per transaction.
    /// </summary>
    public int? MaxQuantityPerTransaction { get; set; }

    /// <summary>
    /// Maximum total redemptions for the promotion.
    /// </summary>
    public int? MaxTotalRedemptions { get; set; }

    /// <summary>
    /// Maximum redemptions per customer (if customer tracking enabled).
    /// </summary>
    public int? MaxRedemptionsPerCustomer { get; set; }

    /// <summary>
    /// Start date of the promotion.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date of the promotion.
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Current status of the promotion.
    /// </summary>
    public PromotionStatus Status { get; set; } = PromotionStatus.Draft;

    /// <summary>
    /// Days of the week when promotion is valid (JSON array of day numbers, 0=Sunday).
    /// </summary>
    public string? ValidDaysOfWeek { get; set; }

    /// <summary>
    /// Start time of day when promotion is valid.
    /// </summary>
    public TimeSpan? ValidFromTime { get; set; }

    /// <summary>
    /// End time of day when promotion is valid.
    /// </summary>
    public TimeSpan? ValidToTime { get; set; }

    /// <summary>
    /// Whether the promotion requires coupon code.
    /// </summary>
    public bool RequiresCouponCode { get; set; }

    /// <summary>
    /// Coupon code if required.
    /// </summary>
    public string? CouponCode { get; set; }

    /// <summary>
    /// Whether promotion is combinable with other promotions.
    /// </summary>
    public bool IsCombinableWithOtherPromotions { get; set; } = true;

    /// <summary>
    /// Priority for applying when multiple promotions match (higher = applied first).
    /// </summary>
    public int Priority { get; set; } = 100;

    /// <summary>
    /// Whether this promotion is created centrally and managed by HQ.
    /// </summary>
    public bool IsCentrallyManaged { get; set; } = true;

    /// <summary>
    /// User ID who created the promotion.
    /// </summary>
    public int? CreatedByUserId { get; set; }

    /// <summary>
    /// Gets whether the promotion is currently active based on dates and status.
    /// </summary>
    public bool IsCurrentlyActive => IsActive
        && Status == PromotionStatus.Active
        && DateTime.UtcNow >= StartDate
        && DateTime.UtcNow <= EndDate;

    /// <summary>
    /// Gets the computed status based on dates.
    /// </summary>
    public PromotionStatus ComputedStatus
    {
        get
        {
            if (!IsActive) return PromotionStatus.Cancelled;
            if (Status == PromotionStatus.Paused) return PromotionStatus.Paused;
            var now = DateTime.UtcNow;
            if (now < StartDate) return PromotionStatus.Scheduled;
            if (now > EndDate) return PromotionStatus.Ended;
            return PromotionStatus.Active;
        }
    }

    // Navigation properties
    public virtual User? CreatedByUser { get; set; }
    public virtual ICollection<PromotionProduct> Products { get; set; } = new List<PromotionProduct>();
    public virtual ICollection<PromotionCategory> Categories { get; set; } = new List<PromotionCategory>();
    public virtual ICollection<PromotionDeployment> Deployments { get; set; } = new List<PromotionDeployment>();
    public virtual ICollection<PromotionRedemption> Redemptions { get; set; } = new List<PromotionRedemption>();
}

/// <summary>
/// Links a promotion to specific products.
/// </summary>
public class PromotionProduct : BaseEntity
{
    public int PromotionId { get; set; }
    public int ProductId { get; set; }

    /// <summary>
    /// If true, this is a qualifying product (must buy). If false, discounted product.
    /// </summary>
    public bool IsQualifyingProduct { get; set; } = true;

    /// <summary>
    /// Required quantity if this is a qualifying product.
    /// </summary>
    public int RequiredQuantity { get; set; } = 1;

    // Navigation properties
    public virtual CentralPromotion Promotion { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}

/// <summary>
/// Links a promotion to categories (applies to all products in category).
/// </summary>
public class PromotionCategory : BaseEntity
{
    public int PromotionId { get; set; }
    public int CategoryId { get; set; }

    // Navigation properties
    public virtual CentralPromotion Promotion { get; set; } = null!;
    public virtual Category Category { get; set; } = null!;
}
