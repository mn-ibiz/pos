## Overview

Enhance the WorkPeriod (Shift/Day Opening) system to capture detailed cash drawer denomination breakdowns when opening and closing shifts. This allows for precise cash reconciliation by counting individual coin and note types rather than just total amounts.

## Background Research

### Why Denomination Tracking?
- **Accountability**: Precise tracking of cash by denomination reduces counting errors
- **Float Management**: Ensures adequate change is available at shift start
- **Discrepancy Detection**: Makes it easier to identify counting mistakes vs theft
- **Bank Deposit Preparation**: Denomination counts help prepare accurate bank deposits
- **Audit Trail**: Detailed records for financial audits and loss prevention

### Industry Best Practices
1. **Start-of-Shift Count**: Cashier counts and declares all coins and bills by denomination
2. **End-of-Shift Count**: Full denomination breakdown before reconciliation
3. **Variance by Denomination**: Track which denominations are over/short
4. **Manager Verification**: Option for manager to verify counts
5. **Historical Tracking**: Maintain denomination history for trend analysis

## Current State Analysis

### Existing WorkPeriod Entity
Located: `HospitalityPOS.Core/Entities/WorkPeriod.cs`

**Current Fields:**
- `OpeningFloat` (decimal) - Single total amount ✓
- `ClosingCash` (decimal?) - Single total amount ✓
- `ExpectedCash` (decimal?) - Calculated expected ✓
- `Variance` (decimal?) - Total variance ✓

**What's Missing:**
- Opening denomination breakdown by coin/note type
- Closing denomination breakdown
- Variance analysis by denomination
- Float composition requirements

### Existing WorkPeriodService
Located: `HospitalityPOS.Infrastructure/Services/WorkPeriodService.cs`

- `OpenWorkPeriodAsync` - Takes single `openingFloat` decimal
- `CloseWorkPeriodAsync` - Takes single `actualClosingCash` decimal

## Requirements

### 1. Kenya-Specific Denominations

```
KENYAN SHILLING DENOMINATIONS
=============================

NOTES:
- KES 1,000 (One Thousand)
- KES 500 (Five Hundred)
- KES 200 (Two Hundred)
- KES 100 (One Hundred)
- KES 50 (Fifty)

COINS:
- KES 40 (Forty - commemorative, rare)
- KES 20 (Twenty)
- KES 10 (Ten)
- KES 5 (Five)
- KES 1 (One)
- KES 0.50 (Fifty Cents)

CONFIGURABLE:
- System should allow configuration for different currencies
- Admin can enable/disable denominations
- Support for future denomination changes
```

### 2. Cash Count Data Structure

```csharp
public class CashDenominationCount : BaseEntity
{
    public int WorkPeriodId { get; set; }
    public CashCountType CountType { get; set; } // Opening, Closing
    public int CountedByUserId { get; set; }
    public DateTime CountedAt { get; set; }
    public int? VerifiedByUserId { get; set; }
    public DateTime? VerifiedAt { get; set; }

    // Notes
    public int Notes1000 { get; set; }
    public int Notes500 { get; set; }
    public int Notes200 { get; set; }
    public int Notes100 { get; set; }
    public int Notes50 { get; set; }

    // Coins
    public int Coins20 { get; set; }
    public int Coins10 { get; set; }
    public int Coins5 { get; set; }
    public int Coins1 { get; set; }
    public int CoinsCents50 { get; set; }

    // Calculated totals
    public decimal TotalNotes { get; set; }
    public decimal TotalCoins { get; set; }
    public decimal GrandTotal { get; set; }

    // Navigation
    public virtual WorkPeriod WorkPeriod { get; set; }
    public virtual User CountedByUser { get; set; }
    public virtual User VerifiedByUser { get; set; }
}

public enum CashCountType
{
    Opening,
    Closing,
    MidShift,
    Verification
}
```

### 3. Alternative: Flexible Denomination Configuration

```csharp
public class CashDenomination : BaseEntity
{
    public string CurrencyCode { get; set; } // KES, USD, etc.
    public DenominationType Type { get; set; } // Note, Coin
    public decimal Value { get; set; } // 1000, 500, 0.50, etc.
    public string DisplayName { get; set; } // "KES 1,000", "50 Cents"
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public class CashCountLine : BaseEntity
{
    public int CashCountId { get; set; }
    public int DenominationId { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; } // Quantity x Denomination.Value

    public virtual CashCount CashCount { get; set; }
    public virtual CashDenomination Denomination { get; set; }
}

public class CashCount : BaseEntity
{
    public int WorkPeriodId { get; set; }
    public CashCountType CountType { get; set; }
    public int CountedByUserId { get; set; }
    public DateTime CountedAt { get; set; }
    public decimal TotalAmount { get; set; }
    public string Notes { get; set; }

    public virtual ICollection<CashCountLine> Lines { get; set; }
}
```

### 4. Core Features

#### 4.1 Opening Cash Count
- [ ] Display denomination entry form when opening shift
- [ ] Auto-calculate totals as quantities entered
- [ ] Validate minimum float requirements (configurable)
- [ ] Alert if insufficient change (e.g., low coins)
- [ ] Option to skip denomination entry (use total only)
- [ ] Manager can set recommended float composition

#### 4.2 Closing Cash Count
- [ ] Full denomination breakdown entry
- [ ] Compare to expected cash by total
- [ ] Show variance breakdown
- [ ] Require explanation for significant variances
- [ ] Manager verification option
- [ ] Support re-count if discrepancy found

#### 4.3 Mid-Shift Count (Cash Drop)
- [ ] Record cash drops with denomination
- [ ] Track what was removed vs kept for float
- [ ] Update expected cash calculation

#### 4.4 Float Management
- [ ] Define standard float composition (e.g., 10x KES 100, 20x KES 50, etc.)
- [ ] Alert when opening count doesn't match recommended
- [ ] Suggest float replenishment

#### 4.5 Reporting
- [ ] Denomination summary on Z-Report
- [ ] Float composition history
- [ ] Variance trends by denomination
- [ ] Cashier accuracy tracking

### 5. UI Components

#### 5.1 Cash Count Entry Dialog
```
┌─────────────────────────────────────────────────────────┐
│  Opening Cash Count - Shift #123                        │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  NOTES                          Qty      Value          │
│  ─────────────────────────────────────────────────      │
│  KES 1,000                    [  5  ]    5,000.00       │
│  KES 500                      [ 10  ]    5,000.00       │
│  KES 200                      [  5  ]    1,000.00       │
│  KES 100                      [ 20  ]    2,000.00       │
│  KES 50                       [ 30  ]    1,500.00       │
│  ─────────────────────────────────────────────────      │
│  Notes Subtotal:                        14,500.00       │
│                                                         │
│  COINS                          Qty      Value          │
│  ─────────────────────────────────────────────────      │
│  KES 20                       [ 25  ]      500.00       │
│  KES 10                       [ 30  ]      300.00       │
│  KES 5                        [ 40  ]      200.00       │
│  KES 1                        [ 50  ]       50.00       │
│  ─────────────────────────────────────────────────      │
│  Coins Subtotal:                         1,050.00       │
│                                                         │
│  ═══════════════════════════════════════════════════    │
│  GRAND TOTAL:                           15,550.00       │
│  ═══════════════════════════════════════════════════    │
│                                                         │
│  [ ] Verify count matches drawer                        │
│                                                         │
│  [  Cancel  ]                        [  Confirm  ]      │
└─────────────────────────────────────────────────────────┘
```

#### 5.2 Closing Count with Variance
```
┌─────────────────────────────────────────────────────────┐
│  Closing Cash Count - Shift #123                        │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Expected Cash: KES 45,750.00                           │
│                                                         │
│  DENOMINATION           Qty      Value                  │
│  ───────────────────────────────────────────────────    │
│  KES 1,000           [ 35  ]   35,000.00                │
│  KES 500             [ 12  ]    6,000.00                │
│  KES 200             [  8  ]    1,600.00                │
│  KES 100             [ 25  ]    2,500.00                │
│  KES 50              [ 10  ]      500.00                │
│  KES 20              [ 10  ]      200.00                │
│  KES 10              [  5  ]       50.00                │
│  ───────────────────────────────────────────────────    │
│  COUNTED TOTAL:                 45,850.00               │
│                                                         │
│  ═══════════════════════════════════════════════════    │
│  VARIANCE:                         +100.00  (OVER)      │
│  ═══════════════════════════════════════════════════    │
│                                                         │
│  Variance Explanation:                                  │
│  [                                                   ]  │
│                                                         │
│  [  Re-count  ]    [  Cancel  ]    [  Submit  ]        │
└─────────────────────────────────────────────────────────┘
```

### 6. Service Interface Enhancement

```csharp
public interface IWorkPeriodService
{
    // Existing methods...

    // New denomination-aware methods
    Task<WorkPeriod> OpenWorkPeriodWithDenominationsAsync(
        CashDenominationCountDto openingCount,
        string notes = null,
        CancellationToken ct = default);

    Task<WorkPeriod> CloseWorkPeriodWithDenominationsAsync(
        int workPeriodId,
        CashDenominationCountDto closingCount,
        string varianceExplanation = null,
        CancellationToken ct = default);

    Task<CashDenominationCount> RecordMidShiftCountAsync(
        int workPeriodId,
        CashDenominationCountDto count,
        CancellationToken ct = default);

    Task<CashDenominationCount> GetDenominationCountAsync(
        int workPeriodId,
        CashCountType countType,
        CancellationToken ct = default);

    Task<FloatRecommendation> GetRecommendedFloatAsync(
        int storeId,
        CancellationToken ct = default);
}

public class CashDenominationCountDto
{
    public Dictionary<decimal, int> Denominations { get; set; }
    // Key: denomination value (1000, 500, 0.50, etc.)
    // Value: quantity counted
}

public class FloatRecommendation
{
    public decimal TotalAmount { get; set; }
    public Dictionary<decimal, int> RecommendedDenominations { get; set; }
    public string Notes { get; set; }
}
```

### 7. Business Rules

1. **Opening Count**
   - Denomination count is optional but encouraged
   - If skipped, only total amount recorded (current behavior)
   - Cannot open shift without minimum float (configurable)

2. **Closing Count**
   - Denomination count required if opening was by denomination
   - Variance explanation required if exceeds threshold
   - Re-count option available before final submission

3. **Verification**
   - Manager can verify any count
   - Verification timestamp and user recorded
   - Discrepancy between counts flagged

4. **Float Standards**
   - Admin can configure recommended float composition
   - Alerts when opening count deviates significantly
   - Track float adequacy over time

## Acceptance Criteria

- [ ] Cashier can enter opening cash count by denomination
- [ ] System auto-calculates totals from denomination quantities
- [ ] Closing cash count captures full denomination breakdown
- [ ] Variance displayed clearly with over/short indication
- [ ] Denomination counts stored and retrievable
- [ ] Z-Report includes denomination summary
- [ ] Mid-shift cash drops can be recorded with denomination
- [ ] Float recommendations can be configured
- [ ] Works in both WPF admin and POS cashier interface
- [ ] Backward compatible (can still use total-only mode)

## Implementation Notes

### Existing Code to Modify
- `WorkPeriod` entity - Add navigation to CashDenominationCount
- `WorkPeriodService` - Add denomination-aware methods
- `OpenWorkPeriodDialog` - Add denomination entry UI
- `CloseWorkPeriodDialog` - Add denomination entry UI

### New Components
- `CashDenomination` entity (if flexible approach)
- `CashDenominationCount` entity
- `CashCountLine` entity (if flexible approach)
- `DenominationCountControl` - Reusable WPF control
- `ICashDenominationService` - Denomination configuration

## References

- [Shopify - Balancing a Cash Drawer](https://www.shopify.com/retail/balancing-a-cash-drawer)
- [Star Micronics - How to Balance Cash Drawers](https://starmicronics.com/blog/how-to-balance-cash-drawers-quickly-and-accurately)
- [Oracle Xstore - Till Management](https://docs.oracle.com/en/industries/retail/retail-xstore-point-of-service/25.0/rpxmg/till-management.htm)

---

**Priority**: High
**Estimated Complexity**: Medium
**Labels**: feature, finance, cash-management
