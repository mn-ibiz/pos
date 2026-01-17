namespace HospitalityPOS.Core.Models.Reports;

/// <summary>
/// Parameters for exception report generation (voids and discounts).
/// </summary>
public class ExceptionReportParameters : ReportParametersBase
{
    /// <summary>
    /// Gets or sets the user/cashier ID filter (optional).
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Gets or sets the void reason ID filter (optional).
    /// </summary>
    public int? VoidReasonId { get; set; }
}

/// <summary>
/// Void report line item.
/// </summary>
public class VoidReportItem
{
    /// <summary>
    /// Gets or sets the receipt number.
    /// </summary>
    public string ReceiptNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the receipt ID.
    /// </summary>
    public int ReceiptId { get; set; }

    /// <summary>
    /// Gets or sets the voided amount.
    /// </summary>
    public decimal VoidedAmount { get; set; }

    /// <summary>
    /// Gets or sets the void timestamp.
    /// </summary>
    public DateTime VoidedAt { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who voided.
    /// </summary>
    public int VoidedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the name of the user who voided.
    /// </summary>
    public string VoidedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ID of the user who authorized the void (if different).
    /// </summary>
    public int? AuthorizedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the name of the user who authorized the void.
    /// </summary>
    public string AuthorizedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the void reason ID.
    /// </summary>
    public int? VoidReasonId { get; set; }

    /// <summary>
    /// Gets or sets the void reason.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional notes.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets the formatted time display.
    /// </summary>
    public string TimeDisplay => VoidedAt.ToLocalTime().ToString("HH:mm");
}

/// <summary>
/// Void summary by reason.
/// </summary>
public class VoidByReasonSummary
{
    /// <summary>
    /// Gets or sets the reason name.
    /// </summary>
    public string ReasonName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the count of voids.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Gets or sets the total amount voided.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets the percentage of total voids.
    /// </summary>
    public decimal Percentage { get; set; }
}

/// <summary>
/// Complete void report result.
/// </summary>
public class VoidReportResult
{
    /// <summary>
    /// Gets or sets the report parameters used.
    /// </summary>
    public ExceptionReportParameters Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the void items.
    /// </summary>
    public List<VoidReportItem> Items { get; set; } = [];

    /// <summary>
    /// Gets or sets the void summary by reason.
    /// </summary>
    public List<VoidByReasonSummary> ByReason { get; set; } = [];

    /// <summary>
    /// Gets or sets the total count of voids.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the total voided amount.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets the average void amount.
    /// </summary>
    public decimal AverageAmount { get; set; }

    /// <summary>
    /// Gets or sets when the report was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the name of the user who generated the report.
    /// </summary>
    public string GeneratedBy { get; set; } = string.Empty;
}

/// <summary>
/// Discount report line item.
/// </summary>
public class DiscountReportItem
{
    /// <summary>
    /// Gets or sets the receipt number.
    /// </summary>
    public string ReceiptNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the receipt ID.
    /// </summary>
    public int ReceiptId { get; set; }

    /// <summary>
    /// Gets or sets the item description.
    /// </summary>
    public string ItemDescription { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the original amount (before discount).
    /// </summary>
    public decimal OriginalAmount { get; set; }

    /// <summary>
    /// Gets or sets the discount amount.
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Gets or sets the final amount (after discount).
    /// </summary>
    public decimal FinalAmount => OriginalAmount - DiscountAmount;

    /// <summary>
    /// Gets or sets the discount percentage.
    /// </summary>
    public decimal DiscountPercentage => OriginalAmount > 0 ? (DiscountAmount / OriginalAmount) * 100 : 0;

    /// <summary>
    /// Gets or sets the discount type (Item or Order).
    /// </summary>
    public string DiscountType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user ID who applied the discount.
    /// </summary>
    public int AppliedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the name of the user who applied the discount.
    /// </summary>
    public string AppliedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the discount was applied.
    /// </summary>
    public DateTime AppliedAt { get; set; }

    /// <summary>
    /// Gets the formatted time display.
    /// </summary>
    public string TimeDisplay => AppliedAt.ToLocalTime().ToString("HH:mm");
}

/// <summary>
/// Discount summary by type.
/// </summary>
public class DiscountByTypeSummary
{
    /// <summary>
    /// Gets or sets the discount type.
    /// </summary>
    public string DiscountType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the count of discounts.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Gets or sets the total discount amount.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets the percentage of total discounts.
    /// </summary>
    public decimal Percentage { get; set; }
}

/// <summary>
/// Discount summary by user.
/// </summary>
public class DiscountByUserSummary
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the user name.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the count of discounts.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Gets or sets the total discount amount.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets the percentage of total discounts.
    /// </summary>
    public decimal Percentage { get; set; }
}

/// <summary>
/// Complete discount report result.
/// </summary>
public class DiscountReportResult
{
    /// <summary>
    /// Gets or sets the report parameters used.
    /// </summary>
    public ExceptionReportParameters Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the discount items.
    /// </summary>
    public List<DiscountReportItem> Items { get; set; } = [];

    /// <summary>
    /// Gets or sets the discount summary by type.
    /// </summary>
    public List<DiscountByTypeSummary> ByType { get; set; } = [];

    /// <summary>
    /// Gets or sets the discount summary by user.
    /// </summary>
    public List<DiscountByUserSummary> ByUser { get; set; } = [];

    /// <summary>
    /// Gets or sets the total discounts given.
    /// </summary>
    public decimal TotalDiscounts { get; set; }

    /// <summary>
    /// Gets or sets the number of discount transactions.
    /// </summary>
    public int DiscountTransactionCount { get; set; }

    /// <summary>
    /// Gets or sets the average discount amount.
    /// </summary>
    public decimal AverageDiscount { get; set; }

    /// <summary>
    /// Gets or sets the discount rate (percentage of sales).
    /// </summary>
    public decimal DiscountRate { get; set; }

    /// <summary>
    /// Gets or sets the total sales amount for calculating discount rate.
    /// </summary>
    public decimal TotalSales { get; set; }

    /// <summary>
    /// Gets or sets when the report was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the name of the user who generated the report.
    /// </summary>
    public string GeneratedBy { get; set; } = string.Empty;
}
