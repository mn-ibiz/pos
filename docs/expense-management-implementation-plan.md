# Expense Management Module - Implementation Plan

## Executive Summary

This document outlines the implementation plan for a comprehensive Expense Management Module for the HospitalityPOS system. The module will enable businesses to accurately track, categorize, and analyze operational expenses, which will integrate with overall business health metrics and reporting.

---

## 1. Research Findings Summary

### 1.1 Industry Best Practices

Based on extensive research from leading expense management solutions (Fyle, ExpensePoint, Ramp, Bill.com), the following best practices are critical:

| Practice | Description |
|----------|-------------|
| **Transparency** | Clear visibility into all expenses across the organization |
| **Automation** | Minimize manual data entry with smart categorization |
| **Policy Enforcement** | Built-in controls to prevent unauthorized spending |
| **Real-time Tracking** | Instant access to current spending information |
| **Customizable Categories** | Industry-specific expense categories |
| **Approval Workflows** | Multi-level approval for expenses above thresholds |
| **Receipt Management** | Digital receipt capture and storage |
| **Integration Ready** | Seamless connection with accounting and reporting systems |

### 1.2 Restaurant/Hospitality Specific Requirements

For hospitality businesses, expense tracking must support **Prime Cost** calculations:

```
Prime Cost = Cost of Goods Sold (COGS) + Total Labor Cost
Prime Cost Percentage = Prime Cost / Total Sales × 100
```

**Industry Benchmarks:**
- Target Prime Cost: **55-65%** of total sales
- Food Cost Target: **25-35%** of sales
- Labor Cost Target: **25-35%** of sales
- Full-Service Restaurants: Should run no more than **65%**
- Limited-Service Restaurants: Target **60% or lower**

### 1.3 Essential Expense Categories for Hospitality

Based on IRS-recognized categories and hospitality industry standards:

**Core Operating Expenses:**
1. Cost of Goods Sold (COGS) - Food & Beverage
2. Labor Costs (wages, benefits, payroll taxes)
3. Rent/Lease payments
4. Utilities (electricity, gas, water, internet)
5. Equipment & Maintenance
6. Marketing & Advertising
7. Insurance
8. Professional Services (accounting, legal)
9. Supplies (cleaning, disposables, office)
10. Licenses & Permits
11. Technology & Software subscriptions
12. Bank Fees & Credit Card Processing
13. Repairs & Maintenance
14. Training & Development
15. Miscellaneous/Other

---

## 2. Data Model Design

### 2.1 Entity Relationship Diagram (Conceptual)

```
┌─────────────────────┐       ┌─────────────────────┐
│   ExpenseCategory   │       │      Vendor         │
├─────────────────────┤       ├─────────────────────┤
│ Id (PK)             │       │ Id (PK)             │
│ Name                │       │ Name                │
│ Description         │       │ ContactName         │
│ ParentCategoryId    │◄──┐   │ Phone               │
│ Type (enum)         │   │   │ Email               │
│ Icon                │   │   │ Address             │
│ Color               │   │   │ TaxId               │
│ IsActive            │   │   │ DefaultCategoryId   │──►
│ SortOrder           │   │   │ Notes               │
│ CreatedAt           │   │   │ IsActive            │
│ UpdatedAt           │   │   └─────────────────────┘
└─────────────────────┘   │
         │                │
         │                │
         ▼                │
┌─────────────────────┐   │   ┌─────────────────────┐
│      Expense        │   │   │   PaymentMethod     │
├─────────────────────┤   │   ├─────────────────────┤
│ Id (PK)             │   │   │ Id (PK)             │
│ CategoryId (FK)     │───┘   │ Name                │
│ VendorId (FK)       │       │ Type (enum)         │
│ PaymentMethodId(FK) │◄──────│ AccountNumber       │
│ Amount              │       │ IsActive            │
│ Date                │       └─────────────────────┘
│ Description         │
│ Reference           │       ┌─────────────────────┐
│ ReceiptImagePath    │       │  RecurringExpense   │
│ IsRecurring         │       ├─────────────────────┤
│ RecurringExpenseId  │◄──────│ Id (PK)             │
│ TaxAmount           │       │ CategoryId (FK)     │
│ IsTaxDeductible     │       │ VendorId (FK)       │
│ Notes               │       │ PaymentMethodId(FK) │
│ Status (enum)       │       │ Amount              │
│ ApprovedById (FK)   │       │ Description         │
│ CreatedById (FK)    │       │ Frequency (enum)    │
│ CreatedAt           │       │ StartDate           │
│ UpdatedAt           │       │ EndDate             │
└─────────────────────┘       │ NextDueDate         │
                              │ IsActive            │
                              │ AutoApprove         │
                              └─────────────────────┘

┌─────────────────────┐       ┌─────────────────────┐
│   ExpenseBudget     │       │   ExpenseAttachment │
├─────────────────────┤       ├─────────────────────┤
│ Id (PK)             │       │ Id (PK)             │
│ CategoryId (FK)     │       │ ExpenseId (FK)      │
│ Amount              │       │ FileName            │
│ Period (enum)       │       │ FilePath            │
│ Year                │       │ FileType            │
│ Month               │       │ FileSize            │
│ StartDate           │       │ UploadedAt          │
│ EndDate             │       └─────────────────────┘
│ AlertThreshold      │
│ IsActive            │
└─────────────────────┘
```

### 2.2 Core Entity Models

#### ExpenseCategory Model
```csharp
public class ExpenseCategory
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    [StringLength(500)]
    public string Description { get; set; }

    // Self-referencing for subcategories
    public int? ParentCategoryId { get; set; }
    public ExpenseCategory ParentCategory { get; set; }
    public ICollection<ExpenseCategory> SubCategories { get; set; }

    // For Prime Cost calculations
    public ExpenseCategoryType Type { get; set; }

    [StringLength(50)]
    public string Icon { get; set; }

    [StringLength(7)]
    public string Color { get; set; }

    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public bool IsSystemCategory { get; set; } // Cannot be deleted

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Expense> Expenses { get; set; }
}

public enum ExpenseCategoryType
{
    COGS,           // Cost of Goods Sold (Food, Beverage)
    Labor,          // Labor costs
    Occupancy,      // Rent, utilities
    Operating,      // General operating expenses
    Marketing,      // Advertising, promotions
    Administrative, // Office, professional services
    Other
}
```

#### Expense Model
```csharp
public class Expense
{
    public int Id { get; set; }

    [Required]
    public int CategoryId { get; set; }
    public ExpenseCategory Category { get; set; }

    public int? VendorId { get; set; }
    public Vendor Vendor { get; set; }

    public int? PaymentMethodId { get; set; }
    public PaymentMethod PaymentMethod { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Required]
    [StringLength(255)]
    public string Description { get; set; }

    [StringLength(100)]
    public string Reference { get; set; } // Invoice/Bill number

    [StringLength(500)]
    public string ReceiptImagePath { get; set; }

    public bool IsRecurring { get; set; }
    public int? RecurringExpenseId { get; set; }
    public RecurringExpense RecurringExpense { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; }

    public bool IsTaxDeductible { get; set; } = true;

    [StringLength(1000)]
    public string Notes { get; set; }

    public ExpenseStatus Status { get; set; } = ExpenseStatus.Pending;

    public int? ApprovedById { get; set; }
    public Employee ApprovedBy { get; set; }

    public int CreatedById { get; set; }
    public Employee CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ExpenseAttachment> Attachments { get; set; }
}

public enum ExpenseStatus
{
    Pending,
    Approved,
    Rejected,
    Paid,
    Voided
}
```

#### RecurringExpense Model
```csharp
public class RecurringExpense
{
    public int Id { get; set; }

    [Required]
    public int CategoryId { get; set; }
    public ExpenseCategory Category { get; set; }

    public int? VendorId { get; set; }
    public Vendor Vendor { get; set; }

    public int? PaymentMethodId { get; set; }
    public PaymentMethod PaymentMethod { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(255)]
    public string Description { get; set; }

    public RecurrenceFrequency Frequency { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public DateTime? NextDueDate { get; set; }

    public int DayOfMonth { get; set; } // For monthly recurring

    public bool IsActive { get; set; } = true;
    public bool AutoApprove { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Expense> GeneratedExpenses { get; set; }
}

public enum RecurrenceFrequency
{
    Daily,
    Weekly,
    BiWeekly,
    Monthly,
    Quarterly,
    Annually
}
```

#### Vendor Model
```csharp
public class Vendor
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; }

    [StringLength(100)]
    public string ContactName { get; set; }

    [StringLength(20)]
    public string Phone { get; set; }

    [StringLength(100)]
    [EmailAddress]
    public string Email { get; set; }

    [StringLength(500)]
    public string Address { get; set; }

    [StringLength(50)]
    public string TaxId { get; set; }

    public int? DefaultCategoryId { get; set; }
    public ExpenseCategory DefaultCategory { get; set; }

    [StringLength(1000)]
    public string Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Expense> Expenses { get; set; }
}
```

#### ExpenseBudget Model
```csharp
public class ExpenseBudget
{
    public int Id { get; set; }

    public int? CategoryId { get; set; } // Null = overall budget
    public ExpenseCategory Category { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public BudgetPeriod Period { get; set; }

    public int Year { get; set; }
    public int? Month { get; set; } // Null for annual budgets

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    [Range(0, 100)]
    public int AlertThreshold { get; set; } = 80; // Alert at 80% spent

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
}

public enum BudgetPeriod
{
    Weekly,
    Monthly,
    Quarterly,
    Annually
}
```

---

## 3. Feature Specifications

### 3.1 Core Features (MVP)

#### 3.1.1 Expense Entry
- **Quick Add**: Simple form for fast expense entry
- **Detailed Add**: Full form with all fields
- **Required Fields**: Category, Amount, Date, Description
- **Optional Fields**: Vendor, Payment Method, Reference, Tax, Notes
- **Receipt Upload**: Support for image attachments (JPG, PNG, PDF)

#### 3.1.2 Category Management
- **Hierarchical Categories**: Parent/child category structure
- **Default Categories**: Pre-populated hospitality-specific categories
- **Custom Categories**: Ability to add, edit, disable categories
- **Category Types**: For automatic Prime Cost grouping

#### 3.1.3 Vendor Management
- **Vendor Directory**: Maintain list of suppliers/vendors
- **Auto-populate**: Link vendors to default categories
- **Contact Information**: Store vendor details

#### 3.1.4 Recurring Expenses
- **Schedule Setup**: Define frequency and duration
- **Auto-generation**: Automatically create expense entries
- **Reminders**: Notifications for upcoming recurring expenses
- **Management**: Edit, pause, or stop recurring expenses

#### 3.1.5 Expense List & Search
- **Filterable List**: By date range, category, vendor, status
- **Sortable Columns**: Date, amount, category, vendor
- **Quick Search**: Search by description or reference
- **Export**: Export to CSV/Excel

### 3.2 Advanced Features (Phase 2)

#### 3.2.1 Budget Management
- **Set Budgets**: Per category or overall
- **Track Progress**: Visual budget utilization
- **Alerts**: Notifications when approaching/exceeding budget
- **Variance Analysis**: Compare actual vs budget

#### 3.2.2 Approval Workflow
- **Threshold-based Approval**: Expenses above X require approval
- **Multi-level Approval**: Role-based approval chain
- **Approval History**: Audit trail of approvals

#### 3.2.3 Analytics Dashboard
- **Expense Trends**: Line/area charts over time
- **Category Breakdown**: Pie/donut charts
- **Top Vendors**: Bar charts
- **Period Comparisons**: Month-over-month, year-over-year
- **Prime Cost Tracking**: Real-time prime cost percentage

### 3.3 Integration Features (Phase 3)

#### 3.3.1 Financial Reports Integration
- **P&L Integration**: Feed expenses into profit/loss reports
- **Prime Cost Reports**: Automated prime cost calculations
- **Cash Flow Impact**: Track cash outflows

#### 3.3.2 Business Health Metrics
- **KPI Dashboard Integration**: Expense metrics in main dashboard
- **Trend Analysis**: Historical expense patterns
- **Forecasting**: Predict future expenses based on history

---

## 4. UI/UX Design Recommendations

### 4.1 Design Principles

Based on research from leading expense management applications:

1. **Intuitive Interface**: Simple language, clear instructions
2. **Minimal Steps**: Quick expense entry in 3 clicks or less
3. **Visual Hierarchy**: Most important info prominently displayed
4. **Mobile-First Considerations**: Touch-friendly, scannable
5. **Consistent with Existing UI**: Match current POS design language

### 4.2 Key Screens

#### 4.2.1 Expense Dashboard
```
┌─────────────────────────────────────────────────────────────────┐
│  EXPENSES                                    [+ Add Expense]    │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐            │
│  │ This Month   │ │ Last Month   │ │ vs Budget    │            │
│  │ $12,450.00   │ │ $11,890.00   │ │ 78% Used     │            │
│  │ ↑ 4.7%       │ │              │ │ ████████░░   │            │
│  └──────────────┘ └──────────────┘ └──────────────┘            │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │ Expenses by Category (This Month)                          ││
│  │ ┌─────────────────────────────────────┐                    ││
│  │ │         [PIE CHART]                 │ COGS      $5,200   ││
│  │ │                                     │ Labor     $4,100   ││
│  │ │                                     │ Utilities $1,500   ││
│  │ │                                     │ Supplies    $850   ││
│  │ │                                     │ Other       $800   ││
│  │ └─────────────────────────────────────┘                    ││
│  └─────────────────────────────────────────────────────────────┘│
│                                                                 │
│  Recent Expenses                              [View All →]      │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │ Today                                                       ││
│  │ ├─ Water Bill (Utilities)            -$285.00    Pending   ││
│  │ ├─ Food Supplies (COGS)              -$1,250.00  Approved  ││
│  │ Yesterday                                                   ││
│  │ ├─ Internet Service (Utilities)      -$89.99     Paid      ││
│  │ └─ Office Supplies (Admin)           -$45.50     Paid      ││
│  └─────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘
```

#### 4.2.2 Add/Edit Expense Form
```
┌─────────────────────────────────────────────────────────────────┐
│  Add New Expense                                    [×]         │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Amount *                                                       │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ $                                                    0.00 │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  Category *                        Date *                       │
│  ┌─────────────────────────┐      ┌─────────────────────────┐  │
│  │ Select category      ▼ │      │ Jan 21, 2026       📅   │  │
│  └─────────────────────────┘      └─────────────────────────┘  │
│                                                                 │
│  Description *                                                  │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ Enter expense description                                 │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  Vendor                            Payment Method               │
│  ┌─────────────────────────┐      ┌─────────────────────────┐  │
│  │ Select vendor        ▼ │      │ Select method        ▼ │  │
│  └─────────────────────────┘      └─────────────────────────┘  │
│                                                                 │
│  Reference/Invoice #               Tax Amount                   │
│  ┌─────────────────────────┐      ┌─────────────────────────┐  │
│  │                         │      │ $                  0.00 │  │
│  └─────────────────────────┘      └─────────────────────────┘  │
│                                                                 │
│  ☐ This is a recurring expense                                 │
│  ☑ Tax deductible                                              │
│                                                                 │
│  Receipt                                                        │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │     📷  Drop image here or click to upload                │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  Notes                                                          │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │                                                           │ │
│  │                                                           │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│         [Cancel]                      [Save Expense]            │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

#### 4.2.3 Expense List View
```
┌─────────────────────────────────────────────────────────────────┐
│  All Expenses                                                   │
├─────────────────────────────────────────────────────────────────┤
│  ┌────────────┐ ┌────────────┐ ┌────────────┐ ┌─────────────┐  │
│  │ Date Range │ │ Category   │ │ Status     │ │ 🔍 Search   │  │
│  │ This Month▼│ │ All      ▼ │ │ All      ▼ │ │             │  │
│  └────────────┘ └────────────┘ └────────────┘ └─────────────┘  │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │ Date     │ Description        │ Category  │ Amount  │ St │  │
│  ├──────────┼────────────────────┼───────────┼─────────┼────┤  │
│  │ 01/21/26 │ Water Bill January │ Utilities │ $285.00 │ ⏳ │  │
│  │ 01/21/26 │ Sysco Food Delivery│ COGS      │$1,250.00│ ✓  │  │
│  │ 01/20/26 │ Comcast Internet   │ Utilities │  $89.99 │ ✓  │  │
│  │ 01/20/26 │ Staples Supplies   │ Admin     │  $45.50 │ ✓  │  │
│  │ 01/19/26 │ PG&E Electric Bill │ Utilities │ $892.00 │ ✓  │  │
│  └──────────┴────────────────────┴───────────┴─────────┴────┘  │
│                                                                 │
│  Showing 1-5 of 127 expenses          [< Prev] [1] [2] [Next >]│
│                                                                 │
│  Total: $2,562.49                                [Export CSV]   │
└─────────────────────────────────────────────────────────────────┘
```

### 4.3 Chart Recommendations

| Visualization | Use Case | Chart Type |
|--------------|----------|------------|
| Category Breakdown | Show spending distribution | Donut/Pie Chart |
| Expense Trends | Show spending over time | Line/Area Chart |
| Budget Progress | Show utilization | Progress Bar/Gauge |
| Top Vendors | Compare vendor spending | Horizontal Bar |
| Period Comparison | Compare months/years | Grouped Bar Chart |
| Prime Cost Trend | Track prime cost % | Line Chart with Target |

---

## 5. Default Category Structure

### 5.1 Pre-populated Categories for Hospitality

```
📁 Cost of Goods Sold (COGS)
   ├── 🍽️ Food Purchases
   │   ├── Produce
   │   ├── Meat & Poultry
   │   ├── Seafood
   │   ├── Dairy & Eggs
   │   ├── Dry Goods & Pantry
   │   └── Frozen Foods
   ├── 🍷 Beverage Purchases
   │   ├── Non-Alcoholic
   │   ├── Beer
   │   ├── Wine
   │   └── Spirits
   └── 📦 Packaging & Disposables

📁 Labor Costs
   ├── 👥 Wages & Salaries
   │   ├── Front of House
   │   ├── Back of House
   │   └── Management
   ├── 💼 Payroll Taxes
   ├── 🏥 Employee Benefits
   └── 📚 Training & Development

📁 Occupancy
   ├── 🏢 Rent/Lease
   ├── 💡 Utilities
   │   ├── Electricity
   │   ├── Gas
   │   ├── Water & Sewer
   │   └── Trash Removal
   ├── 📱 Telecommunications
   │   ├── Phone/Internet
   │   └── POS/Technology Services
   └── 🏠 Property Insurance

📁 Operating Expenses
   ├── 🧹 Cleaning Supplies
   ├── 🛠️ Repairs & Maintenance
   ├── 🔧 Equipment (Small)
   ├── 🧾 Office Supplies
   ├── 💳 Bank & CC Processing Fees
   ├── 📜 Licenses & Permits
   └── 🚗 Delivery & Transportation

📁 Marketing & Advertising
   ├── 📣 Online Advertising
   ├── 🖨️ Print Materials
   ├── 🎁 Promotions & Discounts
   └── 📸 Photography & Media

📁 Administrative
   ├── 📊 Accounting Services
   ├── ⚖️ Legal Services
   ├── 🔐 Security Services
   ├── 💻 Software & Subscriptions
   └── 📋 Professional Development

📁 Other Expenses
   ├── 🎄 Seasonal/Holiday
   ├── 🤝 Charitable Contributions
   └── ❓ Miscellaneous
```

---

## 6. Integration with Business Health Metrics

### 6.1 Key Performance Indicators (KPIs)

The expense module will feed into the following business health metrics:

| KPI | Formula | Target |
|-----|---------|--------|
| **Prime Cost %** | (COGS + Labor) / Sales × 100 | 55-65% |
| **Food Cost %** | Food COGS / Food Sales × 100 | 28-35% |
| **Beverage Cost %** | Bev COGS / Bev Sales × 100 | 18-24% |
| **Labor Cost %** | Labor Costs / Sales × 100 | 25-35% |
| **Occupancy Cost %** | Occupancy / Sales × 100 | 5-10% |
| **Total Operating Expense %** | All Expenses / Sales × 100 | 85-92% |

### 6.2 Data Flow Architecture

```
┌────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│   EXPENSES     │────►│  EXPENSE MODULE  │────►│   REPORTS       │
│   (Input)      │     │  (Processing)    │     │   (Output)      │
└────────────────┘     └──────────────────┘     └─────────────────┘
                              │
                              │ Categorized Data
                              ▼
                    ┌──────────────────┐
                    │  PRIME COST      │
                    │  CALCULATION     │
                    └──────────────────┘
                              │
          ┌───────────────────┼───────────────────┐
          ▼                   ▼                   ▼
   ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
   │   P&L       │    │   KPI       │    │   BUSINESS  │
   │   REPORT    │    │   DASHBOARD │    │   HEALTH    │
   └─────────────┘    └─────────────┘    └─────────────┘
```

---

## 7. Implementation Phases

### Phase 1: Foundation (MVP)
**Duration: 2-3 Sprints**

- [ ] Database schema creation and migrations
- [ ] ExpenseCategory entity and CRUD operations
- [ ] Expense entity and CRUD operations
- [ ] Vendor entity and CRUD operations
- [ ] PaymentMethod entity and CRUD operations
- [ ] Basic expense entry form UI
- [ ] Expense list view with filtering
- [ ] Category management UI
- [ ] Default categories seeding

### Phase 2: Enhanced Features
**Duration: 2-3 Sprints**

- [ ] RecurringExpense entity and logic
- [ ] Recurring expense auto-generation service
- [ ] Receipt image upload and storage
- [ ] ExpenseBudget entity and management
- [ ] Budget tracking and alerts
- [ ] Expense dashboard with charts
- [ ] Export functionality (CSV/Excel)
- [ ] Basic approval workflow

### Phase 3: Analytics & Integration
**Duration: 2 Sprints**

- [ ] Prime cost calculation service
- [ ] Integration with P&L reports
- [ ] Advanced analytics dashboard
- [ ] Period comparison reports
- [ ] Trend analysis and forecasting
- [ ] Integration with main business health dashboard
- [ ] Mobile-optimized views

### Phase 4: Advanced Features
**Duration: 1-2 Sprints**

- [ ] Multi-level approval workflows
- [ ] Vendor performance analytics
- [ ] Budget variance reporting
- [ ] Automated expense categorization suggestions
- [ ] Expense splitting across categories
- [ ] Audit trail and compliance features

---

## 8. Technical Specifications

### 8.1 Technology Stack

| Component | Technology |
|-----------|------------|
| Backend | ASP.NET Core 8 |
| ORM | Entity Framework Core |
| Database | SQL Server |
| Frontend | Blazor / WPF (consistent with existing) |
| Charts | Syncfusion / LiveCharts |
| File Storage | Local filesystem / Azure Blob |
| Reporting | RDLC / Custom PDF generation |

### 8.2 API Endpoints (If Web API)

```
GET    /api/expenses                 - List expenses (with filters)
GET    /api/expenses/{id}            - Get expense details
POST   /api/expenses                 - Create expense
PUT    /api/expenses/{id}            - Update expense
DELETE /api/expenses/{id}            - Delete expense

GET    /api/expenses/categories      - List categories
POST   /api/expenses/categories      - Create category
PUT    /api/expenses/categories/{id} - Update category

GET    /api/expenses/vendors         - List vendors
POST   /api/expenses/vendors         - Create vendor

GET    /api/expenses/recurring       - List recurring expenses
POST   /api/expenses/recurring       - Create recurring expense

GET    /api/expenses/budgets         - List budgets
POST   /api/expenses/budgets         - Create budget

GET    /api/expenses/reports/summary         - Get expense summary
GET    /api/expenses/reports/by-category     - Get by category breakdown
GET    /api/expenses/reports/prime-cost      - Get prime cost report
GET    /api/expenses/reports/trends          - Get expense trends
```

### 8.3 File Structure (Proposed)

```
src/
├── HospitalityPOS.Core/
│   └── Models/
│       └── Expenses/
│           ├── Expense.cs
│           ├── ExpenseCategory.cs
│           ├── ExpenseBudget.cs
│           ├── RecurringExpense.cs
│           ├── Vendor.cs
│           ├── PaymentMethod.cs
│           └── ExpenseAttachment.cs
│
├── HospitalityPOS.Infrastructure/
│   ├── Data/
│   │   └── Configurations/
│   │       └── Expenses/
│   │           ├── ExpenseConfiguration.cs
│   │           ├── ExpenseCategoryConfiguration.cs
│   │           └── ...
│   └── Services/
│       └── Expenses/
│           ├── IExpenseService.cs
│           ├── ExpenseService.cs
│           ├── IRecurringExpenseService.cs
│           ├── RecurringExpenseService.cs
│           └── IPrimeCostCalculationService.cs
│
└── HospitalityPOS.UI/
    └── Views/
        └── Expenses/
            ├── ExpenseDashboardView.xaml
            ├── ExpenseListView.xaml
            ├── ExpenseFormView.xaml
            ├── CategoryManagementView.xaml
            ├── VendorManagementView.xaml
            ├── BudgetManagementView.xaml
            └── RecurringExpenseView.xaml
```

---

## 9. Success Metrics

| Metric | Target |
|--------|--------|
| Expense entry time | < 30 seconds for basic entry |
| Data accuracy | 99%+ (proper categorization) |
| Report generation | < 3 seconds |
| User adoption | 90%+ staff using digital entry |
| Budget visibility | Real-time budget status |
| Prime cost accuracy | Match to penny with manual calc |

---

## 10. Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Data migration from existing systems | High | Provide import tools and templates |
| User adoption resistance | Medium | Training materials, intuitive UI |
| Category misconfiguration | Medium | Sensible defaults, validation rules |
| Performance with large datasets | Low | Pagination, indexing, caching |
| Receipt storage costs | Low | Compression, archival policies |

---

## 11. References

### Research Sources
- Fyle - Expense Management Best Practices
- ExpensePoint - Top 5 Features
- Ramp - Business Expense Categories
- Bill.com - Best Expense Management Software 2025
- NetSuite - 36 Business Expense Categories
- Restaurant365 - Food Cost Guide
- Toast POS - Restaurant Prime Cost
- PatternFly - Dashboard Design Guidelines
- Gartner - Expense Management Software Reviews

### Industry Standards
- IRS Publication 535 - Business Expenses
- GAAP Expense Recognition Principles
- Restaurant Industry Standard Chart of Accounts

---

*Document Version: 1.0*
*Created: January 21, 2026*
*Author: AI Implementation Assistant*
