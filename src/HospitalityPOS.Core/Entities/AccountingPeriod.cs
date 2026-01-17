using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents an accounting period for financial reporting.
/// </summary>
public class AccountingPeriod : BaseEntity
{
    public string PeriodName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public AccountingPeriodStatus Status { get; set; } = AccountingPeriodStatus.Open;
    public int? ClosedByUserId { get; set; }
    public DateTime? ClosedAt { get; set; }

    // Navigation properties
    public virtual User? ClosedByUser { get; set; }
    public virtual ICollection<JournalEntry> JournalEntries { get; set; } = new List<JournalEntry>();
}
