namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a line item in a journal entry (debit or credit).
/// </summary>
public class JournalEntryLine : BaseEntity
{
    public int JournalEntryId { get; set; }
    public int AccountId { get; set; }
    public string? Description { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }

    // Navigation properties
    public virtual JournalEntry JournalEntry { get; set; } = null!;
    public virtual ChartOfAccount Account { get; set; } = null!;
}
