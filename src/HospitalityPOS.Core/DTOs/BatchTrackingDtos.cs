namespace HospitalityPOS.Core.DTOs;

#region Batch DTOs

/// <summary>
/// DTO for product batch information.
/// </summary>
public class ProductBatchDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public int InitialQuantity { get; set; }
    public int CurrentQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int SoldQuantity { get; set; }
    public int DisposedQuantity { get; set; }
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public int? GrnId { get; set; }
    public string? GrnNumber { get; set; }
    public int? TransferReceiptId { get; set; }
    public DateTime ReceivedAt { get; set; }
    public int ReceivedByUserId { get; set; }
    public string ReceivedByUserName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public string? Notes { get; set; }

    // Calculated fields
    public int? DaysUntilExpiry { get; set; }
    public bool IsExpired { get; set; }
    public int AvailableQuantity { get; set; }
    public decimal TotalValue => CurrentQuantity * UnitCost;
}

/// <summary>
/// DTO for creating a new product batch.
/// </summary>
public class CreateProductBatchDto
{
    public int ProductId { get; set; }
    public int StoreId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public int Quantity { get; set; }
    public int? SupplierId { get; set; }
    public int? GrnId { get; set; }
    public int? TransferReceiptId { get; set; }
    public decimal UnitCost { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for batch entry during goods receiving.
/// </summary>
public class BatchReceivingEntryDto
{
    public int ProductId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for batch selection during sales or transfers.
/// </summary>
public class BatchSelectionDto
{
    public int BatchId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public int AvailableQuantity { get; set; }
    public int? DaysUntilExpiry { get; set; }
    public bool IsExpired { get; set; }
    public bool IsNearExpiry { get; set; }
    public decimal UnitCost { get; set; }
    public DateTime ReceivedAt { get; set; }
}

/// <summary>
/// DTO for batch summary per product.
/// </summary>
public class ProductBatchSummaryDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public int TotalBatches { get; set; }
    public int ActiveBatches { get; set; }
    public int ExpiredBatches { get; set; }
    public int NearExpiryBatches { get; set; }
    public int TotalQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public decimal TotalValue { get; set; }
    public DateTime? EarliestExpiry { get; set; }
    public DateTime? LatestExpiry { get; set; }
}

/// <summary>
/// Query parameters for batch searches.
/// </summary>
public class BatchQueryDto
{
    public int? ProductId { get; set; }
    public int? StoreId { get; set; }
    public string? BatchNumber { get; set; }
    public string? Status { get; set; }
    public bool? ExpiringWithinDays { get; set; }
    public int? DaysUntilExpiry { get; set; }
    public bool? IncludeExpired { get; set; }
    public bool? HasAvailableStock { get; set; }
    public DateTime? ExpiryDateFrom { get; set; }
    public DateTime? ExpiryDateTo { get; set; }
    public DateTime? ReceivedDateFrom { get; set; }
    public DateTime? ReceivedDateTo { get; set; }
    public int? SupplierId { get; set; }
}

#endregion

#region Batch Configuration DTOs

/// <summary>
/// DTO for product batch configuration.
/// </summary>
public class ProductBatchConfigurationDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public bool RequiresBatchTracking { get; set; }
    public bool RequiresExpiryDate { get; set; }
    public int ExpiryWarningDays { get; set; }
    public int ExpiryCriticalDays { get; set; }
    public string ExpiredItemAction { get; set; } = string.Empty;
    public string NearExpiryAction { get; set; } = string.Empty;
    public bool UseFifo { get; set; }
    public bool UseFefo { get; set; }
    public bool TrackManufactureDate { get; set; }
    public int MinimumShelfLifeDaysOnReceipt { get; set; }
}

/// <summary>
/// DTO for creating or updating product batch configuration.
/// </summary>
public class UpdateProductBatchConfigurationDto
{
    public int ProductId { get; set; }
    public bool RequiresBatchTracking { get; set; }
    public bool RequiresExpiryDate { get; set; }
    public int ExpiryWarningDays { get; set; } = 30;
    public int ExpiryCriticalDays { get; set; } = 7;
    public string ExpiredItemAction { get; set; } = "Block";
    public string NearExpiryAction { get; set; } = "Warn";
    public bool UseFifo { get; set; } = true;
    public bool UseFefo { get; set; } = true;
    public bool TrackManufactureDate { get; set; }
    public int MinimumShelfLifeDaysOnReceipt { get; set; }
}

#endregion

#region Batch Movement DTOs

/// <summary>
/// DTO for batch stock movement.
/// </summary>
public class BatchStockMovementDto
{
    public int Id { get; set; }
    public int BatchId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string MovementType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int QuantityBefore { get; set; }
    public int QuantityAfter { get; set; }
    public string ReferenceType { get; set; } = string.Empty;
    public int ReferenceId { get; set; }
    public string? ReferenceNumber { get; set; }
    public DateTime MovedAt { get; set; }
    public int? MovedByUserId { get; set; }
    public string? MovedByUserName { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalValue { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for recording a batch movement.
/// </summary>
public class RecordBatchMovementDto
{
    public int BatchId { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string ReferenceType { get; set; } = string.Empty;
    public int ReferenceId { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
}

#endregion

#region Batch Disposal DTOs

/// <summary>
/// DTO for batch disposal record.
/// </summary>
public class BatchDisposalDto
{
    public int Id { get; set; }
    public int BatchId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DisposedAt { get; set; }
    public int ApprovedByUserId { get; set; }
    public string ApprovedByUserName { get; set; } = string.Empty;
    public int DisposedByUserId { get; set; }
    public string DisposedByUserName { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public decimal TotalValue { get; set; }
    public bool IsWitnessed { get; set; }
    public string? WitnessName { get; set; }
    public string? PhotoPath { get; set; }
}

/// <summary>
/// DTO for creating a batch disposal.
/// </summary>
public class CreateBatchDisposalDto
{
    public int BatchId { get; set; }
    public int Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ApprovedByUserId { get; set; }
    public bool IsWitnessed { get; set; }
    public string? WitnessName { get; set; }
    public string? PhotoPath { get; set; }
}

#endregion

#region Expiry Validation DTOs

/// <summary>
/// DTO for expiry validation result.
/// </summary>
public class ExpiryValidationResultDto
{
    public bool IsValid { get; set; }
    public bool RequiresWarning { get; set; }
    public bool IsBlocked { get; set; }
    public bool RequiresManagerOverride { get; set; }
    public string? Message { get; set; }
    public int? DaysUntilExpiry { get; set; }
    public string? ValidationAction { get; set; }
}

/// <summary>
/// DTO for batch availability check during sale.
/// </summary>
public class BatchAvailabilityDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int RequestedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public bool HasSufficientStock { get; set; }
    public bool HasExpiryIssues { get; set; }
    public List<BatchSelectionDto> AvailableBatches { get; set; } = new();
    public List<BatchSelectionDto> SuggestedBatches { get; set; } = new();
    public ExpiryValidationResultDto? ExpiryValidation { get; set; }
}

/// <summary>
/// DTO for receiving validation on minimum shelf life.
/// </summary>
public class ShelfLifeValidationDto
{
    public bool IsValid { get; set; }
    public int MinimumShelfLifeDays { get; set; }
    public int ActualShelfLifeDays { get; set; }
    public string? Message { get; set; }
}

#endregion

#region Batch Allocation DTOs

/// <summary>
/// DTO for batch allocation during sale.
/// </summary>
public class BatchAllocationDto
{
    public int BatchId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public int AllocatedQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

/// <summary>
/// Request DTO for allocating batches.
/// </summary>
public class AllocateBatchesRequestDto
{
    public int ProductId { get; set; }
    public int StoreId { get; set; }
    public int Quantity { get; set; }
    public bool UseFifo { get; set; } = true;
    public bool UseFefo { get; set; } = true;
    public bool AllowExpired { get; set; }
    public bool AllowNearExpiry { get; set; } = true;
}

/// <summary>
/// Response DTO for batch allocation result.
/// </summary>
public class BatchAllocationResultDto
{
    public bool Success { get; set; }
    public int TotalAllocated { get; set; }
    public int Shortfall { get; set; }
    public List<BatchAllocationDto> Allocations { get; set; } = new();
    public ExpiryValidationResultDto? ExpiryValidation { get; set; }
    public string? Message { get; set; }
}

#endregion

#region Expiry Dashboard DTOs

/// <summary>
/// Severity level for expiry alerts.
/// </summary>
public enum ExpiryAlertSeverity
{
    /// <summary>30+ days until expiry.</summary>
    Info = 1,
    /// <summary>14-30 days until expiry.</summary>
    Warning = 2,
    /// <summary>7-14 days until expiry.</summary>
    Urgent = 3,
    /// <summary>0-7 days until expiry (critical).</summary>
    Critical = 4,
    /// <summary>Already expired.</summary>
    Expired = 5
}

/// <summary>
/// Suggested action for expiring/expired items.
/// </summary>
public enum SuggestedAction
{
    /// <summary>Apply discount to encourage sale.</summary>
    Markdown = 1,
    /// <summary>Remove from sales shelf.</summary>
    RemoveFromShelf = 2,
    /// <summary>Dispose of the item.</summary>
    Dispose = 3,
    /// <summary>Return to supplier if agreement exists.</summary>
    ReturnToSupplier = 4,
    /// <summary>Prioritize in sales.</summary>
    PrioritizeSale = 5,
    /// <summary>Transfer to another location.</summary>
    Transfer = 6
}

/// <summary>
/// DTO for the expiry alert dashboard.
/// </summary>
public class ExpiryDashboardDto
{
    public int TotalExpiredItems { get; set; }
    public decimal TotalExpiredValue { get; set; }
    public int TotalExpiringItems { get; set; }
    public decimal TotalExpiringValue { get; set; }
    public int TotalBatchesRequiringAction { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public List<ExpiryGroupDto> ExpiryGroups { get; set; } = new();
    public ExpiryDashboardSummaryDto Summary { get; set; } = new();
}

/// <summary>
/// Summary statistics for the expiry dashboard.
/// </summary>
public class ExpiryDashboardSummaryDto
{
    public int ExpiredCount { get; set; }
    public decimal ExpiredValue { get; set; }
    public int CriticalCount { get; set; }
    public decimal CriticalValue { get; set; }
    public int UrgentCount { get; set; }
    public decimal UrgentValue { get; set; }
    public int WarningCount { get; set; }
    public decimal WarningValue { get; set; }
    public int InfoCount { get; set; }
    public decimal InfoValue { get; set; }
}

/// <summary>
/// DTO for grouping expiring batches by time period.
/// </summary>
public class ExpiryGroupDto
{
    public string Period { get; set; } = string.Empty;
    public int DaysFrom { get; set; }
    public int DaysTo { get; set; }
    public ExpiryAlertSeverity Severity { get; set; }
    public int ItemCount { get; set; }
    public int TotalQuantity { get; set; }
    public decimal TotalValue { get; set; }
    public List<ExpiringBatchDto> Batches { get; set; } = new();
}

/// <summary>
/// DTO for an expiring batch with action suggestions.
/// </summary>
public class ExpiringBatchDto
{
    public int BatchId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public int RemainingQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalValue => RemainingQuantity * UnitCost;
    public int DaysToExpiry { get; set; }
    public ExpiryAlertSeverity Severity { get; set; }
    public List<SuggestedAction> SuggestedActions { get; set; } = new();
    public string? SupplierName { get; set; }
    public DateTime ReceivedAt { get; set; }
}

/// <summary>
/// Query parameters for expiry dashboard.
/// </summary>
public class ExpiryDashboardQueryDto
{
    public int? StoreId { get; set; }
    public int? CategoryId { get; set; }
    public int? SupplierId { get; set; }
    public bool IncludeZeroStock { get; set; }
    public int MaxDaysAhead { get; set; } = 90;
}

/// <summary>
/// DTO for expiry alert notification.
/// </summary>
public class ExpiryAlertDto
{
    public int BatchId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = string.Empty;
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public int DaysToExpiry { get; set; }
    public int Quantity { get; set; }
    public decimal Value { get; set; }
    public ExpiryAlertSeverity Severity { get; set; }
    public string AlertMessage { get; set; } = string.Empty;
    public DateTime AlertGeneratedAt { get; set; } = DateTime.UtcNow;
    public bool IsAcknowledged { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public int? AcknowledgedByUserId { get; set; }
}

/// <summary>
/// DTO for expiry export data.
/// </summary>
public class ExpiryExportDto
{
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public int DaysToExpiry { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalValue { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string SuggestedActions { get; set; } = string.Empty;
    public string? SupplierName { get; set; }
    public DateTime ReceivedDate { get; set; }
}

#endregion

#region Expired Item Blocking DTOs

/// <summary>
/// DTO for checking if a product has expired items.
/// </summary>
public class ExpiredItemCheckDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int StoreId { get; set; }
    public bool IsExpired { get; set; }
    public bool HasNearExpiryItems { get; set; }
    public DateTime? EarliestExpiry { get; set; }
    public int? DaysUntilEarliestExpiry { get; set; }
    public int? DaysExpired { get; set; }
    public bool BlockingEnabled { get; set; }
    public bool RequiresOverride { get; set; }
    public string? BlockReason { get; set; }
    public ExpiryAlertSeverity Severity { get; set; }
    public List<ExpiredBatchInfoDto> ExpiredBatches { get; set; } = new();
}

/// <summary>
/// DTO for expired batch information.
/// </summary>
public class ExpiredBatchInfoDto
{
    public int BatchId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public int DaysExpired { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalValue => Quantity * UnitCost;
}

/// <summary>
/// DTO for sale block record.
/// </summary>
public class ExpirySaleBlockDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public int BatchId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public int DaysExpired { get; set; }
    public int AttemptedByUserId { get; set; }
    public string AttemptedByUserName { get; set; } = string.Empty;
    public DateTime AttemptedAt { get; set; }
    public int AttemptedQuantity { get; set; }
    public bool WasBlocked { get; set; }
    public bool OverrideApplied { get; set; }
    public int? OverrideByUserId { get; set; }
    public string? OverrideByUserName { get; set; }
    public DateTime? OverrideAt { get; set; }
    public string? OverrideReason { get; set; }
    public int? ReceiptId { get; set; }
    public string? ReceiptNumber { get; set; }
}

/// <summary>
/// DTO for creating a sale block record.
/// </summary>
public class CreateExpirySaleBlockDto
{
    public int ProductId { get; set; }
    public int BatchId { get; set; }
    public int StoreId { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int DaysExpired { get; set; }
    public int AttemptedQuantity { get; set; }
}

/// <summary>
/// DTO for requesting an override for an expired sale.
/// </summary>
public class ExpirySaleOverrideRequestDto
{
    public int SaleBlockId { get; set; }
    public int ManagerUserId { get; set; }
    public string ManagerPin { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// DTO for override response.
/// </summary>
public class ExpirySaleOverrideResultDto
{
    public bool Success { get; set; }
    public bool OverrideApproved { get; set; }
    public string? Message { get; set; }
    public int? SaleBlockId { get; set; }
    public DateTime? OverrideAt { get; set; }
}

/// <summary>
/// DTO for category expiry settings.
/// </summary>
public class CategoryExpirySettingsDto
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool RequiresExpiryTracking { get; set; }
    public bool BlockExpiredSales { get; set; }
    public bool AllowManagerOverride { get; set; }
    public int WarningDays { get; set; }
    public int CriticalDays { get; set; }
    public string ExpiredItemAction { get; set; } = string.Empty;
    public string NearExpiryAction { get; set; } = string.Empty;
    public int MinimumShelfLifeDaysOnReceipt { get; set; }
}

/// <summary>
/// DTO for updating category expiry settings.
/// </summary>
public class UpdateCategoryExpirySettingsDto
{
    public int CategoryId { get; set; }
    public bool RequiresExpiryTracking { get; set; }
    public bool BlockExpiredSales { get; set; } = true;
    public bool AllowManagerOverride { get; set; } = true;
    public int WarningDays { get; set; } = 30;
    public int CriticalDays { get; set; } = 7;
    public string ExpiredItemAction { get; set; } = "Block";
    public string NearExpiryAction { get; set; } = "Warn";
    public int MinimumShelfLifeDaysOnReceipt { get; set; }
}

/// <summary>
/// Query parameters for sale block records.
/// </summary>
public class SaleBlockQueryDto
{
    public int? StoreId { get; set; }
    public int? ProductId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool? OverrideApplied { get; set; }
    public int? AttemptedByUserId { get; set; }
    public int? OverrideByUserId { get; set; }
}

/// <summary>
/// Summary of sale blocks for reporting.
/// </summary>
public class SaleBlockSummaryDto
{
    public int TotalBlockedAttempts { get; set; }
    public int TotalOverrides { get; set; }
    public int TotalPermanentBlocks { get; set; }
    public decimal TotalBlockedValue { get; set; }
    public decimal TotalOverrideValue { get; set; }
    public int UniqueProducts { get; set; }
    public int UniqueStores { get; set; }
    public DateTime? EarliestAttempt { get; set; }
    public DateTime? LatestAttempt { get; set; }
    public List<TopBlockedProductDto> TopBlockedProducts { get; set; } = new();
}

/// <summary>
/// DTO for top blocked products.
/// </summary>
public class TopBlockedProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public int BlockCount { get; set; }
    public int OverrideCount { get; set; }
    public decimal BlockedValue { get; set; }
}

#endregion

#region Batch Traceability DTOs

/// <summary>
/// Full traceability report for a batch.
/// </summary>
public class BatchTraceabilityReportDto
{
    // Batch Information
    public int BatchId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSKU { get; set; } = string.Empty;
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;

    // Source Information
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public DateTime ReceivedDate { get; set; }
    public int? GrnId { get; set; }
    public string? GrnNumber { get; set; }
    public int QuantityReceived { get; set; }
    public decimal UnitCost { get; set; }
    public DateTime? ManufactureDate { get; set; }

    // Current Status
    public int CurrentQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public int? DaysUntilExpiry { get; set; }
    public bool IsExpired { get; set; }

    // Movement Summary
    public int QuantitySold { get; set; }
    public int QuantityAdjusted { get; set; }
    public int QuantityDisposed { get; set; }
    public int QuantityTransferredOut { get; set; }
    public int QuantityTransferredIn { get; set; }
    public int QuantityReturned { get; set; }

    // Value Summary
    public decimal TotalReceivedValue { get; set; }
    public decimal TotalSoldValue { get; set; }
    public decimal TotalDisposedValue { get; set; }
    public decimal CurrentStockValue { get; set; }

    // Detailed Movements
    public List<BatchMovementDetailDto> Movements { get; set; } = new();

    // Sale Transactions
    public List<BatchSaleTransactionDto> SaleTransactions { get; set; } = new();

    // Location Summary (for multi-store)
    public List<BatchLocationDto> Locations { get; set; } = new();

    // Recall Information
    public bool HasActiveRecall { get; set; }
    public BatchRecallAlertDto? ActiveRecall { get; set; }
}

/// <summary>
/// DTO for a batch movement detail.
/// </summary>
public class BatchMovementDetailDto
{
    public int Id { get; set; }
    public DateTime MovementDate { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int QuantityBefore { get; set; }
    public int QuantityAfter { get; set; }
    public string ReferenceType { get; set; } = string.Empty;
    public int ReferenceId { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Details { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalValue { get; set; }
    public int? PerformedByUserId { get; set; }
    public string? PerformedByUserName { get; set; }
}

/// <summary>
/// DTO for sale transactions containing a batch.
/// </summary>
public class BatchSaleTransactionDto
{
    public int ReceiptId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public int QuantitySold { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public int CashierId { get; set; }
    public string CashierName { get; set; } = string.Empty;
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
}

/// <summary>
/// DTO for batch location/quantity by store.
/// </summary>
public class BatchLocationDto
{
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public DateTime LastMovementDate { get; set; }
}

/// <summary>
/// Query parameters for batch search.
/// </summary>
public class BatchSearchQueryDto
{
    public string? BatchNumber { get; set; }
    public int? ProductId { get; set; }
    public int? StoreId { get; set; }
    public int? SupplierId { get; set; }
    public DateTime? ReceivedFrom { get; set; }
    public DateTime? ReceivedTo { get; set; }
    public DateTime? ExpiryFrom { get; set; }
    public DateTime? ExpiryTo { get; set; }
    public string? Status { get; set; }
    public bool? HasActiveRecall { get; set; }
    public bool IncludeExpired { get; set; } = true;
    public int? Limit { get; set; } = 100;
}

/// <summary>
/// DTO for batch search result.
/// </summary>
public class BatchSearchResultDto
{
    public int BatchId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSKU { get; set; } = string.Empty;
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string? SupplierName { get; set; }
    public DateTime ReceivedDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int CurrentQuantity { get; set; }
    public int QuantitySold { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool HasActiveRecall { get; set; }
}

/// <summary>
/// DTO for batch recall alert.
/// </summary>
public class BatchRecallAlertDto
{
    public int Id { get; set; }
    public int BatchId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSKU { get; set; } = string.Empty;
    public string RecallReason { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public int IssuedByUserId { get; set; }
    public string IssuedByUserName { get; set; } = string.Empty;
    public int AffectedQuantity { get; set; }
    public int QuantityRecovered { get; set; }
    public int QuantitySold { get; set; }
    public int QuantityInStock { get; set; }
    public string? ExternalReference { get; set; }
    public string? SupplierContactInfo { get; set; }
    public string? ResolutionNotes { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public int? ResolvedByUserId { get; set; }
    public string? ResolvedByUserName { get; set; }
    public List<RecallActionDto> Actions { get; set; } = new();
}

/// <summary>
/// DTO for creating a recall alert.
/// </summary>
public class CreateBatchRecallAlertDto
{
    public int BatchId { get; set; }
    public string RecallReason { get; set; } = string.Empty;
    public string Severity { get; set; } = "Medium";
    public string? ExternalReference { get; set; }
    public string? SupplierContactInfo { get; set; }
}

/// <summary>
/// DTO for updating recall status.
/// </summary>
public class UpdateRecallStatusDto
{
    public int RecallAlertId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? QuantityRecovered { get; set; }
    public string? ResolutionNotes { get; set; }
}

/// <summary>
/// DTO for recall action.
/// </summary>
public class RecallActionDto
{
    public int Id { get; set; }
    public int RecallAlertId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public int? StoreId { get; set; }
    public string? StoreName { get; set; }
    public int Quantity { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime ActionDate { get; set; }
    public int PerformedByUserId { get; set; }
    public string PerformedByUserName { get; set; } = string.Empty;
}

/// <summary>
/// DTO for creating a recall action.
/// </summary>
public class CreateRecallActionDto
{
    public int RecallAlertId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public int? StoreId { get; set; }
    public int Quantity { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Query parameters for recall alerts.
/// </summary>
public class RecallQueryDto
{
    public int? ProductId { get; set; }
    public int? StoreId { get; set; }
    public string? Status { get; set; }
    public string? Severity { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool ActiveOnly { get; set; } = true;
}

/// <summary>
/// Summary of recall statistics.
/// </summary>
public class RecallSummaryDto
{
    public int TotalActiveRecalls { get; set; }
    public int TotalClosedRecalls { get; set; }
    public int TotalAffectedQuantity { get; set; }
    public int TotalRecoveredQuantity { get; set; }
    public int TotalSoldBeforeRecall { get; set; }
    public decimal RecoveryRate { get; set; }
    public int CriticalRecalls { get; set; }
    public int HighRecalls { get; set; }
    public int MediumRecalls { get; set; }
    public int LowRecalls { get; set; }
}

#endregion

#region Waste Reporting DTOs

/// <summary>
/// Query parameters for waste reports.
/// </summary>
public class WasteReportQueryDto
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? StoreId { get; set; }
    public int? CategoryId { get; set; }
    public int? SupplierId { get; set; }
    public int? ProductId { get; set; }
    public string? Reason { get; set; }
    public string GroupBy { get; set; } = "Day"; // Day, Week, Month
}

/// <summary>
/// Full waste summary report.
/// </summary>
public class WasteSummaryReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int? StoreId { get; set; }
    public string? StoreName { get; set; }
    public decimal TotalWasteValue { get; set; }
    public int TotalWasteQuantity { get; set; }
    public int TotalRecords { get; set; }
    public decimal AverageWastePerRecord { get; set; }
    public List<WasteByCategoryDto> ByCategory { get; set; } = new();
    public List<WasteBySupplierDto> BySupplier { get; set; } = new();
    public List<WasteByReasonDto> ByReason { get; set; } = new();
    public List<WasteByProductDto> TopWastedProducts { get; set; } = new();
    public List<WasteTrendDataDto> Trends { get; set; } = new();
    public WasteComparisonDto? Comparison { get; set; }
}

/// <summary>
/// Waste grouped by category.
/// </summary>
public class WasteByCategoryDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Value { get; set; }
    public int RecordCount { get; set; }
    public decimal PercentOfTotal { get; set; }
}

/// <summary>
/// Waste grouped by supplier.
/// </summary>
public class WasteBySupplierDto
{
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Value { get; set; }
    public int RecordCount { get; set; }
    public decimal PercentOfTotal { get; set; }
    public int ProductCount { get; set; }
}

/// <summary>
/// Waste grouped by reason.
/// </summary>
public class WasteByReasonDto
{
    public string Reason { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Value { get; set; }
    public int RecordCount { get; set; }
    public decimal PercentOfTotal { get; set; }
}

/// <summary>
/// Waste grouped by product.
/// </summary>
public class WasteByProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int Quantity { get; set; }
    public decimal Value { get; set; }
    public int RecordCount { get; set; }
    public decimal PercentOfTotal { get; set; }
    public string PrimaryReason { get; set; } = string.Empty;
}

/// <summary>
/// Waste trend data point.
/// </summary>
public class WasteTrendDataDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string PeriodLabel { get; set; } = string.Empty;
    public decimal WasteValue { get; set; }
    public int WasteQuantity { get; set; }
    public int RecordCount { get; set; }
    public decimal? PercentChange { get; set; }
}

/// <summary>
/// Waste comparison with previous period.
/// </summary>
public class WasteComparisonDto
{
    public decimal CurrentPeriodValue { get; set; }
    public decimal PreviousPeriodValue { get; set; }
    public decimal ValueChange { get; set; }
    public decimal ValueChangePercent { get; set; }
    public int CurrentPeriodQuantity { get; set; }
    public int PreviousPeriodQuantity { get; set; }
    public int QuantityChange { get; set; }
    public decimal QuantityChangePercent { get; set; }
}

/// <summary>
/// Waste analysis with insights.
/// </summary>
public class WasteAnalysisDto
{
    public decimal TotalWasteValue { get; set; }
    public decimal AverageMonthlyWaste { get; set; }
    public decimal WasteAsPercentOfCOGS { get; set; }
    public decimal WasteVsPreviousPeriod { get; set; }
    public string TrendDirection { get; set; } = string.Empty; // Increasing, Decreasing, Stable
    public List<WasteTrendDataDto> MonthlyTrends { get; set; } = new();
    public List<WasteByProductDto> TopWastedProducts { get; set; } = new();
    public List<WasteInsightDto> Insights { get; set; } = new();
}

/// <summary>
/// Waste insight/recommendation.
/// </summary>
public class WasteInsightDto
{
    public string Type { get; set; } = string.Empty; // Warning, Suggestion, Info
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ActionItem { get; set; }
    public decimal? PotentialSavings { get; set; }
}

/// <summary>
/// Waste record for export.
/// </summary>
public class WasteExportDto
{
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalRecords { get; set; }
    public decimal TotalValue { get; set; }
    public int TotalQuantity { get; set; }
    public List<WasteExportRecordDto> Records { get; set; } = new();
}

/// <summary>
/// Individual waste record for export.
/// </summary>
public class WasteExportRecordDto
{
    public int DisposalId { get; set; }
    public DateTime DisposalDate { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalValue { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsWitnessed { get; set; }
    public string? WitnessName { get; set; }
}

/// <summary>
/// Waste dashboard summary.
/// </summary>
public class WasteDashboardDto
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public WastePeriodSummaryDto Today { get; set; } = new();
    public WastePeriodSummaryDto ThisWeek { get; set; } = new();
    public WastePeriodSummaryDto ThisMonth { get; set; } = new();
    public WastePeriodSummaryDto ThisYear { get; set; } = new();
    public List<WasteByReasonDto> TopReasons { get; set; } = new();
    public List<WasteByProductDto> TopProducts { get; set; } = new();
    public List<WasteTrendDataDto> RecentTrends { get; set; } = new();
}

/// <summary>
/// Waste summary for a specific period.
/// </summary>
public class WastePeriodSummaryDto
{
    public decimal Value { get; set; }
    public int Quantity { get; set; }
    public int RecordCount { get; set; }
    public decimal? ChangePercent { get; set; }
}

/// <summary>
/// Waste data by store for multi-store comparison.
/// </summary>
public class WasteByStoreDto
{
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string StoreCode { get; set; } = string.Empty;
    public decimal WasteValue { get; set; }
    public int WasteQuantity { get; set; }
    public int WasteRecordCount { get; set; }
    public decimal WastePercentageOfRevenue { get; set; }
    public decimal AverageWastePerDay { get; set; }
    public string TopWasteReason { get; set; } = string.Empty;
    public string TopWasteCategory { get; set; } = string.Empty;
}

#endregion
