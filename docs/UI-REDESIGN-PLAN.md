# HospitalityPOS UI Redesign Implementation Plan

## Overview

This document outlines the comprehensive UI redesign for the HospitalityPOS system, based on industry best practices from Microsoft RMS, Lightspeed, Square, Oracle Xstore, and KORONA POS systems.

## Current State Issues

- No clear navigation structure for management features
- Missing dedicated sections for: Stock Management, Reports, Workday Operations
- Sales screen lacks modern 3-column layout
- No visual hierarchy or consistent design language
- Missing day open/close functionality in UI

---

## Phase 1: Navigation & Shell Restructure

### 1.1 Main Window Shell Redesign

Replace current navigation with a modern sidebar-based layout:

```
┌──────────────────────────────────────────────────────────────────────────┐
│ 🏪 HOSPITALITY POS                    [Store Name] | [User] | [Logout]   │
├─────────────────┬────────────────────────────────────────────────────────┤
│                 │                                                        │
│  SIDEBAR NAV    │              CONTENT AREA                              │
│  (Collapsible)  │                                                        │
│                 │                                                        │
└─────────────────┴────────────────────────────────────────────────────────┘
```

### 1.2 Navigation Menu Structure

```
📊 Dashboard          <- Landing page with KPIs
💳 Point of Sale      <- Sales screen (existing POSView)
📦 Inventory          <- NEW: Stock management section
   ├─ Stock Levels
   ├─ Receive Stock
   ├─ Stock Adjustments
   ├─ Stock Count
   └─ Suppliers
🕐 Workday            <- NEW: Day operations
   ├─ Open Day
   ├─ Close Day
   ├─ Shift Management
   └─ Cash Management
📈 Reports            <- NEW: Comprehensive reports
   ├─ Sales Reports
   ├─ Inventory Reports
   ├─ Financial Reports
   └─ Employee Reports
👥 Employees          <- Staff management
   ├─ Staff List
   ├─ Roles & Permissions
   └─ Time Clock
👤 Customers          <- Customer/Loyalty
   ├─ Customer Directory
   └─ Loyalty Program
🛒 Products           <- Product catalog
   ├─ Product List
   ├─ Categories
   └─ Promotions
⚙️ Settings           <- System configuration
   ├─ Store Settings
   ├─ Register Settings
   ├─ Receipt Settings
   └─ Tax Configuration
```

### 1.3 Files to Create/Modify

| File | Action | Purpose |
|------|--------|---------|
| `Views/Shell/MainShell.xaml` | CREATE | New main window shell with sidebar |
| `Views/Shell/SidebarMenu.xaml` | CREATE | Collapsible sidebar navigation |
| `Views/Shell/TopBar.xaml` | CREATE | Header with user info and actions |
| `ViewModels/ShellViewModel.cs` | CREATE | Navigation state management |
| `MainWindow.xaml` | MODIFY | Integrate new shell |

---

## Phase 2: Dashboard View

### 2.1 Dashboard Layout

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           DASHBOARD                                      │
├─────────────┬─────────────┬─────────────┬─────────────────────────────────┤
│ Today Sales │ Transactions│ Avg Ticket  │ Active Register                │
│   $4,523    │     142     │   $31.85    │     POS-001 ✓                  │
├─────────────┴─────────────┴─────────────┴─────────────────────────────────┤
│                                                                          │
│  ┌─ SALES TODAY ──────────────────┐  ┌─ ALERTS ────────────────────────┐│
│  │ [Hourly Sales Bar Chart]       │  │ ⚠️ 12 items low on stock        ││
│  │                                │  │ ⚠️ 3 items out of stock         ││
│  │ 8AM ██                         │  │ ✓ Day opened at 8:00 AM         ││
│  │ 9AM ████                       │  │ 💰 Cash drop recommended        ││
│  │ 10AM ██████                    │  │                                 ││
│  │ 11AM ████████                  │  └─────────────────────────────────┘│
│  │ 12PM ██████████████            │                                     │
│  └────────────────────────────────┘  ┌─ QUICK ACTIONS ─────────────────┐│
│                                      │ [Open POS] [View Reports]       ││
│  ┌─ TOP PRODUCTS ─────────────────┐  │ [Stock Check] [Close Day]       ││
│  │ 1. Product A - 45 sold         │  └─────────────────────────────────┘│
│  │ 2. Product B - 38 sold         │                                     │
│  │ 3. Product C - 32 sold         │                                     │
│  └────────────────────────────────┘                                     │
└─────────────────────────────────────────────────────────────────────────┘
```

### 2.2 Files to Create

| File | Action | Purpose |
|------|--------|---------|
| `Views/Dashboard/DashboardView.xaml` | CREATE | Main dashboard layout |
| `Views/Dashboard/KpiCard.xaml` | CREATE | Reusable KPI card component |
| `Views/Dashboard/AlertsPanel.xaml` | CREATE | System alerts panel |
| `ViewModels/DashboardViewModel.cs` | CREATE | Dashboard data and logic |

---

## Phase 3: POS Sales Screen Redesign

### 3.1 New 3-Column Layout

```
┌─────────────────────────────────────────────────────────────────────────┐
│  🔍 Search products...    [Barcode: ________]     Register: POS-001     │
├────────────┬────────────────────────────────────┬───────────────────────┤
│            │                                    │                       │
│ CATEGORIES │        PRODUCT GRID                │   ORDER SUMMARY       │
│            │                                    │                       │
│ [All]      │  ┌──────┐ ┌──────┐ ┌──────┐       │  Customer: Walk-in    │
│ [Produce]  │  │ 🍎   │ │ 🍞   │ │ 🥛   │       │  ─────────────────    │
│ [Dairy]    │  │Apple │ │Bread │ │Milk  │       │  Apple x2    $3.98    │
│ [Bakery]   │  │$1.99 │ │$2.49 │ │$3.99 │       │  Bread x1    $2.49    │
│ [Meat]     │  └──────┘ └──────┘ └──────┘       │  Milk x1     $3.99    │
│ [Frozen]   │  ┌──────┐ ┌──────┐ ┌──────┐       │                       │
│ [Beverages]│  │ 🧀   │ │ 🥚   │ │ 🍗   │       │  ─────────────────    │
│ [Snacks]   │  │Cheese│ │Eggs  │ │Chicken│      │  Subtotal:   $10.46   │
│ [Household]│  │$4.99 │ │$2.99 │ │$7.99 │       │  Tax (8%):    $0.84   │
│            │  └──────┘ └──────┘ └──────┘       │  ═════════════════    │
│ ─────────  │                                    │  TOTAL:      $11.30   │
│ [Grid View]│  < 1 2 3 4 5 >                     │                       │
│ [List View]│                                    │  [HOLD] [CLEAR ITEM]  │
│            │                                    │  [DISCOUNT] [CUSTOMER]│
├────────────┴────────────────────────────────────┼───────────────────────┤
│ [Void] [Return] [Price Check] [Suspend] [Recall]│ [💵 CASH] [💳 CARD]  │
│ [Manager] [Drawer] [No Sale]                    │     [PAY NOW]         │
└─────────────────────────────────────────────────┴───────────────────────┘
```

### 3.2 Key Improvements

1. **Categories Panel (Left)**: Vertical list with icons, scrollable
2. **Product Grid (Center)**:
   - Grid/List view toggle
   - Product images with name and price
   - Pagination or infinite scroll
   - Search bar with barcode input
3. **Order Summary (Right)**:
   - Current customer info
   - Line items with quantity controls
   - Running totals
   - Quick action buttons
4. **Action Bar (Bottom)**:
   - Transaction functions (left)
   - Payment buttons (right, prominent)

### 3.3 Files to Modify/Create

| File | Action | Purpose |
|------|--------|---------|
| `Views/POSView.xaml` | MAJOR MODIFY | Implement 3-column layout |
| `Views/POS/CategoryPanel.xaml` | CREATE | Category navigation |
| `Views/POS/ProductGrid.xaml` | CREATE | Product display grid |
| `Views/POS/OrderSummary.xaml` | CREATE | Cart/order panel |
| `Views/POS/PaymentPanel.xaml` | CREATE | Payment buttons |
| `Controls/ProductCard.xaml` | CREATE | Reusable product card |

---

## Phase 4: Inventory Management Module

### 4.1 Stock Levels View

```
┌─────────────────────────────────────────────────────────────────────────┐
│  INVENTORY > Stock Levels                        [Export] [Print]       │
├─────────────────────────────────────────────────────────────────────────┤
│  🔍 Search...   Category: [All ▼]   Status: [All ▼]   [Filter]         │
├─────────────────────────────────────────────────────────────────────────┤
│  ┌─ SUMMARY ────────────────────────────────────────────────────────┐  │
│  │ Total Items: 1,245  │  In Stock: 1,180  │  Low: 52  │  Out: 13   │  │
│  └──────────────────────────────────────────────────────────────────┘  │
├──────┬──────────────┬──────────┬─────────┬─────────┬─────────┬─────────┤
│ SKU  │ Product      │ Category │ On Hand │ Min Qty │ Status  │ Actions │
├──────┼──────────────┼──────────┼─────────┼─────────┼─────────┼─────────┤
│ 1001 │ Apple        │ Produce  │    45   │   20    │ ✓ OK    │ [Edit]  │
│ 1002 │ Milk 1L      │ Dairy    │     8   │   15    │ ⚠️ Low  │ [Edit]  │
│ 1003 │ Bread White  │ Bakery   │     0   │   10    │ ❌ Out  │ [Edit]  │
└──────┴──────────────┴──────────┴─────────┴─────────┴─────────┴─────────┘
│  Showing 1-25 of 1,245                           < 1 2 3 4 5 ... 50 >  │
└─────────────────────────────────────────────────────────────────────────┘
```

### 4.2 Receive Stock View

```
┌─────────────────────────────────────────────────────────────────────────┐
│  INVENTORY > Receive Stock                                              │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Supplier: [Select Supplier ▼]        PO Number: [____________]         │
│  Date: [01/18/2026]                   Reference: [____________]         │
│                                                                         │
├─────────────────────────────────────────────────────────────────────────┤
│  🔍 Scan or search product...                          [Add Item]       │
├──────┬──────────────┬──────────┬───────────┬───────────┬────────────────┤
│ SKU  │ Product      │ Current  │ Receiving │ New Total │ Actions        │
├──────┼──────────────┼──────────┼───────────┼───────────┼────────────────┤
│ 1001 │ Apple        │    45    │   [50]    │    95     │ [Remove]       │
│ 1002 │ Milk 1L      │     8    │   [24]    │    32     │ [Remove]       │
└──────┴──────────────┴──────────┴───────────┴───────────┴────────────────┘
│                                                                         │
│                              [Cancel]    [Save Draft]    [Complete ✓]   │
└─────────────────────────────────────────────────────────────────────────┘
```

### 4.3 Files to Create

| File | Action | Purpose |
|------|--------|---------|
| `Views/Inventory/StockLevelsView.xaml` | CREATE | Main stock view |
| `Views/Inventory/ReceiveStockView.xaml` | CREATE | Stock receiving |
| `Views/Inventory/StockAdjustmentView.xaml` | CREATE | Adjustments |
| `Views/Inventory/StockCountView.xaml` | CREATE | Physical count |
| `Views/Inventory/SuppliersView.xaml` | CREATE | Supplier management |
| `ViewModels/Inventory/StockLevelsViewModel.cs` | CREATE | Stock logic |
| `ViewModels/Inventory/ReceiveStockViewModel.cs` | CREATE | Receiving logic |

---

## Phase 5: Workday Management Module

### 5.1 Open Day View

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         🌅 OPEN BUSINESS DAY                            │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│   Date: January 18, 2026              Store: Main Store                 │
│   Register: POS-001                   Manager: [Select ▼]               │
│                                                                         │
│  ┌─ OPENING FLOAT ─────────────────────────────────────────────────┐   │
│  │                                                                  │   │
│  │   Denomination          Count           Amount                   │   │
│  │   ─────────────────────────────────────────────────             │   │
│  │   $100 bills            [  0 ]          $0.00                   │   │
│  │   $50 bills             [  2 ]          $100.00                 │   │
│  │   $20 bills             [  5 ]          $100.00                 │   │
│  │   $10 bills             [  5 ]          $50.00                  │   │
│  │   $5 bills              [  10]          $50.00                  │   │
│  │   $1 bills              [  20]          $20.00                  │   │
│  │   Coins                 [$30.00]        $30.00                  │   │
│  │   ─────────────────────────────────────────────────             │   │
│  │   TOTAL OPENING FLOAT:                  $350.00                 │   │
│  │                                                                  │   │
│  └──────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│   [x] I confirm the opening float has been counted and verified         │
│   [x] Opening checklist completed                                       │
│                                                                         │
│                    [Cancel]                    [OPEN DAY ✓]             │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 5.2 Close Day View (Z-Report)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         🌙 CLOSE BUSINESS DAY                           │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│   Day Opened: 8:00 AM                 Current: 10:00 PM                 │
│   Register: POS-001                   Manager: John Smith               │
│                                                                         │
│  ┌─ TODAY'S SUMMARY ───────────────────────────────────────────────┐   │
│  │                                                                  │   │
│  │   Gross Sales:                    $4,523.45                     │   │
│  │   Returns:                          -$125.00                    │   │
│  │   Discounts:                         -$89.50                    │   │
│  │   ─────────────────────────────────────────────                 │   │
│  │   Net Sales:                      $4,308.95                     │   │
│  │   Tax Collected:                    $345.72                     │   │
│  │   ─────────────────────────────────────────────                 │   │
│  │   Total Transactions:                  142                      │   │
│  │   Average Ticket:                   $30.34                      │   │
│  │                                                                  │   │
│  └──────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ┌─ PAYMENT RECONCILIATION ────────────────────────────────────────┐   │
│  │                                                                  │   │
│  │   Payment Type      Expected        Counted         Variance    │   │
│  │   ─────────────────────────────────────────────────────────     │   │
│  │   Cash              $1,234.56       [$1,234.00]     -$0.56 ⚠️   │   │
│  │   Credit Card       $2,545.67       [$2,545.67]      $0.00 ✓    │   │
│  │   Debit Card          $528.72       [$  528.72]      $0.00 ✓    │   │
│  │   ─────────────────────────────────────────────────────────     │   │
│  │   TOTAL             $4,308.95       $4,308.39        -$0.56     │   │
│  │                                                                  │   │
│  └──────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│   Variance Note (required if variance): [_________________________]     │
│                                                                         │
│   [Print X-Report]   [Print Z-Report]                                   │
│                                                                         │
│                    [Cancel]                    [CLOSE DAY ✓]            │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 5.3 Files to Create

| File | Action | Purpose |
|------|--------|---------|
| `Views/Workday/OpenDayView.xaml` | CREATE | Day opening wizard |
| `Views/Workday/CloseDayView.xaml` | CREATE | Day closing/Z-report |
| `Views/Workday/ShiftManagementView.xaml` | CREATE | Shift overview |
| `Views/Workday/CashManagementView.xaml` | CREATE | Cash drops/pickups |
| `ViewModels/Workday/OpenDayViewModel.cs` | CREATE | Opening logic |
| `ViewModels/Workday/CloseDayViewModel.cs` | CREATE | Closing logic |
| `Models/WorkdaySession.cs` | CREATE | Workday data model |
| `Services/WorkdayService.cs` | CREATE | Business logic |

---

## Phase 6: Reports Module

### 6.1 Reports Dashboard

```
┌─────────────────────────────────────────────────────────────────────────┐
│  REPORTS                                                                │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌─ SALES REPORTS ─────────────────────────────────────────────────┐   │
│  │ [Daily Sales]  [Sales by Hour]  [Sales by Category]             │   │
│  │ [Sales by Employee]  [Sales by Payment Type]  [Voided Sales]    │   │
│  └──────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ┌─ INVENTORY REPORTS ─────────────────────────────────────────────┐   │
│  │ [Stock on Hand]  [Low Stock Report]  [Stock Movement]           │   │
│  │ [Inventory Valuation]  [Receiving History]  [Dead Stock]        │   │
│  └──────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ┌─ FINANCIAL REPORTS ─────────────────────────────────────────────┐   │
│  │ [Daily Summary]  [Payment Summary]  [Tax Report]                │   │
│  │ [Profit & Loss]  [Cost of Goods Sold]                           │   │
│  └──────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ┌─ EMPLOYEE REPORTS ──────────────────────────────────────────────┐   │
│  │ [Time Clock]  [Sales by Employee]  [Cash Variance by Employee]  │   │
│  └──────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ┌─ WORKDAY REPORTS ───────────────────────────────────────────────┐   │
│  │ [X-Report (Current)]  [Z-Report History]  [Shift Summary]       │   │
│  └──────────────────────────────────────────────────────────────────┘   │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 6.2 Files to Create

| File | Action | Purpose |
|------|--------|---------|
| `Views/Reports/ReportsView.xaml` | CREATE | Reports dashboard |
| `Views/Reports/SalesReportView.xaml` | CREATE | Sales reports |
| `Views/Reports/InventoryReportView.xaml` | CREATE | Inventory reports |
| `Views/Reports/FinancialReportView.xaml` | CREATE | Financial reports |
| `Views/Reports/EmployeeReportView.xaml` | CREATE | Employee reports |
| `ViewModels/Reports/ReportsViewModel.cs` | CREATE | Reports logic |
| `Services/ReportService.cs` | CREATE | Report generation |

---

## Phase 7: Shared Components & Styling

### 7.1 Design System

Create consistent UI components:

| Component | Purpose |
|-----------|---------|
| `Controls/SidebarMenuItem.xaml` | Navigation menu item |
| `Controls/KpiCard.xaml` | Dashboard metric card |
| `Controls/DataGrid.xaml` | Styled data grid |
| `Controls/ActionButton.xaml` | Consistent button styles |
| `Controls/SearchBox.xaml` | Search input with icon |
| `Controls/StatusBadge.xaml` | Status indicators |
| `Controls/Modal.xaml` | Dialog/modal wrapper |

### 7.2 Color Palette

```
Primary:        #2563EB (Blue)
Secondary:      #64748B (Slate)
Success:        #22C55E (Green)
Warning:        #F59E0B (Amber)
Danger:         #EF4444 (Red)
Background:     #F8FAFC (Light) / #1E293B (Dark)
Surface:        #FFFFFF (Light) / #334155 (Dark)
Text Primary:   #1E293B (Light) / #F8FAFC (Dark)
Text Secondary: #64748B
Border:         #E2E8F0 (Light) / #475569 (Dark)
```

### 7.3 Files to Create/Modify

| File | Action | Purpose |
|------|--------|---------|
| `Themes/Colors.xaml` | CREATE | Color definitions |
| `Themes/Typography.xaml` | CREATE | Font styles |
| `Themes/Controls.xaml` | CREATE | Control templates |
| `Themes/DarkTheme.xaml` | CREATE | Dark mode theme |
| `Themes/LightTheme.xaml` | CREATE | Light mode theme |

---

## Implementation Order

### Sprint 1: Foundation (Shell & Navigation)
1. Create new MainShell with sidebar
2. Implement navigation system
3. Create basic Dashboard view
4. Update routing/navigation logic

### Sprint 2: POS Screen Redesign
1. Implement 3-column layout
2. Create CategoryPanel component
3. Create ProductGrid component
4. Create OrderSummary component
5. Implement payment flow

### Sprint 3: Workday Management
1. Create Open Day view and logic
2. Create Close Day view and logic
3. Implement X/Z report generation
4. Create shift management view

### Sprint 4: Inventory Module
1. Create Stock Levels view
2. Create Receive Stock view
3. Create Stock Adjustment view
4. Create Suppliers view

### Sprint 5: Reports Module
1. Create Reports dashboard
2. Implement Sales reports
3. Implement Inventory reports
4. Implement Financial reports

### Sprint 6: Polish & Theming
1. Implement dark/light theme toggle
2. Add animations and transitions
3. Accessibility improvements
4. Performance optimization

---

## Database Changes Required

### New Tables

```sql
-- Workday sessions
CREATE TABLE WorkdaySessions (
    Id INT PRIMARY KEY IDENTITY,
    StoreId INT NOT NULL,
    RegisterId INT NOT NULL,
    OpenedAt DATETIME NOT NULL,
    ClosedAt DATETIME NULL,
    OpenedById INT NOT NULL,
    ClosedById INT NULL,
    OpeningFloat DECIMAL(18,2) NOT NULL,
    ClosingCash DECIMAL(18,2) NULL,
    ExpectedCash DECIMAL(18,2) NULL,
    Variance DECIMAL(18,2) NULL,
    VarianceNote NVARCHAR(500) NULL,
    Status NVARCHAR(20) NOT NULL, -- Open, Closed
    FOREIGN KEY (OpenedById) REFERENCES Users(Id),
    FOREIGN KEY (ClosedById) REFERENCES Users(Id)
);

-- Cash movements
CREATE TABLE CashMovements (
    Id INT PRIMARY KEY IDENTITY,
    WorkdaySessionId INT NOT NULL,
    Type NVARCHAR(20) NOT NULL, -- Drop, Pickup, Payout
    Amount DECIMAL(18,2) NOT NULL,
    Reason NVARCHAR(500),
    PerformedById INT NOT NULL,
    PerformedAt DATETIME NOT NULL,
    FOREIGN KEY (WorkdaySessionId) REFERENCES WorkdaySessions(Id),
    FOREIGN KEY (PerformedById) REFERENCES Users(Id)
);

-- Stock receiving
CREATE TABLE StockReceivings (
    Id INT PRIMARY KEY IDENTITY,
    SupplierId INT NULL,
    PONumber NVARCHAR(50),
    Reference NVARCHAR(100),
    ReceivedAt DATETIME NOT NULL,
    ReceivedById INT NOT NULL,
    Status NVARCHAR(20) NOT NULL,
    Notes NVARCHAR(1000),
    FOREIGN KEY (SupplierId) REFERENCES Suppliers(Id),
    FOREIGN KEY (ReceivedById) REFERENCES Users(Id)
);

CREATE TABLE StockReceivingItems (
    Id INT PRIMARY KEY IDENTITY,
    StockReceivingId INT NOT NULL,
    ProductId INT NOT NULL,
    QuantityReceived INT NOT NULL,
    CostPrice DECIMAL(18,2),
    FOREIGN KEY (StockReceivingId) REFERENCES StockReceivings(Id),
    FOREIGN KEY (ProductId) REFERENCES Products(Id)
);
```

---

## Success Metrics

- Navigation: Users can find any feature within 2 clicks
- POS: Transaction completion time reduced by 20%
- Training: New staff productive within 1 hour
- Errors: Cash variance incidents reduced by 50%
- Adoption: All daily operations use new workflow

---

## Notes

- Maintain backwards compatibility during transition
- Implement feature flags for gradual rollout
- Ensure touch-friendly design (44px minimum touch targets)
- Test with actual POS hardware (touchscreens, barcode scanners)
