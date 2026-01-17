using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for handling permission override requests via PIN authentication.
/// </summary>
public class PermissionOverrideService : IPermissionOverrideService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAuthorizationService _authorizationService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionOverrideService"/> class.
    /// </summary>
    public PermissionOverrideService(
        IServiceScopeFactory scopeFactory,
        IAuthorizationService authorizationService,
        ILogger logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<OverrideResult> ValidatePinAndAuthorizeAsync(
        string pin,
        string requiredPermission,
        string actionDescription,
        int originalUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pin);
        ArgumentException.ThrowIfNullOrWhiteSpace(requiredPermission);

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<POSDbContext>();
        var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();

        try
        {
            // Find user by PIN - need to check all active users since PINs are hashed
            var activeUsers = await context.Users
                .AsNoTracking()
                .Where(u => u.IsActive && u.PinHash != null)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                .ToListAsync(cancellationToken);

            User? authorizingUser = null;
            foreach (var user in activeUsers)
            {
                if (passwordService.VerifyPassword(pin, user.PinHash!))
                {
                    authorizingUser = user;
                    break;
                }
            }

            if (authorizingUser is null)
            {
                _logger.Warning("Permission override failed: Invalid PIN. Action: {Action}, Permission: {Permission}, OriginalUser: {UserId}",
                    actionDescription, requiredPermission, originalUserId);

                await LogOverrideAttemptAsync(context, originalUserId, null, requiredPermission, actionDescription, false, "Invalid PIN", cancellationToken);
                return OverrideResult.Failure("Invalid PIN");
            }

            // Check if the authorizing user is different from the original user (can't authorize yourself)
            if (authorizingUser.Id == originalUserId)
            {
                _logger.Warning("Permission override failed: User cannot authorize their own action. User: {UserId}, Action: {Action}",
                    originalUserId, actionDescription);

                await LogOverrideAttemptAsync(context, originalUserId, authorizingUser.Id, requiredPermission, actionDescription, false, "Cannot authorize own action", cancellationToken);
                return OverrideResult.Failure("You cannot authorize your own action");
            }

            // Check if the authorizing user is locked
            if (authorizingUser.LockoutEnd.HasValue && authorizingUser.LockoutEnd > DateTime.UtcNow)
            {
                _logger.Warning("Permission override failed: Authorizing user account is locked. AuthorizingUser: {AuthUser}, OriginalUser: {UserId}",
                    authorizingUser.Username, originalUserId);

                await LogOverrideAttemptAsync(context, originalUserId, authorizingUser.Id, requiredPermission, actionDescription, false, "Account locked", cancellationToken);
                return OverrideResult.Failure("This account is currently locked");
            }

            // Check if the authorizing user has the required permission
            var hasPermission = authorizingUser.UserRoles
                .SelectMany(ur => ur.Role.RolePermissions)
                .Any(rp => rp.Permission.Name == requiredPermission);

            if (!hasPermission)
            {
                _logger.Warning("Permission override failed: Authorizing user lacks required permission. AuthUser: {AuthUser}, Permission: {Permission}, OriginalUser: {UserId}",
                    authorizingUser.Username, requiredPermission, originalUserId);

                await LogOverrideAttemptAsync(context, originalUserId, authorizingUser.Id, requiredPermission, actionDescription, false, "User lacks required permission", cancellationToken);
                return OverrideResult.Failure($"{authorizingUser.FullName} does not have the required permission");
            }

            // Success - log and return
            _logger.Information("Permission override successful. AuthorizingUser: {AuthUser} ({AuthId}), OriginalUser: {UserId}, Permission: {Permission}, Action: {Action}",
                authorizingUser.Username, authorizingUser.Id, originalUserId, requiredPermission, actionDescription);

            await LogOverrideAttemptAsync(context, originalUserId, authorizingUser.Id, requiredPermission, actionDescription, true, null, cancellationToken);

            return OverrideResult.Success(authorizingUser.Id, authorizingUser.FullName, requiredPermission, actionDescription);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during permission override validation");
            return OverrideResult.Failure("An error occurred during authorization");
        }
    }

    /// <inheritdoc />
    public async Task<User?> GetUserByPinAsync(string pin, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pin))
        {
            return null;
        }

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<POSDbContext>();
        var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();

        var activeUsers = await context.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.PinHash != null)
            .ToListAsync(cancellationToken);

        foreach (var user in activeUsers)
        {
            if (passwordService.VerifyPassword(pin, user.PinHash!))
            {
                return user;
            }
        }

        return null;
    }

    private static async Task LogOverrideAttemptAsync(
        POSDbContext context,
        int originalUserId,
        int? authorizingUserId,
        string permission,
        string actionDescription,
        bool success,
        string? reason,
        CancellationToken cancellationToken)
    {
        var auditLog = new AuditLog
        {
            UserId = originalUserId,
            Action = success ? "PermissionOverrideGranted" : "PermissionOverrideDenied",
            EntityType = "Permission",
            NewValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                Permission = permission,
                ActionDescription = actionDescription,
                AuthorizingUserId = authorizingUserId,
                Success = success,
                Reason = reason
            }),
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };

        await context.AuditLogs.AddAsync(auditLog, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
