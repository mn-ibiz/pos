using FluentAssertions;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for KdsStationService.
/// </summary>
public class KdsStationServiceTests
{
    private readonly Mock<IRepository<KdsStation>> _stationRepoMock;
    private readonly Mock<IRepository<KdsStationCategory>> _stationCategoryRepoMock;
    private readonly Mock<IRepository<KdsDisplaySettings>> _displaySettingsRepoMock;
    private readonly Mock<IRepository<Category>> _categoryRepoMock;
    private readonly Mock<IRepository<Store>> _storeRepoMock;
    private readonly Mock<IRepository<KdsOrderItem>> _orderItemRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<KdsStationService>> _loggerMock;
    private readonly KdsStationService _service;

    public KdsStationServiceTests()
    {
        _stationRepoMock = new Mock<IRepository<KdsStation>>();
        _stationCategoryRepoMock = new Mock<IRepository<KdsStationCategory>>();
        _displaySettingsRepoMock = new Mock<IRepository<KdsDisplaySettings>>();
        _categoryRepoMock = new Mock<IRepository<Category>>();
        _storeRepoMock = new Mock<IRepository<Store>>();
        _orderItemRepoMock = new Mock<IRepository<KdsOrderItem>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<KdsStationService>>();

        _service = new KdsStationService(
            _stationRepoMock.Object,
            _stationCategoryRepoMock.Object,
            _displaySettingsRepoMock.Object,
            _categoryRepoMock.Object,
            _storeRepoMock.Object,
            _orderItemRepoMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullStationRepository_ThrowsArgumentNullException()
    {
        var act = () => new KdsStationService(
            null!,
            _stationCategoryRepoMock.Object,
            _displaySettingsRepoMock.Object,
            _categoryRepoMock.Object,
            _storeRepoMock.Object,
            _orderItemRepoMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("stationRepository");
    }

    [Fact]
    public void Constructor_WithNullUnitOfWork_ThrowsArgumentNullException()
    {
        var act = () => new KdsStationService(
            _stationRepoMock.Object,
            _stationCategoryRepoMock.Object,
            _displaySettingsRepoMock.Object,
            _categoryRepoMock.Object,
            _storeRepoMock.Object,
            _orderItemRepoMock.Object,
            null!,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("unitOfWork");
    }

    #endregion

    #region CreateStationAsync Tests

    [Fact]
    public async Task CreateStationAsync_WithValidDto_CreatesStation()
    {
        // Arrange
        var dto = new CreateKdsStationDto
        {
            Name = "Hot Line",
            DeviceIdentifier = "192.168.1.100",
            StationType = KdsStationTypeDto.PrepStation,
            StoreId = 1,
            DisplayOrder = 1
        };

        var store = new Store { Id = 1, Name = "Main Store", IsActive = true };

        _storeRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(store);

        _stationRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsStation, bool>>>()))
            .ReturnsAsync(new List<KdsStation>());

        _stationRepoMock.Setup(r => r.AddAsync(It.IsAny<KdsStation>()))
            .Returns(Task.CompletedTask)
            .Callback<KdsStation>(s => s.Id = 1);

        _stationCategoryRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsStationCategory, bool>>>()))
            .ReturnsAsync(new List<KdsStationCategory>());

        // Act
        var result = await _service.CreateStationAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Hot Line");
        result.DeviceIdentifier.Should().Be("192.168.1.100");
        _stationRepoMock.Verify(r => r.AddAsync(It.IsAny<KdsStation>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.AtLeastOnce);
    }

    [Fact]
    public async Task CreateStationAsync_WithInvalidStore_ThrowsException()
    {
        // Arrange
        var dto = new CreateKdsStationDto
        {
            Name = "Hot Line",
            DeviceIdentifier = "192.168.1.100",
            StoreId = 999
        };

        _storeRepoMock.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Store?)null);

        // Act
        var act = () => _service.CreateStationAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Store*not found*");
    }

    [Fact]
    public async Task CreateStationAsync_WithDuplicateDeviceIdentifier_ThrowsException()
    {
        // Arrange
        var dto = new CreateKdsStationDto
        {
            Name = "Hot Line",
            DeviceIdentifier = "192.168.1.100",
            StoreId = 1
        };

        var store = new Store { Id = 1, Name = "Main Store", IsActive = true };
        var existingStation = new KdsStation { Id = 1, DeviceIdentifier = "192.168.1.100", IsActive = true };

        _storeRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(store);

        _stationRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsStation, bool>>>()))
            .ReturnsAsync(new List<KdsStation> { existingStation });

        // Act
        var act = () => _service.CreateStationAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Device identifier*already in use*");
    }

    #endregion

    #region GetStationAsync Tests

    [Fact]
    public async Task GetStationAsync_WithValidId_ReturnsStation()
    {
        // Arrange
        var station = new KdsStation
        {
            Id = 1,
            Name = "Hot Line",
            DeviceIdentifier = "192.168.1.100",
            StationType = KdsStationType.PrepStation,
            Status = KdsStationStatus.Online,
            StoreId = 1,
            IsActive = true
        };

        _stationRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(station);

        _stationCategoryRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsStationCategory, bool>>>()))
            .ReturnsAsync(new List<KdsStationCategory>());

        // Act
        var result = await _service.GetStationAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Hot Line");
    }

    [Fact]
    public async Task GetStationAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        _stationRepoMock.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((KdsStation?)null);

        // Act
        var result = await _service.GetStationAsync(999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region SetStationOnlineAsync Tests

    [Fact]
    public async Task SetStationOnlineAsync_WithValidStation_SetsOnline()
    {
        // Arrange
        var station = new KdsStation
        {
            Id = 1,
            Name = "Hot Line",
            Status = KdsStationStatus.Offline,
            IsActive = true
        };

        _stationRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(station);

        _stationCategoryRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsStationCategory, bool>>>()))
            .ReturnsAsync(new List<KdsStationCategory>());

        KdsStationDto? eventResult = null;
        _service.StationOnline += (sender, dto) => eventResult = dto;

        // Act
        var result = await _service.SetStationOnlineAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(KdsStationStatusDto.Online);
        station.LastConnectedAt.Should().NotBeNull();
        eventResult.Should().NotBeNull();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task SetStationOnlineAsync_WithInvalidStation_ThrowsException()
    {
        // Arrange
        _stationRepoMock.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((KdsStation?)null);

        // Act
        var act = () => _service.SetStationOnlineAsync(999);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion

    #region AssignCategoryAsync Tests

    [Fact]
    public async Task AssignCategoryAsync_WithValidData_AssignsCategory()
    {
        // Arrange
        var station = new KdsStation { Id = 1, Name = "Hot Line", IsActive = true };
        var category = new Category { Id = 1, Name = "Entrees", IsActive = true };

        _stationRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(station);

        _categoryRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(category);

        _stationCategoryRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsStationCategory, bool>>>()))
            .ReturnsAsync(new List<KdsStationCategory>());

        // Act
        var result = await _service.AssignCategoryAsync(1, 1);

        // Assert
        result.Should().NotBeNull();
        result.StationId.Should().Be(1);
        result.CategoryId.Should().Be(1);
        result.CategoryName.Should().Be("Entrees");
        _stationCategoryRepoMock.Verify(r => r.AddAsync(It.IsAny<KdsStationCategory>()), Times.Once);
    }

    [Fact]
    public async Task AssignCategoryAsync_WithDuplicateCategory_ThrowsException()
    {
        // Arrange
        var station = new KdsStation { Id = 1, Name = "Hot Line", IsActive = true };
        var category = new Category { Id = 1, Name = "Entrees", IsActive = true };
        var existingAssignment = new KdsStationCategory { StationId = 1, CategoryId = 1, IsActive = true };

        _stationRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(station);

        _categoryRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(category);

        _stationCategoryRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsStationCategory, bool>>>()))
            .ReturnsAsync(new List<KdsStationCategory> { existingAssignment });

        // Act
        var act = () => _service.AssignCategoryAsync(1, 1);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already assigned*");
    }

    #endregion

    #region RemoveCategoryAsync Tests

    [Fact]
    public async Task RemoveCategoryAsync_WithValidData_RemovesCategory()
    {
        // Arrange
        var stationCategory = new KdsStationCategory
        {
            Id = 1,
            StationId = 1,
            CategoryId = 1,
            IsActive = true
        };

        _stationCategoryRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsStationCategory, bool>>>()))
            .ReturnsAsync(new List<KdsStationCategory> { stationCategory });

        _stationRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new KdsStation { Id = 1, IsActive = true });

        // Act
        var result = await _service.RemoveCategoryAsync(1, 1);

        // Assert
        result.Should().BeTrue();
        stationCategory.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveCategoryAsync_WithNonExistentAssignment_ReturnsFalse()
    {
        // Arrange
        _stationCategoryRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsStationCategory, bool>>>()))
            .ReturnsAsync(new List<KdsStationCategory>());

        // Act
        var result = await _service.RemoveCategoryAsync(1, 1);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region IsDeviceIdentifierUniqueAsync Tests

    [Fact]
    public async Task IsDeviceIdentifierUniqueAsync_WithUniqueIdentifier_ReturnsTrue()
    {
        // Arrange
        _stationRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsStation, bool>>>()))
            .ReturnsAsync(new List<KdsStation>());

        // Act
        var result = await _service.IsDeviceIdentifierUniqueAsync("192.168.1.200");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsDeviceIdentifierUniqueAsync_WithDuplicateIdentifier_ReturnsFalse()
    {
        // Arrange
        var existingStation = new KdsStation { Id = 1, DeviceIdentifier = "192.168.1.100", IsActive = true };

        _stationRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsStation, bool>>>()))
            .ReturnsAsync(new List<KdsStation> { existingStation });

        // Act
        var result = await _service.IsDeviceIdentifierUniqueAsync("192.168.1.100");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region DeleteStationAsync Tests

    [Fact]
    public async Task DeleteStationAsync_WithValidStation_DeletesStation()
    {
        // Arrange
        var station = new KdsStation { Id = 1, Name = "Hot Line", IsActive = true };

        _stationRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(station);

        _orderItemRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsOrderItem, bool>>>()))
            .ReturnsAsync(new List<KdsOrderItem>());

        // Act
        var result = await _service.DeleteStationAsync(1);

        // Assert
        result.Should().BeTrue();
        station.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteStationAsync_WithActiveOrders_ThrowsException()
    {
        // Arrange
        var station = new KdsStation { Id = 1, Name = "Hot Line", IsActive = true };
        var activeItem = new KdsOrderItem { Id = 1, StationId = 1, Status = KdsItemStatus.Pending, IsActive = true };

        _stationRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(station);

        _orderItemRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsOrderItem, bool>>>()))
            .ReturnsAsync(new List<KdsOrderItem> { activeItem });

        // Act
        var act = () => _service.DeleteStationAsync(1);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*active orders*");
    }

    #endregion

    #region GetDisplaySettingsAsync Tests

    [Fact]
    public async Task GetDisplaySettingsAsync_WithNoSettings_ReturnsDefaults()
    {
        // Arrange
        var station = new KdsStation { Id = 1, Name = "Hot Line", DisplaySettingsId = null, IsActive = true };

        _stationRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(station);

        // Act
        var result = await _service.GetDisplaySettingsAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.ColumnsCount.Should().Be(4);
        result.FontSize.Should().Be(16);
        result.ShowModifiers.Should().BeTrue();
    }

    [Fact]
    public async Task GetDisplaySettingsAsync_WithSettings_ReturnsSettings()
    {
        // Arrange
        var settings = new KdsDisplaySettings
        {
            Id = 1,
            ColumnsCount = 6,
            FontSize = 20,
            ShowModifiers = false
        };

        var station = new KdsStation { Id = 1, Name = "Hot Line", DisplaySettingsId = 1, IsActive = true };

        _stationRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(station);

        _displaySettingsRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(settings);

        // Act
        var result = await _service.GetDisplaySettingsAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.ColumnsCount.Should().Be(6);
        result.FontSize.Should().Be(20);
        result.ShowModifiers.Should().BeFalse();
    }

    #endregion
}
