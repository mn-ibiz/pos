using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// DTO for creating a new expense.
/// </summary>
public class CreateExpenseDto
{
    public int ExpenseCategoryId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string? PaymentMethod { get; set; }
    public int? PaymentMethodId { get; set; }
    public string? PaymentReference { get; set; }
    public int? SupplierId { get; set; }
    public string? ReceiptImagePath { get; set; }
    public bool IsTaxDeductible { get; set; } = true;
    public string? Notes { get; set; }
    public int? RecurringExpenseId { get; set; }
}

/// <summary>
/// DTO for updating an existing expense.
/// </summary>
public class UpdateExpenseDto
{
    public int ExpenseCategoryId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string? PaymentMethod { get; set; }
    public int? PaymentMethodId { get; set; }
    public string? PaymentReference { get; set; }
    public int? SupplierId { get; set; }
    public string? ReceiptImagePath { get; set; }
    public bool IsTaxDeductible { get; set; } = true;
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for expense filter criteria.
/// </summary>
public class ExpenseFilterDto
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? CategoryId { get; set; }
    public int? SupplierId { get; set; }
    public ExpenseStatus? Status { get; set; }
    public string? SearchTerm { get; set; }
    public bool IncludeInactive { get; set; } = false;
}

/// <summary>
/// DTO for expense summary statistics.
/// </summary>
public class ExpenseSummaryDto
{
    public decimal TotalAmount { get; set; }
    public decimal TotalTax { get; set; }
    public int TotalCount { get; set; }
    public decimal PendingAmount { get; set; }
    public int PendingCount { get; set; }
    public decimal ApprovedAmount { get; set; }
    public int ApprovedCount { get; set; }
    public decimal PaidAmount { get; set; }
    public int PaidCount { get; set; }
    public Dictionary<string, decimal> ByCategory { get; set; } = new();
    public Dictionary<ExpenseCategoryType, decimal> ByType { get; set; } = new();
}

/// <summary>
/// DTO for Prime Cost calculation.
/// </summary>
public class PrimeCostDto
{
    public decimal TotalSales { get; set; }
    public decimal COGS { get; set; }
    public decimal LaborCost { get; set; }
    public decimal PrimeCost { get; set; }
    public decimal PrimeCostPercentage { get; set; }
    public decimal FoodCostPercentage { get; set; }
    public decimal LaborCostPercentage { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}

/// <summary>
/// Service interface for managing expenses.
/// </summary>
public interface IExpenseService
{
    #region Expenses

    /// <summary>
    /// Gets all expenses with optional filtering.
    /// </summary>
    Task<IReadOnlyList<Expense>> GetExpensesAsync(ExpenseFilterDto? filter = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an expense by ID.
    /// </summary>
    Task<Expense?> GetExpenseByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an expense by expense number.
    /// </summary>
    Task<Expense?> GetExpenseByNumberAsync(string expenseNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new expense.
    /// </summary>
    Task<Expense> CreateExpenseAsync(CreateExpenseDto dto, int createdByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing expense.
    /// </summary>
    Task<Expense> UpdateExpenseAsync(int id, UpdateExpenseDto dto, int modifiedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an expense (soft delete).
    /// </summary>
    Task<bool> DeleteExpenseAsync(int id, int deletedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves an expense.
    /// </summary>
    Task<Expense> ApproveExpenseAsync(int id, int approvedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects an expense.
    /// </summary>
    Task<Expense> RejectExpenseAsync(int id, string reason, int rejectedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an expense as paid.
    /// </summary>
    Task<Expense> MarkExpenseAsPaidAsync(int id, int modifiedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets expense summary for a date range.
    /// </summary>
    Task<ExpenseSummaryDto> GetExpenseSummaryAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates the next expense number.
    /// </summary>
    Task<string> GenerateExpenseNumberAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Categories

    /// <summary>
    /// Gets all expense categories.
    /// </summary>
    Task<IReadOnlyList<ExpenseCategory>> GetCategoriesAsync(bool includeInactive = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a category by ID.
    /// </summary>
    Task<ExpenseCategory?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets categories by type.
    /// </summary>
    Task<IReadOnlyList<ExpenseCategory>> GetCategoriesByTypeAsync(ExpenseCategoryType type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new expense category.
    /// </summary>
    Task<ExpenseCategory> CreateCategoryAsync(ExpenseCategory category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing expense category.
    /// </summary>
    Task<ExpenseCategory> UpdateCategoryAsync(ExpenseCategory category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an expense category (soft delete).
    /// </summary>
    Task<bool> DeleteCategoryAsync(int id, CancellationToken cancellationToken = default);

    #endregion

    #region Recurring Expenses

    /// <summary>
    /// Gets all recurring expenses.
    /// </summary>
    Task<IReadOnlyList<RecurringExpense>> GetRecurringExpensesAsync(bool activeOnly = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a recurring expense by ID.
    /// </summary>
    Task<RecurringExpense?> GetRecurringExpenseByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recurring expenses that are due.
    /// </summary>
    Task<IReadOnlyList<RecurringExpense>> GetDueRecurringExpensesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recurring expenses that are upcoming (within reminder period).
    /// </summary>
    Task<IReadOnlyList<RecurringExpense>> GetUpcomingRecurringExpensesAsync(int daysAhead = 7, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new recurring expense.
    /// </summary>
    Task<RecurringExpense> CreateRecurringExpenseAsync(RecurringExpense recurringExpense, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing recurring expense.
    /// </summary>
    Task<RecurringExpense> UpdateRecurringExpenseAsync(RecurringExpense recurringExpense, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a recurring expense (soft delete).
    /// </summary>
    Task<bool> DeleteRecurringExpenseAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an expense from a recurring expense template.
    /// </summary>
    Task<Expense> GenerateExpenseFromRecurringAsync(int recurringExpenseId, int createdByUserId, decimal? actualAmount = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes all due recurring expenses and generates expenses.
    /// </summary>
    Task<IReadOnlyList<Expense>> ProcessDueRecurringExpensesAsync(int createdByUserId, CancellationToken cancellationToken = default);

    #endregion

    #region Budgets

    /// <summary>
    /// Gets all expense budgets.
    /// </summary>
    Task<IReadOnlyList<ExpenseBudget>> GetBudgetsAsync(int? year = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a budget by ID.
    /// </summary>
    Task<ExpenseBudget?> GetBudgetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets budgets for a specific category.
    /// </summary>
    Task<IReadOnlyList<ExpenseBudget>> GetBudgetsByCategoryAsync(int categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current (active) budgets.
    /// </summary>
    Task<IReadOnlyList<ExpenseBudget>> GetCurrentBudgetsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new expense budget.
    /// </summary>
    Task<ExpenseBudget> CreateBudgetAsync(ExpenseBudget budget, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing expense budget.
    /// </summary>
    Task<ExpenseBudget> UpdateBudgetAsync(ExpenseBudget budget, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an expense budget.
    /// </summary>
    Task<bool> DeleteBudgetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recalculates spent amounts for all budgets in a date range.
    /// </summary>
    Task RecalculateBudgetSpentAmountsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets budgets that are over their alert threshold.
    /// </summary>
    Task<IReadOnlyList<ExpenseBudget>> GetBudgetsOverThresholdAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Analytics & Reporting

    /// <summary>
    /// Calculates Prime Cost for a period.
    /// </summary>
    Task<PrimeCostDto> CalculatePrimeCostAsync(DateTime startDate, DateTime endDate, decimal totalSales, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets expense trends over time.
    /// </summary>
    Task<Dictionary<DateTime, decimal>> GetExpenseTrendsAsync(DateTime startDate, DateTime endDate, string groupBy = "day", CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets expense breakdown by category for a period.
    /// </summary>
    Task<Dictionary<string, decimal>> GetExpensesByCategoryAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets expense breakdown by supplier for a period.
    /// </summary>
    Task<Dictionary<string, decimal>> GetExpensesBySupplierAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets expense breakdown by category type for a period.
    /// </summary>
    Task<Dictionary<ExpenseCategoryType, decimal>> GetExpensesByTypeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compares expenses between two periods.
    /// </summary>
    Task<(ExpenseSummaryDto Current, ExpenseSummaryDto Previous, decimal PercentageChange)> ComparePeriodsAsync(
        DateTime currentStart, DateTime currentEnd,
        DateTime previousStart, DateTime previousEnd,
        CancellationToken cancellationToken = default);

    #endregion

    #region Attachments

    /// <summary>
    /// Adds an attachment to an expense.
    /// </summary>
    Task<ExpenseAttachment> AddAttachmentAsync(int expenseId, string fileName, string filePath, string fileType, long fileSize, int uploadedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an attachment from an expense.
    /// </summary>
    Task<bool> RemoveAttachmentAsync(int attachmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all attachments for an expense.
    /// </summary>
    Task<IReadOnlyList<ExpenseAttachment>> GetAttachmentsAsync(int expenseId, CancellationToken cancellationToken = default);

    #endregion
}
