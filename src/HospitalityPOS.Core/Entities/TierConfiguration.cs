using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Configuration for a membership tier level.
/// Defines thresholds, benefits, and multipliers for each tier.
/// </summary>
public class TierConfiguration : BaseEntity
{
    /// <summary>
    /// Gets or sets the tier level this configuration is for.
    /// </summary>
    public MembershipTier Tier { get; set; }

    /// <summary>
    /// Gets or sets the display name for this tier (e.g., "Bronze", "Silver").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the tier and its benefits.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the minimum lifetime spend in KES required to reach this tier.
    /// </summary>
    public decimal SpendThreshold { get; set; }

    /// <summary>
    /// Gets or sets the minimum lifetime points required to reach this tier (alternative to spend).
    /// </summary>
    public decimal PointsThreshold { get; set; }

    /// <summary>
    /// Gets or sets the points earning multiplier for this tier.
    /// 1.0 = standard rate, 1.5 = 50% bonus, 2.0 = double points.
    /// </summary>
    public decimal PointsMultiplier { get; set; } = 1.0m;

    /// <summary>
    /// Gets or sets the base discount percentage for this tier (0-100).
    /// </summary>
    public decimal DiscountPercent { get; set; }

    /// <summary>
    /// Gets or sets whether members of this tier get free delivery.
    /// </summary>
    public bool FreeDelivery { get; set; }

    /// <summary>
    /// Gets or sets whether members of this tier get priority service.
    /// </summary>
    public bool PriorityService { get; set; }

    /// <summary>
    /// Gets or sets the sort order for display (lowest to highest tier).
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Gets or sets the color code for UI display (hex format).
    /// </summary>
    public string? ColorCode { get; set; }

    /// <summary>
    /// Gets or sets the icon name for UI display.
    /// </summary>
    public string? IconName { get; set; }
}
