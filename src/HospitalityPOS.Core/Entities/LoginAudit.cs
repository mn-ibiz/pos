namespace HospitalityPOS.Core.Entities;

/// <summary>
/// Represents a login audit record for tracking login attempts.
/// </summary>
public class LoginAudit
{
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the user ID (null if login failed with invalid username).
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Gets or sets the username that was attempted.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the login was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the failure reason (if applicable).
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// Gets or sets the IP address of the client.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the machine/device name.
    /// </summary>
    public string? MachineName { get; set; }

    /// <summary>
    /// Gets or sets additional device information.
    /// </summary>
    public string? DeviceInfo { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the login attempt.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets whether this is a logout event.
    /// </summary>
    public bool IsLogout { get; set; }

    /// <summary>
    /// Gets or sets the session duration in minutes (for logout events).
    /// </summary>
    public int? SessionDurationMinutes { get; set; }

    // Navigation properties
    public virtual User? User { get; set; }
}
