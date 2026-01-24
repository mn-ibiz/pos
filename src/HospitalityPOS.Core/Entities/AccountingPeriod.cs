using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents an accounting period for financial reporting.
/// </summary>
public class AccountingPeriod : BaseEntity
{
    /// <summary>
    /// Period name (e.g., "January 2024", "Q1 2024", "FY2024").
    /// </summary>
    public string PeriodName { get; set; } = string.Empty;

    /// <summary>
    /// Period code for quick reference (e.g., "2024-01", "2024-Q1").
    /// </summary>
    public string? PeriodCode { get; set; }

    /// <summary>
    /// Period start date (inclusive).
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Period end date (inclusive).
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Fiscal year this period belongs to.
    /// </summary>
    public int FiscalYear { get; set; }

    /// <summary>
    /// Period number within the fiscal year (1-12 for monthly, 1-4 for quarterly).
    /// </summary>
    public int PeriodNumber { get; set; }

    /// <summary>
    /// Period type (Monthly, Quarterly, Annual).
    /// </summary>
    public string PeriodType { get; set; } = "Monthly";

    /// <summary>
    /// Period status.
    /// </summary>
    public AccountingPeriodStatus Status { get; set; } = AccountingPeriodStatus.Open;

    /// <summary>
    /// Whether this period is locked for editing.
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// User who locked the period.
    /// </summary>
    public int? LockedByUserId { get; set; }

    /// <summary>
    /// Lock date.
    /// </summary>
    public DateTime? LockedAt { get; set; }

    /// <summary>
    /// User who closed the period.
    /// </summary>
    public int? ClosedByUserId { get; set; }

    /// <summary>
    /// Close date.
    /// </summary>
    public DateTime? ClosedAt { get; set; }

    /// <summary>
    /// Whether year-end closing entries have been created.
    /// </summary>
    public bool IsYearEndClosed { get; set; }

    /// <summary>
    /// Year-end closing entry ID if created.
    /// </summary>
    public int? YearEndClosingEntryId { get; set; }

    /// <summary>
    /// Opening balance journal entry ID if set.
    /// </summary>
    public int? OpeningBalanceEntryId { get; set; }

    /// <summary>
    /// Total revenue for the period (cached).
    /// </summary>
    public decimal? TotalRevenue { get; set; }

    /// <summary>
    /// Total expenses for the period (cached).
    /// </summary>
    public decimal? TotalExpenses { get; set; }

    /// <summary>
    /// Net income for the period (cached).
    /// </summary>
    public decimal? NetIncome { get; set; }

    /// <summary>
    /// Date when financials were last calculated.
    /// </summary>
    public DateTime? FinancialsLastCalculated { get; set; }

    /// <summary>
    /// Notes about this period.
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public virtual User? ClosedByUser { get; set; }
    public virtual User? LockedByUser { get; set; }
    public virtual JournalEntry? YearEndClosingEntry { get; set; }
    public virtual JournalEntry? OpeningBalanceEntry { get; set; }
    public virtual ICollection<JournalEntry> JournalEntries { get; set; } = new List<JournalEntry>();
    public virtual ICollection<PeriodClose> PeriodCloses { get; set; } = new List<PeriodClose>();
    public virtual ICollection<FinancialStatement> FinancialStatements { get; set; } = new List<FinancialStatement>();

    /// <summary>
    /// Checks if a date falls within this period.
    /// </summary>
    public bool ContainsDate(DateTime date) => date >= StartDate && date <= EndDate;

    /// <summary>
    /// Checks if posting is allowed to this period.
    /// </summary>
    public bool AllowsPosting => Status == AccountingPeriodStatus.Open && !IsLocked;
}
