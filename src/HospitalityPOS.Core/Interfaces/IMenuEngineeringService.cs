using HospitalityPOS.Core.Models.Reports;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for menu engineering and product performance analysis.
/// </summary>
public interface IMenuEngineeringService
{
    /// <summary>
    /// Generates a menu engineering report with classification matrix.
    /// </summary>
    Task<MenuEngineeringReport> GenerateMenuEngineeringReportAsync(
        MenuEngineeringParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a product mix (PMIX) report.
    /// </summary>
    Task<ProductMixReport> GenerateProductMixReportAsync(
        ProductMixParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a modifier analysis report.
    /// </summary>
    Task<ModifierAnalysisReport> GenerateModifierAnalysisReportAsync(
        ModifierAnalysisParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets menu engineering recommendations for specific items.
    /// </summary>
    Task<List<MenuEngineeringRecommendation>> GetRecommendationsAsync(
        int? storeId,
        int? categoryId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the menu engineering classification for a single product.
    /// </summary>
    Task<MenuEngineeringItem> CalculateProductClassificationAsync(
        int productId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets category performance summary.
    /// </summary>
    Task<List<MenuEngineeringCategorySummary>> GetCategoryPerformanceAsync(
        int? storeId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Simulates the impact of a price change.
    /// </summary>
    Task<PriceChangeSimulation> SimulatePriceChangeAsync(
        int productId,
        decimal newPrice,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets trending products (up or down).
    /// </summary>
    Task<TrendingProductsReport> GetTrendingProductsAsync(
        int? storeId,
        int? categoryId,
        int days = 30,
        int topCount = 10,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Price change simulation result.
/// </summary>
public class PriceChangeSimulation
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;

    // Current state
    public decimal CurrentPrice { get; set; }
    public decimal CurrentCost { get; set; }
    public decimal CurrentMargin => CurrentPrice - CurrentCost;
    public decimal CurrentMarginPercentage => CurrentPrice > 0
        ? Math.Round(CurrentMargin / CurrentPrice * 100, 2) : 0;
    public int CurrentMonthlySales { get; set; }
    public decimal CurrentMonthlyRevenue => CurrentPrice * CurrentMonthlySales;
    public decimal CurrentMonthlyProfit => CurrentMargin * CurrentMonthlySales;

    // Proposed state
    public decimal ProposedPrice { get; set; }
    public decimal ProposedMargin => ProposedPrice - CurrentCost;
    public decimal ProposedMarginPercentage => ProposedPrice > 0
        ? Math.Round(ProposedMargin / ProposedPrice * 100, 2) : 0;

    // Price elasticity estimate
    public decimal EstimatedElasticity { get; set; }
    public decimal PriceChangePercentage => CurrentPrice > 0
        ? Math.Round((ProposedPrice - CurrentPrice) / CurrentPrice * 100, 2) : 0;
    public decimal EstimatedSalesChangePercentage => PriceChangePercentage * EstimatedElasticity * -1;

    // Projected impact
    public int ProjectedMonthlySales { get; set; }
    public decimal ProjectedMonthlyRevenue => ProposedPrice * ProjectedMonthlySales;
    public decimal ProjectedMonthlyProfit => ProposedMargin * ProjectedMonthlySales;

    // Net impact
    public decimal RevenueImpact => ProjectedMonthlyRevenue - CurrentMonthlyRevenue;
    public decimal ProfitImpact => ProjectedMonthlyProfit - CurrentMonthlyProfit;

    // Confidence
    public decimal ConfidenceLevel { get; set; }
    public string Recommendation { get; set; } = string.Empty;
}

/// <summary>
/// Trending products report.
/// </summary>
public class TrendingProductsReport
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public int AnalysisDays { get; set; }

    public List<TrendingProduct> TrendingUp { get; set; } = [];
    public List<TrendingProduct> TrendingDown { get; set; } = [];
    public List<TrendingProduct> NewProducts { get; set; } = [];
    public List<TrendingProduct> Declining { get; set; } = [];
}

public class TrendingProduct
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;

    // Current period
    public int CurrentPeriodSales { get; set; }
    public decimal CurrentPeriodRevenue { get; set; }

    // Previous period
    public int PreviousPeriodSales { get; set; }
    public decimal PreviousPeriodRevenue { get; set; }

    // Change
    public decimal SalesChangePercentage { get; set; }
    public decimal RevenueChangePercentage { get; set; }
    public string TrendDirection { get; set; } = string.Empty;

    // Additional insights
    public string TrendReason { get; set; } = string.Empty;
}
