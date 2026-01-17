# Story 39.2: M-Pesa Daraja API Integration (STK Push)

Status: Done

## Story

As a **cashier processing M-Pesa payments**,
I want **to initiate M-Pesa payment with just the customer's phone number and have it auto-confirm**,
so that **transactions are faster, error-free, and I don't need to manually verify payment codes**.

## Business Context

**CRITICAL - COMPETITIVE DISADVANTAGE**

Current implementation requires manual entry of M-Pesa transaction codes:
- **Slow:** Adds 30-60 seconds per transaction
- **Error-prone:** Typos in reference codes
- **Unverifiable:** Cannot confirm payment actually received
- **Uncompetitive:** SimbaPOS, Uzalynx, FortyPOS all have auto-confirmation

**Market Reality:** M-Pesa is the dominant payment method in Kenya. Without STK Push:
- Longer checkout queues
- Higher error rates
- Potential fraud (fake codes)
- Customer frustration

## Acceptance Criteria

### AC1: Daraja API Configuration
- [ ] Admin can configure M-Pesa Till Number or Paybill
- [ ] Consumer Key and Consumer Secret stored securely (encrypted)
- [ ] Passkey for STK Push configured
- [ ] Callback URL configured and tested
- [ ] Sandbox/Production environment toggle

### AC2: STK Push Payment Initiation
- [ ] Cashier enters customer phone number (254XXXXXXXXX format)
- [ ] System validates phone number format
- [ ] STK Push request sent to Daraja API
- [ ] Customer receives payment prompt on phone within 5 seconds
- [ ] POS shows "Waiting for customer confirmation..." status

### AC3: Real-Time Payment Confirmation
- [ ] Callback received when customer completes payment
- [ ] Payment automatically matched to pending receipt
- [ ] Receipt status updated to "Settled" on success
- [ ] M-Pesa receipt number stored with payment
- [ ] Success notification shown on POS

### AC4: Payment Failure Handling
- [ ] Timeout after 60 seconds shows clear message
- [ ] "Payment cancelled" handled gracefully
- [ ] "Insufficient funds" shows appropriate message
- [ ] Retry option available without re-entering details
- [ ] Fallback to manual entry if STK fails

### AC5: Transaction Query
- [ ] Query payment status for pending transactions
- [ ] Reconcile stuck transactions
- [ ] Manual status check button for cashier

### AC6: M-Pesa Reports
- [ ] Daily M-Pesa transaction report
- [ ] Filter by status (Success, Failed, Pending)
- [ ] Reconciliation report with M-Pesa receipt numbers
- [ ] Export to Excel

## Tasks / Subtasks

- [ ] **Task 1: Database Schema for M-Pesa** (AC: 1, 2, 3)
  - [ ] 1.1 Create MpesaConfiguration table (ShortCode, ConsumerKey, ConsumerSecret, PassKey, CallbackUrl, Environment)
  - [ ] 1.2 Create MpesaTransactions table (CheckoutRequestId, PhoneNumber, Amount, Status, MpesaReceiptNumber)
  - [ ] 1.3 Create migration for M-Pesa tables
  - [ ] 1.4 Add encrypted storage for API credentials
  - [ ] 1.5 Entity configuration tests

- [ ] **Task 2: Daraja API Client** (AC: 1, 2, 5)
  - [ ] 2.1 Create IMpesaService interface
  - [ ] 2.2 Implement OAuth token generation (access_token from credentials)
  - [ ] 2.3 Implement STK Push initiation (Lipa Na M-Pesa Online)
  - [ ] 2.4 Implement transaction status query
  - [ ] 2.5 Handle token expiry and refresh
  - [ ] 2.6 Comprehensive logging of all API calls
  - [ ] 2.7 Unit tests with mocked responses

- [ ] **Task 3: Callback Handler** (AC: 3, 4)
  - [ ] 3.1 Create callback endpoint (if REST API exists) OR polling mechanism
  - [ ] 3.2 Parse callback JSON response
  - [ ] 3.3 Match callback to pending transaction by CheckoutRequestId
  - [ ] 3.4 Update payment and receipt status
  - [ ] 3.5 Handle duplicate callbacks idempotently
  - [ ] 3.6 Integration tests for callback flow

- [ ] **Task 4: Payment Processing Integration** (AC: 2, 3, 4)
  - [ ] 4.1 Add M-Pesa STK option to PaymentView
  - [ ] 4.2 Create phone number input with validation (254XXXXXXXXX)
  - [ ] 4.3 Show payment progress indicator
  - [ ] 4.4 Implement timeout handling (60 seconds)
  - [ ] 4.5 Update PaymentViewModel for async M-Pesa flow
  - [ ] 4.6 Add retry/fallback to manual entry
  - [ ] 4.7 UI tests for payment flow

- [ ] **Task 5: M-Pesa Configuration UI** (AC: 1)
  - [ ] 5.1 Create MpesaSettingsView.xaml
  - [ ] 5.2 Create MpesaSettingsViewModel
  - [ ] 5.3 Add credential input with show/hide toggle
  - [ ] 5.4 Add sandbox/production toggle
  - [ ] 5.5 Add "Test Connection" button
  - [ ] 5.6 Add "Send Test STK" button

- [ ] **Task 6: M-Pesa Reports** (AC: 6)
  - [ ] 6.1 Add M-Pesa transactions to ReportingService
  - [ ] 6.2 Create MpesaTransactionReport view
  - [ ] 6.3 Add date range and status filters
  - [ ] 6.4 Add Excel export functionality

## Dev Notes

### Daraja API Integration Flow

```
[Cashier selects M-Pesa]
    → [Enter customer phone: 254712345678]
    → [Initiate STK Push]
        ↓
[Daraja API sends push to customer phone]
        ↓
[Customer sees: "Enter M-Pesa PIN to pay KSh X to BUSINESS_NAME"]
    → [Customer enters PIN] → [Payment processed]
        ↓
[Callback received at our endpoint]
    → [Match to CheckoutRequestId]
    → [Update receipt status = Settled]
    → [Store MpesaReceiptNumber]
        ↓
[POS shows: "Payment Successful! Ref: XXXXXXXXXX"]
```

### Daraja API Endpoints

```
Base URL (Sandbox): https://sandbox.safaricom.co.ke
Base URL (Production): https://api.safaricom.co.ke

1. OAuth Token: POST /oauth/v1/generate?grant_type=client_credentials
   - Basic Auth with Consumer Key:Consumer Secret
   - Returns: access_token (valid 1 hour)

2. STK Push: POST /mpesa/stkpush/v1/processrequest
   - Headers: Authorization: Bearer {access_token}
   - Body: {
       BusinessShortCode, Password, Timestamp, TransactionType,
       Amount, PartyA (phone), PartyB (shortcode), PhoneNumber,
       CallBackURL, AccountReference, TransactionDesc
     }

3. Query Status: POST /mpesa/stkpushquery/v1/query
   - Body: { BusinessShortCode, Password, Timestamp, CheckoutRequestID }
```

### Database Schema (from Gap Analysis)

```sql
CREATE TABLE MpesaConfiguration (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ShortCode NVARCHAR(20) NOT NULL, -- Till or Paybill
    ShortCodeType NVARCHAR(10) NOT NULL, -- Till, Paybill
    ConsumerKey NVARCHAR(100) NOT NULL,
    ConsumerSecret NVARCHAR(100) NOT NULL,
    PassKey NVARCHAR(200), -- For STK Push
    CallbackUrl NVARCHAR(500) NOT NULL,
    Environment NVARCHAR(20) DEFAULT 'sandbox',
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

CREATE TABLE MpesaTransactions (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PaymentId INT FOREIGN KEY REFERENCES Payments(Id),
    CheckoutRequestId NVARCHAR(100),
    MerchantRequestId NVARCHAR(100),
    PhoneNumber NVARCHAR(20) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    TransactionStatus NVARCHAR(20) DEFAULT 'Initiated',
    MpesaReceiptNumber NVARCHAR(50),
    TransactionDate DATETIME2,
    ResultCode INT,
    ResultDesc NVARCHAR(200),
    InitiatedAt DATETIME2 DEFAULT GETUTCDATE(),
    CompletedAt DATETIME2
);
```

### Architecture Compliance

- **Layer:** Infrastructure (MpesaApiClient), Business (MpesaService), WPF (Payment UI)
- **Pattern:** Service pattern, async operations
- **Security:** Encrypt credentials, validate phone numbers
- **Error Handling:** Timeout, retry, fallback to manual

### Callback Considerations

Since this is a desktop app without a public endpoint:
1. **Option A:** Use REST API (Epic 33) callback endpoint
2. **Option B:** Polling mechanism - query status every 3 seconds
3. **Recommended:** Implement polling initially, migrate to callbacks when API deployed

### Phone Number Validation

```csharp
public static bool IsValidKenyanPhone(string phone)
{
    // Must be 254XXXXXXXXX (12 digits)
    return Regex.IsMatch(phone, @"^254[17]\d{8}$");
}
```

### References

- [Source: _bmad-output/feature-gap-analysis-2026-01-16.md#2.2-M-Pesa-Daraja-API-Integration]
- [Source: _bmad-output/architecture.md#Payment-Processing]
- Safaricom Daraja Documentation: https://developer.safaricom.co.ke/

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

### File List
