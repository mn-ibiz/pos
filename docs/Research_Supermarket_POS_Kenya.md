# Research: Commercial Supermarket POS System for Kenya

**Document Version:** 1.0
**Date:** December 2025
**Research Focus:** Large-scale supermarket POS systems for the Kenyan market
**Target Competitors:** Microsoft Dynamics RMS, local Kenyan solutions

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Microsoft Dynamics RMS Analysis](#2-microsoft-dynamics-rms-analysis)
3. [Essential Supermarket POS Features](#3-essential-supermarket-pos-features)
4. [Kenya-Specific Requirements](#4-kenya-specific-requirements)
5. [Competitor Landscape in Kenya](#5-competitor-landscape-in-kenya)
6. [Enterprise & Multi-Store Features](#6-enterprise--multi-store-features)
7. [Technical Recommendations](#7-technical-recommendations)
8. [Gap Analysis vs Hospitality POS](#8-gap-analysis-vs-hospitality-pos)

---

## 1. Executive Summary

### 1.1 Market Context

Kenya's supermarket industry is dominated by chains like **Naivas, Quickmart, Carrefour Kenya, and Chandarana**. Most large retailers currently use **Microsoft Dynamics RMS** or similar enterprise solutions. However, MS Dynamics RMS reached **End of Life in July 2021** (extended support ended), creating an opportunity for modern alternatives.

### 1.2 Key Differentiators from Hospitality POS

| Aspect | Hospitality POS | Supermarket POS |
|--------|-----------------|-----------------|
| Transaction Volume | 50-500/day | 1,000-10,000+/day |
| SKU Count | 100-500 items | 10,000-50,000+ SKUs |
| Checkout Speed | Service-oriented | Speed-critical (<30 sec) |
| Weighing | Minimal | Critical (produce, deli) |
| Inventory | Recipe-based | Unit/weight-based |
| Barcode Scanning | Optional | Mandatory |
| Self-Checkout | Rare | Growing trend |
| Customer Loyalty | Basic | Advanced programs |

### 1.3 Critical Success Factors for Kenya

1. **KRA eTIMS Compliance** - Mandatory electronic tax invoicing
2. **M-Pesa Integration** - Primary payment method
3. **Multi-store centralized management** - Chain operations
4. **Scale integration** - Produce and bulk items
5. **High-volume transaction handling** - Speed and reliability

---

## 2. Microsoft Dynamics RMS Analysis

### 2.1 Product Status

| Milestone | Date |
|-----------|------|
| Launch | 2004 |
| End of Sales to New Users | July 2016 |
| Mainstream Support End | July 2016 |
| Extended Support End | July 13, 2021 |
| Successor | Retail Management Hero (RMH) / Dynamics 365 for Retail |

> **Note:** Despite EOL, many Kenyan retailers still run RMS due to migration costs and familiarity.

### 2.2 Core RMS Features (Benchmark)

#### 2.2.1 Point of Sale Operations
- Touch-screen and keyboard operation
- Quick item lookup by code, name, or barcode
- Multiple payment methods per transaction
- Cash drawer management
- Receipt customization
- Customer display support
- Offline operation mode

#### 2.2.2 Inventory Management
- Real-time stock tracking across locations
- Purchase order management
- Supplier management
- Stock transfers between stores
- Physical inventory counting
- Automatic reorder point alerts
- Batch/lot tracking
- Expiry date tracking

#### 2.2.3 Pricing & Promotions
- Multi-level pricing (retail, wholesale, member)
- Time-based promotions
- Buy X Get Y deals
- Mix and match offers
- Quantity discounts
- Customer-specific pricing
- Price override controls

#### 2.2.4 Customer Management
- Customer database
- Purchase history tracking
- Loyalty points/rewards
- Customer accounts/credit
- Customer categories (VIP, wholesale, regular)

#### 2.2.5 Multi-Store (HQ Module)
- Centralized product master
- Price management across stores
- Inventory visibility across locations
- Consolidated reporting
- Store-level vs. headquarters operations
- Data synchronization

#### 2.2.6 Reporting
- Sales reports (by product, category, cashier, time)
- Inventory reports
- Profit margin analysis
- Staff performance
- Cash reconciliation
- Custom report builder

### 2.3 Why RMS Was Popular in Kenya

1. **Microsoft brand trust** - Reliability perception
2. **Comprehensive features** - All-in-one solution
3. **Customization API** - Extensible for local needs
4. **Reasonable cost** - Affordable for SME retailers
5. **Local integrator ecosystem** - Trained partners
6. **SQL Server backend** - Familiar technology

---

## 3. Essential Supermarket POS Features

### 3.1 Core Transaction Processing

#### 3.1.1 High-Speed Checkout
| Feature | Priority | Description |
|---------|----------|-------------|
| Barcode scanning | Critical | Support for UPC-A, EAN-13, Code 128 |
| PLU lookup | Critical | Price Look-Up codes for produce |
| Quick keys | High | Configurable buttons for common items |
| Quantity entry | Critical | Fast quantity multiplication |
| Price override | High | With authorization controls |
| Void/correction | Critical | Line item and full transaction |
| Suspend/resume | High | Park transactions, resume later |
| Item search | Critical | By name, code, barcode |
| Average checkout time | KPI | Target: < 30 seconds |

#### 3.1.2 Payment Processing
| Payment Method | Priority | Notes |
|----------------|----------|-------|
| Cash | Critical | Change calculation, drawer management |
| M-Pesa | Critical | Kenya's dominant mobile money |
| Airtel Money | High | Secondary mobile money |
| Debit/Credit Card | High | Visa, Mastercard integration |
| Split tender | High | Multiple payment methods per transaction |
| Store credit | Medium | Customer account charges |
| Vouchers/Gift cards | Medium | Prepaid and promotional |
| Layaway | Low | Deposit-based purchasing |

#### 3.1.3 Receipt Management
- Thermal receipt printing (80mm standard)
- Receipt reprint capability
- Digital receipt option (email/SMS)
- KRA-compliant invoice format
- QR code for tax verification
- Customer copy vs. merchant copy

### 3.2 Scale Integration (Critical for Supermarkets)

#### 3.2.1 Supported Scale Types
| Scale Type | Use Case |
|------------|----------|
| POS-integrated scanner/scale | Checkout counter |
| Standalone label-printing scale | Produce section |
| Deli/butchery counter scale | Service departments |
| Price computing scale | Self-service produce |

#### 3.2.2 Scale Features Required
- **Direct POS integration** - Weight auto-populates at checkout
- **PLU code support** - Map products to price lookup codes
- **Random weight barcodes** - Price-embedded barcode reading
- **Tare weight handling** - Container weight deduction
- **Label printing** - Pre-packaged produce labels
- **Multi-unit support** - kg, g, lb as configured

#### 3.2.3 Random Weight Barcode Standards
```
UPC-A Format (12 digits): 2PPPPP CCCCC C
- 2 = Prefix for random weight
- PPPPP = PLU/Item code (5 digits)
- CCCCC = Price in cents (5 digits)
- C = Check digit

EAN-13 Format (13 digits): 02PPPPP CCCCC C
- 02 = Prefix for random weight
- PPPPP = PLU code (5 digits)
- CCCCC = Price (5 digits)
- C = Check digit
```

### 3.3 Inventory Management

#### 3.3.1 Stock Control Features
| Feature | Priority | Description |
|---------|----------|-------------|
| Real-time stock levels | Critical | Updated on every sale |
| Multiple locations | Critical | Store, warehouse, backroom |
| Stock transfers | Critical | Between locations |
| Stock adjustments | Critical | Waste, damage, shrinkage |
| Reorder points | High | Automatic low stock alerts |
| Purchase orders | High | Supplier ordering workflow |
| Goods receiving | Critical | PO-based and direct |
| Stock valuation | High | FIFO, weighted average |
| Batch/lot tracking | High | For recalls and expiry |
| Expiry date tracking | Critical | Perishable goods management |

#### 3.3.2 Category-Specific Inventory
| Category | Special Requirements |
|----------|---------------------|
| Produce | Weight-based, high shrinkage |
| Dairy | Expiry tracking, cold chain |
| Meat/Deli | Random weight, expiry, batch |
| Dry goods | Standard unit inventory |
| Frozen | Temperature monitoring |
| Beverages | Deposit/return handling |

#### 3.3.3 Shrinkage Management
- Waste recording with reason codes
- Damaged goods processing
- Theft/loss tracking
- Variance reporting
- Write-off authorization workflow

### 3.4 Pricing & Promotions

#### 3.4.1 Price Management
| Feature | Description |
|---------|-------------|
| Base price | Standard retail price |
| Cost price | For margin calculations |
| Price levels | Regular, member, wholesale |
| Price changes | Scheduled and immediate |
| Price labels | Shelf label printing |
| Competitor pricing | Price match capability |
| Margin protection | Minimum margin rules |

#### 3.4.2 Promotion Types (Critical for Supermarkets)
| Promotion Type | Example | Priority |
|----------------|---------|----------|
| % Discount | 10% off all beverages | Critical |
| Fixed discount | KSh 50 off on cooking oil | Critical |
| Buy X Get Y Free | Buy 2 Get 1 Free | Critical |
| Buy X Get Y % Off | Buy 2 Get 50% off 3rd | High |
| Mix & Match | Any 3 for KSh 200 | High |
| Bundle pricing | Bread + Milk = KSh 150 | High |
| Quantity breaks | 1-5: KSh 100, 6+: KSh 90 | High |
| Threshold discount | Spend KSh 5000, get 5% off | Medium |
| Time-based | Happy Hour pricing | Medium |
| Customer-specific | Member-only pricing | Critical |
| Coupon redemption | Barcode or code entry | Medium |

#### 3.4.3 Promotion Rules Engine
- Date range validity
- Day-of-week restrictions
- Time-of-day restrictions
- Minimum purchase requirements
- Maximum redemption limits
- Customer eligibility rules
- Store-specific promotions
- Stackable vs. exclusive promotions

### 3.5 Customer Management & Loyalty

#### 3.5.1 Customer Database
| Data Point | Purpose |
|------------|---------|
| Name, contact | Communication |
| Phone number | M-Pesa, SMS marketing |
| Email | Digital receipts, marketing |
| Customer type | Retail, wholesale, VIP |
| Credit limit | Account customers |
| Purchase history | Analytics, personalization |
| Loyalty balance | Points tracking |

#### 3.5.2 Loyalty Program Features
| Feature | Description |
|---------|-------------|
| Points earning | KSh spent = points earned |
| Points redemption | Points as payment |
| Tier levels | Bronze, Silver, Gold, Platinum |
| Member pricing | Exclusive discounts |
| Birthday rewards | Automated offers |
| Referral bonuses | Customer acquisition |
| Points expiry | Configurable validity |
| Statement generation | Points history |

#### 3.5.3 Kenya-Specific Loyalty Considerations
- **Phone number as identifier** - Primary lookup method
- **SMS notifications** - Points earned, balance, offers
- **M-Pesa integration** - Identify customer by payment number
- **Low smartphone penetration** - Don't rely solely on apps

### 3.6 Self-Checkout (Emerging Trend)

#### 3.6.1 Self-Checkout Features
- Customer-operated scanning
- Scale integration for produce
- Payment terminal integration
- Age verification alerts (alcohol)
- Weight verification (security)
- Attendant override capability
- Item lookup interface
- Bagging area sensors

#### 3.6.2 Considerations for Kenya
- Currently limited adoption
- Higher-end stores (Carrefour, Naivas select)
- Security concerns in high-shrinkage environments
- Customer education required
- Phase 2 feature recommendation

---

## 4. Kenya-Specific Requirements

### 4.1 KRA eTIMS Compliance (Mandatory)

#### 4.1.1 Background
The Kenya Revenue Authority (KRA) mandates electronic tax invoice management for all VAT-registered businesses through the **eTIMS (Electronic Tax Invoice Management System)**.

#### 4.1.2 Timeline
| Date | Milestone |
|------|-----------|
| August 2021 | TIMS regulation effective |
| August 2022 | Compliance deadline for VAT businesses |
| 2024 | eTIMS replaces TIMS |
| December 2024 | M-Pesa paybills to become virtual tax registers |

#### 4.1.3 eTIMS Requirements
| Requirement | Description |
|-------------|-------------|
| Real-time transmission | Invoices sent to KRA in real-time |
| Control Unit Number (CUN) | Unique device identifier |
| Invoice numbering | Sequential, tamper-proof |
| Buyer PIN | Customer KRA PIN on invoice |
| QR code | Verification code on receipt |
| Digital signature | Transaction authentication |
| Audit trail | Complete transaction history |

#### 4.1.4 Technical Integration
```
eTIMS Integration Methods:
1. Direct API Integration
   - REST API to KRA servers
   - Real-time invoice transmission
   - Response with Control Unit Invoice Number (CUIN)

2. Virtual Tax Register
   - M-Pesa Paybill integration
   - Automatic invoice generation

3. Third-party Middleware
   - eTIMS-certified intermediary
   - Handles API complexity
```

#### 4.1.5 Invoice Requirements
| Field | Requirement |
|-------|-------------|
| Seller PIN | KRA PIN of business |
| Seller name | Registered business name |
| Buyer PIN | Customer's KRA PIN (if provided) |
| Invoice number | Sequential, unique |
| Date/time | Transaction timestamp |
| Item details | Description, quantity, price |
| VAT breakdown | 16% standard rate, 0% exempt |
| Control code | eTIMS verification code |
| QR code | Scannable verification |

### 4.2 M-Pesa Integration (Critical)

#### 4.2.1 M-Pesa Market Position
- **~83% mobile money market share** in Kenya
- Primary payment method for most consumers
- Essential for any retail POS in Kenya

#### 4.2.2 Integration Options

**Option 1: M-Pesa Paybill**
- Customer initiates payment
- Business receives confirmation
- Manual reconciliation typically required
- Pros: Simple setup
- Cons: Slower, manual matching

**Option 2: M-Pesa Till (Buy Goods)**
- Customer pays to Till number
- Faster confirmation
- Suitable for retail
- Pros: Quicker than Paybill
- Cons: Still requires customer action

**Option 3: Lipa na M-Pesa (Daraja API)**
- Full API integration
- STK Push - prompt sent to customer phone
- Real-time confirmation
- Automatic receipt posting
- Pros: Seamless, fast
- Cons: Development required

#### 4.2.3 Daraja API Integration
```
Integration Flow:
1. Cashier enters amount in POS
2. Cashier enters customer phone number
3. POS sends STK Push via Daraja API
4. Customer receives payment prompt on phone
5. Customer enters M-Pesa PIN
6. Transaction confirmation received
7. POS marks payment complete
8. Receipt prints with M-Pesa transaction ID
```

#### 4.2.4 Technical Requirements
| Requirement | Details |
|-------------|---------|
| API Registration | Safaricom developer portal |
| Shortcode | Business shortcode (Paybill/Till) |
| Consumer Key/Secret | API credentials |
| Passkey | For Lipa na M-Pesa |
| Callback URL | Receive payment confirmations |
| Timeout handling | Graceful failure handling |

### 4.3 Other Mobile Money

#### 4.3.1 Airtel Money
- Second largest mobile money provider
- Similar integration patterns to M-Pesa
- Lower but significant market share

#### 4.3.2 T-Kash (Telkom)
- Smaller market share
- Partnership with M-Pesa for interoperability

### 4.4 Currency & Tax

| Aspect | Detail |
|--------|--------|
| Currency | Kenya Shilling (KES/KSh) |
| VAT Rate | 16% (standard) |
| VAT Exempt | Basic foodstuffs, medical supplies |
| Zero-rated | Exports, supplies to EPZ |
| Decimal handling | Typically round to nearest shilling |

### 4.5 Language & Localization

| Requirement | Details |
|-------------|---------|
| Primary language | English |
| Secondary | Swahili (Kiswahili) |
| Date format | DD/MM/YYYY |
| Number format | 1,000.00 |
| Receipt language | Configurable |

---

## 5. Competitor Landscape in Kenya

### 5.1 Enterprise Solutions (Large Supermarkets)

#### 5.1.1 Microsoft Dynamics 365 for Retail
- **Successor to:** Microsoft Dynamics RMS
- **Target:** Large enterprises, chains
- **Deployment:** Cloud-based
- **Cost:** High (licensing + implementation)
- **Local presence:** Through partners
- **Used by:** Major chains transitioning from RMS

#### 5.1.2 SAP Retail
- **Target:** Large enterprises
- **Integration:** Full ERP suite
- **Cost:** Very high
- **Used by:** Multinational retailers

#### 5.1.3 Oracle Retail
- **Target:** Large chains
- **Features:** Comprehensive retail suite
- **Cost:** Enterprise pricing
- **Adoption:** Limited in Kenya

### 5.2 Local Kenyan Solutions

#### 5.2.1 SalesLife POS
- **Developer:** Software Dynamics Group (Kenya)
- **Target:** SME to mid-market
- **Key features:**
  - KRA eTIMS compliant
  - M-Pesa integration
  - Multi-store support
  - Cloud-based options
- **Pricing:** Subscription-based

#### 5.2.2 SimbaPOS
- **Target:** Small to medium retailers
- **Key features:**
  - Supermarket module
  - Restaurant module
  - M-Pesa integration
  - Basic reporting
- **Pricing:** Affordable, one-time + support

#### 5.2.3 Uzapoint
- **Positioning:** "Built for Africa by Africans"
- **Target:** SMEs across Africa
- **Key features:**
  - Mobile and web POS
  - Multi-currency
  - Offline capability
  - 50K+ businesses claim
- **Recognition:** Award-winning

#### 5.2.4 NEXX Retail ERP (Compulynx)
- **Developer:** Compulynx (Kenya)
- **Target:** Mid-market to enterprise
- **Key features:**
  - Full ERP capabilities
  - KRA eTIMS compliant
  - Multi-store management
  - Advanced inventory
- **Deployment:** On-premise and cloud

#### 5.2.5 Endeavour Africa POS
- **Target:** Retail and pharmacy
- **Key features:**
  - KRA compliant
  - Integration capabilities
  - Fleet tracking (additional)

### 5.3 International Solutions with Kenya Presence

#### 5.3.1 GoFrugal
- **Origin:** India
- **Target:** Retail chains
- **Kenya presence:** Active
- **Key features:**
  - Chain management
  - eTIMS integration
  - Mobile apps
  - Cloud sync

#### 5.3.2 iVend Retail (CitiXsys)
- **Target:** Mid to large retail
- **Integration:** Microsoft Dynamics, SAP
- **Features:** Omnichannel, loyalty

#### 5.3.3 LS Retail (LS Central)
- **Platform:** Microsoft Dynamics 365
- **Target:** Enterprise retail
- **Features:** Unified commerce
- **Cost:** Premium

### 5.4 Feature Comparison Matrix

| Feature | SalesLife | SimbaPOS | NEXX ERP | Dynamics 365 |
|---------|-----------|----------|----------|--------------|
| eTIMS Compliance | ✓ | ✓ | ✓ | Via partner |
| M-Pesa Integration | ✓ | ✓ | ✓ | Via partner |
| Multi-store | ✓ | Limited | ✓ | ✓ |
| Scale Integration | ? | ? | ✓ | ✓ |
| Loyalty Program | Basic | Basic | ✓ | ✓ |
| Cloud Option | ✓ | Limited | ✓ | ✓ |
| Offline Mode | ✓ | ✓ | ✓ | ✓ |
| Price Range | $$ | $ | $$$ | $$$$ |

---

## 6. Enterprise & Multi-Store Features

### 6.1 Headquarters (HQ) Management

#### 6.1.1 Centralized Operations
| Function | Description |
|----------|-------------|
| Product master | Single source of truth for all products |
| Price management | Set prices centrally, override locally |
| Promotion management | Create and deploy promotions chain-wide |
| Vendor management | Centralized supplier relationships |
| User management | Centralized staff administration |
| Store configuration | Manage store profiles and settings |

#### 6.1.2 Data Distribution
```
HQ to Store:
- Product catalog updates
- Price changes
- Promotion rules
- User/role changes
- Policy updates

Store to HQ:
- Sales transactions
- Inventory levels
- Cash reconciliation
- Customer data
- Audit logs
```

#### 6.1.3 Synchronization Patterns
| Pattern | Use Case |
|---------|----------|
| Real-time sync | Cloud-connected stores |
| Scheduled sync | Batch updates (hourly, daily) |
| Store & forward | Offline stores, sync on connect |
| Hybrid | Real-time critical data, batch others |

### 6.2 Multi-Store Inventory

#### 6.2.1 Inventory Visibility
- Real-time stock levels across all stores
- Warehouse stock visibility
- In-transit stock tracking
- Reserved stock (layaway, orders)

#### 6.2.2 Stock Transfers
| Process | Steps |
|---------|-------|
| Request | Store requests items from warehouse/other store |
| Approval | HQ or source location approves |
| Dispatch | Source location dispatches goods |
| In-transit | Track goods movement |
| Receiving | Destination confirms receipt |
| Reconciliation | Variance handling |

#### 6.2.3 Central Purchasing
- Consolidated purchase orders
- Vendor negotiations at scale
- Direct-to-store delivery
- Cross-docking support
- Distribution center management

### 6.3 Consolidated Reporting

#### 6.3.1 Executive Dashboards
| KPI | Description |
|-----|-------------|
| Total sales | All stores combined |
| Sales by store | Comparative performance |
| Basket size | Average transaction value |
| Items per transaction | Measure of up-selling |
| Conversion rate | If foot traffic tracked |
| Inventory turnover | Stock efficiency |
| Gross margin | Profitability |
| Shrinkage rate | Loss percentage |

#### 6.3.2 Operational Reports
| Report | Frequency |
|--------|-----------|
| Daily sales summary | Daily |
| Cash reconciliation | Daily |
| Stock levels | Daily |
| Slow-moving stock | Weekly |
| Expiring items | Daily |
| Price change audit | As needed |
| Staff performance | Weekly |
| Promotion effectiveness | Per campaign |

#### 6.3.3 Analytics & BI
- Sales trend analysis
- Product performance
- Category analysis
- Customer segmentation
- Basket analysis (what sells together)
- Price elasticity
- Forecast/demand planning
- Anomaly detection (fraud, theft)

### 6.4 Enterprise Security

#### 6.4.1 Access Control
| Level | Description |
|-------|-------------|
| HQ Admin | Full system access |
| Regional Manager | Multiple stores |
| Store Manager | Single store operations |
| Supervisor | Shift operations |
| Cashier | POS only |
| Inventory Clerk | Stock functions only |

#### 6.4.2 Audit Trail
- All transactions logged
- User actions tracked
- Void/refund tracking
- Price override logging
- Cash drawer events
- System access logs

---

## 7. Technical Recommendations

### 7.1 Architecture Comparison

#### 7.1.1 On-Premise (Like RMS)
**Pros:**
- Full control over data
- Works without internet
- Lower ongoing costs
- Familiar to Kenya market

**Cons:**
- Hardware investment
- IT maintenance burden
- Limited remote access
- Scaling challenges

#### 7.1.2 Cloud-Based
**Pros:**
- Lower upfront cost
- Automatic updates
- Remote access
- Easier scaling
- Built-in backups

**Cons:**
- Internet dependency
- Data sovereignty concerns
- Ongoing subscription costs
- Kenya connectivity challenges

#### 7.1.3 Hybrid (Recommended for Kenya)
**Architecture:**
- Local server at each store for POS operations
- Cloud sync for HQ functions
- Offline capability at store level
- Real-time sync when connected

### 7.2 Technology Stack Recommendation

| Component | Recommendation | Rationale |
|-----------|---------------|-----------|
| Backend | C# .NET 6+ | Strong typing, enterprise-ready, familiar |
| Database | MS SQL Server | RMS migration path, robust |
| POS UI | WPF/WinUI | Touch-optimized, Windows native |
| HQ Web | ASP.NET Core + React | Modern web interface |
| API | REST + SignalR | Real-time capabilities |
| Mobile | .NET MAUI or React Native | Cross-platform |
| Reporting | SSRS or Power BI | Enterprise reporting |

### 7.3 Integration Requirements

| Integration | Priority | Method |
|-------------|----------|--------|
| KRA eTIMS | Critical | REST API |
| M-Pesa (Daraja) | Critical | REST API |
| Airtel Money | High | API |
| Weighing Scales | Critical | Serial/USB/Network |
| Barcode Scanners | Critical | HID/Serial |
| Receipt Printers | Critical | ESC/POS |
| Label Printers | High | ZPL/EPL |
| Card Terminals | High | Vendor-specific |
| Accounting (QuickBooks, Sage) | Medium | Export/API |

### 7.4 Hardware Requirements

#### 7.4.1 POS Terminal
| Component | Minimum | Recommended |
|-----------|---------|-------------|
| Processor | Intel i3 | Intel i5 |
| RAM | 4 GB | 8 GB |
| Storage | 128 GB SSD | 256 GB SSD |
| Display | 15" touch | 15.6"+ capacitive touch |
| OS | Windows 10 | Windows 11 |

#### 7.4.2 Peripherals
| Device | Specification |
|--------|---------------|
| Barcode scanner | 2D imaging, USB |
| Receipt printer | 80mm thermal, ESC/POS |
| Cash drawer | RJ11 or USB trigger |
| Scanner/scale | Integrated, certified |
| Customer display | 2-line or LCD |
| Label printer | Direct thermal, 2" or 4" |

#### 7.4.3 Store Server
| Component | Specification |
|-----------|---------------|
| Server | Mid-range server or high-end workstation |
| Processor | Intel Xeon or i7 |
| RAM | 16-32 GB |
| Storage | 1 TB+ SSD RAID |
| Network | Gigabit LAN |
| UPS | 1000VA+ with auto-shutdown |

---

## 8. Gap Analysis vs Hospitality POS

### 8.1 Features to Add

| Feature | Hospitality PRD | Supermarket Required | Priority |
|---------|-----------------|---------------------|----------|
| Barcode scanning | Optional | Critical | Must Have |
| Scale integration | No | Critical | Must Have |
| PLU codes | No | Critical | Must Have |
| Random weight barcodes | No | Critical | Must Have |
| Advanced promotions | Basic | Comprehensive | Must Have |
| Multi-store HQ | No | Critical | Must Have |
| Customer loyalty | Basic | Advanced | Must Have |
| KRA eTIMS | No | Critical | Must Have |
| M-Pesa Daraja API | Manual M-Pesa | Integrated | Must Have |
| Self-checkout | No | Future | Should Have |
| Shelf label printing | No | Important | Should Have |
| Central purchasing | No | Critical | Must Have |
| Stock transfers | No | Critical | Must Have |
| Batch/expiry tracking | Mentioned | Detailed | Must Have |

### 8.2 Features to Remove/Modify

| Feature | Change | Rationale |
|---------|--------|-----------|
| Table management | Remove | Not applicable |
| Kitchen Display | Remove | Not applicable |
| Kitchen printing | Modify | Use for backroom prep if needed |
| Course management | Remove | Not applicable |
| Recipe/ingredient costing | Modify | Simplify for retail |
| Work periods | Modify | Shift-based cash reconciliation |
| Bill splitting | Remove | Not applicable |
| Room charge | Remove | Not applicable |

### 8.3 Features to Retain

| Feature | Notes |
|---------|-------|
| User roles & permissions | Expand for multi-store |
| Receipt management | Adapt for retail |
| Cash drawer management | Same |
| X/Z reports | Enhance for KRA |
| Product categories | Same |
| Reporting | Expand significantly |
| Audit trail | Same, enhance |
| Void management | Same |

---

## 9. Recommended Feature Priority

### Phase 1: Core POS (MVP)

1. High-speed checkout with barcode scanning
2. Scale integration (basic)
3. Inventory management (single store)
4. M-Pesa Daraja integration
5. KRA eTIMS compliance
6. Basic customer management
7. Essential reports (daily, cash reconciliation)
8. Receipt printing with KRA requirements

### Phase 2: Enhanced Features

1. Multi-store HQ management
2. Advanced promotions engine
3. Customer loyalty program
4. Central purchasing
5. Stock transfers
6. Advanced reporting and analytics
7. Shelf label printing
8. Airtel Money integration

### Phase 3: Advanced Features

1. Self-checkout support
2. Mobile POS/Handheld
3. E-commerce integration
4. Advanced analytics/BI
5. Customer mobile app
6. Supplier portal
7. API for third-party integrations

---

## 10. Sources

- Microsoft Dynamics RMS Documentation
- Kenya Revenue Authority (KRA) eTIMS guidelines
- Safaricom Daraja API documentation
- ITRetail, POSNation, Square for Retail guides
- GoFrugal, LS Retail, NetSuite ERP documentation
- Local Kenya POS vendor websites (SalesLife, SimbaPOS, NEXX, Uzapoint)
- Industry research on supermarket POS features

---

**Document Status:** Research Complete
**Next Steps:** Create new PRD for Supermarket POS System based on these findings
