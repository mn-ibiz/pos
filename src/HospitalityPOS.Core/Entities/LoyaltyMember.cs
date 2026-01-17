using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a customer enrolled in the loyalty program.
/// </summary>
public class LoyaltyMember : BaseEntity
{
    /// <summary>
    /// Customer's phone number in Kenya format (254XXXXXXXXX).
    /// Primary lookup key for customer identification.
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Customer's name (optional).
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Customer's email address (optional).
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Auto-generated membership number in format LM-YYYYMMDD-XXXXX.
    /// </summary>
    public string MembershipNumber { get; set; } = string.Empty;

    /// <summary>
    /// Current membership tier level.
    /// </summary>
    public MembershipTier Tier { get; set; } = MembershipTier.Bronze;

    /// <summary>
    /// Current redeemable points balance.
    /// </summary>
    public decimal PointsBalance { get; set; }

    /// <summary>
    /// Total points earned since enrollment.
    /// </summary>
    public decimal LifetimePoints { get; set; }

    /// <summary>
    /// Total amount spent since enrollment (KES).
    /// </summary>
    public decimal LifetimeSpend { get; set; }

    /// <summary>
    /// Date and time when the customer enrolled.
    /// </summary>
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date and time of the customer's last transaction.
    /// </summary>
    public DateTime? LastVisit { get; set; }

    /// <summary>
    /// Number of total visits/transactions.
    /// </summary>
    public int VisitCount { get; set; }

    /// <summary>
    /// Notes or comments about the customer.
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the collection of loyalty transactions for this member.
    /// </summary>
    public virtual ICollection<LoyaltyTransaction> Transactions { get; set; } = new List<LoyaltyTransaction>();

    /// <summary>
    /// Gets or sets the collection of receipts associated with this member.
    /// </summary>
    public virtual ICollection<Receipt> Receipts { get; set; } = new List<Receipt>();
}
