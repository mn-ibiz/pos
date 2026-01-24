using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Implementation of the points multiplier service for product-specific points and promotional rules.
/// </summary>
public class PointsMultiplierService : IPointsMultiplierService
{
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<Category> _categoryRepository;
    private readonly IRepository<PointsMultiplierRule> _ruleRepository;
    private readonly IRepository<PointsMultiplierUsage> _usageRepository;
    private readonly IRepository<PointsConfiguration> _pointsConfigRepository;
    private readonly ILoyaltyMemberRepository _memberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PointsMultiplierService> _logger;

    public PointsMultiplierService(
        IRepository<Product> productRepository,
        IRepository<Category> categoryRepository,
        IRepository<PointsMultiplierRule> ruleRepository,
        IRepository<PointsMultiplierUsage> usageRepository,
        IRepository<PointsConfiguration> pointsConfigRepository,
        ILoyaltyMemberRepository memberRepository,
        IUnitOfWork unitOfWork,
        ILogger<PointsMultiplierService> logger)
    {
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _ruleRepository = ruleRepository ?? throw new ArgumentNullException(nameof(ruleRepository));
        _usageRepository = usageRepository ?? throw new ArgumentNullException(nameof(usageRepository));
        _pointsConfigRepository = pointsConfigRepository ?? throw new ArgumentNullException(nameof(pointsConfigRepository));
        _memberRepository = memberRepository ?? throw new ArgumentNullException(nameof(memberRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Item-Level Points Calculation

    /// <inheritdoc />
    public async Task<DetailedPointsCalculationResult> CalculateItemPointsAsync(
        List<TransactionItemDto> items,
        int? memberId = null,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var result = new DetailedPointsCalculationResult();
        var now = DateTime.UtcNow;

        // Get base earning rate
        var config = await GetPointsConfigurationAsync(cancellationToken).ConfigureAwait(false);
        var earningRate = config?.EarningRate ?? 100m;
        var earnOnDiscountedItems = config?.EarnOnDiscountedItems ?? true;
        var earnOnTax = config?.EarnOnTax ?? false;
        result.EarningRate = earningRate;

        // Get member tier for tier bonus calculation
        MembershipTier? memberTier = null;
        if (memberId.HasValue)
        {
            var member = await _memberRepository.GetByIdAsync(memberId.Value, cancellationToken).ConfigureAwait(false);
            memberTier = member?.Tier;
            result.TierMultiplier = GetTierMultiplier(memberTier ?? MembershipTier.Bronze);
        }

        // Get all product IDs
        var productIds = items.Select(i => i.ProductId).Distinct().ToList();

        // Batch load products with categories
        var products = await _productRepository
            .Query()
            .Include(p => p.Category)
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken)
            .ConfigureAwait(false);

        // Get applicable promotional rules
        var applicableRules = await GetApplicableRulesInternalAsync(productIds, storeId, memberTier, now, cancellationToken)
            .ConfigureAwait(false);

        var appliedRulesDict = new Dictionary<int, AppliedMultiplierRuleDto>();

        // Calculate points for each item
        foreach (var item in items)
        {
            var itemResult = new ItemPointsResult
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName
            };

            // Check if product exists
            if (!products.TryGetValue(item.ProductId, out var product))
            {
                itemResult.IsExcluded = true;
                itemResult.ExclusionReason = "Product not found";
                result.ItemBreakdown.Add(itemResult);
                continue;
            }

            // Check if excluded from loyalty points
            if (product.ExcludeFromLoyaltyPoints)
            {
                itemResult.IsExcluded = true;
                itemResult.ExclusionReason = "Product excluded from loyalty";
                result.ItemBreakdown.Add(itemResult);
                continue;
            }

            // Check category exclusion
            if (product.Category?.ExcludeFromLoyaltyPoints == true && !product.PointsMultiplier.HasValue)
            {
                itemResult.IsExcluded = true;
                itemResult.ExclusionReason = "Category excluded from loyalty";
                result.ItemBreakdown.Add(itemResult);
                continue;
            }

            // Calculate eligible amount for this item
            var eligibleAmount = item.Amount;
            if (!earnOnDiscountedItems)
            {
                eligibleAmount -= item.DiscountAmount;
            }
            if (!earnOnTax)
            {
                eligibleAmount -= item.TaxAmount;
            }
            eligibleAmount = Math.Max(0, eligibleAmount);
            itemResult.EligibleAmount = eligibleAmount;

            // Calculate base points
            var basePoints = Math.Floor(eligibleAmount / earningRate);
            itemResult.BasePoints = basePoints;

            // Determine multiplier: Product > Category > Promotional Rules > Global (1.0)
            var (multiplier, source, ruleName) = DetermineMultiplier(product, applicableRules, item.ProductId, item.Quantity);
            itemResult.MultiplierApplied = multiplier;
            itemResult.MultiplierSource = source;
            itemResult.PromotionName = ruleName;

            // Calculate bonus points from multiplier
            if (multiplier > 1.0m)
            {
                var totalWithMultiplier = Math.Floor(basePoints * multiplier);
                itemResult.BonusPoints = totalWithMultiplier - basePoints;

                // Track which rules were applied
                if (ruleName != null)
                {
                    var rule = applicableRules.FirstOrDefault(r => r.Name == ruleName);
                    if (rule != null && !appliedRulesDict.ContainsKey(rule.Id))
                    {
                        appliedRulesDict[rule.Id] = new AppliedMultiplierRuleDto
                        {
                            RuleId = rule.Id,
                            RuleName = rule.Name,
                            RuleType = rule.RuleType.ToString(),
                            Multiplier = rule.Multiplier
                        };
                    }
                    if (rule != null)
                    {
                        appliedRulesDict[rule.Id].AppliedToProductIds.Add(item.ProductId);
                        appliedRulesDict[rule.Id].BonusPointsEarned += itemResult.BonusPoints;
                    }
                }
            }

            result.ItemBreakdown.Add(itemResult);
            result.TotalEligibleAmount += eligibleAmount;
            result.TotalBasePoints += basePoints;
            result.TotalBonusPoints += itemResult.BonusPoints;
        }

        // Calculate tier bonus on top of item points
        if (result.TierMultiplier > 1.0m)
        {
            var pointsBeforeTierBonus = result.TotalBasePoints + result.TotalBonusPoints;
            var totalWithTierBonus = Math.Floor(pointsBeforeTierBonus * result.TierMultiplier);
            result.TierBonusPoints = totalWithTierBonus - pointsBeforeTierBonus;
        }

        result.AppliedRules = appliedRulesDict.Values.ToList();
        result.Description = $"Earned {result.GrandTotalPoints:N0} points on KES {result.TotalEligibleAmount:N0} spend " +
                            $"({result.ItemBreakdown.Count} items, {result.ItemsWithBonusCount} with bonus)";

        return result;
    }

    /// <inheritdoc />
    public async Task<(decimal multiplier, string source, string? ruleName)> GetEffectiveMultiplierAsync(
        int productId,
        int? memberId = null,
        int? storeId = null,
        DateTime? checkTime = null,
        CancellationToken cancellationToken = default)
    {
        var now = checkTime ?? DateTime.UtcNow;

        // Get product with category
        var product = await _productRepository
            .Query()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken)
            .ConfigureAwait(false);

        if (product == null)
        {
            return (1.0m, "Default", null);
        }

        // Check product-level multiplier
        if (product.PointsMultiplier.HasValue)
        {
            return (product.PointsMultiplier.Value, "Product", null);
        }

        // Check category-level multiplier
        if (product.Category?.PointsMultiplier.HasValue == true)
        {
            return (product.Category.PointsMultiplier!.Value, "Category", null);
        }

        // Get member tier for applicable rules
        MembershipTier? memberTier = null;
        if (memberId.HasValue)
        {
            var member = await _memberRepository.GetByIdAsync(memberId.Value, cancellationToken).ConfigureAwait(false);
            memberTier = member?.Tier;
        }

        // Check promotional rules
        var applicableRules = await GetApplicableRulesInternalAsync(
            new List<int> { productId }, storeId, memberTier, now, cancellationToken)
            .ConfigureAwait(false);

        var bestRule = applicableRules
            .Where(r => r.ProductId == productId || r.CategoryId == product.CategoryId || r.RuleType == PointsMultiplierRuleType.Global)
            .OrderByDescending(r => r.Priority)
            .ThenByDescending(r => r.Multiplier)
            .FirstOrDefault();

        if (bestRule != null && bestRule.Multiplier != 1.0m)
        {
            return (bestRule.Multiplier, "Promotion", bestRule.Name);
        }

        return (1.0m, "Default", null);
    }

    /// <inheritdoc />
    public async Task<bool> IsProductExcludedAsync(int productId, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository
            .Query()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken)
            .ConfigureAwait(false);

        if (product == null) return true;
        if (product.ExcludeFromLoyaltyPoints) return true;
        if (product.Category?.ExcludeFromLoyaltyPoints == true && !product.PointsMultiplier.HasValue) return true;

        return false;
    }

    #endregion

    #region Product/Category Multiplier Configuration

    /// <inheritdoc />
    public async Task<ProductPointsConfigDto?> GetProductPointsConfigAsync(int productId, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository
            .Query()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken)
            .ConfigureAwait(false);

        if (product == null) return null;

        var activePromotions = await GetApplicableRulesForProductAsync(productId, null, null, cancellationToken)
            .ConfigureAwait(false);

        return new ProductPointsConfigDto
        {
            ProductId = product.Id,
            ProductName = product.Name,
            SKU = product.SKU,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name,
            ProductMultiplier = product.PointsMultiplier,
            CategoryMultiplier = product.Category?.PointsMultiplier,
            IsExcluded = product.ExcludeFromLoyaltyPoints ||
                        (product.Category?.ExcludeFromLoyaltyPoints == true && !product.PointsMultiplier.HasValue),
            ActivePromotions = activePromotions
        };
    }

    /// <inheritdoc />
    public async Task<List<ProductPointsConfigDto>> GetProductPointsConfigsAsync(List<int> productIds, CancellationToken cancellationToken = default)
    {
        var products = await _productRepository
            .Query()
            .Include(p => p.Category)
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return products.Select(p => new ProductPointsConfigDto
        {
            ProductId = p.Id,
            ProductName = p.Name,
            SKU = p.SKU,
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name,
            ProductMultiplier = p.PointsMultiplier,
            CategoryMultiplier = p.Category?.PointsMultiplier,
            IsExcluded = p.ExcludeFromLoyaltyPoints ||
                        (p.Category?.ExcludeFromLoyaltyPoints == true && !p.PointsMultiplier.HasValue)
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<List<ProductPointsConfigDto>> GetCategoryProductPointsConfigsAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        var products = await _productRepository
            .Query()
            .Include(p => p.Category)
            .Where(p => p.CategoryId == categoryId && !p.IsDeleted)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return products.Select(p => new ProductPointsConfigDto
        {
            ProductId = p.Id,
            ProductName = p.Name,
            SKU = p.SKU,
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name,
            ProductMultiplier = p.PointsMultiplier,
            CategoryMultiplier = p.Category?.PointsMultiplier,
            IsExcluded = p.ExcludeFromLoyaltyPoints ||
                        (p.Category?.ExcludeFromLoyaltyPoints == true && !p.PointsMultiplier.HasValue)
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<bool> UpdateProductPointsMultiplierAsync(UpdateProductPointsDto dto, int updatedByUserId, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(dto.ProductId, cancellationToken).ConfigureAwait(false);
        if (product == null) return false;

        product.PointsMultiplier = dto.PointsMultiplier;
        product.ExcludeFromLoyaltyPoints = dto.ExcludeFromLoyaltyPoints;
        product.UpdatedAt = DateTime.UtcNow;

        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Updated product {ProductId} points multiplier to {Multiplier}, excluded: {Excluded} by user {UserId}",
            dto.ProductId, dto.PointsMultiplier, dto.ExcludeFromLoyaltyPoints, updatedByUserId);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateCategoryPointsMultiplierAsync(UpdateCategoryPointsDto dto, int updatedByUserId, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(dto.CategoryId, cancellationToken).ConfigureAwait(false);
        if (category == null) return false;

        category.PointsMultiplier = dto.PointsMultiplier;
        category.ExcludeFromLoyaltyPoints = dto.ExcludeFromLoyaltyPoints;
        category.UpdatedAt = DateTime.UtcNow;

        _categoryRepository.Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Updated category {CategoryId} points multiplier to {Multiplier}, excluded: {Excluded} by user {UserId}",
            dto.CategoryId, dto.PointsMultiplier, dto.ExcludeFromLoyaltyPoints, updatedByUserId);

        return true;
    }

    /// <inheritdoc />
    public async Task<int> BulkUpdateProductPointsMultipliersAsync(List<UpdateProductPointsDto> updates, int updatedByUserId, CancellationToken cancellationToken = default)
    {
        var productIds = updates.Select(u => u.ProductId).ToList();
        var products = await _productRepository
            .Query()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken)
            .ConfigureAwait(false);

        var updatedCount = 0;
        foreach (var update in updates)
        {
            if (products.TryGetValue(update.ProductId, out var product))
            {
                product.PointsMultiplier = update.PointsMultiplier;
                product.ExcludeFromLoyaltyPoints = update.ExcludeFromLoyaltyPoints;
                product.UpdatedAt = DateTime.UtcNow;
                _productRepository.Update(product);
                updatedCount++;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Bulk updated {Count} products' points multipliers by user {UserId}",
            updatedCount, updatedByUserId);

        return updatedCount;
    }

    #endregion

    #region Points Multiplier Rules

    /// <inheritdoc />
    public async Task<List<PointsMultiplierRuleDto>> GetRulesAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _ruleRepository.Query()
            .Include(r => r.Product)
            .Include(r => r.Category);

        if (!includeInactive)
        {
            query = query.Where(r => r.IsActive);
        }

        var rules = await query
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rules.Select(MapRuleToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<PointsMultiplierRuleDto?> GetRuleAsync(int ruleId, CancellationToken cancellationToken = default)
    {
        var rule = await _ruleRepository.Query()
            .Include(r => r.Product)
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == ruleId, cancellationToken)
            .ConfigureAwait(false);

        return rule != null ? MapRuleToDto(rule) : null;
    }

    /// <inheritdoc />
    public async Task<List<PointsMultiplierRuleDto>> GetApplicableRulesForProductAsync(
        int productId,
        int? storeId = null,
        DateTime? checkTime = null,
        CancellationToken cancellationToken = default)
    {
        var now = checkTime ?? DateTime.UtcNow;

        var product = await _productRepository
            .Query()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken)
            .ConfigureAwait(false);

        if (product == null) return new List<PointsMultiplierRuleDto>();

        var rules = await _ruleRepository.Query()
            .Include(r => r.Product)
            .Include(r => r.Category)
            .Where(r => r.IsActive)
            .Where(r => r.ProductId == productId ||
                       r.CategoryId == product.CategoryId ||
                       r.RuleType == PointsMultiplierRuleType.Global ||
                       r.RuleType == PointsMultiplierRuleType.DayOfWeek ||
                       r.RuleType == PointsMultiplierRuleType.TimeOfDay)
            .Where(r => !r.StoreId.HasValue || r.StoreId == storeId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Filter by time constraints
        var applicableRules = rules
            .Where(r => r.AppliesToTime(now))
            .Where(r => !r.IsUsageLimitReached)
            .Select(MapRuleToDto)
            .ToList();

        return applicableRules;
    }

    /// <inheritdoc />
    public async Task<List<PointsMultiplierRuleDto>> GetActivePromotionsAsync(
        int? storeId = null,
        DateTime? checkTime = null,
        CancellationToken cancellationToken = default)
    {
        var now = checkTime ?? DateTime.UtcNow;

        var rules = await _ruleRepository.Query()
            .Include(r => r.Product)
            .Include(r => r.Category)
            .Where(r => r.IsActive)
            .Where(r => !r.StoreId.HasValue || r.StoreId == storeId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rules
            .Where(r => r.AppliesToTime(now))
            .Where(r => !r.IsUsageLimitReached)
            .Select(MapRuleToDto)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<MultiplierRuleResult> CreateRuleAsync(PointsMultiplierRuleDto dto, int createdByUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return MultiplierRuleResult.Failure("Rule name is required.");
            }

            if (dto.Multiplier <= 0)
            {
                return MultiplierRuleResult.Failure("Multiplier must be greater than zero.");
            }

            var rule = new PointsMultiplierRule
            {
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                RuleType = (PointsMultiplierRuleType)dto.RuleType,
                Multiplier = dto.Multiplier,
                IsStackable = dto.IsStackable,
                Priority = dto.Priority,
                ProductId = dto.ProductId,
                CategoryId = dto.CategoryId,
                MinimumTier = dto.MinimumTier,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                DaysOfWeek = dto.DaysOfWeek,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                MinimumPurchaseAmount = dto.MinimumPurchaseAmount,
                MinimumQuantity = dto.MinimumQuantity,
                MaxBonusPointsPerTransaction = dto.MaxBonusPointsPerTransaction,
                MaxTotalUsages = dto.MaxTotalUsages,
                MaxUsagesPerMember = dto.MaxUsagesPerMember,
                StoreId = dto.StoreId,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = createdByUserId
            };

            await _ruleRepository.AddAsync(rule, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // Reload with navigation properties
            var createdRule = await _ruleRepository.Query()
                .Include(r => r.Product)
                .Include(r => r.Category)
                .FirstOrDefaultAsync(r => r.Id == rule.Id, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "Created points multiplier rule {RuleId}: {RuleName} with multiplier {Multiplier} by user {UserId}",
                rule.Id, rule.Name, rule.Multiplier, createdByUserId);

            return MultiplierRuleResult.Success(MapRuleToDto(createdRule!));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create points multiplier rule: {RuleName}", dto.Name);
            return MultiplierRuleResult.Failure("An error occurred while creating the rule.");
        }
    }

    /// <inheritdoc />
    public async Task<MultiplierRuleResult> UpdateRuleAsync(PointsMultiplierRuleDto dto, int updatedByUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            var rule = await _ruleRepository.GetByIdAsync(dto.Id, cancellationToken).ConfigureAwait(false);
            if (rule == null)
            {
                return MultiplierRuleResult.Failure("Rule not found.");
            }

            rule.Name = dto.Name.Trim();
            rule.Description = dto.Description?.Trim();
            rule.RuleType = (PointsMultiplierRuleType)dto.RuleType;
            rule.Multiplier = dto.Multiplier;
            rule.IsStackable = dto.IsStackable;
            rule.Priority = dto.Priority;
            rule.ProductId = dto.ProductId;
            rule.CategoryId = dto.CategoryId;
            rule.MinimumTier = dto.MinimumTier;
            rule.StartDate = dto.StartDate;
            rule.EndDate = dto.EndDate;
            rule.DaysOfWeek = dto.DaysOfWeek;
            rule.StartTime = dto.StartTime;
            rule.EndTime = dto.EndTime;
            rule.MinimumPurchaseAmount = dto.MinimumPurchaseAmount;
            rule.MinimumQuantity = dto.MinimumQuantity;
            rule.MaxBonusPointsPerTransaction = dto.MaxBonusPointsPerTransaction;
            rule.MaxTotalUsages = dto.MaxTotalUsages;
            rule.MaxUsagesPerMember = dto.MaxUsagesPerMember;
            rule.StoreId = dto.StoreId;
            rule.IsActive = dto.IsActive;
            rule.UpdatedAt = DateTime.UtcNow;
            rule.UpdatedByUserId = updatedByUserId;

            _ruleRepository.Update(rule);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // Reload with navigation properties
            var updatedRule = await _ruleRepository.Query()
                .Include(r => r.Product)
                .Include(r => r.Category)
                .FirstOrDefaultAsync(r => r.Id == rule.Id, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "Updated points multiplier rule {RuleId}: {RuleName} by user {UserId}",
                rule.Id, rule.Name, updatedByUserId);

            return MultiplierRuleResult.Success(MapRuleToDto(updatedRule!));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update points multiplier rule {RuleId}", dto.Id);
            return MultiplierRuleResult.Failure("An error occurred while updating the rule.");
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeactivateRuleAsync(int ruleId, int deactivatedByUserId, CancellationToken cancellationToken = default)
    {
        var rule = await _ruleRepository.GetByIdAsync(ruleId, cancellationToken).ConfigureAwait(false);
        if (rule == null) return false;

        rule.IsActive = false;
        rule.UpdatedAt = DateTime.UtcNow;
        rule.UpdatedByUserId = deactivatedByUserId;

        _ruleRepository.Update(rule);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Deactivated points multiplier rule {RuleId} by user {UserId}", ruleId, deactivatedByUserId);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteRuleAsync(int ruleId, int deletedByUserId, CancellationToken cancellationToken = default)
    {
        var rule = await _ruleRepository.GetByIdAsync(ruleId, cancellationToken).ConfigureAwait(false);
        if (rule == null) return false;

        // Soft delete
        rule.IsActive = false;
        rule.UpdatedAt = DateTime.UtcNow;
        rule.UpdatedByUserId = deletedByUserId;

        _ruleRepository.Update(rule);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Deleted points multiplier rule {RuleId} by user {UserId}", ruleId, deletedByUserId);
        return true;
    }

    #endregion

    #region Rule Usage Tracking

    /// <inheritdoc />
    public async Task RecordRuleUsageAsync(
        int ruleId,
        int memberId,
        int? transactionId,
        int? receiptId,
        decimal basePoints,
        decimal bonusPoints,
        decimal multiplier,
        CancellationToken cancellationToken = default)
    {
        var usage = new PointsMultiplierUsage
        {
            PointsMultiplierRuleId = ruleId,
            LoyaltyMemberId = memberId,
            LoyaltyTransactionId = transactionId,
            ReceiptId = receiptId,
            UsedAt = DateTime.UtcNow,
            BasePoints = basePoints,
            BonusPointsEarned = bonusPoints,
            MultiplierApplied = multiplier,
            CreatedAt = DateTime.UtcNow
        };

        await _usageRepository.AddAsync(usage, cancellationToken).ConfigureAwait(false);

        // Increment rule usage count
        var rule = await _ruleRepository.GetByIdAsync(ruleId, cancellationToken).ConfigureAwait(false);
        if (rule != null)
        {
            rule.CurrentUsageCount++;
            _ruleRepository.Update(rule);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> GetMemberRuleUsageCountAsync(int ruleId, int memberId, CancellationToken cancellationToken = default)
    {
        return await _usageRepository.Query()
            .CountAsync(u => u.PointsMultiplierRuleId == ruleId && u.LoyaltyMemberId == memberId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<(bool canUse, string? reason)> CanMemberUseRuleAsync(
        int ruleId,
        int memberId,
        DateTime? checkTime = null,
        CancellationToken cancellationToken = default)
    {
        var rule = await _ruleRepository.GetByIdAsync(ruleId, cancellationToken).ConfigureAwait(false);
        if (rule == null)
        {
            return (false, "Rule not found");
        }

        if (!rule.IsActive)
        {
            return (false, "Rule is not active");
        }

        var now = checkTime ?? DateTime.UtcNow;
        if (!rule.AppliesToTime(now))
        {
            return (false, "Rule is not active at this time");
        }

        if (rule.IsUsageLimitReached)
        {
            return (false, "Rule usage limit reached");
        }

        // Check member tier requirement
        if (rule.MinimumTier.HasValue)
        {
            var member = await _memberRepository.GetByIdAsync(memberId, cancellationToken).ConfigureAwait(false);
            if (member == null || (int)member.Tier < (int)rule.MinimumTier.Value)
            {
                return (false, $"Requires {rule.MinimumTier.Value} tier or higher");
            }
        }

        // Check member-specific usage limit
        if (rule.MaxUsagesPerMember.HasValue)
        {
            var memberUsageCount = await GetMemberRuleUsageCountAsync(ruleId, memberId, cancellationToken).ConfigureAwait(false);
            if (memberUsageCount >= rule.MaxUsagesPerMember.Value)
            {
                return (false, "Member has reached maximum usages for this rule");
            }
        }

        return (true, null);
    }

    #endregion

    #region Analytics

    /// <inheritdoc />
    public async Task<RuleUsageStatisticsDto> GetRuleUsageStatisticsAsync(
        int ruleId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var rule = await _ruleRepository.GetByIdAsync(ruleId, cancellationToken).ConfigureAwait(false);
        if (rule == null)
        {
            return new RuleUsageStatisticsDto { RuleId = ruleId };
        }

        var usages = await _usageRepository.Query()
            .Where(u => u.PointsMultiplierRuleId == ruleId)
            .Where(u => u.UsedAt >= startDate && u.UsedAt <= endDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new RuleUsageStatisticsDto
        {
            RuleId = ruleId,
            RuleName = rule.Name,
            TotalUsages = usages.Count,
            UniqueMembersCount = usages.Select(u => u.LoyaltyMemberId).Distinct().Count(),
            TotalBonusPointsAwarded = usages.Sum(u => u.BonusPointsEarned),
            RemainingUsages = rule.MaxTotalUsages.HasValue ? rule.MaxTotalUsages.Value - rule.CurrentUsageCount : null
        };
    }

    /// <inheritdoc />
    public async Task<List<ProductBonusPointsSummary>> GetTopBonusPointsProductsAsync(
        int topN,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        // This would require joining with transaction items to get product-level data
        // For now, return products with highest configured multipliers
        var products = await _productRepository.Query()
            .Include(p => p.Category)
            .Where(p => p.PointsMultiplier.HasValue && p.PointsMultiplier > 1.0m)
            .OrderByDescending(p => p.PointsMultiplier)
            .Take(topN)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return products.Select(p => new ProductBonusPointsSummary
        {
            ProductId = p.Id,
            ProductName = p.Name,
            CategoryName = p.Category?.Name,
            CurrentMultiplier = p.PointsMultiplier ?? 1.0m,
            TotalBonusPoints = 0, // Would require transaction analysis
            BonusEarnCount = 0
        }).ToList();
    }

    #endregion

    #region Private Methods

    private async Task<PointsConfiguration?> GetPointsConfigurationAsync(CancellationToken cancellationToken)
    {
        var configs = await _pointsConfigRepository
            .FindAsync(c => c.IsDefault && c.IsActive, cancellationToken)
            .ConfigureAwait(false);

        return configs.FirstOrDefault();
    }

    private static decimal GetTierMultiplier(MembershipTier tier)
    {
        return tier switch
        {
            MembershipTier.Bronze => 1.0m,
            MembershipTier.Silver => 1.25m,
            MembershipTier.Gold => 1.5m,
            MembershipTier.Platinum => 2.0m,
            _ => 1.0m
        };
    }

    private async Task<List<PointsMultiplierRule>> GetApplicableRulesInternalAsync(
        List<int> productIds,
        int? storeId,
        MembershipTier? memberTier,
        DateTime checkTime,
        CancellationToken cancellationToken)
    {
        // Get products with categories
        var products = await _productRepository
            .Query()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.CategoryId })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var categoryIds = products.Where(p => p.CategoryId.HasValue).Select(p => p.CategoryId!.Value).Distinct().ToList();

        var rules = await _ruleRepository.Query()
            .Where(r => r.IsActive)
            .Where(r =>
                productIds.Contains(r.ProductId ?? 0) ||
                categoryIds.Contains(r.CategoryId ?? 0) ||
                r.RuleType == PointsMultiplierRuleType.Global ||
                r.RuleType == PointsMultiplierRuleType.DayOfWeek ||
                r.RuleType == PointsMultiplierRuleType.TimeOfDay ||
                r.RuleType == PointsMultiplierRuleType.TierBased)
            .Where(r => !r.StoreId.HasValue || r.StoreId == storeId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Filter by time and tier
        return rules
            .Where(r => r.AppliesToTime(checkTime))
            .Where(r => !r.IsUsageLimitReached)
            .Where(r => !r.MinimumTier.HasValue || (memberTier.HasValue && (int)memberTier.Value >= (int)r.MinimumTier.Value))
            .ToList();
    }

    private (decimal multiplier, string source, string? ruleName) DetermineMultiplier(
        Product product,
        List<PointsMultiplierRule> applicableRules,
        int productId,
        decimal quantity)
    {
        // Priority: Product-level > Category-level > Promotional rules > Default

        // Check product-level multiplier
        if (product.PointsMultiplier.HasValue && product.PointsMultiplier.Value != 1.0m)
        {
            return (product.PointsMultiplier.Value, "Product", null);
        }

        // Check category-level multiplier
        if (product.Category?.PointsMultiplier.HasValue == true && product.Category.PointsMultiplier.Value != 1.0m)
        {
            return (product.Category.PointsMultiplier.Value, "Category", null);
        }

        // Check promotional rules
        var productRules = applicableRules
            .Where(r => r.ProductId == productId ||
                       r.CategoryId == product.CategoryId ||
                       r.RuleType == PointsMultiplierRuleType.Global ||
                       r.RuleType == PointsMultiplierRuleType.DayOfWeek ||
                       r.RuleType == PointsMultiplierRuleType.TimeOfDay)
            .Where(r => !r.MinimumQuantity.HasValue || quantity >= r.MinimumQuantity.Value)
            .OrderByDescending(r => r.Priority)
            .ThenByDescending(r => r.Multiplier)
            .ToList();

        if (productRules.Count > 0)
        {
            // Check if rules can stack
            var stackableRules = productRules.Where(r => r.IsStackable).ToList();
            var nonStackableRules = productRules.Where(r => !r.IsStackable).ToList();

            if (stackableRules.Count > 0 && nonStackableRules.Count > 0)
            {
                // Use highest non-stackable rule
                var bestNonStackable = nonStackableRules.First();
                return (bestNonStackable.Multiplier, "Promotion", bestNonStackable.Name);
            }
            else if (nonStackableRules.Count > 0)
            {
                var bestRule = nonStackableRules.First();
                return (bestRule.Multiplier, "Promotion", bestRule.Name);
            }
            else if (stackableRules.Count > 0)
            {
                // Multiply all stackable multipliers
                var combinedMultiplier = stackableRules.Aggregate(1.0m, (current, rule) => current * rule.Multiplier);
                return (combinedMultiplier, "Promotion (Stacked)", string.Join(", ", stackableRules.Select(r => r.Name)));
            }
        }

        return (1.0m, "Default", null);
    }

    private static PointsMultiplierRuleDto MapRuleToDto(PointsMultiplierRule rule)
    {
        return new PointsMultiplierRuleDto
        {
            Id = rule.Id,
            Name = rule.Name,
            Description = rule.Description,
            RuleType = (PointsMultiplierRuleTypeDto)rule.RuleType,
            Multiplier = rule.Multiplier,
            IsStackable = rule.IsStackable,
            Priority = rule.Priority,
            ProductId = rule.ProductId,
            ProductName = rule.Product?.Name,
            CategoryId = rule.CategoryId,
            CategoryName = rule.Category?.Name,
            MinimumTier = rule.MinimumTier,
            StartDate = rule.StartDate,
            EndDate = rule.EndDate,
            DaysOfWeek = rule.DaysOfWeek,
            StartTime = rule.StartTime,
            EndTime = rule.EndTime,
            MinimumPurchaseAmount = rule.MinimumPurchaseAmount,
            MinimumQuantity = rule.MinimumQuantity,
            MaxBonusPointsPerTransaction = rule.MaxBonusPointsPerTransaction,
            MaxTotalUsages = rule.MaxTotalUsages,
            CurrentUsageCount = rule.CurrentUsageCount,
            MaxUsagesPerMember = rule.MaxUsagesPerMember,
            StoreId = rule.StoreId,
            IsActive = rule.IsActive,
            IsCurrentlyActive = rule.IsCurrentlyActive
        };
    }

    #endregion
}
