using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Implementation of the multi-store management service.
/// </summary>
public class MultiStoreService : IMultiStoreService
{
    private readonly IRepository<Store> _storeRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<StoreProductOverride> _overrideRepository;
    private readonly IRepository<PricingZone> _pricingZoneRepository;
    private readonly IRepository<ZonePrice> _zonePriceRepository;
    private readonly IRepository<ScheduledPriceChange> _priceChangeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MultiStoreService> _logger;

    public MultiStoreService(
        IRepository<Store> storeRepository,
        IRepository<Product> productRepository,
        IRepository<StoreProductOverride> overrideRepository,
        IRepository<PricingZone> pricingZoneRepository,
        IRepository<ZonePrice> zonePriceRepository,
        IRepository<ScheduledPriceChange> priceChangeRepository,
        IUnitOfWork unitOfWork,
        ILogger<MultiStoreService> logger)
    {
        _storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _overrideRepository = overrideRepository ?? throw new ArgumentNullException(nameof(overrideRepository));
        _pricingZoneRepository = pricingZoneRepository ?? throw new ArgumentNullException(nameof(pricingZoneRepository));
        _zonePriceRepository = zonePriceRepository ?? throw new ArgumentNullException(nameof(zonePriceRepository));
        _priceChangeRepository = priceChangeRepository ?? throw new ArgumentNullException(nameof(priceChangeRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ================== Store Management ==================

    /// <inheritdoc />
    public async Task<IEnumerable<StoreDto>> GetAllStoresAsync(CancellationToken cancellationToken = default)
    {
        var stores = await _storeRepository.FindAsync(s => s.IsActive, cancellationToken).ConfigureAwait(false);
        return stores.Select(MapStoreToDto);
    }

    /// <inheritdoc />
    public async Task<StoreDto?> GetStoreByIdAsync(int storeId, CancellationToken cancellationToken = default)
    {
        var store = await _storeRepository.GetByIdAsync(storeId, cancellationToken).ConfigureAwait(false);
        return store != null ? MapStoreToDto(store) : null;
    }

    /// <inheritdoc />
    public async Task<StoreDto?> GetStoreByCodeAsync(string storeCode, CancellationToken cancellationToken = default)
    {
        var stores = await _storeRepository.FindAsync(
            s => s.StoreCode == storeCode && s.IsActive, cancellationToken).ConfigureAwait(false);
        var store = stores.FirstOrDefault();
        return store != null ? MapStoreToDto(store) : null;
    }

    /// <inheritdoc />
    public async Task<StoreDto?> GetHeadquartersAsync(CancellationToken cancellationToken = default)
    {
        var stores = await _storeRepository.FindAsync(
            s => s.IsHeadquarters && s.IsActive, cancellationToken).ConfigureAwait(false);
        var hq = stores.FirstOrDefault();
        return hq != null ? MapStoreToDto(hq) : null;
    }

    /// <inheritdoc />
    public async Task<StoreDto?> CreateStoreAsync(CreateStoreDto dto, int createdByUserId, CancellationToken cancellationToken = default)
    {
        // Check for duplicate store code
        var existing = await _storeRepository.FindAsync(
            s => s.StoreCode == dto.StoreCode, cancellationToken).ConfigureAwait(false);

        if (existing.Any())
        {
            _logger.LogWarning("Attempted to create store with duplicate code: {Code}", dto.StoreCode);
            return null;
        }

        var store = new Store
        {
            StoreCode = dto.StoreCode,
            Name = dto.Name,
            Address = dto.Address,
            City = dto.City,
            Region = dto.Region,
            PhoneNumber = dto.PhoneNumber,
            Email = dto.Email,
            TaxRegistrationNumber = dto.TaxRegistrationNumber,
            IsHeadquarters = dto.IsHeadquarters,
            ReceivesCentralUpdates = dto.ReceivesCentralUpdates,
            ManagerName = dto.ManagerName,
            IsActive = true,
            CreatedByUserId = createdByUserId
        };

        await _storeRepository.AddAsync(store, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Created new store: {Code} - {Name}", store.StoreCode, store.Name);
        return MapStoreToDto(store);
    }

    /// <inheritdoc />
    public async Task<bool> UpdateStoreAsync(int storeId, CreateStoreDto dto, int updatedByUserId, CancellationToken cancellationToken = default)
    {
        var store = await _storeRepository.GetByIdAsync(storeId, cancellationToken).ConfigureAwait(false);
        if (store == null) return false;

        store.Name = dto.Name;
        store.Address = dto.Address;
        store.City = dto.City;
        store.Region = dto.Region;
        store.PhoneNumber = dto.PhoneNumber;
        store.Email = dto.Email;
        store.TaxRegistrationNumber = dto.TaxRegistrationNumber;
        store.IsHeadquarters = dto.IsHeadquarters;
        store.ReceivesCentralUpdates = dto.ReceivesCentralUpdates;
        store.ManagerName = dto.ManagerName;
        store.UpdatedByUserId = updatedByUserId;

        await _storeRepository.UpdateAsync(store, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Updated store: {Code} - {Name}", store.StoreCode, store.Name);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeactivateStoreAsync(int storeId, int deactivatedByUserId, CancellationToken cancellationToken = default)
    {
        var store = await _storeRepository.GetByIdAsync(storeId, cancellationToken).ConfigureAwait(false);
        if (store == null) return false;

        store.IsActive = false;
        store.UpdatedByUserId = deactivatedByUserId;

        await _storeRepository.UpdateAsync(store, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Deactivated store: {Code} - {Name}", store.StoreCode, store.Name);
        return true;
    }

    // ================== Central Product Management ==================

    /// <inheritdoc />
    public async Task<Product?> CreateCentralProductAsync(Product product, int createdByUserId, CancellationToken cancellationToken = default)
    {
        product.IsCentralProduct = true;
        product.AllowStoreOverride = true;
        product.CreatedByUserId = createdByUserId;
        product.IsActive = true;

        await _productRepository.AddAsync(product, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Created central product: {Code} - {Name}", product.Code, product.Name);
        return product;
    }

    /// <inheritdoc />
    public async Task<ProductPushResult> PushProductsToStoresAsync(PushProductsDto dto, int pushedByUserId, CancellationToken cancellationToken = default)
    {
        if (!dto.ProductIds.Any() || !dto.StoreIds.Any())
        {
            return ProductPushResult.Failure("No products or stores specified.");
        }

        var errors = new List<string>();
        int productsProcessed = 0;
        int storesUpdated = 0;

        // Get products
        var products = await _productRepository.FindAsync(
            p => dto.ProductIds.Contains(p.Id) && p.IsActive, cancellationToken).ConfigureAwait(false);

        if (!products.Any())
        {
            return ProductPushResult.Failure("No valid products found.");
        }

        // Get stores
        var stores = await _storeRepository.FindAsync(
            s => dto.StoreIds.Contains(s.Id) && s.IsActive && s.ReceivesCentralUpdates, cancellationToken).ConfigureAwait(false);

        if (!stores.Any())
        {
            return ProductPushResult.Failure("No valid stores found that receive updates.");
        }

        foreach (var store in stores)
        {
            foreach (var product in products)
            {
                try
                {
                    // Mark product as central if not already
                    if (!product.IsCentralProduct)
                    {
                        product.IsCentralProduct = true;
                        await _productRepository.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
                    }

                    // Update product sync time
                    product.LastSyncTime = DateTime.UtcNow;
                    await _productRepository.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
                    productsProcessed++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to push product {product.Code} to store {store.StoreCode}: {ex.Message}");
                    _logger.LogError(ex, "Failed to push product {ProductId} to store {StoreId}", product.Id, store.Id);
                }
            }

            // Update store sync time
            store.LastSyncTime = DateTime.UtcNow;
            await _storeRepository.UpdateAsync(store, cancellationToken).ConfigureAwait(false);
            storesUpdated++;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (errors.Any())
        {
            _logger.LogWarning("Product push completed with {ErrorCount} errors", errors.Count);
            return ProductPushResult.Failure("Push completed with some errors.", errors);
        }

        _logger.LogInformation("Pushed {ProductCount} products to {StoreCount} stores", productsProcessed, storesUpdated);
        return ProductPushResult.Success(productsProcessed, storesUpdated);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StoreProductDto>> GetStoreProductsAsync(int storeId, int? categoryId = null, CancellationToken cancellationToken = default)
    {
        // Get all central products
        var products = await _productRepository.FindAsync(
            p => p.IsCentralProduct && p.IsActive &&
                 (!categoryId.HasValue || p.CategoryId == categoryId), cancellationToken).ConfigureAwait(false);

        // Get overrides for this store
        var overrides = await _overrideRepository.FindAsync(
            o => o.StoreId == storeId && o.IsActive, cancellationToken).ConfigureAwait(false);

        var overrideDict = overrides.ToDictionary(o => o.ProductId);

        return products.Select(p =>
        {
            overrideDict.TryGetValue(p.Id, out var o);
            return new StoreProductDto
            {
                ProductId = p.Id,
                ProductCode = p.Code,
                ProductName = p.Name,
                CategoryName = p.Category?.Name,
                CentralPrice = p.SellingPrice,
                OverridePrice = o?.OverridePrice,
                CentralCost = p.CostPrice,
                OverrideCost = o?.OverrideCost,
                IsAvailable = o?.IsAvailable ?? true,
                HasOverride = o != null,
                IsCentralProduct = p.IsCentralProduct,
                LastSyncTime = p.LastSyncTime
            };
        }).Where(p => p.IsAvailable);
    }

    /// <inheritdoc />
    public async Task<decimal> GetEffectivePriceAsync(int productId, int storeId, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken).ConfigureAwait(false);
        if (product == null) return 0;

        var overrides = await _overrideRepository.FindAsync(
            o => o.StoreId == storeId && o.ProductId == productId && o.IsActive, cancellationToken).ConfigureAwait(false);

        var o = overrides.FirstOrDefault();
        return o?.OverridePrice ?? product.SellingPrice;
    }

    /// <inheritdoc />
    public async Task<decimal?> GetEffectiveCostAsync(int productId, int storeId, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken).ConfigureAwait(false);
        if (product == null) return null;

        var overrides = await _overrideRepository.FindAsync(
            o => o.StoreId == storeId && o.ProductId == productId && o.IsActive, cancellationToken).ConfigureAwait(false);

        var o = overrides.FirstOrDefault();
        return o?.OverrideCost ?? product.CostPrice;
    }

    // ================== Store Override Management ==================

    /// <inheritdoc />
    public async Task<IEnumerable<StoreProductOverrideDto>> GetStoreOverridesAsync(int storeId, CancellationToken cancellationToken = default)
    {
        var overrides = await _overrideRepository.FindAsync(
            o => o.StoreId == storeId && o.IsActive, cancellationToken).ConfigureAwait(false);

        var result = new List<StoreProductOverrideDto>();
        foreach (var o in overrides)
        {
            var product = await _productRepository.GetByIdAsync(o.ProductId, cancellationToken).ConfigureAwait(false);
            var store = await _storeRepository.GetByIdAsync(o.StoreId, cancellationToken).ConfigureAwait(false);

            result.Add(new StoreProductOverrideDto
            {
                Id = o.Id,
                StoreId = o.StoreId,
                StoreName = store?.Name ?? "Unknown",
                ProductId = o.ProductId,
                ProductName = product?.Name ?? "Unknown",
                ProductCode = product?.Code ?? "",
                CentralPrice = product?.SellingPrice ?? 0,
                OverridePrice = o.OverridePrice,
                OverrideCost = o.OverrideCost,
                IsAvailable = o.IsAvailable,
                OverrideReason = o.OverrideReason,
                LastSyncTime = o.LastSyncTime
            });
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<StoreProductOverrideDto?> GetOverrideAsync(int storeId, int productId, CancellationToken cancellationToken = default)
    {
        var overrides = await _overrideRepository.FindAsync(
            o => o.StoreId == storeId && o.ProductId == productId && o.IsActive, cancellationToken).ConfigureAwait(false);

        var o = overrides.FirstOrDefault();
        if (o == null) return null;

        var product = await _productRepository.GetByIdAsync(productId, cancellationToken).ConfigureAwait(false);
        var store = await _storeRepository.GetByIdAsync(storeId, cancellationToken).ConfigureAwait(false);

        return new StoreProductOverrideDto
        {
            Id = o.Id,
            StoreId = o.StoreId,
            StoreName = store?.Name ?? "Unknown",
            ProductId = o.ProductId,
            ProductName = product?.Name ?? "Unknown",
            ProductCode = product?.Code ?? "",
            CentralPrice = product?.SellingPrice ?? 0,
            OverridePrice = o.OverridePrice,
            OverrideCost = o.OverrideCost,
            IsAvailable = o.IsAvailable,
            OverrideReason = o.OverrideReason,
            LastSyncTime = o.LastSyncTime
        };
    }

    /// <inheritdoc />
    public async Task<StoreProductOverrideDto?> SetOverrideAsync(CreateStoreProductOverrideDto dto, int setByUserId, CancellationToken cancellationToken = default)
    {
        // Verify product allows overrides
        var product = await _productRepository.GetByIdAsync(dto.ProductId, cancellationToken).ConfigureAwait(false);
        if (product == null || !product.AllowStoreOverride)
        {
            _logger.LogWarning("Cannot override product {ProductId} - not found or overrides not allowed", dto.ProductId);
            return null;
        }

        // Check for existing override
        var existing = await _overrideRepository.FindAsync(
            o => o.StoreId == dto.StoreId && o.ProductId == dto.ProductId, cancellationToken).ConfigureAwait(false);

        var existingOverride = existing.FirstOrDefault();

        if (existingOverride != null)
        {
            // Update existing
            existingOverride.OverridePrice = dto.OverridePrice;
            existingOverride.OverrideCost = dto.OverrideCost;
            existingOverride.IsAvailable = dto.IsAvailable;
            existingOverride.OverrideMinStock = dto.OverrideMinStock;
            existingOverride.OverrideMaxStock = dto.OverrideMaxStock;
            existingOverride.OverrideTaxRate = dto.OverrideTaxRate;
            existingOverride.OverrideReason = dto.OverrideReason;
            existingOverride.LastSyncTime = DateTime.UtcNow;
            existingOverride.UpdatedByUserId = setByUserId;
            existingOverride.IsActive = true;

            await _overrideRepository.UpdateAsync(existingOverride, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // Create new
            existingOverride = new StoreProductOverride
            {
                StoreId = dto.StoreId,
                ProductId = dto.ProductId,
                OverridePrice = dto.OverridePrice,
                OverrideCost = dto.OverrideCost,
                IsAvailable = dto.IsAvailable,
                OverrideMinStock = dto.OverrideMinStock,
                OverrideMaxStock = dto.OverrideMaxStock,
                OverrideTaxRate = dto.OverrideTaxRate,
                OverrideReason = dto.OverrideReason,
                LastSyncTime = DateTime.UtcNow,
                CreatedByUserId = setByUserId,
                IsActive = true
            };

            await _overrideRepository.AddAsync(existingOverride, cancellationToken).ConfigureAwait(false);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Set override for product {ProductId} at store {StoreId}", dto.ProductId, dto.StoreId);
        return await GetOverrideAsync(dto.StoreId, dto.ProductId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> RemoveOverrideAsync(int storeId, int productId, int removedByUserId, CancellationToken cancellationToken = default)
    {
        var overrides = await _overrideRepository.FindAsync(
            o => o.StoreId == storeId && o.ProductId == productId && o.IsActive, cancellationToken).ConfigureAwait(false);

        var o = overrides.FirstOrDefault();
        if (o == null) return false;

        o.IsActive = false;
        o.UpdatedByUserId = removedByUserId;

        await _overrideRepository.UpdateAsync(o, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Removed override for product {ProductId} at store {StoreId}", productId, storeId);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> IsProductAvailableAtStoreAsync(int productId, int storeId, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken).ConfigureAwait(false);
        if (product == null || !product.IsActive) return false;

        var overrides = await _overrideRepository.FindAsync(
            o => o.StoreId == storeId && o.ProductId == productId && o.IsActive, cancellationToken).ConfigureAwait(false);

        var o = overrides.FirstOrDefault();
        return o?.IsAvailable ?? true;
    }

    // ================== Sync Management ==================

    /// <inheritdoc />
    public async Task<IEnumerable<StoreSyncStatusDto>> GetStoreSyncStatusesAsync(CancellationToken cancellationToken = default)
    {
        var stores = await _storeRepository.FindAsync(s => s.IsActive, cancellationToken).ConfigureAwait(false);
        var result = new List<StoreSyncStatusDto>();

        foreach (var store in stores)
        {
            result.Add(await BuildSyncStatus(store, cancellationToken).ConfigureAwait(false));
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<StoreSyncStatusDto?> GetStoreSyncStatusAsync(int storeId, CancellationToken cancellationToken = default)
    {
        var store = await _storeRepository.GetByIdAsync(storeId, cancellationToken).ConfigureAwait(false);
        if (store == null) return null;

        return await BuildSyncStatus(store, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpdateStoreSyncTimeAsync(int storeId, CancellationToken cancellationToken = default)
    {
        var store = await _storeRepository.GetByIdAsync(storeId, cancellationToken).ConfigureAwait(false);
        if (store == null) return;

        store.LastSyncTime = DateTime.UtcNow;
        await _storeRepository.UpdateAsync(store, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Product>> GetPendingSyncProductsAsync(int storeId, CancellationToken cancellationToken = default)
    {
        var store = await _storeRepository.GetByIdAsync(storeId, cancellationToken).ConfigureAwait(false);
        if (store == null) return Enumerable.Empty<Product>();

        var lastSync = store.LastSyncTime ?? DateTime.MinValue;

        var products = await _productRepository.FindAsync(
            p => p.IsCentralProduct && p.IsActive &&
                 (p.LastSyncTime == null || p.LastSyncTime > lastSync), cancellationToken).ConfigureAwait(false);

        return products;
    }

    // ================== Private Helpers ==================

    private async Task<StoreSyncStatusDto> BuildSyncStatus(Store store, CancellationToken cancellationToken)
    {
        var totalProducts = await _productRepository.FindAsync(
            p => p.IsCentralProduct && p.IsActive, cancellationToken).ConfigureAwait(false);

        var overrides = await _overrideRepository.FindAsync(
            o => o.StoreId == store.Id && o.IsActive, cancellationToken).ConfigureAwait(false);

        var pendingProducts = await GetPendingSyncProductsAsync(store.Id, cancellationToken).ConfigureAwait(false);

        return new StoreSyncStatusDto
        {
            StoreId = store.Id,
            StoreName = store.Name,
            LastSyncTime = store.LastSyncTime,
            TotalProducts = totalProducts.Count(),
            ProductsWithOverrides = overrides.Count(),
            PendingSyncProducts = pendingProducts.Count(),
            IsOnline = true // Would be determined by actual connectivity check
        };
    }

    private static StoreDto MapStoreToDto(Store store)
    {
        return new StoreDto
        {
            Id = store.Id,
            StoreCode = store.StoreCode,
            Name = store.Name,
            Address = store.Address,
            City = store.City,
            Region = store.Region,
            PhoneNumber = store.PhoneNumber,
            Email = store.Email,
            TaxRegistrationNumber = store.TaxRegistrationNumber,
            IsHeadquarters = store.IsHeadquarters,
            ReceivesCentralUpdates = store.ReceivesCentralUpdates,
            LastSyncTime = store.LastSyncTime,
            ManagerName = store.ManagerName,
            IsActive = store.IsActive
        };
    }

    // ================== Pricing Zone Management ==================

    /// <inheritdoc />
    public async Task<IEnumerable<PricingZoneDto>> GetAllPricingZonesAsync(CancellationToken cancellationToken = default)
    {
        var zones = await _pricingZoneRepository.FindAsync(z => z.IsActive, cancellationToken).ConfigureAwait(false);
        var result = new List<PricingZoneDto>();

        foreach (var zone in zones.OrderBy(z => z.DisplayOrder))
        {
            var stores = await _storeRepository.FindAsync(
                s => s.PricingZoneId == zone.Id && s.IsActive, cancellationToken).ConfigureAwait(false);

            result.Add(MapPricingZoneToDto(zone, stores.Count()));
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<PricingZoneDto?> GetPricingZoneByIdAsync(int zoneId, CancellationToken cancellationToken = default)
    {
        var zone = await _pricingZoneRepository.GetByIdAsync(zoneId, cancellationToken).ConfigureAwait(false);
        if (zone == null) return null;

        var stores = await _storeRepository.FindAsync(
            s => s.PricingZoneId == zoneId && s.IsActive, cancellationToken).ConfigureAwait(false);

        return MapPricingZoneToDto(zone, stores.Count());
    }

    /// <inheritdoc />
    public async Task<PricingZoneDto?> GetDefaultPricingZoneAsync(CancellationToken cancellationToken = default)
    {
        var zones = await _pricingZoneRepository.FindAsync(
            z => z.IsDefault && z.IsActive, cancellationToken).ConfigureAwait(false);

        var zone = zones.FirstOrDefault();
        if (zone == null) return null;

        var stores = await _storeRepository.FindAsync(
            s => s.PricingZoneId == zone.Id && s.IsActive, cancellationToken).ConfigureAwait(false);

        return MapPricingZoneToDto(zone, stores.Count());
    }

    /// <inheritdoc />
    public async Task<PricingZoneDto?> CreatePricingZoneAsync(CreatePricingZoneDto dto, int createdByUserId, CancellationToken cancellationToken = default)
    {
        var existing = await _pricingZoneRepository.FindAsync(
            z => z.ZoneCode == dto.ZoneCode, cancellationToken).ConfigureAwait(false);

        if (existing.Any())
        {
            _logger.LogWarning("Attempted to create pricing zone with duplicate code: {Code}", dto.ZoneCode);
            return null;
        }

        var zone = new PricingZone
        {
            ZoneCode = dto.ZoneCode,
            Name = dto.Name,
            Description = dto.Description,
            CurrencyCode = dto.CurrencyCode,
            DefaultTaxRate = dto.DefaultTaxRate,
            IsDefault = dto.IsDefault,
            DisplayOrder = dto.DisplayOrder,
            IsActive = true,
            CreatedByUserId = createdByUserId
        };

        await _pricingZoneRepository.AddAsync(zone, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Assign stores if specified
        if (dto.StoreIds?.Any() == true)
        {
            await AssignStoresToZoneAsync(zone.Id, dto.StoreIds, createdByUserId, cancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Created pricing zone: {Code} - {Name}", zone.ZoneCode, zone.Name);
        return MapPricingZoneToDto(zone, dto.StoreIds?.Count ?? 0);
    }

    /// <inheritdoc />
    public async Task<bool> UpdatePricingZoneAsync(int zoneId, CreatePricingZoneDto dto, int updatedByUserId, CancellationToken cancellationToken = default)
    {
        var zone = await _pricingZoneRepository.GetByIdAsync(zoneId, cancellationToken).ConfigureAwait(false);
        if (zone == null) return false;

        zone.Name = dto.Name;
        zone.Description = dto.Description;
        zone.CurrencyCode = dto.CurrencyCode;
        zone.DefaultTaxRate = dto.DefaultTaxRate;
        zone.IsDefault = dto.IsDefault;
        zone.DisplayOrder = dto.DisplayOrder;
        zone.UpdatedByUserId = updatedByUserId;

        await _pricingZoneRepository.UpdateAsync(zone, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Updated pricing zone: {Code} - {Name}", zone.ZoneCode, zone.Name);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> AssignStoresToZoneAsync(int zoneId, List<int> storeIds, int assignedByUserId, CancellationToken cancellationToken = default)
    {
        var zone = await _pricingZoneRepository.GetByIdAsync(zoneId, cancellationToken).ConfigureAwait(false);
        if (zone == null) return false;

        var stores = await _storeRepository.FindAsync(
            s => storeIds.Contains(s.Id) && s.IsActive, cancellationToken).ConfigureAwait(false);

        foreach (var store in stores)
        {
            store.PricingZoneId = zoneId;
            store.UpdatedByUserId = assignedByUserId;
            await _storeRepository.UpdateAsync(store, cancellationToken).ConfigureAwait(false);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Assigned {Count} stores to zone: {ZoneName}", stores.Count(), zone.Name);
        return true;
    }

    // ================== Zone Pricing Management ==================

    /// <inheritdoc />
    public async Task<IEnumerable<ZonePriceDto>> GetProductZonePricesAsync(int productId, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken).ConfigureAwait(false);
        if (product == null) return Enumerable.Empty<ZonePriceDto>();

        var zonePrices = await _zonePriceRepository.FindAsync(
            zp => zp.ProductId == productId && zp.IsActive, cancellationToken).ConfigureAwait(false);

        var result = new List<ZonePriceDto>();
        foreach (var zp in zonePrices)
        {
            var zone = await _pricingZoneRepository.GetByIdAsync(zp.PricingZoneId, cancellationToken).ConfigureAwait(false);
            result.Add(MapZonePriceToDto(zp, zone, product));
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ZonePriceDto>> GetZoneProductPricesAsync(int zoneId, CancellationToken cancellationToken = default)
    {
        var zone = await _pricingZoneRepository.GetByIdAsync(zoneId, cancellationToken).ConfigureAwait(false);
        if (zone == null) return Enumerable.Empty<ZonePriceDto>();

        var zonePrices = await _zonePriceRepository.FindAsync(
            zp => zp.PricingZoneId == zoneId && zp.IsActive, cancellationToken).ConfigureAwait(false);

        var result = new List<ZonePriceDto>();
        foreach (var zp in zonePrices)
        {
            var product = await _productRepository.GetByIdAsync(zp.ProductId, cancellationToken).ConfigureAwait(false);
            result.Add(MapZonePriceToDto(zp, zone, product));
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<ZonePriceDto?> SetZonePriceAsync(CreateZonePriceDto dto, int setByUserId, CancellationToken cancellationToken = default)
    {
        var zone = await _pricingZoneRepository.GetByIdAsync(dto.PricingZoneId, cancellationToken).ConfigureAwait(false);
        var product = await _productRepository.GetByIdAsync(dto.ProductId, cancellationToken).ConfigureAwait(false);

        if (zone == null || product == null)
        {
            _logger.LogWarning("Cannot set zone price - zone or product not found");
            return null;
        }

        var existing = await _zonePriceRepository.FindAsync(
            zp => zp.PricingZoneId == dto.PricingZoneId &&
                  zp.ProductId == dto.ProductId &&
                  zp.EffectiveFrom == dto.EffectiveFrom, cancellationToken).ConfigureAwait(false);

        var zonePrice = existing.FirstOrDefault();

        if (zonePrice != null)
        {
            zonePrice.Price = dto.Price;
            zonePrice.CostPrice = dto.CostPrice;
            zonePrice.MinimumPrice = dto.MinimumPrice;
            zonePrice.EffectiveTo = dto.EffectiveTo;
            zonePrice.Reason = dto.Reason;
            zonePrice.UpdatedByUserId = setByUserId;
            zonePrice.IsActive = true;

            await _zonePriceRepository.UpdateAsync(zonePrice, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            zonePrice = new ZonePrice
            {
                PricingZoneId = dto.PricingZoneId,
                ProductId = dto.ProductId,
                Price = dto.Price,
                CostPrice = dto.CostPrice,
                MinimumPrice = dto.MinimumPrice,
                EffectiveFrom = dto.EffectiveFrom,
                EffectiveTo = dto.EffectiveTo,
                Reason = dto.Reason,
                CreatedByUserId = setByUserId,
                IsActive = true
            };

            await _zonePriceRepository.AddAsync(zonePrice, cancellationToken).ConfigureAwait(false);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Set zone price for product {ProductId} in zone {ZoneId}: {Price}",
            dto.ProductId, dto.PricingZoneId, dto.Price);

        return MapZonePriceToDto(zonePrice, zone, product);
    }

    /// <inheritdoc />
    public async Task<bool> RemoveZonePriceAsync(int zoneId, int productId, int removedByUserId, CancellationToken cancellationToken = default)
    {
        var zonePrices = await _zonePriceRepository.FindAsync(
            zp => zp.PricingZoneId == zoneId && zp.ProductId == productId && zp.IsActive, cancellationToken).ConfigureAwait(false);

        var zp = zonePrices.FirstOrDefault();
        if (zp == null) return false;

        zp.IsActive = false;
        zp.UpdatedByUserId = removedByUserId;

        await _zonePriceRepository.UpdateAsync(zp, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Removed zone price for product {ProductId} in zone {ZoneId}", productId, zoneId);
        return true;
    }

    /// <inheritdoc />
    public async Task<ProductPushResult> ApplyBulkZonePriceAsync(BulkZonePriceDto dto, int appliedByUserId, CancellationToken cancellationToken = default)
    {
        if (!dto.ProductIds.Any())
        {
            return ProductPushResult.Failure("No products specified.");
        }

        var zone = await _pricingZoneRepository.GetByIdAsync(dto.PricingZoneId, cancellationToken).ConfigureAwait(false);
        if (zone == null)
        {
            return ProductPushResult.Failure("Pricing zone not found.");
        }

        var errors = new List<string>();
        int processed = 0;

        var products = await _productRepository.FindAsync(
            p => dto.ProductIds.Contains(p.Id) && p.IsActive, cancellationToken).ConfigureAwait(false);

        foreach (var product in products)
        {
            try
            {
                decimal newPrice = product.SellingPrice;

                if (dto.PriceAdjustmentPercent.HasValue)
                {
                    newPrice = product.SellingPrice * (1 + dto.PriceAdjustmentPercent.Value / 100);
                }
                else if (dto.PriceAdjustmentAmount.HasValue)
                {
                    newPrice = product.SellingPrice + dto.PriceAdjustmentAmount.Value;
                }

                newPrice = Math.Round(newPrice, 2);

                var zonePriceDto = new CreateZonePriceDto
                {
                    PricingZoneId = dto.PricingZoneId,
                    ProductId = product.Id,
                    Price = newPrice,
                    EffectiveFrom = dto.EffectiveFrom,
                    Reason = dto.Reason
                };

                await SetZonePriceAsync(zonePriceDto, appliedByUserId, cancellationToken).ConfigureAwait(false);
                processed++;
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to set zone price for {product.Code}: {ex.Message}");
                _logger.LogError(ex, "Failed to apply bulk zone price for product {ProductId}", product.Id);
            }
        }

        if (errors.Any())
        {
            return ProductPushResult.Failure("Bulk zone price applied with some errors.", errors);
        }

        _logger.LogInformation("Applied bulk zone price to {Count} products in zone {ZoneId}", processed, dto.PricingZoneId);
        return ProductPushResult.Success(processed, 1);
    }

    /// <inheritdoc />
    public async Task<decimal> GetZoneEffectivePriceAsync(int productId, int zoneId, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken).ConfigureAwait(false);
        if (product == null) return 0;

        var zonePrices = await _zonePriceRepository.FindAsync(
            zp => zp.PricingZoneId == zoneId &&
                  zp.ProductId == productId &&
                  zp.IsActive &&
                  zp.EffectiveFrom <= DateTime.UtcNow &&
                  (!zp.EffectiveTo.HasValue || zp.EffectiveTo >= DateTime.UtcNow), cancellationToken).ConfigureAwait(false);

        var activeZonePrice = zonePrices.OrderByDescending(zp => zp.EffectiveFrom).FirstOrDefault();

        return activeZonePrice?.Price ?? product.SellingPrice;
    }

    // ================== Scheduled Price Changes ==================

    /// <inheritdoc />
    public async Task<IEnumerable<ScheduledPriceChangeDto>> GetPendingPriceChangesAsync(CancellationToken cancellationToken = default)
    {
        var changes = await _priceChangeRepository.FindAsync(
            c => c.Status == PriceChangeStatus.Scheduled && c.IsActive, cancellationToken).ConfigureAwait(false);

        var result = new List<ScheduledPriceChangeDto>();
        foreach (var change in changes.OrderBy(c => c.EffectiveDate))
        {
            result.Add(await MapScheduledPriceChangeToDto(change, cancellationToken).ConfigureAwait(false));
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ScheduledPriceChangeDto>> GetProductScheduledChangesAsync(int productId, CancellationToken cancellationToken = default)
    {
        var changes = await _priceChangeRepository.FindAsync(
            c => c.ProductId == productId && c.IsActive, cancellationToken).ConfigureAwait(false);

        var result = new List<ScheduledPriceChangeDto>();
        foreach (var change in changes.OrderByDescending(c => c.EffectiveDate))
        {
            result.Add(await MapScheduledPriceChangeToDto(change, cancellationToken).ConfigureAwait(false));
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<ScheduledPriceChangeDto?> CreateScheduledPriceChangeAsync(CreateScheduledPriceChangeDto dto, int createdByUserId, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(dto.ProductId, cancellationToken).ConfigureAwait(false);
        if (product == null)
        {
            _logger.LogWarning("Cannot create scheduled price change - product {ProductId} not found", dto.ProductId);
            return null;
        }

        var priceChange = new ScheduledPriceChange
        {
            ProductId = dto.ProductId,
            PricingZoneId = dto.PricingZoneId,
            StoreId = dto.StoreId,
            OldPrice = product.SellingPrice,
            NewPrice = dto.NewPrice,
            NewCostPrice = dto.NewCostPrice,
            EffectiveDate = dto.EffectiveDate,
            ExpiryDate = dto.ExpiryDate,
            Status = PriceChangeStatus.Scheduled,
            Reason = dto.Reason,
            Notes = dto.Notes,
            CreatedByUserId = createdByUserId,
            IsActive = true
        };

        await _priceChangeRepository.AddAsync(priceChange, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Created scheduled price change for product {ProductId}: {OldPrice} -> {NewPrice} on {Date}",
            dto.ProductId, product.SellingPrice, dto.NewPrice, dto.EffectiveDate);

        return await MapScheduledPriceChangeToDto(priceChange, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> CancelScheduledPriceChangeAsync(int priceChangeId, int cancelledByUserId, CancellationToken cancellationToken = default)
    {
        var priceChange = await _priceChangeRepository.GetByIdAsync(priceChangeId, cancellationToken).ConfigureAwait(false);
        if (priceChange == null || priceChange.Status != PriceChangeStatus.Scheduled) return false;

        priceChange.Status = PriceChangeStatus.Cancelled;
        priceChange.UpdatedByUserId = cancelledByUserId;

        await _priceChangeRepository.UpdateAsync(priceChange, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Cancelled scheduled price change {Id}", priceChangeId);
        return true;
    }

    /// <inheritdoc />
    public async Task<PriceChangeApplicationResult> ApplyDuePriceChangesAsync(int appliedByUserId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var dueChanges = await _priceChangeRepository.FindAsync(
            c => c.Status == PriceChangeStatus.Scheduled &&
                 c.EffectiveDate <= now &&
                 c.IsActive, cancellationToken).ConfigureAwait(false);

        var errors = new List<string>();
        int applied = 0;

        foreach (var change in dueChanges)
        {
            try
            {
                // Apply to central product price
                if (!change.PricingZoneId.HasValue && !change.StoreId.HasValue)
                {
                    var product = await _productRepository.GetByIdAsync(change.ProductId, cancellationToken).ConfigureAwait(false);
                    if (product != null)
                    {
                        product.SellingPrice = change.NewPrice;
                        if (change.NewCostPrice.HasValue)
                        {
                            product.CostPrice = change.NewCostPrice;
                        }
                        await _productRepository.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
                    }
                }
                // Apply to zone price
                else if (change.PricingZoneId.HasValue)
                {
                    await SetZonePriceAsync(new CreateZonePriceDto
                    {
                        PricingZoneId = change.PricingZoneId.Value,
                        ProductId = change.ProductId,
                        Price = change.NewPrice,
                        CostPrice = change.NewCostPrice,
                        EffectiveFrom = DateTime.UtcNow,
                        EffectiveTo = change.ExpiryDate,
                        Reason = change.Reason
                    }, appliedByUserId, cancellationToken).ConfigureAwait(false);
                }
                // Apply to store override
                else if (change.StoreId.HasValue)
                {
                    await SetOverrideAsync(new CreateStoreProductOverrideDto
                    {
                        StoreId = change.StoreId.Value,
                        ProductId = change.ProductId,
                        OverridePrice = change.NewPrice,
                        OverrideCost = change.NewCostPrice,
                        IsAvailable = true,
                        OverrideReason = change.Reason
                    }, appliedByUserId, cancellationToken).ConfigureAwait(false);
                }

                change.Status = PriceChangeStatus.Applied;
                change.AppliedAt = DateTime.UtcNow;
                change.AppliedByUserId = appliedByUserId;
                change.WasAutoApplied = true;

                await _priceChangeRepository.UpdateAsync(change, cancellationToken).ConfigureAwait(false);
                applied++;
            }
            catch (Exception ex)
            {
                change.Status = PriceChangeStatus.Failed;
                await _priceChangeRepository.UpdateAsync(change, cancellationToken).ConfigureAwait(false);

                errors.Add($"Failed to apply price change {change.Id}: {ex.Message}");
                _logger.LogError(ex, "Failed to apply scheduled price change {Id}", change.Id);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (errors.Any())
        {
            return new PriceChangeApplicationResult
            {
                IsSuccess = false,
                ErrorMessage = "Some price changes failed to apply.",
                TotalChanges = dueChanges.Count(),
                AppliedChanges = applied,
                FailedChanges = errors.Count,
                Errors = errors,
                ProcessedAt = DateTime.UtcNow
            };
        }

        _logger.LogInformation("Applied {Count} scheduled price changes", applied);
        return PriceChangeApplicationResult.Success(dueChanges.Count(), applied);
    }

    /// <inheritdoc />
    public async Task<ProductPricingSummaryDto?> GetProductPricingSummaryAsync(int productId, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken).ConfigureAwait(false);
        if (product == null) return null;

        var zonePrices = await GetProductZonePricesAsync(productId, cancellationToken).ConfigureAwait(false);
        var scheduledChanges = await GetProductScheduledChangesAsync(productId, cancellationToken).ConfigureAwait(false);

        return new ProductPricingSummaryDto
        {
            ProductId = product.Id,
            ProductCode = product.Code,
            ProductName = product.Name,
            CentralPrice = product.SellingPrice,
            CentralCost = product.CostPrice,
            ZonePrices = zonePrices.ToList(),
            ScheduledChanges = scheduledChanges.ToList()
        };
    }

    // ================== Additional Private Helpers ==================

    private static PricingZoneDto MapPricingZoneToDto(PricingZone zone, int storeCount)
    {
        return new PricingZoneDto
        {
            Id = zone.Id,
            ZoneCode = zone.ZoneCode,
            Name = zone.Name,
            Description = zone.Description,
            CurrencyCode = zone.CurrencyCode,
            DefaultTaxRate = zone.DefaultTaxRate,
            IsDefault = zone.IsDefault,
            DisplayOrder = zone.DisplayOrder,
            StoreCount = storeCount,
            IsActive = zone.IsActive
        };
    }

    private static ZonePriceDto MapZonePriceToDto(ZonePrice zp, PricingZone? zone, Product? product)
    {
        return new ZonePriceDto
        {
            Id = zp.Id,
            PricingZoneId = zp.PricingZoneId,
            ZoneName = zone?.Name ?? "Unknown",
            ProductId = zp.ProductId,
            ProductCode = product?.Code ?? "",
            ProductName = product?.Name ?? "Unknown",
            CentralPrice = product?.SellingPrice ?? 0,
            Price = zp.Price,
            CostPrice = zp.CostPrice,
            MinimumPrice = zp.MinimumPrice,
            EffectiveFrom = zp.EffectiveFrom,
            EffectiveTo = zp.EffectiveTo,
            Reason = zp.Reason,
            IsCurrentlyEffective = zp.IsCurrentlyEffective
        };
    }

    private async Task<ScheduledPriceChangeDto> MapScheduledPriceChangeToDto(ScheduledPriceChange change, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(change.ProductId, cancellationToken).ConfigureAwait(false);
        var zone = change.PricingZoneId.HasValue
            ? await _pricingZoneRepository.GetByIdAsync(change.PricingZoneId.Value, cancellationToken).ConfigureAwait(false)
            : null;
        var store = change.StoreId.HasValue
            ? await _storeRepository.GetByIdAsync(change.StoreId.Value, cancellationToken).ConfigureAwait(false)
            : null;

        return new ScheduledPriceChangeDto
        {
            Id = change.Id,
            ProductId = change.ProductId,
            ProductCode = product?.Code ?? "",
            ProductName = product?.Name ?? "Unknown",
            PricingZoneId = change.PricingZoneId,
            ZoneName = zone?.Name,
            StoreId = change.StoreId,
            StoreName = store?.Name,
            OldPrice = change.OldPrice,
            NewPrice = change.NewPrice,
            NewCostPrice = change.NewCostPrice,
            EffectiveDate = change.EffectiveDate,
            ExpiryDate = change.ExpiryDate,
            Status = change.Status.ToString(),
            AppliedAt = change.AppliedAt,
            Reason = change.Reason,
            Notes = change.Notes
        };
    }
}
