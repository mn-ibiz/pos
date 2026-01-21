namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Type of variant option (e.g., Size, Color, Material).
/// </summary>
public enum VariantOptionType
{
    /// <summary>Size variants (S, M, L, XL).</summary>
    Size = 1,
    /// <summary>Color variants.</summary>
    Color = 2,
    /// <summary>Material variants.</summary>
    Material = 3,
    /// <summary>Flavor variants.</summary>
    Flavor = 4,
    /// <summary>Weight variants.</summary>
    Weight = 5,
    /// <summary>Volume variants.</summary>
    Volume = 6,
    /// <summary>Pack size variants.</summary>
    PackSize = 7,
    /// <summary>Custom variant type.</summary>
    Custom = 99
}

/// <summary>
/// Represents a variant option definition (e.g., "Size" with values "Small", "Medium", "Large").
/// This is a template that defines what options are available for products.
/// </summary>
public class VariantOption : BaseEntity
{
    /// <summary>
    /// Name of the variant option (e.g., "Size", "Color", "Flavor").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display name for UI.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Type of variant option.
    /// </summary>
    public VariantOptionType OptionType { get; set; } = VariantOptionType.Custom;

    /// <summary>
    /// Description of this variant option.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Display order for UI.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether this option is globally available or product-specific.
    /// </summary>
    public bool IsGlobal { get; set; } = true;

    // Navigation properties
    /// <summary>
    /// Available values for this option.
    /// </summary>
    public virtual ICollection<VariantOptionValue> Values { get; set; } = new List<VariantOptionValue>();

    /// <summary>
    /// Products using this variant option.
    /// </summary>
    public virtual ICollection<ProductVariantOption> ProductVariantOptions { get; set; } = new List<ProductVariantOption>();
}

/// <summary>
/// Represents a specific value for a variant option (e.g., "Small" for Size option).
/// </summary>
public class VariantOptionValue : BaseEntity
{
    /// <summary>
    /// The parent variant option.
    /// </summary>
    public int VariantOptionId { get; set; }

    /// <summary>
    /// The value (e.g., "Small", "Red", "500ml").
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Display name for UI.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Color code for visual representation (hex).
    /// </summary>
    public string? ColorCode { get; set; }

    /// <summary>
    /// Image path for visual representation.
    /// </summary>
    public string? ImagePath { get; set; }

    /// <summary>
    /// Additional price adjustment for this value (can be positive or negative).
    /// </summary>
    public decimal PriceAdjustment { get; set; }

    /// <summary>
    /// Whether price adjustment is a percentage (true) or fixed amount (false).
    /// </summary>
    public bool IsPriceAdjustmentPercent { get; set; }

    /// <summary>
    /// Display order within the option.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// SKU suffix to append to product SKU.
    /// </summary>
    public string? SkuSuffix { get; set; }

    // Navigation properties
    /// <summary>
    /// The parent variant option.
    /// </summary>
    public virtual VariantOption? VariantOption { get; set; }

    /// <summary>
    /// Product variants using this value.
    /// </summary>
    public virtual ICollection<ProductVariantValue> ProductVariantValues { get; set; } = new List<ProductVariantValue>();
}

/// <summary>
/// Links a product to a variant option (e.g., "T-Shirt uses Size option").
/// </summary>
public class ProductVariantOption : BaseEntity
{
    /// <summary>
    /// The product.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// The variant option.
    /// </summary>
    public int VariantOptionId { get; set; }

    /// <summary>
    /// Whether this option is required for the product.
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// Display order for this option on the product.
    /// </summary>
    public int DisplayOrder { get; set; }

    // Navigation properties
    /// <summary>
    /// The product.
    /// </summary>
    public virtual Product? Product { get; set; }

    /// <summary>
    /// The variant option.
    /// </summary>
    public virtual VariantOption? VariantOption { get; set; }
}

/// <summary>
/// Represents a specific product variant (e.g., "T-Shirt Size:Medium Color:Blue").
/// Each variant has its own SKU, barcode, price, and inventory.
/// </summary>
public class ProductVariant : BaseEntity
{
    /// <summary>
    /// The parent product.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Unique SKU for this variant.
    /// </summary>
    public string SKU { get; set; } = string.Empty;

    /// <summary>
    /// Barcode for this variant.
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// Display name (auto-generated or custom).
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Selling price for this variant (overrides product price if set).
    /// </summary>
    public decimal? SellingPrice { get; set; }

    /// <summary>
    /// Cost price for this variant.
    /// </summary>
    public decimal? CostPrice { get; set; }

    /// <summary>
    /// Current stock quantity for this variant.
    /// </summary>
    public int StockQuantity { get; set; }

    /// <summary>
    /// Low stock threshold for this variant.
    /// </summary>
    public int? LowStockThreshold { get; set; }

    /// <summary>
    /// Reorder level for this variant.
    /// </summary>
    public int? ReorderLevel { get; set; }

    /// <summary>
    /// Weight of this variant (for shipping calculations).
    /// </summary>
    public decimal? Weight { get; set; }

    /// <summary>
    /// Weight unit (kg, lb, g, oz).
    /// </summary>
    public string? WeightUnit { get; set; }

    /// <summary>
    /// Dimensions of this variant.
    /// </summary>
    public string? Dimensions { get; set; }

    /// <summary>
    /// Image path for this variant.
    /// </summary>
    public string? ImagePath { get; set; }

    /// <summary>
    /// Whether this variant is available for sale.
    /// </summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// Whether this variant tracks inventory.
    /// </summary>
    public bool TrackInventory { get; set; } = true;

    /// <summary>
    /// Gets the effective selling price (variant price or parent product price).
    /// </summary>
    public decimal EffectivePrice => SellingPrice ?? Product?.SellingPrice ?? 0;

    /// <summary>
    /// Gets whether this variant is low on stock.
    /// </summary>
    public bool IsLowStock => TrackInventory && LowStockThreshold.HasValue && StockQuantity <= LowStockThreshold.Value && StockQuantity > 0;

    /// <summary>
    /// Gets whether this variant is out of stock.
    /// </summary>
    public bool IsOutOfStock => TrackInventory && StockQuantity <= 0;

    // Navigation properties
    /// <summary>
    /// The parent product.
    /// </summary>
    public virtual Product? Product { get; set; }

    /// <summary>
    /// The variant option values that define this variant.
    /// </summary>
    public virtual ICollection<ProductVariantValue> VariantValues { get; set; } = new List<ProductVariantValue>();

    /// <summary>
    /// Order items for this variant.
    /// </summary>
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}

/// <summary>
/// Links a product variant to a specific option value (e.g., Variant1 has Size=Medium).
/// </summary>
public class ProductVariantValue : BaseEntity
{
    /// <summary>
    /// The product variant.
    /// </summary>
    public int ProductVariantId { get; set; }

    /// <summary>
    /// The variant option value.
    /// </summary>
    public int VariantOptionValueId { get; set; }

    // Navigation properties
    /// <summary>
    /// The product variant.
    /// </summary>
    public virtual ProductVariant? ProductVariant { get; set; }

    /// <summary>
    /// The variant option value.
    /// </summary>
    public virtual VariantOptionValue? VariantOptionValue { get; set; }
}

