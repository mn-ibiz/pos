# Story 18.3: Real-Time eTIMS Submission

## Story
**As the** system,
**I want to** submit invoices to KRA in real-time,
**So that** tax records are immediately registered.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Implementation Summary
**Completed:** January 2026

### Files Created/Modified:
- `src/HospitalityPOS.Infrastructure/Services/EtimsService.cs` - Real-time submission with:
  - `SubmitInvoiceAsync` - Submit invoice to KRA API
  - Signature and QR code handling from response
  - Request/response JSON logging
  - Retry tracking and error handling

## Epic
**Epic 18: Kenya eTIMS Compliance (MANDATORY)**

## Context
KRA requires invoices to be submitted in real-time or near-real-time. When internet is available, invoices should be submitted to the eTIMS API immediately after transaction completion, and the response (including Control Code and QR code) should be stored and available for receipt printing.

## Acceptance Criteria

### AC1: Immediate Submission
**Given** a transaction is completed
**When** internet is available
**Then**:
- Invoice is submitted to KRA eTIMS API within 30 seconds
- Submission happens asynchronously (doesn't block checkout)
- Status indicator shows "Submitting to eTIMS..."

### AC2: Successful Response Handling
**Given** submission is successful
**When** receiving KRA response
**Then**:
- Stores Control Code from response
- Stores/generates QR code for verification
- Updates invoice record with eTIMS data
- Status changes to "eTIMS Confirmed"

### AC3: Failed Submission Handling
**Given** submission fails
**When** handling error
**Then**:
- Queues invoice for retry
- Uses exponential backoff (30s, 1m, 5m, 15m, 1h)
- Maximum 24 hours before escalation
- Logs error details for troubleshooting

### AC4: Retry Mechanism
**Given** previous submission failed
**When** retrying
**Then**:
- Attempts resubmission automatically
- Respects rate limits
- Updates retry count and last attempt time
- Alerts admin after 5 consecutive failures

## Technical Notes

### Implementation Details
```csharp
public interface IETimsSubmissionService
{
    Task<SubmissionResult> SubmitInvoiceAsync(ETimsInvoice invoice);
    Task<bool> RetryFailedSubmissionsAsync();
    Task<SubmissionStatus> CheckSubmissionStatusAsync(string invoiceNumber);
}

public class ETimsSubmissionService : IETimsSubmissionService
{
    private readonly HttpClient _httpClient;
    private readonly IETimsConfiguration _config;
    private readonly ILogger<ETimsSubmissionService> _logger;

    public async Task<SubmissionResult> SubmitInvoiceAsync(ETimsInvoice invoice)
    {
        try
        {
            var token = await GetAccessTokenAsync();
            var payload = MapToETimsPayload(invoice);

            var response = await _httpClient.PostAsJsonAsync(
                "/api/invoice/submit",
                payload,
                new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ETimsResponse>();
                return new SubmissionResult
                {
                    Success = true,
                    ControlCode = result.ControlCode,
                    QrCode = result.QrCode
                };
            }

            return HandleError(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "eTIMS submission failed for invoice {InvoiceNumber}",
                invoice.InvoiceNumber);
            return new SubmissionResult { Success = false, Error = ex.Message };
        }
    }
}
```

### Background Retry Service
```csharp
public class ETimsRetryBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessPendingSubmissionsAsync();
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task ProcessPendingSubmissionsAsync()
    {
        var pending = await _repository.GetPendingSubmissionsAsync();
        foreach (var invoice in pending.Where(ShouldRetry))
        {
            await _submissionService.SubmitInvoiceAsync(invoice);
        }
    }
}
```

### API Endpoints
- Submit Invoice: `POST /api/invoice/submit`
- Query Status: `GET /api/invoice/{invoiceNumber}/status`

## Dependencies
- Story 18.1: eTIMS Control Unit Registration (authentication)
- Story 18.2: KRA-Compliant Invoice Generation

## Files to Create/Modify
- `HospitalityPOS.Infrastructure/Services/ETimsSubmissionService.cs`
- `HospitalityPOS.Infrastructure/BackgroundServices/ETimsRetryService.cs`
- `HospitalityPOS.Core/Entities/ETimsSubmissionQueue.cs`
- Database migration for submission queue table

## Testing Requirements
- Unit tests for submission logic
- Integration tests with eTIMS sandbox API
- Tests for retry mechanism
- Tests for error handling scenarios

## Definition of Done
- [ ] Real-time submission working with eTIMS API
- [ ] Control Code and QR code stored on success
- [ ] Retry mechanism with exponential backoff
- [ ] Background service processing failed submissions
- [ ] Admin alerts for persistent failures
- [ ] Integration tests with sandbox passing
- [ ] Code reviewed and approved
