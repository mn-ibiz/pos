using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents an account in the chart of accounts for accounting.
/// </summary>
public class ChartOfAccount : BaseEntity
{
    /// <summary>
    /// Account code (e.g., 1000, 1010, 2000).
    /// </summary>
    public string AccountCode { get; set; } = string.Empty;

    /// <summary>
    /// Account name/title.
    /// </summary>
    public string AccountName { get; set; } = string.Empty;

    /// <summary>
    /// Alias for AccountName for compatibility.
    /// </summary>
    public string Name { get => AccountName; set => AccountName = value; }

    /// <summary>
    /// Primary account type (Asset, Liability, Equity, Revenue, Expense).
    /// </summary>
    public AccountType AccountType { get; set; }

    /// <summary>
    /// Sub-type for detailed classification.
    /// </summary>
    public AccountSubType? AccountSubType { get; set; }

    /// <summary>
    /// Parent account ID for hierarchical structure.
    /// </summary>
    public int? ParentAccountId { get; set; }

    /// <summary>
    /// Account description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this is a system-created account that cannot be deleted.
    /// </summary>
    public bool IsSystemAccount { get; set; }

    /// <summary>
    /// Hierarchical level (1 = top-level, 2 = sub-account, etc.).
    /// </summary>
    public int Level { get; set; } = 1;

    /// <summary>
    /// Full path for display (e.g., "Assets > Current Assets > Cash").
    /// </summary>
    public string? FullPath { get; set; }

    /// <summary>
    /// Normal balance type (Debit or Credit).
    /// </summary>
    public NormalBalance NormalBalance { get; set; }

    /// <summary>
    /// Opening balance when account was created.
    /// </summary>
    public decimal OpeningBalance { get; set; }

    /// <summary>
    /// Opening balance date.
    /// </summary>
    public DateTime? OpeningBalanceDate { get; set; }

    /// <summary>
    /// Current balance (updated by triggers or background job).
    /// </summary>
    public decimal CurrentBalance { get; set; }

    /// <summary>
    /// Last balance calculation timestamp.
    /// </summary>
    public DateTime? BalanceLastCalculated { get; set; }

    /// <summary>
    /// Whether this account is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether transactions can be posted to this account.
    /// </summary>
    public bool AllowPosting { get; set; } = true;

    /// <summary>
    /// Whether this is a header/summary account (no posting allowed).
    /// </summary>
    public bool IsHeaderAccount { get; set; }

    /// <summary>
    /// Tax code if applicable (for automatic tax calculations).
    /// </summary>
    public string? TaxCode { get; set; }

    /// <summary>
    /// Currency code (default is KES).
    /// </summary>
    public string CurrencyCode { get; set; } = "KES";

    /// <summary>
    /// Associated bank account number if this is a bank account.
    /// </summary>
    public string? BankAccountNumber { get; set; }

    /// <summary>
    /// Bank name if this is a bank account.
    /// </summary>
    public string? BankName { get; set; }

    /// <summary>
    /// Sort order for display.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Notes about the account.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Store ID if account is store-specific.
    /// </summary>
    public int? StoreId { get; set; }

    // Navigation properties
    public virtual ChartOfAccount? ParentAccount { get; set; }
    public virtual ICollection<ChartOfAccount> SubAccounts { get; set; } = new List<ChartOfAccount>();
    public virtual ICollection<JournalEntryLine> JournalEntryLines { get; set; } = new List<JournalEntryLine>();
    public virtual ICollection<AccountBalance> AccountBalances { get; set; } = new List<AccountBalance>();
    public virtual ICollection<AccountBudget> Budgets { get; set; } = new List<AccountBudget>();
    public virtual Store? Store { get; set; }

    /// <summary>
    /// Gets the debit/credit balance based on normal balance type.
    /// </summary>
    public decimal GetNormalizedBalance()
    {
        return NormalBalance == NormalBalance.Debit ? CurrentBalance : -CurrentBalance;
    }

    /// <summary>
    /// Determines if this account increases with a debit.
    /// </summary>
    public bool IncreasesWithDebit => AccountType == AccountType.Asset || AccountType == AccountType.Expense;
}
