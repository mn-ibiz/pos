using HospitalityPOS.Core.DTOs;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for batch preparation management and ingredient usage tracking.
/// </summary>
public interface IBatchPrepService
{
    #region Batch Prep CRUD Operations

    /// <summary>
    /// Creates a new batch preparation record.
    /// </summary>
    /// <param name="dto">Batch prep creation data.</param>
    /// <returns>The created batch prep.</returns>
    Task<BatchPrepDto> CreateBatchPrepAsync(CreateBatchPrepDto dto);

    /// <summary>
    /// Gets a batch prep by ID.
    /// </summary>
    /// <param name="id">The batch prep ID.</param>
    /// <returns>The batch prep or null if not found.</returns>
    Task<BatchPrepDto?> GetBatchPrepAsync(int id);

    /// <summary>
    /// Gets batch preps based on query parameters.
    /// </summary>
    /// <param name="query">Query parameters.</param>
    /// <returns>List of batch preps.</returns>
    Task<List<BatchPrepListDto>> GetBatchPrepsAsync(BatchPrepQueryDto query);

    /// <summary>
    /// Updates a planned batch prep.
    /// </summary>
    /// <param name="id">The batch prep ID.</param>
    /// <param name="dto">Update data.</param>
    /// <returns>The updated batch prep.</returns>
    Task<BatchPrepDto> UpdateBatchPrepAsync(int id, UpdateBatchPrepDto dto);

    /// <summary>
    /// Deletes a planned batch prep (not started).
    /// </summary>
    /// <param name="id">The batch prep ID.</param>
    /// <returns>True if deleted.</returns>
    Task<bool> DeleteBatchPrepAsync(int id);

    #endregion

    #region Batch Prep Workflow

    /// <summary>
    /// Starts a batch prep, optionally deducting ingredients.
    /// </summary>
    /// <param name="dto">Start request.</param>
    /// <returns>Start result with deduction details.</returns>
    Task<BatchPrepStartResultDto> StartBatchPrepAsync(StartBatchPrepDto dto);

    /// <summary>
    /// Completes a batch prep, recording actual yield.
    /// </summary>
    /// <param name="dto">Complete request.</param>
    /// <returns>Complete result with inventory details.</returns>
    Task<BatchPrepCompleteResultDto> CompleteBatchPrepAsync(CompleteBatchPrepDto dto);

    /// <summary>
    /// Cancels a batch prep.
    /// </summary>
    /// <param name="dto">Cancel request.</param>
    /// <returns>Cancel result.</returns>
    Task<BatchPrepDto> CancelBatchPrepAsync(CancelBatchPrepDto dto);

    /// <summary>
    /// Records waste for a batch prep.
    /// </summary>
    /// <param name="id">The batch prep ID.</param>
    /// <param name="wastedQuantity">Amount wasted.</param>
    /// <param name="reason">Waste reason.</param>
    /// <returns>Updated batch prep.</returns>
    Task<BatchPrepDto> RecordWasteAsync(int id, decimal wastedQuantity, string reason);

    #endregion

    #region Batch Prep Queries

    /// <summary>
    /// Gets batch prep history for a recipe.
    /// </summary>
    /// <param name="recipeId">The recipe ID.</param>
    /// <param name="fromDate">Start date.</param>
    /// <param name="toDate">End date.</param>
    /// <returns>List of batch preps.</returns>
    Task<List<BatchPrepListDto>> GetBatchPrepHistoryAsync(int recipeId, DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Gets pending batch preps (planned or in progress).
    /// </summary>
    /// <param name="storeId">Optional store filter.</param>
    /// <returns>List of pending batch preps.</returns>
    Task<List<BatchPrepListDto>> GetPendingBatchPrepsAsync(int? storeId = null);

    /// <summary>
    /// Gets batch prep summary for dashboard.
    /// </summary>
    /// <param name="fromDate">Start date.</param>
    /// <param name="toDate">End date.</param>
    /// <param name="storeId">Optional store filter.</param>
    /// <returns>Batch prep summary.</returns>
    Task<BatchPrepSummaryDto> GetBatchPrepSummaryAsync(DateTime fromDate, DateTime toDate, int? storeId = null);

    /// <summary>
    /// Gets ingredients required for a batch prep.
    /// </summary>
    /// <param name="recipeId">The recipe ID.</param>
    /// <param name="batchSize">Batch multiplier.</param>
    /// <returns>List of ingredients with quantities.</returns>
    Task<List<BatchPrepIngredientDto>> GetRequiredIngredientsAsync(int recipeId, decimal batchSize);

    /// <summary>
    /// Validates if a batch prep can be started (checks ingredient availability).
    /// </summary>
    /// <param name="recipeId">The recipe ID.</param>
    /// <param name="batchSize">Batch multiplier.</param>
    /// <param name="storeId">Optional store filter.</param>
    /// <returns>Validation result with warnings.</returns>
    Task<BatchPrepValidationResultDto> ValidateBatchPrepAsync(int recipeId, decimal batchSize, int? storeId = null);

    #endregion

    #region Ingredient Usage Reporting

    /// <summary>
    /// Gets ingredient usage report.
    /// </summary>
    /// <param name="query">Query parameters.</param>
    /// <returns>Ingredient usage report.</returns>
    Task<IngredientUsageReportDto> GetIngredientUsageReportAsync(IngredientUsageQueryDto query);

    /// <summary>
    /// Gets top ingredients by usage.
    /// </summary>
    /// <param name="fromDate">Start date.</param>
    /// <param name="toDate">End date.</param>
    /// <param name="top">Number of top ingredients.</param>
    /// <param name="storeId">Optional store filter.</param>
    /// <returns>Top ingredients list.</returns>
    Task<List<TopIngredientUsageDto>> GetTopIngredientsAsync(DateTime fromDate, DateTime toDate, int top = 10, int? storeId = null);

    /// <summary>
    /// Gets usage trend for an ingredient.
    /// </summary>
    /// <param name="ingredientProductId">The ingredient product ID.</param>
    /// <param name="fromDate">Start date.</param>
    /// <param name="toDate">End date.</param>
    /// <param name="storeId">Optional store filter.</param>
    /// <returns>Usage trend data.</returns>
    Task<IngredientUsageTrendDto> GetIngredientUsageTrendAsync(int ingredientProductId, DateTime fromDate, DateTime toDate, int? storeId = null);

    /// <summary>
    /// Compares usage between two periods.
    /// </summary>
    /// <param name="currentStart">Current period start.</param>
    /// <param name="currentEnd">Current period end.</param>
    /// <param name="previousStart">Previous period start.</param>
    /// <param name="previousEnd">Previous period end.</param>
    /// <param name="storeId">Optional store filter.</param>
    /// <returns>Usage comparison.</returns>
    Task<UsageComparisonDto> CompareUsagePeriodsAsync(
        DateTime currentStart, DateTime currentEnd,
        DateTime previousStart, DateTime previousEnd,
        int? storeId = null);

    /// <summary>
    /// Gets usage by recipe for a date range.
    /// </summary>
    /// <param name="fromDate">Start date.</param>
    /// <param name="toDate">End date.</param>
    /// <param name="storeId">Optional store filter.</param>
    /// <returns>Usage by recipe.</returns>
    Task<List<RecipeUsageSummaryDto>> GetUsageByRecipeAsync(DateTime fromDate, DateTime toDate, int? storeId = null);

    /// <summary>
    /// Gets daily usage summary.
    /// </summary>
    /// <param name="fromDate">Start date.</param>
    /// <param name="toDate">End date.</param>
    /// <param name="storeId">Optional store filter.</param>
    /// <returns>Daily usage summaries.</returns>
    Task<List<DailyUsageSummaryDto>> GetDailyUsageAsync(DateTime fromDate, DateTime toDate, int? storeId = null);

    #endregion

    #region Events

    /// <summary>
    /// Event raised when a batch prep is started.
    /// </summary>
    event EventHandler<BatchPrepDto>? BatchPrepStarted;

    /// <summary>
    /// Event raised when a batch prep is completed.
    /// </summary>
    event EventHandler<BatchPrepCompleteResultDto>? BatchPrepCompleted;

    /// <summary>
    /// Event raised when a batch prep is cancelled.
    /// </summary>
    event EventHandler<BatchPrepDto>? BatchPrepCancelled;

    /// <summary>
    /// Event raised when ingredients are running low during batch prep.
    /// </summary>
    event EventHandler<List<DeductionLowStockWarningDto>>? LowStockDetected;

    #endregion
}

/// <summary>
/// Validation result for batch prep.
/// </summary>
public class BatchPrepValidationResultDto
{
    public bool CanStart { get; set; }
    public bool HasRecipe { get; set; }
    public int? RecipeId { get; set; }
    public string? RecipeName { get; set; }
    public decimal ExpectedYield { get; set; }
    public string YieldUnit { get; set; } = string.Empty;
    public decimal EstimatedCost { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<BatchPrepIngredientAvailabilityDto> IngredientAvailability { get; set; } = new();
}

/// <summary>
/// Ingredient availability for batch prep.
/// </summary>
public class BatchPrepIngredientAvailabilityDto
{
    public int IngredientProductId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public decimal RequiredQuantity { get; set; }
    public decimal AvailableStock { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool IsSufficient { get; set; }
    public decimal Shortage { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
}
