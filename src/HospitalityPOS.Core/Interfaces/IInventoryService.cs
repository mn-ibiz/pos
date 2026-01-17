using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for managing inventory operations.
/// </summary>
public interface IInventoryService
{
    /// <summary>
    /// Deducts stock for a product when sold.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="quantity">The quantity to deduct (positive value).</param>
    /// <param name="reference">Reference description (e.g., receipt number).</param>
    /// <param name="referenceId">Optional reference ID (e.g., receipt ID).</param>
    /// <returns>The created stock movement record, or null if tracking is disabled.</returns>
    Task<StockMovement?> DeductStockAsync(
        int productId,
        decimal quantity,
        string reference,
        int? referenceId = null);

    /// <summary>
    /// Restores stock for a product (e.g., when voiding a receipt).
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="quantity">The quantity to restore (positive value).</param>
    /// <param name="movementType">The type of movement (e.g., Void, Return).</param>
    /// <param name="reference">Reference description.</param>
    /// <param name="referenceId">Optional reference ID.</param>
    /// <returns>The created stock movement record, or null if tracking is disabled.</returns>
    Task<StockMovement?> RestoreStockAsync(
        int productId,
        decimal quantity,
        MovementType movementType,
        string reference,
        int? referenceId = null);

    /// <summary>
    /// Adjusts stock to a new quantity (for manual corrections).
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="newQuantity">The new stock quantity.</param>
    /// <param name="reason">The reason for adjustment.</param>
    /// <param name="notes">Optional additional notes.</param>
    /// <returns>The created stock movement record.</returns>
    Task<StockMovement> AdjustStockAsync(
        int productId,
        decimal newQuantity,
        string reason,
        string? notes = null);

    /// <summary>
    /// Adjusts stock to a new quantity with a specified adjustment reason.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="newQuantity">The new stock quantity.</param>
    /// <param name="adjustmentReasonId">The adjustment reason ID.</param>
    /// <param name="notes">Optional additional notes.</param>
    /// <returns>The created stock movement record.</returns>
    Task<StockMovement> AdjustStockAsync(
        int productId,
        decimal newQuantity,
        int adjustmentReasonId,
        string? notes = null);

    /// <summary>
    /// Gets the current stock level for a product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <returns>The current stock quantity, or 0 if not found.</returns>
    Task<decimal> GetStockLevelAsync(int productId);

    /// <summary>
    /// Checks if sufficient stock is available for a product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="requiredQuantity">The required quantity.</param>
    /// <returns>True if available (or tracking disabled), false otherwise.</returns>
    Task<bool> CheckAvailabilityAsync(int productId, decimal requiredQuantity);

    /// <summary>
    /// Deducts stock for all items in a settled receipt.
    /// </summary>
    /// <param name="receipt">The receipt with items to process.</param>
    /// <returns>List of stock movements created.</returns>
    Task<IEnumerable<StockMovement>> DeductStockForReceiptAsync(Receipt receipt);

    /// <summary>
    /// Restores stock for all items in a voided receipt.
    /// </summary>
    /// <param name="receipt">The receipt being voided.</param>
    /// <returns>List of stock movements created.</returns>
    Task<IEnumerable<StockMovement>> RestoreStockForVoidAsync(Receipt receipt);

    /// <summary>
    /// Gets products with low stock (below minimum level).
    /// </summary>
    /// <returns>List of products with low stock.</returns>
    Task<IEnumerable<Product>> GetLowStockProductsAsync();

    /// <summary>
    /// Gets products that are out of stock.
    /// </summary>
    /// <returns>List of out-of-stock products.</returns>
    Task<IEnumerable<Product>> GetOutOfStockProductsAsync();

    /// <summary>
    /// Gets stock movements for a product within a date range.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="startDate">Start date for the range.</param>
    /// <param name="endDate">End date for the range.</param>
    /// <returns>List of stock movements.</returns>
    Task<IEnumerable<StockMovement>> GetStockMovementsAsync(
        int productId,
        DateTime? startDate = null,
        DateTime? endDate = null);

    /// <summary>
    /// Gets stock movements by reference (e.g., all movements for a receipt).
    /// </summary>
    /// <param name="referenceType">The type of reference (e.g., "Receipt").</param>
    /// <param name="referenceId">The reference ID.</param>
    /// <returns>List of stock movements.</returns>
    Task<IEnumerable<StockMovement>> GetStockMovementsByReferenceAsync(
        string referenceType,
        int referenceId);

    /// <summary>
    /// Gets all active adjustment reasons.
    /// </summary>
    /// <returns>List of active adjustment reasons.</returns>
    Task<IReadOnlyList<AdjustmentReason>> GetAdjustmentReasonsAsync();

    /// <summary>
    /// Receives stock for a product (e.g., from goods receiving).
    /// Increases stock and optionally updates product cost price.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="quantity">The quantity to receive (positive value).</param>
    /// <param name="unitCost">The unit cost at time of receiving.</param>
    /// <param name="reference">Reference description (e.g., GRN number).</param>
    /// <param name="referenceId">Optional reference ID (e.g., GRN ID).</param>
    /// <returns>The created stock movement record, or null if tracking is disabled.</returns>
    Task<StockMovement?> ReceiveStockAsync(
        int productId,
        decimal quantity,
        decimal unitCost,
        string reference,
        int? referenceId = null);
}
