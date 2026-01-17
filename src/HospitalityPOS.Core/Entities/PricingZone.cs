namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a pricing zone for regional pricing management.
/// Stores can be grouped into zones for zone-specific pricing.
/// </summary>
public class PricingZone : BaseEntity
{
    /// <summary>
    /// Zone code for quick reference (e.g., "NRB", "MBA", "UPC").
    /// </summary>
    public string ZoneCode { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the zone (e.g., "Nairobi Region", "Coast Region").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the zone.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Currency code for this zone (e.g., "KES"). Defaults to system currency.
    /// </summary>
    public string CurrencyCode { get; set; } = "KES";

    /// <summary>
    /// Default tax rate for this zone if different from central.
    /// </summary>
    public decimal? DefaultTaxRate { get; set; }

    /// <summary>
    /// Whether this is the default zone for stores without zone assignment.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Display order for UI.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Navigation property - stores in this zone.
    /// </summary>
    public virtual ICollection<Store> Stores { get; set; } = new List<Store>();

    /// <summary>
    /// Navigation property - zone prices.
    /// </summary>
    public virtual ICollection<ZonePrice> ZonePrices { get; set; } = new List<ZonePrice>();

    /// <summary>
    /// Navigation property - scheduled price changes for this zone.
    /// </summary>
    public virtual ICollection<ScheduledPriceChange> ScheduledPriceChanges { get; set; } = new List<ScheduledPriceChange>();
}
