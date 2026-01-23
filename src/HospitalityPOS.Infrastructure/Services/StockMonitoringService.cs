using Microsoft.EntityFrameworkCore;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for stock monitoring and automatic reorder generation.
/// </summary>
public class StockMonitoringService : IStockMonitoringService
{
    private readonly POSDbContext _context;
    private readonly IInventoryAnalyticsService _analyticsService;
    private readonly IPurchaseOrderSettingsService _settingsService;
    private readonly IPurchaseOrderConsolidationService _consolidationService;
    private readonly INotificationService _notificationService;
    private readonly ILogger _logger;

    private bool _isRunning;
    private int _consecutiveFailures;

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public StockMonitoringResult? LastResult { get; private set; }

    public StockMonitoringService(
        POSDbContext context,
        IInventoryAnalyticsService analyticsService,
        IPurchaseOrderSettingsService settingsService,
        IPurchaseOrderConsolidationService consolidationService,
        INotificationService notificationService,
        ILogger logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _consolidationService = consolidationService ?? throw new ArgumentNullException(nameof(consolidationService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<StockMonitoringResult> RunStockMonitoringAsync(int? storeId = null, CancellationToken cancellationToken = default)
    {
        if (_isRunning)
        {
            return new StockMonitoringResult
            {
                Success = false,
                ErrorMessage = "Stock monitoring is already running."
            };
        }

        _isRunning = true;
        var startTime = DateTime.UtcNow;
        var result = new StockMonitoringResult
        {
            StartedAt = startTime
        };

        try
        {
            _logger.Information("Starting stock monitoring run for store {StoreId}", storeId ?? 0);

            // Check if we should run
            if (!await _settingsService.ShouldRunStockMonitoringAsync(storeId, cancellationToken).ConfigureAwait(false))
            {
                result.Success = true;
                result.ErrorMessage = "Stock monitoring not scheduled to run at this time.";
                return result;
            }

            // Get all stores to process
            var stores = storeId.HasValue
                ? new List<int> { storeId.Value }
                : await _context.Stores
                    .Where(s => s.IsActive)
                    .Select(s => s.Id)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

            foreach (var currentStoreId in stores)
            {
                await ProcessStoreAsync(currentStoreId, result, cancellationToken).ConfigureAwait(false);
            }

            // Update last check time
            await _settingsService.UpdateLastStockCheckTimeAsync(storeId, cancellationToken).ConfigureAwait(false);

            result.Success = true;
            _consecutiveFailures = 0;

            _logger.Information(
                "Stock monitoring completed. Suggestions: {Suggestions}, POs: {POs}, Notifications: {Notifications}",
                result.SuggestionsGenerated,
                result.PurchaseOrdersCreated,
                result.NotificationsSent);
        }
        catch (Exception ex)
        {
            _consecutiveFailures++;
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.Error(ex, "Stock monitoring failed with error: {Error}", ex.Message);
        }
        finally
        {
            result.CompletedAt = DateTime.UtcNow;
            result.Duration = result.CompletedAt - result.StartedAt;
            _isRunning = false;
            LastResult = result;
        }

        return result;
    }

    private async Task ProcessStoreAsync(int storeId, StockMonitoringResult result, CancellationToken cancellationToken)
    {
        var settings = await _settingsService.GetEffectiveSettingsAsync(storeId, cancellationToken).ConfigureAwait(false);

        // Generate reorder suggestions
        var suggestions = await _analyticsService.GenerateReorderSuggestionsAsync(storeId, cancellationToken).ConfigureAwait(false);
        var suggestionList = suggestions.ToList();
        result.SuggestionsGenerated += suggestionList.Count;

        if (!suggestionList.Any())
        {
            _logger.Debug("No reorder suggestions generated for store {StoreId}", storeId);
            return;
        }

        // Send low stock notifications
        if (settings.NotifyOnLowStock)
        {
            var criticalItems = suggestionList.Where(s => s.Priority == "Critical" || s.Priority == "High").ToList();
            foreach (var item in criticalItems.Take(10)) // Limit to 10 notifications
            {
                await _notificationService.NotifyLowStockAsync(
                    item.ProductId,
                    item.Product?.Name ?? "Unknown",
                    item.CurrentStock,
                    item.ReorderPoint,
                    cancellationToken).ConfigureAwait(false);
                result.NotificationsSent++;
            }
        }

        // Auto-approve suggestions if threshold allows
        foreach (var suggestion in suggestionList)
        {
            if (suggestion.EstimatedCost <= settings.AutoApprovalThreshold && !settings.RequireManagerApproval)
            {
                suggestion.Status = "Approved";
                suggestion.ApprovedAt = DateTime.UtcNow;
                suggestion.Notes = "Auto-approved (within threshold)";
            }
            else
            {
                suggestion.Status = "Pending";
                suggestion.Notes = "Awaiting manager approval";

                // Send pending approval notification
                if (settings.NotifyOnPOGenerated)
                {
                    await _notificationService.NotifyPOPendingApprovalAsync(
                        0, // No PO yet
                        $"Suggestion for {suggestion.Product?.Name}",
                        suggestion.EstimatedCost,
                        cancellationToken).ConfigureAwait(false);
                    result.NotificationsSent++;
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Create consolidated POs if auto-generation is enabled
        if (settings.AutoGeneratePurchaseOrders)
        {
            var approvedSuggestionIds = suggestionList
                .Where(s => s.Status == "Approved")
                .Select(s => s.Id)
                .ToList();

            if (approvedSuggestionIds.Any())
            {
                var consolidationResult = await _consolidationService.CreateConsolidatedPurchaseOrdersAsync(
                    storeId,
                    approvedSuggestionIds,
                    settings.AutoSendPurchaseOrders,
                    cancellationToken).ConfigureAwait(false);

                result.PurchaseOrdersCreated += consolidationResult.PurchaseOrdersCreated;
            }
        }
    }

    /// <inheritdoc />
    public async Task<StockMonitoringResult> TriggerManualRunAsync(int? storeId = null, CancellationToken cancellationToken = default)
    {
        _logger.Information("Manual stock monitoring run triggered for store {StoreId}", storeId ?? 0);
        return await RunStockMonitoringAsync(storeId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<StockMonitoringStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.GetSettingsAsync(null, cancellationToken).ConfigureAwait(false);
        var checkInterval = settings?.StockCheckIntervalMinutes ?? 15;
        var lastRunTime = settings?.LastStockCheckTime;

        return new StockMonitoringStatus
        {
            IsEnabled = settings?.AutoGeneratePurchaseOrders ?? false,
            IsRunning = _isRunning,
            LastRunTime = lastRunTime,
            NextRunTime = lastRunTime?.AddMinutes(checkInterval) ?? DateTime.UtcNow.AddMinutes(checkInterval),
            CheckIntervalMinutes = checkInterval,
            ConsecutiveFailures = _consecutiveFailures,
            LastErrorMessage = LastResult?.ErrorMessage
        };
    }
}
