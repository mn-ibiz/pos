using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for accounting operations.
/// </summary>
public interface IAccountingService
{
    // Chart of Accounts
    Task<ChartOfAccount> CreateAccountAsync(ChartOfAccount account, CancellationToken cancellationToken = default);
    Task<ChartOfAccount?> GetAccountByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ChartOfAccount?> GetAccountByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChartOfAccount>> GetAllAccountsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChartOfAccount>> GetAccountsByTypeAsync(AccountType accountType, CancellationToken cancellationToken = default);
    Task<ChartOfAccount> UpdateAccountAsync(ChartOfAccount account, CancellationToken cancellationToken = default);
    Task<bool> DeleteAccountAsync(int id, CancellationToken cancellationToken = default);
    Task SeedDefaultAccountsAsync(CancellationToken cancellationToken = default);

    // Accounting Periods
    Task<AccountingPeriod> CreatePeriodAsync(string name, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<AccountingPeriod?> GetCurrentPeriodAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccountingPeriod>> GetAllPeriodsAsync(CancellationToken cancellationToken = default);
    Task<AccountingPeriod> ClosePeriodAsync(int periodId, int closedByUserId, CancellationToken cancellationToken = default);

    // Journal Entries
    Task<JournalEntry> CreateJournalEntryAsync(JournalEntry entry, CancellationToken cancellationToken = default);
    Task<JournalEntry?> GetJournalEntryByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JournalEntry>> GetJournalEntriesByPeriodAsync(int periodId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JournalEntry>> GetJournalEntriesByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JournalEntry>> GetJournalEntriesByAccountAsync(int accountId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<JournalEntry> PostJournalEntryAsync(int entryId, CancellationToken cancellationToken = default);
    Task<JournalEntry> ReverseJournalEntryAsync(int entryId, string reason, CancellationToken cancellationToken = default);
    Task<string> GenerateEntryNumberAsync(CancellationToken cancellationToken = default);

    // Automatic Journal Posting
    Task<JournalEntry> PostSalesJournalAsync(int receiptId, CancellationToken cancellationToken = default);
    Task<JournalEntry> PostPaymentJournalAsync(int paymentId, CancellationToken cancellationToken = default);
    Task<JournalEntry> PostPurchaseJournalAsync(int invoiceId, CancellationToken cancellationToken = default);
    Task<JournalEntry> PostPayrollJournalAsync(int payrollPeriodId, CancellationToken cancellationToken = default);
    Task<JournalEntry> PostExpenseJournalAsync(int expenseId, CancellationToken cancellationToken = default);

    // Ledger & Reports
    Task<AccountLedger> GetAccountLedgerAsync(int accountId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<TrialBalance> GetTrialBalanceAsync(DateTime asOfDate, CancellationToken cancellationToken = default);
    Task<IncomeStatement> GetIncomeStatementAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<BalanceSheet> GetBalanceSheetAsync(DateTime asOfDate, CancellationToken cancellationToken = default);

    // Report generation
    Task<string> GenerateTrialBalanceHtmlAsync(DateTime asOfDate, CancellationToken cancellationToken = default);
    Task<string> GenerateIncomeStatementHtmlAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<string> GenerateBalanceSheetHtmlAsync(DateTime asOfDate, CancellationToken cancellationToken = default);
}

/// <summary>
/// Account ledger showing transactions and running balance.
/// </summary>
public class AccountLedger
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public List<LedgerLine> Lines { get; set; } = [];
}

public class LedgerLine
{
    public DateTime Date { get; set; }
    public string EntryNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
}

/// <summary>
/// Trial balance report.
/// </summary>
public class TrialBalance
{
    public DateTime AsOfDate { get; set; }
    public List<TrialBalanceLine> Lines { get; set; } = [];
    public decimal TotalDebits { get; set; }
    public decimal TotalCredits { get; set; }
    public bool IsBalanced => Math.Abs(TotalDebits - TotalCredits) < 0.01m;
}

public class TrialBalanceLine
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}

/// <summary>
/// Income statement (Profit & Loss).
/// </summary>
public class IncomeStatement
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<IncomeStatementSection> RevenueSections { get; set; } = [];
    public List<IncomeStatementSection> ExpenseSections { get; set; } = [];
    public decimal TotalRevenue { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal NetIncome { get; set; }
}

public class IncomeStatementSection
{
    public string SectionName { get; set; } = string.Empty;
    public List<IncomeStatementLine> Lines { get; set; } = [];
    public decimal Total { get; set; }
}

public class IncomeStatementLine
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

/// <summary>
/// Balance sheet report.
/// </summary>
public class BalanceSheet
{
    public DateTime AsOfDate { get; set; }
    public List<BalanceSheetSection> AssetSections { get; set; } = [];
    public List<BalanceSheetSection> LiabilitySections { get; set; } = [];
    public List<BalanceSheetSection> EquitySections { get; set; } = [];
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal TotalEquity { get; set; }
    public decimal RetainedEarnings { get; set; }
    public bool IsBalanced => Math.Abs(TotalAssets - (TotalLiabilities + TotalEquity)) < 0.01m;
}

public class BalanceSheetSection
{
    public string SectionName { get; set; } = string.Empty;
    public List<BalanceSheetLine> Lines { get; set; } = [];
    public decimal Total { get; set; }
}

public class BalanceSheetLine
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
