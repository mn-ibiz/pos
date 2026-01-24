using Microsoft.EntityFrameworkCore;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;
using System.Text;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service implementation for accounting operations.
/// </summary>
public class AccountingService : IAccountingService
{
    private readonly POSDbContext _context;

    public AccountingService(POSDbContext context)
    {
        _context = context;
    }

    #region Chart of Accounts

    public async Task<ChartOfAccount> CreateAccountAsync(ChartOfAccount account, int? userId = null, CancellationToken cancellationToken = default)
    {
        // Set normal balance based on account type
        if (account.NormalBalance == default)
        {
            account.NormalBalance = (account.AccountType == AccountType.Asset || account.AccountType == AccountType.Expense)
                ? NormalBalance.Debit : NormalBalance.Credit;
        }

        // Set level and full path if parent exists
        if (account.ParentAccountId.HasValue)
        {
            var parent = await _context.ChartOfAccounts.FindAsync([account.ParentAccountId.Value], cancellationToken);
            if (parent != null)
            {
                account.Level = parent.Level + 1;
                account.FullPath = string.IsNullOrEmpty(parent.FullPath)
                    ? $"{parent.AccountName} > {account.AccountName}"
                    : $"{parent.FullPath} > {account.AccountName}";
            }
        }

        if (userId.HasValue)
        {
            account.CreatedByUserId = userId.Value;
        }

        _context.ChartOfAccounts.Add(account);
        await _context.SaveChangesAsync(cancellationToken);
        return account;
    }

    public async Task<ChartOfAccount?> GetAccountByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.ChartOfAccounts
            .Include(a => a.ParentAccount)
            .Include(a => a.SubAccounts)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<ChartOfAccount?> GetAccountByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.ChartOfAccounts
            .FirstOrDefaultAsync(a => a.AccountCode == code, cancellationToken);
    }

    public async Task<IReadOnlyList<ChartOfAccount>> GetAllAccountsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ChartOfAccounts
            .Include(a => a.ParentAccount)
            .OrderBy(a => a.AccountCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ChartOfAccount>> GetAccountsByTypeAsync(AccountType accountType, CancellationToken cancellationToken = default)
    {
        return await _context.ChartOfAccounts
            .Where(a => a.AccountType == accountType)
            .OrderBy(a => a.AccountCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<ChartOfAccount> UpdateAccountAsync(ChartOfAccount account, int? userId = null, CancellationToken cancellationToken = default)
    {
        if (userId.HasValue)
        {
            account.UpdatedAt = DateTime.UtcNow;
        }
        _context.ChartOfAccounts.Update(account);
        await _context.SaveChangesAsync(cancellationToken);
        return account;
    }

    public async Task<bool> DeleteAccountAsync(int id, int? userId = null, CancellationToken cancellationToken = default)
    {
        var account = await _context.ChartOfAccounts.FindAsync([id], cancellationToken);
        if (account == null || account.IsSystemAccount) return false;

        // Check if account has transactions
        var hasTransactions = await _context.JournalEntryLines.AnyAsync(l => l.AccountId == id, cancellationToken);
        if (hasTransactions) return false;

        // Soft delete by deactivating
        account.IsActive = false;
        account.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<ChartOfAccount>> GetAccountsBySubTypeAsync(AccountSubType subType, CancellationToken cancellationToken = default)
    {
        return await _context.ChartOfAccounts
            .Where(a => a.AccountSubType == subType && a.IsActive)
            .OrderBy(a => a.AccountCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ChartOfAccount>> GetChildAccountsAsync(int parentAccountId, CancellationToken cancellationToken = default)
    {
        return await _context.ChartOfAccounts
            .Where(a => a.ParentAccountId == parentAccountId && a.IsActive)
            .OrderBy(a => a.AccountCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ChartOfAccount>> GetAccountHierarchyAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ChartOfAccounts
            .Include(a => a.SubAccounts)
            .Where(a => a.ParentAccountId == null && a.IsActive)
            .OrderBy(a => a.AccountType)
            .ThenBy(a => a.AccountCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> RecalculateAccountBalanceAsync(int accountId, CancellationToken cancellationToken = default)
    {
        var account = await _context.ChartOfAccounts.FindAsync([accountId], cancellationToken);
        if (account == null) return 0;

        var balance = await GetAccountBalanceAsOfAsync(accountId, DateTime.UtcNow, cancellationToken);
        account.CurrentBalance = balance;
        account.BalanceLastCalculated = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return balance;
    }

    public async Task RecalculateAllBalancesAsync(CancellationToken cancellationToken = default)
    {
        var accounts = await _context.ChartOfAccounts.Where(a => a.IsActive).ToListAsync(cancellationToken);
        foreach (var account in accounts)
        {
            var balance = await CalculateAccountBalanceAsync(account.Id, DateTime.UtcNow, cancellationToken);
            account.CurrentBalance = balance;
            account.BalanceLastCalculated = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<decimal> GetAccountBalanceAsOfAsync(int accountId, DateTime asOfDate, CancellationToken cancellationToken = default)
    {
        return await CalculateAccountBalanceAsync(accountId, asOfDate, cancellationToken);
    }

    public async Task<string> GetNextAccountCodeAsync(AccountType accountType, int? parentAccountId = null, CancellationToken cancellationToken = default)
    {
        int baseCode = accountType switch
        {
            AccountType.Asset => 1000,
            AccountType.Liability => 2000,
            AccountType.Equity => 3000,
            AccountType.Revenue => 4000,
            AccountType.Expense => 5000,
            _ => 9000
        };

        var query = _context.ChartOfAccounts.Where(a => a.AccountType == accountType);
        if (parentAccountId.HasValue)
        {
            query = query.Where(a => a.ParentAccountId == parentAccountId);
        }

        var lastAccount = await query
            .OrderByDescending(a => a.AccountCode)
            .FirstOrDefaultAsync(cancellationToken);

        if (lastAccount == null)
        {
            return baseCode.ToString();
        }

        if (int.TryParse(lastAccount.AccountCode, out int lastCode))
        {
            return (lastCode + 10).ToString();
        }

        return (baseCode + 10).ToString();
    }

    public async Task SeedDefaultAccountsAsync(CancellationToken cancellationToken = default)
    {
        if (await _context.ChartOfAccounts.AnyAsync(cancellationToken)) return;

        var defaultAccounts = new List<ChartOfAccount>
        {
            // Assets (1000-1999)
            new() { AccountCode = "1000", AccountName = "Cash", AccountType = AccountType.Asset, IsSystemAccount = true },
            new() { AccountCode = "1010", AccountName = "Cash on Hand", AccountType = AccountType.Asset, IsSystemAccount = true },
            new() { AccountCode = "1020", AccountName = "Cash in Bank", AccountType = AccountType.Asset, IsSystemAccount = true },
            new() { AccountCode = "1100", AccountName = "Accounts Receivable", AccountType = AccountType.Asset, IsSystemAccount = true },
            new() { AccountCode = "1200", AccountName = "Inventory", AccountType = AccountType.Asset, IsSystemAccount = true },
            new() { AccountCode = "1300", AccountName = "Prepaid Expenses", AccountType = AccountType.Asset, IsSystemAccount = true },
            new() { AccountCode = "1500", AccountName = "Fixed Assets", AccountType = AccountType.Asset, IsSystemAccount = true },
            new() { AccountCode = "1510", AccountName = "Equipment", AccountType = AccountType.Asset, IsSystemAccount = true },
            new() { AccountCode = "1520", AccountName = "Furniture & Fixtures", AccountType = AccountType.Asset, IsSystemAccount = true },
            new() { AccountCode = "1590", AccountName = "Accumulated Depreciation", AccountType = AccountType.Asset, IsSystemAccount = true },

            // Liabilities (2000-2999)
            new() { AccountCode = "2000", AccountName = "Accounts Payable", AccountType = AccountType.Liability, IsSystemAccount = true },
            new() { AccountCode = "2100", AccountName = "Accrued Expenses", AccountType = AccountType.Liability, IsSystemAccount = true },
            new() { AccountCode = "2200", AccountName = "VAT Payable", AccountType = AccountType.Liability, IsSystemAccount = true },
            new() { AccountCode = "2300", AccountName = "PAYE Payable", AccountType = AccountType.Liability, IsSystemAccount = true },
            new() { AccountCode = "2310", AccountName = "NHIF Payable", AccountType = AccountType.Liability, IsSystemAccount = true },
            new() { AccountCode = "2320", AccountName = "NSSF Payable", AccountType = AccountType.Liability, IsSystemAccount = true },
            new() { AccountCode = "2330", AccountName = "Housing Levy Payable", AccountType = AccountType.Liability, IsSystemAccount = true },
            new() { AccountCode = "2500", AccountName = "Loans Payable", AccountType = AccountType.Liability, IsSystemAccount = true },

            // Equity (3000-3999)
            new() { AccountCode = "3000", AccountName = "Owner's Capital", AccountType = AccountType.Equity, IsSystemAccount = true },
            new() { AccountCode = "3100", AccountName = "Retained Earnings", AccountType = AccountType.Equity, IsSystemAccount = true },
            new() { AccountCode = "3200", AccountName = "Current Year Earnings", AccountType = AccountType.Equity, IsSystemAccount = true },

            // Revenue (4000-4999)
            new() { AccountCode = "4000", AccountName = "Sales Revenue", AccountType = AccountType.Revenue, IsSystemAccount = true },
            new() { AccountCode = "4100", AccountName = "Food Sales", AccountType = AccountType.Revenue, IsSystemAccount = true },
            new() { AccountCode = "4200", AccountName = "Beverage Sales", AccountType = AccountType.Revenue, IsSystemAccount = true },
            new() { AccountCode = "4300", AccountName = "Other Revenue", AccountType = AccountType.Revenue, IsSystemAccount = true },
            new() { AccountCode = "4900", AccountName = "Discounts Given", AccountType = AccountType.Revenue, IsSystemAccount = true },

            // Expenses (5000-5999)
            new() { AccountCode = "5000", AccountName = "Cost of Goods Sold", AccountType = AccountType.Expense, IsSystemAccount = true },
            new() { AccountCode = "5100", AccountName = "Food Cost", AccountType = AccountType.Expense, IsSystemAccount = true },
            new() { AccountCode = "5200", AccountName = "Beverage Cost", AccountType = AccountType.Expense, IsSystemAccount = true },
            new() { AccountCode = "6000", AccountName = "Operating Expenses", AccountType = AccountType.Expense, IsSystemAccount = true },
            new() { AccountCode = "6100", AccountName = "Salaries & Wages", AccountType = AccountType.Expense, IsSystemAccount = true },
            new() { AccountCode = "6200", AccountName = "Rent Expense", AccountType = AccountType.Expense, IsSystemAccount = true },
            new() { AccountCode = "6300", AccountName = "Utilities Expense", AccountType = AccountType.Expense, IsSystemAccount = true },
            new() { AccountCode = "6400", AccountName = "Insurance Expense", AccountType = AccountType.Expense, IsSystemAccount = true },
            new() { AccountCode = "6500", AccountName = "Depreciation Expense", AccountType = AccountType.Expense, IsSystemAccount = true },
            new() { AccountCode = "6600", AccountName = "Supplies Expense", AccountType = AccountType.Expense, IsSystemAccount = true },
            new() { AccountCode = "6700", AccountName = "Repairs & Maintenance", AccountType = AccountType.Expense, IsSystemAccount = true },
            new() { AccountCode = "6800", AccountName = "Bank Charges", AccountType = AccountType.Expense, IsSystemAccount = true },
            new() { AccountCode = "6900", AccountName = "Miscellaneous Expense", AccountType = AccountType.Expense, IsSystemAccount = true }
        };

        _context.ChartOfAccounts.AddRange(defaultAccounts);
        await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion

    #region Accounting Periods

    public async Task<AccountingPeriod> CreatePeriodAsync(string name, DateTime startDate, DateTime endDate, int? fiscalYear = null, CancellationToken cancellationToken = default)
    {
        var period = new AccountingPeriod
        {
            PeriodName = name,
            PeriodCode = $"{startDate:yyyy-MM}",
            StartDate = startDate,
            EndDate = endDate,
            FiscalYear = fiscalYear ?? startDate.Year,
            PeriodNumber = startDate.Month,
            PeriodType = "Monthly",
            Status = AccountingPeriodStatus.Open
        };

        _context.AccountingPeriods.Add(period);
        await _context.SaveChangesAsync(cancellationToken);
        return period;
    }

    public async Task<IReadOnlyList<AccountingPeriod>> CreateFiscalYearPeriodsAsync(int year, int userId, DateTime? fiscalYearStart = null, CancellationToken cancellationToken = default)
    {
        var startDate = fiscalYearStart ?? new DateTime(year, 1, 1);
        var periods = new List<AccountingPeriod>();

        for (int i = 0; i < 12; i++)
        {
            var periodStart = startDate.AddMonths(i);
            var periodEnd = periodStart.AddMonths(1).AddDays(-1);

            var period = new AccountingPeriod
            {
                PeriodName = periodStart.ToString("MMMM yyyy"),
                PeriodCode = periodStart.ToString("yyyy-MM"),
                StartDate = periodStart,
                EndDate = periodEnd,
                FiscalYear = year,
                PeriodNumber = i + 1,
                PeriodType = "Monthly",
                Status = AccountingPeriodStatus.Open,
                CreatedByUserId = userId
            };

            periods.Add(period);
        }

        _context.AccountingPeriods.AddRange(periods);
        await _context.SaveChangesAsync(cancellationToken);
        return periods;
    }

    public async Task<AccountingPeriod?> GetCurrentPeriodAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        return await _context.AccountingPeriods
            .FirstOrDefaultAsync(p => p.StartDate <= today && p.EndDate >= today && p.Status == AccountingPeriodStatus.Open, cancellationToken);
    }

    public async Task<AccountingPeriod?> GetPeriodForDateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        return await _context.AccountingPeriods
            .FirstOrDefaultAsync(p => p.StartDate <= date && p.EndDate >= date, cancellationToken);
    }

    public async Task<IReadOnlyList<AccountingPeriod>> GetAllPeriodsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AccountingPeriods
            .OrderByDescending(p => p.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AccountingPeriod>> GetPeriodsByFiscalYearAsync(int fiscalYear, CancellationToken cancellationToken = default)
    {
        return await _context.AccountingPeriods
            .Where(p => p.FiscalYear == fiscalYear)
            .OrderBy(p => p.PeriodNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<AccountingPeriod> LockPeriodAsync(int periodId, int userId, CancellationToken cancellationToken = default)
    {
        var period = await _context.AccountingPeriods.FindAsync([periodId], cancellationToken)
            ?? throw new InvalidOperationException($"Accounting period with ID {periodId} not found.");

        period.IsLocked = true;
        period.LockedByUserId = userId;
        period.LockedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return period;
    }

    public async Task<AccountingPeriod> UnlockPeriodAsync(int periodId, int userId, CancellationToken cancellationToken = default)
    {
        var period = await _context.AccountingPeriods.FindAsync([periodId], cancellationToken)
            ?? throw new InvalidOperationException($"Accounting period with ID {periodId} not found.");

        period.IsLocked = false;
        period.LockedByUserId = null;
        period.LockedAt = null;

        await _context.SaveChangesAsync(cancellationToken);
        return period;
    }

    public async Task<AccountingPeriod> ClosePeriodAsync(int periodId, int closedByUserId, CancellationToken cancellationToken = default)
    {
        var period = await _context.AccountingPeriods.FindAsync([periodId], cancellationToken)
            ?? throw new InvalidOperationException($"Accounting period with ID {periodId} not found.");

        // Calculate period totals
        var entries = await _context.JournalEntries
            .Include(e => e.JournalEntryLines)
            .Where(e => e.AccountingPeriodId == periodId && e.Status == JournalEntryStatus.Posted)
            .ToListAsync(cancellationToken);

        var revenueAccounts = await _context.ChartOfAccounts
            .Where(a => a.AccountType == AccountType.Revenue)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);

        var expenseAccounts = await _context.ChartOfAccounts
            .Where(a => a.AccountType == AccountType.Expense)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);

        var allLines = entries.SelectMany(e => e.JournalEntryLines).ToList();
        period.TotalRevenue = allLines.Where(l => revenueAccounts.Contains(l.AccountId)).Sum(l => l.CreditAmount - l.DebitAmount);
        period.TotalExpenses = allLines.Where(l => expenseAccounts.Contains(l.AccountId)).Sum(l => l.DebitAmount - l.CreditAmount);
        period.NetIncome = period.TotalRevenue - period.TotalExpenses;
        period.FinancialsLastCalculated = DateTime.UtcNow;

        period.Status = AccountingPeriodStatus.Closed;
        period.IsLocked = true;
        period.ClosedByUserId = closedByUserId;
        period.ClosedAt = DateTime.UtcNow;

        // Create period close record
        var periodClose = new PeriodClose
        {
            AccountingPeriodId = periodId,
            Status = PeriodCloseStatus.Completed,
            InitiatedByUserId = closedByUserId,
            InitiatedAt = DateTime.UtcNow,
            CompletedByUserId = closedByUserId,
            CompletedAt = DateTime.UtcNow,
            TotalRevenue = period.TotalRevenue ?? 0,
            TotalExpenses = period.TotalExpenses ?? 0,
            NetIncome = period.NetIncome ?? 0
        };
        _context.PeriodCloses.Add(periodClose);

        await _context.SaveChangesAsync(cancellationToken);
        return period;
    }

    public async Task<AccountingPeriod> ReopenPeriodAsync(int periodId, int userId, string reason, CancellationToken cancellationToken = default)
    {
        var period = await _context.AccountingPeriods.FindAsync([periodId], cancellationToken)
            ?? throw new InvalidOperationException($"Accounting period with ID {periodId} not found.");

        var lastClose = await _context.PeriodCloses
            .Where(pc => pc.AccountingPeriodId == periodId)
            .OrderByDescending(pc => pc.CompletedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (lastClose != null)
        {
            lastClose.Status = PeriodCloseStatus.Reopened;
            lastClose.ReopenedByUserId = userId;
            lastClose.ReopenedAt = DateTime.UtcNow;
            lastClose.ReopenReason = reason;
        }

        period.Status = AccountingPeriodStatus.Open;
        period.IsLocked = false;
        period.ClosedByUserId = null;
        period.ClosedAt = null;

        await _context.SaveChangesAsync(cancellationToken);
        return period;
    }

    public async Task<PeriodClose> PerformYearEndCloseAsync(int fiscalYear, int userId, int retainedEarningsAccountId, CancellationToken cancellationToken = default)
    {
        // Get all periods for the fiscal year
        var periods = await GetPeriodsByFiscalYearAsync(fiscalYear, cancellationToken);
        var lastPeriod = periods.LastOrDefault()
            ?? throw new InvalidOperationException($"No periods found for fiscal year {fiscalYear}");

        // Calculate total revenue and expenses for the year
        var revenueAccounts = await _context.ChartOfAccounts
            .Where(a => a.AccountType == AccountType.Revenue && a.IsActive)
            .ToListAsync(cancellationToken);

        var expenseAccounts = await _context.ChartOfAccounts
            .Where(a => a.AccountType == AccountType.Expense && a.IsActive)
            .ToListAsync(cancellationToken);

        var yearStart = periods.First().StartDate;
        var yearEnd = periods.Last().EndDate;

        decimal totalRevenue = 0;
        foreach (var account in revenueAccounts)
        {
            totalRevenue += await CalculateAccountBalanceForPeriodAsync(account.Id, yearStart, yearEnd, cancellationToken);
        }

        decimal totalExpenses = 0;
        foreach (var account in expenseAccounts)
        {
            totalExpenses += await CalculateAccountBalanceForPeriodAsync(account.Id, yearStart, yearEnd, cancellationToken);
        }

        var netIncome = totalRevenue - totalExpenses;

        // Create closing entries
        var closingEntryLines = new List<JournalEntryLine>();

        // Close revenue accounts to Income Summary
        foreach (var account in revenueAccounts)
        {
            var balance = await CalculateAccountBalanceForPeriodAsync(account.Id, yearStart, yearEnd, cancellationToken);
            if (balance != 0)
            {
                closingEntryLines.Add(new JournalEntryLine
                {
                    AccountId = account.Id,
                    Description = $"Year-end closing - {account.AccountName}",
                    DebitAmount = balance,
                    CreditAmount = 0
                });
            }
        }

        // Close expense accounts
        foreach (var account in expenseAccounts)
        {
            var balance = await CalculateAccountBalanceForPeriodAsync(account.Id, yearStart, yearEnd, cancellationToken);
            if (balance != 0)
            {
                closingEntryLines.Add(new JournalEntryLine
                {
                    AccountId = account.Id,
                    Description = $"Year-end closing - {account.AccountName}",
                    DebitAmount = 0,
                    CreditAmount = balance
                });
            }
        }

        // Close to Retained Earnings
        closingEntryLines.Add(new JournalEntryLine
        {
            AccountId = retainedEarningsAccountId,
            Description = $"Net Income for FY{fiscalYear}",
            DebitAmount = netIncome < 0 ? Math.Abs(netIncome) : 0,
            CreditAmount = netIncome > 0 ? netIncome : 0
        });

        var closingEntry = new JournalEntry
        {
            EntryNumber = await GenerateEntryNumberAsync(cancellationToken),
            EntryDate = yearEnd,
            Description = $"Year-end closing entry FY{fiscalYear}",
            ReferenceType = "YearEndClose",
            ReferenceId = fiscalYear,
            AccountingPeriodId = lastPeriod.Id,
            Status = JournalEntryStatus.Posted,
            IsPosted = true,
            IsClosingEntry = true,
            CreatedByUserId = userId,
            JournalEntryLines = closingEntryLines
        };

        _context.JournalEntries.Add(closingEntry);

        lastPeriod.IsYearEndClosed = true;
        lastPeriod.YearEndClosingEntryId = closingEntry.Id;

        var periodClose = new PeriodClose
        {
            AccountingPeriodId = lastPeriod.Id,
            Status = PeriodCloseStatus.Completed,
            InitiatedByUserId = userId,
            InitiatedAt = DateTime.UtcNow,
            CompletedByUserId = userId,
            CompletedAt = DateTime.UtcNow,
            TotalRevenue = totalRevenue,
            TotalExpenses = totalExpenses,
            NetIncome = netIncome,
            Notes = $"Year-end closing for fiscal year {fiscalYear}"
        };

        _context.PeriodCloses.Add(periodClose);
        await _context.SaveChangesAsync(cancellationToken);

        return periodClose;
    }

    public async Task<IReadOnlyList<PeriodClose>> GetPeriodCloseHistoryAsync(int periodId, CancellationToken cancellationToken = default)
    {
        return await _context.PeriodCloses
            .Include(pc => pc.InitiatedByUser)
            .Include(pc => pc.CompletedByUser)
            .Where(pc => pc.AccountingPeriodId == periodId)
            .OrderByDescending(pc => pc.CompletedAt)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Journal Entries

    public async Task<JournalEntry> CreateJournalEntryAsync(JournalEntry entry, int? userId = null, CancellationToken cancellationToken = default)
    {
        // Validate balanced entry
        var totalDebits = entry.JournalEntryLines.Sum(l => l.DebitAmount);
        var totalCredits = entry.JournalEntryLines.Sum(l => l.CreditAmount);

        if (Math.Abs(totalDebits - totalCredits) > 0.01m)
        {
            throw new InvalidOperationException("Journal entry must be balanced (debits must equal credits).");
        }

        if (string.IsNullOrEmpty(entry.EntryNumber))
        {
            entry.EntryNumber = await GenerateEntryNumberAsync(cancellationToken);
        }

        entry.TotalDebits = totalDebits;
        entry.TotalCredits = totalCredits;

        if (userId.HasValue)
        {
            entry.CreatedByUserId = userId.Value;
        }

        // Assign to current period if not specified
        if (!entry.AccountingPeriodId.HasValue)
        {
            var currentPeriod = await GetPeriodForDateAsync(entry.EntryDate, cancellationToken);
            entry.AccountingPeriodId = currentPeriod?.Id;
        }

        _context.JournalEntries.Add(entry);
        await _context.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public async Task<JournalEntry?> GetJournalEntryByNumberAsync(string entryNumber, CancellationToken cancellationToken = default)
    {
        return await _context.JournalEntries
            .Include(e => e.JournalEntryLines)
                .ThenInclude(l => l.Account)
            .FirstOrDefaultAsync(e => e.EntryNumber == entryNumber, cancellationToken);
    }

    public async Task<IReadOnlyList<JournalEntry>> GetJournalEntriesByReferenceAsync(string referenceType, int referenceId, CancellationToken cancellationToken = default)
    {
        return await _context.JournalEntries
            .Include(e => e.JournalEntryLines)
                .ThenInclude(l => l.Account)
            .Where(e => e.ReferenceType == referenceType && e.ReferenceId == referenceId)
            .OrderByDescending(e => e.EntryDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<JournalEntryValidationResult> ValidateJournalEntryAsync(JournalEntry entry, CancellationToken cancellationToken = default)
    {
        var result = new JournalEntryValidationResult { IsValid = true };

        // Check balance
        var totalDebits = entry.JournalEntryLines.Sum(l => l.DebitAmount);
        var totalCredits = entry.JournalEntryLines.Sum(l => l.CreditAmount);
        result.IsBalanced = Math.Abs(totalDebits - totalCredits) < 0.01m;
        if (!result.IsBalanced)
        {
            result.Errors.Add("Debits and credits must be equal.");
            result.IsValid = false;
        }

        // Check accounts exist and are valid
        var accountIds = entry.JournalEntryLines.Select(l => l.AccountId).Distinct().ToList();
        var accounts = await _context.ChartOfAccounts
            .Where(a => accountIds.Contains(a.Id))
            .ToListAsync(cancellationToken);

        result.AccountsAreValid = accounts.Count == accountIds.Count;
        if (!result.AccountsAreValid)
        {
            result.Errors.Add("One or more account IDs are invalid.");
            result.IsValid = false;
        }

        foreach (var account in accounts)
        {
            if (!account.IsActive)
            {
                result.Errors.Add($"Account {account.AccountCode} is inactive.");
                result.IsValid = false;
            }
            if (account.IsHeaderAccount)
            {
                result.Errors.Add($"Account {account.AccountCode} is a header account and cannot accept postings.");
                result.IsValid = false;
            }
        }

        // Check period
        var period = await GetPeriodForDateAsync(entry.EntryDate, cancellationToken);
        result.PeriodIsOpen = period != null && period.Status == AccountingPeriodStatus.Open && !period.IsLocked;
        if (!result.PeriodIsOpen)
        {
            result.Warnings.Add("No open accounting period for this date, or period is locked.");
        }

        return result;
    }

    public async Task<JournalEntry> ApproveJournalEntryAsync(int entryId, int userId, CancellationToken cancellationToken = default)
    {
        var entry = await _context.JournalEntries.FindAsync([entryId], cancellationToken)
            ?? throw new InvalidOperationException($"Journal entry with ID {entryId} not found.");

        entry.IsApproved = true;
        entry.ApprovedByUserId = userId;
        entry.ApprovedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public async Task<JournalEntry> RejectJournalEntryAsync(int entryId, int userId, string reason, CancellationToken cancellationToken = default)
    {
        var entry = await _context.JournalEntries.FindAsync([entryId], cancellationToken)
            ?? throw new InvalidOperationException($"Journal entry with ID {entryId} not found.");

        entry.IsApproved = false;
        entry.Status = JournalEntryStatus.Draft;
        entry.Notes = (entry.Notes ?? "") + $"\nRejected: {reason}";

        await _context.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public async Task<JournalEntry> PostJournalEntryAsync(int entryId, int? userId = null, CancellationToken cancellationToken = default)
    {
        var entry = await _context.JournalEntries.FindAsync([entryId], cancellationToken)
            ?? throw new InvalidOperationException($"Journal entry with ID {entryId} not found.");

        entry.Status = JournalEntryStatus.Posted;
        entry.IsPosted = true;
        entry.PostedAt = DateTime.UtcNow;
        if (userId.HasValue)
        {
            entry.PostedByUserId = userId.Value;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public async Task<JournalEntry> ReverseJournalEntryAsync(int entryId, string reason, int? userId = null, CancellationToken cancellationToken = default)
    {
        var original = await GetJournalEntryByIdAsync(entryId, cancellationToken)
            ?? throw new InvalidOperationException($"Journal entry with ID {entryId} not found.");

        // Mark original as reversed
        original.Status = JournalEntryStatus.Reversed;
        original.ReversedByEntryId = 0; // Will be updated after reversal is created

        // Create reversing entry
        var reversal = new JournalEntry
        {
            EntryNumber = await GenerateEntryNumberAsync(cancellationToken),
            EntryDate = DateTime.Today,
            Description = $"Reversal of {original.EntryNumber}: {reason}",
            ReferenceType = "Reversal",
            ReferenceId = original.Id,
            SourceType = original.SourceType,
            AccountingPeriodId = original.AccountingPeriodId,
            Status = JournalEntryStatus.Posted,
            IsPosted = true,
            IsReversing = true,
            ReversesEntryId = original.Id,
            CreatedByUserId = userId ?? original.CreatedByUserId
        };

        // Swap debits and credits
        foreach (var line in original.JournalEntryLines)
        {
            reversal.JournalEntryLines.Add(new JournalEntryLine
            {
                AccountId = line.AccountId,
                Description = $"Reversal: {line.Description}",
                DebitAmount = line.CreditAmount,
                CreditAmount = line.DebitAmount
            });
        }

        reversal.TotalDebits = reversal.JournalEntryLines.Sum(l => l.DebitAmount);
        reversal.TotalCredits = reversal.JournalEntryLines.Sum(l => l.CreditAmount);

        _context.JournalEntries.Add(reversal);
        await _context.SaveChangesAsync(cancellationToken);

        original.ReversedByEntryId = reversal.Id;
        await _context.SaveChangesAsync(cancellationToken);

        return reversal;
    }

    public async Task<JournalEntry?> GetJournalEntryByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.JournalEntries
            .Include(e => e.JournalEntryLines)
                .ThenInclude(l => l.Account)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<JournalEntry>> GetJournalEntriesByPeriodAsync(int periodId, CancellationToken cancellationToken = default)
    {
        return await _context.JournalEntries
            .Include(e => e.JournalEntryLines)
                .ThenInclude(l => l.Account)
            .Where(e => e.AccountingPeriodId == periodId)
            .OrderByDescending(e => e.EntryDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<JournalEntry>> GetJournalEntriesByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.JournalEntries
            .Include(e => e.JournalEntryLines)
                .ThenInclude(l => l.Account)
            .Where(e => e.EntryDate >= startDate && e.EntryDate <= endDate)
            .OrderByDescending(e => e.EntryDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<JournalEntry>> GetJournalEntriesByAccountAsync(int accountId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var query = _context.JournalEntries
            .Include(e => e.JournalEntryLines)
                .ThenInclude(l => l.Account)
            .Where(e => e.JournalEntryLines.Any(l => l.AccountId == accountId));

        if (startDate.HasValue)
        {
            query = query.Where(e => e.EntryDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(e => e.EntryDate <= endDate.Value);
        }

        return await query.OrderByDescending(e => e.EntryDate).ToListAsync(cancellationToken);
    }

    public async Task<JournalEntry> PostJournalEntryAsync(int entryId, CancellationToken cancellationToken = default)
    {
        var entry = await _context.JournalEntries.FindAsync([entryId], cancellationToken)
            ?? throw new InvalidOperationException($"Journal entry with ID {entryId} not found.");

        entry.Status = JournalEntryStatus.Posted;
        await _context.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public async Task<JournalEntry> ReverseJournalEntryAsync(int entryId, string reason, CancellationToken cancellationToken = default)
    {
        var original = await GetJournalEntryByIdAsync(entryId, cancellationToken)
            ?? throw new InvalidOperationException($"Journal entry with ID {entryId} not found.");

        // Mark original as reversed
        original.Status = JournalEntryStatus.Reversed;

        // Create reversing entry
        var reversal = new JournalEntry
        {
            EntryNumber = await GenerateEntryNumberAsync(cancellationToken),
            EntryDate = DateTime.Today,
            Description = $"Reversal of {original.EntryNumber}: {reason}",
            ReferenceType = "Reversal",
            ReferenceId = original.Id,
            AccountingPeriodId = original.AccountingPeriodId,
            Status = JournalEntryStatus.Posted
        };

        // Swap debits and credits
        foreach (var line in original.JournalEntryLines)
        {
            reversal.JournalEntryLines.Add(new JournalEntryLine
            {
                AccountId = line.AccountId,
                Description = $"Reversal: {line.Description}",
                DebitAmount = line.CreditAmount,
                CreditAmount = line.DebitAmount
            });
        }

        _context.JournalEntries.Add(reversal);
        await _context.SaveChangesAsync(cancellationToken);
        return reversal;
    }

    public async Task<string> GenerateEntryNumberAsync(CancellationToken cancellationToken = default)
    {
        var lastEntry = await _context.JournalEntries
            .OrderByDescending(e => e.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var nextNumber = (lastEntry?.Id ?? 0) + 1;
        return $"JE{DateTime.Today:yyyyMM}{nextNumber:D5}";
    }

    #endregion

    #region Automatic Journal Posting

    public async Task<JournalEntry> PostSalesJournalAsync(int receiptId, CancellationToken cancellationToken = default)
    {
        var receipt = await _context.Receipts
            .Include(r => r.Payments)
            .FirstOrDefaultAsync(r => r.Id == receiptId, cancellationToken)
            ?? throw new InvalidOperationException("Receipt not found.");

        var cashAccount = await GetAccountByCodeAsync("1000", cancellationToken);
        var salesAccount = await GetAccountByCodeAsync("4000", cancellationToken);

        if (cashAccount == null || salesAccount == null)
        {
            await SeedDefaultAccountsAsync(cancellationToken);
            cashAccount = await GetAccountByCodeAsync("1000", cancellationToken);
            salesAccount = await GetAccountByCodeAsync("4000", cancellationToken);
        }

        var entry = new JournalEntry
        {
            EntryNumber = await GenerateEntryNumberAsync(cancellationToken),
            EntryDate = DateTime.Today,
            Description = $"Sales - Receipt #{receiptId}",
            ReferenceType = "Receipt",
            ReferenceId = receiptId,
            Status = JournalEntryStatus.Posted,
            JournalEntryLines =
            [
                new JournalEntryLine
                {
                    AccountId = cashAccount!.Id,
                    Description = "Cash received",
                    DebitAmount = receipt.TotalAmount,
                    CreditAmount = 0
                },
                new JournalEntryLine
                {
                    AccountId = salesAccount!.Id,
                    Description = "Sales revenue",
                    DebitAmount = 0,
                    CreditAmount = receipt.TotalAmount
                }
            ]
        };

        return await CreateJournalEntryAsync(entry, cancellationToken);
    }

    public async Task<JournalEntry> PostPaymentJournalAsync(int paymentId, CancellationToken cancellationToken = default)
    {
        var payment = await _context.Payments
            .Include(p => p.PaymentMethod)
            .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken)
            ?? throw new InvalidOperationException("Payment not found.");

        var cashAccount = await GetAccountByCodeAsync("1000", cancellationToken);
        var arAccount = await GetAccountByCodeAsync("1100", cancellationToken);

        var entry = new JournalEntry
        {
            EntryNumber = await GenerateEntryNumberAsync(cancellationToken),
            EntryDate = DateTime.Today,
            Description = $"Payment received - {payment.PaymentMethod?.Name}",
            ReferenceType = "Payment",
            ReferenceId = paymentId,
            Status = JournalEntryStatus.Posted,
            JournalEntryLines =
            [
                new JournalEntryLine
                {
                    AccountId = cashAccount!.Id,
                    Description = $"Payment via {payment.PaymentMethod?.Name}",
                    DebitAmount = payment.Amount,
                    CreditAmount = 0
                },
                new JournalEntryLine
                {
                    AccountId = arAccount!.Id,
                    Description = "AR reduction",
                    DebitAmount = 0,
                    CreditAmount = payment.Amount
                }
            ]
        };

        return await CreateJournalEntryAsync(entry, cancellationToken);
    }

    public async Task<JournalEntry> PostPurchaseJournalAsync(int invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _context.SupplierInvoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken)
            ?? throw new InvalidOperationException("Invoice not found.");

        var inventoryAccount = await GetAccountByCodeAsync("1200", cancellationToken);
        var apAccount = await GetAccountByCodeAsync("2000", cancellationToken);

        var entry = new JournalEntry
        {
            EntryNumber = await GenerateEntryNumberAsync(cancellationToken),
            EntryDate = DateTime.Today,
            Description = $"Purchase - Invoice #{invoice.InvoiceNumber}",
            ReferenceType = "SupplierInvoice",
            ReferenceId = invoiceId,
            Status = JournalEntryStatus.Posted,
            JournalEntryLines =
            [
                new JournalEntryLine
                {
                    AccountId = inventoryAccount!.Id,
                    Description = "Inventory purchase",
                    DebitAmount = invoice.TotalAmount,
                    CreditAmount = 0
                },
                new JournalEntryLine
                {
                    AccountId = apAccount!.Id,
                    Description = "Accounts payable",
                    DebitAmount = 0,
                    CreditAmount = invoice.TotalAmount
                }
            ]
        };

        return await CreateJournalEntryAsync(entry, cancellationToken);
    }

    public async Task<JournalEntry> PostPayrollJournalAsync(int payrollPeriodId, CancellationToken cancellationToken = default)
    {
        var payslips = await _context.Payslips
            .Include(p => p.PayslipDetails)
                .ThenInclude(pd => pd.SalaryComponent)
            .Where(p => p.PayrollPeriodId == payrollPeriodId)
            .ToListAsync(cancellationToken);

        var salaryAccount = await GetAccountByCodeAsync("6100", cancellationToken);
        var cashAccount = await GetAccountByCodeAsync("1000", cancellationToken);
        var payeAccount = await GetAccountByCodeAsync("2300", cancellationToken);
        var nhifAccount = await GetAccountByCodeAsync("2310", cancellationToken);
        var nssfAccount = await GetAccountByCodeAsync("2320", cancellationToken);
        var housingLevyAccount = await GetAccountByCodeAsync("2330", cancellationToken);

        var totalGross = payslips.Sum(p => p.TotalEarnings);
        var totalNet = payslips.Sum(p => p.NetPay);
        var totalPaye = payslips.SelectMany(p => p.PayslipDetails).Where(d => d.SalaryComponent.Name == "PAYE").Sum(d => d.Amount);
        var totalNhif = payslips.SelectMany(p => p.PayslipDetails).Where(d => d.SalaryComponent.Name == "NHIF").Sum(d => d.Amount);
        var totalNssf = payslips.SelectMany(p => p.PayslipDetails).Where(d => d.SalaryComponent.Name == "NSSF").Sum(d => d.Amount);
        var totalHousingLevy = payslips.SelectMany(p => p.PayslipDetails).Where(d => d.SalaryComponent.Name == "Housing Levy").Sum(d => d.Amount);

        var entry = new JournalEntry
        {
            EntryNumber = await GenerateEntryNumberAsync(cancellationToken),
            EntryDate = DateTime.Today,
            Description = $"Payroll - Period #{payrollPeriodId}",
            ReferenceType = "PayrollPeriod",
            ReferenceId = payrollPeriodId,
            Status = JournalEntryStatus.Posted,
            JournalEntryLines =
            [
                new JournalEntryLine { AccountId = salaryAccount!.Id, Description = "Salaries expense", DebitAmount = totalGross, CreditAmount = 0 },
                new JournalEntryLine { AccountId = cashAccount!.Id, Description = "Net pay", DebitAmount = 0, CreditAmount = totalNet },
                new JournalEntryLine { AccountId = payeAccount!.Id, Description = "PAYE deduction", DebitAmount = 0, CreditAmount = totalPaye },
                new JournalEntryLine { AccountId = nhifAccount!.Id, Description = "NHIF deduction", DebitAmount = 0, CreditAmount = totalNhif },
                new JournalEntryLine { AccountId = nssfAccount!.Id, Description = "NSSF deduction", DebitAmount = 0, CreditAmount = totalNssf },
                new JournalEntryLine { AccountId = housingLevyAccount!.Id, Description = "Housing Levy", DebitAmount = 0, CreditAmount = totalHousingLevy }
            ]
        };

        return await CreateJournalEntryAsync(entry, cancellationToken);
    }

    public async Task<JournalEntry> PostExpenseJournalAsync(int expenseId, CancellationToken cancellationToken = default)
    {
        var expense = await _context.Expenses
            .Include(e => e.ExpenseCategory)
            .FirstOrDefaultAsync(e => e.Id == expenseId, cancellationToken)
            ?? throw new InvalidOperationException("Expense not found.");

        var expenseAccount = await GetAccountByCodeAsync("6900", cancellationToken); // Default to misc expense
        var cashAccount = await GetAccountByCodeAsync("1000", cancellationToken);

        var entry = new JournalEntry
        {
            EntryNumber = await GenerateEntryNumberAsync(cancellationToken),
            EntryDate = DateTime.Today,
            Description = $"Expense - {expense.Description}",
            ReferenceType = "Expense",
            ReferenceId = expenseId,
            Status = JournalEntryStatus.Posted,
            JournalEntryLines =
            [
                new JournalEntryLine
                {
                    AccountId = expenseAccount!.Id,
                    Description = expense.Description,
                    DebitAmount = expense.Amount,
                    CreditAmount = 0
                },
                new JournalEntryLine
                {
                    AccountId = cashAccount!.Id,
                    Description = "Cash payment",
                    DebitAmount = 0,
                    CreditAmount = expense.Amount
                }
            ]
        };

        return await CreateJournalEntryAsync(entry, cancellationToken);
    }

    #endregion

    #region Ledger & Reports

    public async Task<AccountLedger> GetAccountLedgerAsync(int accountId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var account = await GetAccountByIdAsync(accountId, cancellationToken)
            ?? throw new InvalidOperationException("Account not found.");

        var entries = await _context.JournalEntryLines
            .Include(l => l.JournalEntry)
            .Where(l => l.AccountId == accountId &&
                       l.JournalEntry.EntryDate >= startDate &&
                       l.JournalEntry.EntryDate <= endDate &&
                       l.JournalEntry.Status == JournalEntryStatus.Posted)
            .OrderBy(l => l.JournalEntry.EntryDate)
            .ThenBy(l => l.JournalEntry.EntryNumber)
            .ToListAsync(cancellationToken);

        // Calculate opening balance
        var openingBalance = await CalculateAccountBalanceAsync(accountId, startDate.AddDays(-1), cancellationToken);

        var ledger = new AccountLedger
        {
            AccountId = accountId,
            AccountCode = account.AccountCode,
            AccountName = account.AccountName,
            AccountType = account.AccountType,
            OpeningBalance = openingBalance
        };

        var runningBalance = openingBalance;
        foreach (var entry in entries)
        {
            var debit = entry.DebitAmount;
            var credit = entry.CreditAmount;

            // Calculate running balance based on account type
            if (account.AccountType == AccountType.Asset || account.AccountType == AccountType.Expense)
            {
                runningBalance += debit - credit;
            }
            else
            {
                runningBalance += credit - debit;
            }

            ledger.Lines.Add(new LedgerLine
            {
                Date = entry.JournalEntry.EntryDate,
                EntryNumber = entry.JournalEntry.EntryNumber,
                Description = entry.Description ?? entry.JournalEntry.Description ?? "",
                Debit = debit,
                Credit = credit,
                Balance = runningBalance
            });
        }

        ledger.ClosingBalance = runningBalance;
        return ledger;
    }

    public async Task<TrialBalance> GetTrialBalanceAsync(DateTime asOfDate, CancellationToken cancellationToken = default)
    {
        var accounts = await GetAllAccountsAsync(cancellationToken);
        var trialBalance = new TrialBalance { AsOfDate = asOfDate };

        foreach (var account in accounts.Where(a => a.IsActive))
        {
            var balance = await CalculateAccountBalanceAsync(account.Id, asOfDate, cancellationToken);
            if (balance == 0) continue;

            var line = new TrialBalanceLine
            {
                AccountCode = account.AccountCode,
                AccountName = account.AccountName,
                AccountType = account.AccountType
            };

            if (account.AccountType == AccountType.Asset || account.AccountType == AccountType.Expense)
            {
                if (balance >= 0) line.Debit = balance;
                else line.Credit = Math.Abs(balance);
            }
            else
            {
                if (balance >= 0) line.Credit = balance;
                else line.Debit = Math.Abs(balance);
            }

            trialBalance.Lines.Add(line);
        }

        trialBalance.TotalDebits = trialBalance.Lines.Sum(l => l.Debit);
        trialBalance.TotalCredits = trialBalance.Lines.Sum(l => l.Credit);

        return trialBalance;
    }

    public async Task<IncomeStatement> GetIncomeStatementAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var statement = new IncomeStatement { StartDate = startDate, EndDate = endDate };

        var revenueAccounts = await GetAccountsByTypeAsync(AccountType.Revenue, cancellationToken);
        var expenseAccounts = await GetAccountsByTypeAsync(AccountType.Expense, cancellationToken);

        // Revenue section
        var revenueSection = new IncomeStatementSection { SectionName = "Revenue" };
        foreach (var account in revenueAccounts.Where(a => a.IsActive))
        {
            var balance = await CalculateAccountBalanceForPeriodAsync(account.Id, startDate, endDate, cancellationToken);
            if (balance != 0)
            {
                revenueSection.Lines.Add(new IncomeStatementLine
                {
                    AccountCode = account.AccountCode,
                    AccountName = account.AccountName,
                    Amount = balance
                });
            }
        }
        revenueSection.Total = revenueSection.Lines.Sum(l => l.Amount);
        statement.RevenueSections.Add(revenueSection);
        statement.TotalRevenue = revenueSection.Total;

        // Expense section
        var expenseSection = new IncomeStatementSection { SectionName = "Expenses" };
        foreach (var account in expenseAccounts.Where(a => a.IsActive))
        {
            var balance = await CalculateAccountBalanceForPeriodAsync(account.Id, startDate, endDate, cancellationToken);
            if (balance != 0)
            {
                expenseSection.Lines.Add(new IncomeStatementLine
                {
                    AccountCode = account.AccountCode,
                    AccountName = account.AccountName,
                    Amount = balance
                });
            }
        }
        expenseSection.Total = expenseSection.Lines.Sum(l => l.Amount);
        statement.ExpenseSections.Add(expenseSection);
        statement.TotalExpenses = expenseSection.Total;

        statement.GrossProfit = statement.TotalRevenue;
        statement.NetIncome = statement.TotalRevenue - statement.TotalExpenses;

        return statement;
    }

    public async Task<BalanceSheet> GetBalanceSheetAsync(DateTime asOfDate, CancellationToken cancellationToken = default)
    {
        var balanceSheet = new BalanceSheet { AsOfDate = asOfDate };

        var assetAccounts = await GetAccountsByTypeAsync(AccountType.Asset, cancellationToken);
        var liabilityAccounts = await GetAccountsByTypeAsync(AccountType.Liability, cancellationToken);
        var equityAccounts = await GetAccountsByTypeAsync(AccountType.Equity, cancellationToken);

        // Assets
        var assetSection = new BalanceSheetSection { SectionName = "Assets" };
        foreach (var account in assetAccounts.Where(a => a.IsActive))
        {
            var balance = await CalculateAccountBalanceAsync(account.Id, asOfDate, cancellationToken);
            if (balance != 0)
            {
                assetSection.Lines.Add(new BalanceSheetLine
                {
                    AccountCode = account.AccountCode,
                    AccountName = account.AccountName,
                    Amount = balance
                });
            }
        }
        assetSection.Total = assetSection.Lines.Sum(l => l.Amount);
        balanceSheet.AssetSections.Add(assetSection);
        balanceSheet.TotalAssets = assetSection.Total;

        // Liabilities
        var liabilitySection = new BalanceSheetSection { SectionName = "Liabilities" };
        foreach (var account in liabilityAccounts.Where(a => a.IsActive))
        {
            var balance = await CalculateAccountBalanceAsync(account.Id, asOfDate, cancellationToken);
            if (balance != 0)
            {
                liabilitySection.Lines.Add(new BalanceSheetLine
                {
                    AccountCode = account.AccountCode,
                    AccountName = account.AccountName,
                    Amount = balance
                });
            }
        }
        liabilitySection.Total = liabilitySection.Lines.Sum(l => l.Amount);
        balanceSheet.LiabilitySections.Add(liabilitySection);
        balanceSheet.TotalLiabilities = liabilitySection.Total;

        // Equity
        var equitySection = new BalanceSheetSection { SectionName = "Equity" };
        foreach (var account in equityAccounts.Where(a => a.IsActive))
        {
            var balance = await CalculateAccountBalanceAsync(account.Id, asOfDate, cancellationToken);
            if (balance != 0)
            {
                equitySection.Lines.Add(new BalanceSheetLine
                {
                    AccountCode = account.AccountCode,
                    AccountName = account.AccountName,
                    Amount = balance
                });
            }
        }

        // Calculate retained earnings from income statement
        var yearStart = new DateTime(asOfDate.Year, 1, 1);
        var incomeStatement = await GetIncomeStatementAsync(yearStart, asOfDate, cancellationToken);
        balanceSheet.RetainedEarnings = incomeStatement.NetIncome;

        equitySection.Lines.Add(new BalanceSheetLine
        {
            AccountCode = "RE",
            AccountName = "Retained Earnings (YTD)",
            Amount = balanceSheet.RetainedEarnings
        });

        equitySection.Total = equitySection.Lines.Sum(l => l.Amount);
        balanceSheet.EquitySections.Add(equitySection);
        balanceSheet.TotalEquity = equitySection.Total;

        return balanceSheet;
    }

    private async Task<decimal> CalculateAccountBalanceAsync(int accountId, DateTime asOfDate, CancellationToken cancellationToken)
    {
        var entries = await _context.JournalEntryLines
            .Include(l => l.JournalEntry)
            .Where(l => l.AccountId == accountId &&
                       l.JournalEntry.EntryDate <= asOfDate &&
                       l.JournalEntry.Status == JournalEntryStatus.Posted)
            .ToListAsync(cancellationToken);

        var totalDebits = entries.Sum(e => e.DebitAmount);
        var totalCredits = entries.Sum(e => e.CreditAmount);

        var account = await GetAccountByIdAsync(accountId, cancellationToken);
        if (account == null) return 0;

        // For assets and expenses, debit increases; for liabilities, equity, revenue, credit increases
        if (account.AccountType == AccountType.Asset || account.AccountType == AccountType.Expense)
        {
            return totalDebits - totalCredits;
        }
        else
        {
            return totalCredits - totalDebits;
        }
    }

    private async Task<decimal> CalculateAccountBalanceForPeriodAsync(int accountId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        var entries = await _context.JournalEntryLines
            .Include(l => l.JournalEntry)
            .Where(l => l.AccountId == accountId &&
                       l.JournalEntry.EntryDate >= startDate &&
                       l.JournalEntry.EntryDate <= endDate &&
                       l.JournalEntry.Status == JournalEntryStatus.Posted)
            .ToListAsync(cancellationToken);

        var totalDebits = entries.Sum(e => e.DebitAmount);
        var totalCredits = entries.Sum(e => e.CreditAmount);

        var account = await GetAccountByIdAsync(accountId, cancellationToken);
        if (account == null) return 0;

        if (account.AccountType == AccountType.Asset || account.AccountType == AccountType.Expense)
        {
            return totalDebits - totalCredits;
        }
        else
        {
            return totalCredits - totalDebits;
        }
    }

    #endregion

    #region Report Generation

    public async Task<string> GenerateTrialBalanceHtmlAsync(DateTime asOfDate, CancellationToken cancellationToken = default)
    {
        var trialBalance = await GetTrialBalanceAsync(asOfDate, cancellationToken);

        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html><html><head><style>");
        html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
        html.AppendLine(".header { text-align: center; margin-bottom: 30px; }");
        html.AppendLine("table { width: 100%; border-collapse: collapse; }");
        html.AppendLine("th, td { padding: 10px; border: 1px solid #ddd; }");
        html.AppendLine("th { background: #2d2d44; color: white; }");
        html.AppendLine(".amount { text-align: right; }");
        html.AppendLine(".total { font-weight: bold; background: #f5f5f5; }");
        html.AppendLine("</style></head><body>");

        html.AppendLine("<div class='header'>");
        html.AppendLine("<h1>TRIAL BALANCE</h1>");
        html.AppendLine($"<p>As of {asOfDate:dd MMMM yyyy}</p>");
        html.AppendLine("</div>");

        html.AppendLine("<table>");
        html.AppendLine("<tr><th>Account Code</th><th>Account Name</th><th>Debit (KSh)</th><th>Credit (KSh)</th></tr>");

        foreach (var line in trialBalance.Lines.OrderBy(l => l.AccountCode))
        {
            html.AppendLine($"<tr>");
            html.AppendLine($"<td>{line.AccountCode}</td>");
            html.AppendLine($"<td>{line.AccountName}</td>");
            html.AppendLine($"<td class='amount'>{(line.Debit > 0 ? line.Debit.ToString("N2") : "")}</td>");
            html.AppendLine($"<td class='amount'>{(line.Credit > 0 ? line.Credit.ToString("N2") : "")}</td>");
            html.AppendLine("</tr>");
        }

        html.AppendLine($"<tr class='total'>");
        html.AppendLine($"<td colspan='2'>TOTAL</td>");
        html.AppendLine($"<td class='amount'>{trialBalance.TotalDebits:N2}</td>");
        html.AppendLine($"<td class='amount'>{trialBalance.TotalCredits:N2}</td>");
        html.AppendLine("</tr>");

        html.AppendLine("</table>");
        html.AppendLine($"<p>Status: {(trialBalance.IsBalanced ? "BALANCED" : "UNBALANCED")}</p>");
        html.AppendLine("</body></html>");

        return html.ToString();
    }

    public async Task<string> GenerateIncomeStatementHtmlAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var statement = await GetIncomeStatementAsync(startDate, endDate, cancellationToken);

        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html><html><head><style>");
        html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
        html.AppendLine(".header { text-align: center; margin-bottom: 30px; }");
        html.AppendLine(".section { margin-bottom: 20px; }");
        html.AppendLine(".section-title { font-weight: bold; border-bottom: 2px solid #333; padding: 5px 0; }");
        html.AppendLine(".line { display: flex; justify-content: space-between; padding: 5px 20px; }");
        html.AppendLine(".total { font-weight: bold; border-top: 1px solid #333; }");
        html.AppendLine(".net-income { font-size: 1.2em; font-weight: bold; margin-top: 20px; padding: 10px; background: #f5f5f5; }");
        html.AppendLine("</style></head><body>");

        html.AppendLine("<div class='header'>");
        html.AppendLine("<h1>INCOME STATEMENT</h1>");
        html.AppendLine($"<p>For the Period {startDate:dd/MM/yyyy} to {endDate:dd/MM/yyyy}</p>");
        html.AppendLine("</div>");

        foreach (var section in statement.RevenueSections)
        {
            html.AppendLine("<div class='section'>");
            html.AppendLine($"<div class='section-title'>{section.SectionName}</div>");
            foreach (var line in section.Lines)
            {
                html.AppendLine($"<div class='line'><span>{line.AccountName}</span><span>KSh {line.Amount:N2}</span></div>");
            }
            html.AppendLine($"<div class='line total'><span>Total {section.SectionName}</span><span>KSh {section.Total:N2}</span></div>");
            html.AppendLine("</div>");
        }

        foreach (var section in statement.ExpenseSections)
        {
            html.AppendLine("<div class='section'>");
            html.AppendLine($"<div class='section-title'>{section.SectionName}</div>");
            foreach (var line in section.Lines)
            {
                html.AppendLine($"<div class='line'><span>{line.AccountName}</span><span>KSh {line.Amount:N2}</span></div>");
            }
            html.AppendLine($"<div class='line total'><span>Total {section.SectionName}</span><span>KSh {section.Total:N2}</span></div>");
            html.AppendLine("</div>");
        }

        html.AppendLine($"<div class='net-income'>NET INCOME: KSh {statement.NetIncome:N2}</div>");
        html.AppendLine("</body></html>");

        return html.ToString();
    }

    public async Task<string> GenerateBalanceSheetHtmlAsync(DateTime asOfDate, CancellationToken cancellationToken = default)
    {
        var balanceSheet = await GetBalanceSheetAsync(asOfDate, cancellationToken);

        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html><html><head><style>");
        html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
        html.AppendLine(".header { text-align: center; margin-bottom: 30px; }");
        html.AppendLine(".section { margin-bottom: 20px; }");
        html.AppendLine(".section-title { font-weight: bold; border-bottom: 2px solid #333; padding: 5px 0; }");
        html.AppendLine(".line { display: flex; justify-content: space-between; padding: 5px 20px; }");
        html.AppendLine(".total { font-weight: bold; border-top: 1px solid #333; }");
        html.AppendLine(".grand-total { font-size: 1.2em; font-weight: bold; margin-top: 20px; padding: 10px; background: #f5f5f5; }");
        html.AppendLine("</style></head><body>");

        html.AppendLine("<div class='header'>");
        html.AppendLine("<h1>BALANCE SHEET</h1>");
        html.AppendLine($"<p>As of {asOfDate:dd MMMM yyyy}</p>");
        html.AppendLine("</div>");

        foreach (var section in balanceSheet.AssetSections)
        {
            html.AppendLine("<div class='section'>");
            html.AppendLine($"<div class='section-title'>{section.SectionName}</div>");
            foreach (var line in section.Lines)
            {
                html.AppendLine($"<div class='line'><span>{line.AccountName}</span><span>KSh {line.Amount:N2}</span></div>");
            }
            html.AppendLine($"<div class='line total'><span>Total {section.SectionName}</span><span>KSh {section.Total:N2}</span></div>");
            html.AppendLine("</div>");
        }

        foreach (var section in balanceSheet.LiabilitySections)
        {
            html.AppendLine("<div class='section'>");
            html.AppendLine($"<div class='section-title'>{section.SectionName}</div>");
            foreach (var line in section.Lines)
            {
                html.AppendLine($"<div class='line'><span>{line.AccountName}</span><span>KSh {line.Amount:N2}</span></div>");
            }
            html.AppendLine($"<div class='line total'><span>Total {section.SectionName}</span><span>KSh {section.Total:N2}</span></div>");
            html.AppendLine("</div>");
        }

        foreach (var section in balanceSheet.EquitySections)
        {
            html.AppendLine("<div class='section'>");
            html.AppendLine($"<div class='section-title'>{section.SectionName}</div>");
            foreach (var line in section.Lines)
            {
                html.AppendLine($"<div class='line'><span>{line.AccountName}</span><span>KSh {line.Amount:N2}</span></div>");
            }
            html.AppendLine($"<div class='line total'><span>Total {section.SectionName}</span><span>KSh {section.Total:N2}</span></div>");
            html.AppendLine("</div>");
        }

        html.AppendLine($"<div class='grand-total'>TOTAL ASSETS: KSh {balanceSheet.TotalAssets:N2}</div>");
        html.AppendLine($"<div class='grand-total'>TOTAL LIABILITIES + EQUITY: KSh {(balanceSheet.TotalLiabilities + balanceSheet.TotalEquity):N2}</div>");
        html.AppendLine($"<p>Status: {(balanceSheet.IsBalanced ? "BALANCED" : "UNBALANCED")}</p>");

        html.AppendLine("</body></html>");

        return html.ToString();
    }

    public async Task<string> GenerateGeneralLedgerHtmlAsync(DateTime startDate, DateTime endDate, int? accountId = null, CancellationToken cancellationToken = default)
    {
        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html><html><head><style>");
        html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
        html.AppendLine(".header { text-align: center; margin-bottom: 30px; }");
        html.AppendLine(".account-header { background: #2d2d44; color: white; padding: 10px; margin-top: 20px; }");
        html.AppendLine("table { width: 100%; border-collapse: collapse; margin-bottom: 20px; }");
        html.AppendLine("th, td { padding: 8px; border: 1px solid #ddd; }");
        html.AppendLine("th { background: #f5f5f5; }");
        html.AppendLine(".amount { text-align: right; }");
        html.AppendLine(".total-row { font-weight: bold; background: #f0f0f0; }");
        html.AppendLine("</style></head><body>");

        html.AppendLine("<div class='header'>");
        html.AppendLine("<h1>GENERAL LEDGER</h1>");
        html.AppendLine($"<p>Period: {startDate:dd/MM/yyyy} to {endDate:dd/MM/yyyy}</p>");
        html.AppendLine("</div>");

        var accounts = accountId.HasValue
            ? new List<ChartOfAccount> { (await GetAccountByIdAsync(accountId.Value, cancellationToken))! }
            : (await GetAllAccountsAsync(cancellationToken)).Where(a => a.IsActive).ToList();

        foreach (var account in accounts.OrderBy(a => a.AccountCode))
        {
            var ledger = await GetAccountLedgerAsync(account.Id, startDate, endDate, cancellationToken);
            if (ledger.Lines.Count == 0 && ledger.OpeningBalance == 0) continue;

            html.AppendLine($"<div class='account-header'>{account.AccountCode} - {account.AccountName}</div>");
            html.AppendLine("<table>");
            html.AppendLine("<tr><th>Date</th><th>Entry #</th><th>Description</th><th>Debit</th><th>Credit</th><th>Balance</th></tr>");
            html.AppendLine($"<tr><td colspan='5'>Opening Balance</td><td class='amount'>{ledger.OpeningBalance:N2}</td></tr>");

            foreach (var line in ledger.Lines)
            {
                html.AppendLine("<tr>");
                html.AppendLine($"<td>{line.Date:dd/MM/yyyy}</td>");
                html.AppendLine($"<td>{line.EntryNumber}</td>");
                html.AppendLine($"<td>{line.Description}</td>");
                html.AppendLine($"<td class='amount'>{(line.Debit > 0 ? line.Debit.ToString("N2") : "")}</td>");
                html.AppendLine($"<td class='amount'>{(line.Credit > 0 ? line.Credit.ToString("N2") : "")}</td>");
                html.AppendLine($"<td class='amount'>{line.Balance:N2}</td>");
                html.AppendLine("</tr>");
            }

            html.AppendLine($"<tr class='total-row'><td colspan='5'>Closing Balance</td><td class='amount'>{ledger.ClosingBalance:N2}</td></tr>");
            html.AppendLine("</table>");
        }

        html.AppendLine("</body></html>");
        return html.ToString();
    }

    public async Task<string> GenerateCashFlowStatementHtmlAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var cashFlow = await GetCashFlowStatementAsync(startDate, endDate, cancellationToken);

        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html><html><head><style>");
        html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
        html.AppendLine(".header { text-align: center; margin-bottom: 30px; }");
        html.AppendLine(".section { margin-bottom: 20px; }");
        html.AppendLine(".section-title { font-weight: bold; border-bottom: 2px solid #333; padding: 5px 0; }");
        html.AppendLine(".line { display: flex; justify-content: space-between; padding: 5px 20px; }");
        html.AppendLine(".total { font-weight: bold; border-top: 1px solid #333; }");
        html.AppendLine(".grand-total { font-size: 1.2em; font-weight: bold; margin-top: 20px; padding: 10px; background: #f5f5f5; }");
        html.AppendLine("</style></head><body>");

        html.AppendLine("<div class='header'>");
        html.AppendLine("<h1>CASH FLOW STATEMENT</h1>");
        html.AppendLine($"<p>Period: {startDate:dd/MM/yyyy} to {endDate:dd/MM/yyyy}</p>");
        html.AppendLine("</div>");

        html.AppendLine("<div class='section'>");
        html.AppendLine("<div class='section-title'>Operating Activities</div>");
        html.AppendLine($"<div class='line'><span>Net Income</span><span>KSh {cashFlow.NetIncome:N2}</span></div>");
        foreach (var item in cashFlow.OperatingAdjustments)
        {
            html.AppendLine($"<div class='line'><span>{item.Description}</span><span>KSh {item.Amount:N2}</span></div>");
        }
        html.AppendLine($"<div class='line total'><span>Net Cash from Operating Activities</span><span>KSh {cashFlow.NetCashFromOperating:N2}</span></div>");
        html.AppendLine("</div>");

        html.AppendLine("<div class='section'>");
        html.AppendLine("<div class='section-title'>Investing Activities</div>");
        foreach (var item in cashFlow.InvestingActivities)
        {
            html.AppendLine($"<div class='line'><span>{item.Description}</span><span>KSh {item.Amount:N2}</span></div>");
        }
        html.AppendLine($"<div class='line total'><span>Net Cash from Investing Activities</span><span>KSh {cashFlow.NetCashFromInvesting:N2}</span></div>");
        html.AppendLine("</div>");

        html.AppendLine("<div class='section'>");
        html.AppendLine("<div class='section-title'>Financing Activities</div>");
        foreach (var item in cashFlow.FinancingActivities)
        {
            html.AppendLine($"<div class='line'><span>{item.Description}</span><span>KSh {item.Amount:N2}</span></div>");
        }
        html.AppendLine($"<div class='line total'><span>Net Cash from Financing Activities</span><span>KSh {cashFlow.NetCashFromFinancing:N2}</span></div>");
        html.AppendLine("</div>");

        html.AppendLine($"<div class='grand-total'>Net Change in Cash: KSh {cashFlow.NetChangeInCash:N2}</div>");
        html.AppendLine($"<div class='grand-total'>Beginning Cash Balance: KSh {cashFlow.BeginningCashBalance:N2}</div>");
        html.AppendLine($"<div class='grand-total'>Ending Cash Balance: KSh {cashFlow.EndingCashBalance:N2}</div>");

        html.AppendLine("</body></html>");
        return html.ToString();
    }

    public async Task<CashFlowStatement> GetCashFlowStatementAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var cashFlow = new CashFlowStatement { StartDate = startDate, EndDate = endDate };

        // Get income statement for net income
        var incomeStatement = await GetIncomeStatementAsync(startDate, endDate, cancellationToken);
        cashFlow.NetIncome = incomeStatement.NetIncome;

        // Get cash accounts
        var cashAccounts = await _context.ChartOfAccounts
            .Where(a => a.AccountSubType == AccountSubType.Cash || a.AccountSubType == AccountSubType.BankAccount)
            .ToListAsync(cancellationToken);

        decimal beginningCash = 0;
        decimal endingCash = 0;
        foreach (var account in cashAccounts)
        {
            beginningCash += await CalculateAccountBalanceAsync(account.Id, startDate.AddDays(-1), cancellationToken);
            endingCash += await CalculateAccountBalanceAsync(account.Id, endDate, cancellationToken);
        }

        cashFlow.BeginningCashBalance = beginningCash;
        cashFlow.EndingCashBalance = endingCash;
        cashFlow.NetChangeInCash = endingCash - beginningCash;

        // Simplified cash flow - for comprehensive implementation would need more detailed tracking
        cashFlow.NetCashFromOperating = cashFlow.NetIncome;
        cashFlow.NetCashFromInvesting = 0;
        cashFlow.NetCashFromFinancing = 0;

        return cashFlow;
    }

    public async Task<GeneralLedgerReport> GetGeneralLedgerAsync(DateTime startDate, DateTime endDate, AccountType? filterByType = null, CancellationToken cancellationToken = default)
    {
        var report = new GeneralLedgerReport { StartDate = startDate, EndDate = endDate };

        var accounts = filterByType.HasValue
            ? await GetAccountsByTypeAsync(filterByType.Value, cancellationToken)
            : await GetAllAccountsAsync(cancellationToken);

        foreach (var account in accounts.Where(a => a.IsActive).OrderBy(a => a.AccountCode))
        {
            var ledger = await GetAccountLedgerAsync(account.Id, startDate, endDate, cancellationToken);
            if (ledger.Lines.Count > 0 || ledger.OpeningBalance != 0)
            {
                report.Accounts.Add(ledger);
                report.TotalDebits += ledger.Lines.Sum(l => l.Debit);
                report.TotalCredits += ledger.Lines.Sum(l => l.Credit);
            }
        }

        return report;
    }

    public async Task<ComparativeIncomeStatement> GetComparativeIncomeStatementAsync(DateTime currentPeriodStart, DateTime currentPeriodEnd, DateTime comparePeriodStart, DateTime comparePeriodEnd, CancellationToken cancellationToken = default)
    {
        var current = await GetIncomeStatementAsync(currentPeriodStart, currentPeriodEnd, cancellationToken);
        var prior = await GetIncomeStatementAsync(comparePeriodStart, comparePeriodEnd, cancellationToken);

        return new ComparativeIncomeStatement
        {
            CurrentPeriod = current,
            PriorPeriod = prior,
            RevenueChange = current.TotalRevenue - prior.TotalRevenue,
            RevenueChangePercent = prior.TotalRevenue != 0 ? ((current.TotalRevenue - prior.TotalRevenue) / prior.TotalRevenue) * 100 : 0,
            NetIncomeChange = current.NetIncome - prior.NetIncome,
            NetIncomeChangePercent = prior.NetIncome != 0 ? ((current.NetIncome - prior.NetIncome) / prior.NetIncome) * 100 : 0
        };
    }

    #endregion

    #region GL Account Mapping

    public async Task<IReadOnlyList<GLAccountMapping>> GetAllMappingsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.GLAccountMappings
            .Include(m => m.DebitAccount)
            .Include(m => m.CreditAccount)
            .Include(m => m.Category)
            .Where(m => m.IsActive)
            .OrderBy(m => m.SourceType)
            .ThenByDescending(m => m.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task<GLAccountMapping?> GetMappingAsync(TransactionSourceType sourceType, int? categoryId = null, PaymentMethodType? paymentMethod = null, int? storeId = null, CancellationToken cancellationToken = default)
    {
        // Find the most specific mapping (higher priority)
        return await _context.GLAccountMappings
            .Include(m => m.DebitAccount)
            .Include(m => m.CreditAccount)
            .Where(m => m.SourceType == sourceType &&
                       m.IsActive &&
                       (m.CategoryId == null || m.CategoryId == categoryId) &&
                       (m.PaymentMethod == null || m.PaymentMethod == paymentMethod) &&
                       (m.StoreId == null || m.StoreId == storeId))
            .OrderByDescending(m => m.Priority)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<GLAccountMapping> SaveMappingAsync(GLAccountMapping mapping, int? userId = null, CancellationToken cancellationToken = default)
    {
        if (mapping.Id == 0)
        {
            if (userId.HasValue) mapping.CreatedByUserId = userId.Value;
            _context.GLAccountMappings.Add(mapping);
        }
        else
        {
            if (userId.HasValue) mapping.UpdatedAt = DateTime.UtcNow;
            _context.GLAccountMappings.Update(mapping);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return mapping;
    }

    public async Task<bool> DeleteMappingAsync(int mappingId, CancellationToken cancellationToken = default)
    {
        var mapping = await _context.GLAccountMappings.FindAsync([mappingId], cancellationToken);
        if (mapping == null) return false;

        mapping.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task SeedDefaultMappingsAsync(CancellationToken cancellationToken = default)
    {
        if (await _context.GLAccountMappings.AnyAsync(cancellationToken)) return;

        var cashAccount = await GetAccountByCodeAsync("1000", cancellationToken);
        var salesAccount = await GetAccountByCodeAsync("4000", cancellationToken);
        var inventoryAccount = await GetAccountByCodeAsync("1200", cancellationToken);
        var apAccount = await GetAccountByCodeAsync("2000", cancellationToken);
        var cogsAccount = await GetAccountByCodeAsync("5000", cancellationToken);
        var salaryAccount = await GetAccountByCodeAsync("6100", cancellationToken);

        if (cashAccount == null || salesAccount == null) return;

        var defaultMappings = new List<GLAccountMapping>
        {
            new() { SourceType = TransactionSourceType.Sale, DebitAccountId = cashAccount.Id, CreditAccountId = salesAccount.Id, Description = "Cash Sales", IsActive = true },
            new() { SourceType = TransactionSourceType.Purchase, DebitAccountId = inventoryAccount!.Id, CreditAccountId = apAccount!.Id, Description = "Inventory Purchases", IsActive = true },
            new() { SourceType = TransactionSourceType.PayrollPayment, DebitAccountId = salaryAccount!.Id, CreditAccountId = cashAccount.Id, Description = "Payroll Payments", IsActive = true }
        };

        _context.GLAccountMappings.AddRange(defaultMappings);
        await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion

    #region Bank Reconciliation

    public async Task<BankReconciliation> CreateBankReconciliationAsync(int bankAccountId, DateTime statementDate, decimal statementBalance, int userId, CancellationToken cancellationToken = default)
    {
        var lastRecon = await _context.BankReconciliations
            .Where(r => r.BankAccountId == bankAccountId)
            .OrderByDescending(r => r.StatementDate)
            .FirstOrDefaultAsync(cancellationToken);

        var beginningBalance = lastRecon?.EndingBookBalance ?? 0;

        var reconciliation = new BankReconciliation
        {
            ReconciliationNumber = $"RECON-{DateTime.Now:yyyyMMddHHmmss}",
            BankAccountId = bankAccountId,
            StatementDate = statementDate,
            StatementEndingBalance = statementBalance,
            BeginningBookBalance = beginningBalance,
            Status = ReconciliationStatus.InProgress,
            CreatedByUserId = userId
        };

        _context.BankReconciliations.Add(reconciliation);
        await _context.SaveChangesAsync(cancellationToken);
        return reconciliation;
    }

    public async Task<BankReconciliation?> GetBankReconciliationByIdAsync(int reconciliationId, CancellationToken cancellationToken = default)
    {
        return await _context.BankReconciliations
            .Include(r => r.BankAccount)
            .Include(r => r.Items)
                .ThenInclude(i => i.JournalEntryLine)
            .FirstOrDefaultAsync(r => r.Id == reconciliationId, cancellationToken);
    }

    public async Task<IReadOnlyList<BankReconciliation>> GetBankReconciliationsAsync(int bankAccountId, CancellationToken cancellationToken = default)
    {
        return await _context.BankReconciliations
            .Include(r => r.BankAccount)
            .Where(r => r.BankAccountId == bankAccountId)
            .OrderByDescending(r => r.StatementDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<JournalEntryLine>> GetUnreconciledTransactionsAsync(int bankAccountId, CancellationToken cancellationToken = default)
    {
        return await _context.JournalEntryLines
            .Include(l => l.JournalEntry)
            .Where(l => l.AccountId == bankAccountId && !l.IsReconciled && l.JournalEntry.Status == JournalEntryStatus.Posted)
            .OrderBy(l => l.JournalEntry.EntryDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<BankReconciliation> MarkItemsClearedAsync(int reconciliationId, int[] journalEntryLineIds, CancellationToken cancellationToken = default)
    {
        var reconciliation = await GetBankReconciliationByIdAsync(reconciliationId, cancellationToken)
            ?? throw new InvalidOperationException("Bank reconciliation not found.");

        foreach (var lineId in journalEntryLineIds)
        {
            var line = await _context.JournalEntryLines.FindAsync([lineId], cancellationToken);
            if (line != null)
            {
                line.IsReconciled = true;
                line.ReconciledDate = DateTime.UtcNow;
                line.BankReconciliationId = reconciliationId;

                var item = new BankReconciliationItem
                {
                    BankReconciliationId = reconciliationId,
                    JournalEntryLineId = lineId,
                    TransactionDate = line.JournalEntry?.EntryDate ?? DateTime.Today,
                    Amount = line.DebitAmount - line.CreditAmount,
                    Description = line.Description,
                    IsCleared = true,
                    ClearedDate = DateTime.UtcNow
                };
                _context.BankReconciliationItems.Add(item);
            }
        }

        // Recalculate totals
        var clearedItems = reconciliation.Items.Where(i => i.IsCleared).ToList();
        reconciliation.ClearedDeposits = clearedItems.Where(i => i.Amount > 0).Sum(i => i.Amount);
        reconciliation.ClearedWithdrawals = clearedItems.Where(i => i.Amount < 0).Sum(i => Math.Abs(i.Amount));
        reconciliation.EndingBookBalance = reconciliation.BeginningBookBalance + reconciliation.ClearedDeposits - reconciliation.ClearedWithdrawals;
        reconciliation.Difference = reconciliation.StatementEndingBalance - reconciliation.EndingBookBalance;

        await _context.SaveChangesAsync(cancellationToken);
        return reconciliation;
    }

    public async Task<BankReconciliationItem> AddBankAdjustmentAsync(int reconciliationId, BankTransactionType type, decimal amount, string description, int? glAccountId = null, CancellationToken cancellationToken = default)
    {
        var item = new BankReconciliationItem
        {
            BankReconciliationId = reconciliationId,
            TransactionDate = DateTime.Today,
            TransactionType = type,
            Amount = amount,
            Description = description,
            IsBankAdjustment = true,
            IsCleared = true,
            ClearedDate = DateTime.UtcNow
        };

        _context.BankReconciliationItems.Add(item);
        await _context.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task<BankReconciliation> CompleteBankReconciliationAsync(int reconciliationId, int userId, CancellationToken cancellationToken = default)
    {
        var reconciliation = await GetBankReconciliationByIdAsync(reconciliationId, cancellationToken)
            ?? throw new InvalidOperationException("Bank reconciliation not found.");

        if (Math.Abs(reconciliation.Difference) > 0.01m)
        {
            throw new InvalidOperationException($"Cannot complete reconciliation. Difference of {reconciliation.Difference:N2} exists.");
        }

        reconciliation.Status = ReconciliationStatus.Completed;
        reconciliation.CompletedByUserId = userId;
        reconciliation.CompletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return reconciliation;
    }

    public async Task<BankReconciliation> VoidBankReconciliationAsync(int reconciliationId, int userId, string reason, CancellationToken cancellationToken = default)
    {
        var reconciliation = await GetBankReconciliationByIdAsync(reconciliationId, cancellationToken)
            ?? throw new InvalidOperationException("Bank reconciliation not found.");

        // Unreconcile all items
        foreach (var item in reconciliation.Items)
        {
            if (item.JournalEntryLineId.HasValue)
            {
                var line = await _context.JournalEntryLines.FindAsync([item.JournalEntryLineId.Value], cancellationToken);
                if (line != null)
                {
                    line.IsReconciled = false;
                    line.ReconciledDate = null;
                    line.BankReconciliationId = null;
                }
            }
        }

        reconciliation.Status = ReconciliationStatus.Voided;
        reconciliation.Notes = (reconciliation.Notes ?? "") + $"\nVoided: {reason}";

        await _context.SaveChangesAsync(cancellationToken);
        return reconciliation;
    }

    #endregion

    #region Budget

    public async Task<IReadOnlyList<AccountBudget>> GetBudgetsAsync(int fiscalYear, CancellationToken cancellationToken = default)
    {
        return await _context.AccountBudgets
            .Include(b => b.Account)
            .Where(b => b.FiscalYear == fiscalYear)
            .OrderBy(b => b.Account.AccountCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<AccountBudget?> GetAccountBudgetAsync(int accountId, int fiscalYear, CancellationToken cancellationToken = default)
    {
        return await _context.AccountBudgets
            .Include(b => b.Account)
            .FirstOrDefaultAsync(b => b.AccountId == accountId && b.FiscalYear == fiscalYear, cancellationToken);
    }

    public async Task<AccountBudget> SaveBudgetAsync(AccountBudget budget, int? userId = null, CancellationToken cancellationToken = default)
    {
        if (budget.Id == 0)
        {
            if (userId.HasValue) budget.CreatedByUserId = userId.Value;
            _context.AccountBudgets.Add(budget);
        }
        else
        {
            if (userId.HasValue) budget.UpdatedAt = DateTime.UtcNow;
            _context.AccountBudgets.Update(budget);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return budget;
    }

    public async Task<AccountBudget> ApproveBudgetAsync(int budgetId, int userId, CancellationToken cancellationToken = default)
    {
        var budget = await _context.AccountBudgets.FindAsync([budgetId], cancellationToken)
            ?? throw new InvalidOperationException("Budget not found.");

        budget.IsApproved = true;
        budget.ApprovedByUserId = userId;
        budget.ApprovedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return budget;
    }

    public async Task<BudgetVsActualReport> GetBudgetVsActualAsync(int fiscalYear, int? month = null, CancellationToken cancellationToken = default)
    {
        var report = new BudgetVsActualReport { FiscalYear = fiscalYear, Month = month };

        var budgets = await GetBudgetsAsync(fiscalYear, cancellationToken);
        var periods = await GetPeriodsByFiscalYearAsync(fiscalYear, cancellationToken);

        DateTime startDate, endDate;
        if (month.HasValue)
        {
            var period = periods.FirstOrDefault(p => p.PeriodNumber == month.Value);
            if (period == null) return report;
            startDate = period.StartDate;
            endDate = period.EndDate;
        }
        else
        {
            startDate = periods.First().StartDate;
            endDate = periods.Last().EndDate;
        }

        foreach (var budget in budgets)
        {
            var monthlyAmounts = budget.GetMonthlyAmounts();
            var budgetAmount = month.HasValue ? monthlyAmounts[month.Value - 1] : budget.AnnualBudget;

            var actualAmount = await CalculateAccountBalanceForPeriodAsync(budget.AccountId, startDate, endDate, cancellationToken);

            var line = new BudgetVsActualLine
            {
                AccountId = budget.AccountId,
                AccountCode = budget.Account.AccountCode,
                AccountName = budget.Account.AccountName,
                BudgetAmount = budgetAmount,
                ActualAmount = actualAmount,
                Variance = budgetAmount - actualAmount
            };

            // For expenses, under budget is favorable; for revenue, over budget is favorable
            line.IsFavorable = budget.Account.AccountType == AccountType.Expense
                ? line.Variance > 0
                : line.Variance < 0;

            report.Lines.Add(line);
            report.TotalBudget += budgetAmount;
            report.TotalActual += actualAmount;
        }

        report.TotalVariance = report.TotalBudget - report.TotalActual;
        return report;
    }

    #endregion

    #region Audit Logging

    public async Task LogAuditAsync(string action, string entityType, int entityId, string? oldValue, string? newValue, int userId, string? context = null, CancellationToken cancellationToken = default)
    {
        var auditLog = new AccountingAuditLog
        {
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValue = oldValue,
            NewValue = newValue,
            UserId = userId,
            Context = context,
            CreatedAt = DateTime.UtcNow
        };

        _context.AccountingAuditLogs.Add(auditLog);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AccountingAuditLog>> GetAuditLogsAsync(string entityType, int entityId, CancellationToken cancellationToken = default)
    {
        return await _context.AccountingAuditLogs
            .Include(a => a.User)
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    #endregion
}
