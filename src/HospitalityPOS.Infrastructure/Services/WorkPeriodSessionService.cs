using Microsoft.EntityFrameworkCore;
using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Infrastructure.Data;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Service for managing work period sessions (cashier login/logout tracking).
/// </summary>
public class WorkPeriodSessionService : IWorkPeriodSessionService
{
    private readonly POSDbContext _context;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkPeriodSessionService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger.</param>
    public WorkPeriodSessionService(POSDbContext context, ILogger logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<WorkPeriodSession> StartSessionAsync(
        int workPeriodId,
        int terminalId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        // Check for existing active session for this user on this terminal
        var existingSession = await GetActiveSessionAsync(terminalId, userId, cancellationToken)
            .ConfigureAwait(false);

        if (existingSession is not null)
        {
            _logger.Information(
                "Resuming existing session {SessionId} for user {UserId} on terminal {TerminalId}",
                existingSession.Id, userId, terminalId);
            return existingSession;
        }

        var session = new WorkPeriodSession
        {
            WorkPeriodId = workPeriodId,
            TerminalId = terminalId,
            UserId = userId,
            LoginAt = DateTime.UtcNow,
            SalesTotal = 0,
            TransactionCount = 0,
            CashReceived = 0,
            CashPaidOut = 0,
            RefundTotal = 0,
            VoidTotal = 0,
            DiscountTotal = 0,
            CardTotal = 0,
            MpesaTotal = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _context.WorkPeriodSessions.AddAsync(session, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information(
            "Started new session {SessionId} for user {UserId} on terminal {TerminalId} in work period {WorkPeriodId}",
            session.Id, userId, terminalId, workPeriodId);

        return session;
    }

    /// <inheritdoc />
    public async Task<WorkPeriodSession> EndSessionAsync(
        int sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await _context.WorkPeriodSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken)
            .ConfigureAwait(false);

        if (session is null)
        {
            throw new InvalidOperationException($"Session {sessionId} not found.");
        }

        if (session.LogoutAt.HasValue)
        {
            _logger.Warning("Session {SessionId} already ended at {LogoutAt}", sessionId, session.LogoutAt);
            return session;
        }

        session.LogoutAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.Information(
            "Ended session {SessionId} for user {UserId}. Duration: {Duration}, Sales: {Sales}",
            session.Id, session.UserId, session.Duration, session.SalesTotal);

        return session;
    }

    /// <inheritdoc />
    public async Task EndAllSessionsForUserAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var activeSessions = await _context.WorkPeriodSessions
            .Where(s => s.UserId == userId && s.LogoutAt == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var session in activeSessions)
        {
            session.LogoutAt = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;
        }

        if (activeSessions.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.Information("Ended {Count} active sessions for user {UserId}", activeSessions.Count, userId);
        }
    }

    /// <inheritdoc />
    public async Task<WorkPeriodSession?> GetActiveSessionAsync(
        int terminalId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.WorkPeriodSessions
            .AsNoTracking()
            .Include(s => s.WorkPeriod)
            .Include(s => s.Terminal)
            .Include(s => s.User)
            .FirstOrDefaultAsync(
                s => s.TerminalId == terminalId && s.UserId == userId && s.LogoutAt == null,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<WorkPeriodSession?> GetActiveSessionForTerminalAsync(
        int terminalId,
        CancellationToken cancellationToken = default)
    {
        return await _context.WorkPeriodSessions
            .AsNoTracking()
            .Include(s => s.WorkPeriod)
            .Include(s => s.User)
            .FirstOrDefaultAsync(
                s => s.TerminalId == terminalId && s.LogoutAt == null,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WorkPeriodSession>> GetSessionsByWorkPeriodAsync(
        int workPeriodId,
        CancellationToken cancellationToken = default)
    {
        return await _context.WorkPeriodSessions
            .AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.Terminal)
            .Where(s => s.WorkPeriodId == workPeriodId)
            .OrderBy(s => s.LoginAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpdateSessionTotalsAsync(
        int sessionId,
        SessionTransactionUpdate update,
        CancellationToken cancellationToken = default)
    {
        var session = await _context.WorkPeriodSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken)
            .ConfigureAwait(false);

        if (session is null)
        {
            _logger.Warning("Cannot update totals - session {SessionId} not found", sessionId);
            return;
        }

        if (update.IsVoid)
        {
            session.VoidTotal += Math.Abs(update.SaleAmount);
        }
        else if (update.IsRefund)
        {
            session.RefundTotal += Math.Abs(update.SaleAmount);
            session.CashPaidOut += update.CashPaidOut;
        }
        else
        {
            session.SalesTotal += update.SaleAmount;
            session.TransactionCount++;
            session.CashReceived += update.CashAmount;
            session.CardTotal += update.CardAmount;
            session.MpesaTotal += update.MpesaAmount;
        }

        session.DiscountTotal += update.DiscountAmount;
        session.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SessionSummary?> GetSessionSummaryAsync(
        int sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await _context.WorkPeriodSessions
            .AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.Terminal)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken)
            .ConfigureAwait(false);

        if (session is null)
        {
            return null;
        }

        return new SessionSummary
        {
            SessionId = session.Id,
            UserName = session.User?.Username ?? "Unknown",
            TerminalCode = session.Terminal?.Code ?? "Unknown",
            LoginAt = session.LoginAt,
            LogoutAt = session.LogoutAt,
            Duration = session.Duration,
            SalesTotal = session.SalesTotal,
            TransactionCount = session.TransactionCount,
            CashReceived = session.CashReceived,
            CardTotal = session.CardTotal,
            MpesaTotal = session.MpesaTotal,
            RefundTotal = session.RefundTotal,
            VoidTotal = session.VoidTotal,
            DiscountTotal = session.DiscountTotal
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WorkPeriodSession>> GetSessionsByDateAsync(
        int terminalId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        return await _context.WorkPeriodSessions
            .AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.WorkPeriod)
            .Where(s => s.TerminalId == terminalId &&
                        s.LoginAt >= startOfDay &&
                        s.LoginAt < endOfDay)
            .OrderBy(s => s.LoginAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WorkPeriodSession>> GetSessionsByUserAsync(
        int userId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.WorkPeriodSessions
            .AsNoTracking()
            .Include(s => s.Terminal)
            .Include(s => s.WorkPeriod)
            .Where(s => s.UserId == userId);

        if (startDate.HasValue)
        {
            query = query.Where(s => s.LoginAt >= startDate.Value.Date);
        }

        if (endDate.HasValue)
        {
            query = query.Where(s => s.LoginAt < endDate.Value.Date.AddDays(1));
        }

        return await query
            .OrderByDescending(s => s.LoginAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
