using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for ZReportRecord entity.
/// </summary>
public class ZReportRecordConfiguration : IEntityTypeConfiguration<ZReportRecord>
{
    public void Configure(EntityTypeBuilder<ZReportRecord> builder)
    {
        builder.ToTable("ZReportRecords");

        builder.HasKey(e => e.Id);

        // Report identification
        builder.Property(e => e.ReportNumber)
            .IsRequired();

        builder.Property(e => e.ReportDateTime)
            .IsRequired();

        builder.Property(e => e.PeriodStartDateTime)
            .IsRequired();

        builder.Property(e => e.PeriodEndDateTime)
            .IsRequired();

        builder.Property(e => e.GeneratedByUserName)
            .HasMaxLength(200)
            .IsRequired();

        // Sales summary - use decimal precision for currency
        builder.Property(e => e.GrossSales)
            .HasPrecision(18, 4);

        builder.Property(e => e.NetSales)
            .HasPrecision(18, 4);

        builder.Property(e => e.TotalRefunds)
            .HasPrecision(18, 4);

        builder.Property(e => e.TotalVoids)
            .HasPrecision(18, 4);

        builder.Property(e => e.TotalDiscounts)
            .HasPrecision(18, 4);

        builder.Property(e => e.TotalTax)
            .HasPrecision(18, 4);

        builder.Property(e => e.TotalTips)
            .HasPrecision(18, 4);

        builder.Property(e => e.RoundingAdjustment)
            .HasPrecision(18, 4);

        builder.Property(e => e.GrandTotal)
            .HasPrecision(18, 4);

        // Cash reconciliation
        builder.Property(e => e.OpeningCash)
            .HasPrecision(18, 4);

        builder.Property(e => e.CashReceived)
            .HasPrecision(18, 4);

        builder.Property(e => e.CashPaidOut)
            .HasPrecision(18, 4);

        builder.Property(e => e.ExpectedCash)
            .HasPrecision(18, 4);

        builder.Property(e => e.ActualCash)
            .HasPrecision(18, 4);

        builder.Property(e => e.CashVariance)
            .HasPrecision(18, 4);

        builder.Property(e => e.VarianceExplanation)
            .HasMaxLength(2000);

        // Statistics
        builder.Property(e => e.AverageTransactionValue)
            .HasPrecision(18, 4);

        // Integrity
        builder.Property(e => e.ReportHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(e => e.BatchNumber)
            .HasMaxLength(100);

        builder.Property(e => e.ConsolidatedFromReportIds)
            .HasMaxLength(1000);

        // Business info (denormalized for permanence)
        builder.Property(e => e.BusinessName)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.BusinessAddress)
            .HasMaxLength(1000);

        builder.Property(e => e.TaxId)
            .HasMaxLength(100);

        builder.Property(e => e.BusinessPhone)
            .HasMaxLength(50);

        // Full report data as JSON
        builder.Property(e => e.ReportDataJson)
            .HasColumnType("nvarchar(max)");

        // Relationships
        builder.HasOne(e => e.WorkPeriod)
            .WithMany()
            .HasForeignKey(e => e.WorkPeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.GeneratedByUser)
            .WithMany()
            .HasForeignKey(e => e.GeneratedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.VarianceApprovedByUser)
            .WithMany()
            .HasForeignKey(e => e.VarianceApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for efficient querying
        builder.HasIndex(e => e.ReportNumber);
        builder.HasIndex(e => e.ReportDateTime);
        builder.HasIndex(e => e.WorkPeriodId);
        builder.HasIndex(e => e.StoreId);
        builder.HasIndex(e => e.TerminalId);
        builder.HasIndex(e => e.IsFinalized);
        builder.HasIndex(e => new { e.StoreId, e.ReportNumber }).IsUnique();
        builder.HasIndex(e => new { e.StoreId, e.ReportDateTime });
        builder.HasIndex(e => new { e.IsFinalized, e.VarianceRequiresApproval });
    }
}

/// <summary>
/// EF Core configuration for ZReportCategorySales entity.
/// </summary>
public class ZReportCategorySalesConfiguration : IEntityTypeConfiguration<ZReportCategorySales>
{
    public void Configure(EntityTypeBuilder<ZReportCategorySales> builder)
    {
        builder.ToTable("ZReportCategorySales");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.CategoryName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.GrossAmount)
            .HasPrecision(18, 4);

        builder.Property(e => e.DiscountAmount)
            .HasPrecision(18, 4);

        builder.Property(e => e.NetAmount)
            .HasPrecision(18, 4);

        builder.Property(e => e.TaxAmount)
            .HasPrecision(18, 4);

        builder.Property(e => e.CostAmount)
            .HasPrecision(18, 4);

        builder.Property(e => e.GrossProfit)
            .HasPrecision(18, 4);

        builder.Property(e => e.PercentageOfSales)
            .HasPrecision(8, 4);

        builder.HasOne(e => e.ZReportRecord)
            .WithMany(z => z.CategorySales)
            .HasForeignKey(e => e.ZReportRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.ZReportRecordId);
    }
}

/// <summary>
/// EF Core configuration for ZReportPaymentSummary entity.
/// </summary>
public class ZReportPaymentSummaryConfiguration : IEntityTypeConfiguration<ZReportPaymentSummary>
{
    public void Configure(EntityTypeBuilder<ZReportPaymentSummary> builder)
    {
        builder.ToTable("ZReportPaymentSummaries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.PaymentMethodName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.PaymentMethodType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.TotalAmount)
            .HasPrecision(18, 4);

        builder.Property(e => e.RefundAmount)
            .HasPrecision(18, 4);

        builder.Property(e => e.NetAmount)
            .HasPrecision(18, 4);

        builder.Property(e => e.TipAmount)
            .HasPrecision(18, 4);

        builder.Property(e => e.PercentageOfSales)
            .HasPrecision(8, 4);

        builder.HasOne(e => e.ZReportRecord)
            .WithMany(z => z.PaymentSummaries)
            .HasForeignKey(e => e.ZReportRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.ZReportRecordId);
    }
}

/// <summary>
/// EF Core configuration for ZReportHourlySales entity.
/// </summary>
public class ZReportHourlySalesConfiguration : IEntityTypeConfiguration<ZReportHourlySales>
{
    public void Configure(EntityTypeBuilder<ZReportHourlySales> builder)
    {
        builder.ToTable("ZReportHourlySales");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.HourLabel)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.SalesAmount)
            .HasPrecision(18, 4);

        builder.Property(e => e.AverageTransaction)
            .HasPrecision(18, 4);

        builder.HasOne(e => e.ZReportRecord)
            .WithMany(z => z.HourlySales)
            .HasForeignKey(e => e.ZReportRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.ZReportRecordId);
        builder.HasIndex(e => new { e.ZReportRecordId, e.Hour });
    }
}

/// <summary>
/// EF Core configuration for ZReportUserSales entity.
/// </summary>
public class ZReportUserSalesConfiguration : IEntityTypeConfiguration<ZReportUserSales>
{
    public void Configure(EntityTypeBuilder<ZReportUserSales> builder)
    {
        builder.ToTable("ZReportUserSales");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.UserName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.GrossSales)
            .HasPrecision(18, 4);

        builder.Property(e => e.DiscountAmount)
            .HasPrecision(18, 4);

        builder.Property(e => e.NetSales)
            .HasPrecision(18, 4);

        builder.Property(e => e.TipAmount)
            .HasPrecision(18, 4);

        builder.Property(e => e.AverageTransaction)
            .HasPrecision(18, 4);

        builder.Property(e => e.VoidAmount)
            .HasPrecision(18, 4);

        builder.Property(e => e.RefundAmount)
            .HasPrecision(18, 4);

        builder.HasOne(e => e.ZReportRecord)
            .WithMany(z => z.UserSales)
            .HasForeignKey(e => e.ZReportRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.ZReportRecordId);
        builder.HasIndex(e => new { e.ZReportRecordId, e.UserId });
    }
}

/// <summary>
/// EF Core configuration for ZReportTaxSummary entity.
/// </summary>
public class ZReportTaxSummaryConfiguration : IEntityTypeConfiguration<ZReportTaxSummary>
{
    public void Configure(EntityTypeBuilder<ZReportTaxSummary> builder)
    {
        builder.ToTable("ZReportTaxSummaries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TaxName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.TaxRate)
            .HasPrecision(8, 4);

        builder.Property(e => e.TaxableAmount)
            .HasPrecision(18, 4);

        builder.Property(e => e.TaxAmount)
            .HasPrecision(18, 4);

        builder.HasOne(e => e.ZReportRecord)
            .WithMany(z => z.TaxSummaries)
            .HasForeignKey(e => e.ZReportRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.ZReportRecordId);
    }
}

/// <summary>
/// EF Core configuration for ZReportSchedule entity.
/// </summary>
public class ZReportScheduleConfiguration : IEntityTypeConfiguration<ZReportSchedule>
{
    public void Configure(EntityTypeBuilder<ZReportSchedule> builder)
    {
        builder.ToTable("ZReportSchedules");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ScheduledTime)
            .IsRequired();

        builder.Property(e => e.NotificationEmails)
            .HasMaxLength(2000);

        builder.Property(e => e.ReportRecipientEmails)
            .HasMaxLength(2000);

        builder.Property(e => e.LastExecutionResult)
            .HasMaxLength(1000);

        builder.HasIndex(e => e.StoreId);
        builder.HasIndex(e => e.TerminalId);
        builder.HasIndex(e => e.IsEnabled);
        builder.HasIndex(e => e.NextExecutionAt);
    }
}

/// <summary>
/// EF Core configuration for ZReportVarianceThreshold entity.
/// </summary>
public class ZReportVarianceThresholdConfiguration : IEntityTypeConfiguration<ZReportVarianceThreshold>
{
    public void Configure(EntityTypeBuilder<ZReportVarianceThreshold> builder)
    {
        builder.ToTable("ZReportVarianceThresholds");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.AmountThreshold)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(e => e.PercentageThreshold)
            .HasPrecision(8, 4);

        builder.HasIndex(e => e.StoreId);
        builder.HasIndex(e => e.IsActive);
    }
}
