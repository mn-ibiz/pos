# Admin Interface Design Guidelines

> Design best practices for the ProNet POS Admin Interface features.
> Research compiled from industry standards, UX best practices, and POS-specific guidelines.

---

## Design Philosophy

**Core Principle:** Maximize working space with a left sidebar navigation only. The remaining screen real estate is dedicated entirely to the main content area for operations.

### Cross-Cutting Design Principles

| Principle | Implementation |
|-----------|----------------|
| **Left sidebar only** | Collapsible to icons (60px) for more screen real estate |
| **Content area dominates** | Full remaining width for operations |
| **Contextual actions** | Toolbar/action buttons within content area, not additional sidebars |
| **Modal dialogs** | For create/edit forms rather than full page navigations |
| **Data tables with inline actions** | Minimize navigation depth |
| **Consistent spacing** | 16px/24px grid system throughout |
| **Dark theme optimized** | Reduce eye strain for extended use |

---

## Feature 1: Dashboard

**Location:** MAIN GROUP
**Purpose:** Central command view showing real-time business KPIs and operational status

### Layout Best Practices

- **F/Z-pattern scanning:** Users scan top-left to right, then down
- **Most critical KPIs top-left:** This is the most-viewed spot
- **Progressive disclosure:** Overview first → zoom/filter → details-on-demand
- **Limit to 2-3 views max:** Avoid dashboard clutter

### Component Guidelines

| Component | Best Practice |
|-----------|---------------|
| **Big Numbers** | Display key metrics prominently (large font, bold) for at-a-glance assessment |
| **Line Charts** | Use for trends over time (daily/weekly/monthly sales) |
| **Bar Charts** | Use for category comparisons (sales by product, by employee) |
| **Color Coding** | Red = alerts/negative, Green = success/positive, Yellow = warnings |

### KPI Categories

1. **Strategic KPIs:** Revenue growth, profit margins, customer lifetime value
2. **Operational KPIs:** Real-time sales, orders in queue, labor cost %
3. **Analytical KPIs:** Drill-down data, comparative analysis

### Recommended Widgets

```
┌─────────────────────────────────────────────────────────────────┐
│  TODAY'S SALES    │  ORDERS TODAY   │  AVG ORDER VALUE  │  Δ%  │
│  KES 125,400      │  47             │  KES 2,668        │ +12% │
├───────────────────┴─────────────────┴───────────────────┴──────┤
│  [═══════════════ SALES TREND CHART (7 DAYS) ═══════════════]  │
├────────────────────────────┬────────────────────────────────────┤
│  TOP PRODUCTS              │  PAYMENT METHODS BREAKDOWN         │
│  1. Product A - KES 23K    │  [PIE: Cash 45%, Card 35%, M-Pesa] │
│  2. Product B - KES 18K    │                                    │
├────────────────────────────┴────────────────────────────────────┤
│  LOW STOCK ALERTS (5)      │  RECENT TRANSACTIONS              │
└─────────────────────────────────────────────────────────────────┘
```

### Technical Requirements

- Real-time updates (WebSocket or 30-second polling)
- Skeleton loaders during data fetch
- Date range picker with presets (Today, This Week, This Month, Custom)
- Compare to prior period toggle
- Filter by location (for multi-location setups)

---

## Feature 2: Point of Sale

**Location:** MAIN GROUP
**Purpose:** Primary transaction interface for processing sales

### Critical UX Factors

| Factor | Consideration |
|--------|---------------|
| **Viewing Distance** | Cashiers view from 80cm+, not standard 40cm |
| **Time Pressure** | Double the tapping speed of normal apps required |
| **Attention Splitting** | Cashiers divide focus: screen ↔ customer ↔ store |
| **Physical Context** | Interface is part of larger physical setup (scanner, receipt printer, card terminal) |

### Touch Optimization

- **Minimum button size:** 44px × 44px (preferably 48px+)
- **Button spacing:** Minimum 8px gap between interactive elements
- **Touch feedback:** Immediate visual response (color change, ripple effect)
- **No hover states:** Touch interfaces don't have hover

### Layout Structure

```
┌─────────────────────────────────────────────────────────────────┐
│ [SEARCH BAR 🔍] [SCAN 📷]                    [TABLE/ORDER #]    │
├─────────────────────────────────┬───────────────────────────────┤
│                                 │  ORDER SUMMARY                │
│  ┌─────┐ ┌─────┐ ┌─────┐      │  ─────────────────────────     │
│  │ CAT │ │ CAT │ │ CAT │ ...  │  Item 1          KES 500       │
│  │  1  │ │  2  │ │  3  │      │  Item 2 (x2)     KES 800       │
│  └─────┘ └─────┘ └─────┘      │  Item 3          KES 350       │
│  ┌─────┐ ┌─────┐ ┌─────┐      │                                │
│  │PROD │ │PROD │ │PROD │      │  ─────────────────────────     │
│  │ $   │ │ $   │ │ $   │      │  Subtotal        KES 1,650     │
│  └─────┘ └─────┘ └─────┘      │  Tax (16%)       KES 264       │
│  ┌─────┐ ┌─────┐ ┌─────┐      │  ─────────────────────────     │
│  │PROD │ │PROD │ │PROD │      │  TOTAL           KES 1,914     │
│  │ $   │ │ $   │ │ $   │      │                                │
│  └─────┘ └─────┘ └─────┘      │  [    CLEAR    ] [    PAY    ] │
│                                 │         ↑ Secondary  ↑ Primary │
└─────────────────────────────────┴───────────────────────────────┘
```

### Payment Interface Best Practices

- **Button hierarchy:** Most used button (PAY/Continue) is largest and most prominent
- **Clear icons with labels:** Never icons alone - always pair with text
- **Preset tip amounts:** Offer 10%, 15%, 20% buttons instead of manual entry
- **Split payments:** System handles calculations; user only inputs split count
- **Animations:** ONLY for feedback (processing spinner), never decorative

### Form Design (Card Entry, Customer Info)

- **Single column layout:** Vertical stacking of fields
- **Labels above fields:** Not inline or floating
- **Large input fields:** Easy to tap and read
- **Auto-advance:** Move to next field automatically when complete

### Handedness Consideration

- Consider dominant hand placement for primary actions
- PAY button typically on right side (right-hand dominant)
- Configurable for left-handed cashiers

---

## Feature 3: Stock Levels

**Location:** INVENTORY GROUP
**Purpose:** Real-time view of inventory quantities with alerts for low stock

### Visual Alert System

| Status | Color | Trigger |
|--------|-------|---------|
| **Critical/Stockout** | Red (#E53935) | Quantity = 0 |
| **Low Stock** | Yellow/Amber (#FFB300) | Quantity ≤ Reorder Point |
| **Healthy** | Green (#43A047) | Quantity > Reorder Point |
| **Overstocked** | Blue (#1E88E5) | Quantity > Max Level (optional) |

### Reorder Point Formula

```
Reorder Point = (Average Daily Sales × Lead Time in Days) + Safety Stock
```

### Table View Structure

| Column | Description | Sortable | Filterable |
|--------|-------------|----------|------------|
| SKU | Stock keeping unit | Yes | Yes |
| Product Name | Item name with image thumbnail | Yes | Yes |
| Category | Product category | Yes | Yes |
| On Hand | Current quantity | Yes | Yes (range) |
| Reorder Point | Trigger level | Yes | No |
| Status | Visual badge (Critical/Low/OK) | Yes | Yes |
| Last Movement | Date of last transaction | Yes | Yes (date range) |
| Supplier | Primary supplier | Yes | Yes |

### Key Features

- **ABC Analysis highlighting:** Visually distinguish A-items (top 20% SKUs generating 70-80% revenue)
- **Progress bar indicators:** Visual meter showing stock level relative to reorder point
- **Bulk actions:** Select multiple → Reorder, Export, Print Labels
- **Drill-down:** Click item → Movement history, pending orders, supplier info
- **Quick filters:** Category, Status, Location, Supplier

### Mobile Alerts

- Push notifications for critical low stock (configurable threshold)
- Daily low stock summary email option

### Layout

```
┌─────────────────────────────────────────────────────────────────┐
│  STOCK LEVELS                                [+ Add Item] [⋮]   │
├─────────────────────────────────────────────────────────────────┤
│  [🔍 Search by SKU or name...]  [Category ▼] [Status ▼] [📥]   │
├─────────────────────────────────────────────────────────────────┤
│  □  SKU      NAME           CATEGORY   ON HAND   STATUS   ACT  │
│  ─────────────────────────────────────────────────────────────  │
│  □  SKU001   Product One    Beverages  ████░░ 45  🟢 OK    [⋮] │
│  □  SKU002   Product Two    Food       ██░░░░ 12  🟡 LOW   [⋮] │
│  □  SKU003   Product Three  Beverages  ░░░░░░ 0   🔴 OUT   [⋮] │
├─────────────────────────────────────────────────────────────────┤
│  Showing 1-25 of 342 items                    [< 1 2 3 ... 14 >]│
└─────────────────────────────────────────────────────────────────┘
```

---

## Feature 4: Receive Stock (Goods Receiving)

**Location:** INVENTORY GROUP
**Purpose:** Process incoming inventory from suppliers against purchase orders

### Workflow Design

**Recommended 3-Step Flow (for traceability):**

```
┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│   1. INPUT   │ →  │  2. QUALITY  │ →  │   3. STOCK   │
│   Receive    │    │   Control    │    │   Putaway    │
│   at dock    │    │   Inspect    │    │   to shelf   │
└──────────────┘    └──────────────┘    └──────────────┘
```

**Simplified 1-Step Flow (for small operations):**
- Receive directly into stock

### Core Features

| Feature | Implementation |
|---------|----------------|
| **Scan-driven workflow** | Barcode scanning at every touchpoint |
| **PO Matching** | Link receiving to Purchase Order, show expected vs received |
| **Variance capture** | Easy input for received/rejected quantities with reason codes |
| **Real-time updates** | Instant stock level updates upon validation |

### Receiving Form Layout

```
┌─────────────────────────────────────────────────────────────────┐
│  RECEIVE STOCK                                        [Cancel]  │
├─────────────────────────────────────────────────────────────────┤
│  Purchase Order: [PO-2024-0042 ▼]     Supplier: ABC Supplies   │
│  Expected Date: 2024-01-15            Status: ● Pending Receipt │
├─────────────────────────────────────────────────────────────────┤
│  [📷 Scan Item]  [🔍 Search...]                                 │
├─────────────────────────────────────────────────────────────────┤
│  ITEM             EXPECTED   RECEIVED   REJECTED   LOCATION     │
│  ─────────────────────────────────────────────────────────────  │
│  Product One      100        [100  ]    [0   ]     [A-01-01 ▼]  │
│  Product Two      50         [48   ]    [2   ]     [A-01-02 ▼]  │
│    └─ Reason: [Damaged on arrival ▼]                            │
│  Product Three    25         [    ]     [    ]     [         ]  │
├─────────────────────────────────────────────────────────────────┤
│  📎 Attach Documents  📸 Add Photos                             │
├─────────────────────────────────────────────────────────────────┤
│                              [Save Draft]  [Complete Receiving] │
└─────────────────────────────────────────────────────────────────┘
```

### Search & Filter Options

- By Receipt Number
- By PO Number
- By Vendor/Supplier Name
- By Date Range
- By Status (Pending, Partial, Complete)

### Validation Requirements

- Confirmation dialog before final commit
- Warn if received quantity differs significantly from expected
- Require reason code for rejections
- Option to generate Goods Receipt Note (GRN) PDF

---

## Feature 5: Purchase Orders

**Location:** INVENTORY GROUP
**Purpose:** Create, manage, and track orders to suppliers

### Workflow Stages

```
┌────────┐   ┌────────┐   ┌────────┐   ┌────────┐   ┌────────┐   ┌────────┐
│ DRAFT  │ → │PENDING │ → │APPROVED│ → │  SENT  │ → │RECEIVED│ → │ CLOSED │
│        │   │APPROVAL│   │        │   │        │   │        │   │        │
└────────┘   └────────┘   └────────┘   └────────┘   └────────┘   └────────┘
     ↓            ↓
 [CANCELLED]  [REJECTED]
```

### Approval Matrix Design

| Threshold | Approver Level |
|-----------|----------------|
| < KES 10,000 | Auto-approve or Team Lead |
| KES 10,000 - 50,000 | Manager |
| KES 50,000 - 200,000 | Director |
| > KES 200,000 | CFO/Owner |

*Note: Thresholds are configurable in Organization Settings*

### PO List View

```
┌─────────────────────────────────────────────────────────────────┐
│  PURCHASE ORDERS                              [+ New PO] [⋮]    │
├─────────────────────────────────────────────────────────────────┤
│  [🔍 Search...]  [Status ▼] [Supplier ▼] [Date Range 📅]       │
├─────────────────────────────────────────────────────────────────┤
│  PO #         SUPPLIER        DATE       TOTAL      STATUS      │
│  ─────────────────────────────────────────────────────────────  │
│  PO-2024-048  ABC Supplies    Jan 15     KES 45,200 🟡 PENDING  │
│  PO-2024-047  XYZ Traders     Jan 14     KES 12,800 🟢 APPROVED │
│  PO-2024-046  ABC Supplies    Jan 12     KES 78,500 🔵 RECEIVED │
└─────────────────────────────────────────────────────────────────┘
```

### Create/Edit PO Form

- **Auto-populate fields:** Department, cost center from user profile
- **Supplier catalog integration:** Quick item selection from supplier's catalog
- **Budget check:** Real-time budget validation, warn/block on exceed
- **Template support:** Save common orders as templates
- **Document attachment:** Quotes, contracts, specifications

### Key Features

- **Mobile approval:** One-tap approve/reject with push notifications
- **Audit trail:** Timestamp all actions with user attribution
- **Comments thread:** Communication history on each PO
- **Duplicate detection:** Warn on similar recent orders
- **Auto-routing:** Rules-based routing to appropriate approver

---

## Feature 6: Suppliers

**Location:** INVENTORY GROUP
**Purpose:** Maintain supplier/vendor master data and relationships

### Profile Structure

```
┌─────────────────────────────────────────────────────────────────┐
│  SUPPLIER PROFILE                           [Edit] [Deactivate] │
├─────────────────────────────────────────────────────────────────┤
│  ┌──────┐  ABC Supplies Ltd                    Status: ● Active │
│  │ LOGO │  Est. 2010 | Nairobi, Kenya                          │
│  └──────┘  ★★★★☆ 4.2 rating                                     │
├─────────────────────────────────────────────────────────────────┤
│  [Overview] [Contacts] [Documents] [Orders] [Performance]       │
├─────────────────────────────────────────────────────────────────┤
│  CONTACT INFORMATION              │  PAYMENT TERMS              │
│  📧 orders@abcsupplies.co.ke      │  Net 30 days                │
│  📞 +254 700 123 456              │  Bank: KCB 123456789        │
│  📍 Industrial Area, Nairobi      │  M-Pesa: 123456             │
├───────────────────────────────────┴─────────────────────────────┤
│  QUICK STATS                                                    │
│  Total Orders: 47    │  Avg Lead Time: 3 days  │  YTD: KES 1.2M │
└─────────────────────────────────────────────────────────────────┘
```

### Directory Features

| Feature | Description |
|---------|-------------|
| **Search** | By name, category, product type, location |
| **Filters** | Active/Inactive, Category, Region, Payment Terms |
| **Tags** | Custom tags for organization (Preferred, Local, etc.) |
| **Quick Actions** | Create PO, View History, Contact, Edit |

### Data Fields

**Basic Information:**
- Company name, trading name
- Tax ID / PIN
- Physical address, postal address
- Website, email

**Contacts (Multiple):**
- Name, role/title
- Phone, email
- Primary contact flag

**Business Information:**
- Categories supplied
- Lead time (days)
- Minimum order value
- Payment terms
- Currency

**Documents:**
- Contracts (with expiry alerts)
- Certificates/licenses
- Insurance documents
- Bank details verification

### Performance Metrics

- On-time delivery rate (%)
- Quality score (defect rate)
- Price competitiveness
- Response time

---

## Feature 7: Open/Close Day

**Location:** WORKDAY GROUP
**Purpose:** Manage work periods for cash reconciliation and reporting

### Open Day Flow

```
┌─────────────────────────────────────────────────────────────────┐
│  OPEN WORK DAY                                                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Today's Date: Monday, January 15, 2024                        │
│  Register: Main Counter                                         │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  STARTING CASH IN DRAWER                                │   │
│  │                                                         │   │
│  │  Enter amount:  KES [    5,000    ]                    │   │
│  │                                                         │   │
│  │  💡 Previous closing float: KES 5,000                   │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  □ I confirm the cash drawer has been counted                  │
│                                                                 │
│                                    [Cancel]  [🟢 Open Day]      │
└─────────────────────────────────────────────────────────────────┘
```

### Close Day Flow

```
Step 1: Settle Open Items
┌─────────────────────────────────────────────────────────────────┐
│  ⚠️ WARNING: 3 open tabs found                                  │
│                                                                 │
│  Tab #12 - John Smith - KES 1,200                              │
│  Tab #15 - Table 5 - KES 3,450                                 │
│  Tab #18 - Mary Jane - KES 890                                 │
│                                                                 │
│  [Close All Tabs]  [View Details]  [Continue Anyway]           │
└─────────────────────────────────────────────────────────────────┘

Step 2: Settle Credit Cards
┌─────────────────────────────────────────────────────────────────┐
│  SETTLE CARD TERMINAL                                          │
│                                                                 │
│  Card Terminal Batch: 47 transactions                          │
│  Total: KES 125,400                                            │
│                                                                 │
│  [Settle Cards Now]                                            │
└─────────────────────────────────────────────────────────────────┘

Step 3: Cash Count
┌─────────────────────────────────────────────────────────────────┐
│  CASH RECONCILIATION                                           │
├─────────────────────────────────────────────────────────────────┤
│  DENOMINATION CALCULATOR                                        │
│  ─────────────────────────────────────────────────────────────  │
│  KES 1000 × [  12  ] = KES 12,000                              │
│  KES 500  × [   8  ] = KES 4,000                               │
│  KES 200  × [  15  ] = KES 3,000                               │
│  KES 100  × [  23  ] = KES 2,300                               │
│  KES 50   × [  10  ] = KES 500                                 │
│  KES 20   × [   5  ] = KES 100                                 │
│  Coins    × [      ] = KES 85                                  │
│  ─────────────────────────────────────────────────────────────  │
│  TOTAL COUNTED:              KES 21,985                        │
├─────────────────────────────────────────────────────────────────┤
│  RECONCILIATION                                                 │
│  ─────────────────────────────────────────────────────────────  │
│  Opening Float:              KES 5,000                         │
│  + Cash Sales:               KES 17,200                        │
│  - Cash Payouts:             KES 500                           │
│  = EXPECTED CASH:            KES 21,700                        │
│                                                                 │
│  COUNTED:                    KES 21,985                        │
│  DIFFERENCE:                 KES +285  🟢 OVER                 │
├─────────────────────────────────────────────────────────────────┤
│  Leave in drawer for tomorrow: KES [  5,000  ]                 │
│  Deposit to safe:              KES 16,985                      │
│                                                                 │
│  Note: [                                        ]              │
│                                                                 │
│                                    [Back]  [🔴 Close Day]       │
└─────────────────────────────────────────────────────────────────┘
```

### Security Features

- **Role-based access:** Only managers can see detailed figures
- **Restricted view for cashiers:** Shows only "Deposit to Safe" field
- **System calculates over/short:** Prevents manipulation
- **Audit log:** All close-out attempts recorded

### Multi-Shift Support

- Close current shift without closing the day
- Each shift has individual reconciliation
- End-of-day combines all shifts

---

## Feature 8: X-Report

**Location:** WORKDAY GROUP
**Purpose:** Generate sales summaries and batch reports

### Report Types

| Report | Purpose | Closes Batch? |
|--------|---------|---------------|
| **X-Report** | Mid-shift snapshot, check sales anytime | No |
| **Z-Report** | End-of-day official report | Yes |
| **ZZ-Report** | Cumulative report since last ZZ | Yes |

### X-Report Content

```
┌─────────────────────────────────────────────────────────────────┐
│                         X-REPORT                                │
│                    ProNet POS - Main Counter                    │
│                                                                 │
│  Report Date: January 15, 2024                                 │
│  Report Time: 14:32:15                                         │
│  Batch #: 2024-0115-001                                        │
│  Cashier: John Doe                                             │
│  ═══════════════════════════════════════════════════════════   │
│                                                                 │
│  BATCH SUMMARY                                                  │
│  ─────────────────────────────────────────────────────────────  │
│  Batch Opened:        08:00:00                                 │
│  Transactions:        47                                        │
│                                                                 │
│  MONEY IN                                                       │
│  ─────────────────────────────────────────────────────────────  │
│  Gross Sales:                              KES 125,400          │
│  Returns:                                  KES -2,300           │
│  Deposits Received:                        KES 5,000            │
│  ─────────────────────────────────────────────────────────────  │
│  TOTAL MONEY IN:                           KES 128,100          │
│                                                                 │
│  MONEY OUT                                                      │
│  ─────────────────────────────────────────────────────────────  │
│  Pay-outs:                                 KES 500              │
│  Deposits Redeemed:                        KES 3,200            │
│  ─────────────────────────────────────────────────────────────  │
│  TOTAL MONEY OUT:                          KES 3,700            │
│                                                                 │
│  NET TAKINGS:                              KES 124,400          │
│                                                                 │
│  TAXES COLLECTED                                                │
│  ─────────────────────────────────────────────────────────────  │
│  VAT 16%:                                  KES 17,296           │
│  ─────────────────────────────────────────────────────────────  │
│  TOTAL TAX:                                KES 17,296           │
│                                                                 │
│  TENDER SUMMARY                                                 │
│  ─────────────────────────────────────────────────────────────  │
│  Tender        Opening    Shift +/-    Expected    Counted     │
│  ─────────────────────────────────────────────────────────────  │
│  Cash          5,000      +17,200      22,200      [     ]     │
│  Card          0          +85,400      85,400      [     ]     │
│  M-Pesa        0          +22,800      22,800      [     ]     │
│                                                                 │
│  ═══════════════════════════════════════════════════════════   │
│                    *** END OF X-REPORT ***                     │
└─────────────────────────────────────────────────────────────────┘
```

### Best Practices

1. **Always run X-Report before Z-Report** to catch discrepancies
2. **Enter opening/closing amounts** before printing Z-Report
3. **Z-Report closes the batch** - cannot be undone
4. **Keep Z-Reports** for accounting/audit purposes (7+ years)

### Interface Features

- **Print option:** Thermal printer or PDF
- **Email option:** Send to configured addresses
- **Historical access:** View/reprint past reports
- **Filter by employee:** Managers can view per-employee reports
- **Comparison view:** vs previous day/week/period
- **Auto Z-Report:** Schedule automatic end-of-day (e.g., 11:59 PM)

---

## Feature 9: Sales Reports

**Location:** REPORTS GROUP
**Purpose:** Analyze sales data with visualizations and drill-downs

### Dashboard Layout

```
┌─────────────────────────────────────────────────────────────────┐
│  SALES REPORTS            [Today ▼] [vs Last Week ▼] [📥 Export]│
├─────────────────────────────────────────────────────────────────┤
│  ┌───────────┐ ┌───────────┐ ┌───────────┐ ┌───────────┐       │
│  │ REVENUE   │ │  ORDERS   │ │  AOV      │ │  GROWTH   │       │
│  │ KES 125K  │ │    47     │ │ KES 2,668 │ │   +12%    │       │
│  │ ▲ +8.5%   │ │ ▲ +5      │ │ ▲ +3.2%   │ │   🟢      │       │
│  └───────────┘ └───────────┘ └───────────┘ └───────────┘       │
├─────────────────────────────────────────────────────────────────┤
│  SALES TREND                                                    │
│  ▲                                                              │
│  │    ╭─╮                         ╭───╮                         │
│  │ ╭──╯ ╰──╮     ╭───╮     ╭─────╯   ╰─╮                       │
│  │─╯       ╰─────╯   ╰─────╯           ╰────                   │
│  └──────────────────────────────────────────────────────▶       │
│    Mon    Tue    Wed    Thu    Fri    Sat    Sun               │
├────────────────────────────┬────────────────────────────────────┤
│  TOP PRODUCTS              │  SALES BY CATEGORY                 │
│  1. Tusker 500ml   KES 23K │  ████████████ Beverages 45%       │
│  2. Chicken Wings  KES 18K │  ████████ Food 35%                │
│  3. Pizza Large    KES 15K │  ████ Other 20%                   │
│  4. Soda 500ml     KES 12K │                                    │
│  5. French Fries   KES 10K │                                    │
├────────────────────────────┴────────────────────────────────────┤
│  SALES BY HOUR                                                  │
│  Peak: 12:00-14:00 (Lunch) and 18:00-21:00 (Dinner)            │
└─────────────────────────────────────────────────────────────────┘
```

### Chart Selection Guide

| Data Type | Recommended Chart |
|-----------|-------------------|
| Trends over time | Line chart |
| Category comparison | Bar chart (horizontal for many categories) |
| Composition/share | Pie/Donut (max 5 segments) or Treemap |
| Conversion funnel | Funnel chart |
| Rankings | Leaderboard/Table |
| Distribution | Histogram |

### Filter Options

- **Date range:** Presets (Today, Yesterday, This Week, This Month, Custom)
- **Compare to:** Prior period, Same period last year
- **Category:** Filter by product category
- **Product:** Filter by specific product
- **Employee:** Filter by cashier/server
- **Payment method:** Cash, Card, M-Pesa
- **Time of day:** Morning, Lunch, Afternoon, Evening, Night

### Export Options

- **PDF:** Formatted report with charts
- **Excel:** Raw data with pivot-ready structure
- **CSV:** Simple data export
- **Scheduled reports:** Email daily/weekly/monthly summaries

### Interaction Patterns

- **Coordinated views:** Clicking chart segment filters other views
- **Drill-down:** Click category → see products → see transactions
- **Hover details:** Show exact values on hover (desktop)
- **Tap to expand:** Mobile-friendly detail view

---

## Feature 10: Inventory Reports

**Location:** REPORTS GROUP
**Purpose:** Track stock movements, valuations, and inventory health

### Report Types

| Report | Purpose |
|--------|---------|
| **Stock Valuation** | Current value of inventory by item/category |
| **Movement Report** | All ins/outs over a period |
| **Aging Report** | Stock age by purchase date |
| **Turnover Report** | How fast stock sells |
| **Variance Report** | Counted vs system quantities |
| **ABC Analysis** | Items ranked by value/volume contribution |

### Valuation Methods

| Method | Description | Use Case |
|--------|-------------|----------|
| **FIFO** | First In, First Out | Perishables, time-sensitive goods |
| **AVCO** | Average Cost | General merchandise |
| **Standard** | Fixed standard cost | Manufacturing, stable pricing |

### Stock Valuation View

```
┌─────────────────────────────────────────────────────────────────┐
│  INVENTORY VALUATION                     As of: Jan 15, 2024   │
├─────────────────────────────────────────────────────────────────┤
│  TOTAL INVENTORY VALUE: KES 2,456,780                          │
├─────────────────────────────────────────────────────────────────┤
│  CATEGORY            QTY      VALUE        % OF TOTAL          │
│  ─────────────────────────────────────────────────────────────  │
│  Beverages          1,245    KES 892,340   ████████░░ 36%      │
│  Food & Ingredients   892    KES 645,200   ██████░░░░ 26%      │
│  Spirits              234    KES 567,890   █████░░░░░ 23%      │
│  Supplies             456    KES 234,500   ███░░░░░░░ 10%      │
│  Other                123    KES 116,850   █░░░░░░░░░  5%      │
├─────────────────────────────────────────────────────────────────┤
│  [View Details]  [Export]  [Print]                             │
└─────────────────────────────────────────────────────────────────┘
```

### Movement Analysis View

```
┌─────────────────────────────────────────────────────────────────┐
│  STOCK MOVEMENTS                   [This Month ▼] [All Items ▼]│
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ▲ Movement Trend (Last 30 Days)                               │
│  │                                                              │
│  │  Purchases ━━━    Sales ━━━    Adjustments ━━━              │
│  │                                                              │
│  └──────────────────────────────────────────────────────▶      │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│  DATE       TYPE        ITEM          QTY     REFERENCE        │
│  ─────────────────────────────────────────────────────────────  │
│  Jan 15    📥 Purchase  Tusker 500ml  +100    PO-2024-048      │
│  Jan 15    📤 Sale      Tusker 500ml  -12     INV-2024-1234    │
│  Jan 14    ⚠️ Adjust    Chips 1kg     -5      ADJ-2024-023     │
│  Jan 14    📤 Sale      Pizza Base    -8      INV-2024-1233    │
└─────────────────────────────────────────────────────────────────┘
```

### Key Metrics

- **Inventory Turnover Ratio:** Cost of Goods Sold / Average Inventory
- **Days of Supply:** Current Stock / Average Daily Usage
- **Shrinkage Rate:** (Expected - Actual) / Expected × 100
- **Dead Stock:** Items with no movement in X days

### Drill-Through Capability

- Click category → See items in category
- Click item → See all movements for that item
- Click movement → See source document (PO, Invoice, etc.)

---

## Feature 11: Products

**Location:** MANAGEMENT GROUP
**Purpose:** Create and manage product catalog

### List View

```
┌─────────────────────────────────────────────────────────────────┐
│  PRODUCTS                                  [+ Add Product] [⋮]  │
├─────────────────────────────────────────────────────────────────┤
│  [🔍 Search by name or SKU...]  [Category ▼] [Status ▼] [📥]   │
├─────────────────────────────────────────────────────────────────┤
│  □  IMAGE  SKU       NAME           CATEGORY   PRICE    STATUS │
│  ─────────────────────────────────────────────────────────────  │
│  □  [IMG]  BEV001    Tusker 500ml   Beverages  KES 250  ● Active│
│  □  [IMG]  FOD001    Chicken Wings  Food       KES 450  ● Active│
│  □  [IMG]  FOD002    Pizza Large    Food       KES 1200 ○ Inactive│
├─────────────────────────────────────────────────────────────────┤
│  □ Select All      [Bulk Edit ▼]                               │
│  Showing 1-25 of 156 products                  [< 1 2 3 ... 7 >]│
└─────────────────────────────────────────────────────────────────┘
```

### Product Form (Create/Edit)

```
┌─────────────────────────────────────────────────────────────────┐
│  ADD PRODUCT                                    [Cancel] [Save] │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  BASIC INFORMATION                                              │
│  ─────────────────────────────────────────────────────────────  │
│  Product Name *                                                 │
│  [Tusker Lager 500ml                                    ]      │
│                                                                 │
│  SKU *                     Barcode                              │
│  [BEV001        ]         [5012345678901    ] [📷 Scan]        │
│                                                                 │
│  Category *                                                     │
│  [Beverages > Beer                                      ▼]     │
│                                                                 │
│  PRICING                                                        │
│  ─────────────────────────────────────────────────────────────  │
│  Selling Price *           Cost Price                           │
│  KES [    250    ]        KES [    180    ]                    │
│                                                                 │
│  □ Price includes tax      Tax Rate: [VAT 16%           ▼]     │
│                                                                 │
│  INVENTORY                                                      │
│  ─────────────────────────────────────────────────────────────  │
│  □ Track inventory                                              │
│                                                                 │
│  Current Stock             Reorder Point                        │
│  [    45    ]             [    20    ]                         │
│                                                                 │
│  IMAGES                                                         │
│  ─────────────────────────────────────────────────────────────  │
│  ┌─────┐ ┌─────┐ ┌─────┐                                       │
│  │ IMG │ │ IMG │ │  +  │  Drag images or click to upload      │
│  │  ★  │ │     │ │ Add │  Primary image marked with ★          │
│  └─────┘ └─────┘ └─────┘                                       │
│                                                                 │
│  ADDITIONAL DETAILS                              [+ Expand]     │
│  ─────────────────────────────────────────────────────────────  │
│  Description, Variants, Supplier, Custom Fields...             │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### SKU Best Practices

| Rule | Example | Reason |
|------|---------|--------|
| 5-15 characters | `BEV001` | Short enough to read, long enough to be unique |
| ALL CAPS | `BEV001` not `bev001` | Consistency and readability |
| No spaces | `BEV-001` not `BEV 001` | Avoid system issues |
| Avoid 0, O, 1, I, L | Use `BEV-002` not `BEV-O01` | Prevent confusion |
| Logical structure | `[Category]-[Number]` | Easy to understand |

### Form Best Practices

- **Single-column layout:** Vertical flow, easier to scan
- **Labels above fields:** Not inline or placeholder-only
- **Required fields marked:** Asterisk (*) indicator
- **Real-time validation:** Check SKU uniqueness on blur
- **Smart defaults:** Auto-generate SKU, inherit tax from category
- **Grouped sections:** Basic Info, Pricing, Inventory, Images

### Bulk Operations

- Import from CSV/Excel
- Export to CSV/Excel
- Mass price update (by % or fixed amount)
- Mass category change
- Mass status change (activate/deactivate)
- Print barcode labels

---

## Feature 12: Categories

**Location:** MANAGEMENT GROUP
**Purpose:** Organize products into hierarchical categories

### Tree View Interface

```
┌─────────────────────────────────────────────────────────────────┐
│  CATEGORIES                                   [+ Add Category]  │
├─────────────────────────────────────────────────────────────────┤
│  [🔍 Search categories...]                    [Expand All]      │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  📁 All Products (156)                                         │
│   │                                                             │
│   ├─ 📁 Beverages (45)                              [⋮]        │
│   │   ├─ 📁 Beer (18)                               [⋮]        │
│   │   ├─ 📁 Soft Drinks (15)                        [⋮]        │
│   │   ├─ 📁 Wines (8)                               [⋮]        │
│   │   └─ 📁 Spirits (4)                             [⋮]        │
│   │                                                             │
│   ├─ 📁 Food (78)                                   [⋮]        │
│   │   ├─ 📁 Appetizers (12)                         [⋮]        │
│   │   ├─ 📁 Main Course (35)                        [⋮]        │
│   │   │   ├─ 📁 Grills (15)                         [⋮]        │
│   │   │   ├─ 📁 Pasta (10)                          [⋮]        │
│   │   │   └─ 📁 Local Dishes (10)                   [⋮]        │
│   │   ├─ 📁 Desserts (8)                            [⋮]        │
│   │   └─ 📁 Sides (23)                              [⋮]        │
│   │                                                             │
│   └─ 📁 Merchandise (33)                            [⋮]        │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Category Form

```
┌─────────────────────────────────────────────────────────────────┐
│  EDIT CATEGORY                                 [Cancel] [Save]  │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Category Name *                                                │
│  [Beer                                                  ]      │
│                                                                 │
│  Parent Category                                                │
│  [Beverages                                             ▼]     │
│                                                                 │
│  Icon/Image                                                     │
│  ┌─────┐                                                       │
│  │ 🍺  │  [Choose Icon] or [Upload Image]                      │
│  └─────┘                                                       │
│                                                                 │
│  Description (optional)                                         │
│  [All beer products including local and imported brands ]      │
│                                                                 │
│  Display Order                                                  │
│  [  1  ]  (Lower numbers appear first)                         │
│                                                                 │
│  CATEGORY ATTRIBUTES                                            │
│  ─────────────────────────────────────────────────────────────  │
│  Default Tax Rate: [VAT 16%                             ▼]     │
│                                                                 │
│  □ Show in POS grid                                            │
│  □ Available for online ordering                               │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Best Practices

| Guideline | Recommendation |
|-----------|----------------|
| **Hierarchy depth** | Maximum 3-5 levels |
| **Items per level** | No more than 15 categories at any level |
| **Naming** | Simple, customer-friendly language |
| **Single placement** | Each product in ONE primary category |
| **Over-categorization** | Use attributes (size, color) instead of more categories |

### Interactions

- **Drag-and-drop:** Reorder and reparent categories
- **Expand/collapse:** Click arrow to toggle children
- **Quick actions menu:** Add subcategory, Add product, Edit, Delete
- **Breadcrumb:** Show full path when editing (Beverages > Beer)
- **Delete handling:** Prompt to reassign or delete child products

---

## Feature 13: Employees

**Location:** MANAGEMENT GROUP
**Purpose:** Manage staff records, schedules, and time tracking

### Employee List

```
┌─────────────────────────────────────────────────────────────────┐
│  EMPLOYEES                                    [+ Add Employee]  │
├─────────────────────────────────────────────────────────────────┤
│  [🔍 Search...]  [Department ▼] [Status ▼] [Role ▼]            │
├─────────────────────────────────────────────────────────────────┤
│  PHOTO   NAME            ROLE        DEPARTMENT   STATUS       │
│  ─────────────────────────────────────────────────────────────  │
│  [IMG]   John Doe        Cashier     Front of House  ● Active  │
│  [IMG]   Jane Smith      Server      Front of House  ● Active  │
│  [IMG]   Mike Johnson    Chef        Kitchen         ● Active  │
│  [IMG]   Sarah Wilson    Manager     Management      📅 On Leave│
└─────────────────────────────────────────────────────────────────┘
```

### Employee Profile

```
┌─────────────────────────────────────────────────────────────────┐
│  EMPLOYEE PROFILE                           [Edit] [Deactivate] │
├─────────────────────────────────────────────────────────────────┤
│  ┌──────┐  John Doe                           Status: ● Active  │
│  │PHOTO │  Cashier - Front of House                             │
│  └──────┘  Employee ID: EMP-001                                 │
├─────────────────────────────────────────────────────────────────┤
│  [Profile] [Schedule] [Time & Attendance] [Performance]         │
├─────────────────────────────────────────────────────────────────┤
│  CONTACT INFORMATION          │  EMPLOYMENT DETAILS             │
│  📧 john.doe@email.com        │  Start Date: Jan 15, 2023       │
│  📞 +254 700 123 456          │  Department: Front of House     │
│  📍 Nairobi, Kenya            │  Reports To: Jane Manager       │
│  Emergency: Mary Doe          │  System Role: Cashier           │
│             +254 711 234 567  │                                 │
├───────────────────────────────┴─────────────────────────────────┤
│  THIS WEEK'S SCHEDULE                                           │
│  Mon    Tue    Wed    Thu    Fri    Sat    Sun                 │
│  8-4    8-4    OFF    8-4    8-4    10-6   OFF                 │
├─────────────────────────────────────────────────────────────────┤
│  LEAVE BALANCE                                                  │
│  Annual Leave: 15 days remaining (21 - 6 used)                 │
│  Sick Leave: 10 days remaining                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Schedule View (Manager)

```
┌─────────────────────────────────────────────────────────────────┐
│  SCHEDULE                    [< Week of Jan 15-21, 2024 >]     │
├─────────────────────────────────────────────────────────────────┤
│           Mon 15  Tue 16  Wed 17  Thu 18  Fri 19  Sat 20  Sun  │
│  ─────────────────────────────────────────────────────────────  │
│  John D   [8-4]   [8-4]   [OFF]   [8-4]   [8-4]   [10-6]  OFF  │
│  Jane S   [10-6]  [10-6]  [8-4]   [OFF]   [10-6]  [10-6]  OFF  │
│  Mike J   [6-2]   [6-2]   [6-2]   [6-2]   [6-2]   [OFF]   OFF  │
│  Sarah W  [8-4]   [📅]    [📅]    [📅]    [📅]    [OFF]   OFF  │
│           └── Drag to assign shifts    └── 📅 = Leave          │
├─────────────────────────────────────────────────────────────────┤
│  ⚠️ Conflicts: None                                             │
│  Coverage: Mon-Sat OK | Sun: Need 1 more cashier               │
└─────────────────────────────────────────────────────────────────┘
```

### Key Features

| Feature | Description |
|---------|-------------|
| **Shift scheduling** | Drag-and-drop calendar, conflict detection |
| **Time tracking** | Clock in/out, geofenced verification |
| **Self-service** | Employees view schedule, request leave, swap shifts |
| **Leave management** | Request → Approve workflow, balance tracking |
| **Performance** | Sales per employee, hours worked, attendance |
| **Documents** | ID copies, contracts, certifications with expiry alerts |
| **Mobile access** | Employees check schedule on personal devices |

---

## Feature 14: Users

**Location:** MANAGEMENT GROUP
**Purpose:** Manage system user accounts and access

### User List

```
┌─────────────────────────────────────────────────────────────────┐
│  USERS                                           [+ Add User]   │
├─────────────────────────────────────────────────────────────────┤
│  [🔍 Search by name or email...]     [Role ▼] [Status ▼]       │
├─────────────────────────────────────────────────────────────────┤
│  USER               EMAIL                  ROLE      STATUS     │
│  ─────────────────────────────────────────────────────────────  │
│  [JD] John Doe      john@company.com       Admin     ● Active   │
│  [JS] Jane Smith    jane@company.com       Manager   ● Active   │
│  [MJ] Mike Johnson  mike@company.com       Cashier   ● Active   │
│  [SW] Sarah Wilson  sarah@company.com      Cashier   ○ Disabled │
│  [--] Pending User  newuser@company.com    Cashier   ⏳ Invited │
├─────────────────────────────────────────────────────────────────┤
│  □ Select All      [Bulk Actions ▼]                            │
│  Showing 1-10 of 12 users                                      │
└─────────────────────────────────────────────────────────────────┘
```

### Add/Edit User Form

```
┌─────────────────────────────────────────────────────────────────┐
│  ADD USER                                      [Cancel] [Save]  │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ACCOUNT INFORMATION                                            │
│  ─────────────────────────────────────────────────────────────  │
│  Email Address *                                                │
│  [newuser@company.com                                   ]      │
│                                                                 │
│  Full Name *                                                    │
│  [                                                      ]      │
│                                                                 │
│  Role *                                                         │
│  [Cashier                                               ▼]     │
│                                                                 │
│  ASSIGNMENT                                                     │
│  ─────────────────────────────────────────────────────────────  │
│  Location                                                       │
│  [Main Branch                                           ▼]     │
│                                                                 │
│  Register (optional)                                            │
│  [Register 1                                            ▼]     │
│                                                                 │
│  Link to Employee (optional)                                    │
│  [John Doe - EMP-001                                    ▼]     │
│                                                                 │
│  INITIAL SETUP                                                  │
│  ─────────────────────────────────────────────────────────────  │
│  ○ Send invitation email (user sets own password)              │
│  ○ Set temporary password:  [••••••••    ]                     │
│                             □ Require password change on login │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### User Detail View

```
┌─────────────────────────────────────────────────────────────────┐
│  USER DETAILS                       [Edit] [Reset Password] [⋮] │
├─────────────────────────────────────────────────────────────────┤
│  [JD]  John Doe                              Status: ● Active   │
│        john@company.com                                         │
│        Role: Admin                                              │
├─────────────────────────────────────────────────────────────────┤
│  ACCOUNT INFO                     │  ACTIVITY                   │
│  Created: Jan 1, 2024             │  Last Login: Today, 08:15   │
│  Created By: System Admin         │  Login Count: 234           │
│  Location: Main Branch            │  Failed Attempts: 0         │
│  Linked Employee: EMP-001         │                             │
├───────────────────────────────────┴─────────────────────────────┤
│  SECURITY                                                       │
│  Two-Factor Auth: ● Enabled                                    │
│  Password Last Changed: Dec 15, 2023 (31 days ago)             │
│                                                                 │
│  ACTIVE SESSIONS                                                │
│  Chrome on Windows - 192.168.1.100 - Active now   [End Session]│
│  Mobile App - Last active 2 hours ago             [End Session]│
├─────────────────────────────────────────────────────────────────┤
│  RECENT ACTIVITY                               [View Full Log]  │
│  Today 08:15 - Login from Chrome                               │
│  Yesterday 17:30 - Logout                                       │
│  Yesterday 08:00 - Login from Chrome                           │
└─────────────────────────────────────────────────────────────────┘
```

### Key Features

| Feature | Description |
|---------|-------------|
| **CRUD operations** | Add, Edit, Deactivate (soft-delete), Reset Password |
| **Invite flow** | Email with secure password setup link |
| **Status badges** | Active, Disabled, Pending Invite |
| **Role assignment** | Link to RBAC roles |
| **Session management** | View/terminate active sessions |
| **Audit trail** | Last login, all login attempts, actions |
| **Security** | 2FA option, password policies, force logout |
| **Bulk actions** | Mass role change, deactivate, export |

---

## Feature 15: Roles & Permissions

**Location:** MANAGEMENT GROUP
**Purpose:** Define access control through role-based permissions

### Role List

```
┌─────────────────────────────────────────────────────────────────┐
│  ROLES & PERMISSIONS                              [+ Add Role]  │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ROLE           DESCRIPTION               USERS    ACTIONS     │
│  ─────────────────────────────────────────────────────────────  │
│  🔒 Admin       Full system access         2       [Edit] [⋮]  │
│  👔 Manager     Operations management      3       [Edit] [⋮]  │
│  💰 Cashier     POS and basic functions    8       [Edit] [⋮]  │
│  👁️ Viewer      Read-only access           2       [Edit] [⋮]  │
│                                                                 │
│  🔒 = System role (cannot be deleted)                          │
└─────────────────────────────────────────────────────────────────┘
```

### Permission Matrix View

```
┌─────────────────────────────────────────────────────────────────┐
│  ROLE: Manager                                 [Save Changes]   │
├─────────────────────────────────────────────────────────────────┤
│  Role Name: [Manager                    ]                       │
│  Description: [Operations and staff management          ]       │
│  Inherits from: [Cashier                               ▼]      │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  PERMISSION MATRIX                                              │
│  ─────────────────────────────────────────────────────────────  │
│                           VIEW   CREATE  EDIT   DELETE  ADMIN  │
│  ─────────────────────────────────────────────────────────────  │
│  📦 PRODUCTS                                                    │
│     Products              [✓]    [✓]     [✓]    [✓]     [ ]    │
│     Categories            [✓]    [✓]     [✓]    [ ]     [ ]    │
│                                                                 │
│  📊 INVENTORY                                                   │
│     Stock Levels          [✓]    [✓]     [✓]    [ ]     [ ]    │
│     Receive Stock         [✓]    [✓]     [✓]    [ ]     [ ]    │
│     Purchase Orders       [✓]    [✓]     [✓]    [ ]     [ ]    │
│     Suppliers             [✓]    [✓]     [✓]    [ ]     [ ]    │
│                                                                 │
│  💰 SALES                                                       │
│     Point of Sale         [✓]    [✓]     [✓]    [ ]     [ ]    │
│     Discounts             [✓]    [ ]     [ ]    [ ]     [ ]    │
│     Voids/Refunds         [✓]    [✓]     [ ]    [ ]     [ ]    │
│                                                                 │
│  📈 REPORTS                                                     │
│     Sales Reports         [✓]    [ ]     [ ]    [ ]     [ ]    │
│     Inventory Reports     [✓]    [ ]     [ ]    [ ]     [ ]    │
│     X/Z Reports           [✓]    [✓]     [ ]    [ ]     [ ]    │
│                                                                 │
│  👥 MANAGEMENT                                                  │
│     Employees             [✓]    [✓]     [✓]    [ ]     [ ]    │
│     Users                 [✓]    [ ]     [ ]    [ ]     [ ]    │
│     Roles                 [ ]    [ ]     [ ]    [ ]     [ ]    │
│                                                                 │
│  ⚙️ SETTINGS                                                    │
│     Payment Methods       [✓]    [ ]     [ ]    [ ]     [ ]    │
│     Organization          [ ]    [ ]     [ ]    [ ]     [ ]    │
│                                                                 │
│  Legend: [✓] = Granted  [ ] = Denied                           │
└─────────────────────────────────────────────────────────────────┘
```

### Default Roles Template

| Role | Description | Key Permissions |
|------|-------------|-----------------|
| **Admin** | Full access | All permissions |
| **Manager** | Operations lead | All except settings admin |
| **Cashier** | Front-line staff | POS, view products, X-report |
| **Viewer** | Read-only | View all, no create/edit/delete |

### RBAC Best Practices

| Principle | Implementation |
|-----------|----------------|
| **Least Privilege** | Start with minimal permissions, add as needed |
| **Role Hierarchy** | Roles can inherit from other roles |
| **Avoid Privilege Sprawl** | Regular audits to remove unnecessary permissions |
| **Auto-Deprovisioning** | Remove access when employee leaves |
| **Documentation** | Record every role, its permissions, and purpose |

### Additional Features

- **Clone role:** Duplicate existing role as template
- **Compare roles:** Side-by-side permission comparison
- **Audit log:** Track permission changes
- **User count:** Show how many users have each role
- **Test mode:** Preview interface as specific role

---

## Feature 16: Payment Methods

**Location:** SETTINGS GROUP
**Purpose:** Configure accepted payment methods and their settings

### Payment Methods List

```
┌─────────────────────────────────────────────────────────────────┐
│  PAYMENT METHODS                          [+ Add Method] [⋮]    │
├─────────────────────────────────────────────────────────────────┤
│  Drag to reorder display in POS                                 │
├─────────────────────────────────────────────────────────────────┤
│  ≡  METHOD           TYPE      OPENS DRAWER   STATUS   ACTIONS │
│  ─────────────────────────────────────────────────────────────  │
│  ≡  💵 Cash          Cash      ✓              ● ON     [⚙️]    │
│  ≡  💳 Card          Card      ○              ● ON     [⚙️]    │
│  ≡  📱 M-Pesa        Mobile    ○              ● ON     [⚙️]    │
│  ≡  🎁 Gift Card     Voucher   ○              ● ON     [⚙️]    │
│  ≡  📝 On Account    Credit    ○              ○ OFF    [⚙️]    │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Payment Method Configuration

```
┌─────────────────────────────────────────────────────────────────┐
│  CONFIGURE: Cash                               [Cancel] [Save]  │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  DISPLAY                                                        │
│  ─────────────────────────────────────────────────────────────  │
│  Display Name                    Receipt Name                   │
│  [Cash                    ]     [CASH                    ]     │
│                                                                 │
│  Icon                                                           │
│  [💵 Money ▼]                                                  │
│                                                                 │
│  BEHAVIOR                                                       │
│  ─────────────────────────────────────────────────────────────  │
│  [✓] Open cash drawer on selection                             │
│  [✓] Allow in split payments                                   │
│  [✓] Include in float calculations                             │
│  [ ] Require signature                                          │
│                                                                 │
│  CASH-SPECIFIC SETTINGS                                         │
│  ─────────────────────────────────────────────────────────────  │
│  Rounding Rule                                                  │
│  [Round to nearest KES 5                                 ▼]    │
│                                                                 │
│  [✓] Show change calculation                                   │
│  [✓] Show quick cash buttons (exact, +100, +500, +1000)        │
│                                                                 │
│  ACCOUNTING                                                     │
│  ─────────────────────────────────────────────────────────────  │
│  GL Account Code                                                │
│  [1001-CASH                                              ]     │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Method-Specific Settings

| Method | Key Settings |
|--------|--------------|
| **Cash** | Rounding rules, drawer trigger, change calculation |
| **Card** | Terminal integration, supported types, contactless limit |
| **M-Pesa** | STK push config, paybill/till number, timeout |
| **Gift Card** | Validation rules, partial redemption, balance check |
| **On Account** | Credit limit, customer assignment, approval workflow |

### Tipping Configuration

```
┌─────────────────────────────────────────────────────────────────┐
│  TIPPING SETTINGS                                               │
├─────────────────────────────────────────────────────────────────┤
│  [✓] Enable tipping                                            │
│                                                                 │
│  Preset Options                                                 │
│  [ 10% ]  [ 15% ]  [ 20% ]  [+ Add]                           │
│                                                                 │
│  [✓] Allow custom tip amount                                   │
│  [ ] Enable "No Tip" button                                    │
│                                                                 │
│  Apply tipping to:                                              │
│  [✓] Card payments                                             │
│  [✓] M-Pesa payments                                           │
│  [ ] Cash payments                                              │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## Feature 17: Organization Settings

**Location:** SETTINGS GROUP
**Purpose:** Configure business-wide settings and preferences

### Settings Layout

```
┌─────────────────────────────────────────────────────────────────┐
│  ORGANIZATION SETTINGS                                [Save]    │
├─────────────────────────────────────────────────────────────────┤
│  [General] [Tax] [Receipts] [Locations] [Security] [Advanced]  │
├─────────────────────────────────────────────────────────────────┤
```

### General Tab

```
│  BUSINESS INFORMATION                                           │
│  ─────────────────────────────────────────────────────────────  │
│  Business Name *                                                │
│  [ProNet Restaurant                                      ]     │
│                                                                 │
│  Legal/Trading Name                                             │
│  [ProNet Hospitality Ltd                                 ]     │
│                                                                 │
│  Logo                                                           │
│  ┌──────────┐                                                  │
│  │  [LOGO]  │  [Upload New] [Remove]                           │
│  │  200x200 │  PNG or JPG, max 1MB                             │
│  └──────────┘                                                  │
│                                                                 │
│  CONTACT INFORMATION                                            │
│  ─────────────────────────────────────────────────────────────  │
│  Phone                          Email                           │
│  [+254 700 123 456       ]     [info@pronet.co.ke        ]    │
│                                                                 │
│  Address                                                        │
│  [123 Business Street                                    ]     │
│  [Nairobi, Kenya                                         ]     │
│                                                                 │
│  REGIONAL SETTINGS                                              │
│  ─────────────────────────────────────────────────────────────  │
│  Currency                       Timezone                        │
│  [KES - Kenya Shilling   ▼]    [Africa/Nairobi           ▼]   │
│                                                                 │
│  Date Format                    Time Format                     │
│  [DD/MM/YYYY             ▼]    [24-hour                  ▼]   │
│                                                                 │
│  Language                                                       │
│  [English                                                ▼]    │
```

### Tax Tab

```
│  TAX CONFIGURATION                                              │
│  ─────────────────────────────────────────────────────────────  │
│  Tax Registration Number (PIN)                                  │
│  [P051234567X                                            ]     │
│                                                                 │
│  Default Pricing                                                │
│  ○ Prices exclude tax (tax added at checkout)                  │
│  ● Prices include tax (tax-inclusive pricing)                  │
│                                                                 │
│  TAX RATES                                     [+ Add Tax Rate] │
│  ─────────────────────────────────────────────────────────────  │
│  NAME              RATE     DEFAULT    ACTIONS                  │
│  VAT               16%      ✓          [Edit] [Delete]         │
│  Zero-rated        0%                  [Edit] [Delete]         │
│  Exempt            0%                  [Edit] [Delete]         │
```

### Receipts Tab

```
│  RECEIPT SETTINGS                                               │
│  ─────────────────────────────────────────────────────────────  │
│                                                                 │
│  HEADER                                                         │
│  [✓] Show logo                                                 │
│  [✓] Show business name                                        │
│  [✓] Show address                                              │
│  [✓] Show phone number                                         │
│                                                                 │
│  Custom Header Text                                             │
│  [Welcome to ProNet Restaurant!                          ]     │
│                                                                 │
│  FOOTER                                                         │
│  Custom Footer Text                                             │
│  [Thank you for dining with us!                          ]     │
│  [Follow us: @pronetrestaurant                           ]     │
│                                                                 │
│  ADDITIONAL INFO                                                │
│  [✓] Show tax breakdown                                        │
│  [✓] Show server name                                          │
│  [✓] Show order number                                         │
│  [ ] Show QR code (feedback/loyalty)                           │
│                                                                 │
│  RECEIPT PREVIEW                                                │
│  ┌─────────────────────────────┐                               │
│  │      [LOGO]                 │                               │
│  │   ProNet Restaurant         │                               │
│  │   123 Business Street       │                               │
│  │   Tel: +254 700 123 456    │                               │
│  │ ─────────────────────────── │                               │
│  │ Welcome to ProNet!          │                               │
│  │ ─────────────────────────── │                               │
│  │ Order #1234                 │                               │
│  │ Server: John D              │                               │
│  └─────────────────────────────┘                               │
```

### Security Tab

```
│  SECURITY SETTINGS                                              │
│  ─────────────────────────────────────────────────────────────  │
│                                                                 │
│  PASSWORD POLICY                                                │
│  Minimum Length: [ 8 ] characters                              │
│  [✓] Require uppercase letter                                  │
│  [✓] Require number                                            │
│  [ ] Require special character                                  │
│                                                                 │
│  Password Expiry: [90 days                               ▼]    │
│                                                                 │
│  SESSION SETTINGS                                               │
│  Auto-logout after inactivity: [15 minutes               ▼]   │
│  [✓] Allow multiple sessions per user                          │
│                                                                 │
│  TWO-FACTOR AUTHENTICATION                                      │
│  ○ Disabled                                                     │
│  ○ Optional (users can enable)                                  │
│  ● Required for Admin roles                                     │
│  ○ Required for all users                                       │
│                                                                 │
│  AUDIT LOG                                                      │
│  [✓] Log all login attempts                                    │
│  [✓] Log sensitive operations                                  │
│  Retention period: [365 days                             ▼]    │
│                                    [View Audit Log]            │
```

### Settings Grouping Best Practices

| Group | Contents |
|-------|----------|
| **General** | Business info, branding, regional settings |
| **Tax** | Tax rates, registration, pricing rules |
| **Receipts** | Header/footer customization, content options |
| **Locations** | Multi-location management, location-specific settings |
| **Security** | Password policy, sessions, 2FA, audit |
| **Advanced** | API keys, integrations, backup/export |

---

## Summary Checklist

| # | Feature | Group | Status |
|---|---------|-------|--------|
| 1 | Dashboard | MAIN | ✅ Documented |
| 2 | Point of Sale | MAIN | ✅ Documented |
| 3 | Stock Levels | INVENTORY | ✅ Documented |
| 4 | Receive Stock | INVENTORY | ✅ Documented |
| 5 | Purchase Orders | INVENTORY | ✅ Documented |
| 6 | Suppliers | INVENTORY | ✅ Documented |
| 7 | Open/Close Day | WORKDAY | ✅ Documented |
| 8 | X-Report | WORKDAY | ✅ Documented |
| 9 | Sales Reports | REPORTS | ✅ Documented |
| 10 | Inventory Reports | REPORTS | ✅ Documented |
| 11 | Products | MANAGEMENT | ✅ Documented |
| 12 | Categories | MANAGEMENT | ✅ Documented |
| 13 | Employees | MANAGEMENT | ✅ Documented |
| 14 | Users | MANAGEMENT | ✅ Documented |
| 15 | Roles & Permissions | MANAGEMENT | ✅ Documented |
| 16 | Payment Methods | SETTINGS | ✅ Documented |
| 17 | Organization Settings | SETTINGS | ✅ Documented |

**Total: 17/17 Features Documented**

---

## References

- Shopify POS Design Principles (2024)
- Bright Inventions - Payment in POS Design Best Practices
- Retlia - Building Effective Retail KPI Dashboards
- LogRocket - Dashboard UI Best Practices
- Justinmind - Dashboard Design Best Practices
- Various inventory management documentation (Odoo, Hopstack)
- RBAC implementation guides (Permit.io, ScreenConnect)
- Multiple HR portal and employee management sources

---

*Document Version: 1.0*
*Last Updated: January 2024*
*Compiled for: ProNet POS Admin Interface Development*
