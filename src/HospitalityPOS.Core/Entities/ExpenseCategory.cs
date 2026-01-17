namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a category for expenses.
/// </summary>
public class ExpenseCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ParentCategoryId { get; set; }

    // Navigation properties
    public virtual ExpenseCategory? ParentCategory { get; set; }
    public virtual ICollection<ExpenseCategory> SubCategories { get; set; } = new List<ExpenseCategory>();
    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
