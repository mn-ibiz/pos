# Story 18.6: eTIMS Status Dashboard

## Story
**As a** manager,
**I want to** monitor eTIMS compliance status,
**So that** I can ensure all invoices are registered with KRA.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Implementation Summary
**Completed:** January 2026

### Files Created/Modified:
- `src/HospitalityPOS.Infrastructure/Services/EtimsService.cs` - Dashboard with:
  - `GetDashboardDataAsync` - Today's stats (submitted, pending, failed)
  - `GetComplianceReportAsync` - Compliance rate and daily breakdown
  - Device status, queue counts, tax totals
  - Monthly submission and tax summaries

## Epic
**Epic 18: Kenya eTIMS Compliance (MANDATORY)**

## Context
Managers and administrators need visibility into the eTIMS submission status to ensure compliance. The dashboard provides real-time insights into submission success, pending items, and any issues requiring attention.

## Acceptance Criteria

### AC1: Status Overview
**Given** accessing eTIMS dashboard
**When** viewing status
**Then** shows:
- Submitted today: count and total value
- Pending submissions: count (should be 0 ideally)
- Failed submissions: count requiring attention
- Control Unit status: Active/Inactive

### AC2: Failed Submission Details
**Given** failed submissions exist
**When** viewing details
**Then** shows:
- Invoice number and date
- Error reason/message
- Retry count and last attempt
- Action button to retry manually

### AC3: Control Unit Alerts
**Given** CU issues detected
**When** alerting admin
**Then**:
- Displays prominent warning banner
- Shows issue description
- Provides resolution steps or KRA contact
- Logs alert to audit trail

### AC4: Reconciliation Report
**Given** need to reconcile with KRA
**When** running reconciliation
**Then**:
- Lists all invoices for date range
- Shows submission status per invoice
- Highlights discrepancies
- Export to Excel/PDF for KRA audits

## Technical Notes

### Implementation Details
```csharp
public class ETimsDashboardViewModel : ObservableObject
{
    public int SubmittedToday { get; set; }
    public decimal SubmittedValueToday { get; set; }
    public int PendingCount { get; set; }
    public int FailedCount { get; set; }
    public string ControlUnitStatus { get; set; }
    public DateTime LastSuccessfulSync { get; set; }
    public bool HasAlerts => FailedCount > 0 || ControlUnitStatus != "Active";

    public ObservableCollection<FailedSubmissionDto> FailedSubmissions { get; set; }

    [RelayCommand]
    private async Task RetrySubmissionAsync(string invoiceNumber)
    {
        await _eTimsService.RetrySubmissionAsync(invoiceNumber);
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task ExportReconciliationAsync()
    {
        var report = await _reportService.GenerateETimsReconciliationAsync(
            StartDate, EndDate);
        await _exportService.ExportToExcelAsync(report, "eTIMS_Reconciliation.xlsx");
    }
}

public class FailedSubmissionDto
{
    public string InvoiceNumber { get; set; }
    public DateTime InvoiceDate { get; set; }
    public decimal Amount { get; set; }
    public string ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public DateTime LastAttempt { get; set; }
}
```

### Dashboard Metrics Query
```csharp
public async Task<ETimsDashboardMetrics> GetDashboardMetricsAsync()
{
    var today = DateTime.Today;

    var submitted = await _context.ETimsInvoices
        .Where(i => i.SubmittedAt.HasValue &&
                    i.SubmittedAt.Value.Date == today)
        .Select(i => new { i.TotalAmount })
        .ToListAsync();

    var pending = await _context.ETimsOfflineQueue
        .CountAsync(q => q.Status == QueueStatus.Pending);

    var failed = await _context.ETimsOfflineQueue
        .CountAsync(q => q.Status == QueueStatus.Failed);

    return new ETimsDashboardMetrics
    {
        SubmittedToday = submitted.Count,
        SubmittedValueToday = submitted.Sum(s => s.TotalAmount),
        PendingCount = pending,
        FailedCount = failed,
        ControlUnitStatus = await GetControlUnitStatusAsync(),
        LastSync = await GetLastSuccessfulSyncAsync()
    };
}
```

### UI Design
```
╔═══════════════════════════════════════════════════════════════════╗
║                    eTIMS COMPLIANCE DASHBOARD                       ║
╠═══════════════════════════════════════════════════════════════════╣
║  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌───────────┐ ║
║  │ SUBMITTED   │  │  PENDING    │  │   FAILED    │  │ CU STATUS │ ║
║  │    125      │  │      0      │  │      2      │  │  ACTIVE   │ ║
║  │ KSh 450,000 │  │   ────────  │  │   ⚠ View   │  │    ✓      │ ║
║  └─────────────┘  └─────────────┘  └─────────────┘  └───────────┘ ║
║                                                                     ║
║  Last Sync: 2025-12-28 14:30:00                                    ║
║                                                                     ║
║  ⚠ ALERT: 2 invoices failed submission - Manual retry required     ║
╠═══════════════════════════════════════════════════════════════════╣
║  FAILED SUBMISSIONS                                                 ║
║  ┌────────────┬────────────┬───────────┬────────┬────────────────┐ ║
║  │ Invoice    │ Date       │ Amount    │ Retries│ Error          │ ║
║  ├────────────┼────────────┼───────────┼────────┼────────────────┤ ║
║  │ INV-000123 │ 28/12/2025 │ KSh 5,000 │ 3      │ Timeout        │ ║
║  │ INV-000145 │ 28/12/2025 │ KSh 2,500 │ 2      │ Invalid PIN    │ ║
║  └────────────┴────────────┴───────────┴────────┴────────────────┘ ║
║  [Retry Selected]  [View Details]  [Export Reconciliation]         ║
╚═══════════════════════════════════════════════════════════════════╝
```

## Dependencies
- Story 18.3: Real-Time eTIMS Submission
- Story 18.4: Offline eTIMS Queue Management
- Epic 10: Reporting & Analytics

## Files to Create/Modify
- `HospitalityPOS.WPF/ViewModels/Reports/ETimsDashboardViewModel.cs`
- `HospitalityPOS.WPF/Views/Reports/ETimsDashboardView.xaml`
- `HospitalityPOS.Business/Services/ETimsReportingService.cs`
- `HospitalityPOS.Infrastructure/Repositories/ETimsReportRepository.cs`

## Testing Requirements
- Unit tests for metrics calculation
- UI tests for dashboard rendering
- Tests for reconciliation report generation
- Tests for alert conditions

## Definition of Done
- [ ] Dashboard shows all key metrics
- [ ] Failed submissions listed with retry option
- [ ] CU status displayed prominently
- [ ] Alerts shown for issues
- [ ] Reconciliation export working
- [ ] UI is responsive and clear
- [ ] Unit tests passing
- [ ] Code reviewed and approved
