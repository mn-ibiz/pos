## Overview

Add Customers Management module to the Admin Sidebar navigation. Customer data exists in the system but there's no dedicated UI for managing customers from the admin interface.

## Current State Analysis

### Customer Entity (EXISTS)
The system tracks customers through:
- Sales transactions with customer info
- Loyalty program membership
- SMS marketing opt-ins
- M-Pesa payment phone numbers

### What's Missing from Sidebar
- Customer list view
- Customer profile/detail view
- Customer CRUD operations
- Customer search and filtering
- Loyalty management per customer
- Purchase history per customer
- Communication preferences

## Requirements

### 1. Add to Sidebar Navigation

Add CUSTOMERS section or add under existing section:

```xml
<!-- Option A: New CUSTOMERS section after PRODUCTS -->
<TextBlock Text="CUSTOMERS"
           Style="{StaticResource SidebarGroupHeaderStyle}" />

<!-- Customer List -->
<Button Style="{StaticResource SidebarMenuItemStyle}"
        Command="{Binding NavigateToCustomersCommand}">
    <StackPanel Orientation="Horizontal">
        <TextBlock Text="&#xE77B;" FontFamily="Segoe MDL2 Assets" FontSize="16" Width="24" />
        <TextBlock Text="Customers" Margin="12,0,0,0" VerticalAlignment="Center" />
    </StackPanel>
</Button>

<!-- Loyalty Program -->
<Button Style="{StaticResource SidebarMenuItemStyle}"
        Command="{Binding NavigateToLoyaltyProgramCommand}">
    <StackPanel Orientation="Horizontal">
        <TextBlock Text="&#xE735;" FontFamily="Segoe MDL2 Assets" FontSize="16" Width="24" />
        <TextBlock Text="Loyalty Program" Margin="12,0,0,0" VerticalAlignment="Center" />
    </StackPanel>
</Button>
```

### 2. Customer List View

**Components:**
- Customer grid with key info (Name, Phone, Email, Points, Last Visit)
- Search by name, phone, email
- Filter by:
  - Loyalty tier
  - Last visit date range
  - Total spend range
  - SMS opt-in status
- Quick actions (View, Edit, Send SMS)
- Export customer list

### 3. Customer Detail View

**Sections:**
- **Profile**: Name, phone, email, address, birthday
- **Loyalty**: Current tier, points balance, points history
- **Purchase History**: Recent transactions, total lifetime value
- **Communications**: SMS opt-in status, message history
- **Notes**: Staff notes about customer

### 4. Customer Sources

Customers are created from:
1. **POS Sales** - When receipt is linked to customer
2. **Loyalty Sign-up** - Explicit registration
3. **M-Pesa Payments** - Auto-capture phone numbers
4. **Manual Entry** - Admin creates customer

### 5. Loyalty Program Management

**Tier Configuration:**
- Define tiers (Bronze, Silver, Gold, Platinum)
- Points thresholds per tier
- Benefits per tier (discounts, rewards)

**Points Rules:**
- Points per spend (e.g., 1 point per KES 100)
- Bonus points campaigns
- Points expiry rules

**Rewards:**
- Redemption catalog
- Points-to-cash conversion

### 6. Customer Import/Export

- Import customers from CSV
- Export for marketing tools
- Sync with external CRM (future)

## Acceptance Criteria

- [ ] Customers menu item appears in sidebar
- [ ] Customer list view with search and filters
- [ ] Customer detail view shows profile, loyalty, history
- [ ] New customers can be created manually
- [ ] Customer profile can be edited
- [ ] Purchase history displayed per customer
- [ ] Loyalty points visible and manageable
- [ ] Customers exportable to CSV
- [ ] SMS opt-in status visible and editable

## Implementation Notes

### Views to Create
```
Views/Customers/
├── CustomerListView.xaml
├── CustomerDetailView.xaml
├── CustomerEditorDialog.xaml
├── LoyaltyProgramView.xaml
└── LoyaltyTierEditorDialog.xaml

ViewModels/Customers/
├── CustomerListViewModel.cs
├── CustomerDetailViewModel.cs
├── CustomerEditorViewModel.cs
└── LoyaltyProgramViewModel.cs
```

### Navigation Commands
```csharp
public ICommand NavigateToCustomersCommand { get; }
public ICommand NavigateToLoyaltyProgramCommand { get; }
```

---

**Priority**: Medium
**Estimated Complexity**: Medium
**Labels**: feature, customers, ui, navigation
