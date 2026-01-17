using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.BackgroundJobs;

/// <summary>
/// Background job that automatically expires product batches when their expiry date passes.
/// Runs daily at midnight by default.
/// </summary>
public class ExpireBatchesJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExpireBatchesJob> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24);
    private readonly TimeSpan _initialDelay;

    public ExpireBatchesJob(
        IServiceProvider serviceProvider,
        ILogger<ExpireBatchesJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        // Calculate delay until next midnight
        var now = DateTime.Now;
        var nextMidnight = now.Date.AddDays(1);
        _initialDelay = nextMidnight - now;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ExpireBatchesJob started. First run in {Delay}", _initialDelay);

        // Wait until midnight for first run
        await Task.Delay(_initialDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredBatchesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing expired batches");
            }

            // Wait for next check interval
            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task ProcessExpiredBatchesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting expired batch processing at {Time}", DateTime.UtcNow);

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<POSDbContext>();

        var today = DateTime.UtcNow.Date;

        // Find batches that are expired but still marked as Active
        var expiredBatches = await dbContext.Set<ProductBatch>()
            .Where(b => b.ExpiryDate != null &&
                       b.ExpiryDate < today &&
                       b.Status == BatchStatus.Active &&
                       b.CurrentQuantity > 0)
            .Include(b => b.Product)
            .Include(b => b.Store)
            .ToListAsync(cancellationToken);

        if (expiredBatches.Count == 0)
        {
            _logger.LogInformation("No expired batches found");
            return;
        }

        _logger.LogInformation("Found {Count} expired batches to process", expiredBatches.Count);

        var expiredCount = 0;
        var notificationBatches = new List<ProductBatch>();

        foreach (var batch in expiredBatches)
        {
            try
            {
                batch.Status = BatchStatus.Expired;

                // Log the stock movement for the expiration
                var movement = new BatchStockMovement
                {
                    BatchId = batch.Id,
                    ProductId = batch.ProductId,
                    StoreId = batch.StoreId,
                    MovementType = BatchMovementType.Adjustment,
                    Quantity = 0, // Status change only, no quantity change
                    QuantityBefore = batch.CurrentQuantity,
                    QuantityAfter = batch.CurrentQuantity,
                    ReferenceType = "BatchExpiry",
                    ReferenceId = batch.Id,
                    ReferenceNumber = $"EXP-{batch.BatchNumber}",
                    MovedAt = DateTime.UtcNow,
                    Notes = $"Batch automatically marked as expired. Expiry date: {batch.ExpiryDate:yyyy-MM-dd}"
                };

                dbContext.Set<BatchStockMovement>().Add(movement);

                expiredCount++;
                notificationBatches.Add(batch);

                _logger.LogInformation(
                    "Batch {BatchNumber} for product '{ProductName}' at store {StoreId} marked as expired. Qty: {Quantity}",
                    batch.BatchNumber,
                    batch.Product?.Name ?? "Unknown",
                    batch.StoreId,
                    batch.CurrentQuantity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error expiring batch {BatchId}", batch.Id);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Expired batch processing complete. Processed {Count} batches",
            expiredCount);

        // Send notifications to managers (if notification service is available)
        if (notificationBatches.Count > 0)
        {
            await SendExpiryNotificationsAsync(scope.ServiceProvider, notificationBatches, cancellationToken);
        }
    }

    private async Task SendExpiryNotificationsAsync(
        IServiceProvider serviceProvider,
        List<ProductBatch> expiredBatches,
        CancellationToken cancellationToken)
    {
        try
        {
            // Group by store for notification
            var batchesByStore = expiredBatches
                .GroupBy(b => b.StoreId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var (storeId, batches) in batchesByStore)
            {
                var message = $"Expiry Alert: {batches.Count} batch(es) have expired at store {storeId}.\n";
                message += string.Join("\n", batches.Select(b =>
                    $"- {b.Product?.Name ?? "Unknown"}: Batch {b.BatchNumber}, Qty: {b.CurrentQuantity}"));

                _logger.LogWarning(message);

                // TODO: Integrate with notification service when available
                // var notificationService = serviceProvider.GetService<INotificationService>();
                // await notificationService?.SendManagerNotificationAsync(storeId, "Batch Expiry Alert", message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending expiry notifications");
        }

        await Task.CompletedTask;
    }
}
