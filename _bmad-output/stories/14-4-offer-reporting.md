# Story 14.4: Offer Reporting

Status: done

## Story

As a manager,
I want to see how offers are performing,
So that I can evaluate promotional effectiveness.

## Acceptance Criteria

1. **Given** offers have been used
   **When** running Offer Report
   **Then** shows redemption count, revenue, and discount given per offer

2. **Given** date range selected
   **When** filtering report
   **Then** shows only offers active in that period

## Tasks / Subtasks

- [x] Task 1: Create Offer Performance Report
  - [x] Create OfferReportView.xaml
  - [x] Create OfferReportViewModel
  - [x] Date range filter
  - [x] Offer status filter (active/expired/all)

- [x] Task 2: Implement Report Queries
  - [x] Calculate redemption count per offer
  - [x] Calculate total revenue from offer sales
  - [x] Calculate total discount given
  - [x] Calculate ROI metrics (discount percentage, avg savings)

- [x] Task 3: Add Report Export
  - [x] Export to PDF (via ReportPrintService)
  - [x] Export to CSV/Excel (via ReportPrintService)
  - [x] Print report

## Dev Notes

### Offer Report Columns

| Column | Description |
|--------|-------------|
| Offer Name | Name of the promotion |
| Product | Product on offer |
| Original Price | Regular selling price |
| Offer Price | Promotional price |
| Discount % | Calculated discount percentage |
| Redemptions | Number of times used |
| Revenue | Total sales from offer |
| Discount Given | Total savings to customers |
| Status | Active/Expired/Upcoming |
| Start Date | Offer start date |
| End Date | Offer end date |

## Dev Agent Record

### Agent Model Used
Claude claude-opus-4-5-20251101

### Completion Notes List
- Created IOfferService.GetActiveOffersAsync method to get all currently active offers
- Created IOfferService.GetOfferPerformanceAsync method to calculate offer performance metrics
- Created OfferPerformanceData DTO for report data
- Implemented GetActiveOffersAsync in OfferService
- Implemented GetOfferPerformanceAsync in OfferService with redemption counting from OrderItems
- Created OfferReportViewModel with:
  - Date range filters (StartDate, EndDate)
  - Status filter (All/Active/Expired/Upcoming)
  - GenerateReportAsync command
  - ExportToPdfAsync command
  - ExportToExcelAsync (CSV) command
  - PrintReportAsync command
  - HTML report generation for PDF/Print
  - CSV report generation for Excel export
- Created OfferPerformanceItem class for report display
- Created OfferReportView.xaml with:
  - Modern dark theme consistent with application
  - Date picker filters
  - Status filter dropdown
  - DataGrid with all offer metrics
  - Status badges with color coding (green=Active, red=Expired, yellow=Upcoming)
  - Summary footer with total redemptions, total revenue, total discount given
  - Export buttons for PDF, CSV, and Print
- Registered OfferReportViewModel in App.xaml.cs

### File List
- src/HospitalityPOS.Core/Interfaces/IOfferService.cs (GetActiveOffersAsync, GetOfferPerformanceAsync, OfferPerformanceData)
- src/HospitalityPOS.Infrastructure/Services/OfferService.cs (GetActiveOffersAsync, GetOfferPerformanceAsync)
- src/HospitalityPOS.WPF/ViewModels/OfferReportViewModel.cs (new)
- src/HospitalityPOS.WPF/Views/OfferReportView.xaml (new)
- src/HospitalityPOS.WPF/Views/OfferReportView.xaml.cs (new)
- src/HospitalityPOS.WPF/App.xaml.cs (registered OfferReportViewModel)
