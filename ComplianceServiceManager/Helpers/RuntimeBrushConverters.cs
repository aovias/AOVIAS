using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using ComplianceServiceManager.Models.Runtime;

namespace ComplianceServiceManager.Helpers;

/// <summary>
/// Converts <see cref="RuntimeHealth"/> values to display brushes.
/// </summary>
public sealed class RuntimeHealthBrushConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            RuntimeHealth.Healthy or RuntimeHealth.Running or RuntimeHealth.Valid => new SolidColorBrush(Color.FromRgb(34, 197, 94)),
            RuntimeHealth.Degraded or RuntimeHealth.Warning => new SolidColorBrush(Color.FromRgb(249, 115, 22)),
            RuntimeHealth.NotInstalled => new SolidColorBrush(Color.FromRgb(148, 163, 184)),
            RuntimeHealth.Stopped or RuntimeHealth.Unreachable => new SolidColorBrush(Color.FromRgb(244, 63, 94)),
            _ => new SolidColorBrush(Color.FromRgb(100, 116, 139))
        };
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

/// <summary>
/// Converts numeric usage values to progress bar widths for mock gauges.
/// </summary>
public sealed class PercentageToGridLengthConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double percentage)
        {
            return new GridLength(Math.Clamp(percentage, 0, 100), GridUnitType.Star);
        }

        if (value is float floatPercentage)
        {
            return new GridLength(Math.Clamp(floatPercentage, 0, 100), GridUnitType.Star);
        }

        return new GridLength(0, GridUnitType.Star);
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
