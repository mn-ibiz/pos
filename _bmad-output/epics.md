# Hospitality POS System - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for the Hospitality POS System, decomposing the requirements from the PRD and Architecture into implementable stories. The system is a Windows desktop application built with C# and MS SQL Server Express for hotels, bars, and restaurants.

## Requirements Inventory

### Functional Requirements

| ID | Requirement | Priority | Epic |
|----|-------------|----------|------|
| FR-001 | Role-based access control with 5 default roles | Must Have | 2 |
| FR-002 | Work period open/close with cash float tracking | Must Have | 3 |
| FR-003 | Touch-optimized product grid with images | Must Have | 5 |
| FR-004 | Order creation with table assignment | Must Have | 5 |
| FR-005 | Kitchen order ticket (KOT) printing | Must Have | 5 |
| FR-006 | Receipt management (pending, settled, voided) | Must Have | 6 |
| FR-007 | Bill splitting and merging | Must Have | 6 |
| FR-008 | Multiple payment methods (Cash, M-Pesa, Card) | Must Have | 7 |
| FR-009 | Real-time inventory tracking | Must Have | 8 |
| FR-010 | X Report and Z Report generation | Must Have | 10 |
| FR-011 | Product and category management | Must Have | 4 |
| FR-012 | Supplier and purchase order management | Should Have | 9 |
| FR-013 | Stock take and adjustment functionality | Must Have | 8 |
| FR-014 | Audit trail for all transactions | Must Have | 2 |
| FR-015 | Incremental order printing (only new items) | Must Have | 5 |
| FR-016 | Owner-locked receipts (only owner can modify) | Must Have | 6 |
| FR-017 | Permission override with PIN authorization | Should Have | 2 |
| FR-018 | Table/floor management with visual display | Should Have | 11 |

### NonFunctional Requirements

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-001 | Application startup time | < 10 seconds |
| NFR-002 | Transaction processing time | < 2 seconds |
| NFR-003 | Receipt printing time | < 3 seconds |
| NFR-004 | System uptime | 99.9% |
| NFR-005 | Support 10,000+ products | Scalability |
| NFR-006 | Handle 1,000+ transactions/day | Performance |
| NFR-007 | Password hashing with BCrypt | Security |
| NFR-008 | Session timeout after inactivity | Security |

### FR Coverage Map

| Epic | Functional Requirements Covered |
|------|-------------------------------|
| Epic 1 | Foundation for all FRs |
| Epic 2 | FR-001, FR-014, FR-017 |
| Epic 3 | FR-002 |
| Epic 4 | FR-011 |
| Epic 5 | FR-003, FR-004, FR-005, FR-015 |
| Epic 6 | FR-006, FR-007, FR-016 |
| Epic 7 | FR-008 |
| Epic 8 | FR-009, FR-013 |
| Epic 9 | FR-012 |
| Epic 10 | FR-010 |
| Epic 11 | FR-018 |

## Epic List

| Epic # | Title | Priority | Stories | Mode |
|--------|-------|----------|---------|------|
| 1 | Foundation & Infrastructure | Critical | 5 | All |
| 2 | User Authentication & Authorization | Critical | 6 | All |
| 3 | Work Period Management | Critical | 4 | All |
| 4 | Product & Category Management | Critical | 5 | All |
| 5 | Sales & Order Management | Critical | 6 | All |
| 6 | Receipt Management | Critical | 6 | All |
| 7 | Payment Processing | Critical | 5 | All |
| 8 | Inventory Management | High | 5 | All |
| 9 | Purchase & Supplier Management | Medium | 4 | All |
| 10 | Reporting & Analytics | High | 5 | All |
| 11 | Table & Floor Management | Medium | 3 | Hospitality |
| 12 | Printing & Hardware Integration | High | 4 | All |
| 13 | System Mode Configuration | Critical | 3 | All |
| 14 | Product Offers & Promotions | High | 4 | Retail |
| 15 | Supplier Credit Management | High | 4 | Retail |
| 16 | Employee & Payroll Management | Medium | 5 | Retail |
| 17 | Accounting Module | Medium | 5 | Retail |
| **18** | **Kenya eTIMS Compliance** | **Critical** | **6** | **All (Kenya)** |
| **19** | **M-Pesa Daraja API Integration** | **Critical** | **5** | **All (Kenya)** |
| **20** | **Barcode, Scale & PLU Management** | **High** | **5** | **Retail** |
| **21** | **Advanced Loyalty Program** | **High** | **5** | **Retail** |
| **22** | **Multi-Store HQ Management** | **High** | **5** | **Retail (Chains)** |
| **23** | **Stock Transfers** | **Medium** | **4** | **Retail (Chains)** |
| **24** | **Batch & Expiry Tracking** | **High** | **5** | **Retail** |
| **25** | **Offline-First & Cloud Sync** | **Critical** | **5** | **All** |
| **26** | **Recipe & Ingredient Management** | **High** | **4** | **Hospitality** |
| **27** | **Kitchen Display System (KDS)** | **High** | **5** | **Hospitality** |
| **28** | **Shelf Label Printing** | **Medium** | **3** | **Retail** |
| **29** | **Advanced Promotions Engine** | **Critical** | **6** | **Retail** |
| **30** | **Accounts Receivable** | **Critical** | **5** | **All** |
| **31** | **Bank Reconciliation** | **Critical** | **4** | **All** |
| **32** | **Hotel PMS Integration** | **Critical** | **6** | **Hospitality (Hotels)** |
| **33** | **REST API Layer** | **Critical** | **5** | **All** |
| **34** | **Additional Mobile Money** | **High** | **3** | **All (Kenya)** |
| **35** | **Enhanced Financial Reporting** | **High** | **5** | **All** |
| **36** | **Budget & Cost Management** | **High** | **4** | **All** |
| **37** | **Checkout Enhancements** | **Medium** | **4** | **All** |
| **38** | **Inventory Analytics** | **High** | **4** | **Retail** |

---

## Epic 1: Foundation & Infrastructure

**Goal:** Establish the core application architecture, database schema, and project structure that will support all subsequent features. This epic creates the foundation upon which all other functionality is built.

**Dependencies:** None (first epic)

### Story 1.1: Project Setup and Solution Structure

As a developer,
I want a properly structured C# solution with layered architecture,
So that the codebase is maintainable, testable, and follows best practices.

**Acceptance Criteria:**

**Given** a new development environment
**When** the solution is created
**Then** it should have the following projects:
- HospitalityPOS.Core (domain entities, interfaces)
- HospitalityPOS.Infrastructure (data access, EF Core)
- HospitalityPOS.Business (services, business logic)
- HospitalityPOS.WPF (Windows desktop application)
**And** all projects should target .NET 10 (LTS)
**And** NuGet packages should be configured (EF Core, BCrypt, Serilog, CommunityToolkit.Mvvm)

---

### Story 1.2: Database Schema Creation

As a developer,
I want the complete database schema created in SQL Server Express,
So that all entities have proper tables with relationships and constraints.

**Acceptance Criteria:**

**Given** a SQL Server Express instance
**When** the database is initialized
**Then** all core tables should be created:
- Users, Roles, UserRoles, Permissions, RolePermissions
- WorkPeriods
- Categories, Products
- Orders, OrderItems
- Receipts, Payments, PaymentMethods
- Inventory, StockMovements
- Suppliers, PurchaseOrders, PurchaseOrderItems, GoodsReceived
- AuditLog, SystemSettings
**And** all foreign key relationships should be properly defined
**And** indexes should be created for frequently queried columns

---

### Story 1.3: Entity Framework Core Configuration

As a developer,
I want Entity Framework Core configured with proper entity mappings,
So that database operations are type-safe and efficient.

**Acceptance Criteria:**

**Given** the database schema exists
**When** EF Core is configured
**Then** DbContext should be properly set up with all DbSets
**And** entity configurations should define relationships, constraints, and column mappings
**And** connection string should be configurable via appsettings.json
**And** migrations should be enabled for schema evolution

---

### Story 1.4: Base Repository Pattern Implementation

As a developer,
I want a generic repository pattern implemented,
So that data access is consistent and testable across all entities.

**Acceptance Criteria:**

**Given** EF Core is configured
**When** repositories are implemented
**Then** IRepository<T> interface should provide:
- GetByIdAsync(int id)
- GetAllAsync()
- AddAsync(T entity)
- UpdateAsync(T entity)
- DeleteAsync(int id)
- Query() for custom LINQ queries
**And** concrete repositories should be created for each major entity
**And** unit of work pattern should be implemented for transaction management

---

### Story 1.5: MVVM Base Infrastructure

As a developer,
I want the MVVM infrastructure set up for the WPF application,
So that views and business logic are properly separated.

**Acceptance Criteria:**

**Given** the WPF project exists
**When** MVVM infrastructure is implemented
**Then** BaseViewModel should implement INotifyPropertyChanged
**And** RelayCommand/AsyncRelayCommand should be available for command binding
**And** NavigationService should allow view switching
**And** DialogService should provide modal dialog support
**And** dependency injection container should be configured

---

## Epic 2: User Authentication & Authorization

**Goal:** Implement secure user authentication with role-based access control (RBAC), enabling different staff members to access appropriate functionality based on their roles.

**Dependencies:** Epic 1 (Foundation)

### Story 2.1: User Login Screen

As a user,
I want to log into the system with my credentials,
So that I can access the POS functions appropriate to my role.

**Acceptance Criteria:**

**Given** the application is launched
**When** the login screen is displayed
**Then** user can enter username and password
**And** alternatively, user can enter a 4-6 digit PIN
**And** login button should validate credentials against the database
**And** on successful login, user should be redirected to the main POS screen
**And** on failed login, appropriate error message should be shown
**And** after 5 failed attempts, account should be locked for 15 minutes

---

### Story 2.2: Password Management

As an administrator,
I want secure password storage and management,
So that user credentials are protected.

**Acceptance Criteria:**

**Given** a user account exists
**When** password is stored or verified
**Then** passwords should be hashed using BCrypt with cost factor 12
**And** plain-text passwords should never be stored
**And** password reset should generate a new temporary password
**And** password complexity should require: 8+ characters, uppercase, lowercase, number

---

### Story 2.3: Role Management

As an administrator,
I want to create and manage user roles with specific permissions,
So that I can control access to different system functions.

**Acceptance Criteria:**

**Given** the admin is logged in with Administrator role
**When** accessing role management
**Then** admin can view all existing roles
**And** admin can create new custom roles
**And** admin can clone existing roles and modify permissions
**And** admin can assign/remove permissions from roles
**And** system roles (Administrator, Manager, Supervisor, Cashier, Waiter) cannot be deleted

---

### Story 2.4: User Management

As an administrator,
I want to create and manage user accounts,
So that staff members can access the system.

**Acceptance Criteria:**

**Given** the admin is logged in
**When** accessing user management
**Then** admin can create new users with: username, password, full name, email, phone
**And** admin can assign one or more roles to users
**And** admin can activate/deactivate user accounts
**And** admin can reset user passwords
**And** admin can set/change user PINs

---

### Story 2.5: Permission Checking

As the system,
I want to enforce permission checks on all protected actions,
So that users can only perform actions they are authorized for.

**Acceptance Criteria:**

**Given** a user is logged in
**When** attempting any protected action
**Then** system should check if user's role(s) have the required permission
**And** if permission exists, action should proceed
**And** if permission is missing, action should be blocked with "Unauthorized" message
**And** permission checks should be logged to audit trail

---

### Story 2.6: Permission Override with PIN

As a manager,
I want to authorize actions for users who lack permission,
So that operations can continue with proper oversight.

**Acceptance Criteria:**

**Given** a user's action is blocked due to missing permission
**When** the "Request Authorization" option is selected
**Then** a PIN entry dialog should appear
**And** an authorized user can enter their PIN
**And** if the authorizing user has the required permission, action should proceed
**And** both the original user and authorizing user should be logged in audit trail
**And** the override event should be logged with timestamp and reason

---

## Epic 3: Work Period Management

**Goal:** Implement work period (shift/business day) management that controls when transactions can be processed and provides end-of-day reconciliation.

**Dependencies:** Epic 2 (Authentication)

### Story 3.1: Open Work Period

As a manager,
I want to open a work period with an opening cash float,
So that the business day can begin and transactions can be processed.

**Acceptance Criteria:**

**Given** no work period is currently open
**When** a manager opens a new work period
**Then** system should prompt for opening cash float amount
**And** work period should be created with status "Open"
**And** opening timestamp and user should be recorded
**And** visual indicator should show work period is active on dashboard
**And** optional: previous day's closing balance can be carried forward

---

### Story 3.2: Work Period Status Display

As a user,
I want to see the current work period status,
So that I know if transactions can be processed.

**Acceptance Criteria:**

**Given** a user is logged in
**When** viewing the main screen
**Then** work period status should be prominently displayed (Open/Closed)
**And** if open, duration since opening should be shown
**And** if no work period is open, sales functions should be disabled
**And** clear message should indicate "Open work period to begin sales"

---

### Story 3.3: X Report Generation

As a manager,
I want to generate an X Report at any time during the work period,
So that I can see current sales status without closing the day.

**Acceptance Criteria:**

**Given** a work period is open
**When** X Report is requested
**Then** report should show:
- Sales summary (gross, discounts, net, tax)
- Sales by category
- Sales by payment method
- Sales by cashier/user
- Transaction counts and averages
- Voids summary (if any)
**And** report should be displayable on screen
**And** report should be printable on 80mm thermal printer
**And** counters should NOT be reset

---

### Story 3.4: Close Work Period with Z Report

As a manager,
I want to close the work period with cash count reconciliation,
So that the business day is properly finalized with complete records.

**Acceptance Criteria:**

**Given** a work period is open
**When** manager initiates work period close
**Then** system should warn if there are unsettled receipts
**And** system should prompt for physical cash count entry
**And** system should calculate expected cash and display variance
**And** Z Report should be automatically generated with:
- All X Report contents
- Final work period info (open time, close time, duration)
- Cash drawer reconciliation (opening, sales, payouts, expected, actual, variance)
- Sequential Z-Report number
**And** Z Report should be printed on 80mm thermal printer
**And** all transactions for the period should be locked
**And** work period status should change to "Closed"

---

## Epic 4: Product & Category Management

**Goal:** Enable management of the product catalog including categories, products, images, and pricing that will be displayed on the POS screen.

**Dependencies:** Epic 1 (Foundation)

### Story 4.1: Category Management

As an administrator,
I want to create and manage product categories,
So that products are organized for easy navigation.

**Acceptance Criteria:**

**Given** the admin is logged in
**When** accessing category management
**Then** admin can create categories with: name, parent category (optional), image, display order
**And** admin can edit existing categories
**And** admin can activate/deactivate categories
**And** admin can reorder categories via drag-and-drop or order number
**And** hierarchical subcategories should be supported

---

### Story 4.2: Product Creation

As an administrator,
I want to create products with all required information,
So that they can be sold through the POS.

**Acceptance Criteria:**

**Given** at least one category exists
**When** creating a new product
**Then** admin can enter: code/SKU, name, description, category, selling price, cost price
**And** admin can set: tax rate (default 16%), unit of measure, min/max stock levels
**And** admin can upload product image
**And** admin can enter barcode/QR code
**And** product code should be unique
**And** product should be active by default

---

### Story 4.3: Product Editing and Price Management

As an administrator,
I want to edit products and manage prices,
So that the catalog stays current.

**Acceptance Criteria:**

**Given** products exist in the system
**When** editing a product
**Then** admin can modify all product fields
**And** price changes should be logged in audit trail with old and new values
**And** admin can set effective date for price changes (optional)
**And** admin can activate/deactivate products
**And** inactive products should not appear on POS screen

---

### Story 4.4: Product Image Management

As an administrator,
I want to manage product images,
So that products are visually identifiable on the POS screen.

**Acceptance Criteria:**

**Given** a product exists
**When** managing product images
**Then** admin can upload JPG, PNG, or GIF images
**And** images should be automatically resized/optimized for display
**And** images should be stored locally in an organized folder structure
**And** products without images should show a placeholder icon
**And** image should be clearly visible on POS product tiles

---

### Story 4.5: Product Search and Listing

As a user,
I want to search and browse products,
So that I can quickly find items to add to orders.

**Acceptance Criteria:**

**Given** products exist in the system
**When** searching for products
**Then** user can search by product name, code, or barcode
**And** search results should appear within 500ms
**And** products can be filtered by category
**And** product list should show: image, name, price, stock status
**And** out-of-stock products should be visually indicated

---

## Epic 5: Sales & Order Management

**Goal:** Implement the core POS functionality for creating orders, selecting products, and managing the sales workflow with touch-optimized interface.

**Dependencies:** Epic 3 (Work Period), Epic 4 (Products)

### Story 5.1: Touch-Optimized Product Grid

As a waiter/cashier,
I want a touch-friendly product selection interface,
So that I can quickly add items to orders.

**Acceptance Criteria:**

**Given** a work period is open
**When** the POS screen is displayed
**Then** products should be shown as large, finger-friendly tiles (minimum 44x44 pixels)
**And** each tile should display: product image, name, price
**And** category tabs should allow quick navigation
**And** tapping a product should add it to the current order
**And** out-of-stock items should be visually marked and not selectable

---

### Story 5.2: Order Creation

As a waiter/cashier,
I want to create orders with products, quantities, and table assignment,
So that customer orders are properly recorded.

**Acceptance Criteria:**

**Given** the user is on the POS screen
**When** creating an order
**Then** user can add products by tapping on product tiles
**And** quantity can be adjusted with +/- buttons
**And** order can be associated with a table number
**And** optional customer name can be added
**And** running total (subtotal, tax, total) should update in real-time
**And** order notes/special instructions can be added
**And** order should be auto-saved to prevent data loss

---

### Story 5.3: Order Item Management

As a waiter/cashier,
I want to modify order items before printing,
So that I can correct mistakes or accommodate changes.

**Acceptance Criteria:**

**Given** an order has items
**When** managing order items
**Then** user can increase/decrease quantity
**And** user can remove items entirely
**And** user can add modifiers/notes to items (e.g., "no ice", "extra spicy")
**And** changes should reflect immediately in the order total
**And** user can apply item-level discounts (if permitted by role)

---

### Story 5.4: Kitchen Order Ticket (KOT) Printing

As a waiter,
I want to print kitchen order tickets,
So that the kitchen knows what to prepare.

**Acceptance Criteria:**

**Given** an order has been created
**When** the order is submitted/printed
**Then** KOT should be printed on the designated kitchen printer (80mm thermal)
**And** KOT should include: order number, table number, server name, timestamp
**And** KOT should list all items with quantities and modifiers
**And** items should be grouped by preparation station if configured
**And** font should be large and clear for kitchen readability

---

### Story 5.5: Incremental Order Printing

As a waiter,
I want only new items to print when I add to an existing order,
So that the kitchen doesn't reprint already-prepared items.

**Acceptance Criteria:**

**Given** an order has been printed to kitchen
**When** new items are added to the same order
**Then** only the NEW items should print on the KOT
**And** KOT should be clearly marked as "ADDITION" or "ADD-ON"
**And** KOT should reference the original order number
**And** system should track which items have been printed (batch number)
**And** customer receipt should show ALL items (original + additions)

---

### Story 5.6: Order Hold Functionality

As a cashier,
I want to hold an order without printing,
So that I can resume it later.

**Acceptance Criteria:**

**Given** an order is in progress
**When** the "Hold" button is pressed
**Then** order should be saved with status "On Hold"
**And** order should appear in a "Held Orders" list
**And** user can recall a held order and continue adding items
**And** held orders should be associated with the user who created them
**And** held orders should have a visual indicator in the list

---

## Epic 6: Receipt Management

**Goal:** Implement comprehensive receipt lifecycle management including creation, settlement, splitting, merging, and voiding with proper audit trails.

**Dependencies:** Epic 5 (Orders)

### Story 6.1: Receipt Creation

As the system,
I want receipts to be created from orders,
So that payments can be tracked and recorded.

**Acceptance Criteria:**

**Given** an order has been created and printed
**When** a receipt is generated
**Then** receipt should be created with unique receipt number
**And** receipt should be linked to the order and work period
**And** receipt should have status "Pending" (in manual settlement mode)
**And** receipt should be assigned to the user who created it (owner)
**And** receipt should store: subtotal, tax, discounts, total amount

---

### Story 6.2: Receipt Settlement

As a cashier,
I want to settle receipts by recording payment,
So that the sale is completed.

**Acceptance Criteria:**

**Given** a pending receipt exists
**When** the cashier settles the receipt
**Then** cashier must select payment method(s)
**And** payment amount must equal or exceed receipt total
**And** for cash payments, change should be calculated and displayed
**And** receipt status should change to "Settled"
**And** settlement timestamp and user should be recorded
**And** customer receipt should be printed on 80mm thermal printer

---

### Story 6.3: Receipt Ownership Enforcement

As the system,
I want to enforce that only receipt owners can modify their receipts,
So that accountability is maintained.

**Acceptance Criteria:**

**Given** a receipt belongs to a specific user
**When** another user tries to modify it (add items, settle)
**Then** system should block the action with "Not Authorized - Owner Only"
**And** manager override should be available with PIN
**And** all override attempts should be logged
**And** original owner and overriding manager should both appear in audit

---

### Story 6.4: Bill Splitting

As a cashier,
I want to split a receipt into multiple receipts,
So that customers can pay separately.

**Acceptance Criteria:**

**Given** a pending receipt with multiple items
**When** split bill is requested
**Then** user can choose to split:
- Equally by number of people
- By selecting specific items for each split
**And** items can be dragged between split receipts
**And** each split receipt gets a new receipt number referencing the original
**And** each split can be settled with different payment methods
**And** original receipt should maintain reference to split receipts
**And** split operation should be logged in audit trail

---

### Story 6.5: Bill Merging

As a cashier,
I want to merge multiple receipts into one,
So that a customer can pay for multiple tabs at once.

**Acceptance Criteria:**

**Given** multiple pending receipts exist
**When** merge is requested
**Then** user can select 2 or more receipts to merge
**And** only pending/unsettled receipts can be merged
**And** merged receipt should contain all items from source receipts
**And** source receipts should be archived with reference to merged receipt
**And** new totals should be calculated correctly
**And** merge operation should be logged in audit trail

---

### Story 6.6: Receipt Voiding

As a manager,
I want to void receipts with a mandatory reason,
So that erroneous transactions can be cancelled with proper documentation.

**Acceptance Criteria:**

**Given** a receipt exists (pending or settled)
**When** manager voids the receipt
**Then** void reason must be selected or entered (mandatory)
**And** receipt status should change to "Voided"
**And** voided receipt should remain visible in the system (marked as VOID)
**And** voided amount should not count toward sales totals
**And** inventory should be restored (stock returned) for voided items
**And** void should be recorded with: timestamp, user, reason
**And** voided receipts should appear in void report

---

## Epic 7: Payment Processing

**Goal:** Implement flexible payment processing supporting multiple payment methods including cash, M-Pesa, card, and split payments.

**Dependencies:** Epic 6 (Receipts)

### Story 7.1: Payment Method Configuration

As an administrator,
I want to configure available payment methods,
So that the business can accept various forms of payment.

**Acceptance Criteria:**

**Given** the admin is logged in
**When** configuring payment methods
**Then** admin can enable/disable payment methods: Cash, M-Pesa, Airtel Money, Credit Card, Debit Card
**And** admin can set display order for payment method buttons
**And** admin can configure if method requires reference number (e.g., M-Pesa code)
**And** changes should take effect immediately on POS screen

---

### Story 7.2: Cash Payment Processing

As a cashier,
I want to process cash payments with change calculation,
So that customers can pay with physical currency.

**Acceptance Criteria:**

**Given** a receipt is being settled
**When** cash payment is selected
**Then** total amount due should be prominently displayed
**And** cashier can enter amount tendered
**And** system should calculate and display change due
**And** quick-amount buttons should be available (exact, round up)
**And** cash drawer should open automatically (if configured)
**And** receipt should print showing cash amount and change

---

### Story 7.3: M-Pesa Payment Processing

As a cashier,
I want to process M-Pesa payments with transaction code capture,
So that mobile money payments are properly recorded.

**Acceptance Criteria:**

**Given** a receipt is being settled
**When** M-Pesa payment is selected
**Then** cashier should enter the M-Pesa transaction code
**And** amount should equal the receipt total (or partial for split payments)
**And** transaction code should be stored with the payment record
**And** receipt should print showing M-Pesa payment and transaction code

---

### Story 7.4: Card Payment Processing

As a cashier,
I want to record card payments,
So that credit/debit card transactions are documented.

**Acceptance Criteria:**

**Given** a receipt is being settled
**When** card payment is selected
**Then** payment amount should be recorded
**And** optionally, last 4 digits of card can be captured
**And** receipt should print showing card payment type
**And** no card processing integration required (external terminal)

---

### Story 7.5: Split Payment Processing

As a cashier,
I want to accept multiple payment methods for one receipt,
So that customers can pay with a combination of methods.

**Acceptance Criteria:**

**Given** a receipt is being settled
**When** split payment is needed
**Then** cashier can add multiple payment lines
**And** each line has: payment method, amount, reference (if required)
**And** running balance should show remaining amount due
**And** settlement cannot complete until full amount is covered
**And** all payment methods should appear on printed receipt

---

### Story 7.6: Card Payment Gateway Integration (Pesapal/DPO)

As an administrator,
I want to integrate with Kenyan payment gateways (Pesapal, DPO, Cellulant),
So that card payments are processed automatically without manual terminal entry.

**Acceptance Criteria:**

1. **Given** gateway configuration needed
   **When** setting up integration
   **Then** can configure: API credentials, merchant ID, callback URLs, sandbox/production mode

2. **Given** card payment selected
   **When** initiating transaction
   **Then** sends payment request to gateway with amount, reference, and customer details

3. **Given** payment processing
   **When** gateway responds
   **Then** handles success, failure, and pending states appropriately

4. **Given** successful payment
   **When** callback received
   **Then** automatically settles receipt and stores transaction reference

5. **Given** payment failed or timeout
   **When** handling error
   **Then** allows retry or fallback to manual entry mode

6. **Given** refund needed
   **When** processing void with gateway payment
   **Then** initiates refund through gateway API

7. **Given** end of day
   **When** reconciling
   **Then** can match POS transactions with gateway settlement report

---

## Epic 8: Inventory Management

**Goal:** Implement real-time inventory tracking with automatic stock deductions, adjustments, alerts, and stock-taking functionality.

**Dependencies:** Epic 4 (Products), Epic 5 (Sales)

### Story 8.1: Automatic Stock Deduction

As the system,
I want to automatically deduct stock when items are sold,
So that inventory levels stay accurate in real-time.

**Acceptance Criteria:**

**Given** a product has inventory tracking enabled
**When** the product is sold (receipt settled)
**Then** stock quantity should be deducted by the sold amount
**And** stock movement should be recorded with reference to the order
**And** previous and new stock levels should be logged
**And** if stock reaches zero, product should be flagged as out-of-stock

---

### Story 8.2: Stock Level Display and Alerts

As a manager,
I want to see current stock levels and receive low-stock alerts,
So that I can reorder products before they run out.

**Acceptance Criteria:**

**Given** products have stock levels configured
**When** stock falls below minimum level
**Then** low-stock alert should appear on dashboard
**And** out-of-stock products should be prominently flagged
**And** inventory view should show: product, current stock, min level, status
**And** color coding should indicate: green (OK), yellow (low), red (out)

---

### Story 8.3: Manual Stock Adjustment

As a manager,
I want to adjust stock quantities manually,
So that discrepancies can be corrected.

**Acceptance Criteria:**

**Given** a product exists in inventory
**When** manager performs stock adjustment
**Then** manager can increase or decrease stock quantity
**And** adjustment reason must be selected/entered (mandatory)
**And** adjustment should be authorized by manager role or above
**And** stock movement should be logged with reason and user
**And** previous and new quantities should be recorded

---

### Story 8.4: Stock Return on Void

As the system,
I want to restore stock when receipts are voided,
So that inventory remains accurate.

**Acceptance Criteria:**

**Given** a receipt with inventory-tracked items is voided
**When** the void is processed
**Then** stock should be increased by the voided quantities
**And** stock movement should be recorded with void reference
**And** movement type should be "Void/Return"

---

### Story 8.5: Stock Take (Physical Inventory)

As a manager,
I want to conduct stock takes and reconcile physical counts,
So that system inventory matches actual inventory.

**Acceptance Criteria:**

**Given** products exist in the system
**When** stock take is initiated
**Then** user can enter physical count for each product
**And** system should calculate variance (system vs physical)
**And** variance value (quantity * cost) should be calculated
**And** manager can approve adjustments to align system with physical counts
**And** stock take report should be generated showing all variances
**And** approved adjustments should create stock movements

---

## Epic 9: Purchase & Supplier Management

**Goal:** Implement supplier management and purchase receiving functionality to track incoming inventory.

**Dependencies:** Epic 8 (Inventory)

### Story 9.1: Supplier Management

As an administrator,
I want to manage supplier information,
So that purchases can be tracked by vendor.

**Acceptance Criteria:**

**Given** admin access
**When** managing suppliers
**Then** admin can create suppliers with: name, contact person, phone, email, address
**And** admin can edit and deactivate suppliers
**And** supplier list should be searchable
**And** active supplier count should be displayed

---

### Story 9.2: Purchase Order Creation

As a manager,
I want to create purchase orders,
So that expected deliveries are documented.

**Acceptance Criteria:**

**Given** suppliers and products exist
**When** creating a purchase order
**Then** user can select supplier
**And** user can add products with quantities and expected prices
**And** PO should calculate total cost
**And** PO should have status: Draft, Sent, Partially Received, Complete
**And** PO should be printable on 80mm thermal printer

---

### Story 9.3: Goods Receiving (with PO)

As a stock clerk,
I want to receive goods against a purchase order,
So that inventory is updated with accurate quantities.

**Acceptance Criteria:**

**Given** a purchase order has been created
**When** goods are received
**Then** user can select the PO to receive against
**And** user can enter received quantities (may differ from ordered)
**And** actual cost prices can be recorded
**And** partial receiving is supported
**And** stock is automatically increased on save
**And** Goods Received Note (GRN) is generated and printable

---

### Story 9.4: Direct Goods Receiving (without PO)

As a stock clerk,
I want to receive goods without a purchase order,
So that ad-hoc deliveries can be recorded.

**Acceptance Criteria:**

**Given** products and suppliers exist
**When** receiving goods directly
**Then** user can select supplier (optional)
**And** user can add products with quantities and costs
**And** stock is automatically increased on save
**And** GRN is generated for the direct receiving

---

## Epic 10: Reporting & Analytics

**Goal:** Implement comprehensive reporting including X/Z reports, sales analysis, inventory reports, and audit trails with 80mm thermal printer optimization.

**Dependencies:** Epic 3 (Work Period), Epic 5 (Sales), Epic 8 (Inventory)

### Story 10.1: Sales Reports

As a manager,
I want comprehensive sales reports,
So that I can analyze business performance.

**Acceptance Criteria:**

**Given** sales data exists
**When** generating sales reports
**Then** available reports should include:
- Daily Sales Summary
- Sales by Product
- Sales by Category
- Sales by Cashier/User
- Sales by Payment Method
- Hourly Sales Analysis
**And** reports can be filtered by date range
**And** reports can be viewed on screen or printed (80mm optimized)

---

### Story 10.2: Void and Discount Reports

As a manager,
I want void and discount reports,
So that I can monitor exceptions and potential issues.

**Acceptance Criteria:**

**Given** voids and discounts have occurred
**When** generating exception reports
**Then** void report should show: receipt #, amount, user, authorizer, reason, timestamp
**And** discount report should show: receipt #, original amount, discount, user
**And** reports can be filtered by date range and user
**And** totals should be calculated for each report

---

### Story 10.3: Inventory Reports

As a manager,
I want inventory reports,
So that I can monitor stock levels and movement.

**Acceptance Criteria:**

**Given** inventory data exists
**When** generating inventory reports
**Then** available reports should include:
- Current Stock Report (all products with quantities)
- Low Stock Report (products below minimum)
- Stock Movement Report (all ins/outs with reasons)
- Stock Valuation Report (quantity * cost)
- Dead Stock Report (no movement in X days)
**And** reports can be filtered by category
**And** reports are formatted for 80mm thermal printing

---

### Story 10.4: Audit Trail Reports

As an administrator,
I want audit trail reports,
So that all system activities can be reviewed.

**Acceptance Criteria:**

**Given** audit data is being captured
**When** generating audit reports
**Then** available reports should include:
- User Activity Log (login, logout, actions)
- Transaction Log (all sales activities)
- Void/Refund Log
- Price Change Log
- Permission Override Log
**And** reports can be filtered by user, date range, action type
**And** reports show: timestamp, user, action, before/after values

---

### Story 10.5: Report Export

As a manager,
I want to export reports to common formats,
So that data can be shared or further analyzed.

**Acceptance Criteria:**

**Given** a report is generated
**When** export is requested
**Then** report can be exported to CSV format
**And** optionally PDF format (if time permits)
**And** export file should be saved to user-selected location
**And** file naming should include report type and date

---

## Epic 11: Table & Floor Management

**Goal:** Implement visual table/floor management for restaurant operations with table status tracking.

**Dependencies:** Epic 5 (Orders)

### Story 11.1: Floor Plan Configuration

As an administrator,
I want to configure the floor plan with tables,
So that orders can be associated with physical locations.

**Acceptance Criteria:**

**Given** admin access
**When** configuring floor plan
**Then** admin can add tables with: number, capacity, section/zone
**And** admin can define multiple floors (if applicable)
**And** admin can set table positions on a visual grid
**And** tables can be activated/deactivated

---

### Story 11.2: Table Status Display

As a waiter,
I want to see table statuses at a glance,
So that I know which tables need attention.

**Acceptance Criteria:**

**Given** floor plan is configured
**When** viewing table map
**Then** tables should show visual status: Available (green), Occupied (red), Reserved (yellow)
**And** occupied tables should show: assigned waiter, current bill amount
**And** time since seated can be displayed (optional)
**And** tapping a table should open/show its current order

---

### Story 11.3: Table Transfer

As a waiter,
I want to transfer a table to another waiter,
So that handoffs between shifts are smooth.

**Acceptance Criteria:**

**Given** a table is assigned to a waiter
**When** transfer is requested
**Then** waiter can select another active user to transfer to
**And** all pending orders/receipts transfer with the table
**And** transfer should be logged in audit trail
**And** both original and new waiter appear in log

---

## Epic 12: Printing & Hardware Integration

**Goal:** Implement reliable printing to 80mm thermal printers for receipts, kitchen orders, and reports, plus cash drawer integration.

**Dependencies:** Epic 5 (Orders), Epic 6 (Receipts)

### Story 12.1: Receipt Printer Configuration

As an administrator,
I want to configure the receipt printer,
So that customer receipts print correctly.

**Acceptance Criteria:**

**Given** a thermal printer is connected
**When** configuring printer settings
**Then** admin can specify: printer name/port (USB, Serial, Network)
**And** admin can configure receipt header (business name, address, phone)
**And** admin can configure receipt footer (thank you message)
**And** test print functionality should be available
**And** printer status should be monitored

---

### Story 12.2: Kitchen Printer Configuration

As an administrator,
I want to configure kitchen printer(s),
So that orders route to the correct preparation stations.

**Acceptance Criteria:**

**Given** kitchen printer(s) are connected
**When** configuring kitchen printing
**Then** admin can add multiple kitchen printers
**And** each printer can be assigned to product categories
**And** KOT format can be configured (font size, item grouping)
**And** test print should be available for each printer

---

### Story 12.3: ESC/POS Print Implementation

As a developer,
I want reliable ESC/POS printing,
So that all print jobs complete successfully on thermal printers.

**Acceptance Criteria:**

**Given** printers are configured
**When** any print job is triggered
**Then** system should generate proper ESC/POS commands
**And** print jobs should support: text styles (bold, large), alignment, cut
**And** graphics/logo printing should be supported (basic)
**And** print queue should handle multiple jobs gracefully
**And** print errors should be reported to user with retry option

---

### Story 12.4: Cash Drawer Integration

As a cashier,
I want the cash drawer to open automatically on cash transactions,
So that I don't have to manually open it.

**Acceptance Criteria:**

**Given** cash drawer is connected (via printer RJ11 port)
**When** cash payment is processed
**Then** drawer should open automatically
**And** manual drawer open should be available (with permission)
**And** all drawer open events should be logged
**And** drawer can also open via receipt printer kick command

---

## Epic Retrospective Template

After completing each epic, conduct a retrospective covering:
- What went well
- What could be improved
- Lessons learned
- Impact on subsequent epics
- Technical debt incurred

---

## Appendix: Story Status Tracking

| Story ID | Title | Status | Notes |
|----------|-------|--------|-------|
| 1.1 | Project Setup and Solution Structure | backlog | |
| 1.2 | Database Schema Creation | backlog | |
| 1.3 | Entity Framework Core Configuration | backlog | |
| 1.4 | Base Repository Pattern Implementation | backlog | |
| 1.5 | MVVM Base Infrastructure | backlog | |
| 2.1 | User Login Screen | backlog | |
| 2.2 | Password Management | backlog | |
| 2.3 | Role Management | backlog | |
| 2.4 | User Management | backlog | |
| 2.5 | Permission Checking | backlog | |
| 2.6 | Permission Override with PIN | backlog | |
| 3.1 | Open Work Period | backlog | |
| 3.2 | Work Period Status Display | backlog | |
| 3.3 | X Report Generation | backlog | |
| 3.4 | Close Work Period with Z Report | backlog | |
| 4.1 | Category Management | backlog | |
| 4.2 | Product Creation | backlog | |
| 4.3 | Product Editing and Price Management | backlog | |
| 4.4 | Product Image Management | backlog | |
| 4.5 | Product Search and Listing | backlog | |
| 5.1 | Touch-Optimized Product Grid | backlog | |
| 5.2 | Order Creation | backlog | |
| 5.3 | Order Item Management | backlog | |
| 5.4 | Kitchen Order Ticket (KOT) Printing | backlog | |
| 5.5 | Incremental Order Printing | backlog | |
| 5.6 | Order Hold Functionality | backlog | |
| 6.1 | Receipt Creation | backlog | |
| 6.2 | Receipt Settlement | backlog | |
| 6.3 | Receipt Ownership Enforcement | backlog | |
| 6.4 | Bill Splitting | backlog | |
| 6.5 | Bill Merging | backlog | |
| 6.6 | Receipt Voiding | backlog | |
| 7.1 | Payment Method Configuration | backlog | |
| 7.2 | Cash Payment Processing | backlog | |
| 7.3 | M-Pesa Payment Processing | backlog | |
| 7.4 | Card Payment Processing | backlog | |
| 7.5 | Split Payment Processing | backlog | |
| 8.1 | Automatic Stock Deduction | backlog | |
| 8.2 | Stock Level Display and Alerts | backlog | |
| 8.3 | Manual Stock Adjustment | backlog | |
| 8.4 | Stock Return on Void | backlog | |
| 8.5 | Stock Take (Physical Inventory) | backlog | |
| 9.1 | Supplier Management | backlog | |
| 9.2 | Purchase Order Creation | backlog | |
| 9.3 | Goods Receiving (with PO) | backlog | |
| 9.4 | Direct Goods Receiving (without PO) | backlog | |
| 10.1 | Sales Reports | backlog | |
| 10.2 | Void and Discount Reports | backlog | |
| 10.3 | Inventory Reports | backlog | |
| 10.4 | Audit Trail Reports | backlog | |
| 10.5 | Report Export | backlog | |
| 11.1 | Floor Plan Configuration | backlog | |
| 11.2 | Table Status Display | backlog | |
| 11.3 | Table Transfer | backlog | |
| 12.1 | Receipt Printer Configuration | backlog | |
| 12.2 | Kitchen Printer Configuration | backlog | |
| 12.3 | ESC/POS Print Implementation | backlog | |
| 12.4 | Cash Drawer Integration | backlog | |
| **13.1** | **Business Mode Configuration** | **backlog** | |
| **13.2** | **Feature Toggle Management** | **backlog** | |
| **13.3** | **Mode-Aware UI Shell** | **backlog** | |
| **14.1** | **Product Offer Configuration** | **backlog** | |
| **14.2** | **Active Offer Application** | **backlog** | |
| **14.3** | **Offer Price Display** | **backlog** | |
| **14.4** | **Offer Reporting** | **backlog** | |
| **15.1** | **Supplier Payment Terms** | **backlog** | |
| **15.2** | **Delivery Payment Status** | **backlog** | |
| **15.3** | **Supplier Invoice Tracking** | **backlog** | |
| **15.4** | **Accounts Payable Reports** | **backlog** | |
| **16.1** | **Employee Management** | **backlog** | |
| **16.2** | **Salary Component Configuration** | **backlog** | |
| **16.3** | **Payroll Period Management** | **backlog** | |
| **16.4** | **Payslip Generation** | **backlog** | |
| **16.5** | **Payroll Reports** | **backlog** | |
| **17.1** | **Chart of Accounts Setup** | **backlog** | |
| **17.2** | **Expense Management** | **backlog** | |
| **17.3** | **Auto Journal Posting** | **backlog** | |
| **17.4** | **Trial Balance Report** | **backlog** | |
| **17.5** | **Income Statement & Balance Sheet** | **backlog** | |
| **29.1** | **BOGO Promotions (Buy One Get One)** | **backlog** | |
| **29.2** | **Mix & Match Promotions** | **backlog** | |
| **29.3** | **Quantity Break Pricing** | **backlog** | |
| **29.4** | **Combo/Bundle Deals** | **backlog** | |
| **29.5** | **Coupon Management** | **backlog** | |
| **29.6** | **Automatic Markdown Management** | **backlog** | |
| **30.1** | **Customer Credit Account Setup** | **backlog** | |
| **30.2** | **Credit Sale Processing** | **backlog** | |
| **30.3** | **Accounts Receivable Aging** | **backlog** | |
| **30.4** | **Customer Payment Recording** | **backlog** | |
| **30.5** | **Customer Statement Generation** | **backlog** | |
| **31.1** | **Bank Account Configuration** | **backlog** | |
| **31.2** | **Bank Statement Import** | **backlog** | |
| **31.3** | **Automatic Transaction Matching** | **backlog** | |
| **31.4** | **Manual Matching and Reconciliation** | **backlog** | |
| **32.1** | **PMS Connection Configuration** | **backlog** | |
| **32.2** | **Guest Lookup and Verification** | **backlog** | |
| **32.3** | **Room Charge Posting** | **backlog** | |
| **32.4** | **Guest Folio Display** | **backlog** | |
| **32.5** | **Table Reservation System** | **backlog** | |
| **32.6** | **Package/Meal Plan Handling** | **backlog** | |
| **33.1** | **API Framework Setup** | **backlog** | |
| **33.2** | **API Authentication (JWT/OAuth)** | **backlog** | |
| **33.3** | **Core API Endpoints** | **backlog** | |
| **33.4** | **Webhook Event System** | **backlog** | |
| **33.5** | **API Documentation (OpenAPI/Swagger)** | **backlog** | |
| **34.1** | **Airtel Money Integration** | **backlog** | |
| **34.2** | **Airtel Money Payment Processing** | **backlog** | |
| **34.3** | **T-Kash Integration** | **backlog** | |
| **35.1** | **Cash Flow Statement** | **backlog** | |
| **35.2** | **General Ledger Report** | **backlog** | |
| **35.3** | **Gross Margin Analysis** | **backlog** | |
| **35.4** | **Comparative Financial Reports** | **backlog** | |
| **35.5** | **Departmental P&L** | **backlog** | |
| **36.1** | **Budget Creation** | **backlog** | |
| **36.2** | **Budget vs Actual Tracking** | **backlog** | |
| **36.3** | **Recurring Expense Templates** | **backlog** | |
| **36.4** | **Cost Center/Department Setup** | **backlog** | |
| **37.1** | **Suspended Transactions (Park/Recall)** | **backlog** | |
| **37.2** | **Customer-Facing Display** | **backlog** | |
| **37.3** | **Enhanced Split Payment** | **backlog** | |
| **37.4** | **Quick Amount Buttons** | **backlog** | |
| **38.1** | **Stock Valuation Methods** | **backlog** | |
| **38.2** | **Automatic Reorder Generation** | **backlog** | |
| **38.3** | **Shrinkage Analysis** | **backlog** | |
| **38.4** | **Dead Stock Identification** | **backlog** | |

---

## Epic 13: System Mode Configuration (NEW)

**Goal:** Enable the system to operate in different business modes (Restaurant, Supermarket, Hybrid) with appropriate feature sets and UI layouts for each mode.

**Dependencies:** Epic 1 (Foundation)
**Mode:** All

### Story 13.1: Business Mode Configuration

As an administrator,
I want to select the business mode during initial setup,
So that the system is configured with the appropriate features for my business type.

**Acceptance Criteria:**

1. **Given** a fresh installation
   **When** the setup wizard runs
   **Then** administrator can select: Restaurant, Supermarket, or Hybrid mode

2. **Given** a mode is selected
   **When** configuration is saved
   **Then** appropriate feature flags are set based on mode

3. **Given** the system is running
   **When** mode needs to change
   **Then** administrator can switch modes from Settings (requires restart)

---

### Story 13.2: Feature Toggle Management

As an administrator,
I want to enable or disable specific features independently,
So that I can customize the system beyond the default mode settings.

**Acceptance Criteria:**

1. **Given** a configured mode
   **When** accessing Feature Settings
   **Then** all features show current enabled/disabled state

2. **Given** a feature is toggled
   **When** changes are saved
   **Then** the feature is immediately enabled/disabled in the UI

3. **Given** a feature toggle
   **When** viewing dependencies
   **Then** related features show dependency warnings if applicable

---

### Story 13.3: Mode-Aware UI Shell

As a user,
I want the main UI to adapt to the business mode,
So that I see the most relevant layout for my work.

**Acceptance Criteria:**

1. **Given** Restaurant mode
   **When** main screen loads
   **Then** three-panel layout displays (Order Ticket | Categories | Products)

2. **Given** Supermarket mode
   **When** main screen loads
   **Then** barcode-focused layout displays with auto-focus search

3. **Given** Hybrid mode
   **When** main screen loads
   **Then** toggle button allows switching between layouts

---

## Epic 14: Product Offers & Promotions (NEW)

**Goal:** Enable supermarkets to create and manage promotional offers on products with automatic price application at checkout.

**Dependencies:** Epic 4 (Products), Epic 13 (Mode Config)
**Mode:** Supermarket, Hybrid

### Story 14.1: Product Offer Configuration

As a manager,
I want to create promotional offers for products,
So that I can run sales and attract customers.

**Acceptance Criteria:**

1. **Given** access to Offer Management
   **When** creating an offer
   **Then** can specify: product, offer name, offer price or discount %, start date, end date

2. **Given** offer details entered
   **When** saving the offer
   **Then** offer is validated (end date > start date, price > 0)

3. **Given** an active offer exists
   **When** editing the offer
   **Then** changes apply immediately if within date range

---

### Story 14.2: Active Offer Application

As a cashier,
I want offers to be automatically applied at checkout,
So that customers get the correct promotional price.

**Acceptance Criteria:**

1. **Given** a product has an active offer
   **When** product is added to order
   **Then** offer price is applied automatically

2. **Given** multiple offers might apply
   **When** product is scanned
   **Then** only the best offer applies (no stacking)

3. **Given** offer has quantity requirements
   **When** minimum quantity is met
   **Then** offer applies only to qualifying quantity

---

### Story 14.3: Offer Price Display

As a cashier,
I want to see both original and offer prices,
So that I can explain savings to customers.

**Acceptance Criteria:**

1. **Given** a product with active offer
   **When** viewing in product grid
   **Then** tile shows "OFFER" badge and both prices

2. **Given** a product on order with offer
   **When** viewing order line
   **Then** shows original price (strikethrough), offer price, savings

3. **Given** receipt is printed
   **When** offer product is included
   **Then** receipt shows both prices and savings

---

### Story 14.4: Offer Reporting

As a manager,
I want to see how offers are performing,
So that I can evaluate promotional effectiveness.

**Acceptance Criteria:**

1. **Given** offers have been used
   **When** running Offer Report
   **Then** shows redemption count, revenue, and discount given per offer

2. **Given** date range selected
   **When** filtering report
   **Then** shows only offers active in that period

---

## Epic 15: Supplier Credit Management (NEW)

**Goal:** Enable tracking of supplier payment terms and accounts payable for supermarket operations.

**Dependencies:** Epic 9 (Purchase Orders), Epic 13 (Mode Config)
**Mode:** Supermarket, Hybrid

### Story 15.1: Supplier Payment Terms

As a procurement manager,
I want to configure payment terms for each supplier,
So that I know when payments are due.

**Acceptance Criteria:**

1. **Given** creating/editing a supplier
   **When** entering details
   **Then** can set Payment Terms: COD, Net 15, Net 30, Net 60

2. **Given** supplier has credit terms
   **When** viewing supplier
   **Then** shows current balance owed and credit limit

---

### Story 15.2: Delivery Payment Status

As a receiving clerk,
I want to mark whether a delivery is paid or on credit,
So that accounts payable is accurately tracked.

**Acceptance Criteria:**

1. **Given** receiving goods on a PO
   **When** completing receipt
   **Then** must select: Paid Now or Credit (use supplier terms)

2. **Given** Credit is selected
   **When** delivery is completed
   **Then** supplier balance is updated, due date calculated from terms

3. **Given** Paid Now is selected
   **When** delivery is completed
   **Then** no accounts payable is created

---

### Story 15.3: Supplier Invoice Tracking

As an accounts clerk,
I want to track supplier invoices and payments,
So that I know what we owe and when.

**Acceptance Criteria:**

1. **Given** a credit delivery received
   **When** invoice is entered
   **Then** can record supplier's invoice number and date

2. **Given** invoices exist
   **When** viewing Accounts Payable
   **Then** list shows all unpaid invoices with due dates

3. **Given** making a payment
   **When** recording payment
   **Then** can apply to one or more invoices (partial or full)

---

### Story 15.4: Accounts Payable Reports

As a manager,
I want accounts payable reports,
So that I can manage cash flow and supplier relationships.

**Acceptance Criteria:**

1. **Given** unpaid invoices exist
   **When** running AP Aging Report
   **Then** shows invoices grouped by age (Current, 30, 60, 90+ days)

2. **Given** a supplier selected
   **When** running Supplier Statement
   **Then** shows all transactions and running balance

---

### Story 15.5: Withholding Tax Integration

As an accountant,
I want to automatically calculate and track withholding tax on supplier payments,
So that WHT is correctly deducted and reported to KRA.

**Acceptance Criteria:**

1. **Given** WHT configuration needed
   **When** setting up
   **Then** can configure WHT rates by payment type: Professional Services (5%), Contractual (3%), Management Fees (5%), Dividends (5%)

2. **Given** supplier has PIN registered
   **When** creating supplier
   **Then** can mark supplier as WHT-applicable with their KRA PIN

3. **Given** payment to WHT-applicable supplier
   **When** processing payment above threshold (KES 24,000/year)
   **Then** automatically calculates WHT deduction from gross amount

4. **Given** WHT deducted
   **When** completing payment
   **Then** records: gross amount, WHT amount, net payment, supplier PIN

5. **Given** WHT certificate needed
   **When** generating
   **Then** creates KRA-format certificate with: payer details, payee details, gross amount, WHT rate, WHT amount, period

6. **Given** end of month
   **When** running WHT report
   **Then** shows all deductions for monthly KRA return submission

7. **Given** WHT remittance
   **When** marking as remitted
   **Then** records remittance date and reference for audit trail

---

## Epic 16: Employee & Payroll Management (NEW)

**Goal:** Provide basic employee record management and payroll processing for supermarket staff.

**Dependencies:** Epic 2 (Users), Epic 13 (Mode Config)
**Mode:** Supermarket, Hybrid

### Story 16.1: Employee Management

As an HR manager,
I want to maintain employee records,
So that I have all staff information in one place.

**Acceptance Criteria:**

1. **Given** access to Employee module
   **When** creating an employee
   **Then** can enter: name, ID, contact, position, salary, bank details, statutory IDs

2. **Given** an employee exists
   **When** editing record
   **Then** changes are saved with audit trail

3. **Given** employee leaves
   **When** terminating employment
   **Then** record is deactivated with termination date, not deleted

---

### Story 16.2: Salary Component Configuration

As an administrator,
I want to configure salary components,
So that payroll calculations are automated.

**Acceptance Criteria:**

1. **Given** accessing Salary Settings
   **When** creating component
   **Then** can specify: name, type (earning/deduction), fixed/percentage, taxable

2. **Given** statutory deductions
   **When** configured
   **Then** PAYE, NSSF, NHIF are auto-calculated based on rates

---

### Story 16.3: Payroll Period Management

As a payroll administrator,
I want to create and process payroll periods,
So that employees are paid on schedule.

**Acceptance Criteria:**

1. **Given** new pay period needed
   **When** creating period
   **Then** specify: period name, date range, pay date

2. **Given** a period is open
   **When** processing payroll
   **Then** calculates earnings and deductions for all active employees

3. **Given** payroll is processed
   **When** reviewing
   **Then** period must be approved before payment

---

### Story 16.4: Payslip Generation

As an employee,
I want to receive a detailed payslip,
So that I understand my pay breakdown.

**Acceptance Criteria:**

1. **Given** approved payroll
   **When** generating payslips
   **Then** creates individual payslip for each employee

2. **Given** a payslip
   **When** viewing/printing
   **Then** shows: basic salary, all earnings, all deductions, net pay

---

### Story 16.5: Payroll Reports

As a manager,
I want payroll reports,
So that I can track labor costs.

**Acceptance Criteria:**

1. **Given** a payroll period
   **When** running Payroll Summary
   **Then** shows totals for all components and net pay

2. **Given** date range
   **When** running Payroll History
   **Then** shows payroll totals by period

---

### Story 16.6: Housing Levy Calculation

As a payroll administrator,
I want the system to automatically calculate Housing Levy deductions,
So that we comply with Kenya's 2024 statutory requirements.

**Acceptance Criteria:**

1. **Given** Housing Levy is mandatory
   **When** configuring statutory deductions
   **Then** Housing Levy is enabled with rate of 1.5% of gross salary

2. **Given** employee salary processed
   **When** calculating deductions
   **Then** calculates employee contribution: 1.5% of gross salary

3. **Given** employer contribution required
   **When** calculating payroll costs
   **Then** calculates employer contribution: 1.5% of gross salary (matching)

4. **Given** payslip generated
   **When** displaying deductions
   **Then** shows Housing Levy as separate line item with amount

5. **Given** end of month
   **When** generating statutory report
   **Then** shows total Housing Levy: employee portion + employer portion

6. **Given** remittance needed
   **When** preparing for NSSF portal
   **Then** exports Housing Levy data in required format for remittance

---

## Epic 17: Accounting Module (NEW)

**Goal:** Provide semi-accounting functionality with general ledger, expense tracking, and basic financial reports.

**Dependencies:** Epics 5-9, 15, 16 (for auto-posting)
**Mode:** Supermarket, Hybrid

### Story 17.1: Chart of Accounts Setup

As an accountant,
I want a chart of accounts,
So that transactions are properly categorized.

**Acceptance Criteria:**

1. **Given** first-time setup
   **When** accounting module initializes
   **Then** default chart of accounts is created (Assets, Liabilities, Revenue, Expenses)

2. **Given** existing accounts
   **When** adding custom accounts
   **Then** can create under appropriate parent category

---

### Story 17.2: Expense Management

As an accounts clerk,
I want to record business expenses,
So that all costs are tracked.

**Acceptance Criteria:**

1. **Given** accessing Expenses
   **When** creating expense
   **Then** enter: category, description, amount, date, payment method

2. **Given** expense entered
   **When** saving
   **Then** can attach receipt image (optional)

3. **Given** expenses require approval
   **When** manager reviews
   **Then** can approve or reject with reason

---

### Story 17.3: Auto Journal Posting

As the system,
I want to automatically create journal entries for transactions,
So that the ledger is always current without manual entry.

**Acceptance Criteria:**

1. **Given** a sale is completed
   **When** receipt is settled
   **Then** journal entry posts: Dr Cash/AR, Cr Sales Revenue

2. **Given** a purchase is received on credit
   **When** delivery is confirmed
   **Then** journal entry posts: Dr Inventory, Cr Accounts Payable

3. **Given** payroll is approved
   **When** period is closed
   **Then** journal entry posts: Dr Salaries Expense, Cr Cash/Salaries Payable

---

### Story 17.4: Trial Balance Report

As an accountant,
I want a trial balance report,
So that I can verify accounts are balanced.

**Acceptance Criteria:**

1. **Given** journal entries exist
   **When** running Trial Balance
   **Then** shows all accounts with debit and credit balances

2. **Given** trial balance
   **When** viewing totals
   **Then** total debits must equal total credits

---

### Story 17.5: Income Statement & Balance Sheet

As a manager,
I want financial statements,
So that I can understand business performance.

**Acceptance Criteria:**

1. **Given** date range selected
   **When** running Income Statement
   **Then** shows: Revenue - Expenses = Net Income

2. **Given** a date selected
   **When** running Balance Sheet
   **Then** shows: Assets = Liabilities + Equity

3. **Given** financial reports
   **When** exporting
   **Then** can export to PDF or Excel

---

## Epic 18: Kenya eTIMS Compliance (MANDATORY)

**Goal:** Implement KRA Electronic Tax Invoice Management System (eTIMS) integration for all transactions, ensuring 100% tax compliance with Kenya Revenue Authority requirements.

**Dependencies:** Epic 6 (Receipts), Epic 7 (Payments)
**Mode:** All (Mandatory for Kenya deployment)

### Story 18.1: eTIMS Control Unit Registration

As an administrator,
I want to register and activate the eTIMS Control Unit,
So that the POS can communicate with KRA servers.

**Acceptance Criteria:**

1. **Given** the system is being set up
   **When** configuring eTIMS
   **Then** admin can enter Control Unit Number (CUN) assigned by KRA

2. **Given** CUN is entered
   **When** activating with KRA
   **Then** system validates and activates the control unit

3. **Given** activation is complete
   **When** viewing eTIMS status
   **Then** shows CU Serial, activation status, and last sync time

---

### Story 18.2: KRA-Compliant Invoice Generation

As the system,
I want to generate invoices with all KRA-required fields,
So that every transaction is tax compliant.

**Acceptance Criteria:**

1. **Given** a transaction is completed
   **When** generating the invoice
   **Then** includes: Seller PIN, Buyer PIN (if provided), sequential invoice number, all line items with tax codes

2. **Given** invoice is generated
   **When** calculating taxes
   **Then** correctly applies 16% VAT, 0% VAT Exempt, or 0% Zero-Rated per item

3. **Given** invoice is complete
   **When** generating receipt
   **Then** includes eTIMS Control Code and QR code for verification

---

### Story 18.3: Real-Time eTIMS Submission

As the system,
I want to submit invoices to KRA in real-time,
So that tax records are immediately registered.

**Acceptance Criteria:**

1. **Given** a transaction is completed
   **When** internet is available
   **Then** invoice is submitted to KRA eTIMS API within 30 seconds

2. **Given** submission is successful
   **When** receiving KRA response
   **Then** stores Control Code and updates invoice record

3. **Given** submission fails
   **When** handling error
   **Then** queues for retry with exponential backoff

---

### Story 18.4: Offline eTIMS Queue Management

As the system,
I want to queue invoices when offline and sync when connected,
So that offline operation doesn't break tax compliance.

**Acceptance Criteria:**

1. **Given** no internet connection
   **When** completing a transaction
   **Then** receipt shows "eTIMS Pending" and invoice queued locally

2. **Given** connection is restored
   **When** sync service runs
   **Then** all pending invoices submitted to KRA in order

3. **Given** invoices are synced
   **When** viewing dashboard
   **Then** shows pending count and last successful sync time

---

### Story 18.5: eTIMS Credit Note Submission

As a manager,
I want to submit credit notes to KRA for voids and returns,
So that tax adjustments are properly recorded.

**Acceptance Criteria:**

1. **Given** a receipt is voided
   **When** processing the void
   **Then** credit note is generated referencing original invoice

2. **Given** credit note is created
   **When** submitting to KRA
   **Then** uses eTIMS credit note endpoint with reason code

3. **Given** submission is successful
   **When** viewing reports
   **Then** credit note appears in eTIMS reconciliation

---

### Story 18.6: eTIMS Status Dashboard

As a manager,
I want to monitor eTIMS compliance status,
So that I can ensure all invoices are registered with KRA.

**Acceptance Criteria:**

1. **Given** accessing eTIMS dashboard
   **When** viewing status
   **Then** shows: Submitted today (count/value), Pending, Failed, CU Status

2. **Given** failed submissions exist
   **When** viewing details
   **Then** shows error reason and retry status

3. **Given** CU issues detected
   **When** alerting admin
   **Then** displays prominent warning with resolution steps

---

### Story 18.7: KRA PIN Validation API

As a credit controller,
I want to validate customer KRA PINs in real-time,
So that B2B invoices have verified tax information for compliance.

**Acceptance Criteria:**

1. **Given** customer credit account setup
   **When** entering KRA PIN
   **Then** validates format: A followed by 9 digits followed by letter (e.g., A123456789K)

2. **Given** valid PIN format
   **When** saving customer
   **Then** calls KRA iTax API to verify PIN is registered and active

3. **Given** PIN validation successful
   **When** API returns
   **Then** stores: PIN, registered name, status, validation date

4. **Given** PIN validation failed
   **When** API returns error
   **Then** displays reason: "PIN not found", "PIN inactive", "Name mismatch"

5. **Given** B2B invoice created
   **When** customer has unvalidated PIN
   **Then** shows warning but allows override with manager approval

6. **Given** periodic revalidation needed
   **When** PIN older than 90 days
   **Then** flags for re-verification on next transaction

7. **Given** batch validation needed
   **When** running validation job
   **Then** validates all customer PINs and reports results

---

## Epic 19: M-Pesa Daraja API Integration

**Goal:** Implement full M-Pesa Daraja API integration with STK Push (Lipa na M-Pesa) for seamless mobile money payments at checkout.

**Dependencies:** Epic 7 (Payment Processing)
**Mode:** All (Critical for Kenya deployment)

### Story 19.1: Daraja API Configuration

As an administrator,
I want to configure M-Pesa Daraja API credentials,
So that the POS can initiate M-Pesa payments.

**Acceptance Criteria:**

1. **Given** Safaricom Developer Portal credentials
   **When** configuring M-Pesa settings
   **Then** admin can enter: Consumer Key, Consumer Secret, Passkey, Shortcode

2. **Given** credentials are entered
   **When** testing connection
   **Then** system validates OAuth token generation

3. **Given** configuration is complete
   **When** callback URL is set
   **Then** system can receive payment confirmations

---

### Story 19.2: STK Push Payment Initiation

As a cashier,
I want to trigger an M-Pesa payment prompt on the customer's phone,
So that payment is fast and accurate.

**Acceptance Criteria:**

1. **Given** M-Pesa payment is selected
   **When** entering customer phone number
   **Then** validates Kenyan format (254XXXXXXXXX or 07XXXXXXXX)

2. **Given** phone number is valid
   **When** initiating STK Push
   **Then** customer receives payment prompt on their phone within 5 seconds

3. **Given** STK Push is sent
   **When** waiting for response
   **Then** POS shows "Waiting for customer to enter PIN" with timeout counter

---

### Story 19.3: M-Pesa Payment Confirmation

As the system,
I want to receive and process M-Pesa payment callbacks,
So that transactions are confirmed automatically.

**Acceptance Criteria:**

1. **Given** customer enters M-Pesa PIN
   **When** callback is received
   **Then** system processes ResultCode and ResultDesc

2. **Given** payment is successful (ResultCode = 0)
   **When** confirming payment
   **Then** receipt is settled with M-Pesa reference number

3. **Given** payment fails or times out
   **When** handling failure
   **Then** displays error message and allows retry or alternative payment

---

### Story 19.4: M-Pesa Transaction Status Query

As a cashier,
I want to check the status of pending M-Pesa payments,
So that I can resolve stuck transactions.

**Acceptance Criteria:**

1. **Given** an STK Push was initiated
   **When** no callback received after 30 seconds
   **Then** cashier can trigger status query

2. **Given** status query is run
   **When** response indicates success
   **Then** transaction is marked as paid

3. **Given** status indicates failure
   **When** viewing result
   **Then** shows failure reason and clears pending status

---

### Story 19.5: Manual M-Pesa Entry Fallback

As a cashier,
I want to manually enter M-Pesa transaction details,
So that payments can be recorded when STK Push fails.

**Acceptance Criteria:**

1. **Given** STK Push is unavailable
   **When** selecting manual entry
   **Then** cashier can enter M-Pesa transaction code

2. **Given** transaction code is entered
   **When** validating format
   **Then** checks 10-character alphanumeric pattern

3. **Given** valid code entered
   **When** completing payment
   **Then** transaction is recorded with manual entry flag for reconciliation

---

## Epic 20: Barcode, Scale & PLU Management

**Goal:** Implement high-speed barcode scanning, integrated scale support, and PLU code management for retail/supermarket checkout operations.

**Dependencies:** Epic 4 (Products), Epic 5 (Sales)
**Mode:** Retail, Hybrid

### Story 20.1: Barcode Scanner Integration

As a cashier,
I want items to be added instantly when scanned,
So that checkout is fast and accurate.

**Acceptance Criteria:**

1. **Given** a barcode scanner is connected
   **When** scanning a product barcode
   **Then** item is added to order within 100ms

2. **Given** barcode types vary
   **When** scanning
   **Then** system auto-detects UPC-A, EAN-13, and Code 128 formats

3. **Given** unknown barcode is scanned
   **When** no product matches
   **Then** displays "Item not found" with manual search option

---

### Story 20.2: Random Weight Barcode Processing

As a cashier,
I want to scan pre-weighed items with price-embedded barcodes,
So that produce and deli items process correctly.

**Acceptance Criteria:**

1. **Given** a random weight barcode (prefix 02 or 2)
   **When** scanned
   **Then** system extracts PLU code and embedded price

2. **Given** PLU code is extracted
   **When** looking up product
   **Then** matches to product and applies embedded price

3. **Given** price is embedded
   **When** adding to order
   **Then** uses embedded price (not shelf price) and marks as "weighed"

---

### Story 20.3: PLU Code Quick Entry

As a cashier,
I want to enter PLU codes for produce items,
So that items without barcodes can be quickly added.

**Acceptance Criteria:**

1. **Given** product has no barcode
   **When** entering 4-5 digit PLU code
   **Then** product is looked up and added to order

2. **Given** PLU lookup is needed
   **When** accessing PLU quick-keys
   **Then** frequently used produce items have dedicated buttons

3. **Given** PLU not found
   **When** displaying error
   **Then** offers search by product name

---

### Story 20.4: Scale Integration

As a cashier,
I want to weigh items directly at checkout,
So that loose produce can be priced accurately.

**Acceptance Criteria:**

1. **Given** an integrated scale is connected
   **When** placing item on scale
   **Then** weight is displayed in real-time

2. **Given** item requires weighing
   **When** weight is stable
   **Then** cashier can accept weight and calculate price

3. **Given** tare weight is needed
   **When** pressing tare button
   **Then** container weight is deducted from total

---

### Story 20.5: PLU Code Management

As an administrator,
I want to assign and manage PLU codes for products,
So that produce items have quick lookup codes.

**Acceptance Criteria:**

1. **Given** creating/editing a product
   **When** entering PLU code
   **Then** validates 4-5 digit format and uniqueness

2. **Given** PLU codes exist
   **When** viewing PLU list
   **Then** shows all products with PLU codes by department

3. **Given** PLU import is needed
   **When** importing from CSV
   **Then** bulk creates/updates PLU assignments

---

### Story 20.6: GS1 DataBar Full Support

As a cashier,
I want the system to fully decode GS1 DataBar barcodes,
So that I can scan modern produce labels with embedded weight, price, and expiry data.

**Acceptance Criteria:**

1. **Given** GS1 DataBar Expanded scanned
   **When** parsing barcode
   **Then** decodes all Application Identifiers (AIs): (01) GTIN, (3103) Weight, (3922) Price, (10) Batch/Lot, (17) Expiry

2. **Given** AI (01) GTIN decoded
   **When** looking up product
   **Then** matches to product by GTIN or links to existing PLU/barcode

3. **Given** AI (3103) weight present
   **When** processing
   **Then** extracts weight in kg (3 decimal places) and calculates price

4. **Given** AI (3922) price present
   **When** processing
   **Then** uses embedded price instead of system price (for pre-priced items)

5. **Given** AI (10) batch/lot present
   **When** processing
   **Then** records batch number for traceability

6. **Given** AI (17) expiry present
   **When** processing
   **Then** checks against current date and warns if expired or near-expiry

7. **Given** GS1 DataBar Stacked format
   **When** scanning
   **Then** correctly parses multi-row barcode as single scan

8. **Given** unknown AI encountered
   **When** parsing
   **Then** logs for review but continues processing known AIs

---

## Epic 21: Advanced Loyalty Program

**Goal:** Implement a comprehensive customer loyalty program with points, tiers, and personalized rewards for retail operations.

**Dependencies:** Epic 5 (Sales), Epic 7 (Payments)
**Mode:** Retail, Hybrid

### Story 21.1: Customer Enrollment

As a cashier,
I want to quickly enroll customers in the loyalty program,
So that they can start earning points immediately.

**Acceptance Criteria:**

1. **Given** customer wants to join
   **When** enrolling at POS
   **Then** can enter: phone number, name (optional), email (optional)

2. **Given** phone number entered
   **When** validating
   **Then** checks for existing account and prevents duplicates

3. **Given** enrollment complete
   **When** confirming
   **Then** customer receives welcome SMS with member details

---

### Story 21.2: Points Earning

As the system,
I want to award loyalty points on purchases,
So that customers are rewarded for shopping.

**Acceptance Criteria:**

1. **Given** loyalty member makes purchase
   **When** transaction is completed
   **Then** points awarded based on earning rate (e.g., KSh 100 = 1 point)

2. **Given** bonus points promotions exist
   **When** qualifying items purchased
   **Then** additional bonus points are awarded

3. **Given** points are earned
   **When** printing receipt
   **Then** shows points earned this visit and total balance

---

### Story 21.3: Points Redemption

As a cashier,
I want to apply loyalty points as payment,
So that customers can use their rewards.

**Acceptance Criteria:**

1. **Given** customer has points balance
   **When** requesting redemption
   **Then** shows available points and KSh equivalent

2. **Given** redemption amount selected
   **When** applying to transaction
   **Then** deducts points and reduces amount due

3. **Given** minimum threshold configured
   **When** balance is below threshold
   **Then** displays "Insufficient points" message

---

### Story 21.4: Membership Tier Management

As the system,
I want to manage customer tiers based on spending,
So that loyal customers get better benefits.

**Acceptance Criteria:**

1. **Given** tier thresholds configured (Bronze, Silver, Gold, Platinum)
   **When** customer spending reaches threshold
   **Then** customer is automatically upgraded

2. **Given** tier benefits defined
   **When** member transacts
   **Then** tier-specific discounts or earning rates apply

3. **Given** evaluation period ends
   **When** reviewing tiers
   **Then** customers may be downgraded if spending dropped

---

### Story 21.5: Customer Purchase History

As a manager,
I want to view customer purchase history,
So that I can understand buying patterns.

**Acceptance Criteria:**

1. **Given** loyalty member identified
   **When** viewing profile
   **Then** shows: total spend, visit count, average basket, top categories

2. **Given** transaction history needed
   **When** drilling down
   **Then** lists all transactions with dates and amounts

3. **Given** personalization needed
   **When** exporting data
   **Then** can export for marketing campaigns

---

## Epic 22: Multi-Store HQ Management

**Goal:** Implement centralized headquarters management for retail chains, enabling central control of products, pricing, and promotions across all stores.

**Dependencies:** Epic 4 (Products), Epic 14 (Offers)
**Mode:** Retail (Chains)

### Story 22.1: Central Product Master

As an HQ administrator,
I want to manage products centrally for all stores,
So that product catalog is consistent chain-wide.

**Acceptance Criteria:**

1. **Given** HQ access
   **When** creating a product
   **Then** product is available to deploy to all stores

2. **Given** product exists centrally
   **When** updating details
   **Then** can push changes to all stores or selected stores

3. **Given** store-specific overrides needed
   **When** configuring
   **Then** individual stores can have local product variations

---

### Story 22.2: Central Pricing Management

As an HQ administrator,
I want to set prices centrally and manage regional pricing,
So that pricing is controlled and consistent.

**Acceptance Criteria:**

1. **Given** product exists
   **When** setting central price
   **Then** price applies to all stores by default

2. **Given** regional pricing needed
   **When** defining zones
   **Then** can set different prices per zone/region

3. **Given** scheduled price change
   **When** setting effective date
   **Then** prices automatically update on that date across stores

---

### Story 22.3: Central Promotion Deployment

As an HQ administrator,
I want to create and deploy promotions to stores,
So that campaigns run consistently across the chain.

**Acceptance Criteria:**

1. **Given** promotion created at HQ
   **When** deploying
   **Then** can select: all stores, specific region, or individual stores

2. **Given** promotion deployed
   **When** sync runs
   **Then** promotion is active at selected stores

3. **Given** promotion running
   **When** monitoring
   **Then** HQ dashboard shows redemption counts by store

---

### Story 22.4: Consolidated Chain Reporting

As an HQ manager,
I want consolidated reports across all stores,
So that I can monitor chain performance.

**Acceptance Criteria:**

1. **Given** HQ dashboard access
   **When** viewing sales
   **Then** shows real-time totals across all stores

2. **Given** comparison needed
   **When** running store comparison report
   **Then** ranks stores by sales, margin, basket size

3. **Given** product performance analysis
   **When** running report
   **Then** shows chain-wide sales by product with store breakdown

---

### Story 22.5: Store Data Synchronization

As the system,
I want to synchronize data between stores and HQ,
So that central and local systems are consistent.

**Acceptance Criteria:**

1. **Given** store is online
   **When** sync service runs
   **Then** transactions uploaded to HQ database

2. **Given** HQ updates products/prices
   **When** store syncs
   **Then** changes downloaded and applied locally

3. **Given** sync conflicts occur
   **When** detecting conflict
   **Then** applies resolution rules (HQ wins for prices, store wins for transactions)

---

## Epic 23: Stock Transfers

**Goal:** Implement inter-store stock transfer functionality for retail chains to move inventory between locations.

**Dependencies:** Epic 8 (Inventory), Epic 22 (Multi-Store HQ)
**Mode:** Retail (Chains)

### Story 23.1: Transfer Request Creation

As a store manager,
I want to request stock from another location,
So that I can replenish out-of-stock items.

**Acceptance Criteria:**

1. **Given** stock is needed
   **When** creating transfer request
   **Then** can select source location (store/warehouse)

2. **Given** source selected
   **When** adding products
   **Then** shows source location's available stock

3. **Given** request is complete
   **When** submitting
   **Then** request is sent to source location for approval

---

### Story 23.2: Transfer Approval

As a source location manager,
I want to approve or modify transfer requests,
So that I control outgoing stock.

**Acceptance Criteria:**

1. **Given** transfer request received
   **When** reviewing
   **Then** shows requested items and quantities

2. **Given** request is acceptable
   **When** approving
   **Then** stock is reserved for transfer

3. **Given** modification needed
   **When** adjusting quantities
   **Then** updated quantities communicated to requesting store

---

### Story 23.3: Transfer Shipment

As a warehouse clerk,
I want to process the physical transfer,
So that goods are properly picked and shipped.

**Acceptance Criteria:**

1. **Given** transfer is approved
   **When** processing shipment
   **Then** generates pick list for warehouse

2. **Given** items are picked
   **When** confirming shipment
   **Then** stock is deducted from source location

3. **Given** shipment is ready
   **When** dispatching
   **Then** generates transfer document and updates status to "In Transit"

---

### Story 23.4: Transfer Receiving

As a receiving store manager,
I want to receive transferred stock,
So that my inventory is updated.

**Acceptance Criteria:**

1. **Given** transfer shipment arrives
   **When** receiving
   **Then** can enter actual received quantities

2. **Given** variance exists (short/over)
   **When** recording
   **Then** variance is logged and flagged for investigation

3. **Given** receiving is complete
   **When** confirming
   **Then** stock is added to destination location

---

## Epic 24: Batch & Expiry Tracking

**Goal:** Implement batch/lot tracking and expiry date management for inventory items to ensure FIFO/FEFO and food safety compliance.

**Dependencies:** Epic 8 (Inventory), Epic 9 (Purchasing)
**Mode:** Retail, Hybrid

### Story 24.1: Batch Recording on Receipt

As a receiving clerk,
I want to record batch numbers and expiry dates,
So that inventory is traceable.

**Acceptance Criteria:**

1. **Given** goods are being received
   **When** entering receipt details
   **Then** can enter batch/lot number and expiry date per item

2. **Given** expiry date is required
   **When** leaving blank
   **Then** system warns or blocks based on product configuration

3. **Given** batch is recorded
   **When** saving
   **Then** batch details linked to stock movement and stored

---

### Story 24.2: Expiry Alert Dashboard

As a manager,
I want to see items approaching expiry,
So that I can take action before products expire.

**Acceptance Criteria:**

1. **Given** items have expiry dates
   **When** viewing dashboard
   **Then** shows items expiring in 7, 14, 30 days

2. **Given** item is near expiry
   **When** alerting
   **Then** suggests markdown or removal from shelf

3. **Given** item has expired
   **When** detected
   **Then** prominently flagged for disposal

---

### Story 24.3: Expired Item Blocking

As the system,
I want to prevent sale of expired items,
So that food safety is maintained.

**Acceptance Criteria:**

1. **Given** item batch has expired
   **When** attempting to sell
   **Then** system blocks sale with "Item expired" message

2. **Given** blocking is enabled
   **When** configuring
   **Then** admin can enable/disable per category

3. **Given** override needed
   **When** manager authorizes
   **Then** sale proceeds with audit log entry

---

### Story 24.4: Batch Traceability

As a manager,
I want to trace a batch from receipt to sale,
So that I can handle recalls.

**Acceptance Criteria:**

1. **Given** a batch recall is needed
   **When** searching by batch number
   **Then** shows: supplier, receipt date, quantity received, quantity sold

2. **Given** sold items are identified
   **When** drilling down
   **Then** can view transactions containing the batch

3. **Given** remaining stock exists
   **When** viewing
   **Then** shows current quantity and location

---

### Story 24.5: Expiry Waste Reporting

As a manager,
I want reports on expired/wasted inventory,
So that I can track shrinkage and improve ordering.

**Acceptance Criteria:**

1. **Given** expired items are disposed
   **When** recording waste
   **Then** logs quantity and value by reason

2. **Given** waste data exists
   **When** running report
   **Then** shows waste by category, supplier, and period

3. **Given** trend analysis needed
   **When** viewing charts
   **Then** shows waste trends over time

---

## Epic 25: Offline-First Architecture & Cloud Sync

**Goal:** Implement robust offline-first operation with intelligent cloud synchronization for reliable operation in areas with unreliable internet connectivity.

**Dependencies:** All Epics (foundational)
**Mode:** All

### Story 25.1: Local Database Setup

As the system,
I want all operations to work against a local database,
So that internet outage doesn't stop business.

**Acceptance Criteria:**

1. **Given** POS is installed
   **When** initializing
   **Then** SQL Server Express local database is created

2. **Given** local database exists
   **When** performing any operation
   **Then** all data is stored locally first

3. **Given** internet is unavailable
   **When** using POS
   **Then** all core functions work without interruption

---

### Story 25.2: Sync Queue Management

As the system,
I want to queue changes for cloud synchronization,
So that data is eventually consistent with central systems.

**Acceptance Criteria:**

1. **Given** data changes locally
   **When** saving
   **Then** change is added to sync queue with priority

2. **Given** queue has items
   **When** connection is available
   **Then** items sync in priority order (eTIMS first)

3. **Given** sync fails
   **When** retrying
   **Then** uses exponential backoff and logs failures

---

### Story 25.3: Real-Time Sync (SignalR)

As the system,
I want real-time data updates when online,
So that HQ and stores have current data.

**Acceptance Criteria:**

1. **Given** internet is connected
   **When** SignalR connection established
   **Then** changes sync in real-time

2. **Given** connection drops
   **When** reconnecting
   **Then** automatically re-establishes and syncs pending items

3. **Given** large backlog exists
   **When** reconnecting
   **Then** batch syncs in priority order without blocking UI

---

### Story 25.4: Conflict Resolution

As the system,
I want to automatically resolve sync conflicts,
So that data integrity is maintained.

**Acceptance Criteria:**

1. **Given** same record changed locally and remotely
   **When** syncing
   **Then** applies resolution rules (last-write-wins for transactions)

2. **Given** price conflict (local vs HQ)
   **When** resolving
   **Then** HQ price wins, local change flagged for review

3. **Given** unresolvable conflict
   **When** detected
   **Then** flags for manual resolution by manager

---

### Story 25.5: Sync Status Dashboard

As a manager,
I want to see sync status at a glance,
So that I know data is being synchronized.

**Acceptance Criteria:**

1. **Given** accessing sync status
   **When** viewing dashboard
   **Then** shows: Online/Offline, Pending items, Last sync time

2. **Given** sync issues exist
   **When** viewing details
   **Then** lists failed items with error reasons

3. **Given** manual sync needed
   **When** pressing sync button
   **Then** immediately attempts to sync all pending items

---

## Epic 26: Recipe & Ingredient Management (NEW)

**Goal:** Implement recipe/ingredient management for menu items with automatic ingredient costing and stock deduction, enabling accurate food cost tracking in hospitality operations.

**Dependencies:** Epic 4 (Products), Epic 8 (Inventory)
**Mode:** Hospitality, Hybrid

### Story 26.1: Recipe Definition

As a kitchen manager,
I want to define recipes for menu items,
So that ingredient usage is tracked accurately.

**Acceptance Criteria:**

1. **Given** a menu product exists
   **When** creating a recipe
   **Then** can link product to recipe with yield/portions

2. **Given** creating recipe details
   **When** adding ingredients
   **Then** can specify: raw ingredient, quantity, unit of measure

3. **Given** recipe is defined
   **When** saving
   **Then** recipe is validated (ingredients exist, quantities positive)

---

### Story 26.2: Ingredient Costing

As a manager,
I want automatic recipe costing based on ingredient prices,
So that I know the true cost of each menu item.

**Acceptance Criteria:**

1. **Given** recipe has ingredients with costs
   **When** viewing recipe
   **Then** shows calculated total cost per portion

2. **Given** ingredient costs change
   **When** recalculating
   **Then** recipe cost updates automatically

3. **Given** food cost analysis needed
   **When** running cost report
   **Then** shows: recipe cost, selling price, margin %, food cost %

---

### Story 26.3: Automatic Ingredient Deduction

As the system,
I want to deduct ingredients when menu items are sold,
So that inventory reflects actual usage.

**Acceptance Criteria:**

1. **Given** menu item with recipe is sold
   **When** receipt is settled
   **Then** all ingredients are deducted per recipe quantities

2. **Given** item quantity > 1
   **When** deducting
   **Then** multiplies ingredient quantities correctly

3. **Given** ingredient deduction fails (insufficient stock)
   **When** processing
   **Then** logs warning but allows sale (configurable)

---

### Story 26.4: Sub-Recipes and Batch Prep

As a kitchen manager,
I want to create sub-recipes and record batch preparations,
So that prep work is tracked against inventory.

**Acceptance Criteria:**

1. **Given** common components (sauces, stocks)
   **When** creating sub-recipe
   **Then** can use sub-recipe as ingredient in other recipes

2. **Given** batch prep is done
   **When** recording prep
   **Then** deducts raw ingredients, adds prepped item to inventory

3. **Given** ingredient usage tracking
   **When** running reports
   **Then** shows ingredient usage by recipe, by period

---

## Epic 27: Kitchen Display System (KDS) (NEW)

**Goal:** Implement a real-time kitchen display system for order management, replacing or supplementing paper KOT tickets with digital screens for improved kitchen efficiency.

**Dependencies:** Epic 5 (Orders), Epic 12 (Printing)
**Mode:** Hospitality, Hybrid

### Story 27.1: KDS Station Configuration

As an administrator,
I want to configure KDS stations for different kitchen areas,
So that orders route to the correct preparation stations.

**Acceptance Criteria:**

1. **Given** KDS setup
   **When** configuring stations
   **Then** can define: station name, display device, product categories

2. **Given** multiple stations exist
   **When** assigning categories
   **Then** categories route to specific stations (Hot, Cold, Bar, Dessert)

3. **Given** station configuration
   **When** saving
   **Then** KDS displays activate with assigned categories

---

### Story 27.2: Real-Time Order Display

As a kitchen staff member,
I want to see orders appear in real-time on the KDS,
So that I can prepare orders as they come in.

**Acceptance Criteria:**

1. **Given** order is submitted
   **When** containing items for this station
   **Then** order appears on KDS within 2 seconds

2. **Given** order is displayed
   **When** viewing
   **Then** shows: order #, table #, items with modifiers, time elapsed

3. **Given** multiple orders exist
   **When** viewing queue
   **Then** orders sorted by submission time (oldest first)

---

### Story 27.3: Order Status Management

As a kitchen staff member,
I want to update order status as I prepare it,
So that the front of house knows order progress.

**Acceptance Criteria:**

1. **Given** order on KDS
   **When** starting preparation
   **Then** can mark as "In Progress" (color changes)

2. **Given** order is ready
   **When** bumping order
   **Then** order moves to "Ready" queue with audio alert

3. **Given** bumped order needs recall
   **When** pressing recall
   **Then** order returns to active display

---

### Story 27.4: Order Timer and Priorities

As a kitchen manager,
I want to see order age timers and priority indicators,
So that no orders are forgotten or delayed.

**Acceptance Criteria:**

1. **Given** order is displayed
   **When** time elapses
   **Then** timer shows age, color changes (green→yellow→red)

2. **Given** rush order is submitted
   **When** displaying
   **Then** shows priority flag and appears at top of queue

3. **Given** order exceeds time threshold
   **When** alerting
   **Then** visual/audio alert for overdue orders

---

### Story 27.5: Expo Station and All-Call

As an expo/food runner,
I want to see all orders across all stations,
So that I can coordinate plating and delivery.

**Acceptance Criteria:**

1. **Given** expo station configured
   **When** viewing display
   **Then** shows orders from ALL stations with status

2. **Given** order has items from multiple stations
   **When** all items ready
   **Then** order shows "Complete" for plating

3. **Given** communication needed
   **When** using all-call
   **Then** can send message to all stations

---

## Epic 28: Shelf Label Printing (NEW)

**Goal:** Implement shelf label printing for retail products with price updates, barcode labels, and batch printing capabilities.

**Dependencies:** Epic 4 (Products), Epic 12 (Printing)
**Mode:** Retail, Hybrid

### Story 28.1: Label Printer Configuration

As an administrator,
I want to configure label printers for shelf labeling,
So that labels can be printed on appropriate devices.

**Acceptance Criteria:**

1. **Given** label printer connected
   **When** configuring
   **Then** can set: printer name/port, label size, print language (ZPL/EPL)

2. **Given** printer configured
   **When** testing
   **Then** prints test label successfully

3. **Given** multiple printers
   **When** managing
   **Then** can set default printer per category

---

### Story 28.2: Label Template Management

As an administrator,
I want to design and manage label templates,
So that labels match business requirements.

**Acceptance Criteria:**

1. **Given** label design needed
   **When** creating template
   **Then** can specify: product name, barcode, price, description positioning

2. **Given** template exists
   **When** previewing
   **Then** shows label preview with sample data

3. **Given** different label sizes
   **When** managing templates
   **Then** templates linked to specific label sizes

---

### Story 28.3: Individual and Batch Label Printing

As a store clerk,
I want to print labels for single products or batches,
So that shelves are properly labeled.

**Acceptance Criteria:**

1. **Given** product selected
   **When** printing single label
   **Then** prints one label with current product data

2. **Given** price changes occur
   **When** running batch print
   **Then** prints labels for all changed products

3. **Given** category selected
   **When** batch printing
   **Then** prints labels for all products in category

---

## Epic 29: Advanced Promotions Engine (NEW)

**Goal:** Implement a comprehensive promotions engine supporting BOGO, Mix & Match, quantity breaks, combo deals, coupons, and automatic markdown management for retail operations.

**Dependencies:** Epic 4 (Products), Epic 14 (Basic Offers)
**Mode:** Retail, Hybrid

### Story 29.1: BOGO Promotions (Buy One Get One)

As a marketing manager,
I want to create Buy One Get One promotions,
So that I can run standard retail promotional campaigns.

**Acceptance Criteria:**

1. **Given** promotion management access
   **When** creating BOGO promotion
   **Then** can specify: Buy X quantity, Get Y quantity free/discounted

2. **Given** BOGO variants needed
   **When** configuring
   **Then** supports: Buy 1 Get 1 Free, Buy 2 Get 1 Free, Buy 1 Get 1 50% Off

3. **Given** BOGO applies to different products
   **When** configuring
   **Then** can set "Buy Product A, Get Product B free" cross-promotions

4. **Given** BOGO is active
   **When** qualifying items scanned
   **Then** discount automatically applies to lowest-priced qualifying item

---

### Story 29.2: Mix & Match Promotions

As a marketing manager,
I want to create Mix & Match promotions,
So that customers can buy any items from a group for a fixed price.

**Acceptance Criteria:**

1. **Given** promotion management access
   **When** creating Mix & Match
   **Then** can specify: product group, quantity required, fixed price

2. **Given** Mix & Match configured
   **When** customer buys qualifying quantity
   **Then** fixed price applies (e.g., "Any 3 sodas for KES 200")

3. **Given** customer buys more than required quantity
   **When** calculating price
   **Then** creates multiple groups (6 items = 2 groups of 3)

4. **Given** partial qualification
   **When** customer buys fewer than required
   **Then** items priced at normal price

---

### Story 29.3: Quantity Break Pricing

As a marketing manager,
I want to create quantity-based discounts,
So that customers are incentivized to buy more.

**Acceptance Criteria:**

1. **Given** promotion management access
   **When** creating quantity break
   **Then** can specify tier thresholds and discounts (e.g., Buy 5+ save 10%, Buy 10+ save 15%)

2. **Given** multiple tiers exist
   **When** customer meets threshold
   **Then** best applicable tier discount applies

3. **Given** quantity break is active
   **When** viewing order
   **Then** shows original price, quantity discount, and savings

---

### Story 29.4: Combo/Bundle Deals

As a marketing manager,
I want to create combo/bundle deals,
So that customers can buy product combinations at special prices.

**Acceptance Criteria:**

1. **Given** promotion management access
   **When** creating combo deal
   **Then** can specify: required products, bundle price

2. **Given** combo requires specific products
   **When** all products in cart
   **Then** bundle price automatically applies

3. **Given** combo with substitutes
   **When** configuring
   **Then** can allow product alternatives within combo (e.g., any drink)

4. **Given** multiple combos possible
   **When** calculating
   **Then** system applies combination that gives customer best savings

---

### Story 29.5: Coupon Management

As a marketing manager,
I want to create and manage promotional coupons,
So that I can run targeted discount campaigns.

**Acceptance Criteria:**

1. **Given** coupon management access
   **When** creating coupon
   **Then** can specify: code, discount type (% or fixed), applicable products/categories, validity dates

2. **Given** coupon code exists
   **When** cashier enters code at checkout
   **Then** discount applies if conditions met

3. **Given** coupon has usage limits
   **When** configuring
   **Then** can set: single use, per-customer limit, total redemption limit

4. **Given** physical coupon with barcode
   **When** scanned
   **Then** coupon code extracted and applied

5. **Given** coupon is redeemed
   **When** completing transaction
   **Then** redemption logged with customer info (if available)

---

### Story 29.6: Automatic Markdown Management

As a store manager,
I want products to automatically markdown near expiry,
So that waste is reduced and products sell before expiring.

**Acceptance Criteria:**

1. **Given** markdown rules configured
   **When** product approaches expiry
   **Then** automatic discount applied based on days-to-expiry

2. **Given** tiered markdown needed
   **When** configuring rules
   **Then** can set: 7 days = 10% off, 3 days = 25% off, 1 day = 50% off

3. **Given** markdown is applied
   **When** selling product
   **Then** receipt shows original price, markdown reason, and final price

4. **Given** markdown reporting needed
   **When** running report
   **Then** shows markdown value by category, product, and reason

---

## Epic 30: Accounts Receivable (NEW)

**Goal:** Implement customer credit management with credit limits, aging, statements, and collection tracking for B2B supermarket operations and hotel corporate accounts.

**Dependencies:** Epic 6 (Receipts), Epic 17 (Accounting)
**Mode:** All

### Story 30.1: Customer Credit Account Setup

As an accounts manager,
I want to create credit accounts for customers,
So that approved customers can purchase on credit.

**Acceptance Criteria:**

1. **Given** customer requests credit
   **When** creating credit account
   **Then** can specify: credit limit, payment terms (Net 7/15/30/60), contact details

2. **Given** credit account exists
   **When** viewing account
   **Then** shows: credit limit, available credit, current balance, overdue amount

3. **Given** credit application process
   **When** approving credit
   **Then** requires manager authorization and stores approval date/user

---

### Story 30.2: Credit Sale Processing

As a cashier,
I want to process credit sales for approved customers,
So that they can pay later per their terms.

**Acceptance Criteria:**

1. **Given** customer has credit account
   **When** settling receipt
   **Then** "Charge to Account" payment option is available

2. **Given** credit sale requested
   **When** processing
   **Then** validates available credit limit before approving

3. **Given** credit limit exceeded
   **When** attempting sale
   **Then** blocks sale or requires manager override

4. **Given** credit sale completed
   **When** generating receipt
   **Then** shows account balance and payment due date

---

### Story 30.3: Accounts Receivable Aging

As an accounts manager,
I want to see aged receivables by customer,
So that I can prioritize collection efforts.

**Acceptance Criteria:**

1. **Given** credit sales exist
   **When** running AR Aging report
   **Then** shows balances in buckets: Current, 1-30, 31-60, 61-90, 90+ days

2. **Given** aging report
   **When** drilling into customer
   **Then** shows individual invoices with dates and amounts

3. **Given** overdue accounts
   **When** viewing dashboard
   **Then** highlights severely overdue accounts (90+ days)

---

### Story 30.4: Customer Payment Recording

As an accounts clerk,
I want to record customer payments against their account,
So that balances are updated accurately.

**Acceptance Criteria:**

1. **Given** customer makes payment
   **When** recording
   **Then** can enter: amount, payment method, reference number, date

2. **Given** payment recorded
   **When** allocating
   **Then** can apply to specific invoices or oldest-first automatic allocation

3. **Given** partial payment received
   **When** allocating
   **Then** invoice shows remaining balance due

4. **Given** overpayment received
   **When** processing
   **Then** creates credit balance on account

---

### Story 30.5: Customer Statement Generation

As an accounts manager,
I want to generate customer statements,
So that customers know their account status.

**Acceptance Criteria:**

1. **Given** customer has transactions
   **When** generating statement
   **Then** shows: opening balance, all transactions, payments, closing balance

2. **Given** statement generated
   **When** printing/exporting
   **Then** can print or export to PDF for sending to customer

3. **Given** statement period
   **When** selecting dates
   **Then** can generate for any date range

---

## Epic 31: Bank Reconciliation (NEW)

**Goal:** Implement bank statement import and reconciliation to match POS transactions with bank records, ensuring accurate cash management.

**Dependencies:** Epic 7 (Payments), Epic 17 (Accounting)
**Mode:** All

### Story 31.1: Bank Account Configuration

As an administrator,
I want to configure bank accounts in the system,
So that transactions can be reconciled per account.

**Acceptance Criteria:**

1. **Given** bank reconciliation needed
   **When** configuring bank account
   **Then** can specify: bank name, account number, account type, GL account link

2. **Given** multiple accounts exist
   **When** managing
   **Then** can configure separate accounts for operations, savings, M-Pesa float

3. **Given** account configured
   **When** linking to GL
   **Then** bank account maps to Chart of Accounts entry

---

### Story 31.2: Bank Statement Import

As an accountant,
I want to import bank statements,
So that I can reconcile against POS records.

**Acceptance Criteria:**

1. **Given** bank statement available
   **When** importing
   **Then** can upload CSV or OFX/QIF format files

2. **Given** statement imported
   **When** parsing
   **Then** extracts: date, description, reference, debit, credit, balance

3. **Given** import complete
   **When** viewing
   **Then** shows all statement lines ready for matching

---

### Story 31.3: Automatic Transaction Matching

As the system,
I want to automatically match bank transactions to POS records,
So that reconciliation is faster.

**Acceptance Criteria:**

1. **Given** statement imported
   **When** running auto-match
   **Then** matches by: amount + date, reference number, or description patterns

2. **Given** M-Pesa transactions
   **When** matching
   **Then** uses M-Pesa transaction codes to match receipts

3. **Given** match found
   **When** reviewing
   **Then** shows confidence level (exact, probable, suggested)

4. **Given** no match found
   **When** displaying
   **Then** flags transaction for manual matching

---

### Story 31.4: Manual Matching and Reconciliation

As an accountant,
I want to manually match unreconciled items,
So that all transactions are accounted for.

**Acceptance Criteria:**

1. **Given** unmatched transactions
   **When** manual matching
   **Then** can search POS transactions by date range, amount, reference

2. **Given** match is made
   **When** confirming
   **Then** both bank and POS transactions marked as reconciled

3. **Given** bank fee or interest
   **When** recording
   **Then** can create journal entry for items with no POS counterpart

4. **Given** reconciliation complete
   **When** finalizing
   **Then** calculates and displays reconciled vs book balance variance

---

## Epic 32: Hotel PMS Integration (NEW)

**Goal:** Implement integration with Property Management Systems (PMS) to enable guest folio management, room charging, and unified billing for hotel F&B operations.

**Dependencies:** Epic 5 (Sales), Epic 6 (Receipts)
**Mode:** Hospitality (Hotels)

### Story 32.1: PMS Connection Configuration

As an administrator,
I want to configure connection to the hotel PMS,
So that POS can communicate with the property system.

**Acceptance Criteria:**

1. **Given** PMS integration needed
   **When** configuring
   **Then** can enter: PMS type (Opera, Fidelio, Protel, custom), API endpoint, credentials

2. **Given** credentials entered
   **When** testing connection
   **Then** validates connectivity and authentication

3. **Given** connection established
   **When** monitoring
   **Then** shows connection status on dashboard (Online/Offline)

---

### Story 32.2: Guest Lookup and Verification

As a cashier,
I want to look up hotel guests by room number,
So that I can verify identity before posting charges.

**Acceptance Criteria:**

1. **Given** guest wants to charge to room
   **When** entering room number
   **Then** POS queries PMS and returns guest name

2. **Given** guest info returned
   **When** verifying
   **Then** cashier confirms guest name matches (verbal verification)

3. **Given** room is vacant or checked out
   **When** querying
   **Then** system blocks charge with "Room not occupied" message

4. **Given** guest has checkout today
   **When** posting charge
   **Then** warns cashier that guest may be checking out soon

---

### Story 32.3: Room Charge Posting

As a cashier,
I want to post F&B charges to a guest's room,
So that they can pay at checkout.

**Acceptance Criteria:**

1. **Given** guest verified
   **When** settling receipt
   **Then** "Charge to Room" payment option is available

2. **Given** room charge selected
   **When** posting to PMS
   **Then** sends: room number, amount, outlet name, itemized details

3. **Given** posting successful
   **When** receiving confirmation
   **Then** receipt shows PMS posting reference and guest name

4. **Given** posting fails
   **When** handling error
   **Then** falls back to alternative payment with error logged

---

### Story 32.4: Guest Folio Display

As a cashier,
I want to view a guest's current folio balance,
So that I can inform them of their running total.

**Acceptance Criteria:**

1. **Given** guest inquiry
   **When** looking up folio
   **Then** displays current folio balance from PMS

2. **Given** detailed view needed
   **When** drilling down
   **Then** shows recent charges from all outlets (room, restaurant, spa)

3. **Given** guest disputes charge
   **When** reviewing
   **Then** can view itemized POS charges posted to their folio

---

### Story 32.5: Table Reservation System

As a restaurant host,
I want to manage table reservations,
So that guests can book tables in advance.

**Acceptance Criteria:**

1. **Given** reservation request
   **When** creating booking
   **Then** can specify: guest name, phone, date/time, party size, special requests

2. **Given** reservation exists
   **When** viewing floor plan
   **Then** reserved tables show reservation indicator with time

3. **Given** guest arrives
   **When** seating
   **Then** can mark reservation as "Seated" and assign table

4. **Given** no-show tracking
   **When** guest doesn't arrive
   **Then** can mark as no-show with time for reporting

---

### Story 32.6: Package/Meal Plan Handling

As a cashier,
I want to apply guest meal plan credits,
So that included meals don't result in charges.

**Acceptance Criteria:**

1. **Given** guest on half-board or full-board
   **When** looking up room
   **Then** PMS returns meal plan entitlements

2. **Given** meal is included
   **When** settling
   **Then** can apply "Meal Plan" payment covering entitled amount

3. **Given** meal exceeds plan value
   **When** calculating
   **Then** charges difference to room or other payment

4. **Given** meal plan tracking
   **When** reporting
   **Then** shows meal plan redemptions by date and guest

---

## Epic 33: REST API Layer (NEW)

**Goal:** Implement a comprehensive REST API to enable third-party integrations, mobile apps, e-commerce platforms, and external reporting tools to connect with the POS system.

**Dependencies:** All core epics
**Mode:** All

### Story 33.1: API Framework Setup

As a developer,
I want to set up an ASP.NET Core Web API project,
So that external systems can integrate with POS.

**Acceptance Criteria:**

1. **Given** API project needed
   **When** creating
   **Then** ASP.NET Core Web API project with versioning support

2. **Given** API structure
   **When** organizing
   **Then** follows RESTful conventions with proper HTTP methods and status codes

3. **Given** deployment options
   **When** configuring
   **Then** can run as Windows Service or IIS-hosted

---

### Story 33.2: API Authentication (JWT/OAuth)

As an administrator,
I want secure API authentication,
So that only authorized systems can access data.

**Acceptance Criteria:**

1. **Given** API client registers
   **When** creating credentials
   **Then** generates API key and secret for the client

2. **Given** client authenticates
   **When** requesting token
   **Then** issues JWT token with configurable expiration

3. **Given** token issued
   **When** making API calls
   **Then** validates token and extracts permissions

4. **Given** token expired
   **When** attempting access
   **Then** returns 401 Unauthorized with refresh instructions

---

### Story 33.3: Core API Endpoints

As a developer,
I want comprehensive CRUD endpoints for core entities,
So that integrations can read and write POS data.

**Acceptance Criteria:**

1. **Given** API is available
   **When** accessing products endpoint
   **Then** supports GET (list/single), POST (create), PUT (update), DELETE

2. **Given** read endpoints
   **When** listing resources
   **Then** supports pagination, filtering, sorting, and field selection

3. **Given** key entities
   **When** designing endpoints
   **Then** provides APIs for: Products, Categories, Orders, Receipts, Customers, Inventory

---

### Story 33.4: Webhook Event System

As an integrator,
I want to receive real-time notifications of POS events,
So that my system stays synchronized.

**Acceptance Criteria:**

1. **Given** webhook configuration
   **When** registering
   **Then** can specify: endpoint URL, events to subscribe, secret for verification

2. **Given** event occurs (sale, void, stock change)
   **When** triggering webhook
   **Then** sends POST request with event payload to registered endpoints

3. **Given** webhook delivery fails
   **When** retrying
   **Then** uses exponential backoff with configurable retry count

4. **Given** webhook history
   **When** viewing
   **Then** shows delivery attempts, status codes, and payloads

---

### Story 33.5: API Documentation (OpenAPI/Swagger)

As a developer,
I want comprehensive API documentation,
So that integrations can be built efficiently.

**Acceptance Criteria:**

1. **Given** API endpoints exist
   **When** accessing /swagger
   **Then** displays interactive API documentation

2. **Given** documentation
   **When** viewing endpoints
   **Then** shows request/response schemas, examples, and authentication requirements

3. **Given** API changes
   **When** updating
   **Then** documentation auto-generates from code annotations

---

## Epic 34: Additional Mobile Money (NEW)

**Goal:** Implement Airtel Money and T-Kash integration to provide full mobile money coverage for Kenyan customers beyond M-Pesa.

**Dependencies:** Epic 7 (Payments), Epic 19 (M-Pesa)
**Mode:** All (Kenya)

### Story 34.1: Airtel Money Integration

As an administrator,
I want to configure Airtel Money payments,
So that customers can pay using Kenya's second-largest mobile money service.

**Acceptance Criteria:**

1. **Given** Airtel Money merchant account
   **When** configuring
   **Then** can enter: merchant code, API credentials, callback URL

2. **Given** configuration complete
   **When** testing
   **Then** validates connection with Airtel Money API

3. **Given** Airtel Money is enabled
   **When** viewing payment methods
   **Then** appears as payment option at checkout

---

### Story 34.2: Airtel Money Payment Processing

As a cashier,
I want to process Airtel Money payments,
So that customers can pay from their Airtel wallets.

**Acceptance Criteria:**

1. **Given** Airtel Money selected
   **When** entering phone number
   **Then** validates format (0733XXXXXX or 0755XXXXXX)

2. **Given** valid phone
   **When** initiating payment
   **Then** sends USSD push to customer's phone

3. **Given** customer enters PIN
   **When** receiving callback
   **Then** confirms payment and settles receipt

4. **Given** payment fails or times out
   **When** handling
   **Then** allows retry or alternative payment method

---

### Story 34.3: T-Kash Integration

As an administrator,
I want to configure T-Kash (Telkom Kenya) payments,
So that all major mobile money providers are supported.

**Acceptance Criteria:**

1. **Given** T-Kash merchant account
   **When** configuring
   **Then** can enter API credentials and merchant details

2. **Given** T-Kash payment
   **When** processing
   **Then** follows similar flow to M-Pesa/Airtel Money

3. **Given** T-Kash transaction
   **When** completing
   **Then** stores transaction reference for reconciliation

---

## Epic 35: Enhanced Financial Reporting (NEW)

**Goal:** Implement comprehensive financial reports including Cash Flow Statement, General Ledger, Gross Margin Analysis, and Comparative Reports for complete financial visibility.

**Dependencies:** Epic 17 (Accounting), Epic 10 (Reporting)
**Mode:** All

### Story 35.1: Cash Flow Statement

As an accountant,
I want to generate a Cash Flow Statement,
So that I can track cash movements by activity type.

**Acceptance Criteria:**

1. **Given** financial data exists
   **When** generating Cash Flow Statement
   **Then** categorizes cash flows into: Operating, Investing, Financing activities

2. **Given** operating activities
   **When** calculating
   **Then** includes: cash from sales, payments to suppliers, operating expenses

3. **Given** statement period
   **When** selecting dates
   **Then** can generate for month, quarter, or year

4. **Given** statement generated
   **When** exporting
   **Then** can export to PDF or Excel

---

### Story 35.2: General Ledger Report

As an accountant,
I want to view detailed General Ledger transactions,
So that I can audit account activity.

**Acceptance Criteria:**

1. **Given** account selected
   **When** running GL report
   **Then** shows all transactions: date, reference, description, debit, credit, balance

2. **Given** date range specified
   **When** filtering
   **Then** shows transactions within period with opening/closing balances

3. **Given** journal entry reference
   **When** drilling down
   **Then** shows source document (receipt, invoice, adjustment)

---

### Story 35.3: Gross Margin Analysis

As a manager,
I want to see gross margin by product and category,
So that I can identify profitable and unprofitable items.

**Acceptance Criteria:**

1. **Given** sales and cost data
   **When** running Gross Margin report
   **Then** shows: revenue, COGS, gross margin, margin %

2. **Given** breakdown needed
   **When** analyzing
   **Then** can view by: product, category, supplier, time period

3. **Given** low margin items
   **When** highlighting
   **Then** flags products below configurable margin threshold

4. **Given** margin trends
   **When** charting
   **Then** shows margin % over time

---

### Story 35.4: Comparative Financial Reports

As a manager,
I want to compare financial performance across periods,
So that I can identify trends and variances.

**Acceptance Criteria:**

1. **Given** historical data exists
   **When** running comparative P&L
   **Then** shows current period vs prior period with variance

2. **Given** year-over-year analysis
   **When** selecting comparison
   **Then** shows this month vs same month last year

3. **Given** budget comparison
   **When** budget exists
   **Then** shows actual vs budget with variance

4. **Given** comparison report
   **When** viewing variance
   **Then** highlights significant positive/negative variances

---

### Story 35.5: Departmental P&L

As a manager,
I want profit & loss by department/cost center,
So that I can evaluate performance of different business areas.

**Acceptance Criteria:**

1. **Given** departments configured (Grocery, Fresh, Bakery, etc.)
   **When** running Departmental P&L
   **Then** shows revenue and costs allocated by department

2. **Given** shared costs exist
   **When** allocating
   **Then** distributes overhead based on configurable allocation rules

3. **Given** department performance
   **When** comparing
   **Then** can compare department P&L side-by-side

---

### Story 35.6: Multi-Currency Support

As a hotel manager,
I want to accept payments in multiple currencies,
So that international guests can pay in their preferred currency.

**Acceptance Criteria:**

1. **Given** currency setup needed
   **When** configuring
   **Then** can add currencies: code (USD, EUR, GBP), name, symbol, decimal places, active status

2. **Given** base currency defined
   **When** setting up
   **Then** KES is base currency for all reporting and accounting

3. **Given** exchange rates needed
   **When** managing
   **Then** can enter daily rates manually or import from CBK API

4. **Given** foreign currency payment
   **When** processing
   **Then** calculates KES equivalent using current exchange rate

5. **Given** transaction recorded
   **When** saving payment
   **Then** stores: original currency, original amount, exchange rate, KES equivalent

6. **Given** receipt printing
   **When** foreign currency used
   **Then** shows amount in both original currency and KES equivalent

7. **Given** financial reports
   **When** generating
   **Then** all amounts in base currency (KES) with optional original currency column

8. **Given** exchange rate changes
   **When** calculating unrealized gains/losses
   **Then** revalues open foreign currency receivables/payables

---

## Epic 36: Budget & Cost Management (NEW)

**Goal:** Implement budget creation, tracking, and variance analysis along with expense management enhancements for financial planning and control.

**Dependencies:** Epic 17 (Accounting)
**Mode:** All

### Story 36.1: Budget Creation

As a finance manager,
I want to create annual/monthly budgets,
So that I can plan and control spending.

**Acceptance Criteria:**

1. **Given** budget planning
   **When** creating budget
   **Then** can specify: budget name, period (monthly/quarterly/annual), accounts

2. **Given** budget accounts
   **When** entering amounts
   **Then** can set budget amount per GL account per period

3. **Given** prior year data
   **When** creating budget
   **Then** can copy last year's actuals as starting point

4. **Given** budget approval
   **When** finalizing
   **Then** requires manager approval to lock budget

---

### Story 36.2: Budget vs Actual Tracking

As a manager,
I want to track actual spending against budget,
So that I can identify and address variances.

**Acceptance Criteria:**

1. **Given** budget exists
   **When** viewing dashboard
   **Then** shows YTD budget vs actual for key accounts

2. **Given** variance threshold
   **When** exceeded
   **Then** highlights and alerts on significant variances

3. **Given** drill-down needed
   **When** clicking account
   **Then** shows monthly budget vs actual breakdown

---

### Story 36.3: Recurring Expense Templates

As an accounts clerk,
I want to set up recurring expenses,
So that regular costs are automatically posted.

**Acceptance Criteria:**

1. **Given** recurring expense (rent, utilities)
   **When** creating template
   **Then** can specify: expense type, amount, frequency, posting day

2. **Given** template active
   **When** posting date arrives
   **Then** expense entry is auto-created (as draft or posted)

3. **Given** variable amount
   **When** configuring
   **Then** can create as draft requiring amount confirmation

---

### Story 36.4: Cost Center/Department Setup

As an administrator,
I want to configure cost centers,
So that expenses can be tracked by department.

**Acceptance Criteria:**

1. **Given** cost tracking needed
   **When** creating cost centers
   **Then** can define: department name, code, manager, GL mapping

2. **Given** expense entry
   **When** recording
   **Then** must select cost center for allocation

3. **Given** expense allocation
   **When** splitting across departments
   **Then** can split single expense across multiple cost centers

---

### Story 36.5: Petty Cash Management

As a finance manager,
I want to manage petty cash funds with proper controls,
So that small expenses are tracked with custodian accountability.

**Acceptance Criteria:**

1. **Given** petty cash fund needed
   **When** creating
   **Then** can set up: fund name, authorized amount, custodian user, store location

2. **Given** petty cash expense
   **When** recording voucher
   **Then** captures: amount, purpose, expense category, recipient, receipt image (optional)

3. **Given** voucher created
   **When** requiring approval
   **Then** routes to designated approver based on amount threshold

4. **Given** voucher approved
   **When** disbursing
   **Then** deducts from fund balance and logs transaction

5. **Given** fund balance low
   **When** below threshold (e.g., 20%)
   **Then** alerts custodian and finance for replenishment

6. **Given** replenishment needed
   **When** requesting
   **Then** creates replenishment request with supporting voucher summary

7. **Given** replenishment approved
   **When** processing
   **Then** increases fund balance and records source (main cash, bank transfer)

8. **Given** reconciliation required
   **When** performing count
   **Then** compares physical cash to system balance, records variance

9. **Given** custodian handover
   **When** changing custodian
   **Then** requires count, sign-off by both parties, and balance transfer

10. **Given** audit trail
    **When** reviewing
    **Then** shows all transactions, approvals, counts, and handovers

---

## Epic 37: Checkout Enhancements (NEW)

**Goal:** Implement checkout workflow improvements including suspended transactions, customer-facing display, and enhanced payment handling.

**Dependencies:** Epic 5 (Sales), Epic 7 (Payments)
**Mode:** All

### Story 37.1: Suspended Transactions (Park/Recall)

As a cashier,
I want to suspend a transaction and resume it later,
So that checkout can continue when customer needs to get forgotten items or payment.

**Acceptance Criteria:**

1. **Given** transaction in progress
   **When** pressing "Suspend" button
   **Then** transaction is saved with status "Suspended"

2. **Given** suspended transaction exists
   **When** recalling
   **Then** can search by transaction number, customer name, or cashier

3. **Given** recall initiated
   **When** loading transaction
   **Then** all items and modifications are restored

4. **Given** suspend timeout
   **When** configured period expires
   **Then** suspended transactions are automatically voided with reason

---

### Story 37.2: Customer-Facing Display

As a cashier,
I want customers to see their items and total on a second screen,
So that they can verify accuracy during checkout.

**Acceptance Criteria:**

1. **Given** customer display connected
   **When** scanning items
   **Then** display shows: item name, price, running total

2. **Given** payment phase
   **When** entering payment
   **Then** display shows: total due, amount tendered, change

3. **Given** promotional messaging
   **When** idle
   **Then** display shows configurable advertisements/messages

4. **Given** display configuration
   **When** setting up
   **Then** can customize colors, logo, font sizes

---

### Story 37.3: Enhanced Split Payment

As a cashier,
I want to handle complex split payments across 3+ methods,
So that customers can pay however they prefer.

**Acceptance Criteria:**

1. **Given** split payment needed
   **When** initiating
   **Then** can add unlimited payment lines

2. **Given** multiple methods
   **When** adding payment
   **Then** running balance shows remaining amount due

3. **Given** overpayment on one method
   **When** calculating
   **Then** allows overpayment with change given on cash portion only

4. **Given** split complete
   **When** printing receipt
   **Then** shows all payment methods with amounts and references

---

### Story 37.4: Quick Amount Buttons

As a cashier,
I want preset amount buttons for cash payments,
So that common denominations can be quickly selected.

**Acceptance Criteria:**

1. **Given** cash payment selected
   **When** viewing payment screen
   **Then** shows quick buttons: Exact, Round Up, KES 100, 200, 500, 1000

2. **Given** configurable buttons
   **When** customizing
   **Then** admin can set button amounts per currency

3. **Given** quick button pressed
   **When** calculating
   **Then** immediately calculates and displays change

---

### Story 37.5: Cash Back on Debit Transactions

As a cashier,
I want to offer cash back when customers pay by debit card,
So that customers can get cash without visiting an ATM.

**Acceptance Criteria:**

1. **Given** cash back feature enabled
   **When** configuring
   **Then** admin can set: minimum purchase amount, maximum cash back, allowed payment methods, cash back increments

2. **Given** eligible transaction
   **When** debit card payment selected
   **Then** prompts "Would you like cash back?" with amount options (100, 200, 500, 1000, Other)

3. **Given** cash back requested
   **When** processing card payment
   **Then** total charge = purchase amount + cash back amount

4. **Given** payment approved
   **When** completing transaction
   **Then** opens cash drawer for cash back dispensing

5. **Given** receipt printing
   **When** including cash back
   **Then** shows: subtotal, cash back amount, total charged, payment method

6. **Given** end of day
   **When** reconciling
   **Then** cash back totals appear in Z-report affecting expected cash in drawer

---

### Story 37.6: Age Verification Triggers

As a cashier,
I want automatic age verification prompts for restricted products,
So that we comply with alcohol and tobacco sale regulations.

**Acceptance Criteria:**

1. **Given** product configuration
   **When** setting restrictions
   **Then** can mark product as age-restricted with minimum age (18 or 21)

2. **Given** age-restricted item scanned
   **When** adding to order
   **Then** displays prominent age verification prompt blocking further action

3. **Given** verification prompt displayed
   **When** verifying
   **Then** cashier must select: "ID Verified (18+)", "ID Verified (21+)", or "Sale Refused"

4. **Given** customer appears underage
   **When** verifying
   **Then** can optionally enter date of birth from ID for calculation

5. **Given** sale refused
   **When** removing item
   **Then** logs refusal with reason and removes item from order

6. **Given** manager override needed
   **When** requesting
   **Then** requires manager PIN to override verification

7. **Given** compliance reporting
   **When** running verification report
   **Then** shows: total age-restricted sales, verifications performed, refusals, overrides

---

### Story 37.7: Multi-Lane Coordination

As a store manager,
I want to track which cashier is assigned to which checkout lane,
So that I can optimize staffing and track lane performance.

**Acceptance Criteria:**

1. **Given** lane setup needed
   **When** configuring lanes
   **Then** can create lanes with: number, name, type (Regular, Express, Self-Service), max items for express

2. **Given** shift starting
   **When** cashier logs in
   **Then** must select or be assigned to a specific lane

3. **Given** lane assignment
   **When** processing transactions
   **Then** all receipts tagged with lane number for reporting

4. **Given** express lane
   **When** item count exceeds maximum
   **Then** displays warning "Express lane limit exceeded"

5. **Given** lane status
   **When** viewing dashboard
   **Then** shows: open/closed lanes, assigned cashiers, current queue (if integrated)

6. **Given** lane transfer
   **When** cashier moves to different lane
   **Then** logs assignment change with timestamp

7. **Given** lane performance
   **When** running reports
   **Then** shows by lane: transactions, average transaction time, items per hour, sales totals

---

## Epic 38: Inventory Analytics (NEW)

**Goal:** Implement advanced inventory analytics including stock valuation methods, automatic reorder generation, and shrinkage analysis for optimized inventory management.

**Dependencies:** Epic 8 (Inventory), Epic 9 (Purchasing)
**Mode:** Retail

### Story 38.1: Stock Valuation Methods

As an accountant,
I want to choose inventory valuation methods,
So that COGS is calculated according to accounting standards.

**Acceptance Criteria:**

1. **Given** inventory valuation needed
   **When** configuring
   **Then** can select: FIFO (First In First Out), LIFO (Last In First Out), or Weighted Average Cost

2. **Given** FIFO selected
   **When** calculating COGS
   **Then** uses oldest purchase cost for sold items

3. **Given** Weighted Average
   **When** receiving goods
   **Then** recalculates average cost based on (existing value + new purchase) / total qty

4. **Given** valuation method
   **When** running inventory valuation report
   **Then** shows stock value using selected method

---

### Story 38.2: Automatic Reorder Generation

As a purchasing manager,
I want the system to automatically generate purchase orders,
So that stock is replenished before running out.

**Acceptance Criteria:**

1. **Given** reorder points configured
   **When** stock falls below reorder point
   **Then** system generates suggested PO for the supplier

2. **Given** reorder quantity calculation
   **When** suggesting quantity
   **Then** considers: min/max stock, lead time, sales velocity

3. **Given** auto-PO generated
   **When** reviewing
   **Then** purchasing manager can approve, modify, or reject

4. **Given** multiple suppliers
   **When** generating POs
   **Then** groups items by preferred supplier

---

### Story 38.3: Shrinkage Analysis

As a loss prevention manager,
I want detailed shrinkage analysis,
So that I can identify and address inventory loss patterns.

**Acceptance Criteria:**

1. **Given** stock adjustments occur
   **When** analyzing shrinkage
   **Then** categorizes losses by: theft, damage, expiry, admin error

2. **Given** shrinkage data
   **When** running report
   **Then** shows shrinkage value by category, product, and location

3. **Given** trend analysis
   **When** charting
   **Then** shows shrinkage trends over time to identify spikes

4. **Given** high-shrinkage items
   **When** highlighting
   **Then** flags products exceeding shrinkage threshold for investigation

---

### Story 38.4: Dead Stock Identification

As a buyer,
I want to identify slow-moving and dead stock,
So that I can take action to clear inventory.

**Acceptance Criteria:**

1. **Given** inventory history
   **When** running dead stock report
   **Then** shows products with no sales in X days (configurable)

2. **Given** slow-moving stock
   **When** analyzing
   **Then** calculates days of stock on hand based on sales velocity

3. **Given** dead stock identified
   **When** recommending action
   **Then** suggests: markdown, return to supplier, or disposal

4. **Given** working capital impact
   **When** reporting
   **Then** shows value tied up in dead/slow stock
