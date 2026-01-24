using HospitalityPOS.Core.DTOs;
using HospitalityPOS.Core.Models.Reports;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service for generating combined (multi-terminal) reports.
/// Aggregates data from all terminals for a unified view.
/// </summary>
public interface ICombinedReportService
{
    /// <summary>
    /// Generates a combined X-Report for all terminals in the current work period.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Combined X-Report data from all terminals.</returns>
    Task<CombinedXReportData> GenerateCombinedXReportAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a combined X-Report for all terminals in a specific work period.
    /// </summary>
    /// <param name="workPeriodId">The work period ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Combined X-Report data from all terminals.</returns>
    Task<CombinedXReportData> GenerateCombinedXReportForWorkPeriodAsync(
        int workPeriodId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a combined Z-Report preview for all terminals in a work period.
    /// </summary>
    /// <param name="workPeriodId">The work period ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Combined Z-Report preview.</returns>
    Task<CombinedZReportPreview> PreviewCombinedZReportAsync(
        int workPeriodId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a combined Z-Report can be generated for all terminals.
    /// </summary>
    /// <param name="workPeriodId">The work period ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result indicating if generation is allowed.</returns>
    Task<CombinedZReportValidationResult> ValidateCombinedZReportAsync(
        int workPeriodId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets X-Report summaries for all terminals in the current work period.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of terminal X-Report summaries.</returns>
    Task<List<TerminalXReportSummary>> GetTerminalXReportSummariesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets X-Report summaries for all terminals in a specific work period.
    /// </summary>
    /// <param name="workPeriodId">The work period ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of terminal X-Report summaries.</returns>
    Task<List<TerminalXReportSummary>> GetTerminalXReportSummariesForWorkPeriodAsync(
        int workPeriodId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets Z-Report summaries for all terminals in a specific work period.
    /// </summary>
    /// <param name="workPeriodId">The work period ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of terminal Z-Report summaries.</returns>
    Task<List<TerminalZReportSummary>> GetTerminalZReportSummariesAsync(
        int workPeriodId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Combined X-Report data from all terminals.
/// </summary>
public class CombinedXReportData
{
    /// <summary>
    /// Business name.
    /// </summary>
    public string BusinessName { get; set; } = string.Empty;

    /// <summary>
    /// Business address.
    /// </summary>
    public string BusinessAddress { get; set; } = string.Empty;

    /// <summary>
    /// Business phone.
    /// </summary>
    public string BusinessPhone { get; set; } = string.Empty;

    /// <summary>
    /// Combined report number.
    /// </summary>
    public string ReportNumber { get; set; } = string.Empty;

    /// <summary>
    /// When the report was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// User who generated the report.
    /// </summary>
    public string GeneratedByName { get; set; } = string.Empty;

    /// <summary>
    /// Work period ID.
    /// </summary>
    public int WorkPeriodId { get; set; }

    /// <summary>
    /// When the work period (shift) started.
    /// </summary>
    public DateTime ShiftStarted { get; set; }

    /// <summary>
    /// Current time at report generation.
    /// </summary>
    public DateTime CurrentTime { get; set; }

    /// <summary>
    /// Shift duration formatted.
    /// </summary>
    public string ShiftDurationFormatted { get; set; } = string.Empty;

    /// <summary>
    /// Number of terminals included.
    /// </summary>
    public int TerminalCount { get; set; }

    /// <summary>
    /// Per-terminal breakdown.
    /// </summary>
    public List<TerminalXReportSummary> TerminalBreakdown { get; set; } = [];

    // Combined Totals
    /// <summary>
    /// Combined gross sales from all terminals.
    /// </summary>
    public decimal GrossSales { get; set; }

    /// <summary>
    /// Combined discounts from all terminals.
    /// </summary>
    public decimal Discounts { get; set; }

    /// <summary>
    /// Combined refunds from all terminals.
    /// </summary>
    public decimal Refunds { get; set; }

    /// <summary>
    /// Combined net sales from all terminals.
    /// </summary>
    public decimal NetSales { get; set; }

    /// <summary>
    /// Combined tax from all terminals.
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Combined tips from all terminals.
    /// </summary>
    public decimal TipsCollected { get; set; }

    /// <summary>
    /// Combined grand total from all terminals.
    /// </summary>
    public decimal GrandTotal { get; set; }

    /// <summary>
    /// Combined transaction count from all terminals.
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Average transaction value across all terminals.
    /// </summary>
    public decimal AverageTransaction => TransactionCount > 0 ? GrandTotal / TransactionCount : 0;

    /// <summary>
    /// Combined void count from all terminals.
    /// </summary>
    public int VoidCount { get; set; }

    /// <summary>
    /// Combined refund count from all terminals.
    /// </summary>
    public int RefundCount { get; set; }

    /// <summary>
    /// Combined discount count from all terminals.
    /// </summary>
    public int DiscountCount { get; set; }

    /// <summary>
    /// Combined payment method breakdown.
    /// </summary>
    public List<PaymentMethodBreakdownItem> PaymentBreakdown { get; set; } = [];

    /// <summary>
    /// Combined cashier session breakdown.
    /// </summary>
    public List<CashierSessionBreakdownItem> CashierSessions { get; set; } = [];

    // Cash Drawer Totals
    /// <summary>
    /// Combined opening float from all terminals.
    /// </summary>
    public decimal OpeningFloat { get; set; }

    /// <summary>
    /// Combined cash received from all terminals.
    /// </summary>
    public decimal CashReceived { get; set; }

    /// <summary>
    /// Combined cash refunds from all terminals.
    /// </summary>
    public decimal CashRefunds { get; set; }

    /// <summary>
    /// Combined cash payouts from all terminals.
    /// </summary>
    public decimal CashPayouts { get; set; }

    /// <summary>
    /// Combined expected cash from all terminals.
    /// </summary>
    public decimal ExpectedCash { get; set; }
}

/// <summary>
/// Terminal-specific X-Report summary.
/// </summary>
public class TerminalXReportSummary
{
    /// <summary>
    /// Terminal ID.
    /// </summary>
    public int TerminalId { get; set; }

    /// <summary>
    /// Terminal code.
    /// </summary>
    public string TerminalCode { get; set; } = string.Empty;

    /// <summary>
    /// Terminal name.
    /// </summary>
    public string TerminalName { get; set; } = string.Empty;

    /// <summary>
    /// Whether the terminal is online.
    /// </summary>
    public bool IsOnline { get; set; }

    /// <summary>
    /// Terminal gross sales.
    /// </summary>
    public decimal GrossSales { get; set; }

    /// <summary>
    /// Terminal net sales.
    /// </summary>
    public decimal NetSales { get; set; }

    /// <summary>
    /// Terminal grand total.
    /// </summary>
    public decimal GrandTotal { get; set; }

    /// <summary>
    /// Terminal transaction count.
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Terminal void count.
    /// </summary>
    public int VoidCount { get; set; }

    /// <summary>
    /// Terminal refund count.
    /// </summary>
    public int RefundCount { get; set; }

    /// <summary>
    /// Terminal expected cash.
    /// </summary>
    public decimal ExpectedCash { get; set; }

    /// <summary>
    /// Cashier sessions on this terminal.
    /// </summary>
    public List<CashierSessionBreakdownItem> CashierSessions { get; set; } = [];
}

/// <summary>
/// Terminal-specific Z-Report summary.
/// </summary>
public class TerminalZReportSummary
{
    /// <summary>
    /// Terminal ID.
    /// </summary>
    public int TerminalId { get; set; }

    /// <summary>
    /// Terminal code.
    /// </summary>
    public string TerminalCode { get; set; } = string.Empty;

    /// <summary>
    /// Terminal name.
    /// </summary>
    public string TerminalName { get; set; } = string.Empty;

    /// <summary>
    /// Whether Z-Report exists for this terminal.
    /// </summary>
    public bool HasZReport { get; set; }

    /// <summary>
    /// Z-Report ID if exists.
    /// </summary>
    public int? ZReportId { get; set; }

    /// <summary>
    /// Z-Report number if exists.
    /// </summary>
    public string? ZReportNumber { get; set; }

    /// <summary>
    /// Terminal gross sales.
    /// </summary>
    public decimal GrossSales { get; set; }

    /// <summary>
    /// Terminal net sales.
    /// </summary>
    public decimal NetSales { get; set; }

    /// <summary>
    /// Terminal grand total.
    /// </summary>
    public decimal GrandTotal { get; set; }

    /// <summary>
    /// Terminal transaction count.
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Terminal expected cash.
    /// </summary>
    public decimal ExpectedCash { get; set; }

    /// <summary>
    /// Actual cash counted (if Z-Report exists).
    /// </summary>
    public decimal? ActualCash { get; set; }

    /// <summary>
    /// Variance (if Z-Report exists).
    /// </summary>
    public decimal? Variance { get; set; }
}

/// <summary>
/// Combined Z-Report preview.
/// </summary>
public class CombinedZReportPreview
{
    /// <summary>
    /// Work period ID.
    /// </summary>
    public int WorkPeriodId { get; set; }

    /// <summary>
    /// When the work period started.
    /// </summary>
    public DateTime WorkPeriodStart { get; set; }

    /// <summary>
    /// Combined gross sales.
    /// </summary>
    public decimal TotalGrossSales { get; set; }

    /// <summary>
    /// Combined net sales.
    /// </summary>
    public decimal TotalNetSales { get; set; }

    /// <summary>
    /// Combined grand total.
    /// </summary>
    public decimal TotalGrandTotal { get; set; }

    /// <summary>
    /// Combined transaction count.
    /// </summary>
    public int TotalTransactionCount { get; set; }

    /// <summary>
    /// Number of terminals.
    /// </summary>
    public int TerminalCount { get; set; }

    /// <summary>
    /// Per-terminal breakdown.
    /// </summary>
    public List<TerminalZReportSummary> TerminalBreakdown { get; set; } = [];

    /// <summary>
    /// Number of terminals with completed Z-Reports.
    /// </summary>
    public int CompletedZReportCount { get; set; }

    /// <summary>
    /// Number of terminals pending Z-Report.
    /// </summary>
    public int PendingZReportCount { get; set; }
}

/// <summary>
/// Combined Z-Report validation result.
/// </summary>
public class CombinedZReportValidationResult
{
    /// <summary>
    /// Whether all terminals can generate Z-Reports.
    /// </summary>
    public bool AllTerminalsReady { get; set; }

    /// <summary>
    /// Terminals that are ready for Z-Report.
    /// </summary>
    public List<int> ReadyTerminals { get; set; } = [];

    /// <summary>
    /// Terminals that are not ready with reasons.
    /// </summary>
    public List<TerminalValidationIssue> TerminalIssues { get; set; } = [];

    /// <summary>
    /// Whether there are unsettled receipts across any terminal.
    /// </summary>
    public bool HasUnsettledReceipts { get; set; }

    /// <summary>
    /// Total unsettled receipt count.
    /// </summary>
    public int TotalUnsettledReceiptCount { get; set; }

    /// <summary>
    /// Whether there are open orders across any terminal.
    /// </summary>
    public bool HasOpenOrders { get; set; }

    /// <summary>
    /// Total open order count.
    /// </summary>
    public int TotalOpenOrderCount { get; set; }
}

/// <summary>
/// Validation issue for a specific terminal.
/// </summary>
public class TerminalValidationIssue
{
    /// <summary>
    /// Terminal ID.
    /// </summary>
    public int TerminalId { get; set; }

    /// <summary>
    /// Terminal code.
    /// </summary>
    public string TerminalCode { get; set; } = string.Empty;

    /// <summary>
    /// List of issues.
    /// </summary>
    public List<string> Issues { get; set; } = [];
}

/// <summary>
/// Payment method breakdown item (enhanced for combined reports).
/// </summary>
public class PaymentMethodBreakdownItem
{
    /// <summary>
    /// Payment method ID.
    /// </summary>
    public int PaymentMethodId { get; set; }

    /// <summary>
    /// Payment method name.
    /// </summary>
    public string PaymentMethodName { get; set; } = string.Empty;

    /// <summary>
    /// Total amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Transaction count.
    /// </summary>
    public int TransactionCount { get; set; }
}

/// <summary>
/// Cashier session breakdown item (enhanced for combined reports).
/// </summary>
public class CashierSessionBreakdownItem
{
    /// <summary>
    /// User ID.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Cashier name.
    /// </summary>
    public string CashierName { get; set; } = string.Empty;

    /// <summary>
    /// Terminal ID.
    /// </summary>
    public int TerminalId { get; set; }

    /// <summary>
    /// Terminal code.
    /// </summary>
    public string TerminalCode { get; set; } = string.Empty;

    /// <summary>
    /// Session start time.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Session end time.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Session duration.
    /// </summary>
    public TimeSpan Duration => (EndTime ?? DateTime.UtcNow) - StartTime;

    /// <summary>
    /// Formatted duration.
    /// </summary>
    public string DurationFormatted
    {
        get
        {
            var d = Duration;
            return $"{(int)d.TotalHours}h {d.Minutes:D2}m";
        }
    }

    /// <summary>
    /// Transaction count.
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Sales total.
    /// </summary>
    public decimal SalesTotal { get; set; }
}
