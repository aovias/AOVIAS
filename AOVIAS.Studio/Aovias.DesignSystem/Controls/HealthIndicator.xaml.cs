using System.Windows;
using System.Windows.Controls;
using Aovias.DesignSystem.Enums;

namespace Aovias.DesignSystem.Controls;

/// <summary>
/// Compact health indicator dot with label.
/// </summary>
public partial class HealthIndicator : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(HealthIndicator), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty StatusKindProperty =
        DependencyProperty.Register(nameof(StatusKind), typeof(StatusKind), typeof(HealthIndicator), new PropertyMetadata(StatusKind.Healthy));

    public HealthIndicator()
    {
        InitializeComponent();
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public StatusKind StatusKind
    {
        get => (StatusKind)GetValue(StatusKindProperty);
        set => SetValue(StatusKindProperty, value);
    }
}
