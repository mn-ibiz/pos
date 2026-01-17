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
/// Unit tests for the CentralPromotionService class.
/// Tests cover promotion management, deployment operations, redemption tracking,
/// and dashboard/reporting functionality.
/// </summary>
public class CentralPromotionServiceTests
{
    private readonly Mock<IRepository<CentralPromotion>> _promotionRepositoryMock;
    private readonly Mock<IRepository<PromotionProduct>> _promotionProductRepositoryMock;
    private readonly Mock<IRepository<PromotionCategory>> _promotionCategoryRepositoryMock;
    private readonly Mock<IRepository<PromotionDeployment>> _deploymentRepositoryMock;
    private readonly Mock<IRepository<DeploymentStore>> _deploymentStoreRepositoryMock;
    private readonly Mock<IRepository<DeploymentZone>> _deploymentZoneRepositoryMock;
    private readonly Mock<IRepository<PromotionRedemption>> _redemptionRepositoryMock;
    private readonly Mock<IRepository<Store>> _storeRepositoryMock;
    private readonly Mock<IRepository<PricingZone>> _zoneRepositoryMock;
    private readonly Mock<IRepository<Product>> _productRepositoryMock;
    private readonly Mock<IRepository<Category>> _categoryRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<CentralPromotionService>> _loggerMock;
    private readonly CentralPromotionService _service;

    private const int TestUserId = 1;

    public CentralPromotionServiceTests()
    {
        _promotionRepositoryMock = new Mock<IRepository<CentralPromotion>>();
        _promotionProductRepositoryMock = new Mock<IRepository<PromotionProduct>>();
        _promotionCategoryRepositoryMock = new Mock<IRepository<PromotionCategory>>();
        _deploymentRepositoryMock = new Mock<IRepository<PromotionDeployment>>();
        _deploymentStoreRepositoryMock = new Mock<IRepository<DeploymentStore>>();
        _deploymentZoneRepositoryMock = new Mock<IRepository<DeploymentZone>>();
        _redemptionRepositoryMock = new Mock<IRepository<PromotionRedemption>>();
        _storeRepositoryMock = new Mock<IRepository<Store>>();
        _zoneRepositoryMock = new Mock<IRepository<PricingZone>>();
        _productRepositoryMock = new Mock<IRepository<Product>>();
        _categoryRepositoryMock = new Mock<IRepository<Category>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<CentralPromotionService>>();

        _service = new CentralPromotionService(
            _promotionRepositoryMock.Object,
            _promotionProductRepositoryMock.Object,
            _promotionCategoryRepositoryMock.Object,
            _deploymentRepositoryMock.Object,
            _deploymentStoreRepositoryMock.Object,
            _deploymentZoneRepositoryMock.Object,
            _redemptionRepositoryMock.Object,
            _storeRepositoryMock.Object,
            _zoneRepositoryMock.Object,
            _productRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    #region Helper Methods

    private static CentralPromotion CreateTestPromotion(
        int id = 1,
        string code = "PROMO001",
        string name = "Test Promotion",
        PromotionType type = PromotionType.PercentageDiscount,
        PromotionStatus status = PromotionStatus.Active,
        decimal? discountPercent = 10m,
        decimal? discountAmount = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool isActive = true,
        bool isCentrallyManaged = true)
    {
        return new CentralPromotion
        {
            Id = id,
            PromotionCode = code,
            Name = name,
            Description = "Test promotion description",
            Type = type,
            Status = status,
            DiscountPercent = discountPercent,
            DiscountAmount = discountAmount,
            MinQuantity = 1,
            StartDate = startDate ?? DateTime.UtcNow.AddDays(-1),
            EndDate = endDate ?? DateTime.UtcNow.AddDays(30),
            Priority = 100,
            IsCentrallyManaged = isCentrallyManaged,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static Store CreateTestStore(
        int id = 1,
        string storeCode = "STR001",
        string name = "Test Store",
        bool receivesCentralUpdates = true,
        int? pricingZoneId = null,
        bool isActive = true)
    {
        return new Store
        {
            Id = id,
            StoreCode = storeCode,
            Name = name,
            Address = "123 Test Street",
            City = "Nairobi",
            ReceivesCentralUpdates = receivesCentralUpdates,
            PricingZoneId = pricingZoneId,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static Product CreateTestProduct(
        int id = 1,
        string code = "PROD001",
        string name = "Test Product",
        decimal sellingPrice = 100m,
        int? categoryId = null,
        bool isActive = true)
    {
        return new Product
        {
            Id = id,
            Code = code,
            Name = name,
            SellingPrice = sellingPrice,
            CategoryId = categoryId,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static PromotionDeployment CreateTestDeployment(
        int id = 1,
        int promotionId = 1,
        DeploymentScope scope = DeploymentScope.AllStores,
        DeploymentStatus status = DeploymentStatus.Completed,
        int storesDeployedCount = 5,
        bool isActive = true)
    {
        return new PromotionDeployment
        {
            Id = id,
            PromotionId = promotionId,
            Scope = scope,
            Status = status,
            DeployedAt = DateTime.UtcNow,
            CompletedAt = status == DeploymentStatus.Completed ? DateTime.UtcNow : null,
            StoresDeployedCount = storesDeployedCount,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static DeploymentStore CreateTestDeploymentStore(
        int id = 1,
        int deploymentId = 1,
        int storeId = 1,
        DeploymentStatus status = DeploymentStatus.Completed,
        bool isActive = true)
    {
        return new DeploymentStore
        {
            Id = id,
            DeploymentId = deploymentId,
            StoreId = storeId,
            Status = status,
            SyncedAt = status == DeploymentStatus.Completed ? DateTime.UtcNow : null,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static PromotionRedemption CreateTestRedemption(
        int id = 1,
        int promotionId = 1,
        int storeId = 1,
        int receiptId = 100,
        decimal originalAmount = 100m,
        decimal discountGiven = 10m,
        bool isVoided = false,
        bool isActive = true)
    {
        return new PromotionRedemption
        {
            Id = id,
            PromotionId = promotionId,
            StoreId = storeId,
            ReceiptId = receiptId,
            OriginalAmount = originalAmount,
            DiscountGiven = discountGiven,
            FinalAmount = originalAmount - discountGiven,
            QuantityApplied = 1,
            RedeemedAt = DateTime.UtcNow,
            IsVoided = isVoided,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static PromotionProduct CreateTestPromotionProduct(
        int id = 1,
        int promotionId = 1,
        int productId = 1,
        bool isActive = true)
    {
        return new PromotionProduct
        {
            Id = id,
            PromotionId = promotionId,
            ProductId = productId,
            IsQualifyingProduct = true,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static PromotionCategory CreateTestPromotionCategory(
        int id = 1,
        int promotionId = 1,
        int categoryId = 1,
        bool isActive = true)
    {
        return new PromotionCategory
        {
            Id = id,
            PromotionId = promotionId,
            CategoryId = categoryId,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullPromotionRepository_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var action = () => new CentralPromotionService(
            null!,
            _promotionProductRepositoryMock.Object,
            _promotionCategoryRepositoryMock.Object,
            _deploymentRepositoryMock.Object,
            _deploymentStoreRepositoryMock.Object,
            _deploymentZoneRepositoryMock.Object,
            _redemptionRepositoryMock.Object,
            _storeRepositoryMock.Object,
            _zoneRepositoryMock.Object,
            _productRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("promotionRepository");
    }

    [Fact]
    public void Constructor_WithNullPromotionProductRepository_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var action = () => new CentralPromotionService(
            _promotionRepositoryMock.Object,
            null!,
            _promotionCategoryRepositoryMock.Object,
            _deploymentRepositoryMock.Object,
            _deploymentStoreRepositoryMock.Object,
            _deploymentZoneRepositoryMock.Object,
            _redemptionRepositoryMock.Object,
            _storeRepositoryMock.Object,
            _zoneRepositoryMock.Object,
            _productRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("promotionProductRepository");
    }

    [Fact]
    public void Constructor_WithNullUnitOfWork_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var action = () => new CentralPromotionService(
            _promotionRepositoryMock.Object,
            _promotionProductRepositoryMock.Object,
            _promotionCategoryRepositoryMock.Object,
            _deploymentRepositoryMock.Object,
            _deploymentStoreRepositoryMock.Object,
            _deploymentZoneRepositoryMock.Object,
            _redemptionRepositoryMock.Object,
            _storeRepositoryMock.Object,
            _zoneRepositoryMock.Object,
            _productRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            null!,
            _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("unitOfWork");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var action = () => new CentralPromotionService(
            _promotionRepositoryMock.Object,
            _promotionProductRepositoryMock.Object,
            _promotionCategoryRepositoryMock.Object,
            _deploymentRepositoryMock.Object,
            _deploymentStoreRepositoryMock.Object,
            _deploymentZoneRepositoryMock.Object,
            _redemptionRepositoryMock.Object,
            _storeRepositoryMock.Object,
            _zoneRepositoryMock.Object,
            _productRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _unitOfWorkMock.Object,
            null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region GetAllPromotionsAsync Tests

    [Fact]
    public async Task GetAllPromotionsAsync_WithNoFilter_ReturnsAllActivePromotions()
    {
        // Arrange
        var promotions = new List<CentralPromotion>
        {
            CreateTestPromotion(1, "PROMO1"),
            CreateTestPromotion(2, "PROMO2")
        };

        _promotionRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<CentralPromotion, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotions);

        SetupEmptyCollectionMocks();

        // Act
        var result = await _service.GetAllPromotionsAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllPromotionsAsync_WithStatusFilter_ReturnsFilteredPromotions()
    {
        // Arrange
        var promotions = new List<CentralPromotion>
        {
            CreateTestPromotion(1, "PROMO1", status: PromotionStatus.Active),
            CreateTestPromotion(2, "PROMO2", status: PromotionStatus.Paused)
        };

        _promotionRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<CentralPromotion, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotions);

        SetupEmptyCollectionMocks();

        var query = new PromotionQueryDto { Status = PromotionStatus.Active };

        // Act
        var result = await _service.GetAllPromotionsAsync(query);

        // Assert
        result.Should().HaveCount(1);
        result.First().Status.Should().Be(PromotionStatus.Active);
    }

    [Fact]
    public async Task GetAllPromotionsAsync_WithSearchTerm_ReturnsMatchingPromotions()
    {
        // Arrange
        var promotions = new List<CentralPromotion>
        {
            CreateTestPromotion(1, "SUMMER10", "Summer Sale"),
            CreateTestPromotion(2, "WINTER20", "Winter Deal")
        };

        _promotionRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<CentralPromotion, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotions);

        SetupEmptyCollectionMocks();

        var query = new PromotionQueryDto { SearchTerm = "Summer" };

        // Act
        var result = await _service.GetAllPromotionsAsync(query);

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Contain("Summer");
    }

    #endregion

    #region GetPromotionByIdAsync Tests

    [Fact]
    public async Task GetPromotionByIdAsync_WithValidId_ReturnsPromotion()
    {
        // Arrange
        var promotion = CreateTestPromotion();

        _promotionRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotion);

        SetupEmptyCollectionMocks();

        // Act
        var result = await _service.GetPromotionByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetPromotionByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        _promotionRepositoryMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CentralPromotion?)null);

        // Act
        var result = await _service.GetPromotionByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPromotionByIdAsync_WithInactivePromotion_ReturnsNull()
    {
        // Arrange
        var promotion = CreateTestPromotion(isActive: false);

        _promotionRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotion);

        // Act
        var result = await _service.GetPromotionByIdAsync(1);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetPromotionByCodeAsync Tests

    [Fact]
    public async Task GetPromotionByCodeAsync_WithValidCode_ReturnsPromotion()
    {
        // Arrange
        var promotion = CreateTestPromotion(code: "SUMMER10");

        _promotionRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<CentralPromotion, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CentralPromotion> { promotion });

        SetupEmptyCollectionMocks();

        // Act
        var result = await _service.GetPromotionByCodeAsync("SUMMER10");

        // Assert
        result.Should().NotBeNull();
        result!.PromotionCode.Should().Be("SUMMER10");
    }

    [Fact]
    public async Task GetPromotionByCodeAsync_WithInvalidCode_ReturnsNull()
    {
        // Arrange
        _promotionRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<CentralPromotion, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CentralPromotion>());

        // Act
        var result = await _service.GetPromotionByCodeAsync("INVALID");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreatePromotionAsync Tests

    [Fact]
    public async Task CreatePromotionAsync_WithValidDto_CreatesPromotion()
    {
        // Arrange
        var dto = new CreatePromotionDto
        {
            PromotionCode = "NEWPROMO",
            Name = "New Promotion",
            Type = PromotionType.PercentageDiscount,
            DiscountPercent = 15m,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        _promotionRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<CentralPromotion, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CentralPromotion>());

        _promotionRepositoryMock.Setup(r => r.AddAsync(
            It.IsAny<CentralPromotion>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        SetupEmptyCollectionMocks();

        // Act
        var result = await _service.CreatePromotionAsync(dto, TestUserId);

        // Assert
        result.Should().NotBeNull();
        _promotionRepositoryMock.Verify(r => r.AddAsync(
            It.Is<CentralPromotion>(p => p.PromotionCode == "NEWPROMO"),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task CreatePromotionAsync_WithDuplicateCode_ReturnsNull()
    {
        // Arrange
        var existingPromotion = CreateTestPromotion(code: "EXISTING");

        _promotionRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<CentralPromotion, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CentralPromotion> { existingPromotion });

        var dto = new CreatePromotionDto
        {
            PromotionCode = "EXISTING",
            Name = "New Promotion"
        };

        // Act
        var result = await _service.CreatePromotionAsync(dto, TestUserId);

        // Assert
        result.Should().BeNull();
        _promotionRepositoryMock.Verify(r => r.AddAsync(
            It.IsAny<CentralPromotion>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreatePromotionAsync_WithProducts_AddsProductAssociations()
    {
        // Arrange
        var dto = new CreatePromotionDto
        {
            PromotionCode = "PRODPROMO",
            Name = "Product Promotion",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            ProductIds = new List<int> { 1, 2, 3 }
        };

        _promotionRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<CentralPromotion, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CentralPromotion>());

        _promotionRepositoryMock.Setup(r => r.AddAsync(
            It.IsAny<CentralPromotion>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        SetupEmptyCollectionMocks();

        // Act
        var result = await _service.CreatePromotionAsync(dto, TestUserId);

        // Assert
        result.Should().NotBeNull();
        _promotionProductRepositoryMock.Verify(r => r.AddAsync(
            It.IsAny<PromotionProduct>(),
            It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    #endregion

    #region UpdatePromotionAsync Tests

    [Fact]
    public async Task UpdatePromotionAsync_WithValidPromotion_UpdatesSuccessfully()
    {
        // Arrange
        var promotion = CreateTestPromotion();
        var dto = new CreatePromotionDto
        {
            Name = "Updated Name",
            DiscountPercent = 20m,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(60)
        };

        _promotionRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotion);

        _promotionRepositoryMock.Setup(r => r.UpdateAsync(
            It.IsAny<CentralPromotion>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdatePromotionAsync(1, dto, TestUserId);

        // Assert
        result.Should().BeTrue();
        _promotionRepositoryMock.Verify(r => r.UpdateAsync(
            It.Is<CentralPromotion>(p => p.Name == "Updated Name"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePromotionAsync_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        _promotionRepositoryMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CentralPromotion?)null);

        var dto = new CreatePromotionDto { Name = "Test" };

        // Act
        var result = await _service.UpdatePromotionAsync(999, dto, TestUserId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ActivatePromotionAsync Tests

    [Fact]
    public async Task ActivatePromotionAsync_WithValidPromotion_ActivatesSuccessfully()
    {
        // Arrange
        var promotion = CreateTestPromotion(status: PromotionStatus.Draft);

        _promotionRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotion);

        _promotionRepositoryMock.Setup(r => r.UpdateAsync(
            It.IsAny<CentralPromotion>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ActivatePromotionAsync(1, TestUserId);

        // Assert
        result.Should().BeTrue();
        _promotionRepositoryMock.Verify(r => r.UpdateAsync(
            It.Is<CentralPromotion>(p => p.Status == PromotionStatus.Active),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ActivatePromotionAsync_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        _promotionRepositoryMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CentralPromotion?)null);

        // Act
        var result = await _service.ActivatePromotionAsync(999, TestUserId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region PausePromotionAsync Tests

    [Fact]
    public async Task PausePromotionAsync_WithActivePromotion_PausesSuccessfully()
    {
        // Arrange
        var promotion = CreateTestPromotion(status: PromotionStatus.Active);

        _promotionRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotion);

        _promotionRepositoryMock.Setup(r => r.UpdateAsync(
            It.IsAny<CentralPromotion>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.PausePromotionAsync(1, TestUserId);

        // Assert
        result.Should().BeTrue();
        _promotionRepositoryMock.Verify(r => r.UpdateAsync(
            It.Is<CentralPromotion>(p => p.Status == PromotionStatus.Paused),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region CancelPromotionAsync Tests

    [Fact]
    public async Task CancelPromotionAsync_WithValidPromotion_CancelsSuccessfully()
    {
        // Arrange
        var promotion = CreateTestPromotion();

        _promotionRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotion);

        _promotionRepositoryMock.Setup(r => r.UpdateAsync(
            It.IsAny<CentralPromotion>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CancelPromotionAsync(1, TestUserId);

        // Assert
        result.Should().BeTrue();
        _promotionRepositoryMock.Verify(r => r.UpdateAsync(
            It.Is<CentralPromotion>(p =>
                p.Status == PromotionStatus.Cancelled && p.IsActive == false),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region AddProductsToPromotionAsync Tests

    [Fact]
    public async Task AddProductsToPromotionAsync_WithValidProducts_AddsSuccessfully()
    {
        // Arrange
        var promotion = CreateTestPromotion();
        var productIds = new List<int> { 1, 2, 3 };

        _promotionRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotion);

        _promotionProductRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<PromotionProduct, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PromotionProduct>());

        // Act
        var result = await _service.AddProductsToPromotionAsync(1, productIds, TestUserId);

        // Assert
        result.Should().BeTrue();
        _promotionProductRepositoryMock.Verify(r => r.AddAsync(
            It.IsAny<PromotionProduct>(),
            It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task AddProductsToPromotionAsync_WithExistingProducts_SkipsDuplicates()
    {
        // Arrange
        var promotion = CreateTestPromotion();
        var existingProducts = new List<PromotionProduct>
        {
            CreateTestPromotionProduct(1, 1, 1)
        };

        _promotionRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotion);

        _promotionProductRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<PromotionProduct, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProducts);

        var productIds = new List<int> { 1, 2 }; // Product 1 already exists

        // Act
        var result = await _service.AddProductsToPromotionAsync(1, productIds, TestUserId);

        // Assert
        result.Should().BeTrue();
        _promotionProductRepositoryMock.Verify(r => r.AddAsync(
            It.IsAny<PromotionProduct>(),
            It.IsAny<CancellationToken>()), Times.Once); // Only product 2 should be added
    }

    #endregion

    #region RemoveProductsFromPromotionAsync Tests

    [Fact]
    public async Task RemoveProductsFromPromotionAsync_WithExistingProducts_SoftDeletesSuccessfully()
    {
        // Arrange
        var products = new List<PromotionProduct>
        {
            CreateTestPromotionProduct(1, 1, 1),
            CreateTestPromotionProduct(2, 1, 2)
        };

        _promotionProductRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<PromotionProduct, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        var productIds = new List<int> { 1, 2 };

        // Act
        var result = await _service.RemoveProductsFromPromotionAsync(1, productIds, TestUserId);

        // Assert
        result.Should().BeTrue();
        _promotionProductRepositoryMock.Verify(r => r.UpdateAsync(
            It.Is<PromotionProduct>(pp => pp.IsActive == false),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    #endregion

    #region AddCategoriesToPromotionAsync Tests

    [Fact]
    public async Task AddCategoriesToPromotionAsync_WithValidCategories_AddsSuccessfully()
    {
        // Arrange
        var promotion = CreateTestPromotion();
        var categoryIds = new List<int> { 1, 2 };

        _promotionRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotion);

        _promotionCategoryRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<PromotionCategory, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PromotionCategory>());

        // Act
        var result = await _service.AddCategoriesToPromotionAsync(1, categoryIds, TestUserId);

        // Assert
        result.Should().BeTrue();
        _promotionCategoryRepositoryMock.Verify(r => r.AddAsync(
            It.IsAny<PromotionCategory>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    #endregion

    #region DeployPromotionAsync Tests

    [Fact]
    public async Task DeployPromotionAsync_WithAllStoresScope_DeploysToAllStores()
    {
        // Arrange
        var promotion = CreateTestPromotion();
        var stores = new List<Store>
        {
            CreateTestStore(1, "STR001"),
            CreateTestStore(2, "STR002"),
            CreateTestStore(3, "STR003")
        };

        _promotionRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotion);

        _storeRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Store, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(stores);

        _deploymentRepositoryMock.Setup(r => r.AddAsync(
            It.IsAny<PromotionDeployment>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var dto = new DeployPromotionDto
        {
            PromotionId = 1,
            Scope = DeploymentScope.AllStores
        };

        // Act
        var result = await _service.DeployPromotionAsync(dto, TestUserId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StoresDeployed.Should().Be(3);
        _deploymentStoreRepositoryMock.Verify(r => r.AddAsync(
            It.IsAny<DeploymentStore>(),
            It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task DeployPromotionAsync_WithByZoneScope_DeploysToZoneStores()
    {
        // Arrange
        var promotion = CreateTestPromotion();
        var stores = new List<Store>
        {
            CreateTestStore(1, "STR001", pricingZoneId: 1),
            CreateTestStore(2, "STR002", pricingZoneId: 1)
        };

        _promotionRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotion);

        _storeRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Store, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(stores);

        _deploymentRepositoryMock.Setup(r => r.AddAsync(
            It.IsAny<PromotionDeployment>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var dto = new DeployPromotionDto
        {
            PromotionId = 1,
            Scope = DeploymentScope.ByZone,
            ZoneIds = new List<int> { 1 }
        };

        // Act
        var result = await _service.DeployPromotionAsync(dto, TestUserId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StoresDeployed.Should().Be(2);
    }

    [Fact]
    public async Task DeployPromotionAsync_WithByZoneScopeNoZones_ReturnsFailure()
    {
        // Arrange
        var promotion = CreateTestPromotion();

        _promotionRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotion);

        var dto = new DeployPromotionDto
        {
            PromotionId = 1,
            Scope = DeploymentScope.ByZone,
            ZoneIds = null
        };

        // Act
        var result = await _service.DeployPromotionAsync(dto, TestUserId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No zones specified");
    }

    [Fact]
    public async Task DeployPromotionAsync_WithIndividualStoresScope_DeploysToSpecificStores()
    {
        // Arrange
        var promotion = CreateTestPromotion();
        var stores = new List<Store>
        {
            CreateTestStore(1, "STR001"),
            CreateTestStore(2, "STR002")
        };

        _promotionRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotion);

        _storeRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Store, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(stores);

        _deploymentRepositoryMock.Setup(r => r.AddAsync(
            It.IsAny<PromotionDeployment>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var dto = new DeployPromotionDto
        {
            PromotionId = 1,
            Scope = DeploymentScope.IndividualStores,
            StoreIds = new List<int> { 1, 2 }
        };

        // Act
        var result = await _service.DeployPromotionAsync(dto, TestUserId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StoresDeployed.Should().Be(2);
    }

    [Fact]
    public async Task DeployPromotionAsync_WithInvalidPromotion_ReturnsFailure()
    {
        // Arrange
        _promotionRepositoryMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CentralPromotion?)null);

        var dto = new DeployPromotionDto
        {
            PromotionId = 999,
            Scope = DeploymentScope.AllStores
        };

        // Act
        var result = await _service.DeployPromotionAsync(dto, TestUserId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task DeployPromotionAsync_WithNoMatchingStores_ReturnsFailure()
    {
        // Arrange
        var promotion = CreateTestPromotion();

        _promotionRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotion);

        _storeRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Store, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Store>());

        var dto = new DeployPromotionDto
        {
            PromotionId = 1,
            Scope = DeploymentScope.AllStores
        };

        // Act
        var result = await _service.DeployPromotionAsync(dto, TestUserId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No valid stores");
    }

    #endregion

    #region GetPromotionDeploymentsAsync Tests

    [Fact]
    public async Task GetPromotionDeploymentsAsync_WithValidPromotionId_ReturnsDeployments()
    {
        // Arrange
        var deployments = new List<PromotionDeployment>
        {
            CreateTestDeployment(1, 1),
            CreateTestDeployment(2, 1)
        };

        _deploymentRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<PromotionDeployment, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(deployments);

        _promotionRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestPromotion());

        _deploymentStoreRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<DeploymentStore, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DeploymentStore>());

        // Act
        var result = await _service.GetPromotionDeploymentsAsync(1);

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region GetDeploymentByIdAsync Tests

    [Fact]
    public async Task GetDeploymentByIdAsync_WithValidId_ReturnsDeployment()
    {
        // Arrange
        var deployment = CreateTestDeployment();

        _deploymentRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deployment);

        _promotionRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestPromotion());

        _deploymentStoreRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<DeploymentStore, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DeploymentStore>());

        // Act
        var result = await _service.GetDeploymentByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetDeploymentByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        _deploymentRepositoryMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PromotionDeployment?)null);

        // Act
        var result = await _service.GetDeploymentByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetPendingDeploymentsAsync Tests

    [Fact]
    public async Task GetPendingDeploymentsAsync_ReturnsPendingAndInProgressDeployments()
    {
        // Arrange
        var deployments = new List<PromotionDeployment>
        {
            CreateTestDeployment(1, 1, status: DeploymentStatus.Pending),
            CreateTestDeployment(2, 1, status: DeploymentStatus.InProgress)
        };

        _deploymentRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<PromotionDeployment, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(deployments);

        _promotionRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestPromotion());

        _deploymentStoreRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<DeploymentStore, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DeploymentStore>());

        // Act
        var result = await _service.GetPendingDeploymentsAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region UpdateDeploymentStoreStatusAsync Tests

    [Fact]
    public async Task UpdateDeploymentStoreStatusAsync_WithValidIds_UpdatesSuccessfully()
    {
        // Arrange
        var deploymentStore = CreateTestDeploymentStore(status: DeploymentStatus.Pending);

        _deploymentStoreRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<DeploymentStore, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DeploymentStore> { deploymentStore });

        // Act
        var result = await _service.UpdateDeploymentStoreStatusAsync(1, 1, DeploymentStatus.Completed);

        // Assert
        result.Should().BeTrue();
        _deploymentStoreRepositoryMock.Verify(r => r.UpdateAsync(
            It.Is<DeploymentStore>(ds => ds.Status == DeploymentStatus.Completed),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateDeploymentStoreStatusAsync_WithInvalidIds_ReturnsFalse()
    {
        // Arrange
        _deploymentStoreRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<DeploymentStore, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DeploymentStore>());

        // Act
        var result = await _service.UpdateDeploymentStoreStatusAsync(999, 999, DeploymentStatus.Completed);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region RetryDeploymentForStoreAsync Tests

    [Fact]
    public async Task RetryDeploymentForStoreAsync_WithValidIds_RetriesSuccessfully()
    {
        // Arrange
        var deploymentStore = CreateTestDeploymentStore(status: DeploymentStatus.Failed);

        _deploymentStoreRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<DeploymentStore, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DeploymentStore> { deploymentStore });

        // Act
        var result = await _service.RetryDeploymentForStoreAsync(1, 1, TestUserId);

        // Assert
        result.Should().BeTrue();
        _deploymentStoreRepositoryMock.Verify(r => r.UpdateAsync(
            It.Is<DeploymentStore>(ds =>
                ds.Status == DeploymentStatus.Pending &&
                ds.RetryCount == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region RollbackDeploymentAsync Tests

    [Fact]
    public async Task RollbackDeploymentAsync_WithValidId_RollsBackSuccessfully()
    {
        // Arrange
        var deployment = CreateTestDeployment(status: DeploymentStatus.Completed);

        _deploymentRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deployment);

        // Act
        var result = await _service.RollbackDeploymentAsync(1, TestUserId);

        // Assert
        result.Should().BeTrue();
        _deploymentRepositoryMock.Verify(r => r.UpdateAsync(
            It.Is<PromotionDeployment>(d => d.Status == DeploymentStatus.RolledBack),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region RecordRedemptionAsync Tests

    [Fact]
    public async Task RecordRedemptionAsync_WithActivePromotion_RecordsSuccessfully()
    {
        // Arrange
        var promotion = CreateTestPromotion(
            startDate: DateTime.UtcNow.AddDays(-1),
            endDate: DateTime.UtcNow.AddDays(30));

        _promotionRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotion);

        _redemptionRepositoryMock.Setup(r => r.AddAsync(
            It.IsAny<PromotionRedemption>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _storeRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestStore());

        var dto = new RecordRedemptionDto
        {
            PromotionId = 1,
            StoreId = 1,
            ReceiptId = 100,
            OriginalAmount = 100m,
            DiscountGiven = 10m,
            QuantityApplied = 1
        };

        // Act
        var result = await _service.RecordRedemptionAsync(dto, TestUserId);

        // Assert
        result.Should().NotBeNull();
        _redemptionRepositoryMock.Verify(r => r.AddAsync(
            It.Is<PromotionRedemption>(pr =>
                pr.OriginalAmount == 100m &&
                pr.DiscountGiven == 10m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecordRedemptionAsync_WithInactivePromotion_ReturnsNull()
    {
        // Arrange
        _promotionRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CentralPromotion?)null);

        var dto = new RecordRedemptionDto
        {
            PromotionId = 1,
            StoreId = 1,
            ReceiptId = 100,
            OriginalAmount = 100m,
            DiscountGiven = 10m
        };

        // Act
        var result = await _service.RecordRedemptionAsync(dto, TestUserId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region VoidRedemptionAsync Tests

    [Fact]
    public async Task VoidRedemptionAsync_WithValidRedemption_VoidsSuccessfully()
    {
        // Arrange
        var redemption = CreateTestRedemption();

        _redemptionRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(redemption);

        // Act
        var result = await _service.VoidRedemptionAsync(1, "Customer returned item", TestUserId);

        // Assert
        result.Should().BeTrue();
        _redemptionRepositoryMock.Verify(r => r.UpdateAsync(
            It.Is<PromotionRedemption>(pr =>
                pr.IsVoided == true &&
                pr.VoidReason == "Customer returned item"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VoidRedemptionAsync_WithAlreadyVoidedRedemption_ReturnsFalse()
    {
        // Arrange
        var redemption = CreateTestRedemption(isVoided: true);

        _redemptionRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(redemption);

        // Act
        var result = await _service.VoidRedemptionAsync(1, "Test reason", TestUserId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetPromotionRedemptionsAsync Tests

    [Fact]
    public async Task GetPromotionRedemptionsAsync_WithValidPromotionId_ReturnsRedemptions()
    {
        // Arrange
        var redemptions = new List<PromotionRedemption>
        {
            CreateTestRedemption(1),
            CreateTestRedemption(2)
        };

        _redemptionRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<PromotionRedemption, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(redemptions);

        _promotionRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestPromotion());

        _storeRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestStore());

        // Act
        var result = await _service.GetPromotionRedemptionsAsync(1);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPromotionRedemptionsAsync_WithDateFilter_ReturnsFilteredRedemptions()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var redemptions = new List<PromotionRedemption>
        {
            CreateTestRedemption(1),
            CreateTestRedemption(2)
        };
        redemptions[0].RedeemedAt = now.AddDays(-5);
        redemptions[1].RedeemedAt = now.AddDays(-10);

        _redemptionRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<PromotionRedemption, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(redemptions);

        _promotionRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestPromotion());

        _storeRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestStore());

        // Act
        var result = await _service.GetPromotionRedemptionsAsync(1, now.AddDays(-7), now);

        // Assert
        result.Should().HaveCount(1);
    }

    #endregion

    #region GetStoreRedemptionsAsync Tests

    [Fact]
    public async Task GetStoreRedemptionsAsync_WithValidStoreId_ReturnsRedemptions()
    {
        // Arrange
        var redemptions = new List<PromotionRedemption>
        {
            CreateTestRedemption(1, storeId: 1),
            CreateTestRedemption(2, storeId: 1)
        };

        _redemptionRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<PromotionRedemption, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(redemptions);

        _promotionRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestPromotion());

        _storeRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestStore());

        // Act
        var result = await _service.GetStoreRedemptionsAsync(1);

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region GetRedemptionCountAsync Tests

    [Fact]
    public async Task GetRedemptionCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var redemptions = new List<PromotionRedemption>
        {
            CreateTestRedemption(1),
            CreateTestRedemption(2),
            CreateTestRedemption(3)
        };

        _redemptionRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<PromotionRedemption, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(redemptions);

        // Act
        var result = await _service.GetRedemptionCountAsync(1);

        // Assert
        result.Should().Be(3);
    }

    #endregion

    #region GetPromotionDashboardAsync Tests

    [Fact]
    public async Task GetPromotionDashboardAsync_WithValidId_ReturnsDashboardData()
    {
        // Arrange
        var promotion = CreateTestPromotion();
        var redemptions = new List<PromotionRedemption>
        {
            CreateTestRedemption(1, discountGiven: 10m),
            CreateTestRedemption(2, discountGiven: 15m)
        };
        var deployments = new List<PromotionDeployment>
        {
            CreateTestDeployment(storesDeployedCount: 5)
        };

        _promotionRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotion);

        _redemptionRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<PromotionRedemption, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(redemptions);

        _deploymentRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<PromotionDeployment, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(deployments);

        _storeRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestStore());

        // Act
        var result = await _service.GetPromotionDashboardAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.PromotionId.Should().Be(1);
        result.TotalRedemptions.Should().Be(2);
        result.TotalDiscountGiven.Should().Be(25m);
        result.TotalStoresDeployed.Should().Be(5);
    }

    [Fact]
    public async Task GetPromotionDashboardAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        _promotionRepositoryMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CentralPromotion?)null);

        // Act
        var result = await _service.GetPromotionDashboardAsync(999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetRedemptionsByStoreAsync Tests

    [Fact]
    public async Task GetRedemptionsByStoreAsync_GroupsRedemptionsByStore()
    {
        // Arrange
        var redemptions = new List<PromotionRedemption>
        {
            CreateTestRedemption(1, storeId: 1, discountGiven: 10m),
            CreateTestRedemption(2, storeId: 1, discountGiven: 15m),
            CreateTestRedemption(3, storeId: 2, discountGiven: 20m)
        };

        _redemptionRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<PromotionRedemption, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(redemptions);

        _storeRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestStore(1, "STR001", "Store 1"));

        _storeRepositoryMock.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestStore(2, "STR002", "Store 2"));

        // Act
        var result = await _service.GetRedemptionsByStoreAsync(1);

        // Assert
        result.Should().HaveCount(2);
        var store1Summary = result.FirstOrDefault(s => s.StoreId == 1);
        store1Summary.Should().NotBeNull();
        store1Summary!.RedemptionCount.Should().Be(2);
        store1Summary.TotalDiscountGiven.Should().Be(25m);
    }

    #endregion

    #region GetActivePromotionsForStoreAsync Tests

    [Fact]
    public async Task GetActivePromotionsForStoreAsync_ReturnsDeployedPromotions()
    {
        // Arrange
        var store = CreateTestStore(1);
        var deploymentStores = new List<DeploymentStore>
        {
            CreateTestDeploymentStore(1, 1, 1, DeploymentStatus.Completed)
        };
        var deployments = new List<PromotionDeployment>
        {
            CreateTestDeployment(1, 1)
        };
        var promotions = new List<CentralPromotion>
        {
            CreateTestPromotion(1, startDate: DateTime.UtcNow.AddDays(-1), endDate: DateTime.UtcNow.AddDays(30))
        };

        _storeRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(store);

        _deploymentStoreRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<DeploymentStore, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(deploymentStores);

        _deploymentRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<PromotionDeployment, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(deployments);

        _promotionRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<CentralPromotion, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotions);

        _promotionProductRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<PromotionProduct, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PromotionProduct>());

        _promotionCategoryRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<PromotionCategory, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PromotionCategory>());

        // Act
        var result = await _service.GetActivePromotionsForStoreAsync(1);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetActivePromotionsForStoreAsync_WithInvalidStore_ReturnsEmptyList()
    {
        // Arrange
        _storeRepositoryMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Store?)null);

        // Act
        var result = await _service.GetActivePromotionsForStoreAsync(999);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetApplicablePromotionAsync Tests

    [Fact]
    public async Task GetApplicablePromotionAsync_WithMatchingProduct_ReturnsPromotion()
    {
        // Arrange
        var store = CreateTestStore(1);
        var product = CreateTestProduct(1, categoryId: 1);
        var deploymentStores = new List<DeploymentStore>
        {
            CreateTestDeploymentStore(1, 1, 1, DeploymentStatus.Completed)
        };
        var deployments = new List<PromotionDeployment>
        {
            CreateTestDeployment(1, 1)
        };
        var promotions = new List<CentralPromotion>
        {
            CreateTestPromotion(1, startDate: DateTime.UtcNow.AddDays(-1), endDate: DateTime.UtcNow.AddDays(30))
        };
        var promotionProducts = new List<PromotionProduct>
        {
            CreateTestPromotionProduct(1, 1, 1)
        };

        _storeRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(store);

        _productRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _deploymentStoreRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<DeploymentStore, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(deploymentStores);

        _deploymentRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<PromotionDeployment, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(deployments);

        _promotionRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<CentralPromotion, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotions);

        _promotionProductRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<PromotionProduct, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotionProducts);

        _promotionCategoryRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<PromotionCategory, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PromotionCategory>());

        // Act
        var result = await _service.GetApplicablePromotionAsync(1, 1);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetApplicablePromotionAsync_WithInvalidProduct_ReturnsNull()
    {
        // Arrange
        _productRepositoryMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        _storeRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestStore());

        _deploymentStoreRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<DeploymentStore, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DeploymentStore>());

        // Act
        var result = await _service.GetApplicablePromotionAsync(1, 999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Private Helper Methods for Setup

    private void SetupEmptyCollectionMocks()
    {
        _promotionProductRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<PromotionProduct, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PromotionProduct>());

        _promotionCategoryRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<PromotionCategory, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PromotionCategory>());

        _deploymentRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<PromotionDeployment, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PromotionDeployment>());

        _redemptionRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<PromotionRedemption, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PromotionRedemption>());
    }

    #endregion
}
