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
/// Unit tests for ExpoService.
/// </summary>
public class ExpoServiceTests
{
    private readonly Mock<IRepository<KdsOrder>> _kdsOrderRepoMock;
    private readonly Mock<IRepository<KdsOrderItem>> _kdsOrderItemRepoMock;
    private readonly Mock<IRepository<KdsStation>> _stationRepoMock;
    private readonly Mock<IRepository<AllCallMessage>> _allCallRepoMock;
    private readonly Mock<IRepository<AllCallMessageTarget>> _allCallTargetRepoMock;
    private readonly Mock<IRepository<AllCallMessageDismissal>> _allCallDismissalRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<ExpoService>> _loggerMock;
    private readonly ExpoService _service;

    public ExpoServiceTests()
    {
        _kdsOrderRepoMock = new Mock<IRepository<KdsOrder>>();
        _kdsOrderItemRepoMock = new Mock<IRepository<KdsOrderItem>>();
        _stationRepoMock = new Mock<IRepository<KdsStation>>();
        _allCallRepoMock = new Mock<IRepository<AllCallMessage>>();
        _allCallTargetRepoMock = new Mock<IRepository<AllCallMessageTarget>>();
        _allCallDismissalRepoMock = new Mock<IRepository<AllCallMessageDismissal>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<ExpoService>>();

        _service = new ExpoService(
            _kdsOrderRepoMock.Object,
            _kdsOrderItemRepoMock.Object,
            _stationRepoMock.Object,
            _allCallRepoMock.Object,
            _allCallTargetRepoMock.Object,
            _allCallDismissalRepoMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullKdsOrderRepository_ThrowsArgumentNullException()
    {
        var act = () => new ExpoService(
            null!,
            _kdsOrderItemRepoMock.Object,
            _stationRepoMock.Object,
            _allCallRepoMock.Object,
            _allCallTargetRepoMock.Object,
            _allCallDismissalRepoMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("kdsOrderRepository");
    }

    [Fact]
    public void Constructor_WithNullAllCallRepository_ThrowsArgumentNullException()
    {
        var act = () => new ExpoService(
            _kdsOrderRepoMock.Object,
            _kdsOrderItemRepoMock.Object,
            _stationRepoMock.Object,
            null!,
            _allCallTargetRepoMock.Object,
            _allCallDismissalRepoMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("allCallRepository");
    }

    #endregion

    #region GetExpoOrderViewAsync Tests

    [Fact]
    public async Task GetExpoOrderViewAsync_WithValidOrder_ReturnsExpoView()
    {
        // Arrange
        var kdsOrder = new KdsOrder
        {
            Id = 1,
            OrderNumber = "ORD001",
            TableNumber = "T1",
            Status = KdsOrderStatus.InProgress,
            ReceivedAt = DateTime.UtcNow.AddMinutes(-5),
            IsActive = true
        };

        var stations = new List<KdsStation>
        {
            new() { Id = 1, Name = "Hot Line", StoreId = 1, IsActive = true },
            new() { Id = 2, Name = "Cold Line", StoreId = 1, IsActive = true }
        };

        var orderItems = new List<KdsOrderItem>
        {
            new() { Id = 1, KdsOrderId = 1, StationId = 1, ProductName = "Burger", Status = KdsItemStatus.Done, IsActive = true },
            new() { Id = 2, KdsOrderId = 1, StationId = 2, ProductName = "Salad", Status = KdsItemStatus.Preparing, IsActive = true }
        };

        _kdsOrderRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(kdsOrder);

        _stationRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsStation, bool>>>()))
            .ReturnsAsync(stations);

        _kdsOrderItemRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsOrderItem, bool>>>()))
            .ReturnsAsync(orderItems);

        _stationRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(stations[0]);
        _stationRepoMock.Setup(r => r.GetByIdAsync(2))
            .ReturnsAsync(stations[1]);

        // Act
        var result = await _service.GetExpoOrderViewAsync(1, 1);

        // Assert
        result.Should().NotBeNull();
        result!.OrderId.Should().Be(1);
        result.OrderNumber.Should().Be("ORD001");
        result.StationStatuses.Should().HaveCount(2);
        result.IsReadyToServe.Should().BeFalse();
    }

    [Fact]
    public async Task GetExpoOrderViewAsync_WithAllItemsDone_IsReadyToServe()
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

        var stations = new List<KdsStation>
        {
            new() { Id = 1, Name = "Hot Line", StoreId = 1, IsActive = true }
        };

        var orderItems = new List<KdsOrderItem>
        {
            new() { Id = 1, KdsOrderId = 1, StationId = 1, Status = KdsItemStatus.Done, IsActive = true },
            new() { Id = 2, KdsOrderId = 1, StationId = 1, Status = KdsItemStatus.Done, IsActive = true }
        };

        _kdsOrderRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(kdsOrder);

        _stationRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsStation, bool>>>()))
            .ReturnsAsync(stations);

        _kdsOrderItemRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsOrderItem, bool>>>()))
            .ReturnsAsync(orderItems);

        _stationRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(stations[0]);

        // Act
        var result = await _service.GetExpoOrderViewAsync(1, 1);

        // Assert
        result!.IsReadyToServe.Should().BeTrue();
    }

    [Fact]
    public async Task GetExpoOrderViewAsync_WithInvalidOrder_ReturnsNull()
    {
        // Arrange
        _kdsOrderRepoMock.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((KdsOrder?)null);

        // Act
        var result = await _service.GetExpoOrderViewAsync(999, 1);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllExpoOrdersAsync Tests

    [Fact]
    public async Task GetAllExpoOrdersAsync_ReturnsActiveOrders()
    {
        // Arrange
        var expoStation = new KdsStation
        {
            Id = 1,
            Name = "Expo",
            StoreId = 1,
            IsExpo = true,
            IsActive = true
        };

        var kdsOrders = new List<KdsOrder>
        {
            new() { Id = 1, OrderNumber = "ORD001", Status = KdsOrderStatus.InProgress, ReceivedAt = DateTime.UtcNow, IsActive = true },
            new() { Id = 2, OrderNumber = "ORD002", Status = KdsOrderStatus.Ready, ReceivedAt = DateTime.UtcNow, IsActive = true }
        };

        var orderItems = new List<KdsOrderItem>
        {
            new() { Id = 1, KdsOrderId = 1, StationId = 2, Status = KdsItemStatus.Preparing, IsActive = true },
            new() { Id = 2, KdsOrderId = 2, StationId = 2, Status = KdsItemStatus.Done, IsActive = true }
        };

        var stations = new List<KdsStation>
        {
            expoStation,
            new() { Id = 2, Name = "Hot Line", StoreId = 1, IsActive = true }
        };

        _stationRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsStation, bool>>>()))
            .ReturnsAsync(stations);

        _kdsOrderRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsOrder, bool>>>()))
            .ReturnsAsync(kdsOrders);

        _kdsOrderItemRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsOrderItem, bool>>>()))
            .ReturnsAsync(orderItems);

        _stationRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(stations[1]);

        // Act
        var result = await _service.GetAllExpoOrdersAsync(1);

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region MarkOrderCompleteAsync Tests

    [Fact]
    public async Task MarkOrderCompleteAsync_WithValidOrder_MarksComplete()
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

        var orderItems = new List<KdsOrderItem>
        {
            new() { Id = 1, KdsOrderId = 1, StationId = 1, Status = KdsItemStatus.Preparing, IsActive = true }
        };

        _kdsOrderRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(kdsOrder);

        _kdsOrderItemRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsOrderItem, bool>>>()))
            .ReturnsAsync(orderItems);

        _stationRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new KdsStation { Id = 1, Name = "Hot Line" });

        _stationRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsStation, bool>>>()))
            .ReturnsAsync(new List<KdsStation> { new() { Id = 1, Name = "Hot Line", StoreId = 1, IsActive = true } });

        ExpoOrderViewDto? eventResult = null;
        _service.OrderComplete += (sender, dto) => eventResult = dto;

        // Act
        var result = await _service.MarkOrderCompleteAsync(1, 1);

        // Assert
        result.Should().NotBeNull();
        kdsOrder.Status.Should().Be(KdsOrderStatus.Ready);
        eventResult.Should().NotBeNull();
    }

    #endregion

    #region MarkOrderServedAsync Tests

    [Fact]
    public async Task MarkOrderServedAsync_WithReadyOrder_MarksServed()
    {
        // Arrange
        var kdsOrder = new KdsOrder
        {
            Id = 1,
            OrderNumber = "ORD001",
            Status = KdsOrderStatus.Ready,
            ReceivedAt = DateTime.UtcNow,
            IsActive = true
        };

        var orderItems = new List<KdsOrderItem>
        {
            new() { Id = 1, KdsOrderId = 1, StationId = 1, Status = KdsItemStatus.Done, IsActive = true }
        };

        _kdsOrderRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(kdsOrder);

        _kdsOrderItemRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsOrderItem, bool>>>()))
            .ReturnsAsync(orderItems);

        _stationRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new KdsStation { Id = 1, Name = "Hot Line" });

        _stationRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsStation, bool>>>()))
            .ReturnsAsync(new List<KdsStation> { new() { Id = 1, Name = "Hot Line", StoreId = 1, IsActive = true } });

        ExpoOrderViewDto? eventResult = null;
        _service.OrderServed += (sender, dto) => eventResult = dto;

        // Act
        var result = await _service.MarkOrderServedAsync(1, 1);

        // Assert
        result.Should().NotBeNull();
        kdsOrder.Status.Should().Be(KdsOrderStatus.Served);
        kdsOrder.ServedAt.Should().NotBeNull();
        eventResult.Should().NotBeNull();
    }

    #endregion

    #region SendAllCallAsync Tests

    [Fact]
    public async Task SendAllCallAsync_WithValidMessage_SendsMessage()
    {
        // Arrange
        var dto = new SendAllCallDto
        {
            StoreId = 1,
            Message = "Kitchen needs attention!",
            Priority = AllCallPriorityDto.High,
            TargetStationIds = new List<int> { 1, 2 }
        };

        var stations = new List<KdsStation>
        {
            new() { Id = 1, Name = "Hot Line", StoreId = 1, IsActive = true },
            new() { Id = 2, Name = "Cold Line", StoreId = 1, IsActive = true }
        };

        _stationRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsStation, bool>>>()))
            .ReturnsAsync(stations);

        _allCallRepoMock.Setup(r => r.AddAsync(It.IsAny<AllCallMessage>()))
            .Returns(Task.CompletedTask)
            .Callback<AllCallMessage>(m => m.Id = 1);

        AllCallMessageDto? eventResult = null;
        _service.AllCallSent += (sender, message) => eventResult = message;

        // Act
        var result = await _service.SendAllCallAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Message.Should().Be("Kitchen needs attention!");
        result.Priority.Should().Be(AllCallPriorityDto.High);
        eventResult.Should().NotBeNull();
    }

    [Fact]
    public async Task SendAllCallAsync_WithNoTargets_BroadcastsToAllStations()
    {
        // Arrange
        var dto = new SendAllCallDto
        {
            StoreId = 1,
            Message = "General announcement",
            Priority = AllCallPriorityDto.Normal
        };

        var stations = new List<KdsStation>
        {
            new() { Id = 1, Name = "Hot Line", StoreId = 1, IsActive = true },
            new() { Id = 2, Name = "Cold Line", StoreId = 1, IsActive = true },
            new() { Id = 3, Name = "Expo", StoreId = 1, IsExpo = true, IsActive = true }
        };

        _stationRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsStation, bool>>>()))
            .ReturnsAsync(stations);

        _allCallRepoMock.Setup(r => r.AddAsync(It.IsAny<AllCallMessage>()))
            .Returns(Task.CompletedTask)
            .Callback<AllCallMessage>(m => m.Id = 1);

        // Act
        var result = await _service.SendAllCallAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.TargetStationIds.Should().HaveCount(3);
    }

    #endregion

    #region GetActiveAllCallsAsync Tests

    [Fact]
    public async Task GetActiveAllCallsAsync_ReturnsActiveCalls()
    {
        // Arrange
        var allCalls = new List<AllCallMessage>
        {
            new()
            {
                Id = 1,
                StoreId = 1,
                Message = "Urgent message",
                Priority = AllCallPriority.High,
                SentAt = DateTime.UtcNow.AddMinutes(-2),
                ExpiresAt = DateTime.UtcNow.AddMinutes(28),
                IsActive = true
            }
        };

        var targets = new List<AllCallMessageTarget>
        {
            new() { Id = 1, AllCallMessageId = 1, StationId = 1, IsActive = true }
        };

        _allCallRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AllCallMessage, bool>>>()))
            .ReturnsAsync(allCalls);

        _allCallTargetRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AllCallMessageTarget, bool>>>()))
            .ReturnsAsync(targets);

        _allCallDismissalRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AllCallMessageDismissal, bool>>>()))
            .ReturnsAsync(new List<AllCallMessageDismissal>());

        // Act
        var result = await _service.GetActiveAllCallsAsync(1);

        // Assert
        result.Should().HaveCount(1);
        result[0].Message.Should().Be("Urgent message");
    }

    #endregion

    #region DismissAllCallAsync Tests

    [Fact]
    public async Task DismissAllCallAsync_WithValidCall_DismissesForStation()
    {
        // Arrange
        var allCall = new AllCallMessage
        {
            Id = 1,
            StoreId = 1,
            Message = "Test",
            SentAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            IsActive = true
        };

        _allCallRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(allCall);

        _allCallDismissalRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AllCallMessageDismissal, bool>>>()))
            .ReturnsAsync(new List<AllCallMessageDismissal>());

        // Act
        await _service.DismissAllCallAsync(1, 1, 1);

        // Assert
        _allCallDismissalRepoMock.Verify(r => r.AddAsync(It.IsAny<AllCallMessageDismissal>()), Times.Once);
    }

    [Fact]
    public async Task DismissAllCallAsync_WithAlreadyDismissed_DoesNotAddAgain()
    {
        // Arrange
        var allCall = new AllCallMessage
        {
            Id = 1,
            StoreId = 1,
            Message = "Test",
            SentAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            IsActive = true
        };

        var existingDismissal = new AllCallMessageDismissal
        {
            Id = 1,
            AllCallMessageId = 1,
            StationId = 1,
            IsActive = true
        };

        _allCallRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(allCall);

        _allCallDismissalRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AllCallMessageDismissal, bool>>>()))
            .ReturnsAsync(new List<AllCallMessageDismissal> { existingDismissal });

        // Act
        await _service.DismissAllCallAsync(1, 1, 1);

        // Assert
        _allCallDismissalRepoMock.Verify(r => r.AddAsync(It.IsAny<AllCallMessageDismissal>()), Times.Never);
    }

    #endregion

    #region SendOrderBackAsync Tests

    [Fact]
    public async Task SendOrderBackAsync_WithValidOrder_SendsBack()
    {
        // Arrange
        var kdsOrder = new KdsOrder
        {
            Id = 1,
            OrderNumber = "ORD001",
            Status = KdsOrderStatus.Ready,
            ReceivedAt = DateTime.UtcNow,
            IsActive = true
        };

        var orderItems = new List<KdsOrderItem>
        {
            new() { Id = 1, KdsOrderId = 1, StationId = 1, Status = KdsItemStatus.Done, IsActive = true }
        };

        _kdsOrderRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(kdsOrder);

        _kdsOrderItemRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsOrderItem, bool>>>()))
            .ReturnsAsync(orderItems);

        _stationRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new KdsStation { Id = 1, Name = "Hot Line" });

        _stationRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsStation, bool>>>()))
            .ReturnsAsync(new List<KdsStation> { new() { Id = 1, Name = "Hot Line", StoreId = 1, IsActive = true } });

        ExpoOrderViewDto? eventResult = null;
        _service.OrderSentBack += (sender, dto) => eventResult = dto;

        // Act
        var result = await _service.SendOrderBackAsync(1, 1, "Steak needs more cooking");

        // Assert
        result.Should().NotBeNull();
        kdsOrder.Status.Should().Be(KdsOrderStatus.InProgress);
        kdsOrder.SentBackReason.Should().Be("Steak needs more cooking");
        kdsOrder.SentBackCount.Should().Be(1);
        orderItems[0].Status.Should().Be(KdsItemStatus.Preparing);
        eventResult.Should().NotBeNull();
    }

    #endregion

    #region GetExpoStatisticsAsync Tests

    [Fact]
    public async Task GetExpoStatisticsAsync_ReturnsStatistics()
    {
        // Arrange
        var kdsOrders = new List<KdsOrder>
        {
            new() { Id = 1, Status = KdsOrderStatus.InProgress, ReceivedAt = DateTime.UtcNow.AddMinutes(-5), IsActive = true },
            new() { Id = 2, Status = KdsOrderStatus.Ready, ReceivedAt = DateTime.UtcNow.AddMinutes(-3), IsActive = true },
            new() { Id = 3, Status = KdsOrderStatus.Served, ReceivedAt = DateTime.UtcNow.AddMinutes(-10), ServedAt = DateTime.UtcNow.AddMinutes(-2), IsActive = true }
        };

        var stations = new List<KdsStation>
        {
            new() { Id = 1, Name = "Hot Line", StoreId = 1, IsActive = true },
            new() { Id = 2, Name = "Expo", StoreId = 1, IsExpo = true, IsActive = true }
        };

        _stationRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsStation, bool>>>()))
            .ReturnsAsync(stations);

        _kdsOrderRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsOrder, bool>>>()))
            .ReturnsAsync(kdsOrders);

        // Act
        var result = await _service.GetExpoStatisticsAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.TotalActiveOrders.Should().Be(2);
        result.ReadyForPickup.Should().Be(1);
        result.ServedToday.Should().Be(1);
    }

    #endregion
}
