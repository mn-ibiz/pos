using HospitalityPOS.Core.DTOs;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for label template management.
/// </summary>
public interface ILabelTemplateService
{
    #region Events

    /// <summary>
    /// Raised when a template is created.
    /// </summary>
    event EventHandler<LabelTemplateDto>? TemplateCreated;

    /// <summary>
    /// Raised when a template is updated.
    /// </summary>
    event EventHandler<LabelTemplateDto>? TemplateUpdated;

    #endregion

    #region Template CRUD

    /// <summary>
    /// Creates a new label template.
    /// </summary>
    Task<LabelTemplateDto> CreateTemplateAsync(CreateLabelTemplateDto dto);

    /// <summary>
    /// Gets a template by ID.
    /// </summary>
    Task<LabelTemplateDto?> GetTemplateAsync(int templateId);

    /// <summary>
    /// Gets all templates for a store.
    /// </summary>
    Task<List<LabelTemplateDto>> GetAllTemplatesAsync(int storeId);

    /// <summary>
    /// Gets templates by label size.
    /// </summary>
    Task<List<LabelTemplateDto>> GetTemplatesBySizeAsync(int labelSizeId, int storeId);

    /// <summary>
    /// Gets promo templates.
    /// </summary>
    Task<List<LabelTemplateDto>> GetPromoTemplatesAsync(int storeId);

    /// <summary>
    /// Updates an existing template.
    /// </summary>
    Task<LabelTemplateDto> UpdateTemplateAsync(int templateId, UpdateLabelTemplateDto dto);

    /// <summary>
    /// Deletes a template (soft delete).
    /// </summary>
    Task<bool> DeleteTemplateAsync(int templateId);

    /// <summary>
    /// Duplicates an existing template.
    /// </summary>
    Task<LabelTemplateDto> DuplicateTemplateAsync(int templateId, string newName);

    #endregion

    #region Template Operations

    /// <summary>
    /// Sets the default template for a store and print language.
    /// </summary>
    Task SetDefaultTemplateAsync(int templateId, int storeId);

    /// <summary>
    /// Gets the default template for a store.
    /// </summary>
    Task<LabelTemplateDto?> GetDefaultTemplateAsync(int storeId, LabelPrintLanguageDto? printLanguage = null);

    /// <summary>
    /// Gets the template assigned to a category.
    /// </summary>
    Task<LabelTemplateDto?> GetTemplateForCategoryAsync(int categoryId, int storeId);

    /// <summary>
    /// Generates a preview of the template with sample data.
    /// </summary>
    Task<LabelPreviewResultDto> GeneratePreviewAsync(LabelPreviewRequestDto request);

    /// <summary>
    /// Validates a template content.
    /// </summary>
    Task<(bool IsValid, List<string> Errors)> ValidateTemplateAsync(string templateContent, LabelPrintLanguageDto language);

    #endregion

    #region Label Generation

    /// <summary>
    /// Generates label content from a template and product data.
    /// </summary>
    Task<string> GenerateLabelContentAsync(int templateId, ProductLabelDataDto data);

    /// <summary>
    /// Generates label content for multiple products.
    /// </summary>
    Task<List<string>> GenerateBatchLabelContentAsync(int templateId, List<ProductLabelDataDto> dataList);

    /// <summary>
    /// Gets available placeholder fields for templates.
    /// </summary>
    List<string> GetAvailablePlaceholders();

    #endregion

    #region Template Library

    /// <summary>
    /// Gets all templates from the library.
    /// </summary>
    Task<List<LabelTemplateLibraryDto>> GetLibraryTemplatesAsync();

    /// <summary>
    /// Gets library templates by category.
    /// </summary>
    Task<List<LabelTemplateLibraryDto>> GetLibraryTemplatesByCategoryAsync(string category);

    /// <summary>
    /// Imports a template from the library into the store.
    /// </summary>
    Task<LabelTemplateDto> ImportFromLibraryAsync(ImportTemplateFromLibraryDto dto);

    #endregion

    #region Template Fields

    /// <summary>
    /// Adds a field to a template.
    /// </summary>
    Task<LabelTemplateFieldDto> AddFieldAsync(int templateId, CreateLabelTemplateFieldDto dto);

    /// <summary>
    /// Updates a template field.
    /// </summary>
    Task<LabelTemplateFieldDto> UpdateFieldAsync(int fieldId, CreateLabelTemplateFieldDto dto);

    /// <summary>
    /// Removes a field from a template.
    /// </summary>
    Task<bool> RemoveFieldAsync(int fieldId);

    /// <summary>
    /// Reorders template fields.
    /// </summary>
    Task ReorderFieldsAsync(int templateId, List<int> fieldIds);

    #endregion
}
