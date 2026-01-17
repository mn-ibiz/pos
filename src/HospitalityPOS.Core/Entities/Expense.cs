using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a business expense.
/// </summary>
public class Expense : BaseEntity
{
    public string ExpenseNumber { get; set; } = string.Empty;
    public int ExpenseCategoryId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentReference { get; set; }
    public int? SupplierId { get; set; }
    public string? ReceiptImagePath { get; set; }
    public ExpenseStatus Status { get; set; } = ExpenseStatus.Pending;
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }

    // Navigation properties
    public virtual ExpenseCategory ExpenseCategory { get; set; } = null!;
    public virtual Supplier? Supplier { get; set; }
    public virtual User? ApprovedByUser { get; set; }
    public virtual User? CreatedByUser { get; set; }
}
