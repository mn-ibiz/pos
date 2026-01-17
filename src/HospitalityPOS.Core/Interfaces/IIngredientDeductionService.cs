using HospitalityPOS.Core.DTOs;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for automatic ingredient deduction from sales.
/// </summary>
public interface IIngredientDeductionService
{
    #region Deduction Operations

    /// <summary>
    /// Deducts ingredients for a settled receipt.
    /// </summary>
    /// <param name="request">Deduction request with order items.</param>
    /// <returns>Deduction result.</returns>
    Task<DeductionResultDto> DeductIngredientsAsync(DeductIngredientsRequestDto request);

    /// <summary>
    /// Deducts ingredients for a single order item.
    /// </summary>
    /// <param name="productId">The sold product ID.</param>
    /// <param name="quantity">Quantity sold.</param>
    /// <param name="receiptId">The receipt ID.</param>
    /// <param name="receiptLineId">Optional receipt line ID.</param>
    /// <param name="storeId">Optional store ID.</param>
    /// <param name="allowNegativeStock">Whether to allow negative stock.</param>
    /// <returns>Item deduction result.</returns>
    Task<ItemDeductionResultDto> DeductForItemAsync(
        int productId,
        decimal quantity,
        int receiptId,
        int? receiptLineId = null,
        int? storeId = null,
        bool allowNegativeStock = false);

    /// <summary>
    /// Reverses deductions for a voided receipt.
    /// </summary>
    /// <param name="request">Reverse request.</param>
    /// <returns>Reversal result.</returns>
    Task<ReverseDeductionResultDto> ReverseDeductionsAsync(ReverseDeductionRequestDto request);

    /// <summary>
    /// Reverses a single deduction log entry.
    /// </summary>
    /// <param name="logId">The deduction log ID.</param>
    /// <param name="reason">Reason for reversal.</param>
    /// <returns>Reversal result.</returns>
    Task<IngredientDeductionResultDto> ReverseDeductionAsync(int logId, string reason);

    #endregion

    #region Query Operations

    /// <summary>
    /// Gets deduction logs based on query parameters.
    /// </summary>
    /// <param name="query">Query parameters.</param>
    /// <returns>List of deduction logs.</returns>
    Task<List<IngredientDeductionLogDto>> GetDeductionLogsAsync(DeductionLogQueryDto query);

    /// <summary>
    /// Gets deductions for a specific receipt.
    /// </summary>
    /// <param name="receiptId">The receipt ID.</param>
    /// <returns>List of deduction logs.</returns>
    Task<List<IngredientDeductionLogDto>> GetDeductionsForReceiptAsync(int receiptId);

    /// <summary>
    /// Gets deductions for a specific ingredient.
    /// </summary>
    /// <param name="ingredientProductId">The ingredient product ID.</param>
    /// <param name="fromDate">Start date filter.</param>
    /// <param name="toDate">End date filter.</param>
    /// <returns>List of deduction logs.</returns>
    Task<List<IngredientDeductionLogDto>> GetDeductionsForIngredientAsync(
        int ingredientProductId,
        DateTime? fromDate = null,
        DateTime? toDate = null);

    /// <summary>
    /// Gets a deduction summary for reporting.
    /// </summary>
    /// <param name="fromDate">Start date.</param>
    /// <param name="toDate">End date.</param>
    /// <param name="storeId">Optional store filter.</param>
    /// <returns>Deduction summary.</returns>
    Task<DeductionSummaryDto> GetDeductionSummaryAsync(
        DateTime fromDate,
        DateTime toDate,
        int? storeId = null);

    #endregion

    #region Validation

    /// <summary>
    /// Checks if deductions can be made for an order item.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="quantity">Quantity to sell.</param>
    /// <returns>Validation result with warnings if any.</returns>
    Task<DeductionValidationResultDto> ValidateDeductionAsync(int productId, decimal quantity);

    /// <summary>
    /// Gets low stock warnings for a potential sale.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="quantity">Quantity to sell.</param>
    /// <returns>List of low stock warnings.</returns>
    Task<List<DeductionLowStockWarningDto>> GetLowStockWarningsAsync(int productId, decimal quantity);

    #endregion

    #region Configuration

    /// <summary>
    /// Gets current deduction configuration.
    /// </summary>
    /// <returns>Current configuration.</returns>
    DeductionConfigDto GetConfiguration();

    /// <summary>
    /// Updates deduction configuration.
    /// </summary>
    /// <param name="config">New configuration.</param>
    void UpdateConfiguration(DeductionConfigDto config);

    /// <summary>
    /// Whether ingredient deduction is enabled.
    /// </summary>
    bool IsEnabled { get; }

    #endregion

    #region Events

    /// <summary>
    /// Event raised when ingredients are deducted.
    /// </summary>
    event EventHandler<DeductionResultDto>? IngredientsDeducted;

    /// <summary>
    /// Event raised when a deduction fails.
    /// </summary>
    event EventHandler<IngredientDeductionResultDto>? DeductionFailed;

    /// <summary>
    /// Event raised when low stock is detected during deduction.
    /// </summary>
    event EventHandler<DeductionLowStockWarningDto>? LowStockDetected;

    /// <summary>
    /// Event raised when deductions are reversed.
    /// </summary>
    event EventHandler<ReverseDeductionResultDto>? DeductionsReversed;

    #endregion
}

/// <summary>
/// Validation result for deduction.
/// </summary>
public class DeductionValidationResultDto
{
    public bool CanDeduct { get; set; }
    public bool HasRecipe { get; set; }
    public int? RecipeId { get; set; }
    public string? RecipeName { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<DeductionLowStockWarningDto> Warnings { get; set; } = new();
    public List<IngredientAvailabilityDto> IngredientAvailability { get; set; } = new();
}

/// <summary>
/// Ingredient availability for deduction.
/// </summary>
public class IngredientAvailabilityDto
{
    public int IngredientProductId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public decimal RequiredQuantity { get; set; }
    public decimal AvailableStock { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool IsSufficient { get; set; }
    public decimal Shortage { get; set; }
}
