# Story 22.4: Consolidated Chain Reporting

## Story
**As an** HQ manager,
**I want to** get consolidated reports across all stores,
**So that** I can monitor chain performance.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Epic
**Epic 22: Multi-Store HQ Management**

## Acceptance Criteria

### AC1: Real-Time Chain Dashboard
**Given** HQ dashboard access
**When** viewing sales
**Then** shows real-time totals across all stores

### AC2: Store Comparison
**Given** comparison needed
**When** running store comparison report
**Then** ranks stores by sales, margin, basket size

### AC3: Product Performance Analysis
**Given** product performance analysis
**When** running report
**Then** shows chain-wide sales by product with store breakdown

## Technical Notes
```csharp
public class ChainDashboardMetrics
{
    public decimal TotalSalesToday { get; set; }
    public decimal TotalSalesWeek { get; set; }
    public decimal TotalSalesMonth { get; set; }
    public int TransactionCountToday { get; set; }
    public decimal AverageBasketSize { get; set; }
    public List<StoreSummary> StoreBreakdown { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class StoreSummary
{
    public Guid StoreId { get; set; }
    public string StoreName { get; set; }
    public decimal Sales { get; set; }
    public int Transactions { get; set; }
    public decimal AverageBasket { get; set; }
    public bool IsOnline { get; set; }
    public DateTime LastSync { get; set; }
}

public class StoreComparisonReport
{
    public string Period { get; set; }
    public List<StoreRanking> Rankings { get; set; }
}

public class StoreRanking
{
    public int Rank { get; set; }
    public Guid StoreId { get; set; }
    public string StoreName { get; set; }
    public decimal Sales { get; set; }
    public decimal Margin { get; set; }
    public decimal AverageBasket { get; set; }
    public decimal GrowthPercent { get; set; }
}
```

## Definition of Done
- [x] HQ dashboard with chain-wide metrics
- [x] Store comparison reports working
- [x] Product performance reports by store
- [x] Export capability (CSV/PDF)
- [x] Unit tests passing

## Implementation Summary

### DTOs Created (ChainReportingDtos.cs)
- `ChainDashboardMetricsDto` - Chain-wide dashboard with today/week/month metrics, store/product breakdown
- `StoreSummaryDto` - Individual store performance summary
- `StoreComparisonReportDto` - Store ranking report with comparison metrics
- `StoreRankingDto` - Ranked store with growth, contribution, rank changes
- `ProductPerformanceReportDto` - Product performance across chain
- `ProductPerformanceDto` - Product metrics with store breakdown
- `ProductStoreBreakdownDto` - Product performance at specific store
- `CategoryPerformanceReportDto` - Category performance report
- `CategoryPerformanceDto` - Category metrics
- `CategoryStoreBreakdownDto` - Category performance at store
- `TopProductDto` / `TopCategoryDto` - Summary DTOs for dashboard
- `DailySalesTrendDto` / `HourlySalesPatternDto` - Trend data points
- `SalesTrendReportDto` - Sales trend report
- `ChainReportQueryDto` - Query parameters for filtering
- `ChainInventoryStatusDto` - Inventory status across chain
- `StoreInventorySummaryDto` - Inventory summary per store
- `LowStockAlertDto` - Low stock alert details
- `PaymentMethodBreakdownDto` - Payment method analytics
- `ChainFinancialSummaryDto` - Financial summary

### Service Interface (IChainReportingService.cs)
- Dashboard operations (GetChainDashboard, GetAllStoresSummary)
- Store comparison (GetStoreComparisonReport, GetTopPerformingStores, GetUnderperformingStores)
- Product performance (GetProductPerformanceReport, GetTopSellingProducts)
- Category performance (GetCategoryPerformanceReport, GetCategoryPerformanceByStore)
- Sales trends (GetSalesTrendReport, GetDailySalesTrends, GetHourlySalesPatterns)
- Financial summary (GetChainFinancialSummary, GetPaymentMethodBreakdown)
- Inventory (GetChainInventoryStatus, GetLowStockAlerts)

### Service Implementation (ChainReportingService.cs)
- 7 repository dependencies for comprehensive data access
- Real-time chain dashboard metrics calculation
- Store ranking with period comparison and growth calculation
- Product and category performance with store breakdown
- Daily and hourly sales trend analysis
- Financial summary with payment method breakdown
- Inventory status with low stock alerts

### Unit Tests (ChainReportingServiceTests.cs)
- 25+ comprehensive tests covering:
  - Constructor null checks
  - Chain dashboard metrics
  - Store summaries and comparison
  - Product and category performance
  - Sales trends and patterns
  - Financial summaries
  - Inventory status and alerts

### Key Features
1. **Real-Time Dashboard**: Today/week/month metrics with store breakdown
2. **Store Ranking**: Rankings with growth %, contribution %, rank changes
3. **Product Analytics**: Chain-wide and per-store product performance
4. **Category Analytics**: Category contribution with store breakdown
5. **Sales Trends**: Daily and hourly patterns for trend analysis
6. **Financial Summary**: Gross/net sales, discounts, returns, payment breakdown
7. **Inventory Alerts**: Low stock and out-of-stock alerts across chain
8. **Online Status**: Store online/offline tracking based on sync time
