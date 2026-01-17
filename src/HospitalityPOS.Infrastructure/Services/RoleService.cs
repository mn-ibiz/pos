using Microsoft.EntityFrameworkCore;
using Serilog;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Role service implementation for role and permission management.
/// </summary>
public class RoleService : IRoleService
{
    private readonly POSDbContext _context;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoleService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger.</param>
    public RoleService(POSDbContext context, ILogger logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Role>> GetAllRolesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .AsNoTracking()
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .OrderBy(r => r.IsSystem ? 0 : 1)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Role?> GetRoleByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Role?> GetRoleByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return await _context.Roles
            .AsNoTracking()
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Role> CreateRoleAsync(RoleDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.Name);

        // Validate unique name
        if (!await IsRoleNameUniqueAsync(dto.Name, null, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException($"A role with the name '{dto.Name}' already exists.");
        }

        var role = new Role
        {
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            IsSystem = false,
            IsActive = true
        };

        await _context.Roles.AddAsync(role, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Assign permissions
        if (dto.PermissionIds.Count > 0)
        {
            await AssignPermissionsAsync(role.Id, dto.PermissionIds, cancellationToken).ConfigureAwait(false);
        }

        // Audit log
        var auditLog = new AuditLog
        {
            Action = "RoleCreated",
            EntityType = nameof(Role),
            EntityId = role.Id,
            NewValues = $"{{\"Name\": \"{role.Name}\", \"PermissionCount\": {dto.PermissionIds.Count}}}",
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Created new role: {RoleName} with {PermissionCount} permissions",
            role.Name, dto.PermissionIds.Count);

        return role;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateRoleAsync(int id, RoleDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.Name);

        var role = await _context.Roles
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (role is null)
        {
            _logger.Warning("Role not found for update: {RoleId}", id);
            return false;
        }

        // Validate unique name (excluding current role)
        if (!await IsRoleNameUniqueAsync(dto.Name, id, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException($"A role with the name '{dto.Name}' already exists.");
        }

        // Update properties (system role name cannot be changed)
        if (!role.IsSystem)
        {
            role.Name = dto.Name.Trim();
        }
        role.Description = dto.Description?.Trim();

        // Update permissions
        await AssignPermissionsAsync(id, dto.PermissionIds, cancellationToken).ConfigureAwait(false);

        // Audit log
        var auditLog = new AuditLog
        {
            Action = "RoleUpdated",
            EntityType = nameof(Role),
            EntityId = id,
            NewValues = $"{{\"Name\": \"{role.Name}\", \"PermissionCount\": {dto.PermissionIds.Count}}}",
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Updated role: {RoleName}", role.Name);
        return true;
    }

    /// <inheritdoc />
    public async Task<Role> CloneRoleAsync(int sourceRoleId, string newName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);

        var sourceRole = await GetRoleByIdAsync(sourceRoleId, cancellationToken).ConfigureAwait(false);
        if (sourceRole is null)
        {
            throw new InvalidOperationException($"Source role with ID {sourceRoleId} not found.");
        }

        // Validate unique name
        if (!await IsRoleNameUniqueAsync(newName, null, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException($"A role with the name '{newName}' already exists.");
        }

        // Get source role's permission IDs
        var permissionIds = sourceRole.RolePermissions.Select(rp => rp.PermissionId).ToList();

        var dto = new RoleDto
        {
            Name = newName.Trim(),
            Description = $"Cloned from {sourceRole.Name}",
            PermissionIds = permissionIds
        };

        var clonedRole = await CreateRoleAsync(dto, cancellationToken).ConfigureAwait(false);

        _logger.Information("Cloned role {SourceRole} to {NewRole}", sourceRole.Name, newName);
        return clonedRole;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteRoleAsync(int id, CancellationToken cancellationToken = default)
    {
        var role = await _context.Roles
            .Include(r => r.UserRoles)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (role is null)
        {
            _logger.Warning("Role not found for deletion: {RoleId}", id);
            return false;
        }

        if (role.IsSystem)
        {
            _logger.Warning("Cannot delete system role: {RoleName}", role.Name);
            return false;
        }

        if (role.UserRoles.Count > 0)
        {
            _logger.Warning("Cannot delete role with assigned users: {RoleName} has {UserCount} users",
                role.Name, role.UserRoles.Count);
            return false;
        }

        // Remove role permissions first
        var rolePermissions = await _context.RolePermissions
            .Where(rp => rp.RoleId == id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        _context.RolePermissions.RemoveRange(rolePermissions);
        _context.Roles.Remove(role);

        // Audit log
        var auditLog = new AuditLog
        {
            Action = "RoleDeleted",
            EntityType = nameof(Role),
            EntityId = id,
            OldValues = $"{{\"Name\": \"{role.Name}\", \"PermissionCount\": {rolePermissions.Count}}}",
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Deleted role: {RoleName}", role.Name);
        return true;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Permission>> GetAllPermissionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, IReadOnlyList<Permission>>> GetPermissionsByCategoryAsync(CancellationToken cancellationToken = default)
    {
        var permissions = await GetAllPermissionsAsync(cancellationToken).ConfigureAwait(false);

        return permissions
            .GroupBy(p => p.Category ?? "General")
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<Permission>)g.ToList()
            );
    }

    /// <inheritdoc />
    public async Task<bool> AssignPermissionsAsync(int roleId, IEnumerable<int> permissionIds, CancellationToken cancellationToken = default)
    {
        var role = await _context.Roles
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken)
            .ConfigureAwait(false);

        if (role is null)
        {
            return false;
        }

        // Remove existing permissions
        var existingPermissions = await _context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        _context.RolePermissions.RemoveRange(existingPermissions);

        // Add new permissions
        var permissionIdList = permissionIds.Distinct().ToList();
        var validPermissions = await _context.Permissions
            .Where(p => permissionIdList.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var permissionId in validPermissions)
        {
            await _context.RolePermissions.AddAsync(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId
            }, cancellationToken).ConfigureAwait(false);
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> IsRoleNameUniqueAsync(string name, int? excludeRoleId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var query = _context.Roles.Where(r => r.Name == name.Trim());

        if (excludeRoleId.HasValue)
        {
            query = query.Where(r => r.Id != excludeRoleId.Value);
        }

        return !await query.AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> CanDeleteRoleAsync(int roleId, CancellationToken cancellationToken = default)
    {
        var role = await _context.Roles
            .AsNoTracking()
            .Include(r => r.UserRoles)
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken)
            .ConfigureAwait(false);

        if (role is null)
        {
            return false;
        }

        return !role.IsSystem && role.UserRoles.Count == 0;
    }
}
