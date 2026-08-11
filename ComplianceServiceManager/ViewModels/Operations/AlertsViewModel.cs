using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ComplianceServiceManager.Logging;
using ComplianceServiceManager.Models;

namespace ComplianceServiceManager.ViewModels.Operations;

/// <summary>
/// Operations → Alerts workspace: warnings and errors raised anywhere in the Studio session,
/// filterable and searchable. A live view over the central logging service — the same entries,
/// pre-filtered to what an operator needs to act on.
/// </summary>
public sealed partial class AlertsViewModel : ObservableObject
{
    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool showWarnings = true;

    [ObservableProperty]
    private bool showErrors = true;

    public ICollectionView AlertsView { get; }

    public AlertsViewModel(ILoggingService loggingService)
    {
        AlertsView = new CollectionViewSource { Source = loggingService.Entries }.View;
        AlertsView.Filter = FilterAlert;
        if (AlertsView is ICollectionViewLiveShaping live && live.CanChangeLiveFiltering)
        {
            live.LiveFilteringProperties.Add(nameof(LogEntryModel.Level));
            live.IsLiveFiltering = true;
        }
    }

    private bool FilterAlert(object candidate)
    {
        if (candidate is not LogEntryModel entry)
        {
            return false;
        }

        var levelMatch = (entry.Level == LogLevel.Warning && ShowWarnings)
            || (entry.Level == LogLevel.Error && ShowErrors);
        if (!levelMatch)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        return entry.Message.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || entry.Source.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
    }

    partial void OnSearchTextChanged(string value) => AlertsView.Refresh();

    partial void OnShowWarningsChanged(bool value) => AlertsView.Refresh();

    partial void OnShowErrorsChanged(bool value) => AlertsView.Refresh();

    [RelayCommand]
    private void Refresh() => AlertsView.Refresh();
}
