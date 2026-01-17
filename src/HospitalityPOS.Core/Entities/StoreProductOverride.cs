namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents store-specific overrides for a product.
/// Allows individual stores to have different prices or settings.
/// </summary>
public class StoreProductOverride : BaseEntity
{
    /// <summary>
    /// The store this override applies to.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// The product being overridden.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Override selling price for this store. Null means use central price.
    /// </summary>
    public decimal? OverridePrice { get; set; }

    /// <summary>
    /// Override cost price for this store. Null means use central cost.
    /// </summary>
    public decimal? OverrideCost { get; set; }

    /// <summary>
    /// Whether the product is available at this store.
    /// False means the product is hidden at this store.
    /// </summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// Store-specific minimum stock level.
    /// </summary>
    public decimal? OverrideMinStock { get; set; }

    /// <summary>
    /// Store-specific maximum stock level.
    /// </summary>
    public decimal? OverrideMaxStock { get; set; }

    /// <summary>
    /// Store-specific tax rate override.
    /// </summary>
    public decimal? OverrideTaxRate { get; set; }

    /// <summary>
    /// Store-specific kitchen station override.
    /// </summary>
    public string? OverrideKitchenStation { get; set; }

    /// <summary>
    /// Reason for the override.
    /// </summary>
    public string? OverrideReason { get; set; }

    /// <summary>
    /// Date when the override was last synchronized.
    /// </summary>
    public DateTime? LastSyncTime { get; set; }

    // Navigation properties
    public virtual Store? Store { get; set; }
    public virtual Product? Product { get; set; }
}
