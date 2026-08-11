using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Aovias.DesignSystem.Converters;

/// <summary>
/// Converts non-empty strings to <see cref="Visibility.Visible"/>.
/// </summary>
public sealed class StringNotEmptyToVisibilityConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
