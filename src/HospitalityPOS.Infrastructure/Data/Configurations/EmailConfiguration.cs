using HospitalityPOS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for EmailConfiguration entity.
/// </summary>
public class EmailConfigurationEntityConfiguration : IEntityTypeConfiguration<EmailConfiguration>
{
    public void Configure(EntityTypeBuilder<EmailConfiguration> builder)
    {
        builder.ToTable("EmailConfigurations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.SmtpHost)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.SmtpPort)
            .HasDefaultValue(587);

        builder.Property(e => e.SmtpUsername)
            .HasMaxLength(100);

        builder.Property(e => e.SmtpPasswordEncrypted)
            .HasMaxLength(500);

        builder.Property(e => e.FromAddress)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.FromName)
            .HasMaxLength(100);

        builder.Property(e => e.ReplyToAddress)
            .HasMaxLength(100);

        builder.Property(e => e.TimeZone)
            .HasMaxLength(50)
            .HasDefaultValue("Africa/Nairobi");

        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.StoreId)
            .HasDatabaseName("IX_EmailConfigurations_StoreId");

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_EmailConfigurations_IsActive");
    }
}

/// <summary>
/// EF Core configuration for EmailRecipient entity.
/// </summary>
public class EmailRecipientConfiguration : IEntityTypeConfiguration<EmailRecipient>
{
    public void Configure(EntityTypeBuilder<EmailRecipient> builder)
    {
        builder.ToTable("EmailRecipients");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Email)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Name)
            .HasMaxLength(100);

        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.Email)
            .HasDatabaseName("IX_EmailRecipients_Email");

        builder.HasIndex(e => e.StoreId)
            .HasDatabaseName("IX_EmailRecipients_StoreId");

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_EmailRecipients_IsActive");
    }
}

/// <summary>
/// EF Core configuration for EmailSchedule entity.
/// </summary>
public class EmailScheduleConfiguration : IEntityTypeConfiguration<EmailSchedule>
{
    public void Configure(EntityTypeBuilder<EmailSchedule> builder)
    {
        builder.ToTable("EmailSchedules");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ReportType)
            .IsRequired();

        builder.Property(e => e.TimeZone)
            .HasMaxLength(50)
            .HasDefaultValue("Africa/Nairobi");

        builder.Property(e => e.CustomSubject)
            .HasMaxLength(200);

        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.ReportType, e.StoreId })
            .HasDatabaseName("IX_EmailSchedules_ReportType_StoreId");

        builder.HasIndex(e => e.IsEnabled)
            .HasDatabaseName("IX_EmailSchedules_IsEnabled");

        builder.HasIndex(e => e.NextScheduledAt)
            .HasDatabaseName("IX_EmailSchedules_NextScheduledAt");
    }
}

/// <summary>
/// EF Core configuration for EmailLog entity.
/// </summary>
public class EmailLogConfiguration : IEntityTypeConfiguration<EmailLog>
{
    public void Configure(EntityTypeBuilder<EmailLog> builder)
    {
        builder.ToTable("EmailLogs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Recipients)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.Subject)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(500);

        builder.Property(e => e.AttachmentName)
            .HasMaxLength(255);

        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.EmailSchedule)
            .WithMany()
            .HasForeignKey(e => e.EmailScheduleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_EmailLogs_Status");

        builder.HasIndex(e => e.ReportType)
            .HasDatabaseName("IX_EmailLogs_ReportType");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_EmailLogs_CreatedAt");

        builder.HasIndex(e => e.SentAt)
            .HasDatabaseName("IX_EmailLogs_SentAt");

        builder.HasIndex(e => e.StoreId)
            .HasDatabaseName("IX_EmailLogs_StoreId");
    }
}

/// <summary>
/// EF Core configuration for EmailTemplate entity.
/// </summary>
public class EmailTemplateConfiguration : IEntityTypeConfiguration<EmailTemplate>
{
    public void Configure(EntityTypeBuilder<EmailTemplate> builder)
    {
        builder.ToTable("EmailTemplates");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.SubjectTemplate)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.HtmlBodyTemplate)
            .IsRequired();

        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.ReportType, e.IsDefault })
            .HasDatabaseName("IX_EmailTemplates_ReportType_IsDefault");

        builder.HasIndex(e => e.StoreId)
            .HasDatabaseName("IX_EmailTemplates_StoreId");
    }
}

/// <summary>
/// EF Core configuration for LowStockAlertConfig entity.
/// </summary>
public class LowStockAlertConfigConfiguration : IEntityTypeConfiguration<LowStockAlertConfig>
{
    public void Configure(EntityTypeBuilder<LowStockAlertConfig> builder)
    {
        builder.ToTable("LowStockAlertConfigs");

        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.StoreId)
            .HasDatabaseName("IX_LowStockAlertConfigs_StoreId");
    }
}

/// <summary>
/// EF Core configuration for ExpiryAlertConfig entity.
/// </summary>
public class ExpiryAlertConfigConfiguration : IEntityTypeConfiguration<ExpiryAlertConfig>
{
    public void Configure(EntityTypeBuilder<ExpiryAlertConfig> builder)
    {
        builder.ToTable("ExpiryAlertConfigs");

        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.StoreId)
            .HasDatabaseName("IX_ExpiryAlertConfigs_StoreId");
    }
}
