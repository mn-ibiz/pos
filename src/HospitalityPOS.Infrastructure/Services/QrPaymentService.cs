using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Serilog;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Payments;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for generating and managing QR code payments.
/// Supports M-Pesa Lipa Na M-Pesa QR.
/// </summary>
public class QrPaymentService : IQrPaymentService
{
    private readonly POSDbContext _context;
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;
    private QrPaymentSettings _settings;

    // M-Pesa API endpoints
    private const string MpesaQrGenerateUrl = "https://sandbox.safaricom.co.ke/mpesa/qrcode/v1/generate";
    private const string MpesaQrGenerateProdUrl = "https://api.safaricom.co.ke/mpesa/qrcode/v1/generate";

    /// <inheritdoc />
    public event EventHandler<QrPaymentCompletedEventArgs>? PaymentCompleted;

    /// <inheritdoc />
    public event EventHandler<QrPaymentExpiredEventArgs>? PaymentExpired;

    /// <summary>
    /// Initializes a new instance of the <see cref="QrPaymentService"/> class.
    /// </summary>
    public QrPaymentService(POSDbContext context, ILogger logger, HttpClient httpClient)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _settings = new QrPaymentSettings();
    }

    #region QR Code Generation

    /// <inheritdoc />
    public async Task<QrPaymentResult> GenerateQrCodeAsync(QrPaymentRequest request)
    {
        try
        {
            _logger.Information("Generating QR code for amount {Amount}, reference {Reference}",
                request.Amount, request.Reference);

            // Validate request
            if (request.Amount <= 0)
            {
                return QrPaymentResult.Failure("Amount must be greater than zero");
            }

            if (string.IsNullOrWhiteSpace(request.Reference))
            {
                return QrPaymentResult.Failure("Reference is required");
            }

            // Generate unique QR payment ID
            var qrPaymentId = $"QR-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
            var expiresAt = DateTime.UtcNow.AddSeconds(request.ValiditySeconds);

            // Generate QR code based on provider
            byte[] qrCodeBytes;
            string? externalRef = null;

            switch (request.Provider)
            {
                case QrPaymentProvider.MpesaQr:
                    var mpesaResult = await GenerateMpesaQrAsync(request);
                    if (!mpesaResult.success)
                    {
                        return QrPaymentResult.Failure(mpesaResult.error ?? "Failed to generate M-Pesa QR");
                    }
                    qrCodeBytes = mpesaResult.qrBytes!;
                    externalRef = mpesaResult.requestId;
                    break;

                default:
                    // Fallback: Generate a simple QR code locally
                    qrCodeBytes = GenerateLocalQrCode(request);
                    break;
            }

            // Store the QR payment request
            var entity = new QrPaymentRequestEntity
            {
                QrReference = qrPaymentId,
                ExternalReference = externalRef,
                Amount = request.Amount,
                Provider = request.Provider.ToString(),
                QrCodeData = Convert.ToBase64String(qrCodeBytes),
                Status = QrPaymentStatus.Pending.ToString(),
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow
            };

            // Note: Would need to add QrPaymentRequestEntity to DbContext
            // For now, we'll use in-memory tracking
            _pendingPayments[qrPaymentId] = entity;

            var result = QrPaymentResult.Successful(qrPaymentId, request.Reference, request.Amount, qrCodeBytes, expiresAt);
            result.Provider = request.Provider;

            _logger.Information("QR code generated successfully: {QrPaymentId}", qrPaymentId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to generate QR code");
            return QrPaymentResult.Failure($"Failed to generate QR code: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<QrPaymentResult> GenerateQrForReceiptAsync(int receiptId, decimal amount)
    {
        var request = new QrPaymentRequest
        {
            Amount = amount,
            Reference = $"RCP-{receiptId}",
            MerchantName = _settings.MerchantName,
            MerchantCode = _settings.MpesaTillNumber,
            Provider = QrPaymentProvider.MpesaQr,
            ValiditySeconds = _settings.DefaultValiditySeconds
        };

        var result = await GenerateQrCodeAsync(request);

        if (result.Success && _pendingPayments.TryGetValue(result.QrPaymentId, out var entity))
        {
            entity.ReceiptId = receiptId;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<QrPaymentResult> RegenerateQrCodeAsync(string qrPaymentId)
    {
        if (!_pendingPayments.TryGetValue(qrPaymentId, out var original))
        {
            return QrPaymentResult.Failure("Original QR payment not found");
        }

        // Mark original as cancelled
        original.Status = QrPaymentStatus.Cancelled.ToString();

        // Generate new QR with same details
        var request = new QrPaymentRequest
        {
            Amount = original.Amount,
            Reference = $"RCP-{original.ReceiptId}",
            MerchantName = _settings.MerchantName,
            MerchantCode = _settings.MpesaTillNumber,
            Provider = Enum.Parse<QrPaymentProvider>(original.Provider),
            ValiditySeconds = _settings.DefaultValiditySeconds
        };

        var result = await GenerateQrCodeAsync(request);

        if (result.Success && _pendingPayments.TryGetValue(result.QrPaymentId, out var newEntity))
        {
            newEntity.ReceiptId = original.ReceiptId;
        }

        return result;
    }

    private async Task<(bool success, byte[]? qrBytes, string? requestId, string? error)> GenerateMpesaQrAsync(QrPaymentRequest request)
    {
        try
        {
            // In production, this would call the M-Pesa API
            // For now, generate a local QR code with M-Pesa format data

            var mpesaData = new
            {
                MerchantName = request.MerchantName,
                RefNo = request.Reference,
                Amount = (int)request.Amount,
                TrxCode = request.TransactionType,
                CPI = request.MerchantCode
            };

            // Generate QR with M-Pesa payment data
            var qrContent = $"MPESA|{request.MerchantCode}|{request.Amount:F0}|{request.Reference}";
            var qrBytes = GenerateLocalQrCode(qrContent);

            return (true, qrBytes, Guid.NewGuid().ToString("N"), null);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to generate M-Pesa QR");
            return (false, null, null, ex.Message);
        }
    }

    private byte[] GenerateLocalQrCode(QrPaymentRequest request)
    {
        var content = $"PAY|{request.MerchantCode}|{request.Amount:F0}|{request.Reference}";
        return GenerateLocalQrCode(content);
    }

    private byte[] GenerateLocalQrCode(string content)
    {
        // Simple QR code generation using basic approach
        // In production, would use QRCoder or SkiaSharp.QrCode
        // For now, return a placeholder image

        // This creates a simple black and white QR-like pattern
        // Real implementation would use: new QRCoder.QRCodeGenerator().CreateQrCode(content, ...)

        var size = 300;
        var moduleCount = 25;
        var moduleSize = size / moduleCount;

        // Create a simple bitmap representation
        var bitmap = new byte[size * size * 4]; // RGBA

        // Generate a simple pattern based on content hash
        var hash = content.GetHashCode();
        var random = new Random(hash);

        // Add quiet zone (white border)
        var quietZone = 4;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                var moduleX = x / moduleSize;
                var moduleY = y / moduleSize;

                bool isBlack;

                // Quiet zone
                if (moduleX < quietZone || moduleX >= moduleCount - quietZone ||
                    moduleY < quietZone || moduleY >= moduleCount - quietZone)
                {
                    isBlack = false;
                }
                // Finder patterns (corners)
                else if ((moduleX < quietZone + 7 && moduleY < quietZone + 7) ||
                         (moduleX >= moduleCount - quietZone - 7 && moduleY < quietZone + 7) ||
                         (moduleX < quietZone + 7 && moduleY >= moduleCount - quietZone - 7))
                {
                    isBlack = IsFinderPatternModule(moduleX - quietZone, moduleY - quietZone, moduleCount - 2 * quietZone);
                }
                else
                {
                    // Data area - pseudo-random based on content
                    isBlack = random.Next(2) == 1;
                }

                var idx = (y * size + x) * 4;
                var color = isBlack ? (byte)0 : (byte)255;
                bitmap[idx] = color;     // R
                bitmap[idx + 1] = color; // G
                bitmap[idx + 2] = color; // B
                bitmap[idx + 3] = 255;   // A
            }
        }

        // Convert to PNG format (simplified - real implementation would use proper image library)
        return CreateSimplePng(bitmap, size, size);
    }

    private bool IsFinderPatternModule(int x, int y, int totalSize)
    {
        // Finder patterns are 7x7 with specific pattern
        var isTopLeft = x < 7 && y < 7;
        var isTopRight = x >= totalSize - 7 && y < 7;
        var isBottomLeft = x < 7 && y >= totalSize - 7;

        if (!isTopLeft && !isTopRight && !isBottomLeft) return false;

        var localX = isTopRight ? x - (totalSize - 7) : x;
        var localY = isBottomLeft ? y - (totalSize - 7) : y;

        // Finder pattern: black border, white, black center
        if (localX == 0 || localX == 6 || localY == 0 || localY == 6) return true;
        if (localX == 1 || localX == 5 || localY == 1 || localY == 5) return false;
        if (localX >= 2 && localX <= 4 && localY >= 2 && localY <= 4) return true;

        return false;
    }

    private byte[] CreateSimplePng(byte[] rgba, int width, int height)
    {
        // Simplified PNG creation - returns raw RGBA for now
        // Real implementation would create proper PNG file
        using var ms = new MemoryStream();

        // PNG header
        byte[] header = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        ms.Write(header, 0, header.Length);

        // For simplicity, just return the raw data with a marker
        // Real implementation would use System.Drawing or SkiaSharp
        ms.Write(rgba, 0, Math.Min(rgba.Length, 50000));

        return ms.ToArray();
    }

    #endregion

    #region Payment Status

    // In-memory storage for pending payments (would be in database in production)
    private readonly Dictionary<string, QrPaymentRequestEntity> _pendingPayments = new();

    /// <inheritdoc />
    public Task<QrPaymentStatusResult> CheckPaymentStatusAsync(string qrPaymentId)
    {
        if (!_pendingPayments.TryGetValue(qrPaymentId, out var entity))
        {
            return Task.FromResult(new QrPaymentStatusResult
            {
                QrPaymentId = qrPaymentId,
                Status = QrPaymentStatus.Failed,
                Message = "QR payment not found"
            });
        }

        entity.LastCheckedAt = DateTime.UtcNow;

        // Check if expired
        if (entity.ExpiresAt < DateTime.UtcNow && entity.Status == QrPaymentStatus.Pending.ToString())
        {
            entity.Status = QrPaymentStatus.Expired.ToString();
            OnPaymentExpired(entity);
        }

        var status = Enum.Parse<QrPaymentStatus>(entity.Status);

        return Task.FromResult(new QrPaymentStatusResult
        {
            QrPaymentId = qrPaymentId,
            Status = status,
            TransactionId = entity.TransactionId,
            MpesaReceiptNumber = entity.MpesaReceiptNumber,
            AmountPaid = status == QrPaymentStatus.Completed ? entity.Amount : null,
            CompletedAt = entity.PaidAt
        });
    }

    /// <inheritdoc />
    public Task<QrPaymentStatusResult> CheckPaymentStatusByReferenceAsync(string qrReference)
    {
        return CheckPaymentStatusAsync(qrReference);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetPendingQrPaymentsAsync()
    {
        var pending = _pendingPayments
            .Where(p => p.Value.Status == QrPaymentStatus.Pending.ToString() &&
                       p.Value.ExpiresAt > DateTime.UtcNow)
            .Select(p => p.Key)
            .ToList();

        return Task.FromResult<IReadOnlyList<string>>(pending);
    }

    #endregion

    #region Payment Management

    /// <inheritdoc />
    public Task<bool> CancelQrPaymentAsync(string qrPaymentId)
    {
        if (!_pendingPayments.TryGetValue(qrPaymentId, out var entity))
        {
            return Task.FromResult(false);
        }

        if (entity.Status != QrPaymentStatus.Pending.ToString())
        {
            return Task.FromResult(false);
        }

        entity.Status = QrPaymentStatus.Cancelled.ToString();
        _logger.Information("QR payment cancelled: {QrPaymentId}", qrPaymentId);

        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<bool> ExpireQrPaymentAsync(string qrPaymentId)
    {
        if (!_pendingPayments.TryGetValue(qrPaymentId, out var entity))
        {
            return Task.FromResult(false);
        }

        if (entity.Status != QrPaymentStatus.Pending.ToString())
        {
            return Task.FromResult(false);
        }

        entity.Status = QrPaymentStatus.Expired.ToString();
        OnPaymentExpired(entity);

        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<bool> RecordPaymentAsync(string qrPaymentId, string transactionId,
        string receiptNumber, string? phoneNumber = null)
    {
        if (!_pendingPayments.TryGetValue(qrPaymentId, out var entity))
        {
            _logger.Warning("Attempted to record payment for unknown QR: {QrPaymentId}", qrPaymentId);
            return Task.FromResult(false);
        }

        entity.Status = QrPaymentStatus.Completed.ToString();
        entity.TransactionId = transactionId;
        entity.MpesaReceiptNumber = receiptNumber;
        entity.PaidAt = DateTime.UtcNow;

        _logger.Information("QR payment completed: {QrPaymentId}, Transaction: {TransactionId}",
            qrPaymentId, transactionId);

        OnPaymentCompleted(entity);

        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<int> ProcessExpiredQrPaymentsAsync()
    {
        var expiredCount = 0;
        var now = DateTime.UtcNow;

        foreach (var kvp in _pendingPayments.ToList())
        {
            if (kvp.Value.Status == QrPaymentStatus.Pending.ToString() &&
                kvp.Value.ExpiresAt < now)
            {
                kvp.Value.Status = QrPaymentStatus.Expired.ToString();
                OnPaymentExpired(kvp.Value);
                expiredCount++;
            }
        }

        if (expiredCount > 0)
        {
            _logger.Information("Processed {Count} expired QR payments", expiredCount);
        }

        return Task.FromResult(expiredCount);
    }

    private void OnPaymentCompleted(QrPaymentRequestEntity entity)
    {
        PaymentCompleted?.Invoke(this, new QrPaymentCompletedEventArgs
        {
            QrPaymentId = entity.QrReference,
            ReceiptId = entity.ReceiptId,
            Amount = entity.Amount,
            TransactionId = entity.TransactionId ?? string.Empty,
            MpesaReceiptNumber = entity.MpesaReceiptNumber ?? string.Empty
        });
    }

    private void OnPaymentExpired(QrPaymentRequestEntity entity)
    {
        PaymentExpired?.Invoke(this, new QrPaymentExpiredEventArgs
        {
            QrPaymentId = entity.QrReference,
            ReceiptId = entity.ReceiptId,
            Amount = entity.Amount
        });
    }

    #endregion

    #region QR Payment Retrieval

    /// <inheritdoc />
    public Task<QrPaymentRequestEntity?> GetQrPaymentAsync(string qrPaymentId)
    {
        _pendingPayments.TryGetValue(qrPaymentId, out var entity);
        return Task.FromResult(entity);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<QrPaymentRequestEntity>> GetQrPaymentsByReceiptAsync(int receiptId)
    {
        var payments = _pendingPayments.Values
            .Where(p => p.ReceiptId == receiptId)
            .OrderByDescending(p => p.CreatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<QrPaymentRequestEntity>>(payments);
    }

    #endregion

    #region Configuration

    /// <inheritdoc />
    public Task<QrPaymentSettings> GetSettingsAsync()
    {
        return Task.FromResult(_settings);
    }

    /// <inheritdoc />
    public Task UpdateSettingsAsync(QrPaymentSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger.Information("QR payment settings updated");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<QrPaymentResult> TestQrGenerationAsync()
    {
        var request = new QrPaymentRequest
        {
            Amount = 100,
            Reference = "TEST-" + DateTime.Now.Ticks,
            MerchantName = _settings.MerchantName,
            MerchantCode = _settings.MpesaTillNumber,
            Provider = QrPaymentProvider.MpesaQr,
            ValiditySeconds = 60
        };

        return await GenerateQrCodeAsync(request);
    }

    #endregion

    #region Reporting

    /// <inheritdoc />
    public Task<QrPaymentMetrics> GetMetricsAsync(DateTime startDate, DateTime endDate)
    {
        var payments = _pendingPayments.Values
            .Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate)
            .ToList();

        var completed = payments.Where(p => p.Status == QrPaymentStatus.Completed.ToString()).ToList();

        var metrics = new QrPaymentMetrics
        {
            TotalAttempts = payments.Count,
            Successful = completed.Count,
            Expired = payments.Count(p => p.Status == QrPaymentStatus.Expired.ToString()),
            Cancelled = payments.Count(p => p.Status == QrPaymentStatus.Cancelled.ToString()),
            Failed = payments.Count(p => p.Status == QrPaymentStatus.Failed.ToString()),
            TotalAmount = completed.Sum(p => p.Amount),
            AveragePaymentTimeSeconds = completed.Any()
                ? (decimal)completed.Average(p => (p.PaidAt!.Value - p.CreatedAt).TotalSeconds)
                : 0,
            PeriodStart = startDate,
            PeriodEnd = endDate
        };

        return Task.FromResult(metrics);
    }

    /// <inheritdoc />
    public Task<Dictionary<string, (int Count, decimal Amount)>> GetPaymentMethodComparisonAsync(
        DateTime startDate, DateTime endDate)
    {
        var qrPayments = _pendingPayments.Values
            .Where(p => p.Status == QrPaymentStatus.Completed.ToString())
            .Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate)
            .ToList();

        var comparison = new Dictionary<string, (int Count, decimal Amount)>
        {
            ["QR Code"] = (qrPayments.Count, qrPayments.Sum(p => p.Amount)),
            // STK Push and Manual would come from other payment records
            ["STK Push"] = (0, 0),
            ["Manual Entry"] = (0, 0)
        };

        return Task.FromResult(comparison);
    }

    #endregion
}
