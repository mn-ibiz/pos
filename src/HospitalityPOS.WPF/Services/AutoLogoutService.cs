using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Extensions.Configuration;
using Serilog;
using HospitalityPOS.Core.Interfaces;
using HospitalityPOS.Core.Models;
using HospitalityPOS.WPF.ViewModels;

namespace HospitalityPOS.WPF.Services;

/// <summary>
/// Service for automatic user logout based on inactivity or transaction completion.
/// </summary>
public class AutoLogoutService : IAutoLogoutService, IDisposable
{
    private readonly ISessionService _sessionService;
    private readonly INavigationService _navigationService;
    private readonly ILogger _logger;
    private readonly IConfiguration _configuration;

    private readonly DispatcherTimer _inactivityCheckTimer;
    private readonly DispatcherTimer _warningCountdownTimer;

    private DateTime _lastActivityTime;
    private bool _isMonitoring;
    private bool _isWarningActive;
    private int _warningSecondsRemaining;
    private bool _disposed;

    private AutoLogoutSettings _settings = new();

    /// <inheritdoc />
    public AutoLogoutSettings Settings => _settings;

    /// <inheritdoc />
    public TimeSpan TimeUntilLogout
    {
        get
        {
            if (!_isMonitoring || !_sessionService.IsLoggedIn)
                return TimeSpan.Zero;

            var elapsed = DateTime.UtcNow - _lastActivityTime;
            var timeout = TimeSpan.FromMinutes(_settings.InactivityTimeoutMinutes);
            var remaining = timeout - elapsed;

            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }

    /// <inheritdoc />
    public bool IsWarningActive => _isWarningActive;

    /// <inheritdoc />
    public event EventHandler<TimeoutWarningEventArgs>? TimeoutWarning;

    /// <inheritdoc />
    public event EventHandler<int>? WarningCountdownTick;

    /// <inheritdoc />
    public event EventHandler? WarningCancelled;

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoLogoutService"/> class.
    /// </summary>
    /// <param name="sessionService">The session service.</param>
    /// <param name="navigationService">The navigation service.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="configuration">The configuration.</param>
    public AutoLogoutService(
        ISessionService sessionService,
        INavigationService navigationService,
        ILogger logger,
        IConfiguration configuration)
    {
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        // Initialize timers
        _inactivityCheckTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(10)
        };
        _inactivityCheckTimer.Tick += OnInactivityCheck;

        _warningCountdownTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _warningCountdownTimer.Tick += OnWarningCountdownTick;

        // Subscribe to session events
        _sessionService.UserLoggedIn += OnUserLoggedIn;
        _sessionService.UserLoggedOut += OnUserLoggedOut;

        // Load settings
        _ = LoadSettingsAsync();
    }

    /// <inheritdoc />
    public async Task LoadSettingsAsync()
    {
        try
        {
            // Load from configuration
            var section = _configuration.GetSection("AutoLogout");
            if (section.Exists())
            {
                _settings = new AutoLogoutSettings
                {
                    EnableAutoLogout = section.GetValue("EnableAutoLogout", true),
                    LogoutAfterTransaction = section.GetValue("LogoutAfterTransaction", true),
                    LogoutAfterInactivity = section.GetValue("LogoutAfterInactivity", true),
                    InactivityTimeoutMinutes = section.GetValue("InactivityTimeoutMinutes", 5),
                    WarningBeforeLogoutSeconds = section.GetValue("WarningBeforeLogoutSeconds", 30),
                    ShowTimeoutWarning = section.GetValue("ShowTimeoutWarning", true),
                    AllowStayLoggedIn = section.GetValue("AllowStayLoggedIn", true),
                    EnforceOwnTicketsOnly = section.GetValue("EnforceOwnTicketsOnly", true),
                    RequirePinForVoidDiscount = section.GetValue("RequirePinForVoidDiscount", true)
                };
            }

            _logger.Information("Auto-logout settings loaded: Enabled={Enabled}, InactivityTimeout={Timeout}min",
                _settings.EnableAutoLogout, _settings.InactivityTimeoutMinutes);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load auto-logout settings, using defaults");
            _settings = new AutoLogoutSettings();
        }

        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task UpdateSettingsAsync(AutoLogoutSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _settings = settings;
        _logger.Information("Auto-logout settings updated: Enabled={Enabled}, InactivityTimeout={Timeout}min",
            _settings.EnableAutoLogout, _settings.InactivityTimeoutMinutes);

        // TODO: Persist settings to database when SystemSettings entity is available

        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task OnPaymentProcessedAsync()
    {
        if (!_settings.EnableAutoLogout || !_settings.LogoutAfterTransaction)
        {
            return;
        }

        _logger.Information("Payment processed, triggering auto-logout after transaction");

        // Small delay to allow receipt printing (SambaPOS pattern)
        await Task.Delay(TimeSpan.FromSeconds(2));

        await LogoutNowAsync();
    }

    /// <inheritdoc />
    public void ResetInactivityTimer()
    {
        _lastActivityTime = DateTime.UtcNow;

        // Cancel any active warning
        if (_isWarningActive)
        {
            CancelWarning();
        }
    }

    /// <inheritdoc />
    public void StartMonitoring()
    {
        if (_isMonitoring || !_settings.EnableAutoLogout || !_settings.LogoutAfterInactivity)
        {
            return;
        }

        _lastActivityTime = DateTime.UtcNow;
        _isMonitoring = true;
        _inactivityCheckTimer.Start();

        // Hook into WPF input events
        HookInputEvents();

        _logger.Debug("Auto-logout monitoring started");
    }

    /// <inheritdoc />
    public void StopMonitoring()
    {
        if (!_isMonitoring)
        {
            return;
        }

        _isMonitoring = false;
        _inactivityCheckTimer.Stop();
        CancelWarning();

        // Unhook from WPF input events
        UnhookInputEvents();

        _logger.Debug("Auto-logout monitoring stopped");
    }

    /// <inheritdoc />
    public void StayLoggedIn()
    {
        if (_isWarningActive)
        {
            CancelWarning();
            ResetInactivityTimer();
            _logger.Information("User chose to stay logged in");
        }
    }

    /// <inheritdoc />
    public async Task LogoutNowAsync()
    {
        StopMonitoring();

        // Cast to SessionService to call ClearSession with reason
        if (_sessionService is SessionService sessionService)
        {
            sessionService.ClearSession(LogoutReason.InactivityTimeout);
        }
        else
        {
            _sessionService.ClearSession();
        }

        // Navigate to login screen
        _navigationService.ClearHistory();
        _navigationService.NavigateTo<LoginViewModel>();

        _logger.Information("User logged out due to auto-logout");

        await Task.CompletedTask;
    }

    private void OnUserLoggedIn(object? sender, SessionEventArgs e)
    {
        if (_settings.EnableAutoLogout && _settings.LogoutAfterInactivity)
        {
            StartMonitoring();
        }
    }

    private void OnUserLoggedOut(object? sender, SessionEventArgs e)
    {
        StopMonitoring();
    }

    private void OnInactivityCheck(object? sender, EventArgs e)
    {
        if (!_sessionService.IsLoggedIn || !_settings.EnableAutoLogout || !_settings.LogoutAfterInactivity)
        {
            return;
        }

        var elapsed = DateTime.UtcNow - _lastActivityTime;
        var timeout = TimeSpan.FromMinutes(_settings.InactivityTimeoutMinutes);
        var warningTime = TimeSpan.FromSeconds(_settings.WarningBeforeLogoutSeconds);
        var warningThreshold = timeout - warningTime;

        if (elapsed >= timeout)
        {
            // Time's up, logout now
            _ = LogoutNowAsync();
        }
        else if (elapsed >= warningThreshold && !_isWarningActive && _settings.ShowTimeoutWarning)
        {
            // Start warning countdown
            StartWarning();
        }
    }

    private void OnWarningCountdownTick(object? sender, EventArgs e)
    {
        _warningSecondsRemaining--;

        if (_warningSecondsRemaining <= 0)
        {
            // Warning countdown complete, logout
            _ = LogoutNowAsync();
        }
        else
        {
            WarningCountdownTick?.Invoke(this, _warningSecondsRemaining);
        }
    }

    private void StartWarning()
    {
        _isWarningActive = true;
        _warningSecondsRemaining = _settings.WarningBeforeLogoutSeconds;
        _warningCountdownTimer.Start();

        TimeoutWarning?.Invoke(this, new TimeoutWarningEventArgs(_warningSecondsRemaining));
        _logger.Debug("Timeout warning started, {Seconds} seconds remaining", _warningSecondsRemaining);
    }

    private void CancelWarning()
    {
        if (!_isWarningActive)
        {
            return;
        }

        _isWarningActive = false;
        _warningCountdownTimer.Stop();
        WarningCancelled?.Invoke(this, EventArgs.Empty);
        _logger.Debug("Timeout warning cancelled");
    }

    private void HookInputEvents()
    {
        if (Application.Current?.MainWindow is null)
        {
            return;
        }

        InputManager.Current.PreProcessInput += OnPreProcessInput;
    }

    private void UnhookInputEvents()
    {
        InputManager.Current.PreProcessInput -= OnPreProcessInput;
    }

    private void OnPreProcessInput(object sender, PreProcessInputEventArgs e)
    {
        // Reset on any keyboard, mouse, or touch input
        if (e.StagingItem.Input is KeyboardEventArgs ||
            e.StagingItem.Input is MouseEventArgs ||
            e.StagingItem.Input is TouchEventArgs)
        {
            ResetInactivityTimer();
        }
    }

    /// <summary>
    /// Disposes of the service resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes of managed resources.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                StopMonitoring();

                _inactivityCheckTimer.Stop();
                _warningCountdownTimer.Stop();

                _sessionService.UserLoggedIn -= OnUserLoggedIn;
                _sessionService.UserLoggedOut -= OnUserLoggedOut;
            }

            _disposed = true;
        }
    }
}
