using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing advanced promotions including BOGO, Mix &amp; Match, Combos, and Coupons.
/// </summary>
public class AdvancedPromotionService : IAdvancedPromotionService
{
    private readonly POSDbContext _context;
    private readonly ILogger<AdvancedPromotionService> _logger;

    public AdvancedPromotionService(POSDbContext context, ILogger<AdvancedPromotionService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region BOGO Promotions

    /// <inheritdoc />
    public async Task<CentralPromotion> CreateBogoPromotionAsync(
        CentralPromotion promotion,
        BogoPromotion bogoConfig,
        IEnumerable<int> productIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(promotion);
        ArgumentNullException.ThrowIfNull(bogoConfig);

        promotion.Type = PromotionType.BOGO;
        await _context.CentralPromotions.AddAsync(promotion, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        bogoConfig.PromotionId = promotion.Id;
        await _context.BogoPromotions.AddAsync(bogoConfig, cancellationToken).ConfigureAwait(false);

        foreach (var productId in productIds)
        {
            await _context.PromotionProducts.AddAsync(new PromotionProduct
            {
                PromotionId = promotion.Id,
                ProductId = productId,
                IsQualifyingProduct = true,
                RequiredQuantity = bogoConfig.BuyQuantity
            }, cancellationToken).ConfigureAwait(false);
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Created BOGO promotion {PromotionId}: {Name}", promotion.Id, promotion.Name);

        return promotion;
    }

    /// <inheritdoc />
    public async Task<BogoPromotion?> GetBogoConfigurationAsync(int promotionId, CancellationToken cancellationToken = default)
    {
        return await _context.BogoPromotions
            .Include(b => b.Promotion)
            .Include(b => b.GetProduct)
            .Include(b => b.GetCategory)
            .FirstOrDefaultAsync(b => b.PromotionId == promotionId && b.IsActive, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<BogoCalculationResult> CalculateBogoDiscountAsync(
        IEnumerable<OrderItemInfo> items,
        CancellationToken cancellationToken = default)
    {
        var result = new BogoCalculationResult();
        var itemsList = items.ToList();

        var activeBogos = await GetActiveBogoPromotionsAsync(cancellationToken).ConfigureAwait(false);

        foreach (var bogo in activeBogos)
        {
            var promotion = bogo.Promotion;
            if (!promotion.IsCurrentlyActive) continue;

            var qualifyingProducts = await _context.PromotionProducts
                .Where(pp => pp.PromotionId == bogo.PromotionId && pp.IsQualifyingProduct && pp.IsActive)
                .Select(pp => pp.ProductId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var matchingItems = itemsList.Where(i => qualifyingProducts.Contains(i.ProductId)).ToList();
            var totalQualifyingQty = matchingItems.Sum(i => i.Quantity);

            if (totalQualifyingQty >= bogo.BuyQuantity)
            {
                var applications = CalculateBogoApplications(bogo, matchingItems, totalQualifyingQty);

                foreach (var app in applications)
                {
                    result.Applications.Add(app);
                    result.TotalDiscount += app.DiscountAmount;
                }
            }
        }

        result.HasDiscount = result.TotalDiscount > 0;
        return result;
    }

    private List<BogoApplicationDetail> CalculateBogoApplications(
        BogoPromotion bogo,
        List<OrderItemInfo> matchingItems,
        int totalQty)
    {
        var applications = new List<BogoApplicationDetail>();
        var requiredQty = bogo.BuyQuantity + bogo.GetQuantity;
        var applicationCount = totalQty / requiredQty;

        if (bogo.MaxApplicationsPerTransaction.HasValue)
        {
            applicationCount = Math.Min(applicationCount, bogo.MaxApplicationsPerTransaction.Value);
        }

        if (applicationCount <= 0) return applications;

        // Sort by price descending to apply discount to cheapest items
        var sortedItems = matchingItems.OrderByDescending(i => i.UnitPrice).ToList();
        var freeQtyRemaining = applicationCount * bogo.GetQuantity;

        decimal totalDiscount = 0;
        foreach (var item in sortedItems.AsEnumerable().Reverse()) // Start with cheapest
        {
            if (freeQtyRemaining <= 0) break;

            var freeFromThisItem = Math.Min(item.Quantity, freeQtyRemaining);
            var discountPerUnit = item.UnitPrice * (bogo.DiscountPercentOnGetItems / 100);
            totalDiscount += discountPerUnit * freeFromThisItem;
            freeQtyRemaining -= freeFromThisItem;
        }

        if (totalDiscount > 0)
        {
            applications.Add(new BogoApplicationDetail
            {
                PromotionId = bogo.PromotionId,
                PromotionName = bogo.Promotion.Name,
                BuyProductId = matchingItems.First().ProductId,
                GetProductId = bogo.GetProductId ?? matchingItems.First().ProductId,
                FreeQuantity = applicationCount * bogo.GetQuantity,
                DiscountAmount = totalDiscount
            });
        }

        return applications;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BogoPromotion>> GetActiveBogoPromotionsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.BogoPromotions
            .Include(b => b.Promotion)
            .Where(b => b.IsActive
                && b.Promotion.IsActive
                && b.Promotion.Status == PromotionStatus.Active
                && b.Promotion.StartDate <= now
                && b.Promotion.EndDate >= now)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    #endregion

    #region Mix & Match Promotions

    /// <inheritdoc />
    public async Task<CentralPromotion> CreateMixMatchPromotionAsync(
        CentralPromotion promotion,
        MixMatchPromotion mixMatchConfig,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(promotion);
        ArgumentNullException.ThrowIfNull(mixMatchConfig);

        await _context.CentralPromotions.AddAsync(promotion, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        mixMatchConfig.PromotionId = promotion.Id;
        await _context.MixMatchPromotions.AddAsync(mixMatchConfig, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Created Mix & Match promotion {PromotionId}: {Name}", promotion.Id, promotion.Name);
        return promotion;
    }

    /// <inheritdoc />
    public async Task<MixMatchPromotion?> GetMixMatchConfigurationAsync(int promotionId, CancellationToken cancellationToken = default)
    {
        return await _context.MixMatchPromotions
            .Include(m => m.Promotion)
            .Include(m => m.Groups)
                .ThenInclude(g => g.Products)
            .Include(m => m.Groups)
                .ThenInclude(g => g.Categories)
            .FirstOrDefaultAsync(m => m.PromotionId == promotionId && m.IsActive, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<MixMatchCalculationResult> CalculateMixMatchDiscountAsync(
        IEnumerable<OrderItemInfo> items,
        CancellationToken cancellationToken = default)
    {
        var result = new MixMatchCalculationResult();
        var itemsList = items.ToList();

        var activeMixMatches = await _context.MixMatchPromotions
            .Include(m => m.Promotion)
            .Include(m => m.Groups)
                .ThenInclude(g => g.Products)
            .Include(m => m.Groups)
                .ThenInclude(g => g.Categories)
            .Where(m => m.IsActive && m.Promotion.IsCurrentlyActive)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var mixMatch in activeMixMatches)
        {
            var eligibleProductIds = await GetMixMatchEligibleProductsAsync(mixMatch, cancellationToken).ConfigureAwait(false);
            var matchingItems = itemsList.Where(i => eligibleProductIds.Contains(i.ProductId)).ToList();
            var totalQty = matchingItems.Sum(i => i.Quantity);

            if (totalQty >= mixMatch.RequiredQuantity)
            {
                var applicationCount = totalQty / mixMatch.RequiredQuantity;
                if (mixMatch.MaxApplicationsPerTransaction.HasValue)
                {
                    applicationCount = Math.Min(applicationCount, mixMatch.MaxApplicationsPerTransaction.Value);
                }

                var sortedItems = matchingItems.OrderByDescending(i => i.UnitPrice).ToList();
                var includedQty = applicationCount * mixMatch.RequiredQuantity;
                decimal originalTotal = 0;
                var includedProductIds = new List<int>();

                foreach (var item in sortedItems)
                {
                    var qtyFromItem = Math.Min(item.Quantity, includedQty);
                    originalTotal += item.UnitPrice * qtyFromItem;
                    includedQty -= qtyFromItem;
                    if (!includedProductIds.Contains(item.ProductId))
                        includedProductIds.Add(item.ProductId);
                    if (includedQty <= 0) break;
                }

                decimal discountedTotal;
                if (mixMatch.MixMatchType == MixMatchType.AnyXForFixedPrice && mixMatch.FixedPrice.HasValue)
                {
                    discountedTotal = mixMatch.FixedPrice.Value * applicationCount;
                }
                else if (mixMatch.DiscountPercent.HasValue)
                {
                    discountedTotal = originalTotal * (1 - mixMatch.DiscountPercent.Value / 100);
                }
                else
                {
                    continue;
                }

                var discountAmount = originalTotal - discountedTotal;
                if (discountAmount > 0)
                {
                    result.Applications.Add(new MixMatchApplicationDetail
                    {
                        PromotionId = mixMatch.PromotionId,
                        PromotionName = mixMatch.Promotion.Name,
                        IncludedProductIds = includedProductIds,
                        OriginalTotal = originalTotal,
                        DiscountedTotal = discountedTotal,
                        DiscountAmount = discountAmount
                    });
                    result.TotalDiscount += discountAmount;
                }
            }
        }

        result.HasDiscount = result.TotalDiscount > 0;
        return result;
    }

    private async Task<HashSet<int>> GetMixMatchEligibleProductsAsync(MixMatchPromotion mixMatch, CancellationToken cancellationToken)
    {
        var productIds = new HashSet<int>();

        foreach (var group in mixMatch.Groups)
        {
            foreach (var gp in group.Products)
            {
                productIds.Add(gp.ProductId);
            }

            foreach (var gc in group.Categories)
            {
                var categoryProducts = await _context.Products
                    .Where(p => p.CategoryId == gc.CategoryId && p.IsActive)
                    .Select(p => p.Id)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
                foreach (var pid in categoryProducts)
                {
                    productIds.Add(pid);
                }
            }
        }

        return productIds;
    }

    /// <inheritdoc />
    public async Task AddProductsToMixMatchGroupAsync(int groupId, IEnumerable<int> productIds, CancellationToken cancellationToken = default)
    {
        foreach (var productId in productIds)
        {
            await _context.MixMatchGroupProducts.AddAsync(new MixMatchGroupProduct
            {
                MixMatchGroupId = groupId,
                ProductId = productId
            }, cancellationToken).ConfigureAwait(false);
        }
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task AddCategoriesToMixMatchGroupAsync(int groupId, IEnumerable<int> categoryIds, CancellationToken cancellationToken = default)
    {
        foreach (var categoryId in categoryIds)
        {
            await _context.MixMatchGroupCategories.AddAsync(new MixMatchGroupCategory
            {
                MixMatchGroupId = groupId,
                CategoryId = categoryId
            }, cancellationToken).ConfigureAwait(false);
        }
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Quantity Break Pricing

    /// <inheritdoc />
    public async Task CreateQuantityBreakTiersAsync(int promotionId, IEnumerable<QuantityBreakTier> tiers, CancellationToken cancellationToken = default)
    {
        foreach (var tier in tiers)
        {
            tier.PromotionId = promotionId;
            await _context.QuantityBreakTiers.AddAsync(tier, cancellationToken).ConfigureAwait(false);
        }
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Created quantity break tiers for promotion {PromotionId}", promotionId);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<QuantityBreakTier>> GetQuantityBreakTiersAsync(int promotionId, CancellationToken cancellationToken = default)
    {
        return await _context.QuantityBreakTiers
            .Where(t => t.PromotionId == promotionId && t.IsActive)
            .OrderBy(t => t.MinQuantity)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<QuantityBreakTier?> GetApplicableTierAsync(int productId, int quantity, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var productPromotions = await _context.PromotionProducts
            .Where(pp => pp.ProductId == productId && pp.IsActive && pp.Promotion.IsCurrentlyActive)
            .Select(pp => pp.PromotionId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return await _context.QuantityBreakTiers
            .Where(t => productPromotions.Contains(t.PromotionId)
                && t.IsActive
                && t.MinQuantity <= quantity
                && (t.MaxQuantity == null || t.MaxQuantity >= quantity))
            .OrderByDescending(t => t.MinQuantity)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<QuantityBreakCalculationResult> CalculateQuantityBreakDiscountAsync(
        IEnumerable<OrderItemInfo> items,
        CancellationToken cancellationToken = default)
    {
        var result = new QuantityBreakCalculationResult();

        foreach (var item in items)
        {
            var tier = await GetApplicableTierAsync(item.ProductId, item.Quantity, cancellationToken).ConfigureAwait(false);
            if (tier != null)
            {
                decimal discountedPrice;
                if (tier.UnitPrice.HasValue)
                {
                    discountedPrice = tier.UnitPrice.Value;
                }
                else if (tier.DiscountPercent.HasValue)
                {
                    discountedPrice = item.UnitPrice * (1 - tier.DiscountPercent.Value / 100);
                }
                else
                {
                    continue;
                }

                var discountAmount = (item.UnitPrice - discountedPrice) * item.Quantity;
                if (discountAmount > 0)
                {
                    result.Applications.Add(new QuantityBreakApplicationDetail
                    {
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        TierLabel = tier.DisplayLabel ?? $"Buy {tier.MinQuantity}+",
                        OriginalUnitPrice = item.UnitPrice,
                        DiscountedUnitPrice = discountedPrice,
                        DiscountAmount = discountAmount
                    });
                    result.TotalDiscount += discountAmount;
                }
            }
        }

        result.HasDiscount = result.TotalDiscount > 0;
        return result;
    }

    #endregion

    #region Combo/Bundle Deals

    /// <inheritdoc />
    public async Task<CentralPromotion> CreateComboPromotionAsync(
        CentralPromotion promotion,
        ComboPromotion comboConfig,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(promotion);
        ArgumentNullException.ThrowIfNull(comboConfig);

        promotion.Type = PromotionType.Bundle;
        await _context.CentralPromotions.AddAsync(promotion, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        comboConfig.PromotionId = promotion.Id;
        await _context.ComboPromotions.AddAsync(comboConfig, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Created combo promotion {PromotionId}: {Name}", promotion.Id, promotion.Name);
        return promotion;
    }

    /// <inheritdoc />
    public async Task<ComboPromotion?> GetComboConfigurationAsync(int promotionId, CancellationToken cancellationToken = default)
    {
        return await _context.ComboPromotions
            .Include(c => c.Promotion)
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.PromotionId == promotionId && c.IsActive, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ComboPromotion>> GetActiveCombosAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ComboPromotions
            .Include(c => c.Promotion)
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .Where(c => c.IsActive && c.Promotion.IsCurrentlyActive)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ComboMatchResult>> FindApplicableCombosAsync(
        IEnumerable<OrderItemInfo> items,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ComboMatchResult>();
        var itemsList = items.ToList();
        var activeCombos = await GetActiveCombosAsync(cancellationToken).ConfigureAwait(false);

        foreach (var combo in activeCombos)
        {
            var matchResult = new ComboMatchResult
            {
                ComboPromotionId = combo.Id,
                ComboName = combo.ComboName,
                ComboPrice = combo.ComboPrice,
                OriginalPrice = combo.OriginalTotalPrice,
                Savings = combo.OriginalTotalPrice - combo.ComboPrice
            };

            foreach (var comboItem in combo.Items.Where(i => i.IsRequired))
            {
                var matchingOrderItem = itemsList.FirstOrDefault(i => i.ProductId == comboItem.ProductId);
                var itemMatch = new ComboItemMatch
                {
                    ProductId = comboItem.ProductId,
                    ProductName = comboItem.Product.Name,
                    RequiredQuantity = comboItem.Quantity,
                    AvailableQuantity = matchingOrderItem?.Quantity ?? 0
                };

                if (itemMatch.AvailableQuantity >= itemMatch.RequiredQuantity)
                {
                    matchResult.MatchedItems.Add(itemMatch);
                }
                else
                {
                    matchResult.MissingItems.Add(itemMatch);
                }
            }

            results.Add(matchResult);
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<OrderItem>> AddComboToOrderAsync(
        int comboId,
        int orderId,
        IDictionary<int, int>? substitutions = null,
        CancellationToken cancellationToken = default)
    {
        var combo = await GetComboConfigurationAsync(comboId, cancellationToken).ConfigureAwait(false);
        if (combo == null)
            throw new InvalidOperationException($"Combo {comboId} not found");

        var orderItems = new List<OrderItem>();
        var pricePerItem = combo.ComboPrice / combo.Items.Count;

        foreach (var comboItem in combo.Items.Where(i => i.IsRequired))
        {
            var productId = comboItem.ProductId;
            if (substitutions != null && substitutions.TryGetValue(comboItem.ProductId, out var subProductId))
            {
                productId = subProductId;
            }

            var product = await _context.Products.FindAsync(new object[] { productId }, cancellationToken).ConfigureAwait(false);
            if (product == null) continue;

            var orderItem = new OrderItem
            {
                OrderId = orderId,
                ProductId = productId,
                Quantity = comboItem.Quantity,
                UnitPrice = pricePerItem / comboItem.Quantity,
                Notes = $"Part of {combo.ComboName} combo"
            };

            await _context.OrderItems.AddAsync(orderItem, cancellationToken).ConfigureAwait(false);
            orderItems.Add(orderItem);
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return orderItems;
    }

    #endregion

    #region Coupon Management

    /// <inheritdoc />
    public async Task<Coupon> CreateCouponAsync(Coupon coupon, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(coupon);

        if (string.IsNullOrEmpty(coupon.CouponCode))
        {
            coupon.CouponCode = GenerateCouponCode();
        }

        await _context.Coupons.AddAsync(coupon, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Created coupon {CouponCode}", coupon.CouponCode);
        return coupon;
    }

    /// <inheritdoc />
    public async Task<CouponBatch> GenerateCouponBatchAsync(
        CouponBatch batch,
        int count,
        Coupon template,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(batch);
        ArgumentNullException.ThrowIfNull(template);

        batch.TotalCoupons = count;
        await _context.CouponBatches.AddAsync(batch, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        for (int i = 0; i < count; i++)
        {
            var coupon = new Coupon
            {
                PromotionId = template.PromotionId,
                CouponCode = $"{batch.CodePrefix}{GenerateCouponCode(8)}",
                CouponType = template.CouponType,
                DiscountAmount = template.DiscountAmount,
                DiscountPercent = template.DiscountPercent,
                MinimumPurchase = template.MinimumPurchase,
                MaxDiscountAmount = template.MaxDiscountAmount,
                ValidFrom = template.ValidFrom,
                ValidTo = template.ValidTo,
                MaxUses = template.MaxUses,
                BatchId = batch.Id,
                Description = template.Description
            };

            await _context.Coupons.AddAsync(coupon, cancellationToken).ConfigureAwait(false);
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Generated batch {BatchId} with {Count} coupons", batch.Id, count);

        return batch;
    }

    private static string GenerateCouponCode(int length = 10)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var code = new StringBuilder(length);

        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[length];
        rng.GetBytes(bytes);

        foreach (var b in bytes)
        {
            code.Append(chars[b % chars.Length]);
        }

        return code.ToString();
    }

    /// <inheritdoc />
    public async Task<CouponValidationResult> ValidateCouponAsync(
        string couponCode,
        decimal orderTotal,
        int? customerId = null,
        CancellationToken cancellationToken = default)
    {
        var coupon = await GetCouponByCodeAsync(couponCode, cancellationToken).ConfigureAwait(false);

        if (coupon == null)
        {
            return new CouponValidationResult { IsValid = false, ErrorMessage = "Invalid coupon code" };
        }

        if (!coupon.IsCurrentlyValid)
        {
            if (coupon.IsVoided)
                return new CouponValidationResult { IsValid = false, ErrorMessage = "This coupon has been voided" };
            if (DateTime.UtcNow < coupon.ValidFrom)
                return new CouponValidationResult { IsValid = false, ErrorMessage = "This coupon is not yet valid" };
            if (DateTime.UtcNow > coupon.ValidTo)
                return new CouponValidationResult { IsValid = false, ErrorMessage = "This coupon has expired" };
            if (coupon.MaxUses.HasValue && coupon.UseCount >= coupon.MaxUses)
                return new CouponValidationResult { IsValid = false, ErrorMessage = "This coupon has reached its maximum uses" };
        }

        if (coupon.CustomerId.HasValue && coupon.CustomerId != customerId)
        {
            return new CouponValidationResult { IsValid = false, ErrorMessage = "This coupon is not valid for this customer" };
        }

        if (coupon.MinimumPurchase.HasValue && orderTotal < coupon.MinimumPurchase)
        {
            return new CouponValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Minimum purchase of {coupon.MinimumPurchase:C} required"
            };
        }

        decimal discount;
        if (coupon.DiscountPercent.HasValue)
        {
            discount = orderTotal * (coupon.DiscountPercent.Value / 100);
            if (coupon.MaxDiscountAmount.HasValue)
            {
                discount = Math.Min(discount, coupon.MaxDiscountAmount.Value);
            }
        }
        else if (coupon.DiscountAmount.HasValue)
        {
            discount = Math.Min(coupon.DiscountAmount.Value, orderTotal);
        }
        else
        {
            discount = 0;
        }

        return new CouponValidationResult
        {
            IsValid = true,
            Coupon = coupon,
            CalculatedDiscount = discount
        };
    }

    /// <inheritdoc />
    public async Task<CouponRedemption> ApplyCouponAsync(
        string couponCode,
        int receiptId,
        decimal discountAmount,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var coupon = await GetCouponByCodeAsync(couponCode, cancellationToken).ConfigureAwait(false);
        if (coupon == null)
            throw new InvalidOperationException("Coupon not found");

        var redemption = new CouponRedemption
        {
            CouponId = coupon.Id,
            ReceiptId = receiptId,
            DiscountAmount = discountAmount,
            RedeemedAt = DateTime.UtcNow,
            RedeemedByUserId = userId
        };

        coupon.UseCount++;

        await _context.CouponRedemptions.AddAsync(redemption, cancellationToken).ConfigureAwait(false);
        _context.Coupons.Update(coupon);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Applied coupon {CouponCode} to receipt {ReceiptId}, discount {Discount}",
            couponCode, receiptId, discountAmount);

        return redemption;
    }

    /// <inheritdoc />
    public async Task VoidCouponAsync(int couponId, string reason, CancellationToken cancellationToken = default)
    {
        var coupon = await _context.Coupons.FindAsync(new object[] { couponId }, cancellationToken).ConfigureAwait(false);
        if (coupon == null)
            throw new InvalidOperationException($"Coupon {couponId} not found");

        coupon.IsVoided = true;
        _context.Coupons.Update(coupon);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Voided coupon {CouponId}: {Reason}", couponId, reason);
    }

    /// <inheritdoc />
    public async Task<Coupon?> GetCouponByCodeAsync(string couponCode, CancellationToken cancellationToken = default)
    {
        return await _context.Coupons
            .Include(c => c.Promotion)
            .FirstOrDefaultAsync(c => c.CouponCode == couponCode && c.IsActive, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CouponRedemption>> GetCouponRedemptionsAsync(int couponId, CancellationToken cancellationToken = default)
    {
        return await _context.CouponRedemptions
            .Include(r => r.Receipt)
            .Include(r => r.RedeemedByUser)
            .Where(r => r.CouponId == couponId)
            .OrderByDescending(r => r.RedeemedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    #endregion

    #region Automatic Markdown

    /// <inheritdoc />
    public async Task<AutomaticMarkdown> CreateMarkdownRuleAsync(AutomaticMarkdown markdown, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        await _context.AutomaticMarkdowns.AddAsync(markdown, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Created markdown rule {RuleId}: {Name}", markdown.Id, markdown.RuleName);
        return markdown;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AutomaticMarkdown>> GetMarkdownRulesForProductAsync(int productId, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products.FindAsync(new object[] { productId }, cancellationToken).ConfigureAwait(false);
        if (product == null) return Enumerable.Empty<AutomaticMarkdown>();

        return await _context.AutomaticMarkdowns
            .Where(m => m.IsActive && (m.ProductId == productId || m.CategoryId == product.CategoryId))
            .OrderByDescending(m => m.Priority)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<AutomaticMarkdown?> GetActiveMarkdownAsync(
        int productId,
        DateTime currentTime,
        TimeSpan? closingTime = null,
        CancellationToken cancellationToken = default)
    {
        var rules = await GetMarkdownRulesForProductAsync(productId, cancellationToken).ConfigureAwait(false);
        var currentTimeOfDay = currentTime.TimeOfDay;
        var currentDayOfWeek = (int)currentTime.DayOfWeek;

        foreach (var rule in rules)
        {
            // Check day of week restriction
            if (!string.IsNullOrEmpty(rule.ValidDaysOfWeek))
            {
                var validDays = JsonSerializer.Deserialize<int[]>(rule.ValidDaysOfWeek);
                if (validDays != null && !validDays.Contains(currentDayOfWeek))
                    continue;
            }

            var isActive = rule.TriggerType switch
            {
                MarkdownTriggerType.TimeOfDay =>
                    rule.TriggerTime.HasValue && currentTimeOfDay >= rule.TriggerTime.Value,

                MarkdownTriggerType.BeforeClosing =>
                    closingTime.HasValue && rule.HoursBeforeClosing.HasValue &&
                    currentTimeOfDay >= closingTime.Value.Subtract(TimeSpan.FromHours(rule.HoursBeforeClosing.Value)),

                MarkdownTriggerType.Manual => false, // Manual requires explicit activation

                _ => false
            };

            if (isActive)
                return rule;
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ProductMarkdownInfo>> GetAllActiveMarkdownsAsync(
        DateTime currentTime,
        TimeSpan? closingTime = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ProductMarkdownInfo>();
        var products = await _context.Products
            .Where(p => p.IsActive)
            .Select(p => new { p.Id, p.Name, p.SellingPrice })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var product in products)
        {
            var markdown = await GetActiveMarkdownAsync(product.Id, currentTime, closingTime, cancellationToken)
                .ConfigureAwait(false);

            if (markdown != null)
            {
                var markdownPrice = markdown.FinalPrice ?? product.SellingPrice * (1 - markdown.DiscountPercent / 100);
                results.Add(new ProductMarkdownInfo
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    OriginalPrice = product.SellingPrice,
                    MarkdownPrice = markdownPrice,
                    DiscountPercent = markdown.DiscountPercent,
                    RuleName = markdown.RuleName,
                    TriggerType = markdown.TriggerType
                });
            }
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<MarkdownCalculationResult> CalculateMarkdownPriceAsync(
        int productId,
        decimal originalPrice,
        DateTime currentTime,
        TimeSpan? closingTime = null,
        CancellationToken cancellationToken = default)
    {
        var markdown = await GetActiveMarkdownAsync(productId, currentTime, closingTime, cancellationToken)
            .ConfigureAwait(false);

        if (markdown == null)
        {
            return new MarkdownCalculationResult
            {
                HasMarkdown = false,
                OriginalPrice = originalPrice,
                MarkdownPrice = originalPrice
            };
        }

        var markdownPrice = markdown.FinalPrice ?? originalPrice * (1 - markdown.DiscountPercent / 100);

        return new MarkdownCalculationResult
        {
            HasMarkdown = true,
            OriginalPrice = originalPrice,
            MarkdownPrice = markdownPrice,
            DiscountAmount = originalPrice - markdownPrice,
            DiscountPercent = markdown.DiscountPercent,
            RuleName = markdown.RuleName,
            MarkdownRuleId = markdown.Id
        };
    }

    #endregion

    #region Promotion Application

    /// <inheritdoc />
    public async Task<PromotionCalculationResult> CalculateAllPromotionsAsync(
        IEnumerable<OrderItemInfo> items,
        decimal orderTotal,
        int? customerId = null,
        string? couponCode = null,
        CancellationToken cancellationToken = default)
    {
        var itemsList = items.ToList();
        var result = new PromotionCalculationResult
        {
            OriginalTotal = orderTotal
        };

        // Calculate BOGO discounts
        result.BogoResult = await CalculateBogoDiscountAsync(itemsList, cancellationToken).ConfigureAwait(false);
        if (result.BogoResult.HasDiscount)
        {
            result.TotalDiscount += result.BogoResult.TotalDiscount;
            foreach (var app in result.BogoResult.Applications)
            {
                result.AppliedPromotions.Add(new AppliedPromotionSummary
                {
                    PromotionId = app.PromotionId,
                    PromotionName = app.PromotionName,
                    PromotionType = "BOGO",
                    DiscountAmount = app.DiscountAmount,
                    Description = $"Buy {app.BuyProductId}, Get {app.FreeQuantity} free"
                });
            }
        }

        // Calculate Mix & Match discounts
        result.MixMatchResult = await CalculateMixMatchDiscountAsync(itemsList, cancellationToken).ConfigureAwait(false);
        if (result.MixMatchResult.HasDiscount)
        {
            result.TotalDiscount += result.MixMatchResult.TotalDiscount;
            foreach (var app in result.MixMatchResult.Applications)
            {
                result.AppliedPromotions.Add(new AppliedPromotionSummary
                {
                    PromotionId = app.PromotionId,
                    PromotionName = app.PromotionName,
                    PromotionType = "Mix & Match",
                    DiscountAmount = app.DiscountAmount,
                    Description = $"Save {app.DiscountAmount:C} on qualifying items"
                });
            }
        }

        // Calculate quantity break discounts
        result.QuantityBreakResult = await CalculateQuantityBreakDiscountAsync(itemsList, cancellationToken).ConfigureAwait(false);
        if (result.QuantityBreakResult.HasDiscount)
        {
            result.TotalDiscount += result.QuantityBreakResult.TotalDiscount;
            foreach (var app in result.QuantityBreakResult.Applications)
            {
                result.AppliedPromotions.Add(new AppliedPromotionSummary
                {
                    PromotionName = app.TierLabel,
                    PromotionType = "Quantity Break",
                    DiscountAmount = app.DiscountAmount,
                    Description = $"{app.ProductName}: {app.TierLabel}"
                });
            }
        }

        // Calculate markdowns
        var currentTime = DateTime.Now;
        foreach (var item in itemsList)
        {
            var markdownResult = await CalculateMarkdownPriceAsync(
                item.ProductId,
                item.UnitPrice,
                currentTime,
                null,
                cancellationToken).ConfigureAwait(false);

            if (markdownResult.HasMarkdown)
            {
                result.MarkdownResults.Add(markdownResult);
                var lineDiscount = markdownResult.DiscountAmount * item.Quantity;
                result.TotalDiscount += lineDiscount;
                result.AppliedPromotions.Add(new AppliedPromotionSummary
                {
                    PromotionName = markdownResult.RuleName ?? "Markdown",
                    PromotionType = "Markdown",
                    DiscountAmount = lineDiscount,
                    Description = $"{item.ProductName}: {markdownResult.DiscountPercent}% off"
                });
            }
        }

        // Validate and calculate coupon discount
        if (!string.IsNullOrEmpty(couponCode))
        {
            var adjustedTotal = orderTotal - result.TotalDiscount;
            result.CouponResult = await ValidateCouponAsync(couponCode, adjustedTotal, customerId, cancellationToken)
                .ConfigureAwait(false);

            if (result.CouponResult.IsValid && result.CouponResult.Coupon != null)
            {
                result.TotalDiscount += result.CouponResult.CalculatedDiscount;
                result.AppliedPromotions.Add(new AppliedPromotionSummary
                {
                    PromotionName = result.CouponResult.Coupon.Description ?? "Coupon",
                    PromotionType = "Coupon",
                    DiscountAmount = result.CouponResult.CalculatedDiscount,
                    Description = $"Coupon: {couponCode}"
                });
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<PromotionApplication> RecordPromotionApplicationAsync(
        PromotionApplication application,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(application);

        await _context.PromotionApplications.AddAsync(application, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return application;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PromotionApplication>> GetPromotionApplicationsAsync(int receiptId, CancellationToken cancellationToken = default)
    {
        return await _context.PromotionApplications
            .Include(pa => pa.Promotion)
            .Where(pa => pa.ReceiptId == receiptId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    #endregion
}
