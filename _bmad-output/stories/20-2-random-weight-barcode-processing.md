# Story 20.2: Random Weight Barcode Processing

## Story
**As a** cashier,
**I want to** scan pre-weighed items with price-embedded barcodes,
**So that** produce and deli items process correctly.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Implementation Summary
**Completed:** January 2026

### Files Created/Modified:
- `src/HospitalityPOS.Infrastructure/Services/BarcodeService.cs` - Weighted barcode with:
  - `ParseWeightedBarcode` - Extract PLU and price from barcode
  - `IsWeightEmbeddedBarcode` - Detect prefix 02/2 barcodes
  - Price extraction (5 digits as cents)
  - Check digit validation

## Epic
**Epic 20: Barcode, Scale & PLU Management**

## Context
Produce, deli, and butchery items are weighed and labeled in-store with barcodes that contain the PLU code and either the price or weight. The system must parse these barcodes to extract the correct product and price without requiring manual entry.

## Acceptance Criteria

### AC1: Price-Embedded Barcode Recognition
**Given** a random weight barcode (prefix 02 or 2)
**When** scanned
**Then**:
- System recognizes the prefix as random weight
- Extracts PLU code (5 digits after prefix)
- Extracts embedded price (last 5 digits before check digit)
- Adds item with extracted price

### AC2: Product Matching
**Given** PLU code is extracted
**When** looking up product
**Then**:
- Matches to product by PLU code
- Uses product description from database
- Calculates unit price if weight embedded

### AC3: Price Application
**Given** price is embedded
**When** adding to order
**Then**:
- Uses embedded price (not shelf price)
- Shows "Weighed" indicator on line item
- Prevents manual price edit (requires manager override)

### AC4: Configurable Prefix
**Given** different stores use different prefixes
**When** configuring system
**Then**:
- Admin can set random weight prefixes (02, 2, 20-29)
- Can configure price vs weight embedding
- Settings apply to all scans

## Technical Notes

### Barcode Format
```
EAN-13 Price-Embedded:
02 XXXXX YYYYY C
│  │     │     └─ Check digit (calculated)
│  │     └─────── Price in cents (5 digits, e.g., 15000 = KSh 150.00)
│  └───────────── PLU/Item code (5 digits)
└──────────────── Prefix (02 = in-store random weight)

UPC-A Price-Embedded:
2 XXXXX YYYYY C
│ │     │     └─ Check digit
│ │     └─────── Price (5 digits, KSh, e.g., 01500 = KSh 15.00)
│ └───────────── PLU code (5 digits)
└────────────── Prefix (2 = random weight)
```

### Implementation
```csharp
public class RandomWeightBarcodeParser
{
    private readonly List<string> _priceEmbeddedPrefixes;

    public RandomWeightBarcodeParser(IConfiguration config)
    {
        _priceEmbeddedPrefixes = config.GetSection("RandomWeight:Prefixes")
            .Get<List<string>>() ?? new List<string> { "02", "2" };
    }

    public RandomWeightResult Parse(string barcode)
    {
        var prefix = GetMatchingPrefix(barcode);
        if (prefix == null)
            return null;

        var remaining = barcode.Substring(prefix.Length);

        // For EAN-13 with 02 prefix: XXXXX YYYYY C (11 chars after prefix)
        // For UPC-A with 2 prefix: XXXXX YYYYY C (11 chars after prefix)
        if (remaining.Length < 11)
            return null;

        var pluCode = remaining.Substring(0, 5);
        var priceDigits = remaining.Substring(5, 5);
        var checkDigit = remaining.Substring(10, 1);

        // Parse price (stored as cents or shillings depending on config)
        var embeddedPrice = ParsePrice(priceDigits);

        return new RandomWeightResult
        {
            PLUCode = pluCode,
            EmbeddedPrice = embeddedPrice,
            OriginalBarcode = barcode,
            IsValid = ValidateCheckDigit(barcode, checkDigit)
        };
    }

    private decimal ParsePrice(string priceDigits)
    {
        if (!int.TryParse(priceDigits, out var cents))
            return 0;

        // Kenya: prices in shillings, no cents typically
        // Adjust based on local configuration
        return cents / 100m;  // Convert cents to shillings
    }

    private string GetMatchingPrefix(string barcode)
    {
        return _priceEmbeddedPrefixes
            .Where(p => barcode.StartsWith(p))
            .OrderByDescending(p => p.Length)
            .FirstOrDefault();
    }
}
```

### Integration with Barcode Service
```csharp
public class BarcodeService : IBarcodeService
{
    public bool IsWeightEmbeddedBarcode(string barcode)
    {
        return _randomWeightParser.Parse(barcode) != null;
    }

    private async Task<BarcodeResult> ProcessWeightEmbeddedAsync(string barcode)
    {
        var parsed = _randomWeightParser.Parse(barcode);
        if (parsed == null || !parsed.IsValid)
        {
            return new BarcodeResult
            {
                Success = false,
                ErrorMessage = "Invalid random weight barcode"
            };
        }

        var product = await _productRepository.GetByPLUCodeAsync(parsed.PLUCode);
        if (product == null)
        {
            return new BarcodeResult
            {
                Success = false,
                ErrorMessage = $"PLU {parsed.PLUCode} not found"
            };
        }

        return new BarcodeResult
        {
            Success = true,
            Product = product,
            OverridePrice = parsed.EmbeddedPrice,
            IsWeighed = true,
            BarcodeType = BarcodeType.RandomWeight
        };
    }
}
```

## Dependencies
- Story 20.1: Barcode Scanner Integration
- Story 20.5: PLU Code Management

## Files to Create/Modify
- `HospitalityPOS.Business/Services/RandomWeightBarcodeParser.cs`
- `HospitalityPOS.Business/Services/BarcodeService.cs` (add weight handling)
- `appsettings.json` (random weight configuration)
- Product entity: ensure PLUCode field exists

## Testing Requirements
- Unit tests for barcode parsing
- Tests for various price formats
- Tests for invalid barcodes
- Tests for check digit validation

## Definition of Done
- [ ] Random weight barcodes recognized
- [ ] PLU code extracted correctly
- [ ] Price extracted and applied
- [ ] "Weighed" indicator shown
- [ ] Configurable prefixes working
- [ ] Check digit validation working
- [ ] Unit tests passing
- [ ] Code reviewed and approved
