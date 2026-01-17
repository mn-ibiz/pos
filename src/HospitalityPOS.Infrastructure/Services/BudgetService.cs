using Microsoft.EntityFrameworkCore;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for budget and cost management.
/// </summary>
public class BudgetService : IBudgetService
{
    private readonly POSDbContext _context;

    public BudgetService(POSDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    #region Budget Management

    /// <inheritdoc />
    public async Task<Budget> CreateBudgetAsync(CreateBudgetRequest request, CancellationToken cancellationToken = default)
    {
        var budget = new Budget
        {
            StoreId = request.StoreId,
            Name = request.Name,
            Description = request.Description,
            FiscalYear = request.FiscalYear,
            PeriodType = request.PeriodType,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = BudgetStatus.Draft,
            CreatedByUserId = request.CreatedByUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Budgets.Add(budget);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return budget;
    }

    /// <inheritdoc />
    public async Task<Budget?> GetBudgetByIdAsync(int budgetId, CancellationToken cancellationToken = default)
    {
        return await _context.Budgets
            .Include(b => b.Store)
            .Include(b => b.Lines)
                .ThenInclude(l => l.Account)
            .Include(b => b.CreatedByUser)
            .Include(b => b.ApprovedByUser)
            .FirstOrDefaultAsync(b => b.Id == budgetId && !b.IsDeleted, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Budget>> GetBudgetsAsync(
        int? storeId = null,
        int? fiscalYear = null,
        BudgetStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Budgets
            .Include(b => b.Store)
            .Where(b => !b.IsDeleted);

        if (storeId.HasValue)
            query = query.Where(b => b.StoreId == storeId.Value);

        if (fiscalYear.HasValue)
            query = query.Where(b => b.FiscalYear == fiscalYear.Value);

        if (status.HasValue)
            query = query.Where(b => b.Status == status.Value);

        return await query
            .OrderByDescending(b => b.FiscalYear)
            .ThenBy(b => b.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Budget> UpdateBudgetAsync(Budget budget, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Budgets
            .FirstOrDefaultAsync(b => b.Id == budget.Id && !b.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (existing == null)
            throw new InvalidOperationException($"Budget {budget.Id} not found.");

        if (existing.Status != BudgetStatus.Draft)
            throw new InvalidOperationException("Can only update budgets in Draft status.");

        existing.Name = budget.Name;
        existing.Description = budget.Description;
        existing.PeriodType = budget.PeriodType;
        existing.StartDate = budget.StartDate;
        existing.EndDate = budget.EndDate;
        existing.Notes = budget.Notes;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return existing;
    }

    /// <inheritdoc />
    public async Task SubmitBudgetForApprovalAsync(int budgetId, CancellationToken cancellationToken = default)
    {
        var budget = await _context.Budgets
            .FirstOrDefaultAsync(b => b.Id == budgetId && !b.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (budget == null)
            throw new InvalidOperationException($"Budget {budgetId} not found.");

        if (budget.Status != BudgetStatus.Draft)
            throw new InvalidOperationException("Can only submit budgets in Draft status for approval.");

        // Validate budget has lines
        var hasLines = await _context.BudgetLines
            .AnyAsync(l => l.BudgetId == budgetId, cancellationToken)
            .ConfigureAwait(false);

        if (!hasLines)
            throw new InvalidOperationException("Cannot submit budget without any budget lines.");

        budget.Status = BudgetStatus.PendingApproval;
        budget.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ApproveBudgetAsync(int budgetId, int approverUserId, CancellationToken cancellationToken = default)
    {
        var budget = await _context.Budgets
            .FirstOrDefaultAsync(b => b.Id == budgetId && !b.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (budget == null)
            throw new InvalidOperationException($"Budget {budgetId} not found.");

        if (budget.Status != BudgetStatus.PendingApproval)
            throw new InvalidOperationException("Can only approve budgets in PendingApproval status.");

        budget.Status = BudgetStatus.Approved;
        budget.ApprovedByUserId = approverUserId;
        budget.ApprovedAt = DateTime.UtcNow;
        budget.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RejectBudgetAsync(int budgetId, string reason, CancellationToken cancellationToken = default)
    {
        var budget = await _context.Budgets
            .FirstOrDefaultAsync(b => b.Id == budgetId && !b.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (budget == null)
            throw new InvalidOperationException($"Budget {budgetId} not found.");

        if (budget.Status != BudgetStatus.PendingApproval)
            throw new InvalidOperationException("Can only reject budgets in PendingApproval status.");

        budget.Status = BudgetStatus.Draft;
        budget.Notes = $"Rejected: {reason}\n{budget.Notes}";
        budget.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task CloseBudgetAsync(int budgetId, CancellationToken cancellationToken = default)
    {
        var budget = await _context.Budgets
            .FirstOrDefaultAsync(b => b.Id == budgetId && !b.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (budget == null)
            throw new InvalidOperationException($"Budget {budgetId} not found.");

        if (budget.Status != BudgetStatus.Approved)
            throw new InvalidOperationException("Can only close approved budgets.");

        budget.Status = BudgetStatus.Closed;
        budget.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Budget> CreateBudgetFromPriorYearAsync(
        int storeId,
        int fiscalYear,
        decimal adjustmentPercent,
        CancellationToken cancellationToken = default)
    {
        // Find prior year budget
        var priorYearBudget = await _context.Budgets
            .Include(b => b.Lines)
            .Where(b => b.StoreId == storeId && b.FiscalYear == fiscalYear - 1 && !b.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (priorYearBudget == null)
            throw new InvalidOperationException($"No prior year budget found for store {storeId} and fiscal year {fiscalYear - 1}.");

        // Use transaction to ensure atomicity - budget and lines must be created together
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            // Create new budget
            var newBudget = new Budget
            {
                StoreId = storeId,
                Name = $"FY{fiscalYear} Budget (Based on Prior Year)",
                Description = $"Created from FY{fiscalYear - 1} with {adjustmentPercent}% adjustment",
                FiscalYear = fiscalYear,
                PeriodType = priorYearBudget.PeriodType,
                StartDate = priorYearBudget.StartDate.AddYears(1),
                EndDate = priorYearBudget.EndDate.AddYears(1),
                Status = BudgetStatus.Draft,
                IsBasedOnPriorYear = true,
                PriorYearAdjustmentPercent = adjustmentPercent,
                CreatedAt = DateTime.UtcNow
            };

            _context.Budgets.Add(newBudget);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // Copy lines with adjustment
            var adjustmentMultiplier = 1 + (adjustmentPercent / 100);
            foreach (var line in priorYearBudget.Lines)
            {
                var newLine = new BudgetLine
                {
                    BudgetId = newBudget.Id,
                    AccountId = line.AccountId,
                    DepartmentId = line.DepartmentId,
                    PeriodNumber = line.PeriodNumber,
                    Amount = Math.Round(line.Amount * adjustmentMultiplier, 2),
                    Notes = $"Adjusted {adjustmentPercent}% from prior year",
                    CreatedAt = DateTime.UtcNow
                };
                _context.BudgetLines.Add(newLine);
            }

            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return newBudget;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Budget> CopyBudgetAsync(
        int sourceBudgetId,
        string newName,
        int newFiscalYear,
        CancellationToken cancellationToken = default)
    {
        var sourceBudget = await _context.Budgets
            .Include(b => b.Lines)
            .FirstOrDefaultAsync(b => b.Id == sourceBudgetId && !b.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (sourceBudget == null)
            throw new InvalidOperationException($"Source budget {sourceBudgetId} not found.");

        // Use transaction to ensure atomicity - budget and lines must be created together
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            // Create new budget
            var newBudget = new Budget
            {
                StoreId = sourceBudget.StoreId,
                Name = newName,
                Description = $"Copied from {sourceBudget.Name}",
                FiscalYear = newFiscalYear,
                PeriodType = sourceBudget.PeriodType,
                StartDate = new DateTime(newFiscalYear, sourceBudget.StartDate.Month, sourceBudget.StartDate.Day),
                EndDate = new DateTime(newFiscalYear, sourceBudget.EndDate.Month, sourceBudget.EndDate.Day),
                Status = BudgetStatus.Draft,
                CreatedAt = DateTime.UtcNow
            };

            _context.Budgets.Add(newBudget);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // Copy lines
            foreach (var line in sourceBudget.Lines)
            {
                var newLine = new BudgetLine
                {
                    BudgetId = newBudget.Id,
                    AccountId = line.AccountId,
                    DepartmentId = line.DepartmentId,
                    PeriodNumber = line.PeriodNumber,
                    Amount = line.Amount,
                    Notes = line.Notes,
                    CreatedAt = DateTime.UtcNow
                };
                _context.BudgetLines.Add(newLine);
            }

            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return newBudget;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    #endregion

    #region Budget Lines

    /// <inheritdoc />
    public async Task<IEnumerable<BudgetLine>> GetBudgetLinesAsync(int budgetId, CancellationToken cancellationToken = default)
    {
        return await _context.BudgetLines
            .Include(l => l.Account)
            .Include(l => l.Department)
            .Where(l => l.BudgetId == budgetId)
            .OrderBy(l => l.Account.AccountCode)
            .ThenBy(l => l.PeriodNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<BudgetLine> AddBudgetLineAsync(BudgetLine line, CancellationToken cancellationToken = default)
    {
        var budget = await _context.Budgets
            .FirstOrDefaultAsync(b => b.Id == line.BudgetId && !b.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (budget == null)
            throw new InvalidOperationException($"Budget {line.BudgetId} not found.");

        if (budget.Status != BudgetStatus.Draft)
            throw new InvalidOperationException("Can only add lines to budgets in Draft status.");

        line.CreatedAt = DateTime.UtcNow;
        _context.BudgetLines.Add(line);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return line;
    }

    /// <inheritdoc />
    public async Task<BudgetLine> UpdateBudgetLineAsync(BudgetLine line, CancellationToken cancellationToken = default)
    {
        var existing = await _context.BudgetLines
            .Include(l => l.Budget)
            .FirstOrDefaultAsync(l => l.Id == line.Id, cancellationToken)
            .ConfigureAwait(false);

        if (existing == null)
            throw new InvalidOperationException($"Budget line {line.Id} not found.");

        if (existing.Budget.Status != BudgetStatus.Draft)
            throw new InvalidOperationException("Can only update lines in budgets with Draft status.");

        existing.AccountId = line.AccountId;
        existing.DepartmentId = line.DepartmentId;
        existing.PeriodNumber = line.PeriodNumber;
        existing.Amount = line.Amount;
        existing.Notes = line.Notes;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return existing;
    }

    /// <inheritdoc />
    public async Task DeleteBudgetLineAsync(int lineId, CancellationToken cancellationToken = default)
    {
        var line = await _context.BudgetLines
            .Include(l => l.Budget)
            .FirstOrDefaultAsync(l => l.Id == lineId, cancellationToken)
            .ConfigureAwait(false);

        if (line == null)
            throw new InvalidOperationException($"Budget line {lineId} not found.");

        if (line.Budget.Status != BudgetStatus.Draft)
            throw new InvalidOperationException("Can only delete lines from budgets in Draft status.");

        _context.BudgetLines.Remove(line);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task BulkUpdateBudgetLinesAsync(IEnumerable<BudgetLine> lines, CancellationToken cancellationToken = default)
    {
        var linesList = lines.ToList();
        if (!linesList.Any())
            return;

        var budgetId = linesList.First().BudgetId;
        var budget = await _context.Budgets
            .FirstOrDefaultAsync(b => b.Id == budgetId && !b.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (budget == null)
            throw new InvalidOperationException($"Budget {budgetId} not found.");

        if (budget.Status != BudgetStatus.Draft)
            throw new InvalidOperationException("Can only bulk update lines in budgets with Draft status.");

        foreach (var line in linesList)
        {
            var existing = await _context.BudgetLines
                .FirstOrDefaultAsync(l => l.Id == line.Id && l.BudgetId == budgetId, cancellationToken)
                .ConfigureAwait(false);

            if (existing != null)
            {
                existing.Amount = line.Amount;
                existing.Notes = line.Notes;
                existing.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Budget vs Actual Tracking

    /// <inheritdoc />
    public async Task<BudgetVsActualSummary> GetBudgetVsActualSummaryAsync(
        int budgetId,
        DateTime asOfDate,
        CancellationToken cancellationToken = default)
    {
        var budget = await _context.Budgets
            .Include(b => b.Lines)
                .ThenInclude(l => l.Account)
            .FirstOrDefaultAsync(b => b.Id == budgetId && !b.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (budget == null)
            throw new InvalidOperationException($"Budget {budgetId} not found.");

        var summary = new BudgetVsActualSummary
        {
            BudgetId = budgetId,
            BudgetName = budget.Name,
            AsOfDate = asOfDate,
            DaysInPeriod = (int)(budget.EndDate - budget.StartDate).TotalDays + 1,
            DaysElapsed = Math.Max(0, Math.Min((int)(asOfDate - budget.StartDate).TotalDays + 1, (int)(budget.EndDate - budget.StartDate).TotalDays + 1))
        };

        // Get actual amounts from journal entries
        var actualsByAccount = await _context.JournalEntryLines
            .Include(l => l.JournalEntry)
            .Where(l => l.JournalEntry.IsPosted &&
                       l.JournalEntry.EntryDate >= budget.StartDate &&
                       l.JournalEntry.EntryDate <= asOfDate)
            .GroupBy(l => l.AccountId)
            .Select(g => new
            {
                AccountId = g.Key,
                TotalDebit = g.Sum(l => l.DebitAmount),
                TotalCredit = g.Sum(l => l.CreditAmount)
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Group budget lines by account
        var budgetByAccount = budget.Lines
            .GroupBy(l => l.AccountId)
            .Select(g => new
            {
                AccountId = g.Key,
                Account = g.First().Account,
                TotalBudgeted = g.Sum(l => l.Amount)
            })
            .ToList();

        foreach (var budgetAccount in budgetByAccount)
        {
            var actual = actualsByAccount.FirstOrDefault(a => a.AccountId == budgetAccount.AccountId);
            var actualAmount = actual != null
                ? (budgetAccount.Account.AccountType == "Expense" || budgetAccount.Account.AccountType == "Asset"
                    ? actual.TotalDebit - actual.TotalCredit
                    : actual.TotalCredit - actual.TotalDebit)
                : 0;

            summary.AccountSummaries.Add(new BudgetAccountSummary
            {
                AccountId = budgetAccount.AccountId,
                AccountCode = budgetAccount.Account.AccountCode,
                AccountName = budgetAccount.Account.Name,
                Budgeted = budgetAccount.TotalBudgeted,
                Actual = actualAmount
            });

            summary.TotalBudgeted += budgetAccount.TotalBudgeted;
            summary.TotalActual += actualAmount;
        }

        return summary;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BudgetVarianceAlert>> GetBudgetVarianceAlertsAsync(
        int budgetId,
        decimal varianceThresholdPercent = 10,
        CancellationToken cancellationToken = default)
    {
        var summary = await GetBudgetVsActualSummaryAsync(budgetId, DateTime.UtcNow, cancellationToken)
            .ConfigureAwait(false);

        var alerts = new List<BudgetVarianceAlert>();

        foreach (var account in summary.AccountSummaries)
        {
            if (Math.Abs(account.VariancePercent) >= varianceThresholdPercent)
            {
                var alertLevel = Math.Abs(account.VariancePercent) >= varianceThresholdPercent * 2
                    ? "Critical"
                    : "Warning";

                var message = account.Variance > 0
                    ? $"Over budget by {account.VariancePercent:N1}%"
                    : $"Under budget by {Math.Abs(account.VariancePercent):N1}%";

                alerts.Add(new BudgetVarianceAlert
                {
                    AccountId = account.AccountId,
                    AccountCode = account.AccountCode,
                    AccountName = account.AccountName,
                    Budgeted = account.Budgeted,
                    Actual = account.Actual,
                    VariancePercent = account.VariancePercent,
                    AlertLevel = alertLevel,
                    Message = message
                });
            }
        }

        return alerts.OrderByDescending(a => Math.Abs(a.VariancePercent));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BudgetUtilization>> GetBudgetUtilizationAsync(
        int budgetId,
        CancellationToken cancellationToken = default)
    {
        var summary = await GetBudgetVsActualSummaryAsync(budgetId, DateTime.UtcNow, cancellationToken)
            .ConfigureAwait(false);

        var budget = await GetBudgetByIdAsync(budgetId, cancellationToken).ConfigureAwait(false);
        if (budget == null)
            throw new InvalidOperationException($"Budget {budgetId} not found.");

        var monthsElapsed = Math.Max(1, (int)Math.Ceiling((DateTime.UtcNow - budget.StartDate).TotalDays / 30.0));
        var monthsRemaining = Math.Max(0, (int)Math.Ceiling((budget.EndDate - DateTime.UtcNow).TotalDays / 30.0));

        var utilizations = new List<BudgetUtilization>();

        foreach (var account in summary.AccountSummaries)
        {
            var monthlyAvg = monthsElapsed > 0 ? account.Actual / monthsElapsed : 0;

            utilizations.Add(new BudgetUtilization
            {
                AccountId = account.AccountId,
                AccountName = account.AccountName,
                AnnualBudget = account.Budgeted,
                YTDActual = account.Actual,
                MonthlyAvgSpend = monthlyAvg,
                MonthsRemaining = monthsRemaining
            });
        }

        return utilizations.OrderByDescending(u => u.PercentUsed);
    }

    #endregion

    #region Recurring Expense Templates

    /// <inheritdoc />
    public async Task<IEnumerable<RecurringExpenseTemplate>> GetRecurringExpenseTemplatesAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.RecurringExpenseTemplates
            .Include(t => t.Store)
            .Include(t => t.ExpenseCategory)
            .Include(t => t.Account)
            .Include(t => t.Department)
            .Where(t => !t.IsDeleted);

        if (storeId.HasValue)
            query = query.Where(t => t.StoreId == storeId.Value || t.StoreId == null);

        return await query
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<RecurringExpenseTemplate> CreateRecurringExpenseTemplateAsync(
        RecurringExpenseTemplate template,
        CancellationToken cancellationToken = default)
    {
        template.CreatedAt = DateTime.UtcNow;
        template.NextScheduledDate = CalculateNextScheduledDate(template, template.StartDate);

        _context.RecurringExpenseTemplates.Add(template);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return template;
    }

    /// <inheritdoc />
    public async Task<RecurringExpenseTemplate> UpdateRecurringExpenseTemplateAsync(
        RecurringExpenseTemplate template,
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.RecurringExpenseTemplates
            .FirstOrDefaultAsync(t => t.Id == template.Id && !t.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (existing == null)
            throw new InvalidOperationException($"Recurring expense template {template.Id} not found.");

        existing.Name = template.Name;
        existing.Description = template.Description;
        existing.ExpenseCategoryId = template.ExpenseCategoryId;
        existing.AccountId = template.AccountId;
        existing.Amount = template.Amount;
        existing.IsVariableAmount = template.IsVariableAmount;
        existing.Frequency = template.Frequency;
        existing.DayOfMonth = template.DayOfMonth;
        existing.DayOfWeek = template.DayOfWeek;
        existing.AutoPost = template.AutoPost;
        existing.IsEnabled = template.IsEnabled;
        existing.EndDate = template.EndDate;
        existing.DepartmentId = template.DepartmentId;
        existing.SupplierId = template.SupplierId;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return existing;
    }

    /// <inheritdoc />
    public async Task DeleteRecurringExpenseTemplateAsync(int templateId, CancellationToken cancellationToken = default)
    {
        var template = await _context.RecurringExpenseTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId && !t.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (template == null)
            throw new InvalidOperationException($"Recurring expense template {templateId} not found.");

        template.IsDeleted = true;
        template.DeletedAt = DateTime.UtcNow;
        template.IsEnabled = false;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<RecurringExpenseProcessResult> ProcessDueRecurringExpensesAsync(
        DateTime asOfDate,
        CancellationToken cancellationToken = default)
    {
        var result = new RecurringExpenseProcessResult();

        var dueTemplates = await _context.RecurringExpenseTemplates
            .Include(t => t.ExpenseCategory)
            .Include(t => t.Account)
            .Where(t => t.IsEnabled &&
                       !t.IsDeleted &&
                       t.NextScheduledDate <= asOfDate &&
                       (t.EndDate == null || t.EndDate >= asOfDate))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var template in dueTemplates)
        {
            result.TotalProcessed++;

            try
            {
                var entry = new RecurringExpenseEntry
                {
                    TemplateId = template.Id,
                    ScheduledDate = template.NextScheduledDate ?? asOfDate,
                    Amount = template.Amount,
                    CreatedAt = DateTime.UtcNow
                };

                if (template.IsVariableAmount)
                {
                    // Variable amount requires confirmation
                    entry.Status = "Pending";
                    result.PendingConfirmationCount++;
                }
                else if (template.AutoPost)
                {
                    // Auto-post creates the expense automatically
                    var expense = await CreateExpenseFromTemplateAsync(template, entry.ScheduledDate, cancellationToken)
                        .ConfigureAwait(false);
                    entry.ExpenseId = expense?.Id;
                    entry.Status = "Generated";
                    entry.ProcessedAt = DateTime.UtcNow;
                    result.SuccessCount++;
                }
                else
                {
                    // Creates as draft
                    entry.Status = "Generated";
                    entry.ProcessedAt = DateTime.UtcNow;
                    result.SuccessCount++;
                }

                _context.RecurringExpenseEntries.Add(entry);

                result.Details.Add(new RecurringExpenseProcessDetail
                {
                    TemplateId = template.Id,
                    TemplateName = template.Name,
                    GeneratedExpenseId = entry.ExpenseId,
                    IsSuccess = true,
                    RequiresConfirmation = template.IsVariableAmount
                });

                // Update next scheduled date
                template.LastGeneratedDate = asOfDate;
                template.NextScheduledDate = CalculateNextScheduledDate(template, asOfDate);
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.Details.Add(new RecurringExpenseProcessDetail
                {
                    TemplateId = template.Id,
                    TemplateName = template.Name,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return result;
    }

    /// <inheritdoc />
    public async Task<RecurringExpenseEntry> ConfirmRecurringExpenseAmountAsync(
        int entryId,
        decimal amount,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var entry = await _context.RecurringExpenseEntries
            .Include(e => e.Template)
            .FirstOrDefaultAsync(e => e.Id == entryId, cancellationToken)
            .ConfigureAwait(false);

        if (entry == null)
            throw new InvalidOperationException($"Recurring expense entry {entryId} not found.");

        if (entry.Status != "Pending")
            throw new InvalidOperationException("Entry is not pending confirmation.");

        entry.Amount = amount;
        entry.ConfirmedByUserId = userId;
        entry.Status = "Generated";
        entry.ProcessedAt = DateTime.UtcNow;

        // Create the expense
        var expense = await CreateExpenseFromTemplateAsync(entry.Template, entry.ScheduledDate, cancellationToken, amount)
            .ConfigureAwait(false);
        entry.ExpenseId = expense?.Id;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return entry;
    }

    /// <inheritdoc />
    public async Task SkipRecurringExpenseEntryAsync(
        int entryId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var entry = await _context.RecurringExpenseEntries
            .FirstOrDefaultAsync(e => e.Id == entryId, cancellationToken)
            .ConfigureAwait(false);

        if (entry == null)
            throw new InvalidOperationException($"Recurring expense entry {entryId} not found.");

        entry.Status = "Skipped";
        entry.Notes = reason;
        entry.ProcessedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RecurringExpenseEntry>> GetPendingRecurringExpenseEntriesAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.RecurringExpenseEntries
            .Include(e => e.Template)
                .ThenInclude(t => t.ExpenseCategory)
            .Include(e => e.Template)
                .ThenInclude(t => t.Store)
            .Where(e => e.Status == "Pending");

        if (storeId.HasValue)
            query = query.Where(e => e.Template.StoreId == storeId.Value || e.Template.StoreId == null);

        return await query
            .OrderBy(e => e.ScheduledDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    #endregion

    #region Cost Center Management

    /// <inheritdoc />
    public async Task<IEnumerable<CostCenterExpenseSummary>> GetCostCenterExpenseSummaryAsync(
        int? storeId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Expenses
            .Include(e => e.Category)
            .Include(e => e.Department)
            .Where(e => e.ExpenseDate >= startDate && e.ExpenseDate <= endDate && !e.IsDeleted);

        if (storeId.HasValue)
            query = query.Where(e => e.StoreId == storeId.Value);

        var expenses = await query.ToListAsync(cancellationToken).ConfigureAwait(false);

        var summaries = expenses
            .Where(e => e.DepartmentId.HasValue)
            .GroupBy(e => e.DepartmentId!.Value)
            .Select(g => new CostCenterExpenseSummary
            {
                DepartmentId = g.Key,
                DepartmentCode = g.First().Department?.Code ?? "",
                DepartmentName = g.First().Department?.Name ?? "Unknown",
                TotalExpenses = g.Sum(e => e.Amount),
                ExpenseCount = g.Count(),
                ExpensesByCategory = g.GroupBy(e => e.CategoryId)
                    .Select(cg => new ExpenseCategorySummary
                    {
                        CategoryId = cg.Key,
                        CategoryName = cg.First().Category?.Name ?? "Unknown",
                        Amount = cg.Sum(e => e.Amount),
                        Count = cg.Count()
                    })
                    .ToList()
            })
            .ToList();

        // Get budgeted amounts for departments
        var budgets = await _context.Budgets
            .Include(b => b.Lines)
            .Where(b => b.Status == BudgetStatus.Approved &&
                       b.StartDate <= endDate &&
                       b.EndDate >= startDate &&
                       (!storeId.HasValue || b.StoreId == storeId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var summary in summaries)
        {
            var budgetedAmount = budgets
                .SelectMany(b => b.Lines)
                .Where(l => l.DepartmentId == summary.DepartmentId)
                .Sum(l => l.Amount);

            summary.BudgetedAmount = budgetedAmount;
        }

        return summaries.OrderByDescending(s => s.TotalExpenses);
    }

    /// <inheritdoc />
    public async Task AllocateExpenseToCostCentersAsync(
        int expenseId,
        IEnumerable<ExpenseCostCenterAllocation> allocations,
        CancellationToken cancellationToken = default)
    {
        var expense = await _context.Expenses
            .FirstOrDefaultAsync(e => e.Id == expenseId && !e.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (expense == null)
            throw new InvalidOperationException($"Expense {expenseId} not found.");

        var allocationList = allocations.ToList();
        var totalAllocation = allocationList.Sum(a => a.Amount);

        if (Math.Abs(totalAllocation - expense.Amount) > 0.01m)
            throw new InvalidOperationException("Total allocation amount must equal expense amount.");

        // For now, we'll update the expense's primary department to the highest allocation
        var primaryAllocation = allocationList.OrderByDescending(a => a.Amount).First();
        expense.DepartmentId = primaryAllocation.DepartmentId;
        expense.UpdatedAt = DateTime.UtcNow;

        // In a full implementation, we would create expense allocation records
        // to track multi-department allocations

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Private Methods

    private DateTime? CalculateNextScheduledDate(RecurringExpenseTemplate template, DateTime fromDate)
    {
        var nextDate = fromDate.Date;

        switch (template.Frequency)
        {
            case "Daily":
                nextDate = nextDate.AddDays(1);
                break;

            case "Weekly":
                var targetDayOfWeek = template.DayOfWeek ?? 0;
                var daysUntilTarget = ((int)targetDayOfWeek - (int)nextDate.DayOfWeek + 7) % 7;
                if (daysUntilTarget == 0)
                    daysUntilTarget = 7;
                nextDate = nextDate.AddDays(daysUntilTarget);
                break;

            case "Monthly":
                nextDate = nextDate.AddMonths(1);
                var targetDay = template.DayOfMonth ?? 1;
                var daysInMonth = DateTime.DaysInMonth(nextDate.Year, nextDate.Month);
                nextDate = new DateTime(nextDate.Year, nextDate.Month, Math.Min(targetDay, daysInMonth));
                break;

            case "Quarterly":
                nextDate = nextDate.AddMonths(3);
                var qTargetDay = template.DayOfMonth ?? 1;
                var qDaysInMonth = DateTime.DaysInMonth(nextDate.Year, nextDate.Month);
                nextDate = new DateTime(nextDate.Year, nextDate.Month, Math.Min(qTargetDay, qDaysInMonth));
                break;

            case "Annually":
                nextDate = nextDate.AddYears(1);
                break;

            default:
                nextDate = nextDate.AddMonths(1);
                break;
        }

        if (template.EndDate.HasValue && nextDate > template.EndDate.Value)
            return null;

        return nextDate;
    }

    private async Task<Expense?> CreateExpenseFromTemplateAsync(
        RecurringExpenseTemplate template,
        DateTime expenseDate,
        CancellationToken cancellationToken,
        decimal? overrideAmount = null)
    {
        var amount = overrideAmount ?? template.Amount ?? 0;

        var expense = new Expense
        {
            StoreId = template.StoreId ?? 0,
            CategoryId = template.ExpenseCategoryId,
            AccountId = template.AccountId,
            ExpenseDate = expenseDate,
            Amount = amount,
            Description = $"Recurring: {template.Name}",
            DepartmentId = template.DepartmentId,
            SupplierId = template.SupplierId,
            Status = template.AutoPost ? "Approved" : "Draft",
            CreatedAt = DateTime.UtcNow
        };

        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return expense;
    }

    #endregion
}
