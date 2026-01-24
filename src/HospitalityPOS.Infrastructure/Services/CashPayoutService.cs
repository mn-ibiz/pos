using Microsoft.EntityFrameworkCore;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing cash payouts during work periods.
/// </summary>
public class CashPayoutService : ICashPayoutService
{
    private readonly POSDbContext _context;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CashPayoutService"/> class.
    /// </summary>
    public CashPayoutService(POSDbContext context, ILogger logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<CashPayout> RecordPayoutAsync(
        int workPeriodId,
        decimal amount,
        PayoutReason reason,
        int userId,
        string? customReason = null,
        string? reference = null,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        // Validate work period exists and is open
        var workPeriod = await _context.WorkPeriods
            .FirstOrDefaultAsync(wp => wp.Id == workPeriodId, cancellationToken)
            .ConfigureAwait(false);

        if (workPeriod is null)
        {
            throw new InvalidOperationException($"Work period {workPeriodId} not found.");
        }

        if (workPeriod.Status != WorkPeriodStatus.Open)
        {
            throw new InvalidOperationException("Cannot record payout for a closed work period.");
        }

        // Validate amount
        if (amount <= 0)
        {
            throw new ArgumentException("Payout amount must be greater than zero.", nameof(amount));
        }

        // Validate custom reason if reason is Other
        if (reason == PayoutReason.Other && string.IsNullOrWhiteSpace(customReason))
        {
            throw new ArgumentException("Custom reason is required when reason is 'Other'.", nameof(customReason));
        }

        var payout = new CashPayout
        {
            WorkPeriodId = workPeriodId,
            Amount = amount,
            Reason = reason,
            CustomReason = customReason?.Trim(),
            Reference = reference?.Trim(),
            Notes = notes?.Trim(),
            RecordedByUserId = userId,
            RecordedAt = DateTime.UtcNow,
            Status = PayoutStatus.Approved // Auto-approve for now, can add approval workflow later
        };

        await _context.CashPayouts.AddAsync(payout, cancellationToken).ConfigureAwait(false);

        // Create audit log
        var auditLog = new AuditLog
        {
            UserId = userId,
            Action = "CashPayoutRecorded",
            EntityType = nameof(CashPayout),
            NewValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                WorkPeriodId = workPeriodId,
                Amount = amount,
                Reason = reason.ToString(),
                CustomReason = customReason,
                Reference = reference
            }),
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information(
            "Cash payout of {Amount:C} recorded for work period {WorkPeriodId}. Reason: {Reason}",
            amount, workPeriodId, reason);

        return payout;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CashPayout>> GetPayoutsForWorkPeriodAsync(
        int workPeriodId,
        CancellationToken cancellationToken = default)
    {
        return await _context.CashPayouts
            .AsNoTracking()
            .Include(p => p.RecordedByUser)
            .Include(p => p.ApprovedByUser)
            .Where(p => p.WorkPeriodId == workPeriodId)
            .OrderByDescending(p => p.RecordedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<decimal> GetTotalPayoutsAsync(
        int workPeriodId,
        CancellationToken cancellationToken = default)
    {
        return await _context.CashPayouts
            .Where(p => p.WorkPeriodId == workPeriodId)
            .Where(p => p.Status == PayoutStatus.Approved)
            .SumAsync(p => p.Amount, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<CashPayout?> GetByIdAsync(int payoutId, CancellationToken cancellationToken = default)
    {
        return await _context.CashPayouts
            .AsNoTracking()
            .Include(p => p.RecordedByUser)
            .Include(p => p.ApprovedByUser)
            .Include(p => p.WorkPeriod)
            .FirstOrDefaultAsync(p => p.Id == payoutId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> ApprovePayoutAsync(
        int payoutId,
        int approverUserId,
        CancellationToken cancellationToken = default)
    {
        var payout = await _context.CashPayouts
            .FirstOrDefaultAsync(p => p.Id == payoutId, cancellationToken)
            .ConfigureAwait(false);

        if (payout is null)
        {
            _logger.Warning("Payout {PayoutId} not found for approval", payoutId);
            return false;
        }

        if (payout.Status != PayoutStatus.Pending)
        {
            _logger.Warning("Payout {PayoutId} is not pending - cannot approve", payoutId);
            return false;
        }

        payout.Status = PayoutStatus.Approved;
        payout.ApprovedByUserId = approverUserId;
        payout.ApprovedAt = DateTime.UtcNow;

        // Audit log
        var auditLog = new AuditLog
        {
            UserId = approverUserId,
            Action = "CashPayoutApproved",
            EntityType = nameof(CashPayout),
            EntityId = payoutId,
            NewValues = System.Text.Json.JsonSerializer.Serialize(new { Amount = payout.Amount }),
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Cash payout {PayoutId} approved by user {UserId}", payoutId, approverUserId);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> RejectPayoutAsync(
        int payoutId,
        int approverUserId,
        string rejectionReason,
        CancellationToken cancellationToken = default)
    {
        var payout = await _context.CashPayouts
            .FirstOrDefaultAsync(p => p.Id == payoutId, cancellationToken)
            .ConfigureAwait(false);

        if (payout is null)
        {
            _logger.Warning("Payout {PayoutId} not found for rejection", payoutId);
            return false;
        }

        if (payout.Status != PayoutStatus.Pending)
        {
            _logger.Warning("Payout {PayoutId} is not pending - cannot reject", payoutId);
            return false;
        }

        payout.Status = PayoutStatus.Rejected;
        payout.ApprovedByUserId = approverUserId;
        payout.ApprovedAt = DateTime.UtcNow;
        payout.RejectionReason = rejectionReason?.Trim();

        // Audit log
        var auditLog = new AuditLog
        {
            UserId = approverUserId,
            Action = "CashPayoutRejected",
            EntityType = nameof(CashPayout),
            EntityId = payoutId,
            NewValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                Amount = payout.Amount,
                RejectionReason = rejectionReason
            }),
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Cash payout {PayoutId} rejected by user {UserId}. Reason: {Reason}",
            payoutId, approverUserId, rejectionReason);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeletePayoutAsync(
        int payoutId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var payout = await _context.CashPayouts
            .FirstOrDefaultAsync(p => p.Id == payoutId, cancellationToken)
            .ConfigureAwait(false);

        if (payout is null)
        {
            _logger.Warning("Payout {PayoutId} not found for deletion", payoutId);
            return false;
        }

        // Only allow deletion of pending payouts by the same user
        if (payout.Status != PayoutStatus.Pending)
        {
            _logger.Warning("Cannot delete payout {PayoutId} - not pending", payoutId);
            return false;
        }

        if (payout.RecordedByUserId != userId)
        {
            _logger.Warning("User {UserId} cannot delete payout {PayoutId} - not owner", userId, payoutId);
            return false;
        }

        _context.CashPayouts.Remove(payout);

        // Audit log
        var auditLog = new AuditLog
        {
            UserId = userId,
            Action = "CashPayoutDeleted",
            EntityType = nameof(CashPayout),
            EntityId = payoutId,
            OldValues = System.Text.Json.JsonSerializer.Serialize(new { Amount = payout.Amount }),
            MachineName = Environment.MachineName,
            CreatedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken).ConfigureAwait(false);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information("Cash payout {PayoutId} deleted by user {UserId}", payoutId, userId);

        return true;
    }
}
