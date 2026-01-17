# Story 24.2: Expiry Alert Dashboard

## Story
**As a** manager,
**I want to** see items approaching expiry,
**So that** I can take action before products expire.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Epic
**Epic 24: Batch & Expiry Tracking**

## Acceptance Criteria

### AC1: Expiry Timeline View
**Given** items have expiry dates
**When** viewing dashboard
**Then** shows items expiring in 7, 14, 30 days

### AC2: Action Suggestions
**Given** item is near expiry
**When** alerting
**Then** suggests markdown or removal from shelf

### AC3: Expired Item Flagging
**Given** item has expired
**When** detected
**Then** prominently flagged for disposal

## Technical Notes
```csharp
public class ExpiryDashboard
{
    public List<ExpiryGroup> ExpiryGroups { get; set; }
    public int TotalExpiredItems { get; set; }
    public decimal TotalExpiredValue { get; set; }
    public int TotalExpiringItems { get; set; }
    public decimal TotalExpiringValue { get; set; }
}

public class ExpiryGroup
{
    public string Period { get; set; }  // "Already Expired", "7 Days", "14 Days", "30 Days"
    public int ItemCount { get; set; }
    public decimal TotalValue { get; set; }
    public List<ExpiringBatch> Batches { get; set; }
}

public class ExpiringBatch
{
    public Guid BatchId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; }
    public string BatchNumber { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int RemainingQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalValue => RemainingQuantity * UnitCost;
    public int DaysToExpiry { get; set; }
    public ExpiryAlertSeverity Severity { get; set; }
    public List<SuggestedAction> SuggestedActions { get; set; }
}

public enum ExpiryAlertSeverity
{
    Info,       // 30+ days
    Warning,    // 14-30 days
    Urgent,     // 7-14 days
    Critical,   // 0-7 days
    Expired     // Past expiry
}

public enum SuggestedAction
{
    Markdown,
    RemoveFromShelf,
    Dispose,
    ReturnToSupplier
}
```

## Definition of Done
- [x] Expiry dashboard with timeline grouping
- [x] Color-coded severity indicators
- [x] Action suggestions displayed
- [x] Expired items prominently highlighted
- [x] Export capability
- [x] Unit tests passing

## Implementation Summary

### DTOs Added (BatchTrackingDtos.cs)

#### Enums
- `ExpiryAlertSeverity` - Info, Warning, Urgent, Critical, Expired (with days thresholds)
- `SuggestedAction` - Markdown, RemoveFromShelf, Dispose, ReturnToSupplier, PrioritizeSale, Transfer

#### DTOs
- `ExpiryDashboardDto` - Main dashboard data with totals and grouped batches
- `ExpiryDashboardSummaryDto` - Count of batches by severity
- `ExpiryGroupDto` - Timeline group (Already Expired, 0-7 days, 7-14 days, 14-30 days, 30-90 days)
- `ExpiringBatchDto` - Batch with expiry info, severity, and suggested actions
- `ExpiryDashboardQueryDto` - Query filters for dashboard (store, days, severity)
- `ExpiryAlertDto` - Individual expiry alert with severity and actions
- `ExpiryExportDto` - Export format with all batch details and actions

### Service Methods Added (ProductBatchService.cs)

#### Expiry Dashboard Region
- `GetExpiryDashboardAsync` - Returns full dashboard with timeline grouping
- `GetExpiryAlertsAsync` - Returns alerts filtered by store/severity
- `GetSuggestedActionsAsync` - Returns context-aware actions based on expiry status
- `AcknowledgeAlertAsync` - Marks alert as acknowledged
- `GetExpiryExportDataAsync` - Returns export-ready batch data
- `GetExpirySummaryAsync` - Returns counts by severity

### Severity Thresholds
- **Expired**: Past expiry date
- **Critical**: 0-7 days until expiry
- **Urgent**: 7-14 days until expiry
- **Warning**: 14-30 days until expiry
- **Info**: 30+ days until expiry

### Suggested Actions Logic
- **Expired batches**: Dispose, RemoveFromShelf, ReturnToSupplier
- **Critical batches**: Markdown, PrioritizeSale, RemoveFromShelf
- **Urgent batches**: Markdown, PrioritizeSale, Transfer
- **Warning batches**: PrioritizeSale, Transfer
- **Info batches**: No action needed

### Unit Tests Added (ProductBatchServiceTests.cs)
- `GetExpiryDashboardAsync_ReturnsGroupedBatches`
- `GetSuggestedActionsAsync_ForExpiredBatch_ReturnsDisposeAndRemove`
- `GetSuggestedActionsAsync_ForCriticalBatch_ReturnsMarkdownAndPrioritize`
- `GetExpirySummaryAsync_ReturnsCorrectCounts`
- `GetExpiryExportDataAsync_ReturnsFormattedExportData`
- `GetExpiryAlertsAsync_ReturnsAlertsOrderedByExpiry`
