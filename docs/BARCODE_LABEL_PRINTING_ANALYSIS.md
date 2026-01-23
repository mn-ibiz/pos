# Barcode & Shelf Label Printing Feature Analysis

## Executive Summary

This document provides a comprehensive analysis of barcode printing and shelf label printing capabilities in the POS system. The analysis covers current implementation status, gaps identified, industry best practices (including Microsoft RMS), and recommendations for world-class label printing functionality.

---

## 1. Industry Research: How Leading Systems Handle Label Printing

### 1.1 Microsoft RMS (Retail Management System) Approach

Based on research, Microsoft RMS Store Operations handles barcode and label printing as follows:

| Feature | Microsoft RMS Implementation |
|---------|------------------------------|
| **Template Format** | `.lbl` files containing ZPL (Zebra Programming Language) commands |
| **Template Variables** | `<<StoreName>>`, `<<ItemLookupCode>>`, `<<Price>>`, `<<Description>>` |
| **Printer Support** | Zebra thermal printers (ZPL/EPL), Dymo label printers |
| **Design Tool** | Built-in Label Designer utility in Store Operations |
| **Template Portability** | Templates stored as files, copyable via file system |
| **Printer Selection** | Driver-based selection in Windows printer settings |

**Key ZPL Commands Used by Microsoft RMS:**
- `^XA` / `^XZ` - Label start/end
- `^CF` - Font settings
- `^LH` - Label home position
- `^LL` - Label length
- `^FO` - Field origin (positioning)
- `^FD` - Field data
- `^BC` - Barcode generation

### 1.2 Industry Best Practices

| Best Practice | Description |
|---------------|-------------|
| **WYSIWYG Designer** | Visual drag-and-drop label design (ZebraDesigner, BarTender, NiceLabel) |
| **Multiple Print Languages** | Support ZPL, EPL, TSPL for different printer brands |
| **Template Portability** | Export templates as XML/JSON for transfer between systems |
| **Real-time Preview** | Render label preview before printing (via API or local rendering) |
| **Batch Printing** | Print labels for entire categories, price changes, new products |
| **Variable Data Printing** | Merge product data with template placeholders |
| **Printer Auto-Detection** | Discover available printers on network/USB |
| **Label Size Profiles** | Pre-configured sizes (25x25mm, 1"x1", etc.) |

### 1.3 Recommended Label Sizes for Retail

| Size (mm) | Size (inches) | Use Case |
|-----------|---------------|----------|
| 25 x 25 | 1" x 1" | Small shelf labels |
| 38 x 25 | 1.5" x 1" | Standard shelf labels |
| 50 x 25 | 2" x 1" | Wide shelf labels |
| 50 x 30 | 2" x 1.2" | Price + barcode labels |
| 60 x 40 | 2.4" x 1.6" | Promotional labels |
| 100 x 50 | 4" x 2" | Large format labels |

---

## 2. Current Implementation Analysis

### 2.1 Implemented Features (Backend - Comprehensive)

The POS system has **extensive backend infrastructure** for label printing:

#### 2.1.1 Core Entities (`LabelPrintingEntities.cs`)

| Entity | Purpose | Status |
|--------|---------|--------|
| `LabelPrinter` | Printer configuration (Serial/Network/USB/Windows) | **Implemented** |
| `LabelSize` | Label dimensions (mm) and DPI | **Implemented** |
| `LabelTemplate` | Template with ZPL/EPL content | **Implemented** |
| `LabelTemplateField` | Field positioning, fonts, barcode settings | **Implemented** |
| `LabelTemplateLibrary` | Built-in template storage | **Implemented** |
| `CategoryPrinterAssignment` | Category-to-printer routing | **Implemented** |
| `LabelPrintJob` | Print job tracking | **Implemented** |
| `LabelPrintJobItem` | Individual label tracking | **Implemented** |

#### 2.1.2 Printer Support

| Connection Type | Protocol | Status |
|-----------------|----------|--------|
| Serial (COM) | Direct | **Implemented** |
| Network (TCP/IP) | Raw socket | **Implemented** |
| USB | Windows spooler | **Implemented** |
| Windows Driver | System printer | **Implemented** |

| Print Language | Status | Notes |
|----------------|--------|-------|
| ZPL (Zebra) | **Implemented** | Full support with validation |
| EPL (Eltron) | **Implemented** | Full support with validation |
| TSPL (TSC) | **Implemented** | Full support with validation |
| Raw/ESC-POS | **Implemented** | Basic support |

#### 2.1.3 Services

| Service | Key Methods | Status |
|---------|-------------|--------|
| `ILabelPrinterService` | Printer CRUD, connection testing, status monitoring | **Implemented** |
| `ILabelTemplateService` | Template CRUD, validation, placeholder replacement, preview | **Implemented** |
| `ILabelPrintService` | Single/batch printing, price change printing, job tracking | **Implemented** |
| `IBarcodeService` | Barcode generation, validation, weighted barcode parsing | **Implemented** |

#### 2.1.4 Available Placeholders (15+)

```
{{ProductName}}       {{ProductNameLine1}}   {{ProductNameLine2}}
{{Barcode}}           {{Price}}              {{UnitPrice}}
{{OriginalPrice}}     {{Description}}        {{SKU}}
{{CategoryName}}      {{PromoText}}          {{UnitOfMeasure}}
{{EffectiveDate}}     {{CurrentDate}}        {{CurrentTime}}
```

#### 2.1.5 Batch Operations

| Operation | Description | Status |
|-----------|-------------|--------|
| Single Label Print | Print one product label | **Implemented** |
| Batch Print | Print multiple products | **Implemented** |
| Price Change Labels | Automatic detection & printing | **Implemented** |
| Category Labels | Print all products in category | **Implemented** |
| New Product Labels | Print products added since date | **Implemented** |
| Custom Label Print | Direct template + data printing | **Implemented** |

### 2.2 Summary of Backend Implementation

```
Files Implemented:
├── src/HospitalityPOS.Core/
│   ├── Entities/LabelPrintingEntities.cs (391 lines)
│   ├── DTOs/LabelPrintingDtos.cs (467 lines)
│   ├── Interfaces/ILabelPrinterService.cs (144 lines)
│   ├── Interfaces/ILabelTemplateService.cs (159 lines)
│   └── Interfaces/ILabelPrintService.cs (154 lines)
├── src/HospitalityPOS.Infrastructure/Services/
│   ├── LabelPrinterService.cs (815 lines)
│   ├── LabelTemplateService.cs (830 lines)
│   ├── LabelPrintService.cs (150+ lines)
│   └── BarcodeService.cs (657 lines)
└── src/HospitalityPOS.WPF/Views/
    └── BarcodeSettingsView.xaml (337 lines) - Barcode config only
```

---

## 3. Identified Gaps

### 3.1 Critical Gaps (Must Have)

| Gap | Description | Impact | Priority |
|-----|-------------|--------|----------|
| **No Visual Template Designer UI** | Users must manually write ZPL/EPL code to create templates | Users cannot design labels without technical knowledge | **P1** |
| **No Template Export/Import** | Cannot transfer templates between computers via flash drive | Templates locked to single installation | **P1** |
| **No Label Printer Configuration UI** | No WPF view for managing label printers | Cannot add/configure printers from UI | **P1** |
| **No Template Management UI** | No WPF view for creating/editing templates | Templates only manageable via code/database | **P1** |
| **No Visual Preview Rendering** | `GeneratePreviewAsync` returns text, not rendered image | Cannot see actual label appearance | **P2** |

### 3.2 Important Gaps (Should Have)

| Gap | Description | Impact | Priority |
|-----|-------------|--------|----------|
| **No Label Size Management UI** | Cannot add custom label sizes from UI | Limited to pre-configured sizes | **P2** |
| **No Template Library Browser UI** | Cannot browse/import built-in templates from UI | Library templates inaccessible to users | **P2** |
| **No Printer Status Dashboard** | Real-time printer status not visible | Cannot monitor printer health | **P2** |
| **No Print Queue UI** | Cannot view/manage pending print jobs | Limited visibility into print operations | **P2** |
| **No Printer Test Page UI** | Cannot send test prints from UI | Difficult to verify printer configuration | **P2** |

### 3.3 Nice-to-Have Gaps

| Gap | Description | Priority |
|-----|-------------|----------|
| **No Labelary API Integration** | External preview rendering service | **P3** |
| **No Direct Printer Discovery** | Must manually enter connection details | **P3** |
| **No Template Versioning UI** | Version history not visible | **P3** |
| **No Template Sharing Hub** | No centralized template repository | **P3** |

---

## 4. Recommendations for World-Class Implementation

### 4.1 Phase 1: Essential UI Components (Immediate Priority)

#### 4.1.1 Label Printer Configuration View

Create `LabelPrinterConfigurationView.xaml` with:
- List of configured printers with status indicators
- Add/Edit/Delete printer functionality
- Connection type selection (Serial/Network/USB/Windows)
- Connection string builder (port selector, IP address, etc.)
- Test connection button
- Set as default printer option
- Print test label button

#### 4.1.2 Label Template Management View

Create `LabelTemplateManagementView.xaml` with:
- List of templates grouped by label size
- Template CRUD operations
- Template duplication
- Link to visual designer
- Import from library button
- Export to file button

#### 4.1.3 Visual Template Designer View

Create `LabelTemplateDesignerView.xaml` with:
- **Canvas Area**: WYSIWYG design surface scaled to label size
- **Toolbox**: Drag-and-drop elements (Text, Barcode, Price, QR Code, Image, Box, Line)
- **Properties Panel**: Edit selected element properties
- **Placeholders Panel**: List of available placeholders
- **Preview Panel**: Real-time preview with sample data
- **ZPL/EPL Code View**: Show/edit raw template code

**Designer Element Properties:**
| Element Type | Properties |
|--------------|------------|
| Text | Content, Font, Size, Bold, Alignment, Rotation, Position |
| Barcode | Type (EAN-13, Code-128, etc.), Height, Show Text, Position |
| Price | Format, Font, Size, Currency Symbol, Position |
| QR Code | Size, Error Correction, Position |
| Image | Source, Width, Height, Position |
| Box/Line | Width, Height, Thickness, Position |

#### 4.1.4 Label Size Configuration View

Create `LabelSizeConfigurationView.xaml` with:
- Standard sizes pre-populated (25x25, 38x25, 50x25, etc.)
- Add custom sizes
- Set DPI (203, 300, 600 dpi options)
- Preview actual size dimensions

### 4.2 Phase 2: Template Portability (Essential for Multi-Location)

#### 4.2.1 Template Export/Import System

**Export Format (JSON):**
```json
{
  "formatVersion": "1.0",
  "exportedAt": "2026-01-24T10:00:00Z",
  "template": {
    "name": "Standard Shelf Label 38x25",
    "printLanguage": "ZPL",
    "labelSize": { "widthMm": 38, "heightMm": 25, "dpi": 203 },
    "templateContent": "^XA^FO20,20...",
    "fields": [
      {
        "fieldName": "ProductName",
        "fieldType": "Text",
        "positionX": 20,
        "positionY": 20,
        "width": 140,
        "height": 30,
        "fontName": "0",
        "fontSize": 24,
        "alignment": "Left",
        "isBold": false,
        "rotation": 0
      }
    ]
  }
}
```

**Implementation:**
- `ExportTemplateAsync(templateId, filePath)` - Export to .lbt (Label Template) file
- `ImportTemplateAsync(filePath, storeId)` - Import from file
- Validate label size compatibility on import
- Option to adjust template for different DPI

#### 4.2.2 Batch Export/Import

- Export all templates for a store
- Import template package
- Conflict resolution (rename, replace, skip)

### 4.3 Phase 3: Visual Preview Rendering

#### 4.3.1 Option A: Local Rendering with SkiaSharp

Use SkiaSharp to render ZPL-like graphics locally:
```csharp
public async Task<byte[]> RenderPreviewAsync(LabelTemplate template, ProductLabelDataDto data)
{
    var content = GenerateLabelContent(template, data);
    using var surface = SKSurface.Create(new SKImageInfo(widthPx, heightPx));
    var canvas = surface.Canvas;
    canvas.Clear(SKColors.White);

    // Parse and render each element
    foreach (var field in template.Fields)
    {
        RenderField(canvas, field, data);
    }

    return surface.Snapshot().Encode(SKEncodedImageFormat.Png, 100).ToArray();
}
```

#### 4.3.2 Option B: Labelary API Integration

Use free Labelary API for ZPL rendering:
```csharp
public async Task<byte[]> RenderZplPreviewAsync(string zplContent, int widthDots, int heightDots)
{
    using var client = new HttpClient();
    var response = await client.PostAsync(
        $"http://api.labelary.com/v1/printers/8dpmm/labels/{widthDots}x{heightDots}/0/",
        new StringContent(zplContent)
    );
    return await response.Content.ReadAsByteArrayAsync();
}
```

### 4.4 Phase 4: Advanced Features

#### 4.4.1 Printer Auto-Discovery

- Scan for network printers (port 9100)
- Enumerate COM ports
- List Windows printers

#### 4.4.2 Print Queue Dashboard

- Real-time job status
- Retry failed prints
- Cancel pending jobs
- Print history with statistics

#### 4.4.3 Category-Specific Printing Rules

- Assign default printer per category
- Assign default template per category
- Automatic routing based on product category

---

## 5. UI Mockups

### 5.1 Label Printer Configuration View

```
┌─────────────────────────────────────────────────────────────────────┐
│ Label Printer Configuration                                    [X] │
├─────────────────────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────────────────────┐ │
│ │ Printers                                    [+ Add] [Refresh]  │ │
│ ├───────────────────────┬─────────┬───────────┬──────────────────┤ │
│ │ Name                  │ Type    │ Language  │ Status           │ │
│ ├───────────────────────┼─────────┼───────────┼──────────────────┤ │
│ │ ● Shelf Printer 1     │ Network │ ZPL       │ ● Online         │ │
│ │   Barcode Printer     │ USB     │ EPL       │ ● Offline        │ │
│ │   Promo Label Printer │ Serial  │ TSPL      │ ● Online         │ │
│ └───────────────────────┴─────────┴───────────┴──────────────────┘ │
│                                                                     │
│ ┌─ Selected Printer: Shelf Printer 1 ─────────────────────────────┐ │
│ │ Name:        [Shelf Printer 1          ]                        │ │
│ │ Type:        [Network          ▼]                               │ │
│ │ Language:    [ZPL (Zebra)      ▼]                               │ │
│ │ IP Address:  [192.168.1.100    ] Port: [9100]                   │ │
│ │ Label Size:  [38 x 25 mm       ▼]                               │ │
│ │ ☑ Set as Default                                                │ │
│ │                                                                 │ │
│ │ [Test Connection]  [Print Test Label]  [Save]  [Delete]         │ │
│ └─────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────┘
```

### 5.2 Label Template Designer View

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ Label Template Designer - Standard Shelf Label                         [X]  │
├────────────────┬────────────────────────────────────────┬───────────────────┤
│ Toolbox        │ Design Canvas (38mm x 25mm @ 203 DPI)  │ Properties        │
├────────────────┤                                        ├───────────────────┤
│ ┌────────────┐ │  ┌────────────────────────────────┐    │ Selected: Text    │
│ │ Aa  Text   │ │  │ [ProductName..............] ◊ │    │ ─────────────────│
│ │ ||| Barcode│ │  │                                │    │ Content:          │
│ │ $  Price   │ │  │  ║║║║║║║║║║║║║║║║║║║║║║        │    │ {{ProductName}}   │
│ │ ⊞  QR Code │ │  │     5901234123457               │    │                   │
│ │ ▣  Image   │ │  │                                │    │ Position:         │
│ │ □  Box     │ │  │ KSh 199.99         KSh/kg     │    │ X: [20] Y: [20]   │
│ │ ─  Line    │ │  └────────────────────────────────┘    │                   │
│ └────────────┘ │                                        │ Font: [0    ▼]    │
│                │                                        │ Size: [24   ▼]    │
│ Placeholders:  │  Zoom: [100% ▼]   [Grid] [Snap]       │ Bold: [☐]        │
│ ─────────────  │                                        │ Align: [Left ▼]  │
│ {{ProductName}}│                                        │                   │
│ {{Barcode}}    │                                        │ [Apply]           │
│ {{Price}}      ├────────────────────────────────────────┼───────────────────┤
│ {{UnitPrice}}  │ Preview (Sample Data)                  │ Template Info     │
│ {{SKU}}        │ ┌────────────────────────────────┐    │ ─────────────────│
│ {{Category}}   │ │ Coca Cola 500ml                │    │ Name: Standard... │
│ ...            │ │ ║║║║║║║║║║║║║║║║║║║║║║         │    │ Size: 38x25mm     │
│                │ │     5901234123457               │    │ DPI: 203          │
│                │ │ KSh 199.99         KSh 0.40/ml │    │ Language: ZPL     │
│                │ └────────────────────────────────┘    │ Version: 3        │
├────────────────┴────────────────────────────────────────┴───────────────────┤
│ [View ZPL Code]  [Import Template]  [Export Template]  [Save]  [Cancel]     │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 5.3 Template Import/Export Dialog

```
┌─────────────────────────────────────────────────────────────────┐
│ Export Template                                            [X]  │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│ Template: Standard Shelf Label 38x25                            │
│                                                                 │
│ Export Options:                                                 │
│ ☑ Include label size configuration                              │
│ ☑ Include field definitions                                     │
│ ☐ Include printer assignments                                   │
│                                                                 │
│ File Format: [JSON (.lbt)        ▼]                             │
│                                                                 │
│ Save to: [D:\Templates\shelf-label-38x25.lbt    ] [Browse...]   │
│                                                                 │
│                              [Export]  [Cancel]                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 6. Technical Implementation Plan

### 6.1 New Files to Create

| File | Purpose | Priority |
|------|---------|----------|
| `Views/LabelPrinterConfigurationView.xaml` | Printer management UI | P1 |
| `ViewModels/LabelPrinterConfigurationViewModel.cs` | Printer VM | P1 |
| `Views/LabelTemplateManagementView.xaml` | Template list UI | P1 |
| `ViewModels/LabelTemplateManagementViewModel.cs` | Template list VM | P1 |
| `Views/LabelTemplateDesignerView.xaml` | Visual designer UI | P1 |
| `ViewModels/LabelTemplateDesignerViewModel.cs` | Designer VM | P1 |
| `Views/LabelSizeConfigurationView.xaml` | Label size UI | P2 |
| `ViewModels/LabelSizeConfigurationViewModel.cs` | Label size VM | P2 |
| `Services/LabelTemplateExportService.cs` | Export/Import logic | P1 |
| `Services/LabelPreviewRenderService.cs` | Visual rendering | P2 |
| `Controls/LabelDesignCanvas.cs` | Custom design canvas | P1 |
| `Controls/LabelElementControl.cs` | Draggable elements | P1 |

### 6.2 Service Interface Extensions

```csharp
// Add to ILabelTemplateService
Task<byte[]> ExportTemplateAsync(int templateId, string format = "json");
Task<LabelTemplateDto> ImportTemplateAsync(byte[] templateData, int storeId, string newName = null);
Task<byte[]> ExportAllTemplatesAsync(int storeId);
Task<List<LabelTemplateDto>> ImportTemplatePackageAsync(byte[] packageData, int storeId);

// New ILabelPreviewService
public interface ILabelPreviewService
{
    Task<byte[]> RenderPreviewAsync(int templateId, ProductLabelDataDto data);
    Task<byte[]> RenderPreviewAsync(string templateContent, LabelPrintLanguageDto language,
                                     decimal widthMm, decimal heightMm, int dpi,
                                     ProductLabelDataDto data);
    Task<byte[]> RenderZplViaLabelaryAsync(string zplContent, int widthDots, int heightDots);
}
```

### 6.3 Database Additions

```sql
-- Template export tracking
ALTER TABLE LabelTemplates ADD ExportCount INT DEFAULT 0;
ALTER TABLE LabelTemplates ADD LastExportedAt DATETIME NULL;

-- Template import tracking
ALTER TABLE LabelTemplates ADD ImportedFromFile NVARCHAR(500) NULL;
ALTER TABLE LabelTemplates ADD ImportedAt DATETIME NULL;
ALTER TABLE LabelTemplates ADD OriginalTemplateId INT NULL;
```

---

## 7. Comparison: Current vs. Target State

| Feature | Current State | Target State | Gap Level |
|---------|---------------|--------------|-----------|
| Printer Configuration | Backend only | Full UI + Backend | **Critical** |
| Template Design | Manual ZPL/EPL | Visual WYSIWYG Designer | **Critical** |
| Template Portability | None | Export/Import JSON/XML | **Critical** |
| Visual Preview | Text only | Rendered image | **High** |
| Label Size Management | Backend only | Full UI | **Medium** |
| Template Library | Backend only | Browsable UI | **Medium** |
| Printer Status | Backend events | Dashboard UI | **Medium** |
| Print Queue | Backend tracking | Visual queue | **Low** |
| Printer Discovery | Manual entry | Auto-scan | **Low** |

---

## 8. Implementation Roadmap

### Sprint 1: Essential UI (Week 1-2)
- [ ] LabelPrinterConfigurationView + ViewModel
- [ ] LabelTemplateManagementView + ViewModel
- [ ] Label Size Configuration UI
- [ ] Integration with existing services

### Sprint 2: Template Designer (Week 3-4)
- [ ] LabelDesignCanvas custom control
- [ ] Drag-and-drop element placement
- [ ] Properties panel for element editing
- [ ] Placeholder insertion
- [ ] ZPL/EPL code generation from visual design

### Sprint 3: Template Portability (Week 5)
- [ ] Export template to JSON file
- [ ] Import template from file
- [ ] Batch export/import
- [ ] Size compatibility validation

### Sprint 4: Preview & Polish (Week 6)
- [ ] Visual preview rendering (SkiaSharp or Labelary)
- [ ] Print queue dashboard
- [ ] Printer status monitoring
- [ ] Final testing and refinements

---

## 9. Conclusion

The POS system has a **comprehensive backend implementation** for barcode and shelf label printing, including:
- Full support for ZPL, EPL, TSPL thermal printers
- Multi-connection support (Serial, Network, USB, Windows)
- Template management with placeholders
- Batch printing capabilities
- Job tracking and statistics

**Critical gaps exist in the UI layer:**
1. No visual template designer
2. No template export/import for portability
3. No printer/template management UI

Implementing the recommended UI components and portability features will bring the system to a world-class level, matching or exceeding Microsoft RMS capabilities while being more accessible to non-technical users through the visual designer.

---

## Appendix A: Sample ZPL Templates

### Standard Shelf Label (38x25mm)
```zpl
^XA
^LH0,0
^LL200
^FO20,10^A0N,25,25^FB290,2,,^FD{{ProductNameLine1}}^FS
^FO20,35^A0N,25,25^FB290,2,,^FD{{ProductNameLine2}}^FS
^FO20,70^BY2,2,50^BCN,50,Y,N,N^FD{{Barcode}}^FS
^FO20,140^A0N,40,40^FDKSh {{Price}}^FS
^FO200,150^A0N,20,20^FD{{UnitPrice}}^FS
^XZ
```

### Promotional Label (50x30mm)
```zpl
^XA
^LH0,0
^LL240
^FO10,5^GB380,30,30^FS
^FO20,8^A0N,25,25^FR^FD{{PromoText}}^FS
^FO10,40^A0N,22,22^FD{{ProductName}}^FS
^FO10,70^BY2,2,45^BCN,45,Y,N,N^FD{{Barcode}}^FS
^FO10,140^A0N,35,35^FDNOW KSh {{Price}}^FS
^FO10,180^A0N,18,18^SO1^FDWas KSh {{OriginalPrice}}^FS
^XZ
```

### Small Barcode Label (25x25mm)
```zpl
^XA
^LH0,0
^LL200
^FO10,10^A0N,18,18^FD{{ProductName}}^FS
^FO10,35^BY1.5,2,40^BCN,40,Y,N,N^FD{{Barcode}}^FS
^FO10,95^A0N,30,30^FDKSh {{Price}}^FS
^XZ
```

---

## Appendix B: Template Export Format Specification

```json
{
  "$schema": "https://pos-system.com/schemas/label-template-v1.json",
  "formatVersion": "1.0",
  "metadata": {
    "exportedAt": "2026-01-24T10:30:00Z",
    "exportedBy": "admin@store.com",
    "sourceStoreId": 1,
    "sourceTemplateName": "Standard Shelf Label"
  },
  "template": {
    "name": "Standard Shelf Label 38x25",
    "description": "Standard shelf label for retail products",
    "printLanguage": "ZPL",
    "isPromoTemplate": false,
    "labelSize": {
      "name": "38x25mm",
      "widthMm": 38,
      "heightMm": 25,
      "dotsPerMm": 8
    },
    "templateContent": "^XA^LH0,0...",
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
        "rotation": 0
      },
      {
        "fieldName": "Barcode",
        "fieldType": "Barcode",
        "positionX": 20,
        "positionY": 70,
        "width": 290,
        "height": 50,
        "barcodeType": "EAN13",
        "barcodeHeight": 50,
        "showBarcodeText": true
      }
    ]
  }
}
```
