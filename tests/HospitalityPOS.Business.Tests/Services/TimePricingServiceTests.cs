// tests/HospitalityPOS.Business.Tests/Services/TimePricingServiceTests.cs
// Unit tests for TimePricingService
// Story 44-2: Happy Hour / Time-Based Pricing

using FluentAssertions;
using HospitalityPOS.Core.Models.Pricing;
using HospitalityPOS.Infrastructure.Services;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

public class TimePricingServiceTests
{
    private readonly TimePricingService _service;

    public TimePricingServiceTests()
    {
        _service = new TimePricingService();
    }

    #region Rule Management Tests

    [Fact]
    public async Task CreateRuleAsync_ValidRule_ReturnsRuleWithId()
    {
        // Arrange
        var rule = CreateTestRule("Happy Hour", TimeDiscountType.Percentage, 25);

        // Act
        var result = await _service.CreateRuleAsync(rule);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("Happy Hour");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateRuleAsync_NullRule_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.CreateRuleAsync(null!));
    }

    [Fact]
    public async Task CreateRuleAsync_EmptyName_ThrowsArgumentException()
    {
        // Arrange
        var rule = CreateTestRule("", TimeDiscountType.Percentage, 25);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateRuleAsync(rule));
    }

    [Fact]
    public async Task CreateRuleAsync_NoApplicableDays_ThrowsArgumentException()
    {
        // Arrange
        var rule = CreateTestRule("Test", TimeDiscountType.Percentage, 25);
        rule.ApplicableDays.Clear();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateRuleAsync(rule));
    }

    [Fact]
    public async Task CreateRuleAsync_InvalidPercentage_ThrowsArgumentException()
    {
        // Arrange
        var rule = CreateTestRule("Test", TimeDiscountType.Percentage, 150);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateRuleAsync(rule));
    }

    [Fact]
    public async Task CreateRuleAsync_FixedPriceWithoutValue_ThrowsArgumentException()
    {
        // Arrange
        var rule = CreateTestRule("Test", TimeDiscountType.FixedPrice, 0);
        rule.FixedPrice = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateRuleAsync(rule));
    }

    [Fact]
    public async Task UpdateRuleAsync_ExistingRule_UpdatesSuccessfully()
    {
        // Arrange
        var rule = await _service.CreateRuleAsync(CreateTestRule("Original", TimeDiscountType.Percentage, 20));
        rule.Name = "Updated";
        rule.DiscountValue = 30;

        // Act
        var result = await _service.UpdateRuleAsync(rule);

        // Assert
        result.Name.Should().Be("Updated");
        result.DiscountValue.Should().Be(30);
        result.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateRuleAsync_NonExistentRule_ThrowsInvalidOperationException()
    {
        // Arrange
        var rule = CreateTestRule("Test", TimeDiscountType.Percentage, 25);
        rule.Id = 999;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateRuleAsync(rule));
    }

    [Fact]
    public async Task DeleteRuleAsync_ExistingRule_DeletesSuccessfully()
    {
        // Arrange
        var rule = await _service.CreateRuleAsync(CreateTestRule("ToDelete", TimeDiscountType.Percentage, 20));

        // Act
        await _service.DeleteRuleAsync(rule.Id);

        // Assert
        var result = await _service.GetRuleAsync(rule.Id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteRuleAsync_NonExistentRule_ThrowsInvalidOperationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteRuleAsync(999));
    }

    [Fact]
    public async Task GetRuleAsync_ExistingRule_ReturnsRule()
    {
        // Arrange
        var created = await _service.CreateRuleAsync(CreateTestRule("Test", TimeDiscountType.Percentage, 20));

        // Act
        var result = await _service.GetRuleAsync(created.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
    }

    [Fact]
    public async Task GetRuleAsync_NonExistentRule_ReturnsNull()
    {
        // Act
        var result = await _service.GetRuleAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllRulesAsync_MultipleRules_ReturnsAll()
    {
        // Arrange
        await _service.CreateRuleAsync(CreateTestRule("Rule1", TimeDiscountType.Percentage, 10));
        await _service.CreateRuleAsync(CreateTestRule("Rule2", TimeDiscountType.Percentage, 20));
        await _service.CreateRuleAsync(CreateTestRule("Rule3", TimeDiscountType.Percentage, 30));

        // Act
        var result = await _service.GetAllRulesAsync();

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetActiveRulesAsync_MixedRules_ReturnsOnlyActive()
    {
        // Arrange
        await _service.CreateRuleAsync(CreateTestRule("Active1", TimeDiscountType.Percentage, 10));
        var inactive = await _service.CreateRuleAsync(CreateTestRule("Inactive", TimeDiscountType.Percentage, 20));
        await _service.CreateRuleAsync(CreateTestRule("Active2", TimeDiscountType.Percentage, 30));
        await _service.SetRuleActiveAsync(inactive.Id, false);

        // Act
        var result = await _service.GetActiveRulesAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(r => r.IsActive);
    }

    [Fact]
    public async Task SetRuleActiveAsync_EnableDisable_ChangesState()
    {
        // Arrange
        var rule = await _service.CreateRuleAsync(CreateTestRule("Test", TimeDiscountType.Percentage, 20));

        // Act
        await _service.SetRuleActiveAsync(rule.Id, false);
        var disabledRule = await _service.GetRuleAsync(rule.Id);

        await _service.SetRuleActiveAsync(rule.Id, true);
        var enabledRule = await _service.GetRuleAsync(rule.Id);

        // Assert
        disabledRule!.IsActive.Should().BeFalse();
        enabledRule!.IsActive.Should().BeTrue();
    }

    #endregion

    #region Price Calculation Tests

    [Fact]
    public async Task GetEffectivePriceAtTimeAsync_NoActiveRule_ReturnsRegularPrice()
    {
        // Arrange
        var rule = await _service.CreateRuleAsync(CreateTestRule("Morning", TimeDiscountType.Percentage, 20));
        rule.StartTime = new TimeOnly(9, 0);
        rule.EndTime = new TimeOnly(12, 0);
        await _service.UpdateRuleAsync(rule);

        var testTime = new DateTime(2024, 1, 15, 15, 0, 0); // 3pm Monday, outside rule time

        // Act
        var result = await _service.GetEffectivePriceAtTimeAsync(1, 1, 1000m, testTime);

        // Assert
        result.EffectivePrice.Should().Be(1000m);
        result.HasTimePricingDiscount.Should().BeFalse();
        result.ActiveRule.Should().BeNull();
    }

    [Fact]
    public async Task GetEffectivePriceAtTimeAsync_ActivePercentageRule_AppliesDiscount()
    {
        // Arrange
        var rule = CreateTestRule("Happy Hour", TimeDiscountType.Percentage, 25);
        rule.StartTime = new TimeOnly(16, 0);
        rule.EndTime = new TimeOnly(19, 0);
        rule.Scope = PricingRuleScope.AllProducts;
        await _service.CreateRuleAsync(rule);

        var testTime = new DateTime(2024, 1, 15, 17, 0, 0); // 5pm Monday, within rule time

        // Act
        var result = await _service.GetEffectivePriceAtTimeAsync(1, 1, 1000m, testTime);

        // Assert
        result.EffectivePrice.Should().Be(750m); // 1000 - 25%
        result.HasTimePricingDiscount.Should().BeTrue();
        result.ActiveRule.Should().NotBeNull();
        result.DiscountAmount.Should().Be(250m);
        result.DiscountPercent.Should().Be(25m);
    }

    [Fact]
    public async Task GetEffectivePriceAtTimeAsync_ActiveFixedPriceRule_ReturnsFixedPrice()
    {
        // Arrange
        var rule = CreateTestRule("Fixed Deal", TimeDiscountType.FixedPrice, 0);
        rule.FixedPrice = 500m;
        rule.StartTime = new TimeOnly(12, 0);
        rule.EndTime = new TimeOnly(15, 0);
        rule.Scope = PricingRuleScope.AllProducts;
        await _service.CreateRuleAsync(rule);

        var testTime = new DateTime(2024, 1, 15, 13, 0, 0); // 1pm Monday

        // Act
        var result = await _service.GetEffectivePriceAtTimeAsync(1, 1, 1000m, testTime);

        // Assert
        result.EffectivePrice.Should().Be(500m);
        result.HasTimePricingDiscount.Should().BeTrue();
    }

    [Fact]
    public async Task GetEffectivePriceAtTimeAsync_ActiveAmountOffRule_SubtractsAmount()
    {
        // Arrange
        var rule = CreateTestRule("Amount Off", TimeDiscountType.AmountOff, 150);
        rule.StartTime = new TimeOnly(10, 0);
        rule.EndTime = new TimeOnly(14, 0);
        rule.Scope = PricingRuleScope.AllProducts;
        await _service.CreateRuleAsync(rule);

        var testTime = new DateTime(2024, 1, 15, 12, 0, 0); // noon Monday

        // Act
        var result = await _service.GetEffectivePriceAtTimeAsync(1, 1, 1000m, testTime);

        // Assert
        result.EffectivePrice.Should().Be(850m); // 1000 - 150
    }

    [Fact]
    public async Task GetEffectivePriceAtTimeAsync_SpecificProductRule_OnlyAppliesToMatchingProduct()
    {
        // Arrange
        var rule = CreateTestRule("Product Special", TimeDiscountType.Percentage, 20);
        rule.StartTime = new TimeOnly(10, 0);
        rule.EndTime = new TimeOnly(22, 0);
        rule.Scope = PricingRuleScope.SpecificProducts;
        rule.ProductIds = new List<int> { 5, 10, 15 };
        await _service.CreateRuleAsync(rule);

        var testTime = new DateTime(2024, 1, 15, 12, 0, 0);

        // Act
        var matchingResult = await _service.GetEffectivePriceAtTimeAsync(10, 1, 1000m, testTime);
        var nonMatchingResult = await _service.GetEffectivePriceAtTimeAsync(20, 1, 1000m, testTime);

        // Assert
        matchingResult.HasTimePricingDiscount.Should().BeTrue();
        matchingResult.EffectivePrice.Should().Be(800m);

        nonMatchingResult.HasTimePricingDiscount.Should().BeFalse();
        nonMatchingResult.EffectivePrice.Should().Be(1000m);
    }

    [Fact]
    public async Task GetEffectivePriceAtTimeAsync_CategoryRule_OnlyAppliesToMatchingCategory()
    {
        // Arrange
        var rule = CreateTestRule("Category Special", TimeDiscountType.Percentage, 15);
        rule.StartTime = new TimeOnly(10, 0);
        rule.EndTime = new TimeOnly(22, 0);
        rule.Scope = PricingRuleScope.Categories;
        rule.CategoryIds = new List<int> { 3, 4 };
        await _service.CreateRuleAsync(rule);

        var testTime = new DateTime(2024, 1, 15, 12, 0, 0);

        // Act
        var matchingResult = await _service.GetEffectivePriceAtTimeAsync(1, 3, 1000m, testTime);
        var nonMatchingResult = await _service.GetEffectivePriceAtTimeAsync(1, 1, 1000m, testTime);

        // Assert
        matchingResult.HasTimePricingDiscount.Should().BeTrue();
        matchingResult.EffectivePrice.Should().Be(850m);

        nonMatchingResult.HasTimePricingDiscount.Should().BeFalse();
        nonMatchingResult.EffectivePrice.Should().Be(1000m);
    }

    [Fact]
    public async Task GetEffectivePriceAtTimeAsync_MultipleRules_HighestPriorityWins()
    {
        // Arrange
        var lowPriority = CreateTestRule("Low Priority", TimeDiscountType.Percentage, 10);
        lowPriority.Priority = 1;
        lowPriority.Scope = PricingRuleScope.AllProducts;
        await _service.CreateRuleAsync(lowPriority);

        var highPriority = CreateTestRule("High Priority", TimeDiscountType.Percentage, 30);
        highPriority.Priority = 10;
        highPriority.Scope = PricingRuleScope.AllProducts;
        await _service.CreateRuleAsync(highPriority);

        var testTime = new DateTime(2024, 1, 15, 17, 0, 0);

        // Act
        var result = await _service.GetEffectivePriceAtTimeAsync(1, 1, 1000m, testTime);

        // Assert
        result.EffectivePrice.Should().Be(700m); // High priority 30% discount
        result.ActiveRule!.Name.Should().Be("High Priority");
    }

    [Fact]
    public async Task GetEffectivePriceAtTimeAsync_MidnightCrossing_WorksCorrectly()
    {
        // Arrange
        var rule = CreateTestRule("Late Night", TimeDiscountType.Percentage, 25);
        rule.StartTime = new TimeOnly(22, 0); // 10pm
        rule.EndTime = new TimeOnly(2, 0);    // 2am next day
        rule.ApplicableDays = new List<DayOfWeek> { DayOfWeek.Friday, DayOfWeek.Saturday };
        rule.Scope = PricingRuleScope.AllProducts;
        await _service.CreateRuleAsync(rule);

        var beforeMidnight = new DateTime(2024, 1, 12, 23, 0, 0); // 11pm Friday
        var afterMidnight = new DateTime(2024, 1, 13, 1, 0, 0);   // 1am Saturday

        // Act
        var beforeResult = await _service.GetEffectivePriceAtTimeAsync(1, 1, 1000m, beforeMidnight);
        var afterResult = await _service.GetEffectivePriceAtTimeAsync(1, 1, 1000m, afterMidnight);

        // Assert
        beforeResult.HasTimePricingDiscount.Should().BeTrue();
        afterResult.HasTimePricingDiscount.Should().BeTrue();
    }

    [Fact]
    public async Task GetEffectivePricesAsync_MultipleProducts_CalculatesAll()
    {
        // Arrange
        var rule = CreateTestRule("Bulk", TimeDiscountType.Percentage, 20);
        rule.Scope = PricingRuleScope.AllProducts;
        await _service.CreateRuleAsync(rule);

        var products = new List<(int ProductId, int CategoryId, decimal RegularPrice, string ProductName)>
        {
            (1, 1, 500m, "Product A"),
            (2, 1, 750m, "Product B"),
            (3, 2, 1000m, "Product C")
        };

        // Act
        var results = await _service.GetEffectivePricesAsync(products);

        // Assert
        results.Should().HaveCount(3);
        results[0].EffectivePrice.Should().Be(400m);
        results[1].EffectivePrice.Should().Be(600m);
        results[2].EffectivePrice.Should().Be(800m);
    }

    [Fact]
    public async Task HasActiveDiscountAsync_WithActiveDiscount_ReturnsTrue()
    {
        // Arrange
        var rule = CreateTestRule("Active", TimeDiscountType.Percentage, 20);
        rule.Scope = PricingRuleScope.AllProducts;
        // Use current time window
        var now = DateTime.Now;
        rule.StartTime = TimeOnly.FromDateTime(now.AddHours(-1));
        rule.EndTime = TimeOnly.FromDateTime(now.AddHours(1));
        rule.ApplicableDays = new List<DayOfWeek> { now.DayOfWeek };
        await _service.CreateRuleAsync(rule);

        // Act
        var result = await _service.HasActiveDiscountAsync(1, 1);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Active Promotions Tests

    [Fact]
    public async Task GetActivePromotionsAsync_NoActiveRules_ReturnsEmptyStatus()
    {
        // Arrange - no rules created

        // Act
        var result = await _service.GetActivePromotionsAsync();

        // Assert
        result.ActiveRules.Should().BeEmpty();
        result.IsHappyHourActive.Should().BeFalse();
        result.PrimaryPromotionName.Should().BeNull();
    }

    [Fact]
    public async Task GetActivePromotionsAsync_WithActiveRules_ReturnsCorrectStatus()
    {
        // Arrange
        var now = DateTime.Now;
        var rule = CreateTestRule("Happy Hour", TimeDiscountType.Percentage, 25);
        rule.StartTime = TimeOnly.FromDateTime(now.AddHours(-1));
        rule.EndTime = TimeOnly.FromDateTime(now.AddHours(1));
        rule.ApplicableDays = new List<DayOfWeek> { now.DayOfWeek };
        rule.Scope = PricingRuleScope.AllProducts;
        await _service.CreateRuleAsync(rule);

        // Act
        var result = await _service.GetActivePromotionsAsync();

        // Assert
        result.ActiveRules.Should().HaveCount(1);
        result.IsHappyHourActive.Should().BeTrue();
        result.PrimaryPromotionName.Should().Be("Happy Hour");
        result.EndsAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCurrentlyActiveRulesAsync_ReturnsOnlyCurrentlyActive()
    {
        // Arrange
        var now = DateTime.Now;

        var activeRule = CreateTestRule("Active Now", TimeDiscountType.Percentage, 20);
        activeRule.StartTime = TimeOnly.FromDateTime(now.AddHours(-1));
        activeRule.EndTime = TimeOnly.FromDateTime(now.AddHours(1));
        activeRule.ApplicableDays = new List<DayOfWeek> { now.DayOfWeek };
        await _service.CreateRuleAsync(activeRule);

        var inactiveRule = CreateTestRule("Not Active", TimeDiscountType.Percentage, 20);
        inactiveRule.StartTime = TimeOnly.FromDateTime(now.AddHours(2));
        inactiveRule.EndTime = TimeOnly.FromDateTime(now.AddHours(4));
        inactiveRule.ApplicableDays = new List<DayOfWeek> { now.DayOfWeek };
        await _service.CreateRuleAsync(inactiveRule);

        // Act
        var result = await _service.GetCurrentlyActiveRulesAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Active Now");
    }

    [Fact]
    public async Task GetRulesForProductAsync_ReturnsApplicableRules()
    {
        // Arrange
        var productRule = CreateTestRule("Product Rule", TimeDiscountType.Percentage, 10);
        productRule.Scope = PricingRuleScope.SpecificProducts;
        productRule.ProductIds = new List<int> { 5 };
        await _service.CreateRuleAsync(productRule);

        var categoryRule = CreateTestRule("Category Rule", TimeDiscountType.Percentage, 15);
        categoryRule.Scope = PricingRuleScope.Categories;
        categoryRule.CategoryIds = new List<int> { 2 };
        await _service.CreateRuleAsync(categoryRule);

        var otherRule = CreateTestRule("Other Rule", TimeDiscountType.Percentage, 20);
        otherRule.Scope = PricingRuleScope.SpecificProducts;
        otherRule.ProductIds = new List<int> { 10 };
        await _service.CreateRuleAsync(otherRule);

        // Act
        var rulesForProduct5 = await _service.GetRulesForProductAsync(5, 1);
        var rulesForCategory2Product = await _service.GetRulesForProductAsync(1, 2);

        // Assert
        rulesForProduct5.Should().HaveCount(1);
        rulesForProduct5[0].Name.Should().Be("Product Rule");

        rulesForCategory2Product.Should().HaveCount(1);
        rulesForCategory2Product[0].Name.Should().Be("Category Rule");
    }

    [Fact]
    public async Task GetAffectedProductIdsAsync_ReturnsProductsFromActiveRules()
    {
        // Arrange
        var now = DateTime.Now;
        var rule = CreateTestRule("Specific", TimeDiscountType.Percentage, 20);
        rule.StartTime = TimeOnly.FromDateTime(now.AddHours(-1));
        rule.EndTime = TimeOnly.FromDateTime(now.AddHours(1));
        rule.ApplicableDays = new List<DayOfWeek> { now.DayOfWeek };
        rule.Scope = PricingRuleScope.SpecificProducts;
        rule.ProductIds = new List<int> { 1, 2, 3 };
        await _service.CreateRuleAsync(rule);

        // Act
        var result = await _service.GetAffectedProductIdsAsync();

        // Assert
        result.Should().Contain(new[] { 1, 2, 3 });
    }

    [Fact]
    public async Task GetAffectedCategoryIdsAsync_ReturnsCategoriesFromActiveRules()
    {
        // Arrange
        var now = DateTime.Now;
        var rule = CreateTestRule("Category", TimeDiscountType.Percentage, 20);
        rule.StartTime = TimeOnly.FromDateTime(now.AddHours(-1));
        rule.EndTime = TimeOnly.FromDateTime(now.AddHours(1));
        rule.ApplicableDays = new List<DayOfWeek> { now.DayOfWeek };
        rule.Scope = PricingRuleScope.Categories;
        rule.CategoryIds = new List<int> { 3, 4, 5 };
        await _service.CreateRuleAsync(rule);

        // Act
        var result = await _service.GetAffectedCategoryIdsAsync();

        // Assert
        result.Should().Contain(new[] { 3, 4, 5 });
    }

    #endregion

    #region Order Integration Tests

    [Fact]
    public async Task ApplyTimePricingToOrderAsync_WithActiveRules_AppliesDiscounts()
    {
        // Arrange
        var now = DateTime.Now;
        var rule = CreateTestRule("Order Discount", TimeDiscountType.Percentage, 20);
        rule.StartTime = TimeOnly.FromDateTime(now.AddHours(-1));
        rule.EndTime = TimeOnly.FromDateTime(now.AddHours(1));
        rule.ApplicableDays = new List<DayOfWeek> { now.DayOfWeek };
        rule.Scope = PricingRuleScope.AllProducts;
        await _service.CreateRuleAsync(rule);

        var items = new List<(int ProductId, int CategoryId, string ProductName, decimal RegularPrice, int Quantity)>
        {
            (1, 1, "Item A", 500m, 2),
            (2, 1, "Item B", 750m, 1)
        };

        // Act
        var result = await _service.ApplyTimePricingToOrderAsync(items);

        // Assert
        result.Should().HaveCount(2);
        result[0].DiscountedPrice.Should().Be(400m);
        result[0].LineTotal.Should().Be(800m); // 400 * 2
        result[0].AppliedRuleId.Should().NotBeNull();

        result[1].DiscountedPrice.Should().Be(600m);
        result[1].LineTotal.Should().Be(600m);
    }

    [Fact]
    public async Task RecordTimePricingDiscountsAsync_RecordsForAnalytics()
    {
        // Arrange
        var rule = CreateTestRule("Test", TimeDiscountType.Percentage, 20);
        rule.Scope = PricingRuleScope.AllProducts;
        var createdRule = await _service.CreateRuleAsync(rule);

        var items = new List<TimePricedOrderItem>
        {
            new()
            {
                ProductId = 1,
                ProductName = "Test Item",
                Quantity = 2,
                RegularPrice = 500m,
                DiscountedPrice = 400m,
                AppliedRuleId = createdRule.Id,
                PromotionName = "Test"
            }
        };

        // Act
        await _service.RecordTimePricingDiscountsAsync(1001, items);

        // Assert
        var analytics = await _service.GetTodayAnalyticsAsync();
        analytics.TotalDiscountAmount.Should().Be(200m); // (500-400) * 2
    }

    #endregion

    #region Analytics Tests

    [Fact]
    public async Task GetAnalyticsAsync_WithRecordedDiscounts_ReturnsCorrectData()
    {
        // Arrange
        var rule = await _service.CreateRuleAsync(CreateTestRule("Test", TimeDiscountType.Percentage, 20));
        rule.Scope = PricingRuleScope.AllProducts;

        var items = new List<TimePricedOrderItem>
        {
            new()
            {
                ProductId = 1,
                ProductName = "Item A",
                Quantity = 2,
                RegularPrice = 1000m,
                DiscountedPrice = 800m,
                AppliedRuleId = rule.Id,
                PromotionName = "Test"
            },
            new()
            {
                ProductId = 2,
                ProductName = "Item B",
                Quantity = 1,
                RegularPrice = 500m,
                DiscountedPrice = 400m,
                AppliedRuleId = rule.Id,
                PromotionName = "Test"
            }
        };

        await _service.RecordTimePricingDiscountsAsync(1001, items);

        // Act
        var analytics = await _service.GetAnalyticsAsync(DateTime.Today, DateTime.Today.AddDays(1));

        // Assert
        analytics.PromotionPeriodSales.Should().Be(2000m); // (800*2) + 400
        analytics.TotalDiscountAmount.Should().Be(500m); // (200*2) + 100
        analytics.PromotionTransactionCount.Should().Be(1);
        analytics.PromotionBreakdown.Should().HaveCount(1);
        analytics.PopularItems.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetRulePerformanceAsync_ReturnsCorrectPerformanceData()
    {
        // Arrange
        var rule = await _service.CreateRuleAsync(CreateTestRule("Performance Test", TimeDiscountType.Percentage, 15));

        var items = new List<TimePricedOrderItem>
        {
            new()
            {
                ProductId = 1,
                ProductName = "Test",
                Quantity = 5,
                RegularPrice = 1000m,
                DiscountedPrice = 850m,
                AppliedRuleId = rule.Id,
                PromotionName = rule.Name
            }
        };

        await _service.RecordTimePricingDiscountsAsync(1001, items);
        await _service.RecordTimePricingDiscountsAsync(1002, items);

        // Act
        var performance = await _service.GetRulePerformanceAsync(rule.Id, DateTime.Today, DateTime.Today.AddDays(1));

        // Assert
        performance.RuleId.Should().Be(rule.Id);
        performance.PromotionName.Should().Be("Performance Test");
        performance.OrderCount.Should().Be(2);
        performance.ItemsSold.Should().Be(10);
        performance.TotalSales.Should().Be(8500m); // 850 * 5 * 2
        performance.TotalDiscount.Should().Be(1500m); // 150 * 5 * 2
    }

    #endregion

    #region Time Management Tests

    [Fact]
    public async Task GetTimeUntilNextPromotionAsync_NoUpcoming_ReturnsNull()
    {
        // Arrange - no rules

        // Act
        var result = await _service.GetTimeUntilNextPromotionAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTimeUntilPromotionEndsAsync_NoActive_ReturnsNull()
    {
        // Arrange - no active rules

        // Act
        var result = await _service.GetTimeUntilPromotionEndsAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTimeUntilPromotionEndsAsync_WithActive_ReturnsTimeSpan()
    {
        // Arrange
        var now = DateTime.Now;
        var rule = CreateTestRule("Ending Soon", TimeDiscountType.Percentage, 20);
        rule.StartTime = TimeOnly.FromDateTime(now.AddHours(-1));
        rule.EndTime = TimeOnly.FromDateTime(now.AddMinutes(30));
        rule.ApplicableDays = new List<DayOfWeek> { now.DayOfWeek };
        rule.Scope = PricingRuleScope.AllProducts;
        await _service.CreateRuleAsync(rule);

        // Act
        var result = await _service.GetTimeUntilPromotionEndsAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Value.TotalMinutes.Should().BeInRange(25, 35);
    }

    [Fact]
    public async Task CheckPromotionChangesAsync_RaisesEventOnChange()
    {
        // Arrange
        var eventRaised = false;
        _service.PromotionActivationChanged += (sender, args) => { eventRaised = true; };

        var now = DateTime.Now;
        var rule = CreateTestRule("Event Test", TimeDiscountType.Percentage, 20);
        rule.StartTime = TimeOnly.FromDateTime(now.AddHours(-1));
        rule.EndTime = TimeOnly.FromDateTime(now.AddHours(1));
        rule.ApplicableDays = new List<DayOfWeek> { now.DayOfWeek };
        rule.Scope = PricingRuleScope.AllProducts;
        await _service.CreateRuleAsync(rule);

        // Act
        await _service.CheckPromotionChangesAsync();

        // Assert
        eventRaised.Should().BeTrue();
    }

    #endregion

    #region Sample Data Tests

    [Fact]
    public async Task CreateSampleRulesAsync_CreatesFiveRules()
    {
        // Act
        await _service.CreateSampleRulesAsync();

        // Assert
        var rules = await _service.GetAllRulesAsync();
        rules.Should().HaveCount(5);
        rules.Should().Contain(r => r.Name == "Happy Hour");
        rules.Should().Contain(r => r.Name == "Lunch Special");
        rules.Should().Contain(r => r.Name == "Early Bird");
        rules.Should().Contain(r => r.Name == "Late Night");
        rules.Should().Contain(r => r.Name == "Tusker Tuesday");
    }

    #endregion

    #region TimePricingRule Model Tests

    [Fact]
    public void TimePricingRule_IsCurrentlyActive_OutsideTimeWindow_ReturnsFalse()
    {
        // Arrange
        var rule = new TimePricingRule
        {
            IsActive = true,
            StartTime = new TimeOnly(16, 0),
            EndTime = new TimeOnly(19, 0),
            ApplicableDays = new List<DayOfWeek> { DayOfWeek.Monday }
        };

        var testTime = new DateTime(2024, 1, 15, 12, 0, 0); // Monday noon

        // Act
        var result = rule.IsCurrentlyActive(testTime);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TimePricingRule_IsCurrentlyActive_WrongDay_ReturnsFalse()
    {
        // Arrange
        var rule = new TimePricingRule
        {
            IsActive = true,
            StartTime = new TimeOnly(16, 0),
            EndTime = new TimeOnly(19, 0),
            ApplicableDays = new List<DayOfWeek> { DayOfWeek.Monday }
        };

        var testTime = new DateTime(2024, 1, 16, 17, 0, 0); // Tuesday 5pm

        // Act
        var result = rule.IsCurrentlyActive(testTime);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TimePricingRule_IsCurrentlyActive_Disabled_ReturnsFalse()
    {
        // Arrange
        var rule = new TimePricingRule
        {
            IsActive = false,
            StartTime = new TimeOnly(16, 0),
            EndTime = new TimeOnly(19, 0),
            ApplicableDays = new List<DayOfWeek> { DayOfWeek.Monday }
        };

        var testTime = new DateTime(2024, 1, 15, 17, 0, 0); // Monday 5pm

        // Act
        var result = rule.IsCurrentlyActive(testTime);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TimePricingRule_IsCurrentlyActive_OutsideValidDates_ReturnsFalse()
    {
        // Arrange
        var rule = new TimePricingRule
        {
            IsActive = true,
            StartTime = new TimeOnly(16, 0),
            EndTime = new TimeOnly(19, 0),
            ApplicableDays = new List<DayOfWeek> { DayOfWeek.Monday },
            ValidFrom = new DateOnly(2024, 2, 1),
            ValidTo = new DateOnly(2024, 2, 28)
        };

        var testTime = new DateTime(2024, 1, 15, 17, 0, 0); // January, before valid dates

        // Act
        var result = rule.IsCurrentlyActive(testTime);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TimePricingRule_CalculateDiscountedPrice_Percentage()
    {
        // Arrange
        var rule = new TimePricingRule
        {
            DiscountType = TimeDiscountType.Percentage,
            DiscountValue = 20
        };

        // Act
        var result = rule.CalculateDiscountedPrice(1000m);

        // Assert
        result.Should().Be(800m);
    }

    [Fact]
    public void TimePricingRule_CalculateDiscountedPrice_FixedPrice()
    {
        // Arrange
        var rule = new TimePricingRule
        {
            DiscountType = TimeDiscountType.FixedPrice,
            FixedPrice = 599m
        };

        // Act
        var result = rule.CalculateDiscountedPrice(1000m);

        // Assert
        result.Should().Be(599m);
    }

    [Fact]
    public void TimePricingRule_CalculateDiscountedPrice_AmountOff()
    {
        // Arrange
        var rule = new TimePricingRule
        {
            DiscountType = TimeDiscountType.AmountOff,
            DiscountValue = 150
        };

        // Act
        var result = rule.CalculateDiscountedPrice(1000m);

        // Assert
        result.Should().Be(850m);
    }

    [Fact]
    public void TimePricingRule_CalculateDiscountedPrice_AmountOff_DoesNotGoNegative()
    {
        // Arrange
        var rule = new TimePricingRule
        {
            DiscountType = TimeDiscountType.AmountOff,
            DiscountValue = 1500 // More than price
        };

        // Act
        var result = rule.CalculateDiscountedPrice(1000m);

        // Assert
        result.Should().Be(0m);
    }

    [Fact]
    public void TimePricingRule_GetDiscountDisplay_FormatsCorrectly()
    {
        // Arrange
        var percentageRule = new TimePricingRule { DiscountType = TimeDiscountType.Percentage, DiscountValue = 25 };
        var fixedRule = new TimePricingRule { DiscountType = TimeDiscountType.FixedPrice, FixedPrice = 500 };
        var amountRule = new TimePricingRule { DiscountType = TimeDiscountType.AmountOff, DiscountValue = 100 };
        var bogoRule = new TimePricingRule { DiscountType = TimeDiscountType.BuyOneGetOne };

        // Act & Assert
        percentageRule.GetDiscountDisplay().Should().Be("-25%");
        fixedRule.GetDiscountDisplay().Should().Be("Only 500");
        amountRule.GetDiscountDisplay().Should().Be("-100");
        bogoRule.GetDiscountDisplay().Should().Be("BOGO");
    }

    #endregion

    #region Helper Methods

    private TimePricingRule CreateTestRule(string name, TimeDiscountType discountType, decimal discountValue)
    {
        return new TimePricingRule
        {
            Name = name,
            Description = $"Test rule: {name}",
            DisplayText = name,
            StartTime = new TimeOnly(16, 0),
            EndTime = new TimeOnly(19, 0),
            ApplicableDays = new List<DayOfWeek>
            {
                DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                DayOfWeek.Thursday, DayOfWeek.Friday
            },
            DiscountType = discountType,
            DiscountValue = discountValue,
            FixedPrice = discountType == TimeDiscountType.FixedPrice ? discountValue : null,
            Priority = 5,
            CanStackWithOtherDiscounts = false,
            IsActive = true,
            Scope = PricingRuleScope.SpecificProducts,
            ProductIds = new List<int> { 1, 2, 3 },
            BadgeColor = "#F59E0B"
        };
    }

    #endregion
}
