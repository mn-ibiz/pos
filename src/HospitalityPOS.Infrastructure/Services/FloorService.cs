using Microsoft.EntityFrameworkCore;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing floors, sections, and tables.
/// </summary>
public class FloorService : IFloorService
{
    private readonly POSDbContext _context;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FloorService"/> class.
    /// </summary>
    public FloorService(POSDbContext context, ILogger logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Floor Operations

    /// <inheritdoc />
    public async Task<IReadOnlyList<Floor>> GetAllFloorsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Floors
            .AsNoTracking()
            .OrderBy(f => f.DisplayOrder)
            .ThenBy(f => f.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Floor>> GetActiveFloorsWithTablesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Floors
            .AsNoTracking()
            .Where(f => f.IsActive)
            .Include(f => f.Tables.Where(t => t.IsActive))
            .Include(f => f.Sections.Where(s => s.IsActive))
            .OrderBy(f => f.DisplayOrder)
            .ThenBy(f => f.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Floor?> GetFloorByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Floors
            .AsNoTracking()
            .Include(f => f.Sections.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder))
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Floor?> GetFloorWithTablesAsync(int floorId, CancellationToken cancellationToken = default)
    {
        // Use AsSplitQuery to avoid Cartesian explosion with multiple Includes
        return await _context.Floors
            .AsNoTracking()
            .AsSplitQuery()
            .Include(f => f.Tables.Where(t => t.IsActive))
                .ThenInclude(t => t.Section)
            .Include(f => f.Tables.Where(t => t.IsActive))
                .ThenInclude(t => t.CurrentReceipt)
            .Include(f => f.Tables.Where(t => t.IsActive))
                .ThenInclude(t => t.AssignedUser)
            .Include(f => f.Sections.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder))
            .FirstOrDefaultAsync(f => f.Id == floorId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Floor> CreateFloorAsync(FloorDto dto, int createdByUserId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.Name);

        if (!await IsFloorNameUniqueAsync(dto.Name, null, cancellationToken))
        {
            throw new InvalidOperationException($"A floor named '{dto.Name}' already exists.");
        }

        var floor = new Floor
        {
            Name = dto.Name.Trim(),
            DisplayOrder = dto.DisplayOrder,
            GridWidth = dto.GridWidth > 0 ? dto.GridWidth : 10,
            GridHeight = dto.GridHeight > 0 ? dto.GridHeight : 10,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
        };

        await _context.Floors.AddAsync(floor, cancellationToken).ConfigureAwait(false);

        // Create audit log entry (will be saved in same transaction)
        var auditLog = new AuditLog
        {
            UserId = createdByUserId,
            Action = "FloorCreated",
            EntityType = nameof(Floor),
            EntityId = 0, // Will be set after save
            NewValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                Name = floor.Name,
                floor.DisplayOrder,
                floor.GridWidth,
                floor.GridHeight,
                floor.IsActive
            }),
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);

        // Single SaveChangesAsync call - both entities saved in one transaction
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Update audit log with the actual floor ID
        auditLog.EntityId = floor.Id;
        auditLog.NewValues = System.Text.Json.JsonSerializer.Serialize(new
        {
            floor.Id,
            floor.Name,
            floor.DisplayOrder,
            floor.GridWidth,
            floor.GridHeight,
            floor.IsActive
        });
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Floor '{FloorName}' created by user {UserId}", floor.Name, createdByUserId);

        return floor;
    }

    /// <inheritdoc />
    public async Task<Floor> UpdateFloorAsync(int id, FloorDto dto, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.Name);

        var floor = await _context.Floors
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (floor is null)
        {
            throw new InvalidOperationException($"Floor with ID {id} not found.");
        }

        if (!await IsFloorNameUniqueAsync(dto.Name, id, cancellationToken))
        {
            throw new InvalidOperationException($"A floor named '{dto.Name}' already exists.");
        }

        var oldValues = new
        {
            floor.Name,
            floor.DisplayOrder,
            floor.GridWidth,
            floor.GridHeight,
            floor.IsActive
        };

        floor.Name = dto.Name.Trim();
        floor.DisplayOrder = dto.DisplayOrder;
        floor.GridWidth = dto.GridWidth > 0 ? dto.GridWidth : floor.GridWidth;
        floor.GridHeight = dto.GridHeight > 0 ? dto.GridHeight : floor.GridHeight;
        floor.IsActive = dto.IsActive;
        floor.UpdatedAt = DateTime.UtcNow;
        floor.UpdatedByUserId = modifiedByUserId;

        var auditLog = new AuditLog
        {
            UserId = modifiedByUserId,
            Action = "FloorUpdated",
            EntityType = nameof(Floor),
            EntityId = id,
            OldValues = System.Text.Json.JsonSerializer.Serialize(oldValues),
            NewValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                floor.Name,
                floor.DisplayOrder,
                floor.GridWidth,
                floor.GridHeight,
                floor.IsActive
            }),
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Floor '{FloorName}' (ID: {FloorId}) updated by user {UserId}",
            floor.Name, id, modifiedByUserId);

        return floor;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteFloorAsync(int id, int deletedByUserId, CancellationToken cancellationToken = default)
    {
        var floor = await _context.Floors
            .Include(f => f.Tables)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (floor is null)
        {
            return false;
        }

        // Check if any tables are occupied
        if (floor.Tables.Any(t => t.Status == TableStatus.Occupied))
        {
            throw new InvalidOperationException("Cannot delete a floor with occupied tables. Please settle all bills first.");
        }

        // Soft delete by setting IsActive to false
        floor.IsActive = false;
        floor.UpdatedAt = DateTime.UtcNow;
        floor.UpdatedByUserId = deletedByUserId;

        // Also deactivate all tables on this floor
        foreach (var table in floor.Tables)
        {
            table.IsActive = false;
            table.UpdatedAt = DateTime.UtcNow;
            table.UpdatedByUserId = deletedByUserId;
        }

        var auditLog = new AuditLog
        {
            UserId = deletedByUserId,
            Action = "FloorDeleted",
            EntityType = nameof(Floor),
            EntityId = id,
            OldValues = System.Text.Json.JsonSerializer.Serialize(new { floor.Name, TableCount = floor.Tables.Count }),
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Floor '{FloorName}' (ID: {FloorId}) deleted by user {UserId}",
            floor.Name, id, deletedByUserId);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> IsFloorNameUniqueAsync(string name, int? excludeFloorId = null, CancellationToken cancellationToken = default)
    {
        var trimmedName = name.Trim().ToLowerInvariant();

        var query = _context.Floors
            .Where(f => f.IsActive)
            .Where(f => f.Name.ToLower() == trimmedName);

        if (excludeFloorId.HasValue)
        {
            query = query.Where(f => f.Id != excludeFloorId.Value);
        }

        return !await query.AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Section Operations

    /// <inheritdoc />
    public async Task<IReadOnlyList<Section>> GetSectionsByFloorIdAsync(int floorId, CancellationToken cancellationToken = default)
    {
        return await _context.Sections
            .AsNoTracking()
            .Where(s => s.FloorId == floorId && s.IsActive)
            .Include(s => s.Tables.Where(t => t.IsActive))
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Section?> GetSectionByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Sections
            .AsNoTracking()
            .Include(s => s.Floor)
            .Include(s => s.Tables.Where(t => t.IsActive))
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Section> CreateSectionAsync(SectionDto dto, int createdByUserId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.Name);

        // Validate floor exists
        var floorExists = await _context.Floors
            .AnyAsync(f => f.Id == dto.FloorId && f.IsActive, cancellationToken)
            .ConfigureAwait(false);

        if (!floorExists)
        {
            throw new InvalidOperationException($"Floor with ID {dto.FloorId} not found.");
        }

        // Check for duplicate section name on the same floor
        var duplicateName = await _context.Sections
            .AnyAsync(s => s.FloorId == dto.FloorId && s.IsActive && s.Name.ToLower() == dto.Name.Trim().ToLower(), cancellationToken)
            .ConfigureAwait(false);

        if (duplicateName)
        {
            throw new InvalidOperationException($"A section named '{dto.Name}' already exists on this floor.");
        }

        var section = new Section
        {
            Name = dto.Name.Trim(),
            ColorCode = string.IsNullOrWhiteSpace(dto.ColorCode) ? "#4CAF50" : dto.ColorCode,
            FloorId = dto.FloorId,
            DisplayOrder = dto.DisplayOrder,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
        };

        await _context.Sections.AddAsync(section, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var auditLog = new AuditLog
        {
            UserId = createdByUserId,
            Action = "SectionCreated",
            EntityType = nameof(Section),
            EntityId = section.Id,
            NewValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                section.Id,
                section.Name,
                section.FloorId,
                section.ColorCode,
                section.IsActive
            }),
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Section '{SectionName}' created on floor {FloorId} by user {UserId}",
            section.Name, dto.FloorId, createdByUserId);

        return section;
    }

    /// <inheritdoc />
    public async Task<Section> UpdateSectionAsync(int id, SectionDto dto, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.Name);

        var section = await _context.Sections
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (section is null)
        {
            throw new InvalidOperationException($"Section with ID {id} not found.");
        }

        // Check for duplicate section name on the same floor
        var duplicateName = await _context.Sections
            .AnyAsync(s => s.FloorId == section.FloorId && s.IsActive && s.Id != id && s.Name.ToLower() == dto.Name.Trim().ToLower(), cancellationToken)
            .ConfigureAwait(false);

        if (duplicateName)
        {
            throw new InvalidOperationException($"A section named '{dto.Name}' already exists on this floor.");
        }

        var oldValues = new
        {
            section.Name,
            section.ColorCode,
            section.DisplayOrder,
            section.IsActive
        };

        section.Name = dto.Name.Trim();
        section.ColorCode = string.IsNullOrWhiteSpace(dto.ColorCode) ? section.ColorCode : dto.ColorCode;
        section.DisplayOrder = dto.DisplayOrder;
        section.IsActive = dto.IsActive;
        section.UpdatedAt = DateTime.UtcNow;
        section.UpdatedByUserId = modifiedByUserId;

        var auditLog = new AuditLog
        {
            UserId = modifiedByUserId,
            Action = "SectionUpdated",
            EntityType = nameof(Section),
            EntityId = id,
            OldValues = System.Text.Json.JsonSerializer.Serialize(oldValues),
            NewValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                section.Name,
                section.ColorCode,
                section.DisplayOrder,
                section.IsActive
            }),
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Section '{SectionName}' (ID: {SectionId}) updated by user {UserId}",
            section.Name, id, modifiedByUserId);

        return section;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteSectionAsync(int id, int deletedByUserId, CancellationToken cancellationToken = default)
    {
        var section = await _context.Sections
            .Include(s => s.Tables)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (section is null)
        {
            return false;
        }

        // Remove section assignment from tables (don't delete the tables)
        foreach (var table in section.Tables)
        {
            table.SectionId = null;
            table.UpdatedAt = DateTime.UtcNow;
            table.UpdatedByUserId = deletedByUserId;
        }

        section.IsActive = false;
        section.UpdatedAt = DateTime.UtcNow;
        section.UpdatedByUserId = deletedByUserId;

        var auditLog = new AuditLog
        {
            UserId = deletedByUserId,
            Action = "SectionDeleted",
            EntityType = nameof(Section),
            EntityId = id,
            OldValues = System.Text.Json.JsonSerializer.Serialize(new { section.Name, section.FloorId, TableCount = section.Tables.Count }),
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Section '{SectionName}' (ID: {SectionId}) deleted by user {UserId}",
            section.Name, id, deletedByUserId);

        return true;
    }

    #endregion

    #region Table Operations

    /// <inheritdoc />
    public async Task<IReadOnlyList<Table>> GetTablesByFloorIdAsync(int floorId, CancellationToken cancellationToken = default)
    {
        return await _context.Tables
            .AsNoTracking()
            .Where(t => t.FloorId == floorId && t.IsActive)
            .Include(t => t.Section)
            .Include(t => t.CurrentReceipt)
            .Include(t => t.AssignedUser)
            .OrderBy(t => t.TableNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Table>> GetTablesBySectionIdAsync(int sectionId, CancellationToken cancellationToken = default)
    {
        return await _context.Tables
            .AsNoTracking()
            .Where(t => t.SectionId == sectionId && t.IsActive)
            .Include(t => t.CurrentReceipt)
            .Include(t => t.AssignedUser)
            .OrderBy(t => t.TableNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Table?> GetTableByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Tables
            .AsNoTracking()
            .Include(t => t.Floor)
            .Include(t => t.Section)
            .Include(t => t.CurrentReceipt)
            .Include(t => t.AssignedUser)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Table?> GetTableByNumberAsync(int floorId, string tableNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Tables
            .AsNoTracking()
            .Include(t => t.Floor)
            .Include(t => t.Section)
            .Include(t => t.CurrentReceipt)
            .Include(t => t.AssignedUser)
            .FirstOrDefaultAsync(t => t.FloorId == floorId && t.TableNumber == tableNumber && t.IsActive, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Table> CreateTableAsync(TableDto dto, int createdByUserId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.TableNumber);

        // Validate floor exists
        var floorExists = await _context.Floors
            .AnyAsync(f => f.Id == dto.FloorId && f.IsActive, cancellationToken)
            .ConfigureAwait(false);

        if (!floorExists)
        {
            throw new InvalidOperationException($"Floor with ID {dto.FloorId} not found.");
        }

        // Validate section exists if specified
        if (dto.SectionId.HasValue)
        {
            var sectionExists = await _context.Sections
                .AnyAsync(s => s.Id == dto.SectionId.Value && s.FloorId == dto.FloorId && s.IsActive, cancellationToken)
                .ConfigureAwait(false);

            if (!sectionExists)
            {
                throw new InvalidOperationException($"Section with ID {dto.SectionId} not found on floor {dto.FloorId}.");
            }
        }

        // Check for duplicate table number on the same floor
        if (!await IsTableNumberUniqueAsync(dto.FloorId, dto.TableNumber, null, cancellationToken))
        {
            throw new InvalidOperationException($"Table number '{dto.TableNumber}' already exists on this floor.");
        }

        var table = new Table
        {
            TableNumber = dto.TableNumber.Trim(),
            Capacity = dto.Capacity > 0 ? dto.Capacity : 4,
            FloorId = dto.FloorId,
            SectionId = dto.SectionId,
            GridX = dto.GridX,
            GridY = dto.GridY,
            Width = dto.Width > 0 ? dto.Width : 1,
            Height = dto.Height > 0 ? dto.Height : 1,
            Shape = dto.Shape,
            Status = TableStatus.Available,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
        };

        await _context.Tables.AddAsync(table, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var auditLog = new AuditLog
        {
            UserId = createdByUserId,
            Action = "TableCreated",
            EntityType = nameof(Table),
            EntityId = table.Id,
            NewValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                table.Id,
                table.TableNumber,
                table.FloorId,
                table.SectionId,
                table.Capacity,
                table.Shape,
                table.GridX,
                table.GridY
            }),
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Table '{TableNumber}' created on floor {FloorId} by user {UserId}",
            table.TableNumber, dto.FloorId, createdByUserId);

        return table;
    }

    /// <inheritdoc />
    public async Task<Table> UpdateTableAsync(int id, TableDto dto, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.TableNumber);

        var table = await _context.Tables
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (table is null)
        {
            throw new InvalidOperationException($"Table with ID {id} not found.");
        }

        // Validate section exists if specified
        if (dto.SectionId.HasValue)
        {
            var sectionExists = await _context.Sections
                .AnyAsync(s => s.Id == dto.SectionId.Value && s.FloorId == table.FloorId && s.IsActive, cancellationToken)
                .ConfigureAwait(false);

            if (!sectionExists)
            {
                throw new InvalidOperationException($"Section with ID {dto.SectionId} not found on floor {table.FloorId}.");
            }
        }

        // Check for duplicate table number if it changed
        if (!await IsTableNumberUniqueAsync(table.FloorId, dto.TableNumber, id, cancellationToken))
        {
            throw new InvalidOperationException($"Table number '{dto.TableNumber}' already exists on this floor.");
        }

        var oldValues = new
        {
            table.TableNumber,
            table.Capacity,
            table.SectionId,
            table.Shape,
            table.IsActive
        };

        table.TableNumber = dto.TableNumber.Trim();
        table.Capacity = dto.Capacity > 0 ? dto.Capacity : table.Capacity;
        table.SectionId = dto.SectionId;
        table.GridX = dto.GridX;
        table.GridY = dto.GridY;
        table.Width = dto.Width > 0 ? dto.Width : table.Width;
        table.Height = dto.Height > 0 ? dto.Height : table.Height;
        table.Shape = dto.Shape;
        table.IsActive = dto.IsActive;
        table.UpdatedAt = DateTime.UtcNow;
        table.UpdatedByUserId = modifiedByUserId;

        var auditLog = new AuditLog
        {
            UserId = modifiedByUserId,
            Action = "TableUpdated",
            EntityType = nameof(Table),
            EntityId = id,
            OldValues = System.Text.Json.JsonSerializer.Serialize(oldValues),
            NewValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                table.TableNumber,
                table.Capacity,
                table.SectionId,
                table.Shape,
                table.IsActive
            }),
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Table '{TableNumber}' (ID: {TableId}) updated by user {UserId}",
            table.TableNumber, id, modifiedByUserId);

        return table;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteTableAsync(int id, int deletedByUserId, CancellationToken cancellationToken = default)
    {
        var table = await _context.Tables
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (table is null)
        {
            return false;
        }

        if (table.Status == TableStatus.Occupied)
        {
            throw new InvalidOperationException("Cannot delete an occupied table. Please settle the bill first.");
        }

        table.IsActive = false;
        table.UpdatedAt = DateTime.UtcNow;
        table.UpdatedByUserId = deletedByUserId;

        var auditLog = new AuditLog
        {
            UserId = deletedByUserId,
            Action = "TableDeleted",
            EntityType = nameof(Table),
            EntityId = id,
            OldValues = System.Text.Json.JsonSerializer.Serialize(new { table.TableNumber, table.FloorId, table.Capacity }),
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Table '{TableNumber}' (ID: {TableId}) deleted by user {UserId}",
            table.TableNumber, id, deletedByUserId);

        return true;
    }

    /// <inheritdoc />
    public async Task<TableLayoutUpdateResult> UpdateTableLayoutAsync(IEnumerable<TablePositionDto> positions, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(positions);

        var positionsList = positions.ToList();
        if (positionsList.Count == 0)
        {
            return new TableLayoutUpdateResult { Success = true, UpdatedCount = 0 };
        }

        var tableIds = positionsList.Select(p => p.TableId).ToList();
        var tables = await _context.Tables
            .Where(t => tableIds.Contains(t.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var changes = new List<object>();
        var conflicts = new List<TableConcurrencyConflict>();

        foreach (var position in positionsList)
        {
            var table = tables.FirstOrDefault(t => t.Id == position.TableId);
            if (table is null) continue;

            // Check for concurrency conflict if expected version is provided
            if (position.ExpectedRowVersion is not null && position.ExpectedRowVersion.Length > 0)
            {
                if (!table.RowVersion.SequenceEqual(position.ExpectedRowVersion))
                {
                    conflicts.Add(new TableConcurrencyConflict
                    {
                        TableId = table.Id,
                        TableNumber = table.TableNumber,
                        CurrentRowVersion = table.RowVersion
                    });
                    continue;
                }
            }

            if (table.GridX != position.GridX || table.GridY != position.GridY ||
                table.Width != position.Width || table.Height != position.Height)
            {
                changes.Add(new
                {
                    position.TableId,
                    table.TableNumber,
                    OldPosition = new { table.GridX, table.GridY, table.Width, table.Height },
                    NewPosition = new { position.GridX, position.GridY, position.Width, position.Height }
                });

                table.GridX = position.GridX;
                table.GridY = position.GridY;
                table.Width = position.Width > 0 ? position.Width : table.Width;
                table.Height = position.Height > 0 ? position.Height : table.Height;
                table.UpdatedAt = DateTime.UtcNow;
                table.UpdatedByUserId = modifiedByUserId;
            }
        }

        if (changes.Count > 0)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    UserId = modifiedByUserId,
                    Action = "TableLayoutUpdated",
                    EntityType = nameof(Table),
                    NewValues = System.Text.Json.JsonSerializer.Serialize(new { Changes = changes }),
                    MachineName = Environment.MachineName,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);
                await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                _logger.Information("Table layout updated by user {UserId}. Tables updated: {ChangeCount}",
                    modifiedByUserId, changes.Count);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.Warning(ex, "Concurrency conflict during table layout update by user {UserId}", modifiedByUserId);

                // Extract conflicting entries and report them
                foreach (var entry in ex.Entries)
                {
                    if (entry.Entity is Table conflictTable)
                    {
                        var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken).ConfigureAwait(false);
                        if (databaseValues != null)
                        {
                            conflicts.Add(new TableConcurrencyConflict
                            {
                                TableId = conflictTable.Id,
                                TableNumber = conflictTable.TableNumber,
                                CurrentRowVersion = databaseValues.GetValue<byte[]>(nameof(Table.RowVersion)) ?? []
                            });
                        }
                    }
                }

                return new TableLayoutUpdateResult
                {
                    Success = false,
                    UpdatedCount = 0,
                    Conflicts = conflicts
                };
            }
        }

        return new TableLayoutUpdateResult
        {
            Success = conflicts.Count == 0,
            UpdatedCount = changes.Count,
            Conflicts = conflicts
        };
    }

    /// <inheritdoc />
    public async Task<bool> IsTableNumberUniqueAsync(int floorId, string tableNumber, int? excludeTableId = null, CancellationToken cancellationToken = default)
    {
        var trimmedNumber = tableNumber.Trim();

        var query = _context.Tables
            .Where(t => t.FloorId == floorId && t.IsActive)
            .Where(t => t.TableNumber == trimmedNumber);

        if (excludeTableId.HasValue)
        {
            query = query.Where(t => t.Id != excludeTableId.Value);
        }

        return !await query.AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Table>> GetTablesByStatusAsync(TableStatus status, int? floorId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Tables
            .AsNoTracking()
            .Where(t => t.IsActive && t.Status == status);

        if (floorId.HasValue)
        {
            query = query.Where(t => t.FloorId == floorId.Value);
        }

        return await query
            .Include(t => t.Floor)
            .Include(t => t.Section)
            .Include(t => t.CurrentReceipt)
            .Include(t => t.AssignedUser)
            .OrderBy(t => t.Floor.DisplayOrder)
            .ThenBy(t => t.TableNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> GetTableCountByFloorIdAsync(int floorId, CancellationToken cancellationToken = default)
    {
        return await _context.Tables
            .CountAsync(t => t.FloorId == floorId && t.IsActive, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> GetTotalCapacityByFloorIdAsync(int floorId, CancellationToken cancellationToken = default)
    {
        return await _context.Tables
            .Where(t => t.FloorId == floorId && t.IsActive)
            .SumAsync(t => t.Capacity, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Table> UpdateTableStatusAsync(
        int tableId,
        TableStatus status,
        int? receiptId,
        int? assignedUserId,
        int modifiedByUserId,
        CancellationToken cancellationToken = default)
    {
        var table = await _context.Tables
            .FirstOrDefaultAsync(t => t.Id == tableId, cancellationToken)
            .ConfigureAwait(false);

        if (table is null)
        {
            throw new InvalidOperationException($"Table with ID {tableId} not found.");
        }

        var oldValues = new
        {
            table.Status,
            table.CurrentReceiptId,
            table.AssignedUserId,
            table.OccupiedSince
        };

        table.Status = status;
        table.CurrentReceiptId = receiptId;
        table.AssignedUserId = assignedUserId;
        table.UpdatedAt = DateTime.UtcNow;
        table.UpdatedByUserId = modifiedByUserId;

        // Set OccupiedSince when becoming occupied
        if (status == TableStatus.Occupied && oldValues.Status != TableStatus.Occupied)
        {
            table.OccupiedSince = DateTime.UtcNow;
        }
        else if (status == TableStatus.Available)
        {
            table.OccupiedSince = null;
        }

        var auditLog = new AuditLog
        {
            UserId = modifiedByUserId,
            Action = "TableStatusChanged",
            EntityType = nameof(Table),
            EntityId = tableId,
            OldValues = System.Text.Json.JsonSerializer.Serialize(oldValues),
            NewValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                table.Status,
                table.CurrentReceiptId,
                table.AssignedUserId,
                table.OccupiedSince
            }),
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Table '{TableNumber}' status changed from {OldStatus} to {NewStatus} by user {UserId}",
            table.TableNumber, oldValues.Status, status, modifiedByUserId);

        return table;
    }

    /// <inheritdoc />
    public async Task ClearTableAsync(int tableId, int modifiedByUserId, CancellationToken cancellationToken = default)
    {
        var table = await _context.Tables
            .FirstOrDefaultAsync(t => t.Id == tableId, cancellationToken)
            .ConfigureAwait(false);

        if (table is null)
        {
            throw new InvalidOperationException($"Table with ID {tableId} not found.");
        }

        var oldValues = new
        {
            table.Status,
            table.CurrentReceiptId,
            table.AssignedUserId,
            table.OccupiedSince
        };

        table.Status = TableStatus.Available;
        table.CurrentReceiptId = null;
        table.AssignedUserId = null;
        table.OccupiedSince = null;
        table.UpdatedAt = DateTime.UtcNow;
        table.UpdatedByUserId = modifiedByUserId;

        var auditLog = new AuditLog
        {
            UserId = modifiedByUserId,
            Action = "TableCleared",
            EntityType = nameof(Table),
            EntityId = tableId,
            OldValues = System.Text.Json.JsonSerializer.Serialize(oldValues),
            NewValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                table.Status,
                table.CurrentReceiptId,
                table.AssignedUserId,
                table.OccupiedSince
            }),
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Table '{TableNumber}' cleared by user {UserId}", table.TableNumber, modifiedByUserId);
    }

    #endregion
}
