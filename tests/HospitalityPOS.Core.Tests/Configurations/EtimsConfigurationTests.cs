using FluentAssertions;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using Xunit;

namespace HospitalityPOS.Core.Tests.Configurations;

/// <summary>
/// Unit tests for eTIMS entity configurations verifying proper entity setup,
/// default values, and business rules.
/// </summary>
public class EtimsConfigurationTests
{
    #region EtimsDevice Tests

    [Fact]
    public void EtimsDevice_NewInstance_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var device = new EtimsDevice();

        // Assert
        device.Id.Should().Be(0);
        device.DeviceSerialNumber.Should().BeEmpty();
        device.ControlUnitId.Should().BeEmpty();
        device.BusinessPin.Should().BeEmpty();
        device.BusinessName.Should().BeEmpty();
        device.BranchCode.Should().Be("001");
        device.BranchName.Should().Be("Main Branch");
        device.ApiBaseUrl.Should().Be("https://etims.kra.go.ke");
        device.ApiKey.Should().BeEmpty();
        device.ApiSecret.Should().BeEmpty();
        device.RegistrationDate.Should().BeNull();
        device.LastCommunication.Should().BeNull();
        device.Status.Should().Be(EtimsDeviceStatus.Pending);
        device.LastInvoiceNumber.Should().Be(0);
        device.LastCreditNoteNumber.Should().Be(0);
        device.IsPrimary.Should().BeFalse();
        device.Environment.Should().Be("Sandbox");
        device.IsActive.Should().BeTrue();
    }

    [Fact]
    public void EtimsDevice_ShouldInheritFromBaseEntity()
    {
        // Arrange & Act
        var device = new EtimsDevice();

        // Assert
        device.Should().BeAssignableTo<BaseEntity>();
    }

    [Fact]
    public void EtimsDevice_ShouldSetAllProperties()
    {
        // Arrange
        var registrationDate = DateTime.UtcNow;
        var lastCommunication = DateTime.UtcNow.AddMinutes(-5);

        // Act
        var device = new EtimsDevice
        {
            Id = 1,
            DeviceSerialNumber = "ETIMS-001-2024",
            ControlUnitId = "CU-KE-001",
            BusinessPin = "P051234567A",
            BusinessName = "Test Restaurant Ltd",
            BranchCode = "002",
            BranchName = "Downtown Branch",
            ApiBaseUrl = "https://api.etims.kra.go.ke",
            ApiKey = "test-api-key",
            ApiSecret = "test-api-secret",
            RegistrationDate = registrationDate,
            LastCommunication = lastCommunication,
            Status = EtimsDeviceStatus.Active,
            LastInvoiceNumber = 1000,
            LastCreditNoteNumber = 50,
            IsPrimary = true,
            Environment = "Production"
        };

        // Assert
        device.Id.Should().Be(1);
        device.DeviceSerialNumber.Should().Be("ETIMS-001-2024");
        device.ControlUnitId.Should().Be("CU-KE-001");
        device.BusinessPin.Should().Be("P051234567A");
        device.BusinessName.Should().Be("Test Restaurant Ltd");
        device.BranchCode.Should().Be("002");
        device.BranchName.Should().Be("Downtown Branch");
        device.ApiBaseUrl.Should().Be("https://api.etims.kra.go.ke");
        device.ApiKey.Should().Be("test-api-key");
        device.ApiSecret.Should().Be("test-api-secret");
        device.RegistrationDate.Should().Be(registrationDate);
        device.LastCommunication.Should().Be(lastCommunication);
        device.Status.Should().Be(EtimsDeviceStatus.Active);
        device.LastInvoiceNumber.Should().Be(1000);
        device.LastCreditNoteNumber.Should().Be(50);
        device.IsPrimary.Should().BeTrue();
        device.Environment.Should().Be("Production");
    }

    [Theory]
    [InlineData(EtimsDeviceStatus.Pending)]
    [InlineData(EtimsDeviceStatus.Registered)]
    [InlineData(EtimsDeviceStatus.Active)]
    [InlineData(EtimsDeviceStatus.Suspended)]
    [InlineData(EtimsDeviceStatus.Deactivated)]
    public void EtimsDevice_ShouldAcceptAllDeviceStatusValues(EtimsDeviceStatus status)
    {
        // Arrange & Act
        var device = new EtimsDevice { Status = status };

        // Assert
        device.Status.Should().Be(status);
    }

    #endregion

    #region EtimsInvoice Tests

    [Fact]
    public void EtimsInvoice_NewInstance_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var invoice = new EtimsInvoice();

        // Assert
        invoice.Id.Should().Be(0);
        invoice.ReceiptId.Should().Be(0);
        invoice.DeviceId.Should().Be(0);
        invoice.InvoiceNumber.Should().BeEmpty();
        invoice.InternalReceiptNumber.Should().BeEmpty();
        invoice.DocumentType.Should().Be(EtimsDocumentType.TaxInvoice);
        invoice.CustomerType.Should().Be(EtimsCustomerType.Consumer);
        invoice.CustomerPin.Should().BeNull();
        invoice.CustomerName.Should().Be("Walk-in Customer");
        invoice.CustomerPhone.Should().BeNull();
        invoice.TaxableAmount.Should().Be(0);
        invoice.TaxAmount.Should().Be(0);
        invoice.TotalAmount.Should().Be(0);
        invoice.StandardRatedAmount.Should().Be(0);
        invoice.ZeroRatedAmount.Should().Be(0);
        invoice.ExemptAmount.Should().Be(0);
        invoice.Status.Should().Be(EtimsSubmissionStatus.Pending);
        invoice.SubmissionAttempts.Should().Be(0);
        invoice.LastSubmissionAttempt.Should().BeNull();
        invoice.SubmittedAt.Should().BeNull();
        invoice.ReceiptSignature.Should().BeNull();
        invoice.KraInternalData.Should().BeNull();
        invoice.QrCode.Should().BeNull();
        invoice.ErrorMessage.Should().BeNull();
        invoice.RequestJson.Should().BeNull();
        invoice.ResponseJson.Should().BeNull();
        invoice.Items.Should().BeEmpty();
        invoice.IsActive.Should().BeTrue();
    }

    [Fact]
    public void EtimsInvoice_ShouldInheritFromBaseEntity()
    {
        // Arrange & Act
        var invoice = new EtimsInvoice();

        // Assert
        invoice.Should().BeAssignableTo<BaseEntity>();
    }

    [Fact]
    public void EtimsInvoice_ShouldSetAllProperties()
    {
        // Arrange
        var invoiceDate = DateTime.UtcNow;
        var submissionAttempt = DateTime.UtcNow.AddSeconds(-30);
        var submittedAt = DateTime.UtcNow.AddSeconds(-25);

        // Act
        var invoice = new EtimsInvoice
        {
            Id = 1,
            ReceiptId = 100,
            DeviceId = 1,
            InvoiceNumber = "CU001-001-2024-000001",
            InternalReceiptNumber = "RCP-2024-000100",
            InvoiceDate = invoiceDate,
            DocumentType = EtimsDocumentType.TaxInvoice,
            CustomerType = EtimsCustomerType.Business,
            CustomerPin = "P051234567B",
            CustomerName = "ABC Company Ltd",
            CustomerPhone = "+254712345678",
            TaxableAmount = 8620.69m,
            TaxAmount = 1379.31m,
            TotalAmount = 10000.00m,
            StandardRatedAmount = 8620.69m,
            ZeroRatedAmount = 0m,
            ExemptAmount = 0m,
            Status = EtimsSubmissionStatus.Accepted,
            SubmissionAttempts = 1,
            LastSubmissionAttempt = submissionAttempt,
            SubmittedAt = submittedAt,
            ReceiptSignature = "SGVsbG8gV29ybGQ=",
            KraInternalData = "{\"rcptNo\":\"123456\"}",
            QrCode = "https://etims.kra.go.ke/verify?inv=CU001-001-2024-000001"
        };

        // Assert
        invoice.Id.Should().Be(1);
        invoice.ReceiptId.Should().Be(100);
        invoice.DeviceId.Should().Be(1);
        invoice.InvoiceNumber.Should().Be("CU001-001-2024-000001");
        invoice.TaxableAmount.Should().Be(8620.69m);
        invoice.TaxAmount.Should().Be(1379.31m);
        invoice.TotalAmount.Should().Be(10000.00m);
        invoice.Status.Should().Be(EtimsSubmissionStatus.Accepted);
    }

    [Theory]
    [InlineData(EtimsDocumentType.TaxInvoice)]
    [InlineData(EtimsDocumentType.CreditNote)]
    [InlineData(EtimsDocumentType.DebitNote)]
    [InlineData(EtimsDocumentType.SimplifiedTaxInvoice)]
    public void EtimsInvoice_ShouldAcceptAllDocumentTypes(EtimsDocumentType documentType)
    {
        // Arrange & Act
        var invoice = new EtimsInvoice { DocumentType = documentType };

        // Assert
        invoice.DocumentType.Should().Be(documentType);
    }

    [Theory]
    [InlineData(EtimsCustomerType.Business)]
    [InlineData(EtimsCustomerType.Consumer)]
    [InlineData(EtimsCustomerType.Government)]
    [InlineData(EtimsCustomerType.Export)]
    public void EtimsInvoice_ShouldAcceptAllCustomerTypes(EtimsCustomerType customerType)
    {
        // Arrange & Act
        var invoice = new EtimsInvoice { CustomerType = customerType };

        // Assert
        invoice.CustomerType.Should().Be(customerType);
    }

    [Theory]
    [InlineData(EtimsSubmissionStatus.Pending)]
    [InlineData(EtimsSubmissionStatus.Queued)]
    [InlineData(EtimsSubmissionStatus.Submitted)]
    [InlineData(EtimsSubmissionStatus.Accepted)]
    [InlineData(EtimsSubmissionStatus.Rejected)]
    [InlineData(EtimsSubmissionStatus.Failed)]
    public void EtimsInvoice_ShouldAcceptAllSubmissionStatuses(EtimsSubmissionStatus status)
    {
        // Arrange & Act
        var invoice = new EtimsInvoice { Status = status };

        // Assert
        invoice.Status.Should().Be(status);
    }

    #endregion

    #region EtimsInvoiceItem Tests

    [Fact]
    public void EtimsInvoiceItem_NewInstance_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var item = new EtimsInvoiceItem();

        // Assert
        item.Id.Should().Be(0);
        item.EtimsInvoiceId.Should().Be(0);
        item.SequenceNumber.Should().Be(0);
        item.ItemCode.Should().BeEmpty();
        item.ItemDescription.Should().BeEmpty();
        item.HsCode.Should().BeNull();
        item.UnitOfMeasure.Should().Be("PCS");
        item.Quantity.Should().Be(0);
        item.UnitPrice.Should().Be(0);
        item.DiscountAmount.Should().Be(0);
        item.TaxType.Should().Be(KraTaxType.A);
        item.TaxRate.Should().Be(16m);
        item.TaxableAmount.Should().Be(0);
        item.TaxAmount.Should().Be(0);
        item.TotalAmount.Should().Be(0);
        item.IsActive.Should().BeTrue();
    }

    [Fact]
    public void EtimsInvoiceItem_ShouldInheritFromBaseEntity()
    {
        // Arrange & Act
        var item = new EtimsInvoiceItem();

        // Assert
        item.Should().BeAssignableTo<BaseEntity>();
    }

    [Fact]
    public void EtimsInvoiceItem_ShouldSetAllProperties()
    {
        // Arrange & Act
        var item = new EtimsInvoiceItem
        {
            Id = 1,
            EtimsInvoiceId = 100,
            SequenceNumber = 1,
            ItemCode = "PROD-001",
            ItemDescription = "Premium Coffee",
            HsCode = "0901.21.00",
            UnitOfMeasure = "KG",
            Quantity = 2.5m,
            UnitPrice = 500.00m,
            DiscountAmount = 50.00m,
            TaxType = KraTaxType.A,
            TaxRate = 16m,
            TaxableAmount = 1034.48m,
            TaxAmount = 165.52m,
            TotalAmount = 1200.00m
        };

        // Assert
        item.Id.Should().Be(1);
        item.EtimsInvoiceId.Should().Be(100);
        item.SequenceNumber.Should().Be(1);
        item.ItemCode.Should().Be("PROD-001");
        item.ItemDescription.Should().Be("Premium Coffee");
        item.HsCode.Should().Be("0901.21.00");
        item.UnitOfMeasure.Should().Be("KG");
        item.Quantity.Should().Be(2.5m);
        item.UnitPrice.Should().Be(500.00m);
        item.DiscountAmount.Should().Be(50.00m);
        item.TaxType.Should().Be(KraTaxType.A);
        item.TaxRate.Should().Be(16m);
        item.TaxableAmount.Should().Be(1034.48m);
        item.TaxAmount.Should().Be(165.52m);
        item.TotalAmount.Should().Be(1200.00m);
    }

    [Theory]
    [InlineData(KraTaxType.A, 16)]    // Standard rated
    [InlineData(KraTaxType.B, 0)]     // Zero rated
    [InlineData(KraTaxType.C, 0)]     // Exempt
    [InlineData(KraTaxType.D, 0)]     // Out of scope
    [InlineData(KraTaxType.E, 0)]     // Insurance premium levy
    public void EtimsInvoiceItem_ShouldAcceptAllTaxTypes(KraTaxType taxType, decimal expectedDefaultRate)
    {
        // Arrange & Act
        var item = new EtimsInvoiceItem
        {
            TaxType = taxType,
            TaxRate = expectedDefaultRate
        };

        // Assert
        item.TaxType.Should().Be(taxType);
        item.TaxRate.Should().Be(expectedDefaultRate);
    }

    #endregion

    #region EtimsCreditNote Tests

    [Fact]
    public void EtimsCreditNote_NewInstance_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var creditNote = new EtimsCreditNote();

        // Assert
        creditNote.Id.Should().Be(0);
        creditNote.ReceiptVoidId.Should().BeNull();
        creditNote.OriginalInvoiceId.Should().Be(0);
        creditNote.DeviceId.Should().Be(0);
        creditNote.CreditNoteNumber.Should().BeEmpty();
        creditNote.OriginalInvoiceNumber.Should().BeEmpty();
        creditNote.Reason.Should().BeEmpty();
        creditNote.CustomerPin.Should().BeNull();
        creditNote.CustomerName.Should().BeEmpty();
        creditNote.CreditAmount.Should().Be(0);
        creditNote.TaxAmount.Should().Be(0);
        creditNote.Status.Should().Be(EtimsSubmissionStatus.Pending);
        creditNote.SubmissionAttempts.Should().Be(0);
        creditNote.LastSubmissionAttempt.Should().BeNull();
        creditNote.SubmittedAt.Should().BeNull();
        creditNote.KraSignature.Should().BeNull();
        creditNote.ErrorMessage.Should().BeNull();
        creditNote.RequestJson.Should().BeNull();
        creditNote.ResponseJson.Should().BeNull();
        creditNote.Items.Should().BeEmpty();
        creditNote.IsActive.Should().BeTrue();
    }

    [Fact]
    public void EtimsCreditNote_ShouldInheritFromBaseEntity()
    {
        // Arrange & Act
        var creditNote = new EtimsCreditNote();

        // Assert
        creditNote.Should().BeAssignableTo<BaseEntity>();
    }

    [Fact]
    public void EtimsCreditNote_ShouldSetAllProperties()
    {
        // Arrange
        var creditNoteDate = DateTime.UtcNow;
        var submittedAt = DateTime.UtcNow.AddSeconds(-10);

        // Act
        var creditNote = new EtimsCreditNote
        {
            Id = 1,
            ReceiptVoidId = 50,
            OriginalInvoiceId = 100,
            DeviceId = 1,
            CreditNoteNumber = "CN-CU001-001-2024-000001",
            OriginalInvoiceNumber = "CU001-001-2024-000100",
            CreditNoteDate = creditNoteDate,
            Reason = "Customer return - wrong item ordered",
            CustomerPin = "P051234567B",
            CustomerName = "ABC Company Ltd",
            CreditAmount = 5000.00m,
            TaxAmount = 689.66m,
            Status = EtimsSubmissionStatus.Accepted,
            SubmissionAttempts = 1,
            SubmittedAt = submittedAt,
            KraSignature = "SGVsbG8gV29ybGQ="
        };

        // Assert
        creditNote.Id.Should().Be(1);
        creditNote.ReceiptVoidId.Should().Be(50);
        creditNote.OriginalInvoiceId.Should().Be(100);
        creditNote.DeviceId.Should().Be(1);
        creditNote.CreditNoteNumber.Should().Be("CN-CU001-001-2024-000001");
        creditNote.OriginalInvoiceNumber.Should().Be("CU001-001-2024-000100");
        creditNote.CreditNoteDate.Should().Be(creditNoteDate);
        creditNote.Reason.Should().Be("Customer return - wrong item ordered");
        creditNote.CreditAmount.Should().Be(5000.00m);
        creditNote.TaxAmount.Should().Be(689.66m);
        creditNote.Status.Should().Be(EtimsSubmissionStatus.Accepted);
    }

    #endregion

    #region EtimsCreditNoteItem Tests

    [Fact]
    public void EtimsCreditNoteItem_NewInstance_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var item = new EtimsCreditNoteItem();

        // Assert
        item.Id.Should().Be(0);
        item.EtimsCreditNoteId.Should().Be(0);
        item.SequenceNumber.Should().Be(0);
        item.ItemCode.Should().BeEmpty();
        item.ItemDescription.Should().BeEmpty();
        item.Quantity.Should().Be(0);
        item.UnitPrice.Should().Be(0);
        item.TaxRate.Should().Be(0);
        item.TaxableAmount.Should().Be(0);
        item.TaxAmount.Should().Be(0);
        item.TotalAmount.Should().Be(0);
        item.IsActive.Should().BeTrue();
    }

    [Fact]
    public void EtimsCreditNoteItem_ShouldInheritFromBaseEntity()
    {
        // Arrange & Act
        var item = new EtimsCreditNoteItem();

        // Assert
        item.Should().BeAssignableTo<BaseEntity>();
    }

    #endregion

    #region EtimsQueueEntry Tests

    [Fact]
    public void EtimsQueueEntry_NewInstance_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var entry = new EtimsQueueEntry();

        // Assert
        entry.Id.Should().Be(0);
        entry.DocumentId.Should().Be(0);
        entry.Priority.Should().Be(100);
        entry.QueuedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        entry.RetryAfter.Should().BeNull();
        entry.Attempts.Should().Be(0);
        entry.MaxAttempts.Should().Be(10);
        entry.Status.Should().Be(EtimsSubmissionStatus.Queued);
        entry.LastError.Should().BeNull();
        entry.LastProcessedAt.Should().BeNull();
        entry.CompletedAt.Should().BeNull();
        entry.IsActive.Should().BeTrue();
    }

    [Fact]
    public void EtimsQueueEntry_ShouldInheritFromBaseEntity()
    {
        // Arrange & Act
        var entry = new EtimsQueueEntry();

        // Assert
        entry.Should().BeAssignableTo<BaseEntity>();
    }

    [Fact]
    public void EtimsQueueEntry_ShouldSetAllProperties()
    {
        // Arrange
        var queuedAt = DateTime.UtcNow;
        var retryAfter = DateTime.UtcNow.AddMinutes(5);
        var lastProcessedAt = DateTime.UtcNow.AddSeconds(-30);
        var completedAt = DateTime.UtcNow;

        // Act
        var entry = new EtimsQueueEntry
        {
            Id = 1,
            DocumentType = EtimsDocumentType.TaxInvoice,
            DocumentId = 100,
            Priority = 1,
            QueuedAt = queuedAt,
            RetryAfter = retryAfter,
            Attempts = 3,
            MaxAttempts = 5,
            Status = EtimsSubmissionStatus.Accepted,
            LastError = "Connection timeout",
            LastProcessedAt = lastProcessedAt,
            CompletedAt = completedAt
        };

        // Assert
        entry.Id.Should().Be(1);
        entry.DocumentType.Should().Be(EtimsDocumentType.TaxInvoice);
        entry.DocumentId.Should().Be(100);
        entry.Priority.Should().Be(1);
        entry.QueuedAt.Should().Be(queuedAt);
        entry.RetryAfter.Should().Be(retryAfter);
        entry.Attempts.Should().Be(3);
        entry.MaxAttempts.Should().Be(5);
        entry.Status.Should().Be(EtimsSubmissionStatus.Accepted);
        entry.LastError.Should().Be("Connection timeout");
        entry.LastProcessedAt.Should().Be(lastProcessedAt);
        entry.CompletedAt.Should().Be(completedAt);
    }

    #endregion

    #region EtimsSyncLog Tests

    [Fact]
    public void EtimsSyncLog_NewInstance_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var log = new EtimsSyncLog();

        // Assert
        log.Id.Should().Be(0);
        log.OperationType.Should().BeEmpty();
        log.DocumentType.Should().BeNull();
        log.DocumentId.Should().BeNull();
        log.CompletedAt.Should().BeNull();
        log.IsSuccess.Should().BeFalse();
        log.ErrorMessage.Should().BeNull();
        log.RequestJson.Should().BeNull();
        log.ResponseJson.Should().BeNull();
        log.HttpStatusCode.Should().BeNull();
        log.DurationMs.Should().BeNull();
        log.IsActive.Should().BeTrue();
    }

    [Fact]
    public void EtimsSyncLog_ShouldInheritFromBaseEntity()
    {
        // Arrange & Act
        var log = new EtimsSyncLog();

        // Assert
        log.Should().BeAssignableTo<BaseEntity>();
    }

    [Fact]
    public void EtimsSyncLog_ShouldSetAllProperties()
    {
        // Arrange
        var startedAt = DateTime.UtcNow.AddSeconds(-2);
        var completedAt = DateTime.UtcNow;

        // Act
        var log = new EtimsSyncLog
        {
            Id = 1,
            OperationType = "SubmitInvoice",
            DocumentType = EtimsDocumentType.TaxInvoice,
            DocumentId = 100,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            IsSuccess = true,
            ErrorMessage = null,
            RequestJson = "{\"invoiceNo\":\"INV-001\"}",
            ResponseJson = "{\"status\":\"success\"}",
            HttpStatusCode = 200,
            DurationMs = 1500
        };

        // Assert
        log.Id.Should().Be(1);
        log.OperationType.Should().Be("SubmitInvoice");
        log.DocumentType.Should().Be(EtimsDocumentType.TaxInvoice);
        log.DocumentId.Should().Be(100);
        log.StartedAt.Should().Be(startedAt);
        log.CompletedAt.Should().Be(completedAt);
        log.IsSuccess.Should().BeTrue();
        log.HttpStatusCode.Should().Be(200);
        log.DurationMs.Should().Be(1500);
    }

    #endregion

    #region Navigation Property Tests

    [Fact]
    public void EtimsInvoice_ShouldHaveNavigationProperties()
    {
        // Arrange
        var invoice = new EtimsInvoice();
        var device = new EtimsDevice { Id = 1, DeviceSerialNumber = "TEST-001" };
        var items = new List<EtimsInvoiceItem>
        {
            new() { Id = 1, ItemCode = "ITEM-001" },
            new() { Id = 2, ItemCode = "ITEM-002" }
        };

        // Act
        invoice.Device = device;
        invoice.Items = items;

        // Assert
        invoice.Device.Should().NotBeNull();
        invoice.Device.DeviceSerialNumber.Should().Be("TEST-001");
        invoice.Items.Should().HaveCount(2);
    }

    [Fact]
    public void EtimsCreditNote_ShouldHaveNavigationProperties()
    {
        // Arrange
        var creditNote = new EtimsCreditNote();
        var originalInvoice = new EtimsInvoice { Id = 1, InvoiceNumber = "INV-001" };
        var device = new EtimsDevice { Id = 1, DeviceSerialNumber = "TEST-001" };
        var items = new List<EtimsCreditNoteItem>
        {
            new() { Id = 1, ItemCode = "ITEM-001" }
        };

        // Act
        creditNote.OriginalInvoice = originalInvoice;
        creditNote.Device = device;
        creditNote.Items = items;

        // Assert
        creditNote.OriginalInvoice.Should().NotBeNull();
        creditNote.OriginalInvoice.InvoiceNumber.Should().Be("INV-001");
        creditNote.Device.Should().NotBeNull();
        creditNote.Items.Should().HaveCount(1);
    }

    #endregion
}
