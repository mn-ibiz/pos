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
/// Unit tests for KdsOrderService.
/// </summary>
public class KdsOrderServiceTests
{
    private readonly Mock<IRepository<KdsOrder>> _kdsOrderRepoMock;
    private readonly Mock<IRepository<KdsOrderItem>> _kdsOrderItemRepoMock;
    private readonly Mock<IRepository<KdsStation>> _stationRepoMock;
    private readonly Mock<IRepository<KdsStationCategory>> _stationCategoryRepoMock;
    private readonly Mock<IRepository<Order>> _orderRepoMock;
    private readonly Mock<IRepository<OrderItem>> _orderItemRepoMock;
    private readonly Mock<IRepository<Product>> _productRepoMock;
    private readonly Mock<IRepository<KdsDisplaySettings>> _displaySettingsRepoMock;
    private readonly Mock<IRepository<AllCallMessage>> _allCallRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<KdsOrderService>> _loggerMock;
    private readonly KdsOrderService _service;

    public KdsOrderServiceTests()
    {
        _kdsOrderRepoMock = new Mock<IRepository<KdsOrder>>();
        _kdsOrderItemRepoMock = new Mock<IRepository<KdsOrderItem>>();
        _stationRepoMock = new Mock<IRepository<KdsStation>>();
        _stationCategoryRepoMock = new Mock<IRepository<KdsStationCategory>>();
        _orderRepoMock = new Mock<IRepository<Order>>();
        _orderItemRepoMock = new Mock<IRepository<OrderItem>>();
        _productRepoMock = new Mock<IRepository<Product>>();
        _displaySettingsRepoMock = new Mock<IRepository<KdsDisplaySettings>>();
        _allCallRepoMock = new Mock<IRepository<AllCallMessage>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<KdsOrderService>>();

        _service = new KdsOrderService(
            _kdsOrderRepoMock.Object,
            _kdsOrderItemRepoMock.Object,
            _stationRepoMock.Object,
            _stationCategoryRepoMock.Object,
            _orderRepoMock.Object,
            _orderItemRepoMock.Object,
            _productRepoMock.Object,
            _displaySettingsRepoMock.Object,
            _allCallRepoMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullKdsOrderRepository_ThrowsArgumentNullException()
    {
        var act = () => new KdsOrderService(
            null!,
            _kdsOrderItemRepoMock.Object,
            _stationRepoMock.Object,
            _stationCategoryRepoMock.Object,
            _orderRepoMock.Object,
            _orderItemRepoMock.Object,
            _productRepoMock.Object,
            _displaySettingsRepoMock.Object,
            _allCallRepoMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("kdsOrderRepository");
    }

    #endregion

    #region RouteOrderToStationsAsync Tests

    [Fact]
    public async Task RouteOrderToStationsAsync_WithValidOrder_RoutesOrder()
    {
        // Arrange
        var dto = new RouteOrderToKdsDto
        {
            OrderId = 1,
            StoreId = 1
        };

        var order = new Order
        {
            Id = 1,
            OrderNumber = "ORD001",
            TableNumber = "T1",
            IsActive = true
        };

        var orderItem = new OrderItem
        {
            Id = 1,
            OrderId = 1,
            ProductId = 1,
            Quantity = 2,
            IsActive = true
        };

        var product = new Product
        {
            Id = 1,
            Name = "Burger",
            CategoryId = 1
        };

        var station = new KdsStation
        {
            Id = 1,
            Name = "Hot Line",
            StoreId = 1,
            IsExpo = false,
            IsActive = true
        };

        var stationCategory = new KdsStationCategory
        {
            Id = 1,
            StationId = 1,
            CategoryId = 1,
            IsActive = true
        };

        _orderRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(order);

        _kdsOrderRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsOrder, bool>>>()))
            .ReturnsAsync(new List<KdsOrder>());

        _orderItemRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<OrderItem, bool>>>()))
            .ReturnsAsync(new List<OrderItem> { orderItem });

        _stationRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsStation, bool>>>()))
            .ReturnsAsync(new List<KdsStation> { station });

        _stationCategoryRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsStationCategory, bool>>>()))
            .ReturnsAsync(new List<KdsStationCategory> { stationCategory });

        _productRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(product);

        _kdsOrderRepoMock.Setup(r => r.AddAsync(It.IsAny<KdsOrder>()))
            .Returns(Task.CompletedTask)
            .Callback<KdsOrder>(ko => ko.Id = 1);

        // Act
        var result = await _service.RouteOrderToStationsAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.KdsOrderId.Should().Be(1);
        result.OrderNumber.Should().Be("ORD001");
        result.ItemRoutings.Should().HaveCount(1);
        result.ItemRoutings[0].ProductName.Should().Be("Burger");
        result.ItemRoutings[0].StationName.Should().Be("Hot Line");
    }

    [Fact]
    public async Task RouteOrderToStationsAsync_WithInvalidOrder_ReturnsError()
    {
        // Arrange
        var dto = new RouteOrderToKdsDto { OrderId = 999, StoreId = 1 };

        _orderRepoMock.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _service.RouteOrderToStationsAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("not found"));
    }

    [Fact]
    public async Task RouteOrderToStationsAsync_WithAlreadyRoutedOrder_ReturnsError()
    {
        // Arrange
        var dto = new RouteOrderToKdsDto { OrderId = 1, StoreId = 1 };
        var order = new Order { Id = 1, OrderNumber = "ORD001", IsActive = true };
        var existingKdsOrder = new KdsOrder { Id = 1, OrderId = 1, IsActive = true };

        _orderRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(order);

        _kdsOrderRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsOrder, bool>>>()))
            .ReturnsAsync(new List<KdsOrder> { existingKdsOrder });

        // Act
        var result = await _service.RouteOrderToStationsAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("already routed"));
    }

    #endregion

    #region GetOrderAsync Tests

    [Fact]
    public async Task GetOrderAsync_WithValidId_ReturnsOrder()
    {
        // Arrange
        var kdsOrder = new KdsOrder
        {
            Id = 1,
            OrderId = 1,
            OrderNumber = "ORD001",
            TableNumber = "T1",
            Status = KdsOrderStatus.New,
            ReceivedAt = DateTime.UtcNow.AddMinutes(-5),
            IsActive = true
        };

        _kdsOrderRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(kdsOrder);

        _kdsOrderItemRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsOrderItem, bool>>>()))
            .ReturnsAsync(new List<KdsOrderItem>());

        // Act
        var result = await _service.GetOrderAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.OrderNumber.Should().Be("ORD001");
        result.Status.Should().Be(KdsOrderStatusDto.New);
    }

    [Fact]
    public async Task GetOrderAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        _kdsOrderRepoMock.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((KdsOrder?)null);

        // Act
        var result = await _service.GetOrderAsync(999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetActiveOrdersAsync Tests

    [Fact]
    public async Task GetActiveOrdersAsync_ReturnsActiveOrders()
    {
        // Arrange
        var kdsOrder = new KdsOrder
        {
            Id = 1,
            OrderNumber = "ORD001",
            Status = KdsOrderStatus.InProgress,
            ReceivedAt = DateTime.UtcNow,
            IsActive = true
        };

        var orderItem = new KdsOrderItem
        {
            Id = 1,
            KdsOrderId = 1,
            StationId = 1,
            Status = KdsItemStatus.Preparing,
            IsActive = true
        };

        _kdsOrderItemRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsOrderItem, bool>>>()))
            .ReturnsAsync(new List<KdsOrderItem> { orderItem });

        _kdsOrderRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsOrder, bool>>>()))
            .ReturnsAsync(new List<KdsOrder> { kdsOrder });

        _stationRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new KdsStation { Id = 1, Name = "Hot Line" });

        // Act
        var result = await _service.GetActiveOrdersAsync(1);

        // Assert
        result.Should().HaveCount(1);
        result[0].OrderNumber.Should().Be("ORD001");
    }

    #endregion

    #region UpdateOrderPriorityAsync Tests

    [Fact]
    public async Task UpdateOrderPriorityAsync_WithValidOrder_UpdatesPriority()
    {
        // Arrange
        var kdsOrder = new KdsOrder
        {
            Id = 1,
            OrderNumber = "ORD001",
            Priority = OrderPriority.Normal,
            IsPriority = false,
            ReceivedAt = DateTime.UtcNow,
            IsActive = true
        };

        _kdsOrderRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(kdsOrder);

        _kdsOrderItemRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsOrderItem, bool>>>()))
            .ReturnsAsync(new List<KdsOrderItem>());

        KdsOrderDto? eventResult = null;
        _service.OrderUpdated += (sender, dto) => eventResult = dto;

        // Act
        var result = await _service.UpdateOrderPriorityAsync(1, OrderPriorityDto.Rush);

        // Assert
        result.Priority.Should().Be(OrderPriorityDto.Rush);
        result.IsPriority.Should().BeTrue();
        eventResult.Should().NotBeNull();
    }

    #endregion

    #region VoidOrderAsync Tests

    [Fact]
    public async Task VoidOrderAsync_WithValidOrder_VoidsOrder()
    {
        // Arrange
        var kdsOrder = new KdsOrder
        {
            Id = 1,
            OrderNumber = "ORD001",
            Status = KdsOrderStatus.InProgress,
            ReceivedAt = DateTime.UtcNow,
            IsActive = true
        };

        var orderItem = new KdsOrderItem
        {
            Id = 1,
            KdsOrderId = 1,
            Status = KdsItemStatus.Preparing,
            IsActive = true
        };

        _kdsOrderRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(kdsOrder);

        _kdsOrderItemRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsOrderItem, bool>>>()))
            .ReturnsAsync(new List<KdsOrderItem> { orderItem });

        _stationRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new KdsStation { Id = 1, Name = "Hot Line" });

        KdsOrderDto? eventResult = null;
        _service.OrderVoided += (sender, dto) => eventResult = dto;

        // Act
        var result = await _service.VoidOrderAsync(1, 1);

        // Assert
        result.Status.Should().Be(KdsOrderStatusDto.Voided);
        kdsOrder.Status.Should().Be(KdsOrderStatus.Voided);
        orderItem.Status.Should().Be(KdsItemStatus.Voided);
        eventResult.Should().NotBeNull();
    }

    #endregion

    #region GetOrderCountByStatusAsync Tests

    [Fact]
    public async Task GetOrderCountByStatusAsync_ReturnsCorrectCounts()
    {
        // Arrange
        var orderItems = new List<KdsOrderItem>
        {
            new() { Id = 1, KdsOrderId = 1, StationId = 1, IsActive = true },
            new() { Id = 2, KdsOrderId = 2, StationId = 1, IsActive = true }
        };

        var kdsOrders = new List<KdsOrder>
        {
            new() { Id = 1, Status = KdsOrderStatus.New, IsActive = true },
            new() { Id = 2, Status = KdsOrderStatus.InProgress, IsActive = true }
        };

        _kdsOrderItemRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsOrderItem, bool>>>()))
            .ReturnsAsync(orderItems);

        _kdsOrderRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsOrder, bool>>>()))
            .ReturnsAsync(kdsOrders);

        // Act
        var result = await _service.GetOrderCountByStatusAsync(1);

        // Assert
        result.Should().ContainKey(KdsOrderStatusDto.New);
        result.Should().ContainKey(KdsOrderStatusDto.InProgress);
        result[KdsOrderStatusDto.New].Should().Be(1);
        result[KdsOrderStatusDto.InProgress].Should().Be(1);
    }

    #endregion
}
