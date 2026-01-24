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

        builder.Property(e => e.CountType)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(e => e.LocationFilter)
            .HasMaxLength(200);

        builder.Property(e => e.ABCClassFilter)
            .HasMaxLength(10);

        builder.Property(e => e.SpotCountProductIds)
            .HasMaxLength(2000);

        builder.Property(e => e.Notes)
            .HasMaxLength(2000);

        builder.Property(e => e.ApprovalNotes)
            .HasMaxLength(1000);

        builder.Property(e => e.RejectionReason)
            .HasMaxLength(1000);

        // Summary statistics
        builder.Property(e => e.TotalSystemValue)
            .HasPrecision(18, 2);

        builder.Property(e => e.TotalCountedValue)
            .HasPrecision(18, 2);

        builder.Property(e => e.TotalVarianceValue)
            .HasPrecision(18, 2);

        builder.Property(e => e.ShrinkagePercentage)
            .HasPrecision(10, 4);

        builder.Property(e => e.VarianceThresholdPercent)
            .HasPrecision(10, 4);

        builder.Property(e => e.VarianceThresholdValue)
            .HasPrecision(18, 2);

        // Indexes
        builder.HasIndex(e => e.StockTakeNumber)
            .IsUnique();

        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.CountDate);
        builder.HasIndex(e => new { e.StoreId, e.Status });
        builder.HasIndex(e => new { e.StoreId, e.CountDate });

        // User relationships
        builder.HasOne(e => e.StartedByUser)
            .WithMany()
            .HasForeignKey(e => e.StartedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ApprovedByUser)
            .WithMany()
            .HasForeignKey(e => e.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.PostedByUser)
            .WithMany()
            .HasForeignKey(e => e.PostedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Store and Category
        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Category)
            .WithMany()
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Journal entry
        builder.HasOne(e => e.JournalEntry)
            .WithMany()
            .HasForeignKey(e => e.JournalEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Items collection
        builder.HasMany(e => e.Items)
            .WithOne(i => i.StockTake)
            .HasForeignKey(i => i.StockTakeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Counters collection
        builder.HasMany(e => e.Counters)
            .WithOne(c => c.StockTake)
            .HasForeignKey(c => c.StockTakeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore computed properties
        builder.Ignore(e => e.ProgressPercentage);
        builder.Ignore(e => e.CanModify);
        builder.Ignore(e => e.IsCountingComplete);
        builder.Ignore(e => e.StartDate);
        builder.Ignore(e => e.TotalItems);
        builder.Ignore(e => e.CountedItems);
    }
}

public class StockTakeItemConfiguration : IEntityTypeConfiguration<StockTakeItem>
{
    public void Configure(EntityTypeBuilder<StockTakeItem> builder)
    {
        builder.ToTable("StockTakeItems");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ProductSku)
            .HasMaxLength(100);

        builder.Property(e => e.ProductName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.ProductCode)
            .HasMaxLength(50);

        builder.Property(e => e.Location)
            .HasMaxLength(100);

        builder.Property(e => e.UnitOfMeasure)
            .HasMaxLength(20);

        // System values
        builder.Property(e => e.SystemQuantity)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(e => e.SystemCostPrice)
            .HasPrecision(18, 4);

        builder.Property(e => e.SystemValue)
            .HasPrecision(18, 2);

        // Primary count
        builder.Property(e => e.CountedQuantity)
            .HasPrecision(18, 3);

        builder.Property(e => e.CountedValue)
            .HasPrecision(18, 2);

        // Second count (double-blind)
        builder.Property(e => e.SecondCountQuantity)
            .HasPrecision(18, 3);

        builder.Property(e => e.ResolvedQuantity)
            .HasPrecision(18, 3);

        // Variance
        builder.Property(e => e.VarianceQuantity)
            .HasPrecision(18, 3);

        builder.Property(e => e.VarianceValue)
            .HasPrecision(18, 2);

        builder.Property(e => e.VariancePercentage)
            .HasPrecision(10, 4);

        builder.Property(e => e.VarianceCause)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(e => e.VarianceNotes)
            .HasMaxLength(1000);

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        // Indexes
        builder.HasIndex(e => new { e.StockTakeId, e.ProductId })
            .IsUnique();

        builder.HasIndex(e => e.ProductId);
        builder.HasIndex(e => e.ExceedsThreshold);
        builder.HasIndex(e => new { e.StockTakeId, e.IsCounted });
        builder.HasIndex(e => new { e.StockTakeId, e.ExceedsThreshold });

        // Relationships
        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CountedByUser)
            .WithMany()
            .HasForeignKey(e => e.CountedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.SecondCountedByUser)
            .WithMany()
            .HasForeignKey(e => e.SecondCountedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ResolvedByUser)
            .WithMany()
            .HasForeignKey(e => e.ResolvedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.StockMovement)
            .WithMany()
            .HasForeignKey(e => e.StockMovementId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ignore computed properties
        builder.Ignore(e => e.HasVariance);
        builder.Ignore(e => e.FinalCountQuantity);
        builder.Ignore(e => e.RequiresResolution);
        builder.Ignore(e => e.HasSecondCount);
        builder.Ignore(e => e.PhysicalQuantity);
        builder.Ignore(e => e.CostPrice);
        builder.Ignore(e => e.Variance);
    }
}

public class StockCountCounterConfiguration : IEntityTypeConfiguration<StockCountCounter>
{
    public void Configure(EntityTypeBuilder<StockCountCounter> builder)
    {
        builder.ToTable("StockCountCounters");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        builder.HasIndex(e => new { e.StockTakeId, e.UserId })
            .IsUnique();

        builder.HasIndex(e => e.UserId);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(e => e.IsComplete);
    }
}

public class StockCountScheduleConfiguration : IEntityTypeConfiguration<StockCountSchedule>
{
    public void Configure(EntityTypeBuilder<StockCountSchedule> builder)
    {
        builder.ToTable("StockCountSchedules");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.CountType)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(e => e.Frequency)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.LocationFilter)
            .HasMaxLength(200);

        builder.Property(e => e.DefaultAssigneeIds)
            .HasMaxLength(500);

        builder.Property(e => e.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(e => new { e.StoreId, e.IsEnabled });
        builder.HasIndex(e => e.NextRunDate);

        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Category)
            .WithMany()
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(e => e.IsDue);
    }
}

public class VarianceThresholdConfiguration : IEntityTypeConfiguration<VarianceThreshold>
{
    public void Configure(EntityTypeBuilder<VarianceThreshold> builder)
    {
        builder.ToTable("VarianceThresholds");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.QuantityThreshold)
            .HasPrecision(18, 3);

        builder.Property(e => e.PercentageThreshold)
            .HasPrecision(10, 4);

        builder.Property(e => e.ValueThreshold)
            .HasPrecision(18, 2);

        builder.Property(e => e.AlertRecipients)
            .HasMaxLength(500);

        builder.HasIndex(e => new { e.StoreId, e.IsActive });
        builder.HasIndex(e => new { e.StoreId, e.CategoryId, e.Priority });

        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Category)
            .WithMany()
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
