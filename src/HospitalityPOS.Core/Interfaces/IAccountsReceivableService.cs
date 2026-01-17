using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for managing customer credit accounts and accounts receivable.
/// </summary>
public interface IAccountsReceivableService
{
    #region Credit Account Management

    /// <summary>
    /// Creates a new customer credit account.
    /// </summary>
    /// <param name="account">The account to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created account.</returns>
    Task<CustomerCreditAccount> CreateAccountAsync(CustomerCreditAccount account, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a credit account by ID.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The account if found.</returns>
    Task<CustomerCreditAccount?> GetAccountByIdAsync(int accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a credit account by account number.
    /// </summary>
    /// <param name="accountNumber">The account number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The account if found.</returns>
    Task<CustomerCreditAccount?> GetAccountByNumberAsync(string accountNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all credit accounts.
    /// </summary>
    /// <param name="status">Optional status filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of credit accounts.</returns>
    Task<IEnumerable<CustomerCreditAccount>> GetAllAccountsAsync(CreditAccountStatus? status = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a credit account.
    /// </summary>
    /// <param name="account">The account to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated account.</returns>
    Task<CustomerCreditAccount> UpdateAccountAsync(CustomerCreditAccount account, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the credit limit for an account.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="newLimit">The new credit limit.</param>
    /// <param name="userId">User making the change.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateCreditLimitAsync(int accountId, decimal newLimit, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Suspends a credit account.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="reason">Reason for suspension.</param>
    /// <param name="userId">User making the change.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SuspendAccountAsync(int accountId, string reason, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reactivates a suspended account.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="userId">User making the change.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ReactivateAccountAsync(int accountId, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a customer can make a credit purchase for a given amount.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="amount">The purchase amount.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating if purchase is allowed.</returns>
    Task<CreditPurchaseCheckResult> CanMakeCreditPurchaseAsync(int accountId, decimal amount, CancellationToken cancellationToken = default);

    #endregion

    #region Credit Transactions

    /// <summary>
    /// Records a credit sale transaction.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="receiptId">The receipt ID.</param>
    /// <param name="amount">Sale amount.</param>
    /// <param name="userId">User processing the sale.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created transaction.</returns>
    Task<CreditTransaction> RecordCreditSaleAsync(
        int accountId,
        int receiptId,
        decimal amount,
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a credit note/refund transaction.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="amount">Credit note amount.</param>
    /// <param name="reason">Reason for credit.</param>
    /// <param name="userId">User processing the credit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created transaction.</returns>
    Task<CreditTransaction> RecordCreditNoteAsync(
        int accountId,
        decimal amount,
        string reason,
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records an adjustment transaction.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="amount">Adjustment amount (positive for debit, negative for credit).</param>
    /// <param name="reason">Reason for adjustment.</param>
    /// <param name="userId">User processing the adjustment.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created transaction.</returns>
    Task<CreditTransaction> RecordAdjustmentAsync(
        int accountId,
        decimal amount,
        string reason,
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets transactions for an account.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="startDate">Optional start date filter.</param>
    /// <param name="endDate">Optional end date filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of transactions.</returns>
    Task<IEnumerable<CreditTransaction>> GetTransactionsAsync(
        int accountId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets outstanding (unpaid) transactions for an account.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of outstanding transactions.</returns>
    Task<IEnumerable<CreditTransaction>> GetOutstandingTransactionsAsync(int accountId, CancellationToken cancellationToken = default);

    #endregion

    #region Payment Processing

    /// <summary>
    /// Records a customer payment.
    /// </summary>
    /// <param name="payment">The payment details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created payment.</returns>
    Task<CustomerPayment> RecordPaymentAsync(CustomerPayment payment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Allocates a payment to specific transactions.
    /// </summary>
    /// <param name="paymentId">The payment ID.</param>
    /// <param name="allocations">List of (transactionId, amount) to allocate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AllocatePaymentAsync(
        int paymentId,
        IEnumerable<(int transactionId, decimal amount)> allocations,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Auto-allocates a payment to oldest outstanding transactions (FIFO).
    /// </summary>
    /// <param name="paymentId">The payment ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of allocations made.</returns>
    Task<IEnumerable<PaymentAllocation>> AutoAllocatePaymentAsync(int paymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets payments for an account.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="startDate">Optional start date filter.</param>
    /// <param name="endDate">Optional end date filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of payments.</returns>
    Task<IEnumerable<CustomerPayment>> GetPaymentsAsync(
        int accountId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Aging Reports

    /// <summary>
    /// Generates AR aging report.
    /// </summary>
    /// <param name="asOfDate">Date to calculate aging as of.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of aging entries.</returns>
    Task<IEnumerable<AgingEntry>> GetAgingReportAsync(DateTime asOfDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets aging summary totals.
    /// </summary>
    /// <param name="asOfDate">Date to calculate aging as of.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aging summary.</returns>
    Task<AgingSummary> GetAgingSummaryAsync(DateTime asOfDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets aging detail for a specific account.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="asOfDate">Date to calculate aging as of.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Account aging detail.</returns>
    Task<AccountAgingDetail> GetAccountAgingDetailAsync(int accountId, DateTime asOfDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets accounts with overdue balances.
    /// </summary>
    /// <param name="daysOverdue">Minimum days overdue.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of overdue accounts.</returns>
    Task<IEnumerable<CustomerCreditAccount>> GetOverdueAccountsAsync(int daysOverdue = 1, CancellationToken cancellationToken = default);

    #endregion

    #region Statement Generation

    /// <summary>
    /// Generates a customer statement.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="startDate">Statement period start.</param>
    /// <param name="endDate">Statement period end.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated statement.</returns>
    Task<CustomerStatement> GenerateStatementAsync(
        int accountId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates statements for all active accounts.
    /// </summary>
    /// <param name="startDate">Statement period start.</param>
    /// <param name="endDate">Statement period end.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of generated statements.</returns>
    Task<IEnumerable<CustomerStatement>> GenerateAllStatementsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets statements for an account.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of statements.</returns>
    Task<IEnumerable<CustomerStatement>> GetStatementsAsync(int accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a statement as sent.
    /// </summary>
    /// <param name="statementId">The statement ID.</param>
    /// <param name="sentVia">How it was sent (Email, Print, SMS).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task MarkStatementSentAsync(int statementId, string sentVia, CancellationToken cancellationToken = default);

    #endregion
}

#region DTOs

/// <summary>
/// Result of credit purchase check.
/// </summary>
public class CreditPurchaseCheckResult
{
    public bool IsAllowed { get; set; }
    public string? DenialReason { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal AvailableCredit { get; set; }
    public decimal RequestedAmount { get; set; }
    public CreditAccountStatus AccountStatus { get; set; }
}

/// <summary>
/// AR aging summary totals.
/// </summary>
public class AgingSummary
{
    public DateTime AsOfDate { get; set; }
    public int TotalAccounts { get; set; }
    public decimal TotalOutstanding { get; set; }
    public decimal CurrentAmount { get; set; }
    public decimal Days1To30 { get; set; }
    public decimal Days31To60 { get; set; }
    public decimal Days61To90 { get; set; }
    public decimal Over90Days { get; set; }

    public decimal PercentCurrent => TotalOutstanding > 0 ? CurrentAmount / TotalOutstanding * 100 : 0;
    public decimal PercentOverdue => TotalOutstanding > 0 ? (Days1To30 + Days31To60 + Days61To90 + Over90Days) / TotalOutstanding * 100 : 0;
}

/// <summary>
/// Aging detail for a single account.
/// </summary>
public class AccountAgingDetail
{
    public int AccountId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
    public decimal TotalBalance { get; set; }

    public decimal CurrentAmount { get; set; }
    public decimal Days1To30 { get; set; }
    public decimal Days31To60 { get; set; }
    public decimal Days61To90 { get; set; }
    public decimal Over90Days { get; set; }

    public IList<TransactionAgingDetail> Transactions { get; set; } = new List<TransactionAgingDetail>();
}

/// <summary>
/// Aging detail for a single transaction.
/// </summary>
public class TransactionAgingDetail
{
    public int TransactionId { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public int DaysOverdue { get; set; }
    public AgingBucket Bucket { get; set; }
}

#endregion
