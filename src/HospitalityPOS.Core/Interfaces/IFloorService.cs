using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// DTO for creating or updating a floor.
/// </summary>
public class FloorDto
{
    /// <summary>
    /// Gets or sets the floor name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display order.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Gets or sets the grid width (columns).
    /// </summary>
    public int GridWidth { get; set; } = 10;

    /// <summary>
    /// Gets or sets the grid height (rows).
    /// </summary>
    public int GridHeight { get; set; } = 10;

    /// <summary>
    /// Gets or sets whether the floor is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// DTO for creating or updating a section.
/// </summary>
public class SectionDto
{
    /// <summary>
    /// Gets or sets the section name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the color code (hex format).
    /// </summary>
    public string ColorCode { get; set; } = "#4CAF50";

    /// <summary>
    /// Gets or sets the floor ID.
    /// </summary>
    public int FloorId { get; set; }

    /// <summary>
    /// Gets or sets the display order.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Gets or sets whether the section is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// DTO for creating or updating a table.
/// </summary>
public class TableDto
{
    /// <summary>
    /// Gets or sets the table number.
    /// </summary>
    public string TableNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the seating capacity.
    /// </summary>
    public int Capacity { get; set; } = 4;

    /// <summary>
    /// Gets or sets the floor ID.
    /// </summary>
    public int FloorId { get; set; }

    /// <summary>
    /// Gets or sets the section ID.
    /// </summary>
    public int? SectionId { get; set; }

    /// <summary>
    /// Gets or sets the X position on the grid.
    /// </summary>
    public int GridX { get; set; }

    /// <summary>
    /// Gets or sets the Y position on the grid.
    /// </summary>
    public int GridY { get; set; }

    /// <summary>
    /// Gets or sets the width in grid cells.
    /// </summary>
    public int Width { get; set; } = 1;

    /// <summary>
    /// Gets or sets the height in grid cells.
    /// </summary>
    public int Height { get; set; } = 1;

    /// <summary>
    /// Gets or sets the table shape.
    /// </summary>
    public TableShape Shape { get; set; } = TableShape.Square;

    /// <summary>
    /// Gets or sets whether the table is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// DTO for updating table position.
/// </summary>
public class TablePositionDto
{
    /// <summary>
    /// Gets or sets the table ID.
    /// </summary>
    public int TableId { get; set; }

    /// <summary>
    /// Gets or sets the new X position.
    /// </summary>
    public int GridX { get; set; }

    /// <summary>
    /// Gets or sets the new Y position.
    /// </summary>
    public int GridY { get; set; }

    /// <summary>
    /// Gets or sets the new width.
    /// </summary>
    public int Width { get; set; } = 1;

    /// <summary>
    /// Gets or sets the new height.
    /// </summary>
    public int Height { get; set; } = 1;

    /// <summary>
    /// Gets or sets the expected row version for optimistic concurrency.
    /// If provided, the update will fail if the table has been modified.
    /// </summary>
    public byte[]? ExpectedRowVersion { get; set; }
}

/// <summary>
/// Result of a table layout update operation.
/// </summary>
public class TableLayoutUpdateResult
{
    /// <summary>
    /// Gets or sets whether the update was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the number of tables updated.
    /// </summary>
    public int UpdatedCount { get; set; }

    /// <summary>
    /// Gets or sets any tables that had concurrency conflicts.
    /// </summary>
    public IReadOnlyList<TableConcurrencyConflict> Conflicts { get; set; } = [];
}

/// <summary>
/// Represents a concurrency conflict for a table update.
/// </summary>
public class TableConcurrencyConflict
{
    /// <summary>
    /// Gets or sets the table ID.
    /// </summary>
    public int TableId { get; set; }

    /// <summary>
    /// Gets or sets the table number.
    /// </summary>
    public string TableNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current row version in the database.
    /// </summary>
    public byte[] CurrentRowVersion { get; set; } = [];
}

/// <summary>
/// Service interface for managing floors, sections, and tables.
/// </summary>
public interface IFloorService
{
    #region Floor Operations

    /// <summary>
    /// Gets all floors.
    /// </summary>
    Task<IReadOnlyList<Floor>> GetAllFloorsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active floors with their tables and sections.
    /// </summary>
    Task<IReadOnlyList<Floor>> GetActiveFloorsWithTablesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a floor by ID with its tables and sections.
    /// </summary>
    Task<Floor?> GetFloorByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a floor with all its tables and section information.
    /// </summary>
    Task<Floor?> GetFloorWithTablesAsync(int floorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new floor.
    /// </summary>
    Task<Floor> CreateFloorAsync(FloorDto dto, int createdByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing floor.
    /// </summary>
    Task<Floor> UpdateFloorAsync(int id, FloorDto dto, int modifiedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a floor.
    /// </summary>
    Task<bool> DeleteFloorAsync(int id, int deletedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a floor name is unique.
    /// </summary>
    Task<bool> IsFloorNameUniqueAsync(string name, int? excludeFloorId = null, CancellationToken cancellationToken = default);

    #endregion

    #region Section Operations

    /// <summary>
    /// Gets all sections for a floor.
    /// </summary>
    Task<IReadOnlyList<Section>> GetSectionsByFloorIdAsync(int floorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a section by ID.
    /// </summary>
    Task<Section?> GetSectionByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new section.
    /// </summary>
    Task<Section> CreateSectionAsync(SectionDto dto, int createdByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing section.
    /// </summary>
    Task<Section> UpdateSectionAsync(int id, SectionDto dto, int modifiedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a section.
    /// </summary>
    Task<bool> DeleteSectionAsync(int id, int deletedByUserId, CancellationToken cancellationToken = default);

    #endregion

    #region Table Operations

    /// <summary>
    /// Gets all tables for a floor.
    /// </summary>
    Task<IReadOnlyList<Table>> GetTablesByFloorIdAsync(int floorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all tables by section.
    /// </summary>
    Task<IReadOnlyList<Table>> GetTablesBySectionIdAsync(int sectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a table by ID.
    /// </summary>
    Task<Table?> GetTableByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a table by its number within a floor.
    /// </summary>
    Task<Table?> GetTableByNumberAsync(int floorId, string tableNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new table.
    /// </summary>
    Task<Table> CreateTableAsync(TableDto dto, int createdByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing table.
    /// </summary>
    Task<Table> UpdateTableAsync(int id, TableDto dto, int modifiedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a table.
    /// </summary>
    Task<bool> DeleteTableAsync(int id, int deletedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates table layout positions with optimistic concurrency control.
    /// </summary>
    /// <param name="positions">The table positions to update.</param>
    /// <param name="modifiedByUserId">The user making the changes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or any concurrency conflicts.</returns>
    Task<TableLayoutUpdateResult> UpdateTableLayoutAsync(IEnumerable<TablePositionDto> positions, int modifiedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a table number is unique within a floor.
    /// </summary>
    Task<bool> IsTableNumberUniqueAsync(int floorId, string tableNumber, int? excludeTableId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tables by status.
    /// </summary>
    Task<IReadOnlyList<Table>> GetTablesByStatusAsync(TableStatus status, int? floorId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of tables on a floor.
    /// </summary>
    Task<int> GetTableCountByFloorIdAsync(int floorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total seating capacity for a floor.
    /// </summary>
    Task<int> GetTotalCapacityByFloorIdAsync(int floorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the status of a table.
    /// </summary>
    /// <param name="tableId">The table ID.</param>
    /// <param name="status">The new status.</param>
    /// <param name="receiptId">The receipt ID if occupied.</param>
    /// <param name="assignedUserId">The assigned user ID.</param>
    /// <param name="modifiedByUserId">The user making the change.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Table> UpdateTableStatusAsync(
        int tableId,
        TableStatus status,
        int? receiptId,
        int? assignedUserId,
        int modifiedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the table status after bill settlement.
    /// </summary>
    /// <param name="tableId">The table ID.</param>
    /// <param name="modifiedByUserId">The user making the change.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ClearTableAsync(int tableId, int modifiedByUserId, CancellationToken cancellationToken = default);

    #endregion
}
