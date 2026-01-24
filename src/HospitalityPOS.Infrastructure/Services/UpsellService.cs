using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service implementation for AI-powered upsell and cross-sell recommendations.
/// </summary>
public class UpsellService : IUpsellService
{
    private readonly POSDbContext _context;
    private readonly ILogger<UpsellService> _logger;

    public UpsellService(
        POSDbContext context,
        ILogger<UpsellService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Real-time Suggestions

    public async Task<List<UpsellSuggestion>> GetSuggestionsAsync(UpsellContext context)
    {
        var config = await GetConfigurationEntityAsync(context.StoreId);
        if (!config.IsEnabled)
            return new List<UpsellSuggestion>();

        var suggestions = new List<UpsellSuggestion>();

        // 1. Rule-based suggestions (highest priority)
        var ruleSuggestions = await GetRuleBasedSuggestionsAsync(context, config);
        suggestions.AddRange(ruleSuggestions);

        // 2. Association-based suggestions
        var associationSuggestions = await GetAssociationBasedSuggestionsAsync(context, config);
        suggestions.AddRange(associationSuggestions);

        // 3. Personalized suggestions
        if (context.IncludePersonalized && context.CustomerId.HasValue && config.IncludePersonalized)
        {
            var personalizedSuggestions = await GetPersonalizedSuggestionsAsync(context, config);
            suggestions.AddRange(personalizedSuggestions);
        }

        // 4. Trending suggestions
        if (context.IncludeTrending && config.IncludeTrending)
        {
            var trendingSuggestions = await GetTrendingSuggestionsAsync(context, config);
            suggestions.AddRange(trendingSuggestions);
        }

        // Merge, dedupe, and rank
        var rankedSuggestions = RankAndFilterSuggestions(suggestions, context, config);

        return rankedSuggestions.Take(context.MaxSuggestions).ToList();
    }

    public async Task<List<UpsellSuggestion>> GetSuggestionsForProductAsync(int productId, int? customerId = null, int? storeId = null)
    {
        var context = new UpsellContext
        {
            CartProductIds = new List<int> { productId },
            CustomerId = customerId,
            StoreId = storeId,
            MaxSuggestions = 3
        };

        return await GetSuggestionsAsync(context);
    }

    public async Task<List<UpsellSuggestion>> GetSuggestionsForCartAsync(List<int> productIds, int? customerId = null, int? storeId = null)
    {
        var context = new UpsellContext
        {
            CartProductIds = productIds,
            CustomerId = customerId,
            StoreId = storeId,
            MaxSuggestions = 3
        };

        return await GetSuggestionsAsync(context);
    }

    private async Task<List<UpsellSuggestion>> GetRuleBasedSuggestionsAsync(UpsellContext context, UpsellConfiguration config)
    {
        var suggestions = new List<UpsellSuggestion>();
        var today = DateTime.UtcNow.Date;
        var now = DateTime.UtcNow;

        // Get applicable rules
        var rules = await _context.UpsellRules
            .Include(r => r.TargetProduct)
            .Where(r => r.IsActive && r.IsEnabled)
            .Where(r => !r.StoreId.HasValue || r.StoreId == context.StoreId)
            .Where(r => !r.StartDate.HasValue || r.StartDate <= now)
            .Where(r => !r.EndDate.HasValue || r.EndDate >= now)
            .Where(r => !r.TimeOfDayFilter.HasValue || r.TimeOfDayFilter == context.TimeOfDay)
            .OrderByDescending(r => r.Priority)
            .ToListAsync();

        foreach (var rule in rules)
        {
            // Check daily limit
            if (rule.MaxSuggestionsPerDay.HasValue)
            {
                if (rule.LastCountResetDate != today)
                {
                    rule.TodaySuggestionCount = 0;
                    rule.LastCountResetDate = today;
                }
                if (rule.TodaySuggestionCount >= rule.MaxSuggestionsPerDay.Value)
                    continue;
            }

            // Check if rule matches cart
            var matches = false;
            if (rule.SourceProductId.HasValue)
            {
                matches = context.CartProductIds.Contains(rule.SourceProductId.Value);
            }
            else if (rule.SourceCategoryId.HasValue)
            {
                var cartProducts = await _context.Products
                    .Where(p => context.CartProductIds.Contains(p.Id))
                    .Select(p => p.CategoryId)
                    .ToListAsync();
                matches = cartProducts.Contains(rule.SourceCategoryId.Value);
            }

            if (!matches)
                continue;

            // Don't suggest items already in cart
            if (context.CartProductIds.Contains(rule.TargetProductId))
                continue;

            var product = rule.TargetProduct;
            if (product == null || !product.IsActive)
                continue;

            suggestions.Add(new UpsellSuggestion
            {
                ProductId = rule.TargetProductId,
                ProductName = product.Name,
                Price = product.Price,
                ImageUrl = product.ImageUrl,
                SuggestionText = rule.SuggestionText ?? $"Add {product.Name}?",
                Reason = $"Rule: {rule.Name}",
                Type = rule.Type,
                ConfidenceScore = 1.0m * config.RuleWeight, // Rules have full confidence
                SavingsAmount = rule.SavingsAmount,
                SourceProductId = rule.SourceProductId,
                RuleId = rule.Id,
                Priority = rule.Priority * 100, // Boost rule priority
                CategoryId = product.CategoryId
            });
        }

        return suggestions;
    }

    private async Task<List<UpsellSuggestion>> GetAssociationBasedSuggestionsAsync(UpsellContext context, UpsellConfiguration config)
    {
        var suggestions = new List<UpsellSuggestion>();

        // Get associations for products in cart
        var associations = await _context.ProductAssociations
            .Include(a => a.AssociatedProduct)
            .Where(a => a.IsActive && context.CartProductIds.Contains(a.ProductId))
            .Where(a => !a.StoreId.HasValue || a.StoreId == context.StoreId)
            .Where(a => a.Lift >= config.MinLift)
            .Where(a => a.Confidence >= config.MinAssociationConfidence)
            .Where(a => !context.CartProductIds.Contains(a.AssociatedProductId))
            .OrderByDescending(a => a.Lift * a.Confidence)
            .Take(10)
            .ToListAsync();

        foreach (var association in associations)
        {
            var product = association.AssociatedProduct;
            if (product == null || !product.IsActive)
                continue;

            suggestions.Add(new UpsellSuggestion
            {
                ProductId = association.AssociatedProductId,
                ProductName = product.Name,
                Price = product.Price,
                ImageUrl = product.ImageUrl,
                SuggestionText = $"Customers also bought {product.Name}",
                Reason = $"Association: Lift={association.Lift:F2}",
                Type = UpsellRuleType.Complementary,
                ConfidenceScore = association.Confidence * config.AssociationWeight,
                SourceProductId = association.ProductId,
                AssociationId = association.Id,
                Priority = (int)(association.Lift * 10),
                CategoryId = product.CategoryId
            });
        }

        return suggestions;
    }

    private async Task<List<UpsellSuggestion>> GetPersonalizedSuggestionsAsync(UpsellContext context, UpsellConfiguration config)
    {
        var suggestions = new List<UpsellSuggestion>();

        if (!context.CustomerId.HasValue)
            return suggestions;

        // Get customer's frequently purchased items not in cart
        var recentCutoff = DateTime.UtcNow.AddDays(-config.ExcludeRecentPurchaseDays);

        var preferences = await _context.CustomerPreferences
            .Include(cp => cp.Product)
            .Where(cp => cp.CustomerId == context.CustomerId.Value)
            .Where(cp => cp.Product!.IsActive)
            .Where(cp => !context.CartProductIds.Contains(cp.ProductId))
            .Where(cp => cp.LastPurchased < recentCutoff) // Exclude recent purchases
            .OrderByDescending(cp => cp.PreferenceScore)
            .Take(5)
            .ToListAsync();

        foreach (var pref in preferences)
        {
            var product = pref.Product!;

            suggestions.Add(new UpsellSuggestion
            {
                ProductId = pref.ProductId,
                ProductName = product.Name,
                Price = product.Price,
                ImageUrl = product.ImageUrl,
                SuggestionText = $"Your favorite: {product.Name}",
                Reason = $"Purchased {pref.PurchaseCount} times",
                Type = UpsellRuleType.Personalized,
                ConfidenceScore = pref.PreferenceScore * config.PersonalizedWeight,
                Priority = (int)(pref.PreferenceScore * 50),
                CategoryId = product.CategoryId
            });
        }

        return suggestions;
    }

    private async Task<List<UpsellSuggestion>> GetTrendingSuggestionsAsync(UpsellContext context, UpsellConfiguration config)
    {
        var suggestions = new List<UpsellSuggestion>();
        var trendingCutoff = DateTime.UtcNow.AddDays(-config.TrendingDays);

        // Get trending products (most sold in recent period)
        var trendingProducts = await _context.ReceiptItems
            .Where(ri => ri.Receipt!.CreatedAt >= trendingCutoff)
            .Where(ri => ri.Product!.IsActive)
            .Where(ri => !context.CartProductIds.Contains(ri.ProductId))
            .GroupBy(ri => ri.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                Count = g.Sum(ri => ri.Quantity),
                Product = g.First().Product
            })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync();

        foreach (var trending in trendingProducts)
        {
            var product = trending.Product;
            if (product == null)
                continue;

            suggestions.Add(new UpsellSuggestion
            {
                ProductId = trending.ProductId,
                ProductName = product.Name,
                Price = product.Price,
                ImageUrl = product.ImageUrl,
                SuggestionText = $"Trending: {product.Name}",
                Reason = $"Popular item ({trending.Count} sold recently)",
                Type = UpsellRuleType.Trending,
                ConfidenceScore = 0.5m * config.TrendingWeight,
                Priority = 10,
                CategoryId = product.CategoryId
            });
        }

        return suggestions;
    }

    private List<UpsellSuggestion> RankAndFilterSuggestions(
        List<UpsellSuggestion> suggestions,
        UpsellContext context,
        UpsellConfiguration config)
    {
        // Remove duplicates (keep highest priority)
        var deduped = suggestions
            .GroupBy(s => s.ProductId)
            .Select(g => g.OrderByDescending(s => s.Priority).ThenByDescending(s => s.ConfidenceScore).First())
            .ToList();

        // Apply category diversity filter
        if (config.EnforceCategoryDiversity)
        {
            var diverse = new List<UpsellSuggestion>();
            var usedCategories = new HashSet<int>();

            foreach (var suggestion in deduped.OrderByDescending(s => s.Priority).ThenByDescending(s => s.ConfidenceScore))
            {
                if (suggestion.CategoryId.HasValue && usedCategories.Contains(suggestion.CategoryId.Value))
                    continue;

                diverse.Add(suggestion);
                if (suggestion.CategoryId.HasValue)
                    usedCategories.Add(suggestion.CategoryId.Value);
            }

            deduped = diverse;
        }

        // Filter by minimum confidence
        deduped = deduped.Where(s => s.ConfidenceScore >= config.MinConfidenceScore).ToList();

        // Final ranking by priority then confidence
        return deduped
            .OrderByDescending(s => s.Priority)
            .ThenByDescending(s => s.ConfidenceScore)
            .ToList();
    }

    #endregion

    #region Personalized Recommendations

    public async Task<List<ProductRecommendation>> GetPersonalizedRecommendationsAsync(int customerId, int maxResults = 5)
    {
        var preferences = await _context.CustomerPreferences
            .Include(cp => cp.Product)
            .Where(cp => cp.CustomerId == customerId && cp.Product!.IsActive)
            .OrderByDescending(cp => cp.PreferenceScore)
            .Take(maxResults)
            .ToListAsync();

        return preferences.Select(p => new ProductRecommendation
        {
            ProductId = p.ProductId,
            ProductName = p.Product!.Name,
            Price = p.Product.Price,
            ImageUrl = p.Product.ImageUrl,
            Type = RecommendationType.PersonalFavorite,
            Reason = $"Purchased {p.PurchaseCount} times",
            Score = p.PreferenceScore,
            CategoryId = p.Product.CategoryId
        }).ToList();
    }

    public async Task<List<ProductRecommendation>> GetTrendingProductsAsync(int? storeId = null, int maxResults = 5)
    {
        var config = await GetConfigurationEntityAsync(storeId);
        var cutoff = DateTime.UtcNow.AddDays(-config.TrendingDays);

        var query = _context.ReceiptItems
            .Where(ri => ri.Receipt!.CreatedAt >= cutoff)
            .Where(ri => ri.Product!.IsActive);

        if (storeId.HasValue)
            query = query.Where(ri => ri.Receipt!.StoreId == storeId);

        var trending = await query
            .GroupBy(ri => ri.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                Count = g.Sum(ri => ri.Quantity),
                Revenue = g.Sum(ri => ri.LineTotal),
                Product = g.First().Product
            })
            .OrderByDescending(x => x.Count)
            .Take(maxResults)
            .ToListAsync();

        var maxCount = trending.Max(t => t.Count);
        return trending.Select(t => new ProductRecommendation
        {
            ProductId = t.ProductId,
            ProductName = t.Product!.Name,
            Price = t.Product.Price,
            ImageUrl = t.Product.ImageUrl,
            Type = RecommendationType.Trending,
            Reason = $"{t.Count} sold this week",
            Score = (decimal)t.Count / maxCount,
            CategoryId = t.Product.CategoryId
        }).ToList();
    }

    public async Task<List<ProductRecommendation>> GetNewArrivalsAsync(int? storeId = null, int maxResults = 5)
    {
        var cutoff = DateTime.UtcNow.AddDays(-30);

        var newProducts = await _context.Products
            .Where(p => p.IsActive && p.CreatedAt >= cutoff)
            .OrderByDescending(p => p.CreatedAt)
            .Take(maxResults)
            .ToListAsync();

        return newProducts.Select(p => new ProductRecommendation
        {
            ProductId = p.Id,
            ProductName = p.Name,
            Price = p.Price,
            ImageUrl = p.ImageUrl,
            Type = RecommendationType.NewArrival,
            Reason = "New on the menu",
            Score = 0.8m,
            CategoryId = p.CategoryId
        }).ToList();
    }

    #endregion

    #region Suggestion Tracking

    public async Task<int> RecordSuggestionAsync(RecordSuggestionRequest request)
    {
        var log = new UpsellSuggestionLog
        {
            ReceiptId = request.ReceiptId,
            SuggestedProductId = request.ProductId,
            SuggestionType = request.SuggestionType,
            ConfidenceScore = request.ConfidenceScore,
            AssociationId = request.AssociationId,
            RuleId = request.RuleId,
            TriggerProductIds = request.SourceProductId?.ToString(),
            UserId = request.UserId,
            CustomerId = request.CustomerId,
            StoreId = request.StoreId,
            SuggestedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.UpsellSuggestionLogs.Add(log);
        await _context.SaveChangesAsync();

        // Update rule suggestion count if applicable
        if (request.RuleId.HasValue)
        {
            var rule = await _context.UpsellRules.FindAsync(request.RuleId.Value);
            if (rule != null)
            {
                var today = DateTime.UtcNow.Date;
                if (rule.LastCountResetDate != today)
                {
                    rule.TodaySuggestionCount = 0;
                    rule.LastCountResetDate = today;
                }
                rule.TodaySuggestionCount++;
                await _context.SaveChangesAsync();
            }
        }

        return log.Id;
    }

    public async Task RecordAcceptanceAsync(int suggestionLogId, int quantity, decimal value)
    {
        var log = await _context.UpsellSuggestionLogs.FindAsync(suggestionLogId);
        if (log == null)
            return;

        log.WasAccepted = true;
        log.AcceptedQuantity = quantity;
        log.AcceptedValue = value;
        log.OutcomeRecordedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task RecordRejectionAsync(int suggestionLogId)
    {
        var log = await _context.UpsellSuggestionLogs.FindAsync(suggestionLogId);
        if (log == null)
            return;

        log.WasAccepted = false;
        log.OutcomeRecordedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task<List<int>> RecordSuggestionsAsync(int receiptId, List<UpsellSuggestion> suggestions, int? userId = null, int? customerId = null, int? storeId = null)
    {
        var ids = new List<int>();

        foreach (var suggestion in suggestions)
        {
            var id = await RecordSuggestionAsync(new RecordSuggestionRequest
            {
                ReceiptId = receiptId,
                ProductId = suggestion.ProductId,
                SuggestionType = suggestion.Type,
                ConfidenceScore = suggestion.ConfidenceScore,
                AssociationId = suggestion.AssociationId,
                RuleId = suggestion.RuleId,
                SourceProductId = suggestion.SourceProductId,
                UserId = userId,
                CustomerId = customerId,
                StoreId = storeId
            });
            ids.Add(id);
        }

        return ids;
    }

    #endregion

    #region Association Mining

    public async Task<AssociationRebuildResult> RebuildAssociationsAsync()
    {
        var config = await GetConfigurationEntityAsync(null);
        var from = DateTime.UtcNow.AddDays(-config.AnalysisDays);
        var to = DateTime.UtcNow;

        return await RebuildAssociationsAsync(from, to, null);
    }

    public async Task<AssociationRebuildResult> RebuildAssociationsAsync(DateTime from, DateTime to, int? storeId = null)
    {
        var result = new AssociationRebuildResult { StartTime = DateTime.UtcNow };

        try
        {
            _logger.LogInformation("Starting association mining from {From} to {To}", from, to);

            var config = await GetConfigurationEntityAsync(storeId);

            // Get all transactions with their items
            var query = _context.Receipts
                .Where(r => r.CreatedAt >= from && r.CreatedAt <= to && !r.IsVoided)
                .Include(r => r.Items)
                .ThenInclude(ri => ri.Product);

            if (storeId.HasValue)
                query = query.Where(r => r.StoreId == storeId);

            var receipts = await query.ToListAsync();
            result.TransactionsAnalyzed = receipts.Count;

            if (receipts.Count == 0)
            {
                result.EndTime = DateTime.UtcNow;
                return result;
            }

            // Build item sets from transactions
            var transactions = receipts
                .Where(r => r.Items.Count >= 2) // Need at least 2 items
                .Select(r => r.Items.Select(i => i.ProductId).Distinct().ToList())
                .ToList();

            // Calculate pairwise associations
            var associations = new Dictionary<(int, int), AssociationMetrics>();
            var productCounts = new Dictionary<int, int>();
            var totalTransactions = transactions.Count;

            // Count single item frequencies
            foreach (var transaction in transactions)
            {
                foreach (var productId in transaction)
                {
                    productCounts.TryAdd(productId, 0);
                    productCounts[productId]++;
                }
            }

            // Count pair frequencies
            var pairCounts = new Dictionary<(int, int), int>();
            foreach (var transaction in transactions)
            {
                var products = transaction.OrderBy(p => p).ToList();
                for (int i = 0; i < products.Count; i++)
                {
                    for (int j = i + 1; j < products.Count; j++)
                    {
                        var pair = (products[i], products[j]);
                        pairCounts.TryAdd(pair, 0);
                        pairCounts[pair]++;
                    }
                }
            }

            // Calculate metrics for each pair
            foreach (var kvp in pairCounts)
            {
                var (productA, productB) = kvp.Key;
                var coOccurrence = kvp.Value;
                var countA = productCounts[productA];
                var countB = productCounts[productB];

                var support = (decimal)coOccurrence / totalTransactions;
                var confidenceAB = (decimal)coOccurrence / countA;
                var confidenceBA = (decimal)coOccurrence / countB;
                var pB = (decimal)countB / totalTransactions;
                var pA = (decimal)countA / totalTransactions;
                var liftAB = confidenceAB / pB;
                var liftBA = confidenceBA / pA;

                // Store A -> B if meets thresholds
                if (support >= config.MinSupport && confidenceAB >= config.MinAssociationConfidence && liftAB >= config.MinLift)
                {
                    associations[(productA, productB)] = new AssociationMetrics
                    {
                        ProductAId = productA,
                        ProductBId = productB,
                        Support = support,
                        Confidence = confidenceAB,
                        Lift = liftAB,
                        CoOccurrenceCount = coOccurrence,
                        ProductACount = countA,
                        ProductBCount = countB,
                        TotalTransactions = totalTransactions
                    };
                }

                // Store B -> A if meets thresholds
                if (support >= config.MinSupport && confidenceBA >= config.MinAssociationConfidence && liftBA >= config.MinLift)
                {
                    associations[(productB, productA)] = new AssociationMetrics
                    {
                        ProductAId = productB,
                        ProductBId = productA,
                        Support = support,
                        Confidence = confidenceBA,
                        Lift = liftBA,
                        CoOccurrenceCount = coOccurrence,
                        ProductACount = countB,
                        ProductBCount = countA,
                        TotalTransactions = totalTransactions
                    };
                }
            }

            result.AssociationsFound = associations.Count;

            // Mark existing associations as inactive
            var existingAssociations = await _context.ProductAssociations
                .Where(a => !storeId.HasValue || a.StoreId == storeId)
                .ToListAsync();

            foreach (var existing in existingAssociations)
            {
                existing.IsActive = false;
            }
            result.AssociationsRemoved = existingAssociations.Count;

            // Add or update associations
            foreach (var kvp in associations)
            {
                var (productA, productB) = kvp.Key;
                var metrics = kvp.Value;

                var existing = existingAssociations.FirstOrDefault(a => a.ProductId == productA && a.AssociatedProductId == productB);
                if (existing != null)
                {
                    existing.Support = metrics.Support;
                    existing.Confidence = metrics.Confidence;
                    existing.Lift = metrics.Lift;
                    existing.TransactionCount = metrics.CoOccurrenceCount;
                    existing.CalculatedAt = DateTime.UtcNow;
                    existing.AnalysisStartDate = from;
                    existing.AnalysisEndDate = to;
                    existing.IsActive = true;
                }
                else
                {
                    _context.ProductAssociations.Add(new ProductAssociation
                    {
                        ProductId = productA,
                        AssociatedProductId = productB,
                        Type = AssociationType.FrequentlyBoughtTogether,
                        Support = metrics.Support,
                        Confidence = metrics.Confidence,
                        Lift = metrics.Lift,
                        TransactionCount = metrics.CoOccurrenceCount,
                        CalculatedAt = DateTime.UtcNow,
                        AnalysisStartDate = from,
                        AnalysisEndDate = to,
                        StoreId = storeId,
                        IsActive = true
                    });
                }
            }

            await _context.SaveChangesAsync();
            result.AssociationsStored = associations.Count;

            _logger.LogInformation("Association mining completed: {Found} associations found, {Stored} stored",
                result.AssociationsFound, result.AssociationsStored);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during association mining");
            result.Errors.Add(ex.Message);
        }

        result.EndTime = DateTime.UtcNow;
        return result;
    }

    public async Task UpdateAssociationsForProductAsync(int productId)
    {
        // For now, trigger full rebuild - could be optimized to update only this product
        await RebuildAssociationsAsync();
    }

    public async Task<List<ProductAssociationDto>> GetAssociationsAsync(int? storeId = null)
    {
        var associations = await _context.ProductAssociations
            .Include(a => a.Product)
            .Include(a => a.AssociatedProduct)
            .Where(a => a.IsActive)
            .Where(a => !storeId.HasValue || a.StoreId == null || a.StoreId == storeId)
            .OrderByDescending(a => a.Lift)
            .ToListAsync();

        return associations.Select(MapToAssociationDto).ToList();
    }

    public async Task<List<ProductAssociationDto>> GetAssociationsForProductAsync(int productId)
    {
        var associations = await _context.ProductAssociations
            .Include(a => a.Product)
            .Include(a => a.AssociatedProduct)
            .Where(a => a.IsActive && a.ProductId == productId)
            .OrderByDescending(a => a.Lift)
            .ToListAsync();

        return associations.Select(MapToAssociationDto).ToList();
    }

    public async Task<AssociationMetrics?> CalculateAssociationMetricsAsync(int productA, int productB, DateTime from, DateTime to)
    {
        var receipts = await _context.Receipts
            .Where(r => r.CreatedAt >= from && r.CreatedAt <= to && !r.IsVoided)
            .Include(r => r.Items)
            .ToListAsync();

        var totalTransactions = receipts.Count;
        if (totalTransactions == 0)
            return null;

        var countA = receipts.Count(r => r.Items.Any(i => i.ProductId == productA));
        var countB = receipts.Count(r => r.Items.Any(i => i.ProductId == productB));
        var countBoth = receipts.Count(r =>
            r.Items.Any(i => i.ProductId == productA) &&
            r.Items.Any(i => i.ProductId == productB));

        if (countA == 0 || countB == 0)
            return null;

        var support = (decimal)countBoth / totalTransactions;
        var confidence = (decimal)countBoth / countA;
        var pB = (decimal)countB / totalTransactions;
        var lift = confidence / pB;

        return new AssociationMetrics
        {
            ProductAId = productA,
            ProductBId = productB,
            Support = support,
            Confidence = confidence,
            Lift = lift,
            CoOccurrenceCount = countBoth,
            ProductACount = countA,
            ProductBCount = countB,
            TotalTransactions = totalTransactions
        };
    }

    #endregion

    #region Manual Rules

    public async Task<UpsellRuleDto> CreateRuleAsync(CreateUpsellRuleRequest request)
    {
        var rule = new UpsellRule
        {
            Name = request.Name,
            Description = request.Description,
            SourceProductId = request.SourceProductId,
            SourceCategoryId = request.SourceCategoryId,
            TargetProductId = request.TargetProductId,
            Type = request.Type,
            SuggestionText = request.SuggestionText,
            SavingsAmount = request.SavingsAmount,
            Priority = request.Priority,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            MaxSuggestionsPerDay = request.MaxSuggestionsPerDay,
            TimeOfDayFilter = request.TimeOfDayFilter,
            StoreId = request.StoreId,
            IsEnabled = true,
            IsActive = true
        };

        _context.UpsellRules.Add(rule);
        await _context.SaveChangesAsync();

        return await GetRuleAsync(rule.Id) ?? throw new InvalidOperationException("Failed to create rule");
    }

    public async Task<UpsellRuleDto> UpdateRuleAsync(int ruleId, CreateUpsellRuleRequest request)
    {
        var rule = await _context.UpsellRules.FindAsync(ruleId)
            ?? throw new InvalidOperationException("Rule not found");

        rule.Name = request.Name;
        rule.Description = request.Description;
        rule.SourceProductId = request.SourceProductId;
        rule.SourceCategoryId = request.SourceCategoryId;
        rule.TargetProductId = request.TargetProductId;
        rule.Type = request.Type;
        rule.SuggestionText = request.SuggestionText;
        rule.SavingsAmount = request.SavingsAmount;
        rule.Priority = request.Priority;
        rule.StartDate = request.StartDate;
        rule.EndDate = request.EndDate;
        rule.MaxSuggestionsPerDay = request.MaxSuggestionsPerDay;
        rule.TimeOfDayFilter = request.TimeOfDayFilter;
        rule.StoreId = request.StoreId;

        await _context.SaveChangesAsync();

        return await GetRuleAsync(rule.Id) ?? throw new InvalidOperationException("Failed to update rule");
    }

    public async Task DeleteRuleAsync(int ruleId)
    {
        var rule = await _context.UpsellRules.FindAsync(ruleId);
        if (rule == null)
            return;

        rule.IsActive = false;
        await _context.SaveChangesAsync();
    }

    public async Task<List<UpsellRuleDto>> GetRulesAsync(bool includeInactive = false, int? storeId = null)
    {
        var query = _context.UpsellRules
            .Include(r => r.SourceProduct)
            .Include(r => r.SourceCategory)
            .Include(r => r.TargetProduct)
            .Where(r => includeInactive || r.IsActive)
            .Where(r => !storeId.HasValue || r.StoreId == null || r.StoreId == storeId)
            .OrderByDescending(r => r.Priority);

        var rules = await query.ToListAsync();
        return rules.Select(MapToRuleDto).ToList();
    }

    public async Task<UpsellRuleDto?> GetRuleAsync(int ruleId)
    {
        var rule = await _context.UpsellRules
            .Include(r => r.SourceProduct)
            .Include(r => r.SourceCategory)
            .Include(r => r.TargetProduct)
            .FirstOrDefaultAsync(r => r.Id == ruleId);

        return rule != null ? MapToRuleDto(rule) : null;
    }

    public async Task<List<UpsellRuleDto>> GetRulesForProductAsync(int productId)
    {
        var rules = await _context.UpsellRules
            .Include(r => r.SourceProduct)
            .Include(r => r.SourceCategory)
            .Include(r => r.TargetProduct)
            .Where(r => r.IsActive && r.IsEnabled)
            .Where(r => r.SourceProductId == productId || r.TargetProductId == productId)
            .OrderByDescending(r => r.Priority)
            .ToListAsync();

        return rules.Select(MapToRuleDto).ToList();
    }

    public async Task EnableRuleAsync(int ruleId)
    {
        var rule = await _context.UpsellRules.FindAsync(ruleId);
        if (rule != null)
        {
            rule.IsEnabled = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DisableRuleAsync(int ruleId)
    {
        var rule = await _context.UpsellRules.FindAsync(ruleId);
        if (rule != null)
        {
            rule.IsEnabled = false;
            await _context.SaveChangesAsync();
        }
    }

    #endregion

    #region Customer Preferences

    public async Task UpdateCustomerPreferencesAsync(int customerId)
    {
        _logger.LogInformation("Updating preferences for customer {CustomerId}", customerId);

        // Get all purchases for this customer
        var purchases = await _context.ReceiptItems
            .Where(ri => ri.Receipt!.CustomerId == customerId && !ri.Receipt.IsVoided)
            .GroupBy(ri => ri.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                PurchaseCount = g.Count(),
                TotalSpent = g.Sum(ri => ri.LineTotal),
                AverageQuantity = g.Average(ri => (decimal)ri.Quantity),
                FirstPurchased = g.Min(ri => ri.Receipt!.CreatedAt),
                LastPurchased = g.Max(ri => ri.Receipt!.CreatedAt)
            })
            .ToListAsync();

        if (!purchases.Any())
            return;

        // Calculate max values for normalization
        var maxCount = purchases.Max(p => p.PurchaseCount);
        var maxSpent = purchases.Max(p => p.TotalSpent);
        var now = DateTime.UtcNow;

        // Get or create preferences
        var existingPrefs = await _context.CustomerPreferences
            .Where(cp => cp.CustomerId == customerId)
            .ToDictionaryAsync(cp => cp.ProductId);

        foreach (var purchase in purchases)
        {
            // Calculate preference score (RFM-like)
            var recency = 1 - Math.Min(1, (now - purchase.LastPurchased).TotalDays / 90);
            var frequency = (decimal)purchase.PurchaseCount / maxCount;
            var monetary = purchase.TotalSpent / maxSpent;
            var score = (decimal)((0.3 * (double)recency) + (0.4 * (double)frequency) + (0.3 * (double)monetary));

            if (existingPrefs.TryGetValue(purchase.ProductId, out var existing))
            {
                existing.PurchaseCount = purchase.PurchaseCount;
                existing.TotalSpent = purchase.TotalSpent;
                existing.AverageQuantity = purchase.AverageQuantity;
                existing.FirstPurchased = purchase.FirstPurchased;
                existing.LastPurchased = purchase.LastPurchased;
                existing.PreferenceScore = score;
                existing.CalculatedAt = now;
            }
            else
            {
                _context.CustomerPreferences.Add(new CustomerPreference
                {
                    CustomerId = customerId,
                    ProductId = purchase.ProductId,
                    PurchaseCount = purchase.PurchaseCount,
                    TotalSpent = purchase.TotalSpent,
                    AverageQuantity = purchase.AverageQuantity,
                    FirstPurchased = purchase.FirstPurchased,
                    LastPurchased = purchase.LastPurchased,
                    PreferenceScore = score,
                    CalculatedAt = now,
                    IsActive = true
                });
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task UpdateAllCustomerPreferencesAsync()
    {
        _logger.LogInformation("Starting customer preference update for all customers");

        var customerIds = await _context.LoyaltyMembers
            .Where(m => m.IsActive)
            .Select(m => m.Id)
            .ToListAsync();

        var count = 0;
        foreach (var customerId in customerIds)
        {
            try
            {
                await UpdateCustomerPreferencesAsync(customerId);
                count++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update preferences for customer {CustomerId}", customerId);
            }
        }

        _logger.LogInformation("Completed customer preference update: {Count} customers processed", count);
    }

    public async Task<List<CustomerPreferenceDto>> GetCustomerPreferencesAsync(int customerId, int top = 10)
    {
        var preferences = await _context.CustomerPreferences
            .Include(cp => cp.Product)
            .Where(cp => cp.CustomerId == customerId && cp.IsActive)
            .OrderByDescending(cp => cp.PreferenceScore)
            .Take(top)
            .ToListAsync();

        return preferences.Select(p => new CustomerPreferenceDto
        {
            Id = p.Id,
            CustomerId = p.CustomerId,
            ProductId = p.ProductId,
            ProductName = p.Product?.Name ?? "Unknown",
            ProductImageUrl = p.Product?.ImageUrl,
            PurchaseCount = p.PurchaseCount,
            TotalSpent = p.TotalSpent,
            AverageQuantity = p.AverageQuantity,
            LastPurchased = p.LastPurchased,
            FirstPurchased = p.FirstPurchased,
            PreferenceScore = p.PreferenceScore
        }).ToList();
    }

    #endregion

    #region Analytics

    public async Task<UpsellAnalytics> GetAnalyticsAsync(DateTime from, DateTime to, int? storeId = null)
    {
        var query = _context.UpsellSuggestionLogs
            .Where(l => l.SuggestedAt >= from && l.SuggestedAt <= to);

        if (storeId.HasValue)
            query = query.Where(l => l.StoreId == storeId);

        var logs = await query.ToListAsync();

        var totalSuggestions = logs.Count;
        var acceptedSuggestions = logs.Count(l => l.WasAccepted == true);
        var totalRevenue = logs.Where(l => l.WasAccepted == true).Sum(l => l.AcceptedValue ?? 0);

        var byType = logs
            .GroupBy(l => l.SuggestionType)
            .Select(g => new SuggestionTypeMetrics
            {
                Type = g.Key,
                TypeName = g.Key.ToString(),
                Suggestions = g.Count(),
                Accepted = g.Count(l => l.WasAccepted == true),
                AcceptanceRate = g.Count() > 0 ? (decimal)g.Count(l => l.WasAccepted == true) / g.Count() : 0,
                Revenue = g.Where(l => l.WasAccepted == true).Sum(l => l.AcceptedValue ?? 0)
            })
            .ToList();

        var dailyTrend = logs
            .GroupBy(l => l.SuggestedAt.Date)
            .Select(g => new DailyUpsellMetrics
            {
                Date = g.Key,
                Suggestions = g.Count(),
                Accepted = g.Count(l => l.WasAccepted == true),
                AcceptanceRate = g.Count() > 0 ? (decimal)g.Count(l => l.WasAccepted == true) / g.Count() : 0,
                Revenue = g.Where(l => l.WasAccepted == true).Sum(l => l.AcceptedValue ?? 0)
            })
            .OrderBy(d => d.Date)
            .ToList();

        return new UpsellAnalytics
        {
            FromDate = from,
            ToDate = to,
            TotalSuggestions = totalSuggestions,
            AcceptedSuggestions = acceptedSuggestions,
            AcceptanceRate = totalSuggestions > 0 ? (decimal)acceptedSuggestions / totalSuggestions : 0,
            TotalRevenue = totalRevenue,
            AverageOrderValueIncrease = acceptedSuggestions > 0 ? totalRevenue / acceptedSuggestions : 0,
            ByType = byType,
            DailyTrend = dailyTrend
        };
    }

    public async Task<UpsellPerformanceReport> GetPerformanceReportAsync(DateTime from, DateTime to, int? storeId = null)
    {
        var query = _context.UpsellSuggestionLogs
            .Include(l => l.SuggestedProduct)
            .Include(l => l.Rule)
            .Include(l => l.Association)
            .ThenInclude(a => a!.Product)
            .Include(l => l.Association)
            .ThenInclude(a => a!.AssociatedProduct)
            .Where(l => l.SuggestedAt >= from && l.SuggestedAt <= to);

        if (storeId.HasValue)
            query = query.Where(l => l.StoreId == storeId);

        var logs = await query.ToListAsync();

        var topProducts = logs
            .GroupBy(l => l.SuggestedProductId)
            .Select(g => new TopUpsellProduct
            {
                ProductId = g.Key,
                ProductName = g.First().SuggestedProduct?.Name ?? "Unknown",
                TimesShown = g.Count(),
                TimesAccepted = g.Count(l => l.WasAccepted == true),
                AcceptanceRate = g.Count() > 0 ? (decimal)g.Count(l => l.WasAccepted == true) / g.Count() : 0,
                TotalRevenue = g.Where(l => l.WasAccepted == true).Sum(l => l.AcceptedValue ?? 0)
            })
            .OrderByDescending(p => p.TotalRevenue)
            .Take(10)
            .ToList();

        var topRules = logs
            .Where(l => l.RuleId.HasValue)
            .GroupBy(l => l.RuleId!.Value)
            .Select(g => new TopUpsellRule
            {
                RuleId = g.Key,
                RuleName = g.First().Rule?.Name ?? "Unknown",
                Type = g.First().SuggestionType,
                TimesTriggered = g.Count(),
                TimesAccepted = g.Count(l => l.WasAccepted == true),
                AcceptanceRate = g.Count() > 0 ? (decimal)g.Count(l => l.WasAccepted == true) / g.Count() : 0,
                TotalRevenue = g.Where(l => l.WasAccepted == true).Sum(l => l.AcceptedValue ?? 0)
            })
            .OrderByDescending(r => r.TotalRevenue)
            .Take(10)
            .ToList();

        var topAssociations = logs
            .Where(l => l.AssociationId.HasValue)
            .GroupBy(l => l.AssociationId!.Value)
            .Select(g => new TopAssociation
            {
                AssociationId = g.Key,
                SourceProductName = g.First().Association?.Product?.Name ?? "Unknown",
                TargetProductName = g.First().Association?.AssociatedProduct?.Name ?? "Unknown",
                Lift = g.First().Association?.Lift ?? 0,
                TimesShown = g.Count(),
                TimesAccepted = g.Count(l => l.WasAccepted == true),
                AcceptanceRate = g.Count() > 0 ? (decimal)g.Count(l => l.WasAccepted == true) / g.Count() : 0,
                TotalRevenue = g.Where(l => l.WasAccepted == true).Sum(l => l.AcceptedValue ?? 0)
            })
            .OrderByDescending(a => a.TotalRevenue)
            .Take(10)
            .ToList();

        return new UpsellPerformanceReport
        {
            FromDate = from,
            ToDate = to,
            TopProducts = topProducts,
            TopRules = topRules,
            TopAssociations = topAssociations
        };
    }

    public async Task<List<TopUpsellProduct>> GetTopUpsellProductsAsync(DateTime from, DateTime to, int top = 10, int? storeId = null)
    {
        var query = _context.UpsellSuggestionLogs
            .Include(l => l.SuggestedProduct)
            .Where(l => l.SuggestedAt >= from && l.SuggestedAt <= to);

        if (storeId.HasValue)
            query = query.Where(l => l.StoreId == storeId);

        var logs = await query.ToListAsync();

        return logs
            .GroupBy(l => l.SuggestedProductId)
            .Select(g => new TopUpsellProduct
            {
                ProductId = g.Key,
                ProductName = g.First().SuggestedProduct?.Name ?? "Unknown",
                TimesShown = g.Count(),
                TimesAccepted = g.Count(l => l.WasAccepted == true),
                AcceptanceRate = g.Count() > 0 ? (decimal)g.Count(l => l.WasAccepted == true) / g.Count() : 0,
                TotalRevenue = g.Where(l => l.WasAccepted == true).Sum(l => l.AcceptedValue ?? 0)
            })
            .OrderByDescending(p => p.TotalRevenue)
            .Take(top)
            .ToList();
    }

    #endregion

    #region Configuration

    public async Task<UpsellConfigurationDto> GetConfigurationAsync(int? storeId = null)
    {
        var config = await GetConfigurationEntityAsync(storeId);
        return MapToConfigurationDto(config);
    }

    public async Task<UpsellConfigurationDto> UpdateConfigurationAsync(UpsellConfigurationDto dto)
    {
        var config = await _context.UpsellConfigurations
            .FirstOrDefaultAsync(c => c.IsActive && (c.StoreId == dto.StoreId || (c.StoreId == null && dto.StoreId == null)));

        if (config == null)
        {
            config = new UpsellConfiguration { StoreId = dto.StoreId, IsActive = true };
            _context.UpsellConfigurations.Add(config);
        }

        config.IsEnabled = dto.IsEnabled;
        config.MaxSuggestions = dto.MaxSuggestions;
        config.MinConfidenceScore = dto.MinConfidenceScore;
        config.MinSupport = dto.MinSupport;
        config.MinAssociationConfidence = dto.MinAssociationConfidence;
        config.MinLift = dto.MinLift;
        config.AnalysisDays = dto.AnalysisDays;
        config.IncludePersonalized = dto.IncludePersonalized;
        config.IncludeTrending = dto.IncludeTrending;
        config.EnforceCategoryDiversity = dto.EnforceCategoryDiversity;
        config.ExcludeRecentPurchaseDays = dto.ExcludeRecentPurchaseDays;
        config.RuleWeight = dto.RuleWeight;
        config.AssociationWeight = dto.AssociationWeight;
        config.PersonalizedWeight = dto.PersonalizedWeight;
        config.TrendingWeight = dto.TrendingWeight;
        config.TrendingDays = dto.TrendingDays;
        config.ShowSavingsAmount = dto.ShowSavingsAmount;
        config.DefaultSuggestionText = dto.DefaultSuggestionText;

        await _context.SaveChangesAsync();

        return MapToConfigurationDto(config);
    }

    private async Task<UpsellConfiguration> GetConfigurationEntityAsync(int? storeId)
    {
        var config = await _context.UpsellConfigurations
            .FirstOrDefaultAsync(c => c.IsActive && c.StoreId == storeId);

        if (config == null && storeId.HasValue)
        {
            // Fall back to global config
            config = await _context.UpsellConfigurations
                .FirstOrDefaultAsync(c => c.IsActive && c.StoreId == null);
        }

        return config ?? new UpsellConfiguration();
    }

    #endregion

    #region Mapping Methods

    private static ProductAssociationDto MapToAssociationDto(ProductAssociation a)
    {
        return new ProductAssociationDto
        {
            Id = a.Id,
            ProductId = a.ProductId,
            ProductName = a.Product?.Name ?? "Unknown",
            AssociatedProductId = a.AssociatedProductId,
            AssociatedProductName = a.AssociatedProduct?.Name ?? "Unknown",
            Type = a.Type,
            Support = a.Support,
            Confidence = a.Confidence,
            Lift = a.Lift,
            TransactionCount = a.TransactionCount,
            CalculatedAt = a.CalculatedAt,
            IsActive = a.IsActive
        };
    }

    private static UpsellRuleDto MapToRuleDto(UpsellRule r)
    {
        return new UpsellRuleDto
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            SourceProductId = r.SourceProductId,
            SourceProductName = r.SourceProduct?.Name,
            SourceCategoryId = r.SourceCategoryId,
            SourceCategoryName = r.SourceCategory?.Name,
            TargetProductId = r.TargetProductId,
            TargetProductName = r.TargetProduct?.Name ?? "Unknown",
            Type = r.Type,
            SuggestionText = r.SuggestionText,
            SavingsAmount = r.SavingsAmount,
            Priority = r.Priority,
            IsEnabled = r.IsEnabled,
            StartDate = r.StartDate,
            EndDate = r.EndDate,
            MaxSuggestionsPerDay = r.MaxSuggestionsPerDay,
            TimeOfDayFilter = r.TimeOfDayFilter,
            StoreId = r.StoreId,
            IsActive = r.IsActive,
            CreatedAt = r.CreatedAt
        };
    }

    private static UpsellConfigurationDto MapToConfigurationDto(UpsellConfiguration c)
    {
        return new UpsellConfigurationDto
        {
            Id = c.Id,
            StoreId = c.StoreId,
            IsEnabled = c.IsEnabled,
            MaxSuggestions = c.MaxSuggestions,
            MinConfidenceScore = c.MinConfidenceScore,
            MinSupport = c.MinSupport,
            MinAssociationConfidence = c.MinAssociationConfidence,
            MinLift = c.MinLift,
            AnalysisDays = c.AnalysisDays,
            IncludePersonalized = c.IncludePersonalized,
            IncludeTrending = c.IncludeTrending,
            EnforceCategoryDiversity = c.EnforceCategoryDiversity,
            ExcludeRecentPurchaseDays = c.ExcludeRecentPurchaseDays,
            RuleWeight = c.RuleWeight,
            AssociationWeight = c.AssociationWeight,
            PersonalizedWeight = c.PersonalizedWeight,
            TrendingWeight = c.TrendingWeight,
            TrendingDays = c.TrendingDays,
            ShowSavingsAmount = c.ShowSavingsAmount,
            DefaultSuggestionText = c.DefaultSuggestionText
        };
    }

    #endregion
}
