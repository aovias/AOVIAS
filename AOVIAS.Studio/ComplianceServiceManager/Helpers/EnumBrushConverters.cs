using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ComplianceServiceManager.Models;

namespace ComplianceServiceManager.Helpers;

public sealed class StatusBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            ServiceStatus.Running => Brushes.LimeGreen,
            ServiceStatus.Stopped => Brushes.IndianRed,
            ServiceStatus.Starting or ServiceStatus.Stopping => Brushes.Gold,
            ServiceStatus.Error => Brushes.Orange,
            ServiceStatus.Diagnosing => Brushes.DeepSkyBlue,
            ServiceStatus.Fixing => Brushes.MediumPurple,
            ServiceStatus.Rebuilding => Brushes.SteelBlue,
            ServiceStatus.Restarting => Brushes.CadetBlue,
            ServiceStatus.Resolved => Brushes.LimeGreen,
            ServiceStatus.FailedRepair => Brushes.OrangeRed,
            _ => Brushes.Gray
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

public sealed class HealthBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            HealthStatus.Healthy => Brushes.LimeGreen,
            HealthStatus.Starting => Brushes.Gold,
            HealthStatus.Unreachable => Brushes.OrangeRed,
            _ => Brushes.Gray
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

public sealed class LogLevelBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            LogLevel.Error => Brushes.OrangeRed,
            LogLevel.Warning => Brushes.Gold,
            LogLevel.Build => Brushes.DeepSkyBlue,
            LogLevel.Process => Brushes.LightGreen,
            _ => Brushes.Gainsboro
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

public sealed class StatusBadgeBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            ServiceStatus.Running => new SolidColorBrush(Color.FromRgb(18, 86, 64)),
            ServiceStatus.Stopped => new SolidColorBrush(Color.FromRgb(92, 36, 46)),
            ServiceStatus.Starting or ServiceStatus.Stopping => new SolidColorBrush(Color.FromRgb(91, 72, 24)),
            ServiceStatus.Error => new SolidColorBrush(Color.FromRgb(108, 48, 31)),
            ServiceStatus.Diagnosing => new SolidColorBrush(Color.FromRgb(35, 72, 120)),
            ServiceStatus.Fixing => new SolidColorBrush(Color.FromRgb(76, 55, 120)),
            ServiceStatus.Rebuilding => new SolidColorBrush(Color.FromRgb(45, 67, 105)),
            ServiceStatus.Restarting => new SolidColorBrush(Color.FromRgb(35, 87, 102)),
            ServiceStatus.Resolved => new SolidColorBrush(Color.FromRgb(18, 86, 64)),
            ServiceStatus.FailedRepair => new SolidColorBrush(Color.FromRgb(105, 38, 38)),
            _ => new SolidColorBrush(Color.FromRgb(52, 61, 78))
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
