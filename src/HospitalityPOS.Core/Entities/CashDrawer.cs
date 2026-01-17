namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Cash drawer pin options (connected via printer RJ11 port).
/// </summary>
public enum CashDrawerPin
{
    /// <summary>
    /// Pin 2 - Most common cash drawer connection.
    /// </summary>
    Pin2 = 0,

    /// <summary>
    /// Pin 5 - Alternative cash drawer connection.
    /// </summary>
    Pin5 = 1
}

/// <summary>
/// Cash drawer status.
/// </summary>
public enum CashDrawerStatus
{
    /// <summary>
    /// Drawer is closed.
    /// </summary>
    Closed,

    /// <summary>
    /// Drawer is open.
    /// </summary>
    Open,

    /// <summary>
    /// Drawer status is unknown (cannot detect).
    /// </summary>
    Unknown
}

/// <summary>
/// Represents a cash drawer connected to a receipt printer.
/// </summary>
public class CashDrawer
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the drawer name.
    /// </summary>
    public string Name { get; set; } = "Main Drawer";

    /// <summary>
    /// Gets or sets the linked printer ID.
    /// </summary>
    public int LinkedPrinterId { get; set; }

    /// <summary>
    /// Gets or sets the drawer pin configuration.
    /// </summary>
    public CashDrawerPin DrawerPin { get; set; } = CashDrawerPin.Pin2;

    /// <summary>
    /// Gets or sets whether drawer auto-opens on cash payment.
    /// </summary>
    public bool AutoOpenOnCashPayment { get; set; } = true;

    /// <summary>
    /// Gets or sets whether drawer auto-opens on cash refund.
    /// </summary>
    public bool AutoOpenOnCashRefund { get; set; } = true;

    /// <summary>
    /// Gets or sets whether drawer auto-opens on drawer count.
    /// </summary>
    public bool AutoOpenOnDrawerCount { get; set; } = true;

    /// <summary>
    /// Gets or sets the current drawer status.
    /// </summary>
    public CashDrawerStatus Status { get; set; } = CashDrawerStatus.Closed;

    /// <summary>
    /// Gets or sets when the drawer was last opened.
    /// </summary>
    public DateTime? LastOpenedAt { get; set; }

    /// <summary>
    /// Gets or sets the user ID who last opened the drawer.
    /// </summary>
    public int? LastOpenedByUserId { get; set; }

    /// <summary>
    /// Gets or sets whether the drawer is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets when the drawer was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when the drawer was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties

    /// <summary>
    /// Gets or sets the linked printer.
    /// </summary>
    public Printer LinkedPrinter { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user who last opened the drawer.
    /// </summary>
    public User? LastOpenedByUser { get; set; }

    /// <summary>
    /// Gets or sets the drawer logs.
    /// </summary>
    public ICollection<CashDrawerLog> Logs { get; set; } = new List<CashDrawerLog>();
}
