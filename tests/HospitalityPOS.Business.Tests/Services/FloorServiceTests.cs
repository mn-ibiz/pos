using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;
using HospitalityPOS.Infrastructure.Services;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for the FloorService class.
/// Tests cover floor, section, and table management operations.
/// </summary>
public class FloorServiceTests : IDisposable
{
    private readonly POSDbContext _context;
    private readonly Mock<ILogger> _loggerMock;
    private readonly FloorService _floorService;
    private const int TestUserId = 1;

    public FloorServiceTests()
    {
        var options = new DbContextOptionsBuilder<POSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new POSDbContext(options);
        _loggerMock = new Mock<ILogger>();

        _floorService = new FloorService(_context, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Helper Methods

    private async Task<Floor> CreateTestFloorAsync(
        string name = "Main Floor",
        int displayOrder = 1,
        int gridWidth = 10,
        int gridHeight = 10,
        bool isActive = true)
    {
        var floor = new Floor
        {
            Name = name,
            DisplayOrder = displayOrder,
            GridWidth = gridWidth,
            GridHeight = gridHeight,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = TestUserId
        };

        _context.Floors.Add(floor);
        await _context.SaveChangesAsync();

        return floor;
    }

    private async Task<Section> CreateTestSectionAsync(
        int floorId,
        string name = "Section A",
        string colorCode = "#4CAF50",
        int displayOrder = 1,
        bool isActive = true)
    {
        var section = new Section
        {
            Name = name,
            ColorCode = colorCode,
            FloorId = floorId,
            DisplayOrder = displayOrder,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = TestUserId
        };

        _context.Sections.Add(section);
        await _context.SaveChangesAsync();

        return section;
    }

    private async Task<Table> CreateTestTableAsync(
        int floorId,
        string tableNumber = "T1",
        int capacity = 4,
        int? sectionId = null,
        TableShape shape = TableShape.Square,
        TableStatus status = TableStatus.Available,
        int gridX = 0,
        int gridY = 0,
        bool isActive = true)
    {
        var table = new Table
        {
            TableNumber = tableNumber,
            Capacity = capacity,
            FloorId = floorId,
            SectionId = sectionId,
            Shape = shape,
            Status = status,
            GridX = gridX,
            GridY = gridY,
            Width = 1,
            Height = 1,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = TestUserId
        };

        _context.Tables.Add(table);
        await _context.SaveChangesAsync();

        return table;
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new FloorService(null!, _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new FloorService(_context, null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region Floor Operations Tests

    [Fact]
    public async Task GetAllFloorsAsync_ShouldReturnAllFloors_OrderedByDisplayOrder()
    {
        // Arrange
        await CreateTestFloorAsync("Third Floor", 3);
        await CreateTestFloorAsync("First Floor", 1);
        await CreateTestFloorAsync("Second Floor", 2);

        // Act
        var floors = await _floorService.GetAllFloorsAsync();

        // Assert
        floors.Should().HaveCount(3);
        floors[0].Name.Should().Be("First Floor");
        floors[1].Name.Should().Be("Second Floor");
        floors[2].Name.Should().Be("Third Floor");
    }

    [Fact]
    public async Task GetActiveFloorsWithTablesAsync_ShouldReturnOnlyActiveFloors()
    {
        // Arrange
        var activeFloor = await CreateTestFloorAsync("Active Floor", isActive: true);
        await CreateTestFloorAsync("Inactive Floor", isActive: false);
        await CreateTestTableAsync(activeFloor.Id, "T1");

        // Act
        var floors = await _floorService.GetActiveFloorsWithTablesAsync();

        // Assert
        floors.Should().HaveCount(1);
        floors[0].Name.Should().Be("Active Floor");
        floors[0].Tables.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetFloorByIdAsync_ShouldReturnFloor_WhenExists()
    {
        // Arrange
        var floor = await CreateTestFloorAsync("Test Floor");

        // Act
        var result = await _floorService.GetFloorByIdAsync(floor.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Floor");
    }

    [Fact]
    public async Task GetFloorByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _floorService.GetFloorByIdAsync(99999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetFloorWithTablesAsync_ShouldReturnFloorWithTables()
    {
        // Arrange
        var floor = await CreateTestFloorAsync("Test Floor");
        await CreateTestTableAsync(floor.Id, "T1");
        await CreateTestTableAsync(floor.Id, "T2");
        await CreateTestTableAsync(floor.Id, "T3", isActive: false);

        // Act
        var result = await _floorService.GetFloorWithTablesAsync(floor.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Tables.Should().HaveCount(2); // Only active tables
    }

    [Fact]
    public async Task CreateFloorAsync_ShouldCreateFloor_WithValidData()
    {
        // Arrange
        var dto = new FloorDto
        {
            Name = "New Floor",
            DisplayOrder = 1,
            GridWidth = 12,
            GridHeight = 8,
            IsActive = true
        };

        // Act
        var result = await _floorService.CreateFloorAsync(dto, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Floor");
        result.GridWidth.Should().Be(12);
        result.GridHeight.Should().Be(8);

        var savedFloor = await _context.Floors.FindAsync(result.Id);
        savedFloor.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateFloorAsync_ShouldTrimName()
    {
        // Arrange
        var dto = new FloorDto
        {
            Name = "  Trimmed Floor  ",
            DisplayOrder = 1,
            GridWidth = 10,
            GridHeight = 10,
            IsActive = true
        };

        // Act
        var result = await _floorService.CreateFloorAsync(dto, TestUserId);

        // Assert
        result.Name.Should().Be("Trimmed Floor");
    }

    [Fact]
    public async Task CreateFloorAsync_ShouldThrow_WhenNameIsDuplicate()
    {
        // Arrange
        await CreateTestFloorAsync("Existing Floor");

        var dto = new FloorDto
        {
            Name = "Existing Floor",
            DisplayOrder = 2,
            GridWidth = 10,
            GridHeight = 10,
            IsActive = true
        };

        // Act
        var action = () => _floorService.CreateFloorAsync(dto, TestUserId);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task CreateFloorAsync_ShouldThrow_WhenNameIsNull()
    {
        // Arrange
        var dto = new FloorDto
        {
            Name = null!,
            DisplayOrder = 1,
            GridWidth = 10,
            GridHeight = 10,
            IsActive = true
        };

        // Act
        var action = () => _floorService.CreateFloorAsync(dto, TestUserId);

        // Assert
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateFloorAsync_ShouldCreateAuditLog()
    {
        // Arrange
        var dto = new FloorDto
        {
            Name = "Audited Floor",
            DisplayOrder = 1,
            GridWidth = 10,
            GridHeight = 10,
            IsActive = true
        };

        // Act
        var result = await _floorService.CreateFloorAsync(dto, TestUserId);

        // Assert
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.EntityType == nameof(Floor) && a.EntityId == result.Id);
        auditLog.Should().NotBeNull();
        auditLog!.Action.Should().Be("FloorCreated");
        auditLog.UserId.Should().Be(TestUserId);
    }

    [Fact]
    public async Task UpdateFloorAsync_ShouldUpdateFloor_WithValidData()
    {
        // Arrange
        var floor = await CreateTestFloorAsync("Original Floor");
        var dto = new FloorDto
        {
            Name = "Updated Floor",
            DisplayOrder = 2,
            GridWidth = 15,
            GridHeight = 12,
            IsActive = true
        };

        // Act
        var result = await _floorService.UpdateFloorAsync(floor.Id, dto, TestUserId);

        // Assert
        result.Name.Should().Be("Updated Floor");
        result.DisplayOrder.Should().Be(2);
        result.GridWidth.Should().Be(15);
        result.GridHeight.Should().Be(12);
    }

    [Fact]
    public async Task UpdateFloorAsync_ShouldThrow_WhenFloorNotFound()
    {
        // Arrange
        var dto = new FloorDto
        {
            Name = "Updated Floor",
            DisplayOrder = 1,
            GridWidth = 10,
            GridHeight = 10,
            IsActive = true
        };

        // Act
        var action = () => _floorService.UpdateFloorAsync(99999, dto, TestUserId);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task UpdateFloorAsync_ShouldThrow_WhenNameIsDuplicate()
    {
        // Arrange
        await CreateTestFloorAsync("Other Floor");
        var floor = await CreateTestFloorAsync("Original Floor");

        var dto = new FloorDto
        {
            Name = "Other Floor",
            DisplayOrder = 1,
            GridWidth = 10,
            GridHeight = 10,
            IsActive = true
        };

        // Act
        var action = () => _floorService.UpdateFloorAsync(floor.Id, dto, TestUserId);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task DeleteFloorAsync_ShouldSoftDeleteFloor()
    {
        // Arrange
        var floor = await CreateTestFloorAsync("Floor to Delete");

        // Act
        var result = await _floorService.DeleteFloorAsync(floor.Id, TestUserId);

        // Assert
        result.Should().BeTrue();

        var deletedFloor = await _context.Floors.FindAsync(floor.Id);
        deletedFloor!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteFloorAsync_ShouldDeactivateTablesOnFloor()
    {
        // Arrange
        var floor = await CreateTestFloorAsync("Floor with Tables");
        await CreateTestTableAsync(floor.Id, "T1");
        await CreateTestTableAsync(floor.Id, "T2");

        // Act
        await _floorService.DeleteFloorAsync(floor.Id, TestUserId);

        // Assert
        var tables = await _context.Tables.Where(t => t.FloorId == floor.Id).ToListAsync();
        tables.Should().AllSatisfy(t => t.IsActive.Should().BeFalse());
    }

    [Fact]
    public async Task DeleteFloorAsync_ShouldThrow_WhenFloorHasOccupiedTables()
    {
        // Arrange
        var floor = await CreateTestFloorAsync("Floor with Occupied Table");
        await CreateTestTableAsync(floor.Id, "T1", status: TableStatus.Occupied);

        // Act
        var action = () => _floorService.DeleteFloorAsync(floor.Id, TestUserId);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*occupied tables*");
    }

    [Fact]
    public async Task DeleteFloorAsync_ShouldReturnFalse_WhenFloorNotFound()
    {
        // Act
        var result = await _floorService.DeleteFloorAsync(99999, TestUserId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsFloorNameUniqueAsync_ShouldReturnTrue_WhenNameIsUnique()
    {
        // Arrange
        await CreateTestFloorAsync("Existing Floor");

        // Act
        var result = await _floorService.IsFloorNameUniqueAsync("New Floor");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsFloorNameUniqueAsync_ShouldReturnFalse_WhenNameExists()
    {
        // Arrange
        await CreateTestFloorAsync("Existing Floor");

        // Act
        var result = await _floorService.IsFloorNameUniqueAsync("Existing Floor");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsFloorNameUniqueAsync_ShouldExcludeSpecifiedFloor()
    {
        // Arrange
        var floor = await CreateTestFloorAsync("Existing Floor");

        // Act
        var result = await _floorService.IsFloorNameUniqueAsync("Existing Floor", floor.Id);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Section Operations Tests

    [Fact]
    public async Task GetSectionsByFloorIdAsync_ShouldReturnActiveSections()
    {
        // Arrange
        var floor = await CreateTestFloorAsync("Test Floor");
        await CreateTestSectionAsync(floor.Id, "Section A", displayOrder: 1);
        await CreateTestSectionAsync(floor.Id, "Section B", displayOrder: 2);
        await CreateTestSectionAsync(floor.Id, "Inactive Section", isActive: false);

        // Act
        var sections = await _floorService.GetSectionsByFloorIdAsync(floor.Id);

        // Assert
        sections.Should().HaveCount(2);
        sections[0].Name.Should().Be("Section A");
        sections[1].Name.Should().Be("Section B");
    }

    [Fact]
    public async Task GetSectionByIdAsync_ShouldReturnSection_WhenExists()
    {
        // Arrange
        var floor = await CreateTestFloorAsync("Test Floor");
        var section = await CreateTestSectionAsync(floor.Id, "Test Section");

        // Act
        var result = await _floorService.GetSectionByIdAsync(section.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Section");
    }

    [Fact]
    public async Task CreateSectionAsync_ShouldCreateSection_WithValidData()
    {
        // Arrange
        var floor = await CreateTestFloorAsync("Test Floor");
        var dto = new SectionDto
        {
            Name = "New Section",
            ColorCode = "#FF5722",
            FloorId = floor.Id,
            DisplayOrder = 1,
            IsActive = true
        };

        // Act
        var result = await _floorService.CreateSectionAsync(dto, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Section");
        result.ColorCode.Should().Be("#FF5722");
        result.FloorId.Should().Be(floor.Id);
    }

    [Fact]
    public async Task CreateSectionAsync_ShouldUseDefaultColor_WhenColorCodeIsEmpty()
    {
        // Arrange
        var floor = await CreateTestFloorAsync("Test Floor");
        var dto = new SectionDto
        {
            Name = "Default Color Section",
            ColorCode = "",
            FloorId = floor.Id,
            DisplayOrder = 1,
            IsActive = true
        };

        // Act
        var result = await _floorService.CreateSectionAsync(dto, TestUserId);

        // Assert
        result.ColorCode.Should().Be("#4CAF50");
    }

    [Fact]
    public async Task CreateSectionAsync_ShouldThrow_WhenFloorNotFound()
    {
        // Arrange
        var dto = new SectionDto
        {
            Name = "New Section",
            ColorCode = "#FF5722",
            FloorId = 99999,
            DisplayOrder = 1,
            IsActive = true
        };

        // Act
        var action = () => _floorService.CreateSectionAsync(dto, TestUserId);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Floor*not found*");
    }

    [Fact]
    public async Task CreateSectionAsync_ShouldThrow_WhenNameIsDuplicate()
    {
        // Arrange
        var floor = await CreateTestFloorAsync("Test Floor");
        await CreateTestSectionAsync(floor.Id, "Existing Section");

        var dto = new SectionDto
        {
            Name = "Existing Section",
            ColorCode = "#FF5722",
            FloorId = floor.Id,
            DisplayOrder = 2,
            IsActive = true
        };

        // Act
        var action = () => _floorService.CreateSectionAsync(dto, TestUserId);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task UpdateSectionAsync_ShouldUpdateSection_WithValidData()
    {
        // Arrange
        var floor = await CreateTestFloorAsync("Test Floor");
        var section = await CreateTestSectionAsync(floor.Id, "Original Section");

        var dto = new SectionDto
        {
            Name = "Updated Section",
            ColorCode = "#2196F3",
            FloorId = floor.Id,
            DisplayOrder = 2,
            IsActive = true
        };

        // Act
        var result = await _floorService.UpdateSectionAsync(section.Id, dto, TestUserId);

        // Assert
        result.Name.Should().Be("Updated Section");
        result.ColorCode.Should().Be("#2196F3");
        result.DisplayOrder.Should().Be(2);
    }

    [Fact]
    public async Task UpdateSectionAsync_ShouldThrow_WhenSectionNotFound()
    {
        // Arrange
        var dto = new SectionDto
        {
            Name = "Updated Section",
            ColorCode = "#2196F3",
            FloorId = 1,
            DisplayOrder = 1,
            IsActive = true
        };

        // Act
        var action = () => _floorService.UpdateSectionAsync(99999, dto, TestUserId);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task DeleteSectionAsync_ShouldSoftDeleteSection()
    {
        // Arrange
        var floor = await CreateTestFloorAsync("Test Floor");
        var section = await CreateTestSectionAsync(floor.Id, "Section to Delete");

        // Act
        var result = await _floorService.DeleteSectionAsync(section.Id, TestUserId);

        // Assert
        result.Should().BeTrue();

        var deletedSection = await _context.Sections.FindAsync(section.Id);
        deletedSection!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteSectionAsync_ShouldRemoveSectionFromTables()
    {
        // Arrange
        var floor = await CreateTestFloorAsync("Test Floor");
        var section = await CreateTestSectionAsync(floor.Id, "Section with Tables");
        await CreateTestTableAsync(floor.Id, "T1", sectionId: section.Id);
        await CreateTestTableAsync(floor.Id, "T2", sectionId: section.Id);

        // Act
        await _floorService.DeleteSectionAsync(section.Id, TestUserId);

        // Assert
        var tables = await _context.Tables.Where(t => t.FloorId == floor.Id).ToListAsync();
        tables.Should().AllSatisfy(t => t.SectionId.Should().BeNull());
    }

    [Fact]
    public async Task DeleteSectionAsync_ShouldReturnFalse_WhenSectionNotFound()
    {
        // Act
        var result = await _floorService.DeleteSectionAsync(99999, TestUserId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Table Operations Tests

    [Fact]
    public async Task GetTablesByFloorIdAsync_ShouldReturnActiveTables()
    {
        // Arrange
        var floor = await CreateTestFloorAsync("Test Floor");
        await CreateTestTableAsync(floor.Id, "T1");
        await CreateTestTableAsync(floor.Id, "T2");
        await CreateTestTableAsync(floor.Id, "T3", isActive: false);

        // Act
        var tables = await _floorService.GetTablesByFloorIdAsync(floor.Id);

        // Assert
        tables.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTablesBySectionIdAsync_ShouldReturnTablesInSection()
    {
        // Arrange
        var floor = await CreateTestFloorAsync("Test Floor");
        var section = await CreateTestSectionAsync(floor.Id, "Test Section");
        await CreateTestTableAsync(floor.Id, "T1", sectionId: section.Id);
        await CreateTestTableAsync(floor.Id, "T2", sectionId: section.Id);
        await CreateTestTableAsync(floor.Id, "T3"); // No section

        // Act
        var tables = await _floorService.GetTablesBySectionIdAsync(section.Id);

        // Assert
        tables.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTableByIdAsync_ShouldReturnTable_WhenExists()
    {
        // Arrange
        var floor = await CreateTestFloorAsync("Test Floor");
        var table = await CreateTestTableAsync(floor.Id, "T1");

        // Act
        var result = await _floorService.GetTableByIdAsync(table.Id);

        // Assert
        result.Should().NotBeNull();
        result!.TableNumber.Should().Be("T1");
    }

    [Fact]
    public async Task GetTableByNumberAsync_ShouldReturnTable_WhenExists()
    {
        // Arrange
        var floor = await CreateTestFloorAsync("Test Floor");
        await CreateTestTableAsync(floor.Id, "T1");

        // Act
        var result = await _floorService.GetTableByNumberAsync(floor.Id, "T1");

        // Assert
        result.Should().NotBeNull();
        result!.TableNumber.Should().Be("T1");
    }

    [Fact]
    public async Task GetTableByNumberAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        var floor = await CreateTestFloorAsync("Test Floor");

        // Act
        var result = await _floorService.GetTableByNumberAsync(floor.Id, "NonExistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateTableAsync_ShouldCreateTable_WithValidData()
    {
        // Arrange
        var floor = await CreateTestFloorAsync("Test Floor");
        var dto = new TableDto
        {
            TableNumber = "T1",
            Capacity = 6,
            FloorId = floor.Id,
            Shape = TableShape.Round,
            GridX = 2,
            GridY = 3,
            Width = 2,
            Height = 2,
            IsActive = true
        };

        // Act
        var result = await _floorService.CreateTableAsync(dto, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result.TableNumber.Should().Be("T1");
        result.Capacity.Should().Be(6);
        result.Shape.Should().Be(TableShape.Round);
        result.Status.Should().Be(TableStatus.Available);
        result.GridX.Should().Be(2);
        result.GridY.Should().Be(3);
    }

    [Fact]
    public async Task CreateTableAsync_ShouldThrow_WhenFloorNotFound()
    {
        // Arrange
        var dto = new TableDto
        {
            TableNumber = "T1",
            Capacity = 4,
            FloorId = 99999,
            Shape = TableShape.Square,
            GridX = 0,
            GridY = 0,
            Width = 1,
            Height = 1,
            IsActive = true
        };

        // Act
        var action = () => _floorService.CreateTableAsync(dto, TestUserId);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Floor*not found*");
    }

    [Fact]
    public async Task CreateTableAsync_ShouldThrow_WhenSectionNotOnFloor()
    {
        // Arrange
        var floor1 = await CreateTestFloorAsync("Floor 1");
        var floor2 = await CreateTestFloorAsync("Floor 2");
        var section = await CreateTestSectionAsync(floor2.Id, "Section on Floor 2");

        var dto = new TableDto
        {
            TableNumber = "T1",
            Capacity = 4,
            FloorId = floor1.Id,
            SectionId = section.Id, // Section from different floor
            Shape = TableShape.Square,
            GridX = 0,
            GridY = 0,
            Width = 1,
            Height = 1,
            IsActive = true
        };

        // Act
        var action = () => _floorService.CreateTableAsync(dto, TestUserId);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Section*not found*");
    }

    [Fact]
    public async Task CreateTableAsync_ShouldThrow_WhenTableNumberIsDuplicate()
    {
        // Arrange
        var floor = await CreateTestFloorAsync("Test Floor");
        await CreateTestTableAsync(floor.Id, "T1");

        var dto = new TableDto
        {
            TableNumber = "T1",
            Capacity = 4,
            FloorId = floor.Id,
            Shape = TableShape.Square,
            GridX = 1,
            GridY = 0,
            Width = 1,
            Height = 1,
            IsActive = true
        };

        // Act
        var action = () => _floorService.CreateTableAsync(dto, TestUserId);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task UpdateTableAsync_ShouldUpdateTable_WithValidData()
    {
        // Arrange
        var floor = await CreateTestFloorAsync("Test Floor");
        var table = await CreateTestTableAsync(floor.Id, "T1", capacity: 4);

        var dto = new TableDto
        {
            TableNumber = "T1-Updated",
            Capacity = 8,
            FloorId = floor.Id,
            Shape = TableShape.Rectangle,
            GridX = 5,
            GridY = 5,
            Width = 2,
            Height = 1,
            IsActive = true
        };

        // Act
        var result = await _floorService.UpdateTableAsync(table.Id, dto, TestUserId);

        // Assert
        result.TableNumber.Should().Be("T1-Updated");
        result.Capacity.Should().Be(8);
        result.Shape.Should().Be(TableShape.Rectangle);
    }

    [Fact]
    public async Task UpdateTableAsync_ShouldThrow_WhenTableNotFound()
    {
        // Arrange
        var dto = new TableDto
        {
            TableNumber = "T1",
            Capacity = 4,
            FloorId = 1,
            Shape = TableShape.Square,
            GridX = 0,
            GridY = 0,
            Width = 1,
            Height = 1,
            IsActive = true
        };

        // Act
        var action = () => _floorService.UpdateTableAsync(99999, dto, TestUserId);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task DeleteTableAsync_ShouldSoftDeleteTable()
    {
        // Arrange
        var floor = await CreateTestFloorAsync("Test Floor");
        var table = await CreateTestTableAsync(floor.Id, "T1");

        // Act
        var result = await _floorService.DeleteTableAsync(table.Id, TestUserId);

        // Assert
        result.Should().BeTrue();

        var deletedTable = await _context.Tables.FindAsync(table.Id);
        deletedTable!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteTableAsync_ShouldThrow_WhenTableIsOccupied()
    {
        // Arrange
        var floor = await CreateTestFloorAsync("Test Floor");
        var table = await CreateTestTableAsync(floor.Id, "T1", status: TableStatus.Occupied);

        // Act
        var action = () => _floorService.DeleteTableAsync(table.Id, TestUserId);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*occupied*");
    }

    [Fact]
    public async Task DeleteTableAsync_ShouldReturnFalse_WhenTableNotFound()
    {
        // Act
        var result = await _floorService.DeleteTableAsync(99999, TestUserId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateTableLayoutAsync_ShouldUpdateTablePositions()
    {
        // Arrange
        var floor = await CreateTestFloorAsync("Test Floor");
        var table1 = await CreateTestTableAsync(floor.Id, "T1", gridX: 0, gridY: 0);
        var table2 = await CreateTestTableAsync(floor.Id, "T2", gridX: 1, gridY: 0);

        var positions = new List<TablePositionDto>
        {
            new() { TableId = table1.Id, GridX = 5, GridY = 5, Width = 1, Height = 1 },
            new() { TableId = table2.Id, GridX = 6, GridY = 5, Width = 2, Height = 2 }
        };

        // Act
        await _floorService.UpdateTableLayoutAsync(positions, TestUserId);

        // Assert
        var updatedTable1 = await _context.Tables.FindAsync(table1.Id);
        var updatedTable2 = await _context.Tables.FindAsync(table2.Id);

        updatedTable1!.GridX.Should().Be(5);
        updatedTable1.GridY.Should().Be(5);
        updatedTable2!.GridX.Should().Be(6);
        updatedTable2.GridY.Should().Be(5);
        updatedTable2.Width.Should().Be(2);
        updatedTable2.Height.Should().Be(2);
    }

    [Fact]
    public async Task UpdateTableLayoutAsync_ShouldNotCreateAuditLog_WhenNoChanges()
    {
        // Arrange
        var floor = await CreateTestFloorAsync("Test Floor");
        var table = await CreateTestTableAsync(floor.Id, "T1", gridX: 5, gridY: 5);

        var initialAuditCount = await _context.AuditLogs.CountAsync();

        var positions = new List<TablePositionDto>
        {
            new() { TableId = table.Id, GridX = 5, GridY = 5, Width = 1, Height = 1 } // Same position
        };

        // Act
        await _floorService.UpdateTableLayoutAsync(positions, TestUserId);

        // Assert
        var finalAuditCount = await _context.AuditLogs.CountAsync();
        finalAuditCount.Should().Be(initialAuditCount); // No new audit logs
    }

    [Fact]
    public async Task IsTableNumberUniqueAsync_ShouldReturnTrue_WhenNumberIsUnique()
    {
        // Arrange
        var floor = await CreateTestFloorAsync("Test Floor");
        await CreateTestTableAsync(floor.Id, "T1");

        // Act
        var result = await _floorService.IsTableNumberUniqueAsync(floor.Id, "T2");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsTableNumberUniqueAsync_ShouldReturnFalse_WhenNumberExists()
    {
        // Arrange
        var floor = await CreateTestFloorAsync("Test Floor");
        await CreateTestTableAsync(floor.Id, "T1");

        // Act
        var result = await _floorService.IsTableNumberUniqueAsync(floor.Id, "T1");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsTableNumberUniqueAsync_ShouldExcludeSpecifiedTable()
    {
        // Arrange
        var floor = await CreateTestFloorAsync("Test Floor");
        var table = await CreateTestTableAsync(floor.Id, "T1");

        // Act
        var result = await _floorService.IsTableNumberUniqueAsync(floor.Id, "T1", table.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetTablesByStatusAsync_ShouldReturnTablesByStatus()
    {
        // Arrange
        var floor = await CreateTestFloorAsync("Test Floor");
        await CreateTestTableAsync(floor.Id, "T1", status: TableStatus.Available);
        await CreateTestTableAsync(floor.Id, "T2", status: TableStatus.Occupied);
        await CreateTestTableAsync(floor.Id, "T3", status: TableStatus.Available);
        await CreateTestTableAsync(floor.Id, "T4", status: TableStatus.Reserved);

        // Act
        var availableTables = await _floorService.GetTablesByStatusAsync(TableStatus.Available);

        // Assert
        availableTables.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTablesByStatusAsync_ShouldFilterByFloor()
    {
        // Arrange
        var floor1 = await CreateTestFloorAsync("Floor 1");
        var floor2 = await CreateTestFloorAsync("Floor 2");
        await CreateTestTableAsync(floor1.Id, "T1", status: TableStatus.Available);
        await CreateTestTableAsync(floor2.Id, "T2", status: TableStatus.Available);

        // Act
        var tables = await _floorService.GetTablesByStatusAsync(TableStatus.Available, floor1.Id);

        // Assert
        tables.Should().HaveCount(1);
        tables[0].TableNumber.Should().Be("T1");
    }

    [Fact]
    public async Task GetTableCountByFloorIdAsync_ShouldReturnCount()
    {
        // Arrange
        var floor = await CreateTestFloorAsync("Test Floor");
        await CreateTestTableAsync(floor.Id, "T1");
        await CreateTestTableAsync(floor.Id, "T2");
        await CreateTestTableAsync(floor.Id, "T3", isActive: false);

        // Act
        var count = await _floorService.GetTableCountByFloorIdAsync(floor.Id);

        // Assert
        count.Should().Be(2); // Only active tables
    }

    [Fact]
    public async Task GetTotalCapacityByFloorIdAsync_ShouldReturnTotalCapacity()
    {
        // Arrange
        var floor = await CreateTestFloorAsync("Test Floor");
        await CreateTestTableAsync(floor.Id, "T1", capacity: 4);
        await CreateTestTableAsync(floor.Id, "T2", capacity: 6);
        await CreateTestTableAsync(floor.Id, "T3", capacity: 2, isActive: false);

        // Act
        var capacity = await _floorService.GetTotalCapacityByFloorIdAsync(floor.Id);

        // Assert
        capacity.Should().Be(10); // 4 + 6, inactive table not counted
    }

    #endregion
}
