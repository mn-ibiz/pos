namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a cash denomination type (note or coin) for a specific currency.
/// </summary>
public class CashDenomination : BaseEntity
{
    /// <summary>
    /// Gets or sets the currency code (e.g., "KES", "USD").
    /// </summary>
    public string CurrencyCode { get; set; } = "KES";

    /// <summary>
    /// Gets or sets the denomination type.
    /// </summary>
    public DenominationType Type { get; set; }

    /// <summary>
    /// Gets or sets the denomination value (e.g., 1000, 500, 0.50).
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>
    /// Gets or sets the display name (e.g., "KES 1,000", "50 Cents").
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sort order for display.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Gets or sets whether this denomination is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Type of cash denomination.
/// </summary>
public enum DenominationType
{
    /// <summary>
    /// Paper currency note.
    /// </summary>
    Note = 0,

    /// <summary>
    /// Metal currency coin.
    /// </summary>
    Coin = 1
}
