# Story 18.4: Offline eTIMS Queue Management

## Story
**As the** system,
**I want to** queue invoices when offline and sync when connected,
**So that** offline operation doesn't break tax compliance.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Implementation Summary
**Completed:** January 2026

### Files Created/Modified:
- `src/HospitalityPOS.Infrastructure/Services/EtimsService.cs` - Offline queue with:
  - `QueueForSubmissionAsync` - Queue documents for later submission
  - `ProcessQueueAsync` - Background queue processor
  - `GetQueueStatsAsync` - Queue statistics (pending, failed, submitted)
  - Exponential backoff retry (1, 2, 4, 8... up to 60 min)

## Epic
**Epic 18: Kenya eTIMS Compliance (MANDATORY)**

## Context
Internet connectivity in Kenya can be unreliable. The POS must continue operating during outages while ensuring all invoices are eventually submitted to KRA. The offline queue ensures no transactions are lost and all are eventually synced to eTIMS.

## Acceptance Criteria

### AC1: Offline Transaction Handling
**Given** no internet connection
**When** completing a transaction
**Then**:
- Invoice is generated and stored locally
- Receipt shows "eTIMS Pending" status indicator
- Invoice queued for later submission
- Transaction completes normally without blocking

### AC2: Queue Sync on Reconnection
**Given** connection is restored
**When** sync service runs
**Then**:
- All pending invoices submitted to KRA in chronological order
- Maintains invoice sequence integrity
- Updates each invoice status upon successful submission
- Logs sync activity

### AC3: Dashboard Visibility
**Given** invoices are in queue
**When** viewing dashboard
**Then** shows:
- Pending submission count
- Oldest pending invoice timestamp
- Last successful sync time
- Visual indicator (yellow when pending, green when synced)

### AC4: Priority Queue Processing
**Given** multiple pending invoices
**When** processing queue
**Then**:
- Submits in FIFO order (oldest first)
- Respects KRA rate limits
- Pauses on rate limit errors
- Continues processing after brief delay

## Technical Notes

### Implementation Details
```csharp
public class ETimsOfflineQueue
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; }
    public string InvoicePayload { get; set; }  // JSON serialized
    public DateTime CreatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public int RetryCount { get; set; }
    public QueueStatus Status { get; set; }
    public string? LastError { get; set; }
}

public enum QueueStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Expired  // Beyond 24-hour window
}

public interface IETimsQueueService
{
    Task EnqueueAsync(ETimsInvoice invoice);
    Task<int> ProcessQueueAsync();
    Task<QueueSummary> GetQueueSummaryAsync();
}
```

### Queue Processing Service
```csharp
public class ETimsQueueProcessor
{
    private readonly SemaphoreSlim _processLock = new(1, 1);

    public async Task ProcessQueueAsync()
    {
        if (!await _connectivityService.IsOnlineAsync())
            return;

        await _processLock.WaitAsync();
        try
        {
            var pending = await _queueRepository.GetPendingAsync(batchSize: 50);

            foreach (var item in pending)
            {
                try
                {
                    item.Status = QueueStatus.Processing;
                    await _queueRepository.UpdateAsync(item);

                    var invoice = JsonSerializer.Deserialize<ETimsInvoice>(item.InvoicePayload);
                    var result = await _submissionService.SubmitInvoiceAsync(invoice);

                    if (result.Success)
                    {
                        item.Status = QueueStatus.Completed;
                        item.SubmittedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        item.RetryCount++;
                        item.LastError = result.Error;
                        item.Status = QueueStatus.Pending;
                    }

                    await _queueRepository.UpdateAsync(item);
                }
                catch (RateLimitException)
                {
                    await Task.Delay(TimeSpan.FromSeconds(60));
                    break;
                }
            }
        }
        finally
        {
            _processLock.Release();
        }
    }
}
```

### Receipt Indicator
```xaml
<!-- Receipt template snippet -->
<TextBlock x:Name="ETimsStatus"
           Text="{Binding ETimsStatusText}"
           FontWeight="Bold"
           Foreground="{Binding ETimsStatusColor}"/>
<!-- Shows "eTIMS: Confirmed" (Green) or "eTIMS: Pending" (Yellow) -->
```

## Dependencies
- Story 18.2: KRA-Compliant Invoice Generation
- Story 18.3: Real-Time eTIMS Submission
- Epic 25: Offline-First Architecture

## Files to Create/Modify
- `HospitalityPOS.Core/Entities/ETimsOfflineQueue.cs`
- `HospitalityPOS.Infrastructure/Repositories/ETimsQueueRepository.cs`
- `HospitalityPOS.Business/Services/ETimsQueueService.cs`
- `HospitalityPOS.Infrastructure/BackgroundServices/ETimsQueueProcessor.cs`
- Modify receipt template for eTIMS status indicator
- Database migration for queue table

## Testing Requirements
- Unit tests for queue operations
- Integration tests simulating offline/online transitions
- Tests for FIFO ordering
- Tests for rate limit handling

## Definition of Done
- [ ] Offline transactions queued automatically
- [ ] Receipt shows pending/confirmed status
- [ ] Queue processes on reconnection
- [ ] Dashboard shows queue status
- [ ] FIFO order maintained
- [ ] Unit tests passing
- [ ] Code reviewed and approved
