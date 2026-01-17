using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for checkout enhancement features.
/// </summary>
public interface ICheckoutEnhancementService
{
    #region Suspended Transactions (Park/Recall)

    /// <summary>
    /// Parks (suspends) a transaction for later recall.
    /// </summary>
    Task<SuspendedTransaction> ParkTransactionAsync(ParkTransactionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a suspended transaction by ID.
    /// </summary>
    Task<SuspendedTransaction?> GetSuspendedTransactionAsync(int transactionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a suspended transaction by reference number.
    /// </summary>
    Task<SuspendedTransaction?> GetSuspendedTransactionByReferenceAsync(string referenceNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all parked transactions for a store.
    /// </summary>
    Task<IEnumerable<SuspendedTransaction>> GetParkedTransactionsAsync(int storeId, int? terminalId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recalls a suspended transaction.
    /// </summary>
    Task<RecallTransactionResult> RecallTransactionAsync(int transactionId, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Voids a suspended transaction.
    /// </summary>
    Task VoidSuspendedTransactionAsync(int transactionId, int userId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a suspended transaction as completed.
    /// </summary>
    Task CompleteSuspendedTransactionAsync(int transactionId, int receiptId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes expired suspended transactions.
    /// </summary>
    Task<int> ProcessExpiredTransactionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches suspended transactions.
    /// </summary>
    Task<IEnumerable<SuspendedTransaction>> SearchSuspendedTransactionsAsync(SuspendedTransactionSearchRequest request, CancellationToken cancellationToken = default);

    #endregion

    #region Customer-Facing Display

    /// <summary>
    /// Gets customer display configuration.
    /// </summary>
    Task<CustomerDisplayConfig?> GetCustomerDisplayConfigAsync(int storeId, int? terminalId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves customer display configuration.
    /// </summary>
    Task<CustomerDisplayConfig> SaveCustomerDisplayConfigAsync(CustomerDisplayConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets promotional messages for display.
    /// </summary>
    Task<IEnumerable<CustomerDisplayMessage>> GetActivePromotionalMessagesAsync(int displayConfigId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a promotional message.
    /// </summary>
    Task<CustomerDisplayMessage> SavePromotionalMessageAsync(CustomerDisplayMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a promotional message.
    /// </summary>
    Task DeletePromotionalMessageAsync(int messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current display state for a terminal.
    /// </summary>
    Task<CustomerDisplayState> GetCustomerDisplayStateAsync(int storeId, int? terminalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates current display state.
    /// </summary>
    Task UpdateCustomerDisplayStateAsync(int storeId, int? terminalId, CustomerDisplayState state, CancellationToken cancellationToken = default);

    #endregion

    #region Enhanced Split Payment

    /// <summary>
    /// Gets split payment configuration.
    /// </summary>
    Task<SplitPaymentConfig?> GetSplitPaymentConfigAsync(int storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves split payment configuration.
    /// </summary>
    Task<SplitPaymentConfig> SaveSplitPaymentConfigAsync(SplitPaymentConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates a split payment session.
    /// </summary>
    Task<SplitPaymentSession> InitiateSplitPaymentAsync(InitiateSplitPaymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a split payment session.
    /// </summary>
    Task<SplitPaymentSession?> GetSplitPaymentSessionAsync(int sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active split payment sessions for a store.
    /// </summary>
    Task<IEnumerable<SplitPaymentSession>> GetActiveSplitSessionsAsync(int storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a payment for a split part.
    /// </summary>
    Task<SplitPaymentPart> ProcessSplitPartPaymentAsync(ProcessSplitPartRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates equal split amounts.
    /// </summary>
    Task<EqualSplitResult> CalculateEqualSplitAsync(decimal totalAmount, int numberOfSplits, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates item-based split.
    /// </summary>
    Task<ItemSplitResult> CalculateItemSplitAsync(int receiptId, IEnumerable<ItemSplitAssignment> assignments, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a split payment session.
    /// </summary>
    Task CancelSplitSessionAsync(int sessionId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes a split payment session.
    /// </summary>
    Task<SplitPaymentSession> CompleteSplitSessionAsync(int sessionId, CancellationToken cancellationToken = default);

    #endregion

    #region Quick Amount Buttons

    /// <summary>
    /// Gets quick amount buttons for a store.
    /// </summary>
    Task<IEnumerable<QuickAmountButton>> GetQuickAmountButtonsAsync(int storeId, string? paymentMethod = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a quick amount button.
    /// </summary>
    Task<QuickAmountButton> SaveQuickAmountButtonAsync(QuickAmountButton button, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a quick amount button.
    /// </summary>
    Task DeleteQuickAmountButtonAsync(int buttonId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets quick amount button sets.
    /// </summary>
    Task<IEnumerable<QuickAmountButtonSet>> GetQuickAmountButtonSetsAsync(int storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a quick amount button set.
    /// </summary>
    Task<QuickAmountButtonSet> SaveQuickAmountButtonSetAsync(QuickAmountButtonSet buttonSet, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates default quick amount buttons based on common denominations.
    /// </summary>
    Task<IEnumerable<QuickAmountButton>> GenerateDefaultQuickAmountButtonsAsync(int storeId, string currency, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates round-up amount.
    /// </summary>
    Task<decimal> CalculateRoundUpAmountAsync(decimal amount, decimal roundTo, CancellationToken cancellationToken = default);

    #endregion
}

#region DTOs

/// <summary>
/// Request to park a transaction.
/// </summary>
public class ParkTransactionRequest
{
    public int StoreId { get; set; }
    public int? TerminalId { get; set; }
    public int ParkedByUserId { get; set; }
    public string? CustomerName { get; set; }
    public string? TableNumber { get; set; }
    public string? OrderType { get; set; }
    public string? Notes { get; set; }
    public int? LoyaltyMemberId { get; set; }
    public string? AppliedPromotionIds { get; set; }
    public int? ExpirationMinutes { get; set; }
    public List<ParkTransactionItem> Items { get; set; } = new();
}

/// <summary>
/// Item to park.
/// </summary>
public class ParkTransactionItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public string? Notes { get; set; }
    public string? ModifiersJson { get; set; }
}

/// <summary>
/// Result of recalling a transaction.
/// </summary>
public class RecallTransactionResult
{
    public bool Success { get; set; }
    public SuspendedTransaction? Transaction { get; set; }
    public string? Message { get; set; }
    public List<RecallTransactionItem> Items { get; set; } = new();
}

/// <summary>
/// Item from recalled transaction.
/// </summary>
public class RecallTransactionItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public string? Notes { get; set; }
    public string? ModifiersJson { get; set; }
    public bool IsAvailable { get; set; } = true;
    public decimal? CurrentPrice { get; set; }
}

/// <summary>
/// Search request for suspended transactions.
/// </summary>
public class SuspendedTransactionSearchRequest
{
    public int StoreId { get; set; }
    public int? TerminalId { get; set; }
    public string? CustomerName { get; set; }
    public string? TableNumber { get; set; }
    public string? ReferenceNumber { get; set; }
    public SuspendedTransactionStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? ParkedByUserId { get; set; }
}

/// <summary>
/// Current state for customer-facing display.
/// </summary>
public class CustomerDisplayState
{
    public string State { get; set; } = "Idle"; // Idle, Transaction, Payment, Complete
    public string? WelcomeMessage { get; set; }
    public string? CurrentMessage { get; set; }
    public List<CustomerDisplayItem> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? PaymentMethod { get; set; }
    public decimal? AmountTendered { get; set; }
    public decimal? ChangeAmount { get; set; }
    public string? ThankYouMessage { get; set; }
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Item for customer display.
/// </summary>
public class CustomerDisplayItem
{
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public string? ImageUrl { get; set; }
}

/// <summary>
/// Request to initiate split payment.
/// </summary>
public class InitiateSplitPaymentRequest
{
    public int ReceiptId { get; set; }
    public int InitiatedByUserId { get; set; }
    public SplitPaymentMethodType SplitMethod { get; set; }
    public int NumberOfSplits { get; set; }
    public List<SplitPartDefinition>? Parts { get; set; }
}

/// <summary>
/// Definition of a split part.
/// </summary>
public class SplitPartDefinition
{
    public int PartNumber { get; set; }
    public string? PayerName { get; set; }
    public decimal? Amount { get; set; }
    public decimal? Percentage { get; set; }
    public List<int>? ItemIds { get; set; }
}

/// <summary>
/// Request to process split part payment.
/// </summary>
public class ProcessSplitPartRequest
{
    public int SplitSessionId { get; set; }
    public int PartNumber { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? PaymentReference { get; set; }
    public decimal Amount { get; set; }
}

/// <summary>
/// Result of equal split calculation.
/// </summary>
public class EqualSplitResult
{
    public decimal TotalAmount { get; set; }
    public int NumberOfSplits { get; set; }
    public decimal AmountPerSplit { get; set; }
    public decimal Remainder { get; set; }
    public List<decimal> SplitAmounts { get; set; } = new();
}

/// <summary>
/// Item split assignment.
/// </summary>
public class ItemSplitAssignment
{
    public int ItemId { get; set; }
    public int PartNumber { get; set; }
}

/// <summary>
/// Result of item-based split calculation.
/// </summary>
public class ItemSplitResult
{
    public int ReceiptId { get; set; }
    public List<ItemSplitPart> Parts { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public bool IsValid { get; set; }
    public string? ValidationMessage { get; set; }
}

/// <summary>
/// Part in item-based split.
/// </summary>
public class ItemSplitPart
{
    public int PartNumber { get; set; }
    public List<int> ItemIds { get; set; } = new();
    public decimal Amount { get; set; }
    public List<ItemSplitDetail> Items { get; set; } = new();
}

/// <summary>
/// Item detail in split.
/// </summary>
public class ItemSplitDetail
{
    public int ItemId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
}

#endregion
