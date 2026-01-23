# feat: Implement Label Template Management UI

**Labels:** `enhancement` `frontend` `printing` `labels` `priority-critical`

## Overview

Create a WPF view (`LabelTemplateManagementView.xaml`) that allows administrators to view, create, edit, duplicate, and delete label templates. The backend service `LabelTemplateService.cs` (830 lines) is fully implemented but there is no UI to manage templates.

## Background

The backend supports:
- Template CRUD operations
- Field management (add, update, remove, reorder)
- 15+ placeholders for dynamic content
- ZPL/EPL/TSPL template validation
- Preview generation with sample data
- Template library management and import
- Template duplication
- Promo template support

But users cannot access any of this functionality without a UI.

## Requirements

### View Components

Create `Views/Settings/LabelTemplateManagementView.xaml`:

#### 1. Template List Section
```
┌─────────────────────────────────────────────────────────────────┐
│ Label Templates                           [+ New] [Import] [↻]  │
├─────────────────────────────────────────────────────────────────┤
│ Filter: [All Sizes ▼]  [All Types ▼]  🔍 [Search...        ]    │
├───────────────────────────────────────────────────────────────── │
│ ┌─ 38 x 25 mm (Standard Shelf) ─────────────────────────────────┐
│ │ ★ Standard Shelf Label      ZPL   v3   Default               │
│ │   Promo Shelf Label         ZPL   v2   Promo                  │
│ │   Clearance Label           ZPL   v1                          │
│ └───────────────────────────────────────────────────────────────┘
│ ┌─ 50 x 30 mm (Price Label) ────────────────────────────────────┐
│ │   Large Price Tag           EPL   v1                          │
│ └───────────────────────────────────────────────────────────────┘
└─────────────────────────────────────────────────────────────────┘
```

- Templates grouped by label size
- Collapsible groups
- Default template marked with star
- Promo templates marked with badge
- Version number displayed
- Search/filter functionality

#### 2. Template Actions Panel
```
┌─ Selected: Standard Shelf Label ────────────────────────────────┐
│                                                                 │
│ [✏️ Edit Design]  [📋 Duplicate]  [📤 Export]  [🗑️ Delete]      │
│                                                                 │
│ ── Details ──                                                   │
│ Size:      38 x 25 mm (304 x 200 dots @ 203 DPI)               │
│ Language:  ZPL (Zebra Programming Language)                     │
│ Version:   3                                                    │
│ Fields:    5 (ProductName, Barcode, Price, UnitPrice, Date)    │
│ Created:   2026-01-15                                           │
│ Updated:   2026-01-20                                           │
│                                                                 │
│ ── Preview ──                                                   │
│ ┌───────────────────────────────────────────────────────────┐  │
│ │ [Preview rendered with sample data]                       │  │
│ └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

#### 3. New Template Dialog
```
┌─ Create New Template ───────────────────────────────────────────┐
│                                                                 │
│ Name:         [                                           ]     │
│ Label Size:   [38 x 25 mm - Standard Shelf              ▼]     │
│ Language:     [ZPL (Zebra)                              ▼]     │
│ Type:         (●) Standard  ( ) Promo  ( ) Clearance           │
│ Description:  [                                           ]     │
│                                                                 │
│ Start From:   (●) Blank  ( ) Library Template  ( ) Copy Existing│
│                                                                 │
│               [Cancel]  [Create & Open Designer]                │
└─────────────────────────────────────────────────────────────────┘
```

#### 4. Import from Library Dialog
```
┌─ Import from Library ───────────────────────────────────────────┐
│                                                                 │
│ Category: [Standard ▼]                                          │
│                                                                 │
│ ┌─ Available Templates ─────────────────────────────────────┐  │
│ │ ☐ Standard Shelf Label (38x25mm, ZPL)                     │  │
│ │ ☐ Standard Shelf Label (50x30mm, EPL)                     │  │
│ │ ☐ Promo Label Large (60x40mm, ZPL)                        │  │
│ │ ☐ Barcode Only (25x25mm, ZPL)                             │  │
│ └───────────────────────────────────────────────────────────┘  │
│                                                                 │
│ Import as:    [                                           ]     │
│ Target Size:  [38 x 25 mm                               ▼]     │
│                                                                 │
│               [Cancel]  [Import Selected]                       │
└─────────────────────────────────────────────────────────────────┘
```

### ViewModel

Create `ViewModels/Settings/LabelTemplateManagementViewModel.cs`:

```csharp
public partial class LabelTemplateManagementViewModel : ObservableObject
{
    private readonly ILabelTemplateService _templateService;
    private readonly ILabelPrinterService _printerService;

    [ObservableProperty] private ObservableCollection<LabelSizeGroup> _templateGroups;
    [ObservableProperty] private LabelTemplateDto? _selectedTemplate;
    [ObservableProperty] private string _searchText;
    [ObservableProperty] private int? _filterSizeId;
    [ObservableProperty] private string? _filterType;

    // Preview
    [ObservableProperty] private string _previewContent;
    [ObservableProperty] private byte[]? _previewImage;

    // Library
    [ObservableProperty] private ObservableCollection<LabelTemplateLibraryDto> _libraryTemplates;

    // Commands
    public IAsyncRelayCommand LoadTemplatesCommand { get; }
    public IAsyncRelayCommand<LabelTemplateDto> OpenDesignerCommand { get; }
    public IAsyncRelayCommand<LabelTemplateDto> DuplicateTemplateCommand { get; }
    public IAsyncRelayCommand<LabelTemplateDto> ExportTemplateCommand { get; }
    public IAsyncRelayCommand<LabelTemplateDto> DeleteTemplateCommand { get; }
    public IAsyncRelayCommand<LabelTemplateDto> SetDefaultCommand { get; }
    public IAsyncRelayCommand OpenNewTemplateDialogCommand { get; }
    public IAsyncRelayCommand OpenLibraryDialogCommand { get; }
    public IAsyncRelayCommand<LabelTemplateDto> GeneratePreviewCommand { get; }
}

public class LabelSizeGroup
{
    public LabelSizeDto Size { get; set; }
    public ObservableCollection<LabelTemplateDto> Templates { get; set; }
    public bool IsExpanded { get; set; } = true;
}
```

### Service Integration

Use existing `ILabelTemplateService` methods:
- `GetAllTemplatesAsync(storeId)` - Load templates
- `CreateTemplateAsync(dto)` - Create template
- `UpdateTemplateAsync(id, dto)` - Update template
- `DeleteTemplateAsync(id)` - Delete template
- `DuplicateTemplateAsync(id, newName)` - Duplicate
- `SetDefaultTemplateAsync(id, storeId)` - Set default
- `GetLibraryTemplatesAsync()` - Get library templates
- `ImportFromLibraryAsync(dto)` - Import from library
- `GeneratePreviewAsync(request)` - Generate preview
- `GetAvailablePlaceholders()` - List placeholders

## Acceptance Criteria

### Functional Requirements
- [ ] View displays all templates for current store
- [ ] Templates grouped by label size with collapsible sections
- [ ] Can search templates by name
- [ ] Can filter by label size
- [ ] Can filter by type (Standard, Promo, Clearance)
- [ ] Default template clearly indicated
- [ ] Promo templates visually distinguished
- [ ] Can create new template from blank, library, or existing
- [ ] Can open template in visual designer (Issue #12)
- [ ] Can duplicate template with new name
- [ ] Can export template to file (Issue #13)
- [ ] Can delete template (with confirmation)
- [ ] Can set template as default
- [ ] Preview shows template with sample data
- [ ] Version history visible

### UI/UX Requirements
- [ ] Consistent with existing admin settings views
- [ ] Templates grouped visually by size
- [ ] Quick actions accessible on hover/selection
- [ ] Loading indicators during async operations
- [ ] Empty state for no templates
- [ ] Confirmation dialog before delete
- [ ] Success/error toasts for operations

### Integration
- [ ] View accessible from Settings menu (under Label Printers)
- [ ] "Edit Design" opens LabelTemplateDesignerView (Issue #12)
- [ ] "Export" triggers export dialog (Issue #13)
- [ ] Navigation with template ID passed to designer

## Technical Notes

### Existing Backend Files
- `Core/Interfaces/ILabelTemplateService.cs` - Service interface (159 lines)
- `Infrastructure/Services/LabelTemplateService.cs` - Full implementation (830 lines)
- `Core/Entities/LabelPrintingEntities.cs` - LabelTemplate, LabelTemplateField (lines 187-261)
- `Core/DTOs/LabelPrintingDtos.cs` - Template DTOs

### Available Placeholders (from LabelTemplateService.cs:23-40)
```
{{ProductName}}, {{ProductNameLine1}}, {{ProductNameLine2}}
{{Barcode}}, {{Price}}, {{UnitPrice}}, {{OriginalPrice}}
{{Description}}, {{SKU}}, {{CategoryName}}
{{PromoText}}, {{UnitOfMeasure}}, {{EffectiveDate}}
{{CurrentDate}}, {{CurrentTime}}
```

### Library Templates (from LabelTemplateLibrary entity)
Pre-built templates organized by:
- Category: Standard, Promo, Clearance
- Size: Various dimensions
- Language: ZPL, EPL, TSPL

## UI Mockup

```
┌─────────────────────────────────────────────────────────────────────────┐
│ Settings > Label Templates                                         [X] │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│ ┌─ Templates ──────────────────────── [+ New] [📥 Import] [↻ Refresh] ─┐
│ │                                                                      │
│ │ Filter: [All Sizes ▼]  [All Types ▼]   🔍 [Search templates...   ]  │
│ │                                                                      │
│ │ ▼ 38 x 25 mm - Standard Shelf (3 templates)                         │
│ │   ┌──────────────────────────────────────────────────────────────┐  │
│ │   │ ★ Standard Shelf Label                                       │  │
│ │   │   ZPL • Version 3 • Updated Jan 20, 2026                     │  │
│ │   │   [Edit] [Duplicate] [Export] [Delete]                       │  │
│ │   ├──────────────────────────────────────────────────────────────┤  │
│ │   │ 🏷️ Promo Shelf Label                                         │  │
│ │   │   ZPL • Version 2 • Promo Template                           │  │
│ │   │   [Edit] [Duplicate] [Export] [Delete]                       │  │
│ │   ├──────────────────────────────────────────────────────────────┤  │
│ │   │   Clearance Label                                            │  │
│ │   │   ZPL • Version 1                                            │  │
│ │   │   [Edit] [Duplicate] [Export] [Delete]                       │  │
│ │   └──────────────────────────────────────────────────────────────┘  │
│ │                                                                      │
│ │ ▶ 50 x 30 mm - Price Label (1 template)                             │
│ │                                                                      │
│ │ ▶ 25 x 25 mm - Small Barcode (0 templates)                          │
│ │                                                                      │
│ └──────────────────────────────────────────────────────────────────────┘
│                                                                         │
│ ┌─ Preview: Standard Shelf Label ───────────────────────────────────────┐
│ │                                                                       │
│ │  ┌───────────────────────────────────────────────────────────────┐   │
│ │  │                                                               │   │
│ │  │  Coca Cola 500ml                                              │   │
│ │  │                                                               │   │
│ │  │  ║║║║║║║║║║║║║║║║║║║║║║║║║║║║                                 │   │
│ │  │       5901234123457                                           │   │
│ │  │                                                               │   │
│ │  │  KSh 199.99              KSh 0.40/ml                          │   │
│ │  │                                                               │   │
│ │  └───────────────────────────────────────────────────────────────┘   │
│ │                                                                       │
│ │  Size: 38 x 25 mm (304 x 200 dots @ 203 DPI)                         │
│ │  Language: ZPL (Zebra Programming Language)                          │
│ │  Fields: ProductName, Barcode, Price, UnitPrice                      │
│ │                                                                       │
│ └───────────────────────────────────────────────────────────────────────┘
└─────────────────────────────────────────────────────────────────────────┘
```

## Test Cases

1. **Load Templates** - Templates load and group by size
2. **Search** - Filter templates by name
3. **Filter by Size** - Show only templates for selected size
4. **Filter by Type** - Show only Promo or Standard
5. **Create Blank** - Opens designer with empty template
6. **Create from Library** - Import and customize
7. **Duplicate** - Creates copy with new name
8. **Delete** - Removes template after confirmation
9. **Set Default** - Updates default, removes from previous
10. **Preview** - Shows template with sample data

## Dependencies

- Issue #010: Label Printer Configuration UI

## Blocks

- Issue #012: Visual Template Designer (Edit Design button)
- Issue #013: Template Export/Import (Export button)

## Estimated Complexity

**Medium** - CRUD UI with grouping, filtering, and preview functionality
