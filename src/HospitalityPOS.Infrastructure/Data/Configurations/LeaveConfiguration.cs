// src/HospitalityPOS.Infrastructure/Data/Configurations/LeaveConfiguration.cs
// EF Core configurations for leave management entities.

using HospitalityPOS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// Configuration for LeaveType entity.
/// </summary>
public class LeaveTypeConfiguration : IEntityTypeConfiguration<LeaveType>
{
    public void Configure(EntityTypeBuilder<LeaveType> builder)
    {
        builder.ToTable("LeaveTypes");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Code)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.DefaultDaysPerYear)
            .HasPrecision(5, 2);

        builder.Property(e => e.MaxCarryOverDays)
            .HasPrecision(5, 2);

        builder.Property(e => e.DisplayColor)
            .HasMaxLength(10);

        builder.HasIndex(e => e.Code).IsUnique();
        builder.HasIndex(e => e.Name).IsUnique();
    }
}

/// <summary>
/// Configuration for LeaveAllocation entity.
/// </summary>
public class LeaveAllocationConfiguration : IEntityTypeConfiguration<LeaveAllocation>
{
    public void Configure(EntityTypeBuilder<LeaveAllocation> builder)
    {
        builder.ToTable("LeaveAllocations");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.AllocatedDays)
            .HasPrecision(5, 2);

        builder.Property(e => e.UsedDays)
            .HasPrecision(5, 2);

        builder.Property(e => e.CarriedOverDays)
            .HasPrecision(5, 2);

        builder.Property(e => e.PendingDays)
            .HasPrecision(5, 2);

        // Unique constraint: one allocation per employee/leave type/year
        builder.HasIndex(e => new { e.EmployeeId, e.LeaveTypeId, e.Year }).IsUnique();

        builder.HasOne(e => e.Employee)
            .WithMany(emp => emp.LeaveAllocations)
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.LeaveType)
            .WithMany(lt => lt.Allocations)
            .HasForeignKey(e => e.LeaveTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// Configuration for LeaveRequest entity.
/// </summary>
public class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.ToTable("LeaveRequests");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.DaysRequested)
            .HasPrecision(5, 2);

        builder.Property(e => e.Reason)
            .HasMaxLength(500);

        builder.Property(e => e.ReviewNotes)
            .HasMaxLength(500);

        builder.Property(e => e.DocumentationPath)
            .HasMaxLength(500);

        builder.Property(e => e.ContactWhileOnLeave)
            .HasMaxLength(100);

        builder.Property(e => e.HandoverNotes)
            .HasMaxLength(1000);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(e => new { e.EmployeeId, e.StartDate });
        builder.HasIndex(e => e.Status);

        builder.HasOne(e => e.Employee)
            .WithMany(emp => emp.LeaveRequests)
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.LeaveType)
            .WithMany(lt => lt.Requests)
            .HasForeignKey(e => e.LeaveTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ReviewedByUser)
            .WithMany()
            .HasForeignKey(e => e.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// Configuration for LeaveBalanceAdjustment entity.
/// </summary>
public class LeaveBalanceAdjustmentConfiguration : IEntityTypeConfiguration<LeaveBalanceAdjustment>
{
    public void Configure(EntityTypeBuilder<LeaveBalanceAdjustment> builder)
    {
        builder.ToTable("LeaveBalanceAdjustments");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Days)
            .HasPrecision(5, 2);

        builder.Property(e => e.Reason)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.AdjustmentType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne(e => e.LeaveAllocation)
            .WithMany(la => la.Adjustments)
            .HasForeignKey(e => e.LeaveAllocationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.AdjustedByUser)
            .WithMany()
            .HasForeignKey(e => e.AdjustedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// Configuration for PublicHoliday entity.
/// </summary>
public class PublicHolidayConfiguration : IEntityTypeConfiguration<PublicHoliday>
{
    public void Configure(EntityTypeBuilder<PublicHoliday> builder)
    {
        builder.ToTable("PublicHolidays");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.HasIndex(e => e.Date);
        builder.HasIndex(e => new { e.RecurringMonth, e.RecurringDay });
    }
}
