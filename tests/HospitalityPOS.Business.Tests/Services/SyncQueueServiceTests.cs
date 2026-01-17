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
/// Unit tests for SyncQueueService.
/// </summary>
public class SyncQueueServiceTests
{
    private readonly Mock<IRepository<SyncQueueItem>> _queueRepoMock;
    private readonly Mock<ILogger<SyncQueueService>> _loggerMock;
    private readonly SyncRetryPolicy _retryPolicy;
    private readonly SyncQueueService _service;

    public SyncQueueServiceTests()
    {
        _queueRepoMock = new Mock<IRepository<SyncQueueItem>>();
        _loggerMock = new Mock<ILogger<SyncQueueService>>();
        _retryPolicy = new SyncRetryPolicy
        {
            MaxRetries = 5,
            BaseDelaySeconds = 30,
            MaxDelaySeconds = 3600
        };
        _service = new SyncQueueService(_queueRepoMock.Object, _loggerMock.Object, _retryPolicy);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        var action = () => new SyncQueueService(null!, _loggerMock.Object, _retryPolicy);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("queueRepository");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var action = () => new SyncQueueService(_queueRepoMock.Object, null!, _retryPolicy);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullRetryPolicy_UsesDefaultPolicy()
    {
        // Act - should not throw
        var service = new SyncQueueService(_queueRepoMock.Object, _loggerMock.Object, null);

        // Verify default policy values
        var delay = service.CalculateRetryDelaySeconds(1);
        delay.Should().Be(30); // Default base delay
    }

    #endregion

    #region Enqueue Tests

    [Fact]
    public async Task EnqueueAsync_WithEntityTypeAndId_CreatesQueueItem()
    {
        // Arrange
        _queueRepoMock.Setup(r => r.AddAsync(It.IsAny<SyncQueueItem>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.EnqueueAsync("Product", 1, SyncQueueOperationType.Create, "{}", SyncQueuePriority.High);

        // Assert
        result.Should().NotBeNull();
        result.EntityType.Should().Be("Product");
        result.EntityId.Should().Be(1);
        result.Priority.Should().Be(SyncPriority.High);
        result.Status.Should().Be(SyncQueueStatus.Pending);

        _queueRepoMock.Verify(r => r.AddAsync(It.Is<SyncQueueItem>(i =>
            i.EntityType == "Product" &&
            i.EntityId == 1 &&
            i.Priority == SyncQueuePriority.High)), Times.Once);
    }

    [Fact]
    public async Task EnqueueAsync_WithDefaultPriority_UsesNormalPriority()
    {
        // Arrange
        _queueRepoMock.Setup(r => r.AddAsync(It.IsAny<SyncQueueItem>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.EnqueueAsync("Order", 100, SyncQueueOperationType.Update);

        // Assert
        result.Priority.Should().Be(SyncPriority.Normal);
    }

    [Fact]
    public async Task EnqueueAsync_WithEntity_SerializesPayload()
    {
        // Arrange
        var entity = new TestEntity { Id = 5, Name = "Test Product" };
        _queueRepoMock.Setup(r => r.AddAsync(It.IsAny<SyncQueueItem>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.EnqueueAsync(entity, SyncQueueOperationType.Create, SyncQueuePriority.Normal);

        // Assert
        result.EntityId.Should().Be(5);
        result.Payload.Should().Contain("Test Product");
    }

    #endregion

    #region Get Pending Items Tests

    [Fact]
    public async Task GetPendingItemsAsync_OrdersByPriorityThenCreatedAt()
    {
        // Arrange
        var items = new List<SyncQueueItem>
        {
            new SyncQueueItem { Id = 1, EntityType = "Product", EntityId = 1, Priority = SyncQueuePriority.Low, Status = SyncQueueItemStatus.Pending, CreatedAt = DateTime.UtcNow.AddMinutes(-5) },
            new SyncQueueItem { Id = 2, EntityType = "Receipt", EntityId = 1, Priority = SyncQueuePriority.High, Status = SyncQueueItemStatus.Pending, CreatedAt = DateTime.UtcNow.AddMinutes(-10) },
            new SyncQueueItem { Id = 3, EntityType = "EtimsInvoice", EntityId = 1, Priority = SyncQueuePriority.Critical, Status = SyncQueueItemStatus.Pending, CreatedAt = DateTime.UtcNow.AddMinutes(-1) },
            new SyncQueueItem { Id = 4, EntityType = "Order", EntityId = 1, Priority = SyncQueuePriority.Normal, Status = SyncQueueItemStatus.Pending, CreatedAt = DateTime.UtcNow }
        };

        _queueRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(items);

        // Act
        var result = await _service.GetPendingItemsAsync(10);

        // Assert
        result.Should().HaveCount(4);
        // Priority order: Low=1, Normal=2, High=3, Critical=4 (ascending sorts Critical first)
        result[0].EntityType.Should().Be("Product"); // Low priority first when sorting ascending
    }

    [Fact]
    public async Task GetPendingItemsAsync_ExcludesNonPendingItems()
    {
        // Arrange
        var items = new List<SyncQueueItem>
        {
            new SyncQueueItem { Id = 1, EntityType = "Product", Status = SyncQueueItemStatus.Pending },
            new SyncQueueItem { Id = 2, EntityType = "Product", Status = SyncQueueItemStatus.Completed },
            new SyncQueueItem { Id = 3, EntityType = "Product", Status = SyncQueueItemStatus.Failed },
            new SyncQueueItem { Id = 4, EntityType = "Product", Status = SyncQueueItemStatus.InProgress },
            new SyncQueueItem { Id = 5, EntityType = "Product", Status = SyncQueueItemStatus.Pending }
        };

        _queueRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(items);

        // Act
        var result = await _service.GetPendingItemsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.All(i => i.Status == SyncQueueStatus.Pending).Should().BeTrue();
    }

    [Fact]
    public async Task GetPendingItemsAsync_RespectsLimit()
    {
        // Arrange
        var items = Enumerable.Range(1, 100)
            .Select(i => new SyncQueueItem { Id = i, EntityType = "Product", Status = SyncQueueItemStatus.Pending })
            .ToList();

        _queueRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(items);

        // Act
        var result = await _service.GetPendingItemsAsync(10);

        // Assert
        result.Should().HaveCount(10);
    }

    [Fact]
    public async Task GetPendingItemsByTypeAsync_FiltersCorrectly()
    {
        // Arrange
        var items = new List<SyncQueueItem>
        {
            new SyncQueueItem { Id = 1, EntityType = "Product", Status = SyncQueueItemStatus.Pending },
            new SyncQueueItem { Id = 2, EntityType = "Order", Status = SyncQueueItemStatus.Pending },
            new SyncQueueItem { Id = 3, EntityType = "Product", Status = SyncQueueItemStatus.Pending },
            new SyncQueueItem { Id = 4, EntityType = "Receipt", Status = SyncQueueItemStatus.Pending }
        };

        _queueRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(items);

        // Act
        var result = await _service.GetPendingItemsByTypeAsync("Product");

        // Assert
        result.Should().HaveCount(2);
        result.All(i => i.EntityType == "Product").Should().BeTrue();
    }

    #endregion

    #region Get Failed Items Tests

    [Fact]
    public async Task GetFailedItemsAsync_ReturnsOnlyFailedItems()
    {
        // Arrange
        var items = new List<SyncQueueItem>
        {
            new SyncQueueItem { Id = 1, EntityType = "Product", Status = SyncQueueItemStatus.Failed, LastAttemptAt = DateTime.UtcNow },
            new SyncQueueItem { Id = 2, EntityType = "Product", Status = SyncQueueItemStatus.Pending },
            new SyncQueueItem { Id = 3, EntityType = "Product", Status = SyncQueueItemStatus.Failed, LastAttemptAt = DateTime.UtcNow.AddMinutes(-5) }
        };

        _queueRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(items);

        // Act
        var result = await _service.GetFailedItemsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.All(i => i.Status == SyncQueueStatus.Failed).Should().BeTrue();
    }

    #endregion

    #region Processing Tests

    [Fact]
    public async Task ProcessQueueAsync_ProcessesPendingItems()
    {
        // Arrange
        var items = new List<SyncQueueItem>
        {
            new SyncQueueItem { Id = 1, EntityType = "Product", EntityId = 1, Status = SyncQueueItemStatus.Pending },
            new SyncQueueItem { Id = 2, EntityType = "Product", EntityId = 2, Status = SyncQueueItemStatus.Pending }
        };

        _queueRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(items);
        _queueRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => items.FirstOrDefault(i => i.Id == id));
        _queueRepoMock.Setup(r => r.UpdateAsync(It.IsAny<SyncQueueItem>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ProcessQueueAsync(10);

        // Assert
        result.TotalProcessed.Should().Be(2);
        result.Succeeded.Should().Be(2);
        result.Failed.Should().Be(0);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessQueueAsync_RespectsCancellation()
    {
        // Arrange
        var items = Enumerable.Range(1, 100)
            .Select(i => new SyncQueueItem { Id = i, EntityType = "Product", EntityId = i, Status = SyncQueueItemStatus.Pending })
            .ToList();

        _queueRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(items);
        _queueRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => items.FirstOrDefault(i => i.Id == id));
        _queueRepoMock.Setup(r => r.UpdateAsync(It.IsAny<SyncQueueItem>()))
            .Returns(Task.CompletedTask);

        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act
        var result = await _service.ProcessQueueAsync(100, cts.Token);

        // Assert
        result.TotalProcessed.Should().Be(0);
    }

    [Fact]
    public async Task MarkAsInProgressAsync_UpdatesItemStatus()
    {
        // Arrange
        var item = new SyncQueueItem { Id = 1, Status = SyncQueueItemStatus.Pending };
        _queueRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);
        _queueRepoMock.Setup(r => r.UpdateAsync(It.IsAny<SyncQueueItem>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.MarkAsInProgressAsync(1);

        // Assert
        result.Should().BeTrue();
        _queueRepoMock.Verify(r => r.UpdateAsync(It.Is<SyncQueueItem>(i =>
            i.Status == SyncQueueItemStatus.InProgress &&
            i.LastAttemptAt.HasValue)), Times.Once);
    }

    [Fact]
    public async Task MarkAsCompletedAsync_UpdatesItemStatus()
    {
        // Arrange
        var item = new SyncQueueItem { Id = 1, Status = SyncQueueItemStatus.InProgress };
        _queueRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);
        _queueRepoMock.Setup(r => r.UpdateAsync(It.IsAny<SyncQueueItem>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.MarkAsCompletedAsync(1);

        // Assert
        result.Should().BeTrue();
        _queueRepoMock.Verify(r => r.UpdateAsync(It.Is<SyncQueueItem>(i =>
            i.Status == SyncQueueItemStatus.Completed)), Times.Once);
    }

    [Fact]
    public async Task MarkAsFailedAsync_UpdatesStatusAndSetsRetryTime()
    {
        // Arrange
        var item = new SyncQueueItem { Id = 1, Status = SyncQueueItemStatus.InProgress, RetryCount = 0, MaxRetries = 5 };
        _queueRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);
        _queueRepoMock.Setup(r => r.UpdateAsync(It.IsAny<SyncQueueItem>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.MarkAsFailedAsync(1, "Test error");

        // Assert
        result.Should().BeTrue();
        _queueRepoMock.Verify(r => r.UpdateAsync(It.Is<SyncQueueItem>(i =>
            i.Status == SyncQueueItemStatus.Failed &&
            i.LastError == "Test error" &&
            i.RetryCount == 1 &&
            i.NextRetryAt.HasValue)), Times.Once);
    }

    [Fact]
    public async Task MarkAsFailedAsync_AfterMaxRetries_DoesNotSetNextRetryTime()
    {
        // Arrange
        var item = new SyncQueueItem { Id = 1, Status = SyncQueueItemStatus.InProgress, RetryCount = 4, MaxRetries = 5 };
        _queueRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);
        _queueRepoMock.Setup(r => r.UpdateAsync(It.IsAny<SyncQueueItem>())).Returns(Task.CompletedTask);

        // Act
        await _service.MarkAsFailedAsync(1, "Final error");

        // Assert - RetryCount will be 5 (>= MaxRetries), so no NextRetryAt
        _queueRepoMock.Verify(r => r.UpdateAsync(It.Is<SyncQueueItem>(i =>
            i.RetryCount == 5 &&
            !i.NextRetryAt.HasValue)), Times.Once);
    }

    [Fact]
    public async Task CancelItemAsync_CancelsPendingItem()
    {
        // Arrange
        var item = new SyncQueueItem { Id = 1, Status = SyncQueueItemStatus.Pending };
        _queueRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);
        _queueRepoMock.Setup(r => r.UpdateAsync(It.IsAny<SyncQueueItem>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CancelItemAsync(1);

        // Assert
        result.Should().BeTrue();
        _queueRepoMock.Verify(r => r.UpdateAsync(It.Is<SyncQueueItem>(i =>
            i.Status == SyncQueueItemStatus.Cancelled)), Times.Once);
    }

    [Fact]
    public async Task CancelItemAsync_CannotCancelCompletedItem()
    {
        // Arrange
        var item = new SyncQueueItem { Id = 1, Status = SyncQueueItemStatus.Completed };
        _queueRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);

        // Act
        var result = await _service.CancelItemAsync(1);

        // Assert
        result.Should().BeFalse();
        _queueRepoMock.Verify(r => r.UpdateAsync(It.IsAny<SyncQueueItem>()), Times.Never);
    }

    #endregion

    #region Retry Tests

    [Fact]
    public async Task RetryItemAsync_ResetsFailedItemToPending()
    {
        // Arrange
        var item = new SyncQueueItem { Id = 1, Status = SyncQueueItemStatus.Failed, RetryCount = 2, MaxRetries = 5 };
        _queueRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);
        _queueRepoMock.Setup(r => r.UpdateAsync(It.IsAny<SyncQueueItem>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.RetryItemAsync(1);

        // Assert
        result.Should().BeTrue();
        _queueRepoMock.Verify(r => r.UpdateAsync(It.Is<SyncQueueItem>(i =>
            i.Status == SyncQueueItemStatus.Pending &&
            !i.NextRetryAt.HasValue)), Times.Once);
    }

    [Fact]
    public async Task RetryItemAsync_CannotRetryWhenMaxRetriesExceeded()
    {
        // Arrange
        var item = new SyncQueueItem { Id = 1, Status = SyncQueueItemStatus.Failed, RetryCount = 5, MaxRetries = 5 };
        _queueRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);

        // Act
        var result = await _service.RetryItemAsync(1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetItemsDueForRetryAsync_ReturnsItemsWithNextRetryInPast()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var items = new List<SyncQueueItem>
        {
            new SyncQueueItem { Id = 1, Status = SyncQueueItemStatus.Failed, RetryCount = 1, MaxRetries = 5, NextRetryAt = now.AddMinutes(-5) },
            new SyncQueueItem { Id = 2, Status = SyncQueueItemStatus.Failed, RetryCount = 2, MaxRetries = 5, NextRetryAt = now.AddMinutes(10) }, // Future
            new SyncQueueItem { Id = 3, Status = SyncQueueItemStatus.Failed, RetryCount = 1, MaxRetries = 5, NextRetryAt = now.AddMinutes(-10) },
            new SyncQueueItem { Id = 4, Status = SyncQueueItemStatus.Pending } // Not failed
        };

        _queueRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(items);

        // Act
        var result = await _service.GetItemsDueForRetryAsync();

        // Assert
        result.Should().HaveCount(2);
        result.All(i => i.Id == 1 || i.Id == 3).Should().BeTrue();
    }

    [Fact]
    public async Task RetryFailedItemsAsync_SchedulesRetryForDueItems()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var items = new List<SyncQueueItem>
        {
            new SyncQueueItem { Id = 1, Status = SyncQueueItemStatus.Failed, RetryCount = 1, MaxRetries = 5, NextRetryAt = now.AddMinutes(-5) },
            new SyncQueueItem { Id = 2, Status = SyncQueueItemStatus.Failed, RetryCount = 1, MaxRetries = 5, NextRetryAt = now.AddMinutes(-10) }
        };

        _queueRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(items);
        _queueRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => items.FirstOrDefault(i => i.Id == id));
        _queueRepoMock.Setup(r => r.UpdateAsync(It.IsAny<SyncQueueItem>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.RetryFailedItemsAsync();

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public void CalculateRetryDelaySeconds_UsesExponentialBackoff()
    {
        // Assert exponential backoff: 30, 60, 120, 240, 480...
        _service.CalculateRetryDelaySeconds(1).Should().Be(30);
        _service.CalculateRetryDelaySeconds(2).Should().Be(60);
        _service.CalculateRetryDelaySeconds(3).Should().Be(120);
        _service.CalculateRetryDelaySeconds(4).Should().Be(240);
        _service.CalculateRetryDelaySeconds(5).Should().Be(480);
    }

    [Fact]
    public void CalculateRetryDelaySeconds_RespectsMaxDelay()
    {
        // With MaxDelaySeconds = 3600 (1 hour), high retry counts should cap at that
        var delay = _service.CalculateRetryDelaySeconds(10); // Would be 30 * 2^9 = 15360 without cap
        delay.Should().Be(3600);
    }

    #endregion

    #region Queue Status Tests

    [Fact]
    public async Task GetQueueSummaryAsync_ReturnsCorrectCounts()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        var items = new List<SyncQueueItem>
        {
            new SyncQueueItem { Id = 1, Status = SyncQueueItemStatus.Pending },
            new SyncQueueItem { Id = 2, Status = SyncQueueItemStatus.Pending },
            new SyncQueueItem { Id = 3, Status = SyncQueueItemStatus.InProgress },
            new SyncQueueItem { Id = 4, Status = SyncQueueItemStatus.Failed },
            new SyncQueueItem { Id = 5, Status = SyncQueueItemStatus.Completed, UpdatedAt = today.AddHours(1) },
            new SyncQueueItem { Id = 6, Status = SyncQueueItemStatus.Completed, UpdatedAt = today.AddDays(-1) },
            new SyncQueueItem { Id = 7, Status = SyncQueueItemStatus.Conflict }
        };

        _queueRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(items);

        // Act
        var result = await _service.GetQueueSummaryAsync();

        // Assert
        result.PendingCount.Should().Be(2);
        result.InProgressCount.Should().Be(1);
        result.FailedCount.Should().Be(1);
        result.CompletedToday.Should().Be(1);
        result.ConflictCount.Should().Be(1);
    }

    [Fact]
    public async Task GetQueueCountByPriorityAsync_GroupsCorrectly()
    {
        // Arrange
        var items = new List<SyncQueueItem>
        {
            new SyncQueueItem { Id = 1, Priority = SyncQueuePriority.Critical, Status = SyncQueueItemStatus.Pending },
            new SyncQueueItem { Id = 2, Priority = SyncQueuePriority.Critical, Status = SyncQueueItemStatus.Pending },
            new SyncQueueItem { Id = 3, Priority = SyncQueuePriority.High, Status = SyncQueueItemStatus.Pending },
            new SyncQueueItem { Id = 4, Priority = SyncQueuePriority.Normal, Status = SyncQueueItemStatus.Pending },
            new SyncQueueItem { Id = 5, Priority = SyncQueuePriority.Normal, Status = SyncQueueItemStatus.Completed } // Not pending
        };

        _queueRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(items);

        // Act
        var result = await _service.GetQueueCountByPriorityAsync();

        // Assert
        result.Should().HaveCount(3);
        result[SyncQueuePriority.Critical].Should().Be(2);
        result[SyncQueuePriority.High].Should().Be(1);
        result[SyncQueuePriority.Normal].Should().Be(1);
    }

    [Fact]
    public async Task GetQueueCountByEntityTypeAsync_GroupsCorrectly()
    {
        // Arrange
        var items = new List<SyncQueueItem>
        {
            new SyncQueueItem { Id = 1, EntityType = "Product", Status = SyncQueueItemStatus.Pending },
            new SyncQueueItem { Id = 2, EntityType = "Product", Status = SyncQueueItemStatus.Pending },
            new SyncQueueItem { Id = 3, EntityType = "Order", Status = SyncQueueItemStatus.Pending },
            new SyncQueueItem { Id = 4, EntityType = "Receipt", Status = SyncQueueItemStatus.Pending }
        };

        _queueRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(items);

        // Act
        var result = await _service.GetQueueCountByEntityTypeAsync();

        // Assert
        result.Should().HaveCount(3);
        result["Product"].Should().Be(2);
        result["Order"].Should().Be(1);
        result["Receipt"].Should().Be(1);
    }

    [Fact]
    public async Task IsQueueEmptyAsync_ReturnsTrueWhenEmpty()
    {
        // Arrange
        _queueRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<SyncQueueItem>());

        // Act
        var result = await _service.IsQueueEmptyAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsQueueEmptyAsync_ReturnsFalseWhenNotEmpty()
    {
        // Arrange
        var items = new List<SyncQueueItem>
        {
            new SyncQueueItem { Id = 1, Status = SyncQueueItemStatus.Pending }
        };

        _queueRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(items);

        // Act
        var result = await _service.IsQueueEmptyAsync();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Cleanup Tests

    [Fact]
    public async Task CleanupCompletedItemsAsync_RemovesOldCompletedItems()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var items = new List<SyncQueueItem>
        {
            new SyncQueueItem { Id = 1, Status = SyncQueueItemStatus.Completed, UpdatedAt = now.AddDays(-10) },
            new SyncQueueItem { Id = 2, Status = SyncQueueItemStatus.Completed, UpdatedAt = now.AddDays(-5) },
            new SyncQueueItem { Id = 3, Status = SyncQueueItemStatus.Completed, UpdatedAt = now.AddDays(-1) },
            new SyncQueueItem { Id = 4, Status = SyncQueueItemStatus.Pending }
        };

        _queueRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(items);
        _queueRepoMock.Setup(r => r.UpdateAsync(It.IsAny<SyncQueueItem>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CleanupCompletedItemsAsync(7);

        // Assert
        result.Should().Be(1); // Only item 1 is older than 7 days
        _queueRepoMock.Verify(r => r.UpdateAsync(It.Is<SyncQueueItem>(i =>
            i.Id == 1 && !i.IsActive)), Times.Once);
    }

    [Fact]
    public async Task ClearQueueAsync_DeactivatesAllItems()
    {
        // Arrange
        var items = new List<SyncQueueItem>
        {
            new SyncQueueItem { Id = 1, Status = SyncQueueItemStatus.Pending },
            new SyncQueueItem { Id = 2, Status = SyncQueueItemStatus.Failed },
            new SyncQueueItem { Id = 3, Status = SyncQueueItemStatus.InProgress }
        };

        _queueRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(items);
        _queueRepoMock.Setup(r => r.UpdateAsync(It.IsAny<SyncQueueItem>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.ClearQueueAsync();

        // Assert
        result.Should().Be(3);
        _queueRepoMock.Verify(r => r.UpdateAsync(It.Is<SyncQueueItem>(i => !i.IsActive)), Times.Exactly(3));
    }

    [Fact]
    public async Task ResetStuckItemsAsync_ResetsItemsStuckInProgress()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var items = new List<SyncQueueItem>
        {
            new SyncQueueItem { Id = 1, Status = SyncQueueItemStatus.InProgress, LastAttemptAt = now.AddMinutes(-60) }, // Stuck
            new SyncQueueItem { Id = 2, Status = SyncQueueItemStatus.InProgress, LastAttemptAt = now.AddMinutes(-10) }, // Not stuck
            new SyncQueueItem { Id = 3, Status = SyncQueueItemStatus.Pending }
        };

        _queueRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(items);
        _queueRepoMock.Setup(r => r.UpdateAsync(It.IsAny<SyncQueueItem>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.ResetStuckItemsAsync(30);

        // Assert
        result.Should().Be(1);
        _queueRepoMock.Verify(r => r.UpdateAsync(It.Is<SyncQueueItem>(i =>
            i.Id == 1 && i.Status == SyncQueueItemStatus.Pending)), Times.Once);
    }

    #endregion

    #region Bulk Operations Tests

    [Fact]
    public async Task EnqueueBatchAsync_EnqueuesMultipleItems()
    {
        // Arrange
        var entities = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Product 1" },
            new TestEntity { Id = 2, Name = "Product 2" },
            new TestEntity { Id = 3, Name = "Product 3" }
        };

        _queueRepoMock.Setup(r => r.AddAsync(It.IsAny<SyncQueueItem>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.EnqueueBatchAsync(entities, SyncQueueOperationType.Create, SyncQueuePriority.Normal);

        // Assert
        result.Should().HaveCount(3);
        _queueRepoMock.Verify(r => r.AddAsync(It.IsAny<SyncQueueItem>()), Times.Exactly(3));
    }

    [Fact]
    public async Task CancelBatchAsync_CancelsMultipleItems()
    {
        // Arrange
        var items = new List<SyncQueueItem>
        {
            new SyncQueueItem { Id = 1, Status = SyncQueueItemStatus.Pending },
            new SyncQueueItem { Id = 2, Status = SyncQueueItemStatus.Pending },
            new SyncQueueItem { Id = 3, Status = SyncQueueItemStatus.Completed } // Can't cancel
        };

        _queueRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => items.FirstOrDefault(i => i.Id == id));
        _queueRepoMock.Setup(r => r.UpdateAsync(It.IsAny<SyncQueueItem>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CancelBatchAsync(new[] { 1, 2, 3 });

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task RetryBatchAsync_RetriesMultipleItems()
    {
        // Arrange
        var items = new List<SyncQueueItem>
        {
            new SyncQueueItem { Id = 1, Status = SyncQueueItemStatus.Failed, RetryCount = 1, MaxRetries = 5 },
            new SyncQueueItem { Id = 2, Status = SyncQueueItemStatus.Failed, RetryCount = 2, MaxRetries = 5 }
        };

        _queueRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => items.FirstOrDefault(i => i.Id == id));
        _queueRepoMock.Setup(r => r.UpdateAsync(It.IsAny<SyncQueueItem>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.RetryBatchAsync(new[] { 1, 2 });

        // Assert
        result.Should().Be(2);
    }

    #endregion

    #region SyncRetryPolicy Tests

    [Fact]
    public void SyncRetryPolicy_GetDelaySeconds_ExponentialBackoff()
    {
        var policy = new SyncRetryPolicy
        {
            BaseDelaySeconds = 30,
            MaxDelaySeconds = 3600
        };

        policy.GetDelaySeconds(1).Should().Be(30);
        policy.GetDelaySeconds(2).Should().Be(60);
        policy.GetDelaySeconds(3).Should().Be(120);
        policy.GetDelaySeconds(4).Should().Be(240);
        policy.GetDelaySeconds(5).Should().Be(480);
    }

    [Fact]
    public void SyncRetryPolicy_GetDelaySeconds_CapsAtMaxDelay()
    {
        var policy = new SyncRetryPolicy
        {
            BaseDelaySeconds = 30,
            MaxDelaySeconds = 300 // 5 minutes
        };

        // At attempt 5: 30 * 2^4 = 480, but max is 300
        policy.GetDelaySeconds(5).Should().Be(300);
    }

    [Fact]
    public void SyncRetryPolicy_GetDelaySeconds_HandlesZeroAndNegative()
    {
        var policy = new SyncRetryPolicy { BaseDelaySeconds = 30 };

        policy.GetDelaySeconds(0).Should().Be(30);
        policy.GetDelaySeconds(-1).Should().Be(30);
    }

    #endregion

    #region SyncProcessingResult Tests

    [Fact]
    public void SyncProcessingResult_Success_CreatesSuccessResult()
    {
        var started = DateTime.UtcNow.AddSeconds(-5);

        var result = SyncProcessingResult.Success(10, 10, started);

        result.IsSuccess.Should().BeTrue();
        result.TotalProcessed.Should().Be(10);
        result.Succeeded.Should().Be(10);
        result.Failed.Should().Be(0);
        result.DurationMs.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public void SyncProcessingResult_WithFailures_CreatesFailureResult()
    {
        var started = DateTime.UtcNow.AddSeconds(-5);
        var errors = new List<string> { "Error 1", "Error 2" };

        var result = SyncProcessingResult.WithFailures(10, 8, 2, errors, started);

        result.IsSuccess.Should().BeFalse();
        result.TotalProcessed.Should().Be(10);
        result.Succeeded.Should().Be(8);
        result.Failed.Should().Be(2);
        result.Errors.Should().HaveCount(2);
    }

    #endregion

    /// <summary>
    /// Test entity for testing generic enqueue methods.
    /// </summary>
    private class TestEntity : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
    }
}
