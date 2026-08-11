using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ComplianceServiceManager.Helpers;

/// <summary>
/// Shows an element only when the bound wizard step index equals the converter parameter,
/// letting a pipeline view declare one section per step without code-behind switching.
/// </summary>
public sealed class StepVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int step
            && parameter is string expectedText
            && int.TryParse(expectedText, out var expected))
        {
            return step == expected ? Visibility.Visible : Visibility.Collapsed;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Visible when the bound value is non-null (e.g. an optional port number badge).
/// </summary>
public sealed class NotNullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is null ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Flips a bool — e.g. binding a second RadioButton's IsChecked to the logical opposite of the property the first one already owns.</summary>
public sealed class InverseBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool boolValue && !boolValue;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool boolValue && !boolValue;
}

/// <summary>Visible when the bound bool is false — e.g. showing a "Password" field only when a
/// "Use SSH key" toggle is off. Distinct from <see cref="InverseBooleanConverter"/>, which returns a
/// bool: binding that directly to a Visibility property fails silently (WPF leaves the element at
/// its default Visible rather than throwing), which is exactly the bug this type exists to avoid.</summary>
public sealed class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool boolValue && !boolValue ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
