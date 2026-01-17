namespace HospitalityPOS.Core.Models.Payments;

/// <summary>
/// QR payment provider types.
/// </summary>
public enum QrPaymentProvider
{
    /// <summary>M-Pesa Lipa Na M-Pesa QR.</summary>
    MpesaQr,
    /// <summary>PesaLink QR (future).</summary>
    PesaLinkQr,
    /// <summary>Bank-specific QR (future).</summary>
    BankQr
}

/// <summary>
/// Status of a QR payment request.
/// </summary>
public enum QrPaymentStatus
{
    /// <summary>QR generated, waiting for customer to scan.</summary>
    Pending,
    /// <summary>Customer has scanned the QR (if detectable).</summary>
    Scanned,
    /// <summary>Payment has been completed successfully.</summary>
    Completed,
    /// <summary>QR has expired without payment.</summary>
    Expired,
    /// <summary>Payment was cancelled by user.</summary>
    Cancelled,
    /// <summary>Payment failed.</summary>
    Failed
}

/// <summary>
/// Request to generate a QR code for payment.
/// </summary>
public class QrPaymentRequest
{
    /// <summary>Amount to pay.</summary>
    public decimal Amount { get; set; }

    /// <summary>Reference number (receipt number or transaction ID).</summary>
    public string Reference { get; set; } = string.Empty;

    /// <summary>Merchant name to display.</summary>
    public string MerchantName { get; set; } = string.Empty;

    /// <summary>QR provider to use.</summary>
    public QrPaymentProvider Provider { get; set; } = QrPaymentProvider.MpesaQr;

    /// <summary>Transaction type (BG=Buy Goods, PB=Paybill).</summary>
    public string TransactionType { get; set; } = "BG";

    /// <summary>Till number or Paybill number.</summary>
    public string MerchantCode { get; set; } = string.Empty;

    /// <summary>Account reference (for Paybill).</summary>
    public string? AccountReference { get; set; }

    /// <summary>Validity in seconds (default 5 minutes).</summary>
    public int ValiditySeconds { get; set; } = 300;
}

/// <summary>
/// Result of QR code generation.
/// </summary>
public class QrPaymentResult
{
    /// <summary>Whether QR generation was successful.</summary>
    public bool Success { get; set; }

    /// <summary>Error message if generation failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Unique identifier for this QR payment request.</summary>
    public string QrPaymentId { get; set; } = string.Empty;

    /// <summary>Reference used in the QR code.</summary>
    public string Reference { get; set; } = string.Empty;

    /// <summary>QR code image as Base64 string.</summary>
    public string? QrCodeBase64 { get; set; }

    /// <summary>QR code image as byte array.</summary>
    public byte[]? QrCodeBytes { get; set; }

    /// <summary>Raw QR code data (for debugging).</summary>
    public string? QrCodeData { get; set; }

    /// <summary>Amount in the QR code.</summary>
    public decimal Amount { get; set; }

    /// <summary>Provider used.</summary>
    public QrPaymentProvider Provider { get; set; }

    /// <summary>When the QR code expires.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>When the QR was generated.</summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Seconds remaining until expiry.</summary>
    public int SecondsRemaining => Math.Max(0, (int)(ExpiresAt - DateTime.UtcNow).TotalSeconds);

    /// <summary>Whether the QR is still valid.</summary>
    public bool IsValid => DateTime.UtcNow < ExpiresAt;

    /// <summary>Creates a failed result.</summary>
    public static QrPaymentResult Failure(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage
    };

    /// <summary>Creates a successful result.</summary>
    public static QrPaymentResult Successful(string qrPaymentId, string reference, decimal amount, byte[] qrCodeBytes, DateTime expiresAt) => new()
    {
        Success = true,
        QrPaymentId = qrPaymentId,
        Reference = reference,
        Amount = amount,
        QrCodeBytes = qrCodeBytes,
        QrCodeBase64 = Convert.ToBase64String(qrCodeBytes),
        ExpiresAt = expiresAt
    };
}

/// <summary>
/// Status check result for a QR payment.
/// </summary>
public class QrPaymentStatusResult
{
    /// <summary>The QR payment ID.</summary>
    public string QrPaymentId { get; set; } = string.Empty;

    /// <summary>Current status.</summary>
    public QrPaymentStatus Status { get; set; }

    /// <summary>Status message.</summary>
    public string? Message { get; set; }

    /// <summary>M-Pesa transaction ID if payment completed.</summary>
    public string? TransactionId { get; set; }

    /// <summary>M-Pesa receipt number if payment completed.</summary>
    public string? MpesaReceiptNumber { get; set; }

    /// <summary>Phone number that made the payment (masked).</summary>
    public string? PhoneNumber { get; set; }

    /// <summary>Amount paid.</summary>
    public decimal? AmountPaid { get; set; }

    /// <summary>When payment was completed.</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Whether this status indicates payment completion.</summary>
    public bool IsComplete => Status == QrPaymentStatus.Completed;

    /// <summary>Whether this status indicates a terminal state (no more polling needed).</summary>
    public bool IsTerminal => Status is QrPaymentStatus.Completed or
        QrPaymentStatus.Expired or QrPaymentStatus.Cancelled or QrPaymentStatus.Failed;
}

/// <summary>
/// M-Pesa QR code generation API request.
/// </summary>
public class MpesaQrGenerateRequest
{
    /// <summary>Merchant name.</summary>
    public string MerchantName { get; set; } = string.Empty;

    /// <summary>Reference number.</summary>
    public string RefNo { get; set; } = string.Empty;

    /// <summary>Amount to pay.</summary>
    public decimal Amount { get; set; }

    /// <summary>Transaction type code (BG=Buy Goods, PB=Paybill, SM=Send Money, SB=Send to Business, WA=Withdraw at Agent).</summary>
    public string TrxCode { get; set; } = "BG";

    /// <summary>Credit Party Identifier (Till Number for BG, Paybill for PB).</summary>
    public string CPI { get; set; } = string.Empty;

    /// <summary>Size of QR code (1-4).</summary>
    public int Size { get; set; } = 300;
}

/// <summary>
/// M-Pesa QR code generation API response.
/// </summary>
public class MpesaQrGenerateResponse
{
    /// <summary>Response code (00 = success).</summary>
    public string ResponseCode { get; set; } = string.Empty;

    /// <summary>Request ID.</summary>
    public string RequestID { get; set; } = string.Empty;

    /// <summary>Response description.</summary>
    public string ResponseDescription { get; set; } = string.Empty;

    /// <summary>Base64 encoded QR code image.</summary>
    public string QRCode { get; set; } = string.Empty;

    /// <summary>Whether the request was successful.</summary>
    public bool IsSuccess => ResponseCode == "00";
}

/// <summary>
/// QR payment entity for database storage.
/// </summary>
public class QrPaymentRequestEntity
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Related payment ID (if payment created).</summary>
    public int? PaymentId { get; set; }

    /// <summary>Receipt ID this QR is for.</summary>
    public int ReceiptId { get; set; }

    /// <summary>Unique QR reference.</summary>
    public string QrReference { get; set; } = string.Empty;

    /// <summary>External reference (M-Pesa request ID).</summary>
    public string? ExternalReference { get; set; }

    /// <summary>Amount to pay.</summary>
    public decimal Amount { get; set; }

    /// <summary>QR provider.</summary>
    public string Provider { get; set; } = "MpesaQr";

    /// <summary>Base64 QR code data.</summary>
    public string? QrCodeData { get; set; }

    /// <summary>Current status.</summary>
    public string Status { get; set; } = "Pending";

    /// <summary>When the QR expires.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>M-Pesa transaction ID if paid.</summary>
    public string? TransactionId { get; set; }

    /// <summary>M-Pesa receipt number if paid.</summary>
    public string? MpesaReceiptNumber { get; set; }

    /// <summary>When payment was completed.</summary>
    public DateTime? PaidAt { get; set; }

    /// <summary>When QR was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Last status check time.</summary>
    public DateTime? LastCheckedAt { get; set; }
}

/// <summary>
/// QR payment configuration settings.
/// </summary>
public class QrPaymentSettings
{
    /// <summary>Whether QR payments are enabled.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>M-Pesa Till Number.</summary>
    public string MpesaTillNumber { get; set; } = string.Empty;

    /// <summary>M-Pesa Paybill Number (alternative to Till).</summary>
    public string? MpesaPaybillNumber { get; set; }

    /// <summary>Merchant name to display on QR.</summary>
    public string MerchantName { get; set; } = string.Empty;

    /// <summary>Default validity in seconds.</summary>
    public int DefaultValiditySeconds { get; set; } = 300;

    /// <summary>Polling interval in milliseconds.</summary>
    public int PollingIntervalMs { get; set; } = 3000;

    /// <summary>Show QR on customer display.</summary>
    public bool ShowOnCustomerDisplay { get; set; } = true;

    /// <summary>Print QR on receipt.</summary>
    public bool PrintOnReceipt { get; set; } = false;
}

/// <summary>
/// QR payment metrics for reporting.
/// </summary>
public class QrPaymentMetrics
{
    /// <summary>Total QR payments attempted.</summary>
    public int TotalAttempts { get; set; }

    /// <summary>Successful QR payments.</summary>
    public int Successful { get; set; }

    /// <summary>Expired QR payments.</summary>
    public int Expired { get; set; }

    /// <summary>Cancelled QR payments.</summary>
    public int Cancelled { get; set; }

    /// <summary>Failed QR payments.</summary>
    public int Failed { get; set; }

    /// <summary>Success rate percentage.</summary>
    public decimal SuccessRate => TotalAttempts > 0
        ? Math.Round((decimal)Successful / TotalAttempts * 100, 2)
        : 0;

    /// <summary>Total amount collected via QR.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Average payment time in seconds.</summary>
    public decimal AveragePaymentTimeSeconds { get; set; }

    /// <summary>Reporting period start.</summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>Reporting period end.</summary>
    public DateTime PeriodEnd { get; set; }
}
