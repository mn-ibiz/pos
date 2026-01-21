using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a category for expenses with support for hierarchical organization
/// and Prime Cost categorization for hospitality businesses.
/// </summary>
public class ExpenseCategory : BaseEntity
{
    /// <summary>
    /// Gets or sets the category name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the category description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the parent category ID for hierarchical organization.
    /// </summary>
    public int? ParentCategoryId { get; set; }

    /// <summary>
    /// Gets or sets the expense category type for Prime Cost calculations.
    /// </summary>
    public ExpenseCategoryType Type { get; set; } = ExpenseCategoryType.Operating;

    /// <summary>
    /// Gets or sets the icon name for UI display.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets the color hex code for UI display.
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Gets or sets the sort order for display purposes.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Gets or sets whether this is a system category that cannot be deleted.
    /// </summary>
    public bool IsSystemCategory { get; set; }

    /// <summary>
    /// Gets or sets the default account ID for accounting integration.
    /// </summary>
    public int? DefaultAccountId { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the parent category for hierarchical organization.
    /// </summary>
    public virtual ExpenseCategory? ParentCategory { get; set; }

    /// <summary>
    /// Gets or sets the subcategories.
    /// </summary>
    public virtual ICollection<ExpenseCategory> SubCategories { get; set; } = new List<ExpenseCategory>();

    /// <summary>
    /// Gets or sets the expenses in this category.
    /// </summary>
    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();

    /// <summary>
    /// Gets or sets the recurring expenses in this category.
    /// </summary>
    public virtual ICollection<RecurringExpense> RecurringExpenses { get; set; } = new List<RecurringExpense>();

    /// <summary>
    /// Gets or sets the budgets for this category.
    /// </summary>
    public virtual ICollection<ExpenseBudget> Budgets { get; set; } = new List<ExpenseBudget>();

    /// <summary>
    /// Gets or sets the default chart of account for this category.
    /// </summary>
    public virtual ChartOfAccount? DefaultAccount { get; set; }
}
