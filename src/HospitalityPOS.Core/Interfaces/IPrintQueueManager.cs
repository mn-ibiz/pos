using HospitalityPOS.Core.Printing;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Manages print job queue with priority and retry support.
/// </summary>
public interface IPrintQueueManager
{
    /// <summary>
    /// Event raised when a job is queued.
    /// </summary>
    event EventHandler<PrintJobEventArgs>? JobQueued;

    /// <summary>
    /// Event raised when a job starts printing.
    /// </summary>
    event EventHandler<PrintJobEventArgs>? JobStarted;

    /// <summary>
    /// Event raised when a job completes.
    /// </summary>
    event EventHandler<PrintJobEventArgs>? JobCompleted;

    /// <summary>
    /// Event raised when a job fails.
    /// </summary>
    event EventHandler<PrintJobEventArgs>? JobFailed;

    /// <summary>
    /// Gets whether the queue processor is running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Gets the number of jobs in the queue.
    /// </summary>
    int QueueLength { get; }

    /// <summary>
    /// Starts the queue processor.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops the queue processor.
    /// </summary>
    void Stop();

    /// <summary>
    /// Enqueues a print job.
    /// </summary>
    /// <param name="job">The print job to queue.</param>
    /// <returns>The job ID.</returns>
    Guid Enqueue(PrintJob job);

    /// <summary>
    /// Enqueues raw data for printing.
    /// </summary>
    /// <param name="printerName">Printer name.</param>
    /// <param name="data">Raw print data.</param>
    /// <param name="jobType">Type of print job.</param>
    /// <param name="priority">Job priority.</param>
    /// <param name="documentName">Document name.</param>
    /// <returns>The job ID.</returns>
    Guid Enqueue(string printerName, byte[] data, PrintJobType jobType = PrintJobType.Receipt,
        PrintJobPriority priority = PrintJobPriority.Normal, string documentName = "POS Document");

    /// <summary>
    /// Enqueues a print document.
    /// </summary>
    /// <param name="printerName">Printer name.</param>
    /// <param name="document">The ESC/POS document.</param>
    /// <param name="jobType">Type of print job.</param>
    /// <param name="priority">Job priority.</param>
    /// <returns>The job ID.</returns>
    Guid Enqueue(string printerName, EscPosPrintDocument document, PrintJobType jobType = PrintJobType.Receipt,
        PrintJobPriority priority = PrintJobPriority.Normal);

    /// <summary>
    /// Prints immediately without queuing (blocking).
    /// </summary>
    /// <param name="printerName">Printer name.</param>
    /// <param name="data">Raw print data.</param>
    /// <param name="documentName">Document name.</param>
    /// <returns>Print result.</returns>
    Task<PrintResult> PrintImmediateAsync(string printerName, byte[] data, string documentName = "POS Document");

    /// <summary>
    /// Prints a document immediately without queuing.
    /// </summary>
    Task<PrintResult> PrintImmediateAsync(string printerName, EscPosPrintDocument document);

    /// <summary>
    /// Cancels a pending job.
    /// </summary>
    /// <param name="jobId">The job ID to cancel.</param>
    /// <returns>True if cancelled.</returns>
    bool Cancel(Guid jobId);

    /// <summary>
    /// Cancels all pending jobs.
    /// </summary>
    void CancelAll();

    /// <summary>
    /// Cancels all pending jobs for a specific printer.
    /// </summary>
    /// <param name="printerName">The printer name.</param>
    void CancelAllForPrinter(string printerName);

    /// <summary>
    /// Retries a failed job.
    /// </summary>
    /// <param name="jobId">The job ID to retry.</param>
    /// <returns>True if job was requeued.</returns>
    bool Retry(Guid jobId);

    /// <summary>
    /// Gets a job by ID.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <returns>The print job or null.</returns>
    PrintJob? GetJob(Guid jobId);

    /// <summary>
    /// Gets all pending jobs.
    /// </summary>
    /// <returns>List of pending jobs.</returns>
    IReadOnlyList<PrintJob> GetPendingJobs();

    /// <summary>
    /// Gets all failed jobs.
    /// </summary>
    /// <returns>List of failed jobs.</returns>
    IReadOnlyList<PrintJob> GetFailedJobs();

    /// <summary>
    /// Gets queue statistics.
    /// </summary>
    /// <returns>Queue statistics.</returns>
    PrintQueueStats GetStatistics();

    /// <summary>
    /// Clears completed and failed job history.
    /// </summary>
    void ClearHistory();
}
