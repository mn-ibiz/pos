using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;
using HospitalityPOS.Infrastructure.Services;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for EtimsService.
/// </summary>
public class EtimsServiceTests : IDisposable
{
    private readonly POSDbContext _context;
    private readonly Mock<ILogger<EtimsService>> _loggerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly EtimsService _service;

    public EtimsServiceTests()
    {
        var options = new DbContextOptionsBuilder<POSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new POSDbContext(options);
        _loggerMock = new Mock<ILogger<EtimsService>>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();

        // Setup default HTTP client
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"status\":\"success\"}")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient("EtimsApi")).Returns(httpClient);

        _service = new EtimsService(_context, _loggerMock.Object, _httpClientFactoryMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region Device Management Tests

    [Fact]
    public async Task RegisterDeviceAsync_ShouldRegisterDevice()
    {
        // Arrange
        var device = new EtimsDevice
        {
            DeviceSerialNumber = "ETIMS-001",
            ControlUnitId = "CU-001",
            BusinessPin = "P051234567A",
            BusinessName = "Test Business",
            IsPrimary = true
        };

        // Act
        var result = await _service.RegisterDeviceAsync(device);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Status.Should().Be(EtimsDeviceStatus.Registered);
        result.RegistrationDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RegisterDeviceAsync_WithPrimary_ShouldDeactivateOtherPrimaryDevices()
    {
        // Arrange
        var existingDevice = new EtimsDevice
        {
            DeviceSerialNumber = "ETIMS-001",
            ControlUnitId = "CU-001",
            BusinessPin = "P051234567A",
            BusinessName = "Test Business",
            IsPrimary = true,
            Status = EtimsDeviceStatus.Active
        };
        _context.EtimsDevices.Add(existingDevice);
        await _context.SaveChangesAsync();

        var newDevice = new EtimsDevice
        {
            DeviceSerialNumber = "ETIMS-002",
            ControlUnitId = "CU-002",
            BusinessPin = "P051234567A",
            BusinessName = "Test Business",
            IsPrimary = true
        };

        // Act
        await _service.RegisterDeviceAsync(newDevice);

        // Assert
        var updatedExisting = await _context.EtimsDevices.FindAsync(existingDevice.Id);
        updatedExisting!.IsPrimary.Should().BeFalse();
    }

    [Fact]
    public async Task GetActiveDeviceAsync_ShouldReturnPrimaryActiveDevice()
    {
        // Arrange
        var device = new EtimsDevice
        {
            DeviceSerialNumber = "ETIMS-001",
            ControlUnitId = "CU-001",
            BusinessPin = "P051234567A",
            BusinessName = "Test Business",
            IsPrimary = true,
            Status = EtimsDeviceStatus.Active
        };
        _context.EtimsDevices.Add(device);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetActiveDeviceAsync();

        // Assert
        result.Should().NotBeNull();
        result!.DeviceSerialNumber.Should().Be("ETIMS-001");
    }

    [Fact]
    public async Task GetActiveDeviceAsync_WhenNoActiveDevice_ShouldReturnNull()
    {
        // Arrange - no devices

        // Act
        var result = await _service.GetActiveDeviceAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ActivateDeviceAsync_ShouldActivateAndSetAsPrimary()
    {
        // Arrange
        var device = new EtimsDevice
        {
            DeviceSerialNumber = "ETIMS-001",
            ControlUnitId = "CU-001",
            BusinessPin = "P051234567A",
            BusinessName = "Test Business",
            Status = EtimsDeviceStatus.Registered
        };
        _context.EtimsDevices.Add(device);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ActivateDeviceAsync(device.Id);

        // Assert
        result.Should().BeTrue();
        var updatedDevice = await _context.EtimsDevices.FindAsync(device.Id);
        updatedDevice!.Status.Should().Be(EtimsDeviceStatus.Active);
        updatedDevice.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateDeviceAsync_ShouldDeactivateDevice()
    {
        // Arrange
        var device = new EtimsDevice
        {
            DeviceSerialNumber = "ETIMS-001",
            ControlUnitId = "CU-001",
            BusinessPin = "P051234567A",
            BusinessName = "Test Business",
            IsPrimary = true,
            Status = EtimsDeviceStatus.Active
        };
        _context.EtimsDevices.Add(device);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeactivateDeviceAsync(device.Id);

        // Assert
        result.Should().BeTrue();
        var updatedDevice = await _context.EtimsDevices.FindAsync(device.Id);
        updatedDevice!.Status.Should().Be(EtimsDeviceStatus.Deactivated);
        updatedDevice.IsPrimary.Should().BeFalse();
    }

    #endregion

    #region Invoice Generation Tests

    [Fact]
    public async Task GenerateInvoiceAsync_ShouldGenerateInvoiceFromReceipt()
    {
        // Arrange
        await SeedActiveDevice();
        var receipt = await SeedReceipt();

        // Act
        var result = await _service.GenerateInvoiceAsync(receipt.Id);

        // Assert
        result.Should().NotBeNull();
        result.ReceiptId.Should().Be(receipt.Id);
        result.InvoiceNumber.Should().NotBeEmpty();
        result.Status.Should().Be(EtimsSubmissionStatus.Pending);
        result.TotalAmount.Should().Be(receipt.Total);
        result.Items.Should().HaveCount(receipt.Items.Count);
    }

    [Fact]
    public async Task GenerateInvoiceAsync_ForExistingInvoice_ShouldReturnExisting()
    {
        // Arrange
        await SeedActiveDevice();
        var receipt = await SeedReceipt();
        var firstInvoice = await _service.GenerateInvoiceAsync(receipt.Id);

        // Act
        var secondInvoice = await _service.GenerateInvoiceAsync(receipt.Id);

        // Assert
        secondInvoice.Id.Should().Be(firstInvoice.Id);
    }

    [Fact]
    public async Task GenerateInvoiceAsync_WithoutActiveDevice_ShouldThrow()
    {
        // Arrange
        var receipt = await SeedReceipt();

        // Act
        Func<Task> act = () => _service.GenerateInvoiceAsync(receipt.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No active eTIMS device*");
    }

    [Fact]
    public async Task GenerateInvoiceNumberAsync_ShouldIncrementAndFormat()
    {
        // Arrange
        var device = await SeedActiveDevice();
        var initialNumber = device.LastInvoiceNumber;

        // Act
        var invoiceNumber = await _service.GenerateInvoiceNumberAsync();

        // Assert
        invoiceNumber.Should().Contain(device.ControlUnitId);
        invoiceNumber.Should().Contain(device.BranchCode);
        invoiceNumber.Should().Contain(DateTime.Now.Year.ToString());

        var updatedDevice = await _context.EtimsDevices.FindAsync(device.Id);
        updatedDevice!.LastInvoiceNumber.Should().Be(initialNumber + 1);
    }

    #endregion

    #region Invoice Submission Tests

    [Fact]
    public async Task SubmitInvoiceAsync_InSandbox_ShouldSucceed()
    {
        // Arrange
        var device = await SeedActiveDevice();
        var receipt = await SeedReceipt();
        var invoice = await _service.GenerateInvoiceAsync(receipt.Id);

        // Act
        var result = await _service.SubmitInvoiceAsync(invoice.Id);

        // Assert
        result.Status.Should().Be(EtimsSubmissionStatus.Accepted);
        result.SubmittedAt.Should().NotBeNull();
        result.QrCode.Should().NotBeNullOrEmpty();
        result.ReceiptSignature.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SubmitInvoiceAsync_AlreadyAccepted_ShouldReturnWithoutResubmitting()
    {
        // Arrange
        var device = await SeedActiveDevice();
        var receipt = await SeedReceipt();
        var invoice = await _service.GenerateInvoiceAsync(receipt.Id);
        await _service.SubmitInvoiceAsync(invoice.Id);

        // Act
        var result = await _service.SubmitInvoiceAsync(invoice.Id);

        // Assert
        result.SubmissionAttempts.Should().Be(1);
    }

    [Fact]
    public async Task GetInvoiceByReceiptIdAsync_ShouldReturnInvoice()
    {
        // Arrange
        await SeedActiveDevice();
        var receipt = await SeedReceipt();
        await _service.GenerateInvoiceAsync(receipt.Id);

        // Act
        var result = await _service.GetInvoiceByReceiptIdAsync(receipt.Id);

        // Assert
        result.Should().NotBeNull();
        result!.ReceiptId.Should().Be(receipt.Id);
    }

    [Fact]
    public async Task GetInvoicesByStatusAsync_ShouldReturnMatchingInvoices()
    {
        // Arrange
        await SeedActiveDevice();
        var receipt = await SeedReceipt();
        await _service.GenerateInvoiceAsync(receipt.Id);

        // Act
        var result = await _service.GetInvoicesByStatusAsync(EtimsSubmissionStatus.Pending);

        // Assert
        result.Should().HaveCount(1);
    }

    #endregion

    #region Credit Note Tests

    [Fact]
    public async Task GenerateCreditNoteAsync_ShouldGenerateCreditNote()
    {
        // Arrange
        var device = await SeedActiveDevice();
        var receipt = await SeedReceipt();
        var invoice = await _service.GenerateInvoiceAsync(receipt.Id);
        await _service.SubmitInvoiceAsync(invoice.Id);

        // Act
        var result = await _service.GenerateCreditNoteAsync(invoice.Id, "Customer return");

        // Assert
        result.Should().NotBeNull();
        result.OriginalInvoiceId.Should().Be(invoice.Id);
        result.OriginalInvoiceNumber.Should().Be(invoice.InvoiceNumber);
        result.Reason.Should().Be("Customer return");
        result.CreditAmount.Should().Be(invoice.TotalAmount);
        result.Items.Should().HaveCount(invoice.Items.Count);
    }

    [Fact]
    public async Task SubmitCreditNoteAsync_InSandbox_ShouldSucceed()
    {
        // Arrange
        var device = await SeedActiveDevice();
        var receipt = await SeedReceipt();
        var invoice = await _service.GenerateInvoiceAsync(receipt.Id);
        await _service.SubmitInvoiceAsync(invoice.Id);
        var creditNote = await _service.GenerateCreditNoteAsync(invoice.Id, "Customer return");

        // Act
        var result = await _service.SubmitCreditNoteAsync(creditNote.Id);

        // Assert
        result.Status.Should().Be(EtimsSubmissionStatus.Accepted);
        result.SubmittedAt.Should().NotBeNull();
        result.KraSignature.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateCreditNoteNumberAsync_ShouldIncrementAndFormat()
    {
        // Arrange
        var device = await SeedActiveDevice();
        var initialNumber = device.LastCreditNoteNumber;

        // Act
        var creditNoteNumber = await _service.GenerateCreditNoteNumberAsync();

        // Assert
        creditNoteNumber.Should().StartWith("CN-");
        creditNoteNumber.Should().Contain(device.ControlUnitId);

        var updatedDevice = await _context.EtimsDevices.FindAsync(device.Id);
        updatedDevice!.LastCreditNoteNumber.Should().Be(initialNumber + 1);
    }

    #endregion

    #region Queue Management Tests

    [Fact]
    public async Task QueueForSubmissionAsync_ShouldCreateQueueEntry()
    {
        // Arrange
        var device = await SeedActiveDevice();
        var receipt = await SeedReceipt();
        var invoice = await _service.GenerateInvoiceAsync(receipt.Id);

        // Act
        var result = await _service.QueueForSubmissionAsync(EtimsDocumentType.TaxInvoice, invoice.Id);

        // Assert
        result.Should().NotBeNull();
        result.DocumentType.Should().Be(EtimsDocumentType.TaxInvoice);
        result.DocumentId.Should().Be(invoice.Id);
        result.Status.Should().Be(EtimsSubmissionStatus.Queued);
        result.QueuedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetPendingQueueEntriesAsync_ShouldReturnQueuedEntries()
    {
        // Arrange
        var device = await SeedActiveDevice();
        var receipt = await SeedReceipt();
        var invoice = await _service.GenerateInvoiceAsync(receipt.Id);
        await _service.QueueForSubmissionAsync(EtimsDocumentType.TaxInvoice, invoice.Id);

        // Act
        var result = await _service.GetPendingQueueEntriesAsync();

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetPendingQueueEntriesAsync_ShouldExcludeFutureRetries()
    {
        // Arrange
        var entry = new EtimsQueueEntry
        {
            DocumentType = EtimsDocumentType.TaxInvoice,
            DocumentId = 1,
            Status = EtimsSubmissionStatus.Queued,
            RetryAfter = DateTime.UtcNow.AddHours(1) // Future retry time
        };
        _context.EtimsQueue.Add(entry);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPendingQueueEntriesAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessQueueAsync_ShouldProcessPendingInvoices()
    {
        // Arrange
        var device = await SeedActiveDevice();
        var receipt = await SeedReceipt();
        var invoice = await _service.GenerateInvoiceAsync(receipt.Id);
        await _service.QueueForSubmissionAsync(EtimsDocumentType.TaxInvoice, invoice.Id);

        // Act
        await _service.ProcessQueueAsync();

        // Assert
        var processedInvoice = await _context.EtimsInvoices.FindAsync(invoice.Id);
        processedInvoice!.Status.Should().Be(EtimsSubmissionStatus.Accepted);
    }

    [Fact]
    public async Task GetQueueCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            _context.EtimsQueue.Add(new EtimsQueueEntry
            {
                DocumentType = EtimsDocumentType.TaxInvoice,
                DocumentId = i,
                Status = EtimsSubmissionStatus.Queued
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetQueueCountAsync();

        // Assert
        result.Should().Be(5);
    }

    [Fact]
    public async Task GetQueueStatsAsync_ShouldReturnStats()
    {
        // Arrange
        _context.EtimsQueue.AddRange(new[]
        {
            new EtimsQueueEntry { DocumentType = EtimsDocumentType.TaxInvoice, DocumentId = 1, Status = EtimsSubmissionStatus.Queued },
            new EtimsQueueEntry { DocumentType = EtimsDocumentType.TaxInvoice, DocumentId = 2, Status = EtimsSubmissionStatus.Failed },
            new EtimsQueueEntry { DocumentType = EtimsDocumentType.TaxInvoice, DocumentId = 3, Status = EtimsSubmissionStatus.Accepted, CompletedAt = DateTime.UtcNow },
            new EtimsQueueEntry { DocumentType = EtimsDocumentType.CreditNote, DocumentId = 4, Status = EtimsSubmissionStatus.Failed }
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetQueueStatsAsync();

        // Assert
        result.TotalPending.Should().Be(1);
        result.TotalFailed.Should().Be(2);
        result.TotalSubmitted.Should().Be(1);
        result.FailedInvoices.Should().Be(1);
        result.FailedCreditNotes.Should().Be(1);
    }

    #endregion

    #region Dashboard & Reports Tests

    [Fact]
    public async Task GetDashboardDataAsync_ShouldReturnDashboardData()
    {
        // Arrange
        var device = await SeedActiveDevice();
        var receipt = await SeedReceipt();
        var invoice = await _service.GenerateInvoiceAsync(receipt.Id);
        await _service.SubmitInvoiceAsync(invoice.Id);

        // Act
        var result = await _service.GetDashboardDataAsync();

        // Assert
        result.IsDeviceRegistered.Should().BeTrue();
        result.IsDeviceActive.Should().BeTrue();
        result.DeviceSerialNumber.Should().Be(device.DeviceSerialNumber);
        result.TodayInvoicesSubmitted.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task GetComplianceReportAsync_ShouldReturnReport()
    {
        // Arrange
        var device = await SeedActiveDevice();
        var receipt = await SeedReceipt();
        var invoice = await _service.GenerateInvoiceAsync(receipt.Id);
        await _service.SubmitInvoiceAsync(invoice.Id);

        var startDate = DateTime.Today.AddDays(-1);
        var endDate = DateTime.Today.AddDays(1);

        // Act
        var result = await _service.GetComplianceReportAsync(startDate, endDate);

        // Assert
        result.StartDate.Should().Be(startDate);
        result.EndDate.Should().Be(endDate);
        result.SubmittedInvoices.Should().BeGreaterOrEqualTo(1);
        result.TotalSalesAmount.Should().BeGreaterThan(0);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task ValidateCustomerPinAsync_WithValidPin_ShouldReturnTrue()
    {
        // Arrange
        var validPin = "P051234567A";

        // Act
        var result = await _service.ValidateCustomerPinAsync(validPin);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("ABC")]
    [InlineData("12345678901")]
    [InlineData("1234567890A")]
    [InlineData("A12345678A0")]
    public async Task ValidateCustomerPinAsync_WithInvalidPin_ShouldReturnFalse(string invalidPin)
    {
        // Act
        var result = await _service.ValidateCustomerPinAsync(invalidPin);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task LookupCustomerByPinAsync_WithValidPin_ShouldReturnCustomerInfo()
    {
        // Arrange
        var validPin = "P051234567A";

        // Act
        var result = await _service.LookupCustomerByPinAsync(validPin);

        // Assert
        result.Should().NotBeNull();
        result!.Pin.Should().Be(validPin);
        result.IsValidPin.Should().BeTrue();
    }

    [Fact]
    public async Task LookupCustomerByPinAsync_WithInvalidPin_ShouldReturnNull()
    {
        // Arrange
        var invalidPin = "INVALID";

        // Act
        var result = await _service.LookupCustomerByPinAsync(invalidPin);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Retry Tests

    [Fact]
    public async Task RetryFailedSubmissionsAsync_ShouldQueueFailedInvoices()
    {
        // Arrange
        var device = await SeedActiveDevice();
        var invoice = new EtimsInvoice
        {
            ReceiptId = 1,
            DeviceId = device.Id,
            InvoiceNumber = "INV-001",
            InvoiceDate = DateTime.UtcNow,
            DocumentType = EtimsDocumentType.TaxInvoice,
            Status = EtimsSubmissionStatus.Failed,
            SubmissionAttempts = 1
        };
        _context.EtimsInvoices.Add(invoice);
        await _context.SaveChangesAsync();

        // Act
        await _service.RetryFailedSubmissionsAsync();

        // Assert
        var queueCount = await _service.GetQueueCountAsync();
        queueCount.Should().Be(1);
    }

    #endregion

    #region Helper Methods

    private async Task<EtimsDevice> SeedActiveDevice()
    {
        var device = new EtimsDevice
        {
            DeviceSerialNumber = "ETIMS-TEST-001",
            ControlUnitId = "CU-TEST-001",
            BusinessPin = "P051234567A",
            BusinessName = "Test Business",
            BranchCode = "001",
            BranchName = "Main Branch",
            ApiBaseUrl = "https://etims.kra.go.ke",
            IsPrimary = true,
            Status = EtimsDeviceStatus.Active,
            Environment = "Sandbox"
        };
        _context.EtimsDevices.Add(device);
        await _context.SaveChangesAsync();
        return device;
    }

    private async Task<Receipt> SeedReceipt()
    {
        // Create product
        var product = new Product
        {
            Name = "Test Product",
            SKU = "TEST-001",
            UnitPrice = 1000m,
            IsActive = true
        };
        _context.Products.Add(product);

        // Create receipt
        var receipt = new Receipt
        {
            ReceiptNumber = "RCP-001",
            Status = ReceiptStatus.Settled,
            SubTotal = 2000m,
            TaxAmount = 320m,
            Total = 2000m,
            CreatedAt = DateTime.UtcNow
        };
        _context.Receipts.Add(receipt);
        await _context.SaveChangesAsync();

        // Add items
        receipt.Items.Add(new ReceiptItem
        {
            ReceiptId = receipt.Id,
            ProductId = product.Id,
            ProductName = product.Name,
            Quantity = 2,
            UnitPrice = 1000m,
            TotalPrice = 2000m,
            Product = product
        });
        await _context.SaveChangesAsync();

        return receipt;
    }

    #endregion
}
