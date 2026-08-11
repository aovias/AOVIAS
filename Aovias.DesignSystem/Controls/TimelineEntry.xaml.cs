using System.Windows;
using System.Windows.Controls;
using Aovias.DesignSystem.Enums;

namespace Aovias.DesignSystem.Controls;

/// <summary>
/// Timeline row for live activity feeds.
/// </summary>
public partial class TimelineEntry : UserControl
{
    public static readonly DependencyProperty TimestampProperty =
        DependencyProperty.Register(nameof(Timestamp), typeof(string), typeof(TimelineEntry), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(TimelineEntry), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(TimelineEntry), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty CategoryProperty =
        DependencyProperty.Register(nameof(Category), typeof(string), typeof(TimelineEntry), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty StatusKindProperty =
        DependencyProperty.Register(nameof(StatusKind), typeof(StatusKind), typeof(TimelineEntry), new PropertyMetadata(StatusKind.Information));

    public TimelineEntry()
    {
        InitializeComponent();
    }

    public string Timestamp
    {
        get => (string)GetValue(TimestampProperty);
        set => SetValue(TimestampProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public string Category
    {
        get => (string)GetValue(CategoryProperty);
        set => SetValue(CategoryProperty, value);
    }

    public StatusKind StatusKind
    {
        get => (StatusKind)GetValue(StatusKindProperty);
        set => SetValue(StatusKindProperty, value);
    }
}
