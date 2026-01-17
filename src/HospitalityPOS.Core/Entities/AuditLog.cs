namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents an audit log entry for tracking user actions.
/// Uses BIGINT for Id to support high volume of audit entries.
/// </summary>
public class AuditLog
{
    public long Id { get; set; }
    public int? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public int? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public string? MachineName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User? User { get; set; }
}
