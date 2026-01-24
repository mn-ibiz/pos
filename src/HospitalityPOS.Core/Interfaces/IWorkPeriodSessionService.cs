using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for managing work period sessions (cashier login/logout tracking).
/// </summary>
public interface IWorkPeriodSessionService
{
    /// <summary>
    /// Starts a new session when a cashier logs in.
    /// </summary>
    /// <param name="workPeriodId">The work period ID.</param>
    /// <param name="terminalId">The terminal ID.</param>
    /// <param name="userId">The user (cashier) ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created session.</returns>
    Task<WorkPeriodSession> StartSessionAsync(
        int workPeriodId,
        int terminalId,
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ends the current session when a cashier logs out.
    /// </summary>
    /// <param name="sessionId">The session ID to end.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ended session.</returns>
    Task<WorkPeriodSession> EndSessionAsync(
        int sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ends all active sessions for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task EndAllSessionsForUserAsync(
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the active session for a user on a terminal.
    /// </summary>
    /// <param name="terminalId">The terminal ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The active session if found.</returns>
    Task<WorkPeriodSession?> GetActiveSessionAsync(
        int terminalId,
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current active session for a terminal (any user).
    /// </summary>
    /// <param name="terminalId">The terminal ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The active session if found.</returns>
    Task<WorkPeriodSession?> GetActiveSessionForTerminalAsync(
        int terminalId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all sessions for a work period.
    /// </summary>
    /// <param name="workPeriodId">The work period ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of sessions.</returns>
    Task<IReadOnlyList<WorkPeriodSession>> GetSessionsByWorkPeriodAsync(
        int workPeriodId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates session totals after a transaction.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="update">The transaction update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateSessionTotalsAsync(
        int sessionId,
        SessionTransactionUpdate update,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets session summary for reporting.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The session summary.</returns>
    Task<SessionSummary?> GetSessionSummaryAsync(
        int sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all sessions for a terminal on a specific date.
    /// </summary>
    /// <param name="terminalId">The terminal ID.</param>
    /// <param name="date">The date to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of sessions.</returns>
    Task<IReadOnlyList<WorkPeriodSession>> GetSessionsByDateAsync(
        int terminalId,
        DateTime date,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all sessions for a user across all terminals.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="startDate">Start date filter.</param>
    /// <param name="endDate">End date filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of sessions.</returns>
    Task<IReadOnlyList<WorkPeriodSession>> GetSessionsByUserAsync(
        int userId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Transaction update data for session totals.
/// </summary>
public class SessionTransactionUpdate
{
    /// <summary>
    /// Gets or sets the sale amount (positive for sales, negative for refunds).
    /// </summary>
    public decimal SaleAmount { get; set; }

    /// <summary>
    /// Gets or sets the cash amount received.
    /// </summary>
    public decimal CashAmount { get; set; }

    /// <summary>
    /// Gets or sets the card payment amount.
    /// </summary>
    public decimal CardAmount { get; set; }

    /// <summary>
    /// Gets or sets the M-Pesa payment amount.
    /// </summary>
    public decimal MpesaAmount { get; set; }

    /// <summary>
    /// Gets or sets whether this is a refund.
    /// </summary>
    public bool IsRefund { get; set; }

    /// <summary>
    /// Gets or sets whether this is a void.
    /// </summary>
    public bool IsVoid { get; set; }

    /// <summary>
    /// Gets or sets the discount amount applied.
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Gets or sets any cash paid out (e.g., refund cash).
    /// </summary>
    public decimal CashPaidOut { get; set; }
}

/// <summary>
/// Session summary for reporting.
/// </summary>
public class SessionSummary
{
    /// <summary>
    /// Gets or sets the session ID.
    /// </summary>
    public int SessionId { get; set; }

    /// <summary>
    /// Gets or sets the user name.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the terminal code.
    /// </summary>
    public string TerminalCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the login time.
    /// </summary>
    public DateTime LoginAt { get; set; }

    /// <summary>
    /// Gets or sets the logout time.
    /// </summary>
    public DateTime? LogoutAt { get; set; }

    /// <summary>
    /// Gets or sets the session duration.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets the total sales.
    /// </summary>
    public decimal SalesTotal { get; set; }

    /// <summary>
    /// Gets or sets the transaction count.
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Gets or sets the average transaction value.
    /// </summary>
    public decimal AverageTransaction => TransactionCount > 0 ? SalesTotal / TransactionCount : 0;

    /// <summary>
    /// Gets or sets the cash received.
    /// </summary>
    public decimal CashReceived { get; set; }

    /// <summary>
    /// Gets or sets the card total.
    /// </summary>
    public decimal CardTotal { get; set; }

    /// <summary>
    /// Gets or sets the M-Pesa total.
    /// </summary>
    public decimal MpesaTotal { get; set; }

    /// <summary>
    /// Gets or sets the refund total.
    /// </summary>
    public decimal RefundTotal { get; set; }

    /// <summary>
    /// Gets or sets the void total.
    /// </summary>
    public decimal VoidTotal { get; set; }

    /// <summary>
    /// Gets or sets the discount total.
    /// </summary>
    public decimal DiscountTotal { get; set; }
}
