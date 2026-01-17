# Architecture Document: Multi-Mode POS System

**Project:** Multi-Mode Point of Sale (POS) System
**Version:** 2.0
**Date:** December 2025
**Platform:** Windows Desktop Application (Touch-Enabled)
**Modes:** Restaurant/Hospitality | Supermarket/Retail | Hybrid

---

## 1. Executive Summary

This architecture document defines the technical foundation for the Multi-Mode POS System - a comprehensive point-of-sale solution that supports **both hospitality (restaurants, bars, hotels) and retail (supermarkets, shops) environments**. The system is built as a Windows desktop application using C# and MS SQL Server Express, optimized for touch-screen operation.

### 1.0 Business Mode Support

The system is designed to operate in three distinct modes, configurable at installation:

| Mode | Target Business | Key Features |
|------|-----------------|--------------|
| **Restaurant** | Hotels, Bars, Restaurants | Table management, waiter assignment, kitchen display, split bills |
| **Supermarket** | Retail stores, Supermarkets | Barcode scanning, product offers, supplier credit, fast checkout |
| **Hybrid** | Mixed operations | All features enabled, context-aware UI |

```
┌─────────────────────────────────────────────────────────────────────┐
│                    SYSTEM MODE CONFIGURATION                         │
├─────────────────────────────────────────────────────────────────────┤
│  Business Mode: [Restaurant ▼]  [Supermarket]  [Hybrid]             │
│                                                                      │
│  Restaurant Features:           Supermarket Features:                │
│  ☑ Table Management             ☑ Barcode Auto-Focus                │
│  ☑ Waiter Assignment            ☑ Product Offers/Promotions         │
│  ☑ Kitchen Display              ☑ Supplier Credit Management        │
│  ☑ Split Bills                  ☑ Employee/Payroll Module           │
│  ☑ Tab/Running Tickets          ☑ Accounting Module                 │
└─────────────────────────────────────────────────────────────────────┘
```

### 1.1 Key Architectural Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Language/Framework | C# 14 / .NET 10 (LTS) | Latest LTS release (Nov 2025), extension members, field-backed properties, 30-40% faster startup |
| UI Framework | WPF (Windows Presentation Foundation) | Touch-optimized, XAML flexibility, MVVM support, .NET 10 performance optimizations |
| Database | MS SQL Server Express 2022+ | Free tier, reliable, strong .NET integration, JSON column support |
| ORM | Entity Framework Core 10 | LeftJoin/RightJoin operators, named query filters, JSON mapping, ExecuteUpdateAsync improvements |
| Architecture Pattern | Layered Architecture + MVVM | Separation of concerns, testability |
| Printing | ESC/POS Protocol | Industry standard for thermal printers |

### 1.2 .NET 10 & C# 14 Benefits

**.NET 10 (LTS - Released November 11, 2025):**
- Long-term support through November 2028
- 30-40% faster startup vs .NET Framework 4.8
- Improved JIT inlining and method devirtualization
- Enhanced WPF font rendering, XAML parsing, and input composition

**C# 14 New Features Used:**
- **Extension members** - Extension properties and operators for cleaner service extensions
- **Field-backed properties** - `field` keyword for simpler property accessors in ViewModels
- **Partial constructors** - Better partial class organization in generated code
- **`nameof` with unbound generics** - Improved logging and error messages

**Entity Framework Core 10 Benefits:**
- **LeftJoin/RightJoin** - Explicit join operators for complex report queries
- **Named query filters** - Individual filter control (soft delete, tenant isolation)
- **JSON column mapping** - Native storage for audit data and settings
- **ExecuteUpdateAsync lambda** - Simpler bulk update operations
- **Primitive collection optimization** - Fixes large IN clause performance issues

---

## 2. System Architecture

### 2.1 High-Level Architecture Diagram

```
+------------------------------------------------------------------+
|                    PRESENTATION LAYER (WPF + MVVM)               |
|  +---------------+  +---------------+  +---------------+         |
|  |   POS View    |  |  Admin View   |  | Reports View  |         |
|  |  (MainWindow) |  | (BackOffice)  |  |  (Reports)    |         |
|  +-------+-------+  +-------+-------+  +-------+-------+         |
|          |                  |                  |                 |
|  +-------v------------------v------------------v-------+         |
|  |              VIEW MODELS (MVVM Pattern)             |         |
|  | POSViewModel | AdminViewModel | ReportsViewModel    |         |
|  +-----------------------------+------------------------+        |
+------------------------------------------------------------------+
                              |
                              v
+------------------------------------------------------------------+
|                    BUSINESS LOGIC LAYER                          |
|  +-------------+  +-------------+  +-------------+               |
|  |   Sales     |  | Inventory   |  |    User     |               |
|  |  Service    |  |  Service    |  |   Service   |               |
|  +-------------+  +-------------+  +-------------+               |
|  +-------------+  +-------------+  +-------------+               |
|  |  Payment    |  | Reporting   |  |   Audit     |               |
|  |  Service    |  |  Service    |  |   Service   |               |
|  +-------------+  +-------------+  +-------------+               |
|  +-------------+  +-------------+  +-------------+               |
|  | WorkPeriod  |  |  Receipt    |  |  Printing   |               |
|  |  Service    |  |  Service    |  |   Service   |               |
|  +-------------+  +-------------+  +-------------+               |
+------------------------------------------------------------------+
                              |
                              v
+------------------------------------------------------------------+
|                    DATA ACCESS LAYER                             |
|  +----------------------------------------------------------+   |
|  |              Entity Framework Core DbContext              |   |
|  |  +----------+  +----------+  +----------+  +----------+  |   |
|  |  | Orders   |  | Products |  |  Users   |  |Inventory |  |   |
|  |  |  Repo    |  |   Repo   |  |   Repo   |  |   Repo   |  |   |
|  |  +----------+  +----------+  +----------+  +----------+  |   |
|  |  +----------+  +----------+  +----------+  +----------+  |   |
|  |  | Receipts |  | Payments |  | Suppliers|  | AuditLog |  |   |
|  |  |   Repo   |  |   Repo   |  |   Repo   |  |   Repo   |  |   |
|  |  +----------+  +----------+  +----------+  +----------+  |   |
|  +----------------------------------------------------------+   |
+------------------------------------------------------------------+
                              |
                              v
+------------------------------------------------------------------+
|                    DATABASE LAYER                                |
|           +------------------------------------+                 |
|           |     MS SQL Server Express          |                 |
|           |     +------------------------+     |                 |
|           |     |   HospitalityPOS_DB    |     |                 |
|           |     +------------------------+     |                 |
|           +------------------------------------+                 |
+------------------------------------------------------------------+
                              |
                              v
+------------------------------------------------------------------+
|                    HARDWARE LAYER                                |
|  +-------------+  +-------------+  +-------------+               |
|  |   Receipt   |  |   Kitchen   |  |    Cash     |               |
|  |   Printer   |  |   Printer   |  |   Drawer    |               |
|  +-------------+  +-------------+  +-------------+               |
|  +-------------+  +-------------+                                |
|  |   Barcode   |  |  Customer   |                                |
|  |   Scanner   |  |   Display   |                                |
|  +-------------+  +-------------+                                |
+------------------------------------------------------------------+
```

### 2.2 Layer Responsibilities

| Layer | Responsibility | Technologies |
|-------|---------------|--------------|
| Presentation | UI rendering, user interaction, view state | WPF, XAML, MVVM |
| View Models | UI logic, command binding, data transformation | C#, INotifyPropertyChanged |
| Business Logic | Business rules, workflows, validations | C# Services |
| Data Access | Database operations, entity mapping | EF Core, Repositories |
| Database | Data persistence, constraints, stored procedures | SQL Server Express |
| Hardware | Peripheral communication | ESC/POS, USB, Serial |

---

## 2.3 Mode-Aware UI Architecture

The system switches between two distinct UI layouts based on the configured business mode:

### Restaurant Mode Layout (SambaPOS Pattern)
```
+---------------------------+----------------+----------------------------------+
|       ORDER TICKET        |   CATEGORIES   |         PRODUCTS GRID            |
|         (Left)            |    (Middle)    |           (Right)                |
+---------------------------+----------------+----------------------------------+
| Table: Inside 01          |   [Pizza]      | [1] [2] [3] [4] [5]  <- Pages    |
| Waiter: John              |   [Burgers]    |                                  |
|                           |   [Drinks]*    | +--------+ +--------+ +--------+ |
| Spicy Italian   $9.90     |   [Desserts]   | | Product| | Product| | Product| |
| Turkey Breast   $6.95     |                | |  Tile  | |  Tile  | |  Tile  | |
|                           |   * = selected | +--------+ +--------+ +--------+ |
| Balance:        $31.50    |                |                                  |
| [Cash] [Card] [Settle]    |                |                                  |
+---------------------------+----------------+----------------------------------+
```

### Supermarket Mode Layout (Fast Checkout Pattern)
```
+----------------------------------------------------------------------+
|  🔍 [Barcode/Search Input - AUTO FOCUS]              [F2: Manual]    |
+----------------------------------------------------------------------+
|                                                                      |
|  SCANNED ITEMS LIST                                                  |
|  ┌─────────────────────────────────────────────────────────────────┐ |
|  │ #   Product                 Qty      Price      Total           │ |
|  │ 1   Milk 1L (6001234567)    x2      $3.50      $7.00           │ |
|  │ 2   Bread Loaf              x1      $2.00      $2.00           │ |
|  │ 3   Eggs 12pk (OFFER!)      x1      $4.50      $3.99  ← Offer  │ |
|  │     ~~~~~~~~~~~~~~~~~~~~~~~~                    ~~~~~           │ |
|  │     Was: $4.50  Now: $3.99 (11% off)                           │ |
|  └─────────────────────────────────────────────────────────────────┘ |
|                                                                      |
|  SUBTOTAL:     $12.99          ┌──────────────────────────────────┐ |
|  TAX (16%):     $2.08          │  [CASH]   [CARD]   [M-PESA]     │ |
|  ─────────────────────         │                                  │ |
|  TOTAL:        $15.07          │  [VOID ITEM]  [VOID ALL]  [PAY] │ |
|                                └──────────────────────────────────┘ |
+----------------------------------------------------------------------+
```

### Mode Configuration Entity

```csharp
public enum BusinessMode
{
    Restaurant = 1,
    Supermarket = 2,
    Hybrid = 3
}

public class SystemConfiguration
{
    public BusinessMode Mode { get; set; } = BusinessMode.Restaurant;

    // Feature Flags
    public bool EnableTableManagement { get; set; } = true;
    public bool EnableKitchenDisplay { get; set; } = true;
    public bool EnableWaiterAssignment { get; set; } = true;
    public bool EnableBarcodeAutoFocus { get; set; } = false;
    public bool EnableProductOffers { get; set; } = false;
    public bool EnableSupplierCredit { get; set; } = false;
    public bool EnablePayroll { get; set; } = false;
    public bool EnableAccounting { get; set; } = false;

    // Auto-set based on mode
    public void ApplyModeDefaults()
    {
        switch (Mode)
        {
            case BusinessMode.Restaurant:
                EnableTableManagement = true;
                EnableKitchenDisplay = true;
                EnableWaiterAssignment = true;
                EnableBarcodeAutoFocus = false;
                EnableProductOffers = false;
                EnableSupplierCredit = false;
                break;

            case BusinessMode.Supermarket:
                EnableTableManagement = false;
                EnableKitchenDisplay = false;
                EnableWaiterAssignment = false;
                EnableBarcodeAutoFocus = true;
                EnableProductOffers = true;
                EnableSupplierCredit = true;
                break;

            case BusinessMode.Hybrid:
                // All features enabled
                EnableTableManagement = true;
                EnableKitchenDisplay = true;
                EnableWaiterAssignment = true;
                EnableBarcodeAutoFocus = true;
                EnableProductOffers = true;
                EnableSupplierCredit = true;
                break;
        }
    }
}
```

### UI Shell Service

```csharp
public interface IUiShellService
{
    BusinessMode CurrentMode { get; }
    void SwitchLayout(BusinessMode mode);
    Type GetMainViewType();
    Type GetProductSelectionViewType();
}

public class UiShellService : IUiShellService
{
    public Type GetMainViewType() => CurrentMode switch
    {
        BusinessMode.Restaurant => typeof(RestaurantPOSView),
        BusinessMode.Supermarket => typeof(SupermarketPOSView),
        BusinessMode.Hybrid => typeof(HybridPOSView),
        _ => typeof(RestaurantPOSView)
    };
}
```

---

## 3. Project Structure

```
HospitalityPOS/
├── src/
│   ├── HospitalityPOS.Core/              # Core domain models and interfaces
│   │   ├── Entities/                     # Domain entities
│   │   │   ├── User.cs
│   │   │   ├── Role.cs
│   │   │   ├── Product.cs
│   │   │   ├── Category.cs
│   │   │   ├── Order.cs
│   │   │   ├── OrderItem.cs
│   │   │   ├── Receipt.cs
│   │   │   ├── Payment.cs
│   │   │   ├── WorkPeriod.cs
│   │   │   ├── Inventory.cs
│   │   │   ├── Supplier.cs
│   │   │   ├── PurchaseOrder.cs
│   │   │   └── AuditLog.cs
│   │   ├── Enums/                        # Enumerations
│   │   │   ├── ReceiptStatus.cs          # Created, Pending, Settled, Voided
│   │   │   ├── PaymentMethod.cs          # Cash, MPesa, Card, etc.
│   │   │   ├── OrderStatus.cs
│   │   │   └── UserRole.cs
│   │   ├── Interfaces/                   # Service and repository interfaces
│   │   │   ├── IRepository.cs
│   │   │   ├── ISalesService.cs
│   │   │   ├── IInventoryService.cs
│   │   │   ├── IPaymentService.cs
│   │   │   ├── IReportingService.cs
│   │   │   ├── IAuditService.cs
│   │   │   ├── IWorkPeriodService.cs
│   │   │   ├── IPrintService.cs
│   │   │   └── IUserService.cs
│   │   └── DTOs/                         # Data transfer objects
│   │
│   ├── HospitalityPOS.Infrastructure/    # Data access and external services
│   │   ├── Data/
│   │   │   ├── HospitalityDbContext.cs
│   │   │   ├── Configurations/           # EF Core entity configurations
│   │   │   └── Migrations/               # EF Core migrations
│   │   ├── Repositories/
│   │   │   ├── BaseRepository.cs
│   │   │   ├── ProductRepository.cs
│   │   │   ├── OrderRepository.cs
│   │   │   ├── ReceiptRepository.cs
│   │   │   ├── UserRepository.cs
│   │   │   └── InventoryRepository.cs
│   │   ├── Printing/
│   │   │   ├── EscPosService.cs          # ESC/POS printer commands
│   │   │   ├── ReceiptPrintService.cs
│   │   │   └── KitchenPrintService.cs
│   │   └── Hardware/
│   │       ├── CashDrawerService.cs
│   │       └── BarcodeReaderService.cs
│   │
│   ├── HospitalityPOS.Business/          # Business logic services
│   │   ├── Services/
│   │   │   ├── SalesService.cs
│   │   │   ├── InventoryService.cs
│   │   │   ├── PaymentService.cs
│   │   │   ├── ReportingService.cs
│   │   │   ├── WorkPeriodService.cs
│   │   │   ├── ReceiptService.cs
│   │   │   ├── UserService.cs
│   │   │   ├── AuditService.cs
│   │   │   └── AuthorizationService.cs
│   │   └── Validators/
│   │       ├── OrderValidator.cs
│   │       └── PaymentValidator.cs
│   │
│   └── HospitalityPOS.WPF/               # WPF desktop application
│       ├── App.xaml                      # Application entry point
│       ├── App.xaml.cs
│       ├── Views/                        # XAML views
│       │   ├── MainWindow.xaml           # Main POS interface
│       │   ├── LoginView.xaml
│       │   ├── POSView.xaml              # Sales/order entry
│       │   ├── PaymentView.xaml
│       │   ├── ReceiptListView.xaml
│       │   ├── TableMapView.xaml
│       │   ├── ProductManagementView.xaml
│       │   ├── InventoryView.xaml
│       │   ├── UserManagementView.xaml
│       │   ├── ReportsView.xaml
│       │   ├── SettingsView.xaml
│       │   └── WorkPeriodView.xaml
│       ├── ViewModels/                   # MVVM ViewModels
│       │   ├── BaseViewModel.cs
│       │   ├── MainViewModel.cs
│       │   ├── LoginViewModel.cs
│       │   ├── POSViewModel.cs
│       │   ├── PaymentViewModel.cs
│       │   ├── ReceiptListViewModel.cs
│       │   ├── ProductManagementViewModel.cs
│       │   ├── InventoryViewModel.cs
│       │   ├── UserManagementViewModel.cs
│       │   ├── ReportsViewModel.cs
│       │   └── SettingsViewModel.cs
│       ├── Controls/                     # Custom WPF controls
│       │   ├── ProductTileControl.xaml
│       │   ├── OrderItemControl.xaml
│       │   ├── PaymentMethodButton.xaml
│       │   ├── NumericKeypad.xaml
│       │   └── TableStatusControl.xaml
│       ├── Converters/                   # Value converters
│       │   ├── CurrencyConverter.cs
│       │   ├── StatusColorConverter.cs
│       │   └── BoolToVisibilityConverter.cs
│       ├── Resources/                    # Styles and resources
│       │   ├── Styles.xaml
│       │   ├── Colors.xaml
│       │   └── Icons.xaml
│       └── Services/                     # WPF-specific services
│           ├── NavigationService.cs
│           └── DialogService.cs
│
├── tests/
│   ├── HospitalityPOS.Core.Tests/
│   ├── HospitalityPOS.Business.Tests/
│   └── HospitalityPOS.WPF.Tests/
│
├── docs/
│   └── PRD_Hospitality_POS_System.md
│
├── HospitalityPOS.sln                    # Visual Studio solution file
└── README.md
```

---

## 4. Database Schema

### 4.1 Core Tables

```sql
-- Users and Authentication
CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(256) NOT NULL,
    PIN NVARCHAR(10),
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100),
    Phone NVARCHAR(20),
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2
);

CREATE TABLE Roles (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(200),
    IsSystem BIT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

CREATE TABLE UserRoles (
    UserId INT FOREIGN KEY REFERENCES Users(Id),
    RoleId INT FOREIGN KEY REFERENCES Roles(Id),
    PRIMARY KEY (UserId, RoleId)
);

CREATE TABLE Permissions (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL UNIQUE,
    Category NVARCHAR(50),
    Description NVARCHAR(200)
);

CREATE TABLE RolePermissions (
    RoleId INT FOREIGN KEY REFERENCES Roles(Id),
    PermissionId INT FOREIGN KEY REFERENCES Permissions(Id),
    PRIMARY KEY (RoleId, PermissionId)
);

-- Work Period Management
CREATE TABLE WorkPeriods (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OpenedAt DATETIME2 NOT NULL,
    ClosedAt DATETIME2,
    OpenedByUserId INT FOREIGN KEY REFERENCES Users(Id),
    ClosedByUserId INT FOREIGN KEY REFERENCES Users(Id),
    OpeningFloat DECIMAL(18,2) NOT NULL,
    ClosingCash DECIMAL(18,2),
    ExpectedCash DECIMAL(18,2),
    Variance DECIMAL(18,2),
    ZReportNumber INT,
    Status NVARCHAR(20) DEFAULT 'Open', -- Open, Closed
    Notes NVARCHAR(500)
);

-- Products and Categories
CREATE TABLE Categories (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    ParentCategoryId INT FOREIGN KEY REFERENCES Categories(Id),
    ImagePath NVARCHAR(500),
    DisplayOrder INT DEFAULT 0,
    IsActive BIT DEFAULT 1
);

CREATE TABLE Products (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Code NVARCHAR(50) NOT NULL UNIQUE,
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(500),
    CategoryId INT FOREIGN KEY REFERENCES Categories(Id),
    SellingPrice DECIMAL(18,2) NOT NULL,
    CostPrice DECIMAL(18,2),
    TaxRate DECIMAL(5,2) DEFAULT 16.00,
    UnitOfMeasure NVARCHAR(20) DEFAULT 'Each',
    ImagePath NVARCHAR(500),
    Barcode NVARCHAR(50),
    MinStockLevel DECIMAL(18,3),
    MaxStockLevel DECIMAL(18,3),
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2
);

-- Product Offers/Promotions (Supermarket Feature)
CREATE TABLE ProductOffers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProductId INT FOREIGN KEY REFERENCES Products(Id),
    OfferName NVARCHAR(100) NOT NULL,
    OfferPrice DECIMAL(18,2) NOT NULL,
    DiscountPercent DECIMAL(5,2), -- Alternative to fixed price
    StartDate DATETIME2 NOT NULL,
    EndDate DATETIME2 NOT NULL,
    IsActive BIT DEFAULT 1,
    MinQuantity INT DEFAULT 1, -- Buy X get discount
    MaxQuantity INT, -- Limit per transaction
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    CreatedByUserId INT FOREIGN KEY REFERENCES Users(Id)
);

-- Index for quick offer lookup
CREATE INDEX IX_ProductOffers_Active ON ProductOffers (ProductId, StartDate, EndDate)
    WHERE IsActive = 1;

-- Orders and Receipts
CREATE TABLE Orders (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OrderNumber NVARCHAR(20) NOT NULL UNIQUE,
    WorkPeriodId INT FOREIGN KEY REFERENCES WorkPeriods(Id),
    UserId INT FOREIGN KEY REFERENCES Users(Id),
    TableNumber NVARCHAR(20),
    CustomerName NVARCHAR(100),
    Subtotal DECIMAL(18,2) NOT NULL,
    TaxAmount DECIMAL(18,2) NOT NULL,
    DiscountAmount DECIMAL(18,2) DEFAULT 0,
    TotalAmount DECIMAL(18,2) NOT NULL,
    Status NVARCHAR(20) DEFAULT 'Open', -- Open, Printed, Completed
    Notes NVARCHAR(500),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2
);

CREATE TABLE OrderItems (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT FOREIGN KEY REFERENCES Orders(Id),
    ProductId INT FOREIGN KEY REFERENCES Products(Id),
    Quantity DECIMAL(18,3) NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    DiscountAmount DECIMAL(18,2) DEFAULT 0,
    TaxAmount DECIMAL(18,2) NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL,
    Modifiers NVARCHAR(500),
    Notes NVARCHAR(200),
    BatchNumber INT DEFAULT 1, -- For tracking additions
    PrintedToKitchen BIT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

CREATE TABLE Receipts (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ReceiptNumber NVARCHAR(20) NOT NULL UNIQUE,
    OrderId INT FOREIGN KEY REFERENCES Orders(Id),
    WorkPeriodId INT FOREIGN KEY REFERENCES WorkPeriods(Id),
    OwnerId INT FOREIGN KEY REFERENCES Users(Id),
    Status NVARCHAR(20) DEFAULT 'Pending', -- Created, Pending, Settled, Voided
    Subtotal DECIMAL(18,2) NOT NULL,
    TaxAmount DECIMAL(18,2) NOT NULL,
    DiscountAmount DECIMAL(18,2) DEFAULT 0,
    TotalAmount DECIMAL(18,2) NOT NULL,
    PaidAmount DECIMAL(18,2) DEFAULT 0,
    ChangeAmount DECIMAL(18,2) DEFAULT 0,
    VoidedAt DATETIME2,
    VoidedByUserId INT FOREIGN KEY REFERENCES Users(Id),
    VoidReason NVARCHAR(200),
    ParentReceiptId INT FOREIGN KEY REFERENCES Receipts(Id), -- For split receipts
    MergedIntoReceiptId INT FOREIGN KEY REFERENCES Receipts(Id), -- For merged receipts
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    SettledAt DATETIME2
);

-- Payments
CREATE TABLE PaymentMethods (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL UNIQUE,
    Code NVARCHAR(20) NOT NULL UNIQUE,
    IsActive BIT DEFAULT 1,
    RequiresReference BIT DEFAULT 0, -- For M-Pesa transaction codes
    DisplayOrder INT DEFAULT 0
);

CREATE TABLE Payments (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ReceiptId INT FOREIGN KEY REFERENCES Receipts(Id),
    PaymentMethodId INT FOREIGN KEY REFERENCES PaymentMethods(Id),
    Amount DECIMAL(18,2) NOT NULL,
    Reference NVARCHAR(100), -- M-Pesa code, card last 4 digits
    ProcessedByUserId INT FOREIGN KEY REFERENCES Users(Id),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- Inventory
CREATE TABLE Inventory (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProductId INT FOREIGN KEY REFERENCES Products(Id) UNIQUE,
    CurrentStock DECIMAL(18,3) NOT NULL DEFAULT 0,
    ReservedStock DECIMAL(18,3) DEFAULT 0,
    LastUpdated DATETIME2 DEFAULT GETUTCDATE()
);

CREATE TABLE StockMovements (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProductId INT FOREIGN KEY REFERENCES Products(Id),
    MovementType NVARCHAR(20) NOT NULL, -- Sale, Purchase, Adjustment, Void, StockTake
    Quantity DECIMAL(18,3) NOT NULL, -- Positive for in, negative for out
    PreviousStock DECIMAL(18,3) NOT NULL,
    NewStock DECIMAL(18,3) NOT NULL,
    ReferenceType NVARCHAR(50), -- Order, PurchaseOrder, Adjustment
    ReferenceId INT,
    Reason NVARCHAR(200),
    UserId INT FOREIGN KEY REFERENCES Users(Id),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- Suppliers and Purchases (Enhanced for Supermarket Credit Management)
CREATE TABLE Suppliers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    ContactPerson NVARCHAR(100),
    Phone NVARCHAR(50),
    Email NVARCHAR(100),
    Address NVARCHAR(500),
    TaxId NVARCHAR(50), -- KRA PIN for Kenya
    -- Credit Terms (Supermarket Feature)
    PaymentTermDays INT DEFAULT 0, -- 0 = COD, 30 = Net 30, etc.
    CreditLimit DECIMAL(18,2) DEFAULT 0,
    CurrentBalance DECIMAL(18,2) DEFAULT 0, -- Amount we owe them
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

CREATE TABLE PurchaseOrders (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PONumber NVARCHAR(20) NOT NULL UNIQUE,
    SupplierId INT FOREIGN KEY REFERENCES Suppliers(Id),
    Status NVARCHAR(20) DEFAULT 'Draft', -- Draft, Sent, PartiallyReceived, Complete, Cancelled
    TotalAmount DECIMAL(18,2),
    -- Payment Status (Supermarket Feature)
    PaymentStatus NVARCHAR(20) DEFAULT 'Unpaid', -- Paid, Unpaid, PartiallyPaid
    AmountPaid DECIMAL(18,2) DEFAULT 0,
    DueDate DATE, -- Based on supplier payment terms
    PaidDate DATETIME2,
    InvoiceNumber NVARCHAR(50), -- Supplier's invoice number
    CreatedByUserId INT FOREIGN KEY REFERENCES Users(Id),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    ExpectedDate DATE,
    ReceivedAt DATETIME2
);

-- Supplier Invoices / Accounts Payable (Supermarket Feature)
CREATE TABLE SupplierInvoices (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    InvoiceNumber NVARCHAR(50) NOT NULL,
    SupplierId INT FOREIGN KEY REFERENCES Suppliers(Id),
    PurchaseOrderId INT FOREIGN KEY REFERENCES PurchaseOrders(Id),
    InvoiceDate DATE NOT NULL,
    DueDate DATE NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL,
    PaidAmount DECIMAL(18,2) DEFAULT 0,
    Status NVARCHAR(20) DEFAULT 'Unpaid', -- Unpaid, PartiallyPaid, Paid, Overdue
    Notes NVARCHAR(500),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_SupplierInvoice UNIQUE (SupplierId, InvoiceNumber)
);

-- Supplier Payments (Track payments to suppliers)
CREATE TABLE SupplierPayments (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SupplierInvoiceId INT FOREIGN KEY REFERENCES SupplierInvoices(Id),
    SupplierId INT FOREIGN KEY REFERENCES Suppliers(Id),
    PaymentDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    Amount DECIMAL(18,2) NOT NULL,
    PaymentMethod NVARCHAR(50), -- Cash, Bank Transfer, Cheque
    Reference NVARCHAR(100), -- Cheque number, transaction ref
    ProcessedByUserId INT FOREIGN KEY REFERENCES Users(Id),
    Notes NVARCHAR(200),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

CREATE TABLE PurchaseOrderItems (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PurchaseOrderId INT FOREIGN KEY REFERENCES PurchaseOrders(Id),
    ProductId INT FOREIGN KEY REFERENCES Products(Id),
    OrderedQuantity DECIMAL(18,3) NOT NULL,
    ReceivedQuantity DECIMAL(18,3) DEFAULT 0,
    UnitCost DECIMAL(18,2) NOT NULL,
    TotalCost DECIMAL(18,2) NOT NULL
);

CREATE TABLE GoodsReceived (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    GRNNumber NVARCHAR(20) NOT NULL UNIQUE,
    PurchaseOrderId INT FOREIGN KEY REFERENCES PurchaseOrders(Id),
    SupplierId INT FOREIGN KEY REFERENCES Suppliers(Id),
    ReceivedByUserId INT FOREIGN KEY REFERENCES Users(Id),
    TotalValue DECIMAL(18,2),
    Notes NVARCHAR(500),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- =====================================================
-- EMPLOYEE & PAYROLL MODULE (Supermarket Feature)
-- =====================================================

CREATE TABLE Employees (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT FOREIGN KEY REFERENCES Users(Id), -- Link to system user if applicable
    EmployeeNumber NVARCHAR(20) NOT NULL UNIQUE,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    NationalId NVARCHAR(50),
    Phone NVARCHAR(50),
    Email NVARCHAR(100),
    Address NVARCHAR(500),
    DateOfBirth DATE,
    HireDate DATE NOT NULL,
    TerminationDate DATE,
    Department NVARCHAR(50),
    Position NVARCHAR(100),
    EmploymentType NVARCHAR(20) DEFAULT 'FullTime', -- FullTime, PartTime, Contract
    -- Salary Information
    BasicSalary DECIMAL(18,2) NOT NULL DEFAULT 0,
    PayFrequency NVARCHAR(20) DEFAULT 'Monthly', -- Weekly, BiWeekly, Monthly
    BankName NVARCHAR(100),
    BankAccountNumber NVARCHAR(50),
    TaxId NVARCHAR(50), -- KRA PIN
    NssfNumber NVARCHAR(50),
    NhifNumber NVARCHAR(50),
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

CREATE TABLE SalaryComponents (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    ComponentType NVARCHAR(20) NOT NULL, -- Earning, Deduction
    IsFixed BIT DEFAULT 1, -- Fixed amount or percentage
    DefaultAmount DECIMAL(18,2),
    DefaultPercent DECIMAL(5,2),
    IsTaxable BIT DEFAULT 1,
    IsStatutory BIT DEFAULT 0, -- PAYE, NHIF, NSSF
    DisplayOrder INT DEFAULT 0,
    IsActive BIT DEFAULT 1
);

CREATE TABLE EmployeeSalaryComponents (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT FOREIGN KEY REFERENCES Employees(Id),
    SalaryComponentId INT FOREIGN KEY REFERENCES SalaryComponents(Id),
    Amount DECIMAL(18,2),
    Percent DECIMAL(5,2),
    EffectiveFrom DATE NOT NULL,
    EffectiveTo DATE,
    CONSTRAINT UQ_EmployeeSalaryComponent UNIQUE (EmployeeId, SalaryComponentId, EffectiveFrom)
);

CREATE TABLE PayrollPeriods (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PeriodName NVARCHAR(50) NOT NULL, -- e.g., "December 2025"
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    PayDate DATE NOT NULL,
    Status NVARCHAR(20) DEFAULT 'Draft', -- Draft, Processing, Approved, Paid
    ProcessedByUserId INT FOREIGN KEY REFERENCES Users(Id),
    ApprovedByUserId INT FOREIGN KEY REFERENCES Users(Id),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    ProcessedAt DATETIME2,
    ApprovedAt DATETIME2
);

CREATE TABLE Payslips (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PayrollPeriodId INT FOREIGN KEY REFERENCES PayrollPeriods(Id),
    EmployeeId INT FOREIGN KEY REFERENCES Employees(Id),
    BasicSalary DECIMAL(18,2) NOT NULL,
    TotalEarnings DECIMAL(18,2) NOT NULL,
    TotalDeductions DECIMAL(18,2) NOT NULL,
    NetPay DECIMAL(18,2) NOT NULL,
    PaymentStatus NVARCHAR(20) DEFAULT 'Pending', -- Pending, Paid
    PaymentMethod NVARCHAR(50),
    PaymentReference NVARCHAR(100),
    PaidAt DATETIME2,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_Payslip UNIQUE (PayrollPeriodId, EmployeeId)
);

CREATE TABLE PayslipDetails (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PayslipId INT FOREIGN KEY REFERENCES Payslips(Id),
    SalaryComponentId INT FOREIGN KEY REFERENCES SalaryComponents(Id),
    ComponentType NVARCHAR(20) NOT NULL, -- Earning, Deduction
    Amount DECIMAL(18,2) NOT NULL
);

-- =====================================================
-- EXPENSES MODULE (Supermarket Feature)
-- =====================================================

CREATE TABLE ExpenseCategories (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(200),
    ParentCategoryId INT FOREIGN KEY REFERENCES ExpenseCategories(Id),
    IsActive BIT DEFAULT 1
);

CREATE TABLE Expenses (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ExpenseNumber NVARCHAR(20) NOT NULL UNIQUE,
    ExpenseCategoryId INT FOREIGN KEY REFERENCES ExpenseCategories(Id),
    Description NVARCHAR(500) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    TaxAmount DECIMAL(18,2) DEFAULT 0,
    ExpenseDate DATE NOT NULL,
    PaymentMethod NVARCHAR(50),
    PaymentReference NVARCHAR(100),
    SupplierId INT FOREIGN KEY REFERENCES Suppliers(Id), -- If expense is to a supplier
    ReceiptImagePath NVARCHAR(500),
    Status NVARCHAR(20) DEFAULT 'Pending', -- Pending, Approved, Rejected, Paid
    ApprovedByUserId INT FOREIGN KEY REFERENCES Users(Id),
    CreatedByUserId INT FOREIGN KEY REFERENCES Users(Id),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    ApprovedAt DATETIME2
);

-- =====================================================
-- ACCOUNTING MODULE (Semi-Accounting Feature)
-- =====================================================

CREATE TABLE ChartOfAccounts (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    AccountCode NVARCHAR(20) NOT NULL UNIQUE,
    AccountName NVARCHAR(100) NOT NULL,
    AccountType NVARCHAR(20) NOT NULL, -- Asset, Liability, Equity, Revenue, Expense
    ParentAccountId INT FOREIGN KEY REFERENCES ChartOfAccounts(Id),
    Description NVARCHAR(200),
    IsSystemAccount BIT DEFAULT 0, -- Cannot be deleted
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

CREATE TABLE AccountingPeriods (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PeriodName NVARCHAR(50) NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    Status NVARCHAR(20) DEFAULT 'Open', -- Open, Closed
    ClosedByUserId INT FOREIGN KEY REFERENCES Users(Id),
    ClosedAt DATETIME2
);

CREATE TABLE JournalEntries (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EntryNumber NVARCHAR(20) NOT NULL UNIQUE,
    EntryDate DATE NOT NULL,
    Description NVARCHAR(500),
    ReferenceType NVARCHAR(50), -- Sale, Purchase, Payment, Expense, Payroll, Manual
    ReferenceId INT, -- Link to source transaction
    AccountingPeriodId INT FOREIGN KEY REFERENCES AccountingPeriods(Id),
    Status NVARCHAR(20) DEFAULT 'Posted', -- Draft, Posted, Reversed
    CreatedByUserId INT FOREIGN KEY REFERENCES Users(Id),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

CREATE TABLE JournalEntryLines (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    JournalEntryId INT FOREIGN KEY REFERENCES JournalEntries(Id),
    AccountId INT FOREIGN KEY REFERENCES ChartOfAccounts(Id),
    Description NVARCHAR(200),
    DebitAmount DECIMAL(18,2) DEFAULT 0,
    CreditAmount DECIMAL(18,2) DEFAULT 0,
    CONSTRAINT CK_DebitCredit CHECK (
        (DebitAmount > 0 AND CreditAmount = 0) OR
        (CreditAmount > 0 AND DebitAmount = 0)
    )
);

-- Default Chart of Accounts
INSERT INTO ChartOfAccounts (AccountCode, AccountName, AccountType, IsSystemAccount) VALUES
-- Assets
('1000', 'Cash', 'Asset', 1),
('1010', 'Bank Account', 'Asset', 1),
('1100', 'Accounts Receivable', 'Asset', 1),
('1200', 'Inventory', 'Asset', 1),
-- Liabilities
('2000', 'Accounts Payable', 'Liability', 1),
('2100', 'Salaries Payable', 'Liability', 1),
('2200', 'Tax Payable', 'Liability', 1),
-- Revenue
('4000', 'Sales Revenue', 'Revenue', 1),
('4100', 'Other Income', 'Revenue', 1),
-- Expenses
('5000', 'Cost of Goods Sold', 'Expense', 1),
('5100', 'Salaries Expense', 'Expense', 1),
('5200', 'Rent Expense', 'Expense', 1),
('5300', 'Utilities Expense', 'Expense', 1),
('5400', 'Supplies Expense', 'Expense', 1),
('5500', 'Other Expenses', 'Expense', 1);

-- =====================================================
-- AUDIT LOG
-- =====================================================

-- Audit Log
CREATE TABLE AuditLog (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId INT FOREIGN KEY REFERENCES Users(Id),
    Action NVARCHAR(100) NOT NULL,
    EntityType NVARCHAR(100),
    EntityId INT,
    OldValues NVARCHAR(MAX),
    NewValues NVARCHAR(MAX),
    IpAddress NVARCHAR(50),
    MachineName NVARCHAR(100),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- System Settings
CREATE TABLE SystemSettings (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SettingKey NVARCHAR(100) NOT NULL UNIQUE,
    SettingValue NVARCHAR(MAX),
    SettingType NVARCHAR(50), -- String, Int, Bool, Decimal, Json
    Category NVARCHAR(50),
    Description NVARCHAR(200),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE()
);
```

### 4.2 Default Data

```sql
-- Default Payment Methods
INSERT INTO PaymentMethods (Name, Code, IsActive, RequiresReference, DisplayOrder) VALUES
('Cash', 'CASH', 1, 0, 1),
('M-Pesa', 'MPESA', 1, 1, 2),
('Airtel Money', 'AIRTEL', 1, 1, 3),
('Credit Card', 'CARD', 1, 0, 4),
('Debit Card', 'DEBIT', 1, 0, 5);

-- Default Roles
INSERT INTO Roles (Name, Description, IsSystem) VALUES
('Administrator', 'Full system access', 1),
('Manager', 'Day-to-day operational management', 1),
('Supervisor', 'Floor supervision', 1),
('Cashier', 'Payment processing', 1),
('Waiter', 'Order taking and service', 1);

-- Default System Settings
INSERT INTO SystemSettings (SettingKey, SettingValue, SettingType, Category, Description) VALUES
('BusinessName', 'Hospitality POS', 'String', 'Business', 'Business name on receipts'),
('BusinessAddress', '', 'String', 'Business', 'Business address on receipts'),
('BusinessPhone', '', 'String', 'Business', 'Contact phone number'),
('TaxRate', '16', 'Decimal', 'Tax', 'Default VAT rate'),
('Currency', 'KES', 'String', 'Regional', 'Currency code'),
('CurrencySymbol', 'KSh', 'String', 'Regional', 'Currency symbol'),
('AutoSettleOnPrint', 'false', 'Bool', 'Receipts', 'Auto-settle receipts when printed'),
('SessionTimeout', '30', 'Int', 'Security', 'Session timeout in minutes'),
('RequireVoidReason', 'true', 'Bool', 'Operations', 'Require reason for voids');
```

---

## 5. Security Architecture

### 5.1 Authentication

| Component | Implementation |
|-----------|----------------|
| Password Hashing | BCrypt with cost factor 12 |
| Session Management | In-memory session with timeout |
| PIN Authentication | 4-6 digit PIN for quick access |
| Failed Login Lockout | 5 attempts, 15-minute lockout |

### 5.2 Role-Based Access Control (RBAC)

```csharp
public enum Permission
{
    // Work Period
    WorkPeriod_Open,
    WorkPeriod_Close,
    WorkPeriod_ViewHistory,

    // Sales
    Sales_Create,
    Sales_ViewOwn,
    Sales_ViewAll,
    Sales_Modify,
    Sales_Void,

    // Receipts
    Receipts_View,
    Receipts_Split,
    Receipts_Merge,
    Receipts_Reprint,
    Receipts_Void,

    // Products
    Products_View,
    Products_Create,
    Products_Edit,
    Products_Delete,
    Products_SetPrices,

    // Inventory
    Inventory_View,
    Inventory_Adjust,
    Inventory_ReceivePurchase,
    Inventory_FullAccess,

    // Users
    Users_View,
    Users_Create,
    Users_Edit,
    Users_Delete,
    Users_AssignRoles,

    // Reports
    Reports_XReport,
    Reports_ZReport,
    Reports_Sales,
    Reports_Inventory,
    Reports_Audit,

    // Discounts
    Discounts_Apply10,
    Discounts_Apply20,
    Discounts_Apply50,
    Discounts_ApplyAny,

    // Settings
    Settings_View,
    Settings_Modify
}
```

### 5.3 Authorization Override Workflow

```
[User Action Blocked]
        |
        v
[Show PIN Entry Dialog]
        |
        v
[Authorized User Enters PIN]
        |
        v
[Validate Permission]
        |
    +---+---+
    |       |
    v       v
[Execute] [Deny]
    |
    v
[Log Override: original_user, authorizing_user, action, timestamp]
```

---

## 6. Core Services Design

### 6.1 Work Period Service

```csharp
public interface IWorkPeriodService
{
    Task<WorkPeriod> OpenWorkPeriodAsync(decimal openingFloat, int userId);
    Task<WorkPeriod> CloseWorkPeriodAsync(decimal closingCash, int userId);
    Task<WorkPeriod> GetCurrentWorkPeriodAsync();
    bool IsWorkPeriodOpen();
    Task<ZReport> GenerateZReportAsync(int workPeriodId);
    Task<XReport> GenerateXReportAsync();
}
```

### 6.2 Sales Service

```csharp
public interface ISalesService
{
    Task<Order> CreateOrderAsync(OrderDto orderDto, int userId);
    Task<OrderItem> AddItemToOrderAsync(int orderId, OrderItemDto item);
    Task<Order> UpdateOrderAsync(int orderId, OrderDto orderDto);
    Task<bool> VoidOrderAsync(int orderId, string reason, int authorizedUserId);
    Task<Order> GetOrderByIdAsync(int orderId);
    Task<IEnumerable<Order>> GetOrdersByWorkPeriodAsync(int workPeriodId);
}
```

### 6.3 Receipt Service

```csharp
public interface IReceiptService
{
    Task<Receipt> CreateReceiptFromOrderAsync(int orderId, int userId);
    Task<Receipt> SettleReceiptAsync(int receiptId, List<PaymentDto> payments);
    Task<IEnumerable<Receipt>> SplitReceiptAsync(int receiptId, List<SplitDto> splits);
    Task<Receipt> MergeReceiptsAsync(List<int> receiptIds);
    Task<bool> VoidReceiptAsync(int receiptId, string reason, int authorizedUserId);
    Task<bool> CanUserModifyReceiptAsync(int receiptId, int userId);
    Task<Receipt> AddItemsToReceiptAsync(int receiptId, List<OrderItemDto> items);
}
```

### 6.4 Payment Service

```csharp
public interface IPaymentService
{
    Task<Payment> ProcessPaymentAsync(int receiptId, PaymentDto payment);
    Task<IEnumerable<PaymentMethod>> GetActivePaymentMethodsAsync();
    Task<decimal> CalculateChangeAsync(int receiptId, decimal amountTendered);
    Task<bool> ValidateSplitPaymentAsync(int receiptId, List<PaymentDto> payments);
}
```

### 6.5 Inventory Service

```csharp
public interface IInventoryService
{
    Task<decimal> GetCurrentStockAsync(int productId);
    Task DeductStockAsync(int productId, decimal quantity, string reference);
    Task AddStockAsync(int productId, decimal quantity, string reference);
    Task<bool> AdjustStockAsync(int productId, decimal newQuantity, string reason, int userId);
    Task<IEnumerable<Product>> GetLowStockProductsAsync();
    Task ReceiveGoodsAsync(GoodsReceivedDto grn);
}
```

### 6.6 Product Offer Service (Supermarket Feature)

```csharp
public interface IProductOfferService
{
    Task<ProductOffer?> GetActiveOfferAsync(int productId);
    Task<decimal> GetEffectivePriceAsync(int productId);
    Task<IEnumerable<ProductOffer>> GetActiveOffersAsync();
    Task<ProductOffer> CreateOfferAsync(ProductOfferDto offer, int userId);
    Task<ProductOffer> UpdateOfferAsync(int offerId, ProductOfferDto offer);
    Task DeactivateOfferAsync(int offerId);
    Task<IEnumerable<Product>> GetProductsOnOfferAsync();
}

// DTO for product with offer price display
public class ProductWithPriceDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Barcode { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal? OfferPrice { get; set; }
    public decimal EffectivePrice => OfferPrice ?? SellingPrice;
    public bool HasActiveOffer => OfferPrice.HasValue;
    public string? OfferName { get; set; }
    public decimal? DiscountPercent { get; set; }
}
```

### 6.7 Supplier Credit Service (Supermarket Feature)

```csharp
public interface ISupplierCreditService
{
    Task<decimal> GetSupplierBalanceAsync(int supplierId);
    Task<IEnumerable<SupplierInvoice>> GetUnpaidInvoicesAsync(int? supplierId = null);
    Task<IEnumerable<SupplierInvoice>> GetOverdueInvoicesAsync();
    Task<SupplierInvoice> CreateInvoiceAsync(SupplierInvoiceDto invoice);
    Task<SupplierPayment> RecordPaymentAsync(SupplierPaymentDto payment);
    Task<SupplierStatement> GetSupplierStatementAsync(int supplierId, DateTime from, DateTime to);
    Task UpdateSupplierBalanceAsync(int supplierId);
}

public interface IPurchaseOrderService
{
    Task<PurchaseOrder> CreatePurchaseOrderAsync(PurchaseOrderDto po, int userId);
    Task<PurchaseOrder> ReceiveGoodsAsync(int poId, GoodsReceivedDto grn, bool isPaid);
    Task<IEnumerable<PurchaseOrder>> GetUnpaidPurchaseOrdersAsync();
    Task MarkAsPaidAsync(int poId, PaymentDto payment);
}
```

### 6.8 Employee & Payroll Service (Supermarket Feature)

```csharp
public interface IEmployeeService
{
    Task<Employee> CreateEmployeeAsync(EmployeeDto employee);
    Task<Employee> UpdateEmployeeAsync(int employeeId, EmployeeDto employee);
    Task<IEnumerable<Employee>> GetActiveEmployeesAsync();
    Task<Employee> GetEmployeeByIdAsync(int employeeId);
    Task TerminateEmployeeAsync(int employeeId, DateTime terminationDate);
}

public interface IPayrollService
{
    Task<PayrollPeriod> CreatePayrollPeriodAsync(PayrollPeriodDto period);
    Task<PayrollPeriod> ProcessPayrollAsync(int periodId);
    Task<PayrollPeriod> ApprovePayrollAsync(int periodId, int approverUserId);
    Task<Payslip> GeneratePayslipAsync(int periodId, int employeeId);
    Task<IEnumerable<Payslip>> GetPayslipsForPeriodAsync(int periodId);
    Task MarkPayslipAsPaidAsync(int payslipId, string paymentMethod, string reference);
    Task<PayrollSummary> GetPayrollSummaryAsync(int periodId);
}
```

### 6.9 Expense Service (Supermarket Feature)

```csharp
public interface IExpenseService
{
    Task<Expense> CreateExpenseAsync(ExpenseDto expense, int userId);
    Task<Expense> ApproveExpenseAsync(int expenseId, int approverUserId);
    Task RejectExpenseAsync(int expenseId, string reason, int userId);
    Task<IEnumerable<Expense>> GetPendingExpensesAsync();
    Task<IEnumerable<Expense>> GetExpensesByDateRangeAsync(DateTime from, DateTime to);
    Task<ExpenseSummary> GetExpenseSummaryAsync(DateTime from, DateTime to);
}
```

### 6.10 Accounting Service (Supermarket Feature)

```csharp
public interface IAccountingService
{
    // Journal Entries
    Task<JournalEntry> CreateJournalEntryAsync(JournalEntryDto entry, int userId);
    Task PostJournalEntryAsync(int entryId);
    Task ReverseJournalEntryAsync(int entryId, int userId);

    // Auto-posting from transactions
    Task PostSaleToLedgerAsync(Receipt receipt);
    Task PostPurchaseToLedgerAsync(PurchaseOrder po);
    Task PostExpenseToLedgerAsync(Expense expense);
    Task PostPayrollToLedgerAsync(PayrollPeriod period);

    // Reports
    Task<TrialBalance> GetTrialBalanceAsync(DateTime asOfDate);
    Task<IncomeStatement> GetIncomeStatementAsync(DateTime from, DateTime to);
    Task<BalanceSheet> GetBalanceSheetAsync(DateTime asOfDate);
    Task<GeneralLedger> GetGeneralLedgerAsync(int accountId, DateTime from, DateTime to);

    // Period Management
    Task<AccountingPeriod> OpenPeriodAsync(AccountingPeriodDto period);
    Task ClosePeriodAsync(int periodId, int userId);
}

// Report DTOs
public class TrialBalance
{
    public DateTime AsOfDate { get; set; }
    public List<TrialBalanceLine> Lines { get; set; }
    public decimal TotalDebits { get; set; }
    public decimal TotalCredits { get; set; }
    public bool IsBalanced => TotalDebits == TotalCredits;
}

public class IncomeStatement
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<AccountSummary> Revenue { get; set; }
    public decimal TotalRevenue { get; set; }
    public List<AccountSummary> Expenses { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetIncome => TotalRevenue - TotalExpenses;
}
```

---

## 7. Printing Architecture

### 7.1 ESC/POS Implementation

```csharp
public interface IPrintService
{
    Task<bool> PrintReceiptAsync(Receipt receipt, PrinterConfig printer);
    Task<bool> PrintKitchenOrderAsync(Order order, List<OrderItem> newItems, PrinterConfig printer);
    Task<bool> PrintXReportAsync(XReport report, PrinterConfig printer);
    Task<bool> PrintZReportAsync(ZReport report, PrinterConfig printer);
    Task<bool> OpenCashDrawerAsync(PrinterConfig printer);
}
```

### 7.2 Receipt Template

```
+--------------------------------+
|       [BUSINESS LOGO]          |
|       Business Name            |
|       Address Line 1           |
|       Phone: xxx-xxx-xxxx      |
+--------------------------------+
| Receipt #: R-00001             |
| Date: 2025-12-20 14:35         |
| Cashier: John Doe              |
| Table: 5                       |
+--------------------------------+
| Item            Qty    Amount  |
|--------------------------------|
| Beer             2     $20.00  |
| Pizza            1     $15.00  |
| Soda             1      $5.00  |
+--------------------------------+
| Subtotal:              $40.00  |
| Tax (16%):              $6.40  |
| TOTAL:                 $46.40  |
+--------------------------------+
| Payment: CASH          $50.00  |
| Change:                 $3.60  |
+--------------------------------+
|     Thank you for visiting!    |
|        Please come again       |
+--------------------------------+
```

---

## 8. Performance Requirements

| Metric | Target | Implementation |
|--------|--------|----------------|
| App Startup | < 10 seconds | Lazy loading, compiled XAML |
| Product Search | < 500ms | Indexed DB queries, local caching |
| Transaction Processing | < 2 seconds | Async operations, batch commits |
| Receipt Printing | < 3 seconds | Background printing, print queue |
| Report Generation | < 30 seconds | Indexed views, query optimization |

---

## 9. Error Handling Strategy

### 9.1 Exception Categories

| Category | Handling | User Message |
|----------|----------|--------------|
| Validation | Show field-level errors | Specific guidance |
| Business Rule | Block action, show reason | Clear explanation |
| Database | Retry with backoff | "Processing, please wait" |
| Hardware | Graceful degradation | Device-specific guidance |
| Unexpected | Log, generic message | "An error occurred" |

### 9.2 Logging

- **Framework:** Serilog
- **Sinks:** File (rolling daily), Windows Event Log
- **Levels:** Error, Warning, Information, Debug
- **Retention:** 30 days rolling

---

## 10. Deployment

### 10.1 Installation Package

- **Installer:** WiX Toolset or Inno Setup
- **Prerequisites:** .NET 10 Runtime (LTS), SQL Server Express
- **Database:** Auto-create on first run with migrations
- **Updates:** In-app update checker

### 10.2 Configuration

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=HospitalityPOS;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Printing": {
    "ReceiptPrinter": "USB001",
    "KitchenPrinter": "NET:192.168.1.100",
    "PaperWidth": 80
  },
  "Security": {
    "SessionTimeoutMinutes": 30,
    "MaxFailedLoginAttempts": 5,
    "LockoutMinutes": 15
  }
}
```

---

## 11. Testing Strategy

| Test Type | Coverage Target | Tools |
|-----------|-----------------|-------|
| Unit Tests | 80% business logic | xUnit, Moq |
| Integration Tests | All DB operations | TestContainers |
| UI Tests | Critical paths | Appium, WinAppDriver |
| Performance Tests | Key operations | BenchmarkDotNet |

---

## 12. Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.EntityFrameworkCore | 6.0+ | ORM |
| Microsoft.EntityFrameworkCore.SqlServer | 6.0+ | SQL Server provider |
| BCrypt.Net-Next | 4.0+ | Password hashing |
| Serilog | 2.0+ | Logging |
| CommunityToolkit.Mvvm | 8.0+ | MVVM helpers |
| FastReport | Latest | Reporting (alternative: RDLC) |

---

## 13. Constraints and Assumptions

### Constraints
- Windows 10/11 only (no cross-platform)
- Single database instance per installation
- Local network operation (no cloud dependency for core functions)
- Thermal printers must support ESC/POS

### Assumptions
- Stable power supply (or UPS recommended)
- LAN connectivity for multi-terminal setups
- Regular database backups configured
- Staff have basic computer literacy

---

## Appendix A: Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Classes | PascalCase | `OrderService` |
| Interfaces | I + PascalCase | `IOrderService` |
| Methods | PascalCase | `CreateOrderAsync` |
| Properties | PascalCase | `TotalAmount` |
| Private fields | _camelCase | `_orderRepository` |
| Constants | UPPER_SNAKE | `MAX_DISCOUNT_PERCENT` |
| DB Tables | PascalCase, Plural | `Orders`, `OrderItems` |
| DB Columns | PascalCase | `CreatedAt`, `TotalAmount` |

---

## Appendix B: API Response Codes

| Code | Meaning | Action |
|------|---------|--------|
| Success | Operation completed | Continue |
| ValidationError | Input validation failed | Show field errors |
| NotFound | Resource not found | Show message |
| Unauthorized | Permission denied | Request authorization |
| WorkPeriodClosed | No active work period | Prompt to open |
| InsufficientStock | Stock below requested | Show current stock |
| ReceiptSettled | Cannot modify settled receipt | Block action |
