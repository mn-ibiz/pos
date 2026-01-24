using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Serilog;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing POS terminals.
/// </summary>
public class TerminalService : ITerminalService
{
    private readonly POSDbContext _context;
    private readonly ILogger _logger;
    private const int HeartbeatTimeoutSeconds = 60;

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger.</param>
    public TerminalService(POSDbContext context, ILogger logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region CRUD Operations

    /// <inheritdoc />
    public async Task<Terminal> CreateTerminalAsync(
        CreateTerminalRequest request,
        int createdByUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Code);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);

        // Validate uniqueness
        if (!await IsTerminalCodeUniqueAsync(request.StoreId, request.Code, null, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException($"Terminal code '{request.Code}' already exists in this store.");
        }

        if (!string.IsNullOrWhiteSpace(request.MachineIdentifier) &&
            !await IsMachineIdentifierAvailableAsync(request.MachineIdentifier, null, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException($"Machine identifier '{request.MachineIdentifier}' is already in use.");
        }

        var terminal = new Terminal
        {
            StoreId = request.StoreId,
            Code = request.Code.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            MachineIdentifier = request.MachineIdentifier?.Trim() ?? string.Empty,
            TerminalType = request.TerminalType,
            BusinessMode = request.BusinessMode,
            IsMainRegister = request.IsMainRegister,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
        };

        await _context.Terminals.AddAsync(terminal, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await CreateAuditLogAsync("TerminalCreated", terminal.Id, null, terminal, createdByUserId, cancellationToken).ConfigureAwait(false);

        _logger.Information("Terminal '{Code}' ({Name}) created by user {UserId}",
            terminal.Code, terminal.Name, createdByUserId);

        return terminal;
    }

    /// <inheritdoc />
    public async Task<Terminal?> GetTerminalByIdAsync(
        int terminalId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Terminals
            .AsNoTracking()
            .Include(t => t.Store)
            .FirstOrDefaultAsync(t => t.Id == terminalId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Terminal?> GetTerminalByCodeAsync(
        int storeId,
        string code,
        CancellationToken cancellationToken = default)
    {
        return await _context.Terminals
            .AsNoTracking()
            .Include(t => t.Store)
            .FirstOrDefaultAsync(t => t.StoreId == storeId && t.Code == code.ToUpperInvariant(), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Terminal?> GetTerminalByMachineIdAsync(
        string machineIdentifier,
        CancellationToken cancellationToken = default)
    {
        return await _context.Terminals
            .AsNoTracking()
            .Include(t => t.Store)
            .FirstOrDefaultAsync(t => t.MachineIdentifier == machineIdentifier, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Terminal>> GetTerminalsByStoreAsync(
        int storeId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Terminals
            .AsNoTracking()
            .Where(t => t.StoreId == storeId)
            .OrderBy(t => t.Code)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Terminal> UpdateTerminalAsync(
        int terminalId,
        UpdateTerminalRequest request,
        int modifiedByUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var terminal = await _context.Terminals
            .FirstOrDefaultAsync(t => t.Id == terminalId, cancellationToken)
            .ConfigureAwait(false);

        if (terminal is null)
        {
            throw new InvalidOperationException($"Terminal with ID {terminalId} not found.");
        }

        var oldValues = new
        {
            terminal.Code,
            terminal.Name,
            terminal.Description,
            terminal.TerminalType,
            terminal.BusinessMode,
            terminal.IsMainRegister
        };

        // Validate and update code if changed
        if (!string.IsNullOrWhiteSpace(request.Code) && request.Code != terminal.Code)
        {
            if (!await IsTerminalCodeUniqueAsync(terminal.StoreId, request.Code, terminalId, cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidOperationException($"Terminal code '{request.Code}' already exists in this store.");
            }
            terminal.Code = request.Code.Trim().ToUpperInvariant();
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
            terminal.Name = request.Name.Trim();

        if (request.Description is not null)
            terminal.Description = request.Description.Trim();

        if (request.TerminalType.HasValue)
            terminal.TerminalType = request.TerminalType.Value;

        if (request.BusinessMode.HasValue)
            terminal.BusinessMode = request.BusinessMode.Value;

        if (request.IsMainRegister.HasValue)
            terminal.IsMainRegister = request.IsMainRegister.Value;

        if (request.PrinterConfiguration is not null)
            terminal.PrinterConfiguration = request.PrinterConfiguration;

        if (request.HardwareConfiguration is not null)
            terminal.HardwareConfiguration = request.HardwareConfiguration;

        terminal.UpdatedAt = DateTime.UtcNow;
        terminal.UpdatedByUserId = modifiedByUserId;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await CreateAuditLogAsync("TerminalUpdated", terminal.Id, oldValues, terminal, modifiedByUserId, cancellationToken).ConfigureAwait(false);

        _logger.Information("Terminal '{Code}' (ID: {Id}) updated by user {UserId}",
            terminal.Code, terminalId, modifiedByUserId);

        return terminal;
    }

    /// <inheritdoc />
    public async Task<bool> DeactivateTerminalAsync(
        int terminalId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var terminal = await _context.Terminals
            .FirstOrDefaultAsync(t => t.Id == terminalId, cancellationToken)
            .ConfigureAwait(false);

        if (terminal is null)
        {
            return false;
        }

        terminal.IsActive = false;
        terminal.UpdatedAt = DateTime.UtcNow;
        terminal.UpdatedByUserId = userId;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await CreateAuditLogAsync("TerminalDeactivated", terminal.Id, new { IsActive = true }, new { IsActive = false }, userId, cancellationToken).ConfigureAwait(false);

        _logger.Information("Terminal '{Code}' (ID: {Id}) deactivated by user {UserId}",
            terminal.Code, terminalId, userId);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ReactivateTerminalAsync(
        int terminalId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var terminal = await _context.Terminals
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == terminalId, cancellationToken)
            .ConfigureAwait(false);

        if (terminal is null)
        {
            return false;
        }

        terminal.IsActive = true;
        terminal.UpdatedAt = DateTime.UtcNow;
        terminal.UpdatedByUserId = userId;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await CreateAuditLogAsync("TerminalReactivated", terminal.Id, new { IsActive = false }, new { IsActive = true }, userId, cancellationToken).ConfigureAwait(false);

        _logger.Information("Terminal '{Code}' (ID: {Id}) reactivated by user {UserId}",
            terminal.Code, terminalId, userId);

        return true;
    }

    #endregion

    #region Validation

    /// <inheritdoc />
    public async Task<bool> IsTerminalCodeUniqueAsync(
        int storeId,
        string code,
        int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        var query = _context.Terminals
            .Where(t => t.StoreId == storeId && t.Code == normalizedCode);

        if (excludeId.HasValue)
        {
            query = query.Where(t => t.Id != excludeId.Value);
        }

        return !await query.AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> IsMachineIdentifierAvailableAsync(
        string machineIdentifier,
        int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Terminals
            .Where(t => t.MachineIdentifier == machineIdentifier);

        if (excludeId.HasValue)
        {
            query = query.Where(t => t.Id != excludeId.Value);
        }

        return !await query.AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TerminalValidationResult> ValidateTerminalAsync(
        int terminalId,
        string? machineIdentifier = null,
        CancellationToken cancellationToken = default)
    {
        var terminal = await _context.Terminals
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == terminalId, cancellationToken)
            .ConfigureAwait(false);

        if (terminal is null)
        {
            return TerminalValidationResult.Failure("Terminal not found.");
        }

        var result = new TerminalValidationResult
        {
            IsActive = terminal.IsActive,
            IsOnline = terminal.LastHeartbeat.HasValue &&
                       terminal.LastHeartbeat.Value > DateTime.UtcNow.AddSeconds(-HeartbeatTimeoutSeconds),
            MachineIdentifierMatches = string.IsNullOrWhiteSpace(machineIdentifier) ||
                                       terminal.MachineIdentifier == machineIdentifier
        };

        if (!terminal.IsActive)
        {
            result.Errors.Add("Terminal is deactivated.");
        }

        if (!result.MachineIdentifierMatches)
        {
            result.Errors.Add("Machine identifier does not match.");
        }

        result.IsValid = result.IsActive && result.MachineIdentifierMatches;

        return result;
    }

    #endregion

    #region Registration

    /// <inheritdoc />
    public async Task<Terminal> RegisterTerminalAsync(
        TerminalRegistrationRequest request,
        int createdByUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.MachineIdentifier);

        // Check if machine is already registered
        var existingTerminal = await GetTerminalByMachineIdAsync(request.MachineIdentifier, cancellationToken).ConfigureAwait(false);
        if (existingTerminal is not null)
        {
            throw new InvalidOperationException($"Machine is already registered as terminal '{existingTerminal.Code}'.");
        }

        // Auto-generate code if not provided
        var code = string.IsNullOrWhiteSpace(request.Code)
            ? await GenerateTerminalCodeAsync(request.StoreId, request.TerminalType, cancellationToken).ConfigureAwait(false)
            : request.Code;

        var terminal = new Terminal
        {
            StoreId = request.StoreId,
            Code = code.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            MachineIdentifier = request.MachineIdentifier.Trim(),
            TerminalType = request.TerminalType,
            BusinessMode = request.BusinessMode,
            IpAddress = request.IpAddress,
            LastHeartbeat = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
        };

        if (request.Hardware is not null)
        {
            terminal.HardwareConfiguration = JsonSerializer.Serialize(request.Hardware);
        }

        await _context.Terminals.AddAsync(terminal, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await CreateAuditLogAsync("TerminalRegistered", terminal.Id, null, terminal, createdByUserId, cancellationToken).ConfigureAwait(false);

        _logger.Information("Terminal '{Code}' registered with machine ID '{MachineId}' by user {UserId}",
            terminal.Code, request.MachineIdentifier, createdByUserId);

        return terminal;
    }

    /// <inheritdoc />
    public async Task<bool> BindMachineAsync(
        int terminalId,
        string machineIdentifier,
        int userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(machineIdentifier);

        if (!await IsMachineIdentifierAvailableAsync(machineIdentifier, terminalId, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Machine identifier is already in use by another terminal.");
        }

        var terminal = await _context.Terminals
            .FirstOrDefaultAsync(t => t.Id == terminalId, cancellationToken)
            .ConfigureAwait(false);

        if (terminal is null)
        {
            return false;
        }

        var oldMachineId = terminal.MachineIdentifier;
        terminal.MachineIdentifier = machineIdentifier.Trim();
        terminal.UpdatedAt = DateTime.UtcNow;
        terminal.UpdatedByUserId = userId;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await CreateAuditLogAsync("MachineBound", terminal.Id,
            new { MachineIdentifier = oldMachineId },
            new { MachineIdentifier = machineIdentifier },
            userId, cancellationToken).ConfigureAwait(false);

        _logger.Information("Terminal '{Code}' bound to machine '{MachineId}' by user {UserId}",
            terminal.Code, machineIdentifier, userId);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> UnbindMachineAsync(
        int terminalId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var terminal = await _context.Terminals
            .FirstOrDefaultAsync(t => t.Id == terminalId, cancellationToken)
            .ConfigureAwait(false);

        if (terminal is null)
        {
            return false;
        }

        var oldMachineId = terminal.MachineIdentifier;
        terminal.MachineIdentifier = string.Empty;
        terminal.UpdatedAt = DateTime.UtcNow;
        terminal.UpdatedByUserId = userId;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await CreateAuditLogAsync("MachineUnbound", terminal.Id,
            new { MachineIdentifier = oldMachineId },
            new { MachineIdentifier = string.Empty },
            userId, cancellationToken).ConfigureAwait(false);

        _logger.Information("Terminal '{Code}' unbound from machine '{MachineId}' by user {UserId}",
            terminal.Code, oldMachineId, userId);

        return true;
    }

    #endregion

    #region Status

    /// <inheritdoc />
    public async Task<bool> UpdateHeartbeatAsync(
        int terminalId,
        TerminalHeartbeat heartbeat,
        CancellationToken cancellationToken = default)
    {
        var terminal = await _context.Terminals
            .FirstOrDefaultAsync(t => t.Id == terminalId, cancellationToken)
            .ConfigureAwait(false);

        if (terminal is null)
        {
            return false;
        }

        terminal.LastHeartbeat = DateTime.UtcNow;
        terminal.IpAddress = heartbeat.IpAddress;

        if (heartbeat.CurrentUserId.HasValue)
        {
            terminal.LastLoginUserId = heartbeat.CurrentUserId;
            terminal.LastLoginAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return true;
    }

    /// <inheritdoc />
    public async Task<TerminalStatusDto?> GetTerminalStatusAsync(
        int terminalId,
        CancellationToken cancellationToken = default)
    {
        var terminal = await _context.Terminals
            .AsNoTracking()
            .Include(t => t.LastLoginUser)
            .FirstOrDefaultAsync(t => t.Id == terminalId, cancellationToken)
            .ConfigureAwait(false);

        if (terminal is null)
        {
            return null;
        }

        return MapToStatusDto(terminal);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TerminalStatusDto>> GetAllTerminalStatusesAsync(
        int storeId,
        CancellationToken cancellationToken = default)
    {
        var terminals = await _context.Terminals
            .AsNoTracking()
            .Include(t => t.LastLoginUser)
            .Where(t => t.StoreId == storeId)
            .OrderBy(t => t.Code)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return terminals.Select(MapToStatusDto).ToList();
    }

    #endregion

    #region Queries

    /// <inheritdoc />
    public async Task<IReadOnlyList<Terminal>> GetUnassignedTerminalsAsync(
        int storeId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Terminals
            .AsNoTracking()
            .Where(t => t.StoreId == storeId && string.IsNullOrEmpty(t.MachineIdentifier))
            .OrderBy(t => t.Code)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Terminal>> GetActiveTerminalsAsync(
        int storeId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Terminals
            .AsNoTracking()
            .Where(t => t.StoreId == storeId && t.IsActive)
            .OrderBy(t => t.Code)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> GetNextTerminalNumberAsync(
        int storeId,
        TerminalType type,
        CancellationToken cancellationToken = default)
    {
        var prefix = GetTerminalCodePrefix(type);
        var pattern = $"{prefix}-%";

        var existingCodes = await _context.Terminals
            .Where(t => t.StoreId == storeId && EF.Functions.Like(t.Code, pattern))
            .Select(t => t.Code)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var maxNumber = 0;
        foreach (var code in existingCodes)
        {
            var parts = code.Split('-');
            if (parts.Length >= 2 && int.TryParse(parts[^1], out var number))
            {
                maxNumber = Math.Max(maxNumber, number);
            }
        }

        return maxNumber + 1;
    }

    /// <inheritdoc />
    public async Task<string> GenerateTerminalCodeAsync(
        int storeId,
        TerminalType type,
        CancellationToken cancellationToken = default)
    {
        var prefix = GetTerminalCodePrefix(type);
        var nextNumber = await GetNextTerminalNumberAsync(storeId, type, cancellationToken).ConfigureAwait(false);
        return $"{prefix}-{nextNumber:D3}";
    }

    #endregion

    #region Private Helpers

    private static string GetTerminalCodePrefix(TerminalType type)
    {
        return type switch
        {
            TerminalType.Register => "REG",
            TerminalType.Till => "TILL",
            TerminalType.AdminWorkstation => "ADMIN",
            TerminalType.KitchenDisplay => "KDS",
            TerminalType.MobileTerminal => "MOB",
            TerminalType.SelfCheckout => "SCO",
            _ => "TERM"
        };
    }

    private TerminalStatusDto MapToStatusDto(Terminal terminal)
    {
        return new TerminalStatusDto
        {
            TerminalId = terminal.Id,
            Code = terminal.Code,
            Name = terminal.Name,
            TerminalType = terminal.TerminalType,
            IsOnline = terminal.LastHeartbeat.HasValue &&
                       terminal.LastHeartbeat.Value > DateTime.UtcNow.AddSeconds(-HeartbeatTimeoutSeconds),
            LastHeartbeat = terminal.LastHeartbeat,
            CurrentUserName = terminal.LastLoginUser?.Username,
            IpAddress = terminal.IpAddress,
            IsActive = terminal.IsActive,
            // These would be updated from heartbeat data in a real implementation
            IsWorkPeriodOpen = false,
            IsPrinterAvailable = true,
            IsCashDrawerAvailable = true
        };
    }

    private async Task CreateAuditLogAsync(
        string action,
        int entityId,
        object? oldValues,
        object? newValues,
        int userId,
        CancellationToken cancellationToken)
    {
        var auditLog = new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = nameof(Terminal),
            EntityId = entityId,
            OldValues = oldValues is not null ? JsonSerializer.Serialize(oldValues) : null,
            NewValues = newValues is not null ? JsonSerializer.Serialize(newValues) : null,
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };

        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    #endregion
}
