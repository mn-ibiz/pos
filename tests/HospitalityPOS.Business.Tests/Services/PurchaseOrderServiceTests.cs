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
/// Unit tests for the PurchaseOrderService class.
/// Tests cover purchase order lifecycle, items management, and status transitions.
/// </summary>
public class PurchaseOrderServiceTests : IAsyncLifetime, IDisposable
{
    private readonly POSDbContext _context;
    private readonly Mock<ILogger> _loggerMock;
    private readonly PurchaseOrderService _purchaseOrderService;
    private const int TestUserId = 1;

    public PurchaseOrderServiceTests()
    {
        var options = new DbContextOptionsBuilder<POSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new POSDbContext(options);
        _loggerMock = new Mock<ILogger>();

        _purchaseOrderService = new PurchaseOrderService(_context, _loggerMock.Object);
    }

    public async Task InitializeAsync()
    {
        // Seed test user
        await SeedTestUserAsync();
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

    #region Helper Methods

    private async Task SeedTestUserAsync()
    {
        var user = new User
        {
            Id = TestUserId,
            Username = "testuser",
            FullName = "Test User",
            PasswordHash = "hash",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }

    private async Task<Supplier> CreateTestSupplierAsync(
        string code = "SUP-0001",
        string name = "Test Supplier")
    {
        var supplier = new Supplier
        {
            Code = code,
            Name = name,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = TestUserId
        };

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        return supplier;
    }

    private async Task<Product> CreateTestProductAsync(
        string name = "Test Product",
        decimal costPrice = 100.00m)
    {
        var category = new Category
        {
            Name = "Test Category",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = TestUserId
        };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var product = new Product
        {
            Name = name,
            CostPrice = costPrice,
            SellingPrice = costPrice * 1.3m,
            CategoryId = category.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = TestUserId
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return product;
    }

    private async Task<PurchaseOrder> CreateTestPurchaseOrderAsync(
        int supplierId,
        PurchaseOrderStatus status = PurchaseOrderStatus.Draft,
        string? poNumber = null,
        DateTime? orderDate = null)
    {
        var po = new PurchaseOrder
        {
            PONumber = poNumber ?? $"PO-{DateTime.UtcNow:yyyyMMdd}-0001",
            SupplierId = supplierId,
            OrderDate = orderDate ?? DateTime.UtcNow,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = TestUserId
        };

        _context.PurchaseOrders.Add(po);
        await _context.SaveChangesAsync();

        return po;
    }

    private async Task<PurchaseOrderItem> AddItemToPurchaseOrderAsync(
        int purchaseOrderId,
        int productId,
        decimal orderedQuantity = 10,
        decimal unitCost = 100.00m)
    {
        var item = new PurchaseOrderItem
        {
            PurchaseOrderId = purchaseOrderId,
            ProductId = productId,
            OrderedQuantity = orderedQuantity,
            UnitCost = unitCost,
            TotalCost = orderedQuantity * unitCost
        };

        _context.PurchaseOrderItems.Add(item);
        await _context.SaveChangesAsync();

        return item;
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new PurchaseOrderService(null!, _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new PurchaseOrderService(_context, null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region GetAllPurchaseOrdersAsync Tests

    [Fact]
    public async Task GetAllPurchaseOrdersAsync_WithNoPOs_ShouldReturnEmptyList()
    {
        // Act
        var result = await _purchaseOrderService.GetAllPurchaseOrdersAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllPurchaseOrdersAsync_ShouldReturnOrderedByDateDescending()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync();
        await CreateTestPurchaseOrderAsync(supplier.Id, orderDate: DateTime.UtcNow.AddDays(-2), poNumber: "PO-OLD");
        await CreateTestPurchaseOrderAsync(supplier.Id, orderDate: DateTime.UtcNow, poNumber: "PO-NEW");
        await CreateTestPurchaseOrderAsync(supplier.Id, orderDate: DateTime.UtcNow.AddDays(-1), poNumber: "PO-MID");

        // Act
        var result = await _purchaseOrderService.GetAllPurchaseOrdersAsync();

        // Assert
        result.Should().HaveCount(3);
        result[0].PONumber.Should().Be("PO-NEW");
        result[2].PONumber.Should().Be("PO-OLD");
    }

    [Fact]
    public async Task GetAllPurchaseOrdersAsync_IncludeItems_ShouldLoadItems()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync();
        var product = await CreateTestProductAsync();
        var po = await CreateTestPurchaseOrderAsync(supplier.Id);
        await AddItemToPurchaseOrderAsync(po.Id, product.Id);

        // Act
        var result = await _purchaseOrderService.GetAllPurchaseOrdersAsync(includeItems: true);

        // Assert
        result.Should().HaveCount(1);
        result[0].PurchaseOrderItems.Should().HaveCount(1);
    }

    #endregion

    #region GetPurchaseOrdersByStatusAsync Tests

    [Fact]
    public async Task GetPurchaseOrdersByStatusAsync_ShouldFilterByStatus()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync();
        await CreateTestPurchaseOrderAsync(supplier.Id, PurchaseOrderStatus.Draft, "PO-DRAFT");
        await CreateTestPurchaseOrderAsync(supplier.Id, PurchaseOrderStatus.Sent, "PO-SENT");
        await CreateTestPurchaseOrderAsync(supplier.Id, PurchaseOrderStatus.Complete, "PO-COMPLETE");

        // Act
        var draftPOs = await _purchaseOrderService.GetPurchaseOrdersByStatusAsync(PurchaseOrderStatus.Draft);
        var sentPOs = await _purchaseOrderService.GetPurchaseOrdersByStatusAsync(PurchaseOrderStatus.Sent);

        // Assert
        draftPOs.Should().HaveCount(1);
        draftPOs[0].PONumber.Should().Be("PO-DRAFT");
        sentPOs.Should().HaveCount(1);
        sentPOs[0].PONumber.Should().Be("PO-SENT");
    }

    #endregion

    #region GetPurchaseOrdersBySupplierAsync Tests

    [Fact]
    public async Task GetPurchaseOrdersBySupplierAsync_ShouldFilterBySupplier()
    {
        // Arrange
        var supplier1 = await CreateTestSupplierAsync("SUP-001", "Supplier One");
        var supplier2 = await CreateTestSupplierAsync("SUP-002", "Supplier Two");
        await CreateTestPurchaseOrderAsync(supplier1.Id, poNumber: "PO-SUP1-1");
        await CreateTestPurchaseOrderAsync(supplier1.Id, poNumber: "PO-SUP1-2");
        await CreateTestPurchaseOrderAsync(supplier2.Id, poNumber: "PO-SUP2-1");

        // Act
        var result = await _purchaseOrderService.GetPurchaseOrdersBySupplierAsync(supplier1.Id);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(po => po.SupplierId.Should().Be(supplier1.Id));
    }

    #endregion

    #region GetPurchaseOrderByIdAsync Tests

    [Fact]
    public async Task GetPurchaseOrderByIdAsync_WithValidId_ShouldReturn()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync();
        var po = await CreateTestPurchaseOrderAsync(supplier.Id, poNumber: "PO-TEST");

        // Act
        var result = await _purchaseOrderService.GetPurchaseOrderByIdAsync(po.Id);

        // Assert
        result.Should().NotBeNull();
        result!.PONumber.Should().Be("PO-TEST");
    }

    [Fact]
    public async Task GetPurchaseOrderByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var result = await _purchaseOrderService.GetPurchaseOrderByIdAsync(99999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetPurchaseOrderByNumberAsync Tests

    [Fact]
    public async Task GetPurchaseOrderByNumberAsync_WithValidNumber_ShouldReturn()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync();
        await CreateTestPurchaseOrderAsync(supplier.Id, poNumber: "PO-UNIQUE-123");

        // Act
        var result = await _purchaseOrderService.GetPurchaseOrderByNumberAsync("PO-UNIQUE-123");

        // Assert
        result.Should().NotBeNull();
        result!.PONumber.Should().Be("PO-UNIQUE-123");
    }

    [Fact]
    public async Task GetPurchaseOrderByNumberAsync_WithNullNumber_ShouldReturnNull()
    {
        // Act
        var result = await _purchaseOrderService.GetPurchaseOrderByNumberAsync(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPurchaseOrderByNumberAsync_WithEmptyNumber_ShouldReturnNull()
    {
        // Act
        var result = await _purchaseOrderService.GetPurchaseOrderByNumberAsync("");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreatePurchaseOrderAsync Tests

    [Fact]
    public async Task CreatePurchaseOrderAsync_WithValidData_ShouldCreate()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync();
        var po = new PurchaseOrder
        {
            SupplierId = supplier.Id,
            OrderDate = DateTime.UtcNow
        };

        // Act
        var result = await _purchaseOrderService.CreatePurchaseOrderAsync(po, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Status.Should().Be(PurchaseOrderStatus.Draft);
        result.PONumber.Should().NotBeNullOrEmpty();
        result.CreatedByUserId.Should().Be(TestUserId);
    }

    [Fact]
    public async Task CreatePurchaseOrderAsync_ShouldGeneratePONumber()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync();
        var orderDate = new DateTime(2025, 6, 15);
        var po = new PurchaseOrder
        {
            SupplierId = supplier.Id,
            OrderDate = orderDate
        };

        // Act
        var result = await _purchaseOrderService.CreatePurchaseOrderAsync(po, TestUserId);

        // Assert
        result.PONumber.Should().StartWith("PO-20250615-");
    }

    [Fact]
    public async Task CreatePurchaseOrderAsync_WithItems_ShouldCalculateTotals()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync();
        var product = await CreateTestProductAsync();
        var po = new PurchaseOrder
        {
            SupplierId = supplier.Id,
            OrderDate = DateTime.UtcNow,
            PurchaseOrderItems =
            [
                new PurchaseOrderItem { ProductId = product.Id, OrderedQuantity = 10, UnitCost = 100 }
            ]
        };

        // Act
        var result = await _purchaseOrderService.CreatePurchaseOrderAsync(po, TestUserId);

        // Assert
        result.SubTotal.Should().Be(1000); // 10 * 100
        result.TaxAmount.Should().Be(160); // 16% of 1000
        result.TotalAmount.Should().Be(1160); // 1000 + 160
    }

    [Fact]
    public async Task CreatePurchaseOrderAsync_WithNullPO_ShouldThrow()
    {
        // Act
        var action = () => _purchaseOrderService.CreatePurchaseOrderAsync(null!, TestUserId);

        // Assert
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region UpdatePurchaseOrderAsync Tests

    [Fact]
    public async Task UpdatePurchaseOrderAsync_WithDraftPO_ShouldUpdate()
    {
        // Arrange
        var supplier1 = await CreateTestSupplierAsync("SUP-001", "Original Supplier");
        var supplier2 = await CreateTestSupplierAsync("SUP-002", "New Supplier");
        var po = await CreateTestPurchaseOrderAsync(supplier1.Id, PurchaseOrderStatus.Draft);

        var updatedPO = new PurchaseOrder
        {
            Id = po.Id,
            SupplierId = supplier2.Id,
            OrderDate = DateTime.UtcNow.AddDays(1),
            Notes = "Updated notes"
        };

        // Act
        var result = await _purchaseOrderService.UpdatePurchaseOrderAsync(updatedPO);

        // Assert
        result.SupplierId.Should().Be(supplier2.Id);
        result.Notes.Should().Be("Updated notes");
    }

    [Fact]
    public async Task UpdatePurchaseOrderAsync_WithNonDraftPO_ShouldThrow()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync();
        var po = await CreateTestPurchaseOrderAsync(supplier.Id, PurchaseOrderStatus.Sent);

        var updatedPO = new PurchaseOrder
        {
            Id = po.Id,
            SupplierId = supplier.Id,
            OrderDate = DateTime.UtcNow
        };

        // Act
        var action = () => _purchaseOrderService.UpdatePurchaseOrderAsync(updatedPO);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot modify*");
    }

    [Fact]
    public async Task UpdatePurchaseOrderAsync_WithNonExistentId_ShouldThrow()
    {
        // Arrange
        var updatedPO = new PurchaseOrder
        {
            Id = 99999,
            SupplierId = 1,
            OrderDate = DateTime.UtcNow
        };

        // Act
        var action = () => _purchaseOrderService.UpdatePurchaseOrderAsync(updatedPO);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    #endregion

    #region AddItemAsync Tests

    [Fact]
    public async Task AddItemAsync_ToDraftPO_ShouldAdd()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync();
        var product = await CreateTestProductAsync();
        var po = await CreateTestPurchaseOrderAsync(supplier.Id, PurchaseOrderStatus.Draft);

        var item = new PurchaseOrderItem
        {
            ProductId = product.Id,
            OrderedQuantity = 5,
            UnitCost = 50
        };

        // Act
        var result = await _purchaseOrderService.AddItemAsync(po.Id, item);

        // Assert
        result.Should().NotBeNull();
        result.TotalCost.Should().Be(250); // 5 * 50

        // Verify PO totals updated
        var updatedPO = await _context.PurchaseOrders
            .Include(p => p.PurchaseOrderItems)
            .FirstAsync(p => p.Id == po.Id);
        updatedPO.SubTotal.Should().Be(250);
    }

    [Fact]
    public async Task AddItemAsync_ToNonDraftPO_ShouldThrow()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync();
        var product = await CreateTestProductAsync();
        var po = await CreateTestPurchaseOrderAsync(supplier.Id, PurchaseOrderStatus.Sent);

        var item = new PurchaseOrderItem
        {
            ProductId = product.Id,
            OrderedQuantity = 5,
            UnitCost = 50
        };

        // Act
        var action = () => _purchaseOrderService.AddItemAsync(po.Id, item);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot add items*");
    }

    [Fact]
    public async Task AddItemAsync_ToNonExistentPO_ShouldThrow()
    {
        // Arrange
        var item = new PurchaseOrderItem
        {
            ProductId = 1,
            OrderedQuantity = 5,
            UnitCost = 50
        };

        // Act
        var action = () => _purchaseOrderService.AddItemAsync(99999, item);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    #endregion

    #region UpdateItemAsync Tests

    [Fact]
    public async Task UpdateItemAsync_InDraftPO_ShouldUpdate()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync();
        var product = await CreateTestProductAsync();
        var po = await CreateTestPurchaseOrderAsync(supplier.Id, PurchaseOrderStatus.Draft);
        var item = await AddItemToPurchaseOrderAsync(po.Id, product.Id, 10, 100);

        var updatedItem = new PurchaseOrderItem
        {
            Id = item.Id,
            ProductId = product.Id,
            OrderedQuantity = 20,
            UnitCost = 150,
            Notes = "Updated quantity and price"
        };

        // Act
        var result = await _purchaseOrderService.UpdateItemAsync(updatedItem);

        // Assert
        result.OrderedQuantity.Should().Be(20);
        result.UnitCost.Should().Be(150);
        result.TotalCost.Should().Be(3000); // 20 * 150
    }

    [Fact]
    public async Task UpdateItemAsync_InNonDraftPO_ShouldThrow()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync();
        var product = await CreateTestProductAsync();
        var po = await CreateTestPurchaseOrderAsync(supplier.Id, PurchaseOrderStatus.Sent);
        var item = await AddItemToPurchaseOrderAsync(po.Id, product.Id);

        var updatedItem = new PurchaseOrderItem
        {
            Id = item.Id,
            ProductId = product.Id,
            OrderedQuantity = 20,
            UnitCost = 150
        };

        // Act
        var action = () => _purchaseOrderService.UpdateItemAsync(updatedItem);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot modify items*");
    }

    #endregion

    #region RemoveItemAsync Tests

    [Fact]
    public async Task RemoveItemAsync_FromDraftPO_ShouldRemove()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync();
        var product = await CreateTestProductAsync();
        var po = await CreateTestPurchaseOrderAsync(supplier.Id, PurchaseOrderStatus.Draft);
        var item = await AddItemToPurchaseOrderAsync(po.Id, product.Id);

        // Act
        var result = await _purchaseOrderService.RemoveItemAsync(item.Id);

        // Assert
        result.Should().BeTrue();
        var items = await _context.PurchaseOrderItems.Where(i => i.PurchaseOrderId == po.Id).ToListAsync();
        items.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveItemAsync_FromNonDraftPO_ShouldThrow()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync();
        var product = await CreateTestProductAsync();
        var po = await CreateTestPurchaseOrderAsync(supplier.Id, PurchaseOrderStatus.Sent);
        var item = await AddItemToPurchaseOrderAsync(po.Id, product.Id);

        // Act
        var action = () => _purchaseOrderService.RemoveItemAsync(item.Id);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot remove items*");
    }

    [Fact]
    public async Task RemoveItemAsync_NonExistentItem_ShouldReturnFalse()
    {
        // Act
        var result = await _purchaseOrderService.RemoveItemAsync(99999);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region SendToSupplierAsync Tests

    [Fact]
    public async Task SendToSupplierAsync_WithDraftPOAndItems_ShouldSend()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync();
        var product = await CreateTestProductAsync();
        var po = await CreateTestPurchaseOrderAsync(supplier.Id, PurchaseOrderStatus.Draft);
        await AddItemToPurchaseOrderAsync(po.Id, product.Id);

        // Act
        var result = await _purchaseOrderService.SendToSupplierAsync(po.Id);

        // Assert
        result.Status.Should().Be(PurchaseOrderStatus.Sent);
    }

    [Fact]
    public async Task SendToSupplierAsync_WithNonDraftPO_ShouldThrow()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync();
        var product = await CreateTestProductAsync();
        var po = await CreateTestPurchaseOrderAsync(supplier.Id, PurchaseOrderStatus.Sent);
        await AddItemToPurchaseOrderAsync(po.Id, product.Id);

        // Act
        var action = () => _purchaseOrderService.SendToSupplierAsync(po.Id);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Only Draft POs can be sent*");
    }

    [Fact]
    public async Task SendToSupplierAsync_WithNoItems_ShouldThrow()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync();
        var po = await CreateTestPurchaseOrderAsync(supplier.Id, PurchaseOrderStatus.Draft);

        // Act
        var action = () => _purchaseOrderService.SendToSupplierAsync(po.Id);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no items*");
    }

    [Fact]
    public async Task SendToSupplierAsync_WithNonExistentPO_ShouldThrow()
    {
        // Act
        var action = () => _purchaseOrderService.SendToSupplierAsync(99999);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    #endregion

    #region CancelPurchaseOrderAsync Tests

    [Fact]
    public async Task CancelPurchaseOrderAsync_WithDraftPO_ShouldCancel()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync();
        var po = await CreateTestPurchaseOrderAsync(supplier.Id, PurchaseOrderStatus.Draft);

        // Act
        var result = await _purchaseOrderService.CancelPurchaseOrderAsync(po.Id, "No longer needed");

        // Assert
        result.Status.Should().Be(PurchaseOrderStatus.Cancelled);
        result.Notes.Should().Contain("No longer needed");
    }

    [Fact]
    public async Task CancelPurchaseOrderAsync_WithSentPO_ShouldCancel()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync();
        var po = await CreateTestPurchaseOrderAsync(supplier.Id, PurchaseOrderStatus.Sent);

        // Act
        var result = await _purchaseOrderService.CancelPurchaseOrderAsync(po.Id);

        // Assert
        result.Status.Should().Be(PurchaseOrderStatus.Cancelled);
    }

    [Fact]
    public async Task CancelPurchaseOrderAsync_WithCompletePO_ShouldThrow()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync();
        var po = await CreateTestPurchaseOrderAsync(supplier.Id, PurchaseOrderStatus.Complete);

        // Act
        var action = () => _purchaseOrderService.CancelPurchaseOrderAsync(po.Id);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot cancel a completed*");
    }

    [Fact]
    public async Task CancelPurchaseOrderAsync_AlreadyCancelled_ShouldThrow()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync();
        var po = await CreateTestPurchaseOrderAsync(supplier.Id, PurchaseOrderStatus.Cancelled);

        // Act
        var action = () => _purchaseOrderService.CancelPurchaseOrderAsync(po.Id);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already cancelled*");
    }

    #endregion

    #region GeneratePONumberAsync Tests

    [Fact]
    public async Task GeneratePONumberAsync_FirstOfDay_ShouldReturn0001()
    {
        // Arrange
        var date = new DateTime(2025, 7, 20);

        // Act
        var result = await _purchaseOrderService.GeneratePONumberAsync(date);

        // Assert
        result.Should().Be("PO-20250720-0001");
    }

    [Fact]
    public async Task GeneratePONumberAsync_WithExistingPOs_ShouldIncrement()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync();
        var date = new DateTime(2025, 7, 20);
        await CreateTestPurchaseOrderAsync(supplier.Id, poNumber: "PO-20250720-0001", orderDate: date);
        await CreateTestPurchaseOrderAsync(supplier.Id, poNumber: "PO-20250720-0002", orderDate: date);

        // Act
        var result = await _purchaseOrderService.GeneratePONumberAsync(date);

        // Assert
        result.Should().Be("PO-20250720-0003");
    }

    #endregion

    #region GetCountByStatusAsync Tests

    [Fact]
    public async Task GetCountByStatusAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync();
        await CreateTestPurchaseOrderAsync(supplier.Id, PurchaseOrderStatus.Draft, "PO-1");
        await CreateTestPurchaseOrderAsync(supplier.Id, PurchaseOrderStatus.Draft, "PO-2");
        await CreateTestPurchaseOrderAsync(supplier.Id, PurchaseOrderStatus.Sent, "PO-3");

        // Act
        var draftCount = await _purchaseOrderService.GetCountByStatusAsync(PurchaseOrderStatus.Draft);
        var sentCount = await _purchaseOrderService.GetCountByStatusAsync(PurchaseOrderStatus.Sent);
        var completeCount = await _purchaseOrderService.GetCountByStatusAsync(PurchaseOrderStatus.Complete);

        // Assert
        draftCount.Should().Be(2);
        sentCount.Should().Be(1);
        completeCount.Should().Be(0);
    }

    #endregion

    #region GetPurchaseOrdersByDateRangeAsync Tests

    [Fact]
    public async Task GetPurchaseOrdersByDateRangeAsync_ShouldFilterByDateRange()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync();
        await CreateTestPurchaseOrderAsync(supplier.Id, orderDate: new DateTime(2025, 1, 1), poNumber: "PO-JAN");
        await CreateTestPurchaseOrderAsync(supplier.Id, orderDate: new DateTime(2025, 3, 15), poNumber: "PO-MAR");
        await CreateTestPurchaseOrderAsync(supplier.Id, orderDate: new DateTime(2025, 6, 30), poNumber: "PO-JUN");

        // Act
        var result = await _purchaseOrderService.GetPurchaseOrdersByDateRangeAsync(
            new DateTime(2025, 2, 1),
            new DateTime(2025, 5, 31));

        // Assert
        result.Should().HaveCount(1);
        result[0].PONumber.Should().Be("PO-MAR");
    }

    #endregion

    #region SearchPurchaseOrdersAsync Tests

    [Fact]
    public async Task SearchPurchaseOrdersAsync_ByPONumber_ShouldFindMatches()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync("SUP-001", "Test Supplier");
        await CreateTestPurchaseOrderAsync(supplier.Id, poNumber: "PO-ABC-001");
        await CreateTestPurchaseOrderAsync(supplier.Id, poNumber: "PO-XYZ-001");
        await CreateTestPurchaseOrderAsync(supplier.Id, poNumber: "PO-ABC-002");

        // Act
        var result = await _purchaseOrderService.SearchPurchaseOrdersAsync("ABC");

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchPurchaseOrdersAsync_BySupplierName_ShouldFindMatches()
    {
        // Arrange
        var supplier1 = await CreateTestSupplierAsync("SUP-001", "Metro Distributors");
        var supplier2 = await CreateTestSupplierAsync("SUP-002", "Alpha Traders");
        await CreateTestPurchaseOrderAsync(supplier1.Id, poNumber: "PO-001");
        await CreateTestPurchaseOrderAsync(supplier2.Id, poNumber: "PO-002");

        // Act
        var result = await _purchaseOrderService.SearchPurchaseOrdersAsync("Metro");

        // Assert
        result.Should().HaveCount(1);
        result[0].PONumber.Should().Be("PO-001");
    }

    [Fact]
    public async Task SearchPurchaseOrdersAsync_CaseInsensitive_ShouldWork()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync("SUP-001", "UPPERCASE SUPPLIER");
        await CreateTestPurchaseOrderAsync(supplier.Id, poNumber: "PO-001");

        // Act
        var result = await _purchaseOrderService.SearchPurchaseOrdersAsync("uppercase");

        // Assert
        result.Should().HaveCount(1);
    }

    #endregion

    #region RecalculateTotalsAsync Tests

    [Fact]
    public async Task RecalculateTotalsAsync_ShouldUpdateTotals()
    {
        // Arrange
        var supplier = await CreateTestSupplierAsync();
        var product1 = await CreateTestProductAsync("Product 1");
        var product2 = await CreateTestProductAsync("Product 2");
        var po = await CreateTestPurchaseOrderAsync(supplier.Id);
        await AddItemToPurchaseOrderAsync(po.Id, product1.Id, 10, 100); // 1000
        await AddItemToPurchaseOrderAsync(po.Id, product2.Id, 5, 200);  // 1000

        // Act
        var result = await _purchaseOrderService.RecalculateTotalsAsync(po.Id);

        // Assert
        result.SubTotal.Should().Be(2000);
        result.TaxAmount.Should().Be(320); // 16% of 2000
        result.TotalAmount.Should().Be(2320);
    }

    [Fact]
    public async Task RecalculateTotalsAsync_NonExistentPO_ShouldThrow()
    {
        // Act
        var action = () => _purchaseOrderService.RecalculateTotalsAsync(99999);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    #endregion
}
