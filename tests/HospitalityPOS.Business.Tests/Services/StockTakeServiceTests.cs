using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;
using HospitalityPOS.Infrastructure.Services;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for the StockTakeService class.
/// </summary>
public class StockTakeServiceTests : IDisposable
{
    private readonly POSDbContext _context;
    private readonly Mock<IInventoryService> _inventoryServiceMock;
    private readonly Mock<ISessionService> _sessionServiceMock;
    private readonly Mock<ILogger> _loggerMock;
    private readonly StockTakeService _service;

    public StockTakeServiceTests()
    {
        // Create in-memory database
        var options = new DbContextOptionsBuilder<POSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new POSDbContext(options);

        _inventoryServiceMock = new Mock<IInventoryService>();
        _sessionServiceMock = new Mock<ISessionService>();
        _loggerMock = new Mock<ILogger>();

        _sessionServiceMock.Setup(s => s.CurrentUserId).Returns(1);

        _service = new StockTakeService(
            _context,
            _inventoryServiceMock.Object,
            _sessionServiceMock.Object,
            _loggerMock.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create test user
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            DisplayName = "Test User",
            PasswordHash = "hash",
            IsActive = true
        };
        _context.Users.Add(user);

        // Create test category
        var category = new Category
        {
            Id = 1,
            Name = "Test Category",
            IsActive = true
        };
        _context.Categories.Add(category);

        // Create test products with inventory
        var products = new[]
        {
            new Product
            {
                Id = 1,
                Name = "Product A",
                Code = "PROD-A",
                SellingPrice = 100m,
                CostPrice = 80m,
                TrackInventory = true,
                IsActive = true,
                CategoryId = 1,
                Inventory = new Inventory { ProductId = 1, CurrentStock = 50 }
            },
            new Product
            {
                Id = 2,
                Name = "Product B",
                Code = "PROD-B",
                SellingPrice = 200m,
                CostPrice = 150m,
                TrackInventory = true,
                IsActive = true,
                CategoryId = 1,
                Inventory = new Inventory { ProductId = 2, CurrentStock = 30 }
            },
            new Product
            {
                Id = 3,
                Name = "Product C (No Tracking)",
                Code = "PROD-C",
                SellingPrice = 50m,
                CostPrice = 40m,
                TrackInventory = false,
                IsActive = true,
                CategoryId = 1
            }
        };

        _context.Products.AddRange(products);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region StartStockTakeAsync Tests

    [Fact]
    public async Task StartStockTakeAsync_ShouldCreateStockTakeWithItems()
    {
        // Act
        var result = await _service.StartStockTakeAsync(1, "Test stock take");

        // Assert
        result.Should().NotBeNull();
        result.StockTakeNumber.Should().StartWith("ST-");
        result.Status.Should().Be(StockTakeStatus.InProgress);
        result.StartedByUserId.Should().Be(1);
        result.Notes.Should().Be("Test stock take");
        result.Items.Should().HaveCount(2); // Only tracked products
        result.TotalItems.Should().Be(2);
    }

    [Fact]
    public async Task StartStockTakeAsync_ShouldPopulateSystemQuantities()
    {
        // Act
        var result = await _service.StartStockTakeAsync(1);

        // Assert
        var productAItem = result.Items.First(i => i.ProductId == 1);
        var productBItem = result.Items.First(i => i.ProductId == 2);

        productAItem.SystemQuantity.Should().Be(50);
        productAItem.CostPrice.Should().Be(80m);
        productAItem.IsCounted.Should().BeFalse();

        productBItem.SystemQuantity.Should().Be(30);
        productBItem.CostPrice.Should().Be(150m);
    }

    [Fact]
    public async Task StartStockTakeAsync_ShouldThrowIfStockTakeAlreadyInProgress()
    {
        // Arrange
        await _service.StartStockTakeAsync(1);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.StartStockTakeAsync(1));
    }

    [Fact]
    public async Task StartStockTakeAsync_ShouldFilterByCategory()
    {
        // Arrange - Add another category and product
        var category2 = new Category { Id = 2, Name = "Category 2", IsActive = true };
        _context.Categories.Add(category2);

        var product4 = new Product
        {
            Id = 4,
            Name = "Product D",
            Code = "PROD-D",
            SellingPrice = 300m,
            CostPrice = 250m,
            TrackInventory = true,
            IsActive = true,
            CategoryId = 2,
            Inventory = new Inventory { ProductId = 4, CurrentStock = 20 }
        };
        _context.Products.Add(product4);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.StartStockTakeAsync(1, categoryId: 1);

        // Assert
        result.Items.Should().HaveCount(2); // Only products from category 1
        result.Items.All(i => i.Product.CategoryId == 1).Should().BeTrue();
    }

    #endregion

    #region RecordCountAsync Tests

    [Fact]
    public async Task RecordCountAsync_ShouldRecordPhysicalCount()
    {
        // Arrange
        var stockTake = await _service.StartStockTakeAsync(1);
        var item = stockTake.Items.First();

        // Act
        var result = await _service.RecordCountAsync(item.Id, 48, 1, "Test note");

        // Assert
        result.PhysicalQuantity.Should().Be(48);
        result.VarianceQuantity.Should().Be(48 - item.SystemQuantity);
        result.IsCounted.Should().BeTrue();
        result.CountedByUserId.Should().Be(1);
        result.Notes.Should().Be("Test note");
        result.CountedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RecordCountAsync_ShouldCalculateVarianceValue()
    {
        // Arrange
        var stockTake = await _service.StartStockTakeAsync(1);
        var item = stockTake.Items.First(i => i.ProductId == 1);
        // System: 50, Cost: 80

        // Act - Physical count is 48 (shortage of 2)
        var result = await _service.RecordCountAsync(item.Id, 48, 1);

        // Assert
        result.VarianceQuantity.Should().Be(-2);
        result.VarianceValue.Should().Be(-2 * 80); // -160
    }

    [Fact]
    public async Task RecordCountAsync_ShouldUpdateStockTakeSummary()
    {
        // Arrange
        var stockTake = await _service.StartStockTakeAsync(1);
        var item = stockTake.Items.First();

        // Act
        await _service.RecordCountAsync(item.Id, 45, 1);

        // Assert
        var updatedStockTake = await _service.GetStockTakeAsync(stockTake.Id);
        updatedStockTake!.CountedItems.Should().Be(1);
    }

    [Fact]
    public async Task RecordCountAsync_ShouldThrowForNonInProgressStockTake()
    {
        // Arrange
        var stockTake = await _service.StartStockTakeAsync(1);
        var item = stockTake.Items.First();

        // Mark as pending approval
        stockTake.Status = StockTakeStatus.PendingApproval;
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RecordCountAsync(item.Id, 45, 1));
    }

    #endregion

    #region RecordCountsAsync Tests

    [Fact]
    public async Task RecordCountsAsync_ShouldRecordMultipleCounts()
    {
        // Arrange
        var stockTake = await _service.StartStockTakeAsync(1);
        var counts = new Dictionary<int, decimal>
        {
            { 1, 48 },
            { 2, 32 }
        };

        // Act
        var result = await _service.RecordCountsAsync(stockTake.Id, counts, 1);

        // Assert
        result.Should().HaveCount(2);

        var updatedStockTake = await _service.GetStockTakeAsync(stockTake.Id);
        updatedStockTake!.CountedItems.Should().Be(2);
    }

    #endregion

    #region SubmitForApprovalAsync Tests

    [Fact]
    public async Task SubmitForApprovalAsync_ShouldChangeStatusToPendingApproval()
    {
        // Arrange
        var stockTake = await _service.StartStockTakeAsync(1);

        // Act
        var result = await _service.SubmitForApprovalAsync(stockTake.Id);

        // Assert
        result.Status.Should().Be(StockTakeStatus.PendingApproval);
    }

    [Fact]
    public async Task SubmitForApprovalAsync_ShouldUpdateSummaryValues()
    {
        // Arrange
        var stockTake = await _service.StartStockTakeAsync(1);
        foreach (var item in stockTake.Items)
        {
            await _service.RecordCountAsync(item.Id, item.SystemQuantity - 2, 1);
        }

        // Act
        var result = await _service.SubmitForApprovalAsync(stockTake.Id);

        // Assert
        result.CountedItems.Should().Be(2);
        result.ItemsWithVariance.Should().Be(2);
        result.TotalVarianceValue.Should().BeLessThan(0);
    }

    #endregion

    #region ApproveStockTakeAsync Tests

    [Fact]
    public async Task ApproveStockTakeAsync_ShouldApplyAdjustments()
    {
        // Arrange
        var stockTake = await _service.StartStockTakeAsync(1);
        var item = stockTake.Items.First();
        await _service.RecordCountAsync(item.Id, 45, 1);
        await _service.SubmitForApprovalAsync(stockTake.Id);

        _inventoryServiceMock
            .Setup(s => s.AdjustStockAsync(It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(new StockMovement());

        // Act
        var result = await _service.ApproveStockTakeAsync(stockTake.Id, 1);

        // Assert
        result.Status.Should().Be(StockTakeStatus.Approved);
        result.CompletedAt.Should().NotBeNull();
        result.ApprovedByUserId.Should().Be(1);

        _inventoryServiceMock.Verify(
            s => s.AdjustStockAsync(item.ProductId, 45, It.IsAny<string>(), It.IsAny<string?>()),
            Times.Once);
    }

    [Fact]
    public async Task ApproveStockTakeAsync_ShouldNotAdjustItemsWithNoVariance()
    {
        // Arrange
        var stockTake = await _service.StartStockTakeAsync(1);
        var item = stockTake.Items.First();
        // Count same as system quantity (no variance)
        await _service.RecordCountAsync(item.Id, item.SystemQuantity, 1);
        await _service.SubmitForApprovalAsync(stockTake.Id);

        // Act
        await _service.ApproveStockTakeAsync(stockTake.Id, 1);

        // Assert
        _inventoryServiceMock.Verify(
            s => s.AdjustStockAsync(It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public async Task ApproveStockTakeAsync_ShouldThrowForNonPendingApprovalStatus()
    {
        // Arrange
        var stockTake = await _service.StartStockTakeAsync(1);
        // Don't submit for approval

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ApproveStockTakeAsync(stockTake.Id, 1));
    }

    #endregion

    #region CancelStockTakeAsync Tests

    [Fact]
    public async Task CancelStockTakeAsync_ShouldCancelInProgressStockTake()
    {
        // Arrange
        var stockTake = await _service.StartStockTakeAsync(1);

        // Act
        var result = await _service.CancelStockTakeAsync(stockTake.Id, "User requested cancellation");

        // Assert
        result.Status.Should().Be(StockTakeStatus.Cancelled);
        result.Notes.Should().Contain("Cancelled: User requested cancellation");
    }

    [Fact]
    public async Task CancelStockTakeAsync_ShouldThrowForApprovedStockTake()
    {
        // Arrange
        var stockTake = await _service.StartStockTakeAsync(1);
        stockTake.Status = StockTakeStatus.Approved;
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CancelStockTakeAsync(stockTake.Id, "Test"));
    }

    #endregion

    #region GetVarianceSummaryAsync Tests

    [Fact]
    public async Task GetVarianceSummaryAsync_ShouldCalculateCorrectSummary()
    {
        // Arrange
        var stockTake = await _service.StartStockTakeAsync(1);
        var productA = stockTake.Items.First(i => i.ProductId == 1); // System: 50, Cost: 80
        var productB = stockTake.Items.First(i => i.ProductId == 2); // System: 30, Cost: 150

        await _service.RecordCountAsync(productA.Id, 48, 1); // -2 variance, -160 value
        await _service.RecordCountAsync(productB.Id, 32, 1); // +2 variance, +300 value

        // Act
        var summary = await _service.GetVarianceSummaryAsync(stockTake.Id);

        // Assert
        summary.TotalItems.Should().Be(2);
        summary.CountedItems.Should().Be(2);
        summary.ItemsWithVariance.Should().Be(2);
        summary.Shortages.Should().HaveCount(1);
        summary.Overages.Should().HaveCount(1);
        summary.ShortageValue.Should().Be(160); // Absolute value
        summary.OverageValue.Should().Be(300);
        summary.NetVarianceValue.Should().Be(140); // 300 - 160
    }

    [Fact]
    public async Task GetVarianceSummaryAsync_ShouldCalculateProgressPercentage()
    {
        // Arrange
        var stockTake = await _service.StartStockTakeAsync(1);
        var item = stockTake.Items.First();
        await _service.RecordCountAsync(item.Id, 45, 1);

        // Act
        var summary = await _service.GetVarianceSummaryAsync(stockTake.Id);

        // Assert
        summary.ProgressPercentage.Should().Be(50); // 1 of 2 counted
    }

    #endregion

    #region GenerateStockTakeNumberAsync Tests

    [Fact]
    public async Task GenerateStockTakeNumberAsync_ShouldGenerateSequentialNumbers()
    {
        // Act
        var number1 = await _service.GenerateStockTakeNumberAsync();
        var stockTake1 = await _service.StartStockTakeAsync(1);

        // Cancel to allow starting another
        await _service.CancelStockTakeAsync(stockTake1.Id, "Test");

        var number2 = await _service.GenerateStockTakeNumberAsync();

        // Assert
        number1.Should().EndWith("-001");
        number2.Should().EndWith("-002");
    }

    #endregion

    #region GetStockTakesAsync Tests

    [Fact]
    public async Task GetStockTakesAsync_ShouldFilterByStatus()
    {
        // Arrange
        var stockTake = await _service.StartStockTakeAsync(1);
        await _service.SubmitForApprovalAsync(stockTake.Id);

        // Act
        var pendingResults = await _service.GetStockTakesAsync(status: StockTakeStatus.PendingApproval);
        var inProgressResults = await _service.GetStockTakesAsync(status: StockTakeStatus.InProgress);

        // Assert
        pendingResults.Should().HaveCount(1);
        inProgressResults.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStockTakesAsync_ShouldFilterByDateRange()
    {
        // Arrange
        var stockTake = await _service.StartStockTakeAsync(1);

        // Act
        var results = await _service.GetStockTakesAsync(
            startDate: DateTime.UtcNow.AddHours(-1),
            endDate: DateTime.UtcNow.AddHours(1));

        // Assert
        results.Should().HaveCount(1);
    }

    #endregion
}
