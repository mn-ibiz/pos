using Serilog;
using HospitalityPOS.Core.Enums;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.Infrastructure.Services;

/// <summary>
/// Provides terminal session context throughout the application lifecycle.
/// </summary>
public class TerminalSessionContext : ITerminalSessionContext
{
    private readonly ILogger _logger;
    private readonly ITerminalConfigurationService _configurationService;
    private readonly object _stateLock = new();

    private int _terminalId;
    private string _terminalCode = string.Empty;
    private string _terminalName = string.Empty;
    private int _storeId;
    private string _storeName = string.Empty;
    private TerminalType _terminalType;
    private BusinessMode _businessMode;
    private bool _isMainRegister;
    private int? _currentUserId;
    private string? _currentUserName;
    private int? _currentWorkPeriodId;
    private bool _isInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalSessionContext"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationService">The terminal configuration service.</param>
    public TerminalSessionContext(
        ILogger logger,
        ITerminalConfigurationService configurationService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
    }

    /// <inheritdoc />
    public int TerminalId
    {
        get
        {
            lock (_stateLock)
            {
                return _terminalId;
            }
        }
    }

    /// <inheritdoc />
    public string TerminalCode
    {
        get
        {
            lock (_stateLock)
            {
                return _terminalCode;
            }
        }
    }

    /// <inheritdoc />
    public string TerminalName
    {
        get
        {
            lock (_stateLock)
            {
                return _terminalName;
            }
        }
    }

    /// <inheritdoc />
    public int StoreId
    {
        get
        {
            lock (_stateLock)
            {
                return _storeId;
            }
        }
    }

    /// <inheritdoc />
    public string StoreName
    {
        get
        {
            lock (_stateLock)
            {
                return _storeName;
            }
        }
    }

    /// <inheritdoc />
    public TerminalType TerminalType
    {
        get
        {
            lock (_stateLock)
            {
                return _terminalType;
            }
        }
    }

    /// <inheritdoc />
    public BusinessMode BusinessMode
    {
        get
        {
            lock (_stateLock)
            {
                return _businessMode;
            }
        }
    }

    /// <inheritdoc />
    public int? CurrentUserId
    {
        get
        {
            lock (_stateLock)
            {
                return _currentUserId;
            }
        }
    }

    /// <inheritdoc />
    public string? CurrentUserName
    {
        get
        {
            lock (_stateLock)
            {
                return _currentUserName;
            }
        }
    }

    /// <inheritdoc />
    public int? CurrentWorkPeriodId
    {
        get
        {
            lock (_stateLock)
            {
                return _currentWorkPeriodId;
            }
        }
    }

    /// <inheritdoc />
    public bool IsInitialized
    {
        get
        {
            lock (_stateLock)
            {
                return _isInitialized;
            }
        }
    }

    /// <inheritdoc />
    public bool IsUserLoggedIn
    {
        get
        {
            lock (_stateLock)
            {
                return _currentUserId.HasValue;
            }
        }
    }

    /// <inheritdoc />
    public bool IsWorkPeriodOpen
    {
        get
        {
            lock (_stateLock)
            {
                return _currentWorkPeriodId.HasValue;
            }
        }
    }

    /// <inheritdoc />
    public bool IsMainRegister
    {
        get
        {
            lock (_stateLock)
            {
                return _isMainRegister;
            }
        }
    }

    /// <inheritdoc />
    public event EventHandler<TerminalSessionChangedEventArgs>? SessionChanged;

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var config = _configurationService.GetLocalConfiguration();

        if (config is null)
        {
            _logger.Warning("Cannot initialize terminal session - no local configuration found");
            return Task.CompletedTask;
        }

        lock (_stateLock)
        {
            _terminalId = config.Terminal.Id;
            _terminalCode = config.Terminal.Code;
            _terminalName = config.Terminal.Name;
            _storeId = config.Terminal.StoreId;
            _storeName = config.Terminal.StoreName;
            _terminalType = config.Terminal.Type;
            _businessMode = config.Terminal.BusinessMode;
            _isMainRegister = config.Terminal.IsMainRegister;
            _isInitialized = true;
        }

        _logger.Information(
            "Terminal session initialized: {Code} ({Name}) - Store: {StoreName} - Type: {Type}",
            _terminalCode, _terminalName, _storeName, _terminalType);

        OnSessionChanged(new TerminalSessionChangedEventArgs
        {
            ChangeType = TerminalSessionChangeType.Initialized
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void SetCurrentUser(int userId, string userName)
    {
        lock (_stateLock)
        {
            _currentUserId = userId;
            _currentUserName = userName;
        }

        _logger.Information("User logged in: {UserName} (ID: {UserId}) on terminal {TerminalCode}",
            userName, userId, _terminalCode);

        OnSessionChanged(new TerminalSessionChangedEventArgs
        {
            ChangeType = TerminalSessionChangeType.UserLoggedIn,
            UserId = userId,
            UserName = userName
        });
    }

    /// <inheritdoc />
    public void ClearCurrentUser()
    {
        int? previousUserId;
        string? previousUserName;

        lock (_stateLock)
        {
            previousUserId = _currentUserId;
            previousUserName = _currentUserName;
            _currentUserId = null;
            _currentUserName = null;
        }

        _logger.Information("User logged out: {UserName} (ID: {UserId}) from terminal {TerminalCode}",
            previousUserName, previousUserId, _terminalCode);

        OnSessionChanged(new TerminalSessionChangedEventArgs
        {
            ChangeType = TerminalSessionChangeType.UserLoggedOut,
            UserId = previousUserId,
            UserName = previousUserName
        });
    }

    /// <inheritdoc />
    public void SetWorkPeriod(int workPeriodId)
    {
        lock (_stateLock)
        {
            _currentWorkPeriodId = workPeriodId;
        }

        _logger.Information("Work period started: {WorkPeriodId} on terminal {TerminalCode}",
            workPeriodId, _terminalCode);

        OnSessionChanged(new TerminalSessionChangedEventArgs
        {
            ChangeType = TerminalSessionChangeType.WorkPeriodStarted,
            WorkPeriodId = workPeriodId
        });
    }

    /// <inheritdoc />
    public void ClearWorkPeriod()
    {
        int? previousWorkPeriodId;

        lock (_stateLock)
        {
            previousWorkPeriodId = _currentWorkPeriodId;
            _currentWorkPeriodId = null;
        }

        _logger.Information("Work period closed: {WorkPeriodId} on terminal {TerminalCode}",
            previousWorkPeriodId, _terminalCode);

        OnSessionChanged(new TerminalSessionChangedEventArgs
        {
            ChangeType = TerminalSessionChangeType.WorkPeriodClosed,
            WorkPeriodId = previousWorkPeriodId
        });
    }

    /// <inheritdoc />
    public void Reset()
    {
        lock (_stateLock)
        {
            _terminalId = 0;
            _terminalCode = string.Empty;
            _terminalName = string.Empty;
            _storeId = 0;
            _storeName = string.Empty;
            _terminalType = TerminalType.Register;
            _businessMode = BusinessMode.Supermarket;
            _isMainRegister = false;
            _currentUserId = null;
            _currentUserName = null;
            _currentWorkPeriodId = null;
            _isInitialized = false;
        }

        _logger.Information("Terminal session reset");

        OnSessionChanged(new TerminalSessionChangedEventArgs
        {
            ChangeType = TerminalSessionChangeType.Reset
        });
    }

    /// <summary>
    /// Raises the SessionChanged event.
    /// </summary>
    /// <param name="args">The event arguments.</param>
    protected virtual void OnSessionChanged(TerminalSessionChangedEventArgs args)
    {
        SessionChanged?.Invoke(this, args);
    }
}
