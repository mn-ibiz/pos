using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EmployeeNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.NationalId)
            .HasMaxLength(50);

        builder.Property(e => e.Phone)
            .HasMaxLength(50);

        builder.Property(e => e.Email)
            .HasMaxLength(100);

        builder.Property(e => e.Address)
            .HasMaxLength(500);

        builder.Property(e => e.Department)
            .HasMaxLength(50);

        builder.Property(e => e.Position)
            .HasMaxLength(100);

        builder.Property(e => e.EmploymentType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(EmploymentType.FullTime);

        builder.Property(e => e.BasicSalary)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(e => e.PayFrequency)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(PayFrequency.Monthly);

        builder.Property(e => e.BankName)
            .HasMaxLength(100);

        builder.Property(e => e.BankAccountNumber)
            .HasMaxLength(50);

        builder.Property(e => e.TaxId)
            .HasMaxLength(50);

        builder.Property(e => e.NssfNumber)
            .HasMaxLength(50);

        builder.Property(e => e.NhifNumber)
            .HasMaxLength(50);

        builder.HasIndex(e => e.EmployeeNumber)
            .IsUnique();

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Ignore computed property
        builder.Ignore(e => e.FullName);
    }
}

public class SalaryComponentConfiguration : IEntityTypeConfiguration<SalaryComponent>
{
    public void Configure(EntityTypeBuilder<SalaryComponent> builder)
    {
        builder.ToTable("SalaryComponents");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.ComponentType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.DefaultAmount)
            .HasPrecision(18, 2);

        builder.Property(e => e.DefaultPercent)
            .HasPrecision(5, 2);
    }
}

public class EmployeeSalaryComponentConfiguration : IEntityTypeConfiguration<EmployeeSalaryComponent>
{
    public void Configure(EntityTypeBuilder<EmployeeSalaryComponent> builder)
    {
        builder.ToTable("EmployeeSalaryComponents");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Amount)
            .HasPrecision(18, 2);

        builder.Property(e => e.Percent)
            .HasPrecision(5, 2);

        builder.Property(e => e.EffectiveFrom)
            .IsRequired();

        builder.HasIndex(e => new { e.EmployeeId, e.SalaryComponentId, e.EffectiveFrom })
            .IsUnique();

        builder.HasOne(e => e.Employee)
            .WithMany(emp => emp.EmployeeSalaryComponents)
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.SalaryComponent)
            .WithMany(sc => sc.EmployeeSalaryComponents)
            .HasForeignKey(e => e.SalaryComponentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PayrollPeriodConfiguration : IEntityTypeConfiguration<PayrollPeriod>
{
    public void Configure(EntityTypeBuilder<PayrollPeriod> builder)
    {
        builder.ToTable("PayrollPeriods");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.PeriodName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(PayrollStatus.Draft);

        builder.HasOne(e => e.ProcessedByUser)
            .WithMany()
            .HasForeignKey(e => e.ProcessedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ApprovedByUser)
            .WithMany()
            .HasForeignKey(e => e.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PayslipConfiguration : IEntityTypeConfiguration<Payslip>
{
    public void Configure(EntityTypeBuilder<Payslip> builder)
    {
        builder.ToTable("Payslips");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.BasicSalary)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.TotalEarnings)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.TotalDeductions)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.NetPay)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.PaymentStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(PayslipPaymentStatus.Pending);

        builder.Property(e => e.PaymentMethod)
            .HasMaxLength(50);

        builder.Property(e => e.PaymentReference)
            .HasMaxLength(100);

        builder.HasIndex(e => new { e.PayrollPeriodId, e.EmployeeId })
            .IsUnique();

        builder.HasOne(e => e.PayrollPeriod)
            .WithMany(pp => pp.Payslips)
            .HasForeignKey(e => e.PayrollPeriodId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Employee)
            .WithMany(emp => emp.Payslips)
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PayslipDetailConfiguration : IEntityTypeConfiguration<PayslipDetail>
{
    public void Configure(EntityTypeBuilder<PayslipDetail> builder)
    {
        builder.ToTable("PayslipDetails");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ComponentType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        // Index for payslip detail queries
        builder.HasIndex(e => e.PayslipId);

        builder.HasOne(e => e.Payslip)
            .WithMany(ps => ps.PayslipDetails)
            .HasForeignKey(e => e.PayslipId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.SalaryComponent)
            .WithMany(sc => sc.PayslipDetails)
            .HasForeignKey(e => e.SalaryComponentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AttendanceConfiguration : IEntityTypeConfiguration<Attendance>
{
    public void Configure(EntityTypeBuilder<Attendance> builder)
    {
        builder.ToTable("Attendances");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.AttendanceDate)
            .IsRequired();

        builder.Property(e => e.HoursWorked)
            .HasPrecision(5, 2)
            .HasDefaultValue(0);

        builder.Property(e => e.OvertimeHours)
            .HasPrecision(5, 2)
            .HasDefaultValue(0);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(AttendanceStatus.Present);

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        // Unique attendance per employee per day
        builder.HasIndex(e => new { e.EmployeeId, e.AttendanceDate })
            .IsUnique();

        builder.HasOne(e => e.Employee)
            .WithMany()
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.ApprovedByUser)
            .WithMany()
            .HasForeignKey(e => e.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
