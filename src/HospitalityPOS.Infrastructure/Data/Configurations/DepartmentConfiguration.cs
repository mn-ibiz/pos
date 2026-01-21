// src/HospitalityPOS.Infrastructure/Data/Configurations/DepartmentConfiguration.cs
// EF Core configuration for Department entity.

using HospitalityPOS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// Configuration for Department entity.
/// </summary>
public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Code)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.CostCenter)
            .HasMaxLength(50);

        builder.Property(e => e.Location)
            .HasMaxLength(100);

        builder.Property(e => e.Phone)
            .HasMaxLength(20);

        builder.Property(e => e.Email)
            .HasMaxLength(100);

        builder.HasIndex(e => e.Code).IsUnique();
        builder.HasIndex(e => e.Name);

        // Self-referencing relationship for hierarchy
        builder.HasOne(e => e.ParentDepartment)
            .WithMany(e => e.SubDepartments)
            .HasForeignKey(e => e.ParentDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Manager relationship (avoid circular reference issues)
        builder.HasOne(e => e.Manager)
            .WithMany()
            .HasForeignKey(e => e.ManagerEmployeeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
