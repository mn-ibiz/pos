using HospitalityPOS.Core.DTOs;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for recipe cost calculation and analysis.
/// </summary>
public interface IRecipeCostService
{
    #region Cost Calculation

    /// <summary>
    /// Calculates the cost for a single recipe.
    /// </summary>
    /// <param name="recipeId">The recipe ID.</param>
    /// <returns>Cost calculation result.</returns>
    Task<RecipeCostDto> CalculateRecipeCostAsync(int recipeId);

    /// <summary>
    /// Gets detailed cost analysis for a recipe.
    /// </summary>
    /// <param name="recipeId">The recipe ID.</param>
    /// <returns>Detailed cost analysis.</returns>
    Task<RecipeCostAnalysisDto> GetCostAnalysisAsync(int recipeId);

    /// <summary>
    /// Recalculates costs for specified recipes or all recipes.
    /// </summary>
    /// <param name="request">Recalculation request.</param>
    /// <returns>Recalculation result.</returns>
    Task<RecalculateCostsResultDto> RecalculateCostsAsync(RecalculateCostsRequestDto request);

    /// <summary>
    /// Recalculates costs for all recipes affected by an ingredient price change.
    /// </summary>
    /// <param name="ingredientProductId">The ingredient product ID.</param>
    /// <param name="reason">Reason for recalculation.</param>
    /// <returns>Number of recipes updated.</returns>
    Task<int> RecalculateForIngredientAsync(int ingredientProductId, string? reason = null);

    #endregion

    #region Cost Reports

    /// <summary>
    /// Gets a summary report of all recipe costs.
    /// </summary>
    /// <param name="targetFoodCostPercent">Target food cost percentage for comparison.</param>
    /// <returns>Cost report summary.</returns>
    Task<CostReportSummaryDto> GetCostReportSummaryAsync(decimal targetFoodCostPercent = 30);

    /// <summary>
    /// Gets cost analysis for multiple recipes based on query.
    /// </summary>
    /// <param name="query">Query parameters.</param>
    /// <returns>List of recipe cost analyses.</returns>
    Task<List<RecipeCostAnalysisDto>> GetCostReportAsync(CostReportQueryDto query);

    /// <summary>
    /// Gets recipes with food cost above threshold.
    /// </summary>
    /// <param name="thresholdPercent">Food cost threshold percentage.</param>
    /// <returns>List of high cost recipes.</returns>
    Task<List<RecipeCostAnalysisDto>> GetHighFoodCostRecipesAsync(decimal thresholdPercent);

    /// <summary>
    /// Gets recipes with lowest profit margins.
    /// </summary>
    /// <param name="count">Number of recipes to return.</param>
    /// <returns>List of low margin recipes.</returns>
    Task<List<RecipeCostAnalysisDto>> GetLowestMarginRecipesAsync(int count = 10);

    /// <summary>
    /// Gets cost summary by category.
    /// </summary>
    /// <returns>List of category cost summaries.</returns>
    Task<List<CategoryCostSummaryDto>> GetCostByCategoryAsync();

    #endregion

    #region Alerts and Monitoring

    /// <summary>
    /// Gets all high cost alerts.
    /// </summary>
    /// <param name="targetFoodCostPercent">Target food cost percentage.</param>
    /// <returns>List of high cost alerts.</returns>
    Task<List<HighCostAlertDto>> GetHighCostAlertsAsync(decimal targetFoodCostPercent = 30);

    /// <summary>
    /// Gets recipes needing cost recalculation.
    /// </summary>
    /// <param name="staleDays">Days since last calculation to consider stale.</param>
    /// <returns>List of recipes needing update.</returns>
    Task<List<RecipeListDto>> GetRecipesNeedingUpdateAsync(int staleDays = 7);

    /// <summary>
    /// Checks if a recipe's cost needs recalculation.
    /// </summary>
    /// <param name="recipeId">The recipe ID.</param>
    /// <returns>True if needs recalculation.</returns>
    Task<bool> NeedsRecalculationAsync(int recipeId);

    #endregion

    #region Cost Trends

    /// <summary>
    /// Gets cost trend for a recipe over time.
    /// </summary>
    /// <param name="recipeId">The recipe ID.</param>
    /// <param name="days">Number of days to look back.</param>
    /// <returns>Cost trend data.</returns>
    Task<CostTrendDto> GetCostTrendAsync(int recipeId, int days = 90);

    /// <summary>
    /// Gets cost history for a recipe.
    /// </summary>
    /// <param name="recipeId">The recipe ID.</param>
    /// <param name="limit">Maximum entries to return.</param>
    /// <returns>Cost history entries.</returns>
    Task<List<RecipeCostHistoryDto>> GetCostHistoryAsync(int recipeId, int limit = 20);

    #endregion

    #region Pricing

    /// <summary>
    /// Gets pricing suggestion for a recipe based on target margin.
    /// </summary>
    /// <param name="recipeId">The recipe ID.</param>
    /// <param name="targetFoodCostPercent">Target food cost percentage.</param>
    /// <returns>Pricing suggestion.</returns>
    Task<PricingSuggestionDto> GetPricingSuggestionAsync(int recipeId, decimal targetFoodCostPercent);

    /// <summary>
    /// Gets pricing suggestions for all recipes above target cost.
    /// </summary>
    /// <param name="targetFoodCostPercent">Target food cost percentage.</param>
    /// <returns>List of pricing suggestions.</returns>
    Task<List<PricingSuggestionDto>> GetAllPricingSuggestionsAsync(decimal targetFoodCostPercent = 30);

    #endregion

    #region Impact Analysis

    /// <summary>
    /// Analyzes the impact of an ingredient price change.
    /// </summary>
    /// <param name="ingredientProductId">The ingredient product ID.</param>
    /// <param name="newPrice">New price for the ingredient.</param>
    /// <returns>Impact analysis.</returns>
    Task<IngredientPriceImpactDto> AnalyzePriceImpactAsync(int ingredientProductId, decimal newPrice);

    /// <summary>
    /// Gets all recipes using a specific ingredient.
    /// </summary>
    /// <param name="ingredientProductId">The ingredient product ID.</param>
    /// <returns>List of affected recipes.</returns>
    Task<List<AffectedRecipeDto>> GetRecipesUsingIngredientAsync(int ingredientProductId);

    #endregion

    #region Events

    /// <summary>
    /// Event raised when recipe costs are recalculated.
    /// </summary>
    event EventHandler<RecalculateCostsResultDto>? CostsRecalculated;

    /// <summary>
    /// Event raised when a high cost alert is triggered.
    /// </summary>
    event EventHandler<HighCostAlertDto>? HighCostAlertTriggered;

    #endregion
}
