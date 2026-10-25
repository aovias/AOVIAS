using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ComplianceServiceManager.Helpers;

/// <summary>
/// Shows a section panel when the selected settings section matches the converter parameter.
/// </summary>
public sealed class SectionVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var selected = value?.ToString() ?? string.Empty;
        var section = parameter?.ToString() ?? string.Empty;
        return string.Equals(selected, section, StringComparison.OrdinalIgnoreCase)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
