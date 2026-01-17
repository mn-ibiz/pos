namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a supplier of products.
/// </summary>
public class Supplier : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique supplier code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the supplier name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the contact person name.
    /// </summary>
    public string? ContactPerson { get; set; }

    /// <summary>
    /// Gets or sets the phone number.
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the street address.
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Gets or sets the city.
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// Gets or sets the country.
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// Gets or sets the tax ID (KRA PIN for Kenya).
    /// </summary>
    public string? TaxId { get; set; }

    /// <summary>
    /// Gets or sets the bank account number.
    /// </summary>
    public string? BankAccount { get; set; }

    /// <summary>
    /// Gets or sets the bank name.
    /// </summary>
    public string? BankName { get; set; }

    /// <summary>
    /// Gets or sets the payment term in days.
    /// </summary>
    public int PaymentTermDays { get; set; }

    /// <summary>
    /// Gets or sets the credit limit.
    /// </summary>
    public decimal CreditLimit { get; set; }

    /// <summary>
    /// Gets or sets the current outstanding balance.
    /// </summary>
    public decimal CurrentBalance { get; set; }

    /// <summary>
    /// Gets or sets additional notes about the supplier.
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    public virtual ICollection<GoodsReceivedNote> GoodsReceivedNotes { get; set; } = new List<GoodsReceivedNote>();
    public virtual ICollection<SupplierInvoice> SupplierInvoices { get; set; } = new List<SupplierInvoice>();
    public virtual ICollection<SupplierPayment> SupplierPayments { get; set; } = new List<SupplierPayment>();
}
