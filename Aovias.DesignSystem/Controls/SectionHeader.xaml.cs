using System.Windows;
using System.Windows.Controls;

namespace Aovias.DesignSystem.Controls;

/// <summary>
/// Workspace section header with title and subtitle.
/// </summary>
public partial class SectionHeader : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(SectionHeader), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(SectionHeader), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IconGlyphProperty =
        DependencyProperty.Register(nameof(IconGlyph), typeof(string), typeof(SectionHeader), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ActionContentProperty =
        DependencyProperty.Register(nameof(ActionContent), typeof(object), typeof(SectionHeader), new PropertyMetadata(null));

    public SectionHeader()
    {
        InitializeComponent();
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    public object? ActionContent
    {
        get => GetValue(ActionContentProperty);
        set => SetValue(ActionContentProperty, value);
    }
}
