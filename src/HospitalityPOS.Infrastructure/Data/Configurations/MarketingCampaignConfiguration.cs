using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for CampaignFlow.
/// </summary>
public class CampaignFlowConfiguration : IEntityTypeConfiguration<CampaignFlow>
{
    public void Configure(EntityTypeBuilder<CampaignFlow> builder)
    {
        builder.ToTable("CampaignFlows");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.Trigger)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.TriggerDaysOffset)
            .HasDefaultValue(0);

        builder.Property(e => e.MinimumTier)
            .HasConversion<int?>();

        builder.Property(e => e.IsEnabled)
            .HasDefaultValue(true);

        builder.Property(e => e.MaxEnrollmentsPerMember)
            .HasDefaultValue(1);

        builder.Property(e => e.CooldownDays)
            .HasDefaultValue(0);

        // Index on Type for flow type queries
        builder.HasIndex(e => e.Type)
            .HasDatabaseName("IX_CampaignFlows_Type");

        // Index on Trigger for trigger queries
        builder.HasIndex(e => e.Trigger)
            .HasDatabaseName("IX_CampaignFlows_Trigger");

        // Index on IsActive and IsEnabled
        builder.HasIndex(e => new { e.IsActive, e.IsEnabled })
            .HasDatabaseName("IX_CampaignFlows_Active_Enabled");

        // Index on StoreId
        builder.HasIndex(e => e.StoreId)
            .HasDatabaseName("IX_CampaignFlows_StoreId");

        // Foreign key to Store
        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.SetNull);

        // Navigation to steps
        builder.HasMany(e => e.Steps)
            .WithOne(s => s.Flow)
            .HasForeignKey(s => s.FlowId)
            .OnDelete(DeleteBehavior.Cascade);

        // Navigation to enrollments
        builder.HasMany(e => e.Enrollments)
            .WithOne(e => e.Flow)
            .HasForeignKey(e => e.FlowId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// Entity configuration for CampaignFlowStep.
/// </summary>
public class CampaignFlowStepConfiguration : IEntityTypeConfiguration<CampaignFlowStep>
{
    public void Configure(EntityTypeBuilder<CampaignFlowStep> builder)
    {
        builder.ToTable("CampaignFlowSteps");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.DelayDays)
            .HasDefaultValue(0);

        builder.Property(e => e.DelayHours)
            .HasDefaultValue(0);

        builder.Property(e => e.Channel)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.Subject)
            .HasMaxLength(200);

        builder.Property(e => e.Content)
            .HasMaxLength(4000);

        builder.Property(e => e.DiscountPercent)
            .HasPrecision(5, 2);

        builder.Property(e => e.DiscountAmount)
            .HasPrecision(18, 2);

        builder.Property(e => e.ConditionType)
            .HasConversion<int>()
            .HasDefaultValue(StepConditionType.None);

        builder.Property(e => e.ConditionValue)
            .HasMaxLength(200);

        builder.Property(e => e.IsEnabled)
            .HasDefaultValue(true);

        // Index on FlowId + StepOrder
        builder.HasIndex(e => new { e.FlowId, e.StepOrder })
            .HasDatabaseName("IX_CampaignFlowSteps_Flow_Order");

        // Index on IsActive
        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_CampaignFlowSteps_IsActive");

        // Foreign key to Flow
        builder.HasOne(e => e.Flow)
            .WithMany(f => f.Steps)
            .HasForeignKey(e => e.FlowId)
            .OnDelete(DeleteBehavior.Cascade);

        // Navigation to executions
        builder.HasMany(e => e.Executions)
            .WithOne(ex => ex.Step)
            .HasForeignKey(ex => ex.StepId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// Entity configuration for MemberFlowEnrollment.
/// </summary>
public class MemberFlowEnrollmentConfiguration : IEntityTypeConfiguration<MemberFlowEnrollment>
{
    public void Configure(EntityTypeBuilder<MemberFlowEnrollment> builder)
    {
        builder.ToTable("MemberFlowEnrollments");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Status)
            .HasConversion<int>()
            .HasDefaultValue(FlowEnrollmentStatus.Active);

        builder.Property(e => e.EnrolledAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.CancellationReason)
            .HasMaxLength(500);

        builder.Property(e => e.ContextJson)
            .HasMaxLength(2000);

        // Index on MemberId + FlowId
        builder.HasIndex(e => new { e.MemberId, e.FlowId })
            .HasDatabaseName("IX_MemberFlowEnrollments_Member_Flow");

        // Index on Status for active enrollment queries
        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_MemberFlowEnrollments_Status");

        // Index on NextStepScheduledAt for processing
        builder.HasIndex(e => e.NextStepScheduledAt)
            .HasDatabaseName("IX_MemberFlowEnrollments_NextStepScheduledAt");

        // Composite index for active enrollment processing
        builder.HasIndex(e => new { e.Status, e.NextStepScheduledAt, e.IsActive })
            .HasDatabaseName("IX_MemberFlowEnrollments_Status_Schedule_Active");

        // Foreign key to LoyaltyMember
        builder.HasOne(e => e.Member)
            .WithMany()
            .HasForeignKey(e => e.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        // Foreign key to CampaignFlow
        builder.HasOne(e => e.Flow)
            .WithMany(f => f.Enrollments)
            .HasForeignKey(e => e.FlowId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign key to Store
        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.SetNull);

        // Navigation to executions
        builder.HasMany(e => e.Executions)
            .WithOne(ex => ex.Enrollment)
            .HasForeignKey(ex => ex.EnrollmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Entity configuration for FlowStepExecution.
/// </summary>
public class FlowStepExecutionConfiguration : IEntityTypeConfiguration<FlowStepExecution>
{
    public void Configure(EntityTypeBuilder<FlowStepExecution> builder)
    {
        builder.ToTable("FlowStepExecutions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Status)
            .HasConversion<int>()
            .HasDefaultValue(FlowStepExecutionStatus.Scheduled);

        builder.Property(e => e.Channel)
            .HasConversion<int>();

        builder.Property(e => e.ExternalMessageId)
            .HasMaxLength(200);

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(1000);

        builder.Property(e => e.RetryCount)
            .HasDefaultValue(0);

        builder.Property(e => e.DiscountCode)
            .HasMaxLength(50);

        builder.Property(e => e.SkipReason)
            .HasMaxLength(500);

        builder.Property(e => e.RenderedSubject)
            .HasMaxLength(200);

        builder.Property(e => e.RenderedContent)
            .HasMaxLength(4000);

        // Index on EnrollmentId + StepId
        builder.HasIndex(e => new { e.EnrollmentId, e.StepId })
            .HasDatabaseName("IX_FlowStepExecutions_Enrollment_Step");

        // Index on Status for processing
        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_FlowStepExecutions_Status");

        // Index on ScheduledAt for processing
        builder.HasIndex(e => e.ScheduledAt)
            .HasDatabaseName("IX_FlowStepExecutions_ScheduledAt");

        // Composite index for scheduled step processing
        builder.HasIndex(e => new { e.Status, e.ScheduledAt, e.IsActive })
            .HasDatabaseName("IX_FlowStepExecutions_Status_Scheduled_Active");

        // Foreign key to MemberFlowEnrollment
        builder.HasOne(e => e.Enrollment)
            .WithMany(en => en.Executions)
            .HasForeignKey(e => e.EnrollmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Foreign key to CampaignFlowStep
        builder.HasOne(e => e.Step)
            .WithMany(s => s.Executions)
            .HasForeignKey(e => e.StepId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// Entity configuration for CampaignEmailTemplate.
/// </summary>
public class CampaignEmailTemplateConfiguration : IEntityTypeConfiguration<CampaignEmailTemplate>
{
    public void Configure(EntityTypeBuilder<CampaignEmailTemplate> builder)
    {
        builder.ToTable("CampaignEmailTemplates");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.Subject)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.HtmlBody)
            .IsRequired();

        builder.Property(e => e.TextBody)
            .HasMaxLength(4000);

        builder.Property(e => e.Category)
            .HasMaxLength(50);

        // Index on StoreId
        builder.HasIndex(e => e.StoreId)
            .HasDatabaseName("IX_CampaignEmailTemplates_StoreId");

        // Index on Category
        builder.HasIndex(e => e.Category)
            .HasDatabaseName("IX_CampaignEmailTemplates_Category");

        // Foreign key to Store
        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>
/// Entity configuration for CampaignSmsTemplate.
/// </summary>
public class CampaignSmsTemplateConfiguration : IEntityTypeConfiguration<CampaignSmsTemplate>
{
    public void Configure(EntityTypeBuilder<CampaignSmsTemplate> builder)
    {
        builder.ToTable("CampaignSmsTemplates");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.Content)
            .IsRequired()
            .HasMaxLength(480);

        builder.Property(e => e.Category)
            .HasMaxLength(50);

        // Index on StoreId
        builder.HasIndex(e => e.StoreId)
            .HasDatabaseName("IX_CampaignSmsTemplates_StoreId");

        // Index on Category
        builder.HasIndex(e => e.Category)
            .HasDatabaseName("IX_CampaignSmsTemplates_Category");

        // Foreign key to Store
        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>
/// Entity configuration for CampaignFlowConfiguration.
/// </summary>
public class CampaignFlowConfigurationEntityConfiguration : IEntityTypeConfiguration<CampaignFlowConfiguration>
{
    public void Configure(EntityTypeBuilder<CampaignFlowConfiguration> builder)
    {
        builder.ToTable("CampaignFlowConfigurations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.IsEnabled)
            .HasDefaultValue(true);

        builder.Property(e => e.EmailEnabled)
            .HasDefaultValue(true);

        builder.Property(e => e.SmsEnabled)
            .HasDefaultValue(true);

        builder.Property(e => e.DefaultFromEmail)
            .HasMaxLength(200);

        builder.Property(e => e.DefaultFromName)
            .HasMaxLength(100);

        builder.Property(e => e.DefaultSmsFrom)
            .HasMaxLength(20);

        builder.Property(e => e.MaxMessagesPerMemberPerDay)
            .HasDefaultValue(3);

        builder.Property(e => e.QuietHoursStart)
            .HasDefaultValue(21);

        builder.Property(e => e.QuietHoursEnd)
            .HasDefaultValue(8);

        builder.Property(e => e.WinBackInactivityDays)
            .HasDefaultValue(30);

        builder.Property(e => e.BirthdayFlowStartDays)
            .HasDefaultValue(7);

        builder.Property(e => e.PointsExpiryNotifyDays)
            .HasDefaultValue(30);

        builder.Property(e => e.MaxRetryAttempts)
            .HasDefaultValue(3);

        builder.Property(e => e.RetryDelayMinutes)
            .HasDefaultValue(15);

        // Unique index on StoreId
        builder.HasIndex(e => e.StoreId)
            .IsUnique()
            .HasDatabaseName("IX_CampaignFlowConfigurations_StoreId");

        // Foreign key to Store
        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
