# Story 22.3: Central Promotion Deployment

## Story
**As an** HQ administrator,
**I want to** create and deploy promotions to stores,
**So that** campaigns run consistently across the chain.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Epic
**Epic 22: Multi-Store HQ Management**

## Acceptance Criteria

### AC1: Promotion Deployment Selection
**Given** promotion created at HQ
**When** deploying
**Then** can select: all stores, specific region, or individual stores

### AC2: Promotion Sync
**Given** promotion deployed
**When** sync runs
**Then** promotion is active at selected stores

### AC3: Redemption Monitoring
**Given** promotion running
**When** monitoring
**Then** HQ dashboard shows redemption counts by store

## Technical Notes
```csharp
public class CentralPromotion
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public PromotionType Type { get; set; }  // Discount, BOGO, Bundle
    public decimal DiscountAmount { get; set; }
    public decimal? DiscountPercent { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<Guid> ProductIds { get; set; }
    public List<Guid> CategoryIds { get; set; }
}

public class PromotionDeployment
{
    public Guid Id { get; set; }
    public Guid PromotionId { get; set; }
    public DeploymentScope Scope { get; set; }  // AllStores, Zone, Individual
    public List<Guid> TargetIds { get; set; }  // StoreIds or ZoneIds
    public DateTime DeployedAt { get; set; }
    public DeploymentStatus Status { get; set; }
}

public class PromotionRedemption
{
    public Guid Id { get; set; }
    public Guid PromotionId { get; set; }
    public Guid StoreId { get; set; }
    public Guid ReceiptId { get; set; }
    public decimal DiscountGiven { get; set; }
    public DateTime RedeemedAt { get; set; }
}
```

## Definition of Done
- [x] Promotion deployment UI implemented
- [x] Store selection (all/zone/individual) working
- [x] Real-time redemption tracking
- [x] HQ dashboard with store breakdown
- [x] Unit tests passing

## Implementation Summary

### Entities Created
- `CentralPromotion.cs` - Central promotion entity with PromotionType and PromotionStatus enums
- `PromotionProduct.cs` - Links promotions to products
- `PromotionCategory.cs` - Links promotions to categories
- `PromotionDeployment.cs` - Tracks promotion deployments with scope (AllStores, ByZone, IndividualStores)
- `DeploymentZone.cs` - Links deployments to pricing zones
- `DeploymentStore.cs` - Tracks individual store deployment status
- `PromotionRedemption.cs` - Tracks promotion redemptions by store

### DTOs Created
- `CentralPromotionDto` - Promotion display/listing
- `CreatePromotionDto` - Promotion creation/update
- `DeployPromotionDto` - Deployment request
- `PromotionDeploymentDto` - Deployment status/details
- `DeploymentStoreDto` - Per-store deployment status
- `PromotionRedemptionDto` - Redemption records
- `RecordRedemptionDto` - Redemption recording
- `StoreRedemptionSummaryDto` - Redemption summary by store
- `PromotionDashboardDto` - Dashboard analytics
- `DeploymentResult` - Deployment operation result
- `PromotionQueryDto` - Promotion filtering
- `StoreActivePromotionDto` - Active promotions for a store

### Service Implementation
- `ICentralPromotionService` - Interface with ~25 methods
- `CentralPromotionService` - Full implementation with:
  - Promotion CRUD operations
  - Product/category associations
  - Deployment management (AllStores, ByZone, IndividualStores)
  - Redemption tracking with voiding
  - Dashboard and reporting
  - Store active promotion queries

### EF Core Configurations
- `CentralPromotionConfiguration` - Indexes for status, date range, coupon code
- `PromotionProductConfiguration` - Composite unique index
- `PromotionCategoryConfiguration` - Composite unique index
- `PromotionDeploymentConfiguration` - Status and date indexes
- `DeploymentZoneConfiguration` - Deployment-zone relationship
- `DeploymentStoreConfiguration` - Status filtering index
- `PromotionRedemptionConfiguration` - Store/date composite indexes

### Unit Tests
- `CentralPromotionServiceTests.cs` - 50+ tests covering:
  - Constructor null checks
  - Promotion CRUD operations
  - Product/category associations
  - Deployment operations (all scopes)
  - Redemption recording and voiding
  - Dashboard and reporting
  - Store active promotion queries

### Key Features
1. **Deployment Scopes**: AllStores, ByZone (PricingZone), IndividualStores
2. **Promotion Types**: PercentageDiscount, AmountDiscount, BOGO, Bundle, FixedPrice
3. **Promotion Status**: Draft, Active, Scheduled, Paused, Ended, Cancelled
4. **Deployment Status**: Pending, InProgress, Completed, PartiallyCompleted, Failed, Cancelled, RolledBack
5. **Redemption Tracking**: By promotion, by store, with date filtering
6. **Dashboard**: Total redemptions, discount given, store breakdown, average per transaction
