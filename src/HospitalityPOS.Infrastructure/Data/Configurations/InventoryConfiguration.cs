using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

public class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        builder.ToTable("Inventory");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.CurrentStock)
            .HasPrecision(18, 3)
            .HasDefaultValue(0);

        builder.Property(e => e.ReservedStock)
            .HasPrecision(18, 3)
            .HasDefaultValue(0);

        builder.HasIndex(e => e.ProductId)
            .IsUnique();

        builder.HasOne(e => e.Product)
            .WithOne(p => p.Inventory)
            .HasForeignKey<Inventory>(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.MovementType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.Quantity)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(e => e.PreviousStock)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(e => e.NewStock)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(e => e.ReferenceType)
            .HasMaxLength(50);

        builder.Property(e => e.Reason)
            .HasMaxLength(200);

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        builder.HasIndex(e => e.ProductId);

        builder.HasOne(e => e.Product)
            .WithMany(p => p.StockMovements)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.User)
            .WithMany(u => u.StockMovements)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.AdjustmentReason)
            .WithMany(ar => ar.StockMovements)
            .HasForeignKey(e => e.AdjustmentReasonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AdjustmentReasonConfiguration : IEntityTypeConfiguration<AdjustmentReason>
{
    public void Configure(EntityTypeBuilder<AdjustmentReason> builder)
    {
        builder.ToTable("AdjustmentReasons");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Code)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(e => e.RequiresNote)
            .HasDefaultValue(false);

        builder.Property(e => e.IsIncrease)
            .HasDefaultValue(false);

        builder.Property(e => e.IsDecrease)
            .HasDefaultValue(true);

        builder.Property(e => e.DisplayOrder)
            .HasDefaultValue(0);

        builder.HasIndex(e => e.Name)
            .IsUnique();

        builder.HasIndex(e => e.Code)
            .IsUnique();

        builder.HasIndex(e => e.DisplayOrder);

        // Seed default adjustment reasons
        builder.HasData(
            new AdjustmentReason { Id = 1, Name = "Damaged/Broken", Code = "DMG", IsDecrease = true, IsIncrease = false, DisplayOrder = 1, IsActive = true },
            new AdjustmentReason { Id = 2, Name = "Expired", Code = "EXP", IsDecrease = true, IsIncrease = false, DisplayOrder = 2, IsActive = true },
            new AdjustmentReason { Id = 3, Name = "Wastage", Code = "WST", IsDecrease = true, IsIncrease = false, DisplayOrder = 3, IsActive = true },
            new AdjustmentReason { Id = 4, Name = "Theft/Missing", Code = "THF", IsDecrease = true, IsIncrease = false, DisplayOrder = 4, IsActive = true },
            new AdjustmentReason { Id = 5, Name = "Found/Recovered", Code = "FND", IsDecrease = false, IsIncrease = true, DisplayOrder = 5, IsActive = true },
            new AdjustmentReason { Id = 6, Name = "Correction", Code = "COR", IsDecrease = true, IsIncrease = true, DisplayOrder = 6, IsActive = true },
            new AdjustmentReason { Id = 7, Name = "Transfer In", Code = "TRI", IsDecrease = false, IsIncrease = true, DisplayOrder = 7, IsActive = true },
            new AdjustmentReason { Id = 8, Name = "Transfer Out", Code = "TRO", IsDecrease = true, IsIncrease = false, DisplayOrder = 8, IsActive = true },
            new AdjustmentReason { Id = 9, Name = "Other", Code = "OTH", IsDecrease = true, IsIncrease = true, RequiresNote = true, DisplayOrder = 99, IsActive = true }
        );
    }
}

public class StockTakeConfiguration : IEntityTypeConfiguration<StockTake>
{
    public void Configure(EntityTypeBuilder<StockTake> builder)
    {
        builder.ToTable("StockTakes");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.StockTakeNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        builder.Property(e => e.TotalVarianceValue)
            .HasPrecision(18, 2);

        builder.HasIndex(e => e.StockTakeNumber)
            .IsUnique();

        builder.HasIndex(e => e.Status);

        builder.HasOne(e => e.StartedByUser)
            .WithMany()
            .HasForeignKey(e => e.StartedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ApprovedByUser)
            .WithMany()
            .HasForeignKey(e => e.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Items)
            .WithOne(i => i.StockTake)
            .HasForeignKey(i => i.StockTakeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore computed property
        builder.Ignore(e => e.ProgressPercentage);
    }
}

public class StockTakeItemConfiguration : IEntityTypeConfiguration<StockTakeItem>
{
    public void Configure(EntityTypeBuilder<StockTakeItem> builder)
    {
        builder.ToTable("StockTakeItems");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ProductName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.ProductCode)
            .HasMaxLength(50);

        builder.Property(e => e.SystemQuantity)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(e => e.PhysicalQuantity)
            .HasPrecision(18, 3);

        builder.Property(e => e.VarianceQuantity)
            .HasPrecision(18, 3);

        builder.Property(e => e.CostPrice)
            .HasPrecision(18, 2);

        builder.Property(e => e.VarianceValue)
            .HasPrecision(18, 2);

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        builder.HasIndex(e => new { e.StockTakeId, e.ProductId })
            .IsUnique();

        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CountedByUser)
            .WithMany()
            .HasForeignKey(e => e.CountedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ignore computed property
        builder.Ignore(e => e.HasVariance);
    }
}
