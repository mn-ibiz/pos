# Story 39.6: Offline Operation with Sync Queue

Status: Done

## Story

As a **store operating in an area with unreliable internet**,
I want **the POS to continue full operation when offline and automatically sync when connectivity returns**,
so that **business operations are never interrupted by network outages**.

## Business Context

**CRITICAL - OPERATIONAL CONTINUITY**

Kenya experiences:
- Frequent power outages (load shedding in some areas)
- Unreliable internet connectivity
- Network downtime during peak hours

Current architecture relies on SQL Server Express (local), but lacks:
- Defined offline queue mechanism for external services (eTIMS, M-Pesa)
- Conflict resolution strategy
- Sync status visibility

**Market Reality:** ConnectPOS, ROBOTILL, Vega POS all have robust offline modes as selling points.

**Business Impact:** Network outage during peak hours = lost sales, frustrated customers.

## Acceptance Criteria

### AC1: Full Offline POS Operation
- [ ] All POS functions work without internet (sales, payments, receipts)
- [ ] Cash payments process normally offline
- [ ] Manual M-Pesa entry works offline (STK Push queued)
- [ ] Receipts print normally offline
- [ ] No blocking errors for network-dependent features

### AC2: Online/Offline Status Indicator
- [ ] Clear visual indicator in UI showing connectivity status
- [ ] Green = Online, Yellow = Degraded, Red = Offline
- [ ] Status visible on main POS screen
- [ ] Tooltip shows last sync time

### AC3: eTIMS Offline Queue
- [ ] eTIMS submissions queue locally when offline
- [ ] Queue stored in SyncQueue table
- [ ] Visual indicator shows pending eTIMS count
- [ ] Auto-submit when online

### AC4: M-Pesa Verification Queue
- [ ] M-Pesa status queries queue when offline
- [ ] STK Push requests marked as "pending verification"
- [ ] Auto-verify when online

### AC5: Automatic Sync on Reconnect
- [ ] Background service monitors connectivity
- [ ] Sync begins within 30 seconds of reconnection
- [ ] Queued items processed in FIFO order
- [ ] Failed items retry with exponential backoff

### AC6: Sync Status Dashboard
- [ ] Dashboard widget shows pending sync items
- [ ] Breakdown by type (eTIMS, M-Pesa, etc.)
- [ ] Manual "Sync Now" button
- [ ] Sync history log

### AC7: Conflict Resolution
- [ ] Handle concurrent edits gracefully
- [ ] Last-write-wins for most data
- [ ] Conflicts logged for manual review
- [ ] No data loss

### AC8: Offline Receipt Numbering
- [ ] Receipt numbers unique even across offline periods
- [ ] Include terminal/device ID in receipt number
- [ ] No duplicate receipt numbers possible

## Tasks / Subtasks

- [ ] **Task 1: Connectivity Monitoring Service** (AC: 2, 5)
  - [ ] 1.1 Create IConnectivityService interface
  - [ ] 1.2 Implement ping-based connectivity check (check eTIMS endpoint)
  - [ ] 1.3 Implement status states: Online, Degraded, Offline
  - [ ] 1.4 Raise events on status change
  - [ ] 1.5 Check interval: every 30 seconds
  - [ ] 1.6 Unit tests for status transitions

- [ ] **Task 2: Sync Queue Infrastructure** (AC: 3, 4, 5)
  - [ ] 2.1 Create SyncQueue table (QueueType, EntityType, EntityId, Payload, Status, RetryCount)
  - [ ] 2.2 Create SyncQueueItem entity
  - [ ] 2.3 Create ISyncQueueService interface
  - [ ] 2.4 Implement AddToQueue(type, entityType, entityId, payload)
  - [ ] 2.5 Implement GetPendingItems(type)
  - [ ] 2.6 Implement MarkCompleted(id)
  - [ ] 2.7 Implement MarkFailed(id, error)
  - [ ] 2.8 Unit tests

- [ ] **Task 3: Sync Processor Background Service** (AC: 5)
  - [ ] 3.1 Create SyncProcessorService as HostedService/Timer
  - [ ] 3.2 Subscribe to connectivity status changes
  - [ ] 3.3 On reconnect: trigger sync
  - [ ] 3.4 Process queue items by type in order
  - [ ] 3.5 Implement exponential backoff (1s, 2s, 4s, 8s, max 5 retries)
  - [ ] 3.6 Log all sync activities
  - [ ] 3.7 Integration tests

- [ ] **Task 4: eTIMS Offline Integration** (AC: 3)
  - [ ] 4.1 Modify EtimsService to check connectivity
  - [ ] 4.2 If offline: queue invoice submission
  - [ ] 4.3 Mark receipt as "eTIMS Pending"
  - [ ] 4.4 Register queue handler for eTIMS type
  - [ ] 4.5 On sync: submit queued invoices
  - [ ] 4.6 Update receipt status on success

- [ ] **Task 5: M-Pesa Offline Integration** (AC: 4)
  - [ ] 5.1 Modify MpesaService to handle offline
  - [ ] 5.2 If offline during STK Push: use manual entry flow
  - [ ] 5.3 Queue transaction status queries
  - [ ] 5.4 On sync: verify pending transactions
  - [ ] 5.5 Update payment status on verification

- [ ] **Task 6: UI Status Indicator** (AC: 2)
  - [ ] 6.1 Create ConnectivityStatusControl.xaml
  - [ ] 6.2 Bind to IConnectivityService.Status
  - [ ] 6.3 Color indicators (green/yellow/red)
  - [ ] 6.4 Tooltip with last sync time
  - [ ] 6.5 Add to MainWindow status bar
  - [ ] 6.6 Add to POS header

- [ ] **Task 7: Sync Status Dashboard** (AC: 6)
  - [ ] 7.1 Create SyncStatusWidget.xaml
  - [ ] 7.2 Display pending items by type
  - [ ] 7.3 "Sync Now" button to trigger manual sync
  - [ ] 7.4 Sync history with timestamps
  - [ ] 7.5 Failed items with error details

- [ ] **Task 8: Offline Receipt Numbering** (AC: 8)
  - [ ] 8.1 Modify receipt number format: {TerminalId}-{Date}-{Sequence}
  - [ ] 8.2 Store terminal ID in local config
  - [ ] 8.3 Maintain local sequence counter
  - [ ] 8.4 Reset sequence daily
  - [ ] 8.5 Test uniqueness across terminals

- [ ] **Task 9: Conflict Resolution** (AC: 7)
  - [ ] 9.1 Implement last-write-wins strategy
  - [ ] 9.2 Add UpdatedAt timestamp to critical entities
  - [ ] 9.3 Log conflicts to SyncConflictLog table
  - [ ] 9.4 Admin view for reviewing conflicts
  - [ ] 9.5 Ensure no data loss

## Dev Notes

### Offline Architecture

```
[POS Application]
    ↓
[SQL Server Express (LOCAL)]  ← All data stored locally
    ↓
[SyncQueue Table]  ← Pending external submissions
    ↓
[ConnectivityService]  ← Monitors internet status
    ↓
[SyncProcessor]  ← Processes queue when online
    ↓
[External Services] ← eTIMS, M-Pesa, etc.
```

### Connectivity Check Logic

```csharp
public class ConnectivityService : IConnectivityService
{
    private readonly Timer _timer;
    private ConnectivityStatus _status = ConnectivityStatus.Unknown;

    public async Task<ConnectivityStatus> CheckConnectivityAsync()
    {
        try
        {
            // Try eTIMS endpoint (critical service)
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = await client.GetAsync("https://etims.kra.go.ke/health");

            if (response.IsSuccessStatusCode)
                return ConnectivityStatus.Online;
            else
                return ConnectivityStatus.Degraded;
        }
        catch (Exception)
        {
            return ConnectivityStatus.Offline;
        }
    }
}
```

### Database Schema (from Gap Analysis)

```sql
CREATE TABLE SyncQueue (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    QueueType NVARCHAR(50) NOT NULL, -- EtimsInvoice, MpesaVerify, CloudBackup
    EntityType NVARCHAR(50) NOT NULL, -- Receipt, Payment, etc.
    EntityId INT NOT NULL,
    Payload NVARCHAR(MAX), -- JSON data to sync
    Status NVARCHAR(20) DEFAULT 'Pending', -- Pending, Processing, Completed, Failed
    RetryCount INT DEFAULT 0,
    MaxRetries INT DEFAULT 5,
    LastError NVARCHAR(500),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    ProcessedAt DATETIME2
);

CREATE INDEX IX_SyncQueue_Pending ON SyncQueue (QueueType, Status, CreatedAt)
    WHERE Status = 'Pending';

CREATE TABLE SyncConflictLog (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    EntityType NVARCHAR(50) NOT NULL,
    EntityId INT NOT NULL,
    ConflictType NVARCHAR(50), -- ConcurrentEdit, VersionMismatch
    LocalData NVARCHAR(MAX),
    RemoteData NVARCHAR(MAX),
    Resolution NVARCHAR(50), -- LastWriteWins, Manual
    ResolvedByUserId INT,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    ResolvedAt DATETIME2
);
```

### Receipt Number Format

```
Format: {TerminalId}-{YYYYMMDD}-{Sequence}
Example: T01-20260116-00001

Components:
- TerminalId: 3-char terminal identifier (T01, T02, etc.)
- Date: YYYYMMDD format
- Sequence: 5-digit daily sequence, reset at midnight

Benefits:
- Unique across terminals
- Unique across days
- Sortable by date
- Identifiable source terminal
```

### Sync Priority Order

1. eTIMS invoices (legal compliance)
2. M-Pesa verifications (payment accuracy)
3. Cloud backup (data safety)
4. Multi-branch sync (operational)

### Architecture Compliance

- **Layer:** Infrastructure (ConnectivityService), Business (SyncQueueService)
- **Pattern:** Background service, event-driven
- **Resilience:** Exponential backoff, max retries
- **Logging:** All sync activities logged

### References

- [Source: _bmad-output/feature-gap-analysis-2026-01-16.md#2.6-Offline-Operation-with-Sync]
- [Source: _bmad-output/architecture.md#System-Architecture]
- [Source: _bmad-output/stories/39-1-etims-api-integration.md] (dependency)
- [Source: _bmad-output/stories/39-2-mpesa-daraja-stk-push.md] (dependency)

### Dependencies

- **Integrates with:** Story 39-1 (eTIMS), Story 39-2 (M-Pesa)
- **Should be implemented after:** Core eTIMS and M-Pesa stories

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

### File List
