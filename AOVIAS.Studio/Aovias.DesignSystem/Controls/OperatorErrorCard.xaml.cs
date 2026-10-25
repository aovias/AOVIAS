using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Aovias.DesignSystem.Controls;

/// <summary>
/// Enterprise error presentation card: categorized title, plain-language problem statement,
/// recommended operator actions, and an expandable technical-details panel. Raw diagnostics
/// are never shown as the primary content — they live behind the details expander, copyable.
/// </summary>
public partial class OperatorErrorCard : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(OperatorErrorCard), new PropertyMetadata("Operation failed"));

    public static readonly DependencyProperty CategoryProperty =
        DependencyProperty.Register(nameof(Category), typeof(string), typeof(OperatorErrorCard), new PropertyMetadata("Error"));

    public static readonly DependencyProperty SubjectProperty =
        DependencyProperty.Register(nameof(Subject), typeof(string), typeof(OperatorErrorCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SubjectDetailProperty =
        DependencyProperty.Register(nameof(SubjectDetail), typeof(string), typeof(OperatorErrorCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ProblemProperty =
        DependencyProperty.Register(nameof(Problem), typeof(string), typeof(OperatorErrorCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DetailsProperty =
        DependencyProperty.Register(nameof(Details), typeof(string), typeof(OperatorErrorCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty RecommendedActionsProperty =
        DependencyProperty.Register(
            nameof(RecommendedActions),
            typeof(IEnumerable),
            typeof(OperatorErrorCard),
            new PropertyMetadata(null, static (d, _) => ((OperatorErrorCard)d).UpdateActionsVisibility()));

    public static readonly DependencyProperty AccentBrushProperty =
        DependencyProperty.Register(nameof(AccentBrush), typeof(Brush), typeof(OperatorErrorCard), new PropertyMetadata(Brushes.IndianRed));

    private static readonly DependencyPropertyKey RecommendedActionsVisibilityKey =
        DependencyProperty.RegisterReadOnly(
            nameof(RecommendedActionsVisibility),
            typeof(Visibility),
            typeof(OperatorErrorCard),
            new PropertyMetadata(Visibility.Collapsed));

    public static readonly DependencyProperty RecommendedActionsVisibilityProperty =
        RecommendedActionsVisibilityKey.DependencyProperty;

    public OperatorErrorCard()
    {
        InitializeComponent();
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Category
    {
        get => (string)GetValue(CategoryProperty);
        set => SetValue(CategoryProperty, value);
    }

    public string Subject
    {
        get => (string)GetValue(SubjectProperty);
        set => SetValue(SubjectProperty, value);
    }

    public string SubjectDetail
    {
        get => (string)GetValue(SubjectDetailProperty);
        set => SetValue(SubjectDetailProperty, value);
    }

    public string Problem
    {
        get => (string)GetValue(ProblemProperty);
        set => SetValue(ProblemProperty, value);
    }

    public string Details
    {
        get => (string)GetValue(DetailsProperty);
        set => SetValue(DetailsProperty, value);
    }

    public IEnumerable? RecommendedActions
    {
        get => (IEnumerable?)GetValue(RecommendedActionsProperty);
        set => SetValue(RecommendedActionsProperty, value);
    }

    public Brush AccentBrush
    {
        get => (Brush)GetValue(AccentBrushProperty);
        set => SetValue(AccentBrushProperty, value);
    }

    public Visibility RecommendedActionsVisibility => (Visibility)GetValue(RecommendedActionsVisibilityProperty);

    private void UpdateActionsVisibility()
    {
        var hasItems = RecommendedActions?.GetEnumerator().MoveNext() == true;
        SetValue(RecommendedActionsVisibilityKey, hasItems ? Visibility.Visible : Visibility.Collapsed);
    }

    private void OnCopyDetails(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(Details))
            {
                Clipboard.SetText(Details);
            }
        }
        catch
        {
            // Clipboard access can fail transiently when another process holds it; non-fatal.
        }
    }
}
