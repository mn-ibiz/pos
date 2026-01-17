using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for managing advanced promotions including BOGO, Mix &amp; Match, Combos, and Coupons.
/// </summary>
public interface IAdvancedPromotionService
{
    #region BOGO Promotions

    /// <summary>
    /// Creates a new BOGO promotion.
    /// </summary>
    /// <param name="promotion">The promotion details.</param>
    /// <param name="bogoConfig">The BOGO configuration.</param>
    /// <param name="productIds">Products that qualify for this BOGO.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created promotion with BOGO configuration.</returns>
    Task<CentralPromotion> CreateBogoPromotionAsync(
        CentralPromotion promotion,
        BogoPromotion bogoConfig,
        IEnumerable<int> productIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets BOGO configuration for a promotion.
    /// </summary>
    /// <param name="promotionId">The promotion ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The BOGO configuration if exists.</returns>
    Task<BogoPromotion?> GetBogoConfigurationAsync(int promotionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates BOGO discount for given order items.
    /// </summary>
    /// <param name="items">The order items to evaluate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>BOGO discount calculation result.</returns>
    Task<BogoCalculationResult> CalculateBogoDiscountAsync(
        IEnumerable<OrderItemInfo> items,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active BOGO promotions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of active BOGO promotions.</returns>
    Task<IEnumerable<BogoPromotion>> GetActiveBogoPromotionsAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Mix & Match Promotions

    /// <summary>
    /// Creates a new Mix &amp; Match promotion.
    /// </summary>
    /// <param name="promotion">The promotion details.</param>
    /// <param name="mixMatchConfig">The Mix &amp; Match configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created promotion with Mix &amp; Match configuration.</returns>
    Task<CentralPromotion> CreateMixMatchPromotionAsync(
        CentralPromotion promotion,
        MixMatchPromotion mixMatchConfig,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets Mix &amp; Match configuration for a promotion.
    /// </summary>
    /// <param name="promotionId">The promotion ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The Mix &amp; Match configuration if exists.</returns>
    Task<MixMatchPromotion?> GetMixMatchConfigurationAsync(int promotionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates Mix &amp; Match discount for given order items.
    /// </summary>
    /// <param name="items">The order items to evaluate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Mix &amp; Match discount calculation result.</returns>
    Task<MixMatchCalculationResult> CalculateMixMatchDiscountAsync(
        IEnumerable<OrderItemInfo> items,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds products to a Mix &amp; Match group.
    /// </summary>
    /// <param name="groupId">The group ID.</param>
    /// <param name="productIds">Product IDs to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddProductsToMixMatchGroupAsync(int groupId, IEnumerable<int> productIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds categories to a Mix &amp; Match group.
    /// </summary>
    /// <param name="groupId">The group ID.</param>
    /// <param name="categoryIds">Category IDs to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddCategoriesToMixMatchGroupAsync(int groupId, IEnumerable<int> categoryIds, CancellationToken cancellationToken = default);

    #endregion

    #region Quantity Break Pricing

    /// <summary>
    /// Creates quantity break tiers for a promotion.
    /// </summary>
    /// <param name="promotionId">The promotion ID.</param>
    /// <param name="tiers">The quantity break tiers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CreateQuantityBreakTiersAsync(int promotionId, IEnumerable<QuantityBreakTier> tiers, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets quantity break tiers for a promotion.
    /// </summary>
    /// <param name="promotionId">The promotion ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of quantity break tiers ordered by MinQuantity.</returns>
    Task<IEnumerable<QuantityBreakTier>> GetQuantityBreakTiersAsync(int promotionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the applicable tier for a product and quantity.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="quantity">The quantity being purchased.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The applicable tier if found.</returns>
    Task<QuantityBreakTier?> GetApplicableTierAsync(int productId, int quantity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates quantity break discount for given order items.
    /// </summary>
    /// <param name="items">The order items to evaluate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Quantity break calculation result.</returns>
    Task<QuantityBreakCalculationResult> CalculateQuantityBreakDiscountAsync(
        IEnumerable<OrderItemInfo> items,
        CancellationToken cancellationToken = default);

    #endregion

    #region Combo/Bundle Deals

    /// <summary>
    /// Creates a new combo promotion.
    /// </summary>
    /// <param name="promotion">The promotion details.</param>
    /// <param name="comboConfig">The combo configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created combo promotion.</returns>
    Task<CentralPromotion> CreateComboPromotionAsync(
        CentralPromotion promotion,
        ComboPromotion comboConfig,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets combo configuration for a promotion.
    /// </summary>
    /// <param name="promotionId">The promotion ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The combo configuration if exists.</returns>
    Task<ComboPromotion?> GetComboConfigurationAsync(int promotionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active combo deals.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of active combo promotions.</returns>
    Task<IEnumerable<ComboPromotion>> GetActiveCombosAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if items in cart can form a combo.
    /// </summary>
    /// <param name="items">The order items to evaluate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of applicable combos.</returns>
    Task<IEnumerable<ComboMatchResult>> FindApplicableCombosAsync(
        IEnumerable<OrderItemInfo> items,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a combo to an order.
    /// </summary>
    /// <param name="comboId">The combo promotion ID.</param>
    /// <param name="orderId">The order ID.</param>
    /// <param name="substitutions">Optional product substitutions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added order items.</returns>
    Task<IEnumerable<OrderItem>> AddComboToOrderAsync(
        int comboId,
        int orderId,
        IDictionary<int, int>? substitutions = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Coupon Management

    /// <summary>
    /// Creates a single coupon.
    /// </summary>
    /// <param name="coupon">The coupon to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created coupon.</returns>
    Task<Coupon> CreateCouponAsync(Coupon coupon, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a batch of coupons.
    /// </summary>
    /// <param name="batch">The batch configuration.</param>
    /// <param name="count">Number of coupons to generate.</param>
    /// <param name="template">Coupon template for all coupons in batch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created batch with coupons.</returns>
    Task<CouponBatch> GenerateCouponBatchAsync(
        CouponBatch batch,
        int count,
        Coupon template,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a coupon code.
    /// </summary>
    /// <param name="couponCode">The coupon code.</param>
    /// <param name="orderTotal">The current order total.</param>
    /// <param name="customerId">Optional customer ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result with coupon details if valid.</returns>
    Task<CouponValidationResult> ValidateCouponAsync(
        string couponCode,
        decimal orderTotal,
        int? customerId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a coupon to a receipt.
    /// </summary>
    /// <param name="couponCode">The coupon code.</param>
    /// <param name="receiptId">The receipt ID.</param>
    /// <param name="discountAmount">The discount amount applied.</param>
    /// <param name="userId">The user applying the coupon.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The coupon redemption record.</returns>
    Task<CouponRedemption> ApplyCouponAsync(
        string couponCode,
        int receiptId,
        decimal discountAmount,
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Voids a coupon (prevents future use).
    /// </summary>
    /// <param name="couponId">The coupon ID.</param>
    /// <param name="reason">Void reason.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task VoidCouponAsync(int couponId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets coupon by code.
    /// </summary>
    /// <param name="couponCode">The coupon code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The coupon if found.</returns>
    Task<Coupon?> GetCouponByCodeAsync(string couponCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets coupon redemption history.
    /// </summary>
    /// <param name="couponId">The coupon ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of redemption records.</returns>
    Task<IEnumerable<CouponRedemption>> GetCouponRedemptionsAsync(int couponId, CancellationToken cancellationToken = default);

    #endregion

    #region Automatic Markdown

    /// <summary>
    /// Creates an automatic markdown rule.
    /// </summary>
    /// <param name="markdown">The markdown rule.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created markdown rule.</returns>
    Task<AutomaticMarkdown> CreateMarkdownRuleAsync(AutomaticMarkdown markdown, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets markdown rules for a product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of markdown rules.</returns>
    Task<IEnumerable<AutomaticMarkdown>> GetMarkdownRulesForProductAsync(int productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets currently active markdown for a product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="currentTime">Current time for evaluation.</param>
    /// <param name="closingTime">Store closing time (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The active markdown if any.</returns>
    Task<AutomaticMarkdown?> GetActiveMarkdownAsync(
        int productId,
        DateTime currentTime,
        TimeSpan? closingTime = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all products with active markdowns.
    /// </summary>
    /// <param name="currentTime">Current time for evaluation.</param>
    /// <param name="closingTime">Store closing time (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of products with their active markdowns.</returns>
    Task<IEnumerable<ProductMarkdownInfo>> GetAllActiveMarkdownsAsync(
        DateTime currentTime,
        TimeSpan? closingTime = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates markdown price for a product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="originalPrice">Original product price.</param>
    /// <param name="currentTime">Current time for evaluation.</param>
    /// <param name="closingTime">Store closing time (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Markdown calculation result.</returns>
    Task<MarkdownCalculationResult> CalculateMarkdownPriceAsync(
        int productId,
        decimal originalPrice,
        DateTime currentTime,
        TimeSpan? closingTime = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Promotion Application

    /// <summary>
    /// Calculates all applicable promotions for order items.
    /// </summary>
    /// <param name="items">The order items.</param>
    /// <param name="orderTotal">Current order total.</param>
    /// <param name="customerId">Optional customer ID.</param>
    /// <param name="couponCode">Optional coupon code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Complete promotion calculation result.</returns>
    Task<PromotionCalculationResult> CalculateAllPromotionsAsync(
        IEnumerable<OrderItemInfo> items,
        decimal orderTotal,
        int? customerId = null,
        string? couponCode = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records promotion application to a receipt.
    /// </summary>
    /// <param name="application">The promotion application record.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The recorded application.</returns>
    Task<PromotionApplication> RecordPromotionApplicationAsync(
        PromotionApplication application,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets promotion applications for a receipt.
    /// </summary>
    /// <param name="receiptId">The receipt ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of promotion applications.</returns>
    Task<IEnumerable<PromotionApplication>> GetPromotionApplicationsAsync(int receiptId, CancellationToken cancellationToken = default);

    #endregion
}

#region DTOs and Result Types

/// <summary>
/// Information about an order item for promotion calculation.
/// </summary>
public class OrderItemInfo
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
    public int? BatchId { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

/// <summary>
/// Result of BOGO discount calculation.
/// </summary>
public class BogoCalculationResult
{
    public bool HasDiscount { get; set; }
    public decimal TotalDiscount { get; set; }
    public IList<BogoApplicationDetail> Applications { get; set; } = new List<BogoApplicationDetail>();
}

/// <summary>
/// Detail of a single BOGO application.
/// </summary>
public class BogoApplicationDetail
{
    public int PromotionId { get; set; }
    public string PromotionName { get; set; } = string.Empty;
    public int BuyProductId { get; set; }
    public int GetProductId { get; set; }
    public int FreeQuantity { get; set; }
    public decimal DiscountAmount { get; set; }
}

/// <summary>
/// Result of Mix &amp; Match discount calculation.
/// </summary>
public class MixMatchCalculationResult
{
    public bool HasDiscount { get; set; }
    public decimal TotalDiscount { get; set; }
    public IList<MixMatchApplicationDetail> Applications { get; set; } = new List<MixMatchApplicationDetail>();
}

/// <summary>
/// Detail of a single Mix &amp; Match application.
/// </summary>
public class MixMatchApplicationDetail
{
    public int PromotionId { get; set; }
    public string PromotionName { get; set; } = string.Empty;
    public IList<int> IncludedProductIds { get; set; } = new List<int>();
    public decimal OriginalTotal { get; set; }
    public decimal DiscountedTotal { get; set; }
    public decimal DiscountAmount { get; set; }
}

/// <summary>
/// Result of quantity break discount calculation.
/// </summary>
public class QuantityBreakCalculationResult
{
    public bool HasDiscount { get; set; }
    public decimal TotalDiscount { get; set; }
    public IList<QuantityBreakApplicationDetail> Applications { get; set; } = new List<QuantityBreakApplicationDetail>();
}

/// <summary>
/// Detail of a single quantity break application.
/// </summary>
public class QuantityBreakApplicationDetail
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string TierLabel { get; set; } = string.Empty;
    public decimal OriginalUnitPrice { get; set; }
    public decimal DiscountedUnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
}

/// <summary>
/// Result of combo matching.
/// </summary>
public class ComboMatchResult
{
    public int ComboPromotionId { get; set; }
    public string ComboName { get; set; } = string.Empty;
    public decimal ComboPrice { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal Savings { get; set; }
    public IList<ComboItemMatch> MatchedItems { get; set; } = new List<ComboItemMatch>();
    public IList<ComboItemMatch> MissingItems { get; set; } = new List<ComboItemMatch>();
    public bool IsComplete => MissingItems.Count == 0;
}

/// <summary>
/// Matched item in a combo.
/// </summary>
public class ComboItemMatch
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int RequiredQuantity { get; set; }
    public int AvailableQuantity { get; set; }
}

/// <summary>
/// Result of coupon validation.
/// </summary>
public class CouponValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public Coupon? Coupon { get; set; }
    public decimal CalculatedDiscount { get; set; }
}

/// <summary>
/// Information about a product's active markdown.
/// </summary>
public class ProductMarkdownInfo
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public decimal MarkdownPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public MarkdownTriggerType TriggerType { get; set; }
}

/// <summary>
/// Result of markdown price calculation.
/// </summary>
public class MarkdownCalculationResult
{
    public bool HasMarkdown { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal MarkdownPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal DiscountPercent { get; set; }
    public string? RuleName { get; set; }
    public int? MarkdownRuleId { get; set; }
}

/// <summary>
/// Complete result of all promotion calculations.
/// </summary>
public class PromotionCalculationResult
{
    public decimal OriginalTotal { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal FinalTotal => OriginalTotal - TotalDiscount;

    public BogoCalculationResult BogoResult { get; set; } = new();
    public MixMatchCalculationResult MixMatchResult { get; set; } = new();
    public QuantityBreakCalculationResult QuantityBreakResult { get; set; } = new();
    public CouponValidationResult? CouponResult { get; set; }
    public IList<MarkdownCalculationResult> MarkdownResults { get; set; } = new List<MarkdownCalculationResult>();

    public IList<AppliedPromotionSummary> AppliedPromotions { get; set; } = new List<AppliedPromotionSummary>();
}

/// <summary>
/// Summary of an applied promotion.
/// </summary>
public class AppliedPromotionSummary
{
    public int? PromotionId { get; set; }
    public string PromotionName { get; set; } = string.Empty;
    public string PromotionType { get; set; } = string.Empty;
    public decimal DiscountAmount { get; set; }
    public string Description { get; set; } = string.Empty;
}

#endregion
