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
    public bool IsActive { get; set; } = true;

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
}
