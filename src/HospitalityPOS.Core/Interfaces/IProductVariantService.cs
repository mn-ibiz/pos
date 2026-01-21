using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Interfaces;

#region DTOs

/// <summary>
/// DTO for creating or updating a variant option.
/// </summary>
public class VariantOptionDto
{
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public VariantOptionType OptionType { get; set; } = VariantOptionType.Custom;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsGlobal { get; set; } = true;
    public List<VariantOptionValueDto> Values { get; set; } = new();
}

/// <summary>
/// DTO for creating or updating a variant option value.
/// </summary>
public class VariantOptionValueDto
{
    public int Id { get; set; }
    public string Value { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? ColorCode { get; set; }
    public string? ImagePath { get; set; }
    public decimal PriceAdjustment { get; set; }
    public bool IsPriceAdjustmentPercent { get; set; }
    public int DisplayOrder { get; set; }
    public string? SkuSuffix { get; set; }
}

/// <summary>
/// DTO for creating a product variant.
/// </summary>
public class ProductVariantDto
{
    public string SKU { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string? DisplayName { get; set; }
    public decimal? SellingPrice { get; set; }
    public decimal? CostPrice { get; set; }
    public int StockQuantity { get; set; }
    public int? LowStockThreshold { get; set; }
    public int? ReorderLevel { get; set; }
    public decimal? Weight { get; set; }
    public string? WeightUnit { get; set; }
    public string? Dimensions { get; set; }
    public string? ImagePath { get; set; }
    public bool IsAvailable { get; set; } = true;
    public bool TrackInventory { get; set; } = true;
    public List<int> VariantOptionValueIds { get; set; } = new();
}

/// <summary>
/// DTO for linking a variant option to a product.
/// </summary>
public class ProductVariantOptionDto
{
    public int VariantOptionId { get; set; }
    public bool IsRequired { get; set; } = true;
    public int DisplayOrder { get; set; }
    public List<int> SelectedValueIds { get; set; } = new();
}

/// <summary>
/// DTO for creating a product barcode.
/// </summary>
public class ProductBarcodeDto
{
    public int? ProductVariantId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public Enums.BarcodeType BarcodeType { get; set; } = Enums.BarcodeType.EAN13;
    public bool IsPrimary { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// DTO for bulk variant generation.
/// </summary>
public class VariantGenerationDto
{
    public int ProductId { get; set; }
    public List<ProductVariantOptionDto> VariantOptions { get; set; } = new();
    public string SkuPrefix { get; set; } = string.Empty;
    public bool GenerateBarcodes { get; set; }
    public decimal? DefaultPrice { get; set; }
    public bool TrackInventory { get; set; } = true;
}

/// <summary>
/// Result of variant generation.
/// </summary>
public class VariantGenerationResult
{
    public int TotalGenerated { get; set; }
    public int SkippedDuplicates { get; set; }
    public List<ProductVariant> GeneratedVariants { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

#endregion

/// <summary>
/// Service interface for managing product variants.
/// </summary>
public interface IProductVariantService
{
    #region Variant Options

    /// <summary>
    /// Gets all variant options.
    /// </summary>
    Task<IReadOnlyList<VariantOption>> GetAllVariantOptionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets global variant options.
    /// </summary>
    Task<IReadOnlyList<VariantOption>> GetGlobalVariantOptionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a variant option by ID with its values.
    /// </summary>
    Task<VariantOption?> GetVariantOptionByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new variant option with values.
    /// </summary>
    Task<VariantOption> CreateVariantOptionAsync(VariantOptionDto dto, int createdByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a variant option and its values.
    /// </summary>
    Task<VariantOption> UpdateVariantOptionAsync(int id, VariantOptionDto dto, int modifiedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a variant option.
    /// </summary>
    Task<bool> DeleteVariantOptionAsync(int id, int deletedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a value to a variant option.
    /// </summary>
    Task<VariantOptionValue> AddVariantOptionValueAsync(int optionId, VariantOptionValueDto dto, int createdByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a variant option value.
    /// </summary>
    Task<VariantOptionValue> UpdateVariantOptionValueAsync(int valueId, VariantOptionValueDto dto, int modifiedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a variant option value.
    /// </summary>
    Task<bool> DeleteVariantOptionValueAsync(int valueId, int deletedByUserId, CancellationToken cancellationToken = default);

    #endregion

    #region Product Variants

    /// <summary>
    /// Gets all variants for a product.
    /// </summary>
    Task<IReadOnlyList<ProductVariant>> GetProductVariantsAsync(int productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available variants for a product (in stock and active).
    /// </summary>
    Task<IReadOnlyList<ProductVariant>> GetAvailableProductVariantsAsync(int productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a product variant by ID.
    /// </summary>
    Task<ProductVariant?> GetVariantByIdAsync(int variantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a product variant by SKU.
    /// </summary>
    Task<ProductVariant?> GetVariantBySkuAsync(string sku, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a product variant by barcode.
    /// </summary>
    Task<ProductVariant?> GetVariantByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new product variant.
    /// </summary>
    Task<ProductVariant> CreateVariantAsync(int productId, ProductVariantDto dto, int createdByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a product variant.
    /// </summary>
    Task<ProductVariant> UpdateVariantAsync(int variantId, ProductVariantDto dto, int modifiedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a product variant.
    /// </summary>
    Task<bool> DeleteVariantAsync(int variantId, int deletedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets variant availability.
    /// </summary>
    Task<ProductVariant> SetVariantAvailabilityAsync(int variantId, bool isAvailable, int modifiedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates variant stock.
    /// </summary>
    Task<ProductVariant> UpdateVariantStockAsync(int variantId, int quantity, int modifiedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adjusts variant stock by a delta amount.
    /// </summary>
    Task<ProductVariant> AdjustVariantStockAsync(int variantId, int delta, string reason, int modifiedByUserId, CancellationToken cancellationToken = default);

    #endregion

    #region Product Variant Configuration

    /// <summary>
    /// Gets variant options configured for a product.
    /// </summary>
    Task<IReadOnlyList<ProductVariantOption>> GetProductVariantOptionsAsync(int productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Links variant options to a product.
    /// </summary>
    Task LinkVariantOptionsToProductAsync(int productId, List<ProductVariantOptionDto> options, int modifiedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a variant option from a product.
    /// </summary>
    Task RemoveVariantOptionFromProductAsync(int productId, int variantOptionId, int modifiedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates all possible variant combinations for a product.
    /// </summary>
    Task<VariantGenerationResult> GenerateVariantsAsync(VariantGenerationDto dto, int createdByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the variant matrix for a product (all option combinations).
    /// </summary>
    Task<List<List<VariantOptionValue>>> GetVariantMatrixAsync(int productId, CancellationToken cancellationToken = default);

    #endregion

    #region Product Barcodes

    /// <summary>
    /// Gets all barcodes for a product.
    /// </summary>
    Task<IReadOnlyList<ProductBarcode>> GetProductBarcodesAsync(int productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a product or variant by any barcode.
    /// </summary>
    Task<(Product? Product, ProductVariant? Variant)> FindByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a barcode to a product or variant.
    /// </summary>
    Task<ProductBarcode> AddBarcodeAsync(int productId, ProductBarcodeDto dto, int createdByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a barcode.
    /// </summary>
    Task<ProductBarcode> UpdateBarcodeAsync(int barcodeId, ProductBarcodeDto dto, int modifiedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a barcode.
    /// </summary>
    Task<bool> DeleteBarcodeAsync(int barcodeId, int deletedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the primary barcode for a product or variant.
    /// </summary>
    Task SetPrimaryBarcodeAsync(int barcodeId, int modifiedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a barcode format.
    /// </summary>
    bool ValidateBarcodeFormat(string barcode, string barcodeType);

    /// <summary>
    /// Generates a unique barcode.
    /// </summary>
    Task<string> GenerateBarcodeAsync(string barcodeType = "EAN13", CancellationToken cancellationToken = default);

    #endregion

    #region Validation

    /// <summary>
    /// Checks if a variant SKU is unique.
    /// </summary>
    Task<bool> IsSkuUniqueAsync(string sku, int? excludeVariantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a barcode is unique.
    /// </summary>
    Task<bool> IsBarcodeUniqueAsync(string barcode, int? excludeBarcodeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates variant option values for a product.
    /// </summary>
    Task<bool> ValidateVariantOptionsAsync(int productId, List<int> optionValueIds, CancellationToken cancellationToken = default);

    #endregion

    #region Statistics

    /// <summary>
    /// Gets variant statistics.
    /// </summary>
    Task<VariantStatistics> GetVariantStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of products using a specific variant option.
    /// </summary>
    Task<int> GetVariantOptionUsageCountAsync(int variantOptionId, CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Statistics about product variants.
/// </summary>
public class VariantStatistics
{
    public int TotalVariantOptions { get; set; }
    public int ActiveVariantOptions { get; set; }
    public int TotalVariantValues { get; set; }
    public int ProductsWithVariants { get; set; }
    public int TotalProductVariants { get; set; }
}
