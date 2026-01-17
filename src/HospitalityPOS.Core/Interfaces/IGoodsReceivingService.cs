using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for goods receiving operations.
/// Handles both PO-based receiving and direct receiving.
/// </summary>
public interface IGoodsReceivingService
{
    /// <summary>
    /// Gets all purchase orders that are pending receiving (Sent or PartiallyReceived status).
    /// </summary>
    /// <returns>List of purchase orders available for receiving.</returns>
    Task<IReadOnlyList<PurchaseOrder>> GetPendingPurchaseOrdersAsync();

    /// <summary>
    /// Gets a purchase order with its items and product details for receiving.
    /// </summary>
    /// <param name="purchaseOrderId">The purchase order ID.</param>
    /// <returns>The purchase order with items, or null if not found.</returns>
    Task<PurchaseOrder?> GetPurchaseOrderForReceivingAsync(int purchaseOrderId);

    /// <summary>
    /// Creates a goods received note for a purchase order.
    /// Updates stock levels and PO received quantities.
    /// </summary>
    /// <param name="purchaseOrderId">The purchase order ID.</param>
    /// <param name="deliveryNote">Optional supplier delivery note number.</param>
    /// <param name="notes">Optional notes about the receiving.</param>
    /// <param name="items">The items being received with quantities and costs.</param>
    /// <returns>The created GRN.</returns>
    Task<GoodsReceivedNote> ReceiveGoodsAsync(
        int purchaseOrderId,
        string? deliveryNote,
        string? notes,
        IEnumerable<GRNItemInput> items);

    /// <summary>
    /// Creates a goods received note for direct receiving (without PO).
    /// Updates stock levels.
    /// </summary>
    /// <param name="supplierId">Optional supplier ID.</param>
    /// <param name="deliveryNote">Optional supplier delivery note number.</param>
    /// <param name="notes">Optional notes about the receiving.</param>
    /// <param name="items">The items being received with quantities and costs.</param>
    /// <returns>The created GRN.</returns>
    Task<GoodsReceivedNote> ReceiveDirectAsync(
        int? supplierId,
        string? deliveryNote,
        string? notes,
        IEnumerable<GRNItemInput> items);

    /// <summary>
    /// Generates the next GRN number in sequence.
    /// Format: GRN-{yyyyMMdd}-{sequence}
    /// </summary>
    /// <returns>The next GRN number.</returns>
    Task<string> GenerateGRNNumberAsync();

    /// <summary>
    /// Gets a goods received note by ID with all related data.
    /// </summary>
    /// <param name="id">The GRN ID.</param>
    /// <returns>The GRN, or null if not found.</returns>
    Task<GoodsReceivedNote?> GetByIdAsync(int id);

    /// <summary>
    /// Gets a goods received note by GRN number with all related data.
    /// </summary>
    /// <param name="grnNumber">The GRN number.</param>
    /// <returns>The GRN, or null if not found.</returns>
    Task<GoodsReceivedNote?> GetByNumberAsync(string grnNumber);

    /// <summary>
    /// Gets goods received notes within a date range.
    /// </summary>
    /// <param name="startDate">Start date (inclusive).</param>
    /// <param name="endDate">End date (inclusive).</param>
    /// <returns>List of GRNs.</returns>
    Task<IReadOnlyList<GoodsReceivedNote>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets all active suppliers for selection.
    /// </summary>
    /// <returns>List of active suppliers.</returns>
    Task<IReadOnlyList<Supplier>> GetActiveSuppliersAsync();

    /// <summary>
    /// Searches products by name, code, or barcode.
    /// </summary>
    /// <param name="searchText">The search text.</param>
    /// <param name="maxResults">Maximum number of results to return.</param>
    /// <returns>List of matching products.</returns>
    Task<IReadOnlyList<Product>> SearchProductsAsync(string searchText, int maxResults = 10);
}

/// <summary>
/// Input model for GRN items.
/// </summary>
public class GRNItemInput
{
    /// <summary>
    /// Gets or sets the purchase order item ID (null for direct receiving).
    /// </summary>
    public int? PurchaseOrderItemId { get; set; }

    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the ordered quantity (0 for direct receiving).
    /// </summary>
    public decimal OrderedQuantity { get; set; }

    /// <summary>
    /// Gets or sets the received quantity.
    /// </summary>
    public decimal ReceivedQuantity { get; set; }

    /// <summary>
    /// Gets or sets the unit cost.
    /// </summary>
    public decimal UnitCost { get; set; }

    /// <summary>
    /// Gets or sets any notes for this item.
    /// </summary>
    public string? Notes { get; set; }
}
