using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for printing Kitchen Order Tickets (KOT).
/// </summary>
public interface IKitchenPrintService
{
    /// <summary>
    /// Prints a Kitchen Order Ticket for an order.
    /// </summary>
    /// <param name="order">The order to print.</param>
    /// <param name="items">The items to include on the ticket.</param>
    /// <returns>True if print was successful, false otherwise.</returns>
    Task<bool> PrintKotAsync(Order order, IEnumerable<OrderItem> items);

    /// <summary>
    /// Prints an addition KOT for newly added items.
    /// </summary>
    /// <param name="order">The order.</param>
    /// <param name="newItems">The new items to print.</param>
    /// <returns>True if print was successful, false otherwise.</returns>
    Task<bool> PrintAdditionKotAsync(Order order, IEnumerable<OrderItem> newItems);

    /// <summary>
    /// Checks if the kitchen printer is available.
    /// </summary>
    /// <returns>True if printer is ready, false otherwise.</returns>
    Task<bool> IsPrinterReadyAsync();

    /// <summary>
    /// Gets the list of available kitchen stations.
    /// </summary>
    /// <returns>List of station names.</returns>
    IEnumerable<string> GetKitchenStations();
}
