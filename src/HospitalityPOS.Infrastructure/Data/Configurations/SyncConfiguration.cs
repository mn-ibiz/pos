using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for SyncConfiguration.
/// </summary>
public class SyncConfigurationEntityConfiguration : IEntityTypeConfiguration<SyncConfiguration>
{
    public void Configure(EntityTypeBuilder<SyncConfiguration> builder)
    {
        builder.ToTable("SyncConfigurations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.SyncIntervalSeconds)
            .HasDefaultValue(30);

        builder.Property(e => e.MaxBatchSize)
            .HasDefaultValue(100);

        builder.Property(e => e.RetryAttempts)
            .HasDefaultValue(3);

        builder.Property(e => e.RetryDelaySeconds)
            .HasDefaultValue(60);

        builder.Property(e => e.LastSyncError)
            .HasMaxLength(1000);

        // Foreign key to Store
        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        // Navigation to entity rules
        builder.HasMany(e => e.EntityRules)
            .WithOne(r => r.SyncConfiguration)
            .HasForeignKey(r => r.SyncConfigurationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique index on StoreId
        builder.HasIndex(e => e.StoreId)
            .IsUnique()
            .HasDatabaseName("IX_SyncConfigurations_StoreId");
    }
}

/// <summary>
/// Entity configuration for SyncEntityRule.
/// </summary>
public class SyncEntityRuleConfiguration : IEntityTypeConfiguration<SyncEntityRule>
{
    public void Configure(EntityTypeBuilder<SyncEntityRule> builder)
    {
        builder.ToTable("SyncEntityRules");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EntityType)
            .HasConversion<int>();

        builder.Property(e => e.Direction)
            .HasConversion<int>();

        builder.Property(e => e.ConflictResolution)
            .HasConversion<int>()
            .HasDefaultValue(ConflictWinner.HQ);

        builder.Property(e => e.Priority)
            .HasDefaultValue(100);

        // Foreign key to SyncConfiguration
        builder.HasOne(e => e.SyncConfiguration)
            .WithMany(c => c.EntityRules)
            .HasForeignKey(e => e.SyncConfigurationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Composite unique index
        builder.HasIndex(e => new { e.SyncConfigurationId, e.EntityType })
            .IsUnique()
            .HasDatabaseName("IX_SyncEntityRules_Config_Entity");
    }
}

/// <summary>
/// Entity configuration for SyncBatch.
/// </summary>
public class SyncBatchConfiguration : IEntityTypeConfiguration<SyncBatch>
{
    public void Configure(EntityTypeBuilder<SyncBatch> builder)
    {
        builder.ToTable("SyncBatches");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Direction)
            .HasConversion<int>();

        builder.Property(e => e.EntityType)
            .HasConversion<int>();

        builder.Property(e => e.Status)
            .HasConversion<int>()
            .HasDefaultValue(SyncBatchStatus.Pending);

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(2000);

        // Foreign key to Store
        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        // Navigation to records
        builder.HasMany(e => e.Records)
            .WithOne(r => r.SyncBatch)
            .HasForeignKey(r => r.SyncBatchId)
            .OnDelete(DeleteBehavior.Cascade);

        // Navigation to conflicts
        builder.HasMany(e => e.Conflicts)
            .WithOne(c => c.SyncBatch)
            .HasForeignKey(c => c.SyncBatchId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index on Status
        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_SyncBatches_Status");

        // Index on StoreId and CreatedAt
        builder.HasIndex(e => new { e.StoreId, e.CreatedAt })
            .HasDatabaseName("IX_SyncBatches_Store_Created");

        // Index on Direction
        builder.HasIndex(e => e.Direction)
            .HasDatabaseName("IX_SyncBatches_Direction");
    }
}

/// <summary>
/// Entity configuration for SyncRecord.
/// </summary>
public class SyncRecordConfiguration : IEntityTypeConfiguration<SyncRecord>
{
    public void Configure(EntityTypeBuilder<SyncRecord> builder)
    {
        builder.ToTable("SyncRecords");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EntityType)
            .HasConversion<int>();

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(1000);

        // Foreign key to SyncBatch
        builder.HasOne(e => e.SyncBatch)
            .WithMany(b => b.Records)
            .HasForeignKey(e => e.SyncBatchId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index on SyncBatchId
        builder.HasIndex(e => e.SyncBatchId)
            .HasDatabaseName("IX_SyncRecords_BatchId");

        // Index on EntityType and EntityId
        builder.HasIndex(e => new { e.EntityType, e.EntityId })
            .HasDatabaseName("IX_SyncRecords_Entity");
    }
}

/// <summary>
/// Entity configuration for SyncConflict.
/// </summary>
public class SyncConflictConfiguration : IEntityTypeConfiguration<SyncConflict>
{
    public void Configure(EntityTypeBuilder<SyncConflict> builder)
    {
        builder.ToTable("SyncConflicts");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EntityType)
            .HasConversion<int>();

        builder.Property(e => e.Resolution)
            .HasConversion<int?>();

        builder.Property(e => e.ResolutionNotes)
            .HasMaxLength(500);

        // Foreign key to SyncBatch
        builder.HasOne(e => e.SyncBatch)
            .WithMany(b => b.Conflicts)
            .HasForeignKey(e => e.SyncBatchId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index on IsResolved
        builder.HasIndex(e => e.IsResolved)
            .HasDatabaseName("IX_SyncConflicts_Resolved");

        // Index on SyncBatchId
        builder.HasIndex(e => e.SyncBatchId)
            .HasDatabaseName("IX_SyncConflicts_BatchId");

        // Index on EntityType and EntityId
        builder.HasIndex(e => new { e.EntityType, e.EntityId })
            .HasDatabaseName("IX_SyncConflicts_Entity");
    }
}

/// <summary>
/// Entity configuration for SyncLog.
/// </summary>
public class SyncLogConfiguration : IEntityTypeConfiguration<SyncLog>
{
    public void Configure(EntityTypeBuilder<SyncLog> builder)
    {
        builder.ToTable("SyncLogs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Operation)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Details)
            .HasMaxLength(2000);

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(2000);

        // Foreign key to Store - use Restrict to avoid cascade cycles
        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign key to SyncBatch (optional) - use NoAction to avoid cascade cycles
        builder.HasOne(e => e.SyncBatch)
            .WithMany()
            .HasForeignKey(e => e.SyncBatchId)
            .OnDelete(DeleteBehavior.NoAction);

        // Index on Timestamp
        builder.HasIndex(e => e.Timestamp)
            .HasDatabaseName("IX_SyncLogs_Timestamp");

        // Index on StoreId and Timestamp
        builder.HasIndex(e => new { e.StoreId, e.Timestamp })
            .HasDatabaseName("IX_SyncLogs_Store_Timestamp");

        // Index on IsSuccess
        builder.HasIndex(e => e.IsSuccess)
            .HasDatabaseName("IX_SyncLogs_Success");
    }
}
