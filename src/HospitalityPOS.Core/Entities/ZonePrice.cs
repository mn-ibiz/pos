namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents zone-specific pricing for a product.
/// Allows setting different prices for products in different pricing zones.
/// </summary>
public class ZonePrice : BaseEntity
{
    /// <summary>
    /// The pricing zone this price applies to.
    /// </summary>
    public int PricingZoneId { get; set; }

    /// <summary>
    /// The product this price applies to.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// The zone-specific selling price.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Zone-specific cost price (optional).
    /// </summary>
    public decimal? CostPrice { get; set; }

    /// <summary>
    /// Zone-specific minimum price for discounting.
    /// </summary>
    public decimal? MinimumPrice { get; set; }

    /// <summary>
    /// When this price becomes effective.
    /// </summary>
    public DateTime EffectiveFrom { get; set; }

    /// <summary>
    /// When this price expires (null = no expiry).
    /// </summary>
    public DateTime? EffectiveTo { get; set; }

    /// <summary>
    /// Reason for this zone price (e.g., "Higher transport costs").
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Whether this zone price is currently active.
    /// </summary>
    public bool IsCurrentlyEffective =>
        IsActive &&
        DateTime.UtcNow >= EffectiveFrom &&
        (!EffectiveTo.HasValue || DateTime.UtcNow <= EffectiveTo.Value);

    /// <summary>
    /// Navigation property - the pricing zone.
    /// </summary>
    public virtual PricingZone? PricingZone { get; set; }

    /// <summary>
    /// Navigation property - the product.
    /// </summary>
    public virtual Product? Product { get; set; }
}
