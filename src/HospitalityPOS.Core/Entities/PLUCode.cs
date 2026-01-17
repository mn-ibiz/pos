using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// PLU (Price Look-Up) code for quick product entry.
/// </summary>
public class PLUCode : BaseEntity
{
    /// <summary>
    /// The PLU number (typically 4-5 digits).
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Related product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Display name (if different from product name).
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Whether this is an active PLU.
    /// </summary>
    public new bool IsActive { get; set; } = true;

    /// <summary>
    /// Sort order for display.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Whether this PLU is for a weighted item.
    /// </summary>
    public bool IsWeighted { get; set; }

    /// <summary>
    /// Tare weight in grams (for weighted items).
    /// </summary>
    public decimal? TareWeight { get; set; }

    // Navigation properties
    public Product Product { get; set; } = null!;
}

/// <summary>
/// Barcode configuration for weighted/priced items.
/// </summary>
public class WeightedBarcodeConfig : BaseEntity
{
    /// <summary>
    /// Name of the configuration.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Barcode prefix (e.g., 20, 21, 22-29).
    /// </summary>
    public WeightedBarcodePrefix Prefix { get; set; }

    /// <summary>
    /// Format of the barcode.
    /// </summary>
    public WeightedBarcodeFormat Format { get; set; }

    /// <summary>
    /// Start position of article code in barcode (0-indexed).
    /// </summary>
    public int ArticleCodeStart { get; set; } = 2;

    /// <summary>
    /// Length of article code digits.
    /// </summary>
    public int ArticleCodeLength { get; set; } = 5;

    /// <summary>
    /// Start position of value (price/weight) in barcode.
    /// </summary>
    public int ValueStart { get; set; } = 7;

    /// <summary>
    /// Length of value digits.
    /// </summary>
    public int ValueLength { get; set; } = 5;

    /// <summary>
    /// Number of decimal places in value.
    /// </summary>
    public int ValueDecimals { get; set; } = 2;

    /// <summary>
    /// Whether the value is price (true) or weight (false).
    /// </summary>
    public bool IsPrice { get; set; } = true;

    /// <summary>
    /// Whether this is the active configuration for the prefix.
    /// </summary>
    public new bool IsActive { get; set; } = true;

    /// <summary>
    /// Notes/description.
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Scale configuration.
/// </summary>
public class ScaleConfiguration : BaseEntity
{
    /// <summary>
    /// Name of the scale.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type of scale.
    /// </summary>
    public ScaleType ScaleType { get; set; }

    /// <summary>
    /// Communication protocol.
    /// </summary>
    public ScaleProtocol Protocol { get; set; }

    /// <summary>
    /// Connection string (COM port, IP address, etc.).
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Baud rate for serial connection.
    /// </summary>
    public int? BaudRate { get; set; }

    /// <summary>
    /// Data bits for serial connection.
    /// </summary>
    public int? DataBits { get; set; }

    /// <summary>
    /// Stop bits for serial connection.
    /// </summary>
    public int? StopBits { get; set; }

    /// <summary>
    /// Parity for serial connection.
    /// </summary>
    public string? Parity { get; set; }

    /// <summary>
    /// Port number for network connection.
    /// </summary>
    public int? Port { get; set; }

    /// <summary>
    /// Weight unit used by scale.
    /// </summary>
    public WeightUnit WeightUnit { get; set; } = WeightUnit.Kilograms;

    /// <summary>
    /// Number of decimal places reported by scale.
    /// </summary>
    public int Decimals { get; set; } = 3;

    /// <summary>
    /// Minimum weight threshold to consider valid.
    /// </summary>
    public decimal MinWeight { get; set; } = 0.005m;

    /// <summary>
    /// Maximum weight capacity.
    /// </summary>
    public decimal MaxWeight { get; set; } = 30.0m;

    /// <summary>
    /// Whether this is the active scale.
    /// </summary>
    public new bool IsActive { get; set; }

    /// <summary>
    /// Last connection status.
    /// </summary>
    public ScaleStatus LastStatus { get; set; } = ScaleStatus.Disconnected;

    /// <summary>
    /// Last connection timestamp.
    /// </summary>
    public DateTime? LastConnectedAt { get; set; }

    /// <summary>
    /// Notes/description.
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Product barcode mapping.
/// </summary>
public class ProductBarcode : BaseEntity
{
    /// <summary>
    /// Related product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Barcode value.
    /// </summary>
    public string Barcode { get; set; } = string.Empty;

    /// <summary>
    /// Barcode type.
    /// </summary>
    public BarcodeType BarcodeType { get; set; }

    /// <summary>
    /// Whether this is the primary barcode for the product.
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Pack size for this barcode (1 for singles).
    /// </summary>
    public decimal PackSize { get; set; } = 1;

    /// <summary>
    /// Description (e.g., "6-pack", "case of 24").
    /// </summary>
    public string? Description { get; set; }

    // Navigation properties
    public Product Product { get; set; } = null!;
}

/// <summary>
/// Internal/store-generated barcode sequence.
/// </summary>
public class InternalBarcodeSequence : BaseEntity
{
    /// <summary>
    /// Prefix for internal barcodes.
    /// </summary>
    public string Prefix { get; set; } = "200";

    /// <summary>
    /// Last sequence number used.
    /// </summary>
    public int LastSequenceNumber { get; set; }

    /// <summary>
    /// Number of digits for sequence portion.
    /// </summary>
    public int SequenceDigits { get; set; } = 9;
}
