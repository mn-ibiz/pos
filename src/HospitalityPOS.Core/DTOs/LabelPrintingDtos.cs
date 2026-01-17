using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.DTOs;

#region Enums

public enum LabelPrinterTypeDto
{
    Serial = 0,
    Network = 1,
    USB = 2,
    Windows = 3
}

public enum LabelPrintLanguageDto
{
    ZPL = 0,
    EPL = 1,
    TSPL = 2,
    Raw = 3
}

public enum LabelFieldTypeDto
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

public enum TextAlignmentDto
{
    Left = 0,
    Center = 1,
    Right = 2
}

public enum BarcodeTypeDto
{
    Code128 = 0,
    EAN13 = 1,
    EAN8 = 2,
    UPCA = 3,
    Code39 = 4,
    QRCode = 5
}

public enum LabelPrintJobTypeDto
{
    Single = 0,
    Batch = 1,
    PriceChange = 2,
    Category = 3,
    NewProducts = 4
}

public enum LabelPrintJobStatusDto
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Failed = 3,
    PartiallyCompleted = 4,
    Cancelled = 5
}

public enum LabelPrintItemStatusDto
{
    Pending = 0,
    Printed = 1,
    Failed = 2,
    Skipped = 3
}

public enum LabelPrinterStatusDto
{
    Offline = 0,
    Online = 1,
    Busy = 2,
    Error = 3
}

#endregion

#region Label Size DTOs

public class LabelSizeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal WidthMm { get; set; }
    public decimal HeightMm { get; set; }
    public int DotsPerMm { get; set; }
    public string? Description { get; set; }
    public int TemplateCount { get; set; }
}

public class CreateLabelSizeDto
{
    public string Name { get; set; } = string.Empty;
    public decimal WidthMm { get; set; }
    public decimal HeightMm { get; set; }
    public int DotsPerMm { get; set; } = 8;
    public string? Description { get; set; }
}

public class UpdateLabelSizeDto
{
    public string? Name { get; set; }
    public decimal? WidthMm { get; set; }
    public decimal? HeightMm { get; set; }
    public int? DotsPerMm { get; set; }
    public string? Description { get; set; }
}

#endregion

#region Label Printer DTOs

public class LabelPrinterDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public int StoreId { get; set; }
    public LabelPrinterTypeDto PrinterType { get; set; }
    public LabelPrintLanguageDto PrintLanguage { get; set; }
    public int? DefaultLabelSizeId { get; set; }
    public string? DefaultLabelSizeName { get; set; }
    public bool IsDefault { get; set; }
    public LabelPrinterStatusDto Status { get; set; }
    public DateTime? LastConnectedAt { get; set; }
    public string? LastErrorMessage { get; set; }
    public int? BaudRate { get; set; }
    public int? Port { get; set; }
    public int? TimeoutMs { get; set; }
    public int CategoryAssignmentCount { get; set; }
}

public class CreateLabelPrinterDto
{
    public string Name { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public int StoreId { get; set; }
    public LabelPrinterTypeDto PrinterType { get; set; }
    public LabelPrintLanguageDto PrintLanguage { get; set; }
    public int? DefaultLabelSizeId { get; set; }
    public bool IsDefault { get; set; }
    public int? BaudRate { get; set; }
    public int? DataBits { get; set; }
    public int? Port { get; set; }
    public int? TimeoutMs { get; set; }
}

public class UpdateLabelPrinterDto
{
    public string? Name { get; set; }
    public string? ConnectionString { get; set; }
    public LabelPrinterTypeDto? PrinterType { get; set; }
    public LabelPrintLanguageDto? PrintLanguage { get; set; }
    public int? DefaultLabelSizeId { get; set; }
    public bool? IsDefault { get; set; }
    public int? BaudRate { get; set; }
    public int? Port { get; set; }
    public int? TimeoutMs { get; set; }
}

public class PrinterConnectionTestResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ResponseTimeMs { get; set; }
    public string? PrinterInfo { get; set; }
}

public class TestLabelResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? LabelContent { get; set; }
}

#endregion

#region Label Template DTOs

public class LabelTemplateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int LabelSizeId { get; set; }
    public string? LabelSizeName { get; set; }
    public int StoreId { get; set; }
    public LabelPrintLanguageDto PrintLanguage { get; set; }
    public string TemplateContent { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsPromoTemplate { get; set; }
    public string? Description { get; set; }
    public int Version { get; set; }
    public List<LabelTemplateFieldDto> Fields { get; set; } = new();
}

public class CreateLabelTemplateDto
{
    public string Name { get; set; } = string.Empty;
    public int LabelSizeId { get; set; }
    public int StoreId { get; set; }
    public LabelPrintLanguageDto PrintLanguage { get; set; }
    public string TemplateContent { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsPromoTemplate { get; set; }
    public string? Description { get; set; }
    public List<CreateLabelTemplateFieldDto>? Fields { get; set; }
}

public class UpdateLabelTemplateDto
{
    public string? Name { get; set; }
    public int? LabelSizeId { get; set; }
    public LabelPrintLanguageDto? PrintLanguage { get; set; }
    public string? TemplateContent { get; set; }
    public bool? IsDefault { get; set; }
    public bool? IsPromoTemplate { get; set; }
    public string? Description { get; set; }
    public List<UpdateLabelTemplateFieldDto>? Fields { get; set; }
}

public class LabelTemplateFieldDto
{
    public int Id { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public LabelFieldTypeDto FieldType { get; set; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string? FontName { get; set; }
    public int FontSize { get; set; }
    public TextAlignmentDto Alignment { get; set; }
    public bool IsBold { get; set; }
    public int Rotation { get; set; }
    public BarcodeTypeDto? BarcodeType { get; set; }
    public int? BarcodeHeight { get; set; }
    public bool? ShowBarcodeText { get; set; }
    public int DisplayOrder { get; set; }
}

public class CreateLabelTemplateFieldDto
{
    public string FieldName { get; set; } = string.Empty;
    public LabelFieldTypeDto FieldType { get; set; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string? FontName { get; set; }
    public int FontSize { get; set; }
    public TextAlignmentDto Alignment { get; set; }
    public bool IsBold { get; set; }
    public int Rotation { get; set; }
    public BarcodeTypeDto? BarcodeType { get; set; }
    public int? BarcodeHeight { get; set; }
    public bool? ShowBarcodeText { get; set; }
    public int DisplayOrder { get; set; }
}

public class UpdateLabelTemplateFieldDto : CreateLabelTemplateFieldDto
{
    public int? Id { get; set; }  // null for new fields
}

public class LabelPreviewRequestDto
{
    public int TemplateId { get; set; }
    public ProductLabelDataDto? SampleData { get; set; }
}

public class LabelPreviewResultDto
{
    public bool Success { get; set; }
    public string? PreviewImageBase64 { get; set; }
    public string? LabelContent { get; set; }
    public string? ErrorMessage { get; set; }
}

#endregion

#region Product Label Data DTOs

public class ProductLabelDataDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal Price { get; set; }
    public string? UnitPrice { get; set; }  // "KSh 50.00/kg"
    public string? Description { get; set; }
    public string? SKU { get; set; }
    public string? CategoryName { get; set; }
    public decimal? OriginalPrice { get; set; }  // For promo labels
    public string? PromoText { get; set; }
    public string? UnitOfMeasure { get; set; }
    public DateTime? EffectiveDate { get; set; }
}

#endregion

#region Category Printer Assignment DTOs

public class CategoryPrinterAssignmentDto
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int LabelPrinterId { get; set; }
    public string PrinterName { get; set; } = string.Empty;
    public int? LabelTemplateId { get; set; }
    public string? TemplateName { get; set; }
    public int StoreId { get; set; }
}

public class AssignCategoryPrinterDto
{
    public int CategoryId { get; set; }
    public int LabelPrinterId { get; set; }
    public int? LabelTemplateId { get; set; }
}

#endregion

#region Label Print Job DTOs

public class LabelPrintJobDto
{
    public int Id { get; set; }
    public LabelPrintJobTypeDto JobType { get; set; }
    public int TotalLabels { get; set; }
    public int PrintedLabels { get; set; }
    public int FailedLabels { get; set; }
    public int SkippedLabels { get; set; }
    public LabelPrintJobStatusDto Status { get; set; }
    public int PrinterId { get; set; }
    public string? PrinterName { get; set; }
    public int? TemplateId { get; set; }
    public string? TemplateName { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int InitiatedByUserId { get; set; }
    public string? InitiatedByUserName { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Notes { get; set; }
    public int CopiesPerLabel { get; set; }
    public double ProgressPercent { get; set; }
    public TimeSpan? Duration { get; set; }
    public List<LabelPrintJobItemDto> Items { get; set; } = new();
}

public class LabelPrintJobItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public LabelPrintItemStatusDto Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? PrintedAt { get; set; }
    public int CopiesPrinted { get; set; }
}

public class LabelBatchRequestDto
{
    public List<int>? ProductIds { get; set; }
    public int? CategoryId { get; set; }
    public int? TemplateId { get; set; }
    public int? PrinterId { get; set; }
    public int CopiesPerLabel { get; set; } = 1;
    public bool IncludeInactive { get; set; }
    public string? Notes { get; set; }
}

public class PrintSingleLabelRequestDto
{
    public int ProductId { get; set; }
    public int? TemplateId { get; set; }
    public int? PrinterId { get; set; }
    public int Copies { get; set; } = 1;
}

public class PrintPriceChangeLabelsRequestDto
{
    public DateTime Since { get; set; }
    public int? CategoryId { get; set; }
    public int? TemplateId { get; set; }
    public int? PrinterId { get; set; }
    public int CopiesPerLabel { get; set; } = 1;
}

public class PrintCategoryLabelsRequestDto
{
    public int CategoryId { get; set; }
    public int? TemplateId { get; set; }
    public int? PrinterId { get; set; }
    public int CopiesPerLabel { get; set; } = 1;
    public bool IncludeSubcategories { get; set; }
}

public class GetPrintJobHistoryRequestDto
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public LabelPrintJobTypeDto? JobType { get; set; }
    public LabelPrintJobStatusDto? Status { get; set; }
    public int? PrinterId { get; set; }
    public int? InitiatedByUserId { get; set; }
}

#endregion

#region Template Library DTOs

public class LabelTemplateLibraryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public LabelPrintLanguageDto PrintLanguage { get; set; }
    public string TemplateContent { get; set; } = string.Empty;
    public decimal WidthMm { get; set; }
    public decimal HeightMm { get; set; }
    public bool IsBuiltIn { get; set; }
    public string Category { get; set; } = "Standard";
}

public class ImportTemplateFromLibraryDto
{
    public int LibraryTemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int LabelSizeId { get; set; }
    public int StoreId { get; set; }
}

#endregion

#region Statistics DTOs

public class LabelPrintingStatisticsDto
{
    public int TotalLabelsToday { get; set; }
    public int TotalJobsToday { get; set; }
    public int FailedLabelsToday { get; set; }
    public int PendingJobs { get; set; }
    public Dictionary<string, int> LabelsByPrinter { get; set; } = new();
    public Dictionary<string, int> LabelsByCategory { get; set; } = new();
    public double AverageJobDurationSeconds { get; set; }
    public DateTime? LastPrintTime { get; set; }
}

#endregion
