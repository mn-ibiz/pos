using HospitalityPOS.Core.Models.Reports;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for generating prime cost and financial analysis reports.
/// </summary>
public interface IPrimeCostReportService
{
    /// <summary>
    /// Generates a prime cost report.
    /// </summary>
    Task<PrimeCostReport> GeneratePrimeCostReportAsync(
        PrimeCostReportParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a food cost report.
    /// </summary>
    Task<FoodCostReport> GenerateFoodCostReportAsync(
        FoodCostReportParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a labor cost report.
    /// </summary>
    Task<LaborCostReport> GenerateLaborCostReportAsync(
        LaborCostReportParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a profit & loss statement.
    /// </summary>
    Task<ProfitLossStatement> GenerateProfitLossStatementAsync(
        ProfitLossParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a day-part analysis report.
    /// </summary>
    Task<DayPartAnalysisReport> GenerateDayPartAnalysisAsync(
        DayPartAnalysisParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a sales per labor hour (SPLH) report.
    /// </summary>
    Task<SalesPerLaborHourReport> GenerateSPLHReportAsync(
        SPLHReportParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets prime cost trend data over time.
    /// </summary>
    Task<List<PrimeCostTrendPoint>> GetPrimeCostTrendAsync(
        int? storeId,
        DateTime startDate,
        DateTime endDate,
        string interval = "day",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets food cost trend data over time.
    /// </summary>
    Task<List<FoodCostTrendPoint>> GetFoodCostTrendAsync(
        int? storeId,
        DateTime startDate,
        DateTime endDate,
        string interval = "day",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets labor cost trend data over time.
    /// </summary>
    Task<List<LaborCostTrendPoint>> GetLaborCostTrendAsync(
        int? storeId,
        DateTime startDate,
        DateTime endDate,
        string interval = "day",
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Prime cost trend data point.
/// </summary>
public class PrimeCostTrendPoint
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public decimal COGS { get; set; }
    public decimal Labor { get; set; }
    public decimal PrimeCost => COGS + Labor;
    public decimal PrimeCostPercentage => Revenue > 0 ? Math.Round(PrimeCost / Revenue * 100, 2) : 0;
}

/// <summary>
/// Food cost trend data point.
/// </summary>
public class FoodCostTrendPoint
{
    public DateTime Date { get; set; }
    public decimal FoodSales { get; set; }
    public decimal FoodCost { get; set; }
    public decimal FoodCostPercentage => FoodSales > 0 ? Math.Round(FoodCost / FoodSales * 100, 2) : 0;
    public decimal TheoreticalCost { get; set; }
    public decimal Variance => FoodCost - TheoreticalCost;
}

/// <summary>
/// Labor cost trend data point.
/// </summary>
public class LaborCostTrendPoint
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public decimal LaborCost { get; set; }
    public decimal LaborCostPercentage => Revenue > 0 ? Math.Round(LaborCost / Revenue * 100, 2) : 0;
    public decimal LaborHours { get; set; }
    public decimal SPLH => LaborHours > 0 ? Math.Round(Revenue / LaborHours, 2) : 0;
}
