using System.Globalization;
using System.Windows;
using System.Windows.Data;
using HospitalityPOS.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalityPOS.WPF.Converters;

/// <summary>
/// Converts a feature flag name to visibility based on whether the feature is enabled.
/// </summary>
public class FeatureVisibilityConverter : IValueConverter
{
    private static ISystemConfigurationService? _configService;

    private static ISystemConfigurationService ConfigService
    {
        get
        {
            if (_configService == null)
            {
                _configService = App.Services.GetService<ISystemConfigurationService>();
            }
            return _configService!;
        }
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is not string featureName)
        {
            return Visibility.Visible;
        }

        try
        {
            var config = ConfigService?.CachedConfiguration;
            if (config == null)
            {
                return Visibility.Visible;
            }

            var isEnabled = featureName switch
            {
                // Restaurant features
                nameof(config.EnableTableManagement) or "TableManagement" => config.EnableTableManagement,
                nameof(config.EnableKitchenDisplay) or "KitchenDisplay" => config.EnableKitchenDisplay,
                nameof(config.EnableWaiterAssignment) or "WaiterAssignment" => config.EnableWaiterAssignment,
                nameof(config.EnableCourseSequencing) or "CourseSequencing" => config.EnableCourseSequencing,
                nameof(config.EnableReservations) or "Reservations" => config.EnableReservations,

                // Retail features
                nameof(config.EnableBarcodeAutoFocus) or "BarcodeAutoFocus" => config.EnableBarcodeAutoFocus,
                nameof(config.EnableProductOffers) or "ProductOffers" => config.EnableProductOffers,
                nameof(config.EnableSupplierCredit) or "SupplierCredit" => config.EnableSupplierCredit,
                nameof(config.EnableLoyaltyProgram) or "LoyaltyProgram" => config.EnableLoyaltyProgram,
                nameof(config.EnableBatchExpiry) or "BatchExpiry" => config.EnableBatchExpiry,
                nameof(config.EnableScaleIntegration) or "ScaleIntegration" => config.EnableScaleIntegration,

                // Enterprise features
                nameof(config.EnablePayroll) or "Payroll" => config.EnablePayroll,
                nameof(config.EnableAccounting) or "Accounting" => config.EnableAccounting,
                nameof(config.EnableMultiStore) or "MultiStore" => config.EnableMultiStore,
                nameof(config.EnableCloudSync) or "CloudSync" => config.EnableCloudSync,

                // Kenya features
                nameof(config.EnableKenyaETims) or "KenyaETims" => config.EnableKenyaETims,
                nameof(config.EnableMpesa) or "Mpesa" => config.EnableMpesa,

                _ => true
            };

            return isEnabled ? Visibility.Visible : Visibility.Collapsed;
        }
        catch
        {
            return Visibility.Visible;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a feature flag name to enabled state based on whether the feature is enabled.
/// </summary>
public class FeatureEnabledConverter : IValueConverter
{
    private static ISystemConfigurationService? _configService;

    private static ISystemConfigurationService ConfigService
    {
        get
        {
            if (_configService == null)
            {
                _configService = App.Services.GetService<ISystemConfigurationService>();
            }
            return _configService!;
        }
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is not string featureName)
        {
            return true;
        }

        try
        {
            var config = ConfigService?.CachedConfiguration;
            if (config == null)
            {
                return true;
            }

            return featureName switch
            {
                // Restaurant features
                nameof(config.EnableTableManagement) or "TableManagement" => config.EnableTableManagement,
                nameof(config.EnableKitchenDisplay) or "KitchenDisplay" => config.EnableKitchenDisplay,
                nameof(config.EnableWaiterAssignment) or "WaiterAssignment" => config.EnableWaiterAssignment,
                nameof(config.EnableCourseSequencing) or "CourseSequencing" => config.EnableCourseSequencing,
                nameof(config.EnableReservations) or "Reservations" => config.EnableReservations,

                // Retail features
                nameof(config.EnableBarcodeAutoFocus) or "BarcodeAutoFocus" => config.EnableBarcodeAutoFocus,
                nameof(config.EnableProductOffers) or "ProductOffers" => config.EnableProductOffers,
                nameof(config.EnableSupplierCredit) or "SupplierCredit" => config.EnableSupplierCredit,
                nameof(config.EnableLoyaltyProgram) or "LoyaltyProgram" => config.EnableLoyaltyProgram,
                nameof(config.EnableBatchExpiry) or "BatchExpiry" => config.EnableBatchExpiry,
                nameof(config.EnableScaleIntegration) or "ScaleIntegration" => config.EnableScaleIntegration,

                // Enterprise features
                nameof(config.EnablePayroll) or "Payroll" => config.EnablePayroll,
                nameof(config.EnableAccounting) or "Accounting" => config.EnableAccounting,
                nameof(config.EnableMultiStore) or "MultiStore" => config.EnableMultiStore,
                nameof(config.EnableCloudSync) or "CloudSync" => config.EnableCloudSync,

                // Kenya features
                nameof(config.EnableKenyaETims) or "KenyaETims" => config.EnableKenyaETims,
                nameof(config.EnableMpesa) or "Mpesa" => config.EnableMpesa,

                _ => true
            };
        }
        catch
        {
            return true;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts business mode to visibility for mode-specific UI elements.
/// </summary>
public class ModeVisibilityConverter : IValueConverter
{
    private static ISystemConfigurationService? _configService;

    private static ISystemConfigurationService ConfigService
    {
        get
        {
            if (_configService == null)
            {
                _configService = App.Services.GetService<ISystemConfigurationService>();
            }
            return _configService!;
        }
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is not string modeName)
        {
            return Visibility.Visible;
        }

        try
        {
            var config = ConfigService?.CachedConfiguration;
            if (config == null)
            {
                return Visibility.Visible;
            }

            return modeName.ToLowerInvariant() switch
            {
                "restaurant" => config.Mode == Core.Entities.BusinessMode.Restaurant ||
                               config.Mode == Core.Entities.BusinessMode.Hybrid
                               ? Visibility.Visible : Visibility.Collapsed,

                "supermarket" or "retail" => config.Mode == Core.Entities.BusinessMode.Supermarket ||
                                             config.Mode == Core.Entities.BusinessMode.Hybrid
                                             ? Visibility.Visible : Visibility.Collapsed,

                "hybrid" => config.Mode == Core.Entities.BusinessMode.Hybrid
                           ? Visibility.Visible : Visibility.Collapsed,

                _ => Visibility.Visible
            };
        }
        catch
        {
            return Visibility.Visible;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
