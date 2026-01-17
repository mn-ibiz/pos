using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a stock movement transaction (in or out).
/// </summary>
public class StockMovement : BaseEntity
{
    public int ProductId { get; set; }
    public MovementType MovementType { get; set; }
    public decimal Quantity { get; set; }
    public decimal PreviousStock { get; set; }
    public decimal NewStock { get; set; }
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the adjustment reason ID (for adjustment movements only).
    /// </summary>
    public int? AdjustmentReasonId { get; set; }

    // Navigation properties
    public virtual Product Product { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual AdjustmentReason? AdjustmentReason { get; set; }
}
