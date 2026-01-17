using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for central promotion management and deployment operations.
/// </summary>
public class CentralPromotionService : ICentralPromotionService
{
    private readonly IRepository<CentralPromotion> _promotionRepository;
    private readonly IRepository<PromotionProduct> _promotionProductRepository;
    private readonly IRepository<PromotionCategory> _promotionCategoryRepository;
    private readonly IRepository<PromotionDeployment> _deploymentRepository;
    private readonly IRepository<DeploymentStore> _deploymentStoreRepository;
    private readonly IRepository<DeploymentZone> _deploymentZoneRepository;
    private readonly IRepository<PromotionRedemption> _redemptionRepository;
    private readonly IRepository<Store> _storeRepository;
    private readonly IRepository<PricingZone> _zoneRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<Category> _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CentralPromotionService> _logger;

    public CentralPromotionService(
        IRepository<CentralPromotion> promotionRepository,
        IRepository<PromotionProduct> promotionProductRepository,
        IRepository<PromotionCategory> promotionCategoryRepository,
        IRepository<PromotionDeployment> deploymentRepository,
        IRepository<DeploymentStore> deploymentStoreRepository,
        IRepository<DeploymentZone> deploymentZoneRepository,
        IRepository<PromotionRedemption> redemptionRepository,
        IRepository<Store> storeRepository,
        IRepository<PricingZone> zoneRepository,
        IRepository<Product> productRepository,
        IRepository<Category> categoryRepository,
        IUnitOfWork unitOfWork,
        ILogger<CentralPromotionService> logger)
    {
        _promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
        _promotionProductRepository = promotionProductRepository ?? throw new ArgumentNullException(nameof(promotionProductRepository));
        _promotionCategoryRepository = promotionCategoryRepository ?? throw new ArgumentNullException(nameof(promotionCategoryRepository));
        _deploymentRepository = deploymentRepository ?? throw new ArgumentNullException(nameof(deploymentRepository));
        _deploymentStoreRepository = deploymentStoreRepository ?? throw new ArgumentNullException(nameof(deploymentStoreRepository));
        _deploymentZoneRepository = deploymentZoneRepository ?? throw new ArgumentNullException(nameof(deploymentZoneRepository));
        _redemptionRepository = redemptionRepository ?? throw new ArgumentNullException(nameof(redemptionRepository));
        _storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
        _zoneRepository = zoneRepository ?? throw new ArgumentNullException(nameof(zoneRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Promotion Management

    public async Task<IEnumerable<CentralPromotionDto>> GetAllPromotionsAsync(PromotionQueryDto? query = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all promotions with query filter");

        var promotions = await _promotionRepository.FindAsync(p => p.IsActive, cancellationToken);

        if (query != null)
        {
            if (query.Status.HasValue)
                promotions = promotions.Where(p => p.Status == query.Status.Value);
            if (query.Type.HasValue)
                promotions = promotions.Where(p => p.Type == query.Type.Value);
            if (query.StartDateFrom.HasValue)
                promotions = promotions.Where(p => p.StartDate >= query.StartDateFrom.Value);
            if (query.StartDateTo.HasValue)
                promotions = promotions.Where(p => p.StartDate <= query.StartDateTo.Value);
            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
                promotions = promotions.Where(p =>
                    p.Name.Contains(query.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.PromotionCode.Contains(query.SearchTerm, StringComparison.OrdinalIgnoreCase));
        }

        var dtos = new List<CentralPromotionDto>();
        foreach (var promo in promotions)
        {
            dtos.Add(await MapToPromotionDtoAsync(promo, cancellationToken));
        }

        return dtos.OrderByDescending(p => p.StartDate);
    }

    public async Task<CentralPromotionDto?> GetPromotionByIdAsync(int promotionId, CancellationToken cancellationToken = default)
    {
        var promotion = await _promotionRepository.GetByIdAsync(promotionId, cancellationToken);
        if (promotion == null || !promotion.IsActive)
            return null;

        return await MapToPromotionDtoAsync(promotion, cancellationToken);
    }

    public async Task<CentralPromotionDto?> GetPromotionByCodeAsync(string promotionCode, CancellationToken cancellationToken = default)
    {
        var promotions = await _promotionRepository.FindAsync(
            p => p.PromotionCode == promotionCode && p.IsActive, cancellationToken);
        var promotion = promotions.FirstOrDefault();

        if (promotion == null)
            return null;

        return await MapToPromotionDtoAsync(promotion, cancellationToken);
    }

    public async Task<CentralPromotionDto?> CreatePromotionAsync(CreatePromotionDto dto, int createdByUserId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating promotion: {PromotionCode}", dto.PromotionCode);

        // Check for duplicate code
        var existing = await _promotionRepository.FindAsync(
            p => p.PromotionCode == dto.PromotionCode && p.IsActive, cancellationToken);
        if (existing.Any())
        {
            _logger.LogWarning("Promotion code already exists: {PromotionCode}", dto.PromotionCode);
            return null;
        }

        var promotion = new CentralPromotion
        {
            PromotionCode = dto.PromotionCode,
            Name = dto.Name,
            Description = dto.Description,
            InternalNotes = dto.InternalNotes,
            Type = dto.Type,
            DiscountAmount = dto.DiscountAmount,
            DiscountPercent = dto.DiscountPercent,
            OfferPrice = dto.OfferPrice,
            MinimumPurchase = dto.MinimumPurchase,
            MinQuantity = dto.MinQuantity,
            MaxQuantityPerTransaction = dto.MaxQuantityPerTransaction,
            MaxTotalRedemptions = dto.MaxTotalRedemptions,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            ValidDaysOfWeek = dto.ValidDaysOfWeek,
            ValidFromTime = dto.ValidFromTime,
            ValidToTime = dto.ValidToTime,
            RequiresCouponCode = dto.RequiresCouponCode,
            CouponCode = dto.CouponCode,
            IsCombinableWithOtherPromotions = dto.IsCombinableWithOtherPromotions,
            Priority = dto.Priority,
            Status = PromotionStatus.Draft,
            IsCentrallyManaged = true,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        await _promotionRepository.AddAsync(promotion, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Add products if specified
        if (dto.ProductIds?.Any() == true)
        {
            foreach (var productId in dto.ProductIds)
            {
                var pp = new PromotionProduct
                {
                    PromotionId = promotion.Id,
                    ProductId = productId,
                    IsQualifyingProduct = true,
                    CreatedAt = DateTime.UtcNow
                };
                await _promotionProductRepository.AddAsync(pp, cancellationToken);
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // Add categories if specified
        if (dto.CategoryIds?.Any() == true)
        {
            foreach (var categoryId in dto.CategoryIds)
            {
                var pc = new PromotionCategory
                {
                    PromotionId = promotion.Id,
                    CategoryId = categoryId,
                    CreatedAt = DateTime.UtcNow
                };
                await _promotionCategoryRepository.AddAsync(pc, cancellationToken);
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Created promotion {PromotionId}: {PromotionCode}", promotion.Id, promotion.PromotionCode);
        return await MapToPromotionDtoAsync(promotion, cancellationToken);
    }

    public async Task<bool> UpdatePromotionAsync(int promotionId, CreatePromotionDto dto, int updatedByUserId, CancellationToken cancellationToken = default)
    {
        var promotion = await _promotionRepository.GetByIdAsync(promotionId, cancellationToken);
        if (promotion == null || !promotion.IsActive)
            return false;

        promotion.Name = dto.Name;
        promotion.Description = dto.Description;
        promotion.InternalNotes = dto.InternalNotes;
        promotion.Type = dto.Type;
        promotion.DiscountAmount = dto.DiscountAmount;
        promotion.DiscountPercent = dto.DiscountPercent;
        promotion.OfferPrice = dto.OfferPrice;
        promotion.MinimumPurchase = dto.MinimumPurchase;
        promotion.MinQuantity = dto.MinQuantity;
        promotion.MaxQuantityPerTransaction = dto.MaxQuantityPerTransaction;
        promotion.MaxTotalRedemptions = dto.MaxTotalRedemptions;
        promotion.StartDate = dto.StartDate;
        promotion.EndDate = dto.EndDate;
        promotion.ValidDaysOfWeek = dto.ValidDaysOfWeek;
        promotion.ValidFromTime = dto.ValidFromTime;
        promotion.ValidToTime = dto.ValidToTime;
        promotion.RequiresCouponCode = dto.RequiresCouponCode;
        promotion.CouponCode = dto.CouponCode;
        promotion.IsCombinableWithOtherPromotions = dto.IsCombinableWithOtherPromotions;
        promotion.Priority = dto.Priority;
        promotion.UpdatedAt = DateTime.UtcNow;
        promotion.UpdatedByUserId = updatedByUserId;

        await _promotionRepository.UpdateAsync(promotion, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated promotion {PromotionId}", promotionId);
        return true;
    }

    public async Task<bool> ActivatePromotionAsync(int promotionId, int activatedByUserId, CancellationToken cancellationToken = default)
    {
        var promotion = await _promotionRepository.GetByIdAsync(promotionId, cancellationToken);
        if (promotion == null || !promotion.IsActive)
            return false;

        promotion.Status = PromotionStatus.Active;
        promotion.UpdatedAt = DateTime.UtcNow;
        promotion.UpdatedByUserId = activatedByUserId;

        await _promotionRepository.UpdateAsync(promotion, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Activated promotion {PromotionId}", promotionId);
        return true;
    }

    public async Task<bool> PausePromotionAsync(int promotionId, int pausedByUserId, CancellationToken cancellationToken = default)
    {
        var promotion = await _promotionRepository.GetByIdAsync(promotionId, cancellationToken);
        if (promotion == null || !promotion.IsActive)
            return false;

        promotion.Status = PromotionStatus.Paused;
        promotion.UpdatedAt = DateTime.UtcNow;
        promotion.UpdatedByUserId = pausedByUserId;

        await _promotionRepository.UpdateAsync(promotion, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Paused promotion {PromotionId}", promotionId);
        return true;
    }

    public async Task<bool> CancelPromotionAsync(int promotionId, int cancelledByUserId, CancellationToken cancellationToken = default)
    {
        var promotion = await _promotionRepository.GetByIdAsync(promotionId, cancellationToken);
        if (promotion == null || !promotion.IsActive)
            return false;

        promotion.Status = PromotionStatus.Cancelled;
        promotion.IsActive = false;
        promotion.UpdatedAt = DateTime.UtcNow;
        promotion.UpdatedByUserId = cancelledByUserId;

        await _promotionRepository.UpdateAsync(promotion, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Cancelled promotion {PromotionId}", promotionId);
        return true;
    }

    #endregion

    #region Promotion Products & Categories

    public async Task<bool> AddProductsToPromotionAsync(int promotionId, List<int> productIds, int addedByUserId, CancellationToken cancellationToken = default)
    {
        var promotion = await _promotionRepository.GetByIdAsync(promotionId, cancellationToken);
        if (promotion == null || !promotion.IsActive)
            return false;

        var existingProducts = await _promotionProductRepository.FindAsync(
            pp => pp.PromotionId == promotionId && pp.IsActive, cancellationToken);
        var existingIds = existingProducts.Select(pp => pp.ProductId).ToHashSet();

        foreach (var productId in productIds.Where(id => !existingIds.Contains(id)))
        {
            var pp = new PromotionProduct
            {
                PromotionId = promotionId,
                ProductId = productId,
                IsQualifyingProduct = true,
                CreatedAt = DateTime.UtcNow
            };
            await _promotionProductRepository.AddAsync(pp, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RemoveProductsFromPromotionAsync(int promotionId, List<int> productIds, int removedByUserId, CancellationToken cancellationToken = default)
    {
        var products = await _promotionProductRepository.FindAsync(
            pp => pp.PromotionId == promotionId && productIds.Contains(pp.ProductId) && pp.IsActive, cancellationToken);

        foreach (var pp in products)
        {
            pp.IsActive = false;
            pp.UpdatedAt = DateTime.UtcNow;
            await _promotionProductRepository.UpdateAsync(pp, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> AddCategoriesToPromotionAsync(int promotionId, List<int> categoryIds, int addedByUserId, CancellationToken cancellationToken = default)
    {
        var promotion = await _promotionRepository.GetByIdAsync(promotionId, cancellationToken);
        if (promotion == null || !promotion.IsActive)
            return false;

        var existingCategories = await _promotionCategoryRepository.FindAsync(
            pc => pc.PromotionId == promotionId && pc.IsActive, cancellationToken);
        var existingIds = existingCategories.Select(pc => pc.CategoryId).ToHashSet();

        foreach (var categoryId in categoryIds.Where(id => !existingIds.Contains(id)))
        {
            var pc = new PromotionCategory
            {
                PromotionId = promotionId,
                CategoryId = categoryId,
                CreatedAt = DateTime.UtcNow
            };
            await _promotionCategoryRepository.AddAsync(pc, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RemoveCategoriesFromPromotionAsync(int promotionId, List<int> categoryIds, int removedByUserId, CancellationToken cancellationToken = default)
    {
        var categories = await _promotionCategoryRepository.FindAsync(
            pc => pc.PromotionId == promotionId && categoryIds.Contains(pc.CategoryId) && pc.IsActive, cancellationToken);

        foreach (var pc in categories)
        {
            pc.IsActive = false;
            pc.UpdatedAt = DateTime.UtcNow;
            await _promotionCategoryRepository.UpdateAsync(pc, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    #endregion

    #region Deployment Management

    public async Task<DeploymentResult> DeployPromotionAsync(DeployPromotionDto dto, int deployedByUserId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deploying promotion {PromotionId} with scope {Scope}", dto.PromotionId, dto.Scope);

        var promotion = await _promotionRepository.GetByIdAsync(dto.PromotionId, cancellationToken);
        if (promotion == null || !promotion.IsActive)
            return DeploymentResult.Failure("Promotion not found");

        // Get target stores based on scope
        var targetStores = new List<Store>();
        switch (dto.Scope)
        {
            case DeploymentScope.AllStores:
                targetStores = (await _storeRepository.FindAsync(s => s.IsActive && s.ReceivesCentralUpdates, cancellationToken)).ToList();
                break;

            case DeploymentScope.ByZone:
                if (dto.ZoneIds?.Any() != true)
                    return DeploymentResult.Failure("No zones specified for zone deployment");
                targetStores = (await _storeRepository.FindAsync(
                    s => s.IsActive && s.ReceivesCentralUpdates && s.PricingZoneId.HasValue && dto.ZoneIds.Contains(s.PricingZoneId.Value), cancellationToken)).ToList();
                break;

            case DeploymentScope.IndividualStores:
                if (dto.StoreIds?.Any() != true)
                    return DeploymentResult.Failure("No stores specified for individual deployment");
                targetStores = (await _storeRepository.FindAsync(
                    s => s.IsActive && dto.StoreIds.Contains(s.Id), cancellationToken)).ToList();
                break;
        }

        if (!targetStores.Any())
            return DeploymentResult.Failure("No valid stores found for deployment");

        // Create deployment record
        var deployment = new PromotionDeployment
        {
            PromotionId = dto.PromotionId,
            Scope = dto.Scope,
            DeployedAt = DateTime.UtcNow,
            Status = DeploymentStatus.InProgress,
            OverwriteExisting = dto.OverwriteExisting,
            Notes = dto.Notes,
            DeployedByUserId = deployedByUserId,
            CreatedAt = DateTime.UtcNow
        };

        await _deploymentRepository.AddAsync(deployment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Add zones if applicable
        if (dto.Scope == DeploymentScope.ByZone && dto.ZoneIds?.Any() == true)
        {
            foreach (var zoneId in dto.ZoneIds)
            {
                var dz = new DeploymentZone
                {
                    DeploymentId = deployment.Id,
                    PricingZoneId = zoneId,
                    CreatedAt = DateTime.UtcNow
                };
                await _deploymentZoneRepository.AddAsync(dz, cancellationToken);
            }
        }

        // Create deployment store records
        foreach (var store in targetStores)
        {
            var ds = new DeploymentStore
            {
                DeploymentId = deployment.Id,
                StoreId = store.Id,
                Status = DeploymentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            await _deploymentStoreRepository.AddAsync(ds, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Mark deployment as completed (in real scenario, this would be async)
        deployment.Status = DeploymentStatus.Completed;
        deployment.CompletedAt = DateTime.UtcNow;
        deployment.StoresDeployedCount = targetStores.Count;
        await _deploymentRepository.UpdateAsync(deployment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deployed promotion {PromotionId} to {StoreCount} stores", dto.PromotionId, targetStores.Count);
        return DeploymentResult.Success(deployment.Id, targetStores.Count);
    }

    public async Task<IEnumerable<PromotionDeploymentDto>> GetPromotionDeploymentsAsync(int promotionId, CancellationToken cancellationToken = default)
    {
        var deployments = await _deploymentRepository.FindAsync(
            d => d.PromotionId == promotionId && d.IsActive, cancellationToken);

        var dtos = new List<PromotionDeploymentDto>();
        foreach (var d in deployments.OrderByDescending(x => x.DeployedAt))
        {
            dtos.Add(await MapToDeploymentDtoAsync(d, cancellationToken));
        }

        return dtos;
    }

    public async Task<PromotionDeploymentDto?> GetDeploymentByIdAsync(int deploymentId, CancellationToken cancellationToken = default)
    {
        var deployment = await _deploymentRepository.GetByIdAsync(deploymentId, cancellationToken);
        if (deployment == null || !deployment.IsActive)
            return null;

        return await MapToDeploymentDtoAsync(deployment, cancellationToken);
    }

    public async Task<IEnumerable<PromotionDeploymentDto>> GetPendingDeploymentsAsync(CancellationToken cancellationToken = default)
    {
        var deployments = await _deploymentRepository.FindAsync(
            d => d.IsActive && (d.Status == DeploymentStatus.Pending || d.Status == DeploymentStatus.InProgress), cancellationToken);

        var dtos = new List<PromotionDeploymentDto>();
        foreach (var d in deployments)
        {
            dtos.Add(await MapToDeploymentDtoAsync(d, cancellationToken));
        }

        return dtos;
    }

    public async Task<bool> UpdateDeploymentStoreStatusAsync(int deploymentId, int storeId, DeploymentStatus status, string? errorMessage = null, CancellationToken cancellationToken = default)
    {
        var stores = await _deploymentStoreRepository.FindAsync(
            ds => ds.DeploymentId == deploymentId && ds.StoreId == storeId && ds.IsActive, cancellationToken);
        var deploymentStore = stores.FirstOrDefault();

        if (deploymentStore == null)
            return false;

        deploymentStore.Status = status;
        deploymentStore.SyncedAt = status == DeploymentStatus.Completed ? DateTime.UtcNow : null;
        deploymentStore.ErrorMessage = errorMessage;
        deploymentStore.UpdatedAt = DateTime.UtcNow;

        await _deploymentStoreRepository.UpdateAsync(deploymentStore, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> RetryDeploymentForStoreAsync(int deploymentId, int storeId, int retriedByUserId, CancellationToken cancellationToken = default)
    {
        var stores = await _deploymentStoreRepository.FindAsync(
            ds => ds.DeploymentId == deploymentId && ds.StoreId == storeId && ds.IsActive, cancellationToken);
        var deploymentStore = stores.FirstOrDefault();

        if (deploymentStore == null)
            return false;

        deploymentStore.Status = DeploymentStatus.Pending;
        deploymentStore.RetryCount++;
        deploymentStore.ErrorMessage = null;
        deploymentStore.UpdatedAt = DateTime.UtcNow;

        await _deploymentStoreRepository.UpdateAsync(deploymentStore, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> RollbackDeploymentAsync(int deploymentId, int rolledBackByUserId, CancellationToken cancellationToken = default)
    {
        var deployment = await _deploymentRepository.GetByIdAsync(deploymentId, cancellationToken);
        if (deployment == null || !deployment.IsActive)
            return false;

        deployment.Status = DeploymentStatus.RolledBack;
        deployment.UpdatedAt = DateTime.UtcNow;

        await _deploymentRepository.UpdateAsync(deployment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Rolled back deployment {DeploymentId}", deploymentId);
        return true;
    }

    #endregion

    #region Redemption Management

    public async Task<PromotionRedemptionDto?> RecordRedemptionAsync(RecordRedemptionDto dto, int processedByUserId, CancellationToken cancellationToken = default)
    {
        var promotion = await _promotionRepository.GetByIdAsync(dto.PromotionId, cancellationToken);
        if (promotion == null || !promotion.IsActive || !promotion.IsCurrentlyActive)
            return null;

        var redemption = new PromotionRedemption
        {
            PromotionId = dto.PromotionId,
            StoreId = dto.StoreId,
            ReceiptId = dto.ReceiptId,
            ReceiptItemId = dto.ReceiptItemId,
            OriginalAmount = dto.OriginalAmount,
            DiscountGiven = dto.DiscountGiven,
            FinalAmount = dto.OriginalAmount - dto.DiscountGiven,
            QuantityApplied = dto.QuantityApplied,
            RedeemedAt = DateTime.UtcNow,
            CouponCodeUsed = dto.CouponCodeUsed,
            LoyaltyMemberId = dto.LoyaltyMemberId,
            ProcessedByUserId = processedByUserId,
            CreatedAt = DateTime.UtcNow
        };

        await _redemptionRepository.AddAsync(redemption, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapToRedemptionDtoAsync(redemption, cancellationToken);
    }

    public async Task<bool> VoidRedemptionAsync(int redemptionId, string reason, int voidedByUserId, CancellationToken cancellationToken = default)
    {
        var redemption = await _redemptionRepository.GetByIdAsync(redemptionId, cancellationToken);
        if (redemption == null || !redemption.IsActive || redemption.IsVoided)
            return false;

        redemption.IsVoided = true;
        redemption.VoidedAt = DateTime.UtcNow;
        redemption.VoidedByUserId = voidedByUserId;
        redemption.VoidReason = reason;
        redemption.UpdatedAt = DateTime.UtcNow;

        await _redemptionRepository.UpdateAsync(redemption, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<IEnumerable<PromotionRedemptionDto>> GetPromotionRedemptionsAsync(int promotionId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var redemptions = await _redemptionRepository.FindAsync(
            r => r.PromotionId == promotionId && r.IsActive && !r.IsVoided, cancellationToken);

        if (fromDate.HasValue)
            redemptions = redemptions.Where(r => r.RedeemedAt >= fromDate.Value);
        if (toDate.HasValue)
            redemptions = redemptions.Where(r => r.RedeemedAt <= toDate.Value);

        var dtos = new List<PromotionRedemptionDto>();
        foreach (var r in redemptions.OrderByDescending(x => x.RedeemedAt))
        {
            dtos.Add(await MapToRedemptionDtoAsync(r, cancellationToken));
        }

        return dtos;
    }

    public async Task<IEnumerable<PromotionRedemptionDto>> GetStoreRedemptionsAsync(int storeId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var redemptions = await _redemptionRepository.FindAsync(
            r => r.StoreId == storeId && r.IsActive && !r.IsVoided, cancellationToken);

        if (fromDate.HasValue)
            redemptions = redemptions.Where(r => r.RedeemedAt >= fromDate.Value);
        if (toDate.HasValue)
            redemptions = redemptions.Where(r => r.RedeemedAt <= toDate.Value);

        var dtos = new List<PromotionRedemptionDto>();
        foreach (var r in redemptions.OrderByDescending(x => x.RedeemedAt))
        {
            dtos.Add(await MapToRedemptionDtoAsync(r, cancellationToken));
        }

        return dtos;
    }

    public async Task<int> GetRedemptionCountAsync(int promotionId, CancellationToken cancellationToken = default)
    {
        var redemptions = await _redemptionRepository.FindAsync(
            r => r.PromotionId == promotionId && r.IsActive && !r.IsVoided, cancellationToken);
        return redemptions.Count();
    }

    #endregion

    #region Dashboard & Reporting

    public async Task<PromotionDashboardDto?> GetPromotionDashboardAsync(int promotionId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var promotion = await _promotionRepository.GetByIdAsync(promotionId, cancellationToken);
        if (promotion == null)
            return null;

        var storeBreakdown = await GetRedemptionsByStoreAsync(promotionId, fromDate, toDate, cancellationToken);
        var deployments = await _deploymentRepository.FindAsync(d => d.PromotionId == promotionId && d.IsActive, cancellationToken);

        var totalRedemptions = storeBreakdown.Sum(s => s.RedemptionCount);
        var totalDiscount = storeBreakdown.Sum(s => s.TotalDiscountGiven);

        return new PromotionDashboardDto
        {
            PromotionId = promotionId,
            PromotionCode = promotion.PromotionCode,
            PromotionName = promotion.Name,
            Status = promotion.ComputedStatus,
            StartDate = promotion.StartDate,
            EndDate = promotion.EndDate,
            TotalStoresDeployed = deployments.Sum(d => d.StoresDeployedCount),
            TotalRedemptions = totalRedemptions,
            TotalDiscountGiven = totalDiscount,
            AverageDiscountPerTransaction = totalRedemptions > 0 ? totalDiscount / totalRedemptions : 0,
            StoreBreakdown = storeBreakdown.ToList()
        };
    }

    public async Task<IEnumerable<StoreRedemptionSummaryDto>> GetRedemptionsByStoreAsync(int promotionId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var redemptions = await _redemptionRepository.FindAsync(
            r => r.PromotionId == promotionId && r.IsActive && !r.IsVoided, cancellationToken);

        if (fromDate.HasValue)
            redemptions = redemptions.Where(r => r.RedeemedAt >= fromDate.Value);
        if (toDate.HasValue)
            redemptions = redemptions.Where(r => r.RedeemedAt <= toDate.Value);

        var grouped = redemptions.GroupBy(r => r.StoreId);
        var summaries = new List<StoreRedemptionSummaryDto>();

        foreach (var group in grouped)
        {
            var store = await _storeRepository.GetByIdAsync(group.Key, cancellationToken);
            if (store == null) continue;

            summaries.Add(new StoreRedemptionSummaryDto
            {
                StoreId = group.Key,
                StoreName = store.Name,
                StoreCode = store.StoreCode,
                RedemptionCount = group.Count(),
                TotalDiscountGiven = group.Sum(r => r.DiscountGiven),
                TotalOriginalAmount = group.Sum(r => r.OriginalAmount),
                TotalFinalAmount = group.Sum(r => r.FinalAmount),
                TotalQuantity = group.Sum(r => r.QuantityApplied),
                LastRedemption = group.Max(r => r.RedeemedAt)
            });
        }

        return summaries.OrderByDescending(s => s.RedemptionCount);
    }

    public async Task<IEnumerable<StoreActivePromotionDto>> GetActivePromotionsForStoreAsync(int storeId, CancellationToken cancellationToken = default)
    {
        var store = await _storeRepository.GetByIdAsync(storeId, cancellationToken);
        if (store == null)
            return Enumerable.Empty<StoreActivePromotionDto>();

        // Get all deployments for this store
        var storeDeployments = await _deploymentStoreRepository.FindAsync(
            ds => ds.StoreId == storeId && ds.IsActive && ds.Status == DeploymentStatus.Completed, cancellationToken);

        var deploymentIds = storeDeployments.Select(ds => ds.DeploymentId).ToList();
        var deployments = await _deploymentRepository.FindAsync(
            d => deploymentIds.Contains(d.Id) && d.IsActive, cancellationToken);

        var promotionIds = deployments.Select(d => d.PromotionId).Distinct().ToList();
        var promotions = await _promotionRepository.FindAsync(
            p => promotionIds.Contains(p.Id) && p.IsActive && p.IsCurrentlyActive, cancellationToken);

        var result = new List<StoreActivePromotionDto>();
        foreach (var promo in promotions)
        {
            var products = await _promotionProductRepository.FindAsync(
                pp => pp.PromotionId == promo.Id && pp.IsActive, cancellationToken);
            var categories = await _promotionCategoryRepository.FindAsync(
                pc => pc.PromotionId == promo.Id && pc.IsActive, cancellationToken);

            result.Add(new StoreActivePromotionDto
            {
                PromotionId = promo.Id,
                PromotionCode = promo.PromotionCode,
                Name = promo.Name,
                Description = promo.Description,
                Type = promo.Type,
                DiscountAmount = promo.DiscountAmount,
                DiscountPercent = promo.DiscountPercent,
                OfferPrice = promo.OfferPrice,
                MinQuantity = promo.MinQuantity,
                StartDate = promo.StartDate,
                EndDate = promo.EndDate,
                RequiresCouponCode = promo.RequiresCouponCode,
                ApplicableProductIds = products.Select(pp => pp.ProductId).ToList(),
                ApplicableCategoryIds = categories.Select(pc => pc.CategoryId).ToList()
            });
        }

        return result.OrderBy(p => p.Priority);
    }

    public async Task<StoreActivePromotionDto?> GetApplicablePromotionAsync(int storeId, int productId, decimal quantity = 1, string? couponCode = null, CancellationToken cancellationToken = default)
    {
        var activePromotions = await GetActivePromotionsForStoreAsync(storeId, cancellationToken);
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
        if (product == null)
            return null;

        foreach (var promo in activePromotions)
        {
            // Check quantity requirement
            if (quantity < promo.MinQuantity)
                continue;

            // Check coupon code if required
            if (promo.RequiresCouponCode && promo.PromotionCode != couponCode)
                continue;

            // Check if product is applicable
            var isApplicable = promo.ApplicableProductIds.Contains(productId) ||
                               (product.CategoryId.HasValue && promo.ApplicableCategoryIds.Contains(product.CategoryId.Value));

            if (isApplicable)
                return promo;
        }

        return null;
    }

    #endregion

    #region Private Helper Methods

    private async Task<CentralPromotionDto> MapToPromotionDtoAsync(CentralPromotion promo, CancellationToken cancellationToken)
    {
        var products = await _promotionProductRepository.FindAsync(pp => pp.PromotionId == promo.Id && pp.IsActive, cancellationToken);
        var categories = await _promotionCategoryRepository.FindAsync(pc => pc.PromotionId == promo.Id && pc.IsActive, cancellationToken);
        var deployments = await _deploymentRepository.FindAsync(d => d.PromotionId == promo.Id && d.IsActive, cancellationToken);
        var redemptions = await _redemptionRepository.FindAsync(r => r.PromotionId == promo.Id && r.IsActive && !r.IsVoided, cancellationToken);

        return new CentralPromotionDto
        {
            Id = promo.Id,
            PromotionCode = promo.PromotionCode,
            Name = promo.Name,
            Description = promo.Description,
            Type = promo.Type,
            DiscountAmount = promo.DiscountAmount,
            DiscountPercent = promo.DiscountPercent,
            OfferPrice = promo.OfferPrice,
            MinimumPurchase = promo.MinimumPurchase,
            MinQuantity = promo.MinQuantity,
            MaxQuantityPerTransaction = promo.MaxQuantityPerTransaction,
            StartDate = promo.StartDate,
            EndDate = promo.EndDate,
            Status = promo.ComputedStatus,
            RequiresCouponCode = promo.RequiresCouponCode,
            CouponCode = promo.CouponCode,
            Priority = promo.Priority,
            IsCentrallyManaged = promo.IsCentrallyManaged,
            ProductCount = products.Count(),
            CategoryCount = categories.Count(),
            DeploymentCount = deployments.Count(),
            TotalRedemptions = redemptions.Count(),
            TotalDiscountGiven = redemptions.Sum(r => r.DiscountGiven),
            IsCurrentlyActive = promo.IsCurrentlyActive
        };
    }

    private async Task<PromotionDeploymentDto> MapToDeploymentDtoAsync(PromotionDeployment deployment, CancellationToken cancellationToken)
    {
        var promotion = await _promotionRepository.GetByIdAsync(deployment.PromotionId, cancellationToken);
        var storeDeployments = await _deploymentStoreRepository.FindAsync(ds => ds.DeploymentId == deployment.Id && ds.IsActive, cancellationToken);

        var storeDetails = new List<DeploymentStoreDto>();
        foreach (var ds in storeDeployments)
        {
            var store = await _storeRepository.GetByIdAsync(ds.StoreId, cancellationToken);
            if (store == null) continue;

            storeDetails.Add(new DeploymentStoreDto
            {
                StoreId = ds.StoreId,
                StoreName = store.Name,
                StoreCode = store.StoreCode,
                Status = ds.Status,
                SyncedAt = ds.SyncedAt,
                ErrorMessage = ds.ErrorMessage,
                RetryCount = ds.RetryCount
            });
        }

        return new PromotionDeploymentDto
        {
            Id = deployment.Id,
            PromotionId = deployment.PromotionId,
            PromotionCode = promotion?.PromotionCode ?? "",
            PromotionName = promotion?.Name ?? "",
            Scope = deployment.Scope,
            DeployedAt = deployment.DeployedAt,
            CompletedAt = deployment.CompletedAt,
            Status = deployment.Status,
            StoresDeployedCount = deployment.StoresDeployedCount,
            StoresFailedCount = deployment.StoresFailedCount,
            TotalStores = storeDeployments.Count(),
            ErrorMessage = deployment.ErrorMessage,
            Notes = deployment.Notes,
            StoreDetails = storeDetails
        };
    }

    private async Task<PromotionRedemptionDto> MapToRedemptionDtoAsync(PromotionRedemption redemption, CancellationToken cancellationToken)
    {
        var promotion = await _promotionRepository.GetByIdAsync(redemption.PromotionId, cancellationToken);
        var store = await _storeRepository.GetByIdAsync(redemption.StoreId, cancellationToken);

        return new PromotionRedemptionDto
        {
            Id = redemption.Id,
            PromotionId = redemption.PromotionId,
            PromotionCode = promotion?.PromotionCode ?? "",
            PromotionName = promotion?.Name ?? "",
            StoreId = redemption.StoreId,
            StoreName = store?.Name ?? "",
            ReceiptId = redemption.ReceiptId,
            OriginalAmount = redemption.OriginalAmount,
            DiscountGiven = redemption.DiscountGiven,
            FinalAmount = redemption.FinalAmount,
            QuantityApplied = redemption.QuantityApplied,
            RedeemedAt = redemption.RedeemedAt,
            CouponCodeUsed = redemption.CouponCodeUsed,
            IsVoided = redemption.IsVoided
        };
    }

    #endregion
}
