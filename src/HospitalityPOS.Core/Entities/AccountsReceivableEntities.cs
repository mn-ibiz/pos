namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Status of a customer credit account.
/// </summary>
public enum CreditAccountStatus
{
    /// <summary>Account is active and in good standing.</summary>
    Active = 1,
    /// <summary>Account is active but has overdue balances.</summary>
    Overdue = 2,
    /// <summary>Account is suspended due to non-payment.</summary>
    Suspended = 3,
    /// <summary>Account is closed.</summary>
    Closed = 4
}

/// <summary>
/// Type of credit transaction.
/// </summary>
public enum CreditTransactionType
{
    /// <summary>Credit sale - increases balance.</summary>
    Sale = 1,
    /// <summary>Payment received - decreases balance.</summary>
    Payment = 2,
    /// <summary>Credit note/refund - decreases balance.</summary>
    CreditNote = 3,
    /// <summary>Debit adjustment - increases balance.</summary>
    DebitAdjustment = 4,
    /// <summary>Credit adjustment - decreases balance.</summary>
    CreditAdjustment = 5,
    /// <summary>Finance charge/interest.</summary>
    FinanceCharge = 6,
    /// <summary>Write-off of bad debt.</summary>
    WriteOff = 7
}

/// <summary>
/// Customer credit account for trade customers.
/// </summary>
public class CustomerCreditAccount : BaseEntity
{
    /// <summary>
    /// Reference to the customer/loyalty member.
    /// </summary>
    public int? CustomerId { get; set; }

    /// <summary>
    /// Account number (unique identifier for the credit account).
    /// </summary>
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// Business/Company name for corporate accounts.
    /// </summary>
    public string? BusinessName { get; set; }

    /// <summary>
    /// Contact person name.
    /// </summary>
    public string ContactName { get; set; } = string.Empty;

    /// <summary>
    /// Contact phone number.
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Contact email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Billing address.
    /// </summary>
    public string? BillingAddress { get; set; }

    /// <summary>
    /// KRA PIN for tax purposes (Kenya).
    /// </summary>
    public string? KRAPin { get; set; }

    /// <summary>
    /// Credit limit for this account.
    /// </summary>
    public decimal CreditLimit { get; set; }

    /// <summary>
    /// Current outstanding balance.
    /// </summary>
    public decimal CurrentBalance { get; set; }

    /// <summary>
    /// Payment terms in days (e.g., 30, 60, 90).
    /// </summary>
    public int PaymentTermsDays { get; set; } = 30;

    /// <summary>
    /// Account status.
    /// </summary>
    public CreditAccountStatus Status { get; set; } = CreditAccountStatus.Active;

    /// <summary>
    /// Date when the account was opened.
    /// </summary>
    public DateTime AccountOpenedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date of last transaction on the account.
    /// </summary>
    public DateTime? LastTransactionDate { get; set; }

    /// <summary>
    /// Date of last payment received.
    /// </summary>
    public DateTime? LastPaymentDate { get; set; }

    /// <summary>
    /// Default discount percentage for this customer.
    /// </summary>
    public decimal? DefaultDiscountPercent { get; set; }

    /// <summary>
    /// Internal notes about the account.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets the available credit (limit - current balance).
    /// </summary>
    public decimal AvailableCredit => CreditLimit - CurrentBalance;

    /// <summary>
    /// Gets whether the account can make purchases.
    /// </summary>
    public bool CanPurchase => Status == CreditAccountStatus.Active && AvailableCredit > 0;

    // Navigation properties
    public virtual LoyaltyMember? Customer { get; set; }
    public virtual ICollection<CreditTransaction> Transactions { get; set; } = new List<CreditTransaction>();
    public virtual ICollection<CustomerStatement> Statements { get; set; } = new List<CustomerStatement>();
}

/// <summary>
/// Transaction on a customer credit account.
/// </summary>
public class CreditTransaction : BaseEntity
{
    /// <summary>
    /// Reference to the credit account.
    /// </summary>
    public int CreditAccountId { get; set; }

    /// <summary>
    /// Type of transaction.
    /// </summary>
    public CreditTransactionType TransactionType { get; set; }

    /// <summary>
    /// Transaction reference number.
    /// </summary>
    public string ReferenceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Date of the transaction.
    /// </summary>
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Due date for payment (for sales).
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Transaction amount (positive value).
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Description of the transaction.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Reference to associated receipt (for sales).
    /// </summary>
    public int? ReceiptId { get; set; }

    /// <summary>
    /// Reference to associated payment record.
    /// </summary>
    public int? PaymentId { get; set; }

    /// <summary>
    /// Running balance after this transaction.
    /// </summary>
    public decimal RunningBalance { get; set; }

    /// <summary>
    /// Amount paid against this transaction (for sales).
    /// </summary>
    public decimal AmountPaid { get; set; }

    /// <summary>
    /// Remaining balance for this transaction.
    /// </summary>
    public decimal RemainingBalance => TransactionType == CreditTransactionType.Sale
        ? Amount - AmountPaid
        : 0;

    /// <summary>
    /// Whether this transaction is fully paid.
    /// </summary>
    public bool IsFullyPaid => RemainingBalance <= 0;

    /// <summary>
    /// Days overdue (for unpaid sales).
    /// </summary>
    public int DaysOverdue => DueDate.HasValue && !IsFullyPaid && DateTime.UtcNow > DueDate.Value
        ? (DateTime.UtcNow - DueDate.Value).Days
        : 0;

    /// <summary>
    /// User who processed this transaction.
    /// </summary>
    public int? ProcessedByUserId { get; set; }

    // Navigation properties
    public virtual CustomerCreditAccount CreditAccount { get; set; } = null!;
    public virtual Receipt? Receipt { get; set; }
    public virtual CustomerPayment? Payment { get; set; }
    public virtual User? ProcessedByUser { get; set; }
    public virtual ICollection<PaymentAllocation> PaymentAllocations { get; set; } = new List<PaymentAllocation>();
}

/// <summary>
/// Payment received from a customer.
/// </summary>
public class CustomerPayment : BaseEntity
{
    /// <summary>
    /// Reference to the credit account.
    /// </summary>
    public int CreditAccountId { get; set; }

    /// <summary>
    /// Payment reference number.
    /// </summary>
    public string PaymentNumber { get; set; } = string.Empty;

    /// <summary>
    /// Date payment was received.
    /// </summary>
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Total payment amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Payment method.
    /// </summary>
    public int PaymentMethodId { get; set; }

    /// <summary>
    /// External reference (cheque number, M-Pesa code, etc.).
    /// </summary>
    public string? ExternalReference { get; set; }

    /// <summary>
    /// Notes about the payment.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Amount allocated to invoices.
    /// </summary>
    public decimal AllocatedAmount { get; set; }

    /// <summary>
    /// Unallocated amount (can be used for future invoices).
    /// </summary>
    public decimal UnallocatedAmount => Amount - AllocatedAmount;

    /// <summary>
    /// User who received/recorded the payment.
    /// </summary>
    public int? ReceivedByUserId { get; set; }

    // Navigation properties
    public virtual CustomerCreditAccount CreditAccount { get; set; } = null!;
    public virtual PaymentMethod PaymentMethod { get; set; } = null!;
    public virtual User? ReceivedByUser { get; set; }
    public virtual ICollection<PaymentAllocation> Allocations { get; set; } = new List<PaymentAllocation>();
}

/// <summary>
/// Allocation of payment to specific transactions/invoices.
/// </summary>
public class PaymentAllocation : BaseEntity
{
    /// <summary>
    /// Reference to the payment.
    /// </summary>
    public int PaymentId { get; set; }

    /// <summary>
    /// Reference to the transaction being paid.
    /// </summary>
    public int TransactionId { get; set; }

    /// <summary>
    /// Amount allocated from payment to this transaction.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Date of allocation.
    /// </summary>
    public DateTime AllocationDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual CustomerPayment Payment { get; set; } = null!;
    public virtual CreditTransaction Transaction { get; set; } = null!;
}

/// <summary>
/// Customer statement for a period.
/// </summary>
public class CustomerStatement : BaseEntity
{
    /// <summary>
    /// Reference to the credit account.
    /// </summary>
    public int CreditAccountId { get; set; }

    /// <summary>
    /// Statement number.
    /// </summary>
    public string StatementNumber { get; set; } = string.Empty;

    /// <summary>
    /// Statement period start date.
    /// </summary>
    public DateTime PeriodStartDate { get; set; }

    /// <summary>
    /// Statement period end date.
    /// </summary>
    public DateTime PeriodEndDate { get; set; }

    /// <summary>
    /// Opening balance for the period.
    /// </summary>
    public decimal OpeningBalance { get; set; }

    /// <summary>
    /// Total charges during the period.
    /// </summary>
    public decimal TotalCharges { get; set; }

    /// <summary>
    /// Total payments during the period.
    /// </summary>
    public decimal TotalPayments { get; set; }

    /// <summary>
    /// Total credits during the period.
    /// </summary>
    public decimal TotalCredits { get; set; }

    /// <summary>
    /// Closing balance for the period.
    /// </summary>
    public decimal ClosingBalance { get; set; }

    /// <summary>
    /// Date the statement was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date the statement was sent to customer.
    /// </summary>
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// How the statement was sent (Email, Print, SMS).
    /// </summary>
    public string? SentVia { get; set; }

    /// <summary>
    /// Path to the generated PDF statement.
    /// </summary>
    public string? PdfPath { get; set; }

    // Navigation properties
    public virtual CustomerCreditAccount CreditAccount { get; set; } = null!;
}

/// <summary>
/// Aging bucket for AR aging report.
/// </summary>
public enum AgingBucket
{
    /// <summary>Current (not yet due).</summary>
    Current = 0,
    /// <summary>1-30 days overdue.</summary>
    Days1To30 = 1,
    /// <summary>31-60 days overdue.</summary>
    Days31To60 = 2,
    /// <summary>61-90 days overdue.</summary>
    Days61To90 = 3,
    /// <summary>Over 90 days overdue.</summary>
    Over90Days = 4
}

/// <summary>
/// AR aging entry for reporting.
/// </summary>
public class AgingEntry
{
    public int CreditAccountId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal CurrentAmount { get; set; }
    public decimal Days1To30 { get; set; }
    public decimal Days31To60 { get; set; }
    public decimal Days61To90 { get; set; }
    public decimal Over90Days { get; set; }
    public decimal TotalBalance { get; set; }
}
