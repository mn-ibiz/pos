using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a table in the establishment where guests are seated.
/// </summary>
public class Table : BaseEntity
{
    /// <summary>
    /// Gets or sets the table number (display identifier).
    /// </summary>
    public string TableNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the seating capacity of the table.
    /// </summary>
    public int Capacity { get; set; } = 4;

    /// <summary>
    /// Gets or sets the floor ID this table belongs to.
    /// </summary>
    public int FloorId { get; set; }

    /// <summary>
    /// Gets or sets the optional section ID this table belongs to.
    /// </summary>
    public int? SectionId { get; set; }

    // Grid position properties

    /// <summary>
    /// Gets or sets the X position on the floor grid.
    /// </summary>
    public int GridX { get; set; }

    /// <summary>
    /// Gets or sets the Y position on the floor grid.
    /// </summary>
    public int GridY { get; set; }

    /// <summary>
    /// Gets or sets the width of the table in grid cells.
    /// </summary>
    public int Width { get; set; } = 1;

    /// <summary>
    /// Gets or sets the height of the table in grid cells.
    /// </summary>
    public int Height { get; set; } = 1;

    /// <summary>
    /// Gets or sets the shape of the table.
    /// </summary>
    public TableShape Shape { get; set; } = TableShape.Square;

    // Status properties

    /// <summary>
    /// Gets or sets the current status of the table.
    /// </summary>
    public TableStatus Status { get; set; } = TableStatus.Available;

    /// <summary>
    /// Gets or sets the current receipt ID if the table is occupied.
    /// </summary>
    public int? CurrentReceiptId { get; set; }

    /// <summary>
    /// Gets or sets the assigned user ID (server/waiter).
    /// </summary>
    public int? AssignedUserId { get; set; }

    /// <summary>
    /// Gets or sets when the table became occupied.
    /// </summary>
    public DateTime? OccupiedSince { get; set; }

    /// <summary>
    /// Gets or sets the number of guests currently seated.
    /// </summary>
    public int? GuestCount { get; set; }

    /// <summary>
    /// Gets or sets the row version for optimistic concurrency control.
    /// </summary>
    public byte[] RowVersion { get; set; } = [];

    // Navigation properties

    /// <summary>
    /// Gets or sets the floor this table belongs to.
    /// </summary>
    public virtual Floor Floor { get; set; } = null!;

    /// <summary>
    /// Gets or sets the section this table belongs to.
    /// </summary>
    public virtual Section? Section { get; set; }

    /// <summary>
    /// Gets or sets the current receipt for this table.
    /// </summary>
    public virtual Receipt? CurrentReceipt { get; set; }

    /// <summary>
    /// Gets or sets the assigned user (server/waiter).
    /// </summary>
    public virtual User? AssignedUser { get; set; }
}
