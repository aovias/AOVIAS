using System.Collections.ObjectModel;
using System.Windows;
using ComplianceServiceManager.Models;

namespace ComplianceServiceManager.Services;

public sealed class RepairHistoryStore : IRepairHistoryStore
{
    public ObservableCollection<RepairHistoryEntry> Entries { get; } = new();

    public void Add(RepairHistoryEntry entry)
    {
        // The collection is bound to the UI, but Add may be called from a background fix thread,
        // so marshal the mutation onto the dispatcher to avoid cross-thread access exceptions.
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(() => AddCore(entry));
            return;
        }

        AddCore(entry);
    }

    private void AddCore(RepairHistoryEntry entry)
    {
        Entries.Insert(0, entry);
        while (Entries.Count > 100)
        {
            Entries.RemoveAt(Entries.Count - 1);
        }
    }
}
