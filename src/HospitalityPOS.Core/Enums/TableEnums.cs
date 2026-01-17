namespace HospitalityPOS.Core.Enums;

/// <summary>
/// Represents the shape of a table on the floor plan.
/// </summary>
public enum TableShape
{
    /// <summary>
    /// Square table.
    /// </summary>
    Square = 0,

    /// <summary>
    /// Round/circular table.
    /// </summary>
    Round = 1,

    /// <summary>
    /// Rectangular table.
    /// </summary>
    Rectangle = 2
}

/// <summary>
/// Represents the current status of a table.
/// </summary>
public enum TableStatus
{
    /// <summary>
    /// Table is available for seating.
    /// </summary>
    Available = 0,

    /// <summary>
    /// Table is currently occupied.
    /// </summary>
    Occupied = 1,

    /// <summary>
    /// Table is reserved for future use.
    /// </summary>
    Reserved = 2,

    /// <summary>
    /// Table is unavailable (e.g., under maintenance).
    /// </summary>
    Unavailable = 3
}
