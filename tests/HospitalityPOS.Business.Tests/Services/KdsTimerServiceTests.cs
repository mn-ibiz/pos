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
/// Unit tests for KdsTimerService.
/// </summary>
public class KdsTimerServiceTests
{
    private readonly Mock<IRepository<KdsOrder>> _kdsOrderRepoMock;
    private readonly Mock<IRepository<KdsOrderItem>> _kdsOrderItemRepoMock;
    private readonly Mock<IRepository<KdsTimerConfig>> _timerConfigRepoMock;
    private readonly Mock<IRepository<KdsStation>> _stationRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<KdsTimerService>> _loggerMock;
    private readonly KdsTimerService _service;

    public KdsTimerServiceTests()
    {
        _kdsOrderRepoMock = new Mock<IRepository<KdsOrder>>();
        _kdsOrderItemRepoMock = new Mock<IRepository<KdsOrderItem>>();
        _timerConfigRepoMock = new Mock<IRepository<KdsTimerConfig>>();
        _stationRepoMock = new Mock<IRepository<KdsStation>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<KdsTimerService>>();

        _service = new KdsTimerService(
            _kdsOrderRepoMock.Object,
            _kdsOrderItemRepoMock.Object,
            _timerConfigRepoMock.Object,
            _stationRepoMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullKdsOrderRepository_ThrowsArgumentNullException()
    {
        var act = () => new KdsTimerService(
            null!,
            _kdsOrderItemRepoMock.Object,
            _timerConfigRepoMock.Object,
            _stationRepoMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("kdsOrderRepository");
    }

    [Fact]
    public void Constructor_WithNullTimerConfigRepository_ThrowsArgumentNullException()
    {
        var act = () => new KdsTimerService(
            _kdsOrderRepoMock.Object,
            _kdsOrderItemRepoMock.Object,
            null!,
            _stationRepoMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("timerConfigRepository");
    }

    #endregion

    #region GetOrderTimerStatusAsync Tests

    [Fact]
    public async Task GetOrderTimerStatusAsync_WithNewOrder_ReturnsGreenStatus()
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

        var timerConfig = new KdsTimerConfig
        {
            Id = 1,
            StoreId = 1,
            GreenThresholdMinutes = 5,
            YellowThresholdMinutes = 10,
            RedThresholdMinutes = 15,
            IsActive = true
        };

        _kdsOrderRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(kdsOrder);

        _timerConfigRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsTimerConfig, bool>>>()))
            .ReturnsAsync(new List<KdsTimerConfig> { timerConfig });

        // Act
        var result = await _service.GetOrderTimerStatusAsync(1, 1);

        // Assert
        result.OrderId.Should().Be(1);
        result.CurrentColor.Should().Be(TimerColorDto.Green);
        result.ElapsedSeconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetOrderTimerStatusAsync_WithOldOrder_ReturnsYellowStatus()
    {
        // Arrange
        var kdsOrder = new KdsOrder
        {
            Id = 1,
            OrderNumber = "ORD001",
            Status = KdsOrderStatus.InProgress,
            ReceivedAt = DateTime.UtcNow.AddMinutes(-8),
            IsActive = true
        };

        var timerConfig = new KdsTimerConfig
        {
            Id = 1,
            StoreId = 1,
            GreenThresholdMinutes = 5,
            YellowThresholdMinutes = 10,
            RedThresholdMinutes = 15,
            IsActive = true
        };

        _kdsOrderRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(kdsOrder);

        _timerConfigRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsTimerConfig, bool>>>()))
            .ReturnsAsync(new List<KdsTimerConfig> { timerConfig });

        // Act
        var result = await _service.GetOrderTimerStatusAsync(1, 1);

        // Assert
        result.CurrentColor.Should().Be(TimerColorDto.Yellow);
    }

    [Fact]
    public async Task GetOrderTimerStatusAsync_WithVeryOldOrder_ReturnsRedStatus()
    {
        // Arrange
        var kdsOrder = new KdsOrder
        {
            Id = 1,
            OrderNumber = "ORD001",
            Status = KdsOrderStatus.InProgress,
            ReceivedAt = DateTime.UtcNow.AddMinutes(-20),
            IsActive = true
        };

        var timerConfig = new KdsTimerConfig
        {
            Id = 1,
            StoreId = 1,
            GreenThresholdMinutes = 5,
            YellowThresholdMinutes = 10,
            RedThresholdMinutes = 15,
            IsActive = true
        };

        _kdsOrderRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(kdsOrder);

        _timerConfigRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsTimerConfig, bool>>>()))
            .ReturnsAsync(new List<KdsTimerConfig> { timerConfig });

        // Act
        var result = await _service.GetOrderTimerStatusAsync(1, 1);

        // Assert
        result.CurrentColor.Should().Be(TimerColorDto.Red);
        result.IsOverdue.Should().BeTrue();
    }

    [Fact]
    public async Task GetOrderTimerStatusAsync_WithInvalidOrder_ThrowsKeyNotFoundException()
    {
        // Arrange
        _kdsOrderRepoMock.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((KdsOrder?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.GetOrderTimerStatusAsync(999, 1));
    }

    #endregion

    #region SetOrderPriorityAsync Tests

    [Fact]
    public async Task SetOrderPriorityAsync_WithNormalToRush_UpdatesPriority()
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

        KdsOrderPriorityChangeEventArgs? eventResult = null;
        _service.PriorityChanged += (sender, args) => eventResult = args;

        // Act
        var result = await _service.SetOrderPriorityAsync(1, OrderPriorityDto.Rush, 1);

        // Assert
        result.Priority.Should().Be(OrderPriorityDto.Rush);
        result.IsPriority.Should().BeTrue();
        kdsOrder.Priority.Should().Be(OrderPriority.Rush);
        eventResult.Should().NotBeNull();
        eventResult!.NewPriority.Should().Be(OrderPriorityDto.Rush);
    }

    [Fact]
    public async Task SetOrderPriorityAsync_WithVIPPriority_SetsIsPriority()
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

        // Act
        var result = await _service.SetOrderPriorityAsync(1, OrderPriorityDto.VIP, 1);

        // Assert
        result.Priority.Should().Be(OrderPriorityDto.VIP);
        result.IsPriority.Should().BeTrue();
    }

    #endregion

    #region GetOverdueOrdersAsync Tests

    [Fact]
    public async Task GetOverdueOrdersAsync_ReturnsOverdueOrders()
    {
        // Arrange
        var orderItems = new List<KdsOrderItem>
        {
            new() { Id = 1, KdsOrderId = 1, StationId = 1, IsActive = true }
        };

        var overdueOrder = new KdsOrder
        {
            Id = 1,
            OrderNumber = "ORD001",
            Status = KdsOrderStatus.InProgress,
            ReceivedAt = DateTime.UtcNow.AddMinutes(-20),
            IsActive = true
        };

        var timerConfig = new KdsTimerConfig
        {
            Id = 1,
            StoreId = 1,
            GreenThresholdMinutes = 5,
            YellowThresholdMinutes = 10,
            RedThresholdMinutes = 15,
            IsActive = true
        };

        _kdsOrderItemRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsOrderItem, bool>>>()))
            .ReturnsAsync(orderItems);

        _kdsOrderRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsOrder, bool>>>()))
            .ReturnsAsync(new List<KdsOrder> { overdueOrder });

        _timerConfigRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsTimerConfig, bool>>>()))
            .ReturnsAsync(new List<KdsTimerConfig> { timerConfig });

        _stationRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new KdsStation { Id = 1, Name = "Hot Line" });

        // Act
        var result = await _service.GetOverdueOrdersAsync(1);

        // Assert
        result.Should().HaveCount(1);
        result[0].OrderNumber.Should().Be("ORD001");
    }

    #endregion

    #region GetTimerConfigurationAsync Tests

    [Fact]
    public async Task GetTimerConfigurationAsync_WithExistingConfig_ReturnsConfig()
    {
        // Arrange
        var config = new KdsTimerConfig
        {
            Id = 1,
            StoreId = 1,
            GreenThresholdMinutes = 5,
            YellowThresholdMinutes = 10,
            RedThresholdMinutes = 15,
            EnableAudioAlerts = true,
            IsActive = true
        };

        _timerConfigRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsTimerConfig, bool>>>()))
            .ReturnsAsync(new List<KdsTimerConfig> { config });

        // Act
        var result = await _service.GetTimerConfigurationAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.GreenThresholdMinutes.Should().Be(5);
        result.YellowThresholdMinutes.Should().Be(10);
        result.RedThresholdMinutes.Should().Be(15);
        result.EnableAudioAlerts.Should().BeTrue();
    }

    [Fact]
    public async Task GetTimerConfigurationAsync_WithNoConfig_ReturnsNull()
    {
        // Arrange
        _timerConfigRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsTimerConfig, bool>>>()))
            .ReturnsAsync(new List<KdsTimerConfig>());

        // Act
        var result = await _service.GetTimerConfigurationAsync(1);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region UpdateTimerConfigurationAsync Tests

    [Fact]
    public async Task UpdateTimerConfigurationAsync_WithValidConfig_UpdatesConfig()
    {
        // Arrange
        var existingConfig = new KdsTimerConfig
        {
            Id = 1,
            StoreId = 1,
            GreenThresholdMinutes = 5,
            YellowThresholdMinutes = 10,
            RedThresholdMinutes = 15,
            IsActive = true
        };

        var updateDto = new UpdateTimerConfigDto
        {
            GreenThresholdMinutes = 7,
            YellowThresholdMinutes = 12,
            RedThresholdMinutes = 18,
            EnableAudioAlerts = true,
            AudioAlertIntervalSeconds = 30
        };

        _timerConfigRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsTimerConfig, bool>>>()))
            .ReturnsAsync(new List<KdsTimerConfig> { existingConfig });

        // Act
        var result = await _service.UpdateTimerConfigurationAsync(1, updateDto);

        // Assert
        result.GreenThresholdMinutes.Should().Be(7);
        result.YellowThresholdMinutes.Should().Be(12);
        result.RedThresholdMinutes.Should().Be(18);
        result.EnableAudioAlerts.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateTimerConfigurationAsync_WithNoExistingConfig_CreatesNewConfig()
    {
        // Arrange
        var updateDto = new UpdateTimerConfigDto
        {
            GreenThresholdMinutes = 5,
            YellowThresholdMinutes = 10,
            RedThresholdMinutes = 15
        };

        _timerConfigRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsTimerConfig, bool>>>()))
            .ReturnsAsync(new List<KdsTimerConfig>());

        _timerConfigRepoMock.Setup(r => r.AddAsync(It.IsAny<KdsTimerConfig>()))
            .Returns(Task.CompletedTask)
            .Callback<KdsTimerConfig>(c => c.Id = 1);

        // Act
        var result = await _service.UpdateTimerConfigurationAsync(1, updateDto);

        // Assert
        result.GreenThresholdMinutes.Should().Be(5);
        _timerConfigRepoMock.Verify(r => r.AddAsync(It.IsAny<KdsTimerConfig>()), Times.Once);
    }

    #endregion

    #region CheckAndTriggerAlertsAsync Tests

    [Fact]
    public async Task CheckAndTriggerAlertsAsync_WithOverdueOrders_TriggersAlert()
    {
        // Arrange
        var orderItems = new List<KdsOrderItem>
        {
            new() { Id = 1, KdsOrderId = 1, StationId = 1, IsActive = true }
        };

        var overdueOrder = new KdsOrder
        {
            Id = 1,
            OrderNumber = "ORD001",
            Status = KdsOrderStatus.InProgress,
            ReceivedAt = DateTime.UtcNow.AddMinutes(-20),
            IsActive = true
        };

        var timerConfig = new KdsTimerConfig
        {
            Id = 1,
            StoreId = 1,
            GreenThresholdMinutes = 5,
            YellowThresholdMinutes = 10,
            RedThresholdMinutes = 15,
            EnableAudioAlerts = true,
            IsActive = true
        };

        _kdsOrderItemRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsOrderItem, bool>>>()))
            .ReturnsAsync(orderItems);

        _kdsOrderRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsOrder, bool>>>()))
            .ReturnsAsync(new List<KdsOrder> { overdueOrder });

        _timerConfigRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsTimerConfig, bool>>>()))
            .ReturnsAsync(new List<KdsTimerConfig> { timerConfig });

        _stationRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new KdsStation { Id = 1, Name = "Hot Line" });

        AudioAlertEventArgs? eventResult = null;
        _service.AudioAlertNeeded += (sender, args) => eventResult = args;

        // Act
        await _service.CheckAndTriggerAlertsAsync(1);

        // Assert
        eventResult.Should().NotBeNull();
        eventResult!.AlertType.Should().Be("overdue");
        eventResult.OverdueCount.Should().Be(1);
    }

    #endregion

    #region GetAverageCompletionTimeAsync Tests

    [Fact]
    public async Task GetAverageCompletionTimeAsync_WithCompletedOrders_ReturnsAverage()
    {
        // Arrange
        var completedOrders = new List<KdsOrder>
        {
            new()
            {
                Id = 1,
                ReceivedAt = DateTime.UtcNow.AddMinutes(-10),
                BumpedAt = DateTime.UtcNow.AddMinutes(-5),
                Status = KdsOrderStatus.Served,
                IsActive = true
            },
            new()
            {
                Id = 2,
                ReceivedAt = DateTime.UtcNow.AddMinutes(-8),
                BumpedAt = DateTime.UtcNow.AddMinutes(-4),
                Status = KdsOrderStatus.Served,
                IsActive = true
            }
        };

        var orderItems = new List<KdsOrderItem>
        {
            new() { Id = 1, KdsOrderId = 1, StationId = 1, IsActive = true },
            new() { Id = 2, KdsOrderId = 2, StationId = 1, IsActive = true }
        };

        _kdsOrderItemRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsOrderItem, bool>>>()))
            .ReturnsAsync(orderItems);

        _kdsOrderRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KdsOrder, bool>>>()))
            .ReturnsAsync(completedOrders);

        // Act
        var result = await _service.GetAverageCompletionTimeAsync(1, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow);

        // Assert
        result.Should().BeGreaterThan(0);
        result.Should().BeLessThan(600); // Less than 10 minutes in seconds
    }

    #endregion
}
