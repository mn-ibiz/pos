# Story 20.1: Barcode Scanner Integration

## Story
**As a** cashier,
**I want** items to be added instantly when scanned,
**So that** checkout is fast and accurate.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Implementation Summary
**Completed:** January 2026

### Files Created/Modified:
- `src/HospitalityPOS.Infrastructure/Services/BarcodeService.cs` - Scanner integration with:
  - `ProcessBarcodeAsync` - Product lookup by barcode
  - `DetectBarcodeType` - UPC-A, EAN-13, EAN-8, Code128 detection
  - Scanner input handler for USB keyboard wedge
  - Quantity multiplier support

## Epic
**Epic 20: Barcode, Scale & PLU Management**

## Context
Retail checkout speed is critical. Barcode scanning must be near-instantaneous (<100ms) to maintain checkout throughput. The system must support common barcode formats and handle unknown items gracefully.

## Acceptance Criteria

### AC1: Instant Item Lookup
**Given** a barcode scanner is connected (USB keyboard wedge)
**When** scanning a product barcode
**Then**:
- Item is added to order within 100ms
- Product name, price, and quantity displayed
- Beep sound confirms successful scan
- Focus returns to scan input

### AC2: Barcode Format Detection
**Given** barcode types vary
**When** scanning
**Then** system auto-detects:
- UPC-A (12 digits)
- EAN-13 (13 digits)
- Code 128 (variable length)
- Internal store codes (configurable prefix)

### AC3: Unknown Barcode Handling
**Given** unknown barcode is scanned
**When** no product matches
**Then**:
- Displays "Item not found" message
- Shows the scanned barcode value
- Provides manual search option
- Option to add new product (with permission)

### AC4: Quantity Multiplier
**Given** multiple of same item
**When** entering quantity before scan
**Then**:
- Quantity input field available
- Next scan multiplies by entered quantity
- Quantity resets to 1 after scan
- Keyboard shortcut (e.g., * key) for quantity

## Technical Notes

### Implementation Details
```csharp
public interface IBarcodeService
{
    Task<BarcodeResult> ProcessBarcodeAsync(string barcode);
    BarcodeType DetectBarcodeType(string barcode);
    bool IsWeightEmbeddedBarcode(string barcode);
}

public class BarcodeService : IBarcodeService
{
    public async Task<BarcodeResult> ProcessBarcodeAsync(string barcode)
    {
        var type = DetectBarcodeType(barcode);

        if (IsWeightEmbeddedBarcode(barcode))
        {
            return await ProcessWeightEmbeddedAsync(barcode);
        }

        var product = await _productRepository.GetByBarcodeAsync(barcode);

        if (product == null)
        {
            return new BarcodeResult
            {
                Success = false,
                Barcode = barcode,
                BarcodeType = type,
                ErrorMessage = "Item not found"
            };
        }

        return new BarcodeResult
        {
            Success = true,
            Product = product,
            BarcodeType = type
        };
    }

    public BarcodeType DetectBarcodeType(string barcode)
    {
        return barcode.Length switch
        {
            12 when barcode.All(char.IsDigit) => BarcodeType.UPCA,
            13 when barcode.All(char.IsDigit) => BarcodeType.EAN13,
            8 when barcode.All(char.IsDigit) => BarcodeType.EAN8,
            _ when barcode.Length >= 4 => BarcodeType.Code128,
            _ => BarcodeType.Unknown
        };
    }
}
```

### Scanner Input Handler
```csharp
public class ScannerInputHandler
{
    private readonly StringBuilder _buffer = new();
    private readonly Stopwatch _inputTimer = new();
    private const int ScanThresholdMs = 50;  // Scans are fast

    public event EventHandler<string> BarcodeScanned;

    public void ProcessKeyPress(Key key)
    {
        if (key == Key.Return || key == Key.Enter)
        {
            if (_buffer.Length > 0 && _inputTimer.ElapsedMilliseconds < ScanThresholdMs * _buffer.Length)
            {
                // Fast input = scanner
                BarcodeScanned?.Invoke(this, _buffer.ToString());
            }
            _buffer.Clear();
            _inputTimer.Reset();
            return;
        }

        if (!_inputTimer.IsRunning)
            _inputTimer.Start();

        _buffer.Append(KeyToChar(key));
    }
}
```

### POS ViewModel Integration
```csharp
public partial class POSViewModel : ObservableObject
{
    [ObservableProperty]
    private int _quantityMultiplier = 1;

    [RelayCommand]
    private async Task ProcessBarcodeAsync(string barcode)
    {
        var result = await _barcodeService.ProcessBarcodeAsync(barcode);

        if (result.Success)
        {
            await AddToOrderAsync(result.Product, QuantityMultiplier);
            QuantityMultiplier = 1;
            PlayBeep();
        }
        else
        {
            ShowItemNotFound(result.Barcode);
        }
    }
}
```

## Dependencies
- Epic 4: Product Management (barcode field on products)
- Epic 5: Sales & Order Management

## Files to Create/Modify
- `HospitalityPOS.Core/Interfaces/IBarcodeService.cs`
- `HospitalityPOS.Business/Services/BarcodeService.cs`
- `HospitalityPOS.WPF/Helpers/ScannerInputHandler.cs`
- `HospitalityPOS.WPF/ViewModels/POS/POSViewModel.cs` (barcode handling)
- Product entity: ensure Barcode field indexed

## Testing Requirements
- Unit tests for barcode type detection
- Unit tests for product lookup
- Performance test: <100ms response time
- Integration tests with scanner simulator

## Definition of Done
- [ ] Scanner input captured correctly
- [ ] All barcode types detected
- [ ] Product lookup <100ms
- [ ] Unknown barcode handling implemented
- [ ] Quantity multiplier working
- [ ] Audio feedback on scan
- [ ] Unit tests passing
- [ ] Code reviewed and approved
