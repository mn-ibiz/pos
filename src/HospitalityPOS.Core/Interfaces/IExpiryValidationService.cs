using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for expiry validation and sale blocking operations.
/// </summary>
public interface IExpiryValidationService
{
    #region Expiry Validation

    /// <summary>
    /// Validates a product for sale, checking for expiry issues.
    /// </summary>
    /// <param name="productId">The product ID to validate.</param>
    /// <param name="storeId">The store ID where sale is attempted.</param>
    /// <param name="quantity">The quantity being sold.</param>
    /// <returns>Expiry check result indicating if sale can proceed.</returns>
    Task<ExpiredItemCheckDto> ValidateProductForSaleAsync(int productId, int storeId, int quantity = 1);

    /// <summary>
    /// Validates a specific batch for sale.
    /// </summary>
    /// <param name="batchId">The batch ID to validate.</param>
    /// <param name="quantity">The quantity being sold.</param>
    /// <returns>Expiry check result.</returns>
    Task<ExpiredItemCheckDto> ValidateBatchForSaleAsync(int batchId, int quantity = 1);

    /// <summary>
    /// Checks if blocking is enabled for a product or category.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <returns>True if blocking is enabled.</returns>
    Task<bool> IsBlockingEnabledAsync(int productId);

    /// <summary>
    /// Checks if override is allowed for a product or category.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <returns>True if override is allowed.</returns>
    Task<bool> IsOverrideAllowedAsync(int productId);

    #endregion

    #region Sale Blocking

    /// <summary>
    /// Records a sale block attempt.
    /// </summary>
    /// <param name="dto">The sale block details.</param>
    /// <param name="userId">The user who attempted the sale.</param>
    /// <returns>The created sale block record.</returns>
    Task<ExpirySaleBlockDto> RecordSaleBlockAsync(CreateExpirySaleBlockDto dto, int userId);

    /// <summary>
    /// Processes a manager override for an expired sale.
    /// </summary>
    /// <param name="request">The override request with manager credentials.</param>
    /// <returns>Override result indicating success or failure.</returns>
    Task<ExpirySaleOverrideResultDto> ProcessOverrideAsync(ExpirySaleOverrideRequestDto request);

    /// <summary>
    /// Links a sale block to a completed receipt.
    /// </summary>
    /// <param name="saleBlockId">The sale block ID.</param>
    /// <param name="receiptId">The receipt ID.</param>
    Task LinkSaleBlockToReceiptAsync(int saleBlockId, int receiptId);

    /// <summary>
    /// Gets a sale block by ID.
    /// </summary>
    /// <param name="saleBlockId">The sale block ID.</param>
    /// <returns>The sale block details.</returns>
    Task<ExpirySaleBlockDto?> GetSaleBlockAsync(int saleBlockId);

    /// <summary>
    /// Gets sale blocks matching the query.
    /// </summary>
    /// <param name="query">The query parameters.</param>
    /// <returns>List of matching sale blocks.</returns>
    Task<List<ExpirySaleBlockDto>> GetSaleBlocksAsync(SaleBlockQueryDto query);

    /// <summary>
    /// Gets summary of sale blocks for reporting.
    /// </summary>
    /// <param name="storeId">Optional store ID filter.</param>
    /// <param name="fromDate">Optional start date filter.</param>
    /// <param name="toDate">Optional end date filter.</param>
    /// <returns>Summary statistics.</returns>
    Task<SaleBlockSummaryDto> GetSaleBlockSummaryAsync(int? storeId = null, DateTime? fromDate = null, DateTime? toDate = null);

    #endregion

    #region Category Settings

    /// <summary>
    /// Gets expiry settings for a category.
    /// </summary>
    /// <param name="categoryId">The category ID.</param>
    /// <returns>Category expiry settings.</returns>
    Task<CategoryExpirySettingsDto?> GetCategorySettingsAsync(int categoryId);

    /// <summary>
    /// Gets all category expiry settings.
    /// </summary>
    /// <returns>List of all category settings.</returns>
    Task<List<CategoryExpirySettingsDto>> GetAllCategorySettingsAsync();

    /// <summary>
    /// Creates or updates category expiry settings.
    /// </summary>
    /// <param name="dto">The settings to save.</param>
    /// <param name="userId">The user making the change.</param>
    /// <returns>The saved settings.</returns>
    Task<CategoryExpirySettingsDto> SaveCategorySettingsAsync(UpdateCategoryExpirySettingsDto dto, int userId);

    /// <summary>
    /// Deletes category expiry settings.
    /// </summary>
    /// <param name="categoryId">The category ID.</param>
    Task DeleteCategorySettingsAsync(int categoryId);

    /// <summary>
    /// Gets effective expiry settings for a product (combines product and category settings).
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <returns>Effective expiry configuration.</returns>
    Task<EffectiveExpirySettingsDto> GetEffectiveSettingsAsync(int productId);

    #endregion

    #region Override Audit

    /// <summary>
    /// Gets override history for audit purposes.
    /// </summary>
    /// <param name="storeId">Optional store filter.</param>
    /// <param name="fromDate">Optional start date.</param>
    /// <param name="toDate">Optional end date.</param>
    /// <returns>List of override records.</returns>
    Task<List<ExpirySaleBlockDto>> GetOverrideHistoryAsync(int? storeId = null, DateTime? fromDate = null, DateTime? toDate = null);

    /// <summary>
    /// Gets override statistics by manager.
    /// </summary>
    /// <param name="storeId">Optional store filter.</param>
    /// <param name="fromDate">Optional start date.</param>
    /// <param name="toDate">Optional end date.</param>
    /// <returns>Override statistics grouped by manager.</returns>
    Task<List<ManagerOverrideStatsDto>> GetOverrideStatsByManagerAsync(int? storeId = null, DateTime? fromDate = null, DateTime? toDate = null);

    #endregion
}

/// <summary>
/// DTO for effective expiry settings (combines product and category).
/// </summary>
public class EffectiveExpirySettingsDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public bool RequiresExpiryTracking { get; set; }
    public bool BlockExpiredSales { get; set; }
    public bool AllowManagerOverride { get; set; }
    public int WarningDays { get; set; }
    public int CriticalDays { get; set; }
    public string ExpiredItemAction { get; set; } = string.Empty;
    public string NearExpiryAction { get; set; } = string.Empty;
    public string SettingsSource { get; set; } = string.Empty; // "Product", "Category", "Default"
}

/// <summary>
/// DTO for manager override statistics.
/// </summary>
public class ManagerOverrideStatsDto
{
    public int ManagerUserId { get; set; }
    public string ManagerName { get; set; } = string.Empty;
    public int TotalOverrides { get; set; }
    public decimal TotalOverrideValue { get; set; }
    public int UniqueProducts { get; set; }
    public DateTime? FirstOverride { get; set; }
    public DateTime? LastOverride { get; set; }
    public List<string> TopOverridedProducts { get; set; } = new();
}
