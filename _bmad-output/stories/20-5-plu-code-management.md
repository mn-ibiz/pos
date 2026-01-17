# Story 20.5: PLU Code Management

## Story
**As an** administrator,
**I want to** assign and manage PLU codes for products,
**So that** produce items have quick lookup codes.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Implementation Summary
**Completed:** January 2026

### Files Created/Modified:
- `src/HospitalityPOS.Infrastructure/Services/BarcodeService.cs` - PLU management with:
  - `ValidatePLUAsync` - 4-5 digit format and uniqueness check
  - `ImportPLUFromCsvAsync` - Bulk PLU import
  - `ExportPLUToCsvAsync` - Export for scale label printers
  - CAS scale format export

## Epic
**Epic 20: Barcode, Scale & PLU Management**

## Context
PLU (Price Look-Up) codes are short numeric codes used to identify produce and other items sold by weight. Administrators need to manage these codes, ensure uniqueness, and be able to import/export for scale label printers.

## Acceptance Criteria

### AC1: PLU Assignment
**Given** creating/editing a product
**When** entering PLU code
**Then**:
- Validates 4-5 digit format
- Checks for uniqueness across products
- Shows warning if near existing PLU
- Saves with product

### AC2: PLU Listing
**Given** PLU codes exist
**When** viewing PLU list
**Then**:
- Shows all products with PLU codes
- Grouped by department/category
- Searchable by PLU or product name
- Sortable columns

### AC3: Bulk Import
**Given** PLU import is needed
**When** importing from CSV
**Then**:
- Accepts CSV format (PLU, ProductName, Price, Department)
- Validates all PLU codes
- Reports conflicts before import
- Creates/updates products as configured

### AC4: Export for Scale Labels
**Given** scale label printers need PLU data
**When** exporting PLU list
**Then**:
- Exports in common formats (CSV, TXT)
- Includes: PLU, Description, Price, Tare, UOM
- Format compatible with scale systems (CAS, Mettler)

## Technical Notes

### PLU Validation
```csharp
public class PLUValidator
{
    public async Task<ValidationResult> ValidateAsync(string pluCode, Guid? excludeProductId = null)
    {
        // Format validation
        if (string.IsNullOrWhiteSpace(pluCode))
            return ValidationResult.Error("PLU code is required");

        if (pluCode.Length < 4 || pluCode.Length > 5)
            return ValidationResult.Error("PLU code must be 4-5 digits");

        if (!pluCode.All(char.IsDigit))
            return ValidationResult.Error("PLU code must contain only digits");

        // Uniqueness check
        var existing = await _productRepository
            .Query()
            .Where(p => p.PLUCode == pluCode && p.IsActive)
            .Where(p => excludeProductId == null || p.Id != excludeProductId)
            .FirstOrDefaultAsync();

        if (existing != null)
            return ValidationResult.Error($"PLU code already assigned to: {existing.Name}");

        return ValidationResult.Success();
    }
}
```

### PLU Import Service
```csharp
public class PLUImportService
{
    public async Task<ImportResult> ImportFromCsvAsync(Stream csvStream, ImportOptions options)
    {
        var result = new ImportResult();
        var records = ParseCsv(csvStream);

        // Validation pass
        foreach (var record in records)
        {
            var validation = await _validator.ValidateAsync(record.PLUCode);
            if (!validation.IsValid)
            {
                result.Errors.Add(new ImportError
                {
                    Row = record.RowNumber,
                    PLUCode = record.PLUCode,
                    Error = validation.Error
                });
            }
        }

        if (result.Errors.Any() && !options.IgnoreErrors)
            return result;

        // Import pass
        foreach (var record in records.Where(r => !result.HasErrorForRow(r.RowNumber)))
        {
            try
            {
                var product = await FindOrCreateProductAsync(record, options);
                product.PLUCode = record.PLUCode;
                await _productRepository.UpdateAsync(product);
                result.ImportedCount++;
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ImportError
                {
                    Row = record.RowNumber,
                    PLUCode = record.PLUCode,
                    Error = ex.Message
                });
            }
        }

        return result;
    }
}
```

### Export for Scales
```csharp
public class PLUExportService
{
    public async Task<byte[]> ExportForCASScaleAsync()
    {
        var products = await _productRepository
            .Query()
            .Where(p => p.PLUCode != null && p.IsActive)
            .OrderBy(p => p.PLUCode)
            .ToListAsync();

        var sb = new StringBuilder();

        foreach (var product in products)
        {
            // CAS scale format: PLU|Description|Price|Tare|Unit
            sb.AppendLine($"{product.PLUCode}|{TruncateDescription(product.Name, 24)}|{FormatPrice(product.Price)}|0|1");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportToCsvAsync()
    {
        var products = await _productRepository
            .Query()
            .Where(p => p.PLUCode != null && p.IsActive)
            .Include(p => p.Category)
            .OrderBy(p => p.Category.Name)
            .ThenBy(p => p.PLUCode)
            .ToListAsync();

        var csv = new StringBuilder();
        csv.AppendLine("PLU,ProductName,Category,UnitPrice,IsSoldByWeight");

        foreach (var product in products)
        {
            csv.AppendLine($"{product.PLUCode},\"{product.Name}\",\"{product.Category?.Name}\",{product.Price},{product.IsSoldByWeight}");
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }
}
```

### UI for PLU Management
```
┌────────────────────────────────────────────────────────────────────┐
│                    PLU CODE MANAGEMENT                              │
├────────────────────────────────────────────────────────────────────┤
│  [Import CSV]  [Export CSV]  [Export for Scales]                   │
│                                                                     │
│  Search: [________________] [Category: All ▼] [Search]             │
├────────────────────────────────────────────────────────────────────┤
│  PLU    │ Product Name         │ Category   │ Price    │ Weighted │
│─────────┼──────────────────────┼────────────┼──────────┼──────────│
│  4011   │ Bananas              │ Produce    │ 120.00/kg│ ✓        │
│  4012   │ Oranges              │ Produce    │ 80.00/kg │ ✓        │
│  4015   │ Apples Red Delicious │ Produce    │ 200.00/kg│ ✓        │
│  4022   │ Grapes Green         │ Produce    │ 350.00/kg│ ✓        │
│  4033   │ Lemons               │ Produce    │ 60.00/kg │ ✓        │
│  4051   │ Mangoes              │ Produce    │ 180.00/kg│ ✓        │
│  4062   │ Cucumber             │ Produce    │ 50.00/ea │          │
│  4064   │ Tomatoes             │ Produce    │ 100.00/kg│ ✓        │
└────────────────────────────────────────────────────────────────────┘
│  Total: 156 PLU codes                              Page 1 of 8     │
└────────────────────────────────────────────────────────────────────┘
```

## Dependencies
- Epic 4: Product Management
- Story 20.3: PLU Code Quick Entry

## Files to Create/Modify
- `HospitalityPOS.Business/Validators/PLUValidator.cs`
- `HospitalityPOS.Business/Services/PLUImportService.cs`
- `HospitalityPOS.Business/Services/PLUExportService.cs`
- `HospitalityPOS.WPF/ViewModels/Admin/PLUManagementViewModel.cs`
- `HospitalityPOS.WPF/Views/Admin/PLUManagementView.xaml`
- Modify ProductViewModel to include PLU validation

## Testing Requirements
- Unit tests for PLU validation
- Unit tests for import/export
- Tests for duplicate detection
- Tests for scale format export

## Definition of Done
- [ ] PLU assignment on products working
- [ ] Uniqueness validation working
- [ ] PLU list view implemented
- [ ] CSV import working
- [ ] Export for scales working
- [ ] Search and filter working
- [ ] Unit tests passing
- [ ] Code reviewed and approved
