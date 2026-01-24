using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for ProductAssociation.
/// </summary>
public class ProductAssociationConfiguration : IEntityTypeConfiguration<ProductAssociation>
{
    public void Configure(EntityTypeBuilder<ProductAssociation> builder)
    {
        builder.ToTable("ProductAssociations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.Support)
            .HasPrecision(10, 6)
            .IsRequired();

        builder.Property(e => e.Confidence)
            .HasPrecision(10, 6)
            .IsRequired();

        builder.Property(e => e.Lift)
            .HasPrecision(10, 4)
            .IsRequired();

        // Index on ProductId for lookups
        builder.HasIndex(e => e.ProductId)
            .HasDatabaseName("IX_ProductAssociations_ProductId");

        // Index on AssociatedProductId
        builder.HasIndex(e => e.AssociatedProductId)
            .HasDatabaseName("IX_ProductAssociations_AssociatedProductId");

        // Composite index for lookup by product pair
        builder.HasIndex(e => new { e.ProductId, e.AssociatedProductId })
            .HasDatabaseName("IX_ProductAssociations_Product_Associated");

        // Index for active associations with lift
        builder.HasIndex(e => new { e.IsActive, e.Lift })
            .HasDatabaseName("IX_ProductAssociations_Active_Lift");

        // Index on StoreId
        builder.HasIndex(e => e.StoreId)
            .HasDatabaseName("IX_ProductAssociations_StoreId");

        // Foreign key to Product
        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign key to AssociatedProduct
        builder.HasOne(e => e.AssociatedProduct)
            .WithMany()
            .HasForeignKey(e => e.AssociatedProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign key to Store
        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>
/// Entity configuration for UpsellRule.
/// </summary>
public class UpsellRuleConfiguration : IEntityTypeConfiguration<UpsellRule>
{
    public void Configure(EntityTypeBuilder<UpsellRule> builder)
    {
        builder.ToTable("UpsellRules");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.SuggestionText)
            .HasMaxLength(200);

        builder.Property(e => e.SavingsAmount)
            .HasPrecision(18, 2);

        builder.Property(e => e.Priority)
            .HasDefaultValue(1);

        builder.Property(e => e.IsEnabled)
            .HasDefaultValue(true);

        builder.Property(e => e.TodaySuggestionCount)
            .HasDefaultValue(0);

        builder.Property(e => e.TimeOfDayFilter)
            .HasConversion<int?>();

        // Index on SourceProductId
        builder.HasIndex(e => e.SourceProductId)
            .HasDatabaseName("IX_UpsellRules_SourceProductId");

        // Index on SourceCategoryId
        builder.HasIndex(e => e.SourceCategoryId)
            .HasDatabaseName("IX_UpsellRules_SourceCategoryId");

        // Index on TargetProductId
        builder.HasIndex(e => e.TargetProductId)
            .HasDatabaseName("IX_UpsellRules_TargetProductId");

        // Index for active enabled rules
        builder.HasIndex(e => new { e.IsActive, e.IsEnabled, e.Priority })
            .HasDatabaseName("IX_UpsellRules_Active_Enabled_Priority");

        // Index on StoreId
        builder.HasIndex(e => e.StoreId)
            .HasDatabaseName("IX_UpsellRules_StoreId");

        // Foreign keys
        builder.HasOne(e => e.SourceProduct)
            .WithMany()
            .HasForeignKey(e => e.SourceProductId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.SourceCategory)
            .WithMany()
            .HasForeignKey(e => e.SourceCategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.TargetProduct)
            .WithMany()
            .HasForeignKey(e => e.TargetProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>
/// Entity configuration for UpsellSuggestionLog.
/// </summary>
public class UpsellSuggestionLogConfiguration : IEntityTypeConfiguration<UpsellSuggestionLog>
{
    public void Configure(EntityTypeBuilder<UpsellSuggestionLog> builder)
    {
        builder.ToTable("UpsellSuggestionLogs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.SuggestionType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.ConfidenceScore)
            .HasPrecision(5, 4)
            .IsRequired();

        builder.Property(e => e.AcceptedValue)
            .HasPrecision(18, 2);

        builder.Property(e => e.TriggerProductIds)
            .HasMaxLength(200);

        builder.Property(e => e.SuggestedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Index on ReceiptId for receipt-based lookups
        builder.HasIndex(e => e.ReceiptId)
            .HasDatabaseName("IX_UpsellSuggestionLogs_ReceiptId");

        // Index on SuggestedProductId
        builder.HasIndex(e => e.SuggestedProductId)
            .HasDatabaseName("IX_UpsellSuggestionLogs_SuggestedProductId");

        // Index on SuggestedAt for date range queries
        builder.HasIndex(e => e.SuggestedAt)
            .HasDatabaseName("IX_UpsellSuggestionLogs_SuggestedAt");

        // Index for analytics queries
        builder.HasIndex(e => new { e.SuggestedAt, e.WasAccepted })
            .HasDatabaseName("IX_UpsellSuggestionLogs_Date_Accepted");

        // Index on RuleId
        builder.HasIndex(e => e.RuleId)
            .HasDatabaseName("IX_UpsellSuggestionLogs_RuleId");

        // Index on AssociationId
        builder.HasIndex(e => e.AssociationId)
            .HasDatabaseName("IX_UpsellSuggestionLogs_AssociationId");

        // Index on StoreId
        builder.HasIndex(e => e.StoreId)
            .HasDatabaseName("IX_UpsellSuggestionLogs_StoreId");

        // Foreign keys
        builder.HasOne(e => e.Receipt)
            .WithMany()
            .HasForeignKey(e => e.ReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.SuggestedProduct)
            .WithMany()
            .HasForeignKey(e => e.SuggestedProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Association)
            .WithMany()
            .HasForeignKey(e => e.AssociationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Rule)
            .WithMany()
            .HasForeignKey(e => e.RuleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Customer)
            .WithMany()
            .HasForeignKey(e => e.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>
/// Entity configuration for CustomerPreference.
/// </summary>
public class CustomerPreferenceConfiguration : IEntityTypeConfiguration<CustomerPreference>
{
    public void Configure(EntityTypeBuilder<CustomerPreference> builder)
    {
        builder.ToTable("CustomerPreferences");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TotalSpent)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.AverageQuantity)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(e => e.PreferenceScore)
            .HasPrecision(5, 4)
            .IsRequired();

        // Unique index on CustomerId + ProductId
        builder.HasIndex(e => new { e.CustomerId, e.ProductId })
            .IsUnique()
            .HasDatabaseName("IX_CustomerPreferences_Customer_Product");

        // Index on PreferenceScore for top-N queries
        builder.HasIndex(e => new { e.CustomerId, e.PreferenceScore })
            .HasDatabaseName("IX_CustomerPreferences_Customer_Score");

        // Index on StoreId
        builder.HasIndex(e => e.StoreId)
            .HasDatabaseName("IX_CustomerPreferences_StoreId");

        // Foreign keys
        builder.HasOne(e => e.Customer)
            .WithMany()
            .HasForeignKey(e => e.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>
/// Entity configuration for UpsellConfiguration.
/// </summary>
public class UpsellConfigurationEntityConfiguration : IEntityTypeConfiguration<UpsellConfiguration>
{
    public void Configure(EntityTypeBuilder<UpsellConfiguration> builder)
    {
        builder.ToTable("UpsellConfigurations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.IsEnabled)
            .HasDefaultValue(true);

        builder.Property(e => e.MaxSuggestions)
            .HasDefaultValue(3);

        builder.Property(e => e.MinConfidenceScore)
            .HasPrecision(5, 4)
            .HasDefaultValue(0.3m);

        builder.Property(e => e.MinSupport)
            .HasPrecision(5, 4)
            .HasDefaultValue(0.01m);

        builder.Property(e => e.MinAssociationConfidence)
            .HasPrecision(5, 4)
            .HasDefaultValue(0.25m);

        builder.Property(e => e.MinLift)
            .HasPrecision(5, 2)
            .HasDefaultValue(1.2m);

        builder.Property(e => e.AnalysisDays)
            .HasDefaultValue(90);

        builder.Property(e => e.IncludePersonalized)
            .HasDefaultValue(true);

        builder.Property(e => e.IncludeTrending)
            .HasDefaultValue(true);

        builder.Property(e => e.EnforceCategoryDiversity)
            .HasDefaultValue(true);

        builder.Property(e => e.ExcludeRecentPurchaseDays)
            .HasDefaultValue(7);

        builder.Property(e => e.RuleWeight)
            .HasPrecision(5, 2)
            .HasDefaultValue(1.5m);

        builder.Property(e => e.AssociationWeight)
            .HasPrecision(5, 2)
            .HasDefaultValue(1.0m);

        builder.Property(e => e.PersonalizedWeight)
            .HasPrecision(5, 2)
            .HasDefaultValue(1.2m);

        builder.Property(e => e.TrendingWeight)
            .HasPrecision(5, 2)
            .HasDefaultValue(0.8m);

        builder.Property(e => e.TrendingDays)
            .HasDefaultValue(7);

        builder.Property(e => e.ShowSavingsAmount)
            .HasDefaultValue(true);

        builder.Property(e => e.DefaultSuggestionText)
            .HasMaxLength(200)
            .HasDefaultValue("Customers also bought {{ProductName}}");

        // Unique index on StoreId
        builder.HasIndex(e => e.StoreId)
            .IsUnique()
            .HasDatabaseName("IX_UpsellConfigurations_StoreId");

        // Foreign key to Store
        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Entity configuration for UpsellDailyMetrics.
/// </summary>
public class UpsellDailyMetricsConfiguration : IEntityTypeConfiguration<UpsellDailyMetrics>
{
    public void Configure(EntityTypeBuilder<UpsellDailyMetrics> builder)
    {
        builder.ToTable("UpsellDailyMetrics");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.AcceptanceRate)
            .HasPrecision(5, 4);

        builder.Property(e => e.TotalRevenue)
            .HasPrecision(18, 2);

        builder.Property(e => e.AverageValue)
            .HasPrecision(18, 2);

        builder.Property(e => e.RuleBasedRevenue)
            .HasPrecision(18, 2);

        builder.Property(e => e.AssociationBasedRevenue)
            .HasPrecision(18, 2);

        builder.Property(e => e.PersonalizedRevenue)
            .HasPrecision(18, 2);

        // Unique index on Date + StoreId
        builder.HasIndex(e => new { e.Date, e.StoreId })
            .IsUnique()
            .HasDatabaseName("IX_UpsellDailyMetrics_Date_Store");

        // Foreign key to Store
        builder.HasOne(e => e.Store)
            .WithMany()
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
