using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using ComplianceServiceManager.ViewModels.Applications;

namespace ComplianceServiceManager.Views.Applications;

/// <summary>
/// Applications → Deploy: the single, unified deployment workflow view.
/// Code-behind owns only visuals the view model must never touch: bringing a
/// stepper-selected section into view, auto-scrolling the live log feed, and
/// kicking the underlying deployment view model's one-time initialization.
/// </summary>
public partial class DeploymentsWorkspaceView : UserControl
{
    private DeploymentsWorkspaceViewModel? attachedViewModel;
    private bool initialized;

    public DeploymentsWorkspaceView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (attachedViewModel is not null)
        {
            attachedViewModel.SectionRequested -= OnSectionRequested;
            attachedViewModel.FilteredConsoleEntries.CollectionChanged -= OnConsoleEntriesChanged;
        }

        attachedViewModel = e.NewValue as DeploymentsWorkspaceViewModel;
        if (attachedViewModel is not null)
        {
            attachedViewModel.SectionRequested += OnSectionRequested;
            attachedViewModel.FilteredConsoleEntries.CollectionChanged += OnConsoleEntriesChanged;
        }
    }

    private async void DeploymentsWorkspaceView_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (initialized || attachedViewModel is null)
        {
            return;
        }

        initialized = true;
        try
        {
            await attachedViewModel.Center.InitializeCommand.ExecuteAsync(null);
        }
        catch (Exception exception)
        {
            attachedViewModel.Center.StatusMessage = $"Deployment workspace failed to load: {exception.Message}";
        }
    }

    private void OnSectionRequested(string sectionName)
    {
        if (FindName(sectionName) is FrameworkElement section)
        {
            section.BringIntoView();
        }
    }

    private void OnConsoleEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Live logs auto-scroll — newest entry always visible without the operator chasing it.
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            LogsScroll.ScrollToEnd();
        }
    }
}
