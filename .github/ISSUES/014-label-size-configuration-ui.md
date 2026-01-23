# feat: Implement Label Size Configuration UI

**Labels:** `enhancement` `frontend` `printing` `labels` `priority-medium`

## Overview

Create a WPF view that allows administrators to manage label sizes (dimensions and DPI settings) used for label templates. The backend entity `LabelSize` exists but there is no UI to manage sizes.

## Background

Different label printers and label stocks require different size configurations:
- **25 x 25 mm** - Small barcode-only labels
- **38 x 25 mm** - Standard shelf labels
- **50 x 30 mm** - Price tags with description
- **60 x 40 mm** - Promotional labels
- **100 x 50 mm** - Large format labels

Users need to:
1. View pre-configured standard sizes
2. Add custom sizes for non-standard label stock
3. Set DPI for accurate positioning
4. See which templates use each size

## Requirements

### View Components

Create `Views/Settings/LabelSizeConfigurationView.xaml`:

```
┌─────────────────────────────────────────────────────────────────────┐
│ Label Sizes                                                    [X] │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│ ┌─ Standard Sizes ──────────────────────────────────────────────── │
│ │                                                                  │
│ │ ┌───────────────────────────────────────────────────────────┐   │
│ │ │ 25 x 25 mm (1" x 1")           203 DPI    2 templates     │   │
│ │ │ Small barcode label                                   [✎] │   │
│ │ ├───────────────────────────────────────────────────────────┤   │
│ │ │ 38 x 25 mm (1.5" x 1")         203 DPI    5 templates  ★  │   │
│ │ │ Standard shelf label                                  [✎] │   │
│ │ ├───────────────────────────────────────────────────────────┤   │
│ │ │ 50 x 30 mm (2" x 1.2")         203 DPI    3 templates     │   │
│ │ │ Price tag with barcode                                [✎] │   │
│ │ ├───────────────────────────────────────────────────────────┤   │
│ │ │ 60 x 40 mm (2.4" x 1.6")       300 DPI    1 template      │   │
│ │ │ Promotional label                                     [✎] │   │
│ │ └───────────────────────────────────────────────────────────┘   │
│ └──────────────────────────────────────────────────────────────────│
│                                                                     │
│ ┌─ Custom Sizes ───────────────────────────── [+ Add Custom Size] ─│
│ │                                                                  │
│ │ ┌───────────────────────────────────────────────────────────┐   │
│ │ │ 45 x 20 mm (custom)            203 DPI    1 template      │   │
│ │ │ Narrow shelf strip                             [✎] [🗑️]  │   │
│ │ └───────────────────────────────────────────────────────────┘   │
│ │                                                                  │
│ │ (No custom sizes - click "Add Custom Size" to create one)       │
│ └──────────────────────────────────────────────────────────────────│
│                                                                     │
│ ┌─ Size Calculator ────────────────────────────────────────────────│
│ │                                                                  │
│ │ Dimensions:  [38  ] mm  x  [25  ] mm    DPI: [203  ▼]           │
│ │              [1.50] in  x  [0.98] in                             │
│ │                                                                  │
│ │ Resolution:  304 dots  x  200 dots                               │
│ │ Print Area:  60,800 total dots                                   │
│ │                                                                  │
│ └──────────────────────────────────────────────────────────────────│
└─────────────────────────────────────────────────────────────────────┘
```

### Add/Edit Size Dialog

```
┌─ Add Custom Label Size ─────────────────────────────────────────────┐
│                                                                     │
│ Name:         [                                               ]     │
│ Description:  [                                               ]     │
│                                                                     │
│ ── Dimensions ──                                                    │
│                                                                     │
│ Width:   [      ] mm   =  [      ] inches                          │
│ Height:  [      ] mm   =  [      ] inches                          │
│                                                                     │
│ ── Printer Resolution ──                                            │
│                                                                     │
│ DPI: (●) 203 DPI (8 dots/mm) - Standard thermal                    │
│      ( ) 300 DPI (12 dots/mm) - High resolution                    │
│      ( ) 600 DPI (24 dots/mm) - Ultra high resolution              │
│      ( ) Custom: [    ] dots/mm                                    │
│                                                                     │
│ ── Calculated Values ──                                             │
│                                                                     │
│ Width in dots:   [304     ] (auto-calculated)                      │
│ Height in dots:  [200     ] (auto-calculated)                      │
│ Total area:      60,800 dots                                       │
│                                                                     │
│ ── Size Preview ──                                                  │
│                                                                     │
│ ┌─────────────────────────────────────┐                            │
│ │                                     │  ← Proportional preview    │
│ │        38 x 25 mm                   │                            │
│ │                                     │                            │
│ └─────────────────────────────────────┘                            │
│                                                                     │
│                           [Cancel]  [Save Size]                     │
└─────────────────────────────────────────────────────────────────────┘
```

### ViewModel

```csharp
public partial class LabelSizeConfigurationViewModel : ObservableObject
{
    private readonly ILabelPrinterService _printerService;
    private readonly ILabelTemplateService _templateService;

    // Size collections
    [ObservableProperty] private ObservableCollection<LabelSizeDto> _standardSizes;
    [ObservableProperty] private ObservableCollection<LabelSizeDto> _customSizes;
    [ObservableProperty] private LabelSizeDto? _selectedSize;

    // Template counts per size
    [ObservableProperty] private Dictionary<int, int> _templateCounts;

    // Calculator
    [ObservableProperty] private decimal _calculatorWidthMm;
    [ObservableProperty] private decimal _calculatorHeightMm;
    [ObservableProperty] private int _calculatorDpi = 203;
    [ObservableProperty] private int _calculatedWidthDots;
    [ObservableProperty] private int _calculatedHeightDots;

    // Commands
    public IAsyncRelayCommand LoadSizesCommand { get; }
    public IRelayCommand OpenAddDialogCommand { get; }
    public IAsyncRelayCommand<LabelSizeDto> EditSizeCommand { get; }
    public IAsyncRelayCommand<LabelSizeDto> DeleteSizeCommand { get; }
    public IRelayCommand CalculateDotsCommand { get; }

    // Calculated properties
    public decimal CalculatorWidthInches => CalculatorWidthMm / 25.4m;
    public decimal CalculatorHeightInches => CalculatorHeightMm / 25.4m;
}
```

### Service Integration

Add to `ILabelPrinterService` (or create `ILabelSizeService`):

```csharp
// Label size methods (some may already exist)
Task<List<LabelSizeDto>> GetAllLabelSizesAsync();
Task<LabelSizeDto> CreateLabelSizeAsync(CreateLabelSizeDto dto);
Task<LabelSizeDto> UpdateLabelSizeAsync(int id, UpdateLabelSizeDto dto);
Task<bool> DeleteLabelSizeAsync(int id);
Task<int> GetTemplateCountForSizeAsync(int sizeId);
```

## Acceptance Criteria

### Functional Requirements
- [ ] View displays all label sizes (standard and custom)
- [ ] Standard sizes are pre-populated (built-in)
- [ ] Custom sizes can be added by user
- [ ] Custom sizes can be edited
- [ ] Custom sizes can be deleted (if no templates use them)
- [ ] Cannot delete size with linked templates (show count)
- [ ] Template count shown for each size
- [ ] Size calculator auto-calculates dots from mm and DPI

### UI/UX Requirements
- [ ] Standard vs custom sizes visually separated
- [ ] Default size indicated with star
- [ ] Inches shown alongside mm for convenience
- [ ] DPI options with common presets
- [ ] Proportional size preview in dialog
- [ ] Delete confirmation with template count warning
- [ ] Real-time calculation as dimensions change

### Validation
- [ ] Width and height must be positive
- [ ] Name is required and unique
- [ ] Reasonable limits (1mm - 300mm)
- [ ] DPI must be positive integer

## Technical Notes

### Standard Sizes (Pre-seed)

| Name | Width | Height | DPI | Dots/mm | Description |
|------|-------|--------|-----|---------|-------------|
| Small Barcode | 25mm | 25mm | 203 | 8 | 1" x 1" barcode only |
| Standard Shelf | 38mm | 25mm | 203 | 8 | 1.5" x 1" standard |
| Wide Shelf | 50mm | 25mm | 203 | 8 | 2" x 1" with description |
| Price Tag | 50mm | 30mm | 203 | 8 | 2" x 1.2" |
| Promo Label | 60mm | 40mm | 203 | 8 | 2.4" x 1.6" |
| Large Format | 100mm | 50mm | 203 | 8 | 4" x 2" |

### Dots Calculation

```csharp
public int CalculateDotsFromMm(decimal mm, int dpi)
{
    // 1 inch = 25.4 mm
    // dots = mm * (dpi / 25.4)
    return (int)Math.Round(mm * dpi / 25.4m);
}

public int DotsPerMm(int dpi)
{
    // Approximate dots per mm
    return dpi switch
    {
        203 => 8,
        300 => 12,
        600 => 24,
        _ => (int)Math.Round(dpi / 25.4m)
    };
}
```

### Existing Entity

From `LabelPrintingEntities.cs:110-127`:
```csharp
public class LabelSize : BaseEntity
{
    public string Name { get; set; }
    public decimal WidthMm { get; set; }
    public decimal HeightMm { get; set; }
    public int DotsPerMm { get; set; } = 8;
    public string? Description { get; set; }
    public virtual ICollection<LabelTemplate> Templates { get; set; }
}
```

## Test Cases

1. **Load Sizes** - Standard and custom sizes display
2. **Add Custom Size** - Create 45x20mm size
3. **Edit Size** - Change description
4. **Delete Empty Size** - Delete size with 0 templates
5. **Delete Used Size** - Block deletion, show template count
6. **Calculator** - Enter 38x25mm @ 203 DPI, verify 304x200 dots
7. **Inch Conversion** - Verify mm to inches accurate
8. **DPI Change** - Same mm different DPI = different dots
9. **Validation** - Reject negative dimensions
10. **Unique Name** - Reject duplicate size name

## Dependencies

- Issue #010: Label Printer Configuration UI (parent settings area)

## Blocks

- None

## Estimated Complexity

**Low-Medium** - Standard CRUD with calculations
