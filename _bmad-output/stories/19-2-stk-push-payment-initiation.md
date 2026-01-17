# Story 19.2: STK Push Payment Initiation

## Story
**As a** cashier,
**I want to** trigger an M-Pesa payment prompt on the customer's phone,
**So that** payment is fast and accurate.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Implementation Summary
**Completed:** January 2026

### Files Created/Modified:
- `src/HospitalityPOS.Infrastructure/Services/MpesaService.cs` - STK Push with:
  - `InitiateSTKPushAsync` - Send payment prompt to customer
  - `NormalizePhoneNumber` - Convert 07XX to 254XX format
  - Password generation (shortcode + passkey + timestamp)
  - Checkout request tracking

## Epic
**Epic 19: M-Pesa Daraja API Integration**

## Context
STK Push (Lipa na M-Pesa) is the preferred payment method as it initiates a secure prompt directly on the customer's phone, reducing errors compared to manual entry. The customer simply enters their M-Pesa PIN to complete the transaction.

## Acceptance Criteria

### AC1: Phone Number Entry
**Given** M-Pesa payment is selected
**When** entering customer phone number
**Then**:
- Input field accepts Kenyan formats (07XXXXXXXX or 254XXXXXXXXX)
- Auto-converts to 254 format for API
- Validates 9-digit number after prefix
- Shows error for invalid numbers

### AC2: STK Push Initiation
**Given** phone number is valid
**When** initiating STK Push
**Then**:
- Customer receives payment prompt within 5 seconds
- Shows exact amount to pay
- Account reference shows receipt number
- Transaction description is clear

### AC3: Waiting State
**Given** STK Push is sent
**When** waiting for response
**Then**:
- POS shows "Waiting for customer to enter PIN..."
- Displays timeout counter (default 60 seconds)
- Cancel button available
- Amount clearly displayed

### AC4: Phone Number Memory
**Given** customer is a loyalty member
**When** initiating M-Pesa payment
**Then**:
- Pre-fills phone number from customer profile
- Allows override if different number needed

## Technical Notes

### Implementation Details
```csharp
public class STKPushRequest
{
    public string BusinessShortCode { get; set; }
    public string Password { get; set; }  // Base64(Shortcode+Passkey+Timestamp)
    public string Timestamp { get; set; }  // yyyyMMddHHmmss
    public string TransactionType { get; set; } = "CustomerPayBillOnline";
    public decimal Amount { get; set; }
    public string PartyA { get; set; }  // Customer phone (254...)
    public string PartyB { get; set; }  // Business shortcode
    public string PhoneNumber { get; set; }  // Customer phone
    public string CallBackURL { get; set; }
    public string AccountReference { get; set; }  // Receipt number
    public string TransactionDesc { get; set; }
}

public class STKPushResponse
{
    public string MerchantRequestID { get; set; }
    public string CheckoutRequestID { get; set; }
    public string ResponseCode { get; set; }
    public string ResponseDescription { get; set; }
    public string CustomerMessage { get; set; }
}

public interface IMPesaPaymentService
{
    Task<STKPushResult> InitiateSTKPushAsync(string phoneNumber, decimal amount, string reference);
    Task<PaymentStatus> CheckStatusAsync(string checkoutRequestId);
    event EventHandler<PaymentCallbackEventArgs> OnPaymentCallback;
}
```

### STK Push Service
```csharp
public class MPesaPaymentService : IMPesaPaymentService
{
    public async Task<STKPushResult> InitiateSTKPushAsync(
        string phoneNumber, decimal amount, string reference)
    {
        var config = await _configService.GetConfigurationAsync();
        var token = await _authService.GetAccessTokenAsync();
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

        var password = Convert.ToBase64String(Encoding.UTF8.GetBytes(
            $"{config.BusinessShortcode}{Decrypt(config.PasskeyEncrypted)}{timestamp}"));

        var request = new STKPushRequest
        {
            BusinessShortCode = config.BusinessShortcode,
            Password = password,
            Timestamp = timestamp,
            Amount = Math.Round(amount),  // M-Pesa doesn't accept decimals
            PartyA = NormalizePhoneNumber(phoneNumber),
            PartyB = config.BusinessShortcode,
            PhoneNumber = NormalizePhoneNumber(phoneNumber),
            CallBackURL = config.CallbackUrl,
            AccountReference = reference,
            TransactionDesc = $"Payment for {reference}"
        };

        var response = await PostAsync<STKPushRequest, STKPushResponse>(
            GetSTKPushUrl(config.Environment),
            request,
            token);

        return new STKPushResult
        {
            Success = response.ResponseCode == "0",
            MerchantRequestId = response.MerchantRequestID,
            CheckoutRequestId = response.CheckoutRequestID,
            Message = response.CustomerMessage
        };
    }

    private string NormalizePhoneNumber(string phone)
    {
        phone = phone.Replace(" ", "").Replace("-", "");
        if (phone.StartsWith("0"))
            return "254" + phone.Substring(1);
        if (phone.StartsWith("+254"))
            return phone.Substring(1);
        return phone;
    }
}
```

### UI Flow
```
┌────────────────────────────────────────────────┐
│           M-PESA PAYMENT                        │
├────────────────────────────────────────────────┤
│                                                 │
│  Amount: KSh 1,500.00                          │
│                                                 │
│  Phone Number:                                  │
│  ┌──────────────────────────────────────────┐  │
│  │ 0712 345 678                             │  │
│  └──────────────────────────────────────────┘  │
│                                                 │
│  ┌────────────────┐  ┌────────────────────┐   │
│  │    CANCEL      │  │   SEND STK PUSH    │   │
│  └────────────────┘  └────────────────────┘   │
│                                                 │
└────────────────────────────────────────────────┘

         ↓ After STK Push sent ↓

┌────────────────────────────────────────────────┐
│         WAITING FOR M-PESA                      │
├────────────────────────────────────────────────┤
│                                                 │
│            📱 Check your phone                  │
│                                                 │
│  A payment prompt has been sent to:            │
│  0712 345 678                                  │
│                                                 │
│  Amount: KSh 1,500.00                          │
│                                                 │
│           ⏱️ Timeout in: 45s                   │
│                                                 │
│  ┌────────────────────────────────────────┐   │
│  │              CANCEL                     │   │
│  └────────────────────────────────────────┘   │
│                                                 │
└────────────────────────────────────────────────┘
```

## Dependencies
- Story 19.1: Daraja API Configuration
- Epic 7: Payment Processing
- Epic 6: Receipt Management

## Files to Create/Modify
- `HospitalityPOS.Core/DTOs/STKPushRequest.cs`
- `HospitalityPOS.Core/DTOs/STKPushResponse.cs`
- `HospitalityPOS.Business/Services/MPesaPaymentService.cs`
- `HospitalityPOS.WPF/ViewModels/POS/MPesaPaymentViewModel.cs`
- `HospitalityPOS.WPF/Views/POS/MPesaPaymentDialog.xaml`

## Testing Requirements
- Unit tests for phone number normalization
- Integration tests with Daraja sandbox
- UI tests for payment flow
- Tests for timeout handling

## Definition of Done
- [ ] Phone number input with validation
- [ ] STK Push initiated successfully
- [ ] Waiting state UI implemented
- [ ] Timeout countdown working
- [ ] Cancel functionality working
- [ ] Sandbox integration tested
- [ ] Unit tests passing
- [ ] Code reviewed and approved
