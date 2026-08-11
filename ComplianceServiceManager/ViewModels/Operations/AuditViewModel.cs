using System.Collections.ObjectModel;
using Aovias.Release.Contracts.Releases;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ComplianceServiceManager.Contracts.Deployment;
using ComplianceServiceManager.Models;
using ComplianceServiceManager.Services;

namespace ComplianceServiceManager.ViewModels.Operations;

/// <summary>
/// Operations → Audit workspace: who did what, when. Combines the platform's deployment
/// history (attributable release operations) with the local repair audit trail.
/// </summary>
public sealed partial class AuditViewModel : ObservableObject
{
    private readonly IDeploymentService deploymentService;

    [ObservableProperty]
    private string statusMessage = "Loading audit records…";

    [ObservableProperty]
    private bool isBusy;

    public ObservableCollection<ReleaseHistoryEntry> DeploymentAudit { get; } = [];

    public ObservableCollection<RepairHistoryEntry> RepairAudit { get; }

    public AuditViewModel(IDeploymentService deploymentService, IRepairHistoryStore repairHistoryStore)
    {
        this.deploymentService = deploymentService;
        RepairAudit = repairHistoryStore.Entries;
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            DeploymentAudit.Clear();
            foreach (var entry in await deploymentService.GetHistoryAsync())
            {
                DeploymentAudit.Add(entry);
            }

            StatusMessage = DeploymentAudit.Count == 0
                ? "No platform operations have been recorded yet."
                : $"{DeploymentAudit.Count} deployment operation(s) on record.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Unable to load audit trail: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
