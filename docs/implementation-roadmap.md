# Implementation Roadmap - Phase 1: POS Quick Wins & Enhancements

**Project:** HospitalityPOS WPF Application
**Phase:** 1 - Quick Wins & Core POS Improvements
**Start Date:** _______________
**Target Completion:** 2 Weeks

---

## Progress Tracker

### Week 1 Progress
| Day | Feature | Status | Notes |
|-----|---------|--------|-------|
| Day 1 | Real-Time Product Search (Part 1) | [x] Completed | Added debounce search, dropdown popup |
| Day 2 | Real-Time Product Search (Part 2) | [x] Completed | Added keyboard navigation, result selection |
| Day 3 | Product Images in POS | [x] Completed | Added HasImage, IsLowStock badges |
| Day 4 | Denomination Counting | [x] Completed | Added collapsible grid with auto-calculation |
| Day 5 | Split Payment (Fast-tracked) | [x] Completed | Created dialog, ViewModel, integrated with POS |

### Week 2 Progress
| Day | Feature | Status | Notes |
|-----|---------|--------|-------|
| Day 1 | Split Payment (Part 1 - UI) | [x] Completed | Fast-tracked to Week 1 |
| Day 2 | Split Payment (Part 2 - ViewModel) | [x] Completed | Fast-tracked to Week 1 |
| Day 3 | Split Payment (Part 3 - Service) | [ ] Pending | Backend integration needed |
| Day 4 | Split Payment (Part 4 - Integration) | [x] Completed | POS integration done |
| Day 5 | Dashboard Sparklines | [x] Completed | OxyPlot sparklines in KPI cards |

---

## Feature 1: Real-Time Product Search

**Priority:** Critical | **Effort:** 2-3 days | **Assigned To:** _______________

### Description
Replace the current Enter-to-search behavior with real-time autocomplete dropdown that shows product suggestions as the user types.

### Files to Modify
- [ ] `Views/POSView.xaml` - Add search popup
- [ ] `ViewModels/POSViewModel.cs` - Add search logic
- [ ] `ViewModels/Models/ProductSearchResult.cs` - NEW FILE
- [ ] `Views/POSView.xaml.cs` - Add keyboard navigation

### Implementation Checklist

#### Day 1 Tasks
- [ ] Create `ProductSearchResult.cs` model class
- [ ] Add `SearchText` property with debounce timer (300ms)
- [ ] Add `SearchResults` ObservableCollection
- [ ] Add `IsSearchDropdownOpen` property
- [ ] Add `IsSearching` property
- [ ] Implement `PerformSearchAsync()` method

#### Day 2 Tasks
- [ ] Update POSView.xaml with Popup control
- [ ] Style dropdown list items (image, name, code, price, stock)
- [ ] Add keyboard navigation (Up/Down arrows)
- [ ] Implement `ProcessSearchInputCommand`
- [ ] Test barcode fallback behavior
- [ ] Test with various search terms

### Testing Checklist
- [ ] Type "cok" - shows matching products within 300ms
- [ ] Type barcode + Enter - adds product directly
- [ ] Arrow Down - highlights next item
- [ ] Enter with selection - adds to order
- [ ] Escape - closes dropdown
- [ ] Click outside - closes dropdown
- [ ] No results - shows "No products found"
- [ ] Fast barcode scan - works correctly

### Completion
- [ ] Feature Complete
- [ ] Code Reviewed
- [ ] Tested on Touch Screen
- **Completed Date:** _______________

---

## Feature 2: Product Images in POS Grid

**Priority:** High | **Effort:** 1 day | **Assigned To:** _______________

### Description
Display product thumbnails in the POS grid instead of placeholder icons. Add image caching for performance.

### Files to Modify
- [ ] `Views/POSView.xaml` - Update product tile template
- [ ] `Services/ImageService.cs` - Add caching
- [ ] `ViewModels/ProductTileViewModel.cs` - Add IsLowStock

### Implementation Checklist

#### Day 3 Tasks
- [ ] Update ProductTileViewModel with `IsLowStock` property
- [ ] Add image caching to ImageService
- [ ] Update product tile XAML template
- [ ] Add "LOW" badge for low stock items
- [ ] Improve out-of-stock overlay
- [ ] Test image loading performance
- [ ] Test with missing images (placeholder)

### Testing Checklist
- [ ] Product with image - shows thumbnail
- [ ] Product without image - shows placeholder icon
- [ ] Out of stock - shows dark overlay with "OUT"
- [ ] Low stock - shows orange "LOW" badge
- [ ] Offer product - shows red "OFFER" badge
- [ ] Scrolling - images load smoothly
- [ ] Memory usage - reasonable after scrolling

### Completion
- [ ] Feature Complete
- [ ] Code Reviewed
- [ ] Tested on Touch Screen
- **Completed Date:** _______________

---

## Feature 3: Denomination Counting (Close Day)

**Priority:** High | **Effort:** 1 day | **Assigned To:** _______________

### Description
Add denomination counting grid to the Close Work Period dialog to help cashiers count cash by bill/coin type.

### Files to Modify
- [ ] `Views/Dialogs/CloseWorkPeriodDialog.xaml` - Add denomination grid
- [ ] `Views/Dialogs/CloseWorkPeriodDialog.xaml.cs` - Add calculation logic

### Implementation Checklist

#### Day 4 Tasks
- [ ] Add denomination grid UI (1000, 500, 200, 100, 50, 20, 10, coins)
- [ ] Add count input fields for each denomination
- [ ] Add per-row total calculation
- [ ] Add grand total calculation
- [ ] Auto-populate cash count field
- [ ] Add toggle to show/hide denomination grid
- [ ] Test calculations

### Denomination Values (Kenya Shillings)
| Denomination | Input Field | Subtotal Field |
|--------------|-------------|----------------|
| KSh 1,000 | D1000Count | D1000Total |
| KSh 500 | D500Count | D500Total |
| KSh 200 | D200Count | D200Total |
| KSh 100 | D100Count | D100Total |
| KSh 50 | D50Count | D50Total |
| KSh 20 | D20Count | D20Total |
| KSh 10 | D10Count | D10Total |
| Coins | CoinsCount | CoinsTotal |

### Testing Checklist
- [ ] Enter counts - subtotals calculate correctly
- [ ] Grand total updates on any change
- [ ] Cash count field auto-updates
- [ ] Toggle hide/show works
- [ ] Handles non-numeric input gracefully
- [ ] Variance calculates correctly with denominations

### Completion
- [ ] Feature Complete
- [ ] Code Reviewed
- [ ] Tested by Cashier
- **Completed Date:** _______________

---

## Feature 4: Split Payment

**Priority:** High | **Effort:** 3-4 days | **Assigned To:** _______________

### Description
Allow customers to pay with multiple payment methods (e.g., part Cash, part M-Pesa).

### Files to Create/Modify
- [ ] `Views/Dialogs/SplitPaymentDialog.xaml` - NEW FILE
- [ ] `Views/Dialogs/SplitPaymentDialog.xaml.cs` - NEW FILE
- [ ] `ViewModels/Dialogs/SplitPaymentDialogViewModel.cs` - NEW FILE
- [ ] `ViewModels/Models/PaymentEntry.cs` - NEW FILE
- [ ] `Views/POSView.xaml` - Add split payment button
- [ ] `ViewModels/POSViewModel.cs` - Add command
- [ ] `Services/OrderService.cs` - Handle multiple payments

### Implementation Checklist

#### Day 1 (Week 2) - UI Creation
- [ ] Create SplitPaymentDialog.xaml layout
- [ ] Add order total display
- [ ] Add payments list section
- [ ] Add "Add Payment" section with amount input
- [ ] Add payment method buttons (Cash, M-Pesa, Card)
- [ ] Add remaining balance footer
- [ ] Style all components

#### Day 2 (Week 2) - ViewModel
- [ ] Create PaymentEntry model
- [ ] Create SplitPaymentDialogViewModel
- [ ] Implement AddCashPayment command
- [ ] Implement AddMpesaPayment command
- [ ] Implement AddCardPayment command
- [ ] Implement RemovePayment command
- [ ] Implement balance calculations
- [ ] Implement quick amount buttons

#### Day 3 (Week 2) - Service Layer
- [ ] Modify Order entity for multiple payments
- [ ] Create PaymentRecord entity if needed
- [ ] Update OrderService to handle split payments
- [ ] Update receipt printing for split payments
- [ ] Handle M-Pesa STK for partial amount

#### Day 4 (Week 2) - Integration
- [ ] Add Split Payment button to POS
- [ ] Wire up OpenSplitPaymentCommand
- [ ] Handle dialog result
- [ ] Process split payment order
- [ ] Test full flow
- [ ] Handle edge cases

### Testing Checklist
- [ ] Dialog opens with correct order total
- [ ] Add cash payment - appears in list
- [ ] Add M-Pesa payment - STK triggers for amount
- [ ] Add card payment - appears in list
- [ ] Remove payment - updates balance
- [ ] Overpay prevention - caps at remaining
- [ ] Complete disabled until fully paid
- [ ] Complete processes all payments
- [ ] Receipt shows all payment methods
- [ ] Reports track payment breakdown

### Completion
- [ ] Feature Complete
- [ ] Code Reviewed
- [ ] Tested on Touch Screen
- [ ] Tested with Real Payments
- **Completed Date:** _______________

---

## Feature 5: Dashboard Sparklines

**Priority:** Medium | **Effort:** 1-2 days | **Assigned To:** _______________

### Description
Add mini trend charts (sparklines) to dashboard KPI cards showing hourly or daily trends.

### Prerequisites
- [ ] Install OxyPlot.Wpf NuGet package

### Files to Modify
- [ ] `HospitalityPOS.WPF.csproj` - Add NuGet
- [ ] `Views/DashboardView.xaml` - Add sparkline controls
- [ ] `ViewModels/DashboardViewModel.cs` - Add trend data

### Implementation Checklist

#### Day 5 (Week 2) Tasks
- [ ] Add OxyPlot.Wpf NuGet package
- [ ] Create reusable SparklineControl or style
- [ ] Add HourlySalesData property to ViewModel
- [ ] Fetch last 12 hours of sales data
- [ ] Integrate sparkline into Sales KPI card
- [ ] Add sparkline to Orders KPI card
- [ ] Test with real data
- [ ] Handle empty data gracefully

### Sparkline Specifications
| KPI Card | Data Source | Color |
|----------|-------------|-------|
| Today's Sales | Hourly sales totals | #22C55E (green) |
| Orders | Hourly order counts | #3B82F6 (blue) |
| Avg Order Value | Hourly averages | #F59E0B (amber) |

### Testing Checklist
- [ ] Sparklines render correctly
- [ ] Data updates on dashboard refresh
- [ ] Empty data shows flat line
- [ ] Performance acceptable
- [ ] Looks good on various screen sizes

### Completion
- [ ] Feature Complete
- [ ] Code Reviewed
- **Completed Date:** _______________

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
- Phase 1 Complete:

---

## Phase 1 Completion Summary

### Features Delivered
| Feature | Planned Effort | Actual Effort | Status |
|---------|----------------|---------------|--------|
| Real-Time Product Search | 2-3 days | ___ days | |
| Product Images in POS | 1 day | ___ days | |
| Denomination Counting | 1 day | ___ days | |
| Split Payment | 3-4 days | ___ days | |
| Dashboard Sparklines | 1-2 days | ___ days | |

### Lessons Learned
1.
2.
3.

### Carry-over Items (if any)
1.
2.

### Phase 2 Preparation
- [ ] Review Phase 2 features
- [ ] Update gap analysis if needed
- [ ] Prioritize next batch

---

## Quick Reference: File Locations

### Views
```
src/HospitalityPOS.WPF/Views/
├── POSView.xaml
├── DashboardView.xaml
└── Dialogs/
    ├── CloseWorkPeriodDialog.xaml
    ├── ProductSearchDialog.xaml
    └── SplitPaymentDialog.xaml (NEW)
```

### ViewModels
```
src/HospitalityPOS.WPF/ViewModels/
├── POSViewModel.cs
├── DashboardViewModel.cs
├── Models/
│   ├── ProductSearchResult.cs (NEW)
│   └── PaymentEntry.cs (NEW)
└── Dialogs/
    └── SplitPaymentDialogViewModel.cs (NEW)
```

### Services
```
src/HospitalityPOS.Infrastructure/Services/
├── ImageService.cs
└── OrderService.cs
```

---

## Related Documents

- [Gap Analysis](./gap-analysis-implementation-vs-best-practices.md)
- [Design Guidelines](./admin-interface-design-guidelines.md)
- [Implementation Specs (Detailed)](./implementation-specs-phase1.md)

---

**Document Version:** 1.0
**Last Updated:** January 2026
