namespace HospitalityPOS.Core.Models.Reports;

/// <summary>
/// Base class for all report parameters with optional pagination support.
/// </summary>
public class ReportParametersBase
{
    private int _pageNumber = 1;
    private int _pageSize = 100;

    /// <summary>
    /// Gets or sets the start date for the report.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date for the report.
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Gets or sets the user ID who generated the report.
    /// </summary>
    public int GeneratedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the work period ID filter (optional).
    /// </summary>
    public int? WorkPeriodId { get; set; }

    /// <summary>
    /// Gets or sets the page number (1-based). Default is 1.
    /// </summary>
    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    /// <summary>
    /// Gets or sets the page size. Default is 100, max is 1000.
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 100 : (value > 1000 ? 1000 : value);
    }

    /// <summary>
    /// Gets the number of records to skip for pagination.
    /// </summary>
    public int Skip => (PageNumber - 1) * PageSize;

    /// <summary>
    /// Gets the number of records to take for pagination.
    /// </summary>
    public int Take => PageSize;

    /// <summary>
    /// Gets or sets whether pagination is enabled. Default is false for backwards compatibility.
    /// </summary>
    public bool EnablePagination { get; set; } = false;
}

/// <summary>
/// Parameters for sales report generation.
/// </summary>
public class SalesReportParameters : ReportParametersBase
{
    /// <summary>
    /// Gets or sets whether to include zero-sale products.
    /// </summary>
    public bool IncludeZeroSales { get; set; }

    /// <summary>
    /// Gets or sets the category ID filter (optional).
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the user/cashier ID filter (optional).
    /// </summary>
    public int? UserId { get; set; }
}

/// <summary>
/// Daily sales summary report data.
/// </summary>
public class DailySalesSummary
{
    /// <summary>
    /// Gets or sets the start date of the report period.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date of the report period.
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Gets or sets the gross sales (subtotal + discounts before deduction).
    /// </summary>
    public decimal GrossSales { get; set; }

    /// <summary>
    /// Gets or sets the total discounts applied.
    /// </summary>
    public decimal Discounts { get; set; }

    /// <summary>
    /// Gets or sets the net sales (after discounts).
    /// </summary>
    public decimal NetSales { get; set; }

    /// <summary>
    /// Gets or sets the tax collected.
    /// </summary>
    public decimal TaxCollected { get; set; }

    /// <summary>
    /// Gets or sets the total revenue (net sales + tax).
    /// </summary>
    public decimal TotalRevenue { get; set; }

    /// <summary>
    /// Gets or sets the number of transactions.
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Gets or sets the average transaction value.
    /// </summary>
    public decimal AverageTransaction { get; set; }

    /// <summary>
    /// Gets or sets the largest transaction amount.
    /// </summary>
    public decimal LargestTransaction { get; set; }

    /// <summary>
    /// Gets or sets the smallest transaction amount.
    /// </summary>
    public decimal SmallestTransaction { get; set; }

    /// <summary>
    /// Gets or sets the number of voided transactions.
    /// </summary>
    public int VoidedCount { get; set; }

    /// <summary>
    /// Gets or sets the total voided amount.
    /// </summary>
    public decimal VoidedAmount { get; set; }

    /// <summary>
    /// Gets or sets the report generation timestamp.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the name of the user who generated the report.
    /// </summary>
    public string GeneratedBy { get; set; } = string.Empty;
}

/// <summary>
/// Product sales report line item.
/// </summary>
public class ProductSalesReport
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
    /// Gets or sets the quantity sold.
    /// </summary>
    public decimal QuantitySold { get; set; }

    /// <summary>
    /// Gets or sets the gross sales amount.
    /// </summary>
    public decimal GrossSales { get; set; }

    /// <summary>
    /// Gets or sets the discount amount.
    /// </summary>
    public decimal Discounts { get; set; }

    /// <summary>
    /// Gets or sets the net sales amount.
    /// </summary>
    public decimal NetSales { get; set; }

    /// <summary>
    /// Gets or sets the percentage of total sales.
    /// </summary>
    public decimal Percentage { get; set; }

    /// <summary>
    /// Gets or sets the cost of goods sold.
    /// </summary>
    public decimal CostOfGoodsSold { get; set; }

    /// <summary>
    /// Gets or sets the gross profit.
    /// </summary>
    public decimal GrossProfit { get; set; }
}

/// <summary>
/// Category sales report line item.
/// </summary>
public class CategorySalesReport
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
    /// Gets or sets the number of items sold.
    /// </summary>
    public int ItemCount { get; set; }

    /// <summary>
    /// Gets or sets the quantity sold.
    /// </summary>
    public decimal QuantitySold { get; set; }

    /// <summary>
    /// Gets or sets the gross sales amount.
    /// </summary>
    public decimal GrossSales { get; set; }

    /// <summary>
    /// Gets or sets the discount amount.
    /// </summary>
    public decimal Discounts { get; set; }

    /// <summary>
    /// Gets or sets the net sales amount.
    /// </summary>
    public decimal NetSales { get; set; }

    /// <summary>
    /// Gets or sets the percentage of total sales.
    /// </summary>
    public decimal Percentage { get; set; }
}

/// <summary>
/// Cashier sales report line item.
/// </summary>
public class CashierSalesReport
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the cashier name.
    /// </summary>
    public string CashierName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of transactions.
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Gets or sets the total sales amount.
    /// </summary>
    public decimal TotalSales { get; set; }

    /// <summary>
    /// Gets or sets the average transaction value.
    /// </summary>
    public decimal AverageTransaction { get; set; }

    /// <summary>
    /// Gets or sets the total discounts given.
    /// </summary>
    public decimal TotalDiscounts { get; set; }

    /// <summary>
    /// Gets or sets the number of voids.
    /// </summary>
    public int VoidCount { get; set; }

    /// <summary>
    /// Gets or sets the total voided amount.
    /// </summary>
    public decimal VoidAmount { get; set; }
}

/// <summary>
/// Payment method sales report line item.
/// </summary>
public class PaymentMethodSalesReport
{
    /// <summary>
    /// Gets or sets the payment method ID.
    /// </summary>
    public int PaymentMethodId { get; set; }

    /// <summary>
    /// Gets or sets the payment method name.
    /// </summary>
    public string PaymentMethodName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of transactions.
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Gets or sets the total amount.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets the percentage of total sales.
    /// </summary>
    public decimal Percentage { get; set; }
}

/// <summary>
/// Hourly sales report line item.
/// </summary>
public class HourlySalesReport
{
    /// <summary>
    /// Gets or sets the hour (0-23).
    /// </summary>
    public int Hour { get; set; }

    /// <summary>
    /// Gets or sets the formatted hour display (e.g., "09:00 - 10:00").
    /// </summary>
    public string HourDisplay { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of transactions.
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Gets or sets the total sales amount.
    /// </summary>
    public decimal TotalSales { get; set; }

    /// <summary>
    /// Gets or sets the average transaction value.
    /// </summary>
    public decimal AverageTransaction { get; set; }

    /// <summary>
    /// Gets or sets the percentage of daily sales.
    /// </summary>
    public decimal Percentage { get; set; }
}

/// <summary>
/// Complete sales report result container.
/// </summary>
public class SalesReportResult
{
    /// <summary>
    /// Gets or sets the report parameters used.
    /// </summary>
    public SalesReportParameters Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the daily summary (always populated).
    /// </summary>
    public DailySalesSummary Summary { get; set; } = new();

    /// <summary>
    /// Gets or sets the product sales breakdown.
    /// </summary>
    public List<ProductSalesReport> ProductSales { get; set; } = [];

    /// <summary>
    /// Gets or sets the category sales breakdown.
    /// </summary>
    public List<CategorySalesReport> CategorySales { get; set; } = [];

    /// <summary>
    /// Gets or sets the cashier sales breakdown.
    /// </summary>
    public List<CashierSalesReport> CashierSales { get; set; } = [];

    /// <summary>
    /// Gets or sets the payment method breakdown.
    /// </summary>
    public List<PaymentMethodSalesReport> PaymentMethodSales { get; set; } = [];

    /// <summary>
    /// Gets or sets the hourly sales breakdown.
    /// </summary>
    public List<HourlySalesReport> HourlySales { get; set; } = [];

    /// <summary>
    /// Gets or sets when the report was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the name of the user who generated the report.
    /// </summary>
    public string GeneratedBy { get; set; } = string.Empty;
}
