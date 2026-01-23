## Overview

Add SMS Marketing module to the Admin Sidebar navigation and complete the UI views for managing promotional campaigns, customer segmentation, and SMS templates. The backend service (`SmsMarketingService`) is fully implemented but lacks UI integration.

## Current State Analysis

### Backend Implementation (COMPLETE)
Located: `HospitalityPOS.Infrastructure/Services/SmsMarketingService.cs`

**Existing Features:**
- SMS Template Management (CRUD, categories, placeholder substitution)
- Customer Segmentation (by purchase history, spend, tier, lapsed customers)
- Campaign Management (create, schedule, send, pause, resume, cancel)
- Opt-In/Opt-Out Management
- Transactional SMS Configuration
- Campaign Reports and Usage Reports

**Default Templates Include:**
- "Promotion - Weekend Special"
- "Points Reminder"
- "Birthday Greeting"
- "Tier Upgrade"
- "Points Expiry Warning"
- "Welcome Message"

### UI Views (MISSING)
No views found for:
- SMS Template Management
- Customer Segment Builder
- Campaign Creation/Management
- SMS Dashboard
- Opt-Out Management
- SMS Settings

### Sidebar Navigation (MISSING)
Not present in `MainWindow.xaml` sidebar sections.

## Requirements

### 1. Add to Sidebar Navigation

Add new "MARKETING" section to sidebar between ANALYTICS and HR:

```xml
<!-- Marketing Group -->
<TextBlock Text="MARKETING"
           Style="{StaticResource SidebarGroupHeaderStyle}" />

<!-- SMS Campaigns -->
<Button Style="{StaticResource SidebarMenuItemStyle}"
        Command="{Binding NavigateToSmsCampaignsCommand}">
    <StackPanel Orientation="Horizontal">
        <TextBlock Text="&#xE8BD;" FontFamily="Segoe MDL2 Assets" FontSize="16" Width="24" />
        <TextBlock Text="SMS Campaigns" Margin="12,0,0,0" VerticalAlignment="Center" />
    </StackPanel>
</Button>

<!-- SMS Templates -->
<Button Style="{StaticResource SidebarMenuItemStyle}"
        Command="{Binding NavigateToSmsTemplatesCommand}">
    <StackPanel Orientation="Horizontal">
        <TextBlock Text="&#xE8C1;" FontFamily="Segoe MDL2 Assets" FontSize="16" Width="24" />
        <TextBlock Text="SMS Templates" Margin="12,0,0,0" VerticalAlignment="Center" />
    </StackPanel>
</Button>

<!-- Customer Segments -->
<Button Style="{StaticResource SidebarMenuItemStyle}"
        Command="{Binding NavigateToCustomerSegmentsCommand}">
    <StackPanel Orientation="Horizontal">
        <TextBlock Text="&#xE902;" FontFamily="Segoe MDL2 Assets" FontSize="16" Width="24" />
        <TextBlock Text="Customer Segments" Margin="12,0,0,0" VerticalAlignment="Center" />
    </StackPanel>
</Button>
```

### 2. SMS Campaign Dashboard View

**Components:**
- Campaign summary cards (Active, Scheduled, Completed, Draft)
- Recent campaigns list with status indicators
- Quick actions (Create Campaign, View Reports)
- Campaign performance metrics
- SMS usage/balance indicator (if provider integration)

**Features:**
- Filter by status, date range
- Search campaigns
- View campaign details
- Pause/Resume running campaigns
- Duplicate campaign
- Export campaign results

### 3. Campaign Creation Wizard

**Step 1: Campaign Basics**
- Campaign name
- Description
- Campaign type (Promotional, Loyalty, Transactional)

**Step 2: Select Audience**
- Use existing segment
- Create new segment with criteria:
  - Customers who purchased in last X days
  - Customers who purchased category/product
  - Customers by spend range
  - Customers by loyalty tier
  - Lapsed customers (no visit in X days)
  - All opted-in customers
- Preview audience count

**Step 3: Compose Message**
- Select template or write custom
- Insert placeholders: {FirstName}, {LastName}, {Points}, {StoreName}, etc.
- Character count with SMS segment indicator
- Preview rendered message
- Test send to admin phone

**Step 4: Schedule**
- Send immediately
- Schedule for specific date/time
- Recurring schedule (weekly promotions)

**Step 5: Review & Launch**
- Summary of all settings
- Estimated cost (if applicable)
- Confirm and launch

### 4. SMS Template Manager View

**Components:**
- Template list with categories
- Template editor with:
  - Name, category, description
  - Message body with placeholder toolbar
  - Character count
  - Preview panel
- Template preview with sample data
- Import/Export templates

**Categories:**
- Promotion
- Loyalty
- Transactional
- Special (Birthdays, Events)

**Placeholders:**
- {FirstName}, {LastName}, {FullName}
- {Phone}, {Email}
- {Points}, {PointsExpiry}
- {LoyaltyTier}
- {StoreName}, {StorePhone}
- {OrderNumber}, {OrderTotal}
- {DiscountCode}, {DiscountPercent}
- {UnsubscribeLink}

### 5. Customer Segment Builder View

**Components:**
- Segment list with customer counts
- Segment builder with:
  - Segment name
  - Criteria builder (AND/OR logic)
  - Real-time customer count preview
- Save segment for reuse

**Criteria Options:**
- **Purchase History**
  - Purchased within last X days
  - Purchased specific category
  - Purchased specific product
  - Purchase amount range
  - Purchase frequency

- **Customer Attributes**
  - Loyalty tier (Bronze, Silver, Gold, Platinum)
  - Points balance range
  - Member since date
  - Birthday month (for birthday campaigns)

- **Engagement**
  - Last visit date
  - Lapsed customers (no visit in X days)
  - SMS engaged (opened/clicked)

### 6. Opt-Out Management View

**Components:**
- Opted-out customer list
- Opt-out log with timestamps
- Manual opt-in/opt-out actions
- Bulk import opt-out list
- GDPR/compliance export

### 7. SMS Settings View

**Components:**
- SMS Provider configuration
  - Provider selection (Africa's Talking, Twilio, etc.)
  - API credentials
  - Sender ID
  - Test connection

- Default settings
  - Default sender name
  - Unsubscribe message format
  - Character encoding

- Compliance settings
  - Quiet hours (no SMS between X and Y)
  - Daily send limit per customer
  - Required opt-in confirmation

### 8. ViewModels Required

```csharp
// Dashboard
public class SmsCampaignDashboardViewModel : ViewModelBase
{
    // Campaign statistics
    // Recent campaigns
    // Quick actions
}

// Campaign Management
public class SmsCampaignListViewModel : ViewModelBase
{
    // Campaign list with filtering
    // CRUD operations
}

public class SmsCampaignEditorViewModel : ViewModelBase
{
    // Campaign wizard logic
    // Audience selection
    // Message composition
    // Scheduling
}

// Templates
public class SmsTemplateListViewModel : ViewModelBase
{
    // Template CRUD
    // Category filtering
}

public class SmsTemplateEditorViewModel : ViewModelBase
{
    // Template editing
    // Placeholder insertion
    // Preview
}

// Segments
public class CustomerSegmentListViewModel : ViewModelBase
{
    // Segment CRUD
    // Customer counts
}

public class CustomerSegmentBuilderViewModel : ViewModelBase
{
    // Criteria builder
    // Preview customers
}

// Settings
public class SmsSettingsViewModel : ViewModelBase
{
    // Provider configuration
    // Default settings
}
```

### 9. Navigation Commands in MainWindowViewModel

```csharp
public ICommand NavigateToSmsCampaignsCommand { get; }
public ICommand NavigateToSmsTemplatesCommand { get; }
public ICommand NavigateToCustomerSegmentsCommand { get; }
public ICommand NavigateToSmsSettingsCommand { get; }

private void NavigateToSmsCampaigns()
{
    MainContent = new SmsCampaignDashboardView();
}
```

## Acceptance Criteria

- [ ] MARKETING section appears in admin sidebar
- [ ] SMS Campaigns menu item navigates to campaign dashboard
- [ ] Campaign dashboard shows active, scheduled, and completed campaigns
- [ ] New campaign can be created with wizard workflow
- [ ] Customer segments can be created with criteria builder
- [ ] Audience count previews correctly
- [ ] SMS templates can be created and managed
- [ ] Template placeholders render correctly in preview
- [ ] Campaigns can be scheduled for future send
- [ ] Campaigns can be paused/resumed
- [ ] Opt-out list is accessible and manageable
- [ ] SMS provider settings are configurable

## Implementation Notes

### Existing Code to Leverage
- `ISmsMarketingService` - Full service interface
- `SmsMarketingService` - Complete implementation
- `SmsDtos.cs` - All DTOs for campaigns, templates, segments

### Files to Create
```
Views/Marketing/
├── SmsCampaignDashboardView.xaml
├── SmsCampaignDashboardView.xaml.cs
├── SmsCampaignListView.xaml
├── SmsCampaignEditorDialog.xaml
├── SmsTemplateListView.xaml
├── SmsTemplateEditorDialog.xaml
├── CustomerSegmentListView.xaml
├── CustomerSegmentBuilderDialog.xaml
├── SmsOptOutManagementView.xaml
└── SmsSettingsView.xaml

ViewModels/Marketing/
├── SmsCampaignDashboardViewModel.cs
├── SmsCampaignListViewModel.cs
├── SmsCampaignEditorViewModel.cs
├── SmsTemplateListViewModel.cs
├── SmsTemplateEditorViewModel.cs
├── CustomerSegmentListViewModel.cs
├── CustomerSegmentBuilderViewModel.cs
└── SmsSettingsViewModel.cs
```

### SMS Provider Integration
The service currently uses in-memory simulation. Production requires:
- Africa's Talking API integration (Kenya)
- Twilio integration (international)
- SMS gateway configuration UI

## References

- [Lightspeed - SMS Marketing](https://www.lightspeedhq.com/blog/what-is-sms-marketing-and-how-to-use-it-for-your-business/)
- [Textdrip - POS Data for SMS Marketing](https://textdrip.com/blog/pos-data-retail-sms-marketing)
- [Klaviyo - SMS Segmentation](https://help.klaviyo.com/hc/en-us/articles/360047879512)

---

**Priority**: Medium
**Estimated Complexity**: Large
**Labels**: feature, marketing, ui
