using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.BackgroundJobs;

/// <summary>
/// Background job that releases expired stock reservations back to available inventory.
/// Runs every hour by default.
/// </summary>
public class ExpireReservationsJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExpireReservationsJob> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

    public ExpireReservationsJob(
        IServiceProvider serviceProvider,
        ILogger<ExpireReservationsJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ExpireReservationsJob started with {Interval} check interval", _checkInterval);

        // Small initial delay to let application fully start
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredReservationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing expired reservations");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task ProcessExpiredReservationsAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Checking for expired reservations at {Time}", DateTime.UtcNow);

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<POSDbContext>();

        var now = DateTime.UtcNow;

        // Find reservations that have expired but are still active
        var expiredReservations = await dbContext.Set<StockReservation>()
            .Where(r => r.ExpiresAt < now && r.Status == ReservationStatus.Active)
            .Include(r => r.Product)
            .Include(r => r.Location)
            .ToListAsync(cancellationToken);

        if (expiredReservations.Count == 0)
        {
            _logger.LogDebug("No expired reservations found");
            return;
        }

        _logger.LogInformation("Found {Count} expired reservations to release", expiredReservations.Count);

        var releasedCount = 0;
        var totalReleasedQuantity = 0;

        foreach (var reservation in expiredReservations)
        {
            try
            {
                // Update reservation status
                reservation.Status = ReservationStatus.Expired;
                reservation.CompletedAt = DateTime.UtcNow;

                // Release the reserved stock back to available
                // Note: For batch-tracked products, we need to update the batch as well
                await ReleaseReservedStockAsync(dbContext, reservation, cancellationToken);

                releasedCount++;
                totalReleasedQuantity += reservation.ReservedQuantity;

                _logger.LogInformation(
                    "Released expired reservation: Product={ProductName}, Location={LocationId}, Qty={Quantity}, Type={Type}, Ref={RefId}",
                    reservation.Product?.Name ?? "Unknown",
                    reservation.LocationId,
                    reservation.ReservedQuantity,
                    reservation.ReferenceType,
                    reservation.ReferenceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error releasing reservation {ReservationId}", reservation.Id);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Expired reservations processing complete. Released {Count} reservations, {TotalQty} total units",
            releasedCount,
            totalReleasedQuantity);
    }

    private async Task ReleaseReservedStockAsync(
        POSDbContext dbContext,
        StockReservation reservation,
        CancellationToken cancellationToken)
    {
        // If this is a transfer reservation, update the batch reserved quantity
        if (reservation.ReferenceType == ReservationType.Transfer)
        {
            // Find any batches that have reserved quantity for this product
            var batches = await dbContext.Set<ProductBatch>()
                .Where(b => b.ProductId == reservation.ProductId &&
                           b.StoreId == reservation.LocationId &&
                           b.ReservedQuantity > 0 &&
                           b.Status == BatchStatus.Active)
                .OrderBy(b => b.ExpiryDate) // FEFO - release from earliest expiry first
                .ThenBy(b => b.ReceivedAt)  // FIFO
                .ToListAsync(cancellationToken);

            var remainingToRelease = reservation.ReservedQuantity;

            foreach (var batch in batches)
            {
                if (remainingToRelease <= 0) break;

                var releaseFromBatch = Math.Min(batch.ReservedQuantity, remainingToRelease);
                batch.ReservedQuantity -= releaseFromBatch;
                remainingToRelease -= releaseFromBatch;

                // Log the stock movement
                var movement = new BatchStockMovement
                {
                    BatchId = batch.Id,
                    ProductId = batch.ProductId,
                    StoreId = batch.StoreId,
                    MovementType = BatchMovementType.Released,
                    Quantity = releaseFromBatch,
                    QuantityBefore = batch.CurrentQuantity, // Reserved qty was already part of current
                    QuantityAfter = batch.CurrentQuantity,
                    ReferenceType = "ReservationExpiry",
                    ReferenceId = reservation.Id,
                    ReferenceNumber = $"RESEXP-{reservation.Id}",
                    MovedAt = DateTime.UtcNow,
                    Notes = $"Reserved stock released due to reservation expiry. Original ref: {reservation.ReferenceType}-{reservation.ReferenceId}"
                };

                dbContext.Set<BatchStockMovement>().Add(movement);
            }

            if (remainingToRelease > 0)
            {
                _logger.LogWarning(
                    "Could not find enough reserved batch stock to release. Reservation {Id}, Remaining: {Remaining}",
                    reservation.Id,
                    remainingToRelease);
            }
        }

        // Also update the Inventory table if it exists
        var inventory = await dbContext.Set<Inventory>()
            .FirstOrDefaultAsync(i =>
                i.ProductId == reservation.ProductId &&
                i.StoreId == reservation.LocationId,
                cancellationToken);

        if (inventory != null)
        {
            // Reserved quantity goes back to available
            // Note: The actual CurrentStock doesn't change, only the reserved portion
            _logger.LogDebug(
                "Inventory for product {ProductId} at location {LocationId}: Current={Current}",
                reservation.ProductId,
                reservation.LocationId,
                inventory.CurrentStock);
        }
    }
}
