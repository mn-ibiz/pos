using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for expiry validation and sale blocking operations.
/// </summary>
public class ExpiryValidationService : IExpiryValidationService
{
    private readonly IRepository<ProductBatch> _batchRepository;
    private readonly IRepository<ProductBatchConfiguration> _batchConfigRepository;
    private readonly IRepository<ExpirySaleBlock> _saleBlockRepository;
    private readonly IRepository<CategoryExpirySettings> _categorySettingsRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<Category> _categoryRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Store> _storeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ExpiryValidationService(
        IRepository<ProductBatch> batchRepository,
        IRepository<ProductBatchConfiguration> batchConfigRepository,
        IRepository<ExpirySaleBlock> saleBlockRepository,
        IRepository<CategoryExpirySettings> categorySettingsRepository,
        IRepository<Product> productRepository,
        IRepository<Category> categoryRepository,
        IRepository<User> userRepository,
        IRepository<Store> storeRepository,
        IUnitOfWork unitOfWork)
    {
        _batchRepository = batchRepository ?? throw new ArgumentNullException(nameof(batchRepository));
        _batchConfigRepository = batchConfigRepository ?? throw new ArgumentNullException(nameof(batchConfigRepository));
        _saleBlockRepository = saleBlockRepository ?? throw new ArgumentNullException(nameof(saleBlockRepository));
        _categorySettingsRepository = categorySettingsRepository ?? throw new ArgumentNullException(nameof(categorySettingsRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    #region Expiry Validation

    public async Task<ExpiredItemCheckDto> ValidateProductForSaleAsync(int productId, int storeId, int quantity = 1)
    {
        var product = await _productRepository.GetByIdAsync(productId);
        if (product == null)
        {
            return new ExpiredItemCheckDto
            {
                ProductId = productId,
                StoreId = storeId,
                BlockingEnabled = false,
                BlockReason = "Product not found"
            };
        }

        // Get effective settings for this product
        var effectiveSettings = await GetEffectiveSettingsAsync(productId);

        // If expiry tracking not required, allow sale
        if (!effectiveSettings.RequiresExpiryTracking)
        {
            return new ExpiredItemCheckDto
            {
                ProductId = productId,
                ProductName = product.Name,
                StoreId = storeId,
                IsExpired = false,
                BlockingEnabled = false
            };
        }

        // Get batches for this product at this store
        var batches = await _batchRepository.FindAsync(b =>
            b.ProductId == productId &&
            b.StoreId == storeId &&
            b.IsActive &&
            b.CurrentQuantity > 0);

        var batchList = batches.ToList();

        // Find expired batches
        var now = DateTime.UtcNow;
        var expiredBatches = batchList
            .Where(b => b.ExpiryDate.HasValue && b.ExpiryDate.Value < now)
            .OrderBy(b => b.ExpiryDate)
            .ToList();

        // Find earliest expiring batch
        var earliestExpiry = batchList
            .Where(b => b.ExpiryDate.HasValue)
            .OrderBy(b => b.ExpiryDate)
            .FirstOrDefault();

        var hasExpiredBatches = expiredBatches.Any();
        var daysExpired = hasExpiredBatches
            ? (int)(now - expiredBatches.First().ExpiryDate!.Value).TotalDays
            : 0;

        var daysUntilExpiry = earliestExpiry?.ExpiryDate != null
            ? (int)(earliestExpiry.ExpiryDate.Value - now).TotalDays
            : (int?)null;

        var result = new ExpiredItemCheckDto
        {
            ProductId = productId,
            ProductName = product.Name,
            StoreId = storeId,
            IsExpired = hasExpiredBatches,
            HasNearExpiryItems = daysUntilExpiry.HasValue && daysUntilExpiry <= effectiveSettings.WarningDays,
            EarliestExpiry = earliestExpiry?.ExpiryDate,
            DaysUntilEarliestExpiry = daysUntilExpiry,
            DaysExpired = daysExpired > 0 ? daysExpired : null,
            BlockingEnabled = effectiveSettings.BlockExpiredSales && hasExpiredBatches,
            RequiresOverride = effectiveSettings.AllowManagerOverride && hasExpiredBatches,
            Severity = GetSeverity(daysUntilExpiry ?? 999),
            ExpiredBatches = expiredBatches.Select(b => new ExpiredBatchInfoDto
            {
                BatchId = b.Id,
                BatchNumber = b.BatchNumber,
                ExpiryDate = b.ExpiryDate!.Value,
                DaysExpired = (int)(now - b.ExpiryDate!.Value).TotalDays,
                Quantity = b.CurrentQuantity,
                UnitCost = b.UnitCost
            }).ToList()
        };

        if (hasExpiredBatches)
        {
            if (effectiveSettings.BlockExpiredSales)
            {
                result.BlockReason = effectiveSettings.AllowManagerOverride
                    ? "Item has expired. Manager override required to proceed."
                    : "Item has expired. Sale blocked.";
            }
            else
            {
                result.BlockReason = "Warning: Item has expired.";
            }
        }
        else if (result.HasNearExpiryItems)
        {
            result.BlockReason = $"Warning: Item expires in {daysUntilExpiry} days.";
        }

        return result;
    }

    public async Task<ExpiredItemCheckDto> ValidateBatchForSaleAsync(int batchId, int quantity = 1)
    {
        var batch = await _batchRepository.GetByIdAsync(batchId);
        if (batch == null)
        {
            return new ExpiredItemCheckDto
            {
                BlockingEnabled = false,
                BlockReason = "Batch not found"
            };
        }

        var product = await _productRepository.GetByIdAsync(batch.ProductId);
        var effectiveSettings = await GetEffectiveSettingsAsync(batch.ProductId);

        var now = DateTime.UtcNow;
        var isExpired = batch.ExpiryDate.HasValue && batch.ExpiryDate.Value < now;
        var daysUntilExpiry = batch.ExpiryDate.HasValue
            ? (int)(batch.ExpiryDate.Value - now).TotalDays
            : (int?)null;
        var daysExpired = isExpired ? (int)(now - batch.ExpiryDate!.Value).TotalDays : 0;

        var result = new ExpiredItemCheckDto
        {
            ProductId = batch.ProductId,
            ProductName = product?.Name ?? "",
            StoreId = batch.StoreId,
            IsExpired = isExpired,
            HasNearExpiryItems = daysUntilExpiry.HasValue && daysUntilExpiry <= effectiveSettings.WarningDays && !isExpired,
            EarliestExpiry = batch.ExpiryDate,
            DaysUntilEarliestExpiry = daysUntilExpiry,
            DaysExpired = daysExpired > 0 ? daysExpired : null,
            BlockingEnabled = effectiveSettings.BlockExpiredSales && isExpired,
            RequiresOverride = effectiveSettings.AllowManagerOverride && isExpired,
            Severity = GetSeverity(daysUntilExpiry ?? 999)
        };

        if (isExpired)
        {
            result.ExpiredBatches.Add(new ExpiredBatchInfoDto
            {
                BatchId = batch.Id,
                BatchNumber = batch.BatchNumber,
                ExpiryDate = batch.ExpiryDate!.Value,
                DaysExpired = daysExpired,
                Quantity = batch.CurrentQuantity,
                UnitCost = batch.UnitCost
            });

            result.BlockReason = effectiveSettings.BlockExpiredSales
                ? (effectiveSettings.AllowManagerOverride
                    ? "Batch has expired. Manager override required."
                    : "Batch has expired. Sale blocked.")
                : "Warning: Batch has expired.";
        }

        return result;
    }

    public async Task<bool> IsBlockingEnabledAsync(int productId)
    {
        var settings = await GetEffectiveSettingsAsync(productId);
        return settings.BlockExpiredSales;
    }

    public async Task<bool> IsOverrideAllowedAsync(int productId)
    {
        var settings = await GetEffectiveSettingsAsync(productId);
        return settings.AllowManagerOverride;
    }

    #endregion

    #region Sale Blocking

    public async Task<ExpirySaleBlockDto> RecordSaleBlockAsync(CreateExpirySaleBlockDto dto, int userId)
    {
        var batch = await _batchRepository.GetByIdAsync(dto.BatchId);
        var product = await _productRepository.GetByIdAsync(dto.ProductId);
        var store = await _storeRepository.GetByIdAsync(dto.StoreId);
        var user = await _userRepository.GetByIdAsync(userId);

        var saleBlock = new ExpirySaleBlock
        {
            ProductId = dto.ProductId,
            BatchId = dto.BatchId,
            StoreId = dto.StoreId,
            ExpiryDate = dto.ExpiryDate,
            DaysExpired = dto.DaysExpired,
            AttemptedByUserId = userId,
            AttemptedAt = DateTime.UtcNow,
            AttemptedQuantity = dto.AttemptedQuantity,
            WasBlocked = true,
            OverrideApplied = false
        };

        await _saleBlockRepository.AddAsync(saleBlock);
        await _unitOfWork.SaveChangesAsync();

        return new ExpirySaleBlockDto
        {
            Id = saleBlock.Id,
            ProductId = saleBlock.ProductId,
            ProductName = product?.Name ?? "",
            ProductCode = product?.SKU ?? "",
            BatchId = saleBlock.BatchId,
            BatchNumber = batch?.BatchNumber ?? "",
            StoreId = saleBlock.StoreId,
            StoreName = store?.Name ?? "",
            ExpiryDate = saleBlock.ExpiryDate,
            DaysExpired = saleBlock.DaysExpired,
            AttemptedByUserId = saleBlock.AttemptedByUserId,
            AttemptedByUserName = user?.Username ?? "",
            AttemptedAt = saleBlock.AttemptedAt,
            AttemptedQuantity = saleBlock.AttemptedQuantity,
            WasBlocked = saleBlock.WasBlocked,
            OverrideApplied = saleBlock.OverrideApplied
        };
    }

    public async Task<ExpirySaleOverrideResultDto> ProcessOverrideAsync(ExpirySaleOverrideRequestDto request)
    {
        var saleBlock = await _saleBlockRepository.GetByIdAsync(request.SaleBlockId);
        if (saleBlock == null)
        {
            return new ExpirySaleOverrideResultDto
            {
                Success = false,
                OverrideApproved = false,
                Message = "Sale block record not found."
            };
        }

        // Verify manager credentials (PIN check)
        var manager = await _userRepository.GetByIdAsync(request.ManagerUserId);
        if (manager == null)
        {
            return new ExpirySaleOverrideResultDto
            {
                Success = false,
                OverrideApproved = false,
                Message = "Manager not found."
            };
        }

        // Verify PIN is provided
        if (string.IsNullOrWhiteSpace(request.ManagerPin))
        {
            return new ExpirySaleOverrideResultDto
            {
                Success = false,
                OverrideApproved = false,
                Message = "Manager PIN required."
            };
        }

        // Verify manager has a PIN configured
        if (string.IsNullOrEmpty(manager.PinHash))
        {
            return new ExpirySaleOverrideResultDto
            {
                Success = false,
                OverrideApproved = false,
                Message = "Manager does not have a PIN configured."
            };
        }

        // Verify PIN against stored hash using BCrypt
        if (!BCrypt.Net.BCrypt.Verify(request.ManagerPin, manager.PinHash))
        {
            return new ExpirySaleOverrideResultDto
            {
                Success = false,
                OverrideApproved = false,
                Message = "Invalid manager PIN."
            };
        }

        // Check if override is allowed
        var isOverrideAllowed = await IsOverrideAllowedAsync(saleBlock.ProductId);
        if (!isOverrideAllowed)
        {
            return new ExpirySaleOverrideResultDto
            {
                Success = false,
                OverrideApproved = false,
                Message = "Override not allowed for this product."
            };
        }

        // Apply override
        saleBlock.OverrideApplied = true;
        saleBlock.OverrideByUserId = request.ManagerUserId;
        saleBlock.OverrideAt = DateTime.UtcNow;
        saleBlock.OverrideReason = request.Reason;
        saleBlock.WasBlocked = false;

        await _saleBlockRepository.UpdateAsync(saleBlock);
        await _unitOfWork.SaveChangesAsync();

        return new ExpirySaleOverrideResultDto
        {
            Success = true,
            OverrideApproved = true,
            Message = "Override approved. Sale can proceed.",
            SaleBlockId = saleBlock.Id,
            OverrideAt = saleBlock.OverrideAt
        };
    }

    public async Task LinkSaleBlockToReceiptAsync(int saleBlockId, int receiptId)
    {
        var saleBlock = await _saleBlockRepository.GetByIdAsync(saleBlockId);
        if (saleBlock != null)
        {
            saleBlock.ReceiptId = receiptId;
            await _saleBlockRepository.UpdateAsync(saleBlock);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task<ExpirySaleBlockDto?> GetSaleBlockAsync(int saleBlockId)
    {
        var saleBlock = await _saleBlockRepository.GetByIdAsync(saleBlockId);
        if (saleBlock == null) return null;

        var product = await _productRepository.GetByIdAsync(saleBlock.ProductId);
        var batch = await _batchRepository.GetByIdAsync(saleBlock.BatchId);
        var store = await _storeRepository.GetByIdAsync(saleBlock.StoreId);
        var attemptedBy = await _userRepository.GetByIdAsync(saleBlock.AttemptedByUserId);
        var overrideBy = saleBlock.OverrideByUserId.HasValue
            ? await _userRepository.GetByIdAsync(saleBlock.OverrideByUserId.Value)
            : null;

        return MapToDto(saleBlock, product, batch, store, attemptedBy, overrideBy);
    }

    public async Task<List<ExpirySaleBlockDto>> GetSaleBlocksAsync(SaleBlockQueryDto query)
    {
        var blocks = await _saleBlockRepository.FindAsync(b =>
            b.IsActive &&
            (!query.StoreId.HasValue || b.StoreId == query.StoreId) &&
            (!query.ProductId.HasValue || b.ProductId == query.ProductId) &&
            (!query.FromDate.HasValue || b.AttemptedAt >= query.FromDate) &&
            (!query.ToDate.HasValue || b.AttemptedAt <= query.ToDate) &&
            (!query.OverrideApplied.HasValue || b.OverrideApplied == query.OverrideApplied) &&
            (!query.AttemptedByUserId.HasValue || b.AttemptedByUserId == query.AttemptedByUserId) &&
            (!query.OverrideByUserId.HasValue || b.OverrideByUserId == query.OverrideByUserId));

        var result = new List<ExpirySaleBlockDto>();
        foreach (var block in blocks.OrderByDescending(b => b.AttemptedAt))
        {
            var product = await _productRepository.GetByIdAsync(block.ProductId);
            var batch = await _batchRepository.GetByIdAsync(block.BatchId);
            var store = await _storeRepository.GetByIdAsync(block.StoreId);
            var attemptedBy = await _userRepository.GetByIdAsync(block.AttemptedByUserId);
            var overrideBy = block.OverrideByUserId.HasValue
                ? await _userRepository.GetByIdAsync(block.OverrideByUserId.Value)
                : null;

            result.Add(MapToDto(block, product, batch, store, attemptedBy, overrideBy));
        }

        return result;
    }

    public async Task<SaleBlockSummaryDto> GetSaleBlockSummaryAsync(int? storeId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var blocks = await _saleBlockRepository.FindAsync(b =>
            b.IsActive &&
            (!storeId.HasValue || b.StoreId == storeId) &&
            (!fromDate.HasValue || b.AttemptedAt >= fromDate) &&
            (!toDate.HasValue || b.AttemptedAt <= toDate));

        var blockList = blocks.ToList();

        // Get product info for value calculation
        var productIds = blockList.Select(b => b.ProductId).Distinct();
        var products = new Dictionary<int, Product>();
        foreach (var productId in productIds)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product != null) products[productId] = product;
        }

        var totalBlockedValue = blockList
            .Where(b => !b.OverrideApplied)
            .Sum(b => b.AttemptedQuantity * (products.TryGetValue(b.ProductId, out var p) ? p.SellingPrice : 0));

        var totalOverrideValue = blockList
            .Where(b => b.OverrideApplied)
            .Sum(b => b.AttemptedQuantity * (products.TryGetValue(b.ProductId, out var p) ? p.SellingPrice : 0));

        var topProducts = blockList
            .GroupBy(b => b.ProductId)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => new TopBlockedProductDto
            {
                ProductId = g.Key,
                ProductName = products.TryGetValue(g.Key, out var p) ? p.Name : "",
                ProductCode = products.TryGetValue(g.Key, out var pr) ? pr.SKU : "",
                BlockCount = g.Count(b => !b.OverrideApplied),
                OverrideCount = g.Count(b => b.OverrideApplied),
                BlockedValue = g.Where(b => !b.OverrideApplied)
                    .Sum(b => b.AttemptedQuantity * (products.TryGetValue(b.ProductId, out var prod) ? prod.SellingPrice : 0))
            })
            .ToList();

        return new SaleBlockSummaryDto
        {
            TotalBlockedAttempts = blockList.Count,
            TotalOverrides = blockList.Count(b => b.OverrideApplied),
            TotalPermanentBlocks = blockList.Count(b => !b.OverrideApplied),
            TotalBlockedValue = totalBlockedValue,
            TotalOverrideValue = totalOverrideValue,
            UniqueProducts = blockList.Select(b => b.ProductId).Distinct().Count(),
            UniqueStores = blockList.Select(b => b.StoreId).Distinct().Count(),
            EarliestAttempt = blockList.Any() ? blockList.Min(b => b.AttemptedAt) : null,
            LatestAttempt = blockList.Any() ? blockList.Max(b => b.AttemptedAt) : null,
            TopBlockedProducts = topProducts
        };
    }

    #endregion

    #region Category Settings

    public async Task<CategoryExpirySettingsDto?> GetCategorySettingsAsync(int categoryId)
    {
        var settings = await _categorySettingsRepository.FindAsync(s => s.CategoryId == categoryId && s.IsActive);
        var setting = settings.FirstOrDefault();
        if (setting == null) return null;

        var category = await _categoryRepository.GetByIdAsync(categoryId);
        return MapCategorySettingsToDto(setting, category);
    }

    public async Task<List<CategoryExpirySettingsDto>> GetAllCategorySettingsAsync()
    {
        var settings = await _categorySettingsRepository.FindAsync(s => s.IsActive);
        var result = new List<CategoryExpirySettingsDto>();

        foreach (var setting in settings)
        {
            var category = await _categoryRepository.GetByIdAsync(setting.CategoryId);
            result.Add(MapCategorySettingsToDto(setting, category));
        }

        return result;
    }

    public async Task<CategoryExpirySettingsDto> SaveCategorySettingsAsync(UpdateCategoryExpirySettingsDto dto, int userId)
    {
        var existing = await _categorySettingsRepository.FindAsync(s => s.CategoryId == dto.CategoryId && s.IsActive);
        var settings = existing.FirstOrDefault();

        if (settings == null)
        {
            settings = new CategoryExpirySettings
            {
                CategoryId = dto.CategoryId
            };
        }

        settings.RequiresExpiryTracking = dto.RequiresExpiryTracking;
        settings.BlockExpiredSales = dto.BlockExpiredSales;
        settings.AllowManagerOverride = dto.AllowManagerOverride;
        settings.WarningDays = dto.WarningDays;
        settings.CriticalDays = dto.CriticalDays;
        settings.ExpiredItemAction = Enum.TryParse<ExpiryAction>(dto.ExpiredItemAction, out var expiredAction)
            ? expiredAction : ExpiryAction.Block;
        settings.NearExpiryAction = Enum.TryParse<ExpiryAction>(dto.NearExpiryAction, out var nearAction)
            ? nearAction : ExpiryAction.Warn;
        settings.MinimumShelfLifeDaysOnReceipt = dto.MinimumShelfLifeDaysOnReceipt;

        if (settings.Id == 0)
        {
            await _categorySettingsRepository.AddAsync(settings);
        }
        else
        {
            await _categorySettingsRepository.UpdateAsync(settings);
        }

        await _unitOfWork.SaveChangesAsync();

        var category = await _categoryRepository.GetByIdAsync(dto.CategoryId);
        return MapCategorySettingsToDto(settings, category);
    }

    public async Task DeleteCategorySettingsAsync(int categoryId)
    {
        var existing = await _categorySettingsRepository.FindAsync(s => s.CategoryId == categoryId && s.IsActive);
        var settings = existing.FirstOrDefault();

        if (settings != null)
        {
            settings.IsActive = false;
            await _categorySettingsRepository.UpdateAsync(settings);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task<EffectiveExpirySettingsDto> GetEffectiveSettingsAsync(int productId)
    {
        var product = await _productRepository.GetByIdAsync(productId);
        if (product == null)
        {
            return GetDefaultSettings(productId);
        }

        // First check product-level configuration
        var productConfigs = await _batchConfigRepository.FindAsync(c => c.ProductId == productId && c.IsActive);
        var productConfig = productConfigs.FirstOrDefault();

        if (productConfig != null && productConfig.RequiresBatchTracking)
        {
            return new EffectiveExpirySettingsDto
            {
                ProductId = productId,
                ProductName = product.Name,
                CategoryId = product.CategoryId,
                RequiresExpiryTracking = productConfig.RequiresExpiryDate,
                BlockExpiredSales = productConfig.ExpiredItemAction == ExpiryAction.Block,
                AllowManagerOverride = productConfig.ExpiredItemAction == ExpiryAction.RequireOverride,
                WarningDays = productConfig.ExpiryWarningDays,
                CriticalDays = productConfig.ExpiryCriticalDays,
                ExpiredItemAction = productConfig.ExpiredItemAction.ToString(),
                NearExpiryAction = productConfig.NearExpiryAction.ToString(),
                SettingsSource = "Product"
            };
        }

        // Check category-level configuration
        if (product.CategoryId > 0)
        {
            var categorySettings = await GetCategorySettingsAsync(product.CategoryId);
            if (categorySettings != null)
            {
                var category = await _categoryRepository.GetByIdAsync(product.CategoryId);
                return new EffectiveExpirySettingsDto
                {
                    ProductId = productId,
                    ProductName = product.Name,
                    CategoryId = product.CategoryId,
                    CategoryName = category?.Name,
                    RequiresExpiryTracking = categorySettings.RequiresExpiryTracking,
                    BlockExpiredSales = categorySettings.BlockExpiredSales,
                    AllowManagerOverride = categorySettings.AllowManagerOverride,
                    WarningDays = categorySettings.WarningDays,
                    CriticalDays = categorySettings.CriticalDays,
                    ExpiredItemAction = categorySettings.ExpiredItemAction,
                    NearExpiryAction = categorySettings.NearExpiryAction,
                    SettingsSource = "Category"
                };
            }
        }

        // Return defaults
        return GetDefaultSettings(productId, product.Name);
    }

    #endregion

    #region Override Audit

    public async Task<List<ExpirySaleBlockDto>> GetOverrideHistoryAsync(int? storeId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = new SaleBlockQueryDto
        {
            StoreId = storeId,
            FromDate = fromDate,
            ToDate = toDate,
            OverrideApplied = true
        };

        return await GetSaleBlocksAsync(query);
    }

    public async Task<List<ManagerOverrideStatsDto>> GetOverrideStatsByManagerAsync(int? storeId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var blocks = await _saleBlockRepository.FindAsync(b =>
            b.IsActive &&
            b.OverrideApplied &&
            b.OverrideByUserId.HasValue &&
            (!storeId.HasValue || b.StoreId == storeId) &&
            (!fromDate.HasValue || b.AttemptedAt >= fromDate) &&
            (!toDate.HasValue || b.AttemptedAt <= toDate));

        var grouped = blocks.GroupBy(b => b.OverrideByUserId!.Value);

        var result = new List<ManagerOverrideStatsDto>();
        foreach (var group in grouped)
        {
            var manager = await _userRepository.GetByIdAsync(group.Key);
            var blockList = group.ToList();

            // Get products for value calculation
            var productIds = blockList.Select(b => b.ProductId).Distinct();
            var products = new Dictionary<int, Product>();
            foreach (var productId in productIds)
            {
                var product = await _productRepository.GetByIdAsync(productId);
                if (product != null) products[productId] = product;
            }

            var totalValue = blockList.Sum(b =>
                b.AttemptedQuantity * (products.TryGetValue(b.ProductId, out var p) ? p.SellingPrice : 0));

            var topProducts = blockList
                .GroupBy(b => b.ProductId)
                .OrderByDescending(g => g.Count())
                .Take(3)
                .Select(g => products.TryGetValue(g.Key, out var p) ? p.Name : "Unknown")
                .ToList();

            result.Add(new ManagerOverrideStatsDto
            {
                ManagerUserId = group.Key,
                ManagerName = manager?.Username ?? "Unknown",
                TotalOverrides = blockList.Count,
                TotalOverrideValue = totalValue,
                UniqueProducts = blockList.Select(b => b.ProductId).Distinct().Count(),
                FirstOverride = blockList.Min(b => b.OverrideAt),
                LastOverride = blockList.Max(b => b.OverrideAt),
                TopOverridedProducts = topProducts
            });
        }

        return result.OrderByDescending(s => s.TotalOverrides).ToList();
    }

    #endregion

    #region Private Helpers

    private static ExpiryAlertSeverity GetSeverity(int daysUntilExpiry)
    {
        if (daysUntilExpiry < 0) return ExpiryAlertSeverity.Expired;
        if (daysUntilExpiry <= 7) return ExpiryAlertSeverity.Critical;
        if (daysUntilExpiry <= 14) return ExpiryAlertSeverity.Urgent;
        if (daysUntilExpiry <= 30) return ExpiryAlertSeverity.Warning;
        return ExpiryAlertSeverity.Info;
    }

    private static EffectiveExpirySettingsDto GetDefaultSettings(int productId, string? productName = null)
    {
        return new EffectiveExpirySettingsDto
        {
            ProductId = productId,
            ProductName = productName ?? "",
            RequiresExpiryTracking = false,
            BlockExpiredSales = true,
            AllowManagerOverride = true,
            WarningDays = 30,
            CriticalDays = 7,
            ExpiredItemAction = "Block",
            NearExpiryAction = "Warn",
            SettingsSource = "Default"
        };
    }

    private static ExpirySaleBlockDto MapToDto(
        ExpirySaleBlock block,
        Product? product,
        ProductBatch? batch,
        Store? store,
        User? attemptedBy,
        User? overrideBy)
    {
        return new ExpirySaleBlockDto
        {
            Id = block.Id,
            ProductId = block.ProductId,
            ProductName = product?.Name ?? "",
            ProductCode = product?.SKU ?? "",
            BatchId = block.BatchId,
            BatchNumber = batch?.BatchNumber ?? "",
            StoreId = block.StoreId,
            StoreName = store?.Name ?? "",
            ExpiryDate = block.ExpiryDate,
            DaysExpired = block.DaysExpired,
            AttemptedByUserId = block.AttemptedByUserId,
            AttemptedByUserName = attemptedBy?.Username ?? "",
            AttemptedAt = block.AttemptedAt,
            AttemptedQuantity = block.AttemptedQuantity,
            WasBlocked = block.WasBlocked,
            OverrideApplied = block.OverrideApplied,
            OverrideByUserId = block.OverrideByUserId,
            OverrideByUserName = overrideBy?.Username,
            OverrideAt = block.OverrideAt,
            OverrideReason = block.OverrideReason,
            ReceiptId = block.ReceiptId
        };
    }

    private static CategoryExpirySettingsDto MapCategorySettingsToDto(CategoryExpirySettings settings, Category? category)
    {
        return new CategoryExpirySettingsDto
        {
            Id = settings.Id,
            CategoryId = settings.CategoryId,
            CategoryName = category?.Name ?? "",
            RequiresExpiryTracking = settings.RequiresExpiryTracking,
            BlockExpiredSales = settings.BlockExpiredSales,
            AllowManagerOverride = settings.AllowManagerOverride,
            WarningDays = settings.WarningDays,
            CriticalDays = settings.CriticalDays,
            ExpiredItemAction = settings.ExpiredItemAction.ToString(),
            NearExpiryAction = settings.NearExpiryAction.ToString(),
            MinimumShelfLifeDaysOnReceipt = settings.MinimumShelfLifeDaysOnReceipt
        };
    }

    #endregion
}
