using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for CourseDefinition.
/// </summary>
public class CourseDefinitionConfiguration : IEntityTypeConfiguration<CourseDefinition>
{
    public void Configure(EntityTypeBuilder<CourseDefinition> builder)
    {
        builder.ToTable("CourseDefinitions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.CourseNumber)
            .IsRequired();

        builder.Property(e => e.DefaultDelayMinutes)
            .HasDefaultValue(10);

        builder.Property(e => e.IconUrl)
            .HasMaxLength(500);

        builder.Property(e => e.Color)
            .HasMaxLength(20);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        // Unique index on StoreId + CourseNumber
        builder.HasIndex(e => new { e.StoreId, e.CourseNumber })
            .IsUnique()
            .HasDatabaseName("IX_CourseDefinitions_Store_CourseNumber");

        // Index on StoreId for store lookups
        builder.HasIndex(e => e.StoreId)
            .HasDatabaseName("IX_CourseDefinitions_StoreId");

        // Index on IsActive for filtering
        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_CourseDefinitions_IsActive");

        // Foreign key to Store
        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        // Navigation to course states
        builder.HasMany(e => e.CourseStates)
            .WithOne(cs => cs.CourseDefinition)
            .HasForeignKey(cs => cs.CourseDefinitionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>
/// Entity configuration for CourseConfiguration.
/// </summary>
public class CourseConfigurationEntityConfiguration : IEntityTypeConfiguration<CourseConfiguration>
{
    public void Configure(EntityTypeBuilder<CourseConfiguration> builder)
    {
        builder.ToTable("CourseConfigurations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EnableCoursing)
            .HasDefaultValue(false);

        builder.Property(e => e.FireMode)
            .HasConversion<int>()
            .HasDefaultValue(CourseFireMode.AutoOnBump);

        builder.Property(e => e.DefaultCoursePacingMinutes)
            .HasDefaultValue(10);

        builder.Property(e => e.AutoFireOnPreviousBump)
            .HasDefaultValue(true);

        builder.Property(e => e.ShowHeldCoursesOnPrepStation)
            .HasDefaultValue(false);

        builder.Property(e => e.RequireExpoConfirmation)
            .HasDefaultValue(false);

        builder.Property(e => e.AllowManualFireOverride)
            .HasDefaultValue(true);

        builder.Property(e => e.AllowRushMode)
            .HasDefaultValue(true);

        builder.Property(e => e.AutoFireFirstCourse)
            .HasDefaultValue(true);

        builder.Property(e => e.FireGracePeriodSeconds)
            .HasDefaultValue(30);

        builder.Property(e => e.ShowCountdownToNextCourse)
            .HasDefaultValue(true);

        builder.Property(e => e.AlertOnReadyToFire)
            .HasDefaultValue(true);

        builder.Property(e => e.FireAlertSound)
            .HasMaxLength(200);

        // Unique index on StoreId - one config per store
        builder.HasIndex(e => e.StoreId)
            .IsUnique()
            .HasDatabaseName("IX_CourseConfigurations_StoreId");

        // Index on IsActive for filtering
        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_CourseConfigurations_IsActive");

        // Foreign key to Store
        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// Entity configuration for KdsCourseState.
/// </summary>
public class KdsCourseStateConfiguration : IEntityTypeConfiguration<KdsCourseState>
{
    public void Configure(EntityTypeBuilder<KdsCourseState> builder)
    {
        builder.ToTable("KdsCourseStates");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.CourseNumber)
            .IsRequired();

        builder.Property(e => e.CourseName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Status)
            .HasConversion<int>()
            .HasDefaultValue(CourseStatus.Pending);

        builder.Property(e => e.HoldReason)
            .HasMaxLength(200);

        builder.Property(e => e.DisplayColor)
            .HasMaxLength(20);

        builder.Property(e => e.TargetMinutesAfterPrevious)
            .HasDefaultValue(10);

        builder.Property(e => e.TotalItems)
            .HasDefaultValue(0);

        builder.Property(e => e.CompletedItems)
            .HasDefaultValue(0);

        // Unique index on KdsOrderId + CourseNumber
        builder.HasIndex(e => new { e.KdsOrderId, e.CourseNumber })
            .IsUnique()
            .HasDatabaseName("IX_KdsCourseStates_Order_CourseNumber");

        // Index on Status for filtering
        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_KdsCourseStates_Status");

        // Index on ScheduledFireAt for scheduled fire processing
        builder.HasIndex(e => e.ScheduledFireAt)
            .HasDatabaseName("IX_KdsCourseStates_ScheduledFireAt");

        // Index on IsOnHold for held course queries
        builder.HasIndex(e => e.IsOnHold)
            .HasDatabaseName("IX_KdsCourseStates_IsOnHold");

        // Composite index for pending courses ready to fire
        builder.HasIndex(e => new { e.Status, e.ScheduledFireAt, e.IsOnHold })
            .HasDatabaseName("IX_KdsCourseStates_Status_Scheduled_Hold");

        // Foreign key to KdsOrder
        builder.HasOne(e => e.KdsOrder)
            .WithMany(o => o.CourseStates)
            .HasForeignKey(e => e.KdsOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign key to CourseDefinition
        builder.HasOne(e => e.CourseDefinition)
            .WithMany(cd => cd.CourseStates)
            .HasForeignKey(e => e.CourseDefinitionId)
            .OnDelete(DeleteBehavior.SetNull);

        // Foreign key to User who fired
        builder.HasOne(e => e.FiredByUser)
            .WithMany()
            .HasForeignKey(e => e.FiredByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Foreign key to User who served
        builder.HasOne(e => e.ServedByUser)
            .WithMany()
            .HasForeignKey(e => e.ServedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Foreign key to User who held
        builder.HasOne(e => e.HeldByUser)
            .WithMany()
            .HasForeignKey(e => e.HeldByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Navigation to items in this course
        builder.HasMany(e => e.Items)
            .WithOne(i => i.CourseState)
            .HasForeignKey(i => i.CourseStateId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>
/// Entity configuration for CourseFiringLog.
/// </summary>
public class CourseFiringLogConfiguration : IEntityTypeConfiguration<CourseFiringLog>
{
    public void Configure(EntityTypeBuilder<CourseFiringLog> builder)
    {
        builder.ToTable("CourseFiringLogs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.CourseNumber)
            .IsRequired();

        builder.Property(e => e.Action)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.PreviousStatus)
            .HasConversion<int?>();

        builder.Property(e => e.NewStatus)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.ActionAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        // Index on KdsOrderId for order log queries
        builder.HasIndex(e => e.KdsOrderId)
            .HasDatabaseName("IX_CourseFiringLogs_KdsOrderId");

        // Index on CourseStateId for course state logs
        builder.HasIndex(e => e.CourseStateId)
            .HasDatabaseName("IX_CourseFiringLogs_CourseStateId");

        // Index on ActionAt for time-based queries
        builder.HasIndex(e => e.ActionAt)
            .HasDatabaseName("IX_CourseFiringLogs_ActionAt");

        // Index on Action for filtering by action type
        builder.HasIndex(e => e.Action)
            .HasDatabaseName("IX_CourseFiringLogs_Action");

        // Foreign key to KdsOrder
        builder.HasOne(e => e.KdsOrder)
            .WithMany()
            .HasForeignKey(e => e.KdsOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign key to KdsCourseState
        builder.HasOne(e => e.CourseState)
            .WithMany()
            .HasForeignKey(e => e.CourseStateId)
            .OnDelete(DeleteBehavior.SetNull);

        // Foreign key to User
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Foreign key to KdsStation
        builder.HasOne(e => e.Station)
            .WithMany()
            .HasForeignKey(e => e.StationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
