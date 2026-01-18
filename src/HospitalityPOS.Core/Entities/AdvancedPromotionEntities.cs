namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Type of BOGO promotion.
/// </summary>
public enum BogoType
{
    /// <summary>Buy X Get Y Free (same product).</summary>
    BuyXGetYFree = 1,
    /// <summary>Buy X Get Y at % Off (same product).</summary>
    BuyXGetYAtPercentOff = 2,
    /// <summary>Buy X Get Y Free (different product).</summary>
    BuyXGetYDifferentFree = 3,
    /// <summary>Buy X Get Cheapest Free (in qualifying group).</summary>
    BuyXGetCheapestFree = 4
}

/// <summary>
/// Type of Mix and Match promotion.
/// </summary>
public enum MixMatchType
{
    /// <summary>Buy any X from group for fixed price.</summary>
    AnyXForFixedPrice = 1,
    /// <summary>Buy any X from group get % off.</summary>
    AnyXForPercentOff = 2,
    /// <summary>Buy X from group A, get Y from group B at discount.</summary>
    CrossGroupDiscount = 3
}

/// <summary>
/// Advanced BOGO promotion configuration.
/// </summary>
public class BogoPromotion : BaseEntity
{
    /// <summary>
    /// Reference to the parent promotion.
    /// </summary>
    public int PromotionId { get; set; }

    /// <summary>
    /// Type of BOGO promotion.
    /// </summary>
    public BogoType BogoType { get; set; } = BogoType.BuyXGetYFree;

    /// <summary>
    /// Quantity that must be purchased to qualify (the "Buy X" part).
    /// </summary>
    public int BuyQuantity { get; set; } = 1;

    /// <summary>
    /// Quantity that gets the discount/free (the "Get Y" part).
    /// </summary>
    public int GetQuantity { get; set; } = 1;

    /// <summary>
    /// Discount percentage on the "Get Y" items (0-100). 100 = free.
    /// </summary>
    public decimal DiscountPercentOnGetItems { get; set; } = 100;

    /// <summary>
    /// Maximum number of times this BOGO can be applied per transaction.
    /// </summary>
    public int? MaxApplicationsPerTransaction { get; set; }

    /// <summary>
    /// Product ID for the "Get" item if different from buy item.
    /// </summary>
    public int? GetProductId { get; set; }

    /// <summary>
    /// Category ID for the "Get" item if any product in category qualifies.
    /// </summary>
    public int? GetCategoryId { get; set; }

    /// <summary>
    /// Whether the cheapest item in the qualifying group is free.
    /// </summary>
    public bool CheapestItemFree { get; set; }

    // Navigation properties
    public virtual CentralPromotion Promotion { get; set; } = null!;
    public virtual Product? GetProduct { get; set; }
    public virtual Category? GetCategory { get; set; }
}

/// <summary>
/// Mix and Match promotion configuration.
/// </summary>
public class MixMatchPromotion : BaseEntity
{
    /// <summary>
    /// Reference to the parent promotion.
    /// </summary>
    public int PromotionId { get; set; }

    /// <summary>
    /// Type of Mix and Match promotion.
    /// </summary>
    public MixMatchType MixMatchType { get; set; } = MixMatchType.AnyXForFixedPrice;

    /// <summary>
    /// Number of items required from the qualifying group.
    /// </summary>
    public int RequiredQuantity { get; set; } = 3;

    /// <summary>
    /// Fixed price for the bundle (for AnyXForFixedPrice type).
    /// </summary>
    public decimal? FixedPrice { get; set; }

    /// <summary>
    /// Discount percentage (for AnyXForPercentOff type).
    /// </summary>
    public decimal? DiscountPercent { get; set; }

    /// <summary>
    /// Maximum number of times this can be applied per transaction.
    /// </summary>
    public int? MaxApplicationsPerTransaction { get; set; }

    // Navigation properties
    public virtual CentralPromotion Promotion { get; set; } = null!;
    public virtual ICollection<MixMatchGroup> Groups { get; set; } = new List<MixMatchGroup>();
}

/// <summary>
/// Group of products for Mix and Match promotions.
/// </summary>
public class MixMatchGroup : BaseEntity
{
    /// <summary>
    /// Reference to the Mix and Match promotion.
    /// </summary>
    public int MixMatchPromotionId { get; set; }

    /// <summary>
    /// Name of the group (e.g., "Beverages", "Snacks").
    /// </summary>
    public string GroupName { get; set; } = string.Empty;

    /// <summary>
    /// Group type: "A" for qualifying, "B" for reward in cross-group discounts.
    /// </summary>
    public string GroupType { get; set; } = "A";

    /// <summary>
    /// Minimum quantity from this group.
    /// </summary>
    public int MinQuantity { get; set; }

    /// <summary>
    /// Maximum quantity from this group (null = unlimited).
    /// </summary>
    public int? MaxQuantity { get; set; }

    // Navigation properties
    public virtual MixMatchPromotion MixMatchPromotion { get; set; } = null!;
    public virtual ICollection<MixMatchGroupProduct> Products { get; set; } = new List<MixMatchGroupProduct>();
    public virtual ICollection<MixMatchGroupCategory> Categories { get; set; } = new List<MixMatchGroupCategory>();
}

/// <summary>
/// Product in a Mix and Match group.
/// </summary>
public class MixMatchGroupProduct : BaseEntity
{
    public int MixMatchGroupId { get; set; }
    public int ProductId { get; set; }

    // Navigation properties
    public virtual MixMatchGroup MixMatchGroup { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}

/// <summary>
/// Category in a Mix and Match group (includes all products in category).
/// </summary>
public class MixMatchGroupCategory : BaseEntity
{
    public int MixMatchGroupId { get; set; }
    public int CategoryId { get; set; }

    // Navigation properties
    public virtual MixMatchGroup MixMatchGroup { get; set; } = null!;
    public virtual Category Category { get; set; } = null!;
}

/// <summary>
/// Quantity break pricing tier.
/// </summary>
public class QuantityBreakTier : BaseEntity
{
    /// <summary>
    /// Reference to the parent promotion.
    /// </summary>
    public int PromotionId { get; set; }

    /// <summary>
    /// Minimum quantity for this tier.
    /// </summary>
    public int MinQuantity { get; set; }

    /// <summary>
    /// Maximum quantity for this tier (null = unlimited).
    /// </summary>
    public int? MaxQuantity { get; set; }

    /// <summary>
    /// Price per unit at this tier.
    /// </summary>
    public decimal? UnitPrice { get; set; }

    /// <summary>
    /// Discount percentage at this tier.
    /// </summary>
    public decimal? DiscountPercent { get; set; }

    /// <summary>
    /// Display label (e.g., "Buy 3+ save 10%").
    /// </summary>
    public string? DisplayLabel { get; set; }

    // Navigation properties
    public virtual CentralPromotion Promotion { get; set; } = null!;
}

/// <summary>
/// Combo/Bundle deal configuration.
/// </summary>
public class ComboPromotion : BaseEntity
{
    /// <summary>
    /// Reference to the parent promotion.
    /// </summary>
    public int PromotionId { get; set; }

    /// <summary>
    /// Name of the combo (e.g., "Breakfast Combo").
    /// </summary>
    public string ComboName { get; set; } = string.Empty;

    /// <summary>
    /// Fixed price for the entire combo.
    /// </summary>
    public decimal ComboPrice { get; set; }

    /// <summary>
    /// Original total price before combo discount.
    /// </summary>
    public decimal OriginalTotalPrice { get; set; }

    /// <summary>
    /// Whether all items in the combo are required.
    /// </summary>
    public bool AllItemsRequired { get; set; } = true;

    /// <summary>
    /// Minimum items required if not all required.
    /// </summary>
    public int? MinItemsRequired { get; set; }

    /// <summary>
    /// Maximum times this combo can be added per transaction.
    /// </summary>
    public int? MaxPerTransaction { get; set; }

    /// <summary>
    /// PLU code for quick entry of this combo.
    /// </summary>
    public string? ComboPLU { get; set; }

    /// <summary>
    /// Image path for the combo display.
    /// </summary>
    public string? ImagePath { get; set; }

    // Navigation properties
    public virtual CentralPromotion Promotion { get; set; } = null!;
    public virtual ICollection<ComboItem> Items { get; set; } = new List<ComboItem>();
}

/// <summary>
/// Item in a combo deal.
/// </summary>
public class ComboItem : BaseEntity
{
    public int ComboPromotionId { get; set; }
    public int ProductId { get; set; }

    /// <summary>
    /// Quantity of this product in the combo.
    /// </summary>
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Whether this item is required in the combo.
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// If not required, this is an optional add-on price.
    /// </summary>
    public decimal? AddOnPrice { get; set; }

    /// <summary>
    /// Display order in the combo.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Category ID for substitution options.
    /// </summary>
    public int? SubstitutionCategoryId { get; set; }

    // Navigation properties
    public virtual ComboPromotion ComboPromotion { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
    public virtual Category? SubstitutionCategory { get; set; }
}

/// <summary>
/// Coupon entity for individual coupon tracking.
/// </summary>
public class Coupon : BaseEntity
{
    /// <summary>
    /// Reference to the parent promotion (null if standalone coupon).
    /// </summary>
    public int? PromotionId { get; set; }

    /// <summary>
    /// Unique coupon code.
    /// </summary>
    public string CouponCode { get; set; } = string.Empty;

    /// <summary>
    /// Coupon type.
    /// </summary>
    public CouponType CouponType { get; set; } = CouponType.SingleUse;

    /// <summary>
    /// Fixed discount amount.
    /// </summary>
    public decimal? DiscountAmount { get; set; }

    /// <summary>
    /// Discount percentage.
    /// </summary>
    public decimal? DiscountPercent { get; set; }

    /// <summary>
    /// Minimum purchase amount to use coupon.
    /// </summary>
    public decimal? MinimumPurchase { get; set; }

    /// <summary>
    /// Maximum discount amount (cap for percentage coupons).
    /// </summary>
    public decimal? MaxDiscountAmount { get; set; }

    /// <summary>
    /// Start date of coupon validity.
    /// </summary>
    public DateTime ValidFrom { get; set; }

    /// <summary>
    /// End date of coupon validity.
    /// </summary>
    public DateTime ValidTo { get; set; }

    /// <summary>
    /// Maximum total uses for this coupon.
    /// </summary>
    public int? MaxUses { get; set; }

    /// <summary>
    /// Current use count.
    /// </summary>
    public int UseCount { get; set; }

    /// <summary>
    /// Customer ID if coupon is customer-specific.
    /// </summary>
    public int? CustomerId { get; set; }

    /// <summary>
    /// Batch ID if coupon was generated in a batch.
    /// </summary>
    public int? BatchId { get; set; }

    /// <summary>
    /// Whether the coupon has been voided.
    /// </summary>
    public bool IsVoided { get; set; }

    /// <summary>
    /// Description shown to customer.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets whether the coupon is currently valid.
    /// </summary>
    public bool IsCurrentlyValid => IsActive
        && !IsVoided
        && DateTime.UtcNow >= ValidFrom
        && DateTime.UtcNow <= ValidTo
        && (MaxUses == null || UseCount < MaxUses);

    // Navigation properties
    public virtual CentralPromotion? Promotion { get; set; }
    public virtual LoyaltyMember? Customer { get; set; }
    public virtual CouponBatch? Batch { get; set; }
    public virtual ICollection<CouponRedemption> Redemptions { get; set; } = new List<CouponRedemption>();
}

/// <summary>
/// Type of coupon.
/// </summary>
public enum CouponType
{
    /// <summary>Single use coupon.</summary>
    SingleUse = 1,
    /// <summary>Multi-use coupon with limit.</summary>
    MultiUse = 2,
    /// <summary>Unlimited use promotional coupon.</summary>
    Unlimited = 3,
    /// <summary>Customer-specific coupon.</summary>
    CustomerSpecific = 4
}

/// <summary>
/// Batch of generated coupons.
/// </summary>
public class CouponBatch : BaseEntity
{
    /// <summary>
    /// Name/description of the batch.
    /// </summary>
    public string BatchName { get; set; } = string.Empty;

    /// <summary>
    /// Reference to the promotion.
    /// </summary>
    public int? PromotionId { get; set; }

    /// <summary>
    /// Prefix for coupon codes in this batch.
    /// </summary>
    public string CodePrefix { get; set; } = string.Empty;

    /// <summary>
    /// Number of coupons generated.
    /// </summary>
    public int TotalCoupons { get; set; }

    /// <summary>
    /// Number of coupons redeemed.
    /// </summary>
    public int RedeemedCount { get; set; }

    /// <summary>
    /// Date the batch was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who generated the batch.
    /// </summary>
    public int? GeneratedByUserId { get; set; }

    // Navigation properties
    public virtual CentralPromotion? Promotion { get; set; }
    public virtual User? GeneratedByUser { get; set; }
    public virtual ICollection<Coupon> Coupons { get; set; } = new List<Coupon>();
}

/// <summary>
/// Record of coupon redemption.
/// </summary>
public class CouponRedemption : BaseEntity
{
    public int CouponId { get; set; }
    public int ReceiptId { get; set; }

    /// <summary>
    /// Amount discounted by this coupon.
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Date and time of redemption.
    /// </summary>
    public DateTime RedeemedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who applied the coupon.
    /// </summary>
    public int? RedeemedByUserId { get; set; }

    // Navigation properties
    public virtual Coupon Coupon { get; set; } = null!;
    public virtual Receipt Receipt { get; set; } = null!;
    public virtual User? RedeemedByUser { get; set; }
}

/// <summary>
/// Automatic markdown schedule for products.
/// </summary>
public class AutomaticMarkdown : BaseEntity
{
    /// <summary>
    /// Reference to the product.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Category ID if applied to entire category.
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Name/description of the markdown rule.
    /// </summary>
    public string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// Type of markdown trigger.
    /// </summary>
    public MarkdownTriggerType TriggerType { get; set; }

    /// <summary>
    /// Time of day when markdown starts (for TimeOfDay trigger).
    /// </summary>
    public TimeSpan? TriggerTime { get; set; }

    /// <summary>
    /// Hours before closing when markdown starts (for BeforeClosing trigger).
    /// </summary>
    public int? HoursBeforeClosing { get; set; }

    /// <summary>
    /// Days before expiry when markdown starts (for NearExpiry trigger).
    /// </summary>
    public int? DaysBeforeExpiry { get; set; }

    /// <summary>
    /// Discount percentage to apply.
    /// </summary>
    public decimal DiscountPercent { get; set; }

    /// <summary>
    /// Final price override (instead of percentage).
    /// </summary>
    public decimal? FinalPrice { get; set; }

    /// <summary>
    /// Days of week when this markdown applies (JSON array).
    /// </summary>
    public string? ValidDaysOfWeek { get; set; }

    /// <summary>
    /// Priority when multiple markdowns apply (higher = first).
    /// </summary>
    public int Priority { get; set; } = 100;

    /// <summary>
    /// Whether to stack with other markdowns.
    /// </summary>
    public bool StackWithOtherMarkdowns { get; set; }

    // Navigation properties
    public virtual Product Product { get; set; } = null!;
    public virtual Category? Category { get; set; }
}

/// <summary>
/// Type of trigger for automatic markdown.
/// </summary>
public enum MarkdownTriggerType
{
    /// <summary>Triggered at specific time of day.</summary>
    TimeOfDay = 1,
    /// <summary>Triggered X hours before store closing.</summary>
    BeforeClosing = 2,
    /// <summary>Triggered X days before expiry (requires batch tracking).</summary>
    NearExpiry = 3,
    /// <summary>Triggered manually by staff.</summary>
    Manual = 4
}

/// <summary>
/// Record of promotion application to an order/receipt.
/// </summary>
public class PromotionApplication : BaseEntity
{
    /// <summary>
    /// Receipt this promotion was applied to.
    /// </summary>
    public int ReceiptId { get; set; }

    /// <summary>
    /// Promotion that was applied.
    /// </summary>
    public int PromotionId { get; set; }

    /// <summary>
    /// Total discount amount from this promotion.
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Number of times this promotion was applied in this receipt.
    /// </summary>
    public int ApplicationCount { get; set; } = 1;

    /// <summary>
    /// JSON details of how promotion was applied (items, quantities).
    /// </summary>
    public string? ApplicationDetails { get; set; }

    /// <summary>
    /// Date and time of application.
    /// </summary>
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Receipt Receipt { get; set; } = null!;
    public virtual CentralPromotion Promotion { get; set; } = null!;
}
