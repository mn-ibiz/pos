using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a journal entry in the accounting system.
/// </summary>
public class JournalEntry : BaseEntity
{
    public string EntryNumber { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public string? Description { get; set; }
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    public int? AccountingPeriodId { get; set; }
    public JournalEntryStatus Status { get; set; } = JournalEntryStatus.Posted;
    public bool IsPosted { get; set; }

    // Navigation properties
    public virtual AccountingPeriod? AccountingPeriod { get; set; }
    public virtual User? CreatedByUser { get; set; }
    public virtual ICollection<JournalEntryLine> JournalEntryLines { get; set; } = new List<JournalEntryLine>();
}
