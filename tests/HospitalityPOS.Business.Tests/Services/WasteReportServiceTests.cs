using FluentAssertions;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Services;
using Moq;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

public class WasteReportServiceTests
{
    private readonly Mock<IRepository<BatchDisposal>> _disposalRepositoryMock;
    private readonly Mock<IRepository<ProductBatch>> _batchRepositoryMock;
    private readonly Mock<IRepository<Product>> _productRepositoryMock;
    private readonly Mock<IRepository<Category>> _categoryRepositoryMock;
    private readonly Mock<IRepository<Supplier>> _supplierRepositoryMock;
    private readonly Mock<IRepository<Store>> _storeRepositoryMock;
    private readonly WasteReportService _service;

    public WasteReportServiceTests()
    {
        _disposalRepositoryMock = new Mock<IRepository<BatchDisposal>>();
        _batchRepositoryMock = new Mock<IRepository<ProductBatch>>();
        _productRepositoryMock = new Mock<IRepository<Product>>();
        _categoryRepositoryMock = new Mock<IRepository<Category>>();
        _supplierRepositoryMock = new Mock<IRepository<Supplier>>();
        _storeRepositoryMock = new Mock<IRepository<Store>>();

        _service = new WasteReportService(
            _disposalRepositoryMock.Object,
            _batchRepositoryMock.Object,
            _productRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _supplierRepositoryMock.Object,
            _storeRepositoryMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullDisposalRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new WasteReportService(
            null!,
            _batchRepositoryMock.Object,
            _productRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _supplierRepositoryMock.Object,
            _storeRepositoryMock.Object);

        action.Should().Throw<ArgumentNullException>().WithParameterName("disposalRepository");
    }

    [Fact]
    public void Constructor_WithNullBatchRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new WasteReportService(
            _disposalRepositoryMock.Object,
            null!,
            _productRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _supplierRepositoryMock.Object,
            _storeRepositoryMock.Object);

        action.Should().Throw<ArgumentNullException>().WithParameterName("batchRepository");
    }

    #endregion

    #region GetWasteSummaryAsync Tests

    [Fact]
    public async Task GetWasteSummaryAsync_WithValidQuery_ReturnsSummary()
    {
        // Arrange
        var query = new WasteReportQueryDto
        {
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow
        };

        var disposals = new List<BatchDisposal>
        {
            CreateDisposal(1, 1, 10, 5.00m, DisposalReason.Expired),
            CreateDisposal(2, 2, 5, 10.00m, DisposalReason.Damaged)
        };

        var batches = new List<ProductBatch>
        {
            CreateBatch(1, 1, 1, "BATCH001"),
            CreateBatch(2, 2, 1, "BATCH002")
        };

        var products = new List<Product>
        {
            CreateProduct(1, "PROD001", "Product 1", 1),
            CreateProduct(2, "PROD002", "Product 2", 1)
        };

        var categories = new List<Category>
        {
            CreateCategory(1, "Category 1")
        };

        var suppliers = new List<Supplier>
        {
            CreateSupplier(1, "Supplier 1")
        };

        SetupRepositories(disposals, batches, products, categories, suppliers);

        // Act
        var result = await _service.GetWasteSummaryAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.TotalWasteValue.Should().Be(100m); // 10*5 + 5*10
        result.TotalWasteQuantity.Should().Be(15);
        result.TotalWasteRecords.Should().Be(2);
        result.UniqueProductsAffected.Should().Be(2);
    }

    [Fact]
    public async Task GetWasteSummaryAsync_WithStoreFilter_FiltersResults()
    {
        // Arrange
        var query = new WasteReportQueryDto
        {
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow,
            StoreId = 1
        };

        var disposals = new List<BatchDisposal>
        {
            CreateDisposal(1, 1, 10, 5.00m, DisposalReason.Expired, storeId: 1),
            CreateDisposal(2, 2, 5, 10.00m, DisposalReason.Damaged, storeId: 2)
        };

        var batches = new List<ProductBatch>
        {
            CreateBatch(1, 1, 1, "BATCH001"),
            CreateBatch(2, 2, 2, "BATCH002")
        };

        SetupRepositories(disposals, batches);

        // Act
        var result = await _service.GetWasteSummaryAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.TotalWasteValue.Should().Be(50m); // Only store 1
        result.TotalWasteQuantity.Should().Be(10);
        result.TotalWasteRecords.Should().Be(1);
    }

    #endregion

    #region GetWasteByCategoryAsync Tests

    [Fact]
    public async Task GetWasteByCategoryAsync_ReturnsWasteGroupedByCategory()
    {
        // Arrange
        var disposals = new List<BatchDisposal>
        {
            CreateDisposal(1, 1, 10, 5.00m, DisposalReason.Expired),
            CreateDisposal(2, 2, 5, 10.00m, DisposalReason.Expired)
        };

        var batches = new List<ProductBatch>
        {
            CreateBatch(1, 1, 1, "BATCH001"),
            CreateBatch(2, 2, 1, "BATCH002")
        };

        var products = new List<Product>
        {
            CreateProduct(1, "PROD001", "Product 1", 1),
            CreateProduct(2, "PROD002", "Product 2", 2)
        };

        var categories = new List<Category>
        {
            CreateCategory(1, "Food"),
            CreateCategory(2, "Beverages")
        };

        SetupRepositories(disposals, batches, products, categories);

        // Act
        var result = await _service.GetWasteByCategoryAsync(null, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);

        // Assert
        result.Should().HaveCount(2);
        result.First().CategoryName.Should().Be("Beverages"); // Higher value
        result.First().WasteValue.Should().Be(50m);
    }

    #endregion

    #region GetWasteByReasonAsync Tests

    [Fact]
    public async Task GetWasteByReasonAsync_ReturnsWasteGroupedByReason()
    {
        // Arrange
        var disposals = new List<BatchDisposal>
        {
            CreateDisposal(1, 1, 10, 5.00m, DisposalReason.Expired),
            CreateDisposal(2, 2, 5, 10.00m, DisposalReason.Damaged),
            CreateDisposal(3, 3, 8, 5.00m, DisposalReason.Expired)
        };

        SetupRepositories(disposals);

        // Act
        var result = await _service.GetWasteByReasonAsync(null, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);

        // Assert
        result.Should().HaveCount(2);
        result.First().Reason.Should().Be("Expired"); // 10*5 + 8*5 = 90
        result.First().WasteValue.Should().Be(90m);
        result.First().WasteQuantity.Should().Be(18);
    }

    #endregion

    #region GetWasteTrendsAsync Tests

    [Fact]
    public async Task GetWasteTrendsAsync_GroupsByDay_ReturnsDailyTrends()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        var disposals = new List<BatchDisposal>
        {
            CreateDisposal(1, 1, 10, 5.00m, DisposalReason.Expired, disposedAt: today),
            CreateDisposal(2, 2, 5, 10.00m, DisposalReason.Expired, disposedAt: today),
            CreateDisposal(3, 3, 8, 5.00m, DisposalReason.Expired, disposedAt: today.AddDays(-1))
        };

        SetupRepositories(disposals);

        // Act
        var result = await _service.GetWasteTrendsAsync(null, today.AddDays(-7), today.AddDays(1), "day");

        // Assert
        result.Should().HaveCount(2);
        var todayData = result.FirstOrDefault(t => t.Date == today);
        todayData.Should().NotBeNull();
        todayData!.WasteValue.Should().Be(100m); // 10*5 + 5*10
    }

    [Fact]
    public async Task GetWasteTrendsAsync_GroupsByWeek_ReturnsWeeklyTrends()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        var disposals = new List<BatchDisposal>
        {
            CreateDisposal(1, 1, 10, 5.00m, DisposalReason.Expired, disposedAt: today),
            CreateDisposal(2, 2, 5, 10.00m, DisposalReason.Expired, disposedAt: today.AddDays(-7))
        };

        SetupRepositories(disposals);

        // Act
        var result = await _service.GetWasteTrendsAsync(null, today.AddDays(-14), today.AddDays(1), "week");

        // Assert
        result.Should().HaveCountGreaterOrEqualTo(1);
    }

    #endregion

    #region GetWasteComparisonAsync Tests

    [Fact]
    public async Task GetWasteComparisonAsync_ComparesPeriodsCorrectly()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        var currentStart = today.AddDays(-7);
        var currentEnd = today;
        var previousStart = today.AddDays(-14);
        var previousEnd = today.AddDays(-7);

        var disposals = new List<BatchDisposal>
        {
            CreateDisposal(1, 1, 10, 10.00m, DisposalReason.Expired, disposedAt: today.AddDays(-3)),
            CreateDisposal(2, 2, 5, 10.00m, DisposalReason.Expired, disposedAt: today.AddDays(-10))
        };

        SetupRepositories(disposals);

        // Act
        var result = await _service.GetWasteComparisonAsync(null, currentStart, currentEnd, previousStart, previousEnd);

        // Assert
        result.Should().NotBeNull();
        result.CurrentPeriodValue.Should().Be(100m);
        result.PreviousPeriodValue.Should().Be(50m);
        result.ValueChangePercent.Should().Be(100m); // 100% increase
    }

    #endregion

    #region GetWasteAnalysisAsync Tests

    [Fact]
    public async Task GetWasteAnalysisAsync_GeneratesInsights()
    {
        // Arrange
        var disposals = new List<BatchDisposal>
        {
            CreateDisposal(1, 1, 100, 10.00m, DisposalReason.Expired),
            CreateDisposal(2, 2, 10, 5.00m, DisposalReason.Expired),
            CreateDisposal(3, 3, 5, 5.00m, DisposalReason.Damaged)
        };

        var batches = new List<ProductBatch>
        {
            CreateBatch(1, 1, 1, "BATCH001", supplierId: 1),
            CreateBatch(2, 2, 1, "BATCH002", supplierId: 1),
            CreateBatch(3, 3, 1, "BATCH003", supplierId: 2)
        };

        var products = new List<Product>
        {
            CreateProduct(1, "PROD001", "Product 1", 1),
            CreateProduct(2, "PROD002", "Product 2", 1),
            CreateProduct(3, "PROD003", "Product 3", 1)
        };

        var categories = new List<Category> { CreateCategory(1, "Food") };
        var suppliers = new List<Supplier>
        {
            CreateSupplier(1, "Main Supplier"),
            CreateSupplier(2, "Other Supplier")
        };

        SetupRepositories(disposals, batches, products, categories, suppliers);

        // Act
        var result = await _service.GetWasteAnalysisAsync(null, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);

        // Assert
        result.Should().NotBeNull();
        result.TotalWasteValue.Should().Be(1075m);
        result.Insights.Should().NotBeEmpty();
    }

    #endregion

    #region GetWasteDashboardAsync Tests

    [Fact]
    public async Task GetWasteDashboardAsync_ReturnsDashboardData()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        var disposals = new List<BatchDisposal>
        {
            CreateDisposal(1, 1, 10, 5.00m, DisposalReason.Expired, disposedAt: today),
            CreateDisposal(2, 2, 5, 10.00m, DisposalReason.Damaged, disposedAt: today.AddDays(-5))
        };

        var batches = new List<ProductBatch>
        {
            CreateBatch(1, 1, 1, "BATCH001"),
            CreateBatch(2, 2, 1, "BATCH002")
        };

        var products = new List<Product>
        {
            CreateProduct(1, "PROD001", "Product 1", 1),
            CreateProduct(2, "PROD002", "Product 2", 1)
        };

        SetupRepositories(disposals, batches, products);

        // Act
        var result = await _service.GetWasteDashboardAsync(null);

        // Assert
        result.Should().NotBeNull();
        result.GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.Today.Should().NotBeNull();
        result.ThisWeek.Should().NotBeNull();
        result.ThisMonth.Should().NotBeNull();
    }

    #endregion

    #region ExportWasteDataAsync Tests

    [Fact]
    public async Task ExportWasteDataAsync_ReturnsExportData()
    {
        // Arrange
        var query = new WasteReportQueryDto
        {
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow
        };

        var disposals = new List<BatchDisposal>
        {
            CreateDisposal(1, 1, 10, 5.00m, DisposalReason.Expired)
        };

        var batches = new List<ProductBatch>
        {
            CreateBatch(1, 1, 1, "BATCH001", supplierId: 1)
        };

        var products = new List<Product>
        {
            CreateProduct(1, "PROD001", "Test Product", 1)
        };

        var categories = new List<Category> { CreateCategory(1, "Food") };
        var suppliers = new List<Supplier> { CreateSupplier(1, "Test Supplier") };
        var stores = new List<Store> { CreateStore(1, "Main Store", "MAIN") };

        SetupRepositories(disposals, batches, products, categories, suppliers, stores);

        // Act
        var result = await _service.ExportWasteDataAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.TotalRecords.Should().Be(1);
        result.TotalValue.Should().Be(50m);
        result.Records.Should().HaveCount(1);
        result.Records.First().ProductName.Should().Be("Test Product");
    }

    [Fact]
    public async Task ExportWasteDataAsCsvAsync_ReturnsCsvContent()
    {
        // Arrange
        var query = new WasteReportQueryDto
        {
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow
        };

        var disposals = new List<BatchDisposal>
        {
            CreateDisposal(1, 1, 10, 5.00m, DisposalReason.Expired)
        };

        var batches = new List<ProductBatch>
        {
            CreateBatch(1, 1, 1, "BATCH001")
        };

        var products = new List<Product>
        {
            CreateProduct(1, "PROD001", "Test Product", 1)
        };

        var stores = new List<Store> { CreateStore(1, "Main Store", "MAIN") };

        SetupRepositories(disposals, batches, products, stores: stores);

        // Act
        var result = await _service.ExportWasteDataAsCsvAsync(query);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Disposal ID");
        result.Should().Contain("PROD001");
    }

    #endregion

    #region GetWasteByStoreAsync Tests

    [Fact]
    public async Task GetWasteByStoreAsync_ReturnsWasteByStore()
    {
        // Arrange
        var stores = new List<Store>
        {
            CreateStore(1, "Store 1", "S1"),
            CreateStore(2, "Store 2", "S2")
        };

        var disposals = new List<BatchDisposal>
        {
            CreateDisposal(1, 1, 10, 10.00m, DisposalReason.Expired, storeId: 1),
            CreateDisposal(2, 2, 5, 10.00m, DisposalReason.Damaged, storeId: 1),
            CreateDisposal(3, 3, 8, 5.00m, DisposalReason.Expired, storeId: 2)
        };

        var batches = new List<ProductBatch>
        {
            CreateBatch(1, 1, 1, "BATCH001"),
            CreateBatch(2, 2, 1, "BATCH002"),
            CreateBatch(3, 3, 2, "BATCH003")
        };

        var products = new List<Product>
        {
            CreateProduct(1, "PROD001", "Product 1", 1),
            CreateProduct(2, "PROD002", "Product 2", 1),
            CreateProduct(3, "PROD003", "Product 3", 1)
        };

        SetupRepositories(disposals, batches, products, stores: stores);

        // Act
        var result = await _service.GetWasteByStoreAsync(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);

        // Assert
        result.Should().HaveCount(2);
        result.First().StoreName.Should().Be("Store 1"); // Higher waste
        result.First().WasteValue.Should().Be(150m);
    }

    #endregion

    #region Helper Methods

    private void SetupRepositories(
        List<BatchDisposal>? disposals = null,
        List<ProductBatch>? batches = null,
        List<Product>? products = null,
        List<Category>? categories = null,
        List<Supplier>? suppliers = null,
        List<Store>? stores = null)
    {
        _disposalRepositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(disposals ?? new List<BatchDisposal>());

        _batchRepositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(batches ?? new List<ProductBatch>());

        _productRepositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(products ?? new List<Product>());

        _categoryRepositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(categories ?? new List<Category>());

        _supplierRepositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(suppliers ?? new List<Supplier>());

        _storeRepositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(stores ?? new List<Store>());
    }

    private static BatchDisposal CreateDisposal(
        int id,
        int batchId,
        int quantity,
        decimal unitCost,
        DisposalReason reason,
        int storeId = 1,
        DateTime? disposedAt = null)
    {
        return new BatchDisposal
        {
            Id = id,
            BatchId = batchId,
            StoreId = storeId,
            Quantity = quantity,
            UnitCost = unitCost,
            Reason = reason,
            DisposedAt = disposedAt ?? DateTime.UtcNow.AddDays(-1),
            ApprovedByUserId = 1,
            DisposedByUserId = 1,
            Description = "Test disposal",
            IsActive = true
        };
    }

    private static ProductBatch CreateBatch(
        int id,
        int productId,
        int storeId,
        string batchNumber,
        int? supplierId = null)
    {
        return new ProductBatch
        {
            Id = id,
            ProductId = productId,
            StoreId = storeId,
            BatchNumber = batchNumber,
            SupplierId = supplierId,
            InitialQuantity = 100,
            CurrentQuantity = 50,
            ReceivedAt = DateTime.UtcNow.AddDays(-30),
            ReceivedByUserId = 1,
            UnitCost = 5.00m,
            IsActive = true
        };
    }

    private static Product CreateProduct(int id, string code, string name, int categoryId)
    {
        return new Product
        {
            Id = id,
            Code = code,
            Name = name,
            CategoryId = categoryId,
            IsActive = true
        };
    }

    private static Category CreateCategory(int id, string name)
    {
        return new Category
        {
            Id = id,
            Name = name,
            IsActive = true
        };
    }

    private static Supplier CreateSupplier(int id, string name)
    {
        return new Supplier
        {
            Id = id,
            Name = name,
            IsActive = true
        };
    }

    private static Store CreateStore(int id, string name, string code)
    {
        return new Store
        {
            Id = id,
            Name = name,
            Code = code,
            IsActive = true
        };
    }

    #endregion
}
