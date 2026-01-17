using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for M-Pesa Daraja API operations.
/// </summary>
public interface IMpesaService
{
    // Configuration
    Task<MpesaConfiguration> SaveConfigurationAsync(MpesaConfiguration config, CancellationToken cancellationToken = default);
    Task<MpesaConfiguration?> GetActiveConfigurationAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MpesaConfiguration>> GetAllConfigurationsAsync(CancellationToken cancellationToken = default);
    Task<bool> ActivateConfigurationAsync(int configId, CancellationToken cancellationToken = default);
    Task<bool> TestConfigurationAsync(int configId, CancellationToken cancellationToken = default);

    // STK Push
    Task<MpesaStkPushResult> InitiateStkPushAsync(string phoneNumber, decimal amount, string accountReference, string description, int? receiptId = null, CancellationToken cancellationToken = default);
    Task<MpesaStkPushRequest?> GetStkPushRequestAsync(int requestId, CancellationToken cancellationToken = default);
    Task<MpesaStkPushRequest?> GetStkPushByCheckoutIdAsync(string checkoutRequestId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MpesaStkPushRequest>> GetPendingStkRequestsAsync(CancellationToken cancellationToken = default);

    // Callback Processing
    Task ProcessStkCallbackAsync(string callbackJson, CancellationToken cancellationToken = default);

    // Transaction Status Query
    Task<MpesaQueryResult> QueryTransactionStatusAsync(string checkoutRequestId, CancellationToken cancellationToken = default);
    Task QueryPendingTransactionsAsync(CancellationToken cancellationToken = default);

    // Manual Entry
    Task<MpesaTransaction> RecordManualTransactionAsync(string mpesaReceiptNumber, decimal amount, string phoneNumber, DateTime transactionDate, string? notes, int userId, CancellationToken cancellationToken = default);
    Task<bool> VerifyTransactionAsync(int transactionId, int verifiedByUserId, CancellationToken cancellationToken = default);

    // Transaction History
    Task<MpesaTransaction?> GetTransactionByReceiptNumberAsync(string mpesaReceiptNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MpesaTransaction>> GetTransactionsByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MpesaTransaction>> GetUnverifiedTransactionsAsync(CancellationToken cancellationToken = default);

    // Validation
    Task<bool> ValidatePhoneNumberAsync(string phoneNumber);
    string FormatPhoneNumber(string phoneNumber);

    // Dashboard
    Task<MpesaDashboardData> GetDashboardDataAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of STK Push initiation.
/// </summary>
public class MpesaStkPushResult
{
    public bool Success { get; set; }
    public int? RequestId { get; set; }
    public string? MerchantRequestId { get; set; }
    public string? CheckoutRequestId { get; set; }
    public string? ResponseCode { get; set; }
    public string? ResponseDescription { get; set; }
    public string? CustomerMessage { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Result of transaction status query.
/// </summary>
public class MpesaQueryResult
{
    public bool Success { get; set; }
    public MpesaStkStatus Status { get; set; }
    public string? ResultCode { get; set; }
    public string? ResultDescription { get; set; }
    public string? MpesaReceiptNumber { get; set; }
    public DateTime? TransactionDate { get; set; }
    public string? PhoneNumber { get; set; }
    public decimal? Amount { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// M-Pesa dashboard statistics.
/// </summary>
public class MpesaDashboardData
{
    public bool IsConfigured { get; set; }
    public bool IsTestMode { get; set; }
    public string? ShortCode { get; set; }
    public DateTime? LastSuccessfulTransaction { get; set; }

    public int TodayTransactions { get; set; }
    public decimal TodayAmount { get; set; }
    public int TodayPending { get; set; }
    public int TodayFailed { get; set; }

    public int MonthTransactions { get; set; }
    public decimal MonthAmount { get; set; }

    public int UnverifiedManualEntries { get; set; }

    public List<MpesaHourlyStats> TodayHourlyStats { get; set; } = [];
}

/// <summary>
/// Hourly M-Pesa statistics.
/// </summary>
public class MpesaHourlyStats
{
    public int Hour { get; set; }
    public int TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
}
