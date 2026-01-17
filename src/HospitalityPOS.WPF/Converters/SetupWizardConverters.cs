using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace HospitalityPOS.WPF.Converters;

/// <summary>
/// Converts step number to background color based on current step.
/// </summary>
public class StepToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int currentStep && parameter is string stepParam && int.TryParse(stepParam, out int targetStep))
        {
            if (currentStep > targetStep)
            {
                // Completed step - green
                return new SolidColorBrush(Color.FromRgb(16, 185, 129)); // #10B981
            }
            else if (currentStep == targetStep)
            {
                // Current step - indigo
                return new SolidColorBrush(Color.FromRgb(99, 102, 241)); // #6366F1
            }
        }

        // Future step - dark
        return new SolidColorBrush(Color.FromRgb(37, 37, 66)); // #252542
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts step number to border color based on current step.
/// </summary>
public class StepToBorderConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int currentStep && parameter is string stepParam && int.TryParse(stepParam, out int targetStep))
        {
            if (currentStep > targetStep)
            {
                // Completed step - green border
                return new SolidColorBrush(Color.FromRgb(16, 185, 129)); // #10B981
            }
            else if (currentStep == targetStep)
            {
                // Current step - indigo border
                return new SolidColorBrush(Color.FromRgb(99, 102, 241)); // #6366F1
            }
        }

        // Future step - gray border
        return new SolidColorBrush(Color.FromRgb(58, 58, 92)); // #3a3a5c
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts step number to visibility based on current step.
/// </summary>
public class StepToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int currentStep && parameter is string stepParam && int.TryParse(stepParam, out int targetStep))
        {
            return currentStep == targetStep ? Visibility.Visible : Visibility.Collapsed;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts two values to check if they are equal.
/// </summary>
public class EqualityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
            return false;

        return Equals(values[0], values[1]);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean to text based on parameter format "TrueText|FalseText".
/// </summary>
public class BoolToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue && parameter is string param)
        {
            var parts = param.Split('|');
            if (parts.Length == 2)
            {
                return boolValue ? parts[0] : parts[1];
            }
        }

        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts null to Visibility (Collapsed if null, Visible if not null).
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Inverts boolean to Visibility (Collapsed if true, Visible if false).
/// </summary>
public class BoolToVisibilityInverseConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        }

        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
