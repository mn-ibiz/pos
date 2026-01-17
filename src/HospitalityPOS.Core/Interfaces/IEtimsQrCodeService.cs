namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for generating eTIMS-compliant QR codes for receipts.
/// </summary>
public interface IEtimsQrCodeService
{
    /// <summary>
    /// Generates a QR code image from the eTIMS verification URL.
    /// </summary>
    /// <param name="qrCodeData">The QR code data (verification URL from KRA).</param>
    /// <param name="pixelsPerModule">Size of each QR module in pixels (default: 4).</param>
    /// <returns>QR code as byte array (PNG format).</returns>
    byte[] GenerateQrCodeImage(string qrCodeData, int pixelsPerModule = 4);

    /// <summary>
    /// Generates a QR code as a Base64-encoded string for embedding in HTML/XAML.
    /// </summary>
    /// <param name="qrCodeData">The QR code data (verification URL from KRA).</param>
    /// <param name="pixelsPerModule">Size of each QR module in pixels (default: 4).</param>
    /// <returns>Base64-encoded PNG image string.</returns>
    string GenerateQrCodeBase64(string qrCodeData, int pixelsPerModule = 4);

    /// <summary>
    /// Generates the eTIMS verification URL from invoice data.
    /// </summary>
    /// <param name="invoiceNumber">The eTIMS invoice number.</param>
    /// <param name="receiptSignature">The KRA receipt signature.</param>
    /// <param name="deviceControlUnitId">The device Control Unit ID.</param>
    /// <returns>The verification URL to encode in QR code.</returns>
    string GenerateVerificationUrl(string invoiceNumber, string? receiptSignature, string deviceControlUnitId);

    /// <summary>
    /// Generates a QR code image for an eTIMS invoice.
    /// </summary>
    /// <param name="etimsInvoiceId">The eTIMS invoice ID.</param>
    /// <param name="pixelsPerModule">Size of each QR module in pixels (default: 4).</param>
    /// <returns>QR code as byte array (PNG format), or null if invoice not found.</returns>
    Task<byte[]?> GenerateQrCodeForInvoiceAsync(int etimsInvoiceId, int pixelsPerModule = 4);

    /// <summary>
    /// Generates ESC/POS commands for printing QR code on thermal printer.
    /// </summary>
    /// <param name="qrCodeData">The QR code data to encode.</param>
    /// <param name="moduleSize">QR code module size (1-8, default: 4).</param>
    /// <returns>ESC/POS command bytes for QR code printing.</returns>
    byte[] GenerateEscPosQrCode(string qrCodeData, int moduleSize = 4);
}
