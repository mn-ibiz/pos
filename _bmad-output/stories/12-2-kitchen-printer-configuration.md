# Story 12.2: Kitchen Printer Configuration

Status: done

## Story

As an administrator,
I want to configure kitchen printer(s),
So that orders route to the correct preparation stations.

## Acceptance Criteria

1. **Given** kitchen printer(s) are connected
   **When** configuring kitchen printing
   **Then** admin can add multiple kitchen printers

2. **Given** printer-category mapping
   **When** configuring routing
   **Then** each printer can be assigned to product categories

3. **Given** KOT formatting
   **When** configuring format
   **Then** KOT format can be configured (font size, item grouping)

4. **Given** printer verification
   **When** testing configuration
   **Then** test print should be available for each printer

## Tasks / Subtasks

- [ ] Task 1: Create Kitchen Printer Entity
  - [ ] Extend Printer entity for kitchen
  - [ ] Create PrinterCategoryMapping entity
  - [ ] Create KOTSettings entity
  - [ ] Configure EF Core mappings

- [ ] Task 2: Create Kitchen Printer Settings Screen
  - [ ] Create KitchenPrinterSettingsView.xaml
  - [ ] Create KitchenPrinterSettingsViewModel
  - [ ] Multiple printer management
  - [ ] Category assignment

- [ ] Task 3: Implement Category Routing
  - [ ] Map categories to printers
  - [ ] Handle unmapped categories
  - [ ] Default printer fallback
  - [ ] Multi-printer routing

- [ ] Task 4: Create KOT Format Editor
  - [ ] Font size configuration
  - [ ] Item grouping options
  - [ ] Modifier display options
  - [ ] Notes/comments display

- [ ] Task 5: Implement Printer Testing
  - [ ] Test print per printer
  - [ ] Print sample KOT
  - [ ] Verify category routing
  - [ ] Monitor print queue

## Dev Notes

### PrinterCategoryMapping Entity

```csharp
public class PrinterCategoryMapping
{
    public int Id { get; set; }
    public int PrinterId { get; set; }
    public int CategoryId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Printer Printer { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
```

### KOTSettings Entity

```csharp
public class KOTSettings
{
    public int Id { get; set; }
    public int PrinterId { get; set; }

    // Format settings
    public KOTFontSize TitleFontSize { get; set; } = KOTFontSize.Large;
    public KOTFontSize ItemFontSize { get; set; } = KOTFontSize.Normal;
    public KOTFontSize ModifierFontSize { get; set; } = KOTFontSize.Small;

    // Display options
    public bool ShowTableNumber { get; set; } = true;
    public bool ShowWaiterName { get; set; } = true;
    public bool ShowOrderTime { get; set; } = true;
    public bool ShowOrderNumber { get; set; } = true;
    public bool ShowCategoryHeader { get; set; } = true;

    // Item display
    public bool GroupByCategory { get; set; } = true;
    public bool ShowQuantityLarge { get; set; } = true;
    public bool ShowModifiersIndented { get; set; } = true;
    public bool ShowNotesHighlighted { get; set; } = true;

    // Alerts
    public bool PrintRushOrders { get; set; } = true;
    public bool HighlightAllergies { get; set; } = true;
    public bool BeepOnPrint { get; set; } = true;
    public int BeepCount { get; set; } = 2;

    // Copies
    public int CopiesPerOrder { get; set; } = 1;

    // Navigation
    public Printer Printer { get; set; } = null!;
}

public enum KOTFontSize
{
    Small,
    Normal,
    Large,
    ExtraLarge
}
```

### Kitchen Printer Settings Screen

```
+------------------------------------------+
|      KITCHEN PRINTER CONFIGURATION        |
+------------------------------------------+
| Kitchen Printers                          |
| +--------------------------------------+  |
| | [+] Add Kitchen Printer              |  |
| +--------------------------------------+  |
| | Kitchen Main      [Online]  [Edit]   |  |
| | Categories: Food, Grills             |  |
| +--------------------------------------+  |
| | Bar Printer       [Online]  [Edit]   |  |
| | Categories: Beverages                |  |
| +--------------------------------------+  |
| | Dessert Station   [Offline] [Edit]   |  |
| | Categories: Desserts                 |  |
| +--------------------------------------+  |
|                                           |
+------------------------------------------+
|      PRINTER DETAILS                      |
+------------------------------------------+
|                                           |
|  Printer Name: [Kitchen Main__________]   |
|                                           |
|  Connection: [Network___] [v]             |
|  IP Address: [192.168.1.101___]           |
|  Port:       [9100___]                    |
|                                           |
|  CATEGORY ROUTING                         |
|  ─────────────────────────────────────    |
|  +------------------------------------+   |
|  | [x] Food                           |   |
|  | [x] Grills                         |   |
|  | [x] Salads                         |   |
|  | [ ] Beverages                      |   |
|  | [ ] Desserts                       |   |
|  +------------------------------------+   |
|                                           |
|  [Test Print]   [Save]                    |
+------------------------------------------+
```

### KOT Format Editor

```
+------------------------------------------+
|      KOT FORMAT SETTINGS                  |
|      Kitchen Main                         |
+------------------------------------------+
|                                           |
|  FONT SIZES                               |
|  ─────────────────────────────────────    |
|  Title:     ( ) Small (x) Normal          |
|             ( ) Large ( ) X-Large         |
|                                           |
|  Items:     ( ) Small (x) Normal          |
|             ( ) Large ( ) X-Large         |
|                                           |
|  Modifiers: (x) Small ( ) Normal          |
|             ( ) Large                     |
|                                           |
|  DISPLAY OPTIONS                          |
|  ─────────────────────────────────────    |
|  [x] Show table number                    |
|  [x] Show waiter name                     |
|  [x] Show order time                      |
|  [x] Show order number                    |
|  [x] Group items by category              |
|                                           |
|  ITEM DISPLAY                             |
|  ─────────────────────────────────────    |
|  [x] Show quantity in large font          |
|  [x] Indent modifiers                     |
|  [x] Highlight notes/special requests     |
|  [x] Highlight allergies                  |
|                                           |
|  ALERTS                                   |
|  ─────────────────────────────────────    |
|  [x] Beep on print                        |
|  Beep count: [2___]                       |
|                                           |
|  Copies per order: [1___]                 |
|                                           |
|  [Preview KOT]                   [Save]   |
+------------------------------------------+
```

### KitchenPrinterSettingsViewModel

```csharp
public partial class KitchenPrinterSettingsViewModel : BaseViewModel
{
    private readonly IPrinterService _printerService;
    private readonly ICategoryService _categoryService;

    [ObservableProperty]
    private ObservableCollection<Printer> _kitchenPrinters = new();

    [ObservableProperty]
    private Printer? _selectedPrinter;

    [ObservableProperty]
    private ObservableCollection<CategorySelection> _categories = new();

    [ObservableProperty]
    private KOTSettings _kotSettings = new();

    public async Task InitializeAsync()
    {
        KitchenPrinters = new ObservableCollection<Printer>(
            await _printerService.GetPrintersAsync(PrinterType.Kitchen));

        var allCategories = await _categoryService.GetAllCategoriesAsync();
        Categories = new ObservableCollection<CategorySelection>(
            allCategories.Select(c => new CategorySelection { Category = c }));

        if (KitchenPrinters.Any())
        {
            SelectedPrinter = KitchenPrinters.First();
        }
    }

    partial void OnSelectedPrinterChanged(Printer? value)
    {
        if (value != null)
        {
            LoadPrinterDetails(value);
        }
    }

    private async void LoadPrinterDetails(Printer printer)
    {
        // Load category mappings
        var mappings = await _printerService.GetCategoryMappingsAsync(printer.Id);
        var mappedIds = mappings.Select(m => m.CategoryId).ToHashSet();

        foreach (var category in Categories)
        {
            category.IsSelected = mappedIds.Contains(category.Category.Id);
        }

        // Load KOT settings
        KotSettings = await _printerService.GetKOTSettingsAsync(printer.Id)
            ?? new KOTSettings { PrinterId = printer.Id };
    }

    [RelayCommand]
    private async Task AddKitchenPrinterAsync()
    {
        var printer = new Printer
        {
            Name = "New Kitchen Printer",
            Type = PrinterType.Kitchen,
            ConnectionType = PrinterConnectionType.Network,
            PaperWidth = 80,
            CharsPerLine = 48,
            Settings = new PrinterSettings()
        };

        await _printerService.SavePrinterAsync(printer);
        KitchenPrinters.Add(printer);
        SelectedPrinter = printer;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedPrinter == null) return;

        // Save printer
        await _printerService.SavePrinterAsync(SelectedPrinter);

        // Save category mappings
        var selectedCategoryIds = Categories
            .Where(c => c.IsSelected)
            .Select(c => c.Category.Id)
            .ToList();

        await _printerService.SaveCategoryMappingsAsync(
            SelectedPrinter.Id,
            selectedCategoryIds);

        // Save KOT settings
        KotSettings.PrinterId = SelectedPrinter.Id;
        await _printerService.SaveKOTSettingsAsync(KotSettings);

        await _dialogService.ShowMessageAsync(
            "Saved",
            "Kitchen printer settings saved successfully.");
    }

    [RelayCommand]
    private async Task TestPrintAsync()
    {
        if (SelectedPrinter == null) return;

        var result = await _printerService.PrintTestKOTAsync(SelectedPrinter);

        if (result.Success)
        {
            await _dialogService.ShowMessageAsync(
                "Test Print",
                "Sample KOT printed successfully!");
        }
        else
        {
            await _dialogService.ShowMessageAsync(
                "Print Failed",
                result.ErrorMessage ?? "Unknown error.");
        }
    }

    [RelayCommand]
    private async Task PreviewKOTAsync()
    {
        var preview = GenerateKOTPreview();
        await _dialogService.ShowKOTPreviewAsync(preview);
    }

    private string GenerateKOTPreview()
    {
        var sb = new StringBuilder();

        // Generate sample KOT based on current settings
        sb.AppendLine("╔══════════════════════════════════════════╗");
        sb.AppendLine("║           KITCHEN ORDER TICKET           ║");
        sb.AppendLine("╠══════════════════════════════════════════╣");

        if (KotSettings.ShowTableNumber)
            sb.AppendLine("║  TABLE: 07                               ║");

        if (KotSettings.ShowWaiterName)
            sb.AppendLine("║  WAITER: John Smith                      ║");

        if (KotSettings.ShowOrderTime)
            sb.AppendLine("║  TIME: 14:35                             ║");

        if (KotSettings.ShowOrderNumber)
            sb.AppendLine("║  ORDER: O-0042                           ║");

        sb.AppendLine("╠══════════════════════════════════════════╣");

        if (KotSettings.ShowCategoryHeader)
            sb.AppendLine("║  ** FOOD **                              ║");

        sb.AppendLine("║  2x  Grilled Chicken                     ║");
        sb.AppendLine("║      - Extra spicy                       ║");
        sb.AppendLine("║      - No onions                         ║");
        sb.AppendLine("║                                          ║");
        sb.AppendLine("║  1x  Fish & Chips                        ║");
        sb.AppendLine("║      *** ALLERGY: Gluten-free ***        ║");

        sb.AppendLine("╚══════════════════════════════════════════╝");

        return sb.ToString();
    }
}

public class CategorySelection : ObservableObject
{
    private bool _isSelected;

    public Category Category { get; set; } = null!;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
```

### Kitchen Order Routing Service

```csharp
public interface IKitchenOrderRoutingService
{
    Task<Dictionary<Printer, List<OrderItem>>> RouteOrderItemsAsync(
        IEnumerable<OrderItem> items);
    Task PrintKOTsAsync(int orderId, bool isIncremental = false);
}

public class KitchenOrderRoutingService : IKitchenOrderRoutingService
{
    private readonly ApplicationDbContext _context;
    private readonly IPrintService _printService;

    public async Task<Dictionary<Printer, List<OrderItem>>> RouteOrderItemsAsync(
        IEnumerable<OrderItem> items)
    {
        var routes = new Dictionary<Printer, List<OrderItem>>();

        // Get all kitchen printers with their category mappings
        var kitchenPrinters = await _context.Printers
            .Where(p => p.Type == PrinterType.Kitchen && p.IsActive)
            .Include(p => p.CategoryMappings)
            .ToListAsync();

        // Get default kitchen printer
        var defaultPrinter = kitchenPrinters.FirstOrDefault(p => p.IsDefault)
            ?? kitchenPrinters.FirstOrDefault();

        foreach (var item in items)
        {
            // Find printer for this item's category
            var printer = kitchenPrinters.FirstOrDefault(p =>
                p.CategoryMappings.Any(m =>
                    m.CategoryId == item.Product.CategoryId && m.IsActive))
                ?? defaultPrinter;

            if (printer != null)
            {
                if (!routes.ContainsKey(printer))
                {
                    routes[printer] = new List<OrderItem>();
                }
                routes[printer].Add(item);
            }
        }

        return routes;
    }

    public async Task PrintKOTsAsync(int orderId, bool isIncremental = false)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Category)
            .Include(o => o.Items)
                .ThenInclude(i => i.Modifiers)
            .Include(o => o.User)
            .Include(o => o.Table)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) return;

        // Get items to print (all or only new)
        var itemsToPrint = isIncremental
            ? order.Items.Where(i => !i.IsSentToKitchen).ToList()
            : order.Items.ToList();

        if (!itemsToPrint.Any()) return;

        // Route items to printers
        var routes = await RouteOrderItemsAsync(itemsToPrint);

        // Print to each printer
        foreach (var route in routes)
        {
            var kotData = new KOTData
            {
                OrderNumber = order.OrderNumber,
                TableNumber = order.Table?.TableNumber,
                WaiterName = order.User?.FullName,
                OrderTime = order.CreatedAt,
                IsIncremental = isIncremental,
                Items = route.Value.Select(i => new KOTItemData
                {
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    CategoryName = i.Product.Category?.Name,
                    Modifiers = i.Modifiers.Select(m => m.Name).ToList(),
                    Notes = i.Notes,
                    IsVoided = i.IsVoided
                }).ToList()
            };

            await _printService.PrintKOTAsync(route.Key, kotData);
        }

        // Mark items as sent to kitchen
        foreach (var item in itemsToPrint)
        {
            item.IsSentToKitchen = true;
            item.SentToKitchenAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }
}
```

### Sample KOT Print (80mm)

```
╔══════════════════════════════════════════════╗
║         ** KITCHEN ORDER TICKET **           ║
╠══════════════════════════════════════════════╣
║  TABLE: 07           TIME: 14:35             ║
║  WAITER: John Smith  ORDER: O-0042           ║
╠══════════════════════════════════════════════╣
║                                              ║
║  ** FOOD **                                  ║
║  ─────────────────────────────────────────   ║
║                                              ║
║   2   GRILLED CHICKEN                        ║
║       - Extra spicy                          ║
║       - No onions                            ║
║       NOTE: Well done                        ║
║                                              ║
║   1   FISH & CHIPS                           ║
║       *** GLUTEN FREE ***                    ║
║                                              ║
║   1   CHIPS REGULAR                          ║
║                                              ║
║  ** GRILLS **                                ║
║  ─────────────────────────────────────────   ║
║                                              ║
║   1   BEEF STEAK (Medium Rare)               ║
║       - Mushroom sauce                       ║
║       - Extra vegetables                     ║
║                                              ║
╠══════════════════════════════════════════════╣
║  ITEMS: 5              NEW ORDER             ║
╚══════════════════════════════════════════════╝
```

### Incremental KOT Print (80mm)

```
╔══════════════════════════════════════════════╗
║      ** KITCHEN ORDER - ADDITION **          ║
╠══════════════════════════════════════════════╣
║  TABLE: 07           TIME: 14:55             ║
║  WAITER: John Smith  ORDER: O-0042           ║
╠══════════════════════════════════════════════╣
║                                              ║
║  >>>>>>>>>> NEW ITEMS <<<<<<<<<<             ║
║                                              ║
║   1   COCA COLA 500ML                        ║
║                                              ║
║   1   GRILLED CHICKEN                        ║
║       - No pepper                            ║
║                                              ║
╠══════════════════════════════════════════════╣
║  ITEMS: 2              ADDITION              ║
╚══════════════════════════════════════════════╝
```

### References
- [Source: docs/PRD_Hospitality_POS_System.md#5.10.2-Kitchen-Printing]
- [Source: docs/PRD_Hospitality_POS_System.md#PR-011 to PR-020]

## Dev Agent Record

### Agent Model Used
{{agent_model_name_version}}

### Completion Notes List

### File List
