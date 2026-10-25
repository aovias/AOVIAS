using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace ComplianceServiceManager.Helpers;

public static class AutoScrollBehavior
{
    public static readonly DependencyProperty AutoScrollProperty =
        DependencyProperty.RegisterAttached(
            "AutoScroll",
            typeof(bool),
            typeof(AutoScrollBehavior),
            new PropertyMetadata(false, OnAutoScrollChanged));

    public static bool GetAutoScroll(DependencyObject element) => (bool)element.GetValue(AutoScrollProperty);

    public static void SetAutoScroll(DependencyObject element, bool value) => element.SetValue(AutoScrollProperty, value);

    private static void OnAutoScrollChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is not ListBox listBox)
        {
            return;
        }

        if (listBox.Items.SourceCollection is INotifyCollectionChanged notifyCollection)
        {
            notifyCollection.CollectionChanged += (_, _) =>
            {
                if (GetAutoScroll(listBox) && listBox.Items.Count > 0)
                {
                    listBox.ScrollIntoView(listBox.Items[^1]);
                }
            };
        }
    }
}
