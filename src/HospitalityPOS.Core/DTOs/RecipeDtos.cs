using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.DTOs;

/// <summary>
/// DTO for displaying a recipe.
/// </summary>
public class RecipeDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public RecipeType RecipeType { get; set; }
    public string RecipeTypeName => RecipeType.ToString();
    public decimal YieldQuantity { get; set; }
    public string YieldUnit { get; set; } = string.Empty;
    public int? PrepTimeMinutes { get; set; }
    public int? CookTimeMinutes { get; set; }
    public int? TotalTimeMinutes { get; set; }
    public decimal EstimatedCostPerPortion { get; set; }
    public decimal TotalEstimatedCost { get; set; }
    public DateTime? LastCostCalculation { get; set; }
    public int Version { get; set; }
    public string? Notes { get; set; }
    public bool IsApproved { get; set; }
    public string? ApprovedByUserName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<RecipeIngredientDto> Ingredients { get; set; } = new();
    public List<RecipeSubRecipeDto> SubRecipes { get; set; } = new();
    public int IngredientCount => Ingredients.Count + SubRecipes.Count;
    public decimal SellingPrice { get; set; }
    public decimal FoodCostPercent => SellingPrice > 0
        ? EstimatedCostPerPortion / SellingPrice * 100
        : 0;
}

/// <summary>
/// DTO for creating a recipe.
/// </summary>
public class CreateRecipeDto
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public RecipeType RecipeType { get; set; } = RecipeType.Standard;
    public decimal YieldQuantity { get; set; } = 1;
    public string YieldUnit { get; set; } = "portion";
    public int? PrepTimeMinutes { get; set; }
    public int? CookTimeMinutes { get; set; }
    public string? Notes { get; set; }
    public List<CreateRecipeIngredientDto>? Ingredients { get; set; }
    public List<CreateRecipeSubRecipeDto>? SubRecipes { get; set; }
}

/// <summary>
/// DTO for updating a recipe.
/// </summary>
public class UpdateRecipeDto
{
    public string? Name { get; set; }
    public string? Instructions { get; set; }
    public RecipeType? RecipeType { get; set; }
    public decimal? YieldQuantity { get; set; }
    public string? YieldUnit { get; set; }
    public int? PrepTimeMinutes { get; set; }
    public int? CookTimeMinutes { get; set; }
    public string? Notes { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// DTO for displaying a recipe ingredient.
/// </summary>
public class RecipeIngredientDto
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public int IngredientProductId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public string IngredientCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public RecipeUnitOfMeasure Unit { get; set; }
    public string? CustomUnit { get; set; }
    public string UnitDisplayName { get; set; } = string.Empty;
    public decimal WastePercent { get; set; }
    public decimal EffectiveQuantity { get; set; }
    public bool IsOptional { get; set; }
    public int SortOrder { get; set; }
    public string? PrepNotes { get; set; }
    public decimal CalculatedCost { get; set; }
    public decimal? IngredientCostPrice { get; set; }
    public decimal CurrentStock { get; set; }
    public bool IsLowStock { get; set; }
}

/// <summary>
/// DTO for creating a recipe ingredient.
/// </summary>
public class CreateRecipeIngredientDto
{
    public int IngredientProductId { get; set; }
    public decimal Quantity { get; set; }
    public RecipeUnitOfMeasure Unit { get; set; } = RecipeUnitOfMeasure.Gram;
    public string? CustomUnit { get; set; }
    public decimal WastePercent { get; set; } = 0;
    public bool IsOptional { get; set; } = false;
    public int SortOrder { get; set; }
    public string? PrepNotes { get; set; }
}

/// <summary>
/// DTO for updating a recipe ingredient.
/// </summary>
public class UpdateRecipeIngredientDto
{
    public decimal? Quantity { get; set; }
    public RecipeUnitOfMeasure? Unit { get; set; }
    public string? CustomUnit { get; set; }
    public decimal? WastePercent { get; set; }
    public bool? IsOptional { get; set; }
    public int? SortOrder { get; set; }
    public string? PrepNotes { get; set; }
}

/// <summary>
/// DTO for displaying a sub-recipe usage.
/// </summary>
public class RecipeSubRecipeDto
{
    public int Id { get; set; }
    public int ParentRecipeId { get; set; }
    public int SubRecipeId { get; set; }
    public string SubRecipeName { get; set; } = string.Empty;
    public string SubRecipeProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string YieldUnit { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string? Notes { get; set; }
    public decimal CalculatedCost { get; set; }
    public decimal SubRecipeCostPerPortion { get; set; }
}

/// <summary>
/// DTO for creating a sub-recipe usage.
/// </summary>
public class CreateRecipeSubRecipeDto
{
    public int SubRecipeId { get; set; }
    public decimal Quantity { get; set; }
    public int SortOrder { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for recipe list display.
/// </summary>
public class RecipeListDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public RecipeType RecipeType { get; set; }
    public string RecipeTypeName => RecipeType.ToString();
    public decimal YieldQuantity { get; set; }
    public string YieldUnit { get; set; } = string.Empty;
    public decimal EstimatedCostPerPortion { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal FoodCostPercent { get; set; }
    public int IngredientCount { get; set; }
    public bool IsApproved { get; set; }
    public bool IsActive { get; set; }
    public int Version { get; set; }
    public DateTime? LastCostCalculation { get; set; }
}

/// <summary>
/// DTO for recipe cost calculation result.
/// </summary>
public class RecipeCostDto
{
    public int RecipeId { get; set; }
    public string RecipeName { get; set; } = string.Empty;
    public decimal TotalCost { get; set; }
    public decimal CostPerPortion { get; set; }
    public decimal YieldQuantity { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal FoodCostPercent { get; set; }
    public decimal GrossMargin { get; set; }
    public decimal GrossMarginPercent { get; set; }
    public List<IngredientCostDto> IngredientCosts { get; set; } = new();
    public List<SubRecipeCostDto> SubRecipeCosts { get; set; } = new();
    public DateTime CalculatedAt { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for individual ingredient cost.
/// </summary>
public class IngredientCostDto
{
    public int IngredientProductId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal EffectiveQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public decimal PercentOfTotal { get; set; }
}

/// <summary>
/// DTO for sub-recipe cost.
/// </summary>
public class SubRecipeCostDto
{
    public int SubRecipeId { get; set; }
    public string SubRecipeName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal CostPerPortion { get; set; }
    public decimal TotalCost { get; set; }
    public decimal PercentOfTotal { get; set; }
}

/// <summary>
/// Query for filtering recipes.
/// </summary>
public class RecipeQueryDto
{
    public string? SearchTerm { get; set; }
    public int? ProductId { get; set; }
    public RecipeType? RecipeType { get; set; }
    public bool? IsApproved { get; set; }
    public bool IncludeInactive { get; set; } = false;
    public bool IncludeIngredients { get; set; } = false;
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 50;
    public string SortBy { get; set; } = "Name";
    public bool SortDescending { get; set; } = false;
}

/// <summary>
/// DTO for recipe validation result.
/// </summary>
public class RecipeValidationResultDto
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public bool HasMissingIngredients { get; set; }
    public List<string> MissingIngredients { get; set; } = new();
    public bool HasCircularDependency { get; set; }

    public static RecipeValidationResultDto Success() => new() { IsValid = true };

    public static RecipeValidationResultDto Failure(params string[] errors) => new()
    {
        IsValid = false,
        Errors = errors.ToList()
    };
}

/// <summary>
/// DTO for approving a recipe.
/// </summary>
public class ApproveRecipeDto
{
    public int RecipeId { get; set; }
    public bool Approved { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for recipe cost history.
/// </summary>
public class RecipeCostHistoryDto
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public decimal CostPerPortion { get; set; }
    public decimal TotalCost { get; set; }
    public string? CalculationReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal ChangePercent { get; set; }
}

/// <summary>
/// DTO for unit conversion.
/// </summary>
public class UnitConversionDto
{
    public int Id { get; set; }
    public RecipeUnitOfMeasure FromUnit { get; set; }
    public string FromUnitName => FromUnit.ToString();
    public RecipeUnitOfMeasure ToUnit { get; set; }
    public string ToUnitName => ToUnit.ToString();
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
    public decimal ConversionFactor { get; set; }
}

/// <summary>
/// DTO for creating a unit conversion.
/// </summary>
public class CreateUnitConversionDto
{
    public RecipeUnitOfMeasure FromUnit { get; set; }
    public RecipeUnitOfMeasure ToUnit { get; set; }
    public int? ProductId { get; set; }
    public decimal ConversionFactor { get; set; }
}

/// <summary>
/// Summary of recipes for dashboard.
/// </summary>
public class RecipeSummaryDto
{
    public int TotalRecipes { get; set; }
    public int ApprovedRecipes { get; set; }
    public int PendingApproval { get; set; }
    public int StandardRecipes { get; set; }
    public int SubRecipes { get; set; }
    public int BatchPrepRecipes { get; set; }
    public decimal AverageFoodCostPercent { get; set; }
    public int RecipesNeedingCostUpdate { get; set; }
    public List<RecipeListDto> RecentlyModified { get; set; } = new();
    public List<RecipeListDto> HighFoodCostRecipes { get; set; } = new();
}

#region Recipe Cost Analysis DTOs

/// <summary>
/// Query parameters for cost reports.
/// </summary>
public class CostReportQueryDto
{
    public int? CategoryId { get; set; }
    public decimal? MinFoodCostPercent { get; set; }
    public decimal? MaxFoodCostPercent { get; set; }
    public bool IncludeInactive { get; set; } = false;
    public bool OnlyNeedingUpdate { get; set; } = false;
    public string SortBy { get; set; } = "FoodCostPercent";
    public bool SortDescending { get; set; } = true;
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 50;
}

/// <summary>
/// Detailed cost analysis report for a recipe.
/// </summary>
public class RecipeCostAnalysisDto
{
    public int RecipeId { get; set; }
    public string RecipeName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public decimal YieldQuantity { get; set; }
    public string YieldUnit { get; set; } = string.Empty;

    // Cost Metrics
    public decimal TotalIngredientCost { get; set; }
    public decimal TotalSubRecipeCost { get; set; }
    public decimal TotalCost { get; set; }
    public decimal CostPerPortion { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal GrossProfitPerPortion { get; set; }

    // Percentages
    public decimal FoodCostPercent { get; set; }
    public decimal GrossMarginPercent { get; set; }

    // Targets
    public decimal? TargetFoodCostPercent { get; set; }
    public decimal FoodCostVariance { get; set; }
    public bool IsAboveTargetCost { get; set; }

    // Cost Breakdown
    public List<IngredientCostLineDto> IngredientBreakdown { get; set; } = new();
    public List<SubRecipeCostLineDto> SubRecipeBreakdown { get; set; } = new();

    // Metadata
    public DateTime? LastCostCalculation { get; set; }
    public bool NeedsRecalculation { get; set; }
    public int DaysSinceLastCalculation { get; set; }
}

/// <summary>
/// Single ingredient line in cost breakdown.
/// </summary>
public class IngredientCostLineDto
{
    public int IngredientProductId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public string IngredientCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal WastePercent { get; set; }
    public decimal EffectiveQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineCost { get; set; }
    public decimal PercentOfTotal { get; set; }
    public bool IsTopCostDriver { get; set; }
}

/// <summary>
/// Single sub-recipe line in cost breakdown.
/// </summary>
public class SubRecipeCostLineDto
{
    public int SubRecipeId { get; set; }
    public string SubRecipeName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal CostPerPortion { get; set; }
    public decimal LineCost { get; set; }
    public decimal PercentOfTotal { get; set; }
}

/// <summary>
/// Summary report of all recipe costs.
/// </summary>
public class CostReportSummaryDto
{
    public int TotalRecipes { get; set; }
    public int RecipesAnalyzed { get; set; }
    public decimal AverageFoodCostPercent { get; set; }
    public decimal MinFoodCostPercent { get; set; }
    public decimal MaxFoodCostPercent { get; set; }
    public decimal AverageGrossMargin { get; set; }
    public decimal TotalPotentialRevenue { get; set; }
    public decimal TotalIngredientCost { get; set; }
    public decimal TotalGrossProfit { get; set; }
    public int RecipesAboveTargetCost { get; set; }
    public int RecipesNeedingUpdate { get; set; }
    public List<RecipeCostAnalysisDto> TopCostRecipes { get; set; } = new();
    public List<RecipeCostAnalysisDto> LowestMarginRecipes { get; set; } = new();
    public List<CategoryCostSummaryDto> CostByCategory { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Cost summary by category.
/// </summary>
public class CategoryCostSummaryDto
{
    public int? CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int RecipeCount { get; set; }
    public decimal AverageFoodCostPercent { get; set; }
    public decimal AverageMarginPercent { get; set; }
    public decimal TotalIngredientCost { get; set; }
    public decimal TotalRevenue { get; set; }
}

/// <summary>
/// High food cost alert.
/// </summary>
public class HighCostAlertDto
{
    public int RecipeId { get; set; }
    public string RecipeName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal FoodCostPercent { get; set; }
    public decimal TargetFoodCostPercent { get; set; }
    public decimal VariancePercent { get; set; }
    public decimal CostPerPortion { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal SuggestedPriceIncrease { get; set; }
    public string AlertLevel { get; set; } = string.Empty; // Warning, Critical
    public List<string> TopCostDrivers { get; set; } = new();
}

/// <summary>
/// Cost trend over time for a recipe.
/// </summary>
public class CostTrendDto
{
    public int RecipeId { get; set; }
    public string RecipeName { get; set; } = string.Empty;
    public List<CostTrendPointDto> TrendPoints { get; set; } = new();
    public decimal PercentChangeOverPeriod { get; set; }
    public decimal CurrentCost { get; set; }
    public decimal StartCost { get; set; }
    public string TrendDirection { get; set; } = string.Empty; // Increasing, Decreasing, Stable
}

/// <summary>
/// Single point in cost trend.
/// </summary>
public class CostTrendPointDto
{
    public DateTime Date { get; set; }
    public decimal CostPerPortion { get; set; }
    public decimal FoodCostPercent { get; set; }
    public string? ChangeReason { get; set; }
}

/// <summary>
/// Request to recalculate costs for specific recipes or ingredients.
/// </summary>
public class RecalculateCostsRequestDto
{
    public List<int>? RecipeIds { get; set; }
    public List<int>? AffectedIngredientIds { get; set; }
    public bool RecalculateAll { get; set; } = false;
    public string? Reason { get; set; }
}

/// <summary>
/// Result of cost recalculation.
/// </summary>
public class RecalculateCostsResultDto
{
    public int RecipesUpdated { get; set; }
    public int RecipesFailed { get; set; }
    public List<RecipeCostChangeDto> Changes { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Cost change for a single recipe after recalculation.
/// </summary>
public class RecipeCostChangeDto
{
    public int RecipeId { get; set; }
    public string RecipeName { get; set; } = string.Empty;
    public decimal OldCostPerPortion { get; set; }
    public decimal NewCostPerPortion { get; set; }
    public decimal ChangeAmount { get; set; }
    public decimal ChangePercent { get; set; }
    public decimal OldFoodCostPercent { get; set; }
    public decimal NewFoodCostPercent { get; set; }
}

/// <summary>
/// Pricing suggestion based on target margin.
/// </summary>
public class PricingSuggestionDto
{
    public int RecipeId { get; set; }
    public string RecipeName { get; set; } = string.Empty;
    public decimal CurrentCostPerPortion { get; set; }
    public decimal CurrentSellingPrice { get; set; }
    public decimal CurrentFoodCostPercent { get; set; }
    public decimal CurrentMarginPercent { get; set; }
    public decimal TargetFoodCostPercent { get; set; }
    public decimal SuggestedPrice { get; set; }
    public decimal PriceAdjustment { get; set; }
    public decimal ProjectedMarginPercent { get; set; }
    public bool RequiresPriceIncrease { get; set; }
}

/// <summary>
/// Ingredient price impact analysis.
/// </summary>
public class IngredientPriceImpactDto
{
    public int IngredientProductId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public decimal OldPrice { get; set; }
    public decimal NewPrice { get; set; }
    public decimal PriceChangePercent { get; set; }
    public List<AffectedRecipeDto> AffectedRecipes { get; set; } = new();
    public decimal TotalCostImpact { get; set; }
}

/// <summary>
/// Recipe affected by ingredient price change.
/// </summary>
public class AffectedRecipeDto
{
    public int RecipeId { get; set; }
    public string RecipeName { get; set; } = string.Empty;
    public decimal QuantityUsed { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal OldIngredientCost { get; set; }
    public decimal NewIngredientCost { get; set; }
    public decimal CostImpact { get; set; }
    public decimal OldFoodCostPercent { get; set; }
    public decimal NewFoodCostPercent { get; set; }
}

#endregion

#region Ingredient Deduction DTOs

/// <summary>
/// Request to deduct ingredients for a receipt.
/// </summary>
public class DeductIngredientsRequestDto
{
    public int ReceiptId { get; set; }
    public int? StoreId { get; set; }
    public List<OrderItemDeductionDto> Items { get; set; } = new();
    public bool AllowNegativeStock { get; set; } = false;
    public bool SkipOnError { get; set; } = false;
}

/// <summary>
/// Order item for ingredient deduction.
/// </summary>
public class OrderItemDeductionDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public int? ReceiptLineId { get; set; }
}

/// <summary>
/// Result of ingredient deduction for a receipt.
/// </summary>
public class DeductionResultDto
{
    public int ReceiptId { get; set; }
    public int BatchId { get; set; }
    public bool Success { get; set; }
    public int TotalItems { get; set; }
    public int ItemsWithRecipes { get; set; }
    public int ItemsWithoutRecipes { get; set; }
    public int TotalIngredientsDeducted { get; set; }
    public int SuccessfulDeductions { get; set; }
    public int FailedDeductions { get; set; }
    public int WarningDeductions { get; set; }
    public List<ItemDeductionResultDto> ItemResults { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Deduction result for a single order item.
/// </summary>
public class ItemDeductionResultDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal QuantitySold { get; set; }
    public int? RecipeId { get; set; }
    public string? RecipeName { get; set; }
    public bool HasRecipe { get; set; }
    public List<IngredientDeductionResultDto> IngredientDeductions { get; set; } = new();
    public bool AllDeductionsSuccessful { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Deduction result for a single ingredient.
/// </summary>
public class IngredientDeductionResultDto
{
    public int LogId { get; set; }
    public int IngredientProductId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public decimal QuantityDeducted { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal StockBefore { get; set; }
    public decimal StockAfter { get; set; }
    public DeductionStatusDto Status { get; set; }
    public bool WasForced { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Status of a deduction.
/// </summary>
public enum DeductionStatusDto
{
    Success = 1,
    Warning = 2,
    Failed = 3,
    Skipped = 4
}

/// <summary>
/// DTO for displaying a deduction log entry.
/// </summary>
public class IngredientDeductionLogDto
{
    public int Id { get; set; }
    public int ReceiptId { get; set; }
    public int? ReceiptLineId { get; set; }
    public int RecipeId { get; set; }
    public string RecipeName { get; set; } = string.Empty;
    public int IngredientProductId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public decimal PortionsSold { get; set; }
    public decimal QuantityDeducted { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal StockBefore { get; set; }
    public decimal StockAfter { get; set; }
    public DeductionStatusDto Status { get; set; }
    public string StatusName => Status.ToString();
    public string? ErrorMessage { get; set; }
    public bool WasForced { get; set; }
    public DateTime DeductedAt { get; set; }
    public bool IsReversed { get; set; }
    public DateTime? ReversedAt { get; set; }
}

/// <summary>
/// Query for deduction logs.
/// </summary>
public class DeductionLogQueryDto
{
    public int? ReceiptId { get; set; }
    public int? RecipeId { get; set; }
    public int? IngredientProductId { get; set; }
    public int? StoreId { get; set; }
    public DeductionStatusDto? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool IncludeReversed { get; set; } = false;
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 50;
}

/// <summary>
/// Request to reverse a deduction.
/// </summary>
public class ReverseDeductionRequestDto
{
    public int ReceiptId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Result of reversing a deduction.
/// </summary>
public class ReverseDeductionResultDto
{
    public int ReceiptId { get; set; }
    public bool Success { get; set; }
    public int DeductionsReversed { get; set; }
    public int ReversalsFailed { get; set; }
    public List<IngredientDeductionResultDto> ReversalDetails { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Summary of deductions for reporting.
/// </summary>
public class DeductionSummaryDto
{
    public int TotalDeductions { get; set; }
    public int SuccessfulDeductions { get; set; }
    public int FailedDeductions { get; set; }
    public int WarningDeductions { get; set; }
    public int ForcedDeductions { get; set; }
    public int ReversedDeductions { get; set; }
    public decimal TotalQuantityDeducted { get; set; }
    public List<IngredientDeductionSummaryDto> ByIngredient { get; set; } = new();
    public List<RecipeDeductionSummaryDto> ByRecipe { get; set; } = new();
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
}

/// <summary>
/// Deduction summary by ingredient.
/// </summary>
public class IngredientDeductionSummaryDto
{
    public int IngredientProductId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public decimal TotalQuantityDeducted { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int DeductionCount { get; set; }
    public decimal AverageDeduction { get; set; }
}

/// <summary>
/// Deduction summary by recipe.
/// </summary>
public class RecipeDeductionSummaryDto
{
    public int RecipeId { get; set; }
    public string RecipeName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal TotalPortionsDeducted { get; set; }
    public int DeductionCount { get; set; }
    public int UniqueIngredients { get; set; }
}

/// <summary>
/// Configuration for ingredient deduction behavior.
/// </summary>
public class DeductionConfigDto
{
    public bool Enabled { get; set; } = true;
    public bool AllowNegativeStock { get; set; } = false;
    public bool LogFailures { get; set; } = true;
    public bool ContinueOnError { get; set; } = false;
    public bool AutoReverseOnVoid { get; set; } = true;
    public bool DeductOnSettlement { get; set; } = true;
    public int RetryAttempts { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 100;
}

/// <summary>
/// Low stock warning from deduction.
/// </summary>
public class DeductionLowStockWarningDto
{
    public int IngredientProductId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal ReorderLevel { get; set; }
    public decimal QuantityDeducted { get; set; }
    public bool IsNegative { get; set; }
    public string WarningMessage { get; set; } = string.Empty;
}

#endregion

#region Batch Prep DTOs

/// <summary>
/// DTO for displaying a batch prep record.
/// </summary>
public class BatchPrepDto
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public string RecipeName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int? StoreId { get; set; }
    public string? StoreName { get; set; }
    public decimal BatchSize { get; set; }
    public decimal ExpectedYield { get; set; }
    public decimal ActualYield { get; set; }
    public decimal YieldVariance => ActualYield - ExpectedYield;
    public decimal YieldVariancePercent => ExpectedYield > 0 ? (YieldVariance / ExpectedYield) * 100 : 0;
    public string YieldUnit { get; set; } = string.Empty;
    public BatchPrepStatusDto Status { get; set; }
    public string StatusName => Status.ToString();
    public int? PreparedByUserId { get; set; }
    public string? PreparedByUserName { get; set; }
    public DateTime? PlannedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool IngredientsDeducted { get; set; }
    public bool AddedToInventory { get; set; }
    public decimal? WastedQuantity { get; set; }
    public string? WasteReason { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<BatchPrepIngredientDto> Ingredients { get; set; } = new();
    public decimal EstimatedCost { get; set; }
    public decimal ActualCost { get; set; }
}

/// <summary>
/// Status of a batch preparation.
/// </summary>
public enum BatchPrepStatusDto
{
    Planned = 1,
    InProgress = 2,
    Completed = 3,
    Partial = 4,
    Cancelled = 5,
    Wasted = 6
}

/// <summary>
/// DTO for creating a batch prep.
/// </summary>
public class CreateBatchPrepDto
{
    public int RecipeId { get; set; }
    public int? StoreId { get; set; }
    public decimal BatchSize { get; set; } = 1;
    public DateTime? PlannedAt { get; set; }
    public string? Notes { get; set; }
    public bool StartImmediately { get; set; } = false;
}

/// <summary>
/// DTO for starting a batch prep.
/// </summary>
public class StartBatchPrepDto
{
    public int BatchPrepId { get; set; }
    public int? PreparedByUserId { get; set; }
    public bool DeductIngredients { get; set; } = true;
}

/// <summary>
/// DTO for completing a batch prep.
/// </summary>
public class CompleteBatchPrepDto
{
    public int BatchPrepId { get; set; }
    public decimal ActualYield { get; set; }
    public decimal? WastedQuantity { get; set; }
    public string? WasteReason { get; set; }
    public bool AddToInventory { get; set; } = true;
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for cancelling a batch prep.
/// </summary>
public class CancelBatchPrepDto
{
    public int BatchPrepId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool ReverseDeductions { get; set; } = true;
}

/// <summary>
/// DTO for updating batch prep notes.
/// </summary>
public class UpdateBatchPrepDto
{
    public DateTime? PlannedAt { get; set; }
    public decimal? BatchSize { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for a batch prep ingredient record.
/// </summary>
public class BatchPrepIngredientDto
{
    public int Id { get; set; }
    public int BatchPrepId { get; set; }
    public int IngredientProductId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public string IngredientCode { get; set; } = string.Empty;
    public decimal PlannedQuantity { get; set; }
    public decimal ActualQuantity { get; set; }
    public decimal QuantityVariance => ActualQuantity - PlannedQuantity;
    public string Unit { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public bool WasDeducted { get; set; }
    public int? DeductionLogId { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Query parameters for batch prep records.
/// </summary>
public class BatchPrepQueryDto
{
    public int? RecipeId { get; set; }
    public int? StoreId { get; set; }
    public BatchPrepStatusDto? Status { get; set; }
    public int? PreparedByUserId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool IncludeCancelled { get; set; } = false;
    public string SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 50;
}

/// <summary>
/// List display for batch preps.
/// </summary>
public class BatchPrepListDto
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public string RecipeName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int? StoreId { get; set; }
    public string? StoreName { get; set; }
    public decimal BatchSize { get; set; }
    public decimal ExpectedYield { get; set; }
    public decimal? ActualYield { get; set; }
    public string YieldUnit { get; set; } = string.Empty;
    public BatchPrepStatusDto Status { get; set; }
    public string StatusName => Status.ToString();
    public string? PreparedByUserName { get; set; }
    public DateTime? PlannedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public decimal EstimatedCost { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Summary of batch preps for dashboard.
/// </summary>
public class BatchPrepSummaryDto
{
    public int TotalBatchPreps { get; set; }
    public int PlannedPreps { get; set; }
    public int InProgressPreps { get; set; }
    public int CompletedPreps { get; set; }
    public int CancelledPreps { get; set; }
    public int WastedPreps { get; set; }
    public decimal TotalYieldProduced { get; set; }
    public decimal TotalWastedQuantity { get; set; }
    public decimal AverageYieldVariancePercent { get; set; }
    public decimal TotalIngredientCost { get; set; }
    public decimal AverageBatchCost { get; set; }
    public List<BatchPrepListDto> RecentPreps { get; set; } = new();
    public List<RecipePrepSummaryDto> ByRecipe { get; set; } = new();
}

/// <summary>
/// Prep summary by recipe.
/// </summary>
public class RecipePrepSummaryDto
{
    public int RecipeId { get; set; }
    public string RecipeName { get; set; } = string.Empty;
    public int PrepCount { get; set; }
    public decimal TotalBatches { get; set; }
    public decimal TotalYield { get; set; }
    public decimal AverageYieldVariancePercent { get; set; }
    public decimal TotalCost { get; set; }
}

/// <summary>
/// Result of starting a batch prep.
/// </summary>
public class BatchPrepStartResultDto
{
    public int BatchPrepId { get; set; }
    public bool Success { get; set; }
    public bool IngredientsDeducted { get; set; }
    public List<IngredientDeductionResultDto> DeductionResults { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Result of completing a batch prep.
/// </summary>
public class BatchPrepCompleteResultDto
{
    public int BatchPrepId { get; set; }
    public bool Success { get; set; }
    public decimal ActualYield { get; set; }
    public decimal YieldVariance { get; set; }
    public decimal YieldVariancePercent { get; set; }
    public bool AddedToInventory { get; set; }
    public int? InventoryTransactionId { get; set; }
    public decimal TotalCost { get; set; }
    public decimal CostPerUnit { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

#endregion

#region Ingredient Usage Report DTOs

/// <summary>
/// Query parameters for ingredient usage report.
/// </summary>
public class IngredientUsageQueryDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int? StoreId { get; set; }
    public int? RecipeId { get; set; }
    public int? IngredientProductId { get; set; }
    public int? CategoryId { get; set; }
    public bool IncludeSales { get; set; } = true;
    public bool IncludeBatchPreps { get; set; } = true;
    public bool IncludeWaste { get; set; } = true;
    public string GroupBy { get; set; } = "Ingredient"; // Ingredient, Recipe, Day, Week, Month
}

/// <summary>
/// Ingredient usage report.
/// </summary>
public class IngredientUsageReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int? StoreId { get; set; }
    public string? StoreName { get; set; }
    public int TotalIngredients { get; set; }
    public int TotalRecipes { get; set; }
    public decimal TotalUsageQuantity { get; set; }
    public decimal TotalUsageCost { get; set; }
    public decimal TotalSalesUsage { get; set; }
    public decimal TotalPrepUsage { get; set; }
    public decimal TotalWasteUsage { get; set; }
    public List<IngredientUsageLineDto> Lines { get; set; } = new();
    public List<RecipeUsageSummaryDto> ByRecipe { get; set; } = new();
    public List<DailyUsageSummaryDto> ByDay { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Single ingredient usage line.
/// </summary>
public class IngredientUsageLineDto
{
    public int IngredientProductId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public string IngredientCode { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public decimal TotalQuantityUsed { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal AverageUnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public decimal PercentOfTotal { get; set; }
    public decimal SalesUsage { get; set; }
    public decimal PrepUsage { get; set; }
    public decimal WasteUsage { get; set; }
    public decimal CurrentStock { get; set; }
    public int DaysOfStock { get; set; }
    public List<RecipeIngredientUsageDto> UsageByRecipe { get; set; } = new();
}

/// <summary>
/// Ingredient usage breakdown by recipe.
/// </summary>
public class RecipeIngredientUsageDto
{
    public int RecipeId { get; set; }
    public string RecipeName { get; set; } = string.Empty;
    public decimal QuantityUsed { get; set; }
    public int TimesUsed { get; set; }
    public decimal TotalCost { get; set; }
}

/// <summary>
/// Recipe usage summary.
/// </summary>
public class RecipeUsageSummaryDto
{
    public int RecipeId { get; set; }
    public string RecipeName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int PortionsSold { get; set; }
    public int BatchesPrepped { get; set; }
    public decimal TotalIngredientCost { get; set; }
    public int UniqueIngredientsUsed { get; set; }
    public List<IngredientUsageByRecipeDto> Ingredients { get; set; } = new();
}

/// <summary>
/// Ingredient usage within a recipe.
/// </summary>
public class IngredientUsageByRecipeDto
{
    public int IngredientProductId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public decimal QuantityUsed { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal TotalCost { get; set; }
}

/// <summary>
/// Daily usage summary.
/// </summary>
public class DailyUsageSummaryDto
{
    public DateTime Date { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal TotalCost { get; set; }
    public int UniqueIngredients { get; set; }
    public int PortionsSold { get; set; }
    public int BatchesPrepped { get; set; }
}

/// <summary>
/// Top ingredients by usage.
/// </summary>
public class TopIngredientUsageDto
{
    public int Rank { get; set; }
    public int IngredientProductId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public decimal TotalQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal TotalCost { get; set; }
    public decimal PercentOfTotalCost { get; set; }
    public decimal AverageDailyUsage { get; set; }
}

/// <summary>
/// Usage trend for an ingredient.
/// </summary>
public class IngredientUsageTrendDto
{
    public int IngredientProductId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public List<UsageTrendPointDto> TrendPoints { get; set; } = new();
    public decimal PercentChangeOverPeriod { get; set; }
    public string TrendDirection { get; set; } = string.Empty; // Increasing, Decreasing, Stable
    public decimal AverageDailyUsage { get; set; }
    public decimal PeakUsage { get; set; }
    public DateTime PeakDate { get; set; }
}

/// <summary>
/// Single point in usage trend.
/// </summary>
public class UsageTrendPointDto
{
    public DateTime Date { get; set; }
    public decimal Quantity { get; set; }
    public decimal Cost { get; set; }
}

/// <summary>
/// Usage comparison between periods.
/// </summary>
public class UsageComparisonDto
{
    public DateTime CurrentPeriodStart { get; set; }
    public DateTime CurrentPeriodEnd { get; set; }
    public DateTime PreviousPeriodStart { get; set; }
    public DateTime PreviousPeriodEnd { get; set; }
    public decimal CurrentPeriodTotal { get; set; }
    public decimal PreviousPeriodTotal { get; set; }
    public decimal ChangeAmount { get; set; }
    public decimal ChangePercent { get; set; }
    public List<IngredientUsageChangeDto> IngredientChanges { get; set; } = new();
}

/// <summary>
/// Change in ingredient usage between periods.
/// </summary>
public class IngredientUsageChangeDto
{
    public int IngredientProductId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public decimal CurrentQuantity { get; set; }
    public decimal PreviousQuantity { get; set; }
    public decimal ChangeAmount { get; set; }
    public decimal ChangePercent { get; set; }
    public decimal CurrentCost { get; set; }
    public decimal PreviousCost { get; set; }
    public decimal CostChange { get; set; }
}

#endregion

/// <summary>
/// Simple product DTO for recipe service.
/// </summary>
public class ProductDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal SellingPrice { get; set; }
    public int? CategoryId { get; set; }
}
