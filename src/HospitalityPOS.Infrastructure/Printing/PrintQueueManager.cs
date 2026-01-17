using System.Collections.Concurrent;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Printing;
using Microsoft.Extensions.Logging;

namespace HospitalityPOS.Infrastructure.Printing;

/// <summary>
/// Manages print job queue with priority and retry support.
/// </summary>
public class PrintQueueManager : IPrintQueueManager, IDisposable
{
    private readonly IPrinterCommunicationService _communicationService;
    private readonly ILogger<PrintQueueManager> _logger;
    private readonly PriorityQueue<PrintJob, int> _queue = new();
    private readonly ConcurrentDictionary<Guid, PrintJob> _jobs = new();
    private readonly ConcurrentDictionary<Guid, PrintJob> _history = new();
    private readonly object _queueLock = new();
    private readonly SemaphoreSlim _processSemaphore = new(1, 1);

    private CancellationTokenSource? _cts;
    private Task? _processingTask;
    private bool _isRunning;
    private bool _disposed;

    // Statistics
    private int _totalProcessed;
    private int _successCount;
    private int _failCount;
    private double _totalPrintTimeMs;

    public event EventHandler<PrintJobEventArgs>? JobQueued;
    public event EventHandler<PrintJobEventArgs>? JobStarted;
    public event EventHandler<PrintJobEventArgs>? JobCompleted;
    public event EventHandler<PrintJobEventArgs>? JobFailed;

    public PrintQueueManager(
        IPrinterCommunicationService communicationService,
        ILogger<PrintQueueManager> logger)
    {
        _communicationService = communicationService;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public int QueueLength
    {
        get
        {
            lock (_queueLock)
            {
                return _queue.Count;
            }
        }
    }

    /// <inheritdoc />
    public void Start()
    {
        if (_isRunning) return;

        _cts = new CancellationTokenSource();
        _isRunning = true;
        _processingTask = ProcessQueueAsync(_cts.Token);

        _logger.LogInformation("Print queue manager started");
    }

    /// <inheritdoc />
    public void Stop()
    {
        if (!_isRunning) return;

        _cts?.Cancel();
        _isRunning = false;

        // Use async-safe wait with timeout
        if (_processingTask != null)
        {
            try
            {
                // Wait with timeout, handling cancellation gracefully
                var completedInTime = _processingTask.Wait(TimeSpan.FromSeconds(5));
                if (!completedInTime)
                {
                    _logger.LogWarning("Print queue processing task did not complete within timeout");
                }
            }
            catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is OperationCanceledException))
            {
                // Expected when cancellation occurs - this is fine
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while stopping print queue manager");
            }
        }

        _logger.LogInformation("Print queue manager stopped");
    }

    /// <inheritdoc />
    public Guid Enqueue(PrintJob job)
    {
        if (job == null) throw new ArgumentNullException(nameof(job));

        _jobs[job.Id] = job;

        lock (_queueLock)
        {
            // Priority queue uses lower numbers = higher priority
            int priority = -(int)job.Priority;
            _queue.Enqueue(job, priority);
        }

        _logger.LogDebug("Print job {JobId} queued for {Printer} (Priority: {Priority})",
            job.Id, job.PrinterName, job.Priority);

        JobQueued?.Invoke(this, new PrintJobEventArgs(job));

        return job.Id;
    }

    /// <inheritdoc />
    public Guid Enqueue(string printerName, byte[] data, PrintJobType jobType = PrintJobType.Receipt,
        PrintJobPriority priority = PrintJobPriority.Normal, string documentName = "POS Document")
    {
        var job = new PrintJob
        {
            PrinterName = printerName,
            Data = data,
            JobType = jobType,
            Priority = priority,
            DocumentName = documentName
        };

        return Enqueue(job);
    }

    /// <inheritdoc />
    public Guid Enqueue(string printerName, EscPosPrintDocument document, PrintJobType jobType = PrintJobType.Receipt,
        PrintJobPriority priority = PrintJobPriority.Normal)
    {
        return Enqueue(printerName, document.Build(), jobType, priority, $"POS {jobType}");
    }

    /// <inheritdoc />
    public async Task<PrintResult> PrintImmediateAsync(string printerName, byte[] data, string documentName = "POS Document")
    {
        var jobId = Guid.NewGuid();

        try
        {
            var startTime = DateTime.Now;
            var success = await _communicationService.SendRawDataAsync(printerName, data, documentName);
            var elapsed = (DateTime.Now - startTime).TotalMilliseconds;

            _totalProcessed++;
            _totalPrintTimeMs += elapsed;

            if (success)
            {
                _successCount++;
                _logger.LogDebug("Immediate print successful on {Printer} ({Ms}ms)", printerName, elapsed);
                return PrintResult.Succeeded(jobId);
            }
            else
            {
                _failCount++;
                _logger.LogWarning("Immediate print failed on {Printer}", printerName);
                return PrintResult.Failed(jobId, "Failed to send data to printer");
            }
        }
        catch (Exception ex)
        {
            _failCount++;
            _logger.LogError(ex, "Immediate print error on {Printer}", printerName);
            return PrintResult.Failed(jobId, ex.Message);
        }
    }

    /// <inheritdoc />
    public Task<PrintResult> PrintImmediateAsync(string printerName, EscPosPrintDocument document)
    {
        return PrintImmediateAsync(printerName, document.Build(), $"POS {document.GetType().Name}");
    }

    /// <inheritdoc />
    public bool Cancel(Guid jobId)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            if (job.Status == PrintJobStatus.Pending)
            {
                job.Status = PrintJobStatus.Cancelled;
                _logger.LogInformation("Print job {JobId} cancelled", jobId);
                return true;
            }
        }
        return false;
    }

    /// <inheritdoc />
    public void CancelAll()
    {
        lock (_queueLock)
        {
            while (_queue.TryDequeue(out var job, out _))
            {
                job.Status = PrintJobStatus.Cancelled;
            }
        }

        _logger.LogInformation("All pending print jobs cancelled");
    }

    /// <inheritdoc />
    public void CancelAllForPrinter(string printerName)
    {
        foreach (var job in _jobs.Values.Where(j => j.PrinterName == printerName && j.Status == PrintJobStatus.Pending))
        {
            job.Status = PrintJobStatus.Cancelled;
        }

        _logger.LogInformation("All pending jobs for printer {Printer} cancelled", printerName);
    }

    /// <inheritdoc />
    public bool Retry(Guid jobId)
    {
        if (_jobs.TryGetValue(jobId, out var job) || _history.TryGetValue(jobId, out job))
        {
            if (job.CanRetry)
            {
                job.Status = PrintJobStatus.Pending;
                job.ErrorMessage = null;
                return Enqueue(job) != Guid.Empty;
            }
        }
        return false;
    }

    /// <inheritdoc />
    public PrintJob? GetJob(Guid jobId)
    {
        if (_jobs.TryGetValue(jobId, out var job))
            return job;

        _history.TryGetValue(jobId, out job);
        return job;
    }

    /// <inheritdoc />
    public IReadOnlyList<PrintJob> GetPendingJobs()
    {
        return _jobs.Values.Where(j => j.Status == PrintJobStatus.Pending).ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<PrintJob> GetFailedJobs()
    {
        return _history.Values.Where(j => j.Status == PrintJobStatus.Failed).ToList();
    }

    /// <inheritdoc />
    public PrintQueueStats GetStatistics()
    {
        return new PrintQueueStats
        {
            TotalJobsProcessed = _totalProcessed,
            SuccessfulJobs = _successCount,
            FailedJobs = _failCount,
            PendingJobs = QueueLength,
            AveragePrintTimeMs = _totalProcessed > 0 ? _totalPrintTimeMs / _totalProcessed : 0
        };
    }

    /// <inheritdoc />
    public void ClearHistory()
    {
        _history.Clear();
        _logger.LogDebug("Print job history cleared");
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                PrintJob? job = null;

                lock (_queueLock)
                {
                    while (_queue.TryDequeue(out var candidate, out _))
                    {
                        // Skip cancelled jobs
                        if (candidate.Status != PrintJobStatus.Cancelled)
                        {
                            job = candidate;
                            break;
                        }
                    }
                }

                if (job != null)
                {
                    await ProcessJobAsync(job, cancellationToken);
                }
                else
                {
                    // No jobs, wait a bit
                    await Task.Delay(100, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in print queue processor");
                await Task.Delay(1000, cancellationToken);
            }
        }
    }

    private async Task ProcessJobAsync(PrintJob job, CancellationToken cancellationToken)
    {
        await _processSemaphore.WaitAsync(cancellationToken);

        try
        {
            job.Status = PrintJobStatus.Printing;
            job.LastAttemptAt = DateTime.Now;

            JobStarted?.Invoke(this, new PrintJobEventArgs(job));

            _logger.LogDebug("Processing print job {JobId} on {Printer}", job.Id, job.PrinterName);

            var startTime = DateTime.Now;
            var success = await _communicationService.SendRawDataAsync(
                job.PrinterName, job.Data, job.DocumentName);
            var elapsed = (DateTime.Now - startTime).TotalMilliseconds;

            _totalProcessed++;
            _totalPrintTimeMs += elapsed;

            if (success)
            {
                job.Status = PrintJobStatus.Completed;
                _successCount++;

                _logger.LogDebug("Print job {JobId} completed ({Ms}ms)", job.Id, elapsed);

                JobCompleted?.Invoke(this, new PrintJobEventArgs(job, PrintResult.Succeeded(job.Id)));

                // Move to history
                _jobs.TryRemove(job.Id, out _);
                _history[job.Id] = job;
            }
            else
            {
                HandleJobFailure(job, "Failed to send data to printer");
            }
        }
        catch (Exception ex)
        {
            HandleJobFailure(job, ex.Message);
        }
        finally
        {
            _processSemaphore.Release();
        }
    }

    private void HandleJobFailure(PrintJob job, string errorMessage)
    {
        job.RetryCount++;
        job.ErrorMessage = errorMessage;

        if (job.CanRetry)
        {
            // Re-queue for retry
            job.Status = PrintJobStatus.Pending;

            lock (_queueLock)
            {
                _queue.Enqueue(job, -(int)job.Priority);
            }

            _logger.LogWarning("Print job {JobId} failed (attempt {Attempt}/{Max}), will retry: {Error}",
                job.Id, job.RetryCount, job.MaxRetries, errorMessage);
        }
        else
        {
            job.Status = PrintJobStatus.Failed;
            _failCount++;

            _logger.LogError("Print job {JobId} failed permanently after {Attempts} attempts: {Error}",
                job.Id, job.RetryCount, errorMessage);

            JobFailed?.Invoke(this, new PrintJobEventArgs(job, PrintResult.Failed(job.Id, errorMessage)));

            // Move to history
            _jobs.TryRemove(job.Id, out _);
            _history[job.Id] = job;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        Stop();
        _cts?.Dispose();
        _processSemaphore.Dispose();

        _disposed = true;
    }
}
