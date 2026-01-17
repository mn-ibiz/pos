using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for managing mobile money payments (Airtel Money, T-Kash).
/// </summary>
public interface IMobileMoneyService
{
    #region Airtel Money Configuration

    /// <summary>
    /// Gets Airtel Money configuration.
    /// </summary>
    Task<AirtelMoneyConfiguration?> GetAirtelMoneyConfigurationAsync(int? storeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves Airtel Money configuration.
    /// </summary>
    Task<AirtelMoneyConfiguration> SaveAirtelMoneyConfigurationAsync(AirtelMoneyConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests Airtel Money connection.
    /// </summary>
    Task<MobileMoneyConnectionTestResult> TestAirtelMoneyConnectionAsync(int configurationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables or disables Airtel Money.
    /// </summary>
    Task SetAirtelMoneyEnabledAsync(int configurationId, bool enabled, CancellationToken cancellationToken = default);

    #endregion

    #region Airtel Money Payments

    /// <summary>
    /// Initiates an Airtel Money payment.
    /// </summary>
    Task<MobileMoneyPaymentResult> InitiateAirtelMoneyPaymentAsync(MobileMoneyPaymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks the status of an Airtel Money payment.
    /// </summary>
    Task<MobileMoneyPaymentStatus> CheckAirtelMoneyPaymentStatusAsync(string transactionReference, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes Airtel Money callback.
    /// </summary>
    Task<MobileMoneyCallbackResult> ProcessAirtelMoneyCallbackAsync(string callbackData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets Airtel Money transaction by reference.
    /// </summary>
    Task<AirtelMoneyRequest?> GetAirtelMoneyTransactionAsync(string transactionReference, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets Airtel Money transactions for a date range.
    /// </summary>
    Task<IEnumerable<AirtelMoneyRequest>> GetAirtelMoneyTransactionsAsync(int storeId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    #endregion

    #region T-Kash Configuration

    /// <summary>
    /// Gets T-Kash configuration.
    /// </summary>
    Task<TKashConfiguration?> GetTKashConfigurationAsync(int? storeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves T-Kash configuration.
    /// </summary>
    Task<TKashConfiguration> SaveTKashConfigurationAsync(TKashConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests T-Kash connection.
    /// </summary>
    Task<MobileMoneyConnectionTestResult> TestTKashConnectionAsync(int configurationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables or disables T-Kash.
    /// </summary>
    Task SetTKashEnabledAsync(int configurationId, bool enabled, CancellationToken cancellationToken = default);

    #endregion

    #region T-Kash Payments

    /// <summary>
    /// Initiates a T-Kash payment.
    /// </summary>
    Task<MobileMoneyPaymentResult> InitiateTKashPaymentAsync(MobileMoneyPaymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks the status of a T-Kash payment.
    /// </summary>
    Task<MobileMoneyPaymentStatus> CheckTKashPaymentStatusAsync(string transactionReference, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes T-Kash callback.
    /// </summary>
    Task<MobileMoneyCallbackResult> ProcessTKashCallbackAsync(string callbackData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets T-Kash transaction by reference.
    /// </summary>
    Task<TKashRequest?> GetTKashTransactionAsync(string transactionReference, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets T-Kash transactions for a date range.
    /// </summary>
    Task<IEnumerable<TKashRequest>> GetTKashTransactionsAsync(int storeId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    #endregion

    #region Common Operations

    /// <summary>
    /// Validates a phone number for a specific provider.
    /// </summary>
    Task<PhoneValidationResult> ValidatePhoneNumberAsync(string phoneNumber, MobileMoneyProvider provider, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available mobile money providers for a store.
    /// </summary>
    Task<IEnumerable<MobileMoneyProviderInfo>> GetAvailableProvidersAsync(int? storeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets transaction logs.
    /// </summary>
    Task<IEnumerable<MobileMoneyTransactionLog>> GetTransactionLogsAsync(int? storeId = null, MobileMoneyProvider? provider = null, DateTime? startDate = null, DateTime? endDate = null, int limit = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets mobile money reconciliation report.
    /// </summary>
    Task<MobileMoneyReconciliationReport> GetReconciliationReportAsync(int storeId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retries a failed payment.
    /// </summary>
    Task<MobileMoneyPaymentResult> RetryPaymentAsync(string transactionReference, MobileMoneyProvider provider, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a pending payment.
    /// </summary>
    Task<bool> CancelPaymentAsync(string transactionReference, MobileMoneyProvider provider, CancellationToken cancellationToken = default);

    #endregion
}

#region DTOs

/// <summary>
/// Request to initiate a mobile money payment.
/// </summary>
public class MobileMoneyPaymentRequest
{
    public int StoreId { get; set; }
    public int? ReceiptId { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "KES";
    public string? Description { get; set; }
    public int? UserId { get; set; }
}

/// <summary>
/// Result of a mobile money payment initiation.
/// </summary>
public class MobileMoneyPaymentResult
{
    public bool IsSuccess { get; set; }
    public string TransactionReference { get; set; } = string.Empty;
    public string? ProviderTransactionId { get; set; }
    public MobileMoneyTransactionStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public DateTime RequestedAt { get; set; }
    public int TimeoutSeconds { get; set; }
}

/// <summary>
/// Status of a mobile money payment.
/// </summary>
public class MobileMoneyPaymentStatus
{
    public string TransactionReference { get; set; } = string.Empty;
    public string? ProviderTransactionId { get; set; }
    public MobileMoneyTransactionStatus Status { get; set; }
    public decimal Amount { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public bool IsFinal { get; set; }
}

/// <summary>
/// Result of processing a mobile money callback.
/// </summary>
public class MobileMoneyCallbackResult
{
    public bool IsSuccess { get; set; }
    public string TransactionReference { get; set; } = string.Empty;
    public MobileMoneyTransactionStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public int? ReceiptId { get; set; }
}

/// <summary>
/// Result of a mobile money connection test.
/// </summary>
public class MobileMoneyConnectionTestResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime TestedAt { get; set; }
    public int ResponseTimeMs { get; set; }
    public string? ApiVersion { get; set; }
}

/// <summary>
/// Result of phone number validation.
/// </summary>
public class PhoneValidationResult
{
    public bool IsValid { get; set; }
    public string? NormalizedNumber { get; set; }
    public MobileMoneyProvider? DetectedProvider { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Information about a mobile money provider.
/// </summary>
public class MobileMoneyProviderInfo
{
    public MobileMoneyProvider Provider { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsConfigured { get; set; }
    public string? PhonePrefix { get; set; }
}

/// <summary>
/// Mobile money reconciliation report.
/// </summary>
public class MobileMoneyReconciliationReport
{
    public int StoreId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public MobileMoneyProviderSummary MPesaSummary { get; set; } = new();
    public MobileMoneyProviderSummary AirtelMoneySummary { get; set; } = new();
    public MobileMoneyProviderSummary TKashSummary { get; set; } = new();

    public decimal TotalAmount => MPesaSummary.TotalAmount + AirtelMoneySummary.TotalAmount + TKashSummary.TotalAmount;
    public int TotalTransactions => MPesaSummary.TotalTransactions + AirtelMoneySummary.TotalTransactions + TKashSummary.TotalTransactions;
}

/// <summary>
/// Summary for a specific mobile money provider.
/// </summary>
public class MobileMoneyProviderSummary
{
    public MobileMoneyProvider Provider { get; set; }
    public int TotalTransactions { get; set; }
    public int SuccessfulTransactions { get; set; }
    public int FailedTransactions { get; set; }
    public int PendingTransactions { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal SuccessfulAmount { get; set; }
    public decimal AverageTransactionAmount { get; set; }
    public double SuccessRate => TotalTransactions > 0 ? (double)SuccessfulTransactions / TotalTransactions * 100 : 0;
}

#endregion
