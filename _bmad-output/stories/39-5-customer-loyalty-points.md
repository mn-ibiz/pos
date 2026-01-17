# Story 39.5: Customer Loyalty Points System

Status: Done

## Story

As a **retail business owner**,
I want **a loyalty points system where customers earn and redeem points at checkout**,
so that **I can increase customer retention, encourage repeat purchases, and compete with major supermarkets**.

## Business Context

**CRITICAL - MARKET EXPECTATION**

Research shows:
- **68% of Kenyans** belong to at least one loyalty program
- Major supermarkets (Naivas, Carrefour, Quickmart) all have loyalty programs
- Customers actively seek out stores with rewards

Without a loyalty program:
- Cannot compete for loyal customers
- No customer retention mechanism
- No customer purchase data for marketing
- Losing to competitors with loyalty

**Business Value:** Loyalty programs increase repeat visits by 20-30% and average basket size by 15%.

## Acceptance Criteria

### AC1: Customer Enrollment
- [ ] Quick enrollment at POS using phone number (primary identifier)
- [ ] Optional: capture name, email, date of birth
- [ ] Phone number validated (Kenyan format)
- [ ] Prevent duplicate enrollment (phone must be unique)
- [ ] Confirmation SMS sent (optional, if SMS gateway configured)

### AC2: Points Earning
- [ ] Points earned automatically on every purchase
- [ ] Configurable earning rate (default: 1 point per KSh 100)
- [ ] Points calculated on net amount (after discounts, before payment)
- [ ] Points transaction recorded with receipt reference
- [ ] Tier-based earning multipliers (e.g., Gold members earn 2x)

### AC3: Points Display at Checkout
- [ ] Customer lookup by phone number
- [ ] Display current points balance before payment
- [ ] Display tier status (Bronze/Silver/Gold)
- [ ] Display points to be earned on this purchase

### AC4: Points Redemption
- [ ] Cashier can apply points as payment
- [ ] Configurable redemption rate (default: 100 points = KSh 10)
- [ ] Minimum redemption threshold (default: 100 points)
- [ ] Partial redemption supported
- [ ] Points deducted from balance
- [ ] Redemption recorded as payment method

### AC5: Membership Tiers
- [ ] Bronze: 0-999 lifetime points (1x earning)
- [ ] Silver: 1,000-4,999 lifetime points (1.5x earning)
- [ ] Gold: 5,000+ lifetime points (2x earning)
- [ ] Automatic tier upgrade on points milestone
- [ ] Tier determines earning multiplier

### AC6: Customer Purchase History
- [ ] View all purchases by customer
- [ ] Total lifetime spend
- [ ] Points earned/redeemed history
- [ ] Last visit date
- [ ] Visit frequency

### AC7: Points Expiry (Optional)
- [ ] Configurable expiry period (default: 12 months from earning)
- [ ] Background job to expire old points
- [ ] Notification before points expire (if SMS configured)

### AC8: Loyalty Reports
- [ ] Total members enrolled
- [ ] Active members (purchased in last 90 days)
- [ ] Points issued vs redeemed
- [ ] Top customers by spend
- [ ] Tier distribution

## Tasks / Subtasks

- [ ] **Task 1: Database Schema for Loyalty** (AC: 1, 2, 4, 5)
  - [ ] 1.1 Create Customers table (PhoneNumber, FirstName, LastName, Email, TierId, TotalPointsEarned, TotalPointsRedeemed, CurrentPointsBalance, LifetimeSpend)
  - [ ] 1.2 Create LoyaltyTiers table (Name, MinimumPoints, EarningMultiplier)
  - [ ] 1.3 Create LoyaltySettings table (PointsPerCurrencyUnit, CurrencyPerPoint, MinimumRedemption, PointsExpiryMonths)
  - [ ] 1.4 Create LoyaltyTransactions table (CustomerId, ReceiptId, TransactionType, Points, ExpiresAt)
  - [ ] 1.5 Insert default tiers: Bronze, Silver, Gold
  - [ ] 1.6 Insert default settings
  - [ ] 1.7 Create migration

- [ ] **Task 2: Loyalty Service** (AC: 1, 2, 4, 5, 6)
  - [ ] 2.1 Create ILoyaltyService interface
  - [ ] 2.2 Implement EnrollCustomer(phone, name?, email?)
  - [ ] 2.3 Implement GetCustomerByPhone(phone)
  - [ ] 2.4 Implement EarnPoints(customerId, amount, receiptId)
  - [ ] 2.5 Implement RedeemPoints(customerId, points, receiptId)
  - [ ] 2.6 Implement GetPointsBalance(customerId)
  - [ ] 2.7 Implement CalculateTier(customerId) with auto-upgrade
  - [ ] 2.8 Implement GetPurchaseHistory(customerId)
  - [ ] 2.9 Unit tests for all methods

- [ ] **Task 3: Customer Enrollment UI** (AC: 1)
  - [ ] 3.1 Create CustomerEnrollmentDialog.xaml
  - [ ] 3.2 Phone number input with validation
  - [ ] 3.3 Optional name/email fields
  - [ ] 3.4 Duplicate phone check with user feedback
  - [ ] 3.5 Success confirmation message

- [ ] **Task 4: POS Loyalty Integration** (AC: 2, 3, 4)
  - [ ] 4.1 Add "Customer" section to POSView
  - [ ] 4.2 Phone number lookup field
  - [ ] 4.3 Display customer name, tier, points balance
  - [ ] 4.4 Display "Points to earn" on this order
  - [ ] 4.5 Add "Redeem Points" button in payment screen
  - [ ] 4.6 Points redemption dialog with amount input
  - [ ] 4.7 Create "Points" payment method
  - [ ] 4.8 Integration tests for earn/redeem flow

- [ ] **Task 5: Receipt Settlement Integration** (AC: 2, 4)
  - [ ] 5.1 Modify ReceiptService.SettleReceiptAsync
  - [ ] 5.2 If customer attached: calculate and earn points
  - [ ] 5.3 Apply tier multiplier to points earned
  - [ ] 5.4 Update customer stats (LifetimeSpend, LastVisit, VisitCount)
  - [ ] 5.5 Check for tier upgrade
  - [ ] 5.6 If points redeemed: deduct from balance

- [ ] **Task 6: Customer Management UI** (AC: 6)
  - [ ] 6.1 Create CustomerListView.xaml in BackOffice
  - [ ] 6.2 Search by phone or name
  - [ ] 6.3 Customer detail view with purchase history
  - [ ] 6.4 Points statement view
  - [ ] 6.5 Manual points adjustment (admin only)

- [ ] **Task 7: Loyalty Settings UI** (AC: 2, 4, 5, 7)
  - [ ] 7.1 Create LoyaltySettingsView.xaml
  - [ ] 7.2 Configure earning rate
  - [ ] 7.3 Configure redemption rate
  - [ ] 7.4 Configure tier thresholds and multipliers
  - [ ] 7.5 Configure points expiry period

- [ ] **Task 8: Points Expiry Job** (AC: 7)
  - [ ] 8.1 Create ExpirePointsJob background service
  - [ ] 8.2 Query points transactions where ExpiresAt < Today AND not already expired
  - [ ] 8.3 Create "Expire" transaction to deduct
  - [ ] 8.4 Update customer balance
  - [ ] 8.5 Run monthly

- [ ] **Task 9: Loyalty Reports** (AC: 8)
  - [ ] 9.1 Add loyalty metrics to ReportingService
  - [ ] 9.2 Create LoyaltyDashboardView.xaml
  - [ ] 9.3 Member count by tier
  - [ ] 9.4 Points issued vs redeemed chart
  - [ ] 9.5 Top 10 customers by spend
  - [ ] 9.6 Export customer list to Excel

## Dev Notes

### Loyalty Flow at POS

```
[Cashier starts sale]
    ↓
[Cashier enters customer phone: 0712345678]
    ↓
[System looks up customer]
    ↓
[If found]
    → Display: "John Doe | Gold | 2,450 pts"
    → Calculate: "This order earns 45 pts (2x)"
    ↓
[Cashier completes order]
    ↓
[Payment screen shows]
    → Total: KSh 4,500
    → Points available: 2,450 (worth KSh 245)
    → [Use Points] button
    ↓
[Customer wants to redeem 1,000 pts]
    ↓
[Apply 1,000 pts = KSh 100 discount]
    → Remaining: KSh 4,400
    → Pay balance with Cash/M-Pesa
    ↓
[On settlement]
    → Deduct 1,000 pts
    → Earn 44 pts (on KSh 4,400 at 2x Gold rate)
    → Update balance: 2,450 - 1,000 + 44 = 1,494 pts
```

### Database Schema (from Gap Analysis)

```sql
CREATE TABLE Customers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PhoneNumber NVARCHAR(20) NOT NULL UNIQUE,
    FirstName NVARCHAR(100),
    LastName NVARCHAR(100),
    Email NVARCHAR(100),
    DateOfBirth DATE,
    Gender NVARCHAR(10),
    Address NVARCHAR(500),
    TierId INT FOREIGN KEY REFERENCES LoyaltyTiers(Id),
    TotalPointsEarned INT DEFAULT 0,
    TotalPointsRedeemed INT DEFAULT 0,
    CurrentPointsBalance INT DEFAULT 0,
    LifetimeSpend DECIMAL(18,2) DEFAULT 0,
    LastVisitDate DATETIME2,
    VisitCount INT DEFAULT 0,
    EnrolledAt DATETIME2 DEFAULT GETUTCDATE(),
    IsActive BIT DEFAULT 1
);

CREATE TABLE LoyaltyTiers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL, -- Bronze, Silver, Gold
    MinimumPoints INT NOT NULL, -- Threshold to reach tier
    EarningMultiplier DECIMAL(5,2) DEFAULT 1.0,
    Description NVARCHAR(200),
    DisplayOrder INT DEFAULT 0,
    IsActive BIT DEFAULT 1
);

CREATE TABLE LoyaltySettings (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PointsPerCurrencyUnit DECIMAL(10,4) DEFAULT 0.01, -- 1 point per 100 KES
    CurrencyPerPoint DECIMAL(10,4) DEFAULT 0.10, -- 10 KES per 100 points
    MinimumRedemptionPoints INT DEFAULT 100,
    PointsExpiryMonths INT DEFAULT 12,
    IsActive BIT DEFAULT 1
);

CREATE TABLE LoyaltyTransactions (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT FOREIGN KEY REFERENCES Customers(Id),
    ReceiptId INT FOREIGN KEY REFERENCES Receipts(Id),
    TransactionType NVARCHAR(20) NOT NULL, -- Earn, Redeem, Expire, Adjust
    Points INT NOT NULL, -- Positive for earn, negative for redeem
    BalanceBefore INT NOT NULL,
    BalanceAfter INT NOT NULL,
    Description NVARCHAR(200),
    ExpiresAt DATETIME2,
    ProcessedByUserId INT FOREIGN KEY REFERENCES Users(Id),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- Default Tiers
INSERT INTO LoyaltyTiers (Name, MinimumPoints, EarningMultiplier, DisplayOrder) VALUES
('Bronze', 0, 1.0, 1),
('Silver', 1000, 1.5, 2),
('Gold', 5000, 2.0, 3);
```

### Points Calculation

```csharp
public int CalculatePointsToEarn(decimal netAmount, Customer customer)
{
    var settings = GetLoyaltySettings();
    var basePoints = (int)(netAmount * settings.PointsPerCurrencyUnit);

    var tier = GetTier(customer.TierId);
    var multipliedPoints = (int)(basePoints * tier.EarningMultiplier);

    return multipliedPoints;
}

public decimal CalculateRedemptionValue(int points)
{
    var settings = GetLoyaltySettings();
    return points * settings.CurrencyPerPoint;
}
```

### Architecture Compliance

- **Layer:** Core (Entities), Business (LoyaltyService), WPF (UI)
- **Pattern:** Service pattern with repository
- **Payment Integration:** Points as payment method type
- **RBAC:** Add permissions for loyalty management

### References

- [Source: _bmad-output/feature-gap-analysis-2026-01-16.md#2.5-Customer-Loyalty-Program]
- [Source: _bmad-output/architecture.md#Payment-Processing]
- [Source: _bmad-output/PRD.md] - Customer & Loyalty requirements

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

### File List
