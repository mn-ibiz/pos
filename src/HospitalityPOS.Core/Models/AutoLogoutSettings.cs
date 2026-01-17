namespace HospitalityPOS.Core.Models;

/// <summary>
/// Settings for automatic user logout behavior.
/// </summary>
public class AutoLogoutSettings
{
    /// <summary>
    /// Gets or sets whether auto-logout is enabled.
    /// </summary>
    public bool EnableAutoLogout { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to logout after each transaction completion.
    /// </summary>
    public bool LogoutAfterTransaction { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to logout after inactivity timeout.
    /// </summary>
    public bool LogoutAfterInactivity { get; set; } = true;

    /// <summary>
    /// Gets or sets the inactivity timeout in minutes.
    /// </summary>
    public int InactivityTimeoutMinutes { get; set; } = 5;

    /// <summary>
    /// Gets or sets the warning time before logout in seconds.
    /// </summary>
    public int WarningBeforeLogoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether to show a warning before timeout logout.
    /// </summary>
    public bool ShowTimeoutWarning { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to allow the user to stay logged in after warning.
    /// </summary>
    public bool AllowStayLoggedIn { get; set; } = true;

    /// <summary>
    /// Gets or sets whether waiters can only view their own tickets.
    /// </summary>
    public bool EnforceOwnTicketsOnly { get; set; } = true;

    /// <summary>
    /// Gets or sets whether PIN is required for void/discount operations.
    /// </summary>
    public bool RequirePinForVoidDiscount { get; set; } = true;

    /// <summary>
    /// Gets the available inactivity timeout options in minutes.
    /// </summary>
    public static int[] AvailableTimeoutMinutes => [1, 2, 5, 10, 15, 30];
}
