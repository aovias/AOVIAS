using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Aovias.DesignSystem.Controls;

/// <summary>
/// Quick action tile for overview dashboards.
/// </summary>
public partial class QuickActionCard : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(QuickActionCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(nameof(Description), typeof(string), typeof(QuickActionCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IconGlyphProperty =
        DependencyProperty.Register(nameof(IconGlyph), typeof(string), typeof(QuickActionCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(QuickActionCard), new PropertyMetadata(null));

    public QuickActionCard()
    {
        InitializeComponent();
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }
}
