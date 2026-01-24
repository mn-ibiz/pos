namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a product category for organizing products.
/// </summary>
public class Category : BaseEntity
{
    /// <summary>
    /// Gets or sets the category name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parent category ID for subcategories.
    /// </summary>
    public int? ParentCategoryId { get; set; }

    /// <summary>
    /// Gets or sets the category image path.
    /// </summary>
    public string? ImagePath { get; set; }

    /// <summary>
    /// Gets or sets the display order for sorting.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Gets or sets whether the category is active.
    /// </summary>
    public new bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this category is soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    // Navigation properties
    /// <summary>
    /// Gets or sets the parent category for subcategories.
    /// </summary>
    public virtual Category? ParentCategory { get; set; }

    /// <summary>
    /// Gets or sets the subcategories.
    /// </summary>
    public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();

    /// <summary>
    /// Gets or sets the products in this category.
    /// </summary>
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    /// <summary>
    /// Default modifier groups for products in this category.
    /// </summary>
    public virtual ICollection<CategoryModifierGroup> DefaultModifierGroups { get; set; } = new List<CategoryModifierGroup>();

    /// <summary>
    /// Modifier presets for products in this category.
    /// </summary>
    public virtual ICollection<ModifierPreset> ModifierPresets { get; set; } = new List<ModifierPreset>();

    // Loyalty points configuration
    /// <summary>
    /// Default points multiplier for products in this category.
    /// 1.0 = normal rate, 2.0 = double points, 0.5 = half points, 0 = no points.
    /// Null means use global default. Product-level multiplier takes precedence.
    /// </summary>
    public decimal? PointsMultiplier { get; set; }

    /// <summary>
    /// Whether products in this category are excluded from earning loyalty points by default.
    /// Product-level setting takes precedence.
    /// </summary>
    public bool ExcludeFromLoyaltyPoints { get; set; }
}
