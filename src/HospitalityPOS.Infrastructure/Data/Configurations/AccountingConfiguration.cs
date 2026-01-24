using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for ChartOfAccount entity.
/// </summary>
public class ChartOfAccountConfiguration : IEntityTypeConfiguration<ChartOfAccount>
{
    public void Configure(EntityTypeBuilder<ChartOfAccount> builder)
    {
        builder.ToTable("ChartOfAccounts");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.AccountCode)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.AccountName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.AccountType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.AccountSubType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.FullPath)
            .HasMaxLength(500);

        builder.Property(e => e.NormalBalance)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(e => e.OpeningBalance)
            .HasPrecision(18, 2);

        builder.Property(e => e.CurrentBalance)
            .HasPrecision(18, 2);

        builder.Property(e => e.TaxCode)
            .HasMaxLength(20);

        builder.Property(e => e.CurrencyCode)
            .HasMaxLength(10)
            .HasDefaultValue("KES");

        builder.Property(e => e.BankAccountNumber)
            .HasMaxLength(50);

        builder.Property(e => e.BankName)
            .HasMaxLength(100);

        builder.Property(e => e.Notes)
            .HasMaxLength(1000);

        builder.HasOne(e => e.ParentAccount)
            .WithMany(e => e.SubAccounts)
            .HasForeignKey(e => e.ParentAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.AccountCode).IsUnique();
        builder.HasIndex(e => e.AccountType);
        builder.HasIndex(e => e.ParentAccountId);
        builder.HasIndex(e => e.IsActive);
        builder.HasIndex(e => new { e.AccountType, e.IsActive });
    }
}

/// <summary>
/// EF Core configuration for AccountingPeriod entity.
/// </summary>
public class AccountingPeriodConfiguration : IEntityTypeConfiguration<AccountingPeriod>
{
    public void Configure(EntityTypeBuilder<AccountingPeriod> builder)
    {
        builder.ToTable("AccountingPeriods");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.PeriodName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.PeriodCode)
            .HasMaxLength(20);

        builder.Property(e => e.PeriodType)
            .HasMaxLength(20)
            .HasDefaultValue("Monthly");

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(AccountingPeriodStatus.Open);

        builder.Property(e => e.TotalRevenue)
            .HasPrecision(18, 2);

        builder.Property(e => e.TotalExpenses)
            .HasPrecision(18, 2);

        builder.Property(e => e.NetIncome)
            .HasPrecision(18, 2);

        builder.Property(e => e.Notes)
            .HasMaxLength(1000);

        builder.HasOne(e => e.ClosedByUser)
            .WithMany()
            .HasForeignKey(e => e.ClosedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.LockedByUser)
            .WithMany()
            .HasForeignKey(e => e.LockedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.YearEndClosingEntry)
            .WithMany()
            .HasForeignKey(e => e.YearEndClosingEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.OpeningBalanceEntry)
            .WithMany()
            .HasForeignKey(e => e.OpeningBalanceEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.StartDate, e.EndDate });
        builder.HasIndex(e => e.FiscalYear);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.PeriodCode);
    }
}

/// <summary>
/// EF Core configuration for JournalEntry entity.
/// </summary>
public class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.ToTable("JournalEntries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EntryNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.EntryDate)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.ReferenceType)
            .HasMaxLength(50);

        builder.Property(e => e.SourceType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(JournalEntryStatus.Posted);

        builder.Property(e => e.TotalDebits)
            .HasPrecision(18, 2);

        builder.Property(e => e.TotalCredits)
            .HasPrecision(18, 2);

        builder.Property(e => e.CurrencyCode)
            .HasMaxLength(10)
            .HasDefaultValue("KES");

        builder.Property(e => e.ExchangeRate)
            .HasPrecision(18, 6)
            .HasDefaultValue(1);

        builder.Property(e => e.Notes)
            .HasMaxLength(1000);

        builder.Property(e => e.Attachments)
            .HasMaxLength(2000);

        builder.HasIndex(e => e.EntryNumber).IsUnique();
        builder.HasIndex(e => e.EntryDate);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.AccountingPeriodId);
        builder.HasIndex(e => new { e.ReferenceType, e.ReferenceId });
        builder.HasIndex(e => e.SourceType);

        builder.HasOne(e => e.AccountingPeriod)
            .WithMany(ap => ap.JournalEntries)
            .HasForeignKey(e => e.AccountingPeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.PostedByUser)
            .WithMany()
            .HasForeignKey(e => e.PostedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ApprovedByUser)
            .WithMany()
            .HasForeignKey(e => e.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ReversesEntry)
            .WithOne(e => e.ReversedByEntry)
            .HasForeignKey<JournalEntry>(e => e.ReversesEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// EF Core configuration for JournalEntryLine entity.
/// </summary>
public class JournalEntryLineConfiguration : IEntityTypeConfiguration<JournalEntryLine>
{
    public void Configure(EntityTypeBuilder<JournalEntryLine> builder)
    {
        builder.ToTable("JournalEntryLines");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.DebitAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(e => e.CreditAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(e => e.LineReference)
            .HasMaxLength(100);

        builder.Property(e => e.TaxCode)
            .HasMaxLength(20);

        builder.Property(e => e.TaxAmount)
            .HasPrecision(18, 2);

        builder.Property(e => e.CostCenter)
            .HasMaxLength(50);

        builder.Property(e => e.ProjectCode)
            .HasMaxLength(50);

        builder.HasOne(e => e.JournalEntry)
            .WithMany(je => je.JournalEntryLines)
            .HasForeignKey(e => e.JournalEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Account)
            .WithMany(a => a.JournalEntryLines)
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.JournalEntryId);
        builder.HasIndex(e => e.AccountId);
        builder.HasIndex(e => e.IsReconciled);
        builder.HasIndex(e => e.BankReconciliationId);
    }
}

/// <summary>
/// EF Core configuration for GLAccountMapping entity.
/// </summary>
public class GLAccountMappingConfiguration : IEntityTypeConfiguration<GLAccountMapping>
{
    public void Configure(EntityTypeBuilder<GLAccountMapping> builder)
    {
        builder.ToTable("GLAccountMappings");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.SourceType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.PaymentMethod)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasOne(e => e.DebitAccount)
            .WithMany()
            .HasForeignKey(e => e.DebitAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CreditAccount)
            .WithMany()
            .HasForeignKey(e => e.CreditAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Category)
            .WithMany()
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.SourceType);
        builder.HasIndex(e => new { e.SourceType, e.CategoryId, e.PaymentMethod, e.StoreId });
        builder.HasIndex(e => e.IsActive);
    }
}

/// <summary>
/// EF Core configuration for BankReconciliation entity.
/// </summary>
public class BankReconciliationConfiguration : IEntityTypeConfiguration<BankReconciliation>
{
    public void Configure(EntityTypeBuilder<BankReconciliation> builder)
    {
        builder.ToTable("BankReconciliations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ReconciliationNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.StatementEndingBalance)
            .HasPrecision(18, 2);

        builder.Property(e => e.BeginningBookBalance)
            .HasPrecision(18, 2);

        builder.Property(e => e.EndingBookBalance)
            .HasPrecision(18, 2);

        builder.Property(e => e.ClearedDeposits)
            .HasPrecision(18, 2);

        builder.Property(e => e.ClearedWithdrawals)
            .HasPrecision(18, 2);

        builder.Property(e => e.Difference)
            .HasPrecision(18, 2);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.Notes)
            .HasMaxLength(1000);

        builder.HasOne(e => e.BankAccount)
            .WithMany()
            .HasForeignKey(e => e.BankAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CompletedByUser)
            .WithMany()
            .HasForeignKey(e => e.CompletedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.ReconciliationNumber).IsUnique();
        builder.HasIndex(e => e.BankAccountId);
        builder.HasIndex(e => e.StatementDate);
        builder.HasIndex(e => e.Status);
    }
}

/// <summary>
/// EF Core configuration for BankReconciliationItem entity.
/// </summary>
public class BankReconciliationItemConfiguration : IEntityTypeConfiguration<BankReconciliationItem>
{
    public void Configure(EntityTypeBuilder<BankReconciliationItem> builder)
    {
        builder.ToTable("BankReconciliationItems");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TransactionType)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(e => e.CheckNumber)
            .HasMaxLength(50);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.Amount)
            .HasPrecision(18, 2);

        builder.Property(e => e.BankReference)
            .HasMaxLength(100);

        builder.HasOne(e => e.BankReconciliation)
            .WithMany(r => r.Items)
            .HasForeignKey(e => e.BankReconciliationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.JournalEntryLine)
            .WithMany()
            .HasForeignKey(e => e.JournalEntryLineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.BankReconciliationId);
        builder.HasIndex(e => e.JournalEntryLineId);
        builder.HasIndex(e => e.IsCleared);
    }
}

/// <summary>
/// EF Core configuration for PeriodClose entity.
/// </summary>
public class PeriodCloseConfiguration : IEntityTypeConfiguration<PeriodClose>
{
    public void Configure(EntityTypeBuilder<PeriodClose> builder)
    {
        builder.ToTable("PeriodCloses");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.TotalRevenue)
            .HasPrecision(18, 2);

        builder.Property(e => e.TotalExpenses)
            .HasPrecision(18, 2);

        builder.Property(e => e.NetIncome)
            .HasPrecision(18, 2);

        builder.Property(e => e.ChecklistJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.Notes)
            .HasMaxLength(1000);

        builder.Property(e => e.ReopenReason)
            .HasMaxLength(500);

        builder.HasOne(e => e.AccountingPeriod)
            .WithMany(p => p.PeriodCloses)
            .HasForeignKey(e => e.AccountingPeriodId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.InitiatedByUser)
            .WithMany()
            .HasForeignKey(e => e.InitiatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CompletedByUser)
            .WithMany()
            .HasForeignKey(e => e.CompletedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ReopenedByUser)
            .WithMany()
            .HasForeignKey(e => e.ReopenedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.RevenueCloseEntry)
            .WithMany()
            .HasForeignKey(e => e.RevenueCloseEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ExpenseCloseEntry)
            .WithMany()
            .HasForeignKey(e => e.ExpenseCloseEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.IncomeSummaryEntry)
            .WithMany()
            .HasForeignKey(e => e.IncomeSummaryEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.AccountingPeriodId);
        builder.HasIndex(e => e.Status);
    }
}

/// <summary>
/// EF Core configuration for FinancialStatement entity.
/// </summary>
public class FinancialStatementConfiguration : IEntityTypeConfiguration<FinancialStatement>
{
    public void Configure(EntityTypeBuilder<FinancialStatement> builder)
    {
        builder.ToTable("FinancialStatements");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.StatementType)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(e => e.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.DataJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.Notes)
            .HasMaxLength(1000);

        builder.HasOne(e => e.AccountingPeriod)
            .WithMany(p => p.FinancialStatements)
            .HasForeignKey(e => e.AccountingPeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.GeneratedByUser)
            .WithMany()
            .HasForeignKey(e => e.GeneratedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.StatementType);
        builder.HasIndex(e => new { e.PeriodStart, e.PeriodEnd });
        builder.HasIndex(e => e.GeneratedAt);
    }
}

/// <summary>
/// EF Core configuration for AccountBalance entity.
/// </summary>
public class AccountBalanceConfiguration : IEntityTypeConfiguration<AccountBalance>
{
    public void Configure(EntityTypeBuilder<AccountBalance> builder)
    {
        builder.ToTable("AccountBalances");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.OpeningBalance)
            .HasPrecision(18, 2);

        builder.Property(e => e.TotalDebits)
            .HasPrecision(18, 2);

        builder.Property(e => e.TotalCredits)
            .HasPrecision(18, 2);

        builder.Property(e => e.ClosingBalance)
            .HasPrecision(18, 2);

        builder.HasOne(e => e.Account)
            .WithMany(a => a.AccountBalances)
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.AccountId, e.Year, e.Month }).IsUnique();
        builder.HasIndex(e => new { e.Year, e.Month });
    }
}

/// <summary>
/// EF Core configuration for AccountBudget entity.
/// </summary>
public class AccountBudgetConfiguration : IEntityTypeConfiguration<AccountBudget>
{
    public void Configure(EntityTypeBuilder<AccountBudget> builder)
    {
        builder.ToTable("AccountBudgets");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.MonthlyAmountsJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.AnnualBudget)
            .HasPrecision(18, 2);

        builder.Property(e => e.Notes)
            .HasMaxLength(1000);

        builder.HasOne(e => e.Account)
            .WithMany(a => a.Budgets)
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.ApprovedByUser)
            .WithMany()
            .HasForeignKey(e => e.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.AccountId, e.FiscalYear }).IsUnique();
        builder.HasIndex(e => e.FiscalYear);
        builder.HasIndex(e => e.IsApproved);
    }
}

/// <summary>
/// EF Core configuration for AccountingAuditLog entity.
/// </summary>
public class AccountingAuditLogConfiguration : IEntityTypeConfiguration<AccountingAuditLog>
{
    public void Configure(EntityTypeBuilder<AccountingAuditLog> builder)
    {
        builder.ToTable("AccountingAuditLogs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Action)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.EntityType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.OldValue)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.NewValue)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.IpAddress)
            .HasMaxLength(50);

        builder.Property(e => e.Context)
            .HasMaxLength(500);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.EntityType, e.EntityId });
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.CreatedAt);
    }
}
