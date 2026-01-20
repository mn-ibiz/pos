using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing system configuration and business mode.
/// </summary>
public class SystemConfigurationService : ISystemConfigurationService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SystemConfigurationService> _logger;
    private SystemConfiguration? _cachedConfiguration;
    private readonly SemaphoreSlim _cacheLock = new(1, 1);
    private bool _disposed;

    /// <inheritdoc />
    public event EventHandler<SystemConfigurationChangedEventArgs>? ConfigurationChanged;

    /// <inheritdoc />
    public SystemConfiguration? CachedConfiguration => _cachedConfiguration;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemConfigurationService"/> class.
    /// </summary>
    public SystemConfigurationService(
        IServiceScopeFactory scopeFactory,
        ILogger<SystemConfigurationService> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<SystemConfiguration?> GetConfigurationAsync()
    {
        if (_cachedConfiguration != null)
        {
            return _cachedConfiguration;
        }

        await RefreshCacheAsync();
        return _cachedConfiguration;
    }

    /// <inheritdoc />
    public async Task<BusinessMode> GetCurrentModeAsync()
    {
        var config = await GetConfigurationAsync();
        return config?.Mode ?? BusinessMode.Restaurant;
    }

    /// <inheritdoc />
    public async Task<bool> IsSetupCompleteAsync()
    {
        try
        {
            var config = await GetConfigurationAsync();
            var result = config?.SetupCompleted ?? false;
            _logger.LogInformation("IsSetupCompleteAsync: Config={Config}, SetupCompleted={Result}",
                config != null ? "Found" : "Null", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if setup is complete");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SaveConfigurationAsync(SystemConfiguration configuration)
    {
        await _cacheLock.WaitAsync();
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<POSDbContext>();

            var existing = await context.SystemConfigurations.FirstOrDefaultAsync();

            if (existing != null)
            {
                // Track changes for the event
                var previousMode = existing.Mode;
                var changedFeatures = GetChangedFeatures(existing, configuration);

                // Preserve key fields before SetValues overwrites them
                var existingId = existing.Id;
                var existingCreatedAt = existing.CreatedAt;

                // Update existing configuration
                context.Entry(existing).CurrentValues.SetValues(configuration);
                existing.Id = existingId; // Restore preserved ID
                existing.CreatedAt = existingCreatedAt; // Restore preserved CreatedAt
                existing.UpdatedAt = DateTime.UtcNow;

                await context.SaveChangesAsync();

                _cachedConfiguration = existing;

                // Raise event if mode or features changed
                if (previousMode != configuration.Mode || changedFeatures.Count > 0)
                {
                    OnConfigurationChanged(new SystemConfigurationChangedEventArgs
                    {
                        PreviousMode = previousMode,
                        NewMode = configuration.Mode,
                        ChangedFeatures = changedFeatures,
                        RequiresRestart = previousMode != configuration.Mode
                    });
                }
            }
            else
            {
                configuration.CreatedAt = DateTime.UtcNow;
                context.SystemConfigurations.Add(configuration);
                await context.SaveChangesAsync();
                _cachedConfiguration = configuration;
            }

            _logger.LogInformation(
                "System configuration saved. Mode: {Mode}, SetupComplete: {SetupComplete}",
                configuration.Mode, configuration.SetupCompleted);

            return true;
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Database update error saving system configuration: {Message}", dbEx.InnerException?.Message ?? dbEx.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving system configuration: {Message}", ex.Message);
            return false;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<SystemConfiguration> CompleteSetupAsync(
        BusinessMode mode,
        string businessName,
        string? businessAddress = null,
        string? businessPhone = null)
    {
        // Check if configuration already exists
        var existing = await GetConfigurationAsync();

        SystemConfiguration configuration;
        if (existing != null)
        {
            // Update existing configuration
            configuration = existing;
            configuration.Mode = mode;
            configuration.BusinessName = businessName;
            configuration.BusinessAddress = businessAddress;
            configuration.BusinessPhone = businessPhone;
            configuration.SetupCompleted = true;
            configuration.SetupCompletedAt = DateTime.UtcNow;
            // Apply mode defaults
            configuration.ApplyModeDefaults();
        }
        else
        {
            // Create new configuration
            configuration = new SystemConfiguration
            {
                Mode = mode,
                BusinessName = businessName,
                BusinessAddress = businessAddress,
                BusinessPhone = businessPhone,
                SetupCompleted = true,
                SetupCompletedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            // Apply mode defaults
            configuration.ApplyModeDefaults();
        }

        var saved = await SaveConfigurationAsync(configuration);
        if (!saved)
        {
            _logger.LogError("Failed to save system configuration during setup");
            throw new InvalidOperationException("Failed to save system configuration. Please check database connection and try again.");
        }

        _logger.LogInformation(
            "Setup completed. Business: {BusinessName}, Mode: {Mode}",
            businessName, mode);

        return configuration;
    }

    /// <inheritdoc />
    public async Task<bool> ChangeModeAsync(BusinessMode newMode, bool applyDefaults = true)
    {
        var config = await GetConfigurationAsync();
        if (config == null)
        {
            _logger.LogWarning("Cannot change mode - no configuration exists");
            return false;
        }

        var previousMode = config.Mode;
        config.Mode = newMode;

        if (applyDefaults)
        {
            config.ApplyModeDefaults();
        }

        var success = await SaveConfigurationAsync(config);

        if (success)
        {
            _logger.LogInformation(
                "Business mode changed from {PreviousMode} to {NewMode}",
                previousMode, newMode);
        }

        return success;
    }

    /// <inheritdoc />
    public async Task<bool> IsFeatureEnabledAsync(string featureName)
    {
        var config = await GetConfigurationAsync();
        if (config == null)
        {
            return false;
        }

        return GetFeatureValue(config, featureName);
    }

    /// <inheritdoc />
    public async Task<bool> SetFeatureEnabledAsync(string featureName, bool enabled)
    {
        var config = await GetConfigurationAsync();
        if (config == null)
        {
            return false;
        }

        if (!SetFeatureValue(config, featureName, enabled))
        {
            return false;
        }

        return await SaveConfigurationAsync(config);
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, bool>> GetAllFeaturesAsync()
    {
        var config = await GetConfigurationAsync();
        if (config == null)
        {
            return new Dictionary<string, bool>();
        }

        return new Dictionary<string, bool>
        {
            // Restaurant features
            [FeatureFlags.TableManagement] = config.EnableTableManagement,
            [FeatureFlags.KitchenDisplay] = config.EnableKitchenDisplay,
            [FeatureFlags.WaiterAssignment] = config.EnableWaiterAssignment,
            [FeatureFlags.CourseSequencing] = config.EnableCourseSequencing,
            [FeatureFlags.Reservations] = config.EnableReservations,

            // Retail features
            [FeatureFlags.BarcodeAutoFocus] = config.EnableBarcodeAutoFocus,
            [FeatureFlags.ProductOffers] = config.EnableProductOffers,
            [FeatureFlags.SupplierCredit] = config.EnableSupplierCredit,
            [FeatureFlags.LoyaltyProgram] = config.EnableLoyaltyProgram,
            [FeatureFlags.BatchExpiry] = config.EnableBatchExpiry,
            [FeatureFlags.ScaleIntegration] = config.EnableScaleIntegration,

            // Enterprise features
            [FeatureFlags.Payroll] = config.EnablePayroll,
            [FeatureFlags.Accounting] = config.EnableAccounting,
            [FeatureFlags.MultiStore] = config.EnableMultiStore,
            [FeatureFlags.CloudSync] = config.EnableCloudSync,

            // Kenya features
            [FeatureFlags.KenyaETims] = config.EnableKenyaETims,
            [FeatureFlags.Mpesa] = config.EnableMpesa
        };
    }

    /// <inheritdoc />
    public async Task RefreshCacheAsync()
    {
        await _cacheLock.WaitAsync();
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<POSDbContext>();

            _cachedConfiguration = await context.SystemConfigurations.FirstOrDefaultAsync();

            if (_cachedConfiguration != null)
            {
                _logger.LogInformation(
                    "System configuration loaded: Mode={Mode}, Business={BusinessName}, SetupCompleted={SetupCompleted}",
                    _cachedConfiguration.Mode, _cachedConfiguration.BusinessName, _cachedConfiguration.SetupCompleted);
            }
            else
            {
                _logger.LogWarning("No system configuration found in database");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing system configuration cache: {Message}", ex.Message);
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    private static bool GetFeatureValue(SystemConfiguration config, string featureName)
    {
        var property = typeof(SystemConfiguration).GetProperty(featureName);
        if (property != null && property.PropertyType == typeof(bool))
        {
            return (bool)(property.GetValue(config) ?? false);
        }

        return false;
    }

    private static bool SetFeatureValue(SystemConfiguration config, string featureName, bool value)
    {
        var property = typeof(SystemConfiguration).GetProperty(featureName);
        if (property != null && property.PropertyType == typeof(bool) && property.CanWrite)
        {
            property.SetValue(config, value);
            return true;
        }

        return false;
    }

    private static List<string> GetChangedFeatures(SystemConfiguration existing, SystemConfiguration updated)
    {
        var changedFeatures = new List<string>();

        var boolProperties = typeof(SystemConfiguration)
            .GetProperties()
            .Where(p => p.PropertyType == typeof(bool) && p.Name.StartsWith("Enable"));

        foreach (var prop in boolProperties)
        {
            var oldValue = (bool)(prop.GetValue(existing) ?? false);
            var newValue = (bool)(prop.GetValue(updated) ?? false);

            if (oldValue != newValue)
            {
                changedFeatures.Add(prop.Name);
            }
        }

        return changedFeatures;
    }

    /// <summary>
    /// Raises the ConfigurationChanged event.
    /// </summary>
    protected virtual void OnConfigurationChanged(SystemConfigurationChangedEventArgs e)
    {
        ConfigurationChanged?.Invoke(this, e);
    }

    /// <summary>
    /// Throws if this instance has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    /// <summary>
    /// Releases the resources used by this instance.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the resources used by this instance.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _cacheLock.Dispose();
        }

        _disposed = true;
    }
}
