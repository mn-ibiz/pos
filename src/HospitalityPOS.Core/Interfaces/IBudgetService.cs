using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for budget and cost management.
/// </summary>
public interface IBudgetService
{
    #region Budget Management

    /// <summary>
    /// Creates a new budget.
    /// </summary>
    Task<Budget> CreateBudgetAsync(CreateBudgetRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a budget by ID.
    /// </summary>
    Task<Budget?> GetBudgetByIdAsync(int budgetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all budgets.
    /// </summary>
    Task<IEnumerable<Budget>> GetBudgetsAsync(int? storeId = null, int? fiscalYear = null, BudgetStatus? status = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a budget.
    /// </summary>
    Task<Budget> UpdateBudgetAsync(Budget budget, CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits a budget for approval.
    /// </summary>
    Task SubmitBudgetForApprovalAsync(int budgetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves a budget.
    /// </summary>
    Task ApproveBudgetAsync(int budgetId, int approverUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects a budget.
    /// </summary>
    Task RejectBudgetAsync(int budgetId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes a budget.
    /// </summary>
    Task CloseBudgetAsync(int budgetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a budget from prior year actuals.
    /// </summary>
    Task<Budget> CreateBudgetFromPriorYearAsync(int storeId, int fiscalYear, decimal adjustmentPercent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies a budget.
    /// </summary>
    Task<Budget> CopyBudgetAsync(int sourceBudgetId, string newName, int newFiscalYear, CancellationToken cancellationToken = default);

    #endregion

    #region Budget Lines

    /// <summary>
    /// Gets budget lines.
    /// </summary>
    Task<IEnumerable<BudgetLine>> GetBudgetLinesAsync(int budgetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a budget line.
    /// </summary>
    Task<BudgetLine> AddBudgetLineAsync(BudgetLine line, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a budget line.
    /// </summary>
    Task<BudgetLine> UpdateBudgetLineAsync(BudgetLine line, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a budget line.
    /// </summary>
    Task DeleteBudgetLineAsync(int lineId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk updates budget lines.
    /// </summary>
    Task BulkUpdateBudgetLinesAsync(IEnumerable<BudgetLine> lines, CancellationToken cancellationToken = default);

    #endregion

    #region Budget vs Actual Tracking

    /// <summary>
    /// Gets budget vs actual summary.
    /// </summary>
    Task<BudgetVsActualSummary> GetBudgetVsActualSummaryAsync(int budgetId, DateTime asOfDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets accounts with significant variances.
    /// </summary>
    Task<IEnumerable<BudgetVarianceAlert>> GetBudgetVarianceAlertsAsync(int budgetId, decimal varianceThresholdPercent = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets budget utilization by account.
    /// </summary>
    Task<IEnumerable<BudgetUtilization>> GetBudgetUtilizationAsync(int budgetId, CancellationToken cancellationToken = default);

    #endregion

    #region Recurring Expense Templates

    /// <summary>
    /// Gets recurring expense templates.
    /// </summary>
    Task<IEnumerable<RecurringExpenseTemplate>> GetRecurringExpenseTemplatesAsync(int? storeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a recurring expense template.
    /// </summary>
    Task<RecurringExpenseTemplate> CreateRecurringExpenseTemplateAsync(RecurringExpenseTemplate template, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a recurring expense template.
    /// </summary>
    Task<RecurringExpenseTemplate> UpdateRecurringExpenseTemplateAsync(RecurringExpenseTemplate template, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a recurring expense template.
    /// </summary>
    Task DeleteRecurringExpenseTemplateAsync(int templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes due recurring expenses.
    /// </summary>
    Task<RecurringExpenseProcessResult> ProcessDueRecurringExpensesAsync(DateTime asOfDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms a variable amount recurring expense.
    /// </summary>
    Task<RecurringExpenseEntry> ConfirmRecurringExpenseAmountAsync(int entryId, decimal amount, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Skips a recurring expense entry.
    /// </summary>
    Task SkipRecurringExpenseEntryAsync(int entryId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending recurring expense entries.
    /// </summary>
    Task<IEnumerable<RecurringExpenseEntry>> GetPendingRecurringExpenseEntriesAsync(int? storeId = null, CancellationToken cancellationToken = default);

    #endregion

    #region Cost Center Management

    /// <summary>
    /// Gets expense summary by cost center.
    /// </summary>
    Task<IEnumerable<CostCenterExpenseSummary>> GetCostCenterExpenseSummaryAsync(int? storeId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Allocates expense to cost centers.
    /// </summary>
    Task AllocateExpenseToCostCentersAsync(int expenseId, IEnumerable<ExpenseCostCenterAllocation> allocations, CancellationToken cancellationToken = default);

    #endregion
}

#region DTOs

/// <summary>
/// Request to create a budget.
/// </summary>
public class CreateBudgetRequest
{
    public int? StoreId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int FiscalYear { get; set; }
    public BudgetPeriodType PeriodType { get; set; } = BudgetPeriodType.Monthly;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int CreatedByUserId { get; set; }
}

/// <summary>
/// Budget vs actual summary.
/// </summary>
public class BudgetVsActualSummary
{
    public int BudgetId { get; set; }
    public string BudgetName { get; set; } = string.Empty;
    public DateTime AsOfDate { get; set; }
    public decimal TotalBudgeted { get; set; }
    public decimal TotalActual { get; set; }
    public decimal TotalVariance => TotalActual - TotalBudgeted;
    public decimal VariancePercent => TotalBudgeted != 0 ? (TotalVariance / Math.Abs(TotalBudgeted)) * 100 : 0;
    public decimal PercentUtilized => TotalBudgeted != 0 ? (TotalActual / TotalBudgeted) * 100 : 0;
    public int DaysInPeriod { get; set; }
    public int DaysElapsed { get; set; }
    public decimal ExpectedUtilization => DaysInPeriod > 0 ? ((decimal)DaysElapsed / DaysInPeriod) * 100 : 0;
    public List<BudgetAccountSummary> AccountSummaries { get; set; } = new();
}

/// <summary>
/// Budget account summary.
/// </summary>
public class BudgetAccountSummary
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Budgeted { get; set; }
    public decimal Actual { get; set; }
    public decimal Variance => Actual - Budgeted;
    public decimal VariancePercent => Budgeted != 0 ? (Variance / Math.Abs(Budgeted)) * 100 : 0;
    public decimal PercentUtilized => Budgeted != 0 ? (Actual / Budgeted) * 100 : 0;
    public bool IsOverBudget => Actual > Budgeted;
}

/// <summary>
/// Budget variance alert.
/// </summary>
public class BudgetVarianceAlert
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Budgeted { get; set; }
    public decimal Actual { get; set; }
    public decimal VariancePercent { get; set; }
    public string AlertLevel { get; set; } = "Warning"; // Warning, Critical
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Budget utilization.
/// </summary>
public class BudgetUtilization
{
    public int AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public decimal AnnualBudget { get; set; }
    public decimal YTDActual { get; set; }
    public decimal Remaining => AnnualBudget - YTDActual;
    public decimal PercentUsed => AnnualBudget != 0 ? (YTDActual / AnnualBudget) * 100 : 0;
    public decimal MonthlyAvgSpend { get; set; }
    public int MonthsRemaining { get; set; }
    public decimal ProjectedYearEnd => YTDActual + (MonthlyAvgSpend * MonthsRemaining);
    public bool WillExceedBudget => ProjectedYearEnd > AnnualBudget;
}

/// <summary>
/// Result of processing recurring expenses.
/// </summary>
public class RecurringExpenseProcessResult
{
    public int TotalProcessed { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public int PendingConfirmationCount { get; set; }
    public List<RecurringExpenseProcessDetail> Details { get; set; } = new();
}

/// <summary>
/// Detail of recurring expense processing.
/// </summary>
public class RecurringExpenseProcessDetail
{
    public int TemplateId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public int? GeneratedExpenseId { get; set; }
    public bool IsSuccess { get; set; }
    public bool RequiresConfirmation { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Cost center expense summary.
/// </summary>
public class CostCenterExpenseSummary
{
    public int DepartmentId { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public decimal TotalExpenses { get; set; }
    public decimal BudgetedAmount { get; set; }
    public decimal Variance => TotalExpenses - BudgetedAmount;
    public int ExpenseCount { get; set; }
    public List<ExpenseCategorySummary> ExpensesByCategory { get; set; } = new();
}

/// <summary>
/// Expense category summary.
/// </summary>
public class ExpenseCategorySummary
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Count { get; set; }
}

/// <summary>
/// Expense cost center allocation.
/// </summary>
public class ExpenseCostCenterAllocation
{
    public int DepartmentId { get; set; }
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
}

#endregion
