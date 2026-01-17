using FluentAssertions;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Services;
using Moq;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for ProductBatchService.
/// </summary>
public class ProductBatchServiceTests
{
    private readonly Mock<IRepository<ProductBatch>> _batchRepoMock;
    private readonly Mock<IRepository<ProductBatchConfiguration>> _configRepoMock;
    private readonly Mock<IRepository<BatchStockMovement>> _movementRepoMock;
    private readonly Mock<IRepository<BatchDisposal>> _disposalRepoMock;
    private readonly Mock<IRepository<Product>> _productRepoMock;
    private readonly Mock<IRepository<Store>> _storeRepoMock;
    private readonly Mock<IRepository<Supplier>> _supplierRepoMock;
    private readonly Mock<IRepository<GoodsReceivedNote>> _grnRepoMock;
    private readonly Mock<IRepository<User>> _userRepoMock;
    private readonly ProductBatchService _service;

    public ProductBatchServiceTests()
    {
        _batchRepoMock = new Mock<IRepository<ProductBatch>>();
        _configRepoMock = new Mock<IRepository<ProductBatchConfiguration>>();
        _movementRepoMock = new Mock<IRepository<BatchStockMovement>>();
        _disposalRepoMock = new Mock<IRepository<BatchDisposal>>();
        _productRepoMock = new Mock<IRepository<Product>>();
        _storeRepoMock = new Mock<IRepository<Store>>();
        _supplierRepoMock = new Mock<IRepository<Supplier>>();
        _grnRepoMock = new Mock<IRepository<GoodsReceivedNote>>();
        _userRepoMock = new Mock<IRepository<User>>();

        _service = new ProductBatchService(
            _batchRepoMock.Object,
            _configRepoMock.Object,
            _movementRepoMock.Object,
            _disposalRepoMock.Object,
            _productRepoMock.Object,
            _storeRepoMock.Object,
            _supplierRepoMock.Object,
            _grnRepoMock.Object,
            _userRepoMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullBatchRepository_ThrowsArgumentNullException()
    {
        var action = () => new ProductBatchService(
            null!,
            _configRepoMock.Object,
            _movementRepoMock.Object,
            _disposalRepoMock.Object,
            _productRepoMock.Object,
            _storeRepoMock.Object,
            _supplierRepoMock.Object,
            _grnRepoMock.Object,
            _userRepoMock.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("batchRepository");
    }

    [Fact]
    public void Constructor_WithNullConfigRepository_ThrowsArgumentNullException()
    {
        var action = () => new ProductBatchService(
            _batchRepoMock.Object,
            null!,
            _movementRepoMock.Object,
            _disposalRepoMock.Object,
            _productRepoMock.Object,
            _storeRepoMock.Object,
            _supplierRepoMock.Object,
            _grnRepoMock.Object,
            _userRepoMock.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("configRepository");
    }

    [Fact]
    public void Constructor_WithNullMovementRepository_ThrowsArgumentNullException()
    {
        var action = () => new ProductBatchService(
            _batchRepoMock.Object,
            _configRepoMock.Object,
            null!,
            _disposalRepoMock.Object,
            _productRepoMock.Object,
            _storeRepoMock.Object,
            _supplierRepoMock.Object,
            _grnRepoMock.Object,
            _userRepoMock.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("movementRepository");
    }

    #endregion

    #region Batch Management Tests

    [Fact]
    public async Task CreateBatchAsync_WithValidData_CreatesBatch()
    {
        // Arrange
        var dto = new CreateProductBatchDto
        {
            ProductId = 1,
            StoreId = 1,
            BatchNumber = "BATCH001",
            ExpiryDate = DateTime.UtcNow.AddMonths(6),
            Quantity = 100,
            SupplierId = 1,
            GrnId = 1,
            UnitCost = 10.00m
        };

        _configRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<ProductBatchConfiguration>());

        _batchRepoMock.Setup(r => r.AddAsync(It.IsAny<ProductBatch>()))
            .Returns(Task.CompletedTask);

        _batchRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new ProductBatch
            {
                Id = 1,
                ProductId = dto.ProductId,
                StoreId = dto.StoreId,
                BatchNumber = dto.BatchNumber,
                ExpiryDate = dto.ExpiryDate,
                InitialQuantity = dto.Quantity,
                CurrentQuantity = dto.Quantity,
                Status = BatchStatus.Active,
                UnitCost = dto.UnitCost
            });

        _movementRepoMock.Setup(r => r.AddAsync(It.IsAny<BatchStockMovement>()))
            .Returns(Task.CompletedTask);

        _productRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Product { Id = 1, Name = "Test Product", Code = "P001" });

        _storeRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Store { Id = 1, Name = "Test Store" });

        _supplierRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Supplier { Id = 1, Name = "Test Supplier" });

        _grnRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new GoodsReceivedNote { Id = 1, GRNNumber = "GRN001" });

        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new User { Id = 1, FullName = "Test User" });

        // Act
        var result = await _service.CreateBatchAsync(dto, 1);

        // Assert
        result.Should().NotBeNull();
        result.BatchNumber.Should().Be(dto.BatchNumber);
        result.CurrentQuantity.Should().Be(dto.Quantity);
        result.Status.Should().Be("Active");

        _batchRepoMock.Verify(r => r.AddAsync(It.IsAny<ProductBatch>()), Times.Once);
        _movementRepoMock.Verify(r => r.AddAsync(It.IsAny<BatchStockMovement>()), Times.Once);
    }

    [Fact]
    public async Task CreateBatchAsync_WithRequiredExpiryMissing_ThrowsException()
    {
        // Arrange
        var dto = new CreateProductBatchDto
        {
            ProductId = 1,
            StoreId = 1,
            BatchNumber = "BATCH001",
            ExpiryDate = null, // Missing expiry
            Quantity = 100,
            UnitCost = 10.00m
        };

        _configRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<ProductBatchConfiguration>
            {
                new() { ProductId = 1, RequiresExpiryDate = true }
            });

        // Act
        var action = () => _service.CreateBatchAsync(dto, 1);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Expiry date is required*");
    }

    [Fact]
    public async Task GetBatchAsync_WithValidId_ReturnsBatch()
    {
        // Arrange
        var batch = new ProductBatch
        {
            Id = 1,
            ProductId = 1,
            StoreId = 1,
            BatchNumber = "BATCH001",
            ExpiryDate = DateTime.UtcNow.AddMonths(6),
            CurrentQuantity = 100,
            Status = BatchStatus.Active
        };

        _batchRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(batch);

        _productRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Product { Id = 1, Name = "Test Product" });

        _storeRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Store { Id = 1, Name = "Test Store" });

        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new User { Id = 1, FullName = "Test User" });

        // Act
        var result = await _service.GetBatchAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.BatchNumber.Should().Be("BATCH001");
    }

    [Fact]
    public async Task GetBatchAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        _batchRepoMock.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((ProductBatch?)null);

        // Act
        var result = await _service.GetBatchAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAvailableBatchesAsync_ReturnsBatchesOrderedByExpiryDate()
    {
        // Arrange
        var batches = new List<ProductBatch>
        {
            new() { Id = 1, ProductId = 1, StoreId = 1, BatchNumber = "B1", ExpiryDate = DateTime.UtcNow.AddDays(30), CurrentQuantity = 10, ReservedQuantity = 0 },
            new() { Id = 2, ProductId = 1, StoreId = 1, BatchNumber = "B2", ExpiryDate = DateTime.UtcNow.AddDays(10), CurrentQuantity = 20, ReservedQuantity = 0 },
            new() { Id = 3, ProductId = 1, StoreId = 1, BatchNumber = "B3", ExpiryDate = DateTime.UtcNow.AddDays(60), CurrentQuantity = 15, ReservedQuantity = 0 }
        };

        _batchRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(batches);

        _configRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<ProductBatchConfiguration>());

        // Act
        var result = await _service.GetAvailableBatchesAsync(1, 1);

        // Assert
        result.Should().HaveCount(3);
        result[0].BatchNumber.Should().Be("B2"); // Earliest expiry first (FEFO)
        result[1].BatchNumber.Should().Be("B1");
        result[2].BatchNumber.Should().Be("B3");
    }

    [Fact]
    public async Task GetAvailableBatchesAsync_ExcludesExpiredBatches()
    {
        // Arrange
        var batches = new List<ProductBatch>
        {
            new() { Id = 1, ProductId = 1, StoreId = 1, BatchNumber = "B1", ExpiryDate = DateTime.UtcNow.AddDays(30), CurrentQuantity = 10, ReservedQuantity = 0 },
            new() { Id = 2, ProductId = 1, StoreId = 1, BatchNumber = "B2", ExpiryDate = DateTime.UtcNow.AddDays(-5), CurrentQuantity = 20, ReservedQuantity = 0 } // Expired
        };

        _batchRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(batches);

        _configRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<ProductBatchConfiguration>());

        // Act
        var result = await _service.GetAvailableBatchesAsync(1, 1, includeExpired: false);

        // Assert
        result.Should().HaveCount(1);
        result[0].BatchNumber.Should().Be("B1");
    }

    #endregion

    #region Batch Configuration Tests

    [Fact]
    public async Task GetBatchConfigurationAsync_WithExistingConfig_ReturnsConfig()
    {
        // Arrange
        var config = new ProductBatchConfiguration
        {
            Id = 1,
            ProductId = 1,
            RequiresBatchTracking = true,
            RequiresExpiryDate = true,
            ExpiryWarningDays = 30,
            ExpiryCriticalDays = 7
        };

        _configRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<ProductBatchConfiguration> { config });

        _productRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Product { Id = 1, Name = "Test Product" });

        // Act
        var result = await _service.GetBatchConfigurationAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.RequiresBatchTracking.Should().BeTrue();
        result.RequiresExpiryDate.Should().BeTrue();
        result.ExpiryWarningDays.Should().Be(30);
    }

    [Fact]
    public async Task SaveBatchConfigurationAsync_WithNewConfig_CreatesConfig()
    {
        // Arrange
        var dto = new UpdateProductBatchConfigurationDto
        {
            ProductId = 1,
            RequiresBatchTracking = true,
            RequiresExpiryDate = true,
            ExpiryWarningDays = 30,
            ExpiryCriticalDays = 7,
            ExpiredItemAction = "Block",
            NearExpiryAction = "Warn",
            UseFifo = true,
            UseFefo = true
        };

        _configRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<ProductBatchConfiguration>());

        _configRepoMock.Setup(r => r.AddAsync(It.IsAny<ProductBatchConfiguration>()))
            .Returns(Task.CompletedTask);

        _productRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Product { Id = 1, Name = "Test Product" });

        // Act
        var result = await _service.SaveBatchConfigurationAsync(dto, 1);

        // Assert
        result.Should().NotBeNull();
        result.RequiresBatchTracking.Should().BeTrue();

        _configRepoMock.Verify(r => r.AddAsync(It.IsAny<ProductBatchConfiguration>()), Times.Once);
    }

    [Fact]
    public async Task RequiresBatchTrackingAsync_WithConfiguredProduct_ReturnsTrue()
    {
        // Arrange
        _configRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<ProductBatchConfiguration>
            {
                new() { ProductId = 1, RequiresBatchTracking = true }
            });

        // Act
        var result = await _service.RequiresBatchTrackingAsync(1);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task RequiresBatchTrackingAsync_WithNonConfiguredProduct_ReturnsFalse()
    {
        // Arrange
        _configRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<ProductBatchConfiguration>());

        // Act
        var result = await _service.RequiresBatchTrackingAsync(1);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Expiry Validation Tests

    [Fact]
    public async Task ValidateShelfLifeAsync_WithSufficientShelfLife_ReturnsValid()
    {
        // Arrange
        _configRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<ProductBatchConfiguration>
            {
                new() { ProductId = 1, MinimumShelfLifeDaysOnReceipt = 30 }
            });

        var expiryDate = DateTime.UtcNow.AddDays(60);

        // Act
        var result = await _service.ValidateShelfLifeAsync(1, expiryDate);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ActualShelfLifeDays.Should().BeGreaterOrEqualTo(59);
    }

    [Fact]
    public async Task ValidateShelfLifeAsync_WithInsufficientShelfLife_ReturnsInvalid()
    {
        // Arrange
        _configRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<ProductBatchConfiguration>
            {
                new() { ProductId = 1, MinimumShelfLifeDaysOnReceipt = 30 }
            });

        var expiryDate = DateTime.UtcNow.AddDays(15);

        // Act
        var result = await _service.ValidateShelfLifeAsync(1, expiryDate);

        // Assert
        result.IsValid.Should().BeFalse();
        result.MinimumShelfLifeDays.Should().Be(30);
        result.ActualShelfLifeDays.Should().BeLessThan(30);
    }

    [Fact]
    public async Task ValidateBatchForSaleAsync_WithExpiredBatch_ReturnsBlocked()
    {
        // Arrange
        var batch = new ProductBatch
        {
            Id = 1,
            ProductId = 1,
            ExpiryDate = DateTime.UtcNow.AddDays(-5), // Expired
            CurrentQuantity = 10
        };

        _batchRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(batch);

        _configRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<ProductBatchConfiguration>
            {
                new() { ProductId = 1, ExpiredItemAction = ExpiryAction.Block }
            });

        // Act
        var result = await _service.ValidateBatchForSaleAsync(1);

        // Assert
        result.IsBlocked.Should().BeTrue();
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("expired");
    }

    [Fact]
    public async Task ValidateBatchForSaleAsync_WithNearExpiryBatch_ReturnsWarning()
    {
        // Arrange
        var batch = new ProductBatch
        {
            Id = 1,
            ProductId = 1,
            ExpiryDate = DateTime.UtcNow.AddDays(5), // Near expiry
            CurrentQuantity = 10
        };

        _batchRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(batch);

        _configRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<ProductBatchConfiguration>
            {
                new() { ProductId = 1, ExpiryCriticalDays = 7, NearExpiryAction = ExpiryAction.Warn }
            });

        // Act
        var result = await _service.ValidateBatchForSaleAsync(1);

        // Assert
        result.IsValid.Should().BeTrue();
        result.RequiresWarning.Should().BeTrue();
        result.IsBlocked.Should().BeFalse();
    }

    #endregion

    #region Batch Allocation Tests

    [Fact]
    public async Task AllocateBatchesAsync_WithSufficientStock_AllocatesSuccessfully()
    {
        // Arrange
        var batches = new List<ProductBatch>
        {
            new() { Id = 1, ProductId = 1, StoreId = 1, BatchNumber = "B1", ExpiryDate = DateTime.UtcNow.AddDays(30), CurrentQuantity = 50, ReservedQuantity = 0, UnitCost = 10m },
            new() { Id = 2, ProductId = 1, StoreId = 1, BatchNumber = "B2", ExpiryDate = DateTime.UtcNow.AddDays(60), CurrentQuantity = 50, ReservedQuantity = 0, UnitCost = 10m }
        };

        _batchRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(batches);

        _batchRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => batches.FirstOrDefault(b => b.Id == id));

        _configRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<ProductBatchConfiguration>());

        var request = new AllocateBatchesRequestDto
        {
            ProductId = 1,
            StoreId = 1,
            Quantity = 70,
            UseFifo = true,
            UseFefo = true,
            AllowExpired = false,
            AllowNearExpiry = true
        };

        // Act
        var result = await _service.AllocateBatchesAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.TotalAllocated.Should().Be(70);
        result.Shortfall.Should().Be(0);
        result.Allocations.Should().HaveCount(2);
    }

    [Fact]
    public async Task AllocateBatchesAsync_WithInsufficientStock_ReturnsShortfall()
    {
        // Arrange
        var batches = new List<ProductBatch>
        {
            new() { Id = 1, ProductId = 1, StoreId = 1, BatchNumber = "B1", ExpiryDate = DateTime.UtcNow.AddDays(30), CurrentQuantity = 30, ReservedQuantity = 0, UnitCost = 10m }
        };

        _batchRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(batches);

        _batchRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => batches.FirstOrDefault(b => b.Id == id));

        _configRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<ProductBatchConfiguration>());

        var request = new AllocateBatchesRequestDto
        {
            ProductId = 1,
            StoreId = 1,
            Quantity = 50,
            UseFifo = true,
            UseFefo = true,
            AllowExpired = false
        };

        // Act
        var result = await _service.AllocateBatchesAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.TotalAllocated.Should().Be(30);
        result.Shortfall.Should().Be(20);
        result.Message.Should().Contain("Insufficient stock");
    }

    [Fact]
    public async Task DeductBatchQuantityAsync_WithSufficientQuantity_DeductsSuccessfully()
    {
        // Arrange
        var batch = new ProductBatch
        {
            Id = 1,
            ProductId = 1,
            StoreId = 1,
            BatchNumber = "BATCH001",
            InitialQuantity = 100,
            CurrentQuantity = 100,
            ReservedQuantity = 0,
            SoldQuantity = 0,
            Status = BatchStatus.Active,
            UnitCost = 10m
        };

        _batchRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(batch);

        _batchRepoMock.Setup(r => r.UpdateAsync(It.IsAny<ProductBatch>()))
            .Returns(Task.CompletedTask);

        _movementRepoMock.Setup(r => r.AddAsync(It.IsAny<BatchStockMovement>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeductBatchQuantityAsync(1, 30, "Sale", 100, "REC001", 1);

        // Assert
        batch.CurrentQuantity.Should().Be(70);
        batch.SoldQuantity.Should().Be(30);

        _batchRepoMock.Verify(r => r.UpdateAsync(It.IsAny<ProductBatch>()), Times.Once);
        _movementRepoMock.Verify(r => r.AddAsync(It.IsAny<BatchStockMovement>()), Times.Once);
    }

    [Fact]
    public async Task DeductBatchQuantityAsync_WithInsufficientQuantity_ThrowsException()
    {
        // Arrange
        var batch = new ProductBatch
        {
            Id = 1,
            CurrentQuantity = 10
        };

        _batchRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(batch);

        // Act
        var action = () => _service.DeductBatchQuantityAsync(1, 20, "Sale", 100, "REC001", 1);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Insufficient quantity*");
    }

    #endregion

    #region Batch Disposal Tests

    [Fact]
    public async Task CreateDisposalAsync_WithValidData_CreatesDisposal()
    {
        // Arrange
        var batch = new ProductBatch
        {
            Id = 1,
            ProductId = 1,
            StoreId = 1,
            BatchNumber = "BATCH001",
            CurrentQuantity = 100,
            DisposedQuantity = 0,
            Status = BatchStatus.Active,
            UnitCost = 10m
        };

        var dto = new CreateBatchDisposalDto
        {
            BatchId = 1,
            Quantity = 20,
            Reason = "Expired",
            Description = "Batch expired and disposed",
            ApprovedByUserId = 2,
            IsWitnessed = true,
            WitnessName = "John Doe"
        };

        _batchRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(batch);

        _batchRepoMock.Setup(r => r.UpdateAsync(It.IsAny<ProductBatch>()))
            .Returns(Task.CompletedTask);

        _disposalRepoMock.Setup(r => r.AddAsync(It.IsAny<BatchDisposal>()))
            .Returns(Task.CompletedTask);

        _movementRepoMock.Setup(r => r.AddAsync(It.IsAny<BatchStockMovement>()))
            .Returns(Task.CompletedTask);

        _productRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Product { Id = 1, Name = "Test Product" });

        _storeRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Store { Id = 1, Name = "Test Store" });

        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new User { Id = 1, FullName = "Test User" });

        // Act
        var result = await _service.CreateDisposalAsync(dto, 1);

        // Assert
        result.Should().NotBeNull();
        result.Quantity.Should().Be(20);
        result.Reason.Should().Be("Expired");

        batch.CurrentQuantity.Should().Be(80);
        batch.DisposedQuantity.Should().Be(20);

        _disposalRepoMock.Verify(r => r.AddAsync(It.IsAny<BatchDisposal>()), Times.Once);
        _batchRepoMock.Verify(r => r.UpdateAsync(It.IsAny<ProductBatch>()), Times.Once);
    }

    #endregion

    #region Expiry Monitoring Tests

    [Fact]
    public async Task GetExpiringBatchesAsync_ReturnsBatchesWithinDays()
    {
        // Arrange
        var batches = new List<ProductBatch>
        {
            new() { Id = 1, ProductId = 1, StoreId = 1, BatchNumber = "B1", ExpiryDate = DateTime.UtcNow.AddDays(5), CurrentQuantity = 10 },
            new() { Id = 2, ProductId = 1, StoreId = 1, BatchNumber = "B2", ExpiryDate = DateTime.UtcNow.AddDays(20), CurrentQuantity = 20 },
            new() { Id = 3, ProductId = 1, StoreId = 1, BatchNumber = "B3", ExpiryDate = DateTime.UtcNow.AddDays(40), CurrentQuantity = 15 }
        };

        _batchRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(batches);

        _productRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Product { Id = 1, Name = "Test Product" });

        _storeRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Store { Id = 1, Name = "Test Store" });

        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new User { Id = 1, FullName = "Test User" });

        // Act
        var result = await _service.GetExpiringBatchesAsync(30); // Within 30 days

        // Assert
        result.Should().HaveCount(2);
        result.All(b => b.BatchNumber == "B1" || b.BatchNumber == "B2").Should().BeTrue();
    }

    [Fact]
    public async Task GetExpiredBatchesAsync_ReturnsOnlyExpiredBatches()
    {
        // Arrange
        var batches = new List<ProductBatch>
        {
            new() { Id = 1, ProductId = 1, StoreId = 1, BatchNumber = "B1", ExpiryDate = DateTime.UtcNow.AddDays(-5), CurrentQuantity = 10 }, // Expired
            new() { Id = 2, ProductId = 1, StoreId = 1, BatchNumber = "B2", ExpiryDate = DateTime.UtcNow.AddDays(20), CurrentQuantity = 20 }, // Not expired
            new() { Id = 3, ProductId = 1, StoreId = 1, BatchNumber = "B3", ExpiryDate = DateTime.UtcNow.AddDays(-10), CurrentQuantity = 15 } // Expired
        };

        _batchRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(batches);

        _productRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Product { Id = 1, Name = "Test Product" });

        _storeRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Store { Id = 1, Name = "Test Store" });

        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new User { Id = 1, FullName = "Test User" });

        // Act
        var result = await _service.GetExpiredBatchesAsync();

        // Assert
        result.Should().HaveCount(2);
        result.All(b => b.BatchNumber == "B1" || b.BatchNumber == "B3").Should().BeTrue();
    }

    [Fact]
    public async Task UpdateExpiryStatusesAsync_UpdatesExpiredBatchStatus()
    {
        // Arrange
        var batch = new ProductBatch
        {
            Id = 1,
            ProductId = 1,
            StoreId = 1,
            BatchNumber = "B1",
            ExpiryDate = DateTime.UtcNow.AddDays(-5), // Expired
            CurrentQuantity = 10,
            Status = BatchStatus.Active
        };

        _batchRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<ProductBatch> { batch });

        _batchRepoMock.Setup(r => r.UpdateAsync(It.IsAny<ProductBatch>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.UpdateExpiryStatusesAsync();

        // Assert
        batch.Status.Should().Be(BatchStatus.Expired);
        _batchRepoMock.Verify(r => r.UpdateAsync(It.IsAny<ProductBatch>()), Times.Once);
    }

    #endregion

    #region Expiry Dashboard Tests

    [Fact]
    public async Task GetExpiryDashboardAsync_ReturnsGroupedBatches()
    {
        // Arrange
        var batches = new List<ProductBatch>
        {
            new() { Id = 1, ProductId = 1, StoreId = 1, BatchNumber = "B1", ExpiryDate = DateTime.UtcNow.AddDays(-5), CurrentQuantity = 10, UnitCost = 10m }, // Expired
            new() { Id = 2, ProductId = 1, StoreId = 1, BatchNumber = "B2", ExpiryDate = DateTime.UtcNow.AddDays(5), CurrentQuantity = 20, UnitCost = 10m }, // Critical
            new() { Id = 3, ProductId = 1, StoreId = 1, BatchNumber = "B3", ExpiryDate = DateTime.UtcNow.AddDays(10), CurrentQuantity = 15, UnitCost = 10m }, // Urgent
            new() { Id = 4, ProductId = 1, StoreId = 1, BatchNumber = "B4", ExpiryDate = DateTime.UtcNow.AddDays(20), CurrentQuantity = 25, UnitCost = 10m } // Warning
        };

        _batchRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(batches);

        _batchRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => batches.FirstOrDefault(b => b.Id == id));

        _productRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Product { Id = 1, Name = "Test Product", Code = "P001" });

        _storeRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Store { Id = 1, Name = "Test Store" });

        // Act
        var result = await _service.GetExpiryDashboardAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalExpiredItems.Should().Be(1);
        result.TotalExpiringItems.Should().Be(3);
        result.ExpiryGroups.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task GetSuggestedActionsAsync_ForExpiredBatch_ReturnsDisposeAndRemove()
    {
        // Arrange
        var batch = new ProductBatch
        {
            Id = 1,
            ProductId = 1,
            ExpiryDate = DateTime.UtcNow.AddDays(-5), // Expired
            CurrentQuantity = 10
        };

        _batchRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(batch);

        // Act
        var result = await _service.GetSuggestedActionsAsync(1);

        // Assert
        result.Should().Contain(SuggestedAction.Dispose);
        result.Should().Contain(SuggestedAction.RemoveFromShelf);
    }

    [Fact]
    public async Task GetSuggestedActionsAsync_ForCriticalBatch_ReturnsMarkdownAndPrioritize()
    {
        // Arrange
        var batch = new ProductBatch
        {
            Id = 1,
            ProductId = 1,
            ExpiryDate = DateTime.UtcNow.AddDays(5), // Critical (0-7 days)
            CurrentQuantity = 10
        };

        _batchRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(batch);

        // Act
        var result = await _service.GetSuggestedActionsAsync(1);

        // Assert
        result.Should().Contain(SuggestedAction.Markdown);
        result.Should().Contain(SuggestedAction.PrioritizeSale);
    }

    [Fact]
    public async Task GetExpirySummaryAsync_ReturnsCorrectCounts()
    {
        // Arrange
        var batches = new List<ProductBatch>
        {
            new() { Id = 1, ProductId = 1, StoreId = 1, ExpiryDate = DateTime.UtcNow.AddDays(-5), CurrentQuantity = 10, UnitCost = 10m }, // Expired
            new() { Id = 2, ProductId = 1, StoreId = 1, ExpiryDate = DateTime.UtcNow.AddDays(3), CurrentQuantity = 20, UnitCost = 10m }, // Critical
            new() { Id = 3, ProductId = 1, StoreId = 1, ExpiryDate = DateTime.UtcNow.AddDays(10), CurrentQuantity = 15, UnitCost = 10m }, // Urgent
            new() { Id = 4, ProductId = 1, StoreId = 1, ExpiryDate = DateTime.UtcNow.AddDays(20), CurrentQuantity = 25, UnitCost = 10m } // Warning
        };

        _batchRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(batches);

        // Act
        var result = await _service.GetExpirySummaryAsync();

        // Assert
        result.ExpiredCount.Should().Be(1);
        result.ExpiredValue.Should().Be(100m);
        result.CriticalCount.Should().Be(1);
        result.CriticalValue.Should().Be(200m);
        result.UrgentCount.Should().Be(1);
        result.UrgentValue.Should().Be(150m);
        result.WarningCount.Should().Be(1);
        result.WarningValue.Should().Be(250m);
    }

    [Fact]
    public async Task GetExpiryExportDataAsync_ReturnsFormattedExportData()
    {
        // Arrange
        var batches = new List<ProductBatch>
        {
            new() { Id = 1, ProductId = 1, StoreId = 1, BatchNumber = "B1", ExpiryDate = DateTime.UtcNow.AddDays(5), CurrentQuantity = 10, UnitCost = 10m, ReceivedAt = DateTime.UtcNow.AddDays(-30) }
        };

        _batchRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(batches);

        _batchRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(batches[0]);

        _productRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Product { Id = 1, Name = "Test Product", Code = "P001" });

        _storeRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Store { Id = 1, Name = "Test Store" });

        // Act
        var result = await _service.GetExpiryExportDataAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].ProductCode.Should().Be("P001");
        result[0].ProductName.Should().Be("Test Product");
        result[0].BatchNumber.Should().Be("B1");
        result[0].Severity.Should().Be("Critical");
    }

    [Fact]
    public async Task GetExpiryAlertsAsync_ReturnsAlertsOrderedByExpiry()
    {
        // Arrange
        var batches = new List<ProductBatch>
        {
            new() { Id = 1, ProductId = 1, StoreId = 1, BatchNumber = "B1", ExpiryDate = DateTime.UtcNow.AddDays(20), CurrentQuantity = 10, UnitCost = 10m },
            new() { Id = 2, ProductId = 1, StoreId = 1, BatchNumber = "B2", ExpiryDate = DateTime.UtcNow.AddDays(5), CurrentQuantity = 20, UnitCost = 10m },
            new() { Id = 3, ProductId = 1, StoreId = 1, BatchNumber = "B3", ExpiryDate = DateTime.UtcNow.AddDays(-2), CurrentQuantity = 15, UnitCost = 10m }
        };

        _batchRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(batches);

        _productRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Product { Id = 1, Name = "Test Product" });

        _storeRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Store { Id = 1, Name = "Test Store" });

        // Act
        var result = await _service.GetExpiryAlertsAsync();

        // Assert
        result.Should().HaveCount(3);
        result[0].DaysToExpiry.Should().BeLessThan(result[1].DaysToExpiry);
        result[1].DaysToExpiry.Should().BeLessThan(result[2].DaysToExpiry);
    }

    #endregion

    #region Batch Movement Tests

    [Fact]
    public async Task GetBatchMovementsAsync_ReturnsMovementsForBatch()
    {
        // Arrange
        var movements = new List<BatchStockMovement>
        {
            new() { Id = 1, BatchId = 1, ProductId = 1, StoreId = 1, MovementType = BatchMovementType.Receipt, Quantity = 100, MovedAt = DateTime.UtcNow.AddDays(-10) },
            new() { Id = 2, BatchId = 1, ProductId = 1, StoreId = 1, MovementType = BatchMovementType.Sale, Quantity = -20, MovedAt = DateTime.UtcNow.AddDays(-5) },
            new() { Id = 3, BatchId = 2, ProductId = 1, StoreId = 1, MovementType = BatchMovementType.Receipt, Quantity = 50, MovedAt = DateTime.UtcNow } // Different batch
        };

        _movementRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(movements);

        _batchRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new ProductBatch { Id = 1, BatchNumber = "B1", ProductId = 1, StoreId = 1 });

        _productRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Product { Id = 1, Name = "Test Product" });

        _storeRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Store { Id = 1, Name = "Test Store" });

        // Act
        var result = await _service.GetBatchMovementsAsync(1);

        // Assert
        result.Should().HaveCount(2);
        result.All(m => m.BatchId == 1).Should().BeTrue();
    }

    #endregion
}
