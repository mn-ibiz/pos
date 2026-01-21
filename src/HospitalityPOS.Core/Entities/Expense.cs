using System.ComponentModel.DataAnnotations.Schema;
using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a business expense with support for tracking, approval workflows,
/// and integration with business health metrics.
/// </summary>
public class Expense : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique expense number for reference.
    /// </summary>
    public string ExpenseNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expense category ID.
    /// </summary>
    public int ExpenseCategoryId { get; set; }

    /// <summary>
    /// Gets or sets the expense description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expense amount before tax.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the tax amount.
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Gets or sets the date the expense was incurred.
    /// </summary>
    public DateTime ExpenseDate { get; set; }

    /// <summary>
    /// Gets or sets the payment method used.
    /// </summary>
    public string? PaymentMethod { get; set; }

    /// <summary>
    /// Gets or sets the payment method ID for linked payment methods.
    /// </summary>
    public int? PaymentMethodId { get; set; }

    /// <summary>
    /// Gets or sets the payment reference (invoice number, check number, etc.).
    /// </summary>
    public string? PaymentReference { get; set; }

    /// <summary>
    /// Gets or sets the supplier/vendor ID.
    /// </summary>
    public int? SupplierId { get; set; }

    /// <summary>
    /// Gets or sets the path to the receipt image.
    /// </summary>
    public string? ReceiptImagePath { get; set; }

    /// <summary>
    /// Gets or sets the expense status.
    /// </summary>
    public ExpenseStatus Status { get; set; } = ExpenseStatus.Pending;

    /// <summary>
    /// Gets or sets the user ID who approved the expense.
    /// </summary>
    public int? ApprovedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the approval date and time.
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// Gets or sets whether this expense is from a recurring expense template.
    /// </summary>
    public bool IsRecurring { get; set; }

    /// <summary>
    /// Gets or sets the recurring expense template ID if applicable.
    /// </summary>
    public int? RecurringExpenseId { get; set; }

    /// <summary>
    /// Gets or sets whether this expense is tax deductible.
    /// </summary>
    public bool IsTaxDeductible { get; set; } = true;

    /// <summary>
    /// Gets or sets additional notes for the expense.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the rejection reason if expense was rejected.
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Gets or sets the date the expense was paid.
    /// </summary>
    public DateTime? PaidAt { get; set; }

    /// <summary>
    /// Gets the total expense amount including tax.
    /// </summary>
    [NotMapped]
    public decimal TotalAmount => Amount + TaxAmount;

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
    /// Gets or sets the user who approved the expense.
    /// </summary>
    public virtual User? ApprovedByUser { get; set; }

    /// <summary>
    /// Gets or sets the user who created the expense.
    /// </summary>
    public virtual User? CreatedByUser { get; set; }

    /// <summary>
    /// Gets or sets the recurring expense template.
    /// </summary>
    public virtual RecurringExpense? RecurringExpense { get; set; }

    /// <summary>
    /// Gets or sets the linked payment method entity.
    /// </summary>
    public virtual PaymentMethod? PaymentMethodEntity { get; set; }

    /// <summary>
    /// Gets or sets the expense attachments.
    /// </summary>
    public virtual ICollection<ExpenseAttachment> Attachments { get; set; } = new List<ExpenseAttachment>();
}
