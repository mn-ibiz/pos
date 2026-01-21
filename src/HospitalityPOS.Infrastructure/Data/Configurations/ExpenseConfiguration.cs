using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for ExpenseCategory entity.
/// </summary>
public class ExpenseCategoryConfiguration : IEntityTypeConfiguration<ExpenseCategory>
{
    public void Configure(EntityTypeBuilder<ExpenseCategory> builder)
    {
        builder.ToTable("ExpenseCategories");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(ExpenseCategoryType.Operating);

        builder.Property(e => e.Icon)
            .HasMaxLength(50);

        builder.Property(e => e.Color)
            .HasMaxLength(7);

        builder.Property(e => e.SortOrder)
            .HasDefaultValue(0);

        builder.Property(e => e.IsSystemCategory)
            .HasDefaultValue(false);

        builder.HasIndex(e => e.Name);
        builder.HasIndex(e => e.Type);
        builder.HasIndex(e => e.SortOrder);

        builder.HasOne(e => e.ParentCategory)
            .WithMany(c => c.SubCategories)
            .HasForeignKey(e => e.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.DefaultAccount)
            .WithMany()
            .HasForeignKey(e => e.DefaultAccountId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>
/// EF Core configuration for Expense entity.
/// </summary>
public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("Expenses");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ExpenseNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.TaxAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(e => e.ExpenseDate)
            .IsRequired();

        builder.Property(e => e.PaymentMethod)
            .HasMaxLength(50);

        builder.Property(e => e.PaymentReference)
            .HasMaxLength(100);

        builder.Property(e => e.ReceiptImagePath)
            .HasMaxLength(500);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(ExpenseStatus.Pending);

        builder.Property(e => e.IsRecurring)
            .HasDefaultValue(false);

        builder.Property(e => e.IsTaxDeductible)
            .HasDefaultValue(true);

        builder.Property(e => e.Notes)
            .HasMaxLength(1000);

        builder.Property(e => e.RejectionReason)
            .HasMaxLength(500);

        // Ignore computed properties
        builder.Ignore(e => e.TotalAmount);

        // Indexes
        builder.HasIndex(e => e.ExpenseNumber)
            .IsUnique();

        builder.HasIndex(e => e.ExpenseDate);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.ExpenseCategoryId);
        builder.HasIndex(e => e.SupplierId);

        // Relationships
        builder.HasOne(e => e.ExpenseCategory)
            .WithMany(c => c.Expenses)
            .HasForeignKey(e => e.ExpenseCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Supplier)
            .WithMany(s => s.Expenses)
            .HasForeignKey(e => e.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.ApprovedByUser)
            .WithMany()
            .HasForeignKey(e => e.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.RecurringExpense)
            .WithMany(r => r.GeneratedExpenses)
            .HasForeignKey(e => e.RecurringExpenseId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.PaymentMethodEntity)
            .WithMany()
            .HasForeignKey(e => e.PaymentMethodId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>
/// EF Core configuration for RecurringExpense entity.
/// </summary>
public class RecurringExpenseConfiguration : IEntityTypeConfiguration<RecurringExpense>
{
    public void Configure(EntityTypeBuilder<RecurringExpense> builder)
    {
        builder.ToTable("RecurringExpenses");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.IsEstimatedAmount)
            .HasDefaultValue(false);

        builder.Property(e => e.Frequency)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(RecurrenceFrequency.Monthly);

        builder.Property(e => e.StartDate)
            .IsRequired();

        builder.Property(e => e.DayOfMonth)
            .HasDefaultValue(1);

        builder.Property(e => e.ReminderDaysBefore)
            .HasDefaultValue(3);

        builder.Property(e => e.AutoApprove)
            .HasDefaultValue(false);

        builder.Property(e => e.AutoGenerate)
            .HasDefaultValue(true);

        builder.Property(e => e.OccurrenceCount)
            .HasDefaultValue(0);

        builder.Property(e => e.Notes)
            .HasMaxLength(1000);

        // Ignore computed properties
        builder.Ignore(e => e.IsDue);
        builder.Ignore(e => e.IsUpcoming);

        // Indexes
        builder.HasIndex(e => e.NextDueDate);
        builder.HasIndex(e => e.Frequency);
        builder.HasIndex(e => e.IsActive);

        // Relationships
        builder.HasOne(e => e.ExpenseCategory)
            .WithMany(c => c.RecurringExpenses)
            .HasForeignKey(e => e.ExpenseCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Supplier)
            .WithMany(s => s.RecurringExpenses)
            .HasForeignKey(e => e.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.PaymentMethod)
            .WithMany()
            .HasForeignKey(e => e.PaymentMethodId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>
/// EF Core configuration for ExpenseBudget entity.
/// </summary>
public class ExpenseBudgetConfiguration : IEntityTypeConfiguration<ExpenseBudget>
{
    public void Configure(EntityTypeBuilder<ExpenseBudget> builder)
    {
        builder.ToTable("ExpenseBudgets");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.Period)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(BudgetPeriod.Monthly);

        builder.Property(e => e.Year)
            .IsRequired();

        builder.Property(e => e.StartDate)
            .IsRequired();

        builder.Property(e => e.EndDate)
            .IsRequired();

        builder.Property(e => e.AlertThreshold)
            .HasDefaultValue(80);

        builder.Property(e => e.AlertSent)
            .HasDefaultValue(false);

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        builder.Property(e => e.SpentAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        // Ignore computed properties
        builder.Ignore(e => e.RemainingAmount);
        builder.Ignore(e => e.UtilizationPercentage);
        builder.Ignore(e => e.IsOverThreshold);
        builder.Ignore(e => e.IsExceeded);
        builder.Ignore(e => e.IsCurrent);

        // Indexes
        builder.HasIndex(e => new { e.Year, e.Month });
        builder.HasIndex(e => e.ExpenseCategoryId);
        builder.HasIndex(e => new { e.StartDate, e.EndDate });

        // Relationships
        builder.HasOne(e => e.ExpenseCategory)
            .WithMany(c => c.Budgets)
            .HasForeignKey(e => e.ExpenseCategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// EF Core configuration for ExpenseAttachment entity.
/// </summary>
public class ExpenseAttachmentConfiguration : IEntityTypeConfiguration<ExpenseAttachment>
{
    public void Configure(EntityTypeBuilder<ExpenseAttachment> builder)
    {
        builder.ToTable("ExpenseAttachments");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.FilePath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.FileType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.FileSize)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.UploadedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(e => e.ExpenseId);

        // Relationships
        builder.HasOne(e => e.Expense)
            .WithMany(exp => exp.Attachments)
            .HasForeignKey(e => e.ExpenseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.UploadedByUser)
            .WithMany()
            .HasForeignKey(e => e.UploadedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
