# Story 13.2: Feature Toggle Management

Status: done

## Story

As an administrator,
I want to enable or disable specific features independently,
So that I can customize the system beyond the default mode settings.

## Acceptance Criteria

1. **Given** a configured mode
   **When** accessing Feature Settings
   **Then** all features show current enabled/disabled state

2. **Given** a feature is toggled
   **When** changes are saved
   **Then** the feature is immediately enabled/disabled in the UI

3. **Given** a feature toggle
   **When** viewing dependencies
   **Then** related features show dependency warnings if applicable

## Tasks / Subtasks

- [x] Task 1: Create Feature Settings View
  - [x] Create FeatureSettingsView.xaml
  - [x] Create FeatureSettingsViewModel
  - [x] Display toggle switches for each feature
  - [x] Group features by category

- [x] Task 2: Implement Feature Toggle Logic
  - [x] Create IFeatureToggleService interface
  - [x] Implement IsFeatureEnabled(string featureName)
  - [x] Implement SetFeatureEnabled(string, bool)
  - [x] Persist to SystemSettings table

- [x] Task 3: Add Feature Dependencies
  - [x] Define feature dependency map
  - [x] Warn when disabling feature with dependents
  - [x] Auto-enable required features

- [x] Task 4: Integrate Feature Checks in UI
  - [x] Create FeatureVisibilityConverter
  - [x] Hide/show menu items based on features
  - [x] Hide/show UI panels based on features

## Dev Notes

### Feature Categories

| Category | Features |
|----------|----------|
| Restaurant | Table Management, Kitchen Display, Waiter Assignment |
| Supermarket | Barcode Auto-Focus, Product Offers, Supplier Credit |
| HR/Finance | Payroll, Accounting |

### Feature Toggle Service

```csharp
public interface IFeatureToggleService
{
    bool IsFeatureEnabled(string featureName);
    void SetFeatureEnabled(string featureName, bool enabled);
    IEnumerable<FeatureInfo> GetAllFeatures();
    IEnumerable<string> GetDependencies(string featureName);
}
```

### XAML Feature Visibility

```xml
<MenuItem Header="Table Management"
          Visibility="{Binding IsTableManagementEnabled,
                      Converter={StaticResource BoolToVisibility}}"/>
```

## Dev Agent Record

### Agent Model Used
Claude Opus 4.5 (claude-opus-4-5-20251101)

### Completion Notes List
- Created FeatureSettingsViewModel with category-grouped feature toggles
- Created FeatureSettingsView.xaml with modern toggle switch UI
- Features organized into 4 categories: Restaurant, Retail, Enterprise, Kenya Compliance
- Feature dependency system with auto-enable/disable for dependent features
- Required features (like Kenya eTIMS) cannot be disabled
- Reset to mode defaults functionality
- Unsaved changes warning and discard option
- Created FeatureVisibilityConverter for XAML-based feature checks
- Created FeatureEnabledConverter for IsEnabled binding
- Created ModeVisibilityConverter for mode-specific UI elements
- All converters use cached configuration for performance

### File List
- src/HospitalityPOS.WPF/ViewModels/FeatureSettingsViewModel.cs
- src/HospitalityPOS.WPF/Views/FeatureSettingsView.xaml
- src/HospitalityPOS.WPF/Views/FeatureSettingsView.xaml.cs
- src/HospitalityPOS.WPF/Converters/FeatureVisibilityConverter.cs
- src/HospitalityPOS.WPF/Resources/Converters.xaml (updated)
- src/HospitalityPOS.WPF/App.xaml.cs (updated)
