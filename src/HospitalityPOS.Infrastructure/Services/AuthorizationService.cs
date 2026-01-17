using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using HospitalityPOS.Core.Constants;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Authorization service implementation for checking user permissions.
/// Registered as Singleton to maintain permission cache across the application lifecycle.
/// Uses IServiceScopeFactory for database access to avoid captive dependency issues.
/// </summary>
public class AuthorizationService : IAuthorizationService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISessionService _sessionService;
    private readonly ILogger _logger;

    private HashSet<string>? _cachedPermissions;
    private int? _cachedUserId;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizationService"/> class.
    /// </summary>
    public AuthorizationService(
        IServiceScopeFactory scopeFactory,
        ISessionService sessionService,
        ILogger logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public bool HasPermission(string permissionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionName);

        var user = _sessionService.CurrentUser;
        if (user is null)
        {
            _logger.Warning("Permission check failed - no user logged in. Permission: {Permission}", permissionName);
            return false;
        }

        var permissions = GetOrLoadPermissions(user.Id);
        var hasPermission = permissions.Contains(permissionName);

        // Log permission check for audit trail
        LogPermissionCheck(user.Id, user.Username, permissionName, hasPermission);

        return hasPermission;
    }

    /// <inheritdoc />
    public bool HasAnyPermission(params string[] permissionNames)
    {
        if (permissionNames is null || permissionNames.Length == 0)
        {
            return false;
        }

        var user = _sessionService.CurrentUser;
        if (user is null)
        {
            return false;
        }

        var permissions = GetOrLoadPermissions(user.Id);
        var hasAny = permissionNames.Any(p => permissions.Contains(p));

        // Log the check (summarized)
        _logger.Debug("Permission check (any): User={Username}, Permissions=[{Permissions}], Result={Result}",
            user.Username, string.Join(", ", permissionNames), hasAny);

        return hasAny;
    }

    /// <inheritdoc />
    public bool HasAllPermissions(params string[] permissionNames)
    {
        if (permissionNames is null || permissionNames.Length == 0)
        {
            return true;
        }

        var user = _sessionService.CurrentUser;
        if (user is null)
        {
            return false;
        }

        var permissions = GetOrLoadPermissions(user.Id);
        var hasAll = permissionNames.All(p => permissions.Contains(p));

        // Log the check (summarized)
        _logger.Debug("Permission check (all): User={Username}, Permissions=[{Permissions}], Result={Result}",
            user.Username, string.Join(", ", permissionNames), hasAll);

        return hasAll;
    }

    /// <inheritdoc />
    public IReadOnlySet<string> GetCurrentUserPermissions()
    {
        var user = _sessionService.CurrentUser;
        if (user is null)
        {
            return new HashSet<string>();
        }

        return GetOrLoadPermissions(user.Id);
    }

    /// <inheritdoc />
    public void RefreshPermissions()
    {
        _cachedPermissions = null;
        _cachedUserId = null;
        _logger.Debug("Permission cache cleared");
    }

    /// <inheritdoc />
    public bool CanApplyDiscount(decimal discountPercent)
    {
        if (discountPercent <= 0)
        {
            return true;
        }

        // Check from highest to lowest
        if (HasPermission(PermissionNames.Discounts.ApplyAny))
        {
            return true;
        }

        if (discountPercent <= 50 && HasPermission(PermissionNames.Discounts.Apply50))
        {
            return true;
        }

        if (discountPercent <= 20 && HasPermission(PermissionNames.Discounts.Apply20))
        {
            return true;
        }

        if (discountPercent <= 10 && HasPermission(PermissionNames.Discounts.Apply10))
        {
            return true;
        }

        return false;
    }

    private HashSet<string> GetOrLoadPermissions(int userId)
    {
        // Check if we have valid cached permissions for this user
        if (_cachedPermissions is not null && _cachedUserId == userId)
        {
            return _cachedPermissions;
        }

        // Load permissions from the session user if available (already has roles loaded)
        var user = _sessionService.CurrentUser;
        if (user?.UserRoles is not null && user.Id == userId)
        {
            _cachedPermissions = user.UserRoles
                .Where(ur => ur.Role?.RolePermissions is not null)
                .SelectMany(ur => ur.Role!.RolePermissions)
                .Where(rp => rp.Permission is not null)
                .Select(rp => rp.Permission!.Name)
                .ToHashSet();
        }
        else
        {
            // Fall back to database query using a scope
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<POSDbContext>();

            _cachedPermissions = context.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .SelectMany(u => u.UserRoles)
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission.Name)
                .ToHashSet();
        }

        _cachedUserId = userId;

        _logger.Debug("Loaded {PermissionCount} permissions for user {UserId}",
            _cachedPermissions.Count, userId);

        return _cachedPermissions;
    }

    private void LogPermissionCheck(int userId, string username, string permission, bool result)
    {
        // Log to structured logging (not audit log for every check - too verbose)
        // Only log failures or sensitive permissions to audit trail
        if (!result)
        {
            _logger.Information("Permission denied: User={Username} ({UserId}), Permission={Permission}",
                username, userId, permission);

            // Create audit log for permission denial (fire-and-forget with proper exception handling)
            _ = CreateAuditLogSafeAsync(userId, permission, result);
        }
        else
        {
            _logger.Debug("Permission granted: User={Username}, Permission={Permission}",
                username, permission);
        }
    }

    /// <summary>
    /// Safely creates an audit log without throwing exceptions.
    /// Wraps CreateAuditLogAsync with exception handling for fire-and-forget usage.
    /// </summary>
    private async Task CreateAuditLogSafeAsync(int userId, string permission, bool granted)
    {
        try
        {
            await CreateAuditLogAsync(userId, permission, granted);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to create audit log for user {UserId}, permission {Permission}", userId, permission);
            // Swallow exception - this is background work that shouldn't crash the app
        }
    }

    private async Task CreateAuditLogAsync(int userId, string permission, bool granted)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<POSDbContext>();

            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = granted ? "PermissionGranted" : "PermissionDenied",
                EntityType = "Permission",
                NewValues = $"{{\"Permission\": \"{permission}\", \"Granted\": {granted.ToString().ToLowerInvariant()}}}",
                MachineName = Environment.MachineName,
                CreatedAt = DateTime.UtcNow
            };

            await context.AuditLogs.AddAsync(auditLog).ConfigureAwait(false);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to create audit log for permission check");
        }
    }
}
