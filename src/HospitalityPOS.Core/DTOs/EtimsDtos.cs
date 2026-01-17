using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.DTOs;

#region Device Registration DTOs

/// <summary>
/// DTO for eTIMS device registration request.
/// </summary>
public class EtimsDeviceRegistrationDto
{
    /// <summary>
    /// Business KRA PIN.
    /// </summary>
    public string BusinessPin { get; set; } = string.Empty;

    /// <summary>
    /// Business name as registered with KRA.
    /// </summary>
    public string BusinessName { get; set; } = string.Empty;

    /// <summary>
    /// Branch code (001 for main branch).
    /// </summary>
    public string BranchCode { get; set; } = "001";

    /// <summary>
    /// Branch name.
    /// </summary>
    public string BranchName { get; set; } = "Main Branch";

    /// <summary>
    /// eTIMS API endpoint URL.
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://etims.kra.go.ke";

    /// <summary>
    /// API key for authentication.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// API secret for authentication.
    /// </summary>
    public string ApiSecret { get; set; } = string.Empty;

    /// <summary>
    /// Environment (Sandbox or Production).
    /// </summary>
    public string Environment { get; set; } = "Sandbox";
}

/// <summary>
/// DTO for device registration response from KRA.
/// </summary>
public class EtimsDeviceRegistrationResultDto
{
    /// <summary>
    /// Whether registration was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// KRA-assigned device serial number.
    /// </summary>
    public string? DeviceSerialNumber { get; set; }

    /// <summary>
    /// KRA-assigned Control Unit ID.
    /// </summary>
    public string? ControlUnitId { get; set; }

    /// <summary>
    /// Error message if registration failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Error code if registration failed.
    /// </summary>
    public string? ErrorCode { get; set; }
}

/// <summary>
/// DTO for device status information.
/// </summary>
public class EtimsDeviceStatusDto
{
    /// <summary>
    /// Device ID.
    /// </summary>
    public int DeviceId { get; set; }

    /// <summary>
    /// Device serial number.
    /// </summary>
    public string DeviceSerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// Control Unit ID.
    /// </summary>
    public string ControlUnitId { get; set; } = string.Empty;

    /// <summary>
    /// Current status.
    /// </summary>
    public EtimsDeviceStatus Status { get; set; }

    /// <summary>
    /// Last successful communication with eTIMS.
    /// </summary>
    public DateTime? LastCommunication { get; set; }

    /// <summary>
    /// Last invoice number issued.
    /// </summary>
    public int LastInvoiceNumber { get; set; }

    /// <summary>
    /// Last credit note number issued.
    /// </summary>
    public int LastCreditNoteNumber { get; set; }

    /// <summary>
    /// Whether this is the primary device.
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Environment (Sandbox/Production).
    /// </summary>
    public string Environment { get; set; } = string.Empty;
}

#endregion

#region Invoice Submission DTOs

/// <summary>
/// DTO for submitting an invoice to eTIMS.
/// </summary>
public class EtimsInvoiceSubmissionDto
{
    /// <summary>
    /// Receipt ID from POS system.
    /// </summary>
    public int ReceiptId { get; set; }

    /// <summary>
    /// Internal receipt number.
    /// </summary>
    public string InternalReceiptNumber { get; set; } = string.Empty;

    /// <summary>
    /// Invoice date and time.
    /// </summary>
    public DateTime InvoiceDate { get; set; }

    /// <summary>
    /// Document type.
    /// </summary>
    public EtimsDocumentType DocumentType { get; set; } = EtimsDocumentType.TaxInvoice;

    /// <summary>
    /// Customer type.
    /// </summary>
    public EtimsCustomerType CustomerType { get; set; } = EtimsCustomerType.Consumer;

    /// <summary>
    /// Customer KRA PIN (required for B2B).
    /// </summary>
    public string? CustomerPin { get; set; }

    /// <summary>
    /// Customer name.
    /// </summary>
    public string CustomerName { get; set; } = "Walk-in Customer";

    /// <summary>
    /// Customer phone number.
    /// </summary>
    public string? CustomerPhone { get; set; }

    /// <summary>
    /// Invoice line items.
    /// </summary>
    public List<EtimsInvoiceItemDto> Items { get; set; } = [];
}

/// <summary>
/// DTO for invoice line item.
/// </summary>
public class EtimsInvoiceItemDto
{
    /// <summary>
    /// Sequence number (1-based).
    /// </summary>
    public int SequenceNumber { get; set; }

    /// <summary>
    /// Product/item code.
    /// </summary>
    public string ItemCode { get; set; } = string.Empty;

    /// <summary>
    /// Item description.
    /// </summary>
    public string ItemDescription { get; set; } = string.Empty;

    /// <summary>
    /// HS Code (Harmonized System).
    /// </summary>
    public string? HsCode { get; set; }

    /// <summary>
    /// Unit of measure.
    /// </summary>
    public string UnitOfMeasure { get; set; } = "PCS";

    /// <summary>
    /// Quantity.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit price (inclusive of tax).
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Discount amount.
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Tax type code.
    /// </summary>
    public KraTaxType TaxType { get; set; } = KraTaxType.A;

    /// <summary>
    /// Tax rate percentage.
    /// </summary>
    public decimal TaxRate { get; set; } = 16m;
}

/// <summary>
/// DTO for invoice submission result.
/// </summary>
public class EtimsInvoiceSubmissionResultDto
{
    /// <summary>
    /// Whether submission was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// eTIMS invoice ID in local database.
    /// </summary>
    public int? EtimsInvoiceId { get; set; }

    /// <summary>
    /// KRA-assigned invoice number.
    /// </summary>
    public string? InvoiceNumber { get; set; }

    /// <summary>
    /// QR code data for receipt.
    /// </summary>
    public string? QrCode { get; set; }

    /// <summary>
    /// KRA receipt signature.
    /// </summary>
    public string? ReceiptSignature { get; set; }

    /// <summary>
    /// Submission status.
    /// </summary>
    public EtimsSubmissionStatus Status { get; set; }

    /// <summary>
    /// Error message if submission failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Error code if submission failed.
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Whether the invoice was queued for later submission.
    /// </summary>
    public bool WasQueued { get; set; }
}

#endregion

#region Credit Note DTOs

/// <summary>
/// DTO for submitting a credit note to eTIMS.
/// </summary>
public class EtimsCreditNoteSubmissionDto
{
    /// <summary>
    /// Receipt void ID (if from void operation).
    /// </summary>
    public int? ReceiptVoidId { get; set; }

    /// <summary>
    /// Original eTIMS invoice ID.
    /// </summary>
    public int OriginalInvoiceId { get; set; }

    /// <summary>
    /// Original invoice number (from KRA).
    /// </summary>
    public string OriginalInvoiceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Credit note date.
    /// </summary>
    public DateTime CreditNoteDate { get; set; }

    /// <summary>
    /// Reason for credit note.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Customer KRA PIN.
    /// </summary>
    public string? CustomerPin { get; set; }

    /// <summary>
    /// Customer name.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Credit note line items.
    /// </summary>
    public List<EtimsCreditNoteItemDto> Items { get; set; } = [];
}

/// <summary>
/// DTO for credit note line item.
/// </summary>
public class EtimsCreditNoteItemDto
{
    /// <summary>
    /// Sequence number.
    /// </summary>
    public int SequenceNumber { get; set; }

    /// <summary>
    /// Item code.
    /// </summary>
    public string ItemCode { get; set; } = string.Empty;

    /// <summary>
    /// Item description.
    /// </summary>
    public string ItemDescription { get; set; } = string.Empty;

    /// <summary>
    /// Quantity being credited.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit price.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Tax type.
    /// </summary>
    public KraTaxType TaxType { get; set; }

    /// <summary>
    /// Tax rate.
    /// </summary>
    public decimal TaxRate { get; set; }
}

/// <summary>
/// DTO for credit note submission result.
/// </summary>
public class EtimsCreditNoteSubmissionResultDto
{
    /// <summary>
    /// Whether submission was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Credit note ID in local database.
    /// </summary>
    public int? CreditNoteId { get; set; }

    /// <summary>
    /// KRA-assigned credit note number.
    /// </summary>
    public string? CreditNoteNumber { get; set; }

    /// <summary>
    /// KRA signature.
    /// </summary>
    public string? KraSignature { get; set; }

    /// <summary>
    /// Submission status.
    /// </summary>
    public EtimsSubmissionStatus Status { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Error code if failed.
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Whether the credit note was queued.
    /// </summary>
    public bool WasQueued { get; set; }
}

#endregion

#region Queue and Status DTOs

/// <summary>
/// DTO for eTIMS queue entry.
/// </summary>
public class EtimsQueueEntryDto
{
    /// <summary>
    /// Queue entry ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Document type.
    /// </summary>
    public EtimsDocumentType DocumentType { get; set; }

    /// <summary>
    /// Document ID.
    /// </summary>
    public int DocumentId { get; set; }

    /// <summary>
    /// Priority (lower = higher priority).
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// When queued.
    /// </summary>
    public DateTime QueuedAt { get; set; }

    /// <summary>
    /// When to retry.
    /// </summary>
    public DateTime? RetryAfter { get; set; }

    /// <summary>
    /// Number of attempts.
    /// </summary>
    public int Attempts { get; set; }

    /// <summary>
    /// Maximum attempts.
    /// </summary>
    public int MaxAttempts { get; set; }

    /// <summary>
    /// Current status.
    /// </summary>
    public EtimsSubmissionStatus Status { get; set; }

    /// <summary>
    /// Last error message.
    /// </summary>
    public string? LastError { get; set; }
}

/// <summary>
/// DTO for eTIMS dashboard/status summary.
/// </summary>
public class EtimsDashboardDto
{
    /// <summary>
    /// Device status information.
    /// </summary>
    public EtimsDeviceStatusDto? Device { get; set; }

    /// <summary>
    /// Total invoices submitted today.
    /// </summary>
    public int InvoicesSubmittedToday { get; set; }

    /// <summary>
    /// Total invoices pending submission.
    /// </summary>
    public int InvoicesPending { get; set; }

    /// <summary>
    /// Total invoices failed.
    /// </summary>
    public int InvoicesFailed { get; set; }

    /// <summary>
    /// Total credit notes submitted today.
    /// </summary>
    public int CreditNotesSubmittedToday { get; set; }

    /// <summary>
    /// Total credit notes pending.
    /// </summary>
    public int CreditNotesPending { get; set; }

    /// <summary>
    /// Total credit notes failed.
    /// </summary>
    public int CreditNotesFailed { get; set; }

    /// <summary>
    /// Queue entries awaiting processing.
    /// </summary>
    public int QueuedItems { get; set; }

    /// <summary>
    /// Invoices older than 48 hours not submitted (compliance alert).
    /// </summary>
    public int OverdueInvoices { get; set; }

    /// <summary>
    /// Last successful sync time.
    /// </summary>
    public DateTime? LastSuccessfulSync { get; set; }

    /// <summary>
    /// Whether the device is online and communicating.
    /// </summary>
    public bool IsOnline { get; set; }

    /// <summary>
    /// Recent failed submissions for review.
    /// </summary>
    public List<EtimsQueueEntryDto> RecentFailures { get; set; } = [];
}

/// <summary>
/// DTO for invoice status query.
/// </summary>
public class EtimsInvoiceStatusDto
{
    /// <summary>
    /// Invoice ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Receipt ID.
    /// </summary>
    public int ReceiptId { get; set; }

    /// <summary>
    /// Invoice number.
    /// </summary>
    public string InvoiceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Invoice date.
    /// </summary>
    public DateTime InvoiceDate { get; set; }

    /// <summary>
    /// Total amount.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Submission status.
    /// </summary>
    public EtimsSubmissionStatus Status { get; set; }

    /// <summary>
    /// Submission attempts.
    /// </summary>
    public int SubmissionAttempts { get; set; }

    /// <summary>
    /// Last submission attempt time.
    /// </summary>
    public DateTime? LastSubmissionAttempt { get; set; }

    /// <summary>
    /// When successfully submitted.
    /// </summary>
    public DateTime? SubmittedAt { get; set; }

    /// <summary>
    /// QR code for verification.
    /// </summary>
    public string? QrCode { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

#endregion

#region Batch Submission DTOs

/// <summary>
/// DTO for VSCU batch submission request.
/// </summary>
public class EtimsBatchSubmissionDto
{
    /// <summary>
    /// Device ID to use for submission.
    /// </summary>
    public int DeviceId { get; set; }

    /// <summary>
    /// Invoice IDs to submit in batch.
    /// </summary>
    public List<int> InvoiceIds { get; set; } = [];

    /// <summary>
    /// Credit note IDs to submit in batch.
    /// </summary>
    public List<int> CreditNoteIds { get; set; } = [];
}

/// <summary>
/// DTO for batch submission result.
/// </summary>
public class EtimsBatchSubmissionResultDto
{
    /// <summary>
    /// Whether all submissions succeeded.
    /// </summary>
    public bool AllSucceeded { get; set; }

    /// <summary>
    /// Number of successful submissions.
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of failed submissions.
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Total items in batch.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Individual results.
    /// </summary>
    public List<EtimsBatchItemResultDto> Results { get; set; } = [];
}

/// <summary>
/// DTO for individual item result in batch submission.
/// </summary>
public class EtimsBatchItemResultDto
{
    /// <summary>
    /// Document type.
    /// </summary>
    public EtimsDocumentType DocumentType { get; set; }

    /// <summary>
    /// Document ID.
    /// </summary>
    public int DocumentId { get; set; }

    /// <summary>
    /// Whether submission succeeded.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Assigned number (invoice or credit note number).
    /// </summary>
    public string? AssignedNumber { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

#endregion

#region API Request/Response DTOs (KRA Format)

/// <summary>
/// KRA eTIMS API invoice request format.
/// </summary>
public class KraInvoiceRequest
{
    /// <summary>
    /// Control Unit ID.
    /// </summary>
    public string CuId { get; set; } = string.Empty;

    /// <summary>
    /// Business PIN.
    /// </summary>
    public string TaxPayerPin { get; set; } = string.Empty;

    /// <summary>
    /// Branch code.
    /// </summary>
    public string BranchId { get; set; } = string.Empty;

    /// <summary>
    /// Invoice date (yyyyMMddHHmmss format).
    /// </summary>
    public string InvoiceDate { get; set; } = string.Empty;

    /// <summary>
    /// Customer type code.
    /// </summary>
    public string CustomerType { get; set; } = string.Empty;

    /// <summary>
    /// Customer PIN (if B2B).
    /// </summary>
    public string? CustomerPin { get; set; }

    /// <summary>
    /// Customer name.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Invoice items.
    /// </summary>
    public List<KraInvoiceItemRequest> Items { get; set; } = [];

    /// <summary>
    /// Taxable amount.
    /// </summary>
    public decimal TaxableAmount { get; set; }

    /// <summary>
    /// Tax amount.
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Total amount.
    /// </summary>
    public decimal TotalAmount { get; set; }
}

/// <summary>
/// KRA invoice item request format.
/// </summary>
public class KraInvoiceItemRequest
{
    /// <summary>
    /// Sequence number.
    /// </summary>
    public int SeqNo { get; set; }

    /// <summary>
    /// Item code.
    /// </summary>
    public string ItemCode { get; set; } = string.Empty;

    /// <summary>
    /// Item description.
    /// </summary>
    public string ItemDesc { get; set; } = string.Empty;

    /// <summary>
    /// HS code.
    /// </summary>
    public string? HsCode { get; set; }

    /// <summary>
    /// Quantity.
    /// </summary>
    public decimal Qty { get; set; }

    /// <summary>
    /// Unit price.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Discount amount.
    /// </summary>
    public decimal DiscAmt { get; set; }

    /// <summary>
    /// Tax type code.
    /// </summary>
    public string TaxType { get; set; } = string.Empty;

    /// <summary>
    /// Tax rate.
    /// </summary>
    public decimal TaxRate { get; set; }

    /// <summary>
    /// Taxable amount.
    /// </summary>
    public decimal TaxableAmt { get; set; }

    /// <summary>
    /// Tax amount.
    /// </summary>
    public decimal TaxAmt { get; set; }

    /// <summary>
    /// Total amount.
    /// </summary>
    public decimal TotAmt { get; set; }
}

/// <summary>
/// KRA eTIMS API response format.
/// </summary>
public class KraApiResponse
{
    /// <summary>
    /// Response code.
    /// </summary>
    public string ResultCd { get; set; } = string.Empty;

    /// <summary>
    /// Response message.
    /// </summary>
    public string ResultMsg { get; set; } = string.Empty;

    /// <summary>
    /// Response data.
    /// </summary>
    public KraInvoiceResponseData? Data { get; set; }
}

/// <summary>
/// KRA invoice response data.
/// </summary>
public class KraInvoiceResponseData
{
    /// <summary>
    /// Invoice number assigned by KRA.
    /// </summary>
    public string InvoiceNo { get; set; } = string.Empty;

    /// <summary>
    /// Receipt signature for QR code.
    /// </summary>
    public string RcptSign { get; set; } = string.Empty;

    /// <summary>
    /// Internal data for verification.
    /// </summary>
    public string IntrlData { get; set; } = string.Empty;

    /// <summary>
    /// QR code data.
    /// </summary>
    public string QrCode { get; set; } = string.Empty;
}

#endregion
