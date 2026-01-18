using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using BarcodeType = HospitalityPOS.Core.Enums.BarcodeType;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for barcode, PLU, and scale operations.
/// </summary>
public interface IBarcodeService
{
    // Barcode Lookup
    Task<BarcodeLookupResult> LookupBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
    Task<Product?> GetProductByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
    Task<Product?> GetProductByPLUAsync(string pluCode, CancellationToken cancellationToken = default);

    // Product Barcodes
    Task<ProductBarcode> AddProductBarcodeAsync(int productId, string barcode, BarcodeType type, bool isPrimary = false, decimal packSize = 1, CancellationToken cancellationToken = default);
    Task<bool> RemoveProductBarcodeAsync(int barcodeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductBarcode>> GetProductBarcodesAsync(int productId, CancellationToken cancellationToken = default);
    Task<bool> ValidateBarcodeAsync(string barcode, int? excludeProductId = null, CancellationToken cancellationToken = default);

    // PLU Management
    Task<PLUCode> AddPLUCodeAsync(int productId, string code, bool isWeighted = false, decimal? tareWeight = null, CancellationToken cancellationToken = default);
    Task<PLUCode> UpdatePLUCodeAsync(PLUCode pluCode, CancellationToken cancellationToken = default);
    Task<bool> DeletePLUCodeAsync(int pluId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PLUCode>> GetAllPLUCodesAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
    Task<PLUCode?> GetPLUCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> ValidatePLUCodeAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<string> GenerateNextPLUCodeAsync(CancellationToken cancellationToken = default);

    // Weighted Barcode Config
    Task<WeightedBarcodeConfig> SaveWeightedBarcodeConfigAsync(WeightedBarcodeConfig config, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WeightedBarcodeConfig>> GetWeightedBarcodeConfigsAsync(CancellationToken cancellationToken = default);
    Task<WeightedBarcodeConfig?> GetWeightedBarcodeConfigAsync(WeightedBarcodePrefix prefix, CancellationToken cancellationToken = default);
    Task<WeightedBarcodeParseResult> ParseWeightedBarcodeAsync(string barcode, CancellationToken cancellationToken = default);

    // Scale Configuration
    Task<ScaleConfiguration> SaveScaleConfigurationAsync(ScaleConfiguration config, CancellationToken cancellationToken = default);
    Task<ScaleConfiguration?> GetActiveScaleConfigurationAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ScaleConfiguration>> GetAllScaleConfigurationsAsync(CancellationToken cancellationToken = default);
    Task<bool> ActivateScaleAsync(int scaleId, CancellationToken cancellationToken = default);
    Task<bool> TestScaleConnectionAsync(int scaleId, CancellationToken cancellationToken = default);

    // Scale Reading
    Task<ScaleReadResult> ReadScaleAsync(CancellationToken cancellationToken = default);
    Task<decimal> TareScaleAsync(CancellationToken cancellationToken = default);
    Task<bool> ZeroScaleAsync(CancellationToken cancellationToken = default);

    // Internal Barcode Generation
    Task<string> GenerateInternalBarcodeAsync(int productId, CancellationToken cancellationToken = default);
    Task<InternalBarcodeSequence> GetBarcodeSequenceAsync(CancellationToken cancellationToken = default);
    Task<InternalBarcodeSequence> UpdateBarcodeSequenceAsync(InternalBarcodeSequence sequence, CancellationToken cancellationToken = default);

    // Barcode Validation
    bool IsValidEAN13(string barcode);
    bool IsValidEAN8(string barcode);
    bool IsValidUPCA(string barcode);
    string CalculateCheckDigit(string barcode);
}

/// <summary>
/// Result of barcode lookup.
/// </summary>
public class BarcodeLookupResult
{
    public bool Found { get; set; }
    public Product? Product { get; set; }
    public ProductBarcode? ProductBarcode { get; set; }
    public PLUCode? PLUCode { get; set; }
    public bool IsWeighted { get; set; }
    public decimal? Weight { get; set; }
    public decimal? EmbeddedPrice { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal? TareWeight { get; set; }
    public string? ErrorMessage { get; set; }
    public BarcodeType? DetectedType { get; set; }
}

/// <summary>
/// Result of parsing a weighted barcode.
/// </summary>
public class WeightedBarcodeParseResult
{
    public bool Success { get; set; }
    public string? ArticleCode { get; set; }
    public decimal? Value { get; set; }
    public bool IsPrice { get; set; }
    public string? ErrorMessage { get; set; }
    public WeightedBarcodePrefix Prefix { get; set; }
}

/// <summary>
/// Result of reading from scale.
/// </summary>
public class ScaleReadResult
{
    public bool Success { get; set; }
    public decimal Weight { get; set; }
    public WeightUnit Unit { get; set; }
    public bool IsStable { get; set; }
    public ScaleStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Dashboard data for barcode/PLU management.
/// </summary>
public class BarcodeDashboardData
{
    public int TotalProducts { get; set; }
    public int ProductsWithBarcodes { get; set; }
    public int ProductsWithPLU { get; set; }
    public int WeightedProducts { get; set; }
    public int DuplicateBarcodes { get; set; }
    public bool ScaleConnected { get; set; }
    public string? ScaleName { get; set; }
    public List<WeightedBarcodeConfig> WeightedConfigs { get; set; } = [];
}
