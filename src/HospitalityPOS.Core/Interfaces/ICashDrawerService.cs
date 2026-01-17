using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for cash drawer operations.
/// </summary>
public interface ICashDrawerService
{
    /// <summary>
    /// Opens the cash drawer using the configured printer.
    /// </summary>
    /// <param name="reason">The reason for opening the drawer.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> OpenDrawerAsync(string reason);

    /// <summary>
    /// Opens a specific cash drawer.
    /// </summary>
    /// <param name="drawerId">The drawer ID.</param>
    /// <param name="reason">The reason for opening.</param>
    /// <param name="reference">Optional reference (e.g., receipt number).</param>
    /// <param name="notes">Optional notes (for manual opens).</param>
    /// <param name="authorizedByUserId">Optional user who authorized (for overrides).</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> OpenDrawerAsync(
        int drawerId,
        CashDrawerOpenReason reason,
        string? reference = null,
        string? notes = null,
        int? authorizedByUserId = null);

    /// <summary>
    /// Opens the cash drawer for a specific payment.
    /// </summary>
    /// <param name="receiptNumber">The receipt number associated with the payment.</param>
    /// <param name="paymentMethodCode">The payment method code.</param>
    /// <param name="amount">The payment amount.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> OpenDrawerForPaymentAsync(string receiptNumber, string paymentMethodCode, decimal amount);

    /// <summary>
    /// Checks if the cash drawer is configured and available.
    /// </summary>
    /// <returns>True if the drawer is ready, false otherwise.</returns>
    Task<bool> IsDrawerReadyAsync();

    /// <summary>
    /// Gets the default cash drawer.
    /// </summary>
    /// <returns>The default cash drawer or null.</returns>
    Task<CashDrawer?> GetDefaultDrawerAsync();

    /// <summary>
    /// Gets all active cash drawers.
    /// </summary>
    /// <returns>List of active cash drawers.</returns>
    Task<List<CashDrawer>> GetAllDrawersAsync();

    /// <summary>
    /// Gets a cash drawer by ID.
    /// </summary>
    /// <param name="drawerId">The drawer ID.</param>
    /// <returns>The cash drawer or null.</returns>
    Task<CashDrawer?> GetDrawerAsync(int drawerId);

    /// <summary>
    /// Creates a new cash drawer.
    /// </summary>
    /// <param name="drawer">The drawer to create.</param>
    /// <returns>The created drawer.</returns>
    Task<CashDrawer> CreateDrawerAsync(CashDrawer drawer);

    /// <summary>
    /// Updates a cash drawer.
    /// </summary>
    /// <param name="drawer">The drawer to update.</param>
    /// <returns>True if successful.</returns>
    Task<bool> UpdateDrawerAsync(CashDrawer drawer);

    /// <summary>
    /// Deletes a cash drawer.
    /// </summary>
    /// <param name="drawerId">The drawer ID.</param>
    /// <returns>True if successful.</returns>
    Task<bool> DeleteDrawerAsync(int drawerId);

    /// <summary>
    /// Gets cash drawer status.
    /// </summary>
    /// <param name="drawerId">The drawer ID.</param>
    /// <returns>The drawer status.</returns>
    Task<CashDrawerStatus> GetStatusAsync(int drawerId);

    /// <summary>
    /// Gets cash drawer logs for a specific date.
    /// </summary>
    /// <param name="drawerId">The drawer ID.</param>
    /// <param name="date">The date to get logs for.</param>
    /// <returns>List of drawer logs.</returns>
    Task<List<CashDrawerLog>> GetLogsAsync(int drawerId, DateTime date);

    /// <summary>
    /// Gets cash drawer logs for a date range.
    /// </summary>
    /// <param name="drawerId">The drawer ID.</param>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <returns>List of drawer logs.</returns>
    Task<List<CashDrawerLog>> GetLogsAsync(int drawerId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Tests a cash drawer by sending a kick command.
    /// </summary>
    /// <param name="drawerId">The drawer ID.</param>
    /// <returns>True if successful.</returns>
    Task<bool> TestDrawerAsync(int drawerId);

    /// <summary>
    /// Event raised when the cash drawer is opened.
    /// </summary>
    event EventHandler<CashDrawerEventArgs>? DrawerOpened;
}

/// <summary>
/// Event arguments for cash drawer events.
/// </summary>
public class CashDrawerEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the drawer ID.
    /// </summary>
    public int DrawerId { get; set; }

    /// <summary>
    /// Gets or sets the reason the drawer was opened.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the open reason enum.
    /// </summary>
    public CashDrawerOpenReason ReasonType { get; set; }

    /// <summary>
    /// Gets or sets the receipt number (if applicable).
    /// </summary>
    public string? ReceiptNumber { get; set; }

    /// <summary>
    /// Gets or sets the payment method code (if applicable).
    /// </summary>
    public string? PaymentMethodCode { get; set; }

    /// <summary>
    /// Gets or sets the payment amount (if applicable).
    /// </summary>
    public decimal? Amount { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the drawer was opened.
    /// </summary>
    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the user ID who opened the drawer.
    /// </summary>
    public int OpenedByUserId { get; set; }

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
