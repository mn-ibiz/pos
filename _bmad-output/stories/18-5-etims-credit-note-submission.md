# Story 18.5: eTIMS Credit Note Submission

## Story
**As a** manager,
**I want to** submit credit notes to KRA for voids and returns,
**So that** tax adjustments are properly recorded.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Implementation Summary
**Completed:** January 2026

### Files Created/Modified:
- `src/HospitalityPOS.Infrastructure/Services/EtimsService.cs` - Credit notes with:
  - `GenerateCreditNoteAsync` - Generate credit note from original invoice
  - `SubmitCreditNoteAsync` - Submit to KRA
  - `GenerateCreditNoteNumberAsync` - CN-CU-BRANCH-YYYY-NNNNNN format
  - Linked to original invoice for audit trail

## Epic
**Epic 18: Kenya eTIMS Compliance (MANDATORY)**

## Context
When a receipt is voided or items are returned, KRA requires a credit note to be submitted to reverse the tax liability. Credit notes must reference the original invoice and include a reason code for the reversal.

## Acceptance Criteria

### AC1: Credit Note Generation on Void
**Given** a receipt is voided
**When** processing the void
**Then**:
- Credit note is generated automatically
- References original invoice number
- Includes all items being reversed
- Captures void reason (from predefined list)

### AC2: KRA Credit Note Submission
**Given** credit note is created
**When** submitting to KRA
**Then**:
- Uses eTIMS credit note endpoint
- Includes reason code (01-Void, 02-Return, etc.)
- Receives and stores credit note control code
- Links credit note to original invoice

### AC3: Reconciliation View
**Given** submission is successful
**When** viewing reports
**Then**:
- Credit note appears in eTIMS reconciliation
- Shows alongside original invoice
- Net tax effect is calculated correctly

### AC4: Partial Return Handling
**Given** partial item return
**When** creating credit note
**Then**:
- Only includes returned items
- Calculates correct tax for partial amount
- Original invoice remains linked

## Technical Notes

### Implementation Details
```csharp
public class ETimsCreditNote
{
    public Guid Id { get; set; }
    public string CreditNoteNumber { get; set; }
    public DateTime IssueDate { get; set; }

    // Reference to original
    public string OriginalInvoiceNumber { get; set; }
    public Guid OriginalInvoiceId { get; set; }

    // Reason
    public CreditNoteReason Reason { get; set; }
    public string ReasonDescription { get; set; }

    // Amounts (negative or positive showing reversal)
    public decimal TotalExcludingTax { get; set; }
    public decimal TotalTax { get; set; }
    public decimal TotalAmount { get; set; }

    // eTIMS
    public string? ControlCode { get; set; }
    public ETimsSubmissionStatus Status { get; set; }

    public List<ETimsCreditNoteLine> Lines { get; set; }
}

public enum CreditNoteReason
{
    [Description("01")] Void = 1,
    [Description("02")] Return = 2,
    [Description("03")] PriceAdjustment = 3,
    [Description("04")] QuantityCorrection = 4,
    [Description("09")] Other = 9
}
```

### Credit Note Service
```csharp
public interface ICreditNoteService
{
    Task<ETimsCreditNote> CreateCreditNoteAsync(
        Guid originalReceiptId,
        CreditNoteReason reason,
        List<CreditNoteItem>? partialItems = null);

    Task<SubmissionResult> SubmitCreditNoteAsync(ETimsCreditNote creditNote);
}

public class CreditNoteService : ICreditNoteService
{
    public async Task<ETimsCreditNote> CreateCreditNoteAsync(
        Guid originalReceiptId,
        CreditNoteReason reason,
        List<CreditNoteItem>? partialItems = null)
    {
        var originalReceipt = await _receiptRepository.GetByIdAsync(originalReceiptId);
        var originalInvoice = await _invoiceRepository
            .GetByReceiptIdAsync(originalReceiptId);

        var creditNote = new ETimsCreditNote
        {
            CreditNoteNumber = await GenerateCreditNoteNumberAsync(),
            IssueDate = DateTime.UtcNow,
            OriginalInvoiceNumber = originalInvoice.InvoiceNumber,
            OriginalInvoiceId = originalInvoice.Id,
            Reason = reason,
            ReasonDescription = GetReasonDescription(reason)
        };

        // Create lines from original or partial items
        creditNote.Lines = partialItems != null
            ? CreatePartialLines(originalInvoice, partialItems)
            : CreateFullReversalLines(originalInvoice);

        // Calculate totals
        creditNote.TotalExcludingTax = creditNote.Lines.Sum(l => l.LineTotal);
        creditNote.TotalTax = creditNote.Lines.Sum(l => l.TaxAmount);
        creditNote.TotalAmount = creditNote.TotalExcludingTax + creditNote.TotalTax;

        return creditNote;
    }
}
```

### API Endpoint
- Submit Credit Note: `POST /api/creditnote/submit`

## Dependencies
- Story 18.2: KRA-Compliant Invoice Generation
- Story 18.3: Real-Time eTIMS Submission
- Story 6.6: Receipt Voiding

## Files to Create/Modify
- `HospitalityPOS.Core/Entities/ETimsCreditNote.cs`
- `HospitalityPOS.Core/Entities/ETimsCreditNoteLine.cs`
- `HospitalityPOS.Business/Services/CreditNoteService.cs`
- Modify void workflow to trigger credit note creation
- Database migration for credit note tables

## Testing Requirements
- Unit tests for credit note generation
- Integration tests with eTIMS sandbox
- Tests for full void vs partial return
- Tests for reason code mapping

## Definition of Done
- [ ] Credit note auto-generated on void
- [ ] Submission to KRA working
- [ ] Reason codes properly mapped
- [ ] Partial returns supported
- [ ] Reconciliation report includes credit notes
- [ ] Integration tests passing
- [ ] Code reviewed and approved
