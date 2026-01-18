namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Stock valuation method.
/// </summary>
public enum StockValuationMethod
{
    /// <summary>First In First Out.</summary>
    FIFO = 1,
    /// <summary>Last In First Out.</summary>
    LIFO = 2,
    /// <summary>Weighted Average Cost.</summary>
    WeightedAverage = 3,
    /// <summary>Specific Identification.</summary>
    SpecificIdentification = 4,
    /// <summary>Standard Cost.</summary>
    StandardCost = 5
}

/// <summary>
/// Stock valuation configuration per store.
/// </summary>
public class StockValuationConfig : BaseEntity
{
    /// <summary>
    /// Store this applies to.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Default valuation method.
    /// </summary>
    public StockValuationMethod DefaultMethod { get; set; } = StockValuationMethod.WeightedAverage;

    /// <summary>
    /// Whether to calculate valuation automatically on stock movements.
    /// </summary>
    public bool AutoCalculateOnMovement { get; set; } = true;

    /// <summary>
    /// Whether to include tax in cost.
    /// </summary>
    public bool IncludeTaxInCost { get; set; }

    /// <summary>
    /// Whether to include freight in cost.
    /// </summary>
    public bool IncludeFreightInCost { get; set; } = true;

    /// <summary>
    /// Standard cost update frequency (days).
    /// </summary>
    public int StandardCostUpdateFrequencyDays { get; set; } = 30;

    /// <summary>
    /// Last standard cost calculation date.
    /// </summary>
    public DateTime? LastStandardCostCalculation { get; set; }

    /// <summary>
    /// Whether this configuration is soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    // Navigation properties
    public virtual Store Store { get; set; } = null!;
}

/// <summary>
/// Stock valuation snapshot for reporting.
/// </summary>
public class StockValuationSnapshot : BaseEntity
{
    /// <summary>
    /// Store ID.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Snapshot date.
    /// </summary>
    public DateTime SnapshotDate { get; set; }

    /// <summary>
    /// Valuation method used.
    /// </summary>
    public StockValuationMethod Method { get; set; }

    /// <summary>
    /// Total stock value.
    /// </summary>
    public decimal TotalValue { get; set; }

    /// <summary>
    /// Total quantity on hand.
    /// </summary>
    public decimal TotalQuantity { get; set; }

    /// <summary>
    /// Number of SKUs included.
    /// </summary>
    public int SkuCount { get; set; }

    /// <summary>
    /// Whether this is a period-end snapshot.
    /// </summary>
    public bool IsPeriodEnd { get; set; }

    /// <summary>
    /// Period (e.g., "2024-01" for January 2024).
    /// </summary>
    public string? Period { get; set; }

    /// <summary>
    /// Notes.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Whether this snapshot is soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    // Navigation properties
    public virtual Store Store { get; set; } = null!;
    public virtual ICollection<StockValuationDetail> Details { get; set; } = new List<StockValuationDetail>();
}

/// <summary>
/// Stock valuation detail per product.
/// </summary>
public class StockValuationDetail : BaseEntity
{
    /// <summary>
    /// Reference to snapshot.
    /// </summary>
    public int SnapshotId { get; set; }

    /// <summary>
    /// Product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Quantity on hand.
    /// </summary>
    public decimal QuantityOnHand { get; set; }

    /// <summary>
    /// Unit cost.
    /// </summary>
    public decimal UnitCost { get; set; }

    /// <summary>
    /// Total value.
    /// </summary>
    public decimal TotalValue { get; set; }

    /// <summary>
    /// Weighted average cost (for reference).
    /// </summary>
    public decimal? WeightedAverageCost { get; set; }

    /// <summary>
    /// FIFO cost (for reference).
    /// </summary>
    public decimal? FifoCost { get; set; }

    /// <summary>
    /// Standard cost (for reference).
    /// </summary>
    public decimal? StandardCost { get; set; }

    // Navigation properties
    public virtual StockValuationSnapshot Snapshot { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}

/// <summary>
/// Reorder rule for automatic purchase order generation.
/// </summary>
public class ReorderRule : BaseEntity
{
    /// <summary>
    /// Store this rule applies to.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Minimum stock level to trigger reorder.
    /// </summary>
    public decimal ReorderPoint { get; set; }

    /// <summary>
    /// Quantity to reorder.
    /// </summary>
    public decimal ReorderQuantity { get; set; }

    /// <summary>
    /// Maximum stock level.
    /// </summary>
    public decimal? MaxStockLevel { get; set; }

    /// <summary>
    /// Safety stock buffer.
    /// </summary>
    public decimal SafetyStock { get; set; }

    /// <summary>
    /// Lead time in days.
    /// </summary>
    public int LeadTimeDays { get; set; }

    /// <summary>
    /// Preferred supplier ID.
    /// </summary>
    public int? PreferredSupplierId { get; set; }

    /// <summary>
    /// Whether automatic reorder is enabled.
    /// </summary>
    public bool IsAutoReorderEnabled { get; set; } = true;

    /// <summary>
    /// Whether to consolidate reorders.
    /// </summary>
    public bool ConsolidateReorders { get; set; } = true;

    /// <summary>
    /// Minimum order quantity (supplier requirement).
    /// </summary>
    public decimal? MinOrderQuantity { get; set; }

    /// <summary>
    /// Order multiple (must order in multiples of this quantity).
    /// </summary>
    public decimal? OrderMultiple { get; set; }

    /// <summary>
    /// Economic order quantity (calculated).
    /// </summary>
    public decimal? EconomicOrderQuantity { get; set; }

    /// <summary>
    /// Last calculated date.
    /// </summary>
    public DateTime? LastCalculatedDate { get; set; }

    /// <summary>
    /// Average daily sales (for EOQ calculation).
    /// </summary>
    public decimal? AverageDailySales { get; set; }

    /// <summary>
    /// Whether this rule is soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    // Navigation properties
    public virtual Store Store { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
    public virtual Supplier? PreferredSupplier { get; set; }
}

/// <summary>
/// Generated reorder suggestion.
/// </summary>
public class ReorderSuggestion : BaseEntity
{
    /// <summary>
    /// Store ID.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Supplier ID.
    /// </summary>
    public int? SupplierId { get; set; }

    /// <summary>
    /// Current stock level.
    /// </summary>
    public decimal CurrentStock { get; set; }

    /// <summary>
    /// Reorder point that triggered suggestion.
    /// </summary>
    public decimal ReorderPoint { get; set; }

    /// <summary>
    /// Suggested quantity to order.
    /// </summary>
    public decimal SuggestedQuantity { get; set; }

    /// <summary>
    /// Estimated cost.
    /// </summary>
    public decimal EstimatedCost { get; set; }

    /// <summary>
    /// Status: Pending, Approved, Rejected, Converted.
    /// </summary>
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Priority: Low, Medium, High, Critical.
    /// </summary>
    public string Priority { get; set; } = "Medium";

    /// <summary>
    /// Days until stockout (projected).
    /// </summary>
    public int? DaysUntilStockout { get; set; }

    /// <summary>
    /// Purchase order ID if converted.
    /// </summary>
    public int? PurchaseOrderId { get; set; }

    /// <summary>
    /// Approved by user ID.
    /// </summary>
    public int? ApprovedByUserId { get; set; }

    /// <summary>
    /// Approved date.
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// Notes.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Whether this suggestion has been deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    // Navigation properties
    public virtual Store Store { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
    public virtual Supplier? Supplier { get; set; }
    public virtual PurchaseOrder? PurchaseOrder { get; set; }
    public virtual User? ApprovedByUser { get; set; }
}

/// <summary>
/// Shrinkage category.
/// </summary>
public enum ShrinkageType
{
    /// <summary>Unknown/unidentified shrinkage.</summary>
    Unknown = 1,
    /// <summary>Theft (internal or external).</summary>
    Theft = 2,
    /// <summary>Administrative error.</summary>
    AdministrativeError = 3,
    /// <summary>Damaged goods.</summary>
    Damage = 4,
    /// <summary>Expired products.</summary>
    Expiry = 5,
    /// <summary>Vendor fraud.</summary>
    VendorFraud = 6,
    /// <summary>Spoilage.</summary>
    Spoilage = 7,
    /// <summary>Breakage.</summary>
    Breakage = 8
}

/// <summary>
/// Shrinkage record.
/// </summary>
public class ShrinkageRecord : BaseEntity
{
    /// <summary>
    /// Store ID.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Date of shrinkage.
    /// </summary>
    public DateTime ShrinkageDate { get; set; }

    /// <summary>
    /// Quantity lost.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit cost at time of loss.
    /// </summary>
    public decimal UnitCost { get; set; }

    /// <summary>
    /// Total value lost.
    /// </summary>
    public decimal TotalValue { get; set; }

    /// <summary>
    /// Type of shrinkage.
    /// </summary>
    public ShrinkageType Type { get; set; } = ShrinkageType.Unknown;

    /// <summary>
    /// Cause description.
    /// </summary>
    public string? Cause { get; set; }

    /// <summary>
    /// Source reference (Stock Take ID, etc.).
    /// </summary>
    public string? SourceReference { get; set; }

    /// <summary>
    /// Source type (StockTake, Manual, System).
    /// </summary>
    public string SourceType { get; set; } = "Manual";

    /// <summary>
    /// Whether shrinkage has been investigated.
    /// </summary>
    public bool IsInvestigated { get; set; }

    /// <summary>
    /// Investigation notes.
    /// </summary>
    public string? InvestigationNotes { get; set; }

    /// <summary>
    /// Recovered quantity.
    /// </summary>
    public decimal? RecoveredQuantity { get; set; }

    /// <summary>
    /// Recovered value.
    /// </summary>
    public decimal? RecoveredValue { get; set; }

    /// <summary>
    /// Recorded by user ID.
    /// </summary>
    public int RecordedByUserId { get; set; }

    /// <summary>
    /// Department ID (if applicable).
    /// </summary>
    public int? DepartmentId { get; set; }

    /// <summary>
    /// Whether this record is soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    // Navigation properties
    public virtual Store Store { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
    public virtual User RecordedByUser { get; set; } = null!;
    public virtual Department? Department { get; set; }
}

/// <summary>
/// Shrinkage analysis period summary.
/// </summary>
public class ShrinkageAnalysisPeriod : BaseEntity
{
    /// <summary>
    /// Store ID.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Period start date.
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// Period end date.
    /// </summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>
    /// Total shrinkage value.
    /// </summary>
    public decimal TotalShrinkageValue { get; set; }

    /// <summary>
    /// Total sales during period.
    /// </summary>
    public decimal TotalSales { get; set; }

    /// <summary>
    /// Shrinkage rate as percentage of sales.
    /// </summary>
    public decimal ShrinkageRate { get; set; }

    /// <summary>
    /// Number of shrinkage incidents.
    /// </summary>
    public int IncidentCount { get; set; }

    /// <summary>
    /// Number of products affected.
    /// </summary>
    public int ProductsAffected { get; set; }

    /// <summary>
    /// Comparison to prior period (percent change).
    /// </summary>
    public decimal? PriorPeriodChange { get; set; }

    /// <summary>
    /// Industry benchmark comparison.
    /// </summary>
    public decimal? IndustryBenchmark { get; set; }

    /// <summary>
    /// Analysis notes.
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public virtual Store Store { get; set; } = null!;
}

/// <summary>
/// Dead stock classification.
/// </summary>
public enum DeadStockClassification
{
    /// <summary>No sales in 90+ days.</summary>
    SlowMoving = 1,
    /// <summary>No sales in 180+ days.</summary>
    NonMoving = 2,
    /// <summary>No sales in 365+ days.</summary>
    DeadStock = 3,
    /// <summary>Obsolete/discontinued.</summary>
    Obsolete = 4
}

/// <summary>
/// Dead stock item identification.
/// </summary>
public class DeadStockItem : BaseEntity
{
    /// <summary>
    /// Store ID.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Classification.
    /// </summary>
    public DeadStockClassification Classification { get; set; }

    /// <summary>
    /// Days since last sale.
    /// </summary>
    public int DaysSinceLastSale { get; set; }

    /// <summary>
    /// Last sale date.
    /// </summary>
    public DateTime? LastSaleDate { get; set; }

    /// <summary>
    /// Current quantity on hand.
    /// </summary>
    public decimal QuantityOnHand { get; set; }

    /// <summary>
    /// Current stock value.
    /// </summary>
    public decimal StockValue { get; set; }

    /// <summary>
    /// Age of oldest stock (days).
    /// </summary>
    public int? OldestStockAgeDays { get; set; }

    /// <summary>
    /// Recommended action (Clearance, Transfer, Donate, Write-off).
    /// </summary>
    public string RecommendedAction { get; set; } = "Review";

    /// <summary>
    /// Suggested clearance price.
    /// </summary>
    public decimal? SuggestedClearancePrice { get; set; }

    /// <summary>
    /// Potential loss if written off.
    /// </summary>
    public decimal PotentialLoss { get; set; }

    /// <summary>
    /// Status: Identified, UnderReview, ActionTaken, Resolved.
    /// </summary>
    public string Status { get; set; } = "Identified";

    /// <summary>
    /// Action taken description.
    /// </summary>
    public string? ActionTaken { get; set; }

    /// <summary>
    /// Action taken date.
    /// </summary>
    public DateTime? ActionTakenDate { get; set; }

    /// <summary>
    /// Identified date.
    /// </summary>
    public DateTime IdentifiedDate { get; set; }

    /// <summary>
    /// Notes.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Whether this item is soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    // Navigation properties
    public virtual Store Store { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}

/// <summary>
/// Dead stock analysis configuration.
/// </summary>
public class DeadStockConfig : BaseEntity
{
    /// <summary>
    /// Store ID.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Days without sales to classify as slow-moving.
    /// </summary>
    public int SlowMovingDays { get; set; } = 90;

    /// <summary>
    /// Days without sales to classify as non-moving.
    /// </summary>
    public int NonMovingDays { get; set; } = 180;

    /// <summary>
    /// Days without sales to classify as dead stock.
    /// </summary>
    public int DeadStockDays { get; set; } = 365;

    /// <summary>
    /// Minimum stock value to include in analysis.
    /// </summary>
    public decimal MinStockValue { get; set; } = 0;

    /// <summary>
    /// Categories to exclude from analysis (JSON array).
    /// </summary>
    public string? ExcludedCategoryIds { get; set; }

    /// <summary>
    /// Products to exclude from analysis (JSON array).
    /// </summary>
    public string? ExcludedProductIds { get; set; }

    /// <summary>
    /// Automatic analysis frequency (days).
    /// </summary>
    public int AnalysisFrequencyDays { get; set; } = 7;

    /// <summary>
    /// Last analysis date.
    /// </summary>
    public DateTime? LastAnalysisDate { get; set; }

    /// <summary>
    /// Default clearance discount percentage.
    /// </summary>
    public decimal DefaultClearanceDiscountPercent { get; set; } = 50;

    /// <summary>
    /// Send alerts for new dead stock.
    /// </summary>
    public bool SendAlerts { get; set; } = true;

    /// <summary>
    /// Whether this configuration is soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    // Navigation properties
    public virtual Store Store { get; set; } = null!;
}

/// <summary>
/// Inventory turnover analysis.
/// </summary>
public class InventoryTurnoverAnalysis : BaseEntity
{
    /// <summary>
    /// Store ID.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Product ID (null for store-wide).
    /// </summary>
    public int? ProductId { get; set; }

    /// <summary>
    /// Category ID (null for all categories).
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Analysis period start.
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// Analysis period end.
    /// </summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>
    /// Cost of goods sold during period.
    /// </summary>
    public decimal COGS { get; set; }

    /// <summary>
    /// Average inventory value.
    /// </summary>
    public decimal AverageInventoryValue { get; set; }

    /// <summary>
    /// Turnover ratio.
    /// </summary>
    public decimal TurnoverRatio { get; set; }

    /// <summary>
    /// Days sales of inventory.
    /// </summary>
    public decimal DaysSalesOfInventory { get; set; }

    /// <summary>
    /// Industry benchmark turnover.
    /// </summary>
    public decimal? BenchmarkTurnover { get; set; }

    /// <summary>
    /// Performance rating (Above/At/Below benchmark).
    /// </summary>
    public string? PerformanceRating { get; set; }

    /// <summary>
    /// Analysis notes.
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public virtual Store Store { get; set; } = null!;
    public virtual Product? Product { get; set; }
    public virtual Category? Category { get; set; }
}
