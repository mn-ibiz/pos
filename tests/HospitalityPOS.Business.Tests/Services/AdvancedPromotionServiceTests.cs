using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;
using HospitalityPOS.Infrastructure.Services;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for AdvancedPromotionService.
/// </summary>
public class AdvancedPromotionServiceTests : IDisposable
{
    private readonly POSDbContext _context;
    private readonly Mock<ILogger<AdvancedPromotionService>> _loggerMock;
    private readonly AdvancedPromotionService _service;

    public AdvancedPromotionServiceTests()
    {
        var options = new DbContextOptionsBuilder<POSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new POSDbContext(options);
        _loggerMock = new Mock<ILogger<AdvancedPromotionService>>();
        _service = new AdvancedPromotionService(_context, _loggerMock.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create categories
        var category = new Category { Id = 1, Name = "Beverages", IsActive = true };
        _context.Categories.Add(category);

        // Create products
        var products = new[]
        {
            new Product { Id = 1, Name = "Cola", SellingPrice = 50, CategoryId = 1, IsActive = true },
            new Product { Id = 2, Name = "Sprite", SellingPrice = 50, CategoryId = 1, IsActive = true },
            new Product { Id = 3, Name = "Water", SellingPrice = 30, CategoryId = 1, IsActive = true },
            new Product { Id = 4, Name = "Juice", SellingPrice = 80, CategoryId = 1, IsActive = true },
        };
        _context.Products.AddRange(products);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region BOGO Tests

    [Fact]
    public async Task CreateBogoPromotionAsync_ShouldCreatePromotion_WithConfiguration()
    {
        // Arrange
        var promotion = new CentralPromotion
        {
            Name = "Buy 2 Get 1 Free Cola",
            PromotionCode = "BOGO-COLA",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            Status = PromotionStatus.Active
        };

        var bogoConfig = new BogoPromotion
        {
            BogoType = BogoType.BuyXGetYFree,
            BuyQuantity = 2,
            GetQuantity = 1,
            DiscountPercentOnGetItems = 100
        };

        // Act
        var result = await _service.CreateBogoPromotionAsync(promotion, bogoConfig, new[] { 1 });

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Type.Should().Be(PromotionType.BOGO);

        var savedBogo = await _context.BogoPromotions.FirstOrDefaultAsync(b => b.PromotionId == result.Id);
        savedBogo.Should().NotBeNull();
        savedBogo!.BuyQuantity.Should().Be(2);
        savedBogo.GetQuantity.Should().Be(1);
    }

    [Fact]
    public async Task CalculateBogoDiscountAsync_ShouldApplyDiscount_WhenQuantityMet()
    {
        // Arrange
        var promotion = new CentralPromotion
        {
            Name = "Buy 2 Get 1 Free",
            PromotionCode = "BOGO-TEST",
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(30),
            Status = PromotionStatus.Active,
            IsActive = true
        };
        await _context.CentralPromotions.AddAsync(promotion);
        await _context.SaveChangesAsync();

        var bogoConfig = new BogoPromotion
        {
            PromotionId = promotion.Id,
            BogoType = BogoType.BuyXGetYFree,
            BuyQuantity = 2,
            GetQuantity = 1,
            DiscountPercentOnGetItems = 100,
            IsActive = true
        };
        await _context.BogoPromotions.AddAsync(bogoConfig);

        await _context.PromotionProducts.AddAsync(new PromotionProduct
        {
            PromotionId = promotion.Id,
            ProductId = 1,
            IsQualifyingProduct = true,
            RequiredQuantity = 2,
            IsActive = true
        });
        await _context.SaveChangesAsync();

        var items = new List<OrderItemInfo>
        {
            new() { ProductId = 1, ProductName = "Cola", UnitPrice = 50, Quantity = 3, CategoryId = 1 }
        };

        // Act
        var result = await _service.CalculateBogoDiscountAsync(items);

        // Assert
        result.HasDiscount.Should().BeTrue();
        result.TotalDiscount.Should().Be(50); // 1 free item at 50
        result.Applications.Should().HaveCount(1);
    }

    [Fact]
    public async Task CalculateBogoDiscountAsync_ShouldNotApplyDiscount_WhenQuantityNotMet()
    {
        // Arrange - create promotion with Buy 3 Get 1
        var promotion = new CentralPromotion
        {
            Name = "Buy 3 Get 1 Free",
            PromotionCode = "BOGO-3",
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(30),
            Status = PromotionStatus.Active,
            IsActive = true
        };
        await _context.CentralPromotions.AddAsync(promotion);
        await _context.SaveChangesAsync();

        var bogoConfig = new BogoPromotion
        {
            PromotionId = promotion.Id,
            BogoType = BogoType.BuyXGetYFree,
            BuyQuantity = 3,
            GetQuantity = 1,
            DiscountPercentOnGetItems = 100,
            IsActive = true
        };
        await _context.BogoPromotions.AddAsync(bogoConfig);

        await _context.PromotionProducts.AddAsync(new PromotionProduct
        {
            PromotionId = promotion.Id,
            ProductId = 1,
            IsQualifyingProduct = true,
            RequiredQuantity = 3,
            IsActive = true
        });
        await _context.SaveChangesAsync();

        var items = new List<OrderItemInfo>
        {
            new() { ProductId = 1, ProductName = "Cola", UnitPrice = 50, Quantity = 2, CategoryId = 1 }
        };

        // Act
        var result = await _service.CalculateBogoDiscountAsync(items);

        // Assert
        result.HasDiscount.Should().BeFalse();
        result.TotalDiscount.Should().Be(0);
    }

    #endregion

    #region Mix & Match Tests

    [Fact]
    public async Task CreateMixMatchPromotionAsync_ShouldCreatePromotion_WithConfiguration()
    {
        // Arrange
        var promotion = new CentralPromotion
        {
            Name = "Any 3 Drinks for KSh 120",
            PromotionCode = "MIX3",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            Status = PromotionStatus.Active
        };

        var mixMatchConfig = new MixMatchPromotion
        {
            MixMatchType = MixMatchType.AnyXForFixedPrice,
            RequiredQuantity = 3,
            FixedPrice = 120
        };

        // Act
        var result = await _service.CreateMixMatchPromotionAsync(promotion, mixMatchConfig);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);

        var savedMixMatch = await _context.MixMatchPromotions.FirstOrDefaultAsync(m => m.PromotionId == result.Id);
        savedMixMatch.Should().NotBeNull();
        savedMixMatch!.RequiredQuantity.Should().Be(3);
        savedMixMatch.FixedPrice.Should().Be(120);
    }

    [Fact]
    public async Task CalculateMixMatchDiscountAsync_ShouldCalculateCorrectDiscount()
    {
        // Arrange
        var promotion = new CentralPromotion
        {
            Name = "Any 3 Drinks for KSh 120",
            PromotionCode = "MIX3-TEST",
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(30),
            Status = PromotionStatus.Active,
            IsActive = true
        };
        await _context.CentralPromotions.AddAsync(promotion);
        await _context.SaveChangesAsync();

        var mixMatchConfig = new MixMatchPromotion
        {
            PromotionId = promotion.Id,
            MixMatchType = MixMatchType.AnyXForFixedPrice,
            RequiredQuantity = 3,
            FixedPrice = 120,
            IsActive = true
        };
        await _context.MixMatchPromotions.AddAsync(mixMatchConfig);
        await _context.SaveChangesAsync();

        var group = new MixMatchGroup
        {
            MixMatchPromotionId = mixMatchConfig.Id,
            GroupName = "Beverages",
            GroupType = "A",
            MinQuantity = 3,
            IsActive = true
        };
        await _context.MixMatchGroups.AddAsync(group);
        await _context.SaveChangesAsync();

        await _context.MixMatchGroupProducts.AddRangeAsync(new[]
        {
            new MixMatchGroupProduct { MixMatchGroupId = group.Id, ProductId = 1, IsActive = true },
            new MixMatchGroupProduct { MixMatchGroupId = group.Id, ProductId = 2, IsActive = true },
            new MixMatchGroupProduct { MixMatchGroupId = group.Id, ProductId = 3, IsActive = true }
        });
        await _context.SaveChangesAsync();

        var items = new List<OrderItemInfo>
        {
            new() { ProductId = 1, ProductName = "Cola", UnitPrice = 50, Quantity = 1, CategoryId = 1 },
            new() { ProductId = 2, ProductName = "Sprite", UnitPrice = 50, Quantity = 1, CategoryId = 1 },
            new() { ProductId = 3, ProductName = "Water", UnitPrice = 30, Quantity = 1, CategoryId = 1 }
        };

        // Act
        var result = await _service.CalculateMixMatchDiscountAsync(items);

        // Assert
        result.HasDiscount.Should().BeTrue();
        // Original: 50 + 50 + 30 = 130, Discounted: 120, Saving: 10
        result.TotalDiscount.Should().Be(10);
    }

    #endregion

    #region Quantity Break Tests

    [Fact]
    public async Task CreateQuantityBreakTiersAsync_ShouldCreateTiers()
    {
        // Arrange
        var promotion = new CentralPromotion
        {
            Name = "Volume Discount",
            PromotionCode = "VOL",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            Status = PromotionStatus.Active
        };
        await _context.CentralPromotions.AddAsync(promotion);
        await _context.SaveChangesAsync();

        var tiers = new[]
        {
            new QuantityBreakTier { MinQuantity = 3, MaxQuantity = 5, DiscountPercent = 5, DisplayLabel = "Buy 3-5, save 5%" },
            new QuantityBreakTier { MinQuantity = 6, MaxQuantity = 9, DiscountPercent = 10, DisplayLabel = "Buy 6-9, save 10%" },
            new QuantityBreakTier { MinQuantity = 10, DiscountPercent = 15, DisplayLabel = "Buy 10+, save 15%" }
        };

        // Act
        await _service.CreateQuantityBreakTiersAsync(promotion.Id, tiers);

        // Assert
        var savedTiers = await _context.QuantityBreakTiers.Where(t => t.PromotionId == promotion.Id).ToListAsync();
        savedTiers.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetApplicableTierAsync_ShouldReturnCorrectTier()
    {
        // Arrange
        var promotion = new CentralPromotion
        {
            Name = "Volume Discount",
            PromotionCode = "VOL-TEST",
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(30),
            Status = PromotionStatus.Active,
            IsActive = true
        };
        await _context.CentralPromotions.AddAsync(promotion);
        await _context.SaveChangesAsync();

        await _context.PromotionProducts.AddAsync(new PromotionProduct
        {
            PromotionId = promotion.Id,
            ProductId = 1,
            IsQualifyingProduct = true,
            IsActive = true
        });

        var tiers = new[]
        {
            new QuantityBreakTier { PromotionId = promotion.Id, MinQuantity = 3, MaxQuantity = 5, DiscountPercent = 5, IsActive = true },
            new QuantityBreakTier { PromotionId = promotion.Id, MinQuantity = 6, MaxQuantity = 9, DiscountPercent = 10, IsActive = true },
            new QuantityBreakTier { PromotionId = promotion.Id, MinQuantity = 10, DiscountPercent = 15, IsActive = true }
        };
        await _context.QuantityBreakTiers.AddRangeAsync(tiers);
        await _context.SaveChangesAsync();

        // Act
        var tier = await _service.GetApplicableTierAsync(1, 7);

        // Assert
        tier.Should().NotBeNull();
        tier!.DiscountPercent.Should().Be(10); // 6-9 tier
    }

    #endregion

    #region Combo Tests

    [Fact]
    public async Task CreateComboPromotionAsync_ShouldCreateCombo()
    {
        // Arrange
        var promotion = new CentralPromotion
        {
            Name = "Breakfast Combo",
            PromotionCode = "COMBO-BFAST",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            Status = PromotionStatus.Active
        };

        var comboConfig = new ComboPromotion
        {
            ComboName = "Breakfast Combo",
            ComboPrice = 200,
            OriginalTotalPrice = 250,
            AllItemsRequired = true
        };

        // Act
        var result = await _service.CreateComboPromotionAsync(promotion, comboConfig);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be(PromotionType.Bundle);

        var savedCombo = await _context.ComboPromotions.FirstOrDefaultAsync(c => c.PromotionId == result.Id);
        savedCombo.Should().NotBeNull();
        savedCombo!.ComboPrice.Should().Be(200);
    }

    [Fact]
    public async Task FindApplicableCombosAsync_ShouldFindMatchingCombos()
    {
        // Arrange
        var promotion = new CentralPromotion
        {
            Name = "Drink Combo",
            PromotionCode = "COMBO-DRINK",
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(30),
            Status = PromotionStatus.Active,
            IsActive = true
        };
        await _context.CentralPromotions.AddAsync(promotion);
        await _context.SaveChangesAsync();

        var comboConfig = new ComboPromotion
        {
            PromotionId = promotion.Id,
            ComboName = "Drink Combo",
            ComboPrice = 100,
            OriginalTotalPrice = 130,
            AllItemsRequired = true,
            IsActive = true
        };
        await _context.ComboPromotions.AddAsync(comboConfig);
        await _context.SaveChangesAsync();

        await _context.ComboItems.AddRangeAsync(new[]
        {
            new ComboItem { ComboPromotionId = comboConfig.Id, ProductId = 1, Quantity = 1, IsRequired = true, IsActive = true },
            new ComboItem { ComboPromotionId = comboConfig.Id, ProductId = 3, Quantity = 1, IsRequired = true, IsActive = true }
        });
        await _context.SaveChangesAsync();

        var items = new List<OrderItemInfo>
        {
            new() { ProductId = 1, ProductName = "Cola", UnitPrice = 50, Quantity = 1, CategoryId = 1 },
            new() { ProductId = 3, ProductName = "Water", UnitPrice = 30, Quantity = 1, CategoryId = 1 }
        };

        // Act
        var result = await _service.FindApplicableCombosAsync(items);

        // Assert
        result.Should().HaveCount(1);
        var combo = result.First();
        combo.IsComplete.Should().BeTrue();
        combo.Savings.Should().Be(30);
    }

    #endregion

    #region Coupon Tests

    [Fact]
    public async Task CreateCouponAsync_ShouldCreateCoupon_WithGeneratedCode()
    {
        // Arrange
        var coupon = new Coupon
        {
            CouponType = CouponType.SingleUse,
            DiscountPercent = 10,
            ValidFrom = DateTime.UtcNow,
            ValidTo = DateTime.UtcNow.AddDays(30)
        };

        // Act
        var result = await _service.CreateCouponAsync(coupon);

        // Assert
        result.Should().NotBeNull();
        result.CouponCode.Should().NotBeNullOrEmpty();
        result.CouponCode.Should().HaveLength(10);
    }

    [Fact]
    public async Task ValidateCouponAsync_ShouldReturnValid_ForValidCoupon()
    {
        // Arrange
        var coupon = new Coupon
        {
            CouponCode = "TEST10OFF",
            CouponType = CouponType.SingleUse,
            DiscountPercent = 10,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(30),
            IsActive = true
        };
        await _context.Coupons.AddAsync(coupon);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ValidateCouponAsync("TEST10OFF", 1000);

        // Assert
        result.IsValid.Should().BeTrue();
        result.CalculatedDiscount.Should().Be(100); // 10% of 1000
    }

    [Fact]
    public async Task ValidateCouponAsync_ShouldReturnInvalid_ForExpiredCoupon()
    {
        // Arrange
        var coupon = new Coupon
        {
            CouponCode = "EXPIRED",
            CouponType = CouponType.SingleUse,
            DiscountPercent = 10,
            ValidFrom = DateTime.UtcNow.AddDays(-30),
            ValidTo = DateTime.UtcNow.AddDays(-1), // Expired
            IsActive = true
        };
        await _context.Coupons.AddAsync(coupon);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ValidateCouponAsync("EXPIRED", 1000);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("expired");
    }

    [Fact]
    public async Task ValidateCouponAsync_ShouldRespectMinimumPurchase()
    {
        // Arrange
        var coupon = new Coupon
        {
            CouponCode = "MIN500",
            CouponType = CouponType.SingleUse,
            DiscountAmount = 50,
            MinimumPurchase = 500,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(30),
            IsActive = true
        };
        await _context.Coupons.AddAsync(coupon);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ValidateCouponAsync("MIN500", 300);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("500");
    }

    [Fact]
    public async Task GenerateCouponBatchAsync_ShouldGenerateSpecifiedCount()
    {
        // Arrange
        var batch = new CouponBatch
        {
            BatchName = "Holiday Promo",
            CodePrefix = "HOLIDAY"
        };

        var template = new Coupon
        {
            CouponType = CouponType.SingleUse,
            DiscountPercent = 15,
            ValidFrom = DateTime.UtcNow,
            ValidTo = DateTime.UtcNow.AddDays(30)
        };

        // Act
        var result = await _service.GenerateCouponBatchAsync(batch, 10, template);

        // Assert
        result.Should().NotBeNull();
        result.TotalCoupons.Should().Be(10);

        var coupons = await _context.Coupons.Where(c => c.BatchId == result.Id).ToListAsync();
        coupons.Should().HaveCount(10);
        coupons.All(c => c.CouponCode.StartsWith("HOLIDAY")).Should().BeTrue();
    }

    [Fact]
    public async Task ApplyCouponAsync_ShouldIncrementUseCount()
    {
        // Arrange
        var coupon = new Coupon
        {
            CouponCode = "APPLY-TEST",
            CouponType = CouponType.MultiUse,
            DiscountAmount = 50,
            MaxUses = 5,
            UseCount = 0,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(30),
            IsActive = true
        };
        await _context.Coupons.AddAsync(coupon);

        // Create a receipt for the redemption
        var receipt = new Receipt { Id = 1, ReceiptNumber = "R001", TotalAmount = 500, IsActive = true };
        await _context.Receipts.AddAsync(receipt);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ApplyCouponAsync("APPLY-TEST", 1, 50, 1);

        // Assert
        result.Should().NotBeNull();
        result.DiscountAmount.Should().Be(50);

        var updatedCoupon = await _context.Coupons.FindAsync(coupon.Id);
        updatedCoupon!.UseCount.Should().Be(1);
    }

    #endregion

    #region Markdown Tests

    [Fact]
    public async Task CreateMarkdownRuleAsync_ShouldCreateRule()
    {
        // Arrange
        var markdown = new AutomaticMarkdown
        {
            ProductId = 1,
            RuleName = "Evening Special",
            TriggerType = MarkdownTriggerType.TimeOfDay,
            TriggerTime = new TimeSpan(18, 0, 0), // 6 PM
            DiscountPercent = 20
        };

        // Act
        var result = await _service.CreateMarkdownRuleAsync(markdown);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetActiveMarkdownAsync_ShouldReturnActiveMarkdown_WhenTimeMatches()
    {
        // Arrange
        var markdown = new AutomaticMarkdown
        {
            ProductId = 1,
            RuleName = "Evening Special",
            TriggerType = MarkdownTriggerType.TimeOfDay,
            TriggerTime = new TimeSpan(18, 0, 0), // 6 PM
            DiscountPercent = 20,
            IsActive = true
        };
        await _context.AutomaticMarkdowns.AddAsync(markdown);
        await _context.SaveChangesAsync();

        var testTime = new DateTime(2025, 1, 1, 19, 0, 0); // 7 PM - after trigger time

        // Act
        var result = await _service.GetActiveMarkdownAsync(1, testTime);

        // Assert
        result.Should().NotBeNull();
        result!.RuleName.Should().Be("Evening Special");
    }

    [Fact]
    public async Task GetActiveMarkdownAsync_ShouldReturnNull_WhenTimeBeforeTrigger()
    {
        // Arrange
        var markdown = new AutomaticMarkdown
        {
            ProductId = 1,
            RuleName = "Evening Special",
            TriggerType = MarkdownTriggerType.TimeOfDay,
            TriggerTime = new TimeSpan(18, 0, 0), // 6 PM
            DiscountPercent = 20,
            IsActive = true
        };
        await _context.AutomaticMarkdowns.AddAsync(markdown);
        await _context.SaveChangesAsync();

        var testTime = new DateTime(2025, 1, 1, 14, 0, 0); // 2 PM - before trigger time

        // Act
        var result = await _service.GetActiveMarkdownAsync(1, testTime);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CalculateMarkdownPriceAsync_ShouldCalculateCorrectPrice()
    {
        // Arrange
        var markdown = new AutomaticMarkdown
        {
            ProductId = 1,
            RuleName = "20% Off",
            TriggerType = MarkdownTriggerType.TimeOfDay,
            TriggerTime = new TimeSpan(18, 0, 0),
            DiscountPercent = 20,
            IsActive = true
        };
        await _context.AutomaticMarkdowns.AddAsync(markdown);
        await _context.SaveChangesAsync();

        var testTime = new DateTime(2025, 1, 1, 19, 0, 0);

        // Act
        var result = await _service.CalculateMarkdownPriceAsync(1, 100, testTime);

        // Assert
        result.HasMarkdown.Should().BeTrue();
        result.OriginalPrice.Should().Be(100);
        result.MarkdownPrice.Should().Be(80);
        result.DiscountAmount.Should().Be(20);
    }

    #endregion

    #region Combined Promotion Calculation Tests

    [Fact]
    public async Task CalculateAllPromotionsAsync_ShouldCombineMultipleDiscounts()
    {
        // Arrange - setup quantity break promotion
        var qbPromotion = new CentralPromotion
        {
            Name = "Volume Discount",
            PromotionCode = "VOL-COMBO",
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(30),
            Status = PromotionStatus.Active,
            IsActive = true
        };
        await _context.CentralPromotions.AddAsync(qbPromotion);
        await _context.SaveChangesAsync();

        await _context.PromotionProducts.AddAsync(new PromotionProduct
        {
            PromotionId = qbPromotion.Id,
            ProductId = 1,
            IsQualifyingProduct = true,
            IsActive = true
        });

        await _context.QuantityBreakTiers.AddAsync(new QuantityBreakTier
        {
            PromotionId = qbPromotion.Id,
            MinQuantity = 3,
            DiscountPercent = 10,
            IsActive = true
        });
        await _context.SaveChangesAsync();

        var items = new List<OrderItemInfo>
        {
            new() { ProductId = 1, ProductName = "Cola", UnitPrice = 50, Quantity = 5, CategoryId = 1 }
        };

        // Act
        var result = await _service.CalculateAllPromotionsAsync(items, 250);

        // Assert
        result.Should().NotBeNull();
        result.OriginalTotal.Should().Be(250);
        result.TotalDiscount.Should().BeGreaterThan(0);
        result.FinalTotal.Should().BeLessThan(250);
    }

    #endregion
}
