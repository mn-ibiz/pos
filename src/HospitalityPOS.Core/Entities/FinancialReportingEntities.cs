namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Type of cash flow activity.
/// </summary>
public enum CashFlowActivityType
{
    /// <summary>Operating activities (day-to-day business).</summary>
    Operating = 1,
    /// <summary>Investing activities (capital expenditures).</summary>
    Investing = 2,
    /// <summary>Financing activities (loans, equity).</summary>
    Financing = 3
}

/// <summary>
/// Department or cost center for financial allocation.
/// </summary>
public class Department : BaseEntity
{
    /// <summary>
    /// Store this department belongs to (null for corporate-wide).
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// Department code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Department name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Manager user ID.
    /// </summary>
    public int? ManagerUserId { get; set; }

    /// <summary>
    /// Parent department ID for hierarchy.
    /// </summary>
    public int? ParentDepartmentId { get; set; }

    /// <summary>
    /// Display order.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether this is a profit center (tracks revenue).
    /// </summary>
    public bool IsProfitCenter { get; set; }

    /// <summary>
    /// Whether this is active.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Categories allocated to this department.
    /// </summary>
    public string? AllocatedCategoryIds { get; set; }

    /// <summary>
    /// GL account mapping for this department.
    /// </summary>
    public int? GLAccountId { get; set; }

    // Navigation properties
    public virtual Store? Store { get; set; }
    public virtual User? Manager { get; set; }
    public virtual Department? ParentDepartment { get; set; }
    public virtual ICollection<Department> ChildDepartments { get; set; } = new List<Department>();
    public virtual ChartOfAccount? GLAccount { get; set; }
}

/// <summary>
/// Overhead allocation rule for distributing shared costs.
/// </summary>
public class OverheadAllocationRule : BaseEntity
{
    /// <summary>
    /// Store this rule applies to.
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// Rule name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// GL account being allocated.
    /// </summary>
    public int SourceAccountId { get; set; }

    /// <summary>
    /// Allocation basis: Revenue, HeadCount, SquareFootage, Fixed.
    /// </summary>
    public string AllocationBasis { get; set; } = "Revenue";

    /// <summary>
    /// Whether this rule is active.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    // Navigation properties
    public virtual Store? Store { get; set; }
    public virtual ChartOfAccount SourceAccount { get; set; } = null!;
    public virtual ICollection<OverheadAllocationDetail> AllocationDetails { get; set; } = new List<OverheadAllocationDetail>();
}

/// <summary>
/// Detail of overhead allocation to departments.
/// </summary>
public class OverheadAllocationDetail : BaseEntity
{
    /// <summary>
    /// Reference to allocation rule.
    /// </summary>
    public int AllocationRuleId { get; set; }

    /// <summary>
    /// Department receiving allocation.
    /// </summary>
    public int DepartmentId { get; set; }

    /// <summary>
    /// Allocation percentage (for Fixed basis).
    /// </summary>
    public decimal AllocationPercentage { get; set; }

    // Navigation properties
    public virtual OverheadAllocationRule AllocationRule { get; set; } = null!;
    public virtual Department Department { get; set; } = null!;
}

/// <summary>
/// Cash flow statement line item mapping.
/// </summary>
public class CashFlowMapping : BaseEntity
{
    /// <summary>
    /// GL account ID.
    /// </summary>
    public int AccountId { get; set; }

    /// <summary>
    /// Cash flow activity type.
    /// </summary>
    public CashFlowActivityType ActivityType { get; set; }

    /// <summary>
    /// Cash flow line item name.
    /// </summary>
    public string LineItem { get; set; } = string.Empty;

    /// <summary>
    /// Display order within activity type.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether this is an inflow (true) or outflow (false).
    /// </summary>
    public bool IsInflow { get; set; }

    // Navigation properties
    public virtual ChartOfAccount Account { get; set; } = null!;
}

/// <summary>
/// Saved financial report configuration.
/// </summary>
public class SavedReport : BaseEntity
{
    /// <summary>
    /// Store this report belongs to.
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// Report name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Report type: CashFlow, GL, GrossMargin, Comparative, DepartmentalPL.
    /// </summary>
    public string ReportType { get; set; } = string.Empty;

    /// <summary>
    /// Report parameters as JSON.
    /// </summary>
    public string? ParametersJson { get; set; }

    /// <summary>
    /// User who created the report.
    /// </summary>
    public new int? CreatedByUserId { get; set; }

    /// <summary>
    /// Whether this is a scheduled report.
    /// </summary>
    public bool IsScheduled { get; set; }

    /// <summary>
    /// Schedule configuration as JSON.
    /// </summary>
    public string? ScheduleJson { get; set; }

    /// <summary>
    /// Email recipients for scheduled reports.
    /// </summary>
    public string? EmailRecipients { get; set; }

    /// <summary>
    /// Last run timestamp.
    /// </summary>
    public DateTime? LastRunAt { get; set; }

    /// <summary>
    /// Next scheduled run timestamp.
    /// </summary>
    public DateTime? NextRunAt { get; set; }

    // Navigation properties
    public virtual Store? Store { get; set; }
    public virtual User? CreatedByUser { get; set; }
}

/// <summary>
/// Report execution history.
/// </summary>
public class ReportExecutionLog : BaseEntity
{
    /// <summary>
    /// Reference to saved report (if applicable).
    /// </summary>
    public int? SavedReportId { get; set; }

    /// <summary>
    /// Report type.
    /// </summary>
    public string ReportType { get; set; } = string.Empty;

    /// <summary>
    /// Report parameters as JSON.
    /// </summary>
    public string? ParametersJson { get; set; }

    /// <summary>
    /// User who ran the report.
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Execution start time.
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Execution end time.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Execution duration in milliseconds.
    /// </summary>
    public int? DurationMs { get; set; }

    /// <summary>
    /// Whether execution was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Output format: PDF, Excel, CSV.
    /// </summary>
    public string? OutputFormat { get; set; }

    /// <summary>
    /// File path if exported.
    /// </summary>
    public string? FilePath { get; set; }

    // Navigation properties
    public virtual SavedReport? SavedReport { get; set; }
    public virtual User? User { get; set; }
}

/// <summary>
/// Margin threshold configuration for alerts.
/// </summary>
public class MarginThreshold : BaseEntity
{
    /// <summary>
    /// Store this applies to.
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// Category ID (null for all categories).
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Product ID (null for category-level).
    /// </summary>
    public int? ProductId { get; set; }

    /// <summary>
    /// Minimum acceptable margin percentage.
    /// </summary>
    public decimal MinMarginPercent { get; set; }

    /// <summary>
    /// Target margin percentage.
    /// </summary>
    public decimal TargetMarginPercent { get; set; }

    /// <summary>
    /// Alert on below minimum.
    /// </summary>
    public bool AlertOnBelowMinimum { get; set; } = true;

    /// <summary>
    /// Alert on below target.
    /// </summary>
    public bool AlertOnBelowTarget { get; set; }

    // Navigation properties
    public virtual Store? Store { get; set; }
    public virtual Category? Category { get; set; }
    public virtual Product? Product { get; set; }
}
