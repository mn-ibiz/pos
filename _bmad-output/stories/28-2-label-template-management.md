# Story 28.2: Label Template Management

## Story
**As an** administrator,
**I want to** design and manage label templates,
**So that** labels match business requirements.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Epic
**Epic 28: Shelf Label Printing**

## Acceptance Criteria

### AC1: Template Creation
**Given** label design needed
**When** creating template
**Then** can specify: product name, barcode, price, description positioning

### AC2: Template Preview
**Given** template exists
**When** previewing
**Then** shows label preview with sample data

### AC3: Size Linking
**Given** different label sizes
**When** managing templates
**Then** templates linked to specific label sizes

## Technical Notes
```csharp
public class LabelTemplate
{
    public Guid Id { get; set; }
    public string Name { get; set; }  // "Standard Shelf Label", "Promo Label"
    public Guid LabelSizeId { get; set; }
    public LabelSize LabelSize { get; set; }
    public LabelPrintLanguage PrintLanguage { get; set; }
    public string TemplateContent { get; set; }  // ZPL/EPL template with placeholders
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class LabelTemplateField
{
    public Guid Id { get; set; }
    public Guid TemplateId { get; set; }
    public string FieldName { get; set; }  // "ProductName", "Price", "Barcode"
    public LabelFieldType FieldType { get; set; }
    public int PositionX { get; set; }  // Dots from left
    public int PositionY { get; set; }  // Dots from top
    public int Width { get; set; }
    public int Height { get; set; }
    public string FontName { get; set; }
    public int FontSize { get; set; }
    public TextAlignment Alignment { get; set; }
    public bool IsBold { get; set; }
}

public enum LabelFieldType
{
    Text,
    Barcode,
    Price,
    Date,
    QRCode,
    Image
}

public enum TextAlignment
{
    Left,
    Center,
    Right
}

public interface ILabelTemplateService
{
    Task<LabelTemplate> CreateTemplateAsync(LabelTemplateDto template);
    Task<LabelTemplate> UpdateTemplateAsync(Guid id, LabelTemplateDto template);
    Task<bool> DeleteTemplateAsync(Guid id);
    Task<List<LabelTemplate>> GetAllTemplatesAsync();
    Task<List<LabelTemplate>> GetTemplatesBySizeAsync(Guid labelSizeId);
    Task<byte[]> GeneratePreviewAsync(Guid templateId, PreviewDataDto sampleData);
    Task SetDefaultTemplateAsync(Guid templateId);
}

public class LabelGenerator
{
    public string GenerateLabel(LabelTemplate template, ProductLabelData data)
    {
        var content = template.TemplateContent;

        // Replace placeholders with actual data
        content = content.Replace("{{ProductName}}", data.ProductName);
        content = content.Replace("{{Barcode}}", data.Barcode);
        content = content.Replace("{{Price}}", data.Price.ToString("N2"));
        content = content.Replace("{{UnitPrice}}", data.UnitPrice);
        content = content.Replace("{{Description}}", data.Description);
        content = content.Replace("{{SKU}}", data.SKU);
        content = content.Replace("{{Category}}", data.CategoryName);
        content = content.Replace("{{Date}}", DateTime.Now.ToString("dd/MM/yyyy"));

        return content;
    }
}

public class ProductLabelData
{
    public string ProductName { get; set; }
    public string Barcode { get; set; }
    public decimal Price { get; set; }
    public string UnitPrice { get; set; }  // "KSh 50.00/kg"
    public string Description { get; set; }
    public string SKU { get; set; }
    public string CategoryName { get; set; }
    public decimal? OriginalPrice { get; set; }  // For promo labels
    public string PromoText { get; set; }
}

// Sample ZPL Template
public static class ZplTemplates
{
    public const string StandardShelfLabel = @"
^XA
^FO20,20^A0N,30,30^FD{{ProductName}}^FS
^FO20,60^BY2,2,50^BCN,50,Y,N,N^FD{{Barcode}}^FS
^FO20,130^A0N,40,40^FDKSH {{Price}}^FS
^FO20,180^A0N,20,20^FD{{UnitPrice}}^FS
^XZ";

    public const string PromoLabel = @"
^XA
^FO20,10^GB360,25,25^FS
^FO30,10^A0N,25,25^FR^FDSPECIAL OFFER^FS
^FO20,45^A0N,25,25^FD{{ProductName}}^FS
^FO20,80^BY2,2,40^BCN,40,Y,N,N^FD{{Barcode}}^FS
^FO20,140^A0N,35,35^FDKSH {{Price}}^FS
^FO180,140^A0N,20,20^FD(was {{OriginalPrice}})^FS
^XZ";
}
```

## Definition of Done
- [x] LabelTemplate entity and database table
- [x] Field positioning and styling
- [x] Placeholder replacement system
- [x] ZPL/EPL template generation
- [x] Preview image generation
- [x] Standard template library
- [x] Promo label template support
- [x] Template-to-size association
- [x] Unit tests passing

## Implementation Summary
**Completed:** January 2026

### Files Created/Modified:
- `src/HospitalityPOS.Core/Entities/LabelPrintingEntities.cs` - LabelTemplate, LabelTemplateField, LabelTemplateLibrary entities
- `src/HospitalityPOS.Core/DTOs/LabelPrintingDtos.cs` - Template DTOs with field definitions
- `src/HospitalityPOS.Core/Interfaces/ILabelTemplateService.cs` - Service interface with events
- `src/HospitalityPOS.Infrastructure/Services/LabelTemplateService.cs` - Full implementation with:
  - Template CRUD operations
  - Field management (add, update, remove, reorder)
  - Placeholder replacement system with 15+ available placeholders
  - ZPL/EPL/TSPL template validation
  - Preview generation with sample data
  - Template library management and import
  - Template duplication
  - Promo template support with original price display
- `tests/HospitalityPOS.Business.Tests/Services/LabelTemplateServiceTests.cs` - Comprehensive unit tests
