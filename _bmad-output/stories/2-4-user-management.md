# Story 2.4: User Management

Status: done

## Story

As an administrator,
I want to create and manage user accounts,
So that staff members can access the system.

## Acceptance Criteria

1. **Given** the admin is logged in
   **When** accessing user management
   **Then** admin can create new users with: username, password, full name, email, phone

2. **Given** a user is created
   **When** roles are configured
   **Then** admin can assign one or more roles to users

3. **Given** users exist
   **When** managing user status
   **Then** admin can activate/deactivate user accounts

4. **Given** a user needs password help
   **When** password reset is requested
   **Then** admin can reset user passwords

5. **Given** a user account exists
   **When** PIN is needed for quick login
   **Then** admin can set/change user PINs

## Tasks / Subtasks

- [x] Task 1: Create User List View (AC: #1, #3)
  - [x] Create UserManagementView.xaml
  - [x] Display DataGrid with users (username, full name, roles, status)
  - [x] Add search/filter functionality
  - [x] Add Create, Edit, Deactivate buttons
  - [x] Show active/inactive status with color coding

- [x] Task 2: Create User Editor View (AC: #1, #2, #5)
  - [x] Create UserEditorView.xaml
  - [x] Add fields: Username, Password, Full Name, Email, Phone
  - [x] Add role selection (multi-select ListBox or CheckBoxes)
  - [x] Add PIN entry field
  - [x] Add IsActive toggle
  - [x] Add Save and Cancel buttons

- [x] Task 3: Implement User Service (AC: #1-5)
  - [x] Create IUserService interface (extend from Story 2.1)
  - [x] Implement GetAllUsersAsync
  - [x] Implement CreateUserAsync with password hashing
  - [x] Implement UpdateUserAsync
  - [x] Implement ResetPasswordAsync
  - [x] Implement SetPinAsync
  - [x] Implement ActivateDeactivateAsync

- [x] Task 4: Create User Management ViewModel
  - [x] Implement UserManagementViewModel
  - [x] Load users on initialization
  - [x] Implement search/filter
  - [x] Implement CRUD commands
  - [x] Handle validation

- [x] Task 5: Create User Editor ViewModel
  - [x] Implement UserEditorViewModel
  - [x] Load roles for selection
  - [x] Validate all fields
  - [x] Handle password complexity
  - [x] Save user with roles

## Dev Notes

### User List Layout

```
+--------------------------------------------------+
|  User Management                    [+ New User] |
+--------------------------------------------------+
|  Search: [________________] [Filter: Active ▼]   |
+--------------------------------------------------+
|  Username   | Full Name    | Roles     | Status  |
|-------------|--------------|-----------|---------|
|  admin      | Administrator| Admin     | Active  |
|  jsmith     | John Smith   | Manager   | Active  |
|  mwilson    | Mary Wilson  | Cashier   | Active  |
|  tpatel     | Tom Patel    | Waiter    | Inactive|
+--------------------------------------------------+
|  [Edit] [Reset Password] [Deactivate]            |
+--------------------------------------------------+
```

### User Editor Layout

```
+------------------------------------------+
|  User Editor                             |
+------------------------------------------+
|  Username*: [____________________]       |
|  Password*: [____________________]       |
|  Confirm:   [____________________]       |
|  Full Name*:[____________________]       |
|  Email:     [____________________]       |
|  Phone:     [____________________]       |
|  PIN:       [______]                     |
|                                          |
|  Roles*:                                 |
|  +------------------------------------+  |
|  | [ ] Administrator                  |  |
|  | [x] Manager                        |  |
|  | [ ] Supervisor                     |  |
|  | [ ] Cashier                        |  |
|  | [ ] Waiter                         |  |
|  +------------------------------------+  |
|                                          |
|  [x] Active                              |
|                                          |
|  [Save]  [Cancel]                        |
+------------------------------------------+
```

### IUserService Interface (Extended)

```csharp
public interface IUserService
{
    // Authentication (from Story 2.1)
    Task<User?> AuthenticateAsync(string username, string password);
    Task<User?> AuthenticateByPinAsync(string pin);

    // User CRUD
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task<User?> GetUserByIdAsync(int id);
    Task<User?> GetUserByUsernameAsync(string username);
    Task<User> CreateUserAsync(CreateUserDto dto);
    Task UpdateUserAsync(int id, UpdateUserDto dto);
    Task<bool> DeactivateUserAsync(int id);
    Task<bool> ActivateUserAsync(int id);

    // Password and PIN
    Task ResetPasswordAsync(int userId, string newPassword);
    Task SetPinAsync(int userId, string pin);

    // Roles
    Task AssignRolesAsync(int userId, IEnumerable<int> roleIds);
}
```

### CreateUserDto

```csharp
public class CreateUserDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? PIN { get; set; }
    public List<int> RoleIds { get; set; } = new();
}
```

### Validation Rules
- Username: required, unique, 3-50 characters, alphanumeric only
- Password: required for new users, must pass complexity check
- Full Name: required, 2-100 characters
- Email: optional, valid email format
- Phone: optional, valid phone format
- PIN: optional, 4-6 digits only
- At least one role must be assigned

### Security Considerations
- Only Administrators can manage users
- Managers can only create Cashiers and Waiters
- Users cannot deactivate their own account
- Password reset generates temporary password requiring change on next login

### Audit Logging
Log these events:
- User created
- User updated
- User activated/deactivated
- Password reset
- PIN changed
- Roles changed

### References
- [Source: docs/PRD_Hospitality_POS_System.md#4.2-Role-Definitions]
- [Source: _bmad-output/architecture.md#5-Security-Architecture]

## Dev Agent Record

### Agent Model Used
Claude Opus 4.5 (claude-opus-4-5-20251101)

### Completion Notes List
- Extended IUserService interface with user management methods (GetAllUsersAsync, CreateUserAsync, UpdateUserAsync, etc.)
- Implemented full user CRUD operations in UserService
- Added PIN validation and uniqueness checking (since PINs are hashed, need to verify against all users)
- Added audit logging for all user management operations
- Created CreateUserDto and UpdateUserDto for data transfer
- Created UserManagementViewModel with search/filter, CRUD commands
- Created UserEditorViewModel with role selection and validation
- Created touch-optimized UserManagementView with DataGrid
- Created UserEditorView with two-panel layout (details + roles)
- Added username validation (alphanumeric + underscore only)
- Added protection against deactivating last admin account
- Added protection against self-deactivation
- New users must change password on first login
- Registered ViewModels in App.xaml.cs
- Added DataTemplates in MainWindow.xaml

### File List
- src/HospitalityPOS.Core/DTOs/CreateUserDto.cs (new)
- src/HospitalityPOS.Core/DTOs/UpdateUserDto.cs (new)
- src/HospitalityPOS.Core/Interfaces/IUserService.cs (modified)
- src/HospitalityPOS.Infrastructure/Services/UserService.cs (modified)
- src/HospitalityPOS.WPF/ViewModels/UserManagementViewModel.cs (new)
- src/HospitalityPOS.WPF/ViewModels/UserEditorViewModel.cs (new)
- src/HospitalityPOS.WPF/Views/UserManagementView.xaml (new)
- src/HospitalityPOS.WPF/Views/UserManagementView.xaml.cs (new)
- src/HospitalityPOS.WPF/Views/UserEditorView.xaml (new)
- src/HospitalityPOS.WPF/Views/UserEditorView.xaml.cs (new)
- src/HospitalityPOS.WPF/Views/MainWindow.xaml (modified)
- src/HospitalityPOS.WPF/App.xaml.cs (modified)

### Code Review Results
**Review Date:** 2025-12-30
**Issues Found:** 3 (1 MEDIUM, 2 LOW)
**Issues Fixed:** 2
**Issues Deferred:** 1

| Issue | Severity | Description | Resolution |
|-------|----------|-------------|------------|
| #1 | MEDIUM | UpdateUserAsync bypasses last-admin protection when setting IsActive=false | FIXED - Added admin protection check to UpdateUserAsync |
| #2 | LOW | CanSave doesn't check IsLoading (double-submit risk) | FIXED - Added IsLoading check and NotifyCanExecuteChangedFor |
| #3 | LOW | Fire-and-forget async in OnNavigatedTo | DEFERRED - Standard WPF MVVM pattern |

### Change Log
- 2025-12-30: Story implemented - all tasks completed
- 2025-12-30: Code review completed - 2 issues fixed (admin protection, double-submit prevention)
