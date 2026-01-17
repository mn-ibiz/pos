using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;
using HospitalityPOS.Infrastructure.Services;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for the InventoryService class.
/// Tests cover stock deduction on sale, stock restoration on void, and stock adjustments.
/// </summary>
public class InventoryServiceTests : IDisposable
{
    private readonly POSDbContext _context;
    private readonly Mock<ISessionService> _sessionServiceMock;
    private readonly Mock<ILogger> _loggerMock;
    private readonly InventoryService _inventoryService;
    private const int TestUserId = 1;

    public InventoryServiceTests()
    {
        var options = new DbContextOptionsBuilder<POSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new POSDbContext(options);
        _sessionServiceMock = new Mock<ISessionService>();
        _loggerMock = new Mock<ILogger>();

        _sessionServiceMock.Setup(s => s.CurrentUserId).Returns(TestUserId);

        _inventoryService = new InventoryService(
            _context,
            _sessionServiceMock.Object,
            _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<Product> CreateTestProductAsync(
        string name = "Test Product",
        decimal currentStock = 100,
        bool trackInventory = true,
        decimal? minStockLevel = 10)
    {
        var product = new Product
        {
            Code = $"PROD-{Guid.NewGuid():N}".Substring(0, 20),
            Name = name,
            SellingPrice = 100m,
            CostPrice = 50m,
            TrackInventory = trackInventory,
            MinStockLevel = minStockLevel,
            IsActive = true
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var inventory = new Inventory
        {
            ProductId = product.Id,
            CurrentStock = currentStock,
            ReservedStock = 0,
            LastUpdated = DateTime.UtcNow
        };

        _context.Inventories.Add(inventory);
        await _context.SaveChangesAsync();

        return product;
    }

    private async Task<Receipt> CreateTestReceiptAsync(params (int productId, decimal quantity)[] items)
    {
        var receipt = new Receipt
        {
            ReceiptNumber = $"R-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid():N}".Substring(0, 20),
            OwnerId = TestUserId,
            Status = ReceiptStatus.Pending,
            Subtotal = 0,
            TotalAmount = 0
        };

        _context.Receipts.Add(receipt);
        await _context.SaveChangesAsync();

        foreach (var (productId, quantity) in items)
        {
            var product = await _context.Products.FindAsync(productId);
            var receiptItem = new ReceiptItem
            {
                ReceiptId = receipt.Id,
                ProductId = productId,
                ProductName = product?.Name ?? "Unknown",
                Quantity = quantity,
                UnitPrice = product?.SellingPrice ?? 100m,
                TotalAmount = quantity * (product?.SellingPrice ?? 100m)
            };
            _context.ReceiptItems.Add(receiptItem);
        }

        await _context.SaveChangesAsync();

        return await _context.Receipts
            .Include(r => r.ReceiptItems)
            .FirstAsync(r => r.Id == receipt.Id);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new InventoryService(null!, _sessionServiceMock.Object, _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public void Constructor_WithNullSessionService_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new InventoryService(_context, null!, _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("sessionService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new InventoryService(_context, _sessionServiceMock.Object, null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region DeductStockAsync Tests

    [Fact]
    public async Task DeductStockAsync_ShouldDeductStock_WhenProductExists()
    {
        // Arrange
        var product = await CreateTestProductAsync(currentStock: 100);

        // Act
        var movement = await _inventoryService.DeductStockAsync(
            product.Id,
            quantity: 10,
            reference: "R-TEST-001",
            referenceId: 1);

        // Assert
        movement.Should().NotBeNull();
        movement!.Quantity.Should().Be(-10);
        movement.PreviousStock.Should().Be(100);
        movement.NewStock.Should().Be(90);
        movement.MovementType.Should().Be(MovementType.Sale);

        var inventory = await _context.Inventories.FirstAsync(i => i.ProductId == product.Id);
        inventory.CurrentStock.Should().Be(90);
    }

    [Fact]
    public async Task DeductStockAsync_ShouldSkipDeduction_WhenTrackInventoryIsFalse()
    {
        // Arrange
        var product = await CreateTestProductAsync(trackInventory: false);

        // Act
        var movement = await _inventoryService.DeductStockAsync(
            product.Id,
            quantity: 10,
            reference: "R-TEST-001");

        // Assert
        movement.Should().BeNull();

        // Stock should remain unchanged
        var inventory = await _context.Inventories.FirstAsync(i => i.ProductId == product.Id);
        inventory.CurrentStock.Should().Be(100);
    }

    [Fact]
    public async Task DeductStockAsync_ShouldNotGoNegative_WhenDeductingMoreThanAvailable()
    {
        // Arrange
        var product = await CreateTestProductAsync(currentStock: 5);

        // Act
        var movement = await _inventoryService.DeductStockAsync(
            product.Id,
            quantity: 10,
            reference: "R-TEST-001");

        // Assert
        movement.Should().NotBeNull();
        movement!.NewStock.Should().Be(0); // Should be capped at 0

        var inventory = await _context.Inventories.FirstAsync(i => i.ProductId == product.Id);
        inventory.CurrentStock.Should().Be(0);
    }

    [Fact]
    public async Task DeductStockAsync_ShouldReturnNull_WhenProductNotFound()
    {
        // Act
        var movement = await _inventoryService.DeductStockAsync(
            productId: 99999,
            quantity: 10,
            reference: "R-TEST-001");

        // Assert
        movement.Should().BeNull();
    }

    [Fact]
    public async Task DeductStockAsync_ShouldCreateInventoryRecord_WhenNoInventoryExists()
    {
        // Arrange - Create product without inventory
        var product = new Product
        {
            Code = "PROD-NEW",
            Name = "New Product",
            SellingPrice = 100m,
            TrackInventory = true,
            IsActive = true
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act
        var movement = await _inventoryService.DeductStockAsync(
            product.Id,
            quantity: 5,
            reference: "R-TEST-001");

        // Assert
        movement.Should().NotBeNull();
        movement!.PreviousStock.Should().Be(0);
        movement.NewStock.Should().Be(0); // 0 - 5 = -5, capped at 0

        var inventory = await _context.Inventories.FirstAsync(i => i.ProductId == product.Id);
        inventory.Should().NotBeNull();
    }

    #endregion

    #region RestoreStockAsync Tests

    [Fact]
    public async Task RestoreStockAsync_ShouldRestoreStock_WhenProductExists()
    {
        // Arrange
        var product = await CreateTestProductAsync(currentStock: 50);

        // Act
        var movement = await _inventoryService.RestoreStockAsync(
            product.Id,
            quantity: 10,
            MovementType.Void,
            "Void: R-TEST-001",
            referenceId: 1);

        // Assert
        movement.Should().NotBeNull();
        movement!.Quantity.Should().Be(10);
        movement.PreviousStock.Should().Be(50);
        movement.NewStock.Should().Be(60);
        movement.MovementType.Should().Be(MovementType.Void);

        var inventory = await _context.Inventories.FirstAsync(i => i.ProductId == product.Id);
        inventory.CurrentStock.Should().Be(60);
    }

    [Fact]
    public async Task RestoreStockAsync_ShouldSkipRestore_WhenTrackInventoryIsFalse()
    {
        // Arrange
        var product = await CreateTestProductAsync(trackInventory: false, currentStock: 50);

        // Act
        var movement = await _inventoryService.RestoreStockAsync(
            product.Id,
            quantity: 10,
            MovementType.Void,
            "Void: R-TEST-001");

        // Assert
        movement.Should().BeNull();

        var inventory = await _context.Inventories.FirstAsync(i => i.ProductId == product.Id);
        inventory.CurrentStock.Should().Be(50);
    }

    #endregion

    #region AdjustStockAsync Tests

    [Fact]
    public async Task AdjustStockAsync_ShouldSetNewQuantity_WhenIncreasing()
    {
        // Arrange
        var product = await CreateTestProductAsync(currentStock: 50);

        // Act
        var movement = await _inventoryService.AdjustStockAsync(
            product.Id,
            newQuantity: 100,
            reason: "Correction",
            notes: "Found extra stock");

        // Assert
        movement.Should().NotBeNull();
        movement.Quantity.Should().Be(50); // 100 - 50
        movement.PreviousStock.Should().Be(50);
        movement.NewStock.Should().Be(100);
        movement.MovementType.Should().Be(MovementType.Adjustment);

        var inventory = await _context.Inventories.FirstAsync(i => i.ProductId == product.Id);
        inventory.CurrentStock.Should().Be(100);
    }

    [Fact]
    public async Task AdjustStockAsync_ShouldSetNewQuantity_WhenDecreasing()
    {
        // Arrange
        var product = await CreateTestProductAsync(currentStock: 100);

        // Act
        var movement = await _inventoryService.AdjustStockAsync(
            product.Id,
            newQuantity: 30,
            reason: "Wastage",
            notes: "Damaged items");

        // Assert
        movement.Should().NotBeNull();
        movement.Quantity.Should().Be(-70); // 30 - 100
        movement.NewStock.Should().Be(30);

        var inventory = await _context.Inventories.FirstAsync(i => i.ProductId == product.Id);
        inventory.CurrentStock.Should().Be(30);
    }

    [Fact]
    public async Task AdjustStockAsync_ShouldThrow_WhenProductNotFound()
    {
        // Act
        var action = () => _inventoryService.AdjustStockAsync(
            productId: 99999,
            newQuantity: 50,
            reason: "Correction");

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Product with ID 99999 not found.");
    }

    [Fact]
    public async Task AdjustStockAsync_WithReasonId_ShouldSetNewQuantity()
    {
        // Arrange
        var product = await CreateTestProductAsync(currentStock: 50);
        var adjustmentReason = new AdjustmentReason
        {
            Name = "Damaged/Broken",
            Code = "DMG",
            IsActive = true,
            IsDecrease = true,
            DisplayOrder = 1
        };
        _context.AdjustmentReasons.Add(adjustmentReason);
        await _context.SaveChangesAsync();

        // Act
        var movement = await _inventoryService.AdjustStockAsync(
            product.Id,
            newQuantity: 40,
            adjustmentReasonId: adjustmentReason.Id,
            notes: "5 items damaged in storage");

        // Assert
        movement.Should().NotBeNull();
        movement.Quantity.Should().Be(-10); // 40 - 50
        movement.PreviousStock.Should().Be(50);
        movement.NewStock.Should().Be(40);
        movement.AdjustmentReasonId.Should().Be(adjustmentReason.Id);
        movement.Notes.Should().Be("5 items damaged in storage");
        movement.Reason.Should().Be("Damaged/Broken");

        var inventory = await _context.Inventories.FirstAsync(i => i.ProductId == product.Id);
        inventory.CurrentStock.Should().Be(40);
    }

    [Fact]
    public async Task AdjustStockAsync_WithReasonId_ShouldThrow_WhenReasonNotFound()
    {
        // Arrange
        var product = await CreateTestProductAsync(currentStock: 50);

        // Act
        var action = () => _inventoryService.AdjustStockAsync(
            product.Id,
            newQuantity: 40,
            adjustmentReasonId: 99999,
            notes: "Test");

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Adjustment reason with ID 99999 not found or is inactive.");
    }

    [Fact]
    public async Task AdjustStockAsync_WithReasonId_ShouldThrow_WhenReasonIsInactive()
    {
        // Arrange
        var product = await CreateTestProductAsync(currentStock: 50);
        var inactiveReason = new AdjustmentReason
        {
            Name = "Inactive Reason",
            Code = "INA",
            IsActive = false, // Inactive
            IsDecrease = true,
            DisplayOrder = 1
        };
        _context.AdjustmentReasons.Add(inactiveReason);
        await _context.SaveChangesAsync();

        // Act
        var action = () => _inventoryService.AdjustStockAsync(
            product.Id,
            newQuantity: 40,
            adjustmentReasonId: inactiveReason.Id,
            notes: "Test");

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Adjustment reason with ID {inactiveReason.Id} not found or is inactive.");
    }

    #endregion

    #region DeductStockForReceiptAsync Tests

    [Fact]
    public async Task DeductStockForReceiptAsync_ShouldDeductAllItems()
    {
        // Arrange
        var product1 = await CreateTestProductAsync(name: "Product 1", currentStock: 100);
        var product2 = await CreateTestProductAsync(name: "Product 2", currentStock: 50);

        var receipt = await CreateTestReceiptAsync(
            (product1.Id, 10),
            (product2.Id, 5));

        // Act
        var movements = await _inventoryService.DeductStockForReceiptAsync(receipt);

        // Assert
        movements.Should().HaveCount(2);

        var inv1 = await _context.Inventories.FirstAsync(i => i.ProductId == product1.Id);
        inv1.CurrentStock.Should().Be(90);

        var inv2 = await _context.Inventories.FirstAsync(i => i.ProductId == product2.Id);
        inv2.CurrentStock.Should().Be(45);
    }

    [Fact]
    public async Task DeductStockForReceiptAsync_ShouldSkipNonTrackedProducts()
    {
        // Arrange
        var trackedProduct = await CreateTestProductAsync(name: "Tracked", currentStock: 100);
        var nonTrackedProduct = await CreateTestProductAsync(name: "Non-Tracked", currentStock: 100, trackInventory: false);

        var receipt = await CreateTestReceiptAsync(
            (trackedProduct.Id, 10),
            (nonTrackedProduct.Id, 5));

        // Act
        var movements = await _inventoryService.DeductStockForReceiptAsync(receipt);

        // Assert
        movements.Should().HaveCount(1); // Only tracked product

        var trackedInv = await _context.Inventories.FirstAsync(i => i.ProductId == trackedProduct.Id);
        trackedInv.CurrentStock.Should().Be(90);

        var nonTrackedInv = await _context.Inventories.FirstAsync(i => i.ProductId == nonTrackedProduct.Id);
        nonTrackedInv.CurrentStock.Should().Be(100); // Unchanged
    }

    [Fact]
    public async Task DeductStockForReceiptAsync_ShouldReturnEmptyList_WhenNoItems()
    {
        // Arrange
        var receipt = new Receipt
        {
            ReceiptNumber = "R-EMPTY",
            OwnerId = TestUserId,
            Status = ReceiptStatus.Pending,
            ReceiptItems = new List<ReceiptItem>()
        };

        // Act
        var movements = await _inventoryService.DeductStockForReceiptAsync(receipt);

        // Assert
        movements.Should().BeEmpty();
    }

    #endregion

    #region RestoreStockForVoidAsync Tests

    [Fact]
    public async Task RestoreStockForVoidAsync_ShouldRestoreAllItems()
    {
        // Arrange
        var product1 = await CreateTestProductAsync(name: "Product 1", currentStock: 90);
        var product2 = await CreateTestProductAsync(name: "Product 2", currentStock: 45);

        var receipt = await CreateTestReceiptAsync(
            (product1.Id, 10),
            (product2.Id, 5));

        // Act
        var movements = await _inventoryService.RestoreStockForVoidAsync(receipt);

        // Assert
        movements.Should().HaveCount(2);

        var inv1 = await _context.Inventories.FirstAsync(i => i.ProductId == product1.Id);
        inv1.CurrentStock.Should().Be(100); // 90 + 10

        var inv2 = await _context.Inventories.FirstAsync(i => i.ProductId == product2.Id);
        inv2.CurrentStock.Should().Be(50); // 45 + 5
    }

    [Fact]
    public async Task RestoreStockForVoidAsync_ShouldBeIdempotent()
    {
        // Arrange
        var product = await CreateTestProductAsync(currentStock: 90);
        var receipt = await CreateTestReceiptAsync((product.Id, 10));

        // First restore
        var movements1 = await _inventoryService.RestoreStockForVoidAsync(receipt);

        // Act - Second restore attempt
        var movements2 = await _inventoryService.RestoreStockForVoidAsync(receipt);

        // Assert
        movements1.Should().HaveCount(1);
        movements2.Should().HaveCount(1); // Returns existing movements

        // Stock should only be restored once
        var inventory = await _context.Inventories.FirstAsync(i => i.ProductId == product.Id);
        inventory.CurrentStock.Should().Be(100); // 90 + 10, not 110
    }

    #endregion

    #region GetLowStockProductsAsync Tests

    [Fact]
    public async Task GetLowStockProductsAsync_ShouldReturnProductsBelowMinLevel()
    {
        // Arrange
        var lowStockProduct = await CreateTestProductAsync(name: "Low Stock", currentStock: 5, minStockLevel: 10);
        var normalStockProduct = await CreateTestProductAsync(name: "Normal Stock", currentStock: 50, minStockLevel: 10);

        // Act
        var lowStockProducts = await _inventoryService.GetLowStockProductsAsync();

        // Assert
        lowStockProducts.Should().HaveCount(1);
        lowStockProducts.First().Name.Should().Be("Low Stock");
    }

    [Fact]
    public async Task GetLowStockProductsAsync_ShouldNotIncludeOutOfStock()
    {
        // Arrange - Out of stock products have CurrentStock <= 0
        await CreateTestProductAsync(name: "Out of Stock", currentStock: 0, minStockLevel: 10);
        var lowStockProduct = await CreateTestProductAsync(name: "Low Stock", currentStock: 5, minStockLevel: 10);

        // Act
        var lowStockProducts = await _inventoryService.GetLowStockProductsAsync();

        // Assert
        lowStockProducts.Should().HaveCount(1);
        lowStockProducts.First().Name.Should().Be("Low Stock");
    }

    #endregion

    #region GetOutOfStockProductsAsync Tests

    [Fact]
    public async Task GetOutOfStockProductsAsync_ShouldReturnProductsWithZeroStock()
    {
        // Arrange
        var outOfStockProduct = await CreateTestProductAsync(name: "Out of Stock", currentStock: 0);
        var inStockProduct = await CreateTestProductAsync(name: "In Stock", currentStock: 50);

        // Act
        var outOfStockProducts = await _inventoryService.GetOutOfStockProductsAsync();

        // Assert
        outOfStockProducts.Should().HaveCount(1);
        outOfStockProducts.First().Name.Should().Be("Out of Stock");
    }

    #endregion

    #region GetStockLevelAsync Tests

    [Fact]
    public async Task GetStockLevelAsync_ShouldReturnCurrentStock()
    {
        // Arrange
        var product = await CreateTestProductAsync(currentStock: 75);

        // Act
        var stockLevel = await _inventoryService.GetStockLevelAsync(product.Id);

        // Assert
        stockLevel.Should().Be(75);
    }

    [Fact]
    public async Task GetStockLevelAsync_ShouldReturnZero_WhenNoInventoryExists()
    {
        // Act
        var stockLevel = await _inventoryService.GetStockLevelAsync(99999);

        // Assert
        stockLevel.Should().Be(0);
    }

    #endregion

    #region CheckAvailabilityAsync Tests

    [Fact]
    public async Task CheckAvailabilityAsync_ShouldReturnTrue_WhenSufficientStock()
    {
        // Arrange
        var product = await CreateTestProductAsync(currentStock: 100);

        // Act
        var isAvailable = await _inventoryService.CheckAvailabilityAsync(product.Id, 50);

        // Assert
        isAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task CheckAvailabilityAsync_ShouldReturnFalse_WhenInsufficientStock()
    {
        // Arrange
        var product = await CreateTestProductAsync(currentStock: 10);

        // Act
        var isAvailable = await _inventoryService.CheckAvailabilityAsync(product.Id, 50);

        // Assert
        isAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task CheckAvailabilityAsync_ShouldReturnTrue_WhenTrackingDisabled()
    {
        // Arrange
        var product = await CreateTestProductAsync(trackInventory: false, currentStock: 0);

        // Act
        var isAvailable = await _inventoryService.CheckAvailabilityAsync(product.Id, 1000);

        // Assert
        isAvailable.Should().BeTrue(); // Always available when tracking disabled
    }

    #endregion

    #region GetStockMovementsAsync Tests

    [Fact]
    public async Task GetStockMovementsAsync_ShouldReturnMovementsForProduct()
    {
        // Arrange
        var product = await CreateTestProductAsync(currentStock: 100);
        await _inventoryService.DeductStockAsync(product.Id, 10, "Sale 1");
        await _inventoryService.DeductStockAsync(product.Id, 20, "Sale 2");

        // Act
        var movements = await _inventoryService.GetStockMovementsAsync(product.Id);

        // Assert
        movements.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetStockMovementsAsync_ShouldFilterByDateRange()
    {
        // Arrange
        var product = await CreateTestProductAsync(currentStock: 100);
        await _inventoryService.DeductStockAsync(product.Id, 10, "Sale 1");

        var startDate = DateTime.UtcNow.AddMinutes(-1);
        var endDate = DateTime.UtcNow.AddMinutes(1);

        // Act
        var movements = await _inventoryService.GetStockMovementsAsync(product.Id, startDate, endDate);

        // Assert
        movements.Should().HaveCount(1);
    }

    #endregion
}
