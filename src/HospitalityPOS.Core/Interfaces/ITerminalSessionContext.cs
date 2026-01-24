using HospitalityPOS.Core.Enums;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Provides terminal session context throughout the application lifecycle.
/// </summary>
public interface ITerminalSessionContext
{
    /// <summary>
    /// Gets the current terminal ID from local config.
    /// </summary>
    int TerminalId { get; }

    /// <summary>
    /// Gets the current terminal code (e.g., "REG-001").
    /// </summary>
    string TerminalCode { get; }

    /// <summary>
    /// Gets the current terminal name (e.g., "Register 1").
    /// </summary>
    string TerminalName { get; }

    /// <summary>
    /// Gets the store ID from terminal configuration.
    /// </summary>
    int StoreId { get; }

    /// <summary>
    /// Gets the store name.
    /// </summary>
    string StoreName { get; }

    /// <summary>
    /// Gets the terminal type (Register, Till, Admin, etc.).
    /// </summary>
    TerminalType TerminalType { get; }

    /// <summary>
    /// Gets the business mode (Supermarket, Restaurant, Admin).
    /// </summary>
    BusinessMode BusinessMode { get; }

    /// <summary>
    /// Gets the current logged-in user ID (null if not logged in).
    /// </summary>
    int? CurrentUserId { get; }

    /// <summary>
    /// Gets the current logged-in username.
    /// </summary>
    string? CurrentUserName { get; }

    /// <summary>
    /// Gets the current active work period ID for this terminal.
    /// </summary>
    int? CurrentWorkPeriodId { get; }

    /// <summary>
    /// Gets whether the terminal is properly initialized.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Gets whether a user is currently logged in.
    /// </summary>
    bool IsUserLoggedIn { get; }

    /// <summary>
    /// Gets whether a work period is currently open.
    /// </summary>
    bool IsWorkPeriodOpen { get; }

    /// <summary>
    /// Gets whether this is the main register for the store.
    /// </summary>
    bool IsMainRegister { get; }

    /// <summary>
    /// Initializes the context from local configuration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the current user after login.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="userName">The username.</param>
    void SetCurrentUser(int userId, string userName);

    /// <summary>
    /// Clears the current user on logout.
    /// </summary>
    void ClearCurrentUser();

    /// <summary>
    /// Sets the current work period.
    /// </summary>
    /// <param name="workPeriodId">The work period ID.</param>
    void SetWorkPeriod(int workPeriodId);

    /// <summary>
    /// Clears the work period when closed.
    /// </summary>
    void ClearWorkPeriod();

    /// <summary>
    /// Resets the session context (for re-initialization).
    /// </summary>
    void Reset();

    /// <summary>
    /// Event fired when session state changes.
    /// </summary>
    event EventHandler<TerminalSessionChangedEventArgs>? SessionChanged;
}

/// <summary>
/// Event arguments for terminal session state changes.
/// </summary>
public class TerminalSessionChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the type of change.
    /// </summary>
    public TerminalSessionChangeType ChangeType { get; set; }

    /// <summary>
    /// Gets or sets the user ID (if applicable).
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Gets or sets the username (if applicable).
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Gets or sets the work period ID (if applicable).
    /// </summary>
    public int? WorkPeriodId { get; set; }

    /// <summary>
    /// Gets or sets when the change occurred.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Defines the types of terminal session changes.
/// </summary>
public enum TerminalSessionChangeType
{
    /// <summary>
    /// Terminal session initialized.
    /// </summary>
    Initialized = 1,

    /// <summary>
    /// User logged in.
    /// </summary>
    UserLoggedIn = 2,

    /// <summary>
    /// User logged out.
    /// </summary>
    UserLoggedOut = 3,

    /// <summary>
    /// Work period started.
    /// </summary>
    WorkPeriodStarted = 4,

    /// <summary>
    /// Work period closed.
    /// </summary>
    WorkPeriodClosed = 5,

    /// <summary>
    /// Session reset.
    /// </summary>
    Reset = 6
}
