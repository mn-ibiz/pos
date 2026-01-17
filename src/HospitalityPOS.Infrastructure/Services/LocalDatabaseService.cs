using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing the local database.
/// </summary>
public class LocalDatabaseService : ILocalDatabaseService
{
    private readonly POSDbContext _context;
    private readonly ILogger<LocalDatabaseService> _logger;
    private LocalDatabaseConfiguration _configuration;

    public LocalDatabaseService(
        POSDbContext context,
        ILogger<LocalDatabaseService> logger,
        LocalDatabaseConfiguration? configuration = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? new LocalDatabaseConfiguration
        {
            DatabaseName = "POS_Local",
            DataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HospitalityPOS", "Data"),
            BackupDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HospitalityPOS", "Backups"),
            AutoBackup = true,
            BackupRetentionDays = 7
        };
    }

    #region Database Initialization

    /// <inheritdoc />
    public async Task<DatabaseOperationResult> InitializeDatabaseAsync()
    {
        try
        {
            _logger.LogInformation("Initializing local database...");

            // Ensure data directory exists
            if (!string.IsNullOrEmpty(_configuration.DataDirectory) && !Directory.Exists(_configuration.DataDirectory))
            {
                Directory.CreateDirectory(_configuration.DataDirectory);
                _logger.LogInformation("Created data directory: {DataDirectory}", _configuration.DataDirectory);
            }

            // Ensure backup directory exists
            if (!string.IsNullOrEmpty(_configuration.BackupDirectory) && !Directory.Exists(_configuration.BackupDirectory))
            {
                Directory.CreateDirectory(_configuration.BackupDirectory);
                _logger.LogInformation("Created backup directory: {BackupDirectory}", _configuration.BackupDirectory);
            }

            // Check if database can be connected
            var canConnect = await _context.Database.CanConnectAsync();
            if (!canConnect)
            {
                // Create the database
                await _context.Database.EnsureCreatedAsync();
                _logger.LogInformation("Database created successfully");
            }

            // Apply any pending migrations
            var migrationResult = await ApplyMigrationsAsync();
            if (!migrationResult.Success)
            {
                return migrationResult;
            }

            _logger.LogInformation("Local database initialized successfully");
            return DatabaseOperationResult.Succeeded("Database initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize local database");
            return DatabaseOperationResult.Failed("Failed to initialize database", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<DatabaseOperationResult> ValidateDatabaseSchemaAsync()
    {
        try
        {
            _logger.LogInformation("Validating database schema...");

            var canConnect = await _context.Database.CanConnectAsync();
            if (!canConnect)
            {
                return DatabaseOperationResult.Failed("Cannot connect to database");
            }

            // Check for pending migrations
            var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                return DatabaseOperationResult.Failed(
                    $"Database schema is outdated. {pendingMigrations.Count()} pending migrations.",
                    string.Join(", ", pendingMigrations));
            }

            // Verify critical tables exist by querying them
            try
            {
                _ = await _context.Users.AnyAsync();
                _ = await _context.Products.AnyAsync();
                _ = await _context.Stores.AnyAsync();
            }
            catch (Exception ex)
            {
                return DatabaseOperationResult.Failed("Critical tables missing", ex.Message);
            }

            _logger.LogInformation("Database schema validation passed");
            return DatabaseOperationResult.Succeeded("Database schema is valid");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate database schema");
            return DatabaseOperationResult.Failed("Schema validation failed", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<DatabaseOperationResult> ApplyMigrationsAsync()
    {
        try
        {
            _logger.LogInformation("Checking for pending migrations...");

            var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
            if (!pendingMigrations.Any())
            {
                _logger.LogInformation("No pending migrations");
                return DatabaseOperationResult.Succeeded("No migrations to apply");
            }

            _logger.LogInformation("Applying {Count} pending migrations...", pendingMigrations.Count());

            // Create backup before migration
            if (_configuration.AutoBackup)
            {
                var backupResult = await BackupDatabaseAsync();
                if (!backupResult.Success)
                {
                    _logger.LogWarning("Failed to create pre-migration backup: {Message}", backupResult.Message);
                }
            }

            await _context.Database.MigrateAsync();

            _logger.LogInformation("Successfully applied {Count} migrations", pendingMigrations.Count());
            return DatabaseOperationResult.Succeeded($"Applied {pendingMigrations.Count()} migrations successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply migrations");
            return DatabaseOperationResult.Failed("Failed to apply migrations", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<bool> DatabaseExistsAsync()
    {
        try
        {
            return await _context.Database.CanConnectAsync();
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Database Status

    /// <inheritdoc />
    public async Task<DatabaseStatusDto> GetStatusAsync()
    {
        try
        {
            var status = new DatabaseStatusDto();

            // Check connectivity
            status.IsOnline = await _context.Database.CanConnectAsync();

            if (!status.IsOnline)
            {
                return status;
            }

            status.IsInitialized = true;

            // Get version info
            status.ServerVersion = await GetServerVersionAsync();
            status.Version = await GetDatabaseVersionAsync();

            // Get migration info
            var appliedMigrations = await _context.Database.GetAppliedMigrationsAsync();
            status.AppliedMigrations = appliedMigrations.ToList();
            status.LastMigration = appliedMigrations.Any()
                ? DateTime.UtcNow // Would need to store migration timestamps
                : DateTime.MinValue;

            // Get table count
            status.TableCount = await GetTableCountAsync();

            // Get size
            status.SizeBytes = await GetDatabaseSizeAsync();

            // Get last backup
            status.LastBackup = await GetLastBackupDateAsync();

            // Get pending sync count
            status.PendingSyncItems = await GetPendingSyncCountAsync();

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get database status");
            return new DatabaseStatusDto { IsOnline = false, IsInitialized = false };
        }
    }

    /// <inheritdoc />
    public async Task<string> GetDatabaseVersionAsync()
    {
        try
        {
            var appliedMigrations = await _context.Database.GetAppliedMigrationsAsync();
            var lastMigration = appliedMigrations.LastOrDefault();
            return lastMigration ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    /// <inheritdoc />
    public async Task<int> GetPendingSyncCountAsync()
    {
        try
        {
            // Check if SyncQueue table exists and get count
            var syncQueues = _context.SyncQueues;
            if (syncQueues != null)
            {
                return await syncQueues.CountAsync(s => s.Status == SyncQueueItemStatus.Pending);
            }
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    private async Task<string> GetServerVersionAsync()
    {
        try
        {
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();
            var version = connection.ServerVersion;
            await connection.CloseAsync();
            return version;
        }
        catch
        {
            return "Unknown";
        }
    }

    private async Task<int> GetTableCountAsync()
    {
        try
        {
            // Use raw SQL to count tables
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";
            var result = await command.ExecuteScalarAsync();
            await connection.CloseAsync();

            return result != null ? Convert.ToInt32(result) : 0;
        }
        catch
        {
            return 0;
        }
    }

    #endregion

    #region Backup and Restore

    /// <inheritdoc />
    public async Task<DatabaseOperationResult> BackupDatabaseAsync(string? backupPath = null)
    {
        try
        {
            var backupDir = backupPath ?? _configuration.BackupDirectory;

            if (string.IsNullOrEmpty(backupDir))
            {
                backupDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "HospitalityPOS", "Backups");
            }

            if (!Directory.Exists(backupDir))
            {
                Directory.CreateDirectory(backupDir);
            }

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var backupFileName = $"{_configuration.DatabaseName}_Backup_{timestamp}.bak";
            var fullBackupPath = Path.Combine(backupDir, backupFileName);

            _logger.LogInformation("Creating database backup: {BackupPath}", fullBackupPath);

            // Execute backup command using SQL
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = $"BACKUP DATABASE [{_configuration.DatabaseName}] TO DISK = '{fullBackupPath}' WITH FORMAT, INIT";
            command.CommandTimeout = 300; // 5 minutes timeout

            await command.ExecuteNonQueryAsync();
            await connection.CloseAsync();

            // Verify backup was created
            if (!File.Exists(fullBackupPath))
            {
                return DatabaseOperationResult.Failed("Backup file was not created");
            }

            var fileInfo = new FileInfo(fullBackupPath);
            _logger.LogInformation("Backup created successfully: {BackupPath}, Size: {Size} bytes",
                fullBackupPath, fileInfo.Length);

            return DatabaseOperationResult.Succeeded($"Backup created successfully: {backupFileName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create database backup");
            return DatabaseOperationResult.Failed("Failed to create backup", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<DatabaseOperationResult> RestoreDatabaseAsync(string backupPath)
    {
        try
        {
            if (!File.Exists(backupPath))
            {
                return DatabaseOperationResult.Failed("Backup file not found", backupPath);
            }

            _logger.LogInformation("Restoring database from: {BackupPath}", backupPath);

            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            // Set database to single user mode and restore
            using var command = connection.CreateCommand();
            command.CommandText = $@"
                ALTER DATABASE [{_configuration.DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                RESTORE DATABASE [{_configuration.DatabaseName}] FROM DISK = '{backupPath}' WITH REPLACE;
                ALTER DATABASE [{_configuration.DatabaseName}] SET MULTI_USER;";
            command.CommandTimeout = 600; // 10 minutes timeout

            await command.ExecuteNonQueryAsync();
            await connection.CloseAsync();

            _logger.LogInformation("Database restored successfully from: {BackupPath}", backupPath);
            return DatabaseOperationResult.Succeeded("Database restored successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore database from backup");
            return DatabaseOperationResult.Failed("Failed to restore database", ex.Message);
        }
    }

    /// <inheritdoc />
    public Task<List<BackupInfoDto>> GetAvailableBackupsAsync()
    {
        var backups = new List<BackupInfoDto>();

        try
        {
            var backupDir = _configuration.BackupDirectory;
            if (string.IsNullOrEmpty(backupDir) || !Directory.Exists(backupDir))
            {
                return Task.FromResult(backups);
            }

            var backupFiles = Directory.GetFiles(backupDir, "*.bak")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTimeUtc);

            foreach (var file in backupFiles)
            {
                backups.Add(new BackupInfoDto
                {
                    FileName = file.Name,
                    FilePath = file.FullName,
                    CreatedAt = file.CreationTimeUtc,
                    SizeBytes = file.Length,
                    DatabaseVersion = ExtractVersionFromFileName(file.Name),
                    IsCompressed = false
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available backups");
        }

        return Task.FromResult(backups);
    }

    /// <inheritdoc />
    public async Task<int> CleanupOldBackupsAsync(int retentionDays)
    {
        try
        {
            var backups = await GetAvailableBackupsAsync();
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

            var oldBackups = backups.Where(b => b.CreatedAt < cutoffDate).ToList();
            var deletedCount = 0;

            foreach (var backup in oldBackups)
            {
                try
                {
                    File.Delete(backup.FilePath);
                    deletedCount++;
                    _logger.LogInformation("Deleted old backup: {FileName}", backup.FileName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete backup: {FileName}", backup.FileName);
                }
            }

            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old backups");
            return 0;
        }
    }

    /// <inheritdoc />
    public async Task<DateTime?> GetLastBackupDateAsync()
    {
        var backups = await GetAvailableBackupsAsync();
        return backups.FirstOrDefault()?.CreatedAt;
    }

    private static string ExtractVersionFromFileName(string fileName)
    {
        // Extract version from backup file name pattern: Database_Backup_YYYYMMDD_HHMMSS.bak
        try
        {
            var parts = Path.GetFileNameWithoutExtension(fileName).Split('_');
            if (parts.Length >= 3)
            {
                return parts[^2] + "_" + parts[^1]; // Returns date_time
            }
        }
        catch { }
        return "Unknown";
    }

    #endregion

    #region Configuration

    /// <inheritdoc />
    public LocalDatabaseConfiguration GetConfiguration()
    {
        return _configuration;
    }

    /// <inheritdoc />
    public void UpdateConfiguration(LocalDatabaseConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    #endregion

    #region Maintenance

    /// <inheritdoc />
    public async Task<DatabaseOperationResult> OptimizeDatabaseAsync()
    {
        try
        {
            _logger.LogInformation("Optimizing database...");

            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            using var command = connection.CreateCommand();

            // Rebuild indexes
            command.CommandText = $@"
                DECLARE @TableName NVARCHAR(255)
                DECLARE TableCursor CURSOR FOR
                SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'

                OPEN TableCursor
                FETCH NEXT FROM TableCursor INTO @TableName

                WHILE @@FETCH_STATUS = 0
                BEGIN
                    EXEC('ALTER INDEX ALL ON [' + @TableName + '] REBUILD')
                    FETCH NEXT FROM TableCursor INTO @TableName
                END

                CLOSE TableCursor
                DEALLOCATE TableCursor";
            command.CommandTimeout = 600;

            await command.ExecuteNonQueryAsync();

            // Update statistics
            command.CommandText = "EXEC sp_updatestats";
            await command.ExecuteNonQueryAsync();

            await connection.CloseAsync();

            _logger.LogInformation("Database optimization completed");
            return DatabaseOperationResult.Succeeded("Database optimized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to optimize database");
            return DatabaseOperationResult.Failed("Failed to optimize database", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<DatabaseOperationResult> CheckIntegrityAsync()
    {
        try
        {
            _logger.LogInformation("Checking database integrity...");

            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = $"DBCC CHECKDB ([{_configuration.DatabaseName}]) WITH NO_INFOMSGS";
            command.CommandTimeout = 600;

            await command.ExecuteNonQueryAsync();
            await connection.CloseAsync();

            _logger.LogInformation("Database integrity check passed");
            return DatabaseOperationResult.Succeeded("Database integrity check passed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database integrity check failed");
            return DatabaseOperationResult.Failed("Database integrity check failed", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<long> GetDatabaseSizeAsync()
    {
        try
        {
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT SUM(CAST(size AS BIGINT) * 8 * 1024) AS SizeBytes
                FROM sys.database_files";

            var result = await command.ExecuteScalarAsync();
            await connection.CloseAsync();

            return result != null && result != DBNull.Value ? Convert.ToInt64(result) : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get database size");
            return 0;
        }
    }

    #endregion
}
