using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using System.Diagnostics;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for recipe cost calculation and analysis.
/// </summary>
public class RecipeCostService : IRecipeCostService
{
    private readonly IRepository<Recipe> _recipeRepository;
    private readonly IRepository<RecipeIngredient> _ingredientRepository;
    private readonly IRepository<RecipeSubRecipe> _subRecipeRepository;
    private readonly IRepository<RecipeCostHistory> _costHistoryRepository;
    private readonly IRepository<UnitConversion> _conversionRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<Category> _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    // Standard unit conversions
    private static readonly Dictionary<(RecipeUnitOfMeasure From, RecipeUnitOfMeasure To), decimal> StandardConversions = new()
    {
        // Weight
        { (RecipeUnitOfMeasure.Gram, RecipeUnitOfMeasure.Kilogram), 0.001m },
        { (RecipeUnitOfMeasure.Kilogram, RecipeUnitOfMeasure.Gram), 1000m },
        { (RecipeUnitOfMeasure.Ounce, RecipeUnitOfMeasure.Gram), 28.3495m },
        { (RecipeUnitOfMeasure.Gram, RecipeUnitOfMeasure.Ounce), 0.035274m },
        { (RecipeUnitOfMeasure.Pound, RecipeUnitOfMeasure.Gram), 453.592m },
        { (RecipeUnitOfMeasure.Gram, RecipeUnitOfMeasure.Pound), 0.00220462m },
        { (RecipeUnitOfMeasure.Pound, RecipeUnitOfMeasure.Ounce), 16m },
        { (RecipeUnitOfMeasure.Ounce, RecipeUnitOfMeasure.Pound), 0.0625m },
        { (RecipeUnitOfMeasure.Kilogram, RecipeUnitOfMeasure.Pound), 2.20462m },
        { (RecipeUnitOfMeasure.Pound, RecipeUnitOfMeasure.Kilogram), 0.453592m },

        // Volume
        { (RecipeUnitOfMeasure.Milliliter, RecipeUnitOfMeasure.Liter), 0.001m },
        { (RecipeUnitOfMeasure.Liter, RecipeUnitOfMeasure.Milliliter), 1000m },
        { (RecipeUnitOfMeasure.Teaspoon, RecipeUnitOfMeasure.Milliliter), 4.92892m },
        { (RecipeUnitOfMeasure.Tablespoon, RecipeUnitOfMeasure.Milliliter), 14.7868m },
        { (RecipeUnitOfMeasure.Cup, RecipeUnitOfMeasure.Milliliter), 236.588m },
        { (RecipeUnitOfMeasure.Milliliter, RecipeUnitOfMeasure.Teaspoon), 0.202884m },
        { (RecipeUnitOfMeasure.Milliliter, RecipeUnitOfMeasure.Tablespoon), 0.067628m },
        { (RecipeUnitOfMeasure.Milliliter, RecipeUnitOfMeasure.Cup), 0.00422675m },
        { (RecipeUnitOfMeasure.Tablespoon, RecipeUnitOfMeasure.Teaspoon), 3m },
        { (RecipeUnitOfMeasure.Teaspoon, RecipeUnitOfMeasure.Tablespoon), 0.333333m },
        { (RecipeUnitOfMeasure.Cup, RecipeUnitOfMeasure.Tablespoon), 16m },
        { (RecipeUnitOfMeasure.Tablespoon, RecipeUnitOfMeasure.Cup), 0.0625m },

        // Small measures
        { (RecipeUnitOfMeasure.Pinch, RecipeUnitOfMeasure.Gram), 0.36m },
        { (RecipeUnitOfMeasure.Gram, RecipeUnitOfMeasure.Pinch), 2.78m },
        { (RecipeUnitOfMeasure.Dash, RecipeUnitOfMeasure.Milliliter), 0.62m },
        { (RecipeUnitOfMeasure.Milliliter, RecipeUnitOfMeasure.Dash), 1.613m }
    };

    public event EventHandler<RecalculateCostsResultDto>? CostsRecalculated;
    public event EventHandler<HighCostAlertDto>? HighCostAlertTriggered;

    public RecipeCostService(
        IRepository<Recipe> recipeRepository,
        IRepository<RecipeIngredient> ingredientRepository,
        IRepository<RecipeSubRecipe> subRecipeRepository,
        IRepository<RecipeCostHistory> costHistoryRepository,
        IRepository<UnitConversion> conversionRepository,
        IRepository<Product> productRepository,
        IRepository<Category> categoryRepository,
        IUnitOfWork unitOfWork)
    {
        _recipeRepository = recipeRepository ?? throw new ArgumentNullException(nameof(recipeRepository));
        _ingredientRepository = ingredientRepository ?? throw new ArgumentNullException(nameof(ingredientRepository));
        _subRecipeRepository = subRecipeRepository ?? throw new ArgumentNullException(nameof(subRecipeRepository));
        _costHistoryRepository = costHistoryRepository ?? throw new ArgumentNullException(nameof(costHistoryRepository));
        _conversionRepository = conversionRepository ?? throw new ArgumentNullException(nameof(conversionRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    #region Cost Calculation

    public async Task<RecipeCostDto> CalculateRecipeCostAsync(int recipeId)
    {
        var recipes = await _recipeRepository.FindAsync(r => r.Id == recipeId && r.IsActive);
        var recipe = recipes.FirstOrDefault();
        if (recipe == null)
            throw new KeyNotFoundException($"Recipe with ID {recipeId} not found.");

        return await CalculateCostInternalAsync(recipe);
    }

    public async Task<RecipeCostAnalysisDto> GetCostAnalysisAsync(int recipeId)
    {
        var recipes = await _recipeRepository.FindAsync(r => r.Id == recipeId && r.IsActive);
        var recipe = recipes.FirstOrDefault();
        if (recipe == null)
            throw new KeyNotFoundException($"Recipe with ID {recipeId} not found.");

        var product = await _productRepository.GetByIdAsync(recipe.ProductId);
        var costResult = await CalculateCostInternalAsync(recipe);

        var analysis = new RecipeCostAnalysisDto
        {
            RecipeId = recipe.Id,
            RecipeName = recipe.Name,
            ProductName = product?.Name ?? string.Empty,
            ProductCode = product?.Code ?? string.Empty,
            CategoryName = product?.Category?.Name,
            YieldQuantity = recipe.YieldQuantity,
            YieldUnit = recipe.YieldUnit,
            TotalIngredientCost = costResult.IngredientCosts.Sum(ic => ic.TotalCost),
            TotalSubRecipeCost = costResult.SubRecipeCosts.Sum(sc => sc.TotalCost),
            TotalCost = costResult.TotalCost,
            CostPerPortion = costResult.CostPerPortion,
            SellingPrice = costResult.SellingPrice,
            GrossProfit = costResult.GrossMargin,
            GrossProfitPerPortion = costResult.GrossMargin / (recipe.YieldQuantity > 0 ? recipe.YieldQuantity : 1),
            FoodCostPercent = costResult.FoodCostPercent,
            GrossMarginPercent = costResult.GrossMarginPercent,
            TargetFoodCostPercent = 30, // Default target
            LastCostCalculation = recipe.LastCostCalculation,
            NeedsRecalculation = await NeedsRecalculationAsync(recipeId)
        };

        analysis.FoodCostVariance = analysis.FoodCostPercent - (analysis.TargetFoodCostPercent ?? 0);
        analysis.IsAboveTargetCost = analysis.FoodCostVariance > 0;
        analysis.DaysSinceLastCalculation = recipe.LastCostCalculation.HasValue
            ? (int)(DateTime.UtcNow - recipe.LastCostCalculation.Value).TotalDays
            : int.MaxValue;

        // Build ingredient breakdown
        var totalCost = costResult.TotalCost > 0 ? costResult.TotalCost : 1;
        analysis.IngredientBreakdown = costResult.IngredientCosts
            .Select(ic => new IngredientCostLineDto
            {
                IngredientProductId = ic.IngredientProductId,
                IngredientName = ic.IngredientName,
                IngredientCode = string.Empty,
                Quantity = ic.Quantity,
                Unit = ic.Unit,
                WastePercent = 0, // Would need additional data
                EffectiveQuantity = ic.EffectiveQuantity,
                UnitCost = ic.UnitCost,
                LineCost = ic.TotalCost,
                PercentOfTotal = ic.PercentOfTotal,
                IsTopCostDriver = ic.PercentOfTotal >= 20
            })
            .OrderByDescending(ic => ic.LineCost)
            .ToList();

        // Build sub-recipe breakdown
        analysis.SubRecipeBreakdown = costResult.SubRecipeCosts
            .Select(sc => new SubRecipeCostLineDto
            {
                SubRecipeId = sc.SubRecipeId,
                SubRecipeName = sc.SubRecipeName,
                Quantity = sc.Quantity,
                CostPerPortion = sc.CostPerPortion,
                LineCost = sc.TotalCost,
                PercentOfTotal = sc.PercentOfTotal
            })
            .OrderByDescending(sc => sc.LineCost)
            .ToList();

        return analysis;
    }

    public async Task<RecalculateCostsResultDto> RecalculateCostsAsync(RecalculateCostsRequestDto request)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new RecalculateCostsResultDto();

        IEnumerable<Recipe> recipesToUpdate;

        if (request.RecalculateAll)
        {
            var allRecipes = await _recipeRepository.FindAsync(r => r.IsActive);
            recipesToUpdate = allRecipes;
        }
        else if (request.RecipeIds?.Any() == true)
        {
            var recipes = await _recipeRepository.FindAsync(r => request.RecipeIds.Contains(r.Id) && r.IsActive);
            recipesToUpdate = recipes;
        }
        else if (request.AffectedIngredientIds?.Any() == true)
        {
            // Find recipes using these ingredients
            var ingredientUsages = await _ingredientRepository.FindAsync(
                i => request.AffectedIngredientIds.Contains(i.IngredientProductId) && i.IsActive);
            var recipeIds = ingredientUsages.Select(i => i.RecipeId).Distinct().ToList();
            var recipes = await _recipeRepository.FindAsync(r => recipeIds.Contains(r.Id) && r.IsActive);
            recipesToUpdate = recipes;
        }
        else
        {
            result.Errors.Add("No recipes specified for recalculation.");
            return result;
        }

        foreach (var recipe in recipesToUpdate)
        {
            try
            {
                var oldCost = recipe.EstimatedCostPerPortion;
                var oldFoodCostPercent = await GetFoodCostPercentAsync(recipe);

                var costResult = await CalculateCostInternalAsync(recipe);
                await SaveCostHistoryAsync(recipe, costResult, request.Reason);

                var newFoodCostPercent = await GetFoodCostPercentAsync(recipe);

                result.Changes.Add(new RecipeCostChangeDto
                {
                    RecipeId = recipe.Id,
                    RecipeName = recipe.Name,
                    OldCostPerPortion = oldCost,
                    NewCostPerPortion = costResult.CostPerPortion,
                    ChangeAmount = costResult.CostPerPortion - oldCost,
                    ChangePercent = oldCost > 0 ? ((costResult.CostPerPortion - oldCost) / oldCost) * 100 : 0,
                    OldFoodCostPercent = oldFoodCostPercent,
                    NewFoodCostPercent = newFoodCostPercent
                });

                result.RecipesUpdated++;
            }
            catch (Exception ex)
            {
                result.RecipesFailed++;
                result.Errors.Add($"Failed to recalculate recipe {recipe.Id} ({recipe.Name}): {ex.Message}");
            }
        }

        stopwatch.Stop();
        result.Duration = stopwatch.Elapsed;

        CostsRecalculated?.Invoke(this, result);

        return result;
    }

    public async Task<int> RecalculateForIngredientAsync(int ingredientProductId, string? reason = null)
    {
        var request = new RecalculateCostsRequestDto
        {
            AffectedIngredientIds = new List<int> { ingredientProductId },
            Reason = reason ?? $"Ingredient price change (Product ID: {ingredientProductId})"
        };

        var result = await RecalculateCostsAsync(request);
        return result.RecipesUpdated;
    }

    #endregion

    #region Cost Reports

    public async Task<CostReportSummaryDto> GetCostReportSummaryAsync(decimal targetFoodCostPercent = 30)
    {
        var recipes = await _recipeRepository.FindAsync(r => r.IsActive);
        var analyses = new List<RecipeCostAnalysisDto>();

        foreach (var recipe in recipes)
        {
            try
            {
                var analysis = await GetCostAnalysisAsync(recipe.Id);
                analysis.TargetFoodCostPercent = targetFoodCostPercent;
                analysis.FoodCostVariance = analysis.FoodCostPercent - targetFoodCostPercent;
                analysis.IsAboveTargetCost = analysis.FoodCostVariance > 0;
                analyses.Add(analysis);
            }
            catch
            {
                // Skip recipes that fail to calculate
            }
        }

        var summary = new CostReportSummaryDto
        {
            TotalRecipes = recipes.Count(),
            RecipesAnalyzed = analyses.Count,
            AverageFoodCostPercent = analyses.Any() ? analyses.Average(a => a.FoodCostPercent) : 0,
            MinFoodCostPercent = analyses.Any() ? analyses.Min(a => a.FoodCostPercent) : 0,
            MaxFoodCostPercent = analyses.Any() ? analyses.Max(a => a.FoodCostPercent) : 0,
            AverageGrossMargin = analyses.Any() ? analyses.Average(a => a.GrossMarginPercent) : 0,
            TotalPotentialRevenue = analyses.Sum(a => a.SellingPrice),
            TotalIngredientCost = analyses.Sum(a => a.TotalCost),
            TotalGrossProfit = analyses.Sum(a => a.GrossProfit),
            RecipesAboveTargetCost = analyses.Count(a => a.IsAboveTargetCost),
            RecipesNeedingUpdate = analyses.Count(a => a.NeedsRecalculation),
            TopCostRecipes = analyses.OrderByDescending(a => a.FoodCostPercent).Take(5).ToList(),
            LowestMarginRecipes = analyses.OrderBy(a => a.GrossMarginPercent).Take(5).ToList(),
            GeneratedAt = DateTime.UtcNow
        };

        // Calculate cost by category
        summary.CostByCategory = await GetCostByCategoryAsync();

        return summary;
    }

    public async Task<List<RecipeCostAnalysisDto>> GetCostReportAsync(CostReportQueryDto query)
    {
        var recipes = await _recipeRepository.FindAsync(r =>
            (query.IncludeInactive || r.IsActive));

        var analyses = new List<RecipeCostAnalysisDto>();

        foreach (var recipe in recipes)
        {
            try
            {
                var analysis = await GetCostAnalysisAsync(recipe.Id);

                // Apply filters
                if (query.CategoryId.HasValue)
                {
                    var product = await _productRepository.GetByIdAsync(recipe.ProductId);
                    if (product?.CategoryId != query.CategoryId)
                        continue;
                }

                if (query.MinFoodCostPercent.HasValue && analysis.FoodCostPercent < query.MinFoodCostPercent)
                    continue;

                if (query.MaxFoodCostPercent.HasValue && analysis.FoodCostPercent > query.MaxFoodCostPercent)
                    continue;

                if (query.OnlyNeedingUpdate && !analysis.NeedsRecalculation)
                    continue;

                analyses.Add(analysis);
            }
            catch
            {
                // Skip recipes that fail
            }
        }

        // Apply sorting
        analyses = query.SortBy?.ToLower() switch
        {
            "foodcostpercent" => query.SortDescending
                ? analyses.OrderByDescending(a => a.FoodCostPercent).ToList()
                : analyses.OrderBy(a => a.FoodCostPercent).ToList(),
            "marginpercent" => query.SortDescending
                ? analyses.OrderByDescending(a => a.GrossMarginPercent).ToList()
                : analyses.OrderBy(a => a.GrossMarginPercent).ToList(),
            "totalcost" => query.SortDescending
                ? analyses.OrderByDescending(a => a.TotalCost).ToList()
                : analyses.OrderBy(a => a.TotalCost).ToList(),
            "name" => query.SortDescending
                ? analyses.OrderByDescending(a => a.RecipeName).ToList()
                : analyses.OrderBy(a => a.RecipeName).ToList(),
            _ => analyses.OrderByDescending(a => a.FoodCostPercent).ToList()
        };

        // Apply pagination
        return analyses.Skip(query.Skip).Take(query.Take).ToList();
    }

    public async Task<List<RecipeCostAnalysisDto>> GetHighFoodCostRecipesAsync(decimal thresholdPercent)
    {
        var query = new CostReportQueryDto
        {
            MinFoodCostPercent = thresholdPercent,
            SortBy = "FoodCostPercent",
            SortDescending = true
        };

        return await GetCostReportAsync(query);
    }

    public async Task<List<RecipeCostAnalysisDto>> GetLowestMarginRecipesAsync(int count = 10)
    {
        var query = new CostReportQueryDto
        {
            SortBy = "MarginPercent",
            SortDescending = false,
            Take = count
        };

        return await GetCostReportAsync(query);
    }

    public async Task<List<CategoryCostSummaryDto>> GetCostByCategoryAsync()
    {
        var recipes = await _recipeRepository.FindAsync(r => r.IsActive);
        var summaries = new Dictionary<int?, CategoryCostSummaryDto>();

        foreach (var recipe in recipes)
        {
            try
            {
                var product = await _productRepository.GetByIdAsync(recipe.ProductId);
                var categoryId = product?.CategoryId;
                var categoryName = product?.Category?.Name ?? "Uncategorized";

                if (!summaries.ContainsKey(categoryId))
                {
                    summaries[categoryId] = new CategoryCostSummaryDto
                    {
                        CategoryId = categoryId,
                        CategoryName = categoryName
                    };
                }

                var analysis = await GetCostAnalysisAsync(recipe.Id);
                var summary = summaries[categoryId];

                summary.RecipeCount++;
                summary.TotalIngredientCost += analysis.TotalCost;
                summary.TotalRevenue += analysis.SellingPrice;
            }
            catch
            {
                // Skip failed recipes
            }
        }

        // Calculate averages
        foreach (var summary in summaries.Values)
        {
            if (summary.RecipeCount > 0 && summary.TotalRevenue > 0)
            {
                summary.AverageFoodCostPercent = (summary.TotalIngredientCost / summary.TotalRevenue) * 100;
                summary.AverageMarginPercent = ((summary.TotalRevenue - summary.TotalIngredientCost) / summary.TotalRevenue) * 100;
            }
        }

        return summaries.Values.OrderBy(s => s.CategoryName).ToList();
    }

    #endregion

    #region Alerts and Monitoring

    public async Task<List<HighCostAlertDto>> GetHighCostAlertsAsync(decimal targetFoodCostPercent = 30)
    {
        var alerts = new List<HighCostAlertDto>();
        var analyses = await GetHighFoodCostRecipesAsync(targetFoodCostPercent);

        foreach (var analysis in analyses)
        {
            var variance = analysis.FoodCostPercent - targetFoodCostPercent;
            var alertLevel = variance > 15 ? "Critical" : "Warning";

            var suggestedPrice = analysis.CostPerPortion / (targetFoodCostPercent / 100);

            var alert = new HighCostAlertDto
            {
                RecipeId = analysis.RecipeId,
                RecipeName = analysis.RecipeName,
                ProductName = analysis.ProductName,
                FoodCostPercent = analysis.FoodCostPercent,
                TargetFoodCostPercent = targetFoodCostPercent,
                VariancePercent = variance,
                CostPerPortion = analysis.CostPerPortion,
                SellingPrice = analysis.SellingPrice,
                SuggestedPriceIncrease = suggestedPrice - analysis.SellingPrice,
                AlertLevel = alertLevel,
                TopCostDrivers = analysis.IngredientBreakdown
                    .Where(i => i.IsTopCostDriver)
                    .Select(i => i.IngredientName)
                    .Take(3)
                    .ToList()
            };

            alerts.Add(alert);
            HighCostAlertTriggered?.Invoke(this, alert);
        }

        return alerts;
    }

    public async Task<List<RecipeListDto>> GetRecipesNeedingUpdateAsync(int staleDays = 7)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-staleDays);
        var recipes = await _recipeRepository.FindAsync(r =>
            r.IsActive && (!r.LastCostCalculation.HasValue || r.LastCostCalculation < cutoffDate));

        return recipes.Select(r => new RecipeListDto
        {
            Id = r.Id,
            ProductId = r.ProductId,
            ProductName = r.Product?.Name ?? string.Empty,
            ProductCode = r.Product?.Code ?? string.Empty,
            Name = r.Name,
            RecipeType = r.RecipeType,
            YieldQuantity = r.YieldQuantity,
            YieldUnit = r.YieldUnit,
            EstimatedCostPerPortion = r.EstimatedCostPerPortion,
            SellingPrice = r.Product?.SellingPrice ?? 0,
            IsApproved = r.IsApproved,
            IsActive = r.IsActive,
            Version = r.Version,
            LastCostCalculation = r.LastCostCalculation
        }).ToList();
    }

    public async Task<bool> NeedsRecalculationAsync(int recipeId)
    {
        var recipes = await _recipeRepository.FindAsync(r => r.Id == recipeId);
        var recipe = recipes.FirstOrDefault();
        if (recipe == null)
            return false;

        // Needs recalculation if never calculated or older than 7 days
        if (!recipe.LastCostCalculation.HasValue)
            return true;

        var daysSinceCalculation = (DateTime.UtcNow - recipe.LastCostCalculation.Value).TotalDays;
        return daysSinceCalculation > 7;
    }

    #endregion

    #region Cost Trends

    public async Task<CostTrendDto> GetCostTrendAsync(int recipeId, int days = 90)
    {
        var recipes = await _recipeRepository.FindAsync(r => r.Id == recipeId);
        var recipe = recipes.FirstOrDefault();
        if (recipe == null)
            throw new KeyNotFoundException($"Recipe with ID {recipeId} not found.");

        var cutoffDate = DateTime.UtcNow.AddDays(-days);
        var history = await _costHistoryRepository.FindAsync(h =>
            h.RecipeId == recipeId && h.CreatedAt >= cutoffDate);

        var trendPoints = history
            .OrderBy(h => h.CreatedAt)
            .Select(h =>
            {
                var product = _productRepository.GetByIdAsync(recipe.ProductId).Result;
                var sellingPrice = product?.SellingPrice ?? 0;
                return new CostTrendPointDto
                {
                    Date = h.CreatedAt,
                    CostPerPortion = h.CostPerPortion,
                    FoodCostPercent = sellingPrice > 0 ? (h.CostPerPortion / sellingPrice) * 100 : 0,
                    ChangeReason = h.CalculationReason
                };
            })
            .ToList();

        var trend = new CostTrendDto
        {
            RecipeId = recipeId,
            RecipeName = recipe.Name,
            TrendPoints = trendPoints,
            CurrentCost = trendPoints.LastOrDefault()?.CostPerPortion ?? recipe.EstimatedCostPerPortion,
            StartCost = trendPoints.FirstOrDefault()?.CostPerPortion ?? recipe.EstimatedCostPerPortion
        };

        if (trend.StartCost > 0)
        {
            trend.PercentChangeOverPeriod = ((trend.CurrentCost - trend.StartCost) / trend.StartCost) * 100;
        }

        trend.TrendDirection = trend.PercentChangeOverPeriod switch
        {
            > 5 => "Increasing",
            < -5 => "Decreasing",
            _ => "Stable"
        };

        return trend;
    }

    public async Task<List<RecipeCostHistoryDto>> GetCostHistoryAsync(int recipeId, int limit = 20)
    {
        var history = await _costHistoryRepository.FindAsync(h => h.RecipeId == recipeId);

        var orderedHistory = history.OrderByDescending(h => h.CreatedAt).Take(limit).ToList();
        var result = new List<RecipeCostHistoryDto>();

        for (int i = 0; i < orderedHistory.Count; i++)
        {
            var entry = orderedHistory[i];
            var previousEntry = i < orderedHistory.Count - 1 ? orderedHistory[i + 1] : null;

            var dto = new RecipeCostHistoryDto
            {
                Id = entry.Id,
                RecipeId = entry.RecipeId,
                CostPerPortion = entry.CostPerPortion,
                TotalCost = entry.TotalCost,
                CalculationReason = entry.CalculationReason,
                CreatedAt = entry.CreatedAt,
                ChangePercent = previousEntry != null && previousEntry.CostPerPortion > 0
                    ? ((entry.CostPerPortion - previousEntry.CostPerPortion) / previousEntry.CostPerPortion) * 100
                    : 0
            };

            result.Add(dto);
        }

        return result;
    }

    #endregion

    #region Pricing

    public async Task<PricingSuggestionDto> GetPricingSuggestionAsync(int recipeId, decimal targetFoodCostPercent)
    {
        var analysis = await GetCostAnalysisAsync(recipeId);

        var suggestedPrice = targetFoodCostPercent > 0
            ? analysis.CostPerPortion / (targetFoodCostPercent / 100)
            : analysis.SellingPrice;

        return new PricingSuggestionDto
        {
            RecipeId = analysis.RecipeId,
            RecipeName = analysis.RecipeName,
            CurrentCostPerPortion = analysis.CostPerPortion,
            CurrentSellingPrice = analysis.SellingPrice,
            CurrentFoodCostPercent = analysis.FoodCostPercent,
            CurrentMarginPercent = analysis.GrossMarginPercent,
            TargetFoodCostPercent = targetFoodCostPercent,
            SuggestedPrice = suggestedPrice,
            PriceAdjustment = suggestedPrice - analysis.SellingPrice,
            ProjectedMarginPercent = suggestedPrice > 0
                ? ((suggestedPrice - analysis.CostPerPortion) / suggestedPrice) * 100
                : 0,
            RequiresPriceIncrease = suggestedPrice > analysis.SellingPrice
        };
    }

    public async Task<List<PricingSuggestionDto>> GetAllPricingSuggestionsAsync(decimal targetFoodCostPercent = 30)
    {
        var highCostRecipes = await GetHighFoodCostRecipesAsync(targetFoodCostPercent);
        var suggestions = new List<PricingSuggestionDto>();

        foreach (var analysis in highCostRecipes)
        {
            var suggestion = await GetPricingSuggestionAsync(analysis.RecipeId, targetFoodCostPercent);
            suggestions.Add(suggestion);
        }

        return suggestions.OrderByDescending(s => s.PriceAdjustment).ToList();
    }

    #endregion

    #region Impact Analysis

    public async Task<IngredientPriceImpactDto> AnalyzePriceImpactAsync(int ingredientProductId, decimal newPrice)
    {
        var ingredient = await _productRepository.GetByIdAsync(ingredientProductId);
        if (ingredient == null)
            throw new KeyNotFoundException($"Ingredient product with ID {ingredientProductId} not found.");

        var oldPrice = ingredient.CostPrice;
        var priceChangePercent = oldPrice > 0 ? ((newPrice - oldPrice) / oldPrice) * 100 : 0;

        var affectedRecipes = await GetRecipesUsingIngredientAsync(ingredientProductId);

        // Update affected recipes with new price calculations
        foreach (var affected in affectedRecipes)
        {
            affected.NewIngredientCost = affected.OldIngredientCost * (newPrice / (oldPrice > 0 ? oldPrice : 1));
            affected.CostImpact = affected.NewIngredientCost - affected.OldIngredientCost;

            // Calculate new food cost percent
            var recipe = (await _recipeRepository.FindAsync(r => r.Id == affected.RecipeId)).FirstOrDefault();
            if (recipe != null)
            {
                var product = await _productRepository.GetByIdAsync(recipe.ProductId);
                var sellingPrice = product?.SellingPrice ?? 0;
                if (sellingPrice > 0)
                {
                    var newTotalCost = recipe.EstimatedCostPerPortion + (affected.CostImpact / recipe.YieldQuantity);
                    affected.NewFoodCostPercent = (newTotalCost / sellingPrice) * 100;
                }
            }
        }

        return new IngredientPriceImpactDto
        {
            IngredientProductId = ingredientProductId,
            IngredientName = ingredient.Name,
            OldPrice = oldPrice,
            NewPrice = newPrice,
            PriceChangePercent = priceChangePercent,
            AffectedRecipes = affectedRecipes,
            TotalCostImpact = affectedRecipes.Sum(r => r.CostImpact)
        };
    }

    public async Task<List<AffectedRecipeDto>> GetRecipesUsingIngredientAsync(int ingredientProductId)
    {
        var ingredientUsages = await _ingredientRepository.FindAsync(i =>
            i.IngredientProductId == ingredientProductId && i.IsActive);

        var affected = new List<AffectedRecipeDto>();

        foreach (var usage in ingredientUsages)
        {
            var recipe = (await _recipeRepository.FindAsync(r => r.Id == usage.RecipeId && r.IsActive)).FirstOrDefault();
            if (recipe == null) continue;

            var ingredient = await _productRepository.GetByIdAsync(ingredientProductId);
            var product = await _productRepository.GetByIdAsync(recipe.ProductId);
            var sellingPrice = product?.SellingPrice ?? 0;

            var ingredientCost = CalculateIngredientCost(usage, ingredient?.CostPrice ?? 0);

            affected.Add(new AffectedRecipeDto
            {
                RecipeId = recipe.Id,
                RecipeName = recipe.Name,
                QuantityUsed = usage.Quantity,
                Unit = usage.Unit.ToString(),
                OldIngredientCost = ingredientCost,
                NewIngredientCost = ingredientCost, // Will be updated in AnalyzePriceImpactAsync
                CostImpact = 0,
                OldFoodCostPercent = sellingPrice > 0 ? (recipe.EstimatedCostPerPortion / sellingPrice) * 100 : 0,
                NewFoodCostPercent = 0 // Will be calculated
            });
        }

        return affected;
    }

    #endregion

    #region Private Methods

    private async Task<RecipeCostDto> CalculateCostInternalAsync(Recipe recipe)
    {
        var ingredientCosts = new List<IngredientCostDto>();
        var subRecipeCosts = new List<SubRecipeCostDto>();
        decimal totalCost = 0;

        // Load ingredients
        var ingredients = await _ingredientRepository.FindAsync(i => i.RecipeId == recipe.Id && i.IsActive);

        // Calculate ingredient costs
        foreach (var ingredient in ingredients)
        {
            var product = await _productRepository.GetByIdAsync(ingredient.IngredientProductId);
            if (product == null) continue;

            var effectiveQuantity = ingredient.Quantity * (1 + ingredient.WastePercent / 100);
            var unitCost = await GetUnitCostAsync(product.CostPrice, ingredient.Unit, ingredient.IngredientProductId);
            var lineCost = effectiveQuantity * unitCost;
            totalCost += lineCost;

            ingredientCosts.Add(new IngredientCostDto
            {
                IngredientProductId = ingredient.IngredientProductId,
                IngredientName = product.Name,
                Quantity = ingredient.Quantity,
                EffectiveQuantity = effectiveQuantity,
                Unit = ingredient.UnitDisplayName,
                UnitCost = unitCost,
                TotalCost = lineCost
            });
        }

        // Load and calculate sub-recipe costs
        var subRecipes = await _subRecipeRepository.FindAsync(sr => sr.ParentRecipeId == recipe.Id && sr.IsActive);
        foreach (var subRecipeUsage in subRecipes)
        {
            var subRecipe = (await _recipeRepository.FindAsync(r => r.Id == subRecipeUsage.SubRecipeId)).FirstOrDefault();
            if (subRecipe == null) continue;

            var subRecipeCost = subRecipe.EstimatedCostPerPortion * subRecipeUsage.Quantity;
            totalCost += subRecipeCost;

            subRecipeCosts.Add(new SubRecipeCostDto
            {
                SubRecipeId = subRecipeUsage.SubRecipeId,
                SubRecipeName = subRecipe.Name,
                Quantity = subRecipeUsage.Quantity,
                CostPerPortion = subRecipe.EstimatedCostPerPortion,
                TotalCost = subRecipeCost
            });
        }

        // Calculate percentages
        if (totalCost > 0)
        {
            foreach (var ic in ingredientCosts)
                ic.PercentOfTotal = (ic.TotalCost / totalCost) * 100;
            foreach (var sc in subRecipeCosts)
                sc.PercentOfTotal = (sc.TotalCost / totalCost) * 100;
        }

        var costPerPortion = recipe.YieldQuantity > 0 ? totalCost / recipe.YieldQuantity : totalCost;

        // Get selling price
        var recipeProduct = await _productRepository.GetByIdAsync(recipe.ProductId);
        var sellingPrice = recipeProduct?.SellingPrice ?? 0;
        var foodCostPercent = sellingPrice > 0 ? (costPerPortion / sellingPrice) * 100 : 0;
        var grossMargin = sellingPrice - costPerPortion;
        var grossMarginPercent = sellingPrice > 0 ? (grossMargin / sellingPrice) * 100 : 0;

        // Update recipe with calculated values
        recipe.EstimatedCostPerPortion = costPerPortion;
        recipe.TotalEstimatedCost = totalCost;
        recipe.LastCostCalculation = DateTime.UtcNow;
        await _recipeRepository.UpdateAsync(recipe);
        await _unitOfWork.SaveChangesAsync();

        return new RecipeCostDto
        {
            RecipeId = recipe.Id,
            RecipeName = recipe.Name,
            TotalCost = totalCost,
            CostPerPortion = costPerPortion,
            YieldQuantity = recipe.YieldQuantity,
            SellingPrice = sellingPrice,
            FoodCostPercent = foodCostPercent,
            GrossMargin = grossMargin,
            GrossMarginPercent = grossMarginPercent,
            IngredientCosts = ingredientCosts,
            SubRecipeCosts = subRecipeCosts,
            CalculatedAt = DateTime.UtcNow
        };
    }

    private async Task<decimal> GetUnitCostAsync(decimal baseCost, RecipeUnitOfMeasure unit, int? productId = null)
    {
        // baseCost is typically per base unit (gram for weight, ml for volume)
        // Convert to the recipe unit

        // Check for product-specific conversions first
        if (productId.HasValue)
        {
            var customConversions = await _conversionRepository.FindAsync(c =>
                c.ProductId == productId && c.IsActive);

            var conversion = customConversions.FirstOrDefault(c =>
                c.FromUnit == RecipeUnitOfMeasure.Gram && c.ToUnit == unit);

            if (conversion != null)
                return baseCost * conversion.ConversionFactor;
        }

        // Use standard conversions
        // Assume baseCost is per gram/ml
        var baseUnit = IsVolumeUnit(unit) ? RecipeUnitOfMeasure.Milliliter : RecipeUnitOfMeasure.Gram;

        if (unit == baseUnit)
            return baseCost;

        if (StandardConversions.TryGetValue((baseUnit, unit), out var factor))
            return baseCost / factor;

        return baseCost;
    }

    private static bool IsVolumeUnit(RecipeUnitOfMeasure unit)
    {
        return unit is RecipeUnitOfMeasure.Milliliter or RecipeUnitOfMeasure.Liter
            or RecipeUnitOfMeasure.Teaspoon or RecipeUnitOfMeasure.Tablespoon
            or RecipeUnitOfMeasure.Cup or RecipeUnitOfMeasure.Dash;
    }

    private decimal CalculateIngredientCost(RecipeIngredient ingredient, decimal unitCost)
    {
        var effectiveQuantity = ingredient.Quantity * (1 + ingredient.WastePercent / 100);
        return effectiveQuantity * unitCost;
    }

    private async Task<decimal> GetFoodCostPercentAsync(Recipe recipe)
    {
        var product = await _productRepository.GetByIdAsync(recipe.ProductId);
        var sellingPrice = product?.SellingPrice ?? 0;
        return sellingPrice > 0 ? (recipe.EstimatedCostPerPortion / sellingPrice) * 100 : 0;
    }

    private async Task SaveCostHistoryAsync(Recipe recipe, RecipeCostDto costResult, string? reason)
    {
        var historyEntry = new RecipeCostHistory
        {
            RecipeId = recipe.Id,
            CostPerPortion = costResult.CostPerPortion,
            TotalCost = costResult.TotalCost,
            CalculationReason = reason,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _costHistoryRepository.AddAsync(historyEntry);
        await _unitOfWork.SaveChangesAsync();
    }

    #endregion
}
