using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for dynamic pricing operations.
/// </summary>
public class DynamicPricingService : IDynamicPricingService
{
    private readonly IDbContextFactory<POSDbContext> _contextFactory;
    private readonly ILogger<DynamicPricingService> _logger;

    public DynamicPricingService(
        IDbContextFactory<POSDbContext> contextFactory,
        ILogger<DynamicPricingService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    #region Configuration

    public async Task<DynamicPricingConfigurationDto> GetConfigurationAsync(int storeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var config = await context.Set<Core.Entities.DynamicPricingConfiguration>()
            .FirstOrDefaultAsync(c => c.StoreId == storeId);

        if (config == null)
        {
            return new DynamicPricingConfigurationDto
            {
                StoreId = storeId,
                EnableDynamicPricing = false,
                RequireManagerApproval = true,
                MaxPriceIncreasePercent = 25m,
                MaxPriceDecreasePercent = 50m,
                PriceUpdateIntervalMinutes = 15,
                ShowOriginalPrice = true,
                NotifyOnPriceChange = true,
                MinMarginPercent = 10m
            };
        }

        return MapToConfigurationDto(config);
    }

    public async Task<DynamicPricingConfigurationDto> UpdateConfigurationAsync(DynamicPricingConfigurationDto dto)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var config = await context.Set<Core.Entities.DynamicPricingConfiguration>()
            .FirstOrDefaultAsync(c => c.StoreId == dto.StoreId);

        if (config == null)
        {
            config = new Core.Entities.DynamicPricingConfiguration
            {
                StoreId = dto.StoreId,
                CreatedAt = DateTime.UtcNow
            };
            context.Set<Core.Entities.DynamicPricingConfiguration>().Add(config);
        }

        config.EnableDynamicPricing = dto.EnableDynamicPricing;
        config.RequireManagerApproval = dto.RequireManagerApproval;
        config.MaxPriceIncreasePercent = dto.MaxPriceIncreasePercent;
        config.MaxPriceDecreasePercent = dto.MaxPriceDecreasePercent;
        config.PriceUpdateIntervalMinutes = dto.PriceUpdateIntervalMinutes;
        config.ShowOriginalPrice = dto.ShowOriginalPrice;
        config.NotifyOnPriceChange = dto.NotifyOnPriceChange;
        config.MinMarginPercent = dto.MinMarginPercent;
        config.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        dto.Id = config.Id;
        return dto;
    }

    public async Task<bool> IsDynamicPricingEnabledAsync(int storeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var config = await context.Set<Core.Entities.DynamicPricingConfiguration>()
            .FirstOrDefaultAsync(c => c.StoreId == storeId);
        return config?.EnableDynamicPricing ?? false;
    }

    #endregion

    #region Price Calculation

    public async Task<DynamicPriceDto> GetDynamicPriceAsync(int productId, int storeId, DateTime? asOf = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Check for cached current price
        var cached = await context.Set<CurrentDynamicPrice>()
            .Include(p => p.AppliedRule)
            .FirstOrDefaultAsync(p => p.ProductId == productId && p.StoreId == storeId);

        if (cached != null && cached.ExpiresAt > DateTime.UtcNow && asOf == null)
        {
            return new DynamicPriceDto
            {
                ProductId = productId,
                BasePrice = cached.BasePrice,
                CurrentPrice = cached.CurrentPrice,
                AdjustmentAmount = cached.CurrentPrice - cached.BasePrice,
                AdjustmentPercent = cached.BasePrice > 0 ? ((cached.CurrentPrice - cached.BasePrice) / cached.BasePrice) * 100 : 0,
                IsAdjusted = cached.IsAdjusted,
                AppliedRuleId = cached.AppliedRuleId,
                AppliedRuleName = cached.AppliedRule?.Name,
                CalculatedAt = cached.CalculatedAt,
                ExpiresAt = cached.ExpiresAt
            };
        }

        // Calculate fresh
        var pricingContext = new DynamicPricingContext
        {
            CurrentTime = asOf ?? DateTime.Now,
            DayOfWeek = (asOf ?? DateTime.Now).DayOfWeek,
            StoreId = storeId
        };

        return await CalculatePriceAsync(productId, pricingContext);
    }

    public async Task<List<DynamicPriceDto>> GetDynamicPricesAsync(List<int> productIds, int storeId)
    {
        var results = new List<DynamicPriceDto>();
        foreach (var productId in productIds)
        {
            results.Add(await GetDynamicPriceAsync(productId, storeId));
        }
        return results;
    }

    public async Task<DynamicPriceDto> CalculatePriceAsync(int productId, DynamicPricingContext pricingContext)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var product = await context.Products.FindAsync(productId);
        if (product == null)
        {
            throw new InvalidOperationException($"Product {productId} not found");
        }

        var basePrice = product.Price;
        var result = new DynamicPriceDto
        {
            ProductId = productId,
            ProductName = product.Name,
            BasePrice = basePrice,
            CurrentPrice = basePrice,
            IsAdjusted = false,
            CalculatedAt = DateTime.UtcNow
        };

        // Get configuration
        var config = await GetConfigurationAsync(pricingContext.StoreId);
        if (!config.EnableDynamicPricing)
        {
            return result;
        }

        // Get applicable rules
        var rules = await GetApplicableRulesAsync(context, productId, product.CategoryId, pricingContext);

        if (!rules.Any())
        {
            return result;
        }

        // Apply highest priority rule
        var winningRule = rules.OrderByDescending(r => r.Priority).First();
        var adjustedPrice = ApplyAdjustment(basePrice, winningRule, config);

        result.CurrentPrice = adjustedPrice;
        result.AdjustmentAmount = adjustedPrice - basePrice;
        result.AdjustmentPercent = basePrice > 0 ? ((adjustedPrice - basePrice) / basePrice) * 100 : 0;
        result.IsAdjusted = adjustedPrice != basePrice;
        result.AppliedRuleId = winningRule.Id;
        result.AppliedRuleName = winningRule.Name;
        result.AdjustmentReason = GetAdjustmentReason(winningRule);
        result.ExpiresAt = CalculateExpiryTime(winningRule, pricingContext);

        return result;
    }

    public async Task<decimal> GetCurrentPriceAsync(int productId, int storeId)
    {
        var price = await GetDynamicPriceAsync(productId, storeId);
        return price.CurrentPrice;
    }

    public async Task<List<DynamicPriceDto>> GetAllCurrentPricesAsync(int storeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var cachedPrices = await context.Set<CurrentDynamicPrice>()
            .Include(p => p.Product)
            .Include(p => p.AppliedRule)
            .Where(p => p.StoreId == storeId)
            .ToListAsync();

        return cachedPrices.Select(c => new DynamicPriceDto
        {
            ProductId = c.ProductId,
            ProductName = c.Product?.Name ?? "",
            BasePrice = c.BasePrice,
            CurrentPrice = c.CurrentPrice,
            AdjustmentAmount = c.CurrentPrice - c.BasePrice,
            AdjustmentPercent = c.BasePrice > 0 ? ((c.CurrentPrice - c.BasePrice) / c.BasePrice) * 100 : 0,
            IsAdjusted = c.IsAdjusted,
            AppliedRuleId = c.AppliedRuleId,
            AppliedRuleName = c.AppliedRule?.Name,
            CalculatedAt = c.CalculatedAt,
            ExpiresAt = c.ExpiresAt
        }).ToList();
    }

    #endregion

    #region Rule Management

    public async Task<List<DynamicPricingRuleDto>> GetRulesAsync(int? storeId = null, bool includeInactive = false)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.Set<DynamicPricingRule>()
            .Include(r => r.Product)
            .Include(r => r.Category)
            .Include(r => r.CreatedByUser)
            .Include(r => r.Exceptions)
            .AsQueryable();

        if (storeId.HasValue)
        {
            query = query.Where(r => r.StoreId == null || r.StoreId == storeId);
        }

        if (!includeInactive)
        {
            query = query.Where(r => r.IsActive);
        }

        var rules = await query.OrderByDescending(r => r.Priority).ToListAsync();
        return rules.Select(MapToRuleDto).ToList();
    }

    public async Task<List<DynamicPricingRuleDto>> GetActiveRulesAsync(int? storeId = null)
    {
        return await GetRulesAsync(storeId, includeInactive: false);
    }

    public async Task<List<DynamicPricingRuleDto>> GetRulesForProductAsync(int productId, int storeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var product = await context.Products.FindAsync(productId);
        if (product == null)
        {
            return new List<DynamicPricingRuleDto>();
        }

        var rules = await context.Set<DynamicPricingRule>()
            .Include(r => r.Product)
            .Include(r => r.Category)
            .Include(r => r.Exceptions)
            .Where(r => r.IsActive)
            .Where(r => r.StoreId == null || r.StoreId == storeId)
            .Where(r =>
                r.AppliesToAllProducts ||
                r.ProductId == productId ||
                r.CategoryId == product.CategoryId)
            .Where(r => !r.Exceptions.Any(e => e.ProductId == productId))
            .OrderByDescending(r => r.Priority)
            .ToListAsync();

        return rules.Select(MapToRuleDto).ToList();
    }

    public async Task<DynamicPricingRuleDto?> GetRuleAsync(int ruleId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var rule = await context.Set<DynamicPricingRule>()
            .Include(r => r.Product)
            .Include(r => r.Category)
            .Include(r => r.CreatedByUser)
            .Include(r => r.Exceptions)
            .FirstOrDefaultAsync(r => r.Id == ruleId);

        return rule != null ? MapToRuleDto(rule) : null;
    }

    public async Task<DynamicPricingRuleDto> CreateRuleAsync(CreateDynamicPricingRuleRequest request, int userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var rule = new DynamicPricingRule
        {
            Name = request.Name,
            Description = request.Description,
            Trigger = request.Trigger,
            AdjustmentType = request.AdjustmentType,
            AdjustmentValue = request.AdjustmentValue,
            MinPrice = request.MinPrice,
            MaxPrice = request.MaxPrice,
            ProductId = request.ProductId,
            CategoryId = request.CategoryId,
            AppliesToAllProducts = request.AppliesToAllProducts,
            Priority = request.Priority,
            IsActive = request.IsActive,
            RequiresApproval = request.RequiresApproval,
            StoreId = request.StoreId,
            CreatedByUserId = userId,
            ActiveFromTime = request.ActiveFromTime,
            ActiveToTime = request.ActiveToTime,
            ActiveDays = request.ActiveDays.Any() ? string.Join(",", request.ActiveDays.Select(d => (int)d)) : null,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            DemandThresholdHigh = request.DemandThresholdHigh,
            DemandThresholdLow = request.DemandThresholdLow,
            StockThresholdLow = request.StockThresholdLow,
            StockThresholdHigh = request.StockThresholdHigh,
            DaysToExpiry = request.DaysToExpiry,
            WeatherCondition = request.WeatherCondition,
            EventName = request.EventName,
            CreatedAt = DateTime.UtcNow
        };

        context.Set<DynamicPricingRule>().Add(rule);
        await context.SaveChangesAsync();

        // Add exceptions
        foreach (var excludedProductId in request.ExcludedProductIds)
        {
            context.Set<DynamicPricingException>().Add(new DynamicPricingException
            {
                RuleId = rule.Id,
                ProductId = excludedProductId,
                CreatedAt = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();

        _logger.LogInformation("Created dynamic pricing rule {RuleId}: {RuleName}", rule.Id, rule.Name);

        return (await GetRuleAsync(rule.Id))!;
    }

    public async Task<DynamicPricingRuleDto> UpdateRuleAsync(int ruleId, CreateDynamicPricingRuleRequest request)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var rule = await context.Set<DynamicPricingRule>()
            .Include(r => r.Exceptions)
            .FirstOrDefaultAsync(r => r.Id == ruleId);

        if (rule == null)
        {
            throw new InvalidOperationException($"Rule {ruleId} not found");
        }

        rule.Name = request.Name;
        rule.Description = request.Description;
        rule.Trigger = request.Trigger;
        rule.AdjustmentType = request.AdjustmentType;
        rule.AdjustmentValue = request.AdjustmentValue;
        rule.MinPrice = request.MinPrice;
        rule.MaxPrice = request.MaxPrice;
        rule.ProductId = request.ProductId;
        rule.CategoryId = request.CategoryId;
        rule.AppliesToAllProducts = request.AppliesToAllProducts;
        rule.Priority = request.Priority;
        rule.IsActive = request.IsActive;
        rule.RequiresApproval = request.RequiresApproval;
        rule.StoreId = request.StoreId;
        rule.ActiveFromTime = request.ActiveFromTime;
        rule.ActiveToTime = request.ActiveToTime;
        rule.ActiveDays = request.ActiveDays.Any() ? string.Join(",", request.ActiveDays.Select(d => (int)d)) : null;
        rule.StartDate = request.StartDate;
        rule.EndDate = request.EndDate;
        rule.DemandThresholdHigh = request.DemandThresholdHigh;
        rule.DemandThresholdLow = request.DemandThresholdLow;
        rule.StockThresholdLow = request.StockThresholdLow;
        rule.StockThresholdHigh = request.StockThresholdHigh;
        rule.DaysToExpiry = request.DaysToExpiry;
        rule.WeatherCondition = request.WeatherCondition;
        rule.EventName = request.EventName;
        rule.UpdatedAt = DateTime.UtcNow;

        // Update exceptions
        context.Set<DynamicPricingException>().RemoveRange(rule.Exceptions);
        foreach (var excludedProductId in request.ExcludedProductIds)
        {
            context.Set<DynamicPricingException>().Add(new DynamicPricingException
            {
                RuleId = rule.Id,
                ProductId = excludedProductId,
                CreatedAt = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();

        _logger.LogInformation("Updated dynamic pricing rule {RuleId}", ruleId);

        return (await GetRuleAsync(ruleId))!;
    }

    public async Task DeleteRuleAsync(int ruleId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var rule = await context.Set<DynamicPricingRule>()
            .Include(r => r.Exceptions)
            .FirstOrDefaultAsync(r => r.Id == ruleId);

        if (rule != null)
        {
            context.Set<DynamicPricingException>().RemoveRange(rule.Exceptions);
            context.Set<DynamicPricingRule>().Remove(rule);
            await context.SaveChangesAsync();

            _logger.LogInformation("Deleted dynamic pricing rule {RuleId}", ruleId);
        }
    }

    public async Task ActivateRuleAsync(int ruleId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var rule = await context.Set<DynamicPricingRule>().FindAsync(ruleId);
        if (rule != null)
        {
            rule.IsActive = true;
            rule.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    public async Task DeactivateRuleAsync(int ruleId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var rule = await context.Set<DynamicPricingRule>().FindAsync(ruleId);
        if (rule != null)
        {
            rule.IsActive = false;
            rule.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    public async Task AddRuleExceptionAsync(int ruleId, int productId, string? reason = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var existing = await context.Set<DynamicPricingException>()
            .FirstOrDefaultAsync(e => e.RuleId == ruleId && e.ProductId == productId);

        if (existing == null)
        {
            context.Set<DynamicPricingException>().Add(new DynamicPricingException
            {
                RuleId = ruleId,
                ProductId = productId,
                Reason = reason,
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }
    }

    public async Task RemoveRuleExceptionAsync(int ruleId, int productId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var exception = await context.Set<DynamicPricingException>()
            .FirstOrDefaultAsync(e => e.RuleId == ruleId && e.ProductId == productId);

        if (exception != null)
        {
            context.Set<DynamicPricingException>().Remove(exception);
            await context.SaveChangesAsync();
        }
    }

    #endregion

    #region Batch Operations

    public async Task<PriceUpdateResult> ApplyDynamicPricingAsync(int storeId)
    {
        var result = new PriceUpdateResult { ProcessedAt = DateTime.UtcNow };

        await using var context = await _contextFactory.CreateDbContextAsync();

        var config = await GetConfigurationAsync(storeId);
        if (!config.EnableDynamicPricing)
        {
            return result;
        }

        var products = await context.Products
            .Where(p => p.IsActive)
            .ToListAsync();

        var pricingContext = new DynamicPricingContext
        {
            CurrentTime = DateTime.Now,
            DayOfWeek = DateTime.Now.DayOfWeek,
            StoreId = storeId
        };

        foreach (var product in products)
        {
            result.ProductsEvaluated++;

            var dynamicPrice = await CalculatePriceAsync(product.Id, pricingContext);

            if (dynamicPrice.IsAdjusted)
            {
                // Update or create cached price
                var cached = await context.Set<CurrentDynamicPrice>()
                    .FirstOrDefaultAsync(p => p.ProductId == product.Id && p.StoreId == storeId);

                if (cached == null)
                {
                    cached = new CurrentDynamicPrice
                    {
                        ProductId = product.Id,
                        StoreId = storeId,
                        CreatedAt = DateTime.UtcNow
                    };
                    context.Set<CurrentDynamicPrice>().Add(cached);
                }

                cached.BasePrice = dynamicPrice.BasePrice;
                cached.CurrentPrice = dynamicPrice.CurrentPrice;
                cached.AppliedRuleId = dynamicPrice.AppliedRuleId;
                cached.CalculatedAt = DateTime.UtcNow;
                cached.ExpiresAt = dynamicPrice.ExpiresAt ?? DateTime.UtcNow.AddMinutes(config.PriceUpdateIntervalMinutes);
                cached.IsAdjusted = true;
                cached.UpdatedAt = DateTime.UtcNow;

                result.PricesChanged++;
                if (dynamicPrice.AdjustmentAmount > 0)
                {
                    result.PricesIncreased++;
                }
                else
                {
                    result.PricesDecreased++;
                }

                result.Changes.Add(new PriceChangePreview
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    CurrentPrice = dynamicPrice.BasePrice,
                    NewPrice = dynamicPrice.CurrentPrice,
                    ChangeAmount = dynamicPrice.AdjustmentAmount,
                    ChangePercent = dynamicPrice.AdjustmentPercent,
                    RuleId = dynamicPrice.AppliedRuleId,
                    RuleName = dynamicPrice.AppliedRuleName,
                    Reason = dynamicPrice.AdjustmentReason ?? ""
                });
            }
        }

        await context.SaveChangesAsync();

        _logger.LogInformation("Applied dynamic pricing: {Changed}/{Evaluated} products updated",
            result.PricesChanged, result.ProductsEvaluated);

        return result;
    }

    public async Task<DynamicPricingJobResult> RefreshAllPricesAsync(int storeId)
    {
        var result = new DynamicPricingJobResult { StartTime = DateTime.UtcNow };

        try
        {
            var updateResult = await ApplyDynamicPricingAsync(storeId);
            result.ProductsEvaluated = updateResult.ProductsEvaluated;
            result.PricesUpdated = updateResult.PricesChanged;

            // Update daily metrics
            await UpdateDailyMetricsAsync(storeId, DateTime.UtcNow.Date);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing prices for store {StoreId}", storeId);
            result.Errors.Add(ex.Message);
        }

        result.EndTime = DateTime.UtcNow;
        return result;
    }

    public async Task<PriceUpdateResult> PreviewPriceChangesAsync(int storeId)
    {
        // Similar to ApplyDynamicPricingAsync but doesn't save changes
        var result = new PriceUpdateResult { ProcessedAt = DateTime.UtcNow };

        await using var context = await _contextFactory.CreateDbContextAsync();

        var config = await GetConfigurationAsync(storeId);
        if (!config.EnableDynamicPricing)
        {
            return result;
        }

        var products = await context.Products
            .Where(p => p.IsActive)
            .ToListAsync();

        var pricingContext = new DynamicPricingContext
        {
            CurrentTime = DateTime.Now,
            DayOfWeek = DateTime.Now.DayOfWeek,
            StoreId = storeId
        };

        foreach (var product in products)
        {
            result.ProductsEvaluated++;

            var dynamicPrice = await CalculatePriceAsync(product.Id, pricingContext);

            if (dynamicPrice.IsAdjusted)
            {
                result.PricesChanged++;
                if (dynamicPrice.AdjustmentAmount > 0)
                {
                    result.PricesIncreased++;
                }
                else
                {
                    result.PricesDecreased++;
                }

                result.Changes.Add(new PriceChangePreview
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    CurrentPrice = dynamicPrice.BasePrice,
                    NewPrice = dynamicPrice.CurrentPrice,
                    ChangeAmount = dynamicPrice.AdjustmentAmount,
                    ChangePercent = dynamicPrice.AdjustmentPercent,
                    RuleId = dynamicPrice.AppliedRuleId,
                    RuleName = dynamicPrice.AppliedRuleName,
                    Reason = dynamicPrice.AdjustmentReason ?? ""
                });
            }
        }

        return result;
    }

    public async Task<ExpiryPricingJobResult> ApplyExpiryDiscountsAsync(int storeId)
    {
        var result = new ExpiryPricingJobResult { StartTime = DateTime.UtcNow };

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Find products with expiring batches
            var expiryRules = await context.Set<DynamicPricingRule>()
                .Where(r => r.IsActive)
                .Where(r => r.Trigger == DynamicPricingTrigger.ExpiryApproaching)
                .Where(r => r.StoreId == null || r.StoreId == storeId)
                .ToListAsync();

            foreach (var rule in expiryRules)
            {
                if (!rule.DaysToExpiry.HasValue)
                {
                    continue;
                }

                var expiryThreshold = DateTime.UtcNow.AddDays(rule.DaysToExpiry.Value);

                // Find batches expiring soon
                var expiringBatches = await context.ProductBatches
                    .Include(b => b.Product)
                    .Where(b => b.ExpiryDate <= expiryThreshold)
                    .Where(b => b.RemainingQuantity > 0)
                    .ToListAsync();

                result.ProductsWithExpiringBatches = expiringBatches.Select(b => b.ProductId).Distinct().Count();

                // Apply discounts
                var config = await GetConfigurationAsync(storeId);

                foreach (var batch in expiringBatches)
                {
                    var product = batch.Product;
                    if (product == null) continue;

                    var adjustedPrice = ApplyAdjustment(product.Price, rule, config);

                    if (adjustedPrice != product.Price)
                    {
                        result.PricesDiscounted++;
                        result.TotalDiscountValue += (product.Price - adjustedPrice) * batch.RemainingQuantity;

                        // Log the discount
                        await LogPriceChangeAsync(
                            product.Id,
                            rule.Id,
                            product.Price,
                            adjustedPrice,
                            $"Expiry discount - batch expires {batch.ExpiryDate:yyyy-MM-dd}",
                            storeId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying expiry discounts for store {StoreId}", storeId);
            result.Errors.Add(ex.Message);
        }

        result.EndTime = DateTime.UtcNow;
        return result;
    }

    #endregion

    #region Approval Workflow

    public async Task<PendingPriceChangeDto> RequestPriceChangeAsync(int productId, decimal newPrice, string reason, int requestedByUserId, int storeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var product = await context.Products.FindAsync(productId);
        if (product == null)
        {
            throw new InvalidOperationException($"Product {productId} not found");
        }

        var pending = new PendingPriceChange
        {
            ProductId = productId,
            CurrentPrice = product.Price,
            ProposedPrice = newPrice,
            Reason = reason,
            Status = PriceChangeStatus.Pending,
            RequestedByUserId = requestedByUserId,
            RequestedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            StoreId = storeId,
            CreatedAt = DateTime.UtcNow
        };

        context.Set<PendingPriceChange>().Add(pending);
        await context.SaveChangesAsync();

        return await GetPendingChangeDto(context, pending.Id);
    }

    public async Task<PendingPriceChangeDto> ApprovePriceChangeAsync(int pendingChangeId, int approverUserId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var pending = await context.Set<PendingPriceChange>()
            .FirstOrDefaultAsync(p => p.Id == pendingChangeId);

        if (pending == null)
        {
            throw new InvalidOperationException($"Pending change {pendingChangeId} not found");
        }

        if (pending.Status != PriceChangeStatus.Pending)
        {
            throw new InvalidOperationException($"Change is not pending (status: {pending.Status})");
        }

        pending.Status = PriceChangeStatus.Approved;
        pending.ReviewedByUserId = approverUserId;
        pending.ReviewedAt = DateTime.UtcNow;
        pending.UpdatedAt = DateTime.UtcNow;

        // Log the price change
        await LogPriceChangeAsync(
            pending.ProductId,
            pending.RuleId,
            pending.CurrentPrice,
            pending.ProposedPrice,
            pending.Reason ?? "Manual price change",
            pending.StoreId,
            approverUserId);

        await context.SaveChangesAsync();

        _logger.LogInformation("Approved price change {ChangeId} for product {ProductId}",
            pendingChangeId, pending.ProductId);

        return await GetPendingChangeDto(context, pendingChangeId);
    }

    public async Task<PendingPriceChangeDto> RejectPriceChangeAsync(int pendingChangeId, int rejecterUserId, string reason)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var pending = await context.Set<PendingPriceChange>()
            .FirstOrDefaultAsync(p => p.Id == pendingChangeId);

        if (pending == null)
        {
            throw new InvalidOperationException($"Pending change {pendingChangeId} not found");
        }

        pending.Status = PriceChangeStatus.Rejected;
        pending.ReviewedByUserId = rejecterUserId;
        pending.ReviewedAt = DateTime.UtcNow;
        pending.RejectionReason = reason;
        pending.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return await GetPendingChangeDto(context, pendingChangeId);
    }

    public async Task<List<PendingPriceChangeDto>> GetPendingApprovalsAsync(int storeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var pending = await context.Set<PendingPriceChange>()
            .Include(p => p.Product)
            .Include(p => p.Rule)
            .Include(p => p.RequestedByUser)
            .Where(p => p.StoreId == storeId)
            .Where(p => p.Status == PriceChangeStatus.Pending)
            .Where(p => p.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(p => p.RequestedAt)
            .ToListAsync();

        return pending.Select(MapToPendingChangeDto).ToList();
    }

    public async Task<int> GetPendingApprovalsCountAsync(int storeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Set<PendingPriceChange>()
            .CountAsync(p => p.StoreId == storeId &&
                            p.Status == PriceChangeStatus.Pending &&
                            p.ExpiresAt > DateTime.UtcNow);
    }

    public async Task ExpirePendingChangesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var expired = await context.Set<PendingPriceChange>()
            .Where(p => p.Status == PriceChangeStatus.Pending)
            .Where(p => p.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync();

        foreach (var change in expired)
        {
            change.Status = PriceChangeStatus.Expired;
            change.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();

        if (expired.Any())
        {
            _logger.LogInformation("Expired {Count} pending price changes", expired.Count);
        }
    }

    #endregion

    #region Simulation

    public async Task<PricingSimulation> SimulatePriceChangeAsync(int productId, decimal newPrice, int storeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var product = await context.Products.FindAsync(productId);
        if (product == null)
        {
            throw new InvalidOperationException($"Product {productId} not found");
        }

        var priceChange = newPrice - product.Price;
        var priceChangePercent = product.Price > 0 ? (priceChange / product.Price) * 100 : 0;

        // Get historical sales data for elasticity calculation
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var salesData = await context.OrderItems
            .Where(oi => oi.ProductId == productId)
            .Where(oi => oi.Order != null && oi.Order.CreatedAt >= thirtyDaysAgo)
            .GroupBy(oi => 1)
            .Select(g => new
            {
                TotalQuantity = g.Sum(oi => oi.Quantity),
                TotalRevenue = g.Sum(oi => oi.Total),
                AveragePrice = g.Average(oi => oi.UnitPrice)
            })
            .FirstOrDefaultAsync();

        // Simple elasticity estimation (would be more sophisticated in production)
        var elasticity = -1.2m; // Default assumption of elastic demand
        var estimatedDemandChange = priceChangePercent * elasticity;
        var currentMonthlyQuantity = salesData?.TotalQuantity ?? 100;
        var newEstimatedQuantity = currentMonthlyQuantity * (1 + (estimatedDemandChange / 100));
        var estimatedRevenueChange = (newPrice * newEstimatedQuantity) - (product.Price * currentMonthlyQuantity);

        var simulation = new PricingSimulation
        {
            ProductId = productId,
            ProductName = product.Name,
            CurrentPrice = product.Price,
            ProposedPrice = newPrice,
            PriceChange = priceChange,
            PriceChangePercent = priceChangePercent,
            EstimatedDemandChange = estimatedDemandChange,
            EstimatedRevenueChange = estimatedRevenueChange,
            PriceElasticity = elasticity,
            ConfidenceLevel = salesData != null ? 0.7m : 0.3m, // Lower confidence without data
            Risks = new List<string>(),
            Opportunities = new List<string>()
        };

        // Add risks and opportunities
        if (priceChange > 0)
        {
            simulation.Risks.Add($"Price increase may reduce demand by approximately {Math.Abs(estimatedDemandChange):F1}%");
            if (priceChangePercent > 20)
            {
                simulation.Risks.Add("Large price increase may lead to customer complaints");
            }
            simulation.Opportunities.Add("Higher margin per unit sold");
        }
        else
        {
            simulation.Opportunities.Add($"Price decrease may increase demand by approximately {Math.Abs(estimatedDemandChange):F1}%");
            simulation.Risks.Add("Reduced margin per unit");
            if (newPrice < product.CostPrice * 1.1m)
            {
                simulation.Risks.Add("Price is close to or below minimum margin threshold");
            }
        }

        simulation.Recommendation = estimatedRevenueChange > 0
            ? "Price change is projected to increase revenue"
            : "Price change may decrease revenue - consider carefully";

        return simulation;
    }

    public async Task<PricingSimulation> SimulateRuleAsync(CreateDynamicPricingRuleRequest rule, int storeId)
    {
        var affectedProducts = await PreviewAffectedProductsAsync(rule, storeId);

        var totalCurrentRevenue = affectedProducts.Sum(p => p.CurrentPrice);
        var totalProjectedRevenue = affectedProducts.Sum(p => p.ProjectedPrice);

        return new PricingSimulation
        {
            ProposedPrice = 0, // N/A for rule simulation
            PriceChange = affectedProducts.Average(p => p.ChangeAmount),
            PriceChangePercent = affectedProducts.Average(p => p.ChangePercent),
            EstimatedRevenueChange = totalProjectedRevenue - totalCurrentRevenue,
            ConfidenceLevel = 0.6m,
            Risks = new List<string>
            {
                $"Rule will affect {affectedProducts.Count} products"
            },
            Recommendation = $"Review affected products before activating rule"
        };
    }

    public async Task<List<AffectedProduct>> GetAffectedProductsAsync(int ruleId)
    {
        var rule = await GetRuleAsync(ruleId);
        if (rule == null)
        {
            return new List<AffectedProduct>();
        }

        return await PreviewAffectedProductsAsync(new CreateDynamicPricingRuleRequest
        {
            ProductId = rule.ProductId,
            CategoryId = rule.CategoryId,
            AppliesToAllProducts = rule.AppliesToAllProducts,
            AdjustmentType = rule.AdjustmentType,
            AdjustmentValue = rule.AdjustmentValue,
            MinPrice = rule.MinPrice,
            MaxPrice = rule.MaxPrice,
            ExcludedProductIds = rule.ExcludedProductIds,
            StoreId = rule.StoreId
        }, rule.StoreId ?? 1);
    }

    public async Task<List<AffectedProduct>> PreviewAffectedProductsAsync(CreateDynamicPricingRuleRequest rule, int storeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var config = await GetConfigurationAsync(storeId);

        var query = context.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive)
            .AsQueryable();

        if (rule.ProductId.HasValue)
        {
            query = query.Where(p => p.Id == rule.ProductId);
        }
        else if (rule.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == rule.CategoryId);
        }

        if (rule.ExcludedProductIds.Any())
        {
            query = query.Where(p => !rule.ExcludedProductIds.Contains(p.Id));
        }

        var products = await query.ToListAsync();

        var dummyRule = new DynamicPricingRule
        {
            AdjustmentType = rule.AdjustmentType,
            AdjustmentValue = rule.AdjustmentValue,
            MinPrice = rule.MinPrice,
            MaxPrice = rule.MaxPrice
        };

        return products.Select(p =>
        {
            var projectedPrice = ApplyAdjustment(p.Price, dummyRule, config);
            return new AffectedProduct
            {
                ProductId = p.Id,
                ProductName = p.Name,
                CategoryName = p.Category?.Name,
                CurrentPrice = p.Price,
                ProjectedPrice = projectedPrice,
                ChangeAmount = projectedPrice - p.Price,
                ChangePercent = p.Price > 0 ? ((projectedPrice - p.Price) / p.Price) * 100 : 0
            };
        }).ToList();
    }

    #endregion

    #region Analytics

    public async Task<DynamicPricingAnalytics> GetAnalyticsAsync(DateTime from, DateTime to, int? storeId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var logsQuery = context.Set<DynamicPriceLog>()
            .Where(l => l.AppliedAt >= from && l.AppliedAt <= to);

        if (storeId.HasValue)
        {
            logsQuery = logsQuery.Where(l => l.StoreId == storeId);
        }

        var logs = await logsQuery.ToListAsync();

        var dailyMetrics = await context.Set<DynamicPricingDailyMetrics>()
            .Where(m => m.Date >= from.Date && m.Date <= to.Date)
            .Where(m => !storeId.HasValue || m.StoreId == storeId)
            .OrderBy(m => m.Date)
            .Select(m => new DailyPricingMetrics
            {
                Date = m.Date,
                TotalPriceChanges = m.TotalPriceChanges,
                ProductsAffected = m.ProductsWithDynamicPricing,
                AverageAdjustmentPercent = m.AverageAdjustmentPercent,
                EstimatedRevenueImpact = m.EstimatedRevenueImpact
            })
            .ToListAsync();

        return new DynamicPricingAnalytics
        {
            FromDate = from,
            ToDate = to,
            TotalPriceChanges = logs.Count,
            ProductsAffected = logs.Select(l => l.ProductId).Distinct().Count(),
            RulesApplied = logs.Where(l => l.RuleId.HasValue).Select(l => l.RuleId).Distinct().Count(),
            AverageAdjustmentPercent = logs.Any() ? logs.Average(l => l.AdjustmentPercent) : 0,
            PriceIncreases = logs.Count(l => l.AdjustmentAmount > 0),
            PriceDecreases = logs.Count(l => l.AdjustmentAmount < 0),
            DailyMetrics = dailyMetrics
        };
    }

    public async Task<PriceElasticityReport> GetElasticityReportAsync(int productId, DateTime from, DateTime to)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var product = await context.Products.FindAsync(productId);
        if (product == null)
        {
            throw new InvalidOperationException($"Product {productId} not found");
        }

        // Get price points from order history
        var pricePoints = await context.OrderItems
            .Where(oi => oi.ProductId == productId)
            .Where(oi => oi.Order != null && oi.Order.CreatedAt >= from && oi.Order.CreatedAt <= to)
            .GroupBy(oi => new { oi.UnitPrice, oi.Order!.CreatedAt.Date })
            .Select(g => new PricePoint
            {
                Price = g.Key.UnitPrice,
                QuantitySold = g.Sum(oi => oi.Quantity),
                Revenue = g.Sum(oi => oi.Total),
                Date = g.Key.Date
            })
            .OrderBy(p => p.Date)
            .ToListAsync();

        // Simple elasticity calculation
        decimal elasticity = -1.0m;
        if (pricePoints.Count >= 2)
        {
            var distinctPrices = pricePoints.GroupBy(p => p.Price).ToList();
            if (distinctPrices.Count >= 2)
            {
                var lowPrice = distinctPrices.OrderBy(g => g.Key).First();
                var highPrice = distinctPrices.OrderByDescending(g => g.Key).First();

                var avgQtyLow = lowPrice.Average(p => p.QuantitySold);
                var avgQtyHigh = highPrice.Average(p => p.QuantitySold);

                var priceDiff = (highPrice.Key - lowPrice.Key) / lowPrice.Key;
                var qtyDiff = avgQtyLow > 0 ? (avgQtyHigh - avgQtyLow) / avgQtyLow : 0;

                if (priceDiff != 0)
                {
                    elasticity = qtyDiff / priceDiff;
                }
            }
        }

        var elasticityCategory = Math.Abs(elasticity) > 1 ? "Elastic" :
                                 Math.Abs(elasticity) < 1 ? "Inelastic" : "Unit Elastic";

        return new PriceElasticityReport
        {
            ProductId = productId,
            ProductName = product.Name,
            CurrentPrice = product.Price,
            Elasticity = elasticity,
            OptimalPrice = product.Price, // Would need more sophisticated calculation
            HistoricalPricePoints = pricePoints,
            ElasticityCategory = elasticityCategory,
            Recommendation = elasticityCategory == "Elastic"
                ? "Consider price decreases to increase volume"
                : "Product can sustain price increases"
        };
    }

    public async Task<RevenueImpactReport> GetRevenueImpactAsync(int ruleId, DateTime from, DateTime to)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var rule = await context.Set<DynamicPricingRule>().FindAsync(ruleId);
        if (rule == null)
        {
            throw new InvalidOperationException($"Rule {ruleId} not found");
        }

        var metrics = await context.Set<DynamicPricingRuleMetrics>()
            .Where(m => m.RuleId == ruleId)
            .Where(m => m.Date >= from.Date && m.Date <= to.Date)
            .ToListAsync();

        var totalRevenue = metrics.Sum(m => m.TotalSalesValue);
        var totalImpact = metrics.Sum(m => m.EstimatedRevenueImpact);

        return new RevenueImpactReport
        {
            RuleId = ruleId,
            RuleName = rule.Name,
            FromDate = from,
            ToDate = to,
            TotalRevenueWithRule = totalRevenue,
            EstimatedRevenueWithoutRule = totalRevenue - totalImpact,
            RevenueImpact = totalImpact,
            ImpactPercent = totalRevenue > 0 ? (totalImpact / totalRevenue) * 100 : 0,
            TimesApplied = metrics.Sum(m => m.TimesApplied),
            ProductsAffected = metrics.Max(m => m.ProductsAffected)
        };
    }

    public async Task<List<RulePerformance>> GetTopPerformingRulesAsync(int storeId, int count = 10)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        var ruleMetrics = await context.Set<DynamicPricingRuleMetrics>()
            .Include(m => m.Rule)
            .Where(m => m.StoreId == storeId)
            .Where(m => m.Date >= thirtyDaysAgo)
            .GroupBy(m => new { m.RuleId, RuleName = m.Rule!.Name })
            .Select(g => new RulePerformance
            {
                RuleId = g.Key.RuleId,
                RuleName = g.Key.RuleName,
                TimesApplied = g.Sum(m => m.TimesApplied),
                ProductsAffected = g.Max(m => m.ProductsAffected),
                TotalSalesValue = g.Sum(m => m.TotalSalesValue),
                EstimatedRevenueImpact = g.Sum(m => m.EstimatedRevenueImpact)
            })
            .OrderByDescending(r => r.EstimatedRevenueImpact)
            .Take(count)
            .ToListAsync();

        return ruleMetrics;
    }

    public async Task UpdateDailyMetricsAsync(int storeId, DateTime date)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var dateOnly = date.Date;

        var logs = await context.Set<DynamicPriceLog>()
            .Where(l => l.StoreId == storeId)
            .Where(l => l.AppliedAt.Date == dateOnly)
            .ToListAsync();

        var metrics = await context.Set<DynamicPricingDailyMetrics>()
            .FirstOrDefaultAsync(m => m.StoreId == storeId && m.Date == dateOnly);

        if (metrics == null)
        {
            metrics = new DynamicPricingDailyMetrics
            {
                StoreId = storeId,
                Date = dateOnly,
                CreatedAt = DateTime.UtcNow
            };
            context.Set<DynamicPricingDailyMetrics>().Add(metrics);
        }

        metrics.TotalPriceChanges = logs.Count;
        metrics.ProductsWithDynamicPricing = logs.Select(l => l.ProductId).Distinct().Count();
        metrics.PriceIncreases = logs.Count(l => l.AdjustmentAmount > 0);
        metrics.PriceDecreases = logs.Count(l => l.AdjustmentAmount < 0);
        metrics.AverageAdjustmentPercent = logs.Any() ? logs.Average(l => l.AdjustmentPercent) : 0;
        metrics.ActiveRulesCount = logs.Where(l => l.RuleId.HasValue).Select(l => l.RuleId).Distinct().Count();
        metrics.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }

    #endregion

    #region Price History

    public async Task<List<DynamicPriceLog>> GetPriceHistoryAsync(int productId, DateTime from, DateTime to)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Set<DynamicPriceLog>()
            .Include(l => l.Rule)
            .Include(l => l.ApprovedByUser)
            .Where(l => l.ProductId == productId)
            .Where(l => l.AppliedAt >= from && l.AppliedAt <= to)
            .OrderByDescending(l => l.AppliedAt)
            .ToListAsync();
    }

    public async Task LogPriceChangeAsync(int productId, int? ruleId, decimal originalPrice, decimal adjustedPrice, string reason, int storeId, int? approvedByUserId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var log = new DynamicPriceLog
        {
            ProductId = productId,
            RuleId = ruleId,
            OriginalPrice = originalPrice,
            AdjustedPrice = adjustedPrice,
            AdjustmentAmount = adjustedPrice - originalPrice,
            AdjustmentPercent = originalPrice > 0 ? ((adjustedPrice - originalPrice) / originalPrice) * 100 : 0,
            Reason = reason,
            AppliedAt = DateTime.UtcNow,
            ApprovedByUserId = approvedByUserId,
            StoreId = storeId,
            CreatedAt = DateTime.UtcNow
        };

        context.Set<DynamicPriceLog>().Add(log);
        await context.SaveChangesAsync();
    }

    #endregion

    #region Private Helpers

    private async Task<List<DynamicPricingRule>> GetApplicableRulesAsync(
        POSDbContext context,
        int productId,
        int? categoryId,
        DynamicPricingContext pricingContext)
    {
        var now = pricingContext.CurrentTime;
        var timeOnly = TimeOnly.FromDateTime(now);
        var dayOfWeek = (int)pricingContext.DayOfWeek;

        var rules = await context.Set<DynamicPricingRule>()
            .Include(r => r.Exceptions)
            .Where(r => r.IsActive)
            .Where(r => r.StoreId == null || r.StoreId == pricingContext.StoreId)
            .Where(r =>
                r.AppliesToAllProducts ||
                r.ProductId == productId ||
                (r.CategoryId != null && r.CategoryId == categoryId))
            .Where(r => !r.Exceptions.Any(e => e.ProductId == productId))
            .ToListAsync();

        return rules.Where(r => IsRuleApplicable(r, pricingContext, timeOnly, dayOfWeek)).ToList();
    }

    private static bool IsRuleApplicable(
        DynamicPricingRule rule,
        DynamicPricingContext context,
        TimeOnly currentTime,
        int dayOfWeek)
    {
        // Check date range
        if (rule.StartDate.HasValue && context.CurrentTime < rule.StartDate)
        {
            return false;
        }

        if (rule.EndDate.HasValue && context.CurrentTime > rule.EndDate)
        {
            return false;
        }

        // Check time of day
        if (rule.ActiveFromTime.HasValue && rule.ActiveToTime.HasValue)
        {
            if (rule.ActiveFromTime <= rule.ActiveToTime)
            {
                if (currentTime < rule.ActiveFromTime || currentTime > rule.ActiveToTime)
                {
                    return false;
                }
            }
            else // Spans midnight
            {
                if (currentTime < rule.ActiveFromTime && currentTime > rule.ActiveToTime)
                {
                    return false;
                }
            }
        }

        // Check days of week
        if (!string.IsNullOrEmpty(rule.ActiveDays))
        {
            var activeDays = rule.ActiveDays.Split(',').Select(int.Parse).ToList();
            if (!activeDays.Contains(dayOfWeek))
            {
                return false;
            }
        }

        // Check trigger-specific conditions
        return rule.Trigger switch
        {
            DynamicPricingTrigger.DemandLevel =>
                (rule.DemandThresholdHigh.HasValue && context.RecentDemandLevel >= rule.DemandThresholdHigh) ||
                (rule.DemandThresholdLow.HasValue && context.RecentDemandLevel <= rule.DemandThresholdLow),

            DynamicPricingTrigger.StockLevel =>
                (rule.StockThresholdLow.HasValue && context.CurrentStockLevel <= rule.StockThresholdLow) ||
                (rule.StockThresholdHigh.HasValue && context.CurrentStockLevel >= rule.StockThresholdHigh),

            DynamicPricingTrigger.ExpiryApproaching =>
                rule.DaysToExpiry.HasValue &&
                context.ExpiryDate.HasValue &&
                (context.ExpiryDate.Value - context.CurrentTime).TotalDays <= rule.DaysToExpiry,

            DynamicPricingTrigger.WeatherCondition =>
                !string.IsNullOrEmpty(rule.WeatherCondition) &&
                rule.WeatherCondition.Equals(context.WeatherCondition, StringComparison.OrdinalIgnoreCase),

            DynamicPricingTrigger.SpecialEvent =>
                context.IsSpecialEvent &&
                (string.IsNullOrEmpty(rule.EventName) ||
                 rule.EventName.Equals(context.EventName, StringComparison.OrdinalIgnoreCase)),

            _ => true // TimeOfDay, DayOfWeek, DateRange are handled above
        };
    }

    private static decimal ApplyAdjustment(
        decimal basePrice,
        DynamicPricingRule rule,
        DynamicPricingConfigurationDto config)
    {
        var adjustedPrice = rule.AdjustmentType switch
        {
            PriceAdjustmentType.PercentageDiscount => basePrice * (1 - (rule.AdjustmentValue / 100)),
            PriceAdjustmentType.PercentageIncrease => basePrice * (1 + (rule.AdjustmentValue / 100)),
            PriceAdjustmentType.FixedDiscount => basePrice - rule.AdjustmentValue,
            PriceAdjustmentType.FixedIncrease => basePrice + rule.AdjustmentValue,
            PriceAdjustmentType.SetPrice => rule.AdjustmentValue,
            PriceAdjustmentType.RoundTo => Math.Round(basePrice / rule.AdjustmentValue) * rule.AdjustmentValue,
            _ => basePrice
        };

        // Apply safety limits
        var maxIncrease = basePrice * (1 + (config.MaxPriceIncreasePercent / 100));
        var maxDecrease = basePrice * (1 - (config.MaxPriceDecreasePercent / 100));

        if (adjustedPrice > maxIncrease)
        {
            adjustedPrice = maxIncrease;
        }

        if (adjustedPrice < maxDecrease)
        {
            adjustedPrice = maxDecrease;
        }

        // Apply rule-specific limits
        if (rule.MinPrice.HasValue && adjustedPrice < rule.MinPrice)
        {
            adjustedPrice = rule.MinPrice.Value;
        }

        if (rule.MaxPrice.HasValue && adjustedPrice > rule.MaxPrice)
        {
            adjustedPrice = rule.MaxPrice.Value;
        }

        return Math.Max(adjustedPrice, 0);
    }

    private static string GetAdjustmentReason(DynamicPricingRule rule)
    {
        return rule.Trigger switch
        {
            DynamicPricingTrigger.TimeOfDay => $"Time-based: {rule.Name}",
            DynamicPricingTrigger.DayOfWeek => $"Day-based: {rule.Name}",
            DynamicPricingTrigger.DateRange => $"Date range: {rule.Name}",
            DynamicPricingTrigger.DemandLevel => $"Demand-based: {rule.Name}",
            DynamicPricingTrigger.StockLevel => $"Stock-based: {rule.Name}",
            DynamicPricingTrigger.ExpiryApproaching => $"Expiry discount: {rule.Name}",
            DynamicPricingTrigger.WeatherCondition => $"Weather-based: {rule.Name}",
            DynamicPricingTrigger.SpecialEvent => $"Event pricing: {rule.Name}",
            _ => rule.Name
        };
    }

    private static DateTime? CalculateExpiryTime(DynamicPricingRule rule, DynamicPricingContext context)
    {
        // Calculate when this price should be recalculated
        if (rule.ActiveToTime.HasValue)
        {
            var today = context.CurrentTime.Date;
            var expiryTime = today.Add(rule.ActiveToTime.Value.ToTimeSpan());
            if (expiryTime > context.CurrentTime)
            {
                return expiryTime;
            }
            return today.AddDays(1).Add(rule.ActiveToTime.Value.ToTimeSpan());
        }

        if (rule.EndDate.HasValue)
        {
            return rule.EndDate;
        }

        return context.CurrentTime.AddMinutes(15); // Default 15 minute cache
    }

    private static DynamicPricingConfigurationDto MapToConfigurationDto(Core.Entities.DynamicPricingConfiguration config)
    {
        return new DynamicPricingConfigurationDto
        {
            Id = config.Id,
            StoreId = config.StoreId,
            EnableDynamicPricing = config.EnableDynamicPricing,
            RequireManagerApproval = config.RequireManagerApproval,
            MaxPriceIncreasePercent = config.MaxPriceIncreasePercent,
            MaxPriceDecreasePercent = config.MaxPriceDecreasePercent,
            PriceUpdateIntervalMinutes = config.PriceUpdateIntervalMinutes,
            ShowOriginalPrice = config.ShowOriginalPrice,
            NotifyOnPriceChange = config.NotifyOnPriceChange,
            MinMarginPercent = config.MinMarginPercent
        };
    }

    private static DynamicPricingRuleDto MapToRuleDto(DynamicPricingRule rule)
    {
        var activeDays = new List<DayOfWeek>();
        if (!string.IsNullOrEmpty(rule.ActiveDays))
        {
            activeDays = rule.ActiveDays.Split(',')
                .Select(d => (DayOfWeek)int.Parse(d))
                .ToList();
        }

        return new DynamicPricingRuleDto
        {
            Id = rule.Id,
            Name = rule.Name,
            Description = rule.Description,
            Trigger = rule.Trigger,
            AdjustmentType = rule.AdjustmentType,
            AdjustmentValue = rule.AdjustmentValue,
            MinPrice = rule.MinPrice,
            MaxPrice = rule.MaxPrice,
            ProductId = rule.ProductId,
            ProductName = rule.Product?.Name,
            CategoryId = rule.CategoryId,
            CategoryName = rule.Category?.Name,
            AppliesToAllProducts = rule.AppliesToAllProducts,
            Priority = rule.Priority,
            IsActive = rule.IsActive,
            RequiresApproval = rule.RequiresApproval,
            StoreId = rule.StoreId,
            CreatedByUserId = rule.CreatedByUserId,
            CreatedByUserName = rule.CreatedByUser?.FullName,
            ActiveFromTime = rule.ActiveFromTime,
            ActiveToTime = rule.ActiveToTime,
            ActiveDays = activeDays,
            StartDate = rule.StartDate,
            EndDate = rule.EndDate,
            DemandThresholdHigh = rule.DemandThresholdHigh,
            DemandThresholdLow = rule.DemandThresholdLow,
            StockThresholdLow = rule.StockThresholdLow,
            StockThresholdHigh = rule.StockThresholdHigh,
            DaysToExpiry = rule.DaysToExpiry,
            WeatherCondition = rule.WeatherCondition,
            EventName = rule.EventName,
            ExcludedProductIds = rule.Exceptions.Select(e => e.ProductId).ToList(),
            CreatedAt = rule.CreatedAt
        };
    }

    private static PendingPriceChangeDto MapToPendingChangeDto(PendingPriceChange change)
    {
        return new PendingPriceChangeDto
        {
            Id = change.Id,
            ProductId = change.ProductId,
            ProductName = change.Product?.Name ?? "",
            RuleId = change.RuleId,
            RuleName = change.Rule?.Name,
            CurrentPrice = change.CurrentPrice,
            ProposedPrice = change.ProposedPrice,
            ChangeAmount = change.ProposedPrice - change.CurrentPrice,
            ChangePercent = change.CurrentPrice > 0 ? ((change.ProposedPrice - change.CurrentPrice) / change.CurrentPrice) * 100 : 0,
            Reason = change.Reason,
            Status = change.Status,
            RequestedByUserId = change.RequestedByUserId,
            RequestedByUserName = change.RequestedByUser?.FullName ?? "",
            RequestedAt = change.RequestedAt,
            ExpiresAt = change.ExpiresAt,
            ReviewedByUserId = change.ReviewedByUserId,
            ReviewedByUserName = change.ReviewedByUser?.FullName,
            ReviewedAt = change.ReviewedAt,
            RejectionReason = change.RejectionReason
        };
    }

    private async Task<PendingPriceChangeDto> GetPendingChangeDto(POSDbContext context, int id)
    {
        var change = await context.Set<PendingPriceChange>()
            .Include(p => p.Product)
            .Include(p => p.Rule)
            .Include(p => p.RequestedByUser)
            .Include(p => p.ReviewedByUser)
            .FirstAsync(p => p.Id == id);

        return MapToPendingChangeDto(change);
    }

    #endregion
}
