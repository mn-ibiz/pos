using Microsoft.EntityFrameworkCore;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service implementation for table transfer operations.
/// </summary>
public class TableTransferService : ITableTransferService
{
    private readonly POSDbContext _context;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TableTransferService"/> class.
    /// </summary>
    public TableTransferService(POSDbContext context, ILogger logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TransferResult> TransferTableAsync(
        TransferTableRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var table = await _context.Tables
                .Include(t => t.CurrentReceipt)
                .Include(t => t.AssignedUser)
                .FirstOrDefaultAsync(t => t.Id == request.TableId && t.IsActive, cancellationToken);

            if (table == null)
            {
                _logger.Warning("Table transfer failed: Table {TableId} not found", request.TableId);
                return TransferResult.Failed("Table not found");
            }

            if (table.Status != TableStatus.Occupied)
            {
                _logger.Warning("Table transfer failed: Table {TableNumber} is not occupied (Status: {Status})",
                    table.TableNumber, table.Status);
                return TransferResult.Failed("Only occupied tables can be transferred");
            }

            var originalUserId = table.AssignedUserId;
            var originalUserName = table.AssignedUser?.FullName ?? "Unknown";

            if (originalUserId == null || originalUserId == 0)
            {
                _logger.Warning("Table transfer failed: Table {TableNumber} has no assigned waiter",
                    table.TableNumber);
                return TransferResult.Failed("Table has no assigned waiter");
            }

            var newUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.NewWaiterId && u.IsActive, cancellationToken);

            if (newUser == null)
            {
                _logger.Warning("Table transfer failed: New waiter {WaiterId} not found", request.NewWaiterId);
                return TransferResult.Failed("New waiter not found");
            }

            if (originalUserId == request.NewWaiterId)
            {
                return TransferResult.Failed("Cannot transfer table to the same waiter");
            }

            // Transfer table ownership
            table.AssignedUserId = request.NewWaiterId;
            table.UpdatedAt = DateTime.UtcNow;
            table.UpdatedByUserId = request.TransferredByUserId;

            // Transfer receipt ownership if exists
            if (table.CurrentReceipt != null)
            {
                table.CurrentReceipt.OwnerId = request.NewWaiterId;
                table.CurrentReceipt.UpdatedAt = DateTime.UtcNow;
                table.CurrentReceipt.UpdatedByUserId = request.TransferredByUserId;
            }

            // Create transfer log entry
            var transferLog = new TableTransferLog
            {
                TableId = table.Id,
                TableNumber = table.TableNumber,
                FromUserId = originalUserId.Value,
                FromUserName = originalUserName,
                ToUserId = request.NewWaiterId,
                ToUserName = newUser.FullName,
                ReceiptId = table.CurrentReceiptId,
                ReceiptAmount = table.CurrentReceipt?.TotalAmount ?? 0,
                Reason = request.Reason,
                TransferredAt = DateTime.UtcNow,
                TransferredByUserId = request.TransferredByUserId
            };

            _context.TableTransferLogs.Add(transferLog);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.Information(
                "Table {TableNumber} transferred from {FromUser} to {ToUser} by user {TransferredBy}. Reason: {Reason}",
                table.TableNumber,
                originalUserName,
                newUser.FullName,
                request.TransferredByUserId,
                request.Reason ?? "Not specified");

            return TransferResult.Success(transferLog);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error transferring table {TableId}", request.TableId);
            return TransferResult.Failed($"An error occurred during transfer: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<TransferResult> BulkTransferAsync(
        BulkTransferRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.TableIds.Any())
        {
            return TransferResult.Failed("No tables selected for transfer");
        }

        var results = new List<TableTransferLog>();
        var errors = new List<string>();

        foreach (var tableId in request.TableIds)
        {
            var result = await TransferTableAsync(new TransferTableRequest
            {
                TableId = tableId,
                NewWaiterId = request.NewWaiterId,
                Reason = request.Reason ?? "Bulk transfer",
                TransferredByUserId = request.TransferredByUserId
            }, cancellationToken);

            if (result.IsSuccess && result.TransferLog != null)
            {
                results.Add(result.TransferLog);
            }
            else
            {
                var table = await _context.Tables
                    .Where(t => t.Id == tableId)
                    .Select(t => t.TableNumber)
                    .FirstOrDefaultAsync(cancellationToken);

                errors.Add($"Table {table ?? tableId.ToString()}: {result.ErrorMessage}");
            }
        }

        if (!results.Any())
        {
            return TransferResult.Failed("No tables were transferred: " + string.Join("; ", errors));
        }

        if (errors.Any())
        {
            _logger.Warning(
                "Bulk transfer partially completed. Transferred: {TransferredCount}, Errors: {ErrorCount}",
                results.Count,
                errors.Count);

            return TransferResult.PartialSuccess(results, errors);
        }

        _logger.Information(
            "Bulk transfer completed. {Count} tables transferred to waiter {WaiterId}",
            results.Count,
            request.NewWaiterId);

        return TransferResult.Success(results);
    }

    /// <inheritdoc />
    public async Task<List<TableTransferLog>> GetTransferHistoryAsync(
        int tableId,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        return await _context.TableTransferLogs
            .Where(t => t.TableId == tableId)
            .OrderByDescending(t => t.TransferredAt)
            .Take(limit)
            .Include(t => t.FromUser)
            .Include(t => t.ToUser)
            .Include(t => t.TransferredByUser)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Table>> GetTablesByWaiterAsync(
        int waiterId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Tables
            .Where(t => t.IsActive
                && t.AssignedUserId == waiterId
                && t.Status == TableStatus.Occupied)
            .Include(t => t.CurrentReceipt)
            .Include(t => t.Floor)
            .OrderBy(t => t.Floor.DisplayOrder)
            .ThenBy(t => t.TableNumber)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<User>> GetActiveWaitersAsync(CancellationToken cancellationToken = default)
    {
        // Get users who have roles that allow them to be assigned tables
        // For now, get all active users - in production this could be filtered by role
        return await _context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.FullName)
            .ToListAsync(cancellationToken);
    }
}
