# Story 22.2: Central Pricing Management

## Story
**As an** HQ administrator,
**I want to** set prices centrally and manage regional pricing,
**So that** pricing is controlled and consistent.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Epic
**Epic 22: Multi-Store HQ Management**

## Acceptance Criteria

### AC1: Central Price Setting
**Given** product exists
**When** setting central price
**Then** price applies to all stores by default

### AC2: Regional Pricing
**Given** regional pricing needed
**When** defining zones
**Then** can set different prices per zone/region

### AC3: Scheduled Price Changes
**Given** scheduled price change
**When** setting effective date
**Then** prices automatically update on that date across stores

## Technical Notes
```csharp
public class PricingZone
{
    public Guid Id { get; set; }
    public string Name { get; set; }  // e.g., "Nairobi", "Mombasa", "Upcountry"
    public List<Guid> StoreIds { get; set; }
}

public class ZonePrice
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid ZoneId { get; set; }
    public decimal Price { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public class ScheduledPriceChange
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid? ZoneId { get; set; }  // null = all zones
    public decimal NewPrice { get; set; }
    public DateTime EffectiveDate { get; set; }
    public PriceChangeStatus Status { get; set; }  // Scheduled, Applied, Cancelled
}
```

## Definition of Done
- [x] Central price management implemented
- [x] Zone/regional pricing working
- [x] Scheduled price changes functional
- [x] Price change audit trail
- [x] Unit tests passing

## Implementation Notes

### Entities Created
- **PricingZone**: Regional pricing zone groupings for stores
  - Location: `src/HospitalityPOS.Core/Entities/PricingZone.cs`
  - Features: Zone code, currency code, default tax rate, store assignments

- **ZonePrice**: Zone-specific product pricing with effective dates
  - Location: `src/HospitalityPOS.Core/Entities/ZonePrice.cs`
  - Features: Zone pricing, effective dates, minimum prices, cost prices

- **ScheduledPriceChange**: Future scheduled price changes
  - Location: `src/HospitalityPOS.Core/Entities/ScheduledPriceChange.cs`
  - Features: Status tracking (Scheduled/Applied/Cancelled/Failed/Expired), auto-apply, expiry dates

### EF Configurations
- **PricingConfiguration.cs**: Configurations for PricingZone, ZonePrice, ScheduledPriceChange
  - Proper indexes for zone code uniqueness, effective dates
  - Cascade delete for zone prices, set null for scheduled changes

### Service Methods Added
- **Pricing Zone Management**:
  - `GetAllPricingZonesAsync`, `GetPricingZoneByIdAsync`, `GetDefaultPricingZoneAsync`
  - `CreatePricingZoneAsync`, `UpdatePricingZoneAsync`, `AssignStoresToZoneAsync`

- **Zone Pricing**:
  - `GetProductZonePricesAsync`, `GetZoneProductPricesAsync`
  - `SetZonePriceAsync`, `RemoveZonePriceAsync`, `GetZoneEffectivePriceAsync`
  - `ApplyBulkZonePriceAsync` for bulk adjustments

- **Scheduled Price Changes**:
  - `GetPendingPriceChangesAsync`, `GetProductScheduledChangesAsync`
  - `CreateScheduledPriceChangeAsync`, `CancelScheduledPriceChangeAsync`
  - `ApplyDuePriceChangesAsync` for automatic price change application
  - `GetProductPricingSummaryAsync` for comprehensive pricing view

### DTOs Added
- `PricingZoneDto`, `CreatePricingZoneDto`
- `ZonePriceDto`, `CreateZonePriceDto`, `BulkZonePriceDto`
- `ScheduledPriceChangeDto`, `CreateScheduledPriceChangeDto`
- `PriceChangeApplicationResult`, `ProductPricingSummaryDto`

### Unit Tests
- 35+ additional tests for pricing functionality in `MultiStoreServiceTests.cs`
- Coverage: Pricing zones, zone pricing, scheduled price changes, edge cases

### Price Hierarchy
1. Store-level override (highest priority)
2. Zone-level pricing
3. Central product price (default)
