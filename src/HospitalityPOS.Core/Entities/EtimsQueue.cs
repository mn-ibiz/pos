using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Queue entry for offline eTIMS submissions.
/// </summary>
public class EtimsQueueEntry : BaseEntity
{
    /// <summary>
    /// Queue entry type (Invoice or CreditNote).
    /// </summary>
    public EtimsDocumentType DocumentType { get; set; }

    /// <summary>
    /// Related document ID (EtimsInvoiceId or EtimsCreditNoteId).
    /// </summary>
    public int DocumentId { get; set; }

    /// <summary>
    /// Priority (lower = higher priority).
    /// </summary>
    public int Priority { get; set; } = 100;

    /// <summary>
    /// When this entry was queued.
    /// </summary>
    public DateTime QueuedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When to retry (null = retry immediately).
    /// </summary>
    public DateTime? RetryAfter { get; set; }

    /// <summary>
    /// Number of attempts made.
    /// </summary>
    public int Attempts { get; set; }

    /// <summary>
    /// Maximum retry attempts.
    /// </summary>
    public int MaxAttempts { get; set; } = 10;

    /// <summary>
    /// Current status.
    /// </summary>
    public EtimsSubmissionStatus Status { get; set; } = EtimsSubmissionStatus.Queued;

    /// <summary>
    /// Last error message.
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Last processing time.
    /// </summary>
    public DateTime? LastProcessedAt { get; set; }

    /// <summary>
    /// Successfully processed time.
    /// </summary>
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// eTIMS synchronization log.
/// </summary>
public class EtimsSyncLog : BaseEntity
{
    /// <summary>
    /// Sync operation type.
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Document type being synced.
    /// </summary>
    public EtimsDocumentType? DocumentType { get; set; }

    /// <summary>
    /// Document ID if applicable.
    /// </summary>
    public int? DocumentId { get; set; }

    /// <summary>
    /// Start time.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// End time.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Whether operation was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Request JSON.
    /// </summary>
    public string? RequestJson { get; set; }

    /// <summary>
    /// Response JSON.
    /// </summary>
    public string? ResponseJson { get; set; }

    /// <summary>
    /// HTTP status code.
    /// </summary>
    public int? HttpStatusCode { get; set; }

    /// <summary>
    /// Duration in milliseconds.
    /// </summary>
    public long? DurationMs { get; set; }
}
