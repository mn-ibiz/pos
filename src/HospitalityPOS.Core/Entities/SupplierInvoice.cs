using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents an invoice from a supplier (Accounts Payable).
/// </summary>
public class SupplierInvoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public int? PurchaseOrderId { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Unpaid;
    public string? Notes { get; set; }

    // Navigation properties
    public virtual Supplier Supplier { get; set; } = null!;
    public virtual PurchaseOrder? PurchaseOrder { get; set; }
    public virtual ICollection<SupplierPayment> SupplierPayments { get; set; } = new List<SupplierPayment>();
}
