# Implementation Roadmap - Phase 3: Reporting & Communication

**Project:** HospitalityPOS WPF Application
**Phase:** 3 - Reporting Enhancements & Communication Features
**Start Date:** _______________
**Target Completion:** 2 Weeks

---

## Progress Tracker

### Week 1 Progress
| Day | Feature | Status | Notes |
|-----|---------|--------|-------|
| Day 1 | Hourly Sales Chart (Part 1) | [ ] Pending | LiveCharts integration |
| Day 2 | Hourly Sales Chart (Part 2) | [ ] Pending | X-Report integration |
| Day 3 | Logo Upload (Part 1) | [ ] Pending | File upload UI |
| Day 4 | Logo Upload (Part 2) | [ ] Pending | Receipt integration |
| Day 5 | Overdue PO Indicator | [ ] Pending | Visual indicators |

### Week 2 Progress
| Day | Feature | Status | Notes |
|-----|---------|--------|-------|
| Day 1 | Duplicate PO | [ ] Pending | Copy PO functionality |
| Day 2 | Email Service Setup | [ ] Pending | SMTP configuration |
| Day 3 | Email Reports | [ ] Pending | Send reports via email |
| Day 4 | PO Email to Supplier | [ ] Pending | PDF generation & email |
| Day 5 | Testing & Polish | [ ] Pending | Integration testing |

---

## Feature 1: Hourly Sales Chart

**Priority:** High | **Effort:** 2 days | **Gap Reference:** Section 8 (X-Report)

### Description
Add an hourly sales breakdown chart to the X-Report dialog showing sales distribution across hours of the day using LiveCharts.

### Files to Create/Modify
- [ ] `HospitalityPOS.WPF.csproj` - Add LiveCharts2 NuGet package
- [ ] `Views/Dialogs/XReportDialog.xaml` - Add chart section
- [ ] `ViewModels/Dialogs/XReportDialogViewModel.cs` - Add chart data
- [ ] `Core/Models/Reports/HourlySalesData.cs` - NEW: Data model

### Implementation Checklist

#### Day 1 Tasks
- [ ] Add LiveCharts2 NuGet package to WPF project
- [ ] Create HourlySalesData model in Core project
- [ ] Add GetHourlySalesAsync method to IReportService
- [ ] Implement hourly sales query in ReportService

#### Day 2 Tasks
- [ ] Add chart section to XReportDialog.xaml
- [ ] Bind chart to HourlySales data
- [ ] Style chart to match dark theme
- [ ] Add toggle to show/hide chart
- [ ] Test with various data scenarios

### Testing Checklist
- [ ] Chart displays correct hourly breakdown
- [ ] Chart respects dark theme styling
- [ ] Chart handles no-data scenario gracefully
- [ ] Print function includes chart (if possible)
- [ ] Performance acceptable with large datasets

---

## Feature 2: Business Logo Upload

**Priority:** High | **Effort:** 2 days | **Gap Reference:** Section 17 (Organization Settings)

### Description
Allow uploading a business logo that appears on receipts and reports.

### Files to Create/Modify
- [ ] `Views/OrganizationSettingsView.xaml` - Add logo upload section
- [ ] `ViewModels/OrganizationSettingsViewModel.cs` - Add upload logic
- [ ] `Core/Entities/SystemConfiguration.cs` - Add LogoPath property (if not exists)
- [ ] `Services/ReceiptPrintService.cs` - Include logo in receipts
- [ ] `Views/Dialogs/ReceiptPreviewDialog.xaml` - Display logo

### Implementation Checklist

#### Day 3 Tasks
- [ ] Add LogoPath property to SystemConfiguration (if not exists)
- [ ] Create logo upload section in OrganizationSettingsView
- [ ] Implement file picker dialog for image selection
- [ ] Add image preview with current logo display
- [ ] Save logo to application data folder
- [ ] Store logo path in configuration

#### Day 4 Tasks
- [ ] Update ReceiptPreviewDialog to show logo
- [ ] Update receipt print service to include logo
- [ ] Add "Remove Logo" functionality
- [ ] Handle missing logo gracefully
- [ ] Test with various image sizes and formats

### Testing Checklist
- [ ] Can upload PNG/JPG images
- [ ] Logo displays in preview
- [ ] Logo appears on printed receipts
- [ ] Logo can be removed
- [ ] Invalid file types rejected
- [ ] Large images resized appropriately

---

## Feature 3: Overdue PO Indicator

**Priority:** High | **Effort:** 1 day | **Gap Reference:** Section 5 (Purchase Orders)

### Description
Highlight purchase orders that are overdue for delivery with visual indicators.

### Files to Modify
- [ ] `ViewModels/PurchaseOrdersViewModel.cs` - Add overdue calculation
- [ ] `Views/PurchaseOrdersView.xaml` - Add overdue styling
- [ ] `Core/Entities/PurchaseOrder.cs` - Add ExpectedDeliveryDate (if not exists)

### Implementation Checklist

#### Day 5 Tasks
- [ ] Add ExpectedDeliveryDate to PurchaseOrder entity (if needed)
- [ ] Add IsOverdue computed property to PO ViewModel
- [ ] Add overdue count to statistics bar
- [ ] Style overdue rows with red/orange highlighting
- [ ] Add "Overdue" badge to status column
- [ ] Add filter for overdue POs
- [ ] Test date calculations

### Testing Checklist
- [ ] Overdue POs show visual indicator
- [ ] Overdue count displays in stats bar
- [ ] Filter correctly shows only overdue POs
- [ ] POs without expected date handle gracefully
- [ ] Date boundary (today) handled correctly

---

## Feature 4: Duplicate Purchase Order

**Priority:** Medium | **Effort:** 1 day | **Gap Reference:** Section 5 (Purchase Orders)

### Description
Allow duplicating an existing PO to create a new draft with the same items.

### Files to Modify
- [ ] `ViewModels/PurchaseOrdersViewModel.cs` - Add duplicate command
- [ ] `Views/PurchaseOrdersView.xaml` - Add duplicate button
- [ ] `Core/Interfaces/IPurchaseOrderService.cs` - Add duplicate method
- [ ] `Infrastructure/Services/PurchaseOrderService.cs` - Implement duplicate

### Implementation Checklist

#### Day 1 (Week 2) Tasks
- [ ] Add DuplicatePurchaseOrderAsync to IPurchaseOrderService
- [ ] Implement duplicate logic (copy items, reset status to Draft)
- [ ] Add DuplicateCommand to PurchaseOrdersViewModel
- [ ] Add "Duplicate" button to row actions
- [ ] Navigate to edit view after duplicate
- [ ] Test duplicate functionality

### Testing Checklist
- [ ] Duplicate creates new PO with Draft status
- [ ] All line items copied correctly
- [ ] Quantities and prices preserved
- [ ] Original PO unchanged
- [ ] User navigated to edit new PO

---

## Feature 5: Email Service & Configuration

**Priority:** High | **Effort:** 1 day | **Gap Reference:** Section 9, 46 (Sales Reports, Email Settings)

### Description
Set up email service infrastructure for sending reports and notifications.

### Files to Create/Modify
- [ ] `Core/Interfaces/IEmailService.cs` - Review/update interface
- [ ] `Infrastructure/Services/EmailService.cs` - Implement SMTP sending
- [ ] `Views/EmailSettingsView.xaml` - Review/update settings UI
- [ ] `ViewModels/EmailSettingsViewModel.cs` - Add test email function

### Implementation Checklist

#### Day 2 (Week 2) Tasks
- [ ] Review existing IEmailService interface
- [ ] Implement SMTP email sending with MailKit
- [ ] Add email configuration to SystemConfiguration
- [ ] Create "Test Email" functionality in settings
- [ ] Handle connection errors gracefully
- [ ] Support SSL/TLS options
- [ ] Test with common email providers (Gmail, Outlook)

### Testing Checklist
- [ ] Test email sends successfully
- [ ] SSL/TLS connections work
- [ ] Invalid credentials show clear error
- [ ] Timeout handled gracefully
- [ ] HTML email formatting works

---

## Feature 6: Email Reports

**Priority:** Medium | **Effort:** 1 day | **Gap Reference:** Section 9 (Sales Reports)

### Description
Allow sending generated reports via email directly from the reports view.

### Files to Modify
- [ ] `ViewModels/SalesReportsViewModel.cs` - Add email command
- [ ] `Views/SalesReportsView.xaml` - Add email button
- [ ] `Views/Dialogs/EmailReportDialog.xaml` - NEW: Email dialog
- [ ] `ViewModels/Dialogs/EmailReportDialogViewModel.cs` - NEW: Dialog VM

### Implementation Checklist

#### Day 3 (Week 2) Tasks
- [ ] Create EmailReportDialog with recipient input
- [ ] Add EmailReportDialogViewModel
- [ ] Add "Email Report" button to SalesReportsView
- [ ] Generate CSV/PDF attachment from report data
- [ ] Send email with report attached
- [ ] Show success/failure message
- [ ] Test email delivery

### Testing Checklist
- [ ] Email dialog opens correctly
- [ ] Can enter recipient email
- [ ] Report attached correctly
- [ ] Email sent successfully
- [ ] Error handling for failed sends

---

## Feature 7: Email PO to Supplier

**Priority:** High | **Effort:** 1 day | **Gap Reference:** Section 5 (Purchase Orders)

### Description
Send purchase orders to suppliers via email with PDF attachment.

### Files to Create/Modify
- [ ] `ViewModels/PurchaseOrdersViewModel.cs` - Enhance send command
- [ ] `Services/PurchaseOrderPdfService.cs` - NEW: PDF generation
- [ ] `Core/Interfaces/IPurchaseOrderPdfService.cs` - NEW: Interface

### Implementation Checklist

#### Day 4 (Week 2) Tasks
- [ ] Create PDF generation service for POs using QuestPDF or similar
- [ ] Design professional PO PDF template
- [ ] Include business logo in PDF
- [ ] Add email dialog for PO sending
- [ ] Pre-fill supplier email address
- [ ] Update PO status after sending
- [ ] Record send history/timestamp
- [ ] Test PDF generation and email

### Testing Checklist
- [ ] PDF generates correctly
- [ ] Logo appears on PDF
- [ ] All PO details present
- [ ] Email sends with attachment
- [ ] Supplier email pre-filled
- [ ] Status updated after send

---

## Daily Standup Notes

### Week 1

**Day 1 - Date: _______________**
- Worked on:
- Blockers:
- Tomorrow:

**Day 2 - Date: _______________**
- Worked on:
- Blockers:
- Tomorrow:

**Day 3 - Date: _______________**
- Worked on:
- Blockers:
- Tomorrow:

**Day 4 - Date: _______________**
- Worked on:
- Blockers:
- Tomorrow:

**Day 5 - Date: _______________**
- Worked on:
- Blockers:
- Next Week:

### Week 2

**Day 1 - Date: _______________**
- Worked on:
- Blockers:
- Tomorrow:

**Day 2 - Date: _______________**
- Worked on:
- Blockers:
- Tomorrow:

**Day 3 - Date: _______________**
- Worked on:
- Blockers:
- Tomorrow:

**Day 4 - Date: _______________**
- Worked on:
- Blockers:
- Tomorrow:

**Day 5 - Date: _______________**
- Worked on:
- Blockers:
- Phase 3 Complete:

---

## Phase 3 Completion Summary

### Features Delivered
| Feature | Planned Effort | Actual Effort | Status |
|---------|----------------|---------------|--------|
| Hourly Sales Chart | 2 days | ___ days | |
| Logo Upload | 2 days | ___ days | |
| Overdue PO Indicator | 1 day | ___ days | |
| Duplicate PO | 1 day | ___ days | |
| Email Service Setup | 1 day | ___ days | |
| Email Reports | 1 day | ___ days | |
| PO Email to Supplier | 1 day | ___ days | |

### Lessons Learned
1.
2.
3.

---

## Quick Reference: File Locations

### New Files (Phase 3)
```
src/HospitalityPOS.WPF/
├── Views/
│   └── Dialogs/
│       └── EmailReportDialog.xaml (NEW)
├── ViewModels/
│   └── Dialogs/
│       └── EmailReportDialogViewModel.cs (NEW)
├── Services/
│   └── PurchaseOrderPdfService.cs (NEW)

src/HospitalityPOS.Core/
├── Models/
│   └── Reports/
│       └── HourlySalesData.cs (NEW)
├── Interfaces/
│   └── IPurchaseOrderPdfService.cs (NEW)
```

### NuGet Packages to Add
- `LiveChartsCore.SkiaSharpView.WPF` - For charts
- `QuestPDF` - For PDF generation (if not already present)
- `MailKit` - For email sending (if not already present)

---

**Document Version:** 1.0
**Last Updated:** January 2026
