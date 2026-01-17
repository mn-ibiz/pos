using System.Globalization;
using System.Windows;
using System.Windows.Data;
using HospitalityPOS.Core.Interfaces;

namespace HospitalityPOS.WPF.Converters;

/// <summary>
/// Converter that returns Visibility.Visible if the current user has the specified permission,
/// Visibility.Collapsed otherwise.
/// </summary>
public class PermissionToVisibilityConverter : IValueConverter
{
    private static IAuthorizationService? _authService;

    private static IAuthorizationService GetAuthService()
    {
        return _authService ??= App.Services.GetRequiredService<IAuthorizationService>();
    }

    /// <summary>
    /// Converts a permission name to Visibility.
    /// </summary>
    /// <param name="value">Not used - can be null.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">The permission name (string).</param>
    /// <param name="culture">The culture info.</param>
    /// <returns>Visibility.Visible if user has permission; Visibility.Collapsed otherwise.</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is not string permissionName)
        {
            return Visibility.Visible;
        }

        try
        {
            var authService = GetAuthService();
            return authService.HasPermission(permissionName)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        catch (Exception)
        {
            // If authorization service is not available, show the element
            return Visibility.Visible;
        }
    }

    /// <summary>
    /// Not supported - one-way converter.
    /// </summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("PermissionToVisibilityConverter is one-way only.");
    }
}

/// <summary>
/// Converter that returns Visibility.Visible if the current user has any of the specified permissions,
/// Visibility.Collapsed otherwise.
/// </summary>
public class AnyPermissionToVisibilityConverter : IValueConverter
{
    private static IAuthorizationService? _authService;

    private static IAuthorizationService GetAuthService()
    {
        return _authService ??= App.Services.GetRequiredService<IAuthorizationService>();
    }

    /// <summary>
    /// Converts permission names to Visibility.
    /// </summary>
    /// <param name="value">Not used - can be null.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">Comma-separated permission names (string).</param>
    /// <param name="culture">The culture info.</param>
    /// <returns>Visibility.Visible if user has any permission; Visibility.Collapsed otherwise.</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is not string permissionNames)
        {
            return Visibility.Visible;
        }

        try
        {
            var permissions = permissionNames.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var authService = GetAuthService();
            return authService.HasAnyPermission(permissions)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        catch (Exception)
        {
            return Visibility.Visible;
        }
    }

    /// <summary>
    /// Not supported - one-way converter.
    /// </summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("AnyPermissionToVisibilityConverter is one-way only.");
    }
}

/// <summary>
/// Converter that returns true if the current user has the specified permission,
/// false otherwise. Use for IsEnabled binding.
/// </summary>
public class PermissionToEnabledConverter : IValueConverter
{
    private static IAuthorizationService? _authService;

    private static IAuthorizationService GetAuthService()
    {
        return _authService ??= App.Services.GetRequiredService<IAuthorizationService>();
    }

    /// <summary>
    /// Converts a permission name to boolean.
    /// </summary>
    /// <param name="value">Not used - can be null.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">The permission name (string).</param>
    /// <param name="culture">The culture info.</param>
    /// <returns>True if user has permission; false otherwise.</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is not string permissionName)
        {
            return true;
        }

        try
        {
            var authService = GetAuthService();
            return authService.HasPermission(permissionName);
        }
        catch (Exception)
        {
            return true;
        }
    }

    /// <summary>
    /// Not supported - one-way converter.
    /// </summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("PermissionToEnabledConverter is one-way only.");
    }
}

/// <summary>
/// Multi-value converter that combines a bound IsEnabled value with permission check.
/// Use when you need both data binding and permission check for IsEnabled.
/// Values[0]: The bound IsEnabled value (bool)
/// ConverterParameter: The permission name (string)
/// </summary>
public class PermissionAndBindingEnabledConverter : IMultiValueConverter
{
    private static IAuthorizationService? _authService;

    private static IAuthorizationService GetAuthService()
    {
        return _authService ??= App.Services.GetRequiredService<IAuthorizationService>();
    }

    /// <summary>
    /// Combines bound enabled value with permission check.
    /// </summary>
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        // First value is the bound IsEnabled
        var boundEnabled = values.Length > 0 && values[0] is true;

        // Parameter is the permission name
        if (parameter is not string permissionName)
        {
            return boundEnabled;
        }

        try
        {
            var authService = GetAuthService();
            var hasPermission = authService.HasPermission(permissionName);
            return boundEnabled && hasPermission;
        }
        catch (Exception)
        {
            return boundEnabled;
        }
    }

    /// <summary>
    /// Not supported - one-way converter.
    /// </summary>
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException("PermissionAndBindingEnabledConverter is one-way only.");
    }
}
