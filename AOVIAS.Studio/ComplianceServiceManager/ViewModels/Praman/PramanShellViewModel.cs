using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ComplianceServiceManager.Contracts.Praman;
using ComplianceServiceManager.ViewModels;

namespace ComplianceServiceManager.ViewModels.Praman;

/// <summary>
/// PRAMAN application workspace with nested operator sections.
/// </summary>
public sealed partial class PramanShellViewModel : ObservableObject
{
    private readonly IPramanPlatformRegistry platformRegistry;
    private readonly MainViewModel complianceViewModel;

    [ObservableProperty]
    private string selectedSection = SectionOverview;

    [ObservableProperty]
    private string statusMessage = "Loading PRAMAN platform registration...";

    [ObservableProperty]
    private string applicationDisplayName = "PRAMAN";

    public MainViewModel ComplianceViewModel => complianceViewModel;

    public ObservableCollection<PramanServiceDefinition> RegisteredServices { get; } = [];

    public const string SectionOverview = "Overview";
    public const string SectionCompliance = "Compliance";
    public const string SectionEvidence = "Evidence";
    public const string SectionAudit = "Audit";

    public bool IsOverviewSelected => SelectedSection == SectionOverview;
    public bool IsComplianceSelected => SelectedSection == SectionCompliance;
    public bool IsEvidenceSelected => SelectedSection == SectionEvidence;
    public bool IsAuditSelected => SelectedSection == SectionAudit;

    public PramanServiceDefinition? EvidenceService =>
        RegisteredServices.FirstOrDefault(service => service.ServiceId == "praman-knowledgebase");

    public PramanServiceDefinition? AuditService =>
        RegisteredServices.FirstOrDefault(service => service.ServiceId == "praman-audit");

    public PramanShellViewModel(IPramanPlatformRegistry platformRegistry, MainViewModel complianceViewModel)
    {
        this.platformRegistry = platformRegistry;
        this.complianceViewModel = complianceViewModel;
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        try
        {
            var registry = await platformRegistry.LoadAsync();
            ApplicationDisplayName = registry.DisplayName;
            RegisteredServices.Clear();
            foreach (var service in registry.Services.OrderBy(service => service.Category))
            {
                RegisteredServices.Add(service);
            }

            StatusMessage = $"{RegisteredServices.Count} PRAMAN workloads registered with AOVIAS Runtime.";
            OnPropertyChanged(nameof(EvidenceService));
            OnPropertyChanged(nameof(AuditService));
        }
        catch (Exception exception)
        {
            StatusMessage = exception.Message;
        }
    }

    [RelayCommand]
    private void ShowOverview() => SelectSection(SectionOverview);

    [RelayCommand]
    private void ShowCompliance() => SelectSection(SectionCompliance);

    [RelayCommand]
    private void ShowEvidence() => SelectSection(SectionEvidence);

    [RelayCommand]
    private void ShowAudit() => SelectSection(SectionAudit);

    private void SelectSection(string section)
    {
        SelectedSection = section;
        OnPropertyChanged(nameof(IsOverviewSelected));
        OnPropertyChanged(nameof(IsComplianceSelected));
        OnPropertyChanged(nameof(IsEvidenceSelected));
        OnPropertyChanged(nameof(IsAuditSelected));
    }
}
