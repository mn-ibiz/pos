using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Models;
using HospitalityPOS.Infrastructure.Data;
using HospitalityPOS.Infrastructure.Services;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for the TableTransferService class.
/// Tests cover table transfer operations, bulk transfers, and transfer history.
/// </summary>
public class TableTransferServiceTests : IDisposable
{
    private readonly POSDbContext _context;
    private readonly Mock<ILogger> _loggerMock;
    private readonly TableTransferService _tableTransferService;
    private const int TestUserId = 1;
    private const int TestManagerId = 99;

    public TableTransferServiceTests()
    {
        var options = new DbContextOptionsBuilder<POSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new POSDbContext(options);
        _loggerMock = new Mock<ILogger>();

        _tableTransferService = new TableTransferService(_context, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Helper Methods

    private async Task<User> CreateTestUserAsync(
        string username = "testuser",
        string fullName = "Test User",
        bool isActive = true)
    {
        var user = new User
        {
            Username = username,
            FullName = fullName,
            PasswordHash = "hash",
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = TestUserId
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    private async Task<Floor> CreateTestFloorAsync(string name = "Main Floor")
    {
        var floor = new Floor
        {
            Name = name,
            DisplayOrder = 1,
            GridWidth = 10,
            GridHeight = 10,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = TestUserId
        };

        _context.Floors.Add(floor);
        await _context.SaveChangesAsync();

        return floor;
    }

    private async Task<Table> CreateTestTableAsync(
        int floorId,
        string tableNumber = "T1",
        TableStatus status = TableStatus.Available,
        int? assignedUserId = null,
        int? currentReceiptId = null,
        bool isActive = true)
    {
        var table = new Table
        {
            TableNumber = tableNumber,
            Capacity = 4,
            FloorId = floorId,
            Status = status,
            AssignedUserId = assignedUserId,
            CurrentReceiptId = currentReceiptId,
            GridX = 0,
            GridY = 0,
            Width = 1,
            Height = 1,
            Shape = TableShape.Square,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = TestUserId
        };

        _context.Tables.Add(table);
        await _context.SaveChangesAsync();

        return table;
    }

    private async Task<Receipt> CreateTestReceiptAsync(
        int tableId,
        int userId,
        decimal totalAmount = 100.00m)
    {
        var receipt = new Receipt
        {
            ReceiptNumber = $"RCP-{DateTime.UtcNow:yyyyMMddHHmmss}",
            TableId = tableId,
            UserId = userId,
            Status = ReceiptStatus.Open,
            TotalAmount = totalAmount,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        _context.Receipts.Add(receipt);
        await _context.SaveChangesAsync();

        return receipt;
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_ShouldNotThrow()
    {
        // Act
        var service = new TableTransferService(_context, _loggerMock.Object);

        // Assert
        service.Should().NotBeNull();
    }

    #endregion

    #region TransferTableAsync Tests

    [Fact]
    public async Task TransferTableAsync_WithValidRequest_ShouldTransferSuccessfully()
    {
        // Arrange
        var floor = await CreateTestFloorAsync();
        var fromUser = await CreateTestUserAsync("waiter1", "John Doe");
        var toUser = await CreateTestUserAsync("waiter2", "Jane Smith");
        var table = await CreateTestTableAsync(floor.Id, "T1", TableStatus.Occupied, fromUser.Id);
        var receipt = await CreateTestReceiptAsync(table.Id, fromUser.Id, 150.00m);

        // Update table with receipt
        table.CurrentReceiptId = receipt.Id;
        await _context.SaveChangesAsync();

        var request = new TransferTableRequest
        {
            TableId = table.Id,
            NewWaiterId = toUser.Id,
            Reason = "Shift change",
            TransferredByUserId = TestManagerId
        };

        // Act
        var result = await _tableTransferService.TransferTableAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.TransferLog.Should().NotBeNull();
        result.TransferLog!.FromUserId.Should().Be(fromUser.Id);
        result.TransferLog.ToUserId.Should().Be(toUser.Id);
        result.TransferLog.Reason.Should().Be("Shift change");

        // Verify table ownership changed
        var updatedTable = await _context.Tables.FindAsync(table.Id);
        updatedTable!.AssignedUserId.Should().Be(toUser.Id);

        // Verify receipt ownership changed
        var updatedReceipt = await _context.Receipts.FindAsync(receipt.Id);
        updatedReceipt!.UserId.Should().Be(toUser.Id);
    }

    [Fact]
    public async Task TransferTableAsync_WithNonExistentTable_ShouldReturnError()
    {
        // Arrange
        var toUser = await CreateTestUserAsync("waiter2", "Jane Smith");

        var request = new TransferTableRequest
        {
            TableId = 99999,
            NewWaiterId = toUser.Id,
            Reason = "Test",
            TransferredByUserId = TestManagerId
        };

        // Act
        var result = await _tableTransferService.TransferTableAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task TransferTableAsync_WithInactiveTable_ShouldReturnError()
    {
        // Arrange
        var floor = await CreateTestFloorAsync();
        var fromUser = await CreateTestUserAsync("waiter1", "John Doe");
        var toUser = await CreateTestUserAsync("waiter2", "Jane Smith");
        var table = await CreateTestTableAsync(floor.Id, "T1", TableStatus.Occupied, fromUser.Id, isActive: false);

        var request = new TransferTableRequest
        {
            TableId = table.Id,
            NewWaiterId = toUser.Id,
            Reason = "Test",
            TransferredByUserId = TestManagerId
        };

        // Act
        var result = await _tableTransferService.TransferTableAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task TransferTableAsync_WithAvailableTable_ShouldReturnError()
    {
        // Arrange
        var floor = await CreateTestFloorAsync();
        var fromUser = await CreateTestUserAsync("waiter1", "John Doe");
        var toUser = await CreateTestUserAsync("waiter2", "Jane Smith");
        var table = await CreateTestTableAsync(floor.Id, "T1", TableStatus.Available, fromUser.Id);

        var request = new TransferTableRequest
        {
            TableId = table.Id,
            NewWaiterId = toUser.Id,
            Reason = "Test",
            TransferredByUserId = TestManagerId
        };

        // Act
        var result = await _tableTransferService.TransferTableAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("occupied");
    }

    [Fact]
    public async Task TransferTableAsync_WithNoAssignedWaiter_ShouldReturnError()
    {
        // Arrange
        var floor = await CreateTestFloorAsync();
        var toUser = await CreateTestUserAsync("waiter2", "Jane Smith");
        var table = await CreateTestTableAsync(floor.Id, "T1", TableStatus.Occupied, assignedUserId: null);

        var request = new TransferTableRequest
        {
            TableId = table.Id,
            NewWaiterId = toUser.Id,
            Reason = "Test",
            TransferredByUserId = TestManagerId
        };

        // Act
        var result = await _tableTransferService.TransferTableAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("no assigned waiter");
    }

    [Fact]
    public async Task TransferTableAsync_WithNonExistentNewWaiter_ShouldReturnError()
    {
        // Arrange
        var floor = await CreateTestFloorAsync();
        var fromUser = await CreateTestUserAsync("waiter1", "John Doe");
        var table = await CreateTestTableAsync(floor.Id, "T1", TableStatus.Occupied, fromUser.Id);

        var request = new TransferTableRequest
        {
            TableId = table.Id,
            NewWaiterId = 99999,
            Reason = "Test",
            TransferredByUserId = TestManagerId
        };

        // Act
        var result = await _tableTransferService.TransferTableAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("New waiter not found");
    }

    [Fact]
    public async Task TransferTableAsync_WithInactiveNewWaiter_ShouldReturnError()
    {
        // Arrange
        var floor = await CreateTestFloorAsync();
        var fromUser = await CreateTestUserAsync("waiter1", "John Doe");
        var toUser = await CreateTestUserAsync("waiter2", "Jane Smith", isActive: false);
        var table = await CreateTestTableAsync(floor.Id, "T1", TableStatus.Occupied, fromUser.Id);

        var request = new TransferTableRequest
        {
            TableId = table.Id,
            NewWaiterId = toUser.Id,
            Reason = "Test",
            TransferredByUserId = TestManagerId
        };

        // Act
        var result = await _tableTransferService.TransferTableAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task TransferTableAsync_ToSameWaiter_ShouldReturnError()
    {
        // Arrange
        var floor = await CreateTestFloorAsync();
        var waiter = await CreateTestUserAsync("waiter1", "John Doe");
        var table = await CreateTestTableAsync(floor.Id, "T1", TableStatus.Occupied, waiter.Id);

        var request = new TransferTableRequest
        {
            TableId = table.Id,
            NewWaiterId = waiter.Id,
            Reason = "Test",
            TransferredByUserId = TestManagerId
        };

        // Act
        var result = await _tableTransferService.TransferTableAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("same waiter");
    }

    [Fact]
    public async Task TransferTableAsync_ShouldCreateTransferLog()
    {
        // Arrange
        var floor = await CreateTestFloorAsync();
        var fromUser = await CreateTestUserAsync("waiter1", "John Doe");
        var toUser = await CreateTestUserAsync("waiter2", "Jane Smith");
        var table = await CreateTestTableAsync(floor.Id, "T1", TableStatus.Occupied, fromUser.Id);

        var request = new TransferTableRequest
        {
            TableId = table.Id,
            NewWaiterId = toUser.Id,
            Reason = "Shift change",
            TransferredByUserId = TestManagerId
        };

        // Act
        await _tableTransferService.TransferTableAsync(request);

        // Assert
        var logs = await _context.TableTransferLogs.Where(l => l.TableId == table.Id).ToListAsync();
        logs.Should().HaveCount(1);
        logs[0].FromUserId.Should().Be(fromUser.Id);
        logs[0].ToUserId.Should().Be(toUser.Id);
        logs[0].TransferredByUserId.Should().Be(TestManagerId);
        logs[0].Reason.Should().Be("Shift change");
    }

    [Fact]
    public async Task TransferTableAsync_WithoutReceipt_ShouldTransferSuccessfully()
    {
        // Arrange
        var floor = await CreateTestFloorAsync();
        var fromUser = await CreateTestUserAsync("waiter1", "John Doe");
        var toUser = await CreateTestUserAsync("waiter2", "Jane Smith");
        var table = await CreateTestTableAsync(floor.Id, "T1", TableStatus.Occupied, fromUser.Id);

        var request = new TransferTableRequest
        {
            TableId = table.Id,
            NewWaiterId = toUser.Id,
            Reason = "No receipt test",
            TransferredByUserId = TestManagerId
        };

        // Act
        var result = await _tableTransferService.TransferTableAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.TransferLog!.ReceiptId.Should().BeNull();
        result.TransferLog.ReceiptAmount.Should().Be(0);
    }

    #endregion

    #region BulkTransferAsync Tests

    [Fact]
    public async Task BulkTransferAsync_WithEmptyTableList_ShouldReturnError()
    {
        // Arrange
        var toUser = await CreateTestUserAsync("waiter2", "Jane Smith");

        var request = new BulkTransferRequest
        {
            TableIds = [],
            NewWaiterId = toUser.Id,
            Reason = "Bulk test",
            TransferredByUserId = TestManagerId
        };

        // Act
        var result = await _tableTransferService.BulkTransferAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No tables selected");
    }

    [Fact]
    public async Task BulkTransferAsync_WithValidTables_ShouldTransferAll()
    {
        // Arrange
        var floor = await CreateTestFloorAsync();
        var fromUser = await CreateTestUserAsync("waiter1", "John Doe");
        var toUser = await CreateTestUserAsync("waiter2", "Jane Smith");

        var table1 = await CreateTestTableAsync(floor.Id, "T1", TableStatus.Occupied, fromUser.Id);
        var table2 = await CreateTestTableAsync(floor.Id, "T2", TableStatus.Occupied, fromUser.Id);
        var table3 = await CreateTestTableAsync(floor.Id, "T3", TableStatus.Occupied, fromUser.Id);

        var request = new BulkTransferRequest
        {
            TableIds = [table1.Id, table2.Id, table3.Id],
            NewWaiterId = toUser.Id,
            Reason = "End of shift",
            TransferredByUserId = TestManagerId
        };

        // Act
        var result = await _tableTransferService.BulkTransferAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.TransferLogs.Should().HaveCount(3);
        result.Errors.Should().BeEmpty();

        // Verify all tables transferred
        var tables = await _context.Tables
            .Where(t => new[] { table1.Id, table2.Id, table3.Id }.Contains(t.Id))
            .ToListAsync();

        tables.Should().AllSatisfy(t => t.AssignedUserId.Should().Be(toUser.Id));
    }

    [Fact]
    public async Task BulkTransferAsync_WithSomeInvalidTables_ShouldReturnPartialSuccess()
    {
        // Arrange
        var floor = await CreateTestFloorAsync();
        var fromUser = await CreateTestUserAsync("waiter1", "John Doe");
        var toUser = await CreateTestUserAsync("waiter2", "Jane Smith");

        var validTable = await CreateTestTableAsync(floor.Id, "T1", TableStatus.Occupied, fromUser.Id);
        var availableTable = await CreateTestTableAsync(floor.Id, "T2", TableStatus.Available, fromUser.Id);
        var inactiveTable = await CreateTestTableAsync(floor.Id, "T3", TableStatus.Occupied, fromUser.Id, isActive: false);

        var request = new BulkTransferRequest
        {
            TableIds = [validTable.Id, availableTable.Id, inactiveTable.Id],
            NewWaiterId = toUser.Id,
            Reason = "Mixed test",
            TransferredByUserId = TestManagerId
        };

        // Act
        var result = await _tableTransferService.BulkTransferAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsPartialSuccess.Should().BeTrue();
        result.TransferLogs.Should().HaveCount(1);
        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public async Task BulkTransferAsync_WithAllInvalidTables_ShouldReturnError()
    {
        // Arrange
        var floor = await CreateTestFloorAsync();
        var fromUser = await CreateTestUserAsync("waiter1", "John Doe");
        var toUser = await CreateTestUserAsync("waiter2", "Jane Smith");

        var availableTable1 = await CreateTestTableAsync(floor.Id, "T1", TableStatus.Available, fromUser.Id);
        var availableTable2 = await CreateTestTableAsync(floor.Id, "T2", TableStatus.Available, fromUser.Id);

        var request = new BulkTransferRequest
        {
            TableIds = [availableTable1.Id, availableTable2.Id],
            NewWaiterId = toUser.Id,
            Reason = "All invalid test",
            TransferredByUserId = TestManagerId
        };

        // Act
        var result = await _tableTransferService.BulkTransferAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsPartialSuccess.Should().BeFalse();
        result.TransferLogs.Should().BeEmpty();
        result.ErrorMessage.Should().Contain("No tables were transferred");
    }

    [Fact]
    public async Task BulkTransferAsync_WithDefaultReason_ShouldUseBulkTransferReason()
    {
        // Arrange
        var floor = await CreateTestFloorAsync();
        var fromUser = await CreateTestUserAsync("waiter1", "John Doe");
        var toUser = await CreateTestUserAsync("waiter2", "Jane Smith");
        var table = await CreateTestTableAsync(floor.Id, "T1", TableStatus.Occupied, fromUser.Id);

        var request = new BulkTransferRequest
        {
            TableIds = [table.Id],
            NewWaiterId = toUser.Id,
            Reason = null, // No reason provided
            TransferredByUserId = TestManagerId
        };

        // Act
        var result = await _tableTransferService.BulkTransferAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.TransferLogs[0].Reason.Should().Be("Bulk transfer");
    }

    #endregion

    #region GetTransferHistoryAsync Tests

    [Fact]
    public async Task GetTransferHistoryAsync_WithNoHistory_ShouldReturnEmptyList()
    {
        // Arrange
        var floor = await CreateTestFloorAsync();
        var table = await CreateTestTableAsync(floor.Id, "T1");

        // Act
        var history = await _tableTransferService.GetTransferHistoryAsync(table.Id);

        // Assert
        history.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTransferHistoryAsync_WithHistory_ShouldReturnOrderedByDate()
    {
        // Arrange
        var floor = await CreateTestFloorAsync();
        var user1 = await CreateTestUserAsync("waiter1", "John Doe");
        var user2 = await CreateTestUserAsync("waiter2", "Jane Smith");
        var user3 = await CreateTestUserAsync("waiter3", "Bob Wilson");
        var table = await CreateTestTableAsync(floor.Id, "T1", TableStatus.Occupied, user1.Id);

        // Create transfer 1
        await _tableTransferService.TransferTableAsync(new TransferTableRequest
        {
            TableId = table.Id,
            NewWaiterId = user2.Id,
            Reason = "First transfer",
            TransferredByUserId = TestManagerId
        });

        // Update table status for next transfer
        table.AssignedUserId = user2.Id;
        await _context.SaveChangesAsync();

        await Task.Delay(10); // Small delay to ensure different timestamps

        // Create transfer 2
        await _tableTransferService.TransferTableAsync(new TransferTableRequest
        {
            TableId = table.Id,
            NewWaiterId = user3.Id,
            Reason = "Second transfer",
            TransferredByUserId = TestManagerId
        });

        // Act
        var history = await _tableTransferService.GetTransferHistoryAsync(table.Id);

        // Assert
        history.Should().HaveCount(2);
        history[0].Reason.Should().Be("Second transfer"); // Most recent first
        history[1].Reason.Should().Be("First transfer");
    }

    [Fact]
    public async Task GetTransferHistoryAsync_WithLimit_ShouldRespectLimit()
    {
        // Arrange
        var floor = await CreateTestFloorAsync();
        var table = await CreateTestTableAsync(floor.Id, "T1", TableStatus.Occupied);

        // Create 5 transfer logs manually
        for (int i = 0; i < 5; i++)
        {
            var user = await CreateTestUserAsync($"waiter{i}", $"Waiter {i}");
            _context.TableTransferLogs.Add(new TableTransferLog
            {
                TableId = table.Id,
                TableNumber = table.TableNumber,
                FromUserId = TestUserId,
                FromUserName = "From User",
                ToUserId = user.Id,
                ToUserName = user.FullName,
                Reason = $"Transfer {i}",
                TransferredAt = DateTime.UtcNow.AddMinutes(-i),
                TransferredByUserId = TestManagerId
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var history = await _tableTransferService.GetTransferHistoryAsync(table.Id, limit: 3);

        // Assert
        history.Should().HaveCount(3);
    }

    #endregion

    #region GetTablesByWaiterAsync Tests

    [Fact]
    public async Task GetTablesByWaiterAsync_WithNoTables_ShouldReturnEmptyList()
    {
        // Arrange
        var waiter = await CreateTestUserAsync("waiter1", "John Doe");

        // Act
        var tables = await _tableTransferService.GetTablesByWaiterAsync(waiter.Id);

        // Assert
        tables.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTablesByWaiterAsync_ShouldReturnOnlyOccupiedTables()
    {
        // Arrange
        var floor = await CreateTestFloorAsync();
        var waiter = await CreateTestUserAsync("waiter1", "John Doe");

        var occupiedTable1 = await CreateTestTableAsync(floor.Id, "T1", TableStatus.Occupied, waiter.Id);
        var occupiedTable2 = await CreateTestTableAsync(floor.Id, "T2", TableStatus.Occupied, waiter.Id);
        var availableTable = await CreateTestTableAsync(floor.Id, "T3", TableStatus.Available, waiter.Id);
        var reservedTable = await CreateTestTableAsync(floor.Id, "T4", TableStatus.Reserved, waiter.Id);

        // Act
        var tables = await _tableTransferService.GetTablesByWaiterAsync(waiter.Id);

        // Assert
        tables.Should().HaveCount(2);
        tables.Should().Contain(t => t.Id == occupiedTable1.Id);
        tables.Should().Contain(t => t.Id == occupiedTable2.Id);
    }

    [Fact]
    public async Task GetTablesByWaiterAsync_ShouldNotReturnInactiveTables()
    {
        // Arrange
        var floor = await CreateTestFloorAsync();
        var waiter = await CreateTestUserAsync("waiter1", "John Doe");

        var activeTable = await CreateTestTableAsync(floor.Id, "T1", TableStatus.Occupied, waiter.Id, isActive: true);
        var inactiveTable = await CreateTestTableAsync(floor.Id, "T2", TableStatus.Occupied, waiter.Id, isActive: false);

        // Act
        var tables = await _tableTransferService.GetTablesByWaiterAsync(waiter.Id);

        // Assert
        tables.Should().HaveCount(1);
        tables[0].Id.Should().Be(activeTable.Id);
    }

    [Fact]
    public async Task GetTablesByWaiterAsync_ShouldOrderByFloorThenTableNumber()
    {
        // Arrange
        var floor1 = await CreateTestFloorAsync("Floor 1");
        floor1.DisplayOrder = 1;
        var floor2 = await CreateTestFloorAsync("Floor 2");
        floor2.DisplayOrder = 2;
        await _context.SaveChangesAsync();

        var waiter = await CreateTestUserAsync("waiter1", "John Doe");

        var table3 = await CreateTestTableAsync(floor2.Id, "T3", TableStatus.Occupied, waiter.Id);
        var table1 = await CreateTestTableAsync(floor1.Id, "T1", TableStatus.Occupied, waiter.Id);
        var table2 = await CreateTestTableAsync(floor1.Id, "T2", TableStatus.Occupied, waiter.Id);

        // Act
        var tables = await _tableTransferService.GetTablesByWaiterAsync(waiter.Id);

        // Assert
        tables.Should().HaveCount(3);
        tables[0].TableNumber.Should().Be("T1");
        tables[1].TableNumber.Should().Be("T2");
        tables[2].TableNumber.Should().Be("T3");
    }

    #endregion

    #region GetActiveWaitersAsync Tests

    [Fact]
    public async Task GetActiveWaitersAsync_WithNoUsers_ShouldReturnEmptyList()
    {
        // Act
        var waiters = await _tableTransferService.GetActiveWaitersAsync();

        // Assert
        waiters.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActiveWaitersAsync_ShouldReturnOnlyActiveUsers()
    {
        // Arrange
        var activeUser1 = await CreateTestUserAsync("waiter1", "Active One", isActive: true);
        var activeUser2 = await CreateTestUserAsync("waiter2", "Active Two", isActive: true);
        var inactiveUser = await CreateTestUserAsync("waiter3", "Inactive One", isActive: false);

        // Act
        var waiters = await _tableTransferService.GetActiveWaitersAsync();

        // Assert
        waiters.Should().HaveCount(2);
        waiters.Should().Contain(u => u.Id == activeUser1.Id);
        waiters.Should().Contain(u => u.Id == activeUser2.Id);
        waiters.Should().NotContain(u => u.Id == inactiveUser.Id);
    }

    [Fact]
    public async Task GetActiveWaitersAsync_ShouldOrderByFullName()
    {
        // Arrange
        await CreateTestUserAsync("user3", "Charlie Smith");
        await CreateTestUserAsync("user1", "Alice Johnson");
        await CreateTestUserAsync("user2", "Bob Williams");

        // Act
        var waiters = await _tableTransferService.GetActiveWaitersAsync();

        // Assert
        waiters.Should().HaveCount(3);
        waiters[0].FullName.Should().Be("Alice Johnson");
        waiters[1].FullName.Should().Be("Bob Williams");
        waiters[2].FullName.Should().Be("Charlie Smith");
    }

    #endregion

    #region Cancellation Token Tests

    [Fact]
    public async Task TransferTableAsync_WithCancelledToken_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var floor = await CreateTestFloorAsync();
        var fromUser = await CreateTestUserAsync("waiter1", "John Doe");
        var toUser = await CreateTestUserAsync("waiter2", "Jane Smith");
        var table = await CreateTestTableAsync(floor.Id, "T1", TableStatus.Occupied, fromUser.Id);

        var request = new TransferTableRequest
        {
            TableId = table.Id,
            NewWaiterId = toUser.Id,
            Reason = "Test",
            TransferredByUserId = TestManagerId
        };

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _tableTransferService.TransferTableAsync(request, cts.Token));
    }

    #endregion
}
