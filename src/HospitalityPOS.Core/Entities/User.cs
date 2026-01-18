namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a system user who can log in and perform operations.
/// </summary>
public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    /// <summary>
    /// Hashed PIN for quick access authentication. MUST be hashed using BCrypt before storage.
    /// </summary>
    public string? PINHash { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? PinHash { get => PINHash; set => PINHash = value; }
    public string FullName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }

    /// <summary>
    /// Number of consecutive failed login attempts.
    /// </summary>
    public int FailedLoginAttempts { get; set; }

    /// <summary>
    /// UTC time when the account lockout expires. Null if account is not locked.
    /// </summary>
    public DateTime? LockoutEnd { get; set; }

    /// <summary>
    /// UTC time of the user's last successful login.
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Indicates whether the user must change their password on next login.
    /// Set to true when password is reset by an administrator.
    /// </summary>
    public bool MustChangePassword { get; set; }

    /// <summary>
    /// UTC time when the password was last changed.
    /// </summary>
    public DateTime? PasswordChangedAt { get; set; }

    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    public virtual ICollection<WorkPeriod> OpenedWorkPeriods { get; set; } = new List<WorkPeriod>();
    public virtual ICollection<WorkPeriod> ClosedWorkPeriods { get; set; } = new List<WorkPeriod>();
    public virtual ICollection<Receipt> OwnedReceipts { get; set; } = new List<Receipt>();
    public virtual ICollection<Receipt> VoidedReceipts { get; set; } = new List<Receipt>();
    public virtual ICollection<Receipt> SettledReceipts { get; set; } = new List<Receipt>();
    public virtual ICollection<Payment> ProcessedPayments { get; set; } = new List<Payment>();
    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
