namespace HospitalityPOS.Core.Models.Reports;

/// <summary>
/// Inventory report types.
/// </summary>
public enum InventoryReportType
{
    /// <summary>
    /// Current stock levels for all products.
    /// </summary>
    CurrentStock = 1,

    /// <summary>
    /// Products below minimum stock level.
    /// </summary>
    LowStock = 2,

    /// <summary>
    /// Stock movement history.
    /// </summary>
    StockMovement = 3,

    /// <summary>
    /// Stock value calculation.
    /// </summary>
    StockValuation = 4,

    /// <summary>
    /// Products with no movement (dead stock).
    /// </summary>
    DeadStock = 5
}

/// <summary>
/// Parameters for inventory report generation.
/// </summary>
public class InventoryReportParameters : ReportParametersBase
{
    /// <summary>
    /// Gets or sets the category ID filter (optional).
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the days threshold for dead stock report.
    /// </summary>
    public int DeadStockDaysThreshold { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether to include out of stock items.
    /// </summary>
    public bool IncludeOutOfStock { get; set; } = true;
}

/// <summary>
/// Current stock report item.
/// </summary>
public class CurrentStockItem
{
    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product code.
    /// </summary>
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the category name.
    /// </summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current stock level.
    /// </summary>
    public decimal CurrentStock { get; set; }

    /// <summary>
    /// Gets or sets the stock unit.
    /// </summary>
    public string StockUnit { get; set; } = "pcs";

    /// <summary>
    /// Gets or sets the cost price.
    /// </summary>
    public decimal CostPrice { get; set; }

    /// <summary>
    /// Gets or sets the selling price.
    /// </summary>
    public decimal SellingPrice { get; set; }

    /// <summary>
    /// Gets or sets the stock value (CurrentStock * CostPrice).
    /// </summary>
    public decimal StockValue { get; set; }

    /// <summary>
    /// Gets or sets the retail value (CurrentStock * SellingPrice).
    /// </summary>
    public decimal RetailValue { get; set; }

    /// <summary>
    /// Gets or sets the minimum stock level.
    /// </summary>
    public decimal MinStock { get; set; }

    /// <summary>
    /// Gets or sets the maximum stock level.
    /// </summary>
    public decimal MaxStock { get; set; }

    /// <summary>
    /// Gets or sets the stock status (OK, LOW, OUT).
    /// </summary>
    public string Status { get; set; } = "OK";

    /// <summary>
    /// Gets whether the item is low on stock.
    /// </summary>
    public bool IsLowStock => Status == "LOW" || Status == "OUT";
}

/// <summary>
/// Current stock report result.
/// </summary>
public class CurrentStockReportResult
{
    /// <summary>
    /// Gets or sets the report parameters.
    /// </summary>
    public InventoryReportParameters Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the stock items.
    /// </summary>
    public List<CurrentStockItem> Items { get; set; } = [];

    /// <summary>
    /// Gets or sets the total SKU count.
    /// </summary>
    public int TotalSkuCount { get; set; }

    /// <summary>
    /// Gets or sets the items in stock count.
    /// </summary>
    public int ItemsInStock { get; set; }

    /// <summary>
    /// Gets or sets the out of stock count.
    /// </summary>
    public int OutOfStockCount { get; set; }

    /// <summary>
    /// Gets or sets the low stock count.
    /// </summary>
    public int LowStockCount { get; set; }

    /// <summary>
    /// Gets or sets the total stock value at cost.
    /// </summary>
    public decimal TotalStockValue { get; set; }

    /// <summary>
    /// Gets or sets the total retail value.
    /// </summary>
    public decimal TotalRetailValue { get; set; }

    /// <summary>
    /// Gets or sets the potential profit margin.
    /// </summary>
    public decimal PotentialProfit => TotalRetailValue - TotalStockValue;

    /// <summary>
    /// Gets or sets when the report was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the name of the user who generated the report.
    /// </summary>
    public string GeneratedBy { get; set; } = string.Empty;
}

/// <summary>
/// Low stock report item.
/// </summary>
public class LowStockItem
{
    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product code.
    /// </summary>
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the category name.
    /// </summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current stock level.
    /// </summary>
    public decimal CurrentStock { get; set; }

    /// <summary>
    /// Gets or sets the minimum stock level.
    /// </summary>
    public decimal MinStock { get; set; }

    /// <summary>
    /// Gets or sets the maximum stock level.
    /// </summary>
    public decimal MaxStock { get; set; }

    /// <summary>
    /// Gets or sets the reorder quantity needed.
    /// </summary>
    public decimal ReorderQty { get; set; }

    /// <summary>
    /// Gets or sets the cost price.
    /// </summary>
    public decimal CostPrice { get; set; }

    /// <summary>
    /// Gets or sets the reorder value (ReorderQty * CostPrice).
    /// </summary>
    public decimal ReorderValue { get; set; }

    /// <summary>
    /// Gets or sets the status (CRITICAL or LOW).
    /// </summary>
    public string Status { get; set; } = "LOW";

    /// <summary>
    /// Gets whether the item is critical (out of stock).
    /// </summary>
    public bool IsCritical => Status == "CRITICAL";
}

/// <summary>
/// Low stock report result.
/// </summary>
public class LowStockReportResult
{
    /// <summary>
    /// Gets or sets the report parameters.
    /// </summary>
    public InventoryReportParameters Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the low stock items.
    /// </summary>
    public List<LowStockItem> Items { get; set; } = [];

    /// <summary>
    /// Gets or sets the critical (out of stock) items count.
    /// </summary>
    public int CriticalCount { get; set; }

    /// <summary>
    /// Gets or sets the low stock items count.
    /// </summary>
    public int LowStockCount { get; set; }

    /// <summary>
    /// Gets or sets the total reorder value.
    /// </summary>
    public decimal TotalReorderValue { get; set; }

    /// <summary>
    /// Gets or sets when the report was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the name of the user who generated the report.
    /// </summary>
    public string GeneratedBy { get; set; } = string.Empty;
}

/// <summary>
/// Stock movement report item.
/// </summary>
public class StockMovementItem
{
    /// <summary>
    /// Gets or sets the movement ID.
    /// </summary>
    public int MovementId { get; set; }

    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the movement date.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Gets the time display.
    /// </summary>
    public string TimeDisplay => Date.ToLocalTime().ToString("HH:mm");

    /// <summary>
    /// Gets or sets the movement type.
    /// </summary>
    public string MovementType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quantity moved (positive for in, negative for out).
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Gets or sets the stock before movement.
    /// </summary>
    public decimal PreviousStock { get; set; }

    /// <summary>
    /// Gets or sets the stock after movement.
    /// </summary>
    public decimal NewStock { get; set; }

    /// <summary>
    /// Gets or sets the reference (receipt number, GRN, etc.).
    /// </summary>
    public string Reference { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user who made the movement.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets notes/reason for the movement.
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Stock movement report result with pagination support.
/// </summary>
public class StockMovementReportResult
{
    /// <summary>
    /// Gets or sets the report parameters.
    /// </summary>
    public InventoryReportParameters Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the movement items for the current page.
    /// </summary>
    public List<StockMovementItem> Items { get; set; } = [];

    /// <summary>
    /// Gets or sets the total items received (across all pages).
    /// </summary>
    public decimal TotalReceived { get; set; }

    /// <summary>
    /// Gets or sets the total items sold (across all pages).
    /// </summary>
    public decimal TotalSold { get; set; }

    /// <summary>
    /// Gets or sets the total adjustments (across all pages).
    /// </summary>
    public decimal TotalAdjusted { get; set; }

    /// <summary>
    /// Gets or sets the net movement (across all pages).
    /// </summary>
    public decimal NetMovement { get; set; }

    /// <summary>
    /// Gets or sets when the report was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the name of the user who generated the report.
    /// </summary>
    public string GeneratedBy { get; set; } = string.Empty;

    // Pagination properties

    /// <summary>
    /// Gets or sets the total count of movement records (across all pages).
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the current page number (1-based).
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; } = 100;

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;

    /// <summary>
    /// Gets whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Gets whether there is a next page.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;
}

/// <summary>
/// Category valuation item.
/// </summary>
public class CategoryValuation
{
    /// <summary>
    /// Gets or sets the category ID.
    /// </summary>
    public int CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the category name.
    /// </summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the item count.
    /// </summary>
    public int ItemCount { get; set; }

    /// <summary>
    /// Gets or sets the total units in stock.
    /// </summary>
    public decimal TotalUnits { get; set; }

    /// <summary>
    /// Gets or sets the cost value.
    /// </summary>
    public decimal CostValue { get; set; }

    /// <summary>
    /// Gets or sets the retail value.
    /// </summary>
    public decimal RetailValue { get; set; }

    /// <summary>
    /// Gets the potential profit.
    /// </summary>
    public decimal PotentialProfit => RetailValue - CostValue;

    /// <summary>
    /// Gets the margin percentage.
    /// </summary>
    public decimal MarginPercentage => RetailValue > 0 ? Math.Round((RetailValue - CostValue) / RetailValue * 100, 2) : 0;
}

/// <summary>
/// Stock valuation report result.
/// </summary>
public class StockValuationReportResult
{
    /// <summary>
    /// Gets or sets the report parameters.
    /// </summary>
    public InventoryReportParameters Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the as of date.
    /// </summary>
    public DateTime AsOfDate { get; set; }

    /// <summary>
    /// Gets or sets the category valuations.
    /// </summary>
    public List<CategoryValuation> Categories { get; set; } = [];

    /// <summary>
    /// Gets or sets the total cost value.
    /// </summary>
    public decimal TotalCostValue { get; set; }

    /// <summary>
    /// Gets or sets the total retail value.
    /// </summary>
    public decimal TotalRetailValue { get; set; }

    /// <summary>
    /// Gets or sets the potential profit.
    /// </summary>
    public decimal PotentialProfit { get; set; }

    /// <summary>
    /// Gets the overall margin percentage.
    /// </summary>
    public decimal MarginPercentage => TotalRetailValue > 0 ? Math.Round((TotalRetailValue - TotalCostValue) / TotalRetailValue * 100, 2) : 0;

    /// <summary>
    /// Gets or sets when the report was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the name of the user who generated the report.
    /// </summary>
    public string GeneratedBy { get; set; } = string.Empty;
}

/// <summary>
/// Dead stock report item.
/// </summary>
public class DeadStockItem
{
    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product code.
    /// </summary>
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the category name.
    /// </summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current stock level.
    /// </summary>
    public decimal CurrentStock { get; set; }

    /// <summary>
    /// Gets or sets the stock value.
    /// </summary>
    public decimal StockValue { get; set; }

    /// <summary>
    /// Gets or sets the last movement date.
    /// </summary>
    public DateTime? LastMovementDate { get; set; }

    /// <summary>
    /// Gets or sets the days since last movement.
    /// </summary>
    public int DaysSinceMovement { get; set; }
}

/// <summary>
/// Dead stock report result.
/// </summary>
public class DeadStockReportResult
{
    /// <summary>
    /// Gets or sets the report parameters.
    /// </summary>
    public InventoryReportParameters Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the dead stock items.
    /// </summary>
    public List<DeadStockItem> Items { get; set; } = [];

    /// <summary>
    /// Gets or sets the total dead stock count.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the total dead stock value.
    /// </summary>
    public decimal TotalValue { get; set; }

    /// <summary>
    /// Gets or sets the days threshold used.
    /// </summary>
    public int DaysThreshold { get; set; }

    /// <summary>
    /// Gets or sets when the report was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the name of the user who generated the report.
    /// </summary>
    public string GeneratedBy { get; set; } = string.Empty;
}
