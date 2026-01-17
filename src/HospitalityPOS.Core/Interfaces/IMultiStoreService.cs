using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for multi-store management operations.
/// </summary>
public interface IMultiStoreService
{
    // ================== Store Management ==================

    /// <summary>
    /// Gets all active stores.
    /// </summary>
    Task<IEnumerable<StoreDto>> GetAllStoresAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a store by ID.
    /// </summary>
    Task<StoreDto?> GetStoreByIdAsync(int storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a store by code.
    /// </summary>
    Task<StoreDto?> GetStoreByCodeAsync(string storeCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the headquarters store.
    /// </summary>
    Task<StoreDto?> GetHeadquartersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new store.
    /// </summary>
    Task<StoreDto?> CreateStoreAsync(CreateStoreDto dto, int createdByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a store.
    /// </summary>
    Task<bool> UpdateStoreAsync(int storeId, CreateStoreDto dto, int updatedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a store.
    /// </summary>
    Task<bool> DeactivateStoreAsync(int storeId, int deactivatedByUserId, CancellationToken cancellationToken = default);

    // ================== Central Product Management ==================

    /// <summary>
    /// Creates a product as a central/HQ product.
    /// </summary>
    Task<Product?> CreateCentralProductAsync(Product product, int createdByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pushes products to selected stores.
    /// </summary>
    Task<ProductPushResult> PushProductsToStoresAsync(PushProductsDto dto, int pushedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets products available at a specific store with effective pricing.
    /// </summary>
    Task<IEnumerable<StoreProductDto>> GetStoreProductsAsync(int storeId, int? categoryId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the effective price for a product at a specific store.
    /// </summary>
    Task<decimal> GetEffectivePriceAsync(int productId, int storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the effective cost for a product at a specific store.
    /// </summary>
    Task<decimal?> GetEffectiveCostAsync(int productId, int storeId, CancellationToken cancellationToken = default);

    // ================== Store Override Management ==================

    /// <summary>
    /// Gets all overrides for a store.
    /// </summary>
    Task<IEnumerable<StoreProductOverrideDto>> GetStoreOverridesAsync(int storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific override.
    /// </summary>
    Task<StoreProductOverrideDto?> GetOverrideAsync(int storeId, int productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a store product override.
    /// </summary>
    Task<StoreProductOverrideDto?> SetOverrideAsync(CreateStoreProductOverrideDto dto, int setByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an override (reverts to central pricing).
    /// </summary>
    Task<bool> RemoveOverrideAsync(int storeId, int productId, int removedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a product is available at a store.
    /// </summary>
    Task<bool> IsProductAvailableAtStoreAsync(int productId, int storeId, CancellationToken cancellationToken = default);

    // ================== Sync Management ==================

    /// <summary>
    /// Gets sync status for all stores.
    /// </summary>
    Task<IEnumerable<StoreSyncStatusDto>> GetStoreSyncStatusesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets sync status for a specific store.
    /// </summary>
    Task<StoreSyncStatusDto?> GetStoreSyncStatusAsync(int storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the last sync time for a store.
    /// </summary>
    Task UpdateStoreSyncTimeAsync(int storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets products that need to be synced to a store.
    /// </summary>
    Task<IEnumerable<Product>> GetPendingSyncProductsAsync(int storeId, CancellationToken cancellationToken = default);

    // ================== Pricing Zone Management ==================

    /// <summary>
    /// Gets all pricing zones.
    /// </summary>
    Task<IEnumerable<PricingZoneDto>> GetAllPricingZonesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a pricing zone by ID.
    /// </summary>
    Task<PricingZoneDto?> GetPricingZoneByIdAsync(int zoneId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default pricing zone.
    /// </summary>
    Task<PricingZoneDto?> GetDefaultPricingZoneAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new pricing zone.
    /// </summary>
    Task<PricingZoneDto?> CreatePricingZoneAsync(CreatePricingZoneDto dto, int createdByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a pricing zone.
    /// </summary>
    Task<bool> UpdatePricingZoneAsync(int zoneId, CreatePricingZoneDto dto, int updatedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns stores to a pricing zone.
    /// </summary>
    Task<bool> AssignStoresToZoneAsync(int zoneId, List<int> storeIds, int assignedByUserId, CancellationToken cancellationToken = default);

    // ================== Zone Pricing Management ==================

    /// <summary>
    /// Gets zone prices for a product.
    /// </summary>
    Task<IEnumerable<ZonePriceDto>> GetProductZonePricesAsync(int productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets prices for all products in a zone.
    /// </summary>
    Task<IEnumerable<ZonePriceDto>> GetZoneProductPricesAsync(int zoneId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a zone price for a product.
    /// </summary>
    Task<ZonePriceDto?> SetZonePriceAsync(CreateZonePriceDto dto, int setByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a zone price (reverts to central price).
    /// </summary>
    Task<bool> RemoveZonePriceAsync(int zoneId, int productId, int removedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies bulk zone price adjustments.
    /// </summary>
    Task<ProductPushResult> ApplyBulkZonePriceAsync(BulkZonePriceDto dto, int appliedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the effective price for a product in a zone.
    /// </summary>
    Task<decimal> GetZoneEffectivePriceAsync(int productId, int zoneId, CancellationToken cancellationToken = default);

    // ================== Scheduled Price Changes ==================

    /// <summary>
    /// Gets all pending scheduled price changes.
    /// </summary>
    Task<IEnumerable<ScheduledPriceChangeDto>> GetPendingPriceChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets scheduled price changes for a product.
    /// </summary>
    Task<IEnumerable<ScheduledPriceChangeDto>> GetProductScheduledChangesAsync(int productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a scheduled price change.
    /// </summary>
    Task<ScheduledPriceChangeDto?> CreateScheduledPriceChangeAsync(CreateScheduledPriceChangeDto dto, int createdByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a scheduled price change.
    /// </summary>
    Task<bool> CancelScheduledPriceChangeAsync(int priceChangeId, int cancelledByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies all due scheduled price changes.
    /// </summary>
    Task<PriceChangeApplicationResult> ApplyDuePriceChangesAsync(int appliedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets product pricing summary with all zone prices and scheduled changes.
    /// </summary>
    Task<ProductPricingSummaryDto?> GetProductPricingSummaryAsync(int productId, CancellationToken cancellationToken = default);
}
