namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Reasons for opening the cash drawer.
/// </summary>
public enum CashDrawerOpenReason
{
    /// <summary>
    /// Drawer opened for cash payment.
    /// </summary>
    CashPayment,

    /// <summary>
    /// Drawer opened for cash refund.
    /// </summary>
    CashRefund,

    /// <summary>
    /// Drawer opened manually.
    /// </summary>
    ManualOpen,

    /// <summary>
    /// Drawer opened for drawer count.
    /// </summary>
    DrawerCount,

    /// <summary>
    /// Drawer opened for opening float.
    /// </summary>
    OpeningFloat,

    /// <summary>
    /// Drawer opened for closing count.
    /// </summary>
    ClosingCount,

    /// <summary>
    /// Drawer opened for cash drop.
    /// </summary>
    CashDrop,

    /// <summary>
    /// Drawer opened for petty cash.
    /// </summary>
    PettyCash,

    /// <summary>
    /// Drawer opened for test.
    /// </summary>
    Test,

    /// <summary>
    /// Drawer opened for other reason.
    /// </summary>
    Other
}

/// <summary>
/// Log entry for cash drawer open events.
/// </summary>
public class CashDrawerLog
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the cash drawer ID.
    /// </summary>
    public int CashDrawerId { get; set; }

    /// <summary>
    /// Gets or sets the user ID who opened the drawer.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the reason for opening.
    /// </summary>
    public CashDrawerOpenReason Reason { get; set; }

    /// <summary>
    /// Gets or sets the reference (e.g., receipt number).
    /// </summary>
    public string? Reference { get; set; }

    /// <summary>
    /// Gets or sets notes for manual opens.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets when the drawer was opened.
    /// </summary>
    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the user ID who authorized (for manager overrides).
    /// </summary>
    public int? AuthorizedByUserId { get; set; }

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Gets or sets the error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the cash drawer.
    /// </summary>
    public CashDrawer CashDrawer { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user who opened the drawer.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user who authorized the open.
    /// </summary>
    public User? AuthorizedByUser { get; set; }
}
