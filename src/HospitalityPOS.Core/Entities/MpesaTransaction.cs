using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// M-Pesa STK Push request record.
/// </summary>
public class MpesaStkPushRequest : BaseEntity
{
    /// <summary>
    /// Related payment ID.
    /// </summary>
    public int? PaymentId { get; set; }

    /// <summary>
    /// Related receipt ID.
    /// </summary>
    public int? ReceiptId { get; set; }

    /// <summary>
    /// M-Pesa configuration used.
    /// </summary>
    public int ConfigurationId { get; set; }

    /// <summary>
    /// Merchant Request ID from Daraja.
    /// </summary>
    public string MerchantRequestId { get; set; } = string.Empty;

    /// <summary>
    /// Checkout Request ID from Daraja.
    /// </summary>
    public string CheckoutRequestId { get; set; } = string.Empty;

    /// <summary>
    /// Customer phone number (format: 254XXXXXXXXX).
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Amount to be paid.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Account reference.
    /// </summary>
    public string AccountReference { get; set; } = string.Empty;

    /// <summary>
    /// Transaction description.
    /// </summary>
    public string TransactionDescription { get; set; } = string.Empty;

    /// <summary>
    /// Request status.
    /// </summary>
    public MpesaStkStatus Status { get; set; } = MpesaStkStatus.Pending;

    /// <summary>
    /// Daraja response code.
    /// </summary>
    public string? ResponseCode { get; set; }

    /// <summary>
    /// Response description.
    /// </summary>
    public string? ResponseDescription { get; set; }

    /// <summary>
    /// Result code from callback.
    /// </summary>
    public string? ResultCode { get; set; }

    /// <summary>
    /// Result description from callback.
    /// </summary>
    public string? ResultDescription { get; set; }

    /// <summary>
    /// M-Pesa receipt number (on success).
    /// </summary>
    public string? MpesaReceiptNumber { get; set; }

    /// <summary>
    /// Transaction date from M-Pesa.
    /// </summary>
    public DateTime? TransactionDate { get; set; }

    /// <summary>
    /// Phone number that made payment (masked).
    /// </summary>
    public string? PhoneNumberUsed { get; set; }

    /// <summary>
    /// Request timestamp.
    /// </summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Callback received timestamp.
    /// </summary>
    public DateTime? CallbackReceivedAt { get; set; }

    /// <summary>
    /// Number of status query attempts.
    /// </summary>
    public int QueryAttempts { get; set; }

    /// <summary>
    /// Last query timestamp.
    /// </summary>
    public DateTime? LastQueryAt { get; set; }

    /// <summary>
    /// Raw request JSON.
    /// </summary>
    public string? RequestJson { get; set; }

    /// <summary>
    /// Raw response JSON.
    /// </summary>
    public string? ResponseJson { get; set; }

    /// <summary>
    /// Callback data JSON.
    /// </summary>
    public string? CallbackJson { get; set; }

    // Navigation properties
    public Payment? Payment { get; set; }
    public Receipt? Receipt { get; set; }
    public MpesaConfiguration Configuration { get; set; } = null!;
}

/// <summary>
/// M-Pesa transaction record for completed/manual transactions.
/// </summary>
public class MpesaTransaction : BaseEntity
{
    /// <summary>
    /// Related payment ID.
    /// </summary>
    public int? PaymentId { get; set; }

    /// <summary>
    /// Related STK push request ID (if initiated via STK).
    /// </summary>
    public int? StkPushRequestId { get; set; }

    /// <summary>
    /// M-Pesa confirmation code/receipt number.
    /// </summary>
    public string MpesaReceiptNumber { get; set; } = string.Empty;

    /// <summary>
    /// Transaction amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Phone number (masked).
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Transaction date/time.
    /// </summary>
    public DateTime TransactionDate { get; set; }

    /// <summary>
    /// Transaction status.
    /// </summary>
    public MpesaTransactionStatus Status { get; set; } = MpesaTransactionStatus.Completed;

    /// <summary>
    /// Whether this was a manual entry.
    /// </summary>
    public bool IsManualEntry { get; set; }

    /// <summary>
    /// User who recorded manual entry.
    /// </summary>
    public int? RecordedByUserId { get; set; }

    /// <summary>
    /// Notes (for manual entries).
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Verification status.
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// Verified by user ID.
    /// </summary>
    public int? VerifiedByUserId { get; set; }

    /// <summary>
    /// Verification timestamp.
    /// </summary>
    public DateTime? VerifiedAt { get; set; }

    // Navigation properties
    public Payment? Payment { get; set; }
    public MpesaStkPushRequest? StkPushRequest { get; set; }
    public User? RecordedByUser { get; set; }
    public User? VerifiedByUser { get; set; }
}
