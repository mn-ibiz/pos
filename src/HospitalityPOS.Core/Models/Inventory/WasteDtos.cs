// src/HospitalityPOS.Core/Models/Inventory/WasteDtos.cs
// DTOs for waste and shrinkage tracking
// Story 46-1: Waste and Shrinkage Tracking

namespace HospitalityPOS.Core.Models.Inventory;

#region Enums

/// <summary>
/// Category of waste reason.
/// </summary>
public enum WasteReasonCategory
{
    /// <summary>Expired or spoiled products.</summary>
    Expiry,
    /// <summary>Physical damage to products.</summary>
    Damage,
    /// <summary>Suspected or confirmed theft.</summary>
    Theft,
    /// <summary>Administrative errors, variances.</summary>
    Administrative,
    /// <summary>Other reasons.</summary>
    Other
}

/// <summary>
/// Status of a waste record.
/// </summary>
public enum WasteRecordStatus
{
    /// <summary>Waste recorded, no approval needed.</summary>
    Recorded,
    /// <summary>Awaiting manager approval.</summary>
    PendingApproval,
    /// <summary>Approved by manager.</summary>
    Approved,
    /// <summary>Rejected by manager.</summary>
    Rejected,
    /// <summary>Reversed/voided.</summary>
    Reversed
}

/// <summary>
/// Type of alert for loss prevention.
/// </summary>
public enum LossPreventionAlertType
{
    /// <summary>Unusual void pattern detected.</summary>
    UnusualVoidPattern,
    /// <summary>High-value waste recorded.</summary>
    HighValueWaste,
    /// <summary>Repeated shrinkage on same item.</summary>
    RepeatedShrinkage,
    /// <summary>Shrinkage threshold exceeded.</summary>
    ThresholdExceeded,
    /// <summary>Stock variance detected.</summary>
    StockVariance
}

/// <summary>
/// Severity of alert.
/// </summary>
public enum AlertSeverity
{
    /// <summary>Informational.</summary>
    Info,
    /// <summary>Warning level.</summary>
    Warning,
    /// <summary>Critical issue.</summary>
    Critical
}

#endregion

#region Waste Reasons

/// <summary>
/// Configurable waste reason.
/// </summary>
public class WasteReason
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public WasteReasonCategory Category { get; set; }
    public bool RequiresApproval { get; set; }
    public decimal? ApprovalThresholdValue { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public int SortOrder { get; set; }

    // Statistics
    public int RecordCount { get; set; }
    public decimal TotalValue { get; set; }
}

/// <summary>
/// Request to create or update a waste reason.
/// </summary>
public class WasteReasonRequest
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public WasteReasonCategory Category { get; set; }
    public bool RequiresApproval { get; set; }
    public decimal? ApprovalThresholdValue { get; set; }
}

#endregion

#region Waste Records

/// <summary>
/// Recorded waste event.
/// </summary>
public class WasteRecord
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductSku { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int? ProductBatchId { get; set; }
    public string? BatchNumber { get; set; }
    public DateOnly? ExpiryDate { get; set; }

    public decimal Quantity { get; set; }
    public string UnitName { get; set; } = "pc";
    public decimal UnitCost { get; set; }
    public decimal TotalValue => Quantity * UnitCost;

    public int WasteReasonId { get; set; }
    public string WasteReasonName { get; set; } = string.Empty;
    public WasteReasonCategory ReasonCategory { get; set; }

    public string? Notes { get; set; }
    public string? ImagePath { get; set; }
    public List<string> ImagePaths { get; set; } = new();

    public int RecordedByUserId { get; set; }
    public string RecordedByName { get; set; } = string.Empty;
    public DateTime RecordedAt { get; set; }

    public WasteRecordStatus Status { get; set; } = WasteRecordStatus.Recorded;
    public int? ApprovedByUserId { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovalNotes { get; set; }

    public DateOnly WasteDate { get; set; }
    public DateTime CreatedAt { get; set; }

    // Linked variance
    public int? VarianceRecordId { get; set; }
}

/// <summary>
/// Request to record waste.
/// </summary>
public class WasteRecordRequest
{
    public int ProductId { get; set; }
    public int? ProductBatchId { get; set; }
    public decimal Quantity { get; set; }
    public int WasteReasonId { get; set; }
    public string? Notes { get; set; }
    public List<string>? ImagePaths { get; set; }
    public DateOnly? WasteDate { get; set; }
    public int RecordedByUserId { get; set; }
}

/// <summary>
/// Request to approve or reject waste.
/// </summary>
public class WasteApprovalRequest
{
    public int WasteRecordId { get; set; }
    public int ApproverUserId { get; set; }
    public bool Approve { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Result of waste operation.
/// </summary>
public class WasteResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public WasteRecord? Record { get; set; }
    public List<string> Warnings { get; set; } = new();

    public static WasteResult Succeeded(WasteRecord record, string message = "Waste recorded successfully")
        => new() { Success = true, Message = message, Record = record };

    public static WasteResult Failed(string message)
        => new() { Success = false, Message = message };
}

#endregion

#region Shrinkage

/// <summary>
/// Shrinkage metrics for a period.
/// </summary>
public class ShrinkageMetrics
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }

    public decimal OpeningStockValue { get; set; }
    public decimal PurchasesValue { get; set; }
    public decimal SalesValue { get; set; } // Cost of goods sold
    public decimal ExpectedClosingValue { get; set; }
    public decimal ActualClosingValue { get; set; }

    public decimal ShrinkageValue => ExpectedClosingValue - ActualClosingValue;
    public decimal ShrinkagePercent => PurchasesValue > 0 ? (ShrinkageValue / PurchasesValue) * 100 : 0;
    public decimal ShrinkageOfSales => SalesValue > 0 ? (ShrinkageValue / SalesValue) * 100 : 0;

    public decimal WasteValue { get; set; } // Known/documented waste
    public decimal UnexplainedVariance => ShrinkageValue - WasteValue;

    public decimal TargetPercent { get; set; } = 1.5m;
    public bool ExceedsTarget => ShrinkagePercent > TargetPercent;
}

/// <summary>
/// Shrinkage snapshot for a point in time.
/// </summary>
public class ShrinkageSnapshot
{
    public int Id { get; set; }
    public DateOnly SnapshotDate { get; set; }
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }

    public decimal ExpectedStock { get; set; }
    public decimal ActualStock { get; set; }
    public decimal Variance => ExpectedStock - ActualStock;
    public decimal VariancePercent => ExpectedStock > 0 ? (Variance / ExpectedStock) * 100 : 0;
    public decimal VarianceValue { get; set; }

    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Stock variance record.
/// </summary>
public class StockVarianceRecord
{
    public int Id { get; set; }
    public int StockTakeId { get; set; }
    public DateOnly CountDate { get; set; }

    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductSku { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }

    public decimal SystemQuantity { get; set; }
    public decimal CountedQuantity { get; set; }
    public decimal Variance => SystemQuantity - CountedQuantity;
    public decimal VariancePercent => SystemQuantity > 0 ? (Variance / SystemQuantity) * 100 : 0;
    public decimal UnitCost { get; set; }
    public decimal VarianceValue => Variance * UnitCost;

    public bool IsSignificant { get; set; }
    public string? InvestigationNotes { get; set; }
    public VarianceInvestigationStatus InvestigationStatus { get; set; }

    // Linked waste record if created
    public int? WasteRecordId { get; set; }
}

/// <summary>
/// Status of variance investigation.
/// </summary>
public enum VarianceInvestigationStatus
{
    /// <summary>Not yet investigated.</summary>
    Pending,
    /// <summary>Under investigation.</summary>
    Investigating,
    /// <summary>Explained and resolved.</summary>
    Resolved,
    /// <summary>Unable to explain, written off.</summary>
    WrittenOff
}

#endregion

#region Reports

/// <summary>
/// Waste report.
/// </summary>
public class WasteReport
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateOnly GeneratedDate { get; set; }
    public int? CategoryFilter { get; set; }
    public string? CategoryName { get; set; }

    public List<WasteRecord> Records { get; set; } = new();
    public List<WasteByReason> ByReason { get; set; } = new();
    public List<WasteByCategory> ByCategory { get; set; } = new();
    public List<WasteByProduct> ByProduct { get; set; } = new();
    public List<DailyWaste> ByDay { get; set; } = new();

    public int TotalRecords { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal TotalValue { get; set; }
    public decimal AveragePerDay => (EndDate.DayNumber - StartDate.DayNumber + 1) > 0
        ? TotalValue / (EndDate.DayNumber - StartDate.DayNumber + 1) : 0;
}

/// <summary>
/// Waste summary by reason.
/// </summary>
public class WasteByReason
{
    public int WasteReasonId { get; set; }
    public string WasteReasonName { get; set; } = string.Empty;
    public WasteReasonCategory Category { get; set; }
    public int RecordCount { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal TotalValue { get; set; }
    public decimal PercentOfTotal { get; set; }
}

/// <summary>
/// Waste summary by product category.
/// </summary>
public class WasteByCategory
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int RecordCount { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal TotalValue { get; set; }
    public decimal PercentOfTotal { get; set; }
}

/// <summary>
/// Waste summary by product.
/// </summary>
public class WasteByProduct
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductSku { get; set; }
    public string? CategoryName { get; set; }
    public int RecordCount { get; set; }
    public decimal TotalQuantity { get; set; }
    public string UnitName { get; set; } = "pc";
    public decimal TotalValue { get; set; }
    public decimal PercentOfTotal { get; set; }
    public string PrimaryReason { get; set; } = string.Empty;
}

/// <summary>
/// Daily waste summary.
/// </summary>
public class DailyWaste
{
    public DateOnly Date { get; set; }
    public int RecordCount { get; set; }
    public decimal TotalValue { get; set; }
}

/// <summary>
/// Shrinkage report.
/// </summary>
public class ShrinkageReport
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateOnly GeneratedDate { get; set; }

    public ShrinkageMetrics OverallMetrics { get; set; } = new();
    public List<ShrinkageByCategory> ByCategory { get; set; } = new();
    public List<ShrinkageByProduct> TopShrinkageProducts { get; set; } = new();
    public List<MonthlyShrinkage> MonthlyTrend { get; set; } = new();
    public List<StockVarianceRecord> SignificantVariances { get; set; } = new();

    public decimal IndustryBenchmark { get; set; } = 1.5m;
    public string PerformanceVsBenchmark => OverallMetrics.ShrinkagePercent <= IndustryBenchmark ? "Good" :
        OverallMetrics.ShrinkagePercent <= IndustryBenchmark * 1.5m ? "Attention Needed" : "Critical";
}

/// <summary>
/// Shrinkage by category.
/// </summary>
public class ShrinkageByCategory
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal ShrinkageValue { get; set; }
    public decimal ShrinkagePercent { get; set; }
    public decimal WasteValue { get; set; }
    public decimal UnexplainedVariance { get; set; }
    public int ProductCount { get; set; }
}

/// <summary>
/// Shrinkage by product.
/// </summary>
public class ShrinkageByProduct
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductSku { get; set; }
    public string? CategoryName { get; set; }
    public decimal ShrinkageValue { get; set; }
    public decimal ShrinkagePercent { get; set; }
    public decimal WasteCount { get; set; }
    public string MostCommonReason { get; set; } = string.Empty;
}

/// <summary>
/// Monthly shrinkage summary.
/// </summary>
public class MonthlyShrinkage
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal ShrinkageValue { get; set; }
    public decimal ShrinkagePercent { get; set; }
    public decimal WasteValue { get; set; }
    public int WasteRecordCount { get; set; }
}

#endregion

#region Dashboard & Alerts

/// <summary>
/// Shrinkage dashboard summary.
/// </summary>
public class ShrinkageDashboard
{
    public DateOnly AsOfDate { get; set; }
    public decimal CurrentMonthShrinkagePercent { get; set; }
    public decimal CurrentMonthShrinkageValue { get; set; }
    public decimal TargetPercent { get; set; } = 1.5m;
    public bool ExceedsTarget => CurrentMonthShrinkagePercent > TargetPercent;

    public decimal PreviousMonthShrinkagePercent { get; set; }
    public decimal ChangeFromPrevious => CurrentMonthShrinkagePercent - PreviousMonthShrinkagePercent;

    public List<TopShrinkageItem> TopLossItems { get; set; } = new();
    public List<WasteByReason> WasteByReasonSummary { get; set; } = new();
    public List<ShrinkageTrendPoint> TrendData { get; set; } = new();
    public int ActiveAlerts { get; set; }
}

/// <summary>
/// Top shrinkage item for dashboard.
/// </summary>
public class TopShrinkageItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal TotalLossValue { get; set; }
    public int IncidentCount { get; set; }
}

/// <summary>
/// Shrinkage trend point for charting.
/// </summary>
public class ShrinkageTrendPoint
{
    public DateOnly Date { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal ShrinkagePercent { get; set; }
    public decimal ShrinkageValue { get; set; }
}

/// <summary>
/// Loss prevention alert.
/// </summary>
public class LossPreventionAlert
{
    public int Id { get; set; }
    public LossPreventionAlertType AlertType { get; set; }
    public AlertSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsAcknowledged { get; set; }
    public int? AcknowledgedByUserId { get; set; }
    public DateTime? AcknowledgedAt { get; set; }

    // Reference to related record
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
    public int? WasteRecordId { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }

    public decimal? Value { get; set; }
    public decimal? Threshold { get; set; }
}

/// <summary>
/// Alert rule configuration.
/// </summary>
public class AlertRuleConfig
{
    public int Id { get; set; }
    public LossPreventionAlertType AlertType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsEnabled { get; set; } = true;

    // Threshold settings
    public decimal? ValueThreshold { get; set; }
    public decimal? PercentThreshold { get; set; }
    public int? CountThreshold { get; set; }
    public int? TimeWindowHours { get; set; }

    // Notification settings
    public AlertSeverity DefaultSeverity { get; set; } = AlertSeverity.Warning;
    public bool NotifyManager { get; set; } = true;
    public bool NotifyOwner { get; set; }
}

#endregion

#region Settings

/// <summary>
/// Waste tracking settings.
/// </summary>
public class WasteTrackingSettings
{
    /// <summary>Require approval for all waste records.</summary>
    public bool RequireApprovalForAll { get; set; }

    /// <summary>Require approval for waste above this value.</summary>
    public decimal ApprovalThresholdValue { get; set; } = 5000m;

    /// <summary>Target shrinkage percentage.</summary>
    public decimal TargetShrinkagePercent { get; set; } = 1.5m;

    /// <summary>Significant variance threshold (%).</summary>
    public decimal SignificantVarianceThreshold { get; set; } = 5m;

    /// <summary>Auto-deduct stock when waste recorded.</summary>
    public bool AutoDeductStock { get; set; } = true;

    /// <summary>Require photos for high-value waste.</summary>
    public bool RequirePhotosForHighValue { get; set; }

    /// <summary>High-value threshold for photos.</summary>
    public decimal HighValuePhotoThreshold { get; set; } = 10000m;

    /// <summary>Alert on unusual void patterns.</summary>
    public bool AlertOnUnusualVoids { get; set; } = true;

    /// <summary>Void count threshold per user per day.</summary>
    public int VoidCountThreshold { get; set; } = 10;
}

#endregion

#region Events

/// <summary>
/// Event args for waste events.
/// </summary>
public class WasteEventArgs : EventArgs
{
    public WasteRecord Record { get; }
    public string EventType { get; }
    public DateTime Timestamp { get; }

    public WasteEventArgs(WasteRecord record, string eventType)
    {
        Record = record;
        EventType = eventType;
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// Event args for alert events.
/// </summary>
public class AlertEventArgs : EventArgs
{
    public LossPreventionAlert Alert { get; }
    public DateTime Timestamp { get; }

    public AlertEventArgs(LossPreventionAlert alert)
    {
        Alert = alert;
        Timestamp = DateTime.UtcNow;
    }
}

#endregion
