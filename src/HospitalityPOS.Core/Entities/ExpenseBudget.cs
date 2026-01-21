using System.ComponentModel.DataAnnotations.Schema;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a budget allocation for expense tracking and control.
/// Budgets can be set for specific categories or overall spending.
/// </summary>
public class ExpenseBudget : BaseEntity
{
    /// <summary>
    /// Gets or sets the expense category ID (null for overall budget).
    /// </summary>
    public int? ExpenseCategoryId { get; set; }

    /// <summary>
    /// Gets or sets the budget name/title.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the budgeted amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the budget period type.
    /// </summary>
    public BudgetPeriod Period { get; set; } = BudgetPeriod.Monthly;

    /// <summary>
    /// Gets or sets the year for the budget.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Gets or sets the month for monthly budgets (1-12, null for annual).
    /// </summary>
    public int? Month { get; set; }

    /// <summary>
    /// Gets or sets the quarter for quarterly budgets (1-4, null for non-quarterly).
    /// </summary>
    public int? Quarter { get; set; }

    /// <summary>
    /// Gets or sets the budget period start date.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Gets or sets the budget period end date.
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Gets or sets the alert threshold percentage (0-100).
    /// Notifications are sent when spending reaches this percentage.
    /// </summary>
    public int AlertThreshold { get; set; } = 80;

    /// <summary>
    /// Gets or sets whether alerts have been sent for this budget period.
    /// </summary>
    public bool AlertSent { get; set; }

    /// <summary>
    /// Gets or sets additional notes.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the actual spent amount (cached for performance).
    /// This should be updated when expenses are added/modified.
    /// </summary>
    public decimal SpentAmount { get; set; }

    /// <summary>
    /// Gets or sets when the spent amount was last calculated.
    /// </summary>
    public DateTime? LastCalculatedAt { get; set; }

    /// <summary>
    /// Gets the remaining budget amount.
    /// </summary>
    [NotMapped]
    public decimal RemainingAmount => Amount - SpentAmount;

    /// <summary>
    /// Gets the budget utilization percentage.
    /// </summary>
    [NotMapped]
    public decimal UtilizationPercentage => Amount > 0 ? (SpentAmount / Amount) * 100 : 0;

    /// <summary>
    /// Gets whether the budget is over the alert threshold.
    /// </summary>
    [NotMapped]
    public bool IsOverThreshold => UtilizationPercentage >= AlertThreshold;

    /// <summary>
    /// Gets whether the budget has been exceeded.
    /// </summary>
    [NotMapped]
    public bool IsExceeded => SpentAmount > Amount;

    /// <summary>
    /// Gets whether this budget period is currently active.
    /// </summary>
    [NotMapped]
    public bool IsCurrent => DateTime.Today >= StartDate && DateTime.Today <= EndDate;

    // Navigation properties

    /// <summary>
    /// Gets or sets the expense category (null for overall budget).
    /// </summary>
    public virtual ExpenseCategory? ExpenseCategory { get; set; }
}
