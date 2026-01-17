using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Models;

/// <summary>
/// Request model for voiding a receipt.
/// </summary>
public class VoidRequest
{
    /// <summary>
    /// Gets or sets the receipt ID to void.
    /// </summary>
    public int ReceiptId { get; set; }

    /// <summary>
    /// Gets or sets the void reason ID.
    /// </summary>
    public int VoidReasonId { get; set; }

    /// <summary>
    /// Gets or sets additional notes for the void.
    /// </summary>
    public string? AdditionalNotes { get; set; }

    /// <summary>
    /// Gets or sets the authorizing user ID (for permission override).
    /// </summary>
    public int? AuthorizedByUserId { get; set; }
}

/// <summary>
/// Result model for void operations.
/// </summary>
public class VoidResult
{
    /// <summary>
    /// Gets or sets whether the void was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if void failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the void record created.
    /// </summary>
    public ReceiptVoid? VoidRecord { get; set; }

    /// <summary>
    /// Gets or sets the voided receipt.
    /// </summary>
    public Receipt? VoidedReceipt { get; set; }

    /// <summary>
    /// Creates a successful void result.
    /// </summary>
    public static VoidResult Successful(ReceiptVoid voidRecord, Receipt receipt) => new()
    {
        Success = true,
        VoidRecord = voidRecord,
        VoidedReceipt = receipt
    };

    /// <summary>
    /// Creates a failed void result.
    /// </summary>
    public static VoidResult Failed(string error) => new()
    {
        Success = false,
        ErrorMessage = error
    };
}

/// <summary>
/// Report item for void reports.
/// </summary>
public class VoidReportItem
{
    /// <summary>
    /// Gets or sets the receipt number.
    /// </summary>
    public string ReceiptNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the voided amount.
    /// </summary>
    public decimal VoidedAmount { get; set; }

    /// <summary>
    /// Gets or sets the void reason name.
    /// </summary>
    public string VoidReason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional notes.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the name of user who voided.
    /// </summary>
    public string VoidedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of user who authorized (if override).
    /// </summary>
    public string? AuthorizedBy { get; set; }

    /// <summary>
    /// Gets or sets the void timestamp.
    /// </summary>
    public DateTime VoidedAt { get; set; }
}
