using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Serilog;
using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Infrastructure.Data;
using HospitalityPOS.Infrastructure.Services;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for the TerminalService class.
/// Tests cover terminal CRUD operations, validation, registration, and health monitoring.
/// </summary>
public class TerminalServiceTests : IDisposable
{
    private readonly POSDbContext _context;
    private readonly Mock<ILogger> _loggerMock;
    private readonly TerminalService _terminalService;
    private const int TestUserId = 1;
    private const int TestStoreId = 1;

    public TerminalServiceTests()
    {
        var options = new DbContextOptionsBuilder<POSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new POSDbContext(options);
        _loggerMock = new Mock<ILogger>();

        _terminalService = new TerminalService(_context, _loggerMock.Object);

        // Seed test store
        SeedTestStore();
    }

    private void SeedTestStore()
    {
        var store = new Store
        {
            Id = TestStoreId,
            Name = "Test Store",
            Code = "TS001",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = TestUserId
        };
        _context.Stores.Add(store);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Helper Methods

    private async Task<Terminal> CreateTestTerminalAsync(
        string code = "REG-001",
        string name = "Register 1",
        TerminalType terminalType = TerminalType.Register,
        BusinessMode businessMode = BusinessMode.Supermarket,
        bool isActive = true,
        string? machineIdentifier = null,
        DateTime? lastHeartbeat = null,
        int? storeId = null)
    {
        var terminal = new Terminal
        {
            StoreId = storeId ?? TestStoreId,
            Code = code,
            Name = name,
            TerminalType = terminalType,
            BusinessMode = businessMode,
            MachineIdentifier = machineIdentifier ?? string.Empty,
            IsActive = isActive,
            LastHeartbeat = lastHeartbeat,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = TestUserId
        };

        _context.Terminals.Add(terminal);
        await _context.SaveChangesAsync();

        return terminal;
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new TerminalService(null!, _loggerMock.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new TerminalService(_context, null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region Create Terminal Tests

    [Fact]
    public async Task CreateTerminalAsync_ShouldCreateTerminal_WithValidData()
    {
        // Arrange
        var request = new CreateTerminalRequest
        {
            StoreId = TestStoreId,
            Code = "REG-001",
            Name = "Register 1",
            Description = "Main register",
            TerminalType = TerminalType.Register,
            BusinessMode = BusinessMode.Supermarket,
            IsMainRegister = true
        };

        // Act
        var result = await _terminalService.CreateTerminalAsync(request, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result.Code.Should().Be("REG-001");
        result.Name.Should().Be("Register 1");
        result.TerminalType.Should().Be(TerminalType.Register);
        result.IsMainRegister.Should().BeTrue();
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateTerminalAsync_ShouldTrimCodeAndName()
    {
        // Arrange
        var request = new CreateTerminalRequest
        {
            StoreId = TestStoreId,
            Code = "  REG-001  ",
            Name = "  Register 1  ",
            TerminalType = TerminalType.Register,
            BusinessMode = BusinessMode.Supermarket
        };

        // Act
        var result = await _terminalService.CreateTerminalAsync(request, TestUserId);

        // Assert
        result.Code.Should().Be("REG-001");
        result.Name.Should().Be("Register 1");
    }

    [Fact]
    public async Task CreateTerminalAsync_ShouldUppercaseCode()
    {
        // Arrange
        var request = new CreateTerminalRequest
        {
            StoreId = TestStoreId,
            Code = "reg-001",
            Name = "Register 1",
            TerminalType = TerminalType.Register,
            BusinessMode = BusinessMode.Supermarket
        };

        // Act
        var result = await _terminalService.CreateTerminalAsync(request, TestUserId);

        // Assert
        result.Code.Should().Be("REG-001");
    }

    [Fact]
    public async Task CreateTerminalAsync_ShouldThrow_WhenCodeIsDuplicate()
    {
        // Arrange
        await CreateTestTerminalAsync(code: "REG-001");

        var request = new CreateTerminalRequest
        {
            StoreId = TestStoreId,
            Code = "REG-001",
            Name = "Another Register",
            TerminalType = TerminalType.Register,
            BusinessMode = BusinessMode.Supermarket
        };

        // Act
        var action = () => _terminalService.CreateTerminalAsync(request, TestUserId);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task CreateTerminalAsync_ShouldCreateAuditLog()
    {
        // Arrange
        var request = new CreateTerminalRequest
        {
            StoreId = TestStoreId,
            Code = "REG-001",
            Name = "Register 1",
            TerminalType = TerminalType.Register,
            BusinessMode = BusinessMode.Supermarket
        };

        // Act
        var result = await _terminalService.CreateTerminalAsync(request, TestUserId);

        // Assert
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.EntityType == nameof(Terminal) && a.EntityId == result.Id);
        auditLog.Should().NotBeNull();
        auditLog!.Action.Should().Be("TerminalCreated");
        auditLog.UserId.Should().Be(TestUserId);
    }

    #endregion

    #region Get Terminal Tests

    [Fact]
    public async Task GetTerminalByIdAsync_ShouldReturnTerminal_WhenExists()
    {
        // Arrange
        var terminal = await CreateTestTerminalAsync();

        // Act
        var result = await _terminalService.GetTerminalByIdAsync(terminal.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be("REG-001");
    }

    [Fact]
    public async Task GetTerminalByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _terminalService.GetTerminalByIdAsync(99999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTerminalByCodeAsync_ShouldReturnTerminal_WhenExists()
    {
        // Arrange
        await CreateTestTerminalAsync(code: "REG-001");

        // Act
        var result = await _terminalService.GetTerminalByCodeAsync(TestStoreId, "REG-001");

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be("REG-001");
    }

    [Fact]
    public async Task GetTerminalByCodeAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _terminalService.GetTerminalByCodeAsync(TestStoreId, "NONEXISTENT");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTerminalByMachineIdAsync_ShouldReturnTerminal_WhenExists()
    {
        // Arrange
        await CreateTestTerminalAsync(machineIdentifier: "AA:BB:CC:DD:EE:FF");

        // Act
        var result = await _terminalService.GetTerminalByMachineIdAsync("AA:BB:CC:DD:EE:FF");

        // Assert
        result.Should().NotBeNull();
        result!.MachineIdentifier.Should().Be("AA:BB:CC:DD:EE:FF");
    }

    [Fact]
    public async Task GetTerminalsByStoreAsync_ShouldReturnAllTerminals()
    {
        // Arrange
        await CreateTestTerminalAsync(code: "REG-001");
        await CreateTestTerminalAsync(code: "REG-002");
        await CreateTestTerminalAsync(code: "TILL-001", terminalType: TerminalType.Till);

        // Act
        var result = await _terminalService.GetTerminalsByStoreAsync(TestStoreId);

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetActiveTerminalsAsync_ShouldReturnOnlyActiveTerminals()
    {
        // Arrange
        await CreateTestTerminalAsync(code: "REG-001", isActive: true);
        await CreateTestTerminalAsync(code: "REG-002", isActive: true);
        await CreateTestTerminalAsync(code: "REG-003", isActive: false);

        // Act
        var result = await _terminalService.GetActiveTerminalsAsync(TestStoreId);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUnassignedTerminalsAsync_ShouldReturnUnassignedTerminals()
    {
        // Arrange
        await CreateTestTerminalAsync(code: "REG-001", machineIdentifier: null);
        await CreateTestTerminalAsync(code: "REG-002", machineIdentifier: "AA:BB:CC:DD:EE:FF");
        await CreateTestTerminalAsync(code: "REG-003", machineIdentifier: "");

        // Act
        var result = await _terminalService.GetUnassignedTerminalsAsync(TestStoreId);

        // Assert
        result.Should().HaveCount(2); // REG-001 and REG-003
    }

    #endregion

    #region Update Terminal Tests

    [Fact]
    public async Task UpdateTerminalAsync_ShouldUpdateTerminal_WithValidData()
    {
        // Arrange
        var terminal = await CreateTestTerminalAsync(code: "REG-001", name: "Original Name");

        var request = new UpdateTerminalRequest
        {
            Code = "REG-001-UPDATED",
            Name = "Updated Name",
            Description = "Updated description",
            TerminalType = TerminalType.Till,
            BusinessMode = BusinessMode.Restaurant,
            IsMainRegister = true
        };

        // Act
        var result = await _terminalService.UpdateTerminalAsync(terminal.Id, request, TestUserId);

        // Assert
        result.Code.Should().Be("REG-001-UPDATED");
        result.Name.Should().Be("Updated Name");
        result.Description.Should().Be("Updated description");
        result.TerminalType.Should().Be(TerminalType.Till);
        result.IsMainRegister.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateTerminalAsync_ShouldThrow_WhenTerminalNotFound()
    {
        // Arrange
        var request = new UpdateTerminalRequest
        {
            Code = "REG-001",
            Name = "Updated Name",
            TerminalType = TerminalType.Register,
            BusinessMode = BusinessMode.Supermarket
        };

        // Act
        var action = () => _terminalService.UpdateTerminalAsync(99999, request, TestUserId);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task UpdateTerminalAsync_ShouldThrow_WhenCodeIsDuplicate()
    {
        // Arrange
        await CreateTestTerminalAsync(code: "REG-001");
        var terminal = await CreateTestTerminalAsync(code: "REG-002");

        var request = new UpdateTerminalRequest
        {
            Code = "REG-001", // Duplicate
            Name = "Updated Name",
            TerminalType = TerminalType.Register,
            BusinessMode = BusinessMode.Supermarket
        };

        // Act
        var action = () => _terminalService.UpdateTerminalAsync(terminal.Id, request, TestUserId);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already in use*");
    }

    [Fact]
    public async Task UpdateTerminalConfigurationAsync_ShouldUpdateConfiguration()
    {
        // Arrange
        var terminal = await CreateTestTerminalAsync();
        var printerConfig = """{"EnableReceiptPrinting":true,"ReceiptPrinterId":1}""";
        var hardwareConfig = """{"EnableCashDrawer":true,"CashDrawerPort":"COM1"}""";

        // Act
        var result = await _terminalService.UpdateTerminalConfigurationAsync(
            terminal.Id, printerConfig, hardwareConfig, TestUserId);

        // Assert
        result.Should().BeTrue();

        var updated = await _context.Terminals.FindAsync(terminal.Id);
        updated!.PrinterConfiguration.Should().Be(printerConfig);
        updated.HardwareConfiguration.Should().Be(hardwareConfig);
    }

    #endregion

    #region Deactivate/Reactivate Tests

    [Fact]
    public async Task DeactivateTerminalAsync_ShouldDeactivateTerminal()
    {
        // Arrange
        var terminal = await CreateTestTerminalAsync(isActive: true);

        // Act
        var result = await _terminalService.DeactivateTerminalAsync(terminal.Id, TestUserId);

        // Assert
        result.Should().BeTrue();

        var deactivated = await _context.Terminals.FindAsync(terminal.Id);
        deactivated!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateTerminalAsync_ShouldReturnFalse_WhenTerminalNotFound()
    {
        // Act
        var result = await _terminalService.DeactivateTerminalAsync(99999, TestUserId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ReactivateTerminalAsync_ShouldReactivateTerminal()
    {
        // Arrange
        var terminal = await CreateTestTerminalAsync(isActive: false);

        // Act
        var result = await _terminalService.ReactivateTerminalAsync(terminal.Id, TestUserId);

        // Assert
        result.Should().BeTrue();

        var reactivated = await _context.Terminals.FindAsync(terminal.Id);
        reactivated!.IsActive.Should().BeTrue();
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task IsTerminalCodeUniqueAsync_ShouldReturnTrue_WhenCodeIsUnique()
    {
        // Arrange
        await CreateTestTerminalAsync(code: "REG-001");

        // Act
        var result = await _terminalService.IsTerminalCodeUniqueAsync(TestStoreId, "REG-002");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsTerminalCodeUniqueAsync_ShouldReturnFalse_WhenCodeExists()
    {
        // Arrange
        await CreateTestTerminalAsync(code: "REG-001");

        // Act
        var result = await _terminalService.IsTerminalCodeUniqueAsync(TestStoreId, "REG-001");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsTerminalCodeUniqueAsync_ShouldExcludeSpecifiedTerminal()
    {
        // Arrange
        var terminal = await CreateTestTerminalAsync(code: "REG-001");

        // Act
        var result = await _terminalService.IsTerminalCodeUniqueAsync(TestStoreId, "REG-001", terminal.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsMachineIdentifierAvailableAsync_ShouldReturnTrue_WhenAvailable()
    {
        // Arrange
        await CreateTestTerminalAsync(machineIdentifier: "AA:BB:CC:DD:EE:FF");

        // Act
        var result = await _terminalService.IsMachineIdentifierAvailableAsync("11:22:33:44:55:66");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsMachineIdentifierAvailableAsync_ShouldReturnFalse_WhenInUse()
    {
        // Arrange
        await CreateTestTerminalAsync(machineIdentifier: "AA:BB:CC:DD:EE:FF");

        // Act
        var result = await _terminalService.IsMachineIdentifierAvailableAsync("AA:BB:CC:DD:EE:FF");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateTerminalAsync_ShouldReturnValid_WhenTerminalIsValid()
    {
        // Arrange
        var terminal = await CreateTestTerminalAsync(
            code: "REG-001",
            machineIdentifier: "AA:BB:CC:DD:EE:FF",
            isActive: true);

        // Act
        var result = await _terminalService.ValidateTerminalAsync(terminal.Id, "AA:BB:CC:DD:EE:FF");

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.TerminalId.Should().Be(terminal.Id);
    }

    [Fact]
    public async Task ValidateTerminalAsync_ShouldReturnInvalid_WhenTerminalNotFound()
    {
        // Act
        var result = await _terminalService.ValidateTerminalAsync(99999);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task ValidateTerminalAsync_ShouldReturnInvalid_WhenTerminalIsInactive()
    {
        // Arrange
        var terminal = await CreateTestTerminalAsync(isActive: false);

        // Act
        var result = await _terminalService.ValidateTerminalAsync(terminal.Id);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("inactive");
    }

    [Fact]
    public async Task ValidateTerminalAsync_ShouldReturnInvalid_WhenMachineIdMismatch()
    {
        // Arrange
        var terminal = await CreateTestTerminalAsync(machineIdentifier: "AA:BB:CC:DD:EE:FF");

        // Act
        var result = await _terminalService.ValidateTerminalAsync(terminal.Id, "11:22:33:44:55:66");

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("mismatch");
    }

    #endregion

    #region Registration Tests

    [Fact]
    public async Task RegisterTerminalAsync_ShouldRegisterTerminal_WithValidData()
    {
        // Arrange
        var request = new TerminalRegistrationRequest
        {
            StoreId = TestStoreId,
            Code = "REG-001",
            Name = "Register 1",
            MachineIdentifier = "AA:BB:CC:DD:EE:FF",
            TerminalType = TerminalType.Register,
            BusinessMode = BusinessMode.Supermarket,
            IpAddress = "192.168.1.100"
        };

        // Act
        var result = await _terminalService.RegisterTerminalAsync(request, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result.Code.Should().Be("REG-001");
        result.MachineIdentifier.Should().Be("AA:BB:CC:DD:EE:FF");
        result.LastHeartbeat.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterTerminalAsync_ShouldThrow_WhenMachineAlreadyRegistered()
    {
        // Arrange
        await CreateTestTerminalAsync(machineIdentifier: "AA:BB:CC:DD:EE:FF");

        var request = new TerminalRegistrationRequest
        {
            StoreId = TestStoreId,
            Code = "REG-002",
            Name = "Register 2",
            MachineIdentifier = "AA:BB:CC:DD:EE:FF", // Already in use
            TerminalType = TerminalType.Register,
            BusinessMode = BusinessMode.Supermarket
        };

        // Act
        var action = () => _terminalService.RegisterTerminalAsync(request, TestUserId);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already registered*");
    }

    [Fact]
    public async Task BindMachineAsync_ShouldBindMachine_WhenAvailable()
    {
        // Arrange
        var terminal = await CreateTestTerminalAsync(machineIdentifier: null);

        // Act
        var result = await _terminalService.BindMachineAsync(terminal.Id, "AA:BB:CC:DD:EE:FF", TestUserId);

        // Assert
        result.Should().BeTrue();

        var bound = await _context.Terminals.FindAsync(terminal.Id);
        bound!.MachineIdentifier.Should().Be("AA:BB:CC:DD:EE:FF");
    }

    [Fact]
    public async Task BindMachineAsync_ShouldThrow_WhenMachineInUse()
    {
        // Arrange
        await CreateTestTerminalAsync(code: "REG-001", machineIdentifier: "AA:BB:CC:DD:EE:FF");
        var terminal = await CreateTestTerminalAsync(code: "REG-002", machineIdentifier: null);

        // Act
        var action = () => _terminalService.BindMachineAsync(terminal.Id, "AA:BB:CC:DD:EE:FF", TestUserId);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already in use*");
    }

    [Fact]
    public async Task UnbindMachineAsync_ShouldUnbindMachine()
    {
        // Arrange
        var terminal = await CreateTestTerminalAsync(machineIdentifier: "AA:BB:CC:DD:EE:FF");

        // Act
        var result = await _terminalService.UnbindMachineAsync(terminal.Id, TestUserId);

        // Assert
        result.Should().BeTrue();

        var unbound = await _context.Terminals.FindAsync(terminal.Id);
        unbound!.MachineIdentifier.Should().BeEmpty();
    }

    #endregion

    #region Status Tests

    [Fact]
    public async Task UpdateHeartbeatAsync_ShouldUpdateHeartbeat()
    {
        // Arrange
        var terminal = await CreateTestTerminalAsync();
        var heartbeat = new TerminalHeartbeat
        {
            IpAddress = "192.168.1.100",
            CurrentUserId = TestUserId
        };

        // Act
        var result = await _terminalService.UpdateHeartbeatAsync(terminal.Id, heartbeat);

        // Assert
        result.Should().BeTrue();

        var updated = await _context.Terminals.FindAsync(terminal.Id);
        updated!.LastHeartbeat.Should().NotBeNull();
        updated.IpAddress.Should().Be("192.168.1.100");
        updated.LastLoginUserId.Should().Be(TestUserId);
    }

    [Fact]
    public async Task GetTerminalStatusAsync_ShouldReturnStatus_WhenExists()
    {
        // Arrange
        var terminal = await CreateTestTerminalAsync(
            code: "REG-001",
            lastHeartbeat: DateTime.UtcNow);

        // Act
        var result = await _terminalService.GetTerminalStatusAsync(terminal.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be("REG-001");
        result.IsOnline.Should().BeTrue();
    }

    [Fact]
    public async Task GetTerminalStatusAsync_ShouldShowOffline_WhenHeartbeatExpired()
    {
        // Arrange
        var terminal = await CreateTestTerminalAsync(
            code: "REG-001",
            lastHeartbeat: DateTime.UtcNow.AddMinutes(-5));

        // Act
        var result = await _terminalService.GetTerminalStatusAsync(terminal.Id);

        // Assert
        result.Should().NotBeNull();
        result!.IsOnline.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllTerminalStatusesAsync_ShouldReturnAllStatuses()
    {
        // Arrange
        await CreateTestTerminalAsync(code: "REG-001", lastHeartbeat: DateTime.UtcNow);
        await CreateTestTerminalAsync(code: "REG-002", lastHeartbeat: DateTime.UtcNow.AddMinutes(-5));

        // Act
        var result = await _terminalService.GetAllTerminalStatusesAsync(TestStoreId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(s => s.IsOnline);
        result.Should().Contain(s => !s.IsOnline);
    }

    #endregion

    #region Code Generation Tests

    [Fact]
    public async Task GenerateTerminalCodeAsync_ShouldGenerateCorrectCode_ForRegister()
    {
        // Act
        var result = await _terminalService.GenerateTerminalCodeAsync(TestStoreId, TerminalType.Register);

        // Assert
        result.Should().Be("REG-001");
    }

    [Fact]
    public async Task GenerateTerminalCodeAsync_ShouldGenerateCorrectCode_ForTill()
    {
        // Act
        var result = await _terminalService.GenerateTerminalCodeAsync(TestStoreId, TerminalType.Till);

        // Assert
        result.Should().Be("TILL-001");
    }

    [Fact]
    public async Task GenerateTerminalCodeAsync_ShouldGenerateCorrectCode_ForKitchenDisplay()
    {
        // Act
        var result = await _terminalService.GenerateTerminalCodeAsync(TestStoreId, TerminalType.KitchenDisplay);

        // Assert
        result.Should().Be("KDS-001");
    }

    [Fact]
    public async Task GenerateTerminalCodeAsync_ShouldIncrementNumber_WhenCodesExist()
    {
        // Arrange
        await CreateTestTerminalAsync(code: "REG-001");
        await CreateTestTerminalAsync(code: "REG-002");

        // Act
        var result = await _terminalService.GenerateTerminalCodeAsync(TestStoreId, TerminalType.Register);

        // Assert
        result.Should().Be("REG-003");
    }

    [Fact]
    public async Task GetNextTerminalNumberAsync_ShouldReturnOne_WhenNoTerminals()
    {
        // Act
        var result = await _terminalService.GetNextTerminalNumberAsync(TestStoreId, TerminalType.Register);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task GetNextTerminalNumberAsync_ShouldFindGaps_InSequence()
    {
        // Arrange
        await CreateTestTerminalAsync(code: "REG-001");
        await CreateTestTerminalAsync(code: "REG-003"); // Skip 002

        // Act
        var result = await _terminalService.GetNextTerminalNumberAsync(TestStoreId, TerminalType.Register);

        // Assert
        result.Should().Be(4); // Returns next after max, not fills gap
    }

    #endregion
}
