using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Models;

/// <summary>
/// Result of a receipt split operation.
/// </summary>
public class SplitResult
{
    /// <summary>
    /// Gets or sets whether the split was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if split failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the original receipt that was split.
    /// </summary>
    public Receipt? OriginalReceipt { get; set; }

    /// <summary>
    /// Gets or sets the list of split receipts created.
    /// </summary>
    public List<Receipt> SplitReceipts { get; set; } = new();

    /// <summary>
    /// Creates a successful split result.
    /// </summary>
    public static SplitResult Successful(Receipt original, List<Receipt> splits) => new()
    {
        Success = true,
        OriginalReceipt = original,
        SplitReceipts = splits
    };

    /// <summary>
    /// Creates a failed split result.
    /// </summary>
    public static SplitResult Failed(string error) => new()
    {
        Success = false,
        ErrorMessage = error
    };
}

/// <summary>
/// Request for item-based split specifying which items go to a split receipt.
/// </summary>
public class SplitItemRequest
{
    /// <summary>
    /// Gets or sets the list of receipt item IDs for this split.
    /// </summary>
    public List<int> ItemIds { get; set; } = new();

    /// <summary>
    /// Gets or sets an optional customer name for this split.
    /// </summary>
    public string? CustomerName { get; set; }
}

/// <summary>
/// Request for equal split specifying number of ways to split.
/// </summary>
public class EqualSplitRequest
{
    /// <summary>
    /// Gets or sets the receipt ID to split.
    /// </summary>
    public int ReceiptId { get; set; }

    /// <summary>
    /// Gets or sets the number of ways to split.
    /// </summary>
    public int NumberOfWays { get; set; }
}

/// <summary>
/// Request for item-based split.
/// </summary>
public class ItemBasedSplitRequest
{
    /// <summary>
    /// Gets or sets the receipt ID to split.
    /// </summary>
    public int ReceiptId { get; set; }

    /// <summary>
    /// Gets or sets the list of split requests (one per new split receipt).
    /// </summary>
    public List<SplitItemRequest> Splits { get; set; } = new();
}

/// <summary>
/// Result of a receipt merge operation.
/// </summary>
public class MergeResult
{
    /// <summary>
    /// Gets or sets whether the merge was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if merge failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the merged receipt created.
    /// </summary>
    public Receipt? MergedReceipt { get; set; }

    /// <summary>
    /// Gets or sets the source receipts that were merged.
    /// </summary>
    public List<Receipt> SourceReceipts { get; set; } = new();

    /// <summary>
    /// Creates a successful merge result.
    /// </summary>
    public static MergeResult Successful(Receipt merged, List<Receipt> sources) => new()
    {
        Success = true,
        MergedReceipt = merged,
        SourceReceipts = sources
    };

    /// <summary>
    /// Creates a failed merge result.
    /// </summary>
    public static MergeResult Failed(string error) => new()
    {
        Success = false,
        ErrorMessage = error
    };
}
