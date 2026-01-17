# Story 14.3: Offer Price Display

Status: done

## Story

As a cashier,
I want to see both original and offer prices,
So that I can explain savings to customers.

## Acceptance Criteria

1. **Given** a product with active offer
   **When** viewing in product grid
   **Then** tile shows "OFFER" badge and both prices

2. **Given** a product on order with offer
   **When** viewing order line
   **Then** shows original price (strikethrough), offer price, savings

3. **Given** receipt is printed
   **When** offer product is included
   **Then** receipt shows both prices and savings

## Tasks / Subtasks

- [x] Task 1: Update Product Tile for Offers
  - [x] Add "OFFER" badge overlay
  - [x] Show original price with strikethrough
  - [x] Show offer price highlighted
  - [x] Display savings percentage

- [x] Task 2: Update Order Item Display
  - [x] Show both prices on order line
  - [x] Calculate and display savings
  - [x] Highlight offer items differently

- [x] Task 3: Update Receipt Printing
  - [x] Add OfferItemLine method to EscPosPrintDocument
  - [x] Show "Was: X  Now: Y  (Save Z%)" format
  - [x] Add TotalSavingsLine for total savings at bottom

## Dev Notes

### Product Tile with Offer

```
+------------------+
|    [OFFER]       | <- Badge (red)
|    [IMAGE]       |
|------------------|
| Tusker Lager     |
| ~~KSh 400~~ <- strikethrough (gray)
| KSh 350     <- offer price (green)
| -13%        <- savings (red)
+------------------+
```

### Order Item with Offer Display

```
| Tusker Lager          x2    KSh 700.00 |
|   Was: KSh 400  Now: KSh 350 (Promo)   |
| KSh 350 x 2 = KSh 700.00 (Save KSh 100)|
```

### Receipt Format

```
  Tusker Lager            x2
    Was: 400.00  Now: 350.00
    Promo - Save: 100.00
    Item Total:              KSh 700.00
```

## Dev Agent Record

### Agent Model Used
Claude claude-opus-4-5-20251101

### Completion Notes List
- Updated ProductTileViewModel with offer properties (HasActiveOffer, OfferPrice, OfferName, OfferId, DisplayPrice, SavingsPercent, SavingsAmount)
- Updated RefreshProductGridAsync to load active offers and populate offer data in product tiles
- Updated POSView.xaml product tile template with:
  - "OFFER" badge overlay on product image (red badge, right-aligned)
  - Strikethrough original price in gray
  - Highlighted offer price in green
  - Savings percentage in red
- Updated POSView.xaml order item template with:
  - "Was: X Now: Y (OfferName)" line when offer is applied
  - "(Save KSh X)" amount shown on price calculation line
- Added "You Save" total savings row to order summary section
- Added OfferItemLine method to EscPosPrintDocument for offer item receipt printing
- Added TotalSavingsLine method to EscPosPrintDocument for total savings display

### File List
- src/HospitalityPOS.WPF/ViewModels/POSViewModel.cs (ProductTileViewModel, RefreshProductGridAsync)
- src/HospitalityPOS.WPF/Views/POSView.xaml (product tile template, order item template, order summary)
- src/HospitalityPOS.Core/Printing/EscPosPrintDocument.cs (OfferItemLine, TotalSavingsLine methods)
