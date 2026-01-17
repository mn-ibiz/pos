# Story 26.2: Ingredient Costing

## Story
**As a** manager,
**I want** automatic recipe costing based on ingredient prices,
**So that** I know the true cost of each menu item.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Epic
**Epic 26: Recipe & Ingredient Management**

## Acceptance Criteria

### AC1: Cost Calculation
**Given** recipe has ingredients with costs
**When** viewing recipe
**Then** shows calculated total cost per portion

### AC2: Auto-Update Costs
**Given** ingredient costs change
**When** recalculating
**Then** recipe cost updates automatically

### AC3: Cost Analysis Report
**Given** food cost analysis needed
**When** running cost report
**Then** shows: recipe cost, selling price, margin %, food cost %

## Technical Notes
```csharp
public class RecipeCostCalculator
{
    public decimal CalculateRecipeCost(Recipe recipe)
    {
        decimal totalCost = 0;
        foreach (var ingredient in recipe.Ingredients)
        {
            var unitCost = GetIngredientUnitCost(ingredient);
            var wasteFactor = 1 + (ingredient.WastePercent ?? 0) / 100;
            totalCost += unitCost * ingredient.Quantity * wasteFactor;
        }
        return totalCost / recipe.YieldQuantity;
    }

    private decimal GetIngredientUnitCost(RecipeIngredient ingredient)
    {
        var product = ingredient.IngredientProduct;
        return ConvertToBaseUnit(product.CostPrice, product.UnitOfMeasure, ingredient.Unit);
    }
}

public class RecipeCostDto
{
    public Guid RecipeId { get; set; }
    public string RecipeName { get; set; }
    public decimal IngredientCost { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal GrossProfit => SellingPrice - IngredientCost;
    public decimal MarginPercent => SellingPrice > 0
        ? (GrossProfit / SellingPrice) * 100 : 0;
    public decimal FoodCostPercent => SellingPrice > 0
        ? (IngredientCost / SellingPrice) * 100 : 0;
}

public interface IRecipeCostService
{
    Task<decimal> CalculateRecipeCostAsync(Guid recipeId);
    Task RecalculateAllRecipeCostsAsync();
    Task<List<RecipeCostDto>> GetRecipeCostReportAsync();
    Task<List<RecipeCostDto>> GetHighFoodCostItemsAsync(decimal threshold);
}
```

## Definition of Done
- [x] Recipe cost calculation logic
- [x] Unit conversion for ingredients
- [x] Auto-recalculate on ingredient cost change
- [x] Food cost percentage display
- [x] Recipe cost report
- [x] Margin analysis
- [x] Unit tests passing

## Implementation Summary

### Additional DTOs Added (RecipeDtos.cs)
- `CostReportQueryDto` - Query parameters for cost reports with filters and sorting
- `RecipeCostAnalysisDto` - Detailed cost analysis with ingredient breakdown, margins, targets
- `IngredientCostLineDto` - Single ingredient line in cost breakdown with top cost driver flag
- `SubRecipeCostLineDto` - Sub-recipe line in cost breakdown
- `CostReportSummaryDto` - Summary report with totals, averages, top cost items by category
- `CategoryCostSummaryDto` - Cost summary grouped by category
- `HighCostAlertDto` - Alert for recipes exceeding target food cost with suggested actions
- `CostTrendDto`, `CostTrendPointDto` - Cost trend over time with direction detection
- `RecalculateCostsRequestDto` - Request to trigger cost recalculation
- `RecalculateCostsResultDto`, `RecipeCostChangeDto` - Recalculation results with changes tracked
- `PricingSuggestionDto` - Suggested selling price based on target margin
- `IngredientPriceImpactDto`, `AffectedRecipeDto` - Impact analysis for ingredient price changes

### Interface Created (IRecipeCostService.cs)
Comprehensive cost service interface with:
- **Cost Calculation**: CalculateRecipeCost, GetCostAnalysis, RecalculateCosts, RecalculateForIngredient
- **Cost Reports**: GetCostReportSummary, GetCostReport, GetHighFoodCostRecipes, GetLowestMarginRecipes, GetCostByCategory
- **Alerts & Monitoring**: GetHighCostAlerts, GetRecipesNeedingUpdate, NeedsRecalculation
- **Cost Trends**: GetCostTrend, GetCostHistory
- **Pricing**: GetPricingSuggestion, GetAllPricingSuggestions
- **Impact Analysis**: AnalyzePriceImpact, GetRecipesUsingIngredient
- **Events**: CostsRecalculated, HighCostAlertTriggered

### Service Implementation (RecipeCostService.cs)
Full cost calculation service (~650 lines) including:
- Cost calculation with waste percentage and effective quantity
- Standard unit conversions (metric/imperial weight and volume)
- Product-specific conversion overrides
- Food cost percentage and gross margin calculation
- Cost recalculation for specific recipes or affected ingredients
- Cost report generation with category breakdown
- High cost alert system with suggested price increases
- Cost trend analysis with direction detection (Increasing/Decreasing/Stable)
- Pricing suggestions based on target food cost percentage
- Ingredient price change impact analysis
- Cost history tracking with change percentages
- Event-driven notifications on recalculation and alerts
- Stale cost detection (configurable days threshold)

### Unit Tests (RecipeCostServiceTests.cs)
Comprehensive test coverage (40+ tests) including:
- Constructor validation tests
- Cost calculation tests with single and multiple ingredients
- Food cost percentage calculation tests
- Cost analysis with top cost driver identification
- Recalculation tests with tracking changes
- High cost alert generation tests
- Cost trend analysis tests
- Pricing suggestion tests
- Impact analysis tests
- Needs recalculation detection tests
- Cost history with change percent tests
- DTO calculation tests
