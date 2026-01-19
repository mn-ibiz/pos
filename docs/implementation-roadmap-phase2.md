# Implementation Roadmap - Phase 2: Operational Efficiency & Audit

**Project:** HospitalityPOS WPF Application
**Phase:** 2 - Operational Efficiency & Audit Features
**Start Date:** _______________
**Target Completion:** 2 Weeks

---

## Progress Tracker

### Week 1 Progress
| Day | Feature | Status | Notes |
|-----|---------|--------|-------|
| Day 1 | Barcode Receiving (Part 1) | [x] Completed | UI updates for barcode input |
| Day 2 | Barcode Receiving (Part 2) | [x] Completed | ViewModel logic and integration |
| Day 3 | Stock Movement History (Part 1) | [x] Completed | History panel UI |
| Day 4 | Stock Movement History (Part 2) | [x] Completed | Service and data integration |
| Day 5 | Receipt Preview | [x] Completed | Preview dialog before printing |

### Week 2 Progress
| Day | Feature | Status | Notes |
|-----|---------|--------|-------|
| Day 1 | Login Audit Trail (Part 1) | [x] Completed | Database model and service |
| Day 2 | Login Audit Trail (Part 2) | [x] Completed | UI for viewing login history |
| Day 3 | Date Range Presets | [x] Completed | Quick date filters for reports |
| Day 4 | Blind Count Mode | [x] Completed | Hide expected during cash count |
| Day 5 | Testing & Polish | [ ] Pending | Integration testing |

---

## Feature 1: Barcode Receiving

**Priority:** High | **Effort:** 2 days | **Gap Reference:** Section 4

### Description
Enable barcode scanning during goods receiving to speed up the process. Scan a product barcode to auto-fill the received quantity or increment it.

### Files to Modify
- [ ] `Views/GoodsReceivingView.xaml` - Add barcode input field
- [ ] `ViewModels/GoodsReceivingViewModel.cs` - Add barcode processing

### Implementation Checklist

#### Day 1 Tasks
- [ ] Add barcode input TextBox to GoodsReceivingView
- [ ] Style barcode input with scan icon
- [ ] Add visual feedback for successful scan
- [ ] Add "Scan Mode" toggle button

#### Day 2 Tasks
- [ ] Implement `ProcessBarcodeCommand` in ViewModel
- [ ] Match barcode to PO line items
- [ ] Auto-increment received quantity on scan
- [ ] Add audio/visual feedback for not-found barcodes
- [ ] Test with various barcode formats

### Testing Checklist
- [ ] Scan product barcode - increments quantity by 1
- [ ] Scan same product again - increments further
- [ ] Scan product not in PO - shows error message
- [ ] Manual entry still works alongside scanning
- [ ] Focus returns to barcode field after scan

---

## Feature 2: Stock Movement History

**Priority:** High | **Effort:** 2 days | **Gap Reference:** Section 3

### Description
Add a panel to view historical stock movements for any product, showing adjustments, sales, receiving, and transfers.

### Files to Modify/Create
- [ ] `Views/Inventory/StockHistoryPanel.xaml` - NEW: History panel control
- [ ] `Views/Inventory/StockHistoryPanel.xaml.cs` - NEW: Code-behind
- [ ] `ViewModels/Inventory/StockHistoryViewModel.cs` - NEW: ViewModel
- [ ] `Views/Inventory/InventoryView.xaml` - Add history panel integration
- [ ] `Core/Interfaces/IInventoryService.cs` - Add history method
- [ ] `Infrastructure/Services/InventoryService.cs` - Implement history query

### Implementation Checklist

#### Day 3 Tasks
- [ ] Create StockMovement DTO if not exists
- [ ] Add `GetStockHistoryAsync(productId)` to service interface
- [ ] Implement service method to query movements
- [ ] Create StockHistoryPanel.xaml with DataGrid

#### Day 4 Tasks
- [ ] Create StockHistoryViewModel
- [ ] Wire up history panel in InventoryView
- [ ] Add "View History" button to product row
- [ ] Implement date range filter for history
- [ ] Add movement type icons (In/Out/Adjust)
- [ ] Test with real stock movement data

### Testing Checklist
- [ ] Click "View History" - shows movement panel
- [ ] History shows sales deductions
- [ ] History shows receiving additions
- [ ] History shows manual adjustments
- [ ] Date filter works correctly
- [ ] Export history to CSV works

---

## Feature 3: Receipt Preview

**Priority:** High | **Effort:** 1 day | **Gap Reference:** Section 2

### Description
Show an on-screen receipt preview before printing, allowing cashiers to verify the receipt content.

### Files to Create/Modify
- [ ] `Views/Dialogs/ReceiptPreviewDialog.xaml` - NEW: Preview dialog
- [ ] `Views/Dialogs/ReceiptPreviewDialog.xaml.cs` - NEW: Code-behind
- [ ] `ViewModels/Dialogs/ReceiptPreviewDialogViewModel.cs` - NEW: ViewModel
- [ ] `ViewModels/POSViewModel.cs` - Add preview option

### Implementation Checklist

#### Day 5 Tasks
- [ ] Create ReceiptPreviewDialog layout (thermal receipt style)
- [ ] Display business header (name, address, contact)
- [ ] Show order items with prices
- [ ] Show subtotal, tax, discounts, total
- [ ] Show payment method and change
- [ ] Add "Print" and "Cancel" buttons
- [ ] Integrate with POS payment flow

### Testing Checklist
- [ ] Preview shows correct order items
- [ ] Preview shows correct totals
- [ ] Preview shows payment details
- [ ] Print button prints and closes
- [ ] Cancel returns to POS without printing

---

## Feature 4: Login Audit Trail

**Priority:** High | **Effort:** 2 days | **Gap Reference:** Section 14

### Description
Track all login attempts (successful and failed) with timestamps, IP addresses, and device info for security auditing.

### Files to Create/Modify
- [ ] `Core/Entities/LoginAudit.cs` - NEW: Entity
- [ ] `Infrastructure/Data/AppDbContext.cs` - Add DbSet
- [ ] `Core/Interfaces/IAuditService.cs` - Add audit interface
- [ ] `Infrastructure/Services/AuditService.cs` - NEW: Implementation
- [ ] `Views/LoginAuditView.xaml` - NEW: Audit view
- [ ] `ViewModels/LoginAuditViewModel.cs` - NEW: ViewModel
- [ ] `ViewModels/LoginViewModel.cs` - Record login attempts

### Implementation Checklist

#### Day 1 (Week 2) Tasks
- [ ] Create LoginAudit entity (UserId, Username, Timestamp, Success, IPAddress, DeviceInfo)
- [ ] Add migration for LoginAudit table
- [ ] Create IAuditService interface
- [ ] Implement AuditService with RecordLoginAttempt method
- [ ] Integrate audit recording in LoginViewModel

#### Day 2 (Week 2) Tasks
- [ ] Create LoginAuditView with DataGrid
- [ ] Add filters (User, Date Range, Success/Failed)
- [ ] Add suspicious activity indicators (multiple failed attempts)
- [ ] Add navigation from User Management
- [ ] Test audit recording for login/logout

### Testing Checklist
- [ ] Successful login creates audit record
- [ ] Failed login creates audit record with Success=false
- [ ] Audit view shows all login history
- [ ] Filter by user works
- [ ] Filter by date range works
- [ ] Multiple failed attempts highlighted

---

## Feature 5: Date Range Presets for Reports

**Priority:** Medium | **Effort:** 1 day | **Gap Reference:** Section 9

### Description
Add quick preset buttons for common date ranges in reports (Today, Yesterday, This Week, Last Week, This Month, Last Month, MTD, YTD).

### Files to Modify
- [ ] `Views/SalesReportsView.xaml` - Add preset buttons
- [ ] `ViewModels/SalesReportsViewModel.cs` - Add preset commands
- [ ] `Views/InventoryReportsView.xaml` - Add preset buttons
- [ ] `ViewModels/InventoryReportsViewModel.cs` - Add preset commands

### Implementation Checklist

#### Day 3 (Week 2) Tasks
- [ ] Create DateRangePreset enum
- [ ] Add preset button row to SalesReportsView
- [ ] Implement preset commands (Today, Yesterday, This Week, etc.)
- [ ] Style preset buttons as toggle/chip buttons
- [ ] Apply same to InventoryReportsView
- [ ] Test date calculations for each preset

### Testing Checklist
- [ ] "Today" sets correct date range
- [ ] "Yesterday" sets correct date range
- [ ] "This Week" shows Mon-Sun current week
- [ ] "Last Week" shows previous Mon-Sun
- [ ] "This Month" shows 1st to today
- [ ] "Last Month" shows full previous month
- [ ] "MTD" (Month to Date) works correctly
- [ ] "YTD" (Year to Date) works correctly

---

## Feature 6: Blind Count Mode

**Priority:** Medium | **Effort:** 0.5 day | **Gap Reference:** Section 7

### Description
Add option to hide the expected cash amount during close day count to prevent cashiers from gaming the system.

### Files to Modify
- [ ] `Views/Dialogs/CloseWorkPeriodDialog.xaml` - Add blind mode toggle
- [ ] `Views/Dialogs/CloseWorkPeriodDialog.xaml.cs` - Add toggle logic

### Implementation Checklist

#### Day 4 (Week 2) Tasks
- [ ] Add "Blind Count" toggle switch
- [ ] Hide Expected Cash field when blind mode enabled
- [ ] Hide Running Variance during count
- [ ] Show variance only after submission
- [ ] Remember blind mode preference

### Testing Checklist
- [ ] Toggle hides expected amount
- [ ] Variance hidden until count complete
- [ ] Manager can enable/disable blind mode
- [ ] Setting persists between sessions

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
- Phase 2 Complete:

---

## Phase 2 Completion Summary

### Features Delivered
| Feature | Planned Effort | Actual Effort | Status |
|---------|----------------|---------------|--------|
| Barcode Receiving | 2 days | ___ days | |
| Stock Movement History | 2 days | ___ days | |
| Receipt Preview | 1 day | ___ days | |
| Login Audit Trail | 2 days | ___ days | |
| Date Range Presets | 1 day | ___ days | |
| Blind Count Mode | 0.5 day | ___ days | |

### Lessons Learned
1.
2.
3.

---

## Quick Reference: File Locations

### New Files (Phase 2)
```
src/HospitalityPOS.WPF/
├── Views/
│   ├── Dialogs/
│   │   └── ReceiptPreviewDialog.xaml (NEW)
│   ├── Inventory/
│   │   └── StockHistoryPanel.xaml (NEW)
│   └── LoginAuditView.xaml (NEW)
├── ViewModels/
│   ├── Dialogs/
│   │   └── ReceiptPreviewDialogViewModel.cs (NEW)
│   ├── Inventory/
│   │   └── StockHistoryViewModel.cs (NEW)
│   └── LoginAuditViewModel.cs (NEW)

src/HospitalityPOS.Core/
├── Entities/
│   └── LoginAudit.cs (NEW)
├── Interfaces/
│   └── IAuditService.cs (NEW)

src/HospitalityPOS.Infrastructure/
└── Services/
    └── AuditService.cs (NEW)
```

---

**Document Version:** 1.0
**Last Updated:** January 2026
