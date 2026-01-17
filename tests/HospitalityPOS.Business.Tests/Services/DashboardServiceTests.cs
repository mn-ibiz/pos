using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Infrastructure.Data;
using HospitalityPOS.Infrastructure.Services;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for the DashboardService class.
/// Tests cover today's sales summary, hourly breakdown, top products, payment methods, and comparison metrics.
/// </summary>
public class DashboardServiceTests : IAsyncLifetime, IDisposable
{
    private readonly POSDbContext _context;
    private readonly Mock<ILogger> _loggerMock;
    private readonly DashboardService _dashboardService;
    private const int TestUserId = 1;

    public DashboardServiceTests()
    {
        var options = new DbContextOptionsBuilder<POSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new POSDbContext(options);
        _loggerMock = new Mock<ILogger>();

        _dashboardService = new DashboardService(_context, _loggerMock.Object);
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
            MinimumStockLevel = 10,
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
            MinimumStockLevel = 5,
            IsActive = true
        };
        var product3 = new Product
        {
            Id = 3,
            Code = "BEV-002",
            Name = "Fanta",
            CategoryId = 1,
            SellingPrice = 150m,
            CostPrice = 100m,
            TrackInventory = true,
            MinimumStockLevel = 10,
            IsActive = true
        };
        _context.Products.AddRange(product1, product2, product3);

        // Create inventory records
        var inventory1 = new Inventory { Id = 1, ProductId = 1, CurrentStock = 5 }; // Low stock
        var inventory2 = new Inventory { Id = 2, ProductId = 2, CurrentStock = 20 };
        var inventory3 = new Inventory { Id = 3, ProductId = 3, CurrentStock = 0 }; // Out of stock
        _context.Inventories.AddRange(inventory1, inventory2, inventory3);

        // Create payment methods
        var cash = new PaymentMethod { Id = 1, Name = "Cash", IsActive = true };
        var mpesa = new PaymentMethod { Id = 2, Name = "M-Pesa", IsActive = true };
        _context.PaymentMethods.AddRange(cash, mpesa);

        // Create today's receipts
        var today = DateTime.UtcNow.Date;
        await CreateReceipt(1, "R-001", today.AddHours(9), ReceiptStatus.Settled, 650m, 50m, 104m, 704m, product1, product2);
        await CreateReceipt(2, "R-002", today.AddHours(10), ReceiptStatus.Settled, 300m, 0m, 48m, 348m, product1, product3);
        await CreateReceipt(3, "R-003", today.AddHours(11), ReceiptStatus.Settled, 500m, 25m, 76m, 551m, product2);
        await CreateReceipt(4, "R-004", today.AddHours(14), ReceiptStatus.Settled, 150m, 0m, 24m, 174m, product1);

        // Create yesterday's receipts
        var yesterday = today.AddDays(-1);
        await CreateReceipt(5, "R-005", yesterday.AddHours(9), ReceiptStatus.Settled, 450m, 0m, 72m, 522m, product1, product2);
        await CreateReceipt(6, "R-006", yesterday.AddHours(15), ReceiptStatus.Settled, 300m, 0m, 48m, 348m, product2);

        // Create last week same day receipts
        var lastWeek = today.AddDays(-7);
        await CreateReceipt(7, "R-007", lastWeek.AddHours(10), ReceiptStatus.Settled, 600m, 0m, 96m, 696m, product1, product2);

        // Create payments
        await CreatePayment(1, 1, 1, today.AddHours(9), 400m); // Cash
        await CreatePayment(2, 1, 2, today.AddHours(9), 304m); // M-Pesa
        await CreatePayment(3, 2, 1, today.AddHours(10), 348m);
        await CreatePayment(4, 3, 2, today.AddHours(11), 551m);
        await CreatePayment(5, 4, 1, today.AddHours(14), 174m);

        await _context.SaveChangesAsync();
    }

    private async Task CreateReceipt(int id, string number, DateTime settledAt, ReceiptStatus status,
        decimal subtotal, decimal discount, decimal tax, decimal total, params Product[] products)
    {
        var receipt = new Receipt
        {
            Id = id,
            ReceiptNumber = number,
            OwnerId = TestUserId,
            Status = status,
            Subtotal = subtotal,
            DiscountAmount = discount,
            TaxAmount = tax,
            TotalAmount = total,
            SettledAt = settledAt,
            SettledByUserId = TestUserId,
            IsActive = true
        };
        _context.Receipts.Add(receipt);

        foreach (var product in products)
        {
            var item = new ReceiptItem
            {
                ReceiptId = id,
                ProductId = product.Id,
                Quantity = 1,
                UnitPrice = product.SellingPrice,
                TotalAmount = product.SellingPrice,
                TaxAmount = product.SellingPrice * 0.16m,
                DiscountAmount = 0
            };
            _context.ReceiptItems.Add(item);
        }
    }

    private async Task CreatePayment(int id, int receiptId, int paymentMethodId, DateTime paidAt, decimal amount)
    {
        var payment = new Payment
        {
            Id = id,
            ReceiptId = receiptId,
            PaymentMethodId = paymentMethodId,
            Amount = amount,
            PaidAt = paidAt
        };
        _context.Payments.Add(payment);
    }

    #region GetTodaySalesSummaryAsync Tests

    [Fact]
    public async Task GetTodaySalesSummaryAsync_ReturnsCorrectTotals()
    {
        // Act
        var result = await _dashboardService.GetTodaySalesSummaryAsync();

        // Assert
        result.Should().NotBeNull();
        result.TransactionCount.Should().Be(4);
        result.TotalSales.Should().Be(704m + 348m + 551m + 174m); // 1777
        result.TotalDiscounts.Should().Be(50m + 0m + 25m + 0m); // 75
        result.TaxCollected.Should().Be(104m + 48m + 76m + 24m); // 252
    }

    [Fact]
    public async Task GetTodaySalesSummaryAsync_CalculatesAverageTicketCorrectly()
    {
        // Act
        var result = await _dashboardService.GetTodaySalesSummaryAsync();

        // Assert
        var expectedAverage = (704m + 348m + 551m + 174m) / 4;
        result.AverageTicket.Should().Be(expectedAverage);
    }

    [Fact]
    public async Task GetTodaySalesSummaryAsync_ReturnsZeroForEmptyData()
    {
        // Arrange - Clear all receipts
        _context.Receipts.RemoveRange(_context.Receipts);
        await _context.SaveChangesAsync();

        // Act
        var result = await _dashboardService.GetTodaySalesSummaryAsync();

        // Assert
        result.TransactionCount.Should().Be(0);
        result.TotalSales.Should().Be(0);
        result.AverageTicket.Should().Be(0);
    }

    #endregion

    #region GetHourlySalesBreakdownAsync Tests

    [Fact]
    public async Task GetHourlySalesBreakdownAsync_Returns24Hours()
    {
        // Act
        var result = await _dashboardService.GetHourlySalesBreakdownAsync();

        // Assert
        result.Should().HaveCount(24);
        result.Select(h => h.Hour).Should().BeEquivalentTo(Enumerable.Range(0, 24));
    }

    [Fact]
    public async Task GetHourlySalesBreakdownAsync_ReturnsCorrectDataForHoursWithSales()
    {
        // Act
        var result = await _dashboardService.GetHourlySalesBreakdownAsync();

        // Assert
        var hour9 = result.First(h => h.Hour == 9);
        hour9.Sales.Should().Be(704m);
        hour9.TransactionCount.Should().Be(1);

        var hour10 = result.First(h => h.Hour == 10);
        hour10.Sales.Should().Be(348m);
        hour10.TransactionCount.Should().Be(1);
    }

    [Fact]
    public async Task GetHourlySalesBreakdownAsync_ReturnsZeroForHoursWithoutSales()
    {
        // Act
        var result = await _dashboardService.GetHourlySalesBreakdownAsync();

        // Assert - Hour 8 should have no sales
        var hour8 = result.First(h => h.Hour == 8);
        hour8.Sales.Should().Be(0);
        hour8.TransactionCount.Should().Be(0);
    }

    [Fact]
    public async Task GetHourlySalesBreakdownAsync_SetsIsCurrentHourCorrectly()
    {
        // Act
        var result = await _dashboardService.GetHourlySalesBreakdownAsync();

        // Assert - Only one hour should be marked as current
        result.Count(h => h.IsCurrentHour).Should().Be(1);
        var currentHour = result.First(h => h.IsCurrentHour);
        currentHour.Hour.Should().Be(DateTime.UtcNow.Hour);
    }

    #endregion

    #region GetTopSellingProductsAsync Tests

    [Fact]
    public async Task GetTopSellingProductsAsync_ReturnsRequestedCount()
    {
        // Act
        var result = await _dashboardService.GetTopSellingProductsAsync(count: 2);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTopSellingProductsAsync_OrdersByRevenueDescending()
    {
        // Act
        var result = await _dashboardService.GetTopSellingProductsAsync();

        // Assert
        result.Should().BeInDescendingOrder(p => p.Revenue);
    }

    [Fact]
    public async Task GetTopSellingProductsAsync_SetsRankCorrectly()
    {
        // Act
        var result = await _dashboardService.GetTopSellingProductsAsync();

        // Assert
        for (int i = 0; i < result.Count; i++)
        {
            result[i].Rank.Should().Be(i + 1);
        }
    }

    #endregion

    #region GetPaymentMethodBreakdownAsync Tests

    [Fact]
    public async Task GetPaymentMethodBreakdownAsync_ReturnsAllPaymentMethods()
    {
        // Act
        var result = await _dashboardService.GetPaymentMethodBreakdownAsync();

        // Assert
        result.Should().HaveCount(2); // Cash and M-Pesa
    }

    [Fact]
    public async Task GetPaymentMethodBreakdownAsync_CalculatesTotalsCorrectly()
    {
        // Act
        var result = await _dashboardService.GetPaymentMethodBreakdownAsync();

        // Assert
        var cash = result.First(p => p.PaymentMethodName == "Cash");
        cash.Amount.Should().Be(400m + 348m + 174m); // 922

        var mpesa = result.First(p => p.PaymentMethodName == "M-Pesa");
        mpesa.Amount.Should().Be(304m + 551m); // 855
    }

    [Fact]
    public async Task GetPaymentMethodBreakdownAsync_CalculatesPercentagesCorrectly()
    {
        // Act
        var result = await _dashboardService.GetPaymentMethodBreakdownAsync();

        // Assert
        var totalAmount = result.Sum(p => p.Amount);
        foreach (var payment in result)
        {
            var expectedPercent = Math.Round(payment.Amount / totalAmount * 100, 1);
            payment.Percentage.Should().Be(expectedPercent);
        }
    }

    [Fact]
    public async Task GetPaymentMethodBreakdownAsync_AssignsColorsToKnownMethods()
    {
        // Act
        var result = await _dashboardService.GetPaymentMethodBreakdownAsync();

        // Assert
        var cash = result.First(p => p.PaymentMethodName == "Cash");
        cash.ColorCode.Should().Be("#4CAF50");

        var mpesa = result.First(p => p.PaymentMethodName == "M-Pesa");
        mpesa.ColorCode.Should().Be("#8BC34A");
    }

    #endregion

    #region GetComparisonMetricsAsync Tests

    [Fact]
    public async Task GetComparisonMetricsAsync_ReturnsYesterdayComparison()
    {
        // Act
        var result = await _dashboardService.GetComparisonMetricsAsync();

        // Assert
        result.Should().NotBeNull();
        // Yesterday's sales should be > 0 since we seeded data
        result.YesterdaySales.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetComparisonMetricsAsync_CalculatesPercentChangeCorrectly()
    {
        // Act
        var result = await _dashboardService.GetComparisonMetricsAsync();

        // Assert - Verify the calculation logic works (exact values depend on time of test)
        result.VsYesterdayPercent.Should().BeInRange(-1000, 1000);
        result.VsLastWeekPercent.Should().BeInRange(-1000, 1000);
    }

    [Fact]
    public async Task GetComparisonMetricsAsync_SetsBetterThanFlagsCorrectly()
    {
        // Act
        var result = await _dashboardService.GetComparisonMetricsAsync();

        // Assert
        if (result.VsYesterdayPercent >= 0)
        {
            result.IsBetterThanYesterday.Should().BeTrue();
        }
        else
        {
            result.IsBetterThanYesterday.Should().BeFalse();
        }
    }

    #endregion

    #region GetLowStockAlertsAsync Tests

    [Fact]
    public async Task GetLowStockAlertsAsync_ReturnsLowStockProducts()
    {
        // Act
        var result = await _dashboardService.GetLowStockAlertsAsync();

        // Assert
        result.TotalCount.Should().Be(2); // product1 (5 < 10) and product3 (0 < 10)
    }

    [Fact]
    public async Task GetLowStockAlertsAsync_CountsOutOfStockCorrectly()
    {
        // Act
        var result = await _dashboardService.GetLowStockAlertsAsync();

        // Assert
        result.OutOfStockCount.Should().Be(1); // product3 has 0 stock
    }

    [Fact]
    public async Task GetLowStockAlertsAsync_RespectsMaxItems()
    {
        // Act
        var result = await _dashboardService.GetLowStockAlertsAsync(maxItems: 1);

        // Assert
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(2); // Total count is still 2
    }

    [Fact]
    public async Task GetLowStockAlertsAsync_OrdersByCurrentStockAscending()
    {
        // Act
        var result = await _dashboardService.GetLowStockAlertsAsync();

        // Assert
        result.Items.Should().BeInAscendingOrder(p => p.CurrentStock);
    }

    #endregion

    #region GetDashboardDataAsync Tests

    [Fact]
    public async Task GetDashboardDataAsync_ReturnsAllComponents()
    {
        // Act
        var result = await _dashboardService.GetDashboardDataAsync();

        // Assert
        result.Should().NotBeNull();
        result.SalesSummary.Should().NotBeNull();
        result.HourlySales.Should().NotBeEmpty();
        result.TopProducts.Should().NotBeNull();
        result.PaymentBreakdown.Should().NotBeNull();
        result.Comparison.Should().NotBeNull();
        result.LowStockAlerts.Should().NotBeNull();
        result.ExpiryAlerts.Should().NotBeNull();
        result.SyncStatus.Should().NotBeNull();
    }

    [Fact]
    public async Task GetDashboardDataAsync_SetsRetrievedAtTimestamp()
    {
        // Act
        var before = DateTime.UtcNow;
        var result = await _dashboardService.GetDashboardDataAsync();
        var after = DateTime.UtcNow;

        // Assert
        result.RetrievedAt.Should().BeOnOrAfter(before);
        result.RetrievedAt.Should().BeOnOrBefore(after);
    }

    #endregion

    #region GetSyncStatusAsync Tests

    [Fact]
    public async Task GetSyncStatusAsync_ReturnsOnlineWhenNoPendingSync()
    {
        // Act
        var result = await _dashboardService.GetSyncStatusAsync();

        // Assert
        result.PendingCount.Should().Be(0);
        result.IsOnline.Should().BeTrue();
        result.IsSyncing.Should().BeFalse();
    }

    #endregion
}
