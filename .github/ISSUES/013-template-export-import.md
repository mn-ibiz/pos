# feat: Implement Template Export/Import for Portability

**Labels:** `enhancement` `backend` `frontend` `printing` `labels` `priority-high`

## Overview

Implement template export and import functionality that allows users to save label templates to files (e.g., flash drive) and import them on another computer. This enables template sharing between stores and backup of template designs.

## Business Value

- **Multi-Location Consistency**: Share templates across all stores
- **Template Backup**: Export templates before system updates
- **Easy Migration**: Move templates when setting up new locations
- **Collaboration**: Share custom templates with other businesses
- **Disaster Recovery**: Restore templates from backup files

## Requirements

### Export Format Specification

Create `.lbt` (Label Template) file format using JSON:

```json
{
  "$schema": "https://pos-system.com/schemas/label-template-v1.json",
  "formatVersion": "1.0",
  "metadata": {
    "exportedAt": "2026-01-24T10:30:00Z",
    "exportedBy": "admin@store.com",
    "sourceStoreId": 1,
    "sourceTemplateName": "Standard Shelf Label",
    "checksum": "sha256:abc123..."
  },
  "template": {
    "name": "Standard Shelf Label 38x25",
    "description": "Standard shelf label for retail products",
    "printLanguage": "ZPL",
    "isPromoTemplate": false,
    "version": 3,
    "labelSize": {
      "name": "38x25mm Standard",
      "widthMm": 38,
      "heightMm": 25,
      "dotsPerMm": 8
    },
    "templateContent": "^XA\n^LH0,0\n^LL200\n...",
    "fields": [
      {
        "fieldName": "ProductName",
        "fieldType": "Text",
        "positionX": 20,
        "positionY": 10,
        "width": 290,
        "height": 50,
        "fontName": "0",
        "fontSize": 25,
        "alignment": "Left",
        "isBold": false,
        "rotation": 0,
        "displayOrder": 1
      },
      {
        "fieldName": "Barcode",
        "fieldType": "Barcode",
        "positionX": 20,
        "positionY": 70,
        "width": 290,
        "height": 60,
        "barcodeType": "EAN13",
        "barcodeHeight": 50,
        "showBarcodeText": true,
        "displayOrder": 2
      }
    ]
  }
}
```

### Service Implementation

Add to `ILabelTemplateService`:

```csharp
public interface ILabelTemplateService
{
    // ... existing methods ...

    // Single template export/import
    Task<byte[]> ExportTemplateAsync(int templateId, ExportOptions? options = null);
    Task<LabelTemplateDto> ImportTemplateAsync(byte[] templateData, ImportOptions options);

    // Batch export/import
    Task<byte[]> ExportAllTemplatesAsync(int storeId, ExportOptions? options = null);
    Task<List<ImportResult>> ImportTemplatePackageAsync(byte[] packageData, ImportOptions options);

    // Validation
    Task<ValidationResult> ValidateImportFileAsync(byte[] templateData);
}

public class ExportOptions
{
    public bool IncludeLabelSize { get; set; } = true;
    public bool IncludeFields { get; set; } = true;
    public bool IncludeMetadata { get; set; } = true;
    public bool Minify { get; set; } = false;
}

public class ImportOptions
{
    public int StoreId { get; set; }
    public string? NewName { get; set; }  // Rename on import
    public int? TargetLabelSizeId { get; set; }  // Override label size
    public ConflictResolution OnConflict { get; set; } = ConflictResolution.Rename;
    public bool ValidateOnly { get; set; } = false;  // Dry run
}

public enum ConflictResolution
{
    Rename,     // Add (2), (3) suffix
    Replace,    // Overwrite existing
    Skip,       // Don't import
    Fail        // Throw exception
}

public class ImportResult
{
    public string OriginalName { get; set; }
    public string ImportedName { get; set; }
    public int? TemplateId { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Warnings { get; set; }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public string FormatVersion { get; set; }
    public string TemplateName { get; set; }
    public string PrintLanguage { get; set; }
    public decimal WidthMm { get; set; }
    public decimal HeightMm { get; set; }
    public int FieldCount { get; set; }
    public List<string> Errors { get; set; }
    public List<string> Warnings { get; set; }
    public bool SizeCompatible { get; set; }  // Matches existing size?
}
```

### Export Dialog

```
┌─ Export Template ───────────────────────────────────────────────┐
│                                                                 │
│ Template: Standard Shelf Label (38x25mm)                        │
│                                                                 │
│ ── Export Options ──                                            │
│ ☑ Include label size configuration                              │
│ ☑ Include field definitions                                     │
│ ☑ Include metadata (date, author)                               │
│ ☐ Minify output (smaller file, not human-readable)              │
│                                                                 │
│ Save to:                                                        │
│ [D:\Templates\shelf-label-38x25.lbt         ] [Browse...]       │
│                                                                 │
│                              [Cancel]  [Export]                 │
└─────────────────────────────────────────────────────────────────┘
```

### Import Dialog

```
┌─ Import Template ───────────────────────────────────────────────┐
│                                                                 │
│ File: [D:\Templates\shelf-label-38x25.lbt   ] [Browse...]       │
│                                                                 │
│ ── File Information ──                                          │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ Name:        Standard Shelf Label                           │ │
│ │ Language:    ZPL (Zebra)                                    │ │
│ │ Size:        38 x 25 mm                                     │ │
│ │ Fields:      5 (ProductName, Barcode, Price, UnitPrice,     │ │
│ │              Date)                                          │ │
│ │ Exported:    2026-01-20 by admin@store.com                  │ │
│ │ Source:      Store #1 - Main Branch                         │ │
│ │                                                             │ │
│ │ ⚠️ Warning: Label size "38x25mm" exists. Template will use  │ │
│ │    existing size configuration.                             │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│ ── Import Options ──                                            │
│ Import as:  [Standard Shelf Label (copy)               ]        │
│ Label Size: [38 x 25 mm - Standard (existing)        ▼]         │
│                                                                 │
│ If name exists:                                                 │
│ (●) Rename automatically (add suffix)                           │
│ ( ) Replace existing template                                   │
│ ( ) Skip (don't import)                                         │
│                                                                 │
│             [Validate Only]  [Cancel]  [Import]                 │
└─────────────────────────────────────────────────────────────────┘
```

### Batch Export Dialog

```
┌─ Export All Templates ──────────────────────────────────────────┐
│                                                                 │
│ Export all templates from current store to a single package.   │
│                                                                 │
│ Templates to export: 8                                          │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ ☑ Standard Shelf Label (38x25mm)                            │ │
│ │ ☑ Promo Shelf Label (38x25mm)                               │ │
│ │ ☑ Clearance Label (38x25mm)                                 │ │
│ │ ☑ Large Price Tag (50x30mm)                                 │ │
│ │ ☑ Small Barcode (25x25mm)                                   │ │
│ │ ☑ Promo Banner (60x40mm)                                    │ │
│ │ ☐ Test Template (do not export)                             │ │
│ │ ☑ Seasonal Label (50x25mm)                                  │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│ Save to:                                                        │
│ [D:\Templates\all-templates-2026-01-24.lbtpkg ] [Browse...]     │
│                                                                 │
│                              [Cancel]  [Export Package]         │
└─────────────────────────────────────────────────────────────────┘
```

### ViewModels

```csharp
public partial class ExportTemplateViewModel : ObservableObject
{
    [ObservableProperty] private LabelTemplateDto _template;
    [ObservableProperty] private string _filePath;
    [ObservableProperty] private bool _includeLabelSize = true;
    [ObservableProperty] private bool _includeFields = true;
    [ObservableProperty] private bool _includeMetadata = true;
    [ObservableProperty] private bool _minify = false;

    public IAsyncRelayCommand ExportCommand { get; }
    public IRelayCommand BrowseCommand { get; }
}

public partial class ImportTemplateViewModel : ObservableObject
{
    [ObservableProperty] private string _filePath;
    [ObservableProperty] private ValidationResult? _validationResult;
    [ObservableProperty] private string _importName;
    [ObservableProperty] private int? _targetLabelSizeId;
    [ObservableProperty] private ConflictResolution _conflictResolution;
    [ObservableProperty] private bool _isValidating;

    public IAsyncRelayCommand<string> LoadFileCommand { get; }
    public IAsyncRelayCommand ValidateCommand { get; }
    public IAsyncRelayCommand ImportCommand { get; }
    public IRelayCommand BrowseCommand { get; }
}
```

## Acceptance Criteria

### Export Functionality
- [ ] Can export single template to .lbt file
- [ ] Export includes all template content (ZPL/EPL code)
- [ ] Export includes field definitions
- [ ] Export includes label size information
- [ ] Export includes metadata (date, author)
- [ ] Can choose export location via file dialog
- [ ] Default filename includes template name
- [ ] Checksum calculated and included for integrity verification

### Import Functionality
- [ ] Can import single template from .lbt file
- [ ] File validation before import (format, version, required fields)
- [ ] Preview shows template information before import
- [ ] Warnings shown for size incompatibility
- [ ] Can rename template during import
- [ ] Can select target label size
- [ ] Conflict resolution options (rename, replace, skip)
- [ ] "Validate Only" option for dry run
- [ ] Import creates new template in database
- [ ] Import links to existing or creates new label size

### Batch Operations
- [ ] Can export multiple templates to .lbtpkg package
- [ ] Can select which templates to include
- [ ] Package file is a ZIP containing multiple .lbt files
- [ ] Can import .lbtpkg package
- [ ] Individual import results shown for each template

### Validation
- [ ] Invalid JSON rejected with clear error
- [ ] Missing required fields detected
- [ ] Unknown fields tolerated (forward compatibility)
- [ ] Version mismatch warnings (but allows import)
- [ ] Corrupted checksum detected

### File Format
- [ ] .lbt extension for single template
- [ ] .lbtpkg extension for package (ZIP format)
- [ ] UTF-8 encoding
- [ ] Human-readable by default
- [ ] Minified option reduces file size

## Technical Notes

### Implementation in LabelTemplateService

```csharp
public async Task<byte[]> ExportTemplateAsync(int templateId, ExportOptions? options = null)
{
    options ??= new ExportOptions();

    var template = await GetTemplateAsync(templateId);
    if (template == null)
        throw new KeyNotFoundException($"Template {templateId} not found");

    var size = await _sizeRepository.GetByIdAsync(template.LabelSizeId);

    var exportData = new LabelTemplateExport
    {
        FormatVersion = "1.0",
        Metadata = options.IncludeMetadata ? new ExportMetadata
        {
            ExportedAt = DateTime.UtcNow,
            ExportedBy = _currentUserService.GetCurrentUserEmail(),
            SourceStoreId = template.StoreId,
            SourceTemplateName = template.Name
        } : null,
        Template = new TemplateData
        {
            Name = template.Name,
            Description = template.Description,
            PrintLanguage = template.PrintLanguage.ToString(),
            IsPromoTemplate = template.IsPromoTemplate,
            Version = template.Version,
            TemplateContent = template.TemplateContent,
            LabelSize = options.IncludeLabelSize ? new LabelSizeData
            {
                Name = size?.Name,
                WidthMm = size?.WidthMm ?? 0,
                HeightMm = size?.HeightMm ?? 0,
                DotsPerMm = size?.DotsPerMm ?? 8
            } : null,
            Fields = options.IncludeFields ? template.Fields : null
        }
    };

    var jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = !options.Minify,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    var json = JsonSerializer.Serialize(exportData, jsonOptions);

    // Add checksum to metadata
    if (options.IncludeMetadata && exportData.Metadata != null)
    {
        var checksum = ComputeChecksum(exportData.Template);
        exportData.Metadata.Checksum = $"sha256:{checksum}";
        json = JsonSerializer.Serialize(exportData, jsonOptions);
    }

    return Encoding.UTF8.GetBytes(json);
}
```

### File Association (Optional)
- Register .lbt file type with Windows
- Double-click opens import dialog

### Database Changes

```sql
-- Track export/import history
ALTER TABLE LabelTemplates ADD ExportCount INT DEFAULT 0;
ALTER TABLE LabelTemplates ADD LastExportedAt DATETIME NULL;
ALTER TABLE LabelTemplates ADD ImportedFromFile NVARCHAR(500) NULL;
ALTER TABLE LabelTemplates ADD ImportedAt DATETIME NULL;
ALTER TABLE LabelTemplates ADD OriginalChecksum NVARCHAR(100) NULL;
```

## Test Cases

1. **Export Single** - Export template, verify JSON structure
2. **Export with Options** - Test each option toggle
3. **Import Valid File** - Import and verify template created
4. **Import Invalid JSON** - Verify error message
5. **Import Missing Fields** - Verify validation catches
6. **Import Different DPI** - Verify size adjustment/warning
7. **Import Name Conflict - Rename** - Verify suffix added
8. **Import Name Conflict - Replace** - Verify replaced
9. **Import Name Conflict - Skip** - Verify not imported
10. **Batch Export** - Export 5 templates to package
11. **Batch Import** - Import package, verify all templates
12. **Validate Only** - Dry run with no changes
13. **Checksum Verification** - Detect corrupted file

## Dependencies

- Issue #011: Label Template Management UI (export/import buttons)

## Blocks

- None (enhances existing functionality)

## Estimated Complexity

**Medium** - JSON serialization, file I/O, validation logic
