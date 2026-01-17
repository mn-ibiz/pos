namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a receipt template with header, footer, and branding.
/// </summary>
public class ReceiptTemplate
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the template name.
    /// </summary>
    public string Name { get; set; } = "Default";

    /// <summary>
    /// Gets or sets the business name (header).
    /// </summary>
    public string BusinessName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the business subtitle (header).
    /// </summary>
    public string? BusinessSubtitle { get; set; }

    /// <summary>
    /// Gets or sets the business address (header).
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Gets or sets the phone number (header).
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Gets or sets the email address (header).
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the KRA Tax PIN (header).
    /// </summary>
    public string? TaxPin { get; set; }

    /// <summary>
    /// Gets or sets footer line 1.
    /// </summary>
    public string? FooterLine1 { get; set; } = "Thank you for your visit!";

    /// <summary>
    /// Gets or sets footer line 2.
    /// </summary>
    public string? FooterLine2 { get; set; } = "Please come again";

    /// <summary>
    /// Gets or sets footer line 3.
    /// </summary>
    public string? FooterLine3 { get; set; }

    /// <summary>
    /// Gets or sets whether to show tax breakdown.
    /// </summary>
    public bool ShowTaxBreakdown { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to show cashier name.
    /// </summary>
    public bool ShowCashierName { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to show table number.
    /// </summary>
    public bool ShowTableNumber { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to show QR code.
    /// </summary>
    public bool ShowQRCode { get; set; }

    /// <summary>
    /// Gets or sets the QR code content (URL or text).
    /// </summary>
    public string? QRCodeContent { get; set; }

    /// <summary>
    /// Gets or sets whether this is the default template.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Gets or sets whether the template is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets when the template was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when the template was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
