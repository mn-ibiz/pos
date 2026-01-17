// src/HospitalityPOS.Core/Models/Pricing/TimePricingDtos.cs
// DTOs for Time-Based Pricing (Happy Hour, Lunch Specials, etc.)
// Story 44-2: Happy Hour / Time-Based Pricing

namespace HospitalityPOS.Core.Models.Pricing;

/// <summary>
/// Type of discount for time-based pricing.
/// </summary>
public enum TimeDiscountType
{
    /// <summary>Percentage off regular price.</summary>
    Percentage,

    /// <summary>Fixed price replacement.</summary>
    FixedPrice,

    /// <summary>Fixed amount off regular price.</summary>
    AmountOff,

    /// <summary>Buy one get one free.</summary>
    BuyOneGetOne
}

/// <summary>
/// Scope of application for a pricing rule.
/// </summary>
public enum PricingRuleScope
{
    /// <summary>Applies to specific products.</summary>
    SpecificProducts,

    /// <summary>Applies to entire categories.</summary>
    Categories,

    /// <summary>Applies to all products.</summary>
    AllProducts
}

/// <summary>
/// Time-based pricing rule configuration.
/// </summary>
public class TimePricingRule
{
    /// <summary>Unique identifier.</summary>
    public int Id { get; set; }

    /// <summary>Rule name (e.g., "Happy Hour", "Lunch Special").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Description of the promotion.</summary>
    public string? Description { get; set; }

    /// <summary>Short display text for receipts.</summary>
    public string DisplayText { get; set; } = string.Empty;

    /// <summary>Start time of day (e.g., 16:00).</summary>
    public TimeOnly StartTime { get; set; }

    /// <summary>End time of day (e.g., 19:00).</summary>
    public TimeOnly EndTime { get; set; }

    /// <summary>Days of week when rule applies.</summary>
    public List<DayOfWeek> ApplicableDays { get; set; } = new();

    /// <summary>Type of discount.</summary>
    public TimeDiscountType DiscountType { get; set; } = TimeDiscountType.Percentage;

    /// <summary>Discount value (percentage or amount depending on type).</summary>
    public decimal DiscountValue { get; set; }

    /// <summary>Fixed price if using FixedPrice discount type.</summary>
    public decimal? FixedPrice { get; set; }

    /// <summary>Priority for overlapping rules (higher = takes precedence).</summary>
    public int Priority { get; set; }

    /// <summary>Whether this discount can stack with other discounts.</summary>
    public bool CanStackWithOtherDiscounts { get; set; }

    /// <summary>Whether the rule is currently enabled.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Start date for seasonal rules (null = always valid).</summary>
    public DateOnly? ValidFrom { get; set; }

    /// <summary>End date for seasonal rules (null = always valid).</summary>
    public DateOnly? ValidTo { get; set; }

    /// <summary>Scope of application.</summary>
    public PricingRuleScope Scope { get; set; } = PricingRuleScope.SpecificProducts;

    /// <summary>Product IDs this rule applies to (if scope is SpecificProducts).</summary>
    public List<int> ProductIds { get; set; } = new();

    /// <summary>Category IDs this rule applies to (if scope is Categories).</summary>
    public List<int> CategoryIds { get; set; } = new();

    /// <summary>Color for UI display (hex).</summary>
    public string BadgeColor { get; set; } = "#F59E0B";

    /// <summary>Icon name for UI display.</summary>
    public string? IconName { get; set; }

    /// <summary>Created date.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Last updated date.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Checks if the rule is currently active based on time and date.
    /// </summary>
    public bool IsCurrentlyActive(DateTime currentTime)
    {
        if (!IsActive) return false;

        // Check date validity
        var currentDate = DateOnly.FromDateTime(currentTime);
        if (ValidFrom.HasValue && currentDate < ValidFrom.Value) return false;
        if (ValidTo.HasValue && currentDate > ValidTo.Value) return false;

        // Check day of week
        if (!ApplicableDays.Contains(currentTime.DayOfWeek)) return false;

        // Check time window
        var currentTimeOfDay = TimeOnly.FromDateTime(currentTime);

        if (StartTime <= EndTime)
        {
            // Normal window (e.g., 16:00 - 19:00)
            return currentTimeOfDay >= StartTime && currentTimeOfDay < EndTime;
        }
        else
        {
            // Crosses midnight (e.g., 22:00 - 02:00)
            return currentTimeOfDay >= StartTime || currentTimeOfDay < EndTime;
        }
    }

    /// <summary>
    /// Calculates the discounted price for a given regular price.
    /// </summary>
    public decimal CalculateDiscountedPrice(decimal regularPrice)
    {
        return DiscountType switch
        {
            TimeDiscountType.Percentage => Math.Round(regularPrice * (1 - DiscountValue / 100), 2),
            TimeDiscountType.FixedPrice => FixedPrice ?? regularPrice,
            TimeDiscountType.AmountOff => Math.Max(0, regularPrice - DiscountValue),
            TimeDiscountType.BuyOneGetOne => regularPrice, // Handled differently at cart level
            _ => regularPrice
        };
    }

    /// <summary>
    /// Calculates the discount amount for a given regular price.
    /// </summary>
    public decimal CalculateDiscountAmount(decimal regularPrice)
    {
        return regularPrice - CalculateDiscountedPrice(regularPrice);
    }

    /// <summary>
    /// Gets a formatted display string for the discount.
    /// </summary>
    public string GetDiscountDisplay()
    {
        return DiscountType switch
        {
            TimeDiscountType.Percentage => $"-{DiscountValue:N0}%",
            TimeDiscountType.FixedPrice => FixedPrice.HasValue ? $"Only {FixedPrice:N0}" : "",
            TimeDiscountType.AmountOff => $"-{DiscountValue:N0}",
            TimeDiscountType.BuyOneGetOne => "BOGO",
            _ => ""
        };
    }
}

/// <summary>
/// Product pricing information with time-based rules applied.
/// </summary>
public class ProductPricingInfo
{
    /// <summary>Product ID.</summary>
    public int ProductId { get; set; }

    /// <summary>Product name.</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Category ID.</summary>
    public int CategoryId { get; set; }

    /// <summary>Regular price before any time-based discount.</summary>
    public decimal RegularPrice { get; set; }

    /// <summary>Effective price after time-based discount.</summary>
    public decimal EffectivePrice { get; set; }

    /// <summary>Whether a time-based discount is applied.</summary>
    public bool HasTimePricingDiscount { get; set; }

    /// <summary>Active rule that provides the discount (if any).</summary>
    public TimePricingRule? ActiveRule { get; set; }

    /// <summary>Discount amount if time-based pricing active.</summary>
    public decimal DiscountAmount => RegularPrice - EffectivePrice;

    /// <summary>Discount percentage if time-based pricing active.</summary>
    public decimal DiscountPercent => RegularPrice > 0
        ? Math.Round((DiscountAmount / RegularPrice) * 100, 1)
        : 0;

    /// <summary>Display text for the promotion.</summary>
    public string? PromotionDisplayText => ActiveRule?.DisplayText;

    /// <summary>Badge color for UI.</summary>
    public string? BadgeColor => ActiveRule?.BadgeColor;
}

/// <summary>
/// Status of active time-based promotions.
/// </summary>
public class ActivePromotionStatus
{
    /// <summary>List of currently active rules.</summary>
    public List<TimePricingRule> ActiveRules { get; set; } = new();

    /// <summary>Whether any happy hour type promotion is active.</summary>
    public bool IsHappyHourActive { get; set; }

    /// <summary>Primary active promotion name.</summary>
    public string? PrimaryPromotionName { get; set; }

    /// <summary>Time when current promotion ends.</summary>
    public TimeOnly? EndsAt { get; set; }

    /// <summary>Formatted string for when promotion ends.</summary>
    public string? EndsAtDisplay => EndsAt?.ToString("h:mm tt");

    /// <summary>Number of products affected.</summary>
    public int AffectedProductCount { get; set; }

    /// <summary>Check timestamp.</summary>
    public DateTime CheckedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Request to create or update a time pricing rule.
/// </summary>
public class TimePricingRuleRequest
{
    /// <summary>Rule ID (0 for new rule).</summary>
    public int Id { get; set; }

    /// <summary>Rule name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Description.</summary>
    public string? Description { get; set; }

    /// <summary>Display text for receipts.</summary>
    public string DisplayText { get; set; } = string.Empty;

    /// <summary>Start time (format: "HH:mm").</summary>
    public string StartTime { get; set; } = "16:00";

    /// <summary>End time (format: "HH:mm").</summary>
    public string EndTime { get; set; } = "19:00";

    /// <summary>Applicable days (0=Sunday, 6=Saturday).</summary>
    public List<int> ApplicableDayNumbers { get; set; } = new() { 1, 2, 3, 4, 5 }; // Mon-Fri

    /// <summary>Discount type.</summary>
    public TimeDiscountType DiscountType { get; set; }

    /// <summary>Discount value.</summary>
    public decimal DiscountValue { get; set; }

    /// <summary>Fixed price if applicable.</summary>
    public decimal? FixedPrice { get; set; }

    /// <summary>Priority.</summary>
    public int Priority { get; set; }

    /// <summary>Whether can stack with other discounts.</summary>
    public bool CanStack { get; set; }

    /// <summary>Product IDs.</summary>
    public List<int> ProductIds { get; set; } = new();

    /// <summary>Category IDs.</summary>
    public List<int> CategoryIds { get; set; } = new();

    /// <summary>Start date (format: "yyyy-MM-dd").</summary>
    public string? ValidFrom { get; set; }

    /// <summary>End date (format: "yyyy-MM-dd").</summary>
    public string? ValidTo { get; set; }

    /// <summary>Converts to TimePricingRule.</summary>
    public TimePricingRule ToRule()
    {
        return new TimePricingRule
        {
            Id = Id,
            Name = Name,
            Description = Description,
            DisplayText = DisplayText,
            StartTime = TimeOnly.Parse(StartTime),
            EndTime = TimeOnly.Parse(EndTime),
            ApplicableDays = ApplicableDayNumbers.Select(d => (DayOfWeek)d).ToList(),
            DiscountType = DiscountType,
            DiscountValue = DiscountValue,
            FixedPrice = FixedPrice,
            Priority = Priority,
            CanStackWithOtherDiscounts = CanStack,
            ProductIds = ProductIds,
            CategoryIds = CategoryIds,
            Scope = ProductIds.Count > 0 ? PricingRuleScope.SpecificProducts
                : CategoryIds.Count > 0 ? PricingRuleScope.Categories
                : PricingRuleScope.AllProducts,
            ValidFrom = !string.IsNullOrEmpty(ValidFrom) ? DateOnly.Parse(ValidFrom) : null,
            ValidTo = !string.IsNullOrEmpty(ValidTo) ? DateOnly.Parse(ValidTo) : null,
            IsActive = true
        };
    }
}

/// <summary>
/// Report data for time-based pricing analytics.
/// </summary>
public class TimePricingAnalytics
{
    /// <summary>Report date range start.</summary>
    public DateTime StartDate { get; set; }

    /// <summary>Report date range end.</summary>
    public DateTime EndDate { get; set; }

    /// <summary>Total sales during promotion periods.</summary>
    public decimal PromotionPeriodSales { get; set; }

    /// <summary>Total sales during regular periods.</summary>
    public decimal RegularPeriodSales { get; set; }

    /// <summary>Total discount amount given.</summary>
    public decimal TotalDiscountAmount { get; set; }

    /// <summary>Number of transactions during promotions.</summary>
    public int PromotionTransactionCount { get; set; }

    /// <summary>Average transaction value during promotions.</summary>
    public decimal PromotionAverageTransaction => PromotionTransactionCount > 0
        ? PromotionPeriodSales / PromotionTransactionCount
        : 0;

    /// <summary>Number of transactions during regular periods.</summary>
    public int RegularTransactionCount { get; set; }

    /// <summary>Average transaction value during regular periods.</summary>
    public decimal RegularAverageTransaction => RegularTransactionCount > 0
        ? RegularPeriodSales / RegularTransactionCount
        : 0;

    /// <summary>Breakdown by promotion.</summary>
    public List<PromotionPerformance> PromotionBreakdown { get; set; } = new();

    /// <summary>Popular items during promotions.</summary>
    public List<PopularPromotionItem> PopularItems { get; set; } = new();
}

/// <summary>
/// Performance data for a specific promotion.
/// </summary>
public class PromotionPerformance
{
    /// <summary>Rule ID.</summary>
    public int RuleId { get; set; }

    /// <summary>Promotion name.</summary>
    public string PromotionName { get; set; } = string.Empty;

    /// <summary>Total sales during this promotion.</summary>
    public decimal TotalSales { get; set; }

    /// <summary>Total discount given.</summary>
    public decimal TotalDiscount { get; set; }

    /// <summary>Number of orders.</summary>
    public int OrderCount { get; set; }

    /// <summary>Number of items sold.</summary>
    public int ItemsSold { get; set; }

    /// <summary>Active hours during period.</summary>
    public decimal ActiveHours { get; set; }

    /// <summary>Sales per hour.</summary>
    public decimal SalesPerHour => ActiveHours > 0 ? TotalSales / ActiveHours : 0;
}

/// <summary>
/// Popular item during promotions.
/// </summary>
public class PopularPromotionItem
{
    /// <summary>Product ID.</summary>
    public int ProductId { get; set; }

    /// <summary>Product name.</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Quantity sold during promotions.</summary>
    public int QuantitySold { get; set; }

    /// <summary>Revenue generated.</summary>
    public decimal Revenue { get; set; }

    /// <summary>Discount given.</summary>
    public decimal DiscountGiven { get; set; }
}

/// <summary>
/// Order line item with time-based pricing info.
/// </summary>
public class TimePricedOrderItem
{
    /// <summary>Product ID.</summary>
    public int ProductId { get; set; }

    /// <summary>Product name.</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Quantity.</summary>
    public int Quantity { get; set; }

    /// <summary>Regular unit price.</summary>
    public decimal RegularPrice { get; set; }

    /// <summary>Discounted unit price.</summary>
    public decimal DiscountedPrice { get; set; }

    /// <summary>Applied rule ID.</summary>
    public int? AppliedRuleId { get; set; }

    /// <summary>Promotion name.</summary>
    public string? PromotionName { get; set; }

    /// <summary>Line total at discounted price.</summary>
    public decimal LineTotal => DiscountedPrice * Quantity;

    /// <summary>Discount amount per unit.</summary>
    public decimal UnitDiscount => RegularPrice - DiscountedPrice;

    /// <summary>Total discount for the line.</summary>
    public decimal TotalDiscount => UnitDiscount * Quantity;

    /// <summary>Format for receipt display.</summary>
    public string FormatForReceipt(string currencySymbol = "KSh")
    {
        if (AppliedRuleId.HasValue && UnitDiscount > 0)
        {
            return $"{ProductName}\n  {PromotionName} {UnitDiscount / RegularPrice * 100:N0}% off\n  Was: {currencySymbol} {RegularPrice:N0}  Now: {currencySymbol} {DiscountedPrice:N0}";
        }
        return $"{ProductName}\n  {currencySymbol} {DiscountedPrice:N0}";
    }
}

/// <summary>
/// Event args for promotion activation changes.
/// </summary>
public class PromotionActivationChangedEventArgs : EventArgs
{
    /// <summary>Whether promotions are now active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Newly activated rules.</summary>
    public List<TimePricingRule> ActivatedRules { get; set; } = new();

    /// <summary>Just deactivated rules.</summary>
    public List<TimePricingRule> DeactivatedRules { get; set; } = new();

    /// <summary>Change timestamp.</summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
