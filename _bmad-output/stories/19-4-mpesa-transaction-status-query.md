# Story 19.4: M-Pesa Transaction Status Query

## Story
**As a** cashier,
**I want to** check the status of pending M-Pesa payments,
**So that** I can resolve stuck transactions.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Implementation Summary
**Completed:** January 2026

### Files Created/Modified:
- `src/HospitalityPOS.Infrastructure/Services/MpesaService.cs` - Status query with:
  - `QueryTransactionStatusAsync` - Check pending payment status
  - Auto-query after 30 seconds without callback
  - Manual query support for stuck transactions
  - Status mapping to payment outcomes

## Epic
**Epic 19: M-Pesa Daraja API Integration**

## Context
Sometimes callbacks may be delayed or lost due to network issues. The cashier needs the ability to manually query the transaction status from Safaricom to resolve pending payments without requiring the customer to pay again.

## Acceptance Criteria

### AC1: Automatic Status Check
**Given** an STK Push was initiated
**When** no callback received after 30 seconds
**Then**:
- System automatically queries transaction status
- Displays result to cashier
- Updates payment status accordingly

### AC2: Manual Status Query
**Given** payment is stuck in pending
**When** cashier triggers status query
**Then**:
- Shows loading indicator
- Queries Daraja Query API
- Displays result immediately

### AC3: Success Result Handling
**Given** status query is run
**When** response indicates success
**Then**:
- Transaction is marked as paid
- M-Pesa receipt number retrieved
- Receipt can proceed to print
- No duplicate payment risk

### AC4: Failure Result Handling
**Given** status indicates failure
**When** viewing result
**Then**:
- Shows failure reason clearly
- Clears pending status
- Enables retry or alternative payment

## Technical Notes

### Query API Request/Response
```csharp
public class StatusQueryRequest
{
    public string BusinessShortCode { get; set; }
    public string Password { get; set; }
    public string Timestamp { get; set; }
    public string CheckoutRequestID { get; set; }
}

public class StatusQueryResponse
{
    public string ResponseCode { get; set; }
    public string ResponseDescription { get; set; }
    public string MerchantRequestID { get; set; }
    public string CheckoutRequestID { get; set; }
    public string ResultCode { get; set; }
    public string ResultDesc { get; set; }
}
```

### Status Query Service
```csharp
public class MPesaPaymentService : IMPesaPaymentService
{
    public async Task<PaymentStatus> CheckStatusAsync(string checkoutRequestId)
    {
        var config = await _configService.GetConfigurationAsync();
        var token = await _authService.GetAccessTokenAsync();
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

        var password = Convert.ToBase64String(Encoding.UTF8.GetBytes(
            $"{config.BusinessShortcode}{Decrypt(config.PasskeyEncrypted)}{timestamp}"));

        var request = new StatusQueryRequest
        {
            BusinessShortCode = config.BusinessShortcode,
            Password = password,
            Timestamp = timestamp,
            CheckoutRequestID = checkoutRequestId
        };

        var response = await PostAsync<StatusQueryRequest, StatusQueryResponse>(
            GetQueryUrl(config.Environment),
            request,
            token);

        if (response.ResponseCode == "0")
        {
            // Query was successful, check the actual transaction result
            return MapToPaymentStatus(response.ResultCode, response.ResultDesc);
        }

        return new PaymentStatus
        {
            IsResolved = false,
            Message = response.ResponseDescription
        };
    }

    private PaymentStatus MapToPaymentStatus(string resultCode, string resultDesc)
    {
        return resultCode switch
        {
            "0" => new PaymentStatus
            {
                IsResolved = true,
                IsSuccessful = true,
                Message = "Payment confirmed"
            },
            "1032" => new PaymentStatus
            {
                IsResolved = true,
                IsSuccessful = false,
                Message = "Payment cancelled by user"
            },
            "1037" => new PaymentStatus
            {
                IsResolved = true,
                IsSuccessful = false,
                Message = "Customer did not respond"
            },
            _ => new PaymentStatus
            {
                IsResolved = true,
                IsSuccessful = false,
                Message = resultDesc
            }
        };
    }
}
```

### Auto-Query Background Task
```csharp
public class PendingPaymentChecker
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _pendingChecks = new();

    public void StartMonitoring(string checkoutRequestId, Action<PaymentStatus> onResult)
    {
        var cts = new CancellationTokenSource();
        _pendingChecks[checkoutRequestId] = cts;

        Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(30), cts.Token);

            if (!cts.Token.IsCancellationRequested)
            {
                var status = await _paymentService.CheckStatusAsync(checkoutRequestId);
                onResult(status);
            }
        }, cts.Token);
    }

    public void StopMonitoring(string checkoutRequestId)
    {
        if (_pendingChecks.TryRemove(checkoutRequestId, out var cts))
        {
            cts.Cancel();
        }
    }
}
```

## Dependencies
- Story 19.1: Daraja API Configuration
- Story 19.2: STK Push Payment Initiation

## Files to Create/Modify
- `HospitalityPOS.Core/DTOs/StatusQueryRequest.cs`
- `HospitalityPOS.Core/DTOs/StatusQueryResponse.cs`
- `HospitalityPOS.Business/Services/MPesaPaymentService.cs` (CheckStatus)
- `HospitalityPOS.Business/Services/PendingPaymentChecker.cs`
- `HospitalityPOS.WPF/ViewModels/POS/MPesaPaymentViewModel.cs` (Query button)

## Testing Requirements
- Unit tests for status query logic
- Integration tests with Daraja sandbox
- Tests for timeout scenarios
- Tests for all result codes

## Definition of Done
- [ ] Automatic status check after 30s
- [ ] Manual query button available
- [ ] All result codes handled
- [ ] Payment updated on success
- [ ] Clear error messages on failure
- [ ] Unit tests passing
- [ ] Code reviewed and approved
