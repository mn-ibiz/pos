namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a scheduled price change that will be applied at a future date.
/// </summary>
public class ScheduledPriceChange : BaseEntity
{
    /// <summary>
    /// The product for this price change.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Optional pricing zone. If null, applies to all zones.
    /// </summary>
    public int? PricingZoneId { get; set; }

    /// <summary>
    /// Optional specific store. If null, applies to zone or all stores.
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// The old price (for audit/display).
    /// </summary>
    public decimal OldPrice { get; set; }

    /// <summary>
    /// The new price to be applied.
    /// </summary>
    public decimal NewPrice { get; set; }

    /// <summary>
    /// Optional new cost price.
    /// </summary>
    public decimal? NewCostPrice { get; set; }

    /// <summary>
    /// The date/time when this price change should be applied.
    /// </summary>
    public DateTime EffectiveDate { get; set; }

    /// <summary>
    /// Optional expiry date for the new price (for temporary prices).
    /// </summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>
    /// Current status of the price change.
    /// </summary>
    public PriceChangeStatus Status { get; set; } = PriceChangeStatus.Scheduled;

    /// <summary>
    /// When the price change was actually applied.
    /// </summary>
    public DateTime? AppliedAt { get; set; }

    /// <summary>
    /// Who applied the price change (system or user).
    /// </summary>
    public int? AppliedByUserId { get; set; }

    /// <summary>
    /// Reason for the price change.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Notes about this price change.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Whether the change was applied automatically by the system.
    /// </summary>
    public bool WasAutoApplied { get; set; }

    /// <summary>
    /// Navigation property - the product.
    /// </summary>
    public virtual Product? Product { get; set; }

    /// <summary>
    /// Navigation property - the pricing zone (if applicable).
    /// </summary>
    public virtual PricingZone? PricingZone { get; set; }

    /// <summary>
    /// Navigation property - the specific store (if applicable).
    /// </summary>
    public virtual Store? Store { get; set; }
}

/// <summary>
/// Status of a scheduled price change.
/// </summary>
public enum PriceChangeStatus
{
    /// <summary>
    /// Price change is scheduled for a future date.
    /// </summary>
    Scheduled = 0,

    /// <summary>
    /// Price change has been applied.
    /// </summary>
    Applied = 1,

    /// <summary>
    /// Price change was cancelled before being applied.
    /// </summary>
    Cancelled = 2,

    /// <summary>
    /// Price change failed to apply.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Price change expired without being applied.
    /// </summary>
    Expired = 4
}
