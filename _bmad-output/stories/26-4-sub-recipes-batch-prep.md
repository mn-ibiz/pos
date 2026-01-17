# Story 26.4: Sub-Recipes and Batch Prep

## Story
**As a** kitchen manager,
**I want to** create sub-recipes and record batch preparations,
**So that** prep work is tracked against inventory.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Epic
**Epic 26: Recipe & Ingredient Management**

## Acceptance Criteria

### AC1: Sub-Recipe Creation
**Given** common components (sauces, stocks)
**When** creating sub-recipe
**Then** can use sub-recipe as ingredient in other recipes

### AC2: Batch Prep Recording
**Given** batch prep is done
**When** recording prep
**Then** deducts raw ingredients, adds prepped item to inventory

### AC3: Usage Reporting
**Given** ingredient usage tracking
**When** running reports
**Then** shows ingredient usage by recipe, by period

## Technical Notes
```csharp
public class Recipe
{
    // ... existing properties
    public bool IsSubRecipe { get; set; }
    public Guid? ParentRecipeId { get; set; }  // For sub-recipes
    public Recipe ParentRecipe { get; set; }
}

public class BatchPrep
{
    public Guid Id { get; set; }
    public Guid RecipeId { get; set; }
    public Recipe Recipe { get; set; }
    public decimal BatchSize { get; set; }  // Multiplier of recipe yield
    public decimal ActualYield { get; set; }
    public Guid PreparedByUserId { get; set; }
    public DateTime PreparedAt { get; set; }
    public string Notes { get; set; }
    public BatchPrepStatus Status { get; set; }
}

public enum BatchPrepStatus
{
    Completed,
    Partial,
    Wasted
}

public interface IBatchPrepService
{
    Task<BatchPrep> RecordBatchPrepAsync(BatchPrepDto prep);
    Task<List<BatchPrep>> GetBatchPrepHistoryAsync(Guid recipeId, DateTime from, DateTime to);
    Task<IngredientUsageReport> GetIngredientUsageReportAsync(DateTime from, DateTime to);
}

public class IngredientUsageReport
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<IngredientUsageLine> Lines { get; set; }
}

public class IngredientUsageLine
{
    public Guid IngredientId { get; set; }
    public string IngredientName { get; set; }
    public decimal TotalUsed { get; set; }
    public decimal TotalCost { get; set; }
    public Dictionary<string, decimal> UsageByRecipe { get; set; }
}
```

## Definition of Done
- [x] Sub-recipe flag on Recipe entity
- [x] Sub-recipe nesting (recipe uses sub-recipe)
- [x] BatchPrep entity and table
- [x] Batch prep recording workflow
- [x] Ingredient deduction on batch prep
- [x] Inventory increase for prepped items
- [x] Ingredient usage report
- [x] Unit tests passing

## Implementation Summary

### Entities Added (RecipeEntities.cs)
- `BatchPrepStatus` enum - Planned, InProgress, Completed, Partial, Cancelled, Wasted
- `BatchPrep` - Batch preparation record with recipe link, batch size, expected/actual yield, status, timestamps
- `BatchPrepIngredient` - Ingredient deduction record for batch prep with expected/actual quantities, costs
- `IngredientUsageSummary` - Summary entity for reporting usage by ingredient/period/store

### DTOs Added (RecipeDtos.cs)
- `BatchPrepDto` - Full batch prep display with ingredients list
- `BatchPrepStatusDto` - Status enum for DTOs
- `CreateBatchPrepDto` - Create batch prep request
- `StartBatchPrepDto` - Start batch prep with optional ingredient deduction
- `CompleteBatchPrepDto` - Complete with actual yield and inventory addition
- `CancelBatchPrepDto` - Cancel with optional deduction reversal
- `UpdateBatchPrepDto` - Update planned batch prep
- `BatchPrepIngredientDto` - Ingredient deduction display
- `BatchPrepQueryDto` - Query parameters for batch preps
- `BatchPrepListDto` - List display for batch preps
- `BatchPrepSummaryDto` - Summary for dashboard
- `RecipePrepSummaryDto` - Summary by recipe
- `BatchPrepStartResultDto` - Start result with deductions
- `BatchPrepCompleteResultDto` - Complete result with inventory

### Ingredient Usage Report DTOs
- `IngredientUsageQueryDto` - Query parameters for usage report
- `IngredientUsageReportDto` - Full usage report with breakdowns
- `IngredientUsageLineDto` - Single ingredient usage with recipe breakdown
- `RecipeIngredientUsageDto` - Usage by recipe
- `RecipeUsageSummaryDto` - Recipe-level usage summary
- `IngredientUsageByRecipeDto` - Ingredient usage within a recipe
- `DailyUsageSummaryDto` - Daily usage summary
- `TopIngredientUsageDto` - Top ingredients ranking
- `IngredientUsageTrendDto` - Usage trend over time
- `UsageTrendPointDto` - Single trend point
- `UsageComparisonDto` - Period comparison
- `IngredientUsageChangeDto` - Change between periods

### Interface Created (IBatchPrepService.cs)
Comprehensive interface with:
- **CRUD Operations**: CreateBatchPrep, GetBatchPrep, GetBatchPreps, UpdateBatchPrep, DeleteBatchPrep
- **Workflow**: StartBatchPrep, CompleteBatchPrep, CancelBatchPrep, RecordWaste
- **Queries**: GetBatchPrepHistory, GetPendingBatchPreps, GetBatchPrepSummary, GetRequiredIngredients, ValidateBatchPrep
- **Usage Reporting**: GetIngredientUsageReport, GetTopIngredients, GetIngredientUsageTrend, CompareUsagePeriods, GetUsageByRecipe, GetDailyUsage
- **Events**: BatchPrepStarted, BatchPrepCompleted, BatchPrepCancelled, LowStockDetected

### Service Implementation (BatchPrepService.cs)
Full implementation (~750 lines) including:
- Batch prep CRUD with recipe-based yield calculation
- Start workflow with ingredient deduction via IInventoryService
- Complete workflow with inventory addition for prepped items
- Cancel with optional deduction reversal
- Waste recording and status updates
- Validation with ingredient availability checking
- Low stock detection with event notification
- Comprehensive ingredient usage reporting
- Usage comparison between periods
- Daily usage summaries
- Top ingredient rankings
- Usage trends over time
- Event-driven architecture for integration

### Unit Tests (BatchPrepServiceTests.cs)
Comprehensive test coverage (40+ tests) including:
- Constructor validation tests
- Batch prep creation tests
- Start workflow tests with deduction
- Complete workflow tests with inventory
- Cancel and reversal tests
- Query and filter tests
- Validation tests
- Usage report tests
- Event handling tests
- Update and delete tests
- Waste recording tests
