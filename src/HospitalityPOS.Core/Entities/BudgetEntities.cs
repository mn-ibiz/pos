namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Status of a budget.
/// </summary>
public enum BudgetStatus
{
    /// <summary>Budget is being drafted.</summary>
    Draft = 1,
    /// <summary>Budget is pending approval.</summary>
    PendingApproval = 2,
    /// <summary>Budget is approved and active.</summary>
    Approved = 3,
    /// <summary>Budget is closed.</summary>
    Closed = 4
}

/// <summary>
/// Budget period type.
/// </summary>
public enum BudgetPeriodType
{
    /// <summary>Monthly budget.</summary>
    Monthly = 1,
    /// <summary>Quarterly budget.</summary>
    Quarterly = 2,
    /// <summary>Annual budget.</summary>
    Annual = 3
}

/// <summary>
/// Budget for financial planning.
/// </summary>
public class Budget : BaseEntity
{
    /// <summary>
    /// Store this budget applies to (null for corporate).
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// Budget name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Fiscal year.
    /// </summary>
    public int FiscalYear { get; set; }

    /// <summary>
    /// Budget period type.
    /// </summary>
    public BudgetPeriodType PeriodType { get; set; } = BudgetPeriodType.Monthly;

    /// <summary>
    /// Start date.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date.
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Budget status.
    /// </summary>
    public BudgetStatus Status { get; set; } = BudgetStatus.Draft;

    /// <summary>
    /// User who created the budget.
    /// </summary>
    public int? CreatedByUserId { get; set; }

    /// <summary>
    /// User who approved the budget.
    /// </summary>
    public int? ApprovedByUserId { get; set; }

    /// <summary>
    /// Approval date.
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// Notes.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Whether this is based on prior year actuals.
    /// </summary>
    public bool IsBasedOnPriorYear { get; set; }

    /// <summary>
    /// Adjustment percentage applied to prior year (if applicable).
    /// </summary>
    public decimal? PriorYearAdjustmentPercent { get; set; }

    // Navigation properties
    public virtual Store? Store { get; set; }
    public virtual User? CreatedByUser { get; set; }
    public virtual User? ApprovedByUser { get; set; }
    public virtual ICollection<BudgetLine> Lines { get; set; } = new List<BudgetLine>();
}

/// <summary>
/// Budget line item.
/// </summary>
public class BudgetLine : BaseEntity
{
    /// <summary>
    /// Reference to budget.
    /// </summary>
    public int BudgetId { get; set; }

    /// <summary>
    /// GL account.
    /// </summary>
    public int AccountId { get; set; }

    /// <summary>
    /// Department (optional).
    /// </summary>
    public int? DepartmentId { get; set; }

    /// <summary>
    /// Period number (1-12 for monthly, 1-4 for quarterly, 1 for annual).
    /// </summary>
    public int PeriodNumber { get; set; }

    /// <summary>
    /// Budgeted amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Notes.
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public virtual Budget Budget { get; set; } = null!;
    public virtual ChartOfAccount Account { get; set; } = null!;
    public virtual Department? Department { get; set; }
}

/// <summary>
/// Recurring expense template.
/// </summary>
public class RecurringExpenseTemplate : BaseEntity
{
    /// <summary>
    /// Store this applies to.
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// Template name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Expense category ID.
    /// </summary>
    public int ExpenseCategoryId { get; set; }

    /// <summary>
    /// GL account ID.
    /// </summary>
    public int AccountId { get; set; }

    /// <summary>
    /// Fixed amount (null for variable).
    /// </summary>
    public decimal? Amount { get; set; }

    /// <summary>
    /// Whether amount is variable (requires confirmation).
    /// </summary>
    public bool IsVariableAmount { get; set; }

    /// <summary>
    /// Frequency: Daily, Weekly, Monthly, Quarterly, Annually.
    /// </summary>
    public string Frequency { get; set; } = "Monthly";

    /// <summary>
    /// Day of month to post (1-28 for monthly).
    /// </summary>
    public int? DayOfMonth { get; set; }

    /// <summary>
    /// Day of week (0-6 for weekly).
    /// </summary>
    public int? DayOfWeek { get; set; }

    /// <summary>
    /// Whether to auto-post or create as draft.
    /// </summary>
    public bool AutoPost { get; set; }

    /// <summary>
    /// Whether template is active.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Start date for recurring.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date (null for indefinite).
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Last generated date.
    /// </summary>
    public DateTime? LastGeneratedDate { get; set; }

    /// <summary>
    /// Next scheduled date.
    /// </summary>
    public DateTime? NextScheduledDate { get; set; }

    /// <summary>
    /// Department/cost center ID.
    /// </summary>
    public int? DepartmentId { get; set; }

    /// <summary>
    /// Supplier ID (if applicable).
    /// </summary>
    public int? SupplierId { get; set; }

    // Navigation properties
    public virtual Store? Store { get; set; }
    public virtual ExpenseCategory ExpenseCategory { get; set; } = null!;
    public virtual ChartOfAccount Account { get; set; } = null!;
    public virtual Department? Department { get; set; }
    public virtual Supplier? Supplier { get; set; }
}

/// <summary>
/// Generated recurring expense entry.
/// </summary>
public class RecurringExpenseEntry : BaseEntity
{
    /// <summary>
    /// Reference to template.
    /// </summary>
    public int TemplateId { get; set; }

    /// <summary>
    /// Generated expense ID (if created).
    /// </summary>
    public int? ExpenseId { get; set; }

    /// <summary>
    /// Scheduled date.
    /// </summary>
    public DateTime ScheduledDate { get; set; }

    /// <summary>
    /// Status: Pending, Generated, Skipped, Failed.
    /// </summary>
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Amount (may differ from template for variable amounts).
    /// </summary>
    public decimal? Amount { get; set; }

    /// <summary>
    /// Notes.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Processed date.
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// User who confirmed (for variable amounts).
    /// </summary>
    public int? ConfirmedByUserId { get; set; }

    // Navigation properties
    public virtual RecurringExpenseTemplate Template { get; set; } = null!;
    public virtual Expense? Expense { get; set; }
    public virtual User? ConfirmedByUser { get; set; }
}
