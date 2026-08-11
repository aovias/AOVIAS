using System.Collections.ObjectModel;
using ComplianceServiceManager.Models;

namespace ComplianceServiceManager.Services;

public interface IRepairHistoryStore
{
    ObservableCollection<RepairHistoryEntry> Entries { get; }

    void Add(RepairHistoryEntry entry);
}
