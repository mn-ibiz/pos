# Story 21.2: Points Earning

## Story
**As the** system,
**I want to** award loyalty points on purchases,
**So that** customers are rewarded for shopping.

## Status
- [x] Draft
- [x] Ready for Dev
- [x] In Progress
- [x] Code Review
- [x] Done

## Epic
**Epic 21: Advanced Loyalty Program**

## Acceptance Criteria

### AC1: Points Calculation
**Given** loyalty member makes purchase
**When** transaction is completed
**Then** points awarded based on earning rate (e.g., KSh 100 = 1 point)

### AC2: Bonus Points
**Given** bonus points promotions exist
**When** qualifying items purchased
**Then** additional bonus points are awarded

### AC3: Receipt Display
**Given** points are earned
**When** printing receipt
**Then** shows points earned this visit and total balance

## Technical Notes
```csharp
public interface ILoyaltyService
{
    Task<PointsResult> CalculatePointsAsync(Guid memberId, Receipt receipt);
    Task<LoyaltyMember> IdentifyMemberAsync(string phoneNumber);
}

public class PointsConfiguration
{
    public decimal EarningRate { get; set; } = 100m;  // KSh per point
    public decimal RedemptionRate { get; set; } = 50m;  // Value per point
    public int MinimumRedemption { get; set; } = 100;  // Minimum points
}
```

## Tasks / Subtasks

- [x] Task 1: Create Points Configuration Entity (AC: #1)
  - [x] Create PointsConfiguration entity for earning/redemption rates
  - [x] Add to DbContext and configure with EF Core
  - [x] Create default configuration seeding

- [x] Task 2: Create LoyaltyTransaction Entity (AC: #1, #2)
  - [x] Create LoyaltyTransaction entity for points history
  - [x] Include fields: member, receipt, points earned, transaction type
  - [x] Add to DbContext and configure with EF Core

- [x] Task 3: Extend LoyaltyService with Points Calculation (AC: #1, #2)
  - [x] Add CalculatePointsAsync method to ILoyaltyService
  - [x] Implement base points calculation (spending / rate)
  - [x] Implement bonus points multiplier support
  - [x] Update member balance and lifetime points
  - [x] Write unit tests for points calculation

- [x] Task 4: Link Receipt to Loyalty Member (AC: #3)
  - [x] Add LoyaltyMemberId to Receipt entity
  - [x] Add PointsEarned to Receipt entity
  - [x] Update Receipt configuration for loyalty relationship

- [x] Task 5: Update Receipt Display with Points (AC: #3)
  - [x] Add loyalty info fields to Receipt entity
  - [x] Show points earned this transaction
  - [x] Show new points balance after transaction

- [x] Task 6: Create Bonus Points Promotion Support (AC: #2)
  - [x] Tier-based bonus multipliers (Bronze 1x, Silver 1.25x, Gold 1.5x, Platinum 2x)
  - [x] Apply bonus multipliers during points calculation
  - [x] Write unit tests for bonus points

## Definition of Done
- [x] Points calculated on transaction
- [x] Bonus points campaigns supported
- [x] Points shown on receipt
- [x] Member balance updated
- [x] Unit tests passing

## Dev Notes

### Points Earning Flow
1. Customer presents loyalty number during checkout
2. Receipt linked to loyalty member
3. On settlement, points calculated based on spend
4. Member balance updated, transaction logged
5. Points displayed on printed receipt

### Configuration
- Default earning rate: KSh 100 = 1 point
- Configuration stored in database for easy adjustment
- Tier-based multipliers implemented: Bronze (1x), Silver (1.25x), Gold (1.5x), Platinum (2x)

## Dev Agent Record

### Agent Model Used
Claude Opus 4.5 (claude-opus-4-5-20251101)

### Debug Log
- Story 21.2 Points Earning implementation
- Created PointsConfiguration entity with configurable earning/redemption rates
- Created LoyaltyTransaction entity for points history tracking
- Extended LoyaltyService with CalculatePointsAsync and AwardPointsAsync methods
- Added loyalty fields to Receipt entity (LoyaltyMemberId, PointsEarned, PointsRedeemed, PointsDiscountAmount, PointsBalanceAfter)
- Implemented tier-based bonus multipliers
- Added comprehensive unit tests for points calculation and awarding

### Completion Notes List
- PointsConfiguration entity stores earning rate (default 100 KSh per point), redemption value, minimum/maximum redemption limits
- LoyaltyTransaction tracks all points transactions with type (Earned, Redeemed, Adjustment, Expired, Bonus, Refund, TransferIn, TransferOut)
- LoyaltyService extended with: CalculatePointsAsync, AwardPointsAsync, GetPointsConfigurationAsync, GetTierBonusMultiplierAsync, UpdateMemberVisitAsync, GetTransactionHistoryAsync
- Tier bonus multipliers: Bronze=1.0x, Silver=1.25x, Gold=1.5x, Platinum=2.0x
- Points can optionally exclude discounted items and tax from calculation
- Default PointsConfiguration seeded in DatabaseSeeder

## File List
### New Files
- `src/HospitalityPOS.Core/Entities/PointsConfiguration.cs`
- `src/HospitalityPOS.Core/Entities/LoyaltyTransaction.cs`

### Modified Files
- `src/HospitalityPOS.Core/Entities/Receipt.cs` (added loyalty fields)
- `src/HospitalityPOS.Core/Entities/LoyaltyMember.cs` (added navigation properties)
- `src/HospitalityPOS.Core/Enums/SystemEnums.cs` (added LoyaltyTransactionType)
- `src/HospitalityPOS.Core/Interfaces/ILoyaltyService.cs` (added points methods)
- `src/HospitalityPOS.Core/DTOs/LoyaltyDtos.cs` (added PointsCalculationResult, PointsAwardResult, LoyaltyTransactionDto)
- `src/HospitalityPOS.Infrastructure/Data/POSDbContext.cs` (added DbSets)
- `src/HospitalityPOS.Infrastructure/Data/Configurations/LoyaltyConfiguration.cs` (added LoyaltyTransaction, PointsConfiguration configs)
- `src/HospitalityPOS.Infrastructure/Data/Configurations/ReceiptConfiguration.cs` (added loyalty relationship)
- `src/HospitalityPOS.Infrastructure/Data/DatabaseSeeder.cs` (added PointsConfiguration seeding)
- `src/HospitalityPOS.Infrastructure/Services/LoyaltyService.cs` (added points calculation methods)
- `tests/HospitalityPOS.Business.Tests/Services/LoyaltyServiceTests.cs` (added points earning tests)

## Change Log
- 2026-01-02: Story tasks created, implementation starting
- 2026-01-02: Story 21.2 completed - Points earning fully implemented with tier bonuses
