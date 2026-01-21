using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service implementation for managing expenses.
/// </summary>
public class ExpenseService : IExpenseService
{
    private readonly POSDbContext _context;
    private readonly ILogger<ExpenseService> _logger;

    public ExpenseService(POSDbContext context, ILogger<ExpenseService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Expenses

    public async Task<IReadOnlyList<Expense>> GetExpensesAsync(ExpenseFilterDto? filter = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Expenses
            .AsNoTracking()
            .Include(e => e.ExpenseCategory)
            .Include(e => e.Supplier)
            .Include(e => e.CreatedByUser)
            .Include(e => e.ApprovedByUser)
            .AsQueryable();

        if (filter != null)
        {
            if (!filter.IncludeInactive)
            {
                query = query.Where(e => e.IsActive);
            }

            if (filter.StartDate.HasValue)
            {
                query = query.Where(e => e.ExpenseDate >= filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                query = query.Where(e => e.ExpenseDate <= filter.EndDate.Value);
            }

            if (filter.CategoryId.HasValue)
            {
                query = query.Where(e => e.ExpenseCategoryId == filter.CategoryId.Value);
            }

            if (filter.SupplierId.HasValue)
            {
                query = query.Where(e => e.SupplierId == filter.SupplierId.Value);
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(e => e.Status == filter.Status.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var searchTerm = filter.SearchTerm.ToLower();
                query = query.Where(e =>
                    e.Description.ToLower().Contains(searchTerm) ||
                    e.ExpenseNumber.ToLower().Contains(searchTerm) ||
                    (e.PaymentReference != null && e.PaymentReference.ToLower().Contains(searchTerm)));
            }
        }

        return await query
            .OrderByDescending(e => e.ExpenseDate)
            .ThenByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Expense?> GetExpenseByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Expenses
            .AsNoTracking()
            .Include(e => e.ExpenseCategory)
            .Include(e => e.Supplier)
            .Include(e => e.CreatedByUser)
            .Include(e => e.ApprovedByUser)
            .Include(e => e.Attachments)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Expense?> GetExpenseByNumberAsync(string expenseNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Expenses
            .AsNoTracking()
            .Include(e => e.ExpenseCategory)
            .Include(e => e.Supplier)
            .FirstOrDefaultAsync(e => e.ExpenseNumber == expenseNumber, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Expense> CreateExpenseAsync(CreateExpenseDto dto, int createdByUserId, CancellationToken cancellationToken = default)
    {
        var expenseNumber = await GenerateExpenseNumberAsync(cancellationToken).ConfigureAwait(false);

        var expense = new Expense
        {
            ExpenseNumber = expenseNumber,
            ExpenseCategoryId = dto.ExpenseCategoryId,
            Description = dto.Description,
            Amount = dto.Amount,
            TaxAmount = dto.TaxAmount,
            ExpenseDate = dto.ExpenseDate,
            PaymentMethod = dto.PaymentMethod,
            PaymentMethodId = dto.PaymentMethodId,
            PaymentReference = dto.PaymentReference,
            SupplierId = dto.SupplierId,
            ReceiptImagePath = dto.ReceiptImagePath,
            IsTaxDeductible = dto.IsTaxDeductible,
            Notes = dto.Notes,
            RecurringExpenseId = dto.RecurringExpenseId,
            IsRecurring = dto.RecurringExpenseId.HasValue,
            Status = ExpenseStatus.Pending,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Created expense {ExpenseNumber} for amount {Amount}", expense.ExpenseNumber, expense.Amount);

        // Update budget spent amounts
        await UpdateBudgetSpentAmountAsync(expense.ExpenseCategoryId, expense.ExpenseDate, cancellationToken).ConfigureAwait(false);

        return expense;
    }

    public async Task<Expense> UpdateExpenseAsync(int id, UpdateExpenseDto dto, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        var expense = await _context.Expenses.FindAsync(new object[] { id }, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Expense with ID {id} not found.");

        var oldCategoryId = expense.ExpenseCategoryId;
        var oldDate = expense.ExpenseDate;

        expense.ExpenseCategoryId = dto.ExpenseCategoryId;
        expense.Description = dto.Description;
        expense.Amount = dto.Amount;
        expense.TaxAmount = dto.TaxAmount;
        expense.ExpenseDate = dto.ExpenseDate;
        expense.PaymentMethod = dto.PaymentMethod;
        expense.PaymentMethodId = dto.PaymentMethodId;
        expense.PaymentReference = dto.PaymentReference;
        expense.SupplierId = dto.SupplierId;
        expense.ReceiptImagePath = dto.ReceiptImagePath;
        expense.IsTaxDeductible = dto.IsTaxDeductible;
        expense.Notes = dto.Notes;
        expense.UpdatedByUserId = modifiedByUserId;
        expense.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Updated expense {ExpenseNumber}", expense.ExpenseNumber);

        // Update budget spent amounts for both old and new categories if changed
        await UpdateBudgetSpentAmountAsync(expense.ExpenseCategoryId, expense.ExpenseDate, cancellationToken).ConfigureAwait(false);
        if (oldCategoryId != expense.ExpenseCategoryId)
        {
            await UpdateBudgetSpentAmountAsync(oldCategoryId, oldDate, cancellationToken).ConfigureAwait(false);
        }

        return expense;
    }

    public async Task<bool> DeleteExpenseAsync(int id, int deletedByUserId, CancellationToken cancellationToken = default)
    {
        var expense = await _context.Expenses.FindAsync(new object[] { id }, cancellationToken).ConfigureAwait(false);
        if (expense == null) return false;

        expense.IsActive = false;
        expense.UpdatedByUserId = deletedByUserId;
        expense.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Deleted expense {ExpenseNumber}", expense.ExpenseNumber);

        // Update budget spent amounts
        await UpdateBudgetSpentAmountAsync(expense.ExpenseCategoryId, expense.ExpenseDate, cancellationToken).ConfigureAwait(false);

        return true;
    }

    public async Task<Expense> ApproveExpenseAsync(int id, int approvedByUserId, CancellationToken cancellationToken = default)
    {
        var expense = await _context.Expenses.FindAsync(new object[] { id }, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Expense with ID {id} not found.");

        expense.Status = ExpenseStatus.Approved;
        expense.ApprovedByUserId = approvedByUserId;
        expense.ApprovedAt = DateTime.UtcNow;
        expense.UpdatedByUserId = approvedByUserId;
        expense.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Approved expense {ExpenseNumber}", expense.ExpenseNumber);

        return expense;
    }

    public async Task<Expense> RejectExpenseAsync(int id, string reason, int rejectedByUserId, CancellationToken cancellationToken = default)
    {
        var expense = await _context.Expenses.FindAsync(new object[] { id }, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Expense with ID {id} not found.");

        expense.Status = ExpenseStatus.Rejected;
        expense.RejectionReason = reason;
        expense.UpdatedByUserId = rejectedByUserId;
        expense.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Rejected expense {ExpenseNumber} - Reason: {Reason}", expense.ExpenseNumber, reason);

        return expense;
    }

    public async Task<Expense> MarkExpenseAsPaidAsync(int id, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        var expense = await _context.Expenses.FindAsync(new object[] { id }, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Expense with ID {id} not found.");

        expense.Status = ExpenseStatus.Paid;
        expense.PaidAt = DateTime.UtcNow;
        expense.UpdatedByUserId = modifiedByUserId;
        expense.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Marked expense {ExpenseNumber} as paid", expense.ExpenseNumber);

        return expense;
    }

    public async Task<ExpenseSummaryDto> GetExpenseSummaryAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var expenses = await _context.Expenses
            .AsNoTracking()
            .Include(e => e.ExpenseCategory)
            .Where(e => e.IsActive && e.ExpenseDate >= startDate && e.ExpenseDate <= endDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var summary = new ExpenseSummaryDto
        {
            TotalAmount = expenses.Sum(e => e.Amount),
            TotalTax = expenses.Sum(e => e.TaxAmount),
            TotalCount = expenses.Count,
            PendingAmount = expenses.Where(e => e.Status == ExpenseStatus.Pending).Sum(e => e.Amount),
            PendingCount = expenses.Count(e => e.Status == ExpenseStatus.Pending),
            ApprovedAmount = expenses.Where(e => e.Status == ExpenseStatus.Approved).Sum(e => e.Amount),
            ApprovedCount = expenses.Count(e => e.Status == ExpenseStatus.Approved),
            PaidAmount = expenses.Where(e => e.Status == ExpenseStatus.Paid).Sum(e => e.Amount),
            PaidCount = expenses.Count(e => e.Status == ExpenseStatus.Paid),
            ByCategory = expenses.GroupBy(e => e.ExpenseCategory?.Name ?? "Uncategorized")
                .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount)),
            ByType = expenses.GroupBy(e => e.ExpenseCategory?.Type ?? ExpenseCategoryType.Other)
                .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount))
        };

        return summary;
    }

    public async Task<string> GenerateExpenseNumberAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        var prefix = $"EXP-{today:yyyyMMdd}-";

        var lastExpense = await _context.Expenses
            .AsNoTracking()
            .Where(e => e.ExpenseNumber.StartsWith(prefix))
            .OrderByDescending(e => e.ExpenseNumber)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        int nextNumber = 1;
        if (lastExpense != null)
        {
            var lastNumberStr = lastExpense.ExpenseNumber.Replace(prefix, "");
            if (int.TryParse(lastNumberStr, out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"{prefix}{nextNumber:D4}";
    }

    #endregion

    #region Categories

    public async Task<IReadOnlyList<ExpenseCategory>> GetCategoriesAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _context.ExpenseCategories
            .AsNoTracking()
            .Include(c => c.ParentCategory)
            .Include(c => c.SubCategories)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        return await query
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ExpenseCategory?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.ExpenseCategories
            .AsNoTracking()
            .Include(c => c.ParentCategory)
            .Include(c => c.SubCategories)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ExpenseCategory>> GetCategoriesByTypeAsync(ExpenseCategoryType type, CancellationToken cancellationToken = default)
    {
        return await _context.ExpenseCategories
            .AsNoTracking()
            .Where(c => c.IsActive && c.Type == type)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ExpenseCategory> CreateCategoryAsync(ExpenseCategory category, CancellationToken cancellationToken = default)
    {
        category.CreatedAt = DateTime.UtcNow;
        category.IsActive = true;

        _context.ExpenseCategories.Add(category);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Created expense category {CategoryName}", category.Name);

        return category;
    }

    public async Task<ExpenseCategory> UpdateCategoryAsync(ExpenseCategory category, CancellationToken cancellationToken = default)
    {
        var existing = await _context.ExpenseCategories.FindAsync(new object[] { category.Id }, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Category with ID {category.Id} not found.");

        existing.Name = category.Name;
        existing.Description = category.Description;
        existing.ParentCategoryId = category.ParentCategoryId;
        existing.Type = category.Type;
        existing.Icon = category.Icon;
        existing.Color = category.Color;
        existing.SortOrder = category.SortOrder;
        existing.DefaultAccountId = category.DefaultAccountId;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Updated expense category {CategoryName}", existing.Name);

        return existing;
    }

    public async Task<bool> DeleteCategoryAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _context.ExpenseCategories.FindAsync(new object[] { id }, cancellationToken).ConfigureAwait(false);
        if (category == null || category.IsSystemCategory) return false;

        // Check if category has expenses
        var hasExpenses = await _context.Expenses.AnyAsync(e => e.ExpenseCategoryId == id, cancellationToken).ConfigureAwait(false);
        if (hasExpenses)
        {
            // Soft delete
            category.IsActive = false;
            category.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _context.ExpenseCategories.Remove(category);
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Deleted expense category {CategoryName}", category.Name);

        return true;
    }

    #endregion

    #region Recurring Expenses

    public async Task<IReadOnlyList<RecurringExpense>> GetRecurringExpensesAsync(bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        var query = _context.RecurringExpenses
            .AsNoTracking()
            .Include(r => r.ExpenseCategory)
            .Include(r => r.Supplier)
            .AsQueryable();

        if (activeOnly)
        {
            query = query.Where(r => r.IsActive);
        }

        return await query
            .OrderBy(r => r.NextDueDate)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<RecurringExpense?> GetRecurringExpenseByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.RecurringExpenses
            .AsNoTracking()
            .Include(r => r.ExpenseCategory)
            .Include(r => r.Supplier)
            .Include(r => r.GeneratedExpenses)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<RecurringExpense>> GetDueRecurringExpensesAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        return await _context.RecurringExpenses
            .AsNoTracking()
            .Include(r => r.ExpenseCategory)
            .Include(r => r.Supplier)
            .Where(r => r.IsActive && r.NextDueDate.HasValue && r.NextDueDate.Value.Date <= today)
            .OrderBy(r => r.NextDueDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<RecurringExpense>> GetUpcomingRecurringExpensesAsync(int daysAhead = 7, CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        var futureDate = today.AddDays(daysAhead);

        return await _context.RecurringExpenses
            .AsNoTracking()
            .Include(r => r.ExpenseCategory)
            .Include(r => r.Supplier)
            .Where(r => r.IsActive && r.NextDueDate.HasValue &&
                        r.NextDueDate.Value.Date > today &&
                        r.NextDueDate.Value.Date <= futureDate)
            .OrderBy(r => r.NextDueDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<RecurringExpense> CreateRecurringExpenseAsync(RecurringExpense recurringExpense, CancellationToken cancellationToken = default)
    {
        recurringExpense.CreatedAt = DateTime.UtcNow;
        recurringExpense.IsActive = true;
        recurringExpense.NextDueDate = CalculateNextDueDate(recurringExpense.StartDate, recurringExpense.Frequency, recurringExpense.DayOfMonth);

        _context.RecurringExpenses.Add(recurringExpense);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Created recurring expense {Name} with frequency {Frequency}", recurringExpense.Name, recurringExpense.Frequency);

        return recurringExpense;
    }

    public async Task<RecurringExpense> UpdateRecurringExpenseAsync(RecurringExpense recurringExpense, CancellationToken cancellationToken = default)
    {
        var existing = await _context.RecurringExpenses.FindAsync(new object[] { recurringExpense.Id }, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Recurring expense with ID {recurringExpense.Id} not found.");

        existing.Name = recurringExpense.Name;
        existing.Description = recurringExpense.Description;
        existing.ExpenseCategoryId = recurringExpense.ExpenseCategoryId;
        existing.SupplierId = recurringExpense.SupplierId;
        existing.PaymentMethodId = recurringExpense.PaymentMethodId;
        existing.Amount = recurringExpense.Amount;
        existing.IsEstimatedAmount = recurringExpense.IsEstimatedAmount;
        existing.Frequency = recurringExpense.Frequency;
        existing.StartDate = recurringExpense.StartDate;
        existing.EndDate = recurringExpense.EndDate;
        existing.DayOfMonth = recurringExpense.DayOfMonth;
        existing.DayOfWeek = recurringExpense.DayOfWeek;
        existing.ReminderDaysBefore = recurringExpense.ReminderDaysBefore;
        existing.AutoApprove = recurringExpense.AutoApprove;
        existing.AutoGenerate = recurringExpense.AutoGenerate;
        existing.Notes = recurringExpense.Notes;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Updated recurring expense {Name}", existing.Name);

        return existing;
    }

    public async Task<bool> DeleteRecurringExpenseAsync(int id, CancellationToken cancellationToken = default)
    {
        var recurringExpense = await _context.RecurringExpenses.FindAsync(new object[] { id }, cancellationToken).ConfigureAwait(false);
        if (recurringExpense == null) return false;

        recurringExpense.IsActive = false;
        recurringExpense.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Deleted recurring expense {Name}", recurringExpense.Name);

        return true;
    }

    public async Task<Expense> GenerateExpenseFromRecurringAsync(int recurringExpenseId, int createdByUserId, decimal? actualAmount = null, CancellationToken cancellationToken = default)
    {
        var recurring = await _context.RecurringExpenses
            .Include(r => r.ExpenseCategory)
            .FirstOrDefaultAsync(r => r.Id == recurringExpenseId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Recurring expense with ID {recurringExpenseId} not found.");

        var dto = new CreateExpenseDto
        {
            ExpenseCategoryId = recurring.ExpenseCategoryId,
            Description = recurring.Description,
            Amount = actualAmount ?? recurring.Amount,
            TaxAmount = 0,
            ExpenseDate = recurring.NextDueDate ?? DateTime.Today,
            PaymentMethodId = recurring.PaymentMethodId,
            SupplierId = recurring.SupplierId,
            Notes = $"Generated from recurring expense: {recurring.Name}",
            RecurringExpenseId = recurring.Id
        };

        var expense = await CreateExpenseAsync(dto, createdByUserId, cancellationToken).ConfigureAwait(false);

        // Auto-approve if configured
        if (recurring.AutoApprove)
        {
            await ApproveExpenseAsync(expense.Id, createdByUserId, cancellationToken).ConfigureAwait(false);
        }

        // Update recurring expense
        recurring.LastGeneratedDate = DateTime.UtcNow;
        recurring.OccurrenceCount++;
        recurring.NextDueDate = CalculateNextDueDate(recurring.NextDueDate ?? DateTime.Today, recurring.Frequency, recurring.DayOfMonth);

        // Check if end date has been reached
        if (recurring.EndDate.HasValue && recurring.NextDueDate > recurring.EndDate)
        {
            recurring.IsActive = false;
            recurring.NextDueDate = null;
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Generated expense {ExpenseNumber} from recurring expense {RecurringName}", expense.ExpenseNumber, recurring.Name);

        return expense;
    }

    public async Task<IReadOnlyList<Expense>> ProcessDueRecurringExpensesAsync(int createdByUserId, CancellationToken cancellationToken = default)
    {
        var dueExpenses = await GetDueRecurringExpensesAsync(cancellationToken).ConfigureAwait(false);
        var generatedExpenses = new List<Expense>();

        foreach (var recurring in dueExpenses.Where(r => r.AutoGenerate))
        {
            try
            {
                var expense = await GenerateExpenseFromRecurringAsync(recurring.Id, createdByUserId, null, cancellationToken).ConfigureAwait(false);
                generatedExpenses.Add(expense);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate expense from recurring expense {RecurringId}", recurring.Id);
            }
        }

        return generatedExpenses;
    }

    private static DateTime CalculateNextDueDate(DateTime fromDate, RecurrenceFrequency frequency, int dayOfMonth)
    {
        return frequency switch
        {
            RecurrenceFrequency.Daily => fromDate.AddDays(1),
            RecurrenceFrequency.Weekly => fromDate.AddDays(7),
            RecurrenceFrequency.BiWeekly => fromDate.AddDays(14),
            RecurrenceFrequency.Monthly => GetNextMonthlyDate(fromDate, dayOfMonth),
            RecurrenceFrequency.Quarterly => GetNextMonthlyDate(fromDate, dayOfMonth).AddMonths(2),
            RecurrenceFrequency.Annually => fromDate.AddYears(1),
            _ => fromDate.AddMonths(1)
        };
    }

    private static DateTime GetNextMonthlyDate(DateTime fromDate, int dayOfMonth)
    {
        var nextMonth = fromDate.AddMonths(1);
        var daysInMonth = DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month);
        var day = Math.Min(dayOfMonth, daysInMonth);
        return new DateTime(nextMonth.Year, nextMonth.Month, day);
    }

    #endregion

    #region Budgets

    public async Task<IReadOnlyList<ExpenseBudget>> GetBudgetsAsync(int? year = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ExpenseBudgets
            .AsNoTracking()
            .Include(b => b.ExpenseCategory)
            .Where(b => b.IsActive);

        if (year.HasValue)
        {
            query = query.Where(b => b.Year == year.Value);
        }

        return await query
            .OrderBy(b => b.Year)
            .ThenBy(b => b.Month)
            .ThenBy(b => b.ExpenseCategory!.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ExpenseBudget?> GetBudgetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.ExpenseBudgets
            .AsNoTracking()
            .Include(b => b.ExpenseCategory)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ExpenseBudget>> GetBudgetsByCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        return await _context.ExpenseBudgets
            .AsNoTracking()
            .Include(b => b.ExpenseCategory)
            .Where(b => b.IsActive && b.ExpenseCategoryId == categoryId)
            .OrderByDescending(b => b.Year)
            .ThenByDescending(b => b.Month)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ExpenseBudget>> GetCurrentBudgetsAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        return await _context.ExpenseBudgets
            .AsNoTracking()
            .Include(b => b.ExpenseCategory)
            .Where(b => b.IsActive && b.StartDate <= today && b.EndDate >= today)
            .OrderBy(b => b.ExpenseCategory!.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ExpenseBudget> CreateBudgetAsync(ExpenseBudget budget, CancellationToken cancellationToken = default)
    {
        budget.CreatedAt = DateTime.UtcNow;
        budget.IsActive = true;

        _context.ExpenseBudgets.Add(budget);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Calculate initial spent amount
        await RecalculateBudgetSpentAmountAsync(budget, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Created expense budget {BudgetName} for {Amount}", budget.Name, budget.Amount);

        return budget;
    }

    public async Task<ExpenseBudget> UpdateBudgetAsync(ExpenseBudget budget, CancellationToken cancellationToken = default)
    {
        var existing = await _context.ExpenseBudgets.FindAsync(new object[] { budget.Id }, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Budget with ID {budget.Id} not found.");

        existing.Name = budget.Name;
        existing.ExpenseCategoryId = budget.ExpenseCategoryId;
        existing.Amount = budget.Amount;
        existing.Period = budget.Period;
        existing.Year = budget.Year;
        existing.Month = budget.Month;
        existing.Quarter = budget.Quarter;
        existing.StartDate = budget.StartDate;
        existing.EndDate = budget.EndDate;
        existing.AlertThreshold = budget.AlertThreshold;
        existing.Notes = budget.Notes;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Recalculate spent amount
        await RecalculateBudgetSpentAmountAsync(existing, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Updated expense budget {BudgetName}", existing.Name);

        return existing;
    }

    public async Task<bool> DeleteBudgetAsync(int id, CancellationToken cancellationToken = default)
    {
        var budget = await _context.ExpenseBudgets.FindAsync(new object[] { id }, cancellationToken).ConfigureAwait(false);
        if (budget == null) return false;

        budget.IsActive = false;
        budget.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Deleted expense budget {BudgetName}", budget.Name);

        return true;
    }

    public async Task RecalculateBudgetSpentAmountsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var budgets = await _context.ExpenseBudgets
            .Where(b => b.IsActive &&
                        ((b.StartDate >= startDate && b.StartDate <= endDate) ||
                         (b.EndDate >= startDate && b.EndDate <= endDate) ||
                         (b.StartDate <= startDate && b.EndDate >= endDate)))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var budget in budgets)
        {
            await RecalculateBudgetSpentAmountAsync(budget, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<ExpenseBudget>> GetBudgetsOverThresholdAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        var budgets = await _context.ExpenseBudgets
            .AsNoTracking()
            .Include(b => b.ExpenseCategory)
            .Where(b => b.IsActive && b.StartDate <= today && b.EndDate >= today)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return budgets.Where(b => b.IsOverThreshold).ToList();
    }

    private async Task RecalculateBudgetSpentAmountAsync(ExpenseBudget budget, CancellationToken cancellationToken)
    {
        var query = _context.Expenses
            .Where(e => e.IsActive &&
                        e.ExpenseDate >= budget.StartDate &&
                        e.ExpenseDate <= budget.EndDate &&
                        e.Status != ExpenseStatus.Rejected);

        if (budget.ExpenseCategoryId.HasValue)
        {
            query = query.Where(e => e.ExpenseCategoryId == budget.ExpenseCategoryId.Value);
        }

        budget.SpentAmount = await query.SumAsync(e => e.Amount, cancellationToken).ConfigureAwait(false);
        budget.LastCalculatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task UpdateBudgetSpentAmountAsync(int categoryId, DateTime expenseDate, CancellationToken cancellationToken)
    {
        var budgets = await _context.ExpenseBudgets
            .Where(b => b.IsActive &&
                        b.StartDate <= expenseDate &&
                        b.EndDate >= expenseDate &&
                        (b.ExpenseCategoryId == categoryId || !b.ExpenseCategoryId.HasValue))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var budget in budgets)
        {
            await RecalculateBudgetSpentAmountAsync(budget, cancellationToken).ConfigureAwait(false);
        }
    }

    #endregion

    #region Analytics & Reporting

    public async Task<PrimeCostDto> CalculatePrimeCostAsync(DateTime startDate, DateTime endDate, decimal totalSales, CancellationToken cancellationToken = default)
    {
        var expensesByType = await GetExpensesByTypeAsync(startDate, endDate, cancellationToken).ConfigureAwait(false);

        var cogs = expensesByType.GetValueOrDefault(ExpenseCategoryType.COGS, 0);
        var laborCost = expensesByType.GetValueOrDefault(ExpenseCategoryType.Labor, 0);
        var primeCost = cogs + laborCost;

        return new PrimeCostDto
        {
            TotalSales = totalSales,
            COGS = cogs,
            LaborCost = laborCost,
            PrimeCost = primeCost,
            PrimeCostPercentage = totalSales > 0 ? (primeCost / totalSales) * 100 : 0,
            FoodCostPercentage = totalSales > 0 ? (cogs / totalSales) * 100 : 0,
            LaborCostPercentage = totalSales > 0 ? (laborCost / totalSales) * 100 : 0,
            PeriodStart = startDate,
            PeriodEnd = endDate
        };
    }

    public async Task<Dictionary<DateTime, decimal>> GetExpenseTrendsAsync(DateTime startDate, DateTime endDate, string groupBy = "day", CancellationToken cancellationToken = default)
    {
        var expenses = await _context.Expenses
            .AsNoTracking()
            .Where(e => e.IsActive && e.ExpenseDate >= startDate && e.ExpenseDate <= endDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return groupBy.ToLower() switch
        {
            "week" => expenses.GroupBy(e => StartOfWeek(e.ExpenseDate))
                             .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount)),
            "month" => expenses.GroupBy(e => new DateTime(e.ExpenseDate.Year, e.ExpenseDate.Month, 1))
                              .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount)),
            _ => expenses.GroupBy(e => e.ExpenseDate.Date)
                        .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount))
        };
    }

    public async Task<Dictionary<string, decimal>> GetExpensesByCategoryAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.Expenses
            .AsNoTracking()
            .Include(e => e.ExpenseCategory)
            .Where(e => e.IsActive && e.ExpenseDate >= startDate && e.ExpenseDate <= endDate)
            .GroupBy(e => e.ExpenseCategory!.Name)
            .Select(g => new { Category = g.Key, Total = g.Sum(e => e.Amount) })
            .ToDictionaryAsync(x => x.Category, x => x.Total, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Dictionary<string, decimal>> GetExpensesBySupplierAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.Expenses
            .AsNoTracking()
            .Include(e => e.Supplier)
            .Where(e => e.IsActive && e.ExpenseDate >= startDate && e.ExpenseDate <= endDate && e.SupplierId.HasValue)
            .GroupBy(e => e.Supplier!.Name)
            .Select(g => new { Supplier = g.Key, Total = g.Sum(e => e.Amount) })
            .ToDictionaryAsync(x => x.Supplier, x => x.Total, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Dictionary<ExpenseCategoryType, decimal>> GetExpensesByTypeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var expenses = await _context.Expenses
            .AsNoTracking()
            .Include(e => e.ExpenseCategory)
            .Where(e => e.IsActive && e.ExpenseDate >= startDate && e.ExpenseDate <= endDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return expenses
            .GroupBy(e => e.ExpenseCategory?.Type ?? ExpenseCategoryType.Other)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));
    }

    public async Task<(ExpenseSummaryDto Current, ExpenseSummaryDto Previous, decimal PercentageChange)> ComparePeriodsAsync(
        DateTime currentStart, DateTime currentEnd,
        DateTime previousStart, DateTime previousEnd,
        CancellationToken cancellationToken = default)
    {
        var current = await GetExpenseSummaryAsync(currentStart, currentEnd, cancellationToken).ConfigureAwait(false);
        var previous = await GetExpenseSummaryAsync(previousStart, previousEnd, cancellationToken).ConfigureAwait(false);

        var percentageChange = previous.TotalAmount > 0
            ? ((current.TotalAmount - previous.TotalAmount) / previous.TotalAmount) * 100
            : 0;

        return (current, previous, percentageChange);
    }

    private static DateTime StartOfWeek(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff).Date;
    }

    #endregion

    #region Attachments

    public async Task<ExpenseAttachment> AddAttachmentAsync(int expenseId, string fileName, string filePath, string fileType, long fileSize, int uploadedByUserId, CancellationToken cancellationToken = default)
    {
        var attachment = new ExpenseAttachment
        {
            ExpenseId = expenseId,
            FileName = fileName,
            FilePath = filePath,
            FileType = fileType,
            FileSize = fileSize,
            UploadedByUserId = uploadedByUserId,
            UploadedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.ExpenseAttachments.Add(attachment);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Added attachment {FileName} to expense {ExpenseId}", fileName, expenseId);

        return attachment;
    }

    public async Task<bool> RemoveAttachmentAsync(int attachmentId, CancellationToken cancellationToken = default)
    {
        var attachment = await _context.ExpenseAttachments.FindAsync(new object[] { attachmentId }, cancellationToken).ConfigureAwait(false);
        if (attachment == null) return false;

        attachment.IsActive = false;
        attachment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Removed attachment {AttachmentId}", attachmentId);

        return true;
    }

    public async Task<IReadOnlyList<ExpenseAttachment>> GetAttachmentsAsync(int expenseId, CancellationToken cancellationToken = default)
    {
        return await _context.ExpenseAttachments
            .AsNoTracking()
            .Where(a => a.ExpenseId == expenseId && a.IsActive)
            .OrderByDescending(a => a.UploadedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    #endregion
}
