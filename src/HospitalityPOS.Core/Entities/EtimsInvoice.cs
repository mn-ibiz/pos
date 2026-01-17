using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// eTIMS invoice submission record.
/// </summary>
public class EtimsInvoice : BaseEntity
{
    /// <summary>
    /// Related receipt ID.
    /// </summary>
    public int ReceiptId { get; set; }

    /// <summary>
    /// eTIMS device used for submission.
    /// </summary>
    public int DeviceId { get; set; }

    /// <summary>
    /// KRA-compliant invoice number (format: CU-BRANCH-YYYY-NNNNNN).
    /// </summary>
    public string InvoiceNumber { get; set; } = string.Empty;

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
    /// Customer PIN (for B2B transactions).
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
    /// Taxable amount (net of VAT).
    /// </summary>
    public decimal TaxableAmount { get; set; }

    /// <summary>
    /// Tax amount (VAT at 16%).
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Total invoice amount.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Standard rated amount (16% VAT).
    /// </summary>
    public decimal StandardRatedAmount { get; set; }

    /// <summary>
    /// Zero rated amount.
    /// </summary>
    public decimal ZeroRatedAmount { get; set; }

    /// <summary>
    /// Exempt amount.
    /// </summary>
    public decimal ExemptAmount { get; set; }

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
    /// KRA-assigned receipt signature/QR code data.
    /// </summary>
    public string? ReceiptSignature { get; set; }

    /// <summary>
    /// KRA-assigned internal data (for verification).
    /// </summary>
    public string? KraInternalData { get; set; }

    /// <summary>
    /// QR code string for receipt verification.
    /// </summary>
    public string? QrCode { get; set; }

    /// <summary>
    /// Error message if submission failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Raw request JSON sent to eTIMS.
    /// </summary>
    public string? RequestJson { get; set; }

    /// <summary>
    /// Raw response JSON from eTIMS.
    /// </summary>
    public string? ResponseJson { get; set; }

    // Navigation properties
    public Receipt Receipt { get; set; } = null!;
    public EtimsDevice Device { get; set; } = null!;
    public ICollection<EtimsInvoiceItem> Items { get; set; } = [];
}

/// <summary>
/// eTIMS invoice line item.
/// </summary>
public class EtimsInvoiceItem : BaseEntity
{
    /// <summary>
    /// Parent invoice ID.
    /// </summary>
    public int EtimsInvoiceId { get; set; }

    /// <summary>
    /// Line item sequence number.
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
    /// HS Code (Harmonized System Code) for the item.
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

    /// <summary>
    /// Taxable amount (before VAT).
    /// </summary>
    public decimal TaxableAmount { get; set; }

    /// <summary>
    /// Tax amount.
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Total amount (including tax).
    /// </summary>
    public decimal TotalAmount { get; set; }

    // Navigation property
    public EtimsInvoice EtimsInvoice { get; set; } = null!;
}
