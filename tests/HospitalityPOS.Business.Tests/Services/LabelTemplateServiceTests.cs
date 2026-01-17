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

public class LabelTemplateServiceTests
{
    private readonly Mock<IRepository<LabelTemplate>> _templateRepoMock;
    private readonly Mock<IRepository<LabelTemplateField>> _fieldRepoMock;
    private readonly Mock<IRepository<LabelSize>> _sizeRepoMock;
    private readonly Mock<IRepository<LabelTemplateLibrary>> _libraryRepoMock;
    private readonly Mock<IRepository<CategoryPrinterAssignment>> _assignmentRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<LabelTemplateService>> _loggerMock;
    private readonly LabelTemplateService _service;

    public LabelTemplateServiceTests()
    {
        _templateRepoMock = new Mock<IRepository<LabelTemplate>>();
        _fieldRepoMock = new Mock<IRepository<LabelTemplateField>>();
        _sizeRepoMock = new Mock<IRepository<LabelSize>>();
        _libraryRepoMock = new Mock<IRepository<LabelTemplateLibrary>>();
        _assignmentRepoMock = new Mock<IRepository<CategoryPrinterAssignment>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<LabelTemplateService>>();

        _service = new LabelTemplateService(
            _templateRepoMock.Object,
            _fieldRepoMock.Object,
            _sizeRepoMock.Object,
            _libraryRepoMock.Object,
            _assignmentRepoMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    #region Template CRUD Tests

    [Fact]
    public async Task CreateTemplateAsync_ValidDto_CreatesTemplate()
    {
        // Arrange
        var dto = new CreateLabelTemplateDto
        {
            Name = "Standard Product Label",
            LabelSizeId = 1,
            StoreId = 1,
            PrintLanguage = LabelPrintLanguageDto.ZPL,
            TemplateContent = "^XA^FO50,50^A0N,50,50^FD{{ProductName}}^FS^XZ",
            IsDefault = true
        };

        _templateRepoMock.Setup(r => r.AddAsync(It.IsAny<LabelTemplate>()))
            .Callback<LabelTemplate>(t => t.Id = 1)
            .Returns(Task.CompletedTask);
        _templateRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new LabelTemplate { Id = 1, Name = dto.Name, StoreId = 1, LabelSizeId = 1, IsActive = true });
        _templateRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelTemplate, bool>>>()))
            .ReturnsAsync(new List<LabelTemplate>());
        _fieldRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelTemplateField, bool>>>()))
            .ReturnsAsync(new List<LabelTemplateField>());
        _sizeRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new LabelSize { Id = 1, Name = "Standard" });

        // Act
        var result = await _service.CreateTemplateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(dto.Name);
        _templateRepoMock.Verify(r => r.AddAsync(It.IsAny<LabelTemplate>()), Times.Once);
    }

    [Fact]
    public async Task CreateTemplateAsync_WithFields_CreatesTemplateWithFields()
    {
        // Arrange
        var dto = new CreateLabelTemplateDto
        {
            Name = "Template with Fields",
            LabelSizeId = 1,
            StoreId = 1,
            PrintLanguage = LabelPrintLanguageDto.ZPL,
            TemplateContent = "^XA^XZ",
            Fields = new List<CreateLabelTemplateFieldDto>
            {
                new() { FieldName = "ProductName", FieldType = LabelFieldTypeDto.Text, PositionX = 50, PositionY = 50 },
                new() { FieldName = "Barcode", FieldType = LabelFieldTypeDto.Barcode, PositionX = 50, PositionY = 100 }
            }
        };

        _templateRepoMock.Setup(r => r.AddAsync(It.IsAny<LabelTemplate>()))
            .Callback<LabelTemplate>(t => t.Id = 1)
            .Returns(Task.CompletedTask);
        _templateRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new LabelTemplate { Id = 1, Name = dto.Name, StoreId = 1, LabelSizeId = 1, IsActive = true });
        _templateRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelTemplate, bool>>>()))
            .ReturnsAsync(new List<LabelTemplate>());
        _fieldRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelTemplateField, bool>>>()))
            .ReturnsAsync(new List<LabelTemplateField>());
        _sizeRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new LabelSize { Id = 1, Name = "Standard" });

        // Act
        await _service.CreateTemplateAsync(dto);

        // Assert
        _fieldRepoMock.Verify(r => r.AddAsync(It.IsAny<LabelTemplateField>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetTemplateAsync_ExistingId_ReturnsTemplate()
    {
        // Arrange
        var template = new LabelTemplate
        {
            Id = 1,
            Name = "Test Template",
            StoreId = 1,
            LabelSizeId = 1,
            PrintLanguage = LabelPrintLanguage.ZPL,
            TemplateContent = "^XA^XZ",
            IsActive = true
        };
        var size = new LabelSize { Id = 1, Name = "Standard" };

        _templateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(template);
        _fieldRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelTemplateField, bool>>>()))
            .ReturnsAsync(new List<LabelTemplateField>());
        _sizeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(size);

        // Act
        var result = await _service.GetTemplateAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Template");
        result.LabelSizeName.Should().Be("Standard");
    }

    [Fact]
    public async Task GetTemplateAsync_InactiveTemplate_ReturnsNull()
    {
        // Arrange
        var template = new LabelTemplate { Id = 1, IsActive = false };
        _templateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(template);

        // Act
        var result = await _service.GetTemplateAsync(1);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllTemplatesAsync_StoreId_ReturnsStoreTemplates()
    {
        // Arrange
        var templates = new List<LabelTemplate>
        {
            new() { Id = 1, Name = "Template A", StoreId = 1, LabelSizeId = 1, IsActive = true },
            new() { Id = 2, Name = "Template B", StoreId = 1, LabelSizeId = 1, IsActive = true }
        };

        _templateRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelTemplate, bool>>>()))
            .ReturnsAsync(templates);
        _fieldRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelTemplateField, bool>>>()))
            .ReturnsAsync(new List<LabelTemplateField>());
        _sizeRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new LabelSize { Id = 1, Name = "Standard" });

        // Act
        var result = await _service.GetAllTemplatesAsync(1);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPromoTemplatesAsync_ReturnsOnlyPromoTemplates()
    {
        // Arrange
        var promoTemplates = new List<LabelTemplate>
        {
            new() { Id = 1, Name = "Promo 1", StoreId = 1, LabelSizeId = 1, IsPromoTemplate = true, IsActive = true },
            new() { Id = 2, Name = "Promo 2", StoreId = 1, LabelSizeId = 1, IsPromoTemplate = true, IsActive = true }
        };

        _templateRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelTemplate, bool>>>()))
            .ReturnsAsync(promoTemplates);
        _fieldRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelTemplateField, bool>>>()))
            .ReturnsAsync(new List<LabelTemplateField>());
        _sizeRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new LabelSize { Id = 1, Name = "Standard" });

        // Act
        var result = await _service.GetPromoTemplatesAsync(1);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(t => t.IsPromoTemplate);
    }

    [Fact]
    public async Task UpdateTemplateAsync_ValidDto_UpdatesTemplate()
    {
        // Arrange
        var template = new LabelTemplate
        {
            Id = 1,
            Name = "Old Name",
            StoreId = 1,
            LabelSizeId = 1,
            PrintLanguage = LabelPrintLanguage.ZPL,
            Version = 1,
            IsActive = true
        };

        var dto = new UpdateLabelTemplateDto { Name = "New Name" };

        _templateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(template);
        _templateRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelTemplate, bool>>>()))
            .ReturnsAsync(new List<LabelTemplate>());
        _fieldRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelTemplateField, bool>>>()))
            .ReturnsAsync(new List<LabelTemplateField>());
        _sizeRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new LabelSize { Id = 1, Name = "Standard" });

        // Act
        var result = await _service.UpdateTemplateAsync(1, dto);

        // Assert
        result.Name.Should().Be("New Name");
        template.Version.Should().Be(2); // Version incremented
    }

    [Fact]
    public async Task DeleteTemplateAsync_NoAssignments_DeletesTemplate()
    {
        // Arrange
        var template = new LabelTemplate { Id = 1, Name = "Test", IsActive = true };

        _templateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(template);
        _assignmentRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<CategoryPrinterAssignment, bool>>>()))
            .ReturnsAsync(new List<CategoryPrinterAssignment>());
        _fieldRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelTemplateField, bool>>>()))
            .ReturnsAsync(new List<LabelTemplateField>());

        // Act
        var result = await _service.DeleteTemplateAsync(1);

        // Assert
        result.Should().BeTrue();
        template.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteTemplateAsync_HasAssignments_ThrowsException()
    {
        // Arrange
        var template = new LabelTemplate { Id = 1, Name = "Test", IsActive = true };
        var assignment = new CategoryPrinterAssignment { Id = 1, LabelTemplateId = 1, IsActive = true };

        _templateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(template);
        _assignmentRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<CategoryPrinterAssignment, bool>>>()))
            .ReturnsAsync(new List<CategoryPrinterAssignment> { assignment });

        // Act & Assert
        await _service.Invoking(s => s.DeleteTemplateAsync(1))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*assigned to categories*");
    }

    [Fact]
    public async Task DuplicateTemplateAsync_ValidTemplate_CreatesCopy()
    {
        // Arrange
        var original = new LabelTemplate
        {
            Id = 1,
            Name = "Original",
            StoreId = 1,
            LabelSizeId = 1,
            PrintLanguage = LabelPrintLanguage.ZPL,
            TemplateContent = "^XA^XZ",
            IsActive = true
        };
        var originalFields = new List<LabelTemplateField>
        {
            new() { Id = 1, LabelTemplateId = 1, FieldName = "Name", IsActive = true }
        };

        _templateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(original);
        _fieldRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelTemplateField, bool>>>()))
            .ReturnsAsync(originalFields);
        _templateRepoMock.Setup(r => r.AddAsync(It.IsAny<LabelTemplate>()))
            .Callback<LabelTemplate>(t => t.Id = 2)
            .Returns(Task.CompletedTask);
        _sizeRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new LabelSize { Id = 1, Name = "Standard" });

        // Act
        var result = await _service.DuplicateTemplateAsync(1, "Copy of Original");

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Copy of Original");
        result.IsDefault.Should().BeFalse();
        _fieldRepoMock.Verify(r => r.AddAsync(It.IsAny<LabelTemplateField>()), Times.Once);
    }

    #endregion

    #region Template Operations Tests

    [Fact]
    public async Task SetDefaultTemplateAsync_ValidTemplate_SetsAsDefault()
    {
        // Arrange
        var template = new LabelTemplate
        {
            Id = 1,
            StoreId = 1,
            PrintLanguage = LabelPrintLanguage.ZPL,
            IsDefault = false,
            IsActive = true
        };

        _templateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(template);
        _templateRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelTemplate, bool>>>()))
            .ReturnsAsync(new List<LabelTemplate>());

        // Act
        await _service.SetDefaultTemplateAsync(1, 1);

        // Assert
        template.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task GetDefaultTemplateAsync_HasDefault_ReturnsDefaultTemplate()
    {
        // Arrange
        var template = new LabelTemplate
        {
            Id = 1,
            Name = "Default Template",
            StoreId = 1,
            LabelSizeId = 1,
            IsDefault = true,
            IsActive = true
        };

        _templateRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelTemplate, bool>>>()))
            .ReturnsAsync(new List<LabelTemplate> { template });
        _fieldRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelTemplateField, bool>>>()))
            .ReturnsAsync(new List<LabelTemplateField>());
        _sizeRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new LabelSize { Id = 1, Name = "Standard" });

        // Act
        var result = await _service.GetDefaultTemplateAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task GeneratePreviewAsync_ValidRequest_ReturnsPreview()
    {
        // Arrange
        var template = new LabelTemplate
        {
            Id = 1,
            TemplateContent = "^XA^FO50,50^FD{{ProductName}}^FS^XZ",
            IsActive = true
        };

        _templateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(template);

        var request = new LabelPreviewRequestDto
        {
            TemplateId = 1,
            SampleData = new ProductLabelDataDto { ProductId = 1, ProductName = "Test Product", Price = 99.99m }
        };

        // Act
        var result = await _service.GeneratePreviewAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.LabelContent.Should().Contain("Test Product");
    }

    [Fact]
    public async Task ValidateTemplateAsync_ValidZpl_ReturnsValid()
    {
        // Arrange
        var content = "^XA^FO50,50^FD{{ProductName}}^FS^XZ";

        // Act
        var (isValid, errors) = await _service.ValidateTemplateAsync(content, LabelPrintLanguageDto.ZPL);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateTemplateAsync_InvalidZpl_ReturnsErrors()
    {
        // Arrange
        var content = "^FO50,50^FD{{ProductName}}^FS"; // Missing ^XA and ^XZ

        // Act
        var (isValid, errors) = await _service.ValidateTemplateAsync(content, LabelPrintLanguageDto.ZPL);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("^XA"));
        errors.Should().Contain(e => e.Contains("^XZ"));
    }

    [Fact]
    public async Task ValidateTemplateAsync_UnknownPlaceholder_ReturnsError()
    {
        // Arrange
        var content = "^XA^FO50,50^FD{{UnknownField}}^FS^XZ";

        // Act
        var (isValid, errors) = await _service.ValidateTemplateAsync(content, LabelPrintLanguageDto.ZPL);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("Unknown placeholder"));
    }

    #endregion

    #region Label Generation Tests

    [Fact]
    public async Task GenerateLabelContentAsync_ValidData_ReplacesPlaceholders()
    {
        // Arrange
        var template = new LabelTemplate
        {
            Id = 1,
            TemplateContent = "^XA^FO50,50^FD{{ProductName}}^FS^FO50,100^FD{{Price}}^FS^XZ",
            IsActive = true
        };
        var data = new ProductLabelDataDto
        {
            ProductId = 1,
            ProductName = "Test Product",
            Price = 199.99m
        };

        _templateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(template);

        // Act
        var result = await _service.GenerateLabelContentAsync(1, data);

        // Assert
        result.Should().Contain("Test Product");
        result.Should().Contain("KSh 199.99");
        result.Should().NotContain("{{ProductName}}");
        result.Should().NotContain("{{Price}}");
    }

    [Fact]
    public async Task GenerateBatchLabelContentAsync_MultipleProducts_GeneratesAll()
    {
        // Arrange
        var template = new LabelTemplate
        {
            Id = 1,
            TemplateContent = "^XA^FD{{ProductName}}^FS^XZ",
            IsActive = true
        };
        var dataList = new List<ProductLabelDataDto>
        {
            new() { ProductId = 1, ProductName = "Product A", Price = 10m },
            new() { ProductId = 2, ProductName = "Product B", Price = 20m },
            new() { ProductId = 3, ProductName = "Product C", Price = 30m }
        };

        _templateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(template);

        // Act
        var result = await _service.GenerateBatchLabelContentAsync(1, dataList);

        // Assert
        result.Should().HaveCount(3);
        result[0].Should().Contain("Product A");
        result[1].Should().Contain("Product B");
        result[2].Should().Contain("Product C");
    }

    [Fact]
    public void GetAvailablePlaceholders_ReturnsAllPlaceholders()
    {
        // Act
        var result = _service.GetAvailablePlaceholders();

        // Assert
        result.Should().Contain("{{ProductName}}");
        result.Should().Contain("{{Barcode}}");
        result.Should().Contain("{{Price}}");
        result.Should().Contain("{{OriginalPrice}}");
        result.Should().Contain("{{CurrentDate}}");
    }

    #endregion

    #region Template Library Tests

    [Fact]
    public async Task GetLibraryTemplatesAsync_ReturnsAllLibraryTemplates()
    {
        // Arrange
        var libraryTemplates = new List<LabelTemplateLibrary>
        {
            new() { Id = 1, Name = "Standard", Category = "Basic", IsActive = true },
            new() { Id = 2, Name = "Promo", Category = "Marketing", IsActive = true }
        };

        _libraryRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelTemplateLibrary, bool>>>()))
            .ReturnsAsync(libraryTemplates);

        // Act
        var result = await _service.GetLibraryTemplatesAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetLibraryTemplatesByCategoryAsync_ReturnsFilteredTemplates()
    {
        // Arrange
        var marketingTemplates = new List<LabelTemplateLibrary>
        {
            new() { Id = 2, Name = "Promo", Category = "Marketing", IsActive = true }
        };

        _libraryRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelTemplateLibrary, bool>>>()))
            .ReturnsAsync(marketingTemplates);

        // Act
        var result = await _service.GetLibraryTemplatesByCategoryAsync("Marketing");

        // Assert
        result.Should().HaveCount(1);
        result[0].Category.Should().Be("Marketing");
    }

    [Fact]
    public async Task ImportFromLibraryAsync_ValidLibraryTemplate_ImportsSuccessfully()
    {
        // Arrange
        var libraryTemplate = new LabelTemplateLibrary
        {
            Id = 1,
            Name = "Standard Label",
            PrintLanguage = LabelPrintLanguage.ZPL,
            TemplateContent = "^XA^XZ",
            IsActive = true
        };

        var dto = new ImportTemplateFromLibraryDto
        {
            LibraryTemplateId = 1,
            Name = "My Standard Label",
            LabelSizeId = 1,
            StoreId = 1
        };

        _libraryRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(libraryTemplate);
        _templateRepoMock.Setup(r => r.AddAsync(It.IsAny<LabelTemplate>()))
            .Callback<LabelTemplate>(t => t.Id = 1)
            .Returns(Task.CompletedTask);
        _templateRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new LabelTemplate { Id = 1, Name = "My Standard Label", StoreId = 1, LabelSizeId = 1, IsActive = true });
        _templateRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelTemplate, bool>>>()))
            .ReturnsAsync(new List<LabelTemplate>());
        _fieldRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelTemplateField, bool>>>()))
            .ReturnsAsync(new List<LabelTemplateField>());
        _sizeRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new LabelSize { Id = 1, Name = "Standard" });

        // Act
        var result = await _service.ImportFromLibraryAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("My Standard Label");
    }

    #endregion

    #region Template Field Tests

    [Fact]
    public async Task AddFieldAsync_ValidDto_AddsField()
    {
        // Arrange
        var template = new LabelTemplate { Id = 1, Name = "Test", IsActive = true };
        var dto = new CreateLabelTemplateFieldDto
        {
            FieldName = "ProductName",
            FieldType = LabelFieldTypeDto.Text,
            PositionX = 50,
            PositionY = 50,
            FontSize = 24
        };

        _templateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(template);
        _fieldRepoMock.Setup(r => r.AddAsync(It.IsAny<LabelTemplateField>()))
            .Callback<LabelTemplateField>(f => f.Id = 1)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.AddFieldAsync(1, dto);

        // Assert
        result.Should().NotBeNull();
        result.FieldName.Should().Be("ProductName");
        _fieldRepoMock.Verify(r => r.AddAsync(It.IsAny<LabelTemplateField>()), Times.Once);
    }

    [Fact]
    public async Task UpdateFieldAsync_ValidDto_UpdatesField()
    {
        // Arrange
        var field = new LabelTemplateField
        {
            Id = 1,
            LabelTemplateId = 1,
            FieldName = "OldName",
            PositionX = 50,
            PositionY = 50,
            IsActive = true
        };
        var dto = new CreateLabelTemplateFieldDto
        {
            FieldName = "NewName",
            FieldType = LabelFieldTypeDto.Text,
            PositionX = 100,
            PositionY = 100
        };

        _fieldRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(field);

        // Act
        var result = await _service.UpdateFieldAsync(1, dto);

        // Assert
        result.FieldName.Should().Be("NewName");
        result.PositionX.Should().Be(100);
    }

    [Fact]
    public async Task RemoveFieldAsync_ValidId_RemovesField()
    {
        // Arrange
        var field = new LabelTemplateField { Id = 1, IsActive = true };
        _fieldRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(field);

        // Act
        var result = await _service.RemoveFieldAsync(1);

        // Assert
        result.Should().BeTrue();
        field.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ReorderFieldsAsync_ValidIds_ReordersFields()
    {
        // Arrange
        var fields = new List<LabelTemplateField>
        {
            new() { Id = 1, LabelTemplateId = 1, DisplayOrder = 0 },
            new() { Id = 2, LabelTemplateId = 1, DisplayOrder = 1 },
            new() { Id = 3, LabelTemplateId = 1, DisplayOrder = 2 }
        };

        _fieldRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(fields[0]);
        _fieldRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(fields[1]);
        _fieldRepoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(fields[2]);

        var newOrder = new List<int> { 3, 1, 2 }; // Reordered

        // Act
        await _service.ReorderFieldsAsync(1, newOrder);

        // Assert
        fields[2].DisplayOrder.Should().Be(0);
        fields[0].DisplayOrder.Should().Be(1);
        fields[1].DisplayOrder.Should().Be(2);
    }

    #endregion

    #region Events Tests

    [Fact]
    public async Task CreateTemplateAsync_RaisesTemplateCreatedEvent()
    {
        // Arrange
        var dto = new CreateLabelTemplateDto
        {
            Name = "Test",
            LabelSizeId = 1,
            StoreId = 1,
            PrintLanguage = LabelPrintLanguageDto.ZPL,
            TemplateContent = "^XA^XZ"
        };

        _templateRepoMock.Setup(r => r.AddAsync(It.IsAny<LabelTemplate>()))
            .Callback<LabelTemplate>(t => t.Id = 1)
            .Returns(Task.CompletedTask);
        _templateRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new LabelTemplate { Id = 1, Name = dto.Name, StoreId = 1, LabelSizeId = 1, IsActive = true });
        _templateRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelTemplate, bool>>>()))
            .ReturnsAsync(new List<LabelTemplate>());
        _fieldRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<LabelTemplateField, bool>>>()))
            .ReturnsAsync(new List<LabelTemplateField>());
        _sizeRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new LabelSize { Id = 1, Name = "Standard" });

        LabelTemplateDto? eventTemplate = null;
        _service.TemplateCreated += (s, t) => eventTemplate = t;

        // Act
        await _service.CreateTemplateAsync(dto);

        // Assert
        eventTemplate.Should().NotBeNull();
        eventTemplate!.Name.Should().Be("Test");
    }

    #endregion
}
