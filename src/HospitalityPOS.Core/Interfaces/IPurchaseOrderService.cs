using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for managing purchase orders.
/// </summary>
public interface IPurchaseOrderService
{
    /// <summary>
    /// Gets all purchase orders.
    /// </summary>
    /// <param name="includeItems">Whether to include line items.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of purchase orders.</returns>
    Task<IReadOnlyList<PurchaseOrder>> GetAllPurchaseOrdersAsync(bool includeItems = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets purchase orders by status.
    /// </summary>
    /// <param name="status">The PO status to filter by.</param>
    /// <param name="includeItems">Whether to include line items.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of purchase orders with the specified status.</returns>
    Task<IReadOnlyList<PurchaseOrder>> GetPurchaseOrdersByStatusAsync(PurchaseOrderStatus status, bool includeItems = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets purchase orders for a specific supplier.
    /// </summary>
    /// <param name="supplierId">The supplier ID.</param>
    /// <param name="includeItems">Whether to include line items.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of purchase orders for the supplier.</returns>
    Task<IReadOnlyList<PurchaseOrder>> GetPurchaseOrdersBySupplierAsync(int supplierId, bool includeItems = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a purchase order by ID.
    /// </summary>
    /// <param name="id">The PO ID.</param>
    /// <param name="includeItems">Whether to include line items.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The purchase order if found; otherwise, null.</returns>
    Task<PurchaseOrder?> GetPurchaseOrderByIdAsync(int id, bool includeItems = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a purchase order by PO number.
    /// </summary>
    /// <param name="poNumber">The PO number.</param>
    /// <param name="includeItems">Whether to include line items.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The purchase order if found; otherwise, null.</returns>
    Task<PurchaseOrder?> GetPurchaseOrderByNumberAsync(string poNumber, bool includeItems = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new purchase order.
    /// </summary>
    /// <param name="purchaseOrder">The purchase order to create.</param>
    /// <param name="createdByUserId">The ID of the user creating the PO.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created purchase order with generated PO number.</returns>
    Task<PurchaseOrder> CreatePurchaseOrderAsync(PurchaseOrder purchaseOrder, int createdByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing purchase order.
    /// </summary>
    /// <param name="purchaseOrder">The purchase order to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated purchase order.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the PO cannot be modified (e.g., already sent).</exception>
    Task<PurchaseOrder> UpdatePurchaseOrderAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an item to a purchase order.
    /// </summary>
    /// <param name="purchaseOrderId">The PO ID.</param>
    /// <param name="item">The item to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added item.</returns>
    Task<PurchaseOrderItem> AddItemAsync(int purchaseOrderId, PurchaseOrderItem item, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an item in a purchase order.
    /// </summary>
    /// <param name="item">The item to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated item.</returns>
    Task<PurchaseOrderItem> UpdateItemAsync(PurchaseOrderItem item, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an item from a purchase order.
    /// </summary>
    /// <param name="itemId">The item ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the item was removed; otherwise, false.</returns>
    Task<bool> RemoveItemAsync(int itemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the status of a purchase order.
    /// </summary>
    /// <param name="purchaseOrderId">The PO ID.</param>
    /// <param name="status">The new status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated purchase order.</returns>
    Task<PurchaseOrder> UpdateStatusAsync(int purchaseOrderId, PurchaseOrderStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a purchase order to the supplier (changes status from Draft to Sent).
    /// </summary>
    /// <param name="purchaseOrderId">The PO ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated purchase order.</returns>
    Task<PurchaseOrder> SendToSupplierAsync(int purchaseOrderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a purchase order.
    /// </summary>
    /// <param name="purchaseOrderId">The PO ID.</param>
    /// <param name="reason">The cancellation reason.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cancelled purchase order.</returns>
    Task<PurchaseOrder> CancelPurchaseOrderAsync(int purchaseOrderId, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates the next PO number for the given date.
    /// Format: PO-{yyyyMMdd}-{sequence}
    /// </summary>
    /// <param name="orderDate">The order date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The next available PO number.</returns>
    Task<string> GeneratePONumberAsync(DateTime orderDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recalculates totals for a purchase order based on its items.
    /// </summary>
    /// <param name="purchaseOrderId">The PO ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated purchase order with recalculated totals.</returns>
    Task<PurchaseOrder> RecalculateTotalsAsync(int purchaseOrderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of purchase orders by status.
    /// </summary>
    /// <param name="status">The status to count.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of purchase orders.</returns>
    Task<int> GetCountByStatusAsync(PurchaseOrderStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets purchase orders within a date range.
    /// </summary>
    /// <param name="startDate">The start date.</param>
    /// <param name="endDate">The end date.</param>
    /// <param name="includeItems">Whether to include line items.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of purchase orders within the date range.</returns>
    Task<IReadOnlyList<PurchaseOrder>> GetPurchaseOrdersByDateRangeAsync(DateTime startDate, DateTime endDate, bool includeItems = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches purchase orders by PO number or supplier name.
    /// </summary>
    /// <param name="searchTerm">The search term.</param>
    /// <param name="includeItems">Whether to include line items.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of matching purchase orders.</returns>
    Task<IReadOnlyList<PurchaseOrder>> SearchPurchaseOrdersAsync(string searchTerm, bool includeItems = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Duplicates an existing purchase order, creating a new draft PO with the same items.
    /// </summary>
    /// <param name="purchaseOrderId">The ID of the PO to duplicate.</param>
    /// <param name="createdByUserId">The ID of the user creating the duplicate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created duplicate purchase order.</returns>
    Task<PurchaseOrder> DuplicatePurchaseOrderAsync(int purchaseOrderId, int createdByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Archives a purchase order. Only Draft, Complete, and Cancelled POs can be archived.
    /// </summary>
    /// <param name="purchaseOrderId">The PO ID.</param>
    /// <param name="userId">The ID of the user archiving the PO.</param>
    /// <param name="reason">Optional reason for archiving.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if archived successfully; otherwise, false.</returns>
    Task<bool> ArchivePurchaseOrderAsync(int purchaseOrderId, int userId, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores an archived purchase order back to Draft status.
    /// </summary>
    /// <param name="purchaseOrderId">The PO ID.</param>
    /// <param name="userId">The ID of the user restoring the PO.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if restored successfully; otherwise, false.</returns>
    Task<bool> RestorePurchaseOrderAsync(int purchaseOrderId, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes a purchase order. Only Draft, Archived, and Cancelled POs can be deleted.
    /// </summary>
    /// <param name="purchaseOrderId">The PO ID.</param>
    /// <param name="userId">The ID of the user deleting the PO.</param>
    /// <param name="reason">Reason for deletion.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted successfully; otherwise, false.</returns>
    Task<bool> DeletePurchaseOrderAsync(int purchaseOrderId, int userId, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently deletes a purchase order. Admin only, for cleanup.
    /// </summary>
    /// <param name="purchaseOrderId">The PO ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if permanently deleted; otherwise, false.</returns>
    Task<bool> PermanentlyDeletePurchaseOrderAsync(int purchaseOrderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets archived purchase orders.
    /// </summary>
    /// <param name="fromDate">Optional start date filter.</param>
    /// <param name="toDate">Optional end date filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of archived purchase orders.</returns>
    Task<IReadOnlyList<PurchaseOrder>> GetArchivedPurchaseOrdersAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets soft-deleted purchase orders for admin recovery.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of deleted purchase orders.</returns>
    Task<IReadOnlyList<PurchaseOrder>> GetDeletedPurchaseOrdersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Recovers a soft-deleted purchase order.
    /// </summary>
    /// <param name="purchaseOrderId">The PO ID.</param>
    /// <param name="userId">The ID of the user recovering the PO.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if recovered successfully; otherwise, false.</returns>
    Task<bool> RecoverPurchaseOrderAsync(int purchaseOrderId, int userId, CancellationToken cancellationToken = default);
}
