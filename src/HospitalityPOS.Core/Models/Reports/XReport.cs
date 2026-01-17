namespace HospitalityPOS.Core.Models.Reports;

/// <summary>
/// Represents an X-Report (running total report) for the current work period.
/// X-Reports do not reset counters and can be generated multiple times during a work period.
/// </summary>
public class XReport
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
    /// Gets or sets when the report was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// Gets or sets the X-report sequence number for this work period.
    /// </summary>
    public int ReportNumber { get; set; }

    /// <summary>
    /// Gets or sets the name of the user who generated the report.
    /// </summary>
    public string GeneratedBy { get; set; } = string.Empty;

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
    /// Gets or sets the duration of the work period.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets the opening float amount.
    /// </summary>
    public decimal OpeningFloat { get; set; }

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

    // Cash Position
    /// <summary>
    /// Gets or sets the expected cash in drawer.
    /// </summary>
    public decimal ExpectedCash { get; set; }
}

/// <summary>
/// Summary of sales by category.
/// </summary>
public class CategorySalesSummary
{
    /// <summary>
    /// Gets or sets the category name.
    /// </summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of items sold.
    /// </summary>
    public int ItemCount { get; set; }

    /// <summary>
    /// Gets or sets the total amount sold.
    /// </summary>
    public decimal TotalAmount { get; set; }
}

/// <summary>
/// Summary of sales by payment method.
/// </summary>
public class PaymentMethodSummary
{
    /// <summary>
    /// Gets or sets the payment method name.
    /// </summary>
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of transactions.
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Gets or sets the total amount.
    /// </summary>
    public decimal TotalAmount { get; set; }
}

/// <summary>
/// Summary of sales by user/cashier.
/// </summary>
public class UserSalesSummary
{
    /// <summary>
    /// Gets or sets the user's full name.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of transactions.
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Gets or sets the total amount.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets the average transaction value.
    /// </summary>
    public decimal AverageTransaction { get; set; }
}

/// <summary>
/// Summary of a voided transaction.
/// </summary>
public class VoidSummary
{
    /// <summary>
    /// Gets or sets the receipt number.
    /// </summary>
    public string ReceiptNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the voided amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the void reason.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets who voided the receipt.
    /// </summary>
    public string VoidedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the receipt was voided.
    /// </summary>
    public DateTime VoidedAt { get; set; }
}
