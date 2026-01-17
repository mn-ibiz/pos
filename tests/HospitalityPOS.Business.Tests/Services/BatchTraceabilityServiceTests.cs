using FluentAssertions;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Services;
using Moq;
using System.Linq.Expressions;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for the BatchTraceabilityService.
/// </summary>
public class BatchTraceabilityServiceTests
{
    private readonly Mock<IRepository<ProductBatch>> _batchRepository;
    private readonly Mock<IRepository<BatchStockMovement>> _movementRepository;
    private readonly Mock<IRepository<BatchRecallAlert>> _recallRepository;
    private readonly Mock<IRepository<RecallAction>> _recallActionRepository;
    private readonly Mock<IRepository<Product>> _productRepository;
    private readonly Mock<IRepository<Store>> _storeRepository;
    private readonly Mock<IRepository<Supplier>> _supplierRepository;
    private readonly Mock<IRepository<User>> _userRepository;
    private readonly Mock<IRepository<GoodsReceivedNote>> _grnRepository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly BatchTraceabilityService _service;

    public BatchTraceabilityServiceTests()
    {
        _batchRepository = new Mock<IRepository<ProductBatch>>();
        _movementRepository = new Mock<IRepository<BatchStockMovement>>();
        _recallRepository = new Mock<IRepository<BatchRecallAlert>>();
        _recallActionRepository = new Mock<IRepository<RecallAction>>();
        _productRepository = new Mock<IRepository<Product>>();
        _storeRepository = new Mock<IRepository<Store>>();
        _supplierRepository = new Mock<IRepository<Supplier>>();
        _userRepository = new Mock<IRepository<User>>();
        _grnRepository = new Mock<IRepository<GoodsReceivedNote>>();
        _unitOfWork = new Mock<IUnitOfWork>();

        _service = new BatchTraceabilityService(
            _batchRepository.Object,
            _movementRepository.Object,
            _recallRepository.Object,
            _recallActionRepository.Object,
            _productRepository.Object,
            _storeRepository.Object,
            _supplierRepository.Object,
            _userRepository.Object,
            _grnRepository.Object,
            _unitOfWork.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullBatchRepository_ThrowsArgumentNullException()
    {
        var act = () => new BatchTraceabilityService(
            null!,
            _movementRepository.Object,
            _recallRepository.Object,
            _recallActionRepository.Object,
            _productRepository.Object,
            _storeRepository.Object,
            _supplierRepository.Object,
            _userRepository.Object,
            _grnRepository.Object,
            _unitOfWork.Object);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullUnitOfWork_ThrowsArgumentNullException()
    {
        var act = () => new BatchTraceabilityService(
            _batchRepository.Object,
            _movementRepository.Object,
            _recallRepository.Object,
            _recallActionRepository.Object,
            _productRepository.Object,
            _storeRepository.Object,
            _supplierRepository.Object,
            _userRepository.Object,
            _grnRepository.Object,
            null!);

        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Batch Search Tests

    [Fact]
    public async Task SearchBatchesAsync_ByBatchNumber_ReturnsMatches()
    {
        // Arrange
        var batches = new List<ProductBatch>
        {
            new() {
                Id = 1,
                BatchNumber = "BATCH001",
                ProductId = 1,
                StoreId = 1,
                ReceivedAt = DateTime.UtcNow,
                IsActive = true
            }
        };

        _batchRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProductBatch, bool>>>()))
            .ReturnsAsync(batches);
        _productRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new Product { Id = 1, Name = "Test Product", SKU = "SKU001" });
        _storeRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new Store { Id = 1, Name = "Test Store" });
        _recallRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<BatchRecallAlert, bool>>>()))
            .ReturnsAsync(new List<BatchRecallAlert>());

        var query = new BatchSearchQueryDto { BatchNumber = "BATCH" };

        // Act
        var result = await _service.SearchBatchesAsync(query);

        // Assert
        result.Should().HaveCount(1);
        result[0].BatchNumber.Should().Be("BATCH001");
    }

    [Fact]
    public async Task GetProductBatchesAsync_ReturnsBatchesForProduct()
    {
        // Arrange
        var batches = new List<ProductBatch>
        {
            new() { Id = 1, BatchNumber = "B001", ProductId = 1, StoreId = 1, ReceivedAt = DateTime.UtcNow, IsActive = true },
            new() { Id = 2, BatchNumber = "B002", ProductId = 1, StoreId = 1, ReceivedAt = DateTime.UtcNow, IsActive = true }
        };

        _batchRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProductBatch, bool>>>()))
            .ReturnsAsync(batches);
        _productRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new Product { Id = 1, Name = "Test" });
        _storeRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new Store { Id = 1, Name = "Store" });
        _recallRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<BatchRecallAlert, bool>>>()))
            .ReturnsAsync(new List<BatchRecallAlert>());

        // Act
        var result = await _service.GetProductBatchesAsync(1);

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region Traceability Report Tests

    [Fact]
    public async Task GetTraceabilityReportAsync_BatchNotFound_ReturnsNull()
    {
        // Arrange
        _batchRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((ProductBatch?)null);

        // Act
        var result = await _service.GetTraceabilityReportAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTraceabilityReportAsync_ValidBatch_ReturnsFullReport()
    {
        // Arrange
        var batch = new ProductBatch
        {
            Id = 1,
            BatchNumber = "BATCH001",
            ProductId = 1,
            StoreId = 1,
            SupplierId = 1,
            GrnId = 1,
            InitialQuantity = 100,
            CurrentQuantity = 50,
            SoldQuantity = 40,
            DisposedQuantity = 10,
            UnitCost = 10m,
            ReceivedAt = DateTime.UtcNow.AddDays(-30),
            ExpiryDate = DateTime.UtcNow.AddDays(30),
            IsActive = true
        };
        var product = new Product { Id = 1, Name = "Test Product", SKU = "SKU001" };
        var store = new Store { Id = 1, Name = "Test Store" };
        var supplier = new Supplier { Id = 1, Name = "Test Supplier" };
        var grn = new GoodsReceivedNote { Id = 1, GrnNumber = "GRN001" };

        _batchRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(batch);
        _productRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        _storeRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(store);
        _supplierRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(supplier);
        _grnRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(grn);
        _movementRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<BatchStockMovement, bool>>>()))
            .ReturnsAsync(new List<BatchStockMovement>());
        _recallRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<BatchRecallAlert, bool>>>()))
            .ReturnsAsync(new List<BatchRecallAlert>());

        // Act
        var result = await _service.GetTraceabilityReportAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.BatchNumber.Should().Be("BATCH001");
        result.ProductName.Should().Be("Test Product");
        result.SupplierName.Should().Be("Test Supplier");
        result.GrnNumber.Should().Be("GRN001");
        result.QuantityReceived.Should().Be(100);
        result.CurrentQuantity.Should().Be(50);
        result.TotalReceivedValue.Should().Be(1000m);
    }

    [Fact]
    public async Task GetBatchMovementsAsync_ReturnsMovementHistory()
    {
        // Arrange
        var movements = new List<BatchStockMovement>
        {
            new()
            {
                Id = 1,
                BatchId = 1,
                MovementType = BatchMovementType.Receipt,
                Quantity = 100,
                QuantityBefore = 0,
                QuantityAfter = 100,
                MovedAt = DateTime.UtcNow.AddDays(-10),
                IsActive = true
            },
            new()
            {
                Id = 2,
                BatchId = 1,
                MovementType = BatchMovementType.Sale,
                Quantity = -20,
                QuantityBefore = 100,
                QuantityAfter = 80,
                MovedAt = DateTime.UtcNow.AddDays(-5),
                ReferenceType = "Receipt",
                ReferenceNumber = "R001",
                IsActive = true
            }
        };

        _movementRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<BatchStockMovement, bool>>>()))
            .ReturnsAsync(movements);

        // Act
        var result = await _service.GetBatchMovementsAsync(1);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(m => m.MovementType == "Receipt");
        result.Should().Contain(m => m.MovementType == "Sale");
    }

    [Fact]
    public async Task GetBatchSaleTransactionsAsync_ReturnsSales()
    {
        // Arrange
        var saleMovements = new List<BatchStockMovement>
        {
            new()
            {
                Id = 1,
                BatchId = 1,
                StoreId = 1,
                MovementType = BatchMovementType.Sale,
                Quantity = -5,
                ReferenceType = "Receipt",
                ReferenceId = 100,
                ReferenceNumber = "R001",
                UnitCost = 10m,
                MovedAt = DateTime.UtcNow,
                MovedByUserId = 1,
                IsActive = true
            }
        };

        _movementRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<BatchStockMovement, bool>>>()))
            .ReturnsAsync(saleMovements);
        _userRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new User { Id = 1, Username = "cashier" });
        _storeRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Store { Id = 1, Name = "Store" });

        // Act
        var result = await _service.GetBatchSaleTransactionsAsync(1);

        // Assert
        result.Should().HaveCount(1);
        result[0].ReceiptNumber.Should().Be("R001");
        result[0].QuantitySold.Should().Be(5);
    }

    #endregion

    #region Recall Management Tests

    [Fact]
    public async Task CreateRecallAlertAsync_BatchNotFound_ThrowsException()
    {
        // Arrange
        _batchRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((ProductBatch?)null);

        var dto = new CreateBatchRecallAlertDto
        {
            BatchId = 999,
            RecallReason = "Contamination",
            Severity = "High"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateRecallAlertAsync(dto, 1));
    }

    [Fact]
    public async Task CreateRecallAlertAsync_ValidBatch_CreatesRecall()
    {
        // Arrange
        var batch = new ProductBatch
        {
            Id = 1,
            BatchNumber = "BATCH001",
            ProductId = 1,
            InitialQuantity = 100,
            CurrentQuantity = 80,
            SoldQuantity = 20,
            IsActive = true
        };

        _batchRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(batch);
        _productRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Product { Id = 1, Name = "Test" });
        _userRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new User { Id = 1, Username = "admin" });
        _recallActionRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<RecallAction, bool>>>()))
            .ReturnsAsync(new List<RecallAction>());

        var dto = new CreateBatchRecallAlertDto
        {
            BatchId = 1,
            RecallReason = "Contamination detected",
            Severity = "High"
        };

        // Act
        var result = await _service.CreateRecallAlertAsync(dto, 1);

        // Assert
        result.BatchNumber.Should().Be("BATCH001");
        result.Severity.Should().Be("High");
        result.Status.Should().Be("Active");
        result.AffectedQuantity.Should().Be(100);
        _recallRepository.Verify(r => r.AddAsync(It.IsAny<BatchRecallAlert>()), Times.Once);
        _batchRepository.Verify(r => r.UpdateAsync(It.Is<ProductBatch>(b => b.Status == BatchStatus.Recalled)), Times.Once);
    }

    [Fact]
    public async Task UpdateRecallStatusAsync_SetsResolvedInfo()
    {
        // Arrange
        var recall = new BatchRecallAlert
        {
            Id = 1,
            BatchId = 1,
            ProductId = 1,
            BatchNumber = "BATCH001",
            Status = RecallStatus.Active,
            IsActive = true
        };

        _recallRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recall);
        _productRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Product { Id = 1, Name = "Test" });
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new User { Id = 1, Username = "admin" });
        _recallActionRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<RecallAction, bool>>>()))
            .ReturnsAsync(new List<RecallAction>());

        var dto = new UpdateRecallStatusDto
        {
            RecallAlertId = 1,
            Status = "Recovered",
            QuantityRecovered = 80,
            ResolutionNotes = "All items recovered"
        };

        // Act
        var result = await _service.UpdateRecallStatusAsync(dto, 1);

        // Assert
        result.Status.Should().Be("Recovered");
        result.QuantityRecovered.Should().Be(80);
        _recallRepository.Verify(r => r.UpdateAsync(It.Is<BatchRecallAlert>(ra =>
            ra.Status == RecallStatus.Recovered &&
            ra.ResolvedAt.HasValue)), Times.Once);
    }

    [Fact]
    public async Task RecordRecallActionAsync_RecordsAction()
    {
        // Arrange
        var recall = new BatchRecallAlert
        {
            Id = 1,
            BatchId = 1,
            QuantityRecovered = 0,
            IsActive = true
        };

        _recallRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recall);
        _userRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new User { Id = 1, Username = "user" });
        _storeRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Store { Id = 1, Name = "Store" });

        var dto = new CreateRecallActionDto
        {
            RecallAlertId = 1,
            ActionType = "Quarantine",
            StoreId = 1,
            Quantity = 50,
            Description = "Quarantined 50 units"
        };

        // Act
        var result = await _service.RecordRecallActionAsync(dto, 1);

        // Assert
        result.ActionType.Should().Be("Quarantine");
        result.Quantity.Should().Be(50);
        _recallActionRepository.Verify(r => r.AddAsync(It.IsAny<RecallAction>()), Times.Once);
        // Quarantine should update recovered quantity
        _recallRepository.Verify(r => r.UpdateAsync(It.Is<BatchRecallAlert>(ra => ra.QuantityRecovered == 50)), Times.Once);
    }

    [Fact]
    public async Task HasActiveRecallAsync_WithActiveRecall_ReturnsTrue()
    {
        // Arrange
        var recalls = new List<BatchRecallAlert>
        {
            new()
            {
                Id = 1,
                BatchId = 1,
                Status = RecallStatus.Active,
                IsActive = true
            }
        };

        _recallRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<BatchRecallAlert, bool>>>()))
            .ReturnsAsync(recalls);

        // Act
        var result = await _service.HasActiveRecallAsync(1);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasActiveRecallAsync_NoActiveRecall_ReturnsFalse()
    {
        // Arrange
        _recallRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<BatchRecallAlert, bool>>>()))
            .ReturnsAsync(new List<BatchRecallAlert>());

        // Act
        var result = await _service.HasActiveRecallAsync(1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetRecallSummaryAsync_ReturnsCorrectStats()
    {
        // Arrange
        var recalls = new List<BatchRecallAlert>
        {
            new() { Id = 1, Status = RecallStatus.Active, Severity = RecallSeverity.Critical, AffectedQuantity = 100, QuantityRecovered = 50, QuantitySold = 20, IsActive = true },
            new() { Id = 2, Status = RecallStatus.Recovered, Severity = RecallSeverity.Medium, AffectedQuantity = 50, QuantityRecovered = 50, QuantitySold = 0, IsActive = true },
            new() { Id = 3, Status = RecallStatus.Active, Severity = RecallSeverity.High, AffectedQuantity = 80, QuantityRecovered = 0, QuantitySold = 10, IsActive = true }
        };

        _recallRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<BatchRecallAlert, bool>>>()))
            .ReturnsAsync(recalls);

        // Act
        var result = await _service.GetRecallSummaryAsync();

        // Assert
        result.TotalActiveRecalls.Should().Be(2);
        result.TotalClosedRecalls.Should().Be(1);
        result.TotalAffectedQuantity.Should().Be(230);
        result.TotalRecoveredQuantity.Should().Be(100);
        result.TotalSoldBeforeRecall.Should().Be(30);
        result.CriticalRecalls.Should().Be(1);
        result.HighRecalls.Should().Be(1);
    }

    #endregion

    #region Quarantine Tests

    [Fact]
    public async Task QuarantineBatchAsync_UpdatesBatchAndRecordsAction()
    {
        // Arrange
        var batch = new ProductBatch
        {
            Id = 1,
            BatchNumber = "BATCH001",
            StoreId = 1,
            CurrentQuantity = 50,
            Status = BatchStatus.Active,
            IsActive = true
        };
        var recall = new BatchRecallAlert { Id = 1, BatchId = 1, IsActive = true };

        _batchRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(batch);
        _recallRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recall);
        _userRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new User { Id = 1, Username = "user" });
        _storeRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Store { Id = 1, Name = "Store" });

        // Act
        await _service.QuarantineBatchAsync(1, 1, 1);

        // Assert
        _batchRepository.Verify(r => r.UpdateAsync(It.Is<ProductBatch>(b => b.Status == BatchStatus.Recalled)), Times.Once);
        _recallActionRepository.Verify(r => r.AddAsync(It.Is<RecallAction>(a =>
            a.ActionType == "Quarantine" &&
            a.Quantity == 50)), Times.Once);
    }

    #endregion
}
