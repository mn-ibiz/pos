using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents an account in the chart of accounts for accounting.
/// </summary>
public class ChartOfAccount : BaseEntity
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public int? ParentAccountId { get; set; }
    public string? Description { get; set; }
    public bool IsSystemAccount { get; set; }

    // Navigation properties
    public virtual ChartOfAccount? ParentAccount { get; set; }
    public virtual ICollection<ChartOfAccount> SubAccounts { get; set; } = new List<ChartOfAccount>();
    public virtual ICollection<JournalEntryLine> JournalEntryLines { get; set; } = new List<JournalEntryLine>();
}
