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
/// Service for voiding receipts with full audit trail and stock restoration.
/// </summary>
public class ReceiptVoidService : IReceiptVoidService
{
    private readonly POSDbContext _context;
    private readonly ISessionService _sessionService;
    private readonly IInventoryService _inventoryService;
    private readonly IEtimsService? _etimsService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReceiptVoidService"/> class.
    /// </summary>
    public ReceiptVoidService(
        POSDbContext context,
        ISessionService sessionService,
        IInventoryService inventoryService,
        ILogger logger,
        IEtimsService? etimsService = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
        _etimsService = etimsService; // Optional - eTIMS integration
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<VoidResult> VoidReceiptAsync(
        VoidRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var currentUserId = _sessionService.CurrentUserId;
        if (currentUserId == 0)
        {
            return VoidResult.Failed("No user is currently logged in");
        }

        var (canVoid, reason) = await CanVoidReceiptAsync(request.ReceiptId, cancellationToken);
        if (!canVoid)
        {
            return VoidResult.Failed(reason!);
        }

        // Validate void reason exists
        var voidReason = await _context.VoidReasons
            .FirstOrDefaultAsync(vr => vr.Id == request.VoidReasonId && vr.IsActive, cancellationToken);

        if (voidReason is null)
        {
            return VoidResult.Failed("Invalid void reason");
        }

        // Check if notes are required but not provided
        if (voidReason.RequiresNote && string.IsNullOrWhiteSpace(request.AdditionalNotes))
        {
            return VoidResult.Failed("Additional notes are required for this void reason");
        }

        var receipt = await _context.Receipts
            .Include(r => r.ReceiptItems)
                .ThenInclude(ri => ri.Product)
            .Include(r => r.Owner)
            .Include(r => r.WorkPeriod)
            .FirstOrDefaultAsync(r => r.Id == request.ReceiptId, cancellationToken);

        if (receipt is null)
        {
            return VoidResult.Failed("Receipt not found");
        }

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Create void record
            var voidRecord = new ReceiptVoid
            {
                ReceiptId = receipt.Id,
                VoidReasonId = request.VoidReasonId,
                AdditionalNotes = request.AdditionalNotes,
                VoidedByUserId = currentUserId,
                AuthorizedByUserId = request.AuthorizedByUserId,
                VoidedAmount = receipt.TotalAmount,
                VoidedAt = DateTime.UtcNow,
                StockRestored = false
            };

            // Update receipt status
            receipt.Status = ReceiptStatus.Voided;
            receipt.VoidedAt = DateTime.UtcNow;
            receipt.VoidedByUserId = currentUserId;
            receipt.VoidReason = $"{voidReason.Name}: {request.AdditionalNotes ?? ""}".Trim();
            receipt.UpdatedAt = DateTime.UtcNow;

            // Restore stock for inventory-tracked items
            var stockRestored = await RestoreStockForReceiptAsync(receipt, cancellationToken);
            voidRecord.StockRestored = stockRestored;

            _context.ReceiptVoids.Add(voidRecord);

            // Create audit log
            var auditLog = new AuditLog
            {
                UserId = currentUserId,
                Action = AuditActionType.ReceiptVoided.ToString(),
                EntityType = "Receipt",
                EntityId = receipt.Id,
                OldValues = JsonSerializer.Serialize(new
                {
                    Status = "Pending",
                    TotalAmount = receipt.TotalAmount
                }),
                NewValues = JsonSerializer.Serialize(new
                {
                    Status = "Voided",
                    VoidReasonId = request.VoidReasonId,
                    VoidReason = voidReason.Name,
                    AdditionalNotes = request.AdditionalNotes,
                    VoidedAmount = receipt.TotalAmount,
                    AuthorizedByUserId = request.AuthorizedByUserId,
                    StockRestored = stockRestored
                }),
                MachineName = Environment.MachineName,
                CreatedAt = DateTime.UtcNow
            };
            _context.AuditLogs.Add(auditLog);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.Information(
                "Receipt {ReceiptNumber} voided by user {UserId}. Reason: {VoidReason}. Amount: {Amount}. Stock restored: {StockRestored}",
                receipt.ReceiptNumber,
                currentUserId,
                voidReason.Name,
                receipt.TotalAmount,
                stockRestored);

            // Submit eTIMS credit note for tax compliance (Kenya KRA requirement)
            await SubmitEtimsCreditNoteAsync(receipt.Id, voidReason.Name, request.AdditionalNotes);

            return VoidResult.Successful(voidRecord, receipt);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.Error(ex, "Failed to void receipt {ReceiptId}", request.ReceiptId);
            return VoidResult.Failed($"Failed to void receipt: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<(bool CanVoid, string? Reason)> CanVoidReceiptAsync(
        int receiptId,
        CancellationToken cancellationToken = default)
    {
        var receipt = await _context.Receipts
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == receiptId, cancellationToken);

        if (receipt is null)
        {
            return (false, "Receipt not found");
        }

        if (receipt.Status == ReceiptStatus.Voided)
        {
            return (false, "Receipt is already voided");
        }

        if (receipt.Status == ReceiptStatus.Split)
        {
            return (false, "Cannot void a split receipt. Void the child receipts instead.");
        }

        if (receipt.Status == ReceiptStatus.Merged)
        {
            return (false, "Cannot void a merged receipt. Void the merged-into receipt instead.");
        }

        // Check if work period is still open (optional - can be allowed for managers)
        var workPeriod = await _context.WorkPeriods
            .AsNoTracking()
            .FirstOrDefaultAsync(wp => wp.Id == receipt.WorkPeriodId, cancellationToken);

        if (workPeriod != null && workPeriod.Status == WorkPeriodStatus.Closed)
        {
            // Work period is closed - requires elevated permissions
            // This is a warning, not a blocker - UI should request manager override
        }

        return (true, null);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<VoidReason>> GetVoidReasonsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.VoidReasons
            .Where(vr => vr.IsActive)
            .OrderBy(vr => vr.DisplayOrder)
            .ThenBy(vr => vr.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ReceiptVoid?> GetVoidRecordAsync(int receiptId, CancellationToken cancellationToken = default)
    {
        return await _context.ReceiptVoids
            .Include(rv => rv.VoidReason)
            .Include(rv => rv.VoidedByUser)
            .Include(rv => rv.AuthorizedByUser)
            .FirstOrDefaultAsync(rv => rv.ReceiptId == receiptId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<VoidReportItem>> GetVoidReportAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.ReceiptVoids
            .Where(v => v.VoidedAt >= startDate && v.VoidedAt <= endDate)
            .Include(v => v.Receipt)
            .Include(v => v.VoidReason)
            .Include(v => v.VoidedByUser)
            .Include(v => v.AuthorizedByUser)
            .Select(v => new VoidReportItem
            {
                ReceiptNumber = v.Receipt.ReceiptNumber,
                VoidedAmount = v.VoidedAmount,
                VoidReason = v.VoidReason.Name,
                Notes = v.AdditionalNotes,
                VoidedBy = v.VoidedByUser.DisplayName,
                AuthorizedBy = v.AuthorizedByUser != null ? v.AuthorizedByUser.DisplayName : null,
                VoidedAt = v.VoidedAt
            })
            .OrderByDescending(v => v.VoidedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<decimal> GetTotalVoidedAmountAsync(int workPeriodId, CancellationToken cancellationToken = default)
    {
        return await _context.ReceiptVoids
            .Include(rv => rv.Receipt)
            .Where(rv => rv.Receipt.WorkPeriodId == workPeriodId)
            .SumAsync(rv => rv.VoidedAmount, cancellationToken);
    }

    /// <summary>
    /// Restores stock for items in the voided receipt using the centralized InventoryService.
    /// The InventoryService has built-in idempotency checks to prevent double restoration.
    /// </summary>
    private async Task<bool> RestoreStockForReceiptAsync(Receipt receipt, CancellationToken cancellationToken)
    {
        // Use the centralized InventoryService which has idempotency built in
        // This prevents double restoration if void is attempted multiple times
        var movements = await _inventoryService.RestoreStockForVoidAsync(receipt);
        var movementCount = movements.Count();

        if (movementCount > 0)
        {
            _logger.Information(
                "Stock restored for {ProductCount} products in voided receipt {ReceiptNumber}",
                movementCount,
                receipt.ReceiptNumber);
        }

        return movementCount > 0;
    }

    /// <summary>
    /// Submits an eTIMS credit note when a receipt with an existing eTIMS invoice is voided.
    /// This operation is non-blocking - failures are queued for retry.
    /// </summary>
    /// <param name="receiptId">The voided receipt ID.</param>
    /// <param name="voidReason">The void reason name.</param>
    /// <param name="additionalNotes">Additional notes for the void.</param>
    private async Task SubmitEtimsCreditNoteAsync(int receiptId, string voidReason, string? additionalNotes)
    {
        if (_etimsService == null)
        {
            // eTIMS not configured - skip credit note submission
            return;
        }

        try
        {
            // Check if this receipt has an eTIMS invoice
            var existingInvoice = await _etimsService.GetInvoiceByReceiptIdAsync(receiptId);
            if (existingInvoice == null)
            {
                // No eTIMS invoice for this receipt - nothing to reverse
                _logger.Debug("Receipt {ReceiptId} has no eTIMS invoice - credit note not required", receiptId);
                return;
            }

            // Only submit credit note if the invoice was successfully accepted by KRA
            if (existingInvoice.Status != Enums.EtimsSubmissionStatus.Accepted)
            {
                _logger.Debug("eTIMS invoice {InvoiceNumber} was not accepted - credit note not required",
                    existingInvoice.InvoiceNumber);
                return;
            }

            // Build reason string
            var creditNoteReason = string.IsNullOrWhiteSpace(additionalNotes)
                ? voidReason
                : $"{voidReason}: {additionalNotes}";

            // Generate credit note from the original invoice
            var creditNote = await _etimsService.GenerateCreditNoteAsync(
                existingInvoice.Id,
                creditNoteReason);

            // Attempt real-time submission
            var submittedCreditNote = await _etimsService.SubmitCreditNoteAsync(creditNote.Id);

            if (submittedCreditNote.Status == Enums.EtimsSubmissionStatus.Accepted)
            {
                _logger.Information(
                    "eTIMS credit note {CreditNoteNumber} submitted successfully for voided receipt {ReceiptId}",
                    submittedCreditNote.CreditNoteNumber, receiptId);
            }
            else if (submittedCreditNote.Status == Enums.EtimsSubmissionStatus.Failed ||
                     submittedCreditNote.Status == Enums.EtimsSubmissionStatus.Rejected)
            {
                // Queue for retry
                await _etimsService.QueueForSubmissionAsync(
                    Enums.EtimsDocumentType.CreditNote,
                    creditNote.Id,
                    priority: 50);

                _logger.Warning(
                    "eTIMS credit note {CreditNoteNumber} submission failed, queued for retry: {Error}",
                    submittedCreditNote.CreditNoteNumber, submittedCreditNote.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            // Don't block void operations if eTIMS fails
            _logger.Error(ex, "eTIMS credit note submission error for voided receipt {ReceiptId} - will retry later", receiptId);

            // Try to queue for later submission if credit note was created
            try
            {
                var existingInvoice = await _etimsService.GetInvoiceByReceiptIdAsync(receiptId);
                if (existingInvoice != null)
                {
                    var creditNotes = await _etimsService.GetCreditNotesByInvoiceAsync(existingInvoice.Id);
                    var pendingCreditNote = creditNotes.FirstOrDefault(cn =>
                        cn.Status == Enums.EtimsSubmissionStatus.Pending ||
                        cn.Status == Enums.EtimsSubmissionStatus.Failed);

                    if (pendingCreditNote != null)
                    {
                        await _etimsService.QueueForSubmissionAsync(
                            Enums.EtimsDocumentType.CreditNote,
                            pendingCreditNote.Id,
                            priority: 100);
                    }
                }
            }
            catch (Exception queueEx)
            {
                _logger.Error(queueEx, "Failed to queue eTIMS credit note for receipt {ReceiptId}", receiptId);
            }
        }
    }
}
