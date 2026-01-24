using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a work period (shift) during which sales transactions occur.
/// </summary>
public class WorkPeriod : BaseEntity
{
    public DateTime OpenedAt { get; set; }
    public DateTime StartTime { get => OpenedAt; set => OpenedAt = value; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? EndTime { get => ClosedAt; set => ClosedAt = value; }
    public int OpenedByUserId { get; set; }
    public int? ClosedByUserId { get; set; }
    public decimal OpeningFloat { get; set; }
    public decimal? ClosingCash { get; set; }
    public decimal? ExpectedCash { get; set; }
    public decimal? Variance { get; set; }
    public int? ZReportNumber { get; set; }
    public WorkPeriodStatus Status { get; set; } = WorkPeriodStatus.Open;
    public string? Notes { get; set; }

    // Navigation properties
    public virtual User OpenedByUser { get; set; } = null!;
    public virtual User? ClosedByUser { get; set; }
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    public virtual ICollection<Receipt> Receipts { get; set; } = new List<Receipt>();
    public virtual ICollection<CashDenominationCount> CashDenominationCounts { get; set; } = new List<CashDenominationCount>();
}
