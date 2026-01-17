# Story 2.5: Permission Checking

Status: done

## Story

As the system,
I want to enforce permission checks on all protected actions,
So that users can only perform actions they are authorized for.

## Acceptance Criteria

1. **Given** a user is logged in
   **When** attempting any protected action
   **Then** system should check if user's role(s) have the required permission

2. **Given** permission check is performed
   **When** user has the required permission
   **Then** action should proceed

3. **Given** permission check is performed
   **When** user lacks the required permission
   **Then** action should be blocked with "Unauthorized" message

4. **Given** any permission check occurs
   **When** the check completes
   **Then** permission checks should be logged to audit trail

## Tasks / Subtasks

- [x] Task 1: Create Authorization Service (AC: #1, #2, #3)
  - [x] Create IAuthorizationService interface
  - [x] Implement HasPermission method
  - [x] Implement HasAnyPermission method
  - [x] Implement HasAllPermissions method
  - [x] Cache user permissions for performance

- [x] Task 2: Create Permission Constants (AC: #1)
  - [x] Create PermissionNames static class with all permission constants
  - [x] Organize by category (WorkPeriod, Sales, Receipts, Products, etc.)
  - [x] Match constants to database-seeded permission names

- [x] Task 3: Implement Permission Guard in ViewModels (AC: #2, #3)
  - [x] Add AuthorizationService access to ViewModelBase
  - [x] Create HasPermission/HasAnyPermission/HasAllPermissions helper methods
  - [x] Create RequirePermission method with error message display
  - [x] Create CheckPermission for CanExecute integration

- [x] Task 4: Implement Audit Logging (AC: #4)
  - [x] Log permission denials to AuditLog table
  - [x] Include: user, permission, result
  - [x] Log timestamp and machine name

- [x] Task 5: Create UI Permission Binding
  - [x] Create PermissionToVisibilityConverter
  - [x] Create AnyPermissionToVisibilityConverter
  - [x] Create PermissionToEnabledConverter
  - [x] Create PermissionAndBindingEnabledConverter (multi-value)
  - [x] Register converters in App.xaml ResourceDictionary

## Dev Notes

### Permission Constants Usage

```csharp
// Use constants from PermissionNames class
using HospitalityPOS.Core.Constants;

// Check single permission
if (HasPermission(PermissionNames.Users.Create))
{
    // Create user
}

// Check any permission
if (HasAnyPermission(PermissionNames.Sales.ViewOwn, PermissionNames.Sales.ViewAll))
{
    // View sales
}
```

### ViewModel Integration Example

```csharp
public partial class UserManagementViewModel : ViewModelBase
{
    [RelayCommand(CanExecute = nameof(CanCreateUser))]
    private async Task CreateUserAsync()
    {
        if (!RequirePermission(PermissionNames.Users.Create, "create users"))
            return;

        // Action proceeds
    }

    private bool CanCreateUser()
    {
        return CheckPermission(PermissionNames.Users.Create);
    }
}
```

### XAML Permission Binding Examples

```xml
<!-- Hide button if no permission -->
<Button Content="Void Receipt"
        Visibility="{Binding Converter={StaticResource PermissionToVisibilityConverter},
                     ConverterParameter=Receipts.Void}" />

<!-- Show if ANY permission matches -->
<StackPanel Visibility="{Binding Converter={StaticResource AnyPermissionToVisibilityConverter},
                         ConverterParameter='Sales.ViewOwn,Sales.ViewAll'}" />

<!-- Disable without permission -->
<Button Content="Delete"
        IsEnabled="{Binding Converter={StaticResource PermissionToEnabledConverter},
                    ConverterParameter=Products.Delete}" />

<!-- Combine data binding and permission -->
<Button Content="Submit">
    <Button.IsEnabled>
        <MultiBinding Converter="{StaticResource PermissionAndBindingEnabledConverter}"
                      ConverterParameter="Sales.Create">
            <Binding Path="CanSubmit" />
        </MultiBinding>
    </Button.IsEnabled>
</Button>
```

### AuthorizationService Caching

- Permissions are cached per user ID
- Cache is cleared on RefreshPermissions() call
- Primary source: session user's loaded roles (avoids DB query)
- Fallback: database query using IServiceScopeFactory

### References
- [Source: docs/PRD_Hospitality_POS_System.md#4-User-Roles]
- [Source: _bmad-output/architecture.md#5.2-RBAC]

## Dev Agent Record

### Agent Model Used
Claude Opus 4.5 (claude-opus-4-5-20251101)

### Completion Notes List
- Created PermissionNames static class with all permission constants organized by category
- Created IAuthorizationService interface with HasPermission, HasAnyPermission, HasAllPermissions, GetCurrentUserPermissions, RefreshPermissions, and CanApplyDiscount methods
- Implemented AuthorizationService with permission caching and IServiceScopeFactory for database access
- Added audit logging for permission denials (fire-and-forget to avoid blocking)
- Created PermissionToVisibilityConverter for hiding UI elements
- Created AnyPermissionToVisibilityConverter for OR-based permission checks
- Created PermissionToEnabledConverter for disabling UI elements
- Created PermissionAndBindingEnabledConverter for combining data binding with permissions
- Added Converters.xaml ResourceDictionary with all converters
- Updated ViewModelBase with permission helper methods (HasPermission, CheckPermission, RequirePermission)
- Registered IAuthorizationService as Singleton in App.xaml.cs
- Used IServiceScopeFactory pattern to avoid captive dependency on scoped DbContext

### File List
- src/HospitalityPOS.Core/Constants/PermissionNames.cs (new)
- src/HospitalityPOS.Core/Interfaces/IAuthorizationService.cs (new)
- src/HospitalityPOS.Infrastructure/Services/AuthorizationService.cs (new)
- src/HospitalityPOS.WPF/Converters/PermissionConverters.cs (new)
- src/HospitalityPOS.WPF/Resources/Converters.xaml (new)
- src/HospitalityPOS.WPF/ViewModels/ViewModelBase.cs (modified)
- src/HospitalityPOS.WPF/App.xaml (modified)
- src/HospitalityPOS.WPF/App.xaml.cs (modified)

### Code Review Results
**Review Date:** 2025-12-30
**Issues Found:** 3 (0 CRITICAL, 0 MEDIUM, 3 LOW)
**Issues Fixed:** 0
**Issues Deferred:** 3 (all acceptable patterns)

| Issue | Severity | Description | Resolution |
|-------|----------|-------------|------------|
| #1 | LOW | Fire-and-forget Task.Run in LogPermissionCheck for audit logging | ACCEPTABLE - Has proper try-catch in CreateAuditLogAsync |
| #2 | LOW | Static AuthorizationService access in ViewModelBase via App.Services | ACCEPTABLE - Standard WPF DI pattern for singleton access |
| #3 | LOW | PermissionConverters lazy load from App.Services | ACCEPTABLE - XAML resources are loaded after app initialization |

**Additional Notes:**
- Permission caching by userId correctly auto-invalidates when different user logs in
- PermissionNames constants verified to match all 32 database-seeded permissions
- ISessionService.HasPermission duplicates IAuthorizationService - pre-existing code, out of scope

### Change Log
- 2025-12-30: Story implemented - all tasks completed
- 2025-12-30: Code review completed - 3 LOW issues deferred (all acceptable patterns)
