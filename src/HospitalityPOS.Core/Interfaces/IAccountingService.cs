using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for accounting operations.
/// </summary>
public interface IAccountingService
{
    #region Chart of Accounts

    /// <summary>
    /// Creates a new account in the chart of accounts.
    /// </summary>
    Task<ChartOfAccount> CreateAccountAsync(ChartOfAccount account, int? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an account by ID.
    /// </summary>
    Task<ChartOfAccount?> GetAccountByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an account by code.
    /// </summary>
    Task<ChartOfAccount?> GetAccountByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all accounts.
    /// </summary>
    Task<IReadOnlyList<ChartOfAccount>> GetAllAccountsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets accounts by type.
    /// </summary>
    Task<IReadOnlyList<ChartOfAccount>> GetAccountsByTypeAsync(AccountType accountType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets accounts by sub-type.
    /// </summary>
    Task<IReadOnlyList<ChartOfAccount>> GetAccountsBySubTypeAsync(AccountSubType subType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets child accounts of a parent account.
    /// </summary>
    Task<IReadOnlyList<ChartOfAccount>> GetChildAccountsAsync(int parentAccountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets accounts in hierarchical structure.
    /// </summary>
    Task<IReadOnlyList<ChartOfAccount>> GetAccountHierarchyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an account.
    /// </summary>
    Task<ChartOfAccount> UpdateAccountAsync(ChartOfAccount account, int? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an account (soft delete/deactivate).
    /// </summary>
    Task<bool> DeleteAccountAsync(int id, int? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Seeds default chart of accounts.
    /// </summary>
    Task SeedDefaultAccountsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Recalculates the current balance for an account.
    /// </summary>
    Task<decimal> RecalculateAccountBalanceAsync(int accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recalculates all account balances.
    /// </summary>
    Task RecalculateAllBalancesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets account balance as of a specific date.
    /// </summary>
    Task<decimal> GetAccountBalanceAsOfAsync(int accountId, DateTime asOfDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the next available account code for a given account type.
    /// </summary>
    Task<string> GetNextAccountCodeAsync(AccountType accountType, int? parentAccountId = null, CancellationToken cancellationToken = default);

    #endregion

    #region Accounting Periods

    /// <summary>
    /// Creates a new accounting period.
    /// </summary>
    Task<AccountingPeriod> CreatePeriodAsync(string name, DateTime startDate, DateTime endDate, int? fiscalYear = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates all periods for a fiscal year.
    /// </summary>
    Task<IReadOnlyList<AccountingPeriod>> CreateFiscalYearPeriodsAsync(int year, int userId, DateTime? fiscalYearStart = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current open accounting period.
    /// </summary>
    Task<AccountingPeriod?> GetCurrentPeriodAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the period for a specific date.
    /// </summary>
    Task<AccountingPeriod?> GetPeriodForDateAsync(DateTime date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all accounting periods.
    /// </summary>
    Task<IReadOnlyList<AccountingPeriod>> GetAllPeriodsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets periods for a fiscal year.
    /// </summary>
    Task<IReadOnlyList<AccountingPeriod>> GetPeriodsByFiscalYearAsync(int fiscalYear, CancellationToken cancellationToken = default);

    /// <summary>
    /// Locks an accounting period.
    /// </summary>
    Task<AccountingPeriod> LockPeriodAsync(int periodId, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unlocks an accounting period.
    /// </summary>
    Task<AccountingPeriod> UnlockPeriodAsync(int periodId, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes an accounting period.
    /// </summary>
    Task<AccountingPeriod> ClosePeriodAsync(int periodId, int closedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reopens a closed period.
    /// </summary>
    Task<AccountingPeriod> ReopenPeriodAsync(int periodId, int userId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs year-end closing.
    /// </summary>
    Task<PeriodClose> PerformYearEndCloseAsync(int fiscalYear, int userId, int retainedEarningsAccountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets period close history.
    /// </summary>
    Task<IReadOnlyList<PeriodClose>> GetPeriodCloseHistoryAsync(int periodId, CancellationToken cancellationToken = default);

    #endregion

    #region Journal Entries

    /// <summary>
    /// Creates a journal entry.
    /// </summary>
    Task<JournalEntry> CreateJournalEntryAsync(JournalEntry entry, int? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a journal entry by ID.
    /// </summary>
    Task<JournalEntry?> GetJournalEntryByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a journal entry by entry number.
    /// </summary>
    Task<JournalEntry?> GetJournalEntryByNumberAsync(string entryNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets journal entries by period.
    /// </summary>
    Task<IReadOnlyList<JournalEntry>> GetJournalEntriesByPeriodAsync(int periodId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets journal entries by date range.
    /// </summary>
    Task<IReadOnlyList<JournalEntry>> GetJournalEntriesByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets journal entries by account.
    /// </summary>
    Task<IReadOnlyList<JournalEntry>> GetJournalEntriesByAccountAsync(int accountId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets journal entries by source reference.
    /// </summary>
    Task<IReadOnlyList<JournalEntry>> GetJournalEntriesByReferenceAsync(string referenceType, int referenceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Posts a draft journal entry.
    /// </summary>
    Task<JournalEntry> PostJournalEntryAsync(int entryId, int? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reverses a posted journal entry.
    /// </summary>
    Task<JournalEntry> ReverseJournalEntryAsync(int entryId, string reason, int? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates the next journal entry number.
    /// </summary>
    Task<string> GenerateEntryNumberAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a journal entry.
    /// </summary>
    Task<JournalEntryValidationResult> ValidateJournalEntryAsync(JournalEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves a journal entry requiring approval.
    /// </summary>
    Task<JournalEntry> ApproveJournalEntryAsync(int entryId, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects a journal entry requiring approval.
    /// </summary>
    Task<JournalEntry> RejectJournalEntryAsync(int entryId, int userId, string reason, CancellationToken cancellationToken = default);

    #endregion

    #region Automatic Journal Posting

    /// <summary>
    /// Creates journal entry for a sale/receipt.
    /// </summary>
    Task<JournalEntry> PostSalesJournalAsync(int receiptId, int? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates journal entry for a payment received.
    /// </summary>
    Task<JournalEntry> PostPaymentJournalAsync(int paymentId, int? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates journal entry for a purchase/goods received.
    /// </summary>
    Task<JournalEntry> PostPurchaseJournalAsync(int purchaseOrderId, int? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates journal entry for a supplier payment.
    /// </summary>
    Task<JournalEntry> PostSupplierPaymentJournalAsync(int invoiceId, int? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates journal entry for payroll.
    /// </summary>
    Task<JournalEntry> PostPayrollJournalAsync(int payrollPeriodId, int? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates journal entry for an expense.
    /// </summary>
    Task<JournalEntry> PostExpenseJournalAsync(int expenseId, int? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates journal entry for inventory adjustment.
    /// </summary>
    Task<JournalEntry> PostInventoryAdjustmentJournalAsync(int adjustmentId, int? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates journal entry for depreciation.
    /// </summary>
    Task<JournalEntry> PostDepreciationJournalAsync(int assetId, decimal amount, int? userId = null, CancellationToken cancellationToken = default);

    #endregion

    #region GL Account Mapping

    /// <summary>
    /// Gets all GL account mappings.
    /// </summary>
    Task<IReadOnlyList<GLAccountMapping>> GetAllMappingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets mapping for a transaction type.
    /// </summary>
    Task<GLAccountMapping?> GetMappingAsync(TransactionSourceType sourceType, int? categoryId = null, PaymentMethodType? paymentMethod = null, int? storeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a GL mapping.
    /// </summary>
    Task<GLAccountMapping> SaveMappingAsync(GLAccountMapping mapping, int? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a GL mapping.
    /// </summary>
    Task<bool> DeleteMappingAsync(int mappingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Seeds default GL mappings.
    /// </summary>
    Task SeedDefaultMappingsAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Ledger & Reports

    /// <summary>
    /// Gets the general ledger for an account.
    /// </summary>
    Task<AccountLedger> GetAccountLedgerAsync(int accountId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the general ledger for all accounts.
    /// </summary>
    Task<GeneralLedgerReport> GetGeneralLedgerAsync(DateTime startDate, DateTime endDate, AccountType? filterByType = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a trial balance report.
    /// </summary>
    Task<TrialBalance> GetTrialBalanceAsync(DateTime asOfDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an income statement (P&L).
    /// </summary>
    Task<IncomeStatement> GetIncomeStatementAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a comparative income statement.
    /// </summary>
    Task<ComparativeIncomeStatement> GetComparativeIncomeStatementAsync(DateTime currentPeriodStart, DateTime currentPeriodEnd, DateTime comparePeriodStart, DateTime comparePeriodEnd, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a balance sheet report.
    /// </summary>
    Task<BalanceSheet> GetBalanceSheetAsync(DateTime asOfDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a cash flow statement.
    /// </summary>
    Task<CashFlowStatement> GetCashFlowStatementAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    #endregion

    #region Report Generation (HTML)

    /// <summary>
    /// Generates trial balance HTML.
    /// </summary>
    Task<string> GenerateTrialBalanceHtmlAsync(DateTime asOfDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates income statement HTML.
    /// </summary>
    Task<string> GenerateIncomeStatementHtmlAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates balance sheet HTML.
    /// </summary>
    Task<string> GenerateBalanceSheetHtmlAsync(DateTime asOfDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates general ledger HTML.
    /// </summary>
    Task<string> GenerateGeneralLedgerHtmlAsync(DateTime startDate, DateTime endDate, int? accountId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates cash flow statement HTML.
    /// </summary>
    Task<string> GenerateCashFlowStatementHtmlAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    #endregion

    #region Budget

    /// <summary>
    /// Gets budgets for a fiscal year.
    /// </summary>
    Task<IReadOnlyList<AccountBudget>> GetBudgetsAsync(int fiscalYear, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets budget for an account.
    /// </summary>
    Task<AccountBudget?> GetAccountBudgetAsync(int accountId, int fiscalYear, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a budget.
    /// </summary>
    Task<AccountBudget> SaveBudgetAsync(AccountBudget budget, int? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves a budget.
    /// </summary>
    Task<AccountBudget> ApproveBudgetAsync(int budgetId, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets budget vs actual report.
    /// </summary>
    Task<BudgetVsActualReport> GetBudgetVsActualAsync(int fiscalYear, int? month = null, CancellationToken cancellationToken = default);

    #endregion

    #region Bank Reconciliation

    /// <summary>
    /// Creates a new bank reconciliation.
    /// </summary>
    Task<BankReconciliation> CreateBankReconciliationAsync(int bankAccountId, DateTime statementDate, decimal statementBalance, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a bank reconciliation by ID.
    /// </summary>
    Task<BankReconciliation?> GetBankReconciliationByIdAsync(int reconciliationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets bank reconciliations for an account.
    /// </summary>
    Task<IReadOnlyList<BankReconciliation>> GetBankReconciliationsAsync(int bankAccountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unreconciled transactions for a bank account.
    /// </summary>
    Task<IReadOnlyList<JournalEntryLine>> GetUnreconciledTransactionsAsync(int bankAccountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks items as cleared in a reconciliation.
    /// </summary>
    Task<BankReconciliation> MarkItemsClearedAsync(int reconciliationId, int[] journalEntryLineIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a bank adjustment to a reconciliation.
    /// </summary>
    Task<BankReconciliationItem> AddBankAdjustmentAsync(int reconciliationId, BankTransactionType type, decimal amount, string description, int? glAccountId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes a bank reconciliation.
    /// </summary>
    Task<BankReconciliation> CompleteBankReconciliationAsync(int reconciliationId, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Voids a bank reconciliation.
    /// </summary>
    Task<BankReconciliation> VoidBankReconciliationAsync(int reconciliationId, int userId, string reason, CancellationToken cancellationToken = default);

    #endregion

    #region Audit

    /// <summary>
    /// Logs an accounting audit entry.
    /// </summary>
    Task LogAuditAsync(string action, string entityType, int entityId, string? oldValue, string? newValue, int userId, string? context = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for an entity.
    /// </summary>
    Task<IReadOnlyList<AccountingAuditLog>> GetAuditLogsAsync(string entityType, int entityId, CancellationToken cancellationToken = default);

    #endregion
}

#region Report Models

/// <summary>
/// Account ledger showing transactions and running balance.
/// </summary>
public class AccountLedger
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public decimal TotalDebits { get; set; }
    public decimal TotalCredits { get; set; }
    public List<LedgerLine> Lines { get; set; } = [];
}

public class LedgerLine
{
    public DateTime Date { get; set; }
    public string EntryNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
}

/// <summary>
/// General ledger report for all accounts.
/// </summary>
public class GeneralLedgerReport
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public List<AccountLedger> Accounts { get; set; } = [];
    public decimal TotalDebits { get; set; }
    public decimal TotalCredits { get; set; }
}

/// <summary>
/// Trial balance report.
/// </summary>
public class TrialBalance
{
    public DateTime AsOfDate { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public List<TrialBalanceLine> Lines { get; set; } = [];
    public decimal TotalDebits { get; set; }
    public decimal TotalCredits { get; set; }
    public bool IsBalanced => Math.Abs(TotalDebits - TotalCredits) < 0.01m;
}

public class TrialBalanceLine
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}

/// <summary>
/// Income statement (Profit and Loss).
/// </summary>
public class IncomeStatement
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public List<IncomeStatementSection> RevenueSections { get; set; } = [];
    public List<IncomeStatementSection> CostOfGoodsSoldSections { get; set; } = [];
    public List<IncomeStatementSection> OperatingExpenseSections { get; set; } = [];
    public List<IncomeStatementSection> OtherIncomeSections { get; set; } = [];
    public List<IncomeStatementSection> OtherExpenseSections { get; set; } = [];
    public decimal TotalRevenue { get; set; }
    public decimal TotalCostOfGoodsSold { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal TotalOperatingExpenses { get; set; }
    public decimal OperatingIncome { get; set; }
    public decimal TotalOtherIncome { get; set; }
    public decimal TotalOtherExpenses { get; set; }
    public decimal IncomeBeforeTax { get; set; }
    public decimal TaxExpense { get; set; }
    public decimal NetIncome { get; set; }
    public decimal GrossProfitMargin => TotalRevenue != 0 ? (GrossProfit / TotalRevenue) * 100 : 0;
    public decimal NetProfitMargin => TotalRevenue != 0 ? (NetIncome / TotalRevenue) * 100 : 0;
}

public class IncomeStatementSection
{
    public string SectionName { get; set; } = string.Empty;
    public List<IncomeStatementLine> Lines { get; set; } = [];
    public decimal Total { get; set; }
}

public class IncomeStatementLine
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

/// <summary>
/// Comparative income statement.
/// </summary>
public class ComparativeIncomeStatement
{
    public IncomeStatement CurrentPeriod { get; set; } = new();
    public IncomeStatement PriorPeriod { get; set; } = new();
    public decimal RevenueChange { get; set; }
    public decimal RevenueChangePercent { get; set; }
    public decimal NetIncomeChange { get; set; }
    public decimal NetIncomeChangePercent { get; set; }
}

/// <summary>
/// Balance sheet report.
/// </summary>
public class BalanceSheet
{
    public DateTime AsOfDate { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public List<BalanceSheetSection> CurrentAssetSections { get; set; } = [];
    public List<BalanceSheetSection> NonCurrentAssetSections { get; set; } = [];
    public List<BalanceSheetSection> CurrentLiabilitySections { get; set; } = [];
    public List<BalanceSheetSection> NonCurrentLiabilitySections { get; set; } = [];
    public List<BalanceSheetSection> EquitySections { get; set; } = [];
    public decimal TotalCurrentAssets { get; set; }
    public decimal TotalNonCurrentAssets { get; set; }
    public decimal TotalAssets { get; set; }
    public decimal TotalCurrentLiabilities { get; set; }
    public decimal TotalNonCurrentLiabilities { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal TotalEquity { get; set; }
    public decimal RetainedEarnings { get; set; }
    public decimal TotalLiabilitiesAndEquity { get; set; }
    public bool IsBalanced => Math.Abs(TotalAssets - TotalLiabilitiesAndEquity) < 0.01m;
    public decimal CurrentRatio => TotalCurrentLiabilities != 0 ? TotalCurrentAssets / TotalCurrentLiabilities : 0;
    public decimal DebtToEquityRatio => TotalEquity != 0 ? TotalLiabilities / TotalEquity : 0;
}

public class BalanceSheetSection
{
    public string SectionName { get; set; } = string.Empty;
    public List<BalanceSheetLine> Lines { get; set; } = [];
    public decimal Total { get; set; }
}

public class BalanceSheetLine
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

/// <summary>
/// Cash flow statement.
/// </summary>
public class CashFlowStatement
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    // Operating Activities
    public decimal NetIncome { get; set; }
    public List<CashFlowLine> OperatingAdjustments { get; set; } = [];
    public decimal NetCashFromOperating { get; set; }

    // Investing Activities
    public List<CashFlowLine> InvestingActivities { get; set; } = [];
    public decimal NetCashFromInvesting { get; set; }

    // Financing Activities
    public List<CashFlowLine> FinancingActivities { get; set; } = [];
    public decimal NetCashFromFinancing { get; set; }

    // Summary
    public decimal NetChangeInCash { get; set; }
    public decimal BeginningCashBalance { get; set; }
    public decimal EndingCashBalance { get; set; }
}

public class CashFlowLine
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

/// <summary>
/// Journal entry validation result.
/// </summary>
public class JournalEntryValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public bool IsBalanced { get; set; }
    public bool PeriodIsOpen { get; set; }
    public bool AccountsAreValid { get; set; }
}

/// <summary>
/// Budget vs actual report.
/// </summary>
public class BudgetVsActualReport
{
    public int FiscalYear { get; set; }
    public int? Month { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public List<BudgetVsActualLine> Lines { get; set; } = [];
    public decimal TotalBudget { get; set; }
    public decimal TotalActual { get; set; }
    public decimal TotalVariance { get; set; }
    public decimal VariancePercentage => TotalBudget != 0 ? (TotalVariance / TotalBudget) * 100 : 0;
}

public class BudgetVsActualLine
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal BudgetAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal Variance { get; set; }
    public decimal VariancePercentage => BudgetAmount != 0 ? (Variance / BudgetAmount) * 100 : 0;
    public bool IsFavorable { get; set; }
}

#endregion
