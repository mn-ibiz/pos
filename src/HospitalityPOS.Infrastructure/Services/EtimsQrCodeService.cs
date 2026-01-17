using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QRCoder;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for generating eTIMS-compliant QR codes for receipts.
/// Implements KRA eTIMS specification for invoice verification QR codes.
/// </summary>
public class EtimsQrCodeService : IEtimsQrCodeService
{
    private readonly POSDbContext _context;
    private readonly ILogger<EtimsQrCodeService> _logger;

    /// <summary>
    /// KRA eTIMS verification base URL.
    /// </summary>
    private const string KRA_VERIFICATION_BASE_URL = "https://etims.kra.go.ke/verify";

    public EtimsQrCodeService(POSDbContext context, ILogger<EtimsQrCodeService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public byte[] GenerateQrCodeImage(string qrCodeData, int pixelsPerModule = 4)
    {
        if (string.IsNullOrWhiteSpace(qrCodeData))
        {
            throw new ArgumentException("QR code data cannot be empty", nameof(qrCodeData));
        }

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeDataObj = qrGenerator.CreateQrCode(qrCodeData, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(qrCodeDataObj);

        var pngBytes = qrCode.GetGraphic(pixelsPerModule);

        _logger.LogDebug("Generated QR code image with {ModuleSize}px modules", pixelsPerModule);

        return pngBytes;
    }

    /// <inheritdoc />
    public string GenerateQrCodeBase64(string qrCodeData, int pixelsPerModule = 4)
    {
        var pngBytes = GenerateQrCodeImage(qrCodeData, pixelsPerModule);
        return Convert.ToBase64String(pngBytes);
    }

    /// <inheritdoc />
    public string GenerateVerificationUrl(string invoiceNumber, string? receiptSignature, string deviceControlUnitId)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
        {
            throw new ArgumentException("Invoice number cannot be empty", nameof(invoiceNumber));
        }

        // Build verification URL per KRA specification
        // Format: https://etims.kra.go.ke/verify?inv={invoiceNumber}&sig={signature}&cu={controlUnitId}
        var queryParams = new List<string>
        {
            $"inv={Uri.EscapeDataString(invoiceNumber)}"
        };

        if (!string.IsNullOrWhiteSpace(receiptSignature))
        {
            queryParams.Add($"sig={Uri.EscapeDataString(receiptSignature)}");
        }

        if (!string.IsNullOrWhiteSpace(deviceControlUnitId))
        {
            queryParams.Add($"cu={Uri.EscapeDataString(deviceControlUnitId)}");
        }

        return $"{KRA_VERIFICATION_BASE_URL}?{string.Join("&", queryParams)}";
    }

    /// <inheritdoc />
    public async Task<byte[]?> GenerateQrCodeForInvoiceAsync(int etimsInvoiceId, int pixelsPerModule = 4)
    {
        var invoice = await _context.Set<EtimsInvoice>()
            .Include(i => i.Device)
            .FirstOrDefaultAsync(i => i.Id == etimsInvoiceId);

        if (invoice == null)
        {
            _logger.LogWarning("eTIMS invoice {InvoiceId} not found for QR code generation", etimsInvoiceId);
            return null;
        }

        // Use stored QR code data if available (from KRA response)
        string qrCodeData;
        if (!string.IsNullOrWhiteSpace(invoice.QrCode))
        {
            qrCodeData = invoice.QrCode;
        }
        else
        {
            // Generate verification URL
            qrCodeData = GenerateVerificationUrl(
                invoice.InvoiceNumber,
                invoice.ReceiptSignature,
                invoice.Device?.ControlUnitId ?? "");
        }

        _logger.LogInformation("Generating QR code for eTIMS invoice {InvoiceNumber}", invoice.InvoiceNumber);

        return GenerateQrCodeImage(qrCodeData, pixelsPerModule);
    }

    /// <inheritdoc />
    public byte[] GenerateEscPosQrCode(string qrCodeData, int moduleSize = 4)
    {
        if (string.IsNullOrWhiteSpace(qrCodeData))
        {
            throw new ArgumentException("QR code data cannot be empty", nameof(qrCodeData));
        }

        // Clamp module size to valid range (1-8)
        moduleSize = Math.Clamp(moduleSize, 1, 8);

        var dataBytes = System.Text.Encoding.UTF8.GetBytes(qrCodeData);
        var dataLength = dataBytes.Length;

        // Calculate store length (data + 3 for function parameters)
        var storeLength = dataLength + 3;
        var pL = (byte)(storeLength % 256);
        var pH = (byte)(storeLength / 256);

        // ESC/POS QR Code commands
        using var ms = new MemoryStream();

        // Set QR code model (Model 2 recommended)
        // GS ( k pL pH cn fn n
        ms.Write([0x1D, 0x28, 0x6B, 0x04, 0x00, 0x31, 0x41, 0x32, 0x00]);

        // Set module size
        // GS ( k pL pH cn fn n
        ms.Write([0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x43, (byte)moduleSize]);

        // Set error correction level (M = ~15%)
        // GS ( k pL pH cn fn n (48=L, 49=M, 50=Q, 51=H)
        ms.Write([0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x45, 0x31]);

        // Store QR code data
        // GS ( k pL pH cn fn m d1...dk
        ms.Write([0x1D, 0x28, 0x6B, pL, pH, 0x31, 0x50, 0x30]);
        ms.Write(dataBytes);

        // Print QR code
        // GS ( k pL pH cn fn m
        ms.Write([0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x51, 0x30]);

        _logger.LogDebug("Generated ESC/POS QR code commands for {DataLength} bytes of data", dataLength);

        return ms.ToArray();
    }
}
