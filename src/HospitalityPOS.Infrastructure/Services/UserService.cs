using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Serilog;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// User service implementation for authentication and user management.
/// </summary>
public partial class UserService : IUserService
{
    private readonly POSDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly ILogger _logger;

    /// <summary>
    /// Maximum number of failed attempts before lockout.
    /// </summary>
    private const int MaxFailedAttempts = 5;

    /// <summary>
    /// Lockout duration in minutes.
    /// </summary>
    private const int LockoutDurationMinutes = 15;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="passwordService">The password service.</param>
    /// <param name="logger">The logger.</param>
    public UserService(POSDbContext context, IPasswordService passwordService, ILogger logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<User?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        _logger.Debug("Attempting authentication for user: {Username}", username);

        // Check if account is locked
        if (await IsAccountLockedAsync(username, cancellationToken).ConfigureAwait(false))
        {
            _logger.Warning("Login attempt for locked account: {Username}", username);
            return null;
        }

        // Get the user with roles
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            _logger.Warning("Login failed - user not found: {Username}", username);
            await RecordFailedAttemptAsync(username, cancellationToken).ConfigureAwait(false);
            return null;
        }

        // Verify password using password service
        if (!_passwordService.VerifyPassword(password, user.PasswordHash))
        {
            _logger.Warning("Login failed - invalid password for user: {Username}", username);
            await RecordFailedAttemptAsync(username, cancellationToken).ConfigureAwait(false);
            return null;
        }

        // Successful login - reset failed attempts and update last login
        await ResetFailedAttemptsAsync(username, cancellationToken).ConfigureAwait(false);
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("User authenticated successfully: {Username}", username);
        return user;
    }

    /// <inheritdoc />
    public async Task<User?> AuthenticateByPinAsync(string pin, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pin);

        _logger.Debug("Attempting PIN authentication");

        // Get all active users with PINs (we need to verify against all since PIN hash includes salt)
        var usersWithPins = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .Where(u => u.IsActive && u.PINHash != null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var user in usersWithPins)
        {
            // Skip locked accounts
            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
            {
                continue;
            }

            if (_passwordService.VerifyPin(pin, user.PINHash!))
            {
                // Successful PIN login
                await ResetFailedAttemptsAsync(user.Username, cancellationToken).ConfigureAwait(false);
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                _logger.Information("User authenticated by PIN: {Username}", user.Username);
                return user;
            }
        }

        _logger.Warning("PIN authentication failed - no matching PIN found");
        return null;
    }

    /// <inheritdoc />
    public async Task<bool> IsAccountLockedAsync(string username, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            return false;
        }

        return user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow;
    }

    /// <inheritdoc />
    public async Task<TimeSpan> GetLockoutRemainingTimeAsync(string username, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken)
            .ConfigureAwait(false);

        if (user?.LockoutEnd is null || user.LockoutEnd.Value <= DateTime.UtcNow)
        {
            return TimeSpan.Zero;
        }

        return user.LockoutEnd.Value - DateTime.UtcNow;
    }

    /// <inheritdoc />
    public async Task RecordFailedAttemptAsync(string username, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            // Don't reveal that the user doesn't exist
            return;
        }

        user.FailedLoginAttempts++;

        if (user.FailedLoginAttempts >= MaxFailedAttempts)
        {
            user.LockoutEnd = DateTime.UtcNow.AddMinutes(LockoutDurationMinutes);
            _logger.Warning("Account locked due to {Attempts} failed attempts: {Username}",
                user.FailedLoginAttempts, username);
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ResetFailedAttemptsAsync(string username, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            return;
        }

        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<User>> GetActiveUsersForQuickLoginAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.PINHash != null)
            .OrderBy(u => u.FullName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        return await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PasswordChangeResult> ChangePasswordAsync(
        int userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentPassword);
        ArgumentException.ThrowIfNullOrWhiteSpace(newPassword);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            _logger.Warning("Password change failed - user not found: {UserId}", userId);
            return PasswordChangeResult.Failure("User not found");
        }

        // Verify current password
        if (!_passwordService.VerifyPassword(currentPassword, user.PasswordHash))
        {
            _logger.Warning("Password change failed - invalid current password for user: {Username}", user.Username);
            return PasswordChangeResult.Failure("Current password is incorrect");
        }

        // Validate new password complexity
        var validationResult = _passwordService.ValidatePasswordComplexity(newPassword);
        if (!validationResult.IsValid)
        {
            return PasswordChangeResult.ValidationFailure(validationResult.Errors);
        }

        // Check that new password is different from current
        if (_passwordService.VerifyPassword(newPassword, user.PasswordHash))
        {
            return PasswordChangeResult.Failure("New password must be different from current password");
        }

        // Update password
        user.PasswordHash = _passwordService.HashPassword(newPassword);
        user.PasswordChangedAt = DateTime.UtcNow;
        user.MustChangePassword = false;

        // Create audit log entry for password change
        var auditLog = new AuditLog
        {
            UserId = userId,
            Action = "PasswordChanged",
            EntityType = nameof(User),
            EntityId = userId,
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Password changed successfully for user: {Username}", user.Username);
        return PasswordChangeResult.Success();
    }

    /// <inheritdoc />
    public async Task<string?> ResetPasswordAsync(int userId, int adminUserId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            _logger.Warning("Password reset failed - user not found: {UserId}", userId);
            return null;
        }

        // Generate temporary password
        var temporaryPassword = _passwordService.GenerateTemporaryPassword();

        // Update password
        user.PasswordHash = _passwordService.HashPassword(temporaryPassword);
        user.PasswordChangedAt = DateTime.UtcNow;
        user.MustChangePassword = true;

        // Reset lockout
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;

        // Create audit log entry for password reset
        var auditLog = new AuditLog
        {
            UserId = adminUserId,
            Action = "PasswordReset",
            EntityType = nameof(User),
            EntityId = userId,
            NewValues = $"{{\"TargetUser\": \"{user.Username}\", \"ResetBy\": {adminUserId}}}",
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Password reset by admin {AdminId} for user: {Username}", adminUserId, user.Username);

        // Return temporary password (will be displayed to admin)
        return temporaryPassword;
    }

    /// <inheritdoc />
    public PasswordValidationResult ValidatePassword(string password)
    {
        return _passwordService.ValidatePasswordComplexity(password);
    }

    // ========== User Management Methods (Story 2.4) ==========

    /// <inheritdoc />
    public async Task<IReadOnlyList<User>> GetAllUsersAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role);

        if (!includeInactive)
        {
            query = query.Where(u => u.IsActive);
        }

        return await query
            .OrderBy(u => u.FullName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<User> CreateUserAsync(CreateUserDto dto, int adminUserId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.Username);
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.Password);
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.FullName);

        // Validate unique username
        if (!await IsUsernameUniqueAsync(dto.Username, null, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException($"Username '{dto.Username}' is already taken.");
        }

        // Validate password complexity
        var passwordValidation = _passwordService.ValidatePasswordComplexity(dto.Password);
        if (!passwordValidation.IsValid)
        {
            throw new InvalidOperationException($"Password does not meet complexity requirements: {string.Join(", ", passwordValidation.Errors)}");
        }

        // Validate PIN if provided
        if (!string.IsNullOrWhiteSpace(dto.PIN))
        {
            if (!ValidatePinFormat(dto.PIN))
            {
                throw new InvalidOperationException("PIN must be 4-6 digits.");
            }

            if (!await IsPinUniqueAsync(dto.PIN, null, cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidOperationException("This PIN is already in use by another user.");
            }
        }

        // Validate at least one role
        if (dto.RoleIds.Count == 0)
        {
            throw new InvalidOperationException("At least one role must be assigned.");
        }

        var user = new User
        {
            Username = dto.Username.Trim().ToLowerInvariant(),
            PasswordHash = _passwordService.HashPassword(dto.Password),
            FullName = dto.FullName.Trim(),
            Email = dto.Email?.Trim(),
            Phone = dto.Phone?.Trim(),
            PINHash = string.IsNullOrWhiteSpace(dto.PIN) ? null : _passwordService.HashPin(dto.PIN),
            IsActive = dto.IsActive,
            MustChangePassword = true, // New users must change password on first login
            PasswordChangedAt = DateTime.UtcNow
        };

        await _context.Users.AddAsync(user, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Assign roles
        await AssignRolesAsync(user.Id, dto.RoleIds, cancellationToken).ConfigureAwait(false);

        // Audit log
        var auditLog = new AuditLog
        {
            UserId = adminUserId,
            Action = "UserCreated",
            EntityType = nameof(User),
            EntityId = user.Id,
            NewValues = $"{{\"Username\": \"{user.Username}\", \"FullName\": \"{user.FullName}\", \"RoleCount\": {dto.RoleIds.Count}}}",
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("User created: {Username} by admin {AdminId}", user.Username, adminUserId);

        return user;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateUserAsync(int userId, UpdateUserDto dto, int adminUserId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.FullName);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            _logger.Warning("User not found for update: {UserId}", userId);
            return false;
        }

        // Validate at least one role
        if (dto.RoleIds.Count == 0)
        {
            throw new InvalidOperationException("At least one role must be assigned.");
        }

        // Check if deactivating the last admin via UpdateUserAsync
        if (user.IsActive && !dto.IsActive)
        {
            // Cannot deactivate yourself
            if (userId == adminUserId)
            {
                throw new InvalidOperationException("You cannot deactivate your own account.");
            }

            // Check if this is the last active admin
            var userWithRoles = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                .ConfigureAwait(false);

            var isAdmin = userWithRoles?.UserRoles.Any(ur => ur.Role.Name == "Administrator") ?? false;
            if (isAdmin)
            {
                var activeAdminCount = await _context.Users
                    .Where(u => u.IsActive && u.Id != userId)
                    .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "Administrator"))
                    .CountAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (activeAdminCount == 0)
                {
                    throw new InvalidOperationException("Cannot deactivate the last administrator account.");
                }
            }
        }

        var oldValues = $"{{\"FullName\": \"{user.FullName}\", \"IsActive\": {user.IsActive.ToString().ToLowerInvariant()}}}";

        // Update properties
        user.FullName = dto.FullName.Trim();
        user.Email = dto.Email?.Trim();
        user.Phone = dto.Phone?.Trim();
        user.IsActive = dto.IsActive;

        // Update roles
        await AssignRolesAsync(userId, dto.RoleIds, cancellationToken).ConfigureAwait(false);

        // Audit log
        var auditLog = new AuditLog
        {
            UserId = adminUserId,
            Action = "UserUpdated",
            EntityType = nameof(User),
            EntityId = userId,
            OldValues = oldValues,
            NewValues = $"{{\"FullName\": \"{user.FullName}\", \"IsActive\": {user.IsActive.ToString().ToLowerInvariant()}, \"RoleCount\": {dto.RoleIds.Count}}}",
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("User updated: {Username} by admin {AdminId}", user.Username, adminUserId);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ActivateUserAsync(int userId, int adminUserId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            _logger.Warning("User not found for activation: {UserId}", userId);
            return false;
        }

        if (user.IsActive)
        {
            return true; // Already active
        }

        user.IsActive = true;
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;

        // Audit log
        var auditLog = new AuditLog
        {
            UserId = adminUserId,
            Action = "UserActivated",
            EntityType = nameof(User),
            EntityId = userId,
            NewValues = $"{{\"Username\": \"{user.Username}\"}}",
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("User activated: {Username} by admin {AdminId}", user.Username, adminUserId);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeactivateUserAsync(int userId, int adminUserId, CancellationToken cancellationToken = default)
    {
        // Cannot deactivate yourself
        if (userId == adminUserId)
        {
            _logger.Warning("User attempted to deactivate their own account: {UserId}", userId);
            throw new InvalidOperationException("You cannot deactivate your own account.");
        }

        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            _logger.Warning("User not found for deactivation: {UserId}", userId);
            return false;
        }

        if (!user.IsActive)
        {
            return true; // Already inactive
        }

        // Check if this is the last active admin
        var isAdmin = user.UserRoles.Any(ur => ur.Role.Name == "Administrator");
        if (isAdmin)
        {
            var activeAdminCount = await _context.Users
                .Where(u => u.IsActive && u.Id != userId)
                .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "Administrator"))
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);

            if (activeAdminCount == 0)
            {
                throw new InvalidOperationException("Cannot deactivate the last administrator account.");
            }
        }

        user.IsActive = false;

        // Audit log
        var auditLog = new AuditLog
        {
            UserId = adminUserId,
            Action = "UserDeactivated",
            EntityType = nameof(User),
            EntityId = userId,
            NewValues = $"{{\"Username\": \"{user.Username}\"}}",
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("User deactivated: {Username} by admin {AdminId}", user.Username, adminUserId);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> SetPinAsync(int userId, string? pin, int adminUserId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            _logger.Warning("User not found for PIN update: {UserId}", userId);
            return false;
        }

        if (string.IsNullOrWhiteSpace(pin))
        {
            // Remove PIN
            user.PINHash = null;
        }
        else
        {
            // Validate PIN format
            if (!ValidatePinFormat(pin))
            {
                throw new InvalidOperationException("PIN must be 4-6 digits.");
            }

            // Check PIN uniqueness
            if (!await IsPinUniqueAsync(pin, userId, cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidOperationException("This PIN is already in use by another user.");
            }

            user.PINHash = _passwordService.HashPin(pin);
        }

        // Audit log
        var auditLog = new AuditLog
        {
            UserId = adminUserId,
            Action = string.IsNullOrWhiteSpace(pin) ? "PINRemoved" : "PINChanged",
            EntityType = nameof(User),
            EntityId = userId,
            NewValues = $"{{\"Username\": \"{user.Username}\"}}",
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("PIN {Action} for user: {Username} by admin {AdminId}",
            string.IsNullOrWhiteSpace(pin) ? "removed" : "changed", user.Username, adminUserId);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> AssignRolesAsync(int userId, IEnumerable<int> roleIds, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            return false;
        }

        // Remove existing roles
        var existingRoles = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        _context.UserRoles.RemoveRange(existingRoles);

        // Add new roles
        var roleIdList = roleIds.Distinct().ToList();
        var validRoles = await _context.Roles
            .Where(r => roleIdList.Contains(r.Id))
            .Select(r => r.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var roleId in validRoles)
        {
            await _context.UserRoles.AddAsync(new UserRole
            {
                UserId = userId,
                RoleId = roleId
            }, cancellationToken).ConfigureAwait(false);
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> IsUsernameUniqueAsync(string username, int? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        var normalizedUsername = username.Trim().ToLowerInvariant();
        var query = _context.Users.Where(u => u.Username == normalizedUsername);

        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId.Value);
        }

        return !await query.AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> IsPinUniqueAsync(string pin, int? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pin);

        // We need to check all users' PIN hashes since they're salted
        var usersWithPins = await _context.Users
            .Where(u => u.PINHash != null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var user in usersWithPins)
        {
            if (excludeUserId.HasValue && user.Id == excludeUserId.Value)
            {
                continue;
            }

            if (_passwordService.VerifyPin(pin, user.PINHash!))
            {
                return false; // PIN is not unique
            }
        }

        return true;
    }

    /// <inheritdoc />
    public bool ValidatePinFormat(string? pin)
    {
        if (string.IsNullOrWhiteSpace(pin))
        {
            return true; // Empty PIN is valid (means no PIN)
        }

        // PIN must be 4-6 digits only
        return PinRegex().IsMatch(pin);
    }

    [GeneratedRegex(@"^\d{4,6}$")]
    private static partial Regex PinRegex();
}
