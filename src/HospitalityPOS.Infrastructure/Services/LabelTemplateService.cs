using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.RegularExpressions;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for label template management and generation.
/// </summary>
public class LabelTemplateService : ILabelTemplateService
{
    private readonly IRepository<LabelTemplate> _templateRepository;
    private readonly IRepository<LabelTemplateField> _fieldRepository;
    private readonly IRepository<LabelSize> _sizeRepository;
    private readonly IRepository<LabelTemplateLibrary> _libraryRepository;
    private readonly IRepository<CategoryPrinterAssignment> _assignmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LabelTemplateService> _logger;

    private static readonly List<string> AvailablePlaceholders = new()
    {
        "{{ProductName}}",
        "{{ProductNameLine1}}",
        "{{ProductNameLine2}}",
        "{{Barcode}}",
        "{{Price}}",
        "{{UnitPrice}}",
        "{{OriginalPrice}}",
        "{{Description}}",
        "{{SKU}}",
        "{{CategoryName}}",
        "{{PromoText}}",
        "{{UnitOfMeasure}}",
        "{{EffectiveDate}}",
        "{{CurrentDate}}",
        "{{CurrentTime}}"
    };

    public event EventHandler<LabelTemplateDto>? TemplateCreated;
    public event EventHandler<LabelTemplateDto>? TemplateUpdated;

    public LabelTemplateService(
        IRepository<LabelTemplate> templateRepository,
        IRepository<LabelTemplateField> fieldRepository,
        IRepository<LabelSize> sizeRepository,
        IRepository<LabelTemplateLibrary> libraryRepository,
        IRepository<CategoryPrinterAssignment> assignmentRepository,
        IUnitOfWork unitOfWork,
        ILogger<LabelTemplateService> logger)
    {
        _templateRepository = templateRepository ?? throw new ArgumentNullException(nameof(templateRepository));
        _fieldRepository = fieldRepository ?? throw new ArgumentNullException(nameof(fieldRepository));
        _sizeRepository = sizeRepository ?? throw new ArgumentNullException(nameof(sizeRepository));
        _libraryRepository = libraryRepository ?? throw new ArgumentNullException(nameof(libraryRepository));
        _assignmentRepository = assignmentRepository ?? throw new ArgumentNullException(nameof(assignmentRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Template CRUD

    public async Task<LabelTemplateDto> CreateTemplateAsync(CreateLabelTemplateDto dto)
    {
        var template = new LabelTemplate
        {
            Name = dto.Name,
            LabelSizeId = dto.LabelSizeId,
            StoreId = dto.StoreId,
            PrintLanguage = (LabelPrintLanguage)dto.PrintLanguage,
            TemplateContent = dto.TemplateContent,
            IsDefault = dto.IsDefault,
            IsPromoTemplate = dto.IsPromoTemplate,
            Description = dto.Description,
            Version = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        if (dto.IsDefault)
        {
            await ClearDefaultTemplateAsync(dto.StoreId, (LabelPrintLanguage)dto.PrintLanguage);
        }

        await _templateRepository.AddAsync(template);
        await _unitOfWork.SaveChangesAsync();

        // Add fields if provided
        if (dto.Fields != null && dto.Fields.Count > 0)
        {
            foreach (var fieldDto in dto.Fields)
            {
                var field = CreateFieldFromDto(template.Id, fieldDto);
                await _fieldRepository.AddAsync(field);
            }
            await _unitOfWork.SaveChangesAsync();
        }

        _logger.LogInformation("Created label template {TemplateId} - {Name}", template.Id, template.Name);

        var result = await GetTemplateAsync(template.Id) ?? throw new InvalidOperationException("Failed to retrieve created template");
        TemplateCreated?.Invoke(this, result);
        return result;
    }

    public async Task<LabelTemplateDto?> GetTemplateAsync(int templateId)
    {
        var template = await _templateRepository.GetByIdAsync(templateId);
        if (template == null || !template.IsActive) return null;

        var fields = await _fieldRepository.FindAsync(f => f.LabelTemplateId == templateId && f.IsActive);
        var size = await _sizeRepository.GetByIdAsync(template.LabelSizeId);

        return MapToDto(template, fields.ToList(), size);
    }

    public async Task<List<LabelTemplateDto>> GetAllTemplatesAsync(int storeId)
    {
        var templates = await _templateRepository.FindAsync(t => t.StoreId == storeId && t.IsActive);
        var result = new List<LabelTemplateDto>();

        foreach (var template in templates.OrderBy(t => t.Name))
        {
            var fields = await _fieldRepository.FindAsync(f => f.LabelTemplateId == template.Id && f.IsActive);
            var size = await _sizeRepository.GetByIdAsync(template.LabelSizeId);
            result.Add(MapToDto(template, fields.ToList(), size));
        }

        return result;
    }

    public async Task<List<LabelTemplateDto>> GetTemplatesBySizeAsync(int labelSizeId, int storeId)
    {
        var templates = await _templateRepository.FindAsync(t =>
            t.StoreId == storeId && t.LabelSizeId == labelSizeId && t.IsActive);

        var result = new List<LabelTemplateDto>();
        var size = await _sizeRepository.GetByIdAsync(labelSizeId);

        foreach (var template in templates.OrderBy(t => t.Name))
        {
            var fields = await _fieldRepository.FindAsync(f => f.LabelTemplateId == template.Id && f.IsActive);
            result.Add(MapToDto(template, fields.ToList(), size));
        }

        return result;
    }

    public async Task<List<LabelTemplateDto>> GetPromoTemplatesAsync(int storeId)
    {
        var templates = await _templateRepository.FindAsync(t =>
            t.StoreId == storeId && t.IsPromoTemplate && t.IsActive);

        var result = new List<LabelTemplateDto>();

        foreach (var template in templates.OrderBy(t => t.Name))
        {
            var fields = await _fieldRepository.FindAsync(f => f.LabelTemplateId == template.Id && f.IsActive);
            var size = await _sizeRepository.GetByIdAsync(template.LabelSizeId);
            result.Add(MapToDto(template, fields.ToList(), size));
        }

        return result;
    }

    public async Task<LabelTemplateDto> UpdateTemplateAsync(int templateId, UpdateLabelTemplateDto dto)
    {
        var template = await _templateRepository.GetByIdAsync(templateId);
        if (template == null || !template.IsActive)
            throw new KeyNotFoundException($"Template {templateId} not found");

        if (dto.Name != null) template.Name = dto.Name;
        if (dto.LabelSizeId.HasValue) template.LabelSizeId = dto.LabelSizeId.Value;
        if (dto.PrintLanguage.HasValue) template.PrintLanguage = (LabelPrintLanguage)dto.PrintLanguage.Value;
        if (dto.TemplateContent != null) template.TemplateContent = dto.TemplateContent;
        if (dto.IsPromoTemplate.HasValue) template.IsPromoTemplate = dto.IsPromoTemplate.Value;
        if (dto.Description != null) template.Description = dto.Description;

        if (dto.IsDefault.HasValue && dto.IsDefault.Value && !template.IsDefault)
        {
            await ClearDefaultTemplateAsync(template.StoreId, template.PrintLanguage);
            template.IsDefault = true;
        }
        else if (dto.IsDefault.HasValue && !dto.IsDefault.Value)
        {
            template.IsDefault = false;
        }

        template.Version++;
        template.UpdatedAt = DateTime.UtcNow;
        await _templateRepository.UpdateAsync(template);

        // Update fields if provided
        if (dto.Fields != null)
        {
            await UpdateTemplateFieldsAsync(templateId, dto.Fields);
        }

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Updated label template {TemplateId}", templateId);

        var result = await GetTemplateAsync(templateId) ?? throw new InvalidOperationException("Failed to retrieve updated template");
        TemplateUpdated?.Invoke(this, result);
        return result;
    }

    public async Task<bool> DeleteTemplateAsync(int templateId)
    {
        var template = await _templateRepository.GetByIdAsync(templateId);
        if (template == null) return false;

        // Check for category assignments
        var assignments = await _assignmentRepository.FindAsync(a => a.LabelTemplateId == templateId && a.IsActive);
        if (assignments.Any())
            throw new InvalidOperationException("Cannot delete template that is assigned to categories");

        template.IsActive = false;
        template.UpdatedAt = DateTime.UtcNow;
        await _templateRepository.UpdateAsync(template);

        // Deactivate fields
        var fields = await _fieldRepository.FindAsync(f => f.LabelTemplateId == templateId && f.IsActive);
        foreach (var field in fields)
        {
            field.IsActive = false;
            field.UpdatedAt = DateTime.UtcNow;
            await _fieldRepository.UpdateAsync(field);
        }

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Deleted label template {TemplateId}", templateId);

        return true;
    }

    public async Task<LabelTemplateDto> DuplicateTemplateAsync(int templateId, string newName)
    {
        var original = await _templateRepository.GetByIdAsync(templateId);
        if (original == null || !original.IsActive)
            throw new KeyNotFoundException($"Template {templateId} not found");

        var originalFields = await _fieldRepository.FindAsync(f => f.LabelTemplateId == templateId && f.IsActive);

        var duplicate = new LabelTemplate
        {
            Name = newName,
            LabelSizeId = original.LabelSizeId,
            StoreId = original.StoreId,
            PrintLanguage = original.PrintLanguage,
            TemplateContent = original.TemplateContent,
            IsDefault = false,
            IsPromoTemplate = original.IsPromoTemplate,
            Description = $"Copy of {original.Name}",
            Version = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _templateRepository.AddAsync(duplicate);
        await _unitOfWork.SaveChangesAsync();

        // Copy fields
        foreach (var originalField in originalFields.OrderBy(f => f.DisplayOrder))
        {
            var duplicateField = new LabelTemplateField
            {
                LabelTemplateId = duplicate.Id,
                FieldName = originalField.FieldName,
                FieldType = originalField.FieldType,
                PositionX = originalField.PositionX,
                PositionY = originalField.PositionY,
                Width = originalField.Width,
                Height = originalField.Height,
                FontName = originalField.FontName,
                FontSize = originalField.FontSize,
                Alignment = originalField.Alignment,
                IsBold = originalField.IsBold,
                Rotation = originalField.Rotation,
                BarcodeType = originalField.BarcodeType,
                BarcodeHeight = originalField.BarcodeHeight,
                ShowBarcodeText = originalField.ShowBarcodeText,
                DisplayOrder = originalField.DisplayOrder,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _fieldRepository.AddAsync(duplicateField);
        }

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Duplicated template {OriginalId} to {NewId} as {Name}", templateId, duplicate.Id, newName);

        return await GetTemplateAsync(duplicate.Id) ?? throw new InvalidOperationException("Failed to retrieve duplicated template");
    }

    #endregion

    #region Template Operations

    public async Task SetDefaultTemplateAsync(int templateId, int storeId)
    {
        var template = await _templateRepository.GetByIdAsync(templateId);
        if (template == null || !template.IsActive)
            throw new KeyNotFoundException($"Template {templateId} not found");

        await ClearDefaultTemplateAsync(storeId, template.PrintLanguage);

        template.IsDefault = true;
        template.UpdatedAt = DateTime.UtcNow;
        await _templateRepository.UpdateAsync(template);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<LabelTemplateDto?> GetDefaultTemplateAsync(int storeId, LabelPrintLanguageDto? printLanguage = null)
    {
        IEnumerable<LabelTemplate> templates;
        if (printLanguage.HasValue)
        {
            templates = await _templateRepository.FindAsync(t =>
                t.StoreId == storeId && t.IsDefault && t.IsActive &&
                t.PrintLanguage == (LabelPrintLanguage)printLanguage.Value);
        }
        else
        {
            templates = await _templateRepository.FindAsync(t =>
                t.StoreId == storeId && t.IsDefault && t.IsActive);
        }

        var template = templates.FirstOrDefault();
        if (template == null) return null;

        var fields = await _fieldRepository.FindAsync(f => f.LabelTemplateId == template.Id && f.IsActive);
        var size = await _sizeRepository.GetByIdAsync(template.LabelSizeId);

        return MapToDto(template, fields.ToList(), size);
    }

    public async Task<LabelTemplateDto?> GetTemplateForCategoryAsync(int categoryId, int storeId)
    {
        var assignments = await _assignmentRepository.FindAsync(a =>
            a.CategoryId == categoryId && a.StoreId == storeId && a.IsActive && a.LabelTemplateId.HasValue);
        var assignment = assignments.FirstOrDefault();

        if (assignment?.LabelTemplateId != null)
        {
            return await GetTemplateAsync(assignment.LabelTemplateId.Value);
        }

        // Fall back to default template
        return await GetDefaultTemplateAsync(storeId);
    }

    public async Task<LabelPreviewResultDto> GeneratePreviewAsync(LabelPreviewRequestDto request)
    {
        try
        {
            var template = await _templateRepository.GetByIdAsync(request.TemplateId);
            if (template == null)
            {
                return new LabelPreviewResultDto
                {
                    Success = false,
                    ErrorMessage = "Template not found"
                };
            }

            var data = request.SampleData ?? GetSampleProductData();
            var content = await GenerateLabelContentAsync(request.TemplateId, data);

            return new LabelPreviewResultDto
            {
                Success = true,
                LabelContent = content,
                PreviewImageBase64 = null // Image generation would require additional graphics libraries
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate preview for template {TemplateId}", request.TemplateId);
            return new LabelPreviewResultDto
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public Task<(bool IsValid, List<string> Errors)> ValidateTemplateAsync(string templateContent, LabelPrintLanguageDto language)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(templateContent))
        {
            errors.Add("Template content cannot be empty");
            return Task.FromResult((false, errors));
        }

        switch (language)
        {
            case LabelPrintLanguageDto.ZPL:
                ValidateZplTemplate(templateContent, errors);
                break;
            case LabelPrintLanguageDto.EPL:
                ValidateEplTemplate(templateContent, errors);
                break;
            case LabelPrintLanguageDto.TSPL:
                ValidateTsplTemplate(templateContent, errors);
                break;
        }

        // Check for valid placeholders
        var placeholderPattern = @"\{\{(\w+)\}\}";
        var matches = Regex.Matches(templateContent, placeholderPattern);
        foreach (Match match in matches)
        {
            var placeholder = $"{{{{{match.Groups[1].Value}}}}}";
            if (!AvailablePlaceholders.Contains(placeholder))
            {
                errors.Add($"Unknown placeholder: {placeholder}");
            }
        }

        return Task.FromResult((errors.Count == 0, errors));
    }

    #endregion

    #region Label Generation

    public async Task<string> GenerateLabelContentAsync(int templateId, ProductLabelDataDto data)
    {
        var template = await _templateRepository.GetByIdAsync(templateId);
        if (template == null)
            throw new KeyNotFoundException($"Template {templateId} not found");

        return ReplacePlaceholders(template.TemplateContent, data);
    }

    public async Task<List<string>> GenerateBatchLabelContentAsync(int templateId, List<ProductLabelDataDto> dataList)
    {
        var template = await _templateRepository.GetByIdAsync(templateId);
        if (template == null)
            throw new KeyNotFoundException($"Template {templateId} not found");

        var results = new List<string>();
        foreach (var data in dataList)
        {
            results.Add(ReplacePlaceholders(template.TemplateContent, data));
        }

        return results;
    }

    public List<string> GetAvailablePlaceholders()
    {
        return new List<string>(AvailablePlaceholders);
    }

    #endregion

    #region Template Library

    public async Task<List<LabelTemplateLibraryDto>> GetLibraryTemplatesAsync()
    {
        var templates = await _libraryRepository.FindAsync(t => t.IsActive);
        return templates.OrderBy(t => t.Category).ThenBy(t => t.Name)
            .Select(MapToLibraryDto).ToList();
    }

    public async Task<List<LabelTemplateLibraryDto>> GetLibraryTemplatesByCategoryAsync(string category)
    {
        var templates = await _libraryRepository.FindAsync(t => t.Category == category && t.IsActive);
        return templates.OrderBy(t => t.Name).Select(MapToLibraryDto).ToList();
    }

    public async Task<LabelTemplateDto> ImportFromLibraryAsync(ImportTemplateFromLibraryDto dto)
    {
        var libraryTemplate = await _libraryRepository.GetByIdAsync(dto.LibraryTemplateId);
        if (libraryTemplate == null)
            throw new KeyNotFoundException($"Library template {dto.LibraryTemplateId} not found");

        var createDto = new CreateLabelTemplateDto
        {
            Name = dto.Name,
            LabelSizeId = dto.LabelSizeId,
            StoreId = dto.StoreId,
            PrintLanguage = (LabelPrintLanguageDto)libraryTemplate.PrintLanguage,
            TemplateContent = libraryTemplate.TemplateContent,
            IsDefault = false,
            IsPromoTemplate = false,
            Description = $"Imported from library: {libraryTemplate.Name}"
        };

        var result = await CreateTemplateAsync(createDto);
        _logger.LogInformation("Imported library template {LibraryId} as {TemplateId}", dto.LibraryTemplateId, result.Id);

        return result;
    }

    #endregion

    #region Template Fields

    public async Task<LabelTemplateFieldDto> AddFieldAsync(int templateId, CreateLabelTemplateFieldDto dto)
    {
        var template = await _templateRepository.GetByIdAsync(templateId);
        if (template == null || !template.IsActive)
            throw new KeyNotFoundException($"Template {templateId} not found");

        var field = CreateFieldFromDto(templateId, dto);
        await _fieldRepository.AddAsync(field);
        await _unitOfWork.SaveChangesAsync();

        return MapToFieldDto(field);
    }

    public async Task<LabelTemplateFieldDto> UpdateFieldAsync(int fieldId, CreateLabelTemplateFieldDto dto)
    {
        var field = await _fieldRepository.GetByIdAsync(fieldId);
        if (field == null || !field.IsActive)
            throw new KeyNotFoundException($"Field {fieldId} not found");

        field.FieldName = dto.FieldName;
        field.FieldType = (LabelFieldType)dto.FieldType;
        field.PositionX = dto.PositionX;
        field.PositionY = dto.PositionY;
        field.Width = dto.Width;
        field.Height = dto.Height;
        field.FontName = dto.FontName;
        field.FontSize = dto.FontSize;
        field.Alignment = (TextAlignment)dto.Alignment;
        field.IsBold = dto.IsBold;
        field.Rotation = dto.Rotation;
        field.BarcodeType = dto.BarcodeType.HasValue ? (BarcodeType)dto.BarcodeType.Value : null;
        field.BarcodeHeight = dto.BarcodeHeight;
        field.ShowBarcodeText = dto.ShowBarcodeText;
        field.DisplayOrder = dto.DisplayOrder;
        field.UpdatedAt = DateTime.UtcNow;

        await _fieldRepository.UpdateAsync(field);
        await _unitOfWork.SaveChangesAsync();

        return MapToFieldDto(field);
    }

    public async Task<bool> RemoveFieldAsync(int fieldId)
    {
        var field = await _fieldRepository.GetByIdAsync(fieldId);
        if (field == null) return false;

        field.IsActive = false;
        field.UpdatedAt = DateTime.UtcNow;
        await _fieldRepository.UpdateAsync(field);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task ReorderFieldsAsync(int templateId, List<int> fieldIds)
    {
        for (int i = 0; i < fieldIds.Count; i++)
        {
            var field = await _fieldRepository.GetByIdAsync(fieldIds[i]);
            if (field != null && field.LabelTemplateId == templateId)
            {
                field.DisplayOrder = i;
                field.UpdatedAt = DateTime.UtcNow;
                await _fieldRepository.UpdateAsync(field);
            }
        }

        await _unitOfWork.SaveChangesAsync();
    }

    #endregion

    #region Private Methods

    private async Task ClearDefaultTemplateAsync(int storeId, LabelPrintLanguage language)
    {
        var templates = await _templateRepository.FindAsync(t =>
            t.StoreId == storeId && t.PrintLanguage == language && t.IsDefault);

        foreach (var template in templates)
        {
            template.IsDefault = false;
            template.UpdatedAt = DateTime.UtcNow;
            await _templateRepository.UpdateAsync(template);
        }
    }

    private async Task UpdateTemplateFieldsAsync(int templateId, List<UpdateLabelTemplateFieldDto> fieldDtos)
    {
        var existingFields = await _fieldRepository.FindAsync(f => f.LabelTemplateId == templateId && f.IsActive);
        var existingIds = existingFields.Select(f => f.Id).ToHashSet();
        var updatedIds = new HashSet<int>();

        foreach (var dto in fieldDtos)
        {
            if (dto.Id.HasValue && existingIds.Contains(dto.Id.Value))
            {
                // Update existing field
                var field = existingFields.First(f => f.Id == dto.Id.Value);
                field.FieldName = dto.FieldName;
                field.FieldType = (LabelFieldType)dto.FieldType;
                field.PositionX = dto.PositionX;
                field.PositionY = dto.PositionY;
                field.Width = dto.Width;
                field.Height = dto.Height;
                field.FontName = dto.FontName;
                field.FontSize = dto.FontSize;
                field.Alignment = (TextAlignment)dto.Alignment;
                field.IsBold = dto.IsBold;
                field.Rotation = dto.Rotation;
                field.BarcodeType = dto.BarcodeType.HasValue ? (BarcodeType)dto.BarcodeType.Value : null;
                field.BarcodeHeight = dto.BarcodeHeight;
                field.ShowBarcodeText = dto.ShowBarcodeText;
                field.DisplayOrder = dto.DisplayOrder;
                field.UpdatedAt = DateTime.UtcNow;
                await _fieldRepository.UpdateAsync(field);
                updatedIds.Add(dto.Id.Value);
            }
            else
            {
                // Add new field
                var field = CreateFieldFromDto(templateId, dto);
                await _fieldRepository.AddAsync(field);
            }
        }

        // Remove fields not in update list
        foreach (var field in existingFields.Where(f => !updatedIds.Contains(f.Id)))
        {
            field.IsActive = false;
            field.UpdatedAt = DateTime.UtcNow;
            await _fieldRepository.UpdateAsync(field);
        }
    }

    private LabelTemplateField CreateFieldFromDto(int templateId, CreateLabelTemplateFieldDto dto)
    {
        return new LabelTemplateField
        {
            LabelTemplateId = templateId,
            FieldName = dto.FieldName,
            FieldType = (LabelFieldType)dto.FieldType,
            PositionX = dto.PositionX,
            PositionY = dto.PositionY,
            Width = dto.Width,
            Height = dto.Height,
            FontName = dto.FontName,
            FontSize = dto.FontSize,
            Alignment = (TextAlignment)dto.Alignment,
            IsBold = dto.IsBold,
            Rotation = dto.Rotation,
            BarcodeType = dto.BarcodeType.HasValue ? (BarcodeType)dto.BarcodeType.Value : null,
            BarcodeHeight = dto.BarcodeHeight,
            ShowBarcodeText = dto.ShowBarcodeText,
            DisplayOrder = dto.DisplayOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    private string ReplacePlaceholders(string template, ProductLabelDataDto data)
    {
        var result = template;

        // Product-specific placeholders
        result = result.Replace("{{ProductName}}", data.ProductName);
        result = result.Replace("{{ProductNameLine1}}", GetProductNameLine(data.ProductName, 1));
        result = result.Replace("{{ProductNameLine2}}", GetProductNameLine(data.ProductName, 2));
        result = result.Replace("{{Barcode}}", data.Barcode ?? string.Empty);
        result = result.Replace("{{Price}}", FormatPrice(data.Price));
        result = result.Replace("{{UnitPrice}}", data.UnitPrice ?? string.Empty);
        result = result.Replace("{{OriginalPrice}}", data.OriginalPrice.HasValue ? FormatPrice(data.OriginalPrice.Value) : string.Empty);
        result = result.Replace("{{Description}}", data.Description ?? string.Empty);
        result = result.Replace("{{SKU}}", data.SKU ?? string.Empty);
        result = result.Replace("{{CategoryName}}", data.CategoryName ?? string.Empty);
        result = result.Replace("{{PromoText}}", data.PromoText ?? string.Empty);
        result = result.Replace("{{UnitOfMeasure}}", data.UnitOfMeasure ?? string.Empty);
        result = result.Replace("{{EffectiveDate}}", data.EffectiveDate?.ToString("yyyy-MM-dd") ?? string.Empty);

        // Dynamic placeholders
        result = result.Replace("{{CurrentDate}}", DateTime.Now.ToString("yyyy-MM-dd"));
        result = result.Replace("{{CurrentTime}}", DateTime.Now.ToString("HH:mm"));

        return result;
    }

    private string GetProductNameLine(string productName, int line)
    {
        if (string.IsNullOrEmpty(productName)) return string.Empty;

        var words = productName.Split(' ');
        if (words.Length <= 3)
        {
            return line == 1 ? productName : string.Empty;
        }

        var midpoint = words.Length / 2;
        if (line == 1)
        {
            return string.Join(" ", words.Take(midpoint));
        }
        return string.Join(" ", words.Skip(midpoint));
    }

    private string FormatPrice(decimal price)
    {
        return $"KSh {price:N2}";
    }

    private ProductLabelDataDto GetSampleProductData()
    {
        return new ProductLabelDataDto
        {
            ProductId = 1,
            ProductName = "Sample Product Name",
            Barcode = "5901234123457",
            Price = 199.99m,
            UnitPrice = "KSh 49.99/kg",
            Description = "Sample product description",
            SKU = "SKU-001",
            CategoryName = "Sample Category",
            OriginalPrice = 249.99m,
            PromoText = "SPECIAL OFFER!",
            UnitOfMeasure = "kg",
            EffectiveDate = DateTime.Today
        };
    }

    private void ValidateZplTemplate(string content, List<string> errors)
    {
        if (!content.Contains("^XA"))
        {
            errors.Add("ZPL template must start with ^XA");
        }
        if (!content.Contains("^XZ"))
        {
            errors.Add("ZPL template must end with ^XZ");
        }
    }

    private void ValidateEplTemplate(string content, List<string> errors)
    {
        if (!content.TrimStart().StartsWith("N"))
        {
            errors.Add("EPL template should start with N (new label)");
        }
        if (!content.Contains("P"))
        {
            errors.Add("EPL template should contain P (print) command");
        }
    }

    private void ValidateTsplTemplate(string content, List<string> errors)
    {
        if (!content.Contains("SIZE"))
        {
            errors.Add("TSPL template should contain SIZE command");
        }
        if (!content.Contains("PRINT"))
        {
            errors.Add("TSPL template should contain PRINT command");
        }
    }

    private LabelTemplateDto MapToDto(LabelTemplate template, List<LabelTemplateField> fields, LabelSize? size)
    {
        return new LabelTemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            LabelSizeId = template.LabelSizeId,
            LabelSizeName = size?.Name,
            StoreId = template.StoreId,
            PrintLanguage = (LabelPrintLanguageDto)template.PrintLanguage,
            TemplateContent = template.TemplateContent,
            IsDefault = template.IsDefault,
            IsPromoTemplate = template.IsPromoTemplate,
            Description = template.Description,
            Version = template.Version,
            Fields = fields.OrderBy(f => f.DisplayOrder).Select(MapToFieldDto).ToList()
        };
    }

    private LabelTemplateFieldDto MapToFieldDto(LabelTemplateField field)
    {
        return new LabelTemplateFieldDto
        {
            Id = field.Id,
            FieldName = field.FieldName,
            FieldType = (LabelFieldTypeDto)field.FieldType,
            PositionX = field.PositionX,
            PositionY = field.PositionY,
            Width = field.Width,
            Height = field.Height,
            FontName = field.FontName,
            FontSize = field.FontSize,
            Alignment = (TextAlignmentDto)field.Alignment,
            IsBold = field.IsBold,
            Rotation = field.Rotation,
            BarcodeType = field.BarcodeType.HasValue ? (BarcodeTypeDto)field.BarcodeType.Value : null,
            BarcodeHeight = field.BarcodeHeight,
            ShowBarcodeText = field.ShowBarcodeText,
            DisplayOrder = field.DisplayOrder
        };
    }

    private LabelTemplateLibraryDto MapToLibraryDto(LabelTemplateLibrary template)
    {
        return new LabelTemplateLibraryDto
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            PrintLanguage = (LabelPrintLanguageDto)template.PrintLanguage,
            TemplateContent = template.TemplateContent,
            WidthMm = template.WidthMm,
            HeightMm = template.HeightMm,
            IsBuiltIn = template.IsBuiltIn,
            Category = template.Category
        };
    }

    #endregion
}
