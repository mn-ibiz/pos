# Story 4.2: Product Creation

Status: done

## Story

As an administrator,
I want to create products with all required information,
So that they can be sold through the POS.

## Acceptance Criteria

1. **Given** at least one category exists
   **When** creating a new product
   **Then** admin can enter: code/SKU, name, description, category, selling price, cost price

2. **Given** product details are entered
   **When** configuring additional fields
   **Then** admin can set: tax rate (default 16%), unit of measure, min/max stock levels

3. **Given** product is being created
   **When** visual identity is needed
   **Then** admin can upload product image

4. **Given** product is being created
   **When** barcode scanning is needed
   **Then** admin can enter barcode/QR code

5. **Given** product code is entered
   **When** uniqueness is checked
   **Then** product code should be unique

6. **Given** product is created
   **When** default status is set
   **Then** product should be active by default

## Tasks / Subtasks

- [x] Task 1: Create Product Management View (AC: #1-6)
  - [x] Create ProductManagementView.xaml with dark theme
  - [x] Display product list with DataGrid (Code, Name, Category, Price, Stock, Status)
  - [x] Add search/filter with 300ms delay
  - [x] Add Create, Edit, Delete, Activate/Deactivate buttons
  - [x] Filter by category dropdown

- [x] Task 2: Create Product Editor Dialog (AC: #1-4)
  - [x] Create ProductEditorDialog.xaml with touch-optimized UI
  - [x] Add all required fields (Code, Name, Description, Category, Barcode)
  - [x] Add category dropdown with active categories
  - [x] Add image upload section with preview
  - [x] Add pricing section (Selling Price, Cost Price, Tax Rate)
  - [x] Add inventory section (Unit of Measure, Min/Max Stock, Initial Stock)

- [x] Task 3: Implement Product Service (AC: #1-6)
  - [x] Create IProductService interface with DTOs
  - [x] Implement ProductService with full CRUD operations
  - [x] Validate code/barcode uniqueness
  - [x] Create inventory record on product creation
  - [x] Handle image path storage
  - [x] Add audit logging for all operations

- [x] Task 4: Create Product ViewModel (AC: #5, #6)
  - [x] Implement ProductManagementViewModel with INavigationAware
  - [x] Load categories for filtering
  - [x] Validate all required fields in dialog
  - [x] Permission-based access (Products.Create/Edit/Delete)
  - [x] Search by name, code, or barcode

- [x] Task 5: Implement Image Upload
  - [x] Add OpenFileDialog for image selection
  - [x] Display preview in ProductEditorDialog
  - [x] Clear image functionality
  - [x] Store image path (full path for now)

- [x] Task 6: Register Services and Navigation
  - [x] Register IProductService/ProductService in App.xaml.cs
  - [x] Register ProductManagementViewModel in App.xaml.cs
  - [x] Add ShowProductEditorDialogAsync to IDialogService
  - [x] Implement ShowProductEditorDialogAsync in DialogService
  - [x] Add NavigateToProductManagement command to MainViewModel

## Dev Notes

### Product Entity

```csharp
public class Product
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal? CostPrice { get; set; }
    public decimal TaxRate { get; set; } = 16.00m;
    public string UnitOfMeasure { get; set; } = "Each";
    public string? ImagePath { get; set; }
    public string? Barcode { get; set; }
    public decimal? MinStockLevel { get; set; }
    public decimal? MaxStockLevel { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Category Category { get; set; } = null!;
    public Inventory? Inventory { get; set; }
}
```

### Product Editor Layout

```
+------------------------------------------+
|  Product Editor                           |
+------------------------------------------+
|  +-------------+                          |
|  |   [IMAGE]   |  [Upload Image]          |
|  |   Preview   |                          |
|  +-------------+                          |
|                                           |
|  Basic Information                        |
|  ─────────────────────────────────────    |
|  Code/SKU*:  [BEV-001_____________]       |
|  Name*:      [Tusker Lager________]       |
|  Description:[Cold bottled beer___]       |
|              [___________________]        |
|  Category*:  [Beverages > Beer    ▼]      |
|  Barcode:    [5901234123457_______]       |
|                                           |
|  Pricing                                  |
|  ─────────────────────────────────────    |
|  Selling Price*: KSh [350.00______]       |
|  Cost Price:     KSh [200.00______]       |
|  Tax Rate:       [16] %                   |
|                                           |
|  Inventory                                |
|  ─────────────────────────────────────    |
|  Unit of Measure: [Bottle         ▼]      |
|  Min Stock Level: [10_____________]       |
|  Max Stock Level: [100____________]       |
|                                           |
|  [x] Active                               |
|                                           |
|  [Save Product]  [Cancel]                 |
+------------------------------------------+
```

### IProductService Interface

```csharp
public interface IProductService
{
    Task<IEnumerable<Product>> GetAllProductsAsync();
    Task<IEnumerable<Product>> GetActiveProductsAsync();
    Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId);
    Task<Product?> GetByIdAsync(int id);
    Task<Product?> GetByCodeAsync(string code);
    Task<Product?> GetByBarcodeAsync(string barcode);
    Task<Product> CreateProductAsync(CreateProductDto dto);
    Task UpdateProductAsync(int id, UpdateProductDto dto);
    Task<bool> DeactivateProductAsync(int id);
    Task<bool> IsCodeUniqueAsync(string code, int? excludeId = null);
    Task<IEnumerable<Product>> SearchAsync(string searchTerm);
}
```

### CreateProductDto

```csharp
public class CreateProductDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal? CostPrice { get; set; }
    public decimal TaxRate { get; set; } = 16.00m;
    public string UnitOfMeasure { get; set; } = "Each";
    public string? ImagePath { get; set; }
    public string? Barcode { get; set; }
    public decimal? MinStockLevel { get; set; }
    public decimal? MaxStockLevel { get; set; }
    public decimal InitialStock { get; set; } = 0;
}
```

### Validation Rules
- Code: Required, unique, 3-50 characters
- Name: Required, 2-200 characters
- Category: Required
- Selling Price: Required, must be > 0
- Cost Price: Optional, must be >= 0
- Tax Rate: 0-100, default 16%
- Min Stock: Optional, must be >= 0
- Max Stock: Optional, must be > Min Stock

### Unit of Measure Options
- Each (default)
- Bottle
- Can
- Glass
- Plate
- Portion
- Kilogram
- Gram
- Liter
- Milliliter

### Image Storage
- Path: Images/Products/{productCode}.jpg
- Size: 300x300 pixels
- Supported formats: JPG, PNG, GIF
- Max file size: 2MB

### References
- [Source: docs/PRD_Hospitality_POS_System.md#6.1-Product-Management]
- [Source: docs/PRD_Hospitality_POS_System.md#6.1.1-Product-Information]

## Dev Agent Record

### Agent Model Used
Claude Opus 4.5 (claude-opus-4-5-20251101)

### Completion Notes List

1. **Product Entity** - Already exists with all required properties via BaseEntity inheritance (Id, IsActive, CreatedAt, UpdatedAt, CreatedByUserId, UpdatedByUserId)

2. **IProductService Interface** - Created with:
   - CreateProductDto and UpdateProductDto
   - CRUD operations: GetAllProductsAsync, GetActiveProductsAsync, GetByIdAsync, GetByCodeAsync, GetByBarcodeAsync, CreateProductAsync, UpdateProductAsync, DeleteProductAsync
   - Uniqueness checks: IsCodeUniqueAsync, IsBarcodeUniqueAsync
   - Query operations: GetByCategoryAsync, SearchAsync, GetLowStockProductsAsync
   - Status management: SetProductActiveAsync

3. **ProductService** - Full implementation with:
   - IServiceScopeFactory pattern for scoped DbContext access
   - Complete validation (code uniqueness, barcode uniqueness, category exists, price > 0, stock levels)
   - Automatic Inventory record creation with initial stock
   - Audit logging for all CRUD operations
   - Soft delete check before hard delete
   - Uses EF.Functions.Collate for case-insensitive string matching

4. **ProductManagementView** - Touch-optimized DataGrid with:
   - Dark theme (#1E1E2E, #2D2D44 backgrounds)
   - 44px minimum height buttons
   - Search with 300ms delay for performance
   - Category filter dropdown
   - Show Inactive checkbox
   - Status badges (Active/Inactive with color coding)

5. **ProductEditorDialog** - Modal dialog with:
   - Sections: Basic Information, Pricing, Inventory, Product Image
   - Unit of Measure dropdown (Each, Bottle, Can, Glass, Plate, Portion, Kilogram, Gram, Liter, Milliliter)
   - Initial Stock field (shown only for new products)
   - Image preview with Browse/Clear buttons
   - Comprehensive validation with error display

6. **Navigation** - Added NavigateToProductManagement command to MainViewModel with Products.View permission check

### File List

**Core Layer:**
- src/HospitalityPOS.Core/Interfaces/IProductService.cs (new)

**Infrastructure Layer:**
- src/HospitalityPOS.Infrastructure/Services/ProductService.cs (new)

**WPF Layer:**
- src/HospitalityPOS.WPF/ViewModels/ProductManagementViewModel.cs (new)
- src/HospitalityPOS.WPF/Views/ProductManagementView.xaml (new)
- src/HospitalityPOS.WPF/Views/ProductManagementView.xaml.cs (new)
- src/HospitalityPOS.WPF/Views/Dialogs/ProductEditorDialog.xaml (new)
- src/HospitalityPOS.WPF/Views/Dialogs/ProductEditorDialog.xaml.cs (new)
- src/HospitalityPOS.WPF/Services/IDialogService.cs (modified - added ShowProductEditorDialogAsync)
- src/HospitalityPOS.WPF/Services/DialogService.cs (modified - implemented ShowProductEditorDialogAsync)
- src/HospitalityPOS.WPF/ViewModels/MainViewModel.cs (modified - added NavigateToProductManagement)
- src/HospitalityPOS.WPF/App.xaml.cs (modified - registered services)

### Code Review Results

**Review Date:** 2025-12-30
**Reviewer:** Claude Opus 4.5 (Adversarial Review)
**Issues Found:** 5
**Issues Fixed:** 5

#### Issues and Fixes:

1. **MEDIUM - IsCodeUniqueAsync collation issue** (ProductService.cs:440)
   - **Problem:** String comparison `p.Code == trimmedCode` depended on database collation
   - **Fix:** Used `EF.Functions.Collate()` with `Latin1_General_CI_AS` for case-insensitive comparison

2. **MEDIUM - IsBarcodeUniqueAsync collation issue** (ProductService.cs:458)
   - **Problem:** Barcode comparison was collation-dependent
   - **Fix:** Used `EF.Functions.Collate()` with `Latin1_General_CI_AS` for case-insensitive comparison

3. **MEDIUM - SearchAsync ToLower() collation issue** (ProductService.cs:494-496)
   - **Problem:** Used `.ToLower()` in EF query which may not work correctly with all SQL Server collations
   - **Fix:** Replaced with `EF.Functions.Collate()` for consistent case-insensitive searching

4. **LOW - SearchAsync LIKE pattern injection** (ProductService.cs:494-496)
   - **Problem:** User input containing `%` or `_` wildcards could produce unexpected results
   - **Fix:** Escaped special LIKE characters (`[`, `%`, `_`) in search term before use

5. **LOW - Missing TaxRate validation in service layer** (ProductService.cs)
   - **Problem:** TaxRate was validated in UI (0-100) but not in service layer
   - **Fix:** Added validation in both CreateProductAsync and UpdateProductAsync to ensure TaxRate is between 0 and 100
