using System.ComponentModel.DataAnnotations.Schema;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a recurring expense template that automatically generates
/// expense entries on a scheduled basis (e.g., monthly utilities, rent).
/// </summary>
public class RecurringExpense : BaseEntity
{
    /// <summary>
    /// Gets or sets the expense category ID.
    /// </summary>
    public int ExpenseCategoryId { get; set; }

    /// <summary>
    /// Gets or sets the supplier/vendor ID.
    /// </summary>
    public int? SupplierId { get; set; }

    /// <summary>
    /// Gets or sets the payment method ID.
    /// </summary>
    public int? PaymentMethodId { get; set; }

    /// <summary>
    /// Gets or sets the recurring expense name/title.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expense description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expected amount for each occurrence.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets whether the amount can vary (estimated vs fixed).
    /// </summary>
    public bool IsEstimatedAmount { get; set; }

    /// <summary>
    /// Gets or sets the recurrence frequency.
    /// </summary>
    public RecurrenceFrequency Frequency { get; set; } = RecurrenceFrequency.Monthly;

    /// <summary>
    /// Gets or sets the start date for the recurring expense.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date for the recurring expense (null for indefinite).
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Gets or sets the next scheduled due date.
    /// </summary>
    public DateTime? NextDueDate { get; set; }

    /// <summary>
    /// Gets or sets the day of the month for monthly recurrence (1-31).
    /// </summary>
    public int DayOfMonth { get; set; } = 1;

    /// <summary>
    /// Gets or sets the day of the week for weekly recurrence (0=Sunday, 6=Saturday).
    /// </summary>
    public int? DayOfWeek { get; set; }

    /// <summary>
    /// Gets or sets the number of days before due date to generate reminder.
    /// </summary>
    public int ReminderDaysBefore { get; set; } = 3;

    /// <summary>
    /// Gets or sets whether to automatically approve generated expenses.
    /// </summary>
    public bool AutoApprove { get; set; }

    /// <summary>
    /// Gets or sets whether to automatically generate expenses or require manual trigger.
    /// </summary>
    public bool AutoGenerate { get; set; } = true;

    /// <summary>
    /// Gets or sets the date when the last expense was generated.
    /// </summary>
    public DateTime? LastGeneratedDate { get; set; }

    /// <summary>
    /// Gets or sets the total number of occurrences generated so far.
    /// </summary>
    public int OccurrenceCount { get; set; }

    /// <summary>
    /// Gets or sets additional notes.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets whether this recurring expense is currently due.
    /// </summary>
    [NotMapped]
    public bool IsDue => IsActive && NextDueDate.HasValue && NextDueDate.Value.Date <= DateTime.Today;

    /// <summary>
    /// Gets whether this recurring expense is upcoming (within reminder period).
    /// </summary>
    [NotMapped]
    public bool IsUpcoming => IsActive && NextDueDate.HasValue &&
                              NextDueDate.Value.Date <= DateTime.Today.AddDays(ReminderDaysBefore);

    // Navigation properties

    /// <summary>
    /// Gets or sets the expense category.
    /// </summary>
    public virtual ExpenseCategory ExpenseCategory { get; set; } = null!;

    /// <summary>
    /// Gets or sets the supplier/vendor.
    /// </summary>
    public virtual Supplier? Supplier { get; set; }

    /// <summary>
    /// Gets or sets the payment method.
    /// </summary>
    public virtual PaymentMethod? PaymentMethod { get; set; }

    /// <summary>
    /// Gets or sets the expenses generated from this template.
    /// </summary>
    public virtual ICollection<Expense> GeneratedExpenses { get; set; } = new List<Expense>();
}
