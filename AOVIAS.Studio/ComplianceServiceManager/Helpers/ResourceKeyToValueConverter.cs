using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ComplianceServiceManager.Helpers;

/// <summary>
/// Resolves a resource key (e.g. an icon glyph key from the design system) to its value,
/// letting data-driven templates reference theme resources by name.
/// </summary>
public sealed class ResourceKeyToValueConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string key && !string.IsNullOrWhiteSpace(key))
        {
            return Application.Current.TryFindResource(key) ?? string.Empty;
        }

        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
