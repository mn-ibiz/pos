# Story 26.3: Automatic Ingredient Deduction

## Story
**As the** system,
**I want to** deduct ingredients when menu items are sold,
**So that** inventory reflects actual usage.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Epic
**Epic 26: Recipe & Ingredient Management**

## Acceptance Criteria

### AC1: Auto-Deduction on Sale
**Given** menu item with recipe is sold
**When** receipt is settled
**Then** all ingredients are deducted per recipe quantities

### AC2: Quantity Multiplication
**Given** item quantity > 1
**When** deducting
**Then** multiplies ingredient quantities correctly

### AC3: Low Stock Handling
**Given** ingredient deduction fails (insufficient stock)
**When** processing
**Then** logs warning but allows sale (configurable)

## Technical Notes
```csharp
public class RecipeIngredientDeductionService
{
    private readonly IRecipeService _recipeService;
    private readonly IInventoryService _inventoryService;
    private readonly IConfiguration _config;

    public async Task DeductIngredientsForSaleAsync(OrderItem orderItem)
    {
        var recipe = await _recipeService.GetRecipeByProductAsync(orderItem.ProductId);
        if (recipe == null) return;  // No recipe = no deduction

        var multiplier = orderItem.Quantity / recipe.YieldQuantity;

        foreach (var ingredient in recipe.Ingredients)
        {
            var deductQty = ingredient.Quantity * multiplier;

            try
            {
                await _inventoryService.DeductStockAsync(
                    ingredient.IngredientProductId,
                    deductQty,
                    $"Recipe deduction: {recipe.Name}"
                );
            }
            catch (InsufficientStockException ex)
            {
                if (_config.GetValue<bool>("AllowNegativeStock"))
                {
                    await _inventoryService.ForceDeductStockAsync(
                        ingredient.IngredientProductId,
                        deductQty,
                        $"Recipe deduction (forced): {recipe.Name}"
                    );
                    _logger.LogWarning($"Negative stock for {ex.ProductName}");
                }
                else
                {
                    _logger.LogError($"Insufficient stock for {ex.ProductName}");
                    throw;
                }
            }
        }
    }
}

public class IngredientDeductionLog
{
    public Guid Id { get; set; }
    public Guid ReceiptId { get; set; }
    public Guid RecipeId { get; set; }
    public Guid IngredientProductId { get; set; }
    public decimal QuantityDeducted { get; set; }
    public decimal StockBefore { get; set; }
    public decimal StockAfter { get; set; }
    public DateTime DeductedAt { get; set; }
}
```

## Definition of Done
- [x] Auto-deduction on receipt settlement
- [x] Quantity multiplication logic
- [x] Configurable negative stock handling
- [x] Deduction logging/audit trail
- [x] Integration with inventory service
- [x] Event hook for order completion
- [x] Unit tests passing

## Implementation Summary

### Entities Added (RecipeEntities.cs)
- `DeductionStatus` enum - Success, Warning, Failed, Skipped, Reversed
- `IngredientDeductionLog` - Audit trail with receipt/recipe/ingredient links, stock before/after, status, force flag
- `ReceiptDeductionBatch` - Batch record for processing receipts with success/failure counts

### DTOs Added (RecipeDtos.cs)
- `DeductIngredientsRequestDto` - Request with order items, store, negative stock handling
- `OrderItemDeductionDto` - Single order item for deduction
- `DeductionResultDto` - Result with item counts, successes, failures, warnings
- `ItemDeductionResultDto` - Result for single order item with ingredient deductions
- `IngredientDeductionResultDto` - Single ingredient deduction result
- `DeductionStatusDto` enum - DTO version of status
- `IngredientDeductionLogDto` - Display log entry
- `DeductionLogQueryDto` - Query parameters for logs
- `ReverseDeductionRequestDto`, `ReverseDeductionResultDto` - Reversal handling
- `DeductionSummaryDto` - Summary by ingredient and recipe
- `IngredientDeductionSummaryDto`, `RecipeDeductionSummaryDto` - Summary breakdowns
- `DeductionConfigDto` - Configuration for behavior
- `DeductionLowStockWarningDto` - Low stock warning
- `DeductionValidationResultDto`, `IngredientAvailabilityDto` - Pre-sale validation

### Interface Created (IIngredientDeductionService.cs)
Comprehensive interface with:
- **Deduction Operations**: DeductIngredients, DeductForItem, ReverseDeductions, ReverseDeduction
- **Query Operations**: GetDeductionLogs, GetDeductionsForReceipt, GetDeductionsForIngredient, GetDeductionSummary
- **Validation**: ValidateDeduction, GetLowStockWarnings
- **Configuration**: GetConfiguration, UpdateConfiguration, IsEnabled property
- **Events**: IngredientsDeducted, DeductionFailed, LowStockDetected, DeductionsReversed

### Service Implementation (IngredientDeductionService.cs)
Full implementation (~550 lines) including:
- Batch deduction for entire receipts
- Per-item deduction with recipe lookup
- Quantity multiplication based on yield quantity
- Waste percentage handling
- Sub-recipe ingredient deduction (recursive)
- Configurable negative stock handling (block or allow with warning)
- Full audit trail with log entries
- Deduction reversal for voided receipts
- Pre-sale validation with stock availability check
- Low stock detection and event notification
- Summary reporting by ingredient and recipe
- Event-driven architecture for integration

### Unit Tests (IngredientDeductionServiceTests.cs)
Comprehensive test coverage (35+ tests) including:
- Constructor validation tests
- Batch deduction tests with/without recipes
- Single item deduction with quantity multiplication
- Waste percentage calculation tests
- Insufficient stock handling tests
- Forced deduction tests
- Reversal tests
- Validation tests
- Summary report tests
- Configuration tests
- Event handling tests
