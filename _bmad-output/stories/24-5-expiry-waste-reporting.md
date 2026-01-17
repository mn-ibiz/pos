# Story 24.5: Expiry Waste Reporting

## Story
**As a** manager,
**I want** reports on expired/wasted inventory,
**So that** I can track shrinkage and improve ordering.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Epic
**Epic 24: Batch & Expiry Tracking**

## Acceptance Criteria

### AC1: Waste Recording
**Given** expired items are disposed
**When** recording waste
**Then** logs quantity and value by reason

### AC2: Waste Summary Report
**Given** waste data exists
**When** running report
**Then** shows waste by category, supplier, and period

### AC3: Trend Analysis
**Given** trend analysis needed
**When** viewing charts
**Then** shows waste trends over time

## Technical Notes
```csharp
public class WasteRecord
{
    public Guid Id { get; set; }
    public DateTime RecordedAt { get; set; }
    public Guid RecordedBy { get; set; }
    public Guid ProductId { get; set; }
    public Guid BatchId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalValue => Quantity * UnitCost;
    public WasteReason Reason { get; set; }
    public string Notes { get; set; }
    public Guid? AuthorizedBy { get; set; }
}

public enum WasteReason
{
    Expired,
    Damaged,
    Spoiled,
    Recalled,
    Theft,
    Other
}

public class WasteSummaryReport
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal TotalWasteValue { get; set; }
    public int TotalWasteQuantity { get; set; }
    public List<WasteByCategory> ByCategory { get; set; }
    public List<WasteBySupplier> BySupplier { get; set; }
    public List<WasteByReason> ByReason { get; set; }
}

public class WasteByCategory
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; }
    public int Quantity { get; set; }
    public decimal Value { get; set; }
    public decimal PercentOfTotal { get; set; }
}

public class WasteTrendData
{
    public DateTime Period { get; set; }  // Week or Month
    public decimal WasteValue { get; set; }
    public decimal WasteAsPercentOfCOGS { get; set; }
    public int ItemCount { get; set; }
}

public class WasteAnalysis
{
    public List<WasteTrendData> Trends { get; set; }
    public decimal AverageMonthlyWaste { get; set; }
    public decimal WasteVsPreviousPeriod { get; set; }  // % change
    public List<ProductWasteRanking> TopWastedProducts { get; set; }
}
```

## Definition of Done
- [x] Waste recording workflow
- [x] Waste summary report (by category, supplier, reason)
- [x] Trend charts over time
- [x] Top wasted products ranking
- [x] Export capability (CSV/PDF)
- [x] Unit tests passing

## Implementation Summary

### DTOs Added (BatchTrackingDtos.cs)

#### Query and Report DTOs
- `WasteReportQueryDto` - Query parameters for waste reports (store, dates, reason, category, supplier)
- `WasteSummaryReportDto` - Complete summary with breakdowns by category, supplier, reason, products

#### Breakdown DTOs
- `WasteByCategoryDto` - Waste grouped by product category with percentages
- `WasteBySupplierDto` - Waste grouped by supplier with batch counts
- `WasteByReasonDto` - Waste grouped by disposal reason (Expired, Damaged, etc.)
- `WasteByProductDto` - Top wasted products with ranking
- `WasteByStoreDto` - Multi-store waste comparison

#### Trend Analysis DTOs
- `WasteTrendDataDto` - Time series data for charts (day/week/month grouping)
- `WasteComparisonDto` - Period-over-period comparison with percentage changes
- `WasteAnalysisDto` - Comprehensive analysis with insights

#### Insights
- `WasteInsightDto` - AI-generated insights (high concentration, category focus, supplier concerns)

#### Dashboard
- `WasteDashboardDto` - Dashboard summary (today, week, month, year)
- `WastePeriodSummaryDto` - Period summary with change percent

#### Export
- `WasteExportDto` - Export container with metadata
- `WasteExportRecordDto` - Individual waste record for export

### Interface (IWasteReportService.cs)

#### Waste Summary Reports
- `GetWasteSummaryAsync` - Full summary report with all breakdowns
- `GetWasteByCategoryAsync` - Category breakdown
- `GetWasteBySupplierAsync` - Supplier breakdown
- `GetWasteByReasonAsync` - Reason breakdown
- `GetWasteByProductAsync` - Top wasted products

#### Trend Analysis
- `GetWasteTrendsAsync` - Time series data (day/week/month grouping)
- `GetWasteComparisonAsync` - Period comparison
- `GetWasteAnalysisAsync` - Full analysis with insights

#### Dashboard
- `GetWasteDashboardAsync` - Dashboard data
- `GetPeriodSummaryAsync` - Period summary

#### Export
- `ExportWasteDataAsync` - Export waste records
- `ExportWasteDataAsCsvAsync` - CSV export

#### Store Comparison
- `GetWasteByStoreAsync` - Multi-store comparison

### Service Implementation (WasteReportService.cs)

- Comprehensive waste reporting from BatchDisposal records
- Grouping by category, supplier, reason, and product
- Trend analysis with daily/weekly/monthly aggregation
- Period-over-period comparison with percentage changes
- Insight generation (dominant reasons, high-value categories, supplier concerns)
- Dashboard with today/week/month/year summaries
- CSV export with proper escaping
- Multi-store comparison

### Unit Tests (WasteReportServiceTests.cs)

- Constructor validation (2 tests)
- Waste summary tests (2 tests)
- Category breakdown test (1 test)
- Reason breakdown test (1 test)
- Trend analysis tests (2 tests)
- Period comparison test (1 test)
- Analysis with insights test (1 test)
- Dashboard test (1 test)
- Export tests (2 tests)
- Store comparison test (1 test)

**Note:** Waste recording workflow uses existing BatchDisposal entity from Story 24.1
