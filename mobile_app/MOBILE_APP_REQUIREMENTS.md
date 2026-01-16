# HospitalityPOS Owner Mobile App - Requirements Document

## Executive Summary

This document defines the requirements for a mobile application that enables business owners to monitor their retail/supermarket operations remotely. The app is designed as a **read-heavy, monitoring-focused** companion to the main POS system - not a replacement for it.

**Primary Goal**: Give business owners peace of mind by providing real-time visibility into their business operations from anywhere, at any time.

---

## Technology Decision: Flutter (Recommended)

### Why Flutter Over React Native

Based on comprehensive research, **Flutter** is the recommended framework for this application.

| Criteria | Flutter | React Native | Winner |
|----------|---------|--------------|--------|
| **Performance** | Near-native (60-120 FPS), 2.5x faster computations | Good but JS bridge overhead | Flutter |
| **Offline-First** | Excellent with SQLite, Drift, Brick packages | Good but more complex setup | Flutter |
| **Data-Heavy Dashboards** | Smooth scrolling, no bottlenecks with large datasets | Can cause bottlenecks with financial data | Flutter |
| **Charts & Visualizations** | fl_chart, syncfusion - excellent performance | Victory Native, good but heavier | Flutter |
| **Low-End Android Devices** | Significantly better on budget phones (common in Kenya) | Performance degrades on low-end devices | Flutter |
| **Firebase Integration** | Native Google integration, seamless | Good but third-party | Flutter |
| **Developer Availability (Kenya)** | Growing rapidly | More established | React Native |
| **Long-term Maintainability** | Single Dart codebase, consistent | JS ecosystem fragmentation | Flutter |

### Key Flutter Advantages for This Project

1. **Offline-First Architecture**: Flutter's `brick_offline_first` and `drift` packages provide enterprise-grade offline sync patterns that match our POS system's architecture.

2. **Performance on Budget Devices**: Most Kenyan business owners use mid-to-low-end Android phones. Flutter's Impeller rendering engine performs significantly better on these devices.

3. **Financial Dashboard Performance**: Research shows React Native can bottleneck on data-heavy financial dashboards. Flutter maintains 60 FPS even with complex charts and large datasets.

4. **Consistent UI**: Flutter draws every pixel, ensuring identical appearance on Android and iOS - important for support and documentation.

### Recommended Flutter Stack

```
State Management:     Riverpod 2.x (recommended) or BLoC
Local Database:       Drift (SQLite) or Isar
API Client:           Dio with interceptors
Offline Sync:         brick_offline_first_with_rest
Charts:               fl_chart or syncfusion_flutter_charts
Push Notifications:   Firebase Cloud Messaging (FCM)
Authentication:       JWT tokens with secure storage (flutter_secure_storage)
```

---

## App Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    MOBILE APP (Flutter)                      │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │   UI Layer  │  │   BLoC/     │  │   Repository Layer  │  │
│  │   (Screens) │◄─┤   Riverpod  │◄─┤   (Data Access)     │  │
│  └─────────────┘  └─────────────┘  └──────────┬──────────┘  │
│                                               │              │
│  ┌────────────────────────────────────────────┴───────────┐ │
│  │              Offline-First Data Layer                   │ │
│  │  ┌─────────────┐    ┌─────────────┐    ┌────────────┐  │ │
│  │  │ Local SQLite│◄──►│ Sync Engine │◄──►│ REST API   │  │ │
│  │  │  (Drift)    │    │  (Queue)    │    │ Client     │  │ │
│  │  └─────────────┘    └─────────────┘    └────────────┘  │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                  CLOUD SYNC GATEWAY API                      │
│           (ASP.NET Core - Already in POS System)             │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                   CENTRAL SQL SERVER DATABASE                │
└─────────────────────────────────────────────────────────────┘
```

### Offline-First Strategy

The app must work seamlessly when the owner has poor or no internet connectivity:

1. **Local Cache**: All viewed data is cached locally in SQLite
2. **Background Sync**: Data syncs in background when connectivity is available
3. **Optimistic UI**: Show cached data immediately, update when fresh data arrives
4. **Sync Indicators**: Clear visual indicators showing data freshness ("Last updated: 5 min ago")
5. **Manual Refresh**: Pull-to-refresh for on-demand sync

---

## Feature Modules

### Module 1: Dashboard (Home Screen)

**Purpose**: At-a-glance overview of business health

#### 1.1 Sales Summary Card

| Field | Description | Data Source |
|-------|-------------|-------------|
| Today's Sales | Total revenue for current day | `Receipt.Total` where `Date = Today` |
| Yesterday's Sales | Comparison reference | `Receipt.Total` where `Date = Yesterday` |
| This Week | Week-to-date total | Aggregated receipts |
| This Month | Month-to-date total | Aggregated receipts |
| vs Last Period | Percentage change indicator | Calculated |

**Visual**: Large prominent number with trend arrow (↑ green / ↓ red)

#### 1.2 Stock Valuation Card (Critical Feature)

This is a key differentiator - showing stock value changes throughout the day.

| Field | Description | Calculation |
|-------|-------------|-------------|
| **Opening Stock Value** | Stock value at start of business day | `StockValuationSnapshot` at WorkPeriod.StartTime |
| **Current Stock Value** | Real-time current valuation | `SUM(Inventory.Quantity * Product.CostPrice)` |
| **Closing Stock Value** | End of day value (if day closed) | `StockValuationSnapshot` at WorkPeriod.EndTime |
| **Stock Movement** | Change in value | `Opening - Current` |
| **Reconciliation** | Expected vs actual | `Opening - Sales(COGS) = Expected Current` |

**Display Logic**:
```
Morning (before business):
  "Opening Stock: KES 1,250,000"

During Business Hours:
  "Opening Stock: KES 1,250,000"
  "Current Stock: KES 1,180,000"
  "Change: -KES 70,000 (Sales: KES 85,000, COGS: KES 70,000)"

After Closing:
  "Opening Stock: KES 1,250,000"
  "Closing Stock: KES 1,150,000"
  "Day's Movement: -KES 100,000"
  "Gross Profit: KES 30,000"
```

#### 1.3 Quick Metrics Row

| Metric | Description |
|--------|-------------|
| Transactions | Number of receipts today |
| Average Ticket | Average sale value |
| Items Sold | Total line items |
| Active Cashiers | Currently logged-in staff |

#### 1.4 Alerts Summary

| Alert Type | Trigger | Priority |
|------------|---------|----------|
| Low Stock | Items below reorder point | High |
| Unpaid Invoices | Overdue supplier bills | High |
| Price Changes | Recent price modifications | Medium |
| Expiring Stock | Items nearing expiry | Medium |
| Large Transactions | Sales above threshold | Low |

---

### Module 2: Notifications & Activity Log

**Purpose**: Real-time awareness of business events and audit trail

#### 2.1 Push Notification Types

| Notification | Trigger | Content |
|--------------|---------|---------|
| **Low Stock Alert** | `Inventory.Quantity <= ReorderPoint` | "[Product] is running low (5 left). Reorder now?" |
| **Large Sale** | `Receipt.Total > ConfiguredThreshold` | "Large sale: KES 50,000 at [Store] by [Cashier]" |
| **Price Change** | `Product.Price` modified | "[Product] price changed from KES X to KES Y by [User]" |
| **Void/Refund** | Receipt voided | "Receipt #123 voided by [User]. Reason: [Reason]" |
| **Stock Adjustment** | Manual adjustment made | "Stock adjusted: [Product] +/- [Qty] by [User]" |
| **Day Close** | Z-Report generated | "Day closed at [Store]. Sales: KES X" |
| **Supplier Delivery** | GRN created | "Delivery received from [Supplier]: KES X" |
| **Invoice Due** | Payment due date approaching | "Invoice from [Supplier] due in 3 days: KES X" |

#### 2.2 Activity Log Screen

A searchable, filterable timeline of all business events.

**Log Entry Structure**:
```
┌─────────────────────────────────────────────────────────┐
│ 🏷️ PRICE CHANGE                           2:30 PM Today │
├─────────────────────────────────────────────────────────┤
│ Product: Coca-Cola 500ml                                │
│ Old Price: KES 80.00                                    │
│ New Price: KES 85.00                                    │
│ Changed By: John Kamau                                  │
│ Store: Main Branch                                      │
│ Reason: Supplier price increase                         │
└─────────────────────────────────────────────────────────┘
```

**Filter Options**:
- By Event Type (Price Change, Stock Adjustment, Void, etc.)
- By Store/Branch
- By User
- By Date Range
- By Product

**Log Types to Track**:

| Event Type | Fields Captured |
|------------|-----------------|
| Price Change | Product, OldPrice, NewPrice, User, Timestamp, Reason |
| Stock Adjustment | Product, OldQty, NewQty, User, Timestamp, Reason |
| Receipt Void | ReceiptNo, Amount, User, Timestamp, Reason, ApprovedBy |
| Discount Applied | ReceiptNo, DiscountAmount, User, Timestamp, ApprovalCode |
| User Login/Logout | User, Store, Terminal, Timestamp |
| Day Open/Close | Store, User, OpeningFloat, ClosingCash, Variance |
| Stock Transfer | FromStore, ToStore, Products, User, Timestamp |
| Supplier Payment | Supplier, Amount, User, Timestamp, PaymentMethod |

---

### Module 3: Reports

**Purpose**: Historical analysis and performance tracking

#### 3.1 Sales Reports

**Date Range Selector**: Today, Yesterday, This Week, This Month, Custom Range

**Sales Summary Report**:
| Metric | Description |
|--------|-------------|
| Gross Sales | Total before discounts |
| Discounts | Total discounts given |
| Net Sales | Gross - Discounts |
| Returns/Refunds | Returned items value |
| Tax Collected | VAT collected |
| Number of Transactions | Receipt count |
| Average Transaction | Net Sales / Transactions |
| Items Sold | Total line items |

**Sales by Category** (Pie/Bar Chart):
- Category name
- Revenue
- Percentage of total
- Quantity sold

**Sales by Payment Method**:
- Cash, M-Pesa, Card, Credit
- Amount and percentage

**Hourly Sales Trend** (Line Chart):
- Sales by hour for selected period
- Identify peak hours

**Top Selling Products** (Table):
- Product name
- Quantity sold
- Revenue
- Profit margin

**Sales by Cashier/Staff**:
- Employee name
- Total sales
- Transaction count
- Average ticket

#### 3.2 Inventory Reports

**Stock Levels Report**:
| Column | Description |
|--------|-------------|
| Product | Name and SKU |
| Category | Product category |
| Current Stock | Quantity on hand |
| Reorder Point | Configured threshold |
| Status | OK / Low / Out of Stock |
| Stock Value | Qty × Cost Price |
| Retail Value | Qty × Selling Price |

**Stock Movement Report**:
- Opening stock for period
- Purchases (received)
- Sales (sold)
- Adjustments (+/-)
- Transfers (+/-)
- Closing stock

---

### Module 4: Reorder Management

**Purpose**: Proactive inventory management

#### 4.1 Reorder Suggestions Screen

Display products that need reordering:

```
┌─────────────────────────────────────────────────────────┐
│ ⚠️ 23 Items Need Reordering                             │
├─────────────────────────────────────────────────────────┤
│ 🔴 OUT OF STOCK (5)                                     │
│ ┌─────────────────────────────────────────────────────┐ │
│ │ Bread - Superloaf 400g                              │ │
│ │ Stock: 0  |  Reorder: 50  |  Last Order: 3 days ago │ │
│ │ Supplier: Superloaf Ltd  |  Lead Time: 1 day        │ │
│ │ [📞 Call Supplier] [📝 Create PO]                   │ │
│ └─────────────────────────────────────────────────────┘ │
│                                                         │
│ 🟡 LOW STOCK (18)                                       │
│ ┌─────────────────────────────────────────────────────┐ │
│ │ Milk - Brookside 500ml                              │ │
│ │ Stock: 12  |  Reorder: 50  |  Daily Sales: 25       │ │
│ │ Days of Stock: ~0.5 days                            │ │
│ │ Suggested Order: 100 units                          │ │
│ └─────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
```

**Features**:
- Sort by urgency (out of stock first)
- Filter by category
- Filter by supplier
- Quick action: Call supplier (tap to dial)
- Quick action: Generate purchase order draft

---

### Module 5: Expenses Tracking

**Purpose**: Monitor operational expenses

#### 5.1 Expenses Dashboard

| Metric | Description |
|--------|-------------|
| Today's Expenses | Sum of expenses recorded today |
| This Month | Month-to-date expenses |
| By Category | Breakdown (Utilities, Rent, Salaries, etc.) |
| Pending Approvals | Expenses awaiting approval |

#### 5.2 Expense Categories

| Category | Examples |
|----------|----------|
| Utilities | Electricity, Water, Internet |
| Rent | Premises rent |
| Salaries | Staff wages |
| Transport | Delivery costs |
| Supplies | Office supplies, cleaning |
| Maintenance | Repairs, equipment service |
| Marketing | Advertising, promotions |
| Miscellaneous | Other operational costs |

#### 5.3 Expense Entry (If Allowed)

For convenience, allow owners to record expenses on-the-go:
- Category selection
- Amount
- Description
- Receipt photo (optional)
- Syncs to main POS system

---

### Module 6: Accounts Payable (Supplier Invoices)

**Purpose**: Track what the business owes to suppliers

#### 6.1 Unpaid Invoices Dashboard

```
┌─────────────────────────────────────────────────────────┐
│ 💰 ACCOUNTS PAYABLE                                      │
├─────────────────────────────────────────────────────────┤
│ Total Outstanding: KES 450,000                          │
│                                                         │
│ ⏰ Overdue:        KES 120,000  (3 invoices)            │
│ 📅 Due This Week:  KES 180,000  (5 invoices)            │
│ 📆 Due Later:      KES 150,000  (8 invoices)            │
└─────────────────────────────────────────────────────────┘
```

#### 6.2 Invoice List

| Column | Description |
|--------|-------------|
| Supplier | Supplier name |
| Invoice # | Supplier's invoice number |
| Amount | Invoice total |
| Due Date | Payment due date |
| Status | Overdue / Due Soon / Not Due |
| Age | Days since invoice date |

**Aging Buckets**:
- Current (not yet due)
- 1-30 days overdue
- 31-60 days overdue
- 61-90 days overdue
- 90+ days overdue

#### 6.3 Invoice Detail

- Supplier information
- Invoice details (number, date, due date)
- Line items (products received)
- GRN reference
- Payment history (partial payments)
- Outstanding balance

---

### Module 7: Financial Overview

**Purpose**: Complete picture of business financial health

#### 7.1 Cash Position Dashboard

```
┌─────────────────────────────────────────────────────────┐
│ 💵 CASH POSITION                        As of Today     │
├─────────────────────────────────────────────────────────┤
│                                                         │
│ AVAILABLE CASH                                          │
│ ├─ Cash in Registers:     KES    85,000                 │
│ ├─ Bank Account (KCB):    KES   450,000                 │
│ ├─ Bank Account (Equity): KES   280,000                 │
│ ├─ M-Pesa Float:          KES    35,000                 │
│ └─ Total Available:       KES   850,000                 │
│                                                         │
│ PENDING INFLOWS                                         │
│ ├─ Customer Credit (AR):  KES   120,000                 │
│ ├─ Pending M-Pesa:        KES     5,000                 │
│ └─ Total Expected:        KES   125,000                 │
│                                                         │
│ PENDING OUTFLOWS                                        │
│ ├─ Supplier Invoices:     KES   450,000                 │
│ ├─ Pending Cheques:       KES    80,000  ⚠️             │
│ ├─ Upcoming Expenses:     KES    60,000                 │
│ └─ Total Payables:        KES   590,000                 │
│                                                         │
│ ═══════════════════════════════════════════════════════ │
│ NET POSITION:             KES   385,000                 │
└─────────────────────────────────────────────────────────┘
```

#### 7.2 Cheque Management (Critical Feature)

Track pending cheques to ensure sufficient bank balance:

```
┌─────────────────────────────────────────────────────────┐
│ 📝 PENDING CHEQUES                                       │
├─────────────────────────────────────────────────────────┤
│ Cheque #001234 | To: ABC Suppliers | KES 50,000         │
│ Issue Date: Jan 10 | Expected Clear: Jan 15             │
│ Bank: KCB Main | Current Balance: KES 450,000           │
│ ✅ Sufficient funds                                      │
├─────────────────────────────────────────────────────────┤
│ Cheque #001235 | To: XYZ Distributors | KES 80,000      │
│ Issue Date: Jan 12 | Expected Clear: Jan 17             │
│ Bank: KCB Main | Current Balance: KES 450,000           │
│ ⚠️ After Cheque #001234 clears: KES 400,000            │
│ ✅ Sufficient funds                                      │
├─────────────────────────────────────────────────────────┤
│ SUMMARY                                                 │
│ Total Pending Cheques: KES 130,000                      │
│ Bank Balance After All Clear: KES 320,000               │
└─────────────────────────────────────────────────────────┘
```

**Cheque Tracking Fields**:
| Field | Description |
|-------|-------------|
| Cheque Number | Unique identifier |
| Payee | Recipient name |
| Amount | Cheque value |
| Issue Date | Date written |
| Expected Clearance | When it should clear |
| Bank Account | Which account it's drawn from |
| Status | Pending / Cleared / Bounced |

**Alert**: Warn if bank balance after pending cheques is below safety threshold.

#### 7.3 Daily Summary

```
┌─────────────────────────────────────────────────────────┐
│ 📊 TODAY'S FINANCIAL SUMMARY                             │
├─────────────────────────────────────────────────────────┤
│                                                         │
│ INFLOWS                                                 │
│ ├─ Cash Sales:            KES    65,000                 │
│ ├─ M-Pesa Sales:          KES    45,000                 │
│ ├─ Card Sales:            KES    12,000                 │
│ ├─ Credit Collections:    KES     8,000                 │
│ └─ Total Inflows:         KES   130,000                 │
│                                                         │
│ OUTFLOWS                                                │
│ ├─ Supplier Payments:     KES    45,000                 │
│ ├─ Expenses:              KES     5,500                 │
│ ├─ Salaries:              KES         0                 │
│ └─ Total Outflows:        KES    50,500                 │
│                                                         │
│ ═══════════════════════════════════════════════════════ │
│ NET CASH FLOW:            KES    79,500  ↑              │
└─────────────────────────────────────────────────────────┘
```

---

### Module 8: Multi-Store Management

**Purpose**: Overview and comparison of multiple branches

#### 8.1 Store Selector

- Persistent store selector in app header
- Options: "All Stores" or individual store
- All data filtered by selected store(s)

#### 8.2 Store Comparison Dashboard

```
┌─────────────────────────────────────────────────────────┐
│ 🏪 STORE COMPARISON                      Today          │
├─────────────────────────────────────────────────────────┤
│                                                         │
│ Store          | Sales      | Trans | Avg   | Status    │
│ ─────────────────────────────────────────────────────── │
│ Main Branch    | KES 85,000 | 156   | 545   | 🟢 Open   │
│ Westlands      | KES 62,000 | 98    | 633   | 🟢 Open   │
│ Mombasa Rd     | KES 41,000 | 72    | 569   | 🟢 Open   │
│ ─────────────────────────────────────────────────────── │
│ TOTAL          | KES 188,000| 326   | 577   |           │
└─────────────────────────────────────────────────────────┘
```

#### 8.3 Store Health Indicators

| Indicator | Green | Yellow | Red |
|-----------|-------|--------|-----|
| Sales vs Target | ≥100% | 80-99% | <80% |
| Stock Health | <5% low stock | 5-10% low | >10% low |
| Cash Variance | <1% | 1-3% | >3% |

---

### Module 9: Settings & Profile

#### 9.1 User Profile
- Business owner details
- Linked stores
- Contact information

#### 9.2 Notification Settings
- Enable/disable notification types
- Quiet hours
- Alert thresholds (e.g., "Notify for sales > KES 10,000")

#### 9.3 Security
- Change PIN/Password
- Biometric authentication (fingerprint/face)
- Session management
- Logout

#### 9.4 Data & Sync
- Last sync timestamp
- Manual sync button
- Clear cache option
- Data usage settings (WiFi only option)

---

## User Interface Guidelines

### Design Principles

1. **Glanceability**: Key metrics visible immediately without scrolling
2. **Thumb-Friendly**: All interactive elements ≥48x48 pixels
3. **Dark Mode**: Support for both light and dark themes (battery saving)
4. **Offline Indicators**: Clear visual cues when data is cached/stale
5. **Kenya-Optimized**:
   - Large, readable numbers
   - KES currency formatting
   - Date format: DD/MM/YYYY
   - Time format: 12-hour with AM/PM

### Color Coding

| Color | Meaning |
|-------|---------|
| Green | Positive (profit, increase, OK) |
| Red | Negative (loss, decrease, alert) |
| Orange | Warning (approaching threshold) |
| Blue | Information, neutral |
| Gray | Inactive, disabled |

### Typography

- **Headlines**: Bold, large (24-32pt)
- **Metrics**: Extra-large, prominent (36-48pt)
- **Body**: Regular (14-16pt)
- **Captions**: Light (12pt)

### Loading States

- Skeleton screens for initial load
- Pull-to-refresh with spinner
- Background sync indicator (subtle top bar)
- "Last updated" timestamp on all data

---

## API Requirements

### Authentication

```
POST /api/auth/login
POST /api/auth/refresh
POST /api/auth/logout
```

### Dashboard Data

```
GET /api/mobile/dashboard/summary
GET /api/mobile/dashboard/sales-today
GET /api/mobile/dashboard/stock-valuation
GET /api/mobile/dashboard/alerts
```

### Reports

```
GET /api/mobile/reports/sales?startDate=&endDate=&storeId=
GET /api/mobile/reports/sales-by-category?startDate=&endDate=
GET /api/mobile/reports/sales-by-hour?date=
GET /api/mobile/reports/top-products?startDate=&endDate=&limit=
GET /api/mobile/reports/inventory-status?storeId=
GET /api/mobile/reports/stock-movement?startDate=&endDate=&productId=
```

### Activity Log

```
GET /api/mobile/activity-log?type=&startDate=&endDate=&storeId=&userId=&page=&limit=
GET /api/mobile/activity-log/price-changes
GET /api/mobile/activity-log/stock-adjustments
GET /api/mobile/activity-log/voids
```

### Inventory

```
GET /api/mobile/inventory/reorder-suggestions?storeId=
GET /api/mobile/inventory/low-stock?storeId=
GET /api/mobile/inventory/expiring?days=&storeId=
```

### Financials

```
GET /api/mobile/financials/cash-position
GET /api/mobile/financials/accounts-payable
GET /api/mobile/financials/accounts-receivable
GET /api/mobile/financials/pending-cheques
GET /api/mobile/financials/daily-summary?date=
GET /api/mobile/invoices/unpaid?storeId=
GET /api/mobile/invoices/{id}
```

### Expenses

```
GET /api/mobile/expenses?startDate=&endDate=&category=
POST /api/mobile/expenses
GET /api/mobile/expenses/categories
```

### Multi-Store

```
GET /api/mobile/stores
GET /api/mobile/stores/{id}/summary
GET /api/mobile/stores/comparison?date=
```

### Push Notifications

```
POST /api/mobile/devices/register  (FCM token registration)
DELETE /api/mobile/devices/{deviceId}
GET /api/mobile/notifications/settings
PUT /api/mobile/notifications/settings
```

### Sync

```
GET /api/mobile/sync/status
POST /api/mobile/sync/pull?lastSyncTimestamp=
GET /api/mobile/sync/changes?since=
```

---

## Security Requirements

### Authentication
- JWT tokens with short expiry (15 min access, 7 day refresh)
- Secure token storage (flutter_secure_storage)
- Biometric authentication option
- PIN fallback

### Authorization
- Role-based access (Owner, Manager, Viewer)
- Store-level permissions
- Audit all API access

### Data Protection
- TLS 1.3 for all API communication
- Encrypt local SQLite database
- No sensitive data in logs
- Automatic logout after inactivity

### Device Security
- Prevent screenshots of sensitive screens (optional)
- Remote logout capability
- Device registration and management

---

## Offline Capabilities

### Data Cached Locally

| Data | Cache Duration | Priority |
|------|----------------|----------|
| Dashboard metrics | Until next sync | High |
| Recent activity log | 7 days | High |
| Product catalog | Until changed | Medium |
| Reports (generated) | 24 hours | Medium |
| Store list | Until changed | Low |

### Actions Available Offline

| Action | Offline Support |
|--------|-----------------|
| View cached dashboard | ✅ Yes |
| View cached reports | ✅ Yes |
| View activity log | ✅ Yes (cached entries) |
| Record expense | ✅ Yes (queued for sync) |
| Refresh data | ❌ No (requires network) |
| Real-time alerts | ❌ No (requires network) |

### Sync Strategy

1. **On App Open**: Check for connectivity, sync if available
2. **Background Sync**: Every 15 minutes when connected
3. **Manual Sync**: Pull-to-refresh gesture
4. **Priority Sync**: Critical data (dashboard) syncs first

---

## Push Notification Strategy

### Notification Channels (Android)

| Channel | Priority | Description |
|---------|----------|-------------|
| Critical Alerts | High | Low stock, large voids, system issues |
| Sales Updates | Default | Daily summaries, milestones |
| Informational | Low | Price changes, minor updates |

### Best Practices Implemented

1. **Personalization**: Include store name, product names, employee names
2. **Actionable**: Deep link to relevant screen
3. **Frequency Limits**: Max 2-3 critical alerts per day
4. **Quiet Hours**: Respect user's configured quiet time
5. **Rich Content**: Include amount, percentage change where relevant

### Notification Content Examples

```
🔴 CRITICAL: Out of Stock
Bread - Superloaf 400g is now out of stock at Main Branch.
[View Reorder Suggestions]

📈 Daily Sales Milestone
Main Branch has hit KES 100,000 in sales today! 🎉
That's 15% above yesterday.
[View Dashboard]

💰 Large Transaction
KES 25,000 sale processed by Jane at Westlands Branch.
Payment: M-Pesa
[View Details]

⚠️ Price Changed
Cooking Oil 5L: KES 1,200 → KES 1,350
Changed by: John Kamau at 2:30 PM
[View Activity Log]
```

---

## Performance Requirements

| Metric | Target |
|--------|--------|
| App Launch (cold) | < 3 seconds |
| App Launch (warm) | < 1 second |
| Dashboard Load (cached) | < 500ms |
| Dashboard Load (network) | < 3 seconds |
| Report Generation | < 5 seconds |
| Sync (incremental) | < 10 seconds |
| Memory Usage | < 150MB |
| Battery Impact | < 2% per hour (background) |

---

## Testing Requirements

### Unit Tests
- All business logic
- Data transformations
- Offline sync logic

### Integration Tests
- API communication
- Local database operations
- Push notification handling

### UI Tests
- Critical user flows
- Offline mode scenarios
- Multi-store switching

### Performance Tests
- Large dataset handling (1000+ products)
- Network latency simulation
- Battery consumption measurement

---

## Deployment

### App Stores
- **Google Play Store**: Primary (80%+ market in Kenya)
- **Apple App Store**: Secondary

### Distribution
- Internal testing (Firebase App Distribution)
- Beta testing with select customers
- Staged rollout (10% → 50% → 100%)

### Updates
- Force update for critical fixes
- Soft prompt for feature updates
- Background update download

---

## Future Enhancements (Phase 2+)

1. **WhatsApp Integration**: Send reports via WhatsApp
2. **Voice Summaries**: "Hey, how are my sales today?"
3. **Smart Alerts**: AI-powered anomaly detection
4. **Supplier Direct Ordering**: Create PO and send to supplier from app
5. **Customer Insights**: Top customers, purchase patterns
6. **Predictive Reordering**: AI suggests optimal order quantities
7. **Photo Reports**: Visual verification of stock, displays
8. **Team Chat**: In-app communication with staff

---

## Appendix A: Entity Mappings

### Dashboard → POS Entities

| Dashboard Field | POS Entity | Fields |
|-----------------|------------|--------|
| Today's Sales | Receipt | SUM(Total) WHERE Date = Today |
| Stock Value | Inventory + Product | SUM(Quantity × CostPrice) |
| Open Stock | StockValuationSnapshot | Value at WorkPeriod.Start |
| Transactions | Receipt | COUNT(*) WHERE Date = Today |
| Low Stock Items | Inventory + ReorderRule | WHERE Quantity <= ReorderPoint |
| Unpaid Invoices | SupplierInvoice | WHERE Status = Unpaid |
| Price Changes | AuditLog | WHERE EntityType = Product AND Field = Price |

### Activity Log → AuditLog Entity

| Log Type | AuditLog Filter |
|----------|-----------------|
| Price Change | EntityType = 'Product', FieldName = 'SellingPrice' |
| Stock Adjust | EntityType = 'StockMovement', Type = 'Adjustment' |
| Void | EntityType = 'ReceiptVoid' |
| User Login | EntityType = 'Session', Action = 'Login' |

---

## Appendix B: Wireframe Sketches

*[To be created in design phase]*

### Screen List
1. Login / Biometric Auth
2. Dashboard (Home)
3. Notifications List
4. Activity Log (with filters)
5. Reports Menu
6. Sales Report (with date picker)
7. Sales by Category
8. Inventory Status
9. Reorder Suggestions
10. Expenses List
11. Add Expense
12. Unpaid Invoices
13. Invoice Detail
14. Cash Position
15. Cheque Management
16. Store Comparison
17. Settings
18. Profile

---

## Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-01-17 | Claude | Initial requirements document |

---

## Sign-Off

- [ ] Product Owner
- [ ] Technical Lead
- [ ] UI/UX Designer
- [ ] Development Team Lead
