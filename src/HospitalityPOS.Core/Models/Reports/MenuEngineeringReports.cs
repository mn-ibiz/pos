namespace HospitalityPOS.Core.Models.Reports;

/// <summary>
/// Menu item classification based on profitability and popularity.
/// </summary>
public enum MenuItemClassification
{
    /// <summary>High profit, High popularity - Feature prominently.</summary>
    Star,
    /// <summary>Low profit, High popularity - Increase price or reduce cost.</summary>
    Plow,
    /// <summary>High profit, Low popularity - Promote more.</summary>
    Puzzle,
    /// <summary>Low profit, Low popularity - Consider removing.</summary>
    Dog
}

/// <summary>
/// Menu Engineering Report - Analyzes menu items by profitability and popularity.
/// </summary>
public class MenuEngineeringReport
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string GeneratedBy { get; set; } = string.Empty;

    // Summary metrics
    public int TotalItems { get; set; }
    public int TotalItemsSold { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalContributionMargin => TotalRevenue - TotalCost;
    public decimal AverageContributionMargin => TotalItems > 0
        ? Math.Round(TotalContributionMargin / TotalItems, 2) : 0;
    public decimal AveragePopularity => TotalItems > 0
        ? Math.Round((decimal)TotalItemsSold / TotalItems, 2) : 0;

    // Classification counts
    public int StarCount => Items.Count(i => i.Classification == MenuItemClassification.Star);
    public int PlowCount => Items.Count(i => i.Classification == MenuItemClassification.Plow);
    public int PuzzleCount => Items.Count(i => i.Classification == MenuItemClassification.Puzzle);
    public int DogCount => Items.Count(i => i.Classification == MenuItemClassification.Dog);

    // Classification percentages
    public decimal StarPercentage => TotalItems > 0 ? Math.Round((decimal)StarCount / TotalItems * 100, 1) : 0;
    public decimal PlowPercentage => TotalItems > 0 ? Math.Round((decimal)PlowCount / TotalItems * 100, 1) : 0;
    public decimal PuzzlePercentage => TotalItems > 0 ? Math.Round((decimal)PuzzleCount / TotalItems * 100, 1) : 0;
    public decimal DogPercentage => TotalItems > 0 ? Math.Round((decimal)DogCount / TotalItems * 100, 1) : 0;

    // Contribution by classification
    public decimal StarContribution => Items.Where(i => i.Classification == MenuItemClassification.Star)
        .Sum(i => i.ContributionMargin);
    public decimal PlowContribution => Items.Where(i => i.Classification == MenuItemClassification.Plow)
        .Sum(i => i.ContributionMargin);
    public decimal PuzzleContribution => Items.Where(i => i.Classification == MenuItemClassification.Puzzle)
        .Sum(i => i.ContributionMargin);
    public decimal DogContribution => Items.Where(i => i.Classification == MenuItemClassification.Dog)
        .Sum(i => i.ContributionMargin);

    // Items
    public List<MenuEngineeringItem> Items { get; set; } = [];

    // Category breakdown
    public List<MenuEngineeringCategorySummary> CategoryBreakdown { get; set; } = [];

    // Recommendations
    public List<MenuEngineeringRecommendation> Recommendations { get; set; } = [];

    // Thresholds used
    public decimal PopularityThreshold { get; set; }
    public decimal ProfitabilityThreshold { get; set; }
}

/// <summary>
/// Individual menu item analysis.
/// </summary>
public class MenuEngineeringItem
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;

    // Sales data
    public int QuantitySold { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal TotalRevenue { get; set; }

    // Cost data
    public decimal FoodCost { get; set; }
    public decimal TotalCost { get; set; }

    // Contribution margin
    public decimal ContributionMargin => SellingPrice - FoodCost;
    public decimal TotalContributionMargin => TotalRevenue - TotalCost;
    public decimal ContributionMarginPercentage => SellingPrice > 0
        ? Math.Round(ContributionMargin / SellingPrice * 100, 2) : 0;

    // Popularity and profitability indices
    public decimal PopularityIndex { get; set; }
    public decimal ProfitabilityIndex { get; set; }
    public bool IsHighPopularity { get; set; }
    public bool IsHighProfitability { get; set; }

    // Classification
    public MenuItemClassification Classification { get; set; }
    public string ClassificationName => Classification.ToString();
    public string ClassificationColor => Classification switch
    {
        MenuItemClassification.Star => "#10B981",   // Green
        MenuItemClassification.Plow => "#F59E0B",   // Yellow/Orange
        MenuItemClassification.Puzzle => "#6366F1", // Purple
        MenuItemClassification.Dog => "#EF4444",    // Red
        _ => "#6B7280"
    };

    // Menu mix percentage
    public decimal MenuMixPercentage { get; set; }

    // Recommendation
    public string Recommendation { get; set; } = string.Empty;
    public string RecommendationDetail { get; set; } = string.Empty;

    // Trend
    public decimal? PreviousPeriodSales { get; set; }
    public decimal? SalesTrend => PreviousPeriodSales.HasValue && PreviousPeriodSales > 0
        ? Math.Round((QuantitySold - PreviousPeriodSales.Value) / PreviousPeriodSales.Value * 100, 2) : null;
}

/// <summary>
/// Category summary for menu engineering.
/// </summary>
public class MenuEngineeringCategorySummary
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public int TotalSold { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalContribution { get; set; }
    public decimal AverageContributionMargin => TotalItems > 0
        ? Math.Round(TotalContribution / TotalItems, 2) : 0;
    public decimal CategoryMixPercentage { get; set; }

    // Classification breakdown
    public int StarCount { get; set; }
    public int PlowCount { get; set; }
    public int PuzzleCount { get; set; }
    public int DogCount { get; set; }

    // Category health score (0-100)
    public decimal HealthScore { get; set; }
}

/// <summary>
/// Menu engineering recommendation.
/// </summary>
public class MenuEngineeringRecommendation
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public MenuItemClassification CurrentClassification { get; set; }
    public string RecommendationType { get; set; } = string.Empty; // Price, Promote, Reposition, Remove, Reformulate
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ExpectedImpact { get; set; } = string.Empty;
    public decimal? EstimatedRevenueChange { get; set; }
    public decimal? EstimatedProfitChange { get; set; }
    public int Priority { get; set; } // 1 = High, 2 = Medium, 3 = Low
    public string PriorityLabel => Priority switch
    {
        1 => "High",
        2 => "Medium",
        3 => "Low",
        _ => "Unknown"
    };
}

/// <summary>
/// Product Mix (PMIX) Report.
/// </summary>
public class ProductMixReport
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string GeneratedBy { get; set; } = string.Empty;

    public decimal TotalRevenue { get; set; }
    public int TotalItemsSold { get; set; }
    public int TotalTransactions { get; set; }
    public decimal AverageItemsPerTransaction => TotalTransactions > 0
        ? Math.Round((decimal)TotalItemsSold / TotalTransactions, 2) : 0;

    public List<ProductMixCategoryGroup> Categories { get; set; } = [];
    public List<ProductMixItem> AllItems { get; set; } = [];

    // Top performers
    public List<ProductMixItem> TopByRevenue { get; set; } = [];
    public List<ProductMixItem> TopByQuantity { get; set; } = [];
    public List<ProductMixItem> TopByMargin { get; set; } = [];
}

public class ProductMixCategoryGroup
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal RevenuePercentage { get; set; }
    public int QuantitySold { get; set; }
    public decimal QuantityPercentage { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal GrossMargin { get; set; }
    public decimal GrossMarginPercentage => Revenue > 0
        ? Math.Round(GrossMargin / Revenue * 100, 2) : 0;

    public List<ProductMixItem> Items { get; set; } = [];
}

public class ProductMixItem
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
    public decimal Cost { get; set; }
    public decimal GrossMargin => Revenue - Cost;
    public decimal Price { get; set; }
    public decimal RevenuePercentage { get; set; }
    public decimal QuantityPercentage { get; set; }
    public decimal CumulativeRevenuePercentage { get; set; }
    public decimal MarginPercentage => Revenue > 0 ? Math.Round(GrossMargin / Revenue * 100, 2) : 0;
    public int Rank { get; set; }
}

/// <summary>
/// Modifier Analysis Report.
/// </summary>
public class ModifierAnalysisReport
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string GeneratedBy { get; set; } = string.Empty;

    public int TotalOrdersWithModifiers { get; set; }
    public int TotalOrders { get; set; }
    public decimal ModifierAttachRate => TotalOrders > 0
        ? Math.Round((decimal)TotalOrdersWithModifiers / TotalOrders * 100, 2) : 0;

    public decimal TotalModifierRevenue { get; set; }
    public decimal AverageModifierRevenuePerOrder => TotalOrdersWithModifiers > 0
        ? Math.Round(TotalModifierRevenue / TotalOrdersWithModifiers, 2) : 0;

    public List<ModifierGroupAnalysis> ModifierGroups { get; set; } = [];
    public List<ModifierItemAnalysis> TopModifiers { get; set; } = [];
    public List<ProductModifierAnalysis> ProductsWithHighestAttachRate { get; set; } = [];
}

public class ModifierGroupAnalysis
{
    public int ModifierGroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int TimesSelected { get; set; }
    public decimal Revenue { get; set; }
    public decimal AttachRate { get; set; }
    public List<ModifierItemAnalysis> Items { get; set; } = [];
}

public class ModifierItemAnalysis
{
    public int ModifierId { get; set; }
    public string ModifierName { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public int TimesSelected { get; set; }
    public decimal Price { get; set; }
    public decimal Revenue { get; set; }
    public decimal SelectionPercentage { get; set; }
}

public class ProductModifierAnalysis
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int OrdersWithModifier { get; set; }
    public int TotalOrders { get; set; }
    public decimal AttachRate => TotalOrders > 0
        ? Math.Round((decimal)OrdersWithModifier / TotalOrders * 100, 2) : 0;
    public decimal AverageModifierRevenue { get; set; }
}

/// <summary>
/// Parameters for menu engineering report.
/// </summary>
public class MenuEngineeringParameters
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? StoreId { get; set; }
    public int? CategoryId { get; set; }
    public bool IncludeRecommendations { get; set; } = true;
    public bool IncludePreviousPeriod { get; set; } = true;

    // Threshold calculation method
    public ThresholdMethod PopularityThresholdMethod { get; set; } = ThresholdMethod.Average;
    public ThresholdMethod ProfitabilityThresholdMethod { get; set; } = ThresholdMethod.WeightedAverage;

    // Custom thresholds (if method is Custom)
    public decimal? CustomPopularityThreshold { get; set; }
    public decimal? CustomProfitabilityThreshold { get; set; }

    // Minimum sales threshold (exclude items below this)
    public int MinimumSalesThreshold { get; set; } = 0;
}

public enum ThresholdMethod
{
    Average,
    WeightedAverage,
    Median,
    Custom
}

/// <summary>
/// Parameters for product mix report.
/// </summary>
public class ProductMixParameters
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? StoreId { get; set; }
    public int? CategoryId { get; set; }
    public bool GroupByCategory { get; set; } = true;
    public int TopItemsCount { get; set; } = 20;
    public bool IncludeZeroSales { get; set; } = false;
}

/// <summary>
/// Parameters for modifier analysis report.
/// </summary>
public class ModifierAnalysisParameters
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? StoreId { get; set; }
    public int? ProductId { get; set; }
    public int? ModifierGroupId { get; set; }
    public int TopModifiersCount { get; set; } = 20;
}
