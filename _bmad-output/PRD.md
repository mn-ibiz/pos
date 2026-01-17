# Product Requirements Document (PRD)
# Multi-Mode Point of Sale (POS) System
## For Hospitality & Retail Businesses

**Document Version:** 2.0
**Date:** December 2025
**Technology Stack:** C# 14 / .NET 10 / MS SQL Server Express
**Platform:** Windows Desktop Application (Touch-Enabled)
**Business Modes:** Restaurant | Supermarket | Hybrid

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Product Vision and Goals](#2-product-vision-and-goals)
3. [Target Users and Market](#3-target-users-and-market)
4. [User Roles and Permissions](#4-user-roles-and-permissions)
5. [Core Functional Requirements](#5-core-functional-requirements)
6. [Product and Inventory Management Module](#6-product-and-inventory-management-module)
7. [Reporting and Analytics](#7-reporting-and-analytics)
8. [User Interface Requirements](#8-user-interface-requirements)
9. [Technical Architecture](#9-technical-architecture)
10. [Security and Compliance](#10-security-and-compliance)
11. [Non-Functional Requirements](#11-non-functional-requirements)
12. [**NEW** Supermarket/Retail Mode Features](#12-supermarket-retail-mode-features)
13. [**NEW** Product Offers & Promotions](#13-product-offers-promotions)
14. [**NEW** Supplier Credit Management](#14-supplier-credit-management)
15. [**NEW** Employee & Payroll Module](#15-employee-payroll-module)
16. [**NEW** Accounting Module](#16-accounting-module)
17. [Future Considerations](#17-future-considerations)
18. [Glossary](#18-glossary)

---

## 1. Executive Summary

### 1.1 Purpose

This document defines the product requirements for a comprehensive, touch-enabled Point of Sale (POS) system designed for **multiple business verticals** - including hospitality (hotels, bars, restaurants) and retail (supermarkets, shops). The system operates in configurable modes to match specific business needs while sharing a common core platform.

### 1.2 Business Mode Overview

| Mode | Target Business | Key Characteristics |
|------|-----------------|---------------------|
| **Restaurant** | Hotels, Bars, Restaurants | Table management, waiter assignment, kitchen display, split bills, tabs |
| **Supermarket** | Retail stores, Supermarkets | Barcode scanning, product offers, supplier credit, fast checkout, payroll |
| **Hybrid** | Mixed operations | All features enabled, context-aware UI |

### 1.3 Product Overview

The Multi-Mode POS System is a desktop application built with C# 14 / .NET 10 and MS SQL Server Express that provides:

- **Multi-user environment** with role-based access control
- **Touch-optimized interface** for fast-paced hospitality environments
- **Work period management** for controlled shift operations
- **Comprehensive order and receipt management** including bill splitting, merging, and item additions
- **Flexible payment processing** supporting multiple payment methods (Cash, Card, M-Pesa, etc.)
- **Real-time inventory tracking** with stock management and purchase receiving
- **End-of-day reporting** with X and Z reports
- **Full audit trail** for accountability and compliance

### 1.3 Key Differentiators

| Feature | Description |
|---------|-------------|
| Touch-First Design | Large, finger-friendly buttons optimized for touchscreen operation |
| Visual Product Display | Products displayed with images and prices for quick identification |
| Flexible Receipt Workflow | Support for both auto-settle and manual settlement modes |
| Owner-Locked Receipts | Only receipt owners can modify their own receipts |
| Incremental Order Printing | Only new items print when adding to existing receipts |
| Comprehensive Void Tracking | All voided transactions remain visible for audit purposes |

---

## 2. Product Vision and Goals

### 2.1 Vision Statement

To create a world-class, intuitive POS system that empowers hospitality businesses to operate efficiently, maintain financial control, and deliver exceptional customer service through streamlined order management and real-time operational insights.

### 2.2 Business Goals

1. **Operational Efficiency**: Reduce order processing time by 40% through touch-optimized workflows
2. **Financial Accuracy**: Eliminate manual calculation errors and provide complete transaction accountability
3. **Inventory Control**: Reduce stock discrepancies by 90% through real-time tracking
4. **Staff Accountability**: Provide complete audit trails for all transactions and modifications
5. **Business Intelligence**: Deliver actionable insights through comprehensive sales and performance reports

### 2.3 Success Metrics

| Metric | Target |
|--------|--------|
| Average transaction time | < 30 seconds |
| System uptime | 99.9% during business hours |
| End-of-day reconciliation time | < 15 minutes |
| Stock accuracy rate | > 98% |
| User training time | < 2 hours for waiters |

---

## 3. Target Users and Market

### 3.1 Primary Market Segments

1. **Hotels** - F&B operations including restaurants, bars, room service, and poolside service
2. **Standalone Restaurants** - Full-service and quick-service establishments
3. **Bars and Lounges** - Nightclubs, pubs, and cocktail bars
4. **Cafes and Coffee Shops** - Quick-service beverage and snack establishments

### 3.2 Business Size

- Small to medium enterprises (1-50 employees)
- Single to multi-outlet operations
- Daily transaction volume: 50-1000+ transactions

### 3.3 Geographic Focus

- Primary: East African market (Kenya, Tanzania, Uganda)
- Payment integrations: M-Pesa, Airtel Money, Card payments, Cash

---

## 4. User Roles and Permissions

### 4.1 Role Hierarchy

The system implements a hierarchical role-based access control (RBAC) system with the following default roles:

```
Administrator (Owner)
    └── Manager
        └── Supervisor
            └── Cashier
                └── Waiter
```

### 4.2 Role Definitions

#### 4.2.1 Administrator / Owner

**Description**: Full system access with complete control over all operations, settings, and data.

| Permission Category | Permissions |
|---------------------|-------------|
| Work Period | Open, Close, View History |
| Sales | Create, View All, Modify Any, Void Any |
| Receipts | View All, Void, Reprint, Split, Merge |
| Products | Create, Edit, Delete, Set Prices |
| Inventory | Full Access (Stock, Purchases, Adjustments) |
| Users | Create, Edit, Delete, Assign Roles |
| Roles | Create, Edit, Delete, Assign Permissions |
| Reports | All Reports (X, Z, Sales, Inventory, Audit) |
| Settings | Full System Configuration |
| Discounts | Apply Any Discount |
| Voids | Void Any Transaction |

#### 4.2.2 Manager

**Description**: Day-to-day operational management with limited system configuration access.

| Permission Category | Permissions |
|---------------------|-------------|
| Work Period | Open, Close |
| Sales | Create, View All, Void (with reason) |
| Receipts | View All, Void, Reprint, Split, Merge |
| Products | Create, Edit (no delete) |
| Inventory | View Stock, Receive Purchases, Adjustments |
| Users | Create Cashiers/Waiters, Reset Passwords |
| Reports | X Report, Z Report, Sales Reports |
| Discounts | Apply Up to 50% Discount |
| Voids | Void Transactions (with mandatory reason) |

#### 4.2.3 Supervisor

**Description**: Floor supervision with authority over cashiers and waiters.

| Permission Category | Permissions |
|---------------------|-------------|
| Work Period | View Status Only |
| Sales | Create, View Team Sales |
| Receipts | View Team Receipts, Reprint |
| Products | View Only |
| Inventory | View Stock Levels |
| Reports | X Report (Team), Sales Summary |
| Discounts | Apply Up to 20% Discount |
| Voids | Request Void (requires Manager approval) |

#### 4.2.4 Cashier

**Description**: Handles payments and manages the cash register.

| Permission Category | Permissions |
|---------------------|-------------|
| Sales | Create, View Own Sales |
| Receipts | View Own, Settle, Reprint Own |
| Payment | Accept All Payment Methods |
| Cash Drawer | Open, Close, Count |
| Reports | Own Shift Summary |
| Discounts | Apply Up to 10% Discount |

#### 4.2.5 Waiter / Server

**Description**: Limited access focused on order taking and basic sales functions.

| Permission Category | Permissions |
|---------------------|-------------|
| Sales | Create Orders, View Own Orders |
| Receipts | View Own Receipts, Add Items to Own Receipts |
| Tables | Assign, Transfer (own tables only) |
| Products | View Only (with prices) |
| Reports | View Own Sales Summary (current session) |
| Discounts | None (must request from Supervisor/Manager) |

### 4.3 Custom Role Management

The system allows administrators to:

1. **Create Custom Roles**: Define new roles with specific permission sets
2. **Clone Existing Roles**: Copy a role and modify permissions
3. **Permission Granularity**: Toggle individual permissions within categories
4. **Role Assignment**: Assign multiple roles to a single user if needed
5. **Temporary Elevation**: Allow temporary permission elevation with PIN authorization

### 4.4 Permission Override Workflow

When a user lacks permission for an action:

1. System displays "Authorization Required" prompt
2. User with higher permission enters their PIN
3. Action is executed and logged with both user IDs
4. Audit trail records the override event

---

## 5. Core Functional Requirements

### 5.1 Work Period Management

The work period is the fundamental operational unit that defines a business day or shift.

#### 5.1.1 Opening a Work Period

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| WP-001 | Only Manager or Administrator can open a work period | Must Have |
| WP-002 | System prevents sales transactions when no work period is active | Must Have |
| WP-003 | Opening requires entering opening cash float amount | Must Have |
| WP-004 | System records opening timestamp and user | Must Have |
| WP-005 | Optional: Carry forward previous day's closing balance | Should Have |
| WP-006 | Display clear visual indicator when work period is active | Must Have |

#### 5.1.2 During Work Period

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| WP-010 | All users can log in and perform authorized actions | Must Have |
| WP-011 | Real-time tracking of all transactions | Must Have |
| WP-012 | Ability to run X Reports at any time | Must Have |
| WP-013 | Display work period duration on dashboard | Should Have |

#### 5.1.3 Closing a Work Period

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| WP-020 | Only Manager or Administrator can close work period | Must Have |
| WP-021 | System warns if there are unsettled receipts | Must Have |
| WP-022 | Require cash count entry before closing | Must Have |
| WP-023 | Calculate and display cash variance (expected vs. actual) | Must Have |
| WP-024 | Automatically generate Z Report on close | Must Have |
| WP-025 | Print Z Report and X Report | Must Have |
| WP-026 | Lock all transactions for the closed period | Must Have |
| WP-027 | Option to print detailed end-of-day summary | Should Have |

### 5.2 Sales and Order Management

#### 5.2.1 Creating Orders

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| SO-001 | Touch-friendly product grid with images and prices | Must Have |
| SO-002 | Quick search by product name or code | Must Have |
| SO-003 | Category/subcategory navigation | Must Have |
| SO-004 | Quantity adjustment with +/- buttons | Must Have |
| SO-005 | Item modifiers (e.g., "no ice", "extra spicy") | Should Have |
| SO-006 | Order notes/special instructions | Should Have |
| SO-007 | Associate order with table number | Must Have |
| SO-008 | Associate order with customer name (optional) | Should Have |
| SO-009 | Display running total in real-time | Must Have |
| SO-010 | Hold order functionality (save without printing) | Should Have |

#### 5.2.2 Order Printing

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| SO-020 | Print kitchen order ticket (KOT) on order submission | Must Have |
| SO-021 | Print customer receipt | Must Have |
| SO-022 | Separate printer routing for kitchen vs. bar vs. receipt | Should Have |
| SO-023 | Order ticket includes: items, quantities, modifiers, table, server | Must Have |
| SO-024 | Receipt includes: items, prices, totals, payment method, timestamp | Must Have |
| SO-025 | Configurable receipt header/footer (business name, address, etc.) | Must Have |
| SO-026 | Optional: Logo printing on receipts | Could Have |

### 5.3 Receipt Management

#### 5.3.1 Receipt Lifecycle

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   CREATED   │────▶│   PENDING   │────▶│   SETTLED   │
└─────────────┘     └─────────────┘     └─────────────┘
       │                   │                    │
       │                   ▼                    │
       │            ┌─────────────┐             │
       │            │   VOIDED    │◀────────────┘
       │            └─────────────┘
       │                   ▲
       └───────────────────┘
```

#### 5.3.2 Receipt Settlement Modes

**Mode A: Auto-Settle on Print**
| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| RS-001 | Configure system to auto-settle receipts on print | Must Have |
| RS-002 | Payment method selection before printing | Must Have |
| RS-003 | Receipt prints as "PAID" with payment method | Must Have |

**Mode B: Manual Settlement (Pending Mode)**
| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| RS-010 | Receipt remains in "Pending" status after printing order | Must Have |
| RS-011 | Pending receipts displayed in dedicated queue | Must Have |
| RS-012 | Cashier can select pending receipt to settle | Must Have |
| RS-013 | Settlement requires payment method selection | Must Have |
| RS-014 | Split payment support (e.g., part cash, part M-Pesa) | Should Have |
| RS-015 | Print final receipt on settlement | Must Have |

#### 5.3.3 Adding Items to Existing Receipts

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| RA-001 | Only receipt owner can add items to their receipt | Must Have |
| RA-002 | Manager override available for non-owner additions | Should Have |
| RA-003 | When adding items, only NEW items print on kitchen order | Must Have |
| RA-004 | Combined receipt prints all items (original + additions) | Must Have |
| RA-005 | System tracks which items were added in which batch | Must Have |
| RA-006 | Visual indication of "added" items on receipt | Should Have |

#### 5.3.4 Bill Splitting

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| SP-001 | Split receipt into multiple receipts | Must Have |
| SP-002 | Drag-and-drop items between split receipts | Must Have |
| SP-003 | Split equally by number of people | Should Have |
| SP-004 | Split by selected items | Must Have |
| SP-005 | Split by seat number (if tables have seats) | Could Have |
| SP-006 | Each split receipt can have different payment method | Must Have |
| SP-007 | Original receipt maintains reference to split receipts | Must Have |
| SP-008 | Audit trail for all split operations | Must Have |

**Example Split Workflow:**
```
Original Receipt: $30 (3 items: Beer $10, Pizza $15, Soda $5)
                           │
        ┌──────────────────┼──────────────────┐
        ▼                  ▼                  ▼
   Receipt A          Receipt B          Receipt C
   Beer: $10          Pizza: $15         Soda: $5
   (Cash)             (M-Pesa)           (Card)
```

#### 5.3.5 Bill Merging

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| MG-001 | Merge multiple receipts into one | Should Have |
| MG-002 | Only pending/unsettled receipts can be merged | Must Have |
| MG-003 | Merged receipt shows all items from source receipts | Must Have |
| MG-004 | Source receipts archived with reference to merged receipt | Must Have |

#### 5.3.6 Voiding Receipts/Transactions

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| VD-001 | Only Manager or Administrator can void receipts | Must Have |
| VD-002 | Void requires mandatory reason selection/entry | Must Have |
| VD-003 | Voided receipts remain visible in system (marked as VOID) | Must Have |
| VD-004 | Voided items do not count toward sales totals | Must Have |
| VD-005 | Void reverses inventory deductions (returns stock) | Should Have |
| VD-006 | Audit log records: who voided, when, why | Must Have |
| VD-007 | Voided receipts appear in end-of-day void report | Must Have |
| VD-008 | Option to void individual items vs. entire receipt | Should Have |

### 5.4 Payment Processing

#### 5.4.1 Supported Payment Methods

| Payment Method | Description | Priority |
|----------------|-------------|----------|
| Cash | Physical currency | Must Have |
| M-Pesa | Mobile money (Safaricom) | Must Have |
| Airtel Money | Mobile money (Airtel) | Should Have |
| Credit/Debit Card | Visa, Mastercard | Should Have |
| Room Charge | Charge to hotel room (for hotels) | Could Have |
| Voucher/Gift Card | Prepaid vouchers | Could Have |
| Credit Account | Customer credit account | Could Have |
| Split Payment | Multiple methods for one receipt | Must Have |

#### 5.4.2 Payment Workflow

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| PY-001 | Display total amount due prominently | Must Have |
| PY-002 | Quick-select buttons for payment methods | Must Have |
| PY-003 | Cash payment: Calculate and display change | Must Have |
| PY-004 | M-Pesa: Record transaction code | Must Have |
| PY-005 | Card: Record last 4 digits (optional) | Could Have |
| PY-006 | Print payment confirmation on receipt | Must Have |
| PY-007 | Handle partial payments and balance tracking | Should Have |

#### 5.4.3 Cash Drawer Management

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| CD-001 | Automatic drawer open on cash transactions | Must Have |
| CD-002 | Manual drawer open (with permission) | Must Have |
| CD-003 | Track all drawer open events | Must Have |
| CD-004 | Cash drop (remove excess cash) with logging | Should Have |
| CD-005 | Paid in/Paid out transactions (non-sale) | Should Have |

### 5.5 Table and Floor Management

#### 5.5.1 Floor Plan Configuration

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| TM-001 | Visual floor plan designer | Should Have |
| TM-002 | Drag-and-drop table placement | Should Have |
| TM-003 | Multiple floor support (e.g., Ground Floor, Rooftop) | Should Have |
| TM-004 | Table shapes (round, square, rectangular) | Could Have |
| TM-005 | Table capacity (number of seats) | Should Have |
| TM-006 | Section/zone definition (e.g., Smoking, Non-smoking) | Could Have |

#### 5.5.2 Table Status Management

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| TM-010 | Visual table status indicators (Available, Occupied, Reserved) | Should Have |
| TM-011 | Color-coded status display | Should Have |
| TM-012 | Show assigned waiter per table | Should Have |
| TM-013 | Show current bill amount per table | Should Have |
| TM-014 | Table timer (time since seated) | Could Have |
| TM-015 | Transfer table to another waiter | Should Have |

### 5.6 Kitchen Display System (KDS) Integration

#### 5.6.1 Order Routing

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| KD-001 | Route orders to appropriate preparation station | Should Have |
| KD-002 | Separate queues for Kitchen, Bar, Cold Station | Should Have |
| KD-003 | Priority/Rush order flagging | Should Have |
| KD-004 | Order bump (mark as complete) functionality | Should Have |
| KD-005 | Add-on items displayed separately with "ADD-ON" label | Should Have |

#### 5.6.2 Kitchen Printer Integration

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| KD-010 | Print orders to designated kitchen printers | Must Have |
| KD-011 | Large, clear font for kitchen tickets | Must Have |
| KD-012 | Include modifiers and special instructions | Must Have |
| KD-013 | Order number and table number prominently displayed | Must Have |
| KD-014 | Timestamp on all kitchen tickets | Must Have |

---

## 6. Product and Inventory Management Module

### 6.1 Product Management

#### 6.1.1 Product Information

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| PM-001 | Product name (display name) | Must Have |
| PM-002 | Product code/SKU | Must Have |
| PM-003 | Product category and subcategory | Must Have |
| PM-004 | Selling price | Must Have |
| PM-005 | Cost price | Must Have |
| PM-006 | Product image (for POS display) | Must Have |
| PM-007 | Product description | Should Have |
| PM-008 | Barcode/QR code | Should Have |
| PM-009 | Unit of measure (each, kg, liter, etc.) | Must Have |
| PM-010 | Tax category/rate | Must Have |
| PM-011 | Active/Inactive status | Must Have |
| PM-012 | Minimum stock level (reorder point) | Should Have |
| PM-013 | Maximum stock level | Should Have |

#### 6.1.2 Product Categories

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| PC-001 | Create unlimited categories | Must Have |
| PC-002 | Hierarchical subcategories | Should Have |
| PC-003 | Category image/icon | Should Have |
| PC-004 | Category display order | Must Have |
| PC-005 | Category-based reporting | Should Have |

#### 6.1.3 Product Modifiers

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| PD-001 | Create modifier groups (e.g., "Size", "Temperature") | Should Have |
| PD-002 | Modifier options with price adjustments | Should Have |
| PD-003 | Required vs. optional modifiers | Should Have |
| PD-004 | Assign modifier groups to products | Should Have |

#### 6.1.4 Combo/Bundle Products

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| CB-001 | Create combo products from multiple items | Should Have |
| CB-002 | Combo pricing (fixed or discounted sum) | Should Have |
| CB-003 | Component inventory deduction | Should Have |

### 6.2 Inventory/Stock Management

#### 6.2.1 Stock Tracking

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| ST-001 | Real-time stock quantity tracking | Must Have |
| ST-002 | Automatic stock deduction on sale | Must Have |
| ST-003 | Stock return on void/refund | Must Have |
| ST-004 | Current stock level display | Must Have |
| ST-005 | Stock value calculation (quantity × cost) | Must Have |
| ST-006 | Stock history/movement log | Must Have |

#### 6.2.2 Stock Alerts

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| SA-001 | Low stock alerts when below minimum level | Must Have |
| SA-002 | Out-of-stock alerts | Must Have |
| SA-003 | Dashboard notification for stock alerts | Should Have |
| SA-004 | Optional: Email/SMS alerts for critical stock levels | Could Have |
| SA-005 | Auto-86 items when stock reaches zero | Should Have |

#### 6.2.3 Stock Adjustments

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| SJ-001 | Manual stock adjustment (increase/decrease) | Must Have |
| SJ-002 | Adjustment reason required | Must Have |
| SJ-003 | Adjustment authorization (Manager+) | Should Have |
| SJ-004 | Adjustment history and audit trail | Must Have |
| SJ-005 | Variance reports (expected vs. actual) | Should Have |

### 6.3 Purchase Receiving

#### 6.3.1 Supplier Management

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| SU-001 | Create and manage suppliers | Must Have |
| SU-002 | Supplier contact information | Must Have |
| SU-003 | Supplier product associations | Should Have |
| SU-004 | Supplier payment terms | Could Have |
| SU-005 | Supplier performance tracking | Could Have |

#### 6.3.2 Purchase Orders

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| PO-001 | Create purchase orders | Should Have |
| PO-002 | Select supplier and products | Should Have |
| PO-003 | Specify quantities and expected prices | Should Have |
| PO-004 | PO approval workflow | Could Have |
| PO-005 | Print/Email purchase orders | Should Have |
| PO-006 | Track PO status (Draft, Sent, Partially Received, Complete) | Should Have |

#### 6.3.3 Goods Receiving

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| GR-001 | Receive goods against purchase order | Must Have |
| GR-002 | Direct receiving without PO | Must Have |
| GR-003 | Record received quantities | Must Have |
| GR-004 | Record actual cost prices | Must Have |
| GR-005 | Partial receiving support | Should Have |
| GR-006 | Automatic stock quantity update on receiving | Must Have |
| GR-007 | Goods Received Note (GRN) generation | Must Have |
| GR-008 | Barcode scanning for receiving | Should Have |
| GR-009 | Batch/Lot number tracking | Could Have |
| GR-010 | Expiry date tracking | Could Have |

#### 6.3.4 Stock Take / Physical Inventory

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| SK-001 | Create stock take sessions | Must Have |
| SK-002 | Enter physical count quantities | Must Have |
| SK-003 | Calculate variance (system vs. physical) | Must Have |
| SK-004 | Variance value calculation | Must Have |
| SK-005 | Approve and apply stock take adjustments | Must Have |
| SK-006 | Stock take reports | Must Have |
| SK-007 | Partial stock takes (by category/location) | Should Have |

### 6.4 Recipe/Ingredient Management (For Prepared Items)

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| RC-001 | Define recipes for prepared menu items | Should Have |
| RC-002 | Link raw ingredients to finished products | Should Have |
| RC-003 | Automatic ingredient deduction on sale | Should Have |
| RC-004 | Recipe costing (calculate cost from ingredients) | Should Have |
| RC-005 | Yield/portion management | Could Have |

---

## 7. Reporting and Analytics

### 7.1 X Report (Mid-Day/Mid-Shift Report)

**Purpose**: Provides a snapshot of sales activity since the work period opened or since the last Z Report, without resetting any counters.

#### 7.1.1 X Report Contents

| Section | Details |
|---------|---------|
| Header | Business name, date/time, report number, user who generated |
| Sales Summary | Gross sales, discounts, net sales, tax collected |
| Sales by Category | Breakdown of sales per product category |
| Sales by Payment Method | Cash, M-Pesa, Card, etc. with totals |
| Sales by Cashier/User | Individual sales totals per staff member |
| Transaction Counts | Number of transactions, average transaction value |
| Voids Summary | Number of voids, void value, void reasons (if any during period) |

### 7.2 Z Report (End-of-Day Report)

**Purpose**: Final report for the work period that resets all counters and closes the business day. Required for tax compliance and accounting.

#### 7.2.1 Z Report Contents

| Section | Details |
|---------|---------|
| Header | Business name, date, Z-Report number (sequential), closing user |
| Work Period Info | Open time, close time, duration |
| **Sales Summary** | |
| - Gross Sales | Total sales before discounts |
| - Discounts | Total discounts applied |
| - Net Sales | Gross sales minus discounts |
| - Tax Collected | Breakdown by tax rate |
| - Grand Total | Final total collected |
| **Sales by User/Cashier** | |
| - Per User | Sales total, transaction count, average |
| **Payment Method Breakdown** | |
| - Cash | Amount, transaction count |
| - M-Pesa | Amount, transaction count |
| - Card | Amount, transaction count |
| - Other Methods | Amount, transaction count |
| **Settlements** | |
| - Settled Receipts | Count, total value |
| - Pending Receipts | Count, total value (warning if any) |
| **Voids Section** | |
| - Void Count | Number of voided transactions |
| - Void Value | Total value of voids |
| - Void Details | Receipt #, amount, voided by, reason |
| **Items Sold Summary** | |
| - By Category | Category name, quantity, value |
| - Detailed List | Item name, quantity sold, total value |
| **Cash Drawer** | |
| - Opening Balance | Starting cash |
| - Cash Sales | Cash received |
| - Cash Payouts | Cash removed |
| - Expected Balance | Calculated expected cash |
| - Actual Count | Entered physical count |
| - Variance | Difference (over/short) |
| Footer | Report generation timestamp, sequential Z number |

### 7.3 Additional Reports

#### 7.3.1 Sales Reports

| Report | Description | Priority |
|--------|-------------|----------|
| Daily Sales Summary | Sales totals by day | Must Have |
| Hourly Sales Analysis | Sales breakdown by hour | Should Have |
| Product Sales Report | Sales by individual product | Must Have |
| Category Sales Report | Sales by product category | Must Have |
| User/Cashier Performance | Sales per staff member | Must Have |
| Payment Method Report | Breakdown by payment type | Must Have |
| Discount Report | All discounts applied with details | Should Have |
| Void Report | All voided transactions with reasons | Must Have |
| Refund Report | All refunds processed | Should Have |

#### 7.3.2 Inventory Reports

| Report | Description | Priority |
|--------|-------------|----------|
| Current Stock Report | Stock levels for all products | Must Have |
| Low Stock Report | Products below reorder point | Must Have |
| Stock Movement Report | All stock ins/outs with reasons | Must Have |
| Stock Valuation Report | Total inventory value | Should Have |
| Purchase History | All purchases by date/supplier | Must Have |
| Stock Take Variance | Variance from last stock take | Should Have |
| Dead Stock Report | Items with no movement | Should Have |

#### 7.3.3 Audit Reports

| Report | Description | Priority |
|--------|-------------|----------|
| Transaction Log | All transactions with timestamps | Must Have |
| User Activity Log | Login/logout and actions per user | Must Have |
| Void/Refund Log | All voids and refunds with authorization | Must Have |
| Price Change Log | History of price modifications | Should Have |
| Permission Override Log | All manager overrides | Should Have |

---

## 8. User Interface Requirements

### 8.1 General UI Principles

| Principle | Description |
|-----------|-------------|
| Touch-First | All interactive elements minimum 44x44 pixels |
| High Contrast | Clear visibility in various lighting conditions |
| Minimal Clicks | Most common actions within 2-3 taps |
| Visual Feedback | Clear indication of selected/active states |
| Error Prevention | Confirmation dialogs for destructive actions |
| Accessibility | Support for larger fonts, high contrast modes |

### 8.2 Main POS Screen Layout

```
┌─────────────────────────────────────────────────────────────────────────┐
│  [Logo]  HOSPITALITY POS          Work Period: OPEN    User: John Doe  │
│          12/20/2025 14:35         Table: 5             [Logout]        │
├────────────────────────────────────┬────────────────────────────────────┤
│                                    │                                    │
│        PRODUCT GRID                │         ORDER PANEL                │
│   ┌─────────┐  ┌─────────┐        │   ┌────────────────────────────┐   │
│   │ [IMG]   │  │ [IMG]   │        │   │ Beer (x2)           $20.00 │   │
│   │ Beer    │  │ Wine    │        │   │ Pizza               $15.00 │   │
│   │ $10.00  │  │ $12.00  │        │   │ Soda                 $5.00 │   │
│   └─────────┘  └─────────┘        │   └────────────────────────────┘   │
│   ┌─────────┐  ┌─────────┐        │                                    │
│   │ [IMG]   │  │ [IMG]   │        │   Subtotal:             $40.00    │
│   │ Pizza   │  │ Burger  │        │   Tax (16%):             $6.40    │
│   │ $15.00  │  │ $12.00  │        │   ─────────────────────────────   │
│   └─────────┘  └─────────┘        │   TOTAL:                $46.40    │
│                                    │                                    │
│   [DRINKS] [FOOD] [DESSERTS]      │   [HOLD]  [CLEAR]  [PRINT ORDER]  │
│                                    │                                    │
│   🔍 Search...                     │   [SETTLE]         [SPLIT BILL]   │
│                                    │                                    │
└────────────────────────────────────┴────────────────────────────────────┘
```

### 8.3 Product Display Requirements

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| UI-001 | Products displayed as touch-friendly tiles | Must Have |
| UI-002 | Product image prominently displayed | Must Have |
| UI-003 | Product name clearly visible | Must Have |
| UI-004 | Price displayed on product tile | Must Have |
| UI-005 | Visual indicator for out-of-stock items | Should Have |
| UI-006 | Category tabs/buttons for navigation | Must Have |
| UI-007 | Quick search with on-screen keyboard | Must Have |
| UI-008 | Favorite/frequent items section | Should Have |
| UI-009 | Grid view with adjustable tile size | Should Have |

### 8.4 Screen Requirements

| Screen | Description | Priority |
|--------|-------------|----------|
| Login Screen | PIN or username/password entry | Must Have |
| Main POS Screen | Product selection and order entry | Must Have |
| Payment Screen | Payment method selection and processing | Must Have |
| Receipt List Screen | View and manage receipts | Must Have |
| Table Map Screen | Visual floor plan with tables | Should Have |
| Reports Screen | Generate and view reports | Must Have |
| Product Management Screen | Add/edit products | Must Have |
| Inventory Screen | Stock levels and adjustments | Must Have |
| User Management Screen | Manage users and roles | Must Have |
| Settings Screen | System configuration | Must Have |

---

## 9. Technical Architecture

### 9.1 Technology Stack

| Component | Technology |
|-----------|------------|
| Programming Language | C# 14 (.NET 10 LTS) |
| UI Framework | WPF (Windows Presentation Foundation) or WinUI 3 |
| Database | Microsoft SQL Server Express |
| ORM | Entity Framework Core |
| Reporting | RDLC Reports or FastReport |
| Printing | ESC/POS for thermal printers |

### 9.2 System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                      PRESENTATION LAYER                          │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │
│  │ POS Screen  │  │   Admin     │  │  Reports    │              │
│  │   (WPF)     │  │   Panel     │  │   Viewer    │              │
│  └─────────────┘  └─────────────┘  └─────────────┘              │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      BUSINESS LOGIC LAYER                        │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │
│  │   Sales     │  │  Inventory  │  │   User      │              │
│  │  Service    │  │   Service   │  │  Service    │              │
│  └─────────────┘  └─────────────┘  └─────────────┘              │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │
│  │  Payment    │  │  Reporting  │  │   Audit     │              │
│  │  Service    │  │   Service   │  │  Service    │              │
│  └─────────────┘  └─────────────┘  └─────────────┘              │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      DATA ACCESS LAYER                           │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │              Entity Framework Core                       │    │
│  │  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐        │    │
│  │  │ Orders  │ │Products │ │  Users  │ │Inventory│        │    │
│  │  │  Repo   │ │  Repo   │ │  Repo   │ │  Repo   │        │    │
│  │  └─────────┘ └─────────┘ └─────────┘ └─────────┘        │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      DATABASE LAYER                              │
│           ┌─────────────────────────────────┐                   │
│           │    MS SQL Server Express        │                   │
│           │    ┌─────────────────────┐      │                   │
│           │    │  HospitalityPOS_DB  │      │                   │
│           │    └─────────────────────┘      │                   │
│           └─────────────────────────────────┘                   │
└─────────────────────────────────────────────────────────────────┘
```

### 9.3 Database Design Overview

#### Core Tables

| Table | Description |
|-------|-------------|
| Users | System users and authentication |
| Roles | Role definitions |
| RolePermissions | Permission assignments per role |
| WorkPeriods | Work period records |
| Products | Product catalog |
| Categories | Product categories |
| Orders | Order headers |
| OrderItems | Order line items |
| Receipts | Receipt records |
| Payments | Payment transactions |
| PaymentMethods | Available payment methods |
| Inventory | Current stock levels |
| StockMovements | Stock movement history |
| Suppliers | Supplier information |
| PurchaseOrders | Purchase order headers |
| PurchaseOrderItems | Purchase order lines |
| GoodsReceived | Goods receiving records |
| AuditLog | System audit trail |
| SystemSettings | Configuration settings |

### 9.4 Hardware Requirements

#### Minimum Requirements

| Component | Specification |
|-----------|---------------|
| Processor | Intel Core i3 or equivalent |
| RAM | 4 GB |
| Storage | 50 GB available space |
| Display | 1024x768 resolution (touch-enabled recommended) |
| OS | Windows 10/11 |

#### Recommended Requirements

| Component | Specification |
|-----------|---------------|
| Processor | Intel Core i5 or equivalent |
| RAM | 8 GB |
| Storage | 100 GB SSD |
| Display | 1920x1080 touch screen |
| OS | Windows 11 |

#### Peripheral Support

| Device | Interface |
|--------|-----------|
| Receipt Printer | USB, Serial, Network (ESC/POS) |
| Cash Drawer | RJ11 (via printer) or USB |
| Barcode Scanner | USB HID |
| Kitchen Printer | Network (ESC/POS) |
| Customer Display | USB, Serial |

---

## 10. Security and Compliance

### 10.1 Authentication and Authorization

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| SEC-001 | Unique user credentials for each staff member | Must Have |
| SEC-002 | Password complexity requirements | Must Have |
| SEC-003 | Optional PIN-based login for quick access | Should Have |
| SEC-004 | Session timeout after inactivity | Must Have |
| SEC-005 | Role-based access control (RBAC) | Must Have |
| SEC-006 | Failed login attempt lockout | Should Have |
| SEC-007 | Password expiry policy (configurable) | Should Have |

### 10.2 Data Security

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| SEC-010 | Password hashing (bcrypt or similar) | Must Have |
| SEC-011 | Database connection encryption | Should Have |
| SEC-012 | Sensitive data encryption at rest | Should Have |
| SEC-013 | No plain-text storage of credentials | Must Have |
| SEC-014 | Secure backup and restore procedures | Must Have |

### 10.3 Audit Trail Requirements

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| AUD-001 | Log all user logins/logouts | Must Have |
| AUD-002 | Log all transactions (sales, voids, refunds) | Must Have |
| AUD-003 | Log all permission overrides | Must Have |
| AUD-004 | Log all inventory adjustments | Must Have |
| AUD-005 | Log all price changes | Must Have |
| AUD-006 | Log all user/role modifications | Must Have |
| AUD-007 | Immutable audit records (no deletion) | Must Have |
| AUD-008 | Audit log retention (minimum 7 years) | Should Have |
| AUD-009 | Audit log includes: timestamp, user, action, before/after values | Must Have |

### 10.4 Compliance Considerations

| Compliance Area | Requirements |
|-----------------|--------------|
| Tax Compliance | Accurate tax calculation and reporting |
| Financial Records | Complete transaction history retention |
| Receipt Requirements | All required legal information on receipts |
| Z-Report Sequencing | Sequential, tamper-evident Z-Report numbering |
| Data Protection | User data handling per local regulations |

---

## 11. Non-Functional Requirements

### 11.1 Performance

| Requirement | Target |
|-------------|--------|
| Application startup time | < 10 seconds |
| Product search response | < 500ms |
| Transaction processing | < 2 seconds |
| Report generation (daily) | < 30 seconds |
| Receipt printing | < 3 seconds |
| Concurrent users | Up to 10 per terminal |

### 11.2 Reliability

| Requirement | Target |
|-------------|--------|
| System uptime | 99.9% during business hours |
| Data backup | Automatic daily backup |
| Recovery time | < 30 minutes from backup |
| Transaction integrity | ACID compliance |
| Offline capability | Queue transactions if network fails |

### 11.3 Usability

| Requirement | Description |
|-------------|-------------|
| Training time | New waiter proficient within 2 hours |
| Error messages | Clear, actionable messages in plain language |
| Help system | Context-sensitive help available |
| Localization | Support for multiple languages |
| Accessibility | High contrast mode, adjustable font sizes |

### 11.4 Scalability

| Requirement | Description |
|-------------|-------------|
| Products | Support for 10,000+ products |
| Transactions | Handle 1,000+ transactions per day |
| History | 5+ years of transaction history |
| Multi-terminal | Support for 5+ POS terminals |
| Multi-outlet | Optional multi-location support |

### 11.5 Maintainability

| Requirement | Description |
|-------------|-------------|
| Updates | In-app update mechanism |
| Database migrations | Automated schema updates |
| Logging | Comprehensive error logging |
| Diagnostics | Built-in diagnostic tools |
| Documentation | Admin and user documentation |

---

## 12. Supermarket/Retail Mode Features

### 12.1 Mode Selection

During initial setup, the administrator selects the business mode:

| Setting | Options | Effect |
|---------|---------|--------|
| Business Mode | Restaurant / Supermarket / Hybrid | Determines default UI and enabled features |
| Feature Toggles | Individual ON/OFF | Fine-tune which features are active |

### 12.2 Supermarket POS Interface

The supermarket mode provides a streamlined checkout interface optimized for barcode scanning:

```
┌──────────────────────────────────────────────────────────────────────┐
│  🔍 [Barcode/Search - AUTO FOCUS]                    [F2: Manual]    │
├──────────────────────────────────────────────────────────────────────┤
│  SCANNED ITEMS                                                       │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ #  Product                    Qty     Price      Total         │  │
│  │ 1  Milk 1L (6001234567890)    x2     KSh 150    KSh 300       │  │
│  │ 2  Bread White Loaf           x1     KSh 60     KSh 60        │  │
│  │ 3  Eggs 12pk (OFFER!)         x1     KSh 350    KSh 299 ←     │  │
│  │    ~~~~ Was: KSh 350  Now: KSh 299 (Save 15%) ~~~~            │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                                                                      │
│  SUBTOTAL:     KSh 659          ┌────────────────────────────────┐  │
│  VAT (16%):    KSh 105          │ [CASH]  [CARD]  [M-PESA]      │  │
│  ────────────────────           │                                │  │
│  TOTAL:        KSh 764          │ [VOID ITEM] [VOID ALL]  [PAY] │  │
│                                 └────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────┘
```

### 12.3 Barcode Scanner Integration

| Requirement | Description |
|-------------|-------------|
| Auto-focus | Search field maintains focus after each scan |
| Multi-scan | Scanning same item increases quantity |
| Unknown barcode | Prompt to add product or enter manually |
| Keyboard wedge | Standard USB barcode scanner support |

### 12.4 Key Differences from Restaurant Mode

| Feature | Restaurant Mode | Supermarket Mode |
|---------|-----------------|------------------|
| Primary input | Touch product tiles | Barcode scanner |
| Order display | Three-panel layout | Single list view |
| Table management | Yes | No |
| Kitchen printing | Yes | No |
| Product offers | Optional | Standard |
| Supplier credit | Optional | Standard |

---

## 13. Product Offers & Promotions

### 13.1 Offer Types

| Offer Type | Description | Example |
|------------|-------------|---------|
| Fixed Price | Product sells at set offer price | "Special: KSh 299" |
| Percentage Discount | Percentage off regular price | "15% off" |
| Buy X Get Y | Purchase requirement for discount | "Buy 2, Get 1 Free" |
| Bundle Deal | Multiple products at package price | "Milk + Bread = KSh 180" |

### 13.2 Offer Configuration

| Field | Description | Required |
|-------|-------------|----------|
| Product | Product(s) included in offer | Yes |
| Offer Name | Display name for receipt | Yes |
| Offer Price | Fixed price or discount % | Yes |
| Start Date | When offer becomes active | Yes |
| End Date | When offer expires | Yes |
| Min Quantity | Minimum qty for offer to apply | No |
| Max Quantity | Maximum qty per transaction | No |

### 13.3 Offer Display

Products with active offers show:
- Original price (strikethrough)
- Offer price (highlighted)
- Savings amount or percentage
- "OFFER" badge on product tile

### 13.4 Requirements

| ID | Requirement |
|----|-------------|
| OF-001 | System automatically applies active offers at checkout |
| OF-002 | Offers can be date-range limited (start/end date) |
| OF-003 | Multiple offers cannot stack on same product |
| OF-004 | Receipt shows both original and offer price |
| OF-005 | Offer report shows redemption count and revenue impact |
| OF-006 | Expired offers automatically deactivate |

---

## 14. Supplier Credit Management

### 14.1 Overview

Suppliers often provide goods on credit with payment terms (e.g., Net 30). The system tracks what is owed to each supplier and when payments are due.

### 14.2 Supplier Configuration

| Field | Description |
|-------|-------------|
| Name | Supplier company name |
| Contact Person | Primary contact |
| Payment Terms | COD, Net 15, Net 30, Net 60 |
| Credit Limit | Maximum credit allowed |
| Current Balance | Amount currently owed |
| Tax ID | KRA PIN for tax purposes |

### 14.3 Purchase Order Flow

```
[Create PO] → [Send to Supplier] → [Receive Goods] → [Mark Payment Status]
                                          ↓
                                   [Paid] or [Credit]
                                          ↓
                              [Update Supplier Balance]
```

### 14.4 Accounts Payable

| Field | Description |
|-------|-------------|
| Invoice Number | Supplier's invoice reference |
| Invoice Date | Date on supplier invoice |
| Due Date | Calculated from payment terms |
| Amount | Total invoice amount |
| Status | Unpaid / Partially Paid / Paid / Overdue |

### 14.5 Requirements

| ID | Requirement |
|----|-------------|
| SC-001 | Track payment terms per supplier (COD, Net 30, etc.) |
| SC-002 | Mark deliveries as Paid or Credit at receiving |
| SC-003 | Calculate due dates from invoice date + payment terms |
| SC-004 | Alert for overdue invoices |
| SC-005 | Supplier statement report (all transactions) |
| SC-006 | Accounts payable aging report |
| SC-007 | Record partial payments against invoices |

---

## 15. Employee & Payroll Module

### 15.1 Overview

Manage employee records and process payroll for supermarket staff. This is a foundation module for basic payroll operations.

### 15.2 Employee Management

| Field | Description |
|-------|-------------|
| Employee Number | Unique identifier |
| Personal Details | Name, ID, contact info |
| Employment Type | Full-time, Part-time, Contract |
| Department | Sales, Warehouse, Admin, etc. |
| Basic Salary | Monthly/weekly base pay |
| Bank Details | For salary payment |
| Statutory IDs | KRA PIN, NSSF, NHIF numbers |

### 15.3 Salary Components

| Type | Examples |
|------|----------|
| Earnings | Basic Salary, House Allowance, Transport Allowance, Overtime |
| Deductions | PAYE Tax, NSSF, NHIF, Loan Repayment, Advances |

### 15.4 Payroll Processing

```
[Create Pay Period] → [Process Payroll] → [Review] → [Approve] → [Pay]
       ↓                    ↓                              ↓
 "December 2025"    Calculate earnings/        Generate payslips
                    deductions for all
                    employees
```

### 15.5 Requirements

| ID | Requirement |
|----|-------------|
| EP-001 | Maintain employee master records |
| EP-002 | Configure salary components (earnings/deductions) |
| EP-003 | Create monthly/weekly payroll periods |
| EP-004 | Auto-calculate statutory deductions (PAYE, NSSF, NHIF) |
| EP-005 | Generate individual payslips |
| EP-006 | Payroll summary report |
| EP-007 | Track payment status per employee |

---

## 16. Accounting Module

### 16.1 Overview

A semi-accounting system to track financial transactions, generate basic financial reports, and maintain a general ledger. This is not a full accounting system but provides essential financial visibility.

### 16.2 Chart of Accounts

Default account structure:

| Code | Account | Type |
|------|---------|------|
| 1000 | Cash | Asset |
| 1010 | Bank Account | Asset |
| 1100 | Accounts Receivable | Asset |
| 1200 | Inventory | Asset |
| 2000 | Accounts Payable | Liability |
| 2100 | Salaries Payable | Liability |
| 2200 | Tax Payable | Liability |
| 4000 | Sales Revenue | Revenue |
| 5000 | Cost of Goods Sold | Expense |
| 5100 | Salaries Expense | Expense |
| 5200 | Rent Expense | Expense |

### 16.3 Automatic Journal Entries

The system automatically creates journal entries for:

| Transaction | Debit | Credit |
|-------------|-------|--------|
| Sale (Cash) | Cash | Sales Revenue |
| Sale (Credit) | Accounts Receivable | Sales Revenue |
| Purchase | Inventory | Accounts Payable / Cash |
| Supplier Payment | Accounts Payable | Cash / Bank |
| Expense | Expense Account | Cash / Accounts Payable |
| Payroll | Salaries Expense | Cash / Bank |

### 16.4 Reports

| Report | Description |
|--------|-------------|
| Trial Balance | All account balances (debits = credits) |
| Income Statement | Revenue - Expenses = Net Income |
| Balance Sheet | Assets = Liabilities + Equity |
| General Ledger | Transaction history per account |
| Accounts Payable Aging | What's owed to suppliers by age |

### 16.5 Expense Tracking

| Field | Description |
|-------|-------------|
| Expense Category | Rent, Utilities, Supplies, etc. |
| Description | What the expense is for |
| Amount | Expense amount |
| Date | When incurred |
| Payment Method | Cash, Bank, Cheque |
| Receipt | Option to attach image |
| Approval Status | Pending / Approved / Rejected |

### 16.6 Requirements

| ID | Requirement |
|----|-------------|
| AC-001 | Maintain chart of accounts |
| AC-002 | Auto-post sales to revenue account |
| AC-003 | Auto-post purchases to inventory/payable |
| AC-004 | Auto-post payroll to expense/payable |
| AC-005 | Record and categorize expenses |
| AC-006 | Generate Trial Balance report |
| AC-007 | Generate Income Statement |
| AC-008 | Generate Balance Sheet |
| AC-009 | Accounting period management (open/close) |

---

## 17. Future Considerations

### 17.1 Potential Enhancements (Phase 2)

| Feature | Description |
|---------|-------------|
| Cloud Sync | Sync data to cloud for backup and analytics |
| Mobile Companion App | Manager app for real-time monitoring |
| Customer Loyalty | Points and rewards program |
| Kitchen Display System | Digital KDS integration |
| Online Ordering | Accept orders from web/mobile |
| Reservation System | Table booking and management |
| Customer Accounts | Credit accounts and prepaid balances |
| Advanced Analytics | AI-powered insights and predictions |
| Multi-Language Menu | Menu display in multiple languages |
| Weight Scale Integration | For items sold by weight |

### 17.2 Integration Possibilities

| Integration | Description |
|-------------|-------------|
| Accounting Software | Export to QuickBooks, Sage, etc. |
| Hotel PMS | Property Management System integration |
| Payment Gateways | Online payment processing |
| Delivery Platforms | Integration with food delivery apps |
| HR Systems | Employee time and attendance |
| M-Pesa API | Direct M-Pesa integration (Daraja API) |

---

## 18. Glossary

| Term | Definition |
|------|------------|
| **Work Period** | A defined operational period (shift or business day) during which transactions can be processed |
| **X Report** | A mid-period report showing current sales without resetting counters |
| **Z Report** | An end-of-period report that provides final totals and resets counters for the next period |
| **KOT** | Kitchen Order Ticket - printed order sent to kitchen |
| **Receipt** | Customer-facing document showing purchased items and payment |
| **Void** | Cancellation of a transaction, removing it from sales totals |
| **Settlement** | The process of receiving payment and closing a receipt |
| **Split Bill** | Dividing one receipt into multiple receipts for separate payment |
| **86'd** | Industry term for an item that is out of stock and unavailable |
| **POS** | Point of Sale - the system where sales transactions occur |
| **SKU** | Stock Keeping Unit - unique identifier for a product |
| **RBAC** | Role-Based Access Control - permission system based on user roles |
| **GRN** | Goods Received Note - document confirming receipt of purchased goods |
| **ESC/POS** | Epson Standard Code for POS printers - common thermal printer protocol |
| **Float** | Opening cash amount placed in the cash drawer at start of business |

---

## Document Approval

| Role | Name | Signature | Date |
|------|------|-----------|------|
| Product Owner | | | |
| Technical Lead | | | |
| Business Analyst | | | |
| Stakeholder | | | |

---

**Document History**

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | December 2025 | | Initial PRD creation |

---

*This document serves as the foundation for the development of the Hospitality POS System. All requirements are subject to review and may be updated based on stakeholder feedback and technical feasibility assessments.*
