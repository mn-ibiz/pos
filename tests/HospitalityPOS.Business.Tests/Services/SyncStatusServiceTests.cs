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
/// Unit tests for SyncStatusService.
/// </summary>
public class SyncStatusServiceTests : IDisposable
{
    private readonly Mock<ISyncQueueService> _syncQueueServiceMock;
    private readonly Mock<IRepository<SyncQueueItem>> _queueRepositoryMock;
    private readonly Mock<IRepository<SyncLog>> _logRepositoryMock;
    private readonly Mock<ILogger<SyncStatusService>> _loggerMock;
    private readonly Mock<ISyncHubService> _syncHubServiceMock;
    private readonly Mock<IConflictResolutionService> _conflictResolutionServiceMock;
    private readonly SyncStatusService _service;

    public SyncStatusServiceTests()
    {
        _syncQueueServiceMock = new Mock<ISyncQueueService>();
        _queueRepositoryMock = new Mock<IRepository<SyncQueueItem>>();
        _logRepositoryMock = new Mock<IRepository<SyncLog>>();
        _loggerMock = new Mock<ILogger<SyncStatusService>>();
        _syncHubServiceMock = new Mock<ISyncHubService>();
        _conflictResolutionServiceMock = new Mock<IConflictResolutionService>();

        _service = new SyncStatusService(
            _syncQueueServiceMock.Object,
            _queueRepositoryMock.Object,
            _logRepositoryMock.Object,
            _loggerMock.Object,
            _syncHubServiceMock.Object,
            _conflictResolutionServiceMock.Object);
    }

    public void Dispose()
    {
        _service.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullSyncQueueService_ThrowsArgumentNullException()
    {
        var action = () => new SyncStatusService(
            null!,
            _queueRepositoryMock.Object,
            _logRepositoryMock.Object,
            _loggerMock.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("syncQueueService");
    }

    [Fact]
    public void Constructor_WithNullQueueRepository_ThrowsArgumentNullException()
    {
        var action = () => new SyncStatusService(
            _syncQueueServiceMock.Object,
            null!,
            _logRepositoryMock.Object,
            _loggerMock.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("queueRepository");
    }

    [Fact]
    public void Constructor_WithNullLogRepository_ThrowsArgumentNullException()
    {
        var action = () => new SyncStatusService(
            _syncQueueServiceMock.Object,
            _queueRepositoryMock.Object,
            null!,
            _loggerMock.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logRepository");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var action = () => new SyncStatusService(
            _syncQueueServiceMock.Object,
            _queueRepositoryMock.Object,
            _logRepositoryMock.Object,
            null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithOptionalServicesNull_DoesNotThrow()
    {
        var service = new SyncStatusService(
            _syncQueueServiceMock.Object,
            _queueRepositoryMock.Object,
            _logRepositoryMock.Object,
            _loggerMock.Object);

        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        var service = new SyncStatusService(
            _syncQueueServiceMock.Object,
            _queueRepositoryMock.Object,
            _logRepositoryMock.Object,
            _loggerMock.Object);

        service.ConnectionState.Should().Be(SyncConnectionState.Offline);
        service.HealthStatus.Should().Be(SyncHealthStatus.Warning);
        service.IsOnline.Should().BeFalse();
        service.IsSyncing.Should().BeFalse();
        service.LastSyncTime.Should().BeNull();
    }

    #endregion

    #region Dashboard Tests

    [Fact]
    public async Task GetDashboardAsync_ReturnsCompleteDashboard()
    {
        SetupEmptyQueueRepository();
        _conflictResolutionServiceMock.Setup(c => c.GetConflictSummaryAsync(It.IsAny<int?>()))
            .ReturnsAsync(new ConflictSummaryDto());

        var dashboard = await _service.GetDashboardAsync();

        dashboard.Should().NotBeNull();
        dashboard.QueueSummary.Should().NotBeNull();
        dashboard.Metrics.Should().NotBeNull();
    }

    [Fact]
    public async Task GetDashboardAsync_WithStoreId_FiltersByStore()
    {
        var items = new List<SyncQueueItem>
        {
            CreateTestQueueItem(1, SyncQueueItemStatus.Pending, storeId: 1),
            CreateTestQueueItem(2, SyncQueueItemStatus.Pending, storeId: 2),
            CreateTestQueueItem(3, SyncQueueItemStatus.Pending, storeId: 1)
        };

        _queueRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<SyncQueueItem, bool>>>()))
            .ReturnsAsync((Expression<Func<SyncQueueItem, bool>> predicate) =>
                items.Where(predicate.Compile()));

        _conflictResolutionServiceMock.Setup(c => c.GetConflictSummaryAsync(1))
            .ReturnsAsync(new ConflictSummaryDto());

        var dashboard = await _service.GetDashboardAsync(1);

        dashboard.StoreId.Should().Be(1);
    }

    [Fact]
    public async Task GetStatusBarAsync_ReturnsCompactStatus()
    {
        SetupEmptyQueueRepository();

        var statusBar = await _service.GetStatusBarAsync();

        statusBar.Should().NotBeNull();
        statusBar.State.Should().Be(SyncConnectionState.Offline);
        statusBar.StatusText.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Queue Summary Tests

    [Fact]
    public async Task GetQueueSummaryAsync_WithMixedItems_ReturnsCorrectCounts()
    {
        var items = new List<SyncQueueItem>
        {
            CreateTestQueueItem(1, SyncQueueItemStatus.Pending, SyncQueuePriority.Critical),
            CreateTestQueueItem(2, SyncQueueItemStatus.Pending, SyncQueuePriority.High),
            CreateTestQueueItem(3, SyncQueueItemStatus.Pending, SyncQueuePriority.Normal),
            CreateTestQueueItem(4, SyncQueueItemStatus.Pending, SyncQueuePriority.Low),
            CreateTestQueueItem(5, SyncQueueItemStatus.Failed),
            CreateTestQueueItem(6, SyncQueueItemStatus.Completed),
            CreateTestQueueItem(7, SyncQueueItemStatus.Completed)
        };

        _queueRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<SyncQueueItem, bool>>>()))
            .ReturnsAsync(items);

        var summary = await _service.GetQueueSummaryAsync();

        summary.TotalPending.Should().Be(4);
        summary.CriticalPending.Should().Be(1);
        summary.HighPending.Should().Be(1);
        summary.NormalPending.Should().Be(1);
        summary.LowPending.Should().Be(1);
        summary.FailedItems.Should().Be(1);
    }

    [Fact]
    public async Task GetQueueSummaryAsync_WithNoItems_ReturnsZeroCounts()
    {
        SetupEmptyQueueRepository();

        var summary = await _service.GetQueueSummaryAsync();

        summary.TotalPending.Should().Be(0);
        summary.FailedItems.Should().Be(0);
        summary.HasCriticalItems.Should().BeFalse();
        summary.HasFailures.Should().BeFalse();
    }

    [Fact]
    public async Task GetPendingCountAsync_ReturnsCorrectCount()
    {
        var items = new List<SyncQueueItem>
        {
            CreateTestQueueItem(1, SyncQueueItemStatus.Pending),
            CreateTestQueueItem(2, SyncQueueItemStatus.InProgress),
            CreateTestQueueItem(3, SyncQueueItemStatus.Completed)
        };

        _queueRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<SyncQueueItem, bool>>>()))
            .ReturnsAsync((Expression<Func<SyncQueueItem, bool>> predicate) =>
                items.Where(predicate.Compile()));

        var count = await _service.GetPendingCountAsync();

        count.Should().Be(2);
    }

    [Fact]
    public async Task GetFailedCountAsync_ReturnsCorrectCount()
    {
        var items = new List<SyncQueueItem>
        {
            CreateTestQueueItem(1, SyncQueueItemStatus.Failed),
            CreateTestQueueItem(2, SyncQueueItemStatus.Failed),
            CreateTestQueueItem(3, SyncQueueItemStatus.Pending)
        };

        _queueRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<SyncQueueItem, bool>>>()))
            .ReturnsAsync((Expression<Func<SyncQueueItem, bool>> predicate) =>
                items.Where(predicate.Compile()));

        var count = await _service.GetFailedCountAsync();

        count.Should().Be(2);
    }

    #endregion

    #region Error Tests

    [Fact]
    public async Task GetRecentErrorsAsync_ReturnsFailedItems()
    {
        var items = new List<SyncQueueItem>
        {
            CreateTestQueueItem(1, SyncQueueItemStatus.Failed, lastError: "Error 1"),
            CreateTestQueueItem(2, SyncQueueItemStatus.Failed, lastError: "Error 2")
        };

        _queueRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<SyncQueueItem, bool>>>()))
            .ReturnsAsync(items);

        var errors = await _service.GetRecentErrorsAsync();

        errors.Should().HaveCount(2);
        errors.Should().OnlyContain(e => e.ErrorMessage != null);
    }

    [Fact]
    public async Task GetRecentErrorsAsync_RespectsLimit()
    {
        var items = Enumerable.Range(1, 50)
            .Select(i => CreateTestQueueItem(i, SyncQueueItemStatus.Failed))
            .ToList();

        _queueRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<SyncQueueItem, bool>>>()))
            .ReturnsAsync(items);

        var errors = await _service.GetRecentErrorsAsync(10);

        errors.Should().HaveCount(10);
    }

    [Fact]
    public async Task GetErrorDetailsAsync_WithValidId_ReturnsError()
    {
        var item = CreateTestQueueItem(1, SyncQueueItemStatus.Failed, lastError: "Test error");

        _queueRepositoryMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(item);

        var error = await _service.GetErrorDetailsAsync(1);

        error.Should().NotBeNull();
        error!.ErrorMessage.Should().Be("Test error");
    }

    [Fact]
    public async Task GetErrorDetailsAsync_WithInvalidId_ReturnsNull()
    {
        _queueRepositoryMock.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((SyncQueueItem?)null);

        var error = await _service.GetErrorDetailsAsync(999);

        error.Should().BeNull();
    }

    [Fact]
    public async Task ClearErrorsAsync_ClearsAllErrors()
    {
        var items = new List<SyncQueueItem>
        {
            CreateTestQueueItem(1, SyncQueueItemStatus.Failed),
            CreateTestQueueItem(2, SyncQueueItemStatus.Failed)
        };

        _queueRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<SyncQueueItem, bool>>>()))
            .ReturnsAsync(items);

        var count = await _service.ClearErrorsAsync();

        count.Should().Be(2);
        items.Should().OnlyContain(i => i.Status == SyncQueueItemStatus.Cancelled);
    }

    #endregion

    #region Sync Operations Tests

    [Fact]
    public async Task TriggerManualSyncAsync_ProcessesQueue()
    {
        var request = new ManualSyncRequestDto { StoreId = 1 };

        _syncQueueServiceMock.Setup(s => s.ProcessQueueAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncProcessingResult
            {
                TotalProcessed = 10,
                SuccessCount = 8,
                FailedCount = 2
            });

        var result = await _service.TriggerManualSyncAsync(request);

        result.TotalItems.Should().Be(10);
        result.SuccessfulItems.Should().Be(8);
        result.FailedItems.Should().Be(2);
    }

    [Fact]
    public async Task TriggerManualSyncAsync_UpdatesConnectionState()
    {
        var request = new ManualSyncRequestDto { StoreId = 1 };
        var stateChanges = new List<SyncConnectionState>();
        _service.ConnectionStateChanged += (s, state) => stateChanges.Add(state);

        _syncQueueServiceMock.Setup(s => s.ProcessQueueAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncProcessingResult());

        await _service.TriggerManualSyncAsync(request);

        stateChanges.Should().Contain(SyncConnectionState.Syncing);
        stateChanges.Should().Contain(SyncConnectionState.Online);
    }

    [Fact]
    public async Task TriggerManualSyncAsync_WhileAlreadySyncing_ReturnsFailed()
    {
        var request = new ManualSyncRequestDto { StoreId = 1 };

        _syncQueueServiceMock.Setup(s => s.ProcessQueueAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await Task.Delay(100);
                return new SyncProcessingResult();
            });

        // Start first sync
        var firstSync = _service.TriggerManualSyncAsync(request);

        // Try second sync immediately
        await Task.Delay(10);
        var secondResult = await _service.TriggerManualSyncAsync(request);

        secondResult.Success.Should().BeFalse();
        secondResult.Summary.Should().Contain("already in progress");

        await firstSync;
    }

    [Fact]
    public async Task RetryItemAsync_WithValidItem_ReturnsTrue()
    {
        var item = CreateTestQueueItem(1, SyncQueueItemStatus.Failed);

        _queueRepositoryMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(item);

        var result = await _service.RetryItemAsync(1);

        result.Should().BeTrue();
        item.Status.Should().Be(SyncQueueItemStatus.Pending);
        item.RetryCount.Should().Be(0);
    }

    [Fact]
    public async Task RetryItemAsync_WithNonExistentItem_ReturnsFalse()
    {
        _queueRepositoryMock.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((SyncQueueItem?)null);

        var result = await _service.RetryItemAsync(999);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CancelItemAsync_WithPendingItem_ReturnsTrue()
    {
        var item = CreateTestQueueItem(1, SyncQueueItemStatus.Pending);

        _queueRepositoryMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(item);

        var result = await _service.CancelItemAsync(1);

        result.Should().BeTrue();
        item.Status.Should().Be(SyncQueueItemStatus.Cancelled);
    }

    [Fact]
    public async Task CancelItemAsync_WithInProgressItem_ReturnsFalse()
    {
        var item = CreateTestQueueItem(1, SyncQueueItemStatus.InProgress);

        _queueRepositoryMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(item);

        var result = await _service.CancelItemAsync(1);

        result.Should().BeFalse();
        item.Status.Should().Be(SyncQueueItemStatus.InProgress);
    }

    #endregion

    #region Health Calculation Tests

    [Fact]
    public void CalculateHealthStatus_WithCriticalPending_ReturnsCritical()
    {
        var summary = new SyncQueueSummaryDto { CriticalPending = 1 };

        var health = _service.CalculateHealthStatus(summary, DateTime.UtcNow, true);

        health.Should().Be(SyncHealthStatus.Critical);
    }

    [Fact]
    public void CalculateHealthStatus_WithManyFailedItems_ReturnsCritical()
    {
        var summary = new SyncQueueSummaryDto { FailedItems = 15 };

        var health = _service.CalculateHealthStatus(summary, DateTime.UtcNow, true);

        health.Should().Be(SyncHealthStatus.Critical);
    }

    [Fact]
    public void CalculateHealthStatus_WithOldLastSync_ReturnsCritical()
    {
        var summary = new SyncQueueSummaryDto();
        var oldSync = DateTime.UtcNow.AddHours(-25);

        var health = _service.CalculateHealthStatus(summary, oldSync, true);

        health.Should().Be(SyncHealthStatus.Critical);
    }

    [Fact]
    public void CalculateHealthStatus_WithSomeFailedItems_ReturnsDegraded()
    {
        var summary = new SyncQueueSummaryDto { FailedItems = 3 };

        var health = _service.CalculateHealthStatus(summary, DateTime.UtcNow, true);

        health.Should().Be(SyncHealthStatus.Degraded);
    }

    [Fact]
    public void CalculateHealthStatus_WithManyPendingItems_ReturnsDegraded()
    {
        var summary = new SyncQueueSummaryDto { TotalPending = 150 };

        var health = _service.CalculateHealthStatus(summary, DateTime.UtcNow, true);

        health.Should().Be(SyncHealthStatus.Degraded);
    }

    [Fact]
    public void CalculateHealthStatus_OfflineWithPending_ReturnsDegraded()
    {
        var summary = new SyncQueueSummaryDto { TotalPending = 5 };

        var health = _service.CalculateHealthStatus(summary, DateTime.UtcNow, isOnline: false);

        health.Should().Be(SyncHealthStatus.Degraded);
    }

    [Fact]
    public void CalculateHealthStatus_WithSomePending_ReturnsWarning()
    {
        var summary = new SyncQueueSummaryDto { TotalPending = 5 };

        var health = _service.CalculateHealthStatus(summary, DateTime.UtcNow, true);

        health.Should().Be(SyncHealthStatus.Warning);
    }

    [Fact]
    public void CalculateHealthStatus_AllClear_ReturnsHealthy()
    {
        var summary = new SyncQueueSummaryDto();

        var health = _service.CalculateHealthStatus(summary, DateTime.UtcNow, true);

        health.Should().Be(SyncHealthStatus.Healthy);
    }

    [Fact]
    public void GetHealthMessage_ReturnsMeaningfulMessage()
    {
        var summary = new SyncQueueSummaryDto { CriticalPending = 5 };

        var message = _service.GetHealthMessage(SyncHealthStatus.Critical, summary);

        message.Should().Contain("Critical");
        message.Should().Contain("5");
    }

    #endregion

    #region Connection Management Tests

    [Fact]
    public async Task CheckConnectionAsync_WithHubService_ReturnsHubStatus()
    {
        _syncHubServiceMock.Setup(s => s.IsConnected).Returns(true);

        var result = await _service.CheckConnectionAsync();

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ReconnectAsync_SuccessfulConnection_UpdatesState()
    {
        _syncHubServiceMock.Setup(s => s.ConnectAsync())
            .ReturnsAsync(true);

        var result = await _service.ReconnectAsync();

        result.Should().BeTrue();
        _service.ConnectionState.Should().Be(SyncConnectionState.Online);
    }

    [Fact]
    public async Task ReconnectAsync_FailedConnection_SetsErrorState()
    {
        _syncHubServiceMock.Setup(s => s.ConnectAsync())
            .ReturnsAsync(false);

        var result = await _service.ReconnectAsync();

        result.Should().BeFalse();
        _service.ConnectionState.Should().Be(SyncConnectionState.Error);
    }

    [Fact]
    public void SetConnectionState_RaisesEvent()
    {
        SyncConnectionState? receivedState = null;
        _service.ConnectionStateChanged += (s, state) => receivedState = state;

        _service.SetConnectionState(SyncConnectionState.Online);

        receivedState.Should().Be(SyncConnectionState.Online);
        _service.ConnectionState.Should().Be(SyncConnectionState.Online);
    }

    [Fact]
    public void SetConnectionState_SameState_DoesNotRaiseEvent()
    {
        var eventRaised = false;
        _service.SetConnectionState(SyncConnectionState.Offline);
        _service.ConnectionStateChanged += (s, state) => eventRaised = true;

        _service.SetConnectionState(SyncConnectionState.Offline);

        eventRaised.Should().BeFalse();
    }

    #endregion

    #region DTO Tests

    [Fact]
    public void SyncStatusDashboardDto_TimeSinceLastSync_CalculatesCorrectly()
    {
        var dashboard = new SyncStatusDashboardDto
        {
            LastSyncTime = DateTime.UtcNow.AddMinutes(-30)
        };

        dashboard.TimeSinceLastSync.Should().NotBeNull();
        dashboard.TimeSinceLastSync!.Value.TotalMinutes.Should().BeApproximately(30, 1);
    }

    [Fact]
    public void SyncStatusDashboardDto_IsOnline_ReflectsConnectionState()
    {
        var dashboard = new SyncStatusDashboardDto();

        dashboard.ConnectionState = SyncConnectionState.Online;
        dashboard.IsOnline.Should().BeTrue();

        dashboard.ConnectionState = SyncConnectionState.Syncing;
        dashboard.IsOnline.Should().BeTrue();

        dashboard.ConnectionState = SyncConnectionState.Offline;
        dashboard.IsOnline.Should().BeFalse();
    }

    [Fact]
    public void SyncQueueSummaryDto_HasCriticalItems_ReturnsCorrectly()
    {
        var summary = new SyncQueueSummaryDto { CriticalPending = 0 };
        summary.HasCriticalItems.Should().BeFalse();

        summary.CriticalPending = 1;
        summary.HasCriticalItems.Should().BeTrue();
    }

    [Fact]
    public void SyncErrorDto_CanRetry_ReturnsCorrectly()
    {
        var error = new SyncErrorDto { AttemptCount = 3, MaxAttempts = 5 };
        error.CanRetry.Should().BeTrue();

        error.AttemptCount = 5;
        error.CanRetry.Should().BeFalse();
    }

    [Fact]
    public void SyncStatusBarDto_GetStatusText_ReturnsCorrectText()
    {
        var statusBar = new SyncStatusBarDto();

        statusBar.IsSyncing = true;
        statusBar.GetStatusText().Should().Be("Syncing...");

        statusBar.IsSyncing = false;
        statusBar.State = SyncConnectionState.Offline;
        statusBar.GetStatusText().Should().Be("Offline");

        statusBar.State = SyncConnectionState.Online;
        statusBar.ErrorCount = 5;
        statusBar.GetStatusText().Should().Contain("5 errors");

        statusBar.ErrorCount = 0;
        statusBar.PendingCount = 10;
        statusBar.GetStatusText().Should().Contain("10 pending");

        statusBar.PendingCount = 0;
        statusBar.GetStatusText().Should().Be("Up to date");
    }

    [Fact]
    public void SyncStatusBarDto_GetHealthColor_ReturnsCorrectColors()
    {
        var statusBar = new SyncStatusBarDto();

        statusBar.Health = SyncHealthStatus.Healthy;
        statusBar.GetHealthColor().Should().Contain("22C55E"); // Green

        statusBar.Health = SyncHealthStatus.Warning;
        statusBar.GetHealthColor().Should().Contain("EAB308"); // Yellow

        statusBar.Health = SyncHealthStatus.Degraded;
        statusBar.GetHealthColor().Should().Contain("F97316"); // Orange

        statusBar.Health = SyncHealthStatus.Critical;
        statusBar.GetHealthColor().Should().Contain("EF4444"); // Red
    }

    [Fact]
    public void ManualSyncResultDto_Succeeded_SetsCorrectValues()
    {
        var started = DateTime.UtcNow.AddSeconds(-5);

        var result = ManualSyncResultDto.Succeeded(started, 100, 95, 5);

        result.Success.Should().BeFalse(); // Has failures
        result.TotalItems.Should().Be(100);
        result.SuccessfulItems.Should().Be(95);
        result.FailedItems.Should().Be(5);
        result.Duration.TotalSeconds.Should().BeGreaterOrEqualTo(5);
    }

    [Fact]
    public void ManualSyncResultDto_Failed_SetsCorrectValues()
    {
        var started = DateTime.UtcNow;

        var result = ManualSyncResultDto.Failed(started, "Test error");

        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Test error");
        result.Summary.Should().Contain("failed");
    }

    #endregion

    #region Metrics Tests

    [Fact]
    public async Task GetMetricsAsync_CalculatesSuccessRate()
    {
        var items = new List<SyncQueueItem>
        {
            CreateTestQueueItem(1, SyncQueueItemStatus.Completed),
            CreateTestQueueItem(2, SyncQueueItemStatus.Completed),
            CreateTestQueueItem(3, SyncQueueItemStatus.Completed),
            CreateTestQueueItem(4, SyncQueueItemStatus.Failed)
        };

        _queueRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<SyncQueueItem, bool>>>()))
            .ReturnsAsync(items);

        var metrics = await _service.GetMetricsAsync();

        metrics.SuccessRatePercent.Should().Be(75);
        metrics.FailureRatePercent.Should().Be(25);
    }

    [Fact]
    public async Task GetRecentActivityAsync_ReturnsStoredActivity()
    {
        SetupEmptyQueueRepository();

        // Trigger some activity through manual sync
        _syncQueueServiceMock.Setup(s => s.ProcessQueueAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncProcessingResult { TotalProcessed = 5, SuccessCount = 5 });

        await _service.TriggerManualSyncAsync(new ManualSyncRequestDto { StoreId = 1 });

        var activity = await _service.GetRecentActivityAsync();

        activity.Should().NotBeEmpty();
    }

    #endregion

    #region Helper Methods

    private void SetupEmptyQueueRepository()
    {
        _queueRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<SyncQueueItem, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<SyncQueueItem>());
    }

    private static SyncQueueItem CreateTestQueueItem(
        int id,
        SyncQueueItemStatus status,
        SyncQueuePriority priority = SyncQueuePriority.Normal,
        int? storeId = null,
        string? lastError = null)
    {
        return new SyncQueueItem
        {
            Id = id,
            EntityType = "TestEntity",
            EntityId = id,
            OperationType = SyncQueueOperationType.Create,
            Priority = priority,
            Status = status,
            StoreId = storeId,
            LastError = lastError,
            RetryCount = status == SyncQueueItemStatus.Failed ? 3 : 0,
            MaxRetries = 5,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddMinutes(-id),
            UpdatedAt = DateTime.UtcNow
        };
    }

    #endregion
}
