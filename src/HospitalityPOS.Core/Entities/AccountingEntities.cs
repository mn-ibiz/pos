using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

#region Enums

/// <summary>
/// Account sub-types for detailed classification.
/// </summary>
public enum AccountSubType
{
    // Asset Sub-Types
    Cash = 1,
    BankAccount = 2,
    AccountsReceivable = 3,
    Inventory = 4,
    PrepaidExpenses = 5,
    FixedAssets = 6,
    AccumulatedDepreciation = 7,
    OtherCurrentAsset = 8,
    OtherAsset = 9,

    // Liability Sub-Types
    AccountsPayable = 20,
    CreditCard = 21,
    SalesTaxPayable = 22,
    PayrollLiabilities = 23,
    ShortTermLoan = 24,
    LongTermLoan = 25,
    OtherCurrentLiability = 26,
    OtherLiability = 27,

    // Equity Sub-Types
    OwnersEquity = 40,
    RetainedEarnings = 41,
    CommonStock = 42,
    PreferredStock = 43,
    AdditionalPaidInCapital = 44,
    Dividends = 45,

    // Revenue Sub-Types
    SalesRevenue = 60,
    ServiceRevenue = 61,
    OtherIncome = 62,
    InterestIncome = 63,
    Discounts = 64,
    ReturnsAndAllowances = 65,

    // Expense Sub-Types
    CostOfGoodsSold = 80,
    PayrollExpense = 81,
    RentExpense = 82,
    UtilitiesExpense = 83,
    DepreciationExpense = 84,
    InsuranceExpense = 85,
    MarketingExpense = 86,
    OfficeExpense = 87,
    ProfessionalFees = 88,
    InterestExpense = 89,
    TaxExpense = 90,
    OtherExpense = 91
}

/// <summary>
/// Normal balance type for accounts.
/// </summary>
public enum NormalBalance
{
    Debit = 1,
    Credit = 2
}

/// <summary>
/// Bank reconciliation status.
/// </summary>
public enum ReconciliationStatus
{
    InProgress = 1,
    Completed = 2,
    Voided = 3
}

/// <summary>
/// Bank transaction type.
/// </summary>
public enum BankTransactionType
{
    Deposit = 1,
    Withdrawal = 2,
    Transfer = 3,
    BankFee = 4,
    InterestEarned = 5,
    Check = 6,
    DirectDebit = 7,
    DirectCredit = 8
}

/// <summary>
/// Period close status.
/// </summary>
public enum PeriodCloseStatus
{
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    Reopened = 4
}

/// <summary>
/// Financial statement types.
/// </summary>
public enum FinancialStatementType
{
    IncomeStatement = 1,
    BalanceSheet = 2,
    CashFlowStatement = 3,
    TrialBalance = 4,
    GeneralLedger = 5
}

/// <summary>
/// Transaction source types for GL mapping.
/// </summary>
public enum TransactionSourceType
{
    Sale = 1,
    SalePayment = 2,
    SaleRefund = 3,
    Purchase = 4,
    PurchasePayment = 5,
    PurchaseReturn = 6,
    InventoryAdjustment = 7,
    StockTransfer = 8,
    Expense = 9,
    PayrollPayment = 10,
    BankDeposit = 11,
    BankWithdrawal = 12,
    JournalEntry = 13,
    OpeningBalance = 14,
    ClosingEntry = 15,
    Depreciation = 16,
    LoyaltyRedemption = 17,
    SupplierPayment = 18,
    CustomerPayment = 19,
    TaxPayment = 20
}

#endregion

#region GL Account Mapping

/// <summary>
/// Maps transaction types to GL accounts for automated journal entry generation.
/// </summary>
public class GLAccountMapping : BaseEntity
{
    /// <summary>
    /// The transaction source type this mapping applies to.
    /// </summary>
    public TransactionSourceType SourceType { get; set; }

    /// <summary>
    /// Optional description for this mapping.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Account to debit for this transaction type.
    /// </summary>
    public int DebitAccountId { get; set; }

    /// <summary>
    /// Account to credit for this transaction type.
    /// </summary>
    public int CreditAccountId { get; set; }

    /// <summary>
    /// Optional category filter (e.g., specific product category).
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Optional payment method filter.
    /// </summary>
    public PaymentMethodType? PaymentMethod { get; set; }

    /// <summary>
    /// Optional store filter for multi-store setups.
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// Whether this mapping is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Priority for mapping selection (higher = more specific).
    /// </summary>
    public int Priority { get; set; } = 0;

    // Navigation properties
    public virtual ChartOfAccount DebitAccount { get; set; } = null!;
    public virtual ChartOfAccount CreditAccount { get; set; } = null!;
    public virtual Category? Category { get; set; }
    public virtual Store? Store { get; set; }
}

#endregion

#region Bank Reconciliation

/// <summary>
/// Represents a bank reconciliation session.
/// </summary>
public class BankReconciliation : BaseEntity
{
    /// <summary>
    /// Reference number for this reconciliation.
    /// </summary>
    public string ReconciliationNumber { get; set; } = string.Empty;

    /// <summary>
    /// Bank account being reconciled.
    /// </summary>
    public int BankAccountId { get; set; }

    /// <summary>
    /// Statement date from bank.
    /// </summary>
    public DateTime StatementDate { get; set; }

    /// <summary>
    /// Statement ending balance from bank.
    /// </summary>
    public decimal StatementEndingBalance { get; set; }

    /// <summary>
    /// Beginning book balance (GL balance at start).
    /// </summary>
    public decimal BeginningBookBalance { get; set; }

    /// <summary>
    /// Ending book balance after all transactions.
    /// </summary>
    public decimal EndingBookBalance { get; set; }

    /// <summary>
    /// Total cleared deposits.
    /// </summary>
    public decimal ClearedDeposits { get; set; }

    /// <summary>
    /// Total cleared withdrawals/checks.
    /// </summary>
    public decimal ClearedWithdrawals { get; set; }

    /// <summary>
    /// Calculated difference (should be zero when balanced).
    /// </summary>
    public decimal Difference { get; set; }

    /// <summary>
    /// Reconciliation status.
    /// </summary>
    public ReconciliationStatus Status { get; set; } = ReconciliationStatus.InProgress;

    /// <summary>
    /// Notes about the reconciliation.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// User who completed the reconciliation.
    /// </summary>
    public int? CompletedByUserId { get; set; }

    /// <summary>
    /// Date/time when completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    // Navigation properties
    public virtual ChartOfAccount BankAccount { get; set; } = null!;
    public virtual User? CompletedByUser { get; set; }
    public virtual ICollection<BankReconciliationItem> Items { get; set; } = new List<BankReconciliationItem>();
}

/// <summary>
/// Individual transaction in a bank reconciliation.
/// </summary>
public class BankReconciliationItem : BaseEntity
{
    /// <summary>
    /// Parent reconciliation.
    /// </summary>
    public int BankReconciliationId { get; set; }

    /// <summary>
    /// Related journal entry line if from GL.
    /// </summary>
    public int? JournalEntryLineId { get; set; }

    /// <summary>
    /// Transaction date.
    /// </summary>
    public DateTime TransactionDate { get; set; }

    /// <summary>
    /// Transaction type.
    /// </summary>
    public BankTransactionType TransactionType { get; set; }

    /// <summary>
    /// Check number if applicable.
    /// </summary>
    public string? CheckNumber { get; set; }

    /// <summary>
    /// Description/payee.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Transaction amount (positive for deposits, negative for withdrawals).
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Whether this item has been cleared/reconciled.
    /// </summary>
    public bool IsCleared { get; set; }

    /// <summary>
    /// Date when item was cleared.
    /// </summary>
    public DateTime? ClearedDate { get; set; }

    /// <summary>
    /// Bank reference/statement line number.
    /// </summary>
    public string? BankReference { get; set; }

    /// <summary>
    /// Whether this is a bank-side adjustment (fee, interest, etc.).
    /// </summary>
    public bool IsBankAdjustment { get; set; }

    // Navigation properties
    public virtual BankReconciliation BankReconciliation { get; set; } = null!;
    public virtual JournalEntryLine? JournalEntryLine { get; set; }
}

#endregion

#region Period Close

/// <summary>
/// Tracks the closing process for an accounting period.
/// </summary>
public class PeriodClose : BaseEntity
{
    /// <summary>
    /// The accounting period being closed.
    /// </summary>
    public int AccountingPeriodId { get; set; }

    /// <summary>
    /// Close status.
    /// </summary>
    public PeriodCloseStatus Status { get; set; } = PeriodCloseStatus.Pending;

    /// <summary>
    /// User who initiated the close.
    /// </summary>
    public int InitiatedByUserId { get; set; }

    /// <summary>
    /// When the close was initiated.
    /// </summary>
    public DateTime InitiatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who completed the close.
    /// </summary>
    public int? CompletedByUserId { get; set; }

    /// <summary>
    /// When the close was completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Revenue closing journal entry.
    /// </summary>
    public int? RevenueCloseEntryId { get; set; }

    /// <summary>
    /// Expense closing journal entry.
    /// </summary>
    public int? ExpenseCloseEntryId { get; set; }

    /// <summary>
    /// Income summary to retained earnings entry.
    /// </summary>
    public int? IncomeSummaryEntryId { get; set; }

    /// <summary>
    /// Total revenue for the period.
    /// </summary>
    public decimal TotalRevenue { get; set; }

    /// <summary>
    /// Total expenses for the period.
    /// </summary>
    public decimal TotalExpenses { get; set; }

    /// <summary>
    /// Net income (Revenue - Expenses).
    /// </summary>
    public decimal NetIncome { get; set; }

    /// <summary>
    /// Close checklist items completed.
    /// </summary>
    public string? ChecklistJson { get; set; }

    /// <summary>
    /// Notes about the close.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// User who reopened the period (if applicable).
    /// </summary>
    public int? ReopenedByUserId { get; set; }

    /// <summary>
    /// When period was reopened.
    /// </summary>
    public DateTime? ReopenedAt { get; set; }

    /// <summary>
    /// Reason for reopening.
    /// </summary>
    public string? ReopenReason { get; set; }

    // Navigation properties
    public virtual AccountingPeriod AccountingPeriod { get; set; } = null!;
    public virtual User InitiatedByUser { get; set; } = null!;
    public virtual User? CompletedByUser { get; set; }
    public virtual User? ReopenedByUser { get; set; }
    public virtual JournalEntry? RevenueCloseEntry { get; set; }
    public virtual JournalEntry? ExpenseCloseEntry { get; set; }
    public virtual JournalEntry? IncomeSummaryEntry { get; set; }
}

/// <summary>
/// Period close checklist item.
/// </summary>
public class PeriodCloseChecklistItem
{
    public string ItemName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? CompletedByUserId { get; set; }
    public string? Notes { get; set; }
}

#endregion

#region Financial Statements

/// <summary>
/// Generated financial statement record.
/// </summary>
public class FinancialStatement : BaseEntity
{
    /// <summary>
    /// Statement type.
    /// </summary>
    public FinancialStatementType StatementType { get; set; }

    /// <summary>
    /// Report title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Period start date.
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// Period end date.
    /// </summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>
    /// Accounting period if applicable.
    /// </summary>
    public int? AccountingPeriodId { get; set; }

    /// <summary>
    /// Store filter if applicable.
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// User who generated the report.
    /// </summary>
    public int GeneratedByUserId { get; set; }

    /// <summary>
    /// Generation timestamp.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// JSON data of the statement for regeneration/comparison.
    /// </summary>
    public string DataJson { get; set; } = "{}";

    /// <summary>
    /// Notes about this statement version.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Whether this is a comparative statement.
    /// </summary>
    public bool IsComparative { get; set; }

    /// <summary>
    /// Comparison period start if comparative.
    /// </summary>
    public DateTime? ComparisonPeriodStart { get; set; }

    /// <summary>
    /// Comparison period end if comparative.
    /// </summary>
    public DateTime? ComparisonPeriodEnd { get; set; }

    // Navigation properties
    public virtual AccountingPeriod? AccountingPeriod { get; set; }
    public virtual Store? Store { get; set; }
    public virtual User GeneratedByUser { get; set; } = null!;
}

#endregion

#region Account Balance Tracking

/// <summary>
/// Monthly account balance snapshot for reporting efficiency.
/// </summary>
public class AccountBalance : BaseEntity
{
    /// <summary>
    /// The account.
    /// </summary>
    public int AccountId { get; set; }

    /// <summary>
    /// Year.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Month (1-12).
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// Opening balance for the month.
    /// </summary>
    public decimal OpeningBalance { get; set; }

    /// <summary>
    /// Total debits for the month.
    /// </summary>
    public decimal TotalDebits { get; set; }

    /// <summary>
    /// Total credits for the month.
    /// </summary>
    public decimal TotalCredits { get; set; }

    /// <summary>
    /// Closing balance for the month.
    /// </summary>
    public decimal ClosingBalance { get; set; }

    /// <summary>
    /// Transaction count for the month.
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Last updated timestamp.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ChartOfAccount Account { get; set; } = null!;
}

#endregion

#region Budget

/// <summary>
/// Budget for an account or category.
/// </summary>
public class AccountBudget : BaseEntity
{
    /// <summary>
    /// Budget name/description.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Account this budget applies to.
    /// </summary>
    public int AccountId { get; set; }

    /// <summary>
    /// Fiscal year.
    /// </summary>
    public int FiscalYear { get; set; }

    /// <summary>
    /// Monthly budget amounts (JSON array of 12 values).
    /// </summary>
    public string MonthlyAmountsJson { get; set; } = "[]";

    /// <summary>
    /// Total annual budget.
    /// </summary>
    public decimal AnnualBudget { get; set; }

    /// <summary>
    /// Whether budget is approved.
    /// </summary>
    public bool IsApproved { get; set; }

    /// <summary>
    /// User who approved the budget.
    /// </summary>
    public int? ApprovedByUserId { get; set; }

    /// <summary>
    /// Approval date.
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// Notes about the budget.
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public virtual ChartOfAccount Account { get; set; } = null!;
    public virtual User? ApprovedByUser { get; set; }

    /// <summary>
    /// Gets the monthly budget amounts.
    /// </summary>
    public decimal[] GetMonthlyAmounts()
    {
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<decimal[]>(MonthlyAmountsJson) ?? new decimal[12];
        }
        catch
        {
            return new decimal[12];
        }
    }

    /// <summary>
    /// Sets the monthly budget amounts.
    /// </summary>
    public void SetMonthlyAmounts(decimal[] amounts)
    {
        MonthlyAmountsJson = System.Text.Json.JsonSerializer.Serialize(amounts);
        AnnualBudget = amounts.Sum();
    }
}

#endregion

#region Audit Trail

/// <summary>
/// Audit trail for accounting changes.
/// </summary>
public class AccountingAuditLog : BaseEntity
{
    /// <summary>
    /// Action type.
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Entity type affected.
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Entity ID affected.
    /// </summary>
    public int EntityId { get; set; }

    /// <summary>
    /// Previous value (JSON).
    /// </summary>
    public string? OldValue { get; set; }

    /// <summary>
    /// New value (JSON).
    /// </summary>
    public string? NewValue { get; set; }

    /// <summary>
    /// User who made the change.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// IP address if available.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Additional context.
    /// </summary>
    public string? Context { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
}

#endregion
