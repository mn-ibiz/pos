namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Type of points multiplier rule.
/// </summary>
public enum PointsMultiplierRuleType
{
    /// <summary>
    /// Applies to a specific product.
    /// </summary>
    Product = 1,

    /// <summary>
    /// Applies to all products in a category.
    /// </summary>
    Category = 2,

    /// <summary>
    /// Applies to all products (promotional period).
    /// </summary>
    Global = 3,

    /// <summary>
    /// Applies based on member tier.
    /// </summary>
    TierBased = 4,

    /// <summary>
    /// Applies based on day of week.
    /// </summary>
    DayOfWeek = 5,

    /// <summary>
    /// Applies based on time of day (happy hour).
    /// </summary>
    TimeOfDay = 6
}

/// <summary>
/// Defines promotional or conditional points multiplier rules.
/// These rules can override or stack with product/category multipliers.
/// </summary>
public class PointsMultiplierRule : BaseEntity
{
    /// <summary>
    /// Gets or sets the rule name for identification.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rule description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the rule type.
    /// </summary>
    public PointsMultiplierRuleType RuleType { get; set; }

    /// <summary>
    /// Gets or sets the points multiplier to apply.
    /// 1.0 = normal, 2.0 = double points, 0.5 = half points.
    /// </summary>
    public decimal Multiplier { get; set; } = 1.0m;

    /// <summary>
    /// Gets or sets whether this multiplier stacks with other multipliers.
    /// If false, this multiplier replaces other multipliers when it applies.
    /// </summary>
    public bool IsStackable { get; set; } = false;

    /// <summary>
    /// Gets or sets the priority for rule evaluation (higher = evaluated first).
    /// </summary>
    public int Priority { get; set; } = 100;

    // Target constraints

    /// <summary>
    /// Gets or sets the product ID (for Product rule type).
    /// </summary>
    public int? ProductId { get; set; }

    /// <summary>
    /// Gets or sets the category ID (for Category rule type).
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the minimum membership tier required (for TierBased rule type).
    /// </summary>
    public MembershipTier? MinimumTier { get; set; }

    // Time constraints

    /// <summary>
    /// Gets or sets the start date when this rule becomes active.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date when this rule expires.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Gets or sets days of week when this rule applies (comma-separated: "Monday,Tuesday,Wednesday").
    /// Null means all days.
    /// </summary>
    public string? DaysOfWeek { get; set; }

    /// <summary>
    /// Gets or sets the start time of day when this rule applies (e.g., 14:00 for 2 PM).
    /// </summary>
    public TimeOnly? StartTime { get; set; }

    /// <summary>
    /// Gets or sets the end time of day when this rule applies (e.g., 18:00 for 6 PM).
    /// </summary>
    public TimeOnly? EndTime { get; set; }

    // Purchase constraints

    /// <summary>
    /// Gets or sets the minimum transaction amount required for this rule to apply.
    /// </summary>
    public decimal? MinimumPurchaseAmount { get; set; }

    /// <summary>
    /// Gets or sets the minimum quantity of the product required (for Product rule type).
    /// </summary>
    public int? MinimumQuantity { get; set; }

    /// <summary>
    /// Gets or sets the maximum bonus points that can be earned from this rule per transaction.
    /// Null means no limit.
    /// </summary>
    public decimal? MaxBonusPointsPerTransaction { get; set; }

    // Usage limits

    /// <summary>
    /// Gets or sets the maximum number of times this rule can be used globally.
    /// Null means unlimited.
    /// </summary>
    public int? MaxTotalUsages { get; set; }

    /// <summary>
    /// Gets or sets the current usage count.
    /// </summary>
    public int CurrentUsageCount { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of times a single member can use this rule.
    /// Null means unlimited.
    /// </summary>
    public int? MaxUsagesPerMember { get; set; }

    // Store constraint

    /// <summary>
    /// Gets or sets the store ID this rule applies to. Null means all stores.
    /// </summary>
    public int? StoreId { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the associated product.
    /// </summary>
    public virtual Product? Product { get; set; }

    /// <summary>
    /// Gets or sets the associated category.
    /// </summary>
    public virtual Category? Category { get; set; }

    /// <summary>
    /// Gets or sets the associated store.
    /// </summary>
    public virtual Store? Store { get; set; }

    /// <summary>
    /// Gets or sets the usage history for this rule.
    /// </summary>
    public virtual ICollection<PointsMultiplierUsage> Usages { get; set; } = new List<PointsMultiplierUsage>();

    // Helper methods

    /// <summary>
    /// Checks if this rule is currently active based on date constraints.
    /// </summary>
    public bool IsCurrentlyActive
    {
        get
        {
            if (!IsActive) return false;
            var now = DateTime.UtcNow;
            if (StartDate.HasValue && now < StartDate.Value) return false;
            if (EndDate.HasValue && now > EndDate.Value) return false;
            return true;
        }
    }

    /// <summary>
    /// Checks if this rule applies at the given time.
    /// </summary>
    public bool AppliesToTime(DateTime dateTime)
    {
        if (!IsCurrentlyActive) return false;

        // Check day of week
        if (!string.IsNullOrEmpty(DaysOfWeek))
        {
            var days = DaysOfWeek.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var currentDay = dateTime.DayOfWeek.ToString();
            if (!days.Any(d => d.Trim().Equals(currentDay, StringComparison.OrdinalIgnoreCase)))
                return false;
        }

        // Check time of day
        var timeOnly = TimeOnly.FromDateTime(dateTime);
        if (StartTime.HasValue && timeOnly < StartTime.Value) return false;
        if (EndTime.HasValue && timeOnly > EndTime.Value) return false;

        return true;
    }

    /// <summary>
    /// Checks if usage limit has been reached.
    /// </summary>
    public bool IsUsageLimitReached => MaxTotalUsages.HasValue && CurrentUsageCount >= MaxTotalUsages.Value;
}

/// <summary>
/// Tracks usage of points multiplier rules by members.
/// </summary>
public class PointsMultiplierUsage : BaseEntity
{
    /// <summary>
    /// Gets or sets the rule ID.
    /// </summary>
    public int PointsMultiplierRuleId { get; set; }

    /// <summary>
    /// Gets or sets the loyalty member ID.
    /// </summary>
    public int LoyaltyMemberId { get; set; }

    /// <summary>
    /// Gets or sets the loyalty transaction ID where this rule was applied.
    /// </summary>
    public int? LoyaltyTransactionId { get; set; }

    /// <summary>
    /// Gets or sets the receipt ID.
    /// </summary>
    public int? ReceiptId { get; set; }

    /// <summary>
    /// Gets or sets when the rule was used.
    /// </summary>
    public DateTime UsedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the base points before multiplier.
    /// </summary>
    public decimal BasePoints { get; set; }

    /// <summary>
    /// Gets or sets the bonus points earned from this rule.
    /// </summary>
    public decimal BonusPointsEarned { get; set; }

    /// <summary>
    /// Gets or sets the multiplier that was applied.
    /// </summary>
    public decimal MultiplierApplied { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the associated rule.
    /// </summary>
    public virtual PointsMultiplierRule? Rule { get; set; }

    /// <summary>
    /// Gets or sets the associated loyalty member.
    /// </summary>
    public virtual LoyaltyMember? Member { get; set; }

    /// <summary>
    /// Gets or sets the associated loyalty transaction.
    /// </summary>
    public virtual LoyaltyTransaction? Transaction { get; set; }
}
