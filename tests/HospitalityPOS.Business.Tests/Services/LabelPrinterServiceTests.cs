using FluentAssertions;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

public class LabelPrinterServiceTests
{
    private readonly Mock<IRepository<LabelPrinter>> _printerRepoMock;
    private readonly Mock<IRepository<LabelSize>> _sizeRepoMock;
    private readonly Mock<IRepository<CategoryPrinterAssignment>> _assignmentRepoMock;
    private readonly Mock<IRepository<LabelPrintJob>> _jobRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<LabelPrinterService>> _loggerMock;
    private readonly LabelPrinterService _service;

    public LabelPrinterServiceTests()
    {
        _printerRepoMock = new Mock<IRepository<LabelPrinter>>();
        _sizeRepoMock = new Mock<IRepository<LabelSize>>();
        _assignmentRepoMock = new Mock<IRepository<CategoryPrinterAssignment>>();
        _jobRepoMock = new Mock<IRepository<LabelPrintJob>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<LabelPrinterService>>();

        _service = new LabelPrinterService(
            _printerRepoMock.Object,
            _sizeRepoMock.Object,
            _assignmentRepoMock.Object,
            _jobRepoMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    #region Printer CRUD Tests

    [Fact]
    public async Task CreatePrinterAsync_ValidDto_CreatesPrinter()
    {
        // Arrange
        var dto = new CreateLabelPrinterDto
        {
            Name = "Test Printer",
            ConnectionString = "192.168.1.100:9100",
            StoreId = 1,
            PrinterType = LabelPrinterTypeDto.Network,
            PrintLanguage = LabelPrintLanguageDto.ZPL,
            IsDefault = true
        };

        _printerRepoMock.Setup(r => r.AddAsync(It.IsAny<LabelPrinter>()))
            .Callback<LabelPrinter>(p => p.Id = 1)
            .Returns(Task.CompletedTask);
        _printerRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new LabelPrinter { Id = 1, Name = dto.Name, StoreId = 1, IsActive = true });
        _printerRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelPrinter, bool>>>()))
            .ReturnsAsync(new List<LabelPrinter>());
        _assignmentRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<CategoryPrinterAssignment, bool>>>()))
            .ReturnsAsync(new List<CategoryPrinterAssignment>());

        // Act
        var result = await _service.CreatePrinterAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(dto.Name);
        _printerRepoMock.Verify(r => r.AddAsync(It.IsAny<LabelPrinter>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetPrinterAsync_ExistingId_ReturnsPrinter()
    {
        // Arrange
        var printer = new LabelPrinter
        {
            Id = 1,
            Name = "Test Printer",
            ConnectionString = "192.168.1.100",
            StoreId = 1,
            PrinterType = LabelPrinterType.Network,
            PrintLanguage = LabelPrintLanguage.ZPL,
            IsActive = true
        };

        _printerRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(printer);
        _assignmentRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<CategoryPrinterAssignment, bool>>>()))
            .ReturnsAsync(new List<CategoryPrinterAssignment>());

        // Act
        var result = await _service.GetPrinterAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Printer");
    }

    [Fact]
    public async Task GetPrinterAsync_InactivePrinter_ReturnsNull()
    {
        // Arrange
        var printer = new LabelPrinter { Id = 1, Name = "Test", IsActive = false };
        _printerRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(printer);

        // Act
        var result = await _service.GetPrinterAsync(1);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllPrintersAsync_StoreId_ReturnsStorePrinters()
    {
        // Arrange
        var printers = new List<LabelPrinter>
        {
            new() { Id = 1, Name = "Printer A", StoreId = 1, IsActive = true },
            new() { Id = 2, Name = "Printer B", StoreId = 1, IsActive = true }
        };

        _printerRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelPrinter, bool>>>()))
            .ReturnsAsync(printers);
        _assignmentRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<CategoryPrinterAssignment, bool>>>()))
            .ReturnsAsync(new List<CategoryPrinterAssignment>());

        // Act
        var result = await _service.GetAllPrintersAsync(1);

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeInAscendingOrder(p => p.Name);
    }

    [Fact]
    public async Task UpdatePrinterAsync_ValidDto_UpdatesPrinter()
    {
        // Arrange
        var printer = new LabelPrinter
        {
            Id = 1,
            Name = "Old Name",
            StoreId = 1,
            IsActive = true
        };

        var dto = new UpdateLabelPrinterDto { Name = "New Name" };

        _printerRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(printer);
        _printerRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelPrinter, bool>>>()))
            .ReturnsAsync(new List<LabelPrinter>());
        _assignmentRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<CategoryPrinterAssignment, bool>>>()))
            .ReturnsAsync(new List<CategoryPrinterAssignment>());

        // Act
        var result = await _service.UpdatePrinterAsync(1, dto);

        // Assert
        result.Name.Should().Be("New Name");
        _printerRepoMock.Verify(r => r.UpdateAsync(It.Is<LabelPrinter>(p => p.Name == "New Name")), Times.Once);
    }

    [Fact]
    public async Task DeletePrinterAsync_NoActiveJobs_DeletesPrinter()
    {
        // Arrange
        var printer = new LabelPrinter { Id = 1, Name = "Test", IsActive = true };

        _printerRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(printer);
        _jobRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelPrintJob, bool>>>()))
            .ReturnsAsync(new List<LabelPrintJob>());
        _assignmentRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<CategoryPrinterAssignment, bool>>>()))
            .ReturnsAsync(new List<CategoryPrinterAssignment>());

        // Act
        var result = await _service.DeletePrinterAsync(1);

        // Assert
        result.Should().BeTrue();
        printer.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeletePrinterAsync_HasActiveJobs_ThrowsException()
    {
        // Arrange
        var printer = new LabelPrinter { Id = 1, Name = "Test", IsActive = true };
        var activeJobs = new List<LabelPrintJob>
        {
            new() { Id = 1, PrinterId = 1, Status = LabelPrintJobStatus.InProgress }
        };

        _printerRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(printer);
        _jobRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelPrintJob, bool>>>()))
            .ReturnsAsync(activeJobs);

        // Act & Assert
        await _service.Invoking(s => s.DeletePrinterAsync(1))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*active print jobs*");
    }

    #endregion

    #region Printer Operations Tests

    [Fact]
    public async Task TestPrinterConnectionAsync_PrinterNotFound_ReturnsFailure()
    {
        // Arrange
        _printerRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((LabelPrinter?)null);

        // Act
        var result = await _service.TestPrinterConnectionAsync(1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task PrintTestLabelAsync_Success_ReturnsLabelContent()
    {
        // Arrange
        var printer = new LabelPrinter
        {
            Id = 1,
            Name = "Test",
            PrinterType = LabelPrinterType.Network,
            PrintLanguage = LabelPrintLanguage.ZPL,
            ConnectionString = "192.168.1.100:9100",
            IsActive = true
        };

        _printerRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(printer);

        // Act
        var result = await _service.PrintTestLabelAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.LabelContent.Should().NotBeNullOrEmpty();
        result.LabelContent.Should().Contain("TEST LABEL");
    }

    [Fact]
    public async Task SetDefaultPrinterAsync_ValidPrinter_SetsAsDefault()
    {
        // Arrange
        var printer = new LabelPrinter { Id = 1, StoreId = 1, IsActive = true, IsDefault = false };

        _printerRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(printer);
        _printerRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelPrinter, bool>>>()))
            .ReturnsAsync(new List<LabelPrinter>());

        // Act
        await _service.SetDefaultPrinterAsync(1, 1);

        // Assert
        printer.IsDefault.Should().BeTrue();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task GetDefaultPrinterAsync_HasDefault_ReturnsDefaultPrinter()
    {
        // Arrange
        var printer = new LabelPrinter
        {
            Id = 1,
            Name = "Default Printer",
            StoreId = 1,
            IsDefault = true,
            IsActive = true
        };

        _printerRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelPrinter, bool>>>()))
            .ReturnsAsync(new List<LabelPrinter> { printer });

        // Act
        var result = await _service.GetDefaultPrinterAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task GetPrinterForCategoryAsync_HasAssignment_ReturnsAssignedPrinter()
    {
        // Arrange
        var assignment = new CategoryPrinterAssignment
        {
            Id = 1,
            CategoryId = 10,
            LabelPrinterId = 5,
            StoreId = 1,
            IsActive = true
        };
        var printer = new LabelPrinter { Id = 5, Name = "Category Printer", IsActive = true };

        _assignmentRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<CategoryPrinterAssignment, bool>>>()))
            .ReturnsAsync(new List<CategoryPrinterAssignment> { assignment });
        _printerRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(printer);

        // Act
        var result = await _service.GetPrinterForCategoryAsync(10, 1);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Category Printer");
    }

    [Fact]
    public async Task GetPrinterForCategoryAsync_NoAssignment_ReturnsDefaultPrinter()
    {
        // Arrange
        var defaultPrinter = new LabelPrinter
        {
            Id = 1,
            Name = "Default Printer",
            StoreId = 1,
            IsDefault = true,
            IsActive = true
        };

        _assignmentRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<CategoryPrinterAssignment, bool>>>()))
            .ReturnsAsync(new List<CategoryPrinterAssignment>());
        _printerRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelPrinter, bool>>>()))
            .ReturnsAsync(new List<LabelPrinter> { defaultPrinter });

        // Act
        var result = await _service.GetPrinterForCategoryAsync(10, 1);

        // Assert
        result.Should().NotBeNull();
        result!.IsDefault.Should().BeTrue();
    }

    #endregion

    #region Label Size Tests

    [Fact]
    public async Task CreateLabelSizeAsync_ValidDto_CreatesSize()
    {
        // Arrange
        var dto = new CreateLabelSizeDto
        {
            Name = "Standard 2x1",
            WidthMm = 50,
            HeightMm = 25,
            DotsPerMm = 8
        };

        _sizeRepoMock.Setup(r => r.AddAsync(It.IsAny<LabelSize>()))
            .Callback<LabelSize>(s => s.Id = 1)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateLabelSizeAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Standard 2x1");
        result.WidthMm.Should().Be(50);
        result.HeightMm.Should().Be(25);
    }

    [Fact]
    public async Task GetAllLabelSizesAsync_ReturnsSortedSizes()
    {
        // Arrange
        var sizes = new List<LabelSize>
        {
            new() { Id = 1, Name = "Large", WidthMm = 100, HeightMm = 50, IsActive = true },
            new() { Id = 2, Name = "Medium", WidthMm = 75, HeightMm = 35, IsActive = true },
            new() { Id = 3, Name = "Small", WidthMm = 50, HeightMm = 25, IsActive = true }
        };

        _sizeRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelSize, bool>>>()))
            .ReturnsAsync(sizes);

        // Act
        var result = await _service.GetAllLabelSizesAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().BeInAscendingOrder(s => s.Name);
    }

    [Fact]
    public async Task UpdateLabelSizeAsync_ValidDto_UpdatesSize()
    {
        // Arrange
        var size = new LabelSize { Id = 1, Name = "Old Name", WidthMm = 50, HeightMm = 25, IsActive = true };
        var dto = new UpdateLabelSizeDto { Name = "New Name", WidthMm = 60 };

        _sizeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(size);

        // Act
        var result = await _service.UpdateLabelSizeAsync(1, dto);

        // Assert
        result.Name.Should().Be("New Name");
        result.WidthMm.Should().Be(60);
    }

    [Fact]
    public async Task DeleteLabelSizeAsync_ValidId_DeletesSize()
    {
        // Arrange
        var size = new LabelSize { Id = 1, Name = "Test", IsActive = true };
        _sizeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(size);

        // Act
        var result = await _service.DeleteLabelSizeAsync(1);

        // Assert
        result.Should().BeTrue();
        size.IsActive.Should().BeFalse();
    }

    #endregion

    #region Category Assignment Tests

    [Fact]
    public async Task AssignCategoryPrinterAsync_NewAssignment_CreatesAssignment()
    {
        // Arrange
        var dto = new AssignCategoryPrinterDto
        {
            CategoryId = 10,
            LabelPrinterId = 5,
            LabelTemplateId = 3
        };

        _assignmentRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<CategoryPrinterAssignment, bool>>>()))
            .ReturnsAsync(new List<CategoryPrinterAssignment>());
        _assignmentRepoMock.Setup(r => r.AddAsync(It.IsAny<CategoryPrinterAssignment>()))
            .Callback<CategoryPrinterAssignment>(a => a.Id = 1)
            .Returns(Task.CompletedTask);
        _printerRepoMock.Setup(r => r.GetByIdAsync(5))
            .ReturnsAsync(new LabelPrinter { Id = 5, Name = "Printer 5" });

        // Act
        var result = await _service.AssignCategoryPrinterAsync(dto, 1);

        // Assert
        result.Should().NotBeNull();
        result.CategoryId.Should().Be(10);
        result.LabelPrinterId.Should().Be(5);
        _assignmentRepoMock.Verify(r => r.AddAsync(It.IsAny<CategoryPrinterAssignment>()), Times.Once);
    }

    [Fact]
    public async Task AssignCategoryPrinterAsync_ReplacesExisting_DeactivatesOld()
    {
        // Arrange
        var existingAssignment = new CategoryPrinterAssignment
        {
            Id = 1,
            CategoryId = 10,
            LabelPrinterId = 3,
            StoreId = 1,
            IsActive = true
        };

        var dto = new AssignCategoryPrinterDto { CategoryId = 10, LabelPrinterId = 5 };

        _assignmentRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<CategoryPrinterAssignment, bool>>>()))
            .ReturnsAsync(new List<CategoryPrinterAssignment> { existingAssignment });
        _assignmentRepoMock.Setup(r => r.AddAsync(It.IsAny<CategoryPrinterAssignment>()))
            .Callback<CategoryPrinterAssignment>(a => a.Id = 2)
            .Returns(Task.CompletedTask);
        _printerRepoMock.Setup(r => r.GetByIdAsync(5))
            .ReturnsAsync(new LabelPrinter { Id = 5, Name = "Printer 5" });

        // Act
        var result = await _service.AssignCategoryPrinterAsync(dto, 1);

        // Assert
        existingAssignment.IsActive.Should().BeFalse();
        result.LabelPrinterId.Should().Be(5);
    }

    [Fact]
    public async Task GetCategoryAssignmentsAsync_ReturnsStoreAssignments()
    {
        // Arrange
        var assignments = new List<CategoryPrinterAssignment>
        {
            new() { Id = 1, CategoryId = 10, LabelPrinterId = 5, StoreId = 1, IsActive = true },
            new() { Id = 2, CategoryId = 20, LabelPrinterId = 5, StoreId = 1, IsActive = true }
        };

        _assignmentRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<CategoryPrinterAssignment, bool>>>()))
            .ReturnsAsync(assignments);
        _printerRepoMock.Setup(r => r.GetByIdAsync(5))
            .ReturnsAsync(new LabelPrinter { Id = 5, Name = "Printer 5" });

        // Act
        var result = await _service.GetCategoryAssignmentsAsync(1);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task RemoveCategoryAssignmentAsync_ValidId_RemovesAssignment()
    {
        // Arrange
        var assignment = new CategoryPrinterAssignment { Id = 1, IsActive = true };
        _assignmentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(assignment);

        // Act
        var result = await _service.RemoveCategoryAssignmentAsync(1);

        // Assert
        result.Should().BeTrue();
        assignment.IsActive.Should().BeFalse();
    }

    #endregion

    #region Statistics Tests

    [Fact]
    public async Task GetPrinterUsageAsync_ReturnsUsageByPrinter()
    {
        // Arrange
        var jobs = new List<LabelPrintJob>
        {
            new() { Id = 1, PrinterId = 1, PrintedLabels = 50, StoreId = 1, StartedAt = DateTime.UtcNow },
            new() { Id = 2, PrinterId = 1, PrintedLabels = 30, StoreId = 1, StartedAt = DateTime.UtcNow },
            new() { Id = 3, PrinterId = 2, PrintedLabels = 20, StoreId = 1, StartedAt = DateTime.UtcNow }
        };

        _jobRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelPrintJob, bool>>>()))
            .ReturnsAsync(jobs);

        // Act
        var result = await _service.GetPrinterUsageAsync(1, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);

        // Assert
        result.Should().HaveCount(2);
        result[1].Should().Be(80);
        result[2].Should().Be(20);
    }

    #endregion

    #region Events Tests

    [Fact]
    public async Task TestPrinterConnectionAsync_Success_RaisesPrinterConnectedEvent()
    {
        // Arrange
        var printer = new LabelPrinter
        {
            Id = 1,
            Name = "Test",
            PrinterType = LabelPrinterType.Windows,
            ConnectionString = "Printer Name",
            IsActive = true
        };

        _printerRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(printer);

        LabelPrinterDto? eventPrinter = null;
        _service.PrinterConnected += (s, p) => eventPrinter = p;

        // Act
        var result = await _service.TestPrinterConnectionAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        eventPrinter.Should().NotBeNull();
    }

    #endregion
}
