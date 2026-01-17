using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework Core configuration for Floor entity.
/// </summary>
public class FloorConfiguration : IEntityTypeConfiguration<Floor>
{
    public void Configure(EntityTypeBuilder<Floor> builder)
    {
        builder.ToTable("Floors");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.GridWidth)
            .HasDefaultValue(10);

        builder.Property(e => e.GridHeight)
            .HasDefaultValue(10);

        builder.HasIndex(e => e.Name)
            .IsUnique()
            .HasFilter("[IsActive] = 1");

        builder.HasIndex(e => e.DisplayOrder);
    }
}

/// <summary>
/// Entity Framework Core configuration for Section entity.
/// </summary>
public class SectionConfiguration : IEntityTypeConfiguration<Section>
{
    public void Configure(EntityTypeBuilder<Section> builder)
    {
        builder.ToTable("Sections");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.ColorCode)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("#4CAF50");

        builder.HasIndex(e => new { e.FloorId, e.Name })
            .IsUnique()
            .HasFilter("[IsActive] = 1");

        builder.HasOne(e => e.Floor)
            .WithMany(f => f.Sections)
            .HasForeignKey(e => e.FloorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Entity Framework Core configuration for Table entity.
/// </summary>
public class TableConfiguration : IEntityTypeConfiguration<Table>
{
    public void Configure(EntityTypeBuilder<Table> builder)
    {
        builder.ToTable("Tables");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TableNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.Capacity)
            .HasDefaultValue(4);

        builder.Property(e => e.GridX)
            .HasDefaultValue(0);

        builder.Property(e => e.GridY)
            .HasDefaultValue(0);

        builder.Property(e => e.Width)
            .HasDefaultValue(1);

        builder.Property(e => e.Height)
            .HasDefaultValue(1);

        builder.Property(e => e.Shape)
            .HasDefaultValue(Core.Enums.TableShape.Square);

        builder.Property(e => e.Status)
            .HasDefaultValue(Core.Enums.TableStatus.Available);

        // Concurrency token for optimistic locking
        builder.Property(e => e.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        // Unique table number per floor
        builder.HasIndex(e => new { e.FloorId, e.TableNumber })
            .IsUnique()
            .HasFilter("[IsActive] = 1");

        builder.HasIndex(e => e.Status);

        builder.HasOne(e => e.Floor)
            .WithMany(f => f.Tables)
            .HasForeignKey(e => e.FloorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Section)
            .WithMany(s => s.Tables)
            .HasForeignKey(e => e.SectionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.CurrentReceipt)
            .WithMany()
            .HasForeignKey(e => e.CurrentReceiptId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.AssignedUser)
            .WithMany()
            .HasForeignKey(e => e.AssignedUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>
/// Entity Framework Core configuration for TableTransferLog entity.
/// </summary>
public class TableTransferLogConfiguration : IEntityTypeConfiguration<TableTransferLog>
{
    public void Configure(EntityTypeBuilder<TableTransferLog> builder)
    {
        builder.ToTable("TableTransferLogs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TableNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.FromUserName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.ToUserName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.ReceiptAmount)
            .HasPrecision(18, 2);

        builder.Property(e => e.Reason)
            .HasMaxLength(500);

        builder.Property(e => e.TransferredAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Indexes for common queries
        builder.HasIndex(e => e.TableId);
        builder.HasIndex(e => e.FromUserId);
        builder.HasIndex(e => e.ToUserId);
        builder.HasIndex(e => e.TransferredAt);

        // Navigation properties
        builder.HasOne(e => e.Table)
            .WithMany()
            .HasForeignKey(e => e.TableId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.FromUser)
            .WithMany()
            .HasForeignKey(e => e.FromUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.ToUser)
            .WithMany()
            .HasForeignKey(e => e.ToUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.Receipt)
            .WithMany()
            .HasForeignKey(e => e.ReceiptId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.TransferredByUser)
            .WithMany()
            .HasForeignKey(e => e.TransferredByUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
