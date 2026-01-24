using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework Core configuration for the WorkPeriodSession entity.
/// </summary>
public class WorkPeriodSessionConfiguration : IEntityTypeConfiguration<WorkPeriodSession>
{
    public void Configure(EntityTypeBuilder<WorkPeriodSession> builder)
    {
        builder.ToTable("WorkPeriodSessions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.WorkPeriodId)
            .IsRequired();

        builder.Property(e => e.TerminalId)
            .IsRequired();

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.LoginAt)
            .IsRequired();

        builder.Property(e => e.SalesTotal)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(e => e.TransactionCount)
            .HasDefaultValue(0);

        builder.Property(e => e.CashReceived)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(e => e.CashPaidOut)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(e => e.RefundTotal)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(e => e.VoidTotal)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(e => e.DiscountTotal)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(e => e.CardTotal)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(e => e.MpesaTotal)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        // Index for work period queries
        builder.HasIndex(e => e.WorkPeriodId)
            .HasDatabaseName("IX_WorkPeriodSessions_WorkPeriodId");

        // Index for terminal queries
        builder.HasIndex(e => e.TerminalId)
            .HasDatabaseName("IX_WorkPeriodSessions_TerminalId");

        // Index for user queries
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_WorkPeriodSessions_UserId");

        // Index for active session lookup
        builder.HasIndex(e => new { e.TerminalId, e.UserId, e.LogoutAt })
            .HasDatabaseName("IX_WorkPeriodSessions_Active");

        // Index for date queries
        builder.HasIndex(e => new { e.TerminalId, e.LoginAt })
            .HasDatabaseName("IX_WorkPeriodSessions_TerminalId_LoginAt");

        // Relationships
        builder.HasOne(e => e.WorkPeriod)
            .WithMany(wp => wp.Sessions)
            .HasForeignKey(e => e.WorkPeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Terminal)
            .WithMany()
            .HasForeignKey(e => e.TerminalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
