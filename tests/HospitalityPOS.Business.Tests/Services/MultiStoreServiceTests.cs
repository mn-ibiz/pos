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
/// Unit tests for the MultiStoreService class.
/// Tests cover store management, central product management, override management,
/// sync operations, pricing zones, zone pricing, and scheduled price changes.
/// </summary>
public class MultiStoreServiceTests
{
    private readonly Mock<IRepository<Store>> _storeRepositoryMock;
    private readonly Mock<IRepository<Product>> _productRepositoryMock;
    private readonly Mock<IRepository<StoreProductOverride>> _overrideRepositoryMock;
    private readonly Mock<IRepository<PricingZone>> _pricingZoneRepositoryMock;
    private readonly Mock<IRepository<ZonePrice>> _zonePriceRepositoryMock;
    private readonly Mock<IRepository<ScheduledPriceChange>> _priceChangeRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<MultiStoreService>> _loggerMock;
    private readonly MultiStoreService _service;

    private const int TestUserId = 1;

    public MultiStoreServiceTests()
    {
        _storeRepositoryMock = new Mock<IRepository<Store>>();
        _productRepositoryMock = new Mock<IRepository<Product>>();
        _overrideRepositoryMock = new Mock<IRepository<StoreProductOverride>>();
        _pricingZoneRepositoryMock = new Mock<IRepository<PricingZone>>();
        _zonePriceRepositoryMock = new Mock<IRepository<ZonePrice>>();
        _priceChangeRepositoryMock = new Mock<IRepository<ScheduledPriceChange>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<MultiStoreService>>();

        _service = new MultiStoreService(
            _storeRepositoryMock.Object,
            _productRepositoryMock.Object,
            _overrideRepositoryMock.Object,
            _pricingZoneRepositoryMock.Object,
            _zonePriceRepositoryMock.Object,
            _priceChangeRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    #region Helper Methods

    private static Store CreateTestStore(
        int id = 1,
        string storeCode = "STR001",
        string name = "Test Store",
        bool isHeadquarters = false,
        bool receivesCentralUpdates = true,
        bool isActive = true,
        DateTime? lastSyncTime = null)
    {
        return new Store
        {
            Id = id,
            StoreCode = storeCode,
            Name = name,
            Address = "123 Test Street",
            City = "Nairobi",
            Region = "Nairobi County",
            PhoneNumber = "0712345678",
            Email = "store@test.com",
            IsHeadquarters = isHeadquarters,
            ReceivesCentralUpdates = receivesCentralUpdates,
            IsActive = isActive,
            LastSyncTime = lastSyncTime,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static Product CreateTestProduct(
        int id = 1,
        string code = "PROD001",
        string name = "Test Product",
        decimal sellingPrice = 100m,
        decimal? costPrice = 50m,
        bool isCentralProduct = false,
        bool allowStoreOverride = true,
        bool isActive = true,
        DateTime? lastSyncTime = null)
    {
        return new Product
        {
            Id = id,
            Code = code,
            Name = name,
            SellingPrice = sellingPrice,
            CostPrice = costPrice,
            IsCentralProduct = isCentralProduct,
            AllowStoreOverride = allowStoreOverride,
            IsActive = isActive,
            LastSyncTime = lastSyncTime,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static StoreProductOverride CreateTestOverride(
        int id = 1,
        int storeId = 1,
        int productId = 1,
        decimal? overridePrice = null,
        decimal? overrideCost = null,
        bool isAvailable = true,
        bool isActive = true)
    {
        return new StoreProductOverride
        {
            Id = id,
            StoreId = storeId,
            ProductId = productId,
            OverridePrice = overridePrice,
            OverrideCost = overrideCost,
            IsAvailable = isAvailable,
            IsActive = isActive,
            LastSyncTime = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static PricingZone CreateTestPricingZone(
        int id = 1,
        string zoneCode = "ZONE001",
        string name = "Test Zone",
        bool isDefault = false,
        bool isActive = true)
    {
        return new PricingZone
        {
            Id = id,
            ZoneCode = zoneCode,
            Name = name,
            Description = "Test zone description",
            CurrencyCode = "KES",
            DefaultTaxRate = 16.0m,
            IsDefault = isDefault,
            DisplayOrder = 1,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static ZonePrice CreateTestZonePrice(
        int id = 1,
        int zoneId = 1,
        int productId = 1,
        decimal price = 100m,
        DateTime? effectiveFrom = null,
        DateTime? effectiveTo = null,
        bool isActive = true)
    {
        return new ZonePrice
        {
            Id = id,
            PricingZoneId = zoneId,
            ProductId = productId,
            Price = price,
            CostPrice = price * 0.5m,
            MinimumPrice = price * 0.7m,
            EffectiveFrom = effectiveFrom ?? DateTime.UtcNow.AddDays(-1),
            EffectiveTo = effectiveTo,
            Reason = "Test zone price",
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static ScheduledPriceChange CreateTestScheduledPriceChange(
        int id = 1,
        int productId = 1,
        int? zoneId = null,
        int? storeId = null,
        decimal oldPrice = 100m,
        decimal newPrice = 90m,
        DateTime? effectiveDate = null,
        PriceChangeStatus status = PriceChangeStatus.Scheduled,
        bool isActive = true)
    {
        return new ScheduledPriceChange
        {
            Id = id,
            ProductId = productId,
            PricingZoneId = zoneId,
            StoreId = storeId,
            OldPrice = oldPrice,
            NewPrice = newPrice,
            NewCostPrice = newPrice * 0.5m,
            EffectiveDate = effectiveDate ?? DateTime.UtcNow.AddDays(1),
            Status = status,
            Reason = "Price adjustment",
            Notes = "Test scheduled change",
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullStoreRepository_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new MultiStoreService(
            null!,
            _productRepositoryMock.Object,
            _overrideRepositoryMock.Object,
            _pricingZoneRepositoryMock.Object,
            _zonePriceRepositoryMock.Object,
            _priceChangeRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("storeRepository");
    }

    [Fact]
    public void Constructor_WithNullProductRepository_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new MultiStoreService(
            _storeRepositoryMock.Object,
            null!,
            _overrideRepositoryMock.Object,
            _pricingZoneRepositoryMock.Object,
            _zonePriceRepositoryMock.Object,
            _priceChangeRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("productRepository");
    }

    [Fact]
    public void Constructor_WithNullOverrideRepository_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new MultiStoreService(
            _storeRepositoryMock.Object,
            _productRepositoryMock.Object,
            null!,
            _pricingZoneRepositoryMock.Object,
            _zonePriceRepositoryMock.Object,
            _priceChangeRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("overrideRepository");
    }

    [Fact]
    public void Constructor_WithNullPricingZoneRepository_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new MultiStoreService(
            _storeRepositoryMock.Object,
            _productRepositoryMock.Object,
            _overrideRepositoryMock.Object,
            null!,
            _zonePriceRepositoryMock.Object,
            _priceChangeRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("pricingZoneRepository");
    }

    [Fact]
    public void Constructor_WithNullZonePriceRepository_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new MultiStoreService(
            _storeRepositoryMock.Object,
            _productRepositoryMock.Object,
            _overrideRepositoryMock.Object,
            _pricingZoneRepositoryMock.Object,
            null!,
            _priceChangeRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("zonePriceRepository");
    }

    [Fact]
    public void Constructor_WithNullPriceChangeRepository_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new MultiStoreService(
            _storeRepositoryMock.Object,
            _productRepositoryMock.Object,
            _overrideRepositoryMock.Object,
            _pricingZoneRepositoryMock.Object,
            _zonePriceRepositoryMock.Object,
            null!,
            _unitOfWorkMock.Object,
            _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("priceChangeRepository");
    }

    [Fact]
    public void Constructor_WithNullUnitOfWork_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new MultiStoreService(
            _storeRepositoryMock.Object,
            _productRepositoryMock.Object,
            _overrideRepositoryMock.Object,
            _pricingZoneRepositoryMock.Object,
            _zonePriceRepositoryMock.Object,
            _priceChangeRepositoryMock.Object,
            null!,
            _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("unitOfWork");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new MultiStoreService(
            _storeRepositoryMock.Object,
            _productRepositoryMock.Object,
            _overrideRepositoryMock.Object,
            _pricingZoneRepositoryMock.Object,
            _zonePriceRepositoryMock.Object,
            _priceChangeRepositoryMock.Object,
            _unitOfWorkMock.Object,
            null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region Store Management Tests

    [Fact]
    public async Task GetAllStoresAsync_ShouldReturnAllActiveStores()
    {
        // Arrange
        var stores = new List<Store>
        {
            CreateTestStore(1, "STR001", "Store 1"),
            CreateTestStore(2, "STR002", "Store 2"),
            CreateTestStore(3, "STR003", "Store 3")
        };

        _storeRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Store, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stores);

        // Act
        var result = await _service.GetAllStoresAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Select(s => s.Name).Should().Contain(new[] { "Store 1", "Store 2", "Store 3" });
    }

    [Fact]
    public async Task GetStoreByIdAsync_ShouldReturnStore_WhenExists()
    {
        // Arrange
        var store = CreateTestStore();
        _storeRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(store);

        // Act
        var result = await _service.GetStoreByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.StoreCode.Should().Be("STR001");
        result.Name.Should().Be("Test Store");
    }

    [Fact]
    public async Task GetStoreByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        _storeRepositoryMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Store?)null);

        // Act
        var result = await _service.GetStoreByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetStoreByCodeAsync_ShouldReturnStore_WhenExists()
    {
        // Arrange
        var store = CreateTestStore();
        _storeRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Store, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Store> { store });

        // Act
        var result = await _service.GetStoreByCodeAsync("STR001");

        // Assert
        result.Should().NotBeNull();
        result!.StoreCode.Should().Be("STR001");
    }

    [Fact]
    public async Task GetStoreByCodeAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        _storeRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Store, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Store>());

        // Act
        var result = await _service.GetStoreByCodeAsync("NONEXISTENT");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetHeadquartersAsync_ShouldReturnHeadquarters_WhenExists()
    {
        // Arrange
        var hq = CreateTestStore(1, "HQ001", "Headquarters", isHeadquarters: true);
        _storeRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Store, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Store> { hq });

        // Act
        var result = await _service.GetHeadquartersAsync();

        // Assert
        result.Should().NotBeNull();
        result!.IsHeadquarters.Should().BeTrue();
        result.Name.Should().Be("Headquarters");
    }

    [Fact]
    public async Task CreateStoreAsync_ShouldCreateStore_WhenCodeIsUnique()
    {
        // Arrange
        var dto = new CreateStoreDto
        {
            StoreCode = "NEW001",
            Name = "New Store",
            City = "Nairobi",
            ReceivesCentralUpdates = true
        };

        _storeRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Store, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Store>());

        _storeRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Store>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.CreateStoreAsync(dto, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result!.StoreCode.Should().Be("NEW001");
        result.Name.Should().Be("New Store");

        _storeRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Store>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateStoreAsync_ShouldReturnNull_WhenCodeIsDuplicate()
    {
        // Arrange
        var existingStore = CreateTestStore();
        var dto = new CreateStoreDto { StoreCode = "STR001", Name = "Duplicate Store" };

        _storeRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Store, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Store> { existingStore });

        // Act
        var result = await _service.CreateStoreAsync(dto, TestUserId);

        // Assert
        result.Should().BeNull();
        _storeRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Store>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStoreAsync_ShouldUpdateStore_WhenExists()
    {
        // Arrange
        var store = CreateTestStore();
        var dto = new CreateStoreDto
        {
            StoreCode = "STR001",
            Name = "Updated Store",
            City = "Mombasa",
            ReceivesCentralUpdates = false
        };

        _storeRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(store);

        _storeRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Store>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.UpdateStoreAsync(1, dto, TestUserId);

        // Assert
        result.Should().BeTrue();
        store.Name.Should().Be("Updated Store");
        store.City.Should().Be("Mombasa");
        store.ReceivesCentralUpdates.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateStoreAsync_ShouldReturnFalse_WhenNotExists()
    {
        // Arrange
        _storeRepositoryMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Store?)null);

        var dto = new CreateStoreDto { StoreCode = "STR001", Name = "Test" };

        // Act
        var result = await _service.UpdateStoreAsync(999, dto, TestUserId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateStoreAsync_ShouldDeactivateStore_WhenExists()
    {
        // Arrange
        var store = CreateTestStore();
        _storeRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(store);

        _storeRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Store>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.DeactivateStoreAsync(1, TestUserId);

        // Assert
        result.Should().BeTrue();
        store.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateStoreAsync_ShouldReturnFalse_WhenNotExists()
    {
        // Arrange
        _storeRepositoryMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Store?)null);

        // Act
        var result = await _service.DeactivateStoreAsync(999, TestUserId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Central Product Management Tests

    [Fact]
    public async Task CreateCentralProductAsync_ShouldMarkProductAsCentral()
    {
        // Arrange
        var product = CreateTestProduct(isCentralProduct: false);

        _productRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.CreateCentralProductAsync(product, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result!.IsCentralProduct.Should().BeTrue();
        result.AllowStoreOverride.Should().BeTrue();
        result.CreatedByUserId.Should().Be(TestUserId);
    }

    [Fact]
    public async Task PushProductsToStoresAsync_ShouldReturnFailure_WhenNoProductsSpecified()
    {
        // Arrange
        var dto = new PushProductsDto
        {
            ProductIds = new List<int>(),
            StoreIds = new List<int> { 1, 2 }
        };

        // Act
        var result = await _service.PushProductsToStoresAsync(dto, TestUserId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No products or stores specified");
    }

    [Fact]
    public async Task PushProductsToStoresAsync_ShouldReturnFailure_WhenNoStoresSpecified()
    {
        // Arrange
        var dto = new PushProductsDto
        {
            ProductIds = new List<int> { 1, 2 },
            StoreIds = new List<int>()
        };

        // Act
        var result = await _service.PushProductsToStoresAsync(dto, TestUserId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No products or stores specified");
    }

    [Fact]
    public async Task PushProductsToStoresAsync_ShouldReturnFailure_WhenNoValidProductsFound()
    {
        // Arrange
        var dto = new PushProductsDto
        {
            ProductIds = new List<int> { 1, 2 },
            StoreIds = new List<int> { 1 }
        };

        _productRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        // Act
        var result = await _service.PushProductsToStoresAsync(dto, TestUserId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No valid products found");
    }

    [Fact]
    public async Task PushProductsToStoresAsync_ShouldPushProducts_WhenValidData()
    {
        // Arrange
        var dto = new PushProductsDto
        {
            ProductIds = new List<int> { 1, 2 },
            StoreIds = new List<int> { 1, 2 }
        };

        var products = new List<Product>
        {
            CreateTestProduct(1, "PROD001", "Product 1"),
            CreateTestProduct(2, "PROD002", "Product 2")
        };

        var stores = new List<Store>
        {
            CreateTestStore(1, "STR001", "Store 1", receivesCentralUpdates: true),
            CreateTestStore(2, "STR002", "Store 2", receivesCentralUpdates: true)
        };

        _productRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        _storeRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Store, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stores);

        _productRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _storeRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Store>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.PushProductsToStoresAsync(dto, TestUserId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StoresUpdated.Should().Be(2);
    }

    [Fact]
    public async Task GetStoreProductsAsync_ShouldReturnProductsWithOverrides()
    {
        // Arrange
        var products = new List<Product>
        {
            CreateTestProduct(1, "PROD001", "Product 1", 100m, isCentralProduct: true),
            CreateTestProduct(2, "PROD002", "Product 2", 200m, isCentralProduct: true)
        };

        var overrides = new List<StoreProductOverride>
        {
            CreateTestOverride(1, 1, 1, overridePrice: 90m) // Product 1 has override
        };

        _productRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        _overrideRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<StoreProductOverride, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(overrides);

        // Act
        var result = await _service.GetStoreProductsAsync(1);

        // Assert
        result.Should().HaveCount(2);

        var product1 = result.First(p => p.ProductId == 1);
        product1.HasOverride.Should().BeTrue();
        product1.OverridePrice.Should().Be(90m);
        product1.EffectivePrice.Should().Be(90m);

        var product2 = result.First(p => p.ProductId == 2);
        product2.HasOverride.Should().BeFalse();
        product2.EffectivePrice.Should().Be(200m);
    }

    [Fact]
    public async Task GetEffectivePriceAsync_ShouldReturnOverridePrice_WhenOverrideExists()
    {
        // Arrange
        var product = CreateTestProduct(1, "PROD001", sellingPrice: 100m);
        var overrides = new List<StoreProductOverride>
        {
            CreateTestOverride(1, 1, 1, overridePrice: 80m)
        };

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _overrideRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<StoreProductOverride, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(overrides);

        // Act
        var result = await _service.GetEffectivePriceAsync(1, 1);

        // Assert
        result.Should().Be(80m);
    }

    [Fact]
    public async Task GetEffectivePriceAsync_ShouldReturnCentralPrice_WhenNoOverride()
    {
        // Arrange
        var product = CreateTestProduct(1, "PROD001", sellingPrice: 100m);

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _overrideRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<StoreProductOverride, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StoreProductOverride>());

        // Act
        var result = await _service.GetEffectivePriceAsync(1, 1);

        // Assert
        result.Should().Be(100m);
    }

    [Fact]
    public async Task GetEffectiveCostAsync_ShouldReturnOverrideCost_WhenOverrideExists()
    {
        // Arrange
        var product = CreateTestProduct(1, "PROD001", costPrice: 50m);
        var overrides = new List<StoreProductOverride>
        {
            CreateTestOverride(1, 1, 1, overrideCost: 40m)
        };

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _overrideRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<StoreProductOverride, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(overrides);

        // Act
        var result = await _service.GetEffectiveCostAsync(1, 1);

        // Assert
        result.Should().Be(40m);
    }

    #endregion

    #region Store Override Management Tests

    [Fact]
    public async Task GetStoreOverridesAsync_ShouldReturnOverridesForStore()
    {
        // Arrange
        var store = CreateTestStore(1, "STR001", "Store 1");
        var product = CreateTestProduct(1, "PROD001", "Product 1", sellingPrice: 100m);
        var overrides = new List<StoreProductOverride>
        {
            CreateTestOverride(1, 1, 1, overridePrice: 90m)
        };

        _overrideRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<StoreProductOverride, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(overrides);

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _storeRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(store);

        // Act
        var result = await _service.GetStoreOverridesAsync(1);

        // Assert
        result.Should().HaveCount(1);
        var o = result.First();
        o.StoreName.Should().Be("Store 1");
        o.ProductName.Should().Be("Product 1");
        o.CentralPrice.Should().Be(100m);
        o.OverridePrice.Should().Be(90m);
        o.EffectivePrice.Should().Be(90m);
    }

    [Fact]
    public async Task GetOverrideAsync_ShouldReturnOverride_WhenExists()
    {
        // Arrange
        var store = CreateTestStore(1, "STR001", "Store 1");
        var product = CreateTestProduct(1, "PROD001", "Product 1", sellingPrice: 100m);
        var overrides = new List<StoreProductOverride>
        {
            CreateTestOverride(1, 1, 1, overridePrice: 85m)
        };

        _overrideRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<StoreProductOverride, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(overrides);

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _storeRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(store);

        // Act
        var result = await _service.GetOverrideAsync(1, 1);

        // Assert
        result.Should().NotBeNull();
        result!.OverridePrice.Should().Be(85m);
    }

    [Fact]
    public async Task GetOverrideAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        _overrideRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<StoreProductOverride, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StoreProductOverride>());

        // Act
        var result = await _service.GetOverrideAsync(1, 1);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetOverrideAsync_ShouldCreateNewOverride_WhenNotExists()
    {
        // Arrange
        var product = CreateTestProduct(1, "PROD001", allowStoreOverride: true);
        var store = CreateTestStore(1, "STR001", "Store 1");
        var dto = new CreateStoreProductOverrideDto
        {
            StoreId = 1,
            ProductId = 1,
            OverridePrice = 75m,
            IsAvailable = true,
            OverrideReason = "Local promotion"
        };

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _overrideRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<StoreProductOverride, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StoreProductOverride>());

        _overrideRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<StoreProductOverride>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // After creation, return the created override
        var createdOverride = CreateTestOverride(1, 1, 1, overridePrice: 75m);
        _overrideRepositoryMock
            .SetupSequence(r => r.FindAsync(It.IsAny<Expression<Func<StoreProductOverride, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StoreProductOverride>())
            .ReturnsAsync(new List<StoreProductOverride> { createdOverride });

        _storeRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(store);

        // Act
        var result = await _service.SetOverrideAsync(dto, TestUserId);

        // Assert
        result.Should().NotBeNull();
        _overrideRepositoryMock.Verify(r => r.AddAsync(It.IsAny<StoreProductOverride>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetOverrideAsync_ShouldUpdateExistingOverride_WhenExists()
    {
        // Arrange
        var product = CreateTestProduct(1, "PROD001", allowStoreOverride: true);
        var store = CreateTestStore(1, "STR001", "Store 1");
        var existingOverride = CreateTestOverride(1, 1, 1, overridePrice: 80m);
        var dto = new CreateStoreProductOverrideDto
        {
            StoreId = 1,
            ProductId = 1,
            OverridePrice = 70m,
            IsAvailable = true
        };

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _overrideRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<StoreProductOverride, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StoreProductOverride> { existingOverride });

        _overrideRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<StoreProductOverride>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _storeRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(store);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.SetOverrideAsync(dto, TestUserId);

        // Assert
        result.Should().NotBeNull();
        existingOverride.OverridePrice.Should().Be(70m);
        _overrideRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<StoreProductOverride>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetOverrideAsync_ShouldReturnNull_WhenProductNotAllowOverride()
    {
        // Arrange
        var product = CreateTestProduct(1, "PROD001", allowStoreOverride: false);
        var dto = new CreateStoreProductOverrideDto
        {
            StoreId = 1,
            ProductId = 1,
            OverridePrice = 75m
        };

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _service.SetOverrideAsync(dto, TestUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RemoveOverrideAsync_ShouldDeactivateOverride_WhenExists()
    {
        // Arrange
        var o = CreateTestOverride(1, 1, 1, overridePrice: 80m);
        _overrideRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<StoreProductOverride, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StoreProductOverride> { o });

        _overrideRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<StoreProductOverride>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.RemoveOverrideAsync(1, 1, TestUserId);

        // Assert
        result.Should().BeTrue();
        o.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveOverrideAsync_ShouldReturnFalse_WhenNotExists()
    {
        // Arrange
        _overrideRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<StoreProductOverride, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StoreProductOverride>());

        // Act
        var result = await _service.RemoveOverrideAsync(1, 999, TestUserId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsProductAvailableAtStoreAsync_ShouldReturnTrue_WhenNoOverride()
    {
        // Arrange
        var product = CreateTestProduct(1, "PROD001", isActive: true);
        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _overrideRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<StoreProductOverride, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StoreProductOverride>());

        // Act
        var result = await _service.IsProductAvailableAtStoreAsync(1, 1);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsProductAvailableAtStoreAsync_ShouldReturnFalse_WhenOverrideMarksUnavailable()
    {
        // Arrange
        var product = CreateTestProduct(1, "PROD001", isActive: true);
        var o = CreateTestOverride(1, 1, 1, isAvailable: false);

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _overrideRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<StoreProductOverride, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StoreProductOverride> { o });

        // Act
        var result = await _service.IsProductAvailableAtStoreAsync(1, 1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsProductAvailableAtStoreAsync_ShouldReturnFalse_WhenProductNotFound()
    {
        // Arrange
        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _service.IsProductAvailableAtStoreAsync(999, 1);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Sync Management Tests

    [Fact]
    public async Task GetStoreSyncStatusesAsync_ShouldReturnStatusForAllStores()
    {
        // Arrange
        var stores = new List<Store>
        {
            CreateTestStore(1, "STR001", "Store 1", lastSyncTime: DateTime.UtcNow.AddHours(-1)),
            CreateTestStore(2, "STR002", "Store 2", lastSyncTime: DateTime.UtcNow.AddDays(-1))
        };

        var centralProducts = new List<Product>
        {
            CreateTestProduct(1, "PROD001", isCentralProduct: true),
            CreateTestProduct(2, "PROD002", isCentralProduct: true)
        };

        _storeRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Store, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stores);

        _productRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(centralProducts);

        _overrideRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<StoreProductOverride, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StoreProductOverride>());

        // Act
        var result = await _service.GetStoreSyncStatusesAsync();

        // Assert
        result.Should().HaveCount(2);
        result.All(s => s.TotalProducts == 2).Should().BeTrue();
    }

    [Fact]
    public async Task GetStoreSyncStatusAsync_ShouldReturnStatus_WhenStoreExists()
    {
        // Arrange
        var store = CreateTestStore(1, "STR001", "Store 1", lastSyncTime: DateTime.UtcNow);

        _storeRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(store);

        _productRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        _overrideRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<StoreProductOverride, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StoreProductOverride>());

        // Act
        var result = await _service.GetStoreSyncStatusAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.StoreName.Should().Be("Store 1");
    }

    [Fact]
    public async Task GetStoreSyncStatusAsync_ShouldReturnNull_WhenStoreNotExists()
    {
        // Arrange
        _storeRepositoryMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Store?)null);

        // Act
        var result = await _service.GetStoreSyncStatusAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateStoreSyncTimeAsync_ShouldUpdateSyncTime()
    {
        // Arrange
        var store = CreateTestStore(1, "STR001", lastSyncTime: null);

        _storeRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(store);

        _storeRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Store>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _service.UpdateStoreSyncTimeAsync(1);

        // Assert
        store.LastSyncTime.Should().NotBeNull();
        store.LastSyncTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetPendingSyncProductsAsync_ShouldReturnProductsModifiedAfterLastSync()
    {
        // Arrange
        var lastSync = DateTime.UtcNow.AddHours(-2);
        var store = CreateTestStore(1, "STR001", lastSyncTime: lastSync);

        var products = new List<Product>
        {
            CreateTestProduct(1, "PROD001", isCentralProduct: true, lastSyncTime: DateTime.UtcNow.AddHours(-1)), // After last sync
            CreateTestProduct(2, "PROD002", isCentralProduct: true, lastSyncTime: DateTime.UtcNow.AddHours(-3))  // Before last sync
        };

        _storeRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(store);

        // Return only products modified after the store's last sync
        _productRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(products.Where(p => p.LastSyncTime > lastSync).ToList());

        // Act
        var result = await _service.GetPendingSyncProductsAsync(1);

        // Assert
        result.Should().HaveCount(1);
        result.First().Code.Should().Be("PROD001");
    }

    [Fact]
    public async Task GetPendingSyncProductsAsync_ShouldReturnEmpty_WhenStoreNotExists()
    {
        // Arrange
        _storeRepositoryMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Store?)null);

        // Act
        var result = await _service.GetPendingSyncProductsAsync(999);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region Pricing Zone Management Tests

    [Fact]
    public async Task GetAllPricingZonesAsync_ShouldReturnAllActiveZones()
    {
        // Arrange
        var zones = new List<PricingZone>
        {
            CreateTestPricingZone(1, "ZONE001", "Zone 1"),
            CreateTestPricingZone(2, "ZONE002", "Zone 2", isDefault: true)
        };

        _pricingZoneRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<PricingZone, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(zones);

        _storeRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Store, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Store>());

        // Act
        var result = await _service.GetAllPricingZonesAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Select(z => z.Name).Should().Contain(new[] { "Zone 1", "Zone 2" });
    }

    [Fact]
    public async Task GetPricingZoneByIdAsync_ShouldReturnZone_WhenExists()
    {
        // Arrange
        var zone = CreateTestPricingZone();
        _pricingZoneRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(zone);

        _storeRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Store, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Store>());

        // Act
        var result = await _service.GetPricingZoneByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.ZoneCode.Should().Be("ZONE001");
        result.Name.Should().Be("Test Zone");
    }

    [Fact]
    public async Task GetPricingZoneByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        _pricingZoneRepositoryMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PricingZone?)null);

        // Act
        var result = await _service.GetPricingZoneByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetDefaultPricingZoneAsync_ShouldReturnDefaultZone_WhenExists()
    {
        // Arrange
        var defaultZone = CreateTestPricingZone(1, "DEFAULT", "Default Zone", isDefault: true);
        _pricingZoneRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<PricingZone, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PricingZone> { defaultZone });

        _storeRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Store, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Store>());

        // Act
        var result = await _service.GetDefaultPricingZoneAsync();

        // Assert
        result.Should().NotBeNull();
        result!.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task CreatePricingZoneAsync_ShouldCreateZone_WhenCodeIsUnique()
    {
        // Arrange
        var dto = new CreatePricingZoneDto
        {
            ZoneCode = "NEWZONE",
            Name = "New Zone",
            CurrencyCode = "KES",
            IsDefault = false
        };

        _pricingZoneRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<PricingZone, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PricingZone>());

        _pricingZoneRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<PricingZone>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.CreatePricingZoneAsync(dto, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result!.ZoneCode.Should().Be("NEWZONE");
        result.Name.Should().Be("New Zone");

        _pricingZoneRepositoryMock.Verify(r => r.AddAsync(It.IsAny<PricingZone>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreatePricingZoneAsync_ShouldReturnNull_WhenCodeIsDuplicate()
    {
        // Arrange
        var existingZone = CreateTestPricingZone();
        var dto = new CreatePricingZoneDto { ZoneCode = "ZONE001", Name = "Duplicate Zone" };

        _pricingZoneRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<PricingZone, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PricingZone> { existingZone });

        // Act
        var result = await _service.CreatePricingZoneAsync(dto, TestUserId);

        // Assert
        result.Should().BeNull();
        _pricingZoneRepositoryMock.Verify(r => r.AddAsync(It.IsAny<PricingZone>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePricingZoneAsync_ShouldUpdateZone_WhenExists()
    {
        // Arrange
        var zone = CreateTestPricingZone();
        var dto = new CreatePricingZoneDto
        {
            ZoneCode = "ZONE001",
            Name = "Updated Zone",
            CurrencyCode = "USD",
            DefaultTaxRate = 10m
        };

        _pricingZoneRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(zone);

        _pricingZoneRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<PricingZone>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.UpdatePricingZoneAsync(1, dto, TestUserId);

        // Assert
        result.Should().BeTrue();
        zone.Name.Should().Be("Updated Zone");
        zone.CurrencyCode.Should().Be("USD");
    }

    [Fact]
    public async Task UpdatePricingZoneAsync_ShouldReturnFalse_WhenNotExists()
    {
        // Arrange
        _pricingZoneRepositoryMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PricingZone?)null);

        var dto = new CreatePricingZoneDto { ZoneCode = "ZONE001", Name = "Test" };

        // Act
        var result = await _service.UpdatePricingZoneAsync(999, dto, TestUserId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AssignStoresToZoneAsync_ShouldAssignStores_WhenZoneExists()
    {
        // Arrange
        var zone = CreateTestPricingZone();
        var stores = new List<Store>
        {
            CreateTestStore(1, "STR001", "Store 1"),
            CreateTestStore(2, "STR002", "Store 2")
        };

        _pricingZoneRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(zone);

        _storeRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Store, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stores);

        _storeRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Store>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.AssignStoresToZoneAsync(1, new List<int> { 1, 2 }, TestUserId);

        // Assert
        result.Should().BeTrue();
        _storeRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Store>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task AssignStoresToZoneAsync_ShouldReturnFalse_WhenZoneNotExists()
    {
        // Arrange
        _pricingZoneRepositoryMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PricingZone?)null);

        // Act
        var result = await _service.AssignStoresToZoneAsync(999, new List<int> { 1, 2 }, TestUserId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Zone Pricing Tests

    [Fact]
    public async Task GetProductZonePricesAsync_ShouldReturnZonePrices()
    {
        // Arrange
        var product = CreateTestProduct(1, "PROD001", sellingPrice: 100m);
        var zone = CreateTestPricingZone();
        var zonePrices = new List<ZonePrice>
        {
            CreateTestZonePrice(1, 1, 1, price: 90m)
        };

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _zonePriceRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ZonePrice, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(zonePrices);

        _pricingZoneRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(zone);

        // Act
        var result = await _service.GetProductZonePricesAsync(1);

        // Assert
        result.Should().HaveCount(1);
        var zp = result.First();
        zp.Price.Should().Be(90m);
        zp.CentralPrice.Should().Be(100m);
    }

    [Fact]
    public async Task GetZoneProductPricesAsync_ShouldReturnPricesForZone()
    {
        // Arrange
        var zone = CreateTestPricingZone();
        var product = CreateTestProduct(1, "PROD001", sellingPrice: 100m);
        var zonePrices = new List<ZonePrice>
        {
            CreateTestZonePrice(1, 1, 1, price: 90m)
        };

        _pricingZoneRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(zone);

        _zonePriceRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ZonePrice, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(zonePrices);

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _service.GetZoneProductPricesAsync(1);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task SetZonePriceAsync_ShouldCreateNewZonePrice_WhenNotExists()
    {
        // Arrange
        var zone = CreateTestPricingZone();
        var product = CreateTestProduct();
        var dto = new CreateZonePriceDto
        {
            PricingZoneId = 1,
            ProductId = 1,
            Price = 85m,
            EffectiveFrom = DateTime.UtcNow,
            Reason = "Regional adjustment"
        };

        _pricingZoneRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(zone);

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _zonePriceRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ZonePrice, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ZonePrice>());

        _zonePriceRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<ZonePrice>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.SetZonePriceAsync(dto, TestUserId);

        // Assert
        result.Should().NotBeNull();
        _zonePriceRepositoryMock.Verify(r => r.AddAsync(It.IsAny<ZonePrice>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetZonePriceAsync_ShouldReturnNull_WhenZoneNotExists()
    {
        // Arrange
        _pricingZoneRepositoryMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PricingZone?)null);

        var dto = new CreateZonePriceDto { PricingZoneId = 999, ProductId = 1, Price = 85m };

        // Act
        var result = await _service.SetZonePriceAsync(dto, TestUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RemoveZonePriceAsync_ShouldDeactivateZonePrice_WhenExists()
    {
        // Arrange
        var zonePrice = CreateTestZonePrice(1, 1, 1, price: 90m);
        _zonePriceRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ZonePrice, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ZonePrice> { zonePrice });

        _zonePriceRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<ZonePrice>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.RemoveZonePriceAsync(1, 1, TestUserId);

        // Assert
        result.Should().BeTrue();
        zonePrice.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveZonePriceAsync_ShouldReturnFalse_WhenNotExists()
    {
        // Arrange
        _zonePriceRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ZonePrice, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ZonePrice>());

        // Act
        var result = await _service.RemoveZonePriceAsync(1, 999, TestUserId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetZoneEffectivePriceAsync_ShouldReturnZonePrice_WhenExists()
    {
        // Arrange
        var product = CreateTestProduct(1, "PROD001", sellingPrice: 100m);
        var zonePrice = CreateTestZonePrice(1, 1, 1, price: 85m,
            effectiveFrom: DateTime.UtcNow.AddDays(-1));

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _zonePriceRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ZonePrice, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ZonePrice> { zonePrice });

        // Act
        var result = await _service.GetZoneEffectivePriceAsync(1, 1);

        // Assert
        result.Should().Be(85m);
    }

    [Fact]
    public async Task GetZoneEffectivePriceAsync_ShouldReturnCentralPrice_WhenNoZonePrice()
    {
        // Arrange
        var product = CreateTestProduct(1, "PROD001", sellingPrice: 100m);

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _zonePriceRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ZonePrice, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ZonePrice>());

        // Act
        var result = await _service.GetZoneEffectivePriceAsync(1, 1);

        // Assert
        result.Should().Be(100m);
    }

    #endregion

    #region Scheduled Price Change Tests

    [Fact]
    public async Task GetPendingPriceChangesAsync_ShouldReturnScheduledChanges()
    {
        // Arrange
        var product = CreateTestProduct();
        var priceChanges = new List<ScheduledPriceChange>
        {
            CreateTestScheduledPriceChange(1, 1, effectiveDate: DateTime.UtcNow.AddDays(1)),
            CreateTestScheduledPriceChange(2, 1, effectiveDate: DateTime.UtcNow.AddDays(2))
        };

        _priceChangeRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ScheduledPriceChange, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(priceChanges);

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _pricingZoneRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PricingZone?)null);

        _storeRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Store?)null);

        // Act
        var result = await _service.GetPendingPriceChangesAsync();

        // Assert
        result.Should().HaveCount(2);
        result.All(c => c.Status == "Scheduled").Should().BeTrue();
    }

    [Fact]
    public async Task GetProductScheduledChangesAsync_ShouldReturnChangesForProduct()
    {
        // Arrange
        var product = CreateTestProduct();
        var priceChanges = new List<ScheduledPriceChange>
        {
            CreateTestScheduledPriceChange(1, 1)
        };

        _priceChangeRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ScheduledPriceChange, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(priceChanges);

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _pricingZoneRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PricingZone?)null);

        _storeRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Store?)null);

        // Act
        var result = await _service.GetProductScheduledChangesAsync(1);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateScheduledPriceChangeAsync_ShouldCreateChange_WhenProductExists()
    {
        // Arrange
        var product = CreateTestProduct(1, "PROD001", sellingPrice: 100m);
        var dto = new CreateScheduledPriceChangeDto
        {
            ProductId = 1,
            NewPrice = 90m,
            EffectiveDate = DateTime.UtcNow.AddDays(7),
            Reason = "Seasonal pricing"
        };

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _priceChangeRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<ScheduledPriceChange>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _pricingZoneRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PricingZone?)null);

        _storeRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Store?)null);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.CreateScheduledPriceChangeAsync(dto, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result!.OldPrice.Should().Be(100m);
        result.NewPrice.Should().Be(90m);
        _priceChangeRepositoryMock.Verify(r => r.AddAsync(It.IsAny<ScheduledPriceChange>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateScheduledPriceChangeAsync_ShouldReturnNull_WhenProductNotExists()
    {
        // Arrange
        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var dto = new CreateScheduledPriceChangeDto
        {
            ProductId = 999,
            NewPrice = 90m,
            EffectiveDate = DateTime.UtcNow.AddDays(7)
        };

        // Act
        var result = await _service.CreateScheduledPriceChangeAsync(dto, TestUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CancelScheduledPriceChangeAsync_ShouldCancelChange_WhenExists()
    {
        // Arrange
        var priceChange = CreateTestScheduledPriceChange(1, 1, status: PriceChangeStatus.Scheduled);
        _priceChangeRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(priceChange);

        _priceChangeRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<ScheduledPriceChange>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.CancelScheduledPriceChangeAsync(1, TestUserId);

        // Assert
        result.Should().BeTrue();
        priceChange.Status.Should().Be(PriceChangeStatus.Cancelled);
    }

    [Fact]
    public async Task CancelScheduledPriceChangeAsync_ShouldReturnFalse_WhenNotExists()
    {
        // Arrange
        _priceChangeRepositoryMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ScheduledPriceChange?)null);

        // Act
        var result = await _service.CancelScheduledPriceChangeAsync(999, TestUserId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CancelScheduledPriceChangeAsync_ShouldReturnFalse_WhenAlreadyApplied()
    {
        // Arrange
        var priceChange = CreateTestScheduledPriceChange(1, 1, status: PriceChangeStatus.Applied);
        _priceChangeRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(priceChange);

        // Act
        var result = await _service.CancelScheduledPriceChangeAsync(1, TestUserId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ApplyDuePriceChangesAsync_ShouldApplyDueChanges()
    {
        // Arrange
        var product = CreateTestProduct(1, "PROD001", sellingPrice: 100m);
        var dueChanges = new List<ScheduledPriceChange>
        {
            CreateTestScheduledPriceChange(1, 1, oldPrice: 100m, newPrice: 90m,
                effectiveDate: DateTime.UtcNow.AddHours(-1), status: PriceChangeStatus.Scheduled)
        };

        _priceChangeRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ScheduledPriceChange, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dueChanges);

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _productRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _priceChangeRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<ScheduledPriceChange>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.ApplyDuePriceChangesAsync(TestUserId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.AppliedChanges.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task ApplyDuePriceChangesAsync_ShouldReturnSuccess_WhenNoChanges()
    {
        // Arrange
        _priceChangeRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ScheduledPriceChange, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScheduledPriceChange>());

        // Act
        var result = await _service.ApplyDuePriceChangesAsync(TestUserId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.TotalChanges.Should().Be(0);
        result.AppliedChanges.Should().Be(0);
    }

    [Fact]
    public async Task GetProductPricingSummaryAsync_ShouldReturnSummary_WhenProductExists()
    {
        // Arrange
        var product = CreateTestProduct(1, "PROD001", sellingPrice: 100m, costPrice: 50m);
        var zone = CreateTestPricingZone();
        var zonePrices = new List<ZonePrice>
        {
            CreateTestZonePrice(1, 1, 1, price: 90m)
        };
        var scheduledChanges = new List<ScheduledPriceChange>
        {
            CreateTestScheduledPriceChange(1, 1, effectiveDate: DateTime.UtcNow.AddDays(7))
        };

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _zonePriceRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ZonePrice, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(zonePrices);

        _pricingZoneRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(zone);

        _priceChangeRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ScheduledPriceChange, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(scheduledChanges);

        _storeRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Store?)null);

        // Act
        var result = await _service.GetProductPricingSummaryAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.ProductCode.Should().Be("PROD001");
        result.CentralPrice.Should().Be(100m);
        result.CentralCost.Should().Be(50m);
        result.ZonePrices.Should().HaveCount(1);
        result.ScheduledChanges.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetProductPricingSummaryAsync_ShouldReturnNull_WhenProductNotExists()
    {
        // Arrange
        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _service.GetProductPricingSummaryAsync(999);

        // Assert
        result.Should().BeNull();
    }

    #endregion
}
