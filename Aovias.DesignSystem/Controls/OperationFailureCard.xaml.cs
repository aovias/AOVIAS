using System.Windows;
using System.Windows.Controls;

namespace Aovias.DesignSystem.Controls;

/// <summary>
/// Categorized failure card for an Operation Engine run: Problem / Cause / Recommended Action,
/// a Retry button, and an "Advanced Details" expander for the raw diagnostic — never the raw
/// exception text inline. Generic — takes only primitive values, no dependency on the Operation
/// Engine's own <c>OperationFailure</c> type (this assembly never references logic/domain
/// projects).
/// </summary>
public partial class OperationFailureCard : UserControl
{
    public static readonly DependencyProperty ProblemProperty =
        DependencyProperty.Register(nameof(Problem), typeof(string), typeof(OperationFailureCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty CauseProperty =
        DependencyProperty.Register(nameof(Cause), typeof(string), typeof(OperationFailureCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty RecommendedActionProperty =
        DependencyProperty.Register(nameof(RecommendedAction), typeof(string), typeof(OperationFailureCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DetailsProperty =
        DependencyProperty.Register(nameof(Details), typeof(string), typeof(OperationFailureCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty CanRetryProperty =
        DependencyProperty.Register(nameof(CanRetry), typeof(bool), typeof(OperationFailureCard), new PropertyMetadata(true));

    public static readonly DependencyProperty RetryCommandProperty =
        DependencyProperty.Register(nameof(RetryCommand), typeof(System.Windows.Input.ICommand), typeof(OperationFailureCard), new PropertyMetadata(null));

    public OperationFailureCard()
    {
        InitializeComponent();
    }

    public string Problem
    {
        get => (string)GetValue(ProblemProperty);
        set => SetValue(ProblemProperty, value);
    }

    public string Cause
    {
        get => (string)GetValue(CauseProperty);
        set => SetValue(CauseProperty, value);
    }

    public string RecommendedAction
    {
        get => (string)GetValue(RecommendedActionProperty);
        set => SetValue(RecommendedActionProperty, value);
    }

    public string Details
    {
        get => (string)GetValue(DetailsProperty);
        set => SetValue(DetailsProperty, value);
    }

    public bool CanRetry
    {
        get => (bool)GetValue(CanRetryProperty);
        set => SetValue(CanRetryProperty, value);
    }

    public System.Windows.Input.ICommand? RetryCommand
    {
        get => (System.Windows.Input.ICommand?)GetValue(RetryCommandProperty);
        set => SetValue(RetryCommandProperty, value);
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
            // Clipboard can transiently be locked by another process — not worth surfacing to the operator.
        }
    }
}
