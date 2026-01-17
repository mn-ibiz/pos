namespace HospitalityPOS.Core.Printing;

/// <summary>
/// Represents a print job in the print queue.
/// </summary>
public class PrintJob
{
    /// <summary>
    /// Gets or sets the unique job identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the printer name.
    /// </summary>
    public string PrinterName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the document name (for spooler identification).
    /// </summary>
    public string DocumentName { get; set; } = "POS Document";

    /// <summary>
    /// Gets or sets the raw print data.
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the job type.
    /// </summary>
    public PrintJobType JobType { get; set; } = PrintJobType.Receipt;

    /// <summary>
    /// Gets or sets the job priority.
    /// </summary>
    public PrintJobPriority Priority { get; set; } = PrintJobPriority.Normal;

    /// <summary>
    /// Gets or sets the job status.
    /// </summary>
    public PrintJobStatus Status { get; set; } = PrintJobStatus.Pending;

    /// <summary>
    /// Gets or sets the time the job was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Gets or sets the time the job was last attempted.
    /// </summary>
    public DateTime? LastAttemptAt { get; set; }

    /// <summary>
    /// Gets or sets the number of retry attempts.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of retries.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets optional reference ID (e.g., receipt ID, order ID).
    /// </summary>
    public int? ReferenceId { get; set; }

    /// <summary>
    /// Gets or sets optional reference type.
    /// </summary>
    public string? ReferenceType { get; set; }

    /// <summary>
    /// Gets whether the job can be retried.
    /// </summary>
    public bool CanRetry => Status == PrintJobStatus.Failed && RetryCount < MaxRetries;
}

/// <summary>
/// Print job types.
/// </summary>
public enum PrintJobType
{
    /// <summary>
    /// Customer receipt.
    /// </summary>
    Receipt,

    /// <summary>
    /// Kitchen order ticket.
    /// </summary>
    KOT,

    /// <summary>
    /// X or Z report.
    /// </summary>
    Report,

    /// <summary>
    /// Cash drawer kick.
    /// </summary>
    CashDrawer,

    /// <summary>
    /// Test print.
    /// </summary>
    Test,

    /// <summary>
    /// Other document type.
    /// </summary>
    Other
}

/// <summary>
/// Print job priority levels.
/// </summary>
public enum PrintJobPriority
{
    /// <summary>
    /// Low priority (reports, non-urgent documents).
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority (standard receipts).
    /// </summary>
    Normal = 1,

    /// <summary>
    /// High priority (KOTs, time-sensitive).
    /// </summary>
    High = 2,

    /// <summary>
    /// Urgent priority (cash drawer, immediate print).
    /// </summary>
    Urgent = 3
}

/// <summary>
/// Print job status.
/// </summary>
public enum PrintJobStatus
{
    /// <summary>
    /// Job is pending in queue.
    /// </summary>
    Pending,

    /// <summary>
    /// Job is currently being printed.
    /// </summary>
    Printing,

    /// <summary>
    /// Job completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Job failed after retries exhausted.
    /// </summary>
    Failed,

    /// <summary>
    /// Job was cancelled.
    /// </summary>
    Cancelled
}

/// <summary>
/// Result of a print operation.
/// </summary>
public class PrintResult
{
    /// <summary>
    /// Gets or sets whether the print was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the print job ID.
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// Gets or sets the error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the Windows print job ID if available.
    /// </summary>
    public int? WindowsJobId { get; set; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static PrintResult Succeeded(Guid jobId, int? windowsJobId = null)
    {
        return new PrintResult
        {
            Success = true,
            JobId = jobId,
            WindowsJobId = windowsJobId
        };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static PrintResult Failed(Guid jobId, string errorMessage)
    {
        return new PrintResult
        {
            Success = false,
            JobId = jobId,
            ErrorMessage = errorMessage
        };
    }
}

/// <summary>
/// Printer status information.
/// </summary>
public class PrinterStatus
{
    /// <summary>
    /// Gets or sets the printer name.
    /// </summary>
    public string PrinterName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the printer is online.
    /// </summary>
    public bool IsOnline { get; set; }

    /// <summary>
    /// Gets or sets whether the printer is ready.
    /// </summary>
    public bool IsReady { get; set; }

    /// <summary>
    /// Gets or sets whether the printer has paper.
    /// </summary>
    public bool HasPaper { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the cover is open.
    /// </summary>
    public bool IsCoverOpen { get; set; }

    /// <summary>
    /// Gets or sets whether there's a paper jam.
    /// </summary>
    public bool HasPaperJam { get; set; }

    /// <summary>
    /// Gets or sets whether the printer is in error state.
    /// </summary>
    public bool HasError { get; set; }

    /// <summary>
    /// Gets or sets the error description.
    /// </summary>
    public string? ErrorDescription { get; set; }

    /// <summary>
    /// Gets or sets the number of jobs in queue.
    /// </summary>
    public int JobsInQueue { get; set; }

    /// <summary>
    /// Gets or sets the raw status flags.
    /// </summary>
    public int RawStatus { get; set; }

    /// <summary>
    /// Gets or sets the last status check time.
    /// </summary>
    public DateTime LastChecked { get; set; } = DateTime.Now;
}

/// <summary>
/// Print queue statistics.
/// </summary>
public class PrintQueueStats
{
    /// <summary>
    /// Gets or sets the total jobs processed.
    /// </summary>
    public int TotalJobsProcessed { get; set; }

    /// <summary>
    /// Gets or sets the successful jobs count.
    /// </summary>
    public int SuccessfulJobs { get; set; }

    /// <summary>
    /// Gets or sets the failed jobs count.
    /// </summary>
    public int FailedJobs { get; set; }

    /// <summary>
    /// Gets or sets the pending jobs count.
    /// </summary>
    public int PendingJobs { get; set; }

    /// <summary>
    /// Gets or sets the average print time in milliseconds.
    /// </summary>
    public double AveragePrintTimeMs { get; set; }

    /// <summary>
    /// Gets the success rate as a percentage.
    /// </summary>
    public double SuccessRate => TotalJobsProcessed > 0
        ? (double)SuccessfulJobs / TotalJobsProcessed * 100
        : 0;
}

/// <summary>
/// Event args for print job events.
/// </summary>
public class PrintJobEventArgs : EventArgs
{
    /// <summary>
    /// Gets the print job.
    /// </summary>
    public PrintJob Job { get; }

    /// <summary>
    /// Gets the print result if available.
    /// </summary>
    public PrintResult? Result { get; }

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    public PrintJobEventArgs(PrintJob job, PrintResult? result = null)
    {
        Job = job;
        Result = result;
    }
}

/// <summary>
/// Configuration for image conversion.
/// </summary>
public class ImageConversionOptions
{
    /// <summary>
    /// Gets or sets the maximum width in pixels.
    /// </summary>
    public int MaxWidth { get; set; } = 384; // Standard 80mm printer width

    /// <summary>
    /// Gets or sets the dithering method.
    /// </summary>
    public DitheringMethod Dithering { get; set; } = DitheringMethod.FloydSteinberg;

    /// <summary>
    /// Gets or sets the brightness adjustment (-100 to 100).
    /// </summary>
    public int Brightness { get; set; } = 0;

    /// <summary>
    /// Gets or sets the contrast adjustment (-100 to 100).
    /// </summary>
    public int Contrast { get; set; } = 0;

    /// <summary>
    /// Gets or sets whether to invert colors.
    /// </summary>
    public bool Invert { get; set; }

    /// <summary>
    /// Gets or sets the threshold for binary conversion (0-255).
    /// </summary>
    public int Threshold { get; set; } = 128;
}

/// <summary>
/// Image dithering methods.
/// </summary>
public enum DitheringMethod
{
    /// <summary>
    /// No dithering, simple threshold.
    /// </summary>
    None,

    /// <summary>
    /// Floyd-Steinberg dithering (best quality).
    /// </summary>
    FloydSteinberg,

    /// <summary>
    /// Ordered dithering (Bayer matrix).
    /// </summary>
    Ordered,

    /// <summary>
    /// Atkinson dithering (good for text/logos).
    /// </summary>
    Atkinson
}

/// <summary>
/// Result of image conversion.
/// </summary>
public class ImageConversionResult
{
    /// <summary>
    /// Gets or sets whether conversion was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the raster image data.
    /// </summary>
    public byte[] ImageData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the width in bytes.
    /// </summary>
    public int WidthBytes { get; set; }

    /// <summary>
    /// Gets or sets the width in pixels.
    /// </summary>
    public int WidthPixels { get; set; }

    /// <summary>
    /// Gets or sets the height in pixels.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Gets or sets the error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static ImageConversionResult Succeeded(byte[] imageData, int widthPixels, int height)
    {
        return new ImageConversionResult
        {
            Success = true,
            ImageData = imageData,
            WidthPixels = widthPixels,
            WidthBytes = (widthPixels + 7) / 8,
            Height = height
        };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static ImageConversionResult Failed(string errorMessage)
    {
        return new ImageConversionResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}
