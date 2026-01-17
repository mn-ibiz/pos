# Product Requirements Document (PRD)
# Unified Point of Sale (POS) System
## For Hospitality & Retail/Supermarket Deployments

**Document Version:** 2.0
**Date:** December 2025
**Technology Stack:** C# / .NET 8 / WPF / MS SQL Server
**Platform:** Windows Desktop (Touch-Enabled) with Cloud Synchronization
**Target Market:** Kenya & East Africa
**Benchmark Reference:** Microsoft Dynamics RMS (EOL July 2021)

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Product Vision and Goals](#2-product-vision-and-goals)
3. [System Architecture Overview](#3-system-architecture-overview)
4. [Deployment Modes and Feature Activation](#4-deployment-modes-and-feature-activation)
5. [User Roles and Permissions](#5-user-roles-and-permissions)
6. [Core Modules (Always Active)](#6-core-modules-always-active)
7. [Hospitality Modules](#7-hospitality-modules)
8. [Retail/Supermarket Modules](#8-retailsupermarket-modules)
9. [Kenya Compliance and Integrations](#9-kenya-compliance-and-integrations)
10. [Offline-First Architecture and Cloud Sync](#10-offline-first-architecture-and-cloud-sync)
11. [Database Design](#11-database-design)
12. [Reporting and Analytics](#12-reporting-and-analytics)
13. [User Interface Requirements](#13-user-interface-requirements)
14. [Hardware Requirements](#14-hardware-requirements)
15. [Security and Compliance](#15-security-and-compliance)
16. [Non-Functional Requirements](#16-non-functional-requirements)
17. [API Design](#17-api-design)
18. [Implementation Roadmap](#18-implementation-roadmap)
19. [Glossary](#19-glossary)

---

## 1. Executive Summary

### 1.1 Purpose

This document defines comprehensive requirements for a **unified, modular Point of Sale system** designed to serve two distinct industry verticals from a single codebase:

1. **Hospitality Mode** - Hotels, restaurants, bars, cafes, lounges
2. **Retail/Supermarket Mode** - Supermarkets, grocery stores, retail chains, mini-marts

The system employs configuration-driven feature activation, enabling businesses to deploy the exact feature set required for their industry while sharing core infrastructure, reducing development and maintenance costs.

### 1.2 Market Context

Kenya's retail and hospitality industries require modern POS solutions that address:

- **Microsoft Dynamics RMS End-of-Life** (July 2021) - Many Kenyan retailers still use RMS, creating a migration opportunity
- **KRA eTIMS Mandate** - All VAT-registered businesses must use electronic tax invoice systems
- **M-Pesa Dominance** - ~83% mobile money market share requires seamless integration
- **Connectivity Challenges** - Unreliable internet necessitates offline-first architecture
- **Multi-Store Operations** - Major chains (Naivas, Quickmart, Carrefour) need centralized management

### 1.3 Product Overview

The Unified POS System provides:

| Capability | Description |
|------------|-------------|
| **Dual-Mode Operation** | Single codebase deployable for hospitality OR retail |
| **Modular Architecture** | Feature flags enable/disable modules based on deployment |
| **Offline-First Design** | Full operation without internet connectivity |
| **Cloud Synchronization** | Real-time or scheduled sync to central cloud database |
| **Multi-Store HQ** | Headquarters management for retail chains |
| **Kenya Compliance** | Built-in KRA eTIMS and M-Pesa Daraja integration |
| **Touch-Optimized** | Large buttons, visual product grid, fast workflows |
| **Enterprise-Grade** | RBAC, audit trails, comprehensive reporting |

### 1.4 Key Differentiators

| Feature | Description |
|---------|-------------|
| **Unified Platform** | One codebase serves hospitality and retail - unique in Kenya market |
| **Offline-First** | Local database with queue-and-sync - no lost transactions |
| **Kenya-Native** | Built-in KRA eTIMS, M-Pesa STK Push, KES currency handling |
| **RMS Migration Path** | Familiar SQL Server backend for RMS users |
| **Modern Stack** | .NET 8, Entity Framework Core, SignalR for real-time sync |
| **Scalable** | From single outlet to 100+ store chains |

### 1.5 Deployment Mode Comparison

| Aspect | Hospitality Mode | Retail/Supermarket Mode |
|--------|------------------|------------------------|
| **Primary Workflow** | Order → Kitchen → Serve → Pay | Scan → Pay → Go |
| **Transaction Style** | Service-oriented (2-30 min) | Speed-critical (<30 sec) |
| **Typical SKU Count** | 100-500 items | 10,000-50,000+ |
| **Table Management** | Yes (floor plans, sections) | No |
| **Kitchen Display/Printing** | Yes (KOT routing) | No |
| **Scale Integration** | Minimal (bar items) | Critical (produce, deli) |
| **Barcode Scanning** | Optional | Mandatory |
| **Bill Splitting/Merging** | Yes | No |
| **Room Charge** | Yes (hotels) | No |
| **Self-Checkout** | No | Yes (optional) |
| **PLU Codes** | No | Yes (produce) |
| **Advanced Promotions** | Basic discounts | Buy X Get Y, Mix & Match |
| **Loyalty Program** | Basic | Advanced tiers |
| **Multi-Store HQ** | Optional | Critical for chains |
| **Reservations** | Yes | No |

### 1.6 Success Metrics

| Metric | Hospitality Target | Retail Target |
|--------|-------------------|---------------|
| Average transaction time | < 30 sec (quick service) | < 30 sec |
| Full service transaction | 2-5 min | N/A |
| System uptime | 99.9% during business hours | 99.9% |
| End-of-day reconciliation | < 15 minutes | < 15 minutes |
| Stock accuracy | > 98% | > 99% |
| New cashier training | < 2 hours | < 2 hours |
| eTIMS compliance rate | 100% | 100% |
| Offline transaction queue | Unlimited (sync on connect) | Unlimited |

---

## 2. Product Vision and Goals

### 2.1 Vision Statement

To create the **leading unified POS platform for East Africa** that empowers hospitality and retail businesses of all sizes to operate efficiently, maintain tax compliance, and scale confidently—whether online or offline.

### 2.2 Mission

Deliver a robust, user-friendly, Kenya-compliant POS system that:
- Replaces aging Microsoft Dynamics RMS installations
- Provides genuine offline capability for unreliable connectivity
- Integrates seamlessly with M-Pesa and KRA eTIMS
- Scales from single outlets to enterprise chains
- Serves both hospitality and retail from one platform

### 2.3 Business Goals

| Goal | Target | Measurement |
|------|--------|-------------|
| Market Penetration | 15% of Kenya POS market in 3 years | Customer count, revenue |
| RMS Migration | 500+ RMS customers migrated | Migration completions |
| Dual-Mode Adoption | 30% customers using both modes | Deployment statistics |
| eTIMS Compliance | 100% of customers compliant | KRA audit results |
| Uptime | 99.9% during business hours | System monitoring |
| Customer Satisfaction | NPS > 50 | Quarterly surveys |
| Support Tickets | < 2 per customer per month | Help desk metrics |

### 2.4 Target Market Segments

#### 2.4.1 Hospitality
| Segment | Size | Key Needs |
|---------|------|-----------|
| Hotels (F&B) | 50-500 employees | Room charge, multi-outlet, reporting |
| Full-Service Restaurants | 10-50 employees | Table management, KDS, reservations |
| Quick-Service Restaurants | 5-20 employees | Speed, order queue, kitchen printing |
| Bars & Lounges | 5-30 employees | Tab management, happy hour pricing |
| Cafes & Coffee Shops | 3-15 employees | Quick checkout, modifiers, loyalty |

#### 2.4.2 Retail/Supermarket
| Segment | Size | Key Needs |
|---------|------|-----------|
| Supermarket Chains | 100-5000 employees | Multi-store HQ, central purchasing, scale |
| Independent Supermarkets | 20-100 employees | Full features, offline operation |
| Mini-Marts/Convenience | 2-10 employees | Speed, basic inventory, M-Pesa |
| Specialty Retail | 5-50 employees | Loyalty, customer management |

### 2.5 Geographic Focus

- **Primary Market:** Kenya
- **Secondary Markets:** Tanzania, Uganda, Rwanda
- **Currency:** Kenya Shilling (KES/KSh)
- **Tax Authority:** Kenya Revenue Authority (KRA)
- **Payment Ecosystem:** M-Pesa (Safaricom), Airtel Money, T-Kash, Visa/Mastercard

---

## 3. System Architecture Overview

### 3.1 High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              CLOUD LAYER                                     │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐              │
│  │  Azure SQL DB   │  │  SignalR Hub    │  │  Blob Storage   │              │
│  │  (Central DB)   │  │  (Real-time)    │  │  (Images/Docs)  │              │
│  └────────┬────────┘  └────────┬────────┘  └────────┬────────┘              │
│           │                    │                    │                        │
│  ┌────────┴────────────────────┴────────────────────┴────────┐              │
│  │                    Sync Gateway API                        │              │
│  │         (REST + SignalR for Real-time Events)             │              │
│  └────────────────────────────────────────────────────────────┘              │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    │ Internet (when available)
                                    │
┌─────────────────────────────────────────────────────────────────────────────┐
│                           STORE/OUTLET LAYER                                 │
│                                                                              │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │                         STORE SERVER                                   │  │
│  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐       │  │
│  │  │ SQL Server      │  │ Sync Service    │  │ Print Service   │       │  │
│  │  │ Express (Local) │  │ (Background)    │  │ (Spooler)       │       │  │
│  │  └─────────────────┘  └─────────────────┘  └─────────────────┘       │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                    │                                         │
│                                    │ LAN                                     │
│          ┌─────────────────────────┼─────────────────────────┐              │
│          │                         │                         │              │
│  ┌───────┴───────┐  ┌──────────────┴──────────────┐  ┌──────┴───────┐     │
│  │  POS Terminal │  │      POS Terminal           │  │ POS Terminal │     │
│  │  (Cashier 1)  │  │      (Cashier 2)            │  │ (Cashier 3)  │     │
│  └───────────────┘  └─────────────────────────────┘  └──────────────┘     │
│          │                         │                         │              │
│          └─────────────────────────┼─────────────────────────┘              │
│                                    │                                         │
│  ┌─────────────────────────────────┼─────────────────────────────────────┐  │
│  │                            PERIPHERALS                                 │  │
│  │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐   │  │
│  │  │ Receipt  │ │ Barcode  │ │ Scanner/ │ │ Cash     │ │ Customer │   │  │
│  │  │ Printer  │ │ Scanner  │ │ Scale    │ │ Drawer   │ │ Display  │   │  │
│  │  └──────────┘ └──────────┘ └──────────┘ └──────────┘ └──────────┘   │  │
│  │  ┌──────────┐ ┌──────────┐ ┌──────────┐                             │  │
│  │  │ Kitchen  │ │ Label    │ │ Card     │                             │  │
│  │  │ Printer  │ │ Printer  │ │ Terminal │                             │  │
│  │  └──────────┘ └──────────┘ └──────────┘                             │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 3.2 Technology Stack

| Layer | Technology | Rationale |
|-------|------------|-----------|
| **POS Application** | C# / .NET 8 | Enterprise-grade, strong typing, familiar to RMS developers |
| **UI Framework** | WPF (Windows Presentation Foundation) | Touch-optimized, MVVM, rich controls |
| **Local Database** | SQL Server Express 2022 | RMS migration path, robust, free |
| **Cloud Database** | Azure SQL Database | Managed, scalable, geo-redundant |
| **ORM** | Entity Framework Core 8 | Code-first, migrations, LINQ |
| **Real-time Sync** | SignalR | Bidirectional, automatic reconnection |
| **API** | ASP.NET Core Web API | REST + SignalR hub |
| **Background Services** | .NET Worker Services | Sync, print queue, eTIMS |
| **Reporting** | RDLC / FastReport | Local report generation |
| **Receipt Printing** | ESC/POS Commands | Industry standard thermal printers |
| **Label Printing** | ZPL / EPL | Zebra and compatible printers |

### 3.3 Application Layers

```
┌─────────────────────────────────────────────────────────────────┐
│                    PRESENTATION LAYER                            │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌───────────┐ │
│  │ POS Views   │ │ Admin Views │ │ Report Views│ │ KDS Views │ │
│  │ (WPF/XAML)  │ │ (WPF/XAML)  │ │ (WPF/XAML)  │ │ (WPF/XAML)│ │
│  └─────────────┘ └─────────────┘ └─────────────┘ └───────────┘ │
│                         ViewModels (MVVM)                        │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    APPLICATION LAYER                             │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌───────────┐ │
│  │ Sales       │ │ Inventory   │ │ Customer    │ │ Sync      │ │
│  │ Service     │ │ Service     │ │ Service     │ │ Service   │ │
│  └─────────────┘ └─────────────┘ └─────────────┘ └───────────┘ │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌───────────┐ │
│  │ Payment     │ │ Reporting   │ │ eTIMS       │ │ Print     │ │
│  │ Service     │ │ Service     │ │ Service     │ │ Service   │ │
│  └─────────────┘ └─────────────┘ └─────────────┘ └───────────┘ │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐               │
│  │ Promotion   │ │ Loyalty     │ │ User        │               │
│  │ Engine      │ │ Engine      │ │ Service     │               │
│  └─────────────┘ └─────────────┘ └─────────────┘               │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    DOMAIN LAYER                                  │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌───────────┐ │
│  │ Entities    │ │ Value       │ │ Domain      │ │ Domain    │ │
│  │             │ │ Objects     │ │ Events      │ │ Services  │ │
│  └─────────────┘ └─────────────┘ └─────────────┘ └───────────┘ │
│          (Transaction, Product, Customer, Order, etc.)          │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                 INFRASTRUCTURE LAYER                             │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌───────────┐ │
│  │ EF Core     │ │ M-Pesa      │ │ eTIMS       │ │ Hardware  │ │
│  │ Repositories│ │ Client      │ │ Client      │ │ Drivers   │ │
│  └─────────────┘ └─────────────┘ └─────────────┘ └───────────┘ │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐               │
│  │ SignalR     │ │ Print       │ │ Scale       │               │
│  │ Client      │ │ Adapter     │ │ Adapter     │               │
│  └─────────────┘ └─────────────┘ └─────────────┘               │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    DATA LAYER                                    │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │              SQL Server Express (Local)                    │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

### 3.4 Module Organization

```
UnifiedPOS/
├── src/
│   ├── UnifiedPOS.Core/                    # Shared domain entities, interfaces
│   ├── UnifiedPOS.Application/             # Application services, use cases
│   ├── UnifiedPOS.Infrastructure/          # EF Core, external integrations
│   ├── UnifiedPOS.Desktop/                 # WPF application
│   │   ├── Views/
│   │   │   ├── Shared/                     # Login, Dashboard, Settings
│   │   │   ├── POS/                        # Transaction screen
│   │   │   ├── Hospitality/                # Tables, KDS, Reservations
│   │   │   ├── Retail/                     # Scale, Self-checkout
│   │   │   ├── Inventory/                  # Stock management
│   │   │   ├── Customers/                  # CRM, Loyalty
│   │   │   ├── Reports/                    # All reports
│   │   │   └── Admin/                      # Users, Roles, Config
│   │   └── ViewModels/
│   ├── UnifiedPOS.Sync/                    # Background sync service
│   ├── UnifiedPOS.Print/                   # Print service
│   └── UnifiedPOS.KDS/                     # Kitchen Display (optional)
├── cloud/
│   ├── UnifiedPOS.API/                     # Cloud API + SignalR Hub
│   ├── UnifiedPOS.HQ/                      # Headquarters web portal
│   └── UnifiedPOS.Functions/               # Azure Functions (scheduled jobs)
└── tests/
```

---

## 4. Deployment Modes and Feature Activation

### 4.1 Deployment Mode Configuration

The system supports three deployment modes, configured at installation and stored in `appsettings.json`:

```json
{
  "DeploymentMode": "RETAIL",
  "BusinessType": "Supermarket",
  "StoreId": "NVS-001",
  "StoreName": "Naivas Westlands",
  "CountryCode": "KE",
  "CurrencyCode": "KES",
  "TaxAuthority": "KRA",
  "Features": {
    "EnableTableManagement": false,
    "EnableKitchenDisplay": false,
    "EnableScaleIntegration": true,
    "EnableAdvancedPromotions": true,
    "EnableLoyaltyProgram": true,
    "EnableMultiStoreHQ": true,
    "EnableSelfCheckout": false,
    "EnableRoomCharge": false,
    "EnableReservations": false
  }
}
```

### 4.2 Deployment Modes

| Mode | Value | Use Case |
|------|-------|----------|
| **Hospitality** | `HOSPITALITY` | Hotels, restaurants, bars, cafes |
| **Retail** | `RETAIL` | Supermarkets, grocery, retail stores |
| **Hybrid** | `HYBRID` | Food courts, hotel gift shops, mixed-use venues |

### 4.3 Feature Activation Matrix

| Module / Feature | Hospitality | Retail | Hybrid | Can Override |
|------------------|:-----------:|:------:|:------:|:------------:|
| **CORE (Always Active)** |
| User Management | ✓ | ✓ | ✓ | No |
| Product Management | ✓ | ✓ | ✓ | No |
| Basic Inventory | ✓ | ✓ | ✓ | No |
| Cash Payments | ✓ | ✓ | ✓ | No |
| M-Pesa Integration | ✓ | ✓ | ✓ | No |
| KRA eTIMS | ✓ | ✓ | ✓ | No |
| Receipt Printing | ✓ | ✓ | ✓ | No |
| X/Z Reports | ✓ | ✓ | ✓ | No |
| Work Periods | ✓ | ✓ | ✓ | No |
| Audit Trail | ✓ | ✓ | ✓ | No |
| **HOSPITALITY MODULES** |
| Table Management | ✓ | ✗ | Optional | Yes |
| Floor Plan Designer | ✓ | ✗ | Optional | Yes |
| Kitchen Order Tickets (KOT) | ✓ | ✗ | Optional | Yes |
| Kitchen Display System (KDS) | ✓ | ✗ | Optional | Yes |
| Course Management | ✓ | ✗ | Optional | Yes |
| Bill Splitting | ✓ | ✗ | Optional | Yes |
| Bill Merging | ✓ | ✗ | Optional | Yes |
| Room Charge (Hotels) | Optional | ✗ | Optional | Yes |
| Reservations | Optional | ✗ | Optional | Yes |
| Service Charge | ✓ | ✗ | Optional | Yes |
| Recipe/Ingredient Costing | ✓ | ✗ | Optional | Yes |
| **RETAIL MODULES** |
| Barcode Scanning | Optional | ✓ | ✓ | Yes |
| Scale Integration | ✗ | ✓ | Optional | Yes |
| PLU Code Management | ✗ | ✓ | Optional | Yes |
| Random Weight Barcodes | ✗ | ✓ | Optional | Yes |
| Advanced Promotions Engine | Basic | ✓ | ✓ | Yes |
| Loyalty Program (Advanced) | Basic | ✓ | ✓ | Yes |
| Multi-Store HQ | ✗ | ✓ | Optional | Yes |
| Central Purchasing | ✗ | ✓ | Optional | Yes |
| Stock Transfers | ✗ | ✓ | Optional | Yes |
| Self-Checkout Mode | ✗ | Optional | Optional | Yes |
| Shelf Label Printing | ✗ | ✓ | Optional | Yes |
| Batch/Expiry Tracking | Basic | ✓ | ✓ | Yes |

### 4.4 Feature Flag Implementation

```csharp
public interface IFeatureService
{
    bool IsEnabled(Feature feature);
    DeploymentMode GetDeploymentMode();
    IEnumerable<Feature> GetEnabledFeatures();
}

public enum DeploymentMode
{
    Hospitality,
    Retail,
    Hybrid
}

public enum Feature
{
    // Core (always enabled)
    UserManagement,
    ProductManagement,
    BasicInventory,
    CashPayments,
    MPesaIntegration,
    ETIMSCompliance,
    ReceiptPrinting,
    XZReports,
    WorkPeriods,
    AuditTrail,

    // Hospitality
    TableManagement,
    FloorPlanDesigner,
    KitchenOrderTickets,
    KitchenDisplaySystem,
    CourseManagement,
    BillSplitting,
    BillMerging,
    RoomCharge,
    Reservations,
    ServiceCharge,
    RecipeCosting,

    // Retail
    BarcodeScanning,
    ScaleIntegration,
    PLUManagement,
    RandomWeightBarcodes,
    AdvancedPromotions,
    AdvancedLoyalty,
    MultiStoreHQ,
    CentralPurchasing,
    StockTransfers,
    SelfCheckout,
    ShelfLabelPrinting,
    BatchExpiryTracking
}
```

### 4.5 UI Adaptation by Mode

The user interface automatically adapts based on deployment mode:

#### 4.5.1 Hospitality Mode UI
- **Main Screen**: Product grid with table selector
- **Navigation**: Tables, Orders, Kitchen, Reservations, Reports
- **Order Flow**: Select Table → Add Items → Send to Kitchen → Serve → Settle Bill
- **Additional Panels**: Table map, KDS monitor, course timing

#### 4.5.2 Retail Mode UI
- **Main Screen**: Product grid with barcode input field prominently displayed
- **Navigation**: POS, Inventory, Customers, Promotions, HQ, Reports
- **Order Flow**: Scan Items → Apply Promotions → Collect Payment → Print Receipt
- **Additional Panels**: Scale weight display, loyalty lookup, promotion alerts

#### 4.5.3 Hybrid Mode UI
- **Main Screen**: Configurable layout with both table and quick-sale options
- **Navigation**: Combined navigation with section separators
- **Order Flow**: Supports both table-based and quick checkout workflows
- **Mode Toggle**: Easy switch between hospitality and retail workflows

### 4.6 Mode Switching

```csharp
// Mode can be changed by Administrator
// Requires application restart
public class DeploymentModeService
{
    public async Task<bool> ChangeDeploymentMode(
        DeploymentMode newMode,
        string adminPin)
    {
        // Validate admin authorization
        if (!await _authService.ValidateAdminPin(adminPin))
            return false;

        // Log the change
        await _auditService.LogModeChange(
            _currentMode,
            newMode,
            _currentUser);

        // Update configuration
        await _configService.UpdateDeploymentMode(newMode);

        // Signal restart required
        _applicationService.RequestRestart(
            "Deployment mode changed. Restart required.");

        return true;
    }
}
```

---

## 5. User Roles and Permissions

### 5.1 Role Hierarchy

The system implements a hierarchical Role-Based Access Control (RBAC) system:

```
                    ┌─────────────────┐
                    │  ADMINISTRATOR  │
                    │    (Owner)      │
                    └────────┬────────┘
                             │
              ┌──────────────┼──────────────┐
              │              │              │
    ┌─────────┴─────────┐   │   ┌──────────┴─────────┐
    │  REGIONAL MANAGER │   │   │   HQ ADMINISTRATOR  │
    │   (Multi-Store)   │   │   │   (Chain Operations)│
    └─────────┬─────────┘   │   └────────────────────┘
              │             │
    ┌─────────┴─────────┐   │
    │   STORE MANAGER   │◄──┘
    └─────────┬─────────┘
              │
    ┌─────────┴─────────┐
    │    SUPERVISOR     │
    └─────────┬─────────┘
              │
    ┌─────────┴─────────┐
    │      CASHIER      │
    └─────────┬─────────┘
              │
    ┌─────────┴─────────┐
    │  WAITER / CLERK   │
    └───────────────────┘
```

### 5.2 Role Definitions

#### 5.2.1 Administrator / Owner

**Description**: Full system access with complete control over all operations, settings, and data.

| Permission Category | Permissions |
|---------------------|-------------|
| Work Period | Open, Close, View All History, Override |
| Sales | Create, View All, Modify Any, Void Any, Refund |
| Receipts | View All, Void, Reprint, Split, Merge |
| Products | Create, Edit, Delete, Set Prices, Import/Export |
| Inventory | Full Access (Stock, Purchases, Adjustments, Write-offs) |
| Users | Create, Edit, Delete, Assign Roles, Reset Passwords |
| Roles | Create, Edit, Delete, Assign Permissions |
| Reports | All Reports (X, Z, Sales, Inventory, Audit, eTIMS) |
| Settings | Full System Configuration |
| Discounts | Apply Any Discount (0-100%) |
| Voids | Void Any Transaction |
| eTIMS | Configure, View Logs, Manual Retry |
| Multi-Store | View All Stores, Transfer Products, Set Prices |
| Sync | Force Sync, View Sync Status, Resolve Conflicts |

#### 5.2.2 HQ Administrator (Retail Chains)

**Description**: Headquarters operations for multi-store chains. Read access to all stores, write access to central data.

| Permission Category | Permissions |
|---------------------|-------------|
| Products | Create, Edit, Set Centralized Prices |
| Promotions | Create Chain-wide Promotions |
| Inventory | View All Stores, Create Stock Transfers |
| Purchasing | Create Central POs, Manage Suppliers |
| Reports | Consolidated Multi-Store Reports |
| Pricing | Set/Override Store Prices |
| Stores | Configure Store Settings, Add New Stores |
| Users | Manage Store Managers, View All Staff |

#### 5.2.3 Regional Manager

**Description**: Oversees multiple stores within a region.

| Permission Category | Permissions |
|---------------------|-------------|
| Stores | View/Manage Assigned Region Stores |
| Reports | Regional Consolidated Reports |
| Staff | View Regional Staff Performance |
| Inventory | View Regional Stock, Approve Transfers |
| Pricing | Regional Price Adjustments (within limits) |

#### 5.2.4 Store Manager

**Description**: Day-to-day operational management of a single store.

| Permission Category | Permissions |
|---------------------|-------------|
| Work Period | Open, Close |
| Sales | Create, View All Store Sales, Void (with reason) |
| Receipts | View All, Void, Reprint, Split, Merge |
| Products | Edit (no delete), Set Store-specific Prices |
| Inventory | View Stock, Receive Purchases, Adjustments |
| Users | Create Cashiers/Waiters/Clerks, Reset Passwords |
| Reports | X Report, Z Report, Store Sales Reports |
| Discounts | Apply Up to 50% Discount |
| Voids | Void Transactions (with mandatory reason) |
| Promotions | Activate/Deactivate Store Promotions |
| eTIMS | View Logs, Manual Retry Failed Invoices |

#### 5.2.5 Supervisor

**Description**: Floor supervision with authority over cashiers and waiters/clerks.

| Permission Category | Permissions |
|---------------------|-------------|
| Work Period | View Status Only |
| Sales | Create, View Team Sales, Override Price |
| Receipts | View Team Receipts, Reprint |
| Products | View Only |
| Inventory | View Stock Levels |
| Reports | X Report (Team), Sales Summary |
| Discounts | Apply Up to 20% Discount |
| Voids | Request Void (requires Manager approval) |
| Tables (Hospitality) | Assign/Reassign Tables |
| Promotions | Apply Manual Promotions |

#### 5.2.6 Cashier

**Description**: Handles payments and manages the cash register.

| Permission Category | Permissions |
|---------------------|-------------|
| Sales | Create, View Own Sales |
| Receipts | View Own, Settle, Reprint Own |
| Payment | Accept All Payment Methods |
| Cash Drawer | Open, Close, Count |
| Reports | Own Shift Summary |
| Discounts | Apply Up to 10% Discount |
| Scale (Retail) | Weigh Items, Apply PLU |
| Loyalty | Look Up, Apply Points |

#### 5.2.7 Waiter / Server (Hospitality)

**Description**: Order taking and basic sales functions in hospitality settings.

| Permission Category | Permissions |
|---------------------|-------------|
| Sales | Create Orders, View Own Orders |
| Receipts | View Own Receipts, Add Items to Own Receipts |
| Tables | Assign, Transfer (own tables only) |
| Products | View Only (with prices) |
| Reports | View Own Sales Summary |
| Discounts | None (must request from Supervisor/Manager) |
| Kitchen | Send Orders, View Order Status |

#### 5.2.8 Inventory Clerk (Retail)

**Description**: Stock management without sales access.

| Permission Category | Permissions |
|---------------------|-------------|
| Inventory | Full Stock Management |
| Receiving | Receive Goods, Create GRN |
| Stock Take | Conduct Physical Counts |
| Products | View, Update Stock Levels Only |
| Labels | Print Shelf Labels |
| Reports | Inventory Reports Only |
| Transfers | Initiate Stock Transfer Requests |

### 5.3 Custom Role Management

Administrators can create custom roles:

| Capability | Description |
|------------|-------------|
| Create Custom Roles | Define new roles with specific permission sets |
| Clone Existing Roles | Copy a role and modify permissions |
| Permission Granularity | Toggle individual permissions within categories |
| Role Assignment | Assign multiple roles to a single user |
| Temporary Elevation | Allow temporary permission elevation with PIN |
| Time-Based Permissions | Permissions active only during certain hours |
| Store-Specific Roles | Different permissions per store location |

### 5.4 Permission Override Workflow

When a user lacks permission for an action:

```
┌─────────────────────────────────────────────────────────────────┐
│                    PERMISSION OVERRIDE FLOW                      │
└─────────────────────────────────────────────────────────────────┘

    User Attempts          System Displays           Authorized User
    Restricted Action  →  "Authorization Required" → Enters PIN
         │                        │                       │
         │                        ▼                       │
         │              ┌───────────────────┐            │
         │              │  Enter Manager    │            │
         │              │      PIN          │◄───────────┘
         │              └─────────┬─────────┘
         │                        │
         │                        ▼
         │              ┌───────────────────┐
         │              │ Validate Against  │
         │              │ Permission Level  │
         │              └─────────┬─────────┘
         │                        │
         │         ┌──────────────┴──────────────┐
         │         ▼                             ▼
         │   ┌───────────┐                ┌───────────┐
         │   │  Denied   │                │  Approved │
         │   └─────┬─────┘                └─────┬─────┘
         │         │                            │
         │         ▼                            ▼
         │   Display Error               Execute Action
         │   "Insufficient               Log Override:
         │    Permission"                - Original User
         │                               - Authorizing User
         │                               - Timestamp
         │                               - Action Details
         └─────────────────────────────────────────────────
```

### 5.5 Authentication Requirements

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| AUTH-001 | Unique credentials per staff member | Must Have |
| AUTH-002 | Password minimum 8 characters, complexity rules | Must Have |
| AUTH-003 | PIN login option (4-6 digits) for quick access | Must Have |
| AUTH-004 | Biometric login support (fingerprint) | Could Have |
| AUTH-005 | Session timeout after 15 min inactivity (configurable) | Must Have |
| AUTH-006 | Failed login lockout (5 attempts, 15 min) | Must Have |
| AUTH-007 | Password expiry policy (configurable, default 90 days) | Should Have |
| AUTH-008 | Force password change on first login | Must Have |
| AUTH-009 | Password history (prevent reuse of last 5) | Should Have |
| AUTH-010 | Concurrent session control (one session per user) | Should Have |

### 5.6 User Data Model

```csharp
public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string PasswordHash { get; set; }
    public string Pin { get; set; }  // Hashed
    public bool IsActive { get; set; }
    public bool MustChangePassword { get; set; }
    public DateTime? PasswordExpiresAt { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public Guid? StoreId { get; set; }  // null = all stores

    // Navigation
    public virtual ICollection<UserRole> UserRoles { get; set; }
    public virtual Store Store { get; set; }
}

public class Role
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsSystemRole { get; set; }  // Cannot be deleted
    public int HierarchyLevel { get; set; }  // Lower = more powerful
    public DateTime CreatedAt { get; set; }

    // Navigation
    public virtual ICollection<RolePermission> Permissions { get; set; }
    public virtual ICollection<UserRole> UserRoles { get; set; }
}

public class Permission
{
    public Guid Id { get; set; }
    public string Code { get; set; }  // e.g., "VOID_RECEIPT"
    public string Name { get; set; }
    public string Category { get; set; }
    public string Description { get; set; }
    public DeploymentMode? RequiredMode { get; set; }  // null = all modes
}
```

---

## 6. Core Modules (Always Active)

These modules are enabled regardless of deployment mode and form the foundation of the system.

### 6.1 Work Period Management

The work period defines a business day or shift. All transactions must occur within an active work period.

#### 6.1.1 Opening a Work Period

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| WP-001 | Only Manager or Administrator can open work period | Must Have |
| WP-002 | System prevents all sales when no work period is active | Must Have |
| WP-003 | Opening requires entering opening cash float amount | Must Have |
| WP-004 | System records opening timestamp, user, and terminal | Must Have |
| WP-005 | Option to carry forward previous day's closing balance | Should Have |
| WP-006 | Clear visual indicator when work period is active | Must Have |
| WP-007 | Support multiple concurrent work periods (shift overlap) | Should Have |
| WP-008 | Configurable work period duration limits | Could Have |

#### 6.1.2 During Work Period

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| WP-010 | All authorized users can log in and perform actions | Must Have |
| WP-011 | Real-time tracking of all transactions | Must Have |
| WP-012 | X Reports available at any time (non-destructive) | Must Have |
| WP-013 | Display work period duration on dashboard | Should Have |
| WP-014 | Cash drops (remove excess cash) with logging | Should Have |
| WP-015 | Paid in/Paid out for non-sale cash movements | Should Have |

#### 6.1.3 Closing a Work Period

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| WP-020 | Only Manager or Administrator can close | Must Have |
| WP-021 | Warning if unsettled receipts exist (hospitality) | Must Have |
| WP-022 | Require physical cash count entry | Must Have |
| WP-023 | Calculate and display cash variance | Must Have |
| WP-024 | Automatically generate Z Report | Must Have |
| WP-025 | Print Z Report and summary | Must Have |
| WP-026 | Lock all transactions for closed period | Must Have |
| WP-027 | Send Z Report to cloud on sync | Must Have |
| WP-028 | Queue eTIMS Z-report submission | Must Have |

### 6.2 Product Management

#### 6.2.1 Product Information

| Field | Type | Hospitality | Retail | Priority |
|-------|------|:-----------:|:------:|----------|
| Product ID | GUID | ✓ | ✓ | Must Have |
| SKU/Code | String | ✓ | ✓ | Must Have |
| Barcode (EAN/UPC) | String | Optional | ✓ | Must Have |
| PLU Code | String | ✗ | ✓ | Must Have |
| Name | String | ✓ | ✓ | Must Have |
| Short Name (Receipt) | String | ✓ | ✓ | Should Have |
| Description | Text | ✓ | ✓ | Should Have |
| Category | FK | ✓ | ✓ | Must Have |
| Subcategory | FK | ✓ | ✓ | Should Have |
| Selling Price | Decimal | ✓ | ✓ | Must Have |
| Cost Price | Decimal | ✓ | ✓ | Must Have |
| Tax Category | FK | ✓ | ✓ | Must Have |
| Unit of Measure | Enum | ✓ | ✓ | Must Have |
| Is Weighable | Bool | ✗ | ✓ | Must Have |
| Product Image | Blob/URL | ✓ | Optional | Should Have |
| Min Stock Level | Decimal | ✓ | ✓ | Should Have |
| Max Stock Level | Decimal | ✓ | ✓ | Should Have |
| Is Active | Bool | ✓ | ✓ | Must Have |
| Track Inventory | Bool | ✓ | ✓ | Must Have |
| Allow Price Override | Bool | ✓ | ✓ | Should Have |
| Supplier | FK | ✓ | ✓ | Should Have |

#### 6.2.2 Units of Measure

| Unit | Code | Used For |
|------|------|----------|
| Each | EA | Individual items |
| Kilogram | KG | Weighed produce, meat |
| Gram | G | Small quantities |
| Liter | L | Beverages, liquids |
| Milliliter | ML | Small beverages |
| Pack | PK | Multi-item packs |
| Box | BX | Bulk items |
| Dozen | DZ | Eggs, etc. |
| Bottle | BTL | Beverages |
| Can | CAN | Canned goods |

#### 6.2.3 Product Categories

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| PC-001 | Unlimited category creation | Must Have |
| PC-002 | Hierarchical subcategories (3 levels max) | Should Have |
| PC-003 | Category image/icon for POS display | Should Have |
| PC-004 | Category display order (drag-and-drop) | Must Have |
| PC-005 | Category-based tax assignment | Should Have |
| PC-006 | Category-based printer routing (hospitality) | Should Have |
| PC-007 | Category-based reporting | Should Have |

#### 6.2.4 Tax Categories (Kenya)

| Tax Category | Rate | Description | Examples |
|--------------|------|-------------|----------|
| Standard VAT | 16% | Default for most goods | Electronics, clothing |
| VAT Exempt | 0% | Basic necessities | Unprocessed food, medical |
| Zero-Rated | 0% | Exports, EPZ | Exported goods |
| Excise Duty | Variable | Specific products | Alcohol, tobacco |

### 6.3 Inventory Management

#### 6.3.1 Stock Tracking

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| INV-001 | Real-time stock quantity per location | Must Have |
| INV-002 | Automatic stock deduction on sale | Must Have |
| INV-003 | Stock return on void/refund | Must Have |
| INV-004 | Multiple stock locations (store, backroom, warehouse) | Should Have |
| INV-005 | Negative stock prevention (configurable) | Should Have |
| INV-006 | Stock valuation (FIFO, weighted average) | Should Have |
| INV-007 | Stock movement history log | Must Have |

#### 6.3.2 Stock Alerts

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| INV-010 | Low stock alerts (at/below minimum) | Must Have |
| INV-011 | Out-of-stock alerts and product blocking | Must Have |
| INV-012 | Dashboard notification for critical stock | Should Have |
| INV-013 | Auto-86 items when stock = 0 | Should Have |
| INV-014 | Expiry date alerts (days before) | Must Have (Retail) |
| INV-015 | Overstock alerts (above maximum) | Could Have |

#### 6.3.3 Stock Adjustments

| Adjustment Type | Code | Description |
|-----------------|------|-------------|
| Waste | WASTE | Spoiled, expired |
| Damage | DAMAGE | Damaged goods |
| Theft/Shrinkage | THEFT | Unknown loss |
| Internal Use | INTERNAL | Staff meals, samples |
| Correction | CORRECTION | Data entry fix |
| Opening Stock | OPENING | Initial stock entry |
| Stock Take | STOCKTAKE | Physical count adjustment |

#### 6.3.4 Purchase Receiving

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| INV-020 | Receive against purchase order | Should Have |
| INV-021 | Direct receiving without PO | Must Have |
| INV-022 | Record received quantities | Must Have |
| INV-023 | Record actual cost prices | Must Have |
| INV-024 | Partial receiving support | Should Have |
| INV-025 | Automatic stock update on receive | Must Have |
| INV-026 | Goods Received Note (GRN) generation | Must Have |
| INV-027 | Barcode scanning for receiving | Should Have |
| INV-028 | Batch/lot number capture | Should Have |
| INV-029 | Expiry date capture | Must Have |

#### 6.3.5 Stock Take

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| INV-030 | Create stock take sessions | Must Have |
| INV-031 | Enter physical count quantities | Must Have |
| INV-032 | Calculate variance (system vs. physical) | Must Have |
| INV-033 | Variance value calculation at cost | Must Have |
| INV-034 | Approve and apply adjustments | Must Have |
| INV-035 | Stock take reports | Must Have |
| INV-036 | Partial stock takes (by category/location) | Should Have |
| INV-037 | Barcode scanner support for counting | Should Have |
| INV-038 | Mobile device support for counting | Could Have |

### 6.4 Payment Processing

#### 6.4.1 Supported Payment Methods

| Payment Method | Code | Hospitality | Retail | Priority |
|----------------|------|:-----------:|:------:|----------|
| Cash | CASH | ✓ | ✓ | Must Have |
| M-Pesa (STK Push) | MPESA | ✓ | ✓ | Must Have |
| M-Pesa (Manual/Till) | MPESA_MANUAL | ✓ | ✓ | Must Have |
| Airtel Money | AIRTEL | ✓ | ✓ | Should Have |
| Credit/Debit Card | CARD | ✓ | ✓ | Should Have |
| Room Charge | ROOM | ✓ (Hotels) | ✗ | Should Have |
| Customer Account | ACCOUNT | ✓ | ✓ | Should Have |
| Loyalty Points | POINTS | ✓ | ✓ | Should Have |
| Voucher/Gift Card | VOUCHER | ✓ | ✓ | Should Have |
| Credit Note | CREDIT_NOTE | ✓ | ✓ | Should Have |
| Split Payment | SPLIT | ✓ | ✓ | Must Have |

#### 6.4.2 Cash Payment

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| PAY-001 | Display total amount prominently | Must Have |
| PAY-002 | Quick amount buttons (exact, round up) | Should Have |
| PAY-003 | Change calculation and display | Must Have |
| PAY-004 | Automatic cash drawer open | Must Have |
| PAY-005 | Cash in different denominations tracking | Could Have |
| PAY-006 | Forced cash count at shift end | Must Have |

#### 6.4.3 Mobile Money (M-Pesa/Airtel)

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| PAY-010 | STK Push initiation (auto-prompt) | Must Have |
| PAY-011 | Manual transaction code entry (fallback) | Must Have |
| PAY-012 | Transaction code validation format | Should Have |
| PAY-013 | Real-time payment confirmation | Must Have |
| PAY-014 | Timeout handling (30 seconds) | Must Have |
| PAY-015 | Retry mechanism for failed pushes | Should Have |
| PAY-016 | Payment confirmation on receipt | Must Have |

#### 6.4.4 Card Payment

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| PAY-020 | Integration with card terminal | Should Have |
| PAY-021 | Record last 4 digits and approval code | Should Have |
| PAY-022 | Support contactless payments | Should Have |
| PAY-023 | Tip entry support (hospitality) | Should Have |

#### 6.4.5 Split Payment

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| PAY-030 | Multiple payment methods per transaction | Must Have |
| PAY-031 | Show remaining balance during split | Must Have |
| PAY-032 | Support 2+ payment types | Must Have |
| PAY-033 | Receipt shows all payment methods | Must Have |

### 6.5 Receipt Management

#### 6.5.1 Receipt Lifecycle

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

| Status | Description |
|--------|-------------|
| CREATED | Order entered, not yet printed/submitted |
| PENDING | Order printed, awaiting payment (hospitality) |
| SETTLED | Payment received, transaction complete |
| VOIDED | Cancelled transaction |
| REFUNDED | Full or partial refund processed |

#### 6.5.2 Receipt Operations

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| RCP-001 | Unique sequential receipt number | Must Have |
| RCP-002 | KRA-compliant receipt format | Must Have |
| RCP-003 | QR code for eTIMS verification | Must Have |
| RCP-004 | Reprint capability | Must Have |
| RCP-005 | Email/SMS receipt option | Should Have |
| RCP-006 | Void with reason (manager authorization) | Must Have |
| RCP-007 | Void audit trail | Must Have |
| RCP-008 | Refund processing | Should Have |

#### 6.5.3 Receipt Content

| Section | Content |
|---------|---------|
| Header | Business name, address, KRA PIN, eTIMS CU number |
| Transaction Info | Receipt #, date/time, cashier, terminal |
| Items | Description, qty, unit price, line total |
| Subtotals | Subtotal, discounts, tax breakdown |
| Totals | Grand total, payment method(s), change |
| Footer | QR code, "Powered by", return policy |

### 6.6 Customer Management

#### 6.6.1 Customer Data

| Field | Type | Description | Priority |
|-------|------|-------------|----------|
| Customer ID | GUID | System identifier | Must Have |
| Phone Number | String | Primary identifier (Kenya) | Must Have |
| Full Name | String | Customer name | Must Have |
| Email | String | Digital receipts, marketing | Should Have |
| ID/Passport | String | For credit accounts | Could Have |
| Customer Type | Enum | Retail, Wholesale, VIP | Should Have |
| Tax PIN | String | For B2B invoicing | Should Have |
| Credit Limit | Decimal | Account customers | Should Have |
| Loyalty Number | String | Loyalty card number | Should Have |
| Date of Birth | Date | Birthday promotions | Could Have |
| Notes | Text | Special instructions | Could Have |

#### 6.6.2 Customer Lookup

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| CUS-001 | Search by phone number (primary) | Must Have |
| CUS-002 | Search by loyalty number | Should Have |
| CUS-003 | Search by name | Should Have |
| CUS-004 | Auto-lookup from M-Pesa payment | Should Have |
| CUS-005 | Quick customer creation at POS | Must Have |
| CUS-006 | Merge duplicate customers | Should Have |

#### 6.6.3 Purchase History

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| CUS-010 | View all transactions by customer | Must Have |
| CUS-011 | Total spend calculation | Should Have |
| CUS-012 | Average basket value | Should Have |
| CUS-013 | Frequently purchased items | Could Have |
| CUS-014 | Last visit date | Should Have |

### 6.7 Reporting Foundation

#### 6.7.1 X Report (Mid-Period)

Non-destructive snapshot of current period activity:

| Section | Details |
|---------|---------|
| Header | Business name, date/time, report number, user |
| Sales Summary | Gross sales, discounts, net sales, tax collected |
| Sales by Category | Breakdown per product category |
| Sales by Payment | Cash, M-Pesa, Card, etc. with totals |
| Sales by User | Individual totals per staff member |
| Transaction Stats | Count, average value |
| Voids | Count, value, reasons (if any) |

#### 6.7.2 Z Report (End-of-Period)

Final report that closes the period:

| Section | Details |
|---------|---------|
| Header | Business name, date, Z-number (sequential), closing user |
| Period Info | Open time, close time, duration |
| **Sales Summary** | |
| - Gross Sales | Total before discounts |
| - Discounts | Total discounts applied |
| - Net Sales | After discounts |
| - Tax Breakdown | By tax category (16%, 0%) |
| - Grand Total | Final collected amount |
| **By User** | Per-user sales total, transaction count |
| **By Payment** | Cash, M-Pesa, Card breakdown |
| **Voids** | Count, value, details, reasons |
| **Cash Drawer** | Opening, sales, payouts, expected, actual, variance |
| **eTIMS Summary** | Submitted invoices, pending, failed |

### 6.8 Audit Trail

#### 6.8.1 Logged Events

| Category | Events |
|----------|--------|
| Authentication | Login, logout, failed attempts, lockouts |
| Transactions | Create, modify, void, refund, settle |
| Payments | All payment attempts, successes, failures |
| Inventory | Adjustments, receiving, transfers, stock takes |
| Products | Create, edit, delete, price changes |
| Users | Create, edit, delete, role changes, password resets |
| System | Work period open/close, settings changes, sync events |
| Overrides | All permission overrides with authorizer |
| eTIMS | Submissions, responses, retries, failures |

#### 6.8.2 Audit Log Structure

```csharp
public class AuditLog
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid UserId { get; set; }
    public Guid? AuthorizingUserId { get; set; }  // For overrides
    public string Action { get; set; }
    public string Category { get; set; }
    public string EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string OldValues { get; set; }  // JSON
    public string NewValues { get; set; }  // JSON
    public string IpAddress { get; set; }
    public string TerminalId { get; set; }
    public Guid? StoreId { get; set; }
    public Guid WorkPeriodId { get; set; }
    public bool IsSynced { get; set; }
}
```

---

## 7. Hospitality Modules

These modules are enabled when `DeploymentMode = HOSPITALITY` or `HYBRID`.

### 7.1 Table Management

#### 7.1.1 Floor Plan Configuration

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| TBL-001 | Visual floor plan designer (drag-and-drop) | Should Have |
| TBL-002 | Multiple floors/sections (Ground, Rooftop, VIP) | Should Have |
| TBL-003 | Table shapes (round, square, rectangular, bar) | Should Have |
| TBL-004 | Table capacity (number of seats) | Must Have |
| TBL-005 | Table numbering/naming | Must Have |
| TBL-006 | Section/zone definition (Smoking, Non-smoking, Outdoor) | Could Have |
| TBL-007 | Clone floor plan for similar layouts | Could Have |

#### 7.1.2 Table Status

| Status | Color | Description |
|--------|-------|-------------|
| Available | Green | Ready for seating |
| Occupied | Red | Currently in use |
| Reserved | Yellow | Future reservation |
| Dirty | Orange | Needs cleaning |
| Blocked | Gray | Out of service |

#### 7.1.3 Table Operations

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| TBL-010 | Visual status indicators on floor plan | Must Have |
| TBL-011 | Show assigned server per table | Should Have |
| TBL-012 | Show current bill amount per table | Must Have |
| TBL-013 | Table timer (time since seated) | Should Have |
| TBL-014 | Transfer table to another server | Should Have |
| TBL-015 | Merge tables for large parties | Should Have |
| TBL-016 | Quick table status change | Must Have |

### 7.2 Order Workflow

#### 7.2.1 Order Creation

```
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│ Select Table │────▶│  Add Items   │────▶│ Send to      │
│              │     │              │     │ Kitchen      │
└──────────────┘     └──────────────┘     └──────────────┘
                            │                    │
                            ▼                    ▼
                     ┌──────────────┐     ┌──────────────┐
                     │ Add Modifiers│     │ Print KOT    │
                     │ and Notes    │     │              │
                     └──────────────┘     └──────────────┘
```

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| ORD-001 | Associate order with table | Must Have |
| ORD-002 | Associate order with covers (guest count) | Should Have |
| ORD-003 | Support multiple open orders per table (per seat/guest) | Could Have |
| ORD-004 | Add items to existing order | Must Have |
| ORD-005 | Only order owner can modify (or manager override) | Must Have |
| ORD-006 | Only NEW items sent to kitchen on additions | Must Have |
| ORD-007 | Hold order (save without sending) | Should Have |
| ORD-008 | Fire/rush order to kitchen | Should Have |

#### 7.2.2 Course Management

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| ORD-010 | Assign items to courses (Starter, Main, Dessert) | Should Have |
| ORD-011 | Manual course fire (send next course) | Should Have |
| ORD-012 | Automatic course timing | Could Have |
| ORD-013 | Course status tracking | Should Have |

#### 7.2.3 Item Modifiers

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| MOD-001 | Modifier groups (Size, Temperature, Add-ons) | Must Have |
| MOD-002 | Required vs. optional modifiers | Should Have |
| MOD-003 | Price adjustments for modifiers | Must Have |
| MOD-004 | Free-text special instructions | Must Have |
| MOD-005 | Common modifiers quick buttons (No Ice, Extra Sauce) | Should Have |

### 7.3 Kitchen Display System (KDS)

#### 7.3.1 Order Display

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| KDS-001 | Real-time order display | Should Have |
| KDS-002 | Color-coded order status (New, In Progress, Ready) | Should Have |
| KDS-003 | Order age timer (color changes with age) | Should Have |
| KDS-004 | Touch to bump (mark complete) | Should Have |
| KDS-005 | Recall bumped orders | Should Have |
| KDS-006 | Priority/rush order highlighting | Should Have |

#### 7.3.2 Station Routing

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| KDS-010 | Route by product category | Should Have |
| KDS-011 | Multiple stations (Hot Kitchen, Cold, Bar, Dessert) | Should Have |
| KDS-012 | All-call display (expo station) | Should Have |
| KDS-013 | Station-specific printers | Should Have |

### 7.4 Kitchen Printing

#### 7.4.1 Kitchen Order Ticket (KOT)

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| KOT-001 | Print to designated kitchen printers | Must Have |
| KOT-002 | Large, clear font for kitchen environment | Must Have |
| KOT-003 | Include modifiers and special instructions | Must Have |
| KOT-004 | Table number prominently displayed | Must Have |
| KOT-005 | Server name and timestamp | Must Have |
| KOT-006 | Course indication | Should Have |
| KOT-007 | New items highlighted when adding to order | Must Have |
| KOT-008 | Void tickets clearly marked | Must Have |

#### 7.4.2 Printer Routing

| Station | Categories | Printer |
|---------|------------|---------|
| Hot Kitchen | Main Dishes, Appetizers | Kitchen Printer 1 |
| Cold Kitchen | Salads, Cold Appetizers | Kitchen Printer 2 |
| Bar | Beverages, Cocktails | Bar Printer |
| Dessert | Desserts, Coffee | Dessert Printer |
| Expo | All Items | Expo Printer |

### 7.5 Bill Management

#### 7.5.1 Bill Splitting

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| SPL-001 | Split bill by items (drag items to new bill) | Must Have |
| SPL-002 | Split equally by number of guests | Should Have |
| SPL-003 | Split by seat number | Could Have |
| SPL-004 | Each split bill can have different payment | Must Have |
| SPL-005 | Original bill maintains reference to splits | Must Have |
| SPL-006 | Audit trail for all split operations | Must Have |

**Split Example:**
```
Original Bill: KSh 3,000 (Beer, Pizza, Salad)
           │
    ┌──────┼──────┐
    ▼      ▼      ▼
 Bill A  Bill B  Bill C
 Beer    Pizza   Salad
 KSh 500 KSh 1,500 KSh 1,000
 (Cash)  (M-Pesa) (Card)
```

#### 7.5.2 Bill Merging

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| MRG-001 | Merge multiple open bills into one | Should Have |
| MRG-002 | Only pending/unsettled bills can merge | Must Have |
| MRG-003 | Merged bill shows all items from sources | Must Have |
| MRG-004 | Source bills archived with reference | Must Have |

#### 7.5.3 Service Charge

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| SVC-001 | Configurable service charge percentage | Should Have |
| SVC-002 | Apply automatically based on rules | Should Have |
| SVC-003 | Manual service charge override | Should Have |
| SVC-004 | Service charge on receipt as separate line | Must Have |
| SVC-005 | Tax on service charge (configurable) | Should Have |

### 7.6 Room Charge (Hotels)

#### 7.6.1 PMS Integration

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| ROOM-001 | Search guest by room number | Must Have |
| ROOM-002 | Verify guest name before posting | Must Have |
| ROOM-003 | Check guest checkout status | Should Have |
| ROOM-004 | Post charge to guest folio | Must Have |
| ROOM-005 | Real-time or batch posting options | Should Have |
| ROOM-006 | Charge limit enforcement | Should Have |

#### 7.6.2 Supported PMS Systems

| PMS | Integration Method | Priority |
|-----|-------------------|----------|
| Opera (Oracle) | API | Should Have |
| Fidelio | API | Could Have |
| Protel | API | Could Have |
| Custom/Generic | File Export | Should Have |

### 7.7 Reservations

#### 7.7.1 Reservation Creation

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| RES-001 | Create reservation with date/time | Should Have |
| RES-002 | Guest name and contact | Must Have |
| RES-003 | Party size (covers) | Must Have |
| RES-004 | Table preference/assignment | Should Have |
| RES-005 | Special requests/notes | Should Have |
| RES-006 | Confirmation number generation | Should Have |
| RES-007 | SMS/Email confirmation | Could Have |

#### 7.7.2 Reservation Management

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| RES-010 | Calendar view of reservations | Should Have |
| RES-011 | Modify/cancel reservations | Must Have |
| RES-012 | Waitlist management | Could Have |
| RES-013 | No-show tracking | Should Have |
| RES-014 | Reservation status (Confirmed, Seated, No-Show) | Should Have |

### 7.8 Recipe and Ingredient Costing

#### 7.8.1 Recipe Management

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| REC-001 | Define recipes for menu items | Should Have |
| REC-002 | Link raw ingredients to finished products | Should Have |
| REC-003 | Ingredient quantities with units | Must Have |
| REC-004 | Recipe costing (auto-calculate from ingredients) | Should Have |
| REC-005 | Yield/portion management | Could Have |
| REC-006 | Sub-recipes (e.g., sauces) | Could Have |

#### 7.8.2 Ingredient Deduction

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| REC-010 | Automatic ingredient deduction on sale | Should Have |
| REC-011 | Batch ingredient adjustment for prep | Should Have |
| REC-012 | Ingredient usage reports | Should Have |

---

## 8. Retail/Supermarket Modules

These modules are enabled when `DeploymentMode = RETAIL` or `HYBRID`.

### 8.1 High-Speed Checkout

#### 8.1.1 Barcode Scanning

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| BAR-001 | Support UPC-A (12 digit) barcodes | Must Have |
| BAR-002 | Support EAN-13 (13 digit) barcodes | Must Have |
| BAR-003 | Support Code 128 barcodes | Should Have |
| BAR-004 | Support QR codes (for promotions/coupons) | Should Have |
| BAR-005 | Auto-detect barcode type | Must Have |
| BAR-006 | Keyboard wedge scanner support | Must Have |
| BAR-007 | Serial scanner support | Should Have |
| BAR-008 | Manual barcode entry fallback | Must Have |
| BAR-009 | Unknown barcode handling (search prompt) | Must Have |

#### 8.1.2 Checkout Speed Features

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| CHK-001 | Instant item lookup on scan (<100ms) | Must Have |
| CHK-002 | Quantity multiplier (e.g., 3 × item) | Must Have |
| CHK-003 | Quick item lookup by PLU code | Must Have |
| CHK-004 | Quick item lookup by name search | Must Have |
| CHK-005 | Favorite/frequent items grid | Should Have |
| CHK-006 | Suspend and resume transactions | Must Have |
| CHK-007 | Multiple suspended transactions | Should Have |
| CHK-008 | Target checkout time: <30 seconds | KPI |

### 8.2 Scale Integration

#### 8.2.1 Supported Scale Types

| Scale Type | Use Case | Connection |
|------------|----------|------------|
| POS-integrated scanner/scale | Checkout counter | USB/RS-232 |
| Standalone label-printing scale | Produce section | Network/USB |
| Deli/butchery counter scale | Service departments | RS-232 |
| Price computing scale | Self-service produce | RS-232/USB |

#### 8.2.2 Scale Requirements

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| SCL-001 | Direct weight reading from integrated scale | Must Have |
| SCL-002 | Auto-populate weight field at checkout | Must Have |
| SCL-003 | Tare weight handling (container deduction) | Should Have |
| SCL-004 | Zero/calibration function | Should Have |
| SCL-005 | Weight stability indicator | Should Have |
| SCL-006 | Multi-unit support (kg, g, lb) | Should Have |
| SCL-007 | Configurable scale protocols (Mettler, CAS, etc.) | Must Have |

#### 8.2.3 PLU Code Management

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| PLU-001 | 4-5 digit PLU code assignment | Must Have |
| PLU-002 | PLU quick-key buttons on POS | Must Have |
| PLU-003 | PLU lookup at scale terminals | Should Have |
| PLU-004 | PLU code import/export | Should Have |
| PLU-005 | Department/category-based PLU ranges | Could Have |

#### 8.2.4 Random Weight Barcode Handling

```
EAN-13 Random Weight Format:
02 XXXXX YYYYY C
│  │     │     └─ Check digit
│  │     └─────── Price in cents (5 digits)
│  └───────────── PLU/Item code (5 digits)
└──────────────── Prefix for in-store/random weight

UPC-A Random Weight Format:
2 XXXXX YYYYY C
│ │     │     └─ Check digit
│ │     └─────── Price (5 digits, KSh format)
│ └───────────── PLU code (5 digits)
└────────────── Prefix for random weight
```

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| RWB-001 | Parse price-embedded barcodes | Must Have |
| RWB-002 | Configurable prefix (02, 2, 20-29) | Must Have |
| RWB-003 | Validate check digit | Should Have |
| RWB-004 | Calculate unit price from embedded price | Must Have |
| RWB-005 | Support both price and weight embedding | Should Have |

### 8.3 Advanced Promotions Engine

#### 8.3.1 Promotion Types

| Promotion Type | Example | Priority |
|----------------|---------|----------|
| Percentage Discount | 10% off all beverages | Must Have |
| Fixed Amount Discount | KSh 50 off on cooking oil | Must Have |
| Buy X Get Y Free | Buy 2 Get 1 Free | Must Have |
| Buy X Get Y % Off | Buy 2 Get 50% off 3rd item | Should Have |
| Mix & Match | Any 3 items for KSh 500 | Must Have |
| Bundle Pricing | Bread + Milk = KSh 150 | Should Have |
| Quantity Breaks | 1-5: KSh 100 each, 6+: KSh 90 each | Should Have |
| Threshold Discount | Spend KSh 5,000, get 5% off total | Should Have |
| Time-Based | Happy Hour: 4-6pm 20% off | Should Have |
| Member-Only | Loyalty members get extra 5% | Must Have |
| First-Time Buyer | 10% off first purchase | Could Have |
| Coupon Redemption | Enter code or scan barcode | Should Have |

#### 8.3.2 Promotion Rules Engine

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| PRM-001 | Date range validity (start/end dates) | Must Have |
| PRM-002 | Day-of-week restrictions | Should Have |
| PRM-003 | Time-of-day restrictions | Should Have |
| PRM-004 | Minimum purchase requirements | Should Have |
| PRM-005 | Maximum redemption limits (per customer, total) | Should Have |
| PRM-006 | Customer eligibility rules (member, new, all) | Must Have |
| PRM-007 | Store-specific vs. chain-wide promotions | Must Have |
| PRM-008 | Product/category inclusion/exclusion | Must Have |
| PRM-009 | Stackable vs. exclusive promotions | Should Have |
| PRM-010 | Priority ordering for multiple promotions | Should Have |
| PRM-011 | Automatic best-deal selection for customer | Should Have |

#### 8.3.3 Promotion Execution

```csharp
public class PromotionEngine
{
    public async Task<List<AppliedPromotion>> EvaluateCart(
        Cart cart,
        Customer customer,
        DateTime transactionTime)
    {
        var eligiblePromotions = await GetEligiblePromotions(
            cart, customer, transactionTime);

        var appliedPromotions = new List<AppliedPromotion>();

        foreach (var promo in eligiblePromotions.OrderByDescending(p => p.Priority))
        {
            if (promo.IsExclusive && appliedPromotions.Any())
                continue;

            var result = await promo.Evaluate(cart);
            if (result.IsApplicable)
            {
                appliedPromotions.Add(result);
                cart.ApplyDiscount(result.Discount);
            }
        }

        return appliedPromotions;
    }
}
```

### 8.4 Loyalty Program

#### 8.4.1 Membership

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| LOY-001 | Phone number as primary identifier | Must Have |
| LOY-002 | Loyalty card number support | Should Have |
| LOY-003 | Quick enrollment at POS | Must Have |
| LOY-004 | Customer profile management | Should Have |
| LOY-005 | Membership tiers (Bronze, Silver, Gold, Platinum) | Should Have |
| LOY-006 | Tier upgrade/downgrade rules | Should Have |

#### 8.4.2 Points System

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| LOY-010 | Points earning rate (e.g., KSh 100 = 1 point) | Must Have |
| LOY-011 | Category-specific earning rates | Should Have |
| LOY-012 | Bonus points campaigns | Should Have |
| LOY-013 | Points expiry (configurable, e.g., 12 months) | Should Have |
| LOY-014 | Points redemption as payment | Must Have |
| LOY-015 | Redemption rate (e.g., 100 points = KSh 50) | Must Have |
| LOY-016 | Minimum points for redemption | Should Have |
| LOY-017 | Points statement/history | Should Have |

#### 8.4.3 Loyalty Benefits

| Benefit Type | Description |
|--------------|-------------|
| Member Pricing | Exclusive lower prices |
| Points Multipliers | Double points days |
| Birthday Rewards | Free item or bonus points |
| Tier Benefits | Increasing discounts by tier |
| Early Access | New product promotions |
| Personalized Offers | Based on purchase history |

#### 8.4.4 Kenya-Specific Considerations

| Consideration | Implementation |
|---------------|----------------|
| Phone as ID | Primary lookup method (254XXXXXXXXX) |
| SMS Notifications | Points earned, balance, offers |
| M-Pesa Integration | Identify customer from payment |
| Low Smartphone | Don't rely solely on app |
| USSD Option | Balance check via USSD |

### 8.5 Multi-Store Headquarters (HQ)

#### 8.5.1 Centralized Product Management

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| HQ-001 | Central product master database | Must Have |
| HQ-002 | Push products to all stores | Must Have |
| HQ-003 | Store-specific product overrides | Should Have |
| HQ-004 | Central barcode management | Must Have |
| HQ-005 | Bulk product import/export (Excel, CSV) | Must Have |
| HQ-006 | Product approval workflow | Could Have |

#### 8.5.2 Centralized Pricing

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| HQ-010 | Set prices centrally for all stores | Must Have |
| HQ-011 | Regional/zone pricing | Should Have |
| HQ-012 | Store-specific price overrides | Should Have |
| HQ-013 | Scheduled price changes | Should Have |
| HQ-014 | Price change approval workflow | Could Have |
| HQ-015 | Margin protection rules | Should Have |

#### 8.5.3 Centralized Promotions

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| HQ-020 | Create chain-wide promotions | Must Have |
| HQ-021 | Regional promotions | Should Have |
| HQ-022 | Store-specific promotions | Should Have |
| HQ-023 | Promotion deployment to stores | Must Have |
| HQ-024 | Promotion performance tracking | Should Have |

#### 8.5.4 Consolidated Reporting

| Report Type | Description |
|-------------|-------------|
| Chain Sales Dashboard | Real-time sales across all stores |
| Store Comparison | Sales, margin, basket size by store |
| Product Performance | Chain-wide product analysis |
| Inventory Overview | Stock levels across all locations |
| Staff Performance | Cashier metrics by store |
| Promotion Analysis | Campaign effectiveness |
| Shrinkage Report | Loss by store/category |

### 8.6 Central Purchasing

#### 8.6.1 Supplier Management

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| PUR-001 | Central supplier database | Must Have |
| PUR-002 | Supplier contact and terms | Must Have |
| PUR-003 | Supplier product catalog | Should Have |
| PUR-004 | Supplier performance tracking | Should Have |
| PUR-005 | Multiple suppliers per product | Should Have |

#### 8.6.2 Purchase Orders

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| PUR-010 | Central purchase order creation | Must Have |
| PUR-011 | Auto-generate PO from reorder points | Should Have |
| PUR-012 | PO approval workflow | Should Have |
| PUR-013 | Direct-to-store delivery | Must Have |
| PUR-014 | Distribution center receiving | Should Have |
| PUR-015 | Cross-docking support | Could Have |

#### 8.6.3 Receiving Workflows

```
Direct Store Delivery:              Distribution Center:
Supplier → Store                    Supplier → DC → Store

┌──────────┐    ┌──────────┐       ┌──────────┐    ┌────┐    ┌──────────┐
│ Supplier │───▶│  Store   │       │ Supplier │───▶│ DC │───▶│  Store   │
└──────────┘    └──────────┘       └──────────┘    └────┘    └──────────┘
     │               │                  │            │            │
     │               ▼                  │            ▼            ▼
     │         ┌──────────┐             │       ┌────────┐   ┌────────┐
     │         │   GRN    │             │       │  GRN   │   │  GRN   │
     │         └──────────┘             │       │  (DC)  │   │(Store) │
     │               │                  │       └────────┘   └────────┘
     └───────────────┴──────────────────┴──────────────────────────────
```

### 8.7 Stock Transfers

#### 8.7.1 Transfer Workflow

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| TRF-001 | Request transfer from store/warehouse | Must Have |
| TRF-002 | Transfer approval workflow | Should Have |
| TRF-003 | Pick and pack at source location | Should Have |
| TRF-004 | In-transit tracking | Should Have |
| TRF-005 | Receiving confirmation at destination | Must Have |
| TRF-006 | Variance handling (short/over) | Should Have |
| TRF-007 | Transfer document printing | Should Have |

#### 8.7.2 Transfer Types

| Type | Description |
|------|-------------|
| Store-to-Store | Direct transfer between stores |
| Warehouse-to-Store | Distribution center replenishment |
| Store-to-Warehouse | Returns or consolidation |
| Emergency Transfer | Rush transfer for stockouts |

### 8.8 Self-Checkout (Optional)

#### 8.8.1 Self-Checkout Features

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| SCO-001 | Customer-operated scanning interface | Should Have |
| SCO-002 | Integrated scale for produce | Should Have |
| SCO-003 | PLU lookup with pictures | Should Have |
| SCO-004 | Payment terminal integration | Should Have |
| SCO-005 | Age verification alerts (alcohol) | Must Have |
| SCO-006 | Weight verification (security) | Should Have |
| SCO-007 | Attendant override station | Must Have |
| SCO-008 | Receipt printing | Must Have |

#### 8.8.2 Security Measures

| Measure | Description |
|---------|-------------|
| Bagging area scale | Verify item weight after scan |
| Video monitoring | Camera at each station |
| Random audits | Staff spot-checks |
| Item limits | Max items per transaction |
| Restricted items | Require attendant for alcohol |

### 8.9 Shelf Label Printing

#### 8.9.1 Label Types

| Label Type | Use Case |
|------------|----------|
| Standard shelf label | Regular price display |
| Promotional label | Sale/special offer |
| Clearance label | Markdown items |
| Multi-buy label | Buy X for Y deals |
| Unit price label | Price per kg/liter |

#### 8.9.2 Label Content

| Field | Description |
|-------|-------------|
| Product Name | Item description |
| Price | Current selling price |
| Unit Price | Price per unit of measure |
| Barcode | Product barcode |
| Promotion | Current offer details |
| Date | Price effective date |

#### 8.9.3 Label Requirements

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| LBL-001 | Batch label printing by category | Should Have |
| LBL-002 | Single label reprint | Must Have |
| LBL-003 | Label format templates | Should Have |
| LBL-004 | ZPL/EPL printer support | Must Have |
| LBL-005 | Price change auto-triggers print | Should Have |

### 8.10 Batch and Expiry Tracking

#### 8.10.1 Batch Management

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| BTH-001 | Batch/lot number recording on receiving | Must Have |
| BTH-002 | Expiry date recording | Must Have |
| BTH-003 | FIFO/FEFO inventory management | Should Have |
| BTH-004 | Batch traceability (supplier to sale) | Should Have |
| BTH-005 | Batch recall functionality | Should Have |

#### 8.10.2 Expiry Alerts

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| EXP-001 | Dashboard alerts for expiring items | Must Have |
| EXP-002 | Configurable alert thresholds (7, 14, 30 days) | Should Have |
| EXP-003 | Markdown recommendations | Should Have |
| EXP-004 | Expired item blocking at POS | Should Have |
| EXP-005 | Expiry waste reporting | Must Have |

---

## 9. Kenya Compliance and Integrations

### 9.1 KRA eTIMS Integration (Mandatory)

#### 9.1.1 Background

The Kenya Revenue Authority (KRA) mandates electronic tax invoice management for all VAT-registered businesses through the **Electronic Tax Invoice Management System (eTIMS)**. This replaced the earlier TIMS system.

| Milestone | Date |
|-----------|------|
| TIMS Regulation Effective | August 2021 |
| Initial Compliance Deadline | August 2022 |
| eTIMS Replacement | 2024 |
| M-Pesa Virtual Tax Register | December 2024 |

#### 9.1.2 eTIMS Requirements

| Requirement | Description |
|-------------|-------------|
| Real-time Transmission | Invoices sent to KRA in real-time or near-real-time |
| Control Unit Number (CUN) | Unique device identifier assigned by KRA |
| Invoice Numbering | Sequential, tamper-proof numbering |
| Buyer PIN | Customer's KRA PIN on invoice (if provided) |
| QR Code | Verification code on every receipt |
| Digital Signature | Transaction authentication |
| Audit Trail | Complete, immutable transaction history |

#### 9.1.3 Invoice Fields (KRA Compliant)

| Field | Requirement | Example |
|-------|-------------|---------|
| Seller PIN | KRA PIN of business | P051234567X |
| Seller Name | Registered business name | ABC Supermarket Ltd |
| Seller Address | Physical address | Westlands, Nairobi |
| Buyer PIN | Customer's KRA PIN (optional) | A001234567B |
| Buyer Name | Customer name (if PIN provided) | John Doe |
| Invoice Number | Sequential, unique | INV-2025-000001 |
| Invoice Date/Time | ISO 8601 format | 2025-12-28T14:30:00 |
| Item Description | Product/service name | Bread - 500g |
| Quantity | Amount sold | 2 |
| Unit Price | Price per unit (excl VAT) | KSh 50.00 |
| Tax Code | VAT category | A (16%), B (0%) |
| Tax Amount | VAT per line | KSh 16.00 |
| Total Amount | Line total | KSh 116.00 |
| Control Code | eTIMS verification | ABC123DEF456 |
| CU Serial | Control Unit serial | CU-1234567890 |
| QR Code | Scannable verification | [QR Data] |

#### 9.1.4 eTIMS Integration Flow

```
┌───────────────────────────────────────────────────────────────────┐
│                        eTIMS INTEGRATION FLOW                      │
└───────────────────────────────────────────────────────────────────┘

  POS Terminal                  Local Queue                 KRA eTIMS
       │                            │                           │
       │  1. Create Transaction     │                           │
       │─────────────────────────►  │                           │
       │                            │                           │
       │  2. Generate Invoice       │                           │
       │  (with temp Control Code)  │                           │
       │◄─────────────────────────  │                           │
       │                            │                           │
       │  3. Print Receipt          │                           │
       │  (shows "Pending eTIMS")   │                           │
       │                            │                           │
       │                            │  4. Submit to eTIMS       │
       │                            │─────────────────────────► │
       │                            │                           │
       │                            │  5. eTIMS Response        │
       │                            │  (Control Code, QR)       │
       │                            │◄───────────────────────── │
       │                            │                           │
       │  6. Update Invoice         │                           │
       │  (with real Control Code)  │                           │
       │◄─────────────────────────  │                           │
       │                            │                           │
       │  7. Optional: Reprint      │                           │
       │  Receipt with QR Code      │                           │
       │                            │                           │

OFFLINE SCENARIO:
       │                            │                           │
       │  Transaction created       │                           │
       │  (stored locally)          │                           │
       │                            │                           │
       │  Receipt printed           │   [NO CONNECTION]         │
       │  (shows "eTIMS Pending")   │                           │
       │                            │                           │
       │  ... Internet restored ... │                           │
       │                            │                           │
       │                            │  Sync Service submits     │
       │                            │  queued transactions      │
       │                            │─────────────────────────► │
       │                            │                           │
```

#### 9.1.5 eTIMS API Integration

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| ETM-001 | Direct REST API integration with KRA servers | Must Have |
| ETM-002 | OAuth 2.0 authentication | Must Have |
| ETM-003 | Control Unit registration/activation | Must Have |
| ETM-004 | Invoice submission endpoint | Must Have |
| ETM-005 | Credit note submission | Must Have |
| ETM-006 | Invoice query/verification | Should Have |
| ETM-007 | Retry mechanism for failed submissions | Must Have |
| ETM-008 | Offline queue with automatic sync | Must Have |
| ETM-009 | eTIMS status dashboard | Should Have |
| ETM-010 | Failed invoice alerts | Must Have |

#### 9.1.6 eTIMS Error Handling

| Error Type | Handling |
|------------|----------|
| Network Timeout | Queue for retry (max 24 hours) |
| Invalid Data | Flag for manual review |
| Duplicate Invoice | Skip (already submitted) |
| CU Inactive | Alert administrator |
| Rate Limit | Exponential backoff |

### 9.2 M-Pesa Integration (Daraja API)

#### 9.2.1 M-Pesa Market Position

- **~83% mobile money market share** in Kenya
- **Primary payment method** for most consumers
- **Essential** for any retail/hospitality POS in Kenya

#### 9.2.2 Integration Methods

| Method | Description | Priority |
|--------|-------------|----------|
| STK Push (Lipa na M-Pesa) | Auto-prompt on customer phone | Must Have |
| Manual Till/Paybill | Customer initiates, manual entry | Must Have |
| C2B API | Real-time payment confirmation | Must Have |

#### 9.2.3 STK Push Flow

```
┌───────────────────────────────────────────────────────────────────┐
│                     M-PESA STK PUSH FLOW                           │
└───────────────────────────────────────────────────────────────────┘

  Cashier            POS System           Daraja API         Customer Phone
     │                   │                    │                    │
     │ 1. Select M-Pesa  │                    │                    │
     │ Enter phone #     │                    │                    │
     │──────────────────►│                    │                    │
     │                   │                    │                    │
     │                   │ 2. STK Push Request│                    │
     │                   │───────────────────►│                    │
     │                   │                    │                    │
     │                   │                    │ 3. Push Payment    │
     │                   │                    │ Prompt             │
     │                   │                    │───────────────────►│
     │                   │                    │                    │
     │                   │                    │                    │ 4. Customer
     │                   │                    │                    │ Enters PIN
     │                   │                    │                    │
     │                   │                    │ 5. PIN Validated   │
     │                   │                    │◄───────────────────│
     │                   │                    │                    │
     │                   │ 6. Callback with   │                    │
     │                   │ ResultCode         │                    │
     │                   │◄───────────────────│                    │
     │                   │                    │                    │
     │ 7. Payment        │                    │                    │
     │ Confirmed         │                    │                    │
     │◄──────────────────│                    │                    │
     │                   │                    │                    │
     │ 8. Print Receipt  │                    │                    │
     │ with M-Pesa Ref   │                    │                    │
     │                   │                    │                    │
```

#### 9.2.4 Daraja API Requirements

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| MPS-001 | Safaricom Developer Portal registration | Must Have |
| MPS-002 | Business shortcode (Paybill/Till) | Must Have |
| MPS-003 | Consumer Key and Secret management | Must Have |
| MPS-004 | Passkey for Lipa na M-Pesa | Must Have |
| MPS-005 | OAuth token management (refresh) | Must Have |
| MPS-006 | STK Push initiation | Must Have |
| MPS-007 | Transaction status query | Must Have |
| MPS-008 | Callback URL handling | Must Have |
| MPS-009 | Timeout handling (30 seconds) | Must Have |
| MPS-010 | Transaction reversal | Should Have |

#### 9.2.5 M-Pesa Data Model

```csharp
public class MPesaTransaction
{
    public Guid Id { get; set; }
    public string MerchantRequestId { get; set; }
    public string CheckoutRequestId { get; set; }
    public decimal Amount { get; set; }
    public string PhoneNumber { get; set; }  // 254XXXXXXXXX
    public string AccountReference { get; set; }  // Receipt number
    public string TransactionDesc { get; set; }

    // Response fields
    public int? ResultCode { get; set; }
    public string ResultDesc { get; set; }
    public string MpesaReceiptNumber { get; set; }
    public DateTime? TransactionDate { get; set; }
    public string PhoneNumberUsed { get; set; }

    // Status
    public MPesaStatus Status { get; set; }
    public DateTime InitiatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Links
    public Guid TransactionId { get; set; }
    public Guid? CustomerId { get; set; }
}

public enum MPesaStatus
{
    Initiated,
    Pending,
    Completed,
    Failed,
    Cancelled,
    TimedOut
}
```

### 9.3 Airtel Money Integration

| Requirement ID | Description | Priority |
|----------------|-------------|----------|
| AIR-001 | Airtel Money API registration | Should Have |
| AIR-002 | Payment push initiation | Should Have |
| AIR-003 | Manual entry fallback | Should Have |
| AIR-004 | Transaction confirmation | Should Have |
| AIR-005 | Callback handling | Should Have |

### 9.4 Kenya Tax Configuration

#### 9.4.1 Tax Rates

| Tax Category | Code | Rate | Description | Examples |
|--------------|------|------|-------------|----------|
| Standard VAT | A | 16% | Default rate | Electronics, clothing, processed food |
| VAT Exempt | B | 0% | Exempt from VAT | Unprocessed food, medical supplies |
| Zero-Rated | C | 0% | Zero-rated supplies | Exports, supplies to EPZ |
| Excise Duty | E | Variable | Specific products | Alcohol, tobacco, fuel |

#### 9.4.2 VAT Exempt Items (Common)

| Category | Examples |
|----------|----------|
| Basic Foodstuffs | Unprocessed maize, wheat, rice, beans |
| Agricultural Inputs | Seeds, fertilizers, pesticides |
| Medical | Medicines, medical equipment |
| Educational | Books, school supplies |
| Financial Services | Banking services, insurance |

#### 9.4.3 Tax Calculation

```csharp
public class KenyaTaxCalculator : ITaxCalculator
{
    public TaxBreakdown Calculate(decimal amount, TaxCategory category)
    {
        return category switch
        {
            TaxCategory.StandardVAT => new TaxBreakdown
            {
                ExclusiveAmount = Math.Round(amount / 1.16m, 2),
                TaxAmount = Math.Round(amount - (amount / 1.16m), 2),
                TaxRate = 0.16m,
                TaxCode = "A"
            },
            TaxCategory.VATExempt or TaxCategory.ZeroRated => new TaxBreakdown
            {
                ExclusiveAmount = amount,
                TaxAmount = 0,
                TaxRate = 0,
                TaxCode = category == TaxCategory.VATExempt ? "B" : "C"
            },
            _ => throw new ArgumentException($"Unknown tax category: {category}")
        };
    }
}
```

### 9.5 Currency and Localization

#### 9.5.1 Kenya Shilling (KES)

| Setting | Value |
|---------|-------|
| Currency Code | KES |
| Currency Symbol | KSh |
| Symbol Position | Before amount (KSh 1,000) |
| Decimal Places | 2 (but typically rounded) |
| Thousand Separator | Comma (,) |
| Decimal Separator | Period (.) |
| Rounding | Nearest shilling (common practice) |

#### 9.5.2 Rounding Rules

| Scenario | Rule | Example |
|----------|------|---------|
| Cash Payment | Round to nearest KSh | KSh 99.50 → KSh 100 |
| M-Pesa Payment | Exact amount | KSh 99.50 → KSh 99.50 |
| Card Payment | Exact amount | KSh 99.50 → KSh 99.50 |
| Display | 2 decimal places | KSh 99.50 |

#### 9.5.3 Localization Settings

| Setting | Value |
|---------|-------|
| Primary Language | English |
| Secondary Language | Swahili (Kiswahili) |
| Date Format | DD/MM/YYYY |
| Time Format | 24-hour (HH:mm) or 12-hour |
| Number Format | 1,000.00 |
| Phone Format | +254 XXX XXX XXX |
| Address Format | [Street], [Area], [City] |

### 9.6 Compliance Reporting

#### 9.6.1 eTIMS Reports

| Report | Frequency | Purpose |
|--------|-----------|---------|
| Daily Sales Summary | Daily | Reconciliation with eTIMS |
| VAT Report | Monthly | VAT return preparation |
| Failed Submissions | Real-time | Identify pending invoices |
| Control Unit Status | On-demand | CU health check |

#### 9.6.2 KRA Integration Status Dashboard

| Metric | Display |
|--------|---------|
| Submitted Today | Count and value |
| Pending | Count (should be 0) |
| Failed | Count with details |
| Last Sync | Timestamp |
| CU Status | Active/Inactive |
| API Health | Green/Yellow/Red |

---

## 10. Offline-First Architecture and Cloud Sync

### 10.1 Design Philosophy

The system is designed for **offline-first operation**, recognizing that internet connectivity in Kenya can be unreliable. All core POS operations must work without internet, with data synchronized when connectivity is available.

#### 10.1.1 Offline Capabilities

| Operation | Offline Support | Notes |
|-----------|:---------------:|-------|
| Create transactions | ✓ | Full support |
| Process cash payments | ✓ | Full support |
| Process M-Pesa (manual entry) | ✓ | Verify later |
| Print receipts | ✓ | eTIMS pending status |
| View products/prices | ✓ | Local database |
| Manage inventory | ✓ | Local updates |
| Run X/Z reports | ✓ | Local data |
| User login | ✓ | Cached credentials |
| STK Push (M-Pesa) | ✗ | Requires internet |
| eTIMS submission | Queue | Syncs when online |
| Cloud reporting | ✗ | Requires internet |
| Multi-store sync | Queue | Syncs when online |

### 10.2 Local Database Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    STORE SERVER / POS TERMINAL                   │
│                                                                   │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │                  SQL SERVER EXPRESS (Local)                  │ │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │ │
│  │  │  Products   │  │Transactions │  │   Users     │         │ │
│  │  │  Inventory  │  │  Payments   │  │   Roles     │         │ │
│  │  │  Categories │  │  Receipts   │  │   Audit     │         │ │
│  │  └─────────────┘  └─────────────┘  └─────────────┘         │ │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │ │
│  │  │  Customers  │  │  Sync Queue │  │  Settings   │         │ │
│  │  │   Loyalty   │  │  Conflicts  │  │   Config    │         │ │
│  │  └─────────────┘  └─────────────┘  └─────────────┘         │ │
│  └─────────────────────────────────────────────────────────────┘ │
│                              │                                    │
│                              │ Local Operations                   │
│                              ▼                                    │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │                     POS APPLICATION                          │ │
│  └─────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                               │
                               │ Sync (when online)
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                         CLOUD LAYER                              │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │                  AZURE SQL DATABASE (Central)                │ │
│  │     Consolidated data from all stores for HQ reporting       │ │
│  └─────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

### 10.3 Sync Engine

#### 10.3.1 Sync Queue Management

```csharp
public class SyncQueue
{
    public Guid Id { get; set; }
    public string EntityType { get; set; }    // "Transaction", "Product", etc.
    public Guid EntityId { get; set; }
    public SyncOperation Operation { get; set; }  // Create, Update, Delete
    public string Payload { get; set; }       // JSON serialized entity
    public DateTime CreatedAt { get; set; }
    public int RetryCount { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public string LastError { get; set; }
    public SyncStatus Status { get; set; }
    public int Priority { get; set; }         // Higher = more urgent
}

public enum SyncStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Conflict
}

public enum SyncOperation
{
    Create,
    Update,
    Delete,
    Upsert
}
```

#### 10.3.2 Sync Priority

| Entity Type | Priority | Sync Frequency |
|-------------|----------|----------------|
| Transactions (Sales) | High (10) | Real-time when online |
| eTIMS Invoices | Critical (20) | Immediate |
| M-Pesa Payments | Critical (20) | Immediate |
| Stock Adjustments | High (10) | Real-time |
| Customer Updates | Medium (5) | Every 5 minutes |
| Product Changes (from HQ) | Medium (5) | Every 5 minutes |
| Audit Logs | Low (1) | Batch (hourly) |

#### 10.3.3 Sync Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                        SYNC ENGINE FLOW                          │
└─────────────────────────────────────────────────────────────────┘

    Local Change                                          Cloud
         │                                                  │
         │  1. Record change in local DB                    │
         │  2. Add to SyncQueue (Status: Pending)           │
         │                                                  │
         │                  ┌──────────────┐                │
         │                  │ Check Online │                │
         │                  └──────┬───────┘                │
         │                         │                        │
         │         ┌───────────────┴───────────────┐       │
         │         ▼                               ▼       │
         │   ┌──────────┐                   ┌──────────┐   │
         │   │ OFFLINE  │                   │  ONLINE  │   │
         │   └────┬─────┘                   └────┬─────┘   │
         │        │                              │         │
         │        │ Wait for                     │         │
         │        │ connectivity                 │         │
         │        │                              ▼         │
         │        │                    ┌─────────────────┐ │
         │        │                    │ Process Queue   │ │
         │        │                    │ (by priority)   │ │
         │        │                    └────────┬────────┘ │
         │        │                             │          │
         │        │                             ▼          │
         │        │                    ┌─────────────────┐ │
         │        │                    │ Send to Cloud   │─┼──────►│
         │        │                    └────────┬────────┘ │       │
         │        │                             │          │       │
         │        │               ┌─────────────┴─────────┐│       │
         │        │               ▼                       ▼│       │
         │        │        ┌───────────┐           ┌──────────┐    │
         │        │        │  Success  │           │  Failure │    │
         │        │        └─────┬─────┘           └────┬─────┘    │
         │        │              │                      │          │
         │        │              ▼                      ▼          │
         │        │      Mark Completed          Increment Retry   │
         │        │      Remove from Queue       Schedule Retry    │
         │        │                              (exponential      │
         │        │                               backoff)         │
```

#### 10.3.4 Conflict Resolution

| Conflict Type | Resolution Strategy |
|---------------|---------------------|
| Same record updated | Last-write-wins with timestamp |
| Price changed locally and centrally | Central wins, flag for review |
| Customer merged | Keep both, flag duplicate |
| Stock quantity mismatch | Alert for manual resolution |
| Transaction already exists | Skip (idempotent) |

```csharp
public class ConflictResolution
{
    public async Task<ResolvedChange> Resolve(
        SyncConflict conflict,
        ConflictStrategy strategy)
    {
        return strategy switch
        {
            ConflictStrategy.LocalWins => conflict.LocalValue,
            ConflictStrategy.RemoteWins => conflict.RemoteValue,
            ConflictStrategy.LastWriteWins =>
                conflict.LocalTimestamp > conflict.RemoteTimestamp
                    ? conflict.LocalValue
                    : conflict.RemoteValue,
            ConflictStrategy.ManualReview =>
                await FlagForManualReview(conflict),
            ConflictStrategy.Merge =>
                await AttemptMerge(conflict),
            _ => throw new NotSupportedException()
        };
    }
}
```

### 10.4 Real-Time Sync (SignalR)

#### 10.4.1 SignalR Hub

```csharp
public class SyncHub : Hub
{
    public async Task JoinStoreGroup(string storeId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, storeId);
    }

    public async Task BroadcastProductUpdate(ProductDto product)
    {
        await Clients.Group(product.StoreId)
            .SendAsync("ProductUpdated", product);
    }

    public async Task BroadcastPriceChange(PriceChangeDto priceChange)
    {
        await Clients.Group(priceChange.StoreId)
            .SendAsync("PriceChanged", priceChange);
    }

    public async Task BroadcastPromotionUpdate(PromotionDto promotion)
    {
        await Clients.All.SendAsync("PromotionUpdated", promotion);
    }
}
```

#### 10.4.2 Client-Side Handling

```csharp
public class SyncHubClient
{
    private readonly HubConnection _connection;

    public async Task StartAsync()
    {
        _connection.On<ProductDto>("ProductUpdated", async product =>
        {
            await _productService.UpdateLocalProduct(product);
            await _uiRefreshService.RefreshProductGrid();
        });

        _connection.On<PriceChangeDto>("PriceChanged", async change =>
        {
            await _productService.UpdateLocalPrice(change);
            await _uiRefreshService.RefreshPrices();
        });

        await _connection.StartAsync();
    }

    public async Task ReconnectAsync()
    {
        // Automatic reconnection with exponential backoff
        await _connection.StartAsync();
        await SyncPendingChanges();
    }
}
```

### 10.5 Data Synchronization Patterns

#### 10.5.1 Master Data (HQ → Stores)

| Data Type | Direction | Trigger |
|-----------|-----------|---------|
| Products | HQ → Store | On change, scheduled |
| Prices | HQ → Store | On change, immediate |
| Promotions | HQ → Store | On change |
| Categories | HQ → Store | On change |
| Users (central) | HQ → Store | On change |

#### 10.5.2 Transactional Data (Stores → HQ)

| Data Type | Direction | Trigger |
|-----------|-----------|---------|
| Transactions | Store → HQ | Real-time when online |
| Payments | Store → HQ | Real-time when online |
| Stock Movements | Store → HQ | Real-time when online |
| Audit Logs | Store → HQ | Batch (hourly) |
| Z Reports | Store → HQ | On close |

### 10.6 Offline Mode Indicators

| Indicator | Location | Purpose |
|-----------|----------|---------|
| Connection Status | Header bar | Green/Yellow/Red icon |
| Pending Sync Count | Status bar | Number of queued items |
| Last Sync Time | Status bar | "Last synced: 5 min ago" |
| eTIMS Status | Receipt | "eTIMS: Pending" or QR code |
| Offline Mode Alert | Dashboard | Warning when offline >1 hour |

### 10.7 Recovery Procedures

#### 10.7.1 Connection Lost During Transaction

| Scenario | Handling |
|----------|----------|
| Cash payment | Complete locally, queue sync |
| M-Pesa STK Push | Fall back to manual entry |
| Card payment | Depends on terminal (offline mode) |
| eTIMS submission | Queue for later |

#### 10.7.2 Extended Offline Period

| Duration | Actions |
|----------|---------|
| < 1 hour | Normal operation, sync on reconnect |
| 1-4 hours | Warning alert, prioritize critical syncs |
| 4-24 hours | Manager notification, batch sync mode |
| > 24 hours | Escalation, possible manual intervention |

---

## 11. Database Design

### 11.1 Entity Relationship Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        CORE ENTITY RELATIONSHIPS                             │
└─────────────────────────────────────────────────────────────────────────────┘

    ┌──────────┐         ┌──────────┐         ┌──────────┐
    │   User   │────────▶│UserRole  │◀────────│   Role   │
    └──────────┘         └──────────┘         └──────────┘
         │                                          │
         │                                          ▼
         │                                    ┌──────────────┐
         │                                    │RolePermission│
         │                                    └──────────────┘
         │                                          │
         ▼                                          ▼
    ┌──────────┐                             ┌──────────────┐
    │WorkPeriod│                             │  Permission  │
    └──────────┘                             └──────────────┘
         │
         ▼
    ┌──────────────┐         ┌──────────────┐         ┌──────────────┐
    │ Transaction  │────────▶│TransactionItem│◀────────│   Product    │
    └──────────────┘         └──────────────┘         └──────────────┘
         │                                                   │
         │                                                   ▼
         ▼                                            ┌──────────────┐
    ┌──────────────┐                                  │   Category   │
    │   Payment    │                                  └──────────────┘
    └──────────────┘
         │
         ▼
    ┌──────────────┐         ┌──────────────┐
    │MPesaPayment  │         │  ETIMSInvoice│
    └──────────────┘         └──────────────┘
```

### 11.2 Core Tables

#### 11.2.1 Store and Configuration

```sql
CREATE TABLE Stores (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code NVARCHAR(20) NOT NULL UNIQUE,
    Name NVARCHAR(100) NOT NULL,
    Address NVARCHAR(500),
    City NVARCHAR(100),
    PhoneNumber NVARCHAR(20),
    Email NVARCHAR(100),
    KRAPinNumber NVARCHAR(20) NOT NULL,
    ETIMSControlUnitSerial NVARCHAR(50),
    DeploymentMode NVARCHAR(20) NOT NULL, -- HOSPITALITY, RETAIL, HYBRID
    IsActive BIT NOT NULL DEFAULT 1,
    TimeZone NVARCHAR(50) DEFAULT 'Africa/Nairobi',
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2
);

CREATE TABLE Terminals (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    StoreId UNIQUEIDENTIFIER NOT NULL REFERENCES Stores(Id),
    TerminalNumber INT NOT NULL,
    Name NVARCHAR(50),
    IsActive BIT NOT NULL DEFAULT 1,
    LastHeartbeat DATETIME2,
    CONSTRAINT UQ_Terminal_Store UNIQUE (StoreId, TerminalNumber)
);
```

#### 11.2.2 Products and Inventory

```sql
CREATE TABLE Categories (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ParentCategoryId UNIQUEIDENTIFIER REFERENCES Categories(Id),
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    DisplayOrder INT NOT NULL DEFAULT 0,
    ImageUrl NVARCHAR(500),
    IsActive BIT NOT NULL DEFAULT 1,
    PrinterRouting NVARCHAR(50), -- For kitchen routing
    TaxCategoryId UNIQUEIDENTIFIER REFERENCES TaxCategories(Id),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE TABLE Products (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    SKU NVARCHAR(50) NOT NULL UNIQUE,
    Barcode NVARCHAR(50),
    PLUCode NVARCHAR(10),
    Name NVARCHAR(200) NOT NULL,
    ShortName NVARCHAR(50),
    Description NVARCHAR(1000),
    CategoryId UNIQUEIDENTIFIER NOT NULL REFERENCES Categories(Id),
    UnitOfMeasure NVARCHAR(10) NOT NULL, -- EA, KG, L, etc.
    SellingPrice DECIMAL(18,2) NOT NULL,
    CostPrice DECIMAL(18,2),
    TaxCategoryId UNIQUEIDENTIFIER NOT NULL REFERENCES TaxCategories(Id),
    IsWeighable BIT NOT NULL DEFAULT 0,
    TrackInventory BIT NOT NULL DEFAULT 1,
    AllowPriceOverride BIT NOT NULL DEFAULT 0,
    MinStockLevel DECIMAL(18,3),
    MaxStockLevel DECIMAL(18,3),
    ImageUrl NVARCHAR(500),
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2,
    SyncVersion ROWVERSION
);

CREATE TABLE ProductStock (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ProductId UNIQUEIDENTIFIER NOT NULL REFERENCES Products(Id),
    StoreId UNIQUEIDENTIFIER NOT NULL REFERENCES Stores(Id),
    LocationId UNIQUEIDENTIFIER REFERENCES StockLocations(Id),
    Quantity DECIMAL(18,3) NOT NULL DEFAULT 0,
    LastStockTakeDate DATETIME2,
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_ProductStock UNIQUE (ProductId, StoreId, LocationId)
);

CREATE TABLE ProductBatches (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ProductId UNIQUEIDENTIFIER NOT NULL REFERENCES Products(Id),
    StoreId UNIQUEIDENTIFIER NOT NULL REFERENCES Stores(Id),
    BatchNumber NVARCHAR(50) NOT NULL,
    ExpiryDate DATE,
    Quantity DECIMAL(18,3) NOT NULL,
    CostPrice DECIMAL(18,2),
    ReceivingDate DATETIME2 NOT NULL,
    SupplierId UNIQUEIDENTIFIER REFERENCES Suppliers(Id),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
```

#### 11.2.3 Transactions and Payments

```sql
CREATE TABLE Transactions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ReceiptNumber NVARCHAR(50) NOT NULL,
    StoreId UNIQUEIDENTIFIER NOT NULL REFERENCES Stores(Id),
    TerminalId UNIQUEIDENTIFIER NOT NULL REFERENCES Terminals(Id),
    WorkPeriodId UNIQUEIDENTIFIER NOT NULL REFERENCES WorkPeriods(Id),
    UserId UNIQUEIDENTIFIER NOT NULL REFERENCES Users(Id),
    CustomerId UNIQUEIDENTIFIER REFERENCES Customers(Id),
    TableId UNIQUEIDENTIFIER REFERENCES Tables(Id), -- Hospitality
    Status NVARCHAR(20) NOT NULL, -- CREATED, PENDING, SETTLED, VOIDED
    SubTotal DECIMAL(18,2) NOT NULL,
    DiscountAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    TaxAmount DECIMAL(18,2) NOT NULL,
    ServiceCharge DECIMAL(18,2) DEFAULT 0,
    TotalAmount DECIMAL(18,2) NOT NULL,
    PaidAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    ChangeAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    Notes NVARCHAR(500),
    VoidReason NVARCHAR(500),
    VoidedByUserId UNIQUEIDENTIFIER REFERENCES Users(Id),
    VoidedAt DATETIME2,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    SettledAt DATETIME2,
    SyncStatus NVARCHAR(20) NOT NULL DEFAULT 'Pending',
    SyncedAt DATETIME2,
    CONSTRAINT UQ_Receipt UNIQUE (StoreId, ReceiptNumber)
);

CREATE TABLE TransactionItems (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    TransactionId UNIQUEIDENTIFIER NOT NULL REFERENCES Transactions(Id),
    ProductId UNIQUEIDENTIFIER NOT NULL REFERENCES Products(Id),
    ProductName NVARCHAR(200) NOT NULL, -- Denormalized for history
    Quantity DECIMAL(18,3) NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    DiscountPercent DECIMAL(5,2) DEFAULT 0,
    DiscountAmount DECIMAL(18,2) DEFAULT 0,
    TaxRate DECIMAL(5,2) NOT NULL,
    TaxAmount DECIMAL(18,2) NOT NULL,
    LineTotal DECIMAL(18,2) NOT NULL,
    Modifiers NVARCHAR(MAX), -- JSON for modifiers
    Notes NVARCHAR(500),
    Course INT, -- For hospitality
    IsVoided BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE TABLE Payments (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    TransactionId UNIQUEIDENTIFIER NOT NULL REFERENCES Transactions(Id),
    PaymentMethod NVARCHAR(20) NOT NULL, -- CASH, MPESA, CARD, etc.
    Amount DECIMAL(18,2) NOT NULL,
    Reference NVARCHAR(100), -- M-Pesa code, card approval, etc.
    Status NVARCHAR(20) NOT NULL, -- PENDING, COMPLETED, FAILED
    ProcessedAt DATETIME2,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE TABLE MPesaTransactions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PaymentId UNIQUEIDENTIFIER NOT NULL REFERENCES Payments(Id),
    MerchantRequestId NVARCHAR(100),
    CheckoutRequestId NVARCHAR(100),
    PhoneNumber NVARCHAR(15) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    AccountReference NVARCHAR(50),
    ResultCode INT,
    ResultDesc NVARCHAR(500),
    MpesaReceiptNumber NVARCHAR(50),
    TransactionDate DATETIME2,
    Status NVARCHAR(20) NOT NULL,
    InitiatedAt DATETIME2 NOT NULL,
    CompletedAt DATETIME2
);
```

#### 11.2.4 eTIMS Integration

```sql
CREATE TABLE ETIMSInvoices (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    TransactionId UNIQUEIDENTIFIER NOT NULL REFERENCES Transactions(Id),
    InvoiceNumber NVARCHAR(50) NOT NULL,
    ControlUnitSerial NVARCHAR(50) NOT NULL,
    SellerPIN NVARCHAR(20) NOT NULL,
    BuyerPIN NVARCHAR(20),
    BuyerName NVARCHAR(200),
    TotalExclTax DECIMAL(18,2) NOT NULL,
    TotalTax DECIMAL(18,2) NOT NULL,
    TotalInclTax DECIMAL(18,2) NOT NULL,
    InvoiceDate DATETIME2 NOT NULL,
    ControlCode NVARCHAR(100),
    QRCode NVARCHAR(MAX),
    Status NVARCHAR(20) NOT NULL, -- PENDING, SUBMITTED, CONFIRMED, FAILED
    SubmittedAt DATETIME2,
    ConfirmedAt DATETIME2,
    ErrorMessage NVARCHAR(1000),
    RetryCount INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE TABLE ETIMSInvoiceItems (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ETIMSInvoiceId UNIQUEIDENTIFIER NOT NULL REFERENCES ETIMSInvoices(Id),
    ItemDescription NVARCHAR(200) NOT NULL,
    Quantity DECIMAL(18,3) NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    TaxCode CHAR(1) NOT NULL, -- A, B, C, E
    TaxRate DECIMAL(5,2) NOT NULL,
    TaxAmount DECIMAL(18,2) NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL
);
```

#### 11.2.5 Hospitality-Specific Tables

```sql
CREATE TABLE Tables (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    StoreId UNIQUEIDENTIFIER NOT NULL REFERENCES Stores(Id),
    FloorId UNIQUEIDENTIFIER REFERENCES Floors(Id),
    TableNumber NVARCHAR(20) NOT NULL,
    Name NVARCHAR(50),
    Capacity INT NOT NULL DEFAULT 4,
    Shape NVARCHAR(20), -- ROUND, SQUARE, RECTANGULAR
    Status NVARCHAR(20) NOT NULL DEFAULT 'Available',
    AssignedUserId UNIQUEIDENTIFIER REFERENCES Users(Id),
    CurrentTransactionId UNIQUEIDENTIFIER,
    PositionX INT,
    PositionY INT,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT UQ_Table UNIQUE (StoreId, TableNumber)
);

CREATE TABLE KitchenOrders (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    TransactionId UNIQUEIDENTIFIER NOT NULL REFERENCES Transactions(Id),
    StationId UNIQUEIDENTIFIER REFERENCES KitchenStations(Id),
    OrderNumber INT NOT NULL,
    Status NVARCHAR(20) NOT NULL, -- NEW, IN_PROGRESS, READY, SERVED
    Priority INT NOT NULL DEFAULT 0,
    Course INT,
    PrintedAt DATETIME2,
    StartedAt DATETIME2,
    CompletedAt DATETIME2,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
```

#### 11.2.6 Sync Management Tables

```sql
CREATE TABLE SyncQueue (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    EntityType NVARCHAR(50) NOT NULL,
    EntityId UNIQUEIDENTIFIER NOT NULL,
    Operation NVARCHAR(20) NOT NULL, -- CREATE, UPDATE, DELETE
    Payload NVARCHAR(MAX) NOT NULL,
    Priority INT NOT NULL DEFAULT 5,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Pending',
    RetryCount INT NOT NULL DEFAULT 0,
    MaxRetries INT NOT NULL DEFAULT 5,
    LastAttemptAt DATETIME2,
    LastError NVARCHAR(MAX),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ProcessedAt DATETIME2
);

CREATE TABLE SyncConflicts (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    EntityType NVARCHAR(50) NOT NULL,
    EntityId UNIQUEIDENTIFIER NOT NULL,
    LocalValue NVARCHAR(MAX) NOT NULL,
    RemoteValue NVARCHAR(MAX) NOT NULL,
    LocalTimestamp DATETIME2 NOT NULL,
    RemoteTimestamp DATETIME2 NOT NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Pending',
    ResolvedBy UNIQUEIDENTIFIER REFERENCES Users(Id),
    Resolution NVARCHAR(20),
    ResolvedAt DATETIME2,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
```

### 11.3 Indexing Strategy

```sql
-- Transaction Performance
CREATE INDEX IX_Transactions_StoreDate ON Transactions(StoreId, CreatedAt DESC);
CREATE INDEX IX_Transactions_WorkPeriod ON Transactions(WorkPeriodId);
CREATE INDEX IX_Transactions_Status ON Transactions(Status) WHERE Status != 'SETTLED';
CREATE INDEX IX_Transactions_Sync ON Transactions(SyncStatus) WHERE SyncStatus = 'Pending';

-- Product Lookup
CREATE INDEX IX_Products_Barcode ON Products(Barcode) WHERE Barcode IS NOT NULL;
CREATE INDEX IX_Products_PLU ON Products(PLUCode) WHERE PLUCode IS NOT NULL;
CREATE INDEX IX_Products_Category ON Products(CategoryId);
CREATE INDEX IX_Products_Active ON Products(IsActive) WHERE IsActive = 1;

-- Inventory
CREATE INDEX IX_ProductStock_Store ON ProductStock(StoreId, ProductId);
CREATE INDEX IX_ProductBatches_Expiry ON ProductBatches(ExpiryDate) WHERE Quantity > 0;

-- Sync Queue
CREATE INDEX IX_SyncQueue_Status ON SyncQueue(Status, Priority DESC, CreatedAt);

-- eTIMS
CREATE INDEX IX_ETIMSInvoices_Status ON ETIMSInvoices(Status) WHERE Status = 'PENDING';
CREATE INDEX IX_ETIMSInvoices_Date ON ETIMSInvoices(InvoiceDate DESC);
```

---

## 12. Reporting and Analytics

### 12.1 Report Categories

| Category | Reports | Mode |
|----------|---------|------|
| **Operational** | X Report, Z Report, Shift Summary | Both |
| **Sales** | Daily Sales, Sales by Category, Sales by Product | Both |
| **Inventory** | Stock Levels, Stock Movement, Expiry | Both |
| **Financial** | Payment Summary, VAT Report, Cash Reconciliation | Both |
| **Staff** | Cashier Performance, Sales by User | Both |
| **Customer** | Customer Sales, Loyalty Points, Top Customers | Both |
| **HQ/Chain** | Multi-Store Comparison, Consolidated Sales | Retail |
| **Hospitality** | Table Turnover, Server Performance, Kitchen Times | Hospitality |

### 12.2 Core Reports

#### 12.2.1 Daily Sales Report

| Section | Fields |
|---------|--------|
| Header | Store, Date, Generated By, Print Time |
| Summary | Gross Sales, Discounts, Net Sales, Tax, Grand Total |
| By Hour | Hourly breakdown of sales |
| By Category | Sales per product category |
| By Product | Top 20 products by revenue |
| By Payment | Cash, M-Pesa, Card, Other |
| Transactions | Count, Average Value |
| Voids | Count, Value, Percentage |

#### 12.2.2 Inventory Reports

| Report | Purpose | Key Metrics |
|--------|---------|-------------|
| Stock Levels | Current inventory | Qty, Value, Days of Stock |
| Stock Movement | Period activity | Received, Sold, Adjusted |
| Low Stock Alert | Reorder needs | Items below min level |
| Expiry Report | Expiring items | Days to expiry, Qty, Value |
| Shrinkage Report | Loss analysis | Variance, Reason codes |
| FIFO Valuation | Inventory value | Weighted average cost |

#### 12.2.3 VAT Report (Kenya)

| Section | Details |
|---------|---------|
| Period | Month/Quarter/Year |
| Taxable Sales | 16% VAT items total |
| Exempt Sales | VAT exempt items |
| Zero-Rated Sales | Export/EPZ items |
| Output VAT | Tax collected |
| eTIMS Summary | Submitted, Pending, Failed |

### 12.3 Retail-Specific Reports

#### 12.3.1 Multi-Store Dashboard

| Metric | Display |
|--------|---------|
| Total Sales (Chain) | Real-time aggregate |
| Sales by Store | Comparison chart |
| Top Performing Stores | Ranked list |
| Basket Analysis | Avg items, value per store |
| Promotion Performance | Redemption rates |
| Stock Alerts | Low stock across chain |

#### 12.3.2 Promotion Analysis

| Metric | Description |
|--------|-------------|
| Redemption Count | Times promotion applied |
| Discount Value | Total discount given |
| Incremental Sales | Additional revenue attributed |
| Basket Lift | Increase in basket size |
| Customer Reach | Unique customers |

### 12.4 Hospitality-Specific Reports

#### 12.4.1 Table Turnover Report

| Metric | Description |
|--------|-------------|
| Covers | Number of guests served |
| Turnover Rate | Average table turns per day |
| Average Check | Revenue per cover |
| Average Time | Duration per table |
| Peak Hours | Busiest times |

#### 12.4.2 Kitchen Performance

| Metric | Description |
|--------|-------------|
| Average Ticket Time | Order to ready |
| Orders by Station | Distribution |
| Rush Order % | Priority orders |
| Remake Rate | Items remade |

### 12.5 Report Generation

| Format | Use Case |
|--------|----------|
| Screen Display | Real-time viewing |
| Thermal Print | Shift-end reports |
| PDF Export | Email, archive |
| Excel Export | Further analysis |
| Scheduled Email | Daily summaries to management |

---

## 13. User Interface Requirements

### 13.1 Design Principles

| Principle | Description |
|-----------|-------------|
| Touch-First | Designed for touchscreen with large tap targets |
| Speed | Minimal taps to complete common actions |
| Clarity | Clear visual hierarchy, readable fonts |
| Feedback | Immediate visual/audio feedback on actions |
| Error Prevention | Confirmation for destructive actions |
| Offline Awareness | Clear indicators of sync status |

### 13.2 Screen Layouts

#### 13.2.1 Main POS Screen (Retail)

```
┌─────────────────────────────────────────────────────────────────┐
│ [Logo] Store Name          [User: Jane]  [Sync: ●] 14:32  [≡]  │
├─────────────────────────────────────────────────────────────────┤
│ ┌─────────────────────────────┐ ┌─────────────────────────────┐ │
│ │      BARCODE INPUT          │ │    PRODUCT SEARCH           │ │
│ │  [________________________] │ │  [________________________] │ │
│ └─────────────────────────────┘ └─────────────────────────────┘ │
├─────────────────────────────────┬───────────────────────────────┤
│                                 │                               │
│  TRANSACTION ITEMS              │   CATEGORY / PRODUCT GRID    │
│  ─────────────────────────────  │   ─────────────────────────  │
│  Bread 500g          x1   50.00│  [Bakery] [Dairy] [Produce]  │
│  Milk 500ml          x2  140.00│  [Meat]  [Frozen] [Drinks]   │
│  Sugar 2kg           x1  220.00│                               │
│  [Void Last]                    │  ┌─────┐ ┌─────┐ ┌─────┐    │
│                                 │  │Bread│ │Milk │ │Eggs │    │
│                                 │  │ 50  │ │ 70  │ │ 450 │    │
│                                 │  └─────┘ └─────┘ └─────┘    │
│  ─────────────────────────────  │                               │
│  Subtotal:            410.00    │  ┌─────┐ ┌─────┐ ┌─────┐    │
│  VAT (16%):            56.55    │  │Sugar│ │Rice │ │Flour│    │
│  TOTAL:           KSh 466.55    │  │ 220 │ │ 180 │ │ 120 │    │
│                                 │  └─────┘ └─────┘ └─────┘    │
├─────────────────────────────────┴───────────────────────────────┤
│ [HOLD] [CUSTOMER] [DISCOUNT] [WEIGHT]     [CLEAR] [PAY KSh 467]│
└─────────────────────────────────────────────────────────────────┘
```

#### 13.2.2 Payment Screen

```
┌─────────────────────────────────────────────────────────────────┐
│                         PAYMENT                                  │
│                    Total: KSh 466.55                            │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│   ┌───────────────┐  ┌───────────────┐  ┌───────────────┐      │
│   │               │  │               │  │               │      │
│   │     CASH      │  │    M-PESA     │  │     CARD      │      │
│   │               │  │               │  │               │      │
│   └───────────────┘  └───────────────┘  └───────────────┘      │
│                                                                  │
│   ┌───────────────┐  ┌───────────────┐  ┌───────────────┐      │
│   │    LOYALTY    │  │    SPLIT      │  │    CREDIT     │      │
│   │    POINTS     │  │   PAYMENT     │  │    NOTE       │      │
│   └───────────────┘  └───────────────┘  └───────────────┘      │
│                                                                  │
│                     ┌─────────────────┐                         │
│                     │     CANCEL      │                         │
│                     └─────────────────┘                         │
└─────────────────────────────────────────────────────────────────┘
```

### 13.3 Accessibility Requirements

| Requirement | Description |
|-------------|-------------|
| Font Size | Minimum 14pt, scalable up to 24pt |
| Color Contrast | WCAG AA compliant (4.5:1 ratio) |
| Touch Targets | Minimum 44x44 pixels |
| Audio Feedback | Optional beeps for scans, errors |
| High Contrast Mode | Alternative color scheme |

---

## 14. Hardware Requirements

### 14.1 POS Terminal Specifications

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| Processor | Intel Core i3 / AMD Ryzen 3 | Intel Core i5 / AMD Ryzen 5 |
| RAM | 8 GB | 16 GB |
| Storage | 256 GB SSD | 512 GB SSD |
| Display | 15" Touchscreen (1024x768) | 15.6" Touchscreen (1920x1080) |
| OS | Windows 10 Pro | Windows 11 Pro |
| Network | 100 Mbps Ethernet | Gigabit Ethernet + WiFi |
| USB Ports | 4 × USB 3.0 | 6 × USB 3.0 |
| Serial Ports | 1 × RS-232 (or USB adapter) | 2 × RS-232 |

### 14.2 Store Server Specifications

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| Processor | Intel Core i5 | Intel Xeon / Core i7 |
| RAM | 16 GB | 32 GB |
| Storage | 500 GB SSD + 1 TB HDD | 1 TB NVMe + 2 TB HDD |
| OS | Windows Server 2019 | Windows Server 2022 |
| Database | SQL Server Express | SQL Server Standard |
| Network | Gigabit Ethernet | Dual Gigabit + WiFi |
| UPS | 1000VA | 2000VA |

### 14.3 Peripherals

| Peripheral | Requirement | Connectivity |
|------------|-------------|--------------|
| Receipt Printer | 80mm thermal, ESC/POS | USB / Network |
| Barcode Scanner | 1D/2D, keyboard wedge | USB |
| Cash Drawer | Auto-open via printer | RJ-11 |
| Customer Display | 2-line VFD or LCD | USB / Serial |
| Kitchen Printer | Impact or thermal | Network |
| Label Printer | Zebra/compatible, ZPL | USB / Network |
| Scale | POS-integrated | USB / Serial |
| Card Terminal | EMV + contactless | USB / Network |

### 14.4 Network Requirements

| Requirement | Specification |
|-------------|---------------|
| LAN Speed | 100 Mbps minimum, 1 Gbps recommended |
| Internet | 5 Mbps minimum for sync and eTIMS |
| Latency | < 100ms to local server |
| WiFi (backup) | 802.11ac or later |
| VPN | For HQ connectivity (optional) |

---

## 15. Security and Compliance

### 15.1 Data Protection

| Requirement | Implementation |
|-------------|----------------|
| Password Storage | bcrypt with salt (work factor 12) |
| PIN Storage | SHA-256 with salt |
| Data in Transit | TLS 1.2+ for all API calls |
| Data at Rest | SQL Server TDE (optional) |
| PII Handling | Mask phone numbers, card numbers |
| Backup Encryption | AES-256 |

### 15.2 Access Control

| Control | Implementation |
|---------|----------------|
| Authentication | Username/Password + PIN |
| Session Management | Auto-logout after 15 min inactivity |
| Role-Based Access | Granular permissions per role |
| Audit Logging | All actions logged with user, timestamp |
| Failed Login Lockout | 5 attempts, 15 min lockout |

### 15.3 PCI-DSS Considerations

| Requirement | Approach |
|-------------|----------|
| Card Data | Never stored; use tokenization |
| Card Terminal | P2PE compliant terminal |
| Network Segmentation | Payment terminal isolated |
| Vulnerability Scanning | Quarterly scans |

### 15.4 Kenya Data Protection Act

| Requirement | Implementation |
|-------------|----------------|
| Consent | Customer consent for loyalty signup |
| Data Minimization | Collect only necessary data |
| Right to Access | Customer can request their data |
| Right to Erasure | Customer can request deletion |
| Data Localization | Primary data in Kenya datacenter |

---

## 16. Non-Functional Requirements

### 16.1 Performance

| Metric | Target |
|--------|--------|
| Barcode scan to item display | < 100ms |
| Transaction completion | < 2 seconds |
| Report generation (daily) | < 5 seconds |
| Application startup | < 10 seconds |
| Page navigation | < 500ms |
| Database query (typical) | < 200ms |
| Concurrent users per store | 20+ |
| Products supported | 100,000+ |

### 16.2 Reliability

| Metric | Target |
|--------|--------|
| System uptime | 99.9% during business hours |
| Mean Time Between Failures | > 2000 hours |
| Mean Time to Recovery | < 15 minutes |
| Data durability | 99.999% |
| Offline operation | Unlimited transactions |

### 16.3 Scalability

| Dimension | Capacity |
|-----------|----------|
| Products per store | 100,000+ |
| Transactions per day | 10,000+ per store |
| Concurrent terminals | 50+ per store |
| Stores per chain | 500+ |
| Historical data | 7+ years |

### 16.4 Usability

| Metric | Target |
|--------|--------|
| New cashier training | < 2 hours |
| Task completion rate | > 95% |
| Error rate | < 1% |
| User satisfaction | > 4.0/5.0 |

---

## 17. API Design

### 17.1 API Structure

| Endpoint Group | Purpose |
|----------------|---------|
| `/api/v1/auth` | Authentication |
| `/api/v1/products` | Product management |
| `/api/v1/transactions` | Sales transactions |
| `/api/v1/inventory` | Stock management |
| `/api/v1/customers` | Customer data |
| `/api/v1/reports` | Report generation |
| `/api/v1/sync` | Data synchronization |
| `/api/v1/etims` | eTIMS integration |
| `/api/v1/mpesa` | M-Pesa integration |

### 17.2 Authentication

```
POST /api/v1/auth/login
{
  "username": "cashier01",
  "password": "password123",
  "storeId": "uuid"
}

Response:
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "uuid",
  "expiresIn": 3600,
  "user": {
    "id": "uuid",
    "fullName": "John Doe",
    "roles": ["Cashier"]
  }
}
```

### 17.3 Sync API

```
POST /api/v1/sync/push
{
  "storeId": "uuid",
  "terminalId": "uuid",
  "changes": [
    {
      "entityType": "Transaction",
      "entityId": "uuid",
      "operation": "Create",
      "payload": { ... },
      "timestamp": "2025-12-28T14:30:00Z"
    }
  ]
}

Response:
{
  "results": [
    {
      "entityId": "uuid",
      "status": "Success"
    }
  ],
  "serverTimestamp": "2025-12-28T14:30:05Z"
}
```

---

## 18. Implementation Roadmap

### 18.1 Phase Overview

| Phase | Duration | Focus |
|-------|----------|-------|
| Phase 1 | 4 months | Core POS + Retail |
| Phase 2 | 3 months | Hospitality + KDS |
| Phase 3 | 3 months | Multi-Store HQ + Advanced Features |
| Phase 4 | 2 months | Polish + Deployment |

### 18.2 Phase 1: Core POS + Retail (Months 1-4)

| Milestone | Deliverables |
|-----------|--------------|
| M1.1 (Month 1) | Project setup, DB schema, User/Auth |
| M1.2 (Month 2) | Products, Categories, Basic POS UI |
| M1.3 (Month 3) | Transactions, Payments, Receipt printing |
| M1.4 (Month 4) | M-Pesa integration, eTIMS integration, X/Z Reports |

### 18.3 Phase 2: Hospitality + KDS (Months 5-7)

| Milestone | Deliverables |
|-----------|--------------|
| M2.1 (Month 5) | Table management, Floor plans |
| M2.2 (Month 6) | Kitchen printing, KDS |
| M2.3 (Month 7) | Bill split/merge, Modifiers, Service charge |

### 18.4 Phase 3: Multi-Store + Advanced (Months 8-10)

| Milestone | Deliverables |
|-----------|--------------|
| M3.1 (Month 8) | Sync engine, Cloud API |
| M3.2 (Month 9) | HQ portal, Multi-store management |
| M3.3 (Month 10) | Promotions engine, Loyalty program |

### 18.5 Phase 4: Polish + Deploy (Months 11-12)

| Milestone | Deliverables |
|-----------|--------------|
| M4.1 (Month 11) | Performance optimization, Testing |
| M4.2 (Month 12) | Pilot deployment, Training, Documentation |

---

## 19. Glossary

| Term | Definition |
|------|------------|
| **CU** | Control Unit - KRA-issued device identifier for eTIMS |
| **eTIMS** | Electronic Tax Invoice Management System (KRA) |
| **GRN** | Goods Received Note |
| **KDS** | Kitchen Display System |
| **KES** | Kenya Shilling (currency) |
| **KOT** | Kitchen Order Ticket |
| **KRA** | Kenya Revenue Authority |
| **M-Pesa** | Mobile money service by Safaricom |
| **POS** | Point of Sale |
| **PLU** | Price Look-Up code (for produce) |
| **PMS** | Property Management System (hotels) |
| **RBAC** | Role-Based Access Control |
| **SKU** | Stock Keeping Unit |
| **STK Push** | SIM Toolkit Push (M-Pesa payment prompt) |
| **UPC** | Universal Product Code (barcode) |
| **EAN** | European Article Number (barcode) |
| **VAT** | Value Added Tax (16% in Kenya) |
| **FIFO** | First In, First Out (inventory method) |
| **FEFO** | First Expired, First Out (inventory method) |

---

## Document Approval

| Role | Name | Signature | Date |
|------|------|-----------|------|
| Product Owner | | | |
| Technical Lead | | | |
| Project Manager | | | |
| QA Lead | | | |

---

## Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | Dec 2025 | AI Assistant | Initial comprehensive PRD |
| 2.0 | Dec 2025 | AI Assistant | Enhanced with full research integration |

---

*End of Document*
