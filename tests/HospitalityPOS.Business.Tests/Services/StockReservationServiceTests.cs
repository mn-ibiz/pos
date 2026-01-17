using FluentAssertions;
using Moq;
using Xunit;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Services;
using System.Linq.Expressions;

namespace HospitalityPOS.Business.Tests.Services;

public class StockReservationServiceTests
{
    private readonly Mock<IRepository<StockReservation>> _reservationRepoMock;
    private readonly Mock<IRepository<Inventory>> _inventoryRepoMock;
    private readonly Mock<IRepository<Product>> _productRepoMock;
    private readonly Mock<IRepository<Store>> _storeRepoMock;
    private readonly Mock<IRepository<StockTransferRequest>> _transferRequestRepoMock;
    private readonly StockReservationService _service;

    public StockReservationServiceTests()
    {
        _reservationRepoMock = new Mock<IRepository<StockReservation>>();
        _inventoryRepoMock = new Mock<IRepository<Inventory>>();
        _productRepoMock = new Mock<IRepository<Product>>();
        _storeRepoMock = new Mock<IRepository<Store>>();
        _transferRequestRepoMock = new Mock<IRepository<StockTransferRequest>>();

        _service = new StockReservationService(
            _reservationRepoMock.Object,
            _inventoryRepoMock.Object,
            _productRepoMock.Object,
            _storeRepoMock.Object,
            _transferRequestRepoMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullReservationRepository_ThrowsArgumentNullException()
    {
        var act = () => new StockReservationService(
            null!,
            _inventoryRepoMock.Object,
            _productRepoMock.Object,
            _storeRepoMock.Object,
            _transferRequestRepoMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("reservationRepository");
    }

    [Fact]
    public void Constructor_WithNullInventoryRepository_ThrowsArgumentNullException()
    {
        var act = () => new StockReservationService(
            _reservationRepoMock.Object,
            null!,
            _productRepoMock.Object,
            _storeRepoMock.Object,
            _transferRequestRepoMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("inventoryRepository");
    }

    [Fact]
    public void Constructor_WithNullProductRepository_ThrowsArgumentNullException()
    {
        var act = () => new StockReservationService(
            _reservationRepoMock.Object,
            _inventoryRepoMock.Object,
            null!,
            _storeRepoMock.Object,
            _transferRequestRepoMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("productRepository");
    }

    [Fact]
    public void Constructor_WithNullStoreRepository_ThrowsArgumentNullException()
    {
        var act = () => new StockReservationService(
            _reservationRepoMock.Object,
            _inventoryRepoMock.Object,
            _productRepoMock.Object,
            null!,
            _transferRequestRepoMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("storeRepository");
    }

    [Fact]
    public void Constructor_WithNullTransferRequestRepository_ThrowsArgumentNullException()
    {
        var act = () => new StockReservationService(
            _reservationRepoMock.Object,
            _inventoryRepoMock.Object,
            _productRepoMock.Object,
            _storeRepoMock.Object,
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("transferRequestRepository");
    }

    #endregion

    #region CreateReservationAsync Tests

    [Fact]
    public async Task CreateReservationAsync_WithSufficientStock_CreatesReservation()
    {
        // Arrange
        var dto = new CreateStockReservationDto
        {
            LocationId = 1,
            LocationType = TransferLocationType.Store,
            ProductId = 100,
            Quantity = 10,
            ReferenceId = 5,
            ReferenceType = ReservationType.Transfer,
            ExpirationHours = 48
        };

        SetupInventoryWithStock(1, 100, 50);
        SetupNoActiveReservations();
        SetupStoreAndProduct();

        StockReservation? capturedReservation = null;
        _reservationRepoMock.Setup(r => r.AddAsync(It.IsAny<StockReservation>()))
            .Callback<StockReservation>(r => capturedReservation = r)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateReservationAsync(dto, 1);

        // Assert
        result.Should().NotBeNull();
        capturedReservation.Should().NotBeNull();
        capturedReservation!.ReservedQuantity.Should().Be(10);
        capturedReservation.ReferenceType.Should().Be(ReservationType.Transfer);
        capturedReservation.Status.Should().Be(ReservationStatus.Active);
        _reservationRepoMock.Verify(r => r.AddAsync(It.IsAny<StockReservation>()), Times.Once);
        _reservationRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateReservationAsync_WithInsufficientStock_ThrowsException()
    {
        // Arrange
        var dto = new CreateStockReservationDto
        {
            LocationId = 1,
            LocationType = TransferLocationType.Store,
            ProductId = 100,
            Quantity = 100, // More than available
            ReferenceId = 5,
            ReferenceType = ReservationType.Transfer
        };

        SetupInventoryWithStock(1, 100, 50);
        SetupNoActiveReservations();

        // Act & Assert
        var act = () => _service.CreateReservationAsync(dto, 1);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Insufficient stock*");
    }

    #endregion

    #region CreateBatchReservationsAsync Tests

    [Fact]
    public async Task CreateBatchReservationsAsync_WithValidLines_CreatesAllReservations()
    {
        // Arrange
        var dto = new CreateBatchReservationsDto
        {
            LocationId = 1,
            LocationType = TransferLocationType.Store,
            ReferenceId = 5,
            ReferenceType = ReservationType.Transfer,
            Lines = new List<ReservationLineDto>
            {
                new() { ProductId = 100, Quantity = 5 },
                new() { ProductId = 101, Quantity = 10 }
            }
        };

        SetupInventoryWithMultipleProducts();
        SetupNoActiveReservations();
        SetupStoreAndProducts();

        var capturedReservations = new List<StockReservation>();
        _reservationRepoMock.Setup(r => r.AddAsync(It.IsAny<StockReservation>()))
            .Callback<StockReservation>(r => capturedReservations.Add(r))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateBatchReservationsAsync(dto, 1);

        // Assert
        result.Should().HaveCount(2);
        capturedReservations.Should().HaveCount(2);
        _reservationRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateBatchReservationsAsync_WithInsufficientStock_ThrowsException()
    {
        // Arrange
        var dto = new CreateBatchReservationsDto
        {
            LocationId = 1,
            LocationType = TransferLocationType.Store,
            ReferenceId = 5,
            ReferenceType = ReservationType.Transfer,
            Lines = new List<ReservationLineDto>
            {
                new() { ProductId = 100, Quantity = 1000 } // Way more than available
            }
        };

        SetupInventoryWithStock(1, 100, 50);
        SetupNoActiveReservations();
        SetupStoreAndProduct();

        // Act & Assert
        var act = () => _service.CreateBatchReservationsAsync(dto, 1);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot reserve all items*");
    }

    #endregion

    #region GetReservedQuantityAsync Tests

    [Fact]
    public async Task GetReservedQuantityAsync_WithActiveReservations_ReturnsTotalReserved()
    {
        // Arrange
        var reservations = new List<StockReservation>
        {
            CreateActiveReservation(1, 100, 10),
            CreateActiveReservation(1, 100, 15)
        };

        _reservationRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<StockReservation, bool>>>()))
            .ReturnsAsync(reservations);

        // Act
        var result = await _service.GetReservedQuantityAsync(1, 100);

        // Assert
        result.Should().Be(25);
    }

    [Fact]
    public async Task GetReservedQuantityAsync_WithNoReservations_ReturnsZero()
    {
        // Arrange
        _reservationRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<StockReservation, bool>>>()))
            .ReturnsAsync(new List<StockReservation>());

        // Act
        var result = await _service.GetReservedQuantityAsync(1, 100);

        // Assert
        result.Should().Be(0);
    }

    #endregion

    #region GetAvailableQuantityAsync Tests

    [Fact]
    public async Task GetAvailableQuantityAsync_WithStockAndReservations_ReturnsCorrectAmount()
    {
        // Arrange
        SetupInventoryWithStock(1, 100, 50);

        var reservations = new List<StockReservation>
        {
            CreateActiveReservation(1, 100, 15)
        };

        _reservationRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<StockReservation, bool>>>()))
            .ReturnsAsync(reservations);

        // Act
        var result = await _service.GetAvailableQuantityAsync(1, 100);

        // Assert
        result.Should().Be(35); // 50 - 15
    }

    #endregion

    #region FulfillReservationAsync Tests

    [Fact]
    public async Task FulfillReservationAsync_WithActiveReservation_UpdatesStatus()
    {
        // Arrange
        var reservation = CreateActiveReservation(1, 100, 10);
        reservation.Id = 1;

        _reservationRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(reservation);

        SetupStoreAndProduct();

        // Act
        var result = await _service.FulfillReservationAsync(1, 99);

        // Assert
        result.Should().NotBeNull();
        reservation.Status.Should().Be(ReservationStatus.Fulfilled);
        reservation.CompletedAt.Should().NotBeNull();
        reservation.CompletedByUserId.Should().Be(99);
        _reservationRepoMock.Verify(r => r.UpdateAsync(reservation), Times.Once);
    }

    [Fact]
    public async Task FulfillReservationAsync_WithNonExistentReservation_ThrowsException()
    {
        // Arrange
        _reservationRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync((StockReservation?)null);

        // Act & Assert
        var act = () => _service.FulfillReservationAsync(1, 99);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    #endregion

    #region ReleaseReservationAsync Tests

    [Fact]
    public async Task ReleaseReservationAsync_WithActiveReservation_UpdatesStatus()
    {
        // Arrange
        var reservation = CreateActiveReservation(1, 100, 10);
        reservation.Id = 1;

        _reservationRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(reservation);

        SetupStoreAndProduct();

        // Act
        var result = await _service.ReleaseReservationAsync(1, 99, "Cancelled");

        // Assert
        result.Should().NotBeNull();
        reservation.Status.Should().Be(ReservationStatus.Released);
        reservation.CompletedAt.Should().NotBeNull();
        reservation.Notes.Should().Contain("Cancelled");
        _reservationRepoMock.Verify(r => r.UpdateAsync(reservation), Times.Once);
    }

    #endregion

    #region FulfillByReferenceAsync Tests

    [Fact]
    public async Task FulfillByReferenceAsync_WithMultipleReservations_FulfillsAll()
    {
        // Arrange
        var reservations = new List<StockReservation>
        {
            CreateActiveReservation(1, 100, 10),
            CreateActiveReservation(1, 101, 5)
        };
        reservations[0].ReferenceId = 5;
        reservations[0].ReferenceType = ReservationType.Transfer;
        reservations[1].ReferenceId = 5;
        reservations[1].ReferenceType = ReservationType.Transfer;

        _reservationRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<StockReservation, bool>>>()))
            .ReturnsAsync(reservations);

        SetupStoreAndProducts();

        // Act
        var result = await _service.FulfillByReferenceAsync(ReservationType.Transfer, 5, 99);

        // Assert
        result.Should().HaveCount(2);
        reservations.All(r => r.Status == ReservationStatus.Fulfilled).Should().BeTrue();
        _reservationRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    #endregion

    #region ExpireOverdueReservationsAsync Tests

    [Fact]
    public async Task ExpireOverdueReservationsAsync_WithExpiredReservations_ExpiresAll()
    {
        // Arrange
        var expiredReservation = CreateActiveReservation(1, 100, 10);
        expiredReservation.ExpiresAt = DateTime.UtcNow.AddHours(-1); // Already expired

        _reservationRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<StockReservation, bool>>>()))
            .ReturnsAsync(new List<StockReservation> { expiredReservation });

        // Act
        var result = await _service.ExpireOverdueReservationsAsync();

        // Assert
        result.Should().Be(1);
        expiredReservation.Status.Should().Be(ReservationStatus.Expired);
        _reservationRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    #endregion

    #region CanReserveAsync Tests

    [Fact]
    public async Task CanReserveAsync_WithSufficientStock_ReturnsTrue()
    {
        // Arrange
        SetupInventoryWithStock(1, 100, 50);
        SetupNoActiveReservations();

        // Act
        var result = await _service.CanReserveAsync(1, 100, 30);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanReserveAsync_WithInsufficientStock_ReturnsFalse()
    {
        // Arrange
        SetupInventoryWithStock(1, 100, 50);
        SetupNoActiveReservations();

        // Act
        var result = await _service.CanReserveAsync(1, 100, 100);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetLocationSummaryAsync Tests

    [Fact]
    public async Task GetLocationSummaryAsync_ReturnsCorrectSummary()
    {
        // Arrange
        var store = new Store { Id = 1, Name = "Test Store", IsActive = true };
        _storeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(store);

        var reservations = new List<StockReservation>
        {
            CreateActiveReservation(1, 100, 10),
            CreateActiveReservation(1, 101, 5)
        };

        _reservationRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<StockReservation, bool>>>()))
            .ReturnsAsync(reservations);

        // Act
        var result = await _service.GetLocationSummaryAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.LocationId.Should().Be(1);
        result.LocationName.Should().Be("Test Store");
        result.ActiveReservations.Should().Be(2);
        result.TotalQuantityReserved.Should().Be(15);
    }

    #endregion

    #region Helper Methods

    private void SetupInventoryWithStock(int storeId, int productId, int quantity)
    {
        var inventory = new Inventory
        {
            StoreId = storeId,
            ProductId = productId,
            Quantity = quantity,
            IsActive = true
        };

        _inventoryRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Inventory, bool>>>()))
            .ReturnsAsync(new List<Inventory> { inventory });
    }

    private void SetupInventoryWithMultipleProducts()
    {
        var inventories = new List<Inventory>
        {
            new() { StoreId = 1, ProductId = 100, Quantity = 50, IsActive = true },
            new() { StoreId = 1, ProductId = 101, Quantity = 50, IsActive = true }
        };

        _inventoryRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Inventory, bool>>>()))
            .ReturnsAsync((Expression<Func<Inventory, bool>> predicate) =>
                inventories.Where(predicate.Compile()).ToList());
    }

    private void SetupNoActiveReservations()
    {
        _reservationRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<StockReservation, bool>>>()))
            .ReturnsAsync(new List<StockReservation>());
    }

    private void SetupStoreAndProduct()
    {
        var store = new Store { Id = 1, Name = "Test Store", IsActive = true };
        var product = new Product { Id = 100, Name = "Test Product", SKU = "TST001", IsActive = true };

        _storeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(store);
        _productRepoMock.Setup(r => r.GetByIdAsync(100)).ReturnsAsync(product);
        _transferRequestRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new StockTransferRequest { Id = 5, RequestNumber = "TR-2024-00001" });
    }

    private void SetupStoreAndProducts()
    {
        var store = new Store { Id = 1, Name = "Test Store", IsActive = true };
        var product1 = new Product { Id = 100, Name = "Product 1", SKU = "P001", IsActive = true };
        var product2 = new Product { Id = 101, Name = "Product 2", SKU = "P002", IsActive = true };

        _storeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(store);
        _productRepoMock.Setup(r => r.GetByIdAsync(100)).ReturnsAsync(product1);
        _productRepoMock.Setup(r => r.GetByIdAsync(101)).ReturnsAsync(product2);
        _transferRequestRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new StockTransferRequest { Id = 5, RequestNumber = "TR-2024-00001" });
    }

    private static StockReservation CreateActiveReservation(int locationId, int productId, int quantity)
    {
        return new StockReservation
        {
            LocationId = locationId,
            LocationType = TransferLocationType.Store,
            ProductId = productId,
            ReservedQuantity = quantity,
            ReferenceId = 1,
            ReferenceType = ReservationType.Transfer,
            Status = ReservationStatus.Active,
            ReservedAt = DateTime.UtcNow,
            ReservedByUserId = 1,
            ExpiresAt = DateTime.UtcNow.AddHours(48),
            IsActive = true
        };
    }

    #endregion
}
