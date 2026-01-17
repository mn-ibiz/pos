# Story 25.1: Local Database Setup

## Story
**As the** system,
**I want** all operations to work against a local database,
**So that** internet outage doesn't stop business.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Epic
**Epic 25: Offline-First Architecture & Cloud Sync**

## Acceptance Criteria

### AC1: Local Database Creation
**Given** POS is installed
**When** initializing
**Then** SQL Server Express local database is created

### AC2: Local-First Operations
**Given** local database exists
**When** performing any operation
**Then** all data is stored locally first

### AC3: Offline Operation
**Given** internet is unavailable
**When** using POS
**Then** all core functions work without interruption

## Technical Notes
```csharp
public class LocalDatabaseConfiguration
{
    public string ConnectionString { get; set; }
    public string DatabaseName { get; set; } = "POS_Local";
    public string DataDirectory { get; set; }
    public bool AutoBackup { get; set; } = true;
    public int BackupRetentionDays { get; set; } = 7;
}

public interface ILocalDatabaseService
{
    Task<bool> InitializeDatabaseAsync();
    Task<bool> ValidateDatabaseSchemaAsync();
    Task<bool> ApplyMigrationsAsync();
    Task<DatabaseStatus> GetStatusAsync();
    Task<bool> BackupDatabaseAsync(string backupPath);
    Task<bool> RestoreDatabaseAsync(string backupPath);
}

public class DatabaseStatus
{
    public bool IsInitialized { get; set; }
    public string Version { get; set; }
    public long SizeBytes { get; set; }
    public DateTime LastBackup { get; set; }
    public int PendingSyncItems { get; set; }
}

public class OfflineCapability
{
    public static readonly List<string> OfflineOperations = new()
    {
        "CreateOrder",
        "SettleReceipt",
        "ProcessPayment",
        "PrintReceipt",
        "ManageInventory",
        "ViewReports"
    };
}
```

## Definition of Done
- [x] SQL Server Express auto-installation/configuration
- [x] Database initialization on first run
- [x] Schema migration system
- [x] All core operations work offline
- [x] Auto-backup functionality
- [x] Unit tests passing

## Implementation Summary

### DTOs Created (SyncDtos.cs)
- `LocalDatabaseConfiguration` - Configuration for local database settings
- `DatabaseStatusDto` - Status information including version, size, online status
- `DatabaseOperationResult` - Result of database operations with success/failure
- `BackupInfoDto` - Backup file information including path, size, date
- `SyncQueueStatus`, `SyncPriority`, `SyncOperationType` - Sync-related enums
- `SyncQueueItemDto`, `CreateSyncQueueItemDto` - Sync queue item DTOs
- `SyncSummaryDto`, `SyncProgressDto` - Sync progress tracking
- `OfflineCapabilityDto`, `OfflineOperations` - Offline capability definitions
- `ConnectionStatusDto`, `SyncDashboardDto` - Connection and dashboard DTOs

### Interface Created (ILocalDatabaseService.cs)
Full interface with methods for:
- Database initialization: `InitializeDatabaseAsync`, `ValidateDatabaseSchemaAsync`, `ApplyMigrationsAsync`, `DatabaseExistsAsync`
- Status: `GetStatusAsync`, `GetDatabaseVersionAsync`, `GetPendingSyncCountAsync`
- Backup/Restore: `BackupDatabaseAsync`, `RestoreDatabaseAsync`, `GetAvailableBackupsAsync`, `CleanupOldBackupsAsync`, `GetLastBackupDateAsync`
- Configuration: `GetConfiguration`, `UpdateConfiguration`
- Maintenance: `OptimizeDatabaseAsync`, `CheckIntegrityAsync`, `GetDatabaseSizeAsync`

### Service Implementation (LocalDatabaseService.cs)
Full implementation including:
- Database initialization with directory creation
- Schema validation with migration check
- Auto-migration with pre-backup option
- SQL Server backup/restore via T-SQL commands
- Backup retention cleanup
- Database optimization (index rebuild, statistics update)
- Integrity checking via DBCC CHECKDB

### Entity Created (SyncEntities.cs)
- `SyncQueueItemStatus` enum - Pending, InProgress, Completed, Failed, Cancelled, Conflict
- `SyncQueuePriority` enum - Low, Normal, High, Critical
- `SyncQueueOperationType` enum - Create, Update, Delete
- `SyncQueueItem` entity - Full entity for sync queue with retry tracking

### DbContext Updated (POSDbContext.cs)
- Added `DbSet<SyncQueueItem> SyncQueues` for sync queue persistence

### Unit Tests (LocalDatabaseServiceTests.cs)
Comprehensive test coverage including:
- Constructor validation tests
- Configuration management tests
- Database initialization tests
- Database status tests
- Backup management tests (list, cleanup, last backup date)
- Restore validation tests
- Schema validation tests
- Sync queue counting tests
- Integration workflow tests
