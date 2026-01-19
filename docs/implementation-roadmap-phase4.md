# Implementation Roadmap - Phase 4: User Experience & Inventory

**Project:** HospitalityPOS WPF Application
**Phase:** 4 - UX Improvements & Inventory Enhancements
**Start Date:** _______________
**Target Completion:** 2 Weeks

---

## Progress Tracker

### Week 1 Progress
| Day | Feature | Status | Notes |
|-----|---------|--------|-------|
| Day 1 | Quick Access Favorites | [ ] Pending | Pinned products |
| Day 2 | Order Notes/Instructions | [ ] Pending | Special instructions |
| Day 3 | Low Stock Dashboard Widget | [ ] Pending | Alert summary |
| Day 4 | Reorder Suggestions | [ ] Pending | Auto-suggest reorders |
| Day 5 | Customer Search at POS | [ ] Pending | Customer lookup |

### Week 2 Progress
| Day | Feature | Status | Notes |
|-----|---------|--------|-------|
| Day 1 | Keyboard Shortcuts | [ ] Pending | Global hotkeys |
| Day 2 | Profit Margin Display | [ ] Pending | Real-time margins |
| Day 3 | System Health Dashboard | [ ] Pending | Status indicators |
| Day 4 | Enhanced Print Settings | [ ] Pending | Print configuration |
| Day 5 | Testing & Polish | [ ] Pending | Integration testing |

---

## Feature 1: Quick Access Favorites

**Priority:** High | **Effort:** 1 day | **Description:** Allow pinning frequently used products for quick access

### Files to Create/Modify
- [ ] `Core/Entities/ProductFavorite.cs` - NEW: Favorite entity
- [ ] `ViewModels/POSViewModel.cs` - Add favorites management
- [ ] `Views/POSView.xaml` - Add favorites panel
- [ ] `Infrastructure/Services/ProductService.cs` - Favorites CRUD

### Implementation Checklist
- [ ] Create ProductFavorite entity (UserId, ProductId, DisplayOrder)
- [ ] Add favorites panel to POS (collapsible top row)
- [ ] Implement "Add to Favorites" context menu
- [ ] Implement drag-drop reordering
- [ ] Persist favorites per user
- [ ] Quick-tap to add to order

### Testing Checklist
- [ ] Can add product to favorites
- [ ] Favorites persist between sessions
- [ ] Can remove from favorites
- [ ] Quick tap adds to order
- [ ] Different users have separate favorites

---

## Feature 2: Order Notes/Special Instructions

**Priority:** High | **Effort:** 1 day | **Description:** Add notes to individual order items and entire orders

### Files to Create/Modify
- [ ] `Core/Entities/OrderItem.cs` - Add Notes property (if not exists)
- [ ] `Core/Entities/Order.cs` - Add Notes property (if not exists)
- [ ] `ViewModels/POSViewModel.cs` - Add note commands
- [ ] `Views/POSView.xaml` - Add notes UI
- [ ] `Views/Dialogs/OrderItemNoteDialog.xaml` - NEW: Note dialog

### Implementation Checklist
- [ ] Add Notes property to OrderItem entity
- [ ] Create OrderItemNoteDialog for entering notes
- [ ] Add "Add Note" button to order item row
- [ ] Display note icon/indicator on items with notes
- [ ] Include notes in receipt printing
- [ ] Include notes in kitchen display (if enabled)

### Testing Checklist
- [ ] Can add note to item
- [ ] Note displays in order list
- [ ] Note prints on receipt
- [ ] Note sent to kitchen display
- [ ] Can edit/clear existing note

---

## Feature 3: Low Stock Dashboard Widget

**Priority:** High | **Effort:** 1 day | **Description:** Show low stock summary on dashboard

### Files to Create/Modify
- [ ] `ViewModels/DashboardViewModel.cs` - Add low stock data
- [ ] `Views/DashboardView.xaml` - Add widget section
- [ ] `Core/DTOs/LowStockAlertDto.cs` - NEW: Alert DTO

### Implementation Checklist
- [ ] Create LowStockAlertDto with product info and current/reorder levels
- [ ] Add GetLowStockAlertsAsync method to IInventoryService
- [ ] Query products where CurrentStock <= ReorderLevel
- [ ] Add widget to dashboard showing count and top 5 items
- [ ] Add "View All" link to inventory view
- [ ] Refresh on dashboard load

### Testing Checklist
- [ ] Widget shows correct count
- [ ] Top 5 low stock items displayed
- [ ] Click item navigates to product
- [ ] "View All" opens inventory view
- [ ] Updates after stock changes

---

## Feature 4: Reorder Suggestions

**Priority:** Medium | **Effort:** 1 day | **Description:** Auto-suggest products that need reordering

### Files to Create/Modify
- [ ] `ViewModels/PurchaseOrdersViewModel.cs` - Add suggestions
- [ ] `Views/PurchaseOrdersView.xaml` - Add suggestions panel
- [ ] `Core/DTOs/ReorderSuggestionDto.cs` - NEW: Suggestion DTO
- [ ] `Infrastructure/Services/PurchaseOrderService.cs` - Suggestion logic

### Implementation Checklist
- [ ] Create ReorderSuggestionDto (Product, Supplier, SuggestedQty, CurrentStock)
- [ ] Add GetReorderSuggestionsAsync method
- [ ] Calculate suggested qty based on reorder level and economic order qty
- [ ] Add collapsible suggestions panel to PO view
- [ ] "Create PO from Suggestions" button
- [ ] Group suggestions by supplier

### Testing Checklist
- [ ] Suggestions show low stock items
- [ ] Grouped by supplier correctly
- [ ] Create PO creates draft with items
- [ ] Suggested qty reasonable
- [ ] Can dismiss suggestions

---

## Feature 5: Customer Search at POS

**Priority:** High | **Effort:** 1 day | **Description:** Search and assign customer to order at POS

### Files to Create/Modify
- [ ] `ViewModels/POSViewModel.cs` - Add customer search
- [ ] `Views/POSView.xaml` - Add customer section
- [ ] `Views/Dialogs/CustomerSearchDialog.xaml` - NEW: Search dialog

### Implementation Checklist
- [ ] Create CustomerSearchDialog with search box and results
- [ ] Add "Assign Customer" button to POS header
- [ ] Show assigned customer name in header
- [ ] Link Order to CustomerId
- [ ] Display customer loyalty points (if enabled)
- [ ] "Clear Customer" option

### Testing Checklist
- [ ] Can search for customers
- [ ] Can assign customer to order
- [ ] Customer name shows in header
- [ ] Loyalty points display
- [ ] Can clear customer
- [ ] Customer saved with order

---

## Feature 6: Keyboard Shortcuts

**Priority:** Medium | **Effort:** 1 day | **Description:** Global keyboard shortcuts for common actions

### Files to Create/Modify
- [ ] `Views/MainWindow.xaml.cs` - Add key bindings
- [ ] `Views/POSView.xaml.cs` - Add POS-specific shortcuts
- [ ] `Services/ShortcutService.cs` - NEW: Shortcut management
- [ ] `Views/Dialogs/ShortcutsHelpDialog.xaml` - NEW: Help dialog

### Shortcuts to Implement
| Shortcut | Action |
|----------|--------|
| F1 | Open Help/Shortcuts |
| F2 | Focus Search Box |
| F3 | New Order |
| F4 | Park Order |
| F5 | Refresh |
| F8 | Open Cash Drawer |
| F9 | Void Last Item |
| F12 | Quick Pay (Cash) |
| Ctrl+P | Print Last Receipt |
| Ctrl+S | Settle Order |
| Esc | Cancel/Close Dialog |

### Testing Checklist
- [ ] All shortcuts work correctly
- [ ] F1 shows help dialog
- [ ] Shortcuts don't conflict
- [ ] Work in POS view
- [ ] Disabled when dialog open

---

## Feature 7: Profit Margin Display

**Priority:** Medium | **Effort:** 1 day | **Description:** Show profit margin on products and orders

### Files to Create/Modify
- [ ] `ViewModels/POSViewModel.cs` - Add margin calculations
- [ ] `Views/POSView.xaml` - Add margin display (manager only)
- [ ] `Core/Entities/Product.cs` - Ensure CostPrice exists
- [ ] `Converters/MarginColorConverter.cs` - NEW: Color coding

### Implementation Checklist
- [ ] Ensure CostPrice property on Product
- [ ] Calculate margin: (SalePrice - CostPrice) / SalePrice * 100
- [ ] Display margin % next to product in order list
- [ ] Color code: Red < 10%, Yellow 10-20%, Green > 20%
- [ ] Only visible to managers/admins
- [ ] Show order total margin in footer

### Testing Checklist
- [ ] Margin calculates correctly
- [ ] Color coding works
- [ ] Hidden from cashiers
- [ ] Visible to managers
- [ ] Order total margin accurate

---

## Feature 8: System Health Dashboard

**Priority:** Medium | **Effort:** 1 day | **Description:** Show system status indicators

### Files to Create/Modify
- [ ] `ViewModels/MainWindowViewModel.cs` - Add health checks
- [ ] `Views/MainWindow.xaml` - Add status bar
- [ ] `Services/SystemHealthService.cs` - NEW: Health checks
- [ ] `Core/DTOs/SystemHealthDto.cs` - NEW: Status DTO

### Health Checks to Implement
| Check | Indicator |
|-------|-----------|
| Database Connection | Green/Red dot |
| Printer Status | Green/Yellow/Red |
| Last Sync Time | Timestamp |
| Disk Space | Warning if < 1GB |
| Memory Usage | Warning if > 80% |

### Testing Checklist
- [ ] Database indicator works
- [ ] Printer status updates
- [ ] Warnings display correctly
- [ ] Clicking expands details
- [ ] Auto-refresh every 30s

---

## Feature 9: Enhanced Print Settings

**Priority:** Medium | **Effort:** 1 day | **Description:** Configure receipt and report printing

### Files to Create/Modify
- [ ] `Views/SettingsView.xaml` - Add print settings section
- [ ] `ViewModels/SettingsViewModel.cs` - Print configuration
- [ ] `Core/Entities/PrintConfiguration.cs` - NEW: Print settings
- [ ] `Services/PrintService.cs` - Apply settings

### Settings to Include
- Default receipt printer selection
- Default report printer selection
- Receipt copies count
- Auto-print on settlement (on/off)
- Receipt width (58mm/80mm)
- Print logo (on/off)
- Print footer message
- Kitchen ticket printer

### Testing Checklist
- [ ] Can select printers
- [ ] Receipt copies setting works
- [ ] Auto-print toggle works
- [ ] Width setting affects layout
- [ ] Logo setting works
- [ ] Footer message prints

---

## Phase 4 Completion Summary

### Features Delivered
| Feature | Planned Effort | Actual Effort | Status |
|---------|----------------|---------------|--------|
| Quick Access Favorites | 1 day | ___ days | |
| Order Notes | 1 day | ___ days | |
| Low Stock Widget | 1 day | ___ days | |
| Reorder Suggestions | 1 day | ___ days | |
| Customer Search at POS | 1 day | ___ days | |
| Keyboard Shortcuts | 1 day | ___ days | |
| Profit Margin Display | 1 day | ___ days | |
| System Health Dashboard | 1 day | ___ days | |
| Enhanced Print Settings | 1 day | ___ days | |

---

**Document Version:** 1.0
**Last Updated:** January 2026
