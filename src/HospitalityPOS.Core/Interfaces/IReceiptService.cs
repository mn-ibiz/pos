using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for managing receipts.
/// </summary>
public interface IReceiptService
{
    /// <summary>
    /// Creates a receipt from an order.
    /// </summary>
    /// <param name="orderId">The order ID to create receipt from.</param>
    /// <returns>The created receipt with receipt items.</returns>
    Task<Receipt> CreateReceiptFromOrderAsync(int orderId);

    /// <summary>
    /// Gets a receipt by ID.
    /// </summary>
    /// <param name="id">The receipt ID.</param>
    /// <returns>The receipt if found, null otherwise.</returns>
    Task<Receipt?> GetByIdAsync(int id);

    /// <summary>
    /// Gets a receipt by receipt number.
    /// </summary>
    /// <param name="receiptNumber">The receipt number.</param>
    /// <returns>The receipt if found, null otherwise.</returns>
    Task<Receipt?> GetByReceiptNumberAsync(string receiptNumber);

    /// <summary>
    /// Gets all pending receipts for the current work period.
    /// </summary>
    /// <returns>List of pending receipts.</returns>
    Task<IEnumerable<Receipt>> GetPendingReceiptsAsync();

    /// <summary>
    /// Gets all receipts for a specific work period.
    /// </summary>
    /// <param name="workPeriodId">The work period ID.</param>
    /// <returns>List of receipts.</returns>
    Task<IEnumerable<Receipt>> GetReceiptsByWorkPeriodAsync(int workPeriodId);

    /// <summary>
    /// Gets all receipts owned by a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>List of receipts.</returns>
    Task<IEnumerable<Receipt>> GetReceiptsByUserAsync(int userId);

    /// <summary>
    /// Gets receipts for an order.
    /// </summary>
    /// <param name="orderId">The order ID.</param>
    /// <returns>List of receipts for the order.</returns>
    Task<IEnumerable<Receipt>> GetReceiptsByOrderAsync(int orderId);

    /// <summary>
    /// Updates a receipt.
    /// </summary>
    /// <param name="receipt">The receipt to update.</param>
    /// <returns>The updated receipt.</returns>
    Task<Receipt> UpdateReceiptAsync(Receipt receipt);

    /// <summary>
    /// Generates the next receipt number for the current date.
    /// </summary>
    /// <returns>The generated receipt number (format: R-yyyyMMdd-sequence).</returns>
    Task<string> GenerateReceiptNumberAsync();

    /// <summary>
    /// Gets all active payment methods.
    /// </summary>
    /// <returns>List of active payment methods.</returns>
    Task<IEnumerable<PaymentMethod>> GetPaymentMethodsAsync();

    /// <summary>
    /// Settles a receipt with the given payments.
    /// </summary>
    /// <param name="receiptId">The receipt ID.</param>
    /// <param name="payments">The payments to apply.</param>
    /// <returns>The settled receipt.</returns>
    Task<Receipt> SettleReceiptAsync(int receiptId, IEnumerable<Payment> payments);

    /// <summary>
    /// Adds a payment to a receipt.
    /// </summary>
    /// <param name="payment">The payment to add.</param>
    /// <returns>The created payment.</returns>
    Task<Payment> AddPaymentAsync(Payment payment);

    /// <summary>
    /// Checks if a payment reference number has already been used.
    /// </summary>
    /// <param name="reference">The reference number to check.</param>
    /// <returns>The existing payment if found, null otherwise.</returns>
    Task<Payment?> GetPaymentByReferenceAsync(string reference);

    /// <summary>
    /// Removes a payment from a receipt (for split payment adjustments before finalization).
    /// </summary>
    /// <param name="paymentId">The payment ID to remove.</param>
    /// <returns>True if removed, false otherwise.</returns>
    Task<bool> RemovePaymentAsync(int paymentId);

    /// <summary>
    /// Gets the total sales amount for settled receipts within a date range.
    /// </summary>
    /// <param name="startDate">The start date of the period.</param>
    /// <param name="endDate">The end date of the period.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The total sales amount for the period.</returns>
    Task<decimal> GetSalesTotalAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}
