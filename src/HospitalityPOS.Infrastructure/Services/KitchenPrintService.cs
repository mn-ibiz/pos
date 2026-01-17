using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for printing Kitchen Order Tickets (KOT).
/// Currently a stub implementation that logs print requests.
/// </summary>
public class KitchenPrintService : IKitchenPrintService
{
    private readonly ILogger _logger;

    /// <summary>
    /// Standard kitchen stations.
    /// </summary>
    private static readonly string[] StandardStations =
    [
        "KITCHEN",
        "BAR",
        "COLD STATION",
        "PASTRY",
        "GENERAL"
    ];

    /// <summary>
    /// Initializes a new instance of the <see cref="KitchenPrintService"/> class.
    /// </summary>
    public KitchenPrintService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<bool> PrintKotAsync(Order order, IEnumerable<OrderItem> items)
    {
        var itemsList = items.ToList();
        if (!itemsList.Any())
        {
            _logger.Warning("No items to print for order {OrderNumber}", order.OrderNumber);
            return false;
        }

        _logger.Information(
            "Printing KOT for order {OrderNumber}: {ItemCount} items, Table: {Table}, Server: {Server}",
            order.OrderNumber,
            itemsList.Count,
            order.TableNumber ?? "N/A",
            order.User?.FullName ?? "Unknown");

        // Group items by station for logging
        var groupedItems = itemsList.GroupBy(i =>
            string.IsNullOrEmpty(i.Product?.KitchenStation) ? "GENERAL" : i.Product.KitchenStation);
        foreach (var group in groupedItems)
        {
            _logger.Debug("  Station [{Station}]:", group.Key);
            foreach (var item in group)
            {
                var productName = item.Product?.Name ?? $"Product #{item.ProductId}";
                var modifiers = !string.IsNullOrEmpty(item.Modifiers) ? $" - {item.Modifiers}" : "";
                var notes = !string.IsNullOrEmpty(item.Notes) ? $" - {item.Notes}" : "";
                _logger.Debug("    {Qty}x {Product}{Modifiers}{Notes}",
                    item.Quantity, productName, modifiers, notes);
            }
        }

        // Simulate print delay
        await Task.Delay(100);

        // TODO: Implement actual ESC/POS printing
        // For now, just return success
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> PrintAdditionKotAsync(Order order, IEnumerable<OrderItem> newItems)
    {
        var itemsList = newItems.ToList();
        if (!itemsList.Any())
        {
            return false;
        }

        _logger.Information(
            "Printing ADDITION KOT for order {OrderNumber}: {ItemCount} new items",
            order.OrderNumber,
            itemsList.Count);

        return await PrintKotAsync(order, itemsList);
    }

    /// <inheritdoc />
    public Task<bool> IsPrinterReadyAsync()
    {
        // TODO: Check actual printer status
        // For now, always return true
        _logger.Debug("Checking kitchen printer status (stub: always ready)");
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public IEnumerable<string> GetKitchenStations()
    {
        return StandardStations;
    }
}
