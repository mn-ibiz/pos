namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a line item in a journal entry (debit or credit).
/// </summary>
public class JournalEntryLine : BaseEntity
{
    /// <summary>
    /// Parent journal entry ID.
    /// </summary>
    public int JournalEntryId { get; set; }

    /// <summary>
    /// GL account ID.
    /// </summary>
    public int AccountId { get; set; }

    /// <summary>
    /// Line description/memo.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Debit amount (0 if credit line).
    /// </summary>
    public decimal DebitAmount { get; set; }

    /// <summary>
    /// Credit amount (0 if debit line).
    /// </summary>
    public decimal CreditAmount { get; set; }

    /// <summary>
    /// Line number for ordering.
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Reference for linking to source line (e.g., receipt item).
    /// </summary>
    public string? LineReference { get; set; }

    /// <summary>
    /// Tax code if applicable.
    /// </summary>
    public string? TaxCode { get; set; }

    /// <summary>
    /// Tax amount if separated.
    /// </summary>
    public decimal? TaxAmount { get; set; }

    /// <summary>
    /// Cost center/department for departmental reporting.
    /// </summary>
    public string? CostCenter { get; set; }

    /// <summary>
    /// Project code for project-based accounting.
    /// </summary>
    public string? ProjectCode { get; set; }

    /// <summary>
    /// Whether this line has been reconciled (for bank accounts).
    /// </summary>
    public bool IsReconciled { get; set; }

    /// <summary>
    /// Reconciliation date.
    /// </summary>
    public DateTime? ReconciledDate { get; set; }

    /// <summary>
    /// Bank reconciliation ID if reconciled.
    /// </summary>
    public int? BankReconciliationId { get; set; }

    // Navigation properties
    public virtual JournalEntry JournalEntry { get; set; } = null!;
    public virtual ChartOfAccount Account { get; set; } = null!;

    /// <summary>
    /// Gets the net amount (positive for debit, negative for credit).
    /// </summary>
    public decimal NetAmount => DebitAmount - CreditAmount;

    /// <summary>
    /// Gets whether this is a debit line.
    /// </summary>
    public bool IsDebit => DebitAmount > 0;
}
