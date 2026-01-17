# Story 21.5: Customer Purchase History

## Story
**As a** manager,
**I want to** view customer purchase history,
**So that** I can understand buying patterns.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Epic
**Epic 21: Advanced Loyalty Program**

## Acceptance Criteria

### AC1: Customer Profile View
**Given** loyalty member identified
**When** viewing profile
**Then** shows: total spend, visit count, average basket, top categories, tier status

### AC2: Transaction History
**Given** transaction history needed
**When** drilling down
**Then** lists all transactions with dates, amounts, and points earned

### AC3: Export Capability
**Given** personalization needed
**When** exporting data
**Then** can export customer data for marketing campaigns

## Technical Notes
```csharp
public class CustomerAnalytics
{
    public decimal TotalSpend { get; set; }
    public int VisitCount { get; set; }
    public decimal AverageBasket { get; set; }
    public List<CategorySpend> TopCategories { get; set; }
    public DateTime FirstVisit { get; set; }
    public DateTime LastVisit { get; set; }
    public int DaysSinceLastVisit { get; set; }
}
```

## Definition of Done
- [x] Customer profile view implemented
- [x] Transaction history displayed
- [x] Analytics calculated correctly
- [x] Export to CSV working
- [x] Unit tests passing

## Implementation Notes

### Files Modified

1. **LoyaltyDtos.cs** - Added new DTOs:
   - `CategorySpendDto` - Spending by category for analytics
   - `CustomerAnalyticsDto` - Comprehensive customer analytics
   - `CustomerExportFilterDto` - Filter options for export
   - `CustomerExportResult` - Export operation result

2. **ILoyaltyService.cs** - Added methods:
   - `GetCustomerAnalyticsAsync` - Get comprehensive analytics
   - `GetTopCategoriesAsync` - Get top spending categories
   - `ExportCustomerDataAsync` - Export customer data to CSV
   - `CalculateEngagementScoreAsync` - Calculate RFM engagement score

3. **LoyaltyService.cs** - Implemented:
   - Customer analytics calculation (average basket, visit frequency, etc.)
   - RFM-based engagement score (Recency, Frequency, Monetary)
   - CSV export with filters (tier, spend, date range)
   - Engagement levels: Champion, Loyal, Regular, At Risk, Dormant

4. **LoyaltyServiceTests.cs** - Added tests for all analytics methods

### Features

**Customer Analytics:**
- Total spend and visit count
- Average basket size
- Average days between visits
- Current tier and configuration
- Engagement score (0-100)

**Engagement Scoring (RFM Analysis):**
- Recency: 0-40 points based on days since last visit
- Frequency: 0-30 points based on visits per month
- Monetary: 0-30 points based on average basket size

**Export Capability:**
- CSV format with all member data
- Filterable by tier, spend, points, date range
- Includes active/inactive option

## Code Review: 2026-01-03

**Reviewer:** Claude Code (Adversarial Senior Developer Review)
**Verdict:** PASS (after fixes applied)

### Issues Found & Fixed (8 total):

| # | Severity | Issue | Fix Applied |
|---|----------|-------|-------------|
| 1 | Medium-High | Fire-and-forget `Task.Run` loses async context | Changed to `ThreadPool.QueueUserWorkItem` |
| 2 | Medium-High | Phone regex rejects Telkom Kenya (01xx) numbers | Updated regex to `^254[17]\d{8}$` |
| 3 | Medium | `GetTransactionHistoryAsync` uses sync `.ToList()` | Changed to `await .ToListAsync()` |
| 4 | Medium | No email format validation in `UpdateMemberAsync` | Added `IsValidEmail()` validation |
| 5 | Medium | `GetTransactionHistoryAsync` has no test coverage | Added 5 comprehensive tests |
| 6 | Low-Medium | `EnrollmentResult.Duplicate()` inconsistent DTO fields | Set both `Member` and `ExistingMember` |
| 7 | Medium | CSV export vulnerable to formula injection | Added formula character sanitization |
| 8 | Low | `UpdateMemberVisitAsync` silent failure on missing member | Changed return type to `Task<bool>` |

### Files Modified in Review:
- `src/HospitalityPOS.Core/DTOs/LoyaltyDtos.cs`
- `src/HospitalityPOS.Core/Interfaces/ILoyaltyService.cs`
- `src/HospitalityPOS.Infrastructure/Services/LoyaltyService.cs`
- `tests/HospitalityPOS.Business.Tests/Services/LoyaltyServiceTests.cs`

### New Tests Added:
- `GetTransactionHistoryAsync_WithValidMember_ShouldReturnTransactions`
- `GetTransactionHistoryAsync_WithDateFilters_ShouldFilterCorrectly`
- `GetTransactionHistoryAsync_WithMaxResultsLimit_ShouldRespectLimit`
- `GetTransactionHistoryAsync_WithNoTransactions_ShouldReturnEmptyList`
- `GetTransactionHistoryAsync_ShouldExcludeInactiveTransactions`
- Extended phone validation tests for Telkom Kenya
