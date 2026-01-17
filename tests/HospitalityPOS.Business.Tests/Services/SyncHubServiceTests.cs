using FluentAssertions;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for SyncHubService.
/// Tests focus on the testable logic since SignalR HubConnection is difficult to mock directly.
/// </summary>
public class SyncHubServiceTests : IDisposable
{
    private readonly Mock<ILogger<SyncHubService>> _loggerMock;
    private readonly Mock<ISyncQueueService> _syncQueueServiceMock;
    private readonly SignalRConfiguration _configuration;

    public SyncHubServiceTests()
    {
        _loggerMock = new Mock<ILogger<SyncHubService>>();
        _syncQueueServiceMock = new Mock<ISyncQueueService>();
        _configuration = new SignalRConfiguration
        {
            HubUrl = "https://test-hub.example.com/sync",
            ReconnectDelaySeconds = 5,
            MaxReconnectAttempts = 3,
            KeepAliveIntervalSeconds = 15,
            BatchSize = 50,
            AutoReconnect = true
        };
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var action = () => new SyncHubService(null!, _syncQueueServiceMock.Object, _configuration);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullSyncQueueService_ThrowsArgumentNullException()
    {
        var action = () => new SyncHubService(_loggerMock.Object, null!, _configuration);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("syncQueueService");
    }

    [Fact]
    public void Constructor_WithNullConfiguration_UsesDefault()
    {
        // Act
        var service = new SyncHubService(_loggerMock.Object, _syncQueueServiceMock.Object, null);

        // Assert
        service.Configuration.Should().NotBeNull();
        service.Configuration.HubUrl.Should().BeEmpty();
        service.Configuration.ReconnectDelaySeconds.Should().Be(5);
        service.Configuration.MaxReconnectAttempts.Should().Be(10);
    }

    [Fact]
    public void Constructor_WithValidParameters_InitializesCorrectly()
    {
        // Act
        var service = new SyncHubService(_loggerMock.Object, _syncQueueServiceMock.Object, _configuration);

        // Assert
        service.Configuration.Should().Be(_configuration);
        service.IsConnected.Should().BeFalse();
        service.ConnectionStatus.State.Should().Be(SignalRConnectionState.Disconnected);
    }

    #endregion

    #region Connection Status Tests

    [Fact]
    public void ConnectionStatus_InitialState_IsDisconnected()
    {
        // Arrange
        var service = new SyncHubService(_loggerMock.Object, _syncQueueServiceMock.Object, _configuration);

        // Assert
        service.ConnectionStatus.State.Should().Be(SignalRConnectionState.Disconnected);
        service.ConnectionStatus.IsConnected.Should().BeFalse();
        service.ConnectionStatus.ServerUrl.Should().Be(_configuration.HubUrl);
    }

    [Fact]
    public void IsConnected_WhenNotConnected_ReturnsFalse()
    {
        // Arrange
        var service = new SyncHubService(_loggerMock.Object, _syncQueueServiceMock.Object, _configuration);

        // Assert
        service.IsConnected.Should().BeFalse();
    }

    #endregion

    #region Connect Tests

    [Fact]
    public async Task ConnectAsync_WithEmptyHubUrl_ReturnsFalse()
    {
        // Arrange
        var configWithNoUrl = new SignalRConfiguration { HubUrl = string.Empty };
        var service = new SyncHubService(_loggerMock.Object, _syncQueueServiceMock.Object, configWithNoUrl);

        // Act
        var result = await service.ConnectAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ConnectAsync_WithInvalidUrl_ReturnsFalseAndSetsError()
    {
        // Arrange
        var service = new SyncHubService(_loggerMock.Object, _syncQueueServiceMock.Object, _configuration);
        string? receivedError = null;
        service.ErrorOccurred += (sender, error) => receivedError = error;

        // Act - This will fail because the URL is not reachable
        var result = await service.ConnectAsync();

        // Assert
        result.Should().BeFalse();
        service.ConnectionStatus.LastError.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void UpdateConfiguration_WithValidConfiguration_UpdatesConfiguration()
    {
        // Arrange
        var service = new SyncHubService(_loggerMock.Object, _syncQueueServiceMock.Object, _configuration);
        var newConfig = new SignalRConfiguration
        {
            HubUrl = "https://new-hub.example.com/sync",
            BatchSize = 100,
            MaxReconnectAttempts = 5
        };

        // Act
        service.UpdateConfiguration(newConfig);

        // Assert
        service.Configuration.Should().Be(newConfig);
        service.Configuration.HubUrl.Should().Be("https://new-hub.example.com/sync");
        service.ConnectionStatus.ServerUrl.Should().Be("https://new-hub.example.com/sync");
    }

    [Fact]
    public void UpdateConfiguration_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        var service = new SyncHubService(_loggerMock.Object, _syncQueueServiceMock.Object, _configuration);

        // Act
        var action = () => service.UpdateConfiguration(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    #endregion

    #region Send Sync Item Tests

    [Fact]
    public async Task SendSyncItemAsync_WhenNotConnected_ReturnsFalse()
    {
        // Arrange
        var service = new SyncHubService(_loggerMock.Object, _syncQueueServiceMock.Object, _configuration);
        var item = new SignalRSyncItemDto
        {
            EntityType = "Product",
            EntityId = 1,
            Operation = "Create",
            Payload = "{}"
        };

        // Act
        var result = await service.SendSyncItemAsync(item);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendBatchAsync_WhenNotConnected_ReturnsZero()
    {
        // Arrange
        var service = new SyncHubService(_loggerMock.Object, _syncQueueServiceMock.Object, _configuration);
        var items = new List<SignalRSyncItemDto>
        {
            new SignalRSyncItemDto { EntityType = "Product", EntityId = 1 },
            new SignalRSyncItemDto { EntityType = "Product", EntityId = 2 }
        };

        // Act
        var result = await service.SendBatchAsync(items);

        // Assert
        result.Should().Be(0);
    }

    #endregion

    #region Sync Pending Items Tests

    [Fact]
    public async Task SyncPendingItemsAsync_WhenNotConnected_ReturnsEmptyProgress()
    {
        // Arrange
        var service = new SyncHubService(_loggerMock.Object, _syncQueueServiceMock.Object, _configuration);

        // Act
        var result = await service.SyncPendingItemsAsync();

        // Assert
        result.TotalItems.Should().Be(0);
        result.CompletedItems.Should().Be(0);
    }

    #endregion

    #region Request Batch Sync Tests

    [Fact]
    public async Task RequestBatchSyncAsync_WhenNotConnected_ReturnsNull()
    {
        // Arrange
        var service = new SyncHubService(_loggerMock.Object, _syncQueueServiceMock.Object, _configuration);
        var request = new SignalRBatchSyncRequestDto
        {
            StoreId = 1,
            BatchSize = 50
        };

        // Act
        var result = await service.RequestBatchSyncAsync(request);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Heartbeat Tests

    [Fact]
    public void LastHeartbeatResponse_Initially_IsNull()
    {
        // Arrange
        var service = new SyncHubService(_loggerMock.Object, _syncQueueServiceMock.Object, _configuration);

        // Assert
        service.LastHeartbeatResponse.Should().BeNull();
    }

    [Fact]
    public async Task SendHeartbeatAsync_WhenNotConnected_DoesNotThrow()
    {
        // Arrange
        var service = new SyncHubService(_loggerMock.Object, _syncQueueServiceMock.Object, _configuration);
        var heartbeat = new SignalRHeartbeatDto
        {
            StoreId = 1,
            PendingSyncCount = 5
        };

        // Act - Should not throw
        await service.SendHeartbeatAsync(heartbeat);

        // Assert - No exception thrown
        service.LastHeartbeatResponse.Should().BeNull();
    }

    #endregion

    #region Invoke Tests

    [Fact]
    public async Task InvokeAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = new SyncHubService(_loggerMock.Object, _syncQueueServiceMock.Object, _configuration);

        // Act
        var action = async () => await service.InvokeAsync("TestMethod", "arg1");

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Not connected to hub");
    }

    [Fact]
    public async Task InvokeAsyncGeneric_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = new SyncHubService(_loggerMock.Object, _syncQueueServiceMock.Object, _configuration);

        // Act
        var action = async () => await service.InvokeAsync<string>("TestMethod", "arg1");

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Not connected to hub");
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_MultipleCalls_DoesNotThrow()
    {
        // Arrange
        var service = new SyncHubService(_loggerMock.Object, _syncQueueServiceMock.Object, _configuration);

        // Act - Should not throw
        service.Dispose();
        service.Dispose();
    }

    #endregion

    #region Event Tests

    [Fact]
    public async Task ErrorOccurred_WhenConnectionFails_IsRaised()
    {
        // Arrange
        var service = new SyncHubService(_loggerMock.Object, _syncQueueServiceMock.Object, _configuration);
        string? receivedError = null;
        service.ErrorOccurred += (sender, error) => receivedError = error;

        // Act
        await service.ConnectAsync();

        // Assert - Error should be raised due to connection failure
        receivedError.Should().NotBeNull();
        receivedError.Should().Contain("Connection failed");
    }

    [Fact]
    public async Task ConnectionStatusChanged_WhenConnecting_IsRaised()
    {
        // Arrange
        var service = new SyncHubService(_loggerMock.Object, _syncQueueServiceMock.Object, _configuration);
        var statusChanges = new List<SignalRConnectionStatusDto>();
        service.ConnectionStatusChanged += (sender, status) => statusChanges.Add(status);

        // Act
        await service.ConnectAsync();

        // Assert - Status should change to Connecting, then Disconnected (on failure)
        statusChanges.Should().HaveCountGreaterOrEqualTo(1);
        statusChanges.Should().Contain(s => s.State == SignalRConnectionState.Connecting);
    }

    #endregion

    #region Factory Tests

    [Fact]
    public void SyncHubServiceFactory_Create_ReturnsNewInstance()
    {
        // Arrange
        var factory = new SyncHubServiceFactory(_loggerMock.Object, _syncQueueServiceMock.Object);
        var config = new SignalRConfiguration { HubUrl = "https://test.example.com/sync" };

        // Act
        var service = factory.Create(config);

        // Assert
        service.Should().NotBeNull();
        service.Should().BeOfType<SyncHubService>();
        service.Configuration.HubUrl.Should().Be("https://test.example.com/sync");
    }

    [Fact]
    public void SyncHubServiceFactory_Create_ReturnsUniqueInstances()
    {
        // Arrange
        var factory = new SyncHubServiceFactory(_loggerMock.Object, _syncQueueServiceMock.Object);
        var config = new SignalRConfiguration { HubUrl = "https://test.example.com/sync" };

        // Act
        var service1 = factory.Create(config);
        var service2 = factory.Create(config);

        // Assert
        service1.Should().NotBeSameAs(service2);
    }

    #endregion

    #region SignalR DTO Tests

    [Fact]
    public void SignalRConfiguration_DefaultValues_AreCorrect()
    {
        var config = new SignalRConfiguration();

        config.HubUrl.Should().BeEmpty();
        config.ReconnectDelaySeconds.Should().Be(5);
        config.MaxReconnectAttempts.Should().Be(10);
        config.KeepAliveIntervalSeconds.Should().Be(15);
        config.BatchSize.Should().Be(50);
        config.ConnectionTimeoutSeconds.Should().Be(30);
        config.AutoReconnect.Should().BeTrue();
        config.AccessToken.Should().BeNull();
    }

    [Fact]
    public void SignalRConnectionStatusDto_IsConnected_ReturnsTrueOnlyWhenConnected()
    {
        var status = new SignalRConnectionStatusDto();

        status.State = SignalRConnectionState.Disconnected;
        status.IsConnected.Should().BeFalse();

        status.State = SignalRConnectionState.Connecting;
        status.IsConnected.Should().BeFalse();

        status.State = SignalRConnectionState.Reconnecting;
        status.IsConnected.Should().BeFalse();

        status.State = SignalRConnectionState.Connected;
        status.IsConnected.Should().BeTrue();
    }

    [Fact]
    public void SignalRConnectionStatusDto_ConnectionDuration_CalculatesCorrectly()
    {
        var status = new SignalRConnectionStatusDto
        {
            State = SignalRConnectionState.Connected,
            LastConnectedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        status.ConnectionDuration.Should().NotBeNull();
        status.ConnectionDuration!.Value.TotalMinutes.Should().BeApproximately(5, 1);
    }

    [Fact]
    public void SignalRConnectionStatusDto_ConnectionDuration_IsNullWhenNotConnected()
    {
        var status = new SignalRConnectionStatusDto
        {
            State = SignalRConnectionState.Disconnected,
            LastConnectedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        status.ConnectionDuration.Should().BeNull();
    }

    [Fact]
    public void SignalRSyncProgressDto_ProgressPercent_CalculatesCorrectly()
    {
        var progress = new SignalRSyncProgressDto
        {
            TotalItems = 100,
            CompletedItems = 25
        };

        progress.ProgressPercent.Should().Be(25);
    }

    [Fact]
    public void SignalRSyncProgressDto_ProgressPercent_ReturnsZeroWhenNoItems()
    {
        var progress = new SignalRSyncProgressDto
        {
            TotalItems = 0,
            CompletedItems = 0
        };

        progress.ProgressPercent.Should().Be(0);
    }

    [Fact]
    public void SignalRSyncProgressDto_IsComplete_ReturnsTrueWhenAllProcessed()
    {
        var progress = new SignalRSyncProgressDto
        {
            TotalItems = 10,
            CompletedItems = 8,
            FailedItems = 2
        };

        progress.IsComplete.Should().BeTrue();
    }

    [Fact]
    public void SignalRBatchSyncResponseDto_DefaultValues_AreCorrect()
    {
        var response = new SignalRBatchSyncResponseDto();

        response.Success.Should().BeFalse();
        response.TotalItems.Should().Be(0);
        response.ProcessedItems.Should().Be(0);
        response.Items.Should().BeEmpty();
        response.HasMore.Should().BeFalse();
        response.NextCursor.Should().BeNull();
    }

    [Fact]
    public void SignalRHeartbeatDto_Timestamp_DefaultsToUtcNow()
    {
        var before = DateTime.UtcNow;
        var heartbeat = new SignalRHeartbeatDto();
        var after = DateTime.UtcNow;

        heartbeat.Timestamp.Should().BeOnOrAfter(before);
        heartbeat.Timestamp.Should().BeOnOrBefore(after);
    }

    #endregion
}
