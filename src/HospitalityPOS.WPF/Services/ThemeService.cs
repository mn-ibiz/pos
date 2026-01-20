using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;
using Serilog;
using ThemeMode = HospitalityPOS.Core.Enums.ThemeMode;

namespace HospitalityPOS.WPF.Services;

/// <summary>
/// Service for managing application theme (Light/Dark mode).
/// </summary>
public class ThemeService : IThemeService
{
    private const string LightThemePath = "Resources/Themes/LightTheme.xaml";
    private const string DarkThemePath = "Resources/Themes/DarkTheme.xaml";
    private const string SettingsFileName = "theme-settings.json";

    private static readonly string SettingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "HospitalityPOS",
        SettingsFileName);

    private ThemeMode _currentTheme = ThemeMode.Dark;
    private ThemeMode _savedThemePreference = ThemeMode.Dark;
    private ResourceDictionary? _currentThemeDictionary;

    /// <inheritdoc />
    public ThemeMode CurrentTheme => _currentTheme;

    /// <inheritdoc />
    public bool IsDarkTheme => _currentTheme == ThemeMode.Dark;

    /// <inheritdoc />
    public event EventHandler<ThemeMode>? ThemeChanged;

    public ThemeService()
    {
    }

    /// <inheritdoc />
    public void SetTheme(ThemeMode theme)
    {
        _savedThemePreference = theme;
        var effectiveTheme = theme;

        // If System mode, detect Windows theme
        if (theme == ThemeMode.System)
        {
            effectiveTheme = GetSystemTheme();
        }

        if (_currentTheme == effectiveTheme && _currentThemeDictionary != null)
        {
            return;
        }

        _currentTheme = effectiveTheme;
        ApplyTheme(effectiveTheme);
        ThemeChanged?.Invoke(this, effectiveTheme);

        Log.Information("Theme changed to {Theme} (preference: {Preference})", effectiveTheme, theme);
    }

    /// <inheritdoc />
    public void ToggleTheme()
    {
        var newTheme = _currentTheme == ThemeMode.Light ? ThemeMode.Dark : ThemeMode.Light;
        SetTheme(newTheme);
    }

    /// <inheritdoc />
    public async Task LoadSavedThemeAsync()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = await File.ReadAllTextAsync(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<ThemeSettings>(json);

                if (settings != null && Enum.IsDefined(typeof(ThemeMode), settings.ThemeMode))
                {
                    _savedThemePreference = (ThemeMode)settings.ThemeMode;
                    SetTheme(_savedThemePreference);
                    return;
                }
            }

            // Default to Dark theme
            SetTheme(ThemeMode.Dark);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to load saved theme preference, defaulting to Dark theme");
            SetTheme(ThemeMode.Dark);
        }
    }

    /// <inheritdoc />
    public async Task SaveThemePreferenceAsync()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var settings = new ThemeSettings { ThemeMode = (int)_savedThemePreference };
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(SettingsFilePath, json);

            Log.Information("Theme preference saved: {Theme}", _savedThemePreference);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save theme preference");
        }
    }

    private class ThemeSettings
    {
        public int ThemeMode { get; set; }
    }

    private void ApplyTheme(ThemeMode theme)
    {
        var app = Application.Current;
        if (app == null) return;

        var themePath = theme == ThemeMode.Dark ? DarkThemePath : LightThemePath;
        var newThemeDictionary = new ResourceDictionary
        {
            Source = new Uri(themePath, UriKind.Relative)
        };

        // Remove old theme dictionary if exists
        if (_currentThemeDictionary != null)
        {
            app.Resources.MergedDictionaries.Remove(_currentThemeDictionary);
        }

        // Add new theme dictionary at the beginning so it can be overridden by other styles if needed
        app.Resources.MergedDictionaries.Insert(0, newThemeDictionary);
        _currentThemeDictionary = newThemeDictionary;
    }

    private static ThemeMode GetSystemTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");

            if (key != null)
            {
                var value = key.GetValue("AppsUseLightTheme");
                if (value is int useLightTheme)
                {
                    return useLightTheme == 1 ? ThemeMode.Light : ThemeMode.Dark;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to detect system theme");
        }

        // Default to Dark theme if detection fails
        return ThemeMode.Dark;
    }
}
