namespace HospitalityPOS.Core.Models;

/// <summary>
/// Result from the Open Work Period dialog.
/// </summary>
public class OpenWorkPeriodResult
{
    /// <summary>
    /// Gets or sets the opening float amount entered.
    /// </summary>
    public decimal OpeningFloat { get; set; }

    /// <summary>
    /// Gets or sets optional notes for the work period.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets whether the user confirmed opening the work period.
    /// </summary>
    public bool Confirmed { get; set; }

    /// <summary>
    /// Creates a confirmed result with the specified opening float.
    /// </summary>
    public static OpenWorkPeriodResult Success(decimal openingFloat, string? notes = null) => new()
    {
        OpeningFloat = openingFloat,
        Notes = notes,
        Confirmed = true
    };

    /// <summary>
    /// Creates a cancelled result.
    /// </summary>
    public static OpenWorkPeriodResult Cancelled() => new()
    {
        Confirmed = false
    };
}
