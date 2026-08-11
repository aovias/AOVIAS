using System.Windows;
using System.Windows.Controls;

namespace Aovias.DesignSystem.Controls;

/// <summary>
/// Friendly empty state panel.
/// </summary>
public partial class EmptyState : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(EmptyState), new PropertyMetadata("Nothing here yet"));

    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(EmptyState), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IconGlyphProperty =
        DependencyProperty.Register(nameof(IconGlyph), typeof(string), typeof(EmptyState), new PropertyMetadata("\uE946"));

    public EmptyState()
    {
        InitializeComponent();
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

    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }
}
