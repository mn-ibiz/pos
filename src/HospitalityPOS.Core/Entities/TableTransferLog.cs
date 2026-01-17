namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a log entry for table transfers between waiters.
/// </summary>
public class TableTransferLog
{
    /// <summary>
    /// Gets or sets the unique identifier for this transfer log entry.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the table ID that was transferred.
    /// </summary>
    public int TableId { get; set; }

    /// <summary>
    /// Gets or sets the table number for display purposes.
    /// </summary>
    public string TableNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user ID of the original waiter.
    /// </summary>
    public int FromUserId { get; set; }

    /// <summary>
    /// Gets or sets the full name of the original waiter.
    /// </summary>
    public string FromUserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user ID of the new waiter.
    /// </summary>
    public int ToUserId { get; set; }

    /// <summary>
    /// Gets or sets the full name of the new waiter.
    /// </summary>
    public string ToUserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the receipt ID if the table had an active receipt.
    /// </summary>
    public int? ReceiptId { get; set; }

    /// <summary>
    /// Gets or sets the receipt amount at the time of transfer.
    /// </summary>
    public decimal ReceiptAmount { get; set; }

    /// <summary>
    /// Gets or sets the optional reason for the transfer.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets when the transfer occurred.
    /// </summary>
    public DateTime TransferredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the user ID who initiated the transfer.
    /// </summary>
    public int TransferredByUserId { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the table that was transferred.
    /// </summary>
    public virtual Table Table { get; set; } = null!;

    /// <summary>
    /// Gets or sets the original waiter.
    /// </summary>
    public virtual User FromUser { get; set; } = null!;

    /// <summary>
    /// Gets or sets the new waiter.
    /// </summary>
    public virtual User ToUser { get; set; } = null!;

    /// <summary>
    /// Gets or sets the receipt if present at time of transfer.
    /// </summary>
    public virtual Receipt? Receipt { get; set; }

    /// <summary>
    /// Gets or sets the user who initiated the transfer.
    /// </summary>
    public virtual User TransferredByUser { get; set; } = null!;
}
