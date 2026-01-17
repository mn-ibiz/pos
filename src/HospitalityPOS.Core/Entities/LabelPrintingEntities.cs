using System.ComponentModel.DataAnnotations;

namespace HospitalityPOS.Core.Entities;

#region Enums

/// <summary>
/// Types of label printer connections.
/// </summary>
public enum LabelPrinterType
{
    Serial = 0,     // COM port
    Network = 1,    // TCP/IP
    USB = 2,        // Direct USB
    Windows = 3     // Windows printer driver
}

/// <summary>
/// Label printing languages supported.
/// </summary>
public enum LabelPrintLanguage
{
    ZPL = 0,    // Zebra Programming Language
    EPL = 1,    // Eltron Programming Language
    TSPL = 2,   // TSC Printer Language
    Raw = 3     // Raw ESC/POS or driver-managed
}

/// <summary>
/// Types of fields that can appear on labels.
/// </summary>
public enum LabelFieldType
{
    Text = 0,
    Barcode = 1,
    Price = 2,
    Date = 3,
    QRCode = 4,
    Image = 5,
    Box = 6,
    Line = 7
}

/// <summary>
/// Text alignment options for label fields.
/// </summary>
public enum TextAlignment
{
    Left = 0,
    Center = 1,
    Right = 2
}

/// <summary>
/// Barcode types supported on labels.
/// </summary>
public enum BarcodeType
{
    Code128 = 0,
    EAN13 = 1,
    EAN8 = 2,
    UPCA = 3,
    Code39 = 4,
    QRCode = 5
}

/// <summary>
/// Types of label print jobs.
/// </summary>
public enum LabelPrintJobType
{
    Single = 0,
    Batch = 1,
    PriceChange = 2,
    Category = 3,
    NewProducts = 4
}

/// <summary>
/// Status of a label print job.
/// </summary>
public enum LabelPrintJobStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Failed = 3,
    PartiallyCompleted = 4,
    Cancelled = 5
}

/// <summary>
/// Status of an individual label print item.
/// </summary>
public enum LabelPrintItemStatus
{
    Pending = 0,
    Printed = 1,
    Failed = 2,
    Skipped = 3
}

#endregion

#region Entities

/// <summary>
/// Represents a label size configuration.
/// </summary>
public class LabelSize : BaseEntity
{
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;  // "Small", "Medium", "Large"

    public decimal WidthMm { get; set; }

    public decimal HeightMm { get; set; }

    public int DotsPerMm { get; set; } = 8;  // 203 DPI = 8 dots/mm

    [StringLength(200)]
    public string? Description { get; set; }

    // Navigation properties
    public virtual ICollection<LabelTemplate> Templates { get; set; } = new List<LabelTemplate>();
}

/// <summary>
/// Represents a configured label printer.
/// </summary>
public class LabelPrinter : BaseEntity
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;  // "Shelf Label Printer 1"

    [Required]
    [StringLength(200)]
    public string ConnectionString { get; set; } = string.Empty;  // COM port, IP, or USB path

    public int StoreId { get; set; }

    public LabelPrinterType PrinterType { get; set; }

    public LabelPrintLanguage PrintLanguage { get; set; }

    public int? DefaultLabelSizeId { get; set; }

    public bool IsDefault { get; set; }

    public LabelPrinterStatus Status { get; set; } = LabelPrinterStatus.Offline;

    public DateTime? LastConnectedAt { get; set; }

    [StringLength(500)]
    public string? LastErrorMessage { get; set; }

    // Serial port settings
    public int? BaudRate { get; set; }
    public int? DataBits { get; set; }

    // Network settings
    public int? Port { get; set; }
    public int? TimeoutMs { get; set; } = 5000;

    // Navigation properties
    public virtual LabelSize? DefaultLabelSize { get; set; }
    public virtual ICollection<CategoryPrinterAssignment> CategoryAssignments { get; set; } = new List<CategoryPrinterAssignment>();
    public virtual ICollection<LabelPrintJob> PrintJobs { get; set; } = new List<LabelPrintJob>();
}

/// <summary>
/// Printer status enumeration.
/// </summary>
public enum LabelPrinterStatus
{
    Offline = 0,
    Online = 1,
    Busy = 2,
    Error = 3
}

/// <summary>
/// Represents a label template design.
/// </summary>
public class LabelTemplate : BaseEntity
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;  // "Standard Shelf Label", "Promo Label"

    public int LabelSizeId { get; set; }

    public int StoreId { get; set; }

    public LabelPrintLanguage PrintLanguage { get; set; }

    [Required]
    public string TemplateContent { get; set; } = string.Empty;  // ZPL/EPL template with placeholders

    public bool IsDefault { get; set; }

    public bool IsPromoTemplate { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public int Version { get; set; } = 1;

    // Navigation properties
    public virtual LabelSize? LabelSize { get; set; }
    public virtual ICollection<LabelTemplateField> Fields { get; set; } = new List<LabelTemplateField>();
    public virtual ICollection<CategoryPrinterAssignment> CategoryAssignments { get; set; } = new List<CategoryPrinterAssignment>();
}

/// <summary>
/// Represents a field within a label template.
/// </summary>
public class LabelTemplateField : BaseEntity
{
    public int TemplateId { get; set; }

    [Required]
    [StringLength(50)]
    public string FieldName { get; set; } = string.Empty;  // "ProductName", "Price", "Barcode"

    public LabelFieldType FieldType { get; set; }

    public int PositionX { get; set; }  // Dots from left

    public int PositionY { get; set; }  // Dots from top

    public int Width { get; set; }

    public int Height { get; set; }

    [StringLength(50)]
    public string? FontName { get; set; }

    public int FontSize { get; set; }

    public TextAlignment Alignment { get; set; }

    public bool IsBold { get; set; }

    public int Rotation { get; set; }  // 0, 90, 180, 270 degrees

    public BarcodeType? BarcodeType { get; set; }

    public int? BarcodeHeight { get; set; }

    public bool? ShowBarcodeText { get; set; }

    public int DisplayOrder { get; set; }

    // Navigation properties
    public virtual LabelTemplate? Template { get; set; }
}

/// <summary>
/// Links categories to their default label printers and templates.
/// </summary>
public class CategoryPrinterAssignment : BaseEntity
{
    public int CategoryId { get; set; }

    public int LabelPrinterId { get; set; }

    public int? LabelTemplateId { get; set; }

    public int StoreId { get; set; }

    // Navigation properties
    public virtual Category? Category { get; set; }
    public virtual LabelPrinter? LabelPrinter { get; set; }
    public virtual LabelTemplate? LabelTemplate { get; set; }
}

/// <summary>
/// Represents a label printing job (single or batch).
/// </summary>
public class LabelPrintJob : BaseEntity
{
    public LabelPrintJobType JobType { get; set; }

    public int TotalLabels { get; set; }

    public int PrintedLabels { get; set; }

    public int FailedLabels { get; set; }

    public int SkippedLabels { get; set; }

    public LabelPrintJobStatus Status { get; set; }

    public int PrinterId { get; set; }

    public int? TemplateId { get; set; }

    public int? CategoryId { get; set; }

    public int InitiatedByUserId { get; set; }

    public int StoreId { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    [StringLength(500)]
    public string? ErrorMessage { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public int CopiesPerLabel { get; set; } = 1;

    // Navigation properties
    public virtual LabelPrinter? Printer { get; set; }
    public virtual LabelTemplate? Template { get; set; }
    public virtual Category? Category { get; set; }
    public virtual ICollection<LabelPrintJobItem> Items { get; set; } = new List<LabelPrintJobItem>();
}

/// <summary>
/// Represents an individual item in a label print job.
/// </summary>
public class LabelPrintJobItem : BaseEntity
{
    public int JobId { get; set; }

    public int ProductId { get; set; }

    [StringLength(200)]
    public string ProductName { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Barcode { get; set; }

    public decimal Price { get; set; }

    public decimal? OriginalPrice { get; set; }

    public LabelPrintItemStatus Status { get; set; }

    [StringLength(500)]
    public string? ErrorMessage { get; set; }

    public DateTime? PrintedAt { get; set; }

    public int CopiesPrinted { get; set; }

    // Navigation properties
    public virtual LabelPrintJob? Job { get; set; }
    public virtual Product? Product { get; set; }
}

/// <summary>
/// Stores standard ZPL/EPL template content.
/// </summary>
public class LabelTemplateLibrary : BaseEntity
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public LabelPrintLanguage PrintLanguage { get; set; }

    [Required]
    public string TemplateContent { get; set; } = string.Empty;

    public decimal WidthMm { get; set; }

    public decimal HeightMm { get; set; }

    public bool IsBuiltIn { get; set; }

    [StringLength(50)]
    public string Category { get; set; } = "Standard";  // "Standard", "Promo", "Clearance"
}

#endregion
