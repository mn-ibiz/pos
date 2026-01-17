# Story 9.1: Supplier Management

Status: complete

## Story

As an administrator,
I want to manage supplier information,
So that purchases can be tracked by vendor.

## Acceptance Criteria

1. **Given** admin access
   **When** managing suppliers
   **Then** admin can create suppliers with: name, contact person, phone, email, address

2. **Given** supplier exists
   **When** modifying suppliers
   **Then** admin can edit and deactivate suppliers

3. **Given** suppliers exist
   **When** searching
   **Then** supplier list should be searchable

4. **Given** supplier list is displayed
   **When** viewing summary
   **Then** active supplier count should be displayed

## Tasks / Subtasks

- [x] Task 1: Create Supplier Entity
  - [x] Create Supplier entity class
  - [x] Configure EF Core mappings
  - [x] Create database migration (skipped per user request)
  - [x] Add navigation properties

- [x] Task 2: Create Supplier Service (Repository pattern via service)
  - [x] Create ISupplierService interface
  - [x] Implement CRUD methods
  - [x] Add search functionality
  - [x] Add active/inactive filter

- [x] Task 3: Create Supplier Management Screen
  - [x] Create SuppliersView.xaml
  - [x] Create SuppliersViewModel
  - [x] Display supplier grid
  - [x] Add search and filter

- [x] Task 4: Create Supplier Editor Dialog
  - [x] Create SupplierEditorDialog.xaml
  - [x] Create SupplierEditorResult class
  - [x] Validate required fields
  - [x] Handle create and edit modes

- [x] Task 5: Implement Supplier Actions
  - [x] Add new supplier
  - [x] Edit existing supplier
  - [x] Activate/deactivate supplier
  - [x] View purchase orders for supplier (navigation)

## Dev Notes

### Supplier Entity

```csharp
public class Supplier
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; } = "Kenya";
    public string? TaxId { get; set; }  // KRA PIN
    public string? BankAccount { get; set; }
    public string? BankName { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    public ICollection<GoodsReceivedNote> GoodsReceivedNotes { get; set; } = new List<GoodsReceivedNote>();
}
```

### Supplier Management Screen

```
+------------------------------------------+
|      SUPPLIER MANAGEMENT                  |
+------------------------------------------+
| Search: [______________]  [+ New Supplier]|
| Filter: [x]Active [ ]Inactive [x]All      |
+------------------------------------------+
| Code   | Name          | Contact | Status |
|--------|---------------|---------|--------|
| SUP001 | ABC Beverages | John    | Active |
| SUP002 | XYZ Foods     | Mary    | Active |
| SUP003 | Quick Supply  | Peter   | Inactive|
+------------------------------------------+
| Active Suppliers: 12  Total: 15           |
+------------------------------------------+
```

### Supplier Editor Dialog

```
+------------------------------------------+
|      NEW SUPPLIER                         |
+------------------------------------------+
|                                           |
|  Code: [SUP004________] (Auto-generated)  |
|                                           |
|  Company Name: *                          |
|  [ABC Beverages Ltd__________________]    |
|                                           |
|  Contact Person:                          |
|  [John Smith_________________________]    |
|                                           |
|  Phone:                                   |
|  [+254 7XX XXX XXX___________________]    |
|                                           |
|  Email:                                   |
|  [john@abcbeverages.co.ke____________]    |
|                                           |
|  Address:                                 |
|  [123 Industrial Area________________]    |
|  [Nairobi, Kenya_____________________]    |
|                                           |
|  KRA PIN:                                 |
|  [A12345678Z_________________________]    |
|                                           |
|  Bank Details:                            |
|  Account: [1234567890_____]               |
|  Bank: [Kenya Commercial Bank__]          |
|                                           |
|  Notes:                                   |
|  [Main beverage supplier_____________]    |
|                                           |
|  [Cancel]              [Save Supplier]    |
+------------------------------------------+
```

### SuppliersViewModel

```csharp
public partial class SuppliersViewModel : BaseViewModel
{
    [ObservableProperty]
    private ObservableCollection<Supplier> _suppliers = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _showActive = true;

    [ObservableProperty]
    private bool _showInactive;

    [ObservableProperty]
    private int _activeCount;

    [ObservableProperty]
    private int _totalCount;

    public async Task LoadAsync()
    {
        var suppliers = await _supplierRepo.GetAllAsync();
        Suppliers = new ObservableCollection<Supplier>(suppliers);

        ActiveCount = Suppliers.Count(s => s.IsActive);
        TotalCount = Suppliers.Count;

        ApplyFilters();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnShowActiveChanged(bool value) => ApplyFilters();
    partial void OnShowInactiveChanged(bool value) => ApplyFilters();

    private void ApplyFilters()
    {
        var filtered = Suppliers.Where(s =>
        {
            // Text search
            if (!string.IsNullOrEmpty(SearchText))
            {
                var search = SearchText.ToLower();
                if (!s.Name.ToLower().Contains(search) &&
                    !s.Code.ToLower().Contains(search) &&
                    !(s.ContactPerson?.ToLower().Contains(search) ?? false))
                    return false;
            }

            // Status filter
            if (s.IsActive && !ShowActive) return false;
            if (!s.IsActive && !ShowInactive) return false;

            return true;
        });

        FilteredSuppliers = new ObservableCollection<Supplier>(filtered);
    }

    [RelayCommand]
    private async Task AddSupplierAsync()
    {
        var newSupplier = new Supplier
        {
            Code = await GenerateSupplierCodeAsync()
        };

        var dialog = new SupplierEditorDialog(newSupplier, isNew: true);
        var result = await _dialogService.ShowDialogAsync(dialog);

        if (result == true)
        {
            await _supplierRepo.AddAsync(newSupplier);
            await _unitOfWork.SaveChangesAsync();

            Suppliers.Add(newSupplier);
            ActiveCount++;
            TotalCount++;
            ApplyFilters();
        }
    }

    [RelayCommand]
    private async Task EditSupplierAsync(Supplier supplier)
    {
        var dialog = new SupplierEditorDialog(supplier, isNew: false);
        var result = await _dialogService.ShowDialogAsync(dialog);

        if (result == true)
        {
            supplier.UpdatedAt = DateTime.UtcNow;
            await _supplierRepo.UpdateAsync(supplier);
            await _unitOfWork.SaveChangesAsync();
            ApplyFilters();
        }
    }

    [RelayCommand]
    private async Task ToggleActiveAsync(Supplier supplier)
    {
        supplier.IsActive = !supplier.IsActive;
        supplier.UpdatedAt = DateTime.UtcNow;

        await _supplierRepo.UpdateAsync(supplier);
        await _unitOfWork.SaveChangesAsync();

        ActiveCount = Suppliers.Count(s => s.IsActive);
        ApplyFilters();
    }

    private async Task<string> GenerateSupplierCodeAsync()
    {
        var count = await _supplierRepo.CountAsync();
        return $"SUP{(count + 1):D3}";
    }
}
```

### SupplierEditorViewModel

```csharp
public partial class SupplierEditorViewModel : BaseViewModel
{
    [ObservableProperty]
    private Supplier _supplier = null!;

    [ObservableProperty]
    private bool _isNew;

    [ObservableProperty]
    private string? _nameError;

    [ObservableProperty]
    private string? _phoneError;

    [ObservableProperty]
    private string? _emailError;

    public bool Validate()
    {
        var isValid = true;

        // Name is required
        if (string.IsNullOrWhiteSpace(Supplier.Name))
        {
            NameError = "Company name is required";
            isValid = false;
        }
        else
        {
            NameError = null;
        }

        // Phone format validation
        if (!string.IsNullOrEmpty(Supplier.Phone))
        {
            if (!Regex.IsMatch(Supplier.Phone, @"^\+?[\d\s-]+$"))
            {
                PhoneError = "Invalid phone format";
                isValid = false;
            }
            else
            {
                PhoneError = null;
            }
        }

        // Email format validation
        if (!string.IsNullOrEmpty(Supplier.Email))
        {
            if (!Regex.IsMatch(Supplier.Email, @"^[^@]+@[^@]+\.[^@]+$"))
            {
                EmailError = "Invalid email format";
                isValid = false;
            }
            else
            {
                EmailError = null;
            }
        }

        return isValid;
    }

    [RelayCommand]
    private void Save()
    {
        if (Validate())
        {
            CloseDialog(true);
        }
    }
}
```

### EF Core Configuration

```csharp
public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Code)
            .IsRequired()
            .HasMaxLength(20);
        builder.HasIndex(s => s.Code).IsUnique();

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Phone).HasMaxLength(20);
        builder.Property(s => s.Email).HasMaxLength(100);
        builder.Property(s => s.Address).HasMaxLength(200);

        builder.HasMany(s => s.PurchaseOrders)
            .WithOne(po => po.Supplier)
            .HasForeignKey(po => po.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

### Supplier List Print (80mm)

```
================================================
     SUPPLIER LIST
     2025-12-20
================================================
Code   | Name               | Phone
-------|--------------------|-----------------
SUP001 | ABC Beverages      | +254 7XX XXX
SUP002 | XYZ Foods          | +254 7XX XXX
SUP003 | Quick Supply       | +254 7XX XXX
SUP004 | Farm Fresh         | +254 7XX XXX
================================================
Total Active: 12
Total Suppliers: 15
================================================
```

### References
- [Source: docs/PRD_Hospitality_POS_System.md#5.6.1-Supplier-Management]
- [Source: docs/PRD_Hospitality_POS_System.md#PS-001 to PS-005]

## Dev Agent Record

### Agent Model Used
Claude Opus 4.5 (claude-opus-4-5-20251101)

### Completion Notes List

1. **Supplier Entity Enhanced**: Extended existing Supplier entity with Code, City, Country, BankAccount, BankName, and Notes properties. Country defaults to "Kenya".

2. **Supplier Code Auto-Generation**: Implemented automatic supplier code generation using pattern "SUP-XXXX" where XXXX is a zero-padded sequence number.

3. **ISupplierService Interface**: Created comprehensive service interface with CRUD operations, search functionality, active/inactive filtering, code uniqueness validation, and code generation.

4. **SupplierService Implementation**: Full implementation with Serilog logging, EF Core integration, and proper error handling. Uses IServiceScopeFactory for scoped DbContext access.

5. **SuppliersViewModel**: Implements INavigationAware for proper lifecycle management. Features search by name/code/contact, active/inactive filtering, and statistics display (total/active counts).

6. **SuppliersView.xaml**: Modern dark-themed UI matching existing views. Includes DataGrid with supplier list, search bar, filter toggles, statistics panel, and action buttons.

7. **SupplierEditorDialog**: Full-featured dialog with sections for Basic Info, Contact Details, Address (with City/Country), Tax & Banking (KRA PIN, Bank details), and Notes. Auto-generates supplier code for new suppliers.

8. **IDialogService Extended**: Added ShowSupplierEditorDialogAsync, ShowInfoAsync, and ShowConfirmAsync methods to support the new dialog workflow.

9. **DI Registration**: Registered ISupplierService/SupplierService in ServiceCollectionExtensions.cs and SuppliersViewModel in App.xaml.cs.

10. **Navigation Integration**: SuppliersViewModel can navigate to PurchaseOrdersView filtered by selected supplier.

### File List

**Modified Files:**
- src/HospitalityPOS.Core/Entities/Supplier.cs
- src/HospitalityPOS.Infrastructure/Data/Configurations/SupplierConfiguration.cs
- src/HospitalityPOS.Infrastructure/Extensions/ServiceCollectionExtensions.cs
- src/HospitalityPOS.WPF/App.xaml.cs
- src/HospitalityPOS.WPF/Services/IDialogService.cs
- src/HospitalityPOS.WPF/Services/DialogService.cs

**Created Files:**
- src/HospitalityPOS.Core/Interfaces/ISupplierService.cs
- src/HospitalityPOS.Infrastructure/Services/SupplierService.cs
- src/HospitalityPOS.WPF/ViewModels/SuppliersViewModel.cs
- src/HospitalityPOS.WPF/Views/SuppliersView.xaml
- src/HospitalityPOS.WPF/Views/SuppliersView.xaml.cs
- src/HospitalityPOS.WPF/Views/Dialogs/SupplierEditorDialog.xaml
- src/HospitalityPOS.WPF/Views/Dialogs/SupplierEditorDialog.xaml.cs
