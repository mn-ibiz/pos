# Story 25.4: Conflict Resolution

## Story
**As the** system,
**I want to** automatically resolve sync conflicts,
**So that** data integrity is maintained.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Epic
**Epic 25: Offline-First Architecture & Cloud Sync**

## Acceptance Criteria

### AC1: Transaction Conflicts
**Given** same record changed locally and remotely
**When** syncing
**Then** applies resolution rules (last-write-wins for transactions)

### AC2: Price Conflicts
**Given** price conflict (local vs HQ)
**When** resolving
**Then** HQ price wins, local change flagged for review

### AC3: Unresolvable Conflicts
**Given** unresolvable conflict
**When** detected
**Then** flags for manual resolution by manager

## Technical Notes
```csharp
public class SyncConflict
{
    public Guid Id { get; set; }
    public string EntityType { get; set; }
    public Guid EntityId { get; set; }
    public string LocalValue { get; set; }  // JSON
    public string RemoteValue { get; set; }  // JSON
    public DateTime LocalModifiedAt { get; set; }
    public DateTime RemoteModifiedAt { get; set; }
    public ConflictStatus Status { get; set; }
    public ConflictResolutionType Resolution { get; set; }
    public Guid? ResolvedBy { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string ResolutionNotes { get; set; }
}

public enum ConflictStatus
{
    Detected,
    AutoResolved,
    PendingManual,
    Resolved,
    Ignored
}

public enum ConflictResolutionType
{
    LocalWins,
    RemoteWins,
    LastWriteWins,
    Merged,
    Manual
}

public class ConflictResolutionRule
{
    public string EntityType { get; set; }
    public string PropertyName { get; set; }  // null = entire entity
    public ConflictResolutionType DefaultResolution { get; set; }
    public bool RequireManualReview { get; set; }
}

public static class DefaultConflictRules
{
    public static readonly List<ConflictResolutionRule> Rules = new()
    {
        new() { EntityType = "Receipt", DefaultResolution = ConflictResolutionType.LocalWins },
        new() { EntityType = "Product.Price", DefaultResolution = ConflictResolutionType.RemoteWins },
        new() { EntityType = "Inventory", DefaultResolution = ConflictResolutionType.LastWriteWins },
        new() { EntityType = "Customer", PropertyName = "PointsBalance", DefaultResolution = ConflictResolutionType.Manual }
    };
}

public interface IConflictResolutionService
{
    Task<ConflictResolutionType> ResolveAsync(SyncConflict conflict);
    Task<List<SyncConflict>> GetPendingConflictsAsync();
    Task ManualResolveAsync(Guid conflictId, ConflictResolutionType resolution, string notes);
}
```

## Definition of Done
- [x] Conflict detection during sync
- [x] Auto-resolution rules implemented
- [x] HQ-wins for pricing
- [x] Manual resolution queue
- [x] Conflict audit trail
- [x] Unit tests passing

## Implementation Summary

### DTOs Added (SyncDtos.cs)
- `ConflictStatus` enum - Detected, AutoResolved, PendingManual, Resolved, Ignored
- `ConflictResolutionType` enum - LocalWins, RemoteWins, LastWriteWins, Merged, Manual
- `ConflictResolutionRuleDto` - Rule for entity/property with priority and manual review flag
- `DefaultConflictRules` - Static class with default rules:
  - Receipts: LocalWins (financial integrity)
  - Product.Price: RemoteWins (HQ controls pricing)
  - Products: RemoteWins (HQ master data)
  - Inventory: LastWriteWins (most recent count)
  - Customer.PointsBalance: Manual (fraud prevention)
- `ConflictDetailDto` - Extended conflict with resolution details
- `ManualResolveConflictDto` - Request for manual resolution
- `ConflictResolutionResultDto` - Result with success/failure and resolved data
- `ConflictSummaryDto` - Dashboard summary with counts by status
- `ConflictQueryDto` - Query filters for conflicts
- `ConflictAuditDto` - Audit trail entry
- `CreateConflictRuleDto` - Create new resolution rule

### Interface Created (IConflictResolutionService.cs)
Comprehensive interface with:
- **Conflict Detection**: `DetectConflictAsync`, `GetConflictingFields`, `HasMeaningfulDifference`
- **Resolution**: `ResolveAsync`, `ResolveByIdAsync`, `ManualResolveAsync`, `IgnoreConflictAsync`, `ApplyResolutionAsync`
- **Queries**: `GetPendingConflictsAsync`, `GetConflictByIdAsync`, `QueryConflictsAsync`, `GetConflictCountByStatusAsync`, `GetConflictSummaryAsync`
- **Rules**: `GetApplicableRule`, `GetAllRules`, `AddOrUpdateRule`, `RemoveRule`, `ResetToDefaultRules`
- **Audit**: `GetConflictAuditTrailAsync`, `LogAuditAsync`
- **Batch Operations**: `AutoResolveAllAsync`, `BulkResolveAsync`, `PurgeResolvedConflictsAsync`

### Service Implementation (ConflictResolutionService.cs)
Full conflict resolution service including:
- JSON-based conflict detection with field-level comparison
- Rule-based auto-resolution with priority ordering
- Property-specific rules (e.g., Product.Price vs Product)
- Manual resolution with user tracking
- Ignore functionality for non-critical conflicts
- In-memory audit trail logging
- Bulk operations for efficiency
- Soft-delete purge for old conflicts

### Unit Tests (ConflictResolutionServiceTests.cs)
Comprehensive test coverage (45+ tests) including:
- Constructor validation tests
- Conflict detection tests (null handling, identical data, different data)
- Field comparison tests
- Resolution tests (LocalWins, RemoteWins, LastWriteWins, Manual)
- Manual resolution tests
- Ignore conflict tests
- Query and filter tests
- Resolution rules tests (get, add, update, remove, reset)
- Audit trail tests
- Batch operation tests
- DTO tests
