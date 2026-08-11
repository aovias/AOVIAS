using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Aovias.DesignSystem.Enums;

namespace Aovias.DesignSystem.Controls;

/// <summary>
/// Enterprise KPI card with title, metrics, status, and optional action.
/// </summary>
public partial class MetricCard : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(MetricCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty PrimaryMetricProperty =
        DependencyProperty.Register(nameof(PrimaryMetric), typeof(string), typeof(MetricCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SecondaryMetricProperty =
        DependencyProperty.Register(nameof(SecondaryMetric), typeof(string), typeof(MetricCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty StatusTextProperty =
        DependencyProperty.Register(nameof(StatusText), typeof(string), typeof(MetricCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty StatusKindProperty =
        DependencyProperty.Register(nameof(StatusKind), typeof(StatusKind), typeof(MetricCard), new PropertyMetadata(StatusKind.Neutral));

    public static readonly DependencyProperty ActionTextProperty =
        DependencyProperty.Register(nameof(ActionText), typeof(string), typeof(MetricCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ActionCommandProperty =
        DependencyProperty.Register(nameof(ActionCommand), typeof(ICommand), typeof(MetricCard), new PropertyMetadata(null));

    public MetricCard()
    {
        InitializeComponent();
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string PrimaryMetric
    {
        get => (string)GetValue(PrimaryMetricProperty);
        set => SetValue(PrimaryMetricProperty, value);
    }

    public string SecondaryMetric
    {
        get => (string)GetValue(SecondaryMetricProperty);
        set => SetValue(SecondaryMetricProperty, value);
    }

    public string StatusText
    {
        get => (string)GetValue(StatusTextProperty);
        set => SetValue(StatusTextProperty, value);
    }

    public StatusKind StatusKind
    {
        get => (StatusKind)GetValue(StatusKindProperty);
        set => SetValue(StatusKindProperty, value);
    }

    public string ActionText
    {
        get => (string)GetValue(ActionTextProperty);
        set => SetValue(ActionTextProperty, value);
    }

    public ICommand? ActionCommand
    {
        get => (ICommand?)GetValue(ActionCommandProperty);
        set => SetValue(ActionCommandProperty, value);
    }
}
