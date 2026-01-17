using FluentAssertions;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Services;
using Moq;
using System.Linq.Expressions;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for the ExpiryValidationService.
/// </summary>
public class ExpiryValidationServiceTests
{
    private readonly Mock<IRepository<ProductBatch>> _batchRepository;
    private readonly Mock<IRepository<ProductBatchConfiguration>> _batchConfigRepository;
    private readonly Mock<IRepository<ExpirySaleBlock>> _saleBlockRepository;
    private readonly Mock<IRepository<CategoryExpirySettings>> _categorySettingsRepository;
    private readonly Mock<IRepository<Product>> _productRepository;
    private readonly Mock<IRepository<Category>> _categoryRepository;
    private readonly Mock<IRepository<User>> _userRepository;
    private readonly Mock<IRepository<Store>> _storeRepository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly ExpiryValidationService _service;

    public ExpiryValidationServiceTests()
    {
        _batchRepository = new Mock<IRepository<ProductBatch>>();
        _batchConfigRepository = new Mock<IRepository<ProductBatchConfiguration>>();
        _saleBlockRepository = new Mock<IRepository<ExpirySaleBlock>>();
        _categorySettingsRepository = new Mock<IRepository<CategoryExpirySettings>>();
        _productRepository = new Mock<IRepository<Product>>();
        _categoryRepository = new Mock<IRepository<Category>>();
        _userRepository = new Mock<IRepository<User>>();
        _storeRepository = new Mock<IRepository<Store>>();
        _unitOfWork = new Mock<IUnitOfWork>();

        _service = new ExpiryValidationService(
            _batchRepository.Object,
            _batchConfigRepository.Object,
            _saleBlockRepository.Object,
            _categorySettingsRepository.Object,
            _productRepository.Object,
            _categoryRepository.Object,
            _userRepository.Object,
            _storeRepository.Object,
            _unitOfWork.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullBatchRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ExpiryValidationService(
            null!,
            _batchConfigRepository.Object,
            _saleBlockRepository.Object,
            _categorySettingsRepository.Object,
            _productRepository.Object,
            _categoryRepository.Object,
            _userRepository.Object,
            _storeRepository.Object,
            _unitOfWork.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullUnitOfWork_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ExpiryValidationService(
            _batchRepository.Object,
            _batchConfigRepository.Object,
            _saleBlockRepository.Object,
            _categorySettingsRepository.Object,
            _productRepository.Object,
            _categoryRepository.Object,
            _userRepository.Object,
            _storeRepository.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region ValidateProductForSaleAsync Tests

    [Fact]
    public async Task ValidateProductForSaleAsync_ProductNotFound_ReturnsNoBlocking()
    {
        // Arrange
        _productRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _service.ValidateProductForSaleAsync(1, 1);

        // Assert
        result.BlockingEnabled.Should().BeFalse();
        result.BlockReason.Should().Be("Product not found");
    }

    [Fact]
    public async Task ValidateProductForSaleAsync_NoExpiryTrackingRequired_AllowsSale()
    {
        // Arrange
        var product = new Product { Id = 1, Name = "Test Product", CategoryId = 1 };
        _productRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        _batchConfigRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProductBatchConfiguration, bool>>>()))
            .ReturnsAsync(new List<ProductBatchConfiguration>());
        _categorySettingsRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<CategoryExpirySettings, bool>>>()))
            .ReturnsAsync(new List<CategoryExpirySettings>());

        // Act
        var result = await _service.ValidateProductForSaleAsync(1, 1);

        // Assert
        result.IsExpired.Should().BeFalse();
        result.BlockingEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateProductForSaleAsync_WithExpiredBatch_BlocksSale()
    {
        // Arrange
        var product = new Product { Id = 1, Name = "Test Product", CategoryId = 1 };
        var expiredBatch = new ProductBatch
        {
            Id = 1,
            ProductId = 1,
            StoreId = 1,
            BatchNumber = "BATCH001",
            ExpiryDate = DateTime.UtcNow.AddDays(-5),
            CurrentQuantity = 10,
            UnitCost = 10m,
            IsActive = true
        };

        var categorySettings = new CategoryExpirySettings
        {
            CategoryId = 1,
            RequiresExpiryTracking = true,
            BlockExpiredSales = true,
            AllowManagerOverride = true,
            IsActive = true
        };

        _productRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        _batchRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProductBatch, bool>>>()))
            .ReturnsAsync(new List<ProductBatch> { expiredBatch });
        _batchConfigRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProductBatchConfiguration, bool>>>()))
            .ReturnsAsync(new List<ProductBatchConfiguration>());
        _categorySettingsRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<CategoryExpirySettings, bool>>>()))
            .ReturnsAsync(new List<CategoryExpirySettings> { categorySettings });
        _categoryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Category { Id = 1, Name = "Test Category" });

        // Act
        var result = await _service.ValidateProductForSaleAsync(1, 1);

        // Assert
        result.IsExpired.Should().BeTrue();
        result.BlockingEnabled.Should().BeTrue();
        result.RequiresOverride.Should().BeTrue();
        result.ExpiredBatches.Should().HaveCount(1);
    }

    [Fact]
    public async Task ValidateProductForSaleAsync_WithNearExpiryBatch_WarnsButAllows()
    {
        // Arrange
        var product = new Product { Id = 1, Name = "Test Product", CategoryId = 1 };
        var nearExpiryBatch = new ProductBatch
        {
            Id = 1,
            ProductId = 1,
            StoreId = 1,
            BatchNumber = "BATCH001",
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            CurrentQuantity = 10,
            UnitCost = 10m,
            IsActive = true
        };

        var categorySettings = new CategoryExpirySettings
        {
            CategoryId = 1,
            RequiresExpiryTracking = true,
            BlockExpiredSales = true,
            AllowManagerOverride = true,
            WarningDays = 30,
            IsActive = true
        };

        _productRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        _batchRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProductBatch, bool>>>()))
            .ReturnsAsync(new List<ProductBatch> { nearExpiryBatch });
        _batchConfigRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProductBatchConfiguration, bool>>>()))
            .ReturnsAsync(new List<ProductBatchConfiguration>());
        _categorySettingsRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<CategoryExpirySettings, bool>>>()))
            .ReturnsAsync(new List<CategoryExpirySettings> { categorySettings });
        _categoryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Category { Id = 1, Name = "Test Category" });

        // Act
        var result = await _service.ValidateProductForSaleAsync(1, 1);

        // Assert
        result.IsExpired.Should().BeFalse();
        result.HasNearExpiryItems.Should().BeTrue();
        result.BlockingEnabled.Should().BeFalse();
    }

    #endregion

    #region ProcessOverrideAsync Tests

    [Fact]
    public async Task ProcessOverrideAsync_SaleBlockNotFound_ReturnsFailure()
    {
        // Arrange
        _saleBlockRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((ExpirySaleBlock?)null);

        var request = new ExpirySaleOverrideRequestDto
        {
            SaleBlockId = 1,
            ManagerUserId = 1,
            ManagerPin = "1234",
            Reason = "Customer request"
        };

        // Act
        var result = await _service.ProcessOverrideAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Sale block record not found.");
    }

    [Fact]
    public async Task ProcessOverrideAsync_ManagerNotFound_ReturnsFailure()
    {
        // Arrange
        var saleBlock = new ExpirySaleBlock
        {
            Id = 1,
            ProductId = 1,
            BatchId = 1,
            StoreId = 1
        };

        _saleBlockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(saleBlock);
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        var request = new ExpirySaleOverrideRequestDto
        {
            SaleBlockId = 1,
            ManagerUserId = 999,
            ManagerPin = "1234",
            Reason = "Customer request"
        };

        // Act
        var result = await _service.ProcessOverrideAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Manager not found.");
    }

    [Fact]
    public async Task ProcessOverrideAsync_MissingPin_ReturnsFailure()
    {
        // Arrange
        var saleBlock = new ExpirySaleBlock
        {
            Id = 1,
            ProductId = 1,
            BatchId = 1,
            StoreId = 1
        };
        var manager = new User { Id = 1, Username = "manager" };

        _saleBlockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(saleBlock);
        _userRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(manager);

        var request = new ExpirySaleOverrideRequestDto
        {
            SaleBlockId = 1,
            ManagerUserId = 1,
            ManagerPin = "",
            Reason = "Customer request"
        };

        // Act
        var result = await _service.ProcessOverrideAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Manager PIN required.");
    }

    [Fact]
    public async Task ProcessOverrideAsync_ValidRequest_ApprovesOverride()
    {
        // Arrange
        var saleBlock = new ExpirySaleBlock
        {
            Id = 1,
            ProductId = 1,
            BatchId = 1,
            StoreId = 1,
            WasBlocked = true
        };
        var manager = new User { Id = 1, Username = "manager" };
        var product = new Product { Id = 1, Name = "Test", CategoryId = 1 };
        var categorySettings = new CategoryExpirySettings
        {
            CategoryId = 1,
            RequiresExpiryTracking = true,
            BlockExpiredSales = true,
            AllowManagerOverride = true,
            IsActive = true
        };

        _saleBlockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(saleBlock);
        _userRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(manager);
        _productRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        _batchConfigRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProductBatchConfiguration, bool>>>()))
            .ReturnsAsync(new List<ProductBatchConfiguration>());
        _categorySettingsRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<CategoryExpirySettings, bool>>>()))
            .ReturnsAsync(new List<CategoryExpirySettings> { categorySettings });
        _categoryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Category { Id = 1, Name = "Test Category" });

        var request = new ExpirySaleOverrideRequestDto
        {
            SaleBlockId = 1,
            ManagerUserId = 1,
            ManagerPin = "1234",
            Reason = "Customer request"
        };

        // Act
        var result = await _service.ProcessOverrideAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.OverrideApproved.Should().BeTrue();
        result.SaleBlockId.Should().Be(1);
        _saleBlockRepository.Verify(r => r.UpdateAsync(It.Is<ExpirySaleBlock>(b =>
            b.OverrideApplied &&
            b.OverrideByUserId == 1 &&
            !b.WasBlocked)), Times.Once);
    }

    #endregion

    #region RecordSaleBlockAsync Tests

    [Fact]
    public async Task RecordSaleBlockAsync_CreatesBlockRecord()
    {
        // Arrange
        var batch = new ProductBatch { Id = 1, BatchNumber = "BATCH001" };
        var product = new Product { Id = 1, Name = "Test Product", SKU = "SKU001" };
        var store = new Store { Id = 1, Name = "Test Store" };
        var user = new User { Id = 1, Username = "cashier" };

        _batchRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(batch);
        _productRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        _storeRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(store);
        _userRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

        var dto = new CreateExpirySaleBlockDto
        {
            ProductId = 1,
            BatchId = 1,
            StoreId = 1,
            ExpiryDate = DateTime.UtcNow.AddDays(-5),
            DaysExpired = 5,
            AttemptedQuantity = 2
        };

        // Act
        var result = await _service.RecordSaleBlockAsync(dto, 1);

        // Assert
        result.ProductName.Should().Be("Test Product");
        result.BatchNumber.Should().Be("BATCH001");
        result.WasBlocked.Should().BeTrue();
        result.OverrideApplied.Should().BeFalse();
        _saleBlockRepository.Verify(r => r.AddAsync(It.IsAny<ExpirySaleBlock>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    #endregion

    #region Category Settings Tests

    [Fact]
    public async Task GetCategorySettingsAsync_ExistingSettings_ReturnsDto()
    {
        // Arrange
        var settings = new CategoryExpirySettings
        {
            Id = 1,
            CategoryId = 1,
            RequiresExpiryTracking = true,
            BlockExpiredSales = true,
            AllowManagerOverride = true,
            WarningDays = 30,
            CriticalDays = 7,
            IsActive = true
        };
        var category = new Category { Id = 1, Name = "Dairy" };

        _categorySettingsRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<CategoryExpirySettings, bool>>>()))
            .ReturnsAsync(new List<CategoryExpirySettings> { settings });
        _categoryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);

        // Act
        var result = await _service.GetCategorySettingsAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.CategoryId.Should().Be(1);
        result.CategoryName.Should().Be("Dairy");
        result.RequiresExpiryTracking.Should().BeTrue();
        result.BlockExpiredSales.Should().BeTrue();
    }

    [Fact]
    public async Task GetCategorySettingsAsync_NoSettings_ReturnsNull()
    {
        // Arrange
        _categorySettingsRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<CategoryExpirySettings, bool>>>()))
            .ReturnsAsync(new List<CategoryExpirySettings>());

        // Act
        var result = await _service.GetCategorySettingsAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveCategorySettingsAsync_NewSettings_CreatesRecord()
    {
        // Arrange
        _categorySettingsRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<CategoryExpirySettings, bool>>>()))
            .ReturnsAsync(new List<CategoryExpirySettings>());
        _categoryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Category { Id = 1, Name = "Dairy" });

        var dto = new UpdateCategoryExpirySettingsDto
        {
            CategoryId = 1,
            RequiresExpiryTracking = true,
            BlockExpiredSales = true,
            AllowManagerOverride = true,
            WarningDays = 14,
            CriticalDays = 3
        };

        // Act
        var result = await _service.SaveCategorySettingsAsync(dto, 1);

        // Assert
        result.CategoryId.Should().Be(1);
        result.RequiresExpiryTracking.Should().BeTrue();
        _categorySettingsRepository.Verify(r => r.AddAsync(It.IsAny<CategoryExpirySettings>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    #endregion

    #region GetEffectiveSettingsAsync Tests

    [Fact]
    public async Task GetEffectiveSettingsAsync_ProductNotFound_ReturnsDefaults()
    {
        // Arrange
        _productRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Product?)null);

        // Act
        var result = await _service.GetEffectiveSettingsAsync(999);

        // Assert
        result.ProductId.Should().Be(999);
        result.SettingsSource.Should().Be("Default");
        result.RequiresExpiryTracking.Should().BeFalse();
    }

    [Fact]
    public async Task GetEffectiveSettingsAsync_ProductConfigExists_ReturnsProductSettings()
    {
        // Arrange
        var product = new Product { Id = 1, Name = "Test", CategoryId = 1 };
        var productConfig = new ProductBatchConfiguration
        {
            ProductId = 1,
            RequiresBatchTracking = true,
            RequiresExpiryDate = true,
            ExpiredItemAction = ExpiryAction.Block,
            NearExpiryAction = ExpiryAction.Warn,
            ExpiryWarningDays = 14,
            ExpiryCriticalDays = 3,
            IsActive = true
        };

        _productRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        _batchConfigRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProductBatchConfiguration, bool>>>()))
            .ReturnsAsync(new List<ProductBatchConfiguration> { productConfig });

        // Act
        var result = await _service.GetEffectiveSettingsAsync(1);

        // Assert
        result.ProductId.Should().Be(1);
        result.SettingsSource.Should().Be("Product");
        result.RequiresExpiryTracking.Should().BeTrue();
        result.WarningDays.Should().Be(14);
    }

    [Fact]
    public async Task GetEffectiveSettingsAsync_NoProductConfig_UsesCategorySettings()
    {
        // Arrange
        var product = new Product { Id = 1, Name = "Test", CategoryId = 1 };
        var category = new Category { Id = 1, Name = "Dairy" };
        var categorySettings = new CategoryExpirySettings
        {
            CategoryId = 1,
            RequiresExpiryTracking = true,
            BlockExpiredSales = true,
            AllowManagerOverride = true,
            WarningDays = 21,
            CriticalDays = 5,
            IsActive = true
        };

        _productRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        _batchConfigRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProductBatchConfiguration, bool>>>()))
            .ReturnsAsync(new List<ProductBatchConfiguration>());
        _categorySettingsRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<CategoryExpirySettings, bool>>>()))
            .ReturnsAsync(new List<CategoryExpirySettings> { categorySettings });
        _categoryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);

        // Act
        var result = await _service.GetEffectiveSettingsAsync(1);

        // Assert
        result.ProductId.Should().Be(1);
        result.SettingsSource.Should().Be("Category");
        result.CategoryName.Should().Be("Dairy");
        result.WarningDays.Should().Be(21);
    }

    #endregion

    #region GetSaleBlockSummaryAsync Tests

    [Fact]
    public async Task GetSaleBlockSummaryAsync_ReturnsCorrectSummary()
    {
        // Arrange
        var blocks = new List<ExpirySaleBlock>
        {
            new() { Id = 1, ProductId = 1, StoreId = 1, AttemptedQuantity = 2, OverrideApplied = false, IsActive = true },
            new() { Id = 2, ProductId = 1, StoreId = 1, AttemptedQuantity = 3, OverrideApplied = true, IsActive = true },
            new() { Id = 3, ProductId = 2, StoreId = 2, AttemptedQuantity = 1, OverrideApplied = false, IsActive = true }
        };
        var product1 = new Product { Id = 1, Name = "Product 1", SKU = "P001", SellingPrice = 10m };
        var product2 = new Product { Id = 2, Name = "Product 2", SKU = "P002", SellingPrice = 20m };

        _saleBlockRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ExpirySaleBlock, bool>>>()))
            .ReturnsAsync(blocks);
        _productRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product1);
        _productRepository.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(product2);

        // Act
        var result = await _service.GetSaleBlockSummaryAsync();

        // Assert
        result.TotalBlockedAttempts.Should().Be(3);
        result.TotalOverrides.Should().Be(1);
        result.TotalPermanentBlocks.Should().Be(2);
        result.UniqueProducts.Should().Be(2);
        result.UniqueStores.Should().Be(2);
    }

    #endregion

    #region GetOverrideHistoryAsync Tests

    [Fact]
    public async Task GetOverrideHistoryAsync_ReturnsOnlyOverrides()
    {
        // Arrange
        var blocks = new List<ExpirySaleBlock>
        {
            new() {
                Id = 1,
                ProductId = 1,
                BatchId = 1,
                StoreId = 1,
                OverrideApplied = true,
                OverrideByUserId = 1,
                OverrideAt = DateTime.UtcNow,
                AttemptedAt = DateTime.UtcNow,
                AttemptedByUserId = 2,
                IsActive = true
            }
        };

        _saleBlockRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ExpirySaleBlock, bool>>>()))
            .ReturnsAsync(blocks);
        _productRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new Product { Id = 1, Name = "Test" });
        _batchRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new ProductBatch { Id = 1, BatchNumber = "B001" });
        _storeRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new Store { Id = 1, Name = "Store" });
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new User { Id = 1, Username = "user" });

        // Act
        var result = await _service.GetOverrideHistoryAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].OverrideApplied.Should().BeTrue();
    }

    #endregion
}
