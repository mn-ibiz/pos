using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Models;

/// <summary>
/// Request model for transferring a single table.
/// </summary>
public class TransferTableRequest
{
    /// <summary>
    /// Gets or sets the table ID to transfer.
    /// </summary>
    public int TableId { get; set; }

    /// <summary>
    /// Gets or sets the new waiter's user ID.
    /// </summary>
    public int NewWaiterId { get; set; }

    /// <summary>
    /// Gets or sets the optional reason for the transfer.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the user ID who initiated the transfer.
    /// </summary>
    public int TransferredByUserId { get; set; }
}

/// <summary>
/// Request model for bulk transfer of multiple tables.
/// </summary>
public class BulkTransferRequest
{
    /// <summary>
    /// Gets or sets the list of table IDs to transfer.
    /// </summary>
    public List<int> TableIds { get; set; } = new();

    /// <summary>
    /// Gets or sets the new waiter's user ID.
    /// </summary>
    public int NewWaiterId { get; set; }

    /// <summary>
    /// Gets or sets the optional reason for the transfer.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the user ID who initiated the transfer.
    /// </summary>
    public int TransferredByUserId { get; set; }
}

/// <summary>
/// Result model for table transfer operations.
/// </summary>
public class TransferResult
{
    /// <summary>
    /// Gets or sets whether the transfer was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the error message if transfer failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the transfer log entry for single transfer.
    /// </summary>
    public TableTransferLog? TransferLog { get; set; }

    /// <summary>
    /// Gets or sets the transfer log entries for bulk transfer.
    /// </summary>
    public List<TableTransferLog>? TransferLogs { get; set; }

    /// <summary>
    /// Gets or sets the list of errors for partial failures.
    /// </summary>
    public List<string>? Errors { get; set; }

    /// <summary>
    /// Creates a successful result for single table transfer.
    /// </summary>
    public static TransferResult Success(TableTransferLog log) => new()
    {
        IsSuccess = true,
        TransferLog = log
    };

    /// <summary>
    /// Creates a successful result for bulk transfer.
    /// </summary>
    public static TransferResult Success(List<TableTransferLog> logs) => new()
    {
        IsSuccess = true,
        TransferLogs = logs
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static TransferResult Failed(string error) => new()
    {
        IsSuccess = false,
        ErrorMessage = error
    };

    /// <summary>
    /// Creates a partial success result for bulk transfer.
    /// </summary>
    public static TransferResult PartialSuccess(List<TableTransferLog> logs, List<string> errors) => new()
    {
        IsSuccess = false,
        TransferLogs = logs,
        Errors = errors
    };
}

/// <summary>
/// Summary model for printing transfer receipt.
/// </summary>
public class TransferSummary
{
    /// <summary>
    /// Gets or sets the original waiter's name.
    /// </summary>
    public string FromWaiter { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the new waiter's name.
    /// </summary>
    public string ToWaiter { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of transferred tables.
    /// </summary>
    public List<Table> Tables { get; set; } = new();

    /// <summary>
    /// Gets or sets the total value of all open bills.
    /// </summary>
    public decimal TotalValue { get; set; }

    /// <summary>
    /// Gets or sets the transfer reason.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets when the summary was printed.
    /// </summary>
    public DateTime PrintedAt { get; set; } = DateTime.Now;
}
