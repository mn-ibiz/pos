using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// DTO for creating a new product.
/// </summary>
public class CreateProductDto
{
    /// <summary>
    /// Gets or sets the product code/SKU.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the category ID.
    /// </summary>
    public int CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the selling price.
    /// </summary>
    public decimal SellingPrice { get; set; }

    /// <summary>
    /// Gets or sets the cost price.
    /// </summary>
    public decimal? CostPrice { get; set; }

    /// <summary>
    /// Gets or sets the tax rate (default 16% for Kenya VAT).
    /// </summary>
    public decimal TaxRate { get; set; } = 16.00m;

    /// <summary>
    /// Gets or sets the unit of measure.
    /// </summary>
    public string UnitOfMeasure { get; set; } = "Each";

    /// <summary>
    /// Gets or sets the image path.
    /// </summary>
    public string? ImagePath { get; set; }

    /// <summary>
    /// Gets or sets the barcode/QR code.
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// Gets or sets the minimum stock level.
    /// </summary>
    public decimal? MinStockLevel { get; set; }

    /// <summary>
    /// Gets or sets the maximum stock level.
    /// </summary>
    public decimal? MaxStockLevel { get; set; }

    /// <summary>
    /// Gets or sets whether the product is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the initial stock quantity.
    /// </summary>
    public decimal InitialStock { get; set; } = 0;
}

/// <summary>
/// DTO for updating an existing product.
/// </summary>
public class UpdateProductDto
{
    /// <summary>
    /// Gets or sets the product code/SKU.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the category ID.
    /// </summary>
    public int CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the selling price.
    /// </summary>
    public decimal SellingPrice { get; set; }

    /// <summary>
    /// Gets or sets the cost price.
    /// </summary>
    public decimal? CostPrice { get; set; }

    /// <summary>
    /// Gets or sets the tax rate.
    /// </summary>
    public decimal TaxRate { get; set; } = 16.00m;

    /// <summary>
    /// Gets or sets the unit of measure.
    /// </summary>
    public string UnitOfMeasure { get; set; } = "Each";

    /// <summary>
    /// Gets or sets the image path.
    /// </summary>
    public string? ImagePath { get; set; }

    /// <summary>
    /// Gets or sets the barcode/QR code.
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// Gets or sets the minimum stock level.
    /// </summary>
    public decimal? MinStockLevel { get; set; }

    /// <summary>
    /// Gets or sets the maximum stock level.
    /// </summary>
    public decimal? MaxStockLevel { get; set; }

    /// <summary>
    /// Gets or sets whether the product is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Service interface for managing products.
/// </summary>
public interface IProductService
{
    /// <summary>
    /// Gets all products.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All products.</returns>
    Task<IReadOnlyList<Product>> GetAllProductsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active products.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Active products.</returns>
    Task<IReadOnlyList<Product>> GetActiveProductsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets products by category.
    /// </summary>
    /// <param name="categoryId">The category ID.</param>
    /// <param name="includeInactive">Include inactive products.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Products in the category.</returns>
    Task<IReadOnlyList<Product>> GetByCategoryAsync(int categoryId, bool includeInactive = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a product by ID.
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The product if found; otherwise, null.</returns>
    Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a product by code/SKU.
    /// </summary>
    /// <param name="code">The product code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The product if found; otherwise, null.</returns>
    Task<Product?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a product by barcode.
    /// </summary>
    /// <param name="barcode">The barcode.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The product if found; otherwise, null.</returns>
    Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new product.
    /// </summary>
    /// <param name="dto">The product data.</param>
    /// <param name="createdByUserId">The ID of the user creating the product.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created product.</returns>
    Task<Product> CreateProductAsync(CreateProductDto dto, int createdByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing product.
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <param name="dto">The updated product data.</param>
    /// <param name="modifiedByUserId">The ID of the user modifying the product.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated product.</returns>
    Task<Product> UpdateProductAsync(int id, UpdateProductDto dto, int modifiedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates or deactivates a product.
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <param name="isActive">The new active status.</param>
    /// <param name="modifiedByUserId">The ID of the user modifying the product.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated product.</returns>
    Task<Product> SetProductActiveAsync(int id, bool isActive, int modifiedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a product (soft delete).
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <param name="deletedByUserId">The ID of the user deleting the product.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted; false if not found.</returns>
    Task<bool> DeleteProductAsync(int id, int deletedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a product code is unique.
    /// </summary>
    /// <param name="code">The product code.</param>
    /// <param name="excludeProductId">Product ID to exclude from check (for updates).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the code is unique.</returns>
    Task<bool> IsCodeUniqueAsync(string code, int? excludeProductId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a barcode is unique.
    /// </summary>
    /// <param name="barcode">The barcode.</param>
    /// <param name="excludeProductId">Product ID to exclude from check (for updates).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the barcode is unique.</returns>
    Task<bool> IsBarcodeUniqueAsync(string barcode, int? excludeProductId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for products by name, code, or barcode.
    /// </summary>
    /// <param name="searchTerm">The search term.</param>
    /// <param name="categoryId">Optional category filter.</param>
    /// <param name="activeOnly">Only include active products.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching products.</returns>
    Task<IReadOnlyList<Product>> SearchAsync(string searchTerm, int? categoryId = null, bool activeOnly = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets products with low stock (below minimum level).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Products with low stock.</returns>
    Task<IReadOnlyList<Product>> GetLowStockProductsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of products by category.
    /// </summary>
    /// <param name="categoryId">The category ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of products in the category.</returns>
    Task<int> GetProductCountByCategoryAsync(int categoryId, CancellationToken cancellationToken = default);
}
