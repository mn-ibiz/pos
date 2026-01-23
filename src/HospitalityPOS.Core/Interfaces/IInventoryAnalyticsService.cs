using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for inventory analytics and reporting.
/// </summary>
public interface IInventoryAnalyticsService
{
    #region Stock Valuation Methods

    /// <summary>
    /// Gets stock valuation configuration.
    /// </summary>
    Task<StockValuationConfig?> GetStockValuationConfigAsync(int storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves stock valuation configuration.
    /// </summary>
    Task<StockValuationConfig> SaveStockValuationConfigAsync(StockValuationConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates stock valuation using specified method.
    /// </summary>
    Task<StockValuationResult> CalculateStockValuationAsync(int storeId, StockValuationMethod method, DateTime? asOfDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a stock valuation snapshot.
    /// </summary>
    Task<StockValuationSnapshot> CreateValuationSnapshotAsync(int storeId, StockValuationMethod method, string? period = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets stock valuation snapshots.
    /// </summary>
    Task<IEnumerable<StockValuationSnapshot>> GetValuationSnapshotsAsync(int storeId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets stock valuation snapshot details.
    /// </summary>
    Task<StockValuationSnapshot?> GetValuationSnapshotDetailAsync(int snapshotId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates product cost using FIFO method.
    /// </summary>
    Task<ProductCostResult> CalculateFIFOCostAsync(int storeId, int productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates product cost using weighted average method.
    /// </summary>
    Task<ProductCostResult> CalculateWeightedAverageCostAsync(int storeId, int productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates standard costs for all products.
    /// </summary>
    Task<StandardCostUpdateResult> UpdateStandardCostsAsync(int storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compares valuation across different methods.
    /// </summary>
    Task<ValuationComparisonResult> CompareValuationMethodsAsync(int storeId, CancellationToken cancellationToken = default);

    #endregion

    #region Automatic Reorder Generation

    /// <summary>
    /// Gets reorder rule for a product.
    /// </summary>
    Task<ReorderRule?> GetReorderRuleAsync(int storeId, int productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all reorder rules for a store.
    /// </summary>
    Task<IEnumerable<ReorderRule>> GetReorderRulesAsync(int storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a reorder rule.
    /// </summary>
    Task<ReorderRule> SaveReorderRuleAsync(ReorderRule rule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a reorder rule.
    /// </summary>
    Task DeleteReorderRuleAsync(int ruleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates economic order quantity.
    /// </summary>
    Task<decimal> CalculateEOQAsync(int storeId, int productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates reorder suggestions based on current stock levels.
    /// </summary>
    Task<IEnumerable<ReorderSuggestion>> GenerateReorderSuggestionsAsync(int storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending reorder suggestions.
    /// </summary>
    Task<IEnumerable<ReorderSuggestion>> GetPendingReorderSuggestionsAsync(int storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets reorder suggestions filtered by status.
    /// </summary>
    Task<IEnumerable<ReorderSuggestion>> GetReorderSuggestionsAsync(int storeId, string? status = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a reorder suggestion.
    /// </summary>
    Task<ReorderSuggestion> UpdateReorderSuggestionAsync(ReorderSuggestion suggestion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves a reorder suggestion.
    /// </summary>
    Task<ReorderSuggestion> ApproveReorderSuggestionAsync(int suggestionId, int userId, decimal? adjustedQuantity = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects a reorder suggestion.
    /// </summary>
    Task RejectReorderSuggestionAsync(int suggestionId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts approved suggestions to purchase orders.
    /// </summary>
    Task<ConvertSuggestionsResult> ConvertSuggestionsToPurchaseOrdersAsync(int storeId, IEnumerable<int>? suggestionIds = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates reorder points based on sales history.
    /// </summary>
    Task<ReorderPointCalculationResult> CalculateReorderPointsAsync(int storeId, int lookbackDays = 90, CancellationToken cancellationToken = default);

    #endregion

    #region Shrinkage Analysis

    /// <summary>
    /// Records a shrinkage incident.
    /// </summary>
    Task<ShrinkageRecord> RecordShrinkageAsync(ShrinkageRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets shrinkage records.
    /// </summary>
    Task<IEnumerable<ShrinkageRecord>> GetShrinkageRecordsAsync(int storeId, DateTime? fromDate = null, DateTime? toDate = null, ShrinkageType? type = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets shrinkage summary for a period.
    /// </summary>
    Task<ShrinkageSummary> GetShrinkageSummaryAsync(int storeId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes shrinkage patterns.
    /// </summary>
    Task<ShrinkageAnalysisResult> AnalyzeShrinkagePatternsAsync(int storeId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets top shrinkage products.
    /// </summary>
    Task<IEnumerable<ProductShrinkageSummary>> GetTopShrinkageProductsAsync(int storeId, DateTime startDate, DateTime endDate, int topN = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves shrinkage analysis period.
    /// </summary>
    Task<ShrinkageAnalysisPeriod> SaveShrinkageAnalysisPeriodAsync(ShrinkageAnalysisPeriod analysis, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets shrinkage trend analysis.
    /// </summary>
    Task<ShrinkageTrendResult> GetShrinkageTrendAsync(int storeId, DateTime startDate, DateTime endDate, string groupBy = "month", CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports shrinkage from stock take variance.
    /// </summary>
    Task<int> ImportShrinkageFromStockTakeAsync(int stockTakeId, ShrinkageType defaultType, int userId, CancellationToken cancellationToken = default);

    #endregion

    #region Dead Stock Identification

    /// <summary>
    /// Gets dead stock configuration.
    /// </summary>
    Task<DeadStockConfig?> GetDeadStockConfigAsync(int storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves dead stock configuration.
    /// </summary>
    Task<DeadStockConfig> SaveDeadStockConfigAsync(DeadStockConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Identifies dead stock items.
    /// </summary>
    Task<IEnumerable<DeadStockItem>> IdentifyDeadStockAsync(int storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets identified dead stock items.
    /// </summary>
    Task<IEnumerable<DeadStockItem>> GetDeadStockItemsAsync(int storeId, DeadStockClassification? classification = null, string? status = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates dead stock item status.
    /// </summary>
    Task<DeadStockItem> UpdateDeadStockItemAsync(int itemId, string status, string? actionTaken = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets dead stock summary.
    /// </summary>
    Task<DeadStockSummary> GetDeadStockSummaryAsync(int storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates suggested clearance prices.
    /// </summary>
    Task<IEnumerable<ClearancePriceSuggestion>> CalculateClearancePricesAsync(int storeId, decimal minMarginPercent = 0, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates clearance promotion for dead stock.
    /// </summary>
    Task<int> CreateClearancePromotionAsync(int storeId, IEnumerable<int> deadStockItemIds, decimal discountPercent, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates inventory turnover.
    /// </summary>
    Task<InventoryTurnoverAnalysis> CalculateInventoryTurnoverAsync(int storeId, DateTime startDate, DateTime endDate, int? productId = null, int? categoryId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets inventory turnover by category.
    /// </summary>
    Task<IEnumerable<CategoryTurnoverSummary>> GetInventoryTurnoverByCategoryAsync(int storeId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets ABC analysis of inventory.
    /// </summary>
    Task<ABCAnalysisResult> GetABCAnalysisAsync(int storeId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    #endregion
}

#region DTOs

/// <summary>
/// Stock valuation result.
/// </summary>
public class StockValuationResult
{
    public int StoreId { get; set; }
    public DateTime ValuationDate { get; set; }
    public StockValuationMethod Method { get; set; }
    public decimal TotalValue { get; set; }
    public decimal TotalQuantity { get; set; }
    public int SkuCount { get; set; }
    public List<ProductValuation> Products { get; set; } = new();
}

/// <summary>
/// Product valuation detail.
/// </summary>
public class ProductValuation
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public decimal QuantityOnHand { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalValue { get; set; }
}

/// <summary>
/// Product cost calculation result.
/// </summary>
public class ProductCostResult
{
    public int ProductId { get; set; }
    public StockValuationMethod Method { get; set; }
    public decimal UnitCost { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal TotalValue { get; set; }
    public List<CostLayer> CostLayers { get; set; } = new();
}

/// <summary>
/// Cost layer for FIFO/LIFO.
/// </summary>
public class CostLayer
{
    public DateTime ReceivedDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public string? ReferenceNumber { get; set; }
}

/// <summary>
/// Standard cost update result.
/// </summary>
public class StandardCostUpdateResult
{
    public int ProductsUpdated { get; set; }
    public decimal TotalValueChange { get; set; }
    public List<StandardCostChange> Changes { get; set; } = new();
}

/// <summary>
/// Standard cost change detail.
/// </summary>
public class StandardCostChange
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal OldCost { get; set; }
    public decimal NewCost { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal ValueChange { get; set; }
}

/// <summary>
/// Valuation comparison across methods.
/// </summary>
public class ValuationComparisonResult
{
    public int StoreId { get; set; }
    public DateTime ComparisonDate { get; set; }
    public decimal FIFOValue { get; set; }
    public decimal LIFOValue { get; set; }
    public decimal WeightedAverageValue { get; set; }
    public decimal FIFOvsLIFODifference { get; set; }
    public decimal FIFOvsWADifference { get; set; }
    public List<ProductValuationComparison> ProductComparisons { get; set; } = new();
}

/// <summary>
/// Product valuation comparison.
/// </summary>
public class ProductValuationComparison
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal QuantityOnHand { get; set; }
    public decimal FIFOValue { get; set; }
    public decimal LIFOValue { get; set; }
    public decimal WeightedAverageValue { get; set; }
}

/// <summary>
/// Result of converting suggestions to purchase orders.
/// </summary>
public class ConvertSuggestionsResult
{
    public int SuggestionsProcessed { get; set; }
    public int PurchaseOrdersCreated { get; set; }
    public List<int> PurchaseOrderIds { get; set; } = new();
    public decimal TotalOrderValue { get; set; }
}

/// <summary>
/// Reorder point calculation result.
/// </summary>
public class ReorderPointCalculationResult
{
    public int ProductsAnalyzed { get; set; }
    public int RulesUpdated { get; set; }
    public List<ReorderPointDetail> Details { get; set; } = new();
}

/// <summary>
/// Reorder point detail.
/// </summary>
public class ReorderPointDetail
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal AverageDailySales { get; set; }
    public int LeadTimeDays { get; set; }
    public decimal CalculatedReorderPoint { get; set; }
    public decimal RecommendedSafetyStock { get; set; }
    public decimal RecommendedEOQ { get; set; }
}

/// <summary>
/// Shrinkage summary.
/// </summary>
public class ShrinkageSummary
{
    public int StoreId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalShrinkageValue { get; set; }
    public decimal TotalShrinkageQuantity { get; set; }
    public int IncidentCount { get; set; }
    public int ProductsAffected { get; set; }
    public decimal ShrinkageRate { get; set; }
    public Dictionary<ShrinkageType, decimal> ByType { get; set; } = new();
}

/// <summary>
/// Shrinkage analysis result.
/// </summary>
public class ShrinkageAnalysisResult
{
    public ShrinkageSummary Summary { get; set; } = new();
    public List<ShrinkageByDepartment> ByDepartment { get; set; } = new();
    public List<ShrinkageByCategory> ByCategory { get; set; } = new();
    public List<string> Insights { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// Shrinkage by department.
/// </summary>
public class ShrinkageByDepartment
{
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public decimal TotalValue { get; set; }
    public int IncidentCount { get; set; }
}

/// <summary>
/// Shrinkage by category.
/// </summary>
public class ShrinkageByCategory
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal TotalValue { get; set; }
    public int IncidentCount { get; set; }
}

/// <summary>
/// Product shrinkage summary.
/// </summary>
public class ProductShrinkageSummary
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public decimal TotalQuantityLost { get; set; }
    public decimal TotalValueLost { get; set; }
    public int IncidentCount { get; set; }
    public ShrinkageType PrimaryType { get; set; }
}

/// <summary>
/// Shrinkage trend result.
/// </summary>
public class ShrinkageTrendResult
{
    public List<ShrinkageTrendPoint> TrendPoints { get; set; } = new();
    public decimal OverallTrend { get; set; } // Positive = increasing, Negative = decreasing
    public string TrendDescription { get; set; } = string.Empty;
}

/// <summary>
/// Shrinkage trend point.
/// </summary>
public class ShrinkageTrendPoint
{
    public string Period { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public decimal TotalValue { get; set; }
    public decimal ShrinkageRate { get; set; }
    public int IncidentCount { get; set; }
}

/// <summary>
/// Dead stock summary.
/// </summary>
public class DeadStockSummary
{
    public int StoreId { get; set; }
    public int TotalDeadStockItems { get; set; }
    public decimal TotalDeadStockValue { get; set; }
    public decimal TotalPotentialLoss { get; set; }
    public Dictionary<DeadStockClassification, int> ByClassification { get; set; } = new();
    public Dictionary<DeadStockClassification, decimal> ValueByClassification { get; set; } = new();
    public decimal PercentOfTotalInventory { get; set; }
}

/// <summary>
/// Clearance price suggestion.
/// </summary>
public class ClearancePriceSuggestion
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal Cost { get; set; }
    public decimal SuggestedClearancePrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal PotentialRecovery { get; set; }
    public int DaysSinceLastSale { get; set; }
}

/// <summary>
/// Category turnover summary.
/// </summary>
public class CategoryTurnoverSummary
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal COGS { get; set; }
    public decimal AverageInventory { get; set; }
    public decimal TurnoverRatio { get; set; }
    public decimal DaysSalesOfInventory { get; set; }
    public string PerformanceRating { get; set; } = string.Empty;
}

/// <summary>
/// ABC analysis result.
/// </summary>
public class ABCAnalysisResult
{
    public int StoreId { get; set; }
    public DateTime AnalysisDate { get; set; }
    public List<ABCClassificationItem> AItems { get; set; } = new();
    public List<ABCClassificationItem> BItems { get; set; } = new();
    public List<ABCClassificationItem> CItems { get; set; } = new();
    public ABCClassSummary ASummary { get; set; } = new();
    public ABCClassSummary BSummary { get; set; } = new();
    public ABCClassSummary CSummary { get; set; } = new();
}

/// <summary>
/// ABC classification item.
/// </summary>
public class ABCClassificationItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public decimal SalesValue { get; set; }
    public decimal CumulativePercentage { get; set; }
    public string Classification { get; set; } = string.Empty;
}

/// <summary>
/// ABC class summary.
/// </summary>
public class ABCClassSummary
{
    public string Classification { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public decimal PercentOfItems { get; set; }
    public decimal SalesValue { get; set; }
    public decimal PercentOfSales { get; set; }
}

#endregion
