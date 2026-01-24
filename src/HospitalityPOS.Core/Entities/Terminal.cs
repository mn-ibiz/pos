using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a physical POS terminal/register in the store network.
/// </summary>
public class Terminal : BaseEntity
{
    /// <summary>
    /// Gets or sets the parent store ID.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the display code (e.g., REG-001).
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the friendly name (e.g., Register 1).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the machine identifier (MAC address or GUID).
    /// </summary>
    public string MachineIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the terminal type.
    /// </summary>
    public TerminalType TerminalType { get; set; } = TerminalType.Register;

    /// <summary>
    /// Gets or sets the business mode.
    /// </summary>
    public BusinessMode BusinessMode { get; set; } = BusinessMode.Supermarket;

    /// <summary>
    /// Gets or sets whether this is the main/primary register for the store.
    /// </summary>
    public bool IsMainRegister { get; set; }

    /// <summary>
    /// Gets or sets the last heartbeat timestamp.
    /// </summary>
    public DateTime? LastHeartbeat { get; set; }

    /// <summary>
    /// Gets or sets the last logged-in user ID.
    /// </summary>
    public int? LastLoginUserId { get; set; }

    /// <summary>
    /// Gets or sets the last login timestamp.
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Gets or sets the current IP address of the terminal.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the printer configuration as JSON.
    /// </summary>
    public string? PrinterConfiguration { get; set; }

    /// <summary>
    /// Gets or sets the hardware configuration (drawer, display, scale) as JSON.
    /// </summary>
    public string? HardwareConfiguration { get; set; }

    #region Navigation Properties

    /// <summary>
    /// Gets or sets the parent store.
    /// </summary>
    public virtual Store? Store { get; set; }

    /// <summary>
    /// Gets or sets the user who created this terminal.
    /// </summary>
    public virtual User? CreatedByUser { get; set; }

    /// <summary>
    /// Gets or sets the user who last modified this terminal.
    /// </summary>
    public virtual User? UpdatedByUser { get; set; }

    /// <summary>
    /// Gets or sets the last logged-in user.
    /// </summary>
    public virtual User? LastLoginUser { get; set; }

    #endregion
}
