namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Type of mobile money provider.
/// </summary>
public enum MobileMoneyProvider
{
    /// <summary>Safaricom M-Pesa.</summary>
    MPesa = 1,
    /// <summary>Airtel Money.</summary>
    AirtelMoney = 2,
    /// <summary>Telkom T-Kash.</summary>
    TKash = 3
}

/// <summary>
/// Status of a mobile money transaction.
/// </summary>
public enum MobileMoneyTransactionStatus
{
    /// <summary>Transaction initiated, awaiting customer confirmation.</summary>
    Pending = 1,
    /// <summary>Transaction completed successfully.</summary>
    Completed = 2,
    /// <summary>Transaction failed.</summary>
    Failed = 3,
    /// <summary>Transaction timed out.</summary>
    TimedOut = 4,
    /// <summary>Transaction cancelled by user.</summary>
    Cancelled = 5,
    /// <summary>Transaction reversed/refunded.</summary>
    Reversed = 6
}

/// <summary>
/// Airtel Money configuration.
/// </summary>
public class AirtelMoneyConfiguration : BaseEntity
{
    /// <summary>
    /// Store this configuration belongs to.
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// Merchant code assigned by Airtel.
    /// </summary>
    public string MerchantCode { get; set; } = string.Empty;

    /// <summary>
    /// Client ID for API authentication.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client secret (encrypted).
    /// </summary>
    public string ClientSecretEncrypted { get; set; } = string.Empty;

    /// <summary>
    /// API base URL.
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://openapiuat.airtel.africa";

    /// <summary>
    /// Callback URL for payment notifications.
    /// </summary>
    public string CallbackUrl { get; set; } = string.Empty;

    /// <summary>
    /// Country code (e.g., KE for Kenya).
    /// </summary>
    public string CountryCode { get; set; } = "KE";

    /// <summary>
    /// Currency code.
    /// </summary>
    public string CurrencyCode { get; set; } = "KES";

    /// <summary>
    /// Environment (sandbox/production).
    /// </summary>
    public string Environment { get; set; } = "sandbox";

    /// <summary>
    /// Whether this configuration is active.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Transaction timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Last connection test result.
    /// </summary>
    public bool? LastTestSuccessful { get; set; }

    /// <summary>
    /// Last connection test timestamp.
    /// </summary>
    public DateTime? LastTestedAt { get; set; }

    /// <summary>
    /// Last test error message.
    /// </summary>
    public string? LastTestError { get; set; }

    // Navigation properties
    public virtual Store? Store { get; set; }
}

/// <summary>
/// Airtel Money transaction request.
/// </summary>
public class AirtelMoneyRequest : BaseEntity
{
    /// <summary>
    /// Reference to the receipt being paid.
    /// </summary>
    public int? ReceiptId { get; set; }

    /// <summary>
    /// Store ID where transaction occurred.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Internal transaction reference.
    /// </summary>
    public string TransactionReference { get; set; } = string.Empty;

    /// <summary>
    /// Airtel transaction ID.
    /// </summary>
    public string? AirtelTransactionId { get; set; }

    /// <summary>
    /// Customer phone number.
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Transaction amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency code.
    /// </summary>
    public string CurrencyCode { get; set; } = "KES";

    /// <summary>
    /// Transaction status.
    /// </summary>
    public MobileMoneyTransactionStatus Status { get; set; } = MobileMoneyTransactionStatus.Pending;

    /// <summary>
    /// Request timestamp.
    /// </summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Completion timestamp.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Raw API request.
    /// </summary>
    public string? RawRequest { get; set; }

    /// <summary>
    /// Raw API response.
    /// </summary>
    public string? RawResponse { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Error code if failed.
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Callback data received.
    /// </summary>
    public string? CallbackData { get; set; }

    /// <summary>
    /// Callback received timestamp.
    /// </summary>
    public DateTime? CallbackReceivedAt { get; set; }

    /// <summary>
    /// Number of status check attempts.
    /// </summary>
    public int StatusCheckAttempts { get; set; }

    /// <summary>
    /// User who initiated the request.
    /// </summary>
    public int? UserId { get; set; }

    // Navigation properties
    public virtual Receipt? Receipt { get; set; }
    public virtual Store Store { get; set; } = null!;
    public virtual User? User { get; set; }
}

/// <summary>
/// T-Kash (Telkom Kenya) configuration.
/// </summary>
public class TKashConfiguration : BaseEntity
{
    /// <summary>
    /// Store this configuration belongs to.
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// Merchant ID assigned by Telkom.
    /// </summary>
    public string MerchantId { get; set; } = string.Empty;

    /// <summary>
    /// API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// API secret (encrypted).
    /// </summary>
    public string ApiSecretEncrypted { get; set; } = string.Empty;

    /// <summary>
    /// API base URL.
    /// </summary>
    public string ApiBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Callback URL for payment notifications.
    /// </summary>
    public string CallbackUrl { get; set; } = string.Empty;

    /// <summary>
    /// Environment (sandbox/production).
    /// </summary>
    public string Environment { get; set; } = "sandbox";

    /// <summary>
    /// Whether this configuration is active.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Transaction timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Last connection test result.
    /// </summary>
    public bool? LastTestSuccessful { get; set; }

    /// <summary>
    /// Last connection test timestamp.
    /// </summary>
    public DateTime? LastTestedAt { get; set; }

    /// <summary>
    /// Last test error message.
    /// </summary>
    public string? LastTestError { get; set; }

    // Navigation properties
    public virtual Store? Store { get; set; }
}

/// <summary>
/// T-Kash transaction request.
/// </summary>
public class TKashRequest : BaseEntity
{
    /// <summary>
    /// Reference to the receipt being paid.
    /// </summary>
    public int? ReceiptId { get; set; }

    /// <summary>
    /// Store ID where transaction occurred.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Internal transaction reference.
    /// </summary>
    public string TransactionReference { get; set; } = string.Empty;

    /// <summary>
    /// T-Kash transaction ID.
    /// </summary>
    public string? TKashTransactionId { get; set; }

    /// <summary>
    /// Customer phone number.
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Transaction amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency code.
    /// </summary>
    public string CurrencyCode { get; set; } = "KES";

    /// <summary>
    /// Transaction status.
    /// </summary>
    public MobileMoneyTransactionStatus Status { get; set; } = MobileMoneyTransactionStatus.Pending;

    /// <summary>
    /// Request timestamp.
    /// </summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Completion timestamp.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Raw API request.
    /// </summary>
    public string? RawRequest { get; set; }

    /// <summary>
    /// Raw API response.
    /// </summary>
    public string? RawResponse { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Error code if failed.
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Callback data received.
    /// </summary>
    public string? CallbackData { get; set; }

    /// <summary>
    /// Callback received timestamp.
    /// </summary>
    public DateTime? CallbackReceivedAt { get; set; }

    /// <summary>
    /// Number of status check attempts.
    /// </summary>
    public int StatusCheckAttempts { get; set; }

    /// <summary>
    /// User who initiated the request.
    /// </summary>
    public int? UserId { get; set; }

    // Navigation properties
    public virtual Receipt? Receipt { get; set; }
    public virtual Store Store { get; set; } = null!;
    public virtual User? User { get; set; }
}

/// <summary>
/// Mobile money transaction log for all providers.
/// </summary>
public class MobileMoneyTransactionLog : BaseEntity
{
    /// <summary>
    /// Mobile money provider.
    /// </summary>
    public MobileMoneyProvider Provider { get; set; }

    /// <summary>
    /// Reference to provider-specific request.
    /// </summary>
    public int? RequestId { get; set; }

    /// <summary>
    /// Store ID.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Internal transaction reference.
    /// </summary>
    public string TransactionReference { get; set; } = string.Empty;

    /// <summary>
    /// Provider transaction ID.
    /// </summary>
    public string? ProviderTransactionId { get; set; }

    /// <summary>
    /// Customer phone number.
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Transaction amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency code.
    /// </summary>
    public string CurrencyCode { get; set; } = "KES";

    /// <summary>
    /// Transaction status.
    /// </summary>
    public MobileMoneyTransactionStatus Status { get; set; }

    /// <summary>
    /// Log entry type.
    /// </summary>
    public string EntryType { get; set; } = string.Empty;

    /// <summary>
    /// Message or details.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Timestamp.
    /// </summary>
    public DateTime LoggedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Store Store { get; set; } = null!;
}
