using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing local terminal configuration files.
/// </summary>
public class TerminalConfigurationService : ITerminalConfigurationService
{
    private readonly ILogger _logger;
    private readonly IMachineIdentifierService _machineIdentifierService;
    private readonly ITerminalService _terminalService;
    private readonly object _configLock = new();
    private TerminalConfiguration? _cachedConfiguration;
    private bool _configurationLoaded;

    private const string ConfigFileName = "terminal.json";
    private const string AppFolderName = "ProNetPOS";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalConfigurationService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="machineIdentifierService">The machine identifier service.</param>
    /// <param name="terminalService">The terminal service.</param>
    public TerminalConfigurationService(
        ILogger logger,
        IMachineIdentifierService machineIdentifierService,
        ITerminalService terminalService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _machineIdentifierService = machineIdentifierService ?? throw new ArgumentNullException(nameof(machineIdentifierService));
        _terminalService = terminalService ?? throw new ArgumentNullException(nameof(terminalService));
    }

    /// <inheritdoc />
    public TerminalConfiguration? GetLocalConfiguration()
    {
        lock (_configLock)
        {
            if (_configurationLoaded)
            {
                return _cachedConfiguration;
            }

            LoadConfiguration();
            return _cachedConfiguration;
        }
    }

    /// <inheritdoc />
    public async Task SaveLocalConfigurationAsync(
        TerminalConfiguration config,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);

        var filePath = GetConfigurationFilePath();
        var directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            _logger.Information("Created configuration directory: {Directory}", directory);
        }

        var json = JsonSerializer.Serialize(config, JsonOptions);

        await File.WriteAllTextAsync(filePath, json, cancellationToken).ConfigureAwait(false);

        lock (_configLock)
        {
            _cachedConfiguration = config;
            _configurationLoaded = true;
        }

        _logger.Information("Terminal configuration saved to {FilePath}", filePath);
    }

    /// <inheritdoc />
    public async Task<TerminalValidationResult> ValidateConfigurationAsync(
        CancellationToken cancellationToken = default)
    {
        var config = GetLocalConfiguration();

        if (config is null)
        {
            return TerminalValidationResult.Failure("No local configuration found.");
        }

        // Validate machine identifier
        var currentMachineId = _machineIdentifierService.GetMachineIdentifier();
        if (!string.Equals(config.Terminal.MachineIdentifier, currentMachineId, StringComparison.OrdinalIgnoreCase))
        {
            // Check if any of our MAC addresses match
            if (!_machineIdentifierService.ValidateMachineIdentifier(config.Terminal.MachineIdentifier))
            {
                return new TerminalValidationResult
                {
                    IsValid = false,
                    MachineIdentifierMatches = false,
                    IsActive = false,
                    Errors = new List<string> { "Machine identifier does not match. This terminal may have been moved to a different machine." }
                };
            }
        }

        // Validate against database
        var dbTerminal = await _terminalService.GetTerminalByIdAsync(config.Terminal.Id, cancellationToken).ConfigureAwait(false);

        if (dbTerminal is null)
        {
            return TerminalValidationResult.Failure("Terminal not found in database.");
        }

        if (!dbTerminal.IsActive)
        {
            return new TerminalValidationResult
            {
                IsValid = false,
                MachineIdentifierMatches = true,
                IsActive = false,
                Errors = new List<string> { "Terminal has been deactivated." }
            };
        }

        // Check if machine ID in DB matches
        if (!string.Equals(dbTerminal.MachineIdentifier, config.Terminal.MachineIdentifier, StringComparison.OrdinalIgnoreCase))
        {
            return new TerminalValidationResult
            {
                IsValid = false,
                MachineIdentifierMatches = false,
                IsActive = true,
                Errors = new List<string> { "Terminal has been re-bound to a different machine." }
            };
        }

        return TerminalValidationResult.Success();
    }

    /// <inheritdoc />
    public bool IsTerminalConfigured()
    {
        var config = GetLocalConfiguration();
        return config is not null && config.Terminal.Id > 0;
    }

    /// <inheritdoc />
    public int? GetCurrentTerminalId()
    {
        var config = GetLocalConfiguration();
        return config?.Terminal.Id;
    }

    /// <inheritdoc />
    public string? GetCurrentTerminalCode()
    {
        var config = GetLocalConfiguration();
        return config?.Terminal.Code;
    }

    /// <inheritdoc />
    public string GetConfigurationFilePath()
    {
        // Use AppData\Local for user-specific config (preferred)
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appDataPath, AppFolderName, ConfigFileName);
    }

    /// <inheritdoc />
    public async Task DeleteLocalConfigurationAsync(
        CancellationToken cancellationToken = default)
    {
        var filePath = GetConfigurationFilePath();

        if (File.Exists(filePath))
        {
            // Create backup before deletion
            var backupPath = filePath + ".bak";
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }
            File.Move(filePath, backupPath);

            _logger.Information("Terminal configuration deleted (backed up to {BackupPath})", backupPath);
        }

        lock (_configLock)
        {
            _cachedConfiguration = null;
            _configurationLoaded = false;
        }

        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<bool> SyncWithDatabaseAsync(
        CancellationToken cancellationToken = default)
    {
        var config = GetLocalConfiguration();

        if (config is null)
        {
            _logger.Warning("Cannot sync - no local configuration found");
            return false;
        }

        try
        {
            var dbTerminal = await _terminalService.GetTerminalByIdAsync(config.Terminal.Id, cancellationToken).ConfigureAwait(false);

            if (dbTerminal is null)
            {
                _logger.Warning("Cannot sync - terminal {TerminalId} not found in database", config.Terminal.Id);
                return false;
            }

            // Update local config from database
            config.Terminal.Code = dbTerminal.Code;
            config.Terminal.Name = dbTerminal.Name;
            config.Terminal.Type = dbTerminal.TerminalType;
            config.Terminal.BusinessMode = dbTerminal.BusinessMode;
            config.Terminal.IsMainRegister = dbTerminal.IsMainRegister;
            config.Terminal.StoreName = dbTerminal.Store?.Name ?? config.Terminal.StoreName;
            config.LastSync = DateTime.UtcNow;

            await SaveLocalConfigurationAsync(config, cancellationToken).ConfigureAwait(false);

            _logger.Information("Terminal configuration synced with database");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to sync terminal configuration with database");
            return false;
        }
    }

    /// <inheritdoc />
    public void ReloadConfiguration()
    {
        lock (_configLock)
        {
            _configurationLoaded = false;
            _cachedConfiguration = null;
        }

        LoadConfiguration();
    }

    /// <inheritdoc />
    public async Task UpdateLastSyncAsync(
        CancellationToken cancellationToken = default)
    {
        var config = GetLocalConfiguration();

        if (config is not null)
        {
            config.LastSync = DateTime.UtcNow;
            await SaveLocalConfigurationAsync(config, cancellationToken).ConfigureAwait(false);
        }
    }

    private void LoadConfiguration()
    {
        lock (_configLock)
        {
            _configurationLoaded = true;
            _cachedConfiguration = null;

            var filePath = GetConfigurationFilePath();

            if (!File.Exists(filePath))
            {
                _logger.Debug("Terminal configuration file not found at {FilePath}", filePath);
                return;
            }

            try
            {
                var json = File.ReadAllText(filePath);
                _cachedConfiguration = JsonSerializer.Deserialize<TerminalConfiguration>(json, JsonOptions);

                if (_cachedConfiguration is not null)
                {
                    _logger.Information(
                        "Terminal configuration loaded: {Code} ({Name}) - Store: {StoreName}",
                        _cachedConfiguration.Terminal.Code,
                        _cachedConfiguration.Terminal.Name,
                        _cachedConfiguration.Terminal.StoreName);
                }
            }
            catch (JsonException ex)
            {
                _logger.Error(ex, "Failed to parse terminal configuration file at {FilePath}", filePath);

                // Backup corrupted file
                var backupPath = filePath + ".corrupted";
                try
                {
                    if (File.Exists(backupPath))
                    {
                        File.Delete(backupPath);
                    }
                    File.Move(filePath, backupPath);
                    _logger.Warning("Corrupted configuration backed up to {BackupPath}", backupPath);
                }
                catch (Exception backupEx)
                {
                    _logger.Error(backupEx, "Failed to backup corrupted configuration file");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to load terminal configuration from {FilePath}", filePath);
            }
        }
    }
}
