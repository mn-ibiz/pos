using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Models;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for user authentication and management operations.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Authenticates a user by username and password.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="password">The plaintext password.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authenticated user or null if authentication fails.</returns>
    Task<User?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Authenticates a user by PIN.
    /// </summary>
    /// <param name="pin">The plaintext PIN.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authenticated user or null if authentication fails.</returns>
    Task<User?> AuthenticateByPinAsync(string pin, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an account is currently locked.
    /// </summary>
    /// <param name="username">The username to check.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the account is locked; otherwise, false.</returns>
    Task<bool> IsAccountLockedAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the remaining lockout time for a locked account.
    /// </summary>
    /// <param name="username">The username to check.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The remaining lockout time, or TimeSpan.Zero if not locked.</returns>
    Task<TimeSpan> GetLockoutRemainingTimeAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a failed login attempt and potentially locks the account.
    /// </summary>
    /// <param name="username">The username that failed to authenticate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task RecordFailedAttemptAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the failed login attempt counter for a user.
    /// </summary>
    /// <param name="username">The username to reset.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task ResetFailedAttemptsAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all users for the quick login avatar display.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of active users with their display info.</returns>
    Task<IReadOnlyList<User>> GetActiveUsersForQuickLoginAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by their ID.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The user or null if not found.</returns>
    Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by their username.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The user or null if not found.</returns>
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes a user's password after verifying the current password.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="currentPassword">The current password for verification.</param>
    /// <param name="newPassword">The new password to set.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or the validation errors.</returns>
    Task<PasswordChangeResult> ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets a user's password (admin operation) and generates a temporary password.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="adminUserId">The admin user ID performing the reset.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The temporary password if successful, or null if the user was not found.</returns>
    Task<string?> ResetPasswordAsync(int userId, int adminUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a password against complexity requirements.
    /// </summary>
    /// <param name="password">The password to validate.</param>
    /// <returns>The validation result.</returns>
    PasswordValidationResult ValidatePassword(string password);

    // ========== User Management Methods (Story 2.4) ==========

    /// <summary>
    /// Gets all users in the system.
    /// </summary>
    /// <param name="includeInactive">Whether to include inactive users.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of all users.</returns>
    Task<IReadOnlyList<User>> GetAllUsersAsync(bool includeInactive = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new user.
    /// </summary>
    /// <param name="dto">The user creation data.</param>
    /// <param name="adminUserId">The ID of the admin creating the user.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created user.</returns>
    Task<User> CreateUserAsync(CreateUserDto dto, int adminUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing user.
    /// </summary>
    /// <param name="userId">The user ID to update.</param>
    /// <param name="dto">The user update data.</param>
    /// <param name="adminUserId">The ID of the admin performing the update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if successful; false if user not found.</returns>
    Task<bool> UpdateUserAsync(int userId, UpdateUserDto dto, int adminUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a user account.
    /// </summary>
    /// <param name="userId">The user ID to activate.</param>
    /// <param name="adminUserId">The ID of the admin performing the operation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if successful; false if user not found.</returns>
    Task<bool> ActivateUserAsync(int userId, int adminUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a user account.
    /// </summary>
    /// <param name="userId">The user ID to deactivate.</param>
    /// <param name="adminUserId">The ID of the admin performing the operation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if successful; false if user not found or is the only admin.</returns>
    Task<bool> DeactivateUserAsync(int userId, int adminUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets or updates a user's PIN.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="pin">The new PIN (4-6 digits) or null to remove.</param>
    /// <param name="adminUserId">The ID of the admin performing the operation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if successful; false if user not found.</returns>
    Task<bool> SetPinAsync(int userId, string? pin, int adminUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns roles to a user, replacing any existing roles.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="roleIds">The role IDs to assign.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if successful; false if user not found.</returns>
    Task<bool> AssignRolesAsync(int userId, IEnumerable<int> roleIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a username is unique.
    /// </summary>
    /// <param name="username">The username to check.</param>
    /// <param name="excludeUserId">Optional user ID to exclude from the check.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the username is unique.</returns>
    Task<bool> IsUsernameUniqueAsync(string username, int? excludeUserId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a PIN is unique across all users.
    /// </summary>
    /// <param name="pin">The PIN to check.</param>
    /// <param name="excludeUserId">Optional user ID to exclude from the check.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the PIN is unique.</returns>
    Task<bool> IsPinUniqueAsync(string pin, int? excludeUserId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a PIN format (4-6 digits).
    /// </summary>
    /// <param name="pin">The PIN to validate.</param>
    /// <returns>True if the PIN is valid format.</returns>
    bool ValidatePinFormat(string? pin);
}

/// <summary>
/// Represents the result of a password change operation.
/// </summary>
public class PasswordChangeResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the password change was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the password validation errors if complexity requirements failed.
    /// </summary>
    public List<string> ValidationErrors { get; set; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static PasswordChangeResult Success() => new() { IsSuccess = true };

    /// <summary>
    /// Creates a failure result with an error message.
    /// </summary>
    public static PasswordChangeResult Failure(string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage
    };

    /// <summary>
    /// Creates a failure result with validation errors.
    /// </summary>
    public static PasswordChangeResult ValidationFailure(IEnumerable<string> errors) => new()
    {
        IsSuccess = false,
        ErrorMessage = "Password does not meet complexity requirements",
        ValidationErrors = errors.ToList()
    };
}
