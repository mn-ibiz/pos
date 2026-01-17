# Story 22.5: Store Data Synchronization

## Story
**As the** system,
**I want to** synchronize data between stores and HQ,
**So that** central and local systems are consistent.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Epic
**Epic 22: Multi-Store HQ Management**

## Acceptance Criteria

### AC1: Transaction Upload
**Given** store is online
**When** sync service runs
**Then** transactions uploaded to HQ database

### AC2: Master Data Download
**Given** HQ updates products/prices
**When** store syncs
**Then** changes downloaded and applied locally

### AC3: Conflict Resolution
**Given** sync conflicts occur
**When** detecting conflict
**Then** applies resolution rules (HQ wins for prices, store wins for transactions)

## Technical Notes
```csharp
public enum SyncDirection
{
    Upload,    // Store -> HQ
    Download,  // HQ -> Store
    Bidirectional
}

public class SyncConfiguration
{
    public Guid StoreId { get; set; }
    public int SyncIntervalSeconds { get; set; } = 30;
    public Dictionary<string, SyncDirection> EntitySyncRules { get; set; }
    // Products: Download, Transactions: Upload, Inventory: Bidirectional
}

public class SyncBatch
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public SyncDirection Direction { get; set; }
    public string EntityType { get; set; }
    public int RecordCount { get; set; }
    public SyncBatchStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class ConflictResolution
{
    public string EntityType { get; set; }
    public ConflictWinner Winner { get; set; }  // HQ, Store, LatestTimestamp
    public bool FlagForReview { get; set; }
}

public enum ConflictWinner
{
    HQ,
    Store,
    LatestTimestamp
}
```

## Definition of Done
- [x] Transaction upload to HQ working
- [x] Master data download working
- [x] Conflict resolution rules implemented
- [x] Sync status monitoring
- [x] Unit tests passing

## Implementation Summary

### Entities Created (SyncEntities.cs)
- **Enums**:
  - `SyncDirection` - Upload (Store→HQ), Download (HQ→Store), Bidirectional
  - `SyncBatchStatus` - Pending, InProgress, Completed, PartiallyCompleted, Failed, Cancelled
  - `ConflictWinner` - HQ, Store, LatestTimestamp, Manual
  - `SyncEntityType` - Product, Category, Price, Promotion, Receipt, Order, Inventory, StockMovement, Employee, Customer, LoyaltyMember

- **Entities**:
  - `SyncConfiguration` - Per-store sync settings (interval, batch size, retry policy)
  - `SyncEntityRule` - Entity-specific sync rules (direction, conflict resolution, priority)
  - `SyncBatch` - Batch of records being synchronized
  - `SyncRecord` - Individual record in a sync batch
  - `SyncConflict` - Detected conflicts requiring resolution
  - `SyncLog` - Log of sync operations

### DTOs Created (SyncDtos.cs)
- `SyncConfigurationDto` / `CreateSyncConfigurationDto` - Configuration management
- `SyncEntityRuleDto` / `CreateSyncEntityRuleDto` - Entity rule management
- `SyncBatchDto` - Batch status with progress tracking
- `SyncConflictDto` / `ResolveConflictDto` - Conflict management
- `SyncLogDto` / `SyncLogQueryDto` - Log querying
- `StoreSyncStatusDto` - Per-store sync status
- `ChainSyncDashboardDto` - Chain-wide sync dashboard
- `StartSyncRequestDto` / `SyncResultDto` - Sync operation request/response
- `UploadSyncPayloadDto` / `DownloadSyncPayloadDto` - Data payloads
- `SyncRecordDto` - Individual sync record

### EF Configurations Created (SyncConfiguration.cs)
- 6 entity configurations with proper indexes:
  - `IX_SyncConfigurations_StoreId` - Unique per store
  - `IX_SyncEntityRules_Config_Entity` - Composite for config+entity type
  - `IX_SyncBatches_Status`, `IX_SyncBatches_Store_Created`, `IX_SyncBatches_Direction`
  - `IX_SyncRecords_BatchId`, `IX_SyncRecords_Entity`
  - `IX_SyncConflicts_Resolved`, `IX_SyncConflicts_BatchId`, `IX_SyncConflicts_Entity`
  - `IX_SyncLogs_Timestamp`, `IX_SyncLogs_Store_Timestamp`, `IX_SyncLogs_Success`

### Service Interface (IStoreSyncService.cs)
- **Configuration Management**: CRUD for sync configurations
- **Entity Rules**: Add/update/remove entity-specific rules
- **Sync Operations**: StartSync, Upload, Download, Bidirectional, Cancel, Retry
- **Batch Management**: Get batches, active/pending/failed, cleanup old batches
- **Conflict Management**: Get/resolve conflicts, bulk resolution, counts by type
- **Status Monitoring**: Store status, chain dashboard, syncing check
- **Logging**: Query logs, error logs, operation logging
- **Data Payloads**: Create/process upload/download payloads
- **Auto-Sync**: Check/run auto-sync, stores needing sync
- **Statistics**: Per-store and chain-wide sync statistics

### Service Implementation (StoreSyncService.cs)
- 7 repository dependencies for comprehensive data access
- Full sync workflow implementation:
  1. Configuration-based sync enablement
  2. Entity rule priority ordering
  3. Batch creation and tracking
  4. Record-level processing
  5. Conflict detection and resolution
  6. Comprehensive logging
- Online status detection (synced within 5 minutes)
- Statistics aggregation with success rates

### Unit Tests (StoreSyncServiceTests.cs)
- 35+ comprehensive tests covering:
  - Constructor null checks (7 tests)
  - Configuration management (4 tests)
  - Entity rules (2 tests)
  - Sync operations (6 tests)
  - Batch management (3 tests)
  - Conflict management (4 tests)
  - Status monitoring (5 tests)
  - Logging (3 tests)
  - Auto-sync (3 tests)
  - Statistics (2 tests)
  - Data payloads (2 tests)

### Key Features
1. **Configurable Sync**: Per-store and per-entity configuration
2. **Batch Processing**: Trackable batches with progress monitoring
3. **Conflict Detection**: Automatic conflict detection with resolution rules
4. **Multiple Resolution Strategies**: HQ wins, Store wins, Latest timestamp, Manual
5. **Comprehensive Logging**: All operations logged with duration tracking
6. **Auto-Sync**: Interval-based automatic synchronization
7. **Statistics**: Detailed sync statistics for monitoring and reporting
8. **Chain Dashboard**: Chain-wide visibility into sync status

