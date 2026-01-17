using HospitalityPOS.Core.Models.Payments;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for generating and managing QR code payments.
/// Supports M-Pesa Lipa Na M-Pesa QR and extensible for other providers.
/// </summary>
public interface IQrPaymentService
{
    #region QR Code Generation

    /// <summary>
    /// Generates a QR code for payment.
    /// </summary>
    /// <param name="request">QR payment request details.</param>
    /// <returns>QR code generation result with image data.</returns>
    Task<QrPaymentResult> GenerateQrCodeAsync(QrPaymentRequest request);

    /// <summary>
    /// Generates a QR code for a specific receipt.
    /// </summary>
    /// <param name="receiptId">Receipt ID.</param>
    /// <param name="amount">Amount to pay.</param>
    /// <returns>QR code generation result.</returns>
    Task<QrPaymentResult> GenerateQrForReceiptAsync(int receiptId, decimal amount);

    /// <summary>
    /// Regenerates an expired or cancelled QR code.
    /// </summary>
    /// <param name="qrPaymentId">Original QR payment ID.</param>
    /// <returns>New QR code generation result.</returns>
    Task<QrPaymentResult> RegenerateQrCodeAsync(string qrPaymentId);

    #endregion

    #region Payment Status

    /// <summary>
    /// Checks the payment status for a QR code.
    /// </summary>
    /// <param name="qrPaymentId">QR payment ID.</param>
    /// <returns>Current payment status.</returns>
    Task<QrPaymentStatusResult> CheckPaymentStatusAsync(string qrPaymentId);

    /// <summary>
    /// Checks the payment status by QR reference.
    /// </summary>
    /// <param name="qrReference">QR reference string.</param>
    /// <returns>Current payment status.</returns>
    Task<QrPaymentStatusResult> CheckPaymentStatusByReferenceAsync(string qrReference);

    /// <summary>
    /// Gets all pending QR payments that need status polling.
    /// </summary>
    /// <returns>List of pending QR payment IDs.</returns>
    Task<IReadOnlyList<string>> GetPendingQrPaymentsAsync();

    #endregion

    #region Payment Management

    /// <summary>
    /// Cancels a pending QR payment.
    /// </summary>
    /// <param name="qrPaymentId">QR payment ID.</param>
    /// <returns>True if successfully cancelled.</returns>
    Task<bool> CancelQrPaymentAsync(string qrPaymentId);

    /// <summary>
    /// Marks a QR payment as expired.
    /// </summary>
    /// <param name="qrPaymentId">QR payment ID.</param>
    /// <returns>True if marked as expired.</returns>
    Task<bool> ExpireQrPaymentAsync(string qrPaymentId);

    /// <summary>
    /// Records a successful payment against a QR code.
    /// Called when payment is confirmed.
    /// </summary>
    /// <param name="qrPaymentId">QR payment ID.</param>
    /// <param name="transactionId">M-Pesa transaction ID.</param>
    /// <param name="receiptNumber">M-Pesa receipt number.</param>
    /// <param name="phoneNumber">Phone number that paid.</param>
    /// <returns>True if successfully recorded.</returns>
    Task<bool> RecordPaymentAsync(string qrPaymentId, string transactionId,
        string receiptNumber, string? phoneNumber = null);

    /// <summary>
    /// Processes expired QR payments (background task).
    /// </summary>
    /// <returns>Number of QR payments marked as expired.</returns>
    Task<int> ProcessExpiredQrPaymentsAsync();

    #endregion

    #region QR Payment Retrieval

    /// <summary>
    /// Gets a QR payment request by ID.
    /// </summary>
    /// <param name="qrPaymentId">QR payment ID.</param>
    /// <returns>QR payment entity or null.</returns>
    Task<QrPaymentRequestEntity?> GetQrPaymentAsync(string qrPaymentId);

    /// <summary>
    /// Gets QR payment history for a receipt.
    /// </summary>
    /// <param name="receiptId">Receipt ID.</param>
    /// <returns>List of QR payments for the receipt.</returns>
    Task<IReadOnlyList<QrPaymentRequestEntity>> GetQrPaymentsByReceiptAsync(int receiptId);

    #endregion

    #region Configuration

    /// <summary>
    /// Gets the current QR payment settings.
    /// </summary>
    /// <returns>QR payment settings.</returns>
    Task<QrPaymentSettings> GetSettingsAsync();

    /// <summary>
    /// Updates QR payment settings.
    /// </summary>
    /// <param name="settings">New settings.</param>
    Task UpdateSettingsAsync(QrPaymentSettings settings);

    /// <summary>
    /// Tests QR generation with the current configuration.
    /// </summary>
    /// <returns>Test result with sample QR code.</returns>
    Task<QrPaymentResult> TestQrGenerationAsync();

    #endregion

    #region Reporting

    /// <summary>
    /// Gets QR payment metrics for a period.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <returns>QR payment metrics.</returns>
    Task<QrPaymentMetrics> GetMetricsAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets QR payment comparison (QR vs STK Push vs Manual).
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <returns>Dictionary of payment method to count/amount.</returns>
    Task<Dictionary<string, (int Count, decimal Amount)>> GetPaymentMethodComparisonAsync(
        DateTime startDate, DateTime endDate);

    #endregion

    #region Events

    /// <summary>
    /// Event raised when a QR payment is completed.
    /// </summary>
    event EventHandler<QrPaymentCompletedEventArgs>? PaymentCompleted;

    /// <summary>
    /// Event raised when a QR payment expires.
    /// </summary>
    event EventHandler<QrPaymentExpiredEventArgs>? PaymentExpired;

    #endregion
}

/// <summary>
/// Event args for QR payment completion.
/// </summary>
public class QrPaymentCompletedEventArgs : EventArgs
{
    /// <summary>QR payment ID.</summary>
    public string QrPaymentId { get; set; } = string.Empty;

    /// <summary>Receipt ID.</summary>
    public int ReceiptId { get; set; }

    /// <summary>Amount paid.</summary>
    public decimal Amount { get; set; }

    /// <summary>M-Pesa transaction ID.</summary>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>M-Pesa receipt number.</summary>
    public string MpesaReceiptNumber { get; set; } = string.Empty;
}

/// <summary>
/// Event args for QR payment expiry.
/// </summary>
public class QrPaymentExpiredEventArgs : EventArgs
{
    /// <summary>QR payment ID.</summary>
    public string QrPaymentId { get; set; } = string.Empty;

    /// <summary>Receipt ID.</summary>
    public int ReceiptId { get; set; }

    /// <summary>Amount that was not paid.</summary>
    public decimal Amount { get; set; }
}
