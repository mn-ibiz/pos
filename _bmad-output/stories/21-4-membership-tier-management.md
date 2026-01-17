# Story 21.4: Membership Tier Management

## Story
**As the** system,
**I want to** manage customer tiers based on spending,
**So that** loyal customers get better benefits.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Epic
**Epic 21: Advanced Loyalty Program**

## Acceptance Criteria

### AC1: Tier Upgrade
**Given** tier thresholds configured (Bronze, Silver, Gold, Platinum)
**When** customer spending reaches threshold
**Then** customer is automatically upgraded and notified

### AC2: Tier Benefits
**Given** tier benefits defined
**When** member transacts
**Then** tier-specific discounts or earning rates apply automatically

### AC3: Tier Evaluation
**Given** evaluation period ends (e.g., annually)
**When** reviewing tiers
**Then** customers may be downgraded if spending dropped below threshold

## Technical Notes
```csharp
public class MembershipTier
{
    public int Id { get; set; }
    public string Name { get; set; }  // Bronze, Silver, Gold, Platinum
    public decimal SpendThreshold { get; set; }
    public decimal PointsMultiplier { get; set; }  // 1.0, 1.5, 2.0, 3.0
    public decimal DiscountPercent { get; set; }
}
```

## Definition of Done
- [x] Tier configuration implemented
- [x] Auto-upgrade on threshold
- [x] Benefits applied at checkout
- [x] Annual evaluation process
- [x] Unit tests passing

## Implementation Notes

### Files Created/Modified

1. **TierConfiguration Entity** (`src/HospitalityPOS.Core/Entities/TierConfiguration.cs`)
   - New entity for storing tier configuration
   - Properties: Tier, Name, Description, SpendThreshold, PointsThreshold, PointsMultiplier, DiscountPercent, FreeDelivery, PriorityService

2. **EF Configuration** (`src/HospitalityPOS.Infrastructure/Data/Configurations/LoyaltyConfiguration.cs`)
   - Added TierConfigurationConfiguration with proper indexes

3. **POSDbContext** (`src/HospitalityPOS.Infrastructure/Data/POSDbContext.cs`)
   - Added TierConfigurations DbSet

4. **DTOs** (`src/HospitalityPOS.Core/DTOs/LoyaltyDtos.cs`)
   - Added TierConfigurationDto and TierEvaluationResult

5. **ILoyaltyService** (`src/HospitalityPOS.Core/Interfaces/ILoyaltyService.cs`)
   - Added tier management methods

6. **LoyaltyService** (`src/HospitalityPOS.Infrastructure/Services/LoyaltyService.cs`)
   - Implemented all tier management methods

7. **DatabaseSeeder** (`src/HospitalityPOS.Infrastructure/Data/DatabaseSeeder.cs`)
   - Added SeedTierConfigurationsAsync with default tiers

8. **Unit Tests** (`tests/HospitalityPOS.Business.Tests/Services/LoyaltyServiceTests.cs`)
   - Added comprehensive tier management tests

### Default Tier Configuration
- **Bronze**: 0 KES threshold, 1.0x multiplier, 0% discount
- **Silver**: 25,000 KES threshold, 1.25x multiplier, 5% discount
- **Gold**: 75,000 KES threshold, 1.5x multiplier, 10% discount
- **Platinum**: 150,000 KES threshold, 2.0x multiplier, 15% discount
