using ThemeMode = HospitalityPOS.Core.Enums.ThemeMode;

namespace HospitalityPOS.WPF.Services;

/// <summary>
/// Service for managing application theme (Light/Dark mode).
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Gets the current theme mode.
    /// </summary>
    ThemeMode CurrentTheme { get; }

    /// <summary>
    /// Gets a value indicating whether the current theme is dark.
    /// </summary>
    bool IsDarkTheme { get; }

    /// <summary>
    /// Sets the application theme.
    /// </summary>
    /// <param name="theme">The theme mode to apply.</param>
    void SetTheme(ThemeMode theme);

    /// <summary>
    /// Toggles between light and dark themes.
    /// </summary>
    void ToggleTheme();

    /// <summary>
    /// Loads the saved theme preference from settings.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    Task LoadSavedThemeAsync();

    /// <summary>
    /// Saves the current theme preference to settings.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    Task SaveThemePreferenceAsync();

    /// <summary>
    /// Event raised when the theme changes.
    /// </summary>
    event EventHandler<ThemeMode>? ThemeChanged;
}
