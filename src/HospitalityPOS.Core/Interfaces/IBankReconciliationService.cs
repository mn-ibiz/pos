using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for bank account management and reconciliation.
/// </summary>
public interface IBankReconciliationService
{
    #region Bank Account Management

    /// <summary>
    /// Creates a new bank account.
    /// </summary>
    Task<BankAccount> CreateBankAccountAsync(BankAccount account, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a bank account by ID.
    /// </summary>
    Task<BankAccount?> GetBankAccountByIdAsync(int accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all bank accounts.
    /// </summary>
    Task<IEnumerable<BankAccount>> GetAllBankAccountsAsync(BankAccountStatus? status = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a bank account.
    /// </summary>
    Task<BankAccount> UpdateBankAccountAsync(BankAccount account, CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes a bank account.
    /// </summary>
    Task CloseBankAccountAsync(int accountId, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Links a bank account to M-Pesa short code.
    /// </summary>
    Task LinkMpesaAccountAsync(int accountId, string mpesaShortCode, CancellationToken cancellationToken = default);

    #endregion

    #region Transaction Import

    /// <summary>
    /// Imports transactions from CSV file.
    /// </summary>
    Task<BankStatementImport> ImportFromCsvAsync(
        int bankAccountId,
        Stream fileStream,
        string fileName,
        CsvImportOptions options,
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports transactions from Excel file.
    /// </summary>
    Task<BankStatementImport> ImportFromExcelAsync(
        int bankAccountId,
        Stream fileStream,
        string fileName,
        ExcelImportOptions options,
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports transactions from OFX file.
    /// </summary>
    Task<BankStatementImport> ImportFromOfxAsync(
        int bankAccountId,
        Stream fileStream,
        string fileName,
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets import history for a bank account.
    /// </summary>
    Task<IEnumerable<BankStatementImport>> GetImportHistoryAsync(
        int bankAccountId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets bank transactions for an account.
    /// </summary>
    Task<IEnumerable<BankTransaction>> GetBankTransactionsAsync(
        int bankAccountId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        ReconciliationMatchStatus? matchStatus = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Manually adds a bank transaction.
    /// </summary>
    Task<BankTransaction> AddBankTransactionAsync(BankTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an unmatched bank transaction.
    /// </summary>
    Task DeleteBankTransactionAsync(int transactionId, CancellationToken cancellationToken = default);

    #endregion

    #region Reconciliation Session

    /// <summary>
    /// Starts a new reconciliation session.
    /// </summary>
    Task<ReconciliationSession> StartReconciliationSessionAsync(
        int bankAccountId,
        DateTime periodStartDate,
        DateTime periodEndDate,
        decimal statementBalance,
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a reconciliation session by ID.
    /// </summary>
    Task<ReconciliationSession?> GetReconciliationSessionAsync(int sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets reconciliation sessions for a bank account.
    /// </summary>
    Task<IEnumerable<ReconciliationSession>> GetReconciliationSessionsAsync(
        int bankAccountId,
        ReconciliationSessionStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current active reconciliation session for an account.
    /// </summary>
    Task<ReconciliationSession?> GetActiveReconciliationSessionAsync(int bankAccountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes/approves a reconciliation session.
    /// </summary>
    Task CompleteReconciliationSessionAsync(int sessionId, int userId, string? notes = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects a reconciliation session.
    /// </summary>
    Task RejectReconciliationSessionAsync(int sessionId, int userId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates reconciliation session balances.
    /// </summary>
    Task UpdateSessionBalancesAsync(int sessionId, CancellationToken cancellationToken = default);

    #endregion

    #region Matching

    /// <summary>
    /// Runs automatic matching for a session.
    /// </summary>
    Task<AutoMatchResult> RunAutoMatchAsync(int sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets suggested matches for a bank transaction.
    /// </summary>
    Task<IEnumerable<MatchSuggestion>> GetMatchSuggestionsAsync(int bankTransactionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Manually matches a bank transaction to a POS payment.
    /// </summary>
    Task<ReconciliationMatch> CreateManualMatchAsync(
        int sessionId,
        int bankTransactionId,
        int? paymentId,
        int? receiptId,
        int userId,
        string? notes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a match.
    /// </summary>
    Task UnmatchTransactionAsync(int matchId, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets matches for a session.
    /// </summary>
    Task<IEnumerable<ReconciliationMatch>> GetMatchesAsync(int sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Excludes a transaction from reconciliation.
    /// </summary>
    Task ExcludeTransactionAsync(int bankTransactionId, string reason, int userId, CancellationToken cancellationToken = default);

    #endregion

    #region Discrepancy Handling

    /// <summary>
    /// Creates a discrepancy record.
    /// </summary>
    Task<ReconciliationDiscrepancy> CreateDiscrepancyAsync(
        int sessionId,
        DiscrepancyType type,
        int? bankTransactionId,
        int? paymentId,
        decimal differenceAmount,
        string description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets discrepancies for a session.
    /// </summary>
    Task<IEnumerable<ReconciliationDiscrepancy>> GetDiscrepanciesAsync(
        int sessionId,
        DiscrepancyResolutionStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves a discrepancy.
    /// </summary>
    Task ResolveDiscrepancyAsync(
        int discrepancyId,
        string resolutionAction,
        int userId,
        int? adjustmentJournalEntryId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes off a discrepancy.
    /// </summary>
    Task WriteOffDiscrepancyAsync(
        int discrepancyId,
        int userId,
        int journalEntryId,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Escalates a discrepancy.
    /// </summary>
    Task EscalateDiscrepancyAsync(int discrepancyId, string reason, int userId, CancellationToken cancellationToken = default);

    #endregion

    #region Matching Rules

    /// <summary>
    /// Gets all matching rules.
    /// </summary>
    Task<IEnumerable<ReconciliationMatchingRule>> GetMatchingRulesAsync(int? bankAccountId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a matching rule.
    /// </summary>
    Task<ReconciliationMatchingRule> CreateMatchingRuleAsync(ReconciliationMatchingRule rule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a matching rule.
    /// </summary>
    Task<ReconciliationMatchingRule> UpdateMatchingRuleAsync(ReconciliationMatchingRule rule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a matching rule.
    /// </summary>
    Task DeleteMatchingRuleAsync(int ruleId, CancellationToken cancellationToken = default);

    #endregion

    #region Reports

    /// <summary>
    /// Generates reconciliation summary report.
    /// </summary>
    Task<ReconciliationSummaryReport> GetReconciliationSummaryAsync(int sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets outstanding items report.
    /// </summary>
    Task<OutstandingItemsReport> GetOutstandingItemsReportAsync(int bankAccountId, DateTime asOfDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets bank balance vs book balance comparison.
    /// </summary>
    Task<BalanceComparisonReport> GetBalanceComparisonAsync(int bankAccountId, DateTime asOfDate, CancellationToken cancellationToken = default);

    #endregion
}

#region DTOs

/// <summary>
/// Options for CSV import.
/// </summary>
public class CsvImportOptions
{
    public bool HasHeaderRow { get; set; } = true;
    public char Delimiter { get; set; } = ',';
    public int DateColumnIndex { get; set; }
    public string DateFormat { get; set; } = "dd/MM/yyyy";
    public int DescriptionColumnIndex { get; set; }
    public int? DepositColumnIndex { get; set; }
    public int? WithdrawalColumnIndex { get; set; }
    public int? AmountColumnIndex { get; set; }
    public int? BalanceColumnIndex { get; set; }
    public int? ReferenceColumnIndex { get; set; }
    public bool NegativeForWithdrawals { get; set; } = true;
}

/// <summary>
/// Options for Excel import.
/// </summary>
public class ExcelImportOptions
{
    public string? SheetName { get; set; }
    public int HeaderRow { get; set; } = 1;
    public int DataStartRow { get; set; } = 2;
    public string DateColumn { get; set; } = "A";
    public string DescriptionColumn { get; set; } = "B";
    public string? DepositColumn { get; set; }
    public string? WithdrawalColumn { get; set; }
    public string? AmountColumn { get; set; }
    public string? BalanceColumn { get; set; }
    public string? ReferenceColumn { get; set; }
    public string DateFormat { get; set; } = "dd/MM/yyyy";
}

/// <summary>
/// Result of automatic matching.
/// </summary>
public class AutoMatchResult
{
    public int SessionId { get; set; }
    public int TotalBankTransactions { get; set; }
    public int TotalPOSTransactions { get; set; }
    public int AutoMatchedCount { get; set; }
    public int UnmatchedBankCount { get; set; }
    public int UnmatchedPOSCount { get; set; }
    public decimal TotalMatchedAmount { get; set; }
    public decimal TotalUnmatchedBankAmount { get; set; }
    public decimal TotalUnmatchedPOSAmount { get; set; }
    public IList<ReconciliationMatch> Matches { get; set; } = new List<ReconciliationMatch>();
}

/// <summary>
/// Suggested match for a bank transaction.
/// </summary>
public class MatchSuggestion
{
    public int BankTransactionId { get; set; }
    public int? PaymentId { get; set; }
    public int? ReceiptId { get; set; }
    public string? Reference { get; set; }
    public DateTime TransactionDate { get; set; }
    public decimal BankAmount { get; set; }
    public decimal POSAmount { get; set; }
    public decimal AmountDifference { get; set; }
    public int ConfidenceScore { get; set; }
    public string MatchingRule { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Reconciliation summary report.
/// </summary>
public class ReconciliationSummaryReport
{
    public int SessionId { get; set; }
    public string SessionNumber { get; set; } = string.Empty;
    public DateTime PeriodStartDate { get; set; }
    public DateTime PeriodEndDate { get; set; }

    public decimal StatementOpeningBalance { get; set; }
    public decimal StatementClosingBalance { get; set; }
    public decimal BookOpeningBalance { get; set; }
    public decimal BookClosingBalance { get; set; }

    public int TotalBankTransactions { get; set; }
    public decimal TotalBankDeposits { get; set; }
    public decimal TotalBankWithdrawals { get; set; }

    public int TotalPOSTransactions { get; set; }
    public decimal TotalPOSDeposits { get; set; }
    public decimal TotalPOSWithdrawals { get; set; }

    public int MatchedCount { get; set; }
    public int UnmatchedBankCount { get; set; }
    public int UnmatchedPOSCount { get; set; }
    public int DiscrepancyCount { get; set; }

    public decimal ReconciledDifference { get; set; }
    public bool IsBalanced { get; set; }

    public IList<OutstandingItem> OutstandingDeposits { get; set; } = new List<OutstandingItem>();
    public IList<OutstandingItem> OutstandingWithdrawals { get; set; } = new List<OutstandingItem>();
}

/// <summary>
/// Outstanding items report.
/// </summary>
public class OutstandingItemsReport
{
    public int BankAccountId { get; set; }
    public DateTime AsOfDate { get; set; }

    public decimal TotalOutstandingDeposits { get; set; }
    public decimal TotalOutstandingWithdrawals { get; set; }
    public decimal NetOutstanding { get; set; }

    public IList<OutstandingItem> UnpresentedCheques { get; set; } = new List<OutstandingItem>();
    public IList<OutstandingItem> DepositsInTransit { get; set; } = new List<OutstandingItem>();
    public IList<OutstandingItem> UnmatchedBankItems { get; set; } = new List<OutstandingItem>();
    public IList<OutstandingItem> UnmatchedPOSItems { get; set; } = new List<OutstandingItem>();
}

/// <summary>
/// Outstanding item detail.
/// </summary>
public class OutstandingItem
{
    public int? BankTransactionId { get; set; }
    public int? PaymentId { get; set; }
    public int? ReceiptId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public int AgeDays { get; set; }
}

/// <summary>
/// Balance comparison report.
/// </summary>
public class BalanceComparisonReport
{
    public int BankAccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public DateTime AsOfDate { get; set; }

    public decimal BankStatementBalance { get; set; }
    public decimal BookBalance { get; set; }
    public decimal Difference { get; set; }

    public decimal AddDepositsInTransit { get; set; }
    public decimal LessUnpresentedCheques { get; set; }
    public decimal OtherAdjustments { get; set; }

    public decimal AdjustedBankBalance { get; set; }
    public decimal AdjustedBookBalance { get; set; }

    public bool IsReconciled { get; set; }
    public DateTime? LastReconciliationDate { get; set; }
}

#endregion
