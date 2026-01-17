namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a product available for sale.
/// </summary>
public class Product : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? CategoryId { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal? CostPrice { get; set; }
    public decimal TaxRate { get; set; } = 16.00m; // Kenya VAT
    public string UnitOfMeasure { get; set; } = "Each";
    public string? ImagePath { get; set; }
    public string? Barcode { get; set; }
    public decimal? MinStockLevel { get; set; }
    public decimal? MaxStockLevel { get; set; }

    /// <summary>
    /// Gets or sets whether inventory tracking is enabled for this product.
    /// When false, stock deductions are skipped during sales.
    /// </summary>
    public bool TrackInventory { get; set; } = true;

    /// <summary>
    /// Gets whether the product is low on stock based on MinStockLevel.
    /// </summary>
    public bool IsLowStock => TrackInventory && Inventory != null &&
                              MinStockLevel.HasValue &&
                              Inventory.CurrentStock <= MinStockLevel.Value &&
                              Inventory.CurrentStock > 0;

    /// <summary>
    /// Gets whether the product is out of stock.
    /// </summary>
    public bool IsOutOfStock => TrackInventory && Inventory != null &&
                                Inventory.CurrentStock <= 0;

    /// <summary>
    /// Gets or sets the kitchen station for KOT printing (e.g., KITCHEN, BAR, COLD STATION).
    /// </summary>
    public string? KitchenStation { get; set; }

    /// <summary>
    /// Whether this product was created at HQ (central product).
    /// </summary>
    public bool IsCentralProduct { get; set; }

    /// <summary>
    /// Whether store-specific overrides are allowed.
    /// </summary>
    public bool AllowStoreOverride { get; set; } = true;

    /// <summary>
    /// Last time this product was synchronized from HQ.
    /// </summary>
    public DateTime? LastSyncTime { get; set; }

    // Navigation properties
    public virtual Category? Category { get; set; }
    public virtual Inventory? Inventory { get; set; }
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    public virtual ICollection<ProductOffer> ProductOffers { get; set; } = new List<ProductOffer>();
    public virtual ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();
    public virtual ICollection<GRNItem> GRNItems { get; set; } = new List<GRNItem>();
    public virtual ICollection<StoreProductOverride> StoreOverrides { get; set; } = new List<StoreProductOverride>();
    public virtual ICollection<ZonePrice> ZonePrices { get; set; } = new List<ZonePrice>();
    public virtual ICollection<ScheduledPriceChange> ScheduledPriceChanges { get; set; } = new List<ScheduledPriceChange>();
}
