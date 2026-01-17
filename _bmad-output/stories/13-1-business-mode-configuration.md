# Story 13.1: Business Mode Configuration

Status: done

## Story

As an administrator,
I want to select the business mode during initial setup,
So that the system is configured with the appropriate features for my business type.

## Acceptance Criteria

1. **Given** a fresh installation
   **When** the setup wizard runs
   **Then** administrator can select: Restaurant, Supermarket, or Hybrid mode

2. **Given** a mode is selected
   **When** configuration is saved
   **Then** appropriate feature flags are set based on mode

3. **Given** the system is running
   **When** mode needs to change
   **Then** administrator can switch modes from Settings (requires restart)

## Tasks / Subtasks

- [x] Task 1: Create Business Mode Configuration Entity
  - [x] Create BusinessMode enum (Restaurant, Supermarket, Hybrid)
  - [x] Create SystemConfiguration class with feature flags
  - [x] Add to SystemSettings table
  - [x] Create EF Core configuration

- [x] Task 2: Create Setup Wizard for Mode Selection
  - [x] Create SetupWizardView.xaml
  - [x] Create SetupWizardViewModel
  - [x] Design mode selection cards with descriptions
  - [x] Save mode and apply default feature flags

- [x] Task 3: Create Mode Configuration Service
  - [x] Create ISystemConfigurationService interface
  - [x] Implement GetCurrentMode(), SetMode()
  - [x] Implement ApplyModeDefaults()
  - [x] Add restart notification when mode changes

- [x] Task 4: Integrate Mode Check at Startup
  - [x] Check if mode is configured on app start
  - [x] Show setup wizard if first run
  - [x] Load mode-specific resources

## Dev Notes

### BusinessMode Enum

```csharp
public enum BusinessMode
{
    Restaurant = 1,
    Supermarket = 2,
    Hybrid = 3
}
```

### SystemConfiguration Class

```csharp
public class SystemConfiguration
{
    public BusinessMode Mode { get; set; } = BusinessMode.Restaurant;

    // Feature Flags
    public bool EnableTableManagement { get; set; } = true;
    public bool EnableKitchenDisplay { get; set; } = true;
    public bool EnableWaiterAssignment { get; set; } = true;
    public bool EnableBarcodeAutoFocus { get; set; } = false;
    public bool EnableProductOffers { get; set; } = false;
    public bool EnableSupplierCredit { get; set; } = false;
    public bool EnablePayroll { get; set; } = false;
    public bool EnableAccounting { get; set; } = false;

    public void ApplyModeDefaults()
    {
        switch (Mode)
        {
            case BusinessMode.Restaurant:
                EnableTableManagement = true;
                EnableKitchenDisplay = true;
                EnableWaiterAssignment = true;
                EnableBarcodeAutoFocus = false;
                EnableProductOffers = false;
                EnableSupplierCredit = false;
                break;

            case BusinessMode.Supermarket:
                EnableTableManagement = false;
                EnableKitchenDisplay = false;
                EnableWaiterAssignment = false;
                EnableBarcodeAutoFocus = true;
                EnableProductOffers = true;
                EnableSupplierCredit = true;
                break;

            case BusinessMode.Hybrid:
                // All features enabled
                EnableTableManagement = true;
                EnableKitchenDisplay = true;
                EnableWaiterAssignment = true;
                EnableBarcodeAutoFocus = true;
                EnableProductOffers = true;
                EnableSupplierCredit = true;
                break;
        }
    }
}
```

### Mode Selection UI

```
+------------------------------------------+
|        SELECT YOUR BUSINESS TYPE         |
+------------------------------------------+
|                                          |
|  +----------+  +----------+  +----------+|
|  |  [icon]  |  |  [icon]  |  |  [icon]  ||
|  |Restaurant|  |Supermarket|  | Hybrid  ||
|  |          |  |          |  |          ||
|  | Tables   |  | Barcode  |  |   All    ||
|  | Kitchen  |  | Offers   |  | Features ||
|  | Waiters  |  | Payroll  |  |          ||
|  +----------+  +----------+  +----------+|
|                                          |
|              [Continue >>]               |
+------------------------------------------+
```

### References
- [Source: PRD Section 12.1]
- [Source: Architecture Section 2.3]

## Dev Agent Record

### Agent Model Used
Claude Opus 4.5 (claude-opus-4-5-20251101)

### Completion Notes List
- Created SystemConfiguration entity with BusinessMode enum (Restaurant, Supermarket, Hybrid)
- Comprehensive feature flags for restaurant, retail, enterprise, and Kenya-specific features
- Created EF Core configuration for SystemConfigurations table
- Updated POSDbContext with SystemConfigurations DbSet
- Created ISystemConfigurationService interface with mode management and feature flag methods
- Implemented SystemConfigurationService with caching, feature toggling, and configuration change events
- Created SetupWizardViewModel with 3-step wizard flow (mode selection, business details, review)
- Created SetupWizardView.xaml with responsive design and mode selection cards
- Created SetupWizardConverters for step indicators and visibility management
- Integrated setup check at application startup - shows wizard on first run
- Support for multiple currencies (KES, USD, EUR, GBP, TZS, UGX)
- ApplyModeDefaults() method for automatic feature flag configuration based on mode

### File List
- src/HospitalityPOS.Core/Entities/SystemConfiguration.cs
- src/HospitalityPOS.Core/Interfaces/ISystemConfigurationService.cs
- src/HospitalityPOS.Infrastructure/Data/Configurations/SystemConfigurationConfiguration.cs
- src/HospitalityPOS.Infrastructure/Data/POSDbContext.cs (updated)
- src/HospitalityPOS.Infrastructure/Services/SystemConfigurationService.cs
- src/HospitalityPOS.WPF/ViewModels/SetupWizardViewModel.cs
- src/HospitalityPOS.WPF/Views/SetupWizardView.xaml
- src/HospitalityPOS.WPF/Views/SetupWizardView.xaml.cs
- src/HospitalityPOS.WPF/Converters/SetupWizardConverters.cs
- src/HospitalityPOS.WPF/Resources/Converters.xaml (updated)
- src/HospitalityPOS.WPF/App.xaml.cs (updated)
