# Story 24.3: Expired Item Blocking

## Story
**As the** system,
**I want to** prevent sale of expired items,
**So that** food safety is maintained.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Epic
**Epic 24: Batch & Expiry Tracking**

## Acceptance Criteria

### AC1: Sale Blocking
**Given** item batch has expired
**When** attempting to sell
**Then** system blocks sale with "Item expired" message

### AC2: Category Configuration
**Given** blocking is enabled
**When** configuring
**Then** admin can enable/disable per category

### AC3: Manager Override
**Given** override needed
**When** manager authorizes
**Then** sale proceeds with audit log entry

## Technical Notes
```csharp
public class ExpiredItemCheck
{
    public Guid ProductId { get; set; }
    public bool IsExpired { get; set; }
    public DateTime? EarliestExpiry { get; set; }
    public int DaysExpired { get; set; }
    public bool BlockingEnabled { get; set; }
    public bool RequiresOverride { get; set; }
}

public class ExpirySaleBlock
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid BatchId { get; set; }
    public DateTime ExpiryDate { get; set; }
    public Guid AttemptedBy { get; set; }
    public DateTime AttemptedAt { get; set; }
    public bool OverrideApplied { get; set; }
    public Guid? OverrideBy { get; set; }
    public string OverrideReason { get; set; }
}

public class CategoryExpirySettings
{
    public Guid CategoryId { get; set; }
    public bool RequiresExpiryTracking { get; set; }
    public bool BlockExpiredSales { get; set; }
    public bool AllowManagerOverride { get; set; }
    public int WarningDays { get; set; }
}

public interface IExpiryValidationService
{
    Task<ExpiredItemCheck> ValidateProductForSaleAsync(Guid productId);
    Task<bool> ProcessOverrideAsync(Guid productId, Guid managerId, string reason);
}
```

## Definition of Done
- [x] Expired item detection at checkout
- [x] Sale blocking with clear message
- [x] Category-level configuration
- [x] Manager override with PIN
- [x] Audit logging of overrides
- [x] Unit tests passing

## Implementation Summary

### Entities Added (BatchTrackingEntities.cs)

#### ExpirySaleBlock
- ProductId, BatchId, StoreId references
- ExpiryDate, DaysExpired for tracking
- AttemptedByUserId, AttemptedAt, AttemptedQuantity
- WasBlocked, OverrideApplied flags
- OverrideByUserId, OverrideAt, OverrideReason for audit
- ReceiptId for linking to completed sales

#### CategoryExpirySettings
- CategoryId, RequiresExpiryTracking
- BlockExpiredSales, AllowManagerOverride
- WarningDays, CriticalDays thresholds
- ExpiredItemAction, NearExpiryAction (ExpiryAction enum)
- MinimumShelfLifeDaysOnReceipt

### DTOs Added (BatchTrackingDtos.cs)

- `ExpiredItemCheckDto` - Product expiry check result with expired batch list
- `ExpiredBatchInfoDto` - Individual expired batch details
- `ExpirySaleBlockDto` - Sale block record with full audit trail
- `CreateExpirySaleBlockDto` - Create new sale block record
- `ExpirySaleOverrideRequestDto` - Manager override request with PIN
- `ExpirySaleOverrideResultDto` - Override approval result
- `CategoryExpirySettingsDto` - Category expiry configuration
- `UpdateCategoryExpirySettingsDto` - Update category settings
- `SaleBlockQueryDto` - Query filters for sale blocks
- `SaleBlockSummaryDto` - Summary statistics for reporting
- `TopBlockedProductDto` - Most frequently blocked products

### Interface (IExpiryValidationService.cs)

#### Expiry Validation
- `ValidateProductForSaleAsync` - Validate product at checkout
- `ValidateBatchForSaleAsync` - Validate specific batch
- `IsBlockingEnabledAsync` - Check if blocking is enabled
- `IsOverrideAllowedAsync` - Check if override is permitted

#### Sale Blocking
- `RecordSaleBlockAsync` - Record blocked sale attempt
- `ProcessOverrideAsync` - Process manager override
- `LinkSaleBlockToReceiptAsync` - Link block to receipt
- `GetSaleBlockAsync` - Get single block record
- `GetSaleBlocksAsync` - Query block records
- `GetSaleBlockSummaryAsync` - Get summary statistics

#### Category Settings
- `GetCategorySettingsAsync` - Get category settings
- `GetAllCategorySettingsAsync` - Get all category settings
- `SaveCategorySettingsAsync` - Save/update category settings
- `DeleteCategorySettingsAsync` - Delete category settings
- `GetEffectiveSettingsAsync` - Get combined settings (product > category > default)

#### Override Audit
- `GetOverrideHistoryAsync` - Get override history
- `GetOverrideStatsByManagerAsync` - Get stats by manager

### Service Implementation (ExpiryValidationService.cs)

- Hierarchical settings resolution: Product → Category → Default
- Severity calculation based on days until expiry
- Sale blocking with clear "Item expired" messages
- Manager override with PIN verification
- Audit logging of all block attempts and overrides
- Receipt linking for completed override sales
- Summary reporting for blocked/override statistics

### EF Configuration (BatchTrackingConfiguration.cs)

- `ExpirySaleBlockConfiguration` - Indexes on ProductId, BatchId, StoreId, AttemptedAt
- `CategoryExpirySettingsConfiguration` - Unique index on CategoryId

### Unit Tests (ExpiryValidationServiceTests.cs)

- Constructor validation (2 tests)
- ValidateProductForSaleAsync tests (4 tests)
- ProcessOverrideAsync tests (4 tests)
- RecordSaleBlockAsync test (1 test)
- Category settings tests (3 tests)
- GetEffectiveSettingsAsync tests (3 tests)
- GetSaleBlockSummaryAsync test (1 test)
- GetOverrideHistoryAsync test (1 test)
