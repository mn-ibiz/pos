namespace HospitalityPOS.Core.DTOs;

/// <summary>
/// Data transfer object for creating a new user.
/// </summary>
public class CreateUserDto
{
    /// <summary>
    /// Gets or sets the username (required, unique).
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password (required for new users).
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the full name (required).
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address (optional).
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the phone number (optional).
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Gets or sets the PIN for quick login (optional, 4-6 digits).
    /// </summary>
    public string? PIN { get; set; }

    /// <summary>
    /// Gets or sets the role IDs to assign to the user.
    /// </summary>
    public List<int> RoleIds { get; set; } = [];

    /// <summary>
    /// Gets or sets whether the user is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
