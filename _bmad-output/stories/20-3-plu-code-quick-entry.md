# Story 20.3: PLU Code Quick Entry

## Story
**As a** cashier,
**I want to** enter PLU codes for produce items,
**So that** items without barcodes can be quickly added.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Implementation Summary
**Completed:** January 2026

### Files Created/Modified:
- `src/HospitalityPOS.Infrastructure/Services/BarcodeService.cs` - PLU lookup with:
  - `LookupPLUAsync` - Find product by 4-5 digit PLU code
  - `GetPLUQuickKeysAsync` - Configurable quick-access buttons
  - Similar PLU suggestions when not found
  - Weight prompt integration

## Epic
**Epic 20: Barcode, Scale & PLU Management**

## Context
Not all produce items have barcodes. Cashiers need to quickly enter 4-5 digit PLU codes to add loose produce. Common items should have quick-access buttons to speed up checkout.

## Acceptance Criteria

### AC1: PLU Code Input
**Given** product has no barcode
**When** entering 4-5 digit PLU code
**Then**:
- PLU input field is easily accessible
- Product is looked up on Enter key
- Product added to order with scale prompt if weighted

### AC2: PLU Quick Keys
**Given** frequently used produce items
**When** accessing PLU quick-keys
**Then**:
- Grid of common produce buttons displayed
- Shows item image/icon and name
- Single tap adds item (prompts for weight if needed)
- Configurable by admin

### AC3: PLU Not Found
**Given** PLU entered doesn't exist
**When** displaying error
**Then**:
- Shows "PLU not found" message
- Offers search by product name
- Shows similar PLU suggestions if available

### AC4: Weighted Item Prompt
**Given** PLU item requires weighing
**When** adding to order
**Then**:
- Scale weight read automatically if connected
- Manual weight entry option available
- Price calculated based on weight × unit price

## Technical Notes

### Implementation Details
```csharp
public interface IPLUService
{
    Task<Product> LookupPLUAsync(string pluCode);
    Task<List<PLUQuickKey>> GetQuickKeysAsync();
    Task<List<Product>> SearchByNameAsync(string name);
    Task<List<Product>> GetSimilarPLUsAsync(string pluCode);
}

public class PLUQuickKey
{
    public Guid ProductId { get; set; }
    public string PLUCode { get; set; }
    public string DisplayName { get; set; }
    public string ImagePath { get; set; }
    public int DisplayOrder { get; set; }
    public bool RequiresWeight { get; set; }
}

public class PLUService : IPLUService
{
    public async Task<Product> LookupPLUAsync(string pluCode)
    {
        // Normalize PLU code (remove leading zeros if needed)
        pluCode = pluCode.TrimStart('0').PadLeft(4, '0');

        return await _productRepository
            .Query()
            .FirstOrDefaultAsync(p => p.PLUCode == pluCode && p.IsActive);
    }

    public async Task<List<Product>> GetSimilarPLUsAsync(string pluCode)
    {
        // Find PLUs with similar numbers (typo correction)
        var similar = await _productRepository
            .Query()
            .Where(p => p.PLUCode != null && p.IsActive)
            .ToListAsync();

        return similar
            .Where(p => LevenshteinDistance(p.PLUCode, pluCode) <= 2)
            .OrderBy(p => LevenshteinDistance(p.PLUCode, pluCode))
            .Take(5)
            .ToList();
    }
}
```

### ViewModel for PLU Entry
```csharp
public partial class PLUEntryViewModel : ObservableObject
{
    [ObservableProperty]
    private string _pluCode;

    [ObservableProperty]
    private ObservableCollection<PLUQuickKey> _quickKeys;

    [ObservableProperty]
    private bool _showWeightPrompt;

    [ObservableProperty]
    private decimal _weight;

    [RelayCommand]
    private async Task LookupPLUAsync()
    {
        if (string.IsNullOrWhiteSpace(PLUCode))
            return;

        var product = await _pluService.LookupPLUAsync(PLUCode);

        if (product == null)
        {
            var similar = await _pluService.GetSimilarPLUsAsync(PLUCode);
            ShowPLUNotFound(similar);
            return;
        }

        if (product.IsSoldByWeight)
        {
            ShowWeightPrompt = true;
            // Wait for weight input or scale read
        }
        else
        {
            await AddToOrderAsync(product, 1);
        }

        PLUCode = string.Empty;
    }

    [RelayCommand]
    private async Task SelectQuickKeyAsync(PLUQuickKey key)
    {
        var product = await _productRepository.GetByIdAsync(key.ProductId);

        if (key.RequiresWeight)
        {
            ShowWeightPrompt = true;
        }
        else
        {
            await AddToOrderAsync(product, 1);
        }
    }
}
```

### UI Layout
```
┌────────────────────────────────────────────────────────────────────┐
│  PLU QUICK ENTRY                                                    │
├────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  Enter PLU: [____4294____] [LOOKUP]                                │
│                                                                     │
│  ┌─────────────┬─────────────┬─────────────┬─────────────┐        │
│  │   🍌        │   🍎        │   🍅        │   🥒        │        │
│  │  Bananas    │  Apples     │  Tomatoes   │  Cucumber   │        │
│  │  4011       │  4015       │  4064       │  4062       │        │
│  ├─────────────┼─────────────┼─────────────┼─────────────┤        │
│  │   🥕        │   🧅        │   🥔        │   🥬        │        │
│  │  Carrots    │  Onions     │  Potatoes   │  Cabbage    │        │
│  │  4562       │  4663       │  4072       │  4069       │        │
│  ├─────────────┼─────────────┼─────────────┼─────────────┤        │
│  │   🍋        │   🍊        │   🍇        │   🥭        │        │
│  │  Lemons     │  Oranges    │  Grapes     │  Mangoes    │        │
│  │  4033       │  4012       │  4022       │  4051       │        │
│  └─────────────┴─────────────┴─────────────┴─────────────┘        │
│                                                                     │
│  [SEARCH BY NAME]                              [BACK TO POS]       │
│                                                                     │
└────────────────────────────────────────────────────────────────────┘
```

## Dependencies
- Story 20.1: Barcode Scanner Integration
- Story 20.4: Scale Integration
- Epic 4: Product Management (PLU field)

## Files to Create/Modify
- `HospitalityPOS.Core/Interfaces/IPLUService.cs`
- `HospitalityPOS.Business/Services/PLUService.cs`
- `HospitalityPOS.Core/Entities/PLUQuickKey.cs`
- `HospitalityPOS.WPF/ViewModels/POS/PLUEntryViewModel.cs`
- `HospitalityPOS.WPF/Views/POS/PLUEntryPanel.xaml`
- Database migration for PLUQuickKey table

## Testing Requirements
- Unit tests for PLU lookup
- Unit tests for similar PLU suggestions
- UI tests for quick key functionality
- Integration tests with scale

## Definition of Done
- [ ] PLU input field working
- [ ] Quick key grid displayed
- [ ] Not found handling with suggestions
- [ ] Weight prompt for weighted items
- [ ] Admin can configure quick keys
- [ ] Unit tests passing
- [ ] Code reviewed and approved
