# Feature Gap Analysis & Enhancement Proposals
## Multi-Mode POS System - Kenya/Africa Market Alignment

**Document Version:** 1.0
**Date:** January 16, 2026
**Based On:** Analysis of 50 POS systems in Kenya & Africa
**Purpose:** Source document for creating implementation stories

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Critical Gaps (Must Fix)](#2-critical-gaps-must-fix)
3. [High Priority Enhancements](#3-high-priority-enhancements)
4. [Medium Priority Enhancements](#4-medium-priority-enhancements)
5. [Low Priority Enhancements](#5-low-priority-enhancements)
6. [Feature Enhancement Details](#6-feature-enhancement-details)
7. [Implementation Phases](#7-implementation-phases)
8. [Appendix: Competitive Reference](#appendix-competitive-reference)

---

## 1. Executive Summary

### 1.1 Current State Assessment

| Mode | Readiness | Status |
|------|-----------|--------|
| Hotel/Restaurant | 85% | Ready with minor gaps |
| Supermarket/Retail | 52% | Significant gaps exist |
| **Overall** | **58%** | **Not market-ready for Kenya** |

### 1.2 Gap Categories

| Category | Gaps Found | Critical | High | Medium | Low |
|----------|------------|----------|------|--------|-----|
| Tax Compliance | 6 | 6 | 0 | 0 | 0 |
| Payment Integration | 5 | 3 | 2 | 0 | 0 |
| Inventory Management | 8 | 3 | 3 | 2 | 0 |
| Customer & Loyalty | 8 | 2 | 4 | 2 | 0 |
| Multi-Store Operations | 7 | 0 | 5 | 2 | 0 |
| Offline & Sync | 5 | 2 | 2 | 1 | 0 |
| Reporting & Analytics | 5 | 0 | 3 | 2 | 0 |
| Hospitality Features | 4 | 0 | 2 | 2 | 0 |
| Hardware Integration | 3 | 1 | 2 | 0 | 0 |
| Employee & HR | 4 | 0 | 0 | 3 | 1 |
| **TOTAL** | **55** | **17** | **23** | **14** | **1** |

### 1.3 Business Impact

**Without addressing critical gaps:**
- Cannot legally operate in Kenya (eTIMS requirement)
- Cannot compete with SimbaPOS, Uzalynx, POSmart
- Cannot serve supermarket chains (no multi-branch)
- Cannot serve food retailers (no expiry tracking)
- Will lose customers to systems with M-Pesa auto-confirmation

---

## 2. Critical Gaps (Must Fix)

> **Definition:** These gaps prevent legal operation or make the system fundamentally uncompetitive. Must be addressed before market launch.

---

### 2.1 KRA eTIMS Integration

**Gap ID:** GAP-CRIT-001
**Category:** Tax Compliance
**Severity:** CRITICAL - LEGAL REQUIREMENT
**Current State:** Not implemented (0%)
**Market Expectation:** 100% of legal POS systems in Kenya must have this

#### Problem Statement

The Kenya Revenue Authority (KRA) mandated that ALL businesses must use the Electronic Tax Invoice Management System (eTIMS) for invoicing since 2024. Our system has no eTIMS integration, making it **illegal to use in Kenya**.

#### Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| ETIMS-001 | System must connect to KRA eTIMS API (OSCU or VSCU) | Must Have |
| ETIMS-002 | Generate eTIMS-compliant electronic tax invoices | Must Have |
| ETIMS-003 | Transmit invoice data to KRA in real-time | Must Have |
| ETIMS-004 | Receive and store KRA invoice validation response | Must Have |
| ETIMS-005 | Print eTIMS QR code on all receipts | Must Have |
| ETIMS-006 | Support offline invoicing with VSCU (batch upload) | Must Have |
| ETIMS-007 | Handle eTIMS errors gracefully (retry, queue) | Must Have |
| ETIMS-008 | Store eTIMS Control Unit ID and Device Serial | Must Have |
| ETIMS-009 | Generate eTIMS-compliant credit notes for refunds | Must Have |
| ETIMS-010 | Support customer KRA PIN capture (optional for B2B) | Should Have |

#### Technical Approach

```
Integration Options:
1. OSCU (Online Sales Control Unit) - For always-online systems
2. VSCU (Virtual Sales Control Unit) - For bulk/offline invoicing

Recommended: Implement BOTH
- OSCU as primary for real-time transactions
- VSCU as fallback for offline periods

API Endpoints Required:
- Invoice submission
- Invoice validation
- Credit note submission
- Device registration
- Status query
```

#### Acceptance Criteria

- [ ] System registers with KRA as eTIMS-compliant device
- [ ] Every sale generates a valid eTIMS invoice
- [ ] Receipt prints with valid eTIMS QR code
- [ ] System handles KRA API downtime gracefully
- [ ] Offline transactions queue and sync when online
- [ ] Credit notes properly linked to original invoices

#### Competitive Reference

| System | eTIMS Status |
|--------|--------------|
| SimbaPOS | Certified integrator |
| Uzalynx | Certified integrator |
| POSmart | Certified integrator |
| DigitalPOS | Certified integrator |
| **Our System** | **NOT CERTIFIED** |

---

### 2.2 M-Pesa Daraja API Integration

**Gap ID:** GAP-CRIT-002
**Category:** Payment Integration
**Severity:** CRITICAL - COMPETITIVE DISADVANTAGE
**Current State:** Manual reference entry only
**Market Expectation:** Automatic STK Push and confirmation

#### Problem Statement

Current implementation requires cashiers to manually enter M-Pesa transaction codes. This is:
- Slow (adds 30-60 seconds per transaction)
- Error-prone (typos in reference codes)
- Unverifiable (cannot confirm payment actually received)
- Uncompetitive (all major POS systems have auto-confirmation)

#### Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| MPESA-001 | Integrate with Safaricom Daraja API | Must Have |
| MPESA-002 | Implement STK Push (Lipa Na M-Pesa Online) | Must Have |
| MPESA-003 | Send payment request to customer phone automatically | Must Have |
| MPESA-004 | Receive real-time payment confirmation callback | Must Have |
| MPESA-005 | Auto-match payment to receipt by amount and reference | Must Have |
| MPESA-006 | Display payment status in real-time on POS | Must Have |
| MPESA-007 | Handle payment timeout gracefully | Must Have |
| MPESA-008 | Support M-Pesa Till Number configuration | Must Have |
| MPESA-009 | Support M-Pesa Paybill configuration | Should Have |
| MPESA-010 | Generate M-Pesa payment reports | Must Have |
| MPESA-011 | Support payment reversal requests | Should Have |
| MPESA-012 | Store M-Pesa API credentials securely | Must Have |

#### Technical Approach

```
Daraja API Integration:
1. Register app on Safaricom Developer Portal
2. Obtain API credentials (Consumer Key, Consumer Secret)
3. Implement OAuth token generation
4. Implement STK Push endpoint
5. Set up callback URL for payment notifications
6. Implement C2B validation and confirmation URLs

Flow:
[Cashier selects M-Pesa] → [Enter customer phone] → [STK Push sent]
     ↓
[Customer sees prompt on phone] → [Enters PIN] → [Payment processed]
     ↓
[Callback received] → [Receipt auto-settled] → [Print receipt]
```

#### Acceptance Criteria

- [ ] Cashier can initiate M-Pesa payment with customer phone number
- [ ] Customer receives STK Push prompt within 5 seconds
- [ ] Payment confirmation received within 30 seconds of customer approval
- [ ] Receipt automatically settles on successful payment
- [ ] Failed/cancelled payments show clear error message
- [ ] Transaction reference stored with payment record

#### Competitive Reference

| System | M-Pesa Integration |
|--------|-------------------|
| SimbaPOS | Full Daraja + Auto-confirm |
| Uzalynx | Full Daraja + Auto-confirm |
| FortyPOS | Full Daraja + Auto-confirm |
| Fuatra | STK Push + Auto-confirm |
| **Our System** | **Manual entry only** |

---

### 2.3 Batch and Expiry Date Tracking

**Gap ID:** GAP-CRIT-003
**Category:** Inventory Management
**Severity:** CRITICAL - FOOD SAFETY & LEGAL
**Current State:** "Could Have" in PRD (not prioritized)
**Market Expectation:** Standard feature for supermarkets

#### Problem Statement

Supermarkets selling food products MUST track:
- Batch/lot numbers for traceability (recalls)
- Expiry dates to prevent sale of expired goods
- FIFO/FEFO to ensure oldest stock sells first

Without this, the system:
- Cannot serve food retailers legally
- Creates food safety liability
- Causes inventory waste (expired goods)
- Fails health inspections

#### Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| BATCH-001 | Add BatchNumber field to inventory tracking | Must Have |
| BATCH-002 | Add ExpiryDate field to inventory tracking | Must Have |
| BATCH-003 | Record batch/expiry at goods receiving (GRN) | Must Have |
| BATCH-004 | Support multiple batches of same product | Must Have |
| BATCH-005 | Implement FEFO (First Expiry First Out) deduction | Must Have |
| BATCH-006 | Create Expiry Alert Dashboard | Must Have |
| BATCH-007 | Configure expiry warning days per product/category | Must Have |
| BATCH-008 | Alert when products approaching expiry | Must Have |
| BATCH-009 | Block sale of expired products at POS | Must Have |
| BATCH-010 | Manager override for expired product sale (with reason) | Should Have |
| BATCH-011 | Batch traceability report (which customer bought which batch) | Must Have |
| BATCH-012 | Expiry waste report (expired items, value lost) | Must Have |
| BATCH-013 | Support batch recall workflow | Should Have |

#### Database Changes

```sql
-- New table for batch tracking
CREATE TABLE ProductBatches (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProductId INT FOREIGN KEY REFERENCES Products(Id),
    BatchNumber NVARCHAR(50) NOT NULL,
    ExpiryDate DATE,
    ManufactureDate DATE,
    ReceivedDate DATE NOT NULL,
    ReceivedQuantity DECIMAL(18,3) NOT NULL,
    CurrentQuantity DECIMAL(18,3) NOT NULL,
    CostPrice DECIMAL(18,2),
    SupplierId INT FOREIGN KEY REFERENCES Suppliers(Id),
    GoodsReceivedId INT FOREIGN KEY REFERENCES GoodsReceived(Id),
    Status NVARCHAR(20) DEFAULT 'Active', -- Active, Depleted, Expired, Recalled
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

CREATE INDEX IX_ProductBatches_Expiry ON ProductBatches (ProductId, ExpiryDate)
    WHERE Status = 'Active';

-- Modify stock deduction to use FEFO
-- Sale should deduct from batch with earliest ExpiryDate first
```

#### Acceptance Criteria

- [ ] Goods receiving requires batch number and expiry date for perishables
- [ ] System tracks stock by individual batch
- [ ] Sales deduct from earliest-expiring batch first (FEFO)
- [ ] Dashboard shows products expiring in next 7/14/30 days
- [ ] POS blocks sale of expired products with clear message
- [ ] Manager can override with PIN and reason logged
- [ ] Expiry waste report shows expired quantities and value

---

### 2.4 Expiry Alert Dashboard and Blocking

**Gap ID:** GAP-CRIT-004
**Category:** Inventory Management
**Severity:** CRITICAL - FOOD SAFETY
**Current State:** Not implemented
**Market Expectation:** Standard in DigitalPOS, Smartwas, Uzalynx

#### Problem Statement

Even with batch/expiry data stored, the system needs active monitoring and enforcement:
- Proactive alerts before products expire
- Blocking mechanism at point of sale
- Reporting for waste management

#### Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| EXPIRY-001 | Dashboard widget showing products expiring soon | Must Have |
| EXPIRY-002 | Configurable alert thresholds (7, 14, 30 days) | Must Have |
| EXPIRY-003 | Color-coded expiry status (green/yellow/red) | Must Have |
| EXPIRY-004 | Email/SMS alerts for critical expiry items | Should Have |
| EXPIRY-005 | POS warning when scanning soon-to-expire item | Must Have |
| EXPIRY-006 | POS hard block when scanning expired item | Must Have |
| EXPIRY-007 | Manager override with PIN and reason capture | Must Have |
| EXPIRY-008 | Auto-mark batches as "Expired" when date passes | Must Have |
| EXPIRY-009 | Expired items report with write-off value | Must Have |
| EXPIRY-010 | Option to auto-apply discount for near-expiry items | Should Have |

#### Acceptance Criteria

- [ ] Dashboard shows expiry alerts grouped by urgency
- [ ] Clicking alert shows affected products and quantities
- [ ] POS displays warning icon for items expiring within threshold
- [ ] Expired items cannot be sold without manager override
- [ ] All override events logged with user, reason, timestamp
- [ ] Daily/weekly expiry report available

---

### 2.5 Customer Loyalty Program - Points System

**Gap ID:** GAP-CRIT-005
**Category:** Customer & Loyalty
**Severity:** CRITICAL - MARKET EXPECTATION
**Current State:** Not implemented (stories exist but not prioritized)
**Market Expectation:** 68% of Kenyan consumers belong to loyalty programs

#### Problem Statement

Research shows 68% of Kenyans belong to at least one loyalty program. Major supermarkets (Naivas, Carrefour, Quickmart) all have loyalty programs. Without this:
- Cannot compete for loyal customers
- No customer retention mechanism
- No customer purchase data for marketing
- Losing to competitors with loyalty

#### Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| LOYAL-001 | Customer enrollment at POS (phone number as ID) | Must Have |
| LOYAL-002 | Customer profile storage (name, phone, email) | Must Have |
| LOYAL-003 | Points earning on purchases (configurable rate) | Must Have |
| LOYAL-004 | Points balance display at checkout | Must Have |
| LOYAL-005 | Points redemption at POS (partial or full) | Must Have |
| LOYAL-006 | Configurable points-to-currency conversion | Must Have |
| LOYAL-007 | Membership tier system (Bronze/Silver/Gold) | Should Have |
| LOYAL-008 | Tier-based earning multipliers | Should Have |
| LOYAL-009 | Customer purchase history view | Must Have |
| LOYAL-010 | Points expiry rules (configurable) | Should Have |
| LOYAL-011 | Points statement/report per customer | Must Have |
| LOYAL-012 | Bulk SMS to loyalty members | Should Have |
| LOYAL-013 | Birthday/anniversary bonus points | Could Have |

#### Business Rules

```
Default Configuration:
- Earning Rate: 1 point per KSh 100 spent
- Redemption Rate: 100 points = KSh 10 discount
- Points Expiry: 12 months from earning
- Minimum Redemption: 100 points

Tier Structure (Configurable):
- Bronze: 0-999 lifetime points (1x earning)
- Silver: 1000-4999 lifetime points (1.5x earning)
- Gold: 5000+ lifetime points (2x earning)
```

#### Acceptance Criteria

- [ ] Customer can enroll with phone number at checkout
- [ ] Points earned automatically on each purchase
- [ ] Points balance shown before payment
- [ ] Customer can redeem points (cashier confirms)
- [ ] Points transaction history available
- [ ] Tier upgrades happen automatically
- [ ] Reports show loyalty program performance

---

### 2.6 Offline Operation with Sync

**Gap ID:** GAP-CRIT-006
**Category:** Reliability
**Severity:** CRITICAL - OPERATIONAL CONTINUITY
**Current State:** "Queue transactions if network fails" (vague)
**Market Expectation:** Full offline operation with automatic sync

#### Problem Statement

Kenya experiences:
- Frequent power outages (load shedding in some areas)
- Unreliable internet connectivity
- Network downtime during peak hours

The system must operate fully offline and sync when connectivity returns. Current architecture relies on SQL Server Express (local), but:
- No defined offline queue mechanism
- No conflict resolution strategy
- No sync status visibility

#### Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| OFFLINE-001 | Full POS operation when offline (sales, payments, receipts) | Must Have |
| OFFLINE-002 | Local queue for pending eTIMS submissions | Must Have |
| OFFLINE-003 | Local queue for pending M-Pesa verifications | Must Have |
| OFFLINE-004 | Visual indicator of online/offline status | Must Have |
| OFFLINE-005 | Automatic sync when connectivity restored | Must Have |
| OFFLINE-006 | Conflict resolution for concurrent edits | Must Have |
| OFFLINE-007 | Sync status dashboard (pending items count) | Must Have |
| OFFLINE-008 | Manual sync trigger option | Should Have |
| OFFLINE-009 | Sync error notifications and retry mechanism | Must Have |
| OFFLINE-010 | Offline receipt numbering (prevent duplicates) | Must Have |

#### Technical Approach

```
Offline Architecture:
1. SQL Server Express runs locally (already in place)
2. Add SyncQueue table for pending cloud operations
3. Add SyncStatus table for tracking sync state
4. Background service monitors connectivity
5. On reconnect: process queue in order (FIFO)
6. Conflict resolution: Last-write-wins with audit trail

Queue Types:
- eTIMS invoices
- M-Pesa payment verifications
- Cloud backup data (if future cloud sync added)
- Multi-branch sync (if applicable)
```

#### Acceptance Criteria

- [ ] POS continues all operations during internet outage
- [ ] Clear visual indicator shows offline status
- [ ] Queued items count displayed on dashboard
- [ ] Automatic sync begins within 30 seconds of reconnection
- [ ] All queued eTIMS invoices submitted successfully
- [ ] Conflicts logged and flagged for review
- [ ] No duplicate receipt numbers across offline periods

---

## 3. High Priority Enhancements

> **Definition:** These features are expected by the market and significantly impact competitiveness. Should be implemented within 3 months of launch.

---

### 3.1 Multi-Branch Stock Transfer

**Gap ID:** GAP-HIGH-001
**Category:** Multi-Store Operations
**Current State:** Not implemented
**Impact:** Cannot serve chain supermarkets

#### Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| TRANSFER-001 | Create stock transfer request from any branch | Must Have |
| TRANSFER-002 | Specify source branch, destination branch, products, quantities | Must Have |
| TRANSFER-003 | Transfer approval workflow (requestor → approver) | Must Have |
| TRANSFER-004 | Mark transfer as shipped (deduct from source) | Must Have |
| TRANSFER-005 | Mark transfer as received (add to destination) | Must Have |
| TRANSFER-006 | Partial receiving support | Should Have |
| TRANSFER-007 | Transfer variance tracking (sent vs received) | Must Have |
| TRANSFER-008 | Transfer history and audit trail | Must Have |
| TRANSFER-009 | In-transit inventory visibility | Should Have |
| TRANSFER-010 | Transfer reports by branch, period | Must Have |

#### Workflow

```
[Branch A: Request Transfer]
    ↓
[HQ/Manager: Approve Transfer]
    ↓
[Branch A: Ship Transfer] → Stock deducted, status = "In Transit"
    ↓
[Branch B: Receive Transfer] → Stock added, status = "Complete"
    ↓
[System: Calculate Variance] → Flag if received ≠ shipped
```

---

### 3.2 Central Product and Price Management

**Gap ID:** GAP-HIGH-002
**Category:** Multi-Store Operations
**Current State:** Not implemented
**Impact:** Chain management impossible

#### Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| CENTRAL-001 | Designate one location as "Head Office" | Must Have |
| CENTRAL-002 | Create/edit products at HQ level | Must Have |
| CENTRAL-003 | Push product catalog to all branches | Must Have |
| CENTRAL-004 | Central price management | Must Have |
| CENTRAL-005 | Location-specific price overrides | Should Have |
| CENTRAL-006 | Central promotion deployment | Must Have |
| CENTRAL-007 | Sync status per branch | Must Have |
| CENTRAL-008 | Selective sync (choose what to push) | Should Have |

---

### 3.3 Real-Time Sales Dashboard

**Gap ID:** GAP-HIGH-003
**Category:** Reporting & Analytics
**Current State:** Not implemented
**Impact:** Managers lack real-time visibility

#### Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| DASH-001 | Live sales counter (today's total, updating) | Must Have |
| DASH-002 | Transaction count widget | Must Have |
| DASH-003 | Average transaction value widget | Must Have |
| DASH-004 | Sales by hour chart (live) | Must Have |
| DASH-005 | Top selling products (today) | Should Have |
| DASH-006 | Low stock alerts widget | Must Have |
| DASH-007 | Expiry alerts widget | Must Have |
| DASH-008 | Payment method breakdown (today) | Should Have |
| DASH-009 | Comparison to yesterday/last week | Should Have |
| DASH-010 | Auto-refresh every 30 seconds | Must Have |

---

### 3.4 Weight Scale Integration

**Gap ID:** GAP-HIGH-004
**Category:** Hardware Integration
**Current State:** PRD mentions as "Future"
**Impact:** Cannot sell produce by weight

#### Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| SCALE-001 | USB weight scale integration | Must Have |
| SCALE-002 | Read weight from scale on demand | Must Have |
| SCALE-003 | Calculate price based on weight × price-per-kg | Must Have |
| SCALE-004 | Support tare weight (container deduction) | Must Have |
| SCALE-005 | Print weight on receipt line item | Must Have |
| SCALE-006 | Configure products as "sold by weight" | Must Have |
| SCALE-007 | Support common scale protocols (RS-232, USB HID) | Should Have |

---

### 3.5 QR Code Payment Support

**Gap ID:** GAP-HIGH-005
**Category:** Payment Integration
**Current State:** Not implemented
**Impact:** Missing growing payment method

#### Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| QRPAY-001 | Generate QR code for payment amount | Must Have |
| QRPAY-002 | Display QR on customer-facing screen or printout | Must Have |
| QRPAY-003 | Support M-Pesa QR (Lipa Na M-Pesa) | Must Have |
| QRPAY-004 | Support bank QR codes (PesaLink, etc.) | Should Have |
| QRPAY-005 | Auto-detect payment completion | Must Have |
| QRPAY-006 | Timeout and retry for QR payments | Should Have |

---

### 3.6 Customer Purchase History

**Gap ID:** GAP-HIGH-006
**Category:** Customer & Loyalty
**Current State:** Not implemented
**Impact:** No customer insights

#### Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| HISTORY-001 | Link receipts to customer profile | Must Have |
| HISTORY-002 | View all purchases by customer | Must Have |
| HISTORY-003 | Purchase frequency analysis | Should Have |
| HISTORY-004 | Favorite products per customer | Should Have |
| HISTORY-005 | Total lifetime spend per customer | Must Have |
| HISTORY-006 | Last visit date tracking | Must Have |
| HISTORY-007 | Customer search at POS | Must Have |

---

### 3.7 Consolidated Chain Reporting

**Gap ID:** GAP-HIGH-007
**Category:** Multi-Store Operations
**Current State:** Not implemented
**Impact:** No chain-wide visibility

#### Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| CHAINRPT-001 | Sales report across all branches | Must Have |
| CHAINRPT-002 | Inventory report across all branches | Must Have |
| CHAINRPT-003 | Branch comparison report | Must Have |
| CHAINRPT-004 | Filter reports by branch or all | Must Have |
| CHAINRPT-005 | Export consolidated data to Excel | Should Have |

---

### 3.8 Airtel Money Integration

**Gap ID:** GAP-HIGH-008
**Category:** Payment Integration
**Current State:** Mentioned but not detailed
**Impact:** Missing second-largest mobile money

#### Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| AIRTEL-001 | Integrate with Airtel Money API | Must Have |
| AIRTEL-002 | Implement payment push (similar to M-Pesa STK) | Must Have |
| AIRTEL-003 | Real-time payment confirmation | Must Have |
| AIRTEL-004 | Auto-match payment to receipt | Must Have |
| AIRTEL-005 | Airtel Money transaction reports | Must Have |

---

### 3.9 Mobile Reporting App

**Gap ID:** GAP-HIGH-009
**Category:** Reporting & Analytics
**Current State:** Not implemented
**Impact:** Managers cannot monitor remotely

#### Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| MOBILE-001 | Mobile app for iOS and Android | Should Have |
| MOBILE-002 | View real-time sales summary | Must Have |
| MOBILE-003 | View stock alerts | Must Have |
| MOBILE-004 | View expiry alerts | Must Have |
| MOBILE-005 | Push notifications for critical events | Should Have |
| MOBILE-006 | Secure authentication | Must Have |

---

### 3.10 Automated Email Reports

**Gap ID:** GAP-HIGH-010
**Category:** Reporting & Analytics
**Current State:** Not implemented
**Impact:** Manual report distribution

#### Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| EMAIL-001 | Configure email recipients (owner, manager) | Must Have |
| EMAIL-002 | Daily sales summary email | Must Have |
| EMAIL-003 | Weekly performance report email | Should Have |
| EMAIL-004 | Low stock alert emails | Should Have |
| EMAIL-005 | Expiry alert emails | Should Have |
| EMAIL-006 | Customizable email schedule | Should Have |

---

## 4. Medium Priority Enhancements

> **Definition:** Features that improve competitiveness and user experience. Plan for 3-6 months post-launch.

---

### 4.1 QR Menu and Contactless Ordering

**Gap ID:** GAP-MED-001
**Category:** Hospitality Features
**Current State:** Not implemented
**Impact:** Missing post-COVID feature

#### Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| QRMENU-001 | Generate QR code linking to digital menu | Should Have |
| QRMENU-002 | Web-based menu display (no app needed) | Should Have |
| QRMENU-003 | Customer can browse menu and select items | Should Have |
| QRMENU-004 | Customer submits order via web interface | Should Have |
| QRMENU-005 | Order appears on POS/KDS for processing | Should Have |
| QRMENU-006 | Table number association via QR | Should Have |

---

### 4.2 Reservation System

**Gap ID:** GAP-MED-002
**Category:** Hospitality Features
**Current State:** PRD lists as "Future"
**Impact:** Manual reservation management

#### Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| RESERVE-001 | Create reservation (date, time, party size, name, phone) | Should Have |
| RESERVE-002 | View reservations calendar | Should Have |
| RESERVE-003 | Table assignment for reservations | Should Have |
| RESERVE-004 | Reservation status (confirmed, seated, no-show) | Should Have |
| RESERVE-005 | SMS reminder to customer | Could Have |

---

### 4.3 Time and Attendance

**Gap ID:** GAP-MED-003
**Category:** Employee & HR
**Current State:** Not implemented
**Impact:** Manual attendance tracking

#### Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| ATTEND-001 | Employee clock-in at POS | Should Have |
| ATTEND-002 | Employee clock-out at POS | Should Have |
| ATTEND-003 | PIN-based attendance authentication | Should Have |
| ATTEND-004 | Attendance report by employee/date | Should Have |
| ATTEND-005 | Late arrival flagging | Could Have |
| ATTEND-006 | Integration with payroll for hours worked | Should Have |

---

### 4.4 Shift Scheduling

**Gap ID:** GAP-MED-004
**Category:** Employee & HR
**Current State:** Not implemented
**Impact:** External scheduling tools needed

#### Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| SHIFT-001 | Create shift schedules (date, time, employee) | Should Have |
| SHIFT-002 | View weekly/monthly schedule grid | Should Have |
| SHIFT-003 | Assign employees to shifts | Should Have |
| SHIFT-004 | Shift swap requests | Could Have |
| SHIFT-005 | Schedule conflict detection | Should Have |

---

### 4.5 Happy Hour / Time-Based Pricing

**Gap ID:** GAP-MED-005
**Category:** Promotions & Pricing
**Current State:** Not implemented
**Impact:** Manual price changes for happy hour

#### Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| HAPPY-001 | Configure time-based pricing rules | Should Have |
| HAPPY-002 | Specify start time, end time, days of week | Should Have |
| HAPPY-003 | Apply discount or special price during window | Should Have |
| HAPPY-004 | Auto-activate and deactivate pricing | Should Have |
| HAPPY-005 | Display "Happy Hour" indicator on POS | Should Have |

---

### 4.6 Waste and Shrinkage Tracking

**Gap ID:** GAP-MED-006
**Category:** Inventory Management
**Current State:** Not implemented
**Impact:** No visibility into inventory loss

#### Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| WASTE-001 | Record waste events (damaged, expired, stolen) | Should Have |
| WASTE-002 | Waste reason categories | Should Have |
| WASTE-003 | Waste by product report | Should Have |
| WASTE-004 | Waste value calculation | Should Have |
| WASTE-005 | Shrinkage percentage tracking | Should Have |

---

### 4.7 Comparative Analytics

**Gap ID:** GAP-MED-007
**Category:** Reporting & Analytics
**Current State:** Not implemented
**Impact:** No trend analysis

#### Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| COMPARE-001 | This week vs last week comparison | Should Have |
| COMPARE-002 | This month vs last month comparison | Should Have |
| COMPARE-003 | Year-over-year comparison | Should Have |
| COMPARE-004 | Growth percentage calculations | Should Have |
| COMPARE-005 | Visual trend charts | Should Have |

---

### 4.8 Bank Reconciliation

**Gap ID:** GAP-MED-008
**Category:** Accounting & Finance
**Current State:** Not implemented
**Impact:** Manual bank matching

#### Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| BANK-001 | Import bank statement (CSV/OFX) | Should Have |
| BANK-002 | Match bank transactions to POS payments | Should Have |
| BANK-003 | Flag unmatched transactions | Should Have |
| BANK-004 | Reconciliation report | Should Have |
| BANK-005 | Mark transactions as reconciled | Should Have |

---

### 4.9 SMS Marketing to Customers

**Gap ID:** GAP-MED-009
**Category:** Customer & Loyalty
**Current State:** Not implemented
**Impact:** No direct marketing channel

#### Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| SMS-001 | Integrate with SMS gateway (Africa's Talking, etc.) | Should Have |
| SMS-002 | Send bulk SMS to loyalty members | Should Have |
| SMS-003 | SMS templates for promotions | Should Have |
| SMS-004 | Opt-out management | Must Have |
| SMS-005 | SMS delivery reports | Should Have |

---

### 4.10 Profit Margin Reports

**Gap ID:** GAP-MED-010
**Category:** Reporting & Analytics
**Current State:** Have cost price, no margin analysis
**Impact:** No profitability visibility

#### Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| MARGIN-001 | Calculate margin per product (sell - cost) | Should Have |
| MARGIN-002 | Margin percentage display | Should Have |
| MARGIN-003 | Margin report by product | Should Have |
| MARGIN-004 | Margin report by category | Should Have |
| MARGIN-005 | Low margin alerts | Should Have |
| MARGIN-006 | Margin trend analysis | Could Have |

---

## 5. Low Priority Enhancements

> **Definition:** Nice-to-have features that enhance user experience. Plan for 6+ months post-launch.

---

### 5.1 Commission Calculation

**Gap ID:** GAP-LOW-001
**Category:** Employee & HR
**Current State:** Not implemented
**Impact:** Manual commission tracking

#### Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| COMM-001 | Configure commission rules per role | Could Have |
| COMM-002 | Calculate commission based on sales | Could Have |
| COMM-003 | Commission report by employee | Could Have |
| COMM-004 | Add commission to payroll | Could Have |

---

### 5.2 Leave Management

**Gap ID:** GAP-LOW-002
**Category:** Employee & HR
**Current State:** Not implemented
**Impact:** External leave tracking needed

#### Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| LEAVE-001 | Configure leave types (annual, sick, etc.) | Could Have |
| LEAVE-002 | Leave request workflow | Could Have |
| LEAVE-003 | Leave balance tracking | Could Have |
| LEAVE-004 | Leave calendar view | Could Have |

---

### 5.3 Multi-Currency Support

**Gap ID:** GAP-LOW-003
**Category:** Payment Integration
**Current State:** KES only
**Impact:** Border areas and tourist businesses

#### Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| CURR-001 | Configure additional currencies | Could Have |
| CURR-002 | Set exchange rates | Could Have |
| CURR-003 | Accept payment in foreign currency | Could Have |
| CURR-004 | Calculate change in local currency | Could Have |

---

### 5.4 Customer Display Integration

**Gap ID:** GAP-LOW-004
**Category:** Hardware Integration
**Current State:** Mentioned in PRD peripherals
**Impact:** Customer cannot see items being rung up

#### Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| DISPLAY-001 | Send item and price to customer display | Could Have |
| DISPLAY-002 | Show running total | Could Have |
| DISPLAY-003 | Show payment amount and change | Could Have |
| DISPLAY-004 | Support common display protocols | Could Have |

---

## 6. Feature Enhancement Details

### 6.1 Database Schema Additions

```sql
-- =====================================================
-- ETIMS INTEGRATION TABLES
-- =====================================================

CREATE TABLE EtimsConfiguration (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ControlUnitId NVARCHAR(50) NOT NULL,
    DeviceSerialNumber NVARCHAR(50) NOT NULL,
    BranchId NVARCHAR(20),
    TaxPayerPIN NVARCHAR(20) NOT NULL,
    IntegrationType NVARCHAR(10) NOT NULL, -- OSCU, VSCU
    ApiEndpoint NVARCHAR(500) NOT NULL,
    LastSyncAt DATETIME2,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

CREATE TABLE EtimsInvoices (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ReceiptId INT FOREIGN KEY REFERENCES Receipts(Id),
    InvoiceNumber NVARCHAR(50), -- KRA assigned
    ControlUnitInvoiceNumber NVARCHAR(50), -- Local sequence
    QrCode NVARCHAR(MAX),
    SubmissionStatus NVARCHAR(20) DEFAULT 'Pending', -- Pending, Submitted, Validated, Failed
    KraResponse NVARCHAR(MAX), -- JSON response from KRA
    SubmittedAt DATETIME2,
    ValidatedAt DATETIME2,
    RetryCount INT DEFAULT 0,
    LastError NVARCHAR(500),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

CREATE TABLE EtimsCreditNotes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OriginalEtimsInvoiceId INT FOREIGN KEY REFERENCES EtimsInvoices(Id),
    CreditNoteNumber NVARCHAR(50),
    Reason NVARCHAR(200),
    Amount DECIMAL(18,2),
    SubmissionStatus NVARCHAR(20) DEFAULT 'Pending',
    KraResponse NVARCHAR(MAX),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- =====================================================
-- M-PESA INTEGRATION TABLES
-- =====================================================

CREATE TABLE MpesaConfiguration (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ShortCode NVARCHAR(20) NOT NULL, -- Till or Paybill
    ShortCodeType NVARCHAR(10) NOT NULL, -- Till, Paybill
    ConsumerKey NVARCHAR(100) NOT NULL,
    ConsumerSecret NVARCHAR(100) NOT NULL,
    PassKey NVARCHAR(200), -- For STK Push
    CallbackUrl NVARCHAR(500) NOT NULL,
    Environment NVARCHAR(20) DEFAULT 'sandbox', -- sandbox, production
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

CREATE TABLE MpesaTransactions (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PaymentId INT FOREIGN KEY REFERENCES Payments(Id),
    CheckoutRequestId NVARCHAR(100), -- From STK Push initiation
    MerchantRequestId NVARCHAR(100),
    PhoneNumber NVARCHAR(20) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    TransactionStatus NVARCHAR(20) DEFAULT 'Initiated', -- Initiated, Pending, Completed, Failed, Cancelled
    MpesaReceiptNumber NVARCHAR(50), -- The actual M-Pesa code
    TransactionDate DATETIME2,
    ResultCode INT,
    ResultDesc NVARCHAR(200),
    InitiatedAt DATETIME2 DEFAULT GETUTCDATE(),
    CompletedAt DATETIME2
);

-- =====================================================
-- BATCH/EXPIRY TRACKING TABLES
-- =====================================================

CREATE TABLE ProductBatches (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProductId INT FOREIGN KEY REFERENCES Products(Id),
    BatchNumber NVARCHAR(50) NOT NULL,
    ExpiryDate DATE,
    ManufactureDate DATE,
    ReceivedDate DATE NOT NULL DEFAULT GETDATE(),
    ReceivedQuantity DECIMAL(18,3) NOT NULL,
    CurrentQuantity DECIMAL(18,3) NOT NULL,
    UnitCost DECIMAL(18,2),
    SupplierId INT FOREIGN KEY REFERENCES Suppliers(Id),
    GoodsReceivedId INT FOREIGN KEY REFERENCES GoodsReceived(Id),
    Status NVARCHAR(20) DEFAULT 'Active', -- Active, Depleted, Expired, Recalled
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_ProductBatch UNIQUE (ProductId, BatchNumber)
);

CREATE INDEX IX_ProductBatches_FEFO ON ProductBatches (ProductId, ExpiryDate ASC)
    WHERE Status = 'Active' AND CurrentQuantity > 0;

CREATE TABLE ExpiryAlertSettings (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CategoryId INT FOREIGN KEY REFERENCES Categories(Id), -- NULL = all categories
    WarningDays1 INT DEFAULT 30, -- Yellow alert
    WarningDays2 INT DEFAULT 14, -- Orange alert
    WarningDays3 INT DEFAULT 7,  -- Red alert
    BlockSaleOnExpiry BIT DEFAULT 1,
    AllowOverrideWithPin BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

CREATE TABLE ExpiredItemOverrides (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProductBatchId INT FOREIGN KEY REFERENCES ProductBatches(Id),
    OrderItemId INT FOREIGN KEY REFERENCES OrderItems(Id),
    OverrideByUserId INT FOREIGN KEY REFERENCES Users(Id),
    Reason NVARCHAR(200) NOT NULL,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- =====================================================
-- CUSTOMER LOYALTY TABLES
-- =====================================================

CREATE TABLE Customers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PhoneNumber NVARCHAR(20) NOT NULL UNIQUE, -- Primary identifier
    FirstName NVARCHAR(100),
    LastName NVARCHAR(100),
    Email NVARCHAR(100),
    DateOfBirth DATE,
    Gender NVARCHAR(10),
    Address NVARCHAR(500),
    TierId INT FOREIGN KEY REFERENCES LoyaltyTiers(Id),
    TotalPointsEarned INT DEFAULT 0,
    TotalPointsRedeemed INT DEFAULT 0,
    CurrentPointsBalance INT DEFAULT 0,
    LifetimeSpend DECIMAL(18,2) DEFAULT 0,
    LastVisitDate DATETIME2,
    VisitCount INT DEFAULT 0,
    EnrolledAt DATETIME2 DEFAULT GETUTCDATE(),
    IsActive BIT DEFAULT 1
);

CREATE TABLE LoyaltyTiers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL, -- Bronze, Silver, Gold
    MinimumPoints INT NOT NULL, -- Threshold to reach tier
    EarningMultiplier DECIMAL(5,2) DEFAULT 1.0, -- 1.0 = 1x, 1.5 = 1.5x
    Description NVARCHAR(200),
    DisplayOrder INT DEFAULT 0,
    IsActive BIT DEFAULT 1
);

CREATE TABLE LoyaltySettings (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PointsPerCurrencyUnit DECIMAL(10,4) DEFAULT 0.01, -- 1 point per 100 KES
    CurrencyPerPoint DECIMAL(10,4) DEFAULT 0.10, -- 10 KES per 100 points
    MinimumRedemptionPoints INT DEFAULT 100,
    PointsExpiryMonths INT DEFAULT 12, -- 0 = never expire
    IsActive BIT DEFAULT 1
);

CREATE TABLE LoyaltyTransactions (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT FOREIGN KEY REFERENCES Customers(Id),
    ReceiptId INT FOREIGN KEY REFERENCES Receipts(Id),
    TransactionType NVARCHAR(20) NOT NULL, -- Earn, Redeem, Expire, Adjust
    Points INT NOT NULL, -- Positive for earn, negative for redeem
    BalanceBefore INT NOT NULL,
    BalanceAfter INT NOT NULL,
    Description NVARCHAR(200),
    ExpiresAt DATETIME2, -- For earned points
    ProcessedByUserId INT FOREIGN KEY REFERENCES Users(Id),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- =====================================================
-- MULTI-BRANCH STOCK TRANSFER TABLES
-- =====================================================

CREATE TABLE Branches (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Code NVARCHAR(20) NOT NULL UNIQUE,
    Name NVARCHAR(100) NOT NULL,
    Address NVARCHAR(500),
    Phone NVARCHAR(50),
    IsHeadOffice BIT DEFAULT 0,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

CREATE TABLE StockTransfers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TransferNumber NVARCHAR(20) NOT NULL UNIQUE,
    SourceBranchId INT FOREIGN KEY REFERENCES Branches(Id),
    DestinationBranchId INT FOREIGN KEY REFERENCES Branches(Id),
    Status NVARCHAR(20) DEFAULT 'Requested', -- Requested, Approved, Shipped, InTransit, Received, Cancelled
    RequestedByUserId INT FOREIGN KEY REFERENCES Users(Id),
    ApprovedByUserId INT FOREIGN KEY REFERENCES Users(Id),
    ShippedByUserId INT FOREIGN KEY REFERENCES Users(Id),
    ReceivedByUserId INT FOREIGN KEY REFERENCES Users(Id),
    TotalItems INT DEFAULT 0,
    TotalValue DECIMAL(18,2) DEFAULT 0,
    Notes NVARCHAR(500),
    RequestedAt DATETIME2 DEFAULT GETUTCDATE(),
    ApprovedAt DATETIME2,
    ShippedAt DATETIME2,
    ReceivedAt DATETIME2
);

CREATE TABLE StockTransferItems (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    StockTransferId INT FOREIGN KEY REFERENCES StockTransfers(Id),
    ProductId INT FOREIGN KEY REFERENCES Products(Id),
    RequestedQuantity DECIMAL(18,3) NOT NULL,
    ShippedQuantity DECIMAL(18,3),
    ReceivedQuantity DECIMAL(18,3),
    UnitCost DECIMAL(18,2),
    Variance DECIMAL(18,3), -- Received - Shipped
    VarianceReason NVARCHAR(200)
);

-- =====================================================
-- SYNC QUEUE FOR OFFLINE OPERATIONS
-- =====================================================

CREATE TABLE SyncQueue (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    QueueType NVARCHAR(50) NOT NULL, -- EtimsInvoice, MpesaVerify, CloudBackup
    EntityType NVARCHAR(50) NOT NULL, -- Receipt, Payment, etc.
    EntityId INT NOT NULL,
    Payload NVARCHAR(MAX), -- JSON data to sync
    Status NVARCHAR(20) DEFAULT 'Pending', -- Pending, Processing, Completed, Failed
    RetryCount INT DEFAULT 0,
    MaxRetries INT DEFAULT 3,
    LastError NVARCHAR(500),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    ProcessedAt DATETIME2
);

CREATE INDEX IX_SyncQueue_Pending ON SyncQueue (QueueType, Status, CreatedAt)
    WHERE Status = 'Pending';
```

---

## 7. Implementation Phases

### Phase 0: Critical Compliance (Before Launch)

| Priority | Feature | Estimated Effort | Stories |
|----------|---------|------------------|---------|
| P0 | KRA eTIMS Integration | 3-4 weeks | 5-6 stories |
| P0 | M-Pesa Daraja API | 2-3 weeks | 4-5 stories |
| P0 | Batch/Expiry Tracking | 2-3 weeks | 4-5 stories |
| P0 | Expiry Alert Dashboard | 1-2 weeks | 2-3 stories |
| P0 | Offline Sync Queue | 1-2 weeks | 2-3 stories |

**Total Phase 0:** 9-14 weeks, 17-22 stories

### Phase 1: Competitive Features (0-3 months)

| Priority | Feature | Estimated Effort | Stories |
|----------|---------|------------------|---------|
| P1 | Customer Loyalty (Full) | 3-4 weeks | 6-8 stories |
| P1 | Weight Scale Integration | 1 week | 2 stories |
| P1 | Real-Time Dashboard | 2 weeks | 3-4 stories |
| P1 | QR Code Payments | 1-2 weeks | 2-3 stories |
| P1 | Customer Purchase History | 1 week | 2 stories |
| P1 | Automated Email Reports | 1 week | 2 stories |

**Total Phase 1:** 9-12 weeks, 17-21 stories

### Phase 2: Chain Management (3-6 months)

| Priority | Feature | Estimated Effort | Stories |
|----------|---------|------------------|---------|
| P2 | Multi-Branch Stock Transfer | 3 weeks | 4-5 stories |
| P2 | Central Product Management | 2 weeks | 3-4 stories |
| P2 | Consolidated Chain Reporting | 2 weeks | 3-4 stories |
| P2 | Airtel Money Integration | 2 weeks | 3-4 stories |
| P2 | Mobile Reporting App | 4 weeks | 5-6 stories |

**Total Phase 2:** 13 weeks, 18-23 stories

### Phase 3: Enhanced Features (6+ months)

| Priority | Feature | Estimated Effort | Stories |
|----------|---------|------------------|---------|
| P3 | QR Menu Ordering | 3 weeks | 4-5 stories |
| P3 | Reservation System | 2 weeks | 3-4 stories |
| P3 | Time & Attendance | 2 weeks | 3-4 stories |
| P3 | Shift Scheduling | 2 weeks | 3-4 stories |
| P3 | Happy Hour Pricing | 1 week | 2 stories |
| P3 | Waste Tracking | 1 week | 2 stories |
| P3 | Comparative Analytics | 1 week | 2 stories |
| P3 | Bank Reconciliation | 2 weeks | 3 stories |
| P3 | SMS Marketing | 2 weeks | 3 stories |

**Total Phase 3:** 16 weeks, 25-31 stories

---

## Appendix: Competitive Reference

### A.1 Feature Matrix by Competitor

| Feature | SimbaPOS | Uzalynx | POSmart | **Our Target** |
|---------|----------|---------|---------|----------------|
| eTIMS | Yes | Yes | Yes | **Must Have** |
| M-Pesa STK | Yes | Yes | Yes | **Must Have** |
| Batch/Expiry | Yes | Yes | Yes | **Must Have** |
| Loyalty | Yes | Yes | No | **Must Have** |
| Multi-Branch | Yes | Yes | Yes | **Phase 2** |
| Weight Scale | Yes | Yes | Yes | **Phase 1** |
| QR Payments | Yes | Yes | No | **Phase 1** |
| Offline Mode | Yes | Yes | Yes | **Must Have** |
| Mobile App | Yes | Yes | No | **Phase 2** |
| KDS | Yes | Yes | Yes | **Have** |
| Table Mgmt | Yes | Yes | Yes | **Have** |

### A.2 Sources

1. Exa Web Search: Kenya POS systems analysis (Jan 2026)
2. KRA eTIMS documentation and certified integrator list
3. Safaricom Daraja API documentation
4. Competitor websites and feature lists
5. Market research on Kenyan retail technology

---

**Document End**

*This document serves as the master reference for creating implementation stories to address identified gaps and enhance the Multi-Mode POS System for the Kenya and Africa market.*
