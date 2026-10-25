using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Aovias.DesignSystem.Controls;

/// <summary>
/// The combined, chronological structured-log view for an Operation Engine run — search, auto-
/// scroll, clear. Mirrors the "Pipeline Console" pattern already used for Publishing/Bootstrap, now
/// shared. Deliberately does not filter internally: <see cref="LogLines"/> is rendered as given, and
/// <see cref="SearchText"/> is a two-way bound value the consumer applies to its own source
/// collection — keeping this control ignorant of any specific filtering/collection-view mechanism.
/// </summary>
public partial class OperationLogPane : UserControl
{
    public static readonly DependencyProperty LogLinesProperty =
        DependencyProperty.Register(nameof(LogLines), typeof(IEnumerable), typeof(OperationLogPane), new PropertyMetadata(null, OnLogLinesChanged));

    public static readonly DependencyProperty SearchTextProperty =
        DependencyProperty.Register(nameof(SearchText), typeof(string), typeof(OperationLogPane), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty AutoScrollProperty =
        DependencyProperty.Register(nameof(AutoScroll), typeof(bool), typeof(OperationLogPane), new PropertyMetadata(true));

    public static readonly DependencyProperty ClearCommandProperty =
        DependencyProperty.Register(nameof(ClearCommand), typeof(ICommand), typeof(OperationLogPane), new PropertyMetadata(null));

    public OperationLogPane()
    {
        InitializeComponent();
    }

    public IEnumerable LogLines
    {
        get => (IEnumerable)GetValue(LogLinesProperty);
        set => SetValue(LogLinesProperty, value);
    }

    public string SearchText
    {
        get => (string)GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    public bool AutoScroll
    {
        get => (bool)GetValue(AutoScrollProperty);
        set => SetValue(AutoScrollProperty, value);
    }

    public ICommand? ClearCommand
    {
        get => (ICommand?)GetValue(ClearCommandProperty);
        set => SetValue(ClearCommandProperty, value);
    }

    private static void OnLogLinesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var pane = (OperationLogPane)d;
        if (e.OldValue is INotifyCollectionChanged oldNotifying)
        {
            oldNotifying.CollectionChanged -= pane.OnLogLinesCollectionChanged;
        }

        if (e.NewValue is INotifyCollectionChanged newNotifying)
        {
            newNotifying.CollectionChanged += pane.OnLogLinesCollectionChanged;
        }
    }

    private void OnLogLinesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!AutoScroll || e.Action != NotifyCollectionChangedAction.Add)
        {
            return;
        }

        // Deferred, not called inline: ScrollIntoView forces container generation/layout, and doing
        // that synchronously from inside the very CollectionChanged notification that triggered it
        // races the ItemContainerGenerator against itself — the documented cause of "ItemsControl is
        // inconsistent with its ItemsSource" under back-to-back Add events. Posting past the current
        // dispatch (Background priority, after layout has settled) lets the generator finish
        // processing this notification first.
        Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
        {
            if (LogList.Items.Count > 0)
            {
                LogList.ScrollIntoView(LogList.Items[^1]);
            }
        });
    }
}
