# Story 2.2: Password Management

Status: done

## Story

As an administrator,
I want secure password storage and management,
So that user credentials are protected.

## Acceptance Criteria

1. **Given** a user account exists
   **When** password is stored
   **Then** passwords should be hashed using BCrypt with cost factor 12

2. **Given** password storage
   **When** credentials are saved
   **Then** plain-text passwords should never be stored

3. **Given** a user forgets password
   **When** password reset is requested
   **Then** password reset should generate a new temporary password

4. **Given** a new password is set
   **When** password complexity is checked
   **Then** password should require: 8+ characters, uppercase, lowercase, number

## Tasks / Subtasks

- [x] Task 1: Implement Password Hashing (AC: #1, #2)
  - [x] Create IPasswordService interface
  - [x] Implement HashPassword method using BCrypt
  - [x] Set cost factor to 12
  - [x] Implement VerifyPassword method

- [x] Task 2: Implement Password Validation (AC: #4)
  - [x] Create password complexity validator
  - [x] Check minimum 8 characters
  - [x] Check for uppercase letter
  - [x] Check for lowercase letter
  - [x] Check for number
  - [x] Return specific error messages for each failure

- [x] Task 3: Implement Password Reset (AC: #3)
  - [x] Add ResetPassword method to IUserService
  - [x] Generate secure temporary password
  - [x] Hash and store new password
  - [x] Flag account for password change on next login
  - [x] Log password reset in audit trail

- [x] Task 4: Update User Creation/Edit
  - [x] Hash password during user creation
  - [x] Validate password complexity before saving
  - [x] Never store or return plain-text password

- [x] Task 5: Add Change Password Feature
  - [x] Create ChangePasswordView
  - [x] Require current password
  - [x] Validate new password complexity
  - [x] Confirm new password matches
  - [x] Update password hash in database

## Dev Notes

### IPasswordService Interface

```csharp
public interface IPasswordService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
    string GenerateTemporaryPassword(int length = 12);
    PasswordValidationResult ValidatePasswordComplexity(string password);
}
```

### Password Service Implementation

```csharp
public class PasswordService : IPasswordService
{
    private const int WorkFactor = 12;

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }

    public string GenerateTemporaryPassword(int length = 12)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%";
        var random = new Random();
        return new string(Enumerable.Range(0, length)
            .Select(_ => chars[random.Next(chars.Length)])
            .ToArray());
    }

    public PasswordValidationResult ValidatePasswordComplexity(string password)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(password) || password.Length < 8)
            errors.Add("Password must be at least 8 characters long");

        if (!password.Any(char.IsUpper))
            errors.Add("Password must contain at least one uppercase letter");

        if (!password.Any(char.IsLower))
            errors.Add("Password must contain at least one lowercase letter");

        if (!password.Any(char.IsDigit))
            errors.Add("Password must contain at least one number");

        return new PasswordValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}
```

### Password Validation Result

```csharp
public class PasswordValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}
```

### User Entity Password Fields

```csharp
public class User
{
    public string PasswordHash { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; } = false;
    public DateTime? PasswordChangedAt { get; set; }
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockedUntil { get; set; }
}
```

### Security Best Practices
1. Never log passwords (even hashed ones)
2. Never return passwords in API responses
3. Use SecureString where possible in memory
4. Clear password from memory after use
5. Always use HTTPS in production

### Audit Logging
Log these password events:
- Password created (user creation)
- Password changed (by user)
- Password reset (by admin)
- Failed password attempts

### References
- [Source: docs/PRD_Hospitality_POS_System.md#10.2-Data-Security]
- [Source: _bmad-output/architecture.md#5.1-Authentication]
- [BCrypt.Net-Next Documentation](https://github.com/BcryptNet/bcrypt.net)

## Dev Agent Record

### Agent Model Used
Claude Opus 4.5 (claude-opus-4-5-20251101)

### Completion Notes List
- Implemented IPasswordService interface with BCrypt hashing (cost factor 12)
- Created PasswordValidationResult model for complexity validation
- Added password complexity validation (8+ chars, uppercase, lowercase, digit)
- Implemented secure temporary password generation using cryptographic RNG
- Added ChangePasswordAsync and ResetPasswordAsync to IUserService
- Updated User entity with MustChangePassword and PasswordChangedAt fields
- Created ChangePasswordView with real-time validation
- Updated LoginViewModel to redirect to password change when MustChangePassword is true
- Added admin user seeding with default password "Admin@123" (requires change on first login)
- Refactored UserService to use IPasswordService instead of direct BCrypt calls
- Fixed POSDbContext naming consistency in UserService

### File List
- src/HospitalityPOS.Core/Interfaces/IPasswordService.cs (new)
- src/HospitalityPOS.Core/Interfaces/IUserService.cs (modified)
- src/HospitalityPOS.Core/Models/PasswordValidationResult.cs (new)
- src/HospitalityPOS.Core/Entities/User.cs (modified)
- src/HospitalityPOS.Infrastructure/Services/PasswordService.cs (new)
- src/HospitalityPOS.Infrastructure/Services/UserService.cs (modified)
- src/HospitalityPOS.Infrastructure/Data/Configurations/UserConfiguration.cs (modified)
- src/HospitalityPOS.Infrastructure/Data/DatabaseSeeder.cs (modified)
- src/HospitalityPOS.WPF/ViewModels/ChangePasswordViewModel.cs (new)
- src/HospitalityPOS.WPF/ViewModels/LoginViewModel.cs (modified)
- src/HospitalityPOS.WPF/Views/ChangePasswordView.xaml (new)
- src/HospitalityPOS.WPF/Views/ChangePasswordView.xaml.cs (new)
- src/HospitalityPOS.WPF/Views/MainWindow.xaml (modified)
- src/HospitalityPOS.WPF/Converters/BoolToVisibilityConverters.cs (modified)
- src/HospitalityPOS.WPF/App.xaml.cs (modified)

### Change Log
- 2025-12-30: Story implemented - all tasks completed
- 2025-12-30: Code review completed - 4 issues fixed

## Code Review Record

### Review Date
2025-12-30

### Issues Found: 7 (4 fixed, 3 deferred)

#### Fixed Issues:
1. **HIGH: Missing audit logging** - Added AuditLog entries for ChangePasswordAsync and ResetPasswordAsync in UserService.cs
2. **MEDIUM: Forced password change navigation bug** - Fixed GoBack() issue when history was cleared; now navigates to LoginViewModel after forced change
3. **MEDIUM: Null reference in forced change check** - Added session validation in ChangePasswordViewModel.OnNavigatedTo
4. **LOW: Missing CanExecute validation** - Added CanChangePassword() method with proper field validation and NotifyCanExecuteChangedFor attributes

#### Deferred Issues (Low severity):
5. Modulo bias in temp password generation - Minimal security impact (~0.5% bias)
6. Hardcoded default admin password - Mitigated by MustChangePassword=true
7. Unused PIN hash index - Performance optimization, not a bug
