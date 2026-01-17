// src/HospitalityPOS.Infrastructure/Services/TimePricingService.cs
// Service implementation for Time-Based Pricing (Happy Hour, Lunch Specials, etc.)
// Story 44-2: Happy Hour / Time-Based Pricing

using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Pricing;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing time-based pricing rules and calculating effective prices.
/// Supports Happy Hour, Lunch Specials, and other time-limited promotions.
/// </summary>
public class TimePricingService : ITimePricingService
{
    private readonly Dictionary<int, TimePricingRule> _rules = new();
    private readonly List<TimePricingDiscountRecord> _discountRecords = new();
    private int _nextRuleId = 1;
    private HashSet<int> _previouslyActiveRuleIds = new();

    // Configurable lead time for "starting soon" and "ending soon" notifications
    private readonly TimeSpan _notificationLeadTime = TimeSpan.FromMinutes(15);

    #region Events

    /// <inheritdoc />
    public event EventHandler<PromotionActivationChangedEventArgs>? PromotionActivationChanged;

    /// <inheritdoc />
    public event EventHandler<TimePricingRule>? PromotionStarting;

    /// <inheritdoc />
    public event EventHandler<TimePricingRule>? PromotionEnding;

    #endregion

    #region Rule Management

    /// <inheritdoc />
    public Task<TimePricingRule> CreateRuleAsync(TimePricingRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        rule.Id = _nextRuleId++;
        rule.CreatedAt = DateTime.UtcNow;

        // Validate rule
        ValidateRule(rule);

        _rules[rule.Id] = rule;

        return Task.FromResult(rule);
    }

    /// <inheritdoc />
    public Task<TimePricingRule> UpdateRuleAsync(TimePricingRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        if (!_rules.ContainsKey(rule.Id))
        {
            throw new InvalidOperationException($"Rule with ID {rule.Id} not found.");
        }

        // Validate rule
        ValidateRule(rule);

        rule.UpdatedAt = DateTime.UtcNow;
        _rules[rule.Id] = rule;

        return Task.FromResult(rule);
    }

    /// <inheritdoc />
    public Task DeleteRuleAsync(int ruleId)
    {
        if (!_rules.ContainsKey(ruleId))
        {
            throw new InvalidOperationException($"Rule with ID {ruleId} not found.");
        }

        _rules.Remove(ruleId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<TimePricingRule?> GetRuleAsync(int ruleId)
    {
        _rules.TryGetValue(ruleId, out var rule);
        return Task.FromResult(rule);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TimePricingRule>> GetAllRulesAsync()
    {
        return Task.FromResult<IReadOnlyList<TimePricingRule>>(_rules.Values.ToList());
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TimePricingRule>> GetActiveRulesAsync()
    {
        var activeRules = _rules.Values.Where(r => r.IsActive).ToList();
        return Task.FromResult<IReadOnlyList<TimePricingRule>>(activeRules);
    }

    /// <inheritdoc />
    public Task SetRuleActiveAsync(int ruleId, bool isActive)
    {
        if (!_rules.TryGetValue(ruleId, out var rule))
        {
            throw new InvalidOperationException($"Rule with ID {ruleId} not found.");
        }

        rule.IsActive = isActive;
        rule.UpdatedAt = DateTime.UtcNow;

        return Task.CompletedTask;
    }

    private void ValidateRule(TimePricingRule rule)
    {
        if (string.IsNullOrWhiteSpace(rule.Name))
        {
            throw new ArgumentException("Rule name is required.", nameof(rule));
        }

        if (rule.ApplicableDays == null || rule.ApplicableDays.Count == 0)
        {
            throw new ArgumentException("At least one applicable day is required.", nameof(rule));
        }

        if (rule.DiscountType == TimeDiscountType.Percentage && (rule.DiscountValue <= 0 || rule.DiscountValue > 100))
        {
            throw new ArgumentException("Percentage discount must be between 0 and 100.", nameof(rule));
        }

        if (rule.DiscountType == TimeDiscountType.FixedPrice && (!rule.FixedPrice.HasValue || rule.FixedPrice.Value < 0))
        {
            throw new ArgumentException("Fixed price must be specified and non-negative.", nameof(rule));
        }

        if (rule.DiscountType == TimeDiscountType.AmountOff && rule.DiscountValue <= 0)
        {
            throw new ArgumentException("Amount off must be greater than zero.", nameof(rule));
        }

        if (rule.ValidFrom.HasValue && rule.ValidTo.HasValue && rule.ValidFrom.Value > rule.ValidTo.Value)
        {
            throw new ArgumentException("Valid from date must be before valid to date.", nameof(rule));
        }

        // Validate scope requirements
        if (rule.Scope == PricingRuleScope.SpecificProducts && (rule.ProductIds == null || rule.ProductIds.Count == 0))
        {
            throw new ArgumentException("At least one product ID is required for SpecificProducts scope.", nameof(rule));
        }

        if (rule.Scope == PricingRuleScope.Categories && (rule.CategoryIds == null || rule.CategoryIds.Count == 0))
        {
            throw new ArgumentException("At least one category ID is required for Categories scope.", nameof(rule));
        }
    }

    #endregion

    #region Price Calculation

    /// <inheritdoc />
    public Task<ProductPricingInfo> GetEffectivePriceAsync(int productId, int categoryId, decimal regularPrice)
    {
        return GetEffectivePriceAtTimeAsync(productId, categoryId, regularPrice, DateTime.Now);
    }

    /// <inheritdoc />
    public Task<ProductPricingInfo> GetEffectivePriceAtTimeAsync(int productId, int categoryId, decimal regularPrice, DateTime atTime)
    {
        var applicableRule = FindBestApplicableRule(productId, categoryId, atTime);

        var pricingInfo = new ProductPricingInfo
        {
            ProductId = productId,
            CategoryId = categoryId,
            RegularPrice = regularPrice
        };

        if (applicableRule != null)
        {
            pricingInfo.EffectivePrice = applicableRule.CalculateDiscountedPrice(regularPrice);
            pricingInfo.HasTimePricingDiscount = true;
            pricingInfo.ActiveRule = applicableRule;
        }
        else
        {
            pricingInfo.EffectivePrice = regularPrice;
            pricingInfo.HasTimePricingDiscount = false;
        }

        return Task.FromResult(pricingInfo);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ProductPricingInfo>> GetEffectivePricesAsync(
        IEnumerable<(int ProductId, int CategoryId, decimal RegularPrice, string ProductName)> products)
    {
        var currentTime = DateTime.Now;
        var results = new List<ProductPricingInfo>();

        foreach (var (productId, categoryId, regularPrice, productName) in products)
        {
            var applicableRule = FindBestApplicableRule(productId, categoryId, currentTime);

            var pricingInfo = new ProductPricingInfo
            {
                ProductId = productId,
                ProductName = productName,
                CategoryId = categoryId,
                RegularPrice = regularPrice
            };

            if (applicableRule != null)
            {
                pricingInfo.EffectivePrice = applicableRule.CalculateDiscountedPrice(regularPrice);
                pricingInfo.HasTimePricingDiscount = true;
                pricingInfo.ActiveRule = applicableRule;
            }
            else
            {
                pricingInfo.EffectivePrice = regularPrice;
                pricingInfo.HasTimePricingDiscount = false;
            }

            results.Add(pricingInfo);
        }

        return Task.FromResult<IReadOnlyList<ProductPricingInfo>>(results);
    }

    /// <inheritdoc />
    public Task<bool> HasActiveDiscountAsync(int productId, int categoryId)
    {
        var currentTime = DateTime.Now;
        var applicableRule = FindBestApplicableRule(productId, categoryId, currentTime);
        return Task.FromResult(applicableRule != null);
    }

    private TimePricingRule? FindBestApplicableRule(int productId, int categoryId, DateTime atTime)
    {
        var applicableRules = _rules.Values
            .Where(r => r.IsActive && r.IsCurrentlyActive(atTime) && RuleAppliesToProduct(r, productId, categoryId))
            .OrderByDescending(r => r.Priority)
            .ThenByDescending(r => r.Id) // More recent rules take precedence at same priority
            .ToList();

        return applicableRules.FirstOrDefault();
    }

    private bool RuleAppliesToProduct(TimePricingRule rule, int productId, int categoryId)
    {
        return rule.Scope switch
        {
            PricingRuleScope.AllProducts => true,
            PricingRuleScope.SpecificProducts => rule.ProductIds.Contains(productId),
            PricingRuleScope.Categories => rule.CategoryIds.Contains(categoryId),
            _ => false
        };
    }

    #endregion

    #region Active Promotions

    /// <inheritdoc />
    public async Task<ActivePromotionStatus> GetActivePromotionsAsync()
    {
        var currentTime = DateTime.Now;
        var activeRules = await GetCurrentlyActiveRulesAsync();

        var status = new ActivePromotionStatus
        {
            ActiveRules = activeRules.ToList(),
            IsHappyHourActive = activeRules.Any(),
            CheckedAt = currentTime
        };

        if (activeRules.Count > 0)
        {
            // Get the highest priority rule as primary
            var primaryRule = activeRules.OrderByDescending(r => r.Priority).First();
            status.PrimaryPromotionName = primaryRule.Name;
            status.EndsAt = primaryRule.EndTime;

            // Calculate affected products
            status.AffectedProductCount = await GetAffectedProductCountAsync(activeRules);
        }

        return status;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TimePricingRule>> GetCurrentlyActiveRulesAsync()
    {
        var currentTime = DateTime.Now;
        var activeRules = _rules.Values
            .Where(r => r.IsActive && r.IsCurrentlyActive(currentTime))
            .OrderByDescending(r => r.Priority)
            .ToList();

        return Task.FromResult<IReadOnlyList<TimePricingRule>>(activeRules);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TimePricingRule>> GetRulesForProductAsync(int productId, int categoryId)
    {
        var rules = _rules.Values
            .Where(r => r.IsActive && RuleAppliesToProduct(r, productId, categoryId))
            .OrderByDescending(r => r.Priority)
            .ToList();

        return Task.FromResult<IReadOnlyList<TimePricingRule>>(rules);
    }

    /// <inheritdoc />
    public Task<IReadOnlySet<int>> GetAffectedProductIdsAsync()
    {
        var currentTime = DateTime.Now;
        var activeRules = _rules.Values.Where(r => r.IsActive && r.IsCurrentlyActive(currentTime)).ToList();

        var productIds = new HashSet<int>();
        foreach (var rule in activeRules)
        {
            if (rule.Scope == PricingRuleScope.SpecificProducts)
            {
                foreach (var id in rule.ProductIds)
                {
                    productIds.Add(id);
                }
            }
        }

        return Task.FromResult<IReadOnlySet<int>>(productIds);
    }

    /// <inheritdoc />
    public Task<IReadOnlySet<int>> GetAffectedCategoryIdsAsync()
    {
        var currentTime = DateTime.Now;
        var activeRules = _rules.Values.Where(r => r.IsActive && r.IsCurrentlyActive(currentTime)).ToList();

        var categoryIds = new HashSet<int>();
        foreach (var rule in activeRules)
        {
            if (rule.Scope == PricingRuleScope.Categories)
            {
                foreach (var id in rule.CategoryIds)
                {
                    categoryIds.Add(id);
                }
            }
        }

        return Task.FromResult<IReadOnlySet<int>>(categoryIds);
    }

    private Task<int> GetAffectedProductCountAsync(IReadOnlyList<TimePricingRule> rules)
    {
        // Count specific products
        var specificProductCount = rules
            .Where(r => r.Scope == PricingRuleScope.SpecificProducts)
            .SelectMany(r => r.ProductIds)
            .Distinct()
            .Count();

        // For categories and all products scope, we'd need product catalog integration
        // For now, return specific product count plus estimate for categories
        var categoryCount = rules.Where(r => r.Scope == PricingRuleScope.Categories).Sum(r => r.CategoryIds.Count);
        var hasAllProducts = rules.Any(r => r.Scope == PricingRuleScope.AllProducts);

        var estimate = specificProductCount + (categoryCount * 10); // Estimate 10 products per category
        if (hasAllProducts) estimate += 100; // Estimate for all products

        return Task.FromResult(estimate);
    }

    #endregion

    #region Order Integration

    /// <inheritdoc />
    public async Task<IReadOnlyList<TimePricedOrderItem>> ApplyTimePricingToOrderAsync(
        IEnumerable<(int ProductId, int CategoryId, string ProductName, decimal RegularPrice, int Quantity)> items)
    {
        var currentTime = DateTime.Now;
        var results = new List<TimePricedOrderItem>();

        foreach (var (productId, categoryId, productName, regularPrice, quantity) in items)
        {
            var applicableRule = FindBestApplicableRule(productId, categoryId, currentTime);

            var item = new TimePricedOrderItem
            {
                ProductId = productId,
                ProductName = productName,
                Quantity = quantity,
                RegularPrice = regularPrice
            };

            if (applicableRule != null)
            {
                item.DiscountedPrice = applicableRule.CalculateDiscountedPrice(regularPrice);
                item.AppliedRuleId = applicableRule.Id;
                item.PromotionName = applicableRule.Name;
            }
            else
            {
                item.DiscountedPrice = regularPrice;
            }

            results.Add(item);
        }

        return results;
    }

    /// <inheritdoc />
    public Task RecordTimePricingDiscountsAsync(int orderId, IEnumerable<TimePricedOrderItem> items)
    {
        foreach (var item in items.Where(i => i.AppliedRuleId.HasValue))
        {
            _discountRecords.Add(new TimePricingDiscountRecord
            {
                OrderId = orderId,
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                RegularPrice = item.RegularPrice,
                DiscountedPrice = item.DiscountedPrice,
                RuleId = item.AppliedRuleId!.Value,
                PromotionName = item.PromotionName ?? string.Empty,
                RecordedAt = DateTime.Now
            });
        }

        return Task.CompletedTask;
    }

    #endregion

    #region Analytics

    /// <inheritdoc />
    public Task<TimePricingAnalytics> GetAnalyticsAsync(DateTime startDate, DateTime endDate)
    {
        var relevantRecords = _discountRecords
            .Where(r => r.RecordedAt >= startDate && r.RecordedAt <= endDate)
            .ToList();

        var analytics = new TimePricingAnalytics
        {
            StartDate = startDate,
            EndDate = endDate,
            PromotionPeriodSales = relevantRecords.Sum(r => r.DiscountedPrice * r.Quantity),
            TotalDiscountAmount = relevantRecords.Sum(r => r.TotalDiscount),
            PromotionTransactionCount = relevantRecords.Select(r => r.OrderId).Distinct().Count()
        };

        // Group by rule for breakdown
        var ruleGroups = relevantRecords.GroupBy(r => r.RuleId);
        foreach (var group in ruleGroups)
        {
            var rule = _rules.GetValueOrDefault(group.Key);
            analytics.PromotionBreakdown.Add(new PromotionPerformance
            {
                RuleId = group.Key,
                PromotionName = rule?.Name ?? group.First().PromotionName,
                TotalSales = group.Sum(r => r.DiscountedPrice * r.Quantity),
                TotalDiscount = group.Sum(r => r.TotalDiscount),
                OrderCount = group.Select(r => r.OrderId).Distinct().Count(),
                ItemsSold = group.Sum(r => r.Quantity)
            });
        }

        // Popular items
        var productGroups = relevantRecords.GroupBy(r => r.ProductId);
        foreach (var group in productGroups.OrderByDescending(g => g.Sum(r => r.Quantity)).Take(10))
        {
            analytics.PopularItems.Add(new PopularPromotionItem
            {
                ProductId = group.Key,
                ProductName = group.First().ProductName,
                QuantitySold = group.Sum(r => r.Quantity),
                Revenue = group.Sum(r => r.DiscountedPrice * r.Quantity),
                DiscountGiven = group.Sum(r => r.TotalDiscount)
            });
        }

        return Task.FromResult(analytics);
    }

    /// <inheritdoc />
    public Task<TimePricingAnalytics> GetTodayAnalyticsAsync()
    {
        var today = DateTime.Today;
        return GetAnalyticsAsync(today, today.AddDays(1).AddSeconds(-1));
    }

    /// <inheritdoc />
    public Task<PromotionPerformance> GetRulePerformanceAsync(int ruleId, DateTime startDate, DateTime endDate)
    {
        var rule = _rules.GetValueOrDefault(ruleId);
        var relevantRecords = _discountRecords
            .Where(r => r.RuleId == ruleId && r.RecordedAt >= startDate && r.RecordedAt <= endDate)
            .ToList();

        var performance = new PromotionPerformance
        {
            RuleId = ruleId,
            PromotionName = rule?.Name ?? (relevantRecords.FirstOrDefault()?.PromotionName ?? "Unknown"),
            TotalSales = relevantRecords.Sum(r => r.DiscountedPrice * r.Quantity),
            TotalDiscount = relevantRecords.Sum(r => r.TotalDiscount),
            OrderCount = relevantRecords.Select(r => r.OrderId).Distinct().Count(),
            ItemsSold = relevantRecords.Sum(r => r.Quantity)
        };

        // Calculate active hours
        if (rule != null)
        {
            var duration = rule.EndTime > rule.StartTime
                ? rule.EndTime.ToTimeSpan() - rule.StartTime.ToTimeSpan()
                : TimeSpan.FromHours(24) - rule.StartTime.ToTimeSpan() + rule.EndTime.ToTimeSpan();

            var days = (endDate - startDate).Days + 1;
            var applicableDays = Enumerable.Range(0, days)
                .Select(d => startDate.AddDays(d).DayOfWeek)
                .Count(dow => rule.ApplicableDays.Contains(dow));

            performance.ActiveHours = (decimal)(duration.TotalHours * applicableDays);
        }

        return Task.FromResult(performance);
    }

    #endregion

    #region Time Management

    /// <inheritdoc />
    public Task<TimeSpan?> GetTimeUntilNextPromotionAsync()
    {
        var currentTime = DateTime.Now;
        var currentTimeOfDay = TimeOnly.FromDateTime(currentTime);
        var currentDay = currentTime.DayOfWeek;

        TimeSpan? shortestTime = null;

        foreach (var rule in _rules.Values.Where(r => r.IsActive))
        {
            // Skip if currently active
            if (rule.IsCurrentlyActive(currentTime)) continue;

            // Check date validity
            var currentDate = DateOnly.FromDateTime(currentTime);
            if (rule.ValidFrom.HasValue && currentDate < rule.ValidFrom.Value) continue;
            if (rule.ValidTo.HasValue && currentDate > rule.ValidTo.Value) continue;

            // Find next occurrence
            for (int dayOffset = 0; dayOffset < 7; dayOffset++)
            {
                var checkDay = (DayOfWeek)(((int)currentDay + dayOffset) % 7);
                if (!rule.ApplicableDays.Contains(checkDay)) continue;

                TimeSpan timeUntil;
                if (dayOffset == 0 && currentTimeOfDay < rule.StartTime)
                {
                    // Today, later
                    timeUntil = rule.StartTime.ToTimeSpan() - currentTimeOfDay.ToTimeSpan();
                }
                else if (dayOffset > 0)
                {
                    // Future day
                    timeUntil = TimeSpan.FromDays(dayOffset) + rule.StartTime.ToTimeSpan() - currentTimeOfDay.ToTimeSpan();
                }
                else
                {
                    continue; // Today but already past start time (and not active, so past end)
                }

                if (!shortestTime.HasValue || timeUntil < shortestTime.Value)
                {
                    shortestTime = timeUntil;
                }
                break; // Found next occurrence for this rule
            }
        }

        return Task.FromResult(shortestTime);
    }

    /// <inheritdoc />
    public Task<TimeSpan?> GetTimeUntilPromotionEndsAsync()
    {
        var currentTime = DateTime.Now;
        var currentTimeOfDay = TimeOnly.FromDateTime(currentTime);

        var activeRules = _rules.Values
            .Where(r => r.IsActive && r.IsCurrentlyActive(currentTime))
            .ToList();

        if (!activeRules.Any())
        {
            return Task.FromResult<TimeSpan?>(null);
        }

        // Find the earliest end time among active rules
        TimeSpan? shortestTime = null;

        foreach (var rule in activeRules)
        {
            TimeSpan timeUntilEnd;

            if (rule.StartTime <= rule.EndTime)
            {
                // Normal window
                timeUntilEnd = rule.EndTime.ToTimeSpan() - currentTimeOfDay.ToTimeSpan();
            }
            else
            {
                // Crosses midnight
                if (currentTimeOfDay >= rule.StartTime)
                {
                    // Before midnight
                    timeUntilEnd = TimeSpan.FromHours(24) - currentTimeOfDay.ToTimeSpan() + rule.EndTime.ToTimeSpan();
                }
                else
                {
                    // After midnight
                    timeUntilEnd = rule.EndTime.ToTimeSpan() - currentTimeOfDay.ToTimeSpan();
                }
            }

            if (!shortestTime.HasValue || timeUntilEnd < shortestTime.Value)
            {
                shortestTime = timeUntilEnd;
            }
        }

        return Task.FromResult(shortestTime);
    }

    /// <inheritdoc />
    public async Task CheckPromotionChangesAsync()
    {
        var currentTime = DateTime.Now;
        var currentlyActiveRuleIds = _rules.Values
            .Where(r => r.IsActive && r.IsCurrentlyActive(currentTime))
            .Select(r => r.Id)
            .ToHashSet();

        var activated = currentlyActiveRuleIds.Except(_previouslyActiveRuleIds).ToList();
        var deactivated = _previouslyActiveRuleIds.Except(currentlyActiveRuleIds).ToList();

        if (activated.Count > 0 || deactivated.Count > 0)
        {
            var args = new PromotionActivationChangedEventArgs
            {
                IsActive = currentlyActiveRuleIds.Count > 0,
                ActivatedRules = activated.Select(id => _rules[id]).ToList(),
                DeactivatedRules = deactivated.Where(id => _rules.ContainsKey(id)).Select(id => _rules[id]).ToList(),
                Timestamp = currentTime
            };

            PromotionActivationChanged?.Invoke(this, args);
        }

        // Check for upcoming promotions (starting soon)
        foreach (var rule in _rules.Values.Where(r => r.IsActive && !r.IsCurrentlyActive(currentTime)))
        {
            var timeUntilStart = GetTimeUntilRuleStarts(rule, currentTime);
            if (timeUntilStart.HasValue && timeUntilStart.Value <= _notificationLeadTime && timeUntilStart.Value > TimeSpan.Zero)
            {
                PromotionStarting?.Invoke(this, rule);
            }
        }

        // Check for ending promotions
        foreach (var rule in _rules.Values.Where(r => r.IsActive && r.IsCurrentlyActive(currentTime)))
        {
            var timeUntilEnd = GetTimeUntilRuleEnds(rule, currentTime);
            if (timeUntilEnd.HasValue && timeUntilEnd.Value <= _notificationLeadTime)
            {
                PromotionEnding?.Invoke(this, rule);
            }
        }

        _previouslyActiveRuleIds = currentlyActiveRuleIds;
    }

    private TimeSpan? GetTimeUntilRuleStarts(TimePricingRule rule, DateTime currentTime)
    {
        var currentTimeOfDay = TimeOnly.FromDateTime(currentTime);
        var currentDay = currentTime.DayOfWeek;

        // Check date validity
        var currentDate = DateOnly.FromDateTime(currentTime);
        if (rule.ValidFrom.HasValue && currentDate < rule.ValidFrom.Value) return null;
        if (rule.ValidTo.HasValue && currentDate > rule.ValidTo.Value) return null;

        // Check if today and start time is in the future
        if (rule.ApplicableDays.Contains(currentDay) && currentTimeOfDay < rule.StartTime)
        {
            return rule.StartTime.ToTimeSpan() - currentTimeOfDay.ToTimeSpan();
        }

        return null; // Not starting today
    }

    private TimeSpan? GetTimeUntilRuleEnds(TimePricingRule rule, DateTime currentTime)
    {
        var currentTimeOfDay = TimeOnly.FromDateTime(currentTime);

        if (rule.StartTime <= rule.EndTime)
        {
            // Normal window
            return rule.EndTime.ToTimeSpan() - currentTimeOfDay.ToTimeSpan();
        }
        else
        {
            // Crosses midnight
            if (currentTimeOfDay >= rule.StartTime)
            {
                // Before midnight
                return TimeSpan.FromHours(24) - currentTimeOfDay.ToTimeSpan() + rule.EndTime.ToTimeSpan();
            }
            else
            {
                // After midnight
                return rule.EndTime.ToTimeSpan() - currentTimeOfDay.ToTimeSpan();
            }
        }
    }

    #endregion

    #region Sample Data

    /// <summary>
    /// Creates sample time pricing rules for testing/demo.
    /// </summary>
    public async Task CreateSampleRulesAsync()
    {
        // Happy Hour - 4pm to 7pm on weekdays
        await CreateRuleAsync(new TimePricingRule
        {
            Name = "Happy Hour",
            Description = "Daily happy hour with discounts on selected drinks",
            DisplayText = "Happy Hour",
            StartTime = new TimeOnly(16, 0),
            EndTime = new TimeOnly(19, 0),
            ApplicableDays = new List<DayOfWeek>
            {
                DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                DayOfWeek.Thursday, DayOfWeek.Friday
            },
            DiscountType = TimeDiscountType.Percentage,
            DiscountValue = 25,
            Priority = 10,
            CanStackWithOtherDiscounts = false,
            Scope = PricingRuleScope.Categories,
            CategoryIds = new List<int> { 3, 4 }, // Assume categories 3 and 4 are drinks
            BadgeColor = "#F59E0B", // Amber
            IconName = "clock"
        });

        // Lunch Special - 11am to 2pm on weekdays
        await CreateRuleAsync(new TimePricingRule
        {
            Name = "Lunch Special",
            Description = "Discounted lunch combos",
            DisplayText = "Lunch Deal",
            StartTime = new TimeOnly(11, 0),
            EndTime = new TimeOnly(14, 0),
            ApplicableDays = new List<DayOfWeek>
            {
                DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                DayOfWeek.Thursday, DayOfWeek.Friday
            },
            DiscountType = TimeDiscountType.Percentage,
            DiscountValue = 15,
            Priority = 5,
            CanStackWithOtherDiscounts = false,
            Scope = PricingRuleScope.Categories,
            CategoryIds = new List<int> { 1, 2 }, // Assume categories 1 and 2 are food
            BadgeColor = "#10B981", // Green
            IconName = "sun"
        });

        // Early Bird Special - 6am to 9am every day
        await CreateRuleAsync(new TimePricingRule
        {
            Name = "Early Bird",
            Description = "Early morning breakfast discounts",
            DisplayText = "Early Bird",
            StartTime = new TimeOnly(6, 0),
            EndTime = new TimeOnly(9, 0),
            ApplicableDays = new List<DayOfWeek>
            {
                DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday
            },
            DiscountType = TimeDiscountType.Percentage,
            DiscountValue = 20,
            Priority = 5,
            CanStackWithOtherDiscounts = false,
            Scope = PricingRuleScope.Categories,
            CategoryIds = new List<int> { 5 }, // Assume category 5 is breakfast
            BadgeColor = "#8B5CF6", // Purple
            IconName = "sunrise"
        });

        // Late Night Special - 10pm to 2am on weekends
        await CreateRuleAsync(new TimePricingRule
        {
            Name = "Late Night",
            Description = "Late night weekend specials",
            DisplayText = "Night Owl",
            StartTime = new TimeOnly(22, 0),
            EndTime = new TimeOnly(2, 0), // Crosses midnight
            ApplicableDays = new List<DayOfWeek>
            {
                DayOfWeek.Friday, DayOfWeek.Saturday
            },
            DiscountType = TimeDiscountType.Percentage,
            DiscountValue = 30,
            Priority = 15,
            CanStackWithOtherDiscounts = false,
            Scope = PricingRuleScope.AllProducts,
            BadgeColor = "#6366F1", // Indigo
            IconName = "moon"
        });

        // Fixed Price Special - Specific item at fixed price
        await CreateRuleAsync(new TimePricingRule
        {
            Name = "Tusker Tuesday",
            Description = "All Tusker beers at fixed price",
            DisplayText = "Tusker Deal",
            StartTime = new TimeOnly(17, 0),
            EndTime = new TimeOnly(21, 0),
            ApplicableDays = new List<DayOfWeek> { DayOfWeek.Tuesday },
            DiscountType = TimeDiscountType.FixedPrice,
            FixedPrice = 200, // KSh 200
            Priority = 20,
            CanStackWithOtherDiscounts = false,
            Scope = PricingRuleScope.SpecificProducts,
            ProductIds = new List<int> { 101, 102, 103 }, // Tusker product IDs
            BadgeColor = "#EF4444", // Red
            IconName = "beer"
        });
    }

    #endregion
}

/// <summary>
/// Internal record for tracking discount applications.
/// </summary>
internal class TimePricingDiscountRecord
{
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal RegularPrice { get; set; }
    public decimal DiscountedPrice { get; set; }
    public int RuleId { get; set; }
    public string PromotionName { get; set; } = string.Empty;
    public DateTime RecordedAt { get; set; }
    public decimal TotalDiscount => (RegularPrice - DiscountedPrice) * Quantity;
}
