# Story 39.1: KRA eTIMS API Integration

Status: Done

## Story

As a **business owner operating in Kenya**,
I want **the POS system to automatically submit all sales invoices to KRA eTIMS in real-time**,
so that **I comply with Kenya tax law and avoid penalties for non-compliance**.

## Business Context

**CRITICAL - LEGAL REQUIREMENT**

The Kenya Revenue Authority (KRA) mandated that ALL businesses must use the Electronic Tax Invoice Management System (eTIMS) for invoicing since 2024. Without this integration:
- The system is **ILLEGAL to use in Kenya**
- Customers cannot claim input VAT
- Business PIN may be flagged by KRA
- Penalties and fines apply

**Competitive Reference:** SimbaPOS, Uzalynx, POSmart, DigitalPOS are all KRA-certified integrators.

## Acceptance Criteria

### AC1: eTIMS Device Registration
- [ ] System can register as eTIMS-compliant device with KRA
- [ ] Control Unit ID and Device Serial Number are stored securely
- [ ] Business KRA PIN is configured and validated
- [ ] OSCU (Online) and VSCU (Virtual) modes are supported

### AC2: Real-Time Invoice Submission
- [ ] Every completed sale automatically generates eTIMS invoice
- [ ] Invoice data transmitted to KRA API in real-time (<5 seconds)
- [ ] KRA validation response is received and stored
- [ ] Invoice number from KRA is recorded with receipt

### AC3: eTIMS QR Code on Receipts
- [ ] Valid eTIMS QR code printed on all receipts
- [ ] QR code contains KRA-compliant verification data
- [ ] Customers can scan QR to verify invoice with KRA

### AC4: Offline/Error Handling
- [ ] Transactions queue locally when KRA API unreachable
- [ ] Automatic retry with exponential backoff
- [ ] VSCU batch upload for offline periods
- [ ] Clear error messages for submission failures

### AC5: Credit Note Support
- [ ] Refunds generate eTIMS-compliant credit notes
- [ ] Credit notes linked to original invoice
- [ ] Credit notes submitted to KRA with proper reference

### AC6: eTIMS Status Dashboard
- [ ] Dashboard shows pending/submitted/failed invoices
- [ ] Retry button for failed submissions
- [ ] Daily submission summary
- [ ] Alert for invoices older than 48 hours not submitted

## Tasks / Subtasks

- [ ] **Task 1: Database Schema for eTIMS** (AC: 1, 2)
  - [ ] 1.1 Create EtimsConfiguration table (ControlUnitId, DeviceSerial, TaxPayerPIN, IntegrationType, ApiEndpoint)
  - [ ] 1.2 Create EtimsInvoices table (ReceiptId, InvoiceNumber, QrCode, SubmissionStatus, KraResponse)
  - [ ] 1.3 Create EtimsCreditNotes table (OriginalInvoiceId, CreditNoteNumber, Reason, Amount)
  - [ ] 1.4 Create EtimsConfiguration migration
  - [ ] 1.5 Add unit tests for entity configurations

- [ ] **Task 2: eTIMS API Client Service** (AC: 1, 2, 5)
  - [ ] 2.1 Create IEtimsService interface with methods: RegisterDevice, SubmitInvoice, SubmitCreditNote, QueryStatus
  - [ ] 2.2 Implement EtimsApiClient with OAuth token handling
  - [ ] 2.3 Implement OSCU real-time submission flow
  - [ ] 2.4 Implement VSCU batch submission flow
  - [ ] 2.5 Implement credit note submission
  - [ ] 2.6 Add comprehensive error handling and logging
  - [ ] 2.7 Write unit tests with mocked API responses

- [ ] **Task 3: Auto-Submit on Receipt Settlement** (AC: 2)
  - [ ] 3.1 Modify ReceiptService.SettleReceiptAsync to call eTIMS submission
  - [ ] 3.2 Store KRA invoice number with receipt
  - [ ] 3.3 Handle submission failure gracefully (don't block POS)
  - [ ] 3.4 Queue failed submissions for retry
  - [ ] 3.5 Integration tests for settlement flow

- [ ] **Task 4: Offline Queue Management** (AC: 4)
  - [ ] 4.1 Create SyncQueue table entry for eTIMS type
  - [ ] 4.2 Implement background service to process queue
  - [ ] 4.3 Implement retry logic with exponential backoff (1s, 2s, 4s, 8s, max 5 retries)
  - [ ] 4.4 Implement VSCU batch upload for >100 queued items
  - [ ] 4.5 Add connectivity check before submission
  - [ ] 4.6 Unit tests for queue processing

- [ ] **Task 5: QR Code Generation** (AC: 3)
  - [ ] 5.1 Add QRCoder NuGet package
  - [ ] 5.2 Implement eTIMS QR code format per KRA spec
  - [ ] 5.3 Modify receipt printing to include QR code
  - [ ] 5.4 Test QR scanning with KRA verification app

- [ ] **Task 6: eTIMS Configuration UI** (AC: 1, 6)
  - [ ] 6.1 Create EtimsSettingsView.xaml in BackOffice
  - [ ] 6.2 Create EtimsSettingsViewModel with device registration flow
  - [ ] 6.3 Add API credential input fields with secure storage
  - [ ] 6.4 Add test connection button
  - [ ] 6.5 Create eTIMS status dashboard widget

- [ ] **Task 7: Credit Note Integration** (AC: 5)
  - [ ] 7.1 Modify VoidReceiptAsync to create eTIMS credit note
  - [ ] 7.2 Link credit note to original eTIMS invoice
  - [ ] 7.3 Submit credit note to KRA
  - [ ] 7.4 Store credit note response
  - [ ] 7.5 Integration tests for void/refund flow

## Dev Notes

### KRA eTIMS API Integration Methods

```
Integration Options:
1. OSCU (Online Sales Control Unit) - For always-online systems
2. VSCU (Virtual Sales Control Unit) - For bulk/offline invoicing

Recommended: Implement BOTH
- OSCU as primary for real-time transactions
- VSCU as fallback for offline periods

API Endpoints Required:
- POST /api/invoice/submit - Submit invoice
- POST /api/creditnote/submit - Submit credit note
- GET /api/invoice/{id}/status - Query status
- POST /api/device/register - Register device
```

### Database Schema (from Gap Analysis)

```sql
CREATE TABLE EtimsConfiguration (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ControlUnitId NVARCHAR(50) NOT NULL,
    DeviceSerialNumber NVARCHAR(50) NOT NULL,
    BranchId NVARCHAR(20),
    TaxPayerPIN NVARCHAR(20) NOT NULL,
    IntegrationType NVARCHAR(10) NOT NULL, -- OSCU, VSCU
    ApiEndpoint NVARCHAR(500) NOT NULL,
    LastSyncAt DATETIME2,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

CREATE TABLE EtimsInvoices (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ReceiptId INT FOREIGN KEY REFERENCES Receipts(Id),
    InvoiceNumber NVARCHAR(50), -- KRA assigned
    ControlUnitInvoiceNumber NVARCHAR(50), -- Local sequence
    QrCode NVARCHAR(MAX),
    SubmissionStatus NVARCHAR(20) DEFAULT 'Pending',
    KraResponse NVARCHAR(MAX),
    SubmittedAt DATETIME2,
    ValidatedAt DATETIME2,
    RetryCount INT DEFAULT 0,
    LastError NVARCHAR(500),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);
```

### Architecture Compliance

- **Layer:** Infrastructure (API Client), Business (Service), WPF (Settings UI)
- **Pattern:** Repository for EtimsInvoices, Service for submission logic
- **Async:** All API calls MUST be async
- **Error Handling:** Queue failures, don't block POS operations
- **Security:** Store API credentials encrypted in SystemSettings

### Project Structure

```
src/
├── HospitalityPOS.Core/
│   └── Entities/
│       ├── EtimsConfiguration.cs
│       ├── EtimsInvoice.cs
│       └── EtimsCreditNote.cs
├── HospitalityPOS.Infrastructure/
│   └── External/
│       └── EtimsApiClient.cs
├── HospitalityPOS.Business/
│   └── Services/
│       └── EtimsService.cs
└── HospitalityPOS.WPF/
    └── Views/BackOffice/
        └── EtimsSettingsView.xaml
```

### Testing Requirements

- Unit tests for EtimsService with mocked API
- Integration tests for queue processing
- Manual test with KRA sandbox environment

### References

- [Source: _bmad-output/feature-gap-analysis-2026-01-16.md#2.1-KRA-eTIMS-Integration]
- [Source: _bmad-output/architecture.md#Database-Schema]
- [Source: _bmad-output/project-context.md#Technology-Stack]
- KRA eTIMS Developer Documentation: https://www.kra.go.ke/etims

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

### File List
