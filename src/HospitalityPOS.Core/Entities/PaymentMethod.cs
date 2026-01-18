using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a payment method available in the system.
/// </summary>
public class PaymentMethod : BaseEntity
{
    /// <summary>
    /// Gets or sets the display name of the payment method.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique code for the payment method (CASH, MPESA, etc.).
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the payment method type.
    /// </summary>
    public PaymentMethodType Type { get; set; } = PaymentMethodType.Cash;

    /// <summary>
    /// Gets or sets the optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether the payment method is active.
    /// </summary>
    public new bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets whether a reference number is required.
    /// </summary>
    public bool RequiresReference { get; set; }

    /// <summary>
    /// Gets or sets the label for the reference field (e.g., "M-Pesa Code").
    /// </summary>
    public string? ReferenceLabel { get; set; }

    /// <summary>
    /// Gets or sets the minimum length for the reference number.
    /// </summary>
    public int? ReferenceMinLength { get; set; }

    /// <summary>
    /// Gets or sets the maximum length for the reference number.
    /// </summary>
    public int? ReferenceMaxLength { get; set; }

    /// <summary>
    /// Gets or sets whether this method supports change calculation (cash only).
    /// </summary>
    public bool SupportsChange { get; set; }

    /// <summary>
    /// Gets or sets whether this method triggers cash drawer opening.
    /// </summary>
    public bool OpensDrawer { get; set; }

    /// <summary>
    /// Gets or sets the display order for button arrangement.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Gets or sets the icon path for the payment button.
    /// </summary>
    public string? IconPath { get; set; }

    /// <summary>
    /// Gets or sets the background color (hex) for the payment button.
    /// </summary>
    public string? BackgroundColor { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the payments made using this method.
    /// </summary>
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
