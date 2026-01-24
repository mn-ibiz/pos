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
    /// Gets or sets the general email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the email specifically for receiving Purchase Orders.
    /// Falls back to main Email if not set.
    /// </summary>
    public string? OrderEmail { get; set; }

    /// <summary>
    /// Gets or sets the email for accounts/invoices/payments.
    /// </summary>
    public string? AccountsEmail { get; set; }

    /// <summary>
    /// Gets or sets whether to automatically send POs by email when submitted.
    /// </summary>
    public bool SendPOByEmail { get; set; }

    /// <summary>
    /// Gets or sets CC email addresses for PO emails (comma-separated).
    /// </summary>
    public string? EmailCcAddresses { get; set; }

    /// <summary>
    /// Gets or sets the preferred language for email communications.
    /// </summary>
    public string? PreferredLanguage { get; set; }

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

    /// <summary>
    /// Gets or sets the default expense category ID for this supplier.
    /// </summary>
    public int? DefaultExpenseCategoryId { get; set; }

    // Navigation properties
    public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    public virtual ICollection<GoodsReceivedNote> GoodsReceivedNotes { get; set; } = new List<GoodsReceivedNote>();
    public virtual ICollection<SupplierInvoice> SupplierInvoices { get; set; } = new List<SupplierInvoice>();
    public virtual ICollection<SupplierPayment> SupplierPayments { get; set; } = new List<SupplierPayment>();
    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public virtual ICollection<RecurringExpense> RecurringExpenses { get; set; } = new List<RecurringExpense>();
    public virtual ICollection<SupplierContact> Contacts { get; set; } = new List<SupplierContact>();
    public virtual ExpenseCategory? DefaultExpenseCategory { get; set; }

    /// <summary>
    /// Gets the primary email for PO communications.
    /// Returns OrderEmail if set, otherwise falls back to general Email.
    /// </summary>
    public string? GetPOEmail() => !string.IsNullOrEmpty(OrderEmail) ? OrderEmail : Email;
}
