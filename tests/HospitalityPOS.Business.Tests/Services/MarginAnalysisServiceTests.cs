using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Models.Analytics;
using HospitalityPOS.Infrastructure.Data;
using HospitalityPOS.Infrastructure.Services;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for <see cref="MarginAnalysisService"/>.
/// </summary>
public class MarginAnalysisServiceTests : IDisposable
{
    private readonly POSDbContext _context;
    private readonly Mock<ILogger> _loggerMock;
    private readonly MarginAnalysisService _service;

    public MarginAnalysisServiceTests()
    {
        var options = new DbContextOptionsBuilder<POSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new POSDbContext(options);
        _loggerMock = new Mock<ILogger>();
        _service = new MarginAnalysisService(_context, _loggerMock.Object);

        SeedTestData();
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private void SeedTestData()
    {
        // Create categories
        var category1 = new Category { Id = 1, Name = "Beverages", IsActive = true };
        var category2 = new Category { Id = 2, Name = "Snacks", IsActive = true };
        _context.Categories.AddRange(category1, category2);

        // Create products with varying margins
        var products = new[]
        {
            new Product { Id = 1, Code = "BEV001", Name = "Premium Coffee", CategoryId = 1, SellingPrice = 500, CostPrice = 200, IsActive = true }, // 60% margin
            new Product { Id = 2, Code = "BEV002", Name = "Fresh Juice", CategoryId = 1, SellingPrice = 300, CostPrice = 150, IsActive = true },    // 50% margin
            new Product { Id = 3, Code = "BEV003", Name = "Bottled Water", CategoryId = 1, SellingPrice = 100, CostPrice = 90, IsActive = true },   // 10% margin (low)
            new Product { Id = 4, Code = "SNK001", Name = "Chips", CategoryId = 2, SellingPrice = 150, CostPrice = 100, IsActive = true },          // 33% margin
            new Product { Id = 5, Code = "SNK002", Name = "Biscuits", CategoryId = 2, SellingPrice = 80, CostPrice = 70, IsActive = true },         // 12.5% margin (low)
            new Product { Id = 6, Code = "MIS001", Name = "No Cost Product", CategoryId = null, SellingPrice = 200, CostPrice = null, IsActive = true } // No cost price
        };
        _context.Products.AddRange(products);

        // Create receipts with sales data
        var receipt1 = new Receipt
        {
            Id = 1,
            ReceiptNumber = "RCP-001",
            TotalAmount = 1500,
            IsVoided = false,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };
        var receipt2 = new Receipt
        {
            Id = 2,
            ReceiptNumber = "RCP-002",
            TotalAmount = 800,
            IsVoided = false,
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        };
        var receipt3 = new Receipt
        {
            Id = 3,
            ReceiptNumber = "RCP-003",
            TotalAmount = 500,
            IsVoided = true, // Voided - should be excluded
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };
        _context.Receipts.AddRange(receipt1, receipt2, receipt3);

        // Create receipt items
        var receiptItems = new[]
        {
            new ReceiptItem { Id = 1, ReceiptId = 1, ProductId = 1, ProductName = "Premium Coffee", Quantity = 2, UnitPrice = 500, TotalAmount = 1000 },
            new ReceiptItem { Id = 2, ReceiptId = 1, ProductId = 2, ProductName = "Fresh Juice", Quantity = 1, UnitPrice = 300, TotalAmount = 300 },
            new ReceiptItem { Id = 3, ReceiptId = 1, ProductId = 3, ProductName = "Bottled Water", Quantity = 2, UnitPrice = 100, TotalAmount = 200 },
            new ReceiptItem { Id = 4, ReceiptId = 2, ProductId = 4, ProductName = "Chips", Quantity = 3, UnitPrice = 150, TotalAmount = 450 },
            new ReceiptItem { Id = 5, ReceiptId = 2, ProductId = 5, ProductName = "Biscuits", Quantity = 5, UnitPrice = 80, TotalAmount = 400 },
            new ReceiptItem { Id = 6, ReceiptId = 3, ProductId = 1, ProductName = "Premium Coffee", Quantity = 1, UnitPrice = 500, TotalAmount = 500 } // Voided
        };
        _context.ReceiptItems.AddRange(receiptItems);

        // Create GRN data for cost history
        var grn1 = new GoodsReceivedNote
        {
            Id = 1,
            GRNNumber = "GRN-001",
            ReceivedDate = DateTime.UtcNow.AddMonths(-2),
            TotalAmount = 2000
        };
        var grn2 = new GoodsReceivedNote
        {
            Id = 2,
            GRNNumber = "GRN-002",
            ReceivedDate = DateTime.UtcNow.AddDays(-10),
            TotalAmount = 2500
        };
        _context.Set<GoodsReceivedNote>().AddRange(grn1, grn2);

        var grnItems = new[]
        {
            new GRNItem { Id = 1, GoodsReceivedNoteId = 1, ProductId = 1, ReceivedQuantity = 50, UnitCost = 180, TotalCost = 9000 }, // Old cost
            new GRNItem { Id = 2, GoodsReceivedNoteId = 2, ProductId = 1, ReceivedQuantity = 30, UnitCost = 200, TotalCost = 6000 }, // New cost (increased)
            new GRNItem { Id = 3, GoodsReceivedNoteId = 1, ProductId = 3, ReceivedQuantity = 100, UnitCost = 85, TotalCost = 8500 },
            new GRNItem { Id = 4, GoodsReceivedNoteId = 2, ProductId = 3, ReceivedQuantity = 100, UnitCost = 90, TotalCost = 9000 }
        };
        _context.Set<GRNItem>().AddRange(grnItems);

        _context.SaveChanges();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new MarginAnalysisService(null!, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new MarginAnalysisService(_context, null!));
    }

    #endregion

    #region Product Margin Tests

    [Fact]
    public async Task GetProductMarginsAsync_ReturnsAllActiveProductsWithCostPrice()
    {
        // Act
        var result = await _service.GetProductMarginsAsync();

        // Assert
        result.Should().HaveCount(5); // Excludes product without cost price
        result.Should().OnlyContain(p => p.CostPrice > 0);
    }

    [Fact]
    public async Task GetProductMarginsAsync_FiltersByCategory()
    {
        // Act
        var result = await _service.GetProductMarginsAsync(categoryId: 1);

        // Assert
        result.Should().HaveCount(3);
        result.Should().OnlyContain(p => p.CategoryId == 1);
    }

    [Fact]
    public async Task GetProductMarginsAsync_CalculatesMarginCorrectly()
    {
        // Act
        var result = await _service.GetProductMarginsAsync();
        var premiumCoffee = result.First(p => p.ProductId == 1);

        // Assert
        premiumCoffee.SellingPrice.Should().Be(500);
        premiumCoffee.CostPrice.Should().Be(200);
        premiumCoffee.Margin.Should().Be(300);
        premiumCoffee.MarginPercent.Should().Be(60);
        premiumCoffee.Health.Should().Be(MarginHealth.Good);
    }

    [Fact]
    public async Task GetProductMarginsAsync_IdentifiesLowMarginProducts()
    {
        // Act
        var result = await _service.GetProductMarginsAsync();
        var bottledWater = result.First(p => p.ProductId == 3);
        var biscuits = result.First(p => p.ProductId == 5);

        // Assert
        bottledWater.MarginPercent.Should().Be(10);
        bottledWater.Health.Should().Be(MarginHealth.Low);
        bottledWater.IsBelowThreshold.Should().BeTrue();

        biscuits.MarginPercent.Should().Be(12.5m);
        biscuits.Health.Should().Be(MarginHealth.Low);
    }

    [Fact]
    public async Task GetProductMarginAsync_ReturnsCorrectProduct()
    {
        // Act
        var result = await _service.GetProductMarginAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.ProductId.Should().Be(1);
        result.ProductName.Should().Be("Premium Coffee");
        result.MarginPercent.Should().Be(60);
    }

    [Fact]
    public async Task GetProductMarginAsync_ReturnsNullForInvalidId()
    {
        // Act
        var result = await _service.GetProductMarginAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetProductMarginsWithSalesAsync_IncludesSalesData()
    {
        // Arrange
        var request = new MarginReportRequest
        {
            StartDate = DateTime.Today.AddDays(-30),
            EndDate = DateTime.Today
        };

        // Act
        var result = await _service.GetProductMarginsWithSalesAsync(request);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().OnlyContain(p => p.UnitsSold > 0 || p.TotalRevenue > 0);

        var premiumCoffee = result.FirstOrDefault(p => p.ProductId == 1);
        premiumCoffee.Should().NotBeNull();
        premiumCoffee!.UnitsSold.Should().Be(2); // From receipt1 only (receipt3 is voided)
    }

    #endregion

    #region Category Margin Tests

    [Fact]
    public async Task GetCategoryMarginsAsync_AggregatesByCategory()
    {
        // Arrange
        var request = new MarginReportRequest
        {
            StartDate = DateTime.Today.AddDays(-30),
            EndDate = DateTime.Today
        };

        // Act
        var result = await _service.GetCategoryMarginsAsync(request);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(c => c.CategoryName == "Beverages");
        result.Should().Contain(c => c.CategoryName == "Snacks");
    }

    [Fact]
    public async Task GetCategoryMarginsAsync_CalculatesTotalProfitCorrectly()
    {
        // Arrange
        var request = new MarginReportRequest
        {
            StartDate = DateTime.Today.AddDays(-30),
            EndDate = DateTime.Today
        };

        // Act
        var result = await _service.GetCategoryMarginsAsync(request);
        var beverages = result.First(c => c.CategoryName == "Beverages");

        // Assert
        beverages.TotalRevenue.Should().BeGreaterThan(0);
        beverages.TotalCost.Should().BeGreaterThan(0);
        beverages.TotalProfit.Should().Be(beverages.TotalRevenue - beverages.TotalCost);
    }

    [Fact]
    public async Task GetCategoryMarginsAsync_AssignsProfitabilityRanks()
    {
        // Arrange
        var request = new MarginReportRequest
        {
            StartDate = DateTime.Today.AddDays(-30),
            EndDate = DateTime.Today
        };

        // Act
        var result = await _service.GetCategoryMarginsAsync(request);

        // Assert
        result.Should().OnlyContain(c => c.ProfitabilityRank > 0);
        result.Select(c => c.ProfitabilityRank).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetTopProfitableCategoriesAsync_ReturnsLimitedResults()
    {
        // Arrange
        var request = new MarginReportRequest
        {
            StartDate = DateTime.Today.AddDays(-30),
            EndDate = DateTime.Today
        };

        // Act
        var result = await _service.GetTopProfitableCategoriesAsync(request, limit: 1);

        // Assert
        result.Should().HaveCount(1);
    }

    #endregion

    #region Gross Profit Tests

    [Fact]
    public async Task GetGrossProfitSummaryAsync_CalculatesCorrectly()
    {
        // Arrange
        var request = new MarginReportRequest
        {
            StartDate = DateTime.Today.AddDays(-30),
            EndDate = DateTime.Today
        };

        // Act
        var result = await _service.GetGrossProfitSummaryAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.TotalRevenue.Should().BeGreaterThan(0);
        result.GrossProfit.Should().Be(result.TotalRevenue - result.TotalCost);
        result.GrossProfitPercent.Should().BeInRange(0, 100);
        result.TransactionCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetGrossProfitSummaryAsync_ExcludesVoidedReceipts()
    {
        // Arrange
        var request = new MarginReportRequest
        {
            StartDate = DateTime.Today.AddDays(-30),
            EndDate = DateTime.Today
        };

        // Act
        var result = await _service.GetGrossProfitSummaryAsync(request);

        // Assert
        // Receipt3 is voided, so transaction count should be 2 not 3
        result.TransactionCount.Should().Be(2);
    }

    [Fact]
    public async Task GetDailyGrossProfitAsync_ReturnsDailyData()
    {
        // Arrange
        var request = new MarginReportRequest
        {
            StartDate = DateTime.Today.AddDays(-30),
            EndDate = DateTime.Today
        };

        // Act
        var result = await _service.GetDailyGrossProfitAsync(request);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().OnlyContain(d => d.Date >= request.StartDate && d.Date <= request.EndDate);
    }

    [Fact]
    public async Task GetMonthlyGrossProfitAsync_ReturnsMonthlyData()
    {
        // Act
        var result = await _service.GetMonthlyGrossProfitAsync(
            DateTime.Today.AddMonths(-6),
            DateTime.Today);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().OnlyContain(m => m.Date.Day == 1); // Monthly data should be first of month
    }

    #endregion

    #region Low Margin Alert Tests

    [Fact]
    public async Task GetLowMarginAlertsAsync_ReturnsProductsBelowThreshold()
    {
        // Act
        var result = await _service.GetLowMarginAlertsAsync(thresholdPercent: 15);

        // Assert
        result.Should().HaveCount(2); // Bottled Water (10%) and Biscuits (12.5%)
        result.Should().OnlyContain(a => a.CurrentMarginPercent < 15);
    }

    [Fact]
    public async Task GetLowMarginAlertsAsync_CalculatesSuggestedPrice()
    {
        // Act
        var result = await _service.GetLowMarginAlertsAsync(thresholdPercent: 20);
        var bottledWater = result.First(a => a.ProductCode == "BEV003");

        // Assert
        bottledWater.ThresholdPercent.Should().Be(20);
        bottledWater.SuggestedPrice.Should().BeGreaterThan(bottledWater.SellingPrice);
        // At 20% margin, price should be: cost / (1 - 0.20) = 90 / 0.80 = 112.5
        bottledWater.SuggestedPrice.Should().BeApproximately(112.5m, 0.01m);
    }

    [Fact]
    public async Task GetLowMarginAlertsAsync_CalculatesAlertSeverity()
    {
        // Act
        var result = await _service.GetLowMarginAlertsAsync(thresholdPercent: 25);

        // Assert
        result.Should().Contain(a => a.Severity == AlertSeverity.Critical ||
                                     a.Severity == AlertSeverity.High ||
                                     a.Severity == AlertSeverity.Medium);
    }

    [Fact]
    public async Task GetLowMarginCountAsync_ReturnsCorrectCount()
    {
        // Act
        var count = await _service.GetLowMarginCountAsync(thresholdPercent: 15);

        // Assert
        count.Should().Be(2);
    }

    #endregion

    #region Margin Trend Tests

    [Fact]
    public async Task GetProductMarginTrendAsync_ReturnsValidTrendData()
    {
        // Act
        var result = await _service.GetProductMarginTrendAsync(1, months: 6);

        // Assert
        result.Should().NotBeNull();
        result!.ProductId.Should().Be(1);
        result.ProductName.Should().Be("Premium Coffee");
        result.CurrentMarginPercent.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetProductMarginTrendAsync_ReturnsNullForInvalidProduct()
    {
        // Act
        var result = await _service.GetProductMarginTrendAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCostPriceHistoryAsync_ReturnsHistoryFromGRNs()
    {
        // Act
        var result = await _service.GetCostPriceHistoryAsync(1);

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeInDescendingOrder(h => h.EffectiveDate);
    }

    [Fact]
    public async Task GetCostPriceHistoryAsync_CalculatesCostChanges()
    {
        // Act
        var result = await _service.GetCostPriceHistoryAsync(1);
        var latestEntry = result.First();

        // Assert
        latestEntry.CostPrice.Should().Be(200);
        latestEntry.PreviousCostPrice.Should().Be(180);
        latestEntry.CostChange.Should().Be(20);
    }

    #endregion

    #region Complete Report Tests

    [Fact]
    public async Task GetMarginAnalyticsReportAsync_ReturnsCompleteReport()
    {
        // Arrange
        var request = new MarginReportRequest
        {
            StartDate = DateTime.Today.AddDays(-30),
            EndDate = DateTime.Today,
            MinimumMarginThreshold = 15
        };

        // Act
        var result = await _service.GetMarginAnalyticsReportAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.GrossProfitSummary.Should().NotBeNull();
        result.CategoryMargins.Should().NotBeEmpty();
        result.ProductMargins.Should().NotBeEmpty();
        result.LowMarginAlerts.Should().NotBeEmpty();
        result.TotalProductsAnalyzed.Should().BeGreaterThan(0);
        result.ProductsWithCostPrice.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetMarginAnalyticsReportAsync_CalculatesCoverageCorrectly()
    {
        // Arrange
        var request = new MarginReportRequest
        {
            StartDate = DateTime.Today.AddDays(-30),
            EndDate = DateTime.Today
        };

        // Act
        var result = await _service.GetMarginAnalyticsReportAsync(request);

        // Assert
        result.CostPriceCoverage.Should().BeInRange(0, 100);
        // 5 out of 6 products have cost price = 83.3%
        result.CostPriceCoverage.Should().BeApproximately(83.3m, 1m);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public async Task GetMinimumMarginThresholdAsync_ReturnsDefaultThreshold()
    {
        // Act
        var result = await _service.GetMinimumMarginThresholdAsync();

        // Assert
        result.Should().Be(15.0m);
    }

    [Fact]
    public async Task SetMinimumMarginThresholdAsync_UpdatesThreshold()
    {
        // Arrange
        var newThreshold = 20.0m;

        // Act
        await _service.SetMinimumMarginThresholdAsync(newThreshold);
        var result = await _service.GetMinimumMarginThresholdAsync();

        // Assert
        result.Should().Be(newThreshold);
    }

    [Fact]
    public async Task SetMinimumMarginThresholdAsync_ThrowsForInvalidThreshold()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _service.SetMinimumMarginThresholdAsync(-5));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _service.SetMinimumMarginThresholdAsync(150));
    }

    #endregion

    #region Margin Health Classification Tests

    [Fact]
    public void MarginHealth_ClassifiesCorrectly()
    {
        // Arrange
        var lowMargin = new ProductMarginDto { SellingPrice = 100, CostPrice = 90 }; // 10%
        var mediumMargin = new ProductMarginDto { SellingPrice = 100, CostPrice = 75 }; // 25%
        var goodMargin = new ProductMarginDto { SellingPrice = 100, CostPrice = 50 }; // 50%

        // Assert
        lowMargin.Health.Should().Be(MarginHealth.Low);
        lowMargin.HealthColor.Should().Be("#EF4444"); // Red

        mediumMargin.Health.Should().Be(MarginHealth.Medium);
        mediumMargin.HealthColor.Should().Be("#F59E0B"); // Yellow

        goodMargin.Health.Should().Be(MarginHealth.Good);
        goodMargin.HealthColor.Should().Be("#22C55E"); // Green
    }

    [Fact]
    public void ProductMarginDto_HandlesZeroSellingPrice()
    {
        // Arrange
        var margin = new ProductMarginDto { SellingPrice = 0, CostPrice = 50 };

        // Assert
        margin.MarginPercent.Should().Be(0);
        margin.Health.Should().Be(MarginHealth.Low);
    }

    #endregion

    #region Alert Severity Tests

    [Fact]
    public void AlertSeverity_ClassifiesCorrectly()
    {
        // Arrange
        var mediumAlert = new LowMarginAlertDto { CurrentMarginPercent = 13, ThresholdPercent = 15 }; // 2% gap
        var highAlert = new LowMarginAlertDto { CurrentMarginPercent = 8, ThresholdPercent = 15 };    // 7% gap
        var criticalAlert = new LowMarginAlertDto { CurrentMarginPercent = 2, ThresholdPercent = 15 }; // 13% gap

        // Assert
        mediumAlert.Severity.Should().Be(AlertSeverity.Medium);
        highAlert.Severity.Should().Be(AlertSeverity.High);
        criticalAlert.Severity.Should().Be(AlertSeverity.Critical);
    }

    #endregion
}
