using FluentAssertions;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Services;
using Moq;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for StoreSyncService.
/// </summary>
public class StoreSyncServiceTests
{
    private readonly Mock<IRepository<SyncConfiguration>> _syncConfigRepoMock;
    private readonly Mock<IRepository<SyncEntityRule>> _entityRuleRepoMock;
    private readonly Mock<IRepository<SyncBatch>> _batchRepoMock;
    private readonly Mock<IRepository<SyncRecord>> _recordRepoMock;
    private readonly Mock<IRepository<SyncConflict>> _conflictRepoMock;
    private readonly Mock<IRepository<SyncLog>> _logRepoMock;
    private readonly Mock<IRepository<Store>> _storeRepoMock;
    private readonly StoreSyncService _service;

    public StoreSyncServiceTests()
    {
        _syncConfigRepoMock = new Mock<IRepository<SyncConfiguration>>();
        _entityRuleRepoMock = new Mock<IRepository<SyncEntityRule>>();
        _batchRepoMock = new Mock<IRepository<SyncBatch>>();
        _recordRepoMock = new Mock<IRepository<SyncRecord>>();
        _conflictRepoMock = new Mock<IRepository<SyncConflict>>();
        _logRepoMock = new Mock<IRepository<SyncLog>>();
        _storeRepoMock = new Mock<IRepository<Store>>();

        _service = new StoreSyncService(
            _syncConfigRepoMock.Object,
            _entityRuleRepoMock.Object,
            _batchRepoMock.Object,
            _recordRepoMock.Object,
            _conflictRepoMock.Object,
            _logRepoMock.Object,
            _storeRepoMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullSyncConfigRepository_ThrowsArgumentNullException()
    {
        var action = () => new StoreSyncService(
            null!,
            _entityRuleRepoMock.Object,
            _batchRepoMock.Object,
            _recordRepoMock.Object,
            _conflictRepoMock.Object,
            _logRepoMock.Object,
            _storeRepoMock.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("syncConfigRepository");
    }

    [Fact]
    public void Constructor_WithNullEntityRuleRepository_ThrowsArgumentNullException()
    {
        var action = () => new StoreSyncService(
            _syncConfigRepoMock.Object,
            null!,
            _batchRepoMock.Object,
            _recordRepoMock.Object,
            _conflictRepoMock.Object,
            _logRepoMock.Object,
            _storeRepoMock.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("entityRuleRepository");
    }

    [Fact]
    public void Constructor_WithNullBatchRepository_ThrowsArgumentNullException()
    {
        var action = () => new StoreSyncService(
            _syncConfigRepoMock.Object,
            _entityRuleRepoMock.Object,
            null!,
            _recordRepoMock.Object,
            _conflictRepoMock.Object,
            _logRepoMock.Object,
            _storeRepoMock.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("batchRepository");
    }

    [Fact]
    public void Constructor_WithNullRecordRepository_ThrowsArgumentNullException()
    {
        var action = () => new StoreSyncService(
            _syncConfigRepoMock.Object,
            _entityRuleRepoMock.Object,
            _batchRepoMock.Object,
            null!,
            _conflictRepoMock.Object,
            _logRepoMock.Object,
            _storeRepoMock.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("recordRepository");
    }

    [Fact]
    public void Constructor_WithNullConflictRepository_ThrowsArgumentNullException()
    {
        var action = () => new StoreSyncService(
            _syncConfigRepoMock.Object,
            _entityRuleRepoMock.Object,
            _batchRepoMock.Object,
            _recordRepoMock.Object,
            null!,
            _logRepoMock.Object,
            _storeRepoMock.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("conflictRepository");
    }

    [Fact]
    public void Constructor_WithNullLogRepository_ThrowsArgumentNullException()
    {
        var action = () => new StoreSyncService(
            _syncConfigRepoMock.Object,
            _entityRuleRepoMock.Object,
            _batchRepoMock.Object,
            _recordRepoMock.Object,
            _conflictRepoMock.Object,
            null!,
            _storeRepoMock.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logRepository");
    }

    [Fact]
    public void Constructor_WithNullStoreRepository_ThrowsArgumentNullException()
    {
        var action = () => new StoreSyncService(
            _syncConfigRepoMock.Object,
            _entityRuleRepoMock.Object,
            _batchRepoMock.Object,
            _recordRepoMock.Object,
            _conflictRepoMock.Object,
            _logRepoMock.Object,
            null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("storeRepository");
    }

    #endregion

    #region Sync Configuration Tests

    [Fact]
    public async Task GetSyncConfigurationAsync_WithExistingConfig_ReturnsConfiguration()
    {
        // Arrange
        var storeId = 1;
        var config = new SyncConfiguration
        {
            Id = 1,
            StoreId = storeId,
            SyncIntervalSeconds = 30,
            IsEnabled = true,
            MaxBatchSize = 100
        };

        var store = new Store { Id = storeId, Code = "STORE01", Name = "Test Store" };

        _syncConfigRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<SyncConfiguration> { config });
        _storeRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Store> { store });
        _entityRuleRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<SyncEntityRule>());

        // Act
        var result = await _service.GetSyncConfigurationAsync(storeId);

        // Assert
        result.Should().NotBeNull();
        result!.StoreId.Should().Be(storeId);
        result.StoreName.Should().Be("Test Store");
        result.SyncIntervalSeconds.Should().Be(30);
        result.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task GetSyncConfigurationAsync_WithNoConfig_ReturnsNull()
    {
        // Arrange
        _syncConfigRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<SyncConfiguration>());

        // Act
        var result = await _service.GetSyncConfigurationAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateSyncConfigurationAsync_CreatesConfiguration()
    {
        // Arrange
        var dto = new CreateSyncConfigurationDto
        {
            StoreId = 1,
            SyncIntervalSeconds = 60,
            IsEnabled = true,
            MaxBatchSize = 200
        };

        var store = new Store { Id = 1, Code = "STORE01", Name = "Test Store" };

        _syncConfigRepoMock.Setup(r => r.AddAsync(It.IsAny<SyncConfiguration>()))
            .Returns(Task.CompletedTask);
        _storeRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Store> { store });
        _entityRuleRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<SyncEntityRule>());

        // Act
        var result = await _service.CreateSyncConfigurationAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.StoreId.Should().Be(1);
        result.SyncIntervalSeconds.Should().Be(60);
        _syncConfigRepoMock.Verify(r => r.AddAsync(It.IsAny<SyncConfiguration>()), Times.Once);
    }

    [Fact]
    public async Task SetSyncEnabledAsync_EnablesSync()
    {
        // Arrange
        var config = new SyncConfiguration
        {
            Id = 1,
            StoreId = 1,
            IsEnabled = false
        };

        _syncConfigRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<SyncConfiguration> { config });
        _syncConfigRepoMock.Setup(r => r.UpdateAsync(It.IsAny<SyncConfiguration>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.SetSyncEnabledAsync(1, true);

        // Assert
        result.Should().BeTrue();
        _syncConfigRepoMock.Verify(r => r.UpdateAsync(It.Is<SyncConfiguration>(c => c.IsEnabled)), Times.Once);
    }

    #endregion

    #region Entity Rules Tests

    [Fact]
    public async Task AddEntityRuleAsync_AddsRule()
    {
        // Arrange
        var dto = new CreateSyncEntityRuleDto
        {
            EntityType = SyncEntityType.Product,
            Direction = SyncDirection.Download,
            ConflictResolution = ConflictWinner.HQ,
            Priority = 10
        };

        _entityRuleRepoMock.Setup(r => r.AddAsync(It.IsAny<SyncEntityRule>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.AddEntityRuleAsync(1, dto);

        // Assert
        result.Should().NotBeNull();
        result.EntityType.Should().Be(SyncEntityType.Product);
        result.Direction.Should().Be(SyncDirection.Download);
        _entityRuleRepoMock.Verify(r => r.AddAsync(It.IsAny<SyncEntityRule>()), Times.Once);
    }

    [Fact]
    public async Task GetEntitySyncDirectionAsync_ReturnsDirection()
    {
        // Arrange
        var config = new SyncConfiguration { Id = 1, StoreId = 1 };
        var rule = new SyncEntityRule
        {
            Id = 1,
            SyncConfigurationId = 1,
            EntityType = SyncEntityType.Product,
            Direction = SyncDirection.Download,
            IsEnabled = true
        };

        _syncConfigRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<SyncConfiguration> { config });
        _entityRuleRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<SyncEntityRule> { rule });

        // Act
        var result = await _service.GetEntitySyncDirectionAsync(1, SyncEntityType.Product);

        // Assert
        result.Should().Be(SyncDirection.Download);
    }

    #endregion

    #region Sync Operations Tests

    [Fact]
    public async Task StartSyncAsync_WithNoConfig_ReturnsFailure()
    {
        // Arrange
        var request = new StartSyncRequestDto { StoreId = 999 };

        _syncConfigRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<SyncConfiguration>());
        _logRepoMock.Setup(r => r.AddAsync(It.IsAny<SyncLog>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.StartSyncAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task StartSyncAsync_WithDisabledSync_ReturnsFailure()
    {
        // Arrange
        var config = new SyncConfiguration
        {
            Id = 1,
            StoreId = 1,
            IsEnabled = false
        };

        var request = new StartSyncRequestDto { StoreId = 1, Force = false };

        _syncConfigRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<SyncConfiguration> { config });
        _logRepoMock.Setup(r => r.AddAsync(It.IsAny<SyncLog>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.StartSyncAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("disabled");
    }

    [Fact]
    public async Task UploadDataAsync_CreatesAndCompletesBatch()
    {
        // Arrange
        _batchRepoMock.Setup(r => r.AddAsync(It.IsAny<SyncBatch>()))
            .Returns(Task.CompletedTask);
        _batchRepoMock.Setup(r => r.UpdateAsync(It.IsAny<SyncBatch>()))
            .Returns(Task.CompletedTask);
        _logRepoMock.Setup(r => r.AddAsync(It.IsAny<SyncLog>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UploadDataAsync(1, SyncEntityType.Order);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _batchRepoMock.Verify(r => r.AddAsync(It.Is<SyncBatch>(b =>
            b.Direction == SyncDirection.Upload &&
            b.EntityType == SyncEntityType.Order)), Times.Once);
    }

    [Fact]
    public async Task DownloadDataAsync_CreatesAndCompletesBatch()
    {
        // Arrange
        _batchRepoMock.Setup(r => r.AddAsync(It.IsAny<SyncBatch>()))
            .Returns(Task.CompletedTask);
        _batchRepoMock.Setup(r => r.UpdateAsync(It.IsAny<SyncBatch>()))
            .Returns(Task.CompletedTask);
        _logRepoMock.Setup(r => r.AddAsync(It.IsAny<SyncLog>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DownloadDataAsync(1, SyncEntityType.Product);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _batchRepoMock.Verify(r => r.AddAsync(It.Is<SyncBatch>(b =>
            b.Direction == SyncDirection.Download &&
            b.EntityType == SyncEntityType.Product)), Times.Once);
    }

    [Fact]
    public async Task CancelSyncAsync_CancelsPendingBatch()
    {
        // Arrange
        var batch = new SyncBatch
        {
            Id = 1,
            StoreId = 1,
            Status = SyncBatchStatus.Pending
        };

        _batchRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(batch);
        _batchRepoMock.Setup(r => r.UpdateAsync(It.IsAny<SyncBatch>()))
            .Returns(Task.CompletedTask);
        _logRepoMock.Setup(r => r.AddAsync(It.IsAny<SyncLog>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CancelSyncAsync(1);

        // Assert
        result.Should().BeTrue();
        _batchRepoMock.Verify(r => r.UpdateAsync(It.Is<SyncBatch>(b =>
            b.Status == SyncBatchStatus.Cancelled)), Times.Once);
    }

    [Fact]
    public async Task CancelSyncAsync_DoesNotCancelCompletedBatch()
    {
        // Arrange
        var batch = new SyncBatch
        {
            Id = 1,
            StoreId = 1,
            Status = SyncBatchStatus.Completed
        };

        _batchRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(batch);

        // Act
        var result = await _service.CancelSyncAsync(1);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Batch Management Tests

    [Fact]
    public async Task GetActiveBatchesAsync_ReturnsInProgressBatches()
    {
        // Arrange
        var batches = new List<SyncBatch>
        {
            new SyncBatch { Id = 1, StoreId = 1, Status = SyncBatchStatus.InProgress },
            new SyncBatch { Id = 2, StoreId = 1, Status = SyncBatchStatus.Completed },
            new SyncBatch { Id = 3, StoreId = 2, Status = SyncBatchStatus.InProgress }
        };

        var stores = new List<Store>
        {
            new Store { Id = 1, Code = "S1", Name = "Store 1" },
            new Store { Id = 2, Code = "S2", Name = "Store 2" }
        };

        _batchRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(batches);
        _storeRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(stores);

        // Act
        var result = await _service.GetActiveBatchesAsync();

        // Assert
        result.Should().HaveCount(2);
        result.All(b => b.Status == SyncBatchStatus.InProgress).Should().BeTrue();
    }

    [Fact]
    public async Task GetFailedBatchesAsync_ReturnsFailedBatches()
    {
        // Arrange
        var batches = new List<SyncBatch>
        {
            new SyncBatch { Id = 1, StoreId = 1, Status = SyncBatchStatus.Failed },
            new SyncBatch { Id = 2, StoreId = 1, Status = SyncBatchStatus.Completed },
            new SyncBatch { Id = 3, StoreId = 2, Status = SyncBatchStatus.Failed }
        };

        var stores = new List<Store>
        {
            new Store { Id = 1, Code = "S1", Name = "Store 1" },
            new Store { Id = 2, Code = "S2", Name = "Store 2" }
        };

        _batchRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(batches);
        _storeRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(stores);

        // Act
        var result = await _service.GetFailedBatchesAsync();

        // Assert
        result.Should().HaveCount(2);
        result.All(b => b.Status == SyncBatchStatus.Failed).Should().BeTrue();
    }

    [Fact]
    public async Task CleanupOldBatchesAsync_MarksOldBatchesInactive()
    {
        // Arrange
        var oldDate = DateTime.UtcNow.AddDays(-60);
        var batches = new List<SyncBatch>
        {
            new SyncBatch { Id = 1, Status = SyncBatchStatus.Completed, CompletedAt = oldDate },
            new SyncBatch { Id = 2, Status = SyncBatchStatus.Completed, CompletedAt = DateTime.UtcNow },
            new SyncBatch { Id = 3, Status = SyncBatchStatus.Cancelled, CompletedAt = oldDate }
        };

        _batchRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(batches);
        _batchRepoMock.Setup(r => r.UpdateAsync(It.IsAny<SyncBatch>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CleanupOldBatchesAsync(30);

        // Assert
        result.Should().Be(2);
        _batchRepoMock.Verify(r => r.UpdateAsync(It.Is<SyncBatch>(b => !b.IsActive)), Times.Exactly(2));
    }

    #endregion

    #region Conflict Management Tests

    [Fact]
    public async Task GetUnresolvedConflictsAsync_ReturnsUnresolvedConflicts()
    {
        // Arrange
        var conflicts = new List<SyncConflict>
        {
            new SyncConflict { Id = 1, SyncBatchId = 1, IsResolved = false },
            new SyncConflict { Id = 2, SyncBatchId = 1, IsResolved = true },
            new SyncConflict { Id = 3, SyncBatchId = 2, IsResolved = false }
        };

        _conflictRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(conflicts);

        // Act
        var result = await _service.GetUnresolvedConflictsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.All(c => !c.IsResolved).Should().BeTrue();
    }

    [Fact]
    public async Task ResolveConflictAsync_ResolvesConflict()
    {
        // Arrange
        var conflict = new SyncConflict
        {
            Id = 1,
            SyncBatchId = 1,
            EntityType = SyncEntityType.Product,
            EntityId = 100,
            IsResolved = false
        };

        var dto = new ResolveConflictDto
        {
            ConflictId = 1,
            Resolution = ConflictWinner.HQ,
            Notes = "HQ data is authoritative"
        };

        _conflictRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(conflict);
        _conflictRepoMock.Setup(r => r.UpdateAsync(It.IsAny<SyncConflict>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ResolveConflictAsync(dto);

        // Assert
        result.IsResolved.Should().BeTrue();
        result.Resolution.Should().Be(ConflictWinner.HQ);
        result.ResolutionNotes.Should().Be("HQ data is authoritative");
    }

    [Fact]
    public async Task BulkResolveConflictsAsync_ResolvesMultipleConflicts()
    {
        // Arrange
        var conflicts = new List<SyncConflict>
        {
            new SyncConflict { Id = 1, IsResolved = false },
            new SyncConflict { Id = 2, IsResolved = false },
            new SyncConflict { Id = 3, IsResolved = true } // Already resolved
        };

        _conflictRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(conflicts[0]);
        _conflictRepoMock.Setup(r => r.GetByIdAsync(2))
            .ReturnsAsync(conflicts[1]);
        _conflictRepoMock.Setup(r => r.GetByIdAsync(3))
            .ReturnsAsync(conflicts[2]);
        _conflictRepoMock.Setup(r => r.UpdateAsync(It.IsAny<SyncConflict>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkResolveConflictsAsync(
            new List<int> { 1, 2, 3 },
            ConflictWinner.Store,
            "Bulk resolution");

        // Assert
        result.Should().Be(2); // Only 2 unresolved conflicts
    }

    #endregion

    #region Sync Status Tests

    [Fact]
    public async Task GetStoreSyncStatusAsync_ReturnsStatus()
    {
        // Arrange
        var store = new Store { Id = 1, Code = "STORE01", Name = "Test Store" };
        var config = new SyncConfiguration
        {
            Id = 1,
            StoreId = 1,
            IsEnabled = true,
            LastSuccessfulSync = DateTime.UtcNow.AddMinutes(-2) // Online
        };

        _storeRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Store> { store });
        _syncConfigRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<SyncConfiguration> { config });
        _batchRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<SyncBatch>());
        _conflictRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<SyncConflict>());

        // Act
        var result = await _service.GetStoreSyncStatusAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.StoreId.Should().Be(1);
        result.StoreName.Should().Be("Test Store");
        result.IsConfigured.Should().BeTrue();
        result.IsEnabled.Should().BeTrue();
        result.IsOnline.Should().BeTrue();
    }

    [Fact]
    public async Task IsStoreSyncingAsync_ReturnsTrueWhenSyncing()
    {
        // Arrange
        var batches = new List<SyncBatch>
        {
            new SyncBatch { Id = 1, StoreId = 1, Status = SyncBatchStatus.InProgress }
        };

        _batchRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(batches);

        // Act
        var result = await _service.IsStoreSyncingAsync(1);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetTimeSinceLastSyncAsync_ReturnsTimeSpan()
    {
        // Arrange
        var lastSync = DateTime.UtcNow.AddMinutes(-30);
        var config = new SyncConfiguration
        {
            Id = 1,
            StoreId = 1,
            LastSuccessfulSync = lastSync
        };

        _syncConfigRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<SyncConfiguration> { config });

        // Act
        var result = await _service.GetTimeSinceLastSyncAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Value.TotalMinutes.Should().BeApproximately(30, 1);
    }

    [Fact]
    public async Task GetChainSyncDashboardAsync_ReturnsDashboard()
    {
        // Arrange
        var stores = new List<Store>
        {
            new Store { Id = 1, Code = "S1", Name = "Store 1" },
            new Store { Id = 2, Code = "S2", Name = "Store 2" }
        };

        var configs = new List<SyncConfiguration>
        {
            new SyncConfiguration { Id = 1, StoreId = 1, IsEnabled = true, LastSuccessfulSync = DateTime.UtcNow.AddMinutes(-1) },
            new SyncConfiguration { Id = 2, StoreId = 2, IsEnabled = true, LastSuccessfulSync = DateTime.UtcNow.AddHours(-1) }
        };

        _storeRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(stores);
        _syncConfigRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(configs);
        _batchRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<SyncBatch>());
        _conflictRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<SyncConflict>());
        _logRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<SyncLog>());

        // Act
        var result = await _service.GetChainSyncDashboardAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalStores.Should().Be(2);
        result.StoresOnline.Should().Be(1); // Only store 1 synced within 5 minutes
        result.StoresOffline.Should().Be(1);
    }

    #endregion

    #region Sync Logs Tests

    [Fact]
    public async Task LogSyncOperationAsync_CreatesLog()
    {
        // Arrange
        _logRepoMock.Setup(r => r.AddAsync(It.IsAny<SyncLog>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.LogSyncOperationAsync(1, 100, "Upload Products", true, "Success", null, 1500);

        // Assert
        _logRepoMock.Verify(r => r.AddAsync(It.Is<SyncLog>(l =>
            l.StoreId == 1 &&
            l.SyncBatchId == 100 &&
            l.Operation == "Upload Products" &&
            l.IsSuccess &&
            l.DurationMs == 1500)), Times.Once);
    }

    [Fact]
    public async Task GetSyncLogsAsync_FiltersLogs()
    {
        // Arrange
        var logs = new List<SyncLog>
        {
            new SyncLog { Id = 1, StoreId = 1, IsSuccess = true, Timestamp = DateTime.UtcNow },
            new SyncLog { Id = 2, StoreId = 1, IsSuccess = false, Timestamp = DateTime.UtcNow },
            new SyncLog { Id = 3, StoreId = 2, IsSuccess = true, Timestamp = DateTime.UtcNow }
        };

        _logRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(logs);

        var query = new SyncLogQueryDto { StoreId = 1, IsSuccess = false };

        // Act
        var result = await _service.GetSyncLogsAsync(query);

        // Assert
        result.Should().HaveCount(1);
        result[0].IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetErrorLogsAsync_ReturnsOnlyErrors()
    {
        // Arrange
        var logs = new List<SyncLog>
        {
            new SyncLog { Id = 1, StoreId = 1, IsSuccess = true, Timestamp = DateTime.UtcNow },
            new SyncLog { Id = 2, StoreId = 1, IsSuccess = false, ErrorMessage = "Error 1", Timestamp = DateTime.UtcNow },
            new SyncLog { Id = 3, StoreId = 1, IsSuccess = false, ErrorMessage = "Error 2", Timestamp = DateTime.UtcNow }
        };

        _logRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(logs);

        // Act
        var result = await _service.GetErrorLogsAsync(1);

        // Assert
        result.Should().HaveCount(2);
        result.All(l => !l.IsSuccess).Should().BeTrue();
    }

    #endregion

    #region Auto-Sync Tests

    [Fact]
    public async Task NeedsSyncAsync_ReturnsTrueWhenIntervalExceeded()
    {
        // Arrange
        var config = new SyncConfiguration
        {
            Id = 1,
            StoreId = 1,
            IsEnabled = true,
            SyncIntervalSeconds = 60,
            LastSuccessfulSync = DateTime.UtcNow.AddSeconds(-120) // 2 minutes ago
        };

        _syncConfigRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<SyncConfiguration> { config });

        // Act
        var result = await _service.NeedsSyncAsync(1);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task NeedsSyncAsync_ReturnsFalseWhenRecentlySync()
    {
        // Arrange
        var config = new SyncConfiguration
        {
            Id = 1,
            StoreId = 1,
            IsEnabled = true,
            SyncIntervalSeconds = 300,
            LastSuccessfulSync = DateTime.UtcNow.AddSeconds(-60) // 1 minute ago
        };

        _syncConfigRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<SyncConfiguration> { config });

        // Act
        var result = await _service.NeedsSyncAsync(1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetStoresNeedingSyncAsync_ReturnsStoresNeedingSync()
    {
        // Arrange
        var configs = new List<SyncConfiguration>
        {
            new SyncConfiguration
            {
                Id = 1,
                StoreId = 1,
                IsEnabled = true,
                SyncIntervalSeconds = 60,
                LastSuccessfulSync = DateTime.UtcNow.AddMinutes(-5) // Needs sync
            },
            new SyncConfiguration
            {
                Id = 2,
                StoreId = 2,
                IsEnabled = true,
                SyncIntervalSeconds = 600,
                LastSuccessfulSync = DateTime.UtcNow.AddMinutes(-5) // Doesn't need sync
            },
            new SyncConfiguration
            {
                Id = 3,
                StoreId = 3,
                IsEnabled = false, // Disabled
                SyncIntervalSeconds = 60,
                LastSuccessfulSync = DateTime.UtcNow.AddMinutes(-5)
            }
        };

        _syncConfigRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(configs);

        // Act
        var result = await _service.GetStoresNeedingSyncAsync();

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(1);
    }

    #endregion

    #region Statistics Tests

    [Fact]
    public async Task GetSyncStatisticsAsync_ReturnsStatistics()
    {
        // Arrange
        var store = new Store { Id = 1, Code = "S1", Name = "Store 1" };
        var batches = new List<SyncBatch>
        {
            new SyncBatch { Id = 1, StoreId = 1, Status = SyncBatchStatus.Completed, CreatedAt = DateTime.UtcNow },
            new SyncBatch { Id = 2, StoreId = 1, Status = SyncBatchStatus.Failed, CreatedAt = DateTime.UtcNow },
            new SyncBatch { Id = 3, StoreId = 1, Status = SyncBatchStatus.Completed, CreatedAt = DateTime.UtcNow }
        };

        var records = new List<SyncRecord>
        {
            new SyncRecord { Id = 1, SyncBatchId = 1, EntityType = SyncEntityType.Product, IsSuccess = true },
            new SyncRecord { Id = 2, SyncBatchId = 1, EntityType = SyncEntityType.Product, IsSuccess = true },
            new SyncRecord { Id = 3, SyncBatchId = 3, EntityType = SyncEntityType.Order, IsSuccess = true }
        };

        var logs = new List<SyncLog>
        {
            new SyncLog { Id = 1, StoreId = 1, DurationMs = 1000 },
            new SyncLog { Id = 2, StoreId = 1, DurationMs = 2000 }
        };

        _storeRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Store> { store });
        _batchRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(batches);
        _recordRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(records);
        _logRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(logs);
        _conflictRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<SyncConflict>());

        // Act
        var result = await _service.GetSyncStatisticsAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.TotalBatches.Should().Be(3);
        result.SuccessfulBatches.Should().Be(2);
        result.FailedBatches.Should().Be(1);
        result.TotalRecordsSynced.Should().Be(3);
        result.AverageSyncDurationMs.Should().Be(1500);
        result.SuccessRate.Should().BeApproximately(66.67, 1);
    }

    [Fact]
    public async Task GetChainSyncStatisticsAsync_ReturnsChainStatistics()
    {
        // Arrange
        var stores = new List<Store>
        {
            new Store { Id = 1, Code = "S1", Name = "Store 1" },
            new Store { Id = 2, Code = "S2", Name = "Store 2" }
        };

        var batches = new List<SyncBatch>
        {
            new SyncBatch { Id = 1, StoreId = 1, Status = SyncBatchStatus.Completed, CreatedAt = DateTime.UtcNow },
            new SyncBatch { Id = 2, StoreId = 2, Status = SyncBatchStatus.Completed, CreatedAt = DateTime.UtcNow }
        };

        _storeRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(stores);
        _batchRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(batches);
        _recordRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<SyncRecord>());
        _logRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<SyncLog>());
        _conflictRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<SyncConflict>());
        _syncConfigRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<SyncConfiguration>());
        _entityRuleRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<SyncEntityRule>());

        // Act
        var result = await _service.GetChainSyncStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalStores.Should().Be(2);
        result.TotalBatches.Should().Be(2);
        result.SuccessfulBatches.Should().Be(2);
        result.StoreStatistics.Should().HaveCount(2);
    }

    #endregion

    #region Data Payload Tests

    [Fact]
    public async Task ProcessUploadPayloadAsync_ProcessesPayload()
    {
        // Arrange
        var payload = new UploadSyncPayloadDto
        {
            StoreId = 1,
            EntityType = SyncEntityType.Order,
            Records = new List<SyncRecordDto>
            {
                new SyncRecordDto { EntityId = 1, EntityData = "{}", Timestamp = DateTime.UtcNow },
                new SyncRecordDto { EntityId = 2, EntityData = "{}", Timestamp = DateTime.UtcNow }
            }
        };

        _batchRepoMock.Setup(r => r.AddAsync(It.IsAny<SyncBatch>()))
            .Returns(Task.CompletedTask);
        _batchRepoMock.Setup(r => r.UpdateAsync(It.IsAny<SyncBatch>()))
            .Returns(Task.CompletedTask);
        _recordRepoMock.Setup(r => r.AddAsync(It.IsAny<SyncRecord>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ProcessUploadPayloadAsync(payload);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.RecordsProcessed.Should().Be(2);
        result.RecordsSucceeded.Should().Be(2);
        _recordRepoMock.Verify(r => r.AddAsync(It.IsAny<SyncRecord>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CreateUploadPayloadAsync_ReturnsPayload()
    {
        // Act
        var result = await _service.CreateUploadPayloadAsync(1, SyncEntityType.Order);

        // Assert
        result.Should().NotBeNull();
        result.StoreId.Should().Be(1);
        result.EntityType.Should().Be(SyncEntityType.Order);
    }

    #endregion
}
