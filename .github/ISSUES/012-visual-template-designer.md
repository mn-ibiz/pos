# feat: Implement Visual WYSIWYG Template Designer

**Labels:** `enhancement` `frontend` `printing` `labels` `priority-critical`

## Overview

Create a visual WYSIWYG (What You See Is What You Get) label template designer that allows users to design labels by dragging and dropping elements onto a canvas, without needing to write ZPL/EPL code manually. This is the most critical gap in the current implementation.

## Background

Currently, users must manually write ZPL/EPL template code like:
```zpl
^XA
^FO20,20^A0N,30,30^FD{{ProductName}}^FS
^FO20,60^BY2,2,50^BCN,50,Y,N,N^FD{{Barcode}}^FS
^FO20,130^A0N,40,40^FDKSh {{Price}}^FS
^XZ
```

This requires technical knowledge and makes label design inaccessible to most users. A visual designer will:
- Allow drag-and-drop element placement
- Show real-time preview
- Automatically generate ZPL/EPL code
- Make label design accessible to non-technical users

## Requirements

### View Components

Create `Views/Settings/LabelTemplateDesignerView.xaml`:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ Label Template Designer - Standard Shelf Label                         [X] │
├────────────────┬────────────────────────────────────────┬───────────────────┤
│ Toolbox        │ Design Canvas (38mm x 25mm @ 203 DPI)  │ Properties        │
├────────────────┼────────────────────────────────────────┼───────────────────┤
│                │                                        │                   │
│ ┌────────────┐ │  ┌────────────────────────────────┐    │ Selected: Text    │
│ │ Aa  Text   │ │  │                                │    │ ─────────────────│
│ │ ║║║ Barcode│ │  │  [ProductName] ◊               │    │                   │
│ │ $  Price   │ │  │                                │    │ Content:          │
│ │ ⊞  QR Code │ │  │  ║║║║║║║║║║║║║║║║║║║║║║        │    │ [{{ProductName}}] │
│ │ ▣  Image   │ │  │     5901234123457               │    │                   │
│ │ □  Box     │ │  │                                │    │ Position:         │
│ │ ─  Line    │ │  │  KSh 199.99         KSh/kg     │    │ X: [20  ] Y: [20] │
│ └────────────┘ │  │                                │    │ W: [290 ] H: [30] │
│                │  └────────────────────────────────┘    │                   │
│ Placeholders:  │                                        │ Font: [0     ▼]   │
│ ─────────────  │                                        │ Size: [24    ▼]   │
│ {{ProductName}}│  [Grid ☑] [Snap ☑] Zoom: [100% ▼]     │ Bold: [☐]         │
│ {{Barcode}}    │                                        │ Align: [Left  ▼]  │
│ {{Price}}      │                                        │ Rotate: [0°   ▼]  │
│ {{UnitPrice}}  │                                        │                   │
│ {{SKU}}        │                                        │ [Apply Changes]   │
│ {{Category}}   │                                        │                   │
│ ...more...     │                                        │                   │
│                ├────────────────────────────────────────┼───────────────────┤
│                │ Preview                                │ Template Info     │
│                │ ┌────────────────────────────────┐    │ ─────────────────│
│                │ │ Coca Cola 500ml                │    │ Name: Standard... │
│                │ │ ║║║║║║║║║║║║║║║║║║║║║║         │    │ Size: 38x25mm     │
│                │ │     5901234123457               │    │ DPI: 203          │
│                │ │ KSh 199.99         KSh 0.40/ml │    │ Language: ZPL     │
│                │ └────────────────────────────────┘    │ Version: 3        │
├────────────────┴────────────────────────────────────────┴───────────────────┤
│ [📜 View Code]  [⬇️ Import]  [⬆️ Export]  [💾 Save]  [❌ Cancel]            │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Design Canvas

Create custom WPF control `Controls/LabelDesignCanvas.cs`:

```csharp
public class LabelDesignCanvas : Canvas
{
    // Properties
    public double LabelWidthMm { get; set; }
    public double LabelHeightMm { get; set; }
    public int DotsPerMm { get; set; } = 8; // 203 DPI

    public bool ShowGrid { get; set; } = true;
    public bool SnapToGrid { get; set; } = true;
    public int GridSizeDots { get; set; } = 10;

    public double ZoomLevel { get; set; } = 1.0;

    // Selected element
    public LabelElement? SelectedElement { get; set; }

    // Elements collection
    public ObservableCollection<LabelElement> Elements { get; }

    // Events
    public event EventHandler<LabelElement> ElementSelected;
    public event EventHandler<LabelElement> ElementMoved;
    public event EventHandler<LabelElement> ElementResized;
}
```

### Label Element Control

Create `Controls/LabelElementControl.cs`:

```csharp
public class LabelElement : ContentControl
{
    public LabelFieldTypeDto ElementType { get; set; }
    public string FieldName { get; set; }
    public string Content { get; set; } // Placeholder or static text

    // Position (in dots)
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    // Text properties
    public string FontName { get; set; }
    public int FontSize { get; set; }
    public TextAlignmentDto Alignment { get; set; }
    public bool IsBold { get; set; }
    public int Rotation { get; set; } // 0, 90, 180, 270

    // Barcode properties (if type is Barcode)
    public BarcodeTypeDto? BarcodeType { get; set; }
    public int? BarcodeHeight { get; set; }
    public bool? ShowBarcodeText { get; set; }

    // Interaction
    public bool IsSelected { get; set; }
    public bool IsDragging { get; set; }
    public bool IsResizing { get; set; }
}
```

### Element Types

| Type | Icon | Properties | Visual Representation |
|------|------|------------|----------------------|
| Text | Aa | Content, Font, Size, Bold, Align, Rotate | Text with placeholder preview |
| Barcode | ║║║ | BarcodeType, Height, ShowText | Barcode symbol |
| Price | $ | Content ({{Price}}), Font, Size, Currency | Formatted price |
| QRCode | ⊞ | Size, ErrorCorrection | QR placeholder |
| Image | ▣ | Source, Width, Height | Image placeholder |
| Box | □ | Width, Height, Thickness | Rectangle outline |
| Line | ─ | Length, Thickness, Orientation | Line |

### ZPL/EPL Code Generator

Create `Services/LabelCodeGeneratorService.cs`:

```csharp
public interface ILabelCodeGeneratorService
{
    string GenerateZpl(LabelTemplate template, List<LabelElement> elements);
    string GenerateEpl(LabelTemplate template, List<LabelElement> elements);
    string GenerateTspl(LabelTemplate template, List<LabelElement> elements);
    List<LabelElement> ParseZpl(string zplCode);
    List<LabelElement> ParseEpl(string eplCode);
}

public class LabelCodeGeneratorService : ILabelCodeGeneratorService
{
    public string GenerateZpl(LabelTemplate template, List<LabelElement> elements)
    {
        var sb = new StringBuilder();
        sb.AppendLine("^XA");
        sb.AppendLine("^LH0,0");
        sb.AppendLine($"^LL{template.LabelSize.HeightMm * template.LabelSize.DotsPerMm}");

        foreach (var element in elements.OrderBy(e => e.DisplayOrder))
        {
            sb.AppendLine(GenerateZplElement(element));
        }

        sb.AppendLine("^XZ");
        return sb.ToString();
    }

    private string GenerateZplElement(LabelElement element)
    {
        return element.ElementType switch
        {
            LabelFieldTypeDto.Text => GenerateZplText(element),
            LabelFieldTypeDto.Barcode => GenerateZplBarcode(element),
            LabelFieldTypeDto.Price => GenerateZplText(element), // Price is text with formatting
            LabelFieldTypeDto.QRCode => GenerateZplQrCode(element),
            LabelFieldTypeDto.Box => GenerateZplBox(element),
            LabelFieldTypeDto.Line => GenerateZplLine(element),
            _ => ""
        };
    }

    private string GenerateZplText(LabelElement e)
    {
        // ^FO{x},{y}^A{font}N,{height},{width}^FD{content}^FS
        return $"^FO{e.PositionX},{e.PositionY}^A{e.FontName}N,{e.FontSize},{e.FontSize}^FD{e.Content}^FS";
    }

    private string GenerateZplBarcode(LabelElement e)
    {
        // ^FO{x},{y}^BY{moduleWidth},{ratio},{height}^BC{orientation},{height},{printLine},{printAbove},{checkDigit}^FD{content}^FS
        var showText = e.ShowBarcodeText == true ? "Y" : "N";
        return $"^FO{e.PositionX},{e.PositionY}^BY2,2,{e.BarcodeHeight}^BCN,{e.BarcodeHeight},{showText},N,N^FD{e.Content}^FS";
    }
}
```

### ViewModel

Create `ViewModels/Settings/LabelTemplateDesignerViewModel.cs`:

```csharp
public partial class LabelTemplateDesignerViewModel : ObservableObject
{
    private readonly ILabelTemplateService _templateService;
    private readonly ILabelCodeGeneratorService _codeGenerator;

    // Template being edited
    [ObservableProperty] private int? _templateId;
    [ObservableProperty] private string _templateName;
    [ObservableProperty] private LabelSizeDto _labelSize;
    [ObservableProperty] private LabelPrintLanguageDto _printLanguage;

    // Canvas state
    [ObservableProperty] private ObservableCollection<LabelElement> _elements;
    [ObservableProperty] private LabelElement? _selectedElement;
    [ObservableProperty] private bool _showGrid = true;
    [ObservableProperty] private bool _snapToGrid = true;
    [ObservableProperty] private double _zoomLevel = 1.0;

    // Generated code
    [ObservableProperty] private string _generatedCode;
    [ObservableProperty] private bool _isCodeViewVisible;

    // Preview
    [ObservableProperty] private string _previewContent;
    [ObservableProperty] private byte[]? _previewImage;

    // Placeholders
    public List<string> AvailablePlaceholders => _templateService.GetAvailablePlaceholders();

    // Commands
    public IRelayCommand<LabelFieldTypeDto> AddElementCommand { get; }
    public IRelayCommand DeleteSelectedCommand { get; }
    public IRelayCommand DuplicateSelectedCommand { get; }
    public IRelayCommand<LabelElement> SelectElementCommand { get; }
    public IRelayCommand MoveUpCommand { get; }
    public IRelayCommand MoveDownCommand { get; }
    public IRelayCommand ToggleCodeViewCommand { get; }
    public IAsyncRelayCommand GeneratePreviewCommand { get; }
    public IAsyncRelayCommand SaveTemplateCommand { get; }
    public IAsyncRelayCommand CancelCommand { get; }

    // Element manipulation
    public void OnElementDragged(LabelElement element, int newX, int newY);
    public void OnElementResized(LabelElement element, int newWidth, int newHeight);
}
```

## Acceptance Criteria

### Canvas & Interaction
- [ ] Canvas displays label at correct aspect ratio (scaled to fit)
- [ ] Canvas shows grid overlay (toggleable)
- [ ] Elements snap to grid when dragging (toggleable)
- [ ] Zoom control (50%, 75%, 100%, 150%, 200%)
- [ ] Elements can be selected by clicking
- [ ] Selected element shows resize handles
- [ ] Elements can be dragged to new position
- [ ] Elements can be resized via handles
- [ ] Multi-select with Ctrl+Click
- [ ] Delete key removes selected element
- [ ] Ctrl+D duplicates selected element

### Toolbox & Elements
- [ ] All element types available: Text, Barcode, Price, QRCode, Image, Box, Line
- [ ] Drag from toolbox to canvas creates element
- [ ] Double-click toolbox item adds to center
- [ ] Elements render with representative visuals
- [ ] Barcode elements show barcode placeholder graphic
- [ ] Text elements show content with placeholder markers

### Properties Panel
- [ ] Panel updates when element selected
- [ ] Position X/Y editable (in dots)
- [ ] Width/Height editable
- [ ] Font dropdown for text elements
- [ ] Font size dropdown
- [ ] Bold checkbox
- [ ] Alignment dropdown (Left, Center, Right)
- [ ] Rotation dropdown (0°, 90°, 180°, 270°)
- [ ] Barcode type dropdown for barcode elements
- [ ] Barcode height field
- [ ] Show barcode text checkbox
- [ ] Changes apply immediately to element

### Placeholders Panel
- [ ] Lists all 15+ available placeholders
- [ ] Click placeholder inserts into selected text element
- [ ] Drag placeholder onto canvas creates text element
- [ ] Tooltip shows placeholder description

### Code View
- [ ] "View Code" toggles code panel
- [ ] Shows generated ZPL/EPL/TSPL code
- [ ] Code updates in real-time as elements change
- [ ] Code is read-only (generated, not editable)
- [ ] Syntax highlighting (optional)

### Preview
- [ ] Preview panel shows label with sample data
- [ ] Placeholders replaced with sample values
- [ ] Updates when elements change
- [ ] Shows approximate print output

### Save & Load
- [ ] Save generates ZPL/EPL from elements
- [ ] Save updates template.TemplateContent
- [ ] Save updates template.Fields collection
- [ ] Load parses existing template content
- [ ] Load populates elements from Fields
- [ ] Unsaved changes warning on close

## Technical Notes

### Coordinate System
- All positions in **dots** (not mm or pixels)
- 203 DPI = 8 dots per mm
- 300 DPI = ~12 dots per mm
- Canvas scales dots to screen pixels based on zoom

### Element Rendering
- Text: Use WPF TextBlock with font mapping
- Barcode: Use placeholder image or generate with library
- Box: Rectangle with stroke, no fill
- Line: Line element

### ZPL Font Mapping
| ZPL Font | WPF Equivalent | Notes |
|----------|----------------|-------|
| 0 | Default | Scalable |
| A-Z | Fixed | Various sizes |

### Existing Files to Reference
- `LabelTemplateService.cs` - GenerateLabelContentAsync for placeholder replacement
- `LabelPrintingEntities.cs` - LabelTemplateField for field structure
- Stories `28-2-label-template-management.md` - Sample ZPL templates

## Test Cases

1. **Add Text Element** - Drag text to canvas, verify position
2. **Add Barcode** - Create barcode, set type to EAN-13
3. **Move Element** - Drag element, verify new coordinates
4. **Resize Element** - Use handles, verify new dimensions
5. **Snap to Grid** - Enable snap, drag element
6. **Insert Placeholder** - Click {{Price}}, verify in text element
7. **Change Font** - Select text, change font size
8. **Rotate Element** - Set rotation to 90°
9. **Generate ZPL** - Verify correct ZPL output
10. **Save Template** - Save and reload, verify elements restored
11. **Preview** - Check preview shows sample data

## UI Mockup (Detailed Canvas)

```
┌─ Design Canvas ─────────────────────────────────────────────────────┐
│   10  20  30  40  50  60  70  80  90 100 110 120 130 140 150       │
│ ┌────────────────────────────────────────────────────────────────┐ │
│ │                                                                │10│
│ │  ┌─────────────────────────────────────────────────────────┐  │20│
│ │  │ {{ProductName}}                                     ◊  │  │30│
│ │  └─────────────────────────────────────────────────────────┘  │40│
│ │                                                                │50│
│ │  ┌─────────────────────────────────────────────────────────┐  │60│
│ │  │ ║ ║║ ║ ║║║ ║║ ║ ║║ ║ ║║║ ║║ ║                       ◊  │  │70│
│ │  │           {{Barcode}}                                   │  │80│
│ │  └─────────────────────────────────────────────────────────┘  │90│
│ │                                                                │100
│ │  ┌──────────────────────┐  ┌──────────────────────────┐       │110
│ │  │ KSh {{Price}}    ◊  │  │ {{UnitPrice}}        ◊  │       │120
│ │  └──────────────────────┘  └──────────────────────────┘       │130
│ │                                                                │140
│ └────────────────────────────────────────────────────────────────┘ │
│                                                                     │
│ [☑ Grid] [☑ Snap] Zoom: [100% ▼]  Elements: 4  Selected: Price     │
└─────────────────────────────────────────────────────────────────────┘

◊ = Resize handle (shown on selected element)
```

## Dependencies

- Issue #011: Label Template Management UI (navigation to designer)

## Blocks

- Issue #015: Visual Label Preview Rendering (enhanced preview)

## Estimated Complexity

**High** - Custom WPF controls, drag-and-drop, code generation, coordinate management
