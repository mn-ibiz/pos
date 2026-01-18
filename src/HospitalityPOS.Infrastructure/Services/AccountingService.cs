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

    public async Task<ChartOfAccount> CreateAccountAsync(ChartOfAccount account, CancellationToken cancellationToken = default)
    {
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

    public async Task<ChartOfAccount> UpdateAccountAsync(ChartOfAccount account, CancellationToken cancellationToken = default)
    {
        _context.ChartOfAccounts.Update(account);
        await _context.SaveChangesAsync(cancellationToken);
        return account;
    }

    public async Task<bool> DeleteAccountAsync(int id, CancellationToken cancellationToken = default)
    {
        var account = await _context.ChartOfAccounts.FindAsync([id], cancellationToken);
        if (account == null || account.IsSystemAccount) return false;

        // Check if account has transactions
        var hasTransactions = await _context.JournalEntryLines.AnyAsync(l => l.AccountId == id, cancellationToken);
        if (hasTransactions) return false;

        _context.ChartOfAccounts.Remove(account);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
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

    public async Task<AccountingPeriod> CreatePeriodAsync(string name, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var period = new AccountingPeriod
        {
            PeriodName = name,
            StartDate = startDate,
            EndDate = endDate,
            Status = AccountingPeriodStatus.Open
        };

        _context.AccountingPeriods.Add(period);
        await _context.SaveChangesAsync(cancellationToken);
        return period;
    }

    public async Task<AccountingPeriod?> GetCurrentPeriodAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        return await _context.AccountingPeriods
            .FirstOrDefaultAsync(p => p.StartDate <= today && p.EndDate >= today && p.Status == AccountingPeriodStatus.Open, cancellationToken);
    }

    public async Task<IReadOnlyList<AccountingPeriod>> GetAllPeriodsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AccountingPeriods
            .OrderByDescending(p => p.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<AccountingPeriod> ClosePeriodAsync(int periodId, int closedByUserId, CancellationToken cancellationToken = default)
    {
        var period = await _context.AccountingPeriods.FindAsync([periodId], cancellationToken)
            ?? throw new InvalidOperationException($"Accounting period with ID {periodId} not found.");

        period.Status = AccountingPeriodStatus.Closed;
        period.ClosedByUserId = closedByUserId;
        period.ClosedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return period;
    }

    #endregion

    #region Journal Entries

    public async Task<JournalEntry> CreateJournalEntryAsync(JournalEntry entry, CancellationToken cancellationToken = default)
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

        _context.JournalEntries.Add(entry);
        await _context.SaveChangesAsync(cancellationToken);
        return entry;
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

    #endregion
}
