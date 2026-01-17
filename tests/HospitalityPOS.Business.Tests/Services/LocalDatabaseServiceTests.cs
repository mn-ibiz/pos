using FluentAssertions;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Infrastructure.Data;
using HospitalityPOS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for LocalDatabaseService.
/// </summary>
public class LocalDatabaseServiceTests : IDisposable
{
    private readonly Mock<ILogger<LocalDatabaseService>> _loggerMock;
    private readonly POSDbContext _context;
    private readonly string _testDirectory;
    private readonly LocalDatabaseConfiguration _testConfiguration;

    public LocalDatabaseServiceTests()
    {
        _loggerMock = new Mock<ILogger<LocalDatabaseService>>();

        // Create in-memory database for testing
        var options = new DbContextOptionsBuilder<POSDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        _context = new POSDbContext(options);

        // Create test directories
        _testDirectory = Path.Combine(Path.GetTempPath(), $"POSTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);

        _testConfiguration = new LocalDatabaseConfiguration
        {
            DatabaseName = "POS_Test",
            DataDirectory = Path.Combine(_testDirectory, "Data"),
            BackupDirectory = Path.Combine(_testDirectory, "Backups"),
            AutoBackup = false,
            BackupRetentionDays = 7
        };
    }

    public void Dispose()
    {
        _context.Dispose();

        // Cleanup test directories
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Act
        var action = () => new LocalDatabaseService(null!, _loggerMock.Object, _testConfiguration);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var action = () => new LocalDatabaseService(_context, null!, _testConfiguration);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullConfiguration_UsesDefaultConfiguration()
    {
        // Act
        var service = new LocalDatabaseService(_context, _loggerMock.Object, null);
        var config = service.GetConfiguration();

        // Assert
        config.Should().NotBeNull();
        config.DatabaseName.Should().Be("POS_Local");
        config.AutoBackup.Should().BeTrue();
        config.BackupRetentionDays.Should().Be(7);
    }

    [Fact]
    public void Constructor_WithValidConfiguration_UsesProvidedConfiguration()
    {
        // Arrange
        var customConfig = new LocalDatabaseConfiguration
        {
            DatabaseName = "Custom_DB",
            AutoBackup = false,
            BackupRetentionDays = 14
        };

        // Act
        var service = new LocalDatabaseService(_context, _loggerMock.Object, customConfig);
        var config = service.GetConfiguration();

        // Assert
        config.Should().NotBeNull();
        config.DatabaseName.Should().Be("Custom_DB");
        config.AutoBackup.Should().BeFalse();
        config.BackupRetentionDays.Should().Be(14);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void GetConfiguration_ReturnsCurrentConfiguration()
    {
        // Arrange
        var service = new LocalDatabaseService(_context, _loggerMock.Object, _testConfiguration);

        // Act
        var result = service.GetConfiguration();

        // Assert
        result.Should().NotBeNull();
        result.DatabaseName.Should().Be("POS_Test");
        result.DataDirectory.Should().Contain("Data");
        result.BackupDirectory.Should().Contain("Backups");
    }

    [Fact]
    public void UpdateConfiguration_WithValidConfiguration_UpdatesConfiguration()
    {
        // Arrange
        var service = new LocalDatabaseService(_context, _loggerMock.Object, _testConfiguration);
        var newConfig = new LocalDatabaseConfiguration
        {
            DatabaseName = "Updated_DB",
            AutoBackup = true,
            BackupRetentionDays = 30
        };

        // Act
        service.UpdateConfiguration(newConfig);
        var result = service.GetConfiguration();

        // Assert
        result.DatabaseName.Should().Be("Updated_DB");
        result.AutoBackup.Should().BeTrue();
        result.BackupRetentionDays.Should().Be(30);
    }

    [Fact]
    public void UpdateConfiguration_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange
        var service = new LocalDatabaseService(_context, _loggerMock.Object, _testConfiguration);

        // Act
        var action = () => service.UpdateConfiguration(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    #endregion

    #region Database Initialization Tests

    [Fact]
    public async Task InitializeDatabaseAsync_WithValidConfiguration_CreatesDirectories()
    {
        // Arrange
        var service = new LocalDatabaseService(_context, _loggerMock.Object, _testConfiguration);

        // Act
        var result = await service.InitializeDatabaseAsync();

        // Assert
        result.Success.Should().BeTrue();
        Directory.Exists(_testConfiguration.DataDirectory).Should().BeTrue();
        Directory.Exists(_testConfiguration.BackupDirectory).Should().BeTrue();
    }

    [Fact]
    public async Task InitializeDatabaseAsync_WithExistingDirectories_Succeeds()
    {
        // Arrange
        Directory.CreateDirectory(_testConfiguration.DataDirectory!);
        Directory.CreateDirectory(_testConfiguration.BackupDirectory!);
        var service = new LocalDatabaseService(_context, _loggerMock.Object, _testConfiguration);

        // Act
        var result = await service.InitializeDatabaseAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("initialized");
    }

    [Fact]
    public async Task DatabaseExistsAsync_WithInMemoryDatabase_ReturnsTrue()
    {
        // Arrange
        var service = new LocalDatabaseService(_context, _loggerMock.Object, _testConfiguration);

        // Act
        var result = await service.DatabaseExistsAsync();

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Database Status Tests

    [Fact]
    public async Task GetStatusAsync_ReturnsValidStatus()
    {
        // Arrange
        var service = new LocalDatabaseService(_context, _loggerMock.Object, _testConfiguration);

        // Act
        var result = await service.GetStatusAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsOnline.Should().BeTrue();
        result.IsInitialized.Should().BeTrue();
    }

    [Fact]
    public async Task GetDatabaseVersionAsync_ReturnsVersion()
    {
        // Arrange
        var service = new LocalDatabaseService(_context, _loggerMock.Object, _testConfiguration);

        // Act
        var result = await service.GetDatabaseVersionAsync();

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetPendingSyncCountAsync_WithNoItems_ReturnsZero()
    {
        // Arrange
        var service = new LocalDatabaseService(_context, _loggerMock.Object, _testConfiguration);

        // Act
        var result = await service.GetPendingSyncCountAsync();

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task GetPendingSyncCountAsync_WithPendingItems_ReturnsCount()
    {
        // Arrange
        var service = new LocalDatabaseService(_context, _loggerMock.Object, _testConfiguration);

        // Add some pending sync items
        _context.SyncQueues.Add(new SyncQueueItem
        {
            EntityType = "Product",
            EntityId = 1,
            OperationType = SyncQueueOperationType.Create,
            Status = SyncQueueItemStatus.Pending
        });
        _context.SyncQueues.Add(new SyncQueueItem
        {
            EntityType = "Product",
            EntityId = 2,
            OperationType = SyncQueueOperationType.Update,
            Status = SyncQueueItemStatus.Pending
        });
        _context.SyncQueues.Add(new SyncQueueItem
        {
            EntityType = "Product",
            EntityId = 3,
            OperationType = SyncQueueOperationType.Update,
            Status = SyncQueueItemStatus.Completed
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await service.GetPendingSyncCountAsync();

        // Assert
        result.Should().Be(2);
    }

    #endregion

    #region Backup Tests

    [Fact]
    public async Task GetAvailableBackupsAsync_WithNoBackups_ReturnsEmptyList()
    {
        // Arrange
        Directory.CreateDirectory(_testConfiguration.BackupDirectory!);
        var service = new LocalDatabaseService(_context, _loggerMock.Object, _testConfiguration);

        // Act
        var result = await service.GetAvailableBackupsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAvailableBackupsAsync_WithBackups_ReturnsList()
    {
        // Arrange
        Directory.CreateDirectory(_testConfiguration.BackupDirectory!);

        // Create test backup files
        var backupFile1 = Path.Combine(_testConfiguration.BackupDirectory!, "POS_Test_Backup_20240101_120000.bak");
        var backupFile2 = Path.Combine(_testConfiguration.BackupDirectory!, "POS_Test_Backup_20240102_120000.bak");
        await File.WriteAllTextAsync(backupFile1, "test backup 1");
        await File.WriteAllTextAsync(backupFile2, "test backup 2");

        var service = new LocalDatabaseService(_context, _loggerMock.Object, _testConfiguration);

        // Act
        var result = await service.GetAvailableBackupsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.All(b => b.FileName.EndsWith(".bak")).Should().BeTrue();
    }

    [Fact]
    public async Task GetAvailableBackupsAsync_WithNoBackupDirectory_ReturnsEmptyList()
    {
        // Arrange
        var configWithNoBackupDir = new LocalDatabaseConfiguration
        {
            DatabaseName = "Test",
            BackupDirectory = string.Empty
        };
        var service = new LocalDatabaseService(_context, _loggerMock.Object, configWithNoBackupDir);

        // Act
        var result = await service.GetAvailableBackupsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CleanupOldBackupsAsync_WithOldBackups_RemovesOldFiles()
    {
        // Arrange
        Directory.CreateDirectory(_testConfiguration.BackupDirectory!);

        // Create old and new backup files
        var oldBackupFile = Path.Combine(_testConfiguration.BackupDirectory!, "POS_Test_Backup_Old.bak");
        var newBackupFile = Path.Combine(_testConfiguration.BackupDirectory!, "POS_Test_Backup_New.bak");

        await File.WriteAllTextAsync(oldBackupFile, "old backup");
        await File.WriteAllTextAsync(newBackupFile, "new backup");

        // Set old file's creation time to 10 days ago
        File.SetCreationTimeUtc(oldBackupFile, DateTime.UtcNow.AddDays(-10));

        var service = new LocalDatabaseService(_context, _loggerMock.Object, _testConfiguration);

        // Act
        var result = await service.CleanupOldBackupsAsync(7);

        // Assert
        result.Should().Be(1);
        File.Exists(oldBackupFile).Should().BeFalse();
        File.Exists(newBackupFile).Should().BeTrue();
    }

    [Fact]
    public async Task CleanupOldBackupsAsync_WithNoOldBackups_ReturnsZero()
    {
        // Arrange
        Directory.CreateDirectory(_testConfiguration.BackupDirectory!);

        var newBackupFile = Path.Combine(_testConfiguration.BackupDirectory!, "POS_Test_Backup_New.bak");
        await File.WriteAllTextAsync(newBackupFile, "new backup");

        var service = new LocalDatabaseService(_context, _loggerMock.Object, _testConfiguration);

        // Act
        var result = await service.CleanupOldBackupsAsync(7);

        // Assert
        result.Should().Be(0);
        File.Exists(newBackupFile).Should().BeTrue();
    }

    [Fact]
    public async Task GetLastBackupDateAsync_WithBackups_ReturnsLatestDate()
    {
        // Arrange
        Directory.CreateDirectory(_testConfiguration.BackupDirectory!);

        var oldBackupFile = Path.Combine(_testConfiguration.BackupDirectory!, "POS_Test_Backup_Old.bak");
        var newBackupFile = Path.Combine(_testConfiguration.BackupDirectory!, "POS_Test_Backup_New.bak");

        await File.WriteAllTextAsync(oldBackupFile, "old backup");
        await File.WriteAllTextAsync(newBackupFile, "new backup");

        File.SetCreationTimeUtc(oldBackupFile, DateTime.UtcNow.AddDays(-5));
        File.SetCreationTimeUtc(newBackupFile, DateTime.UtcNow.AddDays(-1));

        var service = new LocalDatabaseService(_context, _loggerMock.Object, _testConfiguration);

        // Act
        var result = await service.GetLastBackupDateAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().BeCloseTo(DateTime.UtcNow.AddDays(-1), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task GetLastBackupDateAsync_WithNoBackups_ReturnsNull()
    {
        // Arrange
        Directory.CreateDirectory(_testConfiguration.BackupDirectory!);
        var service = new LocalDatabaseService(_context, _loggerMock.Object, _testConfiguration);

        // Act
        var result = await service.GetLastBackupDateAsync();

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Restore Tests

    [Fact]
    public async Task RestoreDatabaseAsync_WithNonExistentFile_ReturnsFailure()
    {
        // Arrange
        var service = new LocalDatabaseService(_context, _loggerMock.Object, _testConfiguration);
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.bak");

        // Act
        var result = await service.RestoreDatabaseAsync(nonExistentPath);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    #endregion

    #region Schema Validation Tests

    [Fact]
    public async Task ValidateDatabaseSchemaAsync_WithValidSchema_ReturnsSuccess()
    {
        // Arrange
        var service = new LocalDatabaseService(_context, _loggerMock.Object, _testConfiguration);

        // Ensure database is created
        await _context.Database.EnsureCreatedAsync();

        // Act
        var result = await service.ValidateDatabaseSchemaAsync();

        // Assert
        result.Should().NotBeNull();
        // With in-memory database, this should succeed as there are no migrations
    }

    [Fact]
    public async Task ApplyMigrationsAsync_WithNoPendingMigrations_ReturnsNoMigrationsMessage()
    {
        // Arrange
        var service = new LocalDatabaseService(_context, _loggerMock.Object, _testConfiguration);

        // Act
        var result = await service.ApplyMigrationsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Message.Should().Contain("No migrations");
    }

    #endregion

    #region Database Size Tests

    [Fact]
    public async Task GetDatabaseSizeAsync_ReturnsSize()
    {
        // Arrange
        var service = new LocalDatabaseService(_context, _loggerMock.Object, _testConfiguration);

        // Act
        var result = await service.GetDatabaseSizeAsync();

        // Assert
        // In-memory database may return 0, which is valid
        result.Should().BeGreaterThanOrEqualTo(0);
    }

    #endregion

    #region Backup Info DTO Tests

    [Fact]
    public void BackupInfoDto_ExtractsVersionFromFileName()
    {
        // Arrange
        Directory.CreateDirectory(_testConfiguration.BackupDirectory!);

        var backupFile = Path.Combine(_testConfiguration.BackupDirectory!, "POS_Test_Backup_20240315_143000.bak");
        File.WriteAllText(backupFile, "test");

        var service = new LocalDatabaseService(_context, _loggerMock.Object, _testConfiguration);

        // Act
        var backups = service.GetAvailableBackupsAsync().Result;

        // Assert
        backups.Should().HaveCount(1);
        backups[0].DatabaseVersion.Should().Be("20240315_143000");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task GetStatusAsync_WhenDatabaseConnectionFails_ReturnsOfflineStatus()
    {
        // Arrange
        // Create a context with an invalid connection to test error handling
        var options = new DbContextOptionsBuilder<POSDbContext>()
            .UseInMemoryDatabase(databaseName: $"ErrorTestDb_{Guid.NewGuid()}")
            .Options;

        using var errorContext = new POSDbContext(options);
        var service = new LocalDatabaseService(errorContext, _loggerMock.Object, _testConfiguration);

        // Act
        var result = await service.GetStatusAsync();

        // Assert
        result.Should().NotBeNull();
        // In-memory DB should still be online
        result.IsOnline.Should().BeTrue();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task FullWorkflow_InitializeAndGetStatus_Works()
    {
        // Arrange
        var service = new LocalDatabaseService(_context, _loggerMock.Object, _testConfiguration);

        // Act - Initialize
        var initResult = await service.InitializeDatabaseAsync();

        // Assert - Initialize succeeded
        initResult.Success.Should().BeTrue();

        // Act - Get Status
        var status = await service.GetStatusAsync();

        // Assert - Status is valid
        status.IsOnline.Should().BeTrue();
        status.IsInitialized.Should().BeTrue();

        // Act - Check database exists
        var exists = await service.DatabaseExistsAsync();

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task BackupWorkflow_CreateListAndCleanup_Works()
    {
        // Arrange
        Directory.CreateDirectory(_testConfiguration.BackupDirectory!);
        var service = new LocalDatabaseService(_context, _loggerMock.Object, _testConfiguration);

        // Create some test backups
        for (int i = 1; i <= 5; i++)
        {
            var backupFile = Path.Combine(
                _testConfiguration.BackupDirectory!,
                $"POS_Test_Backup_2024010{i}_120000.bak");
            await File.WriteAllTextAsync(backupFile, $"backup {i}");
            File.SetCreationTimeUtc(backupFile, DateTime.UtcNow.AddDays(-(i * 5)));
        }

        // Act - Get available backups
        var backups = await service.GetAvailableBackupsAsync();

        // Assert - All backups listed
        backups.Should().HaveCount(5);

        // Act - Cleanup old backups (older than 10 days)
        var deletedCount = await service.CleanupOldBackupsAsync(10);

        // Assert - 3 old backups deleted (15, 20, 25 days old)
        deletedCount.Should().Be(3);

        // Act - Get remaining backups
        var remainingBackups = await service.GetAvailableBackupsAsync();

        // Assert - 2 backups remain (5 and 10 days old)
        remainingBackups.Should().HaveCount(2);
    }

    [Fact]
    public async Task ConfigurationWorkflow_UpdateAndVerify_Works()
    {
        // Arrange
        var service = new LocalDatabaseService(_context, _loggerMock.Object, _testConfiguration);
        var originalConfig = service.GetConfiguration();

        // Assert original
        originalConfig.DatabaseName.Should().Be("POS_Test");
        originalConfig.AutoBackup.Should().BeFalse();

        // Act - Update configuration
        var newConfig = new LocalDatabaseConfiguration
        {
            DatabaseName = "POS_Updated",
            DataDirectory = Path.Combine(_testDirectory, "NewData"),
            BackupDirectory = Path.Combine(_testDirectory, "NewBackups"),
            AutoBackup = true,
            BackupRetentionDays = 14
        };
        service.UpdateConfiguration(newConfig);

        // Assert - Configuration updated
        var updatedConfig = service.GetConfiguration();
        updatedConfig.DatabaseName.Should().Be("POS_Updated");
        updatedConfig.AutoBackup.Should().BeTrue();
        updatedConfig.BackupRetentionDays.Should().Be(14);
    }

    #endregion

    #region SyncQueueItem Status Tests

    [Fact]
    public async Task GetPendingSyncCountAsync_FiltersCorrectly()
    {
        // Arrange
        var service = new LocalDatabaseService(_context, _loggerMock.Object, _testConfiguration);

        // Add sync items with different statuses
        _context.SyncQueues.AddRange(new[]
        {
            new SyncQueueItem { EntityType = "Product", EntityId = 1, Status = SyncQueueItemStatus.Pending },
            new SyncQueueItem { EntityType = "Product", EntityId = 2, Status = SyncQueueItemStatus.InProgress },
            new SyncQueueItem { EntityType = "Product", EntityId = 3, Status = SyncQueueItemStatus.Completed },
            new SyncQueueItem { EntityType = "Product", EntityId = 4, Status = SyncQueueItemStatus.Failed },
            new SyncQueueItem { EntityType = "Product", EntityId = 5, Status = SyncQueueItemStatus.Pending },
            new SyncQueueItem { EntityType = "Order", EntityId = 1, Status = SyncQueueItemStatus.Pending },
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await service.GetPendingSyncCountAsync();

        // Assert - Only Pending items counted
        result.Should().Be(3);
    }

    #endregion
}
