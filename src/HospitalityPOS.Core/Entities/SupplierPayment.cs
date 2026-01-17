namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a payment made to a supplier.
/// </summary>
public class SupplierPayment : BaseEntity
{
    public int? SupplierInvoiceId { get; set; }
    public int SupplierId { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public decimal Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Reference { get; set; }
    public int ProcessedByUserId { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public virtual SupplierInvoice? SupplierInvoice { get; set; }
    public virtual Supplier Supplier { get; set; } = null!;
    public virtual User ProcessedByUser { get; set; } = null!;
}
