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
/// Unit tests for the GoodsReceivingService class.
/// Tests cover goods receiving with PO and direct receiving without PO.
/// </summary>
public class GoodsReceivingServiceTests : IDisposable
{
    private readonly POSDbContext _context;
    private readonly Mock<IInventoryService> _inventoryServiceMock;
    private readonly Mock<ISessionService> _sessionServiceMock;
    private readonly Mock<ILogger> _loggerMock;
    private readonly GoodsReceivingService _goodsReceivingService;
    private const int TestUserId = 1;

    public GoodsReceivingServiceTests()
    {
        var options = new DbContextOptionsBuilder<POSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new POSDbContext(options);
        _inventoryServiceMock = new Mock<IInventoryService>();
        _sessionServiceMock = new Mock<ISessionService>();
        _loggerMock = new Mock<ILogger>();

        _sessionServiceMock.Setup(s => s.CurrentUserId).Returns(TestUserId);

        _goodsReceivingService = new GoodsReceivingService(
            _context,
            _inventoryServiceMock.Object,
            _sessionServiceMock.Object,
            _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Test Data Helpers

    private async Task<User> CreateTestUserAsync()
    {
        var user = new User
        {
            Username = "testuser",
            PasswordHash = "hash",
            DisplayName = "Test User",
            IsActive = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private async Task<Supplier> CreateTestSupplierAsync(string name = "Test Supplier")
    {
        var supplier = new Supplier
        {
            Code = $"SUP-{Guid.NewGuid():N}".Substring(0, 10),
            Name = name,
            IsActive = true,
            PaymentTermDays = 30
        };
        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();
        return supplier;
    }

    private async Task<Product> CreateTestProductAsync(string name = "Test Product", decimal costPrice = 100m)
    {
        var product = new Product
        {
            Code = $"PROD-{Guid.NewGuid():N}".Substring(0, 15),
            Name = name,
            SellingPrice = 150m,
            CostPrice = costPrice,
            TrackInventory = true,
            IsActive = true
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var inventory = new Inventory
        {
            ProductId = product.Id,
            CurrentStock = 50,
            ReservedStock = 0,
            LastUpdated = DateTime.UtcNow
        };
        _context.Inventories.Add(inventory);
        await _context.SaveChangesAsync();

        return product;
    }

    private async Task<PurchaseOrder> CreateTestPurchaseOrderAsync(Supplier supplier, params Product[] products)
    {
        var user = await CreateTestUserAsync();
        var po = new PurchaseOrder
        {
            PONumber = $"PO-{DateTime.UtcNow:yyyyMMdd}-001",
            SupplierId = supplier.Id,
            OrderDate = DateTime.UtcNow,
            Status = PurchaseOrderStatus.Sent,
            CreatedByUserId = user.Id,
            IsActive = true
        };

        foreach (var product in products)
        {
            po.PurchaseOrderItems.Add(new PurchaseOrderItem
            {
                ProductId = product.Id,
                OrderedQuantity = 10,
                ReceivedQuantity = 0,
                UnitCost = product.CostPrice ?? 100m,
                TotalCost = 10 * (product.CostPrice ?? 100m),
                IsActive = true
            });
        }

        _context.PurchaseOrders.Add(po);
        await _context.SaveChangesAsync();
        return po;
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new GoodsReceivingService(
            null!,
            _inventoryServiceMock.Object,
            _sessionServiceMock.Object,
            _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public void Constructor_WithNullInventoryService_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new GoodsReceivingService(
            _context,
            null!,
            _sessionServiceMock.Object,
            _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("inventoryService");
    }

    [Fact]
    public void Constructor_WithNullSessionService_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new GoodsReceivingService(
            _context,
            _inventoryServiceMock.Object,
            null!,
            _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("sessionService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new GoodsReceivingService(
            _context,
            _inventoryServiceMock.Object,
            _sessionServiceMock.Object,
            null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region GenerateGRNNumberAsync Tests

    [Fact]
    public async Task GenerateGRNNumberAsync_ShouldGenerateCorrectFormat()
    {
        // Act
        var grnNumber = await _goodsReceivingService.GenerateGRNNumberAsync();

        // Assert
        grnNumber.Should().StartWith("GRN-");
        grnNumber.Should().MatchRegex(@"GRN-\d{8}-\d{3}");
    }

    [Fact]
    public async Task GenerateGRNNumberAsync_ShouldIncrementSequence()
    {
        // Arrange - Create an existing GRN
        var user = await CreateTestUserAsync();
        var today = DateTime.UtcNow.Date;
        var existingGRN = new GoodsReceivedNote
        {
            GRNNumber = $"GRN-{today:yyyyMMdd}-001",
            ReceivedDate = DateTime.UtcNow,
            TotalAmount = 100,
            ReceivedByUserId = user.Id,
            IsActive = true
        };
        _context.GoodsReceivedNotes.Add(existingGRN);
        await _context.SaveChangesAsync();

        // Act
        var grnNumber = await _goodsReceivingService.GenerateGRNNumberAsync();

        // Assert
        grnNumber.Should().EndWith("-002");
    }

    #endregion

    #region GetPendingPurchaseOrdersAsync Tests

    [Fact]
    public async Task GetPendingPurchaseOrdersAsync_ShouldReturnSentPurchaseOrders()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync();
        var product = await CreateTestProductAsync();
        var po = await CreateTestPurchaseOrderAsync(supplier, product);

        // Act
        var pendingPOs = await _goodsReceivingService.GetPendingPurchaseOrdersAsync();

        // Assert
        pendingPOs.Should().HaveCount(1);
        pendingPOs.First().PONumber.Should().Be(po.PONumber);
    }

    [Fact]
    public async Task GetPendingPurchaseOrdersAsync_ShouldReturnPartiallyReceivedPurchaseOrders()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync();
        var product = await CreateTestProductAsync();
        var po = await CreateTestPurchaseOrderAsync(supplier, product);
        po.Status = PurchaseOrderStatus.PartiallyReceived;
        await _context.SaveChangesAsync();

        // Act
        var pendingPOs = await _goodsReceivingService.GetPendingPurchaseOrdersAsync();

        // Assert
        pendingPOs.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetPendingPurchaseOrdersAsync_ShouldNotReturnCompletedOrCancelledOrders()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync();
        var product = await CreateTestProductAsync();

        var completePO = await CreateTestPurchaseOrderAsync(supplier, product);
        completePO.Status = PurchaseOrderStatus.Complete;

        // Create cancelled PO
        var user = await _context.Users.FirstAsync();
        var cancelledPO = new PurchaseOrder
        {
            PONumber = "PO-CANCELLED",
            SupplierId = supplier.Id,
            OrderDate = DateTime.UtcNow,
            Status = PurchaseOrderStatus.Cancelled,
            CreatedByUserId = user.Id,
            IsActive = true
        };
        _context.PurchaseOrders.Add(cancelledPO);
        await _context.SaveChangesAsync();

        // Act
        var pendingPOs = await _goodsReceivingService.GetPendingPurchaseOrdersAsync();

        // Assert
        pendingPOs.Should().BeEmpty();
    }

    #endregion

    #region ReceiveGoodsAsync Tests

    [Fact]
    public async Task ReceiveGoodsAsync_ShouldCreateGRN()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync();
        var product = await CreateTestProductAsync();
        var po = await CreateTestPurchaseOrderAsync(supplier, product);
        var poItem = po.PurchaseOrderItems.First();

        var items = new[]
        {
            new GRNItemInput
            {
                PurchaseOrderItemId = poItem.Id,
                ProductId = product.Id,
                OrderedQuantity = 10,
                ReceivedQuantity = 5,
                UnitCost = 100m
            }
        };

        // Act
        var grn = await _goodsReceivingService.ReceiveGoodsAsync(
            po.Id,
            "DN-12345",
            "Test notes",
            items);

        // Assert
        grn.Should().NotBeNull();
        grn.GRNNumber.Should().StartWith("GRN-");
        grn.PurchaseOrderId.Should().Be(po.Id);
        grn.SupplierId.Should().Be(supplier.Id);
        grn.DeliveryNote.Should().Be("DN-12345");
        grn.Notes.Should().Be("Test notes");
        grn.TotalAmount.Should().Be(500m); // 5 * 100
        grn.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task ReceiveGoodsAsync_ShouldCallInventoryService()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync();
        var product = await CreateTestProductAsync();
        var po = await CreateTestPurchaseOrderAsync(supplier, product);
        var poItem = po.PurchaseOrderItems.First();

        var items = new[]
        {
            new GRNItemInput
            {
                PurchaseOrderItemId = poItem.Id,
                ProductId = product.Id,
                OrderedQuantity = 10,
                ReceivedQuantity = 5,
                UnitCost = 100m
            }
        };

        // Act
        await _goodsReceivingService.ReceiveGoodsAsync(po.Id, null, null, items);

        // Assert
        _inventoryServiceMock.Verify(
            s => s.ReceiveStockAsync(
                product.Id,
                5,
                100m,
                It.IsAny<string>(),
                It.IsAny<int?>()),
            Times.Once);
    }

    [Fact]
    public async Task ReceiveGoodsAsync_ShouldUpdatePOReceivedQuantity()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync();
        var product = await CreateTestProductAsync();
        var po = await CreateTestPurchaseOrderAsync(supplier, product);
        var poItem = po.PurchaseOrderItems.First();

        var items = new[]
        {
            new GRNItemInput
            {
                PurchaseOrderItemId = poItem.Id,
                ProductId = product.Id,
                OrderedQuantity = 10,
                ReceivedQuantity = 5,
                UnitCost = 100m
            }
        };

        // Act
        await _goodsReceivingService.ReceiveGoodsAsync(po.Id, null, null, items);

        // Assert
        var updatedPO = await _context.PurchaseOrders
            .Include(p => p.PurchaseOrderItems)
            .FirstAsync(p => p.Id == po.Id);

        updatedPO.PurchaseOrderItems.First().ReceivedQuantity.Should().Be(5);
        updatedPO.Status.Should().Be(PurchaseOrderStatus.PartiallyReceived);
    }

    [Fact]
    public async Task ReceiveGoodsAsync_ShouldSetPOStatusToComplete_WhenFullyReceived()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync();
        var product = await CreateTestProductAsync();
        var po = await CreateTestPurchaseOrderAsync(supplier, product);
        var poItem = po.PurchaseOrderItems.First();

        var items = new[]
        {
            new GRNItemInput
            {
                PurchaseOrderItemId = poItem.Id,
                ProductId = product.Id,
                OrderedQuantity = 10,
                ReceivedQuantity = 10, // Full quantity
                UnitCost = 100m
            }
        };

        // Act
        await _goodsReceivingService.ReceiveGoodsAsync(po.Id, null, null, items);

        // Assert
        var updatedPO = await _context.PurchaseOrders.FirstAsync(p => p.Id == po.Id);
        updatedPO.Status.Should().Be(PurchaseOrderStatus.Complete);
    }

    [Fact]
    public async Task ReceiveGoodsAsync_ShouldThrow_WhenPONotFound()
    {
        // Arrange
        var items = new[]
        {
            new GRNItemInput { ProductId = 1, ReceivedQuantity = 5, UnitCost = 100m }
        };

        // Act
        var action = () => _goodsReceivingService.ReceiveGoodsAsync(99999, null, null, items);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task ReceiveGoodsAsync_ShouldThrow_WhenNoItems()
    {
        // Arrange
        var items = Array.Empty<GRNItemInput>();

        // Act
        var action = () => _goodsReceivingService.ReceiveGoodsAsync(1, null, null, items);

        // Assert
        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*at least one item*");
    }

    #endregion

    #region ReceiveDirectAsync Tests

    [Fact]
    public async Task ReceiveDirectAsync_ShouldCreateGRNWithoutPO()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync();
        var product = await CreateTestProductAsync();

        var items = new[]
        {
            new GRNItemInput
            {
                ProductId = product.Id,
                ReceivedQuantity = 10,
                UnitCost = 100m
            }
        };

        // Act
        var grn = await _goodsReceivingService.ReceiveDirectAsync(
            supplier.Id,
            "DN-DIRECT",
            "Direct receiving notes",
            items);

        // Assert
        grn.Should().NotBeNull();
        grn.GRNNumber.Should().StartWith("GRN-");
        grn.PurchaseOrderId.Should().BeNull();
        grn.SupplierId.Should().Be(supplier.Id);
        grn.IsDirectReceiving.Should().BeTrue();
        grn.TotalAmount.Should().Be(1000m); // 10 * 100
    }

    [Fact]
    public async Task ReceiveDirectAsync_ShouldWorkWithoutSupplier()
    {
        // Arrange
        var product = await CreateTestProductAsync();

        var items = new[]
        {
            new GRNItemInput
            {
                ProductId = product.Id,
                ReceivedQuantity = 5,
                UnitCost = 50m
            }
        };

        // Act
        var grn = await _goodsReceivingService.ReceiveDirectAsync(
            null, // No supplier
            null,
            null,
            items);

        // Assert
        grn.Should().NotBeNull();
        grn.SupplierId.Should().BeNull();
        grn.IsDirectReceiving.Should().BeTrue();
        grn.Notes.Should().Be("Direct Receiving"); // Default notes
    }

    [Fact]
    public async Task ReceiveDirectAsync_ShouldCallInventoryService()
    {
        // Arrange
        var product = await CreateTestProductAsync();

        var items = new[]
        {
            new GRNItemInput
            {
                ProductId = product.Id,
                ReceivedQuantity = 15,
                UnitCost = 75m
            }
        };

        // Act
        await _goodsReceivingService.ReceiveDirectAsync(null, null, null, items);

        // Assert
        _inventoryServiceMock.Verify(
            s => s.ReceiveStockAsync(
                product.Id,
                15,
                75m,
                It.IsAny<string>(),
                It.IsAny<int?>()),
            Times.Once);
    }

    [Fact]
    public async Task ReceiveDirectAsync_ShouldThrow_WhenSupplierNotFound()
    {
        // Arrange
        var product = await CreateTestProductAsync();
        var items = new[]
        {
            new GRNItemInput { ProductId = product.Id, ReceivedQuantity = 5, UnitCost = 100m }
        };

        // Act
        var action = () => _goodsReceivingService.ReceiveDirectAsync(99999, null, null, items);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Supplier*not found*");
    }

    #endregion

    #region SearchProductsAsync Tests

    [Fact]
    public async Task SearchProductsAsync_ShouldFindByName()
    {
        // Arrange
        var product = await CreateTestProductAsync("Test Widget");

        // Act
        var results = await _goodsReceivingService.SearchProductsAsync("Widget");

        // Assert
        results.Should().HaveCount(1);
        results.First().Name.Should().Be("Test Widget");
    }

    [Fact]
    public async Task SearchProductsAsync_ShouldFindByCode()
    {
        // Arrange
        var product = await CreateTestProductAsync();

        // Act
        var results = await _goodsReceivingService.SearchProductsAsync(product.Code.Substring(0, 8));

        // Assert
        results.Should().ContainSingle();
    }

    [Fact]
    public async Task SearchProductsAsync_ShouldReturnEmpty_WhenSearchTooShort()
    {
        // Arrange
        await CreateTestProductAsync("Test");

        // Act
        var results = await _goodsReceivingService.SearchProductsAsync("T");

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchProductsAsync_ShouldLimitResults()
    {
        // Arrange
        for (int i = 0; i < 20; i++)
        {
            await CreateTestProductAsync($"Widget {i}");
        }

        // Act
        var results = await _goodsReceivingService.SearchProductsAsync("Widget", maxResults: 5);

        // Assert
        results.Should().HaveCount(5);
    }

    #endregion

    #region GetByIdAsync and GetByNumberAsync Tests

    [Fact]
    public async Task GetByIdAsync_ShouldReturnGRNWithDetails()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var product = await CreateTestProductAsync();

        var grn = new GoodsReceivedNote
        {
            GRNNumber = "GRN-TEST-001",
            ReceivedDate = DateTime.UtcNow,
            TotalAmount = 500m,
            ReceivedByUserId = user.Id,
            IsActive = true
        };
        grn.Items.Add(new GRNItem
        {
            ProductId = product.Id,
            ReceivedQuantity = 5,
            UnitCost = 100m,
            TotalCost = 500m,
            IsActive = true
        });
        _context.GoodsReceivedNotes.Add(grn);
        await _context.SaveChangesAsync();

        // Act
        var result = await _goodsReceivingService.GetByIdAsync(grn.Id);

        // Assert
        result.Should().NotBeNull();
        result!.GRNNumber.Should().Be("GRN-TEST-001");
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByNumberAsync_ShouldReturnGRN()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var grn = new GoodsReceivedNote
        {
            GRNNumber = "GRN-TEST-002",
            ReceivedDate = DateTime.UtcNow,
            TotalAmount = 100m,
            ReceivedByUserId = user.Id,
            IsActive = true
        };
        _context.GoodsReceivedNotes.Add(grn);
        await _context.SaveChangesAsync();

        // Act
        var result = await _goodsReceivingService.GetByNumberAsync("GRN-TEST-002");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(grn.Id);
    }

    #endregion

    #region GetActiveSuppliersAsync Tests

    [Fact]
    public async Task GetActiveSuppliersAsync_ShouldReturnOnlyActiveSuppliers()
    {
        // Arrange
        var activeSupplier = await CreateTestSupplierAsync("Active Supplier");
        var inactiveSupplier = new Supplier
        {
            Code = "SUP-INACTIVE",
            Name = "Inactive Supplier",
            IsActive = false
        };
        _context.Suppliers.Add(inactiveSupplier);
        await _context.SaveChangesAsync();

        // Act
        var suppliers = await _goodsReceivingService.GetActiveSuppliersAsync();

        // Assert
        suppliers.Should().ContainSingle();
        suppliers.First().Name.Should().Be("Active Supplier");
    }

    #endregion
}
