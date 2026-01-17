namespace HospitalityPOS.Core.Models.Reports;

/// <summary>
/// Represents a Z-Report (end-of-day report) generated when closing a work period.
/// Z-Reports finalize the period and reset counters.
/// </summary>
public class ZReport
{
    // Header Information
    /// <summary>
    /// Gets or sets the business name.
    /// </summary>
    public string BusinessName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the business address.
    /// </summary>
    public string BusinessAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the business phone number.
    /// </summary>
    public string BusinessPhone { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Z-Report number (sequential, never resets).
    /// </summary>
    public int ZReportNumber { get; set; }

    // Work Period Information
    /// <summary>
    /// Gets or sets the work period ID.
    /// </summary>
    public int WorkPeriodId { get; set; }

    /// <summary>
    /// Gets or sets when the work period was opened.
    /// </summary>
    public DateTime WorkPeriodOpenedAt { get; set; }

    /// <summary>
    /// Gets or sets who opened the work period.
    /// </summary>
    public string WorkPeriodOpenedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the work period was closed.
    /// </summary>
    public DateTime WorkPeriodClosedAt { get; set; }

    /// <summary>
    /// Gets or sets who closed the work period.
    /// </summary>
    public string WorkPeriodClosedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the duration of the work period.
    /// </summary>
    public TimeSpan Duration { get; set; }

    // Sales Summary
    /// <summary>
    /// Gets or sets the gross sales (before discounts).
    /// </summary>
    public decimal GrossSales { get; set; }

    /// <summary>
    /// Gets or sets the total discounts applied.
    /// </summary>
    public decimal TotalDiscounts { get; set; }

    /// <summary>
    /// Gets or sets the net sales (after discounts, before tax).
    /// </summary>
    public decimal NetSales { get; set; }

    /// <summary>
    /// Gets or sets the tax collected.
    /// </summary>
    public decimal TaxCollected { get; set; }

    /// <summary>
    /// Gets or sets the grand total (net sales + tax).
    /// </summary>
    public decimal GrandTotal { get; set; }

    // Breakdowns
    /// <summary>
    /// Gets or sets the sales by category breakdown.
    /// </summary>
    public List<CategorySalesSummary> SalesByCategory { get; set; } = [];

    /// <summary>
    /// Gets or sets the sales by payment method breakdown.
    /// </summary>
    public List<PaymentMethodSummary> SalesByPaymentMethod { get; set; } = [];

    /// <summary>
    /// Gets or sets the sales by user/cashier breakdown.
    /// </summary>
    public List<UserSalesSummary> SalesByUser { get; set; } = [];

    // Transaction Statistics
    /// <summary>
    /// Gets or sets the total number of transactions.
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Gets or sets the average transaction value.
    /// </summary>
    public decimal AverageTransactionValue { get; set; }

    // Receipt Summary
    /// <summary>
    /// Gets or sets the number of settled receipts.
    /// </summary>
    public int SettledReceiptsCount { get; set; }

    /// <summary>
    /// Gets or sets the total value of settled receipts.
    /// </summary>
    public decimal SettledReceiptsTotal { get; set; }

    /// <summary>
    /// Gets or sets the number of pending/unsettled receipts.
    /// </summary>
    public int PendingReceiptsCount { get; set; }

    /// <summary>
    /// Gets or sets the total value of pending receipts.
    /// </summary>
    public decimal PendingReceiptsTotal { get; set; }

    // Voids Summary
    /// <summary>
    /// Gets or sets the number of voided transactions.
    /// </summary>
    public int VoidCount { get; set; }

    /// <summary>
    /// Gets or sets the total value of voided transactions.
    /// </summary>
    public decimal VoidTotal { get; set; }

    /// <summary>
    /// Gets or sets the list of void summaries.
    /// </summary>
    public List<VoidSummary> Voids { get; set; } = [];

    // Cash Drawer Reconciliation
    /// <summary>
    /// Gets or sets the opening float amount.
    /// </summary>
    public decimal OpeningFloat { get; set; }

    /// <summary>
    /// Gets or sets the total cash sales.
    /// </summary>
    public decimal CashSales { get; set; }

    /// <summary>
    /// Gets or sets the total cash payouts.
    /// </summary>
    public decimal CashPayouts { get; set; }

    /// <summary>
    /// Gets or sets the expected cash in drawer.
    /// </summary>
    public decimal ExpectedCash { get; set; }

    /// <summary>
    /// Gets or sets the actual counted cash.
    /// </summary>
    public decimal ActualCash { get; set; }

    /// <summary>
    /// Gets or sets the variance (actual - expected).
    /// </summary>
    public decimal Variance { get; set; }

    /// <summary>
    /// Gets a value indicating whether the variance is short (negative).
    /// </summary>
    public bool IsShort => Variance < 0;

    /// <summary>
    /// Gets a value indicating whether the variance is over (positive).
    /// </summary>
    public bool IsOver => Variance > 0;

    /// <summary>
    /// Gets the variance status text.
    /// </summary>
    public string VarianceStatus => Variance switch
    {
        < 0 => "SHORT",
        > 0 => "OVER",
        _ => "EXACT"
    };

    // Top Selling Items
    /// <summary>
    /// Gets or sets the top selling items.
    /// </summary>
    public List<ItemSoldSummary> TopSellingItems { get; set; } = [];

    /// <summary>
    /// Gets or sets the closing notes.
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Summary of items sold during the work period.
/// </summary>
public class ItemSoldSummary
{
    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quantity sold.
    /// </summary>
    public int QuantitySold { get; set; }

    /// <summary>
    /// Gets or sets the total value sold.
    /// </summary>
    public decimal TotalValue { get; set; }
}
