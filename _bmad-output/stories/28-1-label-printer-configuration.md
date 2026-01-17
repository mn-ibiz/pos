# Story 28.1: Label Printer Configuration

## Story
**As an** administrator,
**I want to** configure label printers for shelf labeling,
**So that** labels can be printed on appropriate devices.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Epic
**Epic 28: Shelf Label Printing**

## Acceptance Criteria

### AC1: Printer Setup
**Given** label printer connected
**When** configuring
**Then** can set: printer name/port, label size, print language (ZPL/EPL)

### AC2: Test Print
**Given** printer configured
**When** testing
**Then** prints test label successfully

### AC3: Default Printer Assignment
**Given** multiple printers
**When** managing
**Then** can set default printer per category

## Technical Notes
```csharp
public class LabelPrinter
{
    public Guid Id { get; set; }
    public string Name { get; set; }  // "Shelf Label Printer 1"
    public string ConnectionString { get; set; }  // COM port, IP, or USB path
    public LabelPrinterType PrinterType { get; set; }
    public LabelPrintLanguage PrintLanguage { get; set; }
    public LabelSize DefaultLabelSize { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public enum LabelPrinterType
{
    Serial,     // COM port
    Network,    // TCP/IP
    USB,        // Direct USB
    Windows     // Windows printer driver
}

public enum LabelPrintLanguage
{
    ZPL,    // Zebra Programming Language
    EPL,    // Eltron Programming Language
    TSPL,   // TSC Printer Language
    Raw     // Raw ESC/POS or driver-managed
}

public class LabelSize
{
    public Guid Id { get; set; }
    public string Name { get; set; }  // "Small", "Medium", "Large"
    public decimal WidthMm { get; set; }
    public decimal HeightMm { get; set; }
    public int DotsPerMm { get; set; } = 8;  // 203 DPI = 8 dots/mm
}

public class CategoryPrinterAssignment
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public Category Category { get; set; }
    public Guid LabelPrinterId { get; set; }
    public LabelPrinter LabelPrinter { get; set; }
    public Guid? LabelTemplateId { get; set; }
    public LabelTemplate LabelTemplate { get; set; }
}

public interface ILabelPrinterService
{
    Task<LabelPrinter> CreatePrinterAsync(LabelPrinterDto printer);
    Task<LabelPrinter> UpdatePrinterAsync(Guid id, LabelPrinterDto printer);
    Task<bool> DeletePrinterAsync(Guid id);
    Task<List<LabelPrinter>> GetAllPrintersAsync();
    Task<bool> TestPrinterConnectionAsync(Guid printerId);
    Task<bool> PrintTestLabelAsync(Guid printerId);
    Task SetDefaultPrinterAsync(Guid printerId);
    Task AssignCategoryPrinterAsync(Guid categoryId, Guid printerId, Guid? templateId);
}

public class LabelPrinterService : ILabelPrinterService
{
    public async Task<bool> TestPrinterConnectionAsync(Guid printerId)
    {
        var printer = await _context.LabelPrinters.FindAsync(printerId);
        if (printer == null) return false;

        try
        {
            switch (printer.PrinterType)
            {
                case LabelPrinterType.Serial:
                    return await TestSerialConnectionAsync(printer.ConnectionString);
                case LabelPrinterType.Network:
                    return await TestNetworkConnectionAsync(printer.ConnectionString);
                case LabelPrinterType.USB:
                    return await TestUsbConnectionAsync(printer.ConnectionString);
                case LabelPrinterType.Windows:
                    return await TestWindowsPrinterAsync(printer.ConnectionString);
                default:
                    return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Printer connection test failed for {PrinterId}", printerId);
            return false;
        }
    }

    public async Task<bool> PrintTestLabelAsync(Guid printerId)
    {
        var printer = await _context.LabelPrinters.FindAsync(printerId);
        if (printer == null) return false;

        var testLabel = GenerateTestLabel(printer);
        return await SendToPrinterAsync(printer, testLabel);
    }

    private string GenerateTestLabel(LabelPrinter printer)
    {
        // Generate test label in appropriate language
        return printer.PrintLanguage switch
        {
            LabelPrintLanguage.ZPL => GenerateZplTestLabel(printer.DefaultLabelSize),
            LabelPrintLanguage.EPL => GenerateEplTestLabel(printer.DefaultLabelSize),
            _ => throw new NotSupportedException($"Print language {printer.PrintLanguage} not supported")
        };
    }
}
```

## Definition of Done
- [x] LabelPrinter entity and database table
- [x] Support for ZPL and EPL print languages
- [x] Serial, Network, USB connection types
- [x] Printer CRUD operations
- [x] Connection testing functionality
- [x] Test label printing
- [x] Category-to-printer assignment
- [x] Default printer setting
- [x] Unit tests passing

## Implementation Summary
**Completed:** January 2026

### Files Created/Modified:
- `src/HospitalityPOS.Core/Entities/LabelPrintingEntities.cs` - LabelPrinter, LabelSize, CategoryPrinterAssignment entities with all printer types and languages
- `src/HospitalityPOS.Core/DTOs/LabelPrintingDtos.cs` - All DTOs for printer management
- `src/HospitalityPOS.Core/Interfaces/ILabelPrinterService.cs` - Service interface with events
- `src/HospitalityPOS.Infrastructure/Services/LabelPrinterService.cs` - Full implementation with:
  - Printer CRUD operations (Create, Read, Update, Delete)
  - Connection testing for Serial, Network, USB, Windows printer types
  - Test label generation in ZPL and EPL formats
  - Category-to-printer assignment management
  - Default printer per store
  - Printer usage statistics
- `tests/HospitalityPOS.Business.Tests/Services/LabelPrinterServiceTests.cs` - Comprehensive unit tests
