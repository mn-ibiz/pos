using Serilog;
using HospitalityPOS.Core.Entities;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.WPF.Services;

/// <summary>
/// Session service implementation for managing user session state.
/// </summary>
public class SessionService : ISessionService
{
    private readonly ILogger _logger;
    private readonly ITerminalSessionContext _terminalSession;
    private readonly IWorkPeriodSessionService _workPeriodSessionService;
    private readonly object _lock = new();
    private User? _currentUser;
    private DateTime? _loginTime;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="terminalSession">The terminal session context.</param>
    /// <param name="workPeriodSessionService">The work period session service.</param>
    public SessionService(
        ILogger logger,
        ITerminalSessionContext terminalSession,
        IWorkPeriodSessionService workPeriodSessionService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _terminalSession = terminalSession ?? throw new ArgumentNullException(nameof(terminalSession));
        _workPeriodSessionService = workPeriodSessionService ?? throw new ArgumentNullException(nameof(workPeriodSessionService));
    }

    /// <inheritdoc />
    public User? CurrentUser
    {
        get
        {
            lock (_lock)
            {
                return _currentUser;
            }
        }
    }

    /// <inheritdoc />
    public DateTime? LoginTime
    {
        get
        {
            lock (_lock)
            {
                return _loginTime;
            }
        }
    }

    /// <inheritdoc />
    public bool IsLoggedIn
    {
        get
        {
            lock (_lock)
            {
                return _currentUser is not null;
            }
        }
    }

    /// <inheritdoc />
    public int CurrentUserId
    {
        get
        {
            lock (_lock)
            {
                return _currentUser?.Id ?? 0;
            }
        }
    }

    /// <inheritdoc />
    public string CurrentUserDisplayName
    {
        get
        {
            lock (_lock)
            {
                return _currentUser?.FullName ?? string.Empty;
            }
        }
    }

    /// <inheritdoc />
    public int? CurrentStoreId
    {
        get
        {
            lock (_lock)
            {
                // In single-store mode, return default store ID (1)
                // Multi-store implementation would get this from user assignment
                return 1;
            }
        }
    }

    /// <inheritdoc />
    public event EventHandler<SessionEventArgs>? UserLoggedIn;

    /// <inheritdoc />
    public event EventHandler<SessionEventArgs>? UserLoggedOut;

    /// <inheritdoc />
    public void SetCurrentUser(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        lock (_lock)
        {
            _currentUser = user;
            _loginTime = DateTime.UtcNow;
        }

        // Update terminal session context
        _terminalSession.SetCurrentUser(user.Id, user.Username);

        // Start work period session if work period is open
        _ = StartWorkPeriodSessionAsync(user.Id);

        _logger.Information("User session started: {Username} (ID: {UserId})", user.Username, user.Id);
        UserLoggedIn?.Invoke(this, new SessionEventArgs(user));
    }

    /// <summary>
    /// Starts a work period session for the user asynchronously.
    /// </summary>
    private async Task StartWorkPeriodSessionAsync(int userId)
    {
        try
        {
            if (!_terminalSession.CurrentWorkPeriodId.HasValue || _terminalSession.TerminalId <= 0)
            {
                _logger.Debug("No active work period or terminal not initialized - skipping session start");
                return;
            }

            var session = await _workPeriodSessionService.StartSessionAsync(
                _terminalSession.CurrentWorkPeriodId.Value,
                _terminalSession.TerminalId,
                userId).ConfigureAwait(false);

            _terminalSession.SetWorkPeriodSession(session.Id, session.LoginAt);

            _logger.Information("Started work period session {SessionId} for user {UserId}",
                session.Id, userId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to start work period session for user {UserId}", userId);
        }
    }

    /// <inheritdoc />
    public void ClearSession()
    {
        ClearSession(LogoutReason.UserInitiated);
    }

    /// <summary>
    /// Clears the current session with a specific reason.
    /// </summary>
    /// <param name="reason">The reason for clearing the session.</param>
    public void ClearSession(LogoutReason reason)
    {
        User? previousUser;

        lock (_lock)
        {
            previousUser = _currentUser;
            _currentUser = null;
            _loginTime = null;
        }

        // End work period session
        _ = EndWorkPeriodSessionAsync();

        // Clear terminal session context
        _terminalSession.ClearCurrentUser();

        if (previousUser is not null)
        {
            _logger.Information("User session ended: {Username} (Reason: {Reason})", previousUser.Username, reason);
            UserLoggedOut?.Invoke(this, new SessionEventArgs(previousUser, reason));
        }
    }

    /// <summary>
    /// Ends the current work period session asynchronously.
    /// </summary>
    private async Task EndWorkPeriodSessionAsync()
    {
        try
        {
            var sessionId = _terminalSession.CurrentWorkPeriodSessionId;
            if (!sessionId.HasValue)
            {
                return;
            }

            await _workPeriodSessionService.EndSessionAsync(sessionId.Value).ConfigureAwait(false);
            _terminalSession.ClearWorkPeriodSession();

            _logger.Information("Ended work period session {SessionId}", sessionId.Value);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to end work period session");
        }
    }

    /// <inheritdoc />
    public bool IsSessionExpired(int timeoutMinutes = 30)
    {
        lock (_lock)
        {
            if (_loginTime is null || _currentUser is null)
            {
                return true;
            }

            return DateTime.UtcNow - _loginTime.Value > TimeSpan.FromMinutes(timeoutMinutes);
        }
    }

    /// <inheritdoc />
    public bool HasPermission(string permissionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionName);

        lock (_lock)
        {
            if (_currentUser is null)
            {
                return false;
            }

            // Check if any of the user's roles have the permission
            return _currentUser.UserRoles
                .SelectMany(ur => ur.Role.RolePermissions)
                .Any(rp => rp.Permission.Name.Equals(permissionName, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <inheritdoc />
    public bool HasRole(string roleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleName);

        lock (_lock)
        {
            if (_currentUser is null)
            {
                return false;
            }

            return _currentUser.UserRoles
                .Any(ur => ur.Role.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
