using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// eTIMS credit note submission record for voided/refunded invoices.
/// </summary>
public class EtimsCreditNote : BaseEntity
{
    /// <summary>
    /// Related receipt void ID (if from void operation).
    /// </summary>
    public int? ReceiptVoidId { get; set; }

    /// <summary>
    /// Original eTIMS invoice being credited.
    /// </summary>
    public int OriginalInvoiceId { get; set; }

    /// <summary>
    /// eTIMS device used for submission.
    /// </summary>
    public int DeviceId { get; set; }

    /// <summary>
    /// Credit note number.
    /// </summary>
    public string CreditNoteNumber { get; set; } = string.Empty;

    /// <summary>
    /// Original invoice number being credited.
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
    /// Customer PIN.
    /// </summary>
    public string? CustomerPin { get; set; }

    /// <summary>
    /// Customer name.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Total credit amount.
    /// </summary>
    public decimal CreditAmount { get; set; }

    /// <summary>
    /// Tax amount credited.
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Submission status.
    /// </summary>
    public EtimsSubmissionStatus Status { get; set; } = EtimsSubmissionStatus.Pending;

    /// <summary>
    /// Number of submission attempts.
    /// </summary>
    public int SubmissionAttempts { get; set; }

    /// <summary>
    /// Last submission attempt time.
    /// </summary>
    public DateTime? LastSubmissionAttempt { get; set; }

    /// <summary>
    /// Successful submission time.
    /// </summary>
    public DateTime? SubmittedAt { get; set; }

    /// <summary>
    /// KRA-assigned signature.
    /// </summary>
    public string? KraSignature { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Raw request JSON.
    /// </summary>
    public string? RequestJson { get; set; }

    /// <summary>
    /// Raw response JSON.
    /// </summary>
    public string? ResponseJson { get; set; }

    // Navigation properties
    public EtimsInvoice OriginalInvoice { get; set; } = null!;
    public EtimsDevice Device { get; set; } = null!;
    public ICollection<EtimsCreditNoteItem> Items { get; set; } = [];
}

/// <summary>
/// Credit note line item.
/// </summary>
public class EtimsCreditNoteItem : BaseEntity
{
    /// <summary>
    /// Parent credit note ID.
    /// </summary>
    public int EtimsCreditNoteId { get; set; }

    /// <summary>
    /// Line item sequence.
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
    /// Tax type code.
    /// </summary>
    public KraTaxType TaxType { get; set; }

    /// <summary>
    /// Tax rate.
    /// </summary>
    public decimal TaxRate { get; set; }

    /// <summary>
    /// Taxable amount.
    /// </summary>
    public decimal TaxableAmount { get; set; }

    /// <summary>
    /// Tax amount.
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Total credit amount.
    /// </summary>
    public decimal TotalAmount { get; set; }

    // Navigation property
    public EtimsCreditNote EtimsCreditNote { get; set; } = null!;
}
