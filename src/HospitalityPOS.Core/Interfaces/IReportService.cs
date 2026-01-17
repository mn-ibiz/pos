using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Models.Reports;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for generating sales and business reports.
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Generates a daily sales summary report.
    /// </summary>
    /// <param name="parameters">The report parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The daily sales summary.</returns>
    Task<DailySalesSummary> GenerateDailySummaryAsync(
        SalesReportParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a sales by product report.
    /// </summary>
    /// <param name="parameters">The report parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of product sales data.</returns>
    Task<List<ProductSalesReport>> GenerateProductSalesAsync(
        SalesReportParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a sales by category report.
    /// </summary>
    /// <param name="parameters">The report parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of category sales data.</returns>
    Task<List<CategorySalesReport>> GenerateCategorySalesAsync(
        SalesReportParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a sales by cashier report.
    /// </summary>
    /// <param name="parameters">The report parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of cashier sales data.</returns>
    Task<List<CashierSalesReport>> GenerateCashierSalesAsync(
        SalesReportParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a sales by payment method report.
    /// </summary>
    /// <param name="parameters">The report parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of payment method sales data.</returns>
    Task<List<PaymentMethodSalesReport>> GeneratePaymentMethodSalesAsync(
        SalesReportParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an hourly sales analysis report.
    /// </summary>
    /// <param name="parameters">The report parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of hourly sales data.</returns>
    Task<List<HourlySalesReport>> GenerateHourlySalesAsync(
        SalesReportParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a complete sales report with all breakdowns.
    /// </summary>
    /// <param name="reportType">The type of sales report to generate.</param>
    /// <param name="parameters">The report parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The complete sales report result.</returns>
    Task<SalesReportResult> GenerateSalesReportAsync(
        SalesReportType reportType,
        SalesReportParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a void report.
    /// </summary>
    /// <param name="parameters">The report parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The void report result.</returns>
    Task<VoidReportResult> GenerateVoidReportAsync(
        ExceptionReportParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a discount report.
    /// </summary>
    /// <param name="parameters">The report parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The discount report result.</returns>
    Task<DiscountReportResult> GenerateDiscountReportAsync(
        ExceptionReportParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a current stock report.
    /// </summary>
    /// <param name="parameters">The report parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current stock report result.</returns>
    Task<CurrentStockReportResult> GenerateCurrentStockReportAsync(
        InventoryReportParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a low stock report.
    /// </summary>
    /// <param name="parameters">The report parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The low stock report result.</returns>
    Task<LowStockReportResult> GenerateLowStockReportAsync(
        InventoryReportParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a stock movement report.
    /// </summary>
    /// <param name="parameters">The report parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The stock movement report result.</returns>
    Task<StockMovementReportResult> GenerateStockMovementReportAsync(
        InventoryReportParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a stock valuation report.
    /// </summary>
    /// <param name="parameters">The report parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The stock valuation report result.</returns>
    Task<StockValuationReportResult> GenerateStockValuationReportAsync(
        InventoryReportParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a dead stock report.
    /// </summary>
    /// <param name="parameters">The report parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The dead stock report result.</returns>
    Task<DeadStockReportResult> GenerateDeadStockReportAsync(
        InventoryReportParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a user activity report.
    /// </summary>
    /// <param name="parameters">The report parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user activity report result.</returns>
    Task<UserActivityReportResult> GenerateUserActivityReportAsync(
        AuditReportParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a transaction log report.
    /// </summary>
    /// <param name="parameters">The report parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The transaction log report result.</returns>
    Task<TransactionLogReportResult> GenerateTransactionLogReportAsync(
        AuditReportParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a void/refund log report.
    /// </summary>
    /// <param name="parameters">The report parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The void/refund log report result.</returns>
    Task<VoidRefundLogReportResult> GenerateVoidRefundLogReportAsync(
        AuditReportParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a price change log report.
    /// </summary>
    /// <param name="parameters">The report parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The price change log report result.</returns>
    Task<PriceChangeLogReportResult> GeneratePriceChangeLogReportAsync(
        AuditReportParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a permission override log report.
    /// </summary>
    /// <param name="parameters">The report parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The permission override log report result.</returns>
    Task<PermissionOverrideLogReportResult> GeneratePermissionOverrideLogReportAsync(
        AuditReportParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a combined audit trail report.
    /// </summary>
    /// <param name="parameters">The report parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The audit trail report result.</returns>
    Task<AuditTrailReportResult> GenerateAuditTrailReportAsync(
        AuditReportParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all distinct audit actions for filtering.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of distinct action names.</returns>
    Task<List<string>> GetDistinctAuditActionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all distinct entity types for filtering.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of distinct entity types.</returns>
    Task<List<string>> GetDistinctEntityTypesAsync(CancellationToken cancellationToken = default);
}
