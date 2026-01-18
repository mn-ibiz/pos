namespace HospitalityPOS.Core.DTOs;

/// <summary>
/// Request to initiate M-Pesa STK Push payment.
/// </summary>
public class MpesaSTKPushRequest
{
    /// <summary>
    /// Customer phone number in format 254XXXXXXXXX.
    /// </summary>
    public required string PhoneNumber { get; set; }

    /// <summary>
    /// Payment amount in KES.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Account reference (Receipt/Invoice number).
    /// </summary>
    public required string AccountReference { get; set; }

    /// <summary>
    /// Transaction description.
    /// </summary>
    public string Description { get; set; } = "Payment for goods";
}

/// <summary>
/// Response from M-Pesa STK Push initiation.
/// </summary>
public class MpesaSTKPushResponse
{
    /// <summary>
    /// Whether the STK push was initiated successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Merchant request ID from Safaricom.
    /// </summary>
    public string? MerchantRequestId { get; set; }

    /// <summary>
    /// Checkout request ID for querying status.
    /// </summary>
    public string? CheckoutRequestId { get; set; }

    /// <summary>
    /// Response code from Safaricom.
    /// </summary>
    public string? ResponseCode { get; set; }

    /// <summary>
    /// Response description.
    /// </summary>
    public string? ResponseDescription { get; set; }

    /// <summary>
    /// Customer message to display.
    /// </summary>
    public string? CustomerMessage { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Request to query M-Pesa transaction status.
/// </summary>
public class MpesaQueryRequest
{
    /// <summary>
    /// Checkout request ID from STK push response.
    /// </summary>
    public required string CheckoutRequestId { get; set; }
}

/// <summary>
/// Response from M-Pesa transaction status query.
/// </summary>
public class MpesaQueryResponse
{
    /// <summary>
    /// Whether the query was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Transaction status.
    /// </summary>
    public MpesaTransactionStatus Status { get; set; }

    /// <summary>
    /// Result code from Safaricom.
    /// </summary>
    public string? ResultCode { get; set; }

    /// <summary>
    /// Result description.
    /// </summary>
    public string? ResultDescription { get; set; }

    /// <summary>
    /// M-Pesa receipt number (if successful).
    /// </summary>
    public string? MpesaReceiptNumber { get; set; }

    /// <summary>
    /// Transaction date/time.
    /// </summary>
    public DateTime? TransactionDate { get; set; }

    /// <summary>
    /// Phone number that made the payment.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Amount paid.
    /// </summary>
    public decimal? Amount { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// M-Pesa callback data from Safaricom.
/// </summary>
public class MpesaCallbackData
{
    /// <summary>
    /// Merchant request ID.
    /// </summary>
    public string? MerchantRequestId { get; set; }

    /// <summary>
    /// Checkout request ID.
    /// </summary>
    public string? CheckoutRequestId { get; set; }

    /// <summary>
    /// Result code (0 = success).
    /// </summary>
    public int ResultCode { get; set; }

    /// <summary>
    /// Result description.
    /// </summary>
    public string? ResultDescription { get; set; }

    /// <summary>
    /// M-Pesa receipt number.
    /// </summary>
    public string? MpesaReceiptNumber { get; set; }

    /// <summary>
    /// Transaction date from M-Pesa.
    /// </summary>
    public DateTime? TransactionDate { get; set; }

    /// <summary>
    /// Phone number that made payment.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Amount paid.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Account reference used.
    /// </summary>
    public string? AccountReference { get; set; }
}

/// <summary>
/// M-Pesa transaction status.
/// </summary>
public enum MpesaTransactionStatus
{
    /// <summary>
    /// Request is pending, not yet sent.
    /// </summary>
    Pending,

    /// <summary>
    /// STK push sent to customer phone.
    /// </summary>
    Sent,

    /// <summary>
    /// Customer is entering PIN.
    /// </summary>
    Processing,

    /// <summary>
    /// Payment completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Payment failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Customer cancelled the request.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Request timed out.
    /// </summary>
    Timeout,

    /// <summary>
    /// Insufficient balance.
    /// </summary>
    InsufficientBalance,

    /// <summary>
    /// Wrong PIN entered.
    /// </summary>
    WrongPin,

    /// <summary>
    /// Transaction limit exceeded.
    /// </summary>
    LimitExceeded
}

/// <summary>
/// M-Pesa configuration settings.
/// </summary>
public class MpesaSettings
{
    /// <summary>
    /// Consumer key from Safaricom developer portal.
    /// </summary>
    public string ConsumerKey { get; set; } = string.Empty;

    /// <summary>
    /// Consumer secret from Safaricom developer portal.
    /// </summary>
    public string ConsumerSecret { get; set; } = string.Empty;

    /// <summary>
    /// Business short code (Paybill or Till number).
    /// </summary>
    public string BusinessShortCode { get; set; } = string.Empty;

    /// <summary>
    /// Passkey from Safaricom.
    /// </summary>
    public string Passkey { get; set; } = string.Empty;

    /// <summary>
    /// Transaction type: CustomerPayBillOnline or CustomerBuyGoodsOnline.
    /// </summary>
    public string TransactionType { get; set; } = "CustomerBuyGoodsOnline";

    /// <summary>
    /// Callback URL for payment notifications.
    /// </summary>
    public string CallbackUrl { get; set; } = string.Empty;

    /// <summary>
    /// Whether to use sandbox (test) environment.
    /// </summary>
    public bool UseSandbox { get; set; } = true;

    /// <summary>
    /// Sandbox API base URL.
    /// </summary>
    public string SandboxBaseUrl { get; set; } = "https://sandbox.safaricom.co.ke";

    /// <summary>
    /// Production API base URL.
    /// </summary>
    public string ProductionBaseUrl { get; set; } = "https://api.safaricom.co.ke";

    /// <summary>
    /// Gets the appropriate base URL based on environment.
    /// </summary>
    public string BaseUrl => UseSandbox ? SandboxBaseUrl : ProductionBaseUrl;

    /// <summary>
    /// Timeout in seconds for STK push.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Polling interval in seconds for status checks.
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 3;
}

/// <summary>
/// M-Pesa payment record for database storage.
/// </summary>
public class MpesaPaymentRecord
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Related receipt ID.
    /// </summary>
    public int? ReceiptId { get; set; }

    /// <summary>
    /// Related order ID.
    /// </summary>
    public int? OrderId { get; set; }

    /// <summary>
    /// Checkout request ID from Safaricom.
    /// </summary>
    public string? CheckoutRequestId { get; set; }

    /// <summary>
    /// Merchant request ID from Safaricom.
    /// </summary>
    public string? MerchantRequestId { get; set; }

    /// <summary>
    /// Customer phone number.
    /// </summary>
    public required string PhoneNumber { get; set; }

    /// <summary>
    /// Payment amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Account reference used.
    /// </summary>
    public string? AccountReference { get; set; }

    /// <summary>
    /// Transaction status.
    /// </summary>
    public MpesaTransactionStatus Status { get; set; }

    /// <summary>
    /// M-Pesa receipt number (on success).
    /// </summary>
    public string? MpesaReceiptNumber { get; set; }

    /// <summary>
    /// Result code from Safaricom.
    /// </summary>
    public string? ResultCode { get; set; }

    /// <summary>
    /// Result description.
    /// </summary>
    public string? ResultDescription { get; set; }

    /// <summary>
    /// When the request was initiated.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the status was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// When the transaction completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }
}
