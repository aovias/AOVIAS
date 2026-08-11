using System.Windows;
using System.Windows.Controls;
using Aovias.DesignSystem.Enums;

namespace Aovias.DesignSystem.Controls;

/// <summary>
/// Compact semantic status chip.
/// </summary>
public partial class StatusBadge : UserControl
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(StatusBadge), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty StatusKindProperty =
        DependencyProperty.Register(nameof(StatusKind), typeof(StatusKind), typeof(StatusBadge), new PropertyMetadata(StatusKind.Neutral));

    public StatusBadge()
    {
        InitializeComponent();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public StatusKind StatusKind
    {
        get => (StatusKind)GetValue(StatusKindProperty);
        set => SetValue(StatusKindProperty, value);
    }
}
