using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework Core configuration for the SystemConfiguration entity.
/// </summary>
public class SystemConfigurationConfiguration : IEntityTypeConfiguration<SystemConfiguration>
{
    public void Configure(EntityTypeBuilder<SystemConfiguration> builder)
    {
        builder.ToTable("SystemConfigurations");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Mode)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(c => c.BusinessName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.BusinessAddress)
            .HasMaxLength(500);

        builder.Property(c => c.BusinessPhone)
            .HasMaxLength(50);

        builder.Property(c => c.BusinessEmail)
            .HasMaxLength(200);

        builder.Property(c => c.TaxRegistrationNumber)
            .HasMaxLength(100);

        builder.Property(c => c.CurrencyCode)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("KES");

        builder.Property(c => c.CurrencySymbol)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("Ksh");

        // Restaurant features
        builder.Property(c => c.EnableTableManagement)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(c => c.EnableKitchenDisplay)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(c => c.EnableWaiterAssignment)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(c => c.EnableCourseSequencing)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(c => c.EnableReservations)
            .IsRequired()
            .HasDefaultValue(false);

        // Retail features
        builder.Property(c => c.EnableBarcodeAutoFocus)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(c => c.EnableProductOffers)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(c => c.EnableSupplierCredit)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(c => c.EnableLoyaltyProgram)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(c => c.EnableBatchExpiry)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(c => c.EnableScaleIntegration)
            .IsRequired()
            .HasDefaultValue(false);

        // Enterprise features
        builder.Property(c => c.EnablePayroll)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(c => c.EnableAccounting)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(c => c.EnableMultiStore)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(c => c.EnableCloudSync)
            .IsRequired()
            .HasDefaultValue(false);

        // Kenya features
        builder.Property(c => c.EnableKenyaETims)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(c => c.EnableMpesa)
            .IsRequired()
            .HasDefaultValue(true);

        // Setup status
        builder.Property(c => c.SetupCompleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(c => c.CreatedAt)
            .IsRequired();
    }
}
