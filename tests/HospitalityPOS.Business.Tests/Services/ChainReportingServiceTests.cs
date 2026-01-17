using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Services;
using System.Linq.Expressions;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for the ChainReportingService class.
/// Tests cover dashboard metrics, store comparison, product performance,
/// sales trends, and inventory status reporting.
/// </summary>
public class ChainReportingServiceTests
{
    private readonly Mock<IRepository<Store>> _storeRepositoryMock;
    private readonly Mock<IRepository<Receipt>> _receiptRepositoryMock;
    private readonly Mock<IRepository<ReceiptItem>> _receiptItemRepositoryMock;
    private readonly Mock<IRepository<Product>> _productRepositoryMock;
    private readonly Mock<IRepository<Category>> _categoryRepositoryMock;
    private readonly Mock<IRepository<Inventory>> _inventoryRepositoryMock;
    private readonly Mock<IRepository<Payment>> _paymentRepositoryMock;
    private readonly Mock<ILogger<ChainReportingService>> _loggerMock;
    private readonly ChainReportingService _service;

    public ChainReportingServiceTests()
    {
        _storeRepositoryMock = new Mock<IRepository<Store>>();
        _receiptRepositoryMock = new Mock<IRepository<Receipt>>();
        _receiptItemRepositoryMock = new Mock<IRepository<ReceiptItem>>();
        _productRepositoryMock = new Mock<IRepository<Product>>();
        _categoryRepositoryMock = new Mock<IRepository<Category>>();
        _inventoryRepositoryMock = new Mock<IRepository<Inventory>>();
        _paymentRepositoryMock = new Mock<IRepository<Payment>>();
        _loggerMock = new Mock<ILogger<ChainReportingService>>();

        _service = new ChainReportingService(
            _storeRepositoryMock.Object,
            _receiptRepositoryMock.Object,
            _receiptItemRepositoryMock.Object,
            _productRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _inventoryRepositoryMock.Object,
            _paymentRepositoryMock.Object,
            _loggerMock.Object);
    }

    #region Helper Methods

    private static Store CreateTestStore(
        int id = 1,
        string storeCode = "STR001",
        string name = "Test Store",
        string region = "Nairobi",
        DateTime? lastSyncTime = null,
        bool isActive = true)
    {
        return new Store
        {
            Id = id,
            StoreCode = storeCode,
            Name = name,
            Region = region,
            LastSyncTime = lastSyncTime,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static Receipt CreateTestReceipt(
        int id = 1,
        int? storeId = 1,
        decimal totalAmount = 1000m,
        decimal subTotal = 1000m,
        decimal discountAmount = 0m,
        DateTime? createdAt = null,
        bool isVoid = false,
        bool isActive = true)
    {
        return new Receipt
        {
            Id = id,
            StoreId = storeId,
            TotalAmount = totalAmount,
            SubTotal = subTotal,
            DiscountAmount = discountAmount,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            IsVoid = isVoid,
            IsActive = isActive
        };
    }

    private static ReceiptItem CreateTestReceiptItem(
        int id = 1,
        int receiptId = 1,
        int productId = 1,
        decimal quantity = 1m,
        decimal unitPrice = 100m,
        bool isActive = true)
    {
        return new ReceiptItem
        {
            Id = id,
            ReceiptId = receiptId,
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            TotalPrice = quantity * unitPrice,
            IsActive = isActive
        };
    }

    private static Product CreateTestProduct(
        int id = 1,
        string code = "PROD001",
        string name = "Test Product",
        decimal sellingPrice = 100m,
        decimal? costPrice = 50m,
        int? categoryId = 1,
        bool isActive = true)
    {
        return new Product
        {
            Id = id,
            Code = code,
            Name = name,
            SellingPrice = sellingPrice,
            CostPrice = costPrice,
            CategoryId = categoryId,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static Category CreateTestCategory(
        int id = 1,
        string name = "Test Category",
        bool isActive = true)
    {
        return new Category
        {
            Id = id,
            Name = name,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static Inventory CreateTestInventory(
        int id = 1,
        int storeId = 1,
        int productId = 1,
        decimal currentStock = 100m,
        decimal minimumStock = 10m,
        decimal reorderLevel = 20m,
        bool isActive = true)
    {
        return new Inventory
        {
            Id = id,
            StoreId = storeId,
            ProductId = productId,
            CurrentStock = currentStock,
            MinimumStock = minimumStock,
            ReorderLevel = reorderLevel,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static Payment CreateTestPayment(
        int id = 1,
        int receiptId = 1,
        int paymentMethodId = 1,
        decimal amountPaid = 100m,
        DateTime? paymentDate = null,
        bool isActive = true)
    {
        return new Payment
        {
            Id = id,
            ReceiptId = receiptId,
            PaymentMethodId = paymentMethodId,
            AmountPaid = amountPaid,
            PaymentDate = paymentDate ?? DateTime.UtcNow,
            IsActive = isActive
        };
    }

    private void SetupBasicMocks()
    {
        _storeRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Store, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Store>());

        _receiptRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Receipt, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Receipt>());

        _receiptItemRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<ReceiptItem, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReceiptItem>());

        _productRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Product, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        _categoryRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Category, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category>());

        _inventoryRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Inventory, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Inventory>());

        _paymentRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Payment, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment>());
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullStoreRepository_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var action = () => new ChainReportingService(
            null!,
            _receiptRepositoryMock.Object,
            _receiptItemRepositoryMock.Object,
            _productRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _inventoryRepositoryMock.Object,
            _paymentRepositoryMock.Object,
            _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("storeRepository");
    }

    [Fact]
    public void Constructor_WithNullReceiptRepository_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var action = () => new ChainReportingService(
            _storeRepositoryMock.Object,
            null!,
            _receiptItemRepositoryMock.Object,
            _productRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _inventoryRepositoryMock.Object,
            _paymentRepositoryMock.Object,
            _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("receiptRepository");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var action = () => new ChainReportingService(
            _storeRepositoryMock.Object,
            _receiptRepositoryMock.Object,
            _receiptItemRepositoryMock.Object,
            _productRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _inventoryRepositoryMock.Object,
            _paymentRepositoryMock.Object,
            null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region GetChainDashboardAsync Tests

    [Fact]
    public async Task GetChainDashboardAsync_WithStoresAndReceipts_ReturnsDashboardMetrics()
    {
        // Arrange
        var stores = new List<Store>
        {
            CreateTestStore(1, "STR001", "Store 1", lastSyncTime: DateTime.UtcNow),
            CreateTestStore(2, "STR002", "Store 2", lastSyncTime: DateTime.UtcNow.AddMinutes(-60))
        };

        var receipts = new List<Receipt>
        {
            CreateTestReceipt(1, 1, 1000m, createdAt: DateTime.UtcNow.AddHours(-1)),
            CreateTestReceipt(2, 1, 500m, createdAt: DateTime.UtcNow.AddHours(-2)),
            CreateTestReceipt(3, 2, 750m, createdAt: DateTime.UtcNow.AddDays(-1))
        };

        _storeRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Store, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(stores);

        _receiptRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Receipt, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(receipts);

        _receiptItemRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<ReceiptItem, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReceiptItem>());

        _productRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Product, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        _categoryRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Category, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category>());

        // Act
        var result = await _service.GetChainDashboardAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalStores.Should().Be(2);
        result.OnlineStores.Should().Be(1); // Only store with sync within 30 minutes
        result.OfflineStores.Should().Be(1);
        result.TotalSalesToday.Should().Be(1500m); // Two receipts from today
    }

    [Fact]
    public async Task GetChainDashboardAsync_WithNoData_ReturnsEmptyDashboard()
    {
        // Arrange
        SetupBasicMocks();

        // Act
        var result = await _service.GetChainDashboardAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalStores.Should().Be(0);
        result.TotalSalesToday.Should().Be(0);
        result.TransactionCountToday.Should().Be(0);
    }

    #endregion

    #region GetAllStoresSummaryAsync Tests

    [Fact]
    public async Task GetAllStoresSummaryAsync_ReturnsAllStoreSummaries()
    {
        // Arrange
        var stores = new List<Store>
        {
            CreateTestStore(1, "STR001", "Store 1"),
            CreateTestStore(2, "STR002", "Store 2")
        };

        var receipts = new List<Receipt>
        {
            CreateTestReceipt(1, 1, 1000m, createdAt: DateTime.UtcNow.AddHours(-1)),
            CreateTestReceipt(2, 2, 500m, createdAt: DateTime.UtcNow.AddHours(-2))
        };

        _storeRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Store, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(stores);

        _receiptRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Receipt, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(receipts);

        // Act
        var result = await _service.GetAllStoresSummaryAsync();

        // Assert
        result.Should().HaveCount(2);
        result.First().SalesToday.Should().BeGreaterThan(0);
    }

    #endregion

    #region GetStoreSummaryAsync Tests

    [Fact]
    public async Task GetStoreSummaryAsync_WithValidId_ReturnsStoreSummary()
    {
        // Arrange
        var store = CreateTestStore(1, "STR001", "Test Store");
        var receipts = new List<Receipt>
        {
            CreateTestReceipt(1, 1, 1000m, createdAt: DateTime.UtcNow.AddHours(-1))
        };

        _storeRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(store);

        _receiptRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Receipt, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(receipts);

        // Act
        var result = await _service.GetStoreSummaryAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.StoreId.Should().Be(1);
        result.StoreName.Should().Be("Test Store");
        result.SalesToday.Should().Be(1000m);
    }

    [Fact]
    public async Task GetStoreSummaryAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        _storeRepositoryMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Store?)null);

        // Act
        var result = await _service.GetStoreSummaryAsync(999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetStoreComparisonReportAsync Tests

    [Fact]
    public async Task GetStoreComparisonReportAsync_ReturnsRankedStores()
    {
        // Arrange
        var stores = new List<Store>
        {
            CreateTestStore(1, "STR001", "Store 1"),
            CreateTestStore(2, "STR002", "Store 2"),
            CreateTestStore(3, "STR003", "Store 3")
        };

        var receipts = new List<Receipt>
        {
            CreateTestReceipt(1, 1, 5000m, createdAt: DateTime.UtcNow.AddDays(-5)),
            CreateTestReceipt(2, 2, 3000m, createdAt: DateTime.UtcNow.AddDays(-5)),
            CreateTestReceipt(3, 3, 7000m, createdAt: DateTime.UtcNow.AddDays(-5))
        };

        _storeRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Store, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(stores);

        _receiptRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Receipt, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(receipts);

        // Act
        var result = await _service.GetStoreComparisonReportAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalStores.Should().Be(3);
        result.Rankings.Should().HaveCount(3);
        result.Rankings.First().Rank.Should().Be(1);
        result.Rankings.First().Sales.Should().Be(7000m); // Highest sales
    }

    [Fact]
    public async Task GetStoreComparisonReportAsync_WithDateFilter_ReturnsFilteredResults()
    {
        // Arrange
        var stores = new List<Store> { CreateTestStore(1, "STR001") };

        _storeRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Store, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(stores);

        _receiptRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Receipt, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Receipt>());

        var query = new ChainReportQueryDto
        {
            FromDate = DateTime.UtcNow.AddDays(-7),
            ToDate = DateTime.UtcNow
        };

        // Act
        var result = await _service.GetStoreComparisonReportAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.FromDate.Should().Be(query.FromDate.Value);
        result.ToDate.Should().Be(query.ToDate.Value);
    }

    #endregion

    #region GetTopPerformingStoresAsync Tests

    [Fact]
    public async Task GetTopPerformingStoresAsync_ReturnsTopN()
    {
        // Arrange
        var stores = new List<Store>
        {
            CreateTestStore(1, "STR001"),
            CreateTestStore(2, "STR002"),
            CreateTestStore(3, "STR003")
        };

        var receipts = new List<Receipt>
        {
            CreateTestReceipt(1, 1, 5000m, createdAt: DateTime.UtcNow.AddDays(-1)),
            CreateTestReceipt(2, 2, 3000m, createdAt: DateTime.UtcNow.AddDays(-1)),
            CreateTestReceipt(3, 3, 7000m, createdAt: DateTime.UtcNow.AddDays(-1))
        };

        _storeRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Store, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(stores);

        _receiptRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Receipt, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(receipts);

        // Act
        var result = await _service.GetTopPerformingStoresAsync(2);

        // Assert
        result.Should().HaveCount(2);
        result.First().Sales.Should().BeGreaterOrEqualTo(result.Last().Sales);
    }

    #endregion

    #region GetProductPerformanceReportAsync Tests

    [Fact]
    public async Task GetProductPerformanceReportAsync_ReturnsProductMetrics()
    {
        // Arrange
        var stores = new List<Store> { CreateTestStore(1, "STR001") };
        var products = new List<Product>
        {
            CreateTestProduct(1, "PROD1", "Product 1"),
            CreateTestProduct(2, "PROD2", "Product 2")
        };
        var categories = new List<Category> { CreateTestCategory(1, "Category 1") };
        var receipts = new List<Receipt>
        {
            CreateTestReceipt(1, 1, 1000m, createdAt: DateTime.UtcNow.AddDays(-1))
        };
        var receiptItems = new List<ReceiptItem>
        {
            CreateTestReceiptItem(1, 1, 1, 5, 100m),
            CreateTestReceiptItem(2, 1, 2, 3, 150m)
        };

        _storeRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Store, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(stores);

        _receiptRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Receipt, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(receipts);

        _receiptItemRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<ReceiptItem, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiptItems);

        _productRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Product, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        _categoryRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Category, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        // Act
        var result = await _service.GetProductPerformanceReportAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalProducts.Should().Be(2);
        result.Products.Should().NotBeEmpty();
    }

    #endregion

    #region GetTopSellingProductsAsync Tests

    [Fact]
    public async Task GetTopSellingProductsAsync_ReturnsTopProducts()
    {
        // Arrange
        var products = new List<Product>
        {
            CreateTestProduct(1, "PROD1", "Product 1"),
            CreateTestProduct(2, "PROD2", "Product 2")
        };
        var receipts = new List<Receipt>
        {
            CreateTestReceipt(1, 1, 1000m, createdAt: DateTime.UtcNow.AddDays(-1))
        };
        var receiptItems = new List<ReceiptItem>
        {
            CreateTestReceiptItem(1, 1, 1, 10, 100m),
            CreateTestReceiptItem(2, 1, 2, 5, 100m)
        };

        _storeRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Store, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Store>());

        _receiptRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Receipt, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(receipts);

        _receiptItemRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<ReceiptItem, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiptItems);

        _productRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Product, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        _categoryRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Category, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category>());

        // Act
        var result = await _service.GetTopSellingProductsAsync(5);

        // Assert
        result.Should().NotBeEmpty();
        result.First().TotalQuantitySold.Should().BeGreaterOrEqualTo(result.Last().TotalQuantitySold);
    }

    #endregion

    #region GetCategoryPerformanceReportAsync Tests

    [Fact]
    public async Task GetCategoryPerformanceReportAsync_ReturnsCategoryMetrics()
    {
        // Arrange
        var categories = new List<Category>
        {
            CreateTestCategory(1, "Electronics"),
            CreateTestCategory(2, "Beverages")
        };
        var products = new List<Product>
        {
            CreateTestProduct(1, categoryId: 1),
            CreateTestProduct(2, categoryId: 2)
        };
        var receipts = new List<Receipt>
        {
            CreateTestReceipt(1, 1, 1000m, createdAt: DateTime.UtcNow.AddDays(-1))
        };
        var receiptItems = new List<ReceiptItem>
        {
            CreateTestReceiptItem(1, 1, 1, 5, 100m),
            CreateTestReceiptItem(2, 1, 2, 3, 50m)
        };

        _storeRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Store, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Store>());

        _receiptRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Receipt, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(receipts);

        _receiptItemRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<ReceiptItem, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiptItems);

        _productRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Product, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        _categoryRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Category, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        // Act
        var result = await _service.GetCategoryPerformanceReportAsync();

        // Assert
        result.Should().NotBeNull();
        result.Categories.Should().NotBeEmpty();
    }

    #endregion

    #region GetSalesTrendReportAsync Tests

    [Fact]
    public async Task GetSalesTrendReportAsync_ReturnsTrandData()
    {
        // Arrange
        var receipts = new List<Receipt>
        {
            CreateTestReceipt(1, 1, 1000m, createdAt: DateTime.UtcNow.AddDays(-1)),
            CreateTestReceipt(2, 1, 1500m, createdAt: DateTime.UtcNow.AddDays(-2)),
            CreateTestReceipt(3, 1, 2000m, createdAt: DateTime.UtcNow.AddDays(-3))
        };

        _receiptRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Receipt, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(receipts);

        // Act
        var result = await _service.GetSalesTrendReportAsync(
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow);

        // Assert
        result.Should().NotBeNull();
        result.DailyTrends.Should().NotBeEmpty();
        result.TotalSales.Should().Be(4500m);
        result.TotalTransactions.Should().Be(3);
    }

    #endregion

    #region GetDailySalesTrendsAsync Tests

    [Fact]
    public async Task GetDailySalesTrendsAsync_GroupsByDate()
    {
        // Arrange
        var date1 = DateTime.UtcNow.Date.AddDays(-1);
        var date2 = DateTime.UtcNow.Date.AddDays(-2);

        var receipts = new List<Receipt>
        {
            CreateTestReceipt(1, 1, 1000m, createdAt: date1.AddHours(10)),
            CreateTestReceipt(2, 1, 500m, createdAt: date1.AddHours(15)),
            CreateTestReceipt(3, 1, 2000m, createdAt: date2.AddHours(12))
        };

        _receiptRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Receipt, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(receipts);

        // Act
        var result = await _service.GetDailySalesTrendsAsync(
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow);

        // Assert
        result.Should().HaveCount(2);
        var day1Trend = result.FirstOrDefault(d => d.Date == date1);
        day1Trend.Should().NotBeNull();
        day1Trend!.Sales.Should().Be(1500m);
        day1Trend.Transactions.Should().Be(2);
    }

    #endregion

    #region GetHourlySalesPatternsAsync Tests

    [Fact]
    public async Task GetHourlySalesPatternsAsync_Returns24HourPatterns()
    {
        // Arrange
        _receiptRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Receipt, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Receipt>());

        // Act
        var result = await _service.GetHourlySalesPatternsAsync();

        // Assert
        result.Should().HaveCount(24);
        result.Select(p => p.Hour).Should().BeInAscendingOrder();
    }

    #endregion

    #region GetChainFinancialSummaryAsync Tests

    [Fact]
    public async Task GetChainFinancialSummaryAsync_ReturnsFinancialMetrics()
    {
        // Arrange
        var receipts = new List<Receipt>
        {
            CreateTestReceipt(1, 1, 1000m, subTotal: 1100m, discountAmount: 100m),
            CreateTestReceipt(2, 1, 500m, isVoid: true)
        };

        _storeRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Store, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Store>());

        _receiptRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Receipt, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(receipts);

        _paymentRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Payment, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment>());

        // Act
        var result = await _service.GetChainFinancialSummaryAsync();

        // Assert
        result.Should().NotBeNull();
        result.GrossSales.Should().Be(1100m);
        result.Discounts.Should().Be(100m);
        result.Returns.Should().Be(500m);
    }

    #endregion

    #region GetPaymentMethodBreakdownAsync Tests

    [Fact]
    public async Task GetPaymentMethodBreakdownAsync_GroupsByPaymentMethod()
    {
        // Arrange
        var payments = new List<Payment>
        {
            CreateTestPayment(1, 1, 1, 500m), // Cash
            CreateTestPayment(2, 2, 1, 300m), // Cash
            CreateTestPayment(3, 3, 2, 200m)  // Card
        };

        _paymentRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Payment, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(payments);

        // Act
        var result = await _service.GetPaymentMethodBreakdownAsync();

        // Assert
        result.Should().HaveCount(2); // Two payment methods
        result.Sum(p => p.TotalAmount).Should().Be(1000m);
    }

    #endregion

    #region GetChainInventoryStatusAsync Tests

    [Fact]
    public async Task GetChainInventoryStatusAsync_ReturnsInventoryMetrics()
    {
        // Arrange
        var stores = new List<Store>
        {
            CreateTestStore(1, "STR001")
        };
        var products = new List<Product>
        {
            CreateTestProduct(1, "PROD1", costPrice: 50m),
            CreateTestProduct(2, "PROD2", costPrice: 30m)
        };
        var inventories = new List<Inventory>
        {
            CreateTestInventory(1, 1, 1, 100m, 10m),
            CreateTestInventory(2, 1, 2, 5m, 10m) // Low stock
        };

        _storeRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Store, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(stores);

        _productRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Product, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        _inventoryRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Inventory, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventories);

        // Act
        var result = await _service.GetChainInventoryStatusAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalProducts.Should().Be(2);
        result.LowStockProducts.Should().Be(1); // Product 2 is low
        result.LowStockAlerts.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetChainInventoryStatusAsync_IdentifiesOutOfStock()
    {
        // Arrange
        var stores = new List<Store> { CreateTestStore(1) };
        var products = new List<Product> { CreateTestProduct(1, costPrice: 50m) };
        var inventories = new List<Inventory>
        {
            CreateTestInventory(1, 1, 1, 0m, 10m) // Out of stock
        };

        _storeRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Store, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(stores);

        _productRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Product, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        _inventoryRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Inventory, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventories);

        // Act
        var result = await _service.GetChainInventoryStatusAsync();

        // Assert
        result.OutOfStockProducts.Should().Be(1);
        result.LowStockAlerts.First().IsOutOfStock.Should().BeTrue();
    }

    #endregion

    #region GetLowStockAlertsAsync Tests

    [Fact]
    public async Task GetLowStockAlertsAsync_ReturnsLowStockItems()
    {
        // Arrange
        var stores = new List<Store> { CreateTestStore(1, name: "Test Store") };
        var products = new List<Product>
        {
            CreateTestProduct(1, "PROD1", "Low Product")
        };
        var inventories = new List<Inventory>
        {
            CreateTestInventory(1, 1, 1, 5m, 10m)
        };

        _storeRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Store, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(stores);

        _productRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Product, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        _inventoryRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Inventory, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventories);

        // Act
        var result = await _service.GetLowStockAlertsAsync();

        // Assert
        result.Should().HaveCount(1);
        result.First().ProductName.Should().Be("Low Product");
        result.First().CurrentStock.Should().Be(5m);
    }

    #endregion
}
