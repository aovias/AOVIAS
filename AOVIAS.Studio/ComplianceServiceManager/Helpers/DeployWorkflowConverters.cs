using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ComplianceServiceManager.Helpers;

/// <summary>Null → <see cref="Visibility.Collapsed"/>; anything else → Visible. Used by the unified Deploy workflow to reveal detail panels only once a selection exists.</summary>
public sealed class NullToCollapsedConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is null ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>True when both bound values are the same instance/equal — the unified Deploy workflow's selected-card highlight (WPF offers no converter-free way to compare two bindings).</summary>
public sealed class EqualityToBoolConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
        => values.Length == 2 && values[0] is not null && Equals(values[0], values[1]);

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
