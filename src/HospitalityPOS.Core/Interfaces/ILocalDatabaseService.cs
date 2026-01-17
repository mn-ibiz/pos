using HospitalityPOS.Core.DTOs;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for local database management.
/// </summary>
public interface ILocalDatabaseService
{
    #region Database Initialization

    /// <summary>
    /// Initializes the local database if it doesn't exist.
    /// </summary>
    /// <returns>Operation result.</returns>
    Task<DatabaseOperationResult> InitializeDatabaseAsync();

    /// <summary>
    /// Validates the database schema against expected version.
    /// </summary>
    /// <returns>Operation result.</returns>
    Task<DatabaseOperationResult> ValidateDatabaseSchemaAsync();

    /// <summary>
    /// Applies any pending database migrations.
    /// </summary>
    /// <returns>Operation result.</returns>
    Task<DatabaseOperationResult> ApplyMigrationsAsync();

    /// <summary>
    /// Checks if the database exists and is accessible.
    /// </summary>
    /// <returns>True if database exists and is accessible.</returns>
    Task<bool> DatabaseExistsAsync();

    #endregion

    #region Database Status

    /// <summary>
    /// Gets the current database status.
    /// </summary>
    /// <returns>Database status information.</returns>
    Task<DatabaseStatusDto> GetStatusAsync();

    /// <summary>
    /// Gets the database version.
    /// </summary>
    /// <returns>Database version string.</returns>
    Task<string> GetDatabaseVersionAsync();

    /// <summary>
    /// Gets the count of pending sync items.
    /// </summary>
    /// <returns>Number of pending sync items.</returns>
    Task<int> GetPendingSyncCountAsync();

    #endregion

    #region Backup and Restore

    /// <summary>
    /// Creates a backup of the local database.
    /// </summary>
    /// <param name="backupPath">Optional custom backup path. Uses default if not specified.</param>
    /// <returns>Operation result.</returns>
    Task<DatabaseOperationResult> BackupDatabaseAsync(string? backupPath = null);

    /// <summary>
    /// Restores the database from a backup file.
    /// </summary>
    /// <param name="backupPath">Path to the backup file.</param>
    /// <returns>Operation result.</returns>
    Task<DatabaseOperationResult> RestoreDatabaseAsync(string backupPath);

    /// <summary>
    /// Gets list of available backup files.
    /// </summary>
    /// <returns>List of backup information.</returns>
    Task<List<BackupInfoDto>> GetAvailableBackupsAsync();

    /// <summary>
    /// Cleans up old backups based on retention policy.
    /// </summary>
    /// <param name="retentionDays">Number of days to keep backups.</param>
    /// <returns>Number of backups deleted.</returns>
    Task<int> CleanupOldBackupsAsync(int retentionDays);

    /// <summary>
    /// Gets the date of the last successful backup.
    /// </summary>
    /// <returns>Last backup date or null if no backups exist.</returns>
    Task<DateTime?> GetLastBackupDateAsync();

    #endregion

    #region Configuration

    /// <summary>
    /// Gets the current database configuration.
    /// </summary>
    /// <returns>Database configuration.</returns>
    LocalDatabaseConfiguration GetConfiguration();

    /// <summary>
    /// Updates the database configuration.
    /// </summary>
    /// <param name="configuration">New configuration.</param>
    void UpdateConfiguration(LocalDatabaseConfiguration configuration);

    #endregion

    #region Maintenance

    /// <summary>
    /// Optimizes the database (shrink, rebuild indexes, etc.).
    /// </summary>
    /// <returns>Operation result.</returns>
    Task<DatabaseOperationResult> OptimizeDatabaseAsync();

    /// <summary>
    /// Checks the integrity of the database.
    /// </summary>
    /// <returns>Operation result.</returns>
    Task<DatabaseOperationResult> CheckIntegrityAsync();

    /// <summary>
    /// Gets the database size in bytes.
    /// </summary>
    /// <returns>Database size in bytes.</returns>
    Task<long> GetDatabaseSizeAsync();

    #endregion
}
