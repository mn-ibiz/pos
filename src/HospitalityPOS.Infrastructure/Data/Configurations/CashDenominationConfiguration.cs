using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for CashDenomination entity.
/// </summary>
public class CashDenominationConfiguration : IEntityTypeConfiguration<CashDenomination>
{
    public void Configure(EntityTypeBuilder<CashDenomination> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.CurrencyCode)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(d => d.Value)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(d => d.DisplayName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(d => d.Type)
            .IsRequired();

        builder.Property(d => d.SortOrder)
            .IsRequired();

        builder.Property(d => d.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Seed default Kenya Shilling denominations
        builder.HasData(
            // Notes
            new CashDenomination { Id = 1, CurrencyCode = "KES", Type = DenominationType.Note, Value = 1000, DisplayName = "KES 1,000", SortOrder = 1, IsActive = true },
            new CashDenomination { Id = 2, CurrencyCode = "KES", Type = DenominationType.Note, Value = 500, DisplayName = "KES 500", SortOrder = 2, IsActive = true },
            new CashDenomination { Id = 3, CurrencyCode = "KES", Type = DenominationType.Note, Value = 200, DisplayName = "KES 200", SortOrder = 3, IsActive = true },
            new CashDenomination { Id = 4, CurrencyCode = "KES", Type = DenominationType.Note, Value = 100, DisplayName = "KES 100", SortOrder = 4, IsActive = true },
            new CashDenomination { Id = 5, CurrencyCode = "KES", Type = DenominationType.Note, Value = 50, DisplayName = "KES 50", SortOrder = 5, IsActive = true },
            // Coins
            new CashDenomination { Id = 6, CurrencyCode = "KES", Type = DenominationType.Coin, Value = 40, DisplayName = "KES 40", SortOrder = 6, IsActive = false }, // Commemorative, rare
            new CashDenomination { Id = 7, CurrencyCode = "KES", Type = DenominationType.Coin, Value = 20, DisplayName = "KES 20", SortOrder = 7, IsActive = true },
            new CashDenomination { Id = 8, CurrencyCode = "KES", Type = DenominationType.Coin, Value = 10, DisplayName = "KES 10", SortOrder = 8, IsActive = true },
            new CashDenomination { Id = 9, CurrencyCode = "KES", Type = DenominationType.Coin, Value = 5, DisplayName = "KES 5", SortOrder = 9, IsActive = true },
            new CashDenomination { Id = 10, CurrencyCode = "KES", Type = DenominationType.Coin, Value = 1, DisplayName = "KES 1", SortOrder = 10, IsActive = true },
            new CashDenomination { Id = 11, CurrencyCode = "KES", Type = DenominationType.Coin, Value = 0.50m, DisplayName = "50 Cents", SortOrder = 11, IsActive = false } // Rarely used
        );
    }
}

/// <summary>
/// EF Core configuration for CashDenominationCount entity.
/// </summary>
public class CashDenominationCountConfiguration : IEntityTypeConfiguration<CashDenominationCount>
{
    public void Configure(EntityTypeBuilder<CashDenominationCount> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.CountType)
            .IsRequired();

        builder.Property(c => c.CountedAt)
            .IsRequired();

        builder.Property(c => c.TotalNotes)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(c => c.TotalCoins)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(c => c.GrandTotal)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(c => c.Notes)
            .HasMaxLength(500);

        // Relationships
        builder.HasOne(c => c.WorkPeriod)
            .WithMany(wp => wp.CashDenominationCounts)
            .HasForeignKey(c => c.WorkPeriodId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.CountedByUser)
            .WithMany()
            .HasForeignKey(c => c.CountedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.VerifiedByUser)
            .WithMany()
            .HasForeignKey(c => c.VerifiedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Lines)
            .WithOne(l => l.CashDenominationCount)
            .HasForeignKey(l => l.CashDenominationCountId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(c => new { c.WorkPeriodId, c.CountType });
    }
}

/// <summary>
/// EF Core configuration for CashCountLine entity.
/// </summary>
public class CashCountLineConfiguration : IEntityTypeConfiguration<CashCountLine>
{
    public void Configure(EntityTypeBuilder<CashCountLine> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Quantity)
            .IsRequired();

        builder.Property(l => l.LineTotal)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        // Relationships
        builder.HasOne(l => l.CashDenominationCount)
            .WithMany(c => c.Lines)
            .HasForeignKey(l => l.CashDenominationCountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Denomination)
            .WithMany()
            .HasForeignKey(l => l.DenominationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
