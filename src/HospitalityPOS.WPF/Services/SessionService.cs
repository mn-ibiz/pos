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
    private readonly object _lock = new();
    private User? _currentUser;
    private DateTime? _loginTime;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public SessionService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

        _logger.Information("User session started: {Username} (ID: {UserId})", user.Username, user.Id);
        UserLoggedIn?.Invoke(this, new SessionEventArgs(user));
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

        if (previousUser is not null)
        {
            _logger.Information("User session ended: {Username} (Reason: {Reason})", previousUser.Username, reason);
            UserLoggedOut?.Invoke(this, new SessionEventArgs(previousUser, reason));
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
