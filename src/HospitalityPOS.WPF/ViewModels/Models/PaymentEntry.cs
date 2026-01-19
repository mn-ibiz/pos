namespace HospitalityPOS.WPF.ViewModels.Models;

/// <summary>
/// Payment method types for split payment.
/// </summary>
public enum SplitPaymentMethod
{
    Cash,
    Mpesa,
    Card
}

/// <summary>
/// Represents a single payment entry in a split payment scenario.
/// </summary>
public class PaymentEntry
{
    /// <summary>
    /// Gets or sets the payment method.
    /// </summary>
    public SplitPaymentMethod Method { get; set; }

    /// <summary>
    /// Gets or sets the display name of the payment method.
    /// </summary>
    public string MethodName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the icon for the payment method (Segoe MDL2 Assets).
    /// </summary>
    public string MethodIcon { get; set; } = "\uE8C7";

    /// <summary>
    /// Gets or sets the color for the payment method.
    /// </summary>
    public string MethodColor { get; set; } = "#22C55E";

    /// <summary>
    /// Gets or sets the payment amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the M-Pesa transaction reference (if applicable).
    /// </summary>
    public string? MpesaReference { get; set; }

    /// <summary>
    /// Gets or sets the card last 4 digits (if applicable).
    /// </summary>
    public string? CardLast4 { get; set; }
}
