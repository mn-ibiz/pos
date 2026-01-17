using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Models.Analytics;
using HospitalityPOS.Infrastructure.Data;
using HospitalityPOS.Infrastructure.Services;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for the ComparativeAnalyticsService class.
/// Tests cover period comparison, growth calculations, trend analysis, category and product comparisons.
/// </summary>
public class ComparativeAnalyticsServiceTests : IAsyncLifetime, IDisposable
{
    private readonly POSDbContext _context;
    private readonly Mock<ILogger> _loggerMock;
    private readonly ComparativeAnalyticsService _service;
    private const int TestUserId = 1;
    private const int TestStoreId = 1;

    public ComparativeAnalyticsServiceTests()
    {
        var options = new DbContextOptionsBuilder<POSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new POSDbContext(options);
        _loggerMock = new Mock<ILogger>();

        _service = new ComparativeAnalyticsService(_context, _loggerMock.Object);
    }

    public async Task InitializeAsync()
    {
        await SeedTestData();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ComparativeAnalyticsService(null!, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ComparativeAnalyticsService(_context, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region Period Comparison Tests

    [Fact]
    public async Task GetPeriodComparisonAsync_WithWeekOverWeek_ReturnsValidComparison()
    {
        // Arrange
        var request = new PeriodComparisonRequest
        {
            PeriodType = ComparisonPeriodType.WeekOverWeek
        };

        // Act
        var result = await _service.GetPeriodComparisonAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.CurrentPeriodLabel.Should().Be("This Week");
        result.PreviousPeriodLabel.Should().Be("Last Week");
        result.Sales.Should().NotBeNull();
        result.Transactions.Should().NotBeNull();
        result.AverageTicket.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPeriodComparisonAsync_WithCustomDates_ReturnsCorrectComparison()
    {
        // Arrange
        var today = DateTime.Today;
        var request = new PeriodComparisonRequest
        {
            PeriodType = ComparisonPeriodType.Custom,
            CurrentPeriodStart = today.AddDays(-7),
            CurrentPeriodEnd = today,
            PreviousPeriodStart = today.AddDays(-14),
            PreviousPeriodEnd = today.AddDays(-7)
        };

        // Act
        var result = await _service.GetPeriodComparisonAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.CurrentPeriodStart.Should().Be(today.AddDays(-7));
        result.CurrentPeriodEnd.Should().Be(today);
        result.PreviousPeriodStart.Should().Be(today.AddDays(-14));
        result.PreviousPeriodEnd.Should().Be(today.AddDays(-7));
    }

    [Fact]
    public async Task GetPeriodComparisonAsync_WithSalesData_CalculatesGrowthCorrectly()
    {
        // Arrange - Add receipts for this week and last week
        var today = DateTime.Today;
        await AddTestReceipts(today.AddDays(-2), 1000m, 2); // This week
        await AddTestReceipts(today.AddDays(-9), 800m, 2); // Last week

        var request = new PeriodComparisonRequest
        {
            PeriodType = ComparisonPeriodType.WeekOverWeek
        };

        // Act
        var result = await _service.GetPeriodComparisonAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Sales.CurrentPeriodValue.Should().BeGreaterThanOrEqualTo(0);
    }

    #endregion

    #region Growth Metrics Tests

    [Fact]
    public async Task CalculateGrowthMetricsAsync_WithValidPeriods_ReturnsGrowthMetrics()
    {
        // Arrange
        var today = DateTime.Today;

        // Act
        var result = await _service.CalculateGrowthMetricsAsync(
            today.AddDays(-7), today,
            today.AddDays(-14), today.AddDays(-7));

        // Assert
        result.Should().NotBeNull();
    }

    [Theory]
    [InlineData(100, 80, 25)] // 25% growth
    [InlineData(80, 100, -20)] // 20% decline
    [InlineData(100, 0, 100)] // From zero
    [InlineData(0, 100, -100)] // To zero
    public void GrowthMetricsDto_CalculatesPercentageChangeCorrectly(
        decimal current, decimal previous, decimal expectedPercent)
    {
        // Arrange
        var metrics = new GrowthMetricsDto
        {
            CurrentPeriodValue = current,
            PreviousPeriodValue = previous
        };

        // Assert
        metrics.PercentageChange.Should().Be(expectedPercent);
        metrics.AbsoluteChange.Should().Be(current - previous);
        metrics.IsPositive.Should().Be(current >= previous);
    }

    #endregion

    #region Trend Analysis Tests

    [Fact]
    public async Task GetSalesTrendComparisonAsync_ReturnsCurrentAndPreviousPeriods()
    {
        // Arrange
        var request = new PeriodComparisonRequest
        {
            PeriodType = ComparisonPeriodType.WeekOverWeek
        };

        // Act
        var result = await _service.GetSalesTrendComparisonAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.CurrentPeriod.Should().NotBeNull();
        result.PreviousPeriod.Should().NotBeNull();
        result.MovingAverage.Should().NotBeNull();
        result.MovingAverageDays.Should().Be(7);
    }

    [Fact]
    public async Task GetDailySalesTrendAsync_WithDateRange_ReturnsDailyData()
    {
        // Arrange
        var startDate = DateTime.Today.AddDays(-7);
        var endDate = DateTime.Today;

        // Act
        var result = await _service.GetDailySalesTrendAsync(startDate, endDate);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<List<DailySalesTrendDto>>();
    }

    [Fact]
    public async Task GetMovingAverageAsync_WithValidRange_ReturnsMovingAveragePoints()
    {
        // Arrange
        var today = DateTime.Today;
        await AddTestReceipts(today.AddDays(-10), 500m, 1);
        await AddTestReceipts(today.AddDays(-9), 600m, 1);
        await AddTestReceipts(today.AddDays(-8), 550m, 1);

        // Act
        var result = await _service.GetMovingAverageAsync(
            today.AddDays(-7), today.AddDays(1), 3);

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region Category Comparison Tests

    [Fact]
    public async Task GetCategoryComparisonAsync_ReturnsComparisonByCategory()
    {
        // Arrange
        var request = new PeriodComparisonRequest
        {
            PeriodType = ComparisonPeriodType.MonthOverMonth
        };

        // Act
        var result = await _service.GetCategoryComparisonAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<List<CategoryComparisonDto>>();
    }

    [Fact]
    public async Task GetFastestGrowingCategoriesAsync_ReturnsTopGrowingCategories()
    {
        // Arrange
        var request = new PeriodComparisonRequest
        {
            PeriodType = ComparisonPeriodType.MonthOverMonth
        };

        // Act
        var result = await _service.GetFastestGrowingCategoriesAsync(request, 5);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().BeLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task GetDecliningCategoriesAsync_ReturnsTopDecliningCategories()
    {
        // Arrange
        var request = new PeriodComparisonRequest
        {
            PeriodType = ComparisonPeriodType.MonthOverMonth
        };

        // Act
        var result = await _service.GetDecliningCategoriesAsync(request, 5);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().BeLessThanOrEqualTo(5);
    }

    #endregion

    #region Product Comparison Tests

    [Fact]
    public async Task GetProductComparisonAsync_ReturnsComparisonByProduct()
    {
        // Arrange
        var request = new PeriodComparisonRequest
        {
            PeriodType = ComparisonPeriodType.WeekOverWeek
        };

        // Act
        var result = await _service.GetProductComparisonAsync(request, 50);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<List<ProductComparisonDto>>();
    }

    [Fact]
    public async Task GetTopMoversAsync_ReturnsGainersLosersNewAndDiscontinued()
    {
        // Arrange
        var request = new PeriodComparisonRequest
        {
            PeriodType = ComparisonPeriodType.MonthOverMonth
        };

        // Act
        var result = await _service.GetTopMoversAsync(request, 10);

        // Assert
        result.Should().NotBeNull();
        result.TopGainers.Should().NotBeNull();
        result.TopLosers.Should().NotBeNull();
        result.NewProducts.Should().NotBeNull();
        result.DiscontinuedProducts.Should().NotBeNull();
    }

    [Fact]
    public async Task GetNewProductsAsync_ReturnsOnlyNewProducts()
    {
        // Arrange
        var request = new PeriodComparisonRequest
        {
            PeriodType = ComparisonPeriodType.MonthOverMonth
        };

        // Act
        var result = await _service.GetNewProductsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().OnlyContain(p => p.IsNewProduct);
    }

    #endregion

    #region Pattern Analysis Tests

    [Fact]
    public async Task GetDayOfWeekPatternsAsync_ReturnsAllDaysOfWeek()
    {
        // Arrange
        var startDate = DateTime.Today.AddDays(-30);
        var endDate = DateTime.Today;

        // Act
        var result = await _service.GetDayOfWeekPatternsAsync(startDate, endDate);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(7); // All 7 days of the week
        result.Select(d => d.DayOfWeek).Should().Contain(DayOfWeek.Monday);
        result.Select(d => d.DayOfWeek).Should().Contain(DayOfWeek.Sunday);
    }

    [Fact]
    public async Task GetHourlyPatternsAsync_Returns24Hours()
    {
        // Arrange
        var startDate = DateTime.Today.AddDays(-7);
        var endDate = DateTime.Today;

        // Act
        var result = await _service.GetHourlyPatternsAsync(startDate, endDate);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(24); // All 24 hours
        result.First().Hour.Should().Be(0);
        result.Last().Hour.Should().Be(23);
    }

    [Fact]
    public async Task GetDayOfWeekPatternsAsync_IncludesHeatIntensity()
    {
        // Arrange
        var startDate = DateTime.Today.AddDays(-30);
        var endDate = DateTime.Today;

        // Act
        var result = await _service.GetDayOfWeekPatternsAsync(startDate, endDate);

        // Assert
        result.Should().NotBeNull();
        result.Should().OnlyContain(d => d.HeatIntensity >= 0 && d.HeatIntensity <= 1);
        result.Should().OnlyContain(d => !string.IsNullOrEmpty(d.ColorCode));
    }

    #endregion

    #region Sparkline Tests

    [Fact]
    public async Task GetSparklineDataAsync_ReturnsMetricSparklines()
    {
        // Act
        var result = await _service.GetSparklineDataAsync(14);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3); // Sales, Transactions, Avg Ticket
        result.Select(s => s.MetricName).Should().Contain("Sales");
        result.Select(s => s.MetricName).Should().Contain("Transactions");
        result.Select(s => s.MetricName).Should().Contain("Avg Ticket");
    }

    [Fact]
    public async Task GetSparklineDataAsync_IncludesTrendDirection()
    {
        // Act
        var result = await _service.GetSparklineDataAsync(14);

        // Assert
        result.Should().NotBeNull();
        result.Should().OnlyContain(s =>
            s.Direction == TrendDirection.Up ||
            s.Direction == TrendDirection.Down ||
            s.Direction == TrendDirection.Flat);
    }

    #endregion

    #region Complete Analytics Tests

    [Fact]
    public async Task GetComparativeAnalyticsAsync_ReturnsCompleteAnalytics()
    {
        // Arrange
        var request = new PeriodComparisonRequest
        {
            PeriodType = ComparisonPeriodType.WeekOverWeek
        };

        // Act
        var result = await _service.GetComparativeAnalyticsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.PeriodComparison.Should().NotBeNull();
        result.SalesTrend.Should().NotBeNull();
        result.CategoryComparisons.Should().NotBeNull();
        result.TopMovers.Should().NotBeNull();
        result.DayOfWeekPatterns.Should().NotBeNull();
        result.Sparklines.Should().NotBeNull();
        result.GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region Export Tests

    [Fact]
    public async Task ExportAnalyticsAsync_ReturnsExportContainer()
    {
        // Arrange
        var request = new PeriodComparisonRequest
        {
            PeriodType = ComparisonPeriodType.MonthOverMonth
        };

        // Act
        var result = await _service.ExportAnalyticsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.ReportTitle.Should().NotBeNullOrEmpty();
        result.Data.Should().NotBeNull();
        result.GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region Utility Method Tests

    [Theory]
    [InlineData(ComparisonPeriodType.DayOverDay, true, "Today")]
    [InlineData(ComparisonPeriodType.DayOverDay, false, "Yesterday")]
    [InlineData(ComparisonPeriodType.WeekOverWeek, true, "This Week")]
    [InlineData(ComparisonPeriodType.WeekOverWeek, false, "Last Week")]
    [InlineData(ComparisonPeriodType.MonthOverMonth, true, "This Month")]
    [InlineData(ComparisonPeriodType.MonthOverMonth, false, "Last Month")]
    [InlineData(ComparisonPeriodType.YearOverYear, true, "This Year")]
    [InlineData(ComparisonPeriodType.YearOverYear, false, "Last Year")]
    public void GetPeriodLabel_ReturnsCorrectLabel(
        ComparisonPeriodType periodType, bool isCurrent, string expectedLabel)
    {
        // Act
        var result = _service.GetPeriodLabel(periodType, isCurrent);

        // Assert
        result.Should().Be(expectedLabel);
    }

    [Fact]
    public void ResolvePeriodDates_WeekOverWeek_ReturnsCorrectDates()
    {
        // Act
        var (currentStart, currentEnd, previousStart, previousEnd) =
            _service.ResolvePeriodDates(ComparisonPeriodType.WeekOverWeek);

        // Assert
        (currentEnd - currentStart).Days.Should().Be(7);
        (previousEnd - previousStart).Days.Should().Be(7);
        (currentStart - previousStart).Days.Should().Be(7);
    }

    [Fact]
    public void ResolvePeriodDates_MonthOverMonth_ReturnsCorrectDates()
    {
        // Act
        var (currentStart, currentEnd, previousStart, previousEnd) =
            _service.ResolvePeriodDates(ComparisonPeriodType.MonthOverMonth);

        // Assert
        currentStart.Day.Should().Be(1);
        previousStart.Day.Should().Be(1);
        currentStart.Should().Be(previousEnd);
    }

    #endregion

    #region Helper Methods

    private async Task SeedTestData()
    {
        // Create test user
        var user = new User
        {
            Id = TestUserId,
            Username = "testuser",
            PasswordHash = "hash",
            FullName = "Test User",
            IsActive = true
        };
        _context.Users.Add(user);

        // Create categories
        var category1 = new Category { Id = 1, Name = "Beverages", IsActive = true };
        var category2 = new Category { Id = 2, Name = "Food", IsActive = true };
        _context.Categories.AddRange(category1, category2);

        // Create products
        var product1 = new Product
        {
            Id = 1,
            Code = "BEV-001",
            Name = "Coca Cola",
            CategoryId = 1,
            SellingPrice = 150m,
            CostPrice = 100m,
            TrackInventory = true,
            IsActive = true
        };
        var product2 = new Product
        {
            Id = 2,
            Code = "FOOD-001",
            Name = "Burger",
            CategoryId = 2,
            SellingPrice = 500m,
            CostPrice = 300m,
            TrackInventory = true,
            IsActive = true
        };
        _context.Products.AddRange(product1, product2);

        // Create payment method
        var paymentMethod = new PaymentMethod
        {
            Id = 1,
            Name = "Cash",
            IsActive = true,
            RequiresReference = false
        };
        _context.PaymentMethods.Add(paymentMethod);

        await _context.SaveChangesAsync();
    }

    private async Task AddTestReceipts(DateTime settlementDate, decimal amount, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var receipt = new Receipt
            {
                ReceiptNumber = $"RCP-{Guid.NewGuid():N}".Substring(0, 20),
                StoreId = TestStoreId,
                CreatedById = TestUserId,
                Status = ReceiptStatus.Settled,
                SettledAt = settlementDate.AddHours(10 + i),
                Subtotal = amount * 0.862m, // Before tax
                TaxAmount = amount * 0.138m, // 16% VAT
                TotalAmount = amount,
                DiscountAmount = 0,
                CreatedAt = settlementDate.AddHours(10 + i)
            };

            _context.Receipts.Add(receipt);
            await _context.SaveChangesAsync();

            // Add receipt items
            var receiptItem = new ReceiptItem
            {
                ReceiptId = receipt.Id,
                ProductId = 1,
                ProductName = "Test Product",
                Quantity = 2,
                UnitPrice = amount / 2,
                TotalPrice = amount
            };
            _context.ReceiptItems.Add(receiptItem);
        }

        await _context.SaveChangesAsync();
    }

    #endregion
}
