# Story 4.1: Category Management

Status: done

## Story

As an administrator,
I want to create and manage product categories,
So that products are organized for easy navigation.

## Acceptance Criteria

1. **Given** the admin is logged in
   **When** accessing category management
   **Then** admin can create categories with: name, parent category (optional), image, display order

2. **Given** categories exist
   **When** editing categories
   **Then** admin can edit existing categories

3. **Given** categories exist
   **When** managing status
   **Then** admin can activate/deactivate categories

4. **Given** categories are displayed
   **When** ordering is needed
   **Then** admin can reorder categories via drag-and-drop or order number

5. **Given** categories are created
   **When** nesting is needed
   **Then** hierarchical subcategories should be supported

## Tasks / Subtasks

- [x] Task 1: Create Category Management View (AC: #1, #2, #3)
  - [x] Create CategoryManagementView.xaml
  - [x] Display TreeView for hierarchical categories
  - [x] Add Create, Edit, Delete buttons
  - [x] Show active/inactive status

- [x] Task 2: Create Category Editor Dialog (AC: #1, #4, #5)
  - [x] Create CategoryEditorDialog.xaml
  - [x] Add name input field
  - [x] Add parent category dropdown
  - [x] Add image upload/selection
  - [x] Add display order input

- [x] Task 3: Implement Category Service (AC: #1-5)
  - [x] Create ICategoryService interface
  - [x] Implement GetAllCategoriesAsync
  - [x] Implement GetCategoryTreeAsync
  - [x] Implement CreateCategoryAsync
  - [x] Implement UpdateCategoryAsync
  - [x] Implement ReorderCategoriesAsync

- [x] Task 4: Implement Reordering (AC: #4)
  - [x] Add MoveUp/MoveDown commands in TreeView
  - [x] Update DisplayOrder on move
  - [x] Save new order to database

- [x] Task 5: Implement Image Management
  - [x] Allow image file selection
  - [x] Store images path in category
  - [x] Display preview in editor

## Dev Notes

### Category Entity

```csharp
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentCategoryId { get; set; }
    public string? ImagePath { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Category? ParentCategory { get; set; }
    public ICollection<Category> SubCategories { get; set; } = new List<Category>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
```

### Category Tree View Layout

```
+------------------------------------------+
|  Category Management         [+ New]     |
+------------------------------------------+
|  +------------------------------------+  |
|  | > Beverages                        |  |
|  |   > Hot Drinks                     |  |
|  |     - Coffee                       |  |
|  |     - Tea                          |  |
|  |   > Cold Drinks                    |  |
|  |     - Soda                         |  |
|  |     - Juice                        |  |
|  |   > Alcoholic                      |  |
|  |     - Beer                         |  |
|  |     - Wine                         |  |
|  | > Food                             |  |
|  |   > Main Course                    |  |
|  |   > Starters                       |  |
|  |   > Desserts                       |  |
|  +------------------------------------+  |
|                                          |
|  [Edit] [Deactivate] [Delete]            |
+------------------------------------------+
```

### Category Editor Dialog

```
+------------------------------------------+
|  Category Editor                          |
+------------------------------------------+
|                                           |
|  Name*: [____________________]            |
|                                           |
|  Parent Category:                         |
|  [None (Top Level)          ▼]            |
|                                           |
|  Display Order: [5]                       |
|                                           |
|  Image:                                   |
|  +---------------+                        |
|  |   [IMAGE]     |  [Browse...]           |
|  |   Preview     |  [Clear]               |
|  +---------------+                        |
|                                           |
|  [x] Active                               |
|                                           |
|  [Save]  [Cancel]                         |
+------------------------------------------+
```

### ICategoryService Interface

```csharp
public interface ICategoryService
{
    Task<IEnumerable<Category>> GetAllCategoriesAsync();
    Task<IEnumerable<Category>> GetActiveCategoriesAsync();
    Task<IEnumerable<Category>> GetRootCategoriesAsync();
    Task<Category?> GetCategoryByIdAsync(int id);
    Task<Category> CreateCategoryAsync(CategoryDto dto);
    Task UpdateCategoryAsync(int id, CategoryDto dto);
    Task<bool> DeleteCategoryAsync(int id);
    Task ReorderCategoriesAsync(List<CategoryOrderDto> orderings);
    Task<bool> HasProductsAsync(int categoryId);
}
```

### Validation Rules
- Name is required, max 100 characters
- Name should be unique within same parent
- Cannot delete category with products
- Cannot set parent to self or descendant

### Image Storage
- Store in: Images/Categories/{categoryId}.jpg
- Resize to 200x200 pixels
- Support JPG, PNG, GIF formats
- Show placeholder if no image

### References
- [Source: docs/PRD_Hospitality_POS_System.md#6.1.2-Product-Categories]

## Dev Agent Record

### Agent Model Used
Claude Opus 4.5 (claude-opus-4-5-20251101)

### Completion Notes List
- Created ICategoryService interface with CategoryDto and CategoryOrderDto in Core layer
- Implemented CategoryService with comprehensive CRUD operations, tree building, and audit logging
- Created CategoryManagementView with hierarchical TreeView display and status badges
- Created CategoryManagementViewModel with full CRUD commands and permission checking
- Created CategoryEditorDialog with form fields for name, parent, display order, image, and active status
- Updated IDialogService and DialogService with ShowCategoryEditorDialogAsync method
- Registered ICategoryService and CategoryManagementViewModel in DI container
- Added NavigateToCategoryManagement command to MainViewModel
- Added Products.Manage permission to PermissionNames and database seeder
- Added SetBusy helper method to MainViewModel for busy state management

### File List
- src/HospitalityPOS.Core/Entities/Category.cs (verified - IsActive property exists)
- src/HospitalityPOS.Core/Interfaces/ICategoryService.cs (new)
- src/HospitalityPOS.Core/Constants/PermissionNames.cs (modified - added Products.Manage)
- src/HospitalityPOS.Infrastructure/Services/CategoryService.cs (new)
- src/HospitalityPOS.Infrastructure/Data/DatabaseSeeder.cs (modified - added Products.Manage permission)
- src/HospitalityPOS.WPF/Views/CategoryManagementView.xaml (new)
- src/HospitalityPOS.WPF/Views/CategoryManagementView.xaml.cs (new)
- src/HospitalityPOS.WPF/Views/Dialogs/CategoryEditorDialog.xaml (new)
- src/HospitalityPOS.WPF/Views/Dialogs/CategoryEditorDialog.xaml.cs (new)
- src/HospitalityPOS.WPF/ViewModels/CategoryManagementViewModel.cs (new)
- src/HospitalityPOS.WPF/Services/IDialogService.cs (modified)
- src/HospitalityPOS.WPF/Services/DialogService.cs (modified)
- src/HospitalityPOS.WPF/ViewModels/MainViewModel.cs (modified - added navigation commands)
- src/HospitalityPOS.WPF/App.xaml.cs (modified - registered services)

### Change Log
- 2025-12-30: Story implemented - All tasks completed
- 2025-12-30: Code review completed - 6 issues found and fixed

### Code Review Results

| # | Severity | Issue | Resolution |
|---|----------|-------|------------|
| 1 | CRITICAL | MainViewModel used non-existent `PermissionNames.Users.ManageRoles` | Changed to `PermissionNames.Users.AssignRoles` |
| 2 | HIGH | CategoryManagementViewModel used hardcoded permission strings | Replaced with `PermissionNames.Products.Manage` constant |
| 3 | MEDIUM | CategoryService audit log missing EntityId on create | Moved audit log creation after SaveChangesAsync to capture EntityId |
| 4 | MEDIUM | IsNameUniqueAsync used ToLower() which may have collation issues | Changed to use EF.Functions.Collate for SQL Server compatibility |
| 5 | MEDIUM | IsDescendantOfAsync had N+1 query pattern | Optimized to fetch all parent relationships in single query |
| 6 | LOW | CategoryEditorDialog swallowed exceptions silently | Added Debug.WriteLine logging for image load failures |
