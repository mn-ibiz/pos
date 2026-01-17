using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Models;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for routing order items to appropriate kitchen printers.
/// </summary>
public interface IKitchenOrderRoutingService
{
    /// <summary>
    /// Routes order items to appropriate kitchen printers based on category mappings.
    /// </summary>
    /// <param name="items">The order items to route.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary mapping printers to their assigned items.</returns>
    Task<Dictionary<Printer, List<OrderItem>>> RouteOrderItemsAsync(
        IEnumerable<OrderItem> items,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Prints KOTs for an order to the appropriate kitchen printers.
    /// </summary>
    /// <param name="orderId">The order ID.</param>
    /// <param name="isIncremental">True if printing only new items (additions).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the routing and printing operation.</returns>
    Task<KitchenRoutingResult> PrintKOTsAsync(
        int orderId,
        bool isIncremental = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the printer that would handle a specific category.
    /// </summary>
    /// <param name="categoryId">The category ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The printer assigned to the category, or the default kitchen printer.</returns>
    Task<Printer?> GetPrinterForCategoryAsync(
        int categoryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates KOT data for a set of order items.
    /// </summary>
    /// <param name="order">The order.</param>
    /// <param name="items">The items to include in the KOT.</param>
    /// <param name="isIncremental">Whether this is an incremental order.</param>
    /// <returns>KOT data ready for printing.</returns>
    KOTData GenerateKOTData(Order order, IEnumerable<OrderItem> items, bool isIncremental);

    /// <summary>
    /// Prints a KOT to a specific printer.
    /// </summary>
    /// <param name="printer">The printer to use.</param>
    /// <param name="kotData">The KOT data to print.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Print result.</returns>
    Task<PrintTestResult> PrintKOTAsync(
        Printer printer,
        KOTData kotData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all kitchen printers with their category mappings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of kitchen printers.</returns>
    Task<List<Printer>> GetKitchenPrintersWithMappingsAsync(
        CancellationToken cancellationToken = default);
}
