# Story 2.6: Permission Override with PIN

Status: done

## Story

As a manager,
I want to authorize actions for users who lack permission,
So that operations can continue with proper oversight.

## Acceptance Criteria

1. **Given** a user's action is blocked due to missing permission
   **When** the "Request Authorization" option is selected
   **Then** a PIN entry dialog should appear

2. **Given** PIN entry dialog is shown
   **When** an authorized user enters their PIN
   **Then** system validates the authorizing user has required permission

3. **Given** authorization is successful
   **When** the authorizing user has the required permission
   **Then** action should proceed

4. **Given** any permission override occurs
   **When** authorization is complete
   **Then** both the original user and authorizing user should be logged in audit trail

5. **Given** authorization is complete
   **When** the action proceeds
   **Then** the override event should be logged with timestamp and reason

## Tasks / Subtasks

- [x] Task 1: Create Authorization PIN Dialog (AC: #1, #2)
  - [x] Create AuthorizationOverrideDialog.xaml
  - [x] Add numeric keypad for PIN entry
  - [x] Display action requiring authorization
  - [x] Display required permission
  - [x] Add OK, Clear, and Cancel buttons

- [x] Task 2: Create Authorization Dialog code-behind (AC: #2, #3)
  - [x] Create AuthorizationOverrideDialog.xaml.cs
  - [x] Implement numeric keypad handling
  - [x] PIN masking (asterisks display)
  - [x] Return entered PIN on OK

- [x] Task 3: Implement Override Service (AC: #2, #3)
  - [x] Create IPermissionOverrideService interface
  - [x] Create OverrideResult model
  - [x] Implement ValidatePinAndAuthorizeAsync method
  - [x] Validate PIN and permissions
  - [x] Prevent self-authorization
  - [x] Return OverrideResult with authorizing user info

- [x] Task 4: Implement Audit Logging (AC: #4, #5)
  - [x] Log override request to AuditLog table
  - [x] Log original user ID
  - [x] Log authorizing user ID
  - [x] Log action being authorized
  - [x] Log timestamp and success/failure

- [x] Task 5: Integrate with ViewModels
  - [x] Add ShowAuthorizationOverrideAsync to IDialogService
  - [x] Implement in DialogService
  - [x] Create RequirePermissionOrOverrideAsync helper method in ViewModelBase
  - [x] Create RequirePermissionOrOverrideWithRetryAsync for retry functionality
  - [x] Register PermissionOverrideService in DI container

## Dev Notes

### ViewModel Integration Example

```csharp
public partial class ReceiptViewModel : ViewModelBase
{
    [RelayCommand]
    private async Task VoidReceiptAsync()
    {
        // Check permission or request override
        var result = await RequirePermissionOrOverrideAsync(
            PermissionNames.Receipts.Void,
            $"Void Receipt #{SelectedReceipt.ReceiptNumber}");

        if (!result.IsAuthorized)
        {
            // User cancelled or authorization failed
            return;
        }

        // Proceed with void - optionally log who authorized
        await _receiptService.VoidReceiptAsync(
            SelectedReceipt.Id,
            VoidReason,
            authorizedByUserId: result.AuthorizingUserId);
    }
}
```

### Alternative: With Retry

```csharp
[RelayCommand]
private async Task VoidReceiptAsync()
{
    // Check permission with retry on failure
    if (!await RequirePermissionOrOverrideWithRetryAsync(
        PermissionNames.Receipts.Void,
        $"Void Receipt #{SelectedReceipt.ReceiptNumber}"))
    {
        return; // Cancelled or max retries exceeded
    }

    // Proceed with void
    await _receiptService.VoidReceiptAsync(SelectedReceipt.Id, VoidReason);
}
```

### Security Features
- Cannot authorize your own action (different user must authorize)
- PIN validated against all active users with PINs
- Locked accounts cannot authorize
- All authorization attempts logged to audit trail

### Audit Log Entry Example

```json
{
  "UserId": 5,
  "Action": "PermissionOverrideGranted",
  "EntityType": "Permission",
  "NewValues": {
    "Permission": "Receipts.Void",
    "ActionDescription": "Void Receipt #R-0001",
    "AuthorizingUserId": 2,
    "Success": true,
    "Reason": null
  },
  "MachineName": "POS-TERMINAL-01",
  "CreatedAt": "2025-12-30T14:30:00Z"
}
```

### References
- [Source: docs/PRD_Hospitality_POS_System.md#4.4-Permission-Override-Workflow]
- [Source: _bmad-output/architecture.md#5.3-Authorization-Override]

## Dev Agent Record

### Agent Model Used
Claude Opus 4.5 (claude-opus-4-5-20251101)

### Completion Notes List
- Created OverrideResult model with Success/Failure/Cancelled factory methods
- Created IPermissionOverrideService interface with ValidatePinAndAuthorizeAsync and GetUserByPinAsync
- Implemented PermissionOverrideService with PIN validation against all active users
- Added self-authorization prevention (cannot authorize own actions)
- Added locked account check during authorization
- Created AuthorizationOverrideDialog.xaml with touch-optimized numeric keypad
- Created AuthorizationOverrideDialog.xaml.cs with PIN masking
- Added ShowAuthorizationOverrideAsync to IDialogService and DialogService
- Added RequirePermissionOrOverrideAsync helper to ViewModelBase
- Added RequirePermissionOrOverrideWithRetryAsync with 3-attempt retry logic
- Registered IPermissionOverrideService as Singleton in App.xaml.cs
- Full audit logging of all authorization attempts (success and failure)

### File List
- src/HospitalityPOS.Core/Models/OverrideResult.cs (new)
- src/HospitalityPOS.Core/Interfaces/IPermissionOverrideService.cs (new)
- src/HospitalityPOS.Infrastructure/Services/PermissionOverrideService.cs (new)
- src/HospitalityPOS.WPF/Views/Dialogs/AuthorizationOverrideDialog.xaml (new)
- src/HospitalityPOS.WPF/Views/Dialogs/AuthorizationOverrideDialog.xaml.cs (new)
- src/HospitalityPOS.WPF/Services/IDialogService.cs (modified)
- src/HospitalityPOS.WPF/Services/DialogService.cs (modified)
- src/HospitalityPOS.WPF/ViewModels/ViewModelBase.cs (modified)
- src/HospitalityPOS.WPF/App.xaml.cs (modified)

### Change Log
- 2025-12-30: Story implemented - all tasks completed
