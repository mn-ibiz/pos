# Story 19.3: M-Pesa Payment Confirmation

## Story
**As the** system,
**I want to** receive and process M-Pesa payment callbacks,
**So that** transactions are confirmed automatically.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Implementation Summary
**Completed:** January 2026

### Files Created/Modified:
- `src/HospitalityPOS.Infrastructure/Services/MpesaService.cs` - Callback processing with:
  - `ProcessCallbackAsync` - Handle STK Push callback
  - Result code handling (0=success, 1=insufficient, 1032=cancelled, 1037=timeout)
  - M-Pesa receipt number extraction
  - Payment status updates

## Epic
**Epic 19: M-Pesa Daraja API Integration**

## Context
After a customer enters their M-Pesa PIN, Safaricom sends a callback to our configured URL with the payment result. The system must process this callback to confirm or reject the transaction in real-time.

## Acceptance Criteria

### AC1: Callback Processing
**Given** customer enters M-Pesa PIN
**When** callback is received
**Then**:
- System processes ResultCode and ResultDesc
- Extracts M-Pesa receipt number
- Matches to pending transaction by CheckoutRequestID
- Updates payment status immediately

### AC2: Successful Payment Handling
**Given** payment is successful (ResultCode = 0)
**When** confirming payment
**Then**:
- Receipt is marked as paid
- M-Pesa receipt number stored
- UI updates to show "Payment Successful"
- Receipt can be printed with M-Pesa reference

### AC3: Failed Payment Handling
**Given** payment fails or times out
**When** handling failure
**Then**:
- Displays clear error message to cashier
- Allows retry with same or different number
- Allows switch to alternative payment method
- Logs failure reason for troubleshooting

### AC4: Real-time UI Update
**Given** callback is processed
**When** result is available
**Then**:
- POS waiting screen updates immediately
- No page refresh required (SignalR/polling)
- Sound notification on success/failure

## Technical Notes

### Callback Payload
```csharp
public class MPesaCallback
{
    public CallbackBody Body { get; set; }
}

public class CallbackBody
{
    public StkCallback StkCallback { get; set; }
}

public class StkCallback
{
    public string MerchantRequestID { get; set; }
    public string CheckoutRequestID { get; set; }
    public int ResultCode { get; set; }
    public string ResultDesc { get; set; }
    public CallbackMetadata CallbackMetadata { get; set; }
}

public class CallbackMetadata
{
    public List<CallbackItem> Item { get; set; }
}

// Result codes:
// 0 = Success
// 1 = Insufficient balance
// 1032 = Request cancelled by user
// 1037 = DS timeout (user didn't respond)
```

### Callback Handler
```csharp
[ApiController]
[Route("api/mpesa")]
public class MPesaCallbackController : ControllerBase
{
    private readonly IMPesaPaymentService _paymentService;
    private readonly IHubContext<PaymentHub> _hubContext;

    [HttpPost("callback")]
    public async Task<IActionResult> HandleCallback([FromBody] MPesaCallback callback)
    {
        var stkCallback = callback.Body.StkCallback;

        var payment = await _paymentService.ProcessCallbackAsync(
            stkCallback.CheckoutRequestID,
            stkCallback.ResultCode,
            stkCallback.ResultDesc,
            ExtractMetadata(stkCallback.CallbackMetadata));

        // Notify waiting POS terminal via SignalR
        await _hubContext.Clients
            .Group($"payment_{stkCallback.CheckoutRequestID}")
            .SendAsync("PaymentResult", new
            {
                Success = stkCallback.ResultCode == 0,
                Message = stkCallback.ResultDesc,
                MpesaReference = payment?.MpesaReceiptNumber
            });

        return Ok(new { ResultCode = 0, ResultDesc = "Success" });
    }

    private PaymentMetadata ExtractMetadata(CallbackMetadata metadata)
    {
        var items = metadata?.Item ?? new List<CallbackItem>();
        return new PaymentMetadata
        {
            MpesaReceiptNumber = items.FirstOrDefault(i => i.Name == "MpesaReceiptNumber")?.Value?.ToString(),
            Amount = decimal.Parse(items.FirstOrDefault(i => i.Name == "Amount")?.Value?.ToString() ?? "0"),
            TransactionDate = items.FirstOrDefault(i => i.Name == "TransactionDate")?.Value?.ToString(),
            PhoneNumber = items.FirstOrDefault(i => i.Name == "PhoneNumber")?.Value?.ToString()
        };
    }
}
```

### Payment Update Service
```csharp
public async Task<MPesaTransaction> ProcessCallbackAsync(
    string checkoutRequestId,
    int resultCode,
    string resultDesc,
    PaymentMetadata metadata)
{
    var transaction = await _repository.GetByCheckoutRequestIdAsync(checkoutRequestId);
    if (transaction == null)
    {
        _logger.LogWarning("Callback received for unknown checkout: {CheckoutRequestId}",
            checkoutRequestId);
        return null;
    }

    transaction.ResultCode = resultCode;
    transaction.ResultDesc = resultDesc;
    transaction.CompletedAt = DateTime.UtcNow;

    if (resultCode == 0)
    {
        transaction.Status = MPesaStatus.Completed;
        transaction.MpesaReceiptNumber = metadata.MpesaReceiptNumber;
        transaction.PhoneNumberUsed = metadata.PhoneNumber;

        // Update the receipt payment status
        await _receiptService.ConfirmPaymentAsync(
            transaction.ReceiptId,
            PaymentMethod.MPesa,
            metadata.MpesaReceiptNumber);
    }
    else
    {
        transaction.Status = resultCode switch
        {
            1 => MPesaStatus.InsufficientFunds,
            1032 => MPesaStatus.Cancelled,
            1037 => MPesaStatus.TimedOut,
            _ => MPesaStatus.Failed
        };
    }

    await _repository.UpdateAsync(transaction);
    return transaction;
}
```

## Dependencies
- Story 19.1: Daraja API Configuration
- Story 19.2: STK Push Payment Initiation
- Epic 7: Payment Processing

## Files to Create/Modify
- `HospitalityPOS.API/Controllers/MPesaCallbackController.cs`
- `HospitalityPOS.Core/DTOs/MPesaCallback.cs`
- `HospitalityPOS.Business/Services/MPesaPaymentService.cs` (ProcessCallback)
- `HospitalityPOS.WPF/Hubs/PaymentHub.cs`
- Configure callback URL endpoint

## Testing Requirements
- Unit tests for callback processing
- Integration tests with mock callbacks
- Tests for all result codes
- Tests for SignalR notification

## Definition of Done
- [ ] Callback endpoint implemented and secured
- [ ] All result codes handled correctly
- [ ] Receipt updated on success
- [ ] UI updates in real-time via SignalR
- [ ] Error messages clear and actionable
- [ ] Integration tests passing
- [ ] Code reviewed and approved
