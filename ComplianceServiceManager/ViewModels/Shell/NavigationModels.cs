using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ComplianceServiceManager.ViewModels.Shell;

/// <summary>
/// One selectable destination in the AOVIAS Studio sidebar. Data-driven so the shell renders
/// every workspace through a single template instead of a hand-written button per workspace.
/// </summary>
public sealed partial class NavigationItemViewModel : ObservableObject
{
    private readonly Action<NavigationItemViewModel> navigate;

    public NavigationItemViewModel(string id, string title, string iconKey, Action<NavigationItemViewModel> navigate)
    {
        Id = id;
        Title = title;
        IconKey = iconKey;
        this.navigate = navigate;
    }

    public string Id { get; }

    public string Title { get; }

    public string IconKey { get; }

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private string badge = string.Empty;

    [RelayCommand]
    private void Navigate() => navigate(this);
}

/// <summary>
/// A labelled group of navigation items (PLATFORM, APPLICATIONS, OPERATIONS, ...).
/// </summary>
public sealed class NavigationSectionViewModel
{
    public NavigationSectionViewModel(string title, IEnumerable<NavigationItemViewModel> items)
    {
        Title = title;
        Items = new ObservableCollection<NavigationItemViewModel>(items);
    }

    public string Title { get; }

    public ObservableCollection<NavigationItemViewModel> Items { get; }

    public bool HasTitle => !string.IsNullOrWhiteSpace(Title);
}
