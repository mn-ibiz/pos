# Implementation Plan: Theme System & Loyalty Program

**Document Version:** 1.0
**Created:** 2026-01-24
**Project:** HospitalityPOS

---

## Table of Contents

1. [Phase 1: Theme System - Light Mode Default](#phase-1-theme-system---light-mode-default)
2. [Phase 2: Loyalty POS Integration](#phase-2-loyalty-pos-integration)
3. [Phase 3: SMS OTP Verification for Redemption](#phase-3-sms-otp-verification-for-redemption)
4. [Phase 4: Loyalty Configuration UI](#phase-4-loyalty-configuration-ui)
5. [Phase 5: Sidebar Menu Integration](#phase-5-sidebar-menu-integration)
6. [Database Schema Reference](#database-schema-reference)
7. [API Reference](#api-reference)
8. [Best Practices Reference](#best-practices-reference)

---

## Phase 1: Theme System - Light Mode Default

### Overview
Switch the application default theme from dark mode to light mode while preserving the ability to switch to dark mode.

### Current State
- Default theme: **Dark Mode**
- Theme files exist: `DarkTheme.xaml`, `LightTheme.xaml`
- Theme service implemented: `ThemeService.cs`
- Theme toggle exists in Organization Settings
- User preference persisted to `theme-settings.json`

### Target State
- Default theme: **Light Mode**
- All fallbacks default to Light Mode
- Existing theme toggle continues to work
- User preferences still persist

### Implementation Tasks

#### Task 1.1: Update App.xaml Default Theme
**File:** `src/HospitalityPOS.WPF/App.xaml`

**Current Code (Line 8):**
```xaml
<ResourceDictionary Source="Resources/Themes/DarkTheme.xaml" />
```

**New Code:**
```xaml
<ResourceDictionary Source="Resources/Themes/LightTheme.xaml" />
```

**Purpose:** Sets the initial theme loaded when the application starts before any user preferences are applied.

---

#### Task 1.2: Update ThemeService Default Initialization
**File:** `src/HospitalityPOS.WPF/Services/ThemeService.cs`

**Current Code (Lines 24-25):**
```csharp
private ThemeMode _currentTheme = ThemeMode.Dark;
private ThemeMode _savedThemePreference = ThemeMode.Dark;
```

**New Code:**
```csharp
private ThemeMode _currentTheme = ThemeMode.Light;
private ThemeMode _savedThemePreference = ThemeMode.Light;
```

**Purpose:** Sets the default theme mode used when no user preference exists.

---

#### Task 1.3: Update ThemeService Fallback on Load Failure
**File:** `src/HospitalityPOS.WPF/Services/ThemeService.cs`

**Current Code (Line ~91 in LoadSavedThemeAsync):**
```csharp
SetTheme(ThemeMode.Dark);
```

**New Code:**
```csharp
SetTheme(ThemeMode.Light);
```

**Purpose:** When the theme settings file cannot be read or doesn't exist, default to Light mode.

---

#### Task 1.4: Update ThemeService System Theme Detection Fallback
**File:** `src/HospitalityPOS.WPF/Services/ThemeService.cs`

**Current Code (Line ~172 in GetSystemTheme):**
```csharp
return ThemeMode.Dark;
```

**New Code:**
```csharp
return ThemeMode.Light;
```

**Purpose:** When system theme detection fails (non-Windows or registry read error), default to Light mode.

---

### Testing Checklist
- [ ] Fresh install shows Light mode
- [ ] Existing users with Dark preference keep Dark mode
- [ ] Theme toggle in Organization Settings works
- [ ] System theme option works correctly
- [ ] Theme persists after app restart

---

## Phase 2: Loyalty POS Integration

### Overview
Integrate the loyalty program into the Point of Sale workflow to enable:
1. Customer identification by phone number after transaction
2. Automatic points earning on transaction completion
3. Points redemption during payment

### Current State
- `POSViewModel` has loyalty properties (stub/placeholder)
- `SettlementViewModel` has no loyalty integration
- `LoyaltyService` is fully implemented with all business logic
- No phone prompt after transaction
- No points earning automation
- No redemption flow in payment

### Target State
- After transaction completion, prompt for customer phone number
- Automatically award points based on transaction amount
- Allow points redemption during settlement
- Display loyalty information throughout the flow

---

### Task 2.1: Add Loyalty Member Lookup to POSViewModel

**File:** `src/HospitalityPOS.WPF/ViewModels/POSViewModel.cs`

#### 2.1.1 Add Required Dependencies

Add to constructor injection:
```csharp
private readonly ILoyaltyService _loyaltyService;

public POSViewModel(
    // ... existing parameters ...
    ILoyaltyService loyaltyService)
{
    _loyaltyService = loyaltyService;
    // ... existing initialization ...
}
```

#### 2.1.2 Add Loyalty Search Command

```csharp
[RelayCommand]
private async Task SearchLoyaltyMemberAsync()
{
    if (string.IsNullOrWhiteSpace(LoyaltySearchPhone))
        return;

    try
    {
        IsSearchingLoyalty = true;
        LoyaltySearchError = null;

        // Normalize and validate phone
        if (!_loyaltyService.ValidatePhoneNumber(LoyaltySearchPhone))
        {
            LoyaltySearchError = "Invalid phone number format";
            return;
        }

        var normalizedPhone = _loyaltyService.NormalizePhoneNumber(LoyaltySearchPhone);
        var member = await _loyaltyService.GetByPhoneAsync(normalizedPhone);

        if (member != null)
        {
            AttachedLoyaltyMember = member;
            await CalculateEstimatedPointsAsync();
        }
        else
        {
            // Offer to enroll
            ShowEnrollmentPrompt = true;
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error searching for loyalty member");
        LoyaltySearchError = "Error searching for member";
    }
    finally
    {
        IsSearchingLoyalty = false;
    }
}
```

#### 2.1.3 Add Quick Enrollment Command

```csharp
[RelayCommand]
private async Task QuickEnrollCustomerAsync()
{
    if (string.IsNullOrWhiteSpace(LoyaltySearchPhone))
        return;

    try
    {
        IsEnrollingCustomer = true;

        var enrollmentDto = new EnrollCustomerDto
        {
            PhoneNumber = LoyaltySearchPhone,
            Name = EnrollmentName // Optional, from UI
        };

        var result = await _loyaltyService.EnrollCustomerAsync(enrollmentDto);

        if (result.Success)
        {
            AttachedLoyaltyMember = result.Member;
            await CalculateEstimatedPointsAsync();
            ShowEnrollmentPrompt = false;

            // Show success notification
            _notificationService.ShowSuccess($"Customer enrolled! Membership: {result.Member.MembershipNumber}");
        }
        else
        {
            LoyaltySearchError = result.ErrorMessage;
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error enrolling customer");
        LoyaltySearchError = "Error enrolling customer";
    }
    finally
    {
        IsEnrollingCustomer = false;
    }
}
```

#### 2.1.4 Add Estimated Points Calculation

```csharp
private async Task CalculateEstimatedPointsAsync()
{
    if (AttachedLoyaltyMember == null || CurrentOrder == null)
    {
        EstimatedPointsToEarn = 0;
        return;
    }

    try
    {
        var result = await _loyaltyService.CalculatePointsAsync(
            AttachedLoyaltyMember.Id,
            CurrentOrder.Total,
            CurrentOrder.DiscountAmount,
            CurrentOrder.TaxAmount);

        EstimatedPointsToEarn = result.TotalPoints;
        EstimatedBonusPoints = result.BonusPoints;
        PointsCalculationDescription = result.Description;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error calculating estimated points");
        EstimatedPointsToEarn = 0;
    }
}
```

#### 2.1.5 Add Redemption Preview

```csharp
[RelayCommand]
private async Task CalculateRedemptionPreviewAsync()
{
    if (AttachedLoyaltyMember == null || CurrentOrder == null)
        return;

    try
    {
        var preview = await _loyaltyService.CalculateRedemptionAsync(
            AttachedLoyaltyMember.Id,
            CurrentOrder.Total);

        MaxRedeemablePoints = preview.MaxRedeemablePoints;
        MaxRedemptionValue = preview.MaxValue;
        AvailablePoints = preview.AvailablePoints;
        MinimumRedemptionPoints = preview.MinimumPoints;

        // Suggest optimal redemption
        SuggestedRedemptionPoints = preview.SuggestedPoints;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error calculating redemption preview");
    }
}
```

#### 2.1.6 Add Observable Properties

```csharp
[ObservableProperty]
private bool _isSearchingLoyalty;

[ObservableProperty]
private bool _isEnrollingCustomer;

[ObservableProperty]
private string? _loyaltySearchError;

[ObservableProperty]
private bool _showEnrollmentPrompt;

[ObservableProperty]
private string? _enrollmentName;

[ObservableProperty]
private decimal _estimatedBonusPoints;

[ObservableProperty]
private string? _pointsCalculationDescription;

[ObservableProperty]
private decimal _availablePoints;

[ObservableProperty]
private decimal _minumumRedemptionPoints;

[ObservableProperty]
private decimal _suggestedRedemptionPoints;

[ObservableProperty]
private decimal _maxRedemptionValue;
```

---

### Task 2.2: Add Post-Transaction Phone Prompt

**File:** `src/HospitalityPOS.WPF/ViewModels/POSViewModel.cs`

#### 2.2.1 Add Phone Prompt Dialog State

```csharp
[ObservableProperty]
private bool _showPhonePromptDialog;

[ObservableProperty]
private string? _phonePromptInput;

[ObservableProperty]
private bool _isProcessingPhonePrompt;

[ObservableProperty]
private string? _phonePromptError;
```

#### 2.2.2 Show Prompt After Transaction

In the method that completes a transaction (after payment success):

```csharp
private async Task OnTransactionCompletedAsync(Receipt receipt)
{
    // If no loyalty member was attached during the transaction
    if (AttachedLoyaltyMember == null)
    {
        // Show phone prompt dialog
        ShowPhonePromptDialog = true;
        PhonePromptInput = string.Empty;
        PhonePromptError = null;

        // Store receipt for later points award
        _pendingReceiptForLoyalty = receipt;
    }
    else
    {
        // Award points immediately
        await AwardPointsForReceiptAsync(receipt);
    }
}
```

#### 2.2.3 Handle Phone Prompt Submission

```csharp
[RelayCommand]
private async Task SubmitPhonePromptAsync()
{
    if (string.IsNullOrWhiteSpace(PhonePromptInput))
    {
        // Customer declined to provide phone - skip loyalty
        ShowPhonePromptDialog = false;
        await FinalizeTransactionAsync();
        return;
    }

    try
    {
        IsProcessingPhonePrompt = true;
        PhonePromptError = null;

        // Validate phone format
        if (!_loyaltyService.ValidatePhoneNumber(PhonePromptInput))
        {
            PhonePromptError = "Invalid phone number. Use format: 0712345678";
            return;
        }

        var normalizedPhone = _loyaltyService.NormalizePhoneNumber(PhonePromptInput);
        var member = await _loyaltyService.GetByPhoneAsync(normalizedPhone);

        if (member == null)
        {
            // Auto-enroll new customer
            var enrollResult = await _loyaltyService.EnrollCustomerAsync(new EnrollCustomerDto
            {
                PhoneNumber = PhonePromptInput
            });

            if (enrollResult.Success)
            {
                member = enrollResult.Member;
                _notificationService.ShowSuccess($"New member enrolled: {member.MembershipNumber}");
            }
            else
            {
                PhonePromptError = enrollResult.ErrorMessage;
                return;
            }
        }

        // Award points for the pending receipt
        if (_pendingReceiptForLoyalty != null)
        {
            await AwardPointsForReceiptAsync(_pendingReceiptForLoyalty, member.Id);
        }

        ShowPhonePromptDialog = false;
        await FinalizeTransactionAsync();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing phone prompt");
        PhonePromptError = "An error occurred. Please try again.";
    }
    finally
    {
        IsProcessingPhonePrompt = false;
    }
}

[RelayCommand]
private async Task SkipPhonePromptAsync()
{
    ShowPhonePromptDialog = false;
    _pendingReceiptForLoyalty = null;
    await FinalizeTransactionAsync();
}
```

#### 2.2.4 Award Points Method

```csharp
private async Task AwardPointsForReceiptAsync(Receipt receipt, int? memberId = null)
{
    var loyaltyMemberId = memberId ?? AttachedLoyaltyMember?.Id;

    if (loyaltyMemberId == null)
        return;

    try
    {
        var result = await _loyaltyService.AwardPointsAsync(
            loyaltyMemberId.Value,
            receipt.Total,
            receipt.DiscountAmount,
            receipt.TaxAmount,
            receipt.Id,
            receipt.ReceiptNumber);

        if (result.Success)
        {
            // Show points earned notification
            var message = $"Earned {result.PointsEarned} points";
            if (result.BonusPoints > 0)
                message += $" (+{result.BonusPoints} bonus)";
            message += $". Balance: {result.NewBalance} points";

            _notificationService.ShowSuccess(message);

            // Update receipt with loyalty info for printing
            receipt.LoyaltyPointsEarned = result.PointsEarned + result.BonusPoints;
            receipt.LoyaltyNewBalance = result.NewBalance;
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error awarding loyalty points");
        // Don't fail the transaction - log and continue
    }
}
```

---

### Task 2.3: Add Phone Prompt UI (XAML)

**File:** `src/HospitalityPOS.WPF/Views/POSView.xaml`

Add dialog overlay for phone prompt:

```xaml
<!-- Phone Prompt Dialog Overlay -->
<Grid Visibility="{Binding ShowPhonePromptDialog, Converter={StaticResource BoolToVisibilityConverter}}"
      Background="#80000000"
      Panel.ZIndex="100">
    <Border Background="{DynamicResource ThemeSurfaceBrush}"
            CornerRadius="12"
            Padding="32"
            MaxWidth="400"
            MaxHeight="350"
            VerticalAlignment="Center"
            HorizontalAlignment="Center"
            Effect="{StaticResource CardShadow}">
        <StackPanel>
            <!-- Header -->
            <TextBlock Text="Earn Loyalty Points"
                       FontSize="24"
                       FontWeight="SemiBold"
                       Foreground="{DynamicResource ThemeTextPrimaryBrush}"
                       HorizontalAlignment="Center"
                       Margin="0,0,0,8"/>

            <TextBlock Text="Enter customer phone number to earn points"
                       FontSize="14"
                       Foreground="{DynamicResource ThemeTextSecondaryBrush}"
                       HorizontalAlignment="Center"
                       Margin="0,0,0,24"/>

            <!-- Phone Input -->
            <TextBox Text="{Binding PhonePromptInput, UpdateSourceTrigger=PropertyChanged}"
                     FontSize="24"
                     Height="56"
                     Padding="16,12"
                     Margin="0,0,0,8"
                     MaxLength="13"
                     PlaceholderText="0712 345 678"
                     Style="{StaticResource LargeInputTextBox}"/>

            <!-- Error Message -->
            <TextBlock Text="{Binding PhonePromptError}"
                       Foreground="{DynamicResource DangerBrush}"
                       FontSize="12"
                       Visibility="{Binding PhonePromptError, Converter={StaticResource NullToVisibilityConverter}}"
                       Margin="0,0,0,16"/>

            <!-- Buttons -->
            <Grid Margin="0,16,0,0">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="16"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>

                <Button Grid.Column="0"
                        Content="Skip"
                        Command="{Binding SkipPhonePromptCommand}"
                        Style="{StaticResource SecondaryButton}"
                        Height="48"/>

                <Button Grid.Column="2"
                        Content="{Binding IsProcessingPhonePrompt, Converter={StaticResource BoolToTextConverter}, ConverterParameter='Processing...|Submit'}"
                        Command="{Binding SubmitPhonePromptCommand}"
                        IsEnabled="{Binding IsProcessingPhonePrompt, Converter={StaticResource InverseBoolConverter}}"
                        Style="{StaticResource PrimaryButton}"
                        Height="48"/>
            </Grid>
        </StackPanel>
    </Border>
</Grid>
```

---

### Task 2.4: Integrate Redemption into SettlementViewModel

**File:** `src/HospitalityPOS.WPF/ViewModels/SettlementViewModel.cs`

#### 2.4.1 Add Loyalty Dependencies and Properties

```csharp
private readonly ILoyaltyService _loyaltyService;

// Loyalty Properties
[ObservableProperty]
private LoyaltyMemberDto? _loyaltyMember;

[ObservableProperty]
private decimal _availableLoyaltyPoints;

[ObservableProperty]
private decimal _pointsToRedeem;

[ObservableProperty]
private decimal _pointsRedemptionValue;

[ObservableProperty]
private decimal _maxRedeemablePoints;

[ObservableProperty]
private decimal _minimumRedemptionPoints;

[ObservableProperty]
private bool _isRedemptionEnabled;

[ObservableProperty]
private bool _showRedemptionPanel;

[ObservableProperty]
private bool _isAwaitingOtpVerification;

[ObservableProperty]
private string? _redemptionOtpCode;

[ObservableProperty]
private string? _redemptionError;
```

#### 2.4.2 Initialize Loyalty on Settlement Load

```csharp
public async Task InitializeAsync(Order order, LoyaltyMemberDto? loyaltyMember = null)
{
    CurrentOrder = order;
    LoyaltyMember = loyaltyMember;

    if (LoyaltyMember != null)
    {
        await LoadRedemptionOptionsAsync();
    }

    CalculateTotals();
}

private async Task LoadRedemptionOptionsAsync()
{
    if (LoyaltyMember == null || CurrentOrder == null)
        return;

    try
    {
        var preview = await _loyaltyService.CalculateRedemptionAsync(
            LoyaltyMember.Id,
            CurrentOrder.Total);

        AvailableLoyaltyPoints = preview.AvailablePoints;
        MaxRedeemablePoints = preview.MaxRedeemablePoints;
        MinimumRedemptionPoints = preview.MinimumPoints;

        // Enable redemption if member has enough points
        IsRedemptionEnabled = preview.AvailablePoints >= preview.MinimumPoints;
        ShowRedemptionPanel = IsRedemptionEnabled;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error loading redemption options");
        IsRedemptionEnabled = false;
    }
}
```

#### 2.4.3 Add Redemption Amount Change Handler

```csharp
partial void OnPointsToRedeemChanged(decimal value)
{
    if (value <= 0)
    {
        PointsRedemptionValue = 0;
        return;
    }

    // Validate against constraints
    if (value < MinimumRedemptionPoints)
    {
        RedemptionError = $"Minimum {MinimumRedemptionPoints} points required";
        PointsRedemptionValue = 0;
        return;
    }

    if (value > MaxRedeemablePoints)
    {
        RedemptionError = $"Maximum {MaxRedeemablePoints} points allowed";
        PointsToRedeem = MaxRedeemablePoints;
        return;
    }

    if (value > AvailableLoyaltyPoints)
    {
        RedemptionError = "Insufficient points balance";
        PointsToRedeem = AvailableLoyaltyPoints;
        return;
    }

    RedemptionError = null;

    // Calculate value (async call simplified here)
    Task.Run(async () =>
    {
        PointsRedemptionValue = await _loyaltyService.ConvertPointsToValueAsync(value);
        CalculateTotals();
    });
}
```

#### 2.4.4 Update Totals Calculation

```csharp
private void CalculateTotals()
{
    if (CurrentOrder == null)
        return;

    Subtotal = CurrentOrder.Subtotal;
    TaxAmount = CurrentOrder.TaxAmount;
    DiscountAmount = CurrentOrder.DiscountAmount;

    // Apply loyalty redemption as discount
    LoyaltyDiscount = PointsRedemptionValue;

    GrandTotal = Subtotal + TaxAmount - DiscountAmount - LoyaltyDiscount;

    // Ensure grand total is not negative
    if (GrandTotal < 0)
        GrandTotal = 0;

    RemainingBalance = GrandTotal - TotalPaid;
}
```

---

### Task 2.5: Add Loyalty Section to Settlement UI

**File:** `src/HospitalityPOS.WPF/Views/SettlementView.xaml`

```xaml
<!-- Loyalty Redemption Section -->
<Border Background="{DynamicResource ThemeSurfaceBrush}"
        CornerRadius="8"
        Padding="16"
        Margin="0,0,0,16"
        Visibility="{Binding ShowRedemptionPanel, Converter={StaticResource BoolToVisibilityConverter}}">
    <StackPanel>
        <!-- Header -->
        <Grid Margin="0,0,0,12">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>

            <TextBlock Grid.Column="0"
                       Text="Loyalty Points"
                       FontSize="16"
                       FontWeight="SemiBold"
                       Foreground="{DynamicResource ThemeTextPrimaryBrush}"/>

            <TextBlock Grid.Column="2"
                       FontSize="14"
                       Foreground="{DynamicResource ThemeTextSecondaryBrush}">
                <Run Text="Available: "/>
                <Run Text="{Binding AvailableLoyaltyPoints, StringFormat=N0}"
                     FontWeight="SemiBold"
                     Foreground="{DynamicResource PrimaryBrush}"/>
                <Run Text=" pts"/>
            </TextBlock>
        </Grid>

        <!-- Member Info -->
        <Border Background="{DynamicResource ThemeBackgroundSecondaryBrush}"
                CornerRadius="4"
                Padding="12"
                Margin="0,0,0,12">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>

                <StackPanel Grid.Column="0">
                    <TextBlock Text="{Binding LoyaltyMember.DisplayName}"
                               FontWeight="Medium"
                               Foreground="{DynamicResource ThemeTextPrimaryBrush}"/>
                    <TextBlock Text="{Binding LoyaltyMember.FormattedPhone}"
                               FontSize="12"
                               Foreground="{DynamicResource ThemeTextSecondaryBrush}"/>
                </StackPanel>

                <Border Grid.Column="1"
                        Background="{DynamicResource PrimaryLightBrush}"
                        CornerRadius="4"
                        Padding="8,4">
                    <TextBlock Text="{Binding LoyaltyMember.Tier}"
                               FontSize="12"
                               FontWeight="Medium"
                               Foreground="{DynamicResource PrimaryDarkBrush}"/>
                </Border>
            </Grid>
        </Border>

        <!-- Redemption Input -->
        <Grid Margin="0,0,0,8">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="16"/>
                <ColumnDefinition Width="120"/>
            </Grid.ColumnDefinitions>

            <StackPanel Grid.Column="0">
                <TextBlock Text="Points to Redeem"
                           FontSize="12"
                           Foreground="{DynamicResource ThemeTextSecondaryBrush}"
                           Margin="0,0,0,4"/>
                <TextBox Text="{Binding PointsToRedeem, UpdateSourceTrigger=PropertyChanged}"
                         FontSize="18"
                         Height="44"
                         Padding="12,8"/>
            </StackPanel>

            <StackPanel Grid.Column="2">
                <TextBlock Text="Value"
                           FontSize="12"
                           Foreground="{DynamicResource ThemeTextSecondaryBrush}"
                           Margin="0,0,0,4"/>
                <Border Background="{DynamicResource ThemeBackgroundTertiaryBrush}"
                        CornerRadius="4"
                        Height="44"
                        Padding="12,8">
                    <TextBlock VerticalAlignment="Center"
                               FontSize="18"
                               FontWeight="SemiBold"
                               Foreground="{DynamicResource SuccessBrush}">
                        <Run Text="KES "/>
                        <Run Text="{Binding PointsRedemptionValue, StringFormat=N2}"/>
                    </TextBlock>
                </Border>
            </StackPanel>
        </Grid>

        <!-- Error Message -->
        <TextBlock Text="{Binding RedemptionError}"
                   Foreground="{DynamicResource DangerBrush}"
                   FontSize="12"
                   Visibility="{Binding RedemptionError, Converter={StaticResource NullToVisibilityConverter}}"
                   Margin="0,0,0,8"/>

        <!-- Quick Redemption Buttons -->
        <StackPanel Orientation="Horizontal"
                    Margin="0,8,0,0">
            <Button Content="Use 100 pts"
                    Command="{Binding SetRedemptionPointsCommand}"
                    CommandParameter="100"
                    Style="{StaticResource OutlineButton}"
                    Margin="0,0,8,0"
                    Padding="12,6"/>
            <Button Content="Use 500 pts"
                    Command="{Binding SetRedemptionPointsCommand}"
                    CommandParameter="500"
                    Style="{StaticResource OutlineButton}"
                    Margin="0,0,8,0"
                    Padding="12,6"/>
            <Button Content="Use Max"
                    Command="{Binding SetRedemptionPointsCommand}"
                    CommandParameter="{Binding MaxRedeemablePoints}"
                    Style="{StaticResource OutlineButton}"
                    Padding="12,6"/>
        </StackPanel>
    </StackPanel>
</Border>
```

---

## Phase 3: SMS OTP Verification for Redemption

### Overview
Implement SMS-based OTP verification to secure loyalty points redemption. Customers must verify their identity via a code sent to their registered phone number before redeeming points.

### Security Flow
1. Customer requests points redemption
2. System generates 6-digit OTP
3. OTP sent to customer's registered phone via SMS
4. Customer tells cashier the OTP code
5. Cashier enters OTP to verify
6. On successful verification, redemption proceeds
7. OTP expires after 5 minutes
8. Maximum 3 attempts before lockout

---

### Task 3.1: Create OTP Entity

**File:** `src/HospitalityPOS.Core/Entities/RedemptionOtp.cs`

```csharp
using System;
using HospitalityPOS.Core.Entities.Common;

namespace HospitalityPOS.Core.Entities
{
    public class RedemptionOtp : BaseEntity
    {
        /// <summary>
        /// Reference to the loyalty member requesting redemption
        /// </summary>
        public int LoyaltyMemberId { get; set; }

        /// <summary>
        /// The 6-digit OTP code
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Phone number the OTP was sent to
        /// </summary>
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// When the OTP expires
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Number of verification attempts made
        /// </summary>
        public int AttemptCount { get; set; }

        /// <summary>
        /// Maximum allowed attempts (default: 3)
        /// </summary>
        public int MaxAttempts { get; set; } = 3;

        /// <summary>
        /// Whether the OTP has been successfully verified
        /// </summary>
        public bool IsVerified { get; set; }

        /// <summary>
        /// When the OTP was verified (null if not verified)
        /// </summary>
        public DateTime? VerifiedAt { get; set; }

        /// <summary>
        /// Points amount this OTP authorizes for redemption
        /// </summary>
        public decimal AuthorizedPoints { get; set; }

        /// <summary>
        /// Receipt/transaction this OTP is for (null until used)
        /// </summary>
        public int? ReceiptId { get; set; }

        /// <summary>
        /// User who processed the verification
        /// </summary>
        public int? VerifiedByUserId { get; set; }

        // Navigation properties
        public virtual LoyaltyMember LoyaltyMember { get; set; } = null!;
        public virtual Receipt? Receipt { get; set; }
        public virtual User? VerifiedByUser { get; set; }

        // Computed properties
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
        public bool IsLocked => AttemptCount >= MaxAttempts;
        public bool CanVerify => !IsExpired && !IsLocked && !IsVerified;
        public int RemainingAttempts => Math.Max(0, MaxAttempts - AttemptCount);
    }
}
```

---

### Task 3.2: Create OTP Service Interface

**File:** `src/HospitalityPOS.Core/Interfaces/IOtpService.cs`

```csharp
using System.Threading;
using System.Threading.Tasks;
using HospitalityPOS.Core.DTOs;

namespace HospitalityPOS.Core.Interfaces
{
    public interface IOtpService
    {
        /// <summary>
        /// Generate and send OTP for loyalty points redemption
        /// </summary>
        /// <param name="loyaltyMemberId">Member requesting redemption</param>
        /// <param name="pointsToRedeem">Amount of points to authorize</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result with OTP ID for tracking</returns>
        Task<OtpGenerationResult> GenerateRedemptionOtpAsync(
            int loyaltyMemberId,
            decimal pointsToRedeem,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Verify an OTP code
        /// </summary>
        /// <param name="otpId">The OTP record ID</param>
        /// <param name="code">The code entered by cashier</param>
        /// <param name="userId">User performing verification</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Verification result</returns>
        Task<OtpVerificationResult> VerifyOtpAsync(
            int otpId,
            string code,
            int userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Resend OTP to customer (respects cooldown)
        /// </summary>
        Task<OtpGenerationResult> ResendOtpAsync(
            int otpId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if member has a pending (valid) OTP
        /// </summary>
        Task<RedemptionOtp?> GetPendingOtpAsync(
            int loyaltyMemberId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalidate all pending OTPs for a member
        /// </summary>
        Task InvalidatePendingOtpsAsync(
            int loyaltyMemberId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Mark OTP as used for a specific receipt
        /// </summary>
        Task MarkOtpUsedAsync(
            int otpId,
            int receiptId,
            CancellationToken cancellationToken = default);
    }
}
```

---

### Task 3.3: Create OTP DTOs

**File:** `src/HospitalityPOS.Core/DTOs/OtpDtos.cs`

```csharp
using System;

namespace HospitalityPOS.Core.DTOs
{
    public class OtpGenerationResult
    {
        public bool Success { get; set; }
        public int? OtpId { get; set; }
        public string? MaskedPhone { get; set; } // e.g., "07XX XXX X78"
        public DateTime? ExpiresAt { get; set; }
        public int? ExpiresInSeconds { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ErrorCode { get; set; }

        // Cooldown info for resend
        public bool CanResend { get; set; }
        public int? ResendCooldownSeconds { get; set; }

        public static OtpGenerationResult Succeeded(int otpId, string maskedPhone, DateTime expiresAt)
        {
            return new OtpGenerationResult
            {
                Success = true,
                OtpId = otpId,
                MaskedPhone = maskedPhone,
                ExpiresAt = expiresAt,
                ExpiresInSeconds = (int)(expiresAt - DateTime.UtcNow).TotalSeconds,
                CanResend = false,
                ResendCooldownSeconds = 60
            };
        }

        public static OtpGenerationResult Failed(string errorMessage, string? errorCode = null)
        {
            return new OtpGenerationResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                ErrorCode = errorCode
            };
        }
    }

    public class OtpVerificationResult
    {
        public bool Success { get; set; }
        public bool IsExpired { get; set; }
        public bool IsLocked { get; set; }
        public int RemainingAttempts { get; set; }
        public decimal? AuthorizedPoints { get; set; }
        public string? ErrorMessage { get; set; }

        public static OtpVerificationResult Verified(decimal authorizedPoints)
        {
            return new OtpVerificationResult
            {
                Success = true,
                AuthorizedPoints = authorizedPoints
            };
        }

        public static OtpVerificationResult Failed(string message, int remainingAttempts, bool isExpired = false, bool isLocked = false)
        {
            return new OtpVerificationResult
            {
                Success = false,
                ErrorMessage = message,
                RemainingAttempts = remainingAttempts,
                IsExpired = isExpired,
                IsLocked = isLocked
            };
        }
    }
}
```

---

### Task 3.4: Implement OTP Service

**File:** `src/HospitalityPOS.Infrastructure/Services/OtpService.cs`

```csharp
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.Infrastructure.Services
{
    public class OtpService : IOtpService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISmsService _smsService;
        private readonly ILoyaltyMemberRepository _memberRepository;
        private readonly ILogger<OtpService> _logger;

        private const int OTP_LENGTH = 6;
        private const int OTP_VALIDITY_MINUTES = 5;
        private const int MAX_ATTEMPTS = 3;
        private const int RESEND_COOLDOWN_SECONDS = 60;

        public OtpService(
            IUnitOfWork unitOfWork,
            ISmsService smsService,
            ILoyaltyMemberRepository memberRepository,
            ILogger<OtpService> logger)
        {
            _unitOfWork = unitOfWork;
            _smsService = smsService;
            _memberRepository = memberRepository;
            _logger = logger;
        }

        public async Task<OtpGenerationResult> GenerateRedemptionOtpAsync(
            int loyaltyMemberId,
            decimal pointsToRedeem,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Get member
                var member = await _memberRepository.GetByIdAsync(loyaltyMemberId, cancellationToken);
                if (member == null)
                {
                    return OtpGenerationResult.Failed("Member not found", "MEMBER_NOT_FOUND");
                }

                if (!member.IsActive)
                {
                    return OtpGenerationResult.Failed("Member account is inactive", "MEMBER_INACTIVE");
                }

                // Invalidate any existing pending OTPs
                await InvalidatePendingOtpsAsync(loyaltyMemberId, cancellationToken);

                // Generate OTP code
                var code = GenerateOtpCode();
                var expiresAt = DateTime.UtcNow.AddMinutes(OTP_VALIDITY_MINUTES);

                // Create OTP record
                var otp = new RedemptionOtp
                {
                    LoyaltyMemberId = loyaltyMemberId,
                    Code = code,
                    PhoneNumber = member.PhoneNumber,
                    ExpiresAt = expiresAt,
                    MaxAttempts = MAX_ATTEMPTS,
                    AuthorizedPoints = pointsToRedeem,
                    IsActive = true
                };

                await _unitOfWork.Repository<RedemptionOtp>().AddAsync(otp, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Send SMS (fire and forget with logging)
                _ = SendOtpSmsAsync(member.PhoneNumber, code, member.Name, pointsToRedeem);

                var maskedPhone = MaskPhoneNumber(member.PhoneNumber);

                _logger.LogInformation(
                    "OTP generated for member {MemberId}, OTP ID: {OtpId}, expires at {ExpiresAt}",
                    loyaltyMemberId, otp.Id, expiresAt);

                return OtpGenerationResult.Succeeded(otp.Id, maskedPhone, expiresAt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating OTP for member {MemberId}", loyaltyMemberId);
                return OtpGenerationResult.Failed("Failed to generate verification code", "GENERATION_ERROR");
            }
        }

        public async Task<OtpVerificationResult> VerifyOtpAsync(
            int otpId,
            string code,
            int userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var otp = await _unitOfWork.Repository<RedemptionOtp>()
                    .GetByIdAsync(otpId, cancellationToken);

                if (otp == null)
                {
                    return OtpVerificationResult.Failed("Invalid verification request", 0);
                }

                // Check if expired
                if (otp.IsExpired)
                {
                    _logger.LogWarning("OTP {OtpId} has expired", otpId);
                    return OtpVerificationResult.Failed(
                        "Verification code has expired. Please request a new code.",
                        0,
                        isExpired: true);
                }

                // Check if locked
                if (otp.IsLocked)
                {
                    _logger.LogWarning("OTP {OtpId} is locked due to too many attempts", otpId);
                    return OtpVerificationResult.Failed(
                        "Too many incorrect attempts. Please request a new code.",
                        0,
                        isLocked: true);
                }

                // Check if already verified
                if (otp.IsVerified)
                {
                    _logger.LogWarning("OTP {OtpId} has already been verified", otpId);
                    return OtpVerificationResult.Failed("This code has already been used", 0);
                }

                // Verify code
                otp.AttemptCount++;

                if (!string.Equals(otp.Code, code?.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    _logger.LogWarning(
                        "Invalid OTP attempt for {OtpId}, attempt {Attempt} of {Max}",
                        otpId, otp.AttemptCount, otp.MaxAttempts);

                    return OtpVerificationResult.Failed(
                        $"Incorrect code. {otp.RemainingAttempts} attempts remaining.",
                        otp.RemainingAttempts);
                }

                // Verification successful
                otp.IsVerified = true;
                otp.VerifiedAt = DateTime.UtcNow;
                otp.VerifiedByUserId = userId;

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "OTP {OtpId} verified successfully by user {UserId}",
                    otpId, userId);

                return OtpVerificationResult.Verified(otp.AuthorizedPoints);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying OTP {OtpId}", otpId);
                return OtpVerificationResult.Failed("Verification failed. Please try again.", 0);
            }
        }

        public async Task<OtpGenerationResult> ResendOtpAsync(
            int otpId,
            CancellationToken cancellationToken = default)
        {
            var otp = await _unitOfWork.Repository<RedemptionOtp>()
                .GetByIdAsync(otpId, cancellationToken);

            if (otp == null)
            {
                return OtpGenerationResult.Failed("Invalid request", "INVALID_OTP");
            }

            // Check cooldown (created within last 60 seconds)
            var secondsSinceCreation = (DateTime.UtcNow - otp.CreatedAt).TotalSeconds;
            if (secondsSinceCreation < RESEND_COOLDOWN_SECONDS)
            {
                var remainingCooldown = RESEND_COOLDOWN_SECONDS - (int)secondsSinceCreation;
                return new OtpGenerationResult
                {
                    Success = false,
                    ErrorMessage = $"Please wait {remainingCooldown} seconds before requesting a new code",
                    ErrorCode = "COOLDOWN",
                    CanResend = false,
                    ResendCooldownSeconds = remainingCooldown
                };
            }

            // Generate new OTP for the same member and points
            return await GenerateRedemptionOtpAsync(
                otp.LoyaltyMemberId,
                otp.AuthorizedPoints,
                cancellationToken);
        }

        public async Task<RedemptionOtp?> GetPendingOtpAsync(
            int loyaltyMemberId,
            CancellationToken cancellationToken = default)
        {
            var otps = await _unitOfWork.Repository<RedemptionOtp>()
                .FindAsync(o =>
                    o.LoyaltyMemberId == loyaltyMemberId &&
                    o.IsActive &&
                    !o.IsVerified &&
                    o.ExpiresAt > DateTime.UtcNow &&
                    o.AttemptCount < o.MaxAttempts,
                    cancellationToken);

            return otps.FirstOrDefault();
        }

        public async Task InvalidatePendingOtpsAsync(
            int loyaltyMemberId,
            CancellationToken cancellationToken = default)
        {
            var pendingOtps = await _unitOfWork.Repository<RedemptionOtp>()
                .FindAsync(o =>
                    o.LoyaltyMemberId == loyaltyMemberId &&
                    o.IsActive &&
                    !o.IsVerified,
                    cancellationToken);

            foreach (var otp in pendingOtps)
            {
                otp.IsActive = false;
            }

            if (pendingOtps.Any())
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogDebug("Invalidated {Count} pending OTPs for member {MemberId}",
                    pendingOtps.Count(), loyaltyMemberId);
            }
        }

        public async Task MarkOtpUsedAsync(
            int otpId,
            int receiptId,
            CancellationToken cancellationToken = default)
        {
            var otp = await _unitOfWork.Repository<RedemptionOtp>()
                .GetByIdAsync(otpId, cancellationToken);

            if (otp != null && otp.IsVerified)
            {
                otp.ReceiptId = receiptId;
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        #region Private Methods

        private static string GenerateOtpCode()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            var number = BitConverter.ToUInt32(bytes, 0) % 1000000;
            return number.ToString("D6"); // Pad to 6 digits
        }

        private string MaskPhoneNumber(string phone)
        {
            // Convert 254712345678 to "07XX XXX X78"
            if (string.IsNullOrEmpty(phone) || phone.Length < 9)
                return "****";

            var local = phone.StartsWith("254") ? "0" + phone[3..] : phone;
            if (local.Length >= 10)
            {
                return $"{local[..2]}XX XXX X{local[^2..]}";
            }
            return $"{local[..2]}XX XXX XX";
        }

        private async Task SendOtpSmsAsync(string phone, string code, string? name, decimal points)
        {
            try
            {
                var displayName = string.IsNullOrEmpty(name) ? "Customer" : name;
                var message = $"Hi {displayName}, your redemption code is {code}. " +
                              $"Valid for 5 minutes. Points: {points:N0}. " +
                              "Do not share this code.";

                await _smsService.SendSmsAsync(phone, message, CancellationToken.None);
                _logger.LogDebug("OTP SMS sent to {Phone}", MaskPhoneNumber(phone));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send OTP SMS to {Phone}", MaskPhoneNumber(phone));
                // Don't throw - OTP is still valid even if SMS fails
            }
        }

        #endregion
    }
}
```

---

### Task 3.5: Integrate OTP into Settlement Redemption

**File:** `src/HospitalityPOS.WPF/ViewModels/SettlementViewModel.cs`

Add OTP verification flow:

```csharp
private readonly IOtpService _otpService;

[ObservableProperty]
private int? _pendingOtpId;

[ObservableProperty]
private string? _otpMaskedPhone;

[ObservableProperty]
private int _otpExpiresInSeconds;

[ObservableProperty]
private string? _otpInput;

[ObservableProperty]
private string? _otpError;

[ObservableProperty]
private int _otpRemainingAttempts;

[ObservableProperty]
private bool _isOtpLocked;

[ObservableProperty]
private bool _canResendOtp;

[RelayCommand]
private async Task RequestRedemptionOtpAsync()
{
    if (LoyaltyMember == null || PointsToRedeem <= 0)
        return;

    try
    {
        IsAwaitingOtpVerification = true;
        OtpError = null;
        OtpInput = string.Empty;

        var result = await _otpService.GenerateRedemptionOtpAsync(
            LoyaltyMember.Id,
            PointsToRedeem);

        if (result.Success)
        {
            PendingOtpId = result.OtpId;
            OtpMaskedPhone = result.MaskedPhone;
            OtpExpiresInSeconds = result.ExpiresInSeconds ?? 300;
            OtpRemainingAttempts = 3;
            CanResendOtp = false;

            // Start countdown timer
            StartOtpCountdown();

            _notificationService.ShowInfo($"Verification code sent to {result.MaskedPhone}");
        }
        else
        {
            OtpError = result.ErrorMessage;
            IsAwaitingOtpVerification = false;
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error requesting redemption OTP");
        OtpError = "Failed to send verification code";
        IsAwaitingOtpVerification = false;
    }
}

[RelayCommand]
private async Task VerifyOtpAndRedeemAsync()
{
    if (PendingOtpId == null || string.IsNullOrWhiteSpace(OtpInput))
        return;

    try
    {
        OtpError = null;

        var result = await _otpService.VerifyOtpAsync(
            PendingOtpId.Value,
            OtpInput,
            _currentUserId);

        if (result.Success)
        {
            // OTP verified - proceed with redemption
            await ProcessRedemptionAsync(result.AuthorizedPoints!.Value);

            IsAwaitingOtpVerification = false;
            PendingOtpId = null;

            _notificationService.ShowSuccess("Points redeemed successfully!");
        }
        else
        {
            OtpRemainingAttempts = result.RemainingAttempts;
            IsOtpLocked = result.IsLocked;

            if (result.IsExpired)
            {
                OtpError = "Code expired. Please request a new code.";
                CanResendOtp = true;
            }
            else if (result.IsLocked)
            {
                OtpError = "Too many attempts. Please request a new code.";
                CanResendOtp = true;
            }
            else
            {
                OtpError = result.ErrorMessage;
            }
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error verifying OTP");
        OtpError = "Verification failed. Please try again.";
    }
}

[RelayCommand]
private async Task ResendOtpAsync()
{
    if (PendingOtpId == null)
        return;

    var result = await _otpService.ResendOtpAsync(PendingOtpId.Value);

    if (result.Success)
    {
        PendingOtpId = result.OtpId;
        OtpExpiresInSeconds = result.ExpiresInSeconds ?? 300;
        OtpRemainingAttempts = 3;
        IsOtpLocked = false;
        OtpError = null;
        OtpInput = string.Empty;
        CanResendOtp = false;

        StartOtpCountdown();
        _notificationService.ShowInfo($"New code sent to {result.MaskedPhone}");
    }
    else
    {
        OtpError = result.ErrorMessage;
        if (result.ResendCooldownSeconds > 0)
        {
            // Start cooldown timer
            StartResendCooldown(result.ResendCooldownSeconds.Value);
        }
    }
}

[RelayCommand]
private void CancelOtpVerification()
{
    IsAwaitingOtpVerification = false;
    PendingOtpId = null;
    PointsToRedeem = 0;
    PointsRedemptionValue = 0;
    CalculateTotals();
}

private async Task ProcessRedemptionAsync(decimal authorizedPoints)
{
    var result = await _loyaltyService.RedeemPointsAsync(
        LoyaltyMember!.Id,
        authorizedPoints,
        CurrentOrder!.Total,
        CurrentOrder.Id,
        CurrentOrder.ReceiptNumber ?? "");

    if (result.Success)
    {
        // Update totals with redemption value
        PointsRedemptionValue = result.RedeemedValue;
        CalculateTotals();

        // Mark OTP as used
        if (PendingOtpId != null)
        {
            await _otpService.MarkOtpUsedAsync(PendingOtpId.Value, CurrentOrder.Id);
        }
    }
    else
    {
        OtpError = result.ErrorMessage;
    }
}

private void StartOtpCountdown()
{
    // Use DispatcherTimer to count down OtpExpiresInSeconds
    // Update CanResendOtp to true when countdown reaches 0
}

private void StartResendCooldown(int seconds)
{
    // Timer to enable resend button after cooldown
}
```

---

### Task 3.6: Add OTP Verification UI

**File:** `src/HospitalityPOS.WPF/Views/SettlementView.xaml`

```xaml
<!-- OTP Verification Dialog -->
<Grid Visibility="{Binding IsAwaitingOtpVerification, Converter={StaticResource BoolToVisibilityConverter}}"
      Background="#80000000"
      Panel.ZIndex="100">
    <Border Background="{DynamicResource ThemeSurfaceBrush}"
            CornerRadius="12"
            Padding="32"
            MaxWidth="420"
            VerticalAlignment="Center"
            HorizontalAlignment="Center"
            Effect="{StaticResource CardShadow}">
        <StackPanel>
            <!-- Header -->
            <StackPanel HorizontalAlignment="Center" Margin="0,0,0,24">
                <TextBlock Text="Verify Redemption"
                           FontSize="24"
                           FontWeight="SemiBold"
                           HorizontalAlignment="Center"
                           Foreground="{DynamicResource ThemeTextPrimaryBrush}"/>
                <TextBlock FontSize="14"
                           HorizontalAlignment="Center"
                           Foreground="{DynamicResource ThemeTextSecondaryBrush}"
                           Margin="0,8,0,0">
                    <Run Text="Code sent to "/>
                    <Run Text="{Binding OtpMaskedPhone}" FontWeight="Medium"/>
                </TextBlock>
            </StackPanel>

            <!-- Points being redeemed -->
            <Border Background="{DynamicResource ThemeBackgroundSecondaryBrush}"
                    CornerRadius="8"
                    Padding="16"
                    Margin="0,0,0,24">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>

                    <StackPanel Grid.Column="0">
                        <TextBlock Text="Redeeming"
                                   FontSize="12"
                                   Foreground="{DynamicResource ThemeTextSecondaryBrush}"/>
                        <TextBlock FontSize="20" FontWeight="SemiBold"
                                   Foreground="{DynamicResource ThemeTextPrimaryBrush}">
                            <Run Text="{Binding PointsToRedeem, StringFormat=N0}"/>
                            <Run Text=" points"/>
                        </TextBlock>
                    </StackPanel>

                    <StackPanel Grid.Column="1" HorizontalAlignment="Right">
                        <TextBlock Text="Value"
                                   FontSize="12"
                                   Foreground="{DynamicResource ThemeTextSecondaryBrush}"/>
                        <TextBlock FontSize="20" FontWeight="SemiBold"
                                   Foreground="{DynamicResource SuccessBrush}">
                            <Run Text="KES "/>
                            <Run Text="{Binding PointsRedemptionValue, StringFormat=N2}"/>
                        </TextBlock>
                    </StackPanel>
                </Grid>
            </Border>

            <!-- OTP Input -->
            <TextBlock Text="Enter verification code"
                       FontSize="12"
                       Foreground="{DynamicResource ThemeTextSecondaryBrush}"
                       Margin="0,0,0,8"/>

            <TextBox Text="{Binding OtpInput, UpdateSourceTrigger=PropertyChanged}"
                     FontSize="32"
                     FontWeight="Bold"
                     TextAlignment="Center"
                     MaxLength="6"
                     Height="64"
                     Padding="16"
                     CharacterCasing="Upper"
                     Style="{StaticResource LargeInputTextBox}"/>

            <!-- Timer and Attempts -->
            <Grid Margin="0,12,0,0">
                <TextBlock HorizontalAlignment="Left"
                           FontSize="12"
                           Foreground="{DynamicResource ThemeTextMutedBrush}">
                    <Run Text="Expires in "/>
                    <Run Text="{Binding OtpExpiresInSeconds, Converter={StaticResource SecondsToTimeConverter}}"/>
                </TextBlock>

                <TextBlock HorizontalAlignment="Right"
                           FontSize="12"
                           Foreground="{DynamicResource ThemeTextMutedBrush}"
                           Visibility="{Binding OtpRemainingAttempts, Converter={StaticResource PositiveToVisibilityConverter}}">
                    <Run Text="{Binding OtpRemainingAttempts}"/>
                    <Run Text=" attempts left"/>
                </TextBlock>
            </Grid>

            <!-- Error Message -->
            <TextBlock Text="{Binding OtpError}"
                       Foreground="{DynamicResource DangerBrush}"
                       FontSize="13"
                       TextWrapping="Wrap"
                       Visibility="{Binding OtpError, Converter={StaticResource NullToVisibilityConverter}}"
                       Margin="0,12,0,0"/>

            <!-- Buttons -->
            <Grid Margin="0,24,0,0">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="12"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>

                <Button Grid.Column="0"
                        Content="Cancel"
                        Command="{Binding CancelOtpVerificationCommand}"
                        Style="{StaticResource SecondaryButton}"
                        Height="48"/>

                <Button Grid.Column="2"
                        Content="Verify &amp; Redeem"
                        Command="{Binding VerifyOtpAndRedeemCommand}"
                        Style="{StaticResource PrimaryButton}"
                        Height="48"/>
            </Grid>

            <!-- Resend Link -->
            <Button Content="Resend Code"
                    Command="{Binding ResendOtpCommand}"
                    IsEnabled="{Binding CanResendOtp}"
                    Style="{StaticResource LinkButton}"
                    HorizontalAlignment="Center"
                    Margin="0,16,0,0"/>
        </StackPanel>
    </Border>
</Grid>
```

---

## Phase 4: Loyalty Configuration UI

### Overview
Complete the LoyaltySettingsViewModel to allow saving configuration changes from the UI.

---

### Task 4.1: Complete LoyaltySettingsViewModel Save

**File:** `src/HospitalityPOS.WPF/ViewModels/LoyaltySettingsViewModel.cs`

```csharp
[RelayCommand]
private async Task SaveSettingsAsync()
{
    try
    {
        IsSaving = true;
        ValidationErrors.Clear();

        // Validate settings
        if (!ValidateSettings())
            return;

        // Get or create configuration
        var config = await _loyaltyService.GetPointsConfigurationAsync()
            ?? new PointsConfiguration { Name = "Default", IsDefault = true };

        // Update earning settings
        config.EarningRate = CurrencyUnitsPerPoint; // e.g., 100 KES = 1 point
        config.EarnOnDiscountedItems = EarnOnDiscountedItems;
        config.EarnOnTax = EarnOnTax;

        // Update redemption settings
        config.RedemptionValue = PointValueInKes; // e.g., 1 point = 1 KES
        config.MinimumRedemptionPoints = MinimumRedemptionPoints;
        config.MaxRedemptionPercentage = MaximumRedemptionPercent;

        // Update expiry settings
        config.PointsExpiryDays = EnablePointsExpiry ? PointsExpiryMonths * 30 : 0;

        // Save to database
        await _unitOfWork.Repository<PointsConfiguration>().UpdateAsync(config);
        await _unitOfWork.SaveChangesAsync();

        // Update SMS notification settings (stored separately)
        await SaveSmsSettingsAsync();

        _notificationService.ShowSuccess("Loyalty settings saved successfully");

        _logger.LogInformation("Loyalty settings updated by user {UserId}", _currentUserId);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error saving loyalty settings");
        _notificationService.ShowError("Failed to save settings. Please try again.");
    }
    finally
    {
        IsSaving = false;
    }
}

private bool ValidateSettings()
{
    var isValid = true;

    if (CurrencyUnitsPerPoint <= 0)
    {
        ValidationErrors.Add("Earning rate must be greater than 0");
        isValid = false;
    }

    if (PointValueInKes <= 0)
    {
        ValidationErrors.Add("Redemption value must be greater than 0");
        isValid = false;
    }

    if (MinimumRedemptionPoints < 0)
    {
        ValidationErrors.Add("Minimum redemption points cannot be negative");
        isValid = false;
    }

    if (MaximumRedemptionPercent < 0 || MaximumRedemptionPercent > 100)
    {
        ValidationErrors.Add("Maximum redemption percentage must be between 0 and 100");
        isValid = false;
    }

    if (EnablePointsExpiry && PointsExpiryMonths <= 0)
    {
        ValidationErrors.Add("Expiry period must be at least 1 month");
        isValid = false;
    }

    return isValid;
}

private async Task SaveSmsSettingsAsync()
{
    // Save SMS notification preferences
    var smsSettings = new Dictionary<string, bool>
    {
        ["SendEnrollmentSms"] = SendEnrollmentSms,
        ["SendPointsEarnedSms"] = SendPointsEarnedSms,
        ["SendRedemptionSms"] = SendRedemptionSms,
        ["SendExpiryWarningSms"] = SendExpiryWarningSms
    };

    await _settingsService.SaveLoyaltySmsSettingsAsync(smsSettings);
}
```

---

## Phase 5: Sidebar Menu Integration

### Overview
Add all loyalty-related features to the admin dashboard sidebar for easy access.

---

### Task 5.1: Add Loyalty Menu Items to MainWindow.xaml

**File:** `src/HospitalityPOS.WPF/Views/MainWindow.xaml`

Add a new LOYALTY section to the sidebar:

```xaml
<!-- LOYALTY Section -->
<TextBlock Text="LOYALTY"
           Style="{StaticResource SidebarSectionHeader}"
           Margin="0,24,0,8"/>

<Button Style="{StaticResource SidebarButton}"
        Command="{Binding NavigateToLoyaltySettingsCommand}">
    <StackPanel Orientation="Horizontal">
        <Path Data="{StaticResource SettingsIcon}" Style="{StaticResource SidebarIcon}"/>
        <TextBlock Text="Program Settings" Style="{StaticResource SidebarButtonText}"/>
    </StackPanel>
</Button>

<Button Style="{StaticResource SidebarButton}"
        Command="{Binding NavigateToCustomerListCommand}">
    <StackPanel Orientation="Horizontal">
        <Path Data="{StaticResource UsersIcon}" Style="{StaticResource SidebarIcon}"/>
        <TextBlock Text="Members" Style="{StaticResource SidebarButtonText}"/>
    </StackPanel>
</Button>

<Button Style="{StaticResource SidebarButton}"
        Command="{Binding NavigateToCustomerEnrollmentCommand}">
    <StackPanel Orientation="Horizontal">
        <Path Data="{StaticResource UserPlusIcon}" Style="{StaticResource SidebarIcon}"/>
        <TextBlock Text="Enroll Customer" Style="{StaticResource SidebarButtonText}"/>
    </StackPanel>
</Button>

<Button Style="{StaticResource SidebarButton}"
        Command="{Binding NavigateToCustomerAnalyticsCommand}">
    <StackPanel Orientation="Horizontal">
        <Path Data="{StaticResource ChartIcon}" Style="{StaticResource SidebarIcon}"/>
        <TextBlock Text="Member Analytics" Style="{StaticResource SidebarButtonText}"/>
    </StackPanel>
</Button>
```

---

### Task 5.2: Add Navigation Commands to MainWindowViewModel

**File:** `src/HospitalityPOS.WPF/ViewModels/MainWindowViewModel.cs`

```csharp
[RelayCommand]
private void NavigateToLoyaltySettings()
{
    CurrentView = _serviceProvider.GetRequiredService<LoyaltySettingsViewModel>();
}

[RelayCommand]
private void NavigateToCustomerList()
{
    CurrentView = _serviceProvider.GetRequiredService<CustomerListViewModel>();
}

[RelayCommand]
private void NavigateToCustomerEnrollment()
{
    CurrentView = _serviceProvider.GetRequiredService<CustomerEnrollmentViewModel>();
}

[RelayCommand]
private void NavigateToCustomerAnalytics()
{
    CurrentView = _serviceProvider.GetRequiredService<CustomerAnalyticsViewModel>();
}
```

---

## Database Schema Reference

### Tables Overview

| Table | Purpose |
|-------|---------|
| `LoyaltyMembers` | Customer loyalty accounts |
| `LoyaltyTransactions` | Points earning/redemption history |
| `PointsConfigurations` | Program rules and rates |
| `TierConfigurations` | Tier definitions and benefits |
| `RedemptionOtps` | OTP verification records |

### Key Relationships

```
LoyaltyMember (1) --> (*) LoyaltyTransaction
LoyaltyMember (1) --> (*) RedemptionOtp
Receipt (1) --> (0..1) LoyaltyTransaction
Receipt (1) --> (0..1) RedemptionOtp
```

---

## API Reference

### ILoyaltyService Methods

| Category | Method | Purpose |
|----------|--------|---------|
| **Enrollment** | `EnrollCustomerAsync()` | Register new member |
| | `GetByPhoneAsync()` | Find by phone |
| | `SearchMembersAsync()` | Search members |
| **Earning** | `CalculatePointsAsync()` | Preview points |
| | `AwardPointsAsync()` | Award points |
| **Redemption** | `CalculateRedemptionAsync()` | Preview redemption |
| | `RedeemPointsAsync()` | Process redemption |
| **Tiers** | `GetTierConfigurationsAsync()` | Get tier info |
| | `CheckAndUpgradeTierAsync()` | Auto-upgrade |

### IOtpService Methods

| Method | Purpose |
|--------|---------|
| `GenerateRedemptionOtpAsync()` | Create and send OTP |
| `VerifyOtpAsync()` | Validate OTP code |
| `ResendOtpAsync()` | Resend with cooldown |
| `InvalidatePendingOtpsAsync()` | Cancel pending OTPs |

---

## Best Practices Reference

### Industry Standards (From Research)

| Area | Best Practice | Our Implementation |
|------|---------------|-------------------|
| **Earning Rate** | 1-5% of spend | 100 KES = 1 point (1%) |
| **Redemption Value** | 1-2% return | 1 point = 1 KES |
| **Minimum Redemption** | Low threshold | 100 points |
| **Maximum Redemption** | 25-50% of transaction | 50% |
| **Phone as ID** | Fast lookup | Primary identifier |
| **OTP Verification** | Required for redemption | 6-digit, 5-min expiry |
| **OTP Attempts** | 3 max | Implemented |
| **Resend Cooldown** | 60 seconds | Implemented |
| **Tier Bonuses** | Progressive multipliers | 1x/1.25x/1.5x/2x |

---

## Testing Checklist

### Theme Testing
- [ ] Fresh install shows light mode
- [ ] Theme toggle works in Organization Settings
- [ ] Theme persists after restart
- [ ] All UI elements readable in both themes

### Loyalty Enrollment Testing
- [ ] Phone validation works (0712xxx, 254712xxx)
- [ ] Duplicate detection works
- [ ] Welcome SMS sent
- [ ] Membership number generated

### Points Earning Testing
- [ ] Phone prompt shows after transaction
- [ ] New customer enrollment works
- [ ] Existing customer lookup works
- [ ] Points calculated correctly
- [ ] Tier bonus applied correctly
- [ ] Points earned SMS sent

### Points Redemption Testing
- [ ] Available points displayed
- [ ] Minimum/maximum constraints enforced
- [ ] OTP generated and sent
- [ ] OTP verification works
- [ ] Wrong OTP decrements attempts
- [ ] OTP expiry works
- [ ] OTP lockout works
- [ ] Resend cooldown works
- [ ] Points deducted on success
- [ ] Redemption transaction created

### Settings Testing
- [ ] Settings load correctly
- [ ] Settings save correctly
- [ ] Validation prevents invalid values
- [ ] Changes apply to new transactions

---

## Change Log

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2026-01-24 | Initial document |

---

*End of Implementation Plan Document*
