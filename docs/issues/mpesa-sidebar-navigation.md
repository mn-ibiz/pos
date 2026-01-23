## Overview

Add M-Pesa module to the Admin Sidebar navigation. The backend service and UI views exist but are not accessible from the main navigation menu.

## Current State Analysis

### Backend Implementation (COMPLETE)
Located: `HospitalityPOS.Infrastructure/Services/MpesaService.cs`

**Existing Features:**
- M-Pesa Configuration Management
- STK Push (Lipa Na M-Pesa Online)
- Transaction Recording (auto and manual)
- Transaction Verification
- Payment Matching
- Daraja API Integration Structure

### UI Views (EXIST but not in sidebar)
Located: `HospitalityPOS.WPF/Views/`
- `MpesaSettingsView.xaml` - Configuration management
- `MpesaDashboardView.xaml` - Transaction dashboard
- `MpesaPaymentDialog.xaml` - Payment dialog

### ViewModels (EXIST)
- `MpesaSettingsViewModel.cs`
- `MpesaDashboardViewModel.cs`

### Sidebar Navigation (MISSING)
Not present in `MainWindow.xaml` sidebar - views exist but cannot be accessed.

## Requirements

### 1. Add to Sidebar Navigation

Add M-Pesa items to the FINANCE section (after Expenses):

```xml
<!-- Finance Group -->
<TextBlock Text="FINANCE"
           Style="{StaticResource SidebarGroupHeaderStyle}" />

<!-- Expenses (existing) -->
<Button Style="{StaticResource SidebarMenuItemStyle}"
        Command="{Binding NavigateToExpensesCommand}">
    ...
</Button>

<!-- M-Pesa Dashboard (NEW) -->
<Button Style="{StaticResource SidebarMenuItemStyle}"
        Command="{Binding NavigateToMpesaDashboardCommand}">
    <StackPanel Orientation="Horizontal">
        <TextBlock Text="&#xE8C7;" FontFamily="Segoe MDL2 Assets" FontSize="16" Width="24" />
        <TextBlock Text="M-Pesa Payments" Margin="12,0,0,0" VerticalAlignment="Center" />
    </StackPanel>
</Button>

<!-- M-Pesa Settings (NEW) - Under SETTINGS section -->
<Button Style="{StaticResource SidebarMenuItemStyle}"
        Command="{Binding NavigateToMpesaSettingsCommand}">
    <StackPanel Orientation="Horizontal">
        <TextBlock Text="&#xE912;" FontFamily="Segoe MDL2 Assets" FontSize="16" Width="24" />
        <TextBlock Text="M-Pesa Settings" Margin="12,0,0,0" VerticalAlignment="Center" />
    </StackPanel>
</Button>
```

### 2. Navigation Commands in MainWindowViewModel

```csharp
public ICommand NavigateToMpesaDashboardCommand { get; }
public ICommand NavigateToMpesaSettingsCommand { get; }

private void NavigateToMpesaDashboard()
{
    MainContent = new MpesaDashboardView();
}

private void NavigateToMpesaSettings()
{
    MainContent = new MpesaSettingsView();
}
```

### 3. M-Pesa Dashboard Enhancements

While adding to sidebar, ensure the dashboard includes:

**Summary Cards:**
- Today's M-Pesa Revenue
- Pending STK Push requests
- Failed transactions (needs attention)
- This month's M-Pesa total

**Transaction List:**
- Filter by status (Completed, Pending, Failed)
- Filter by date range
- Search by phone number or receipt number
- Manual entry option for offline payments

**Quick Actions:**
- Initiate STK Push
- Record manual payment
- Verify pending transaction
- Export transactions

### 4. Customer Phone Number Extraction

As mentioned in requirements, M-Pesa payments should:
- Automatically capture customer phone numbers
- Link to customer records if matching
- Create new customer profile option
- Build marketing contact list from payers

**Enhancement to MpesaTransaction:**
```csharp
// Link to customer if phone matches
public int? CustomerId { get; set; }
public virtual Customer Customer { get; set; }

// Customer creation flag
public bool CustomerCreated { get; set; }
```

### 5. Permissions

Add permission checks for M-Pesa access:
- `Permissions.Mpesa.View` - View dashboard
- `Permissions.Mpesa.Manage` - Configure settings
- `Permissions.Mpesa.RecordManual` - Manual entry
- `Permissions.Mpesa.Verify` - Verify transactions

## Acceptance Criteria

- [ ] M-Pesa Payments appears in FINANCE section of sidebar
- [ ] M-Pesa Settings appears in SETTINGS section of sidebar
- [ ] Clicking M-Pesa Payments navigates to dashboard
- [ ] Clicking M-Pesa Settings navigates to configuration
- [ ] Dashboard shows transaction summary and list
- [ ] Transactions can be filtered and searched
- [ ] Manual payment entry works
- [ ] Phone numbers captured for marketing use

## Implementation Notes

### Files to Modify
- `MainWindow.xaml` - Add sidebar menu items
- `MainWindowViewModel.cs` - Add navigation commands

### Existing Views to Use
- `MpesaDashboardView.xaml` - Already exists
- `MpesaSettingsView.xaml` - Already exists
- `MpesaPaymentDialog.xaml` - Already exists

---

**Priority**: High
**Estimated Complexity**: Small
**Labels**: feature, ui, navigation, mpesa
