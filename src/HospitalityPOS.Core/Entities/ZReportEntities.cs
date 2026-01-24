using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a permanent Z Report record - an immutable fiscal document for end-of-day/shift reporting.
/// Z Reports serve as official accounting records and must be retained for tax compliance (6+ years per HMRC guidelines).
/// </summary>
public class ZReportRecord : BaseEntity
{
    /// <summary>
    /// Sequential report number - unique per store, never resets.
    /// </summary>
    public int ReportNumber { get; set; }

    /// <summary>
    /// Store ID for multi-store operations.
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// Terminal/Register ID for multi-terminal stores.
    /// </summary>
    public int? TerminalId { get; set; }

    /// <summary>
    /// Associated work period ID.
    /// </summary>
    public int WorkPeriodId { get; set; }

    /// <summary>
    /// When the Z Report was generated.
    /// </summary>
    public DateTime ReportDateTime { get; set; }

    /// <summary>
    /// When the reporting period started.
    /// </summary>
    public DateTime PeriodStartDateTime { get; set; }

    /// <summary>
    /// When the reporting period ended.
    /// </summary>
    public DateTime PeriodEndDateTime { get; set; }

    /// <summary>
    /// User who generated the Z Report.
    /// </summary>
    public int GeneratedByUserId { get; set; }

    /// <summary>
    /// Name of user who generated the report (denormalized for historical record).
    /// </summary>
    public string GeneratedByUserName { get; set; } = string.Empty;

    #region Sales Summary

    /// <summary>
    /// Gross sales (before discounts, refunds, voids).
    /// </summary>
    public decimal GrossSales { get; set; }

    /// <summary>
    /// Net sales (after discounts, before tax).
    /// </summary>
    public decimal NetSales { get; set; }

    /// <summary>
    /// Total refunds processed.
    /// </summary>
    public decimal TotalRefunds { get; set; }

    /// <summary>
    /// Total void amounts.
    /// </summary>
    public decimal TotalVoids { get; set; }

    /// <summary>
    /// Total discounts applied.
    /// </summary>
    public decimal TotalDiscounts { get; set; }

    /// <summary>
    /// Total tax collected.
    /// </summary>
    public decimal TotalTax { get; set; }

    /// <summary>
    /// Total tips collected.
    /// </summary>
    public decimal TotalTips { get; set; }

    /// <summary>
    /// Rounding adjustments.
    /// </summary>
    public decimal RoundingAdjustment { get; set; }

    /// <summary>
    /// Grand total (net sales + tax).
    /// </summary>
    public decimal GrandTotal { get; set; }

    #endregion

    #region Cash Reconciliation

    /// <summary>
    /// Cash at start of period.
    /// </summary>
    public decimal OpeningCash { get; set; }

    /// <summary>
    /// Total cash received (sales + pay-ins).
    /// </summary>
    public decimal CashReceived { get; set; }

    /// <summary>
    /// Total cash paid out (refunds + payouts).
    /// </summary>
    public decimal CashPaidOut { get; set; }

    /// <summary>
    /// Expected cash based on calculations.
    /// </summary>
    public decimal ExpectedCash { get; set; }

    /// <summary>
    /// Actual cash counted in drawer.
    /// </summary>
    public decimal ActualCash { get; set; }

    /// <summary>
    /// Variance between expected and actual cash.
    /// </summary>
    public decimal CashVariance { get; set; }

    /// <summary>
    /// Explanation for variance if any.
    /// </summary>
    public string? VarianceExplanation { get; set; }

    /// <summary>
    /// Whether variance requires management approval.
    /// </summary>
    public bool VarianceRequiresApproval { get; set; }

    /// <summary>
    /// Manager who approved variance (if applicable).
    /// </summary>
    public int? VarianceApprovedByUserId { get; set; }

    /// <summary>
    /// When variance was approved.
    /// </summary>
    public DateTime? VarianceApprovedAt { get; set; }

    #endregion

    #region Transaction Statistics

    /// <summary>
    /// Number of transactions.
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Number of customers served.
    /// </summary>
    public int CustomerCount { get; set; }

    /// <summary>
    /// Average transaction value.
    /// </summary>
    public decimal AverageTransactionValue { get; set; }

    /// <summary>
    /// Number of void transactions.
    /// </summary>
    public int VoidCount { get; set; }

    /// <summary>
    /// Number of refund transactions.
    /// </summary>
    public int RefundCount { get; set; }

    /// <summary>
    /// Number of discounts applied.
    /// </summary>
    public int DiscountCount { get; set; }

    /// <summary>
    /// Number of cash drawer open events.
    /// </summary>
    public int OpenDrawerCount { get; set; }

    /// <summary>
    /// Number of no-sale transactions.
    /// </summary>
    public int NoSaleCount { get; set; }

    #endregion

    #region Report Integrity

    /// <summary>
    /// SHA-256 hash of report data for integrity verification.
    /// </summary>
    public string ReportHash { get; set; } = string.Empty;

    /// <summary>
    /// Whether the report has been finalized and is immutable.
    /// </summary>
    public bool IsFinalized { get; set; }

    /// <summary>
    /// When the report was finalized.
    /// </summary>
    public DateTime? FinalizedAt { get; set; }

    /// <summary>
    /// Card batch number if batch settlement occurred.
    /// </summary>
    public string? BatchNumber { get; set; }

    /// <summary>
    /// Whether this is a consolidated report across multiple terminals.
    /// </summary>
    public bool IsConsolidated { get; set; }

    /// <summary>
    /// IDs of individual Z reports that were consolidated (if applicable).
    /// </summary>
    public string? ConsolidatedFromReportIds { get; set; }

    #endregion

    #region Full Report Data

    /// <summary>
    /// Complete report data serialized as JSON for historical preservation.
    /// </summary>
    public string ReportDataJson { get; set; } = string.Empty;

    #endregion

    #region Business Information (Denormalized for Report Permanence)

    /// <summary>
    /// Business name at time of report.
    /// </summary>
    public string BusinessName { get; set; } = string.Empty;

    /// <summary>
    /// Business address at time of report.
    /// </summary>
    public string BusinessAddress { get; set; } = string.Empty;

    /// <summary>
    /// Business tax ID at time of report.
    /// </summary>
    public string? TaxId { get; set; }

    /// <summary>
    /// Business phone at time of report.
    /// </summary>
    public string? BusinessPhone { get; set; }

    #endregion

    #region Navigation Properties

    public virtual WorkPeriod WorkPeriod { get; set; } = null!;
    public virtual User GeneratedByUser { get; set; } = null!;
    public virtual User? VarianceApprovedByUser { get; set; }
    public virtual ICollection<ZReportCategorySales> CategorySales { get; set; } = new List<ZReportCategorySales>();
    public virtual ICollection<ZReportPaymentSummary> PaymentSummaries { get; set; } = new List<ZReportPaymentSummary>();
    public virtual ICollection<ZReportHourlySales> HourlySales { get; set; } = new List<ZReportHourlySales>();
    public virtual ICollection<ZReportUserSales> UserSales { get; set; } = new List<ZReportUserSales>();
    public virtual ICollection<ZReportTaxSummary> TaxSummaries { get; set; } = new List<ZReportTaxSummary>();

    #endregion

    #region Helper Methods

    /// <summary>
    /// Computes the SHA-256 hash for report integrity verification.
    /// </summary>
    public string ComputeHash()
    {
        var data = new
        {
            ReportNumber,
            StoreId,
            TerminalId,
            WorkPeriodId,
            PeriodStartDateTime,
            PeriodEndDateTime,
            GrossSales,
            NetSales,
            TotalRefunds,
            TotalVoids,
            TotalDiscounts,
            TotalTax,
            GrandTotal,
            OpeningCash,
            ExpectedCash,
            ActualCash,
            CashVariance,
            TransactionCount
        };

        var json = JsonSerializer.Serialize(data);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes);
    }

    /// <summary>
    /// Verifies the report integrity by comparing stored hash with computed hash.
    /// </summary>
    public bool VerifyIntegrity() => ReportHash == ComputeHash();

    /// <summary>
    /// Gets variance status text.
    /// </summary>
    public string VarianceStatus => CashVariance switch
    {
        < 0 => "SHORT",
        > 0 => "OVER",
        _ => "EXACT"
    };

    #endregion
}

/// <summary>
/// Category-level sales breakdown for a Z Report.
/// </summary>
public class ZReportCategorySales : BaseEntity
{
    public int ZReportRecordId { get; set; }
    public int? CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal CostAmount { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal PercentageOfSales { get; set; }

    // Navigation
    public virtual ZReportRecord ZReportRecord { get; set; } = null!;
}

/// <summary>
/// Payment method breakdown for a Z Report.
/// </summary>
public class ZReportPaymentSummary : BaseEntity
{
    public int ZReportRecordId { get; set; }
    public int? PaymentMethodId { get; set; }
    public string PaymentMethodName { get; set; } = string.Empty;
    public string PaymentMethodType { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal TipAmount { get; set; }
    public decimal PercentageOfSales { get; set; }

    // Navigation
    public virtual ZReportRecord ZReportRecord { get; set; } = null!;
}

/// <summary>
/// Hourly sales breakdown for a Z Report.
/// </summary>
public class ZReportHourlySales : BaseEntity
{
    public int ZReportRecordId { get; set; }
    public int Hour { get; set; }
    public string HourLabel { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public int CustomerCount { get; set; }
    public decimal SalesAmount { get; set; }
    public decimal AverageTransaction { get; set; }
    public bool IsPeakHour { get; set; }

    // Navigation
    public virtual ZReportRecord ZReportRecord { get; set; } = null!;
}

/// <summary>
/// User/Server sales breakdown for a Z Report.
/// </summary>
public class ZReportUserSales : BaseEntity
{
    public int ZReportRecordId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal GrossSales { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetSales { get; set; }
    public decimal TipAmount { get; set; }
    public decimal AverageTransaction { get; set; }
    public int VoidCount { get; set; }
    public decimal VoidAmount { get; set; }
    public int RefundCount { get; set; }
    public decimal RefundAmount { get; set; }

    // Navigation
    public virtual ZReportRecord ZReportRecord { get; set; } = null!;
}

/// <summary>
/// Tax breakdown by type for a Z Report.
/// </summary>
public class ZReportTaxSummary : BaseEntity
{
    public int ZReportRecordId { get; set; }
    public string TaxName { get; set; } = string.Empty;
    public decimal TaxRate { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal TaxAmount { get; set; }

    // Navigation
    public virtual ZReportRecord ZReportRecord { get; set; } = null!;
}

/// <summary>
/// Schedule configuration for automated Z Report generation.
/// </summary>
public class ZReportSchedule : BaseEntity
{
    /// <summary>
    /// Store ID for the schedule.
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// Terminal ID for terminal-specific schedules.
    /// </summary>
    public int? TerminalId { get; set; }

    /// <summary>
    /// Time of day to generate Z Report (local time).
    /// </summary>
    public TimeSpan ScheduledTime { get; set; }

    /// <summary>
    /// Whether automatic generation is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Whether to send email notification before generation.
    /// </summary>
    public bool SendEmailNotification { get; set; }

    /// <summary>
    /// Comma-separated list of notification email addresses.
    /// </summary>
    public string? NotificationEmails { get; set; }

    /// <summary>
    /// Minutes before scheduled time to send warning notification.
    /// </summary>
    public int MinutesWarningBefore { get; set; } = 30;

    /// <summary>
    /// Whether to automatically close open work period when generating.
    /// </summary>
    public bool AutoCloseWorkPeriod { get; set; }

    /// <summary>
    /// Whether to automatically email the report after generation.
    /// </summary>
    public bool EmailReportAfterGeneration { get; set; }

    /// <summary>
    /// Comma-separated list of email addresses to send report to.
    /// </summary>
    public string? ReportRecipientEmails { get; set; }

    /// <summary>
    /// Days of week when schedule is active (bitmask: 1=Sun, 2=Mon, 4=Tue, etc).
    /// </summary>
    public int ActiveDaysOfWeek { get; set; } = 127; // All days by default

    /// <summary>
    /// When the schedule was last executed.
    /// </summary>
    public DateTime? LastExecutedAt { get; set; }

    /// <summary>
    /// When the next execution is expected.
    /// </summary>
    public DateTime? NextExecutionAt { get; set; }

    /// <summary>
    /// Result of last execution.
    /// </summary>
    public string? LastExecutionResult { get; set; }

    #region Helper Methods

    /// <summary>
    /// Checks if the schedule is active on a given day.
    /// </summary>
    public bool IsActiveOnDay(DayOfWeek day)
    {
        var bit = 1 << (int)day;
        return (ActiveDaysOfWeek & bit) != 0;
    }

    /// <summary>
    /// Sets whether the schedule is active on a given day.
    /// </summary>
    public void SetActiveOnDay(DayOfWeek day, bool active)
    {
        var bit = 1 << (int)day;
        if (active)
            ActiveDaysOfWeek |= bit;
        else
            ActiveDaysOfWeek &= ~bit;
    }

    /// <summary>
    /// Gets list of notification email addresses.
    /// </summary>
    public List<string> GetNotificationEmails()
    {
        if (string.IsNullOrWhiteSpace(NotificationEmails))
            return [];
        return NotificationEmails.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    /// <summary>
    /// Gets list of report recipient email addresses.
    /// </summary>
    public List<string> GetReportRecipientEmails()
    {
        if (string.IsNullOrWhiteSpace(ReportRecipientEmails))
            return [];
        return ReportRecipientEmails.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    #endregion
}

/// <summary>
/// Configuration for variance thresholds that require approval.
/// </summary>
public class ZReportVarianceThreshold : BaseEntity
{
    /// <summary>
    /// Store ID (null for global settings).
    /// </summary>
    public int? StoreId { get; set; }

    /// <summary>
    /// Variance amount threshold that triggers approval requirement.
    /// </summary>
    public decimal AmountThreshold { get; set; }

    /// <summary>
    /// Variance percentage threshold that triggers approval requirement.
    /// </summary>
    public decimal? PercentageThreshold { get; set; }

    /// <summary>
    /// Whether variance explanation is required when threshold exceeded.
    /// </summary>
    public bool RequireExplanation { get; set; } = true;

    /// <summary>
    /// Whether manager approval is required when threshold exceeded.
    /// </summary>
    public bool RequireManagerApproval { get; set; }

    /// <summary>
    /// Whether to flag for investigation when threshold exceeded.
    /// </summary>
    public bool FlagForInvestigation { get; set; }

    /// <summary>
    /// Whether this threshold config is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}

#region DTOs

/// <summary>
/// Filter criteria for Z Report searches.
/// </summary>
public class ZReportFilterDto
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? StoreId { get; set; }
    public int? TerminalId { get; set; }
    public int? GeneratedByUserId { get; set; }
    public bool? HasVariance { get; set; }
    public bool? IsConsolidated { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Preview data before generating a Z Report.
/// </summary>
public class ZReportPreview
{
    public int WorkPeriodId { get; set; }
    public DateTime PeriodStartDateTime { get; set; }
    public DateTime? PeriodEndDateTime { get; set; }
    public string OpenedByUserName { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }

    // Sales Summary
    public decimal GrossSales { get; set; }
    public decimal TotalDiscounts { get; set; }
    public decimal NetSales { get; set; }
    public decimal TotalTax { get; set; }
    public decimal TotalTips { get; set; }
    public decimal TotalRefunds { get; set; }
    public decimal TotalVoids { get; set; }
    public decimal GrandTotal { get; set; }

    // Transaction Stats
    public int TransactionCount { get; set; }
    public int VoidCount { get; set; }
    public int RefundCount { get; set; }
    public decimal AverageTransactionValue { get; set; }

    // Cash Position
    public decimal OpeningCash { get; set; }
    public decimal CashReceived { get; set; }
    public decimal CashPaidOut { get; set; }
    public decimal ExpectedCash { get; set; }

    // Validation
    public List<ZReportValidationIssue> ValidationIssues { get; set; } = [];
    public bool CanGenerate => ValidationIssues.Count == 0 || ValidationIssues.All(i => !i.IsBlocking);

    // Breakdown summaries
    public List<CategorySalesSummaryDto> CategorySales { get; set; } = [];
    public List<PaymentSummaryDto> PaymentSummaries { get; set; } = [];
    public List<UserSalesSummaryDto> UserSales { get; set; } = [];
}

/// <summary>
/// Validation issue found during Z Report pre-generation check.
/// </summary>
public class ZReportValidationIssue
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsBlocking { get; set; }
    public string? Resolution { get; set; }
}

/// <summary>
/// Result of Z Report validation check.
/// </summary>
public class ZReportValidationResult
{
    public bool CanGenerate { get; set; }
    public List<ZReportValidationIssue> Issues { get; set; } = [];
    public bool HasUnsettledReceipts { get; set; }
    public int UnsettledReceiptCount { get; set; }
    public bool HasOpenOrders { get; set; }
    public int OpenOrderCount { get; set; }
    public bool IsCashCounted { get; set; }
    public bool HasPreviousPendingReport { get; set; }
}

/// <summary>
/// Category sales summary for preview/report.
/// </summary>
public class CategorySalesSummaryDto
{
    public int? CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal NetAmount { get; set; }
}

/// <summary>
/// Payment summary for preview/report.
/// </summary>
public class PaymentSummaryDto
{
    public int? PaymentMethodId { get; set; }
    public string PaymentMethodName { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
}

/// <summary>
/// User sales summary for preview/report.
/// </summary>
public class UserSalesSummaryDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal TotalSales { get; set; }
}

/// <summary>
/// Schedule configuration DTO.
/// </summary>
public class ZReportScheduleDto
{
    public TimeSpan ScheduledTime { get; set; }
    public bool IsEnabled { get; set; }
    public bool SendEmailNotification { get; set; }
    public List<string> NotificationEmails { get; set; } = [];
    public int MinutesWarningBefore { get; set; } = 30;
    public bool AutoCloseWorkPeriod { get; set; }
    public bool EmailReportAfterGeneration { get; set; }
    public List<string> ReportRecipientEmails { get; set; } = [];
    public List<DayOfWeek> ActiveDays { get; set; } = [];
}

/// <summary>
/// Request to generate a Z Report.
/// </summary>
public class GenerateZReportRequest
{
    public int WorkPeriodId { get; set; }
    public decimal ActualCashCounted { get; set; }
    public string? VarianceExplanation { get; set; }
    public bool ForceGenerate { get; set; }
    public int GeneratedByUserId { get; set; }
}

/// <summary>
/// Z Report summary for list views.
/// </summary>
public class ZReportSummaryDto
{
    public int Id { get; set; }
    public int ReportNumber { get; set; }
    public DateTime ReportDateTime { get; set; }
    public DateTime PeriodStartDateTime { get; set; }
    public DateTime PeriodEndDateTime { get; set; }
    public string GeneratedByUserName { get; set; } = string.Empty;
    public decimal GrandTotal { get; set; }
    public int TransactionCount { get; set; }
    public decimal CashVariance { get; set; }
    public string VarianceStatus { get; set; } = string.Empty;
    public bool IsConsolidated { get; set; }
}

#endregion
