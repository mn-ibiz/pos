# Story 19.5: Manual M-Pesa Entry Fallback

## Story
**As a** cashier,
**I want to** manually enter M-Pesa transaction details,
**So that** payments can be recorded when STK Push fails.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Implementation Summary
**Completed:** January 2026

### Files Created/Modified:
- `src/HospitalityPOS.Infrastructure/Services/MpesaService.cs` - Manual entry with:
  - `RecordManualPaymentAsync` - Record manual M-Pesa code
  - `ValidateTransactionCode` - Check [A-Z][A-Z0-9]{9} format
  - `IsDuplicateCodeAsync` - Prevent duplicate codes
  - Flagging for reconciliation verification

## Epic
**Epic 19: M-Pesa Daraja API Integration**

## Context
When STK Push is unavailable (API issues, customer's phone problems, or preference), customers may pay directly via M-Pesa (Paybill/Buy Goods) and provide the transaction code. The system needs to accept manual entry while flagging these for later reconciliation.

## Acceptance Criteria

### AC1: Manual Entry Option
**Given** STK Push is unavailable or fails
**When** selecting manual entry
**Then**:
- Option clearly available as fallback
- Input field for M-Pesa transaction code
- Amount confirmation required

### AC2: Transaction Code Validation
**Given** transaction code is entered
**When** validating format
**Then**:
- Checks 10-character alphanumeric pattern
- Pattern: [A-Z][A-Z0-9]{9} (e.g., QJK7H8L9M0)
- Shows error for invalid format
- Prevents duplicate codes

### AC3: Payment Recording
**Given** valid code entered
**When** completing payment
**Then**:
- Transaction recorded with manual entry flag
- Receipt can be printed
- Flagged for reconciliation verification
- Audit log entry created

### AC4: Reconciliation Report
**Given** manual entries exist
**When** running reconciliation
**Then**:
- Lists all manual M-Pesa entries
- Shows verification status (Pending/Verified/Mismatch)
- Allows marking as verified
- Highlights unverified entries

## Technical Notes

### Implementation Details
```csharp
public class ManualMPesaEntry
{
    public Guid Id { get; set; }
    public Guid ReceiptId { get; set; }
    public string TransactionCode { get; set; }
    public decimal Amount { get; set; }
    public string PhoneNumber { get; set; }  // Optional
    public DateTime EnteredAt { get; set; }
    public Guid EnteredByUserId { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public Guid? VerifiedByUserId { get; set; }
    public string Notes { get; set; }
}

public class ManualMPesaValidator
{
    private static readonly Regex TransactionCodePattern =
        new Regex(@"^[A-Z][A-Z0-9]{9}$", RegexOptions.Compiled);

    public ValidationResult Validate(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return ValidationResult.Error("Transaction code is required");

        code = code.ToUpperInvariant().Trim();

        if (!TransactionCodePattern.IsMatch(code))
            return ValidationResult.Error("Invalid M-Pesa code format. Expected: 10 characters starting with a letter");

        return ValidationResult.Success(code);
    }
}
```

### Manual Entry Service
```csharp
public interface IManualMPesaService
{
    Task<Result> RecordManualPaymentAsync(ManualMPesaEntry entry);
    Task<bool> IsDuplicateCodeAsync(string transactionCode);
    Task<List<ManualMPesaEntry>> GetPendingVerificationAsync(DateTime fromDate);
    Task MarkAsVerifiedAsync(Guid entryId, Guid verifiedByUserId);
}

public class ManualMPesaService : IManualMPesaService
{
    public async Task<Result> RecordManualPaymentAsync(ManualMPesaEntry entry)
    {
        // Validate code format
        var validation = _validator.Validate(entry.TransactionCode);
        if (!validation.IsValid)
            return Result.Failure(validation.Error);

        // Check for duplicates
        if (await IsDuplicateCodeAsync(entry.TransactionCode))
            return Result.Failure("This transaction code has already been used");

        entry.TransactionCode = validation.NormalizedValue;
        entry.EnteredAt = DateTime.UtcNow;
        entry.IsVerified = false;

        await _repository.AddAsync(entry);

        // Update receipt as paid
        await _receiptService.ConfirmPaymentAsync(
            entry.ReceiptId,
            PaymentMethod.MPesaManual,
            entry.TransactionCode);

        // Audit log
        await _auditService.LogAsync(new AuditEntry
        {
            Action = "ManualMPesaEntry",
            EntityType = "Payment",
            EntityId = entry.Id,
            UserId = entry.EnteredByUserId,
            Details = $"Manual M-Pesa code: {entry.TransactionCode}, Amount: {entry.Amount}"
        });

        return Result.Success();
    }
}
```

### UI Flow
```
┌────────────────────────────────────────────────┐
│       MANUAL M-PESA ENTRY                       │
├────────────────────────────────────────────────┤
│                                                 │
│  Amount Due: KSh 1,500.00                      │
│                                                 │
│  ⚠️ Use this when STK Push is unavailable      │
│  Customer must complete payment via M-Pesa     │
│  first and provide the transaction code.       │
│                                                 │
│  M-Pesa Transaction Code:                      │
│  ┌──────────────────────────────────────────┐  │
│  │ QJK7H8L9M0                               │  │
│  └──────────────────────────────────────────┘  │
│                                                 │
│  Phone Number (optional):                       │
│  ┌──────────────────────────────────────────┐  │
│  │ 0712 345 678                             │  │
│  └──────────────────────────────────────────┘  │
│                                                 │
│  ⓘ This entry will be flagged for            │
│    verification during reconciliation.        │
│                                                 │
│  ┌────────────────┐  ┌────────────────────┐   │
│  │    CANCEL      │  │  RECORD PAYMENT    │   │
│  └────────────────┘  └────────────────────┘   │
│                                                 │
└────────────────────────────────────────────────┘
```

### Reconciliation UI
```
┌────────────────────────────────────────────────────────────────────┐
│              MANUAL M-PESA RECONCILIATION                           │
├────────────────────────────────────────────────────────────────────┤
│  Date Range: [28/12/2025] - [28/12/2025]     [Search M-Pesa API]   │
├────────────────────────────────────────────────────────────────────┤
│  Code       │ Amount   │ Receipt   │ Cashier │ Status    │ Action │
│─────────────┼──────────┼───────────┼─────────┼───────────┼────────│
│ QJK7H8L9M0 │ 1,500.00 │ RCP-00123 │ Jane    │ ✓ Verified│ View   │
│ ABC1234567 │ 2,300.00 │ RCP-00145 │ John    │ ⏳ Pending │ Verify │
│ XYZ9876543 │ 850.00   │ RCP-00167 │ Mary    │ ⚠ No Match│ Review │
└────────────────────────────────────────────────────────────────────┘
```

## Dependencies
- Story 19.1: Daraja API Configuration
- Epic 7: Payment Processing
- Epic 10: Reporting (Reconciliation)

## Files to Create/Modify
- `HospitalityPOS.Core/Entities/ManualMPesaEntry.cs`
- `HospitalityPOS.Business/Services/ManualMPesaService.cs`
- `HospitalityPOS.Business/Validators/ManualMPesaValidator.cs`
- `HospitalityPOS.WPF/ViewModels/POS/ManualMPesaViewModel.cs`
- `HospitalityPOS.WPF/Views/POS/ManualMPesaDialog.xaml`
- `HospitalityPOS.WPF/Views/Reports/MPesaReconciliationView.xaml`
- Database migration for ManualMPesaEntry table

## Testing Requirements
- Unit tests for transaction code validation
- Unit tests for duplicate detection
- Tests for reconciliation workflow
- UI tests for manual entry flow

## Definition of Done
- [ ] Manual entry option available
- [ ] Transaction code validation working
- [ ] Duplicate detection working
- [ ] Entries flagged for verification
- [ ] Reconciliation report available
- [ ] Audit logging complete
- [ ] Unit tests passing
- [ ] Code reviewed and approved
