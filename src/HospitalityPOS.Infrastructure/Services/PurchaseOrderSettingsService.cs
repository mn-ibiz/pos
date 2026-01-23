using Microsoft.EntityFrameworkCore;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing purchase order settings.
/// </summary>
public class PurchaseOrderSettingsService : IPurchaseOrderSettingsService
{
    private readonly POSDbContext _context;
    private PurchaseOrderSettings? _cachedSettings;
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    public PurchaseOrderSettingsService(POSDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task<PurchaseOrderSettings?> GetSettingsAsync(int? storeId = null, CancellationToken cancellationToken = default)
    {
        return await _context.PurchaseOrderSettings
            .FirstOrDefaultAsync(s => s.StoreId == storeId && !s.IsDeleted, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PurchaseOrderSettings> GetEffectiveSettingsAsync(int storeId, CancellationToken cancellationToken = default)
    {
        // Try to get cached settings first
        await _cacheLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_cachedSettings != null)
            {
                return _cachedSettings;
            }

            // Try store-specific settings first
            var settings = await GetSettingsAsync(storeId, cancellationToken).ConfigureAwait(false);

            // Fall back to global settings
            if (settings == null)
            {
                settings = await GetSettingsAsync(null, cancellationToken).ConfigureAwait(false);
            }

            // Create default settings if none exist
            if (settings == null)
            {
                settings = new PurchaseOrderSettings
                {
                    StoreId = null,
                    AutoGeneratePurchaseOrders = false,
                    AutoSendPurchaseOrders = false,
                    RequireManagerApproval = true,
                    StockCheckIntervalMinutes = 15,
                    NotifyOnLowStock = true,
                    NotifyOnPOGenerated = true,
                    ConsolidatePOsBySupplier = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.PurchaseOrderSettings.Add(settings);
                await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            _cachedSettings = settings;
            return settings;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<PurchaseOrderSettings> SaveSettingsAsync(PurchaseOrderSettings settings, int userId, CancellationToken cancellationToken = default)
    {
        await _cacheLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (settings.Id == 0)
            {
                settings.CreatedAt = DateTime.UtcNow;
                settings.UpdatedByUserId = userId;
                _context.PurchaseOrderSettings.Add(settings);
            }
            else
            {
                var existing = await _context.PurchaseOrderSettings
                    .FirstOrDefaultAsync(s => s.Id == settings.Id && !s.IsDeleted, cancellationToken)
                    .ConfigureAwait(false);

                if (existing == null)
                {
                    throw new InvalidOperationException($"Purchase order settings {settings.Id} not found.");
                }

                // Update all properties
                existing.AutoGeneratePurchaseOrders = settings.AutoGeneratePurchaseOrders;
                existing.AutoSendPurchaseOrders = settings.AutoSendPurchaseOrders;
                existing.AutoApprovalThreshold = settings.AutoApprovalThreshold;
                existing.RequireManagerApproval = settings.RequireManagerApproval;
                existing.StockCheckIntervalMinutes = settings.StockCheckIntervalMinutes;
                existing.NotifyOnLowStock = settings.NotifyOnLowStock;
                existing.NotifyOnPOGenerated = settings.NotifyOnPOGenerated;
                existing.NotifyOnPOSent = settings.NotifyOnPOSent;
                existing.LowStockThresholdDays = settings.LowStockThresholdDays;
                existing.DefaultLeadTimeDays = settings.DefaultLeadTimeDays;
                existing.ConsolidatePOsBySupplier = settings.ConsolidatePOsBySupplier;
                existing.MinimumPOAmount = settings.MinimumPOAmount;
                existing.MaxItemsPerPO = settings.MaxItemsPerPO;
                existing.SendDailyPendingPODigest = settings.SendDailyPendingPODigest;
                existing.DailyDigestTime = settings.DailyDigestTime;
                existing.SendWeeklySummary = settings.SendWeeklySummary;
                existing.WeeklySummaryDay = settings.WeeklySummaryDay;
                existing.DigestRecipientEmails = settings.DigestRecipientEmails;
                existing.BusinessHoursStart = settings.BusinessHoursStart;
                existing.BusinessHoursEnd = settings.BusinessHoursEnd;
                existing.RunOnWeekends = settings.RunOnWeekends;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedByUserId = userId;

                settings = existing;
            }

            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // Invalidate cache
            _cachedSettings = null;

            return settings;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> ShouldAutoGeneratePOsAsync(int? storeId = null, CancellationToken cancellationToken = default)
    {
        var settings = storeId.HasValue
            ? await GetEffectiveSettingsAsync(storeId.Value, cancellationToken).ConfigureAwait(false)
            : await GetSettingsAsync(null, cancellationToken).ConfigureAwait(false);

        return settings?.AutoGeneratePurchaseOrders ?? false;
    }

    /// <inheritdoc />
    public async Task<bool> ShouldAutoSendPOsAsync(int? storeId = null, CancellationToken cancellationToken = default)
    {
        var settings = storeId.HasValue
            ? await GetEffectiveSettingsAsync(storeId.Value, cancellationToken).ConfigureAwait(false)
            : await GetSettingsAsync(null, cancellationToken).ConfigureAwait(false);

        return settings?.AutoSendPurchaseOrders ?? false;
    }

    /// <inheritdoc />
    public async Task<bool> RequiresApprovalAsync(decimal poValue, int? storeId = null, CancellationToken cancellationToken = default)
    {
        var settings = storeId.HasValue
            ? await GetEffectiveSettingsAsync(storeId.Value, cancellationToken).ConfigureAwait(false)
            : await GetSettingsAsync(null, cancellationToken).ConfigureAwait(false);

        if (settings == null)
        {
            return true; // Default to requiring approval
        }

        if (settings.RequireManagerApproval)
        {
            return true;
        }

        return poValue > settings.AutoApprovalThreshold;
    }

    /// <inheritdoc />
    public async Task<int> GetStockCheckIntervalAsync(int? storeId = null, CancellationToken cancellationToken = default)
    {
        var settings = storeId.HasValue
            ? await GetEffectiveSettingsAsync(storeId.Value, cancellationToken).ConfigureAwait(false)
            : await GetSettingsAsync(null, cancellationToken).ConfigureAwait(false);

        return settings?.StockCheckIntervalMinutes ?? 15;
    }

    /// <inheritdoc />
    public async Task UpdateLastStockCheckTimeAsync(int? storeId = null, CancellationToken cancellationToken = default)
    {
        var settings = storeId.HasValue
            ? await GetEffectiveSettingsAsync(storeId.Value, cancellationToken).ConfigureAwait(false)
            : await GetSettingsAsync(null, cancellationToken).ConfigureAwait(false);

        if (settings != null)
        {
            settings.LastStockCheckTime = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ShouldRunStockMonitoringAsync(int? storeId = null, CancellationToken cancellationToken = default)
    {
        var settings = storeId.HasValue
            ? await GetEffectiveSettingsAsync(storeId.Value, cancellationToken).ConfigureAwait(false)
            : await GetSettingsAsync(null, cancellationToken).ConfigureAwait(false);

        if (settings == null || !settings.AutoGeneratePurchaseOrders)
        {
            return false;
        }

        var now = DateTime.Now;

        // Check weekend restriction
        if (!settings.RunOnWeekends && (now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday))
        {
            return false;
        }

        // Check business hours
        var currentHour = now.Hour;
        if (currentHour < settings.BusinessHoursStart || currentHour >= settings.BusinessHoursEnd)
        {
            return false;
        }

        return true;
    }
}
