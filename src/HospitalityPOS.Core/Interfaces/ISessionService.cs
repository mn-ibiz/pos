using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for managing user session state.
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Gets the currently logged in user.
    /// </summary>
    User? CurrentUser { get; }

    /// <summary>
    /// Gets the time when the current user logged in.
    /// </summary>
    DateTime? LoginTime { get; }

    /// <summary>
    /// Gets a value indicating whether a user is currently logged in.
    /// </summary>
    bool IsLoggedIn { get; }

    /// <summary>
    /// Gets the current user's ID, or 0 if not logged in.
    /// </summary>
    int CurrentUserId { get; }

    /// <summary>
    /// Gets the current user's display name, or empty string if not logged in.
    /// </summary>
    string CurrentUserDisplayName { get; }

    /// <summary>
    /// Sets the current user for the session after successful login.
    /// </summary>
    /// <param name="user">The authenticated user.</param>
    void SetCurrentUser(User user);

    /// <summary>
    /// Clears the current session (logout) with user-initiated reason.
    /// </summary>
    void ClearSession();

    /// <summary>
    /// Clears the current session (logout) with a specific reason.
    /// </summary>
    /// <param name="reason">The reason for clearing the session.</param>
    void ClearSession(LogoutReason reason);

    /// <summary>
    /// Checks if the current session has expired based on timeout settings.
    /// </summary>
    /// <param name="timeoutMinutes">The session timeout in minutes.</param>
    /// <returns>True if the session has expired; otherwise, false.</returns>
    bool IsSessionExpired(int timeoutMinutes = 30);

    /// <summary>
    /// Checks if the current user has a specific permission.
    /// </summary>
    /// <param name="permissionName">The permission name to check.</param>
    /// <returns>True if the user has the permission; otherwise, false.</returns>
    bool HasPermission(string permissionName);

    /// <summary>
    /// Checks if the current user has a specific role.
    /// </summary>
    /// <param name="roleName">The role name to check.</param>
    /// <returns>True if the user has the role; otherwise, false.</returns>
    bool HasRole(string roleName);

    /// <summary>
    /// Occurs when a user logs in.
    /// </summary>
    event EventHandler<SessionEventArgs>? UserLoggedIn;

    /// <summary>
    /// Occurs when a user logs out.
    /// </summary>
    event EventHandler<SessionEventArgs>? UserLoggedOut;
}

/// <summary>
/// Event arguments for session events.
/// </summary>
public class SessionEventArgs : EventArgs
{
    /// <summary>
    /// Gets the user associated with the event.
    /// </summary>
    public User? User { get; }

    /// <summary>
    /// Gets the time of the event.
    /// </summary>
    public DateTime EventTime { get; }

    /// <summary>
    /// Gets the reason for logout (if applicable).
    /// </summary>
    public LogoutReason Reason { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionEventArgs"/> class for login events.
    /// </summary>
    /// <param name="user">The user who logged in.</param>
    public SessionEventArgs(User user)
    {
        User = user ?? throw new ArgumentNullException(nameof(user));
        EventTime = DateTime.UtcNow;
        Reason = LogoutReason.NotApplicable;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionEventArgs"/> class for logout events.
    /// </summary>
    /// <param name="user">The user who logged out (can be null).</param>
    /// <param name="reason">The reason for logout.</param>
    public SessionEventArgs(User? user, LogoutReason reason)
    {
        User = user;
        EventTime = DateTime.UtcNow;
        Reason = reason;
    }
}

/// <summary>
/// Specifies the reason for a user logout.
/// </summary>
public enum LogoutReason
{
    /// <summary>
    /// Not applicable (used for login events).
    /// </summary>
    NotApplicable,

    /// <summary>
    /// User explicitly logged out.
    /// </summary>
    UserInitiated,

    /// <summary>
    /// Session timed out due to inactivity.
    /// </summary>
    InactivityTimeout,

    /// <summary>
    /// Auto-logout after transaction completion.
    /// </summary>
    AfterTransaction,

    /// <summary>
    /// Application is shutting down.
    /// </summary>
    ApplicationShutdown,

    /// <summary>
    /// Account was locked due to failed attempts.
    /// </summary>
    AccountLocked
}
