using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Aovias.DesignSystem.Enums;

namespace Aovias.DesignSystem.Converters;

/// <summary>
/// Maps <see cref="StatusKind"/> values to themed foreground brushes.
/// </summary>
public sealed class StatusKindToBrushConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var kind = value is StatusKind statusKind ? statusKind : StatusKind.Neutral;
        var resourceKey = kind switch
        {
            StatusKind.Healthy => "StatusHealthyBrush",
            StatusKind.Warning => "StatusWarningBrush",
            StatusKind.Critical => "StatusCriticalBrush",
            StatusKind.Information => "StatusInfoBrush",
            StatusKind.Deployment => "StatusDeploymentBrush",
            StatusKind.Ai => "StatusAiBrush",
            StatusKind.Compliance => "StatusComplianceBrush",
            _ => "TextSecondaryBrush"
        };

        return Application.Current.TryFindResource(resourceKey) as Brush
            ?? Brushes.Gray;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
