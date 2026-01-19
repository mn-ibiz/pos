namespace HospitalityPOS.WPF.ViewModels.Models;

/// <summary>
/// Represents a product search result for the POS real-time search dropdown.
/// </summary>
public class ProductSearchResult
{
    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product barcode.
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// Gets or sets the selling price.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the current stock level.
    /// </summary>
    public decimal CurrentStock { get; set; }

    /// <summary>
    /// Gets or sets whether the product is out of stock.
    /// </summary>
    public bool IsOutOfStock { get; set; }

    /// <summary>
    /// Gets or sets whether the product is low on stock.
    /// </summary>
    public bool IsLowStock { get; set; }

    /// <summary>
    /// Gets or sets the product image path.
    /// </summary>
    public string? ImagePath { get; set; }

    /// <summary>
    /// Gets or sets whether the product has an image.
    /// </summary>
    public bool HasImage => !string.IsNullOrEmpty(ImagePath);
}
