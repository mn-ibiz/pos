using System.Globalization;
using System.Windows.Data;
using HospitalityPOS.Core.Entities;

namespace HospitalityPOS.WPF.Converters;

/// <summary>
/// Converts a decimal value to true if greater than zero.
/// </summary>
public class GreaterThanZeroConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal decimalValue)
        {
            return decimalValue > 0;
        }

        if (value is double doubleValue)
        {
            return doubleValue > 0;
        }

        if (value is int intValue)
        {
            return intValue > 0;
        }

        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a Supplier to credit usage percentage.
/// </summary>
public class CreditUsageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Supplier supplier)
        {
            if (supplier.CreditLimit == 0)
            {
                return "-"; // Unlimited credit
            }

            var percentage = (supplier.CurrentBalance / supplier.CreditLimit) * 100;
            return Math.Min(percentage, 100); // Cap at 100%
        }

        return 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts credit limit to display text.
/// </summary>
public class CreditLimitDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal creditLimit)
        {
            return creditLimit == 0 ? "Unlimited" : $"KSh {creditLimit:N0}";
        }

        return "Unlimited";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts invoice status to color.
/// </summary>
public class InvoiceStatusColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Core.Enums.InvoiceStatus status)
        {
            return status switch
            {
                Core.Enums.InvoiceStatus.Paid => "#4CAF50",
                Core.Enums.InvoiceStatus.PartiallyPaid => "#FF9800",
                Core.Enums.InvoiceStatus.Unpaid => "#2196F3",
                Core.Enums.InvoiceStatus.Overdue => "#F44336",
                _ => "#8888AA"
            };
        }

        return "#8888AA";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts invoice status to display text.
/// </summary>
public class InvoiceStatusTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Core.Enums.InvoiceStatus status)
        {
            return status switch
            {
                Core.Enums.InvoiceStatus.Paid => "Paid",
                Core.Enums.InvoiceStatus.PartiallyPaid => "Partial",
                Core.Enums.InvoiceStatus.Unpaid => "Unpaid",
                Core.Enums.InvoiceStatus.Overdue => "Overdue",
                _ => status.ToString()
            };
        }

        return value?.ToString() ?? "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
