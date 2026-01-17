using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.Protected;
using Serilog;
using System.Net;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models.Payments;
using HospitalityPOS.Infrastructure.Data;
using HospitalityPOS.Infrastructure.Services;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for <see cref="QrPaymentService"/>.
/// </summary>
public class QrPaymentServiceTests : IDisposable
{
    private readonly POSDbContext _context;
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly QrPaymentService _service;

    public QrPaymentServiceTests()
    {
        var options = new DbContextOptionsBuilder<POSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new POSDbContext(options);
        _loggerMock = new Mock<ILogger>();

        _httpHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpHandlerMock.Object);

        _service = new QrPaymentService(_context, _loggerMock.Object, _httpClient);
    }

    public void Dispose()
    {
        _context.Dispose();
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new QrPaymentService(null!, _loggerMock.Object, _httpClient));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new QrPaymentService(_context, null!, _httpClient));
    }

    [Fact]
    public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new QrPaymentService(_context, _loggerMock.Object, null!));
    }

    #endregion

    #region QR Code Generation Tests

    [Fact]
    public async Task GenerateQrCodeAsync_WithValidRequest_ReturnsSuccessfulResult()
    {
        // Arrange
        var request = new QrPaymentRequest
        {
            Amount = 1500,
            Reference = "TEST-001",
            MerchantName = "Test Store",
            MerchantCode = "123456",
            ValiditySeconds = 300
        };

        // Act
        var result = await _service.GenerateQrCodeAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.QrPaymentId.Should().NotBeNullOrEmpty();
        result.Amount.Should().Be(1500);
        result.QrCodeBytes.Should().NotBeNull();
        result.QrCodeBase64.Should().NotBeNullOrEmpty();
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task GenerateQrCodeAsync_WithZeroAmount_ReturnsFailure()
    {
        // Arrange
        var request = new QrPaymentRequest
        {
            Amount = 0,
            Reference = "TEST-001"
        };

        // Act
        var result = await _service.GenerateQrCodeAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Amount must be greater than zero");
    }

    [Fact]
    public async Task GenerateQrCodeAsync_WithNegativeAmount_ReturnsFailure()
    {
        // Arrange
        var request = new QrPaymentRequest
        {
            Amount = -100,
            Reference = "TEST-001"
        };

        // Act
        var result = await _service.GenerateQrCodeAsync(request);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateQrCodeAsync_WithEmptyReference_ReturnsFailure()
    {
        // Arrange
        var request = new QrPaymentRequest
        {
            Amount = 1000,
            Reference = ""
        };

        // Act
        var result = await _service.GenerateQrCodeAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Reference is required");
    }

    [Fact]
    public async Task GenerateQrForReceiptAsync_GeneratesQrWithReceiptReference()
    {
        // Arrange
        var receiptId = 123;
        var amount = 2500m;

        // Act
        var result = await _service.GenerateQrForReceiptAsync(receiptId, amount);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Amount.Should().Be(amount);
    }

    [Fact]
    public async Task RegenerateQrCodeAsync_WithValidId_GeneratesNewQr()
    {
        // Arrange
        var originalResult = await _service.GenerateQrForReceiptAsync(1, 1000);
        originalResult.Success.Should().BeTrue();

        // Act
        var newResult = await _service.RegenerateQrCodeAsync(originalResult.QrPaymentId);

        // Assert
        newResult.Success.Should().BeTrue();
        newResult.QrPaymentId.Should().NotBe(originalResult.QrPaymentId);
        newResult.Amount.Should().Be(originalResult.Amount);
    }

    [Fact]
    public async Task RegenerateQrCodeAsync_WithInvalidId_ReturnsFailure()
    {
        // Act
        var result = await _service.RegenerateQrCodeAsync("INVALID-ID");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    #endregion

    #region Payment Status Tests

    [Fact]
    public async Task CheckPaymentStatusAsync_ForPendingPayment_ReturnsPendingStatus()
    {
        // Arrange
        var qrResult = await _service.GenerateQrForReceiptAsync(1, 1000);

        // Act
        var status = await _service.CheckPaymentStatusAsync(qrResult.QrPaymentId);

        // Assert
        status.Status.Should().Be(QrPaymentStatus.Pending);
        status.IsComplete.Should().BeFalse();
        status.IsTerminal.Should().BeFalse();
    }

    [Fact]
    public async Task CheckPaymentStatusAsync_ForCompletedPayment_ReturnsCompletedStatus()
    {
        // Arrange
        var qrResult = await _service.GenerateQrForReceiptAsync(1, 1000);
        await _service.RecordPaymentAsync(qrResult.QrPaymentId, "TXN123", "RCPT456");

        // Act
        var status = await _service.CheckPaymentStatusAsync(qrResult.QrPaymentId);

        // Assert
        status.Status.Should().Be(QrPaymentStatus.Completed);
        status.IsComplete.Should().BeTrue();
        status.IsTerminal.Should().BeTrue();
        status.TransactionId.Should().Be("TXN123");
        status.MpesaReceiptNumber.Should().Be("RCPT456");
    }

    [Fact]
    public async Task CheckPaymentStatusAsync_ForUnknownId_ReturnsFailedStatus()
    {
        // Act
        var status = await _service.CheckPaymentStatusAsync("UNKNOWN-ID");

        // Assert
        status.Status.Should().Be(QrPaymentStatus.Failed);
        status.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task GetPendingQrPaymentsAsync_ReturnsOnlyPendingPayments()
    {
        // Arrange
        var qr1 = await _service.GenerateQrForReceiptAsync(1, 1000);
        var qr2 = await _service.GenerateQrForReceiptAsync(2, 2000);
        var qr3 = await _service.GenerateQrForReceiptAsync(3, 3000);
        await _service.CancelQrPaymentAsync(qr2.QrPaymentId);

        // Act
        var pending = await _service.GetPendingQrPaymentsAsync();

        // Assert
        pending.Should().HaveCount(2);
        pending.Should().Contain(qr1.QrPaymentId);
        pending.Should().Contain(qr3.QrPaymentId);
        pending.Should().NotContain(qr2.QrPaymentId);
    }

    #endregion

    #region Payment Management Tests

    [Fact]
    public async Task CancelQrPaymentAsync_ForPendingPayment_ReturnsTrue()
    {
        // Arrange
        var qrResult = await _service.GenerateQrForReceiptAsync(1, 1000);

        // Act
        var cancelled = await _service.CancelQrPaymentAsync(qrResult.QrPaymentId);

        // Assert
        cancelled.Should().BeTrue();
        var status = await _service.CheckPaymentStatusAsync(qrResult.QrPaymentId);
        status.Status.Should().Be(QrPaymentStatus.Cancelled);
    }

    [Fact]
    public async Task CancelQrPaymentAsync_ForCompletedPayment_ReturnsFalse()
    {
        // Arrange
        var qrResult = await _service.GenerateQrForReceiptAsync(1, 1000);
        await _service.RecordPaymentAsync(qrResult.QrPaymentId, "TXN123", "RCPT456");

        // Act
        var cancelled = await _service.CancelQrPaymentAsync(qrResult.QrPaymentId);

        // Assert
        cancelled.Should().BeFalse();
    }

    [Fact]
    public async Task CancelQrPaymentAsync_ForUnknownId_ReturnsFalse()
    {
        // Act
        var cancelled = await _service.CancelQrPaymentAsync("UNKNOWN-ID");

        // Assert
        cancelled.Should().BeFalse();
    }

    [Fact]
    public async Task ExpireQrPaymentAsync_ForPendingPayment_ReturnsTrue()
    {
        // Arrange
        var qrResult = await _service.GenerateQrForReceiptAsync(1, 1000);

        // Act
        var expired = await _service.ExpireQrPaymentAsync(qrResult.QrPaymentId);

        // Assert
        expired.Should().BeTrue();
        var status = await _service.CheckPaymentStatusAsync(qrResult.QrPaymentId);
        status.Status.Should().Be(QrPaymentStatus.Expired);
    }

    [Fact]
    public async Task RecordPaymentAsync_ForPendingPayment_ReturnsTrue()
    {
        // Arrange
        var qrResult = await _service.GenerateQrForReceiptAsync(1, 1000);

        // Act
        var recorded = await _service.RecordPaymentAsync(
            qrResult.QrPaymentId, "TXN123", "RCPT456", "254712345678");

        // Assert
        recorded.Should().BeTrue();
        var status = await _service.CheckPaymentStatusAsync(qrResult.QrPaymentId);
        status.Status.Should().Be(QrPaymentStatus.Completed);
    }

    [Fact]
    public async Task RecordPaymentAsync_ForUnknownId_ReturnsFalse()
    {
        // Act
        var recorded = await _service.RecordPaymentAsync("UNKNOWN", "TXN", "RCPT");

        // Assert
        recorded.Should().BeFalse();
    }

    [Fact]
    public async Task ProcessExpiredQrPaymentsAsync_ExpiresOldPayments()
    {
        // Arrange - Create a QR with short expiry
        var request = new QrPaymentRequest
        {
            Amount = 1000,
            Reference = "TEST-001",
            ValiditySeconds = 1 // 1 second expiry
        };
        var qrResult = await _service.GenerateQrCodeAsync(request);

        // Wait for expiry
        await Task.Delay(1500);

        // Act
        var expiredCount = await _service.ProcessExpiredQrPaymentsAsync();

        // Assert
        expiredCount.Should().BeGreaterOrEqualTo(1);
        var status = await _service.CheckPaymentStatusAsync(qrResult.QrPaymentId);
        status.Status.Should().Be(QrPaymentStatus.Expired);
    }

    #endregion

    #region QR Payment Retrieval Tests

    [Fact]
    public async Task GetQrPaymentAsync_ForExistingPayment_ReturnsEntity()
    {
        // Arrange
        var qrResult = await _service.GenerateQrForReceiptAsync(1, 1000);

        // Act
        var entity = await _service.GetQrPaymentAsync(qrResult.QrPaymentId);

        // Assert
        entity.Should().NotBeNull();
        entity!.QrReference.Should().Be(qrResult.QrPaymentId);
        entity.Amount.Should().Be(1000);
    }

    [Fact]
    public async Task GetQrPaymentAsync_ForUnknownId_ReturnsNull()
    {
        // Act
        var entity = await _service.GetQrPaymentAsync("UNKNOWN");

        // Assert
        entity.Should().BeNull();
    }

    [Fact]
    public async Task GetQrPaymentsByReceiptAsync_ReturnsAllPaymentsForReceipt()
    {
        // Arrange
        var receiptId = 42;
        await _service.GenerateQrForReceiptAsync(receiptId, 1000);
        await _service.GenerateQrForReceiptAsync(receiptId, 1500);
        await _service.GenerateQrForReceiptAsync(99, 2000); // Different receipt

        // Act
        var payments = await _service.GetQrPaymentsByReceiptAsync(receiptId);

        // Assert
        payments.Should().HaveCount(2);
        payments.Should().OnlyContain(p => p.ReceiptId == receiptId);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public async Task GetSettingsAsync_ReturnsCurrentSettings()
    {
        // Act
        var settings = await _service.GetSettingsAsync();

        // Assert
        settings.Should().NotBeNull();
        settings.DefaultValiditySeconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task UpdateSettingsAsync_UpdatesSettings()
    {
        // Arrange
        var newSettings = new QrPaymentSettings
        {
            Enabled = true,
            MpesaTillNumber = "999999",
            MerchantName = "Test Store",
            DefaultValiditySeconds = 600
        };

        // Act
        await _service.UpdateSettingsAsync(newSettings);
        var retrievedSettings = await _service.GetSettingsAsync();

        // Assert
        retrievedSettings.MpesaTillNumber.Should().Be("999999");
        retrievedSettings.MerchantName.Should().Be("Test Store");
        retrievedSettings.DefaultValiditySeconds.Should().Be(600);
    }

    [Fact]
    public async Task UpdateSettingsAsync_WithNullSettings_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.UpdateSettingsAsync(null!));
    }

    [Fact]
    public async Task TestQrGenerationAsync_GeneratesTestQr()
    {
        // Arrange
        await _service.UpdateSettingsAsync(new QrPaymentSettings
        {
            MerchantName = "Test Store",
            MpesaTillNumber = "123456"
        });

        // Act
        var result = await _service.TestQrGenerationAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Amount.Should().Be(100); // Test amount
        result.QrCodeBytes.Should().NotBeNull();
    }

    #endregion

    #region Reporting Tests

    [Fact]
    public async Task GetMetricsAsync_ReturnsCorrectMetrics()
    {
        // Arrange
        var qr1 = await _service.GenerateQrForReceiptAsync(1, 1000);
        var qr2 = await _service.GenerateQrForReceiptAsync(2, 2000);
        var qr3 = await _service.GenerateQrForReceiptAsync(3, 3000);

        await _service.RecordPaymentAsync(qr1.QrPaymentId, "TXN1", "RCPT1");
        await _service.RecordPaymentAsync(qr2.QrPaymentId, "TXN2", "RCPT2");
        await _service.CancelQrPaymentAsync(qr3.QrPaymentId);

        // Act
        var metrics = await _service.GetMetricsAsync(
            DateTime.Today.AddDays(-1),
            DateTime.Today.AddDays(1));

        // Assert
        metrics.TotalAttempts.Should().Be(3);
        metrics.Successful.Should().Be(2);
        metrics.Cancelled.Should().Be(1);
        metrics.TotalAmount.Should().Be(3000); // 1000 + 2000
        metrics.SuccessRate.Should().BeApproximately(66.67m, 0.1m);
    }

    [Fact]
    public async Task GetPaymentMethodComparisonAsync_ReturnsComparison()
    {
        // Arrange
        var qr1 = await _service.GenerateQrForReceiptAsync(1, 1000);
        await _service.RecordPaymentAsync(qr1.QrPaymentId, "TXN1", "RCPT1");

        // Act
        var comparison = await _service.GetPaymentMethodComparisonAsync(
            DateTime.Today.AddDays(-1),
            DateTime.Today.AddDays(1));

        // Assert
        comparison.Should().ContainKey("QR Code");
        comparison["QR Code"].Count.Should().Be(1);
        comparison["QR Code"].Amount.Should().Be(1000);
    }

    #endregion

    #region Event Tests

    [Fact]
    public async Task RecordPaymentAsync_RaisesPaymentCompletedEvent()
    {
        // Arrange
        var qrResult = await _service.GenerateQrForReceiptAsync(1, 1000);
        QrPaymentCompletedEventArgs? eventArgs = null;
        _service.PaymentCompleted += (s, e) => eventArgs = e;

        // Act
        await _service.RecordPaymentAsync(qrResult.QrPaymentId, "TXN123", "RCPT456");

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.QrPaymentId.Should().Be(qrResult.QrPaymentId);
        eventArgs.TransactionId.Should().Be("TXN123");
        eventArgs.MpesaReceiptNumber.Should().Be("RCPT456");
    }

    [Fact]
    public async Task ExpireQrPaymentAsync_RaisesPaymentExpiredEvent()
    {
        // Arrange
        var qrResult = await _service.GenerateQrForReceiptAsync(1, 1000);
        QrPaymentExpiredEventArgs? eventArgs = null;
        _service.PaymentExpired += (s, e) => eventArgs = e;

        // Act
        await _service.ExpireQrPaymentAsync(qrResult.QrPaymentId);

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.QrPaymentId.Should().Be(qrResult.QrPaymentId);
        eventArgs.Amount.Should().Be(1000);
    }

    #endregion

    #region QR Result Model Tests

    [Fact]
    public void QrPaymentResult_SecondsRemaining_CalculatesCorrectly()
    {
        // Arrange
        var result = new QrPaymentResult
        {
            Success = true,
            ExpiresAt = DateTime.UtcNow.AddSeconds(120)
        };

        // Assert
        result.SecondsRemaining.Should().BeGreaterThan(100);
        result.SecondsRemaining.Should().BeLessThanOrEqualTo(120);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void QrPaymentResult_IsValid_ReturnsFalseWhenExpired()
    {
        // Arrange
        var result = new QrPaymentResult
        {
            Success = true,
            ExpiresAt = DateTime.UtcNow.AddSeconds(-10) // Already expired
        };

        // Assert
        result.IsValid.Should().BeFalse();
        result.SecondsRemaining.Should().Be(0);
    }

    [Fact]
    public void QrPaymentResult_FailureFactory_CreatesCorrectResult()
    {
        // Act
        var result = QrPaymentResult.Failure("Test error");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Test error");
    }

    #endregion

    #region QR Payment Status Tests

    [Fact]
    public void QrPaymentStatusResult_IsTerminal_CorrectForAllStatuses()
    {
        // Arrange & Assert
        new QrPaymentStatusResult { Status = QrPaymentStatus.Pending }.IsTerminal.Should().BeFalse();
        new QrPaymentStatusResult { Status = QrPaymentStatus.Scanned }.IsTerminal.Should().BeFalse();
        new QrPaymentStatusResult { Status = QrPaymentStatus.Completed }.IsTerminal.Should().BeTrue();
        new QrPaymentStatusResult { Status = QrPaymentStatus.Expired }.IsTerminal.Should().BeTrue();
        new QrPaymentStatusResult { Status = QrPaymentStatus.Cancelled }.IsTerminal.Should().BeTrue();
        new QrPaymentStatusResult { Status = QrPaymentStatus.Failed }.IsTerminal.Should().BeTrue();
    }

    [Fact]
    public void QrPaymentStatusResult_IsComplete_OnlyTrueForCompleted()
    {
        // Arrange & Assert
        new QrPaymentStatusResult { Status = QrPaymentStatus.Completed }.IsComplete.Should().BeTrue();
        new QrPaymentStatusResult { Status = QrPaymentStatus.Pending }.IsComplete.Should().BeFalse();
        new QrPaymentStatusResult { Status = QrPaymentStatus.Expired }.IsComplete.Should().BeFalse();
    }

    #endregion
}
