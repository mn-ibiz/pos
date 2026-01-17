# Story 26.1: Recipe Definition

## Story
**As a** kitchen manager,
**I want to** define recipes for menu items,
**So that** ingredient usage is tracked accurately.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Epic
**Epic 26: Recipe & Ingredient Management**

## Acceptance Criteria

### AC1: Recipe-Product Linking
**Given** a menu product exists
**When** creating a recipe
**Then** can link product to recipe with yield/portions

### AC2: Ingredient Addition
**Given** creating recipe details
**When** adding ingredients
**Then** can specify: raw ingredient, quantity, unit of measure

### AC3: Recipe Validation
**Given** recipe is defined
**When** saving
**Then** recipe is validated (ingredients exist, quantities positive)

## Technical Notes
```csharp
public class Recipe
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }  // The menu item this recipe makes
    public Product Product { get; set; }
    public string Name { get; set; }
    public int YieldQuantity { get; set; } = 1;  // How many portions
    public string YieldUnit { get; set; } = "portion";
    public decimal EstimatedCost { get; set; }
    public bool IsActive { get; set; } = true;
    public List<RecipeIngredient> Ingredients { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class RecipeIngredient
{
    public Guid Id { get; set; }
    public Guid RecipeId { get; set; }
    public Guid IngredientProductId { get; set; }  // Raw ingredient
    public Product IngredientProduct { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; }  // g, ml, piece, etc.
    public decimal? WastePercent { get; set; }  // For prep loss
}

public interface IRecipeService
{
    Task<Recipe> CreateRecipeAsync(RecipeDto recipe);
    Task<Recipe> UpdateRecipeAsync(Guid id, RecipeDto recipe);
    Task AddIngredientAsync(Guid recipeId, RecipeIngredientDto ingredient);
    Task RemoveIngredientAsync(Guid recipeId, Guid ingredientId);
    Task<Recipe> GetRecipeByProductAsync(Guid productId);
    Task<bool> ValidateRecipeAsync(Guid recipeId);
}
```

## Definition of Done
- [x] Recipe entity and database table
- [x] RecipeIngredient entity and table
- [x] Recipe CRUD operations
- [x] Recipe-Product relationship
- [x] Ingredient quantity management
- [x] Recipe validation logic
- [x] Unit tests passing

## Implementation Summary

### Entities Created (RecipeEntities.cs)
- `RecipeUnitOfMeasure` enum - Gram, Kilogram, Milliliter, Liter, Piece, Teaspoon, Tablespoon, Cup, Ounce, Pound, Pinch, Dash, Slice, Portion
- `RecipeType` enum - Standard, SubRecipe, BatchPrep, Composite
- `Recipe` - Main recipe entity with ProductId link, yield quantity/unit, prep/cook times, version, approval tracking
- `RecipeIngredient` - Ingredient with quantity, unit, waste percent, calculated cost, prep notes
- `RecipeSubRecipe` - For composite recipes using other recipes as components
- `RecipeCostHistory` - Historical cost tracking with snapshots
- `UnitConversion` - Unit conversion factors (global and product-specific)

### DTOs Created (RecipeDtos.cs)
- `RecipeDto`, `CreateRecipeDto`, `UpdateRecipeDto` - Recipe CRUD
- `RecipeIngredientDto`, `CreateRecipeIngredientDto`, `UpdateRecipeIngredientDto` - Ingredient management
- `RecipeSubRecipeDto`, `CreateRecipeSubRecipeDto` - Sub-recipe support
- `RecipeListDto` - List display with food cost percentage
- `RecipeCostDto`, `IngredientCostDto`, `SubRecipeCostDto` - Cost calculation results
- `RecipeQueryDto` - Filtering and pagination
- `RecipeValidationResultDto` - Validation with errors/warnings
- `ApproveRecipeDto` - Recipe approval workflow
- `RecipeCostHistoryDto` - Cost history tracking
- `UnitConversionDto`, `CreateUnitConversionDto` - Unit conversions
- `RecipeSummaryDto` - Dashboard summary

### Interface Created (IRecipeService.cs)
Comprehensive interface with:
- **Recipe CRUD**: Create, Get, Update, Delete, Query, GetAll
- **Ingredient Management**: Add, Update, Remove, Get, Reorder
- **Sub-Recipe Management**: Add, Remove, Get, GetRecipesUsing
- **Validation**: ValidateRecipe, ValidateCreate, HasCircularDependency
- **Approval**: ApproveRecipe, GetPendingApproval
- **Costing**: CalculateCost, RecalculateAll, GetCostHistory
- **Unit Conversions**: GetConversions, CreateConversion, ConvertUnits
- **Summary**: GetRecipeSummary, GetProductsWithoutRecipes
- **Cloning**: CloneRecipe

### Service Implementation (RecipeService.cs)
Full implementation (~700 lines) including:
- Recipe CRUD with validation and version tracking
- Ingredient management with duplicate detection
- Sub-recipe support with circular dependency detection (DFS algorithm)
- Cost calculation with waste percentage and unit conversions
- Standard unit conversions (metric weight/volume, imperial)
- Product-specific conversion overrides
- Recipe approval workflow
- Recipe cloning with ingredients and sub-recipes
- Dashboard summary with statistics

### Unit Tests (RecipeServiceTests.cs)
Comprehensive test coverage (50+ tests) including:
- Constructor validation tests
- Recipe CRUD tests (create, read, update, delete)
- Ingredient management tests
- Sub-recipe and circular dependency tests
- Validation tests
- Cost calculation tests with waste percentage
- Approval workflow tests
- Cloning tests
- Unit conversion tests
- DTO calculation tests
