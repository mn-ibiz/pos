using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for central promotion management and deployment operations.
/// </summary>
public interface ICentralPromotionService
{
    // ================== Promotion Management ==================

    /// <summary>
    /// Gets all promotions with optional filtering.
    /// </summary>
    Task<IEnumerable<CentralPromotionDto>> GetAllPromotionsAsync(PromotionQueryDto? query = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a promotion by ID.
    /// </summary>
    Task<CentralPromotionDto?> GetPromotionByIdAsync(int promotionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a promotion by code.
    /// </summary>
    Task<CentralPromotionDto?> GetPromotionByCodeAsync(string promotionCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new central promotion.
    /// </summary>
    Task<CentralPromotionDto?> CreatePromotionAsync(CreatePromotionDto dto, int createdByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a promotion.
    /// </summary>
    Task<bool> UpdatePromotionAsync(int promotionId, CreatePromotionDto dto, int updatedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a promotion.
    /// </summary>
    Task<bool> ActivatePromotionAsync(int promotionId, int activatedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses a promotion.
    /// </summary>
    Task<bool> PausePromotionAsync(int promotionId, int pausedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a promotion.
    /// </summary>
    Task<bool> CancelPromotionAsync(int promotionId, int cancelledByUserId, CancellationToken cancellationToken = default);

    // ================== Promotion Products & Categories ==================

    /// <summary>
    /// Adds products to a promotion.
    /// </summary>
    Task<bool> AddProductsToPromotionAsync(int promotionId, List<int> productIds, int addedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes products from a promotion.
    /// </summary>
    Task<bool> RemoveProductsFromPromotionAsync(int promotionId, List<int> productIds, int removedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds categories to a promotion.
    /// </summary>
    Task<bool> AddCategoriesToPromotionAsync(int promotionId, List<int> categoryIds, int addedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes categories from a promotion.
    /// </summary>
    Task<bool> RemoveCategoriesFromPromotionAsync(int promotionId, List<int> categoryIds, int removedByUserId, CancellationToken cancellationToken = default);

    // ================== Deployment Management ==================

    /// <summary>
    /// Deploys a promotion to stores.
    /// </summary>
    Task<DeploymentResult> DeployPromotionAsync(DeployPromotionDto dto, int deployedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets deployment history for a promotion.
    /// </summary>
    Task<IEnumerable<PromotionDeploymentDto>> GetPromotionDeploymentsAsync(int promotionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific deployment by ID.
    /// </summary>
    Task<PromotionDeploymentDto?> GetDeploymentByIdAsync(int deploymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending deployments that need sync.
    /// </summary>
    Task<IEnumerable<PromotionDeploymentDto>> GetPendingDeploymentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates deployment store status after sync.
    /// </summary>
    Task<bool> UpdateDeploymentStoreStatusAsync(int deploymentId, int storeId, DeploymentStatus status, string? errorMessage = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retries failed deployment for a store.
    /// </summary>
    Task<bool> RetryDeploymentForStoreAsync(int deploymentId, int storeId, int retriedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back a deployment.
    /// </summary>
    Task<bool> RollbackDeploymentAsync(int deploymentId, int rolledBackByUserId, CancellationToken cancellationToken = default);

    // ================== Redemption Management ==================

    /// <summary>
    /// Records a promotion redemption.
    /// </summary>
    Task<PromotionRedemptionDto?> RecordRedemptionAsync(RecordRedemptionDto dto, int processedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Voids a promotion redemption.
    /// </summary>
    Task<bool> VoidRedemptionAsync(int redemptionId, string reason, int voidedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets redemptions for a promotion.
    /// </summary>
    Task<IEnumerable<PromotionRedemptionDto>> GetPromotionRedemptionsAsync(int promotionId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets redemptions for a store.
    /// </summary>
    Task<IEnumerable<PromotionRedemptionDto>> GetStoreRedemptionsAsync(int storeId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets redemption count for a promotion.
    /// </summary>
    Task<int> GetRedemptionCountAsync(int promotionId, CancellationToken cancellationToken = default);

    // ================== Dashboard & Reporting ==================

    /// <summary>
    /// Gets promotion dashboard data.
    /// </summary>
    Task<PromotionDashboardDto?> GetPromotionDashboardAsync(int promotionId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets redemption summary by store for a promotion.
    /// </summary>
    Task<IEnumerable<StoreRedemptionSummaryDto>> GetRedemptionsByStoreAsync(int promotionId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active promotions for a store.
    /// </summary>
    Task<IEnumerable<StoreActivePromotionDto>> GetActivePromotionsForStoreAsync(int storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a promotion is applicable for a product at a store.
    /// </summary>
    Task<StoreActivePromotionDto?> GetApplicablePromotionAsync(int storeId, int productId, decimal quantity = 1, string? couponCode = null, CancellationToken cancellationToken = default);
}
