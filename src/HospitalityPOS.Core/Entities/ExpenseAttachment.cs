namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents an attachment (receipt, invoice, document) associated with an expense.
/// </summary>
public class ExpenseAttachment : BaseEntity
{
    /// <summary>
    /// Gets or sets the expense ID this attachment belongs to.
    /// </summary>
    public int ExpenseId { get; set; }

    /// <summary>
    /// Gets or sets the original file name.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the stored file path.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file MIME type.
    /// </summary>
    public string FileType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Gets or sets the attachment description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets when the file was uploaded.
    /// </summary>
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the user ID who uploaded the attachment.
    /// </summary>
    public int? UploadedByUserId { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the expense this attachment belongs to.
    /// </summary>
    public virtual Expense Expense { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user who uploaded the attachment.
    /// </summary>
    public virtual User? UploadedByUser { get; set; }
}
