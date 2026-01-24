namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a contact person for a supplier.
/// Allows multiple contacts per supplier with different communication preferences.
/// </summary>
public class SupplierContact : BaseEntity
{
    /// <summary>
    /// Supplier this contact belongs to.
    /// </summary>
    public int SupplierId { get; set; }

    /// <summary>
    /// Contact person's full name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Contact email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Contact phone number (landline).
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Contact mobile number.
    /// </summary>
    public string? Mobile { get; set; }

    /// <summary>
    /// Position/role (e.g., Sales Manager, Accounts, Director).
    /// </summary>
    public string? Position { get; set; }

    /// <summary>
    /// Department (e.g., Sales, Accounts, Procurement).
    /// </summary>
    public string? Department { get; set; }

    /// <summary>
    /// Whether this is the primary contact for the supplier.
    /// </summary>
    public bool IsPrimaryContact { get; set; }

    /// <summary>
    /// Whether this contact should receive Purchase Order emails.
    /// </summary>
    public bool ReceivesPOEmails { get; set; } = true;

    /// <summary>
    /// Whether this contact should receive invoice/payment emails.
    /// </summary>
    public bool ReceivesInvoiceEmails { get; set; }

    /// <summary>
    /// Whether this contact should receive order confirmation emails.
    /// </summary>
    public bool ReceivesDeliveryNotifications { get; set; }

    /// <summary>
    /// Preferred contact method (Email, Phone, WhatsApp).
    /// </summary>
    public string? PreferredContactMethod { get; set; }

    /// <summary>
    /// Contact's preferred language for communications.
    /// </summary>
    public string? PreferredLanguage { get; set; }

    /// <summary>
    /// Additional notes about this contact.
    /// </summary>
    public string? Notes { get; set; }

    // Navigation property
    public virtual Supplier Supplier { get; set; } = null!;
}
