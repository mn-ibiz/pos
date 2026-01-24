using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for LaborConfiguration entity.
/// </summary>
public class LaborConfigurationEntityConfiguration : IEntityTypeConfiguration<LaborConfiguration>
{
    public void Configure(EntityTypeBuilder<LaborConfiguration> builder)
    {
        builder.ToTable("LaborConfigurations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TargetLaborPercent)
            .HasPrecision(5, 2)
            .HasDefaultValue(25m);

        builder.Property(e => e.TargetSPLH)
            .HasPrecision(10, 2)
            .HasDefaultValue(50m);

        builder.Property(e => e.OvertimeMultiplier)
            .HasPrecision(4, 2)
            .HasDefaultValue(1.5m);

        builder.Property(e => e.MinStaffPerShift)
            .HasDefaultValue(2);

        builder.Property(e => e.MaxStaffPerShift)
            .HasDefaultValue(20);

        builder.Property(e => e.OvertimeThresholdHours)
            .HasDefaultValue(40);

        builder.Property(e => e.MinShiftHours)
            .HasDefaultValue(4);

        builder.Property(e => e.MaxShiftHours)
            .HasDefaultValue(10);

        builder.Property(e => e.MinHoursBetweenShifts)
            .HasDefaultValue(8);

        builder.Property(e => e.EnableForecasting)
            .HasDefaultValue(true);

        builder.Property(e => e.ForecastHistoryDays)
            .HasDefaultValue(90);

        builder.Property(e => e.ForecastAheadWeeks)
            .HasDefaultValue(2);

        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.StoreId)
            .HasDatabaseName("IX_LaborConfigurations_StoreId");

        builder.HasIndex(e => new { e.StoreId, e.IsActive })
            .HasDatabaseName("IX_LaborConfigurations_StoreId_IsActive");
    }
}

/// <summary>
/// EF Core configuration for LaborRoleConfiguration entity.
/// </summary>
public class LaborRoleConfigurationEntityConfiguration : IEntityTypeConfiguration<LaborRoleConfiguration>
{
    public void Configure(EntityTypeBuilder<LaborRoleConfiguration> builder)
    {
        builder.ToTable("LaborRoleConfigurations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.RoleName)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.HourlyRate)
            .HasPrecision(10, 2);

        builder.Property(e => e.TransactionsPerHour)
            .HasPrecision(10, 2)
            .HasDefaultValue(20m);

        builder.Property(e => e.MinStaff)
            .HasDefaultValue(1);

        builder.Property(e => e.MaxStaff)
            .HasDefaultValue(10);

        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.StoreId)
            .HasDatabaseName("IX_LaborRoleConfigurations_StoreId");

        builder.HasIndex(e => new { e.StoreId, e.RoleName })
            .IsUnique()
            .HasDatabaseName("IX_LaborRoleConfigurations_StoreId_RoleName");
    }
}

/// <summary>
/// EF Core configuration for DailyLaborForecast entity.
/// </summary>
public class DailyLaborForecastEntityConfiguration : IEntityTypeConfiguration<DailyLaborForecast>
{
    public void Configure(EntityTypeBuilder<DailyLaborForecast> builder)
    {
        builder.ToTable("DailyLaborForecasts");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Date)
            .HasColumnType("date");

        builder.Property(e => e.TotalForecastedSales)
            .HasPrecision(18, 2);

        builder.Property(e => e.TotalLaborHoursNeeded)
            .HasPrecision(10, 2);

        builder.Property(e => e.TotalLaborCost)
            .HasPrecision(18, 2);

        builder.Property(e => e.ForecastedLaborPercent)
            .HasPrecision(5, 2);

        builder.Property(e => e.ConfidenceLevel)
            .HasPrecision(4, 3);

        builder.Property(e => e.Status)
            .HasConversion<int>()
            .HasDefaultValue(ForecastStatus.Draft);

        builder.Property(e => e.SpecialFactors)
            .HasMaxLength(1000);

        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.GeneratedByUser)
            .WithMany()
            .HasForeignKey(e => e.GeneratedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(e => e.HourlyForecasts)
            .WithOne(h => h.DailyForecast)
            .HasForeignKey(h => h.DailyForecastId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.ShiftRecommendations)
            .WithOne(s => s.DailyForecast)
            .HasForeignKey(s => s.DailyForecastId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.StoreId)
            .HasDatabaseName("IX_DailyLaborForecasts_StoreId");

        builder.HasIndex(e => e.Date)
            .HasDatabaseName("IX_DailyLaborForecasts_Date");

        builder.HasIndex(e => new { e.StoreId, e.Date })
            .HasDatabaseName("IX_DailyLaborForecasts_StoreId_Date");

        builder.HasIndex(e => new { e.StoreId, e.Date, e.Status })
            .HasDatabaseName("IX_DailyLaborForecasts_StoreId_Date_Status");
    }
}

/// <summary>
/// EF Core configuration for HourlyLaborForecast entity.
/// </summary>
public class HourlyLaborForecastEntityConfiguration : IEntityTypeConfiguration<HourlyLaborForecast>
{
    public void Configure(EntityTypeBuilder<HourlyLaborForecast> builder)
    {
        builder.ToTable("HourlyLaborForecasts");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ForecastedSales)
            .HasPrecision(18, 2);

        builder.Property(e => e.TargetSPLH)
            .HasPrecision(10, 2);

        builder.Property(e => e.LaborCostEstimate)
            .HasPrecision(18, 2);

        builder.Property(e => e.ConfidenceLevel)
            .HasPrecision(4, 3);

        builder.Property(e => e.Factors)
            .HasMaxLength(500);

        builder.HasMany(e => e.RoleForecasts)
            .WithOne(r => r.HourlyForecast)
            .HasForeignKey(r => r.HourlyForecastId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.DailyForecastId)
            .HasDatabaseName("IX_HourlyLaborForecasts_DailyForecastId");

        builder.HasIndex(e => new { e.DailyForecastId, e.Hour })
            .IsUnique()
            .HasDatabaseName("IX_HourlyLaborForecasts_DailyForecastId_Hour");
    }
}

/// <summary>
/// EF Core configuration for HourlyRoleForecast entity.
/// </summary>
public class HourlyRoleForecastEntityConfiguration : IEntityTypeConfiguration<HourlyRoleForecast>
{
    public void Configure(EntityTypeBuilder<HourlyRoleForecast> builder)
    {
        builder.ToTable("HourlyRoleForecasts");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.RoleName)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.LaborCostEstimate)
            .HasPrecision(18, 2);

        builder.HasIndex(e => e.HourlyForecastId)
            .HasDatabaseName("IX_HourlyRoleForecasts_HourlyForecastId");

        builder.HasIndex(e => new { e.HourlyForecastId, e.RoleName })
            .IsUnique()
            .HasDatabaseName("IX_HourlyRoleForecasts_HourlyForecastId_RoleName");
    }
}

/// <summary>
/// EF Core configuration for ShiftRecommendation entity.
/// </summary>
public class ShiftRecommendationEntityConfiguration : IEntityTypeConfiguration<ShiftRecommendation>
{
    public void Configure(EntityTypeBuilder<ShiftRecommendation> builder)
    {
        builder.ToTable("ShiftRecommendations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.RoleName)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.StartTime)
            .HasColumnType("time");

        builder.Property(e => e.EndTime)
            .HasColumnType("time");

        builder.Property(e => e.EstimatedCost)
            .HasPrecision(18, 2);

        builder.Property(e => e.Reason)
            .HasMaxLength(200);

        builder.HasIndex(e => e.DailyForecastId)
            .HasDatabaseName("IX_ShiftRecommendations_DailyForecastId");
    }
}

/// <summary>
/// EF Core configuration for StaffingIssue entity.
/// </summary>
public class StaffingIssueEntityConfiguration : IEntityTypeConfiguration<StaffingIssue>
{
    public void Configure(EntityTypeBuilder<StaffingIssue> builder)
    {
        builder.ToTable("StaffingIssues");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.IssueType)
            .HasConversion<int>();

        builder.Property(e => e.RoleName)
            .HasMaxLength(50);

        builder.Property(e => e.ImpactEstimate)
            .HasPrecision(18, 2);

        builder.Property(e => e.Recommendation)
            .HasMaxLength(500);

        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.StoreId)
            .HasDatabaseName("IX_StaffingIssues_StoreId");

        builder.HasIndex(e => e.IssueDateTime)
            .HasDatabaseName("IX_StaffingIssues_IssueDateTime");

        builder.HasIndex(e => new { e.StoreId, e.IssueDateTime })
            .HasDatabaseName("IX_StaffingIssues_StoreId_IssueDateTime");

        builder.HasIndex(e => new { e.StoreId, e.IsResolved })
            .HasDatabaseName("IX_StaffingIssues_StoreId_IsResolved");
    }
}

/// <summary>
/// EF Core configuration for OptimizationSuggestion entity.
/// </summary>
public class OptimizationSuggestionEntityConfiguration : IEntityTypeConfiguration<OptimizationSuggestion>
{
    public void Configure(EntityTypeBuilder<OptimizationSuggestion> builder)
    {
        builder.ToTable("OptimizationSuggestions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.SuggestionType)
            .HasConversion<int>();

        builder.Property(e => e.ScheduleDate)
            .HasColumnType("date");

        builder.Property(e => e.RoleName)
            .HasMaxLength(50);

        builder.Property(e => e.CurrentValue)
            .HasMaxLength(100);

        builder.Property(e => e.SuggestedValue)
            .HasMaxLength(100);

        builder.Property(e => e.EstimatedSavings)
            .HasPrecision(18, 2);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Employee)
            .WithMany()
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.StoreId)
            .HasDatabaseName("IX_OptimizationSuggestions_StoreId");

        builder.HasIndex(e => e.ScheduleDate)
            .HasDatabaseName("IX_OptimizationSuggestions_ScheduleDate");

        builder.HasIndex(e => new { e.StoreId, e.ScheduleDate })
            .HasDatabaseName("IX_OptimizationSuggestions_StoreId_ScheduleDate");

        builder.HasIndex(e => new { e.StoreId, e.IsApplied })
            .HasDatabaseName("IX_OptimizationSuggestions_StoreId_IsApplied");
    }
}

/// <summary>
/// EF Core configuration for LaborEfficiencyMetrics entity.
/// </summary>
public class LaborEfficiencyMetricsEntityConfiguration : IEntityTypeConfiguration<LaborEfficiencyMetrics>
{
    public void Configure(EntityTypeBuilder<LaborEfficiencyMetrics> builder)
    {
        builder.ToTable("LaborEfficiencyMetrics");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Date)
            .HasColumnType("date");

        builder.Property(e => e.ForecastedSales)
            .HasPrecision(18, 2);

        builder.Property(e => e.ActualSales)
            .HasPrecision(18, 2);

        builder.Property(e => e.SalesForecastAccuracy)
            .HasPrecision(5, 4);

        builder.Property(e => e.ForecastedLaborHours)
            .HasPrecision(10, 2);

        builder.Property(e => e.ActualLaborHours)
            .HasPrecision(10, 2);

        builder.Property(e => e.ForecastedLaborCost)
            .HasPrecision(18, 2);

        builder.Property(e => e.ActualLaborCost)
            .HasPrecision(18, 2);

        builder.Property(e => e.ActualSPLH)
            .HasPrecision(10, 2);

        builder.Property(e => e.TargetSPLH)
            .HasPrecision(10, 2);

        builder.Property(e => e.ActualLaborPercent)
            .HasPrecision(5, 2);

        builder.Property(e => e.TargetLaborPercent)
            .HasPrecision(5, 2);

        builder.Property(e => e.OvertimeHours)
            .HasPrecision(10, 2);

        builder.Property(e => e.OvertimeCost)
            .HasPrecision(18, 2);

        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.StoreId)
            .HasDatabaseName("IX_LaborEfficiencyMetrics_StoreId");

        builder.HasIndex(e => e.Date)
            .HasDatabaseName("IX_LaborEfficiencyMetrics_Date");

        builder.HasIndex(e => new { e.StoreId, e.Date })
            .IsUnique()
            .HasDatabaseName("IX_LaborEfficiencyMetrics_StoreId_Date");
    }
}
