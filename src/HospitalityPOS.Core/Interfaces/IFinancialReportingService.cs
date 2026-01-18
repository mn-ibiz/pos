using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for generating enhanced financial reports.
/// </summary>
public interface IFinancialReportingService
{
    #region Cash Flow Statement

    /// <summary>
    /// Generates a cash flow statement.
    /// </summary>
    Task<CashFlowStatement> GenerateCashFlowStatementAsync(CashFlowStatementRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cash flow mappings.
    /// </summary>
    Task<IEnumerable<CashFlowMapping>> GetCashFlowMappingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a cash flow mapping.
    /// </summary>
    Task<CashFlowMapping> SaveCashFlowMappingAsync(CashFlowMapping mapping, CancellationToken cancellationToken = default);

    #endregion

    #region General Ledger

    /// <summary>
    /// Generates a general ledger report.
    /// </summary>
    Task<GeneralLedgerReport> GenerateGeneralLedgerReportAsync(GeneralLedgerReportRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets GL account activity summary.
    /// </summary>
    Task<IEnumerable<GLAccountActivity>> GetGLAccountActivityAsync(int accountId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    #endregion

    #region Gross Margin Analysis

    /// <summary>
    /// Generates a gross margin report.
    /// </summary>
    Task<GrossMarginReport> GenerateGrossMarginReportAsync(GrossMarginReportRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets gross margin by product.
    /// </summary>
    Task<IEnumerable<ProductMarginDetail>> GetProductMarginsAsync(int? storeId, int? categoryId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets gross margin by category.
    /// </summary>
    Task<IEnumerable<CategoryMarginDetail>> GetCategoryMarginsAsync(int? storeId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets low margin product alerts.
    /// </summary>
    Task<IEnumerable<LowMarginAlert>> GetLowMarginAlertsAsync(int? storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets margin thresholds.
    /// </summary>
    Task<IEnumerable<MarginThreshold>> GetMarginThresholdsAsync(int? storeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a margin threshold.
    /// </summary>
    Task<MarginThreshold> SaveMarginThresholdAsync(MarginThreshold threshold, CancellationToken cancellationToken = default);

    #endregion

    #region Comparative Reports

    /// <summary>
    /// Generates a comparative P/L report.
    /// </summary>
    Task<ComparativePLReport> GenerateComparativePLReportAsync(ComparativePLRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a year-over-year comparison.
    /// </summary>
    Task<YearOverYearReport> GenerateYearOverYearReportAsync(int? storeId, DateTime currentPeriodStart, DateTime currentPeriodEnd, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a budget vs actual report.
    /// </summary>
    Task<BudgetVsActualReport> GenerateBudgetVsActualReportAsync(int? storeId, int budgetId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    #endregion

    #region Departmental P/L

    /// <summary>
    /// Generates a departmental P/L report.
    /// </summary>
    Task<DepartmentalPLReport> GenerateDepartmentalPLReportAsync(DepartmentalPLRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets departments.
    /// </summary>
    Task<IEnumerable<Department>> GetDepartmentsAsync(int? storeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a department.
    /// </summary>
    Task<Department> CreateDepartmentAsync(Department department, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a department.
    /// </summary>
    Task<Department> UpdateDepartmentAsync(Department department, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets overhead allocation rules.
    /// </summary>
    Task<IEnumerable<OverheadAllocationRule>> GetOverheadAllocationRulesAsync(int? storeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves an overhead allocation rule.
    /// </summary>
    Task<OverheadAllocationRule> SaveOverheadAllocationRuleAsync(OverheadAllocationRule rule, CancellationToken cancellationToken = default);

    #endregion

    #region Report Management

    /// <summary>
    /// Saves a report configuration.
    /// </summary>
    Task<SavedReport> SaveReportConfigurationAsync(SavedReport report, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets saved reports.
    /// </summary>
    Task<IEnumerable<SavedReport>> GetSavedReportsAsync(int? storeId = null, string? reportType = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a saved report.
    /// </summary>
    Task DeleteSavedReportAsync(int reportId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs report execution.
    /// </summary>
    Task LogReportExecutionAsync(ReportExecutionLog log, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets report execution history.
    /// </summary>
    Task<IEnumerable<ReportExecutionLog>> GetReportExecutionHistoryAsync(int? savedReportId = null, int limit = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports a report to file.
    /// </summary>
    Task<ReportExportResult> ExportReportAsync(string reportType, object reportData, string format, CancellationToken cancellationToken = default);

    #endregion

    #region Margin Trends

    /// <summary>
    /// Gets margin trend data over time.
    /// </summary>
    Task<IEnumerable<MarginTrendPoint>> GetMarginTrendsAsync(int? storeId, int? categoryId, DateTime startDate, DateTime endDate, string interval = "day", CancellationToken cancellationToken = default);

    #endregion
}

#region DTOs

/// <summary>
/// Request for generating a cash flow statement.
/// </summary>
public class CashFlowStatementRequest
{
    public int? StoreId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IncludePriorPeriodComparison { get; set; }
}

/// <summary>
/// Cash flow statement.
/// </summary>
public class CashFlowStatement
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public CashFlowSection OperatingActivities { get; set; } = new();
    public CashFlowSection InvestingActivities { get; set; } = new();
    public CashFlowSection FinancingActivities { get; set; } = new();

    public decimal NetCashFromOperating => OperatingActivities.NetCash;
    public decimal NetCashFromInvesting => InvestingActivities.NetCash;
    public decimal NetCashFromFinancing => FinancingActivities.NetCash;

    public decimal NetChangeInCash => NetCashFromOperating + NetCashFromInvesting + NetCashFromFinancing;
    public decimal BeginningCashBalance { get; set; }
    public decimal EndingCashBalance => BeginningCashBalance + NetChangeInCash;

    // Prior period comparison
    public CashFlowStatement? PriorPeriod { get; set; }
}

/// <summary>
/// Section of the cash flow statement.
/// </summary>
public class CashFlowSection
{
    public CashFlowActivityType ActivityType { get; set; }
    public List<CashFlowLineItem> LineItems { get; set; } = new();
    public decimal TotalInflows => LineItems.Where(l => l.IsInflow).Sum(l => l.Amount);
    public decimal TotalOutflows => LineItems.Where(l => !l.IsInflow).Sum(l => Math.Abs(l.Amount));
    public decimal NetCash => TotalInflows - TotalOutflows;
}

/// <summary>
/// Line item in cash flow statement.
/// </summary>
public class CashFlowLineItem
{
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsInflow { get; set; }
    public int? AccountId { get; set; }
}

/// <summary>
/// Request for generating a general ledger report.
/// </summary>
public class GeneralLedgerReportRequest
{
    public int AccountId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? StoreId { get; set; }
    public bool IncludeSubAccounts { get; set; }
}

/// <summary>
/// General ledger report.
/// </summary>
public class GeneralLedgerReport
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public decimal OpeningBalance { get; set; }
    public decimal TotalDebits { get; set; }
    public decimal TotalCredits { get; set; }
    public decimal ClosingBalance => OpeningBalance + TotalDebits - TotalCredits;

    public List<GLTransaction> Transactions { get; set; } = new();
}

/// <summary>
/// Transaction in the general ledger.
/// </summary>
public class GLTransaction
{
    public DateTime Date { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal RunningBalance { get; set; }
    public int? JournalEntryId { get; set; }
    public string? SourceDocument { get; set; }
}

/// <summary>
/// GL account activity summary.
/// </summary>
public class GLAccountActivity
{
    public DateTime Date { get; set; }
    public decimal TotalDebits { get; set; }
    public decimal TotalCredits { get; set; }
    public int TransactionCount { get; set; }
}

/// <summary>
/// Request for generating a gross margin report.
/// </summary>
public class GrossMarginReportRequest
{
    public int? StoreId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string GroupBy { get; set; } = "Category"; // Product, Category, Supplier, TimePeriod
    public int? CategoryId { get; set; }
    public int? SupplierId { get; set; }
    public bool HighlightLowMargin { get; set; } = true;
}

/// <summary>
/// Gross margin report.
/// </summary>
public class GrossMarginReport
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public decimal TotalRevenue { get; set; }
    public decimal TotalCOGS { get; set; }
    public decimal TotalGrossMargin => TotalRevenue - TotalCOGS;
    public decimal GrossMarginPercent => TotalRevenue > 0 ? (TotalGrossMargin / TotalRevenue) * 100 : 0;

    public List<MarginGroupSummary> GroupSummaries { get; set; } = new();
    public List<LowMarginAlert> LowMarginAlerts { get; set; } = new();
}

/// <summary>
/// Summary of margin by group.
/// </summary>
public class MarginGroupSummary
{
    public int? GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string GroupType { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal COGS { get; set; }
    public decimal GrossMargin => Revenue - COGS;
    public decimal MarginPercent => Revenue > 0 ? (GrossMargin / Revenue) * 100 : 0;
    public int ItemCount { get; set; }
}

/// <summary>
/// Product margin detail.
/// </summary>
public class ProductMarginDetail
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public decimal QuantitySold { get; set; }
    public decimal Revenue { get; set; }
    public decimal COGS { get; set; }
    public decimal GrossMargin => Revenue - COGS;
    public decimal MarginPercent => Revenue > 0 ? (GrossMargin / Revenue) * 100 : 0;
    public decimal AverageSellingPrice => QuantitySold > 0 ? Revenue / QuantitySold : 0;
    public decimal AverageCost => QuantitySold > 0 ? COGS / QuantitySold : 0;
}

/// <summary>
/// Category margin detail.
/// </summary>
public class CategoryMarginDetail
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public decimal Revenue { get; set; }
    public decimal COGS { get; set; }
    public decimal GrossMargin => Revenue - COGS;
    public decimal MarginPercent => Revenue > 0 ? (GrossMargin / Revenue) * 100 : 0;
    public decimal PercentOfTotalRevenue { get; set; }
}

/// <summary>
/// Low margin alert.
/// </summary>
public class LowMarginAlert
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal CurrentMarginPercent { get; set; }
    public decimal ThresholdMarginPercent { get; set; }
    public decimal GapPercent => ThresholdMarginPercent - CurrentMarginPercent;
    public string AlertLevel { get; set; } = "Warning"; // Warning, Critical
}

/// <summary>
/// Request for comparative P/L report.
/// </summary>
public class ComparativePLRequest
{
    public int? StoreId { get; set; }
    public DateTime CurrentPeriodStart { get; set; }
    public DateTime CurrentPeriodEnd { get; set; }
    public DateTime? ComparisonPeriodStart { get; set; }
    public DateTime? ComparisonPeriodEnd { get; set; }
    public int? BudgetId { get; set; }
    public bool IncludeBudgetComparison { get; set; }
}

/// <summary>
/// Comparative P/L report.
/// </summary>
public class ComparativePLReport
{
    public DateTime CurrentPeriodStart { get; set; }
    public DateTime CurrentPeriodEnd { get; set; }
    public DateTime? ComparisonPeriodStart { get; set; }
    public DateTime? ComparisonPeriodEnd { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public PLPeriodSummary CurrentPeriod { get; set; } = new();
    public PLPeriodSummary? ComparisonPeriod { get; set; }
    public PLPeriodSummary? Budget { get; set; }

    public List<ComparativePLLineItem> LineItems { get; set; } = new();
}

/// <summary>
/// P/L period summary.
/// </summary>
public class PLPeriodSummary
{
    public decimal Revenue { get; set; }
    public decimal CostOfGoodsSold { get; set; }
    public decimal GrossProfit => Revenue - CostOfGoodsSold;
    public decimal GrossProfitPercent => Revenue > 0 ? (GrossProfit / Revenue) * 100 : 0;
    public decimal OperatingExpenses { get; set; }
    public decimal OperatingIncome => GrossProfit - OperatingExpenses;
    public decimal OperatingMarginPercent => Revenue > 0 ? (OperatingIncome / Revenue) * 100 : 0;
    public decimal OtherIncome { get; set; }
    public decimal OtherExpenses { get; set; }
    public decimal NetIncome => OperatingIncome + OtherIncome - OtherExpenses;
    public decimal NetIncomePercent => Revenue > 0 ? (NetIncome / Revenue) * 100 : 0;
}

/// <summary>
/// Comparative P/L line item.
/// </summary>
public class ComparativePLLineItem
{
    public string Category { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public int? AccountId { get; set; }
    public decimal CurrentAmount { get; set; }
    public decimal? ComparisonAmount { get; set; }
    public decimal? BudgetAmount { get; set; }
    public decimal Variance => ComparisonAmount.HasValue ? CurrentAmount - ComparisonAmount.Value : 0;
    public decimal VariancePercent => ComparisonAmount.HasValue && ComparisonAmount.Value != 0
        ? (Variance / Math.Abs(ComparisonAmount.Value)) * 100 : 0;
    public decimal BudgetVariance => BudgetAmount.HasValue ? CurrentAmount - BudgetAmount.Value : 0;
    public decimal BudgetVariancePercent => BudgetAmount.HasValue && BudgetAmount.Value != 0
        ? (BudgetVariance / Math.Abs(BudgetAmount.Value)) * 100 : 0;
    public bool IsSignificantVariance { get; set; }
}

/// <summary>
/// Year-over-year comparison report.
/// </summary>
public class YearOverYearReport
{
    public DateTime CurrentPeriodStart { get; set; }
    public DateTime CurrentPeriodEnd { get; set; }
    public DateTime PriorYearStart { get; set; }
    public DateTime PriorYearEnd { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public PLPeriodSummary CurrentYear { get; set; } = new();
    public PLPeriodSummary PriorYear { get; set; } = new();

    public decimal RevenueGrowth => PriorYear.Revenue > 0
        ? ((CurrentYear.Revenue - PriorYear.Revenue) / PriorYear.Revenue) * 100 : 0;
    public decimal NetIncomeGrowth => PriorYear.NetIncome != 0
        ? ((CurrentYear.NetIncome - PriorYear.NetIncome) / Math.Abs(PriorYear.NetIncome)) * 100 : 0;

    public List<YoYLineItem> LineItems { get; set; } = new();
}

/// <summary>
/// Year-over-year line item.
/// </summary>
public class YoYLineItem
{
    public string Category { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal CurrentYearAmount { get; set; }
    public decimal PriorYearAmount { get; set; }
    public decimal Variance => CurrentYearAmount - PriorYearAmount;
    public decimal VariancePercent => PriorYearAmount != 0
        ? (Variance / Math.Abs(PriorYearAmount)) * 100 : 0;
}

/// <summary>
/// Budget vs actual report.
/// </summary>
public class BudgetVsActualReport
{
    public int BudgetId { get; set; }
    public string BudgetName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public decimal TotalBudgeted { get; set; }
    public decimal TotalActual { get; set; }
    public decimal TotalVariance => TotalActual - TotalBudgeted;
    public decimal TotalVariancePercent => TotalBudgeted != 0
        ? (TotalVariance / Math.Abs(TotalBudgeted)) * 100 : 0;

    public List<BudgetVsActualLineItem> LineItems { get; set; } = new();
    public List<BudgetAlert> Alerts { get; set; } = new();
}

/// <summary>
/// Budget vs actual line item.
/// </summary>
public class BudgetVsActualLineItem
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal BudgetAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal Variance => ActualAmount - BudgetAmount;
    public decimal VariancePercent => BudgetAmount != 0
        ? (Variance / Math.Abs(BudgetAmount)) * 100 : 0;
    public decimal PercentUsed => BudgetAmount != 0
        ? (ActualAmount / BudgetAmount) * 100 : 0;
    public bool IsOverBudget => ActualAmount > BudgetAmount;
}

/// <summary>
/// Budget alert.
/// </summary>
public class BudgetAlert
{
    public int AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public decimal VariancePercent { get; set; }
    public string AlertLevel { get; set; } = "Warning";
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Request for departmental P/L report.
/// </summary>
public class DepartmentalPLRequest
{
    public int? StoreId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<int>? DepartmentIds { get; set; }
    public bool IncludeOverheadAllocation { get; set; } = true;
    public bool CompareDepartments { get; set; }
}

/// <summary>
/// Departmental P/L report.
/// </summary>
public class DepartmentalPLReport
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public List<DepartmentPL> DepartmentResults { get; set; } = new();

    public decimal TotalRevenue => DepartmentResults.Sum(d => d.Revenue);
    public decimal TotalGrossProfit => DepartmentResults.Sum(d => d.GrossProfit);
    public decimal TotalOperatingIncome => DepartmentResults.Sum(d => d.OperatingIncome);
}

/// <summary>
/// P/L for a single department.
/// </summary>
public class DepartmentPL
{
    public int DepartmentId { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;

    public decimal Revenue { get; set; }
    public decimal CostOfGoodsSold { get; set; }
    public decimal GrossProfit => Revenue - CostOfGoodsSold;
    public decimal GrossProfitPercent => Revenue > 0 ? (GrossProfit / Revenue) * 100 : 0;

    public decimal DirectExpenses { get; set; }
    public decimal AllocatedOverhead { get; set; }
    public decimal TotalExpenses => DirectExpenses + AllocatedOverhead;

    public decimal OperatingIncome => GrossProfit - TotalExpenses;
    public decimal OperatingMarginPercent => Revenue > 0 ? (OperatingIncome / Revenue) * 100 : 0;

    public decimal PercentOfTotalRevenue { get; set; }
    public List<DepartmentExpenseDetail> ExpenseDetails { get; set; } = new();
}

/// <summary>
/// Department expense detail.
/// </summary>
public class DepartmentExpenseDetail
{
    public int? AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsAllocated { get; set; }
}

/// <summary>
/// Margin trend data point.
/// </summary>
public class MarginTrendPoint
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public decimal COGS { get; set; }
    public decimal GrossMargin => Revenue - COGS;
    public decimal MarginPercent => Revenue > 0 ? (GrossMargin / Revenue) * 100 : 0;
}

/// <summary>
/// Report export result.
/// </summary>
public class ReportExportResult
{
    public bool IsSuccess { get; set; }
    public string? FilePath { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public byte[]? FileContent { get; set; }
    public string? ErrorMessage { get; set; }
}

#endregion
