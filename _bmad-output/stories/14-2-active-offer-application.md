# Story 14.2: Active Offer Application

Status: done

## Story

As a cashier,
I want offers to be automatically applied at checkout,
So that customers get the correct promotional price.

## Acceptance Criteria

1. **Given** a product has an active offer
   **When** product is added to order
   **Then** offer price is applied automatically

2. **Given** multiple offers might apply
   **When** product is scanned
   **Then** only the best offer applies (no stacking)

3. **Given** offer has quantity requirements
   **When** minimum quantity is met
   **Then** offer applies only to qualifying quantity

## Tasks / Subtasks

- [x] Task 1: Create Product Offer Service
  - [x] Create IOfferService interface (existing from 14-1)
  - [x] Implement GetBestOfferForProductAsync(productId, quantity)
  - [x] Implement GetEffectivePriceAsync logic
  - [x] Cache active offers for performance via EF Core tracking

- [x] Task 2: Integrate Offer into Order Item Creation
  - [x] Check for active offer when adding product
  - [x] Apply offer price to OrderItem
  - [x] Store original price for display
  - [x] Track offer ID on order item

- [x] Task 3: Handle Quantity-Based Offers
  - [x] Check minimum quantity requirements
  - [x] Apply offer to qualifying quantity only
  - [x] Recalculate when quantity changes

- [x] Task 4: Implement Best Offer Selection
  - [x] If multiple offers exist, select lowest price
  - [x] Prevent offer stacking
  - [x] Log offer application for audit

## Dev Notes

### Product Offer Service

```csharp
public interface IOfferService
{
    Task<ProductOffer?> GetBestOfferForProductAsync(int productId, int quantity = 1);
    Task<decimal?> GetEffectivePriceAsync(int productId, int quantity = 1);
    // ... additional methods
}
```

### Order Item with Offer

```csharp
public class OrderItem
{
    // ... existing properties

    public decimal? OriginalUnitPrice { get; set; }
    public int? AppliedOfferId { get; set; }
    public string? AppliedOfferName { get; set; }
    public decimal SavingsAmount => OriginalUnitPrice.HasValue
        ? (OriginalUnitPrice.Value - UnitPrice) * Quantity
        : 0;
    public bool HasOfferApplied => AppliedOfferId.HasValue;
    public virtual ProductOffer? AppliedOffer { get; set; }
}
```

### Offer Application Flow

```
[Product Scanned/Added]
         |
[Check for Active Offer]
         |
    +----+----+
    |         |
[Has Offer]  [No Offer]
    |              |
[Apply Offer  [Use Regular
 Price]        Price]
    |
[Store Original + Offer Price]
    |
[Add to Order]
```

## Dev Agent Record

### Agent Model Used
Claude claude-opus-4-5-20251101

### Completion Notes List
- Updated OrderItem entity with offer tracking properties (OriginalUnitPrice, AppliedOfferId, AppliedOfferName, SavingsAmount, HasOfferApplied)
- Updated OrderItemConfiguration in EF Core to include offer relationship and computed property ignores
- Updated Order entity with computed offer savings properties (TotalOfferSavings, HasOffersApplied, OfferItemsCount)
- Updated OrderConfiguration to ignore computed properties
- Updated POSViewModel to integrate IOfferService for automatic offer application
- Added offer tracking to OrderItemViewModel (OriginalPrice, AppliedOfferId, AppliedOfferName, OfferSavings, HasOfferApplied)
- Updated AddToOrderAsync to automatically check and apply best offer when products are added
- Added RecalculateOfferForItemAsync method for quantity-based offer recalculation
- Updated IncreaseQuantityAsync to recalculate offers when quantity increases
- Updated DecreaseQuantityAsync to recalculate offers when quantity decreases
- Updated order item creation in PlaceOrderAsync to persist offer tracking data
- Updated LoadOrderIntoUI to restore offer data when recalling held orders
- Added OrderSavings and HasOfferSavings properties to POSViewModel
- Updated RecalculateOrderTotals to include offer savings calculation

### File List
- src/HospitalityPOS.Core/Entities/OrderItem.cs
- src/HospitalityPOS.Core/Entities/Order.cs
- src/HospitalityPOS.Infrastructure/Data/Configurations/OrderConfiguration.cs
- src/HospitalityPOS.WPF/ViewModels/POSViewModel.cs
