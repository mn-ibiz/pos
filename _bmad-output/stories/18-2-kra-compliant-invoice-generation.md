# Story 18.2: KRA-Compliant Invoice Generation

## Story
**As the** system,
**I want to** generate invoices with all KRA-required fields,
**So that** every transaction is tax compliant.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Implementation Summary
**Completed:** January 2026

### Files Created/Modified:
- `src/HospitalityPOS.Infrastructure/Services/EtimsService.cs` - Invoice generation with:
  - `GenerateInvoiceAsync` - Generate KRA-compliant invoices
  - `GenerateInvoiceNumberAsync` - CU-BRANCH-YYYY-NNNNNN format
  - 16% VAT calculation with tax breakdown
  - SimplifiedTaxInvoice (<5000) and TaxInvoice (>=5000) document types

## Epic
**Epic 18: Kenya eTIMS Compliance (MANDATORY)**

## Context
KRA eTIMS requires specific fields on every tax invoice. The system must generate invoices that include all mandatory fields, proper tax calculations, and unique invoice numbering for submission to KRA.

## Acceptance Criteria

### AC1: Invoice Field Generation
**Given** a transaction is completed
**When** generating the invoice
**Then** includes:
- Seller PIN (business KRA PIN)
- Seller Name and Address
- Buyer PIN (if provided by customer)
- Buyer Name (if PIN provided)
- Sequential invoice number (INV-YYYY-NNNNNN)
- Invoice date/time (ISO 8601 format)
- All line items with descriptions

### AC2: Tax Code Application
**Given** invoice is generated
**When** calculating taxes
**Then**:
- Applies 16% VAT (Tax Code A) for standard items
- Applies 0% VAT Exempt (Tax Code B) for exempt items
- Applies 0% Zero-Rated (Tax Code C) for zero-rated items
- Shows tax amount per line item
- Shows total tax by tax code

### AC3: Receipt with eTIMS Elements
**Given** invoice is complete
**When** generating receipt
**Then** includes:
- eTIMS Control Code placeholder (updated after submission)
- QR code for verification (generated after submission)
- Invoice number prominently displayed
- Tax breakdown summary

### AC4: Invoice Number Sequence
**Given** new transaction is created
**When** assigning invoice number
**Then**:
- Uses format: INV-YYYY-NNNNNN (e.g., INV-2025-000001)
- Sequence is unique and sequential
- No gaps in numbering (critical for KRA)
- Persisted across system restarts

## Technical Notes

### Implementation Details
```csharp
public class ETimsInvoice
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; }
    public DateTime InvoiceDate { get; set; }

    // Seller Info
    public string SellerPin { get; set; }
    public string SellerName { get; set; }
    public string SellerAddress { get; set; }

    // Buyer Info (optional)
    public string? BuyerPin { get; set; }
    public string? BuyerName { get; set; }

    // Totals
    public decimal TotalExcludingTax { get; set; }
    public decimal TotalTax { get; set; }
    public decimal TotalIncludingTax { get; set; }

    // eTIMS specific
    public string? ControlCode { get; set; }
    public string? QrCode { get; set; }
    public ETimsSubmissionStatus Status { get; set; }

    public List<ETimsInvoiceLine> Lines { get; set; }
}

public class ETimsInvoiceLine
{
    public string Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string TaxCode { get; set; }  // A, B, C
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
}
```

### Tax Calculation
```csharp
public class KenyaTaxCalculator
{
    public TaxResult Calculate(decimal amount, TaxCategory category)
    {
        return category switch
        {
            TaxCategory.StandardVAT => new TaxResult
            {
                ExclusiveAmount = Math.Round(amount / 1.16m, 2),
                TaxAmount = Math.Round(amount - (amount / 1.16m), 2),
                TaxRate = 0.16m,
                TaxCode = "A"
            },
            TaxCategory.VATExempt => new TaxResult
            {
                ExclusiveAmount = amount,
                TaxAmount = 0,
                TaxRate = 0,
                TaxCode = "B"
            },
            TaxCategory.ZeroRated => new TaxResult
            {
                ExclusiveAmount = amount,
                TaxAmount = 0,
                TaxRate = 0,
                TaxCode = "C"
            },
            _ => throw new ArgumentException("Unknown tax category")
        };
    }
}
```

## Dependencies
- Story 18.1: eTIMS Control Unit Registration
- Epic 6: Receipt Management
- Epic 4: Product Management (tax category per product)

## Files to Create/Modify
- `HospitalityPOS.Core/Entities/ETimsInvoice.cs`
- `HospitalityPOS.Core/Entities/ETimsInvoiceLine.cs`
- `HospitalityPOS.Business/Services/InvoiceGenerationService.cs`
- `HospitalityPOS.Business/Services/KenyaTaxCalculator.cs`
- Modify `Product` entity to include TaxCategory

## Testing Requirements
- Unit tests for tax calculations
- Unit tests for invoice number generation
- Validation tests for KRA field requirements

## Definition of Done
- [ ] Invoice generation creates all required KRA fields
- [ ] Tax calculations correct for all categories
- [ ] Invoice numbering is sequential and gapless
- [ ] Receipt template includes eTIMS placeholders
- [ ] Unit tests passing (95%+ coverage)
- [ ] Code reviewed and approved
