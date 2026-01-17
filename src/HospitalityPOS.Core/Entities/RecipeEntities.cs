namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Unit of measure for recipe ingredients.
/// </summary>
public enum RecipeUnitOfMeasure
{
    /// <summary>Grams.</summary>
    Gram = 1,
    /// <summary>Kilograms.</summary>
    Kilogram = 2,
    /// <summary>Milliliters.</summary>
    Milliliter = 3,
    /// <summary>Liters.</summary>
    Liter = 4,
    /// <summary>Individual pieces/units.</summary>
    Piece = 5,
    /// <summary>Teaspoon.</summary>
    Teaspoon = 6,
    /// <summary>Tablespoon.</summary>
    Tablespoon = 7,
    /// <summary>Cup.</summary>
    Cup = 8,
    /// <summary>Ounce.</summary>
    Ounce = 9,
    /// <summary>Pound.</summary>
    Pound = 10,
    /// <summary>Pinch (for seasonings).</summary>
    Pinch = 11,
    /// <summary>Dash (for liquids).</summary>
    Dash = 12,
    /// <summary>Slice.</summary>
    Slice = 13,
    /// <summary>Portion.</summary>
    Portion = 14
}

/// <summary>
/// Type of recipe.
/// </summary>
public enum RecipeType
{
    /// <summary>Standard single-serving recipe.</summary>
    Standard = 1,
    /// <summary>Sub-recipe/base preparation used in other recipes.</summary>
    SubRecipe = 2,
    /// <summary>Batch preparation with multiple servings.</summary>
    BatchPrep = 3,
    /// <summary>Composite recipe that combines finished products.</summary>
    Composite = 4
}

/// <summary>
/// Represents a recipe for a menu item.
/// </summary>
public class Recipe : BaseEntity
{
    /// <summary>
    /// The menu product this recipe produces.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Recipe name (may differ from product name).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Detailed instructions for preparation.
    /// </summary>
    public string? Instructions { get; set; }

    /// <summary>
    /// Type of recipe (Standard, SubRecipe, BatchPrep).
    /// </summary>
    public RecipeType RecipeType { get; set; } = RecipeType.Standard;

    /// <summary>
    /// Number of portions/servings this recipe yields.
    /// </summary>
    public decimal YieldQuantity { get; set; } = 1;

    /// <summary>
    /// Unit for yield (e.g., "portion", "serving", "kg").
    /// </summary>
    public string YieldUnit { get; set; } = "portion";

    /// <summary>
    /// Estimated preparation time in minutes.
    /// </summary>
    public int? PrepTimeMinutes { get; set; }

    /// <summary>
    /// Estimated cooking time in minutes.
    /// </summary>
    public int? CookTimeMinutes { get; set; }

    /// <summary>
    /// Total time in minutes.
    /// </summary>
    public int? TotalTimeMinutes => (PrepTimeMinutes ?? 0) + (CookTimeMinutes ?? 0);

    /// <summary>
    /// Calculated cost per portion based on ingredient costs.
    /// </summary>
    public decimal EstimatedCostPerPortion { get; set; }

    /// <summary>
    /// Total recipe cost (all portions).
    /// </summary>
    public decimal TotalEstimatedCost { get; set; }

    /// <summary>
    /// Last time cost was calculated.
    /// </summary>
    public DateTime? LastCostCalculation { get; set; }

    /// <summary>
    /// Version number for recipe updates.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Notes or additional information.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Whether this recipe is approved for use.
    /// </summary>
    public bool IsApproved { get; set; } = true;

    /// <summary>
    /// User who approved this recipe.
    /// </summary>
    public int? ApprovedByUserId { get; set; }

    /// <summary>
    /// When the recipe was approved.
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    // Navigation Properties

    /// <summary>
    /// The product this recipe produces.
    /// </summary>
    public virtual Product? Product { get; set; }

    /// <summary>
    /// The user who approved this recipe.
    /// </summary>
    public virtual User? ApprovedByUser { get; set; }

    /// <summary>
    /// Ingredients used in this recipe.
    /// </summary>
    public virtual ICollection<RecipeIngredient> Ingredients { get; set; } = new List<RecipeIngredient>();

    /// <summary>
    /// Sub-recipes used in this recipe.
    /// </summary>
    public virtual ICollection<RecipeSubRecipe> SubRecipes { get; set; } = new List<RecipeSubRecipe>();

    /// <summary>
    /// Recipes that use this recipe as a sub-recipe.
    /// </summary>
    public virtual ICollection<RecipeSubRecipe> UsedInRecipes { get; set; } = new List<RecipeSubRecipe>();
}

/// <summary>
/// Represents an ingredient in a recipe.
/// </summary>
public class RecipeIngredient : BaseEntity
{
    /// <summary>
    /// The parent recipe.
    /// </summary>
    public int RecipeId { get; set; }

    /// <summary>
    /// The raw ingredient (product with inventory tracking).
    /// </summary>
    public int IngredientProductId { get; set; }

    /// <summary>
    /// Quantity of ingredient required.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit of measure for this ingredient.
    /// </summary>
    public RecipeUnitOfMeasure Unit { get; set; }

    /// <summary>
    /// Custom unit name if Unit doesn't cover it.
    /// </summary>
    public string? CustomUnit { get; set; }

    /// <summary>
    /// Percentage of waste/loss during preparation (e.g., 10% for peeling).
    /// </summary>
    public decimal WastePercent { get; set; } = 0;

    /// <summary>
    /// Whether this ingredient is optional.
    /// </summary>
    public bool IsOptional { get; set; }

    /// <summary>
    /// Display order in the ingredient list.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Notes about preparation for this ingredient.
    /// </summary>
    public string? PrepNotes { get; set; }

    /// <summary>
    /// Calculated cost for this ingredient line.
    /// </summary>
    public decimal CalculatedCost { get; set; }

    /// <summary>
    /// Effective quantity after waste (Quantity * (1 + WastePercent/100)).
    /// </summary>
    public decimal EffectiveQuantity => Quantity * (1 + WastePercent / 100);

    /// <summary>
    /// Gets the unit display name.
    /// </summary>
    public string UnitDisplayName => !string.IsNullOrEmpty(CustomUnit)
        ? CustomUnit
        : Unit.ToString().ToLower();

    // Navigation Properties

    /// <summary>
    /// The parent recipe.
    /// </summary>
    public virtual Recipe? Recipe { get; set; }

    /// <summary>
    /// The ingredient product.
    /// </summary>
    public virtual Product? IngredientProduct { get; set; }
}

/// <summary>
/// Represents a sub-recipe used within another recipe.
/// </summary>
public class RecipeSubRecipe : BaseEntity
{
    /// <summary>
    /// The parent recipe using the sub-recipe.
    /// </summary>
    public int ParentRecipeId { get; set; }

    /// <summary>
    /// The sub-recipe being used.
    /// </summary>
    public int SubRecipeId { get; set; }

    /// <summary>
    /// Quantity of sub-recipe portions required.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Display order.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Notes about using this sub-recipe.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Calculated cost for this sub-recipe usage.
    /// </summary>
    public decimal CalculatedCost { get; set; }

    // Navigation Properties

    /// <summary>
    /// The parent recipe.
    /// </summary>
    public virtual Recipe? ParentRecipe { get; set; }

    /// <summary>
    /// The sub-recipe.
    /// </summary>
    public virtual Recipe? SubRecipe { get; set; }
}

/// <summary>
/// Historical record of recipe cost calculations.
/// </summary>
public class RecipeCostHistory : BaseEntity
{
    /// <summary>
    /// The recipe.
    /// </summary>
    public int RecipeId { get; set; }

    /// <summary>
    /// Cost per portion at this calculation.
    /// </summary>
    public decimal CostPerPortion { get; set; }

    /// <summary>
    /// Total cost at this calculation.
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// JSON snapshot of ingredient costs used.
    /// </summary>
    public string? IngredientCostSnapshot { get; set; }

    /// <summary>
    /// Reason for recalculation.
    /// </summary>
    public string? CalculationReason { get; set; }

    // Navigation

    /// <summary>
    /// The recipe.
    /// </summary>
    public virtual Recipe? Recipe { get; set; }
}

/// <summary>
/// Unit conversion for recipe ingredients.
/// </summary>
public class UnitConversion : BaseEntity
{
    /// <summary>
    /// Source unit.
    /// </summary>
    public RecipeUnitOfMeasure FromUnit { get; set; }

    /// <summary>
    /// Target unit.
    /// </summary>
    public RecipeUnitOfMeasure ToUnit { get; set; }

    /// <summary>
    /// Product-specific conversion (null for global).
    /// </summary>
    public int? ProductId { get; set; }

    /// <summary>
    /// Conversion factor (ToUnit = FromUnit * Factor).
    /// </summary>
    public decimal ConversionFactor { get; set; }

    // Navigation

    /// <summary>
    /// The product for product-specific conversions.
    /// </summary>
    public virtual Product? Product { get; set; }
}

/// <summary>
/// Status of ingredient deduction.
/// </summary>
public enum DeductionStatus
{
    /// <summary>Deduction completed successfully.</summary>
    Success = 1,
    /// <summary>Deduction completed with warning (e.g., negative stock).</summary>
    Warning = 2,
    /// <summary>Deduction failed.</summary>
    Failed = 3,
    /// <summary>Deduction was skipped (no recipe).</summary>
    Skipped = 4,
    /// <summary>Deduction was reversed.</summary>
    Reversed = 5
}

/// <summary>
/// Log entry for ingredient deductions from sales.
/// </summary>
public class IngredientDeductionLog : BaseEntity
{
    /// <summary>
    /// The receipt that triggered this deduction.
    /// </summary>
    public int ReceiptId { get; set; }

    /// <summary>
    /// The receipt line item.
    /// </summary>
    public int? ReceiptLineId { get; set; }

    /// <summary>
    /// The recipe used for deduction.
    /// </summary>
    public int RecipeId { get; set; }

    /// <summary>
    /// The ingredient product being deducted.
    /// </summary>
    public int IngredientProductId { get; set; }

    /// <summary>
    /// Number of portions sold.
    /// </summary>
    public decimal PortionsSold { get; set; }

    /// <summary>
    /// Quantity deducted from inventory.
    /// </summary>
    public decimal QuantityDeducted { get; set; }

    /// <summary>
    /// Unit of measure for the deduction.
    /// </summary>
    public RecipeUnitOfMeasure Unit { get; set; }

    /// <summary>
    /// Stock level before deduction.
    /// </summary>
    public decimal StockBefore { get; set; }

    /// <summary>
    /// Stock level after deduction.
    /// </summary>
    public decimal StockAfter { get; set; }

    /// <summary>
    /// Status of the deduction.
    /// </summary>
    public DeductionStatus Status { get; set; }

    /// <summary>
    /// Error message if deduction failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Whether this was a forced deduction (allowing negative stock).
    /// </summary>
    public bool WasForced { get; set; }

    /// <summary>
    /// When the deduction occurred.
    /// </summary>
    public DateTime DeductedAt { get; set; }

    /// <summary>
    /// Reference to reversal if this deduction was reversed.
    /// </summary>
    public int? ReversedByLogId { get; set; }

    /// <summary>
    /// When the deduction was reversed.
    /// </summary>
    public DateTime? ReversedAt { get; set; }

    /// <summary>
    /// Store where the deduction occurred.
    /// </summary>
    public int? StoreId { get; set; }

    // Navigation Properties

    /// <summary>
    /// The recipe.
    /// </summary>
    public virtual Recipe? Recipe { get; set; }

    /// <summary>
    /// The ingredient product.
    /// </summary>
    public virtual Product? IngredientProduct { get; set; }
}

/// <summary>
/// Batch deduction record for a single receipt.
/// </summary>
public class ReceiptDeductionBatch : BaseEntity
{
    /// <summary>
    /// The receipt being processed.
    /// </summary>
    public int ReceiptId { get; set; }

    /// <summary>
    /// Store where deduction occurred.
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// When processing started.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// When processing completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Total items processed.
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Items with recipes.
    /// </summary>
    public int ItemsWithRecipes { get; set; }

    /// <summary>
    /// Total ingredients deducted.
    /// </summary>
    public int TotalIngredientsDeducted { get; set; }

    /// <summary>
    /// Deductions that succeeded.
    /// </summary>
    public int SuccessfulDeductions { get; set; }

    /// <summary>
    /// Deductions that failed.
    /// </summary>
    public int FailedDeductions { get; set; }

    /// <summary>
    /// Deductions with warnings.
    /// </summary>
    public int WarningDeductions { get; set; }

    /// <summary>
    /// Whether batch processing completed successfully.
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// Summary of any errors.
    /// </summary>
    public string? ErrorSummary { get; set; }
}

/// <summary>
/// Status of batch preparation.
/// </summary>
public enum BatchPrepStatus
{
    /// <summary>Planned but not started.</summary>
    Planned = 1,
    /// <summary>Currently in progress.</summary>
    InProgress = 2,
    /// <summary>Completed successfully.</summary>
    Completed = 3,
    /// <summary>Partially completed.</summary>
    Partial = 4,
    /// <summary>Cancelled.</summary>
    Cancelled = 5,
    /// <summary>Failed or wasted.</summary>
    Wasted = 6
}

/// <summary>
/// Represents a batch preparation of a recipe.
/// </summary>
public class BatchPrep : BaseEntity
{
    /// <summary>
    /// The recipe being prepared.
    /// </summary>
    public int RecipeId { get; set; }

    /// <summary>
    /// Store where prep was done.
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// Batch size as multiplier of recipe yield.
    /// </summary>
    public decimal BatchSize { get; set; } = 1;

    /// <summary>
    /// Expected yield based on recipe.
    /// </summary>
    public decimal ExpectedYield { get; set; }

    /// <summary>
    /// Actual yield achieved.
    /// </summary>
    public decimal ActualYield { get; set; }

    /// <summary>
    /// Unit for yield.
    /// </summary>
    public string YieldUnit { get; set; } = "portion";

    /// <summary>
    /// Variance between expected and actual yield.
    /// </summary>
    public decimal YieldVariance => ActualYield - ExpectedYield;

    /// <summary>
    /// Yield variance as percentage.
    /// </summary>
    public decimal YieldVariancePercent => ExpectedYield > 0
        ? (YieldVariance / ExpectedYield) * 100
        : 0;

    /// <summary>
    /// When prep was planned.
    /// </summary>
    public DateTime? PlannedAt { get; set; }

    /// <summary>
    /// When prep was started.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When prep was completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// User who performed the prep.
    /// </summary>
    public int? PreparedByUserId { get; set; }

    /// <summary>
    /// Status of the batch prep.
    /// </summary>
    public BatchPrepStatus Status { get; set; } = BatchPrepStatus.Planned;

    /// <summary>
    /// Notes about the prep.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Calculated cost of ingredients used.
    /// </summary>
    public decimal IngredientCost { get; set; }

    /// <summary>
    /// Cost per unit of yield.
    /// </summary>
    public decimal CostPerUnit { get; set; }

    /// <summary>
    /// Whether ingredients have been deducted.
    /// </summary>
    public bool IngredientsDeducted { get; set; }

    /// <summary>
    /// Whether prepped item was added to inventory.
    /// </summary>
    public bool AddedToInventory { get; set; }

    /// <summary>
    /// Reference for inventory addition.
    /// </summary>
    public int? InventoryTransactionId { get; set; }

    // Navigation Properties

    /// <summary>
    /// The recipe.
    /// </summary>
    public virtual Recipe? Recipe { get; set; }

    /// <summary>
    /// The user who performed prep.
    /// </summary>
    public virtual User? PreparedByUser { get; set; }

    /// <summary>
    /// Ingredient deductions for this batch.
    /// </summary>
    public virtual ICollection<BatchPrepIngredient> Ingredients { get; set; } = new List<BatchPrepIngredient>();
}

/// <summary>
/// Ingredient used in a batch preparation.
/// </summary>
public class BatchPrepIngredient : BaseEntity
{
    /// <summary>
    /// The batch prep.
    /// </summary>
    public int BatchPrepId { get; set; }

    /// <summary>
    /// The ingredient product.
    /// </summary>
    public int IngredientProductId { get; set; }

    /// <summary>
    /// Expected quantity based on recipe.
    /// </summary>
    public decimal ExpectedQuantity { get; set; }

    /// <summary>
    /// Actual quantity used.
    /// </summary>
    public decimal ActualQuantity { get; set; }

    /// <summary>
    /// Variance from expected.
    /// </summary>
    public decimal Variance => ActualQuantity - ExpectedQuantity;

    /// <summary>
    /// Unit of measure.
    /// </summary>
    public RecipeUnitOfMeasure Unit { get; set; }

    /// <summary>
    /// Cost at time of prep.
    /// </summary>
    public decimal UnitCost { get; set; }

    /// <summary>
    /// Total cost for this ingredient.
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Stock before deduction.
    /// </summary>
    public decimal StockBefore { get; set; }

    /// <summary>
    /// Stock after deduction.
    /// </summary>
    public decimal StockAfter { get; set; }

    /// <summary>
    /// Whether deduction was successful.
    /// </summary>
    public bool DeductionSuccessful { get; set; }

    /// <summary>
    /// Error if deduction failed.
    /// </summary>
    public string? DeductionError { get; set; }

    // Navigation

    /// <summary>
    /// The batch prep.
    /// </summary>
    public virtual BatchPrep? BatchPrep { get; set; }

    /// <summary>
    /// The ingredient product.
    /// </summary>
    public virtual Product? IngredientProduct { get; set; }
}

/// <summary>
/// Ingredient usage summary for reporting.
/// </summary>
public class IngredientUsageSummary : BaseEntity
{
    /// <summary>
    /// The ingredient product.
    /// </summary>
    public int IngredientProductId { get; set; }

    /// <summary>
    /// Summary period start.
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// Summary period end.
    /// </summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>
    /// Store filter (null for all stores).
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// Total quantity used in sales.
    /// </summary>
    public decimal SalesUsage { get; set; }

    /// <summary>
    /// Total quantity used in batch prep.
    /// </summary>
    public decimal PrepUsage { get; set; }

    /// <summary>
    /// Total quantity wasted.
    /// </summary>
    public decimal WasteAmount { get; set; }

    /// <summary>
    /// Total quantity used overall.
    /// </summary>
    public decimal TotalUsage => SalesUsage + PrepUsage + WasteAmount;

    /// <summary>
    /// Total cost of usage.
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Number of recipes using this ingredient.
    /// </summary>
    public int RecipeCount { get; set; }

    // Navigation

    /// <summary>
    /// The ingredient product.
    /// </summary>
    public virtual Product? IngredientProduct { get; set; }
}
