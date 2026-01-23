using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for managing purchase order settings.
/// </summary>
public interface IPurchaseOrderSettingsService
{
    /// <summary>
    /// Gets the purchase order settings for a store (or global if storeId is null).
    /// </summary>
    Task<PurchaseOrderSettings?> GetSettingsAsync(int? storeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the effective settings for a store (store-specific or global fallback).
    /// </summary>
    Task<PurchaseOrderSettings> GetEffectiveSettingsAsync(int storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves purchase order settings.
    /// </summary>
    Task<PurchaseOrderSettings> SaveSettingsAsync(PurchaseOrderSettings settings, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if auto-generation is enabled.
    /// </summary>
    Task<bool> ShouldAutoGeneratePOsAsync(int? storeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if auto-send is enabled.
    /// </summary>
    Task<bool> ShouldAutoSendPOsAsync(int? storeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a PO requires approval based on value.
    /// </summary>
    Task<bool> RequiresApprovalAsync(decimal poValue, int? storeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the stock check interval in minutes.
    /// </summary>
    Task<int> GetStockCheckIntervalAsync(int? storeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the last stock check time.
    /// </summary>
    Task UpdateLastStockCheckTimeAsync(int? storeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if stock monitoring should run now (based on business hours).
    /// </summary>
    Task<bool> ShouldRunStockMonitoringAsync(int? storeId = null, CancellationToken cancellationToken = default);
}
