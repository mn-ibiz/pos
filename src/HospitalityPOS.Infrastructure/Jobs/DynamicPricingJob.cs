using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Jobs;

/// <summary>
/// Background job that refreshes dynamic prices based on active rules.
/// Runs at configurable intervals (default 15 minutes).
/// </summary>
public class DynamicPricingJob : BackgroundService, IDynamicPricingJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DynamicPricingJob> _logger;
    private DynamicPricingJobResult? _lastResult;

    public DynamicPricingJob(
        IServiceScopeFactory scopeFactory,
        ILogger<DynamicPricingJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Gets the last job run result.
    /// </summary>
    public DynamicPricingJobResult? LastResult => _lastResult;

    /// <summary>
    /// Triggers an immediate price refresh.
    /// </summary>
    public async Task TriggerRefreshAsync()
    {
        _logger.LogInformation("Manual dynamic pricing refresh triggered");
        await ProcessAsync();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DynamicPricingJob started");

        // Initial delay to let the application start up
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DynamicPricingJob execution");
            }

            // Get interval from configuration (default 15 minutes)
            var interval = await GetIntervalAsync();
            await Task.Delay(interval, stoppingToken);
        }

        _logger.LogInformation("DynamicPricingJob stopped");
    }

    private async Task ProcessAsync()
    {
        _lastResult = new DynamicPricingJobResult { StartTime = DateTime.UtcNow };

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<POSDbContext>>();
            var pricingService = scope.ServiceProvider.GetRequiredService<IDynamicPricingService>();

            await using var context = await contextFactory.CreateDbContextAsync();

            // Get all stores with dynamic pricing enabled
            var stores = await context.Set<Core.Entities.DynamicPricingConfiguration>()
                .Where(c => c.EnableDynamicPricing)
                .Select(c => c.StoreId)
                .ToListAsync();

            foreach (var storeId in stores)
            {
                try
                {
                    var result = await pricingService.RefreshAllPricesAsync(storeId);
                    _lastResult.ProductsEvaluated += result.ProductsEvaluated;
                    _lastResult.PricesUpdated += result.PricesUpdated;
                    _lastResult.RulesEvaluated += result.RulesEvaluated;
                    _lastResult.Errors.AddRange(result.Errors);

                    // Expire pending price changes
                    await pricingService.ExpirePendingChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error refreshing prices for store {StoreId}", storeId);
                    _lastResult.Errors.Add($"Store {storeId}: {ex.Message}");
                }
            }

            if (_lastResult.PricesUpdated > 0)
            {
                _logger.LogInformation(
                    "DynamicPricingJob completed: {Evaluated} evaluated, {Updated} updated in {Duration}ms",
                    _lastResult.ProductsEvaluated,
                    _lastResult.PricesUpdated,
                    _lastResult.DurationMs);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process dynamic pricing");
            _lastResult.Errors.Add(ex.Message);
        }

        _lastResult.EndTime = DateTime.UtcNow;
    }

    private async Task<TimeSpan> GetIntervalAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<POSDbContext>>();
            await using var context = await contextFactory.CreateDbContextAsync();

            // Get the minimum interval from all enabled stores
            var interval = await context.Set<Core.Entities.DynamicPricingConfiguration>()
                .Where(c => c.EnableDynamicPricing)
                .MinAsync(c => (int?)c.PriceUpdateIntervalMinutes);

            return TimeSpan.FromMinutes(interval ?? 15);
        }
        catch
        {
            return TimeSpan.FromMinutes(15); // Default
        }
    }
}

/// <summary>
/// Background job that applies expiry-based discounts.
/// Runs daily at midnight.
/// </summary>
public class ExpiryPricingJob : BackgroundService, IExpiryPricingJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpiryPricingJob> _logger;
    private ExpiryPricingJobResult? _lastResult;

    public ExpiryPricingJob(
        IServiceScopeFactory scopeFactory,
        ILogger<ExpiryPricingJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Gets the last job run result.
    /// </summary>
    public ExpiryPricingJobResult? LastResult => _lastResult;

    /// <summary>
    /// Triggers an immediate expiry pricing run.
    /// </summary>
    public async Task TriggerRunAsync()
    {
        _logger.LogInformation("Manual expiry pricing run triggered");
        await ProcessAsync();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ExpiryPricingJob started - runs daily at midnight");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Calculate time until next midnight
                var now = DateTime.Now;
                var nextMidnight = now.Date.AddDays(1);
                var delay = nextMidnight - now;

                _logger.LogDebug("ExpiryPricingJob waiting {Hours:F1} hours until next run", delay.TotalHours);
                await Task.Delay(delay, stoppingToken);

                await ProcessAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExpiryPricingJob execution");
                // Wait an hour before retrying on error
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        _logger.LogInformation("ExpiryPricingJob stopped");
    }

    private async Task ProcessAsync()
    {
        _lastResult = new ExpiryPricingJobResult { StartTime = DateTime.UtcNow };

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<POSDbContext>>();
            var pricingService = scope.ServiceProvider.GetRequiredService<IDynamicPricingService>();

            await using var context = await contextFactory.CreateDbContextAsync();

            // Get all stores with dynamic pricing enabled
            var stores = await context.Set<Core.Entities.DynamicPricingConfiguration>()
                .Where(c => c.EnableDynamicPricing)
                .Select(c => c.StoreId)
                .ToListAsync();

            foreach (var storeId in stores)
            {
                try
                {
                    var result = await pricingService.ApplyExpiryDiscountsAsync(storeId);
                    _lastResult.ProductsWithExpiringBatches += result.ProductsWithExpiringBatches;
                    _lastResult.PricesDiscounted += result.PricesDiscounted;
                    _lastResult.TotalDiscountValue += result.TotalDiscountValue;
                    _lastResult.Errors.AddRange(result.Errors);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error applying expiry discounts for store {StoreId}", storeId);
                    _lastResult.Errors.Add($"Store {storeId}: {ex.Message}");
                }
            }

            if (_lastResult.PricesDiscounted > 0)
            {
                _logger.LogInformation(
                    "ExpiryPricingJob completed: {Products} products with expiring batches, {Discounted} discounted, {Value:C} total discount",
                    _lastResult.ProductsWithExpiringBatches,
                    _lastResult.PricesDiscounted,
                    _lastResult.TotalDiscountValue);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process expiry pricing");
            _lastResult.Errors.Add(ex.Message);
        }

        _lastResult.EndTime = DateTime.UtcNow;
    }
}
