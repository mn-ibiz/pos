using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

public class ChartOfAccountConfiguration : IEntityTypeConfiguration<ChartOfAccount>
{
    public void Configure(EntityTypeBuilder<ChartOfAccount> builder)
    {
        builder.ToTable("ChartOfAccounts");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.AccountCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.AccountName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.AccountType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(200);

        builder.HasIndex(e => e.AccountCode)
            .IsUnique();

        builder.HasOne(e => e.ParentAccount)
            .WithMany(a => a.SubAccounts)
            .HasForeignKey(e => e.ParentAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AccountingPeriodConfiguration : IEntityTypeConfiguration<AccountingPeriod>
{
    public void Configure(EntityTypeBuilder<AccountingPeriod> builder)
    {
        builder.ToTable("AccountingPeriods");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.PeriodName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(AccountingPeriodStatus.Open);

        builder.HasOne(e => e.ClosedByUser)
            .WithMany()
            .HasForeignKey(e => e.ClosedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.ToTable("JournalEntries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EntryNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.EntryDate)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.ReferenceType)
            .HasMaxLength(50);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(JournalEntryStatus.Posted);

        builder.HasIndex(e => e.EntryNumber)
            .IsUnique();

        builder.HasOne(e => e.AccountingPeriod)
            .WithMany(ap => ap.JournalEntries)
            .HasForeignKey(e => e.AccountingPeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        // Map CreatedByUser navigation to BaseEntity.CreatedByUserId
        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class JournalEntryLineConfiguration : IEntityTypeConfiguration<JournalEntryLine>
{
    public void Configure(EntityTypeBuilder<JournalEntryLine> builder)
    {
        builder.ToTable("JournalEntryLines");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Description)
            .HasMaxLength(200);

        builder.Property(e => e.DebitAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(e => e.CreditAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.HasOne(e => e.JournalEntry)
            .WithMany(je => je.JournalEntryLines)
            .HasForeignKey(e => e.JournalEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Account)
            .WithMany(a => a.JournalEntryLines)
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
