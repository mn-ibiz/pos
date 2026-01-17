using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models;
using HospitalityPOS.Infrastructure.Data;
using HospitalityPOS.Infrastructure.Services;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for the ReceiptVoidService class.
/// Tests cover void processing, stock restoration, and audit logging.
/// </summary>
public class ReceiptVoidServiceTests : IDisposable
{
    private readonly POSDbContext _context;
    private readonly Mock<ISessionService> _sessionServiceMock;
    private readonly Mock<IInventoryService> _inventoryServiceMock;
    private readonly Mock<ILogger> _loggerMock;
    private readonly ReceiptVoidService _voidService;
    private const int TestUserId = 1;

    public ReceiptVoidServiceTests()
    {
        var options = new DbContextOptionsBuilder<POSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new POSDbContext(options);
        _sessionServiceMock = new Mock<ISessionService>();
        _inventoryServiceMock = new Mock<IInventoryService>();
        _loggerMock = new Mock<ILogger>();

        _sessionServiceMock.Setup(s => s.CurrentUserId).Returns(TestUserId);

        _voidService = new ReceiptVoidService(
            _context,
            _sessionServiceMock.Object,
            _inventoryServiceMock.Object,
            _loggerMock.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create test user
        var user = new User
        {
            Id = TestUserId,
            Username = "testuser",
            DisplayName = "Test User",
            PasswordHash = "hash",
            IsActive = true
        };
        _context.Users.Add(user);

        // Create work period
        var workPeriod = new WorkPeriod
        {
            Id = 1,
            OpenedByUserId = TestUserId,
            OpenedAt = DateTime.UtcNow.AddHours(-4),
            Status = WorkPeriodStatus.Open
        };
        _context.WorkPeriods.Add(workPeriod);

        // Create void reasons
        var voidReasons = new[]
        {
            new VoidReason { Id = 1, Name = "Wrong Order", IsActive = true, RequiresNote = false, DisplayOrder = 1 },
            new VoidReason { Id = 2, Name = "Customer Changed Mind", IsActive = true, RequiresNote = false, DisplayOrder = 2 },
            new VoidReason { Id = 3, Name = "Other", IsActive = true, RequiresNote = true, DisplayOrder = 99 }
        };
        _context.VoidReasons.AddRange(voidReasons);

        _context.SaveChanges();
    }

    private async Task<Receipt> CreateTestReceiptAsync(
        ReceiptStatus status = ReceiptStatus.Pending,
        decimal totalAmount = 1000m)
    {
        var receipt = new Receipt
        {
            ReceiptNumber = $"R-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid():N}".Substring(0, 20),
            OwnerId = TestUserId,
            WorkPeriodId = 1,
            Status = status,
            Subtotal = totalAmount,
            TotalAmount = totalAmount
        };

        _context.Receipts.Add(receipt);
        await _context.SaveChangesAsync();

        // Add test items
        var product = new Product
        {
            Code = $"PROD-{Guid.NewGuid():N}".Substring(0, 15),
            Name = "Test Product",
            SellingPrice = 100m,
            TrackInventory = true,
            IsActive = true
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var receiptItem = new ReceiptItem
        {
            ReceiptId = receipt.Id,
            ProductId = product.Id,
            ProductName = product.Name,
            Quantity = 2,
            UnitPrice = 100m,
            TotalAmount = 200m
        };
        _context.ReceiptItems.Add(receiptItem);
        await _context.SaveChangesAsync();

        return await _context.Receipts
            .Include(r => r.ReceiptItems)
                .ThenInclude(ri => ri.Product)
            .FirstAsync(r => r.Id == receipt.Id);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new ReceiptVoidService(
            null!,
            _sessionServiceMock.Object,
            _inventoryServiceMock.Object,
            _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public void Constructor_WithNullInventoryService_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new ReceiptVoidService(
            _context,
            _sessionServiceMock.Object,
            null!,
            _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("inventoryService");
    }

    #endregion

    #region VoidReceiptAsync Tests

    [Fact]
    public async Task VoidReceiptAsync_ShouldVoidReceipt_WhenValidRequest()
    {
        // Arrange
        var receipt = await CreateTestReceiptAsync();
        var request = new VoidRequest
        {
            ReceiptId = receipt.Id,
            VoidReasonId = 1,
            AdditionalNotes = "Test void"
        };

        _inventoryServiceMock
            .Setup(s => s.RestoreStockForVoidAsync(It.IsAny<Receipt>()))
            .ReturnsAsync(new List<StockMovement> { new StockMovement() });

        // Act
        var result = await _voidService.VoidReceiptAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.VoidRecord.Should().NotBeNull();
        result.Receipt.Should().NotBeNull();
        result.Receipt!.Status.Should().Be(ReceiptStatus.Voided);
    }

    [Fact]
    public async Task VoidReceiptAsync_ShouldCallRestoreStockForVoidAsync()
    {
        // Arrange
        var receipt = await CreateTestReceiptAsync();
        var request = new VoidRequest
        {
            ReceiptId = receipt.Id,
            VoidReasonId = 1
        };

        _inventoryServiceMock
            .Setup(s => s.RestoreStockForVoidAsync(It.IsAny<Receipt>()))
            .ReturnsAsync(new List<StockMovement> { new StockMovement() });

        // Act
        await _voidService.VoidReceiptAsync(request);

        // Assert
        _inventoryServiceMock.Verify(
            s => s.RestoreStockForVoidAsync(It.Is<Receipt>(r => r.Id == receipt.Id)),
            Times.Once);
    }

    [Fact]
    public async Task VoidReceiptAsync_ShouldFail_WhenReceiptAlreadyVoided()
    {
        // Arrange
        var receipt = await CreateTestReceiptAsync(status: ReceiptStatus.Voided);
        var request = new VoidRequest
        {
            ReceiptId = receipt.Id,
            VoidReasonId = 1
        };

        // Act
        var result = await _voidService.VoidReceiptAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already voided");
    }

    [Fact]
    public async Task VoidReceiptAsync_ShouldFail_WhenVoidReasonNotFound()
    {
        // Arrange
        var receipt = await CreateTestReceiptAsync();
        var request = new VoidRequest
        {
            ReceiptId = receipt.Id,
            VoidReasonId = 99999 // Non-existent reason
        };

        // Act
        var result = await _voidService.VoidReceiptAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid void reason");
    }

    [Fact]
    public async Task VoidReceiptAsync_ShouldFail_WhenNotesRequiredButNotProvided()
    {
        // Arrange
        var receipt = await CreateTestReceiptAsync();
        var request = new VoidRequest
        {
            ReceiptId = receipt.Id,
            VoidReasonId = 3, // "Other" reason requires note
            AdditionalNotes = null
        };

        // Act
        var result = await _voidService.VoidReceiptAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Additional notes are required");
    }

    [Fact]
    public async Task VoidReceiptAsync_ShouldFail_WhenNoUserLoggedIn()
    {
        // Arrange
        _sessionServiceMock.Setup(s => s.CurrentUserId).Returns(0);
        var receipt = await CreateTestReceiptAsync();
        var request = new VoidRequest
        {
            ReceiptId = receipt.Id,
            VoidReasonId = 1
        };

        // Act
        var result = await _voidService.VoidReceiptAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No user is currently logged in");
    }

    [Fact]
    public async Task VoidReceiptAsync_ShouldCreateAuditLog()
    {
        // Arrange
        var receipt = await CreateTestReceiptAsync();
        var request = new VoidRequest
        {
            ReceiptId = receipt.Id,
            VoidReasonId = 1,
            AdditionalNotes = "Test audit"
        };

        _inventoryServiceMock
            .Setup(s => s.RestoreStockForVoidAsync(It.IsAny<Receipt>()))
            .ReturnsAsync(new List<StockMovement>());

        // Act
        await _voidService.VoidReceiptAsync(request);

        // Assert
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(al => al.EntityType == "Receipt" && al.EntityId == receipt.Id);

        auditLog.Should().NotBeNull();
        auditLog!.Action.Should().Be(AuditActionType.ReceiptVoided.ToString());
    }

    #endregion

    #region CanVoidReceiptAsync Tests

    [Fact]
    public async Task CanVoidReceiptAsync_ShouldReturnTrue_WhenReceiptCanBeVoided()
    {
        // Arrange
        var receipt = await CreateTestReceiptAsync();

        // Act
        var (canVoid, reason) = await _voidService.CanVoidReceiptAsync(receipt.Id);

        // Assert
        canVoid.Should().BeTrue();
        reason.Should().BeNull();
    }

    [Fact]
    public async Task CanVoidReceiptAsync_ShouldReturnFalse_WhenReceiptNotFound()
    {
        // Act
        var (canVoid, reason) = await _voidService.CanVoidReceiptAsync(99999);

        // Assert
        canVoid.Should().BeFalse();
        reason.Should().Contain("not found");
    }

    [Fact]
    public async Task CanVoidReceiptAsync_ShouldReturnFalse_WhenAlreadyVoided()
    {
        // Arrange
        var receipt = await CreateTestReceiptAsync(status: ReceiptStatus.Voided);

        // Act
        var (canVoid, reason) = await _voidService.CanVoidReceiptAsync(receipt.Id);

        // Assert
        canVoid.Should().BeFalse();
        reason.Should().Contain("already voided");
    }

    #endregion

    #region GetVoidReasonsAsync Tests

    [Fact]
    public async Task GetVoidReasonsAsync_ShouldReturnActiveReasons()
    {
        // Act
        var reasons = await _voidService.GetVoidReasonsAsync();

        // Assert
        reasons.Should().HaveCount(3);
        reasons.Should().AllSatisfy(r => r.IsActive.Should().BeTrue());
    }

    [Fact]
    public async Task GetVoidReasonsAsync_ShouldReturnOrderedByDisplayOrder()
    {
        // Act
        var reasons = (await _voidService.GetVoidReasonsAsync()).ToList();

        // Assert
        reasons[0].Name.Should().Be("Wrong Order");
        reasons[1].Name.Should().Be("Customer Changed Mind");
        reasons[2].Name.Should().Be("Other");
    }

    #endregion

    #region GetVoidRecordAsync Tests

    [Fact]
    public async Task GetVoidRecordAsync_ShouldReturnVoidRecord_WhenExists()
    {
        // Arrange
        var receipt = await CreateTestReceiptAsync();
        var request = new VoidRequest
        {
            ReceiptId = receipt.Id,
            VoidReasonId = 1,
            AdditionalNotes = "Test"
        };

        _inventoryServiceMock
            .Setup(s => s.RestoreStockForVoidAsync(It.IsAny<Receipt>()))
            .ReturnsAsync(new List<StockMovement>());

        await _voidService.VoidReceiptAsync(request);

        // Act
        var voidRecord = await _voidService.GetVoidRecordAsync(receipt.Id);

        // Assert
        voidRecord.Should().NotBeNull();
        voidRecord!.ReceiptId.Should().Be(receipt.Id);
        voidRecord.VoidReasonId.Should().Be(1);
    }

    [Fact]
    public async Task GetVoidRecordAsync_ShouldReturnNull_WhenNoVoidRecord()
    {
        // Arrange
        var receipt = await CreateTestReceiptAsync();

        // Act
        var voidRecord = await _voidService.GetVoidRecordAsync(receipt.Id);

        // Assert
        voidRecord.Should().BeNull();
    }

    #endregion

    #region eTIMS Credit Note Integration Tests

    [Fact]
    public async Task VoidReceiptAsync_ShouldSubmitCreditNote_WhenEtimsInvoiceExists()
    {
        // Arrange
        var receipt = await CreateTestReceiptAsync();
        var etimsServiceMock = new Mock<IEtimsService>();

        var existingInvoice = new EtimsInvoice
        {
            Id = 1,
            ReceiptId = receipt.Id,
            InvoiceNumber = "INV-001",
            Status = EtimsSubmissionStatus.Accepted
        };

        var creditNote = new EtimsCreditNote
        {
            Id = 1,
            OriginalInvoiceId = 1,
            CreditNoteNumber = "CN-001",
            Status = EtimsSubmissionStatus.Accepted
        };

        etimsServiceMock
            .Setup(s => s.GetInvoiceByReceiptIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingInvoice);

        etimsServiceMock
            .Setup(s => s.GenerateCreditNoteAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(creditNote);

        etimsServiceMock
            .Setup(s => s.SubmitCreditNoteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(creditNote);

        _inventoryServiceMock
            .Setup(s => s.RestoreStockForVoidAsync(It.IsAny<Receipt>()))
            .ReturnsAsync(new List<StockMovement>());

        var voidServiceWithEtims = new ReceiptVoidService(
            _context,
            _sessionServiceMock.Object,
            _inventoryServiceMock.Object,
            _loggerMock.Object,
            etimsServiceMock.Object);

        var request = new VoidRequest
        {
            ReceiptId = receipt.Id,
            VoidReasonId = 1,
            AdditionalNotes = "Test void"
        };

        // Act
        var result = await voidServiceWithEtims.VoidReceiptAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        etimsServiceMock.Verify(
            s => s.GenerateCreditNoteAsync(existingInvoice.Id, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
        etimsServiceMock.Verify(
            s => s.SubmitCreditNoteAsync(creditNote.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task VoidReceiptAsync_ShouldNotSubmitCreditNote_WhenNoEtimsInvoice()
    {
        // Arrange
        var receipt = await CreateTestReceiptAsync();
        var etimsServiceMock = new Mock<IEtimsService>();

        etimsServiceMock
            .Setup(s => s.GetInvoiceByReceiptIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EtimsInvoice?)null);

        _inventoryServiceMock
            .Setup(s => s.RestoreStockForVoidAsync(It.IsAny<Receipt>()))
            .ReturnsAsync(new List<StockMovement>());

        var voidServiceWithEtims = new ReceiptVoidService(
            _context,
            _sessionServiceMock.Object,
            _inventoryServiceMock.Object,
            _loggerMock.Object,
            etimsServiceMock.Object);

        var request = new VoidRequest
        {
            ReceiptId = receipt.Id,
            VoidReasonId = 1
        };

        // Act
        var result = await voidServiceWithEtims.VoidReceiptAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        etimsServiceMock.Verify(
            s => s.GenerateCreditNoteAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task VoidReceiptAsync_ShouldNotSubmitCreditNote_WhenInvoiceNotAccepted()
    {
        // Arrange
        var receipt = await CreateTestReceiptAsync();
        var etimsServiceMock = new Mock<IEtimsService>();

        var pendingInvoice = new EtimsInvoice
        {
            Id = 1,
            ReceiptId = receipt.Id,
            InvoiceNumber = "INV-001",
            Status = EtimsSubmissionStatus.Pending // Not yet accepted
        };

        etimsServiceMock
            .Setup(s => s.GetInvoiceByReceiptIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingInvoice);

        _inventoryServiceMock
            .Setup(s => s.RestoreStockForVoidAsync(It.IsAny<Receipt>()))
            .ReturnsAsync(new List<StockMovement>());

        var voidServiceWithEtims = new ReceiptVoidService(
            _context,
            _sessionServiceMock.Object,
            _inventoryServiceMock.Object,
            _loggerMock.Object,
            etimsServiceMock.Object);

        var request = new VoidRequest
        {
            ReceiptId = receipt.Id,
            VoidReasonId = 1
        };

        // Act
        var result = await voidServiceWithEtims.VoidReceiptAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        etimsServiceMock.Verify(
            s => s.GenerateCreditNoteAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task VoidReceiptAsync_ShouldQueueCreditNote_WhenSubmissionFails()
    {
        // Arrange
        var receipt = await CreateTestReceiptAsync();
        var etimsServiceMock = new Mock<IEtimsService>();

        var existingInvoice = new EtimsInvoice
        {
            Id = 1,
            ReceiptId = receipt.Id,
            InvoiceNumber = "INV-001",
            Status = EtimsSubmissionStatus.Accepted
        };

        var failedCreditNote = new EtimsCreditNote
        {
            Id = 1,
            OriginalInvoiceId = 1,
            CreditNoteNumber = "CN-001",
            Status = EtimsSubmissionStatus.Failed,
            ErrorMessage = "Connection timeout"
        };

        etimsServiceMock
            .Setup(s => s.GetInvoiceByReceiptIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingInvoice);

        etimsServiceMock
            .Setup(s => s.GenerateCreditNoteAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedCreditNote);

        etimsServiceMock
            .Setup(s => s.SubmitCreditNoteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedCreditNote);

        etimsServiceMock
            .Setup(s => s.QueueForSubmissionAsync(
                It.IsAny<EtimsDocumentType>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EtimsQueueEntry());

        _inventoryServiceMock
            .Setup(s => s.RestoreStockForVoidAsync(It.IsAny<Receipt>()))
            .ReturnsAsync(new List<StockMovement>());

        var voidServiceWithEtims = new ReceiptVoidService(
            _context,
            _sessionServiceMock.Object,
            _inventoryServiceMock.Object,
            _loggerMock.Object,
            etimsServiceMock.Object);

        var request = new VoidRequest
        {
            ReceiptId = receipt.Id,
            VoidReasonId = 1
        };

        // Act
        var result = await voidServiceWithEtims.VoidReceiptAsync(request);

        // Assert
        result.Success.Should().BeTrue(); // Void still succeeds
        etimsServiceMock.Verify(
            s => s.QueueForSubmissionAsync(
                EtimsDocumentType.CreditNote,
                failedCreditNote.Id,
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task VoidReceiptAsync_ShouldSucceed_EvenWhenEtimsServiceThrows()
    {
        // Arrange
        var receipt = await CreateTestReceiptAsync();
        var etimsServiceMock = new Mock<IEtimsService>();

        etimsServiceMock
            .Setup(s => s.GetInvoiceByReceiptIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("eTIMS service unavailable"));

        _inventoryServiceMock
            .Setup(s => s.RestoreStockForVoidAsync(It.IsAny<Receipt>()))
            .ReturnsAsync(new List<StockMovement>());

        var voidServiceWithEtims = new ReceiptVoidService(
            _context,
            _sessionServiceMock.Object,
            _inventoryServiceMock.Object,
            _loggerMock.Object,
            etimsServiceMock.Object);

        var request = new VoidRequest
        {
            ReceiptId = receipt.Id,
            VoidReasonId = 1
        };

        // Act
        var result = await voidServiceWithEtims.VoidReceiptAsync(request);

        // Assert
        result.Success.Should().BeTrue(); // Void succeeds even if eTIMS fails
        result.Receipt!.Status.Should().Be(ReceiptStatus.Voided);
    }

    [Fact]
    public void Constructor_WithNullEtimsService_ShouldNotThrow()
    {
        // Act
        var action = () => new ReceiptVoidService(
            _context,
            _sessionServiceMock.Object,
            _inventoryServiceMock.Object,
            _loggerMock.Object,
            etimsService: null);

        // Assert
        action.Should().NotThrow(); // eTIMS is optional
    }

    #endregion
}
