using HospitalityPOS.Core.Models;

namespace HospitalityPOS.Core.Interfaces;

/// <summary>
/// Service interface for automatic user logout functionality.
/// </summary>
public interface IAutoLogoutService
{
    /// <summary>
    /// Gets the current auto-logout settings.
    /// </summary>
    AutoLogoutSettings Settings { get; }

    /// <summary>
    /// Gets the time remaining until auto-logout (based on inactivity).
    /// </summary>
    TimeSpan TimeUntilLogout { get; }

    /// <summary>
    /// Gets a value indicating whether the timeout warning is currently being shown.
    /// </summary>
    bool IsWarningActive { get; }

    /// <summary>
    /// Called when a payment is fully processed to trigger post-transaction logout if enabled.
    /// </summary>
    Task OnPaymentProcessedAsync();

    /// <summary>
    /// Resets the inactivity timer (called on user activity).
    /// </summary>
    void ResetInactivityTimer();

    /// <summary>
    /// Starts monitoring for inactivity.
    /// </summary>
    void StartMonitoring();

    /// <summary>
    /// Stops monitoring for inactivity.
    /// </summary>
    void StopMonitoring();

    /// <summary>
    /// Requests the user to stay logged in (cancels pending logout).
    /// </summary>
    void StayLoggedIn();

    /// <summary>
    /// Logs out the current user immediately.
    /// </summary>
    Task LogoutNowAsync();

    /// <summary>
    /// Updates the auto-logout settings.
    /// </summary>
    /// <param name="settings">The new settings.</param>
    Task UpdateSettingsAsync(AutoLogoutSettings settings);

    /// <summary>
    /// Loads the auto-logout settings from storage.
    /// </summary>
    Task LoadSettingsAsync();

    /// <summary>
    /// Occurs when the timeout warning should be shown.
    /// </summary>
    event EventHandler<TimeoutWarningEventArgs>? TimeoutWarning;

    /// <summary>
    /// Occurs when the timeout warning countdown updates.
    /// </summary>
    event EventHandler<int>? WarningCountdownTick;

    /// <summary>
    /// Occurs when the timeout warning is cancelled.
    /// </summary>
    event EventHandler? WarningCancelled;
}

/// <summary>
/// Event arguments for timeout warning events.
/// </summary>
public class TimeoutWarningEventArgs : EventArgs
{
    /// <summary>
    /// Gets the number of seconds until automatic logout.
    /// </summary>
    public int SecondsRemaining { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TimeoutWarningEventArgs"/> class.
    /// </summary>
    /// <param name="secondsRemaining">The seconds remaining until logout.</param>
    public TimeoutWarningEventArgs(int secondsRemaining)
    {
        SecondsRemaining = secondsRemaining;
    }
}
