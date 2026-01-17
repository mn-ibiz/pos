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
/// Service for merging multiple receipts into one.
/// </summary>
public class ReceiptMergeService : IReceiptMergeService
{
    private readonly POSDbContext _context;
    private readonly IReceiptService _receiptService;
    private readonly ISessionService _sessionService;
    private readonly IWorkPeriodService _workPeriodService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReceiptMergeService"/> class.
    /// </summary>
    public ReceiptMergeService(
        POSDbContext context,
        IReceiptService receiptService,
        ISessionService sessionService,
        IWorkPeriodService workPeriodService,
        ILogger logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _receiptService = receiptService ?? throw new ArgumentNullException(nameof(receiptService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _workPeriodService = workPeriodService ?? throw new ArgumentNullException(nameof(workPeriodService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<(bool CanMerge, string? Reason)> CanMergeReceiptsAsync(
        List<int> receiptIds,
        CancellationToken cancellationToken = default)
    {
        if (receiptIds.Count < 2)
        {
            return (false, "At least 2 receipts are required to merge");
        }

        var receipts = await _context.Receipts
            .AsNoTracking()
            .Where(r => receiptIds.Contains(r.Id))
            .ToListAsync(cancellationToken);

        if (receipts.Count != receiptIds.Count)
        {
            var missingIds = receiptIds.Except(receipts.Select(r => r.Id));
            return (false, $"Receipts not found: {string.Join(", ", missingIds)}");
        }

        // Check all are pending
        var nonPending = receipts.Where(r => r.Status != ReceiptStatus.Pending).ToList();
        if (nonPending.Any())
        {
            return (false, $"Cannot merge non-pending receipts: {string.Join(", ", nonPending.Select(r => r.ReceiptNumber))}");
        }

        // Check same work period
        var workPeriods = receipts.Select(r => r.WorkPeriodId).Distinct().ToList();
        if (workPeriods.Count > 1)
        {
            return (false, "All receipts must be from the same work period");
        }

        // Check none already merged
        var alreadyMerged = receipts.Where(r => r.MergedIntoReceiptId.HasValue || r.IsMerged).ToList();
        if (alreadyMerged.Any())
        {
            return (false, $"Receipts already merged: {string.Join(", ", alreadyMerged.Select(r => r.ReceiptNumber))}");
        }

        // Check none are split receipts (parent receipts that were split)
        var splitParents = receipts.Where(r => r.Status == ReceiptStatus.Split).ToList();
        if (splitParents.Any())
        {
            return (false, $"Cannot merge split parent receipts: {string.Join(", ", splitParents.Select(r => r.ReceiptNumber))}");
        }

        return (true, null);
    }

    /// <inheritdoc />
    public async Task<MergeResult> MergeReceiptsAsync(
        List<int> receiptIds,
        CancellationToken cancellationToken = default)
    {
        var (canMerge, reason) = await CanMergeReceiptsAsync(receiptIds, cancellationToken);
        if (!canMerge)
        {
            return MergeResult.Failed(reason!);
        }

        var currentUserId = _sessionService.CurrentUserId;
        if (currentUserId == 0)
        {
            return MergeResult.Failed("No user is currently logged in");
        }

        var receipts = await _context.Receipts
            .Include(r => r.ReceiptItems)
                .ThenInclude(ri => ri.Product)
            .Include(r => r.Owner)
            .Where(r => receiptIds.Contains(r.Id))
            .ToListAsync(cancellationToken);

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Create merged receipt
            var receiptNumber = await _receiptService.GenerateReceiptNumberAsync().ConfigureAwait(false);
            var firstReceipt = receipts.First();

            var mergedReceipt = new Receipt
            {
                ReceiptNumber = receiptNumber,
                OrderId = firstReceipt.OrderId,
                WorkPeriodId = firstReceipt.WorkPeriodId,
                OwnerId = currentUserId,
                TableNumber = string.Join(", ", receipts
                    .Select(r => r.TableNumber)
                    .Where(t => !string.IsNullOrEmpty(t))
                    .Distinct()),
                CustomerName = string.Join(", ", receipts
                    .Select(r => r.CustomerName)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Distinct()),
                Status = ReceiptStatus.Pending,
                PaidAmount = 0,
                ChangeAmount = 0,
                IsMerged = true
            };

            // Copy all items from source receipts
            decimal totalSubtotal = 0;
            decimal totalTax = 0;
            decimal totalDiscount = 0;

            foreach (var receipt in receipts)
            {
                foreach (var item in receipt.ReceiptItems)
                {
                    mergedReceipt.ReceiptItems.Add(new ReceiptItem
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
                        Notes = $"From {receipt.ReceiptNumber}: {item.Notes}".Trim()
                    });
                }

                totalSubtotal += receipt.Subtotal;
                totalTax += receipt.TaxAmount;
                totalDiscount += receipt.DiscountAmount;
            }

            // Set totals
            mergedReceipt.Subtotal = totalSubtotal;
            mergedReceipt.TaxAmount = totalTax;
            mergedReceipt.DiscountAmount = totalDiscount;
            mergedReceipt.TotalAmount = totalSubtotal + totalTax - totalDiscount;

            _context.Receipts.Add(mergedReceipt);
            await _context.SaveChangesAsync(cancellationToken);

            // Update source receipts to reference merged receipt
            foreach (var receipt in receipts)
            {
                receipt.Status = ReceiptStatus.Merged;
                receipt.MergedIntoReceiptId = mergedReceipt.Id;
                receipt.UpdatedAt = DateTime.UtcNow;
            }

            // Create audit log
            var auditLog = new AuditLog
            {
                UserId = currentUserId,
                Action = "ReceiptMerge",
                EntityType = "Receipt",
                EntityId = mergedReceipt.Id,
                NewValues = JsonSerializer.Serialize(new
                {
                    MergedReceiptId = mergedReceipt.Id,
                    MergedReceiptNumber = mergedReceipt.ReceiptNumber,
                    SourceReceiptIds = receiptIds,
                    SourceReceiptNumbers = receipts.Select(r => r.ReceiptNumber).ToList(),
                    CombinedTotal = mergedReceipt.TotalAmount
                }),
                MachineName = Environment.MachineName,
                CreatedAt = DateTime.UtcNow
            };
            _context.AuditLogs.Add(auditLog);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.Information(
                "Merged {ReceiptCount} receipts ({ReceiptNumbers}) into {MergedReceiptNumber}. Total: {TotalAmount}",
                receipts.Count,
                string.Join(", ", receipts.Select(r => r.ReceiptNumber)),
                mergedReceipt.ReceiptNumber,
                mergedReceipt.TotalAmount);

            return MergeResult.Successful(mergedReceipt, receipts);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.Error(ex, "Failed to merge receipts: {ReceiptIds}", string.Join(", ", receiptIds));
            return MergeResult.Failed($"Failed to merge receipts: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Receipt>> GetMergeableReceiptsAsync(
        CancellationToken cancellationToken = default)
    {
        var currentWorkPeriodId = _workPeriodService.CurrentWorkPeriodId;

        return await _context.Receipts
            .Include(r => r.ReceiptItems)
            .Include(r => r.Owner)
            .Where(r => r.WorkPeriodId == currentWorkPeriodId
                && r.Status == ReceiptStatus.Pending
                && !r.MergedIntoReceiptId.HasValue
                && !r.IsMerged)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Receipt>> GetSourceReceiptsAsync(
        int mergedReceiptId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Receipts
            .Include(r => r.ReceiptItems)
            .Include(r => r.Owner)
            .Where(r => r.MergedIntoReceiptId == mergedReceiptId)
            .OrderBy(r => r.ReceiptNumber)
            .ToListAsync(cancellationToken);
    }
}
