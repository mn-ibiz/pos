# Story 14.1: Product Offer Configuration

Status: done

## Story

As a manager,
I want to create promotional offers for products,
So that I can run sales and attract customers.

## Acceptance Criteria

1. **Given** access to Offer Management
   **When** creating an offer
   **Then** can specify: product, offer name, offer price or discount %, start date, end date

2. **Given** offer details entered
   **When** saving the offer
   **Then** offer is validated (end date > start date, price > 0)

3. **Given** an active offer exists
   **When** editing the offer
   **Then** changes apply immediately if within date range

## Tasks / Subtasks

- [x] Task 1: Create ProductOffer Entity
  - [x] Create ProductOffer class with OfferPricingType and OfferStatus enums
  - [x] Add EF Core configuration (ProductOfferConfiguration.cs)
  - [x] Database indexes for active offer lookup
  - [x] Computed properties for IsCurrentlyActive and Status

- [x] Task 2: Create Offer Management View
  - [x] Create OffersView.xaml with modern dark theme
  - [x] Create OffersViewModel with filtering and stats
  - [x] Display offers grid with status badges
  - [x] Filter by active/expired/upcoming/inactive

- [x] Task 3: Create Offer Editor Dialog
  - [x] Create OfferEditorDialog.xaml with product selector
  - [x] Create OfferEditorViewModel with validation
  - [x] Product search and selection
  - [x] Date pickers for start/end dates
  - [x] Fixed price or percentage discount modes

- [x] Task 4: Implement Offer Validation
  - [x] End date must be after start date
  - [x] Offer price validation for both pricing modes
  - [x] Overlapping offers warning (best offer applied automatically)

## Dev Notes

### ProductOffer Entity

```csharp
public class ProductOffer
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string OfferName { get; set; } = string.Empty;
    public decimal OfferPrice { get; set; }
    public decimal? DiscountPercent { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public int MinQuantity { get; set; } = 1;
    public int? MaxQuantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedByUserId { get; set; }

    // Navigation
    public Product Product { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;

    // Computed
    public bool IsCurrentlyActive => IsActive
        && DateTime.Now >= StartDate
        && DateTime.Now <= EndDate;
}
```

### Offer Editor UI

```
+------------------------------------------+
|           CREATE PRODUCT OFFER           |
+------------------------------------------+
| Product:     [Search Product...      ▼]  |
| Offer Name:  [Summer Sale            ]   |
|                                          |
| Pricing Type: ( ) Fixed Price            |
|               (•) Discount Percent       |
|                                          |
| Original:    KSh 500.00                  |
| Discount:    [15    ] %                  |
| Offer Price: KSh 425.00                  |
|                                          |
| Start Date:  [2025-01-01] [📅]          |
| End Date:    [2025-01-31] [📅]          |
|                                          |
| Min Qty:     [1  ]  Max Qty: [   ]      |
|                                          |
|        [Cancel]        [Save Offer]      |
+------------------------------------------+
```

## Dev Agent Record

### Agent Model Used
Claude Opus 4.5 (claude-opus-4-5-20251101)

### Completion Notes List
- Enhanced ProductOffer entity with OfferPricingType enum (FixedPrice, PercentageDiscount)
- Added OfferStatus enum (Upcoming, Active, Expired, Inactive) for status tracking
- Created ProductOfferConfiguration with indexes for efficient active offer lookups
- Created IOfferService interface with comprehensive offer management methods
- Created OfferService implementation with validation and overlapping offer detection
- Created OffersViewModel with filtering, stats, and CRUD operations
- Created OffersView.xaml with modern dark theme matching app design
- Created OfferEditorViewModel with product search and validation
- Created OfferEditorDialog.xaml with fixed price/percentage modes
- Created DatePickerDialog for offer extension functionality
- Updated IDialogService and DialogService with offer dialog methods
- Registered IOfferService, OffersViewModel, OfferEditorViewModel in App.xaml.cs

### File List
- src/HospitalityPOS.Core/Entities/ProductOffer.cs (updated)
- src/HospitalityPOS.Core/Interfaces/IOfferService.cs
- src/HospitalityPOS.Infrastructure/Data/Configurations/ProductOfferConfiguration.cs
- src/HospitalityPOS.Infrastructure/Services/OfferService.cs
- src/HospitalityPOS.WPF/ViewModels/OffersViewModel.cs
- src/HospitalityPOS.WPF/ViewModels/OfferEditorViewModel.cs
- src/HospitalityPOS.WPF/Views/OffersView.xaml
- src/HospitalityPOS.WPF/Views/OffersView.xaml.cs
- src/HospitalityPOS.WPF/Views/OfferEditorDialog.xaml
- src/HospitalityPOS.WPF/Views/OfferEditorDialog.xaml.cs
- src/HospitalityPOS.WPF/Views/Dialogs/DatePickerDialog.xaml
- src/HospitalityPOS.WPF/Views/Dialogs/DatePickerDialog.xaml.cs
- src/HospitalityPOS.WPF/Services/IDialogService.cs (updated)
- src/HospitalityPOS.WPF/Services/DialogService.cs (updated)
- src/HospitalityPOS.WPF/App.xaml.cs (updated)
