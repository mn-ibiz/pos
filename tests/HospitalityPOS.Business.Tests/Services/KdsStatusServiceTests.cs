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
/// Unit tests for KdsStatusService.
/// </summary>
public class KdsStatusServiceTests
{
    private readonly Mock<IRepository<KdsOrder>> _kdsOrderRepoMock;
    private readonly Mock<IRepository<KdsOrderItem>> _kdsOrderItemRepoMock;
    private readonly Mock<IRepository<KdsOrderStatusLog>> _statusLogRepoMock;
    private readonly Mock<IRepository<KdsStation>> _stationRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<KdsStatusService>> _loggerMock;
    private readonly KdsStatusService _service;

    public KdsStatusServiceTests()
    {
        _kdsOrderRepoMock = new Mock<IRepository<KdsOrder>>();
        _kdsOrderItemRepoMock = new Mock<IRepository<KdsOrderItem>>();
        _statusLogRepoMock = new Mock<IRepository<KdsOrderStatusLog>>();
        _stationRepoMock = new Mock<IRepository<KdsStation>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<KdsStatusService>>();

        _service = new KdsStatusService(
            _kdsOrderRepoMock.Object,
            _kdsOrderItemRepoMock.Object,
            _statusLogRepoMock.Object,
            _stationRepoMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullKdsOrderRepository_ThrowsArgumentNullException()
    {
        var act = () => new KdsStatusService(
            null!,
            _kdsOrderItemRepoMock.Object,
            _statusLogRepoMock.Object,
            _stationRepoMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("kdsOrderRepository");
    }

    [Fact]
    public void Constructor_WithNullKdsOrderItemRepository_ThrowsArgumentNullException()
    {
        var act = () => new KdsStatusService(
            _kdsOrderRepoMock.Object,
            null!,
            _statusLogRepoMock.Object,
            _stationRepoMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("kdsOrderItemRepository");
    }

    #endregion

    #region StartOrderAsync Tests

    [Fact]
    public async Task StartOrderAsync_WithValidOrder_StartsOrder()
    {
        // Arrange
        var kdsOrder = new KdsOrder
        {
            Id = 1,
            OrderNumber = "ORD001",
            Status = KdsOrderStatus.New,
            ReceivedAt = DateTime.UtcNow.AddMinutes(-1),
            IsActive = true
        };

        var orderItem = new KdsOrderItem
        {
            Id = 1,
            KdsOrderId = 1,
            StationId = 1,
            Status = KdsItemStatus.Pending,
            IsActive = true
        };

        _kdsOrderRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(kdsOrder);

        _kdsOrderItemRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsOrderItem, bool>>>()))
            .ReturnsAsync(new List<KdsOrderItem> { orderItem });

        _stationRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new KdsStation { Id = 1, Name = "Hot Line" });

        KdsOrderStatusChangeEventArgs? eventResult = null;
        _service.OrderStatusChanged += (sender, args) => eventResult = args;

        // Act
        var result = await _service.StartOrderAsync(1, 1);

        // Assert
        result.Status.Should().Be(KdsOrderStatusDto.InProgress);
        kdsOrder.Status.Should().Be(KdsOrderStatus.InProgress);
        kdsOrder.StartedAt.Should().NotBeNull();
        eventResult.Should().NotBeNull();
        eventResult!.NewStatus.Should().Be(KdsOrderStatusDto.InProgress);
    }

    [Fact]
    public async Task StartOrderAsync_WithInvalidOrder_ThrowsKeyNotFoundException()
    {
        // Arrange
        _kdsOrderRepoMock.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((KdsOrder?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.StartOrderAsync(999, 1));
    }

    [Fact]
    public async Task StartOrderAsync_WithAlreadyStartedOrder_ThrowsInvalidOperationException()
    {
        // Arrange
        var kdsOrder = new KdsOrder
        {
            Id = 1,
            Status = KdsOrderStatus.InProgress,
            IsActive = true
        };

        _kdsOrderRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(kdsOrder);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.StartOrderAsync(1, 1));
    }

    #endregion

    #region MarkItemDoneAsync Tests

    [Fact]
    public async Task MarkItemDoneAsync_WithValidItem_MarksItemDone()
    {
        // Arrange
        var orderItem = new KdsOrderItem
        {
            Id = 1,
            KdsOrderId = 1,
            StationId = 1,
            Status = KdsItemStatus.Preparing,
            IsActive = true
        };

        var kdsOrder = new KdsOrder
        {
            Id = 1,
            OrderNumber = "ORD001",
            Status = KdsOrderStatus.InProgress,
            ReceivedAt = DateTime.UtcNow,
            IsActive = true
        };

        _kdsOrderItemRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(orderItem);

        _kdsOrderRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(kdsOrder);

        _kdsOrderItemRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsOrderItem, bool>>>()))
            .ReturnsAsync(new List<KdsOrderItem> { orderItem });

        _stationRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new KdsStation { Id = 1, Name = "Hot Line" });

        // Act
        var result = await _service.MarkItemDoneAsync(1, 1);

        // Assert
        result.Status.Should().Be(KdsItemStatusDto.Done);
        orderItem.Status.Should().Be(KdsItemStatus.Done);
        orderItem.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkItemDoneAsync_WithInvalidItem_ThrowsKeyNotFoundException()
    {
        // Arrange
        _kdsOrderItemRepoMock.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((KdsOrderItem?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.MarkItemDoneAsync(999, 1));
    }

    #endregion

    #region BumpOrderAsync Tests

    [Fact]
    public async Task BumpOrderAsync_WithValidOrder_BumpsOrder()
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

        _kdsOrderRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(kdsOrder);

        _kdsOrderItemRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsOrderItem, bool>>>()))
            .ReturnsAsync(new List<KdsOrderItem> { orderItem });

        _stationRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new KdsStation { Id = 1, Name = "Hot Line" });

        BumpOrderEventArgs? eventResult = null;
        _service.OrderBumped += (sender, args) => eventResult = args;

        // Act
        var result = await _service.BumpOrderAsync(1, 1);

        // Assert
        result.Status.Should().Be(KdsOrderStatusDto.Bumped);
        kdsOrder.Status.Should().Be(KdsOrderStatus.Bumped);
        kdsOrder.BumpedAt.Should().NotBeNull();
        orderItem.Status.Should().Be(KdsItemStatus.Done);
        eventResult.Should().NotBeNull();
    }

    [Fact]
    public async Task BumpOrderAsync_WithInvalidOrder_ThrowsKeyNotFoundException()
    {
        // Arrange
        _kdsOrderRepoMock.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((KdsOrder?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.BumpOrderAsync(999, 1));
    }

    #endregion

    #region RecallOrderAsync Tests

    [Fact]
    public async Task RecallOrderAsync_WithBumpedOrder_RecallsOrder()
    {
        // Arrange
        var kdsOrder = new KdsOrder
        {
            Id = 1,
            OrderNumber = "ORD001",
            Status = KdsOrderStatus.Bumped,
            BumpedAt = DateTime.UtcNow.AddMinutes(-1),
            ReceivedAt = DateTime.UtcNow.AddMinutes(-5),
            IsActive = true
        };

        var orderItem = new KdsOrderItem
        {
            Id = 1,
            KdsOrderId = 1,
            StationId = 1,
            Status = KdsItemStatus.Done,
            IsActive = true
        };

        _kdsOrderRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(kdsOrder);

        _kdsOrderItemRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsOrderItem, bool>>>()))
            .ReturnsAsync(new List<KdsOrderItem> { orderItem });

        _stationRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new KdsStation { Id = 1, Name = "Hot Line" });

        KdsOrderDto? eventResult = null;
        _service.OrderRecalled += (sender, dto) => eventResult = dto;

        // Act
        var result = await _service.RecallOrderAsync(1, 1);

        // Assert
        result.Status.Should().Be(KdsOrderStatusDto.InProgress);
        kdsOrder.Status.Should().Be(KdsOrderStatus.InProgress);
        kdsOrder.RecallCount.Should().Be(1);
        orderItem.Status.Should().Be(KdsItemStatus.Preparing);
        eventResult.Should().NotBeNull();
    }

    [Fact]
    public async Task RecallOrderAsync_WithNonBumpedOrder_ThrowsInvalidOperationException()
    {
        // Arrange
        var kdsOrder = new KdsOrder
        {
            Id = 1,
            Status = KdsOrderStatus.New,
            IsActive = true
        };

        _kdsOrderRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(kdsOrder);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.RecallOrderAsync(1, 1));
    }

    #endregion

    #region MarkOrderServedAsync Tests

    [Fact]
    public async Task MarkOrderServedAsync_WithBumpedOrder_MarksAsServed()
    {
        // Arrange
        var kdsOrder = new KdsOrder
        {
            Id = 1,
            OrderNumber = "ORD001",
            Status = KdsOrderStatus.Bumped,
            ReceivedAt = DateTime.UtcNow.AddMinutes(-10),
            IsActive = true
        };

        _kdsOrderRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(kdsOrder);

        _kdsOrderItemRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsOrderItem, bool>>>()))
            .ReturnsAsync(new List<KdsOrderItem>());

        KdsOrderDto? eventResult = null;
        _service.OrderServed += (sender, dto) => eventResult = dto;

        // Act
        var result = await _service.MarkOrderServedAsync(1, 1);

        // Assert
        result.Status.Should().Be(KdsOrderStatusDto.Served);
        kdsOrder.Status.Should().Be(KdsOrderStatus.Served);
        kdsOrder.ServedAt.Should().NotBeNull();
        eventResult.Should().NotBeNull();
    }

    #endregion

    #region GetReadyOrdersAsync Tests

    [Fact]
    public async Task GetReadyOrdersAsync_ReturnsReadyOrders()
    {
        // Arrange
        var orderItems = new List<KdsOrderItem>
        {
            new() { Id = 1, KdsOrderId = 1, StationId = 1, IsActive = true }
        };

        var kdsOrder = new KdsOrder
        {
            Id = 1,
            OrderNumber = "ORD001",
            Status = KdsOrderStatus.Ready,
            ReceivedAt = DateTime.UtcNow,
            IsActive = true
        };

        _kdsOrderItemRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsOrderItem, bool>>>()))
            .ReturnsAsync(orderItems);

        _kdsOrderRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsOrder, bool>>>()))
            .ReturnsAsync(new List<KdsOrder> { kdsOrder });

        _stationRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new KdsStation { Id = 1, Name = "Hot Line" });

        // Act
        var result = await _service.GetReadyOrdersAsync(1);

        // Assert
        result.Should().HaveCount(1);
        result[0].Status.Should().Be(KdsOrderStatusDto.Ready);
    }

    #endregion

    #region GetStatusLogAsync Tests

    [Fact]
    public async Task GetStatusLogAsync_ReturnsStatusLogs()
    {
        // Arrange
        var logs = new List<KdsOrderStatusLog>
        {
            new()
            {
                Id = 1,
                KdsOrderId = 1,
                FromStatus = KdsOrderStatus.New,
                ToStatus = KdsOrderStatus.InProgress,
                ChangedAt = DateTime.UtcNow,
                IsActive = true
            }
        };

        _statusLogRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsOrderStatusLog, bool>>>()))
            .ReturnsAsync(logs);

        // Act
        var result = await _service.GetStatusLogAsync(1);

        // Assert
        result.Should().HaveCount(1);
        result[0].FromStatus.Should().Be(KdsOrderStatusDto.New);
        result[0].ToStatus.Should().Be(KdsOrderStatusDto.InProgress);
    }

    #endregion

    #region BulkBumpOrdersAsync Tests

    [Fact]
    public async Task BulkBumpOrdersAsync_WithMultipleOrders_BumpsAll()
    {
        // Arrange
        var orders = new List<KdsOrder>
        {
            new() { Id = 1, Status = KdsOrderStatus.InProgress, ReceivedAt = DateTime.UtcNow, IsActive = true },
            new() { Id = 2, Status = KdsOrderStatus.InProgress, ReceivedAt = DateTime.UtcNow, IsActive = true }
        };

        var items1 = new List<KdsOrderItem>
        {
            new() { Id = 1, KdsOrderId = 1, StationId = 1, Status = KdsItemStatus.Preparing, IsActive = true }
        };

        var items2 = new List<KdsOrderItem>
        {
            new() { Id = 2, KdsOrderId = 2, StationId = 1, Status = KdsItemStatus.Preparing, IsActive = true }
        };

        _kdsOrderRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(orders[0]);
        _kdsOrderRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(orders[1]);

        _kdsOrderItemRepoMock.SetupSequence(r => r.FindAsync(It.IsAny<Expression<Func<KdsOrderItem, bool>>>()))
            .ReturnsAsync(items1)
            .ReturnsAsync(items2);

        _stationRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new KdsStation { Id = 1, Name = "Hot Line" });

        // Act
        var result = await _service.BulkBumpOrdersAsync(new[] { 1, 2 }, 1);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(o => o.Status == KdsOrderStatusDto.Bumped);
    }

    #endregion
}
