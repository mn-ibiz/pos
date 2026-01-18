using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for splitting receipts into multiple child receipts.
/// </summary>
public class ReceiptSplitService : IReceiptSplitService
{
    private readonly POSDbContext _context;
    private readonly IReceiptService _receiptService;
    private readonly ISessionService _sessionService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReceiptSplitService"/> class.
    /// </summary>
    public ReceiptSplitService(
        POSDbContext context,
        IReceiptService receiptService,
        ISessionService sessionService,
        ILogger logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _receiptService = receiptService ?? throw new ArgumentNullException(nameof(receiptService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<(bool CanSplit, string? Reason)> CanSplitReceiptAsync(
        int receiptId,
        CancellationToken cancellationToken = default)
    {
        var receipt = await _context.Receipts
            .Include(r => r.ReceiptItems)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == receiptId, cancellationToken);

        if (receipt is null)
        {
            return (false, "Receipt not found");
        }

        if (receipt.Status != ReceiptStatus.Pending)
        {
            return (false, $"Cannot split a receipt with status '{receipt.Status}'");
        }

        if (receipt.IsSplit)
        {
            return (false, "This receipt is already a split receipt");
        }

        if (receipt.ReceiptItems.Count < 2)
        {
            return (false, "Receipt must have at least 2 items to split");
        }

        if (receipt.PaidAmount > 0)
        {
            return (false, "Cannot split a receipt that has partial payments");
        }

        return (true, null);
    }

    /// <inheritdoc />
    public async Task<SplitResult> SplitReceiptEquallyAsync(
        int receiptId,
        int numberOfWays,
        CancellationToken cancellationToken = default)
    {
        if (numberOfWays < 2 || numberOfWays > 10)
        {
            return SplitResult.Failed("Number of ways must be between 2 and 10");
        }

        var (canSplit, reason) = await CanSplitReceiptAsync(receiptId, cancellationToken);
        if (!canSplit)
        {
            return SplitResult.Failed(reason!);
        }

        var original = await _context.Receipts
            .Include(r => r.ReceiptItems)
            .Include(r => r.Owner)
            .FirstOrDefaultAsync(r => r.Id == receiptId, cancellationToken);

        if (original is null)
        {
            return SplitResult.Failed("Receipt not found");
        }

        var currentUserId = _sessionService.CurrentUserId;
        if (currentUserId == 0)
        {
            return SplitResult.Failed("No user is currently logged in");
        }

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Calculate amount per split with proper rounding
            var amountPerSplit = Math.Ceiling(original.TotalAmount / numberOfWays * 100) / 100;
            var remainder = original.TotalAmount - (amountPerSplit * numberOfWays);

            var splitReceipts = new List<Receipt>();

            for (int i = 1; i <= numberOfWays; i++)
            {
                var splitAmount = amountPerSplit;
                // Add remainder to last split to ensure total matches
                if (i == numberOfWays && remainder != 0)
                {
                    splitAmount += remainder;
                }

                var receiptNumber = await _receiptService.GenerateReceiptNumberAsync().ConfigureAwait(false);
                var taxRate = original.Subtotal > 0 ? original.TaxAmount / original.Subtotal : 0.16m;
                var subtotal = splitAmount / (1 + taxRate);
                var taxAmount = splitAmount - subtotal;

                var splitReceipt = new Receipt
                {
                    ReceiptNumber = receiptNumber,
                    OrderId = original.OrderId,
                    WorkPeriodId = original.WorkPeriodId,
                    OwnerId = currentUserId,
                    TableNumber = original.TableNumber,
                    CustomerName = $"{original.CustomerName} (Split {i}/{numberOfWays})",
                    Subtotal = Math.Round(subtotal, 2),
                    TaxAmount = Math.Round(taxAmount, 2),
                    DiscountAmount = 0,
                    TotalAmount = splitAmount,
                    Status = ReceiptStatus.Pending,
                    PaidAmount = 0,
                    ChangeAmount = 0,
                    ParentReceiptId = original.Id,
                    IsSplit = true,
                    SplitNumber = i,
                    SplitType = SplitType.Equal
                };

                _context.Receipts.Add(splitReceipt);
                splitReceipts.Add(splitReceipt);
            }

            // Update original receipt status
            original.Status = ReceiptStatus.Split;
            original.UpdatedAt = DateTime.UtcNow;

            // Create audit log
            var auditLog = new AuditLog
            {
                UserId = currentUserId,
                Action = "ReceiptSplit",
                EntityType = "Receipt",
                EntityId = original.Id,
                NewValues = JsonSerializer.Serialize(new
                {
                    OriginalReceiptId = original.Id,
                    OriginalReceiptNumber = original.ReceiptNumber,
                    SplitType = "Equal",
                    NumberOfWays = numberOfWays,
                    AmountPerSplit = amountPerSplit
                }),
                MachineName = Environment.MachineName,
                CreatedAt = DateTime.UtcNow
            };
            _context.AuditLogs.Add(auditLog);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.Information(
                "Receipt {ReceiptNumber} split equally into {NumberOfWays} parts. " +
                "Split receipts: {SplitReceiptNumbers}",
                original.ReceiptNumber,
                numberOfWays,
                string.Join(", ", splitReceipts.Select(s => s.ReceiptNumber)));

            return SplitResult.Successful(original, splitReceipts);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.Error(ex, "Failed to split receipt {ReceiptId} equally", receiptId);
            return SplitResult.Failed($"Failed to split receipt: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<SplitResult> SplitReceiptByItemsAsync(
        int receiptId,
        List<SplitItemRequest> splitRequests,
        CancellationToken cancellationToken = default)
    {
        if (!splitRequests.Any())
        {
            return SplitResult.Failed("At least one split request is required");
        }

        var (canSplit, reason) = await CanSplitReceiptAsync(receiptId, cancellationToken);
        if (!canSplit)
        {
            return SplitResult.Failed(reason!);
        }

        var original = await _context.Receipts
            .Include(r => r.ReceiptItems)
                .ThenInclude(ri => ri.Product)
            .Include(r => r.Owner)
            .FirstOrDefaultAsync(r => r.Id == receiptId, cancellationToken);

        if (original is null)
        {
            return SplitResult.Failed("Receipt not found");
        }

        var currentUserId = _sessionService.CurrentUserId;
        if (currentUserId == 0)
        {
            return SplitResult.Failed("No user is currently logged in");
        }

        // Validate that all item IDs exist in the receipt
        var allItemIds = splitRequests.SelectMany(s => s.ItemIds).Distinct().ToList();
        var validItemIds = original.ReceiptItems.Select(ri => ri.Id).ToHashSet();
        var invalidIds = allItemIds.Except(validItemIds).ToList();
        if (invalidIds.Any())
        {
            return SplitResult.Failed($"Invalid item IDs: {string.Join(", ", invalidIds)}");
        }

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var splitReceipts = new List<Receipt>();
            var splitNumber = 1;

            foreach (var request in splitRequests)
            {
                if (!request.ItemIds.Any())
                {
                    continue;
                }

                var itemsForSplit = original.ReceiptItems
                    .Where(ri => request.ItemIds.Contains(ri.Id))
                    .ToList();

                var receiptNumber = await _receiptService.GenerateReceiptNumberAsync().ConfigureAwait(false);
                var subtotal = itemsForSplit.Sum(i => i.TotalAmount - i.TaxAmount);
                var taxAmount = itemsForSplit.Sum(i => i.TaxAmount);
                var totalAmount = itemsForSplit.Sum(i => i.TotalAmount);
                var discountAmount = itemsForSplit.Sum(i => i.DiscountAmount);

                var splitReceipt = new Receipt
                {
                    ReceiptNumber = receiptNumber,
                    OrderId = original.OrderId,
                    WorkPeriodId = original.WorkPeriodId,
                    OwnerId = currentUserId,
                    TableNumber = original.TableNumber,
                    CustomerName = request.CustomerName ?? $"{original.CustomerName} (Split {splitNumber})",
                    Subtotal = subtotal,
                    TaxAmount = taxAmount,
                    DiscountAmount = discountAmount,
                    TotalAmount = totalAmount,
                    Status = ReceiptStatus.Pending,
                    PaidAmount = 0,
                    ChangeAmount = 0,
                    ParentReceiptId = original.Id,
                    IsSplit = true,
                    SplitNumber = splitNumber++,
                    SplitType = SplitType.ByItem
                };

                // Create new receipt items for the split (copy from original)
                foreach (var item in itemsForSplit)
                {
                    splitReceipt.ReceiptItems.Add(new ReceiptItem
                    {
                        OrderItemId = item.OrderItemId,
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        DiscountAmount = item.DiscountAmount,
                        TaxAmount = item.TaxAmount,
                        TotalAmount = item.TotalAmount,
                        Modifiers = item.Modifiers,
                        Notes = item.Notes
                    });
                }

                _context.Receipts.Add(splitReceipt);
                splitReceipts.Add(splitReceipt);
            }

            // Determine which items remain in the original
            var movedItemIds = splitRequests.SelectMany(s => s.ItemIds).ToHashSet();
            var remainingItems = original.ReceiptItems.Where(ri => !movedItemIds.Contains(ri.Id)).ToList();

            if (remainingItems.Any())
            {
                // Recalculate original totals for remaining items
                original.Subtotal = remainingItems.Sum(i => i.TotalAmount - i.TaxAmount);
                original.TaxAmount = remainingItems.Sum(i => i.TaxAmount);
                original.TotalAmount = remainingItems.Sum(i => i.TotalAmount);
                original.DiscountAmount = remainingItems.Sum(i => i.DiscountAmount);
                original.UpdatedAt = DateTime.UtcNow;

                // Remove moved items from original
                var itemsToRemove = original.ReceiptItems.Where(ri => movedItemIds.Contains(ri.Id)).ToList();
                foreach (var item in itemsToRemove)
                {
                    _context.ReceiptItems.Remove(item);
                }
            }
            else
            {
                // All items moved - mark original as split
                original.Status = ReceiptStatus.Split;
                original.Subtotal = 0;
                original.TaxAmount = 0;
                original.TotalAmount = 0;
                original.UpdatedAt = DateTime.UtcNow;
            }

            // Create audit log
            var auditLog = new AuditLog
            {
                UserId = currentUserId,
                Action = "ReceiptSplit",
                EntityType = "Receipt",
                EntityId = original.Id,
                NewValues = JsonSerializer.Serialize(new
                {
                    OriginalReceiptId = original.Id,
                    OriginalReceiptNumber = original.ReceiptNumber,
                    SplitType = "ByItem",
                    NumberOfSplits = splitReceipts.Count,
                    ItemsPerSplit = splitRequests.Select(s => s.ItemIds.Count).ToList()
                }),
                MachineName = Environment.MachineName,
                CreatedAt = DateTime.UtcNow
            };
            _context.AuditLogs.Add(auditLog);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.Information(
                "Receipt {ReceiptNumber} split by items into {NumberOfSplits} receipts. " +
                "Split receipts: {SplitReceiptNumbers}",
                original.ReceiptNumber,
                splitReceipts.Count,
                string.Join(", ", splitReceipts.Select(s => s.ReceiptNumber)));

            return SplitResult.Successful(original, splitReceipts);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.Error(ex, "Failed to split receipt {ReceiptId} by items", receiptId);
            return SplitResult.Failed($"Failed to split receipt: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Receipt>> GetSplitReceiptsAsync(
        int parentReceiptId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Receipts
            .Include(r => r.ReceiptItems)
            .Include(r => r.Owner)
            .Include(r => r.Payments)
            .Where(r => r.ParentReceiptId == parentReceiptId)
            .OrderBy(r => r.SplitNumber)
            .ToListAsync(cancellationToken);
    }
}
