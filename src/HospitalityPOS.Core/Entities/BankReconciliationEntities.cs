namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Status of a bank account.
/// </summary>
public enum BankAccountStatus
{
    /// <summary>Account is active.</summary>
    Active = 1,
    /// <summary>Account is inactive.</summary>
    Inactive = 2,
    /// <summary>Account is closed.</summary>
    Closed = 3
}

/// <summary>
/// Type of bank transaction.
/// </summary>
public enum BankTransactionType
{
    /// <summary>Deposit/credit to account.</summary>
    Deposit = 1,
    /// <summary>Withdrawal/debit from account.</summary>
    Withdrawal = 2,
    /// <summary>Bank transfer in.</summary>
    TransferIn = 3,
    /// <summary>Bank transfer out.</summary>
    TransferOut = 4,
    /// <summary>Bank fee/charge.</summary>
    Fee = 5,
    /// <summary>Interest earned.</summary>
    Interest = 6,
    /// <summary>Cheque payment.</summary>
    Cheque = 7,
    /// <summary>Direct debit.</summary>
    DirectDebit = 8,
    /// <summary>Standing order.</summary>
    StandingOrder = 9,
    /// <summary>M-Pesa transaction.</summary>
    MpesaTransaction = 10,
    /// <summary>Card payment.</summary>
    CardPayment = 11,
    /// <summary>Other transaction.</summary>
    Other = 99
}

/// <summary>
/// Status of reconciliation matching.
/// </summary>
public enum ReconciliationMatchStatus
{
    /// <summary>Not yet matched.</summary>
    Unmatched = 1,
    /// <summary>Auto-matched by system.</summary>
    AutoMatched = 2,
    /// <summary>Manually matched.</summary>
    ManuallyMatched = 3,
    /// <summary>Partially matched.</summary>
    PartiallyMatched = 4,
    /// <summary>Confirmed discrepancy.</summary>
    Discrepancy = 5,
    /// <summary>Excluded from reconciliation.</summary>
    Excluded = 6
}

/// <summary>
/// Status of a reconciliation session.
/// </summary>
public enum ReconciliationSessionStatus
{
    /// <summary>In progress.</summary>
    InProgress = 1,
    /// <summary>Pending review.</summary>
    PendingReview = 2,
    /// <summary>Approved/completed.</summary>
    Completed = 3,
    /// <summary>Rejected.</summary>
    Rejected = 4
}

/// <summary>
/// Type of discrepancy.
/// </summary>
public enum DiscrepancyType
{
    /// <summary>Transaction in POS but not in bank.</summary>
    MissingFromBank = 1,
    /// <summary>Transaction in bank but not in POS.</summary>
    MissingFromPOS = 2,
    /// <summary>Amount differs between bank and POS.</summary>
    AmountMismatch = 3,
    /// <summary>Date differs between bank and POS.</summary>
    DateMismatch = 4,
    /// <summary>Duplicate transaction.</summary>
    Duplicate = 5,
    /// <summary>Timing difference (cleared after cutoff).</summary>
    TimingDifference = 6,
    /// <summary>Unknown discrepancy.</summary>
    Other = 99
}

/// <summary>
/// Status of discrepancy resolution.
/// </summary>
public enum DiscrepancyResolutionStatus
{
    /// <summary>Open/unresolved.</summary>
    Open = 1,
    /// <summary>Under investigation.</summary>
    Investigating = 2,
    /// <summary>Resolved.</summary>
    Resolved = 3,
    /// <summary>Written off.</summary>
    WrittenOff = 4,
    /// <summary>Escalated.</summary>
    Escalated = 5
}

/// <summary>
/// Bank account configuration for reconciliation.
/// </summary>
public class BankAccount : BaseEntity
{
    /// <summary>
    /// Bank name.
    /// </summary>
    public string BankName { get; set; } = string.Empty;

    /// <summary>
    /// Bank branch.
    /// </summary>
    public string? Branch { get; set; }

    /// <summary>
    /// Account number.
    /// </summary>
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// Account name (as registered with bank).
    /// </summary>
    public string AccountName { get; set; } = string.Empty;

    /// <summary>
    /// Account type (Current, Savings, etc.).
    /// </summary>
    public string AccountType { get; set; } = "Current";

    /// <summary>
    /// Currency code (KES, USD, etc.).
    /// </summary>
    public string CurrencyCode { get; set; } = "KES";

    /// <summary>
    /// SWIFT/BIC code for international transfers.
    /// </summary>
    public string? SwiftCode { get; set; }

    /// <summary>
    /// M-Pesa paybill or till number if linked.
    /// </summary>
    public string? MpesaShortCode { get; set; }

    /// <summary>
    /// Account status.
    /// </summary>
    public BankAccountStatus Status { get; set; } = BankAccountStatus.Active;

    /// <summary>
    /// Opening balance when account was set up.
    /// </summary>
    public decimal OpeningBalance { get; set; }

    /// <summary>
    /// Current book balance.
    /// </summary>
    public decimal CurrentBalance { get; set; }

    /// <summary>
    /// Last reconciled balance.
    /// </summary>
    public decimal? LastReconciledBalance { get; set; }

    /// <summary>
    /// Date of last reconciliation.
    /// </summary>
    public DateTime? LastReconciliationDate { get; set; }

    /// <summary>
    /// Reference to chart of accounts entry.
    /// </summary>
    public int? ChartOfAccountId { get; set; }

    /// <summary>
    /// Notes about the account.
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public virtual ChartOfAccount? ChartOfAccount { get; set; }
    public virtual ICollection<BankTransaction> Transactions { get; set; } = new List<BankTransaction>();
    public virtual ICollection<ReconciliationSession> ReconciliationSessions { get; set; } = new List<ReconciliationSession>();
}

/// <summary>
/// Imported bank transaction for reconciliation.
/// </summary>
public class BankTransaction : BaseEntity
{
    /// <summary>
    /// Reference to bank account.
    /// </summary>
    public int BankAccountId { get; set; }

    /// <summary>
    /// Transaction type.
    /// </summary>
    public BankTransactionType TransactionType { get; set; }

    /// <summary>
    /// Transaction date from bank.
    /// </summary>
    public DateTime TransactionDate { get; set; }

    /// <summary>
    /// Value date from bank.
    /// </summary>
    public DateTime? ValueDate { get; set; }

    /// <summary>
    /// Bank reference number.
    /// </summary>
    public string BankReference { get; set; } = string.Empty;

    /// <summary>
    /// Transaction description from bank.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Deposit/credit amount.
    /// </summary>
    public decimal? DepositAmount { get; set; }

    /// <summary>
    /// Withdrawal/debit amount.
    /// </summary>
    public decimal? WithdrawalAmount { get; set; }

    /// <summary>
    /// Running balance after transaction.
    /// </summary>
    public decimal? RunningBalance { get; set; }

    /// <summary>
    /// Cheque number if applicable.
    /// </summary>
    public string? ChequeNumber { get; set; }

    /// <summary>
    /// Payee/payer name.
    /// </summary>
    public string? PayeePayer { get; set; }

    /// <summary>
    /// M-Pesa transaction code if applicable.
    /// </summary>
    public string? MpesaCode { get; set; }

    /// <summary>
    /// Current match status.
    /// </summary>
    public ReconciliationMatchStatus MatchStatus { get; set; } = ReconciliationMatchStatus.Unmatched;

    /// <summary>
    /// Reference to matched POS transaction.
    /// </summary>
    public int? MatchedPaymentId { get; set; }

    /// <summary>
    /// Reference to reconciliation session.
    /// </summary>
    public int? ReconciliationSessionId { get; set; }

    /// <summary>
    /// Import batch identifier.
    /// </summary>
    public string? ImportBatchId { get; set; }

    /// <summary>
    /// Date/time imported.
    /// </summary>
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Source file name.
    /// </summary>
    public string? SourceFileName { get; set; }

    /// <summary>
    /// Notes about this transaction.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets the transaction amount (positive for deposits, negative for withdrawals).
    /// </summary>
    public decimal Amount => (DepositAmount ?? 0) - (WithdrawalAmount ?? 0);

    // Navigation properties
    public virtual BankAccount BankAccount { get; set; } = null!;
    public virtual Payment? MatchedPayment { get; set; }
    public virtual ReconciliationSession? ReconciliationSession { get; set; }
    public virtual ICollection<ReconciliationMatch> Matches { get; set; } = new List<ReconciliationMatch>();
}

/// <summary>
/// Bank statement import batch.
/// </summary>
public class BankStatementImport : BaseEntity
{
    /// <summary>
    /// Reference to bank account.
    /// </summary>
    public int BankAccountId { get; set; }

    /// <summary>
    /// Unique batch identifier.
    /// </summary>
    public string BatchId { get; set; } = string.Empty;

    /// <summary>
    /// Source file name.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// File format (CSV, OFX, QIF, Excel, etc.).
    /// </summary>
    public string FileFormat { get; set; } = string.Empty;

    /// <summary>
    /// Statement start date.
    /// </summary>
    public DateTime StatementStartDate { get; set; }

    /// <summary>
    /// Statement end date.
    /// </summary>
    public DateTime StatementEndDate { get; set; }

    /// <summary>
    /// Opening balance on statement.
    /// </summary>
    public decimal OpeningBalance { get; set; }

    /// <summary>
    /// Closing balance on statement.
    /// </summary>
    public decimal ClosingBalance { get; set; }

    /// <summary>
    /// Total number of transactions.
    /// </summary>
    public int TotalTransactions { get; set; }

    /// <summary>
    /// Total deposits.
    /// </summary>
    public decimal TotalDeposits { get; set; }

    /// <summary>
    /// Total withdrawals.
    /// </summary>
    public decimal TotalWithdrawals { get; set; }

    /// <summary>
    /// Number of successfully imported transactions.
    /// </summary>
    public int ImportedCount { get; set; }

    /// <summary>
    /// Number of skipped (duplicate) transactions.
    /// </summary>
    public int SkippedCount { get; set; }

    /// <summary>
    /// Number of failed imports.
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Import status.
    /// </summary>
    public string Status { get; set; } = "Completed";

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// User who performed import.
    /// </summary>
    public int? ImportedByUserId { get; set; }

    /// <summary>
    /// Date/time of import.
    /// </summary>
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual BankAccount BankAccount { get; set; } = null!;
    public virtual User? ImportedByUser { get; set; }
}

/// <summary>
/// Reconciliation session/period.
/// </summary>
public class ReconciliationSession : BaseEntity
{
    /// <summary>
    /// Reference to bank account.
    /// </summary>
    public int BankAccountId { get; set; }

    /// <summary>
    /// Session reference number.
    /// </summary>
    public string SessionNumber { get; set; } = string.Empty;

    /// <summary>
    /// Reconciliation period start date.
    /// </summary>
    public DateTime PeriodStartDate { get; set; }

    /// <summary>
    /// Reconciliation period end date.
    /// </summary>
    public DateTime PeriodEndDate { get; set; }

    /// <summary>
    /// Bank statement balance.
    /// </summary>
    public decimal StatementBalance { get; set; }

    /// <summary>
    /// Book/POS balance.
    /// </summary>
    public decimal BookBalance { get; set; }

    /// <summary>
    /// Adjusted bank balance after reconciliation.
    /// </summary>
    public decimal? AdjustedBankBalance { get; set; }

    /// <summary>
    /// Adjusted book balance after reconciliation.
    /// </summary>
    public decimal? AdjustedBookBalance { get; set; }

    /// <summary>
    /// Difference between bank and book.
    /// </summary>
    public decimal Difference { get; set; }

    /// <summary>
    /// Total unreconciled items from bank.
    /// </summary>
    public decimal UnreconciledBankItems { get; set; }

    /// <summary>
    /// Total unreconciled items from POS.
    /// </summary>
    public decimal UnreconciledPOSItems { get; set; }

    /// <summary>
    /// Session status.
    /// </summary>
    public ReconciliationSessionStatus Status { get; set; } = ReconciliationSessionStatus.InProgress;

    /// <summary>
    /// Number of matched transactions.
    /// </summary>
    public int MatchedCount { get; set; }

    /// <summary>
    /// Number of unmatched transactions.
    /// </summary>
    public int UnmatchedCount { get; set; }

    /// <summary>
    /// Number of discrepancies.
    /// </summary>
    public int DiscrepancyCount { get; set; }

    /// <summary>
    /// User who started the session.
    /// </summary>
    public int? StartedByUserId { get; set; }

    /// <summary>
    /// Date/time session started.
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who completed/approved the session.
    /// </summary>
    public int? CompletedByUserId { get; set; }

    /// <summary>
    /// Date/time session completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Notes about the reconciliation.
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public virtual BankAccount BankAccount { get; set; } = null!;
    public virtual User? StartedByUser { get; set; }
    public virtual User? CompletedByUser { get; set; }
    public virtual ICollection<BankTransaction> BankTransactions { get; set; } = new List<BankTransaction>();
    public virtual ICollection<ReconciliationMatch> Matches { get; set; } = new List<ReconciliationMatch>();
    public virtual ICollection<ReconciliationDiscrepancy> Discrepancies { get; set; } = new List<ReconciliationDiscrepancy>();
}

/// <summary>
/// Match between bank transaction and POS transaction.
/// </summary>
public class ReconciliationMatch : BaseEntity
{
    /// <summary>
    /// Reference to reconciliation session.
    /// </summary>
    public int ReconciliationSessionId { get; set; }

    /// <summary>
    /// Reference to bank transaction.
    /// </summary>
    public int BankTransactionId { get; set; }

    /// <summary>
    /// Reference to POS payment (if matched).
    /// </summary>
    public int? PaymentId { get; set; }

    /// <summary>
    /// Reference to receipt (if matched).
    /// </summary>
    public int? ReceiptId { get; set; }

    /// <summary>
    /// Bank transaction amount.
    /// </summary>
    public decimal BankAmount { get; set; }

    /// <summary>
    /// POS transaction amount.
    /// </summary>
    public decimal? POSAmount { get; set; }

    /// <summary>
    /// Amount difference.
    /// </summary>
    public decimal? AmountDifference { get; set; }

    /// <summary>
    /// Match type (Auto/Manual).
    /// </summary>
    public ReconciliationMatchStatus MatchType { get; set; }

    /// <summary>
    /// Confidence score for auto-match (0-100).
    /// </summary>
    public int? MatchConfidence { get; set; }

    /// <summary>
    /// Matching rule that was applied.
    /// </summary>
    public string? MatchingRule { get; set; }

    /// <summary>
    /// Date/time of match.
    /// </summary>
    public DateTime MatchedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who confirmed/made the match.
    /// </summary>
    public int? MatchedByUserId { get; set; }

    /// <summary>
    /// Notes about the match.
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public virtual ReconciliationSession ReconciliationSession { get; set; } = null!;
    public virtual BankTransaction BankTransaction { get; set; } = null!;
    public virtual Payment? Payment { get; set; }
    public virtual Receipt? Receipt { get; set; }
    public virtual User? MatchedByUser { get; set; }
}

/// <summary>
/// Reconciliation discrepancy for investigation.
/// </summary>
public class ReconciliationDiscrepancy : BaseEntity
{
    /// <summary>
    /// Reference to reconciliation session.
    /// </summary>
    public int ReconciliationSessionId { get; set; }

    /// <summary>
    /// Discrepancy reference number.
    /// </summary>
    public string DiscrepancyNumber { get; set; } = string.Empty;

    /// <summary>
    /// Type of discrepancy.
    /// </summary>
    public DiscrepancyType DiscrepancyType { get; set; }

    /// <summary>
    /// Reference to bank transaction (if applicable).
    /// </summary>
    public int? BankTransactionId { get; set; }

    /// <summary>
    /// Reference to POS payment (if applicable).
    /// </summary>
    public int? PaymentId { get; set; }

    /// <summary>
    /// Reference to receipt (if applicable).
    /// </summary>
    public int? ReceiptId { get; set; }

    /// <summary>
    /// Bank amount.
    /// </summary>
    public decimal? BankAmount { get; set; }

    /// <summary>
    /// POS amount.
    /// </summary>
    public decimal? POSAmount { get; set; }

    /// <summary>
    /// Difference amount.
    /// </summary>
    public decimal DifferenceAmount { get; set; }

    /// <summary>
    /// Description of discrepancy.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Resolution status.
    /// </summary>
    public DiscrepancyResolutionStatus ResolutionStatus { get; set; } = DiscrepancyResolutionStatus.Open;

    /// <summary>
    /// Resolution action taken.
    /// </summary>
    public string? ResolutionAction { get; set; }

    /// <summary>
    /// User who resolved.
    /// </summary>
    public int? ResolvedByUserId { get; set; }

    /// <summary>
    /// Date/time resolved.
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Journal entry created for adjustment.
    /// </summary>
    public int? AdjustmentJournalEntryId { get; set; }

    /// <summary>
    /// Notes about the discrepancy.
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public virtual ReconciliationSession ReconciliationSession { get; set; } = null!;
    public virtual BankTransaction? BankTransaction { get; set; }
    public virtual Payment? Payment { get; set; }
    public virtual Receipt? Receipt { get; set; }
    public virtual User? ResolvedByUser { get; set; }
    public virtual JournalEntry? AdjustmentJournalEntry { get; set; }
}

/// <summary>
/// Matching rule for automatic reconciliation.
/// </summary>
public class ReconciliationMatchingRule : BaseEntity
{
    /// <summary>
    /// Rule name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Rule description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Rule priority (lower = higher priority).
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Whether rule is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Match on reference number.
    /// </summary>
    public bool MatchOnReference { get; set; } = true;

    /// <summary>
    /// Match on amount.
    /// </summary>
    public bool MatchOnAmount { get; set; } = true;

    /// <summary>
    /// Amount tolerance for matching (absolute).
    /// </summary>
    public decimal AmountTolerance { get; set; } = 0m;

    /// <summary>
    /// Match on date.
    /// </summary>
    public bool MatchOnDate { get; set; } = true;

    /// <summary>
    /// Date tolerance in days.
    /// </summary>
    public int DateToleranceDays { get; set; } = 3;

    /// <summary>
    /// Match on M-Pesa code.
    /// </summary>
    public bool MatchOnMpesaCode { get; set; } = true;

    /// <summary>
    /// Match on cheque number.
    /// </summary>
    public bool MatchOnChequeNumber { get; set; } = true;

    /// <summary>
    /// Pattern to extract from bank description.
    /// </summary>
    public string? DescriptionPattern { get; set; }

    /// <summary>
    /// Minimum confidence score required.
    /// </summary>
    public int MinimumConfidence { get; set; } = 80;

    /// <summary>
    /// Apply to specific bank account only.
    /// </summary>
    public int? BankAccountId { get; set; }

    /// <summary>
    /// Apply to specific transaction type only.
    /// </summary>
    public BankTransactionType? TransactionType { get; set; }

    // Navigation properties
    public virtual BankAccount? BankAccount { get; set; }
}
