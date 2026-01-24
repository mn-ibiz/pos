namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a cashier session within a work period.
/// Tracks individual cashier sales when multiple cashiers work the same terminal.
/// </summary>
public class WorkPeriodSession : BaseEntity
{
    /// <summary>
    /// Gets or sets the work period ID this session belongs to.
    /// </summary>
    public int WorkPeriodId { get; set; }

    /// <summary>
    /// Gets or sets the terminal ID this session occurred on.
    /// </summary>
    public int TerminalId { get; set; }

    /// <summary>
    /// Gets or sets the user (cashier) ID for this session.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets when the cashier logged in.
    /// </summary>
    public DateTime LoginAt { get; set; }

    /// <summary>
    /// Gets or sets when the cashier logged out (null if still active).
    /// </summary>
    public DateTime? LogoutAt { get; set; }

    /// <summary>
    /// Gets or sets the total sales amount during this session.
    /// </summary>
    public decimal SalesTotal { get; set; }

    /// <summary>
    /// Gets or sets the number of transactions during this session.
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Gets or sets the total cash received during this session.
    /// </summary>
    public decimal CashReceived { get; set; }

    /// <summary>
    /// Gets or sets the total cash paid out during this session.
    /// </summary>
    public decimal CashPaidOut { get; set; }

    /// <summary>
    /// Gets or sets the total refund amount during this session.
    /// </summary>
    public decimal RefundTotal { get; set; }

    /// <summary>
    /// Gets or sets the total voided amount during this session.
    /// </summary>
    public decimal VoidTotal { get; set; }

    /// <summary>
    /// Gets or sets the total discount amount during this session.
    /// </summary>
    public decimal DiscountTotal { get; set; }

    /// <summary>
    /// Gets or sets the total card/mobile payment received.
    /// </summary>
    public decimal CardTotal { get; set; }

    /// <summary>
    /// Gets or sets the total M-Pesa payment received.
    /// </summary>
    public decimal MpesaTotal { get; set; }

    /// <summary>
    /// Gets whether this session is currently active (no logout).
    /// </summary>
    public bool IsSessionActive => LogoutAt is null;

    /// <summary>
    /// Gets the session duration (or duration since login if still active).
    /// </summary>
    public TimeSpan Duration => (LogoutAt ?? DateTime.UtcNow) - LoginAt;

    #region Navigation Properties

    /// <summary>
    /// Gets or sets the work period.
    /// </summary>
    public virtual WorkPeriod? WorkPeriod { get; set; }

    /// <summary>
    /// Gets or sets the terminal.
    /// </summary>
    public virtual Terminal? Terminal { get; set; }

    /// <summary>
    /// Gets or sets the user (cashier).
    /// </summary>
    public virtual User? User { get; set; }

    #endregion
}
