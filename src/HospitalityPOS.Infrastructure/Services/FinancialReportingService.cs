using Microsoft.EntityFrameworkCore;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for generating enhanced financial reports.
/// </summary>
public class FinancialReportingService : IFinancialReportingService
{
    private readonly POSDbContext _context;

    public FinancialReportingService(POSDbContext context)
    {
        _context = context;
    }

    #region Cash Flow Statement

    /// <inheritdoc />
    public async Task<CashFlowStatement> GenerateCashFlowStatementAsync(CashFlowStatementRequest request, CancellationToken cancellationToken = default)
    {
        var statement = new CashFlowStatement
        {
            StartDate = request.StartDate,
            EndDate = request.EndDate
        };

        // Get all journal entries for the period
        var journalEntries = await _context.JournalEntryLines
            .Include(jel => jel.JournalEntry)
            .Include(jel => jel.Account)
            .Where(jel => jel.JournalEntry.EntryDate >= request.StartDate &&
                         jel.JournalEntry.EntryDate <= request.EndDate &&
                         jel.JournalEntry.IsPosted)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Get cash flow mappings
        var mappings = await GetCashFlowMappingsAsync(cancellationToken).ConfigureAwait(false);
        var mappingDict = mappings.ToDictionary(m => m.AccountId);

        // Calculate opening cash balance
        var cashAccounts = await _context.ChartOfAccounts
            .Where(a => a.AccountType == "Asset" && a.Name.Contains("Cash"))
            .Select(a => a.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var openingCashEntries = await _context.JournalEntryLines
            .Where(jel => cashAccounts.Contains(jel.AccountId) &&
                         jel.JournalEntry.EntryDate < request.StartDate &&
                         jel.JournalEntry.IsPosted)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        statement.BeginningCashBalance = openingCashEntries.Sum(e => e.DebitAmount - e.CreditAmount);

        // Group entries by activity type
        var operatingItems = new List<CashFlowLineItem>();
        var investingItems = new List<CashFlowLineItem>();
        var financingItems = new List<CashFlowLineItem>();

        foreach (var entry in journalEntries.GroupBy(e => e.AccountId))
        {
            var accountId = entry.Key;
            var account = entry.First().Account;
            var netAmount = entry.Sum(e => e.DebitAmount - e.CreditAmount);

            if (mappingDict.TryGetValue(accountId, out var mapping))
            {
                var lineItem = new CashFlowLineItem
                {
                    Name = mapping.LineItem,
                    Amount = mapping.IsInflow ? netAmount : -netAmount,
                    IsInflow = mapping.IsInflow,
                    AccountId = accountId
                };

                switch (mapping.ActivityType)
                {
                    case CashFlowActivityType.Operating:
                        operatingItems.Add(lineItem);
                        break;
                    case CashFlowActivityType.Investing:
                        investingItems.Add(lineItem);
                        break;
                    case CashFlowActivityType.Financing:
                        financingItems.Add(lineItem);
                        break;
                }
            }
            else
            {
                // Default to operating for unmapped accounts
                var lineItem = new CashFlowLineItem
                {
                    Name = account?.Name ?? "Unknown",
                    Amount = netAmount,
                    IsInflow = netAmount > 0,
                    AccountId = accountId
                };
                operatingItems.Add(lineItem);
            }
        }

        statement.OperatingActivities = new CashFlowSection
        {
            ActivityType = CashFlowActivityType.Operating,
            LineItems = operatingItems
        };

        statement.InvestingActivities = new CashFlowSection
        {
            ActivityType = CashFlowActivityType.Investing,
            LineItems = investingItems
        };

        statement.FinancingActivities = new CashFlowSection
        {
            ActivityType = CashFlowActivityType.Financing,
            LineItems = financingItems
        };

        // Prior period comparison
        if (request.IncludePriorPeriodComparison)
        {
            var periodLength = request.EndDate - request.StartDate;
            var priorRequest = new CashFlowStatementRequest
            {
                StoreId = request.StoreId,
                StartDate = request.StartDate.AddDays(-periodLength.TotalDays - 1),
                EndDate = request.StartDate.AddDays(-1),
                IncludePriorPeriodComparison = false
            };
            statement.PriorPeriod = await GenerateCashFlowStatementAsync(priorRequest, cancellationToken).ConfigureAwait(false);
        }

        return statement;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CashFlowMapping>> GetCashFlowMappingsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.CashFlowMappings
            .Include(m => m.Account)
            .OrderBy(m => m.ActivityType)
            .ThenBy(m => m.DisplayOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<CashFlowMapping> SaveCashFlowMappingAsync(CashFlowMapping mapping, CancellationToken cancellationToken = default)
    {
        if (mapping.Id == 0)
        {
            _context.CashFlowMappings.Add(mapping);
        }
        else
        {
            _context.CashFlowMappings.Update(mapping);
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return mapping;
    }

    #endregion

    #region General Ledger

    /// <inheritdoc />
    public async Task<GeneralLedgerReport> GenerateGeneralLedgerReportAsync(GeneralLedgerReportRequest request, CancellationToken cancellationToken = default)
    {
        var account = await _context.ChartOfAccounts.FindAsync(new object[] { request.AccountId }, cancellationToken).ConfigureAwait(false);
        if (account == null)
        {
            throw new ArgumentException($"Account {request.AccountId} not found");
        }

        var report = new GeneralLedgerReport
        {
            AccountId = request.AccountId,
            AccountCode = account.AccountCode,
            AccountName = account.Name,
            StartDate = request.StartDate,
            EndDate = request.EndDate
        };

        // Calculate opening balance
        var openingEntries = await _context.JournalEntryLines
            .Where(jel => jel.AccountId == request.AccountId &&
                         jel.JournalEntry.EntryDate < request.StartDate &&
                         jel.JournalEntry.IsPosted)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        report.OpeningBalance = openingEntries.Sum(e => e.DebitAmount - e.CreditAmount);

        // Get transactions for the period
        var transactions = await _context.JournalEntryLines
            .Include(jel => jel.JournalEntry)
            .Where(jel => jel.AccountId == request.AccountId &&
                         jel.JournalEntry.EntryDate >= request.StartDate &&
                         jel.JournalEntry.EntryDate <= request.EndDate &&
                         jel.JournalEntry.IsPosted)
            .OrderBy(jel => jel.JournalEntry.EntryDate)
            .ThenBy(jel => jel.JournalEntry.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var runningBalance = report.OpeningBalance;
        foreach (var trans in transactions)
        {
            runningBalance += trans.DebitAmount - trans.CreditAmount;
            report.Transactions.Add(new GLTransaction
            {
                Date = trans.JournalEntry.EntryDate,
                Reference = trans.JournalEntry.ReferenceNumber,
                Description = trans.Description ?? trans.JournalEntry.Description,
                Debit = trans.DebitAmount,
                Credit = trans.CreditAmount,
                RunningBalance = runningBalance,
                JournalEntryId = trans.JournalEntryId,
                SourceDocument = trans.JournalEntry.SourceDocument
            });

            report.TotalDebits += trans.DebitAmount;
            report.TotalCredits += trans.CreditAmount;
        }

        return report;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<GLAccountActivity>> GetGLAccountActivityAsync(int accountId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var entries = await _context.JournalEntryLines
            .Include(jel => jel.JournalEntry)
            .Where(jel => jel.AccountId == accountId &&
                         jel.JournalEntry.EntryDate >= startDate &&
                         jel.JournalEntry.EntryDate <= endDate &&
                         jel.JournalEntry.IsPosted)
            .GroupBy(jel => jel.JournalEntry.EntryDate.Date)
            .Select(g => new GLAccountActivity
            {
                Date = g.Key,
                TotalDebits = g.Sum(e => e.DebitAmount),
                TotalCredits = g.Sum(e => e.CreditAmount),
                TransactionCount = g.Count()
            })
            .OrderBy(a => a.Date)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return entries;
    }

    #endregion

    #region Gross Margin Analysis

    /// <inheritdoc />
    public async Task<GrossMarginReport> GenerateGrossMarginReportAsync(GrossMarginReportRequest request, CancellationToken cancellationToken = default)
    {
        var report = new GrossMarginReport
        {
            StartDate = request.StartDate,
            EndDate = request.EndDate
        };

        // Get sales data
        var salesQuery = _context.ReceiptItems
            .Include(ri => ri.Receipt)
            .Include(ri => ri.Product)
            .ThenInclude(p => p!.Category)
            .Where(ri => ri.Receipt.ReceiptDate >= request.StartDate &&
                        ri.Receipt.ReceiptDate <= request.EndDate &&
                        ri.Receipt.IsPaid);

        if (request.StoreId.HasValue)
        {
            salesQuery = salesQuery.Where(ri => ri.Receipt.StoreId == request.StoreId);
        }

        if (request.CategoryId.HasValue)
        {
            salesQuery = salesQuery.Where(ri => ri.Product!.CategoryId == request.CategoryId);
        }

        var sales = await salesQuery.ToListAsync(cancellationToken).ConfigureAwait(false);

        report.TotalRevenue = sales.Sum(s => s.Quantity * s.UnitPrice);
        report.TotalCOGS = sales.Sum(s => s.Quantity * (s.Product?.Cost ?? 0));

        // Group by specified dimension
        switch (request.GroupBy.ToLower())
        {
            case "product":
                report.GroupSummaries = sales
                    .GroupBy(s => s.ProductId)
                    .Select(g => new MarginGroupSummary
                    {
                        GroupId = g.Key,
                        GroupName = g.First().Product?.Name ?? "Unknown",
                        GroupType = "Product",
                        Revenue = g.Sum(s => s.Quantity * s.UnitPrice),
                        COGS = g.Sum(s => s.Quantity * (s.Product?.Cost ?? 0)),
                        ItemCount = g.Count()
                    })
                    .OrderByDescending(s => s.Revenue)
                    .ToList();
                break;

            case "category":
            default:
                report.GroupSummaries = sales
                    .GroupBy(s => s.Product?.CategoryId)
                    .Select(g => new MarginGroupSummary
                    {
                        GroupId = g.Key,
                        GroupName = g.First().Product?.Category?.Name ?? "Uncategorized",
                        GroupType = "Category",
                        Revenue = g.Sum(s => s.Quantity * s.UnitPrice),
                        COGS = g.Sum(s => s.Quantity * (s.Product?.Cost ?? 0)),
                        ItemCount = g.Count()
                    })
                    .OrderByDescending(s => s.Revenue)
                    .ToList();
                break;
        }

        // Low margin alerts
        if (request.HighlightLowMargin)
        {
            report.LowMarginAlerts = await GetLowMarginAlertsAsync(request.StoreId, cancellationToken).ConfigureAwait(false);
        }

        return report;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ProductMarginDetail>> GetProductMarginsAsync(int? storeId, int? categoryId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var query = _context.ReceiptItems
            .Include(ri => ri.Receipt)
            .Include(ri => ri.Product)
            .ThenInclude(p => p!.Category)
            .Where(ri => ri.Receipt.ReceiptDate >= startDate &&
                        ri.Receipt.ReceiptDate <= endDate &&
                        ri.Receipt.IsPaid);

        if (storeId.HasValue)
        {
            query = query.Where(ri => ri.Receipt.StoreId == storeId);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(ri => ri.Product!.CategoryId == categoryId);
        }

        var sales = await query.ToListAsync(cancellationToken).ConfigureAwait(false);

        return sales
            .GroupBy(s => s.ProductId)
            .Select(g => new ProductMarginDetail
            {
                ProductId = g.Key,
                ProductCode = g.First().Product?.Code ?? "",
                ProductName = g.First().Product?.Name ?? "Unknown",
                CategoryName = g.First().Product?.Category?.Name,
                QuantitySold = g.Sum(s => s.Quantity),
                Revenue = g.Sum(s => s.Quantity * s.UnitPrice),
                COGS = g.Sum(s => s.Quantity * (s.Product?.Cost ?? 0))
            })
            .OrderByDescending(p => p.Revenue)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CategoryMarginDetail>> GetCategoryMarginsAsync(int? storeId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var query = _context.ReceiptItems
            .Include(ri => ri.Receipt)
            .Include(ri => ri.Product)
            .ThenInclude(p => p!.Category)
            .Where(ri => ri.Receipt.ReceiptDate >= startDate &&
                        ri.Receipt.ReceiptDate <= endDate &&
                        ri.Receipt.IsPaid);

        if (storeId.HasValue)
        {
            query = query.Where(ri => ri.Receipt.StoreId == storeId);
        }

        var sales = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        var totalRevenue = sales.Sum(s => s.Quantity * s.UnitPrice);

        return sales
            .GroupBy(s => s.Product?.CategoryId)
            .Select(g => new CategoryMarginDetail
            {
                CategoryId = g.Key ?? 0,
                CategoryName = g.First().Product?.Category?.Name ?? "Uncategorized",
                ProductCount = g.Select(s => s.ProductId).Distinct().Count(),
                Revenue = g.Sum(s => s.Quantity * s.UnitPrice),
                COGS = g.Sum(s => s.Quantity * (s.Product?.Cost ?? 0)),
                PercentOfTotalRevenue = totalRevenue > 0 ? (g.Sum(s => s.Quantity * s.UnitPrice) / totalRevenue) * 100 : 0
            })
            .OrderByDescending(c => c.Revenue)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<LowMarginAlert>> GetLowMarginAlertsAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        var thresholds = await GetMarginThresholdsAsync(storeId, cancellationToken).ConfigureAwait(false);
        var thresholdDict = thresholds.ToDictionary(t => t.ProductId ?? t.CategoryId ?? 0);

        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-30);

        var products = await GetProductMarginsAsync(storeId, null, startDate, endDate, cancellationToken).ConfigureAwait(false);

        var alerts = new List<LowMarginAlert>();
        var defaultThreshold = 20m; // Default 20% margin

        foreach (var product in products)
        {
            var threshold = thresholdDict.ContainsKey(product.ProductId)
                ? thresholdDict[product.ProductId].MinMarginPercent
                : defaultThreshold;

            if (product.MarginPercent < threshold)
            {
                alerts.Add(new LowMarginAlert
                {
                    ProductId = product.ProductId,
                    ProductCode = product.ProductCode,
                    ProductName = product.ProductName,
                    CurrentMarginPercent = product.MarginPercent,
                    ThresholdMarginPercent = threshold,
                    AlertLevel = product.MarginPercent < threshold * 0.5m ? "Critical" : "Warning"
                });
            }
        }

        return alerts.OrderBy(a => a.CurrentMarginPercent).ToList();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MarginThreshold>> GetMarginThresholdsAsync(int? storeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.MarginThresholds.AsQueryable();

        if (storeId.HasValue)
        {
            query = query.Where(t => t.StoreId == storeId || t.StoreId == null);
        }

        return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<MarginThreshold> SaveMarginThresholdAsync(MarginThreshold threshold, CancellationToken cancellationToken = default)
    {
        if (threshold.Id == 0)
        {
            _context.MarginThresholds.Add(threshold);
        }
        else
        {
            _context.MarginThresholds.Update(threshold);
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return threshold;
    }

    #endregion

    #region Comparative Reports

    /// <inheritdoc />
    public async Task<ComparativePLReport> GenerateComparativePLReportAsync(ComparativePLRequest request, CancellationToken cancellationToken = default)
    {
        var report = new ComparativePLReport
        {
            CurrentPeriodStart = request.CurrentPeriodStart,
            CurrentPeriodEnd = request.CurrentPeriodEnd,
            ComparisonPeriodStart = request.ComparisonPeriodStart,
            ComparisonPeriodEnd = request.ComparisonPeriodEnd
        };

        // Current period
        report.CurrentPeriod = await CalculatePLSummaryAsync(request.StoreId, request.CurrentPeriodStart, request.CurrentPeriodEnd, cancellationToken).ConfigureAwait(false);

        // Comparison period
        if (request.ComparisonPeriodStart.HasValue && request.ComparisonPeriodEnd.HasValue)
        {
            report.ComparisonPeriod = await CalculatePLSummaryAsync(request.StoreId, request.ComparisonPeriodStart.Value, request.ComparisonPeriodEnd.Value, cancellationToken).ConfigureAwait(false);
        }

        // Budget comparison
        if (request.IncludeBudgetComparison && request.BudgetId.HasValue)
        {
            report.Budget = await GetBudgetSummaryAsync(request.BudgetId.Value, request.CurrentPeriodStart, request.CurrentPeriodEnd, cancellationToken).ConfigureAwait(false);
        }

        return report;
    }

    /// <inheritdoc />
    public async Task<YearOverYearReport> GenerateYearOverYearReportAsync(int? storeId, DateTime currentPeriodStart, DateTime currentPeriodEnd, CancellationToken cancellationToken = default)
    {
        var priorYearStart = currentPeriodStart.AddYears(-1);
        var priorYearEnd = currentPeriodEnd.AddYears(-1);

        var report = new YearOverYearReport
        {
            CurrentPeriodStart = currentPeriodStart,
            CurrentPeriodEnd = currentPeriodEnd,
            PriorYearStart = priorYearStart,
            PriorYearEnd = priorYearEnd
        };

        report.CurrentYear = await CalculatePLSummaryAsync(storeId, currentPeriodStart, currentPeriodEnd, cancellationToken).ConfigureAwait(false);
        report.PriorYear = await CalculatePLSummaryAsync(storeId, priorYearStart, priorYearEnd, cancellationToken).ConfigureAwait(false);

        return report;
    }

    /// <inheritdoc />
    public async Task<BudgetVsActualReport> GenerateBudgetVsActualReportAsync(int? storeId, int budgetId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var budget = await _context.Budgets.FindAsync(new object[] { budgetId }, cancellationToken).ConfigureAwait(false);

        var report = new BudgetVsActualReport
        {
            BudgetId = budgetId,
            BudgetName = budget?.Name ?? "Unknown Budget",
            StartDate = startDate,
            EndDate = endDate
        };

        // Get budget lines
        var budgetLines = await _context.BudgetLines
            .Include(bl => bl.Account)
            .Where(bl => bl.BudgetId == budgetId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Get actual amounts
        var actuals = await _context.JournalEntryLines
            .Include(jel => jel.JournalEntry)
            .Include(jel => jel.Account)
            .Where(jel => jel.JournalEntry.EntryDate >= startDate &&
                         jel.JournalEntry.EntryDate <= endDate &&
                         jel.JournalEntry.IsPosted)
            .GroupBy(jel => jel.AccountId)
            .Select(g => new
            {
                AccountId = g.Key,
                ActualAmount = g.Sum(jel => jel.DebitAmount - jel.CreditAmount)
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var actualDict = actuals.ToDictionary(a => a.AccountId, a => a.ActualAmount);

        foreach (var budgetLine in budgetLines)
        {
            var actual = actualDict.GetValueOrDefault(budgetLine.AccountId, 0);
            var lineItem = new BudgetVsActualLineItem
            {
                AccountId = budgetLine.AccountId,
                AccountCode = budgetLine.Account.AccountCode,
                AccountName = budgetLine.Account.Name,
                BudgetAmount = budgetLine.Amount,
                ActualAmount = actual
            };

            report.LineItems.Add(lineItem);
            report.TotalBudgeted += budgetLine.Amount;
            report.TotalActual += actual;

            // Generate alerts for significant variances
            if (Math.Abs(lineItem.VariancePercent) > 10)
            {
                report.Alerts.Add(new BudgetAlert
                {
                    AccountId = budgetLine.AccountId,
                    AccountName = budgetLine.Account.Name,
                    VariancePercent = lineItem.VariancePercent,
                    AlertLevel = Math.Abs(lineItem.VariancePercent) > 25 ? "Critical" : "Warning",
                    Message = lineItem.IsOverBudget
                        ? $"Over budget by {lineItem.VariancePercent:F1}%"
                        : $"Under budget by {Math.Abs(lineItem.VariancePercent):F1}%"
                });
            }
        }

        return report;
    }

    #endregion

    #region Departmental P&L

    /// <inheritdoc />
    public async Task<DepartmentalPLReport> GenerateDepartmentalPLReportAsync(DepartmentalPLRequest request, CancellationToken cancellationToken = default)
    {
        var report = new DepartmentalPLReport
        {
            StartDate = request.StartDate,
            EndDate = request.EndDate
        };

        var departments = await _context.Departments
            .Where(d => d.IsEnabled && d.IsProfitCenter)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (request.StoreId.HasValue)
        {
            departments = departments.Where(d => d.StoreId == request.StoreId || d.StoreId == null).ToList();
        }

        if (request.DepartmentIds != null && request.DepartmentIds.Any())
        {
            departments = departments.Where(d => request.DepartmentIds.Contains(d.Id)).ToList();
        }

        var totalRevenue = 0m;

        foreach (var dept in departments)
        {
            var deptPL = await CalculateDepartmentPLAsync(dept, request.StartDate, request.EndDate, request.IncludeOverheadAllocation, cancellationToken).ConfigureAwait(false);
            report.DepartmentResults.Add(deptPL);
            totalRevenue += deptPL.Revenue;
        }

        // Calculate percent of total
        foreach (var deptPL in report.DepartmentResults)
        {
            deptPL.PercentOfTotalRevenue = totalRevenue > 0 ? (deptPL.Revenue / totalRevenue) * 100 : 0;
        }

        return report;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Department>> GetDepartmentsAsync(int? storeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Departments.Include(d => d.ParentDepartment).AsQueryable();

        if (storeId.HasValue)
        {
            query = query.Where(d => d.StoreId == storeId || d.StoreId == null);
        }

        return await query.OrderBy(d => d.DisplayOrder).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Department> CreateDepartmentAsync(Department department, CancellationToken cancellationToken = default)
    {
        _context.Departments.Add(department);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return department;
    }

    /// <inheritdoc />
    public async Task<Department> UpdateDepartmentAsync(Department department, CancellationToken cancellationToken = default)
    {
        _context.Departments.Update(department);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return department;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<OverheadAllocationRule>> GetOverheadAllocationRulesAsync(int? storeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.OverheadAllocationRules
            .Include(r => r.AllocationDetails)
            .AsQueryable();

        if (storeId.HasValue)
        {
            query = query.Where(r => r.StoreId == storeId || r.StoreId == null);
        }

        return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<OverheadAllocationRule> SaveOverheadAllocationRuleAsync(OverheadAllocationRule rule, CancellationToken cancellationToken = default)
    {
        if (rule.Id == 0)
        {
            _context.OverheadAllocationRules.Add(rule);
        }
        else
        {
            _context.OverheadAllocationRules.Update(rule);
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return rule;
    }

    #endregion

    #region Report Management

    /// <inheritdoc />
    public async Task<SavedReport> SaveReportConfigurationAsync(SavedReport report, CancellationToken cancellationToken = default)
    {
        if (report.Id == 0)
        {
            _context.SavedReports.Add(report);
        }
        else
        {
            _context.SavedReports.Update(report);
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return report;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SavedReport>> GetSavedReportsAsync(int? storeId = null, string? reportType = null, CancellationToken cancellationToken = default)
    {
        var query = _context.SavedReports.AsQueryable();

        if (storeId.HasValue)
        {
            query = query.Where(r => r.StoreId == storeId);
        }

        if (!string.IsNullOrEmpty(reportType))
        {
            query = query.Where(r => r.ReportType == reportType);
        }

        return await query.OrderBy(r => r.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteSavedReportAsync(int reportId, CancellationToken cancellationToken = default)
    {
        var report = await _context.SavedReports.FindAsync(new object[] { reportId }, cancellationToken).ConfigureAwait(false);
        if (report != null)
        {
            _context.SavedReports.Remove(report);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task LogReportExecutionAsync(ReportExecutionLog log, CancellationToken cancellationToken = default)
    {
        _context.ReportExecutionLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ReportExecutionLog>> GetReportExecutionHistoryAsync(int? savedReportId = null, int limit = 50, CancellationToken cancellationToken = default)
    {
        var query = _context.ReportExecutionLogs.AsQueryable();

        if (savedReportId.HasValue)
        {
            query = query.Where(l => l.SavedReportId == savedReportId);
        }

        return await query
            .OrderByDescending(l => l.StartedAt)
            .Take(limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<ReportExportResult> ExportReportAsync(string reportType, object reportData, string format, CancellationToken cancellationToken = default)
    {
        // Placeholder for export functionality
        // In production, this would generate PDF/Excel files
        return Task.FromResult(new ReportExportResult
        {
            IsSuccess = true,
            FileName = $"{reportType}_{DateTime.UtcNow:yyyyMMdd}.{format.ToLower()}",
            ContentType = format.ToLower() switch
            {
                "pdf" => "application/pdf",
                "excel" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "csv" => "text/csv",
                _ => "application/octet-stream"
            }
        });
    }

    #endregion

    #region Margin Trends

    /// <inheritdoc />
    public async Task<IEnumerable<MarginTrendPoint>> GetMarginTrendsAsync(int? storeId, int? categoryId, DateTime startDate, DateTime endDate, string interval = "day", CancellationToken cancellationToken = default)
    {
        var query = _context.ReceiptItems
            .Include(ri => ri.Receipt)
            .Include(ri => ri.Product)
            .Where(ri => ri.Receipt.ReceiptDate >= startDate &&
                        ri.Receipt.ReceiptDate <= endDate &&
                        ri.Receipt.IsPaid);

        if (storeId.HasValue)
        {
            query = query.Where(ri => ri.Receipt.StoreId == storeId);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(ri => ri.Product!.CategoryId == categoryId);
        }

        var sales = await query.ToListAsync(cancellationToken).ConfigureAwait(false);

        var grouped = interval.ToLower() switch
        {
            "week" => sales.GroupBy(s => new DateTime(s.Receipt.ReceiptDate.Year, s.Receipt.ReceiptDate.Month, s.Receipt.ReceiptDate.Day - (int)s.Receipt.ReceiptDate.DayOfWeek)),
            "month" => sales.GroupBy(s => new DateTime(s.Receipt.ReceiptDate.Year, s.Receipt.ReceiptDate.Month, 1)),
            _ => sales.GroupBy(s => s.Receipt.ReceiptDate.Date)
        };

        return grouped
            .Select(g => new MarginTrendPoint
            {
                Date = g.Key,
                Revenue = g.Sum(s => s.Quantity * s.UnitPrice),
                COGS = g.Sum(s => s.Quantity * (s.Product?.Cost ?? 0))
            })
            .OrderBy(t => t.Date)
            .ToList();
    }

    #endregion

    #region Private Methods

    private async Task<PLPeriodSummary> CalculatePLSummaryAsync(int? storeId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        var summary = new PLPeriodSummary();

        // Revenue from sales
        var salesQuery = _context.Receipts
            .Where(r => r.ReceiptDate >= startDate && r.ReceiptDate <= endDate && r.IsPaid);

        if (storeId.HasValue)
        {
            salesQuery = salesQuery.Where(r => r.StoreId == storeId);
        }

        var receipts = await salesQuery.Include(r => r.Items).ThenInclude(ri => ri.Product).ToListAsync(cancellationToken).ConfigureAwait(false);

        summary.Revenue = receipts.Sum(r => r.TotalAmount);
        summary.CostOfGoodsSold = receipts.SelectMany(r => r.Items).Sum(ri => ri.Quantity * (ri.Product?.Cost ?? 0));

        // Expenses from journal entries
        var expenseAccounts = await _context.ChartOfAccounts
            .Where(a => a.AccountType == "Expense")
            .Select(a => a.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var expenses = await _context.JournalEntryLines
            .Where(jel => expenseAccounts.Contains(jel.AccountId) &&
                         jel.JournalEntry.EntryDate >= startDate &&
                         jel.JournalEntry.EntryDate <= endDate &&
                         jel.JournalEntry.IsPosted)
            .SumAsync(jel => jel.DebitAmount - jel.CreditAmount, cancellationToken)
            .ConfigureAwait(false);

        summary.OperatingExpenses = expenses;

        return summary;
    }

    private async Task<PLPeriodSummary?> GetBudgetSummaryAsync(int budgetId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        var budgetLines = await _context.BudgetLines
            .Where(bl => bl.BudgetId == budgetId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!budgetLines.Any())
            return null;

        // This is a simplified budget summary - in production you'd want to prorate by period
        return new PLPeriodSummary
        {
            Revenue = budgetLines.Where(bl => bl.Account.AccountType == "Revenue").Sum(bl => bl.Amount),
            CostOfGoodsSold = budgetLines.Where(bl => bl.Account.Name.Contains("COGS")).Sum(bl => bl.Amount),
            OperatingExpenses = budgetLines.Where(bl => bl.Account.AccountType == "Expense").Sum(bl => bl.Amount)
        };
    }

    private async Task<DepartmentPL> CalculateDepartmentPLAsync(Department department, DateTime startDate, DateTime endDate, bool includeOverhead, CancellationToken cancellationToken)
    {
        var deptPL = new DepartmentPL
        {
            DepartmentId = department.Id,
            DepartmentCode = department.Code,
            DepartmentName = department.Name
        };

        // Get categories allocated to this department
        var categoryIds = string.IsNullOrEmpty(department.AllocatedCategoryIds)
            ? new List<int>()
            : department.AllocatedCategoryIds.Split(',').Select(int.Parse).ToList();

        if (categoryIds.Any())
        {
            // Get sales for department categories
            var sales = await _context.ReceiptItems
                .Include(ri => ri.Receipt)
                .Include(ri => ri.Product)
                .Where(ri => categoryIds.Contains(ri.Product!.CategoryId) &&
                            ri.Receipt.ReceiptDate >= startDate &&
                            ri.Receipt.ReceiptDate <= endDate &&
                            ri.Receipt.IsPaid)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            deptPL.Revenue = sales.Sum(s => s.Quantity * s.UnitPrice);
            deptPL.CostOfGoodsSold = sales.Sum(s => s.Quantity * (s.Product?.Cost ?? 0));
        }

        // Direct expenses (from GL account if mapped)
        if (department.GLAccountId.HasValue)
        {
            var directExpenses = await _context.JournalEntryLines
                .Where(jel => jel.AccountId == department.GLAccountId.Value &&
                             jel.JournalEntry.EntryDate >= startDate &&
                             jel.JournalEntry.EntryDate <= endDate &&
                             jel.JournalEntry.IsPosted)
                .SumAsync(jel => jel.DebitAmount - jel.CreditAmount, cancellationToken)
                .ConfigureAwait(false);

            deptPL.DirectExpenses = directExpenses;
        }

        // Overhead allocation
        if (includeOverhead)
        {
            var rules = await GetOverheadAllocationRulesAsync(department.StoreId, cancellationToken).ConfigureAwait(false);
            foreach (var rule in rules.Where(r => r.IsEnabled))
            {
                var detail = rule.AllocationDetails.FirstOrDefault(d => d.DepartmentId == department.Id);
                if (detail != null)
                {
                    // Get overhead expense amount
                    var overheadAmount = await _context.JournalEntryLines
                        .Where(jel => jel.AccountId == rule.SourceAccountId &&
                                     jel.JournalEntry.EntryDate >= startDate &&
                                     jel.JournalEntry.EntryDate <= endDate &&
                                     jel.JournalEntry.IsPosted)
                        .SumAsync(jel => jel.DebitAmount - jel.CreditAmount, cancellationToken)
                        .ConfigureAwait(false);

                    var allocated = overheadAmount * (detail.AllocationPercentage / 100);
                    deptPL.AllocatedOverhead += allocated;

                    deptPL.ExpenseDetails.Add(new DepartmentExpenseDetail
                    {
                        AccountId = rule.SourceAccountId,
                        AccountName = rule.Name,
                        Amount = allocated,
                        IsAllocated = true
                    });
                }
            }
        }

        return deptPL;
    }

    #endregion
}
